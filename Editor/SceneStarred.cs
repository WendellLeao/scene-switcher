using System;
using System.Linq;
using UnityEditor;

namespace WendellLeao.SceneSwitcher.Editor
{
    internal static class SceneStarred
    {
        private const string EditorPrefsKey = "WendellLeao.SceneSwitcher.Starred";
        private const char Separator = ';';

        public static bool IsStarred(string guid)
        {
            string[] starredGuids = GetStarredGuids();

            return starredGuids.Contains(guid);
        }

        public static void Toggle(string guid)
        {
            string[] starredGuids = GetStarredGuids();

            string[] updatedGuids = starredGuids.Contains(guid)
                ? starredGuids.Where(starredGuid => starredGuid != guid).ToArray()
                : starredGuids.Append(guid).ToArray();

            SetStarredGuids(updatedGuids);
        }

        private static string[] GetStarredGuids()
        {
            string rawValue = EditorPrefs.GetString(EditorPrefsKey, string.Empty);

            if (string.IsNullOrEmpty(rawValue))
            {
                return Array.Empty<string>();
            }

            return rawValue.Split(Separator);
        }

        private static void SetStarredGuids(string[] guids)
        {
            string rawValue = string.Join(Separator, guids);

            EditorPrefs.SetString(EditorPrefsKey, rawValue);
        }
    }
}
