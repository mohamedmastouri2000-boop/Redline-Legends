using System;
using RedlineLegends.Cameras;
using RedlineLegends.Content;
using RedlineLegends.Core;
using RedlineLegends.Input;
using RedlineLegends.Progression;
using RedlineLegends.Vehicles;
using UnityEngine;

namespace RedlineLegends.Race
{
    /// <summary>
    /// Free-drive session: spawns the local player's car on the proving ground with no race rules.
    /// Used by the garage "Test Drive" button and by developers pressing Play in the scene.
    /// </summary>
    public sealed class TestDriveSession : MonoBehaviour, ILocalRacerSource
    {
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private VehicleCameraRig cameraRig;

        private VehicleController _vehicle;
        private MobileInputProvider _input;
        private SettingsService _settings;

        public VehicleController LocalVehicle => _vehicle;
        public event Action<VehicleController> LocalVehicleSpawned;

        private void Start()
        {
            if (!Services.IsReady)
            {
                GameLog.Error("TestDriveSession: services not booted.");
                return;
            }
            var flow = Services.Get<SceneFlowService>();
            var catalog = Services.Get<ContentCatalog>();
            var garage = Services.Get<GarageService>();
            _settings = Services.Get<SettingsService>();
            _input = Services.Get<MobileInputProvider>();

            var request = flow.ConsumePendingRace();
            RaceParticipantSpec spec = request?.FindLocalPlayer();
            VehicleDefinition definition = null;
            if (spec != null) catalog.TryGetVehicle(spec.VehicleId, out definition);
            if (definition == null)
            {
                definition = garage.SelectedVehicle;
                if (definition == null && catalog.Vehicles.Count > 0) definition = catalog.Vehicles[0];
                spec = new RaceParticipantSpec
                {
                    Id = new RacerId(1),
                    DisplayName = "You",
                    VehicleId = definition != null ? definition.Id : "",
                    ControlSource = ControlSource.LocalPlayer,
                    VehicleSpec = definition != null ? garage.BuildSpec(definition.Id) : null,
                    PaintIndex = definition != null ? garage.GetOwned(definition.Id)?.PaintIndex ?? 0 : 0
                };
            }
            if (definition == null)
            {
                GameLog.Error("TestDriveSession: no vehicle available.");
                return;
            }

            Vector3 pos = spawnPoint != null ? spawnPoint.position : Vector3.up;
            Quaternion rot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;
            _input.Enabled = true;
            _input.CalibrateTilt();
            _vehicle = VehicleFactory.Spawn(spec, definition, _input, pos, rot, _settings.Current.Transmission);
            _vehicle.Teleport(pos, rot); // settles the car onto the ground under the spawn point
            _vehicle.ResetRequested += ResetVehicle;
            _vehicle.name = "Vehicle_Player";

            if (cameraRig != null)
            {
                cameraRig.Follow(_vehicle, definition.SupportsCockpitCamera);
                cameraRig.SetMode(_settings.Current.Camera);
                cameraRig.SetShakeIntensity(_settings.Current.CameraShake);
            }
            _settings.Changed += OnSettingsChanged;
            LocalVehicleSpawned?.Invoke(_vehicle);
        }

        private void Update()
        {
            if (_vehicle != null && _vehicle.Telemetry.IsUpsideDown) ResetVehicle();
        }

        private void ResetVehicle()
        {
            if (_vehicle == null) return;
            Vector3 pos = spawnPoint != null ? spawnPoint.position : Vector3.up;
            Quaternion rot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;
            // Reset in place if the car is merely flipped; otherwise back to the spawn.
            var t = _vehicle.transform;
            if (_vehicle.Telemetry.IsUpsideDown)
            {
                pos = t.position + Vector3.up * 1.2f;
                rot = Quaternion.Euler(0f, t.eulerAngles.y, 0f);
            }
            _vehicle.Teleport(pos, rot);
            cameraRig?.SnapBehind();
        }

        private void OnSettingsChanged(Save.SettingsData settings)
        {
            if (_vehicle != null) _vehicle.TransmissionMode = settings.Transmission;
            if (cameraRig != null)
            {
                cameraRig.SetMode(settings.Camera);
                cameraRig.SetShakeIntensity(settings.CameraShake);
            }
        }

        public void ExitToMenu()
        {
            _input.Enabled = false;
            Services.Get<SceneFlowService>().LoadMainMenu();
        }

        private void OnDestroy()
        {
            if (_settings != null) _settings.Changed -= OnSettingsChanged;
            if (_vehicle != null) _vehicle.ResetRequested -= ResetVehicle;
        }

#if UNITY_EDITOR
        public void EditorWire(Transform spawn, VehicleCameraRig rig)
        {
            spawnPoint = spawn;
            cameraRig = rig;
        }
#endif
    }
}
