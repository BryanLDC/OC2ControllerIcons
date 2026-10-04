using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace OC2ControllerIcons.Patches
{
    // Menus / UI: every controller button is shown with the generic icon (four circles
    // with the button to press filled in). When all players use the same layout, that
    // layout's real buttons are shown instead.
    //
    // All of the game's UI (ButtonImage, PlayerButtonImage, DeviceIconSwap,
    // EmbeddedDeviceIconTextLookup...) ends up in ControllerIconLookup.GetIcon.
    // Per-player in-game icons do NOT go through here (IconLibrary.GetForLayout reads the sets directly).
    public static class MenuIconPatch
    {
        private const BindingFlags Inst = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static FieldInfo s_embeddedSemantics;
        private static FieldInfo s_buttonImageButton, s_buttonImageDevice;
        private static MethodInfo s_buttonImageSetData;

        public static void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(ControllerIconLookup), "GetIcon"),
                new HarmonyMethod(typeof(MenuIconPatch), "GetIconPrefix"));

            // Chalkboard "Player N press [button] / Space to join locally": whoever joins may hold
            // any controller, so this prompt always uses the generic icon. (The "rejoin" popup
            // follows the normal rule.)
            foreach (Type t in new Type[] { typeof(EmbeddedDeviceIconTextLookup), typeof(PCDisconnectIconTextLookup) })
            {
                harmony.Patch(AccessTools.Method(t, "GetIcon"),
                    new HarmonyMethod(typeof(MenuIconPatch), "JoinPromptPrefix"),
                    new HarmonyMethod(typeof(MenuIconPatch), "JoinPromptPostfix"));
            }

            // The only hard-coded prompt in the game: the "press A / Space to rejoin" popup forces
            // an Xbox A sprite on PC (m_iconOverridesPC). Route it through the normal rule instead.
            s_embeddedOverrides = typeof(EmbeddedDeviceIconTextLookup).GetField("m_iconOverridesPC", Inst);
            s_embeddedButtons = typeof(EmbeddedDeviceIconTextLookup).GetField("m_buttons", Inst);
            harmony.Patch(AccessTools.Method(typeof(EmbeddedDeviceIconTextLookup), "GetIcon"),
                new HarmonyMethod(typeof(MenuIconPatch), "OverriddenPromptPrefix"));

            s_embeddedSemantics = typeof(EmbeddedContextualIconTextLookup).GetField("m_sprites", Inst);
            harmony.Patch(AccessTools.Method(typeof(EmbeddedContextualIconTextLookup), "GetIcon"),
                new HarmonyMethod(typeof(MenuIconPatch), "EmbeddedSemanticPrefix"));

            s_buttonImageButton = typeof(ButtonImage).GetField("m_buton", Inst);
            s_buttonImageDevice = typeof(ButtonImage).GetField("m_device", Inst);
            s_buttonImageSetData = typeof(ButtonImage).GetMethod("SetData", Inst);

            ModSettings.Changed += RefreshAllUiIcons;
        }

        private static bool GetIconPrefix(ControllerIconLookup __instance, ControlPadInput.Button _button, ControllerIconLookup.IconContext _context,
                                          ControllerIconLookup.DeviceContext _device, ref Sprite __result)
        {
            if (!ModSettings.Enabled || _device != ControllerIconLookup.DeviceContext.Pad) return true;

            // Same Accept/Cancel swap the game performs in ButtonIcons.GetSprite.
            ControlPadInput.Button button = _button;
            if (PlayerManagerShared<PCPlayerManager.PCPlayerProfile>.AcceptAndCancelButtonsInverted)
            {
                if (button == ControlPadInput.Button.A) button = ControlPadInput.Button.B;
                else if (button == ControlPadInput.Button.B) button = ControlPadInput.Button.A;
            }
            Sprite sprite = UiIcon(__instance, button, _context);
            if (sprite == null) return true;
            __result = sprite;
            return false;
        }

        // Icon for a UI prompt: the shared layout's button if every player uses the same
        // layout, otherwise the generic icon. (ButtonIcons are read directly, so this never
        // re-enters ControllerIconLookup.GetIcon.)
        private static FieldInfo s_embeddedOverrides, s_embeddedButtons;

        private static bool OverriddenPromptPrefix(EmbeddedDeviceIconTextLookup __instance, int _materialNum, ref Sprite __result)
        {
            if (!ModSettings.Enabled || (object)s_embeddedOverrides == null || (object)s_embeddedButtons == null) return true;
            try
            {
                Sprite[] overrides = s_embeddedOverrides.GetValue(__instance) as Sprite[];
                if (overrides == null || _materialNum < 0 || _materialNum >= overrides.Length || overrides[_materialNum] == null) return true;
                ControlPadInput.Button[] buttons = s_embeddedButtons.GetValue(__instance) as ControlPadInput.Button[];
                if (buttons == null || _materialNum >= buttons.Length) return true;

                // Same device logic as the game's non-override path.
                PlayerManager pm = GameUtils.RequestManager<PlayerManager>();
                ControllerIconLookup lookup = GameUtils.RequestManager<ControllerIconLookup>();
                if (pm == null || lookup == null) return true;
                ControllerIconLookup.DeviceContext device = KeyboardUtils.IsKeyboard(PlayerInputLookup.Player.One)
                    ? ControllerIconLookup.DeviceContext.Keyboard
                    : PlayerButtonImage.GetDevice(pm, PlayerInputLookup.Player.One);
                __result = lookup.GetIcon(buttons[_materialNum], ControllerIconLookup.IconContext.Bordered, device);
                return false;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("OverriddenPromptPrefix: " + ex.Message);
                return true;
            }
        }

        private static bool s_forceGeneric;
        private static readonly Dictionary<int, bool> s_joinPrompts = new Dictionary<int, bool>();

        private static void JoinPromptPrefix(MonoBehaviour __instance)
        {
            s_forceGeneric = IsJoinPrompt(__instance);
        }

        private static void JoinPromptPostfix()
        {
            s_forceGeneric = false;
        }

        // Chalkboard "Player N press ... / Space to join locally" message.
        private static bool IsJoinPrompt(MonoBehaviour text)
        {
            int id = text.GetInstanceID();
            bool result;
            if (s_joinPrompts.TryGetValue(id, out result)) return result;
            result = false;
            Transform parent = text.transform.parent;
            if (text.name == "HostMessage" && parent != null && parent.name == "ChalkboardMessage") result = true;
            s_joinPrompts[id] = result;
            return result;
        }

        private static Sprite UiIcon(ControllerIconLookup lookup, ControlPadInput.Button button, ControllerIconLookup.IconContext context)
        {
            PadLayout uniform;
            if (!s_forceGeneric && ModSettings.TryGetUniformLayout(out uniform))
            {
                Sprite sprite = IconLibrary.GetForLayout(lookup, button, context, uniform);
                if (sprite != null) return sprite;
            }
            return IconLibrary.GetGeneric(button, context);
        }

        // UI text with embedded "semantic" icons (e.g. "Press [pick up] to...").
        private static bool EmbeddedSemanticPrefix(EmbeddedContextualIconTextLookup __instance, int _materialNum, ref Sprite __result)
        {
            if (!ModSettings.Enabled || (object)s_embeddedSemantics == null) return true;
            try
            {
                SemanticIconLookup.Semantic[] semantics = s_embeddedSemantics.GetValue(__instance) as SemanticIconLookup.Semantic[];
                if (semantics == null || _materialNum < 0 || _materialNum >= semantics.Length) return true;

                PlayerManager pm = GameUtils.RequestManager<PlayerManager>();
                if (pm == null) return true;
                PlayerInputLookup.Player p1 = PlayerInputLookup.Player.One;
                ControllerIconLookup.DeviceContext device = PlayerButtonImage.GetDevice(pm, p1);
                if (device != ControllerIconLookup.DeviceContext.Pad) return true;

                ControlPadInput.Button? button = PlayerButtonImage.GetControlPadButton<ControlPadInput.Button>(
                    IconLibrary.SemanticToLogical(semantics[_materialNum]), p1, device);
                if (!button.HasValue) return true;

                Sprite sprite = UiIcon(GameUtils.RequestManager<ControllerIconLookup>(), button.Value,
                    ControllerIconLookup.IconContext.Bordered);
                if (sprite == null) return true;
                __result = sprite;
                return false;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("EmbeddedSemanticPrefix: " + ex.Message);
                return true;
            }
        }

        // When the mod is toggled, refresh what is already on screen.
        private static void RefreshAllUiIcons()
        {
            // PlayerButtonImage, DeviceIconSwap, EmbeddedDeviceIconTextLookup, ButtonHoverIcon... subscribe to this.
            if (PlayerInputLookup.OnRegenerateControls != null) PlayerInputLookup.OnRegenerateControls();

            // ButtonImage only computes its icon in Awake/SetData.
            if ((object)s_buttonImageSetData == null) return;
            ButtonImage[] images = Resources.FindObjectsOfTypeAll<ButtonImage>();
            for (int i = 0; i < images.Length; i++)
            {
                ButtonImage img = images[i];
                if (img == null || !img.gameObject.scene.IsValid()) continue; // skip prefabs
                try
                {
                    s_buttonImageSetData.Invoke(img, new object[] { s_buttonImageButton.GetValue(img), s_buttonImageDevice.GetValue(img) });
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
