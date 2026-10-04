using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace OC2ControllerIcons.Patches
{
    // Adds the mod options to Settings > Game (the options scroll list) by cloning the
    // game's own "Quality" row: same look, arrows, sounds and controller/keyboard
    // navigation. Save/Discard behave exactly like the game's other options.
    public static class SettingsMenuPatch
    {
        private const BindingFlags Inst = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const string RowPrefix = "OC2CI_";

        private static FieldInfo s_optionField;      // BaseUIOption<INameListOption>.m_Option
        private static FieldInfo s_optionTypeField;  // BaseUIOption<INameListOption>.m_OptionType
        private static FieldInfo s_localizeField;    // SelectorOption.m_LocalizeOption
        private static FieldInfo s_focusButtonField;
        private static FieldInfo s_leftInputField, s_rightInputField;
        private static SelectorOption s_template;
        private static FieldInfo s_didInitField = typeof(BaseMenuBehaviour).GetField("m_bDidSingleTimeInitialize", Inst);

        public static void Apply(Harmony harmony)
        {
            Type baseType = typeof(BaseUIOption<INameListOption>);
            s_optionField = baseType.GetField("m_Option", Inst);
            s_optionTypeField = baseType.GetField("m_OptionType", Inst);
            s_localizeField = typeof(SelectorOption).GetField("m_LocalizeOption", Inst);
            if ((object)s_optionField == null || (object)s_optionTypeField == null)
                throw new Exception("BaseUIOption fields not found");

            harmony.Patch(AccessTools.Method(typeof(FrontendOptionsMenu), "Awake"),
                null, new HarmonyMethod(typeof(SettingsMenuPatch), "AwakePostfix"));
            harmony.Patch(AccessTools.Method(typeof(FrontendOptionsMenu), "Show"),
                null, new HarmonyMethod(typeof(SettingsMenuPatch), "ShowPostfix"));
            s_focusButtonField = typeof(SelectorOption).GetField("m_ButtonToHaveFocusOnForLeftRight", Inst);
            s_leftInputField = typeof(SelectorOption).GetField("m_LeftInput", Inst);
            s_rightInputField = typeof(SelectorOption).GetField("m_RightInput", Inst);
            harmony.Patch(AccessTools.Method(typeof(FrontendOptionsMenu), "SyncAllOptions"),
                null, new HarmonyMethod(typeof(SettingsMenuPatch), "SyncAllPostfix"));
            ModSettings.Changed += SyncModRows;
            harmony.Patch(AccessTools.Method(typeof(SelectorOption), "Update"),
                new HarmonyMethod(typeof(SettingsMenuPatch), "SelectorUpdatePrefix"));

            // Save / Discard / "unsaved changes" handling of the options menu.
            harmony.Patch(AccessTools.Method(typeof(FrontendOptionsMenu), "SaveOptions"),
                new HarmonyMethod(typeof(SettingsMenuPatch), "CommitPostfix"));
            harmony.Patch(AccessTools.Method(typeof(OptionsData), "AddToSave"),
                null, new HarmonyMethod(typeof(SettingsMenuPatch), "CommitPostfix"));
            harmony.Patch(AccessTools.Method(typeof(OptionsData), "LoadFromSave"),
                null, new HarmonyMethod(typeof(SettingsMenuPatch), "RevertPostfix"));
            harmony.Patch(AccessTools.Method(typeof(OptionsData), "AnyChangesToCommit"),
                null, new HarmonyMethod(typeof(SettingsMenuPatch), "AnyChangesPostfix"));
        }

        private static void CommitPostfix()
        {
            ModSettings.Commit();
        }

        private static void RevertPostfix()
        {
            ModSettings.Revert();
        }

        private static void AnyChangesPostfix(ref bool __result)
        {
            if (!__result && ModSettings.HasUncommittedChanges()) __result = true;
        }

        // --------------------------------------------------------------------- rows

        private static void AwakePostfix(FrontendOptionsMenu __instance)
        {
            try
            {
                InjectRows(__instance);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError("Could not add the options to the menu: " + ex);
            }
        }

        // Per-frame fix-ups for our rows before SelectorOption's original Update:
        //  - The root menu assigns its EventSystem to the buttons when it initializes, before
        //    our rows exist: copy it from a sibling row.
        //  - Use the SAME left/right inputs as the original row (a clone's own inputs do not
        //    receive the D-pad reliably).
        private static void SelectorUpdatePrefix(SelectorOption __instance)
        {
            if (!__instance.name.StartsWith(RowPrefix)) return;
            T17Button focus = s_focusButtonField.GetValue(__instance) as T17Button;
            Transform content = __instance.transform.parent;
            if (focus == null || content == null) return;

            if (focus.GetDomain() == null)
            {
                for (int i = 0; i < content.childCount; i++)
                {
                    T17Button other = content.GetChild(i).GetComponent<T17Button>();
                    if (other != null && other.GetDomain() != null)
                    {
                        focus.SetEventSystem(other.GetDomain());
                        break;
                    }
                }
            }

            if (s_template == null || s_template.transform.parent != content) s_template = FindTemplate(content);
            SelectorOption template = s_template;
            if (template != null && (object)s_leftInputField != null)
            {
                object l = s_leftInputField.GetValue(template), r = s_rightInputField.GetValue(template);
                if (l != null && s_leftInputField.GetValue(__instance) != l) s_leftInputField.SetValue(__instance, l);
                if (r != null && s_rightInputField.GetValue(__instance) != r) s_rightInputField.SetValue(__instance, r);
            }
        }

        // Refresh our rows' text as soon as a value changes.
        private static void SyncModRows()
        {
            SelectorOption[] all = Resources.FindObjectsOfTypeAll<SelectorOption>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name.StartsWith(RowPrefix) && all[i].gameObject.scene.IsValid())
                    all[i].SyncUIWithOption();
            }
        }

        // SyncAllOptions uses a list cached before our rows were created.
        private static void SyncAllPostfix(FrontendOptionsMenu __instance)
        {
            SelectorOption[] selectors = __instance.GetComponentsInChildren<SelectorOption>(true);
            for (int i = 0; i < selectors.Length; i++)
            {
                if (selectors[i].name.StartsWith(RowPrefix)) selectors[i].SyncUIWithOption();
            }
        }

        private static void ShowPostfix(FrontendOptionsMenu __instance)
        {
            if (__instance.m_ScrollView != null) SettingsHeader.Ensure(__instance.m_ScrollView.m_ContentParent);
            // Re-apply texts (in case the game re-localized the cloned labels) and values.
            SelectorOption[] selectors = __instance.GetComponentsInChildren<SelectorOption>(true);
            SelectorOption lastRow = null;
            for (int i = 0; i < selectors.Length; i++)
            {
                ModOption option = s_optionField.GetValue(selectors[i]) as ModOption;
                if (option == null) continue;
                SetTitle(selectors[i].gameObject, option.Title);
                selectors[i].SyncUIWithOption();
                if (lastRow == null || selectors[i].transform.GetSiblingIndex() > lastRow.transform.GetSiblingIndex()) lastRow = selectors[i];
            }
            if (lastRow != null) FixUpNavigation(__instance, lastRow);
        }

        // Cancel/Save have "up" serialized to the last original row:
        // redirect it to the last mod row.
        private static void FixUpNavigation(FrontendOptionsMenu menu, SelectorOption lastRow)
        {
            Transform content = lastRow.transform.parent;
            Selectable last = lastRow.GetComponent<Selectable>();
            if (content == null || last == null) return;
            Selectable[] all = menu.GetComponentsInChildren<Selectable>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Selectable s = all[i];
                if (s.transform.IsChildOf(content)) continue;
                Navigation nav = s.navigation;
                if (nav.mode != Navigation.Mode.Explicit || nav.selectOnUp == null) continue;
                if (nav.selectOnUp.transform.parent == content && nav.selectOnUp != last)
                {
                    nav.selectOnUp = last;
                    s.navigation = nav;
                }
            }
        }

        private static void InjectRows(FrontendOptionsMenu menu)
        {
            T17ScrollView scroll = menu.m_ScrollView;
            if (scroll == null) throw new Exception("m_ScrollView is null");

            SelectorOption template = FindTemplate(scroll.transform);
            if (template == null) throw new Exception("no SelectorOption row to clone");
            Transform content = template.transform.parent;
            if (content.Find(RowPrefix + "Enabled") != null) return; // already injected

            List<ModOption> options = BuildOptions();

            // If the list has already registered its rows (up/down navigation + scrolling), ours
            // are registered through the game's own method; otherwise the list does it itself
            // when it initializes.
            // The template's option type is invalidated while cloning, so the clone's Awake
            // does not bind to the game's "Quality" option.
            bool scrollReady = (object)s_didInitField != null && (bool)s_didInitField.GetValue(scroll);

            object originalType = s_optionTypeField.GetValue(template);
            s_optionTypeField.SetValue(template, Enum.ToObject(typeof(OptionsData.OptionType), -1));
            try
            {
                for (int i = 0; i < options.Count; i++)
                {
                    GameObject row = UnityEngine.Object.Instantiate(template.gameObject, content, false);
                    if (scrollReady && content == scroll.m_ContentParent) scroll.AddNewObject(row);
                    row.name = RowPrefix + options[i].Key;
                    row.transform.SetAsLastSibling();
                    SelectorOption selector = row.GetComponent<SelectorOption>();
                    s_optionField.SetValue(selector, options[i]);
                    if ((object)s_localizeField != null) s_localizeField.SetValue(selector, false);
                    SetTitle(row, options[i].Title);
                    selector.SyncUIWithOption();
                }
            }
            finally
            {
                s_optionTypeField.SetValue(template, originalType);
            }
            if (scrollReady) SettingsHeader.Ensure(content);
            Plugin.Log.LogInfo("Mod options added to the Settings menu (" + options.Count + " rows).");
        }

        private static SelectorOption FindTemplate(Transform root)
        {
            SelectorOption fallback = null;
            SelectorOption[] all = root.GetComponentsInChildren<SelectorOption>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name.StartsWith(RowPrefix)) continue;
                if (all[i].name == "Quality") return all[i];
                if (fallback == null) fallback = all[i];
            }
            return fallback;
        }

        private static void SetTitle(GameObject row, string title)
        {
            Transform t = row.transform.Find("Title");
            T17Text text = t != null ? t.GetComponent<T17Text>() : row.GetComponentInChildren<T17Text>(true);
            if (text != null) text.SetNonLocalizedText(title);
        }

        // ------------------------------------------------------------------ options

        private static List<ModOption> BuildOptions()
        {
            List<ModOption> list = new List<ModOption>();
            list.Add(new ModOption("Enabled",
                delegate { return ModStrings.EnabledTitle; },
                delegate { return ModStrings.OffOn; },
                delegate { return ModSettings.Enabled ? 1 : 0; },
                delegate(int v) { ModSettings.Enabled = v == 1; }));

            for (int i = 0; i < ModSettings.MaxPlayers; i++)
            {
                int player = i;
                list.Add(new ModOption("Player" + (i + 1),
                    delegate { return ModStrings.PlayerTitle(player + 1); },
                    delegate { return ModStrings.Layouts; },
                    delegate { return (int)ModSettings.GetLayout(player); },
                    delegate(int v) { ModSettings.SetLayout(player, (PadLayout)v); }));
            }
            return list;
        }
    }

    // Custom option that the game's SelectorOption knows how to display.
    public class ModOption : INameListOption
    {
        public readonly string Key;
        private readonly Func<string> m_title;
        private readonly Func<string[]> m_names;
        private readonly Func<int> m_get;
        private readonly Action<int> m_set;

        public ModOption(string key, Func<string> title, Func<string[]> names, Func<int> get, Action<int> set)
        {
            Key = key;
            m_title = title;
            m_names = names;
            m_get = get;
            m_set = set;
        }

        public string Title { get { return m_title(); } }

        public OptionsData.Categories Category { get { return OptionsData.Categories.Controls; } }
        public string Label { get { return "OC2CI_" + Key; } }
        public string[] GetNames() { return m_names(); }

        public int GetOption()
        {
            int v = m_get();
            return v < 0 || v >= m_names().Length ? 0 : v;
        }

        public void SetOption(int _value)
        {
            if (_value < 0 || _value >= m_names().Length) _value = 0;
            m_set(_value);
        }

        public void Commit()
        {
        }
    }
}
