using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace OC2ControllerIcons
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            ModSettings.Init(Config);

            Harmony harmony = new Harmony(PluginInfo.GUID);
            ApplyPatch(harmony, typeof(Patches.SettingsMenuPatch));
            ApplyPatch(harmony, typeof(Patches.SettingsHeader));
            ApplyPatch(harmony, typeof(Patches.MenuIconPatch));
            ApplyPatch(harmony, typeof(Patches.GameplayIconPatch));
            ApplyPatch(harmony, typeof(Patches.LobbyIconPatch));
            ApplyPatch(harmony, typeof(Patches.DiagramSpritePatch));

            // Own host object (BepInEx's manager object can be destroyed on scene changes in some games).
            GameObject host = new GameObject("OC2ControllerIcons");
            host.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(host);
            host.AddComponent<Patches.HoverIconUpdater>();
            host.AddComponent<PlayerSlotWatcher>();

            Log.LogInfo(PluginInfo.NAME + " " + PluginInfo.VERSION + " ready. Enabled=" + ModSettings.Enabled);
        }

        // Each patch class is applied independently: if one fails (e.g. after a game
        // update) the rest of the mod keeps working.
        private static void ApplyPatch(Harmony harmony, Type patchClass)
        {
            try
            {
                patchClass.GetMethod("Apply").Invoke(null, new object[] { harmony });
                Log.LogInfo("Patch applied: " + patchClass.Name);
            }
            catch (Exception ex)
            {
                Log.LogError("Patch FAILED: " + patchClass.Name + " -> " + ex);
            }
        }
    }

    internal static class PluginInfo
    {
        public const string GUID = "com.oc2mods.controllericons";
        public const string NAME = "OC2 Controller Icons";
        public const string VERSION = "3.1.0";
    }
}
