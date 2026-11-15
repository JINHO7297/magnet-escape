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
            for (int stage = 1; stage <= 4; stage++)
            {
                StageDefinition definition = StageInfo.Definition(stage);
                if (definition == null || definition.Number != stage)
                    throw new BuildFailedException("Missing or mismatched course asset: Resources/Stages/Stage" + stage);
                // Baked revisions own their player instance; later source-prefab edits
                // must not invalidate that independent build set.
                if (scenes[stage] == "Assets/_MagnetEscape/Scenes/Stage" + stage + ".unity")
                {
                    string prefabError = definition.PlayerPrefabError();
                    if (prefabError != null)
                        throw new BuildFailedException("Stage " + stage + ": " + prefabError);
                }
            }
            if (LayerMask.NameToLayer("Ground") < 0)
                throw new BuildFailedException("The Ground physics layer is missing.");
            string path = Path.GetFullPath(output);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            BuildReport report;
            string previousProductName = PlayerSettings.productName;
            // macOS standalone uses EditorUserBuildSettings, not the iOS
            // PlayerSettings.SetArchitecture API. Include both desktop CPUs.
            const string architecture = "x64ARM64";
            string previousArchitecture = EditorUserBuildSettings.GetPlatformSettings("Standalone", "OSXUniversal", "Architecture");
            try
            {
                PlayerSettings.productName = "Magnet Skater";
                EditorUserBuildSettings.SetPlatformSettings("Standalone", "OSXUniversal", "Architecture", architecture);
                report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = path,
                    target = BuildTarget.StandaloneOSX,
                    options = BuildOptions.StrictMode | (development ? BuildOptions.Development : BuildOptions.None)
                });
            }
            finally
            {
                PlayerSettings.productName = previousProductName;
                EditorUserBuildSettings.SetPlatformSettings("Standalone", "OSXUniversal", "Architecture", previousArchitecture);
            }
            foreach (BuildStep step in report.steps)
                foreach (BuildStepMessage message in step.messages)
                    if (message.type == LogType.Warning || message.type == LogType.Error)
                        Debug.Log("[빌드 " + message.type + "] " + message.content);
            BuildSummary summary = report.summary;
            string details = "Result: " + summary.result + "\nUnity: " + Application.unityVersion
                + "\nTarget: " + summary.platform + "\nBytes: " + summary.totalSize
                + "\nRequested architecture: " + architecture
                + "\nErrors: " + summary.totalErrors + "\nWarnings: " + summary.totalWarnings
                + "\nDuration: " + summary.totalTime + "\nOutput: " + path + "\n";
            File.WriteAllText(Path.ChangeExtension(path, ".build.txt"), details);
            if (summary.result != BuildResult.Succeeded || summary.totalErrors > 0)
                throw new BuildFailedException(details);
            string guide = "마그넷 스케이터\n\n" + Path.GetFileName(path) + "을 열어 플레이하세요.\n\n"
                + "자동 이동: 항상 오른쪽으로 전진\nSpace: 가변 높이 점프\nShift: 전방 대시 (무적 없음)\n"
                + "Q / E: 공중 수직 인력 / 척력\nBackspace: 체크포인트 재도전 (충돌 +1, 시간 유지)\n"
                + "R: 코스 처음부터\nEsc: 일시정지 / 계속하기\nM: 음소거\n\n"
                + "첫 실행은 전체 화면입니다. 선택·정지 화면에서 창 모드로 전환할 수 있습니다.\n"
                + "도움말·기록·제작 정보·음량은 선택·일시정지 화면에 있습니다.\n"
                + "4코스 모두 처음부터 선택 가능. 2코스부터 대시, 3코스부터 공중 자력을 사용합니다.\n"
                + "최고 시간과 최고 등급은 따로 저장됩니다. S는 무사망으로 기준 시간 이내 완주해야 합니다.\n\n";
            for (int stage = 1; stage <= 4; stage++)
                guide += stage + "코스 - " + StageInfo.Title(stage) + ": S 기준 " + StageInfo.ParTime(stage) + "초\n";
            guide += "\nmacOS Universal: Intel 64비트 + Apple Silicon.\nUnity " + Application.unityVersion + "\n";
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(path), "PLAYING.txt"), guide);
            Debug.Log(details);
        }
    }
}
