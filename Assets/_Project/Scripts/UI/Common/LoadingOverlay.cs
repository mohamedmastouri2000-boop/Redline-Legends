using RedlineLegends.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RedlineLegends.UI
{
    /// <summary>
    /// Full-screen loading curtain on the persistent AppRoot canvas. Kept deliberately simple:
    /// a fade, a caption and a progress bar. It stays above every scene canvas.
    /// </summary>
    public sealed class LoadingOverlay : MonoBehaviour, ILoadingOverlay
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text caption;
        [SerializeField] private Image progressFill;
        [SerializeField] private float fadeSpeed = 6f;

        private float _targetAlpha;
        private bool _visible;

        public bool IsVisible => _visible;

        private void Awake()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            gameObject.SetActive(true);
        }

        public void Show(string text)
        {
            _visible = true;
            _targetAlpha = 1f;
            group.alpha = 1f; // no fade-in: the load hitch would freeze it halfway anyway
            group.blocksRaycasts = true;
            if (caption != null) caption.text = string.IsNullOrEmpty(text) ? "Loading" : text;
            SetProgress(0f);
        }

        public void SetProgress(float progress01)
        {
            if (progressFill != null) progressFill.fillAmount = Mathf.Clamp01(progress01);
        }

        public void Hide()
        {
            _visible = false;
            _targetAlpha = 0f;
            group.blocksRaycasts = false;
        }

        private void Update()
        {
            if (Mathf.Approximately(group.alpha, _targetAlpha)) return;
            group.alpha = Mathf.MoveTowards(group.alpha, _targetAlpha, fadeSpeed * Time.unscaledDeltaTime);
        }

#if UNITY_EDITOR
        public void EditorWire(CanvasGroup g, TMP_Text c, Image fill)
        {
            group = g;
            caption = c;
            progressFill = fill;
        }
#endif
    }
}
