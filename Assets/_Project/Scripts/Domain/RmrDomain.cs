using System;
using System.Collections.Generic;
using System.Linq;

namespace TacticaUniversalis.Domain
{
    /// <summary>Stockholm 2021 Regional Major Rankings round.</summary>
    public enum RmrRound
    {
        None = 0,
        Circuit2020 = 1,
        Rmr1 = 2,
        Rmr2 = 3,
        Rmr3 = 4
    }

    /// <summary>Major stage seat from regional RMR placement preview.</summary>
    public enum RmrMajorStage
    {
        None = 0,
        Contender = 1,
        Challenger = 2,
        Legend = 3
    }

    [Serializable]
    public sealed class RmrPointBatch
    {
        public string id;
        public string clubId;
        public string competitionId;
        public int earnedHour;
        public float points;
        public int placement;
        public RmrRound round;
        /// <summary>True when points are negative roster-tax deductions.</summary>
        public bool isDeduction;
        public bool seedSnapshot;
    }

    [Serializable]
    public sealed class RmrEntry
    {
        public string clubId;
        public string rmrRegion;
        public int regionRank;
        public float totalPoints;
        public float circuitPoints;
        public float rmr1Points;
        public float rmr2Points;
        public float rmr3Points;
        public float deductionPoints;
        public RmrMajorStage stage;
    }

    /// <summary>Stockholm 2021 RMR points — no HLTV-style decay.</summary>
    public static class RmrRules
    {
        public const float RosterDeductionFraction = 0.20f;

        public static readonly string[] AllRegions =
        {
            "Europe", "NorthAmerica", "CIS", "SouthAmerica", "Asia", "Oceania"
        };

        private static readonly Dictionary<string, string> KnownClubRmrRegions =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["navi_2021"] = "CIS",
                ["gambit_2021"] = "CIS",
                ["spirit_2021"] = "CIS",
                ["virtuspro_2021"] = "CIS",
                ["liquid_2021"] = "NorthAmerica",
                ["eg_2021"] = "NorthAmerica",
                ["complexity_2021"] = "NorthAmerica",
                ["furia_2021"] = "SouthAmerica",
                ["godsent_2021"] = "SouthAmerica",
                ["mibr_2021"] = "SouthAmerica"
            };

        private static readonly Dictionary<string, (RmrRound round, string region)> KnownCompetitionRmr =
            new Dictionary<string, (RmrRound, string)>(StringComparer.OrdinalIgnoreCase)
            {
                ["flashpoint_season_3"] = (RmrRound.Rmr1, "Europe"),
                ["cs_summit_8"] = (RmrRound.Rmr1, "NorthAmerica"),
                ["epic_cis_league_spring_2021"] = (RmrRound.Rmr1, "CIS"),
                ["starladder_cis_rmr_2021"] = (RmrRound.Rmr2, "CIS"),
                ["iem_xvi_fall_europe"] = (RmrRound.Rmr3, "Europe"),
                ["iem_xvi_fall_na"] = (RmrRound.Rmr3, "NorthAmerica"),
                ["iem_xvi_fall_cis"] = (RmrRound.Rmr3, "CIS")
            };

        public static string NormalizeRmrRegion(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;
            string t = raw.Trim();
            if (t.Equals("Europe", StringComparison.OrdinalIgnoreCase) ||
                t.Equals("EU", StringComparison.OrdinalIgnoreCase))
                return "Europe";
            if (t.Equals("NorthAmerica", StringComparison.OrdinalIgnoreCase) ||
                t.Equals("North America", StringComparison.OrdinalIgnoreCase) ||
                t.Equals("NA", StringComparison.OrdinalIgnoreCase))
                return "NorthAmerica";
            if (t.Equals("CIS", StringComparison.OrdinalIgnoreCase))
                return "CIS";
            if (t.Equals("SouthAmerica", StringComparison.OrdinalIgnoreCase) ||
                t.Equals("South America", StringComparison.OrdinalIgnoreCase) ||
                t.Equals("SA", StringComparison.OrdinalIgnoreCase))
                return "SouthAmerica";
            if (t.Equals("Asia", StringComparison.OrdinalIgnoreCase) ||
                t.Equals("Asia-Pacific", StringComparison.OrdinalIgnoreCase) ||
                t.Equals("APAC", StringComparison.OrdinalIgnoreCase))
                return "Asia";
            if (t.Equals("Oceania", StringComparison.OrdinalIgnoreCase) ||
                t.Equals("OCE", StringComparison.OrdinalIgnoreCase))
                return "Oceania";
            if (t.Equals("Americas", StringComparison.OrdinalIgnoreCase))
                return "NorthAmerica";
            return t;
        }

        public static string ResolveClubRmrRegion(CareerClubDefinition club)
        {
            if (club == null)
                return string.Empty;
            string fromField = NormalizeRmrRegion(club.rmrRegion);
            if (!string.IsNullOrEmpty(fromField))
                return fromField;
            if (!string.IsNullOrEmpty(club.id) &&
                KnownClubRmrRegions.TryGetValue(club.id, out string known))
                return known;
            return NormalizeRmrRegion(club.region);
        }

        public static RmrRound ParseRound(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return RmrRound.None;
            string t = raw.Trim();
            if (Enum.TryParse(t, true, out RmrRound parsed) && parsed != RmrRound.None)
                return parsed;
            if (t.Equals("Circuit", StringComparison.OrdinalIgnoreCase) ||
                t.Equals("RMR Circuit 2020", StringComparison.OrdinalIgnoreCase))
                return RmrRound.Circuit2020;
            if (t.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                t.Equals("RMR1", StringComparison.OrdinalIgnoreCase))
                return RmrRound.Rmr1;
            if (t.Equals("2", StringComparison.OrdinalIgnoreCase) ||
                t.Equals("RMR2", StringComparison.OrdinalIgnoreCase))
                return RmrRound.Rmr2;
            if (t.Equals("3", StringComparison.OrdinalIgnoreCase) ||
                t.Equals("RMR3", StringComparison.OrdinalIgnoreCase))
                return RmrRound.Rmr3;
            return RmrRound.None;
        }

        public static bool TryResolveCompetitionRmr(
            CompetitionDefinition competition, out RmrRound round, out string rmrRegion)
        {
            round = RmrRound.None;
            rmrRegion = string.Empty;
            if (competition == null)
                return false;

            round = ParseRound(competition.rmrRound);
            rmrRegion = NormalizeRmrRegion(competition.rmrRegion);
            if (round != RmrRound.None && !string.IsNullOrEmpty(rmrRegion))
                return true;

            if (!string.IsNullOrEmpty(competition.id) &&
                KnownCompetitionRmr.TryGetValue(competition.id, out (RmrRound round, string region) known))
            {
                if (round == RmrRound.None)
                    round = known.round;
                if (string.IsNullOrEmpty(rmrRegion))
                    rmrRegion = known.region;
                return round != RmrRound.None && !string.IsNullOrEmpty(rmrRegion);
            }

            return round != RmrRound.None && !string.IsNullOrEmpty(rmrRegion);
        }

        /// <summary>Stockholm 2021 event point table.</summary>
        public static float PointsFor(RmrRound round, int placement)
        {
            if (placement <= 0 || round == RmrRound.None)
                return 0f;

            switch (round)
            {
                case RmrRound.Circuit2020:
                    if (placement <= 8) return 600f;
                    if (placement <= 16) return 300f;
                    if (placement <= 24) return 100f;
                    return 0f;
                case RmrRound.Rmr1:
                    return Rmr1Table(placement);
                case RmrRound.Rmr2:
                    return Rmr2Table(placement);
                case RmrRound.Rmr3:
                    return Rmr3Table(placement);
                default:
                    return 0f;
            }
        }

        private static float Rmr1Table(int p)
        {
            switch (p)
            {
                case 1: return 1600f;
                case 2: return 1500f;
                case 3: return 1400f;
                case 4: return 1300f;
                case 5: return 1200f;
                case 6: return 1100f;
                case 7: return 1000f;
                case 8: return 900f;
                case 9: return 800f;
                case 10: return 700f;
                case 11: return 600f;
                case 12: return 500f;
                case 13: return 400f;
                case 14: return 300f;
                case 15: return 200f;
                case 16: return 100f;
                default: return 0f;
            }
        }

        private static float Rmr2Table(int p)
        {
            switch (p)
            {
                case 1: return 2000f;
                case 2: return 1875f;
                case 3: return 1750f;
                case 4: return 1625f;
                case 5: return 1500f;
                case 6: return 1375f;
                case 7: return 1250f;
                case 8: return 1125f;
                case 9: return 1000f;
                case 10: return 875f;
                case 11: return 750f;
                case 12: return 625f;
                case 13: return 500f;
                case 14: return 375f;
                case 15: return 250f;
                case 16: return 125f;
                default: return 0f;
            }
        }

        private static float Rmr3Table(int p)
        {
            switch (p)
            {
                case 1: return 2500f;
                case 2: return 2344f;
                case 3: return 2188f;
                case 4: return 2031f;
                case 5: return 1875f;
                case 6: return 1719f;
                case 7: return 1563f;
                case 8: return 1406f;
                case 9: return 1250f;
                case 10: return 1094f;
                case 11: return 938f;
                case 12: return 781f;
                case 13: return 625f;
                case 14: return 469f;
                case 15: return 313f;
                case 16: return 156f;
                default: return 0f;
            }
        }

        public static void GetSpotCounts(
            string rmrRegion, out int legends, out int challengers, out int contenders)
        {
            switch (NormalizeRmrRegion(rmrRegion))
            {
                case "Europe":
                    legends = 3; challengers = 6; contenders = 2; break;
                case "CIS":
                    legends = 2; challengers = 2; contenders = 1; break;
                case "NorthAmerica":
                    legends = 3; challengers = 1; contenders = 1; break;
                case "SouthAmerica":
                case "Asia":
                case "Oceania":
                    legends = 0; challengers = 0; contenders = 1; break;
                default:
                    legends = 0; challengers = 0; contenders = 0; break;
            }
        }

        public static RmrMajorStage ResolveStage(string rmrRegion, int regionRank)
        {
            if (regionRank <= 0)
                return RmrMajorStage.None;
            GetSpotCounts(rmrRegion, out int legends, out int challengers, out int contenders);
            if (regionRank <= legends)
                return RmrMajorStage.Legend;
            if (regionRank <= legends + challengers)
                return RmrMajorStage.Challenger;
            if (regionRank <= legends + challengers + contenders)
                return RmrMajorStage.Contender;
            return RmrMajorStage.None;
        }

        public static string StageDisplayName(RmrMajorStage stage)
        {
            switch (stage)
            {
                case RmrMajorStage.Legend: return "傳奇";
                case RmrMajorStage.Challenger: return "挑戰者";
                case RmrMajorStage.Contender: return "爭奪者";
                default: return "未取得";
            }
        }

        public static string RegionDisplayName(string rmrRegion)
        {
            switch (NormalizeRmrRegion(rmrRegion))
            {
                case "Europe": return "歐洲";
                case "NorthAmerica": return "北美";
                case "CIS": return "獨聯體";
                case "SouthAmerica": return "南美";
                case "Asia": return "亞洲";
                case "Oceania": return "大洋洲";
                default: return rmrRegion ?? string.Empty;
            }
        }

        public static float CurrentTotal(IEnumerable<RmrPointBatch> batches, string clubId)
        {
            if (batches == null || string.IsNullOrWhiteSpace(clubId))
                return 0f;
            return batches
                .Where(x => x != null &&
                            string.Equals(x.clubId, clubId, StringComparison.OrdinalIgnoreCase))
                .Sum(x => x.points);
        }

        public static RmrPointBatch CreatePlacementBatch(
            string clubId, string competitionId, RmrRound round, int placement, int earnedHour)
        {
            float points = PointsFor(round, placement);
            return new RmrPointBatch
            {
                id = $"rmr_{competitionId}_{clubId}_{placement}_{earnedHour}",
                clubId = clubId,
                competitionId = competitionId ?? string.Empty,
                earnedHour = earnedHour,
                points = points,
                placement = placement,
                round = round,
                isDeduction = false,
                seedSnapshot = false
            };
        }

        public static RmrPointBatch CreateCircuitSeedBatch(string clubId, float points)
        {
            return new RmrPointBatch
            {
                id = "rmr_circuit_seed_" + clubId,
                clubId = clubId,
                competitionId = "rmr_circuit_2020",
                earnedHour = 0,
                points = Math.Max(0f, points),
                placement = 0,
                round = RmrRound.Circuit2020,
                isDeduction = false,
                seedSnapshot = true
            };
        }

        /// <summary>20% of current total per core roster replacement; stacks.</summary>
        public static RmrPointBatch CreateRosterDeductionBatch(
            IEnumerable<RmrPointBatch> existing, string clubId, int earnedHour, string reasonId = null)
        {
            float total = Math.Max(0f, CurrentTotal(existing, clubId));
            float deduct = (float)Math.Round(total * RosterDeductionFraction, MidpointRounding.AwayFromZero);
            if (deduct <= 0f)
                return null;
            return new RmrPointBatch
            {
                id = $"rmr_deduct_{clubId}_{earnedHour}_{(reasonId ?? "roster")}",
                clubId = clubId,
                competitionId = reasonId ?? "roster_change",
                earnedHour = earnedHour,
                points = -deduct,
                placement = 0,
                round = RmrRound.None,
                isDeduction = true,
                seedSnapshot = false
            };
        }

        public static int CircuitSeedFromClub(CareerClubDefinition club)
        {
            if (club == null)
                return 0;
            if (club.rmrCircuitSeed > 0)
                return club.rmrCircuitSeed;
            // Fallback bands from opening HLTV snapshot when JSON seed unset.
            if (club.hltvPoints >= 800) return 600;
            if (club.hltvPoints >= 450) return 300;
            if (club.hltvPoints >= 150) return 100;
            return 0;
        }

        public static List<RmrEntry> Build(
            IEnumerable<RmrPointBatch> batches,
            IEnumerable<CareerClubDefinition> clubs,
            string rmrRegionFilter = null)
        {
            string filter = NormalizeRmrRegion(rmrRegionFilter);
            RmrPointBatch[] all = (batches ?? Enumerable.Empty<RmrPointBatch>())
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.clubId))
                .ToArray();
            CareerClubDefinition[] clubArr = (clubs ?? Enumerable.Empty<CareerClubDefinition>())
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.id))
                .ToArray();

            var entries = new List<RmrEntry>();
            foreach (CareerClubDefinition club in clubArr)
            {
                string region = ResolveClubRmrRegion(club);
                if (string.IsNullOrEmpty(region))
                    continue;
                if (!string.IsNullOrEmpty(filter) &&
                    !string.Equals(region, filter, StringComparison.OrdinalIgnoreCase))
                    continue;

                RmrPointBatch[] clubBatches = all
                    .Where(x => string.Equals(x.clubId, club.id, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                var entry = new RmrEntry
                {
                    clubId = club.id,
                    rmrRegion = region,
                    circuitPoints = SumRound(clubBatches, RmrRound.Circuit2020),
                    rmr1Points = SumRound(clubBatches, RmrRound.Rmr1),
                    rmr2Points = SumRound(clubBatches, RmrRound.Rmr2),
                    rmr3Points = SumRound(clubBatches, RmrRound.Rmr3),
                    deductionPoints = clubBatches.Where(x => x.isDeduction || x.points < 0f).Sum(x => x.points),
                    totalPoints = clubBatches.Sum(x => x.points)
                };
                entries.Add(entry);
            }

            // Group by region for ranking even when building all regions.
            foreach (IGrouping<string, RmrEntry> group in entries.GroupBy(
                         x => x.rmrRegion, StringComparer.OrdinalIgnoreCase))
            {
                RmrEntry[] ordered = group
                    .OrderByDescending(x => x.totalPoints)
                    .ThenBy(x => x.clubId, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                for (int i = 0; i < ordered.Length; i++)
                {
                    ordered[i].regionRank = i + 1;
                    ordered[i].stage = ResolveStage(group.Key, ordered[i].regionRank);
                }
            }

            return entries
                .OrderBy(x => RegionSortKey(x.rmrRegion))
                .ThenBy(x => x.regionRank)
                .ThenBy(x => x.clubId, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static int RegionSortKey(string rmrRegion)
        {
            int idx = Array.FindIndex(AllRegions,
                r => string.Equals(r, NormalizeRmrRegion(rmrRegion), StringComparison.OrdinalIgnoreCase));
            return idx >= 0 ? idx : 99;
        }

        private static float SumRound(IEnumerable<RmrPointBatch> batches, RmrRound round) =>
            batches.Where(x => x.round == round && !x.isDeduction).Sum(x => x.points);
    }
}
