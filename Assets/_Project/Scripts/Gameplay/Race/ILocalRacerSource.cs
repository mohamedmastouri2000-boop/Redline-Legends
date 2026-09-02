using System;
using RedlineLegends.Vehicles;

namespace RedlineLegends.Race
{
    /// <summary>
    /// Anything that spawns the local player's car (test drive, race session, later a network
    /// session). HUD, touch controls and camera bind through this instead of knowing the session type.
    /// </summary>
    public interface ILocalRacerSource
    {
        VehicleController LocalVehicle { get; }
        event Action<VehicleController> LocalVehicleSpawned;
    }
}
