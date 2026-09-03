using UnityEngine;

namespace RedlineLegends.UI
{
    /// <summary>
    /// Keeps a full-stretch RectTransform inside <see cref="Screen.safeArea"/> so HUD elements clear
    /// notches, punch-holes and rounded corners. Re-applies when the safe area changes (rotation).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _applied;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea != _applied) Apply();
        }

        private void Apply()
        {
            var area = Screen.safeArea;
            _applied = area;
            float w = Screen.width, h = Screen.height;
            if (w <= 0f || h <= 0f || area.width <= 0f || area.height <= 0f) return;
            var min = new Vector2(area.xMin / w, area.yMin / h);
            var max = new Vector2(area.xMax / w, area.yMax / h);
            if (float.IsNaN(min.x) || float.IsNaN(min.y) || float.IsNaN(max.x) || float.IsNaN(max.y)) return;
            _rect.anchorMin = min;
            _rect.anchorMax = max;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
