using System;
using System.Collections.Generic;
using System.Linq;

namespace TacticaUniversalis.Domain
{
    public enum CompetitionScheduleVerdict
    {
        Allowed = 0,
        /// <summary>Two LAN/Hybrid events overlap — hard block.</summary>
        BlockedDualPhysical = 1,
        /// <summary>New Online signup overlaps existing; UI must confirm.</summary>
        NeedsOnlineOverlapConfirm = 2,
        /// <summary>Registering would create 3+ concurrent events; pick exactly two.</summary>
        NeedsChooseTwo = 3,
        /// <summary>Choose-two required but a locked seat cannot be dropped.</summary>
        BlockedChooseTwoLocked = 4
    }

    public sealed class CompetitionCommitment
    {
        public string competitionId;
        public string displayName;
        public CompetitionFormat format;
        public int startHour;
        public int endHour;
        /// <summary>RMR Major seats etc. cannot be voluntarily cancelled.</summary>
        public bool isLockedSeat;
    }

    public sealed class CompetitionScheduleEvaluation
    {
        public CompetitionScheduleVerdict verdict = CompetitionScheduleVerdict.Allowed;
        public List<string> conflictCompetitionIds = new List<string>();
        public List<string> conflictDisplayNames = new List<string>();
        public string message;
    }

    public sealed class CompetitionRegisterOptions
    {
        public bool confirmOnlineOverlap;
        public string[] keepCompetitionIds;
    }

    public sealed class RegisterCompetitionResult
    {
        public bool success;
        public CompetitionScheduleVerdict scheduleVerdict;
        public string failureReason;
        public List<string> conflictCompetitionIds = new List<string>();
        public List<string> conflictDisplayNames = new List<string>();
    }

    /// <summary>
    /// Concurrent competition schedule: max two at once, never two physical overlapping,
    /// Online overlaps require player confirm for energy cost.
    /// </summary>
    public static class CompetitionScheduleRules
    {
        public const int MaxConcurrentCommitments = 2;

        public static bool IsPhysical(CompetitionFormat format) =>
            format == CompetitionFormat.LAN || format == CompetitionFormat.Hybrid;

        public static bool IsPhysical(CompetitionDefinition competition) =>
            competition != null && IsPhysical(competition.format);

        /// <summary>Inclusive end hour, matching <see cref="CareerCalendar.IsLive"/>.</summary>
        public static bool IntervalsOverlap(int aStart, int aEnd, int bStart, int bEnd)
        {
            if (aEnd < aStart || bEnd < bStart)
                return false;
            return aStart <= bEnd && bStart <= aEnd;
        }

        public static bool CommitmentsOverlap(CompetitionCommitment a, CompetitionCommitment b)
        {
            if (a == null || b == null)
                return false;
            return IntervalsOverlap(a.startHour, a.endHour, b.startHour, b.endHour);
        }

        public static int CountOverlappingWith(
            CompetitionCommitment candidate,
            IEnumerable<CompetitionCommitment> existing)
        {
            if (candidate == null || existing == null)
                return 0;
            int count = 0;
            foreach (CompetitionCommitment other in existing)
            {
                if (other == null)
                    continue;
                if (string.Equals(other.competitionId, candidate.competitionId, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (CommitmentsOverlap(candidate, other))
                    count++;
            }

            return count;
        }

        public static CompetitionScheduleEvaluation EvaluatePlayerSignup(
            CompetitionCommitment candidate,
            IReadOnlyList<CompetitionCommitment> existing,
            bool confirmOnlineOverlap,
            IReadOnlyList<string> keepCompetitionIds)
        {
            var evaluation = new CompetitionScheduleEvaluation();
            if (candidate == null)
            {
                evaluation.verdict = CompetitionScheduleVerdict.BlockedDualPhysical;
                evaluation.message = "找不到賽事";
                return evaluation;
            }

            List<CompetitionCommitment> current = (existing ?? Array.Empty<CompetitionCommitment>())
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.competitionId))
                .Where(x => !string.Equals(x.competitionId, candidate.competitionId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var projected = new List<CompetitionCommitment>(current) { candidate };

            CompetitionCommitment dualPhysical = FindDualPhysicalOverlap(projected);
            if (dualPhysical != null)
            {
                evaluation.verdict = CompetitionScheduleVerdict.BlockedDualPhysical;
                FillConflict(evaluation, projected.Where(x =>
                    IsPhysical(x.format) && CommitmentsOverlap(x, dualPhysical)).ToList());
                evaluation.message = "無法同時參加兩場線下或混合賽事";
                return evaluation;
            }

            List<CompetitionCommitment> overCapCluster = FindOverCapacityCluster(projected);
            if (overCapCluster != null && overCapCluster.Count > MaxConcurrentCommitments)
            {
                FillConflict(evaluation, overCapCluster);
                if (keepCompetitionIds != null && keepCompetitionIds.Count > 0)
                {
                    if (!TryValidateKeepTwo(overCapCluster, keepCompetitionIds, out string keepFailure))
                    {
                        evaluation.verdict = string.Equals(keepFailure, "locked", StringComparison.Ordinal)
                            ? CompetitionScheduleVerdict.BlockedChooseTwoLocked
                            : CompetitionScheduleVerdict.NeedsChooseTwo;
                        evaluation.message = string.Equals(keepFailure, "locked", StringComparison.Ordinal)
                            ? "衝突賽事中有不可取消的席位，無法完成報名"
                            : "請從衝突賽事中選出兩場保留";
                        return evaluation;
                    }

                    // Keep set is valid; caller drops the rest then registers.
                    evaluation.verdict = CompetitionScheduleVerdict.Allowed;
                    evaluation.message = null;
                    return evaluation;
                }

                if (overCapCluster.Any(x => x.isLockedSeat) &&
                    overCapCluster.Count(x => x.isLockedSeat) > MaxConcurrentCommitments)
                {
                    evaluation.verdict = CompetitionScheduleVerdict.BlockedChooseTwoLocked;
                    evaluation.message = "衝突賽事中有不可取消的席位，無法完成報名";
                    return evaluation;
                }

                evaluation.verdict = CompetitionScheduleVerdict.NeedsChooseTwo;
                evaluation.message = "同時間最多只能參加兩場賽事，請選出要保留的兩場";
                return evaluation;
            }

            bool onlineOverlaps = candidate.format == CompetitionFormat.Online &&
                                  current.Any(x => CommitmentsOverlap(candidate, x));
            if (onlineOverlaps && !confirmOnlineOverlap)
            {
                FillConflict(evaluation, current.Where(x => CommitmentsOverlap(candidate, x))
                    .Concat(new[] { candidate }).ToList());
                evaluation.verdict = CompetitionScheduleVerdict.NeedsOnlineOverlapConfirm;
                evaluation.message = "此線上賽事與已報名賽事日期重疊，出賽會額外消耗選手精力。確定要報名嗎？";
                return evaluation;
            }

            evaluation.verdict = CompetitionScheduleVerdict.Allowed;
            return evaluation;
        }

        /// <summary>AI hard rules: no dual physical, no 3+ concurrent. Online overlap auto-accepted.</summary>
        public static bool CanAiAccept(
            CompetitionCommitment candidate,
            IReadOnlyList<CompetitionCommitment> existing)
        {
            CompetitionScheduleEvaluation evaluation = EvaluatePlayerSignup(
                candidate, existing, confirmOnlineOverlap: true, keepCompetitionIds: null);
            return evaluation.verdict == CompetitionScheduleVerdict.Allowed;
        }

        static CompetitionCommitment FindDualPhysicalOverlap(List<CompetitionCommitment> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (!IsPhysical(list[i].format))
                    continue;
                for (int j = i + 1; j < list.Count; j++)
                {
                    if (!IsPhysical(list[j].format))
                        continue;
                    if (CommitmentsOverlap(list[i], list[j]))
                        return list[i];
                }
            }

            return null;
        }

        static List<CompetitionCommitment> FindOverCapacityCluster(List<CompetitionCommitment> list)
        {
            if (list == null || list.Count <= MaxConcurrentCommitments)
                return null;

            var points = new List<(int hour, int delta)>();
            foreach (CompetitionCommitment c in list)
            {
                points.Add((c.startHour, 1));
                // end is inclusive → drop after endHour
                points.Add((c.endHour + 1, -1));
            }

            points.Sort((a, b) =>
            {
                int cmp = a.hour.CompareTo(b.hour);
                if (cmp != 0)
                    return cmp;
                // process ends before starts at same hour
                return a.delta.CompareTo(b.delta);
            });

            int active = 0;
            int max = 0;
            var overHours = new HashSet<int>();
            foreach ((int hour, int delta) in points)
            {
                active += delta;
                if (active > max)
                    max = active;
                if (active > MaxConcurrentCommitments)
                    overHours.Add(hour);
            }

            if (max <= MaxConcurrentCommitments)
                return null;

            // Collect commitments active during any over-capacity moment.
            // Approximate: any commitment overlapping a peer set that reaches 3 at its start.
            var cluster = new List<CompetitionCommitment>();
            foreach (CompetitionCommitment c in list)
            {
                int atStart = list.Count(o =>
                    o.startHour <= c.startHour && o.endHour >= c.startHour);
                if (atStart > MaxConcurrentCommitments)
                {
                    cluster.Add(c);
                    continue;
                }

                // Also include if it overlaps every member of a known triple via pairwise with over-cap peers
                if (list.Count(o => CommitmentsOverlap(c, o)) > MaxConcurrentCommitments)
                    cluster.Add(c);
            }

            if (cluster.Count <= MaxConcurrentCommitments)
            {
                // Fallback: all commitments that overlap the densest start
                CompetitionCommitment densest = list
                    .OrderByDescending(c => list.Count(o =>
                        o.startHour <= c.startHour && o.endHour >= c.startHour))
                    .First();
                cluster = list.Where(o =>
                    o.startHour <= densest.startHour && o.endHour >= densest.startHour).ToList();
            }

            return cluster.Count > MaxConcurrentCommitments ? cluster : null;
        }

        static bool TryValidateKeepTwo(
            List<CompetitionCommitment> cluster,
            IReadOnlyList<string> keepCompetitionIds,
            out string failure)
        {
            failure = null;
            var keep = keepCompetitionIds
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (keep.Count != MaxConcurrentCommitments)
            {
                failure = "count";
                return false;
            }

            var kept = new List<CompetitionCommitment>();
            foreach (string id in keep)
            {
                CompetitionCommitment match = cluster.FirstOrDefault(x =>
                    string.Equals(x.competitionId, id, StringComparison.OrdinalIgnoreCase));
                if (match == null)
                {
                    failure = "count";
                    return false;
                }

                kept.Add(match);
            }

            foreach (CompetitionCommitment drop in cluster)
            {
                if (kept.Any(x => string.Equals(x.competitionId, drop.competitionId, StringComparison.OrdinalIgnoreCase)))
                    continue;
                if (drop.isLockedSeat)
                {
                    failure = "locked";
                    return false;
                }
            }

            if (FindDualPhysicalOverlap(kept) != null)
            {
                failure = "count";
                return false;
            }

            return true;
        }

        static void FillConflict(CompetitionScheduleEvaluation evaluation, List<CompetitionCommitment> items)
        {
            evaluation.conflictCompetitionIds = items
                .Select(x => x.competitionId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            evaluation.conflictDisplayNames = items
                .GroupBy(x => x.competitionId, StringComparer.OrdinalIgnoreCase)
                .Select(g => string.IsNullOrWhiteSpace(g.First().displayName) ? g.Key : g.First().displayName)
                .ToList();
        }
    }
}
