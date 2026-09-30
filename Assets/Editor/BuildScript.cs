using UnityEditor;
using UnityEngine;
using System.IO;

public class BuildScript
{
    public static void BuildAndroid()
    {
        string buildPath = "/home/luis/Dokumente/Unity/Horizon-faser/Builds/HorizonFaser.apk";
        Directory.CreateDirectory(Path.GetDirectoryName(buildPath));

        string[] scenes = new string[] { "Assets/Scenes/SampleScene.unity" };

        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        Debug.Log("[BuildScript] Starting Android build...");
        var report = BuildPipeline.BuildPlayer(buildOptions);

        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"[BuildScript] Build SUCCESS: {buildPath}");
        }
        else
        {
            Debug.LogError($"[BuildScript] Build FAILED: {report.summary.result}");
        }
    }
}
