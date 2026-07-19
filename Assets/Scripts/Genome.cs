using System;
using UnityEngine;

/// <summary>
/// 2～4個のSphereでできた生物の体型と動作パターンを保持します。
/// 配列の0番は中心パーツ、1番以降は中心へ接続する可動パーツです。
/// </summary>
[Serializable]
public class Genome
{
    public const int MaxParts = 4;

    public int partCount;
    public float[] partSizes = new float[MaxParts];
    public float[] connectionX = new float[MaxParts];
    public float[] connectionY = new float[MaxParts];
    public float[] connectionZ = new float[MaxParts];
    public float[] jointAmplitude = new float[MaxParts];
    public float[] jointFrequency = new float[MaxParts];
    public float[] jointPhase = new float[MaxParts];

    public float mass;
    public float drag;
    public float angularDrag;
    public float friction;
    public float bounciness;

    public static Genome CreateRandom()
    {
        Genome genome = new Genome
        {
            partCount = UnityEngine.Random.Range(2, MaxParts + 1),
            mass = UnityEngine.Random.Range(0.5f, 3f),
            drag = UnityEngine.Random.Range(0f, 0.5f),
            angularDrag = UnityEngine.Random.Range(0.05f, 0.8f),
            friction = UnityEngine.Random.Range(0.1f, 1f),
            bounciness = UnityEngine.Random.Range(0f, 0.6f)
        };

        for (int i = 0; i < MaxParts; i++)
        {
            genome.partSizes[i] = UnityEngine.Random.Range(0.45f, 1.25f);
            genome.connectionX[i] = UnityEngine.Random.Range(-1f, 1f);
            genome.connectionY[i] = UnityEngine.Random.Range(-0.35f, 0.75f);
            genome.connectionZ[i] = UnityEngine.Random.Range(-1f, 1f);
            genome.jointAmplitude[i] = UnityEngine.Random.Range(30f, 160f);
            genome.jointFrequency[i] = UnityEngine.Random.Range(0.4f, 2f);
            genome.jointPhase[i] = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        }

        return genome;
    }

    /// <summary>すべての遺伝子について、親Aか親Bを50%で選びます。</summary>
    public static Genome Crossover(Genome parentA, Genome parentB)
    {
        Genome child = new Genome
        {
            partCount = UnityEngine.Random.value < 0.5f ? parentA.partCount : parentB.partCount,
            mass = Choose(parentA.mass, parentB.mass),
            drag = Choose(parentA.drag, parentB.drag),
            angularDrag = Choose(parentA.angularDrag, parentB.angularDrag),
            friction = Choose(parentA.friction, parentB.friction),
            bounciness = Choose(parentA.bounciness, parentB.bounciness)
        };

        for (int i = 0; i < MaxParts; i++)
        {
            child.partSizes[i] = Choose(parentA.partSizes[i], parentB.partSizes[i]);
            child.connectionX[i] = Choose(parentA.connectionX[i], parentB.connectionX[i]);
            child.connectionY[i] = Choose(parentA.connectionY[i], parentB.connectionY[i]);
            child.connectionZ[i] = Choose(parentA.connectionZ[i], parentB.connectionZ[i]);
            child.jointAmplitude[i] = Choose(parentA.jointAmplitude[i], parentB.jointAmplitude[i]);
            child.jointFrequency[i] = Choose(parentA.jointFrequency[i], parentB.jointFrequency[i]);
            child.jointPhase[i] = Choose(parentA.jointPhase[i], parentB.jointPhase[i]);
        }

        return child;
    }

    /// <summary>各遺伝子へ個別に指定確率で小さな突然変異を加えます。</summary>
    public void Mutate(float chance)
    {
        if (UnityEngine.Random.value < chance)
            partCount = Mathf.Clamp(partCount + (UnityEngine.Random.value < 0.5f ? -1 : 1), 2, MaxParts);

        mass = MutateValue(mass, chance, 0.2f, 0.1f, 5f);
        drag = MutateValue(drag, chance, 0.08f, 0f, 2f);
        angularDrag = MutateValue(angularDrag, chance, 0.08f, 0f, 2f);
        friction = MutateValue(friction, chance, 0.1f, 0f, 1f);
        bounciness = MutateValue(bounciness, chance, 0.1f, 0f, 1f);

        for (int i = 0; i < MaxParts; i++)
        {
            partSizes[i] = MutateValue(partSizes[i], chance, 0.1f, 0.25f, 1.75f);
            connectionX[i] = MutateValue(connectionX[i], chance, 0.15f, -1f, 1f);
            connectionY[i] = MutateValue(connectionY[i], chance, 0.15f, -0.5f, 1f);
            connectionZ[i] = MutateValue(connectionZ[i], chance, 0.15f, -1f, 1f);
            jointAmplitude[i] = MutateValue(jointAmplitude[i], chance, 15f, 0f, 240f);
            jointFrequency[i] = MutateValue(jointFrequency[i], chance, 0.15f, 0.1f, 3f);
            jointPhase[i] = MutateValue(jointPhase[i], chance, 0.3f, 0f, Mathf.PI * 2f);
        }
    }

    public Genome Clone()
    {
        Genome clone = new Genome
        {
            partCount = partCount,
            mass = mass,
            drag = drag,
            angularDrag = angularDrag,
            friction = friction,
            bounciness = bounciness
        };

        Array.Copy(partSizes, clone.partSizes, MaxParts);
        Array.Copy(connectionX, clone.connectionX, MaxParts);
        Array.Copy(connectionY, clone.connectionY, MaxParts);
        Array.Copy(connectionZ, clone.connectionZ, MaxParts);
        Array.Copy(jointAmplitude, clone.jointAmplitude, MaxParts);
        Array.Copy(jointFrequency, clone.jointFrequency, MaxParts);
        Array.Copy(jointPhase, clone.jointPhase, MaxParts);
        return clone;
    }

    private static float Choose(float a, float b) => UnityEngine.Random.value < 0.5f ? a : b;

    private static float MutateValue(float value, float chance, float amount, float min, float max)
    {
        return UnityEngine.Random.value < chance
            ? Mathf.Clamp(value + UnityEngine.Random.Range(-amount, amount), min, max)
            : value;
    }
}
