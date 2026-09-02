using RedlineLegends.Core;
using RedlineLegends.Input;
using RedlineLegends.Save;
using UnityEngine;

namespace RedlineLegends.UI
{
    /// <summary>
    /// On-screen driving controls. Shows the layout matching the control style setting and writes
    /// the held state into the local MobileInputProvider every frame. Never touches the vehicle.
    /// </summary>
    public sealed class TouchControls : MonoBehaviour
    {
        [SerializeField] private GameObject steerButtonsGroup;
        [SerializeField] private GameObject steeringWheelGroup;
        [SerializeField] private HoldButton steerLeft;
        [SerializeField] private HoldButton steerRight;
        [SerializeField] private SteeringWheelControl steeringWheel;
        [SerializeField] private HoldButton throttle;
        [SerializeField] private HoldButton brake;
        [SerializeField] private HoldButton handbrake;
        [SerializeField] private HoldButton nitrous;
        [SerializeField] private HoldButton shiftUp;
        [SerializeField] private HoldButton shiftDown;
        [SerializeField] private GameObject manualGroup;

        private MobileInputProvider _provider;
        private SettingsService _settings;

        private void Start()
        {
            if (!Services.IsReady) return;
            _provider = Services.Get<MobileInputProvider>();
            _settings = Services.Get<SettingsService>();
            _settings.Changed += ApplyLayout;
            ApplyLayout(_settings.Current);
        }

        private void OnDestroy()
        {
            if (_settings != null) _settings.Changed -= ApplyLayout;
        }

        private void ApplyLayout(SettingsData settings)
        {
            bool buttons = settings.ControlStyle == ControlStyle.Buttons;
            bool wheel = settings.ControlStyle == ControlStyle.SteeringWheel;
            if (steerButtonsGroup != null) steerButtonsGroup.SetActive(buttons);
            if (steeringWheelGroup != null) steeringWheelGroup.SetActive(wheel);
            if (manualGroup != null) manualGroup.SetActive(settings.Transmission == TransmissionMode.Manual);
        }

        private void Update()
        {
            if (_provider == null) return;
            var touch = new MobileInputProvider.TouchState();
            if (steerButtonsGroup != null && steerButtonsGroup.activeSelf)
            {
                float steer = 0f;
                if (steerLeft != null && steerLeft.IsHeld) steer -= 1f;
                if (steerRight != null && steerRight.IsHeld) steer += 1f;
                touch.Steer = steer;
                touch.SteerActive = steer != 0f;
            }
            else if (steeringWheelGroup != null && steeringWheelGroup.activeSelf && steeringWheel != null)
            {
                touch.Steer = steeringWheel.Value;
                touch.SteerActive = steeringWheel.IsActive;
            }
            touch.Throttle = throttle != null && throttle.IsHeld ? 1f : 0f;
            touch.Brake = brake != null && brake.IsHeld ? 1f : 0f;
            touch.Handbrake = handbrake != null && handbrake.IsHeld;
            touch.Nitrous = nitrous != null && nitrous.IsHeld;
            _provider.Touch = touch;

            if (shiftUp != null && shiftUp.PressedThisFrame) _provider.RequestShiftUp();
            if (shiftDown != null && shiftDown.PressedThisFrame) _provider.RequestShiftDown();
        }

#if UNITY_EDITOR
        public void EditorWire(GameObject buttonsGroup, GameObject wheelGroup, HoldButton left, HoldButton right, SteeringWheelControl wheel,
            HoldButton gas, HoldButton brakePedal, HoldButton hand, HoldButton nos, HoldButton up, HoldButton down, GameObject manual)
        {
            steerButtonsGroup = buttonsGroup; steeringWheelGroup = wheelGroup; steerLeft = left; steerRight = right; steeringWheel = wheel;
            throttle = gas; brake = brakePedal; handbrake = hand; nitrous = nos; shiftUp = up; shiftDown = down; manualGroup = manual;
        }
#endif
    }
}
