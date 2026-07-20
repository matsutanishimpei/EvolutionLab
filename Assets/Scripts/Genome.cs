using System;
using UnityEngine;

/// <summary>個体の形状、関節運動、物理特性を保持する遺伝情報です。</summary>
[Serializable]
public sealed class Genome
{
    // 現在は最大5部品。JSONのmaximumPartsを5にすればコード変更なしで利用できます。
    public const int MaxParts = 5;

    [Serializable]
    public sealed class PartGene
    {
        public float size;
        public Vector3 connection;
        public float jointAmplitude;
        public float jointFrequency;
        public float jointPhase;

        public PartGene Clone() => (PartGene)MemberwiseClone();
    }

    public int partCount;
    public PartGene[] parts = new PartGene[MaxParts];
    public float mass;
    public float drag;
    public float angularDrag;
    public float friction;
    public float bounciness;

    public static Genome CreateRandom(EvolutionConfig.GenomeSettings settings)
    {
        Genome genome = new Genome
        {
            partCount = UnityEngine.Random.Range(settings.minimumParts, settings.maximumParts + 1),
            mass = settings.mass.RandomValue(),
            drag = settings.drag.RandomValue(),
            angularDrag = settings.angularDrag.RandomValue(),
            friction = settings.friction.RandomValue(),
            bounciness = settings.bounciness.RandomValue()
        };

        for (int i = 0; i < MaxParts; i++)
        {
            genome.parts[i] = new PartGene
            {
                size = settings.partSize.RandomValue(),
                connection = new Vector3(
                    settings.connectionX.RandomValue(),
                    settings.connectionY.RandomValue(),
                    settings.connectionZ.RandomValue()),
                jointAmplitude = settings.jointAmplitude.RandomValue(),
                jointFrequency = settings.jointFrequency.RandomValue(),
                jointPhase = settings.jointPhase.RandomValue()
            };
        }

        return genome;
    }

    /// <summary>各遺伝子を親Aまたは親Bから50%の確率で継承します。</summary>
    public static Genome Crossover(Genome parentA, Genome parentB)
    {
        Genome child = new Genome
        {
            partCount = Choose(parentA.partCount, parentB.partCount),
            mass = Choose(parentA.mass, parentB.mass),
            drag = Choose(parentA.drag, parentB.drag),
            angularDrag = Choose(parentA.angularDrag, parentB.angularDrag),
            friction = Choose(parentA.friction, parentB.friction),
            bounciness = Choose(parentA.bounciness, parentB.bounciness)
        };

        for (int i = 0; i < MaxParts; i++)
        {
            PartGene a = parentA.parts[i];
            PartGene b = parentB.parts[i];
            child.parts[i] = new PartGene
            {
                size = Choose(a.size, b.size),
                connection = new Vector3(
                    Choose(a.connection.x, b.connection.x),
                    Choose(a.connection.y, b.connection.y),
                    Choose(a.connection.z, b.connection.z)),
                jointAmplitude = Choose(a.jointAmplitude, b.jointAmplitude),
                jointFrequency = Choose(a.jointFrequency, b.jointFrequency),
                jointPhase = Choose(a.jointPhase, b.jointPhase)
            };
        }

        return child;
    }

    public void Mutate(float chance, EvolutionConfig.GenomeSettings settings)
    {
        if (UnityEngine.Random.value < chance)
        {
            partCount = Mathf.Clamp(partCount + (UnityEngine.Random.value < 0.5f ? -1 : 1),
                settings.minimumParts, settings.maximumParts);
        }

        mass = settings.mass.Mutate(mass, chance);
        drag = settings.drag.Mutate(drag, chance);
        angularDrag = settings.angularDrag.Mutate(angularDrag, chance);
        friction = settings.friction.Mutate(friction, chance);
        bounciness = settings.bounciness.Mutate(bounciness, chance);

        foreach (PartGene part in parts)
        {
            part.size = settings.partSize.Mutate(part.size, chance);
            part.connection = new Vector3(
                settings.connectionX.Mutate(part.connection.x, chance),
                settings.connectionY.Mutate(part.connection.y, chance),
                settings.connectionZ.Mutate(part.connection.z, chance));
            part.jointAmplitude = settings.jointAmplitude.Mutate(part.jointAmplitude, chance);
            part.jointFrequency = settings.jointFrequency.Mutate(part.jointFrequency, chance);
            part.jointPhase = settings.jointPhase.Mutate(part.jointPhase, chance);
        }
    }

    public Genome Clone()
    {
        Genome clone = (Genome)MemberwiseClone();
        clone.parts = new PartGene[MaxParts];
        for (int i = 0; i < MaxParts; i++)
        {
            clone.parts[i] = parts[i].Clone();
        }
        return clone;
    }

    private static float Choose(float a, float b) => UnityEngine.Random.value < 0.5f ? a : b;
    private static int Choose(int a, int b) => UnityEngine.Random.value < 0.5f ? a : b;
}
