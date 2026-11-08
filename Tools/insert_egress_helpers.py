# -*- coding: utf-8 -*-
from pathlib import Path

p = Path("Assets/_Project/Scripts/Domain/BehaviorTreeRoundSimulator.cs")
src = p.read_text(encoding="utf-8")
if "private static string PreferSpawnAdjacentHide" in src:
    print("helpers already present")
else:
    helpers = r'''        private static string PreferSpawnAdjacentHide(BtContext c, string spawn)
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

'''
    anchor = "        private static bool IsSpawnZone(string zoneId) =>"
    if anchor not in src:
        raise SystemExit("anchor missing")
    src = src.replace(anchor, helpers + anchor, 1)
    p.write_text(src, encoding="utf-8")
    print("helpers inserted")

# sanity
src = p.read_text(encoding="utf-8")
for name in (
    "PreferSpawnAdjacentHide",
    "PickEgressZone",
    "ShouldAllowHunt",
    "PostPlantHoldRules.ShouldEvacuate",
    "SpawnHoldRules.RemapAwayFromSpawn",
    "private static int EnemiesInZone",
    "private static float GraphDistance",
):
    print(name, name in src)
