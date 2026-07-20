using UnityEngine;

/// <summary>実験状態を画面左上へ表示します。</summary>
public sealed class EvolutionHud : MonoBehaviour
{
    public int Generation { get; set; }
    public SphereIndividual ReplayIndividual { get; set; }
    public float ChampionFitness { get; set; }

    private void OnGUI()
    {
        if (ReplayIndividual != null)
        {
            GUI.Box(new Rect(10f, 10f, 390f, 120f),
                $"GENERATION {Generation} CHAMPION REPLAY\n"
                + $"Fitness: {ChampionFitness:F2}\nParts: {ReplayIndividual.Genome.partCount}");
            return;
        }

        GUI.Box(new Rect(10f, 10f, 300f, 70f),
            $"FAST EVOLUTION\nGeneration {Generation}\nTime Scale: {Time.timeScale:0.#}x");
    }
}
