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
        note.GetComponent<Note>().Bind(data, spawnPos, targetPos, GameConfig.ApproachMs, this);
        note.SetActive(true);
    }

    public void Release(GameObject go)
    {
        if (!go.activeSelf)
            return;

        go.SetActive(false);
        notePool.Enqueue(go);
    }
}
