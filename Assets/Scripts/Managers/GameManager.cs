using RhythmTherapy.Core;
using UnityEngine;

/// <summary>
/// 판정 이벤트를 받아 게임플레이 시스템(콤보/점수/HP/정확도)을 조립하는 허브.
/// 각 시스템은 순수 C# 로 분리하고 여기서 조립한다. (architecture.md §6)
///
/// 지금은 콤보만 입주. LaneManager.NoteJudged / NoteAutoMissed 를 채점 입력원으로 쓴다.
/// </summary>
public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private readonly ComboSystem combo = new ComboSystem();

    public int Combo => combo.Current;
    public int MaxCombo => combo.Max;

    /// <summary>콤보 값이 바뀔 때 발생 (인자 = 바뀐 콤보).</summary>
    public event System.Action<int> ComboChanged;

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

    private void OnNoteJudged(int lane)
    {
        combo.RegisterHit();
        ComboChanged?.Invoke(combo.Current);
    }

    private void OnNoteAutoMissed(int lane)
    {
        if (combo.Current == 0)
            return;

        combo.Break();
        ComboChanged?.Invoke(0);
    }

    // 검증용 임시 HUD. 정식 Canvas/TMP HUD 는 점수·HP 와 함께 추후 구현.
    private void OnGUI()
    {
        GUI.Label(new Rect(20, 20, 320, 24), $"COMBO {combo.Current}   (MAX {combo.Max})");
    }
}
