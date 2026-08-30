using System;

namespace RhythmTherapy.Core
{
    /// <summary>
    /// HP 트래커. 순수 C# — MonoBehaviour/씬 비의존. [0, Max] 로 클램프.
    ///
    /// 현재 규칙(GameManager 에서 조립):
    /// - 노트를 놓침(자동 Miss) → Damage
    /// - 콤보 HpHealComboThreshold 이상에서 판정 성공 → Heal
    /// </summary>
    public sealed class HpSystem
    {
        public int Max { get; }
        public int Current { get; private set; }
        public bool IsDepleted => Current <= 0;

        public HpSystem(int max)
        {
            Max = max;
            Current = max;
        }

        public void Reset() => Current = Max;

        /// <summary>HP 감소. 반환값 = 감소 후 현재 HP.</summary>
        public int Damage(int amount)
        {
            Current = Math.Max(0, Current - Math.Max(0, amount));
            return Current;
        }

        /// <summary>HP 회복. 반환값 = 회복 후 현재 HP.</summary>
        public int Heal(int amount)
        {
            Current = Math.Min(Max, Current + Math.Max(0, amount));
            return Current;
        }
    }
}
