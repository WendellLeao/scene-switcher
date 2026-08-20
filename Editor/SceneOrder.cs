using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace WendellLeao.SceneSwitcher.Editor
{
    internal static class SceneOrder
    {
        private const string EditorPrefsKey = "WendellLeao.SceneSwitcher.Order";
        private const char Separator = ';';

        public static List<SceneEntry> Sort(IEnumerable<SceneEntry> entries)
        {
            List<string> order = GetOrderedGuids();
            Dictionary<string, int> indexByGuid = new();

            for (int i = 0; i < order.Count; i++)
            {
                indexByGuid[order[i]] = i;
            }

            return entries
                .OrderBy(entry => indexByGuid.TryGetValue(entry.Guid, out int index) ? index : int.MaxValue)
                .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static void MoveBefore(string draggedGuid, string targetGuid)
        {
            Move(draggedGuid, targetGuid, insertAfter: false);
        }

        public static void MoveAfter(string draggedGuid, string targetGuid)
        {
            Move(draggedGuid, targetGuid, insertAfter: true);
        }

        private static void Move(string draggedGuid, string targetGuid, bool insertAfter)
        {
            if (draggedGuid == targetGuid)
            {
                return;
            }

            List<string> order = GetOrderedGuids();

            foreach (SceneEntry entry in SceneCatalog.Entries)
            {
                if (!order.Contains(entry.Guid))
                {
                    order.Add(entry.Guid);
                }
            }

            order.Remove(draggedGuid);

            int targetIndex = order.IndexOf(targetGuid);

            if (targetIndex < 0)
            {
                order.Add(draggedGuid);
            }
            else
            {
                order.Insert(insertAfter ? targetIndex + 1 : targetIndex, draggedGuid);
            }

            SetOrderedGuids(order);
        }

        private static List<string> GetOrderedGuids()
        {
            string rawValue = EditorPrefs.GetString(EditorPrefsKey, string.Empty);

            if (string.IsNullOrEmpty(rawValue))
            {
                return new List<string>();
            }

            return rawValue.Split(Separator).ToList();
        }

        private static void SetOrderedGuids(List<string> guids)
        {
            string rawValue = string.Join(Separator, guids);

            EditorPrefs.SetString(EditorPrefsKey, rawValue);
        }
    }
}
