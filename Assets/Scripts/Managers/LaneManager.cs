using System;
using System.Collections.Generic;

using Unity.Profiling;
using UnityEngine;

public class LaneManager : MonoBehaviour
{
    private static readonly ProfilerMarker s_judgeMarker = new ProfilerMarker("Rhythm.Lane.Judge");
    private static readonly ProfilerMarker s_autoMissMarker = new ProfilerMarker("Rhythm.Lane.CollectAutoMisses");

    // 레인마다 리스트를 하나씩 가지고 해당 리스트에 노트배치데이터에 기반하여서 노트들을 저장한다.
    private static LaneManager instance;
    //public static LaneManager Instance => instance;
    public static LaneManager Instance { get { Init(); return instance; } }

    //레인 개수만큼 리스트 수
    [SerializeField] private List<NoteData>[] laneNotes;
    [SerializeField] private int[] currentIndexes;

    // 판정 게이트(ms). 이 범위 밖 입력은 노트를 소비하지 않고 무시된다(헛침).
    // JudgeSystem.BadMS 와 반드시 같은 값이어야 한다. 게이트가 더 넓으면
    // 판정창 안 입력이 JudgeType.Miss 로 떨어져 노트를 파괴하고 HP까지 깎는다.
    private const int JUDGE_RANGE_MS = 150;

    // 노트 1개가 소비될 때 발생 (int = 레인). 시각 노트 해제에 사용.
    public event Action<int, int> NoteJudged;      // 키 입력으로 판정됨
    public event Action<int> NoteJudgedLane;
    public event Action<int> NoteAutoMissed;  // 판정선을 지나쳐 자동 소멸

    static void Init()
    {
        if (instance != null)
            return;

        GameObject go = GameObject.Find("@Managers");
        if (go == null)
            go = new GameObject { name = "@Managers" };

        // @Managers 가 이미 있어도 LaneManager 컴포넌트가 없으면 붙인다 (Awake 가 instance 세팅).
        if (go.GetComponent<LaneManager>() == null)
            go.AddComponent<LaneManager>();

        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Init();
    }

    public void MakeList(int laneCount)
    {
        laneNotes = new List<NoteData>[laneCount];
        currentIndexes = new int[laneCount];

        for (int i = 0; i < laneCount; i++)
        {
            laneNotes[i] = new List<NoteData>();
            currentIndexes[i] = 0;
        }
    }

    // 노트 추가
    public void NoteAdd(NoteData noteData)
    {
        //레인 위치에 맞춰서 리스트에 추가
        laneNotes[noteData.lane].Add(noteData);
    }


    public void FindAndGetNote(int lane, int currentInputTimeMs)
    {
        using (s_judgeMarker.Auto())
        {
            //해당 레인의 노트를 순회
            while (currentIndexes[lane] < laneNotes[lane].Count)
            {
                NoteData note = laneNotes[lane][currentIndexes[lane]];

                // 이미 지나간 노트는 자동 소멸 처리하며 스킵.
                // (입력 콜백은 Update 보다 먼저 발화하므로 그 프레임의 CollectAutoMisses 가
                //  아직 돌지 않은 노트를 여기서 받아낸다.)
                if (note.HitTimeMS < currentInputTimeMs - JUDGE_RANGE_MS)
                {
                    currentIndexes[lane]++;
                    NoteAutoMissed?.Invoke(lane);
                    continue;
                }

                //입력시간 - 노트판정시간
                int error = Mathf.Abs(currentInputTimeMs - note.HitTimeMS);

                //판정 범위 밖
                if (error > JUDGE_RANGE_MS)
                {
                    break;
                }

                // 범위 안 노트를 찾으면 1개만 소비하고 즉시 종료한다.
                // 이 return 이 없으면 키 1회 입력이 판정창 안의 노트를 전부 연속 소비한다.
                currentIndexes[lane]++;
                NoteJudged?.Invoke(error, lane);
                NoteJudgedLane?.Invoke(lane);
                return;
            }
        }
    }

    // 판정선을 지나쳐(판정범위 밖으로 넘어가) 자동 소멸되는 노트들을 소비한다. 매 프레임 호출.
    public void CollectAutoMisses(int songTimeMs)
    {
        using (s_autoMissMarker.Auto())
        {
            if (laneNotes == null)
                return;

            for (int lane = 0; lane < laneNotes.Length; lane++)
            {
                while (currentIndexes[lane] < laneNotes[lane].Count &&
                       songTimeMs - JUDGE_RANGE_MS > laneNotes[lane][currentIndexes[lane]].HitTimeMS)
                {
                    currentIndexes[lane]++;
                    NoteAutoMissed?.Invoke(lane);
                }
            }
        }
    }


    // 레인노트데이터 초기화
    public void LaneClear()
    {
        for(int i = 0;  i < laneNotes.Length; i++)
        {
            laneNotes[i].Clear();
            currentIndexes[i] = 0;
        }
    }

}
