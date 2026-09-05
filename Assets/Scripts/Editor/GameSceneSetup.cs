using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RhythmTherapy.EditorTools
{
    /// <summary>
    /// GameScene 에 풀콤보 연출용 HUD(FullComboHUD + FullComboView)를 구성한다.
    /// 에디터 메뉴 또는 MCP(RhythmTherapy.EditorTools.GameSceneSetup.Setup())로 1회 실행. 재실행 안전.
    /// </summary>
    public static class GameSceneSetup
    {
        const string GameScenePath = "Assets/Scenes/GameScene.unity";

        [MenuItem("RhythmTherapy/Setup/Wire Game Scene")]
        public static void Setup()
        {
            string original = EditorSceneManager.GetActiveScene().path;

            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            BuildFullComboHud();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            if (!string.IsNullOrEmpty(original) && original != GameScenePath)
                EditorSceneManager.OpenScene(original, OpenSceneMode.Single);

            Debug.Log("[GameSceneSetup] 완료 — FullComboHUD 배선");
        }

        static void BuildFullComboHud()
        {
            GameObject hud = Find("FullComboHUD");
            if (hud == null)
                hud = new GameObject("FullComboHUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            Canvas canvas = hud.GetComponent<Canvas>();
            if (canvas == null)
                canvas = hud.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = hud.GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = hud.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            if (hud.GetComponent<GraphicRaycaster>() == null)
                hud.AddComponent<GraphicRaycaster>();

            CanvasGroup group = hud.GetComponent<CanvasGroup>();
            if (group == null)
                group = hud.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            Sprite burstSprite = LoadArtSprite("Assets/Resources/Arts/judgment_perfect.png", "judgment_perfect_0");

            RectTransform burst = FindChild(hud.transform, "FullComboBurst");
            if (burst == null)
            {
                GameObject go = new GameObject("FullComboBurst", typeof(RectTransform), typeof(Image));
                burst = go.GetComponent<RectTransform>();
                burst.SetParent(hud.transform, false);
            }
            Image burstImg = burst.GetComponent<Image>() ?? burst.gameObject.AddComponent<Image>();
            burstImg.sprite = burstSprite;
            burstImg.raycastTarget = false;
            burstImg.color = new Color(1f, 1f, 1f, 0.45f);
            Center(burst, new Vector2(800f, 800f));

            RectTransform label = FindChild(hud.transform, "FullComboLabel");
            if (label == null)
            {
                GameObject go = new GameObject("FullComboLabel", typeof(RectTransform));
                label = go.GetComponent<RectTransform>();
                label.SetParent(hud.transform, false);
            }
            TextMeshProUGUI tmp = label.GetComponent<TextMeshProUGUI>() ?? label.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = "FULL COMBO";
            tmp.fontSize = 96f;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            Center(label, new Vector2(1000f, 150f));

            FullComboView view = hud.GetComponent<FullComboView>() ?? hud.AddComponent<FullComboView>();
            SerializedObject so = new SerializedObject(view);
            so.FindProperty("group").objectReferenceValue = group;
            so.FindProperty("label").objectReferenceValue = tmp;
            so.FindProperty("burst").objectReferenceValue = burst;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Center(RectTransform rt, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
        }

        static Sprite LoadArtSprite(string assetPath, string spriteName)
        {
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                if (o is Sprite s && s.name == spriteName)
                    return s;
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        static GameObject Find(string name)
        {
            foreach (Transform tr in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (tr.name == name)
                    return tr.gameObject;
            }

            return null;
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
    }
}
