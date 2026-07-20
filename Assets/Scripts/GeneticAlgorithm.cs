using System.Collections.Generic;
using UnityEngine;

/// <summary>初期集団、エリート保存、親選択、交叉、突然変異を担当します。</summary>
public sealed class GeneticAlgorithm
{
    private readonly EvolutionConfig config;

    public GeneticAlgorithm(EvolutionConfig config)
    {
        this.config = config;
    }

    public List<Genome> CreateInitialPopulation()
    {
        List<Genome> genomes = new List<Genome>(config.population.size);
        for (int i = 0; i < config.population.size; i++)
        {
            genomes.Add(Genome.CreateRandom(config.genome));
        }
        return genomes;
    }

    public List<Genome> CreateNextGeneration(List<SphereIndividual> rankedIndividuals)
    {
        int eliteCount = Mathf.Min(config.population.eliteCount, rankedIndividuals.Count);
        List<Genome> next = new List<Genome>(config.population.size);

        for (int i = 0; i < eliteCount; i++)
        {
            next.Add(rankedIndividuals[i].Genome.Clone());
        }

        while (next.Count < config.population.size)
        {
            int parentA = Random.Range(0, eliteCount);
            int parentB = Random.Range(0, eliteCount);
            while (eliteCount > 1 && parentB == parentA)
            {
                parentB = Random.Range(0, eliteCount);
            }

            Genome child = Genome.Crossover(
                rankedIndividuals[parentA].Genome,
                rankedIndividuals[parentB].Genome);
            child.Mutate(config.population.mutationChance, config.genome);
            next.Add(child);
        }

        return next;
    }
}
