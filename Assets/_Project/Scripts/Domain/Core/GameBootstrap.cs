using System.Collections;
using RedlineLegends.Content;
using RedlineLegends.Input;
using RedlineLegends.Progression;
using RedlineLegends.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RedlineLegends.Core
{
    /// <summary>
    /// Composition root. Lives on the persistent AppRoot prefab (Resources/AppRoot) and is created
    /// before the first scene loads, so opening any scene in the editor still boots the game.
    /// Builds every service in dependency order and registers them; nothing else news up services.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameBootstrap : MonoBehaviour, ICoroutineRunner
    {
        public const string AppRootResourcePath = "AppRoot";

        private static GameBootstrap _instance;
        private float _sessionStart;
        private PlayerProfileService _profile;
        private SaveService _save;

        public static bool IsBooted => _instance != null && Services.IsReady;
        public GameConfig Config { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateBeforeFirstScene()
        {
            if (_instance != null) return;
            var prefab = Resources.Load<GameObject>(AppRootResourcePath);
            if (prefab == null)
            {
                GameLog.Error("Resources/" + AppRootResourcePath + " prefab missing. Run Redline Legends > Setup > Generate Project.");
                return;
            }
            var root = Instantiate(prefab);
            root.name = "AppRoot";
            DontDestroyOnLoad(root);
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            Boot();
        }

        private void Boot()
        {
            Config = GameConfig.Load();
            if (Config == null) return;

            Time.fixedDeltaTime = Config.FixedTimestep;
            Physics.defaultSolverIterations = 8;
            Physics.defaultSolverVelocityIterations = 2;

            var container = new ServiceContainer();
            var stateMachine = new GameStateMachine();
            container.Register(stateMachine);
            container.Register(Config);

            var catalog = new ContentCatalog(Config.ContentDatabase);
            container.Register(catalog);

            var migrations = new SaveMigrationPipeline();
            _save = new SaveService(new FileSaveStore(), migrations, Config.SaveFileName,
                Config.DefaultSettings, Config.ProgressionConfig.StartingCredits);
            _save.Load();
            container.Register(_save);

            var settings = new SettingsService(_save);
            settings.ApplyEngineSettings(_save.Data.Settings);
            container.Register(settings);

            _profile = new PlayerProfileService(_save, Config.ProgressionConfig);
            container.Register(_profile);
            var progression = new ProgressionService(_save, catalog, _profile);
            container.Register(progression);
            var garage = new GarageService(_save, catalog, _profile, Config.ProgressionConfig);
            container.Register(garage);
            var achievements = new AchievementService(_save, catalog, _profile, progression, garage);
            progression.OutcomeRecorded += achievements.RecordRaceResult;
            container.Register(achievements);
            container.Register(new HapticsService(settings));

            var localInput = new MobileInputProvider(Config.InputActions, _save.Data.Settings);
            settings.Changed += localInput.ApplySettings;
            container.Register(localInput);

            var overlay = GetComponentInChildren<ILoadingOverlay>(true);
            var sceneFlow = new SceneFlowService(this, stateMachine, overlay);
            container.Register(sceneFlow);

            Services.Install(container);
            _sessionStart = Time.realtimeSinceStartup;
            GameLog.Info("Redline Legends booted. Profile " + _profile.ProfileId + ", level " + _profile.Level + ".");

            // Booting from the Bootstrap scene means a real launch: go to the menu. Booting from a
            // menu/garage/track scene means a developer pressed Play there; leave them in it.
            // Any other scene (e.g. the test runner's) just waits for a scene we know.
            SceneManager.sceneLoaded += OnSceneLoaded;
            HandleSceneEntered(SceneManager.GetActiveScene().name, sceneFlow, stateMachine);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single || !Services.IsReady) return;
            var sceneFlow = Services.Get<SceneFlowService>();
            if (sceneFlow.IsTransitioning) return; // SceneFlow announces its own transitions.
            HandleSceneEntered(scene.name, sceneFlow, Services.Get<GameStateMachine>());
        }

        private void HandleSceneEntered(string sceneName, SceneFlowService sceneFlow, GameStateMachine stateMachine)
        {
            if (sceneName == SceneNames.Bootstrap)
            {
                stateMachine.TransitionTo(GameStateId.Boot);
                StartCoroutine(GoToMenuNextFrame(sceneFlow));
                return;
            }
            var state = GuessStateForScene(sceneName);
            if (state.HasValue) stateMachine.TransitionTo(state.Value);
        }

        private static GameStateId? GuessStateForScene(string sceneName)
        {
            if (sceneName == SceneNames.MainMenu) return GameStateId.MainMenu;
            if (sceneName == SceneNames.Garage) return GameStateId.Garage;
            if (sceneName.StartsWith("Track_")) return GameStateId.Race;
            return null;
        }

        private IEnumerator GoToMenuNextFrame(SceneFlowService sceneFlow)
        {
            yield return null;
            sceneFlow.LoadMainMenu();
        }

        // ---- ICoroutineRunner ----
        public Coroutine Run(IEnumerator routine) => StartCoroutine(routine);
        public void Stop(Coroutine coroutine) { if (coroutine != null) StopCoroutine(coroutine); }

        // ---- Lifecycle persistence ----
        private void OnApplicationPause(bool paused)
        {
            if (paused) FlushSession();
        }

        private void OnApplicationQuit() => FlushSession();

        private void FlushSession()
        {
            if (_save == null || !_save.IsLoaded) return;
            _profile?.AddPlayTime(Time.realtimeSinceStartup - _sessionStart);
            _sessionStart = Time.realtimeSinceStartup;
            _save.Save();
        }

        private void OnDestroy()
        {
            if (_instance != this) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            var input = Services.IsReady && Services.TryGet<MobileInputProvider>(out var provider) ? provider : null;
            input?.Dispose();
            Services.Uninstall();
            _instance = null;
        }
    }
}
