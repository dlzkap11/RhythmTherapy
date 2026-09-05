using DG.Tweening;
using RhythmTherapy.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// ResultScene 루트에 부착. GameManager.EndGame 이 GameSession.LastResult 에 채워 둔 결과를
/// 인트로 배너("STAGE CLEAR" 등) 후 순차 연출로 표시한다. 인스펙터 필드를 비워두면 씬 오브젝트
/// 이름으로 자동 탐색한다 (Score / ACC / Rank / SongName / 판정별 / FcAp / GaugeFill /
/// SongPanel / RankPanel / JudgePanel / IntroBanner / BannerText / BannerBurst).
/// </summary>
public class ResultView : MonoBehaviour
{
    [Header("비워두면 이름으로 자동 탐색")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI accText;
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI songNameText;
    [SerializeField] private TextMeshProUGUI perfectText;
    [SerializeField] private TextMeshProUGUI greatText;
    [SerializeField] private TextMeshProUGUI goodText;
    [SerializeField] private TextMeshProUGUI badText;
    [SerializeField] private TextMeshProUGUI missText;
    [SerializeField] private TextMeshProUGUI fcApText;
    [SerializeField] private Image rankGaugeFill;

    [Header("연출")]
    [SerializeField] private CanvasGroup bannerGroup;
    [SerializeField] private TextMeshProUGUI bannerText;
    [SerializeField] private RectTransform bannerBurst;
    [SerializeField] private CanvasGroup songGroup;
    [SerializeField] private CanvasGroup rankGroup;
    [SerializeField] private CanvasGroup judgeGroup;

    [Header("버튼 (선택, 비워도 무방)")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button lobbyButton;

    private Sequence sequence;

    private void Start()
    {
        AutoBind();

        GameResult r = GameSession.LastResult;
        PopulateTexts(r);

        if (retryButton != null) retryButton.onClick.AddListener(Retry);
        if (lobbyButton != null) lobbyButton.onClick.AddListener(GoToLobby);

        // 연출용 오브젝트가 배선 안 됐으면(씬 미구성) 즉시 전체 표시 + 게이지만 트윈.
        if (bannerGroup == null || bannerText == null)
        {
            SetGroupAlpha(songGroup, 1f);
            SetGroupAlpha(rankGroup, 1f);
            SetGroupAlpha(judgeGroup, 1f);
            FillGauge(r);
            return;
        }

        SetGroupAlpha(songGroup, 0f);
        SetGroupAlpha(rankGroup, 0f);
        SetGroupAlpha(judgeGroup, 0f);
        bannerGroup.alpha = 0f;
        if (rankGaugeFill != null) rankGaugeFill.fillAmount = 0f;
        if (fcApText != null) fcApText.transform.localScale = Vector3.one;

        if (r.cleared)
            PlayClearSequence(r);
        else
            PlayFailSequence();
    }

    private void OnDestroy() => sequence?.Kill();

    private void AutoBind()
    {
        if (scoreText == null) scoreText = FindTextByName("Score");
        if (accText == null) accText = FindTextByName("ACC");
        if (rankText == null) rankText = FindTextByName("Rank");
        if (songNameText == null) songNameText = FindTextByName("SongName");
        if (perfectText == null) perfectText = FindTextByName("Perfect");
        if (greatText == null) greatText = FindTextByName("Great");
        if (goodText == null) goodText = FindTextByName("Good");
        if (badText == null) badText = FindTextByName("Bad");
        if (missText == null) missText = FindTextByName("Miss");
        if (fcApText == null) fcApText = FindTextByName("FcAp");
        if (rankGaugeFill == null) rankGaugeFill = FindImageByName("GaugeFill");

        if (bannerGroup == null) bannerGroup = FindByName<CanvasGroup>("IntroBanner");
        if (bannerText == null) bannerText = FindTextByName("BannerText");
        if (bannerBurst == null) bannerBurst = FindByName<RectTransform>("BannerBurst");
        if (songGroup == null) songGroup = FindByName<CanvasGroup>("SongPanel");
        if (rankGroup == null) rankGroup = FindByName<CanvasGroup>("RankPanel");
        if (judgeGroup == null) judgeGroup = FindByName<CanvasGroup>("JudgePanel");
    }

    private void PopulateTexts(GameResult r)
    {
        if (scoreText != null) scoreText.text = $"SCORE \n {r.score}";
        if (accText != null) accText.text = $"{r.accuracy:F2}%";
        if (rankText != null) rankText.text = string.IsNullOrEmpty(r.grade) ? "-" : r.grade;
        if (songNameText != null) songNameText.text = r.songName;
        if (perfectText != null) perfectText.text = "Perfect : " + r.perfect.ToString();
        if (greatText != null) greatText.text = "Great : " + r.great.ToString();
        if (goodText != null) goodText.text = "Good : " + r.good.ToString();
        if (badText != null) badText.text = "Bad : " + r.bad.ToString();
        if (missText != null) missText.text = "Miss : " + r.miss.ToString();

        if (fcApText != null)
        {
            bool showFcAp = r.allPerfect || r.fullCombo;
            fcApText.gameObject.SetActive(showFcAp);
            if (showFcAp)
                fcApText.text = r.allPerfect ? "ALL PERFECT" : "FULL COMBO";
        }
    }

    private void PlayClearSequence(GameResult r)
    {
        bannerText.text = "STAGE CLEAR";
        bannerText.color = Color.white;

        // 카운트업/순차 등장 대상 초기화.
        if (scoreText != null) scoreText.text = "SCORE \n 0";
        if (accText != null) accText.text = "0.00%";
        TextMeshProUGUI[] judgeLines = { perfectText, greatText, goodText, badText, missText };
        foreach (TextMeshProUGUI line in judgeLines)
        {
            if (line == null) continue;
            line.alpha = 0f;
            line.transform.localScale = Vector3.one;
        }

        sequence?.Kill();
        sequence = DOTween.Sequence();

        sequence.Append(bannerGroup.DOFade(1f, 0.2f));
        if (bannerBurst != null)
        {
            bannerBurst.localScale = Vector3.zero;
            bannerBurst.localEulerAngles = new Vector3(0f, 0f, -20f);
            sequence.Join(bannerBurst.DOScale(1.15f, 0.35f).SetEase(Ease.OutBack));
            sequence.Join(bannerBurst.DORotate(new Vector3(0f, 0f, 10f), 0.6f));
        }
        sequence.Join(bannerText.transform.DOScale(1f, 0.3f).From(0.7f).SetEase(Ease.OutBack));

        sequence.AppendInterval(GameConfig.ResultIntroHoldSeconds);
        sequence.Append(bannerGroup.DOFade(0f, GameConfig.ResultIntroBannerMoveSeconds));
        sequence.Join(bannerGroup.transform.DOLocalMoveY(120f, GameConfig.ResultIntroBannerMoveSeconds).SetRelative());

        float fade = GameConfig.ResultPanelFadeSeconds;
        float stagger = GameConfig.ResultPanelStaggerSeconds;
        AppendGroupFade(songGroup, fade, 0f);
        AppendGroupFade(rankGroup, fade, stagger);

        // Score/ACC 카운트업 + 게이지 채움을 한 블록에서 동시에.
        // rankGroup 페이드인 뒤이므로 시퀀스에 항상 선행 스텝이 있어 첫 트윈도 Join 해도 안전.
        if (scoreText != null)
        {
            int scoreShown = 0;
            sequence.Join(DOTween.To(() => scoreShown, v =>
            {
                scoreShown = v;
                scoreText.text = $"SCORE \n {v}";
            }, r.score, GameConfig.ResultCountUpSeconds).SetEase(Ease.OutCubic));
        }
        if (accText != null)
        {
            float accShown = 0f;
            sequence.Join(DOTween.To(() => accShown, v =>
            {
                accShown = v;
                accText.text = $"{v:F2}%";
            }, r.accuracy, GameConfig.ResultCountUpSeconds).SetEase(Ease.OutCubic));
        }
        if (rankGaugeFill != null)
        {
            rankGaugeFill.color = GradeColor(r.grade);
            sequence.Join(rankGaugeFill.DOFillAmount(Mathf.Clamp01(r.accuracy / 100f), GameConfig.RankGaugeFillSeconds)
                .SetEase(Ease.OutCubic));
        }

        AppendGroupFade(judgeGroup, fade, stagger);

        // 판정 갯수 한 줄씩 순차 팝 등장 (숫자는 이미 최종값).
        foreach (TextMeshProUGUI line in judgeLines)
        {
            if (line == null) continue;
            sequence.Append(line.DOFade(1f, GameConfig.ResultJudgeLineFadeSeconds));
            sequence.Join(line.transform.DOScale(1f, GameConfig.ResultJudgeLineFadeSeconds).From(0.6f).SetEase(Ease.OutBack));
            sequence.AppendInterval(GameConfig.ResultJudgeLineStaggerSeconds);
        }

        if ((r.allPerfect || r.fullCombo) && fcApText != null)
        {
            sequence.Append(fcApText.transform.DOScale(1f, 0.3f).From(0f).SetEase(Ease.OutBack));
        }
    }

    private void PlayFailSequence()
    {
        // 실패 시엔 결과값을 보여주지 않고 배너만 띄운 채 정지한다.
        // TODO: 로비씬(LobyScene)이 생기면 여기서 일정 시간 후 로비로 이동시킨다.
        bannerText.text = "STAGE FAILED";
        bannerText.color = new Color(1f, 0.4f, 0.4f);

        sequence?.Kill();
        sequence = DOTween.Sequence();
        sequence.Append(bannerGroup.DOFade(1f, 0.3f));
        if (bannerBurst != null)
        {
            bannerBurst.localScale = Vector3.zero;
            sequence.Join(bannerBurst.DOScale(0.9f, 0.4f).SetEase(Ease.OutCubic));
        }
        sequence.Join(bannerText.transform.DOScale(1f, 0.35f).From(0.7f).SetEase(Ease.OutBack));
    }

    private void AppendGroupFade(CanvasGroup group, float duration, float delayBefore)
    {
        if (group == null)
            return;

        if (delayBefore > 0f)
            sequence.AppendInterval(delayBefore);

        sequence.Append(group.DOFade(1f, duration));
    }

    private void FillGauge(GameResult r)
    {
        if (rankGaugeFill == null)
            return;

        rankGaugeFill.color = GradeColor(r.grade);
        rankGaugeFill.fillAmount = 0f;
        rankGaugeFill.DOFillAmount(Mathf.Clamp01(r.accuracy / 100f), GameConfig.RankGaugeFillSeconds).SetEase(Ease.OutCubic);
    }

    private static void SetGroupAlpha(CanvasGroup group, float alpha)
    {
        if (group != null)
            group.alpha = alpha;
    }

    /// <summary>등급 문자 → 게이지 채움 색. 단순 표시색이라 GameConfig 로 빼지 않는다.</summary>
    private static Color GradeColor(string grade)
    {
        switch (grade)
        {
            case "S": return new Color(1f, 0.84f, 0.2f);   // 골드
            case "A": return new Color(0.6f, 1f, 0.3f);    // 라임
            case "B": return new Color(0.3f, 0.9f, 1f);    // 시안
            case "C": return new Color(0.35f, 0.6f, 1f);   // 블루
            case "D": return new Color(0.7f, 0.7f, 0.7f);  // 그레이
            case "F": return new Color(1f, 0.35f, 0.35f);  // 레드
            default: return Color.white;
        }
    }

    public void Retry() => SceneManager.LoadScene("GameScene");

    public void GoToLobby() => SceneManager.LoadScene("LobyScene");

    /// <summary>
    /// "/" 등 경로 구분자로 오인될 수 있는 이름도 안전하게 찾기 위해 GameObject.Find 대신
    /// 씬 전체를 순회해 정확히 일치하는 이름을 찾는다.
    /// </summary>
    private static TextMeshProUGUI FindTextByName(string exactName)
    {
        var texts = FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in texts)
        {
            if (t.gameObject.name == exactName)
                return t;
        }

        var transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var tr in transforms)
        {
            if (tr.name != exactName)
                continue;

            var t = tr.GetComponentInChildren<TextMeshProUGUI>(true);
            if (t != null)
                return t;
        }

        return null;
    }

    private static Image FindImageByName(string exactName)
    {
        Image[] images = FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Image img in images)
        {
            if (img.gameObject.name == exactName)
                return img;
        }

        return null;
    }

    private static T FindByName<T>(string exactName) where T : Component
    {
        T[] comps = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (T comp in comps)
        {
            if (comp.gameObject.name == exactName)
                return comp;
        }

        return null;
    }
}
