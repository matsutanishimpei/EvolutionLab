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
    [SerializeField, Min(0.1f)] private float evaluationDuration = 30f;

    [Header("Environment Settings")]
    [SerializeField] private Vector3 windDirection = Vector3.forward;
    [SerializeField, Min(0f)] private float windStrength = 10f;
    [SerializeField, Min(1f)] private float laneSpacing = 12f;
    [SerializeField, Min(0.1f)] private float startHeight = 2f;
    [SerializeField, Min(10f)] private float trackLength = 80f;

    [Header("Course Settings")]
    [SerializeField, Min(1)] private int obstacleRowCount = 3;
    [SerializeField, Min(4f)] private float laneWidth = 10f;
    [SerializeField, Min(2f)] private float obstacleOpeningWidth = 4f;
    [SerializeField, Min(1f)] private float obstacleHeight = 4f;
    [SerializeField, Min(1f)] private float laneWallHeight = 5f;
    [SerializeField, Min(1f)] private float maximumAllowedHeight = 3.5f;

    [Header("Simulation Speed")]
    [SerializeField, Range(1f, 100f)] private float fastForwardTimeScale = 30f;

    [Header("Champion Replay Recording")]
    [SerializeField, Min(1)] private int replayStartGeneration = 1;
    [SerializeField, Min(1)] private int replayInterval = 25;
    [SerializeField, Min(1f)] private float replayDuration = 50f;
    [SerializeField, Min(5f)] private float replayCameraDistance = 18f;
    [SerializeField, Min(5f)] private float replayCameraHeight = 14f;
    [SerializeField, Range(40f, 90f)] private float replayCameraFieldOfView = 70f;

    private readonly List<SphereIndividual> individuals = new List<SphereIndividual>();
    private readonly List<Renderer> courseRenderers = new List<Renderer>();
    private int generation = 1;
    private float generationStartTime;
    private string rankingDisplay = string.Empty;
    private Camera targetCamera;
    private Vector3 normalCameraPosition;
    private Quaternion normalCameraRotation;
    private float normalCameraFieldOfView;
    private Material groundMaterial;
    private Material obstacleMaterial;
    private SphereIndividual replayIndividual;
    private float replayStartTime;
    private float[] checkpointDownwindPositions;
    private float[] checkpointGapOffsets;

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
        CreateObstacleCourse();
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

        if (replayIndividual != null)
        {
            replayIndividual.Simulate(windDirection, windStrength, Time.time - replayStartTime);
        }
    }

    private void LateUpdate()
    {
        if (replayIndividual == null)
        {
            return;
        }

        Vector3 center = replayIndividual.GetCenterPosition();
        Vector3 horizontalWind = Vector3.ProjectOnPlane(windDirection, Vector3.up).normalized;
        targetCamera.transform.position = center
            - horizontalWind * replayCameraDistance
            + Vector3.up * replayCameraHeight;
        targetCamera.transform.LookAt(center + Vector3.up * 0.5f);
        targetCamera.fieldOfView = replayCameraFieldOfView;
    }

    /// <summary>一定時間の評価と世代交代を繰り返します。</summary>
    private IEnumerator EvolutionLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(evaluationDuration);

            EvaluateAndRank();

            if (ShouldRecordChampionReplay())
            {
                yield return StartCoroutine(RecordChampionReplay());
            }

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
            string line = $"{i + 1,3}. {individuals[i].name,-18} "
                + $"CP: {individuals[i].CheckpointsPassed}/{obstacleRowCount}  "
                + $"Speed: {individuals[i].SpeedScore:F1}  "
                + $"Fitness: {individuals[i].Fitness:F2}";
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
        Vector3 startLineCenter = -horizontalWind * (trackLength * 0.35f) + Vector3.up * startHeight;
        float laneCenterOffset = (genomes.Count - 1) * 0.5f;

        for (int i = 0; i < genomes.Count; i++)
        {
            // どの個体も、自分のレーン内では完全に同じ相対位置から開始します。
            Vector3 spawnPosition = startLineCenter
                + lineDirection * ((i - laneCenterOffset) * laneSpacing);

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = $"Gen{generation}_Sphere_{i + 1:000}";
            sphere.transform.position = spawnPosition;

            SphereIndividual individual = sphere.AddComponent<SphereIndividual>();
            // 同じ個体の全パーツには同じ明るい色を使用します。
            float hue = Mathf.Repeat((float)i / genomes.Count + generation * 0.07f, 1f);
            Color bodyColor = Color.HSVToRGB(hue, 0.75f, 0.95f);
            individual.Initialize(genomes[i], bodyColor);
            float laneCoordinate = (i - laneCenterOffset) * laneSpacing;
            individual.ConfigureCourse(
                horizontalWind,
                lineDirection,
                laneCoordinate,
                checkpointDownwindPositions,
                checkpointGapOffsets,
                obstacleOpeningWidth,
                maximumAllowedHeight,
                trackLength,
                evaluationDuration,
                trackLength * 0.5f);
            individuals.Add(individual);
        }

        // 通常世代は常に非表示・高速で進め、専用リプレイ時だけ描画します。
        foreach (SphereIndividual individual in individuals)
        {
            individual.SetVisible(false);
        }
        SetCourseVisible(false);
        Time.timeScale = fastForwardTimeScale;

        Debug.Log($"第{generation}世代を開始しました。個体数: {individuals.Count}, "
            + $"表示: OFF, 時間倍率: {Time.timeScale:0.#}x");
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
        float lineWidth = (populationSize - 1) * laneSpacing + laneWidth + 4f;
        float groundSize = Mathf.Max(lineWidth, trackLength + 20f);

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = Vector3.one * (groundSize / 10f);

        groundMaterial = CreateColoredMaterial("Ground Material", new Color(0.12f, 0.22f, 0.16f));
        Renderer groundRenderer = ground.GetComponent<Renderer>();
        groundRenderer.sharedMaterial = groundMaterial;
        courseRenderers.Add(groundRenderer);
    }

    /// <summary>各個体へ同じ形の独立レーンと障害物を作ります。</summary>
    private void CreateObstacleCourse()
    {
        Vector3 horizontalWind = Vector3.ProjectOnPlane(windDirection, Vector3.up).normalized;
        Vector3 lineDirection = Vector3.Cross(Vector3.up, horizontalWind).normalized;
        Quaternion obstacleRotation = Quaternion.LookRotation(horizontalWind, Vector3.up);
        obstacleMaterial = CreateColoredMaterial("Obstacle Material", new Color(0.9f, 0.32f, 0.08f));
        checkpointDownwindPositions = new float[obstacleRowCount];
        checkpointGapOffsets = new float[obstacleRowCount];

        // どのレーンでも全く同じ順路となるチェックポイント座標を先に作ります。
        for (int row = 0; row < obstacleRowCount; row++)
        {
            float rowProgress = (row + 1f) / (obstacleRowCount + 1f);
            checkpointDownwindPositions[row] = Mathf.Lerp(
                -trackLength * 0.15f,
                trackLength * 0.38f,
                rowProgress);
            float obstacleSide = row % 2 == 0 ? -1f : 1f;
            checkpointGapOffsets[row] = -obstacleSide
                * (laneWidth * 0.5f - obstacleOpeningWidth * 0.5f);
        }

        float laneCenterOffset = (populationSize - 1) * 0.5f;
        for (int lane = 0; lane < populationSize; lane++)
        {
            Vector3 laneCenter = lineDirection * ((lane - laneCenterOffset) * laneSpacing);

            // 両側の高い壁が、他レーンの個体との接触や進入を防ぎます。
            CreateCourseBox(
                laneCenter + lineDirection * (-laneWidth * 0.5f),
                obstacleRotation,
                0.35f, laneWallHeight, trackLength + 10f,
                $"Lane_{lane + 1:00}_LeftWall");
            CreateCourseBox(
                laneCenter + lineDirection * (laneWidth * 0.5f),
                obstacleRotation,
                0.35f, laneWallHeight, trackLength + 10f,
                $"Lane_{lane + 1:00}_RightWall");

            for (int row = 0; row < obstacleRowCount; row++)
            {
                float rowProgress = (row + 1f) / (obstacleRowCount + 1f);
                Vector3 rowCenter = laneCenter + horizontalWind
                    * Mathf.Lerp(-trackLength * 0.15f, trackLength * 0.38f, rowProgress);

                // 全レーンで同じ順番になるよう、障害物を左右交互に配置します。
                float obstacleWidth = laneWidth - obstacleOpeningWidth;
                float side = row % 2 == 0 ? -1f : 1f;
                Vector3 obstacleCenter = rowCenter
                    + lineDirection * side * (laneWidth - obstacleWidth) * 0.5f;
                CreateCourseBox(
                    obstacleCenter,
                    obstacleRotation,
                    obstacleWidth, obstacleHeight, 1.5f,
                    $"Lane_{lane + 1:00}_Obstacle_{row + 1:00}");
            }
        }
    }

    private void CreateCourseBox(
        Vector3 groundPosition,
        Quaternion rotation,
        float width,
        float height,
        float depth,
        string objectName)
    {
        GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstacle.name = objectName;
        obstacle.transform.rotation = rotation;
        obstacle.transform.position = groundPosition + Vector3.up * (height * 0.5f);
        obstacle.transform.localScale = new Vector3(width, height, depth);
        obstacle.AddComponent<EvolutionObstacle>();
        Renderer obstacleRenderer = obstacle.GetComponent<Renderer>();
        obstacleRenderer.sharedMaterial = obstacleMaterial;
        courseRenderers.Add(obstacleRenderer);
    }

    private void SetupCamera()
    {
        targetCamera = Camera.main;
        if (targetCamera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            targetCamera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        float lineWidth = (populationSize - 1) * laneSpacing + laneWidth + 4f;
        float viewSize = Mathf.Max(lineWidth, trackLength);
        targetCamera.transform.position = new Vector3(0f, viewSize * 0.75f, -trackLength * 0.45f);
        targetCamera.transform.LookAt(Vector3.zero);
        targetCamera.fieldOfView = 60f;
        targetCamera.farClipPlane = viewSize * 4f;
        normalCameraPosition = targetCamera.transform.position;
        normalCameraRotation = targetCamera.transform.rotation;
        normalCameraFieldOfView = targetCamera.fieldOfView;
    }

    private bool ShouldRecordChampionReplay()
    {
        return generation >= replayStartGeneration
            && (generation - replayStartGeneration) % replayInterval == 0;
    }

    /// <summary>
    /// 該当世代の最優秀Genomeを新しい個体として同じレーンで再走させ、
    /// 近距離追従カメラの専用動画へ保存します。
    /// </summary>
    private IEnumerator RecordChampionReplay()
    {
        Time.timeScale = 1f;
        SetCourseVisible(true);

        // 評価済み個体を隠し、リプレイ個体との物理的な干渉も無効にします。
        foreach (SphereIndividual individual in individuals)
        {
            individual.SetVisible(false);
            individual.SetCollisionEnabled(false);
        }

        Vector3 horizontalWind = Vector3.ProjectOnPlane(windDirection, Vector3.up).normalized;
        Vector3 lineDirection = Vector3.Cross(Vector3.up, horizontalWind).normalized;
        float laneCenterOffset = (populationSize - 1) * 0.5f;
        int replayLane = populationSize / 2;
        Vector3 replayPosition = -horizontalWind * (trackLength * 0.35f)
            + lineDirection * ((replayLane - laneCenterOffset) * laneSpacing)
            + Vector3.up * startHeight;

        GameObject replayObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        replayObject.name = $"Generation_{generation:0000}_ChampionReplay";
        replayObject.transform.position = replayPosition;
        replayIndividual = replayObject.AddComponent<SphereIndividual>();
        replayIndividual.Initialize(individuals[0].Genome, new Color(1f, 0.75f, 0.08f));
        float replayLaneCoordinate = (replayLane - laneCenterOffset) * laneSpacing;
        replayIndividual.ConfigureCourse(
            horizontalWind,
            lineDirection,
            replayLaneCoordinate,
            checkpointDownwindPositions,
            checkpointGapOffsets,
            obstacleOpeningWidth,
            maximumAllowedHeight,
            trackLength,
            evaluationDuration,
            trackLength * 0.5f);
        replayStartTime = Time.time;

        rankingDisplay = $"GENERATION {generation} CHAMPION REPLAY\n"
            + $"Fitness: {individuals[0].Fitness:F2} m\n"
            + $"Parts: {individuals[0].Genome.partCount}";
        EvolutionRecordingEvents.StartRecording($"ChampionReplay_{generation:0000}");

        float replayEndTime = Time.time + replayDuration;
        while (Time.time < replayEndTime && !replayIndividual.HasReachedFinish)
        {
            yield return null;
        }

        EvolutionRecordingEvents.StopRecording();
        replayIndividual.EvaluateAndStop(windDirection);
        Destroy(replayIndividual.gameObject);
        replayIndividual = null;
        targetCamera.transform.position = normalCameraPosition;
        targetCamera.transform.rotation = normalCameraRotation;
        targetCamera.fieldOfView = normalCameraFieldOfView;
    }

    private void SetCourseVisible(bool visible)
    {
        foreach (Renderer courseRenderer in courseRenderers)
        {
            if (courseRenderer != null)
            {
                courseRenderer.enabled = visible;
            }
        }
    }

    private static Material CreateColoredMaterial(string materialName, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        return new Material(shader)
        {
            name = materialName,
            color = color
        };
    }

    private void OnDestroy()
    {
        // 再生を停止したときにEditorの時間倍率を通常へ戻します。
        Time.timeScale = 1f;
        EvolutionRecordingEvents.StopRecording();

        if (groundMaterial != null)
        {
            Destroy(groundMaterial);
        }

        if (obstacleMaterial != null)
        {
            Destroy(obstacleMaterial);
        }
    }

    private void OnGUI()
    {
        if (replayIndividual != null && !string.IsNullOrEmpty(rankingDisplay))
        {
            GUI.Box(new Rect(10f, 10f, 390f, 120f), rankingDisplay);
        }
        else
        {
            GUI.Box(
                new Rect(10f, 10f, 300f, 70f),
                $"FAST EVOLUTION\nGeneration {generation}\nTime Scale: {Time.timeScale:0.#}x");
        }
    }
}
