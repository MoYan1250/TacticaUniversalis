using System;
using System.Collections.Generic;
using System.Linq;

namespace TacticaUniversalis.Domain
{
    [Serializable]
    public sealed class UtilityLineupEntry
    {
        public string mapId;
        public string fromZoneId;
        public string toZoneId;
        public string[] kinds;
        /// <summary>t / ct / any</summary>
        public string side;
        public int priority;
        public string tag;
    }

    public readonly struct UtilityLineupCandidate
    {
        public readonly UtilityLineupEntry Entry;
        public readonly UtilityKind Kind;
        public readonly int Score;
        public readonly bool ExactStand;

        public UtilityLineupCandidate(
            UtilityLineupEntry entry, UtilityKind kind, int score, bool exactStand)
        {
            Entry = entry;
            Kind = kind;
            Score = score;
            ExactStand = exactStand;
        }
    }

    /// <summary>Pro-style long-range utility lineups (fromZone → far toZone).</summary>
    public static class UtilityLineupRules
    {
        public const int NeighborStandPriorityPenalty = 10;

        private static UtilityLineupCatalog _shared;

        public static UtilityLineupCatalog Shared =>
            _shared ??= UtilityLineupCatalog.FromEntries(CreateDefaultDust2Entries());

        public static void SetShared(UtilityLineupCatalog catalog) =>
            _shared = catalog ?? UtilityLineupCatalog.FromEntries(CreateDefaultDust2Entries());

        public static IReadOnlyList<UtilityLineupEntry> CreateDefaultDust2Entries() =>
            new[]
            {
                E("dust2", "outside_long", "a_long", new[] { "smoke", "flash" }, "t", 90, "outside_long_a_long"),
                E("dust2", "long_doors", "a_long", new[] { "flash" }, "t", 85, "long_doors_flash"),
                E("dust2", "top_mid", "xbox", new[] { "smoke" }, "t", 95, "xbox_smoke"),
                E("dust2", "mid", "ct_mid", new[] { "smoke" }, "t", 88, "mid_ct_smoke"),
                E("dust2", "mid_doors", "ct_mid", new[] { "smoke", "flash" }, "t", 92, "mid_doors_ct"),
                E("dust2", "t_spawn", "mid_doors", new[] { "smoke" }, "t", 80, "spawn_mid_doors"),
                E("dust2", "b_tunnels", "b_doors", new[] { "smoke" }, "t", 90, "tunnels_b_doors"),
                E("dust2", "lower_tunnels", "b_site", new[] { "smoke", "flash" }, "t", 88, "lower_b_site"),
                E("dust2", "b_tunnels", "b_site", new[] { "fire", "flash" }, "t", 86, "tunnels_b_exec"),
                E("dust2", "cat", "a_site", new[] { "smoke", "flash" }, "t", 90, "cat_a_site"),
                E("dust2", "a_short", "a_site", new[] { "flash", "fire" }, "t", 87, "short_a_exec"),
                E("dust2", "ct_spawn", "mid_doors", new[] { "smoke" }, "ct", 90, "ct_mid_doors"),
                E("dust2", "a_site", "a_short", new[] { "flash" }, "ct", 85, "a_retake_short"),
                E("dust2", "a_site", "a_long", new[] { "smoke" }, "ct", 84, "a_retake_long"),
                E("dust2", "b_site", "b_doors", new[] { "flash", "fire" }, "ct", 88, "b_hold_doors"),
                E("dust2", "pit", "a_long", new[] { "flash" }, "any", 75, "pit_long_flash")
            };

        private static UtilityLineupEntry E(
            string mapId, string from, string to, string[] kinds, string side, int priority, string tag) =>
            new UtilityLineupEntry
            {
                mapId = mapId,
                fromZoneId = from,
                toZoneId = to,
                kinds = kinds,
                side = side,
                priority = priority,
                tag = tag
            };

        public static bool SideMatches(string entrySide, Side teamSide)
        {
            if (string.IsNullOrEmpty(entrySide) ||
                string.Equals(entrySide, "any", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(entrySide, "t", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(entrySide, "terrorist", StringComparison.OrdinalIgnoreCase))
                return teamSide == Side.Terrorist;
            if (string.Equals(entrySide, "ct", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(entrySide, "counterterrorist", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(entrySide, "counter-terrorist", StringComparison.OrdinalIgnoreCase))
                return teamSide == Side.CounterTerrorist;
            return true;
        }

        public static bool TryParseKind(string raw, out UtilityKind kind) =>
            UtilityCombatRules.TryParseWeaponId(raw, out kind);

        public static List<UtilityLineupCandidate> FindCandidates(
            UtilityLineupCatalog catalog,
            string mapId,
            string standZoneId,
            Side teamSide,
            MapZoneGraph graph,
            UtilityKind? onlyKind = null)
        {
            var results = new List<UtilityLineupCandidate>();
            if (catalog == null || string.IsNullOrEmpty(standZoneId))
                return results;

            foreach (UtilityLineupEntry entry in catalog.Entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.fromZoneId) || string.IsNullOrEmpty(entry.toZoneId))
                    continue;
                if (!string.IsNullOrEmpty(mapId) &&
                    !string.Equals(entry.mapId, mapId, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!SideMatches(entry.side, teamSide))
                    continue;

                bool exact = string.Equals(
                    standZoneId, entry.fromZoneId, StringComparison.OrdinalIgnoreCase);
                bool neighbor = !exact && graph != null && IsNeighbor(graph, standZoneId, entry.fromZoneId);
                if (!exact && !neighbor)
                    continue;

                if (entry.kinds == null)
                    continue;
                for (int i = 0; i < entry.kinds.Length; i++)
                {
                    if (!TryParseKind(entry.kinds[i], out UtilityKind kind))
                        continue;
                    if (onlyKind.HasValue && kind != onlyKind.Value)
                        continue;
                    int score = entry.priority - (exact ? 0 : NeighborStandPriorityPenalty);
                    results.Add(new UtilityLineupCandidate(entry, kind, score, exact));
                }
            }

            results.Sort((a, b) => b.Score.CompareTo(a.Score));
            return results;
        }

        private static bool IsNeighbor(MapZoneGraph graph, string a, string b)
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
    }

    public sealed class UtilityLineupCatalog
    {
        public IReadOnlyList<UtilityLineupEntry> Entries { get; }

        private UtilityLineupCatalog(IReadOnlyList<UtilityLineupEntry> entries) =>
            Entries = entries ?? Array.Empty<UtilityLineupEntry>();

        public static UtilityLineupCatalog FromEntries(IEnumerable<UtilityLineupEntry> entries)
        {
            var list = (entries ?? Enumerable.Empty<UtilityLineupEntry>())
                .Where(e => e != null &&
                            !string.IsNullOrEmpty(e.fromZoneId) &&
                            !string.IsNullOrEmpty(e.toZoneId))
                .ToList();
            return new UtilityLineupCatalog(list);
        }

        public int CountForMap(string mapId) =>
            Entries.Count(e => string.Equals(e.mapId, mapId, StringComparison.OrdinalIgnoreCase));
    }
}
