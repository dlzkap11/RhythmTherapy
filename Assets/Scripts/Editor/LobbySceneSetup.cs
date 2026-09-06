using System.IO;
using System.Linq;

using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RhythmTherapy.EditorTools
{
    /// <summary>
    /// LobyScene 에 LobbyController 를 배선하고, 캐러셀이 순회할 데모곡(SongDataConfig)이
    /// 3개 미만이면 채워 넣는다. 에디터 메뉴 또는 MCP 로 1회 실행. 재실행 안전.
    /// </summary>
    public static class LobbySceneSetup
    {
        const string LobyScenePath = "Assets/Scenes/LobyScene.unity";
        const string SongDir = "Assets/Resources/SongData";
        const string ClipPath = "Assets/Resources/SongData/oceanking-september.mp3";

        [MenuItem("RhythmTherapy/Setup/Wire Lobby Scene")]
        public static void Setup()
        {
            string original = EditorSceneManager.GetActiveScene().path;

            EnsureDemoSongs();

            Scene scene = EditorSceneManager.OpenScene(LobyScenePath, OpenSceneMode.Single);
            WireLobbyScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            if (!string.IsNullOrEmpty(original) && original != LobyScenePath)
                EditorSceneManager.OpenScene(original, OpenSceneMode.Single);

            Debug.Log("[LobbySceneSetup] 완료 — LobbyController 배선 + 데모곡 확인");
        }

        static void EnsureDemoSongs()
        {
            SongDataConfig[] existing = AssetDatabase.FindAssets("t:SongDataConfig")
                .Select(g => AssetDatabase.LoadAssetAtPath<SongDataConfig>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(s => s != null)
                .ToArray();

            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath);

            // 기존 첫 곡(TestSong 등)에도 테마색을 준다.
            if (existing.Length > 0 && existing[0].ThemeColor == Color.white)
            {
                existing[0].ThemeColor = new Color(0.25f, 0.7f, 1f);
                EditorUtility.SetDirty(existing[0]);
            }

            CreateSongIfMissing(existing, "Song_NeonDrift", 1, "Neon Drift", new Color(1f, 0.85f, 0.2f), clip);
            CreateSongIfMissing(existing, "Song_MidnightRun", 2, "Midnight Run", new Color(0.7f, 0.35f, 0.95f), clip);

            AssetDatabase.SaveAssets();
        }

        static void CreateSongIfMissing(SongDataConfig[] existing, string assetName, int songId, string songName, Color theme, AudioClip clip)
        {
            if (existing.Any(s => s.SongName == songName))
                return;

            if (existing.Length >= 3)
                return;

            SongDataConfig so = ScriptableObject.CreateInstance<SongDataConfig>();
            so.SongID = songId;
            so.SongName = songName;
            so.ThemeColor = theme;
            so.SongAudioClip = clip;

            Directory.CreateDirectory(SongDir);
            AssetDatabase.CreateAsset(so, $"{SongDir}/{assetName}.asset");
        }

        static void WireLobbyScene()
        {
            RenameObject("LefgSong", "LeftSong");

            GameObject chartUi = Find("ChartUI");
            if (chartUi == null)
                return;

            LobbyController controller = chartUi.GetComponent<LobbyController>() ?? chartUi.AddComponent<LobbyController>();

            AudioSource preview = FindPreviewSource(chartUi.transform);

            SerializedObject so = new SerializedObject(controller);
            SetRef(so, "centerImage", GetComponent<Image>("SelectSong"));
            SetRef(so, "leftImage", GetComponent<Image>("LeftSong"));
            SetRef(so, "rightImage", GetComponent<Image>("RightSong"));
            SetRef(so, "circleSprite", AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"));
            SetRef(so, "titleText", GetComponent<TextMeshProUGUI>("Title"));
            SetRef(so, "highScoreText", GetComponent<TextMeshProUGUI>("HighScore"));
            SetRef(so, "playButton", GetComponent<Button>("GameStartButton"));
            SetRef(so, "positionBar", GetComponent<Scrollbar>("Scrollbar"));
            SetRef(so, "previewSource", preview);
            so.ApplyModifiedPropertiesWithoutUndo();

            TextMeshProUGUI buttonText = GetComponent<TextMeshProUGUI>("ButtonText");
            if (buttonText != null && string.IsNullOrWhiteSpace(buttonText.text))
                buttonText.text = "Play!";
        }

        static AudioSource FindPreviewSource(Transform parent)
        {
            Transform existing = parent.Find("@LobbyPreview");
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject("@LobbyPreview", typeof(AudioSource));
                go.transform.SetParent(parent, false);
            }

            AudioSource source = go.GetComponent<AudioSource>();
            if (source == null)
                source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            return source;
        }

        static void SetRef(SerializedObject so, string field, Object value)
        {
            SerializedProperty prop = so.FindProperty(field);
            if (prop != null)
                prop.objectReferenceValue = value;
        }

        static void RenameObject(string oldName, string newName)
        {
            GameObject go = Find(oldName);
            if (go != null)
                go.name = newName;
        }

        static T GetComponent<T>(string objectName) where T : Component
        {
            GameObject go = Find(objectName);
            return go != null ? go.GetComponent<T>() : null;
        }

        static GameObject Find(string exactName)
        {
            foreach (Transform tr in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (tr.name == exactName)
                    return tr.gameObject;
            }

            return null;
        }
    }
}
