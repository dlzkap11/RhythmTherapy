using System.Linq;

using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

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

            Debug.Log("[ResultSceneSetup] 완료 — ResultView 배선 + 이름 정리 + 정확도 게이지 + Build Settings 등록");
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

            GameObject host = GameObject.Find("ResultHUD")
                ?? GameObject.Find("Result")
                ?? new GameObject("ResultView");

            var view = host.GetComponent<ResultView>();
            if (view == null)
                view = host.AddComponent<ResultView>();

            RenameObject("Perpect", "Perfect");
            ConvertFcApToText();
            BuildRankGauge();
            AddPanelCanvasGroups();
            BuildIntroBanner(host.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>씬에서 oldName 오브젝트를 찾아 newName 으로 리네임. 없으면 무시(idempotent).</summary>
        static void RenameObject(string oldName, string newName)
        {
            GameObject go = FindInScene(oldName);
            if (go != null)
                go.name = newName;
        }

        /// <summary>
        /// FC/AP 표시 오브젝트를 Image → TextMeshProUGUI 로 교체하고 FcAp 로 리네임.
        /// "ALL PERFECT" 문구가 들어갈 폭으로 RectTransform 을 넓힌다. 재실행 안전.
        /// </summary>
        static void ConvertFcApToText()
        {
            GameObject go = FindInScene("FcAp") ?? FindInScene("FC/AP");
            if (go == null)
                return;

            go.name = "FcAp";

            Image image = go.GetComponent<Image>();
            if (image != null)
                Object.DestroyImmediate(image);

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
                tmp = go.AddComponent<TextMeshProUGUI>();

            tmp.text = "CLEAR";
            tmp.fontSize = 28f;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;

            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt != null)
                rt.sizeDelta = new Vector2(260f, 60f);
        }

        /// <summary>
        /// Rank 텍스트 주위에 정확도 원형(radial fill) 링 게이지를 구성한다. 재실행 안전.
        /// 아트 의존 없이 Unity 내장 Knob 스프라이트로 track/fill/hole 3겹 링을 만든다.
        /// 채움값·색은 런타임에 ResultView 가 정확도/등급에 따라 세팅한다.
        /// </summary>
        static void BuildRankGauge()
        {
            GameObject rank = FindInScene("Rank");
            if (rank == null)
                return;

            RectTransform rankRt = rank.GetComponent<RectTransform>();
            Transform parent = rank.transform.parent;

            Sprite knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            const float gaugeSize = 220f;

            RectTransform gauge = FindChild(parent, "RankGauge");
            if (gauge == null)
            {
                GameObject go = new GameObject("RankGauge", typeof(RectTransform));
                gauge = go.GetComponent<RectTransform>();
                gauge.SetParent(parent, false);
            }

            gauge.anchorMin = rankRt.anchorMin;
            gauge.anchorMax = rankRt.anchorMax;
            gauge.pivot = rankRt.pivot;
            gauge.anchoredPosition = rankRt.anchoredPosition;
            gauge.sizeDelta = new Vector2(gaugeSize, gaugeSize);

            Image track = GetOrCreateGaugeImage(gauge, "GaugeTrack", knob);
            Stretch(track.rectTransform);
            track.type = Image.Type.Simple;
            track.color = new Color(1f, 1f, 1f, 0.15f);

            Image fill = GetOrCreateGaugeImage(gauge, "GaugeFill", knob);
            Stretch(fill.rectTransform);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top;
            fill.fillClockwise = false;
            fill.fillAmount = 0f;
            fill.color = new Color(0.3f, 0.9f, 1f, 1f);

            Image hole = GetOrCreateGaugeImage(gauge, "GaugeHole", knob);
            hole.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            hole.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            hole.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            hole.rectTransform.anchoredPosition = Vector2.zero;
            hole.rectTransform.sizeDelta = new Vector2(gaugeSize * 0.66f, gaugeSize * 0.66f);
            hole.type = Image.Type.Simple;
            hole.color = new Color(0.06f, 0.06f, 0.12f, 1f);

            TextMeshProUGUI rankTmp = rank.GetComponent<TextMeshProUGUI>();
            if (rankTmp != null)
                rankTmp.fontSize = 72f;

            // Rank 텍스트가 게이지 위에 그려지도록 맨 뒤 sibling 으로.
            rank.transform.SetAsLastSibling();
        }

        /// <summary>순차 페이드인용으로 결과 패널 3개에 CanvasGroup 을 붙인다(없으면). 재실행 안전.</summary>
        static void AddPanelCanvasGroups()
        {
            foreach (string panelName in new[] { "SongPanel", "RankPanel", "JudgePanel" })
            {
                GameObject go = FindInScene(panelName);
                if (go != null && go.GetComponent<CanvasGroup>() == null)
                    go.AddComponent<CanvasGroup>();
            }
        }

        /// <summary>
        /// "STAGE CLEAR" 인트로 배너를 ResultHUD 밑 최상위 sibling 으로 구성한다. 재실행 안전.
        /// 버스트는 judgment_perfect 스프라이트 목업(전용 아트 생기면 교체).
        /// </summary>
        static void BuildIntroBanner(Transform hud)
        {
            RectTransform banner = FindChild(hud, "IntroBanner");
            if (banner == null)
            {
                GameObject go = new GameObject("IntroBanner", typeof(RectTransform), typeof(CanvasGroup));
                banner = go.GetComponent<RectTransform>();
                banner.SetParent(hud, false);
            }

            if (banner.GetComponent<CanvasGroup>() == null)
                banner.gameObject.AddComponent<CanvasGroup>();

            Stretch(banner);
            banner.SetAsLastSibling();

            Sprite burstSprite = LoadBurstSprite();

            RectTransform burst = FindChild(banner, "BannerBurst");
            if (burst == null)
            {
                GameObject go = new GameObject("BannerBurst", typeof(RectTransform), typeof(Image));
                burst = go.GetComponent<RectTransform>();
                burst.SetParent(banner, false);
            }
            Image burstImg = burst.GetComponent<Image>() ?? burst.gameObject.AddComponent<Image>();
            burstImg.sprite = burstSprite;
            burstImg.raycastTarget = false;
            // 색은 런타임(ResultView)에서 상태별로 티팅한다. 여기선 흰색 기본.
            burstImg.color = new Color(1f, 1f, 1f, 0.55f);
            burst.anchorMin = burst.anchorMax = new Vector2(0.5f, 0.5f);
            burst.pivot = new Vector2(0.5f, 0.5f);
            burst.anchoredPosition = Vector2.zero;
            burst.sizeDelta = new Vector2(900f, 900f);

            RectTransform text = FindChild(banner, "BannerText");
            if (text == null)
            {
                GameObject go = new GameObject("BannerText", typeof(RectTransform));
                text = go.GetComponent<RectTransform>();
                text.SetParent(banner, false);
            }
            TextMeshProUGUI tmp = text.GetComponent<TextMeshProUGUI>() ?? text.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = "STAGE CLEAR";
            tmp.fontSize = 110f;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            text.anchorMin = text.anchorMax = new Vector2(0.5f, 0.5f);
            text.pivot = new Vector2(0.5f, 0.5f);
            text.anchoredPosition = Vector2.zero;
            text.sizeDelta = new Vector2(900f, 160f);
        }

        /// <summary>result_burst 스프라이트 로드. 없으면 생성기로 먼저 만든다.</summary>
        static Sprite LoadBurstSprite()
        {
            const string path = "Assets/Resources/Arts/result_burst.png";
            if (!System.IO.File.Exists(path))
                BurstSpriteGenerator.Generate();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Image GetOrCreateGaugeImage(RectTransform parent, string name, Sprite sprite)
        {
            RectTransform child = FindChild(parent, name);
            if (child == null)
            {
                GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
                child = go.GetComponent<RectTransform>();
                child.SetParent(parent, false);
            }

            Image img = child.GetComponent<Image>();
            if (img == null)
                img = child.gameObject.AddComponent<Image>();

            img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static RectTransform FindChild(Transform parent, string name)
        {
            foreach (Transform t in parent)
            {
                if (t.name == name)
                    return t as RectTransform;
            }

            return null;
        }

        static GameObject FindInScene(string exactName)
        {
            Transform[] transforms = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (Transform tr in transforms)
            {
                if (tr.name == exactName)
                    return tr.gameObject;
            }

            return null;
        }
    }
}
