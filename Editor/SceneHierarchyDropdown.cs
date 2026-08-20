using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WendellLeao.SceneSwitcher.Editor
{
    [InitializeOnLoad]
    internal static class SceneHierarchyDropdown
    {
        private struct RowAnchor
        {
            public float YMin;
            public float YMax;
            public int SceneIndex;
        }

        private const float ArrowSize = 16f;
        private const float NameGap = 4f;
        private const float RightPadding = 20f;
        private const float PositionTolerance = 0.5f;

        private static readonly GUIContent ArrowContent = new("▾", "Open Scene Searcher");
        private static readonly List<RowAnchor> RowAnchors = new();

        private static int _sceneHeaderSequence;

        static SceneHierarchyDropdown()
        {
#if UNITY_6000_4_OR_NEWER
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI -= OnHierarchyWindowItemGUI;
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += OnHierarchyWindowItemGUI;
#else
            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyWindowItemGUI;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemGUI;
#endif
        }

#if UNITY_6000_4_OR_NEWER
        private static void OnHierarchyWindowItemGUI(EntityId entityId, Rect selectionRect)
        {
            if (Application.isPlaying)
            {
                return;
            }

            Event evt = Event.current;

            if (evt.type == EventType.Layout)
            {
                RowAnchors.Clear();
                _sceneHeaderSequence = 0;
            }

            GameObject gameObject = EditorUtility.EntityIdToObject(entityId) as GameObject;
#else
        private static void OnHierarchyWindowItemGUI(int instanceID, Rect selectionRect)
        {
            if (Application.isPlaying)
            {
                return;
            }

            Event evt = Event.current;

            if (evt.type == EventType.Layout)
            {
                RowAnchors.Clear();
                _sceneHeaderSequence = 0;
            }

            GameObject gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
#endif

            if (gameObject != null || SceneManager.sceneCount == 0)
            {
                return;
            }

            int sceneIndex = ResolveSceneIndex(evt, selectionRect);

            if (sceneIndex < 0)
            {
                return;
            }

            Scene scene = SceneManager.GetSceneAt(sceneIndex);
            Rect arrowRect = ComputeArrowRect(selectionRect, scene);

            if (GUI.Button(arrowRect, ArrowContent, EditorStyles.label))
            {
                float width = SceneSearchPopup.ComputeWidth();

                PopupWindow.Show(arrowRect, new SceneSearchPopup(width));
            }
        }

        private static int ResolveSceneIndex(Event evt, Rect selectionRect)
        {
            if (evt.type == EventType.Repaint)
            {
                int sceneIndex = Mathf.Clamp(_sceneHeaderSequence, 0, SceneManager.sceneCount - 1);

                RowAnchors.Add(new RowAnchor
                {
                    YMin = selectionRect.y,
                    YMax = selectionRect.y + selectionRect.height,
                    SceneIndex = sceneIndex
                });

                _sceneHeaderSequence++;

                return sceneIndex;
            }

            foreach (RowAnchor anchor in RowAnchors)
            {
                if (selectionRect.y >= anchor.YMin - PositionTolerance && selectionRect.y <= anchor.YMax + PositionTolerance)
                {
                    return anchor.SceneIndex;
                }
            }

            return -1;
        }

        private static Rect ComputeArrowRect(Rect selectionRect, Scene scene)
        {
            float iconWidth = selectionRect.height;
            float nameStartX = selectionRect.x + iconWidth + NameGap;

            GUIStyle nameStyle = scene == SceneManager.GetActiveScene() ? EditorStyles.boldLabel : EditorStyles.label;
            float nameWidth = nameStyle.CalcSize(new GUIContent(scene.name)).x;

            float maxArrowX = selectionRect.xMax - RightPadding - ArrowSize;
            float arrowX = Mathf.Min(nameStartX + nameWidth + NameGap, maxArrowX);
            float arrowY = selectionRect.y + (selectionRect.height - ArrowSize) * 0.5f;

            return new Rect(arrowX, arrowY, ArrowSize, ArrowSize);
        }
    }
}
