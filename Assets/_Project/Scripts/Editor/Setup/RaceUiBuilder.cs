using RedlineLegends.Cameras;
using RedlineLegends.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RedlineLegends.Editor
{
    /// <summary>Builds the in-race HUD and touch controls for any track scene.</summary>
    public static class RaceUiBuilder
    {
        public struct Result
        {
            public Canvas Canvas;
            public RaceHud Hud;
            public TouchControls Controls;
            public Button PauseButton;
            public TMP_Text Countdown;
            public GameObject PausePanel;
            public Button ResumeButton;
            public Button RestartButton;
            public Button QuitButton;
            public ResultsPanel Results;
            public TutorialOverlay Tutorial;
        }

        private static readonly Color PadNeutral = new Color(0.05f, 0.05f, 0.08f, 0.45f);
        private static readonly Color PadGas = new Color(0.08f, 0.32f, 0.16f, 0.55f);
        private static readonly Color PadBrake = new Color(0.45f, 0.08f, 0.08f, 0.55f);
        private static readonly Color PadNos = new Color(0.06f, 0.3f, 0.45f, 0.55f);

        public static Result Build(MonoBehaviour localRacerSource, VehicleCameraRig cameraRig)
        {
            var canvas = UiKit.CreateCanvas("RaceCanvas", 10);
            var overlays = canvas.transform;          // full-screen curtains
            var root = UiKit.SafeRoot(canvas);        // everything the player reads or touches

            // ---- top shading so white text reads over bright skies
            var shade = UiKit.CreatePanel(overlays, "TopShade", new Color(0f, 0f, 0f, 0.5f), false);
            shade.raycastTarget = false;
            shade.sprite = ProceduralTextures.ShadeUp();
            UiKit.AnchorRange((RectTransform)shade.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -170f), Vector2.zero);
            shade.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            shade.transform.SetAsFirstSibling();

            // ---- race block (top left): position big, lap under it
            var raceBox = UiKit.CreatePanel(root, "RaceBox", new Color(0.02f, 0.02f, 0.04f, 0.55f));
            raceBox.raycastTarget = false;
            UiKit.Anchor((RectTransform)raceBox.transform, new Vector2(0f, 1f), new Vector2(30f, -24f), new Vector2(300f, 116f));
            var position = UiKit.CreateText(raceBox.transform, "Position", "", 60f, UiKit.Accent, TextAlignmentOptions.Left, FontStyles.Bold);
            UiKit.Title(position);
            UiKit.Anchor((RectTransform)position.transform, new Vector2(0f, 1f), new Vector2(20f, -6f), new Vector2(260f, 70f));
            var lap = UiKit.CreateText(raceBox.transform, "Lap", "", 26f, UiKit.TextMain, TextAlignmentOptions.Left, FontStyles.Bold);
            lap.characterSpacing = 2f;
            UiKit.Anchor((RectTransform)lap.transform, new Vector2(0f, 0f), new Vector2(22f, 8f), new Vector2(260f, 34f));

            // ---- timer pill (top centre)
            var timerBox = UiKit.CreatePanel(root, "TimerBox", new Color(0.02f, 0.02f, 0.04f, 0.55f));
            timerBox.raycastTarget = false;
            UiKit.Anchor((RectTransform)timerBox.transform, new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(360f, 64f));
            var timer = UiKit.CreateText(timerBox.transform, "Timer", "", 40f, UiKit.TextMain, TextAlignmentOptions.Center, FontStyles.Bold);
            UiKit.Stretch((RectTransform)timer.transform, 12f, 2f, 12f, 2f);
            var info = UiKit.CreateText(root, "Info", "", 28f, UiKit.TextMain, TextAlignmentOptions.Center, FontStyles.Bold);
            UiKit.Anchor((RectTransform)info.transform, new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(1200f, 40f));
            var progressBar = UiKit.CreateFillBar(root, "Progress", new Color(0.02f, 0.02f, 0.04f, 0.55f), UiKit.Accent, out var progressFill);
            UiKit.Anchor((RectTransform)progressBar.transform, new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(520f, 10f));
            progressFill.fillAmount = 0f;

            // ---- buttons (top right)
            var pause = UiKit.CreateButton(root, "PauseButton", "II", new Color(0.02f, 0.02f, 0.04f, 0.6f), 34f, out _);
            UiKit.Anchor((RectTransform)pause.transform, new Vector2(1f, 1f), new Vector2(-30f, -24f), new Vector2(92f, 72f));
            var camera = UiKit.CreateButton(root, "CameraButton", "CAM", new Color(0.02f, 0.02f, 0.04f, 0.6f), 24f, out _);
            UiKit.Anchor((RectTransform)camera.transform, new Vector2(1f, 1f), new Vector2(-134f, -24f), new Vector2(92f, 72f));
            var reset = UiKit.CreateButton(root, "ResetButton", "RESET", new Color(0.02f, 0.02f, 0.04f, 0.6f), 22f, out _);
            UiKit.Anchor((RectTransform)reset.transform, new Vector2(1f, 1f), new Vector2(-238f, -24f), new Vector2(110f, 72f));

            // ---- instrument (bottom centre)
            var gauge = UiKit.CreateGauge(root, "Gauge", 300f);
            UiKit.Anchor(gauge.Root, new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(300f, 300f));
            var nosGroup = UiKit.CreateRect(root, "NitrousGroup");
            UiKit.Anchor(nosGroup, new Vector2(0.5f, 0f), new Vector2(-330f, 40f), new Vector2(160f, 44f));
            var nosLabel = UiKit.CreateText(nosGroup, "Label", "NOS", 18f, new Color(0.4f, 0.8f, 1f), TextAlignmentOptions.Left, FontStyles.Bold);
            nosLabel.characterSpacing = 4f;
            UiKit.Anchor((RectTransform)nosLabel.transform, new Vector2(0f, 1f), Vector2.zero, new Vector2(160f, 20f));
            var nosBar = UiKit.CreateFillBar(nosGroup, "NitrousBar", new Color(0.02f, 0.02f, 0.04f, 0.6f), new Color(0.35f, 0.8f, 1f), out var nosFill);
            UiKit.AnchorRange((RectTransform)nosBar.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 14f));

            var hud = canvas.gameObject.AddComponent<RaceHud>();
            hud.EditorWire(localRacerSource, cameraRig, gauge.Speed, gauge.Unit, gauge.Gear, gauge.Rpm, gauge.RpmArc, gauge.ShiftLight, nosFill,
                nosGroup.gameObject, lap, position, timer, info, progressFill, pause, camera, reset);
            hud.EditorSetRpmSweep(0.75f);

            // ---- touch controls
            var controls = BuildTouchControls(root);

            // ---- countdown
            var countdown = UiKit.CreateText(overlays, "Countdown", "", 170f, UiKit.TextMain, TextAlignmentOptions.Center, FontStyles.Bold);
            UiKit.Title(countdown);
            UiKit.Anchor((RectTransform)countdown.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(900f, 220f));

            // ---- pause panel
            var pausePanel = UiKit.CreatePanel(overlays, "PausePanel", new Color(0f, 0f, 0f, 0.7f), false);
            UiKit.Stretch((RectTransform)pausePanel.transform);
            var pauseCard = UiKit.CreatePanel(pausePanel.transform, "Card", UiKit.PanelDark);
            UiKit.Anchor((RectTransform)pauseCard.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 520f));
            var pauseTitle = UiKit.CreateText(pauseCard.transform, "Title", "PAUSED", 60f, UiKit.TextMain, TextAlignmentOptions.Center, FontStyles.Bold);
            UiKit.Title(pauseTitle);
            UiKit.Anchor((RectTransform)pauseTitle.transform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(560f, 80f));
            var resume = UiKit.CreateButton(pauseCard.transform, "Resume", "RESUME", UiKit.Accent, 32f, out _);
            UiKit.Anchor((RectTransform)resume.transform, new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(480f, 90f));
            var restart = UiKit.CreateButton(pauseCard.transform, "Restart", "RESTART", UiKit.ButtonNormal, 32f, out _);
            UiKit.Anchor((RectTransform)restart.transform, new Vector2(0.5f, 1f), new Vector2(0f, -250f), new Vector2(480f, 90f));
            var quit = UiKit.CreateButton(pauseCard.transform, "Quit", "QUIT TO MENU", UiKit.ButtonNormal, 32f, out _);
            UiKit.Anchor((RectTransform)quit.transform, new Vector2(0.5f, 1f), new Vector2(0f, -360f), new Vector2(480f, 90f));
            pausePanel.gameObject.SetActive(false);

            // ---- results panel, tutorial, click sounds
            var results = BuildResultsPanel(overlays);
            var tutorial = UiKit.CreateTutorialOverlay(overlays);
            canvas.gameObject.AddComponent<UiClickSound>();

            return new Result
            {
                Canvas = canvas, Hud = hud, Controls = controls, PauseButton = pause, Countdown = countdown,
                PausePanel = pausePanel.gameObject, ResumeButton = resume, RestartButton = restart, QuitButton = quit, Results = results,
                Tutorial = tutorial
            };
        }

        /// <summary>Light tree, reaction time, shift feedback and the lane gap bar for drag scenes.</summary>
        public static DragHudPanel BuildDragPanel(Transform root)
        {
            var panelRect = UiKit.CreateRect(root, "DragPanel");
            UiKit.Stretch(panelRect);
            var panel = panelRect.gameObject.AddComponent<DragHudPanel>();

            // Light tree: vertical column on the right-centre.
            var tree = UiKit.CreatePanel(panelRect, "LightTree", new Color(0.02f, 0.02f, 0.04f, 0.6f));
            tree.raycastTarget = false;
            UiKit.Anchor((RectTransform)tree.transform, new Vector2(1f, 0.5f), new Vector2(-60f, 60f), new Vector2(90f, 420f));
            var ambers = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                ambers[i] = Lamp(tree.transform, "Amber" + i, -20f - i * 80f);
            }
            var green = Lamp(tree.transform, "Green", -260f);
            var red = Lamp(tree.transform, "Red", -340f);

            var reaction = UiKit.CreateText(panelRect, "Reaction", "", 34f, UiKit.TextMain, TextAlignmentOptions.Center, FontStyles.Bold);
            UiKit.Anchor((RectTransform)reaction.transform, new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(600f, 44f));
            var shift = UiKit.CreateText(panelRect, "ShiftFeedback", "", 44f, UiKit.TextMain, TextAlignmentOptions.Center, FontStyles.Bold);
            UiKit.Title(shift);
            UiKit.Anchor((RectTransform)shift.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 220f), new Vector2(800f, 60f));

            // Gap bar: strip with two markers.
            var gapBg = UiKit.CreatePanel(panelRect, "GapTrack", new Color(0.02f, 0.02f, 0.04f, 0.6f));
            gapBg.raycastTarget = false;
            UiKit.Anchor((RectTransform)gapBg.transform, new Vector2(0.5f, 0f), new Vector2(0f, 340f), new Vector2(700f, 10f));
            var gapRect = (RectTransform)gapBg.transform;
            var playerMarker = UiKit.CreatePanel(gapBg.transform, "PlayerMarker", UiKit.Accent);
            playerMarker.raycastTarget = false;
            UiKit.Anchor((RectTransform)playerMarker.transform, new Vector2(0f, 0.5f), new Vector2(0f, 12f), new Vector2(14f, 22f));
            var oppMarker = UiKit.CreatePanel(gapBg.transform, "OpponentMarker", new Color(0.4f, 0.75f, 1f, 1f));
            oppMarker.raycastTarget = false;
            UiKit.Anchor((RectTransform)oppMarker.transform, new Vector2(0f, 0.5f), new Vector2(0f, -12f), new Vector2(14f, 22f));
            var opponent = UiKit.CreateText(panelRect, "OpponentGap", "", 26f, UiKit.TextDim, TextAlignmentOptions.Center);
            UiKit.Anchor((RectTransform)opponent.transform, new Vector2(0.5f, 0f), new Vector2(0f, 365f), new Vector2(700f, 34f));

            panel.EditorWire(ambers, green, red, reaction, shift, opponent, gapRect, (RectTransform)playerMarker.transform, (RectTransform)oppMarker.transform);
            return panel;
        }

        private static Image Lamp(Transform tree, string name, float y)
        {
            var lamp = UiKit.CreatePanel(tree, name, new Color(0.2f, 0.2f, 0.22f, 0.8f), false);
            lamp.sprite = ProceduralTextures.Circle();
            lamp.raycastTarget = false;
            UiKit.Anchor((RectTransform)lamp.transform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(60f, 60f));
            return lamp;
        }

        private static ResultsPanel BuildResultsPanel(Transform root)
        {
            var curtain = UiKit.CreatePanel(root, "ResultsPanel", new Color(0.02f, 0.02f, 0.04f, 0.88f), false);
            UiKit.Stretch((RectTransform)curtain.transform);
            var results = curtain.gameObject.AddComponent<ResultsPanel>();
            var card = UiKit.CreatePanel(curtain.transform, "Card", UiKit.PanelDark);
            UiKit.AnchorRange((RectTransform)card.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-600f, -440f), new Vector2(600f, 440f));
            var stripe = UiKit.CreatePanel(card.transform, "Stripe", UiKit.Accent);
            stripe.raycastTarget = false;
            UiKit.AnchorRange((RectTransform)stripe.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -6f), Vector2.zero);

            var title = UiKit.CreateText(card.transform, "Title", "FINISHED", 72f, UiKit.TextMain, TextAlignmentOptions.Center, FontStyles.Bold);
            UiKit.Title(title);
            UiKit.Anchor((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(1100f, 90f));
            var reward = UiKit.CreateText(card.transform, "Reward", "", 34f, new Color(1f, 0.85f, 0.3f), TextAlignmentOptions.Center, FontStyles.Bold);
            UiKit.Anchor((RectTransform)reward.transform, new Vector2(0.5f, 1f), new Vector2(0f, -122f), new Vector2(1100f, 48f));

            UiKit.CreateScrollList(card.transform, "List", out var content);
            UiKit.AnchorRange((RectTransform)content.parent, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(40f, 140f), new Vector2(-40f, -180f));

            var rowBg = UiKit.CreatePanel(content, "RowTemplate", UiKit.PanelMid);
            UiKit.SetPreferredHeight(rowBg, 64f);
            var row = rowBg.gameObject.AddComponent<ResultRow>();
            var pos = UiKit.CreateText(rowBg.transform, "Pos", "1", 32f, UiKit.Accent, TextAlignmentOptions.Center, FontStyles.Bold);
            UiKit.AnchorRange((RectTransform)pos.transform, new Vector2(0f, 0f), new Vector2(0.08f, 1f), Vector2.zero, Vector2.zero);
            var name = UiKit.CreateText(rowBg.transform, "Name", "Racer", 30f, UiKit.TextMain, TextAlignmentOptions.Left, FontStyles.Bold);
            UiKit.AnchorRange((RectTransform)name.transform, new Vector2(0.08f, 0f), new Vector2(0.5f, 1f), new Vector2(12f, 0f), Vector2.zero);
            var time = UiKit.CreateText(rowBg.transform, "Time", "", 28f, UiKit.TextMain, TextAlignmentOptions.Right);
            UiKit.AnchorRange((RectTransform)time.transform, new Vector2(0.5f, 0f), new Vector2(0.76f, 1f), Vector2.zero, Vector2.zero);
            var lap = UiKit.CreateText(rowBg.transform, "BestLap", "", 24f, UiKit.TextDim, TextAlignmentOptions.Right);
            UiKit.AnchorRange((RectTransform)lap.transform, new Vector2(0.76f, 0f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-16f, 0f));
            row.EditorWire(pos, name, time, lap, rowBg);

            var cont = UiKit.CreateButton(card.transform, "Continue", "CONTINUE", UiKit.Accent, 32f, out _);
            UiKit.Anchor((RectTransform)cont.transform, new Vector2(0.5f, 0f), new Vector2(200f, 36f), new Vector2(360f, 84f));
            var restart = UiKit.CreateButton(card.transform, "Restart", "RESTART", UiKit.ButtonNormal, 32f, out _);
            UiKit.Anchor((RectTransform)restart.transform, new Vector2(0.5f, 0f), new Vector2(-200f, 36f), new Vector2(360f, 84f));

            results.EditorWire(title, reward, content, row, cont, restart);
            curtain.gameObject.SetActive(false);
            return results;
        }

        private static TouchControls BuildTouchControls(Transform root)
        {
            var controlsRoot = UiKit.CreateRect(root, "TouchControls");
            UiKit.Stretch(controlsRoot);
            var controls = controlsRoot.gameObject.AddComponent<TouchControls>();
            var arrow = ProceduralTextures.Arrow();
            var pedal = ProceduralTextures.Pedal();

            var buttonsGroup = UiKit.CreateRect(controlsRoot, "SteerButtons");
            UiKit.Stretch(buttonsGroup);
            var left = UiKit.CreatePad(buttonsGroup, "SteerLeft", null, arrow, new Vector2(0f, 0f), new Vector2(150f, 130f), new Vector2(210f, 210f), PadNeutral, true);
            var right = UiKit.CreatePad(buttonsGroup, "SteerRight", null, arrow, new Vector2(0f, 0f), new Vector2(390f, 130f), new Vector2(210f, 210f), PadNeutral);

            var wheelGroup = UiKit.CreateRect(controlsRoot, "SteeringWheel");
            UiKit.Stretch(wheelGroup);
            var wheelImage = UiKit.CreatePanel(wheelGroup, "Wheel", new Color(1f, 1f, 1f, 0.55f), false);
            wheelImage.sprite = ProceduralTextures.Ring();
            UiKit.Anchor((RectTransform)wheelImage.transform, new Vector2(0f, 0f), new Vector2(260f, 240f), new Vector2(360f, 360f));
            var hub = UiKit.CreatePanel(wheelImage.transform, "Hub", new Color(0.05f, 0.05f, 0.08f, 0.35f), false);
            hub.sprite = ProceduralTextures.Circle();
            hub.raycastTarget = false;
            UiKit.Anchor((RectTransform)hub.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 300f));
            var wheelRing = UiKit.CreatePanel(wheelImage.transform, "Marker", UiKit.Accent);
            wheelRing.raycastTarget = false;
            UiKit.Anchor((RectTransform)wheelRing.transform, new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(24f, 60f));
            var wheel = wheelImage.gameObject.AddComponent<SteeringWheelControl>();
            wheel.EditorWire((RectTransform)wheelImage.transform);
            wheelGroup.gameObject.SetActive(false);

            var throttle = UiKit.CreatePad(controlsRoot, "Throttle", "GAS", pedal, new Vector2(1f, 0f), new Vector2(-150f, 140f), new Vector2(230f, 230f), PadGas);
            var brake = UiKit.CreatePad(controlsRoot, "Brake", "BRAKE", pedal, new Vector2(1f, 0f), new Vector2(-400f, 120f), new Vector2(190f, 190f), PadBrake);
            var handbrake = UiKit.CreatePad(controlsRoot, "Handbrake", "HB", null, new Vector2(1f, 0f), new Vector2(-400f, 300f), new Vector2(130f, 130f), PadNeutral);
            var nitrous = UiKit.CreatePad(controlsRoot, "Nitrous", "NOS", null, new Vector2(1f, 0f), new Vector2(-150f, 330f), new Vector2(150f, 150f), PadNos);

            var manualGroup = UiKit.CreateRect(controlsRoot, "ManualShift");
            UiKit.Stretch(manualGroup);
            var shiftUp = UiKit.CreatePad(manualGroup, "ShiftUp", "+", null, new Vector2(0f, 0f), new Vector2(390f, 360f), new Vector2(130f, 130f), PadNeutral);
            var shiftDown = UiKit.CreatePad(manualGroup, "ShiftDown", "-", null, new Vector2(0f, 0f), new Vector2(150f, 360f), new Vector2(130f, 130f), PadNeutral);

            controls.EditorWire(buttonsGroup.gameObject, wheelGroup.gameObject, left, right, wheel, throttle, brake, handbrake, nitrous,
                shiftUp, shiftDown, manualGroup.gameObject);
            return controls;
        }
    }
}
