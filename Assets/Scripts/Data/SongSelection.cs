/// <summary>
/// 로비에서 고른 곡을 게임씬으로 넘기는 정적 전달자. (GameSession 과 같은 패턴)
/// LastIndex 는 결과창 → 로비 복귀 시 캐러셀 위치 복원용.
/// </summary>
public static class SongSelection
{
    public static SongDataConfig Selected;
    public static int LastIndex;
}
