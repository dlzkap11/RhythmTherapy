namespace RhythmTherapy.Core
{
    /// <summary>
    /// 게임 전역 튜닝 값. 지금은 싱크 검증에 필요한 값만 둔다.
    /// 판정창/HP/점수 등 나머지 수치는 추후 별도 설정 자산으로 분리 (skill 규칙 6).
    /// </summary>
    public static class GameConfig
    {
        /// <summary>노트가 스폰 위치에서 판정선까지 이동하는 데 걸리는 시각(ms). 판정에는 영향 없음(순수 시각).</summary>
        public const int ApproachMs = 1500;
    }
}
