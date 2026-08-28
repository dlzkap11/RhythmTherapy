using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteSpawn : MonoBehaviour
{
    //랜덤 노트 생성기
    [SerializeField] private GameObject[] laneNotes;
    
    [SerializeField] private GameObject notePrefabs;
    [SerializeField] private Sprite[] noteSprites;
    [SerializeField] private float minDelayTime;
    [SerializeField] float delay;
    private float spawnTime;

    //임시 값
    public double playTime;
    [SerializeField] SongData testSong;


    private Queue<GameObject> notePool = new Queue<GameObject>();
    private const int MAX_POOL_SIZE = 30;

    private void Awake()
    {
        for(int i = 0;  i < MAX_POOL_SIZE; i++)
        {
            GameObject note = Instantiate(notePrefabs, transform);
            note.SetActive(false);
            notePool.Enqueue(note);
        }

        spawnTime = 0f;
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

        int cnt = 0;
        while (cnt < testSong.NoteDatas.Count)
        {
            LaneManager.Instance.NoteAdd(testSong.NoteDatas[cnt]);
            cnt++;
        }
    }

    int index = 0;
    void Update()
    {
        // 임시 노래재생시간
        playTime += Time.deltaTime;


        // 생성될 때 어느 레인에 나왔냐에 따라 스프라이트 바꾸기
        spawnTime += Time.deltaTime;
        if(spawnTime >= delay)
        {
            if(notePool.Count > 0)
            {
                int result = Random.Range(0, 2);
                GameObject note = notePool.Dequeue();
                note.transform.position = laneNotes[testSong.NoteDatas[index].lane].transform.position;
                note.GetComponent<SpriteRenderer>().sprite = noteSprites[testSong.NoteDatas[index].lane];

                // 노트 데이터 보내주기
                // 재생시간 + 3.0f 임시 판정 보정
                if (index >= testSong.NoteDatas.Count)
                    return;
                note.GetComponent<Note>().InitNoteData(testSong.NoteDatas[index], (playTime + 3.0f)*1000f);
                note.SetActive(true);
                index++;
                
            }
            else
            {
                Debug.Log("pool empty");
            }

            spawnTime = 0f;
            delay = minDelayTime + Random.Range(0, 0.5f);
        }
    }
    public void Get()
    {

    }

    public void Release(GameObject gameObject)
    {
        gameObject.SetActive(false);
        notePool.Enqueue(gameObject);
    }
}
