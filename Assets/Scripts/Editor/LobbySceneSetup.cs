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
    /// LobbyScene 에 LobbyController 를 배선하고, 캐러셀이 순회할 데모곡(SongDataConfig)이
    /// 3개 미만이면 채워 넣는다. 에디터 메뉴 또는 MCP 로 1회 실행. 재실행 안전.
    /// </summary>
    public static class LobbySceneSetup
    {
        const string LobbyScenePath = "Assets/Scenes/LobbyScene.unity";
        const string SongDir = "Assets/Resources/SongData";
        const string ClipPath = "Assets/Resources/SongData/oceanking-september.mp3";

        [MenuItem("RhythmTherapy/Setup/Wire Lobby Scene")]
        public static void Setup()
        {
            string original = EditorSceneManager.GetActiveScene().path;

            EnsureDemoSongs();

            Scene scene = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);
            WireLobbyScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            if (!string.IsNullOrEmpty(original) && original != LobbyScenePath)
                EditorSceneManager.OpenScene(original, OpenSceneMode.Single);

            Debug.Log("[LobbySceneSetup] 완료 — LobbyController 배선 + 데모곡 확인");
        }

        static void EnsureDemoSongs()
        {
            SongDataConfig[] existing = AssetDatabase.FindAssets("t:SongDataConfig")
                .Select(g => AssetDatabase.LoadAssetAtPath<SongDataConfig>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(s => s != null)
                .ToArray();

            // 곡 콘텐츠는 손으로 관리한다. 3개 이상 있으면 손대지 않는다 (빈 프로젝트 부트스트랩 전용).
            if (existing.Length >= 3)
                return;

            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath);
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
            Image[] buffers = BuildBufferCircles(GetComponent<Image>("SelectSong"), GetComponent<Image>("LeftSong"));

            SerializedObject so = new SerializedObject(controller);
            SetRef(so, "centerImage", GetComponent<Image>("SelectSong"));
            SetRef(so, "leftImage", GetComponent<Image>("LeftSong"));
            SetRef(so, "rightImage", GetComponent<Image>("RightSong"));
            SetRefArray(so, "bufferImages", buffers);
            SetRef(so, "circleSprite", AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"));
            SetRef(so, "titleText", GetComponent<TextMeshProUGUI>("Title"));
            SetRef(so, "highScoreText", GetComponent<TextMeshProUGUI>("HighScore"));
            SetRef(so, "playButton", GetComponent<Button>("GameStartButton"));
            SetRef(so, "positionBar", GetComponent<Scrollbar>("Scrollbar"));
            SetRef(so, "previewSource", preview);
            so.ApplyModifiedPropertiesWithoutUndo();

            LayoutInfoTexts();
            StylePlayButton();
        }

        static readonly Color Accent = new Color(0.16f, 0.82f, 0.45f);   // 네온 그린

        /// <summary>
        /// Title / HighScore 겹침 정리 + 텍스트 박스·배경 패널을 넓혀 긴 곡 이름에 여유를 준다. 재실행 안전.
        /// </summary>
        static void LayoutInfoTexts()
        {
            TextMeshProUGUI title = GetComponent<TextMeshProUGUI>("Title");
            if (title != null)
            {
                title.textWrappingMode = TextWrappingModes.NoWrap;
                title.overflowMode = TextOverflowModes.Ellipsis;
                title.enableAutoSizing = true;
                title.fontSizeMin = 18f;
                title.fontSizeMax = 46f;
                WidenBox(title.rectTransform, 520f);

                // 배경 패널(Title 의 부모)도 함께 넓혀 텍스트가 패널 밖으로 안 나가게.
                RectTransform panel = title.rectTransform.parent as RectTransform;
                if (panel != null)
                {
                    Vector2 s = panel.sizeDelta;
                    s.x = 2100f;   // 좌우 앨범 원과 겹치지 않는 최대치 근처
                    panel.sizeDelta = s;
                }
            }

            // 캐러셀(SelectSong/LeftSong/RightSong/Scrollbar) 패널을 맨 뒤 형제로 → 원들이 정보 패널 위에 그려짐.
            Image selectImg = GetComponent<Image>("SelectSong");
            if (selectImg != null && selectImg.transform.parent != null)
            {
                selectImg.transform.parent.SetAsLastSibling();
            }

            TextMeshProUGUI highScore = GetComponent<TextMeshProUGUI>("HighScore");
            if (highScore != null)
            {
                highScore.textWrappingMode = TextWrappingModes.NoWrap;
                highScore.overflowMode = TextOverflowModes.Overflow;
                highScore.enableAutoSizing = false;
                highScore.fontSize = 24f;
                highScore.color = new Color(0.8f, 0.85f, 0.9f);   // 보조 정보라 살짝 흐리게
                WidenBox(highScore.rectTransform, 340f);

                RectTransform rt = highScore.rectTransform;
                Vector2 pos = rt.anchoredPosition;
                if (pos.y > -72f)
                {
                    pos.y = -72f;
                    rt.anchoredPosition = pos;
                }
            }
        }

        /// <summary>Play 버튼을 조금 줄이고 네온 액센트 + 그림자로 스타일. 재실행 안전.</summary>
        static void StylePlayButton()
        {
            GameObject go = Find("GameStartButton");
            if (go == null)
                return;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.localScale = new Vector3(4.4f, 4.4f, 4.4f);
            rt.sizeDelta = new Vector2(180f, 46f);

            Image img = go.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                img.type = Image.Type.Sliced;
                img.color = Color.white;   // 실제 색은 Button.colors 로
            }

            Shadow shadow = go.GetComponent<Shadow>() ?? go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
            shadow.effectDistance = new Vector2(0f, -3f);

            Button btn = go.GetComponent<Button>();
            if (btn != null)
            {
                btn.transition = Selectable.Transition.ColorTint;
                ColorBlock cb = btn.colors;
                cb.normalColor = Accent;
                cb.highlightedColor = Color.Lerp(Accent, Color.white, 0.25f);
                cb.pressedColor = Accent * 0.8f;
                cb.selectedColor = Accent;
                cb.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                cb.fadeDuration = 0.1f;
                btn.colors = cb;
            }

            TextMeshProUGUI label = GetComponent<TextMeshProUGUI>("ButtonText");
            if (label != null)
            {
                label.text = "PLAY";
                label.color = Color.white;
                label.fontStyle = FontStyles.Bold;
                label.enableAutoSizing = false;
                label.fontSize = 26f;
                label.characterSpacing = 10f;
                label.alignment = TextAlignmentOptions.Center;
            }
        }

        static void WidenBox(RectTransform rt, float minWidth)
        {
            Vector2 s = rt.sizeDelta;
            if (s.x < minWidth)
            {
                s.x = minWidth;
                rt.sizeDelta = s;
            }
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

        static void SetRefArray(SerializedObject so, string field, Object[] values)
        {
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
                return;

            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        /// <summary>
        /// 캐러셀 무한 로터리용 버퍼 원 4개(BufferL2/L1/R1/R2)를 SelectSong 의 부모 밑에 만든다.
        /// LeftSong 설정을 복사하고, 씬 뷰에서도 보이도록 각자의 링 슬롯(offset ±2/±3) 위치에 배치한다.
        /// 런타임 LobbyController 가 재배치하므로 위치는 참고용. 재실행 안전.
        /// </summary>
        static Image[] BuildBufferCircles(Image sampleSelect, Image sampleLeft)
        {
            Image sampleRight = GetComponent<Image>("RightSong");
            if (sampleSelect == null || sampleLeft == null || sampleRight == null)
                return new Image[4];

            Transform parent = sampleSelect.transform.parent;
            RectTransform srcRt = sampleLeft.rectTransform;
            Sprite knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            // LobbyController.FitRing 과 동일: 3 슬롯 좌표로 외접원.
            Vector2 pa = sampleLeft.rectTransform.anchoredPosition;
            Vector2 pb = sampleSelect.rectTransform.anchoredPosition;
            Vector2 pc = sampleRight.rectTransform.anchoredPosition;
            float dd = 2f * (pa.x * (pb.y - pc.y) + pb.x * (pc.y - pa.y) + pc.x * (pa.y - pb.y));
            Vector2 ringCenter = pb;
            float ringRadius = 0f, frontAngle = 90f, stepAngle = 58.3f;
            if (Mathf.Abs(dd) > 1e-3f)
            {
                float a2 = pa.sqrMagnitude, b2 = pb.sqrMagnitude, c2 = pc.sqrMagnitude;
                float ux = (a2 * (pb.y - pc.y) + b2 * (pc.y - pa.y) + c2 * (pa.y - pb.y)) / dd;
                float uy = (a2 * (pc.x - pb.x) + b2 * (pa.x - pc.x) + c2 * (pb.x - pa.x)) / dd;
                ringCenter = new Vector2(ux, uy);
                ringRadius = Vector2.Distance(ringCenter, pb);
                frontAngle = Mathf.Atan2(pb.y - uy, pb.x - ux) * Mathf.Rad2Deg;
                stepAngle = Mathf.DeltaAngle(frontAngle, Mathf.Atan2(pa.y - uy, pa.x - ux) * Mathf.Rad2Deg);
            }

            string[] names = { "BufferL2", "BufferL1", "BufferR1", "BufferR2" };
            int[] offsets = { 3, 2, -2, -3 };
            Image[] result = new Image[4];

            for (int i = 0; i < 4; i++)
            {
                GameObject go = Find(names[i]);
                if (go == null)
                {
                    go = new GameObject(names[i], typeof(RectTransform), typeof(Image));
                    go.transform.SetParent(parent, false);
                }

                RectTransform rt = go.GetComponent<RectTransform>();
                rt.anchorMin = srcRt.anchorMin;
                rt.anchorMax = srcRt.anchorMax;
                rt.pivot = srcRt.pivot;
                rt.sizeDelta = srcRt.sizeDelta;

                float ang = (frontAngle + offsets[i] * stepAngle) * Mathf.Deg2Rad;
                rt.anchoredPosition = ringCenter + ringRadius * new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                float k = Mathf.Clamp01(1f - Mathf.Abs(offsets[i]) / 2.7f);   // offset 절댓값 기준 prominence
                rt.localScale = srcRt.localScale * Mathf.Lerp(0.65f, 1f, k);

                Image img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
                img.sprite = knob;
                img.raycastTarget = false;
                float dim = Mathf.Lerp(0.35f, 1f, k);
                img.color = new Color(dim, dim, dim, 1f);
                result[i] = img;
            }

            return result;
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
