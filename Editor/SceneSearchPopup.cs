using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WendellLeao.SceneSwitcher.Editor
{
    internal sealed class SceneSearchPopup : PopupWindowContent
    {
        private const float MinWindowWidth = 260f;
        private const float StarWidth = 16f;
        private const float NameWidthPadding = 40f;
        private const float MinWindowHeight = 90f;
        private const float MaxWindowHeight = 320f;
        private const float ToolbarHeight = 20f;
        private const float SectionSpacing = 6f;
        private const float ElementSpacing = 3f;
        private const float BottomPadding = 28f;

        private static readonly Color ActiveSceneColor = EditorGUIUtility.isProSkin
            ? new Color(0.24f, 0.38f, 0.63f, 0.55f)
            : new Color(0.24f, 0.48f, 0.90f, 0.35f);

        private static readonly Color HoverColor = EditorGUIUtility.isProSkin
            ? new Color(1f, 1f, 1f, 0.06f)
            : new Color(0f, 0f, 0f, 0.06f);

        private readonly float _width;
        private Vector2 _scrollPosition;
        private string _search = string.Empty;

        public SceneSearchPopup(float width)
        {
            _width = width;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(_width, ComputeHeight());
        }

        public override void OnOpen()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        public override void OnClose()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            editorWindow.Repaint();
        }

        public override void OnGUI(Rect rect)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
            }

            List<SceneEntry> filteredEntries = FilterEntries(_search);
            List<SceneEntry> starredEntries = filteredEntries.Where(entry => SceneStarred.IsStarred(entry.Guid)).ToList();
            List<SceneEntry> otherEntries = filteredEntries.Where(entry => !SceneStarred.IsStarred(entry.Guid)).ToList();

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));

            if (starredEntries.Count > 0)
            {
                GUILayout.Label("Starred", EditorStyles.miniBoldLabel);

                foreach (SceneEntry entry in starredEntries)
                {
                    DrawSceneRow(entry);
                }

                GUILayout.Space(6f);
            }

            GUILayout.Label("All Scenes", EditorStyles.miniBoldLabel);

            foreach (SceneEntry entry in otherEntries)
            {
                DrawSceneRow(entry);
            }

            GUILayout.EndScrollView();
        }

        private float ComputeHeight()
        {
            List<SceneEntry> filteredEntries = FilterEntries(_search);
            int starredCount = filteredEntries.Count(entry => SceneStarred.IsStarred(entry.Guid));
            int otherCount = filteredEntries.Count - starredCount;

            float lineHeight = EditorGUIUtility.singleLineHeight;
            int rowCount = starredCount + otherCount;
            int headerCount = starredCount > 0 ? 2 : 1;

            float height = ToolbarHeight
                           + headerCount * lineHeight
                           + rowCount * lineHeight
                           + (starredCount > 0 ? SectionSpacing : 0f)
                           + (rowCount + headerCount) * ElementSpacing
                           + BottomPadding;

            return Mathf.Clamp(height, MinWindowHeight, MaxWindowHeight);
        }

        public static float ComputeWidth()
        {
            float maxNameWidth = 0f;

            foreach (SceneEntry entry in SceneCatalog.Entries)
            {
                float nameWidth = EditorStyles.boldLabel.CalcSize(new GUIContent(entry.Name)).x;

                maxNameWidth = Mathf.Max(maxNameWidth, nameWidth);
            }

            float contentWidth = StarWidth + maxNameWidth + NameWidthPadding;

            return Mathf.Max(contentWidth, MinWindowWidth);
        }

        private void DrawSceneRow(SceneEntry entry)
        {
            Rect rowRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

            Event evt = Event.current;
            bool isActiveScene = SceneManager.GetActiveScene().path == entry.Path;
            bool isHovered = rowRect.Contains(evt.mousePosition);

            if (evt.type == EventType.Repaint)
            {
                if (isActiveScene)
                {
                    EditorGUI.DrawRect(rowRect, ActiveSceneColor);
                }
                else if (isHovered)
                {
                    EditorGUI.DrawRect(rowRect, HoverColor);
                }
            }

            Rect starRect = new Rect(rowRect.xMax - StarWidth, rowRect.y, StarWidth, rowRect.height);
            Rect nameRect = new Rect(rowRect.x, rowRect.y, rowRect.width - StarWidth, rowRect.height);

            GUIStyle nameStyle = isActiveScene ? EditorStyles.boldLabel : EditorStyles.label;

            GUI.Label(nameRect, new GUIContent(entry.Name, entry.Path), nameStyle);

            bool isStarred = SceneStarred.IsStarred(entry.Guid);
            GUIContent starContent = new GUIContent(isStarred ? "★" : "☆", isStarred ? "Remove from Starred" : "Add to Starred");

            if (GUI.Button(starRect, starContent, EditorStyles.label))
            {
                SceneStarred.Toggle(entry.Guid);
            }

            if (evt.type == EventType.MouseDown && nameRect.Contains(evt.mousePosition))
            {
                if (evt.button == 0)
                {
                    if (OpenScene(entry, additive: evt.shift))
                    {
                        editorWindow.Close();
                    }

                    evt.Use();
                }
                else if (evt.button == 1)
                {
                    PingScene(entry);
                    evt.Use();
                }
            }
        }

        private List<SceneEntry> FilterEntries(string search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return SceneCatalog.Entries.ToList();
            }

            return SceneCatalog.Entries
                .Where(entry => entry.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }

        private static bool OpenScene(SceneEntry entry, bool additive)
        {
            OpenSceneMode mode = additive ? OpenSceneMode.Additive : OpenSceneMode.Single;

            if (mode == OpenSceneMode.Single && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return false;
            }

            Scene scene = EditorSceneManager.OpenScene(entry.Path, mode);

            SceneManager.SetActiveScene(scene);

            return true;
        }

        private static void PingScene(SceneEntry entry)
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(entry.Path);

            EditorGUIUtility.PingObject(sceneAsset);
        }
    }
}
