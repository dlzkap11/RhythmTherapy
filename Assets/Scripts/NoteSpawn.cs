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


    void Update()
    {
        // 생성될 때 어느 레인에 나왔냐에 따라 스프라이트 바꾸기
        spawnTime += Time.deltaTime;

        if(spawnTime >= delay)
        {
            if(notePool.Count > 0)
            {
                int result = Random.Range(0, 2);
                GameObject note = notePool.Dequeue();
                note.transform.position = laneNotes[result].transform.position;
                note.GetComponent<SpriteRenderer>().sprite = noteSprites[result];
                note.SetActive(true);
                
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
