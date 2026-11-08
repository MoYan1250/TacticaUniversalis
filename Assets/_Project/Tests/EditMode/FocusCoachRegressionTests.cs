using System.IO;
using System.Linq;
using NUnit.Framework;
using TacticaUniversalis.Application;
using TacticaUniversalis.Domain;
using TacticaUniversalis.Infrastructure;
using UnityEngine;

namespace TacticaUniversalis.Tests
{
    public sealed class FocusCoachRegressionTests
    {
        private ContentDatabaseAsset _asset;
        private ContentDatabase _content;

        [SetUp]
        public void SetUp()
        {
            string root = Path.Combine(UnityEngine.Application.dataPath, "_Project", "Data", "JSON");
            _asset = ScriptableObject.CreateInstance<ContentDatabaseAsset>();
            ContentJsonParser.ParseInto(
                _asset,
                File.ReadAllText(Path.Combine(root, "players.json")),
                File.ReadAllText(Path.Combine(root, "teams.json")),
                File.ReadAllText(Path.Combine(root, "traits.json")),
                File.ReadAllText(Path.Combine(root, "dust2.json")),
                File.ReadAllText(Path.Combine(root, "tactical_cards.json")),
                File.ReadAllText(Path.Combine(root, "focus_cards.json")),
                File.ReadAllText(Path.Combine(root, "coach_cards.json")));
            _content = _asset.BuildDomain();
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_asset);

        [Test]
        public void FullCanonicalContent_IncludesFocusAndCoachCards()
        {
            Assert.That(ContentValidator.Validate(_asset), Is.Empty);
            Assert.That(_content.FocusCards.Count, Is.EqualTo(8));
            Assert.That(_content.CoachCards.Count, Is.EqualTo(9));
        }

        [Test]
        public void Timeout_PenalizesBothTeamsAndConsumesAllowance()
        {
            MatchState state = MakeState();
            float beforeA = state.TeamA.Players[0].Form;
            float beforeB = state.TeamB.Players[0].Form;
            Assert.That(TimeoutCoachRules.TryCallTimeout(state.TeamA, state), Is.True);
            Assert.That(state.TeamA.TimeoutsRemaining, Is.EqualTo(TimeoutRules.RegularTimeoutsPerTeam - 1));
            Assert.That(state.TeamA.Players[0].Form,
                Is.EqualTo(FormRules.Clamp(beforeA - TimeoutRules.BothTeamsFormPenalty)));
            Assert.That(state.TeamB.Players[0].Form,
                Is.EqualTo(FormRules.Clamp(beforeB - TimeoutRules.BothTeamsFormPenalty)));
        }

        [Test]
        public void Timeout_AllowsUpToThreeCoachCardsInOneWindow()
        {
            MatchState state = MakeState();
            CommandPointRules.Initialize(state.TeamA);
            state.TeamA.CommandPoints = 10;
            CoachCardDefinition m = _content.CoachCards["coach_base_mentality"];
            CoachCardDefinition s = _content.CoachCards["coach_base_stamina"];
            CoachCardDefinition h = _content.CoachCards["coach_base_shout"];
            Assert.That(TimeoutCoachRules.TryApplyCoach(state.TeamA, m, "s1mple", state), Is.True);
            Assert.That(TimeoutCoachRules.TryApplyCoach(state.TeamA, s, "s1mple", state), Is.True);
            Assert.That(TimeoutCoachRules.TryApplyCoach(state.TeamA, h, null, state), Is.True);
            Assert.That(state.TeamA.CoachUsedThisTimeout, Has.Count.EqualTo(3));
            Assert.That(TimeoutCoachRules.CanUseCoach(state.TeamA, m), Is.False);
        }

        [Test]
        public void FormDelta_IsClampedForCombat()
        {
            Assert.That(FormRules.AimReactionDelta(95), Is.EqualTo(FormRules.AimReactionBonusCap));
            Assert.That(FormRules.AimReactionDelta(10), Is.EqualTo(FormRules.AimReactionPenaltyFloor));
        }

        private MatchState MakeState()
        {
            TeamDefinition a = _content.Teams["navi_2021"];
            TeamDefinition b = _content.Teams["gambit_2021"];
            var teamA = new TeamRuntime(a,
                a.playerIds.Select(id => new PlayerRuntime(_content.Players[id], 60)));
            var teamB = new TeamRuntime(b,
                b.playerIds.Select(id => new PlayerRuntime(_content.Players[id], 60)));
            return new MatchState(teamA, teamB);
        }
    }
}
