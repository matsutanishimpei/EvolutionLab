using UnityEngine;

/// <summary>通常表示とチャンピオン追従表示のカメラ状態を管理します。</summary>
public sealed class EvolutionCameraController
{
    private readonly Camera camera;
    private readonly Vector3 normalPosition;
    private readonly Quaternion normalRotation;
    private readonly float normalFieldOfView;

    public EvolutionCameraController(EvolutionConfig config)
    {
        camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        float width = (config.population.size - 1) * config.environment.laneSpacing
            + config.course.laneWidth + 4f;
        float viewSize = Mathf.Max(width, config.environment.trackLength);
        camera.transform.position = new Vector3(0f, viewSize * 0.75f,
            -config.environment.trackLength * 0.45f);
        camera.transform.LookAt(Vector3.zero);
        camera.fieldOfView = 60f;
        camera.farClipPlane = viewSize * 4f;
        normalPosition = camera.transform.position;
        normalRotation = camera.transform.rotation;
        normalFieldOfView = camera.fieldOfView;
    }

    public void Follow(SphereIndividual individual, Vector3 downwind,
        EvolutionConfig.ReplaySettings settings)
    {
        Vector3 center = individual.GetCenterPosition();
        camera.transform.position = center - downwind * settings.cameraDistance
            + Vector3.up * settings.cameraHeight;
        camera.transform.LookAt(center + Vector3.up * 0.5f);
        camera.fieldOfView = settings.cameraFieldOfView;
    }

    public void Restore()
    {
        camera.transform.SetPositionAndRotation(normalPosition, normalRotation);
        camera.fieldOfView = normalFieldOfView;
    }
}
