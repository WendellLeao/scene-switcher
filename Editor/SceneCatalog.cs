using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace WendellLeao.SceneSwitcher.Editor
{
    [InitializeOnLoad]
    internal static class SceneCatalog
    {
        private static readonly List<SceneEntry> EntryList = new();
        public static IReadOnlyList<SceneEntry> Entries => EntryList;

        static SceneCatalog()
        {
            Refresh();

            EditorApplication.projectChanged -= Refresh;
            EditorApplication.projectChanged += Refresh;
        }

        private static void Refresh()
        {
            EntryList.Clear();

            foreach (string guid in AssetDatabase.FindAssets("t:Scene"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(path);

                EntryList.Add(new SceneEntry(guid, path, name));
            }

            EntryList.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
