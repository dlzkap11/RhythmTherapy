using RhythmTherapy.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// ResultScene 루트에 부착. GameManager.EndGame 이 GameSession.LastResult 에 채워 둔 결과를
/// 화면에 표시한다. 인스펙터 필드를 비워두면 씬 오브젝트 이름으로 자동 탐색한다
/// (ACC / Score / Rank / SongName / Perpect / Great / Good / Bad / Miss / FC/AP).
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

    [Header("버튼 (선택, 비워도 무방)")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button lobbyButton;

    private void Start()
    {
        AutoBind();
        Apply(GameSession.LastResult);

        if (retryButton != null) retryButton.onClick.AddListener(Retry);
        if (lobbyButton != null) lobbyButton.onClick.AddListener(GoToLobby);
    }

    private void AutoBind()
    {
        if (scoreText == null) scoreText = FindTextByName("Score");
        if (accText == null) accText = FindTextByName("ACC");
        if (rankText == null) rankText = FindTextByName("Rank");
        if (songNameText == null) songNameText = FindTextByName("SongName");
        if (perfectText == null) perfectText = FindTextByName("Perpect") ?? FindTextByName("Perfect");
        if (greatText == null) greatText = FindTextByName("Great");
        if (goodText == null) goodText = FindTextByName("Good");
        if (badText == null) badText = FindTextByName("Bad");
        if (missText == null) missText = FindTextByName("Miss");
        if (fcApText == null) fcApText = FindTextByName("FC/AP");
    }

    private void Apply(GameResult r)
    {
        if (scoreText != null) scoreText.text = $"SCORE  {r.score}";
        if (accText != null) accText.text = $"{r.accuracy:F2}%";
        if (rankText != null) rankText.text = string.IsNullOrEmpty(r.grade) ? "-" : r.grade;
        if (songNameText != null) songNameText.text = r.songName;
        if (perfectText != null) perfectText.text = r.perfect.ToString();
        if (greatText != null) greatText.text = r.great.ToString();
        if (goodText != null) goodText.text = r.good.ToString();
        if (badText != null) badText.text = r.bad.ToString();
        if (missText != null) missText.text = r.miss.ToString();

        if (fcApText != null)
        {
            fcApText.text = !r.cleared ? "FAILED"
                : r.allPerfect ? "ALL PERFECT"
                : r.fullCombo ? "FULL COMBO"
                : "CLEAR";
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
}
