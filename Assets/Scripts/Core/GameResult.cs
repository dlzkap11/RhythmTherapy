namespace RhythmTherapy.Core
{
    /// <summary>
    /// 곡 종료 시 집계되는 최종 결과. GameManager 가 조립해 GameSession 을 통해
    /// ResultScene 으로 전달한다. (architecture.md §2 결과창)
    /// </summary>
    [System.Serializable]
    public struct GameResult
    {
        public string songName;

        public int score;
        public int maxCombo;

        public int perfect;
        public int great;
        public int good;
        public int bad;
        public int miss;
        public int totalNotes;

        /// <summary>0~100. 판정값(Perfect=100…Miss=0) 합 / 전체 노트 수.</summary>
        public float accuracy;

        /// <summary>정확도 등급 (S/A/B/C/D/F). GradeSystem.Evaluate 결과.</summary>
        public string grade;

        /// <summary>HP 0으로 죽지 않고 곡을 완주했는지.</summary>
        public bool cleared;

        /// <summary>Bad/Miss/미처리 없이 전부 처리(콤보가 끊기지 않음).</summary>
        public bool fullCombo;

        /// <summary>전체 노트를 Perfect 로만 처리.</summary>
        public bool allPerfect;
    }
}
