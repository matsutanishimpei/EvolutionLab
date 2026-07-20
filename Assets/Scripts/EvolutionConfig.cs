using System;
using System.IO;
using UnityEngine;

/// <summary>進化実験の環境・評価・録画ルールをまとめた外部設定です。</summary>
[Serializable]
public class EvolutionConfig
{
    public PopulationSettings population = new PopulationSettings();
    public EnvironmentSettings environment = new EnvironmentSettings();
    public CourseSettings course = new CourseSettings();
    public FitnessSettings fitness = new FitnessSettings();
    public ReplaySettings replay = new ReplaySettings();
    public GenomeSettings genome = new GenomeSettings();

    [Serializable]
    public class PopulationSettings
    {
        public int size = 20;
        public int eliteCount = 4;
        public float mutationChance = 0.1f;
        public float evaluationSeconds = 30f;
        public float fastForwardTimeScale = 30f;
    }

    [Serializable]
    public class EnvironmentSettings
    {
        public Vector3 windDirection = Vector3.forward;
        public float windStrength = 10f;
        public float laneSpacing = 12f;
        public float startHeight = 2f;
        public float trackLength = 80f;
    }

    [Serializable]
    public class CourseSettings
    {
        public int obstacleCount = 3;
        public float laneWidth = 10f;
        public float openingWidth = 4f;
        public float obstacleHeight = 4f;
        public float laneWallHeight = 5f;
    }

    [Serializable]
    public class FitnessSettings
    {
        public float checkpointReward = 100f;
        public float speedRewardPerCheckpoint = 25f;
        public float wallContactPenaltyPerSecond = 10f;
        public float maximumAllowedHeight = 3.5f;
        public float heightViolationBaseFitness = -1000f;
        public float heightViolationCheckpointReward = 10f;
    }

    [Serializable]
    public class ReplaySettings
    {
        public int firstGeneration = 1;
        public int interval = 25;
        public float maxSeconds = 50f;
        public float cameraDistance = 18f;
        public float cameraHeight = 14f;
        public float cameraFieldOfView = 70f;
    }

    [Serializable]
    public class GenomeSettings
    {
        public int minimumParts = 2;
        public int maximumParts = 4;
        public float initialMinimumPartSize = 0.45f;
        public float initialMaximumPartSize = 1.25f;
        public float minimumPartSize = 0.25f;
        public float maximumPartSize = 1.75f;
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
            Debug.LogWarning($"設定ファイルがないため既定値を使います: {path}");
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
            Debug.LogError($"設定ファイルを読めないため既定値を使います: {exception.Message}");
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
        config.genome.minimumParts = Mathf.Clamp(config.genome.minimumParts, 2, Genome.MaxParts);
        config.genome.maximumParts = Mathf.Clamp(
            config.genome.maximumParts,
            config.genome.minimumParts,
            Genome.MaxParts);
    }
}
