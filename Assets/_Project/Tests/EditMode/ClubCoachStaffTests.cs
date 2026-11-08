using NUnit.Framework;
using System.Collections.Generic;
using TacticaUniversalis.Domain;

namespace TacticaUniversalis.Tests.EditMode
{
    public sealed class ClubCoachStaffTests
    {
        [Test]
        public void RatingGrade_UsesDesignBands()
        {
            Assert.AreEqual("S", ClubCoachStaffRules.FormatRatingGrade(96));
            Assert.AreEqual("A+", ClubCoachStaffRules.FormatRatingGrade(91));
            Assert.AreEqual("E", ClubCoachStaffRules.FormatRatingGrade(10));
        }

        [Test]
        public void OpponentRead_PlayerFacingOnlyShowsFailure()
        {
            Assert.IsNull(ClubCoachStaffRules.PlayerFacingOpponentReadMessage(OpponentReadOutcome.Success));
            Assert.IsNull(ClubCoachStaffRules.PlayerFacingOpponentReadMessage(OpponentReadOutcome.Misread));
            Assert.AreEqual(
                "解讀對手戰術失敗。",
                ClubCoachStaffRules.PlayerFacingOpponentReadMessage(OpponentReadOutcome.Failure));
        }

        [Test]
        public void CreateFromDefinition_CopiesAssistantAttrs()
        {
            var def = new CareerCoachDefinition
            {
                id = "a1",
                role = "assistant",
                clubId = "navi_2021",
                trainingExecution = 84,
                tacticalDesign = 70,
                vodReview = 75,
                coachSynergy = 55,
                monthlySalary = 9000
            };
            CoachStaffState state = ClubCoachStaffRules.CreateFromDefinition(def);
            Assert.AreEqual("assistant", state.role);
            Assert.AreEqual(84, state.trainingExecution);
            Assert.AreEqual(55, state.coachSynergy);
        }

        [Test]
        public void StaffTrainingMultiplier_UsesAssistantExecution()
        {
            var state = new CareerState
            {
                coaches = new List<CoachStaffState>
                {
                    new CoachStaffState
                    {
                        coachId = "a",
                        clubId = "c1",
                        role = "assistant",
                        trainingExecution = 100
                    }
                }
            };
            double mult = ClubCoachStaffRules.GetStaffTrainingMultiplier(state, "c1");
            Assert.AreEqual(1.20d, mult, 0.001d);
        }

        [Test]
        public void LowSynergy_ReducesTacticalCardBonus()
        {
            var high = new CareerState
            {
                coaches = new List<CoachStaffState>
                {
                    new CoachStaffState { coachId = "h", clubId = "c1", role = "head_coach", tactics = 80 },
                    new CoachStaffState
                    {
                        coachId = "a", clubId = "c1", role = "assistant",
                        tacticalDesign = 90, coachSynergy = 90
                    }
                }
            };
            var low = new CareerState
            {
                coaches = new List<CoachStaffState>
                {
                    new CoachStaffState { coachId = "h", clubId = "c1", role = "head_coach", tactics = 80 },
                    new CoachStaffState
                    {
                        coachId = "a", clubId = "c1", role = "assistant",
                        tacticalDesign = 90, coachSynergy = 20
                    }
                }
            };
            Assert.Greater(
                ClubCoachStaffRules.GetTacticalCardBonusMultiplier(high, "c1"),
                ClubCoachStaffRules.GetTacticalCardBonusMultiplier(low, "c1"));
        }

        [Test]
        public void VersionUpdate_DecaysAnalystAccumulation()
        {
            var state = new CareerState
            {
                coaches = new List<CoachStaffState>
                {
                    new CoachStaffState
                    {
                        coachId = "an",
                        clubId = "c1",
                        role = "analyst",
                        dataAccumulation = 60,
                        patchAdaptation = 0,
                        opponentIntel = new List<CoachOpponentIntelEntry>
                        {
                            new CoachOpponentIntelEntry { opponentClubId = "opp", meetings = 2, depth = 50 }
                        }
                    }
                }
            };
            ClubCoachStaffRules.ApplyGameVersionUpdate(state);
            Assert.AreEqual(52, state.coaches[0].dataAccumulation);
            Assert.AreEqual(42, state.coaches[0].opponentIntel[0].depth);
        }

        [Test]
        public void PromoteAssistant_RedistributesIntoHeadCoach()
        {
            var state = new CareerState
            {
                coaches = new List<CoachStaffState>
                {
                    new CoachStaffState
                    {
                        coachId = "a",
                        clubId = "c1",
                        role = "assistant",
                        experience = 80,
                        trainingExecution = 90,
                        tacticalDesign = 70,
                        vodReview = 60,
                        coachSynergy = 80,
                        monthlySalary = 9000
                    }
                }
            };
            Assert.IsTrue(ClubCoachStaffRules.TryPromoteAssistantToHeadCoach(state, "c1", out _));
            Assert.AreEqual("head_coach", state.coaches[0].role);
            Assert.Greater(state.coaches[0].training, 0);
            Assert.Greater(state.coaches[0].tactics, 0);
        }
    }
}
