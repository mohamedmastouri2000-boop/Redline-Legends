using System;
using System.Collections;
using RedlineLegends.Race;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RedlineLegends.Core
{
    /// <summary>
    /// Owns every scene transition. Race scenes are loaded by the scene name stored in the track
    /// definition, and the pending <see cref="RaceLaunchRequest"/> is what the race scene reads
    /// on load, so the menu never talks to race code directly.
    /// </summary>
    public sealed class SceneFlowService
    {
        private readonly ICoroutineRunner _runner;
        private readonly GameStateMachine _stateMachine;
        private readonly ILoadingOverlay _overlay;
        private Coroutine _active;

        public bool IsTransitioning => _active != null;

        /// <summary>Set before a race scene loads; the race scene's session builder consumes it.</summary>
        public RaceLaunchRequest PendingRace { get; private set; }

        /// <summary>Result of the last finished race, read by the results/menu layer.</summary>
        public RaceOutcome LastRaceOutcome { get; set; }

        public event Action<string> SceneLoaded;

        public SceneFlowService(ICoroutineRunner runner, GameStateMachine stateMachine, ILoadingOverlay overlay)
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
            _overlay = overlay;
        }

        public void LoadMainMenu() => Load(SceneNames.MainMenu, GameStateId.MainMenu, "Main Menu");

        public void LoadGarage() => Load(SceneNames.Garage, GameStateId.Garage, "Garage");

        public void LoadRace(RaceLaunchRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrEmpty(request.TrackSceneName))
                throw new ArgumentException("RaceLaunchRequest has no track scene.", nameof(request));
            PendingRace = request;
            Load(request.TrackSceneName, GameStateId.Race, request.EventDisplayName);
        }

        /// <summary>Called by the race scene once it has consumed the request.</summary>
        public RaceLaunchRequest ConsumePendingRace()
        {
            var request = PendingRace;
            PendingRace = null;
            return request;
        }

        private void Load(string sceneName, GameStateId targetState, string caption)
        {
            if (_active != null)
            {
                GameLog.Warn($"SceneFlow: ignoring load of '{sceneName}' while another transition is running.");
                return;
            }
            _active = _runner.Run(LoadRoutine(sceneName, targetState, caption));
        }

        private IEnumerator LoadRoutine(string sceneName, GameStateId targetState, string caption)
        {
            _stateMachine.TransitionTo(GameStateId.Loading);
            _overlay?.Show(caption);
            _overlay?.SetProgress(0f);

            // One frame so the overlay is drawn before the load hitch.
            yield return null;

            AsyncOperation op = null;
            if (Application.CanStreamedLevelBeLoaded(sceneName))
                op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

            if (op == null)
            {
                GameLog.Error($"SceneFlow: scene '{sceneName}' is not in the build. Returning to main menu.");
                _overlay?.Hide();
                _active = null;
                if (sceneName != SceneNames.MainMenu) LoadMainMenu();
                yield break;
            }

            op.allowSceneActivation = false;
            while (op.progress < 0.9f)
            {
                _overlay?.SetProgress(op.progress / 0.9f);
                yield return null;
            }
            _overlay?.SetProgress(1f);
            op.allowSceneActivation = true;
            while (!op.isDone) yield return null;

            // Let the new scene's Awake/Start run before announcing the state; race scenes build
            // their session in Start and expect the overlay to still cover them.
            yield return null;

            _stateMachine.TransitionTo(targetState);
            _overlay?.Hide();
            _active = null;
            SceneLoaded?.Invoke(sceneName);
        }
    }
}
