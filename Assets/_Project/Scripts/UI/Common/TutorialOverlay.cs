using System;
using RedlineLegends.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RedlineLegends.UI
{
    /// <summary>Short paged tutorial with NEXT and SKIP. Hidden when idle; the caller decides when to show it.</summary>
    public sealed class TutorialOverlay : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private TMP_Text pageText;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button skipButton;

        private TutorialPage[] _pages = Array.Empty<TutorialPage>();
        private int _index;
        private Action _onDone;

        public bool IsShowing => gameObject.activeSelf;

        private void Awake()
        {
            if (nextButton != null) nextButton.onClick.AddListener(Next);
            if (skipButton != null) skipButton.onClick.AddListener(Finish);
            gameObject.SetActive(false);
        }

        public void Show(TutorialPage[] pages, Action onDone)
        {
            _pages = pages ?? Array.Empty<TutorialPage>();
            _onDone = onDone;
            _index = 0;
            if (_pages.Length == 0)
            {
                Finish();
                return;
            }
            gameObject.SetActive(true);
            Render();
        }

        private void Render()
        {
            var page = _pages[_index];
            if (titleText != null) titleText.text = page.Title.ToUpperInvariant();
            if (bodyText != null) bodyText.text = page.Body;
            if (pageText != null) pageText.text = (_index + 1) + " / " + _pages.Length;
            if (nextButton != null)
            {
                var label = nextButton.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = _index == _pages.Length - 1 ? "GO" : "NEXT";
            }
        }

        private void Next()
        {
            if (_index < _pages.Length - 1)
            {
                _index++;
                Render();
            }
            else Finish();
        }

        public void Finish()
        {
            gameObject.SetActive(false);
            var done = _onDone;
            _onDone = null;
            done?.Invoke();
        }

#if UNITY_EDITOR
        public void EditorWire(TMP_Text title, TMP_Text body, TMP_Text page, Button next, Button skip)
        {
            titleText = title; bodyText = body; pageText = page; nextButton = next; skipButton = skip;
        }
#endif
    }
}
