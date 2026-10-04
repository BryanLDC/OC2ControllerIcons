using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace OC2ControllerIcons.Patches
{
    // Replaces known controller diagrams with their generic version in any UI Image
    // (see DiagramGenericizer). Covers sprites assigned from code (setter) as well as
    // sprites serialized in prefabs (OnEnable).
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
            if (value == null) return;
            if (ModSettings.Enabled) value = DiagramGenericizer.Map(value);
            else if (DiagramGenericizer.IsGenerated(value)) value = DiagramGenericizer.GetOriginal(value);
        }

        private static void OnEnablePostfix(MaskableGraphic __instance)
        {
            if (!ModSettings.Enabled) return;
            Image image = __instance as Image;
            if (image == null) return;
            Sprite current = image.sprite;
            if (current == null) return;
            Sprite mapped = DiagramGenericizer.Map(current);
            if (mapped != current) image.sprite = mapped;
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
                    // The patched setter decides whether to apply the generic or the original sprite.
                    Sprite target = ModSettings.Enabled ? DiagramGenericizer.Map(s) : DiagramGenericizer.GetOriginal(s);
                    if (target != s) img.sprite = target;
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
