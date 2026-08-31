using RhythmTherapy.Core;
using System;
using UnityEngine;

/// <summary>
/// 판정 이벤트를 받아 게임플레이 시스템(콤보/HP/점수/정확도)을 조립하는 허브.
/// 각 시스템은 순수 C# 로 분리하고 여기서 조립한다. (architecture.md §6)
///
/// 입주 시스템: 콤보, HP. LaneManager.NoteJudged / NoteAutoMissed 를 채점 입력원으로 쓴다.
/// </summary>
public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private readonly ComboSystem combo = new ComboSystem();
    private readonly HpSystem hp = new HpSystem(GameConfig.HpMax);
    private readonly JudgeSystem judge = new JudgeSystem();


    private bool hpDepletedFired;

    public int Combo => combo.Current;
    public int MaxCombo => combo.Max;
    public int Hp => hp.Current;
    public int HpMax => hp.Max;
    public JudgeType JudgeAC => judge.JudgeAC;


    // 콤보 이벤트
    public event Action<int> ComboChanged;
    // HP 이벤트
    public event Action<int> HpChanged;
    // HP 0 이벤트
    public event Action HpDepleted;
    // 판정 이벤트
    public event Action<int> Judged;
    public event Action<int> JudgeMissed;
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
        combo.Reset();
        hp.Reset();
        hpDepletedFired = false;

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

    //TODO 판정정확도 넣기
    private void OnNoteJudged(int error, int lane)
    {
        int comboAfter = combo.RegisterHit();
        ComboChanged?.Invoke(comboAfter);
        judge.JudgeAC = judge.AccAss(error);
        Judged?.Invoke(lane);
        // 콤보가 임계값 이상이면 판정 성공마다 HP 회복
        if (comboAfter >= GameConfig.HpHealComboThreshold)
        {
            hp.Heal(GameConfig.HpHealPerHit);
            HpChanged?.Invoke(hp.Current);
        }
    }


    private void OnNoteAutoMissed(int lane)
    {
        // HP 감소는 콤보 상태와 무관하게 항상
        hp.Damage(GameConfig.HpMissDamage);
        HpChanged?.Invoke(hp.Current);
        judge.JudgeAC = judge.AccAss(200);
        Judged?.Invoke(lane);
        if (hp.IsDepleted && !hpDepletedFired)
        {
            hpDepletedFired = true;
            HpDepleted?.Invoke();
            Debug.Log("[GameManager] HP depleted");
        }

        if (combo.Current != 0)
        {
            combo.Break();
            ComboChanged?.Invoke(0);
        }
    }
}
