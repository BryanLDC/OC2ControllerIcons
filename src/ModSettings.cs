using System;
using System.IO;
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
        private const int AllSlots = (1 << MaxPlayers) - 1;

        private static ConfigFile s_file;
        private static ConfigEntry<bool> s_enabledEntry;
        private static ConfigEntry<PadLayout>[] s_layoutEntries = new ConfigEntry<PadLayout>[MaxPlayers];
        private static ConfigEntry<int> s_configuredEntry;

        private static bool s_enabled;
        private static PadLayout[] s_layouts = new PadLayout[MaxPlayers];

        // Raised whenever any live value changes (or the set of joined players changes).
        public static event Action Changed;

        public static void Init(ConfigFile cfg)
        {
            s_file = cfg;
            // Existing installs (config file already present) keep every slot as configured,
            // so upgrading never overwrites layouts the user already chose.
            bool upgrading = File.Exists(cfg.ConfigFilePath);

            s_enabledEntry = cfg.Bind("General", "Enabled", false,
                "Enables the mod: universal icons in menus/diagrams and per-player button layouts in-game.");
            for (int i = 0; i < MaxPlayers; i++)
            {
                s_layoutEntries[i] = cfg.Bind("Players", "Player" + (i + 1), PadLayout.Xbox,
                    "Button layout shown to Player " + (i + 1) + " in-game (Xbox, PlayStation, Nintendo, Generic). Default: Xbox.");
            }
            s_configuredEntry = cfg.Bind("Players", "ConfiguredSlots", upgrading ? AllSlots : 0,
                "Bit mask of the players whose layout has been set (1 = Player 1, 2 = Player 2, 4 = Player 3, 8 = Player 4). " +
                "A player joining in a slot that is not configured gets the most common layout of the other players.");
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

        // True when every joined controller player (all four slots if nobody has joined with a
        // controller yet) uses the same concrete layout. Menus then show that layout's buttons.
        public static bool TryGetUniformLayout(out PadLayout layout)
        {
            int mask = PlayerSlots.PadMask();
            if (mask == 0) mask = AllSlots;
            layout = PadLayout.Generic;
            bool first = true;
            for (int i = 0; i < MaxPlayers; i++)
            {
                if ((mask & (1 << i)) == 0) continue;
                if (first)
                {
                    layout = s_layouts[i];
                    first = false;
                }
                else if (s_layouts[i] != layout)
                {
                    return false;
                }
            }
            return layout != PadLayout.Generic;
        }

        // A player joined in a slot whose layout was never set: give it the most common
        // layout among the other joined, configured players (tie -> Player 1's layout).
        public static void OnPlayerJoined(int slot, int joinedMask)
        {
            if (s_configuredEntry == null || slot < 0 || slot >= MaxPlayers) return;
            int configured = s_configuredEntry.Value;
            if ((configured & (1 << slot)) != 0) return;

            int[] counts = new int[4];
            int candidates = 0;
            for (int i = 0; i < MaxPlayers; i++)
            {
                if (i == slot || (joinedMask & (1 << i)) == 0 || (configured & (1 << i)) == 0) continue;
                counts[(int)s_layouts[i]]++;
                candidates |= 1 << i;
            }

            if (candidates != 0)
            {
                PadLayout chosen = Majority(counts, candidates);
                s_layouts[slot] = chosen;
                s_layoutEntries[slot].Value = chosen;
                Plugin.Log.LogInfo("Player " + (slot + 1) + " joined: layout set to " + chosen + " (most common layout).");
            }
            s_configuredEntry.Value = configured | (1 << slot);
            s_file.Save();
        }

        private static PadLayout Majority(int[] counts, int candidates)
        {
            int best = -1, bestCount = 0;
            bool tie = false;
            for (int l = 0; l < counts.Length; l++)
            {
                if (counts[l] > bestCount)
                {
                    best = l;
                    bestCount = counts[l];
                    tie = false;
                }
                else if (counts[l] == bestCount && bestCount > 0)
                {
                    tie = true;
                }
            }
            if (!tie) return (PadLayout)best;

            // Tie: Player 1's layout if Player 1 is among the candidates, else the first candidate.
            for (int i = 0; i < MaxPlayers; i++)
            {
                if ((candidates & (1 << i)) != 0) return s_layouts[i];
            }
            return (PadLayout)best;
        }

        // Called when the user saves the Settings menu: the players present are the ones
        // the configuration was made for.
        public static void MarkConfigured(int joinedMask)
        {
            if (s_configuredEntry == null || joinedMask == 0 || s_configuredEntry.Value == joinedMask) return;
            s_configuredEntry.Value = joinedMask;
            s_file.Save();
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

        public static void NotifyChanged()
        {
            RaiseChanged();
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
