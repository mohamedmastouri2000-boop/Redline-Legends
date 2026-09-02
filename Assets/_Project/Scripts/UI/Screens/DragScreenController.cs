using RedlineLegends.DragRace;
using RedlineLegends.Race;
using RedlineLegends.Vehicles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RedlineLegends.UI
{
    /// <summary>Binds the drag session to the HUD, drag panel, pause menu and results. No rules here.</summary>
    public sealed class DragScreenController : MonoBehaviour
    {
        [SerializeField] private DragSession session;
        [SerializeField] private RaceHud hud;
        [SerializeField] private DragHudPanel dragPanel;
        [SerializeField] private TMP_Text countdownText;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private ResultsPanel resultsPanel;
        [SerializeField] private GameObject touchControls;

        private string _message = "";
        private float _messageTimer;
        private float _countdownFade;

        private void Start()
        {
            if (session == null) return;
            session.Message += ShowMessage;
            session.LightChanged += OnLight;
            session.FalseStarted += OnFalseStart;
            session.PlayerShifted += OnShift;
            session.RaceCompleted += OnCompleted;
            session.StateChanged += OnStateChanged;
            if (hud != null) hud.PauseRequested += TogglePause;
            if (resumeButton != null) resumeButton.onClick.AddListener(TogglePause);
            if (restartButton != null) restartButton.onClick.AddListener(() => session.Restart());
            if (quitButton != null) quitButton.onClick.AddListener(() => session.QuitToMenu());
            if (resultsPanel != null)
            {
                resultsPanel.ContinueButton.onClick.AddListener(() => session.ContinueToMenu());
                resultsPanel.RestartButton.onClick.AddListener(() => session.Restart());
                resultsPanel.gameObject.SetActive(false);
            }
            if (pausePanel != null) pausePanel.SetActive(false);
            if (countdownText != null) countdownText.text = "";
        }

        private void OnDestroy()
        {
            if (session == null) return;
            session.Message -= ShowMessage;
            session.LightChanged -= OnLight;
            session.FalseStarted -= OnFalseStart;
            session.PlayerShifted -= OnShift;
            session.RaceCompleted -= OnCompleted;
            session.StateChanged -= OnStateChanged;
        }

        private void Update()
        {
            if (session == null || hud == null || session.Player == null) return;
            var player = session.Player;
            var opponent = session.Opponent;

            string label = session.Event != null ? session.Event.ModeLabel.ToUpperInvariant() : "DRAG";
            string info = _messageTimer > 0f ? _message : session.State == DragState.Staging ? "STAGE: BUILD YOUR REVS" : "";
            if (_messageTimer > 0f) _messageTimer -= Time.unscaledDeltaTime;
            hud.SetRaceInfo(label, opponent != null && session.State >= DragState.Racing ? "P" + player.Position : "", info,
                Mathf.Clamp01(player.TotalProgress / session.DistanceMeters));
            hud.SetTimer(Mathf.Max(0f, session.RaceTime));

            if (dragPanel != null)
            {
                float opp01 = opponent != null ? opponent.TotalProgress / session.DistanceMeters : 0f;
                float gap = opponent != null ? opponent.TotalProgress - player.TotalProgress : 0f;
                dragPanel.SetProgress(player.TotalProgress / session.DistanceMeters, opp01, gap, opponent != null ? opponent.Spec.DisplayName : "");
                if (player.ReactionTime >= 0f || player.FalseStart) dragPanel.SetReaction(player.ReactionTime, player.FalseStart);
            }

            if (countdownText != null && _countdownFade > 0f)
            {
                _countdownFade -= Time.deltaTime;
                if (_countdownFade <= 0f) countdownText.text = "";
            }
        }

        private void OnLight(int stage)
        {
            dragPanel?.SetLights(stage, session.Player != null && session.Player.FalseStart);
            if (countdownText != null && stage > 3)
            {
                countdownText.text = "GO!";
                _countdownFade = 0.8f;
            }
        }

        private void OnFalseStart(RacerState racer)
        {
            if (racer.IsLocalPlayer) dragPanel?.SetLights(session.LightStage, true);
        }

        private void OnShift(ShiftQuality quality) => dragPanel?.ShowShift(quality);

        private void ShowMessage(string message)
        {
            _message = message.ToUpperInvariant();
            _messageTimer = 2.5f;
        }

        private void OnStateChanged(DragState state)
        {
            if (touchControls != null) touchControls.SetActive(state != DragState.Finished);
        }

        private void OnCompleted(RaceOutcome outcome)
        {
            if (touchControls != null) touchControls.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
            if (resultsPanel != null) resultsPanel.Show(outcome, session.Reward, false);
        }

        private void TogglePause()
        {
            if (session.State == DragState.Finished) return;
            if (session.IsPaused) session.Resume(); else session.Pause();
            if (pausePanel != null) pausePanel.SetActive(session.IsPaused);
            if (touchControls != null) touchControls.SetActive(!session.IsPaused);
        }

#if UNITY_EDITOR
        public void EditorWire(DragSession s, RaceHud h, DragHudPanel panel, TMP_Text countdown, GameObject pause, Button resume,
            Button restart, Button quit, ResultsPanel results, GameObject controls)
        {
            session = s; hud = h; dragPanel = panel; countdownText = countdown; pausePanel = pause; resumeButton = resume;
            restartButton = restart; quitButton = quit; resultsPanel = results; touchControls = controls;
        }
#endif
    }
}
