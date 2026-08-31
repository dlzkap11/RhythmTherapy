using Codice.Client.BaseCommands;
using UnityEngine;
using static Codice.CM.Common.CmCallContext;

namespace RhythmTherapy.Core
{
    public sealed class ScoreSystem
    {
        public int CurrentScore {  get; private set; }
        public int MaxScore { get; private set; }


        public void Reset()
        {
            CurrentScore = 0;
        }

        public int SumScore(int score, int combo)
        {
            // 50 콤보마다 0.5 배 증가 (정수 나누기)
            int bonusMultiplier = combo / 50;
            float multiplier = 1f + bonusMultiplier * 0.5f;
            return CurrentScore += (int)(score * multiplier);
        }

        // 기록갱신
        public void UpdateMaxScore()
        {
            if(CurrentScore > MaxScore)
            {
                MaxScore = CurrentScore;
            }
        }

    }
}
