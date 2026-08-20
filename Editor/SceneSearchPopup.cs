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
        private const float DropIndicatorThickness = 2f;
        private const float DragThreshold = 4f;

        private static readonly Color ActiveSceneColor = EditorGUIUtility.isProSkin
            ? new Color(0.24f, 0.38f, 0.63f, 0.55f)
            : new Color(0.24f, 0.48f, 0.90f, 0.35f);

        private static readonly Color HoverColor = EditorGUIUtility.isProSkin
            ? new Color(1f, 1f, 1f, 0.06f)
            : new Color(0f, 0f, 0f, 0.06f);

        private static readonly Color DraggingRowColor = EditorGUIUtility.isProSkin
            ? new Color(1f, 1f, 1f, 0.10f)
            : new Color(0f, 0f, 0f, 0.10f);

        private static readonly Color DropIndicatorColor = EditorGUIUtility.isProSkin
            ? new Color(0.4f, 0.7f, 1f, 1f)
            : new Color(0.1f, 0.4f, 0.9f, 1f);

        private readonly float _width;
        private Vector2 _scrollPosition;
        private string _search = string.Empty;

        private string _mouseDownGuid;
        private Vector2 _mouseDownPosition;

        private string _draggingGuid;
        private string _dropTargetGuid;
        private bool _dropInsertAfter;
        private bool _dropTargetIsStarredSection;

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
            bool isSearching = !string.IsNullOrWhiteSpace(_search);

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));

            if (isSearching)
            {
                foreach (SceneEntry entry in filteredEntries)
                {
                    DrawSceneRow(entry, isStarredSection: SceneStarred.IsStarred(entry.Guid));
                }
            }
            else
            {
                List<SceneEntry> starredEntries = filteredEntries.Where(entry => SceneStarred.IsStarred(entry.Guid)).ToList();
                List<SceneEntry> otherEntries = filteredEntries.Where(entry => !SceneStarred.IsStarred(entry.Guid)).ToList();

                if (starredEntries.Count > 0)
                {
                    GUILayout.Label("Starred", EditorStyles.miniBoldLabel);

                    foreach (SceneEntry entry in starredEntries)
                    {
                        DrawSceneRow(entry, isStarredSection: true);
                    }

                    GUILayout.Space(6f);
                }

                GUILayout.Label("All Scenes", EditorStyles.miniBoldLabel);

                foreach (SceneEntry entry in otherEntries)
                {
                    DrawSceneRow(entry, isStarredSection: false);
                }
            }

            GUILayout.EndScrollView();

            HandleDragRelease();
        }

        private float ComputeHeight()
        {
            List<SceneEntry> filteredEntries = FilterEntries(_search);
            bool isSearching = !string.IsNullOrWhiteSpace(_search);
            int starredCount = filteredEntries.Count(entry => SceneStarred.IsStarred(entry.Guid));
            int otherCount = filteredEntries.Count - starredCount;

            float lineHeight = EditorGUIUtility.singleLineHeight;
            int rowCount = starredCount + otherCount;
            int headerCount = isSearching ? 0 : (starredCount > 0 ? 2 : 1);
            float sectionSpacing = isSearching ? 0f : (starredCount > 0 ? SectionSpacing : 0f);

            float height = ToolbarHeight
                           + headerCount * lineHeight
                           + rowCount * lineHeight
                           + sectionSpacing
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

        private void DrawSceneRow(SceneEntry entry, bool isStarredSection)
        {
            Rect rowRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

            Event evt = Event.current;
            bool isActiveScene = SceneManager.GetActiveScene().path == entry.Path;
            bool isHovered = rowRect.Contains(evt.mousePosition);
            bool isDragging = _draggingGuid != null;
            bool isBeingDragged = _draggingGuid == entry.Guid;

            if (evt.type == EventType.Repaint)
            {
                if (isBeingDragged)
                {
                    EditorGUI.DrawRect(rowRect, DraggingRowColor);
                }
                else if (isActiveScene)
                {
                    EditorGUI.DrawRect(rowRect, ActiveSceneColor);
                }
                else if (isHovered && !isDragging)
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
                    _mouseDownGuid = entry.Guid;
                    _mouseDownPosition = evt.mousePosition;
                    evt.Use();
                }
                else if (evt.button == 1)
                {
                    PingScene(entry);
                    evt.Use();
                }
            }

            if (!isDragging && _mouseDownGuid == entry.Guid && evt.type == EventType.MouseDrag
                && (evt.mousePosition - _mouseDownPosition).sqrMagnitude > DragThreshold * DragThreshold)
            {
                _draggingGuid = entry.Guid;
                _dropTargetGuid = null;
            }

            if (isDragging && !isBeingDragged && (evt.type == EventType.MouseDrag || evt.type == EventType.Repaint) && isHovered)
            {
                _dropTargetGuid = entry.Guid;
                _dropInsertAfter = evt.mousePosition.y > rowRect.center.y;
                _dropTargetIsStarredSection = isStarredSection;
            }

            if (evt.type == EventType.Repaint && isDragging && !isBeingDragged && _dropTargetGuid == entry.Guid)
            {
                float indicatorY = _dropInsertAfter ? rowRect.yMax - DropIndicatorThickness : rowRect.yMin;
                Rect indicatorRect = new Rect(rowRect.x, indicatorY, rowRect.width, DropIndicatorThickness);

                EditorGUI.DrawRect(indicatorRect, DropIndicatorColor);
            }
        }

        private void HandleDragRelease()
        {
            Event evt = Event.current;

            if (evt.type != EventType.MouseUp)
            {
                return;
            }

            if (_draggingGuid != null)
            {
                if (_dropTargetGuid != null)
                {
                    if (_dropInsertAfter)
                    {
                        SceneOrder.MoveAfter(_draggingGuid, _dropTargetGuid);
                    }
                    else
                    {
                        SceneOrder.MoveBefore(_draggingGuid, _dropTargetGuid);
                    }

                    if (SceneStarred.IsStarred(_draggingGuid) != _dropTargetIsStarredSection)
                    {
                        SceneStarred.Toggle(_draggingGuid);
                    }
                }

                _draggingGuid = null;
                _dropTargetGuid = null;
                _mouseDownGuid = null;
                evt.Use();

                return;
            }

            if (_mouseDownGuid != null)
            {
                SceneEntry? entry = FindEntry(_mouseDownGuid);
                bool additive = evt.shift;

                _mouseDownGuid = null;

                if (entry.HasValue && OpenScene(entry.Value, additive))
                {
                    editorWindow.Close();
                }

                evt.Use();
            }
        }

        private static SceneEntry? FindEntry(string guid)
        {
            foreach (SceneEntry entry in SceneCatalog.Entries)
            {
                if (entry.Guid == guid)
                {
                    return entry;
                }
            }

            return null;
        }

        private List<SceneEntry> FilterEntries(string search)
        {
            IEnumerable<SceneEntry> entries = SceneCatalog.Entries;

            if (!string.IsNullOrWhiteSpace(search))
            {
                entries = entries.Where(entry => entry.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            return SceneOrder.Sort(entries);
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
