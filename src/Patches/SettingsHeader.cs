using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace OC2ControllerIcons.Patches
{
    // Section header ("CONTROLLER LAYOUTS") above the mod rows at the bottom of
    // Settings > Game. It is not selectable: navigation goes straight from the game's
    // last row to the first mod row.
    public static class SettingsHeader
    {
        public const string HeaderName = "OC2CI_Header";
        private const string FirstRowName = "OC2CI_Enabled";
        private const BindingFlags Inst = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static FieldInfo s_desiredPosition;

        public static void Apply(Harmony harmony)
        {
            s_desiredPosition = typeof(T17ScrollView).GetField("m_DesiredPosition", Inst);
            if ((object)s_desiredPosition != null)
            {
                harmony.Patch(AccessTools.Method(typeof(T17ScrollView), "OnElementSelected"),
                    null, new HarmonyMethod(typeof(SettingsHeader), "OnElementSelectedPostfix"));
            }
        }

        // Creates the header (once) by cloning a simple row of the menu itself.
        // Only called once the list has registered its rows, so it is not treated as selectable.
        public static void Ensure(Transform content)
        {
            if (content == null) return;
            Transform first = content.Find(FirstRowName);
            if (first == null) return;

            Transform header = content.Find(HeaderName);
            if (header == null)
            {
                Transform template = content.Find("ScreenAdjuster");
                if (template == null) template = first;
                header = UnityEngine.Object.Instantiate(template.gameObject, content, false).transform;
                header.name = HeaderName;
                StripInteraction(header.gameObject);
                Style(header, template);
            }
            // Right before the first mod row (note: moving backwards shifts sibling indices).
            int h = header.GetSiblingIndex(), f = first.GetSiblingIndex();
            if (h > f) header.SetSiblingIndex(f);
            else if (h < f - 1) header.SetSiblingIndex(f - 1);
            SetText(header, ModStrings.SectionTitle);
        }

        private static void StripInteraction(GameObject go)
        {
            // Animator first (it depends on the others), then everything that reacts to input.
            foreach (Animator a in go.GetComponentsInChildren<Animator>(true)) UnityEngine.Object.DestroyImmediate(a);
            foreach (Selectable s in go.GetComponentsInChildren<Selectable>(true)) UnityEngine.Object.DestroyImmediate(s);
            foreach (MonoBehaviour mb in go.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null) continue;
                string n = mb.GetType().Name;
                if (n == "T17_UISelectDeselectEvents" || n == "EventTrigger" || n == "SelectorOption" || n == "ToggleOption")
                    UnityEngine.Object.DestroyImmediate(mb);
            }
        }

        // Transparent background, title in the menu's bar blue and a line underneath.
        private static void Style(Transform header, Transform template)
        {
            Image bar = header.GetComponent<Image>();
            Image templateBar = template.GetComponent<Image>();
            Color accent = new Color(0.20f, 0.42f, 0.62f, 1f);
            if (bar != null)
            {
                bar.enabled = false;
            }

            Transform title = header.Find("Title");
            if (title != null)
            {
                Text text = title.GetComponent<Text>();
                if (text != null)
                {
                    text.color = accent;
                    text.fontStyle = FontStyle.Bold;
                }
                RectTransform rt = title as RectTransform;
                if (rt != null) rt.anchoredPosition += new Vector2(0f, 4f);
            }

            // Separator line using the same sprite as the row bars.
            GameObject line = new GameObject("Line", typeof(RectTransform));
            line.transform.SetParent(header, false);
            RectTransform lrt = (RectTransform)line.transform;
            lrt.anchorMin = new Vector2(0f, 0f);
            lrt.anchorMax = new Vector2(1f, 0f);
            lrt.pivot = new Vector2(0.5f, 0f);
            lrt.sizeDelta = new Vector2(0f, 4f);
            lrt.anchoredPosition = new Vector2(0f, 8f);
            Image img = line.AddComponent<Image>();
            if (templateBar != null)
            {
                img.sprite = templateBar.sprite;
                img.type = Image.Type.Sliced;
            }
            img.color = accent;
            img.raycastTarget = false;
        }

        private static void SetText(Transform header, string value)
        {
            Transform title = header.Find("Title");
            T17Text text = title != null ? title.GetComponent<T17Text>() : null;
            if (text != null) text.SetNonLocalizedText(value);
        }

        // The game's scroll view advances "one row per step"; with the header in between it
        // would fall short. After its own calculation, correct it so the selected row is fully
        // visible (plus the header when the first mod row is selected).
        private static void OnElementSelectedPostfix(T17ScrollView __instance, Selectable sel)
        {
            try
            {
                RectTransform content = __instance.m_ContentParent;
                RectTransform viewport = __instance.m_ViewPort;
                if (content == null || viewport == null || sel == null) return;
                Transform header = content.Find(HeaderName);
                if (header == null) return;

                RectTransform row = sel.transform as RectTransform;
                while (row != null && row.parent != content) row = row.parent as RectTransform;
                if (row == null) return;

                float top = row.localPosition.y + row.rect.yMax;
                float bottom = row.localPosition.y + row.rect.yMin;
                if (row.name == FirstRowName)
                {
                    RectTransform h = (RectTransform)header;
                    top = Mathf.Max(top, h.localPosition.y + h.rect.yMax);
                }

                Vector2 desired = (Vector2)s_desiredPosition.GetValue(__instance);
                // Content position -> viewport coordinates (content is a direct child).
                float offset = desired.y - content.localPosition.y;
                Vector3 contentInView = content.localPosition + new Vector3(0f, offset, 0f);
                float viewTop = viewport.rect.yMax, viewBottom = viewport.rect.yMin;
                float rowTop = contentInView.y + top, rowBottom = contentInView.y + bottom;

                if (rowBottom < viewBottom) desired.y += viewBottom - rowBottom;
                else if (rowTop > viewTop) desired.y -= rowTop - viewTop;
                s_desiredPosition.SetValue(__instance, desired);
            }
            catch (Exception)
            {
            }
        }
    }
}
