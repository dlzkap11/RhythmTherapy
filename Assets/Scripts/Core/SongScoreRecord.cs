namespace RhythmTherapy.Core
{
    /// <summary>
    /// 곡 1개에 대한 누적 기록. 여러 번 플레이해도 파일에는 곡당 이 레코드 하나만 남는다.
    /// JSON 직렬화 대상이라 필드는 공개 + 직렬화 가능한 타입만 둔다.
    /// </summary>
    [System.Serializable]
    public sealed class SongScoreRecord
    {
        /// <summary>곡 식별자. 저장/조회의 키.</summary>
        public int songId;

        /// <summary>역대 최고 점수.</summary>
        public int bestScore;

        /// <summary>최고 점수를 기록한 판의 정확도(0~100).</summary>
        public float bestAccuracy;

        /// <summary>최고 점수를 기록한 판의 등급 (S/A/B/C/D/F).</summary>
        public string bestGrade;

        /// <summary>최고 점수를 기록한 판의 최대 콤보.</summary>
        public int maxCombo;

        /// <summary>한 번이라도 풀콤보를 달성했는지. 이후 판에서 실패해도 유지.</summary>
        public bool fullCombo;

        /// <summary>한 번이라도 올 퍼펙트를 달성했는지. 이후 판에서 실패해도 유지.</summary>
        public bool allPerfect;

        /// <summary>이 곡을 플레이한 총 횟수.</summary>
        public int playCount;

        /// <summary>HP 0으로 죽지 않고 완주한 횟수.</summary>
        public int clearCount;

        /// <summary>마지막으로 플레이한 시각. DateTime.UtcNow.ToString("o") (ISO 8601).</summary>
        public string lastPlayedIso;
    }
}
