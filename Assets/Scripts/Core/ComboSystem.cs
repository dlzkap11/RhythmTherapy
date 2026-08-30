namespace RhythmTherapy.Core
{
    /// <summary>
    /// 콤보 카운터. 순수 C# — MonoBehaviour/씬 비의존.
    ///
    /// 현재 규칙(판정 등급 미도입): 판정 성공 1회 → +1, 미처리(자동 Miss) → 0 리셋.
    /// 등급이 생기면 Bad 도 리셋 대상으로 확장. (formulas-and-tests.md "콤보 증감")
    /// </summary>
    public sealed class ComboSystem
    {
        public int Current { get; private set; }
        public int Max { get; private set; }

        public void Reset()
        {
            Current = 0;
            Max = 0;
        }

        /// <summary>판정 성공 1회 — 콤보 +1, 최대치 갱신, 갱신 후 값 반환.</summary>
        public int RegisterHit()
        {
            Current++;
            if (Current > Max)
                Max = Current;
            return Current;
        }

        /// <summary>미처리/Miss — 콤보 0 리셋.</summary>
        public void Break()
        {
            Current = 0;
        }
    }
}
