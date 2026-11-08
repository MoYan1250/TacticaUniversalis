using System;

namespace TacticaUniversalis.Domain
{
    /// <summary>
    /// Hit-chance factors: P = clamp(A * R * S * F, 0.04, 0.93).
    /// </summary>
    public readonly struct ShotAccuracy
    {
        public readonly float AimFactor;
        public readonly float RangeFactor;
        public readonly float StanceFactor;
        public readonly bool HipFire;
        public readonly float HitChance;

        public ShotAccuracy(
            float aimFactor,
            float rangeFactor,
            float stanceFactor,
            bool hipFire,
            float hitChance)
        {
            AimFactor = aimFactor;
            RangeFactor = rangeFactor;
            StanceFactor = stanceFactor;
            HipFire = hipFire;
            HitChance = hitChance;
        }
    }

    /// <summary>
    /// General-mode sight. View cone is a fire gate, not part of hit chance.
    /// After the cone check: P(hit) = clamp(A(aim) * R(weapon, d) * S(weapon, d) * F, 0.04, 0.93)
    /// A = aim factor, R = per-weapon UV range curve, S = scope/hip-fire stance, F = FocusAllMult.
    /// Distances use map UV 0–1.
    /// </summary>
    public static class CombatSightRules
    {
        public const float ViewConeDegrees = 78f;

        public static void EnsureRangeStats(WeaponDefinition weapon)
        {
            if (weapon == null)
                return;
            string id = weapon.id ?? string.Empty;
            string cat = weapon.category ?? string.Empty;
            bool sniper = cat.IndexOf("sniper", StringComparison.OrdinalIgnoreCase) >= 0 ||
                          string.Equals(id, "awp", StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(id, "ssg08", StringComparison.OrdinalIgnoreCase);
            bool knife = cat.IndexOf("knife", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         string.Equals(id, "knife", StringComparison.OrdinalIgnoreCase);
            bool smg = cat.IndexOf("smg", StringComparison.OrdinalIgnoreCase) >= 0;
            bool pistol = cat.IndexOf("pistol", StringComparison.OrdinalIgnoreCase) >= 0;

            if (weapon.rangeFalloff <= 0f || weapon.optimalRange <= 0f || weapon.maxEngageRange <= 0f)
            {
                if (knife)
                    SetRange(weapon, 8.5f, 0.02f, 0.05f, 0f);
                else if (IdEq(id, "Dual_Berettas"))
                    SetRange(weapon, 7.2f, 0.04f, 0.10f, 0f);
                else if (IdEq(id, "mac10"))
                    SetRange(weapon, 6.8f, 0.04f, 0.10f, 0f);
                else if (IdEq(id, "mp9"))
                    SetRange(weapon, 5.6f, 0.05f, 0.12f, 0f);
                else if (IdEq(id, "ump45"))
                    SetRange(weapon, 5.0f, 0.05f, 0.12f, 0f);
                else if (IdEq(id, "mp7"))
                    SetRange(weapon, 3.6f, 0.05f, 0.12f, 0f);
                else if (IdEq(id, "p250"))
                    SetRange(weapon, 2.8f, 0.045f, 0.11f, 0f);
                else if (IdEq(id, "glock"))
                    SetRange(weapon, 2.6f, 0.045f, 0.11f, 0f);
                else if (IdEq(id, "usp_s"))
                    SetRange(weapon, 2.4f, 0.045f, 0.11f, 0f);
                else if (IdEq(id, "deagle"))
                    SetRange(weapon, 2.0f, 0.07f, 0.14f, 0f);
                else if (IdEq(id, "famas"))
                    SetRange(weapon, 2.0f, 0.08f, 0.16f, 0f);
                else if (IdEq(id, "Galil_AR"))
                    SetRange(weapon, 1.9f, 0.08f, 0.16f, 0f);
                else if (IdEq(id, "m4a4"))
                    SetRange(weapon, 1.7f, 0.08f, 0.16f, 0f);
                else if (IdEq(id, "ak47"))
                    SetRange(weapon, 1.6f, 0.09f, 0.17f, 0f);
                else if (IdEq(id, "m4a1s"))
                    SetRange(weapon, 1.5f, 0.09f, 0.17f, 0f);
                else if (IdEq(id, "ssg08"))
                    SetRange(weapon, 0.55f, 0.12f, 0.24f, 2.8f);
                else if (IdEq(id, "awp") || sniper)
                    SetRange(weapon, 0.35f, 0.14f, 0.28f, 3.5f);
                else if (smg)
                    SetRange(weapon, 5.0f, 0.05f, 0.12f, 0f);
                else if (pistol)
                    SetRange(weapon, 2.6f, 0.045f, 0.11f, 0f);
                else
                    SetRange(weapon, 1.8f, 0.08f, 0.16f, 0f);
            }

            if (sniper && weapon.closeFalloff <= 0f)
                weapon.closeFalloff = string.Equals(id, "awp", StringComparison.OrdinalIgnoreCase) ? 3.5f : 2.8f;

            if (sniper)
            {
                weapon.requiresScope = true;
                if (weapon.hipFireRange <= 0f)
                    weapon.hipFireRange = weapon.optimalRange;
                if (weapon.hipFireAccuracy <= 0f)
                    weapon.hipFireAccuracy = string.Equals(id, "awp", StringComparison.OrdinalIgnoreCase)
                        ? 0.86f
                        : 0.90f;
            }
        }

        public static bool RequiresScope(WeaponDefinition weapon)
        {
            if (weapon == null)
                return false;
            if (weapon.requiresScope)
                return true;
            string id = weapon.id ?? string.Empty;
            string cat = weapon.category ?? string.Empty;
            return cat.IndexOf("sniper", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   string.Equals(id, "awp", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(id, "ssg08", StringComparison.OrdinalIgnoreCase);
        }

        public static bool ShouldHipFire(WeaponDefinition weapon, float distanceUv)
        {
            if (!RequiresScope(weapon))
                return false;
            EnsureRangeStats(weapon);
            float limit = weapon.hipFireRange > 0f ? weapon.hipFireRange : weapon.optimalRange;
            return distanceUv < limit;
        }

        public static float HipFireAccuracy(WeaponDefinition weapon)
        {
            if (weapon == null)
                return 0.85f;
            EnsureRangeStats(weapon);
            if (weapon.hipFireAccuracy > 0f)
                return Clamp(weapon.hipFireAccuracy, 0.55f, 0.98f);
            return string.Equals(weapon.id, "awp", StringComparison.OrdinalIgnoreCase) ? 0.86f : 0.90f;
        }

        private static void SetRange(
            WeaponDefinition weapon, float falloff, float optimal, float maxEngage, float close)
        {
            if (weapon.rangeFalloff <= 0f)
                weapon.rangeFalloff = falloff;
            if (weapon.optimalRange <= 0f)
                weapon.optimalRange = optimal;
            if (weapon.maxEngageRange <= 0f)
                weapon.maxEngageRange = maxEngage;
            if (close > 0f && weapon.closeFalloff <= 0f)
                weapon.closeFalloff = close;
        }

        public static void FaceToward(PlayerRuntime shooter, PlayerRuntime target, MapDefinition map)
        {
            if (shooter == null || target == null)
                return;
            MapUv(map, shooter, out float sx, out float sy);
            MapUv(map, target, out float tx, out float ty);
            float dx = tx - sx;
            float dy = ty - sy;
            if (dx * dx + dy * dy < 1e-10f)
                return;
            shooter.FacingDeg = (float)(Math.Atan2(dy, dx) * 180.0 / Math.PI);
        }

        public static void FaceTowardZone(PlayerRuntime shooter, string zoneId, MapDefinition map)
        {
            if (shooter == null || string.IsNullOrEmpty(zoneId))
                return;
            MapUv(map, shooter, out float sx, out float sy);
            ZoneUv(map, zoneId, shooter.Definition?.id, out float tx, out float ty);
            float dx = tx - sx;
            float dy = ty - sy;
            if (dx * dx + dy * dy < 1e-10f)
                return;
            shooter.FacingDeg = (float)(Math.Atan2(dy, dx) * 180.0 / Math.PI);
        }

        public static float DistanceUv(MapDefinition map, PlayerRuntime a, PlayerRuntime b)
        {
            MapUv(map, a, out float ax, out float ay);
            MapUv(map, b, out float bx, out float by);
            float dx = bx - ax;
            float dy = by - ay;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public static bool InViewCone(MapDefinition map, PlayerRuntime shooter, PlayerRuntime target)
        {
            if (shooter == null || target == null)
                return false;
            MapUv(map, shooter, out float sx, out float sy);
            MapUv(map, target, out float tx, out float ty);
            return InViewCone(shooter.FacingDeg, tx - sx, ty - sy);
        }

        public static bool InViewCone(float facingDeg, float dx, float dy)
        {
            if (dx * dx + dy * dy < 1e-8f)
                return true;
            float targetDeg = (float)(Math.Atan2(dy, dx) * 180.0 / Math.PI);
            return Math.Abs(DeltaAngle(facingDeg, targetDeg)) <= ViewConeDegrees * 0.5f;
        }

        /// <summary>
        /// Overview collision LOS. Null / not ready fail-open (same as pre-LOS combat).
        /// </summary>
        public static bool HasLineOfSight(
            MapDefinition map, PlayerRuntime shooter, PlayerRuntime target, ICombatLosQuery los)
        {
            if (los == null || !los.IsReady)
                return true;
            if (shooter == null || target == null)
                return false;
            MapUv(map, shooter, out float sx, out float sy);
            MapUv(map, target, out float tx, out float ty);
            return los.HasClearLineUv(sx, sy, tx, ty);
        }

        public static float DeltaAngle(float fromDeg, float toDeg)
        {
            float d = (toDeg - fromDeg) % 360f;
            if (d > 180f)
                d -= 360f;
            if (d < -180f)
                d += 360f;
            return d;
        }

        public static ShotAccuracy Evaluate(
            WeaponDefinition weapon, float aim, float focusAllMult, float distanceUv)
        {
            if (weapon == null)
                return new ShotAccuracy(0f, 0f, 1f, false, 0f);

            EnsureRangeStats(weapon);
            float dist = Math.Max(0f, distanceUv);
            float aimFactor = Clamp(0.38f + aim * 0.0062f, 0.35f, 0.96f);
            float rangeFactor = RangeFactor(weapon, dist);
            bool hipFire = ShouldHipFire(weapon, dist);
            float stanceFactor = hipFire ? HipFireAccuracy(weapon) : 1f;
            float focus = focusAllMult <= 0f ? 1f : focusAllMult;
            float hit = Clamp(aimFactor * rangeFactor * stanceFactor * focus, 0.04f, 0.93f);
            return new ShotAccuracy(aimFactor, rangeFactor, stanceFactor, hipFire, hit);
        }

        public static float HitChance(
            WeaponDefinition weapon, float aim, float focusAllMult, float distanceUv) =>
            Evaluate(weapon, aim, focusAllMult, distanceUv).HitChance;

        public static float RangeFactor(WeaponDefinition weapon, float distanceUv)
        {
            if (weapon == null)
                return 0f;
            EnsureRangeStats(weapon);
            float over = Math.Max(0f, distanceUv - weapon.optimalRange);
            return (float)Math.Exp(-weapon.rangeFalloff * over);
        }

        public static void MapUv(MapDefinition map, PlayerRuntime player, out float x, out float y)
        {
            ZoneUv(map, player?.ZoneId, player?.Definition?.id, out x, out y);
        }

        public static void ZoneUv(MapDefinition map, string zoneId, string playerId, out float x, out float y)
        {
            x = 0.5f;
            y = 0.5f;
            MapZoneDefinition zone = FindZone(map, zoneId);
            if (zone != null)
            {
                x = zone.x;
                y = zone.y;
            }

            int hash = StableHash(playerId);
            x += ((hash & 0xFF) / 255f - 0.5f) * 0.03f;
            y += (((hash >> 8) & 0xFF) / 255f - 0.5f) * 0.03f;
        }

        private static bool IdEq(string id, string expected) =>
            string.Equals(id, expected, StringComparison.OrdinalIgnoreCase);

        private static MapZoneDefinition FindZone(MapDefinition map, string zoneId)
        {
            if (map?.zones == null || string.IsNullOrEmpty(zoneId))
                return null;
            for (int i = 0; i < map.zones.Length; i++)
            {
                MapZoneDefinition zone = map.zones[i];
                if (zone != null &&
                    string.Equals(zone.id, zoneId, StringComparison.OrdinalIgnoreCase))
                    return zone;
            }
            return null;
        }

        private static int StableHash(string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;
            unchecked
            {
                int hash = 23;
                for (int i = 0; i < value.Length; i++)
                    hash = hash * 31 + value[i];
                return hash;
            }
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }
}
