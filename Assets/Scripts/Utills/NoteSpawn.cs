using RhythmTherapy.Core;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

public class NoteSpawn : MonoBehaviour
{
    private static readonly ProfilerMarker s_updateMarker = new ProfilerMarker("Rhythm.NoteSpawn.Update");

    [SerializeField] private GameObject[] laneNotes;        // 레인별 스폰 위치
    [SerializeField] private Transform[] laneJudgeLines;    // 레인별 판정선 위치

    [SerializeField] private GameObject notePrefabs;
    [SerializeField] private Sprite[] noteSprites;

    [Tooltip("로비를 안 거치고 GameScene 을 직접 실행할 때 쓸 곡 (SongSelection.Selected 가 있으면 무시)")]
    [SerializeField] private SongDataConfig editorTestSong;

    [SerializeField] private SongData testSong;

    private Queue<GameObject> notePool = new Queue<GameObject>();
    private const int BaselinePoolSize = 30;   // 최소 풀
    private const int MaxPoolSize = 400;        // 버스트 대비 상한
    private int poolCreated;

    // 스폰된 시각 노트를 레인별 FIFO 로 보관. LaneManager 의 노트 데이터 소비와 1:1 대응.
    private Queue<Note>[] activeByLane;

    private int index = 0;

    private void Awake()
    {
        EnsurePoolCapacity(BaselinePoolSize);
    }

    /// <summary>풀 크기를 target 까지 늘린다(줄이지는 않음). [BaselinePoolSize, MaxPoolSize] 로 클램프.</summary>
    private void EnsurePoolCapacity(int target)
    {
        target = Mathf.Clamp(target, BaselinePoolSize, MaxPoolSize);
        for (; poolCreated < target; poolCreated++)
        {
            GameObject note = Instantiate(notePrefabs, transform);
            note.SetActive(false);
            notePool.Enqueue(note);
        }
    }

    private void Start()
    {
        LaneManager.Instance.MakeList(2);

        activeByLane = new Queue<Note>[laneJudgeLines.Length];
        for (int i = 0; i < activeByLane.Length; i++)
            activeByLane[i] = new Queue<Note>();

        LaneManager.Instance.NoteJudgedLane += OnLaneNoteConsumed;
        LaneManager.Instance.NoteAutoMissed += OnLaneNoteConsumed;

        Conductor conductor = Conductor.Instance;

        // 채보 소스: 로비에서 고른 곡 > 에디터 테스트용 곡 > (둘 다 없거나 채보가 비면) 랜덤 생성 폴백.
        SongDataConfig source = SongSelection.Selected != null ? SongSelection.Selected : editorTestSong;

        if (source != null && source.NoteDatas != null && source.NoteDatas.Count > 0)
        {
            testSong = new SongData
            {
                SongID = source.SongID,
                SongName = source.SongName,
                NoteDatas = new List<NoteData>(source.NoteDatas),
            };
            testSong.NoteDatas.Sort((a, b) => a.HitTimeMS.CompareTo(b.HitTimeMS));

            // 로비를 안 거쳤으면 Conductor 재생 대상도 이 곡으로 맞춘다.
            if (SongSelection.Selected == null && conductor != null)
                conductor.SetSong(source);
        }
        else
        {
            testSong = SongDataFactory.CreateRandomSong(
                songId: 4, songName: "Random Song", noteCount: 50, laneCount: 2, bpm: 120f);
        }

        // 노래 재생 전에 레인별 노트 데이터 삽입 완료 (architecture.md §6)
        for (int i = 0; i < testSong.NoteDatas.Count; i++)
            LaneManager.Instance.NoteAdd(testSong.NoteDatas[i]);

        // 버스트 구간(같은 시각에 몰리는 노트)을 커버할 만큼 풀을 확장한다.
        // 활성 구간 ≈ ApproachMs(스폰~판정선) + 자동 Miss 여유. 그 창 안 최대 동시 노트 + 여유분.
        EnsurePoolCapacity(PeakConcurrency(testSong.NoteDatas, GameConfig.ApproachMs + 400) + 8);

        // 곡 종료 시각 = 마지막 노트 판정시간 + 꼬리 재생 여유.
        int lastHitMs = testSong.NoteDatas.Count > 0
            ? testSong.NoteDatas[testSong.NoteDatas.Count - 1].HitTimeMS
            : 0;
        int songEndMs = lastHitMs + GameConfig.SongEndTailMs;

        string songName = source != null ? source.SongName : testSong.SongName;
        int songID = source != null ? source.SongID : testSong.SongID;
        GameManager.Instance.Configure(testSong.NoteDatas.Count, songEndMs, songName, songID);

        // 노트 삽입이 끝난 뒤 재생 시작 (Conductor.Start() 는 NoteSpawn 이 있으면 자동재생 안 함)
        conductor?.PlayConfigured();
    }

    /// <summary>정렬된 노트 목록에서 windowMs 창 안에 동시에 존재하는 노트 최대 수.</summary>
    private static int PeakConcurrency(List<NoteData> sorted, int windowMs)
    {
        int peak = 0;
        int start = 0;
        for (int end = 0; end < sorted.Count; end++)
        {
            while (sorted[end].HitTimeMS - sorted[start].HitTimeMS > windowMs)
                start++;
            peak = Mathf.Max(peak, end - start + 1);
        }
        return peak;
    }

    private void OnDestroy()
    {
        if (LaneManager.Instance == null)
            return;

        LaneManager.Instance.NoteJudgedLane -= OnLaneNoteConsumed;
        LaneManager.Instance.NoteAutoMissed -= OnLaneNoteConsumed;
    }

    private void Update()
    {
        using (s_updateMarker.Auto())
        {
            Conductor conductor = Conductor.Instance;
            if (conductor == null)
                return;

            double songMs = conductor.SongTimeMs;

            // 판정시간 - 이동시간(ApproachMs) 이 되면 노트 활성화
            while (index < testSong.NoteDatas.Count)
            {
                NoteData data = testSong.NoteDatas[index];
                if (NoteMath.SpawnTimeMs(data.HitTimeMS, GameConfig.ApproachMs) > songMs)
                    break;

                SpawnNote(data);
                index++;
            }

            // 판정선을 지나친 노트 자동 소비 → OnLaneNoteConsumed 로 시각 노트 해제
            LaneManager.Instance.CollectAutoMisses((int)songMs);
        }
    }

    private void SpawnNote(NoteData data)
    {
        if (notePool.Count == 0)
        {
            Debug.LogWarning("[NoteSpawn] pool empty");
            return;
        }

        int lane = Mathf.Clamp(data.lane, 0, laneNotes.Length - 1);
        Vector3 spawnPos = laneNotes[lane].transform.position;
        Vector3 targetPos = laneJudgeLines[lane].position;

        GameObject note = notePool.Dequeue();
        note.transform.position = spawnPos;
        note.GetComponent<SpriteRenderer>().sprite = noteSprites[lane];

        Note noteComp = note.GetComponent<Note>();
        noteComp.Bind(data, spawnPos, targetPos, GameConfig.ApproachMs);
        note.SetActive(true);

        activeByLane[lane].Enqueue(noteComp);
    }

    // 해당 레인에서 데이터 노트 1개가 소비됨 → 가장 오래된 시각 노트를 풀로 반환
    private void OnLaneNoteConsumed(int lane)
    {
        if (lane < 0 || lane >= activeByLane.Length || activeByLane[lane].Count == 0)
            return;

        Note note = activeByLane[lane].Dequeue();
        Release(note.gameObject);
    }

    public void Release(GameObject go)
    {
        if (!go.activeSelf)
            return;

        go.SetActive(false);
        notePool.Enqueue(go);
    }
}
