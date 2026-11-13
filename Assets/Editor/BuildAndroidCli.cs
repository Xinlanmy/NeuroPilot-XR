using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// CLI 入口：Unity -batchmode -nographics -quit -buildTarget Android -executeMethod BuildAndroidCli.BuildApk
// 仅用于 Linux/CI 无人值守出包，不参与运行时逻辑。
public static class BuildAndroidCli
{
    public static void BuildApk()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        Debug.Log($"[BuildAndroidCli] scenes={scenes.Length}: {string.Join(",", scenes)}");

        const string outPath = "Builds/Android/NeuroPilotXR_cli.apk";
        var opts = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outPath,
            target = BuildTarget.Android,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(opts);
        var s = report.summary;
        Debug.Log($"[BuildAndroidCli] result={s.result} size={s.totalSize} errors={s.totalErrors} warnings={s.totalWarnings} time={s.totalTime} output={outPath}");

        if (s.result != BuildResult.Succeeded)
        {
            Debug.LogError("[BuildAndroidCli] BUILD FAILED");
            EditorApplication.Exit(1);
        }
    }
}
