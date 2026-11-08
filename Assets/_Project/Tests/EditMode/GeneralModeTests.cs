using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TacticaUniversalis.Application;
using TacticaUniversalis.Domain;

namespace TacticaUniversalis.Tests
{
    public sealed class GeneralModeTests
    {
        private WeaponCatalog _catalog;

        [SetUp]
        public void SetUp() => _catalog = WeaponCatalog.CreateDefault();

        [Test]
        public void MidDoor_FitsOneBodyWithALittleRoom()
        {
            float spare = MapBodyClearance.MidDoorHammer - MapBodyClearance.BodyDiameterHammer;
            Assert.That(spare, Is.GreaterThan(4f));
            Assert.That(spare, Is.LessThan(10f));
            Assert.That(MapBodyClearance.BodyDiameterHammer, Is.LessThan(MapBodyClearance.MidDoorHammer));
            Assert.That(MapBodyClearance.BodyDiameterHammer * 2f, Is.GreaterThan(MapBodyClearance.MidDoorHammer));

            const float mapWidth = 778f;
            const float overviewWidth = 958f;
            const float hammerPerPixel = 4.314f;
            float radius = MapBodyClearance.LocalRadius(mapWidth, overviewWidth, hammerPerPixel);
            float body = radius * 2f;
            float door = MapBodyClearance.MidDoorHammer / hammerPerPixel / overviewWidth * mapWidth;
            Assert.That(body, Is.LessThan(door));
            Assert.That(door - body, Is.LessThan(body * 0.25f));
        }

        [Test]
        public void HitResolver_HeadshotReducesHealthAndCanKill()
        {
            var attacker = new PlayerRuntime(MinimalPlayer("a", "Rifler", 90), 50f);
            var victim = new PlayerRuntime(MinimalPlayer("b", "Rifler", 50), 50f);
            attacker.Loadout = new PlayerLoadout { PrimaryWeaponId = "ak47", PistolWeaponId = "glock" };
            victim.Loadout = new PlayerLoadout { PistolWeaponId = "usp_s", HasArmor = true, HasHelmet = true };
            victim.Health = 100;
            victim.ArmorHp = 100;
            victim.Alive = true;

            var forcedHead = new FixedRandom(0f); // always headshot (chance check uses Next01 < chance)
            HitResult hit = HitResolver.Apply(attacker, victim, _catalog, forcedHead, "ak47");
            Assert.That(hit.Headshot, Is.True);
            Assert.That(victim.Health, Is.LessThan(100));
            Assert.That(hit.DamageToHealth, Is.GreaterThan(0));
        }

        [Test]
        public void CombatSight_InViewCone_OnlyWhenTargetIsInFront()
        {
            Assert.That(CombatSightRules.InViewCone(90f, 0f, 1f), Is.True);
            Assert.That(CombatSightRules.InViewCone(90f, 0f, -1f), Is.False);
            Assert.That(CombatSightRules.InViewCone(0f, 1f, 0f), Is.True);
            Assert.That(CombatSightRules.InViewCone(0f, -1f, 0f), Is.False);
        }

        [Test]
        public void MapLosGrid_HasClearLineUv_BlockedBySolidWall()
        {
            const int w = 16;
            const int h = 16;
            var solid = new bool[w * h];
            for (int y = 0; y < h; y++)
                solid[y * w + 8] = true; // vertical wall at u≈0.5

            var grid = new MapLosGrid();
            grid.SetCells(solid, w, h);
            Assert.That(grid.IsReady, Is.True);
            Assert.That(grid.HasClearLineUv(0.2f, 0.5f, 0.8f, 0.5f), Is.False);
            Assert.That(grid.HasClearLineUv(0.1f, 0.2f, 0.3f, 0.2f), Is.True);
        }

        [Test]
        public void CombatSight_HasLineOfSight_FailOpenWithoutLosAndBlocksWithWall()
        {
            var map = new MapDefinition
            {
                id = "test",
                zones = new[]
                {
                    new MapZoneDefinition { id = "a", x = 0.2f, y = 0.5f },
                    new MapZoneDefinition { id = "b", x = 0.8f, y = 0.5f }
                }
            };
            var shooter = new PlayerRuntime(MinimalPlayer("s", "Rifler", 80), 50f) { ZoneId = "a" };
            var target = new PlayerRuntime(MinimalPlayer("t", "Rifler", 80), 50f) { ZoneId = "b" };
            CombatSightRules.FaceToward(shooter, target, map);

            Assert.That(CombatSightRules.HasLineOfSight(map, shooter, target, null), Is.True);

            const int w = 16;
            const int h = 16;
            var solid = new bool[w * h];
            for (int y = 0; y < h; y++)
                solid[y * w + 8] = true;
            var grid = new MapLosGrid();
            grid.SetCells(solid, w, h);

            Assert.That(CombatSightRules.HasLineOfSight(map, shooter, target, grid), Is.False);
            Assert.That(
                IntelRules.CanSee(map, shooter, target, 90f, grid),
                Is.False);
        }

        [Test]
        public void CombatSight_HitChance_FallsWithDistanceAndAim()
        {
            WeaponDefinition ak = _catalog.GetOrKnife("ak47");
            float close = CombatSightRules.HitChance(ak, 80f, 1f, 0.05f);
            float mid = CombatSightRules.HitChance(ak, 80f, 1f, 0.12f);
            float far = CombatSightRules.HitChance(ak, 80f, 1f, 0.20f);
            Assert.That(close, Is.GreaterThan(mid));
            Assert.That(mid, Is.GreaterThan(far));

            float lowAim = CombatSightRules.HitChance(ak, 40f, 1f, 0.08f);
            float highAim = CombatSightRules.HitChance(ak, 90f, 1f, 0.08f);
            Assert.That(highAim, Is.GreaterThan(lowAim));
        }

        [Test]
        public void CombatSight_HitChance_IsAimTimesRangeTimesStanceTimesFocus()
        {
            WeaponDefinition ak = _catalog.GetOrKnife("ak47");
            WeaponDefinition awp = _catalog.GetOrKnife("awp");
            AssertHitProduct(ak, 80f, 1f, 0.05f);
            AssertHitProduct(ak, 70f, 1.1f, 0.16f);
            AssertHitProduct(awp, 80f, 1f, 0.03f);
            AssertHitProduct(awp, 90f, 1f, 0.22f);
        }

        [Test]
        public void CombatSight_Rifle_StanceIsOne()
        {
            WeaponDefinition ak = _catalog.GetOrKnife("ak47");
            ShotAccuracy close = CombatSightRules.Evaluate(ak, 80f, 1f, 0.05f);
            ShotAccuracy far = CombatSightRules.Evaluate(ak, 80f, 1f, 0.16f);
            Assert.That(close.StanceFactor, Is.EqualTo(1f));
            Assert.That(far.StanceFactor, Is.EqualTo(1f));
            Assert.That(close.HipFire, Is.False);
            Assert.That(far.RangeFactor, Is.LessThan(close.RangeFactor));
        }

        [Test]
        public void CombatSight_Sniper_HipFireStanceKeepsRangeFactor()
        {
            WeaponDefinition awp = _catalog.GetOrKnife("awp");
            ShotAccuracy hip = CombatSightRules.Evaluate(awp, 80f, 1f, 0.03f);
            Assert.That(hip.HipFire, Is.True);
            Assert.That(hip.StanceFactor, Is.LessThan(1f));
            Assert.That(hip.RangeFactor, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void CombatSight_Sniper_FarUsesScopeAndRangeFalloff()
        {
            WeaponDefinition awp = _catalog.GetOrKnife("awp");
            ShotAccuracy mid = CombatSightRules.Evaluate(awp, 80f, 1f, awp.optimalRange);
            ShotAccuracy far = CombatSightRules.Evaluate(awp, 80f, 1f, 0.22f);
            Assert.That(far.HipFire, Is.False);
            Assert.That(far.StanceFactor, Is.EqualTo(1f));
            Assert.That(mid.StanceFactor, Is.EqualTo(1f));
            Assert.That(far.RangeFactor, Is.LessThan(mid.RangeFactor));
        }

        [Test]
        public void CombatSight_Sniper_WorseWhenTooClose()
        {
            WeaponDefinition awp = _catalog.GetOrKnife("awp");
            Assert.That(awp.closeFalloff, Is.GreaterThan(0f));
            float pointBlank = CombatSightRules.HitChance(awp, 80f, 1f, 0.02f);
            float optimal = CombatSightRules.HitChance(awp, 80f, 1f, awp.optimalRange);
            float far = CombatSightRules.HitChance(awp, 80f, 1f, 0.22f);
            Assert.That(optimal, Is.GreaterThan(pointBlank));
            Assert.That(optimal, Is.GreaterThan(far));
        }

        [Test]
        public void CombatSight_Sniper_HipFiresWhenTooClose()
        {
            WeaponDefinition awp = _catalog.GetOrKnife("awp");
            WeaponDefinition ssg = _catalog.GetOrKnife("ssg08");
            WeaponDefinition ak = _catalog.GetOrKnife("ak47");
            Assert.That(CombatSightRules.RequiresScope(awp), Is.True);
            Assert.That(CombatSightRules.ShouldHipFire(awp, 0.02f), Is.True);
            Assert.That(CombatSightRules.ShouldHipFire(awp, awp.optimalRange), Is.False);
            Assert.That(CombatSightRules.ShouldHipFire(ak, 0.02f), Is.False);

            float awpHip = CombatSightRules.HitChance(awp, 80f, 1f, 0.03f);
            float awpScoped = CombatSightRules.HitChance(awp, 80f, 1f, awp.optimalRange);
            float akClose = CombatSightRules.HitChance(ak, 80f, 1f, 0.03f);
            float ssgHip = CombatSightRules.HitChance(ssg, 80f, 1f, 0.03f);
            Assert.That(awpHip, Is.GreaterThan(awpScoped * 0.78f));
            Assert.That(awpHip, Is.LessThan(awpScoped));
            Assert.That(awpHip, Is.LessThan(akClose));
            Assert.That(ssgHip, Is.GreaterThan(awpHip));
        }

        [Test]
        public void CombatSight_SmgFallsFasterThanRifle()
        {
            WeaponDefinition mp9 = _catalog.GetOrKnife("mp9");
            WeaponDefinition ak = _catalog.GetOrKnife("ak47");
            float mp9Far = CombatSightRules.RangeFactor(mp9, 0.14f);
            float akFar = CombatSightRules.RangeFactor(ak, 0.14f);
            Assert.That(akFar, Is.GreaterThan(mp9Far));
        }

        [Test]
        public void CombatSight_RangeFalloff_OrderAtLongRange()
        {
            const float d = 0.18f;
            float dual = CombatSightRules.RangeFactor(_catalog.GetOrKnife("Dual_Berettas"), d);
            float mac10 = CombatSightRules.RangeFactor(_catalog.GetOrKnife("mac10"), d);
            float mp9 = CombatSightRules.RangeFactor(_catalog.GetOrKnife("mp9"), d);
            float mp7 = CombatSightRules.RangeFactor(_catalog.GetOrKnife("mp7"), d);
            float usp = CombatSightRules.RangeFactor(_catalog.GetOrKnife("usp_s"), d);
            float ak = CombatSightRules.RangeFactor(_catalog.GetOrKnife("ak47"), d);
            float awp = CombatSightRules.RangeFactor(_catalog.GetOrKnife("awp"), d);
            Assert.That(dual, Is.LessThan(mac10));
            Assert.That(mac10, Is.LessThan(mp9));
            Assert.That(mp9, Is.LessThan(mp7));
            Assert.That(mp7, Is.LessThan(usp));
            Assert.That(usp, Is.LessThan(ak));
            Assert.That(ak, Is.LessThan(awp));
            Assert.That(awp, Is.GreaterThan(0.95f));
        }

        [Test]
        public void EconomyCard_FullBuyPurchasesRifleAndArmorSeparately()
        {
            var player = new PlayerRuntime(MinimalPlayer("p1", "Rifler", 70), 50f)
            {
                Money = 5000,
                Loadout = ItemizedEconomyRules.DefaultPistolLoadout(Side.Terrorist)
            };
            EconomyCardLoadoutRules.BuyForPlayer(player, Side.Terrorist, EquipmentLevel.FullBuy, _catalog);
            Assert.That(player.Loadout.PrimaryWeaponId, Is.EqualTo("ak47"));
            Assert.That(player.Loadout.HasArmor, Is.True);
            Assert.That(player.Loadout.HasHelmet, Is.True);
            Assert.That(player.Money, Is.LessThan(5000));
            Assert.That(player.ArmorHp, Is.EqualTo(100));
        }

        [Test]
        public void EconomyCard_DowngradesWhenBroke()
        {
            var player = new PlayerRuntime(MinimalPlayer("p2", "Rifler", 70), 50f)
            {
                Money = 1500,
                Loadout = ItemizedEconomyRules.DefaultPistolLoadout(Side.CounterTerrorist)
            };
            EconomyCardLoadoutRules.BuyForPlayer(player, Side.CounterTerrorist, EquipmentLevel.FullBuy, _catalog);
            Assert.That(player.Loadout.PrimaryWeaponId, Is.Not.Null.And.Not.Empty);
            Assert.That(player.Loadout.PrimaryWeaponId, Is.Not.EqualTo("m4a4"));
            Assert.That(_catalog.GetOrKnife(player.Loadout.PrimaryWeaponId).price, Is.LessThanOrEqualTo(1500));
        }

        [Test]
        public void ItemizedEconomy_TeamMoneyIsSumOfPlayers()
        {
            ContentDatabase content = BuildMinimalContent();
            var session = new InteractiveQuickMatchSession(
                content, 7,
                new RuleBasedTacticalPlanner("mid"),
                new BehaviorTreeRoundSimulator(_catalog),
                "navi_2021", "gambit_2021", "dust2",
                BattleSimMode.BehaviorTree, _catalog);
            Assert.That(session.State.TeamA.Players.Count, Is.EqualTo(5));
            Assert.That(session.State.TeamA.Money, Is.EqualTo(ItemizedEconomyRules.StartingPlayerMoney * 5));

            session.State.TeamA.Players[0].Money = 1000;
            session.State.TeamA.Players[1].Money = 2000;
            session.State.TeamA.Players[2].Money = 3000;
            session.State.TeamA.Players[3].Money = 4000;
            session.State.TeamA.Players[4].Money = 5000;
            ItemizedEconomyRules.SyncTeamMoney(session.State.TeamA);
            Assert.That(session.State.TeamA.Money, Is.EqualTo(15000));
        }

        [Test]
        public void EconomyCard_TeamCostIsSumAndAwperPicksAwp()
        {
            var players = new List<PlayerRuntime>();
            for (int i = 0; i < 4; i++)
            {
                players.Add(new PlayerRuntime(MinimalPlayer("r" + i, "Rifler", 70), 50f)
                {
                    Money = 6000,
                    Loadout = ItemizedEconomyRules.DefaultPistolLoadout(Side.Terrorist)
                });
            }
            players.Add(new PlayerRuntime(MinimalPlayer("awp1", "Awper", 80), 50f)
            {
                Money = 7000,
                Loadout = ItemizedEconomyRules.DefaultPistolLoadout(Side.Terrorist)
            });

            var team = new TeamRuntime(
                new TeamDefinition { id = "t", displayName = "T", playerIds = players.Select(p => p.Definition.id).ToArray() },
                players)
            {
                Side = Side.Terrorist,
                Money = 0
            };
            ItemizedEconomyRules.SyncTeamMoney(team);

            int teamCost = BehaviorTreeBuyRules.EstimateTeamCardCost(team, EquipmentLevel.Hero, _catalog);
            BehaviorTreeBuyRules.SetHeroTargets(team, new[] { "awp1" });
            teamCost = BehaviorTreeBuyRules.EstimateTeamCardCost(team, EquipmentLevel.Hero, _catalog);
            int heroCost = BehaviorTreeBuyRules.EstimatePlayerIdealCost(
                players[4], Side.Terrorist, EquipmentLevel.Hero, _catalog);
            Assert.That(teamCost, Is.EqualTo(heroCost));
            Assert.That(teamCost, Is.GreaterThan(0));
            Assert.That(BehaviorTreeBuyRules.CanAffordCard(team, EquipmentLevel.Hero, _catalog), Is.True);

            BehaviorTreeBuyRules.BuyForTeam(team, EquipmentLevel.Hero, _catalog);
            Assert.That(players[4].Loadout.PrimaryWeaponId, Is.EqualTo("awp"));
            Assert.That(players.Take(4).All(p => string.IsNullOrEmpty(p.Loadout.PrimaryWeaponId)), Is.True);
            Assert.That(team.Money, Is.EqualTo(players.Sum(p => p.Money)));
        }

        [Test]
        public void EconomyCard_HalfBuyGetsDeagleAndArmor()
        {
            var player = new PlayerRuntime(MinimalPlayer("h1", "Rifler", 70), 50f)
            {
                Money = 2500,
                Loadout = ItemizedEconomyRules.DefaultPistolLoadout(Side.Terrorist)
            };
            BehaviorTreeBuyRules.BuyForPlayer(player, Side.Terrorist, EquipmentLevel.HalfBuy, _catalog);
            Assert.That(player.Loadout.PrimaryWeaponId, Is.Null.Or.Empty);
            Assert.That(player.Loadout.PistolWeaponId, Is.EqualTo("deagle"));
            Assert.That(player.Loadout.HasArmor, Is.True);
            Assert.That(BehaviorTreeBuyRules.IsDualBerettas(player.Loadout.PistolWeaponId), Is.False);
        }

        [Test]
        public void PistolRound_DualBerettasAtMostOnePerTeam()
        {
            TeamRuntime always = PistolTeam("ta", Side.Terrorist);
            BehaviorTreeBuyRules.BuyPistolRound(always, _catalog, new FixedRandom(0f));
            Assert.That(BehaviorTreeBuyRules.TeamDualBerettasCount(always), Is.EqualTo(1));
            Assert.That(
                always.Players.Count(p =>
                    BehaviorTreeBuyRules.IsDualBerettas(p.Loadout?.PistolWeaponId)),
                Is.EqualTo(1));

            TeamRuntime never = PistolTeam("tn", Side.Terrorist);
            BehaviorTreeBuyRules.BuyPistolRound(never, _catalog, new FixedRandom(1f));
            Assert.That(BehaviorTreeBuyRules.TeamDualBerettasCount(never), Is.EqualTo(0));

            TeamRuntime noRng = PistolTeam("tx", Side.Terrorist);
            BehaviorTreeBuyRules.BuyPistolRound(noRng, _catalog);
            Assert.That(BehaviorTreeBuyRules.TeamDualBerettasCount(noRng), Is.EqualTo(0));

            TeamRuntime full = PistolTeam("tf", Side.Terrorist);
            foreach (PlayerRuntime p in full.Players)
                p.Money = 5000;
            ItemizedEconomyRules.SyncTeamMoney(full);
            BehaviorTreeBuyRules.BuyForTeam(full, EquipmentLevel.FullBuy, _catalog);
            Assert.That(BehaviorTreeBuyRules.TeamDualBerettasCount(full), Is.EqualTo(0));
        }

        [Test]
        public void PistolRound_TeamUsesAtLeastTwoBuyStyles()
        {
            for (int seed = 0; seed < 24; seed++)
            {
                TeamRuntime t = PistolTeam("pt" + seed, Side.Terrorist);
                BehaviorTreeBuyRules.BuyPistolRound(t, _catalog, new SeededRandom(seed));
                Assert.That(
                    BehaviorTreeBuyRules.CountDistinctPistolRoundBuyRoles(t),
                    Is.GreaterThanOrEqualTo(2),
                    $"seed {seed}");

                bool sawArmor = false, sawPistol = false, sawUtil = false;
                foreach (PlayerRuntime p in t.Players)
                {
                    Assert.That(
                        BehaviorTreeBuyRules.TryClassifyPistolRoundBuy(p, Side.Terrorist, out PistolRoundBuyRole role),
                        Is.True,
                        p.Definition.id);
                    switch (role)
                    {
                        case PistolRoundBuyRole.Armor:
                            sawArmor = true;
                            Assert.That(p.Loadout.HasArmor, Is.True);
                            Assert.That(p.Loadout.PistolWeaponId, Is.EqualTo("glock"));
                            Assert.That(p.Loadout.HasSmoke || p.Loadout.HasFlash || p.Loadout.HasHe, Is.False);
                            break;
                        case PistolRoundBuyRole.BetterPistol:
                            sawPistol = true;
                            Assert.That(p.Loadout.HasArmor, Is.False);
                            Assert.That(p.Loadout.PistolWeaponId, Is.Not.EqualTo("glock"));
                            break;
                        case PistolRoundBuyRole.Utility:
                            sawUtil = true;
                            Assert.That(p.Loadout.HasArmor, Is.False);
                            Assert.That(p.Loadout.PistolWeaponId, Is.EqualTo("glock"));
                            Assert.That(
                                p.Loadout.HasSmoke || p.Loadout.HasFlash || p.Loadout.HasHe || p.Loadout.HasFire,
                                Is.True);
                            break;
                    }
                }

                int styles = (sawArmor ? 1 : 0) + (sawPistol ? 1 : 0) + (sawUtil ? 1 : 0);
                Assert.That(styles, Is.GreaterThanOrEqualTo(2), $"seed {seed} style flags");
            }

            TeamRuntime ct = PistolTeam("pct", Side.CounterTerrorist);
            BehaviorTreeBuyRules.BuyPistolRound(ct, _catalog, new SeededRandom(7));
            Assert.That(BehaviorTreeBuyRules.CountDistinctPistolRoundBuyRoles(ct), Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void MatchRules_PistolRounds()
        {
            Assert.That(MatchRules.IsPistolRound(0), Is.True);
            Assert.That(MatchRules.IsPistolRound(MatchRules.RegulationHalfRounds), Is.True);
            Assert.That(MatchRules.IsPistolRound(1), Is.False);
        }

        [Test]
        public void BehaviorTreeRound_ProducesRoundEndAndMoves()
        {
            ContentDatabase content = BuildMinimalContent();
            var session = new InteractiveQuickMatchSession(
                content, 42,
                new RuleBasedTacticalPlanner("mid"),
                new BehaviorTreeRoundSimulator(_catalog),
                "navi_2021", "gambit_2021", "dust2",
                BattleSimMode.BehaviorTree, _catalog);

            Assert.That(session.UsesItemizedEconomy, Is.True);
            Assert.That(session.State.TeamA.Players[0].Money, Is.EqualTo(ItemizedEconomyRules.StartingPlayerMoney));

            session.PrepareRound();
            // Round 1 wallets are $800; pistol round auto-buys. Use Eco on later funded rounds.
            // pistol-round movement (save only triggers when holding a primary).
            RoundResult round = session.PlayRound("navi_2021", EquipmentLevel.Eco);
            Assert.That(round.winnerTeamId, Is.Not.Null.And.Not.Empty);
            Assert.That(round.events.Any(e => e.type == EventType.Move), Is.True);
            Assert.That(round.events.Any(e => e.type == EventType.RoundEnded), Is.True);
            Assert.That(round.events.Any(e =>
                e.type == EventType.StatusUpdated &&
                e.message != null &&
                e.message.StartsWith("hp:")), Is.True);

            // Fund a buy round and confirm itemized purchases + further movement.
            foreach (PlayerRuntime p in session.State.TeamA.Players.Concat(session.State.TeamB.Players))
                p.Money = 6000;
            ItemizedEconomyRules.SyncTeamMoney(session.State.TeamA);
            ItemizedEconomyRules.SyncTeamMoney(session.State.TeamB);
            session.PrepareRound();
            RoundResult buyRound = session.PlayRound("navi_2021", EquipmentLevel.FullBuy);
            Assert.That(buyRound.events.Any(e => e.type == EventType.Move), Is.True);
            Assert.That(session.State.TeamA.Players.Any(p =>
                !string.IsNullOrEmpty(p.Loadout?.PrimaryWeaponId) ||
                !string.IsNullOrEmpty(p.Loadout?.PistolWeaponId)), Is.True);

            // Eco after a buy: kept primaries must NOT force the whole T side to camp spawn.
            foreach (PlayerRuntime p in session.State.TeamA.Players.Concat(session.State.TeamB.Players))
                p.Money = 800;
            ItemizedEconomyRules.SyncTeamMoney(session.State.TeamA);
            ItemizedEconomyRules.SyncTeamMoney(session.State.TeamB);
            session.PrepareRound();
            RoundResult ecoSaveRound = session.PlayRound("navi_2021", EquipmentLevel.Eco);
            var attackerIds = new HashSet<string>(
                session.State.TeamA.Definition.playerIds, StringComparer.OrdinalIgnoreCase);
            bool attackersLeftSpawn = ecoSaveRound.events.Any(e =>
                e.type == EventType.Move &&
                attackerIds.Contains(e.actorId) &&
                !string.Equals(e.zoneId, "t_spawn", StringComparison.OrdinalIgnoreCase));
            Assert.That(attackersLeftSpawn, Is.True);
            int earlySaves = ecoSaveRound.events.Count(e =>
                e.type == EventType.Save && e.roundTime < 5f);
            Assert.That(earlySaves, Is.EqualTo(0));
        }

        [Test]
        public void BehaviorTreeRound_DefendersLeaveSpawnBeforePlant()
        {
            ContentDatabase content = BuildMinimalContent();
            var session = new InteractiveQuickMatchSession(
                content, 42,
                new RuleBasedTacticalPlanner("a_long"),
                new BehaviorTreeRoundSimulator(_catalog),
                "navi_2021", "gambit_2021", "dust2",
                BattleSimMode.BehaviorTree, _catalog);
            session.PrepareRound();
            RoundResult round = session.PlayRound("navi_2021", EquipmentLevel.FullBuy);
            string[] defenderIds = session.State.TeamA.Definition.id == "navi_2021"
                ? session.State.TeamB.Definition.playerIds
                : session.State.TeamA.Definition.playerIds;
            var defenders = new HashSet<string>(defenderIds, StringComparer.OrdinalIgnoreCase);
            float plantTime = round.events
                .Where(e => e.type == EventType.BombPlanted)
                .Select(e => e.roundTime)
                .DefaultIfEmpty(20f)
                .Min();
            int stillAtSpawn = defenders.Count(id =>
            {
                string zone = round.events
                    .Where(e => e.type == EventType.Move &&
                                e.roundTime <= plantTime + 0.001f &&
                                string.Equals(e.actorId, id, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(e => e.roundTime)
                    .ThenByDescending(e => e.sequence)
                    .Select(e => e.zoneId)
                    .FirstOrDefault();
                return string.IsNullOrEmpty(zone) ||
                       string.Equals(zone, "ct_spawn", StringComparison.OrdinalIgnoreCase);
            });
            Assert.That(stillAtSpawn, Is.LessThanOrEqualTo(1));
        }

        [Test]
        public void BehaviorTreeRound_SearchesAndPeeks()
        {
            ContentDatabase content = BuildMinimalContent();
            var session = new InteractiveQuickMatchSession(
                content, 7,
                new RuleBasedTacticalPlanner("mid"),
                new BehaviorTreeRoundSimulator(_catalog),
                "navi_2021", "gambit_2021", "dust2",
                BattleSimMode.BehaviorTree, _catalog);
            session.PrepareRound();
            RoundResult round = session.PlayRound("navi_2021", EquipmentLevel.Eco);
            Assert.That(round.events.Any(e =>
                e.type == EventType.StatusUpdated &&
                e.reasonTags != null &&
                Array.Exists(e.reasonTags, t =>
                    string.Equals(t, "search", StringComparison.OrdinalIgnoreCase))), Is.True);
            Assert.That(round.events.Any(e =>
                e.type == EventType.Move &&
                e.reasonTags != null &&
                Array.Exists(e.reasonTags, t =>
                    string.Equals(t, "peek", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(t, "wide_peek", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(t, "peek_return", StringComparison.OrdinalIgnoreCase))), Is.True);
        }

        [Test]
        public void SaveHuntRules_AbortSaveOnOneVersusTwoOrBetter()
        {
            Assert.That(SaveHuntRules.ShouldAbortSave(1, 1), Is.True);
            Assert.That(SaveHuntRules.ShouldAbortSave(1, 2), Is.True);
            Assert.That(SaveHuntRules.ShouldAbortSave(2, 2), Is.True);
            Assert.That(SaveHuntRules.ShouldAbortSave(1, 3), Is.False);
            Assert.That(SaveHuntRules.ShouldAbortSave(2, 4), Is.False);
        }

        [Test]
        public void SaveHuntRules_RefuseSaveOnPistolOrCheapGun()
        {
            Assert.That(SaveHuntRules.ShouldRefuseSave(true, 2700), Is.True);
            Assert.That(SaveHuntRules.ShouldRefuseSave(false, 1500), Is.True);
            Assert.That(SaveHuntRules.ShouldRefuseSave(false, SaveHuntRules.MaxCheapWeaponPrice), Is.True);
            Assert.That(SaveHuntRules.ShouldRefuseSave(false, 1800), Is.False);
        }

        [Test]
        public void SaveHuntRules_HuntWhenLeadingByTwoAgainstLowEnemyCount()
        {
            Assert.That(SaveHuntRules.HasHuntAdvantage(5, 2), Is.True);
            Assert.That(SaveHuntRules.HasHuntAdvantage(4, 1), Is.True);
            Assert.That(SaveHuntRules.HasHuntAdvantage(3, 2), Is.False);
            Assert.That(SaveHuntRules.HasHuntAdvantage(5, 3), Is.False);
            Assert.That(SaveHuntRules.HasHuntAdvantage(2, 1), Is.False);
            Assert.That(SaveHuntRules.HasHuntAdvantage(3, 2, isPistolRound: true), Is.True);
            Assert.That(SaveHuntRules.HasHuntAdvantage(3, 2, isPistolRound: false), Is.False);
        }

        [Test]
        public void SaveHuntRules_TerroristsDoNotHuntBeforePlant()
        {
            Assert.That(SaveHuntRules.ShouldAllowHunt(true, false, 5, 2), Is.False);
            Assert.That(SaveHuntRules.ShouldAllowHunt(true, true, 5, 2), Is.True);
            Assert.That(SaveHuntRules.ShouldAllowHunt(false, false, 5, 2), Is.True);
            Assert.That(SaveHuntRules.ShouldAllowHunt(true, false, 5, 2, isPistolRound: true), Is.True);
            Assert.That(SaveHuntRules.ShouldAllowHunt(true, false, 5, 2, isPistolRound: false), Is.False);
        }

        [Test]
        public void PostPlantHoldRules_EvacuateNearBoomWhenClear()
        {
            Assert.That(PostPlantHoldRules.BombRemaining(10f, 40f, 42f), Is.EqualTo(8f).Within(0.001f));
            Assert.That(PostPlantHoldRules.ShouldEvacuate(8f, false, false), Is.True);
            Assert.That(PostPlantHoldRules.ShouldEvacuate(9f, false, false), Is.False);
            Assert.That(PostPlantHoldRules.ShouldEvacuate(5f, true, false), Is.False);
            Assert.That(PostPlantHoldRules.ShouldEvacuate(5f, false, true), Is.False);
        }

        [Test]
        public void SpawnHoldRules_RemapAwayFromSpawnForBothSides()
        {
            Assert.That(SpawnHoldRules.IsSpawnZone("ct_spawn"), Is.True);
            Assert.That(SpawnHoldRules.IsSpawnZone("a_site"), Is.False);
            Assert.That(
                SpawnHoldRules.RemapAwayFromSpawn("ct_spawn", new[] { "ct_spawn", "a_site", "mid" }, "mid", "a_site"),
                Is.EqualTo("a_site"));
            Assert.That(
                SpawnHoldRules.RemapAwayFromSpawn("t_spawn", new[] { "t_spawn" }, "t_spawn", "a_site"),
                Is.EqualTo("a_site"));
            Assert.That(
                SpawnHoldRules.RemapAwayFromSpawn("cat", new[] { "ct_spawn", "cat" }, "mid", "a_site"),
                Is.EqualTo("cat"));
        }

        [Test]
        public void TacticalManeuverRules_SeekCoverWhenTaggedOrOpenFight()
        {
            Assert.That(TacticalManeuverRules.ShouldSeekCover(30f, 1f, 1, 0), Is.True);
            Assert.That(TacticalManeuverRules.ShouldSeekCover(60f, 1f, 0, 1), Is.True);
            Assert.That(TacticalManeuverRules.ShouldSeekCover(80f, 1f, 1, 0), Is.True);
            Assert.That(TacticalManeuverRules.ShouldSeekCover(100f, 10f, 0, 0), Is.False);
            Assert.That(TacticalManeuverRules.ShouldSeekCover(90f, 10f, 0, 1), Is.False);
        }

        [Test]
        public void TacticalManeuverRules_JigglePeekGates()
        {
            Assert.That(TacticalManeuverRules.ShouldJigglePeek(
                true, 80f, 0, 1, false, false, 5f, 0f, false), Is.True);
            Assert.That(TacticalManeuverRules.ShouldJigglePeek(
                true, 80f, 0, 0, false, false, 5f, 0f, true), Is.True);
            Assert.That(TacticalManeuverRules.ShouldJigglePeek(
                true, 80f, 1, 0, false, false, 5f, 0f, true), Is.False);
            Assert.That(TacticalManeuverRules.ShouldJigglePeek(
                true, 20f, 0, 1, false, false, 5f, 0f, true), Is.False);
            Assert.That(TacticalManeuverRules.ShouldJigglePeek(
                true, 80f, 0, 1, true, false, 5f, 0f, true), Is.False);
            Assert.That(TacticalManeuverRules.ShouldJigglePeek(
                true, 80f, 0, 1, false, true, 5f, 99f, false), Is.True);
            Assert.That(TacticalManeuverRules.ShouldStartPeek(
                true, 80f, 0, 1, false, false, 5f, 99f, false, true, false), Is.True);
            Assert.That(TacticalManeuverRules.ShouldStartPeek(
                true, 80f, 0, 1, false, false, 5f, 0f, true, false, true), Is.False);
            Assert.That(TacticalManeuverRules.PeekCooldown(70f, 0.5f, "Rifler", true),
                Is.LessThan(TacticalManeuverRules.PeekCooldown(70f, 0.5f, "Rifler", false)));
        }

        [Test]
        public void TacticalManeuverRules_SearchFasterWithAwareness()
        {
            Assert.That(TacticalManeuverRules.SearchInterval(90f),
                Is.LessThan(TacticalManeuverRules.SearchInterval(40f)));
            Assert.That(TacticalManeuverRules.PeekCooldown(90f, 0.5f, "Entry"),
                Is.LessThan(TacticalManeuverRules.PeekCooldown(90f, 0.5f, "Awper")));
        }

        [Test]
        public void IntelRules_ScanVisible_RecordsCountAndWeapon()
        {
            MapDefinition map = LoadMap("dust2");
            var observer = new PlayerRuntime(MinimalPlayer("o", "Rifler", 80), 50f)
            {
                ZoneId = "a_short",
                Alive = true
            };
            observer.Definition.attributes.awareness = 80;
            var awper = new PlayerRuntime(MinimalPlayer("e1", "Awper", 80), 50f)
            {
                ZoneId = "a_site",
                Alive = true,
                Loadout = new PlayerLoadout { PrimaryWeaponId = "awp", PistolWeaponId = "usp_s" }
            };
            var rifler = new PlayerRuntime(MinimalPlayer("e2", "Rifler", 70), 50f)
            {
                ZoneId = "a_site",
                Alive = true,
                Loadout = new PlayerLoadout { PrimaryWeaponId = "ak47", PistolWeaponId = "glock" }
            };
            CombatSightRules.FaceToward(observer, awper, map);
            List<IntelNote> notes = IntelRules.ScanVisible(
                map, observer, new[] { awper, rifler }, _catalog, 5f);
            IntelNote site = notes.FirstOrDefault(n =>
                string.Equals(n.ZoneId, "a_site", StringComparison.OrdinalIgnoreCase));
            Assert.That(site, Is.Not.Null);
            Assert.That(site.Count, Is.EqualTo(2));
            Assert.That(site.WeaponIds.Any(id =>
                string.Equals(id, "awp", StringComparison.OrdinalIgnoreCase)), Is.True);
        }

        [Test]
        public void Utility_SmokeBlocksCrossZoneSightButNotSameZone()
        {
            MapDefinition map = LoadMap("dust2");
            var graph = new MapZoneGraph(map);
            var a = new PlayerRuntime(MinimalPlayer("ua", "Rifler", 80), 50f)
            {
                ZoneId = "a_short",
                Alive = true,
                Health = 100
            };
            var b = new PlayerRuntime(MinimalPlayer("ub", "Rifler", 80), 50f)
            {
                ZoneId = "a_site",
                Alive = true,
                Health = 100
            };
            CombatSightRules.FaceToward(a, b, map);
            a.Definition.attributes.awareness = 90;
            Assert.That(IntelRules.CanSee(map, a, b, 90f, null, graph, null), Is.True);

            var smoke = new List<ActiveUtilityEffect>
            {
                UtilityCombatRules.CreateSmoke("a_short", "t", "ua", 0f)
            };
            // Path a_short -> a_site typically goes through smoked choke; segment may also clip.
            bool blocked = UtilityCombatRules.BlocksSight(map, graph, a, b, smoke);
            Assert.That(blocked || !IntelRules.CanSee(map, a, b, 90f, null, graph, smoke), Is.True);

            b.ZoneId = "a_short";
            Assert.That(UtilityCombatRules.BlocksSight(map, graph, a, b, smoke), Is.False);
            Assert.That(IntelRules.CanSee(map, a, b, 90f, null, graph, smoke), Is.True);
        }

        [Test]
        public void Utility_PathAvoidsSmokeUnlessPush()
        {
            MapDefinition map = LoadMap("dust2");
            var graph = new MapZoneGraph(map);
            var effects = new List<ActiveUtilityEffect>
            {
                UtilityCombatRules.CreateSmoke("a_short", "t", "p", 0f)
            };
            IReadOnlyList<string> plain = graph.FindPath("t_spawn", "a_site");
            IReadOnlyList<string> avoid = graph.FindPath(
                "t_spawn", "a_site",
                z => UtilityCombatRules.ZonePathCost(effects, z));
            Assert.That(plain, Is.Not.Null.And.Not.Empty);
            Assert.That(avoid, Is.Not.Null.And.Not.Empty);
            // Avoiding path should not prefer walking into smoked zone when an alternate exists.
            bool plainUsesSmoke = plain.Any(z =>
                string.Equals(z, "a_short", StringComparison.OrdinalIgnoreCase));
            bool avoidUsesSmoke = avoid.Any(z =>
                string.Equals(z, "a_short", StringComparison.OrdinalIgnoreCase));
            if (plainUsesSmoke)
                Assert.That(avoidUsesSmoke, Is.False);
            Assert.That(UtilityCombatRules.ShouldPushThrough(
                true, true, false, false, false, 5, 5), Is.True);
            Assert.That(UtilityCombatRules.ShouldPushThrough(
                false, false, false, false, false, 5, 5), Is.False);
        }

        [Test]
        public void Utility_HeDamagesZoneAndFireTicks()
        {
            MapDefinition map = LoadMap("dust2");
            var graph = new MapZoneGraph(map);
            var victim = new PlayerRuntime(MinimalPlayer("vh", "Rifler", 70), 50f)
            {
                ZoneId = "a_site",
                Alive = true,
                Health = 100,
                ArmorHp = 0,
                Loadout = ItemizedEconomyRules.DefaultPistolLoadout(Side.Terrorist)
            };
            var team = new TeamRuntime(
                new TeamDefinition { id = "vt", displayName = "vt", playerIds = new[] { "vh" } },
                new List<PlayerRuntime> { victim })
            { Side = Side.Terrorist };

            int hits = 0;
            UtilityCombatRules.ApplyHeDamage(map, graph, "a_site", team, 1f, (p, dmg) =>
            {
                hits++;
                UtilityCombatRules.ApplyFlatDamage(p, dmg);
            });
            Assert.That(hits, Is.EqualTo(1));
            Assert.That(victim.Health, Is.EqualTo(100 - UtilityCombatRules.HeDamageSameZone));

            victim.Health = 100;
            victim.Alive = true;
            var fires = new List<ActiveUtilityEffect>
            {
                UtilityCombatRules.CreateFire("a_site", "ct", "x", 0f)
            };
            fires[0].NextFireTickAt = 0f;
            int burns = 0;
            UtilityCombatRules.TickFire(fires, map, team, null, 0.1f, (p, dmg, z) => burns++);
            Assert.That(burns, Is.EqualTo(1));
            Assert.That(victim.Health, Is.LessThan(100));
            Assert.That(victim.Health, Is.GreaterThanOrEqualTo(100 - UtilityCombatRules.FireTickDamageMax));
        }

        [Test]
        public void Utility_FireWallSpansNeighborsAndRadiusFalloff()
        {
            MapDefinition map = LoadMap("dust2");
            var graph = new MapZoneGraph(map);
            List<ActiveUtilityEffect> wall = UtilityCombatRules.CreateFireWall(
                map, graph, "a_site", "t", "thrower", 0f);
            Assert.That(wall.Count, Is.GreaterThan(1));
            Assert.That(UtilityCombatRules.HasActiveFire(wall, "a_site"), Is.True);
            Assert.That(wall.Any(e => !string.Equals(e.ZoneId, "a_site", StringComparison.OrdinalIgnoreCase)),
                Is.True);

            Assert.That(UtilityCombatRules.FireTickDamageAtDistance(0f),
                Is.EqualTo(UtilityCombatRules.FireTickDamageMax));
            Assert.That(UtilityCombatRules.FireTickDamageAtDistance(UtilityCombatRules.FireRadiusUv),
                Is.EqualTo(UtilityCombatRules.FireTickDamageMin));
            Assert.That(
                UtilityCombatRules.FireTickDamageAtDistance(UtilityCombatRules.FireRadiusUv * 0.5f),
                Is.LessThan(UtilityCombatRules.FireTickDamageMax));
        }

        [Test]
        public void Utility_HeDamagesFriendlyAndDeathDropsLoot()
        {
            MapDefinition map = LoadMap("dust2");
            var graph = new MapZoneGraph(map);
            var ally = new PlayerRuntime(MinimalPlayer("ally", "Rifler", 70), 50f)
            {
                ZoneId = "a_site",
                Alive = true,
                Health = 50,
                ArmorHp = 0,
                Loadout = new PlayerLoadout
                {
                    PrimaryWeaponId = "ak47",
                    PistolWeaponId = "glock",
                    FlashCount = 1
                }
            };
            var team = new TeamRuntime(
                new TeamDefinition { id = "ff", displayName = "ff", playerIds = new[] { "ally" } },
                new List<PlayerRuntime> { ally })
            { Side = Side.Terrorist };

            int hits = 0;
            UtilityCombatRules.ApplyHeDamage(map, graph, "a_site", new[] { ally }, 1f, (p, dmg) =>
            {
                hits++;
                UtilityCombatRules.ApplyFlatDamage(p, dmg);
            });
            Assert.That(hits, Is.EqualTo(1));
            Assert.That(ally.Alive, Is.False);

            var world = new BehaviorTreeRoundWorld
            {
                Result = new RoundResult(),
                Request = new RoundSimulationRequest { attackers = team },
                Catalog = _catalog
            };
            GroundLootRules.DropFromVictim(world, ally, team, 1f);
            Assert.That(world.GroundLoot.Count, Is.GreaterThan(0));
            Assert.That(world.GroundLoot.Any(l =>
                l.Kind == GroundLootKind.Primary || l.Kind == GroundLootKind.Flash), Is.True);
        }

        [Test]
        public void Utility_ThrowConsumesInventoryAndEmitsEvent()
        {
            var player = new PlayerRuntime(MinimalPlayer("thrower", "Rifler", 75), 50f)
            {
                ZoneId = "a_short",
                Alive = true,
                Health = 100,
                Loadout = ItemizedEconomyRules.DefaultPistolLoadout(Side.Terrorist)
            };
            player.Loadout.HasSmoke = true;
            player.Loadout.HasHe = true;
            Assert.That(UtilityCombatRules.TryConsumeNade(player, UtilityKind.Smoke), Is.True);
            Assert.That(player.Loadout.HasSmoke, Is.False);
            Assert.That(UtilityCombatRules.TryConsumeNade(player, UtilityKind.Smoke), Is.False);

            var effects = new List<ActiveUtilityEffect>
            {
                UtilityCombatRules.CreateSmoke("mid", "t", player.Definition.id, 3f)
            };
            Assert.That(effects[0].ExpireAt, Is.EqualTo(3f + UtilityCombatRules.SmokeDurationSeconds));
            Assert.That(UtilityCombatRules.HasActiveSmoke(effects, "mid"), Is.True);
            UtilityCombatRules.Expire(effects, effects[0].ExpireAt + 0.01f);
            Assert.That(effects.Count, Is.EqualTo(0));

            Assert.That(UtilityCombatRules.TryConsumeNade(player, UtilityKind.He), Is.True);
            Assert.That(player.Loadout.HasHe, Is.False);
            Assert.That(UtilityCombatRules.WeaponIdFor(UtilityKind.Fire), Is.EqualTo("fire"));
            Assert.That(UtilityCombatRules.WeaponIdFor(UtilityKind.Flash), Is.EqualTo("flash"));
            player.Loadout.FlashCount = 2;
            Assert.That(UtilityCombatRules.TryConsumeNade(player, UtilityKind.Flash), Is.True);
            Assert.That(player.Loadout.FlashCount, Is.EqualTo(1));
            Assert.That(player.Loadout.HasFlash, Is.True);
            Assert.That(UtilityCombatRules.TryConsumeNade(player, UtilityKind.Flash), Is.True);
            Assert.That(player.Loadout.FlashCount, Is.EqualTo(0));
            Assert.That(player.Loadout.HasFlash, Is.False);
            Assert.That(UtilityCombatRules.TryConsumeNade(player, UtilityKind.Flash), Is.False);
        }

        [Test]
        public void GroundLoot_FlashCapTwoAndPickupPrioritySmokeBeforeFire()
        {
            var victim = new PlayerRuntime(MinimalPlayer("dv", "Support", 70), 50f)
            {
                ZoneId = "mid",
                Alive = false,
                Loadout = new PlayerLoadout
                {
                    FlashCount = 2,
                    HasSmoke = true,
                    HasFire = true,
                    FireKind = FireNadeKind.Molotov,
                    PistolWeaponId = "glock"
                }
            };
            var team = new TeamRuntime(
                new TeamDefinition { id = "t", displayName = "t", playerIds = new[] { "dv" } },
                new List<PlayerRuntime> { victim })
            {
                Side = Side.Terrorist
            };
            var world = new BehaviorTreeRoundWorld
            {
                Result = new RoundResult { events = new List<SimulationEvent>() },
                Request = new RoundSimulationRequest { attackers = team }
            };

            GroundLootRules.DropFromVictim(world, victim, team, 1f);
            Assert.That(victim.Loadout.FlashCount, Is.EqualTo(0));
            Assert.That(world.GroundLoot.Count(l => l.Kind == GroundLootKind.Flash), Is.EqualTo(2));
            Assert.That(world.GroundLoot.Any(l => l.Kind == GroundLootKind.Smoke), Is.True);
            Assert.That(world.GroundLoot.Any(l => l.Kind == GroundLootKind.Fire), Is.True);

            Assert.That(GroundLootRules.UtilityPickupPriority(GroundLootKind.Smoke),
                Is.LessThan(GroundLootRules.UtilityPickupPriority(GroundLootKind.Fire)));
            Assert.That(GroundLootRules.UtilityPickupPriority(GroundLootKind.Flash),
                Is.LessThan(GroundLootRules.UtilityPickupPriority(GroundLootKind.He)));

            var picker = new PlayerRuntime(MinimalPlayer("dp", "Rifler", 70), 50f)
            {
                ZoneId = "mid",
                Alive = true,
                Loadout = ItemizedEconomyRules.DefaultPistolLoadout(Side.Terrorist)
            };
            GroundLootRules.TryPickupInZone(world, picker, team, _catalog, 2f);
            Assert.That(picker.Loadout.HasSmoke, Is.True);
            Assert.That(picker.Loadout.FlashCount, Is.EqualTo(2));
            Assert.That(picker.Loadout.HasFire, Is.True);
            Assert.That(world.GroundLoot.Count(l =>
                l.Kind == GroundLootKind.Flash ||
                l.Kind == GroundLootKind.Smoke ||
                l.Kind == GroundLootKind.Fire), Is.EqualTo(0));
        }

        [Test]
        public void FullBuy_ThrowerStacksAndSpawnDropsSpareUtil()
        {
            var support = new PlayerRuntime(MinimalPlayer("sup", "Support", 70), 50f)
            {
                Money = 6000,
                Loadout = ItemizedEconomyRules.DefaultPistolLoadout(Side.Terrorist)
            };
            support.Definition.attributes.utility = 95;
            var entry = new PlayerRuntime(MinimalPlayer("ent", "Entry", 70), 50f)
            {
                Money = 6000,
                Loadout = ItemizedEconomyRules.DefaultPistolLoadout(Side.Terrorist)
            };
            entry.Definition.attributes.opening = 95;
            var rifler = new PlayerRuntime(MinimalPlayer("rif", "Rifler", 70), 50f)
            {
                Money = 6000,
                Loadout = ItemizedEconomyRules.DefaultPistolLoadout(Side.Terrorist)
            };

            var team = new TeamRuntime(
                new TeamDefinition
                {
                    id = "buy",
                    displayName = "buy",
                    playerIds = new[] { "sup", "ent", "rif" }
                },
                new List<PlayerRuntime> { support, entry, rifler })
            {
                Side = Side.Terrorist
            };
            ItemizedEconomyRules.SyncTeamMoney(team);

            BehaviorTreeBuyRules.ResolveUtilityAnchors(team, out string utilId, out string entryId);
            Assert.That(utilId, Is.EqualTo("sup"));
            Assert.That(entryId, Is.EqualTo("ent"));

            var drops = new List<GroundLootItem>();
            BehaviorTreeBuyRules.BuyForTeam(
                team, EquipmentLevel.FullBuy, _catalog, drops, "t_spawn", new FixedRandom(1f));

            Assert.That(support.Loadout.PrimaryWeaponId, Is.Not.Null.And.Not.Empty);
            Assert.That(entry.Loadout.PrimaryWeaponId, Is.Not.Null.And.Not.Empty);
            Assert.That(support.Loadout.HasSmoke, Is.True);
            Assert.That(support.Loadout.HasHe, Is.True);
            Assert.That(support.Loadout.FlashCount, Is.EqualTo(2));
            Assert.That(entry.Loadout.FlashCount, Is.LessThanOrEqualTo(1));
            Assert.That(support.Loadout.FlashCount, Is.GreaterThan(entry.Loadout.FlashCount));
            Assert.That(drops.Count(d => d.Kind == GroundLootKind.Flash), Is.GreaterThan(0));
            // Dropped flashes never entered buyers as extra beyond their profile.
            Assert.That(entry.Loadout.FlashCount + support.Loadout.FlashCount, Is.LessThanOrEqualTo(3));
            Assert.That(rifler.Loadout.FlashCount, Is.EqualTo(0));
        }

        [Test]
        public void FullBuy_HelpGunBuysRifleOntoTeammateWithoutPrimary()
        {
            var donor = new PlayerRuntime(MinimalPlayer("don", "Rifler", 70), 50f)
            {
                Money = 8000,
                Loadout = ItemizedEconomyRules.DefaultPistolLoadout(Side.Terrorist)
            };
            donor.Loadout.PrimaryWeaponId = "ak47";
            var broke = new PlayerRuntime(MinimalPlayer("brk", "Entry", 70), 50f)
            {
                Money = 500,
                Loadout = ItemizedEconomyRules.DefaultPistolLoadout(Side.Terrorist)
            };
            // Simulate post-buy: broke still has no primary, donor has leftover.
            broke.Loadout.PrimaryWeaponId = null;
            donor.Money = 4000;

            var team = new TeamRuntime(
                new TeamDefinition
                {
                    id = "hg",
                    displayName = "hg",
                    playerIds = new[] { "don", "brk" }
                },
                new List<PlayerRuntime> { donor, broke })
            {
                Side = Side.Terrorist
            };

            int donorBefore = donor.Money;
            BehaviorTreeBuyRules.ApplyHelpGunBuys(team, Side.Terrorist, _catalog, new FixedRandom(0f));
            Assert.That(broke.Loadout.PrimaryWeaponId, Is.EqualTo("ak47"));
            Assert.That(donor.Money, Is.LessThan(donorBefore));

            broke.Loadout.PrimaryWeaponId = null;
            donor.Money = 4000;
            BehaviorTreeBuyRules.ApplyHelpGunBuys(team, Side.Terrorist, _catalog, new FixedRandom(1f));
            Assert.That(broke.Loadout.PrimaryWeaponId, Is.Null);
        }

        [Test]
        public void GroundLoot_EntryWithUtilSkipsUtilityPickup()
        {
            var entry = new PlayerRuntime(MinimalPlayer("en", "Entry", 70), 50f)
            {
                ZoneId = "mid",
                Alive = true,
                Loadout = ItemizedEconomyRules.DefaultPistolLoadout(Side.Terrorist)
            };
            entry.Loadout.FlashCount = 1;
            Assert.That(
                GroundLootRules.ShouldSkipUtilityPickup(entry, "Entry"), Is.True);
            Assert.That(
                GroundLootRules.ShouldSkipUtilityPickup(entry, "Support"), Is.False);
            entry.Loadout.FlashCount = 0;
            Assert.That(
                GroundLootRules.ShouldSkipUtilityPickup(entry, "Entry"), Is.False);
        }

        [Test]
        public void Utility_FlashBlastSameZoneAndAdjacentDurations()
        {
            MapDefinition map = LoadMap("dust2");
            var graph = new MapZoneGraph(map);
            const string blast = "a_site";
            const string adj = "a_short";

            var sameFace = new PlayerRuntime(MinimalPlayer("fs", "Rifler", 80), 50f)
            {
                ZoneId = blast,
                Alive = true,
                Health = 100
            };
            var sameAway = new PlayerRuntime(MinimalPlayer("fsa", "Rifler", 80), 50f)
            {
                ZoneId = blast,
                Alive = true,
                Health = 100
            };
            var face = new PlayerRuntime(MinimalPlayer("ff", "Rifler", 80), 50f)
            {
                ZoneId = adj,
                Alive = true,
                Health = 100
            };
            var mid = new PlayerRuntime(MinimalPlayer("fm", "Rifler", 80), 50f)
            {
                ZoneId = adj,
                Alive = true,
                Health = 100
            };
            var away = new PlayerRuntime(MinimalPlayer("fa", "Rifler", 80), 50f)
            {
                ZoneId = adj,
                Alive = true,
                Health = 100
            };
            var far = new PlayerRuntime(MinimalPlayer("fx", "Rifler", 80), 50f)
            {
                ZoneId = "t_spawn",
                Alive = true,
                Health = 100
            };

            CombatSightRules.FaceTowardZone(sameFace, blast, map);
            CombatSightRules.FaceTowardZone(sameAway, blast, map);
            sameAway.FacingDeg += 180f;
            CombatSightRules.FaceTowardZone(face, blast, map);
            CombatSightRules.FaceTowardZone(mid, blast, map);
            mid.FacingDeg += 90f;
            CombatSightRules.FaceTowardZone(away, blast, map);
            away.FacingDeg += 180f;

            float t0 = 10f;
            UtilityCombatRules.ApplyFlashBlast(
                map, graph, blast, t0,
                new[] { sameFace, sameAway, face, mid, away, far }, null);

            Assert.That(UtilityCombatRules.IsBlinded(sameFace, t0), Is.True);
            Assert.That(sameFace.BlindedUntil,
                Is.EqualTo(t0 + UtilityCombatRules.FlashSameZoneSeconds).Within(0.05f));
            Assert.That(sameAway.BlindedUntil,
                Is.EqualTo(t0 + UtilityCombatRules.FlashSameZoneMinSeconds).Within(0.05f));
            Assert.That(face.BlindedUntil,
                Is.EqualTo(t0 + UtilityCombatRules.FlashAdjacentFacingSeconds).Within(0.05f));
            Assert.That(away.BlindedUntil,
                Is.EqualTo(t0 + UtilityCombatRules.FlashAdjacentAwaySeconds).Within(0.05f));
            float midExpected = t0 + 0.5f * (
                UtilityCombatRules.FlashAdjacentAwaySeconds +
                UtilityCombatRules.FlashAdjacentFacingSeconds);
            Assert.That(mid.BlindedUntil, Is.EqualTo(midExpected).Within(0.12f));
            Assert.That(far.BlindedUntil, Is.EqualTo(-1f));
            Assert.That(UtilityCombatRules.IsBlinded(far, t0), Is.False);
            Assert.That(sameFace.BlindSeverity01, Is.GreaterThan(sameAway.BlindSeverity01));
            Assert.That(face.BlindSeverity01, Is.GreaterThan(away.BlindSeverity01));
        }

        [Test]
        public void Utility_FlashBlindBlocksCanSeeUntilExpire()
        {
            MapDefinition map = LoadMap("dust2");
            var graph = new MapZoneGraph(map);
            var observer = new PlayerRuntime(MinimalPlayer("fo", "Rifler", 90), 50f)
            {
                ZoneId = "a_short",
                Alive = true,
                Health = 100
            };
            var enemy = new PlayerRuntime(MinimalPlayer("fe", "Rifler", 80), 50f)
            {
                ZoneId = "a_short",
                Alive = true,
                Health = 100
            };
            observer.Definition.attributes.awareness = 90;
            CombatSightRules.FaceToward(observer, enemy, map);

            Assert.That(IntelRules.CanSee(map, observer, enemy, 90f, null, graph, null, 0f), Is.True);

            observer.BlindedUntil = 3.2f;
            Assert.That(IntelRules.CanSee(map, observer, enemy, 90f, null, graph, null, 1f), Is.False);
            Assert.That(IntelRules.CanSee(map, observer, enemy, 90f, null, graph, null, 3.2f), Is.True);
        }

        [Test]
        public void Utility_FlashBlindHitMultiplier_BackBetterThanFace()
        {
            var back = new PlayerRuntime(MinimalPlayer("hb", "Rifler", 80), 50f)
            {
                BlindedUntil = 5f,
                BlindSeverity01 = 0f
            };
            var face = new PlayerRuntime(MinimalPlayer("hf", "Rifler", 80), 50f)
            {
                BlindedUntil = 5f,
                BlindSeverity01 = 1f
            };
            float backMult = UtilityCombatRules.BlindHitMultiplier(back, 1f);
            float faceMult = UtilityCombatRules.BlindHitMultiplier(face, 1f);
            Assert.That(backMult, Is.EqualTo(UtilityCombatRules.FlashBlindHitMultBest).Within(0.001f));
            Assert.That(faceMult, Is.EqualTo(UtilityCombatRules.FlashBlindHitMultWorst).Within(0.001f));
            Assert.That(backMult, Is.GreaterThan(faceMult));
            Assert.That(UtilityCombatRules.BlindHitMultiplier(back, 5f), Is.EqualTo(1f));
        }

        [Test]
        public void UtilityLineups_JsonLoadsDust2Seeds()
        {
            string path = Path.Combine(
                UnityEngine.Application.dataPath, "_Project", "Data", "JSON", "utility_lineups.json");
            Assert.That(File.Exists(path), Is.True);
            UtilityLineupCatalog catalog = Infrastructure.RuntimeContentLoader.ParseUtilityLineupsJson(
                File.ReadAllText(path));
            Assert.That(catalog.CountForMap("dust2"), Is.GreaterThanOrEqualTo(16));

            UtilityLineupEntry longSmoke = catalog.Entries.FirstOrDefault(e =>
                string.Equals(e.fromZoneId, "outside_long", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(e.toZoneId, "a_long", StringComparison.OrdinalIgnoreCase));
            Assert.That(longSmoke, Is.Not.Null);
            Assert.That(longSmoke.kinds, Does.Contain("smoke"));
        }

        [Test]
        public void UtilityLineups_FindCandidates_TopMidXboxSmoke()
        {
            MapDefinition map = LoadMap("dust2");
            var graph = new MapZoneGraph(map);
            UtilityLineupCatalog catalog = UtilityLineupCatalog.FromEntries(
                UtilityLineupRules.CreateDefaultDust2Entries());
            List<UtilityLineupCandidate> hits = UtilityLineupRules.FindCandidates(
                catalog, "dust2", "top_mid", Side.Terrorist, graph, UtilityKind.Smoke);
            Assert.That(hits.Count, Is.GreaterThan(0));
            Assert.That(hits[0].Entry.toZoneId, Is.EqualTo("xbox").IgnoreCase);
            Assert.That(hits[0].Kind, Is.EqualTo(UtilityKind.Smoke));
            Assert.That(hits[0].ExactStand, Is.True);
        }

        [Test]
        public void UtilityLineups_FindCandidates_UnrelatedZoneEmpty()
        {
            MapDefinition map = LoadMap("dust2");
            var graph = new MapZoneGraph(map);
            UtilityLineupCatalog catalog = UtilityLineupCatalog.FromEntries(
                UtilityLineupRules.CreateDefaultDust2Entries());
            List<UtilityLineupCandidate> hits = UtilityLineupRules.FindCandidates(
                catalog, "dust2", "scaffolding", Side.Terrorist, graph);
            Assert.That(hits, Is.Empty);
        }

        [Test]
        public void UtilityLineups_RuntimeLoader_ParsesDiskJson()
        {
            UtilityLineupCatalog catalog = Infrastructure.RuntimeContentLoader.LoadUtilityLineups();
            Assert.That(catalog.CountForMap("dust2"), Is.GreaterThanOrEqualTo(16));
        }

        [Test]
        public void Utility_BehaviorTreeRoundCanEmitFlashUtilityEvent()
        {
            ContentDatabase content = BuildMinimalContent();
            var session = new InteractiveQuickMatchSession(
                content, 42,
                new RuleBasedTacticalPlanner("mid"),
                new BehaviorTreeRoundSimulator(_catalog),
                "navi_2021", "gambit_2021", "dust2",
                BattleSimMode.BehaviorTree, _catalog);

            foreach (PlayerRuntime p in session.State.TeamA.Players.Concat(session.State.TeamB.Players))
            {
                p.Loadout = ItemizedEconomyRules.DefaultPistolLoadout(Side.Terrorist);
                p.Loadout.HasFlash = true;
                p.Loadout.HasSmoke = false;
                p.Loadout.HasHe = false;
                p.Loadout.HasFire = false;
                p.Definition.attributes.utility = 99;
            }

            session.PrepareRound();
            RoundResult round = session.PlayRound("navi_2021", EquipmentLevel.Eco);
            Assert.That(round.events.Any(e =>
                e.type == EventType.Utility &&
                string.Equals(e.weaponId, "flash", StringComparison.OrdinalIgnoreCase)), Is.True);
        }

        [Test]
        public void Utility_BehaviorTreeRoundCanEmitUtilityEvent()
        {
            ContentDatabase content = BuildMinimalContent();
            var session = new InteractiveQuickMatchSession(
                content, 42,
                new RuleBasedTacticalPlanner("mid"),
                new BehaviorTreeRoundSimulator(_catalog),
                "navi_2021", "gambit_2021", "dust2",
                BattleSimMode.BehaviorTree, _catalog);

            foreach (PlayerRuntime p in session.State.TeamA.Players.Concat(session.State.TeamB.Players))
            {
                p.Loadout = ItemizedEconomyRules.DefaultPistolLoadout(Side.Terrorist);
                p.Loadout.HasSmoke = true;
                p.Definition.attributes.utility = 99;
            }

            session.PrepareRound();
            RoundResult round = session.PlayRound("navi_2021", EquipmentLevel.Eco);
            Assert.That(round.events.Any(e => e.type == EventType.Utility), Is.True);
        }

        [Test]
        public void IntelRules_ExpiresAndFormatsCallout()
        {
            var note = new IntelNote
            {
                ZoneId = "a_long",
                Count = 2,
                WeaponIds = new[] { "awp", "ak47" },
                SeenAt = 1f
            };
            Assert.That(IntelRules.IsFresh(note, 5f, 8f), Is.True);
            Assert.That(IntelRules.IsFresh(note, 12f, 8f), Is.False);
            Assert.That(IntelRules.IsNewOrChanged(null, note), Is.True);
            Assert.That(IntelRules.IsNewOrChanged(note, note), Is.False);
            string msg = IntelRules.FormatCallout(note, _catalog);
            Assert.That(msg, Does.StartWith("callout|2|a_long|").IgnoreCase);
            Assert.That(msg, Does.Contain("AWP").IgnoreCase);
            Assert.That(IntelRules.CalloutCooldown(90f), Is.LessThan(IntelRules.CalloutCooldown(40f)));
        }

        [Test]
        public void GroundLootRules_WeaponPriorityPrefersAkAndRoleSnipers()
        {
            Assert.That(GroundLootRules.PreferWeapon("ak47", "Galil_AR", "Rifler"), Is.True);
            Assert.That(GroundLootRules.PreferWeapon("awp", "ak47", "Awper"), Is.True);
            Assert.That(GroundLootRules.PreferWeapon("ak47", "awp", "Rifler"), Is.True);
            // SSG is awper-only and ranks below M4.
            Assert.That(GroundLootRules.PreferWeapon("ssg08", null, "Rifler"), Is.False);
            Assert.That(GroundLootRules.PreferWeapon("ssg08", "mac10", "Rifler"), Is.False);
            Assert.That(GroundLootRules.PreferWeapon("m4a4", "ssg08", "Awper"), Is.True);
            Assert.That(GroundLootRules.PreferWeapon("ssg08", "Galil_AR", "Awper"), Is.True);
            Assert.That(GroundLootRules.PreferWeapon("awp", "ssg08", "Awper"), Is.True);
            Assert.That(GroundLootRules.PreferWeapon("m4a4", "mac10", "Rifler"), Is.True);
        }

        [Test]
        public void GroundLootRules_RetrieveAndGuardGates()
        {
            Assert.That(GroundLootRules.ShouldRetrieveDroppedBomb(Side.Terrorist, false, "mid", 3, 2), Is.True);
            Assert.That(GroundLootRules.ShouldRetrieveDroppedBomb(Side.Terrorist, false, "mid", 1, 4), Is.False);
            Assert.That(GroundLootRules.ShouldRetrieveDroppedBomb(Side.CounterTerrorist, false, "mid", 5, 1), Is.False);
            Assert.That(GroundLootRules.ShouldGuardDroppedBomb(Side.CounterTerrorist, false, "mid"), Is.True);
            Assert.That(GroundLootRules.ShouldGuardDroppedBomb(Side.Terrorist, false, "mid"), Is.False);
            Assert.That(GroundLootRules.ShouldGuardDroppedBomb(Side.CounterTerrorist, true, "mid"), Is.False);
        }

        [Test]
        public void BombExplosionRules_EpicenterKillsAndArmorHalves()
        {
            Assert.That(BombExplosionRules.RawDamage(0f), Is.EqualTo(500f).Within(0.1f));
            Assert.That(BombExplosionRules.RawDamage(BombExplosionRules.RadiusHu), Is.EqualTo(0f));
            Assert.That(BombExplosionRules.MaxDamageForMap("nuke"), Is.EqualTo(400f));
            Assert.That(BombExplosionRules.MaxDamageForMap("inferno"), Is.EqualTo(620f));

            var naked = new PlayerRuntime(MinimalPlayer("n1", "Rifler", 70), 50f)
            {
                Health = 100,
                ArmorHp = 0,
                Alive = true,
                Loadout = new PlayerLoadout { HasArmor = false }
            };
            BombExplosionRules.ApplyToPlayer(naked, 500f);
            Assert.That(naked.Alive, Is.False);

            var armored = new PlayerRuntime(MinimalPlayer("a1", "Rifler", 70), 50f)
            {
                Health = 100,
                ArmorHp = 100,
                Alive = true,
                Loadout = new PlayerLoadout { HasArmor = true, HasHelmet = true }
            };
            BombExplosionRules.ApplyToPlayer(armored, 200f);
            // 200 raw → 100 HP + 50 armor absorbed
            Assert.That(armored.Alive, Is.False);
            Assert.That(armored.Health, Is.EqualTo(0));
            Assert.That(armored.ArmorHp, Is.EqualTo(50));
        }

        [Test]
        public void BombExplosionRules_FarArmoredCanSurvive()
        {
            // ~788 HU armored should be near the 1-HP survival rim for 500-max blasts.
            float raw = BombExplosionRules.RawDamage(788f);
            Assert.That(raw, Is.GreaterThan(150f).And.LessThan(220f));
            var armored = new PlayerRuntime(MinimalPlayer("a2", "Rifler", 70), 50f)
            {
                Health = 100,
                ArmorHp = 100,
                Alive = true,
                Loadout = new PlayerLoadout { HasArmor = true, HasHelmet = true }
            };
            BombExplosionRules.ApplyToPlayer(armored, raw);
            Assert.That(armored.Alive, Is.True);
            Assert.That(armored.Health, Is.GreaterThan(0).And.LessThanOrEqualTo(30));
        }

        [Test]
        public void BehaviorTreeRound_AssignsSpreadPersonalTargets()
        {
            ContentDatabase content = BuildMinimalContent();
            var session = new InteractiveQuickMatchSession(
                content, 11,
                new RuleBasedTacticalPlanner("a_long"),
                new BehaviorTreeRoundSimulator(_catalog),
                "navi_2021", "gambit_2021", "dust2",
                BattleSimMode.BehaviorTree, _catalog);
            session.PrepareRound();
            RoundResult round = session.PlayRound("navi_2021", EquipmentLevel.Eco);
            var attackerIds = new HashSet<string>(
                session.State.TeamA.Definition.playerIds, StringComparer.OrdinalIgnoreCase);
            // Use late-round positions before timeout so paths have settled toward assignments.
            var lastZones = round.events
                .Where(e => e.type == EventType.Move && attackerIds.Contains(e.actorId))
                .GroupBy(e => e.actorId, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderByDescending(x => x.roundTime).ThenByDescending(x => x.sequence).First())
                .Select(e => e.zoneId)
                .Where(z => !z.StartsWith("nav_", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            Assert.That(lastZones.Count, Is.GreaterThanOrEqualTo(3),
                string.Join(",", lastZones));
        }

        [Test]
        public void MapDeploymentRules_DefendBSite_SpreadsAcrossBothSites()
        {
            string[] pool = MapDeploymentRules.BuildDefendSpreadPool("dust2", "b_site");
            Assert.That(pool.Length, Is.EqualTo(5));
            Assert.That(pool.Distinct(StringComparer.OrdinalIgnoreCase).Count(), Is.GreaterThanOrEqualTo(4));

            bool hasA = pool.Any(z =>
                MapDeploymentRules.ClassifyDeploySide(z) == MapDeploymentRules.DeploySide.A);
            bool hasB = pool.Any(z =>
                MapDeploymentRules.ClassifyDeploySide(z) == MapDeploymentRules.DeploySide.B);
            Assert.That(hasA, Is.True);
            Assert.That(hasB, Is.True);

            int maxSameSite = pool
                .Where(z => MapDeploymentRules.ClassifyDeploySide(z) != MapDeploymentRules.DeploySide.Mid)
                .GroupBy(z => MapZones.ResolvePlantSite(z), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.Count())
                .DefaultIfEmpty(0)
                .Max();
            Assert.That(maxSameSite, Is.LessThanOrEqualTo(3));
        }

        [Test]
        public void MapDeploymentRules_AttackALong_HasOneBSideLurk()
        {
            string[] pool = MapDeploymentRules.BuildAttackSpreadPool("dust2", "a_long");
            Assert.That(pool.Length, Is.EqualTo(5));
            int bSide = pool.Count(z =>
                MapDeploymentRules.ClassifyDeploySide(z) == MapDeploymentRules.DeploySide.B);
            Assert.That(bSide, Is.EqualTo(1));
            Assert.That(pool.Distinct(StringComparer.OrdinalIgnoreCase).Count(), Is.EqualTo(5));
        }

        [Test]
        public void BehaviorTreeRound_UsesFineTickButWholeSecondClockTags()
        {
            Assert.That(BehaviorTreeRoundSimulator.TickSeconds, Is.EqualTo(0.05f).Within(0.0001f));
            Assert.That(BehaviorTreeRoundSimulator.MoveIntervalSeconds, Is.EqualTo(0.28f).Within(0.0001f));
            ContentDatabase content = BuildMinimalContent();
            var session = new InteractiveQuickMatchSession(
                content, 3,
                new RuleBasedTacticalPlanner("mid"),
                new BehaviorTreeRoundSimulator(_catalog),
                "navi_2021", "gambit_2021", "dust2",
                BattleSimMode.BehaviorTree, _catalog);
            session.PrepareRound();
            RoundResult round = session.PlayRound("navi_2021", EquipmentLevel.Eco);
            Assert.That(round.events.Any(e =>
                e.type == EventType.Move && e.roundTime > 0.02f && e.roundTime < 0.6f), Is.True);
        }

        [Test]
        public void GameSettings_BehaviorTreeIsImplemented()
        {
            Assert.That(GameSettingsRules.IsBattleModeImplemented(BattleSimMode.BehaviorTree), Is.True);
            Assert.That(GameSettingsRules.BattleModeLabel(BattleSimMode.BehaviorTree), Is.EqualTo("一般模式"));
        }

        [Test]
        public void MapZoneGraph_DisconnectedMapDoesNotInventDirectPath()
        {
            var map = new MapDefinition
            {
                zones = new[]
                {
                    new MapZoneDefinition { id = "left" },
                    new MapZoneDefinition { id = "right" }
                },
                routes = Array.Empty<MapRouteDefinition>()
            };
            Assert.That(new MapZoneGraph(map).FindPath("left", "right"), Is.Empty);
        }

        [TestCase("nuke", 2)]
        [TestCase("vertigo", 2)]
        public void MultiFloorMaps_HaveExplicitConnectedFloorTransitions(string mapId, int expectedFloors)
        {
            MapDefinition map = LoadMap(mapId);
            Assert.That(map.floors, Has.Length.EqualTo(expectedFloors));
            var zones = map.zones.ToDictionary(x => x.id, StringComparer.OrdinalIgnoreCase);
            int transitions = 0;
            foreach (MapRouteDefinition route in map.routes)
            {
                string[] points = route.waypoints ?? new[] { route.from, route.to };
                for (int i = 1; i < points.Length; i++)
                    if (!string.Equals(zones[points[i - 1]].floor, zones[points[i]].floor,
                            StringComparison.OrdinalIgnoreCase))
                        transitions++;
            }
            Assert.That(transitions, Is.GreaterThanOrEqualTo(2));

            var graph = new MapZoneGraph(map);
            foreach (MapZoneDefinition zone in map.zones)
                Assert.That(graph.FindPath("t_spawn", zone.id), Is.Not.Empty, zone.id);
        }

        [Test]
        public void AllPlayableMaps_HaveValidConnectedRouteGraphs()
        {
            foreach (string mapId in new[]
                     {
                         "dust2", "train", "ancient", "overpass",
                         "vertigo", "inferno", "mirage", "nuke"
                     })
            {
                MapDefinition map = LoadMap(mapId);
                var ids = new HashSet<string>(map.zones.Select(x => x.id), StringComparer.OrdinalIgnoreCase);
                foreach (MapRouteDefinition route in map.routes)
                    foreach (string point in route.waypoints ?? new[] { route.from, route.to })
                        Assert.That(ids.Contains(point), Is.True, $"{mapId}:{route.id}:{point}");

                var graph = new MapZoneGraph(map);
                foreach (MapZoneDefinition zone in map.zones.Where(x => !x.navigationOnly))
                    Assert.That(graph.FindPath("t_spawn", zone.id), Is.Not.Empty, $"{mapId}:{zone.id}");
            }
        }

        [Test]
        public void AllPlayableMapJson_PassesContentNavigationValidation()
        {
            string root = Path.Combine(
                UnityEngine.Application.dataPath, "_Project", "Data", "JSON");
            string Read(string name) => File.ReadAllText(Path.Combine(root, name));
            var asset = UnityEngine.ScriptableObject.CreateInstance<Infrastructure.ContentDatabaseAsset>();
            Infrastructure.ContentJsonParser.ParseInto(
                asset,
                Read("players.json"),
                Read("teams.json"),
                Read("traits.json"),
                Read("dust2.json"),
                tacticalCardsJson: Read("tactical_cards.json"),
                focusCardsJson: Read("focus_cards.json"),
                coachCardsJson: Read("coach_cards.json"),
                trainMapJson: Read("train.json"),
                ancientMapJson: Read("ancient.json"),
                overpassMapJson: Read("overpass.json"),
                vertigoMapJson: Read("vertigo.json"),
                infernoMapJson: Read("inferno.json"),
                mirageMapJson: Read("mirage.json"),
                nukeMapJson: Read("nuke.json"));
            Assert.That(Infrastructure.ContentValidator.Validate(asset), Is.Empty);
        }

        [Test]
        public void WeaponAmmo_AkConsumesThenReloadsMagazine()
        {
            var player = new PlayerRuntime(MinimalPlayer("a", "Rifler", 80), 50f);
            player.Loadout = new PlayerLoadout { PrimaryWeaponId = "ak47", PistolWeaponId = "glock" };
            WeaponAmmoRules.FillForLoadout(player, _catalog);
            WeaponDefinition ak = _catalog.GetOrKnife("ak47");
            Assert.That(ak.magazineSize, Is.EqualTo(30));
            Assert.That(player.PrimaryMag, Is.EqualTo(ak.magazineSize));
            Assert.That(player.PrimaryReserve, Is.EqualTo(ak.reserveAmmo));

            for (int i = 0; i < ak.magazineSize; i++)
            {
                Assert.That(WeaponAmmoRules.TryConsumeShot(player, _catalog, i * 0.1f, out string id), Is.True);
                Assert.That(id, Is.EqualTo("ak47").IgnoreCase);
            }

            Assert.That(player.PrimaryMag, Is.EqualTo(0));
            Assert.That(WeaponAmmoRules.TryConsumeShot(player, _catalog, 3.1f, out _), Is.False);
            Assert.That(WeaponAmmoRules.IsReloading(player, 3.1f), Is.True);
            WeaponAmmoRules.TickReload(player, _catalog, 3.1f + ak.reloadTime + 0.05f);
            Assert.That(WeaponAmmoRules.IsReloading(player, 3.1f + ak.reloadTime + 0.05f), Is.False);
            Assert.That(player.PrimaryMag, Is.EqualTo(ak.magazineSize));
            Assert.That(player.PrimaryReserve, Is.EqualTo(ak.reserveAmmo - ak.magazineSize));
        }

        [Test]
        public void WeaponAmmo_KnifeDoesNotConsume()
        {
            var player = new PlayerRuntime(MinimalPlayer("a", "Entry", 80), 50f);
            player.Loadout = new PlayerLoadout { PrimaryWeaponId = "knife", PistolWeaponId = null };
            WeaponAmmoRules.FillForLoadout(player, _catalog);
            for (int i = 0; i < 5; i++)
                Assert.That(WeaponAmmoRules.TryConsumeShot(player, _catalog, i, out string id), Is.True);
            Assert.That(player.PrimaryMag, Is.EqualTo(0));
            Assert.That(WeaponAmmoRules.IsReloading(player, 10f), Is.False);
        }

        [Test]
        public void WeaponAmmo_PreferPistolWhenPrimaryDry()
        {
            var player = new PlayerRuntime(MinimalPlayer("a", "Rifler", 80), 50f);
            player.Loadout = new PlayerLoadout { PrimaryWeaponId = "ak47", PistolWeaponId = "glock" };
            WeaponAmmoRules.FillForLoadout(player, _catalog);
            player.PrimaryMag = 0;
            player.PrimaryReserve = 0;
            Assert.That(WeaponAmmoRules.TryConsumeShot(player, _catalog, 0f, out _), Is.False);
            Assert.That(player.PreferPistol, Is.True);
            Assert.That(WeaponAmmoRules.ActiveWeaponId(player), Is.EqualTo("glock").IgnoreCase);
            Assert.That(WeaponAmmoRules.TryConsumeShot(player, _catalog, 0.2f, out string id), Is.True);
            Assert.That(id, Is.EqualTo("glock").IgnoreCase);
            Assert.That(player.PistolMag, Is.EqualTo(_catalog.GetOrKnife("glock").magazineSize - 1));
        }

        [Test]
        public void WeaponAmmo_EmptyMagReloadsInContact_LowMagWaitsForQuiet()
        {
            var player = new PlayerRuntime(MinimalPlayer("a", "Rifler", 80), 50f);
            player.Loadout = new PlayerLoadout { PrimaryWeaponId = "ak47", PistolWeaponId = "glock" };
            WeaponAmmoRules.FillForLoadout(player, _catalog);
            player.PrimaryMag = 0;
            Assert.That(WeaponAmmoRules.ShouldReload(player, _catalog, 0f, inContact: true), Is.True);

            player.PrimaryMag = 8;
            Assert.That(WeaponAmmoRules.ShouldReload(player, _catalog, 0f, inContact: true), Is.False);
            Assert.That(WeaponAmmoRules.ShouldReload(player, _catalog, 0f, inContact: false), Is.True);
        }

        [Test]
        public void CombatTagging_FirstAkHitSlowsThenRecovers()
        {
            var victim = new PlayerRuntime(MinimalPlayer("v", "Rifler", 80), 50f);
            victim.Loadout = new PlayerLoadout { PrimaryWeaponId = "ak47", PistolWeaponId = "glock" };
            WeaponDefinition ak = _catalog.GetOrKnife("ak47");
            Assert.That(victim.VelocityModifier, Is.EqualTo(1f));
            CombatTaggingRules.ApplyHit(victim, ak, ak);
            Assert.That(victim.VelocityModifier, Is.LessThan(0.5f));
            Assert.That(victim.VelocityModifier, Is.GreaterThanOrEqualTo(0.2f));
            float taggedHop = CombatTaggingRules.ScaledMoveInterval(0.28f, victim);
            Assert.That(taggedHop, Is.GreaterThan(0.28f));

            float afterHit = victim.VelocityModifier;
            CombatTaggingRules.ApplyHit(victim, ak, ak);
            Assert.That(victim.VelocityModifier, Is.LessThanOrEqualTo(afterHit + 0.0001f));

            for (int i = 0; i < 80; i++)
                CombatTaggingRules.Tick(victim, 0.05f);
            Assert.That(victim.VelocityModifier, Is.EqualTo(1f).Within(0.02f));
        }

        private static TeamRuntime PistolTeam(string prefix, Side side)
        {
            var players = new List<PlayerRuntime>
            {
                new PlayerRuntime(MinimalPlayer(prefix + "e", "Entry", 70), 50f)
                {
                    Money = ItemizedEconomyRules.StartingPlayerMoney,
                    Loadout = ItemizedEconomyRules.DefaultPistolLoadout(side)
                }
            };
            for (int i = 0; i < 4; i++)
            {
                players.Add(new PlayerRuntime(MinimalPlayer(prefix + "r" + i, "Rifler", 70), 50f)
                {
                    Money = ItemizedEconomyRules.StartingPlayerMoney,
                    Loadout = ItemizedEconomyRules.DefaultPistolLoadout(side)
                });
            }

            var team = new TeamRuntime(
                new TeamDefinition
                {
                    id = prefix,
                    displayName = prefix,
                    playerIds = players.Select(p => p.Definition.id).ToArray()
                },
                players)
            {
                Side = side
            };
            ItemizedEconomyRules.SyncTeamMoney(team);
            return team;
        }

        private static void AssertHitProduct(WeaponDefinition weapon, float aim, float focus, float dist)
        {
            ShotAccuracy acc = CombatSightRules.Evaluate(weapon, aim, focus, dist);
            float focusMult = focus <= 0f ? 1f : focus;
            float expected = acc.AimFactor * acc.RangeFactor * acc.StanceFactor * focusMult;
            if (expected < 0.04f)
                expected = 0.04f;
            if (expected > 0.93f)
                expected = 0.93f;
            Assert.That(acc.HitChance, Is.EqualTo(expected).Within(0.0001f));
            Assert.That(CombatSightRules.HitChance(weapon, aim, focus, dist), Is.EqualTo(acc.HitChance));
        }

        private static PlayerDefinition MinimalPlayer(string id, string role, int aim) =>
            new PlayerDefinition
            {
                id = id,
                displayName = id,
                primaryRole = role,
                attributes = new CombatAttributes
                {
                    aim = aim,
                    reaction = aim,
                    awareness = 70,
                    utility = 60,
                    teamwork = 70,
                    communication = 70,
                    opening = 70,
                    clutch = 70
                },
                traitIds = System.Array.Empty<string>()
            };

        private static ContentDatabase BuildMinimalContent()
        {
            // Reuse JSON via Runtime path when possible; fall back to in-memory dust2-like map.
            string root = System.IO.Path.Combine(
                UnityEngine.Application.dataPath, "_Project", "Data", "JSON");
            var asset = UnityEngine.ScriptableObject.CreateInstance<Infrastructure.ContentDatabaseAsset>();
            Infrastructure.ContentJsonParser.ParseInto(
                asset,
                System.IO.File.ReadAllText(System.IO.Path.Combine(root, "players.json")),
                System.IO.File.ReadAllText(System.IO.Path.Combine(root, "teams.json")),
                System.IO.File.ReadAllText(System.IO.Path.Combine(root, "traits.json")),
                System.IO.File.ReadAllText(System.IO.Path.Combine(root, "dust2.json")),
                System.IO.File.ReadAllText(System.IO.Path.Combine(root, "tactical_cards.json")));
            return asset.BuildDomain();
        }

        private static MapDefinition LoadMap(string mapId)
        {
            string path = Path.Combine(
                UnityEngine.Application.dataPath, "_Project", "Data", "JSON", mapId + ".json");
            return UnityEngine.JsonUtility.FromJson<Infrastructure.MapJsonRoot>(
                File.ReadAllText(path)).map;
        }

        private sealed class FixedRandom : IRandomSource
        {
            private readonly float _value;
            public FixedRandom(float value) => _value = value;
            public float Next01() => _value;
            public int Range(int inclusiveMin, int exclusiveMax) => inclusiveMin;
        }
    }
}
