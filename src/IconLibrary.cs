using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace OC2ControllerIcons
{
    // Source of every button sprite used by the mod:
    //  - Generic (original art embedded in the DLL, see tools/make_icons.py).
    //  - Per layout (Xbox / PlayStation / Nintendo), read from the icon sets the game
    //    already ships in ControllerIconLookup (the PC build contains all three).
    public static class IconLibrary
    {
        private const BindingFlags Inst = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Dictionary<string, Sprite> s_generic = new Dictionary<string, Sprite>();
        private static readonly HashSet<Sprite> s_genericSet = new HashSet<Sprite>();

        // ------------------------------------------------------------------ generic

        // Generic sprite for a physical button, or null if there is none (Back/Start: the game has none either).
        public static Sprite GetGeneric(ControlPadInput.Button button, ControllerIconLookup.IconContext context)
        {
            string key;
            switch (button)
            {
                case ControlPadInput.Button.A: key = "face_S"; break;
                case ControlPadInput.Button.B: key = "face_E"; break;
                case ControlPadInput.Button.X: key = "face_W"; break;
                case ControlPadInput.Button.Y: key = "face_N"; break;
                case ControlPadInput.Button.LB: key = "sh_LB"; break;
                case ControlPadInput.Button.RB: key = "sh_RB"; break;
                case ControlPadInput.Button.LTrigger: key = "sh_LT"; break;
                case ControlPadInput.Button.RTrigger: key = "sh_RT"; break;
                case ControlPadInput.Button.LeftAnalog: key = "stick_L"; break;
                case ControlPadInput.Button.RightAnalog: key = "stick_R"; break;
                default: return null; // the game's D-pad icon is already universal
            }
            key += context == ControllerIconLookup.IconContext.Borderless ? "_nb" : "_b";
            return LoadGeneric(key);
        }

        public static bool IsGeneric(Sprite sprite)
        {
            return (object)sprite != null && s_genericSet.Contains(sprite);
        }

        private static Sprite LoadGeneric(string key)
        {
            return LoadResourceSprite(key);
        }

        // Loads a PNG embedded in the DLL (res/ folder) as a Sprite. Cached.
        public static Sprite LoadResourceSprite(string key)
        {
            Sprite sprite;
            if (s_generic.TryGetValue(key, out sprite) && sprite != null) return sprite;

            sprite = null;
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("OC2ControllerIcons.res." + key + ".png");
            if (stream != null)
            {
                byte[] data;
                using (stream)
                {
                    data = new byte[stream.Length];
                    int read = 0;
                    while (read < data.Length)
                    {
                        int n = stream.Read(data, read, data.Length - read);
                        if (n <= 0) break;
                        read += n;
                    }
                }
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, true);
                tex.name = "OC2CI_" + key;
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Trilinear;
                tex.anisoLevel = 2;
                if (ImageConversion.LoadImage(tex, data, true))
                {
                    tex.hideFlags = HideFlags.HideAndDontSave;
                    sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    sprite.name = "OC2CI_" + key;
                    sprite.hideFlags = HideFlags.HideAndDontSave;
                    s_genericSet.Add(sprite);
                }
            }
            if (sprite == null) Plugin.Log.LogWarning("Generic icon not found: " + key);
            s_generic[key] = sprite;
            return sprite;
        }

        // --------------------------------------------------------------- per layout

        private static FieldInfo s_borderedField, s_borderlessField;
        private static FieldInfo s_setXbox, s_setPS4, s_setNX;
        private static Dictionary<string, FieldInfo> s_buttonIconsFields;
        private static Dictionary<string, FieldInfo> s_nxIconsFields;
        private static bool s_reflectionReady, s_reflectionFailed;

        private static bool InitReflection()
        {
            if (s_reflectionReady) return true;
            if (s_reflectionFailed) return false;
            try
            {
                Type lookup = typeof(ControllerIconLookup);
                s_borderedField = lookup.GetField("m_borderedIcons", Inst);
                s_borderlessField = lookup.GetField("m_borderlessIcons", Inst);
                Type platformSet = null, buttonIcons = null, nxIcons = null;
                foreach (Type t in lookup.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (t.Name == "PlatformSet") platformSet = t;
                    else if (t.Name == "ButtonIcons") buttonIcons = t;
                    else if (t.Name == "NXButtonIcons") nxIcons = t;
                }
                s_setXbox = platformSet.GetField("XboxOne", Inst);
                s_setPS4 = platformSet.GetField("PS4", Inst);
                s_setNX = platformSet.GetField("NX", Inst);
                s_buttonIconsFields = FieldMap(buttonIcons);
                s_nxIconsFields = FieldMap(nxIcons);
                if ((object)s_borderedField == null || (object)s_borderlessField == null || (object)s_setXbox == null)
                    throw new Exception("ControllerIconLookup fields not found");
                s_reflectionReady = true;
            }
            catch (Exception ex)
            {
                s_reflectionFailed = true;
                Plugin.Log.LogError("Could not read ControllerIconLookup: " + ex);
            }
            return s_reflectionReady;
        }

        private static Dictionary<string, FieldInfo> FieldMap(Type t)
        {
            Dictionary<string, FieldInfo> map = new Dictionary<string, FieldInfo>();
            if ((object)t == null) return map;
            foreach (FieldInfo f in t.GetFields(Inst))
            {
                if (f.FieldType == typeof(Sprite)) map[f.Name] = f;
            }
            return map;
        }

        // Icon of the physical button as printed on a controller of that layout.
        // (Buttons are named by Xbox position: A=south, B=east, X=west, Y=north.)
        public static Sprite GetForLayout(ControllerIconLookup lookup, ControlPadInput.Button button,
                                          ControllerIconLookup.IconContext context, PadLayout layout)
        {
            if (layout == PadLayout.Generic)
            {
                Sprite generic = GetGeneric(button, context);
                if (generic != null) return generic;
                layout = PadLayout.Xbox; // the D-pad has no generic version: use the game's icon
            }
            if (lookup == null || !InitReflection()) return null;
            LogMissingIconsOnce(lookup);

            // Requested style first, then the other style of the SAME layout. Never another
            // brand: returns null if this layout has no icon for the button.
            ControllerIconLookup.IconContext other = context == ControllerIconLookup.IconContext.Borderless
                ? ControllerIconLookup.IconContext.Bordered : ControllerIconLookup.IconContext.Borderless;
            Sprite sprite = ReadLayout(lookup, context, layout, button);
            if (sprite == null) sprite = ReadLayout(lookup, other, layout, button);
            return sprite;
        }

        private static Sprite ReadLayout(ControllerIconLookup lookup, ControllerIconLookup.IconContext context,
                                         PadLayout layout, ControlPadInput.Button button)
        {
            object set = (context == ControllerIconLookup.IconContext.Borderless ? s_borderlessField : s_borderedField).GetValue(lookup);
            if (set == null) return null;
            if (layout == PadLayout.Nintendo)
                return (object)s_setNX != null ? ReadSprite(s_setNX.GetValue(set), s_nxIconsFields, NintendoField(button)) : null;
            if (layout == PadLayout.PlayStation)
                return (object)s_setPS4 != null ? ReadSprite(s_setPS4.GetValue(set), s_buttonIconsFields, StandardField(button)) : null;
            return ReadSprite(s_setXbox.GetValue(set), s_buttonIconsFields, StandardField(button));
        }

        private static bool s_loggedMissing;

        // One-time diagnostic: which PlayStation / Nintendo face icons the game lacks per style.
        private static void LogMissingIconsOnce(ControllerIconLookup lookup)
        {
            if (s_loggedMissing) return;
            s_loggedMissing = true;
            ControlPadInput.Button[] face = { ControlPadInput.Button.A, ControlPadInput.Button.B, ControlPadInput.Button.X, ControlPadInput.Button.Y };
            string missing = "";
            foreach (ControllerIconLookup.IconContext ctx in new ControllerIconLookup.IconContext[] { ControllerIconLookup.IconContext.Bordered, ControllerIconLookup.IconContext.Borderless })
            {
                foreach (PadLayout l in new PadLayout[] { PadLayout.PlayStation, PadLayout.Nintendo })
                {
                    for (int i = 0; i < face.Length; i++)
                    {
                        if (ReadLayout(lookup, ctx, l, face[i]) == null) missing += " " + l + "/" + ctx + "/" + face[i];
                    }
                }
            }
            if (missing.Length > 0) Plugin.Log.LogInfo("Game icon sets without these face buttons (other style used instead):" + missing);
        }

        private static Sprite ReadSprite(object pack, Dictionary<string, FieldInfo> fields, string name)
        {
            FieldInfo f;
            if (pack == null || name == null || !fields.TryGetValue(name, out f)) return null;
            Sprite s = f.GetValue(pack) as Sprite;
            return s != null ? s : null;
        }

        private static string StandardField(ControlPadInput.Button button)
        {
            switch (button)
            {
                case ControlPadInput.Button.A: return "Action1";
                case ControlPadInput.Button.B: return "Action2";
                case ControlPadInput.Button.X: return "Action3";
                case ControlPadInput.Button.Y: return "Action4";
                case ControlPadInput.Button.LB: return "LeftBumper";
                case ControlPadInput.Button.RB: return "RightBumper";
                case ControlPadInput.Button.LTrigger: return "LeftTrigger";
                case ControlPadInput.Button.RTrigger: return "RightTrigger";
                case ControlPadInput.Button.LeftAnalog: return "LeftStick";
                case ControlPadInput.Button.RightAnalog: return "RightStick";
                case ControlPadInput.Button.DPadUp: return "DPadUp";
                case ControlPadInput.Button.DPadRight: return "DPadRight";
                case ControlPadInput.Button.DPadLeft: return "DPadLeft";
                default: return null;
            }
        }

        // Nintendo swaps A/B and X/Y compared to Xbox: the physical POSITION is preserved.
        private static string NintendoField(ControlPadInput.Button button)
        {
            switch (button)
            {
                case ControlPadInput.Button.A: return "B";
                case ControlPadInput.Button.B: return "A";
                case ControlPadInput.Button.X: return "Y";
                case ControlPadInput.Button.Y: return "X";
                case ControlPadInput.Button.LB: return "LeftBumper";
                case ControlPadInput.Button.RB: return "RightBumper";
                case ControlPadInput.Button.LTrigger: return "LeftTrigger";
                case ControlPadInput.Button.RTrigger: return "RightTrigger";
                case ControlPadInput.Button.LeftAnalog: return "LeftStick";
                case ControlPadInput.Button.RightAnalog: return "RightStick";
                case ControlPadInput.Button.DPadUp: return "DPadUp";
                case ControlPadInput.Button.DPadDown: return "DPadDown";
                case ControlPadInput.Button.DPadLeft: return "DPadLeft";
                case ControlPadInput.Button.DPadRight: return "DPadRight";
                default: return null;
            }
        }

        // ------------------------------------------------------------------ helpers

        public static PlayerInputLookup.LogicalButtonID SemanticToLogical(SemanticIconLookup.Semantic semantic)
        {
            // Same table as SemanticIconLookup.ButtonSet.GetButton (private in the game).
            switch (semantic)
            {
                case SemanticIconLookup.Semantic.Pickup:
                case SemanticIconLookup.Semantic.FireExtinguisher:
                case SemanticIconLookup.Semantic.Talk:
                case SemanticIconLookup.Semantic.Portal:
                    return PlayerInputLookup.LogicalButtonID.PickupAndDrop;
                case SemanticIconLookup.Semantic.Switch:
                    return PlayerInputLookup.LogicalButtonID.PlayerSwitch;
                case SemanticIconLookup.Semantic.Dash:
                    return PlayerInputLookup.LogicalButtonID.Dash;
                default:
                    return PlayerInputLookup.LogicalButtonID.WorkstationInteract;
            }
        }

        // "Player N" index (0..3), i.e. the lobby slot used by that player.
        public static int PlayerSlot(PlayerInputLookup.Player player)
        {
            try
            {
                int pad = (int)PlayerInputLookup.GetPadForPlayer(player);
                if (pad >= 0 && pad < ModSettings.MaxPlayers) return pad;
            }
            catch (Exception)
            {
            }
            int p = (int)player;
            return p >= 0 && p < ModSettings.MaxPlayers ? p : 0;
        }

        // In-world prompt icon for a specific player, using THEIR layout.
        // Returns null when the original behaviour must be kept (keyboard, missing data...).
        public static Sprite GetGameplayIcon(SemanticIconLookup.Semantic semantic, PlayerInputLookup.Player player,
                                             ControllerIconLookup.IconContext context)
        {
            PlayerManager pm = GameUtils.RequestManager<PlayerManager>();
            ControllerIconLookup lookup = GameUtils.RequestManager<ControllerIconLookup>();
            if (pm == null || lookup == null) return null;

            ControllerIconLookup.DeviceContext device = PlayerButtonImage.GetDevice(pm, player);
            if (device != ControllerIconLookup.DeviceContext.Pad) return null;

            ControlPadInput.Button? button = PlayerButtonImage.GetControlPadButton<ControlPadInput.Button>(
                SemanticToLogical(semantic), player, device);
            if (!button.HasValue) return null;

            Sprite sprite = GetForLayout(lookup, button.Value, context, ModSettings.GetLayout(PlayerSlot(player)));
            return sprite != null ? sprite : GetForLayout(lookup, button.Value, context, PadLayout.Xbox);
        }
    }
}
