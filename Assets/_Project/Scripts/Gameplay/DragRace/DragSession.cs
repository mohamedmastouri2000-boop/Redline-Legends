using System;
using System.Collections.Generic;
using RedlineLegends.Cameras;
using RedlineLegends.Content;
using RedlineLegends.Core;
using RedlineLegends.Economy;
using RedlineLegends.Events;
using RedlineLegends.Input;
using RedlineLegends.Progression;
using RedlineLegends.Race;
using RedlineLegends.Save;
using RedlineLegends.Tracks;
using RedlineLegends.Utilities;
using RedlineLegends.Vehicles;
using UnityEngine;

namespace RedlineLegends.DragRace
{
    public enum DragState
    {
        Preparing,
        Staging,
        Lights,
        Racing,
        Finishing,
        Finished
    }

    /// <summary>
    /// Runs one drag race: staging (rev up on the brakes), the light tree, reaction timing, false
    /// start detection, shift-quality scoring, finish timing and trap speed, then a
    /// <see cref="RaceOutcome"/> for progression. Red light = disqualified (loses the race).
    /// </summary>
    public sealed class DragSession : MonoBehaviour, ILocalRacerSource
    {
        private const float StagingSeconds = 4f;
        private const float LightInterval = 0.5f;
        private const float FinishHoldSeconds = 3f;
        private const int AmberLights = 3;

        [SerializeField] private TrackLayout layout;
        [SerializeField] private VehicleCameraRig cameraRig;

        private readonly List<RacerState> _racers = new List<RacerState>(2);
        private readonly List<DragAIDriver> _drivers = new List<DragAIDriver>(2);
        private RaceLaunchRequest _request;
        private DragEventDefinition _event;
        private float _distance = 402.336f;
        private Vector3 _startPos;
        private Vector3 _startFwd;
        private Vector3 _startRight;
        private float _stateTimer;
        private float _lightsStartTime;
        private float _greenTime;
        private int _lightStage;
        private RacerState _player;
        private MobileInputProvider _localInput;
        private SceneFlowService _sceneFlow;
        private ProgressionService _progression;
        private SettingsService _settings;
        private ContentCatalog _catalog;
        private bool _paused;
        private readonly int[] _shiftCounts = new int[4];

        public DragState State { get; private set; } = DragState.Preparing;
        /// <summary>Seconds since green (negative during staging/lights).</summary>
        public float RaceTime { get; private set; }
        public float SessionTime { get; private set; }
        public IReadOnlyList<RacerState> Racers => _racers;
        public RacerState Player => _player;
        public RacerState Opponent => _racers.Count > 1 ? (_racers[0] == _player ? _racers[1] : _racers[0]) : null;
        public float DistanceMeters => _distance;
        /// <summary>0 = off, 1..3 amber, 4 green.</summary>
        public int LightStage => _lightStage;
        public bool IsPaused => _paused;
        public RaceOutcome Outcome { get; private set; }
        public RewardResult Reward { get; private set; }
        public DragEventDefinition Event => _event;
        public IReadOnlyList<int> ShiftCounts => _shiftCounts;
        public float PlayerTrapSpeedKmh { get; private set; }

        public VehicleController LocalVehicle => _player?.Vehicle;
        public event Action<VehicleController> LocalVehicleSpawned;
        public event Action<DragState> StateChanged;
        public event Action<int> LightChanged;
        public event Action<RacerState> FalseStarted;
        public event Action<ShiftQuality> PlayerShifted;
        public event Action<RacerState> RacerFinished;
        public event Action<RaceOutcome> RaceCompleted;
        public event Action<string> Message;

        private void Start()
        {
            if (!Services.IsReady)
            {
                GameLog.Error("DragSession: services not booted.");
                return;
            }
            _sceneFlow = Services.Get<SceneFlowService>();
            _catalog = Services.Get<ContentCatalog>();
            _progression = Services.Get<ProgressionService>();
            _settings = Services.Get<SettingsService>();
            _localInput = Services.Get<MobileInputProvider>();

            _request = _sceneFlow.ConsumePendingRace() ?? BuildFallbackRequest();
            if (_request == null || layout == null || layout.DragStart == null)
            {
                GameLog.Error("DragSession: no launch request or drag start.");
                return;
            }
            _catalog.TryGetEvent(_request.EventId, out var evt);
            _event = evt as DragEventDefinition;
            _distance = _event != null ? _event.DistanceMeters : 402.336f;

            var start = layout.DragStart;
            _startPos = start.position;
            _startFwd = start.forward;
            _startFwd.y = 0f;
            _startFwd.Normalize();
            _startRight = Vector3.Cross(Vector3.up, _startFwd);

            SpawnRacers();
            _settings.Changed += OnSettingsChanged;
            SetState(DragState.Staging);
            Message?.Invoke("Rev it up");
        }

        private RaceLaunchRequest BuildFallbackRequest()
        {
            var builder = new RaceLaunchBuilder(_catalog, Services.Get<GarageService>(), Services.Get<PlayerProfileService>());
            string sceneName = gameObject.scene.name;
            foreach (var evt in _catalog.Events)
                if (evt.Mode == RaceMode.Drag && evt.Track != null && evt.Track.SceneName == sceneName)
                {
                    var request = builder.Build(evt, out _);
                    if (request != null) return request;
                }
            return null;
        }

        private void SpawnRacers()
        {
            int lanes = Mathf.Max(1, _request.Participants.Count);
            for (int i = 0; i < _request.Participants.Count; i++)
            {
                var spec = _request.Participants[i];
                if (!_catalog.TryGetVehicle(spec.VehicleId, out var definition)) continue;
                bool isPlayer = spec.ControlSource == ControlSource.LocalPlayer;
                float lane = (spec.GridSlot - (lanes - 1) * 0.5f) * layout.DragLaneSpacing;
                Vector3 pos = _startPos + _startRight * lane - _startFwd * 2.5f;
                Quaternion rot = Quaternion.LookRotation(_startFwd, Vector3.up);

                IInputProvider input = isPlayer ? _localInput : new AIInputProvider();
                if (isPlayer) _localInput.Enabled = true; // revving during staging is part of the game
                var transmission = isPlayer ? _settings.Current.Transmission : TransmissionMode.Manual;
                var vehicle = VehicleFactory.Spawn(spec, definition, input, pos, rot, transmission, transform);
                vehicle.Teleport(pos, rot);
                vehicle.HoldBrakes = true;

                var state = new RacerState { Spec = spec, Vehicle = vehicle, Input = input };
                _racers.Add(state);

                if (isPlayer)
                {
                    _player = state;
                    vehicle.name = "Vehicle_Player";
                    vehicle.Shifted += OnPlayerShifted;
                    if (cameraRig != null)
                    {
                        cameraRig.Follow(vehicle, definition.SupportsCockpitCamera);
                        cameraRig.SetMode(_settings.Current.Camera);
                        cameraRig.SetShakeIntensity(_settings.Current.CameraShake);
                    }
                    _localInput.CalibrateTilt();
                }
                else
                {
                    _catalog.TryGetAIProfile(spec.AIProfileId, out var profile);
                    var driver = new DragAIDriver(profile, vehicle, (AIInputProvider)input, _request.Seed + spec.Id.Value * 131);
                    driver.SetLane(_startPos + _startRight * lane, _startFwd, _distance);
                    _drivers.Add(driver);
                    state.Driver = null;
                    _driverByRacer[state] = driver;
                }
            }
            if (_player != null) LocalVehicleSpawned?.Invoke(_player.Vehicle);
        }

        private readonly Dictionary<RacerState, DragAIDriver> _driverByRacer = new Dictionary<RacerState, DragAIDriver>();

        private void OnPlayerShifted(int from, int to, float rpm, ShiftQuality quality)
        {
            if (to <= from || State != DragState.Racing) return;
            _shiftCounts[(int)quality]++;
            PlayerShifted?.Invoke(quality);
            if (quality == ShiftQuality.Perfect)
            {
                if (Services.TryGet<AchievementService>(out var achievements)) achievements.RecordPerfectShift();
                if (Services.TryGet<HapticsService>(out var haptics)) haptics.Pulse(0.4f);
            }
        }

        private void OnSettingsChanged(SettingsData settings)
        {
            if (_player?.Vehicle != null) _player.Vehicle.TransmissionMode = settings.Transmission;
            if (cameraRig != null)
            {
                cameraRig.SetMode(settings.Camera);
                cameraRig.SetShakeIntensity(settings.CameraShake);
            }
        }

        private void OnDestroy()
        {
            if (_settings != null) _settings.Changed -= OnSettingsChanged;
            if (_player?.Vehicle != null) _player.Vehicle.Shifted -= OnPlayerShifted;
            if (_localInput != null) _localInput.Enabled = false;
            if (Time.timeScale == 0f) Time.timeScale = 1f;
        }

        // ------------------------------------------------------------------ loop

        private void Update()
        {
            if (_paused || State == DragState.Preparing || State == DragState.Finished) return;
            float dt = Time.deltaTime;
            SessionTime += dt;
            _stateTimer += dt;

            switch (State)
            {
                case DragState.Staging:
                    if (_stateTimer >= StagingSeconds) BeginLights();
                    break;
                case DragState.Lights:
                    UpdateLights();
                    DetectFalseStarts();
                    break;
                case DragState.Racing:
                    RaceTime = SessionTime - _greenTime;
                    UpdateRacing();
                    break;
                case DragState.Finishing:
                    RaceTime = SessionTime - _greenTime;
                    UpdateRacing();
                    if (_stateTimer >= FinishHoldSeconds) CompleteRace(false);
                    break;
            }
        }

        private void FixedUpdate()
        {
            if (_paused) return;
            float expectedGreen = _lightsStartTime + LightInterval * (AmberLights + 1);
            for (int i = 0; i < _drivers.Count; i++)
                _drivers[i].FixedTick(Time.fixedDeltaTime, SessionTime, _lightsStartTime, expectedGreen);
        }

        private void BeginLights()
        {
            _lightsStartTime = SessionTime;
            _lightStage = 0;
            SetState(DragState.Lights);
            // Brakes release with the first amber: jumping the start is now possible.
            for (int i = 0; i < _racers.Count; i++) _racers[i].Vehicle.HoldBrakes = false;
            for (int i = 0; i < _drivers.Count; i++) _drivers[i].NotifyLightsStarted();
        }

        private void UpdateLights()
        {
            int stage = Mathf.Min(AmberLights + 1, 1 + Mathf.FloorToInt((SessionTime - _lightsStartTime) / LightInterval));
            if (stage == _lightStage) return;
            _lightStage = stage;
            LightChanged?.Invoke(stage);
            if (stage == AmberLights + 1)
            {
                _greenTime = SessionTime;
                RaceTime = 0f;
                for (int i = 0; i < _drivers.Count; i++) _drivers[i].NotifyGreen(_greenTime);
                SetState(DragState.Racing);
                Message?.Invoke("GO");
            }
        }

        private void DetectFalseStarts()
        {
            for (int i = 0; i < _racers.Count; i++)
            {
                var r = _racers[i];
                if (r.FalseStart || r.Vehicle == null) continue;
                if (DistanceAlong(r) > 0.3f || r.Vehicle.Telemetry.SpeedMs > 0.5f)
                {
                    r.FalseStart = true;
                    r.ReactionTime = -(_lightsStartTime + LightInterval * (AmberLights + 1) - SessionTime);
                    FalseStarted?.Invoke(r);
                    if (r.IsLocalPlayer) Message?.Invoke("Red light!");
                }
            }
        }

        private float DistanceAlong(RacerState r) => Vector3.Dot(r.Vehicle.transform.position - _startPos, _startFwd);

        private void UpdateRacing()
        {
            for (int i = 0; i < _racers.Count; i++)
            {
                var r = _racers[i];
                if (r.Vehicle == null) continue;
                float along = DistanceAlong(r);
                r.DistanceAlongLap = along;
                r.TotalProgress = along;
                var tel = r.Vehicle.Telemetry;
                // Reaction time is green-to-launch: the car leaving the beam, not the throttle,
                // because drivers are already on the throttle while staging on the brakes.
                if (r.ReactionTime < 0f && !r.FalseStart && (along > 0.25f || tel.SpeedMs > 0.4f))
                    r.ReactionTime = Mathf.Max(0f, RaceTime);
                if (!r.Finished && along >= _distance)
                {
                    r.Finished = true;
                    r.FinishTime = RaceTime;
                    if (r.IsLocalPlayer)
                    {
                        PlayerTrapSpeedKmh = tel.SpeedKmh;
                        if (Services.TryGet<AchievementService>(out var achievements)) achievements.RecordTopSpeed(tel.SpeedKmh);
                    }
                    RacerFinished?.Invoke(r);
                    if (r.IsLocalPlayer)
                    {
                        Message?.Invoke("Finish " + MathUtil.FormatRaceTime(r.FinishTime) + "  " + Mathf.RoundToInt(tel.SpeedKmh) + " km/h");
                        BeginFinishing();
                    }
                }
                // Past the finish everyone rolls off the throttle.
                if (r.Finished && along > _distance + 60f) r.Vehicle.HoldBrakes = true;
            }
            if (State == DragState.Racing && RaceTime > 60f) BeginFinishing();
        }

        private void BeginFinishing()
        {
            if (State == DragState.Finishing || State == DragState.Finished) return;
            _localInput.Enabled = false;
            SetState(DragState.Finishing);
        }

        public void Pause()
        {
            if (_paused || State == DragState.Finished) return;
            _paused = true;
            Time.timeScale = 0f;
            _localInput.Enabled = false;
        }

        public void Resume()
        {
            if (!_paused) return;
            _paused = false;
            Time.timeScale = 1f;
            if (State != DragState.Finished && State != DragState.Finishing) _localInput.Enabled = true;
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            if (_request != null) _sceneFlow.LoadRace(_request);
        }

        public void QuitToMenu()
        {
            Time.timeScale = 1f;
            if (State != DragState.Finished) CompleteRace(true);
            _sceneFlow.LoadMainMenu();
        }

        public void ContinueToMenu()
        {
            Time.timeScale = 1f;
            _sceneFlow.LoadMainMenu();
        }

        private void CompleteRace(bool aborted)
        {
            if (State == DragState.Finished) return;
            // Classification: disqualified (red light) last, then by elapsed time, then by distance.
            var ordered = new List<RacerState>(_racers);
            ordered.Sort((a, b) =>
            {
                if (a.FalseStart != b.FalseStart) return a.FalseStart ? 1 : -1;
                if (a.Finished != b.Finished) return a.Finished ? -1 : 1;
                if (a.Finished && b.Finished) return a.FinishTime.CompareTo(b.FinishTime);
                return b.TotalProgress.CompareTo(a.TotalProgress);
            });
            var outcome = new RaceOutcome { EventId = _request.EventId, Mode = RaceMode.Drag, Aborted = aborted };
            for (int i = 0; i < ordered.Count; i++)
            {
                var r = ordered[i];
                r.Position = i + 1;
                outcome.Results.Add(new RacerResult
                {
                    Id = r.Id,
                    DisplayName = r.Spec.DisplayName,
                    VehicleId = r.Spec.VehicleId,
                    ControlSource = r.Spec.ControlSource,
                    Position = i + 1,
                    TotalTimeSeconds = r.Finished ? r.FinishTime : Mathf.Max(RaceTime, 0f),
                    BestLapSeconds = -1f,
                    Finished = r.Finished && !aborted,
                    ReactionTimeSeconds = r.ReactionTime,
                    FalseStart = r.FalseStart
                });
            }
            Outcome = outcome;
            if (!aborted && !string.IsNullOrEmpty(_request.EventId))
                Reward = _progression.RecordOutcome(outcome);
            _sceneFlow.LastRaceOutcome = outcome;
            _localInput.Enabled = false;
            SetState(DragState.Finished);
            RaceCompleted?.Invoke(outcome);
        }

        private void SetState(DragState state)
        {
            if (State == state) return;
            State = state;
            _stateTimer = 0f;
            StateChanged?.Invoke(state);
        }

#if UNITY_EDITOR
        public void EditorWire(TrackLayout trackLayout, VehicleCameraRig rig)
        {
            layout = trackLayout;
            cameraRig = rig;
        }
#endif
    }
}
