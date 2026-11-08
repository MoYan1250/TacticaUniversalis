using System;

namespace TacticaUniversalis.Domain
{
    /// <summary>Bomb plant channel (non-instant) for behavior-tree rounds.</summary>
    public static class PlantChannelRules
    {
        public const float RequiredSeconds = 3.2f;

        public static bool IsChanneling(BehaviorTreeRoundWorld world) =>
            world != null &&
            !world.BombPlanted &&
            !string.IsNullOrEmpty(world.PlanterId) &&
            world.PlantStartTime >= 0f;

        public static void Clear(BehaviorTreeRoundWorld world, float time, bool emitCancel)
        {
            if (world == null)
                return;
            if (emitCancel && IsChanneling(world))
            {
                string site = ContentDatabase.ClampZoneToMap(world.Request.map, world.PlantSiteId);
                string planter = world.PlanterId;
                world.Add(time, EventType.StatusUpdated, planter, null, site,
                    "plant_cancel", world.Request.attackers.Definition.id, null, "plant_cancel");
            }

            world.PlanterId = null;
            world.PlantStartTime = -1f;
            world.PlantRequiredSeconds = 0f;
        }

        public static bool TryBegin(BehaviorTreeRoundWorld world, PlayerRuntime planter, string site, float time)
        {
            if (world == null || planter == null || !planter.Alive || world.BombPlanted)
                return false;
            if (string.IsNullOrEmpty(site))
                return false;
            if (IsChanneling(world) &&
                string.Equals(world.PlanterId, planter.Definition.id, StringComparison.OrdinalIgnoreCase))
                return true;

            world.PlanterId = planter.Definition.id;
            world.PlantStartTime = time;
            world.PlantRequiredSeconds = RequiredSeconds;
            world.PlantSiteId = site;
            world.Add(time, EventType.StatusUpdated, planter.Definition.id, null, site,
                $"planting:{RequiredSeconds:0.0}", world.Request.attackers.Definition.id, null, "planting");
            return true;
        }

        public static void Tick(BehaviorTreeRoundWorld world, float time)
        {
            if (world == null || world.BombPlanted || world.BombExploded)
                return;
            if (!IsChanneling(world))
                return;

            PlayerRuntime planter = null;
            foreach (PlayerRuntime p in world.Request.attackers.AlivePlayers)
            {
                if (string.Equals(p.Definition.id, world.PlanterId, StringComparison.OrdinalIgnoreCase))
                {
                    planter = p;
                    break;
                }
            }

            string site = ContentDatabase.ClampZoneToMap(world.Request.map, world.PlantSiteId);
            bool valid = planter != null &&
                string.Equals(planter.ZoneId, site, StringComparison.OrdinalIgnoreCase);
            if (!valid)
            {
                Clear(world, time, emitCancel: true);
                return;
            }

            if (time + 0.0001f < world.PlantStartTime + world.PlantRequiredSeconds)
                return;

            world.BombPlanted = true;
            world.PlantTime = time;
            world.PlantSiteId = site;
            string planterId = world.PlanterId;
            Clear(world, time, emitCancel: false);
            world.Add(time, EventType.BombPlanted, planterId, null, site,
                $"{planterId} planted", world.Request.attackers.Definition.id);
        }
    }
}
