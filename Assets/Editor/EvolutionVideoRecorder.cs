using System.IO;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;

/// <summary>
/// Unity Recorderを操作し、世代全体とチャンピオン紹介をMP4へ保存します。
/// Editor専用コードなので、ビルドしたPlayerには含まれません。
/// </summary>
[InitializeOnLoad]
public static class EvolutionVideoRecorder
{
    private static RecorderController recorderController;
    private static RecorderControllerSettings controllerSettings;
    private static MovieRecorderSettings movieSettings;

    static EvolutionVideoRecorder()
    {
        EvolutionRecordingEvents.RecordingStarted += StartRecording;
        EvolutionRecordingEvents.RecordingStopped += StopRecording;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void StartRecording(string fileName)
    {
        StopRecording();

        string outputDirectory = Path.GetFullPath(
            Path.Combine(Application.dataPath, "..", "Recordings"));
        Directory.CreateDirectory(outputDirectory);

        controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        movieSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();

        movieSettings.name = "Evolution MP4 Recorder";
        movieSettings.Enabled = true;
        movieSettings.EncoderSettings = new CoreEncoderSettings
        {
            Codec = CoreEncoderSettings.OutputCodec.MP4,
            EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High
        };
        movieSettings.OutputFile = Path.Combine(outputDirectory, fileName);
        movieSettings.CaptureAudio = false;
        movieSettings.FrameRate = 30f;
        movieSettings.CapFrameRate = true;
        movieSettings.ImageInputSettings = new GameViewInputSettings
        {
            OutputWidth = 1280,
            OutputHeight = 720
        };

        controllerSettings.AddRecorderSettings(movieSettings);
        controllerSettings.SetRecordModeToManual();
        recorderController = new RecorderController(controllerSettings);
        recorderController.PrepareRecording();

        if (recorderController.StartRecording())
        {
            Debug.Log($"動画録画を開始しました: {fileName}.mp4");
        }
        else
        {
            Debug.LogError($"動画録画を開始できませんでした: {fileName}.mp4");
        }
    }

    private static void StopRecording()
    {
        if (recorderController != null && recorderController.IsRecording())
        {
            recorderController.StopRecording();
            Debug.Log("動画録画を終了しました。");
        }

        recorderController = null;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            StopRecording();
        }
    }
}
