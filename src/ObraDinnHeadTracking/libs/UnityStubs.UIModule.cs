// Unity stub for CI builds — UIModule slice
namespace UnityEngine {
    public class Canvas : Behaviour {
        public static event WillRenderCanvases willRenderCanvases;
        public delegate void WillRenderCanvases();
        public RenderMode renderMode { get; set; }
        public Camera worldCamera { get; set; }
        public float scaleFactor { get; set; }
    }
    public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }
    public class RectTransform : Transform {
        public Vector2 anchoredPosition { get; set; }
        public Vector2 sizeDelta { get; set; }
        public Vector2 anchorMin { get; set; }
        public Vector2 anchorMax { get; set; }
        public Vector2 pivot { get; set; }
        public Rect rect { get; }
    }
    public class CanvasGroup : Behaviour {
        public float alpha { get; set; }
        public bool interactable { get; set; }
        public bool blocksRaycasts { get; set; }
    }
}
