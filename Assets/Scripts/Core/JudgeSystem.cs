public enum JudgeType
{
    Perfect = 100,
    Great = 75,
    Good = 50,
    Bad = 25,
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

    }
}
