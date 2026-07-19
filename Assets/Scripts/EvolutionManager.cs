using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 共通の実験環境と、選択・交叉・突然変異・世代更新を管理します。
/// </summary>
public class EvolutionManager : MonoBehaviour
{
    [Header("Evolution Settings")]
    [SerializeField, Min(2)] private int populationSize = 20;
    [SerializeField, Min(2)] private int eliteCount = 4;
    [SerializeField, Range(0f, 1f)] private float mutationChance = 0.1f;
    [SerializeField, Min(0.1f)] private float evaluationDuration = 10f;

    [Header("Environment Settings")]
    [SerializeField] private Vector3 windDirection = Vector3.forward;
    [SerializeField, Min(0f)] private float windStrength = 10f;
    [SerializeField, Min(0.1f)] private float startSpacing = 7f;
    [SerializeField, Min(0.1f)] private float startHeight = 2f;
    [SerializeField, Min(10f)] private float trackLength = 80f;

    private readonly List<SphereIndividual> individuals = new List<SphereIndividual>();
    private int generation = 1;
    private float generationStartTime;
    private string rankingDisplay = string.Empty;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateManagerIfNeeded()
    {
        if (FindAnyObjectByType<EvolutionManager>() == null)
        {
            new GameObject("Evolution Manager").AddComponent<EvolutionManager>();
        }
    }

    private void Start()
    {
        // 水平成分がない風向きは実験に使えないため、前方向へ補正します。
        if (Vector3.ProjectOnPlane(windDirection, Vector3.up).sqrMagnitude < 0.0001f)
        {
            windDirection = Vector3.forward;
        }

        CreateGround();
        SetupCamera();

        List<Genome> firstGeneration = new List<Genome>();
        for (int i = 0; i < populationSize; i++)
        {
            firstGeneration.Add(Genome.CreateRandom());
        }

        SpawnGeneration(firstGeneration);
        StartCoroutine(EvolutionLoop());
    }

    /// <summary>
    /// Unityの物理更新ごとに、同じ風向きと強さを全個体へ適用します。
    /// 実際の力は個体の投影面積によって変わります。
    /// </summary>
    private void FixedUpdate()
    {
        foreach (SphereIndividual individual in individuals)
        {
            individual.Simulate(windDirection, windStrength, Time.time - generationStartTime);
        }
    }

    /// <summary>一定時間の評価と世代交代を繰り返します。</summary>
    private IEnumerator EvolutionLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(evaluationDuration);

            EvaluateAndRank();
            List<Genome> nextGeneration = CreateNextGeneration();
            DestroyCurrentGeneration();

            // Destroyの完了を待ち、旧世代と新世代の衝突を防ぎます。
            yield return null;

            generation++;
            SpawnGeneration(nextGeneration);
        }
    }

    private void EvaluateAndRank()
    {
        foreach (SphereIndividual individual in individuals)
        {
            individual.EvaluateAndStop(windDirection);
        }

        individuals.Sort((a, b) => b.Fitness.CompareTo(a.Fitness));

        StringBuilder consoleRanking = new StringBuilder();
        StringBuilder screenRanking = new StringBuilder();
        consoleRanking.AppendLine($"=== Generation {generation} Fitness Ranking ===");
        screenRanking.AppendLine($"GENERATION {generation} - TOP {eliteCount}");

        for (int i = 0; i < individuals.Count; i++)
        {
            string line = $"{i + 1,3}. {individuals[i].name,-18} Fitness: {individuals[i].Fitness:F2} m";
            consoleRanking.AppendLine(line);

            if (i < eliteCount)
            {
                screenRanking.AppendLine(line);
            }
        }

        rankingDisplay = screenRanking.ToString();
        Debug.Log(consoleRanking.ToString());
    }

    /// <summary>上位エリートと、その交叉・突然変異による子を作ります。</summary>
    private List<Genome> CreateNextGeneration()
    {
        int actualEliteCount = Mathf.Min(eliteCount, individuals.Count);
        List<Genome> nextGeneration = new List<Genome>(populationSize);

        for (int i = 0; i < actualEliteCount; i++)
        {
            nextGeneration.Add(individuals[i].Genome.Clone());
        }

        while (nextGeneration.Count < populationSize)
        {
            int parentAIndex = Random.Range(0, actualEliteCount);
            int parentBIndex = Random.Range(0, actualEliteCount);
            while (actualEliteCount > 1 && parentBIndex == parentAIndex)
            {
                parentBIndex = Random.Range(0, actualEliteCount);
            }

            Genome child = Genome.Crossover(
                individuals[parentAIndex].Genome,
                individuals[parentBIndex].Genome);
            child.Mutate(mutationChance);
            nextGeneration.Add(child);
        }

        return nextGeneration;
    }

    /// <summary>全個体を風向きに直交するスタートラインへ等間隔で並べます。</summary>
    private void SpawnGeneration(List<Genome> genomes)
    {
        individuals.Clear();
        generationStartTime = Time.time;

        Vector3 horizontalWind = Vector3.ProjectOnPlane(windDirection, Vector3.up).normalized;
        Vector3 lineDirection = Vector3.Cross(Vector3.up, horizontalWind).normalized;
        Vector3 lineCenter = -horizontalWind * (trackLength * 0.35f) + Vector3.up * startHeight;
        float lineCenterOffset = (genomes.Count - 1) * 0.5f;

        for (int i = 0; i < genomes.Count; i++)
        {
            Vector3 spawnPosition = lineCenter
                + lineDirection * ((i - lineCenterOffset) * startSpacing);

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = $"Gen{generation}_Sphere_{i + 1:000}";
            sphere.transform.position = spawnPosition;

            SphereIndividual individual = sphere.AddComponent<SphereIndividual>();
            individual.Initialize(genomes[i]);
            individuals.Add(individual);
        }

        Debug.Log($"第{generation}世代を開始しました。個体数: {individuals.Count}");
    }

    private void DestroyCurrentGeneration()
    {
        foreach (SphereIndividual individual in individuals)
        {
            Destroy(individual.gameObject);
        }

        individuals.Clear();
    }

    private void CreateGround()
    {
        float lineWidth = (populationSize - 1) * startSpacing + 10f;
        float groundSize = Mathf.Max(lineWidth, trackLength + 20f);

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = Vector3.one * (groundSize / 10f);
    }

    private void SetupCamera()
    {
        Camera targetCamera = Camera.main;
        if (targetCamera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            targetCamera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        float lineWidth = (populationSize - 1) * startSpacing + 10f;
        float viewSize = Mathf.Max(lineWidth, trackLength);
        targetCamera.transform.position = new Vector3(0f, viewSize * 0.75f, -trackLength * 0.45f);
        targetCamera.transform.LookAt(Vector3.zero);
        targetCamera.fieldOfView = 60f;
        targetCamera.farClipPlane = viewSize * 4f;
    }

    private void OnGUI()
    {
        if (!string.IsNullOrEmpty(rankingDisplay))
        {
            GUI.Box(new Rect(10f, 10f, 390f, 465f), rankingDisplay);
        }
    }
}
