using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RedlineLegends.UI
{
    /// <summary>
    /// Touch button that reports whether it is held, with multi-touch safety: each button tracks
    /// its own pointer id so a second finger elsewhere never releases it.
    /// </summary>
    public sealed class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerEnterHandler
    {
        [SerializeField] private Image image;
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.35f);
        [SerializeField] private Color heldColor = new Color(1f, 1f, 1f, 0.7f);

        private int _pointerId = int.MinValue;

        public bool IsHeld { get; private set; }
        /// <summary>Set for one frame when the button goes down (edge trigger for shifts).</summary>
        public bool PressedThisFrame { get; private set; }
        private int _pressedFrame = -1;

        private void Awake()
        {
            if (image == null) image = GetComponent<Image>();
            Apply();
        }

        private void LateUpdate()
        {
            PressedThisFrame = _pressedFrame == Time.frameCount;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (IsHeld) return;
            IsHeld = true;
            _pointerId = eventData.pointerId;
            _pressedFrame = Time.frameCount + 1;
            Apply();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;
            Release();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;
            Release();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Sliding a finger onto a pedal engages it (common thumb behaviour on phones).
            // Touch pointers only exist while a finger is down; a mouse must be holding a button.
            if (IsHeld) return;
            bool touch = eventData.pointerId >= 0;
            bool mouseHeld = eventData.pointerId < 0 && eventData.pointerPress != null;
            if (!touch && !mouseHeld) return;
            IsHeld = true;
            _pointerId = eventData.pointerId;
            Apply();
        }

        private void Release()
        {
            IsHeld = false;
            _pointerId = int.MinValue;
            Apply();
        }

        private void OnDisable() => Release();

        private void Apply()
        {
            if (image != null) image.color = IsHeld ? heldColor : normalColor;
        }

#if UNITY_EDITOR
        public void EditorWire(Image target) { image = target; }
#endif
    }
}
