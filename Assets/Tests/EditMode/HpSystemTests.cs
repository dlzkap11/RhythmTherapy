using NUnit.Framework;
using RhythmTherapy.Core;

namespace RhythmTherapy.Core.Tests
{
    public class HpSystemTests
    {
        [Test]
        public void StartsAtMax()
        {
            var hp = new HpSystem(100);
            Assert.AreEqual(100, hp.Current);
            Assert.AreEqual(100, hp.Max);
            Assert.IsFalse(hp.IsDepleted);
        }

        [Test]
        public void Damage_Reduces()
        {
            var hp = new HpSystem(100);
            Assert.AreEqual(90, hp.Damage(10));
            Assert.AreEqual(80, hp.Damage(10));
        }

        [Test]
        public void Damage_ClampsAtZero_AndIsDepleted()
        {
            var hp = new HpSystem(100);
            for (int i = 0; i < 12; i++)
                hp.Damage(10);
            Assert.AreEqual(0, hp.Current);
            Assert.IsTrue(hp.IsDepleted);
        }

        [Test]
        public void Heal_ClampsAtMax()
        {
            var hp = new HpSystem(100);
            hp.Damage(5);   // 95
            hp.Heal(2);     // 97
            Assert.AreEqual(97, hp.Current);
            hp.Heal(50);    // clamp 100
            Assert.AreEqual(100, hp.Current);
        }

        [Test]
        public void Reset_RestoresMax()
        {
            var hp = new HpSystem(100);
            hp.Damage(40);
            hp.Reset();
            Assert.AreEqual(100, hp.Current);
        }
    }
}
