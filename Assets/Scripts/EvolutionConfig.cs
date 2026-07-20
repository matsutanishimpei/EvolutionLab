using System;
using System.IO;
using UnityEngine;

/// <summary>進化実験の環境・評価・録画ルールをまとめた外部設定です。</summary>
[Serializable]
public sealed class EvolutionConfig
{
    public PopulationSettings population = new PopulationSettings();
    public EnvironmentSettings environment = new EnvironmentSettings();
    public CourseSettings course = new CourseSettings();
    public FitnessSettings fitness = new FitnessSettings();
    public ReplaySettings replay = new ReplaySettings();
    public GenomeSettings genome = new GenomeSettings();

    [Serializable]
    public sealed class PopulationSettings
    {
        public int size = 20;
        public int eliteCount = 4;
        public float mutationChance = 0.1f;
        public float evaluationSeconds = 30f;
        public float fastForwardTimeScale = 30f;
    }

    [Serializable]
    public sealed class EnvironmentSettings
    {
        public Vector3 windDirection = Vector3.forward;
        public float windStrength = 10f;
        public float laneSpacing = 12f;
        public float startHeight = 2f;
        public float trackLength = 80f;
    }

    [Serializable]
    public sealed class CourseSettings
    {
        public int obstacleCount = 3;
        public float laneWidth = 10f;
        public float openingWidth = 4f;
        public float obstacleHeight = 4f;
        public float laneWallHeight = 5f;
    }

    [Serializable]
    public sealed class FitnessSettings
    {
        public float checkpointReward = 100f;
        public float speedRewardPerCheckpoint = 25f;
        public float wallContactPenaltyPerSecond = 10f;
        public float maximumAllowedHeight = 3.5f;
        public float heightViolationBaseFitness = -1000f;
        public float heightViolationCheckpointReward = 10f;
    }

    [Serializable]
    public sealed class ReplaySettings
    {
        public int firstGeneration = 1;
        public int interval = 25;
        public float maxSeconds = 50f;
        public float cameraDistance = 18f;
        public float cameraHeight = 14f;
        public float cameraFieldOfView = 70f;
    }

    /// <summary>浮動小数点遺伝子の初期範囲、制約、突然変異幅です。</summary>
    [Serializable]
    public sealed class GeneRange
    {
        public float initialMin;
        public float initialMax;
        public float min;
        public float max;
        public float mutationAmount;

        public GeneRange(float initialMin, float initialMax, float min, float max, float mutationAmount)
        {
            this.initialMin = initialMin;
            this.initialMax = initialMax;
            this.min = min;
            this.max = max;
            this.mutationAmount = mutationAmount;
        }

        public float RandomValue() => UnityEngine.Random.Range(initialMin, initialMax);

        public float Mutate(float value, float chance)
        {
            return UnityEngine.Random.value < chance
                ? Mathf.Clamp(value + UnityEngine.Random.Range(-mutationAmount, mutationAmount), min, max)
                : value;
        }

        public void Validate()
        {
            if (min > max) (min, max) = (max, min);
            initialMin = Mathf.Clamp(initialMin, min, max);
            initialMax = Mathf.Clamp(initialMax, initialMin, max);
            mutationAmount = Mathf.Max(0f, mutationAmount);
        }
    }

    [Serializable]
    public sealed class GenomeSettings
    {
        public int minimumParts = 2;
        public int maximumParts = 4;
        public GeneRange partSize = new GeneRange(0.45f, 1.25f, 0.25f, 1.75f, 0.1f);
        public GeneRange connectionX = new GeneRange(-1f, 1f, -1f, 1f, 0.15f);
        public GeneRange connectionY = new GeneRange(-0.35f, 0.75f, -0.5f, 1f, 0.15f);
        public GeneRange connectionZ = new GeneRange(-1f, 1f, -1f, 1f, 0.15f);
        public GeneRange jointAmplitude = new GeneRange(30f, 160f, 0f, 240f, 15f);
        public GeneRange jointFrequency = new GeneRange(0.4f, 2f, 0.1f, 3f, 0.15f);
        public GeneRange jointPhase = new GeneRange(0f, 6.283185f, 0f, 6.283185f, 0.3f);
        public GeneRange mass = new GeneRange(0.5f, 3f, 0.1f, 5f, 0.2f);
        public GeneRange drag = new GeneRange(0f, 0.5f, 0f, 2f, 0.08f);
        public GeneRange angularDrag = new GeneRange(0.05f, 0.8f, 0f, 2f, 0.08f);
        public GeneRange friction = new GeneRange(0.1f, 1f, 0f, 1f, 0.1f);
        public GeneRange bounciness = new GeneRange(0f, 0.6f, 0f, 1f, 0.1f);

        public void Validate()
        {
            minimumParts = Mathf.Clamp(minimumParts, 2, Genome.MaxParts);
            maximumParts = Mathf.Clamp(maximumParts, minimumParts, Genome.MaxParts);
            foreach (GeneRange range in new[] { partSize, connectionX, connectionY, connectionZ,
                         jointAmplitude, jointFrequency, jointPhase, mass, drag, angularDrag,
                         friction, bounciness })
            {
                range.Validate();
            }
        }
    }
}

/// <summary>StreamingAssetsのJSONを既定値へ上書きして読み込みます。</summary>
public static class EvolutionConfigLoader
{
    public static EvolutionConfig Load()
    {
        EvolutionConfig config = new EvolutionConfig();
        string path = Path.Combine(Application.streamingAssetsPath, "evolution-config.json");
        if (!File.Exists(path))
        {
            Debug.LogWarning($"設定ファイルがないため既定値を使用します: {path}");
            return config;
        }

        try
        {
            JsonUtility.FromJsonOverwrite(File.ReadAllText(path), config);
            Validate(config);
            Debug.Log($"進化設定を読み込みました: {path}");
        }
        catch (Exception exception)
        {
            Debug.LogError($"設定ファイルを読めないため既定値を使用します: {exception.Message}");
            config = new EvolutionConfig();
        }
        return config;
    }

    private static void Validate(EvolutionConfig config)
    {
        config.population.size = Mathf.Max(2, config.population.size);
        config.population.eliteCount = Mathf.Clamp(config.population.eliteCount, 2, config.population.size);
        config.population.mutationChance = Mathf.Clamp01(config.population.mutationChance);
        config.population.evaluationSeconds = Mathf.Max(0.1f, config.population.evaluationSeconds);
        config.population.fastForwardTimeScale = Mathf.Clamp(config.population.fastForwardTimeScale, 1f, 100f);
        config.course.obstacleCount = Mathf.Max(1, config.course.obstacleCount);
        config.replay.interval = Mathf.Max(1, config.replay.interval);
        config.genome.Validate();
    }
}
