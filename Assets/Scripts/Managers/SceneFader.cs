using System.Collections;

using RhythmTherapy.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 씬 전환 시 화면 전체를 검정으로 페이드 아웃 → 로드 → 페이드 인 한다.
/// @SceneFader GameObject 에 붙는 DontDestroyOnLoad 싱글턴. UI 는 전부 코드로 구성한다.
/// </summary>
public sealed class SceneFader : MonoBehaviour
{
    public static SceneFader Instance { get; private set; }

    private CanvasGroup _group;
    private bool _transitioning;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        new GameObject("@SceneFader").AddComponent<SceneFader>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildOverlay();
    }

    private void BuildOverlay()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        gameObject.AddComponent<GraphicRaycaster>();

        _group = gameObject.AddComponent<CanvasGroup>();
        _group.alpha = 0f;
        _group.blocksRaycasts = false;

        GameObject imageObj = new GameObject("Black", typeof(RectTransform), typeof(Image));
        imageObj.transform.SetParent(transform, false);

        RectTransform rt = imageObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        imageObj.GetComponent<Image>().color = Color.black;
    }

    /// <summary>Instance 가 있으면 페이드 전환, 없으면(부트스트랩 전/테스트) 즉시 로드.</summary>
    public static void Load(string sceneName)
    {
        if (Instance != null)
            Instance.FadeToScene(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    public void FadeToScene(string sceneName)
    {
        if (_transitioning)
            return;

        _transitioning = true;
        StartCoroutine(FadeRoutine(sceneName));
    }

    private IEnumerator FadeRoutine(string sceneName)
    {
        _group.blocksRaycasts = true;

        yield return Fade(1f);

        SceneManager.LoadScene(sceneName);
        yield return null;

        yield return Fade(0f);

        _group.blocksRaycasts = false;
        _transitioning = false;
    }

    private IEnumerator Fade(float target)
    {
        float speed = GameConfig.SceneFadeSeconds > 0f ? 1f / GameConfig.SceneFadeSeconds : float.MaxValue;

        while (!Mathf.Approximately(_group.alpha, target))
        {
            _group.alpha = Mathf.MoveTowards(_group.alpha, target, speed * Time.unscaledDeltaTime);
            yield return null;
        }

        _group.alpha = target;
    }
}
