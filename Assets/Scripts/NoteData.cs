using UnityEngine;

public enum NoteType
{
    Tap,
    Hold
}

[System.Serializable]
public class NoteData
{
    // 노트 타입
    public NoteType type;
    // 생성될 레인 위치
    [Range(0, 1)] public int lane;
    // 노트 판정 시간
    public int HitTimeMS;
    // 롱노트 판정
    public int Duration;
}
