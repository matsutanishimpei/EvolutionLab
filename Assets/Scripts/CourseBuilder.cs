using System.Collections.Generic;
using UnityEngine;

/// <summary>床、独立レーン、障害物を生成し、その表示と破棄を管理します。</summary>
public sealed class CourseBuilder
{
    private readonly EvolutionConfig config;
    private readonly List<GameObject> objects = new List<GameObject>();
    private readonly List<Renderer> renderers = new List<Renderer>();
    private readonly List<Material> materials = new List<Material>();

    public CourseBuilder(EvolutionConfig config) => this.config = config;

    public CourseLayout Build()
    {
        Vector3 downwind = Vector3.ProjectOnPlane(config.environment.windDirection, Vector3.up).normalized;
        if (downwind.sqrMagnitude < 0.0001f) downwind = Vector3.forward;
        Vector3 lateral = Vector3.Cross(Vector3.up, downwind).normalized;
        Quaternion rotation = Quaternion.LookRotation(downwind, Vector3.up);

        Material ground = CreateMaterial("Ground Material", new Color(0.12f, 0.22f, 0.16f));
        Material obstacle = CreateMaterial("Obstacle Material", new Color(0.9f, 0.32f, 0.08f));
        CreateGround(ground);

        float[] positions = new float[config.course.obstacleCount];
        float[] offsets = new float[config.course.obstacleCount];
        for (int row = 0; row < positions.Length; row++)
        {
            float progress = (row + 1f) / (positions.Length + 1f);
            positions[row] = Mathf.Lerp(-config.environment.trackLength * 0.15f,
                config.environment.trackLength * 0.38f, progress);
            float side = row % 2 == 0 ? -1f : 1f;
            offsets[row] = -side * (config.course.laneWidth - config.course.openingWidth) * 0.5f;
        }

        CourseLayout layout = new CourseLayout(downwind, lateral, positions, offsets);
        for (int lane = 0; lane < config.population.size; lane++)
        {
            Vector3 center = lateral * layout.LaneCoordinate(lane, config.population.size,
                config.environment.laneSpacing);
            CreateBox(center + lateral * (-config.course.laneWidth * 0.5f), rotation, 0.35f,
                config.course.laneWallHeight, config.environment.trackLength + 10f,
                $"Lane_{lane + 1:00}_LeftWall", obstacle);
            CreateBox(center + lateral * (config.course.laneWidth * 0.5f), rotation, 0.35f,
                config.course.laneWallHeight, config.environment.trackLength + 10f,
                $"Lane_{lane + 1:00}_RightWall", obstacle);

            for (int row = 0; row < positions.Length; row++)
            {
                float width = config.course.laneWidth - config.course.openingWidth;
                float side = row % 2 == 0 ? -1f : 1f;
                Vector3 obstacleCenter = center + downwind * positions[row]
                    + lateral * side * (config.course.laneWidth - width) * 0.5f;
                CreateBox(obstacleCenter, rotation, width, config.course.obstacleHeight, 1.5f,
                    $"Lane_{lane + 1:00}_Obstacle_{row + 1:00}", obstacle);
            }
        }
        return layout;
    }

    public void SetVisible(bool visible)
    {
        foreach (Renderer item in renderers) if (item != null) item.enabled = visible;
    }

    public void Dispose()
    {
        foreach (GameObject item in objects) if (item != null) Object.Destroy(item);
        foreach (Material item in materials) if (item != null) Object.Destroy(item);
    }

    private void CreateGround(Material material)
    {
        float width = (config.population.size - 1) * config.environment.laneSpacing
            + config.course.laneWidth + 4f;
        float size = Mathf.Max(width, config.environment.trackLength + 20f);
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = Vector3.one * (size / 10f);
        Register(ground, material, false);
    }

    private void CreateBox(Vector3 groundPosition, Quaternion rotation, float width, float height,
        float depth, string objectName, Material material)
    {
        GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
        item.name = objectName;
        item.transform.SetPositionAndRotation(groundPosition + Vector3.up * (height * 0.5f), rotation);
        item.transform.localScale = new Vector3(width, height, depth);
        Register(item, material, true);
    }

    private void Register(GameObject item, Material material, bool isObstacle)
    {
        if (isObstacle) item.AddComponent<EvolutionObstacle>();
        Renderer renderer = item.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        objects.Add(item);
        renderers.Add(renderer);
    }

    private Material CreateMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material material = new Material(shader) { name = name, color = color };
        materials.Add(material);
        return material;
    }
}
