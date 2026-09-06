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

        // --- HP (임시 확정값, 밸런싱 전) ---
        /// <summary>최대 HP.</summary>
        public const int HpMax = 100;
        /// <summary>노트를 놓칠 때(자동 Miss) 1회당 HP 감소량.</summary>
        public const int HpMissDamage = 10;
        /// <summary>회복 조건 충족 시 판정 성공 1회당 HP 회복량.</summary>
        public const int HpHealPerHit = 2;
        /// <summary>이 콤보 이상일 때부터 판정 성공 시 HP 회복.</summary>
        public const int HpHealComboThreshold = 10;

        // --- 곡 종료 판정 (임시 확정값) ---
        /// <summary>마지막 노트 판정시간 이후 곡 종료로 간주하기까지 여유(ms). 판정/음악 꼬리가 재생될 시간.</summary>
        public const int SongEndTailMs = 1500;
        /// <summary>종료 감지 후 마지막 판정을 보여주고 ResultScene 으로 넘어가기까지 대기(초).</summary>
        public const float ResultDelaySeconds = 1.5f;

        // --- 연출 타이밍 (표시용, 밸런싱 무관) ---
        /// <summary>ResultScene 인트로 배너("STAGE CLEAR" 등)가 화면에 머무는 시간(초).</summary>
        public const float ResultIntroHoldSeconds = 0.6f;
        /// <summary>인트로 배너가 슬라이드로 등장·퇴장하는 데 걸리는 시간(초).</summary>
        public const float ResultIntroBannerMoveSeconds = 0.35f;
        /// <summary>결과 패널(곡/랭크/판정)이 페이드인되는 시간(초).</summary>
        public const float ResultPanelFadeSeconds = 0.35f;
        /// <summary>결과 패널들이 순차 등장할 때 패널 사이 지연(초).</summary>
        public const float ResultPanelStaggerSeconds = 0.08f;
        /// <summary>정확도 원형 게이지가 0에서 목표치까지 차오르는 시간(초).</summary>
        public const float RankGaugeFillSeconds = 0.8f;
        /// <summary>Score/ACC 숫자가 0에서 최종값까지 카운트업되는 시간(초).</summary>
        public const float ResultCountUpSeconds = 0.7f;
        /// <summary>결과창 판정 갯수 한 줄이 페이드인되는 시간(초).</summary>
        public const float ResultJudgeLineFadeSeconds = 0.18f;
        /// <summary>결과창 판정 갯수 줄이 순차 등장할 때 줄 사이 간격(초).</summary>
        public const float ResultJudgeLineStaggerSeconds = 0.1f;
        /// <summary>GameScene 풀콤보 연출 배너가 화면에 머무는 시간(초).</summary>
        public const float FullComboHoldSeconds = 0.7f;
        /// <summary>씬 전환 검정 페이드 편도(아웃 또는 인) 시간(초).</summary>
        public const float SceneFadeSeconds = 0.3f;

        /// <summary>정확도(0~100) 구간 하한 → 등급. 높은 구간부터 순서대로 검사.</summary>
        public struct GradeThreshold
        {
            public string grade;
            public float minAccuracy;

            public GradeThreshold(string grade, float minAccuracy)
            {
                this.grade = grade;
                this.minAccuracy = minAccuracy;
            }
        }

        public static readonly GradeThreshold[] GradeThresholds =
        {
            new GradeThreshold("S", 95f),
            new GradeThreshold("A", 90f),
            new GradeThreshold("B", 80f),
            new GradeThreshold("C", 70f),
            new GradeThreshold("D", 60f),
        };
    }
}
