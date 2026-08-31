using System.Collections.Generic;
using RhythmTherapy.Core;
using UnityEngine;

public class NoteSpawn : MonoBehaviour
{
    [SerializeField] private GameObject[] laneNotes;        // 레인별 스폰 위치
    [SerializeField] private Transform[] laneJudgeLines;    // 레인별 판정선 위치

    [SerializeField] private GameObject notePrefabs;
    [SerializeField] private Sprite[] noteSprites;

    // InputManager 가 읽는 공유 재생 시간(초). Conductor 시계로 매 프레임 갱신된다.
    public double playTime;

    [SerializeField] private SongData testSong;

    private Queue<GameObject> notePool = new Queue<GameObject>();
    private const int MAX_POOL_SIZE = 30;

    // 스폰된 시각 노트를 레인별 FIFO 로 보관. LaneManager 의 노트 데이터 소비와 1:1 대응.
    private Queue<Note>[] activeByLane;

    private int index = 0;

    private void Awake()
    {
        for (int i = 0; i < MAX_POOL_SIZE; i++)
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

        testSong = SongDataFactory.CreateRandomSong(
            songId: 4,
            songName: "Random Song",
            noteCount: 50,
            laneCount: 2,
            bpm: 120f);

        // 노래 재생 전에 레인별 노트 데이터 삽입 완료
        for (int i = 0; i < testSong.NoteDatas.Count; i++)
            LaneManager.Instance.NoteAdd(testSong.NoteDatas[i]);
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
        Conductor conductor = Conductor.Instance;
        if (conductor == null)
            return;

        // InputManager.Pop 이 ns.playTime * 1000 으로 입력시간을 만든다 → 같은 시계 공유
        playTime = conductor.SongTime;

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
