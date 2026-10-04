using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace OC2ControllerIcons.Patches
{
    // Menus / UI: every controller button is shown with the generic icon (four circles
    // with the button to press filled in).
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

            s_embeddedSemantics = typeof(EmbeddedContextualIconTextLookup).GetField("m_sprites", Inst);
            harmony.Patch(AccessTools.Method(typeof(EmbeddedContextualIconTextLookup), "GetIcon"),
                new HarmonyMethod(typeof(MenuIconPatch), "EmbeddedSemanticPrefix"));

            s_buttonImageButton = typeof(ButtonImage).GetField("m_buton", Inst);
            s_buttonImageDevice = typeof(ButtonImage).GetField("m_device", Inst);
            s_buttonImageSetData = typeof(ButtonImage).GetMethod("SetData", Inst);

            ModSettings.Changed += RefreshAllUiIcons;
        }

        private static bool GetIconPrefix(ControlPadInput.Button _button, ControllerIconLookup.IconContext _context,
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
            Sprite generic = IconLibrary.GetGeneric(button, _context);
            if (generic == null) return true;
            __result = generic;
            return false;
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

                Sprite generic = IconLibrary.GetGeneric(button.Value, ControllerIconLookup.IconContext.Bordered);
                if (generic == null) return true;
                __result = generic;
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
