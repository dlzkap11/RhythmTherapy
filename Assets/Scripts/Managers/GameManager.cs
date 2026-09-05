using RhythmTherapy.Core;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 판정 이벤트를 받아 게임플레이 시스템(콤보/HP/점수/정확도)을 조립하는 허브.
/// 각 시스템은 순수 C# 로 분리하고 여기서 조립한다. (architecture.md §6)
///
/// 입주 시스템: 콤보, HP, 점수, 판정. LaneManager.NoteJudged / NoteAutoMissed 를 채점 입력원으로 쓴다.
/// 곡 종료(Conductor.SongTimeMs 가 songEndMs 도달, 또는 HP 0)를 감지해 결과를 집계하고
/// GameSession 을 통해 ResultScene 으로 전달한다.
/// </summary>
public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private readonly ComboSystem combo = new ComboSystem();
    private readonly HpSystem hp = new HpSystem(GameConfig.HpMax);
    private readonly JudgeSystem judge = new JudgeSystem();
    private readonly ScoreSystem score = new ScoreSystem();

    private bool hpDepletedFired;
    private bool gameEnded;
    private bool configured;
    private int noteCount;

    // 종료 판정 / 결과 집계용
    private int totalNotes;
    private int songEndMs;
    private string songName = string.Empty;
    private int perfectCount, greatCount, goodCount, badCount, missCount;

    public int Combo => combo.Current;
    public int MaxCombo => combo.Max;
    public int Hp => hp.Current;
    public int HpMax => hp.Max;
    public JudgeType JudgeAC => judge.JudgeAC;
    public int Score => score.CurrentScore;
    public int MaxScore => score.MaxScore;


    // 콤보 이벤트
    public event Action<int> ComboChanged;
    // HP 이벤트
    public event Action<int> HpChanged;
    // HP 0 이벤트
    public event Action HpDepleted;
    // 판정 이벤트
    public event Action<int> Judged;
    public event Action<int> JudgeMissed;
    // 점수 이벤트
    public event Action<int, int> ScoreChanged;
    // 결과 확정 이벤트 (ResultScene 로드 직전)
    public event Action<GameResult> Finished;



    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        GameObject go = GameObject.Find("@Managers");
        if (go == null)
            go = new GameObject("@Managers");

        go.AddComponent<GameManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        LaneManager lm = LaneManager.Instance;
        lm.NoteJudged += OnNoteJudged;
        lm.NoteAutoMissed += OnNoteAutoMissed;
    }

    private void OnDestroy()
    {
        if (LaneManager.Instance != null)
        {
            LaneManager.Instance.NoteJudged -= OnNoteJudged;
            LaneManager.Instance.NoteAutoMissed -= OnNoteAutoMissed;
        }

        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (gameEnded || !configured)
            return;

        Conductor conductor = Conductor.Instance;
        if (conductor != null && conductor.IsPlaying && conductor.SongTimeMs >= songEndMs)
            EndGame(cleared: true);
    }

    /// <summary>
    /// NoteSpawn 이 채보 준비를 마친 뒤 호출. 새 판(첫 시작/재도전) 시작마다 전체 시스템을 리셋한다.
    /// </summary>
    /// <param name="totalNoteCount">이번 곡의 전체 노트 수 (정확도/풀콤보 판정 기준).</param>
    /// <param name="songEndTimeMs">Conductor.SongTimeMs 기준 곡 종료 시각.</param>
    /// <param name="currentSongName">결과창에 표시할 곡 이름.</param>
    public void Configure(int totalNoteCount, int songEndTimeMs, string currentSongName)
    {
        totalNotes = totalNoteCount;
        songEndMs = songEndTimeMs;
        songName = currentSongName ?? string.Empty;

        combo.Reset();
        hp.Reset();
        score.Reset();
        judge.Reset();
        noteCount = 0;
        perfectCount = greatCount = goodCount = badCount = missCount = 0;
        hpDepletedFired = false;
        gameEnded = false;
        configured = true;
    }

    private void OnNoteJudged(int error, int lane)
    {
        noteCount++;
        judge.JudgeAC = judge.AccAss(error);
        Judged?.Invoke(lane);

        switch (judge.JudgeAC)
        {
            case JudgeType.Perfect: perfectCount++; break;
            case JudgeType.Great: greatCount++; break;
            case JudgeType.Good: goodCount++; break;
            case JudgeType.Bad: badCount++; break;
            case JudgeType.Miss: missCount++; break;
        }

        // Bad/Miss 는 콤보를 끊는다 (formulas-and-tests.md "콤보 증감"). Perfect/Great/Good 만 콤보 유지.
        if (judge.JudgeAC == JudgeType.Bad || judge.JudgeAC == JudgeType.Miss)
        {
            if (combo.Current != 0)
                ComboChanged?.Invoke(0);
            combo.Break();

            // Miss 는 자동 미처리와 동일하게 HP 감소. Bad 는 HP 불변.
            if (judge.JudgeAC == JudgeType.Miss)
                ApplyHpDamage(GameConfig.HpMissDamage);
        }
        else
        {
            int comboAfter = combo.RegisterHit();
            ComboChanged?.Invoke(comboAfter);

            // 콤보가 임계값 이상이면 판정 성공마다 HP 회복
            if (comboAfter >= GameConfig.HpHealComboThreshold)
            {
                hp.Heal(GameConfig.HpHealPerHit);
                HpChanged?.Invoke(hp.Current);
            }
        }

        int currentScore = score.SumScore((int)judge.JudgeAC, combo.Current);
        ScoreChanged?.Invoke(currentScore, noteCount);
    }


    private void OnNoteAutoMissed(int lane)
    {
        noteCount++;
        missCount++;

        judge.RegisterMiss();
        Judged?.Invoke(lane);
        ScoreChanged?.Invoke(score.CurrentScore, noteCount);

        // HP 감소는 콤보 상태와 무관하게 항상
        ApplyHpDamage(GameConfig.HpMissDamage);

        if (combo.Current != 0)
        {
            combo.Break();
            ComboChanged?.Invoke(0);
        }
    }

    private void ApplyHpDamage(int amount)
    {
        hp.Damage(amount);
        HpChanged?.Invoke(hp.Current);

        if (hp.IsDepleted && !hpDepletedFired)
        {
            hpDepletedFired = true;
            HpDepleted?.Invoke();
            Debug.Log("[GameManager] HP depleted");
            EndGame(cleared: false);
        }
    }

    /// <summary>
    /// 곡 종료(클리어 또는 HP0 실패) 확정. 결과를 집계하고 잠시 대기 후 ResultScene 을 로드한다.
    /// </summary>
    private void EndGame(bool cleared)
    {
        if (gameEnded)
            return;

        gameEnded = true;
        score.UpdateMaxScore();

        float accuracy = totalNotes > 0 ? judge.score / (float)totalNotes : 0f;
        var result = new GameResult
        {
            songName = songName,
            score = score.CurrentScore,
            maxCombo = combo.Max,
            perfect = perfectCount,
            great = greatCount,
            good = goodCount,
            bad = badCount,
            miss = missCount,
            totalNotes = totalNotes,
            accuracy = accuracy,
            grade = GradeSystem.Evaluate(accuracy),
            cleared = cleared,
            fullCombo = cleared && badCount == 0 && missCount == 0,
            allPerfect = cleared && totalNotes > 0 && perfectCount == totalNotes,
        };

        Debug.Log($"[GameManager] 곡 종료 — cleared={cleared} score={result.score} acc={result.accuracy:F2} grade={result.grade}");

        Finished?.Invoke(result);
        StartCoroutine(ShowResultThenLoad(result));
    }

    private IEnumerator ShowResultThenLoad(GameResult result)
    {
        yield return new WaitForSeconds(GameConfig.ResultDelaySeconds);

        GameSession.LastResult = result;
        SceneManager.LoadScene("ResultScene");
    }
}
