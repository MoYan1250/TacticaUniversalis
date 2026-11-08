using System;
using System.Collections.Generic;
using System.Linq;

namespace TacticaUniversalis.Domain
{
    /// <summary>
    /// CS:GO ~2021 C4 blast: 500 dmg / 1750 HU radius (map overrides), armor halves HP damage,
    /// armor loses about half of the absorbed amount. Walls ignored (zone UV distance only).
    /// </summary>
    public static class BombExplosionRules
    {
        public const float DefaultMaxDamage = 500f;
        public const float RadiusHu = 1750f;
        /// <summary>Maps UV delta of 1.0 to this many hammer units across a typical site map.</summary>
        public const float MapSpanHu = 2100f;

        public static float MaxDamageForMap(string mapId)
        {
            if (string.IsNullOrEmpty(mapId))
                return DefaultMaxDamage;
            if (string.Equals(mapId, "inferno", StringComparison.OrdinalIgnoreCase))
                return 620f;
            if (string.Equals(mapId, "nuke", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(mapId, "vertigo", StringComparison.OrdinalIgnoreCase))
                return 400f;
            return DefaultMaxDamage;
        }

        public static float DistanceHu(MapZoneDefinition from, MapZoneDefinition to)
        {
            if (from == null || to == null)
                return RadiusHu;
            float dx = (from.x - to.x) * MapSpanHu;
            float dy = (from.y - to.y) * MapSpanHu;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Raw pre-armor blast damage. Falloff shaped so ~full at epicenter and 0 at radius;
        /// armored survival ~1 HP near 788 HU and unarmored near 1046 HU for 500-max maps.
        /// </summary>
        public static float RawDamage(float distanceHu, float maxDamage = DefaultMaxDamage)
        {
            if (distanceHu <= 0f)
                return maxDamage;
            if (distanceHu >= RadiusHu)
                return 0f;
            float t = distanceHu / RadiusHu;
            // Power falloff fitted to CS:GO community survival distances.
            const float k = 1.65f;
            return maxDamage * (float)Math.Pow(1.0 - t, k);
        }

        public static void ApplyToPlayer(PlayerRuntime player, float rawDamage)
        {
            if (player == null || !player.Alive || rawDamage <= 0f)
                return;

            bool armored = player.ArmorHp > 0 &&
                           player.Loadout != null &&
                           player.Loadout.HasArmor;

            int toHealth;
            int toArmor = 0;
            if (armored)
            {
                // Armor halves HP damage; half of absorbed (raw/2) comes off armor → raw/4.
                toHealth = Math.Max(0, (int)Math.Ceiling(rawDamage * 0.5f));
                toArmor = Math.Max(0, (int)Math.Ceiling(rawDamage * 0.25f));
                if (toArmor > player.ArmorHp)
                    toArmor = player.ArmorHp;
            }
            else
            {
                toHealth = Math.Max(0, (int)Math.Ceiling(rawDamage));
            }

            player.Health = Math.Max(0, player.Health - toHealth);
            player.ArmorHp = Math.Max(0, player.ArmorHp - toArmor);
            if (player.ArmorHp <= 0 && player.Loadout != null)
            {
                player.Loadout.HasArmor = false;
                player.Loadout.HasHelmet = false;
            }

            if (player.Health <= 0)
            {
                player.Health = 0;
                player.Alive = false;
            }
        }

        public static void ApplyExplosion(BehaviorTreeRoundWorld world, float time)
        {
            if (world?.Request?.map == null || string.IsNullOrEmpty(world.PlantSiteId))
                return;

            MapDefinition map = world.Request.map;
            MapZoneDefinition plantZone = map.zones?.FirstOrDefault(z =>
                string.Equals(z.id, world.PlantSiteId, StringComparison.OrdinalIgnoreCase));
            if (plantZone == null)
                return;

            float maxDamage = MaxDamageForMap(map.id);
            var victims = world.Request.attackers.Players
                .Concat(world.Request.defenders.Players)
                .Where(p => p != null && p.Alive)
                .ToList();

            foreach (PlayerRuntime player in victims)
            {
                MapZoneDefinition zone = map.zones?.FirstOrDefault(z =>
                    string.Equals(z.id, player.ZoneId, StringComparison.OrdinalIgnoreCase));
                float dist = DistanceHu(plantZone, zone);
                float raw = RawDamage(dist, maxDamage);
                if (raw <= 0.5f)
                    continue;

                int hpBefore = player.Health;
                ApplyToPlayer(player, raw);
                string teamId = world.Request.attackers.Players.Contains(player)
                    ? world.Request.attackers.Definition.id
                    : world.Request.defenders.Definition.id;

                world.Add(time + 0.05f, EventType.StatusUpdated, player.Definition.id, null,
                    player.ZoneId,
                    $"hp:{player.Health}|armor:{player.ArmorHp}",
                    teamId,
                    null,
                    "bomb_damage");

                if (!player.Alive && hpBefore > 0)
                {
                    player.Deaths++;
                    world.Add(time + 0.08f, EventType.Kill, null, player.Definition.id,
                        player.ZoneId,
                        $"bomb killed {player.Definition.id}",
                        world.Request.attackers.Definition.id,
                        "c4",
                        "bomb_kill");
                }
            }
        }
    }
}
