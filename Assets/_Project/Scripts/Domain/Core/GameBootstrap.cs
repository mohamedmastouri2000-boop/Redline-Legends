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

            var localInput = new MobileInputProvider(Config.InputActions, _save.Data.Settings);
            settings.Changed += localInput.ApplySettings;
            container.Register(localInput);

            var overlay = GetComponentInChildren<ILoadingOverlay>(true);
            var sceneFlow = new SceneFlowService(this, stateMachine, overlay);
            container.Register(sceneFlow);

            Services.Install(container);
            _sessionStart = Time.realtimeSinceStartup;
            GameLog.Info("Redline Legends booted. Profile " + _profile.ProfileId + ", level " + _profile.Level + ".");

            // Booting from the Bootstrap scene means a real launch: go to the menu. Booting from any
            // other scene means a developer pressed Play there; leave them in it.
            var active = SceneManager.GetActiveScene();
            if (active.name == SceneNames.Bootstrap || string.IsNullOrEmpty(active.name))
                StartCoroutine(GoToMenuNextFrame(sceneFlow));
            else
                stateMachine.TransitionTo(GuessStateForScene(active.name));
        }

        private static GameStateId GuessStateForScene(string sceneName)
        {
            if (sceneName == SceneNames.MainMenu) return GameStateId.MainMenu;
            if (sceneName == SceneNames.Garage) return GameStateId.Garage;
            return GameStateId.Race;
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
            var input = Services.IsReady && Services.TryGet<MobileInputProvider>(out var provider) ? provider : null;
            input?.Dispose();
            Services.Uninstall();
            _instance = null;
        }
    }
}
