using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace OC2ControllerIcons.Patches
{
    // Player-select screen: each slot's controller picture uses the layout chosen for
    // that player (Xbox / PlayStation / Nintendo).
    public static class LobbyIconPatch
    {
        private static int s_currentSlot = -1;

        public static void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(FrontendPlayerSlot), "SetControllerIconForUser"),
                new HarmonyMethod(typeof(LobbyIconPatch), "SlotPrefix"),
                new HarmonyMethod(typeof(LobbyIconPatch), "SlotPostfix"));
            harmony.Patch(AccessTools.Method(typeof(ControllerTypeSprites), "GetControllerSpritesForPlatform"),
                new HarmonyMethod(typeof(LobbyIconPatch), "SpritesPrefix"));
            ModSettings.Changed += RefreshSlots;
        }

        private static void SlotPrefix(FrontendPlayerSlot __instance)
        {
            s_currentSlot = (int)__instance.m_EngagementSlot;
        }

        private static void SlotPostfix()
        {
            s_currentSlot = -1;
        }

        private static bool SpritesPrefix(ControllerTypeSprites __instance, GamepadUser.ControlTypeEnum _type,
                                          ref ControllerTypeSprites.ControllerSprites __result)
        {
            if (!ModSettings.Enabled || s_currentSlot < 0 || _type == GamepadUser.ControlTypeEnum.Keyboard) return true;

            ControllerTypeSprites.ControllerSprites sprites;
            switch (ModSettings.GetLayout(s_currentSlot))
            {
                case PadLayout.PlayStation: sprites = __instance.m_padSpritesPS4; break;
                case PadLayout.Nintendo: sprites = NintendoProSprites(__instance.m_padSpritesNX); break;
                default: sprites = __instance.m_padSpritesX1; break; // Xbox and Generic (the silhouette is unbranded)
            }
            if (sprites == null || sprites.m_full == null) return true;
            __result = sprites;
            return false;
        }

        // The game depicts Nintendo as Joy-Cons in a grip; on PC the Pro Controller is far
        // more common, so an original silhouette is used (tools/make_procontroller.py) in
        // the same mask format. The large engagement images are kept.
        private static ControllerTypeSprites.ControllerSprites s_nxPro;

        private static ControllerTypeSprites.ControllerSprites NintendoProSprites(ControllerTypeSprites.ControllerSprites original)
        {
            if (s_nxPro != null && s_nxPro.m_full != null) return s_nxPro;
            Sprite whole = IconLibrary.LoadResourceSprite("pad_nx_whole");
            if (whole == null) return original;
            ControllerTypeSprites.ControllerSprites pro = new ControllerTypeSprites.ControllerSprites();
            pro.m_full = whole;
            pro.m_left = IconLibrary.LoadResourceSprite("pad_nx_left");
            pro.m_right = IconLibrary.LoadResourceSprite("pad_nx_right");
            if (original != null)
            {
                pro.m_fullEngagement = original.m_fullEngagement;
                pro.m_leftEngagement = original.m_leftEngagement;
                pro.m_rightEngagement = original.m_rightEngagement;
                if (pro.m_left == null) pro.m_left = original.m_left;
                if (pro.m_right == null) pro.m_right = original.m_right;
            }
            s_nxPro = pro;
            return pro;
        }

        private static void RefreshSlots()
        {
            MethodInfo refresh = AccessTools.Method(typeof(FrontendPlayerSlot), "RefreshCosmetics");
            if ((object)refresh == null) return;
            FrontendPlayerSlot[] slots = UnityEngine.Object.FindObjectsOfType<FrontendPlayerSlot>();
            for (int i = 0; i < slots.Length; i++)
            {
                try
                {
                    if (refresh.GetParameters().Length == 0) refresh.Invoke(slots[i], null);
                }
                catch (System.Exception)
                {
                }
            }
        }
    }
}
