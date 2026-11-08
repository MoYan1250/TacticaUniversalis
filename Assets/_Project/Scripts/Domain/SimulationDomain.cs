using System;
using System.Collections.Generic;
using System.Linq;

namespace TacticaUniversalis.Domain
{
    public enum Side { Terrorist, CounterTerrorist }
    public enum TeamId { Navi, Gambit }
    public enum PlayerRole { Entry, Awper, Rifler, Lurker, Support, Igl }
    public enum CombatPhase { Opening, MidRound, Trade, Clutch, Retake }
    public enum EquipmentLevel
    {
        /// <summary>純 Eco：幾乎不花錢。</summary>
        Eco = 0,
        /// <summary>半起：甲 + 強手槍。</summary>
        HalfBuy = 1,
        /// <summary>強起：衝鋒槍／步槍，甲裝可能不全。</summary>
        ForceBuy = 2,
        /// <summary>全起：長槍 + 全甲 + 道具。</summary>
        FullBuy = 3,
        /// <summary>英雄槍：集中資源給 1～2 人；由玩家指定。</summary>
        Hero = 4
    }
    public enum EventType
    {
        RoundStarted, Setup, Move, Utility, Contact, Shot, Kill, BombPlanted, BombDefused,
        BombExploded, Timeout, Save, RoundEnded, HalfTime, OvertimeStarted, MatchEnded,
        TraitTriggered, StatusUpdated
    }

    [Serializable]
    public sealed class CombatAttributes
    {
        public int aim;
        public int reaction;
        public int awareness;
        public int utility;
        public int teamwork;
        public int communication;
        public int opening;
        public int clutch;

        public int Get(string key)
        {
            switch (key)
            {
                case "aim": return aim;
                case "reaction": return reaction;
                case "awareness": return awareness;
                case "utility": return utility;
                case "teamwork": return teamwork;
                case "communication": return communication;
                case "opening": return opening;
                case "clutch": return clutch;
                default: return 0;
            }
        }
    }

    [Serializable]
    public sealed class RoleFitness
    {
        public string role;
        public string grade;
        /// <summary>Training progress toward next grade (0–100). Ignored when grade is S.</summary>
        public int proficiency;
    }

    [Serializable]
    public sealed class MapFitness
    {
        public string map;
        public string grade;
        /// <summary>Training progress toward next grade (0–100). Ignored when grade is S.</summary>
        public int proficiency;
    }

    [Serializable]
    public sealed class RelationshipDefinition
    {
        public string targetId;
        public int affinity;
        public int chemistry;
    }

    [Serializable]
    public sealed class PlayerDefinition
    {
        public string id;
        /// <summary>UI label. Stable technical key remains <see cref="id"/>.</summary>
        public string displayName;
        public int careerStartYear;
        public int age;
        public string country;
        public string[] languages;
        public CombatAttributes attributes;
        public RoleFitness[] roleFitness;
        public MapFitness[] mapFitness;
        public string dust2Grade;
        public int dust2Proficiency;
        public string primaryRole;
        public string secondaryRole;
        public int prestige;
        public int fame;
        public int potential;
        public int marketValue;
        /// <summary>Legacy single status label; prefer <see cref="historicalStatus"/> / <see cref="currentStatus"/>.</summary>
        public string evaluation;
        public string historicalStatus;
        public string currentStatus;
        public int loyalty;
        public int pressure;
        public int emotion;
        public int monthlySalary;
        public float bonusShare;
        public int energy;
        public int health;
        public string[] traitIds;
        /// <summary>日常特性；尚未設計內容時為空，個人資料顯示「無」。</summary>
        public string[] dailyTraitIds;
        public RelationshipDefinition[] relationships;
    }

    [Serializable]
    public sealed class TeamDefinition
    {
        public string id;
        public string displayName;
        public string[] playerIds;
    }

    [Serializable]
    public sealed class TraitDefinition
    {
        public string id;
        public string displayName;
        public string description;
        public string effectKey;
        public bool enabled;
        public string implementationStatus;
    }

    [Serializable]
    public sealed class MapZoneDefinition
    {
        public string id;
        public string displayName;
        public float x;
        public float y;
        public string floor;
        public bool navigationOnly;
        public float attackerAimBonus;
        public float defenderReactionBonus;
        public float attackerTeamworkBonus;
        public float defenderAwarenessBonus;
    }

    [Serializable]
    public sealed class MapFloorDefinition
    {
        public string id;
        public string displayName;
        public string overviewResource;
    }

    [Serializable]
    public sealed class MapRouteDefinition
    {
        public string id;
        public string from;
        public string to;
        public string[] waypoints;
    }

    [Serializable]
    public sealed class MapDefinition
    {
        public string id;
        public string displayName;
        public string defaultFloor;
        public MapFloorDefinition[] floors;
        public MapZoneDefinition[] zones;
        public MapRouteDefinition[] routes;
    }

    /// <summary>
    /// Maps approach / fight zones to the bomb site where a plant is legal.
    /// Attack plans may target a_long / mid / b_tunnels; plants always land on a site.
    /// </summary>
    public static class MapZones
    {
        public static string ResolvePlantSite(string approachZoneId)
        {
            if (string.IsNullOrEmpty(approachZoneId))
                return "a_site";
            // Dust II B approaches
            if (approachZoneId.StartsWith("b", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(approachZoneId, "outside_tunnels", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(approachZoneId, "lower_tunnels", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(approachZoneId, "scaffolding", StringComparison.OrdinalIgnoreCase))
                return "b_site";
            // Train / Ancient B approaches
            if (string.Equals(approachZoneId, "popdog", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(approachZoneId, "underpass", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(approachZoneId, "sidewalk", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(approachZoneId, "oil", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(approachZoneId, "water", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(approachZoneId, "ruins", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(approachZoneId, "ramp", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(approachZoneId, "cave", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(approachZoneId, "tunnel", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(approachZoneId, "catwalk", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(approachZoneId, "monster", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(approachZoneId, "water", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(approachZoneId, "canal", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(approachZoneId, "b_short", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(approachZoneId, "upper_tunnels", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(approachZoneId, "generator", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(approachZoneId, "rafters", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(approachZoneId, "quad", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(approachZoneId, "pillar_b", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(approachZoneId, "b_stairs", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(approachZoneId, "banana", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(approachZoneId, "banana_car", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(approachZoneId, "logs", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(approachZoneId, "construction", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(approachZoneId, "coffins", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(approachZoneId, "b_apps", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(approachZoneId, "market", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(approachZoneId, "kitchen", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(approachZoneId, "van", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(approachZoneId, "secret", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(approachZoneId, "garage", StringComparison.OrdinalIgnoreCase))
                return "b_site";
            return "a_site";
        }
    }

    public sealed class ContentDatabase
    {
        public IReadOnlyDictionary<string, PlayerDefinition> Players { get; }
        public IReadOnlyDictionary<string, TeamDefinition> Teams { get; }
        public IReadOnlyDictionary<string, TraitDefinition> Traits { get; }
        public IReadOnlyDictionary<string, TacticalCardDefinition> TacticalCards { get; }
        public IReadOnlyDictionary<string, FocusCardDefinition> FocusCards { get; }
        public IReadOnlyDictionary<string, CoachCardDefinition> CoachCards { get; }
        public MapDefinition Dust2 { get; }
        public MapDefinition Train { get; }
        public MapDefinition Ancient { get; }
        public MapDefinition Overpass { get; }
        public MapDefinition Vertigo { get; }
        public MapDefinition Inferno { get; }
        public MapDefinition Mirage { get; }
        public MapDefinition Nuke { get; }

        public ContentDatabase(
            IEnumerable<PlayerDefinition> players,
            IEnumerable<TeamDefinition> teams,
            IEnumerable<TraitDefinition> traits,
            MapDefinition dust2,
            IEnumerable<TacticalCardDefinition> tacticalCards = null,
            IEnumerable<FocusCardDefinition> focusCards = null,
            IEnumerable<CoachCardDefinition> coachCards = null,
            MapDefinition train = null,
            MapDefinition ancient = null,
            MapDefinition overpass = null,
            MapDefinition vertigo = null,
            MapDefinition inferno = null,
            MapDefinition mirage = null,
            MapDefinition nuke = null)
        {
            Players = players.ToDictionary(x => x.id, StringComparer.Ordinal);
            Teams = teams.ToDictionary(x => x.id, StringComparer.OrdinalIgnoreCase);
            Traits = traits.ToDictionary(x => x.id, StringComparer.OrdinalIgnoreCase);
            TacticalCards = (tacticalCards ?? Enumerable.Empty<TacticalCardDefinition>())
                .ToDictionary(x => x.id, StringComparer.OrdinalIgnoreCase);
            FocusCards = (focusCards ?? Enumerable.Empty<FocusCardDefinition>())
                .ToDictionary(x => x.id, StringComparer.OrdinalIgnoreCase);
            CoachCards = (coachCards ?? Enumerable.Empty<CoachCardDefinition>())
                .ToDictionary(x => x.id, StringComparer.OrdinalIgnoreCase);
            Dust2 = dust2;
            Train = train;
            Ancient = ancient;
            Overpass = overpass;
            Vertigo = vertigo;
            Inferno = inferno;
            Mirage = mirage;
            Nuke = nuke;
        }

        public MapDefinition GetMap(string mapId)
        {
            if (string.Equals(mapId, "train", StringComparison.OrdinalIgnoreCase) && Train != null)
                return Train;
            if (string.Equals(mapId, "ancient", StringComparison.OrdinalIgnoreCase) && Ancient != null)
                return Ancient;
            if (string.Equals(mapId, "overpass", StringComparison.OrdinalIgnoreCase) && Overpass != null)
                return Overpass;
            if (string.Equals(mapId, "vertigo", StringComparison.OrdinalIgnoreCase) && Vertigo != null)
                return Vertigo;
            if (string.Equals(mapId, "inferno", StringComparison.OrdinalIgnoreCase) && Inferno != null)
                return Inferno;
            if (string.Equals(mapId, "mirage", StringComparison.OrdinalIgnoreCase) && Mirage != null)
                return Mirage;
            if (string.Equals(mapId, "nuke", StringComparison.OrdinalIgnoreCase) && Nuke != null)
                return Nuke;
            return Dust2;
        }

        /// <summary>Clamp a zone id onto the active map; unknown ids fall back to a safe default.</summary>
        public static string ClampZoneToMap(MapDefinition map, string zoneId)
        {
            if (map?.zones == null || map.zones.Length == 0)
                return zoneId ?? "a_site";
            if (!string.IsNullOrEmpty(zoneId) &&
                map.zones.Any(z => string.Equals(z.id, zoneId, StringComparison.OrdinalIgnoreCase)))
                return zoneId;
            if (string.Equals(map.id, "train", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(map.id, "ancient", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(map.id, "overpass", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(map.id, "vertigo", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(map.id, "inferno", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(map.id, "mirage", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(map.id, "nuke", StringComparison.OrdinalIgnoreCase))
                return "a_site";
            return map.zones.Any(z => z.id == "mid") ? "mid" : map.zones[0].id;
        }
    }

    public sealed class PlayerRuntime
    {
        public PlayerDefinition Definition { get; }
        public bool Alive { get; set; }
        public float Stamina { get; set; }
        public float Mentality { get; set; }
        public float Form { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }
        public int RoundKills { get; set; }
        public int ConsecutiveKillRounds { get; set; }
        public int TraitStacks { get; set; }
        public bool SpecialState { get; set; }
        public string ZoneId { get; set; }
        public HashSet<string> States { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public float FocusAllMult { get; set; } = 1f;
        public float FocusAim { get; set; }
        public float FocusReaction { get; set; }
        public float FocusAwareness { get; set; }
        public float FocusUtility { get; set; }
        public float FocusTeamwork { get; set; }
        public float FocusCommunication { get; set; }
        public float FocusOpening { get; set; }
        public float FocusClutch { get; set; }

        /// <summary>一般模式每人錢包；快速模式不使用。</summary>
        public int Money { get; set; }
        /// <summary>一般模式血量 0–100。</summary>
        public int Health { get; set; } = 100;
        /// <summary>一般模式護甲值 0–100。</summary>
        public int ArmorHp { get; set; }
        public PlayerLoadout Loadout { get; set; }
        public bool KeptWeaponFromPriorRound { get; set; }
        /// <summary>一般模式交戰冷卻時間戳。</summary>
        public float NextShotTime { get; set; }
        public float NextMoveTime { get; set; }
        public float LastDamageTime { get; set; } = -999f;
        /// <summary>Map-local facing in degrees, same convention as UI Atan2.</summary>
        public float FacingDeg { get; set; } = 90f;
        public int PrimaryMag { get; set; }
        public int PrimaryReserve { get; set; }
        public int PistolMag { get; set; }
        public int PistolReserve { get; set; }
        public float ReloadEndTime { get; set; } = -1f;
        public string ReloadingWeaponId { get; set; }
        public bool PreferPistol { get; set; }
        /// <summary>CS:GO m_flVelocityModifier. 1 is full speed.</summary>
        public float VelocityModifier { get; set; } = 1f;
        /// <summary>CS:GO m_flFlinchStack. Decays toward 1 between hits.</summary>
        public float FlinchStack { get; set; } = 1f;
        /// <summary>General-mode flashbang: cannot see while time &lt; BlindedUntil; may still shoot with poor accuracy.</summary>
        public float BlindedUntil { get; set; } = -1f;
        /// <summary>0 = back flash, 1 = facing blast at pop; drives blind hit multiplier.</summary>
        public float BlindSeverity01 { get; set; }

        public PlayerRuntime(PlayerDefinition definition, float initialForm)
        {
            Definition = definition;
            Stamina = 100f;
            Mentality = 50f;
            Form = FormRules.Clamp(initialForm);
            ZoneId = "spawn";
            Alive = true;
            Health = 100;
            Money = 0;
        }

        public void ClearFocusModifiers()
        {
            FocusAllMult = 1f;
            FocusAim = FocusReaction = FocusAwareness = FocusUtility = 0f;
            FocusTeamwork = FocusCommunication = FocusOpening = FocusClutch = 0f;
        }

        public void ResetRound(string spawn)
        {
            Alive = true;
            RoundKills = 0;
            SpecialState = false;
            ClearFocusModifiers();
            ZoneId = spawn;
            NextShotTime = 0f;
            NextMoveTime = 0f;
            LastDamageTime = -999f;
            FacingDeg = 90f;
            ReloadEndTime = -1f;
            ReloadingWeaponId = null;
            PreferPistol = false;
            VelocityModifier = 1f;
            FlinchStack = 1f;
            BlindedUntil = -1f;
            BlindSeverity01 = 0f;
        }
    }

    public sealed class TeamRuntime
    {
        public TeamDefinition Definition { get; }
        public List<PlayerRuntime> Players { get; }
        public int Money { get; set; }
        public int LossStreak { get; set; }
        public EquipmentLevel Equipment { get; set; }
        public Side Side { get; set; }
        public int CommandPoints { get; set; }
        public TacticalDeckState TacticalDeck { get; } = new TacticalDeckState();
        public Dictionary<string, int> SpecialUses { get; } =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public float NextRoundFirepowerPenalty { get; set; }
        public int TimeoutsRemaining { get; set; } = TimeoutRules.RegularTimeoutsPerTeam;
        public string SelectedFocusCardId { get; set; }
        public string SelectedFocusTargetId { get; set; }
        public HashSet<string> CoachUsedThisTimeout { get; } =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public bool PaidCoachUsedThisTimeout { get; set; }
        /// <summary>General-mode Hero card: 1–2 player ids designated for the key buy.</summary>
        public List<string> HeroBuyPlayerIds { get; } = new List<string>();

        public IEnumerable<PlayerRuntime> AlivePlayers => Players.Where(x => x.Alive);
        public int AliveCount => Players.Count(x => x.Alive);

        public TeamRuntime(TeamDefinition definition, IEnumerable<PlayerRuntime> players)
        {
            Definition = definition;
            Players = players.ToList();
            Money = EconomyRules.StartingTeamMoney;
        }
    }

    public sealed class MatchState
    {
        public TeamRuntime TeamA { get; }
        public TeamRuntime TeamB { get; }
        public int TeamAScore { get; set; }
        public int TeamBScore { get; set; }
        public int CompletedRounds { get; set; }
        public bool IsOvertime => CompletedRounds >= MatchRules.RegulationRounds;

        public MatchState(TeamRuntime teamA, TeamRuntime teamB)
        {
            TeamA = teamA;
            TeamB = teamB;
        }

        public int ScoreFor(string teamId) =>
            TeamA.Definition.id == teamId ? TeamAScore : TeamBScore;

        public int ScoreAgainst(string teamId) =>
            TeamA.Definition.id == teamId ? TeamBScore : TeamAScore;
    }

    public sealed class TacticalPlan
    {
        public string name;
        public string primaryZoneId;
        public float aggression;
        public float utilityCommitment;
        public float teamworkBonus;
        public float openingBonus;
        public float firepowerBonus;
        public float clutchBonus;
        public float risk;
        public int initiativeBias = 1;
        public float plantPriority;
        public EquipmentLevel equipmentLevel;
        public string[] tacticalCardIds = Array.Empty<string>();
        public string[] specialRuleKeys = Array.Empty<string>();
        public string targetPlayerId;
        public CounterModifiers counterModifiers = new CounterModifiers();
        public Dictionary<string, string> playerAssignments = new Dictionary<string, string>();
    }

    public sealed class RoundSimulationRequest
    {
        public MatchState match;
        public TeamRuntime attackers;
        public TeamRuntime defenders;
        public TacticalPlan attackerPlan;
        public TacticalPlan defenderPlan;
        public MapDefinition map;
        public int roundNumber;
        /// <summary>Optional overview LOS; null = fail-open (no wall block).</summary>
        public ICombatLosQuery Los;
    }

    public sealed class SimulationEvent
    {
        public int sequence;
        public int roundNumber;
        public float roundTime;
        public EventType type;
        public string actorId;
        public string targetId;
        public string teamId;
        public string zoneId;
        public string weaponId;
        public string message;
        public string[] reasonTags;
    }

    public sealed class RoundResult
    {
        public string winnerTeamId;
        public Side winnerSide;
        public string winReason;
        public int attackerAlive;
        public int defenderAlive;
        public List<SimulationEvent> events = new List<SimulationEvent>();
    }

    public sealed class MatchResult
    {
        public string winnerTeamId;
        public int teamAScore;
        public int teamBScore;
        public int seed;
        public List<SimulationEvent> events = new List<SimulationEvent>();
        public List<PlayerMatchSummary> playerSummaries = new List<PlayerMatchSummary>();
    }

    public sealed class PlayerMatchSummary
    {
        public string playerId;
        public string teamId;
        public int kills;
        public int deaths;
        public int assists;
        public float rating;
    }

    public interface IRandomSource
    {
        float Next01();
        int Range(int inclusiveMin, int exclusiveMax);
    }

    public sealed class SeededRandom : IRandomSource
    {
        private readonly Random _random;

        public SeededRandom(int seed) => _random = new Random(seed);
        public float Next01() => (float)_random.NextDouble();
        public int Range(int inclusiveMin, int exclusiveMax) => _random.Next(inclusiveMin, exclusiveMax);
    }

    public interface ITacticalPlanner
    {
        TacticalPlan CreatePlan(MatchState state, TeamRuntime team, Side side, IRandomSource random);
    }

    public interface IRoundSimulator
    {
        RoundResult Simulate(RoundSimulationRequest request, IRandomSource random);
    }

    public interface IMatchSimulator
    {
        MatchResult Simulate(ContentDatabase content, string teamAId, string teamBId, int seed);
    }

    public static class MatchRules
    {
        public const int RegulationHalfRounds = 15;
        public const int RegulationRounds = 30;
        public const int RegulationWins = 16;
        public const int OvertimeHalfRounds = 3;
        public const int OvertimeBlockRounds = 6;
        public const int MaximumRounds = 120;

        public static bool IsHalfTime(int completedRounds) => completedRounds == RegulationHalfRounds;

        /// <summary>Regulation pistol rounds: round 1 and first round after half-time.</summary>
        public static bool IsPistolRound(int completedRounds) =>
            completedRounds == 0 || completedRounds == RegulationHalfRounds;

        public static bool IsOvertimeHalfBoundary(int completedRounds) =>
            completedRounds >= RegulationRounds &&
            (completedRounds - RegulationRounds) % OvertimeHalfRounds == 0;

        public static bool HasWinner(MatchState state)
        {
            if ((state.TeamAScore >= RegulationWins && state.TeamBScore <= 14) ||
                (state.TeamBScore >= RegulationWins && state.TeamAScore <= 14))
                return true;

            if (state.CompletedRounds < RegulationRounds)
                return false;

            int overtimeRounds = state.CompletedRounds - RegulationRounds;
            int roundsInBlock = overtimeRounds % OvertimeBlockRounds;
            if (roundsInBlock == 0 && overtimeRounds > 0)
                roundsInBlock = OvertimeBlockRounds;

            return roundsInBlock >= 4 && Math.Abs(state.TeamAScore - state.TeamBScore) >= 2;
        }

        public static bool TeamAIsTerrorist(int completedRounds)
        {
            if (completedRounds < RegulationRounds)
                return completedRounds < RegulationHalfRounds;

            int overtimeRound = (completedRounds - RegulationRounds) % OvertimeBlockRounds;
            return overtimeRound < OvertimeHalfRounds;
        }
    }

    public static class EconomyRules
    {
        // Demo economy card scale: team pool values (not per-player × 5).
        public const int StartingTeamMoney = 800;
        public const int MaximumTeamMoney = 16000;
        public const int OvertimeTeamMoney = 8000;
        public const float EcoSurvivalBonus = 0.30f;
        public const float KnifeKillChance = 0.015f;

        public static int EquipmentCost(EquipmentLevel level)
        {
            switch (level)
            {
                case EquipmentLevel.HalfBuy: return 1700;
                case EquipmentLevel.ForceBuy: return 2800;
                case EquipmentLevel.FullBuy: return 4700;
                case EquipmentLevel.Hero: return 3200;
                default: return 0;
            }
        }

        public static float EquipmentMultiplier(EquipmentLevel level)
        {
            switch (level)
            {
                case EquipmentLevel.Eco: return 0.55f;
                case EquipmentLevel.HalfBuy: return 0.75f;
                case EquipmentLevel.ForceBuy: return 0.90f;
                case EquipmentLevel.FullBuy: return 1.10f;
                case EquipmentLevel.Hero: return 0.85f;
                default: return 1f;
            }
        }

        public static string CardName(EquipmentLevel level)
        {
            switch (level)
            {
                case EquipmentLevel.Eco: return "純 Eco";
                case EquipmentLevel.HalfBuy: return "半起";
                case EquipmentLevel.ForceBuy: return "強起";
                case EquipmentLevel.FullBuy: return "全起";
                case EquipmentLevel.Hero: return "英雄槍";
                default: return level.ToString();
            }
        }

        /// <summary>Kill reward = design table ÷ 5.</summary>
        public static int KillReward(string weaponId)
        {
            switch ((weaponId ?? string.Empty).ToLowerInvariant())
            {
                case "smg": return 120;   // 600 / 5
                case "awp": return 20;    // 100 / 5
                case "knife": return 300; // 1500 / 5
                case "pistol":
                case "rifle":
                default: return 60;       // 300 / 5
            }
        }

        public static EquipmentLevel BestAffordable(int money, bool hasAwper)
        {
            // Quick mode AI never auto-picks Hero; that card is player-designated in general mode.
            EquipmentLevel desired = EquipmentLevel.FullBuy;
            while (desired > EquipmentLevel.Eco && EquipmentCost(desired) > money)
                desired--;
            return desired;
        }

        public static bool CanAfford(int money, EquipmentLevel level) =>
            EquipmentCost(level) <= money;

        public static void Buy(TeamRuntime team, EquipmentLevel desired)
        {
            EquipmentLevel actual = desired;
            if (actual == EquipmentLevel.Hero)
                actual = EquipmentLevel.FullBuy;
            while (actual > EquipmentLevel.Eco && EquipmentCost(actual) > team.Money)
                actual--;
            team.Equipment = actual;
            team.Money -= EquipmentCost(actual);
        }

        public static int ApplyKillReward(TeamRuntime killerTeam, string weaponId)
        {
            int reward = KillReward(weaponId);
            killerTeam.Money = ClampMoney(killerTeam.Money + reward);
            return reward;
        }

        public static void Settle(TeamRuntime winner, TeamRuntime loser, string reason)
        {
            int winIncome = reason == "bomb_exploded" || reason == "bomb_defused" ? 3500 : 3250;
            winner.Money = ClampMoney(winner.Money + winIncome);
            winner.LossStreak = 0;
            loser.LossStreak = Math.Min(4, loser.LossStreak + 1);
            int lossIncome = Math.Min(3400, 1400 + 500 * loser.LossStreak);
            loser.Money = ClampMoney(loser.Money + lossIncome);
        }

        public static int ClampMoney(int money) => Math.Max(0, Math.Min(MaximumTeamMoney, money));
    }

    public static class GradeScale
    {
        private static readonly string[] Ordered =
        {
            "E", "D", "C", "B", "B+", "A", "A+", "S"
        };

        public static string Normalize(string grade)
        {
            if (string.IsNullOrWhiteSpace(grade))
                return "C";
            string g = grade.Trim().ToUpperInvariant();
            switch (g)
            {
                case "S":
                case "A+":
                case "A":
                case "B+":
                case "B":
                case "C":
                case "D":
                case "E":
                    return g;
                case "A-":
                    return "A";
                case "B-":
                    return "B";
                case "C+":
                    return "C";
                default:
                    return "C";
            }
        }

        public static bool IsMax(string grade) =>
            string.Equals(Normalize(grade), "S", StringComparison.Ordinal);

        /// <summary>Ordinal rank of a grade (E=0 … S=7). Unknown grades normalize to C.</summary>
        public static int Rank(string grade) =>
            Math.Max(0, Array.IndexOf(Ordered, Normalize(grade)));

        /// <summary>True when <paramref name="grade"/> is at least <paramref name="minInclusive"/>.</summary>
        public static bool AtLeast(string grade, string minInclusive) =>
            Rank(grade) >= Rank(minInclusive);

        public static bool TryGetNext(string grade, out string next)
        {
            string current = Normalize(grade);
            int idx = Array.IndexOf(Ordered, current);
            if (idx < 0 || idx >= Ordered.Length - 1)
            {
                next = current;
                return false;
            }

            next = Ordered[idx + 1];
            return true;
        }

        public static bool TryGetPrevious(string grade, out string previous)
        {
            string current = Normalize(grade);
            int idx = Array.IndexOf(Ordered, current);
            if (idx <= 0)
            {
                previous = current;
                return false;
            }

            previous = Ordered[idx - 1];
            return true;
        }

        /// <summary>True when <paramref name="grade"/> is strictly above <paramref name="belowExclusive"/>.</summary>
        public static bool Above(string grade, string belowExclusive) =>
            Rank(grade) > Rank(belowExclusive);

        public static float Multiplier(string grade)
        {
            switch (Normalize(grade))
            {
                case "S": return 1.10f;
                case "A+": return 1.07f;
                case "A": return 1.04f;
                case "B+": return 1.01f;
                case "B": return 0.98f;
                case "C": return 0.93f;
                case "D": return 0.87f;
                case "E": return 0.80f;
                default: return 1f;
            }
        }
    }

    public sealed class EncounterContext
    {
        public MatchState match;
        public TeamRuntime teamA;
        public TeamRuntime teamB;
        public PlayerRuntime playerA;
        public PlayerRuntime playerB;
        public CombatPhase phase;
        public MapZoneDefinition zone;
        public TacticalPlan planA;
        public TacticalPlan planB;
        public bool playerAHasInitiative;
        public int roundNumber;
    }

    public sealed class EncounterOutcome
    {
        public bool resolved;
        public PlayerRuntime killer;
        public PlayerRuntime victim;
        public float winChanceA;
        public bool headshot;
        public string[] reasonTags;
        public List<TraitProc> traitProcs = new List<TraitProc>();
    }

    public sealed class TraitProc
    {
        public string playerId;
        public string traitId;
        public float bonus;
    }

    public sealed class EncounterResolver
    {
        public EncounterOutcome Resolve(EncounterContext context, IRandomSource random)
        {
            var procsA = new List<TraitProc>();
            var procsB = new List<TraitProc>();
            float scoreA = Score(context, true, random, procsA);
            float scoreB = Score(context, false, random, procsB);
            float winChanceA = Clamp(1f / (1f + (float)Math.Exp(-(scoreA - scoreB) / 12f)), 0.15f, 0.85f);
            float resolutionChance = Clamp(
                0.60f + (context.planA.aggression + context.planB.aggression) * 0.15f +
                (context.planA.risk + context.planB.risk) * 0.05f, 0.58f, 0.94f);
            if (random.Next01() > resolutionChance)
                return new EncounterOutcome { resolved = false, winChanceA = winChanceA, reasonTags = new[] { "disengaged" } };

            bool aWins = random.Next01() < winChanceA;
            PlayerRuntime killer = aWins ? context.playerA : context.playerB;
            PlayerRuntime victim = aWins ? context.playerB : context.playerA;
            TeamRuntime victimTeam = aWins ? context.teamB : context.teamA;
            // Eco card: +30% survival — duel fails to convert into a kill.
            if (victimTeam.Equipment == EquipmentLevel.Eco && random.Next01() < EconomyRules.EcoSurvivalBonus)
            {
                return new EncounterOutcome
                {
                    resolved = false,
                    winChanceA = winChanceA,
                    reasonTags = new[] { "eco_survive" }
                };
            }

            float headshotChance = 0.12f + killer.Definition.attributes.aim / 500f;
            if (killer.Definition.traitIds.Contains("headshot_machine"))
                headshotChance += 0.20f;

            var procs = new List<TraitProc>();
            procs.AddRange(procsA);
            procs.AddRange(procsB);

            bool headshot = random.Next01() < Clamp(headshotChance, 0.05f, 0.65f);
            if (headshot && killer.Definition.traitIds.Contains("headshot_machine"))
                procs.Add(new TraitProc { playerId = killer.Definition.id, traitId = "headshot_machine", bonus = 20f });

            return new EncounterOutcome
            {
                resolved = true,
                killer = killer,
                victim = victim,
                winChanceA = winChanceA,
                headshot = headshot,
                reasonTags = new[] { context.phase.ToString(), context.playerAHasInitiative ? context.playerA.Definition.id + "_initiative" : context.playerB.Definition.id + "_initiative" },
                traitProcs = procs
            };
        }

        public float Score(EncounterContext context, bool isA, IRandomSource random, List<TraitProc> procs = null)
        {
            PlayerRuntime player = isA ? context.playerA : context.playerB;
            TeamRuntime team = isA ? context.teamA : context.teamB;
            TacticalPlan plan = isA ? context.planA : context.planB;
            CombatAttributes a = player.Definition.attributes;
            float formAdj = FormRules.AimReactionDelta(player.Form);
            float aim = (a.aim + formAdj + player.FocusAim) * player.FocusAllMult;
            float reaction = (a.reaction + formAdj + player.FocusReaction) * player.FocusAllMult;
            float awareness = (a.awareness + player.FocusAwareness) * player.FocusAllMult;
            float utility = (a.utility + player.FocusUtility) * player.FocusAllMult;
            float teamwork = (a.teamwork + player.FocusTeamwork) * player.FocusAllMult;
            float communication = (a.communication + player.FocusCommunication) * player.FocusAllMult;
            float opening = (a.opening + player.FocusOpening) * player.FocusAllMult;
            float clutch = (a.clutch + player.FocusClutch) * player.FocusAllMult;

            float score;
            switch (context.phase)
            {
                case CombatPhase.Opening:
                    score = aim * 0.30f + reaction * 0.22f + opening * 0.25f +
                            awareness * 0.13f + utility * 0.05f + teamwork * 0.05f;
                    break;
                case CombatPhase.Trade:
                    score = aim * 0.28f + reaction * 0.22f + teamwork * 0.22f +
                            communication * 0.13f + awareness * 0.15f;
                    break;
                case CombatPhase.Clutch:
                    score = clutch * 0.34f + awareness * 0.24f + aim * 0.22f +
                            reaction * 0.12f + utility * 0.08f;
                    break;
                case CombatPhase.Retake:
                    score = awareness * 0.24f + utility * 0.20f + teamwork * 0.20f +
                            aim * 0.20f + reaction * 0.16f;
                    break;
                default:
                    score = aim * 0.25f + awareness * 0.20f + teamwork * 0.18f +
                            utility * 0.14f + reaction * 0.13f + communication * 0.10f;
                    break;
            }

            score *= GradeScale.Multiplier(player.Definition.dust2Grade);
            score *= EconomyRules.EquipmentMultiplier(team.Equipment);
            score *= StatusMultiplier(player);
            float tacticalFirepower = plan.firepowerBonus + (plan.counterModifiers?.firepower ?? 0f);
            score *= Math.Max(0.70f, 1f + tacticalFirepower);
            score += (random.Next01() - 0.5f) * plan.risk * 4f;
            if (player.Definition.primaryRole == "Awper" &&
                team.Equipment != EquipmentLevel.FullBuy &&
                team.Equipment != EquipmentLevel.Hero)
            {
                bool arsenal = player.Definition.traitIds.Contains("all_weapon_arsenal");
                score -= arsenal ? 3f : 6f;
                if (arsenal)
                    Proc(procs, player, "all_weapon_arsenal", 3f);
            }
            score += plan.teamworkBonus * 10f;
            if (player.States.Contains("focus:gemini_link") &&
                team.SpecialUses.TryGetValue("gemini_duo_bonus", out int geminiMilli) &&
                geminiMilli > 0)
            {
                string mateId = string.Equals(player.Definition.id, "s1mple", StringComparison.OrdinalIgnoreCase)
                    ? "electronic"
                    : "s1mple";
                PlayerRuntime mate = team.Players.FirstOrDefault(p =>
                    string.Equals(p.Definition.id, mateId, StringComparison.OrdinalIgnoreCase));
                if (mate != null && mate.Alive &&
                    string.Equals(mate.ZoneId, player.ZoneId, StringComparison.OrdinalIgnoreCase))
                    score += geminiMilli / 100f;
            }
            if (context.phase == CombatPhase.Opening)
                score += (plan.openingBonus + (plan.counterModifiers?.opening ?? 0f)) * 10f;
            if (context.phase == CombatPhase.Clutch &&
                (!plan.specialRuleKeys.Contains("perfecto_clutch_only") ||
                 string.Equals(player.Definition.id, "perfecto", StringComparison.OrdinalIgnoreCase)))
                score += plan.clutchBonus * 10f;
            if (context.zone != null)
            {
                score += isA
                    ? (context.zone.attackerAimBonus + context.zone.attackerTeamworkBonus) * 10f
                    : (context.zone.defenderReactionBonus + context.zone.defenderAwarenessBonus) * 10f;
            }
            if ((isA && context.playerAHasInitiative) || (!isA && !context.playerAHasInitiative))
                score += 4f;

            score += ChemistryModifier(player, team);
            score += TraitModifier(context, player, team, random, procs);
            return score;
        }

        private static float ChemistryModifier(PlayerRuntime player, TeamRuntime team)
        {
            RelationshipDefinition[] relationships = player.Definition.relationships ?? Array.Empty<RelationshipDefinition>();
            int[] chemistry = relationships
                .Where(x => team.Players.Any(p => p.Definition.id.Equals(x.targetId, StringComparison.OrdinalIgnoreCase)))
                .Select(x => x.chemistry)
                .ToArray();
            return chemistry.Length == 0 ? 0f : (float)chemistry.Average() / 50f;
        }

        private static float StatusMultiplier(PlayerRuntime player)
        {
            float stamina = player.Stamina < 15f ? -0.18f : player.Stamina < 30f ? -0.10f : 0f;
            float mentality = Clamp((player.Mentality - 50f) / 10f * 0.015f, -0.06f, 0.06f);
            // Form already folded into aim/reaction via FormRules.AimReactionDelta.
            return 1f + stamina + mentality;
        }

        private static float TraitModifier(
            EncounterContext c, PlayerRuntime player, TeamRuntime team, IRandomSource random, List<TraitProc> procs)
        {
            string[] traits = player.Definition.traitIds ?? Array.Empty<string>();
            float bonus = 0f;
            int ownScore = c.match.ScoreFor(team.Definition.id);
            int opponentScore = c.match.ScoreAgainst(team.Definition.id);

            void AddBonus(string traitId, float amount)
            {
                if (Math.Abs(amount) < 0.01f)
                    return;
                bonus += amount;
                Proc(procs, player, traitId, amount);
            }

            if (traits.Contains("competitive_spirit") && opponentScore - ownScore >= 3) AddBonus("competitive_spirit", 5f);
            if (traits.Contains("clutch_master") && c.phase == CombatPhase.Clutch) AddBonus("clutch_master", 12f);
            if (traits.Contains("pyramid_solution") && player.SpecialState) AddBonus("pyramid_solution", 15f);
            if (traits.Contains("electronic_tyrant") && player.ConsecutiveKillRounds >= 2) AddBonus("electronic_tyrant", 5f);
            if (traits.Contains("front_firepower") && c.phase != CombatPhase.Clutch) AddBonus("front_firepower", 5f);
            if (traits.Contains("young_blood") && c.roundNumber <= 10) AddBonus("young_blood", 2.5f);
            if (traits.Contains("young_blood") && c.roundNumber >= 20) AddBonus("young_blood", -1.5f);
            if (traits.Contains("undying")) AddBonus("undying", c.phase == CombatPhase.Clutch ? 6f : 4f);
            if (traits.Contains("clutch_equation") && c.phase == CombatPhase.Clutch) AddBonus("clutch_equation", 8f);
            if (traits.Contains("tailwind_expert"))
            {
                if (ownScore - opponentScore >= 3) AddBonus("tailwind_expert", 3f);
                else if (opponentScore - ownScore >= 3) AddBonus("tailwind_expert", -5f);
            }
            if (traits.Contains("red_star_child") && player.TraitStacks > 0)
                AddBonus("red_star_child", player.TraitStacks * 2f);
            if (traits.Contains("human_emperor") && player.RoundKills > 0) AddBonus("human_emperor", 10f);
            if (traits.Contains("utility_master"))
                AddBonus("utility_master", c.phase == CombatPhase.Retake ? 5f : 2f);
            if (traits.Contains("trade_king") && c.phase == CombatPhase.Trade) AddBonus("trade_king", 10f);
            if (traits.Contains("trade_specialist") && c.phase == CombatPhase.Trade) AddBonus("trade_specialist", 15f);
            if (traits.Contains("precise_entry") && c.phase == CombatPhase.Opening && random.Next01() < 0.20f)
                AddBonus("precise_entry", 5f);
            if (traits.Contains("veteran") && c.roundNumber <= 3) AddBonus("veteran", 3f);
            if (traits.Contains("battle_igl") && c.roundNumber >= 28) AddBonus("battle_igl", 5f);
            if (traits.Contains("versatile") && player.Definition.primaryRole == "Support") AddBonus("versatile", 2f);
            if (traits.Contains("unsung_hero") && player.Definition.primaryRole == "Support") AddBonus("unsung_hero", 2f);
            if (traits.Contains("silent_leader"))
                AddBonus("silent_leader", player.Definition.attributes.teamwork * 0.02f);
            if (traits.Contains("silent_assassin") && player.Definition.attributes.communication < 75)
                AddBonus("silent_assassin", (75 - player.Definition.attributes.communication) * 0.05f);
            if (player.States.Contains("unlucky") && c.phase == CombatPhase.Opening) AddBonus("unlucky", -6f);
            if (player.States.Contains("adversity_awakened")) AddBonus("adversity_awakened", 3f);

            if (traits.Contains("forest_electronic"))
            {
                PlayerRuntime s1mple = team.Players.FirstOrDefault(x => x.Definition.id == "s1mple");
                if (s1mple != null)
                {
                    RelationshipDefinition relation = player.Definition.relationships?
                        .FirstOrDefault(x => x.targetId == "s1mple");
                    float chem = Math.Min(10f, (relation?.chemistry ?? 0) / 10f) * 0.18f;
                    if (chem > 0.01f) AddBonus("forest_electronic", chem);
                    if (s1mple.SpecialState) AddBonus("forest_electronic", 3f);
                }
            }

            PlayerRuntime igl = team.Players.FirstOrDefault(x => x.Definition.primaryRole == "Igl");
            if (igl != null && igl.Alive)
            {
                if (igl.Definition.traitIds.Contains("system_core")) AddBonus("system_core", 2.5f);
                if (igl.Definition.traitIds.Contains("system_architect")) AddBonus("system_architect", 4f);
                if (igl.Definition.traitIds.Contains("tactical_brain")) AddBonus("tactical_brain", 2.5f);
                if (igl.Definition.traitIds.Contains("calm_analysis")) AddBonus("calm_analysis", 2.5f);
                if (igl.Definition.traitIds.Contains("cis_charge") && c.phase == CombatPhase.Opening &&
                    player != igl && player.Definition.attributes.opening < 80)
                    AddBonus("cis_charge", 3f);
                if (igl.Definition.traitIds.Contains("red_star_academy") &&
                    (player.Definition.id == "sh1ro" || player.Definition.id == "Ax1Le" || player.Definition.id == "interz"))
                    AddBonus("red_star_academy", 0.5f);
            }

            return bonus;
        }

        private static void Proc(List<TraitProc> procs, PlayerRuntime player, string traitId, float bonus)
        {
            if (procs == null || player == null || string.IsNullOrEmpty(traitId))
                return;
            procs.Add(new TraitProc
            {
                playerId = player.Definition.id,
                traitId = traitId,
                bonus = bonus
            });
        }

        private static float Clamp(float value, float min, float max) =>
            Math.Max(min, Math.Min(max, value));
    }

    public sealed class FastRoundSimulator : IRoundSimulator
    {
        private readonly EncounterResolver _encounters = new EncounterResolver();
        private int _sequence;

        public RoundResult Simulate(RoundSimulationRequest request, IRandomSource random)
        {
            _sequence = 0;
            TeamRuntime attackers = request.attackers;
            TeamRuntime defenders = request.defenders;
            attackers.Players.ForEach(x => x.ResetRound("t_spawn"));
            defenders.Players.ForEach(x => x.ResetRound("ct_spawn"));
            EconomyRules.Buy(attackers, request.attackerPlan.equipmentLevel);
            EconomyRules.Buy(defenders, request.defenderPlan.equipmentLevel);

            var result = new RoundResult();
            ActivateRoundTraits(request, result, random);
            Add(result, request, 0f, EventType.RoundStarted, null, null, request.attackerPlan.primaryZoneId,
                $"Round {request.roundNumber}: {attackers.Definition.displayName} attacks {request.attackerPlan.primaryZoneId}");
            EmitStatusSnapshot(request, result, 0.2f);
            Add(result, request, 5f, EventType.Setup, null, null, request.attackerPlan.primaryZoneId,
                $"{request.attackerPlan.name} vs {request.defenderPlan.name}",
                null, null,
                new[]
                {
                    "money:" + attackers.Definition.id + ":" + attackers.Money,
                    "money:" + defenders.Definition.id + ":" + defenders.Money,
                    "card:" + attackers.Definition.id + ":" + (int)attackers.Equipment,
                    "card:" + defenders.Definition.id + ":" + (int)defenders.Equipment,
                    "cp:" + attackers.Definition.id + ":" + attackers.CommandPoints,
                    "cp:" + defenders.Definition.id + ":" + defenders.CommandPoints,
                    "tactic:" + attackers.Definition.id + ":" + string.Join(",", request.attackerPlan.tacticalCardIds),
                    "tactic:" + defenders.Definition.id + ":" + string.Join(",", request.defenderPlan.tacticalCardIds),
                    "counter:" + attackers.Definition.id + ":" + request.attackerPlan.counterModifiers.outcome,
                    "counter:" + defenders.Definition.id + ":" + request.defenderPlan.counterModifiers.outcome
                });

            EmitDeploymentMoves(request, result, attackers, request.attackerPlan, true, 6.5f, 0, random);
            EmitDeploymentMoves(request, result, defenders, request.defenderPlan, false, 6.5f, attackers.Players.Count, random);

            float time = 18f;
            ResolveContact(request, result, CombatPhase.Opening, time, random);
            int contactCount = 2 + random.Range(0, 3);
            for (int i = 0; i < contactCount && attackers.AliveCount > 0 && defenders.AliveCount > 0; i++)
            {
                time += 10f + random.Next01() * 8f;
                ResolveContact(request, result, i > 0 ? CombatPhase.Trade : CombatPhase.MidRound, time, random);
            }

            bool bombPlanted = false;
            string plantSiteId = MapZones.ResolvePlantSite(request.attackerPlan.primaryZoneId);
            if (attackers.AliveCount > 0 && defenders.AliveCount > 0)
            {
                float plantChance = Clamp(0.20f + request.attackerPlan.plantPriority * 0.55f +
                                          (attackers.AliveCount - defenders.AliveCount) * 0.06f, 0.10f, 0.90f);
                bombPlanted = random.Next01() < plantChance;
                time = Math.Min(92f, time + 12f);
                if (bombPlanted)
                {
                    PlayerRuntime carrier = SelectBombCarrier(attackers);
                    Add(result, request, time - 0.5f, EventType.Move, carrier.Definition.id, null,
                        plantSiteId, "Moving to plant");
                    Add(result, request, time, EventType.BombPlanted, carrier.Definition.id, null,
                        plantSiteId, "Bomb planted");
                }
            }

            while (attackers.AliveCount > 0 && defenders.AliveCount > 0 && time < (bombPlanted ? 125f : 105f))
            {
                time += 8f + random.Next01() * 8f;
                ResolveContact(request, result,
                    attackers.AliveCount <= 2 || defenders.AliveCount <= 2 ? CombatPhase.Clutch :
                    bombPlanted ? CombatPhase.Retake : CombatPhase.MidRound, time, random);
            }

            TeamRuntime winner;
            TeamRuntime loser;
            string reason;
            if (attackers.AliveCount == 0)
            {
                winner = defenders;
                loser = attackers;
                reason = "elimination";
            }
            else if (defenders.AliveCount == 0)
            {
                winner = attackers;
                loser = defenders;
                reason = bombPlanted ? "bomb_exploded" : "elimination";
                if (bombPlanted)
                    Add(result, request, time + 3f, EventType.BombExploded, null, null,
                        plantSiteId, "Bomb exploded");
            }
            else if (!bombPlanted)
            {
                winner = defenders;
                loser = attackers;
                reason = "timeout";
                float timeoutTime = Math.Max(115f, result.events.Max(x => x.roundTime) + 1f);
                Add(result, request, timeoutTime, EventType.Timeout, null, null, request.attackerPlan.primaryZoneId,
                    "Round timer expired");
            }
            else
            {
                float retakeScore = (float)defenders.AlivePlayers.Average(x => x.Definition.attributes.clutch + x.Definition.attributes.utility) / 2f;
                float holdScore = (float)attackers.AlivePlayers.Average(x => x.Definition.attributes.clutch + x.Definition.attributes.awareness) / 2f;
                float defuseChance = Clamp(0.5f + (retakeScore - holdScore) / 100f +
                                           (defenders.AliveCount - attackers.AliveCount) * 0.08f, 0.12f, 0.88f);
                if (random.Next01() < defuseChance)
                {
                    winner = defenders;
                    loser = attackers;
                    reason = "bomb_defused";
                    Add(result, request, time + 5f, EventType.BombDefused,
                        defenders.AlivePlayers.First().Definition.id, null, plantSiteId, "Bomb defused");
                }
                else
                {
                    winner = attackers;
                    loser = defenders;
                    reason = "bomb_exploded";
                    Add(result, request, time + 5f, EventType.BombExploded, null, null,
                        plantSiteId, "Bomb exploded");
                }
            }

            result.winnerTeamId = winner.Definition.id;
            result.winnerSide = winner.Side;
            result.winReason = reason;
            result.attackerAlive = attackers.AliveCount;
            result.defenderAlive = defenders.AliveCount;
            ApplyRoundState(request, winner);
            EconomyRules.Settle(winner, loser, reason);
            float roundEndTime = result.events.Count == 0 ? time + 8f : result.events.Max(x => x.roundTime) + 3f;
            EmitStatusSnapshot(request, result, roundEndTime - 0.5f);
            Add(result, request, roundEndTime, EventType.RoundEnded, null, null,
                request.attackerPlan.primaryZoneId, $"{winner.Definition.displayName} wins by {reason}", winner.Definition.id,
                null,
                new[]
                {
                    "money:" + request.attackers.Definition.id + ":" + request.attackers.Money,
                    "money:" + request.defenders.Definition.id + ":" + request.defenders.Money
                });
            result.events = result.events
                .OrderBy(x => x.roundTime)
                .ThenBy(x => x.sequence)
                .Select((evt, index) =>
                {
                    evt.sequence = index;
                    return evt;
                })
                .ToList();
            return result;
        }

        private void ResolveContact(RoundSimulationRequest request, RoundResult result, CombatPhase phase, float time, IRandomSource random)
        {
            if (request.attackers.AliveCount == 0 || request.defenders.AliveCount == 0)
                return;

            PlayerRuntime attacker = SelectPlayer(
                request.attackers, request.attackerPlan, phase, request.attackerPlan.primaryZoneId, random);
            PlayerRuntime defender = SelectPlayer(
                request.defenders, request.defenderPlan, phase, request.defenderPlan.primaryZoneId, random);
            bool attackerInitiative = random.Next01() < Clamp(
                0.5f + (request.attackerPlan.aggression - request.defenderPlan.aggression) * 0.18f +
                (request.attackerPlan.initiativeBias - request.defenderPlan.initiativeBias) * 0.12f,
                0.20f, 0.80f);
            if (defender.Definition.traitIds.Contains("experience_intuition") && random.Next01() < 0.20f)
            {
                attackerInitiative = false;
                Add(result, request, time - 0.1f, EventType.TraitTriggered, defender.Definition.id, null,
                    request.attackerPlan.primaryZoneId, "experience_intuition",
                    TeamFor(request, defender).Definition.id);
            }
            MapZoneDefinition zone = request.map.zones.First(x => x.id == request.attackerPlan.primaryZoneId);
            float moveTime = time - 2.2f;
            if (attacker.ZoneId != zone.id)
            {
                Add(result, request, moveTime, EventType.Move, attacker.Definition.id, null, zone.id,
                    $"{attacker.Definition.id} enters {zone.displayName}", request.attackers.Definition.id);
                attacker.ZoneId = zone.id;
            }
            if (defender.ZoneId != zone.id)
            {
                Add(result, request, moveTime + 0.05f, EventType.Move, defender.Definition.id, null, zone.id,
                    $"{defender.Definition.id} enters {zone.displayName}", request.defenders.Definition.id);
                defender.ZoneId = zone.id;
            }
            Add(result, request, time, EventType.Contact, attacker.Definition.id, defender.Definition.id, zone.id,
                $"{attacker.Definition.id} contacts {defender.Definition.id}");

            var outcome = _encounters.Resolve(new EncounterContext
            {
                match = request.match,
                teamA = request.attackers,
                teamB = request.defenders,
                playerA = attacker,
                playerB = defender,
                phase = phase,
                zone = zone,
                planA = request.attackerPlan,
                planB = request.defenderPlan,
                playerAHasInitiative = attackerInitiative,
                roundNumber = request.roundNumber
            }, random);

            if (!outcome.resolved)
                return;

            outcome.victim.Alive = false;
            outcome.victim.Deaths++;
            int deadThisRound = request.attackers.Players.Concat(request.defenders.Players).Count(p => !p.Alive);
            bool openedDeath = deadThisRound == 1;
            float deathLoss = openedDeath
                ? FormRules.OpenedDeathLoss
                : FormRules.DeathLossMin + random.Next01() * (FormRules.DeathLossMax - FormRules.DeathLossMin);
            outcome.victim.Form = FormRules.Clamp(outcome.victim.Form - deathLoss);

            outcome.killer.Kills++;
            outcome.killer.RoundKills++;
            float killGain = FormRules.KillGainMin +
                             random.Next01() * (FormRules.KillGainMax - FormRules.KillGainMin);
            if (outcome.killer.RoundKills >= 2)
                killGain += FormRules.MultiKillBonusPerExtra;
            outcome.killer.Form = FormRules.Clamp(outcome.killer.Form + killGain);

            ApplyTacticalDeathRules(request, outcome.victim);
            FocusSpecialRules.ApplyDeath(request, outcome.victim);

            TeamRuntime killerTeam = TeamFor(request, outcome.killer);
            var tags = new List<string>(outcome.reasonTags ?? Array.Empty<string>());
            PlayerRuntime assister = killerTeam.AlivePlayers
                .Where(mate => !ReferenceEquals(mate, outcome.killer) &&
                               string.Equals(mate.ZoneId, zone.id, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(mate => mate.Definition.attributes.teamwork + mate.Definition.attributes.awareness)
                .FirstOrDefault();
            // Same-zone teammate is eligible, but only award assist half the time.
            if (assister != null && random.Next01() < 0.5f)
            {
                assister.Assists++;
                tags.Add("assist:" + assister.Definition.id);
            }

            string weapon = WeaponFor(outcome.killer, killerTeam.Equipment, random);
            int killReward = EconomyRules.ApplyKillReward(killerTeam, weapon);
            tags.Add("kill_reward:" + killReward);
            tags.Add("team_money:" + killerTeam.Money);
            string text = $"{outcome.killer.Definition.id} eliminated {outcome.victim.Definition.id}" +
                          (outcome.headshot ? " (headshot)" : string.Empty);
            Add(result, request, time + 0.5f, EventType.Kill, outcome.killer.Definition.id,
                outcome.victim.Definition.id, zone.id, text, killerTeam.Definition.id, weapon, tags.ToArray());

            float traitTime = time + 0.15f;
            foreach (TraitProc proc in outcome.traitProcs ?? Enumerable.Empty<TraitProc>())
            {
                if (Math.Abs(proc.bonus) < 2.4f)
                    continue;
                string sign = proc.bonus >= 0f ? "+" : string.Empty;
                Add(result, request, traitTime, EventType.TraitTriggered, proc.playerId, null, zone.id,
                    $"{proc.traitId}|{sign}{proc.bonus:0.#}",
                    ResolvePlayerTeamId(request, proc.playerId));
                traitTime += 0.02f;
            }
        }

        private static string ResolvePlayerTeamId(RoundSimulationRequest request, string playerId)
        {
            if (request.attackers.Players.Any(p => p.Definition.id.Equals(playerId, StringComparison.OrdinalIgnoreCase)))
                return request.attackers.Definition.id;
            return request.defenders.Definition.id;
        }

        private static PlayerRuntime SelectPlayer(
            TeamRuntime team,
            TacticalPlan plan,
            CombatPhase phase,
            string zone,
            IRandomSource random)
        {
            List<PlayerRuntime> alive = team.AlivePlayers.ToList();
            float total = alive.Sum(x => SelectionWeight(x, plan, phase, zone));
            float roll = random.Next01() * total;
            foreach (PlayerRuntime player in alive)
            {
                roll -= SelectionWeight(player, plan, phase, zone);
                if (roll <= 0f)
                    return player;
            }
            return alive[alive.Count - 1];
        }

        private static float SelectionWeight(
            PlayerRuntime player,
            TacticalPlan plan,
            CombatPhase phase,
            string zone)
        {
            float weight = 1f;
            string role = player.Definition.primaryRole;
            if (phase == CombatPhase.Opening && role == "Entry") weight += 3f;
            if (phase == CombatPhase.Opening && role == "Awper") weight += 2f;
            if (phase == CombatPhase.Trade && (role == "Rifler" || role == "Support")) weight += 2f;
            if (phase == CombatPhase.Clutch) weight += player.Definition.attributes.clutch / 50f;
            if ((zone == "a_long" || zone == "mid" || zone == "xbox") && role == "Awper") weight += 1.5f;
            if (plan.specialRuleKeys.Contains("s1mple_anchor") && player.Definition.id == "s1mple")
                weight += phase == CombatPhase.Opening ? 4f : 2f;
            if (plan.specialRuleKeys.Contains("electronic_duo") &&
                (player.Definition.id == "electronic" || player.Definition.id == "s1mple"))
                weight += 2.5f;
            if (plan.specialRuleKeys.Contains("b1t_death_penalty") && player.Definition.id == "b1t")
                weight += phase == CombatPhase.Opening ? 3f : 1f;
            if (plan.specialRuleKeys.Contains("nafany_igl_risk") && player.Definition.id == "nafany")
                weight += phase == CombatPhase.Opening ? 4f : 1f;
            if (plan.specialRuleKeys.Contains("sh1ro_survival") && player.Definition.id == "sh1ro")
                weight *= phase == CombatPhase.Clutch ? 1.8f : 0.45f;
            if (plan.specialRuleKeys.Contains("perfecto_clutch_only") && player.Definition.id == "perfecto")
                weight *= phase == CombatPhase.Clutch ? 2f : 0.45f;
            if (plan.specialRuleKeys.Contains("ax1le_lurk") && player.Definition.id == "Ax1Le")
                weight *= phase == CombatPhase.Opening ? 0.35f : 2f;
            return weight;
        }

        private static void ApplyTacticalDeathRules(RoundSimulationRequest request, PlayerRuntime victim)
        {
            TeamRuntime team = TeamFor(request, victim);
            TacticalPlan plan = team == request.attackers ? request.attackerPlan : request.defenderPlan;
            TacticalSpecialRules.ApplyDeath(team, plan, victim.Definition.id);
        }

        private static TeamRuntime TeamFor(RoundSimulationRequest request, PlayerRuntime player) =>
            request.attackers.Players.Contains(player) ? request.attackers : request.defenders;

        private static PlayerRuntime SelectBombCarrier(TeamRuntime attackers) =>
            attackers.AlivePlayers.OrderByDescending(x => x.Definition.attributes.awareness + x.Definition.attributes.teamwork).First();

        private static string WeaponFor(PlayerRuntime player, EquipmentLevel equipment, IRandomSource random)
        {
            if (random.Next01() < EconomyRules.KnifeKillChance)
                return "knife";
            if (equipment == EquipmentLevel.Hero)
            {
                if (player.Definition.primaryRole == "Awper")
                    return "awp";
                return "pistol";
            }
            if (equipment == EquipmentLevel.Eco || equipment == EquipmentLevel.HalfBuy)
                return "pistol";
            if (equipment == EquipmentLevel.ForceBuy)
                return "smg";
            if (player.Definition.primaryRole == "Awper" && equipment == EquipmentLevel.FullBuy)
                return "awp";
            return "rifle";
        }

        private void ActivateRoundTraits(RoundSimulationRequest request, RoundResult result, IRandomSource random)
        {
            RoundTraitRules.ActivateRoundStart(request, result, random);
            _sequence = Math.Max(_sequence, result.events.Count);
        }

        private void EmitStatusSnapshot(RoundSimulationRequest request, RoundResult result, float time)
        {
            foreach (PlayerRuntime player in request.attackers.Players.Concat(request.defenders.Players))
            {
                Add(result, request, time, EventType.StatusUpdated, player.Definition.id, null, null,
                    $"{player.Stamina:0}|{player.Mentality:0}|{player.Form:0}",
                    ResolvePlayerTeamId(request, player.Definition.id));
                time += 0.001f;
            }
        }

        private static void ApplyRoundState(RoundSimulationRequest request, TeamRuntime winner)
        {
            foreach (TeamRuntime team in new[] { request.attackers, request.defenders })
            {
                bool won = team == winner;
                bool ecoLoss = !won &&
                    (team.Equipment == EquipmentLevel.Eco || team.Equipment == EquipmentLevel.HalfBuy);
                foreach (PlayerRuntime player in team.Players)
                {
                    player.Stamina = Clamp(player.Stamina - (player.Definition.primaryRole == "Igl" ? 2f : 1.5f), 0f, 100f);
                    player.Mentality = Clamp(player.Mentality + (won ? 2f : -2f), 0f, 100f);
                    if (player.RoundKills > 0)
                    {
                        player.ConsecutiveKillRounds++;
                        if (player.Definition.traitIds.Contains("red_star_child") && player.Alive)
                            player.TraitStacks = Math.Min(5, player.TraitStacks + 1);
                    }
                    else
                    {
                        player.ConsecutiveKillRounds = 0;
                    }
                    if (!player.Alive && player.Definition.traitIds.Contains("red_star_child"))
                        player.TraitStacks = 0;
                    if (player.SpecialState && !won)
                    {
                        player.Mentality = Clamp(player.Mentality - 5f, 0f, 100f);
                        player.Form = FormRules.Clamp(player.Form - 5f);
                    }
                    if (ecoLoss)
                        player.Form = FormRules.Clamp(player.Form - FormRules.EcoRoundTeamLoss);
                }

                if (won && team.AliveCount == 1)
                {
                    PlayerRuntime clutch = team.Players.FirstOrDefault(p => p.Alive);
                    if (clutch != null)
                        clutch.Form = FormRules.Clamp(clutch.Form + FormRules.ClutchWinGain);
                }
            }
        }

        private void EmitDeploymentMoves(
            RoundSimulationRequest request,
            RoundResult result,
            TeamRuntime team,
            TacticalPlan plan,
            bool attacking,
            float startTime,
            int indexOffset,
            IRandomSource random)
        {
            string mapId = request.map?.id;
            string primary = ContentDatabase.ClampZoneToMap(request.map, plan.primaryZoneId);
            string[] attackZones = MapDeploymentRules.AttackDeploymentZones(mapId, primary);
            string[] defendZones = MapDeploymentRules.DefendDeploymentZones(mapId, primary);
            string[] zones = attacking ? attackZones : defendZones;
            for (int i = 0; i < team.Players.Count; i++)
            {
                PlayerRuntime player = team.Players[i];
                string target = ContentDatabase.ClampZoneToMap(
                    request.map,
                    MapDeploymentRules.AssignDeployZone(mapId, player.Definition.primaryRole, primary, zones, attacking, i));
                player.ZoneId = target;
                float t = startTime + (indexOffset + i) * 0.08f;
                Add(result, request, t, EventType.Move, player.Definition.id, null, target,
                    $"{player.Definition.id} moves to {target}", team.Definition.id);
            }
        }

        private void Add(
            RoundResult result, RoundSimulationRequest request, float time, EventType type,
            string actor, string target, string zone, string message, string team = null,
            string weapon = null, string[] tags = null)
        {
            result.events.Add(new SimulationEvent
            {
                sequence = _sequence++,
                roundNumber = request.roundNumber,
                roundTime = time,
                type = type,
                actorId = actor,
                targetId = target,
                teamId = team,
                zoneId = zone,
                weaponId = weapon,
                message = message,
                reasonTags = tags ?? Array.Empty<string>()
            });
        }

        private static float Clamp(float value, float min, float max) =>
            Math.Max(min, Math.Min(max, value));
    }

    /// <summary>
    /// Round-opening trait procs shared by Quick and BehaviorTree simulators
    /// so general-mode battles still feed the 特性觸發 panel.
    /// </summary>
    public static class RoundTraitRules
    {
        public static void ActivateRoundStart(
            RoundSimulationRequest request, RoundResult result, IRandomSource random)
        {
            float t = 0.05f;
            int sequence = result.events.Count;
            foreach (PlayerRuntime player in request.attackers.Players.Concat(request.defenders.Players))
            {
                string[] traits = player.Definition.traitIds ?? Array.Empty<string>();
                string teamId = ResolvePlayerTeamId(request, player.Definition.id);
                if (traits.Contains("pyramid_solution") && random.Next01() < 0.30f)
                {
                    player.SpecialState = true;
                    Add(result, request, ref sequence, t, player.Definition.id, null, null,
                        "pyramid_solution", teamId);
                    t += 0.02f;
                }
                if (request.roundNumber == 1 && traits.Contains("unlucky") && random.Next01() < 0.15f)
                {
                    player.States.Add("unlucky");
                    Add(result, request, ref sequence, t, player.Definition.id, null, null,
                        "unlucky", teamId);
                    t += 0.02f;
                }
                if (request.roundNumber == 6 && player.States.Contains("unlucky"))
                {
                    TeamRuntime team = request.attackers.Players.Contains(player)
                        ? request.attackers
                        : request.defenders;
                    int scoreDifference = Math.Abs(
                        request.match.ScoreFor(team.Definition.id) -
                        request.match.ScoreAgainst(team.Definition.id));
                    if (scoreDifference <= 2)
                    {
                        player.States.Add("adversity_awakened");
                        Add(result, request, ref sequence, t, player.Definition.id, null, null,
                            "adversity_awakened", teamId);
                        t += 0.02f;
                    }
                    player.States.Remove("unlucky");
                }
            }

            PlayerRuntime s1mple = request.attackers.Players.Concat(request.defenders.Players)
                .FirstOrDefault(x => x.Definition.id == "s1mple");
            if (s1mple == null)
                return;
            string[] s1Traits = s1mple.Definition.traitIds ?? Array.Empty<string>();
            if (!s1Traits.Contains("forest_intimidation"))
                return;

            TeamRuntime opponents = request.attackers.Players.Contains(s1mple)
                ? request.defenders
                : request.attackers;
            foreach (PlayerRuntime opponent in opponents.Players.Where(x => x.Definition.prestige < 80))
            {
                opponent.Mentality = Clamp(opponent.Mentality - 2f, 0f, 100f);
                bool formDrop = opponent.Mentality <= 75f || opponent.Form <= 75f;
                if (formDrop)
                    opponent.Form = Clamp(opponent.Form - 2f, 0f, 100f);
                string effect = formDrop ? "心態 -2、手感 -2" : "心態 -2";
                Add(result, request, ref sequence, t, s1mple.Definition.id, opponent.Definition.id, null,
                    $"forest_intimidation|{effect}",
                    ResolvePlayerTeamId(request, s1mple.Definition.id));
                t += 0.02f;
            }

            const string teamDebuffFlag = "forest_intimidation_team_debuff";
            bool kdAboveThreshold = s1mple.Deaths == 0
                ? s1mple.Kills > 0
                : (float)s1mple.Kills / s1mple.Deaths > 1.3f;
            if (s1mple.Kills >= 20 && kdAboveThreshold && !s1mple.States.Contains(teamDebuffFlag))
            {
                s1mple.States.Add(teamDebuffFlag);
                foreach (PlayerRuntime opponent in opponents.Players)
                {
                    opponent.Mentality = Clamp(opponent.Mentality - 5f, 0f, 100f);
                    opponent.Form = Clamp(opponent.Form - 5f, 0f, 100f);
                }

                Add(result, request, ref sequence, t, s1mple.Definition.id, null, null,
                    "forest_intimidation|對手全隊心態 -5、手感 -5（僅一次）",
                    ResolvePlayerTeamId(request, s1mple.Definition.id));
            }
        }

        public static void EmitKillTrait(
            RoundSimulationRequest request,
            RoundResult result,
            PlayerRuntime killer,
            PlayerRuntime victim,
            string zoneId,
            bool headshot,
            float time,
            IRandomSource random)
        {
            if (killer?.Definition?.traitIds == null)
                return;
            if (!headshot || !killer.Definition.traitIds.Contains("headshot_machine"))
                return;
            int sequence = result.events.Count;
            Add(result, request, ref sequence, time, killer.Definition.id, victim?.Definition.id, zoneId,
                "headshot_machine|+20", ResolvePlayerTeamId(request, killer.Definition.id));
        }

        private static void Add(
            RoundResult result, RoundSimulationRequest request, ref int sequence, float time,
            string actor, string target, string zone, string message, string team)
        {
            result.events.Add(new SimulationEvent
            {
                sequence = sequence++,
                roundNumber = request.roundNumber,
                roundTime = time,
                type = EventType.TraitTriggered,
                actorId = actor,
                targetId = target,
                teamId = team,
                zoneId = zone,
                message = message,
                reasonTags = Array.Empty<string>()
            });
        }

        private static string ResolvePlayerTeamId(RoundSimulationRequest request, string playerId)
        {
            if (request.attackers.Players.Any(p =>
                    p.Definition.id.Equals(playerId, StringComparison.OrdinalIgnoreCase)))
                return request.attackers.Definition.id;
            return request.defenders.Definition.id;
        }

        private static float Clamp(float value, float min, float max) =>
            Math.Max(min, Math.Min(max, value));
    }
}
