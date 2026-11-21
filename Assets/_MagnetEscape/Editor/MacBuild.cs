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
            string[] expected = new[] { "StageSelect" }.Concat(Enumerable.Range(1, StageInfo.Count).Select(n => "Stage" + n)).Concat(new[] { "Endless" }).ToArray();
            if (!scenes.Select(Path.GetFileNameWithoutExtension).SequenceEqual(expected))
                throw new BuildFailedException("Enable StageSelect, Stage1-Stage12 and Endless in that order in Build Settings.");
            foreach (string scene in scenes)
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scene) == null)
                    throw new BuildFailedException("Missing build scene: " + scene);
            for (int stage = 1; stage <= StageInfo.Count; stage++)
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
                + "Q / E: 공중 수직 인력 / 척력\n레일: Q 유지로 포착·가속, Space/E 도약, Q 해제 또는 Shift로 분리\nBackspace: 질주를 끝내고 기록 확정\n"
                + "R: 코스 처음부터\nEsc: 일시정지 / 계속하기\nM: 음소거\nH: 도움말 / T: 기록 / C: 제작 정보 / F11: 화면 전환\n정지 중 Enter: 계속 / Tab: 코스 선택 / R: 처음부터 / Backspace: 기록 확정\n\n"
                + "첫 실행은 전체 화면입니다. 선택·정지 화면에서 창 모드로 전환할 수 있습니다.\n"
                + "도움말·기록·제작 정보·음량은 선택·일시정지 화면에 있습니다.\n"
                + "1 쉬움 / 2 어려움. 지형이 계속 생성됩니다. 별이 있는 가장자리에서 점프하고 긴 틈에서는 공중 대시를 이어 쓰세요.\n"
                + "보라색 천장 입구에서 점프 + Q로 반전, E로 복귀합니다. 충돌하거나 떨어지면 거리·별·시설 점수로 난이도별 기기 상위 5개 기록에 도전합니다.\n";
            guide += "\nmacOS Universal: Intel 64비트 + Apple Silicon.\nUnity " + Application.unityVersion + "\n";
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(path), "PLAYING.txt"), guide);
            Debug.Log(details);
        }
    }
}
