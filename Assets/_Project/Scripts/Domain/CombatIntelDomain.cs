using System;
using System.Collections.Generic;

namespace TacticaUniversalis.Domain
{
    public sealed class IntelNote
    {
        public string ZoneId;
        public int Count;
        public string[] WeaponIds = Array.Empty<string>();
        public float SeenAt;
        public string ReporterId;
        /// <summary>Acoustic / action tags: planting, footsteps, defusing.</summary>
        public string[] Tags = Array.Empty<string>();
    }

    public sealed class IntelBoard
    {
        public Dictionary<string, IntelNote> ByZone { get; } =
            new Dictionary<string, IntelNote>(StringComparer.OrdinalIgnoreCase);

        public void Upsert(IntelNote note)
        {
            if (note == null || string.IsNullOrEmpty(note.ZoneId))
                return;
            ByZone[note.ZoneId] = note;
        }

        public bool TryGet(string zoneId, out IntelNote note)
        {
            note = null;
            if (string.IsNullOrEmpty(zoneId))
                return false;
            return ByZone.TryGetValue(zoneId, out note);
        }

        public void Expire(float time, float ttl)
        {
            if (ByZone.Count == 0)
                return;
            List<string> stale = null;
            foreach (KeyValuePair<string, IntelNote> pair in ByZone)
            {
                if (IntelRules.IsFresh(pair.Value, time, ttl))
                    continue;
                if (stale == null)
                    stale = new List<string>();
                stale.Add(pair.Key);
            }

            if (stale == null)
                return;
            for (int i = 0; i < stale.Count; i++)
                ByZone.Remove(stale[i]);
        }
    }

    /// <summary>View-cone memory and callout helpers for general mode.</summary>
    public static class IntelRules
    {
        public static float SpotRange(float awareness)
        {
            float a = Clamp01_100(awareness);
            return 0.20f + a * 0.0005f;
        }

        public static float TtlSeconds(float awareness)
        {
            float a = Clamp01_100(awareness);
            return 8f + a * 0.04f;
        }

        public static float CalloutCooldown(float communication)
        {
            float c = Clamp01_100(communication);
            return Math.Max(0.8f, 3.2f - c * 0.024f);
        }

        public static float CalloutChance(float communication)
        {
            float c = Clamp01_100(communication);
            return Clamp(0.40f + c * 0.005f, 0.35f, 0.95f);
        }

        public static bool IsFresh(IntelNote note, float time, float ttl)
        {
            if (note == null)
                return false;
            return time - note.SeenAt <= ttl;
        }

        public static bool CanSee(
            MapDefinition map, PlayerRuntime observer, PlayerRuntime enemy, float awareness,
            ICombatLosQuery los = null,
            MapZoneGraph graph = null,
            IReadOnlyList<ActiveUtilityEffect> utilities = null,
            float time = 0f)
        {
            if (observer == null || enemy == null || !enemy.Alive)
                return false;
            if (UtilityCombatRules.IsBlinded(observer, time))
                return false;
            if (!CombatSightRules.InViewCone(map, observer, enemy))
                return false;
            if (!CombatSightRules.HasLineOfSight(map, observer, enemy, los))
                return false;
            if (UtilityCombatRules.BlocksSight(map, graph, observer, enemy, utilities))
                return false;
            if (string.Equals(observer.ZoneId, enemy.ZoneId, StringComparison.OrdinalIgnoreCase))
                return true;
            float dist = CombatSightRules.DistanceUv(map, observer, enemy);
            return dist <= SpotRange(awareness);
        }

        public static List<IntelNote> ScanVisible(
            MapDefinition map,
            PlayerRuntime observer,
            IReadOnlyList<PlayerRuntime> enemies,
            WeaponCatalog catalog,
            float time,
            ICombatLosQuery los = null,
            MapZoneGraph graph = null,
            IReadOnlyList<ActiveUtilityEffect> utilities = null)
        {
            var notes = new List<IntelNote>();
            if (observer == null || enemies == null || enemies.Count == 0)
                return notes;

            float awareness = observer.Definition.attributes.awareness + observer.FocusAwareness;
            var byZone = new Dictionary<string, List<PlayerRuntime>>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < enemies.Count; i++)
            {
                PlayerRuntime enemy = enemies[i];
                if (!CanSee(map, observer, enemy, awareness, los, graph, utilities, time))
                    continue;
                string zone = enemy.ZoneId ?? string.Empty;
                if (!byZone.TryGetValue(zone, out List<PlayerRuntime> list))
                {
                    list = new List<PlayerRuntime>();
                    byZone[zone] = list;
                }

                list.Add(enemy);
            }

            foreach (KeyValuePair<string, List<PlayerRuntime>> pair in byZone)
            {
                notes.Add(new IntelNote
                {
                    ZoneId = pair.Key,
                    Count = pair.Value.Count,
                    WeaponIds = RankWeapons(pair.Value, catalog),
                    SeenAt = time,
                    ReporterId = observer.Definition?.id
                });
            }

            return notes;
        }

        public static bool IsNewOrChanged(IntelNote previous, IntelNote current)
        {
            if (current == null)
                return false;
            if (previous == null)
                return true;
            if (previous.Count != current.Count)
                return true;
            return !SameWeapons(previous.WeaponIds, current.WeaponIds);
        }

        public static string BestFocusZone(
            IntelBoard board, string fromZone, MapZoneGraph graph, float time, float ttl)
        {
            if (board == null || board.ByZone.Count == 0)
                return null;

            string best = null;
            float bestScore = float.MinValue;
            foreach (IntelNote note in board.ByZone.Values)
            {
                if (!IsFresh(note, time, ttl) || string.IsNullOrEmpty(note.ZoneId))
                    continue;
                float hops = 1f;
                if (graph != null && !string.IsNullOrEmpty(fromZone))
                {
                    IReadOnlyList<string> path = graph.FindPath(fromZone, note.ZoneId);
                    hops = path == null || path.Count == 0 ? 8f : path.Count;
                }

                float score = note.Count * 3f - hops;
                if (HasSniper(note.WeaponIds))
                    score += 2f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = note.ZoneId;
                }
            }

            return best;
        }

        public static int FreshCountOnSite(IntelBoard board, string siteKey, float time, float ttl)
        {
            if (board == null || string.IsNullOrEmpty(siteKey))
                return 0;
            int total = 0;
            foreach (IntelNote note in board.ByZone.Values)
            {
                if (!IsFresh(note, time, ttl))
                    continue;
                if (string.Equals(MapZones.ResolvePlantSite(note.ZoneId), siteKey, StringComparison.OrdinalIgnoreCase))
                    total += note.Count;
            }

            return total;
        }

        public static string FormatCallout(IntelNote note, WeaponCatalog catalog)
        {
            int count = note?.Count ?? 0;
            string zone = note?.ZoneId ?? string.Empty;
            var labels = new List<string>();
            if (note?.WeaponIds != null)
            {
                for (int i = 0; i < note.WeaponIds.Length; i++)
                {
                    string id = note.WeaponIds[i];
                    if (string.IsNullOrEmpty(id))
                        continue;
                    string label = id;
                    if (catalog != null && catalog.TryGet(id, out WeaponDefinition weapon) &&
                        weapon != null && !string.IsNullOrEmpty(weapon.displayName))
                        label = weapon.displayName;
                    labels.Add(label);
                }
            }

            return $"callout|{count}|{zone}|{string.Join(",", labels)}";
        }

        private static string[] RankWeapons(List<PlayerRuntime> enemies, WeaponCatalog catalog)
        {
            var ranked = new List<KeyValuePair<int, string>>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < enemies.Count; i++)
            {
                string id = WeaponAmmoRules.ActiveWeaponId(enemies[i]);
                if (string.IsNullOrEmpty(id) || !seen.Add(id))
                    continue;
                ranked.Add(new KeyValuePair<int, string>(WeaponRank(id, catalog), id));
            }

            ranked.Sort((a, b) => a.Key.CompareTo(b.Key));
            int take = Math.Min(3, ranked.Count);
            var ids = new string[take];
            for (int i = 0; i < take; i++)
                ids[i] = ranked[i].Value;
            return ids;
        }

        private static int WeaponRank(string weaponId, WeaponCatalog catalog)
        {
            WeaponDefinition weapon = catalog != null ? catalog.GetOrKnife(weaponId) : null;
            string id = weapon?.id ?? weaponId ?? string.Empty;
            string cat = weapon?.category ?? string.Empty;
            if (cat.IndexOf("sniper", StringComparison.OrdinalIgnoreCase) >= 0 ||
                string.Equals(id, "awp", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(id, "ssg08", StringComparison.OrdinalIgnoreCase))
                return 0;
            if (cat.IndexOf("rifle", StringComparison.OrdinalIgnoreCase) >= 0)
                return 1;
            if (cat.IndexOf("smg", StringComparison.OrdinalIgnoreCase) >= 0)
                return 2;
            if (string.Equals(id, "deagle", StringComparison.OrdinalIgnoreCase))
                return 3;
            if (cat.IndexOf("pistol", StringComparison.OrdinalIgnoreCase) >= 0)
                return 4;
            return 5;
        }

        private static bool HasSniper(string[] weaponIds)
        {
            if (weaponIds == null)
                return false;
            for (int i = 0; i < weaponIds.Length; i++)
            {
                string id = weaponIds[i];
                if (string.Equals(id, "awp", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(id, "ssg08", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static bool SameWeapons(string[] a, string[] b)
        {
            if (a == null && b == null)
                return true;
            if (a == null || b == null || a.Length != b.Length)
                return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (!string.Equals(a[i], b[i], StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        private static float Clamp01_100(float value)
        {
            if (value < 0f)
                return 0f;
            if (value > 100f)
                return 100f;
            return value;
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
