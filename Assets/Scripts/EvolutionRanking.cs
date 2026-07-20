using System.Collections.Generic;
using System.Text;

/// <summary>個体の順位付けと表示文字列の作成を担当します。</summary>
public static class EvolutionRanking
{
    public static string EvaluateAndSort(List<SphereIndividual> individuals, int generation,
        EvolutionConfig config)
    {
        foreach (SphereIndividual individual in individuals)
            individual.EvaluateAndStop(config.environment.windDirection);

        individuals.Sort((a, b) => b.Fitness.CompareTo(a.Fitness));
        StringBuilder log = new StringBuilder($"=== Generation {generation} Fitness Ranking ===\n");
        for (int i = 0; i < individuals.Count; i++)
        {
            log.AppendLine($"{i + 1,3}. {individuals[i].name,-18} "
                + $"CP: {individuals[i].CheckpointsPassed}/{config.course.obstacleCount}  "
                + $"Speed: {individuals[i].SpeedScore:F1}  Fitness: {individuals[i].Fitness:F2}");
        }
        return log.ToString();
    }
}
