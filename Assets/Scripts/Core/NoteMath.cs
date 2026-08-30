namespace RhythmTherapy.Core
{
    /// <summary>
    /// 노트 활성화/이동 타이밍 계산. 순수 함수 — MonoBehaviour/씬 비의존.
    ///
    /// 판정은 항상 노래 재생 시간 기준이며, 여기 계산은 노트를 "보기에" 판정선에
    /// 맞게 도착시키기 위한 시각용 값만 다룬다. (RhythmTherapyLab NoteMath 이식)
    /// </summary>
    public static class NoteMath
    {
        /// <summary>노트를 활성화(스폰)해야 하는 노래 시각(ms). 판정시간보다 approach만큼 이르다.</summary>
        public static int SpawnTimeMs(int hitTimeMs, int approachMs) => hitTimeMs - approachMs;

        /// <summary>
        /// 스폰→판정선 이동 진행도.
        /// 스폰 순간 0.0, 판정시간(hitTimeMs)에 정확히 1.0, 그 이후 1.0 초과(화면 밖으로 계속 이동).
        /// approachMs가 0 이하이면 판정시간 전 0, 이후 1 (0 division 방지).
        /// </summary>
        public static double Progress(double songTimeMs, int hitTimeMs, int approachMs)
        {
            if (approachMs <= 0)
                return songTimeMs >= hitTimeMs ? 1.0 : 0.0;

            double spawn = hitTimeMs - approachMs;
            return (songTimeMs - spawn) / approachMs;
        }
    }
}
