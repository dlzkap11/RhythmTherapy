using NUnit.Framework.Internal.Commands;
using RhythmTherapy.Core;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class JudgementView : MonoBehaviour
{
    [SerializeField] GameObject judgePrefab;
    string[] judgeName = { "Perfect", "Great", "Good", "Bad", "Miss" };
    // 여러개가 동시에 나올 수도 있다.
    // 애초에 지금 레인마다 판정UI가 나와줘야함 나오고 바로 사라진다 하더라도 2개는 중복
    // 가능성 있음
    // 어차피 텍스트니까 프리팹은 하나를 두고
    // 생성되면서 해당 판정값을 넣어주면 딱딱딱 아님?

    private Queue<GameObject> judgePool = new Queue<GameObject>();
    private const int MAX_POOL_SIZE = 30;


    private void Awake()
    {
        for (int i = 0; i < MAX_POOL_SIZE; i++)
        {
            // 나중에 꺼낼때 자식 둘 중 하나에 넣어주기
            GameObject judge = Instantiate(judgePrefab, transform);
            judge.SetActive(false);
            judgePool.Enqueue(judge);
        }
    }


    private void Start()
    {
        GameManager.Instance.Judged += JudgementDeath;
    }

    private void OnDestroy()
    {
        GameManager.Instance.Judged -= JudgementDeath;
    }

    // 레인을 받고 해당 레인 위치의 판정 보내기
    public void JudgementDeath(int lane)
    {
        SpawnJudge(lane);
    }

    private void SpawnJudge(int lane)
    {
        if (judgePool.Count == 0)
        {
            Debug.LogWarning("[judgePool] pool empty");
            return;
        }

        Transform parentLane = transform.GetChild(lane);
        Vector3 spawnPos = parentLane.position;

        GameObject judge = judgePool.Dequeue();
        judge.transform.position = spawnPos;
        judge.transform.SetParent(parentLane);
        judge.GetComponent<TextMeshProUGUI>().text = GameManager.Instance.JudgeAC.ToString();

        judge.SetActive(true);
        // TODO DOTween같은 거로 생성되면 위로 조금씩 움직이면서 투명해지면서 사라지기
        StartCoroutine(DeactivateAfterDelay(judge, 1f));
    }

    private IEnumerator DeactivateAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);

        Release(obj);
    }

    public void Release(GameObject go)
    {
        if (!go.activeSelf)
            return;

        go.SetActive(false);
        judgePool.Enqueue(go);
    }

}
