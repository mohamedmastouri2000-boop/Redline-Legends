using UnityEngine;

namespace RedlineLegends.Tracks
{
    public enum TrackTheme
    {
        ModernCity,
        NightCity,
        Desert,
        Mountains,
        Coast,
        Industrial,
        Highway,
        RaceCircuit,
        DragStrip
    }

    /// <summary>
    /// Describes a track scene. The scene itself contains the TrackLayout (checkpoints, grid,
    /// racing line); this asset is what menus and events reference so no code names scenes.
    /// </summary>
    [CreateAssetMenu(fileName = "trk_new", menuName = "Redline Legends/Track Definition")]
    public sealed class TrackDefinition : ScriptableObject
    {
        [SerializeField] private string id = "trk_new";
        [SerializeField] private string displayName = "New Track";
        [SerializeField] private string sceneName = "";
        [SerializeField] private TrackTheme theme = TrackTheme.RaceCircuit;
        [SerializeField] private float lengthMeters = 2000f;
        [SerializeField] private bool isLoop = true;
        [SerializeField] private bool supportsDrag;
        [SerializeField] private int maxParticipants = 8;
        [SerializeField] private Sprite thumbnail;
        [SerializeField] private Sprite minimap;

        public string Id => id;
        public string DisplayName => displayName;
        public string SceneName => sceneName;
        public TrackTheme Theme => theme;
        public float LengthMeters => lengthMeters;
        public bool IsLoop => isLoop;
        public bool SupportsDrag => supportsDrag;
        public int MaxParticipants => maxParticipants;
        public Sprite Thumbnail => thumbnail;
        public Sprite Minimap => minimap;

#if UNITY_EDITOR
        public void EditorInitialize(string newId, string newDisplayName, string newSceneName, TrackTheme newTheme,
            float newLength, bool loop, bool drag, int maxRacers)
        {
            id = newId;
            displayName = newDisplayName;
            sceneName = newSceneName;
            theme = newTheme;
            lengthMeters = newLength;
            isLoop = loop;
            supportsDrag = drag;
            maxParticipants = maxRacers;
        }
#endif
    }
}
