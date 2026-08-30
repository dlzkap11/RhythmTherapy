using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

public class LaneManager : MonoBehaviour
{
    // 레인마다 리스트를 하나씩 가지고 해당 리스트에 노트배치데이터에 기반하여서 노트들을 저장한다.
    private static LaneManager instance;
    //public static LaneManager Instance => instance;
    public static LaneManager Instance { get { Init(); return instance; } }

    //레인 개수만큼 리스트 수
    [SerializeField] private List<NoteData>[] laneNotes;
    [SerializeField] private int[] currentIndexes;

    // 임시 판정 범위(ms). 판정 등급이 정해지면 설정값으로 분리 예정.
    private const int JUDGE_RANGE_MS = 200;

    // 노트 1개가 소비될 때 발생 (int = 레인). 시각 노트 해제에 사용.
    public event System.Action<int> NoteJudged;      // 키 입력으로 판정됨
    public event System.Action<int> NoteAutoMissed;  // 판정선을 지나쳐 자동 소멸

    static void Init()
    {
        if (instance == null)
        {

            GameObject go = GameObject.Find("@Managers");
            if (go == null)
            {
                go = new GameObject { name = "@Managers" };
                go.AddComponent<LaneManager>();
            }
            DontDestroyOnLoad(go);
        }
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


    public int FindAndGetNote(int lane, int currentInputTimeMs)
    {
        //해당 레인의 노트를 순회
        while (currentIndexes[lane] < laneNotes[lane].Count)
        {
            NoteData note = laneNotes[lane][currentIndexes[lane]];

            Debug.Log($"입력시간 :{currentInputTimeMs}, 판정시간 : {note.HitTimeMS}");
            // 이미 지나간 노트는 자동 소멸 처리하며 스킵 (커서 전진 = 이벤트 1회, CollectAutoMisses 와 일관)
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

            // 범위 안 노트를 찾으면 소비하고 반환
            currentIndexes[lane]++;
            NoteJudged?.Invoke(lane);
            return error;

        }


        // 순회 후 못 찾으면 널...
        return -1;
    }

    // 판정선을 지나쳐(판정범위 밖으로 넘어가) 자동 소멸되는 노트들을 소비한다. 매 프레임 호출.
    public void CollectAutoMisses(int songTimeMs)
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
