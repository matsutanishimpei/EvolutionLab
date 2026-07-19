using System;

/// <summary>
/// 実験コードからEditor専用の動画Recorderへ録画命令を渡します。
/// Runtime側をUnityEditor APIへ直接依存させないための橋渡しです。
/// </summary>
public static class EvolutionRecordingEvents
{
    public static event Action<string> RecordingStarted;
    public static event Action RecordingStopped;

    public static void StartRecording(string fileName)
    {
        RecordingStarted?.Invoke(fileName);
    }

    public static void StopRecording()
    {
        RecordingStopped?.Invoke();
    }
}
