using NUnit.Framework;

namespace MagnetEscape.Tests
{
    public sealed class RankTests
    {
        [TestCase(35f, 0, 1, "S")]
        [TestCase(35.01f, 0, 1, "A")]
        [TestCase(10f, 1, 1, "A")]
        [TestCase(52.5f, 2, 1, "A")]
        [TestCase(52.51f, 2, 1, "B")]
        [TestCase(10f, 3, 1, "B")]
        [TestCase(70f, 5, 1, "B")]
        [TestCase(70.01f, 5, 1, "C")]
        [TestCase(10f, 6, 1, "C")]
        [TestCase(65f, 0, 4, "S")]
        public void RankUsesTimeAndDeaths(float seconds, int deaths, int stage, string expected)
        {
            Assert.That(GameSession.EvaluateRank(seconds, deaths, stage), Is.EqualTo(expected));
        }
    }
}
