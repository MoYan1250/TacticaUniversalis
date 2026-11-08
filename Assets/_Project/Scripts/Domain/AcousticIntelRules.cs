using System;
using System.Collections.Generic;

namespace TacticaUniversalis.Domain
{
    /// <summary>
    /// Footstep / plant / defuse audio awareness for general-mode BT rounds.
    /// Same or adjacent zones can be heard; success chance scales with awareness.
    /// </summary>
    public static class AcousticIntelRules
    {
        public const string TagPlanting = "planting";
        public const string TagFootsteps = "footsteps";
        public const string TagDefusing = "defusing";
        public const float AcousticTtlSeconds = 4.5f;

        public static float HearChance(float awareness, bool sameZone)
        {
            float a = awareness < 0f ? 0f : awareness > 100f ? 100f : awareness;
            float baseChance = sameZone ? 0.72f : 0.38f;
            return Clamp(baseChance + a * 0.004f, 0.20f, 0.95f);
        }

        public static bool AreAcousticNeighbors(MapZoneGraph graph, string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
                return false;
            if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
                return true;
            if (graph == null)
                return false;
            foreach (string n in graph.Neighbors(a))
            {
                if (string.Equals(n, b, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public static void UpsertTagged(
            IntelBoard board, string zoneId, string tag, float time, string reporterId, int count = 1)
        {
            if (board == null || string.IsNullOrEmpty(zoneId) || string.IsNullOrEmpty(tag))
                return;

            board.TryGet(zoneId, out IntelNote prev);
            var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (prev?.Tags != null)
            {
                for (int i = 0; i < prev.Tags.Length; i++)
                {
                    if (!string.IsNullOrEmpty(prev.Tags[i]))
                        tags.Add(prev.Tags[i]);
                }
            }

            tags.Add(tag);
            var arr = new string[tags.Count];
            tags.CopyTo(arr);
            board.Upsert(new IntelNote
            {
                ZoneId = zoneId,
                Count = Math.Max(count, prev?.Count ?? 0),
                WeaponIds = prev?.WeaponIds ?? Array.Empty<string>(),
                SeenAt = time,
                ReporterId = reporterId ?? prev?.ReporterId,
                Tags = arr
            });
        }

        public static bool BoardHasFreshTag(IntelBoard board, string tag, float time, float ttl)
        {
            if (board == null || string.IsNullOrEmpty(tag))
                return false;
            foreach (IntelNote note in board.ByZone.Values)
            {
                if (!IntelRules.IsFresh(note, time, ttl) || note.Tags == null)
                    continue;
                for (int i = 0; i < note.Tags.Length; i++)
                {
                    if (string.Equals(note.Tags[i], tag, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }

        public static string FreshTagZone(IntelBoard board, string tag, float time, float ttl)
        {
            if (board == null || string.IsNullOrEmpty(tag))
                return null;
            foreach (KeyValuePair<string, IntelNote> pair in board.ByZone)
            {
                IntelNote note = pair.Value;
                if (!IntelRules.IsFresh(note, time, ttl) || note.Tags == null)
                    continue;
                for (int i = 0; i < note.Tags.Length; i++)
                {
                    if (string.Equals(note.Tags[i], tag, StringComparison.OrdinalIgnoreCase))
                        return note.ZoneId;
                }
            }

            return null;
        }

        /// <summary>
        /// Hear enemy footsteps / active plant or defuse in the same or adjacent zone.
        /// </summary>
        public static void ScanAudible(
            MapZoneGraph graph,
            PlayerRuntime listener,
            IEnumerable<PlayerRuntime> enemies,
            IntelBoard personal,
            IntelBoard team,
            float time,
            IRandomSource random,
            string plantingZoneId,
            string planterId,
            string defuserId,
            string defuseZoneId)
        {
            if (listener == null || !listener.Alive || enemies == null)
                return;

            float awareness = listener.Definition.attributes.awareness + listener.FocusAwareness;
            string listenerZone = listener.ZoneId;

            // Active plant / defuse is louder than quiet idle.
            if (!string.IsNullOrEmpty(plantingZoneId) &&
                AreAcousticNeighbors(graph, listenerZone, plantingZoneId))
            {
                bool same = string.Equals(listenerZone, plantingZoneId, StringComparison.OrdinalIgnoreCase);
                if (random != null && random.Next01() < HearChance(awareness, same) + 0.12f)
                {
                    UpsertTagged(personal, plantingZoneId, TagPlanting, time, listener.Definition.id);
                    UpsertTagged(team, plantingZoneId, TagPlanting, time, listener.Definition.id);
                }
            }

            if (!string.IsNullOrEmpty(defuserId) && !string.IsNullOrEmpty(defuseZoneId) &&
                AreAcousticNeighbors(graph, listenerZone, defuseZoneId))
            {
                bool same = string.Equals(listenerZone, defuseZoneId, StringComparison.OrdinalIgnoreCase);
                if (random != null && random.Next01() < HearChance(awareness, same) + 0.10f)
                {
                    UpsertTagged(personal, defuseZoneId, TagDefusing, time, listener.Definition.id);
                    UpsertTagged(team, defuseZoneId, TagDefusing, time, listener.Definition.id);
                }
            }

            foreach (PlayerRuntime enemy in enemies)
            {
                if (enemy == null || !enemy.Alive)
                    continue;
                if (!AreAcousticNeighbors(graph, listenerZone, enemy.ZoneId))
                    continue;
                // Planter standing still is quieter; treat all alive adjacent as potential footsteps.
                bool same = string.Equals(listenerZone, enemy.ZoneId, StringComparison.OrdinalIgnoreCase);
                if (random == null || random.Next01() > HearChance(awareness, same))
                    continue;
                UpsertTagged(personal, enemy.ZoneId, TagFootsteps, time, listener.Definition.id);
                UpsertTagged(team, enemy.ZoneId, TagFootsteps, time, listener.Definition.id);
            }
        }

        public static bool HearsThreatNear(
            MapZoneGraph graph,
            string selfZone,
            IEnumerable<PlayerRuntime> enemies,
            IntelBoard personal,
            float time)
        {
            if (enemies != null)
            {
                foreach (PlayerRuntime e in enemies)
                {
                    if (e == null || !e.Alive)
                        continue;
                    if (AreAcousticNeighbors(graph, selfZone, e.ZoneId))
                        return true;
                }
            }

            return BoardHasFreshTag(personal, TagFootsteps, time, AcousticTtlSeconds);
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
