using RedlineLegends.Events;
using RedlineLegends.Race;
using RedlineLegends.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RedlineLegends.UI
{
    /// <summary>
    /// Glue between the race session and the race UI: HUD race info, countdown, messages, pause
    /// menu and the results panel. Contains no race rules.
    /// </summary>
    public sealed class RaceScreenController : MonoBehaviour
    {
        [SerializeField] private RaceSession session;
        [SerializeField] private RaceHud hud;
        [SerializeField] private TMP_Text countdownText;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private ResultsPanel resultsPanel;
        [SerializeField] private GameObject touchControls;
        [SerializeField] private TutorialOverlay tutorial;

        private float _messageTimer;
        private string _message = "";
        private float _countdownFade;

        private void Start()
        {
            if (session == null) return;
            session.CountdownTick += OnCountdownTick;
            session.Message += ShowMessage;
            session.LapCompleted += OnLapCompleted;
            session.RaceCompleted += OnRaceCompleted;
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

        private bool _tutorialHandled;

        /// <summary>Start order between session and UI is not guaranteed, so the hand-off is polled.</summary>
        private void HandleTutorial()
        {
            if (_tutorialHandled || session == null || !session.WaitingForTutorial) return;
            _tutorialHandled = true;
            if (tutorial != null && Core.Services.TryGet<Progression.TutorialService>(out var tutorials))
            {
                tutorial.Show(Progression.TutorialService.PagesFor(Progression.TutorialIds.FirstCircuit), () =>
                {
                    tutorials.Complete(Progression.TutorialIds.FirstCircuit);
                    session.BeginRace();
                });
            }
            else session.BeginRace();
        }

        private void OnDestroy()
        {
            if (session == null) return;
            session.CountdownTick -= OnCountdownTick;
            session.Message -= ShowMessage;
            session.LapCompleted -= OnLapCompleted;
            session.RaceCompleted -= OnRaceCompleted;
            session.StateChanged -= OnStateChanged;
        }

        private void Update()
        {
            if (session == null || hud == null) return;
            HandleTutorial();
            var player = session.Player;
            if (player == null) return;

            string lap = session.IsPractice ? "FREE DRIVE"
                : session.EventType == CircuitEventType.TimeAttack ? "LAP " + Mathf.Min(player.Lap, session.Laps) + "/" + session.Laps
                : "LAP " + Mathf.Min(player.Lap, session.Laps) + "/" + session.Laps;
            string position = session.IsPractice || session.EventType == CircuitEventType.TimeAttack
                ? ""
                : "P" + player.Position + "/" + session.Racers.Count;

            string info = _message;
            if (_messageTimer <= 0f)
            {
                if (player.WrongWay) info = "WRONG WAY";
                else if (session.EventType == CircuitEventType.Elimination && session.State == RaceState.Racing)
                    info = "ELIMINATION IN " + Mathf.CeilToInt(session.EliminationRemaining) + "s";
                else if (session.EventType == CircuitEventType.Checkpoint && session.State == RaceState.Racing)
                    info = "TIME " + session.CheckpointClock.ToString("0.0");
                else info = "";
            }
            else _messageTimer -= Time.unscaledDeltaTime;

            float progress = session.Laps > 0 && session.Layout != null && session.Layout.LapLength > 0f
                ? Mathf.Clamp01(player.TotalProgress / (session.Laps * session.Layout.LapLength))
                : 0f;
            hud.SetRaceInfo(lap, position, info, progress);
            hud.SetTimer(session.RaceTime);

            if (countdownText != null && _countdownFade > 0f)
            {
                _countdownFade -= Time.deltaTime;
                if (_countdownFade <= 0f) countdownText.text = "";
            }
        }

        private void OnCountdownTick(int value)
        {
            if (countdownText == null) return;
            countdownText.text = value > 0 ? value.ToString() : "GO!";
            _countdownFade = value > 0 ? 1.5f : 1f;
        }

        private void ShowMessage(string message)
        {
            _message = message.ToUpperInvariant();
            _messageTimer = 2.5f;
        }

        private void OnLapCompleted(RacerState racer, int lap, float time)
        {
            if (!racer.IsLocalPlayer) return;
            ShowMessage("Lap " + lap + "  " + MathUtil.FormatRaceTime(time) + (racer.BestLap >= time - 0.0005f ? "  best" : ""));
        }

        private void OnStateChanged(RaceState state)
        {
            if (touchControls != null) touchControls.SetActive(state == RaceState.Racing || state == RaceState.Countdown);
        }

        private void OnRaceCompleted(RaceOutcome outcome)
        {
            if (touchControls != null) touchControls.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
            if (resultsPanel != null) resultsPanel.Show(outcome, session.Reward, session.IsPractice);
        }

        private void TogglePause()
        {
            if (session.State == RaceState.Finished) return;
            if (session.IsPaused) session.Resume(); else session.Pause();
            if (pausePanel != null) pausePanel.SetActive(session.IsPaused);
            if (touchControls != null) touchControls.SetActive(!session.IsPaused);
        }

#if UNITY_EDITOR
        public void EditorWire(RaceSession s, RaceHud h, TMP_Text countdown, GameObject pause, Button resume, Button restart, Button quit,
            ResultsPanel results, GameObject controls, TutorialOverlay tut)
        {
            session = s; hud = h; countdownText = countdown; pausePanel = pause; resumeButton = resume; restartButton = restart;
            quitButton = quit; resultsPanel = results; touchControls = controls; tutorial = tut;
        }
#endif
    }
}
