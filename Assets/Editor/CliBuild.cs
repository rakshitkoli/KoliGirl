using UnityEditor;
using UnityEngine;

// Temporary CLI build helper for installing dev builds to a USB-debugging device.
// Not part of the shipped game.
public static class CliBuild
{
    public static void BuildAndroidApk()
    {
        var scenes = EditorBuildSettings.scenes;
        var enabledScenePaths = System.Array.FindAll(scenes, s => s.enabled);
        var scenePaths = System.Array.ConvertAll(enabledScenePaths, s => s.path);

        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenePaths,
            locationPathName = "Builds/Android/KoliGirl.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        EditorUserBuildSettings.buildAppBundle = false;

        // Dev-install build: sign with the debug key so this doesn't need the
        // release keystore password (not available non-interactively). The app
        // isn't installed on the target device yet, so there's no signature
        // mismatch. This setting is in-memory only for this process; not saved.
        PlayerSettings.Android.useCustomKeystore = false;

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.LogError("Build failed: " + report.summary.result);
            EditorApplication.Exit(1);
        }
        else
        {
            Debug.Log("Build succeeded: " + report.summary.outputPath);
            EditorApplication.Exit(0);
        }
    }
}
