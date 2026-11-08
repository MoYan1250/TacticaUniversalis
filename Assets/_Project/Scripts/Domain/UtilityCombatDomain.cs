using System;
using System.Collections.Generic;
using System.Linq;

namespace TacticaUniversalis.Domain
{
    public enum UtilityKind
    {
        Smoke = 0,
        He = 1,
        Fire = 2,
        Flash = 3
    }

    public sealed class ActiveUtilityEffect
    {
        public UtilityKind Kind;
        public string ZoneId;
        public string OwnerTeamId;
        public string OwnerPlayerId;
        public float ExpireAt;
        public float RadiusUv = 0.045f;
        public float NextFireTickAt;
    }

    /// <summary>Zone-anchored utility pops: smoke blocks sight, HE damages, fire burns and blocks path.</summary>
    public static class UtilityCombatRules
    {
        public const float SmokeDurationSeconds = 15f;
        public const float FireDurationSeconds = 7f;
        public const float FireTickSeconds = 0.5f;
        public const int FireTickDamageMax = 6;
        public const int FireTickDamageMin = 2;
        /// <summary>Legacy alias for near-center tick damage.</summary>
        public const int FireTickDamage = FireTickDamageMax;
        public const float FireRadiusUv = 0.055f;
        public const int HeDamageSameZone = 55;
        public const int HeDamageAdjacent = 28;
        public const float DefaultSmokeRadiusUv = 0.045f;
        public const float SmokePathCost = 8f;
        public const float FirePathCost = 14f;
        public const float ThrowCooldownSeconds = 4.5f;
        public const float FlashSameZoneSeconds = 3.2f;
        public const float FlashSameZoneMinSeconds = 1.4f;
        public const float FlashAdjacentFacingSeconds = 1.8f;
        public const float FlashAdjacentAwaySeconds = 0.7f;
        /// <summary>HitChance scale while blinded: full face (worst) → back (best among blind).</summary>
        public const float FlashBlindHitMultWorst = 0.05f;
        public const float FlashBlindHitMultBest = 0.28f;

        public static string WeaponIdFor(UtilityKind kind)
        {
            switch (kind)
            {
                case UtilityKind.Smoke: return "smoke";
                case UtilityKind.He: return "he";
                case UtilityKind.Fire: return "fire";
                case UtilityKind.Flash: return "flash";
                default: return "utility";
            }
        }

        public static bool TryParseWeaponId(string weaponId, out UtilityKind kind)
        {
            kind = UtilityKind.Smoke;
            if (string.IsNullOrEmpty(weaponId))
                return false;
            if (string.Equals(weaponId, "smoke", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(weaponId, "smoke_grenade", StringComparison.OrdinalIgnoreCase))
            {
                kind = UtilityKind.Smoke;
                return true;
            }
            if (string.Equals(weaponId, "he", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(weaponId, "hegrenade", StringComparison.OrdinalIgnoreCase))
            {
                kind = UtilityKind.He;
                return true;
            }
            if (string.Equals(weaponId, "fire", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(weaponId, "molotov", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(weaponId, "incendiary", StringComparison.OrdinalIgnoreCase))
            {
                kind = UtilityKind.Fire;
                return true;
            }
            if (string.Equals(weaponId, "flash", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(weaponId, "flashbang", StringComparison.OrdinalIgnoreCase))
            {
                kind = UtilityKind.Flash;
                return true;
            }
            return false;
        }

        public static void Expire(List<ActiveUtilityEffect> effects, float time)
        {
            if (effects == null || effects.Count == 0)
                return;
            for (int i = effects.Count - 1; i >= 0; i--)
            {
                ActiveUtilityEffect e = effects[i];
                if (e == null || time >= e.ExpireAt)
                    effects.RemoveAt(i);
            }
        }

        public static bool HasActiveSmoke(IReadOnlyList<ActiveUtilityEffect> effects, string zoneId) =>
            HasKindInZone(effects, zoneId, UtilityKind.Smoke);

        public static bool HasActiveFire(IReadOnlyList<ActiveUtilityEffect> effects, string zoneId) =>
            HasKindInZone(effects, zoneId, UtilityKind.Fire);

        public static bool HasKindInZone(
            IReadOnlyList<ActiveUtilityEffect> effects, string zoneId, UtilityKind kind)
        {
            if (effects == null || string.IsNullOrEmpty(zoneId))
                return false;
            for (int i = 0; i < effects.Count; i++)
            {
                ActiveUtilityEffect e = effects[i];
                if (e == null || e.Kind != kind)
                    continue;
                if (string.Equals(e.ZoneId, zoneId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        public static float ZonePathCost(IReadOnlyList<ActiveUtilityEffect> effects, string zoneId)
        {
            float cost = 1f;
            if (HasActiveFire(effects, zoneId))
                cost += FirePathCost;
            if (HasActiveSmoke(effects, zoneId))
                cost += SmokePathCost;
            return cost;
        }

        /// <summary>
        /// Same-zone fights still resolve inside smoke. Cross-zone sight is blocked when the
        /// segment clips a smoke radius or the cheapest graph path walks through a smoked zone.
        /// </summary>
        public static bool BlocksSight(
            MapDefinition map,
            MapZoneGraph graph,
            PlayerRuntime observer,
            PlayerRuntime target,
            IReadOnlyList<ActiveUtilityEffect> effects)
        {
            if (observer == null || target == null || effects == null || effects.Count == 0)
                return false;

            if (string.Equals(observer.ZoneId, target.ZoneId, StringComparison.OrdinalIgnoreCase))
                return false;

            CombatSightRules.MapUv(map, observer, out float ox, out float oy);
            CombatSightRules.MapUv(map, target, out float tx, out float ty);
            for (int i = 0; i < effects.Count; i++)
            {
                ActiveUtilityEffect e = effects[i];
                if (e == null || e.Kind != UtilityKind.Smoke || string.IsNullOrEmpty(e.ZoneId))
                    continue;
                CombatSightRules.ZoneUv(map, e.ZoneId, null, out float sx, out float sy);
                float radius = e.RadiusUv > 0f ? e.RadiusUv : DefaultSmokeRadiusUv;
                if (SegmentDistanceToPoint(ox, oy, tx, ty, sx, sy) <= radius)
                    return true;
            }

            if (graph != null &&
                !string.IsNullOrEmpty(observer.ZoneId) &&
                !string.IsNullOrEmpty(target.ZoneId))
            {
                IReadOnlyList<string> path = graph.FindPath(observer.ZoneId, target.ZoneId);
                if (path != null && path.Count > 2)
                {
                    for (int i = 1; i < path.Count - 1; i++)
                    {
                        if (HasActiveSmoke(effects, path[i]))
                            return true;
                    }
                }
            }

            return false;
        }

        public static bool ShouldPushThrough(
            bool bombPlanted,
            bool isRetake,
            bool isHunt,
            bool isBombCarrier,
            bool contestingPlant,
            int aliveSelf,
            int aliveEnemy)
        {
            if (isBombCarrier || contestingPlant)
                return true;
            if (bombPlanted && isRetake)
                return true;
            if (isHunt && aliveEnemy <= 2)
                return true;
            if (aliveSelf <= 2 && aliveEnemy <= 2)
                return true;
            return false;
        }

        public static ActiveUtilityEffect CreateSmoke(
            string zoneId, string ownerTeamId, string ownerPlayerId, float time) =>
            new ActiveUtilityEffect
            {
                Kind = UtilityKind.Smoke,
                ZoneId = zoneId,
                OwnerTeamId = ownerTeamId,
                OwnerPlayerId = ownerPlayerId,
                ExpireAt = time + SmokeDurationSeconds,
                RadiusUv = DefaultSmokeRadiusUv
            };

        public static ActiveUtilityEffect CreateFire(
            string zoneId, string ownerTeamId, string ownerPlayerId, float time) =>
            new ActiveUtilityEffect
            {
                Kind = UtilityKind.Fire,
                ZoneId = zoneId,
                OwnerTeamId = ownerTeamId,
                OwnerPlayerId = ownerPlayerId,
                ExpireAt = time + FireDurationSeconds,
                RadiusUv = FireRadiusUv,
                NextFireTickAt = time + FireTickSeconds
            };

        /// <summary>Anchor zone plus all playable neighbors — multi-cell fire wall.</summary>
        public static List<ActiveUtilityEffect> CreateFireWall(
            MapDefinition map,
            MapZoneGraph graph,
            string anchorZoneId,
            string ownerTeamId,
            string ownerPlayerId,
            float time)
        {
            var list = new List<ActiveUtilityEffect>();
            if (string.IsNullOrEmpty(anchorZoneId))
                return list;

            void TryAdd(string zoneId)
            {
                if (string.IsNullOrEmpty(zoneId) || !IsPlayableZone(map, zoneId))
                    return;
                if (list.Any(e => string.Equals(e.ZoneId, zoneId, StringComparison.OrdinalIgnoreCase)))
                    return;
                list.Add(CreateFire(zoneId, ownerTeamId, ownerPlayerId, time));
            }

            TryAdd(anchorZoneId);
            if (graph != null)
            {
                foreach (string n in graph.Neighbors(anchorZoneId))
                    TryAdd(n);
            }

            return list;
        }

        public static bool IsPlayableZone(MapDefinition map, string zoneId)
        {
            if (map?.zones == null || string.IsNullOrEmpty(zoneId))
                return false;
            for (int i = 0; i < map.zones.Length; i++)
            {
                MapZoneDefinition z = map.zones[i];
                if (z == null || !string.Equals(z.id, zoneId, StringComparison.OrdinalIgnoreCase))
                    continue;
                return !z.navigationOnly;
            }
            return false;
        }

        public static bool TryConsumeNade(PlayerRuntime player, UtilityKind kind)
        {
            if (player?.Loadout == null)
                return false;
            switch (kind)
            {
                case UtilityKind.Smoke:
                    if (!player.Loadout.HasSmoke)
                        return false;
                    player.Loadout.HasSmoke = false;
                    return true;
                case UtilityKind.He:
                    if (!player.Loadout.HasHe)
                        return false;
                    player.Loadout.HasHe = false;
                    return true;
                case UtilityKind.Fire:
                    if (!player.Loadout.HasFire)
                        return false;
                    player.Loadout.HasFire = false;
                    player.Loadout.FireKind = FireNadeKind.None;
                    return true;
                case UtilityKind.Flash:
                    if (player.Loadout.FlashCount <= 0)
                        return false;
                    player.Loadout.FlashCount--;
                    return true;
                default:
                    return false;
            }
        }

        public static bool PlayerHasNade(PlayerRuntime player, UtilityKind kind)
        {
            if (player?.Loadout == null)
                return false;
            switch (kind)
            {
                case UtilityKind.Smoke: return player.Loadout.HasSmoke;
                case UtilityKind.He: return player.Loadout.HasHe;
                case UtilityKind.Fire: return player.Loadout.HasFire;
                case UtilityKind.Flash: return player.Loadout.FlashCount > 0;
                default: return false;
            }
        }

        public static bool IsBlinded(PlayerRuntime player, float time) =>
            player != null && player.BlindedUntil > time;

        /// <summary>
        /// Hit chance scale while blinded. Not blinded → 1. Back flash (low severity) is
        /// highest among blind states; full face is worst.
        /// </summary>
        public static float BlindHitMultiplier(PlayerRuntime player, float time)
        {
            if (!IsBlinded(player, time))
                return 1f;
            float backness = 1f - Clamp01(player.BlindSeverity01);
            return Lerp(FlashBlindHitMultWorst, FlashBlindHitMultBest, backness);
        }

        /// <summary>
        /// Instant flash pop. Duration lerps by facing the blast (back = shortest).
        /// Affects all living players including teammates. Writes BlindSeverity01.
        /// </summary>
        public static void ApplyFlashBlast(
            MapDefinition map,
            MapZoneGraph graph,
            string blastZoneId,
            float time,
            IEnumerable<PlayerRuntime> allAlive,
            Action<PlayerRuntime, float> onBlinded)
        {
            if (map == null || string.IsNullOrEmpty(blastZoneId) || allAlive == null)
                return;

            CombatSightRules.ZoneUv(map, blastZoneId, null, out float bx, out float by);

            foreach (PlayerRuntime player in allAlive)
            {
                if (player == null || !player.Alive)
                    continue;

                float duration = FlashDurationFor(
                    map, graph, player, blastZoneId, bx, by, out float facing01);
                if (duration <= 0f)
                    continue;

                float until = time + duration;
                if (until > player.BlindedUntil)
                {
                    player.BlindedUntil = until;
                    player.BlindSeverity01 = facing01;
                }
                else if (until >= player.BlindedUntil - 0.001f && facing01 > player.BlindSeverity01)
                {
                    player.BlindSeverity01 = facing01;
                }

                onBlinded?.Invoke(player, player.BlindedUntil);
            }
        }

        public static float FlashDurationFor(
            MapDefinition map,
            MapZoneGraph graph,
            PlayerRuntime player,
            string blastZoneId,
            float blastX,
            float blastY) =>
            FlashDurationFor(map, graph, player, blastZoneId, blastX, blastY, out _);

        public static float FlashDurationFor(
            MapDefinition map,
            MapZoneGraph graph,
            PlayerRuntime player,
            string blastZoneId,
            float blastX,
            float blastY,
            out float facing01)
        {
            facing01 = 0f;
            if (player == null || string.IsNullOrEmpty(blastZoneId))
                return 0f;

            bool sameZone = string.Equals(
                player.ZoneId, blastZoneId, StringComparison.OrdinalIgnoreCase);
            bool adjacent = !sameZone && graph != null &&
                            IsAdjacent(graph, player.ZoneId, blastZoneId);
            if (!sameZone && !adjacent)
                return 0f;

            facing01 = FlashFacing01(map, player, blastX, blastY);
            if (sameZone)
                return Lerp(FlashSameZoneMinSeconds, FlashSameZoneSeconds, facing01);
            return Lerp(FlashAdjacentAwaySeconds, FlashAdjacentFacingSeconds, facing01);
        }

        /// <summary>1 = facing blast, 0 = looking away (180°).</summary>
        public static float FlashFacing01(
            MapDefinition map, PlayerRuntime player, float blastX, float blastY)
        {
            if (player == null)
                return 0f;
            CombatSightRules.MapUv(map, player, out float px, out float py);
            float dx = blastX - px;
            float dy = blastY - py;
            if (dx * dx + dy * dy < 1e-10f)
                return 1f;
            float targetDeg = (float)(Math.Atan2(dy, dx) * 180.0 / Math.PI);
            float delta = Math.Abs(CombatSightRules.DeltaAngle(player.FacingDeg, targetDeg));
            return 1f - Clamp01(delta / 180f);
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);

        private static float Clamp01(float v)
        {
            if (v < 0f)
                return 0f;
            if (v > 1f)
                return 1f;
            return v;
        }

        /// <summary>Apply HE damage to living players in target / adjacent zones (all teams).</summary>
        public static void ApplyHeDamage(
            MapDefinition map,
            MapZoneGraph graph,
            string blastZoneId,
            TeamRuntime victims,
            float time,
            Action<PlayerRuntime, int> onDamaged)
        {
            if (victims?.Players == null)
                return;
            ApplyHeDamage(map, graph, blastZoneId, victims.Players, time, onDamaged);
        }

        public static void ApplyHeDamage(
            MapDefinition map,
            MapZoneGraph graph,
            string blastZoneId,
            IEnumerable<PlayerRuntime> allAlive,
            float time,
            Action<PlayerRuntime, int> onDamaged)
        {
            if (allAlive == null || string.IsNullOrEmpty(blastZoneId) || onDamaged == null)
                return;

            foreach (PlayerRuntime p in allAlive)
            {
                if (p == null || !p.Alive)
                    continue;
                int damage = 0;
                if (string.Equals(p.ZoneId, blastZoneId, StringComparison.OrdinalIgnoreCase))
                    damage = HeDamageSameZone;
                else if (graph != null && IsAdjacent(graph, p.ZoneId, blastZoneId))
                    damage = HeDamageAdjacent;
                if (damage <= 0)
                    continue;
                onDamaged(p, damage);
            }
        }

        public static void ApplyFlatDamage(PlayerRuntime victim, int damage)
        {
            if (victim == null || !victim.Alive || damage <= 0)
                return;
            int remaining = damage;
            if (victim.ArmorHp > 0)
            {
                int toArmor = Math.Min(victim.ArmorHp, (remaining + 1) / 2);
                victim.ArmorHp -= toArmor;
                remaining -= toArmor;
                if (victim.ArmorHp <= 0 && victim.Loadout != null)
                {
                    victim.Loadout.HasArmor = false;
                    victim.Loadout.HasHelmet = false;
                }
            }

            victim.Health = Math.Max(0, victim.Health - remaining);
            if (victim.Health <= 0)
                victim.Alive = false;
        }

        public static int FireTickDamageAtDistance(float distanceUv)
        {
            float r = FireRadiusUv <= 1e-6f ? 1e-6f : FireRadiusUv;
            float t = Clamp01(distanceUv / r);
            return (int)Math.Round(Lerp(FireTickDamageMax, FireTickDamageMin, t));
        }

        /// <summary>
        /// Burns living players within FireRadiusUv of any active fire zone center.
        /// Damage falls off with distance. Only runs when at least one fire tick is due.
        /// </summary>
        public static void TickFire(
            List<ActiveUtilityEffect> effects,
            MapDefinition map,
            TeamRuntime attackers,
            TeamRuntime defenders,
            float time,
            Action<PlayerRuntime, int, string> onBurned)
        {
            if (effects == null || effects.Count == 0 || onBurned == null)
                return;

            bool anyDue = false;
            var fireCenters = new List<(ActiveUtilityEffect Effect, float X, float Y)>();
            for (int i = 0; i < effects.Count; i++)
            {
                ActiveUtilityEffect e = effects[i];
                if (e == null || e.Kind != UtilityKind.Fire || string.IsNullOrEmpty(e.ZoneId))
                    continue;
                if (time + 1e-4f >= e.NextFireTickAt)
                {
                    e.NextFireTickAt = time + FireTickSeconds;
                    anyDue = true;
                }
                CombatSightRules.ZoneUv(map, e.ZoneId, null, out float fx, out float fy);
                fireCenters.Add((e, fx, fy));
            }

            if (!anyDue || fireCenters.Count == 0)
                return;

            void BurnPlayer(PlayerRuntime p)
            {
                if (p == null || !p.Alive)
                    return;
                CombatSightRules.MapUv(map, p, out float px, out float py);
                float bestDist = float.MaxValue;
                string bestZone = null;
                for (int i = 0; i < fireCenters.Count; i++)
                {
                    float dx = px - fireCenters[i].X;
                    float dy = py - fireCenters[i].Y;
                    float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestZone = fireCenters[i].Effect.ZoneId;
                    }
                }

                if (bestDist > FireRadiusUv || bestZone == null)
                    return;
                int damage = FireTickDamageAtDistance(bestDist);
                ApplyFlatDamage(p, damage);
                p.LastDamageTime = time;
                onBurned(p, damage, bestZone);
            }

            if (attackers?.Players != null)
            {
                for (int i = 0; i < attackers.Players.Count; i++)
                    BurnPlayer(attackers.Players[i]);
            }
            if (defenders?.Players != null)
            {
                for (int i = 0; i < defenders.Players.Count; i++)
                    BurnPlayer(defenders.Players[i]);
            }
        }

        private static bool IsAdjacent(MapZoneGraph graph, string a, string b)
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

        private static float SegmentDistanceToPoint(
            float x0, float y0, float x1, float y1, float px, float py)
        {
            float dx = x1 - x0;
            float dy = y1 - y0;
            float lenSq = dx * dx + dy * dy;
            if (lenSq < 1e-10f)
            {
                float ex = px - x0;
                float ey = py - y0;
                return (float)Math.Sqrt(ex * ex + ey * ey);
            }

            float t = ((px - x0) * dx + (py - y0) * dy) / lenSq;
            if (t < 0f)
                t = 0f;
            else if (t > 1f)
                t = 1f;
            float cx = x0 + t * dx;
            float cy = y0 + t * dy;
            float ox = px - cx;
            float oy = py - cy;
            return (float)Math.Sqrt(ox * ox + oy * oy);
        }
    }
}
