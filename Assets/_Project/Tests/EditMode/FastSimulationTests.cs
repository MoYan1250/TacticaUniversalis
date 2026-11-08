using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TacticaUniversalis.Application;
using TacticaUniversalis.Domain;
using TacticaUniversalis.Infrastructure;
using UnityEngine;
using SimulationEventType = TacticaUniversalis.Domain.EventType;

namespace TacticaUniversalis.Tests
{
    public sealed class FastSimulationTests
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
        public void CareerMatchTeams_ResolvesRealOpponentWithoutGambitFallback()
        {
            Assert.That(
                CareerMatchTeams.TryResolveSimTeams(
                    _content, "navi_2021", "vitality_2021", out _, out string opponent),
                Is.True);
            Assert.That(opponent, Is.EqualTo("vitality_2021"));
            Assert.That(
                CareerMatchTeams.TryResolveSimTeams(
                    _content, "navi_2021", "not_a_real_club", out _, out _),
                Is.False);
        }

        [Test]
        public void CanonicalContent_IsCompleteAndValid()
        {
            Assert.That(ContentValidator.Validate(_asset), Is.Empty);
            Assert.That(_content.Players.Count, Is.EqualTo(10));
            Assert.That(_content.Teams["navi_2021"].playerIds, Has.Length.EqualTo(5));
            Assert.That(_content.Teams["gambit_2021"].playerIds, Has.Length.EqualTo(5));
            Assert.That(_content.Players["s1mple"].traitIds, Has.Length.EqualTo(5));
            Assert.That(_content.Players["electronic"].displayName, Is.EqualTo("electroNic"));
            Assert.That(_content.Players["perfecto"].displayName, Is.EqualTo("Perfecto"));
            Assert.That(_content.Players["hobbit"].displayName, Is.EqualTo("HObbit"));
            Assert.That(_content.Traits["defense_theory"].enabled, Is.False);
        }

        [Test]
        public void SameSeed_ProducesIdenticalMatch()
        {
            MatchResult a = Simulate(2021);
            MatchResult b = Simulate(2021);
            Assert.That(a.winnerTeamId, Is.EqualTo(b.winnerTeamId));
            Assert.That(a.teamAScore, Is.EqualTo(b.teamAScore));
            Assert.That(a.teamBScore, Is.EqualTo(b.teamBScore));
            Assert.That(a.events.Select(x => x.message), Is.EqualTo(b.events.Select(x => x.message)));
        }

        [Test]
        public void Match_CompletesUnderSafetyLimitAndUsesMr15()
        {
            MatchResult result = Simulate(17);
            int completedRounds = result.teamAScore + result.teamBScore;
            Assert.That(completedRounds, Is.LessThanOrEqualTo(MatchRules.MaximumRounds));
            Assert.That(Math.Max(result.teamAScore, result.teamBScore), Is.GreaterThanOrEqualTo(16));
            Assert.That(Math.Abs(result.teamAScore - result.teamBScore), Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void Overtime_RequiresAtLeastFourRoundsAndTwoPointLead()
        {
            MatchState state = StateAt(15, 15, 33);
            Assert.That(MatchRules.HasWinner(state), Is.False);
            state = StateAt(18, 16, 34);
            Assert.That(MatchRules.HasWinner(state), Is.True);
            state = StateAt(18, 18, 36);
            Assert.That(MatchRules.HasWinner(state), Is.False);
        }

        [Test]
        public void Economy_UsesTeamPoolCardCosts()
        {
            TeamRuntime team = MakeTeam("navi_2021", 1);
            Assert.That(team.Money, Is.EqualTo(800));
            team.Money = 16000;
            EconomyRules.Buy(team, EquipmentLevel.FullBuy);
            Assert.That(team.Money, Is.EqualTo(11300));
            Assert.That(team.Equipment, Is.EqualTo(EquipmentLevel.FullBuy));
        }

        [Test]
        public void Economy_KillRewardsAreDesignDividedByFive()
        {
            Assert.That(EconomyRules.KillReward("pistol"), Is.EqualTo(60));
            Assert.That(EconomyRules.KillReward("smg"), Is.EqualTo(120));
            Assert.That(EconomyRules.KillReward("rifle"), Is.EqualTo(60));
            Assert.That(EconomyRules.KillReward("awp"), Is.EqualTo(20));
            Assert.That(EconomyRules.KillReward("knife"), Is.EqualTo(300));

            TeamRuntime team = MakeTeam("navi_2021", 1);
            team.Money = 1000;
            int reward = EconomyRules.ApplyKillReward(team, "smg");
            Assert.That(reward, Is.EqualTo(120));
            Assert.That(team.Money, Is.EqualTo(1120));
        }

        [Test]
        public void Economy_SettleUsesTeamIncomeWithoutPlayerMultiplier()
        {
            TeamRuntime winner = MakeTeam("navi_2021", 1);
            TeamRuntime loser = MakeTeam("gambit_2021", 2);
            winner.Money = 1000;
            loser.Money = 1000;
            EconomyRules.Settle(winner, loser, "elimination");
            Assert.That(winner.Money, Is.EqualTo(4250));
            Assert.That(loser.Money, Is.EqualTo(2900));
            Assert.That(loser.LossStreak, Is.EqualTo(1));
        }

        [Test]
        public void HigherAim_IncreasesCombatScore()
        {
            PlayerDefinition low = ClonePlayer(_content.Players["b1t"], 60);
            PlayerDefinition high = ClonePlayer(_content.Players["b1t"], 95);
            TeamRuntime teamA = SingleTeam("low", low);
            TeamRuntime teamB = SingleTeam("high", high);
            var match = new MatchState(teamA, teamB);
            var plan = new TacticalPlan { aggression = 0.5f, teamworkBonus = 0f, openingBonus = 0f };
            var context = new EncounterContext
            {
                match = match,
                teamA = teamA,
                teamB = teamB,
                playerA = teamA.Players[0],
                playerB = teamB.Players[0],
                phase = CombatPhase.Opening,
                zone = _content.Dust2.zones.First(),
                planA = plan,
                planB = plan,
                playerAHasInitiative = false,
                roundNumber = 1
            };
            var resolver = new EncounterResolver();
            float lowScore = resolver.Score(context, true, new SeededRandom(1));
            float highScore = resolver.Score(context, false, new SeededRandom(1));
            Assert.That(highScore, Is.GreaterThan(lowScore));
        }

        [Test]
        public void EventLog_NeverLetsDeadPlayerKillAgainInSameRound()
        {
            MatchResult result = Simulate(44);
            int round = -1;
            var dead = new HashSet<string>();
            foreach (SimulationEvent evt in result.events)
            {
                if (evt.roundNumber != round)
                {
                    round = evt.roundNumber;
                    dead.Clear();
                }
                if (evt.type != SimulationEventType.Kill)
                    continue;
                Assert.That(dead.Contains(evt.actorId), Is.False, $"{evt.actorId} acted after death in round {round}");
                dead.Add(evt.targetId);
            }
        }

        [Test]
        public void EventTimes_AreOrderedWithinEachRound()
        {
            MatchResult result = Simulate(99);
            foreach (IGrouping<int, SimulationEvent> group in result.events.GroupBy(x => x.roundNumber))
            {
                float previous = -1f;
                foreach (SimulationEvent evt in group.OrderBy(x => x.sequence))
                {
                    Assert.That(evt.roundTime, Is.GreaterThanOrEqualTo(previous), $"Round {group.Key} event time regressed.");
                    previous = evt.roundTime;
                }
            }
        }

        [Test]
        public void BatchSmoke_OneHundredMatchesCompleteAndRemainPlausible()
        {
            int naviWins = 0;
            int totalRounds = 0;
            for (int seed = 1; seed <= 100; seed++)
            {
                MatchResult result = Simulate(seed);
                if (result.winnerTeamId == "navi_2021") naviWins++;
                totalRounds += result.teamAScore + result.teamBScore;
            }

            float naviWinRate = naviWins / 100f;
            float averageRounds = totalRounds / 100f;
            TestContext.WriteLine($"NAVI win rate: {naviWinRate:P0}; average rounds: {averageRounds:F1}");
            Assert.That(naviWinRate, Is.InRange(0.10f, 0.90f), "Simulation has an extreme team bias.");
            Assert.That(averageRounds, Is.InRange(20f, 50f));
        }

        [Test]
        public void MapZones_ResolvePlantSite_MapsApproachToBombSite()
        {
            Assert.That(MapZones.ResolvePlantSite("a_long"), Is.EqualTo("a_site"));
            Assert.That(MapZones.ResolvePlantSite("a_short"), Is.EqualTo("a_site"));
            Assert.That(MapZones.ResolvePlantSite("cat"), Is.EqualTo("a_site"));
            Assert.That(MapZones.ResolvePlantSite("mid"), Is.EqualTo("a_site"));
            Assert.That(MapZones.ResolvePlantSite("xbox"), Is.EqualTo("a_site"));
            Assert.That(MapZones.ResolvePlantSite("b_tunnels"), Is.EqualTo("b_site"));
            Assert.That(MapZones.ResolvePlantSite("b_site"), Is.EqualTo("b_site"));
            Assert.That(MapZones.ResolvePlantSite("outside_tunnels"), Is.EqualTo("b_site"));
            Assert.That(MapZones.ResolvePlantSite("lower_tunnels"), Is.EqualTo("b_site"));
            Assert.That(MapZones.ResolvePlantSite("b_doors"), Is.EqualTo("b_site"));
            Assert.That(MapZones.ResolvePlantSite("scaffolding"), Is.EqualTo("b_site"));
            Assert.That(MapZones.ResolvePlantSite("pit"), Is.EqualTo("a_site"));
            Assert.That(MapZones.ResolvePlantSite("a_ramp"), Is.EqualTo("a_site"));
            Assert.That(MapZones.ResolvePlantSite("long_doors"), Is.EqualTo("a_site"));
            Assert.That(MapZones.ResolvePlantSite("suicide"), Is.EqualTo("a_site"));
            Assert.That(MapZones.ResolvePlantSite("mid_doors"), Is.EqualTo("a_site"));
            Assert.That(MapZones.ResolvePlantSite("popdog"), Is.EqualTo("b_site"));
            Assert.That(MapZones.ResolvePlantSite("underpass"), Is.EqualTo("b_site"));
            Assert.That(MapZones.ResolvePlantSite("sidewalk"), Is.EqualTo("b_site"));
            Assert.That(MapZones.ResolvePlantSite("ivy"), Is.EqualTo("a_site"));
            Assert.That(MapZones.ResolvePlantSite("connector"), Is.EqualTo("a_site"));
        }

        [Test]
        public void Dust2_IncludesSubdividedZones()
        {
            Assert.That(_content.Dust2.zones.Any(z => z.id == "cat"), Is.True);
            Assert.That(_content.Dust2.zones.Any(z => z.id == "xbox"), Is.True);
            MapZoneDefinition cat = _content.Dust2.zones.First(z => z.id == "cat");
            MapZoneDefinition xbox = _content.Dust2.zones.First(z => z.id == "xbox");
            Assert.That(cat.displayName, Is.EqualTo("Catwalk"));
            Assert.That(xbox.displayName, Is.EqualTo("Xbox"));
            Assert.That(cat.attackerTeamworkBonus, Is.GreaterThan(0f));
            Assert.That(xbox.defenderReactionBonus, Is.GreaterThan(0f));
        }

        [Test]
        public void BombPlanted_UsesSiteNotApproachZone()
        {
            MatchResult result = Simulate(2021);
            foreach (SimulationEvent evt in result.events.Where(x =>
                         x.type == SimulationEventType.BombPlanted ||
                         x.type == SimulationEventType.BombExploded ||
                         x.type == SimulationEventType.BombDefused))
            {
                Assert.That(evt.zoneId, Is.EqualTo("a_site").Or.EqualTo("b_site"),
                    $"{evt.type} should land on a bomb site, got '{evt.zoneId}'");
            }
        }

        private MatchResult Simulate(int seed) =>
            new QuickMatchSimulator(new RuleBasedTacticalPlanner(), new FastRoundSimulator())
                .Simulate(_content, "navi_2021", "gambit_2021", seed);

        private TeamRuntime MakeTeam(string id, int seed)
        {
            TeamDefinition team = _content.Teams[id];
            var random = new SeededRandom(seed);
            return new TeamRuntime(team, team.playerIds.Select(x => new PlayerRuntime(_content.Players[x], 50f + random.Next01() * 20f)));
        }

        private MatchState StateAt(int scoreA, int scoreB, int completedRounds)
        {
            var state = new MatchState(MakeTeam("navi_2021", 1), MakeTeam("gambit_2021", 2))
            {
                TeamAScore = scoreA,
                TeamBScore = scoreB,
                CompletedRounds = completedRounds
            };
            return state;
        }

        private static PlayerDefinition ClonePlayer(PlayerDefinition source, int aim) =>
            new PlayerDefinition
            {
                id = source.id + "_" + aim,
                primaryRole = source.primaryRole,
                dust2Grade = source.dust2Grade,
                traitIds = Array.Empty<string>(),
                attributes = new CombatAttributes
                {
                    aim = aim,
                    reaction = source.attributes.reaction,
                    awareness = source.attributes.awareness,
                    utility = source.attributes.utility,
                    teamwork = source.attributes.teamwork,
                    communication = source.attributes.communication,
                    opening = source.attributes.opening,
                    clutch = source.attributes.clutch
                }
            };

        private static TeamRuntime SingleTeam(string id, PlayerDefinition player) =>
            new TeamRuntime(
                new TeamDefinition { id = id, displayName = id, playerIds = new[] { player.id } },
                new[] { new PlayerRuntime(player, 50f) }) { Money = 80000, Equipment = EquipmentLevel.ForceBuy };
    }
}
