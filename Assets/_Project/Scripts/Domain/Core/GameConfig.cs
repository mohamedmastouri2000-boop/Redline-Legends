using RedlineLegends.Content;
using RedlineLegends.Progression;
using RedlineLegends.Save;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RedlineLegends.Core
{
    /// <summary>
    /// Root configuration asset, the only thing loaded from Resources. Everything else is reached
    /// through references from here so the boot path has exactly one string lookup.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Redline Legends/Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        public const string ResourcePath = "GameConfig";

        [SerializeField] private ContentDatabase contentDatabase;
        [SerializeField] private ProgressionConfig progressionConfig;
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private VfxLibrary vfx;
        [SerializeField] private SettingsData defaultSettings = new SettingsData();
        [SerializeField] private string saveFileName = "profile.sav";
        [Tooltip("Track used by the garage Test Drive button.")]
        [SerializeField] private string testDriveTrackId = "trk_proving_ground";
        [Tooltip("Physics step. 50 Hz is a good mobile compromise; the vehicle sim is stable here.")]
        [SerializeField] private float fixedTimestep = 0.02f;

        public ContentDatabase ContentDatabase => contentDatabase;
        public ProgressionConfig ProgressionConfig => progressionConfig;
        public InputActionAsset InputActions => inputActions;
        public VfxLibrary Vfx => vfx;
        public SettingsData DefaultSettings => defaultSettings;
        public string SaveFileName => saveFileName;
        public string TestDriveTrackId => testDriveTrackId;
        public float FixedTimestep => fixedTimestep;

        public static GameConfig Load()
        {
            var config = Resources.Load<GameConfig>(ResourcePath);
            if (config == null)
                GameLog.Error("GameConfig not found at Resources/" + ResourcePath + ". Run Redline Legends > Setup > Generate Project.");
            return config;
        }

#if UNITY_EDITOR
        public void EditorInitialize(ContentDatabase db, ProgressionConfig progression, InputActionAsset actions, VfxLibrary vfxLibrary)
        {
            contentDatabase = db;
            progressionConfig = progression;
            inputActions = actions;
            vfx = vfxLibrary;
        }
#endif
    }
}
