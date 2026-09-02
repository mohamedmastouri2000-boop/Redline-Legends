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
        }

        public static Result Build(MonoBehaviour localRacerSource, VehicleCameraRig cameraRig)
        {
            var canvas = UiKit.CreateCanvas("RaceCanvas", 10);
            var root = canvas.transform;

            // ---- top bar
            var lap = UiKit.CreateText(root, "Lap", "", 34f, UiKit.TextMain, TextAlignmentOptions.Left, FontStyles.Bold);
            UiKit.Anchor((RectTransform)lap.transform, new Vector2(0f, 1f), new Vector2(40f, -28f), new Vector2(420f, 44f));
            var position = UiKit.CreateText(root, "Position", "", 54f, UiKit.Accent, TextAlignmentOptions.Left, FontStyles.Bold);
            UiKit.Anchor((RectTransform)position.transform, new Vector2(0f, 1f), new Vector2(40f, -74f), new Vector2(420f, 64f));
            var timer = UiKit.CreateText(root, "Timer", "", 40f, UiKit.TextMain, TextAlignmentOptions.Center, FontStyles.Bold);
            UiKit.Anchor((RectTransform)timer.transform, new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(420f, 50f));
            var info = UiKit.CreateText(root, "Info", "", 30f, UiKit.TextMain, TextAlignmentOptions.Center, FontStyles.Bold);
            UiKit.Anchor((RectTransform)info.transform, new Vector2(0.5f, 1f), new Vector2(0f, -84f), new Vector2(1200f, 44f));
            var progressBar = UiKit.CreateFillBar(root, "Progress", new Color(0.1f, 0.1f, 0.12f, 0.6f), UiKit.Accent, out var progressFill);
            UiKit.Anchor((RectTransform)progressBar.transform, new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(520f, 6f));
            progressFill.fillAmount = 0f;

            var pause = UiKit.CreateButton(root, "PauseButton", "II", new Color(0f, 0f, 0f, 0.45f), 34f, out _);
            UiKit.Anchor((RectTransform)pause.transform, new Vector2(1f, 1f), new Vector2(-40f, -28f), new Vector2(96f, 72f));
            var camera = UiKit.CreateButton(root, "CameraButton", "CAM", new Color(0f, 0f, 0f, 0.45f), 24f, out _);
            UiKit.Anchor((RectTransform)camera.transform, new Vector2(1f, 1f), new Vector2(-150f, -28f), new Vector2(96f, 72f));
            var reset = UiKit.CreateButton(root, "ResetButton", "RESET", new Color(0f, 0f, 0f, 0.45f), 22f, out _);
            UiKit.Anchor((RectTransform)reset.transform, new Vector2(1f, 1f), new Vector2(-260f, -28f), new Vector2(110f, 72f));

            // ---- vehicle cluster (bottom centre)
            var cluster = UiKit.CreateRect(root, "Cluster");
            UiKit.Anchor(cluster, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(560f, 190f));
            var rpmBar = UiKit.CreateFillBar(cluster, "RpmBar", new Color(0.08f, 0.08f, 0.1f, 0.7f), Color.white, out var rpmFill);
            UiKit.AnchorRange((RectTransform)rpmBar.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -22f), new Vector2(0f, -4f));
            rpmFill.fillAmount = 0f;
            var shift = UiKit.CreatePanel(cluster, "ShiftLight", new Color(0.3f, 0.3f, 0.3f, 0.6f));
            shift.raycastTarget = false;
            UiKit.Anchor((RectTransform)shift.transform, new Vector2(1f, 1f), new Vector2(26f, -4f), new Vector2(20f, 20f));
            var speed = UiKit.CreateText(cluster, "Speed", "0", 96f, UiKit.TextMain, TextAlignmentOptions.Center, FontStyles.Bold);
            UiKit.Anchor((RectTransform)speed.transform, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(300f, 120f));
            var unit = UiKit.CreateText(cluster, "Unit", "KM/H", 22f, UiKit.TextDim, TextAlignmentOptions.Center, FontStyles.Bold);
            UiKit.Anchor((RectTransform)unit.transform, new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(200f, 28f));
            var gearBox = UiKit.CreatePanel(cluster, "GearBox", new Color(0f, 0f, 0f, 0.5f));
            gearBox.raycastTarget = false;
            UiKit.Anchor((RectTransform)gearBox.transform, new Vector2(1f, 0f), new Vector2(-10f, 40f), new Vector2(90f, 100f));
            var gear = UiKit.CreateText(gearBox.transform, "Gear", "1", 64f, UiKit.Accent, TextAlignmentOptions.Center, FontStyles.Bold);
            UiKit.Stretch((RectTransform)gear.transform);
            var rpm = UiKit.CreateText(cluster, "Rpm", "0", 22f, UiKit.TextDim, TextAlignmentOptions.Left);
            UiKit.Anchor((RectTransform)rpm.transform, new Vector2(0f, 0f), new Vector2(10f, 40f), new Vector2(160f, 30f));
            var nosGroup = UiKit.CreateRect(cluster, "NitrousGroup");
            UiKit.Anchor(nosGroup, new Vector2(0f, 0f), new Vector2(10f, 76f), new Vector2(160f, 40f));
            var nosLabel = UiKit.CreateText(nosGroup, "Label", "NOS", 18f, new Color(0.4f, 0.8f, 1f), TextAlignmentOptions.Left, FontStyles.Bold);
            UiKit.Anchor((RectTransform)nosLabel.transform, new Vector2(0f, 1f), Vector2.zero, new Vector2(160f, 20f));
            var nosBar = UiKit.CreateFillBar(nosGroup, "NitrousBar", new Color(0.08f, 0.08f, 0.1f, 0.7f), new Color(0.35f, 0.8f, 1f), out var nosFill);
            UiKit.AnchorRange((RectTransform)nosBar.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 12f));

            var hud = canvas.gameObject.AddComponent<RaceHud>();
            hud.EditorWire(localRacerSource, cameraRig, speed, unit, gear, rpm, rpmFill, shift, nosFill, nosGroup.gameObject,
                lap, position, timer, info, progressFill, pause, camera, reset);

            // ---- touch controls
            var controls = BuildTouchControls(root);

            return new Result { Canvas = canvas, Hud = hud, Controls = controls, PauseButton = pause };
        }

        private static TouchControls BuildTouchControls(Transform root)
        {
            var controlsRoot = UiKit.CreateRect(root, "TouchControls");
            UiKit.Stretch(controlsRoot);
            var controls = controlsRoot.gameObject.AddComponent<TouchControls>();

            var buttonsGroup = UiKit.CreateRect(controlsRoot, "SteerButtons");
            UiKit.Stretch(buttonsGroup);
            var left = Pad(buttonsGroup, "SteerLeft", "<", new Vector2(0f, 0f), new Vector2(150f, 130f), new Vector2(210f, 210f));
            var right = Pad(buttonsGroup, "SteerRight", ">", new Vector2(0f, 0f), new Vector2(390f, 130f), new Vector2(210f, 210f));

            var wheelGroup = UiKit.CreateRect(controlsRoot, "SteeringWheel");
            UiKit.Stretch(wheelGroup);
            var wheelImage = UiKit.CreatePanel(wheelGroup, "Wheel", new Color(1f, 1f, 1f, 0.3f));
            UiKit.Anchor((RectTransform)wheelImage.transform, new Vector2(0f, 0f), new Vector2(260f, 240f), new Vector2(360f, 360f));
            var wheelRing = UiKit.CreatePanel(wheelImage.transform, "Marker", UiKit.Accent);
            wheelRing.raycastTarget = false;
            UiKit.Anchor((RectTransform)wheelRing.transform, new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(24f, 60f));
            var wheel = wheelImage.gameObject.AddComponent<SteeringWheelControl>();
            wheel.EditorWire((RectTransform)wheelImage.transform);
            wheelGroup.gameObject.SetActive(false);

            var throttle = Pad(controlsRoot, "Throttle", "GAS", new Vector2(1f, 0f), new Vector2(-150f, 140f), new Vector2(230f, 230f));
            var brake = Pad(controlsRoot, "Brake", "BRAKE", new Vector2(1f, 0f), new Vector2(-400f, 120f), new Vector2(190f, 190f));
            var handbrake = Pad(controlsRoot, "Handbrake", "HB", new Vector2(1f, 0f), new Vector2(-400f, 300f), new Vector2(130f, 130f));
            var nitrous = Pad(controlsRoot, "Nitrous", "NOS", new Vector2(1f, 0f), new Vector2(-150f, 330f), new Vector2(150f, 150f));

            var manualGroup = UiKit.CreateRect(controlsRoot, "ManualShift");
            UiKit.Stretch(manualGroup);
            var shiftUp = Pad(manualGroup, "ShiftUp", "+", new Vector2(0f, 0f), new Vector2(390f, 360f), new Vector2(130f, 130f));
            var shiftDown = Pad(manualGroup, "ShiftDown", "-", new Vector2(0f, 0f), new Vector2(150f, 360f), new Vector2(130f, 130f));

            controls.EditorWire(buttonsGroup.gameObject, wheelGroup.gameObject, left, right, wheel, throttle, brake, handbrake, nitrous,
                shiftUp, shiftDown, manualGroup.gameObject);
            return controls;
        }

        private static HoldButton Pad(Transform parent, string name, string label, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var image = UiKit.CreatePanel(parent, name, new Color(1f, 1f, 1f, 0.35f));
            UiKit.Anchor((RectTransform)image.transform, anchor, position, size);
            var text = UiKit.CreateText(image.transform, "Label", label, size.y * 0.32f, new Color(0f, 0f, 0f, 0.7f), TextAlignmentOptions.Center, FontStyles.Bold);
            UiKit.Stretch((RectTransform)text.transform);
            var button = image.gameObject.AddComponent<HoldButton>();
            button.EditorWire(image);
            return button;
        }
    }
}
