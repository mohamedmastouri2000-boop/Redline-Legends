using UnityEngine;
using UnityEngine.EventSystems;

namespace RedlineLegends.UI
{
    /// <summary>
    /// On-screen steering wheel: drag rotates the wheel around its centre; angle maps to steer.
    /// Returns to centre when released.
    /// </summary>
    public sealed class SteeringWheelControl : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform wheelGraphic;
        [SerializeField] private float maxAngle = 120f;
        [SerializeField] private float returnSpeed = 720f;

        private RectTransform _rect;
        private float _angle;
        private float _lastPointerAngle;
        private int _pointerId = int.MinValue;

        public bool IsActive => _pointerId != int.MinValue;
        /// <summary>-1..1</summary>
        public float Value => Mathf.Clamp(-_angle / maxAngle, -1f, 1f);

        private void Awake()
        {
            _rect = (RectTransform)transform;
            if (wheelGraphic == null) wheelGraphic = _rect;
        }

        private void Update()
        {
            if (!IsActive)
                _angle = Mathf.MoveTowards(_angle, 0f, returnSpeed * Time.deltaTime);
            wheelGraphic.localRotation = Quaternion.Euler(0f, 0f, _angle);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (IsActive) return;
            _pointerId = eventData.pointerId;
            _lastPointerAngle = PointerAngle(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;
            float now = PointerAngle(eventData);
            float delta = Mathf.DeltaAngle(_lastPointerAngle, now);
            _lastPointerAngle = now;
            _angle = Mathf.Clamp(_angle + delta, -maxAngle, maxAngle);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;
            _pointerId = int.MinValue;
        }

        private void OnDisable() => _pointerId = int.MinValue;

        private float PointerAngle(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, eventData.position, eventData.pressEventCamera, out var local);
            return Mathf.Atan2(local.y, local.x) * Mathf.Rad2Deg;
        }

#if UNITY_EDITOR
        public void EditorWire(RectTransform graphic) { wheelGraphic = graphic; }
#endif
    }
}
