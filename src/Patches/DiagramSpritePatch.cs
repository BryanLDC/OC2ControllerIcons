using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace OC2ControllerIcons.Patches
{
    // Swaps known controller diagrams on any UI Image for the version that matches the
    // current settings (see DiagramGenericizer.Resolve). Covers sprites assigned from code
    // (setter) as well as sprites serialized in prefabs (OnEnable).
    public static class DiagramSpritePatch
    {
        public static void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.PropertySetter(typeof(Image), "sprite"),
                new HarmonyMethod(typeof(DiagramSpritePatch), "SetSpritePrefix"));
            harmony.Patch(AccessTools.Method(typeof(MaskableGraphic), "OnEnable"),
                null, new HarmonyMethod(typeof(DiagramSpritePatch), "OnEnablePostfix"));
            ModSettings.Changed += RefreshAll;
        }

        private static void SetSpritePrefix(ref Sprite value)
        {
            if (value != null) value = DiagramGenericizer.Resolve(value);
        }

        private static void OnEnablePostfix(MaskableGraphic __instance)
        {
            Image image = __instance as Image;
            if (image == null) return;
            Sprite current = image.sprite;
            if (current == null) return;
            Sprite target = DiagramGenericizer.Resolve(current);
            if (target != current) image.sprite = target;
        }

        private static void RefreshAll()
        {
            Image[] images = Resources.FindObjectsOfTypeAll<Image>();
            for (int i = 0; i < images.Length; i++)
            {
                Image img = images[i];
                if (img == null || !img.gameObject.scene.IsValid()) continue;
                Sprite s = img.sprite;
                if (s == null) continue;
                try
                {
                    Sprite target = DiagramGenericizer.Resolve(s);
                    if (target != s) img.sprite = target;
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
