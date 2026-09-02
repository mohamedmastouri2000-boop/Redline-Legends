using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RedlineLegends.UI
{
    /// <summary>Label + "&lt; value &gt;" selector for enum-like settings. Touch friendly, no dropdowns.</summary>
    public sealed class CycleRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;

        private string[] _options = Array.Empty<string>();
        private int _index;

        public int Index => _index;
        public event Action<int> Changed;

        private void Awake()
        {
            if (prevButton != null) prevButton.onClick.AddListener(() => Step(-1));
            if (nextButton != null) nextButton.onClick.AddListener(() => Step(1));
        }

        public void Setup(string label, string[] options, int index)
        {
            if (labelText != null) labelText.text = label;
            _options = options ?? Array.Empty<string>();
            _index = Mathf.Clamp(index, 0, Mathf.Max(0, _options.Length - 1));
            Refresh();
        }

        public void SetIndex(int index)
        {
            _index = Mathf.Clamp(index, 0, Mathf.Max(0, _options.Length - 1));
            Refresh();
        }

        private void Step(int delta)
        {
            if (_options.Length == 0) return;
            _index = (_index + delta + _options.Length) % _options.Length;
            Refresh();
            Changed?.Invoke(_index);
        }

        private void Refresh()
        {
            if (valueText != null) valueText.text = _options.Length > 0 ? _options[_index] : "";
        }

#if UNITY_EDITOR
        public void EditorWire(TMP_Text label, TMP_Text value, Button prev, Button next)
        {
            labelText = label; valueText = value; prevButton = prev; nextButton = next;
        }
#endif
    }
}
