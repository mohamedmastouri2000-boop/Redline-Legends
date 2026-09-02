using System;

namespace RedlineLegends.Core
{
    public enum GameStateId
    {
        Boot,
        MainMenu,
        Garage,
        Loading,
        Race,
        Results
    }

    /// <summary>
    /// Coarse application state. Systems subscribe to <see cref="StateChanged"/> instead of polling
    /// scene names, so scene layout can change without touching gameplay code.
    /// </summary>
    public sealed class GameStateMachine
    {
        public GameStateId Current { get; private set; } = GameStateId.Boot;
        public GameStateId Previous { get; private set; } = GameStateId.Boot;

        public event Action<GameStateId, GameStateId> StateChanged;

        public void TransitionTo(GameStateId next)
        {
            if (next == Current) return;
            Previous = Current;
            Current = next;
            StateChanged?.Invoke(Previous, next);
        }
    }
}
