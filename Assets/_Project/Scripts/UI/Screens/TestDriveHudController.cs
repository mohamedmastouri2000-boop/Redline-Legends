using RedlineLegends.Race;
using UnityEngine;
using UnityEngine.UI;

namespace RedlineLegends.UI
{
    /// <summary>Small glue for the proving ground: pause/back button returns to the menu.</summary>
    public sealed class TestDriveHudController : MonoBehaviour
    {
        [SerializeField] private TestDriveSession session;
        [SerializeField] private RaceHud hud;
        [SerializeField] private Button exitButton;

        private void Start()
        {
            if (exitButton != null) exitButton.onClick.AddListener(() => session?.ExitToMenu());
            if (hud != null) hud.PauseRequested += () => session?.ExitToMenu();
            if (hud != null) hud.SetRaceInfo("TEST DRIVE", "", "Free drive. Tap EXIT to return.", 0f);
        }

#if UNITY_EDITOR
        public void EditorWire(TestDriveSession s, RaceHud h, Button exit)
        {
            session = s; hud = h; exitButton = exit;
        }
#endif
    }
}
