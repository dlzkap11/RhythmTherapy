using RhythmTherapy.Core;
using UnityEngine;

public class Note : MonoBehaviour
{
    [SerializeField] private NoteData data;

    // 노트데이터안에 있는 내용이라 굳이 없어도 될 듯 (인스펙터 확인용)
    [SerializeField] private NoteType noteType;
    [SerializeField] private int lane;
    [SerializeField] private int hitTime;
    [SerializeField] private double duration;

    private Vector3 spawnPos;
    private Vector3 targetPos;
    private int approachMs;

    /// <summary>
    /// 노트 데이터 + 이동 구간(스폰→판정선) 바인딩. 위치는 노래 재생 시간으로부터 매 프레임 계산한다.
    /// 화면에서 제거되는 시점은 LaneManager(NoteJudged/NoteAutoMissed)가 결정한다.
    /// </summary>
    public void Bind(NoteData noteData, Vector3 spawn, Vector3 target, int approach)
    {
        data = noteData;
        noteType = data.type;
        lane = data.lane;
        hitTime = data.HitTimeMS;        // 실제 판정시간 (가짜값 대입 제거)
        duration = data.Duration;

        spawnPos = spawn;
        targetPos = target;
        approachMs = approach;

        transform.position = spawnPos;
    }

    private void Update()
    {
        Conductor conductor = Conductor.Instance;
        if (conductor == null)
            return;

        float p = (float)NoteMath.Progress(conductor.SongTimeMs, hitTime, approachMs);
        transform.position = Vector3.LerpUnclamped(spawnPos, targetPos, p);
    }
}
