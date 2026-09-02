using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RedlineLegends.UI
{
    /// <summary>Label + slider + numeric readout. Emits on release-free continuous change.</summary>
    public sealed class SliderRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private Slider slider;

        private string _format = "0.00";
        private bool _suppress;

        public float Value => slider != null ? slider.value : 0f;
        public event Action<float> Changed;

        private void Awake()
        {
            if (slider != null) slider.onValueChanged.AddListener(OnSlider);
        }

        public void Setup(string label, float min, float max, float value, string format = "0.00")
        {
            _format = format;
            if (labelText != null) labelText.text = label;
            if (slider != null)
            {
                _suppress = true;
                slider.minValue = min;
                slider.maxValue = max;
                slider.value = value;
                _suppress = false;
            }
            Refresh();
        }

        public void SetValue(float value)
        {
            if (slider == null) return;
            _suppress = true;
            slider.value = value;
            _suppress = false;
            Refresh();
        }

        private void OnSlider(float value)
        {
            Refresh();
            if (!_suppress) Changed?.Invoke(value);
        }

        private void Refresh()
        {
            if (valueText != null && slider != null) valueText.text = slider.value.ToString(_format);
        }

#if UNITY_EDITOR
        public void EditorWire(TMP_Text label, TMP_Text value, Slider s)
        {
            labelText = label; valueText = value; slider = s;
        }
#endif
    }
}
