using System.Collections.Generic;
using UnityEngine;

/// <summary>チャンピオン再走用個体、カメラ、録画イベントをまとめて管理します。</summary>
public sealed class ChampionReplayController
{
    private readonly EvolutionConfig config;
    private readonly CourseLayout layout;
    private readonly IndividualFactory factory;
    private readonly EvolutionCameraController cameraController;

    public SphereIndividual Individual { get; private set; }
    public bool IsRunning => Individual != null;
    public float StartedAt { get; private set; }

    public ChampionReplayController(EvolutionConfig config, CourseLayout layout,
        IndividualFactory factory, EvolutionCameraController cameraController)
    {
        this.config = config;
        this.layout = layout;
        this.factory = factory;
        this.cameraController = cameraController;
    }

    public bool ShouldRun(int generation)
        => generation >= config.replay.firstGeneration
           && (generation - config.replay.firstGeneration) % config.replay.interval == 0;

    public void Begin(int generation, SphereIndividual champion,
        IReadOnlyList<SphereIndividual> evaluatedIndividuals)
    {
        Time.timeScale = 1f;
        foreach (SphereIndividual individual in evaluatedIndividuals)
        {
            individual.SetVisible(false);
            individual.SetCollisionEnabled(false);
        }

        int lane = config.population.size / 2;
        Individual = factory.Create(champion.Genome, lane, generation,
            new Color(1f, 0.75f, 0.08f), $"Generation_{generation:0000}_ChampionReplay");
        StartedAt = Time.time;
        EvolutionRecordingEvents.StartRecording($"ChampionReplay_{generation:0000}");
    }

    public void Simulate()
    {
        if (Individual != null)
            Individual.Simulate(layout.Downwind, config.environment.windStrength, Time.time - StartedAt);
    }

    public void UpdateCamera()
    {
        if (Individual != null)
            cameraController.Follow(Individual, layout.Downwind, config.replay);
    }

    public void End()
    {
        EvolutionRecordingEvents.StopRecording();
        if (Individual != null) Object.Destroy(Individual.gameObject);
        Individual = null;
        cameraController.Restore();
    }
}
