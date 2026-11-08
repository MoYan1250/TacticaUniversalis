using System;
using System.Collections.Generic;
using System.Linq;

namespace TacticaUniversalis.Domain
{
    /// <summary>Mutable world shared by BT agents during one general-mode round.</summary>
    public sealed class BehaviorTreeRoundWorld
    {
        public RoundSimulationRequest Request { get; set; }
        public RoundResult Result { get; set; }
        public WeaponCatalog Catalog { get; set; }
        public MapZoneGraph Graph { get; set; }
        public bool BombPlanted { get; set; }
        public bool BombDefused { get; set; }
        public bool BombExploded { get; set; }
        public string PlantSiteId { get; set; }
        public string BombCarrierId { get; set; }
        /// <summary>Zone where the bomb sits after the carrier died pre-plant.</summary>
        public string DroppedBombZoneId { get; set; }
        public float PlantTime { get; set; } = -1f;
        /// <summary>T currently channeling the plant, or null.</summary>
        public string PlanterId { get; set; }
        public float PlantStartTime { get; set; } = -1f;
        public float PlantRequiredSeconds { get; set; }
        public float RoundTimeLimit { get; set; } = 115f;
        public float BombTimer { get; set; } = 40f;
        /// <summary>CT currently channeling the defuse, or null.</summary>
        public string DefuserId { get; set; }
        public float DefuseStartTime { get; set; } = -1f;
        public float DefuseRequiredSeconds { get; set; }
        /// <summary>Short hold/peek after plant cancel or fake defuse.</summary>
        public Dictionary<string, float> MindGameUntilTime { get; } =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        public int Sequence { get; set; }
        public bool BuysApplied { get; set; }
        public List<GroundLootItem> GroundLoot { get; } = new List<GroundLootItem>();
        public HashSet<string> SaveAnnounced { get; } =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        /// <summary>Per-player hold / approach zone so teammates do not stack on one node.</summary>
        public Dictionary<string, string> PersonalTargets { get; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        /// <summary>Committed rotate destinations (site id).</summary>
        public Dictionary<string, string> RotateTargets { get; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        /// <summary>Last time this player saw an enemy on their hold site.</summary>
        public Dictionary<string, float> LastSiteThreatTime { get; } =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        /// <summary>Cached save rolls keyed by playerId:selfAlive:enemyAlive.</summary>
        public Dictionary<string, bool> SaveRolls { get; } =
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        /// <summary>Last quiet-rotate roll time per player.</summary>
        public Dictionary<string, float> LastQuietRotateRoll { get; } =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        /// <summary>Zone this player is holding as cover for peeks.</summary>
        public Dictionary<string, string> CoverAnchor { get; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, PlayerPeekState> Peeks { get; } =
            new Dictionary<string, PlayerPeekState>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, float> LastSearchTime { get; } =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, int> SearchSweepIndex { get; } =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, IntelBoard> PersonalIntel { get; } =
            new Dictionary<string, IntelBoard>(StringComparer.OrdinalIgnoreCase);
        public IntelBoard AttackerTeamIntel { get; } = new IntelBoard();
        public IntelBoard DefenderTeamIntel { get; } = new IntelBoard();
        public Dictionary<string, float> LastCalloutTime { get; } =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        public List<ActiveUtilityEffect> ActiveUtilities { get; } = new List<ActiveUtilityEffect>();
        public Dictionary<string, float> LastUtilityThrowTime { get; } =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        /// <summary>Pro-style long-range lineups; defaults to <see cref="UtilityLineupRules.Shared"/>.</summary>
        public UtilityLineupCatalog UtilityLineups { get; set; }

        public void Add(
            float time, EventType type, string actorId, string targetId, string zoneId,
            string message, string teamId, string weaponId = null, params string[] tags)
        {
            Result.events.Add(new SimulationEvent
            {
                sequence = Sequence++,
                roundNumber = Request.roundNumber,
                roundTime = time,
                type = type,
                actorId = actorId,
                targetId = targetId,
                teamId = teamId,
                zoneId = zoneId,
                weaponId = weaponId,
                message = message,
                reasonTags = tags ?? Array.Empty<string>()
            });
        }
    }

    public sealed class PlayerPeekState
    {
        public string CoverZoneId;
        public string PeekZoneId;
        public float ExposedAt = -1f;
        public float NextPeekAt;
        public bool Returning;
        public bool Wide;
        public bool SawContact;
    }

    public sealed class BehaviorTreeRoundSimulator : IRoundSimulator
    {
        /// <summary>Simulation clock step — fine enough for combat, moves coalesce in UI.</summary>
        public const float TickSeconds = 0.05f;
        /// <summary>Logical zone hop interval — ~20s+ for long CT rotates to site.</summary>
        public const float MoveIntervalSeconds = 0.28f;
        /// <summary>How often zone fights are resolved.</summary>
        public const float CombatIntervalSeconds = 0.20f;
        /// <summary>Hold-site quiet time before probabilistic rotate.</summary>
        public const float QuietHoldSeconds = 10f;
        /// <summary>Jiggle peek hop is faster than a normal zone walk.</summary>
        public const float PeekMoveIntervalSeconds = 0.12f;
        public const float PeekHoldSeconds = 0.18f;
        public const float WidePeekMoveIntervalSeconds = 0.16f;
        public const float WidePeekHoldSeconds = 0.45f;

        private readonly WeaponCatalog _catalog;
        private readonly BtNode _soldierTree;

        public BehaviorTreeRoundSimulator(WeaponCatalog catalog = null)
        {
            _catalog = catalog ?? WeaponCatalog.CreateDefault();
            _soldierTree = BuildSoldierTree();
        }

        public WeaponCatalog Catalog => _catalog;

        public RoundResult Simulate(RoundSimulationRequest request, IRandomSource random)
        {
            var result = new RoundResult();
            var world = new BehaviorTreeRoundWorld
            {
                Request = request,
                Result = result,
                Catalog = _catalog,
                Graph = new MapZoneGraph(request.map),
                Sequence = 0,
                PlantSiteId = MapZones.ResolvePlantSite(request.attackerPlan.primaryZoneId),
                UtilityLineups = UtilityLineupRules.Shared
            };

            string tSpawn = ContentDatabase.ClampZoneToMap(request.map, "t_spawn");
            string ctSpawn = ContentDatabase.ClampZoneToMap(request.map, "ct_spawn");
            if (!request.map.zones.Any(z => string.Equals(z.id, tSpawn, StringComparison.OrdinalIgnoreCase)))
                tSpawn = request.map.zones[0].id;
            if (!request.map.zones.Any(z => string.Equals(z.id, ctSpawn, StringComparison.OrdinalIgnoreCase)))
                ctSpawn = request.map.zones[Math.Min(1, request.map.zones.Length - 1)].id;

            foreach (PlayerRuntime p in request.attackers.Players)
                ItemizedEconomyRules.ResetRoundCombat(p, tSpawn, Side.Terrorist, _catalog);
            foreach (PlayerRuntime p in request.defenders.Players)
                ItemizedEconomyRules.ResetRoundCombat(p, ctSpawn, Side.CounterTerrorist, _catalog);
            foreach (PlayerRuntime p in request.attackers.Players)
                CombatSightRules.FaceTowardZone(p, ctSpawn, request.map);
            foreach (PlayerRuntime p in request.defenders.Players)
                CombatSightRules.FaceTowardZone(p, tSpawn, request.map);

            ApplyBuys(world, random);
            AssignPersonalTargets(world);
            world.Add(0f, EventType.RoundStarted, null, null,
                request.attackerPlan.primaryZoneId,
                $"Round {request.roundNumber}: {request.attackers.Definition.displayName} attacks",
                request.attackers.Definition.id);
            RoundTraitRules.ActivateRoundStart(request, result, random);
            world.Sequence = Math.Max(world.Sequence, result.events.Count);
            EmitHpSnapshot(world, 0.01f);

            world.BombCarrierId = SelectBombCarrier(request.attackers)?.Definition.id;
            if (!string.IsNullOrEmpty(world.BombCarrierId))
            {
                world.Add(0.02f, EventType.StatusUpdated, world.BombCarrierId, null, tSpawn,
                    "bomb:1", request.attackers.Definition.id, null, "bomb_carrier");
            }

            float time = 0f;
            float lastHeartbeat = -1f;
            float lastCombat = -CombatIntervalSeconds;
            while (time <= world.RoundTimeLimit)
            {
                if (time - lastHeartbeat >= 1f)
                {
                    world.Add(time, EventType.StatusUpdated, null, null,
                        world.Request.attackerPlan.primaryZoneId,
                        $"clock:{time:0}",
                        null);
                    lastHeartbeat = time;
                }

                TickTagging(world);
                TickUtilities(world, time);
                TickAgents(world, time, random);
                PlantChannelRules.Tick(world, time);
                TickDefuse(world, time);
                if (time - lastCombat >= CombatIntervalSeconds - 0.0001f)
                {
                    ResolveZoneFights(world, time, random);
                    lastCombat = time;
                }

                if (TryConcludeElimination(world, time, out TeamRuntime winTeam, out string reason))
                {
                    FinishRound(world, winTeam, reason, time);
                    return result;
                }

                if (world.BombPlanted && !world.BombDefused && !world.BombExploded &&
                    time >= world.PlantTime + world.BombTimer)
                {
                    world.BombExploded = true;
                    BombExplosionRules.ApplyExplosion(world, time);
                    world.Add(time, EventType.BombExploded, null, null, world.PlantSiteId,
                        "Bomb exploded", request.attackers.Definition.id);
                    FinishRound(world, request.attackers, "bomb_exploded", time + 0.5f);
                    return result;
                }

                if (world.BombDefused)
                {
                    FinishRound(world, request.defenders, "bomb_defused", time + 0.5f);
                    return result;
                }

                time += TickSeconds;
            }

            // Timeout: T lose unless bomb planted (then wait already handled above).
            if (world.BombPlanted && !world.BombDefused)
            {
                world.BombExploded = true;
                BombExplosionRules.ApplyExplosion(world, time);
                world.Add(time, EventType.BombExploded, null, null, world.PlantSiteId,
                    "Bomb exploded", request.attackers.Definition.id);
                FinishRound(world, request.attackers, "bomb_exploded", time + 0.5f);
            }
            else
            {
                world.Add(time, EventType.Timeout, null, null, request.attackerPlan.primaryZoneId,
                    "Round timeout", request.defenders.Definition.id);
                FinishRound(world, request.defenders, "timeout", time + 0.5f);
            }

            return result;
        }

        private void ApplyBuys(BehaviorTreeRoundWorld world, IRandomSource random)
        {
            if (world.BuysApplied)
                return;
            bool pistol = MatchRules.IsPistolRound(Math.Max(0, world.Request.roundNumber - 1));
            if (pistol)
            {
                BehaviorTreeBuyRules.BuyPistolRound(world.Request.attackers, _catalog, random);
                BehaviorTreeBuyRules.BuyPistolRound(world.Request.defenders, _catalog, random);
            }
            else
            {
                // Economy card = team-wide spend intent; each agent picks guns by role preference.
                // FullBuy also drops spare util at spawn and may help-buy rifles.
                MapDefinition map = world.Request.map;
                string tSpawn = ContentDatabase.ClampZoneToMap(map, "t_spawn");
                string ctSpawn = ContentDatabase.ClampZoneToMap(map, "ct_spawn");
                BehaviorTreeBuyRules.BuyForTeam(
                    world.Request.attackers,
                    world.Request.attackerPlan.equipmentLevel,
                    _catalog,
                    world.GroundLoot,
                    tSpawn,
                    random);
                BehaviorTreeBuyRules.BuyForTeam(
                    world.Request.defenders,
                    world.Request.defenderPlan.equipmentLevel,
                    _catalog,
                    world.GroundLoot,
                    ctSpawn,
                    random);
            }
            world.BuysApplied = true;
            world.Add(0.02f, EventType.Setup, null, null, world.Request.attackerPlan.primaryZoneId,
                pistol
                    ? "Equipment PistolRound"
                    : $"Equipment {world.Request.attackerPlan.equipmentLevel} / {world.Request.defenderPlan.equipmentLevel}",
                null);
        }

        private static void TickTagging(BehaviorTreeRoundWorld world)
        {
            foreach (PlayerRuntime player in world.Request.attackers.AlivePlayers)
                CombatTaggingRules.Tick(player, TickSeconds);
            foreach (PlayerRuntime player in world.Request.defenders.AlivePlayers)
                CombatTaggingRules.Tick(player, TickSeconds);
        }

        private static void TickUtilities(BehaviorTreeRoundWorld world, float time)
        {
            UtilityCombatRules.Expire(world.ActiveUtilities, time);
            UtilityCombatRules.TickFire(
                world.ActiveUtilities,
                world.Request.map,
                world.Request.attackers,
                world.Request.defenders,
                time,
                (victim, damage, zoneId) =>
                {
                    TeamRuntime victimTeam = ResolvePlayerTeam(world, victim.Definition.id);
                    string ownerId = FindFireOwner(world, zoneId);
                    PlayerRuntime killer = FindPlayer(world, ownerId);
                    TeamRuntime killerTeam = killer != null
                        ? ResolvePlayerTeam(world, killer.Definition.id)
                        : null;
                    world.Add(time, EventType.StatusUpdated, victim.Definition.id, ownerId, zoneId,
                        $"hp:{victim.Health}|armor:{victim.ArmorHp}",
                        victimTeam?.Definition.id,
                        "fire",
                        "utility_burn");
                    if (!victim.Alive)
                    {
                        RegisterPlayerDeath(
                            world, victim, killer, victimTeam, killerTeam, time,
                            "fire",
                            $"{victim.Definition.id} burned",
                            "utility_burn");
                    }
                });
        }

        private static string FindFireOwner(BehaviorTreeRoundWorld world, string zoneId)
        {
            if (world?.ActiveUtilities == null || string.IsNullOrEmpty(zoneId))
                return null;
            for (int i = 0; i < world.ActiveUtilities.Count; i++)
            {
                ActiveUtilityEffect e = world.ActiveUtilities[i];
                if (e == null || e.Kind != UtilityKind.Fire)
                    continue;
                if (string.Equals(e.ZoneId, zoneId, StringComparison.OrdinalIgnoreCase))
                    return e.OwnerPlayerId;
            }
            return null;
        }

        private static PlayerRuntime FindPlayer(BehaviorTreeRoundWorld world, string playerId)
        {
            if (world?.Request == null || string.IsNullOrEmpty(playerId))
                return null;
            foreach (PlayerRuntime p in world.Request.attackers.Players)
            {
                if (p?.Definition != null &&
                    string.Equals(p.Definition.id, playerId, StringComparison.OrdinalIgnoreCase))
                    return p;
            }
            foreach (PlayerRuntime p in world.Request.defenders.Players)
            {
                if (p?.Definition != null &&
                    string.Equals(p.Definition.id, playerId, StringComparison.OrdinalIgnoreCase))
                    return p;
            }
            return null;
        }

        /// <summary>Emit Kill + always drop loot. Kill reward only for enemy killers.</summary>
        private static void RegisterPlayerDeath(
            BehaviorTreeRoundWorld world,
            PlayerRuntime victim,
            PlayerRuntime killer,
            TeamRuntime victimTeam,
            TeamRuntime killerTeam,
            float time,
            string weaponId,
            string message,
            params string[] tags)
        {
            if (world == null || victim == null)
                return;

            victim.Deaths++;
            bool enemyKill = killer != null &&
                             killerTeam?.Definition != null &&
                             victimTeam?.Definition != null &&
                             !string.Equals(
                                 killerTeam.Definition.id,
                                 victimTeam.Definition.id,
                                 StringComparison.OrdinalIgnoreCase);
            if (enemyKill)
            {
                killer.Kills++;
                killer.RoundKills++;
                ItemizedEconomyRules.ApplyKillReward(killer, world.Catalog, weaponId);
                ItemizedEconomyRules.SyncTeamMoney(killerTeam);
            }

            world.Add(
                time + 0.1f,
                EventType.Kill,
                killer?.Definition.id,
                victim.Definition.id,
                victim.ZoneId,
                message,
                killerTeam?.Definition.id ?? victimTeam?.Definition.id,
                weaponId,
                tags ?? Array.Empty<string>());
            GroundLootRules.DropFromVictim(world, victim, victimTeam, time);
        }

        private static TeamRuntime ResolvePlayerTeam(BehaviorTreeRoundWorld world, string playerId)
        {
            if (world?.Request == null || string.IsNullOrEmpty(playerId))
                return null;
            if (world.Request.attackers.Players.Exists(p =>
                    string.Equals(p.Definition.id, playerId, StringComparison.OrdinalIgnoreCase)))
                return world.Request.attackers;
            if (world.Request.defenders.Players.Exists(p =>
                    string.Equals(p.Definition.id, playerId, StringComparison.OrdinalIgnoreCase)))
                return world.Request.defenders;
            return null;
        }

        private void TickAgents(BehaviorTreeRoundWorld world, float time, IRandomSource random)
        {
            var ctx = new BtContext
            {
                World = world,
                Time = time,
                Random = random
            };

            foreach (PlayerRuntime player in world.Request.attackers.AlivePlayers)
            {
                ctx.Self = player;
                ctx.SelfTeam = world.Request.attackers;
                ctx.EnemyTeam = world.Request.defenders;
                ctx.SelfPlan = world.Request.attackerPlan;
                WeaponAmmoRules.TickReload(player, _catalog, time);
                ScanAndShareIntel(ctx);
                _soldierTree.Tick(ctx);
                GroundLootRules.TryPickupInZone(
                    world, player, world.Request.attackers, _catalog, time);
            }

            foreach (PlayerRuntime player in world.Request.defenders.AlivePlayers)
            {
                ctx.Self = player;
                ctx.SelfTeam = world.Request.defenders;
                ctx.EnemyTeam = world.Request.attackers;
                ctx.SelfPlan = world.Request.defenderPlan;
                WeaponAmmoRules.TickReload(player, _catalog, time);
                ScanAndShareIntel(ctx);
                _soldierTree.Tick(ctx);
                GroundLootRules.TryPickupInZone(
                    world, player, world.Request.defenders, _catalog, time);
            }
        }

        private static BtNode BuildSoldierTree() =>
            new BtSelector(
                // 0a. T retrieve dropped bomb (unless desperate 1vX save)
                new BtSequence(
                    new BtCondition(ShouldRetrieveBomb),
                    new BtAction(ActRetrieveBomb)),
                // 0b. CT abandon sites and guard dropped bomb
                new BtSequence(
                    new BtCondition(ShouldGuardDroppedBomb),
                    new BtAction(ActGuardDroppedBomb)),
                // 1. Save: hide when disadvantaged; stay and fight if already contacted
                new BtSequence(
                    new BtCondition(ShouldSave),
                    new BtAction(ActSave)),
                // 2. Numbers advantage vs low enemy count → actively hunt remaining players
                new BtSequence(
                    new BtCondition(ShouldHunt),
                    new BtAction(ActHunt)),
                // 2b. Contest live plant when team heard/saw planting
                new BtSequence(
                    new BtCondition(ShouldContestPlant),
                    new BtAction(ActContestPlant)),
                // 3. Retake after plant
                new BtSequence(
                    new BtCondition(c =>
                        c.World.BombPlanted &&
                        c.SelfTeam.Side == Side.CounterTerrorist &&
                        !c.World.BombDefused),
                    new BtAction(ActRetake)),
                // 4. Rotate / 回防 before plant (other site fallen or quiet hold)
                new BtSequence(
                    new BtCondition(ShouldRotate),
                    new BtAction(ActRotate)),
                // 5. Seek cover when tagged or fighting in the open
                new BtSequence(
                    new BtCondition(ShouldTakeCover),
                    new BtAction(ActSeekCover)),
                // 5a. Silent reload when mag is empty or low and not in contact
                new BtSequence(
                    new BtCondition(ShouldReload),
                    new BtAction(ActReload)),
                // 5a2. Throw smoke / HE / fire when holding nades and a choke makes sense
                new BtSequence(
                    new BtCondition(ShouldThrowUtility),
                    new BtAction(ActThrowUtility)),
                // 5b. Jiggle peek: show a sliver then pull back
                new BtSequence(
                    new BtCondition(ShouldPeek),
                    new BtAction(ActPeek)),
                // 5c. Search angles: sweep facing across neighboring zones
                new BtSequence(
                    new BtCondition(ShouldSearch),
                    new BtAction(ActSearch)),
                // 6. Execute plan (move / plant / hold / defuse)
                new BtAction(ActExecutePlan));

        private static int EnemiesInZone(BtContext c, string zoneId) =>
            c.EnemyTeam.AlivePlayers.Count(e =>
                string.Equals(e.ZoneId, zoneId, StringComparison.OrdinalIgnoreCase));

        private static string SiteKey(string zoneId) =>
            MapZones.ResolvePlantSite(zoneId);

        private static bool ShouldSave(BtContext c)
        {
            if (!c.Self.Alive)
                return false;

            // Bomb carrier never saves — keep pushing plant / recover.
            if (c.SelfTeam.Side == Side.Terrorist &&
                !string.IsNullOrEmpty(c.World.BombCarrierId) &&
                string.Equals(c.World.BombCarrierId, c.Self.Definition.id, StringComparison.OrdinalIgnoreCase))
                return false;

            bool pistolRound = MatchRules.IsPistolRound(Math.Max(0, c.World.Request.roundNumber - 1));
            int weaponPrice = ActiveWeaponPrice(c);
            if (SaveHuntRules.ShouldRefuseSave(pistolRound, weaponPrice))
                return false;

            int aliveSelf = c.SelfTeam.AliveCount;
            int aliveEnemy = c.EnemyTeam.AliveCount;

            // 1v1 / 1v2: stop saving and fight. Equal or better man count: never save.
            if (SaveHuntRules.ShouldAbortSave(aliveSelf, aliveEnemy))
                return false;

            // Need fewer than 3 alive and fewer than the enemy.
            if (aliveSelf >= 3)
                return false;
            if (aliveSelf >= aliveEnemy)
                return false;

            string key = c.Self.Definition.id + ":" + aliveSelf + ":" + aliveEnemy;
            if (c.World.SaveRolls.TryGetValue(key, out bool decided))
                return decided;

            int gap = aliveEnemy - aliveSelf;
            float chance = 0.20f * (3 - aliveSelf) + 0.18f * gap;
            if (c.SelfTeam.Equipment == EquipmentLevel.Eco &&
                (!string.IsNullOrEmpty(c.Self.Loadout?.PrimaryWeaponId) || c.Self.KeptWeaponFromPriorRound))
                chance += 0.12f;
            chance = Math.Min(0.75f, Math.Max(0.15f, chance));
            bool save = c.Random != null && c.Random.Next01() < chance;
            c.World.SaveRolls[key] = save;
            return save;
        }

        private static bool ShouldRetrieveBomb(BtContext c)
        {
            if (!c.Self.Alive)
                return false;
            return GroundLootRules.ShouldRetrieveDroppedBomb(
                c.SelfTeam.Side,
                c.World.BombPlanted,
                c.World.DroppedBombZoneId,
                c.SelfTeam.AliveCount,
                c.EnemyTeam.AliveCount);
        }

        private static BtStatus ActRetrieveBomb(BtContext c)
        {
            string zone = ContentDatabase.ClampZoneToMap(
                c.World.Request.map, c.World.DroppedBombZoneId);
            if (string.IsNullOrEmpty(zone))
                return BtStatus.Failure;
            StepToward(c, zone);
            // Pickup runs after tick in TryPickupInZone.
            return BtStatus.Running;
        }

        private static bool ShouldGuardDroppedBomb(BtContext c)
        {
            if (!c.Self.Alive)
                return false;
            return GroundLootRules.ShouldGuardDroppedBomb(
                c.SelfTeam.Side, c.World.BombPlanted, c.World.DroppedBombZoneId);
        }

        private static BtStatus ActGuardDroppedBomb(BtContext c)
        {
            string zone = ContentDatabase.ClampZoneToMap(
                c.World.Request.map, c.World.DroppedBombZoneId);
            if (string.IsNullOrEmpty(zone))
                return BtStatus.Failure;
            // Converge on the dropped bomb — abandon A/B site holds.
            StepToward(c, zone);
            return BtStatus.Running;
        }

        private static BtStatus ActSave(BtContext c)
        {
            AnnounceSaveOnce(c);

            // Caught by hunters: stay and try for a kill; man-count abort handled next ticks.
            if (EnemiesInZone(c, c.Self.ZoneId) > 0)
                return BtStatus.Running;

            string hide = PickHideZone(c);
            StepToward(c, hide);
            return BtStatus.Running;
        }

        private static void AnnounceSaveOnce(BtContext c)
        {
            if (c.World.SaveAnnounced.Contains(c.Self.Definition.id))
                return;
            c.World.SaveAnnounced.Add(c.Self.Definition.id);
            c.World.Add(c.Time, EventType.Save, c.Self.Definition.id, null, c.Self.ZoneId,
                $"{c.Self.Definition.id} saves weapon", c.SelfTeam.Definition.id);
        }

        private static string PickHideZone(BtContext c)
        {
            string spawn = c.SelfTeam.Side == Side.Terrorist ? "t_spawn" : "ct_spawn";
            spawn = ContentDatabase.ClampZoneToMap(c.World.Request.map, spawn);
            List<PlayerRuntime> enemies = c.EnemyTeam.AlivePlayers.ToList();
            if (enemies.Count == 0)
                return PreferSpawnAdjacentHide(c, spawn);

            var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { spawn };
            if (!string.IsNullOrEmpty(c.Self.ZoneId))
            {
                candidates.Add(c.Self.ZoneId);
                foreach (string n in c.World.Graph.Neighbors(c.Self.ZoneId))
                    candidates.Add(n);
            }
            foreach (string n in c.World.Graph.Neighbors(spawn))
                candidates.Add(n);

            string best = spawn;
            float bestScore = float.MinValue;
            foreach (string zone in candidates)
            {
                if (EnemiesInZone(c, zone) > 0)
                    continue;
                if (!string.Equals(zone, spawn, StringComparison.OrdinalIgnoreCase) &&
                    !IsPlayableHoldZone(c.World.Request.map, zone) &&
                    !(zone?.StartsWith("nav_", StringComparison.OrdinalIgnoreCase) ?? false))
                    continue;

                float nearestEnemy = enemies.Min(e => GraphDistance(c, zone, e.ZoneId));
                float toSpawn = GraphDistance(c, zone, spawn);
                // Prefer far from hunters, still biased toward own spawn for a safe nest.
                float score = nearestEnemy * 4f - toSpawn * 0.35f;
                // Prefer spawn-adjacent playable holds; do not sit on the spawn tile itself.
                if (string.Equals(zone, spawn, StringComparison.OrdinalIgnoreCase))
                    score -= 2.5f;
                else if (c.World.Graph.Neighbors(spawn).Any(n =>
                             string.Equals(n, zone, StringComparison.OrdinalIgnoreCase)))
                    score += 1.2f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = zone;
                }
            }
            return best;
        }

        private static float GraphDistance(BtContext c, string from, string to)
        {
            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
                return 99f;
            if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
                return 0f;
            IReadOnlyList<string> path = c.World.Graph.FindPath(from, to);
            if (path == null || path.Count < 2)
                return 99f;
            return path.Count - 1;
        }

        private static bool ShouldHunt(BtContext c)
        {
            if (!c.Self.Alive)
                return false;
            bool pistolRound = MatchRules.IsPistolRound(Math.Max(0, c.World.Request.roundNumber - 1));
            return SaveHuntRules.ShouldAllowHunt(
                c.SelfTeam.Side == Side.Terrorist,
                c.World.BombPlanted,
                c.SelfTeam.AliveCount,
                c.EnemyTeam.AliveCount,
                pistolRound);
        }

        private static int ActiveWeaponPrice(BtContext c)
        {
            WeaponCatalog catalog = c.World.Catalog;
            if (catalog == null || c.Self.Loadout == null)
                return 0;
            return catalog.GetOrKnife(c.Self.Loadout.ActiveWeaponId).price;
        }

        private static BtStatus ActHunt(BtContext c)
        {
            List<PlayerRuntime> prey = c.EnemyTeam.AlivePlayers
                .OrderBy(p => p.Definition.id, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (prey.Count == 0)
                return BtStatus.Failure;

            int selfIndex = Math.Max(0, c.SelfTeam.Players.FindIndex(p =>
                string.Equals(p.Definition.id, c.Self.Definition.id, StringComparison.OrdinalIgnoreCase)));
            PlayerRuntime target = prey[selfIndex % prey.Count];
            // Spread hunters: stagger onto neighbors of prey when stacked on same target.
            string dest = target.ZoneId;
            if (selfIndex >= prey.Count)
            {
                var near = c.World.Graph.Neighbors(target.ZoneId).OrderBy(z => z).ToList();
                if (near.Count > 0)
                    dest = near[selfIndex % near.Count];
            }
            dest = ContentDatabase.ClampZoneToMap(c.World.Request.map, dest);
            StepToward(c, dest);
            return BtStatus.Running;
        }

        private static bool ShouldRotate(BtContext c)
        {
            // Rotate / 回防 is a defender behavior — attackers keep executing the plan.
            if (!c.Self.Alive || c.World.BombPlanted || c.SelfTeam.Side != Side.CounterTerrorist)
                return false;

            if (c.World.RotateTargets.ContainsKey(c.Self.Definition.id))
                return true;

            string assigned = PersonalTarget(c, c.Self.ZoneId);
            string mySite = SiteKey(assigned);
            string opposite = string.Equals(mySite, "b_site", StringComparison.OrdinalIgnoreCase)
                ? "a_site"
                : "b_site";
            opposite = ContentDatabase.ClampZoneToMap(c.World.Request.map, opposite);

            // Discover: teammates assigned to the other site are all dead → rotate.
            List<PlayerRuntime> oppositeHolders = c.SelfTeam.Players
                .Where(p =>
                {
                    if (!c.World.PersonalTargets.TryGetValue(p.Definition.id, out string tz))
                        return false;
                    return string.Equals(SiteKey(tz), opposite, StringComparison.OrdinalIgnoreCase);
                })
                .ToList();
            if (oppositeHolders.Count > 0 && oppositeHolders.All(p => !p.Alive))
            {
                c.World.RotateTargets[c.Self.Definition.id] = opposite;
                c.World.PersonalTargets[c.Self.Definition.id] = opposite;
                return true;
            }

            // Track threats on the site we are holding.
            bool threat = c.EnemyTeam.AlivePlayers.Any(e =>
                string.Equals(SiteKey(e.ZoneId), mySite, StringComparison.OrdinalIgnoreCase));
            if (threat || !c.World.LastSiteThreatTime.ContainsKey(c.Self.Definition.id))
                c.World.LastSiteThreatTime[c.Self.Definition.id] = c.Time;

            float quiet = c.Time - c.World.LastSiteThreatTime[c.Self.Definition.id];
            float awareness = c.Self.Definition.attributes.awareness + c.Self.FocusAwareness;
            float ttl = IntelRules.TtlSeconds(awareness);
            int intelOpposite = IntelRules.FreshCountOnSite(TeamIntelBoard(c), opposite, c.Time, ttl);
            bool intelPush = intelOpposite >= 2;
            if (quiet < QuietHoldSeconds && !intelPush)
                return false;

            // Probabilistic rotate when this site has been quiet, or intel says 2+ on the other site.
            if (!c.World.LastQuietRotateRoll.TryGetValue(c.Self.Definition.id, out float lastRoll))
                lastRoll = -999f;
            if (c.Time - lastRoll < 2f)
                return false;

            c.World.LastQuietRotateRoll[c.Self.Definition.id] = c.Time;

            float teamwork = c.Self.Definition.attributes.teamwork + c.Self.FocusTeamwork;
            float chance;
            if (quiet < QuietHoldSeconds)
                chance = Math.Min(0.45f, 0.10f + teamwork * 0.003f);
            else
            {
                chance = Math.Min(0.55f, 0.12f + 0.04f * (quiet - QuietHoldSeconds));
                if (intelPush)
                    chance = Math.Min(0.75f, chance + 0.20f + teamwork * 0.002f);
            }

            if (c.Random == null || c.Random.Next01() >= chance)
                return false;

            c.World.RotateTargets[c.Self.Definition.id] = opposite;
            c.World.PersonalTargets[c.Self.Definition.id] = opposite;
            return true;
        }

        private static BtStatus ActRotate(BtContext c)
        {
            if (!c.World.RotateTargets.TryGetValue(c.Self.Definition.id, out string site) ||
                string.IsNullOrEmpty(site))
                return BtStatus.Failure;

            site = ContentDatabase.ClampZoneToMap(c.World.Request.map, site);
            string hold = HoldNearSite(c, site, site);
            StepToward(c, hold);
            return BtStatus.Running;
        }


        private static bool ShouldContestPlant(BtContext c)
        {
            if (!c.Self.Alive || c.SelfTeam.Side != Side.CounterTerrorist || c.World.BombPlanted)
                return false;
            return AcousticIntelRules.BoardHasFreshTag(
                       TeamIntelBoard(c), AcousticIntelRules.TagPlanting, c.Time,
                       AcousticIntelRules.AcousticTtlSeconds) ||
                   AcousticIntelRules.BoardHasFreshTag(
                       PersonalIntelBoard(c), AcousticIntelRules.TagPlanting, c.Time,
                       AcousticIntelRules.AcousticTtlSeconds);
        }

        private static BtStatus ActContestPlant(BtContext c)
        {
            string site = AcousticIntelRules.FreshTagZone(
                TeamIntelBoard(c), AcousticIntelRules.TagPlanting, c.Time,
                AcousticIntelRules.AcousticTtlSeconds);
            if (string.IsNullOrEmpty(site))
                site = AcousticIntelRules.FreshTagZone(
                    PersonalIntelBoard(c), AcousticIntelRules.TagPlanting, c.Time,
                    AcousticIntelRules.AcousticTtlSeconds);
            if (string.IsNullOrEmpty(site))
                site = ContentDatabase.ClampZoneToMap(c.World.Request.map, c.World.PlantSiteId);
            site = ContentDatabase.ClampZoneToMap(c.World.Request.map, site);
            if (string.IsNullOrEmpty(site))
                return BtStatus.Failure;

            StepToward(c, site);
            return BtStatus.Running;
        }

        private static BtStatus ActRetake(BtContext c)
        {
            string site = c.World.PlantSiteId ?? "a_site";
            site = ContentDatabase.ClampZoneToMap(c.World.Request.map, site);
            StepToward(c, site);

            bool onSite = string.Equals(c.Self.ZoneId, site, StringComparison.OrdinalIgnoreCase);
            bool contested = c.EnemyTeam.AlivePlayers.Any(e =>
                string.Equals(e.ZoneId, site, StringComparison.OrdinalIgnoreCase));

            if (!onSite || contested || !c.World.BombPlanted || c.World.BombDefused)
            {
                if (string.Equals(c.World.DefuserId, c.Self.Definition.id, StringComparison.OrdinalIgnoreCase))
                    ClearDefuse(c.World);
                return BtStatus.Running;
            }

            if (c.World.MindGameUntilTime.TryGetValue(c.Self.Definition.id, out float mindUntil) &&
                c.Time < mindUntil)
            {
                return BtStatus.Running;
            }

            float need = c.Self.Loadout != null && c.Self.Loadout.HasDefuseKit ? 5f : 10f;
            bool alreadyDefusing = string.Equals(
                c.World.DefuserId, c.Self.Definition.id, StringComparison.OrdinalIgnoreCase) &&
                c.World.DefuseStartTime >= 0f;

            if (alreadyDefusing)
            {
                float channeled = c.Time - c.World.DefuseStartTime;
                bool hearsT = AcousticIntelRules.HearsThreatNear(
                        c.World.Graph, c.Self.ZoneId, c.EnemyTeam.AlivePlayers,
                        PersonalIntelBoard(c), c.Time) ||
                    EnemiesAdjacent(c) > 0;
                bool bait = channeled >= 0.45f && channeled < need * 0.85f &&
                    c.Random != null && c.Random.Next01() < 0.18f;
                if (hearsT || bait)
                {
                    ClearDefuse(c.World);
                    c.World.MindGameUntilTime[c.Self.Definition.id] = c.Time + 1.6f;
                    c.World.Add(c.Time, EventType.StatusUpdated, c.Self.Definition.id, null, site,
                        "fake_defuse", c.SelfTeam.Definition.id, null, "fake_defuse");
                    return BtStatus.Running;
                }

                c.World.DefuseRequiredSeconds = Math.Min(c.World.DefuseRequiredSeconds, need);
            }
            else
            {
                c.World.DefuserId = c.Self.Definition.id;
                c.World.DefuseStartTime = c.Time;
                c.World.DefuseRequiredSeconds = need;
                c.World.Add(c.Time, EventType.StatusUpdated, c.Self.Definition.id, null, site,
                    $"defusing:{need:0}", c.SelfTeam.Definition.id, null, "defusing");
            }

            return BtStatus.Running;
        }

        private static void ClearDefuse(BehaviorTreeRoundWorld world)
        {
            if (world == null)
                return;
            world.DefuserId = null;
            world.DefuseStartTime = -1f;
            world.DefuseRequiredSeconds = 0f;
        }

        private static void TickDefuse(BehaviorTreeRoundWorld world, float time)
        {
            if (world == null || !world.BombPlanted || world.BombDefused || world.BombExploded)
                return;
            if (string.IsNullOrEmpty(world.DefuserId) || world.DefuseStartTime < 0f)
                return;

            PlayerRuntime defuser = world.Request.defenders.AlivePlayers
                .FirstOrDefault(p => string.Equals(
                    p.Definition.id, world.DefuserId, StringComparison.OrdinalIgnoreCase));
            string site = ContentDatabase.ClampZoneToMap(world.Request.map, world.PlantSiteId);
            bool stillValid = defuser != null &&
                string.Equals(defuser.ZoneId, site, StringComparison.OrdinalIgnoreCase) &&
                !world.Request.attackers.AlivePlayers.Any(e =>
                    string.Equals(e.ZoneId, site, StringComparison.OrdinalIgnoreCase));
            if (!stillValid)
            {
                ClearDefuse(world);
                return;
            }

            if (time + 0.0001f < world.DefuseStartTime + world.DefuseRequiredSeconds)
                return;

            world.BombDefused = true;
            ClearDefuse(world);
            world.Add(time, EventType.BombDefused, defuser.Definition.id, null, site,
                $"{defuser.Definition.id} defused", world.Request.defenders.Definition.id);
        }

        private static bool ShouldTakeCover(BtContext c)
        {
            if (!c.Self.Alive || IsCommittedUtility(c))
                return false;
            PlayerPeekState peek = PeekStateOf(c);
            if (peek.Returning || peek.ExposedAt >= 0f)
                return false;
            int same = EnemiesInZone(c, c.Self.ZoneId);
            int adj = EnemiesAdjacent(c);
            if (!TacticalManeuverRules.ShouldSeekCover(
                    c.Self.Health, c.Time - c.Self.LastDamageTime, same, adj))
                return false;
            string cover = PickCoverZone(c);
            if (string.IsNullOrEmpty(cover))
                return false;
            return !string.Equals(cover, c.Self.ZoneId, StringComparison.OrdinalIgnoreCase);
        }

        private static BtStatus ActSeekCover(BtContext c)
        {
            string cover = PickCoverZone(c);
            if (string.IsNullOrEmpty(cover))
                return BtStatus.Failure;
            if (string.Equals(cover, c.Self.ZoneId, StringComparison.OrdinalIgnoreCase))
            {
                c.World.CoverAnchor[c.Self.Definition.id] = cover;
                return BtStatus.Success;
            }

            StepToward(c, cover);
            if (string.Equals(c.Self.ZoneId, cover, StringComparison.OrdinalIgnoreCase))
                c.World.CoverAnchor[c.Self.Definition.id] = cover;
            return BtStatus.Success;
        }

        private static string PickCoverZone(BtContext c)
        {
            MapDefinition map = c.World.Request.map;
            string best = null;
            float bestScore = float.MinValue;
            foreach (string neighbor in c.World.Graph.Neighbors(c.Self.ZoneId))
            {
                if (!IsPlayableHoldZone(map, neighbor))
                    continue;
                MapZoneDefinition zone = map.zones.FirstOrDefault(z =>
                    string.Equals(z.id, neighbor, StringComparison.OrdinalIgnoreCase));
                if (zone == null)
                    continue;
                int enemies = EnemiesInZone(c, neighbor);
                int mates = c.SelfTeam.AlivePlayers.Count(p =>
                    string.Equals(p.ZoneId, neighbor, StringComparison.OrdinalIgnoreCase));
                float score = zone.defenderReactionBonus * 10f + zone.defenderAwarenessBonus * 8f;
                score -= enemies * 12f;
                score -= mates * 1.5f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = neighbor;
                }
            }

            return best;
        }

        private static bool ShouldReload(BtContext c)
        {
            if (!c.Self.Alive || IsCommittedUtility(c))
                return false;
            PlayerPeekState peek = PeekStateOf(c);
            if (peek.Returning || peek.ExposedAt >= 0f)
                return false;
            WeaponAmmoRules.TickReload(c.Self, c.World.Catalog, c.Time);
            if (WeaponAmmoRules.IsReloading(c.Self, c.Time))
                return true;
            bool contact = EnemiesInZone(c, c.Self.ZoneId) > 0 || EnemiesAdjacent(c) > 0;
            return WeaponAmmoRules.ShouldReload(c.Self, c.World.Catalog, c.Time, contact);
        }

        private static BtStatus ActReload(BtContext c)
        {
            WeaponAmmoRules.TickReload(c.Self, c.World.Catalog, c.Time);
            if (!WeaponAmmoRules.IsReloading(c.Self, c.Time))
                WeaponAmmoRules.BeginReload(c.Self, c.World.Catalog, c.Time);
            if (!WeaponAmmoRules.IsReloading(c.Self, c.Time))
                return BtStatus.Failure;
            // In contact: stand still and eat the window. Cover only when the reload is tactical.
            bool contact = EnemiesInZone(c, c.Self.ZoneId) > 0 || EnemiesAdjacent(c) > 0;
            if (!contact)
            {
                string cover = PickCoverZone(c);
                if (!string.IsNullOrEmpty(cover) &&
                    !string.Equals(cover, c.Self.ZoneId, StringComparison.OrdinalIgnoreCase))
                    StepToward(c, cover);
            }
            return BtStatus.Success;
        }

        private static bool ShouldThrowUtility(BtContext c)
        {
            if (!c.Self.Alive || IsCommittedUtility(c))
                return false;
            if (WeaponAmmoRules.IsReloading(c.Self, c.Time))
                return false;
            if (c.Self.Loadout == null)
                return false;
            if (!c.Self.Loadout.HasSmoke && !c.Self.Loadout.HasHe && !c.Self.Loadout.HasFire &&
                !c.Self.Loadout.HasFlash)
                return false;
            if (c.World.LastUtilityThrowTime.TryGetValue(c.Self.Definition.id, out float last) &&
                c.Time - last < UtilityCombatRules.ThrowCooldownSeconds)
                return false;

            if (!TryPickUtilityThrow(c, out _, out _))
                return false;

            float utility = c.Self.Definition.attributes.utility + c.Self.FocusUtility;
            float chance = Math.Min(0.85f, 0.28f + utility * 0.0055f);
            string role = c.Self.Definition.primaryRole ?? string.Empty;
            if (string.Equals(role, "Support", StringComparison.OrdinalIgnoreCase) || utility >= 85f)
                chance = Math.Min(0.85f, chance * 1.25f);
            else if (string.Equals(role, "Entry", StringComparison.OrdinalIgnoreCase))
                chance *= 0.75f;
            if (c.Random != null && c.Random.Next01() > chance)
                return false;
            return true;
        }

        private static BtStatus ActThrowUtility(BtContext c)
        {
            if (!TryPickUtilityThrow(c, out UtilityKind kind, out string targetZone))
                return BtStatus.Failure;
            if (!UtilityCombatRules.TryConsumeNade(c.Self, kind))
                return BtStatus.Failure;

            c.World.LastUtilityThrowTime[c.Self.Definition.id] = c.Time;
            string weaponId = UtilityCombatRules.WeaponIdFor(kind);
            CombatSightRules.FaceTowardZone(c.Self, targetZone, c.World.Request.map);

            if (kind == UtilityKind.Smoke)
            {
                c.World.ActiveUtilities.Add(UtilityCombatRules.CreateSmoke(
                    targetZone, c.SelfTeam.Definition.id, c.Self.Definition.id, c.Time));
                c.World.Add(c.Time, EventType.Utility, c.Self.Definition.id, null, targetZone,
                    $"{c.Self.Definition.id} throws smoke", c.SelfTeam.Definition.id, weaponId,
                    "utility_smoke");
                return BtStatus.Success;
            }

            if (kind == UtilityKind.Fire)
            {
                foreach (ActiveUtilityEffect fire in UtilityCombatRules.CreateFireWall(
                             c.World.Request.map, c.World.Graph, targetZone,
                             c.SelfTeam.Definition.id, c.Self.Definition.id, c.Time))
                {
                    c.World.ActiveUtilities.Add(fire);
                    c.World.Add(c.Time, EventType.Utility, c.Self.Definition.id, null, fire.ZoneId,
                        $"{c.Self.Definition.id} throws fire", c.SelfTeam.Definition.id, weaponId,
                        "utility_fire");
                }
                return BtStatus.Success;
            }

            if (kind == UtilityKind.Flash)
            {
                c.World.Add(c.Time, EventType.Utility, c.Self.Definition.id, null, targetZone,
                    $"{c.Self.Definition.id} throws flash", c.SelfTeam.Definition.id, weaponId,
                    "utility_flash");
                var everyone = new List<PlayerRuntime>();
                everyone.AddRange(c.World.Request.attackers.AlivePlayers);
                everyone.AddRange(c.World.Request.defenders.AlivePlayers);
                UtilityCombatRules.ApplyFlashBlast(
                    c.World.Request.map, c.World.Graph, targetZone, c.Time, everyone,
                    (victim, until) =>
                    {
                        TeamRuntime victimTeam = ResolvePlayerTeam(c.World, victim.Definition.id);
                        c.World.Add(c.Time + 0.02f, EventType.StatusUpdated, victim.Definition.id,
                            c.Self.Definition.id, victim.ZoneId,
                            $"blinded:{until:0.###}",
                            victimTeam?.Definition.id, weaponId, "utility_blinded");
                    });
                return BtStatus.Success;
            }

            // HE: instant damage on target / adjacent — all teams (friendly fire).
            c.World.Add(c.Time, EventType.Utility, c.Self.Definition.id, null, targetZone,
                $"{c.Self.Definition.id} throws HE", c.SelfTeam.Definition.id, weaponId,
                "utility_he");
            var heTargets = new List<PlayerRuntime>();
            heTargets.AddRange(c.World.Request.attackers.AlivePlayers);
            heTargets.AddRange(c.World.Request.defenders.AlivePlayers);
            UtilityCombatRules.ApplyHeDamage(
                c.World.Request.map, c.World.Graph, targetZone, heTargets, c.Time,
                (victim, damage) =>
                {
                    UtilityCombatRules.ApplyFlatDamage(victim, damage);
                    victim.LastDamageTime = c.Time;
                    TeamRuntime victimTeam = ResolvePlayerTeam(c.World, victim.Definition.id);
                    c.World.Add(c.Time + 0.05f, EventType.StatusUpdated, victim.Definition.id,
                        c.Self.Definition.id, victim.ZoneId,
                        $"hp:{victim.Health}|armor:{victim.ArmorHp}",
                        victimTeam?.Definition.id, weaponId, "utility_he");
                    if (!victim.Alive)
                    {
                        RegisterPlayerDeath(
                            c.World, victim, c.Self, victimTeam, c.SelfTeam, c.Time,
                            weaponId,
                            $"{c.Self.Definition.id} HE {victim.Definition.id}",
                            "utility_he");
                    }
                });
            return BtStatus.Success;
        }

        private static bool TryPickUtilityThrow(
            BtContext c, out UtilityKind kind, out string targetZone)
        {
            kind = UtilityKind.Smoke;
            targetZone = null;
            MapDefinition map = c.World.Request.map;
            string site = ContentDatabase.ClampZoneToMap(map, c.World.PlantSiteId);
            string primary = ContentDatabase.ClampZoneToMap(map, c.SelfPlan?.primaryZoneId);
            string intel = BestKnownEnemyZone(c);

            // Same-zone contact: HE first.
            if (c.Self.Loadout.HasHe && EnemiesInZone(c, c.Self.ZoneId) > 0)
            {
                kind = UtilityKind.He;
                targetZone = c.Self.ZoneId;
                return true;
            }

            // Pro-style long-range lineups (fromZone → far toZone) before local neighbor util.
            if (TryPickLineupUtilityThrow(c, site, primary, intel, out kind, out targetZone))
                return true;

            // Adjacent / intel neighbor: prefer flash before HE (pre-peek pop).
            if (c.Self.Loadout.HasFlash)
            {
                foreach (string n in PlayableNeighbors(c))
                {
                    if (EnemiesInZone(c, n) <= 0)
                        continue;
                    kind = UtilityKind.Flash;
                    targetZone = n;
                    return true;
                }

                if (!string.IsNullOrEmpty(intel) && IsNeighborZone(c, intel))
                {
                    kind = UtilityKind.Flash;
                    targetZone = intel;
                    return true;
                }
            }

            if (c.Self.Loadout.HasHe)
            {
                foreach (string n in PlayableNeighbors(c))
                {
                    if (EnemiesInZone(c, n) <= 0)
                        continue;
                    kind = UtilityKind.He;
                    targetZone = n;
                    return true;
                }

                if (!string.IsNullOrEmpty(intel) &&
                    (string.Equals(intel, c.Self.ZoneId, StringComparison.OrdinalIgnoreCase) ||
                     IsNeighborZone(c, intel)))
                {
                    kind = UtilityKind.He;
                    targetZone = intel;
                    return true;
                }
            }

            // Smoke / fire onto choke toward site, primary, or intel.
            // Flash also usable on choke when pushing with intel / plant.
            string choke = PickChokeZone(c, site, primary, intel);
            if (string.IsNullOrEmpty(choke))
                return false;
            if (UtilityCombatRules.HasActiveSmoke(c.World.ActiveUtilities, choke) &&
                UtilityCombatRules.HasActiveFire(c.World.ActiveUtilities, choke) &&
                !c.Self.Loadout.HasFlash)
                return false;

            if (c.Self.Loadout.HasFlash &&
                (c.World.BombPlanted || EnemiesAdjacent(c) > 0 || !string.IsNullOrEmpty(intel)))
            {
                kind = UtilityKind.Flash;
                targetZone = choke;
                return true;
            }

            if (c.Self.Loadout.HasSmoke &&
                !UtilityCombatRules.HasActiveSmoke(c.World.ActiveUtilities, choke))
            {
                kind = UtilityKind.Smoke;
                targetZone = choke;
                return true;
            }

            if (c.Self.Loadout.HasFire &&
                !UtilityCombatRules.HasActiveFire(c.World.ActiveUtilities, choke) &&
                (c.World.BombPlanted || EnemiesAdjacent(c) > 0 || !string.IsNullOrEmpty(intel)))
            {
                kind = UtilityKind.Fire;
                targetZone = choke;
                return true;
            }

            return false;
        }

        private static bool TryPickLineupUtilityThrow(
            BtContext c, string site, string primary, string intel,
            out UtilityKind kind, out string targetZone)
        {
            kind = UtilityKind.Smoke;
            targetZone = null;
            UtilityLineupCatalog catalog = c.World.UtilityLineups ?? UtilityLineupRules.Shared;
            if (catalog == null || catalog.Entries.Count == 0)
                return false;

            MapDefinition map = c.World.Request.map;
            string mapId = map?.id;
            List<UtilityLineupCandidate> candidates = UtilityLineupRules.FindCandidates(
                catalog, mapId, c.Self.ZoneId, c.SelfTeam.Side, c.World.Graph);
            for (int i = 0; i < candidates.Count; i++)
            {
                UtilityLineupCandidate cand = candidates[i];
                if (!UtilityCombatRules.PlayerHasNade(c.Self, cand.Kind))
                    continue;

                string to = ContentDatabase.ClampZoneToMap(map, cand.Entry.toZoneId);
                if (string.IsNullOrEmpty(to) || !IsPlayableHoldZone(map, to))
                    continue;
                if (cand.Kind == UtilityKind.Smoke &&
                    UtilityCombatRules.HasActiveSmoke(c.World.ActiveUtilities, to))
                    continue;
                if (cand.Kind == UtilityKind.Fire &&
                    UtilityCombatRules.HasActiveFire(c.World.ActiveUtilities, to))
                    continue;
                if (!LineupSituationalMatch(c, to, cand.Kind, site, primary, intel))
                    continue;

                kind = cand.Kind;
                targetZone = to;
                return true;
            }

            return false;
        }

        private static bool LineupSituationalMatch(
            BtContext c, string toZone, UtilityKind kind,
            string site, string primary, string intel)
        {
            if (EnemiesInZone(c, toZone) > 0)
                return true;
            foreach (string n in c.World.Graph.Neighbors(toZone))
            {
                if (EnemiesInZone(c, n) > 0)
                    return true;
            }

            if (!string.IsNullOrEmpty(intel) &&
                (string.Equals(intel, toZone, StringComparison.OrdinalIgnoreCase) ||
                 IsNeighborOf(c.World.Graph, intel, toZone)))
                return true;

            if (!string.IsNullOrEmpty(primary) &&
                (string.Equals(primary, toZone, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(
                     MapZones.ResolvePlantSite(primary),
                     MapZones.ResolvePlantSite(toZone),
                     StringComparison.OrdinalIgnoreCase)))
                return true;

            if (!string.IsNullOrEmpty(site) &&
                (string.Equals(site, toZone, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(
                     site, MapZones.ResolvePlantSite(toZone), StringComparison.OrdinalIgnoreCase)))
                return true;

            if (c.World.BombPlanted)
                return true;

            // Smoke is useful as a default exec block even without hard intel.
            if (kind == UtilityKind.Smoke)
                return true;

            return false;
        }

        private static bool IsNeighborOf(MapZoneGraph graph, string a, string b)
        {
            if (graph == null || string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
                return false;
            foreach (string n in graph.Neighbors(a))
            {
                if (string.Equals(n, b, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static string PickChokeZone(
            BtContext c, string site, string primary, string intel)
        {
            var candidates = new List<string>();
            void Consider(string z)
            {
                z = ContentDatabase.ClampZoneToMap(c.World.Request.map, z);
                if (string.IsNullOrEmpty(z) || !IsPlayableHoldZone(c.World.Request.map, z))
                    return;
                if (string.Equals(z, c.Self.ZoneId, StringComparison.OrdinalIgnoreCase))
                    return;
                if (!candidates.Exists(x => string.Equals(x, z, StringComparison.OrdinalIgnoreCase)))
                    candidates.Add(z);
            }

            foreach (string n in PlayableNeighbors(c))
                Consider(n);
            Consider(primary);
            Consider(site);
            Consider(intel);

            // Prefer neighbor on path to objective.
            string goal = !string.IsNullOrEmpty(intel) ? intel
                : c.World.BombPlanted && c.SelfTeam.Side == Side.CounterTerrorist ? site
                : primary;
            if (!string.IsNullOrEmpty(goal))
            {
                IReadOnlyList<string> path = c.World.Graph.FindPath(c.Self.ZoneId, goal);
                if (path != null && path.Count >= 2)
                    Consider(path[1]);
            }

            if (candidates.Count == 0)
                return null;

            // Score: closer to enemies / objective, not already smoked.
            string best = null;
            float bestScore = float.MinValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                string z = candidates[i];
                float score = 0f;
                if (IsNeighborZone(c, z))
                    score += 4f;
                if (!string.IsNullOrEmpty(intel) &&
                    string.Equals(z, intel, StringComparison.OrdinalIgnoreCase))
                    score += 6f;
                if (!string.IsNullOrEmpty(site) &&
                    string.Equals(z, site, StringComparison.OrdinalIgnoreCase))
                    score += 3f;
                if (UtilityCombatRules.HasActiveSmoke(c.World.ActiveUtilities, z))
                    score -= 8f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = z;
                }
            }

            return best;
        }

        private static bool IsNeighborZone(BtContext c, string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId))
                return false;
            foreach (string n in c.World.Graph.Neighbors(c.Self.ZoneId))
            {
                if (string.Equals(n, zoneId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static string BestKnownEnemyZone(BtContext c)
        {
            float awareness = c.Self.Definition.attributes.awareness + c.Self.FocusAwareness;
            float ttl = IntelRules.TtlSeconds(awareness);
            string personal = IntelRules.BestFocusZone(
                PersonalIntelBoard(c), c.Self.ZoneId, c.World.Graph, c.Time, ttl);
            if (!string.IsNullOrEmpty(personal))
                return personal;
            return IntelRules.BestFocusZone(
                TeamIntelBoard(c), c.Self.ZoneId, c.World.Graph, c.Time, ttl);
        }

        private static bool ShouldPeek(BtContext c)
        {
            if (!c.Self.Alive)
                return false;
            if (UtilityCombatRules.IsBlinded(c.Self, c.Time))
                return false;
            if (WeaponAmmoRules.IsReloading(c.Self, c.Time))
                return false;
            PlayerPeekState peek = PeekStateOf(c);
            bool inProgress = peek.Returning || peek.ExposedAt >= 0f;
            bool punish = AdjacentHasPunishWindow(c);
            bool slowed = c.Self.VelocityModifier > 0f && c.Self.VelocityModifier < 0.50f;
            if (!TacticalManeuverRules.ShouldStartPeek(
                    c.Self.Alive,
                    c.Self.Health,
                    EnemiesInZone(c, c.Self.ZoneId),
                    EnemiesAdjacent(c),
                    IsCommittedUtility(c),
                    inProgress,
                    c.Time,
                    peek.NextPeekAt,
                    AtHoldPosition(c),
                    punish,
                    slowed))
                return false;
            return inProgress || PickPeekZone(c) != null;
        }

        private static BtStatus ActPeek(BtContext c)
        {
            PlayerPeekState peek = PeekStateOf(c);
            MapDefinition map = c.World.Request.map;
            float hop = peek.Wide ? WidePeekMoveIntervalSeconds : PeekMoveIntervalSeconds;
            float hold = peek.Wide ? WidePeekHoldSeconds : PeekHoldSeconds;

            if (peek.Returning)
            {
                string cover = string.IsNullOrEmpty(peek.CoverZoneId) ? c.Self.ZoneId : peek.CoverZoneId;
                StepToward(c, cover, hop, "peek_return");
                if (string.Equals(c.Self.ZoneId, cover, StringComparison.OrdinalIgnoreCase))
                {
                    peek.Returning = false;
                    peek.ExposedAt = -1f;
                    float cooldown = TacticalManeuverRules.PeekCooldown(
                        c.Self.Definition.attributes.opening,
                        c.Random.Next01(),
                        c.Self.Definition.primaryRole,
                        peek.SawContact);
                    peek.NextPeekAt = c.Time + cooldown;
                    peek.Wide = false;
                    peek.SawContact = false;
                    c.World.CoverAnchor[c.Self.Definition.id] = cover;
                    CombatSightRules.FaceTowardZone(c.Self, peek.PeekZoneId, map);
                }

                return BtStatus.Success;
            }

            if (peek.ExposedAt >= 0f)
            {
                if (EnemiesInZone(c, peek.PeekZoneId) > 0 || EnemiesInZone(c, c.Self.ZoneId) > 0)
                    peek.SawContact = true;
                if (c.Time + 0.0001f >= peek.ExposedAt + hold)
                {
                    peek.Returning = true;
                    StepToward(c, peek.CoverZoneId, hop, "peek_return");
                }
                else
                {
                    CombatSightRules.FaceTowardZone(c.Self, peek.PeekZoneId, map);
                }

                return BtStatus.Success;
            }

            string peekZone = PickPeekZone(c);
            if (string.IsNullOrEmpty(peekZone))
                return BtStatus.Failure;

            string coverZone = ResolveCoverAnchor(c);
            if (!string.Equals(c.Self.ZoneId, coverZone, StringComparison.OrdinalIgnoreCase))
            {
                StepToward(c, coverZone, hop, "peek");
                return BtStatus.Success;
            }

            peek.CoverZoneId = coverZone;
            peek.PeekZoneId = peekZone;
            peek.Returning = false;
            peek.Wide = AdjacentHasPunishWindow(c) || EnemiesInZone(c, peekZone) >= 2;
            peek.SawContact = false;
            hop = peek.Wide ? WidePeekMoveIntervalSeconds : PeekMoveIntervalSeconds;
            StepToward(c, peekZone, hop, peek.Wide ? "wide_peek" : "peek");
            if (string.Equals(c.Self.ZoneId, peekZone, StringComparison.OrdinalIgnoreCase))
                peek.ExposedAt = c.Time;
            return BtStatus.Success;
        }

        private static string ResolveCoverAnchor(BtContext c)
        {
            if (c.World.CoverAnchor.TryGetValue(c.Self.Definition.id, out string stored) &&
                !string.IsNullOrEmpty(stored) &&
                string.Equals(stored, c.Self.ZoneId, StringComparison.OrdinalIgnoreCase))
                return stored;
            string picked = PickCoverZone(c);
            if (!string.IsNullOrEmpty(picked) &&
                string.Equals(picked, c.Self.ZoneId, StringComparison.OrdinalIgnoreCase))
                return picked;
            return c.Self.ZoneId;
        }

        private static string PickPeekZone(BtContext c)
        {
            List<string> neighbors = PlayableNeighbors(c).ToList();
            if (neighbors.Count == 0)
                return null;

            string best = null;
            float bestScore = float.MinValue;
            foreach (string zone in neighbors)
            {
                float score = PeekZoneScore(c, zone);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = zone;
                }
            }

            if (bestScore > 0f)
                return best;

            string intel = IntelFocusZone(c);
            if (!string.IsNullOrEmpty(intel) &&
                neighbors.Contains(intel, StringComparer.OrdinalIgnoreCase))
                return intel;
            if (!string.IsNullOrEmpty(intel))
            {
                IReadOnlyList<string> intelPath = c.World.Graph.FindPath(c.Self.ZoneId, intel);
                if (intelPath != null && intelPath.Count >= 2 &&
                    neighbors.Contains(intelPath[1], StringComparer.OrdinalIgnoreCase))
                    return intelPath[1];
            }

            string primary = ContentDatabase.ClampZoneToMap(
                c.World.Request.map, c.SelfPlan.primaryZoneId);
            string assigned = PersonalTarget(c, primary);
            IReadOnlyList<string> path = c.World.Graph.FindPath(c.Self.ZoneId, assigned);
            if (path != null && path.Count >= 2 &&
                neighbors.Contains(path[1], StringComparer.OrdinalIgnoreCase))
                return path[1];

            int idx = Math.Abs(c.Self.Definition.id.GetHashCode());
            return neighbors[idx % neighbors.Count];
        }

        private static float PeekZoneScore(BtContext c, string zone)
        {
            float score = 0f;
            foreach (PlayerRuntime enemy in c.EnemyTeam.AlivePlayers)
            {
                if (enemy == null || !enemy.Alive)
                    continue;
                if (!string.Equals(enemy.ZoneId, zone, StringComparison.OrdinalIgnoreCase))
                    continue;
                score += 2f;
                if (WeaponAmmoRules.IsReloading(enemy, c.Time))
                    score += 6f;
                else if (enemy.VelocityModifier > 0f && enemy.VelocityModifier < 0.55f)
                    score += 4f;
            }

            return score;
        }

        private static bool AdjacentHasPunishWindow(BtContext c)
        {
            foreach (string zone in c.World.Graph.Neighbors(c.Self.ZoneId))
            {
                foreach (PlayerRuntime enemy in c.EnemyTeam.AlivePlayers)
                {
                    if (enemy == null || !enemy.Alive)
                        continue;
                    if (!string.Equals(enemy.ZoneId, zone, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (WeaponAmmoRules.IsReloading(enemy, c.Time))
                        return true;
                    if (enemy.VelocityModifier > 0f && enemy.VelocityModifier < 0.55f)
                        return true;
                }
            }

            return false;
        }

        private static bool ShouldSearch(BtContext c)
        {
            if (!c.Self.Alive || IsCommittedUtility(c))
                return false;
            if (UtilityCombatRules.IsBlinded(c.Self, c.Time))
                return false;
            PlayerPeekState peek = PeekStateOf(c);
            if (peek.Returning || peek.ExposedAt >= 0f)
                return false;
            if (EnemiesInZone(c, c.Self.ZoneId) > 0)
                return false;
            if (c.Time < 0.4f)
                return false;
            // Keep walking until the assigned post. Searching from a neighbor
            // or a path node leaves the player stuck off the setup.
            if (!AtHoldPosition(c))
                return false;
            float interval = TacticalManeuverRules.SearchInterval(c.Self.Definition.attributes.awareness);
            if (c.World.LastSearchTime.TryGetValue(c.Self.Definition.id, out float last) &&
                c.Time - last < interval)
                return false;
            return PlayableNeighbors(c).Any();
        }

        private static BtStatus ActSearch(BtContext c)
        {
            List<string> neighbors = PlayableNeighbors(c).ToList();
            if (neighbors.Count == 0)
                return BtStatus.Failure;

            string id = c.Self.Definition.id;
            string look = null;
            string focus = IntelFocusZone(c);
            if (!string.IsNullOrEmpty(focus))
            {
                if (neighbors.Contains(focus, StringComparer.OrdinalIgnoreCase))
                    look = focus;
                else
                {
                    IReadOnlyList<string> path = c.World.Graph.FindPath(c.Self.ZoneId, focus);
                    if (path != null && path.Count >= 2 &&
                        neighbors.Contains(path[1], StringComparer.OrdinalIgnoreCase))
                        look = path[1];
                }
            }

            if (string.IsNullOrEmpty(look))
            {
                if (!c.World.SearchSweepIndex.TryGetValue(id, out int idx))
                    idx = 0;
                look = neighbors[idx % neighbors.Count];
                c.World.SearchSweepIndex[id] = idx + 1;
            }

            c.World.LastSearchTime[id] = c.Time;
            CombatSightRules.FaceTowardZone(c.Self, look, c.World.Request.map);
            c.World.Add(c.Time, EventType.StatusUpdated, id, null, c.Self.ZoneId,
                $"{id} checks {look}", c.SelfTeam.Definition.id, null, "search");
            return BtStatus.Success;
        }

        private void ScanAndShareIntel(BtContext c)
        {
            IntelBoard personal = PersonalIntelBoard(c);
            float awareness = c.Self.Definition.attributes.awareness + c.Self.FocusAwareness;
            float ttl = IntelRules.TtlSeconds(awareness);
            personal.Expire(c.Time, ttl);
            IntelBoard team = TeamIntelBoard(c);
            team.Expire(c.Time, ttl);

            List<IntelNote> seen = IntelRules.ScanVisible(
                c.World.Request.map, c.Self, c.EnemyTeam.AlivePlayers.ToList(), _catalog, c.Time,
                c.World.Request?.Los, c.World.Graph, c.World.ActiveUtilities);
            for (int i = 0; i < seen.Count; i++)
            {
                IntelNote note = seen[i];
                personal.Upsert(note);
                TryCalloutIntel(c, team, note);
            }

            string plantingZone = PlantChannelRules.IsChanneling(c.World)
                ? ContentDatabase.ClampZoneToMap(c.World.Request.map, c.World.PlantSiteId)
                : null;
            string defuseZone = !string.IsNullOrEmpty(c.World.DefuserId)
                ? ContentDatabase.ClampZoneToMap(c.World.Request.map, c.World.PlantSiteId)
                : null;
            AcousticIntelRules.ScanAudible(
                c.World.Graph, c.Self, c.EnemyTeam.AlivePlayers,
                personal, team, c.Time, c.Random,
                plantingZone, c.World.PlanterId, c.World.DefuserId, defuseZone);

            if (PlantChannelRules.IsChanneling(c.World) &&
                !string.IsNullOrEmpty(plantingZone) &&
                seen.Exists(n => string.Equals(n.ZoneId, plantingZone, StringComparison.OrdinalIgnoreCase)))
            {
                AcousticIntelRules.UpsertTagged(
                    personal, plantingZone, AcousticIntelRules.TagPlanting, c.Time, c.Self.Definition.id);
                AcousticIntelRules.UpsertTagged(
                    team, plantingZone, AcousticIntelRules.TagPlanting, c.Time, c.Self.Definition.id);
                c.World.Add(c.Time, EventType.StatusUpdated, c.Self.Definition.id, null, plantingZone,
                    "heard_plant", c.SelfTeam.Definition.id, null, "heard_plant");
            }
        }

        private void TryCalloutIntel(BtContext c, IntelBoard team, IntelNote note)
        {
            team.TryGet(note.ZoneId, out IntelNote teamPrev);
            if (!IntelRules.IsNewOrChanged(teamPrev, note))
                return;

            float communication = c.Self.Definition.attributes.communication + c.Self.FocusCommunication;
            float cooldown = IntelRules.CalloutCooldown(communication);
            if (c.World.LastCalloutTime.TryGetValue(c.Self.Definition.id, out float last) &&
                c.Time - last < cooldown)
                return;
            if (c.Random == null || c.Random.Next01() > IntelRules.CalloutChance(communication))
                return;

            c.World.LastCalloutTime[c.Self.Definition.id] = c.Time;
            team.Upsert(note);
            c.World.Add(c.Time, EventType.StatusUpdated, c.Self.Definition.id, null, note.ZoneId,
                IntelRules.FormatCallout(note, _catalog), c.SelfTeam.Definition.id, null, "callout");
        }

        private static IntelBoard PersonalIntelBoard(BtContext c)
        {
            string id = c.Self.Definition.id;
            if (!c.World.PersonalIntel.TryGetValue(id, out IntelBoard board) || board == null)
            {
                board = new IntelBoard();
                c.World.PersonalIntel[id] = board;
            }

            return board;
        }

        private static IntelBoard TeamIntelBoard(BtContext c) =>
            c.SelfTeam.Side == Side.Terrorist ? c.World.AttackerTeamIntel : c.World.DefenderTeamIntel;

        private static string IntelFocusZone(BtContext c)
        {
            float awareness = c.Self.Definition.attributes.awareness + c.Self.FocusAwareness;
            float ttl = IntelRules.TtlSeconds(awareness);
            string fromPersonal = IntelRules.BestFocusZone(
                PersonalIntelBoard(c), c.Self.ZoneId, c.World.Graph, c.Time, ttl);
            if (!string.IsNullOrEmpty(fromPersonal))
                return fromPersonal;
            return IntelRules.BestFocusZone(
                TeamIntelBoard(c), c.Self.ZoneId, c.World.Graph, c.Time, ttl);
        }

        private static PlayerPeekState PeekStateOf(BtContext c)
        {
            string id = c.Self.Definition.id;
            if (!c.World.Peeks.TryGetValue(id, out PlayerPeekState state) || state == null)
            {
                state = new PlayerPeekState();
                c.World.Peeks[id] = state;
            }

            return state;
        }

        private static bool IsCommittedUtility(BtContext c)
        {
            if (c.SelfTeam.Side == Side.CounterTerrorist &&
                !string.IsNullOrEmpty(c.World.DefuserId) &&
                string.Equals(c.World.DefuserId, c.Self.Definition.id, StringComparison.OrdinalIgnoreCase))
                return true;
            if (c.SelfTeam.Side == Side.Terrorist &&
                !c.World.BombPlanted &&
                string.Equals(c.World.BombCarrierId, c.Self.Definition.id, StringComparison.OrdinalIgnoreCase))
            {
                string site = ContentDatabase.ClampZoneToMap(c.World.Request.map, c.World.PlantSiteId);
                if (string.Equals(c.Self.ZoneId, site, StringComparison.OrdinalIgnoreCase) && c.Time >= 16f)
                    return true;
            }

            return false;
        }

        private static int EnemiesAdjacent(BtContext c)
        {
            int n = 0;
            foreach (string zone in c.World.Graph.Neighbors(c.Self.ZoneId))
                n += EnemiesInZone(c, zone);
            return n;
        }

        private static IEnumerable<string> PlayableNeighbors(BtContext c)
        {
            return c.World.Graph.Neighbors(c.Self.ZoneId)
                .Where(id => IsPlayableHoldZone(c.World.Request.map, id))
                .OrderBy(id => id, StringComparer.OrdinalIgnoreCase);
        }

        private static bool AtHoldPosition(BtContext c)
        {
            string primary = ContentDatabase.ClampZoneToMap(
                c.World.Request.map, c.SelfPlan.primaryZoneId);
            string assigned = PersonalTarget(c, primary);
            if (c.World.RotateTargets.TryGetValue(c.Self.Definition.id, out string rotated) &&
                !string.IsNullOrEmpty(rotated))
                assigned = ContentDatabase.ClampZoneToMap(c.World.Request.map, rotated);
            if (c.World.BombPlanted)
                assigned = HoldNearSite(c, c.World.PlantSiteId, assigned);
            if (string.IsNullOrEmpty(assigned))
                return false;
            // Only the assigned zone counts. A neighbor, including the spawn
            // link or the last path node, is not the post yet.
            return string.Equals(c.Self.ZoneId, assigned, StringComparison.OrdinalIgnoreCase);
        }

        private static BtStatus ActExecutePlan(BtContext c)
        {
            string primary = ContentDatabase.ClampZoneToMap(
                c.World.Request.map, c.SelfPlan.primaryZoneId);
            string assigned = PersonalTarget(c, primary);
            if (c.World.RotateTargets.TryGetValue(c.Self.Definition.id, out string rotated) &&
                !string.IsNullOrEmpty(rotated))
                assigned = ContentDatabase.ClampZoneToMap(c.World.Request.map, rotated);

            bool attacking = c.SelfTeam.Side == Side.Terrorist;

            if (attacking)
            {
                if (!c.World.BombPlanted)
                {
                    string site = MapZones.ResolvePlantSite(primary);
                    site = ContentDatabase.ClampZoneToMap(c.World.Request.map, site);
                    // Carrier pushes site; others take personal approach / lurk slots.
                    string target = string.Equals(c.Self.Definition.id, c.World.BombCarrierId,
                        StringComparison.OrdinalIgnoreCase)
                        ? site
                        : assigned;
                    // Non-carriers keep personal targets even from spawn (no whole-team site rush).
                    if (string.IsNullOrEmpty(target))
                        target = site;
                    StepToward(c, target);

                    if (string.Equals(c.Self.Definition.id, c.World.BombCarrierId,
                            StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(c.Self.ZoneId, site, StringComparison.OrdinalIgnoreCase) &&
                        c.Time >= 18f)
                    {
                        bool threatened =
                            AcousticIntelRules.HearsThreatNear(
                                c.World.Graph, c.Self.ZoneId, c.EnemyTeam.AlivePlayers,
                                PersonalIntelBoard(c), c.Time) ||
                            EnemiesInZone(c, site) > 0 ||
                            EnemiesAdjacent(c) > 0;
                        if (threatened)
                        {
                            if (PlantChannelRules.IsChanneling(c.World))
                            {
                                PlantChannelRules.Clear(c.World, c.Time, emitCancel: true);
                                c.World.MindGameUntilTime[c.Self.Definition.id] = c.Time + 2.4f;
                            }
                        }
                        else if (c.World.MindGameUntilTime.TryGetValue(c.Self.Definition.id, out float until) &&
                                 c.Time < until)
                        {
                            // Hold / peek after cancel — do not restart plant yet.
                        }
                        else
                        {
                            PlantChannelRules.TryBegin(c.World, c.Self, site, c.Time);
                        }
                    }
                    else if (PlantChannelRules.IsChanneling(c.World) &&
                             string.Equals(c.World.PlanterId, c.Self.Definition.id,
                                 StringComparison.OrdinalIgnoreCase))
                    {
                        PlantChannelRules.Clear(c.World, c.Time, emitCancel: true);
                        c.World.MindGameUntilTime[c.Self.Definition.id] = c.Time + 1.2f;
                    }
                }
                else
                {
                    // Heard CT defusing → collapse to site; else hold near plant with spread.
                    bool heardDefuse = AcousticIntelRules.BoardHasFreshTag(
                        TeamIntelBoard(c), AcousticIntelRules.TagDefusing, c.Time,
                        AcousticIntelRules.AcousticTtlSeconds);
                    string plantSite = ContentDatabase.ClampZoneToMap(
                        c.World.Request.map, c.World.PlantSiteId);
                    bool enemiesNearPlant = EnemiesInZone(c, plantSite) > 0 ||
                        c.World.Graph.Neighbors(plantSite).Any(n => EnemiesInZone(c, n) > 0);
                    float remaining = PostPlantHoldRules.BombRemaining(
                        c.World.PlantTime, c.World.BombTimer, c.Time);
                    string postPlant;
                    if (heardDefuse)
                        postPlant = plantSite;
                    else if (PostPlantHoldRules.ShouldEvacuate(remaining, heardDefuse, enemiesNearPlant))
                        postPlant = PickEgressZone(c, plantSite);
                    else
                        postPlant = HoldNearSite(c, plantSite, assigned);
                    StepToward(c, postPlant);
                }
            }
            else
            {
                string hold = c.World.BombPlanted
                    ? HoldNearSite(c, c.World.PlantSiteId, assigned)
                    : assigned;
                StepToward(c, hold);
            }

            if (AtHoldPosition(c) && EnemiesInZone(c, c.Self.ZoneId) == 0)
                c.World.CoverAnchor[c.Self.Definition.id] = c.Self.ZoneId;
            return BtStatus.Running;
        }

        private static string PersonalTarget(BtContext c, string fallback)
        {
            if (c.World.PersonalTargets.TryGetValue(c.Self.Definition.id, out string zone) &&
                !string.IsNullOrEmpty(zone))
                return ContentDatabase.ClampZoneToMap(c.World.Request.map, zone);
            return fallback;
        }

        private static string HoldNearSite(BtContext c, string site, string assigned)
        {
            site = ContentDatabase.ClampZoneToMap(c.World.Request.map, site);
            assigned = ContentDatabase.ClampZoneToMap(c.World.Request.map, assigned);
            if (string.IsNullOrEmpty(site))
                return assigned;

            if (!string.IsNullOrEmpty(assigned) &&
                !string.Equals(assigned, site, StringComparison.OrdinalIgnoreCase))
            {
                IReadOnlyList<string> path = c.World.Graph.FindPath(assigned, site);
                if (path != null && path.Count > 0 && path.Count <= 5)
                    return assigned;
            }

            var neighbors = c.World.Graph.Neighbors(site)
                .Where(id => IsPlayableHoldZone(c.World.Request.map, id))
                .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (neighbors.Count == 0)
                return site;

            int index = Math.Max(0, c.SelfTeam.Players.FindIndex(p =>
                string.Equals(p.Definition.id, c.Self.Definition.id, StringComparison.OrdinalIgnoreCase)));
            return neighbors[index % neighbors.Count];
        }

        private static bool IsPlayableHoldZone(MapDefinition map, string zoneId)
        {
            MapZoneDefinition zone = map?.zones?.FirstOrDefault(z =>
                string.Equals(z.id, zoneId, StringComparison.OrdinalIgnoreCase));
            return zone != null && !zone.navigationOnly;
        }

        private static bool StepToward(BtContext c, string destination) =>
            StepToward(c, destination, MoveIntervalSeconds, null);

        private static bool StepToward(BtContext c, string destination, float interval, string tag)
        {
            if (string.IsNullOrEmpty(destination))
                return true;
            destination = ContentDatabase.ClampZoneToMap(c.World.Request.map, destination);
            if (string.Equals(c.Self.ZoneId, destination, StringComparison.OrdinalIgnoreCase))
                return true;
            if (c.Time + 0.0001f < c.Self.NextMoveTime)
                return false;

            IReadOnlyList<string> path = FindPathForMove(c, c.Self.ZoneId, destination);
            if (path == null || path.Count < 2)
                return false;

            string next = path[1];
            // Soft refuse: normally do not step into smoke/fire unless push is justified.
            if (!ShouldPushUtilityHazard(c) &&
                (UtilityCombatRules.HasActiveSmoke(c.World.ActiveUtilities, next) ||
                 UtilityCombatRules.HasActiveFire(c.World.ActiveUtilities, next)))
            {
                IReadOnlyList<string> detour = c.World.Graph.FindPath(
                    c.Self.ZoneId, destination,
                    z => UtilityCombatRules.ZonePathCost(c.World.ActiveUtilities, z) +
                         (string.Equals(z, next, StringComparison.OrdinalIgnoreCase) ? 40f : 0f));
                if (detour != null && detour.Count >= 2 &&
                    !string.Equals(detour[1], next, StringComparison.OrdinalIgnoreCase) &&
                    !UtilityCombatRules.HasActiveFire(c.World.ActiveUtilities, detour[1]))
                {
                    next = detour[1];
                }
                else
                {
                    // Wait rather than walk into dense smoke/fire.
                    c.Self.NextMoveTime = c.Time + Math.Max(0.12f, interval * 0.5f);
                    return false;
                }
            }

            CombatSightRules.FaceTowardZone(c.Self, next, c.World.Request.map);
            c.Self.ZoneId = next;
            c.Self.NextMoveTime = c.Time + Math.Max(0.08f, CombatTaggingRules.ScaledMoveInterval(interval, c.Self));
            if (string.IsNullOrEmpty(tag))
            {
                c.World.Add(c.Time, EventType.Move, c.Self.Definition.id, null, next,
                    $"{c.Self.Definition.id} moves to {next}", c.SelfTeam.Definition.id);
            }
            else
            {
                c.World.Add(c.Time, EventType.Move, c.Self.Definition.id, null, next,
                    $"{c.Self.Definition.id} moves to {next}", c.SelfTeam.Definition.id, null, tag);
            }

            return string.Equals(next, destination, StringComparison.OrdinalIgnoreCase);
        }

        private static IReadOnlyList<string> FindPathForMove(
            BtContext c, string from, string to)
        {
            if (ShouldPushUtilityHazard(c))
                return c.World.Graph.FindPath(from, to);
            return c.World.Graph.FindPath(
                from, to, z => UtilityCombatRules.ZonePathCost(c.World.ActiveUtilities, z));
        }

        private static bool ShouldPushUtilityHazard(BtContext c)
        {
            bool isRetake = c.World.BombPlanted && c.SelfTeam.Side == Side.CounterTerrorist;
            bool isHunt = ShouldHunt(c);
            bool isCarrier = c.SelfTeam.Side == Side.Terrorist &&
                !string.IsNullOrEmpty(c.World.BombCarrierId) &&
                string.Equals(c.World.BombCarrierId, c.Self.Definition.id, StringComparison.OrdinalIgnoreCase);
            bool contest = PlantChannelRules.IsChanneling(c.World) &&
                c.SelfTeam.Side == Side.CounterTerrorist;
            return UtilityCombatRules.ShouldPushThrough(
                c.World.BombPlanted, isRetake, isHunt, isCarrier, contest,
                c.SelfTeam.AliveCount, c.EnemyTeam.AliveCount);
        }

        private static void AssignPersonalTargets(BehaviorTreeRoundWorld world)
        {
            world.PersonalTargets.Clear();
            AssignTeamTargets(world, world.Request.attackers, world.Request.attackerPlan, true);
            AssignTeamTargets(world, world.Request.defenders, world.Request.defenderPlan, false);
        }

        private static void AssignTeamTargets(
            BehaviorTreeRoundWorld world, TeamRuntime team, TacticalPlan plan, bool attacking)
        {
            MapDefinition map = world.Request.map;
            string mapId = map?.id;
            string primary = ContentDatabase.ClampZoneToMap(map, plan.primaryZoneId);
            string[] pool = attacking
                ? MapDeploymentRules.BuildAttackSpreadPool(mapId, primary)
                : MapDeploymentRules.BuildDefendSpreadPool(mapId, primary);
            if (pool == null || pool.Length == 0)
                pool = new[] { primary };

            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < team.Players.Count; i++)
            {
                PlayerRuntime player = team.Players[i];
                string role = player.Definition.primaryRole;
                string zone = MapDeploymentRules.AssignDeployZone(
                    mapId, role, primary, pool, attacking, i);
                zone = ContentDatabase.ClampZoneToMap(map, zone);

                if (used.Contains(zone))
                {
                    string alt = pool
                        .Select(z => ContentDatabase.ClampZoneToMap(map, z))
                        .FirstOrDefault(z => !string.IsNullOrEmpty(z) && !used.Contains(z) &&
                                             IsPlayableHoldZone(map, z));
                    if (!string.IsNullOrEmpty(alt))
                        zone = alt;
                }

                if (!IsPlayableHoldZone(map, zone))
                    zone = ContentDatabase.ClampZoneToMap(map, primary);

                // Attackers must push — never park personal targets on spawn.
                if (IsSpawnZone(zone))
                {
                    string[] clampedPool = pool
                        .Select(z => ContentDatabase.ClampZoneToMap(map, z))
                        .Where(z => !string.IsNullOrEmpty(z) && IsPlayableHoldZone(map, z))
                        .ToArray();
                    zone = SpawnHoldRules.RemapAwayFromSpawn(
                        zone,
                        clampedPool,
                        ContentDatabase.ClampZoneToMap(map, primary),
                        ContentDatabase.ClampZoneToMap(map, MapZones.ResolvePlantSite(primary)));
                }

                used.Add(zone);
                world.PersonalTargets[player.Definition.id] = zone;
            }
        }

        private static string PreferSpawnAdjacentHide(BtContext c, string spawn)
        {
            spawn = ContentDatabase.ClampZoneToMap(c.World.Request.map, spawn);
            var neighbors = c.World.Graph.Neighbors(spawn)
                .Where(id => IsPlayableHoldZone(c.World.Request.map, id))
                .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (neighbors.Count == 0)
                return spawn;
            int index = Math.Max(0, c.SelfTeam.Players.FindIndex(p =>
                string.Equals(p.Definition.id, c.Self.Definition.id, StringComparison.OrdinalIgnoreCase)));
            return neighbors[index % neighbors.Count];
        }

        private static string PickEgressZone(BtContext c, string plantSite)
        {
            plantSite = ContentDatabase.ClampZoneToMap(c.World.Request.map, plantSite);
            string spawn = c.SelfTeam.Side == Side.Terrorist ? "t_spawn" : "ct_spawn";
            spawn = ContentDatabase.ClampZoneToMap(c.World.Request.map, spawn);
            string best = PreferSpawnAdjacentHide(c, spawn);
            float bestDist = -1f;
            foreach (string zone in c.World.Graph.Neighbors(spawn)
                         .Concat(c.World.Graph.Neighbors(plantSite))
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!IsPlayableHoldZone(c.World.Request.map, zone))
                    continue;
                if (string.Equals(zone, plantSite, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (EnemiesInZone(c, zone) > 0)
                    continue;
                float dist = GraphDistance(c, zone, plantSite);
                if (dist > bestDist)
                {
                    bestDist = dist;
                    best = zone;
                }
            }
            return best;
        }

        private static bool IsSpawnZone(string zoneId) =>
            SpawnHoldRules.IsSpawnZone(zoneId);

        private void ResolveZoneFights(BehaviorTreeRoundWorld world, float time, IRandomSource random)
        {
            List<PlayerRuntime> atk = world.Request.attackers.AlivePlayers.ToList();
            List<PlayerRuntime> def = world.Request.defenders.AlivePlayers.ToList();
            if (atk.Count == 0 || def.Count == 0)
                return;

            bool contact = false;
            foreach (PlayerRuntime shooter in atk)
            {
                if (!shooter.Alive)
                    continue;
                PlayerRuntime target = PickSightTarget(
                    shooter, def, world.Request.map, random, world.Request?.Los, world, time);
                if (target == null)
                    continue;
                if (!contact && SameZone(shooter, target))
                {
                    world.Add(time, EventType.Contact, shooter.Definition.id, target.Definition.id, shooter.ZoneId,
                        $"Contact in {shooter.ZoneId}", null);
                    contact = true;
                }

                TryShoot(world, shooter, target, world.Request.attackers, world.Request.defenders, time, random);
                def.RemoveAll(p => !p.Alive);
            }

            atk = world.Request.attackers.AlivePlayers.ToList();
            def = world.Request.defenders.AlivePlayers.ToList();
            foreach (PlayerRuntime shooter in def)
            {
                if (!shooter.Alive)
                    continue;
                PlayerRuntime target = PickSightTarget(
                    shooter, atk, world.Request.map, random, world.Request?.Los, world, time);
                if (target == null)
                    continue;
                if (!contact && SameZone(shooter, target))
                {
                    world.Add(time, EventType.Contact, shooter.Definition.id, target.Definition.id, shooter.ZoneId,
                        $"Contact in {shooter.ZoneId}", null);
                    contact = true;
                }

                TryShoot(world, shooter, target, world.Request.defenders, world.Request.attackers, time, random);
                atk.RemoveAll(p => !p.Alive);
            }
        }

        private PlayerRuntime PickSightTarget(
            PlayerRuntime shooter,
            IReadOnlyList<PlayerRuntime> enemies,
            MapDefinition map,
            IRandomSource random,
            ICombatLosQuery los,
            BehaviorTreeRoundWorld world,
            float time)
        {
            WeaponDefinition weapon = _catalog.GetOrKnife(WeaponAmmoRules.ActiveWeaponId(shooter));
            CombatSightRules.EnsureRangeStats(weapon);
            float maxRange = weapon.maxEngageRange > 0f ? weapon.maxEngageRange : 0.18f;
            MapZoneGraph graph = world?.Graph;
            IReadOnlyList<ActiveUtilityEffect> utilities = world?.ActiveUtilities;

            if (UtilityCombatRules.IsBlinded(shooter, time))
                return PickBlindSprayTarget(shooter, enemies, map, los, graph, utilities, maxRange);

            List<PlayerRuntime> same = new List<PlayerRuntime>();
            for (int i = 0; i < enemies.Count; i++)
            {
                PlayerRuntime enemy = enemies[i];
                if (enemy != null && enemy.Alive && SameZone(shooter, enemy))
                    same.Add(enemy);
            }

            if (same.Count > 0)
            {
                PlayerRuntime close = same[random.Range(0, same.Count)];
                CombatSightRules.FaceToward(shooter, close, map);
                float dist = CombatSightRules.DistanceUv(map, shooter, close);
                if (dist <= maxRange &&
                    CombatSightRules.InViewCone(map, shooter, close) &&
                    CombatSightRules.HasLineOfSight(map, shooter, close, los) &&
                    !UtilityCombatRules.BlocksSight(map, graph, shooter, close, utilities))
                    return close;
            }

            PlayerRuntime best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < enemies.Count; i++)
            {
                PlayerRuntime enemy = enemies[i];
                if (enemy == null || !enemy.Alive)
                    continue;
                float dist = CombatSightRules.DistanceUv(map, shooter, enemy);
                if (dist > maxRange)
                    continue;
                if (!CombatSightRules.InViewCone(map, shooter, enemy))
                    continue;
                if (!CombatSightRules.HasLineOfSight(map, shooter, enemy, los))
                    continue;
                if (UtilityCombatRules.BlocksSight(map, graph, shooter, enemy, utilities))
                    continue;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = enemy;
                }
            }

            return best;
        }

        /// <summary>
        /// Blinded: pick nearest in range ignoring cone, turn to face, then require cone + LOS.
        /// </summary>
        private static PlayerRuntime PickBlindSprayTarget(
            PlayerRuntime shooter,
            IReadOnlyList<PlayerRuntime> enemies,
            MapDefinition map,
            ICombatLosQuery los,
            MapZoneGraph graph,
            IReadOnlyList<ActiveUtilityEffect> utilities,
            float maxRange)
        {
            PlayerRuntime nearest = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < enemies.Count; i++)
            {
                PlayerRuntime enemy = enemies[i];
                if (enemy == null || !enemy.Alive)
                    continue;
                float dist = CombatSightRules.DistanceUv(map, shooter, enemy);
                if (dist > maxRange)
                    continue;
                if (UtilityCombatRules.BlocksSight(map, graph, shooter, enemy, utilities))
                    continue;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    nearest = enemy;
                }
            }

            if (nearest == null)
                return null;

            CombatSightRules.FaceToward(shooter, nearest, map);
            if (!CombatSightRules.InViewCone(map, shooter, nearest))
                return null;
            if (!CombatSightRules.HasLineOfSight(map, shooter, nearest, los))
                return null;
            return nearest;
        }

        private static bool SameZone(PlayerRuntime a, PlayerRuntime b)
        {
            return a != null && b != null &&
                   string.Equals(a.ZoneId, b.ZoneId, StringComparison.OrdinalIgnoreCase);
        }

        private void TryShoot(
            BehaviorTreeRoundWorld world,
            PlayerRuntime shooter,
            PlayerRuntime target,
            TeamRuntime shooterTeam,
            TeamRuntime targetTeam,
            float time,
            IRandomSource random)
        {
            if (!shooter.Alive || !target.Alive)
                return;
            if (time < shooter.NextShotTime)
                return;
            WeaponAmmoRules.TickReload(shooter, _catalog, time);
            if (WeaponAmmoRules.IsReloading(shooter, time))
                return;

            WeaponDefinition weapon = _catalog.GetOrKnife(WeaponAmmoRules.ActiveWeaponId(shooter));
            CombatSightRules.EnsureRangeStats(weapon);
            MapDefinition map = world.Request.map;
            float dist = CombatSightRules.DistanceUv(map, shooter, target);
            if (dist > weapon.maxEngageRange)
                return;
            if (!CombatSightRules.InViewCone(map, shooter, target))
                return;
            if (!CombatSightRules.HasLineOfSight(map, shooter, target, world.Request?.Los))
                return;
            if (UtilityCombatRules.BlocksSight(
                    map, world.Graph, shooter, target, world.ActiveUtilities))
                return;
            if (!WeaponAmmoRules.TryConsumeShot(shooter, _catalog, time, out string weaponId))
                return;
            weapon = _catalog.GetOrKnife(weaponId);
            CombatSightRules.EnsureRangeStats(weapon);

            float interval = Math.Max(0.08f, weapon.fireInterval);
            shooter.NextShotTime = time + interval;
            CombatSightRules.FaceToward(shooter, target, map);
            world.Add(time, EventType.Shot, shooter.Definition.id, target.Definition.id,
                shooter.ZoneId, null, shooterTeam.Definition.id, weapon.id);

            float aim = shooter.Definition.attributes.aim + shooter.FocusAim;
            float hitChance = CombatSightRules.HitChance(weapon, aim, shooter.FocusAllMult, dist);
            hitChance *= UtilityCombatRules.BlindHitMultiplier(shooter, time);
            if (random.Next01() > hitChance)
                return;

            HitResult hit = HitResolver.Apply(shooter, target, _catalog, random, weapon.id);
            target.LastDamageTime = time;
            WeaponDefinition held = _catalog.GetOrKnife(WeaponAmmoRules.ActiveWeaponId(target));
            CombatTaggingRules.ApplyHit(target, weapon, held);

            world.Add(time + 0.05f, EventType.StatusUpdated, target.Definition.id, shooter.Definition.id,
                target.ZoneId,
                $"hp:{target.Health}|armor:{target.ArmorHp}",
                targetTeam.Definition.id,
                weapon.id,
                hit.Headshot ? "headshot" : "body");

            if (!hit.Killed)
                return;

            var tags = new List<string>();
            int reward = weapon.killReward;
            tags.Add($"kill_reward:{reward}");
            if (hit.Headshot)
                tags.Add("headshot");
            RegisterPlayerDeath(
                world, target, shooter, targetTeam, shooterTeam, time,
                weapon.id,
                hit.Headshot
                    ? $"{shooter.Definition.id} headshot {target.Definition.id}"
                    : $"{shooter.Definition.id} killed {target.Definition.id}",
                tags.ToArray());
            RoundTraitRules.EmitKillTrait(
                world.Request, world.Result, shooter, target, target.ZoneId, hit.Headshot,
                time + 0.12f, random);
            world.Sequence = Math.Max(world.Sequence, world.Result.events.Count);
        }

        private static bool TryConcludeElimination(
            BehaviorTreeRoundWorld world, float time, out TeamRuntime winner, out string reason)
        {
            winner = null;
            reason = null;
            int atk = world.Request.attackers.AliveCount;
            int def = world.Request.defenders.AliveCount;
            if (atk > 0 && def > 0)
                return false;

            if (atk == 0 && def == 0)
            {
                // Simultaneous wipe: bomb state decides.
                if (world.BombPlanted && !world.BombDefused)
                {
                    winner = world.Request.attackers;
                    reason = "bomb_exploded";
                    return true;
                }
                winner = world.Request.defenders;
                reason = "elimination";
                return true;
            }

            if (atk == 0)
            {
                if (world.BombPlanted && !world.BombDefused)
                    return false; // CT must still defuse
                winner = world.Request.defenders;
                reason = "elimination";
                return true;
            }

            // def == 0
            winner = world.Request.attackers;
            reason = world.BombPlanted ? "bomb_exploded" : "elimination";
            return true;
        }

        private static void FinishRound(
            BehaviorTreeRoundWorld world, TeamRuntime winner, string reason, float time)
        {
            TeamRuntime loser = winner == world.Request.attackers
                ? world.Request.defenders
                : world.Request.attackers;
            ItemizedEconomyRules.Settle(winner, loser, reason);

            // Drop guns of dead players; keep for alive.
            foreach (PlayerRuntime p in loser.Players.Concat(winner.Players))
            {
                if (!p.Alive && p.Loadout != null)
                {
                    p.Loadout.PrimaryWeaponId = null;
                    p.KeptWeaponFromPriorRound = false;
                    p.Loadout.HasArmor = false;
                    p.Loadout.HasHelmet = false;
                    p.ArmorHp = 0;
                }
            }

            world.Result.winnerTeamId = winner.Definition.id;
            world.Result.winnerSide = winner.Side;
            world.Result.winReason = reason;
            world.Result.attackerAlive = world.Request.attackers.AliveCount;
            world.Result.defenderAlive = world.Request.defenders.AliveCount;
            world.DroppedBombZoneId = null;
            world.GroundLoot.Clear();
            PlantChannelRules.Clear(world, time, emitCancel: false);
            world.MindGameUntilTime.Clear();
            ClearDefuse(world);
            EmitHpSnapshot(world, time);
            world.Add(time + 0.2f, EventType.RoundEnded, null, null,
                world.Request.attackerPlan.primaryZoneId,
                $"{winner.Definition.displayName} wins by {reason}",
                winner.Definition.id);
        }

        private static void EmitHpSnapshot(BehaviorTreeRoundWorld world, float time)
        {
            foreach (PlayerRuntime player in
                     world.Request.attackers.Players.Concat(world.Request.defenders.Players))
            {
                string teamId = world.Request.attackers.Players.Contains(player)
                    ? world.Request.attackers.Definition.id
                    : world.Request.defenders.Definition.id;
                world.Add(time, EventType.StatusUpdated, player.Definition.id, null, player.ZoneId,
                    $"hp:{(player.Alive ? player.Health : 0)}|armor:{player.ArmorHp}",
                    teamId);
                time += 0.001f;
            }
        }

        private static PlayerRuntime SelectBombCarrier(TeamRuntime attackers)
        {
            return attackers.Players
                .OrderByDescending(p => p.Definition.primaryRole == "Igl" ? 2 :
                    p.Definition.primaryRole == "Entry" ? 1 : 0)
                .FirstOrDefault();
        }
    }

    /// <summary>Shared man-count gates for save / hunt behavior in general mode.</summary>
    public static class SaveHuntRules
    {
        /// <summary>Active weapon buy price at or below this never triggers save.</summary>
        public const int MaxCheapWeaponPrice = 1600;

        /// <summary>
        /// Pistol rounds or cheap guns: never hide — take fights.
        /// </summary>
        public static bool ShouldRefuseSave(bool isPistolRound, int activeWeaponPrice) =>
            isPistolRound || activeWeaponPrice <= MaxCheapWeaponPrice;

        /// <summary>
        /// Abort save and fight: 1v1, 1v2, or equal/better man count.
        /// </summary>
        public static bool ShouldAbortSave(int aliveSelf, int aliveEnemy)
        {
            if (aliveSelf <= 0 || aliveEnemy < 0)
                return true;
            if (aliveSelf >= aliveEnemy)
                return true;
            // Solo leftover vs 1–2: take the fight instead of hiding.
            if (aliveSelf == 1 && aliveEnemy <= 2)
                return true;
            return false;
        }

        /// <summary>
        /// Significant lead vs a low enemy count — push out and hunt remaining players.
        /// Pistol rounds: any man-count lead is enough.
        /// </summary>
        public static bool HasHuntAdvantage(int aliveSelf, int aliveEnemy, bool isPistolRound = false)
        {
            if (aliveEnemy <= 0 || aliveSelf <= 0)
                return false;
            if (isPistolRound)
                return aliveSelf > aliveEnemy;
            // Enemy already in save-range man count, and we lead by at least two.
            return aliveEnemy < 3 && aliveSelf - aliveEnemy >= 2;
        }

        /// <summary>
        /// Terrorists before plant prioritize execute / plant over hunting leftovers,
        /// except on pistol rounds where the advantaged side hunts freely.
        /// </summary>
        public static bool ShouldAllowHunt(
            bool isTerrorist, bool bombPlanted, int aliveSelf, int aliveEnemy,
            bool isPistolRound = false)
        {
            if (!isPistolRound && isTerrorist && !bombPlanted)
                return false;
            return HasHuntAdvantage(aliveSelf, aliveEnemy, isPistolRound);
        }
    }

    /// <summary>Cover / search / jiggle-peek gates used by the general-mode soldier tree.</summary>
    public static class TacticalManeuverRules
    {
        public static bool ShouldSeekCover(
            float health, float secondsSinceDamage, int sameZoneEnemies, int adjacentEnemies)
        {
            bool recent = secondsSinceDamage <= 3f;
            if (health < 40f && (sameZoneEnemies > 0 || adjacentEnemies > 0 || recent))
                return true;
            if (recent && health < 75f && (sameZoneEnemies > 0 || adjacentEnemies > 0))
                return true;
            if (sameZoneEnemies > 0 && health < 85f)
                return true;
            return false;
        }

        public static bool ShouldJigglePeek(
            bool alive,
            float health,
            int sameZoneEnemies,
            int adjacentEnemies,
            bool committedUtility,
            bool peekInProgress,
            float time,
            float nextPeekAt,
            bool atHold) =>
            ShouldStartPeek(
                alive, health, sameZoneEnemies, adjacentEnemies, committedUtility,
                peekInProgress, time, nextPeekAt, atHold, false, false);

        public static bool ShouldStartPeek(
            bool alive,
            float health,
            int sameZoneEnemies,
            int adjacentEnemies,
            bool committedUtility,
            bool peekInProgress,
            float time,
            float nextPeekAt,
            bool atHold,
            bool punishWindow,
            bool selfSlowed)
        {
            if (!alive || committedUtility)
                return false;
            if (peekInProgress)
                return true;
            if (selfSlowed)
                return false;
            if (health < 28f)
                return false;
            if (sameZoneEnemies > 0)
                return false;
            if (punishWindow && adjacentEnemies > 0 && health >= 40f)
                return true;
            if (time + 0.0001f < nextPeekAt)
                return false;
            if (adjacentEnemies <= 0 && !atHold)
                return false;
            if (adjacentEnemies <= 0 && time < 2f)
                return false;
            return true;
        }

        public static float SearchInterval(float awareness)
        {
            float a = Math.Max(0f, Math.Min(100f, awareness));
            return Math.Max(0.45f, 1.05f - a * 0.006f);
        }

        public static float PeekCooldown(float opening, float roll01, string role, bool sawContact = false)
        {
            float o = Math.Max(0f, Math.Min(100f, opening));
            float cooldown = 2.2f - o * 0.008f;
            if (string.Equals(role, "Entry", StringComparison.OrdinalIgnoreCase))
                cooldown *= 0.85f;
            else if (string.Equals(role, "Awper", StringComparison.OrdinalIgnoreCase))
                cooldown *= 1.15f;
            cooldown += (roll01 - 0.5f) * 0.6f;
            if (sawContact)
                cooldown *= 0.5f;
            return Math.Max(0.45f, cooldown);
        }
    }
}
