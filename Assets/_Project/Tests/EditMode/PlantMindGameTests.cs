using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TacticaUniversalis.Domain;

namespace TacticaUniversalis.Tests.EditMode
{
    public sealed class PlantMindGameTests
    {
        [Test]
        public void PlantChannel_RequiresFullDurationBeforeBombPlanted()
        {
            BehaviorTreeRoundWorld world = BuildWorld("a_site");
            PlayerRuntime planter = world.Request.attackers.Players[0];
            planter.ZoneId = "a_site";

            Assert.That(PlantChannelRules.TryBegin(world, planter, "a_site", 18f), Is.True);
            Assert.That(world.BombPlanted, Is.False);
            Assert.That(PlantChannelRules.IsChanneling(world), Is.True);

            PlantChannelRules.Tick(world, 18f + PlantChannelRules.RequiredSeconds - 0.05f);
            Assert.That(world.BombPlanted, Is.False);

            PlantChannelRules.Tick(world, 18f + PlantChannelRules.RequiredSeconds);
            Assert.That(world.BombPlanted, Is.True);
            Assert.That(PlantChannelRules.IsChanneling(world), Is.False);
            Assert.That(world.Result.events.Any(e => e.type == EventType.BombPlanted), Is.True);
        }

        [Test]
        public void PlantChannel_CancelsWhenPlanterLeavesSite()
        {
            BehaviorTreeRoundWorld world = BuildWorld("a_site");
            PlayerRuntime planter = world.Request.attackers.Players[0];
            planter.ZoneId = "a_site";
            Assert.That(PlantChannelRules.TryBegin(world, planter, "a_site", 20f), Is.True);

            planter.ZoneId = "ct_spawn";
            PlantChannelRules.Tick(world, 20.5f);
            Assert.That(world.BombPlanted, Is.False);
            Assert.That(PlantChannelRules.IsChanneling(world), Is.False);
            Assert.That(world.Result.events.Any(e =>
                e.reasonTags != null &&
                Array.Exists(e.reasonTags, t => t == "plant_cancel")), Is.True);
        }

        [Test]
        public void AcousticIntel_TagsPlantingOnSuccessfulHear()
        {
            var graph = new MapZoneGraph(MinimalDust2LikeMap());
            var listener = new PlayerRuntime(MinimalPlayer("ct1"), 50f) { ZoneId = "a_site" };
            listener.Definition.attributes.awareness = 90;
            var board = new IntelBoard();
            var team = new IntelBoard();
            AcousticIntelRules.ScanAudible(
                graph, listener, Array.Empty<PlayerRuntime>(),
                board, team, 30f, new AlwaysHearRandom(),
                plantingZoneId: "a_site", planterId: "t1",
                defuserId: null, defuseZoneId: null);

            Assert.That(
                AcousticIntelRules.BoardHasFreshTag(
                    team, AcousticIntelRules.TagPlanting, 30f, AcousticIntelRules.AcousticTtlSeconds),
                Is.True);
            Assert.That(
                AcousticIntelRules.FreshTagZone(
                    team, AcousticIntelRules.TagPlanting, 30f, AcousticIntelRules.AcousticTtlSeconds),
                Is.EqualTo("a_site"));
        }

        [Test]
        public void AcousticIntel_HearsAdjacentFootsteps()
        {
            var graph = new MapZoneGraph(MinimalDust2LikeMap());
            var listener = new PlayerRuntime(MinimalPlayer("t1"), 50f) { ZoneId = "a_site" };
            var enemy = new PlayerRuntime(MinimalPlayer("ct1"), 50f) { ZoneId = "a_ramp" };
            var personal = new IntelBoard();

            Assert.That(
                AcousticIntelRules.HearsThreatNear(
                    graph, listener.ZoneId, new[] { enemy }, personal, 40f),
                Is.True);
        }

        [Test]
        public void ContestPlant_FreshPlantingIntelPointsAtSite()
        {
            var team = new IntelBoard();
            AcousticIntelRules.UpsertTagged(team, "a_site", AcousticIntelRules.TagPlanting, 25f, "ct1");
            Assert.That(
                AcousticIntelRules.BoardHasFreshTag(
                    team, AcousticIntelRules.TagPlanting, 25f, AcousticIntelRules.AcousticTtlSeconds),
                Is.True);
            Assert.That(
                AcousticIntelRules.FreshTagZone(
                    team, AcousticIntelRules.TagPlanting, 25f, AcousticIntelRules.AcousticTtlSeconds),
                Is.EqualTo("a_site"));
        }

        [Test]
        public void ActExecutePlan_CancelsPlantWhenThreatNear()
        {
            BehaviorTreeRoundWorld world = BuildWorld("a_site");
            PlayerRuntime planter = world.Request.attackers.Players[0];
            PlayerRuntime ct = world.Request.defenders.Players[0];
            planter.ZoneId = "a_site";
            ct.ZoneId = "a_ramp";
            world.BombCarrierId = planter.Definition.id;
            Assert.That(PlantChannelRules.TryBegin(world, planter, "a_site", 18f), Is.True);

            var ctx = new BtContext
            {
                World = world,
                Self = planter,
                SelfTeam = world.Request.attackers,
                EnemyTeam = world.Request.defenders,
                SelfPlan = world.Request.attackerPlan,
                Time = 19f,
                Random = new NeverRollRandom()
            };

            InvokePrivateStatic("ActExecutePlan", ctx);

            Assert.That(world.BombPlanted, Is.False);
            Assert.That(PlantChannelRules.IsChanneling(world), Is.False);
            Assert.That(world.Result.events.Any(e =>
                e.reasonTags != null &&
                Array.Exists(e.reasonTags, t => t == "plant_cancel")), Is.True);
            Assert.That(world.MindGameUntilTime.ContainsKey(planter.Definition.id), Is.True);
        }

        [Test]
        public void ActRetake_FakeDefuseClearsWhenEnemyAdjacent()
        {
            BehaviorTreeRoundWorld world = BuildWorld("a_site");
            PlayerRuntime ct = world.Request.defenders.Players[0];
            PlayerRuntime t = world.Request.attackers.Players[0];
            ct.ZoneId = "a_site";
            t.ZoneId = "a_ramp";
            world.BombPlanted = true;
            world.PlantSiteId = "a_site";
            world.PlantTime = 20f;
            world.DefuserId = ct.Definition.id;
            world.DefuseStartTime = 30f;
            world.DefuseRequiredSeconds = 10f;

            var ctx = new BtContext
            {
                World = world,
                Self = ct,
                SelfTeam = world.Request.defenders,
                EnemyTeam = world.Request.attackers,
                SelfPlan = world.Request.defenderPlan,
                Time = 30.6f,
                Random = new NeverRollRandom()
            };

            InvokePrivateStatic("ActRetake", ctx);

            Assert.That(world.DefuserId, Is.Null.Or.Empty);
            Assert.That(world.BombDefused, Is.False);
            Assert.That(world.Result.events.Any(e =>
                e.reasonTags != null &&
                Array.Exists(e.reasonTags, t => t == "fake_defuse")), Is.True);
            Assert.That(world.MindGameUntilTime.ContainsKey(ct.Definition.id), Is.True);
        }

        private static void InvokePrivateStatic(string methodName, BtContext ctx)
        {
            MethodInfo method = typeof(BehaviorTreeRoundSimulator).GetMethod(
                methodName, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(null, new object[] { ctx });
        }

        private static BehaviorTreeRoundWorld BuildWorld(string plantSite)
        {
            MapDefinition map = MinimalDust2LikeMap();
            var attackers = new TeamRuntime(
                new TeamDefinition { id = "t", displayName = "T", playerIds = new[] { "t1" } },
                new[] { new PlayerRuntime(MinimalPlayer("t1"), 50f) { ZoneId = plantSite } })
            {
                Side = Side.Terrorist
            };
            var defenders = new TeamRuntime(
                new TeamDefinition { id = "ct", displayName = "CT", playerIds = new[] { "ct1" } },
                new[] { new PlayerRuntime(MinimalPlayer("ct1"), 50f) { ZoneId = "ct_spawn" } })
            {
                Side = Side.CounterTerrorist
            };

            return new BehaviorTreeRoundWorld
            {
                Request = new RoundSimulationRequest
                {
                    attackers = attackers,
                    defenders = defenders,
                    map = map,
                    roundNumber = 1,
                    attackerPlan = new TacticalPlan { primaryZoneId = plantSite },
                    defenderPlan = new TacticalPlan { primaryZoneId = "ct_spawn" }
                },
                Result = new RoundResult(),
                Graph = new MapZoneGraph(map),
                PlantSiteId = plantSite
            };
        }

        private static PlayerDefinition MinimalPlayer(string id) =>
            new PlayerDefinition
            {
                id = id,
                displayName = id,
                primaryRole = "Rifler",
                attributes = new CombatAttributes
                {
                    aim = 70,
                    reaction = 70,
                    utility = 70,
                    teamwork = 70,
                    awareness = 70,
                    opening = 70,
                    clutch = 70,
                    communication = 70
                }
            };

        private static MapDefinition MinimalDust2LikeMap() =>
            new MapDefinition
            {
                id = "dust2",
                displayName = "Dust II",
                zones = new[]
                {
                    new MapZoneDefinition { id = "a_site" },
                    new MapZoneDefinition { id = "a_ramp" },
                    new MapZoneDefinition { id = "ct_spawn" },
                    new MapZoneDefinition { id = "t_spawn" }
                },
                routes = new[]
                {
                    new MapRouteDefinition { from = "a_site", to = "a_ramp" },
                    new MapRouteDefinition { from = "a_ramp", to = "ct_spawn" },
                    new MapRouteDefinition { from = "t_spawn", to = "a_site" }
                }
            };

        private sealed class AlwaysHearRandom : IRandomSource
        {
            public float Next01() => 0f;
            public int Range(int inclusiveMin, int exclusiveMax) => inclusiveMin;
        }

        private sealed class NeverRollRandom : IRandomSource
        {
            public float Next01() => 1f;
            public int Range(int inclusiveMin, int exclusiveMax) => inclusiveMin;
        }
    }
}
