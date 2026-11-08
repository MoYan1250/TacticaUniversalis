using NUnit.Framework;
using TacticaUniversalis.Domain;

namespace TacticaUniversalis.Tests.EditMode
{
    public sealed class ClubEstateSatisfactionTests
    {
        [Test]
        public void Stance_UsesThirtyAndSeventyThresholds()
        {
            Assert.AreEqual(ClubEstateSatisfactionStance.Angry,
                ClubEstateSatisfactionRules.GetStance(29));
            Assert.AreEqual(ClubEstateSatisfactionStance.Neutral,
                ClubEstateSatisfactionRules.GetStance(50));
            Assert.AreEqual(ClubEstateSatisfactionStance.Loyal,
                ClubEstateSatisfactionRules.GetStance(71));
        }

        [Test]
        public void BoardSyncsWithLegacyField()
        {
            var club = new ClubState { clubId = "test", boardSatisfaction = 42 };
            ClubEstateSatisfactionRules.EnsureNormalized(club);
            ClubEstateSatisfactionRules.ApplyDelta(club, ClubEstateId.Board, 5);
            Assert.AreEqual(47, club.boardSatisfaction);
            Assert.AreEqual(47, ClubEstateSatisfactionRules.Get(club, ClubEstateId.Board));
        }

        [Test]
        public void ParseEstateId_AcceptsSnakeCaseKeys()
        {
            Assert.IsTrue(ClubEstateSatisfactionRules.TryParseEstateId("coaching_staff", out ClubEstateId id));
            Assert.AreEqual(ClubEstateId.CoachingStaff, id);
        }
    }
}
