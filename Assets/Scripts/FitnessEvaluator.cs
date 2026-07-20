/// <summary>評価結果と設定から最終fitnessを計算する純粋なルールクラスです。</summary>
public static class FitnessEvaluator
{
    public static float Calculate(
        int checkpointsPassed,
        float downwindDistance,
        float speedScore,
        float obstacleContactTime,
        bool heightViolation,
        EvolutionConfig.FitnessSettings settings)
    {
        if (heightViolation)
        {
            return settings.heightViolationBaseFitness
                + checkpointsPassed * settings.heightViolationCheckpointReward;
        }

        return checkpointsPassed * settings.checkpointReward
            + downwindDistance
            + speedScore
            - obstacleContactTime * settings.wallContactPenaltyPerSecond;
    }
}
