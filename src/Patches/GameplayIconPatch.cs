using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace OC2ControllerIcons.Patches
{
    // In-game prompts (tutorial and icons floating over objects: pick up, chop,
    // wash...) use the layout chosen by each player.
    //
    // These prompts are shared (one per object), so each one follows the layout of
    // the closest local chef: whoever walks up to the crate sees THEIR controller.
    public static class GameplayIconPatch
    {
        private const BindingFlags Inst = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static FieldInfo s_hoverIcon, s_hoverSemantic;

        // Sprite returned by SemanticIconLookup -> semantic that requested it (for tutorial icons).
        private static readonly Dictionary<Sprite, SemanticIconLookup.Semantic> s_spriteSemantics =
            new Dictionary<Sprite, SemanticIconLookup.Semantic>();

        public static void Apply(Harmony harmony)
        {
            MethodInfo getIcon = AccessTools.Method(typeof(SemanticIconLookup), "GetIcon");
            harmony.Patch(getIcon,
                new HarmonyMethod(typeof(GameplayIconPatch), "SemanticGetIconPrefix"),
                new HarmonyMethod(typeof(GameplayIconPatch), "SemanticGetIconPostfix"));

            s_hoverIcon = typeof(ButtonHoverIcon).GetField("m_icon", Inst);
            s_hoverSemantic = typeof(ButtonHoverIcon).GetField("m_semantic", Inst);
            harmony.Patch(AccessTools.Method(typeof(ButtonHoverIcon), "Awake"),
                null, new HarmonyMethod(typeof(GameplayIconPatch), "HoverAwakePostfix"));

            ConstructorInfo iconDataCtor = AccessTools.Constructor(typeof(ClientIconTutorialBase.IconData),
                new Type[] { typeof(IconTutorialBase), typeof(Sprite), typeof(Transform), typeof(ClientIconTutorialBase.ActiveQuery) });
            harmony.Patch(iconDataCtor, null, new HarmonyMethod(typeof(GameplayIconPatch), "IconDataCtorPostfix"));
        }

        // While the mod is enabled, SemanticIconLookup always returns the real button (not the
        // "semantic" hand/knife icon) using the requested player's layout.
        private static bool SemanticGetIconPrefix(SemanticIconLookup.Semantic _semantic, PlayerInputLookup.Player _player,
                                                  ControllerIconLookup.IconContext _context, ref Sprite __result)
        {
            if (!ModSettings.Enabled) return true;
            try
            {
                Sprite sprite = IconLibrary.GetGameplayIcon(_semantic, _player, _context);
                if (sprite == null) return true;
                __result = sprite;
                return false;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("SemanticGetIconPrefix: " + ex.Message);
                return true;
            }
        }

        private static void SemanticGetIconPostfix(SemanticIconLookup.Semantic _semantic, Sprite __result)
        {
            if (__result != null) s_spriteSemantics[__result] = _semantic;
        }

        private static void HoverAwakePostfix(ButtonHoverIcon __instance)
        {
            try
            {
                Image image = s_hoverIcon.GetValue(__instance) as Image;
                if (image == null) return;
                SemanticIconLookup.Semantic semantic = (SemanticIconLookup.Semantic)s_hoverSemantic.GetValue(__instance);
                HoverIconUpdater.Register(image, __instance.transform, null, semantic);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("HoverAwakePostfix: " + ex.Message);
            }
        }

        private static void IconDataCtorPostfix(ClientIconTutorialBase.IconData __instance, Sprite _sprite, Transform _parent)
        {
            SemanticIconLookup.Semantic semantic;
            if (_sprite == null || !s_spriteSemantics.TryGetValue(_sprite, out semantic)) return; // ingredient icon, etc.
            if (__instance.Icon == null) return;
            Transform iconChild = __instance.Icon.transform.Find("Icon");
            Image image = iconChild != null ? iconChild.GetComponent<Image>() : null;
            if (image == null) return;
            HoverIconUpdater.Register(image, _parent, __instance.Icon, semantic);
        }
    }

    // Periodically updates registered icons with the sprite of the closest local chef.
    public class HoverIconUpdater : MonoBehaviour
    {
        private class Entry
        {
            public Image Image;
            public Transform Anchor;
            public HoverIconUIController Follow; // tutorial icons change their target
            public SemanticIconLookup.Semantic Semantic;
            public Sprite Original;
        }

        private static readonly List<Entry> s_entries = new List<Entry>();
        private static readonly List<PlayerIDProvider> s_chefs = new List<PlayerIDProvider>();
        private static bool s_wasEnabled;

        private float m_nextUpdate;
        private float m_nextChefScan;

        public static void Register(Image image, Transform anchor, HoverIconUIController follow, SemanticIconLookup.Semantic semantic)
        {
            Entry e = new Entry();
            e.Image = image;
            e.Anchor = anchor;
            e.Follow = follow;
            e.Semantic = semantic;
            s_entries.Add(e);
        }

        private void Update()
        {
            if (Time.unscaledTime < m_nextUpdate) return;
            m_nextUpdate = Time.unscaledTime + 0.1f;

            bool enabled = ModSettings.Enabled;
            if (!enabled)
            {
                if (s_wasEnabled) RestoreOriginals();
                s_wasEnabled = false;
                PruneDead();
                return;
            }
            s_wasEnabled = true;

            if (Time.unscaledTime >= m_nextChefScan)
            {
                m_nextChefScan = Time.unscaledTime + 1f;
                s_chefs.Clear();
                PlayerIDProvider[] all = FindObjectsOfType<PlayerIDProvider>();
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] != null && all[i].IsLocallyControlled()) s_chefs.Add(all[i]);
                }
            }

            for (int i = s_entries.Count - 1; i >= 0; i--)
            {
                Entry e = s_entries[i];
                if (e.Image == null)
                {
                    s_entries.RemoveAt(i);
                    continue;
                }
                if (!e.Image.isActiveAndEnabled) continue;
                try
                {
                    Transform anchor = e.Anchor;
                    if (e.Follow != null && e.Follow.GetFollowTransform() != null) anchor = e.Follow.GetFollowTransform();
                    PlayerInputLookup.Player player = NearestPlayer(anchor);
                    Sprite sprite = IconLibrary.GetGameplayIcon(e.Semantic, player, ControllerIconLookup.IconContext.Bordered);
                    if (sprite != null && e.Image.sprite != sprite)
                    {
                        if (e.Original == null) e.Original = e.Image.sprite;
                        e.Image.sprite = sprite;
                    }
                }
                catch (Exception)
                {
                    s_entries.RemoveAt(i); // object being destroyed
                }
            }
        }

        private static PlayerInputLookup.Player NearestPlayer(Transform anchor)
        {
            PlayerInputLookup.Player best = PlayerInputLookup.Player.One;
            if (anchor == null) return best;
            Vector3 pos = anchor.position;
            float bestDist = float.MaxValue;
            for (int i = 0; i < s_chefs.Count; i++)
            {
                PlayerIDProvider chef = s_chefs[i];
                if (chef == null || !chef.isActiveAndEnabled) continue;
                Vector3 d = chef.transform.position - pos;
                float dist = d.x * d.x + d.z * d.z;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = chef.GetID();
                }
            }
            return best;
        }

        // When the mod is disabled, ask the game for the original icon again.
        private static void RestoreOriginals()
        {
            SemanticIconLookup lookup = GameUtils.RequestManager<SemanticIconLookup>();
            for (int i = 0; i < s_entries.Count; i++)
            {
                Entry e = s_entries[i];
                if (e.Image == null) continue;
                try
                {
                    Sprite s = lookup != null ? lookup.GetIcon(e.Semantic) : e.Original;
                    if (s != null) e.Image.sprite = s;
                }
                catch (Exception)
                {
                }
                e.Original = null;
            }
        }

        private static void PruneDead()
        {
            for (int i = s_entries.Count - 1; i >= 0; i--)
            {
                if (s_entries[i].Image == null) s_entries.RemoveAt(i);
            }
        }
    }
}
