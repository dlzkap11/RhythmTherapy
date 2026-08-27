using UnityEngine;

public enum NoteType
{
    Tap,
    Hold
}

[System.Serializable]
public class NoteData
{
    [Range(0, 3)] public int lane;
    public NoteType NoteType;

    // 노트 판정 시간
    public double HitTime;

    // 롱노트 판정
    public double Duration;


}
