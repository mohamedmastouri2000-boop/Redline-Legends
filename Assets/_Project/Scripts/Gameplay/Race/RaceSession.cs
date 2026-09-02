using System;
using System.Collections.Generic;
using RedlineLegends.AI;
using RedlineLegends.Cameras;
using RedlineLegends.Content;
using RedlineLegends.Core;
using RedlineLegends.Economy;
using RedlineLegends.Events;
using RedlineLegends.Input;
using RedlineLegends.Progression;
using RedlineLegends.Tracks;
using RedlineLegends.Vehicles;
using UnityEngine;

namespace RedlineLegends.Race
{
    public enum RaceState
    {
        Preparing,
        Countdown,
        Racing,
        Finishing,
        Finished
    }

    /// <summary>
    /// Runs one circuit race: spawns participants from the launch request, counts down, tracks
    /// laps/checkpoints/positions, applies the event type's rules (circuit, sprint, time attack,
    /// elimination, checkpoint), produces a <see cref="RaceOutcome"/> and hands it to progression.
    /// Every racer is handled the same way regardless of who controls it.
    /// </summary>
    public sealed class RaceSession : MonoBehaviour, ILocalRacerSource
    {
        private const float CountdownSeconds = 3.6f;
        private const float RankingInterval = 0.1f;
        private const float FinishHoldSeconds = 3f;

        [SerializeField] private TrackLayout layout;
        [SerializeField] private VehicleCameraRig cameraRig;

        private readonly List<RacerState> _racers = new List<RacerState>(12);
        private readonly List<RacerState> _ranking = new List<RacerState>(12);
        private RaceLaunchRequest _request;
        private RaceEventDefinition _event;
        private CircuitEventType _eventType = CircuitEventType.Circuit;
        private int _laps = 1;
        private float _rankingTimer;
        private float _countdown;
        private float _finishHold;
        private float _eliminationTimer;
        private float _checkpointClock;
        private RacerState _player;
        private float[] _gateAlong = Array.Empty<float>();
        private MobileInputProvider _localInput;
        private SceneFlowService _sceneFlow;
        private ProgressionService _progression;
        private SettingsService _settings;
        private ContentCatalog _catalog;
        private bool _paused;

        public RaceState State { get; private set; } = RaceState.Preparing;
        public float RaceTime { get; private set; }
        public IReadOnlyList<RacerState> Racers => _racers;
        public IReadOnlyList<RacerState> Ranking => _ranking;
        public RacerState Player => _player;
        public RaceLaunchRequest Request => _request;
        public RaceEventDefinition Event => _event;
        public CircuitEventType EventType => _eventType;
        public int Laps => _laps;
        public bool IsPractice => _request != null && _request.IsPractice;
        public float CountdownRemaining => _countdown;
        public float EliminationRemaining => _eliminationTimer;
        public float CheckpointClock => _checkpointClock;
        public RaceOutcome Outcome { get; private set; }
        public RewardResult Reward { get; private set; }
        public bool IsPaused => _paused;
        public TrackLayout Layout => layout;

        public VehicleController LocalVehicle => _player?.Vehicle;
        public event Action<VehicleController> LocalVehicleSpawned;
        public event Action<RaceState> StateChanged;
        public event Action<int> CountdownTick;
        public event Action<RacerState, int, float> LapCompleted;
        public event Action<RacerState> RacerFinished;
        public event Action<RacerState> RacerEliminated;
        public event Action<RaceOutcome> RaceCompleted;
        public event Action<string> Message;

        // ------------------------------------------------------------------ setup

        private void Start()
        {
            if (!Services.IsReady)
            {
                GameLog.Error("RaceSession: services not booted.");
                return;
            }
            _sceneFlow = Services.Get<SceneFlowService>();
            _catalog = Services.Get<ContentCatalog>();
            _progression = Services.Get<ProgressionService>();
            _settings = Services.Get<SettingsService>();
            _localInput = Services.Get<MobileInputProvider>();

            _request = _sceneFlow.ConsumePendingRace() ?? BuildFallbackRequest();
            if (_request == null || layout == null)
            {
                GameLog.Error("RaceSession: no launch request or track layout.");
                return;
            }
            _catalog.TryGetEvent(_request.EventId, out _event);
            if (_event is CircuitEventDefinition circuit)
            {
                _eventType = circuit.EventType;
                _laps = Mathf.Max(1, circuit.Laps);
                _eliminationTimer = circuit.EliminationIntervalSeconds;
                _checkpointClock = circuit.CheckpointStartSeconds;
            }
            else
            {
                _eventType = CircuitEventType.Circuit;
                _laps = IsPractice ? 999 : 1;
            }

            _gateAlong = new float[layout.CheckpointCount];
            for (int i = 0; i < layout.CheckpointCount; i++)
            {
                var gate = layout.Checkpoints[i];
                int idx = layout.RacingLine.FindNearest(gate.transform.position, -1);
                _gateAlong[i] = layout.RacingLine.DistanceAtNode(idx);
            }

            SpawnRacers();
            for (int i = 0; i < layout.Checkpoints.Length; i++)
                layout.Checkpoints[i].Passed += OnCheckpointPassed;

            _settings.Changed += OnSettingsChanged;
            SetState(IsPractice ? RaceState.Racing : RaceState.Countdown);
            _countdown = CountdownSeconds;
            if (IsPractice) ReleaseRacers();
        }

        /// <summary>Developer pressed Play in a track scene: race the first event on this track, or free drive.</summary>
        private RaceLaunchRequest BuildFallbackRequest()
        {
            var garage = Services.Get<GarageService>();
            var profile = Services.Get<PlayerProfileService>();
            var builder = new RaceLaunchBuilder(_catalog, garage, profile);
            string sceneName = gameObject.scene.name;
            foreach (var evt in _catalog.Events)
            {
                if (evt.Mode == RaceMode.Circuit && evt.Track != null && evt.Track.SceneName == sceneName)
                {
                    var request = builder.Build(evt, out _);
                    if (request != null) return request;
                }
            }
            foreach (var track in _catalog.Tracks)
                if (track.SceneName == sceneName) return builder.BuildPractice(track);
            return null;
        }

        private void SpawnRacers()
        {
            var transmission = _settings.Current.Transmission;
            for (int i = 0; i < _request.Participants.Count; i++)
            {
                var spec = _request.Participants[i];
                if (!_catalog.TryGetVehicle(spec.VehicleId, out var definition))
                {
                    GameLog.Error("RaceSession: unknown vehicle " + spec.VehicleId);
                    continue;
                }
                var slot = layout.GetGridSlot(spec.GridSlot);
                IInputProvider input;
                AIDriver driver = null;
                bool isPlayer = spec.ControlSource == ControlSource.LocalPlayer;
                if (isPlayer)
                {
                    input = _localInput;
                    _localInput.Enabled = false;
                }
                else
                {
                    input = new AIInputProvider();
                }

                var vehicle = VehicleFactory.Spawn(spec, definition, input, slot.position, slot.rotation,
                    isPlayer ? transmission : Save.TransmissionMode.Automatic, transform);
                vehicle.Teleport(slot.position, slot.rotation);
                vehicle.HoldBrakes = true;

                if (!isPlayer)
                {
                    _catalog.TryGetAIProfile(spec.AIProfileId, out var aiProfile);
                    driver = new AIDriver(aiProfile, layout.RacingLine, vehicle, (AIInputProvider)input, _request.Seed + spec.Id.Value * 7919);
                }

                var state = new RacerState { Spec = spec, Vehicle = vehicle, Input = input, Driver = driver };
                state.LineHint = layout.RacingLine.FindNearest(vehicle.transform.position, -1);
                state.DistanceAlongLap = layout.RacingLine.DistanceAlong(vehicle.transform.position, state.LineHint);
                // Grid sits just before the start line: treat that as "end of lap 0" so the first
                // crossing of checkpoint 0 does not count as a completed lap.
                state.NextCheckpoint = 0;
                state.Lap = 1;
                _racers.Add(state);

                if (isPlayer)
                {
                    _player = state;
                    vehicle.name = "Vehicle_Player";
                    vehicle.ResetRequested += () => ResetRacer(state);
                    if (cameraRig != null)
                    {
                        cameraRig.Follow(vehicle, definition.SupportsCockpitCamera);
                        cameraRig.SetMode(_settings.Current.Camera);
                        cameraRig.SetShakeIntensity(_settings.Current.CameraShake);
                    }
                    _localInput.CalibrateTilt();
                }
            }
            _ranking.AddRange(_racers);
            UpdateRanking();
            if (_player != null) LocalVehicleSpawned?.Invoke(_player.Vehicle);
        }

        private void OnSettingsChanged(Save.SettingsData settings)
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
            if (layout != null)
                for (int i = 0; i < layout.Checkpoints.Length; i++)
                    layout.Checkpoints[i].Passed -= OnCheckpointPassed;
            if (_localInput != null) _localInput.Enabled = false;
            if (Time.timeScale == 0f) Time.timeScale = 1f;
        }

        // ------------------------------------------------------------------ loop

        private void Update()
        {
            if (_paused) return;
            float dt = Time.deltaTime;
            switch (State)
            {
                case RaceState.Countdown:
                    UpdateCountdown(dt);
                    break;
                case RaceState.Racing:
                    RaceTime += dt;
                    UpdateRacing(dt);
                    break;
                case RaceState.Finishing:
                    RaceTime += dt;
                    UpdateRacing(dt);
                    _finishHold -= dt;
                    if (_finishHold <= 0f) CompleteRace(false);
                    break;
            }
        }

        private void FixedUpdate()
        {
            if (_paused) return;
            float dt = Time.fixedDeltaTime;
            for (int i = 0; i < _racers.Count; i++)
            {
                var r = _racers[i];
                r.Driver?.FixedTick(dt, RaceTime);
            }
        }

        private void UpdateCountdown(float dt)
        {
            int before = Mathf.CeilToInt(_countdown);
            _countdown -= dt;
            int after = Mathf.CeilToInt(_countdown);
            if (after != before && after >= 0) CountdownTick?.Invoke(after);
            if (_countdown <= 0f)
            {
                ReleaseRacers();
                SetState(RaceState.Racing);
                CountdownTick?.Invoke(0);
            }
        }

        private void ReleaseRacers()
        {
            for (int i = 0; i < _racers.Count; i++)
                _racers[i].Vehicle.HoldBrakes = false;
            if (_localInput != null) _localInput.Enabled = true;
        }

        private void UpdateRacing(float dt)
        {
            _rankingTimer += dt;
            if (_rankingTimer >= RankingInterval)
            {
                _rankingTimer = 0f;
                UpdateProgress();
                UpdateRanking();
            }
            UpdateWrongWayAndStuck(dt);
            if (State != RaceState.Racing) return;

            if (_eventType == CircuitEventType.Elimination) UpdateElimination(dt);
            if (_eventType == CircuitEventType.Checkpoint) UpdateCheckpointClock(dt);
        }

        private void UpdateProgress()
        {
            var line = layout.RacingLine;
            float length = line.TotalLength;
            for (int i = 0; i < _racers.Count; i++)
            {
                var r = _racers[i];
                if (r.Vehicle == null) continue;
                Vector3 pos = r.Vehicle.transform.position;
                r.LineHint = line.FindNearest(pos, r.LineHint);
                float along = line.DistanceAlong(pos, r.LineHint);
                // The grid sits just behind the start line, so until the first crossing a racer near
                // the end of the line is really at negative distance on lap 1.
                if (!r.StartLineCrossed && along > length * 0.5f) along -= length;
                r.DistanceAlongLap = along;
                float progress = (r.Lap - 1) * length + along;
                // Checkpoint order is authoritative: a shortcut cannot rank ahead of the next gate.
                float gateLimit = NextGateProgress(r) + 5f;
                if (progress > gateLimit) progress = gateLimit;
                r.TotalProgress = progress;
            }
        }

        /// <summary>Race-space distance of the racer's next gate.</summary>
        private float NextGateProgress(RacerState r)
        {
            float length = layout.RacingLine.TotalLength;
            if (r.NextCheckpoint == 0)
                return r.StartLineCrossed ? r.Lap * length : 0f;
            return (r.Lap - 1) * length + _gateAlong[r.NextCheckpoint];
        }

        private void UpdateRanking()
        {
            _ranking.Sort(CompareRacers);
            for (int i = 0; i < _ranking.Count; i++) _ranking[i].Position = i + 1;
        }

        private static int CompareRacers(RacerState a, RacerState b)
        {
            if (a.Finished != b.Finished) return a.Finished ? -1 : 1;
            if (a.Finished && b.Finished) return a.FinishTime.CompareTo(b.FinishTime);
            if (a.Eliminated != b.Eliminated) return a.Eliminated ? 1 : -1;
            return b.TotalProgress.CompareTo(a.TotalProgress);
        }

        private void UpdateWrongWayAndStuck(float dt)
        {
            var line = layout.RacingLine;
            for (int i = 0; i < _racers.Count; i++)
            {
                var r = _racers[i];
                if (r.Vehicle == null || !r.IsActive) continue;
                var tel = r.Vehicle.Telemetry;
                Vector3 vel = r.Vehicle.Body.linearVelocity;
                vel.y = 0f;
                bool wrong = vel.magnitude > 3f && Vector3.Dot(vel.normalized, line.DirectionAt(r.LineHint)) < -0.4f;
                r.WrongWayTime = wrong ? r.WrongWayTime + dt : 0f;
                r.WrongWay = r.IsLocalPlayer && r.WrongWayTime > 1.2f;

                if (tel.IsUpsideDown) ResetRacer(r);
                // AI that stays stuck for a long time gets a respawn; players choose via the reset button.
                if (!r.IsLocalPlayer)
                {
                    r.StoppedTime = tel.SpeedKmh < 2f ? r.StoppedTime + dt : 0f;
                    if (r.StoppedTime > 8f) ResetRacer(r);
                }
            }
        }

        private void UpdateElimination(float dt)
        {
            _eliminationTimer -= dt;
            if (_eliminationTimer > 0f) return;
            var circuit = _event as CircuitEventDefinition;
            _eliminationTimer = circuit != null ? circuit.EliminationIntervalSeconds : 20f;

            RacerState last = null;
            int active = 0;
            for (int i = _ranking.Count - 1; i >= 0; i--)
            {
                if (!_ranking[i].IsActive) continue;
                active++;
                if (last == null) last = _ranking[i];
            }
            if (last == null || active <= 1) return;
            Eliminate(last);

            // One racer left: they win.
            active--;
            if (active <= 1)
            {
                for (int i = 0; i < _ranking.Count; i++)
                    if (_ranking[i].IsActive) FinishRacer(_ranking[i]);
            }
        }

        private void Eliminate(RacerState r)
        {
            r.Eliminated = true;
            r.FinishTime = RaceTime;
            if (r.Driver != null) r.Driver.Active = false;
            if (r.IsLocalPlayer)
            {
                _localInput.Enabled = false;
                r.Vehicle.HoldBrakes = true;
            }
            Message?.Invoke((r.IsLocalPlayer ? "You were" : r.Spec.DisplayName + " was") + " eliminated");
            RacerEliminated?.Invoke(r);
            UpdateRanking();
            if (r.IsLocalPlayer) BeginFinishing();
        }

        private void UpdateCheckpointClock(float dt)
        {
            _checkpointClock -= dt;
            if (_checkpointClock > 0f || _player == null || !_player.IsActive) return;
            _checkpointClock = 0f;
            Message?.Invoke("Time's up");
            _player.Eliminated = true;
            _localInput.Enabled = false;
            _player.Vehicle.HoldBrakes = true;
            UpdateRanking();
            BeginFinishing();
        }

        // ------------------------------------------------------------------ checkpoints & laps

        private void OnCheckpointPassed(Checkpoint checkpoint, VehicleController vehicle)
        {
            if (State != RaceState.Racing && State != RaceState.Finishing) return;
            var r = FindRacer(vehicle);
            if (r == null || !r.IsActive) return;
            if (checkpoint.Index != r.NextCheckpoint) return; // wrong gate: ignore (shortcut/backwards)

            r.LastCheckpoint = checkpoint;
            r.NextCheckpoint = (checkpoint.Index + 1) % layout.CheckpointCount;

            if (_eventType == CircuitEventType.Checkpoint && r.IsLocalPlayer)
            {
                var circuit = _event as CircuitEventDefinition;
                _checkpointClock += circuit != null ? circuit.CheckpointBonusSeconds : 8f;
            }

            bool finishLine = checkpoint.Index == 0 || (!layout.IsLoop && checkpoint.Index == layout.CheckpointCount - 1);
            if (!finishLine) return;

            if (checkpoint.Index == 0 && !r.StartLineCrossed)
            {
                // Leaving the grid: the lap clock starts at the line.
                r.StartLineCrossed = true;
                r.CurrentLapStart = RaceTime;
                return;
            }
            float lapTime = RaceTime - r.CurrentLapStart;
            r.LapTimes.Add(lapTime);
            if (r.BestLap < 0f || lapTime < r.BestLap) r.BestLap = lapTime;
            r.CurrentLapStart = RaceTime;
            LapCompleted?.Invoke(r, r.Lap, lapTime);
            r.Lap++;
            if (r.Lap > _laps || !layout.IsLoop) FinishRacer(r);
            else if (r.IsLocalPlayer && r.Lap == _laps && _laps > 1) Message?.Invoke("Final lap");
        }

        private RacerState FindRacer(VehicleController vehicle)
        {
            for (int i = 0; i < _racers.Count; i++)
                if (_racers[i].Vehicle == vehicle) return _racers[i];
            return null;
        }

        private void FinishRacer(RacerState r)
        {
            if (r.Finished) return;
            r.Finished = true;
            r.FinishTime = RaceTime;
            if (r.Driver != null) r.Driver.Cruise = true;
            UpdateRanking();
            RacerFinished?.Invoke(r);
            if (r.IsLocalPlayer)
            {
                _localInput.Enabled = false;
                Message?.Invoke("Finish! P" + r.Position);
                BeginFinishing();
            }
        }

        private void BeginFinishing()
        {
            if (State == RaceState.Finishing || State == RaceState.Finished) return;
            _finishHold = FinishHoldSeconds;
            SetState(RaceState.Finishing);
        }

        // ------------------------------------------------------------------ reset / pause

        public void ResetRacer(RacerState r)
        {
            if (r?.Vehicle == null) return;
            Vector3 pos;
            Quaternion rot;
            if (r.LastCheckpoint != null)
            {
                pos = r.LastCheckpoint.RespawnPosition;
                rot = r.LastCheckpoint.RespawnRotation;
            }
            else
            {
                var slot = layout.GetGridSlot(r.Spec.GridSlot);
                pos = slot.position;
                rot = slot.rotation;
            }
            // Never drop a car onto another one: step back along the track until the spot is clear.
            for (int attempt = 0; attempt < 6 && IsOccupied(pos, r); attempt++)
                pos -= rot * Vector3.forward * 7f;
            r.Vehicle.Teleport(pos, rot);
            r.Vehicle.HoldBrakes = false;
            r.StoppedTime = 0f;
            r.WrongWayTime = 0f;
            if (r.IsLocalPlayer) cameraRig?.SnapBehind();
        }

        private bool IsOccupied(Vector3 pos, RacerState self)
        {
            for (int i = 0; i < _racers.Count; i++)
            {
                var other = _racers[i];
                if (other == self || other.Vehicle == null) continue;
                Vector3 d = other.Vehicle.transform.position - pos;
                d.y = 0f;
                if (d.sqrMagnitude < 36f) return true;
            }
            return false;
        }

        public void Pause()
        {
            if (_paused || State == RaceState.Finished) return;
            _paused = true;
            Time.timeScale = 0f;
            if (_localInput != null) _localInput.Enabled = false;
        }

        public void Resume()
        {
            if (!_paused) return;
            _paused = false;
            Time.timeScale = 1f;
            if (_localInput != null && State == RaceState.Racing && _player != null && _player.IsActive) _localInput.Enabled = true;
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            if (_request == null) return;
            _sceneFlow.LoadRace(_request);
        }

        /// <summary>Leave mid-race: counts as an aborted attempt (no rewards, attempt recorded).</summary>
        public void QuitToMenu()
        {
            Time.timeScale = 1f;
            if (State != RaceState.Finished) CompleteRace(true);
            _sceneFlow.LoadMainMenu();
        }

        public void ContinueToMenu()
        {
            Time.timeScale = 1f;
            _sceneFlow.LoadMainMenu();
        }

        // ------------------------------------------------------------------ outcome

        private void CompleteRace(bool aborted)
        {
            if (State == RaceState.Finished) return;
            UpdateProgress();
            UpdateRanking();
            var outcome = new RaceOutcome
            {
                EventId = _request.EventId,
                Mode = RaceMode.Circuit,
                Aborted = aborted
            };
            for (int i = 0; i < _ranking.Count; i++)
            {
                var r = _ranking[i];
                outcome.Results.Add(new RacerResult
                {
                    Id = r.Id,
                    DisplayName = r.Spec.DisplayName,
                    VehicleId = r.Spec.VehicleId,
                    ControlSource = r.Spec.ControlSource,
                    Position = i + 1,
                    TotalTimeSeconds = r.Finished ? r.FinishTime : RaceTime,
                    BestLapSeconds = r.BestLap,
                    Finished = r.Finished && !aborted
                });
            }
            Outcome = outcome;
            if (!IsPractice && !string.IsNullOrEmpty(_request.EventId))
                Reward = _progression.RecordOutcome(outcome);
            _sceneFlow.LastRaceOutcome = IsPractice ? null : outcome;
            if (_localInput != null) _localInput.Enabled = false;
            SetState(RaceState.Finished);
            RaceCompleted?.Invoke(outcome);
        }

        private void SetState(RaceState state)
        {
            if (State == state) return;
            State = state;
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
