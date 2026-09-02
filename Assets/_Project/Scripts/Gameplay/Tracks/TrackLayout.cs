using System;
using UnityEngine;

namespace RedlineLegends.Tracks
{
    /// <summary>
    /// Everything a race session needs from a track scene: ordered checkpoints, grid slots and the
    /// racing line. Adding a track means authoring one of these (the generator does it), never
    /// touching race code.
    /// </summary>
    public sealed class TrackLayout : MonoBehaviour
    {
        [SerializeField] private string trackId = "";
        [SerializeField] private Checkpoint[] checkpoints = Array.Empty<Checkpoint>();
        [SerializeField] private Transform[] gridSlots = Array.Empty<Transform>();
        [SerializeField] private RacingLine racingLine = new RacingLine();
        [SerializeField] private bool isLoop = true;
        [SerializeField] private Transform dragStart;
        [SerializeField] private float dragLaneSpacing = 5f;

        public string TrackId => trackId;
        public Checkpoint[] Checkpoints => checkpoints;
        public int CheckpointCount => checkpoints.Length;
        public RacingLine RacingLine => racingLine;
        public bool IsLoop => isLoop;
        public float LapLength => racingLine.TotalLength;
        public int GridSlotCount => gridSlots.Length;
        public Transform DragStart => dragStart;
        public float DragLaneSpacing => dragLaneSpacing;

        public Transform GetGridSlot(int slot)
        {
            if (gridSlots.Length == 0) return transform;
            return gridSlots[Mathf.Clamp(slot, 0, gridSlots.Length - 1)];
        }

        public Checkpoint GetCheckpoint(int index)
        {
            if (checkpoints.Length == 0) return null;
            int i = index % checkpoints.Length;
            if (i < 0) i += checkpoints.Length;
            return checkpoints[i];
        }

#if UNITY_EDITOR
        public void EditorInitialize(string id, Checkpoint[] cps, Transform[] grid, RacingLine line, bool loop, Transform drag, float laneSpacing)
        {
            trackId = id;
            checkpoints = cps;
            gridSlots = grid;
            racingLine = line;
            isLoop = loop;
            dragStart = drag;
            dragLaneSpacing = laneSpacing;
        }
#endif
    }
}
