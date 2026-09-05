namespace RhythmTherapy.Core
{
    /// <summary>
    /// 씬 전환 간 결과 데이터 전달용 정적 캐리어. GameScene → ResultScene.
    /// DontDestroyOnLoad 오브젝트 없이, 순수 정적 필드로 값만 넘긴다.
    /// </summary>
    public static class GameSession
    {
        public static GameResult LastResult;
    }
}
