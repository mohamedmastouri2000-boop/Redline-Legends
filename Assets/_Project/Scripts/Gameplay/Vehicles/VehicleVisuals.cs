using UnityEngine;

namespace RedlineLegends.Vehicles
{
    /// <summary>
    /// Drives wheel meshes from the simulation (spin, steer, suspension travel). Runs in LateUpdate
    /// after the interpolated rigidbody pose so wheels never lag the body.
    /// </summary>
    public sealed class VehicleVisuals : MonoBehaviour
    {
        private VehicleController _controller;
        private Quaternion[] _wheelRestRotations;

        public void Initialize(VehicleController controller)
        {
            _controller = controller;
            var wheels = controller.Wheels;
            _wheelRestRotations = new Quaternion[wheels.Length];
            for (int i = 0; i < wheels.Length; i++)
                _wheelRestRotations[i] = wheels[i].Visual != null ? wheels[i].Visual.localRotation : Quaternion.identity;
        }

        private void LateUpdate()
        {
            if (_controller == null) return;
            var wheels = _controller.Wheels;
            for (int i = 0; i < wheels.Length; i++)
            {
                var w = wheels[i];
                if (w.Visual == null) continue;
                var pos = w.Visual.localPosition;
                pos.y = w.VisualHubLocalY;
                w.Visual.localPosition = pos;
                w.Visual.localRotation = Quaternion.Euler(0f, w.SteerAngleDeg, 0f) * _wheelRestRotations[i] * Quaternion.Euler(w.RotationDeg, 0f, 0f);
            }
        }
    }
}
