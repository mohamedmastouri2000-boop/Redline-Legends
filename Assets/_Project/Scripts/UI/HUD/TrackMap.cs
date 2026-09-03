using RedlineLegends.Race;
using RedlineLegends.Vehicles;
using UnityEngine;
using UnityEngine.UI;

namespace RedlineLegends.UI
{
    /// <summary>
    /// Minimap: a pre-drawn top-down sprite of the racing line with a dot that follows the local
    /// car. World XZ is mapped into the map rect using the bounds baked at generation time.
    /// </summary>
    public sealed class TrackMap : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour localRacerSource;
        [SerializeField] private RectTransform mapRect;
        [SerializeField] private RectTransform playerDot;
        [SerializeField] private Vector2 worldMin;
        [SerializeField] private Vector2 worldSize;
        [SerializeField] private float padding = 12f;

        private VehicleController _vehicle;

        private void Start()
        {
            if (localRacerSource is ILocalRacerSource source)
            {
                if (source.LocalVehicle != null) Bind(source.LocalVehicle);
                source.LocalVehicleSpawned += Bind;
            }
        }

        private void Bind(VehicleController vehicle)
        {
            _vehicle = vehicle;
            if (playerDot != null) playerDot.gameObject.SetActive(vehicle != null);
        }

        private void LateUpdate()
        {
            if (_vehicle == null || mapRect == null || playerDot == null || worldSize.x <= 0f || worldSize.y <= 0f) return;
            var p = _vehicle.transform.position;
            float u = Mathf.Clamp01((p.x - worldMin.x) / worldSize.x);
            float v = Mathf.Clamp01((p.z - worldMin.y) / worldSize.y);
            var size = mapRect.rect.size;
            playerDot.anchoredPosition = new Vector2(
                Mathf.Lerp(padding, size.x - padding, u),
                Mathf.Lerp(padding, size.y - padding, v));
        }

#if UNITY_EDITOR
        public void EditorWire(MonoBehaviour source, RectTransform map, RectTransform dot, Vector2 min, Vector2 size, float pad)
        {
            localRacerSource = source; mapRect = map; playerDot = dot; worldMin = min; worldSize = size; padding = pad;
        }
#endif
    }
}
