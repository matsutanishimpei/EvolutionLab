using UnityEngine;

/// <summary>Genomeから個体を生成し、共通のコース条件を設定します。</summary>
public sealed class IndividualFactory
{
    private readonly EvolutionConfig config;
    private readonly CourseLayout layout;

    public IndividualFactory(EvolutionConfig config, CourseLayout layout)
    {
        this.config = config;
        this.layout = layout;
    }

    public SphereIndividual Create(Genome genome, int index, int generation, Color color,
        string name = null)
    {
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        root.name = name ?? $"Gen{generation}_Sphere_{index + 1:000}";
        root.transform.position = layout.StartPosition(index, config);

        SphereIndividual individual = root.AddComponent<SphereIndividual>();
        individual.Initialize(genome, color);
        individual.ConfigureCourse(
            layout.Downwind,
            layout.Lateral,
            layout.LaneCoordinate(index, config.population.size, config.environment.laneSpacing),
            layout.CheckpointPositions,
            layout.GapOffsets,
            config.course.openingWidth,
            config.fitness.maximumAllowedHeight,
            config.environment.trackLength,
            config.population.evaluationSeconds,
            config.environment.trackLength * 0.5f,
            config.fitness);
        return individual;
    }
}
