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
    public int CheckpointsPassed { get; private set; }
    public float ObstacleContactTime { get; private set; }
    public bool HeightViolation { get; private set; }
    public float SpeedScore { get; private set; }
    public bool HasReachedFinish { get; private set; }

    private readonly List<Rigidbody> partBodies = new List<Rigidbody>();
    private readonly List<HingeJoint> movingJoints = new List<HingeJoint>();
    private readonly List<GameObject> childParts = new List<GameObject>();
    private Vector3 startCenter;
    private PhysicsMaterial individualMaterial;
    private Material visualMaterial;
    private Vector3 courseWindDirection;
    private Vector3 courseLineDirection;
    private float laneCenterCoordinate;
    private float[] checkpointDownwindPositions;
    private float[] checkpointGapOffsets;
    private float checkpointOpeningWidth;
    private float maximumAllowedHeight;
    private float maximumDownwindDistance;
    private float courseEvaluationDuration;
    private float currentElapsedTime;
    private float finishDownwindPosition;
    private EvolutionConfig.FitnessSettings fitnessSettings;

    public void Initialize(Genome genome, Color bodyColor)
    {
        Genome = genome.Clone();
        Fitness = 0f;
        CheckpointsPassed = 0;
        ObstacleContactTime = 0f;
        HeightViolation = false;
        SpeedScore = 0f;
        HasReachedFinish = false;

        individualMaterial = new PhysicsMaterial($"{name}_Material")
        {
            staticFriction = Genome.friction,
            dynamicFriction = Genome.friction,
            bounciness = Genome.bounciness,
            frictionCombine = PhysicsMaterialCombine.Average,
            bounceCombine = PhysicsMaterialCombine.Average
        };

        Shader bodyShader = Shader.Find("Universal Render Pipeline/Lit");
        if (bodyShader == null)
        {
            bodyShader = Shader.Find("Standard");
        }
        visualMaterial = new Material(bodyShader) { color = bodyColor };

        Rigidbody rootBody = GetComponent<Rigidbody>();
        ConfigurePart(gameObject, rootBody, Genome.partSizes[0]);
        partBodies.Add(rootBody);

        for (int i = 1; i < Genome.partCount; i++)
        {
            CreateConnectedPart(i, rootBody);
        }

        startCenter = CalculateCenter();
    }

    /// <summary>この個体が走るレーンの共通チェックポイント条件を設定します。</summary>
    public void ConfigureCourse(
        Vector3 windDirection,
        Vector3 lineDirection,
        float laneCenter,
        float[] checkpointPositions,
        float[] gapOffsets,
        float openingWidth,
        float maxHeight,
        float maxDistance,
        float evaluationDuration,
        float finishPosition,
        EvolutionConfig.FitnessSettings scoring)
    {
        courseWindDirection = windDirection.normalized;
        courseLineDirection = lineDirection.normalized;
        laneCenterCoordinate = laneCenter;
        checkpointDownwindPositions = checkpointPositions;
        checkpointGapOffsets = gapOffsets;
        checkpointOpeningWidth = openingWidth;
        maximumAllowedHeight = maxHeight;
        maximumDownwindDistance = maxDistance;
        courseEvaluationDuration = evaluationDuration;
        finishDownwindPosition = finishPosition;
        fitnessSettings = scoring;
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
        part.GetComponent<Renderer>().sharedMaterial = visualMaterial;
        part.AddComponent<IndividualCollisionSensor>().Initialize(this);
    }

    /// <summary>物理更新ごとに関節モーターと、面積に比例する風を適用します。</summary>
    public void Simulate(Vector3 windDirection, float windStrength, float elapsedTime)
    {
        currentElapsedTime = elapsedTime;
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

        UpdateCourseProgress();
    }

    private void UpdateCourseProgress()
    {
        if (checkpointDownwindPositions == null)
        {
            return;
        }

        float highestPart = float.MinValue;
        foreach (Rigidbody body in partBodies)
        {
            highestPart = Mathf.Max(highestPart, body.worldCenterOfMass.y);
        }

        if (highestPart > maximumAllowedHeight)
        {
            HeightViolation = true;
        }

        Vector3 center = CalculateCenter();
        float downwindPosition = Vector3.Dot(center, courseWindDirection);
        float lateralPosition = Vector3.Dot(center, courseLineDirection);

        if (CheckpointsPassed >= checkpointDownwindPositions.Length
            && downwindPosition >= finishDownwindPosition)
        {
            HasReachedFinish = true;
        }

        if (CheckpointsPassed >= checkpointDownwindPositions.Length)
        {
            return;
        }

        float requiredLateralPosition = laneCenterCoordinate + checkpointGapOffsets[CheckpointsPassed];

        bool reachedCheckpoint = downwindPosition >= checkpointDownwindPositions[CheckpointsPassed];
        bool insideOpening = Mathf.Abs(lateralPosition - requiredLateralPosition)
            <= checkpointOpeningWidth * 0.45f;

        if (reachedCheckpoint && insideOpening && !HeightViolation)
        {
            // 同じ関門でも、早く到達するほど最大25点の速度ボーナスを得ます。
            float normalizedRemainingTime = 1f
                - Mathf.Clamp01(currentElapsedTime / courseEvaluationDuration);
            SpeedScore += normalizedRemainingTime * fitnessSettings.speedRewardPerCheckpoint;
            CheckpointsPassed++;
        }
    }

    public void RegisterObstacleContact(float contactDuration)
    {
        ObstacleContactTime += contactDuration;
    }

    public void EvaluateAndStop(Vector3 windDirection)
    {
        Vector3 horizontalWind = Vector3.ProjectOnPlane(windDirection, Vector3.up).normalized;
        Vector3 movement = Vector3.ProjectOnPlane(CalculateCenter() - startCenter, Vector3.up);
        float downwindDistance = Mathf.Clamp(
            Vector3.Dot(movement, horizontalWind),
            0f,
            maximumDownwindDistance);

        Fitness = FitnessEvaluator.Calculate(
            CheckpointsPassed,
            downwindDistance,
            SpeedScore,
            ObstacleContactTime,
            HeightViolation,
            fitnessSettings);

        foreach (Rigidbody body in partBodies)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
        }
    }

    /// <summary>個体を構成する全パーツの表示・非表示を切り替えます。</summary>
    public void SetVisible(bool visible)
    {
        foreach (Rigidbody body in partBodies)
        {
            Renderer partRenderer = body.GetComponent<Renderer>();
            if (partRenderer != null)
            {
                partRenderer.enabled = visible;
            }
        }
    }

    /// <summary>リプレイ個体と評価済み個体が衝突しないようColliderを切り替えます。</summary>
    public void SetCollisionEnabled(bool enabled)
    {
        foreach (Rigidbody body in partBodies)
        {
            Collider partCollider = body.GetComponent<Collider>();
            if (partCollider != null)
            {
                partCollider.enabled = enabled;
            }
        }
    }

    /// <summary>カメラ追従用に、全パーツの中心位置を返します。</summary>
    public Vector3 GetCenterPosition()
    {
        return CalculateCenter();
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

        if (visualMaterial != null)
        {
            Destroy(visualMaterial);
        }
    }
}
