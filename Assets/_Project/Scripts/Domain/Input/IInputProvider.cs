using RedlineLegends.Race;

namespace RedlineLegends.Input
{
    /// <summary>
    /// Source of vehicle commands. The VehicleController calls <see cref="Sample"/> once per physics
    /// step; <see cref="Tick"/> runs once per rendered frame for providers that need to poll
    /// hardware or smooth values. A future NetworkInputProvider implements this and nothing in
    /// the vehicle changes.
    /// </summary>
    public interface IInputProvider
    {
        ControlSource Source { get; }
        bool Enabled { get; set; }
        void Tick(float deltaTime);
        /// <summary>Returns the current command state and clears latched button edges.</summary>
        VehicleInputState Sample();
        /// <summary>Peek without clearing edges (HUD display).</summary>
        VehicleInputState Peek();
    }
}
