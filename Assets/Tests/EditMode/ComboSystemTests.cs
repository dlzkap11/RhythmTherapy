using NUnit.Framework;
using RhythmTherapy.Core;

namespace RhythmTherapy.Core.Tests
{
    public class ComboSystemTests
    {
        [Test]
        public void RegisterHit_Increments()
        {
            var combo = new ComboSystem();
            Assert.AreEqual(1, combo.RegisterHit());
            Assert.AreEqual(2, combo.RegisterHit());
            Assert.AreEqual(3, combo.RegisterHit());
        }

        [Test]
        public void Break_ResetsToZero()
        {
            var combo = new ComboSystem();
            combo.RegisterHit();
            combo.RegisterHit();
            combo.Break();
            Assert.AreEqual(0, combo.Current);
        }

        [Test]
        public void RegisterHit_AfterBreak_RestartsFromOne()
        {
            var combo = new ComboSystem();
            combo.RegisterHit();
            combo.Break();
            Assert.AreEqual(1, combo.RegisterHit());
        }

        [Test]
        public void Max_TracksHighestStreak()
        {
            var combo = new ComboSystem();
            combo.RegisterHit(); // 1
            combo.RegisterHit(); // 2
            combo.RegisterHit(); // 3
            combo.Break();       // 0
            combo.RegisterHit(); // 1

            Assert.AreEqual(3, combo.Max);
            Assert.AreEqual(1, combo.Current);
        }

        [Test]
        public void Reset_ClearsCurrentAndMax()
        {
            var combo = new ComboSystem();
            combo.RegisterHit();
            combo.RegisterHit();
            combo.Reset();
            Assert.AreEqual(0, combo.Current);
            Assert.AreEqual(0, combo.Max);
        }
    }
}
