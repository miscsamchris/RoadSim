using UnityEngine;


namespace AmazingAssets.RenderMonster
{    public static class RenderMonsterEditorGUIHelper
    {
        private static GUIStyle style;

        public static void Header(string title)
        {
            if (style == null)
                style = "ShurikenModuleTitle";

            Rect rect = GUILayoutUtility.GetRect(16, 22f, style);
            GUI.Box(rect, title, style);
        }
    }
}

