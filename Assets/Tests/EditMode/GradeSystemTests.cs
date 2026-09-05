using NUnit.Framework;
using RhythmTherapy.Core;

namespace RhythmTherapy.Core.Tests
{
    /// <summary>
    /// GradeSystem — 정확도(0~100) → 등급 경계값 검증. (GameConfig.GradeThresholds: S95/A90/B80/C70/D60)
    /// </summary>
    public class GradeSystemTests
    {
        [TestCase(100f, "S")]
        [TestCase(95f, "S")]
        [TestCase(94.99f, "A")]
        [TestCase(90f, "A")]
        [TestCase(89.99f, "B")]
        [TestCase(80f, "B")]
        [TestCase(79.99f, "C")]
        [TestCase(70f, "C")]
        [TestCase(69.99f, "D")]
        [TestCase(60f, "D")]
        [TestCase(59.99f, "F")]
        [TestCase(0f, "F")]
        public void Evaluate_ReturnsExpectedGrade(float accuracy, string expected)
        {
            Assert.AreEqual(expected, GradeSystem.Evaluate(accuracy));
        }
    }
}
