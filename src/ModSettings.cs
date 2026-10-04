using System;
using BepInEx.Configuration;

namespace OC2ControllerIcons
{
    public enum PadLayout
    {
        Xbox = 0,
        PlayStation = 1,
        Nintendo = 2,
        Generic = 3
    }

    // Mod settings.
    //  - "Live" values: used for rendering (they change instantly while browsing the menu).
    //  - Committed values: BepInEx\config\com.oc2mods.controllericons.cfg
    // The game's Settings menu decides when to commit (Save) or revert (Discard), exactly
    // like its own options. The game's save data is never touched.
    public static class ModSettings
    {
        public const int MaxPlayers = 4;

        private static ConfigFile s_file;
        private static ConfigEntry<bool> s_enabledEntry;
        private static ConfigEntry<PadLayout>[] s_layoutEntries = new ConfigEntry<PadLayout>[MaxPlayers];

        private static bool s_enabled;
        private static PadLayout[] s_layouts = new PadLayout[MaxPlayers];

        // Raised whenever any live value changes.
        public static event Action Changed;

        public static void Init(ConfigFile cfg)
        {
            s_file = cfg;
            s_enabledEntry = cfg.Bind("General", "Enabled", false,
                "Enables the mod: universal icons in menus/diagrams and per-player button layouts in-game.");
            for (int i = 0; i < MaxPlayers; i++)
            {
                s_layoutEntries[i] = cfg.Bind("Players", "Player" + (i + 1), PadLayout.Xbox,
                    "Button layout shown to Player " + (i + 1) + " in-game (Xbox, PlayStation, Nintendo, Generic). Default: Xbox.");
            }
            Revert();
        }

        public static bool Enabled
        {
            get { return s_enabled; }
            set
            {
                if (s_enabled == value) return;
                s_enabled = value;
                RaiseChanged();
            }
        }

        public static PadLayout GetLayout(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= MaxPlayers) playerIndex = 0;
            return s_layouts[playerIndex];
        }

        public static void SetLayout(int playerIndex, PadLayout layout)
        {
            if (playerIndex < 0 || playerIndex >= MaxPlayers) return;
            if (s_layouts[playerIndex] == layout) return;
            s_layouts[playerIndex] = layout;
            RaiseChanged();
        }

        public static bool HasUncommittedChanges()
        {
            if (s_enabledEntry == null) return false;
            if (s_enabled != s_enabledEntry.Value) return true;
            for (int i = 0; i < MaxPlayers; i++)
            {
                if (s_layouts[i] != s_layoutEntries[i].Value) return true;
            }
            return false;
        }

        public static void Commit()
        {
            if (s_enabledEntry == null || !HasUncommittedChanges()) return;
            s_enabledEntry.Value = s_enabled;
            for (int i = 0; i < MaxPlayers; i++)
            {
                s_layoutEntries[i].Value = s_layouts[i];
            }
            s_file.Save();
        }

        public static void Revert()
        {
            if (s_enabledEntry == null) return;
            bool changed = s_enabled != s_enabledEntry.Value;
            s_enabled = s_enabledEntry.Value;
            for (int i = 0; i < MaxPlayers; i++)
            {
                changed |= s_layouts[i] != s_layoutEntries[i].Value;
                s_layouts[i] = s_layoutEntries[i].Value;
            }
            if (changed) RaiseChanged();
        }

        private static void RaiseChanged()
        {
            Action handler = Changed;
            if (handler == null) return;
            try
            {
                handler();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError("Error while refreshing icons: " + ex);
            }
        }
    }
}
