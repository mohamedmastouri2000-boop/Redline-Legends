using RedlineLegends.Core;
using RedlineLegends.Progression;
using RedlineLegends.Vehicles;
using UnityEngine;

namespace RedlineLegends.UI
{
    /// <summary>
    /// Shows the selected car on a slowly turning stage behind the main menu. Reads the garage
    /// selection and paint; swaps the visual when the selection changes. Purely cosmetic.
    /// </summary>
    public sealed class MenuShowcase : MonoBehaviour
    {
        [SerializeField] private Transform turntable;
        [SerializeField] private float spinDegreesPerSecond = 9f;

        private GarageService _garage;
        private GameObject _car;
        private string _shownId;

        private void Start()
        {
            if (!Services.IsReady || turntable == null) return;
            _garage = Services.Get<GarageService>();
            _garage.Changed += Refresh;
            turntable.localRotation = Quaternion.Euler(0f, 210f, 0f);
            Refresh();
        }

        private void OnDestroy()
        {
            if (_garage != null) _garage.Changed -= Refresh;
        }

        private void Update()
        {
            if (turntable != null) turntable.Rotate(0f, spinDegreesPerSecond * Time.deltaTime, 0f, Space.Self);
        }

        private void Refresh()
        {
            var def = _garage.SelectedVehicle;
            if (def == null || def.VisualPrefab == null) return;
            if (_car == null || _shownId != def.Id)
            {
                if (_car != null) Destroy(_car);
                _car = Instantiate(def.VisualPrefab, turntable);
                _car.transform.localPosition = Vector3.zero;
                _car.transform.localRotation = Quaternion.identity;
                _shownId = def.Id;
            }
            int paint = _garage.GetOwned(def.Id)?.PaintIndex ?? 0;
            VehicleVisualUtility.ApplyPaint(_car, def, paint);
        }

#if UNITY_EDITOR
        public void EditorWire(Transform stage) { turntable = stage; }
#endif
    }
}
