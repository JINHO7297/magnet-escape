using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MagnetEscape.Editor
{
    public static class MacBuild
    {
        [MenuItem("Tools/Magnet Escape/Build macOS App")]
        public static void BuildFromMenu() => Build("Builds/MagnetEscape.app", false);

        public static void BuildFromCommandLine()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(arguments, "-magnetBuildPath");
            if (index < 0 || index + 1 >= arguments.Length || arguments[index + 1].StartsWith("-"))
                throw new BuildFailedException("Provide -magnetBuildPath followed by the output .app path.");
            Build(arguments[index + 1], arguments.Contains("-magnetDevelopment"));
        }

        private static void Build(string output, bool development)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new BuildFailedException("Stop Play mode before building.");
            if (!output.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
                throw new BuildFailedException("macOS output must end with .app.");
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX))
                throw new BuildFailedException("Install macOS Build Support for this Unity Editor in Unity Hub.");
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneOSX)
            {
                if (Application.isBatchMode)
                    throw new BuildFailedException("Start Unity with -buildTarget StandaloneOSX.");
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX))
                    throw new BuildFailedException("Could not switch to the macOS build target.");
            }
            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            string[] expected = { "StageSelect", "Stage1", "Stage2", "Stage3", "Stage4" };
            if (!scenes.Select(Path.GetFileNameWithoutExtension).SequenceEqual(expected))
                throw new BuildFailedException("Enable StageSelect and Stage1-Stage4 in that order in Build Settings.");
            foreach (string scene in scenes)
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scene) == null)
                    throw new BuildFailedException("Missing build scene: " + scene);
            if (LayerMask.NameToLayer("Ground") < 0)
                throw new BuildFailedException("The Ground physics layer is missing.");
            string path = Path.GetFullPath(output);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = path,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.StrictMode | (development ? BuildOptions.Development : BuildOptions.None)
            });
            BuildSummary summary = report.summary;
            string details = "Result: " + summary.result + "\nUnity: " + Application.unityVersion
                + "\nTarget: " + summary.platform + "\nBytes: " + summary.totalSize
                + "\nErrors: " + summary.totalErrors + "\nWarnings: " + summary.totalWarnings
                + "\nDuration: " + summary.totalTime + "\nOutput: " + path + "\n";
            File.WriteAllText(Path.ChangeExtension(path, ".build.txt"), details);
            if (summary.result != BuildResult.Succeeded || summary.totalErrors > 0)
                throw new BuildFailedException(details);
            Debug.Log(details);
        }
    }
}
