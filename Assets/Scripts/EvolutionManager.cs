using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>各サービスを組み立て、世代の開始・評価・更新だけを進行します。</summary>
public sealed class EvolutionManager : MonoBehaviour
{
    private readonly List<SphereIndividual> individuals = new List<SphereIndividual>();
    private EvolutionConfig config;
    private GeneticAlgorithm geneticAlgorithm;
    private CourseBuilder courseBuilder;
    private CourseLayout courseLayout;
    private IndividualFactory individualFactory;
    private ChampionReplayController replayController;
    private EvolutionHud hud;
    private int generation = 1;
    private float generationStartedAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateManagerIfNeeded()
    {
        if (FindAnyObjectByType<EvolutionManager>() == null)
            new GameObject("Evolution Manager").AddComponent<EvolutionManager>();
    }

    private void Start()
    {
        config = EvolutionConfigLoader.Load();
        NormalizeWindDirection();
        geneticAlgorithm = new GeneticAlgorithm(config);
        courseBuilder = new CourseBuilder(config);
        courseLayout = courseBuilder.Build();
        individualFactory = new IndividualFactory(config, courseLayout);
        EvolutionCameraController cameraController = new EvolutionCameraController(config);
        replayController = new ChampionReplayController(config, courseLayout, individualFactory,
            cameraController);
        hud = gameObject.AddComponent<EvolutionHud>();
        hud.Generation = generation;

        SpawnGeneration(geneticAlgorithm.CreateInitialPopulation());
        StartCoroutine(EvolutionLoop());
    }

    private void FixedUpdate()
    {
        float elapsed = Time.time - generationStartedAt;
        foreach (SphereIndividual individual in individuals)
            individual.Simulate(courseLayout.Downwind, config.environment.windStrength, elapsed);
        replayController?.Simulate();
    }

    private void LateUpdate() => replayController?.UpdateCamera();

    private IEnumerator EvolutionLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(config.population.evaluationSeconds);
            string ranking = EvolutionRanking.EvaluateAndSort(individuals, generation, config);
            Debug.Log(ranking);

            if (replayController.ShouldRun(generation))
                yield return RunChampionReplay();

            List<Genome> nextGeneration = geneticAlgorithm.CreateNextGeneration(individuals);
            DestroyCurrentGeneration();
            yield return null; // Destroyの完了を待ち、世代間の衝突を防ぎます。
            generation++;
            hud.Generation = generation;
            SpawnGeneration(nextGeneration);
        }
    }

    private IEnumerator RunChampionReplay()
    {
        courseBuilder.SetVisible(true);
        hud.ChampionFitness = individuals[0].Fitness;
        replayController.Begin(generation, individuals[0], individuals);
        hud.ReplayIndividual = replayController.Individual;

        float endTime = Time.time + config.replay.maxSeconds;
        while (Time.time < endTime && !replayController.Individual.HasReachedFinish)
            yield return null;

        replayController.End();
        hud.ReplayIndividual = null;
    }

    private void SpawnGeneration(IReadOnlyList<Genome> genomes)
    {
        individuals.Clear();
        generationStartedAt = Time.time;
        for (int i = 0; i < genomes.Count; i++)
        {
            float hue = Mathf.Repeat((float)i / genomes.Count + generation * 0.07f, 1f);
            SphereIndividual individual = individualFactory.Create(genomes[i], i, generation,
                Color.HSVToRGB(hue, 0.75f, 0.95f));
            individual.SetVisible(false);
            individuals.Add(individual);
        }
        courseBuilder.SetVisible(false);
        Time.timeScale = config.population.fastForwardTimeScale;
        Debug.Log($"第{generation}世代を開始: {individuals.Count}個体、{Time.timeScale:0.#}倍速");
    }

    private void DestroyCurrentGeneration()
    {
        foreach (SphereIndividual individual in individuals)
            if (individual != null) Destroy(individual.gameObject);
        individuals.Clear();
    }

    private void NormalizeWindDirection()
    {
        Vector3 horizontal = Vector3.ProjectOnPlane(config.environment.windDirection, Vector3.up);
        config.environment.windDirection = horizontal.sqrMagnitude < 0.0001f
            ? Vector3.forward
            : horizontal.normalized;
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        replayController?.End();
        courseBuilder?.Dispose();
    }
}
