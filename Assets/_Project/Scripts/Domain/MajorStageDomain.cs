using System;
using System.Collections.Generic;
using System.Linq;

namespace TacticaUniversalis.Domain
{
    public enum MajorStageKind
    {
        None = 0,
        ChallengersSwiss = 1,
        LegendsSwiss = 2,
        ChampionsKnockout = 3
    }

    [Serializable]
    public sealed class MajorQualifiedSeat
    {
        public string clubId;
        public string rmrRegion;
        public RmrMajorStage stage;
        public int regionRank;
        public float totalPoints;
    }

    [Serializable]
    public sealed class MajorSwissStanding
    {
        public string clubId;
        public int wins;
        public int losses;
        public bool advanced;
        public bool eliminated;
        public int finishOrder;
        public List<string> playedOpponents = new List<string>();
    }

    [Serializable]
    public sealed class MajorSwissPairing
    {
        public string clubAId;
        public string clubBId;
        public bool completed;
        public string winnerClubId;
        public int scoreA;
        public int scoreB;
    }

    /// <summary>PGL Major Stockholm 2021 Challengers / Legends Swiss + Champions knockout helpers.</summary>
    public static class StockholmMajorRules
    {
        public const string QualificationModeRmr = "Rmr";
        public const string StructureId = "stockholm_2021";
        public const int WinsToAdvance = 3;
        public const int LossesToEliminate = 3;
        public const int ChallengersField = 16;
        public const int LegendsField = 16;
        public const int ChampionsField = 8;

        public static bool UsesRmrQualification(CompetitionDefinition competition) =>
            competition != null &&
            !string.IsNullOrWhiteSpace(competition.qualificationMode) &&
            competition.qualificationMode.Trim().Equals(QualificationModeRmr, StringComparison.OrdinalIgnoreCase);

        public static bool IsStockholmMajorStructure(CompetitionDefinition competition) =>
            UsesRmrQualification(competition) ||
            (competition != null &&
             string.Equals(competition.id, "pgl_major_stockholm_2021", StringComparison.OrdinalIgnoreCase));

        public static List<MajorQualifiedSeat> BuildQualifiedSeats(IEnumerable<RmrEntry> rankings)
        {
            var seats = new List<MajorQualifiedSeat>();
            if (rankings == null)
                return seats;

            foreach (RmrEntry entry in rankings)
            {
                if (entry == null || entry.stage == RmrMajorStage.None || string.IsNullOrWhiteSpace(entry.clubId))
                    continue;
                seats.Add(new MajorQualifiedSeat
                {
                    clubId = entry.clubId,
                    rmrRegion = entry.rmrRegion,
                    stage = entry.stage,
                    regionRank = entry.regionRank,
                    totalPoints = entry.totalPoints
                });
            }

            return seats
                .OrderBy(x => RegionSortKey(x.rmrRegion))
                .ThenBy(x => StageSortKey(x.stage))
                .ThenBy(x => x.regionRank)
                .ThenBy(x => x.clubId, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static List<string> LegendsClubIds(IEnumerable<MajorQualifiedSeat> seats) =>
            (seats ?? Enumerable.Empty<MajorQualifiedSeat>())
            .Where(x => x != null && x.stage == RmrMajorStage.Legend)
            .Select(x => x.clubId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        public static List<string> ChallengersClubIds(IEnumerable<MajorQualifiedSeat> seats) =>
            (seats ?? Enumerable.Empty<MajorQualifiedSeat>())
            .Where(x => x != null &&
                        (x.stage == RmrMajorStage.Challenger || x.stage == RmrMajorStage.Contender))
            .Select(x => x.clubId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        public static bool IsClubQualified(IEnumerable<MajorQualifiedSeat> seats, string clubId) =>
            !string.IsNullOrWhiteSpace(clubId) &&
            (seats ?? Enumerable.Empty<MajorQualifiedSeat>()).Any(x =>
                x != null && string.Equals(x.clubId, clubId, StringComparison.OrdinalIgnoreCase));

        public static RmrMajorStage SeatStage(IEnumerable<MajorQualifiedSeat> seats, string clubId)
        {
            MajorQualifiedSeat seat = (seats ?? Enumerable.Empty<MajorQualifiedSeat>()).FirstOrDefault(x =>
                x != null && string.Equals(x.clubId, clubId, StringComparison.OrdinalIgnoreCase));
            return seat?.stage ?? RmrMajorStage.None;
        }

        public static List<MajorSwissStanding> CreateSwissStandings(IEnumerable<string> clubIds)
        {
            return (clubIds ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(x => new MajorSwissStanding { clubId = x })
                .ToList();
        }

        public static List<MajorSwissStanding> ActiveSwissTeams(IEnumerable<MajorSwissStanding> standings) =>
            (standings ?? Enumerable.Empty<MajorSwissStanding>())
            .Where(x => x != null && !x.advanced && !x.eliminated)
            .ToList();

        public static bool SwissStageComplete(IEnumerable<MajorSwissStanding> standings, int advanceTarget)
        {
            int advanced = (standings ?? Enumerable.Empty<MajorSwissStanding>()).Count(x => x != null && x.advanced);
            int active = ActiveSwissTeams(standings).Count;
            return advanced >= advanceTarget || active <= 1;
        }

        public static List<MajorSwissPairing> BuildSwissPairings(
            IEnumerable<MajorSwissStanding> standings, int seed)
        {
            List<MajorSwissStanding> active = ActiveSwissTeams(standings)
                .OrderByDescending(x => x.wins)
                .ThenBy(x => x.losses)
                .ThenBy(x => x.clubId, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var pairings = new List<MajorSwissPairing>();
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var random = new SeededRandom(seed);

            foreach (IGrouping<(int wins, int losses), MajorSwissStanding> group in active
                         .GroupBy(x => (x.wins, x.losses))
                         .OrderByDescending(g => g.Key.wins)
                         .ThenBy(g => g.Key.losses))
            {
                List<MajorSwissStanding> pool = group
                    .Where(x => !used.Contains(x.clubId))
                    .OrderBy(x => x.clubId, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                // Light shuffle within score group for rematch avoidance variety.
                    for (int i = pool.Count - 1; i > 0; i--)
                {
                    int j = (int)(random.Next01() * (i + 1));
                    if (j < 0 || j > i)
                        j = i;
                    (pool[i], pool[j]) = (pool[j], pool[i]);
                }

                while (pool.Count >= 2)
                {
                    MajorSwissStanding a = pool[0];
                    pool.RemoveAt(0);
                    int partnerIndex = -1;
                    for (int i = 0; i < pool.Count; i++)
                    {
                        if (!HasPlayed(a, pool[i].clubId))
                        {
                            partnerIndex = i;
                            break;
                        }
                    }
                    if (partnerIndex < 0)
                        partnerIndex = 0;
                    MajorSwissStanding b = pool[partnerIndex];
                    pool.RemoveAt(partnerIndex);
                    used.Add(a.clubId);
                    used.Add(b.clubId);
                    pairings.Add(new MajorSwissPairing { clubAId = a.clubId, clubBId = b.clubId });
                }

                // Odd team in score group: defer to leftover list below.
                if (pool.Count == 1 && !used.Contains(pool[0].clubId))
                    used.Remove(pool[0].clubId);
            }

            List<MajorSwissStanding> leftovers = active
                .Where(x => !pairings.Any(p =>
                    string.Equals(p.clubAId, x.clubId, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(p.clubBId, x.clubId, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            for (int i = 0; i + 1 < leftovers.Count; i += 2)
            {
                pairings.Add(new MajorSwissPairing
                {
                    clubAId = leftovers[i].clubId,
                    clubBId = leftovers[i + 1].clubId
                });
            }

            return pairings;
        }

        public static void ApplySwissResult(
            List<MajorSwissStanding> standings,
            string winnerClubId,
            string loserClubId,
            int advanceTarget)
        {
            MajorSwissStanding winner = FindStanding(standings, winnerClubId);
            MajorSwissStanding loser = FindStanding(standings, loserClubId);
            if (winner == null || loser == null)
                return;

            winner.wins++;
            loser.losses++;
            if (!winner.playedOpponents.Contains(loser.clubId, StringComparer.OrdinalIgnoreCase))
                winner.playedOpponents.Add(loser.clubId);
            if (!loser.playedOpponents.Contains(winner.clubId, StringComparer.OrdinalIgnoreCase))
                loser.playedOpponents.Add(winner.clubId);

            if (!winner.advanced && winner.wins >= WinsToAdvance)
            {
                winner.advanced = true;
                winner.finishOrder = standings.Count(x => x != null && x.advanced);
            }

            if (!loser.eliminated && loser.losses >= LossesToEliminate)
            {
                loser.eliminated = true;
                loser.finishOrder = standings.Count(x => x != null && x.eliminated);
            }

            // If enough have advanced, mark remaining active as eliminated in reverse strength.
            if (standings.Count(x => x != null && x.advanced) >= advanceTarget)
            {
                foreach (MajorSwissStanding team in ActiveSwissTeams(standings))
                {
                    team.eliminated = true;
                    team.finishOrder = standings.Count(x => x != null && x.eliminated);
                }
            }
        }

        public static List<string> AdvancedClubs(IEnumerable<MajorSwissStanding> standings) =>
            (standings ?? Enumerable.Empty<MajorSwissStanding>())
            .Where(x => x != null && x.advanced)
            .OrderBy(x => x.finishOrder)
            .ThenByDescending(x => x.wins)
            .ThenBy(x => x.losses)
            .ThenBy(x => x.clubId, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.clubId)
            .Take(ChampionsField)
            .ToList();

        public static List<string> EliminatedClubs(IEnumerable<MajorSwissStanding> standings) =>
            (standings ?? Enumerable.Empty<MajorSwissStanding>())
            .Where(x => x != null && x.eliminated)
            .OrderByDescending(x => x.wins)
            .ThenBy(x => x.losses)
            .ThenBy(x => x.finishOrder)
            .ThenBy(x => x.clubId, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.clubId)
            .ToList();

        /// <summary>Challengers stage places 17-24; better Swiss record gets better place.</summary>
        public static int ChallengersEliminationPlacement(int eliminatedIndex0Based) =>
            17 + Math.Max(0, Math.Min(7, eliminatedIndex0Based));

        /// <summary>Legends stage places 9-16.</summary>
        public static int LegendsEliminationPlacement(int eliminatedIndex0Based) =>
            9 + Math.Max(0, Math.Min(7, eliminatedIndex0Based));

        public static string StageDisplayName(MajorStageKind stage)
        {
            switch (stage)
            {
                case MajorStageKind.ChallengersSwiss: return "挑戰者階段";
                case MajorStageKind.LegendsSwiss: return "傳奇階段";
                case MajorStageKind.ChampionsKnockout: return "冠軍階段";
                default: return "未開始";
            }
        }

        public static string FormatDetailRulesBlurb()
        {
            return string.Join("\n", new[]
            {
                "賽制：PGL Major Stockholm 2021",
                "資格：由各地區 Major 排名取得席位；未取得資格不可報名，取得後不可取消",
                "名額：歐洲十一、北美五、獨聯體五、亞洲一、大洋洲一、南美一",
                "挑戰者階段：十六隊瑞士制，三勝晉級、三敗淘汰",
                "傳奇階段：八席傳奇種子加八席挑戰者晉級，同樣瑞士制",
                "冠軍階段：八隊單淘汰至冠軍"
            });
        }

        private static MajorSwissStanding FindStanding(IEnumerable<MajorSwissStanding> standings, string clubId) =>
            (standings ?? Enumerable.Empty<MajorSwissStanding>()).FirstOrDefault(x =>
                x != null && string.Equals(x.clubId, clubId, StringComparison.OrdinalIgnoreCase));

        private static bool HasPlayed(MajorSwissStanding team, string opponentId) =>
            team?.playedOpponents != null &&
            team.playedOpponents.Any(x => string.Equals(x, opponentId, StringComparison.OrdinalIgnoreCase));

        private static int RegionSortKey(string rmrRegion)
        {
            int idx = Array.FindIndex(RmrRules.AllRegions,
                r => string.Equals(r, RmrRules.NormalizeRmrRegion(rmrRegion), StringComparison.OrdinalIgnoreCase));
            return idx >= 0 ? idx : 99;
        }

        private static int StageSortKey(RmrMajorStage stage)
        {
            switch (stage)
            {
                case RmrMajorStage.Legend: return 0;
                case RmrMajorStage.Challenger: return 1;
                case RmrMajorStage.Contender: return 2;
                default: return 9;
            }
        }
    }
}
