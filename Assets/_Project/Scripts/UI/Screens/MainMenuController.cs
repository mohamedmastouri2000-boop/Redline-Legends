using RedlineLegends.Core;
using RedlineLegends.Progression;
using RedlineLegends.Race;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RedlineLegends.UI
{
    /// <summary>
    /// Root of the main menu scene. Switches between the home panel and the event list panels.
    /// Reads profile data from services; never mutates progression itself.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject homePanel;
        [SerializeField] private EventListPanel circuitPanel;
        [SerializeField] private EventListPanel dragPanel;
        [SerializeField] private SettingsPanel settingsPanel;
        [SerializeField] private AchievementsPanel achievementsPanel;

        [Header("Home")]
        [SerializeField] private Button circuitButton;
        [SerializeField] private Button dragButton;
        [SerializeField] private Button garageButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button achievementsButton;
        [SerializeField] private TMP_Text profileNameText;
        [SerializeField] private TMP_Text creditsText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private Image xpFill;
        [SerializeField] private TMP_Text selectedCarText;
        [SerializeField] private TMP_Text resultsBanner;

        private PlayerProfileService _profile;
        private GarageService _garage;
        private SceneFlowService _sceneFlow;
        private RaceLaunchBuilder _launchBuilder;

        private void Start()
        {
            if (!Services.IsReady)
            {
                GameLog.Error("MainMenu opened before GameBootstrap. Press Play from the Bootstrap scene or generate the AppRoot prefab.");
                return;
            }
            _profile = Services.Get<PlayerProfileService>();
            _garage = Services.Get<GarageService>();
            _sceneFlow = Services.Get<SceneFlowService>();
            _launchBuilder = new RaceLaunchBuilder(Services.Get<Content.ContentCatalog>(), _garage, _profile);

            circuitButton.onClick.AddListener(() => ShowPanel(circuitPanel.gameObject));
            dragButton.onClick.AddListener(() => ShowPanel(dragPanel.gameObject));
            garageButton.onClick.AddListener(() => _sceneFlow.LoadGarage());
            if (settingsButton != null && settingsPanel != null)
            {
                settingsButton.onClick.AddListener(() => ShowPanel(settingsPanel.gameObject));
                settingsPanel.BackButton.onClick.AddListener(ShowHome);
            }
            if (achievementsButton != null && achievementsPanel != null)
            {
                achievementsButton.onClick.AddListener(() => { ShowPanel(achievementsPanel.gameObject); achievementsPanel.Refresh(); });
                achievementsPanel.BackButton.onClick.AddListener(ShowHome);
            }

            circuitPanel.Initialize(RaceMode.Circuit, LaunchEvent, ShowHome);
            dragPanel.Initialize(RaceMode.Drag, LaunchEvent, ShowHome);

            _profile.Changed += RefreshProfile;
            RefreshProfile();
            ShowLastResult();
            ShowHome();
        }

        private void OnDestroy()
        {
            if (_profile != null) _profile.Changed -= RefreshProfile;
        }

        private void ShowHome() => ShowPanel(homePanel);

        private void ShowPanel(GameObject panel)
        {
            homePanel.SetActive(panel == homePanel);
            circuitPanel.gameObject.SetActive(panel == circuitPanel.gameObject);
            dragPanel.gameObject.SetActive(panel == dragPanel.gameObject);
            if (settingsPanel != null) settingsPanel.gameObject.SetActive(panel == settingsPanel.gameObject);
            if (achievementsPanel != null) achievementsPanel.gameObject.SetActive(panel == achievementsPanel.gameObject);
            if (panel == circuitPanel.gameObject) circuitPanel.Refresh();
            else if (panel == dragPanel.gameObject) dragPanel.Refresh();
        }

        private void LaunchEvent(Events.RaceEventDefinition evt)
        {
            var request = _launchBuilder.Build(evt, out string reason);
            if (request == null)
            {
                resultsBanner.text = reason;
                resultsBanner.gameObject.SetActive(true);
                return;
            }
            _sceneFlow.LoadRace(request);
        }

        private void RefreshProfile()
        {
            profileNameText.text = _profile.DisplayName;
            creditsText.text = _profile.Credits.ToString("N0") + " CR";
            levelText.text = "LVL " + _profile.Level;
            xpFill.fillAmount = _profile.LevelProgress01;
            var car = _garage.SelectedVehicle;
            selectedCarText.text = car != null ? car.BrandName + " " + car.DisplayName : "No car";
        }

        private void ShowLastResult()
        {
            var outcome = _sceneFlow.LastRaceOutcome;
            if (outcome == null)
            {
                resultsBanner.gameObject.SetActive(false);
                return;
            }
            var player = outcome.FindLocalPlayer();
            resultsBanner.gameObject.SetActive(true);
            resultsBanner.text = player != null && player.Finished
                ? "Last race: P" + player.Position + " in " + Utilities.MathUtil.FormatRaceTime(player.TotalTimeSeconds)
                : "Last race: did not finish";
            _sceneFlow.LastRaceOutcome = null;
        }

#if UNITY_EDITOR
        public void EditorWire(GameObject home, EventListPanel circuit, EventListPanel drag, Button circuitBtn, Button dragBtn,
            Button garageBtn, TMP_Text name, TMP_Text credits, TMP_Text level, Image xp, TMP_Text car, TMP_Text banner,
            SettingsPanel settings, Button settingsBtn, AchievementsPanel achievements, Button achievementsBtn)
        {
            homePanel = home; circuitPanel = circuit; dragPanel = drag; circuitButton = circuitBtn; dragButton = dragBtn;
            garageButton = garageBtn; profileNameText = name; creditsText = credits; levelText = level; xpFill = xp;
            selectedCarText = car; resultsBanner = banner; settingsPanel = settings; settingsButton = settingsBtn;
            achievementsPanel = achievements; achievementsButton = achievementsBtn;
        }
#endif
    }
}
