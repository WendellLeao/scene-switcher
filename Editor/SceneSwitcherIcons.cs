using UnityEditor;
using UnityEngine;

namespace WendellLeao.SceneSwitcher.Editor
{
    internal static class SceneSwitcherIcons
    {
        private static readonly Texture2D VisibilityOnDark = Resources.Load<Texture2D>("visibility-on-dark");
        private static readonly Texture2D VisibilityOnLight = Resources.Load<Texture2D>("visibility-on-light");
        private static readonly Texture2D VisibilityOffDark = Resources.Load<Texture2D>("visibility-off-dark");
        private static readonly Texture2D VisibilityOffLight = Resources.Load<Texture2D>("visibility-off-light");

        public static Texture2D VisibilityOn => EditorGUIUtility.isProSkin ? VisibilityOnDark : VisibilityOnLight;
        public static Texture2D VisibilityOff => EditorGUIUtility.isProSkin ? VisibilityOffDark : VisibilityOffLight;
    }
}
