using System.Threading;
using UnityEngine;

public class Note : MonoBehaviour
{
    [SerializeField] private NoteData data;

    // 노트데이터안에 있는 내용이라 굳이 없어도 될 듯
    [SerializeField] private NoteType noteType;
    [SerializeField] private int lane;
    [SerializeField] private int hitTime;
    [SerializeField] private double duration;

    // 노트 데이터 받아오기
    public void InitNoteData(NoteData noteData, double playTimeDouble)
    {
        data = noteData;
        noteType = data.type;
        lane = data.lane;
        //hitTime = data.HitTimeMS;
        hitTime = (int)playTimeDouble;

    }
}
