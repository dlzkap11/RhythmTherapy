public enum JudgeType
{
    Perfect = 100,
    Great = 90,
    Good = 70,
    Bad = 40,
    Miss = 0,
}

namespace RhythmTherapy.Core
{
    public sealed class JudgeSystem
    {
        // 노트 처리시 정확도 판정
        public JudgeType JudgeAC;
        public int score;

        // 각 판정 범위(ms)
        private int PerfectMS = 25;
        private int GreatMS = 50;
        private int GoodMS = 80;
        private int BadMS = 120;

        /// <summary> 정확도 판정. 반환값 = 판정 후 정확도 타입 /// </summary>
        public JudgeType AccAss(int error)
        {
            JudgeAC = error <= PerfectMS ? JudgeType.Perfect :
                error <= GreatMS ? JudgeType.Great :
                error <= GoodMS ? JudgeType.Good :
                error <= BadMS ? JudgeType.Bad :
                             JudgeType.Miss;

            JudgeAvg();

            return JudgeAC;
        }


        public void JudgeAvg()
        {
            score += (int)JudgeAC;
        }

        /// <summary>노트 미처리(자동 Miss) 등, 오차값 없이 Miss 로 확정할 때 사용.</summary>
        public JudgeType RegisterMiss()
        {
            JudgeAC = JudgeType.Miss;
            JudgeAvg();
            return JudgeAC;
        }

        /// <summary>다음 곡 시작 전 누적값 초기화.</summary>
        public void Reset()
        {
            JudgeAC = default;
            score = 0;
        }
    }
}
