using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RhythmTherapy.EditorTools
{
    /// <summary>
    /// ResultScene 에 ResultView 를 부착하고, GameScene/ResultScene(+LobyScene) 을
    /// Build Settings 에 등록한다. Unity 에디터 메뉴에서 1회 실행하면 됨
    /// (Pipeline/MCP 로도 실행 가능: RhythmTherapy.EditorTools.ResultSceneSetup.Setup()).
    ///
    /// GameManager.EndGame() 이 SceneManager.LoadScene("ResultScene") 을 호출하므로,
    /// 두 씬 모두 Build Settings 에 없으면 에디터 Play 모드에서도 로드가 실패한다.
    /// </summary>
    public static class ResultSceneSetup
    {
        const string GameScenePath = "Assets/Scenes/GameScene.unity";
        const string ResultScenePath = "Assets/Scenes/ResultScene.unity";
        const string LobyScenePath = "Assets/Scenes/LobyScene.unity";

        [MenuItem("RhythmTherapy/Setup/Wire Result Scene")]
        public static void Setup()
        {
            string originalScenePath = EditorSceneManager.GetActiveScene().path;

            EnsureBuildSettings();
            WireResultScene();

            // 원래 열려있던 씬으로 복귀 (없으면 GameScene)
            if (!string.IsNullOrEmpty(originalScenePath) && originalScenePath != ResultScenePath)
                EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);

            Debug.Log("[ResultSceneSetup] 완료 — ResultView 배선 + Build Settings 등록");
        }

        static void EnsureBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();

            AddIfMissing(scenes, GameScenePath);
            AddIfMissing(scenes, ResultScenePath);
            AddIfMissing(scenes, LobyScenePath);

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static void AddIfMissing(System.Collections.Generic.List<EditorBuildSettingsScene> scenes, string path)
        {
            if (scenes.Any(s => s.path == path))
                return;

            scenes.Add(new EditorBuildSettingsScene(path, true));
        }

        static void WireResultScene()
        {
            var scene = EditorSceneManager.OpenScene(ResultScenePath, OpenSceneMode.Single);

            GameObject host = GameObject.Find("Result")
                ?? GameObject.Find("ResultHUD")
                ?? new GameObject("ResultView");

            var view = host.GetComponent<ResultView>();
            if (view == null)
                view = host.AddComponent<ResultView>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
