namespace RhythmTherapy.Core
{
    /// <summary>
    /// 정확도(0~100) → 등급(S/A/B/C/D/F). 구간 하한값은 GameConfig.GradeThresholds 에서 관리.
    /// 순수 C# — MonoBehaviour/씬 비의존.
    /// </summary>
    public static class GradeSystem
    {
        public static string Evaluate(float accuracy)
        {
            var thresholds = GameConfig.GradeThresholds;
            for (int i = 0; i < thresholds.Length; i++)
            {
                if (accuracy >= thresholds[i].minAccuracy)
                    return thresholds[i].grade;
            }
            return "F";
        }
    }
}
