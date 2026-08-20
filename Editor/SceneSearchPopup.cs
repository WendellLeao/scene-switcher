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
        private enum SceneSection
        {
            Starred,
            All,
            Hidden
        }

        private const float MinWindowWidth = 260f;
        private const float StarWidth = 16f;
        private const float HideWidth = 16f;
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
        private bool _showHidden;

        private string _mouseDownGuid;
        private Vector2 _mouseDownPosition;

        private string _draggingGuid;
        private string _dropTargetGuid;
        private bool _dropInsertAfter;
        private SceneSection _dropTargetSection;

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

            List<SceneEntry> hiddenEntries = filteredEntries.Where(entry => SceneHidden.IsHidden(entry.Guid)).ToList();
            List<SceneEntry> shownEntries = filteredEntries.Where(entry => !SceneHidden.IsHidden(entry.Guid)).ToList();

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));

            if (isSearching)
            {
                List<SceneEntry> searchEntries = _showHidden ? filteredEntries : shownEntries;

                foreach (SceneEntry entry in searchEntries)
                {
                    DrawSceneRow(entry, ResolveSection(entry));
                }
            }
            else
            {
                List<SceneEntry> starredEntries = shownEntries.Where(entry => SceneStarred.IsStarred(entry.Guid)).ToList();
                List<SceneEntry> otherEntries = shownEntries.Where(entry => !SceneStarred.IsStarred(entry.Guid)).ToList();

                bool showStarredSection = starredEntries.Count > 0;
                bool showAllSection = otherEntries.Count > 0;
                bool showHiddenSection = _showHidden && hiddenEntries.Count > 0;
                bool toggleRendered = false;
                bool sectionRendered = false;

                if (showStarredSection)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Starred", EditorStyles.miniBoldLabel);
                        GUILayout.FlexibleSpace();
                        DrawShowHiddenToggle(hiddenEntries.Count);
                    }

                    toggleRendered = true;

                    foreach (SceneEntry entry in starredEntries)
                    {
                        DrawSceneRow(entry, SceneSection.Starred);
                    }

                    sectionRendered = true;
                }

                if (showAllSection)
                {
                    if (sectionRendered)
                    {
                        GUILayout.Space(SectionSpacing);
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label("All Scenes", EditorStyles.miniBoldLabel);

                        if (!toggleRendered)
                        {
                            GUILayout.FlexibleSpace();
                            DrawShowHiddenToggle(hiddenEntries.Count);
                            toggleRendered = true;
                        }
                    }

                    foreach (SceneEntry entry in otherEntries)
                    {
                        DrawSceneRow(entry, SceneSection.All);
                    }

                    sectionRendered = true;
                }

                if (showHiddenSection)
                {
                    if (sectionRendered)
                    {
                        GUILayout.Space(SectionSpacing);
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Hidden", EditorStyles.miniBoldLabel);

                        if (!toggleRendered)
                        {
                            GUILayout.FlexibleSpace();
                            DrawShowHiddenToggle(hiddenEntries.Count);
                            toggleRendered = true;
                        }
                    }

                    foreach (SceneEntry entry in hiddenEntries)
                    {
                        DrawSceneRow(entry, SceneSection.Hidden);
                    }
                }

                if (!toggleRendered && hiddenEntries.Count > 0)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        DrawShowHiddenToggle(hiddenEntries.Count);
                    }
                }
            }

            GUILayout.EndScrollView();

            HandleDragRelease();
        }

        private float ComputeHeight()
        {
            List<SceneEntry> filteredEntries = FilterEntries(_search);
            bool isSearching = !string.IsNullOrWhiteSpace(_search);
            int hiddenCount = filteredEntries.Count(entry => SceneHidden.IsHidden(entry.Guid));
            int shownCount = filteredEntries.Count - hiddenCount;

            float lineHeight = EditorGUIUtility.singleLineHeight;
            int rowCount;
            int headerCount;
            float sectionSpacing;

            if (isSearching)
            {
                rowCount = _showHidden ? filteredEntries.Count : shownCount;
                headerCount = 0;
                sectionSpacing = 0f;
            }
            else
            {
                int starredCount = filteredEntries.Count(entry => !SceneHidden.IsHidden(entry.Guid) && SceneStarred.IsStarred(entry.Guid));
                int otherCount = shownCount - starredCount;
                bool showStarredSection = starredCount > 0;
                bool showAllSection = otherCount > 0;
                bool showHiddenSection = _showHidden && hiddenCount > 0;

                int sectionCount = (showStarredSection ? 1 : 0) + (showAllSection ? 1 : 0) + (showHiddenSection ? 1 : 0);
                bool showFallbackToggleRow = sectionCount == 0 && hiddenCount > 0;

                rowCount = starredCount + otherCount + (showHiddenSection ? hiddenCount : 0);
                headerCount = sectionCount + (showFallbackToggleRow ? 1 : 0);
                sectionSpacing = sectionCount > 0 ? (sectionCount - 1) * SectionSpacing : 0f;
            }

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

            float contentWidth = StarWidth + HideWidth + maxNameWidth + NameWidthPadding;

            return Mathf.Max(contentWidth, MinWindowWidth);
        }

        private void DrawShowHiddenToggle(int hiddenCount)
        {
            if (hiddenCount == 0)
            {
                return;
            }

            GUIContent toggleContent = new GUIContent(_showHidden ? SceneSwitcherIcons.VisibilityOn : SceneSwitcherIcons.VisibilityOff,
                _showHidden ? "Hide the hidden scenes" : "Show hidden scenes");

            if (GUILayout.Button(toggleContent, EditorStyles.label, GUILayout.Width(StarWidth), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
            {
                _showHidden = !_showHidden;
            }
        }

        private static SceneSection ResolveSection(SceneEntry entry)
        {
            if (SceneHidden.IsHidden(entry.Guid))
            {
                return SceneSection.Hidden;
            }

            return SceneStarred.IsStarred(entry.Guid) ? SceneSection.Starred : SceneSection.All;
        }

        private void DrawSceneRow(SceneEntry entry, SceneSection section)
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
            Rect hideRect = new Rect(starRect.x - HideWidth, rowRect.y, HideWidth, rowRect.height);
            Rect nameRect = new Rect(rowRect.x, rowRect.y, rowRect.width - StarWidth - HideWidth, rowRect.height);

            GUIStyle nameStyle = isActiveScene ? EditorStyles.boldLabel : EditorStyles.label;

            GUI.Label(nameRect, new GUIContent(entry.Name, entry.Path), nameStyle);

            bool isHidden = SceneHidden.IsHidden(entry.Guid);
            GUIContent hideContent = new GUIContent(isHidden ? SceneSwitcherIcons.VisibilityOff : SceneSwitcherIcons.VisibilityOn, isHidden ? "Show scene" : "Hide from Scene Search");

            if (GUI.Button(hideRect, hideContent, EditorStyles.label))
            {
                SceneHidden.Toggle(entry.Guid);
            }

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
                _dropTargetSection = section;
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

                    bool shouldBeHidden = _dropTargetSection == SceneSection.Hidden;

                    if (SceneHidden.IsHidden(_draggingGuid) != shouldBeHidden)
                    {
                        SceneHidden.Toggle(_draggingGuid);
                    }

                    if (_dropTargetSection != SceneSection.Hidden)
                    {
                        bool shouldBeStarred = _dropTargetSection == SceneSection.Starred;

                        if (SceneStarred.IsStarred(_draggingGuid) != shouldBeStarred)
                        {
                            SceneStarred.Toggle(_draggingGuid);
                        }
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
