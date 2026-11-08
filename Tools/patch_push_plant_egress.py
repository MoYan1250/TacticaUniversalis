# -*- coding: utf-8 -*-
from pathlib import Path

root = Path(".")
bt_path = root / "Assets/_Project/Scripts/Domain/BehaviorTreeRoundSimulator.cs"
src = bt_path.read_text(encoding="utf-8")
orig = src

def req(label, old, new):
    global src
    if old not in src:
        raise SystemExit("MISSING " + label + "\n---\n" + old[:200])
    src = src.replace(old, new, 1)
    print("ok", label)

csproj = root / "TacticaUniversalis.Domain.csproj"
ct = csproj.read_text(encoding="utf-8")
anchor = '    <Compile Include="Assets\\_Project\\Scripts\\Domain\\PlantChannelRules.cs" />\n'
for f in ("PostPlantHoldRules.cs", "SpawnHoldRules.cs"):
    line = f'    <Compile Include="Assets\\_Project\\Scripts\\Domain\\{f}" />\n'
    if f not in ct:
        if anchor not in ct:
            raise SystemExit("csproj anchor missing")
        ct = ct.replace(anchor, anchor + line, 1)
        print("csproj +", f)
csproj.write_text(ct, encoding="utf-8")

req("save-chance",
    "                chance += 0.22f;\n            chance = Math.Min(0.92f, Math.Max(0.15f, chance));",
    "                chance += 0.12f;\n            chance = Math.Min(0.75f, Math.Max(0.15f, chance));")

req("hide-empty",
    "            if (enemies.Count == 0)\n                return spawn;",
    "            if (enemies.Count == 0)\n                return PreferSpawnAdjacentHide(c, spawn);")

req("hide-score",
"""                float score = nearestEnemy * 4f - toSpawn * 0.35f;
                if (string.Equals(zone, spawn, StringComparison.OrdinalIgnoreCase))
                    score += 1.5f;
                if (score > bestScore)""",
"""                float score = nearestEnemy * 4f - toSpawn * 0.35f;
                // Prefer spawn-adjacent playable holds; do not sit on the spawn tile itself.
                if (string.Equals(zone, spawn, StringComparison.OrdinalIgnoreCase))
                    score -= 2.5f;
                else if (c.World.Graph.Neighbors(spawn).Any(n =>
                             string.Equals(n, zone, StringComparison.OrdinalIgnoreCase)))
                    score += 1.2f;
                if (score > bestScore)""")

req("should-hunt",
"""        private static bool ShouldHunt(BtContext c)
        {
            if (!c.Self.Alive)
                return false;
            return SaveHuntRules.HasHuntAdvantage(
                c.SelfTeam.AliveCount, c.EnemyTeam.AliveCount);
        }""",
"""        private static bool ShouldHunt(BtContext c)
        {
            if (!c.Self.Alive)
                return false;
            return SaveHuntRules.ShouldAllowHunt(
                c.SelfTeam.Side == Side.Terrorist,
                c.World.BombPlanted,
                c.SelfTeam.AliveCount,
                c.EnemyTeam.AliveCount);
        }""")

req("assign-spawn",
"""                if (attacking && IsSpawnZone(zone))
                {
                    string push = pool
                        .Select(z => ContentDatabase.ClampZoneToMap(map, z))
                        .FirstOrDefault(z => !string.IsNullOrEmpty(z) && !IsSpawnZone(z) &&
                                             IsPlayableHoldZone(map, z));
                    if (string.IsNullOrEmpty(push))
                        push = ContentDatabase.ClampZoneToMap(map, primary);
                    if (IsSpawnZone(push))
                        push = ContentDatabase.ClampZoneToMap(map, MapZones.ResolvePlantSite(primary));
                    zone = push;
                }""",
"""                if (IsSpawnZone(zone))
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
                }""")

req("execute-target",
"""                    // Carrier pushes site; others take personal approach / lurk slots.
                    string target = string.Equals(c.Self.Definition.id, c.World.BombCarrierId,
                        StringComparison.OrdinalIgnoreCase)
                        ? site
                        : assigned;
                    StepToward(c, target);""",
"""                    // Carrier pushes site; others take personal approach / lurk slots.
                    string target = string.Equals(c.Self.Definition.id, c.World.BombCarrierId,
                        StringComparison.OrdinalIgnoreCase)
                        ? site
                        : assigned;
                    // Equal/better man count: never idle on spawn — push the plant site.
                    if (SpawnHoldRules.IsSpawnZone(c.Self.ZoneId) &&
                        c.SelfTeam.AliveCount >= c.EnemyTeam.AliveCount)
                        target = site;
                    StepToward(c, target);""")

req("post-plant",
"""                    bool heardDefuse = AcousticIntelRules.BoardHasFreshTag(
                        TeamIntelBoard(c), AcousticIntelRules.TagDefusing, c.Time,
                        AcousticIntelRules.AcousticTtlSeconds);
                    string postPlant = heardDefuse
                        ? ContentDatabase.ClampZoneToMap(c.World.Request.map, c.World.PlantSiteId)
                        : HoldNearSite(c, c.World.PlantSiteId, assigned);
                    StepToward(c, postPlant);""",
"""                    bool heardDefuse = AcousticIntelRules.BoardHasFreshTag(
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
                    StepToward(c, postPlant);""")

helpers = """        private static string PreferSpawnAdjacentHide(BtContext c, string spawn)
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

"""

if "PreferSpawnAdjacentHide" not in src:
    req("helpers",
        "        private static bool IsSpawnZone(string zoneId) =>",
        helpers + "        private static bool IsSpawnZone(string zoneId) =>")

req("is-spawn-delegate",
"""        private static bool IsSpawnZone(string zoneId) =>
            string.Equals(zoneId, "t_spawn", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(zoneId, "ct_spawn", StringComparison.OrdinalIgnoreCase);""",
"""        private static bool IsSpawnZone(string zoneId) =>
            SpawnHoldRules.IsSpawnZone(zoneId);""")

req("allow-hunt",
"""        public static bool HasHuntAdvantage(int aliveSelf, int aliveEnemy)
        {
            if (aliveEnemy <= 0 || aliveSelf <= 0)
                return false;
            // Enemy already in save-range man count, and we lead by at least two.
            return aliveEnemy < 3 && aliveSelf - aliveEnemy >= 2;
        }
    }""",
"""        public static bool HasHuntAdvantage(int aliveSelf, int aliveEnemy)
        {
            if (aliveEnemy <= 0 || aliveSelf <= 0)
                return false;
            // Enemy already in save-range man count, and we lead by at least two.
            return aliveEnemy < 3 && aliveSelf - aliveEnemy >= 2;
        }

        /// <summary>
        /// Terrorists before plant prioritize execute / plant over hunting leftovers.
        /// </summary>
        public static bool ShouldAllowHunt(
            bool isTerrorist, bool bombPlanted, int aliveSelf, int aliveEnemy)
        {
            if (isTerrorist && !bombPlanted)
                return false;
            return HasHuntAdvantage(aliveSelf, aliveEnemy);
        }
    }""")

if src == orig:
    raise SystemExit("no changes")
bt_path.write_text(src, encoding="utf-8")
print("DONE delta", len(src) - len(orig))
