using UnityEngine;

/// <summary>生成済みコースの座標系とチェックポイント情報を表す不変データです。</summary>
public sealed class CourseLayout
{
    public Vector3 Downwind { get; }
    public Vector3 Lateral { get; }
    public float[] CheckpointPositions { get; }
    public float[] GapOffsets { get; }

    public CourseLayout(Vector3 downwind, Vector3 lateral, float[] positions, float[] offsets)
    {
        Downwind = downwind;
        Lateral = lateral;
        CheckpointPositions = positions;
        GapOffsets = offsets;
    }

    public float LaneCoordinate(int index, int populationSize, float spacing)
        => (index - (populationSize - 1) * 0.5f) * spacing;

    public Vector3 StartPosition(int index, EvolutionConfig config)
        => -Downwind * (config.environment.trackLength * 0.35f)
           + Lateral * LaneCoordinate(index, config.population.size, config.environment.laneSpacing)
           + Vector3.up * config.environment.startHeight;
}
