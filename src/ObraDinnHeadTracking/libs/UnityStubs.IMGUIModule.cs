// Unity stub for CI builds — IMGUIModule slice
namespace UnityEngine {
    public static class GUIUtility {
        public static int keyboardControl { get; set; }
        public static int hotControl { get; set; }
    }
    public class GUIStyle {
        public GUIStyle() { }
        public GUIStyle(GUIStyle other) { }
        public int fontSize { get; set; }
        public TextAnchor alignment { get; set; }
        public GUIStyleState normal { get; set; }
        public Font font { get; set; }
        public bool wordWrap { get; set; }
        public FontStyle fontStyle { get; set; }
        public Vector2 CalcSize(GUIContent content) => default;
    }
    public enum FontStyle { Normal, Bold, Italic, BoldAndItalic }
    public class GUIStyleState {
        public Color textColor { get; set; }
        public Texture2D background { get; set; }
    }
    public static class GUI {
        public static Color color { get; set; }
        public static Color backgroundColor { get; set; }
        public static Color contentColor { get; set; }
        public static Matrix4x4 matrix { get; set; }
        public static GUISkin skin { get; set; }
        public static void Label(Rect position, string text) { }
        public static void Label(Rect position, string text, GUIStyle style) { }
        public static void Label(Rect position, GUIContent content) { }
        public static void Label(Rect position, GUIContent content, GUIStyle style) { }
        public static void Box(Rect position, string text) { }
        public static void Box(Rect position, string text, GUIStyle style) { }
        public static void Box(Rect position, GUIContent content) { }
        public static void Box(Rect position, GUIContent content, GUIStyle style) { }
        public static bool Button(Rect position, string text) => false;
        public static bool Button(Rect position, string text, GUIStyle style) => false;
        public static void DrawTexture(Rect position, Texture image) { }
        public static void DrawTexture(Rect position, Texture image, ScaleMode scaleMode) { }
        public static void BeginGroup(Rect position) { }
        public static void EndGroup() { }
    }
    public class GUIContent {
        public static GUIContent none => new GUIContent();
        public string text { get; set; }
        public Texture image { get; set; }
        public GUIContent() { }
        public GUIContent(string text) { this.text = text; }
    }
    public class GUISkin : Object {
        public GUIStyle label { get; }
        public GUIStyle button { get; }
        public GUIStyle box { get; }
    }
}
