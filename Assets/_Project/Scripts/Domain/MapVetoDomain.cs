using System;
using System.Collections.Generic;
using System.Linq;

namespace TacticaUniversalis.Domain
{
    [Serializable]
    public sealed class MapPoolEntry
    {
        public string id;
        public string displayName;
        public bool playable;
        public string overviewResource;
    }

    public enum MapVetoStepKind
    {
        BanA = 0,
        BanB = 1,
        PickA = 2,
        PickB = 3,
        BanA2 = 4,
        BanB2 = 5,
        Decider = 6
    }

    /// <summary>BO3 seven-step map veto: Ban Ban Pick Pick Ban Ban Decider.</summary>
    public static class MapVetoRules
    {
        public const int PoolSize = 7;
        public const int BestOf = 3;
        public const int WinsNeeded = 2;
        public const int StepCount = 7;

        public static readonly string[] DefaultPoolIds =
        {
            "dust2", "mirage", "inferno", "nuke", "overpass", "train", "vertigo"
        };

        public static readonly string[] DefaultDisplayNames =
        {
            "Dust II", "Mirage", "Inferno", "Nuke", "Overpass", "Train", "Vertigo"
        };

        public static MapPoolEntry[] CreateDefaultPool()
        {
            var entries = new MapPoolEntry[DefaultPoolIds.Length];
            for (int i = 0; i < DefaultPoolIds.Length; i++)
            {
                entries[i] = new MapPoolEntry
                {
                    id = DefaultPoolIds[i],
                    displayName = DefaultDisplayNames[i],
                    playable = string.Equals(DefaultPoolIds[i], "dust2", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(DefaultPoolIds[i], "train", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(DefaultPoolIds[i], "ancient", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(DefaultPoolIds[i], "overpass", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(DefaultPoolIds[i], "vertigo", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(DefaultPoolIds[i], "inferno", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(DefaultPoolIds[i], "mirage", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(DefaultPoolIds[i], "nuke", StringComparison.OrdinalIgnoreCase),
                    overviewResource = string.Equals(DefaultPoolIds[i], "dust2", StringComparison.OrdinalIgnoreCase)
                        ? "Maps/dust2_overview"
                        : string.Equals(DefaultPoolIds[i], "train", StringComparison.OrdinalIgnoreCase)
                            ? "Maps/train_overview"
                            : string.Equals(DefaultPoolIds[i], "ancient", StringComparison.OrdinalIgnoreCase)
                                ? "Maps/ancient_overview"
                                : string.Equals(DefaultPoolIds[i], "overpass", StringComparison.OrdinalIgnoreCase)
                                    ? "Maps/overpass_overview"
                                    : string.Equals(DefaultPoolIds[i], "vertigo", StringComparison.OrdinalIgnoreCase)
                                        ? "Maps/vertigo_overview"
                                        : string.Equals(DefaultPoolIds[i], "inferno", StringComparison.OrdinalIgnoreCase)
                                            ? "Maps/inferno_overview"
                                            : string.Equals(DefaultPoolIds[i], "mirage", StringComparison.OrdinalIgnoreCase)
                                                ? "Maps/mirage_overview"
                                                : string.Equals(DefaultPoolIds[i], "nuke", StringComparison.OrdinalIgnoreCase)
                                                    ? "Maps/nuke_overview"
                                                    : string.Empty
                };
            }

            return entries;
        }

        public static bool PlayerIsVetoSideA(int seed) => (seed & 1) == 0;

        public static MapVetoStepKind GetStepKind(int stepIndex)
        {
            if (stepIndex < 0 || stepIndex >= StepCount)
                throw new ArgumentOutOfRangeException(nameof(stepIndex));
            return (MapVetoStepKind)stepIndex;
        }

        public static bool IsBanStep(MapVetoStepKind kind) =>
            kind == MapVetoStepKind.BanA ||
            kind == MapVetoStepKind.BanB ||
            kind == MapVetoStepKind.BanA2 ||
            kind == MapVetoStepKind.BanB2;

        public static bool IsPickStep(MapVetoStepKind kind) =>
            kind == MapVetoStepKind.PickA || kind == MapVetoStepKind.PickB;

        public static bool IsSideATurn(MapVetoStepKind kind) =>
            kind == MapVetoStepKind.BanA ||
            kind == MapVetoStepKind.PickA ||
            kind == MapVetoStepKind.BanA2;

        public static bool IsPlayerTurn(int stepIndex, bool playerIsVetoA)
        {
            if (stepIndex < 0 || stepIndex >= StepCount - 1)
                return false;
            bool sideA = IsSideATurn(GetStepKind(stepIndex));
            return sideA == playerIsVetoA;
        }

        public static string StepLabelZh(MapVetoStepKind kind, bool playerIsVetoA)
        {
            bool playerTurn = IsSideATurn(kind) == playerIsVetoA;
            string who = playerTurn ? "我方" : "對手";
            if (IsBanStep(kind))
                return who + "禁用";
            if (IsPickStep(kind))
                return who + "選取";
            return "決勝圖";
        }

        /// <summary>Apply ban or pick. Decider step is automatic via TryResolveDecider.</summary>
        public static bool TryApplyChoice(
            CareerMapSeriesState series,
            string mapId,
            out string error)
        {
            error = null;
            if (series == null)
            {
                error = "series missing";
                return false;
            }

            if (series.vetoComplete)
            {
                error = "veto already complete";
                return false;
            }

            if (series.vetoStep < 0 || series.vetoStep >= StepCount - 1)
            {
                error = "invalid veto step";
                return false;
            }

            if (string.IsNullOrWhiteSpace(mapId))
            {
                error = "map id missing";
                return false;
            }

            EnsureLists(series);
            if (!series.poolRemaining.Any(x => string.Equals(x, mapId, StringComparison.OrdinalIgnoreCase)))
            {
                error = "map not in remaining pool";
                return false;
            }

            MapVetoStepKind kind = GetStepKind(series.vetoStep);
            string normalized = series.poolRemaining.First(x =>
                string.Equals(x, mapId, StringComparison.OrdinalIgnoreCase));

            if (IsBanStep(kind))
            {
                series.poolRemaining.RemoveAll(x =>
                    string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase));
                series.bannedIds.Add(normalized);
                series.vetoLog.Add("ban:" + normalized);
            }
            else if (IsPickStep(kind))
            {
                series.poolRemaining.RemoveAll(x =>
                    string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase));
                if (kind == MapVetoStepKind.PickA)
                    series.seriesMapIds[0] = normalized;
                else
                    series.seriesMapIds[1] = normalized;
                series.vetoLog.Add("pick:" + normalized);
            }
            else
            {
                error = "decider is automatic";
                return false;
            }

            series.vetoStep++;
            if (series.vetoStep == (int)MapVetoStepKind.Decider)
                TryResolveDecider(series, out _);
            return true;
        }

        public static bool TryResolveDecider(CareerMapSeriesState series, out string error)
        {
            error = null;
            EnsureLists(series);
            if (series.vetoStep != (int)MapVetoStepKind.Decider)
            {
                error = "not on decider step";
                return false;
            }

            if (series.poolRemaining.Count != 1)
            {
                error = "decider requires exactly one map left";
                return false;
            }

            series.seriesMapIds[2] = series.poolRemaining[0];
            series.poolRemaining.Clear();
            series.vetoLog.Add("decider:" + series.seriesMapIds[2]);
            series.vetoStep = StepCount;
            series.vetoComplete = true;
            series.seriesActive = true;
            series.nextMapIndex = 0;
            return true;
        }

        public static void BeginVeto(CareerMapSeriesState series, int seed, IList<string> poolIds = null)
        {
            if (series == null)
                throw new ArgumentNullException(nameof(series));
            EnsureLists(series);
            series.ResetVeto();
            IEnumerable<string> source = poolIds != null && poolIds.Count == PoolSize
                ? poolIds
                : DefaultPoolIds;
            series.poolRemaining = source.ToList();
            series.playerIsVetoA = PlayerIsVetoSideA(seed);
            series.vetoStep = 0;
            series.vetoComplete = false;
            series.seriesActive = false;
            series.seriesMapIds = new string[3];
            series.mapWinsPlayer = 0;
            series.mapWinsOpponent = 0;
            series.nextMapIndex = 0;
            series.mapScorePlayer = new List<int>();
            series.mapScoreOpponent = new List<int>();
        }

        /// <summary>AI picks a random remaining map for the current ban/pick step.</summary>
        public static bool TryApplyAiChoice(CareerMapSeriesState series, int seed, out string chosenId, out string error)
        {
            chosenId = null;
            error = null;
            EnsureLists(series);
            if (series.vetoComplete || series.poolRemaining.Count == 0)
            {
                error = "no maps left";
                return false;
            }

            int idx = Math.Abs(seed * 397 + series.vetoStep * 911) % series.poolRemaining.Count;
            chosenId = series.poolRemaining[idx];
            return TryApplyChoice(series, chosenId, out error);
        }

        public static void AutoCompleteVeto(CareerMapSeriesState series, int seed, IList<string> poolIds = null)
        {
            BeginVeto(series, seed, poolIds);
            while (!series.vetoComplete)
            {
                if (series.vetoStep == (int)MapVetoStepKind.Decider)
                {
                    TryResolveDecider(series, out _);
                    break;
                }

                if (!TryApplyAiChoice(series, seed, out _, out string err))
                    throw new InvalidOperationException("AI veto failed: " + err);
            }
        }

        public static string ResolvePlayableMapId(string mapId, Func<string, bool> isPlayable = null)
        {
            if (string.IsNullOrEmpty(mapId))
                return "dust2";
            if (isPlayable != null && isPlayable(mapId))
                return mapId;
            if (string.Equals(mapId, "dust2", StringComparison.OrdinalIgnoreCase))
                return "dust2";
            // Unimplemented maps fall back to Dust II for combat assets.
            return "dust2";
        }

        public static bool SeriesIsComplete(CareerMapSeriesState series) =>
            series != null &&
            series.vetoComplete &&
            (series.mapWinsPlayer >= WinsNeeded || series.mapWinsOpponent >= WinsNeeded);

        public static void EnsureLists(CareerMapSeriesState series)
        {
            if (series.poolRemaining == null)
                series.poolRemaining = new List<string>();
            if (series.bannedIds == null)
                series.bannedIds = new List<string>();
            if (series.vetoLog == null)
                series.vetoLog = new List<string>();
            if (series.seriesMapIds == null || series.seriesMapIds.Length != 3)
                series.seriesMapIds = new string[3];
            if (series.mapScorePlayer == null)
                series.mapScorePlayer = new List<int>();
            if (series.mapScoreOpponent == null)
                series.mapScoreOpponent = new List<int>();
        }
    }

    /// <summary>
    /// Active competitive map pool era: Train until hist_train_out_ancient_in, then Ancient.
    /// </summary>
    public static class MapPoolRules
    {
        public const string TrainPoolRemovedFlag = "train_pool_removed";
        public const string TrainOutAncientInEventId = "hist_train_out_ancient_in_2021_04_05";
        public const string TrainMapId = "train";
        public const string AncientMapId = "ancient";

        public static bool IsTrainRemoved(CareerState state)
        {
            if (state == null)
                return false;
            if (CareerFlagRules.HasFlag(
                    state.flags, TrainPoolRemovedFlag, CareerFlagRules.GlobalScope, state.clockHour))
                return true;
            // Event fire stamp is set even when roster-gated club effects are skipped.
            return CareerFlagRules.HasFlag(
                state.flags,
                ScriptedEventTierRules.FiredFlagId(TrainOutAncientInEventId),
                CareerFlagRules.GlobalScope,
                state.clockHour);
        }

        public static string[] ResolveActivePoolIds(bool trainRemoved)
        {
            var ids = new string[MapVetoRules.DefaultPoolIds.Length];
            for (int i = 0; i < MapVetoRules.DefaultPoolIds.Length; i++)
            {
                string id = MapVetoRules.DefaultPoolIds[i];
                if (trainRemoved && string.Equals(id, TrainMapId, StringComparison.OrdinalIgnoreCase))
                    ids[i] = AncientMapId;
                else if (!trainRemoved && string.Equals(id, AncientMapId, StringComparison.OrdinalIgnoreCase))
                    ids[i] = TrainMapId;
                else
                    ids[i] = id;
            }

            return ids;
        }

        public static MapPoolEntry[] ResolveActivePool(MapPoolEntry[] source, bool trainRemoved)
        {
            MapPoolEntry[] basePool = source != null && source.Length == MapVetoRules.PoolSize
                ? source
                : MapVetoRules.CreateDefaultPool();
            var result = new MapPoolEntry[basePool.Length];
            for (int i = 0; i < basePool.Length; i++)
            {
                MapPoolEntry src = basePool[i] ?? new MapPoolEntry();
                string id = src.id ?? string.Empty;
                if (trainRemoved && string.Equals(id, TrainMapId, StringComparison.OrdinalIgnoreCase))
                {
                    result[i] = new MapPoolEntry
                    {
                        id = AncientMapId,
                        displayName = "Ancient",
                        playable = true,
                        overviewResource = "Maps/ancient_overview"
                    };
                    continue;
                }

                if (!trainRemoved && string.Equals(id, AncientMapId, StringComparison.OrdinalIgnoreCase))
                {
                    result[i] = new MapPoolEntry
                    {
                        id = TrainMapId,
                        displayName = "Train",
                        playable = true,
                        overviewResource = "Maps/train_overview"
                    };
                    continue;
                }

                result[i] = new MapPoolEntry
                {
                    id = id,
                    displayName = src.displayName,
                    playable = IsPlayable(id, trainRemoved),
                    overviewResource = src.overviewResource
                };
                if (string.Equals(id, "dust2", StringComparison.OrdinalIgnoreCase) &&
                    string.IsNullOrEmpty(result[i].overviewResource))
                    result[i].overviewResource = "Maps/dust2_overview";
            }

            return result;
        }

        public static bool IsPlayable(string mapId, bool trainRemoved)
        {
            if (string.Equals(mapId, "dust2", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(mapId, "overpass", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(mapId, "vertigo", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(mapId, "inferno", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(mapId, "mirage", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(mapId, "nuke", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(mapId, TrainMapId, StringComparison.OrdinalIgnoreCase))
                return !trainRemoved;
            if (string.Equals(mapId, AncientMapId, StringComparison.OrdinalIgnoreCase))
                return trainRemoved;
            return false;
        }

        public static string DisplayName(string mapId)
        {
            if (string.Equals(mapId, AncientMapId, StringComparison.OrdinalIgnoreCase))
                return "Ancient";
            if (string.Equals(mapId, TrainMapId, StringComparison.OrdinalIgnoreCase))
                return "Train";
            for (int i = 0; i < MapVetoRules.DefaultPoolIds.Length; i++)
            {
                if (string.Equals(MapVetoRules.DefaultPoolIds[i], mapId, StringComparison.OrdinalIgnoreCase))
                    return MapVetoRules.DefaultDisplayNames[i];
            }

            return string.IsNullOrEmpty(mapId) ? "Dust II" : mapId;
        }
    }

    [Serializable]
    public sealed class CareerMapSeriesState
    {
        public bool seriesActive;
        public bool vetoComplete;
        public bool playerIsVetoA;
        public int vetoStep;
        public List<string> poolRemaining = new List<string>();
        public List<string> bannedIds = new List<string>();
        public List<string> vetoLog = new List<string>();
        public string[] seriesMapIds = new string[3];
        public int mapWinsPlayer;
        public int mapWinsOpponent;
        public int nextMapIndex;
        public List<int> mapScorePlayer = new List<int>();
        public List<int> mapScoreOpponent = new List<int>();

        public void ResetVeto()
        {
            seriesActive = false;
            vetoComplete = false;
            vetoStep = 0;
            poolRemaining = new List<string>();
            bannedIds = new List<string>();
            vetoLog = new List<string>();
            seriesMapIds = new string[3];
            mapWinsPlayer = 0;
            mapWinsOpponent = 0;
            nextMapIndex = 0;
            mapScorePlayer = new List<int>();
            mapScoreOpponent = new List<int>();
        }

        public string CurrentMapId =>
            seriesMapIds != null &&
            nextMapIndex >= 0 &&
            nextMapIndex < seriesMapIds.Length
                ? seriesMapIds[nextMapIndex]
                : null;
    }
}
