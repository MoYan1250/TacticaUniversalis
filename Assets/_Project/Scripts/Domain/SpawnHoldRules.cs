using System;
using System.Collections.Generic;

namespace TacticaUniversalis.Domain
{
    /// <summary>Keep personal holds off raw spawn tiles for both sides.</summary>
    public static class SpawnHoldRules
    {
        public static bool IsSpawnZone(string zoneId) =>
            string.Equals(zoneId, "t_spawn", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(zoneId, "ct_spawn", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// If <paramref name="zone"/> is a spawn, pick the first non-spawn entry from
        /// <paramref name="pool"/>, else <paramref name="primary"/>, else <paramref name="plantFallback"/>.
        /// </summary>
        public static string RemapAwayFromSpawn(
            string zone,
            IEnumerable<string> pool,
            string primary,
            string plantFallback)
        {
            if (!IsSpawnZone(zone))
                return zone;

            if (pool != null)
            {
                foreach (string candidate in pool)
                {
                    if (string.IsNullOrEmpty(candidate) || IsSpawnZone(candidate))
                        continue;
                    return candidate;
                }
            }

            if (!string.IsNullOrEmpty(primary) && !IsSpawnZone(primary))
                return primary;
            if (!string.IsNullOrEmpty(plantFallback) && !IsSpawnZone(plantFallback))
                return plantFallback;
            return zone;
        }
    }
}
