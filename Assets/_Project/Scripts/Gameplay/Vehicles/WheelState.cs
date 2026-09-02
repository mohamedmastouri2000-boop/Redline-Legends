using UnityEngine;

namespace RedlineLegends.Vehicles
{
    /// <summary>Authoring data for one wheel, produced by the factory from the visual prefab.</summary>
    public struct WheelSetup
    {
        public string Name;
        /// <summary>Hub position in vehicle local space at rest ride height.</summary>
        public Vector3 LocalHubAtRest;
        public float Radius;
        public bool IsFront;
        public bool IsLeft;
        public Transform Visual;
    }

    /// <summary>
    /// Per-wheel simulation state. Plain class (not a component) so the controller iterates it
    /// without GetComponent calls and the whole vehicle state stays in one object.
    /// </summary>
    public sealed class WheelState
    {
        public string Name;
        public bool IsFront;
        public bool IsLeft;
        public bool IsSteer;
        public bool IsDriven;
        public float Radius;
        /// <summary>Top of the suspension ray in local space (hub at rest + travel).</summary>
        public Vector3 LocalAttachPoint;
        public Transform Visual;

        // Contact
        public bool Grounded;
        public Vector3 ContactPoint;
        public Vector3 ContactNormal;
        public Vector3 ContactVelocity;
        public float HitDistance;

        // Suspension
        public float Compression;      // 0 = fully extended, 1 = bottomed out
        public float PrevCompression;
        public float Load;             // normal force in N

        // Tyre
        public Vector3 Forward;
        public Vector3 Right;
        public float SteerAngleDeg;
        public float LongVelocity;     // m/s along wheel forward at contact
        public float LatVelocity;      // m/s along wheel right at contact
        public float SpinSpeed;        // extra surface speed from wheel spin, m/s
        public float SlipRatio;        // longitudinal slip for effects (0..1+)
        public float SlipAngleDeg;
        public float SlipAmount;       // 0..1 combined sliding amount for smoke/skids/audio
        public bool Locked;
        public float DriveForce;       // N delivered this step
        public float BrakeForce;       // N delivered this step

        // Visual
        public float AngularVelocity;  // rad/s
        public float RotationDeg;
        public float VisualHubLocalY;
    }
}
