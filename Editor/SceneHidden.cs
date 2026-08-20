using System;
using System.Linq;
using UnityEditor;

namespace WendellLeao.SceneSwitcher.Editor
{
    internal static class SceneHidden
    {
        private const string EditorPrefsKey = "WendellLeao.SceneSwitcher.Hidden";
        private const char Separator = ';';

        public static bool IsHidden(string guid)
        {
            string[] hiddenGuids = GetHiddenGuids();

            return hiddenGuids.Contains(guid);
        }

        public static void Toggle(string guid)
        {
            string[] hiddenGuids = GetHiddenGuids();

            string[] updatedGuids = hiddenGuids.Contains(guid)
                ? hiddenGuids.Where(hiddenGuid => hiddenGuid != guid).ToArray()
                : hiddenGuids.Append(guid).ToArray();

            SetHiddenGuids(updatedGuids);
        }

        private static string[] GetHiddenGuids()
        {
            string rawValue = EditorPrefs.GetString(EditorPrefsKey, string.Empty);

            if (string.IsNullOrEmpty(rawValue))
            {
                return Array.Empty<string>();
            }

            return rawValue.Split(Separator);
        }

        private static void SetHiddenGuids(string[] guids)
        {
            string rawValue = string.Join(Separator, guids);

            EditorPrefs.SetString(EditorPrefsKey, rawValue);
        }
    }
}
