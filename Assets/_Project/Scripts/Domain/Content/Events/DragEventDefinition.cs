using RedlineLegends.Race;
using RedlineLegends.Vehicles;
using UnityEngine;

namespace RedlineLegends.Events
{
    public enum DragDistance
    {
        QuarterMile,
        HalfMile
    }

    [CreateAssetMenu(fileName = "evt_drag", menuName = "Redline Legends/Drag Event Definition")]
    public sealed class DragEventDefinition : RaceEventDefinition
    {
        [SerializeField] private DragDistance distance = DragDistance.QuarterMile;
        [Tooltip("Specific opponent car. Null = pick from the AI vehicle pool / database.")]
        [SerializeField] private VehicleDefinition opponentVehicle;
        [SerializeField] private string opponentName = "";
        [Tooltip("Tournament round index for display (0 = not part of a tournament).")]
        [SerializeField] private int tournamentRound;

        public DragDistance Distance => distance;
        public float DistanceMeters => distance == DragDistance.HalfMile ? 804.672f : 402.336f;
        public VehicleDefinition OpponentVehicle => opponentVehicle;
        public string OpponentName => opponentName;
        public int TournamentRound => tournamentRound;

        public override RaceMode Mode => RaceMode.Drag;
        public override int OpponentCount => 1;
        public override string ModeLabel => distance == DragDistance.HalfMile ? "1/2 Mile Drag" : "1/4 Mile Drag";

#if UNITY_EDITOR
        public void EditorInitializeDrag(DragDistance newDistance, VehicleDefinition opponent, string name, int round)
        {
            distance = newDistance;
            opponentVehicle = opponent;
            opponentName = name;
            tournamentRound = round;
        }
#endif
    }
}
