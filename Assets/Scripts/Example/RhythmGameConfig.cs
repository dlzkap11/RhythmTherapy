// RhythmGameConfig.cs
// 리듬게임 전역 설정 (ScriptableObject). 인스펙터에서 값 조정 가능.
using System.Collections.Generic;
using UnityEngine;

namespace PixelBeat
{
    public enum JudgeRank { Perfect, Great, Miss }

    [CreateAssetMenu(fileName = "RhythmGameConfig", menuName = "PixelBeat/RhythmGameConfig")]
    public class RhythmGameConfig : ScriptableObject
    {
        [Header("Lanes")]
        [Range(1, 8)] public int laneCount = 4;
        public float laneWidth = 84f;
        public KeyCode[] laneKeys = { KeyCode.D, KeyCode.F, KeyCode.J, KeyCode.K };
        public Color[] laneColors =
        {
            new Color(0.27f, 0.94f, 1f),   // cyan
            new Color(1f, 0.88f, 0.30f),   // yellow
            new Color(1f, 0.30f, 0.62f),   // pink
            new Color(0.30f, 1f, 0.56f),   // green
        };

        [Header("Timing (seconds)")]
        [Tooltip("PERFECT 판정 윈도우 (절대시간)")]
        public float perfectWindow = 0.045f;
        [Tooltip("GREAT 판정 윈도우 (절대시간)")]
        public float greatWindow = 0.090f;

        [Header("Movement")]
        [Tooltip("노트가 1초에 이동하는 픽셀(UGUI 단위)")]
        public float noteSpeed = 360f;
        [Tooltip("판정선의 Y 위치 (노트 레이어 기준 로컬 Y)")]
        public float judgeLineY = -250f;
        [Tooltip("노트가 판정선을 지난 뒤 제거되기까지 거리(픽셀)")]
        public float missDistance = 30f;

        [Header("Scoring")]
        public int perfectScore = 100;
        public int greatScore = 60;
        public float comboMultiplier = 0.5f;

        [Header("Health")]
        public float maxHealth = 100f;
        public float missHealthLoss = 7f;

        [Header("Song")]
        public float songLength = 95f;
        public string songName = "NEON DRIVE";
        public float bpm = 130f;
    }
}
