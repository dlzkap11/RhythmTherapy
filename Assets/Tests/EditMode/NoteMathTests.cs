using NUnit.Framework;
using RhythmTherapy.Core;

namespace RhythmTherapy.Core.Tests
{
    /// <summary>
    /// NoteMath — 노트 활성화/이동 타이밍 (싱크 리스크 영역).
    /// </summary>
    public class NoteMathTests
    {
        [TestCase(5000, 1500, 3500)]
        [TestCase(0, 1500, -1500)]
        [TestCase(2000, 0, 2000)]
        public void SpawnTimeMs(int hit, int approach, int expected)
        {
            Assert.AreEqual(expected, NoteMath.SpawnTimeMs(hit, approach));
        }

        [Test]
        public void Progress_ZeroAtSpawn()
        {
            Assert.AreEqual(0.0, NoteMath.Progress(3500, 5000, 1500), 1e-9);
        }

        [Test]
        public void Progress_OneAtHitTime()
        {
            Assert.AreEqual(1.0, NoteMath.Progress(5000, 5000, 1500), 1e-9);
        }

        [Test]
        public void Progress_HalfwayAtMidpoint()
        {
            Assert.AreEqual(0.5, NoteMath.Progress(4250, 5000, 1500), 1e-9);
        }

        [Test]
        public void Progress_ExceedsOneAfterHit()
        {
            Assert.Greater(NoteMath.Progress(5750, 5000, 1500), 1.0);
        }

        [Test]
        public void Progress_NegativeBeforeSpawn()
        {
            Assert.Less(NoteMath.Progress(3000, 5000, 1500), 0.0);
        }

        [TestCase(4999.0, 5000, 0.0)]
        [TestCase(5000.0, 5000, 1.0)]
        [TestCase(5001.0, 5000, 1.0)]
        public void Progress_ZeroApproach_StepFunction(double songMs, int hit, double expected)
        {
            Assert.AreEqual(expected, NoteMath.Progress(songMs, hit, 0), 1e-9);
        }
    }
}
