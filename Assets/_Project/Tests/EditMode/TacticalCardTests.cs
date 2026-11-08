using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TacticaUniversalis.Application;
using TacticaUniversalis.Domain;
using TacticaUniversalis.Infrastructure;
using UnityEngine;

namespace TacticaUniversalis.Tests
{
    public sealed class TacticalCardTests
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
                File.ReadAllText(Path.Combine(root, "tactical_cards.json")));
            _content = _asset.BuildDomain();
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_asset);

        [Test]
        public void TacticalContent_HasSixUniversalAndSixPerTeam()
        {
            Assert.That(ContentValidator.Validate(_asset), Is.Empty);
            Assert.That(_content.TacticalCards.Count, Is.EqualTo(18));
            Assert.That(_content.TacticalCards.Values.Count(x => x.teamScope == "universal"), Is.EqualTo(6));
            Assert.That(_content.TacticalCards.Values.Count(x => x.teamScope == "navi_2021"), Is.EqualTo(6));
            Assert.That(_content.TacticalCards.Values.Count(x => x.teamScope == "gambit_2021"), Is.EqualTo(6));
        }

        [Test]
        public void CommandPoints_RefillAndCapAtMaximum()
        {
            TeamRuntime team = MakeTeam("navi_2021");
            CommandPointRules.Initialize(team);
            Assert.That(team.CommandPoints, Is.EqualTo(CommandPointRules.StartingPoints));
            Assert.That(CommandPointRules.TrySpend(team, 3), Is.True);
            Assert.That(team.CommandPoints, Is.EqualTo(CommandPointRules.StartingPoints - 3));
            CommandPointRules.Add(team, 3);
            CommandPointRules.Add(team, 20);
            Assert.That(team.CommandPoints, Is.EqualTo(CommandPointRules.MaximumPoints));
        }

        [Test]
        public void Deck_DrawsTwo_ReshufflesAndKeepsTwelveUniqueCards()
        {
            TeamRuntime team = MakeTeam("navi_2021");
            var random = new SeededRandom(7);
            TacticalDeckRules.Initialize(team, _content.TacticalCards.Values, random);
            Assert.That(team.TacticalDeck.DrawPile, Has.Count.EqualTo(12));
            Assert.That(team.TacticalDeck.DrawPile.Distinct().Count(), Is.EqualTo(12));
            TacticalDeckRules.BeginRound(team, random);
            Assert.That(team.TacticalDeck.Hand, Has.Count.EqualTo(2));

            team.TacticalDeck.DiscardPile.AddRange(team.TacticalDeck.DrawPile);
            team.TacticalDeck.DrawPile.Clear();
            int before = team.TacticalDeck.DiscardPile.Count;
            TacticalDeckRules.Draw(team, 1, random);
            Assert.That(before, Is.GreaterThan(0));
            Assert.That(team.TacticalDeck.DrawPile.Count + team.TacticalDeck.DiscardPile.Count, Is.EqualTo(before - 1));
        }

        [Test]
        public void Deck_EnforcesThreeCardsCostAndHandLimit()
        {
            TeamRuntime team = MakeTeam("navi_2021");
            CommandPointRules.Initialize(team);
            team.TacticalDeck.Hand.AddRange(new[]
            {
                "baseline_control", "eco_gamble", "aggressive_push", "mid_info_control",
                "fake_rotate", "tactical_adjust", "navi_b1t_rising", "navi_boombl4_setup"
            });
            Assert.That(TacticalDeckRules.ToggleSelection(team, "baseline_control", _content.TacticalCards), Is.True);
            Assert.That(TacticalDeckRules.ToggleSelection(team, "eco_gamble", _content.TacticalCards), Is.True);
            Assert.That(TacticalDeckRules.ToggleSelection(team, "aggressive_push", _content.TacticalCards), Is.True);
            Assert.That(TacticalDeckRules.CanSelect(team, "mid_info_control", _content.TacticalCards), Is.False);
            Assert.That(TacticalDeckRules.Commit(team, _content.TacticalCards), Is.False, "Overflow must be discarded first.");
            Assert.That(TacticalDeckRules.DiscardFromHand(team, "fake_rotate"), Is.True);
            Assert.That(TacticalDeckRules.Commit(team, _content.TacticalCards), Is.True);
            Assert.That(team.CommandPoints, Is.EqualTo(CommandPointRules.StartingPoints - 3));
        }

        [Test]
        public void CounterMatrix_IsSymmetricForStrongMatchups()
        {
            TacticalCardDefinition aggressive = _content.TacticalCards["aggressive_push"];
            TacticalCardDefinition discipline = _content.TacticalCards["gambit_iron_wall"];
            CounterModifiers weak = TacticalCounterResolver.Resolve(new[] { aggressive }, new[] { discipline });
            CounterModifiers strong = TacticalCounterResolver.Resolve(new[] { discipline }, new[] { aggressive });
            Assert.That(weak.outcome, Is.EqualTo("even"));
            Assert.That(strong.outcome, Is.EqualTo("advantage"));
            Assert.That(strong.firepower, Is.EqualTo(0.10f));
            Assert.That(strong.opening, Is.EqualTo(0.10f));
        }

        [Test]
        public void PlanFactory_ClampsStackedBonuses()
        {
            TeamRuntime team = MakeTeam("navi_2021");
            var plan = new TacticalPlan { equipmentLevel = EquipmentLevel.ForceBuy };
            TacticalPlanFactory.ApplyCards(plan, team, new[]
            {
                _content.TacticalCards["navi_s1mple_core"],
                _content.TacticalCards["navi_electronic_duo"],
                _content.TacticalCards["aggressive_push"]
            });
            Assert.That(plan.openingBonus, Is.EqualTo(0.30f));
            Assert.That(plan.firepowerBonus, Is.EqualTo(0.20f));
            Assert.That(plan.teamworkBonus, Is.EqualTo(0.13f).Within(0.001f));
            Assert.That(plan.clutchBonus, Is.LessThanOrEqualTo(0.15f));
        }

        [Test]
        public void HobbitSuppressesOpponentBonusesByFortyPercent()
        {
            var plan = new TacticalPlan
            {
                openingBonus = 0.20f,
                firepowerBonus = 0.10f,
                teamworkBonus = 0.10f,
                clutchBonus = 0.10f
            };
            TacticalPlanFactory.ApplyHobbitSuppression(
                plan, new[] { _content.TacticalCards["gambit_hobbit_veteran"] });
            Assert.That(plan.openingBonus, Is.EqualTo(0.12f).Within(0.001f));
            Assert.That(plan.firepowerBonus, Is.EqualTo(0.06f).Within(0.001f));
            Assert.That(plan.teamworkBonus, Is.EqualTo(0.06f).Within(0.001f));
            Assert.That(plan.clutchBonus, Is.EqualTo(0.06f).Within(0.001f));
        }

        [Test]
        public void Boombl4_DrawsFourAdditionalCards()
        {
            var session = new InteractiveQuickMatchSession(
                _content, 8, new RuleBasedTacticalPlanner(), new FastRoundSimulator());
            session.PrepareRound();
            TeamRuntime navi = session.TeamById("navi_2021");
            navi.TacticalDeck.Hand.Clear();
            navi.TacticalDeck.Hand.Add("navi_boombl4_setup");
            int before = navi.TacticalDeck.Hand.Count;
            Assert.That(session.ToggleTacticalCard("navi_2021", "navi_boombl4_setup"), Is.True);
            Assert.That(navi.TacticalDeck.Hand.Count, Is.EqualTo(before + 4));
        }

        [Test]
        public void B1tDeathPenalty_AppliesToNextPlanOnce()
        {
            TeamRuntime team = MakeTeam("navi_2021");
            var active = new TacticalPlan
            {
                specialRuleKeys = new[] { "b1t_death_penalty" }
            };
            TacticalSpecialRules.ApplyDeath(team, active, "b1t");
            Assert.That(team.NextRoundFirepowerPenalty, Is.EqualTo(0.05f));

            var next = new TacticalPlan { equipmentLevel = EquipmentLevel.ForceBuy };
            TacticalPlanFactory.ApplyCards(next, team, new[] { _content.TacticalCards["baseline_control"] });
            Assert.That(next.firepowerBonus, Is.EqualTo(-0.05f));
            Assert.That(team.NextRoundFirepowerPenalty, Is.Zero);
        }

        [Test]
        public void CounterResolver_AllCardPairsNeverGrantBothSidesAdvantage()
        {
            TacticalCardDefinition[] cards = _content.TacticalCards.Values.ToArray();
            foreach (TacticalCardDefinition a in cards)
            foreach (TacticalCardDefinition b in cards)
            {
                CounterModifiers ab = TacticalCounterResolver.Resolve(new[] { a }, new[] { b });
                CounterModifiers ba = TacticalCounterResolver.Resolve(new[] { b }, new[] { a });
                Assert.That(ab.outcome == "advantage" && ba.outcome == "advantage", Is.False,
                    $"{a.id} and {b.id} both received a strong advantage.");
            }
        }

        [Test]
        public void TacticalAi_OnlyChoosesAffordableCardsFromHand()
        {
            TeamRuntime team = MakeTeam("gambit_2021");
            CommandPointRules.Initialize(team);
            team.CommandPoints = 2;
            team.TacticalDeck.Hand.AddRange(new[]
            {
                "gambit_iron_wall", "gambit_team_sync", "baseline_control"
            });
            var state = new MatchState(team, MakeTeam("navi_2021"));
            IReadOnlyList<string> choices = new TacticalCardAi().Choose(
                team, state, _content.TacticalCards, new SeededRandom(3));
            Assert.That(choices, Does.Not.Contain("gambit_iron_wall"));
            Assert.That(choices.Sum(id => _content.TacticalCards[id].commandCost), Is.LessThanOrEqualTo(2));
            Assert.That(choices.All(team.TacticalDeck.Hand.Contains), Is.True);
        }

        [Test]
        public void TacticalSession_RemainsDeterministic()
        {
            MatchResult a = Simulate(221);
            MatchResult b = Simulate(221);
            Assert.That(a.winnerTeamId, Is.EqualTo(b.winnerTeamId));
            Assert.That(a.teamAScore, Is.EqualTo(b.teamAScore));
            Assert.That(a.teamBScore, Is.EqualTo(b.teamBScore));
            Assert.That(a.events.Select(x => x.message), Is.EqualTo(b.events.Select(x => x.message)));
        }

        private MatchResult Simulate(int seed) =>
            new QuickMatchSimulator(new RuleBasedTacticalPlanner(), new FastRoundSimulator())
                .Simulate(_content, "navi_2021", "gambit_2021", seed);

        private TeamRuntime MakeTeam(string id)
        {
            TeamDefinition definition = _content.Teams[id];
            return new TeamRuntime(
                definition,
                definition.playerIds.Select(playerId =>
                    new PlayerRuntime(_content.Players[playerId], 50f)));
        }
    }
}
