using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 複数のSphereとHingeJointで構成される1個体です。
/// 身体の生成、周期運動、風の受け取り、fitness計測を担当します。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class SphereIndividual : MonoBehaviour
{
    public Genome Genome { get; private set; }
    public float Fitness { get; private set; }

    private readonly List<Rigidbody> partBodies = new List<Rigidbody>();
    private readonly List<HingeJoint> movingJoints = new List<HingeJoint>();
    private readonly List<GameObject> childParts = new List<GameObject>();
    private Vector3 startCenter;
    private PhysicsMaterial individualMaterial;

    public void Initialize(Genome genome)
    {
        Genome = genome.Clone();
        Fitness = 0f;

        individualMaterial = new PhysicsMaterial($"{name}_Material")
        {
            staticFriction = Genome.friction,
            dynamicFriction = Genome.friction,
            bounciness = Genome.bounciness,
            frictionCombine = PhysicsMaterialCombine.Average,
            bounceCombine = PhysicsMaterialCombine.Average
        };

        Rigidbody rootBody = GetComponent<Rigidbody>();
        ConfigurePart(gameObject, rootBody, Genome.partSizes[0]);
        partBodies.Add(rootBody);

        for (int i = 1; i < Genome.partCount; i++)
        {
            CreateConnectedPart(i, rootBody);
        }

        startCenter = CalculateCenter();
    }

    private void CreateConnectedPart(int index, Rigidbody rootBody)
    {
        Vector3 direction = new Vector3(
            Genome.connectionX[index],
            Genome.connectionY[index],
            Genome.connectionZ[index]).normalized;

        if (direction.sqrMagnitude < 0.001f)
        {
            direction = Vector3.right;
        }

        float rootRadius = Genome.partSizes[0] * 0.5f;
        float childRadius = Genome.partSizes[index] * 0.5f;

        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        part.name = $"{name}_Part_{index + 1}";
        part.transform.position = transform.position + direction * (rootRadius + childRadius);
        childParts.Add(part);

        Rigidbody body = part.AddComponent<Rigidbody>();
        ConfigurePart(part, body, Genome.partSizes[index]);
        partBodies.Add(body);

        HingeJoint joint = part.AddComponent<HingeJoint>();
        joint.connectedBody = rootBody;
        joint.autoConfigureConnectedAnchor = false;
        joint.anchor = -direction * childRadius;
        joint.connectedAnchor = direction * rootRadius;
        joint.axis = GetJointAxis(direction);
        joint.enableCollision = false;
        joint.useMotor = true;

        JointMotor motor = joint.motor;
        motor.force = 80f;
        motor.freeSpin = false;
        joint.motor = motor;
        movingJoints.Add(joint);
    }

    private void ConfigurePart(GameObject part, Rigidbody body, float size)
    {
        part.transform.localScale = Vector3.one * size;
        body.mass = Genome.mass / Genome.partCount;
        body.linearDamping = Genome.drag;
        body.angularDamping = Genome.angularDrag;
        part.GetComponent<Collider>().material = individualMaterial;
    }

    /// <summary>物理更新ごとに関節モーターと、面積に比例する風を適用します。</summary>
    public void Simulate(Vector3 windDirection, float windStrength, float elapsedTime)
    {
        for (int i = 0; i < movingJoints.Count; i++)
        {
            int genomeIndex = i + 1;
            float angle = elapsedTime * Genome.jointFrequency[genomeIndex] * Mathf.PI * 2f
                + Genome.jointPhase[genomeIndex];

            JointMotor motor = movingJoints[i].motor;
            motor.targetVelocity = Mathf.Sin(angle) * Genome.jointAmplitude[genomeIndex];
            movingJoints[i].motor = motor;
        }

        for (int i = 0; i < partBodies.Count; i++)
        {
            Rigidbody body = partBodies[i];
            if (body == null || body.isKinematic)
            {
                continue;
            }

            float radius = Genome.partSizes[i] * 0.5f;
            float projectedArea = Mathf.PI * radius * radius;
            body.AddForce(windDirection.normalized * windStrength * projectedArea, ForceMode.Force);
        }
    }

    public void EvaluateAndStop(Vector3 windDirection)
    {
        Vector3 horizontalWind = Vector3.ProjectOnPlane(windDirection, Vector3.up).normalized;
        Vector3 movement = Vector3.ProjectOnPlane(CalculateCenter() - startCenter, Vector3.up);
        Fitness = Vector3.Dot(movement, horizontalWind);

        foreach (Rigidbody body in partBodies)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
        }
    }

    private Vector3 CalculateCenter()
    {
        Vector3 center = Vector3.zero;
        foreach (Rigidbody body in partBodies)
        {
            center += body.worldCenterOfMass;
        }
        return center / partBodies.Count;
    }

    private static Vector3 GetJointAxis(Vector3 connectionDirection)
    {
        Vector3 axis = Vector3.Cross(connectionDirection, Vector3.up);
        return axis.sqrMagnitude > 0.001f ? axis.normalized : Vector3.right;
    }

    private void OnDestroy()
    {
        foreach (GameObject part in childParts)
        {
            if (part != null)
            {
                Destroy(part);
            }
        }

        if (individualMaterial != null)
        {
            Destroy(individualMaterial);
        }
    }
}
