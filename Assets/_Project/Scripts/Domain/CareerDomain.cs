using System;
using System.Collections.Generic;
using System.Linq;

namespace TacticaUniversalis.Domain
{
    public enum CareerDifficulty { Normal, Hard, Hardcore }

    /// <summary>Difficulty rules and player-facing copy. Some features are gated until implemented.</summary>
    public static class CareerDifficultyRules
    {
        /// <summary>When false, all modes show full player stats (legacy bypass).</summary>
        public const bool PlayerStatScoutingEnabled = true;

        public static bool AllowsManualSave(CareerDifficulty difficulty) => true;

        public static bool AllowsManualLoad(CareerDifficulty difficulty) =>
            difficulty != CareerDifficulty.Hardcore;

        public static bool UsesIronmanAutoSave(CareerDifficulty difficulty) =>
            difficulty == CareerDifficulty.Hardcore;

        /// <summary>Master switch — achievements are currently disabled for all difficulties.</summary>
        public const bool AchievementsEnabled = false;

        /// <summary>Achievements unlock only while playing on Hardcore when enabled.</summary>
        public static bool AchievementsEligible(CareerDifficulty difficulty) =>
            AchievementsEnabled && difficulty == CareerDifficulty.Hardcore;

        public static bool UsesAttributeScouting(CareerDifficulty difficulty) =>
            PlayerStatScoutingEnabled && difficulty != CareerDifficulty.Normal;

        /// <summary>Own roster always visible; opponents on Hard/Hardcore need an analyst on staff.</summary>
        public static bool CanRevealOpponentAttributes(CareerState state, string viewerClubId)
        {
            if (state == null || !UsesAttributeScouting(state.difficulty))
                return true;
            return ClubCoachStaffRules.FindByRole(state, viewerClubId, CoachStaffRole.Analyst) != null;
        }

        /// <summary>
        /// Format one combat attribute for UI. Own players always exact;
        /// Hard/Hardcore opponents need an analyst, then reveal by dataInvestigation fraction.
        /// </summary>
        public static string FormatVisibleAttribute(
            CareerState state,
            string viewerClubId,
            string targetClubId,
            int value,
            int attributeIndex,
            int attributeCount = 8)
        {
            if (state == null ||
                string.Equals(viewerClubId, targetClubId, StringComparison.OrdinalIgnoreCase) ||
                !UsesAttributeScouting(state.difficulty))
                return value.ToString();

            if (!CanRevealOpponentAttributes(state, viewerClubId))
                return "需分析師";

            int count = Math.Max(1, attributeCount);
            int index = Math.Max(0, attributeIndex);
            double fraction = OpponentStatRevealFraction(state, viewerClubId);
            int revealed = Math.Max(1, (int)Math.Round(count * fraction));
            return index < revealed ? value.ToString() : "—";
        }

        public static string DisplayName(CareerDifficulty difficulty) =>
            difficulty switch
            {
                CareerDifficulty.Hard => "困難",
                CareerDifficulty.Hardcore => "硬核",
                _ => "普通"
            };

        public static string HoverDescription(CareerDifficulty difficulty)
        {
            switch (difficulty)
            {
                case CareerDifficulty.Hard:
                    return
                        "• 可自由存檔與讀檔。\n" +
                        "• 他隊選手能力需透過分析師調查後才會逐步揭露。\n" +
                        "• 揭露深度依分析師「數據調查」等能力。\n" +
                        "• 自家新加入的選手也要打過數場比賽後，能力值才會趨於準確。";
                case CareerDifficulty.Hardcore:
                    return
                        "• 包含困難模式的所有情報限制。\n" +
                        "• 無法讀取存檔（僅自動存檔延續同一條 Ironman 生涯）。";
                default:
                    return
                        "• 可自由存檔與讀檔。\n" +
                        "• 所有選手的能力值完整可見。\n\n" +
                        "適合熟悉系統與編排戰術。";
            }
        }

        public static string FormatVisiblePotential(CareerDifficulty difficulty, int potential)
        {
            if (!PlayerStatScoutingEnabled || difficulty == CareerDifficulty.Normal)
                return potential.ToString();

            if (difficulty == CareerDifficulty.Hard)
                return $"{Math.Max(0, potential - 8)}–{Math.Min(100, potential + 8)}";

            return potential >= 90 ? "頂級" :
                potential >= 80 ? "優秀" :
                potential >= 70 ? "良好" : "普通";
        }

        /// <summary>
        /// Hard/Hardcore: fraction of opponent player stats revealed by analyst data investigation.
        /// Normal (or scouting disabled path) returns 1. See ClubCoachStaffRules.HardModeScoutRevealFraction.
        /// </summary>
        public static double OpponentStatRevealFraction(CareerState state, string clubId) =>
            ClubCoachStaffRules.HardModeScoutRevealFraction(state, clubId);
    }

    public enum FinancialHealth { Wealthy, Healthy, Balanced, Deficit, Crisis, Bankrupt }
    public enum SponsorTier { Local, Regional, International, Elite }
    public enum FacilityType { Training, Academy, Medical, Data }
    /// <summary>Tier ladder: Exhibition &lt; D &lt; C &lt; B &lt; A &lt; Elite &lt; S &lt; SuperElite &lt; Major.
    /// Elite is reserved for future imports (numeric value appended to keep JSON stable).</summary>
    public enum CompetitionTier { Exhibition, D, C, B, A, S, Major, SuperElite, Elite }
    public enum CompetitionFormat { Online, LAN, Hybrid, Cancelled }

    /// <summary>主辦方差旅補貼檔次。None 表示全自理。</summary>
    public enum CompetitionTravelSubsidy
    {
        None = 0,
        FlightEconomy = 1,
        FlightBusiness = 2,
        FlightAndHotelEconomy = 3,
        FlightAndHotelBusiness = 4
    }

    public enum FlightClass
    {
        Economy = 0,
        PremiumEconomy = 1,
        Business = 2
    }
    public enum TransferOfferStatus
    {
        Pending,
        Accepted,
        Rejected,
        Countered,
        Expired,
        ClubAgreed,
        PersonalTerms,
        Outbid,
        Failed
    }
    public enum CareerEventType
    {
        WeekAdvanced, MonthClosed, MatchCompleted, PrizeAwarded, SponsorChanged,
        TrainingApplied, PlayerInjured, ContractChanged, TransferCompleted,
        RankingUpdated, FacilityChanged, FinancialStatusChanged, HoursAdvanced,
        ScriptedEvent, AchievementUnlocked, CommunityPost
    }

    [Serializable]
    public sealed class CareerBalanceDefinition
    {
        public int schemaVersion = 1;
        public int startingYear = 2021;
        public int startingMonth = 1;
        public int startingDay = 1;
        /// <summary>Hour of day at career start (0..23).</summary>
        public int startingHour = 0;
        /// <summary>Hours between training / inactivity ticks (default one week).</summary>
        public int hoursPerTrainingTick = 168;
        public int baseTrainingPointsMilli = 1200;
        /// <summary>
        /// Weeks with neither match nor training before proficiency/attribute rust.
        /// 0 disables rust.
        /// </summary>
        public int inactivityRustWeeks = 8;
        /// <summary>Proficiency points lost per rust tick (role + map).</summary>
        public int inactivityRustProficiencyLoss = 2;
        /// <summary>Attribute / power points lost per rust tick (small).</summary>
        public int inactivityRustAttributeLoss = 1;
        public int marketValueMinimum = 10000;
        public int marketValueMaximum = 15000000;
        public int rankingHalfLifeHours = 4368; // 26 * 168
        public int rankingWindowHours = 8736;  // 52 * 168
        public int facilityBaseUpgradeCost = 100000;
        public int facilityBaseMaintenance = 3000;
        public int startingCash = 1000000;

        // Legacy fields kept so old JSON still deserializes; prefer hour-based fields above.
        public int weeksPerYear = 52;
        public int rankingHalfLifeWeeks = 26;
        public int rankingWindowWeeks = 52;
    }

    [Serializable]
    public sealed class CareerPlayerDefinition
    {
        public string id;
        public string displayName;
        public int age;
        public string role;
        public string clubId;
        public int power;
        public int potential;
        public int fame;
        public int loyalty;
        public int professionalism;
        public string trajectory;
        public int monthlySalary;
        public int marketValue;
        /// <summary>Calendar year the player turned pro. 0 = estimate from age at career start.</summary>
        public int debutYear;
        /// <summary>starter | bench | academy | promoted</summary>
        public string squadStatus = "starter";
    }

    [Serializable]
    public sealed class CareerClubDefinition
    {
        public string id;
        public string displayName;
        public string region;
        /// <summary>Stockholm 2021 RMR region: Europe / NorthAmerica / CIS / SouthAmerica / Asia / Oceania.</summary>
        public string rmrRegion;
        /// <summary>RMR Circuit 2020 seed points. 0 = derive from hltvPoints bands.</summary>
        public int rmrCircuitSeed;
        /// <summary>HLTV world rank snapshot (2021-01-04).</summary>
        public int hltvRank;
        /// <summary>HLTV ranking points at career start.</summary>
        public int hltvPoints;
        public int fame;
        public int ambition;
        public int startingCash;
        /// <summary>Listed / public-company ownership unlocks the Board estate.</summary>
        public bool isPublicCompany;
        public string[] playerIds;
    }

    [Serializable]
    public sealed class SponsorDefinition
    {
        public string id;
        public string displayName;
        public SponsorTier tier;
        public int baseMonthlyFee;
        public int baseResultBonus;
        public int minimumFame;
        public int minimumRankPercentile;
        public int minimumExposure;
    }

    [Serializable]
    public sealed class CompetitionDefinition
    {
        public string id;
        public string displayName;
        public CompetitionTier tier;
        public CompetitionFormat format;
        /// <summary>Absolute hour from career epoch when the event starts.</summary>
        public int startHour;
        /// <summary>Absolute hour from career epoch when the event ends (inclusive).</summary>
        public int endHour;
        public int prizePool;
        public int entryFee;
        public int maximumTeams;
        public int maximumWorldRank;
        public string region;
        public bool invitationOnly;
        public string locationDisplay;
        public string country;
        public string city;
        /// <summary>Optional organizer id; when empty, inferred from competition id.</summary>
        public string organizer;
        /// <summary>LAN 差旅補貼；0 時依 tier 推斷。</summary>
        public CompetitionTravelSubsidy travelSubsidy;
        /// <summary>提前抵達天數；0 表示依跨區距離自動計算。</summary>
        public int earlyArrivalDays;
        /// <summary>Stockholm RMR round: Circuit2020 / Rmr1 / Rmr2 / Rmr3. Empty = not an RMR event.</summary>
        public string rmrRound;
        /// <summary>RMR region this event feeds. Empty = resolve from known id map.</summary>
        public string rmrRegion;
        /// <summary>Empty = manual/world-rank signup. "Rmr" = auto-qualify from regional Major rankings.</summary>
        public string qualificationMode;
    }

    [Serializable]
    public sealed class FacilityDefinition
    {
        public FacilityType type;
        public int baseUpgradeCost;
        public int baseMaintenance;
        /// <summary>Upgrade duration in hours indexed by target level (1-based uses index targetLevel-1).</summary>
        public int[] upgradeHours;
        // Legacy: weeks; converted to hours when upgradeHours is empty.
        public int[] upgradeWeeks;
    }

    [Serializable]
    public sealed class ContractState
    {
        public string id;
        public string playerId;
        public string clubId;
        public int startYear;
        public int startHour;
        public int remainingMonths;
        public int monthlySalary;
        public int signingBonus;
        public int releaseClause;
        public float prizeShare;
        public float appearanceGuarantee;
        public bool noTrade;
        public bool playerOption;
        public string agentId;
    }

    [Serializable]
    public sealed class CareerNamedStat
    {
        public string id;
        /// <summary>0–100, one-decimal proficiency / attribute progress.</summary>
        public float value;
        /// <summary>
        /// Role/map fitness grade. Null/empty for combat attributes.
        /// Ladder: E D D+ C C+ B B+ A A+ S.
        /// </summary>
        public string grade;
    }

    [Serializable]
    public sealed class PlayerCareerState
    {
        public string playerId;
        public string clubId;
        public int age;
        public int power;
        public int potential;
        public int fame;
        public int loyalty;
        /// <summary>0–100 mental load. Seeded ~40; scripted crises can raise it.</summary>
        public int pressure = 40;
        public int health = 100;
        public int energy = 100;
        public int form = 50;
        public int experience;
        public int level;
        public float trainingProgress;
        /// <summary>Legacy aggregate of role training proficiency; prefer roleProficiencies. Default 0.</summary>
        public float roleProficiency;
        /// <summary>Legacy aggregate of map training proficiency; prefer mapProficiencies. Default 0.</summary>
        public float mapProficiency;
        public List<CareerNamedStat> attributes = new List<CareerNamedStat>();
        public List<CareerNamedStat> roleProficiencies = new List<CareerNamedStat>();
        public List<CareerNamedStat> mapProficiencies = new List<CareerNamedStat>();
        public int hoursWithoutMatch;
        public int hoursWithoutTraining;
        public int careerMatches;
        public float recentRating = 1f;
        public float ratingTrend;
        public bool unknownGenius;
        public bool transferRequested;
        /// <summary>Serialized training plan: a:aim,opening|m:Mirage|r:Awper</summary>
        public string trainingFocus = "a:aim|m:Dust II|r:Rifler";
        public string trajectory = "standard";
        /// <summary>Pro debut calendar year. Used for veteran estate (7+ years).</summary>
        public int debutYear;
        /// <summary>starter | bench | academy | promoted — bench/academy/promoted form Reserves.</summary>
        public string squadStatus = "starter";
    }

    [Serializable]
    public sealed class SponsorContract
    {
        public string id;
        public string sponsorId;
        public string clubId;
        public SponsorTier tier;
        public int remainingMonths;
        public int monthlyFee;
        public int resultBonus;
        public int minimumExposure;
        public int minimumWorldRank;
        public int satisfaction = 60;
        public int consecutiveLowMonths;
    }

    [Serializable]
    public sealed class TransferOffer
    {
        public string id;
        public string playerId;
        public string buyerClubId;
        public string sellerClubId;
        public int fee;
        public int createdHour;
        public int expiresHour;
        /// <summary>Deadline for the current negotiation phase (personal terms window, etc.).</summary>
        public int phaseExpiresHour;
        public bool releaseClauseBid;
        public bool isLoan;
        public int loanMonths;
        public int buyOption;
        public int askedSalary;
        public int offeredSalary;
        public int signingBonus;
        public int contractMonths;
        public string outbidByOfferId;
        public int negotiationRound;
        public TransferOfferStatus status;
    }

    public enum CompetitionWithdrawReason
    {
        Voluntary = 0,
        VisaDenied = 1,
        PlayerIllness = 2
    }

    [Serializable]
    public sealed class CompetitionEntry
    {
        public string competitionId;
        public string clubId;
        public bool registered;
        public int registeredHour;
        public int placement;
        public bool completed;
        public bool withdrawn;
        public int withdrawnHour;
        public CompetitionWithdrawReason withdrawReason;
        public int travelDepartHour;
        public int travelArriveHour;
        public int travelOnSiteHour;
        public int travelReturnDepartHour;
        public int travelReturnArriveHour;
        public FlightClass travelFlightClass;
        public int travelClubCost;
        public int travelSubsidyAmount;
        public bool travelCostsPaid;
    }

    [Serializable]
    public sealed class CompetitionSignupBan
    {
        public string clubId;
        public string organizerId;
        public CompetitionTier tier;
        public string sourceCompetitionId;
        public int issuedHour;
        public bool consumed;
    }

    [Serializable]
    public sealed class RankingBatch
    {
        public string id;
        public string clubId;
        public string competitionId;
        public int earnedHour;
        public float points;
        public int placement;
        public CompetitionTier tier;
        public bool seedSnapshot;
    }

    [Serializable]
    public sealed class RankingEntry
    {
        public string clubId;
        public int rank;
        public float points;
        public float recentPoints;
    }

    [Serializable]
    public sealed class FacilityState
    {
        public FacilityType type;
        public int level = 1;
        public int targetLevel = 1;
        public int upgradeHoursRemaining;
        public int unpaidMonths;
        public bool disabled;
    }

    [Serializable]
    public sealed class LedgerEntry
    {
        public string id;
        public int year;
        public int clockHour;
        public string clubId;
        public string category;
        public int amount;
        public string sourceId;
        public string note;
    }

    [Serializable]
    public sealed class CareerEvent
    {
        public string id;
        public CareerEventType type;
        public int year;
        public int clockHour;
        public string clubId;
        public string subjectId;
        /// <summary>Headline shown in the news list orange column.</summary>
        public string title;
        /// <summary>Full body / detail text. List shows a one-line truncation.</summary>
        public string message;
        public string ledgerEntryId;
        /// <summary>Scripted / news tier; Unset treated as B for legacy rows.</summary>
        public ScriptedEventTier eventTier = ScriptedEventTier.Unset;
        /// <summary>D-tier public news flag.</summary>
        public bool publicVisible;
    }

    [Serializable]
    public sealed class ClubState
    {
        public string clubId;
        public int cash;
        public int fame;
        public int fans;
        public int exposure;
        public int boardSatisfaction = 60;
        /// <summary>Per-estate satisfaction (0–100). Board syncs with <see cref="boardSatisfaction"/>.</summary>
        public List<ClubEstateSatisfactionState> estateSatisfactions = new List<ClubEstateSatisfactionState>();
        public FinancialHealth financialHealth = FinancialHealth.Healthy;
        public int consecutiveLossMonths;
        public int consecutiveCrisisMonths;
        public int trailingExpense1;
        public int trailingExpense2;
        public int trailingExpense3;
        public int lastMonthNet;
        public List<FacilityState> facilities = new List<FacilityState>();
    }

    [Serializable]
    public sealed class CareerCompetitionRunState
    {
        public string competitionId;
        public List<string> currentRoundClubs = new List<string>();
        public List<string> roundWinners = new List<string>();
        public int pairIndex;
        public int roundIndex;
        public string pendingOpponentClubId;
        public int pendingMatchSeed;
        public bool awaitingPlayerMatch;
        public int playerPlacement;
        public string pendingPlayerSimTeamId;
        public string pendingOpponentSimTeamId;
        /// <summary>Interactive matches already resolved in this run (knockout).</summary>
        public int playerInteractiveMatchesResolved;
        /// <summary>When &gt; clockHour, next player match is scheduled but not yet due.</summary>
        public int playerMatchDueHour;
        /// <summary>Empty for normal knockout. stockholm_2021 for PGL Major structure.</summary>
        public string structureId;
        public MajorStageKind majorStage;
        public List<string> legendsSeedClubs = new List<string>();
        public List<MajorSwissStanding> swissStandings = new List<MajorSwissStanding>();
        public List<MajorSwissPairing> swissPairings = new List<MajorSwissPairing>();
        public int swissPairIndex;
        public int swissRoundIndex;
        public List<MajorPlacementRecord> majorPlacements = new List<MajorPlacementRecord>();
        /// <summary>BO3 map veto / series progress for the pending interactive match.</summary>
        public CareerMapSeriesState mapSeries = new CareerMapSeriesState();
    }

    [Serializable]
    public sealed class MajorPlacementRecord
    {
        public string clubId;
        public int placement;
    }

    /// <summary>Persisted knockout bracket results for HLTV-style UI (survives after run ends).</summary>
    [Serializable]
    public sealed class CompetitionBracketArchive
    {
        public string competitionId;
        public List<string> seedOrder = new List<string>();
        public List<CompetitionBracketMatchRecord> matches = new List<CompetitionBracketMatchRecord>();
    }

    [Serializable]
    public sealed class CompetitionBracketMatchRecord
    {
        public int roundIndex;
        public int pairIndex;
        public string clubAId;
        public string clubBId;
        public string winnerClubId;
        public int scoreA;
        public int scoreB;
    }

    [Serializable]
    public sealed class CareerState
    {
        public int schemaVersion = 1;
        public string saveId;
        public CareerDifficulty difficulty;
        public int seed;
        public int year;
        /// <summary>Absolute hours elapsed from career balance epoch.</summary>
        public int clockHour;
        public string playerClubId;
        public bool gameOver;
        public List<ClubState> clubs = new List<ClubState>();
        public List<PlayerCareerState> players = new List<PlayerCareerState>();
        public List<ContractState> contracts = new List<ContractState>();
        public List<SponsorContract> sponsors = new List<SponsorContract>();
        public List<TransferOffer> offers = new List<TransferOffer>();
        public List<CompetitionEntry> competitionEntries = new List<CompetitionEntry>();
        public List<CompetitionSignupBan> competitionSignupBans = new List<CompetitionSignupBan>();
        public List<RankingBatch> rankingBatches = new List<RankingBatch>();
        public List<RankingEntry> rankings = new List<RankingEntry>();
        /// <summary>Stockholm 2021 Regional Major Ranking point batches (no HLTV decay).</summary>
        public List<RmrPointBatch> rmrBatches = new List<RmrPointBatch>();
        /// <summary>Cached regional RMR table rebuilt after awards / deductions.</summary>
        public List<RmrEntry> rmrRankings = new List<RmrEntry>();
        public List<LedgerEntry> ledger = new List<LedgerEntry>();
        public List<CareerEvent> events = new List<CareerEvent>();
        public CareerCompetitionRunState competitionRun;
        /// <summary>Player interactive competitions waiting because another run is active.</summary>
        public List<string> queuedInteractiveCompetitionIds = new List<string>();
        public List<CompetitionBracketArchive> competitionBrackets = new List<CompetitionBracketArchive>();
        /// <summary>EU4-style flags (global or club-scoped).</summary>
        public List<CareerFlag> flags = new List<CareerFlag>();
        /// <summary>Scripted events awaiting a player option choice.</summary>
        public List<CareerPendingEvent> pendingEvents = new List<CareerPendingEvent>();
        /// <summary>Unlocked achievement ids (shared condition/flag infrastructure).</summary>
        public List<string> unlockedAchievements = new List<string>();
        /// <summary>Mission tree claim progress for the player club.</summary>
        public CareerMissionProgress missionProgress = new CareerMissionProgress();
        /// <summary>Official competition matches played by the player club.</summary>
        public int playerOfficialMatchesPlayed;
        /// <summary>Year*12+month when the player club last signed a sponsor (at most one sign per calendar month).</summary>
        public int lastSponsorSignMonthKey = -1;
        /// <summary>Player club community / operations post log (newest appended).</summary>
        public List<CommunityPostLogEntry> communityPosts = new List<CommunityPostLogEntry>();
        /// <summary>When true, publishes one 日常動態 per day if quota allows.</summary>
        public bool communityAutoDailyUpdate;
        /// <summary>Hired coaching staff (head coach / assistant / analyst).</summary>
        public List<CoachStaffState> coaches = new List<CoachStaffState>();

        public ClubState Club(string id) =>
            clubs.First(x => string.Equals(x.clubId, id, StringComparison.OrdinalIgnoreCase));

        public bool TryGetClub(string id, out ClubState club)
        {
            club = null;
            if (clubs == null || string.IsNullOrWhiteSpace(id))
                return false;
            club = clubs.FirstOrDefault(x =>
                x != null && string.Equals(x.clubId, id, StringComparison.OrdinalIgnoreCase));
            return club != null;
        }

        public PlayerCareerState Player(string id) =>
            players.First(x => string.Equals(x.playerId, id, StringComparison.OrdinalIgnoreCase));
    }

    public static class CareerMath
    {
        public static double Clamp(double value, double min, double max) =>
            Math.Max(min, Math.Min(max, value));

        public static int RoundMoney(double value) =>
            (int)Math.Round(value, MidpointRounding.AwayFromZero);

        public static double Sigmoid(double value) => 1d / (1d + Math.Exp(-value));

        public static double DifficultyIncome(CareerDifficulty difficulty) =>
            difficulty == CareerDifficulty.Hardcore ? .85d :
            difficulty == CareerDifficulty.Hard ? .92d : 1d;
    }

    /// <summary>
    /// Absolute hour clock from balance epoch. Simulation advances by hours; dates are derived.
    /// </summary>
    public static class CareerCalendar
    {
        public const int HoursPerDay = 24;
        public const int HoursPerWeek = 168;
        /// <summary>賽程表只顯示自此起約六個月內即將／進行中的賽事。</summary>
        public const int ScheduleHorizonHours = 184 * HoursPerDay;

        /// <summary>
        /// Placeholder date for the future scripted event that announces offline/LAN return.
        /// Major-tier events stay off the schedule until this day (or <see cref="OfflineReturnAnnouncedFlag"/>).
        /// </summary>
        public static readonly DateTime OfflineReturnAnnounceDate =
            new DateTime(2021, 9, 15, 0, 0, 0, DateTimeKind.Unspecified);

        /// <summary>Global flag set by the offline-return scripted event when authored.</summary>
        public const string OfflineReturnAnnouncedFlag = "lan_return_announced";

        public static DateTime Epoch(CareerBalanceDefinition balance)
        {
            if (balance == null)
                return new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
            int month = (int)CareerMath.Clamp(balance.startingMonth <= 0 ? 1 : balance.startingMonth, 1, 12);
            int day = (int)CareerMath.Clamp(balance.startingDay <= 0 ? 1 : balance.startingDay, 1, 28);
            int hour = (int)CareerMath.Clamp(balance.startingHour, 0, 23);
            return new DateTime(balance.startingYear, month, day, hour, 0, 0, DateTimeKind.Unspecified);
        }

        public static DateTime ToDateTime(CareerBalanceDefinition balance, int clockHour)
        {
            DateTime epoch = Epoch(balance);
            long hours = Math.Max(0, clockHour);
            return epoch.AddHours(hours);
        }

        public static DateTime ToDateTime(CareerBalanceDefinition balance, CareerState state) =>
            ToDateTime(balance, state?.clockHour ?? 0);

        public static int FromDateTime(CareerBalanceDefinition balance, DateTime when)
        {
            DateTime epoch = Epoch(balance);
            double hours = (when - epoch).TotalHours;
            return (int)Math.Max(0, Math.Floor(hours));
        }

        public static string FormatClock(DateTime when) =>
            $"{when.Year}.{when.Month}.{when.Day} {when.Hour}:{when.Minute:00}";

        public static string FormatHeader(CareerBalanceDefinition balance, CareerState state) =>
            FormatClock(ToDateTime(balance, state));

        public static string FormatShort(CareerBalanceDefinition balance, int clockHour) =>
            FormatClock(ToDateTime(balance, clockHour));

        public static string FormatDateRange(CareerBalanceDefinition balance, CompetitionDefinition competition)
        {
            if (competition == null) return string.Empty;
            DateTime start = ToDateTime(balance, competition.startHour);
            DateTime end = ToDateTime(balance, competition.endHour);
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            if (start.Year == end.Year && start.Month == end.Month && start.Day == end.Day)
                return start.ToString("MMM dd, yyyy", culture);
            return $"{start.ToString("MMM dd", culture)} – {end.ToString("MMM dd, yyyy", culture)}";
        }

        public static bool IsCancelled(CompetitionDefinition competition) =>
            competition != null && competition.format == CompetitionFormat.Cancelled;

        public static bool IsLive(CompetitionDefinition competition, int clockHour)
        {
            if (competition == null || IsCancelled(competition)) return false;
            int end = Math.Max(competition.startHour, competition.endHour);
            return clockHour >= competition.startHour && clockHour <= end;
        }

        public static bool IsRegisterableThisMonth(
            CompetitionDefinition competition, CareerBalanceDefinition balance, int clockHour)
        {
            if (competition == null || IsCancelled(competition)) return false;
            if (clockHour >= competition.startHour) return false;
            DateTime now = ToDateTime(balance, clockHour);
            DateTime start = ToDateTime(balance, competition.startHour);
            return start.Year == now.Year && start.Month == now.Month;
        }

        public static bool VisibleOnMap(
            CompetitionDefinition competition, CareerBalanceDefinition balance, int clockHour) =>
            IsLive(competition, clockHour) || IsRegisterableThisMonth(competition, balance, clockHour);

        /// <summary>
        /// Major stays hidden until offline/LAN return is announced (date placeholder or flag).
        /// </summary>
        public static bool IsOfflineReturnAnnounced(
            CareerBalanceDefinition balance, int clockHour, IEnumerable<CareerFlag> flags = null)
        {
            if (CareerFlagRules.HasFlag(
                    flags, OfflineReturnAnnouncedFlag, CareerFlagRules.GlobalScope, clockHour))
                return true;
            return ToDateTime(balance, clockHour) >= OfflineReturnAnnounceDate;
        }

        public static bool IsRevealedOnSchedule(
            CompetitionDefinition competition,
            CareerBalanceDefinition balance,
            int clockHour,
            IEnumerable<CareerFlag> flags = null)
        {
            if (competition == null || IsCancelled(competition))
                return false;
            if (competition.tier == CompetitionTier.Major &&
                !IsOfflineReturnAnnounced(balance, clockHour, flags))
                return false;
            return true;
        }

        public static bool InScheduleWindow(
            CompetitionDefinition competition,
            CareerBalanceDefinition balance,
            int clockHour,
            int horizonHours = ScheduleHorizonHours,
            IEnumerable<CareerFlag> flags = null)
        {
            if (!IsRevealedOnSchedule(competition, balance, clockHour, flags))
                return false;
            if (IsLive(competition, clockHour)) return true;
            return competition.startHour >= clockHour &&
                   competition.startHour <= clockHour + Math.Max(1, horizonHours);
        }

        public static int ResolveRankingHalfLifeHours(CareerBalanceDefinition balance)
        {
            if (balance == null) return 26 * HoursPerWeek;
            if (balance.rankingHalfLifeHours > 0) return balance.rankingHalfLifeHours;
            return Math.Max(1, balance.rankingHalfLifeWeeks) * HoursPerWeek;
        }

        public static int ResolveRankingWindowHours(CareerBalanceDefinition balance)
        {
            if (balance == null) return 52 * HoursPerWeek;
            if (balance.rankingWindowHours > 0) return balance.rankingWindowHours;
            return Math.Max(1, balance.rankingWindowWeeks) * HoursPerWeek;
        }

        /// <summary>Published HLTV world ranking rebuild interval (7 days).</summary>
        public static int ResolveRankingUpdateHours(CareerBalanceDefinition balance = null) =>
            HoursPerWeek;

        public static int ResolveTrainingTickHours(CareerBalanceDefinition balance) =>
            balance != null && balance.hoursPerTrainingTick > 0
                ? balance.hoursPerTrainingTick
                : HoursPerWeek;
    }

    /// <summary>Energy regained purely by elapsed time, for both players and coaches.</summary>
    public static class EnergyRecoveryRules
    {
        /// <summary>One energy point every this many hours.</summary>
        public const int HoursPerPoint = 2;
        public const int MaxEnergy = 100;

        public static int Recover(int current, int hours, double multiplier = 1d)
        {
            if (hours <= 0 || HoursPerPoint <= 0)
                return current;
            double gain = hours / (double)HoursPerPoint * multiplier;
            return Math.Min(MaxEnergy, current + (int)Math.Floor(gain));
        }
    }

    [Serializable]
    public sealed class CompetitionBracketPair
    {
        public string clubAId;
        public string clubBId;
        public bool involvesPlayerClub;
    }

    [Serializable]
    public sealed class CompetitionBracketMatchView
    {
        public string clubAId;
        public string clubBId;
        public int scoreA = -1;
        public int scoreB = -1;
        public string winnerClubId;
        public bool involvesPlayerClub;
        public bool completed;
    }

    [Serializable]
    public sealed class CompetitionBracketRoundView
    {
        public string title;
        public List<CompetitionBracketMatchView> matches = new List<CompetitionBracketMatchView>();
    }

    [Serializable]
    public sealed class CompetitionBracketSnapshot
    {
        public string competitionId;
        public int registeredCount;
        public int bracketCapacity;
        public bool registrationOpen;
        public bool live;
        public string currentOpponentClubId;
        public string nextRoundHint;
        public List<CompetitionBracketPair> firstRound = new List<CompetitionBracketPair>();
        public List<CompetitionBracketRoundView> rounds = new List<CompetitionBracketRoundView>();
        public bool usesMajorStructure;
        public MajorStageKind majorStage;
        public string majorStageLabel;
        public List<MajorSwissStanding> swissStandings = new List<MajorSwissStanding>();
        public List<MajorSwissPairing> swissPairings = new List<MajorSwissPairing>();
    }

    /// <summary>報名期 AI 陸續加入、淘汰賽籤表預覽。</summary>
    public static class CompetitionRegistrationRules
    {
        public static IReadOnlyList<string> RegisteredClubIds(CareerState state, string competitionId) =>
            state.competitionEntries
                .Where(x => string.Equals(x.competitionId, competitionId, StringComparison.OrdinalIgnoreCase) &&
                            x.registered && !x.withdrawn && !x.completed)
                .Select(x => x.clubId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

        /// <summary>放寬後的世界排名門檻：硬限制再加約三成，至少多 4 名。</summary>
        public static int SoftMaximumWorldRank(CompetitionDefinition competition)
        {
            if (competition == null || competition.maximumWorldRank <= 0)
                return int.MaxValue;
            int hard = competition.maximumWorldRank;
            return Math.Max(hard + 4, (int)Math.Ceiling(hard * 1.35d));
        }

        /// <summary>實際滿額籤位數：2 的次方、最多 16，且不超過賽事列出的上限。</summary>
        public static int EffectiveFieldSize(CompetitionDefinition competition)
        {
            if (competition == null)
                return 2;
            if (StockholmMajorRules.IsStockholmMajorStructure(competition))
                return Math.Max(2, Math.Min(24, Math.Max(2, competition.maximumTeams)));
            return BracketSlotCount(competition, Math.Min(16, Math.Max(2, competition.maximumTeams)));
        }

        /// <summary>AI 可開始陸續報名的時點：賽事前約六個月，且不早於生涯開局。</summary>
        public static int AiRegistrationOpenHour(CompetitionDefinition competition) =>
            competition == null
                ? 0
                : Math.Max(0, competition.startHour - CareerCalendar.ScheduleHorizonHours);

        public static bool IsOpenForAiRegistration(
            CompetitionDefinition competition,
            CareerBalanceDefinition balance,
            int clockHour,
            IEnumerable<CareerFlag> flags = null)
        {
            if (competition == null || CareerCalendar.IsCancelled(competition))
                return false;
            if (clockHour >= competition.startHour)
                return false;
            if (!CareerCalendar.IsRevealedOnSchedule(competition, balance, clockHour, flags))
                return false;
            return clockHour >= AiRegistrationOpenHour(competition);
        }

        public static int TargetRegisteredCount(
            CompetitionDefinition competition, CareerBalanceDefinition balance, int clockHour)
        {
            if (competition == null || CareerCalendar.IsCancelled(competition))
                return 0;

            int cap = EffectiveFieldSize(competition);
            if (clockHour >= competition.startHour)
                return cap;

            int openHour = AiRegistrationOpenHour(competition);
            if (clockHour < openHour)
                return 0;

            int hoursLeft = competition.startHour - clockHour;
            // 開賽前一週強制朝滿額推進，避免賽程上突然灌滿對手。
            if (hoursLeft <= CareerCalendar.HoursPerWeek)
                return cap;

            int window = Math.Max(1, competition.startHour - openHour);
            double progress = (clockHour - openHour) / (double)window;
            progress = CareerMath.Clamp(progress, 0d, 1d);
            return Math.Max(0, (int)Math.Ceiling(progress * 0.92d * cap));
        }

        public static bool IsForceFillWindow(
            CompetitionDefinition competition, CareerBalanceDefinition balance, int clockHour)
        {
            if (competition == null || CareerCalendar.IsCancelled(competition))
                return false;
            if (clockHour >= competition.startHour)
                return true;
            return competition.startHour - clockHour <= CareerCalendar.HoursPerWeek;
        }

        /// <summary>
        /// AI 報名意願 0..1。強隊偏好高階賽；能量／健康差或撞期時傾向休賽。
        /// </summary>
        public static double AiSignupDesire(
            CompetitionDefinition competition,
            int worldRank,
            int ambition,
            double averageEnergy,
            double averageHealth,
            double averageForm,
            int overlappingCommitments,
            bool sameRegion)
        {
            if (competition == null)
                return 0d;

            double ambition01 = CareerMath.Clamp(ambition / 100d, 0.15d, 1d);
            int preferredTier = (int)CompetitionTier.Exhibition +
                                (int)Math.Round(ambition01 * ((int)CompetitionTier.Major));
            preferredTier = Math.Max(0, Math.Min((int)CompetitionTier.Major, preferredTier));
            int tierGap = preferredTier - (int)competition.tier;

            double desire = 0.42d + ambition01 * 0.28d;
            if (worldRank > 0 && worldRank <= 8)
                desire += 0.12d;
            else if (worldRank > 0 && worldRank <= 20)
                desire += 0.06d;

            // 強隊略過低階盃賽；弱隊對過高階則較猶豫。
            if (tierGap >= 3)
                desire *= 0.28d;
            else if (tierGap == 2)
                desire *= 0.55d;
            else if (tierGap <= -3)
                desire *= 0.45d;
            else if (tierGap == -2)
                desire *= 0.7d;

            double energy01 = CareerMath.Clamp(averageEnergy / 100d, 0d, 1d);
            double health01 = CareerMath.Clamp(averageHealth / 100d, 0d, 1d);
            double form01 = CareerMath.Clamp(averageForm / 100d, 0d, 1d);
            desire *= 0.35d + energy01 * 0.4d + health01 * 0.15d + form01 * 0.1d;

            if (averageEnergy < 40d)
                desire *= 0.2d;
            else if (averageEnergy < 55d)
                desire *= 0.45d;

            if (averageHealth < 50d)
                desire *= 0.55d;

            if (overlappingCommitments >= 2)
                desire *= 0.22d;
            else if (overlappingCommitments == 1)
                desire *= 0.55d;

            if (!sameRegion)
                desire *= 0.72d;

            if (competition.invitationOnly && worldRank > 0 && worldRank <= SoftMaximumWorldRank(competition))
                desire += 0.08d;

            return CareerMath.Clamp(desire, 0.04d, 0.96d);
        }

        public static bool AiChoosesRest(double desire, SeededRandom random) =>
            random == null || random.Next01() > desire;

        public static int BracketSlotCount(CompetitionDefinition competition, int registeredCount)
        {
            int cap = Math.Min(16, Math.Max(2, competition.maximumTeams));
            int need = Math.Max(registeredCount, 2);
            int size = 2;
            while (size < need && size < cap)
                size *= 2;
            return Math.Min(cap, size);
        }

        public static List<string> BuildBracketOrder(
            IReadOnlyList<string> registeredClubIds,
            IReadOnlyList<RankingEntry> rankings,
            int slotCount)
        {
            var ordered = registeredClubIds
                .OrderBy(id =>
                {
                    RankingEntry rank = rankings.FirstOrDefault(x =>
                        string.Equals(x.clubId, id, StringComparison.OrdinalIgnoreCase));
                    return rank?.rank ?? 999;
                })
                .ThenBy(id => id, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var slots = new List<string>(slotCount);
            for (int i = 0; i < slotCount; i++)
                slots.Add(i < ordered.Count ? ordered[i] : null);
            return slots;
        }

        public static List<CompetitionBracketPair> BuildFirstRoundPairs(
            List<string> bracketOrder, string playerClubId)
        {
            var pairs = new List<CompetitionBracketPair>();
            for (int i = 0; i + 1 < bracketOrder.Count; i += 2)
            {
                string a = bracketOrder[i];
                string b = bracketOrder[i + 1];
                bool player = string.Equals(a, playerClubId, StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(b, playerClubId, StringComparison.OrdinalIgnoreCase);
                pairs.Add(new CompetitionBracketPair
                {
                    clubAId = a,
                    clubBId = b,
                    involvesPlayerClub = player
                });
            }
            return pairs;
        }

        public static string BuildNextRoundHint(
            List<string> bracketOrder, string playerClubId, Func<string, string> clubLabel)
        {
            if (bracketOrder == null || bracketOrder.Count < 4 || string.IsNullOrEmpty(playerClubId))
                return null;

            int playerIndex = bracketOrder.FindIndex(x =>
                string.Equals(x, playerClubId, StringComparison.OrdinalIgnoreCase));
            if (playerIndex < 0)
                return null;

            int pairIndex = playerIndex / 2;
            int siblingPair = pairIndex ^ 1;
            int siblingStart = siblingPair * 2;
            if (siblingStart + 1 >= bracketOrder.Count)
                return null;

            string Label(string id) =>
                string.IsNullOrEmpty(id) ? "暫定" : clubLabel(id);

            string left = Label(bracketOrder[siblingStart]);
            string right = Label(bracketOrder[siblingStart + 1]);
            if (left == "暫定" && right == "暫定")
                return "下一輪：暫定";
            if (left == "暫定" || right == "暫定")
                return $"下一輪可能：{left} / {right}".Replace("暫定 / ", "").Replace(" / 暫定", "");
            return $"下一輪可能：{left} 或 {right}";
        }

        public static CompetitionBracketSnapshot BuildSnapshot(
            CareerState state,
            CareerBalanceDefinition balance,
            CompetitionDefinition competition,
            string playerClubId,
            Func<string, string> clubLabel)
        {
            var snapshot = new CompetitionBracketSnapshot
            {
                competitionId = competition?.id,
                registrationOpen = competition != null &&
                                   CareerCalendar.IsRegisterableThisMonth(competition, balance, state.clockHour),
                live = competition != null && CareerCalendar.IsLive(competition, state.clockHour)
            };
            if (competition == null)
                return snapshot;

            IReadOnlyList<string> registered = RegisteredClubIds(state, competition.id);
            snapshot.registeredCount = registered.Count;
            snapshot.bracketCapacity = EffectiveFieldSize(competition);
            List<string> order = BuildBracketOrder(registered, state.rankings, snapshot.bracketCapacity);

            CompetitionBracketArchive archive = state.competitionBrackets?.FirstOrDefault(x =>
                string.Equals(x.competitionId, competition.id, StringComparison.OrdinalIgnoreCase));
            if (archive?.seedOrder != null && archive.seedOrder.Count >= 2)
            {
                order = new List<string>(archive.seedOrder);
                snapshot.bracketCapacity = Math.Max(snapshot.bracketCapacity, order.Count);
            }

            snapshot.firstRound = BuildFirstRoundPairs(order, playerClubId);
            snapshot.nextRoundHint = BuildNextRoundHint(order, playerClubId, clubLabel);
            snapshot.rounds = BuildRoundViews(order, playerClubId, archive);

            CareerCompetitionRunState run = state.competitionRun;
            if (run != null &&
                string.Equals(run.competitionId, competition.id, StringComparison.OrdinalIgnoreCase))
            {
                OverlayLiveRoundState(snapshot.rounds, run, playerClubId);
                if (!string.IsNullOrEmpty(run.pendingOpponentClubId) && run.awaitingPlayerMatch)
                    snapshot.currentOpponentClubId = run.pendingOpponentClubId;
                else if (run.roundIndex > 0 && run.currentRoundClubs.Count == 2)
                    snapshot.nextRoundHint = "決賽對手：" +
                        clubLabel(run.currentRoundClubs.FirstOrDefault(x =>
                            !string.Equals(x, playerClubId, StringComparison.OrdinalIgnoreCase)) ?? "暫定");

                if (string.Equals(run.structureId, StockholmMajorRules.StructureId, StringComparison.OrdinalIgnoreCase))
                {
                    snapshot.usesMajorStructure = true;
                    snapshot.majorStage = run.majorStage;
                    snapshot.majorStageLabel = StockholmMajorRules.StageDisplayName(run.majorStage);
                    snapshot.swissStandings = run.swissStandings != null
                        ? new List<MajorSwissStanding>(run.swissStandings)
                        : new List<MajorSwissStanding>();
                    snapshot.swissPairings = run.swissPairings != null
                        ? new List<MajorSwissPairing>(run.swissPairings)
                        : new List<MajorSwissPairing>();
                    if (run.majorStage == MajorStageKind.ChallengersSwiss ||
                        run.majorStage == MajorStageKind.LegendsSwiss)
                    {
                        snapshot.nextRoundHint = snapshot.majorStageLabel +
                            " · 瑞士輪次 " + Math.Max(1, run.swissRoundIndex + 1);
                    }
                }
            }
            else if (StockholmMajorRules.IsStockholmMajorStructure(competition))
            {
                snapshot.usesMajorStructure = true;
                snapshot.majorStage = MajorStageKind.None;
                snapshot.majorStageLabel = "尚未開賽";
            }

            return snapshot;
        }

        public static List<CompetitionBracketRoundView> BuildRoundViews(
            List<string> bracketOrder,
            string playerClubId,
            CompetitionBracketArchive archive)
        {
            var rounds = new List<CompetitionBracketRoundView>();
            if (bracketOrder == null || bracketOrder.Count < 2)
                return rounds;

            int teamCount = bracketOrder.Count;
            int matchCount = teamCount / 2;
            string[] titles = RoundTitlesForTeamCount(teamCount);
            int roundIndex = 0;

            var first = new CompetitionBracketRoundView { title = titles[0] };
            for (int i = 0; i + 1 < bracketOrder.Count; i += 2)
            {
                string a = bracketOrder[i];
                string b = bracketOrder[i + 1];
                first.matches.Add(new CompetitionBracketMatchView
                {
                    clubAId = a,
                    clubBId = b,
                    involvesPlayerClub =
                        string.Equals(a, playerClubId, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(b, playerClubId, StringComparison.OrdinalIgnoreCase)
                });
            }
            rounds.Add(first);
            roundIndex++;

            while (matchCount > 1)
            {
                matchCount /= 2;
                string title = roundIndex < titles.Length
                    ? titles[roundIndex]
                    : StageLabelForTeamsInRound(Math.Max(2, matchCount * 2));
                var round = new CompetitionBracketRoundView { title = title };
                for (int i = 0; i < matchCount; i++)
                    round.matches.Add(new CompetitionBracketMatchView());
                rounds.Add(round);
                roundIndex++;
            }

            if (archive?.matches != null)
            {
                foreach (CompetitionBracketMatchRecord record in archive.matches)
                    ApplyMatchRecord(rounds, record, playerClubId);
                PropagateWinners(rounds, playerClubId);
            }

            return rounds;
        }

        /// <summary>Label for a knockout round by how many clubs remain in that round.</summary>
        public static string StageLabelForTeamsInRound(int teamsInRound)
        {
            if (teamsInRound <= 0) return string.Empty;
            if (teamsInRound >= 32) return "三十二強";
            if (teamsInRound >= 16) return "十六強";
            if (teamsInRound >= 8) return "八強";
            if (teamsInRound >= 4) return "四強";
            return "決賽";
        }

        public static string[] RoundTitlesForTeamCount(int teamCount)
        {
            int size = 2;
            int need = Math.Max(2, teamCount);
            while (size < need)
                size *= 2;

            var titles = new List<string>();
            for (int teams = size; teams >= 2; teams /= 2)
                titles.Add(StageLabelForTeamsInRound(teams));
            return titles.ToArray();
        }

        private static void ApplyMatchRecord(
            List<CompetitionBracketRoundView> rounds,
            CompetitionBracketMatchRecord record,
            string playerClubId)
        {
            if (record == null || rounds == null)
                return;
            if (record.roundIndex < 0 || record.roundIndex >= rounds.Count)
                return;
            CompetitionBracketRoundView round = rounds[record.roundIndex];
            if (record.pairIndex < 0 || record.pairIndex >= round.matches.Count)
                return;

            CompetitionBracketMatchView match = round.matches[record.pairIndex];
            match.clubAId = record.clubAId;
            match.clubBId = record.clubBId;
            match.scoreA = record.scoreA;
            match.scoreB = record.scoreB;
            match.winnerClubId = record.winnerClubId;
            match.completed = !string.IsNullOrEmpty(record.winnerClubId);
            match.involvesPlayerClub =
                string.Equals(record.clubAId, playerClubId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(record.clubBId, playerClubId, StringComparison.OrdinalIgnoreCase);
        }

        private static void PropagateWinners(List<CompetitionBracketRoundView> rounds, string playerClubId)
        {
            if (rounds == null || rounds.Count < 2)
                return;

            for (int r = 0; r < rounds.Count - 1; r++)
            {
                CompetitionBracketRoundView current = rounds[r];
                CompetitionBracketRoundView next = rounds[r + 1];
                for (int i = 0; i < current.matches.Count; i++)
                {
                    CompetitionBracketMatchView match = current.matches[i];
                    if (!match.completed || string.IsNullOrEmpty(match.winnerClubId))
                        continue;
                    int nextPair = i / 2;
                    if (nextPair < 0 || nextPair >= next.matches.Count)
                        continue;
                    CompetitionBracketMatchView dest = next.matches[nextPair];
                    if ((i % 2) == 0)
                        dest.clubAId = match.winnerClubId;
                    else
                        dest.clubBId = match.winnerClubId;
                    dest.involvesPlayerClub =
                        string.Equals(dest.clubAId, playerClubId, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(dest.clubBId, playerClubId, StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        private static void OverlayLiveRoundState(
            List<CompetitionBracketRoundView> rounds,
            CareerCompetitionRunState run,
            string playerClubId)
        {
            if (rounds == null || run == null || run.roundIndex < 0 || run.roundIndex >= rounds.Count)
                return;
            if (run.currentRoundClubs == null || run.currentRoundClubs.Count < 2)
                return;

            CompetitionBracketRoundView round = rounds[run.roundIndex];
            int pairs = Math.Min(round.matches.Count, (run.currentRoundClubs.Count + 1) / 2);
            for (int p = 0; p < pairs; p++)
            {
                int aIndex = p * 2;
                if (aIndex >= run.currentRoundClubs.Count)
                    break;
                CompetitionBracketMatchView match = round.matches[p];
                if (match.completed)
                    continue;

                match.clubAId = run.currentRoundClubs[aIndex];
                match.clubBId = aIndex + 1 < run.currentRoundClubs.Count
                    ? run.currentRoundClubs[aIndex + 1]
                    : null;
                match.involvesPlayerClub =
                    string.Equals(match.clubAId, playerClubId, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(match.clubBId, playerClubId, StringComparison.OrdinalIgnoreCase);

                // Already decided winners waiting for round transition.
                if (p < run.pairIndex && p < run.roundWinners.Count)
                {
                    match.winnerClubId = run.roundWinners[p];
                    match.completed = true;
                }
            }

            PropagateWinners(rounds, playerClubId);
        }
    }

    public static class SponsorRules
    {
        public const int MaxActiveSponsorsPerClub = 6;
        public const int DefaultContractMonths = 12;

        public static int CalendarMonthKey(CareerBalanceDefinition balance, int clockHour)
        {
            DateTime when = CareerCalendar.ToDateTime(balance, clockHour);
            return when.Year * 12 + when.Month;
        }
    }

    public sealed class SponsorSignEvaluation
    {
        public SponsorDefinition definition;
        public bool canSign;
        public string blockReason;
        public bool alreadySigned;
        public bool monthlyLimitReached;
        public bool rosterFull;
        public bool meetsFame;
        public bool meetsExposure;
        public bool meetsRank;
        public int currentFame;
        public int requiredFame;
        public int currentExposure;
        public int requiredExposure;
        public int currentRankPercentile;
        public int requiredRankPercentile;
        public int activeSponsorCount;
    }

    /// <summary>
    /// Club fame / exposure standing helpers for UI modifiers.
    /// fame: how widely known the club is; exposure: how the scene perceives standing.
    /// </summary>
    public static class ClubReputationRules
    {
        public const int HighThreshold = 75;
        public const int LowThreshold = 35;

        public static bool IsHigh(int value) => value >= HighThreshold;
        public static bool IsLow(int value) => value <= LowThreshold;

        public static void AppendModifierHints(
            ClubState club, List<(bool buff, string id, string title, string description)> sink)
        {
            if (club == null || sink == null)
                return;

            if (IsHigh(club.fame))
            {
                sink.Add((true, "fame_high", "高名氣",
                    $"名氣 {club.fame}：俱樂部廣為人知。商品與媒體關注較佳，較易吸引目光與合作邀約。"));
            }
            else if (IsLow(club.fame))
            {
                sink.Add((false, "fame_low", "低名氣",
                    $"名氣 {club.fame}：知名度不足。商品銷售與曝光成長較慢，贊助商更難主動找上門。"));
            }

            if (IsHigh(club.exposure))
            {
                sink.Add((true, "prestige_high", "高威望",
                    $"威望 {club.exposure}：外界觀感良好。邀請賽、董事會支持與引援吸引力提升。"));
            }
            else if (IsLow(club.exposure))
            {
                sink.Add((false, "prestige_low", "低威望",
                    $"威望 {club.exposure}：外界觀感偏弱。邀約與談判較吃力，董事會與贊助滿意度更容易下滑。"));
            }
        }
    }

    /// <summary>Club-scoped scripted crises lasting until cleared by a later event.</summary>
    public static class ClubCrisisRules
    {
        public const string DynastyInternalFractureFlag = "dynasty_internal_fracture";
        public const int DynastyExposureCap = 50;
        /// <summary>Pressure gained when the daily roll succeeds.</summary>
        public const int DynastyDailyPressure = 1;
        /// <summary>Per-member chance each calendar day to gain <see cref="DynastyDailyPressure"/> (independent rolls).</summary>
        public const float DynastyDailyPressureChance = 0.30f;

        public static bool HasDynastyInternalFracture(CareerState state, string clubId)
        {
            if (state == null || string.IsNullOrWhiteSpace(clubId))
                return false;
            return CareerFlagRules.HasFlag(
                state.flags, DynastyInternalFractureFlag, clubId, state.clockHour);
        }

        public static void AppendModifierHints(
            CareerState state,
            ClubState club,
            List<(bool buff, string id, string title, string description)> sink)
        {
            if (state == null || club == null || sink == null)
                return;
            if (!HasDynastyInternalFracture(state, club.clubId))
                return;
            sink.Add((false, DynastyInternalFractureFlag, "A隊王朝的內部矛盾",
                "危機結束前，俱樂部威望無法高於50。每天每位俱樂部成員各自獨立判定，有百分之三十機率壓力加一。"));
        }

        public static void ClampExposureIfCapped(CareerState state, ClubState club)
        {
            if (club == null || !HasDynastyInternalFracture(state, club.clubId))
                return;
            if (club.exposure > DynastyExposureCap)
                club.exposure = DynastyExposureCap;
        }

        public static void ApplyDailyEffects(CareerState state)
        {
            if (state?.clubs == null)
                return;

            var random = new SeededRandom(state.seed + state.clockHour * 7919 + 41);
            foreach (ClubState club in state.clubs)
            {
                if (club == null || !HasDynastyInternalFracture(state, club.clubId))
                    continue;
                ClampExposureIfCapped(state, club);

                if (state.players != null)
                {
                    foreach (PlayerCareerState player in state.players)
                    {
                        if (player == null ||
                            !string.Equals(player.clubId, club.clubId, StringComparison.OrdinalIgnoreCase))
                            continue;
                        // Independent per-member roll — not one team-wide outcome.
                        if (random.Next01() >= DynastyDailyPressureChance)
                            continue;
                        player.pressure = Math.Max(0, Math.Min(100, player.pressure + DynastyDailyPressure));
                    }
                }

                if (state.coaches != null)
                {
                    foreach (CoachStaffState coach in state.coaches)
                    {
                        if (coach == null ||
                            !string.Equals(coach.clubId, club.clubId, StringComparison.OrdinalIgnoreCase))
                            continue;
                        if (random.Next01() >= DynastyDailyPressureChance)
                            continue;
                        coach.pressure = Math.Max(0, Math.Min(100, coach.pressure + DynastyDailyPressure));
                    }
                }
            }
        }
    }

    public sealed class BudgetBreakdownLine
    {
        public string label;
        public int amount;
    }

    public sealed class MonthlyBudgetPreview
    {
        public int cash;
        public int sponsorIncome;
        public int merchandiseIncome;
        /// <summary>Prize money already booked this calendar month (ledger category prize).</summary>
        public int prizeIncome;
        public int salaryExpense;
        public int coachSalaryExpense;
        /// <summary>Facility opex disabled for now; kept for save/UI compatibility and always 0.</summary>
        public int facilityMaintenance;
        public bool hasSponsor;
        public int activeSponsorCount;

        public readonly List<BudgetBreakdownLine> sponsorDetails = new List<BudgetBreakdownLine>();
        public readonly List<BudgetBreakdownLine> merchandiseDetails = new List<BudgetBreakdownLine>();
        public readonly List<BudgetBreakdownLine> prizeDetails = new List<BudgetBreakdownLine>();
        public readonly List<BudgetBreakdownLine> salaryDetails = new List<BudgetBreakdownLine>();
        public readonly List<BudgetBreakdownLine> coachSalaryDetails = new List<BudgetBreakdownLine>();

        public int IncomeTotal => sponsorIncome + merchandiseIncome + prizeIncome;
        public int ExpenseTotal => salaryExpense + coachSalaryExpense;
        public int Net => IncomeTotal - ExpenseTotal;
    }

    public static class FinanceRules
    {
        public static int SponsorMonthly(SponsorContract contract, ClubState club, CareerDifficulty difficulty)
        {
            // 名氣抬高底價能見度；威望抬高外界願意給的條件（EU4-style prestige lean）。
            double fameFactor = .55d + club.fame / 200d;
            double prestigeFactor = .92d + club.exposure / 400d;
            if (ClubReputationRules.IsHigh(club.exposure))
                prestigeFactor += .06d;
            else if (ClubReputationRules.IsLow(club.exposure))
                prestigeFactor -= .08d;

            double result = contract.monthlyFee *
                fameFactor * prestigeFactor *
                (.85d + contract.satisfaction / 400d) *
                CareerMath.DifficultyIncome(difficulty);
            return Math.Max(0, CareerMath.RoundMoney(result));
        }

        public static int Merchandise(ClubState club, int starCount, CareerDifficulty difficulty)
        {
            // 名氣主導商品熱度；低名氣明顯吃虧。
            double fameFactor = .5d + club.fame / 100d;
            if (ClubReputationRules.IsHigh(club.fame))
                fameFactor += .12d;
            else if (ClubReputationRules.IsLow(club.fame))
                fameFactor -= .15d;

            double result = 4000d * (1d + club.fans / 50000d) *
                fameFactor * (1d + starCount * .12d) *
                CareerMath.DifficultyIncome(difficulty);
            return Math.Max(0, CareerMath.RoundMoney(result));
        }

        public static int FacilityMaintenance(FacilityDefinition definition, FacilityState state)
        {
            if (state.disabled)
                return 0;
            return CareerMath.RoundMoney(definition.baseMaintenance * Math.Pow(state.level, 1.35d));
        }

        public static MonthlyBudgetPreview BuildMonthlyPreview(
            ClubState club,
            IEnumerable<SponsorContract> sponsors,
            IEnumerable<ContractState> contracts,
            IEnumerable<CoachStaffState> coaches,
            IEnumerable<LedgerEntry> ledger,
            CareerBalanceDefinition balance,
            int clockHour,
            IReadOnlyDictionary<FacilityType, FacilityDefinition> facilities,
            int starCount,
            CareerDifficulty difficulty)
        {
            SponsorContract[] active = sponsors
                .Where(x => string.Equals(x.clubId, club.clubId, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            var preview = new MonthlyBudgetPreview
            {
                cash = club.cash,
                hasSponsor = active.Length > 0,
                activeSponsorCount = active.Length
            };

            foreach (SponsorContract contract in active)
                preview.sponsorIncome += SponsorMonthly(contract, club, difficulty);

            preview.merchandiseIncome = Merchandise(club, starCount, difficulty);
            preview.salaryExpense = contracts.Sum(x => x.monthlySalary);
            preview.coachSalaryExpense = coaches == null
                ? 0
                : coaches
                    .Where(c => c != null &&
                                string.Equals(c.clubId, club.clubId, StringComparison.OrdinalIgnoreCase))
                    .Sum(c => Math.Max(0, c.monthlySalary));

            // Facility operating cost temporarily disabled.
            preview.facilityMaintenance = 0;
            _ = facilities;

            int monthKey = SponsorRules.CalendarMonthKey(balance, clockHour);
            if (ledger != null)
            {
                foreach (LedgerEntry entry in ledger)
                {
                    if (entry == null ||
                        !string.Equals(entry.clubId, club.clubId, StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(entry.category, "prize", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (SponsorRules.CalendarMonthKey(balance, entry.clockHour) != monthKey)
                        continue;
                    preview.prizeIncome += Math.Max(0, entry.amount);
                    preview.prizeDetails.Add(new BudgetBreakdownLine
                    {
                        label = string.IsNullOrWhiteSpace(entry.note) ? "賽事獎金" : entry.note,
                        amount = entry.amount
                    });
                }
            }

            return preview;
        }

        public static FinancialHealth Evaluate(ClubState club, int monthlyExpense)
        {
            double average = Math.Max(1d,
                (club.trailingExpense1 + club.trailingExpense2 + club.trailingExpense3 + monthlyExpense) / 4d);
            double runway = club.cash / average;
            if (club.consecutiveCrisisMonths >= 12) return FinancialHealth.Bankrupt;
            if (runway < 3d) return FinancialHealth.Crisis;
            if (club.consecutiveLossMonths >= 3) return FinancialHealth.Deficit;
            if (runway <= 6d) return FinancialHealth.Balanced;
            if (runway <= 12d) return FinancialHealth.Healthy;
            return club.lastMonthNet >= 0 ? FinancialHealth.Wealthy : FinancialHealth.Healthy;
        }

        public static void PushExpenseHistory(ClubState club, int expense)
        {
            club.trailingExpense3 = club.trailingExpense2;
            club.trailingExpense2 = club.trailingExpense1;
            club.trailingExpense1 = Math.Max(0, expense);
        }
    }

    public sealed class CareerTrainingPlan
    {
        public List<string> attributes = new List<string>();
        public List<string> maps = new List<string>();
        public string role;
    }

    public static class TrainingPlanRules
    {
        public const int MaxAttributes = 2;
        public const int MaxMaps = 3;

        public static readonly string[] AttributeIds =
        {
            "aim", "reaction", "awareness", "utility",
            "teamwork", "communication", "opening", "clutch"
        };

        public static readonly string[] RoleIds =
            { "Entry", "Awper", "Rifler", "Lurker", "Support", "Igl" };

        public static readonly string[] MapIds =
            { "Vertigo", "Overpass", "Ancient", "Mirage", "Nuke", "Inferno", "Dust II", "Train" };

        public static bool IsActive(CareerTrainingPlan plan) =>
            plan != null &&
            ((plan.attributes != null && plan.attributes.Count > 0) ||
             (plan.maps != null && plan.maps.Count > 0) ||
             !string.IsNullOrWhiteSpace(plan.role));

        public static double AttributeEfficiency(int count) =>
            count <= 0 ? 0d : count == 1 ? 2d : 1d;

        public static double MapEfficiency(int count) =>
            count <= 0 ? 0d :
            count == 1 ? 2d :
            count == 2 ? 1.5d : 1d;

        public static CareerTrainingPlan Parse(string focus)
        {
            var plan = new CareerTrainingPlan();
            if (string.IsNullOrWhiteSpace(focus))
                return plan;

            // Legacy single-mode formats.
            if (string.Equals(focus, "power", StringComparison.OrdinalIgnoreCase))
            {
                plan.attributes.Add("aim");
                return plan;
            }
            if (focus.StartsWith("role", StringComparison.OrdinalIgnoreCase))
            {
                int colon = focus.IndexOf(':');
                plan.role = colon >= 0 && colon + 1 < focus.Length ? focus.Substring(colon + 1) : "Rifler";
                return plan;
            }
            if (focus.StartsWith("map", StringComparison.OrdinalIgnoreCase))
            {
                int colon = focus.IndexOf(':');
                string map = colon >= 0 && colon + 1 < focus.Length ? focus.Substring(colon + 1) : "Dust II";
                plan.maps.Add(map);
                return plan;
            }

            foreach (string part in focus.Split('|'))
            {
                if (string.IsNullOrWhiteSpace(part))
                    continue;
                int colon = part.IndexOf(':');
                string key = colon >= 0 ? part.Substring(0, colon).Trim() : part.Trim();
                string value = colon >= 0 && colon + 1 < part.Length ? part.Substring(colon + 1).Trim() : string.Empty;
                if (key.Equals("a", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("attr", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("attrs", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (string id in SplitCsv(value))
                    {
                        if (plan.attributes.Count >= MaxAttributes)
                            break;
                        if (IsKnown(AttributeIds, id) &&
                            !plan.attributes.Contains(id, StringComparer.OrdinalIgnoreCase))
                            plan.attributes.Add(Canonical(AttributeIds, id));
                    }
                }
                else if (key.Equals("m", StringComparison.OrdinalIgnoreCase) ||
                         key.Equals("map", StringComparison.OrdinalIgnoreCase) ||
                         key.Equals("maps", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (string id in SplitCsv(value))
                    {
                        if (plan.maps.Count >= MaxMaps)
                            break;
                        if (IsKnown(MapIds, id) &&
                            !plan.maps.Contains(id, StringComparer.OrdinalIgnoreCase))
                            plan.maps.Add(Canonical(MapIds, id));
                    }
                }
                else if (key.Equals("r", StringComparison.OrdinalIgnoreCase) ||
                         key.Equals("role", StringComparison.OrdinalIgnoreCase))
                {
                    if (IsKnown(RoleIds, value))
                        plan.role = Canonical(RoleIds, value);
                }
            }

            return plan;
        }

        public static string Serialize(CareerTrainingPlan plan)
        {
            if (!IsActive(plan))
                return string.Empty;

            var parts = new List<string>();
            if (plan.attributes != null && plan.attributes.Count > 0)
                parts.Add("a:" + string.Join(",", plan.attributes.Take(MaxAttributes)));
            if (plan.maps != null && plan.maps.Count > 0)
                parts.Add("m:" + string.Join(",", plan.maps.Take(MaxMaps)));
            if (!string.IsNullOrWhiteSpace(plan.role))
                parts.Add("r:" + plan.role);
            return string.Join("|", parts);
        }

        public static string FormatShort(CareerTrainingPlan plan, Func<string, string> roleLabel)
        {
            if (!IsActive(plan))
                return "休息（未訓練）";

            var bits = new List<string>();
            if (plan.attributes != null && plan.attributes.Count > 0)
            {
                string eff = plan.attributes.Count == 1 ? "×2" : "";
                bits.Add("能力 " + string.Join("+", plan.attributes.Select(AttributeLabel)) + eff);
            }
            if (plan.maps != null && plan.maps.Count > 0)
            {
                string eff = plan.maps.Count == 1 ? "×2" : plan.maps.Count == 2 ? "×1.5" : "";
                bits.Add("地圖 " + string.Join("+", plan.maps) + eff);
            }
            if (!string.IsNullOrWhiteSpace(plan.role))
                bits.Add("位置 " + (roleLabel != null ? roleLabel(plan.role) : plan.role));
            return string.Join(" · ", bits);
        }

        public static string AttributeLabel(string id)
        {
            switch ((id ?? string.Empty).ToLowerInvariant())
            {
                case "aim": return "瞄準";
                case "reaction": return "反應";
                case "awareness": return "意識";
                case "utility": return "道具";
                case "teamwork": return "配合";
                case "communication": return "溝通";
                case "opening": return "首殺";
                case "clutch": return "殘局";
                default: return id ?? "?";
            }
        }

        private static IEnumerable<string> SplitCsv(string value) =>
            string.IsNullOrWhiteSpace(value)
                ? Array.Empty<string>()
                : value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => x.Length > 0);

        private static bool IsKnown(string[] known, string id) =>
            !string.IsNullOrWhiteSpace(id) &&
            known.Any(x => string.Equals(x, id, StringComparison.OrdinalIgnoreCase));

        private static string Canonical(string[] known, string id) =>
            known.First(x => string.Equals(x, id, StringComparison.OrdinalIgnoreCase));
    }

    public static class DevelopmentRules
    {
        public static double WeeklyTrainingPoints(
            PlayerCareerState player, CareerPlayerDefinition definition,
            int coachTraining, int facilityLevel, bool congested)
        {
            double age = player.age <= 18 ? 1.35d : player.age <= 20 ? 1.2d :
                player.age <= 27 ? .75d : player.age <= 30 ? .45d : .25d;
            // Potential is the soft ceiling; room-to-grow still scales gains.
            int ceiling = AbilityCeiling(player);
            double potentialSpace = CareerMath.Clamp((ceiling - player.power) / 35d, .15d, 1.25d);
            double coach = .7d + CareerMath.Clamp(coachTraining, 0, 100) / 200d;
            double facility = .85d + CareerMath.Clamp(facilityLevel, 1, 5) * .05d;
            double professionalism = definition.professionalism >= 80 ? 1.15d :
                definition.professionalism < 45 ? .82d : 1d;
            double fatigue = CareerMath.Clamp(player.energy / 75d, .55d, 1.1d);
            return 1.2d * age * potentialSpace * coach * facility *
                professionalism * fatigue * (congested ? .65d : 1d);
        }

        /// <summary>Ability / power hard ceiling from potential (attribute curve TBD).</summary>
        public static int AbilityCeiling(PlayerCareerState player) =>
            player == null ? 100 : Math.Max(1, Math.Min(100, player.potential));

        public static void EnsureProgressLists(
            PlayerCareerState player,
            string primaryRole,
            CombatAttributes combatSeed = null,
            RoleFitness[] roleFitnessSeed = null,
            MapFitness[] mapFitnessSeed = null,
            string dust2GradeSeed = null)
        {
            if (player == null)
                return;
            player.attributes = player.attributes ?? new List<CareerNamedStat>();
            player.roleProficiencies = player.roleProficiencies ?? new List<CareerNamedStat>();
            player.mapProficiencies = player.mapProficiencies ?? new List<CareerNamedStat>();

            float powerBaseline = (float)CareerMath.Clamp(player.power, 40, 90);
            foreach (string id in TrainingPlanRules.AttributeIds)
            {
                float seed = powerBaseline;
                if (combatSeed != null)
                {
                    int combat = combatSeed.Get(id);
                    if (combat > 0)
                        seed = combat;
                }
                EnsureStat(player.attributes, id, seed);
            }

            // Legacy / early saves filled every combat attr with power only; replace when we have real seeds.
            if (combatSeed != null && LooksLikeUniformPowerAttributes(player))
                ApplyCombatAttributeSeeds(player, combatSeed);

            foreach (string id in TrainingPlanRules.RoleIds)
                EnsureStat(player.roleProficiencies, id, 0f);

            foreach (string id in TrainingPlanRules.MapIds)
                EnsureStat(player.mapProficiencies, id, 0f);

            // Old builds seeded primary≈70 / other≈35–50 / maps≥55; those were mistaken for grades.
            if (LooksLikeLegacyInflatedProficiencies(player))
                ResetRoleMapProficienciesToZero(player);

            SeedFitnessGrades(player, roleFitnessSeed, mapFitnessSeed, dust2GradeSeed);
            SyncLegacyAggregates(player);
        }

        /// <summary>
        /// Copy content grades onto career fitness stats when grade is still missing.
        /// Proficiency stays at its career value (default 0).
        /// </summary>
        public static void SeedFitnessGrades(
            PlayerCareerState player,
            RoleFitness[] roleFitnessSeed,
            MapFitness[] mapFitnessSeed,
            string dust2GradeSeed = null)
        {
            if (player == null)
                return;

            if (roleFitnessSeed != null)
            {
                foreach (RoleFitness fit in roleFitnessSeed)
                {
                    if (fit == null || string.IsNullOrWhiteSpace(fit.role) || string.IsNullOrWhiteSpace(fit.grade))
                        continue;
                    EnsureStat(player.roleProficiencies, fit.role, 0f, fit.grade);
                }
            }

            if (mapFitnessSeed != null)
            {
                foreach (MapFitness fit in mapFitnessSeed)
                {
                    if (fit == null || string.IsNullOrWhiteSpace(fit.map) || string.IsNullOrWhiteSpace(fit.grade))
                        continue;
                    EnsureStat(player.mapProficiencies, fit.map, 0f, fit.grade);
                }
            }

            if (!string.IsNullOrWhiteSpace(dust2GradeSeed))
                EnsureStat(player.mapProficiencies, "Dust II", 0f, dust2GradeSeed);
        }

        /// <summary>
        /// Legacy EnsureProgressLists used high floors for role/map proficiency.
        /// Detect that pattern so we can reset to the real default of 0.
        /// </summary>
        public static bool LooksLikeLegacyInflatedProficiencies(PlayerCareerState player)
        {
            if (player?.roleProficiencies == null || player.roleProficiencies.Count == 0)
                return false;

            bool anyRole = false;
            foreach (string id in TrainingPlanRules.RoleIds)
            {
                CareerNamedStat stat = player.roleProficiencies.FirstOrDefault(x =>
                    string.Equals(x.id, id, StringComparison.OrdinalIgnoreCase));
                if (stat == null)
                    continue;
                anyRole = true;
                // Old floors: primary ≥70, others ≥35.
                if (stat.value < 34.9f)
                    return false;
            }

            if (!anyRole)
                return false;

            if (player.mapProficiencies != null)
            {
                foreach (string id in TrainingPlanRules.MapIds)
                {
                    CareerNamedStat stat = player.mapProficiencies.FirstOrDefault(x =>
                        string.Equals(x.id, id, StringComparison.OrdinalIgnoreCase));
                    if (stat == null)
                        continue;
                    // Old floor: max(mapProficiency, 55).
                    if (stat.value < 54.9f)
                        return false;
                }
            }

            return true;
        }

        public static void ResetRoleMapProficienciesToZero(PlayerCareerState player)
        {
            if (player == null)
                return;
            player.roleProficiencies = player.roleProficiencies ?? new List<CareerNamedStat>();
            player.mapProficiencies = player.mapProficiencies ?? new List<CareerNamedStat>();
            foreach (string id in TrainingPlanRules.RoleIds)
            {
                CareerNamedStat existing = player.roleProficiencies.FirstOrDefault(x =>
                    string.Equals(x.id, id, StringComparison.OrdinalIgnoreCase));
                if (existing == null)
                    player.roleProficiencies.Add(new CareerNamedStat { id = id, value = 0f });
                else
                    existing.value = 0f;
            }

            foreach (string id in TrainingPlanRules.MapIds)
            {
                CareerNamedStat existing = player.mapProficiencies.FirstOrDefault(x =>
                    string.Equals(x.id, id, StringComparison.OrdinalIgnoreCase));
                if (existing == null)
                    player.mapProficiencies.Add(new CareerNamedStat { id = id, value = 0f });
                else
                    existing.value = 0f;
            }

            player.roleProficiency = 0f;
            player.mapProficiency = 0f;
        }

        /// <summary>
        /// True when every combat attribute matches the clamped power baseline
        /// (the old EnsureProgressLists fill), so differentiated combat JSON can safely replace it.
        /// </summary>
        public static bool LooksLikeUniformPowerAttributes(PlayerCareerState player)
        {
            if (player?.attributes == null || player.attributes.Count == 0)
                return true;

            float baseline = Round1((float)CareerMath.Clamp(player.power, 40, 90));
            int matched = 0;
            foreach (string id in TrainingPlanRules.AttributeIds)
            {
                CareerNamedStat stat = player.attributes.FirstOrDefault(x =>
                    string.Equals(x.id, id, StringComparison.OrdinalIgnoreCase));
                if (stat == null)
                    return true;
                if (Math.Abs(Round1(stat.value) - baseline) > 0.05f)
                    return false;
                matched++;
            }
            return matched >= TrainingPlanRules.AttributeIds.Length;
        }

        public static void ApplyCombatAttributeSeeds(PlayerCareerState player, CombatAttributes combatSeed)
        {
            if (player == null || combatSeed == null)
                return;
            player.attributes = player.attributes ?? new List<CareerNamedStat>();
            foreach (string id in TrainingPlanRules.AttributeIds)
            {
                int combat = combatSeed.Get(id);
                if (combat <= 0)
                    continue;
                float value = Math.Max(0f, Math.Min(100f, combat));
                CareerNamedStat existing = player.attributes.FirstOrDefault(x =>
                    string.Equals(x.id, id, StringComparison.OrdinalIgnoreCase));
                if (existing == null)
                    player.attributes.Add(new CareerNamedStat { id = id, value = value });
                else
                    existing.value = value;
            }
        }

        /// <summary>Pure combat strength: sum of the eight attributes. Theoretical max 800.</summary>
        public static int SumCombatAttributes(IList<CareerNamedStat> attributes)
        {
            if (attributes == null || attributes.Count == 0)
                return 0;

            int sum = 0;
            foreach (string id in TrainingPlanRules.AttributeIds)
            {
                CareerNamedStat stat = attributes.FirstOrDefault(x =>
                    string.Equals(x.id, id, StringComparison.OrdinalIgnoreCase));
                if (stat == null)
                    continue;
                sum += (int)Math.Round(CareerMath.Clamp(stat.value, 0d, 100d));
            }
            return sum;
        }

        /// <summary>Pure combat strength from match content attributes. Theoretical max 800.</summary>
        public static int SumCombatAttributes(CombatAttributes attributes)
        {
            if (attributes == null)
                return 0;
            return ClampAttr(attributes.aim) + ClampAttr(attributes.reaction) +
                   ClampAttr(attributes.awareness) + ClampAttr(attributes.utility) +
                   ClampAttr(attributes.teamwork) + ClampAttr(attributes.communication) +
                   ClampAttr(attributes.opening) + ClampAttr(attributes.clutch);
        }

        private static int ClampAttr(int value) =>
            (int)CareerMath.Clamp(value, 0, 100);

        /// <summary>
        /// Applies training for the given number of career hours.
        /// Weekly point budget is scaled by hours / tickLength; proficiency uses one-decimal steps.
        /// </summary>
        public static void ApplyHourlyTraining(
            PlayerCareerState player,
            CareerPlayerDefinition definition,
            CareerTrainingPlan plan,
            double weeklyPoints,
            int hours,
            CareerBalanceDefinition balance)
        {
            if (hours <= 0)
                return;

            EnsureProgressLists(player, definition?.role);
            int ceiling = AbilityCeiling(player);
            int tickHours = Math.Max(1, CareerCalendar.ResolveTrainingTickHours(balance));

            if (!TrainingPlanRules.IsActive(plan))
            {
                player.hoursWithoutTraining += hours;
                return;
            }

            player.hoursWithoutTraining = 0;
            double hourFraction = hours / (double)tickHours;
            double points = weeklyPoints * hourFraction;
            double attrEff = TrainingPlanRules.AttributeEfficiency(plan.attributes.Count);
            double mapEff = TrainingPlanRules.MapEfficiency(plan.maps.Count);

            if (plan.attributes.Count > 0 && attrEff > 0d)
            {
                // Power progress: ~weekly gain spread across hours.
                double gain = Math.Min(.45d, weeklyPoints / 10d * attrEff) * hourFraction;
                player.trainingProgress += (float)gain;
                while (player.trainingProgress >= 1f && player.power < ceiling)
                {
                    player.power++;
                    player.trainingProgress -= 1f;
                }
                player.power = Math.Min(player.power, ceiling);

                // ~1.0–2.0 attribute points per week, paid out hourly as fractions.
                float perAttr = (float)(points * attrEff * 0.85d);
                foreach (string id in plan.attributes)
                    AddStat(player.attributes, id, perAttr, ceiling);
            }

            if (plan.maps.Count > 0 && mapEff > 0d)
            {
                float perMap = (float)(points * mapEff * 0.9d);
                foreach (string id in plan.maps)
                    AddFitnessProgress(player.mapProficiencies, id, perMap);
            }

            if (!string.IsNullOrWhiteSpace(plan.role))
            {
                float roleGain = (float)(points * 0.95d);
                AddFitnessProgress(player.roleProficiencies, plan.role, roleGain);
            }

            SyncLegacyAggregates(player);
        }

        public static bool ApplyInactivityRust(
            PlayerCareerState player,
            CareerPlayerDefinition definition,
            CareerBalanceDefinition balance)
        {
            if (player == null || balance == null || balance.inactivityRustWeeks <= 0)
                return false;

            int tick = CareerCalendar.ResolveTrainingTickHours(balance);
            int thresholdHours = balance.inactivityRustWeeks * tick;
            if (player.hoursWithoutMatch < thresholdHours ||
                player.hoursWithoutTraining < thresholdHours)
                return false;

            EnsureProgressLists(player, definition?.role);
            float profLoss = Math.Max(0.1f, balance.inactivityRustProficiencyLoss);
            float attrLoss = Math.Max(0.1f, balance.inactivityRustAttributeLoss);

            foreach (CareerNamedStat stat in player.roleProficiencies)
                stat.value = Round1(Math.Max(0f, stat.value - profLoss));
            foreach (CareerNamedStat stat in player.mapProficiencies)
                stat.value = Round1(Math.Max(0f, stat.value - profLoss));
            foreach (CareerNamedStat stat in player.attributes)
                stat.value = Round1(Math.Max(25f, stat.value - attrLoss));

            // Slight overall regression; still bounded by potential later.
            int powerLoss = Math.Max(1, (int)Math.Round(attrLoss));
            player.power = Math.Max(35, player.power - powerLoss);
            player.form = Math.Max(25, player.form - 2);
            SyncLegacyAggregates(player);
            return true;
        }

        public static double QuarterlyDecline(
            PlayerCareerState player, string attribute, int health)
        {
            int start = attribute == "reaction" ? 27 : attribute == "aim" ? 29 :
                attribute == "awareness" ? 32 : 30;
            double basis = attribute == "reaction" ? .45d : attribute == "aim" ? .32d :
                attribute == "awareness" ? .12d : .18d;
            int excess = Math.Max(0, player.age - start + 1);
            double trajectory = player.trajectory == "evergreen" ? .65d :
                player.trajectory == "meteor" ? 1.45d : 1d;
            double healthFactor = health < 70 ? 1d + (70 - health) / 100d : 1d;
            double inactivity = player.hoursWithoutMatch >= 8 * CareerCalendar.HoursPerWeek ? 1.25d : 1d;
            return basis * excess * trajectory * healthFactor * inactivity;
        }

        public static float Round1(float value) =>
            (float)(Math.Round(value * 10d, MidpointRounding.AwayFromZero) / 10d);

        public static string FormatProficiency(float value) =>
            Round1(value).ToString("0.0");

        private static void EnsureStat(
            List<CareerNamedStat> list, string id, float defaultValue, string gradeSeed = null)
        {
            CareerNamedStat existing = list.FirstOrDefault(x =>
                string.Equals(x.id, id, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                list.Add(new CareerNamedStat
                {
                    id = id,
                    value = Math.Max(0f, Math.Min(100f, defaultValue)),
                    grade = string.IsNullOrWhiteSpace(gradeSeed) ? null : GradeScale.Normalize(gradeSeed)
                });
                return;
            }

            if (string.IsNullOrWhiteSpace(existing.grade) && !string.IsNullOrWhiteSpace(gradeSeed))
                existing.grade = GradeScale.Normalize(gradeSeed);
        }

        /// <summary>
        /// Adds role/map training proficiency. At 100, promotes grade and carries overflow.
        /// S-grade stays at MAX and ignores further proficiency gains.
        /// </summary>
        public static void AddFitnessProgress(List<CareerNamedStat> list, string id, float delta)
        {
            if (list == null || string.IsNullOrWhiteSpace(id) || delta <= 0f)
                return;

            CareerNamedStat existing = list.FirstOrDefault(x =>
                string.Equals(x.id, id, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                existing = new CareerNamedStat
                {
                    id = id,
                    value = 0f,
                    grade = "E"
                };
                list.Add(existing);
            }

            if (string.IsNullOrWhiteSpace(existing.grade))
                existing.grade = "E";
            existing.grade = GradeScale.Normalize(existing.grade);

            if (GradeScale.IsMax(existing.grade))
            {
                existing.value = 100f;
                return;
            }

            existing.value = Round1(existing.value + delta);
            while (existing.value >= 100f - 0.0001f)
            {
                if (!GradeScale.TryGetNext(existing.grade, out string next))
                {
                    existing.value = 100f;
                    break;
                }

                existing.grade = next;
                existing.value = Round1(existing.value - 100f);
                if (GradeScale.IsMax(existing.grade))
                {
                    existing.value = 100f;
                    break;
                }
            }

            existing.value = Math.Max(0f, Math.Min(100f, existing.value));
        }

        private static void AddStat(List<CareerNamedStat> list, string id, float delta, float ceiling)
        {
            if (delta <= 0f)
                return;
            CareerNamedStat existing = list.FirstOrDefault(x =>
                string.Equals(x.id, id, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                list.Add(new CareerNamedStat
                {
                    id = id,
                    value = Math.Max(0f, Math.Min(ceiling, delta))
                });
                return;
            }
            existing.value = Math.Max(0f, Math.Min(ceiling, existing.value + delta));
        }

        private static void SyncLegacyAggregates(PlayerCareerState player)
        {
            if (player.roleProficiencies != null && player.roleProficiencies.Count > 0)
                player.roleProficiency = Round1((float)player.roleProficiencies.Average(x => x.value));
            if (player.mapProficiencies != null && player.mapProficiencies.Count > 0)
                player.mapProficiency = Round1((float)player.mapProficiencies.Average(x => x.value));
        }

        /// <summary>
        /// Apply a signed proficiency delta on a map fitness grade.
        /// S is treated as a cap grade: losing points demotes to A+ with remainder
        /// (e.g. S −25 → A+/75).
        /// </summary>
        public static void ApplyMapProficiencyDelta(PlayerCareerState player, string mapId, float delta)
        {
            if (player == null || string.IsNullOrWhiteSpace(mapId) || Math.Abs(delta) < 0.0001f)
                return;

            player.mapProficiencies = player.mapProficiencies ?? new List<CareerNamedStat>();
            CareerNamedStat existing = player.mapProficiencies.FirstOrDefault(x =>
                string.Equals(x.id, mapId, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                existing = new CareerNamedStat
                {
                    id = mapId,
                    value = 0f,
                    grade = "E"
                };
                player.mapProficiencies.Add(existing);
            }

            if (string.IsNullOrWhiteSpace(existing.grade))
                existing.grade = "E";
            existing.grade = GradeScale.Normalize(existing.grade);

            if (delta > 0f)
            {
                AddFitnessProgress(player.mapProficiencies, mapId, delta);
                SyncLegacyAggregates(player);
                return;
            }

            float remain = -delta;
            if (GradeScale.IsMax(existing.grade))
            {
                // Cap grade has no partial progress to spend first — demote immediately.
                if (!GradeScale.TryGetPrevious(existing.grade, out string demoted))
                {
                    existing.value = 0f;
                    SyncLegacyAggregates(player);
                    return;
                }

                existing.grade = demoted;
                existing.value = Round1(Math.Max(0f, 100f - remain));
                SyncLegacyAggregates(player);
                return;
            }

            existing.value = Round1(existing.value - remain);
            while (existing.value < -0.0001f)
            {
                if (!GradeScale.TryGetPrevious(existing.grade, out string previous))
                {
                    existing.value = 0f;
                    break;
                }

                existing.grade = previous;
                existing.value = Round1(existing.value + 100f);
            }

            existing.value = Math.Max(0f, Math.Min(100f, existing.value));
            SyncLegacyAggregates(player);
        }
    }

    public static class MarketRules
    {
        public static int CalculateMarketValue(
            PlayerCareerState player, CareerPlayerDefinition definition,
            ContractState contract, int scarcityPercent = 100)
        {
            double baseValue = 25000d * Math.Exp((player.power - 55d) / 14d);
            double age = player.age <= 20 ? 1.35d : player.age <= 24 ? 1.25d :
                player.age <= 27 ? 1d : player.age <= 30 ? .78d : .55d;
            double potential = 1d + Math.Max(0, player.potential - player.power) / 80d;
            if (player.age < 24) potential *= 1.15d;
            double fame = .75d + player.fame / 200d;
            double form = CareerMath.Clamp(.85d + (player.ratingTrend + 10d) / 40d, .75d, 1.25d);
            int months = contract == null ? 0 : contract.remainingMonths;
            double contractFactor = months <= 3 ? .55d : months <= 12 ? .8d : months <= 24 ? 1d : 1.12d;
            double result = baseValue * age * potential * fame * form * contractFactor * scarcityPercent / 100d;
            return (int)CareerMath.Clamp(CareerMath.RoundMoney(result), 10000, 15000000);
        }

        public static int AskingSalary(int marketValue, int age, int fame)
        {
            double security = age >= 29 ? 1.15d : age <= 21 ? .85d : 1d;
            double demand = .8d + fame / 250d;
            return (int)CareerMath.Clamp(CareerMath.RoundMoney(marketValue / 36d * security * demand), 1000, 250000);
        }

        public static double RetentionScore(
            int management, int loyalty, int rankAppeal, int wageParity, int unrest) =>
            CareerMath.Clamp(management * .30d + loyalty * .30d + rankAppeal * .20d +
                wageParity * .20d - unrest, 5d, 95d);

        public const int TargetRosterSize = 5;

        public static int CountRoster(IEnumerable<PlayerCareerState> players, string clubId) =>
            players.Count(x => string.Equals(x.clubId, clubId, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// 陣容已滿時僅在補角色缺口或明顯戰力升級時才會有意願簽人。
        /// </summary>
        public static bool ClubWantsPlayer(
            IEnumerable<PlayerCareerState> players,
            IReadOnlyDictionary<string, CareerPlayerDefinition> definitions,
            string buyerClubId,
            PlayerCareerState target,
            CareerPlayerDefinition targetDefinition)
        {
            if (target == null || targetDefinition == null || string.IsNullOrEmpty(buyerClubId))
                return false;

            PlayerCareerState[] roster = players
                .Where(x => string.Equals(x.clubId, buyerClubId, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (roster.Length < TargetRosterSize)
                return true;

            if (RosterMissingRole(roster, definitions, "Awper") &&
                string.Equals(targetDefinition.role, "Awper", StringComparison.OrdinalIgnoreCase))
                return true;

            if (roster.Length > TargetRosterSize)
                return target.power >= roster.Max(x => x.power) + 3;

            double weakest = roster.Min(x => x.power);
            return target.power >= weakest + 6;
        }

        /// <summary>
        /// 0 = 不會出價；缺人時接近 1，滿編時僅在鎖定強援時略高。
        /// </summary>
        public static double BuyInclination(
            IEnumerable<PlayerCareerState> players,
            IReadOnlyDictionary<string, CareerPlayerDefinition> definitions,
            string buyerClubId,
            PlayerCareerState target,
            CareerPlayerDefinition targetDefinition)
        {
            if (!ClubWantsPlayer(players, definitions, buyerClubId, target, targetDefinition))
                return 0d;

            int count = CountRoster(players, buyerClubId);
            if (count < TargetRosterSize)
                return CareerMath.Clamp(0.55 + (TargetRosterSize - count) * 0.15, 0.55, 1d);

            PlayerCareerState[] roster = players
                .Where(x => string.Equals(x.clubId, buyerClubId, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            double weakest = roster.Length == 0 ? 50d : roster.Min(x => x.power);
            double upgrade = Math.Max(0, target.power - weakest) / 50d;

            if (count == TargetRosterSize)
                return CareerMath.Clamp(0.10 + upgrade * 0.45, 0.08, 0.40);

            return CareerMath.Clamp(0.06 + upgrade * 0.35, 0.05, 0.22);
        }

        private static bool RosterMissingRole(
            PlayerCareerState[] roster,
            IReadOnlyDictionary<string, CareerPlayerDefinition> definitions,
            string role)
        {
            foreach (PlayerCareerState player in roster)
            {
                if (definitions.TryGetValue(player.playerId, out CareerPlayerDefinition def) &&
                    string.Equals(def.role, role, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Staged transfer negotiation: fee agreement, personal terms window, rival outbids, release-clause buyouts.
    /// </summary>
    public static class TransferNegotiationRules
    {
        /// <summary>Temporarily disable transfer quote / negotiation while balancing UI ships.</summary>
        public const bool QuoteSystemEnabled = false;

        public const double OutbidFeeMultiplier = 1.08d;
        public const int PersonalTermsWindowDays = 5;
        public const int FeeOfferWindowWeeks = 2;
        public const int DefaultContractMonths = 24;
        public const float ImportanceDefault = 0.35f;

        public static int PersonalTermsWindowHours =>
            PersonalTermsWindowDays * CareerCalendar.HoursPerDay;

        public static int FeeOfferWindowHours =>
            FeeOfferWindowWeeks * CareerCalendar.HoursPerWeek;

        public static bool IsActiveNegotiation(TransferOfferStatus status) =>
            status == TransferOfferStatus.Pending ||
            status == TransferOfferStatus.Countered ||
            status == TransferOfferStatus.ClubAgreed ||
            status == TransferOfferStatus.PersonalTerms;

        public static bool IsVulnerableToOutbid(TransferOfferStatus status) =>
            status == TransferOfferStatus.ClubAgreed ||
            status == TransferOfferStatus.PersonalTerms;

        public static bool IsReleaseClauseBuyout(ContractState contract, int fee) =>
            contract != null && contract.releaseClause > 0 && fee >= contract.releaseClause;

        public static int SellerReservePrice(
            int marketValue, float importance = ImportanceDefault, float windowPressure = 0f)
        {
            double factor = 1d + importance * 0.35d + windowPressure;
            return CareerMath.RoundMoney(Math.Max(10000, marketValue * factor));
        }

        public static bool SellerAcceptsFee(int fee, int reservePrice) =>
            fee >= reservePrice;

        /// <summary>UI hint: lowest fee the seller might entertain, not a guaranteed accept.</summary>
        public static int EstimatedMinimumFee(int marketValue) =>
            CareerMath.RoundMoney(Math.Max(10000, marketValue * 0.85d));

        public static bool CanOutbid(int rivalFee, int currentAgreedFee, bool releaseClausePath) =>
            releaseClausePath || rivalFee >= CareerMath.RoundMoney(currentAgreedFee * OutbidFeeMultiplier);

        public static int DefaultAskedSalary(PlayerCareerState player, int marketValue) =>
            MarketRules.AskingSalary(marketValue, player.age, player.fame);

        /// <summary>
        /// 0..100 acceptance score for personal terms.
        /// Club fee satisfaction is handled in the fee phase; this scores the player's own willingness
        /// including buyer prestige and roster strength.
        /// </summary>
        public static double PersonalTermsAcceptanceScore(
            int offeredSalary,
            int askedSalary,
            int signingBonus,
            int contractMonths,
            int buyerWorldRank,
            int sellerWorldRank,
            int loyalty,
            bool transferRequested,
            int buyerPrestige,
            double buyerRosterAvgPower,
            int playerPower)
        {
            double salaryRatio = askedSalary <= 0
                ? 1d
                : CareerMath.Clamp(offeredSalary / (double)askedSalary, 0d, 1.5d);
            double salaryScore = salaryRatio >= 1d ? 100d : salaryRatio / 1d * 100d;

            double bonusAsk = Math.Max(askedSalary * 2d, 5000d);
            double bonusScore = CareerMath.Clamp(signingBonus / bonusAsk * 100d, 0d, 100d);

            double lengthScore = contractMonths >= 36 ? 100d :
                contractMonths >= 24 ? 80d :
                contractMonths >= 12 ? 55d : 30d;

            int rankDelta = sellerWorldRank - buyerWorldRank;
            double rankAppeal = CareerMath.Clamp(50d + rankDelta * 4d, 10d, 100d);
            double loyaltyScore = CareerMath.Clamp(100d - loyalty, 5d, 95d);
            if (transferRequested)
                loyaltyScore = CareerMath.Clamp(loyaltyScore + 25d, 5d, 100d);

            double prestigeScore = CareerMath.Clamp(buyerPrestige, 5d, 100d);
            double powerGap = buyerRosterAvgPower - playerPower;
            double rosterScore = CareerMath.Clamp(55d + powerGap * 2.5d, 10d, 100d);

            return CareerMath.Clamp(
                salaryScore * 0.28d +
                bonusScore * 0.12d +
                lengthScore * 0.08d +
                rankAppeal * 0.12d +
                loyaltyScore * 0.15d +
                prestigeScore * 0.12d +
                rosterScore * 0.13d,
                5d, 95d);
        }

        public static bool PlayerAcceptsPersonalTerms(
            double acceptanceScore, SeededRandom random) =>
            random.Next01() * 100d <= acceptanceScore;

        public static void ApplyClubAgreedPhase(TransferOffer offer, int clockHour)
        {
            offer.status = TransferOfferStatus.ClubAgreed;
            offer.phaseExpiresHour = clockHour + PersonalTermsWindowHours;
            offer.expiresHour = Math.Max(offer.expiresHour, offer.phaseExpiresHour);
        }

        public static void ApplyPersonalTermsPhase(
            TransferOffer offer, int clockHour, int askedSalary,
            int offeredSalary = 0, int signingBonus = 0, int contractMonths = 0)
        {
            offer.status = TransferOfferStatus.PersonalTerms;
            offer.phaseExpiresHour = clockHour + PersonalTermsWindowHours;
            offer.expiresHour = Math.Max(offer.expiresHour, offer.phaseExpiresHour);
            offer.askedSalary = askedSalary;
            offer.offeredSalary = offeredSalary > 0 ? offeredSalary : askedSalary;
            offer.signingBonus = Math.Max(0, signingBonus);
            offer.contractMonths = contractMonths > 0 ? contractMonths : DefaultContractMonths;
            offer.negotiationRound = Math.Max(1, offer.negotiationRound);
        }

        public static void MarkOutbid(TransferOffer offer, string rivalOfferId)
        {
            offer.status = TransferOfferStatus.Outbid;
            offer.outbidByOfferId = rivalOfferId;
        }
    }

    public static class RankingRules
    {
        /// <summary>HLTV-style world ranking cap (display and earned total).</summary>
        public const float MaximumHltvPoints = 1000f;

        /// <summary>Recent-form window for tie-break and UI (13 weeks).</summary>
        public static int RecentFormWindowHours => CareerCalendar.HoursPerWeek * 13;

        public static float ClampHltvPoints(float points) =>
            Math.Max(0f, Math.Min(MaximumHltvPoints, points));

        public static float TierBase(CompetitionTier tier)
        {
            switch (tier)
            {
                case CompetitionTier.Major: return 350f;
                case CompetitionTier.SuperElite: return 320f;
                case CompetitionTier.S: return 220f;
                case CompetitionTier.Elite: return 175f;
                case CompetitionTier.A: return 130f;
                case CompetitionTier.B: return 75f;
                case CompetitionTier.C: return 40f;
                case CompetitionTier.D: return 18f;
                case CompetitionTier.Exhibition: return 10f;
                default: return 0f;
            }
        }

        public static float PlacementWeight(int placement)
        {
            if (placement <= 1) return 1f;
            if (placement == 2) return .70f;
            if (placement == 3) return .50f;
            if (placement == 4) return .45f;
            if (placement <= 8) return .28f;
            if (placement <= 16) return .12f;
            return .05f;
        }

        /// <summary>Small ranking bump for a single official match win inside an active tournament.</summary>
        public static float MatchWinPoints(CompetitionTier tier) => TierBase(tier) * 0.012f;

        public static float PowerModifier(double clubPower)
        {
            float mod = 0.88f + (float)clubPower / 320f;
            return Math.Max(0.85f, Math.Min(1.12f, mod));
        }

        public static float CompetitionPlacementPoints(CompetitionTier tier, int placement, double clubPower)
        {
            if (placement <= 0) return 0f;
            float raw = TierBase(tier) * PlacementWeight(placement) * PowerModifier(clubPower);
            return ClampHltvPoints((float)Math.Round(raw, 1));
        }

        public static float DecayedPoints(RankingBatch batch, int currentHour, int halfLifeHours)
        {
            int age = Math.Max(0, currentHour - batch.earnedHour);
            return batch.points * (float)Math.Pow(.5d, age / (double)Math.Max(1, halfLifeHours));
        }

        public static List<RankingEntry> Build(
            IEnumerable<RankingBatch> batches, IEnumerable<string> clubIds,
            int clockHour, int halfLifeHours, int windowHours)
        {
            int recentWindow = Math.Max(1, RecentFormWindowHours);
            List<RankingEntry> result = clubIds.Select(id =>
            {
                RankingBatch[] valid = batches.Where(x =>
                    string.Equals(x.clubId, id, StringComparison.OrdinalIgnoreCase) &&
                    clockHour - x.earnedHour <= windowHours).ToArray();
                return new RankingEntry
                {
                    clubId = id,
                    points = ClampHltvPoints(valid.Sum(x => DecayedPoints(x, clockHour, halfLifeHours))),
                    recentPoints = ClampHltvPoints(valid.Where(x => clockHour - x.earnedHour <= recentWindow)
                        .Sum(x => DecayedPoints(x, clockHour, halfLifeHours)))
                };
            }).OrderByDescending(x => x.points)
              .ThenByDescending(x => x.recentPoints)
              .ThenBy(x => x.clubId, StringComparer.OrdinalIgnoreCase).ToList();
            for (int i = 0; i < result.Count; i++) result[i].rank = i + 1;
            return result;
        }
    }

    /// <summary>
    /// Tournament prize pool split by placement. Higher finish earns more;
    /// early exits still receive a smaller share. Shares are normalized to the field size.
    /// </summary>
    public static class CompetitionPrizeRules
    {
        public static double PlacementWeight(int placement)
        {
            if (placement <= 0) return 0d;
            if (placement == 1) return 40d;
            if (placement == 2) return 18d;
            if (placement <= 4) return 9d;
            if (placement <= 8) return 3.5d;
            if (placement <= 16) return 1.5d;
            return 0.75d;
        }

        public static int PrizeFor(int pool, int placement, int fieldSize)
        {
            if (pool <= 0 || placement <= 0)
                return 0;
            int field = Math.Max(2, fieldSize);
            if (placement > field)
                return 0;

            double totalWeight = 0d;
            for (int p = 1; p <= field; p++)
                totalWeight += PlacementWeight(p);
            if (totalWeight <= 0d)
                return 0;

            return CareerMath.RoundMoney(pool * PlacementWeight(placement) / totalWeight);
        }
    }

    public sealed class CompetitionWithdrawEvaluation
    {
        public bool canWithdraw;
        public bool isLate;
        public bool appliesBan;
        public bool excusedFromBan;
        public int fee;
        public string blockReason;
        public string summary;
    }

    /// <summary>
    /// Withdrawal fees, late bans, and organizer matching for competition signups.
    /// </summary>
    public static class CompetitionWithdrawRules
    {
        public static string ResolveOrganizerId(CompetitionDefinition competition)
        {
            if (competition == null)
                return "independent";
            if (!string.IsNullOrWhiteSpace(competition.organizer))
                return competition.organizer.Trim().ToLowerInvariant();

            string id = competition.id ?? string.Empty;
            if (id.StartsWith("blast_", StringComparison.OrdinalIgnoreCase))
                return "blast";
            if (id.StartsWith("iem_", StringComparison.OrdinalIgnoreCase) ||
                id.StartsWith("esl_", StringComparison.OrdinalIgnoreCase))
                return "esl";
            if (id.StartsWith("dreamhack_", StringComparison.OrdinalIgnoreCase))
                return "dreamhack";
            if (id.StartsWith("pgl_", StringComparison.OrdinalIgnoreCase))
                return "pgl";
            if (id.StartsWith("flashpoint_", StringComparison.OrdinalIgnoreCase))
                return "flashpoint";
            if (id.StartsWith("cs_summit", StringComparison.OrdinalIgnoreCase))
                return "summit";
            if (id.StartsWith("elisa_", StringComparison.OrdinalIgnoreCase))
                return "elisa";
            if (id.StartsWith("funspark_", StringComparison.OrdinalIgnoreCase))
                return "funspark";
            if (id.StartsWith("epic_", StringComparison.OrdinalIgnoreCase))
                return "epic";
            if (id.StartsWith("pinnacle_", StringComparison.OrdinalIgnoreCase))
                return "pinnacle";
            if (id.IndexOf("sweet_snow", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("sweet_spring", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("summer_sweet", StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("autumn_sweet", StringComparison.OrdinalIgnoreCase) >= 0)
                return "gg_bet_circuit";
            return "independent";
        }

        public static bool IsLateWithdraw(CompetitionDefinition competition, int clockHour) =>
            competition != null &&
            clockHour < competition.startHour &&
            competition.startHour - clockHour <= CareerCalendar.HoursPerWeek;

        public static bool IsExcusedReason(CompetitionWithdrawReason reason) =>
            reason == CompetitionWithdrawReason.VisaDenied ||
            reason == CompetitionWithdrawReason.PlayerIllness;

        public static bool BanAppliesToCompetition(CompetitionDefinition competition) =>
            competition != null &&
            competition.tier != CompetitionTier.Major &&
            !CareerCalendar.IsCancelled(competition);

        public static int WithdrawFee(
            CompetitionDefinition competition, bool isLate, bool excusedFromBan)
        {
            if (competition == null || !isLate)
                return 0;

            double rate = competition.format == CompetitionFormat.LAN
                ? 0.028d
                : competition.format == CompetitionFormat.Hybrid
                    ? 0.018d
                    : 0.012d;
            if (excusedFromBan)
                rate *= 0.4d;

            int minFee = excusedFromBan ? 1500 : 4000;
            return Math.Max(minFee, CareerMath.RoundMoney(competition.prizePool * rate));
        }

        public static CompetitionWithdrawEvaluation Evaluate(
            CompetitionDefinition competition,
            CompetitionEntry entry,
            int clockHour,
            CompetitionWithdrawReason reason)
        {
            var evaluation = new CompetitionWithdrawEvaluation();
            if (competition == null || CareerCalendar.IsCancelled(competition))
            {
                evaluation.blockReason = "找不到賽事";
                return evaluation;
            }

            if (entry == null || !entry.registered || entry.withdrawn || entry.completed)
            {
                evaluation.blockReason = "尚未報名或已取消";
                return evaluation;
            }

            if (clockHour >= competition.startHour || CareerCalendar.IsLive(competition, clockHour))
            {
                evaluation.blockReason = "賽事已開始，無法取消報名";
                return evaluation;
            }

            evaluation.canWithdraw = true;
            evaluation.isLate = IsLateWithdraw(competition, clockHour);
            evaluation.excusedFromBan = IsExcusedReason(reason);
            evaluation.appliesBan = evaluation.isLate &&
                                    !evaluation.excusedFromBan &&
                                    BanAppliesToCompetition(competition);
            evaluation.fee = WithdrawFee(competition, evaluation.isLate, evaluation.excusedFromBan);

            if (!evaluation.isLate)
                evaluation.summary = "可免費取消報名。";
            else if (evaluation.appliesBan)
                evaluation.summary =
                    $"開賽前一週內取消：需支付替補相關費用 ${evaluation.fee:N0}，並禁賽同級別下一場同主辦方賽事。Major 不受此禁賽。";
            else
                evaluation.summary =
                    $"因簽證或傷病等事由取消：不禁賽，但仍需支付替補相關費用 ${evaluation.fee:N0}。";

            return evaluation;
        }

        public static bool HasActiveSignupBan(
            IEnumerable<CompetitionSignupBan> bans,
            string clubId,
            CompetitionDefinition competition)
        {
            if (bans == null || string.IsNullOrEmpty(clubId) || !BanAppliesToCompetition(competition))
                return false;

            string organizer = ResolveOrganizerId(competition);
            foreach (CompetitionSignupBan ban in bans)
            {
                if (ban == null || ban.consumed)
                    continue;
                if (!string.Equals(ban.clubId, clubId, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (ban.tier != competition.tier)
                    continue;
                if (!string.Equals(ban.organizerId, organizer, StringComparison.OrdinalIgnoreCase))
                    continue;
                return true;
            }

            return false;
        }

        public static void ConsumeMatchingBans(
            IList<CompetitionSignupBan> bans, CompetitionDefinition competition)
        {
            if (bans == null || !BanAppliesToCompetition(competition))
                return;

            string organizer = ResolveOrganizerId(competition);
            foreach (CompetitionSignupBan ban in bans)
            {
                if (ban == null || ban.consumed)
                    continue;
                if (ban.tier != competition.tier)
                    continue;
                if (!string.Equals(ban.organizerId, organizer, StringComparison.OrdinalIgnoreCase))
                    continue;
                ban.consumed = true;
            }
        }
    }

    public sealed class CompetitionTravelEstimate
    {
        public int flightHours;
        public int earlyArrivalDays;
        public FlightClass flightClass;
        public CompetitionTravelSubsidy subsidyTier;
        public int totalCost;
        public int subsidyAmount;
        public int clubOutOfPocket;
        public int departHour;
        public int arriveHour;
        public int onSiteHour;
        public int returnDepartHour;
        public int returnArriveHour;
    }

    public static class CompetitionTravelRules
    {
        public static bool RequiresTravel(CompetitionDefinition competition) =>
            competition != null &&
            (competition.format == CompetitionFormat.LAN ||
             competition.format == CompetitionFormat.Hybrid);

        public static CompetitionTravelSubsidy ResolveSubsidyTier(CompetitionDefinition competition)
        {
            if (competition == null)
                return CompetitionTravelSubsidy.None;
            if (competition.travelSubsidy != CompetitionTravelSubsidy.None)
                return competition.travelSubsidy;

            if (competition.tier == CompetitionTier.Major)
                return CompetitionTravelSubsidy.FlightAndHotelBusiness;
            if (competition.tier >= CompetitionTier.S)
                return CompetitionTravelSubsidy.FlightAndHotelEconomy;
            if (competition.tier >= CompetitionTier.A)
                return CompetitionTravelSubsidy.FlightEconomy;
            return CompetitionTravelSubsidy.None;
        }

        public static int RegionDistanceBand(string fromRegion, string toRegion)
        {
            if (string.IsNullOrWhiteSpace(fromRegion) || string.IsNullOrWhiteSpace(toRegion))
                return 1;
            if (fromRegion.Equals(toRegion, StringComparison.OrdinalIgnoreCase))
                return 0;

            bool fromEu = fromRegion.Equals("Europe", StringComparison.OrdinalIgnoreCase);
            bool toEu = toRegion.Equals("Europe", StringComparison.OrdinalIgnoreCase);
            bool fromAm = fromRegion.Equals("Americas", StringComparison.OrdinalIgnoreCase);
            bool toAm = toRegion.Equals("Americas", StringComparison.OrdinalIgnoreCase);
            bool fromAs = fromRegion.Equals("Asia", StringComparison.OrdinalIgnoreCase);
            bool toAs = toRegion.Equals("Asia", StringComparison.OrdinalIgnoreCase);

            if ((fromEu && toAm) || (fromAm && toEu))
                return 2;
            if ((fromEu && toAs) || (fromAs && toEu))
                return 3;
            if ((fromAm && toAs) || (fromAs && toAm))
                return 3;
            return 1;
        }

        public static int FlightHours(string fromRegion, string toRegion)
        {
            switch (RegionDistanceBand(fromRegion, toRegion))
            {
                case 0: return 5;
                case 1: return 9;
                case 2: return 11;
                default: return 15;
            }
        }

        public static int EarlyArrivalDays(CompetitionDefinition competition, string fromRegion, string toRegion)
        {
            if (competition == null)
                return 0;
            if (competition.earlyArrivalDays > 0)
                return competition.earlyArrivalDays;

            switch (RegionDistanceBand(fromRegion, toRegion))
            {
                case 0: return 1;
                case 1: return 2;
                case 2: return 4;
                default: return 5;
            }
        }

        public static FlightClass SubsidyFlightClass(CompetitionTravelSubsidy tier)
        {
            switch (tier)
            {
                case CompetitionTravelSubsidy.FlightBusiness:
                case CompetitionTravelSubsidy.FlightAndHotelBusiness:
                    return FlightClass.Business;
                default:
                    return FlightClass.Economy;
            }
        }

        public static bool SubsidyCoversHotel(CompetitionTravelSubsidy tier) =>
            tier == CompetitionTravelSubsidy.FlightAndHotelEconomy ||
            tier == CompetitionTravelSubsidy.FlightAndHotelBusiness;

        public static FlightClass ResolveFlightClass(
            CompetitionDefinition competition,
            CompetitionTravelSubsidy subsidyTier,
            int clubCash,
            bool preferComfort)
        {
            FlightClass covered = SubsidyFlightClass(subsidyTier);
            if (covered == FlightClass.Business)
                return FlightClass.Business;

            if (!preferComfort)
                return FlightClass.Economy;

            int upgradeCost = EstimateFlightOnlyCost(competition, FlightClass.PremiumEconomy) -
                              EstimateFlightOnlyCost(competition, FlightClass.Economy);
            if (clubCash >= upgradeCost * 3)
                return FlightClass.PremiumEconomy;

            upgradeCost = EstimateFlightOnlyCost(competition, FlightClass.Business) -
                          EstimateFlightOnlyCost(competition, FlightClass.Economy);
            if (clubCash >= upgradeCost * 2)
                return FlightClass.Business;

            return FlightClass.Economy;
        }

        static int EstimateFlightOnlyCost(CompetitionDefinition competition, FlightClass flightClass)
        {
            int hours = Math.Max(3, FlightHours("Europe", competition?.region ?? "Europe"));
            return FlightTicketCost(hours, flightClass, 5);
        }

        public static int FlightTicketCost(int flightHours, FlightClass flightClass, int rosterSize)
        {
            double perPerson = flightHours switch
            {
                <= 6 => 420d,
                <= 10 => 780d,
                <= 14 => 1180d,
                _ => 1680d
            };

            double classMul = flightClass switch
            {
                FlightClass.PremiumEconomy => 1.55d,
                FlightClass.Business => 3.2d,
                _ => 1d
            };

            return CareerMath.RoundMoney(perPerson * classMul * Math.Max(1, rosterSize));
        }

        public static int HotelCost(int nights, int rosterSize)
        {
            if (nights <= 0)
                return 0;
            return CareerMath.RoundMoney(95d * nights * Math.Max(1, rosterSize));
        }

        public static void SplitCost(
            int totalCost,
            CompetitionTravelSubsidy subsidyTier,
            int flightHours,
            FlightClass flightClass,
            int earlyDays,
            int eventDays,
            int rosterSize,
            out int subsidyAmount,
            out int clubOutOfPocket)
        {
            int flightCost = FlightTicketCost(flightHours, flightClass, rosterSize);
            int hotelNights = SubsidyCoversHotel(subsidyTier)
                ? earlyDays + Math.Max(1, eventDays)
                : 0;
            int hotelCost = HotelCost(hotelNights, rosterSize);
            int coveredFlight = subsidyTier == CompetitionTravelSubsidy.None
                ? 0
                : FlightTicketCost(
                    flightHours,
                    SubsidyFlightClass(subsidyTier),
                    rosterSize);
            int coveredHotel = SubsidyCoversHotel(subsidyTier)
                ? HotelCost(earlyDays + Math.Max(1, eventDays), rosterSize)
                : 0;

            subsidyAmount = Math.Min(totalCost, coveredFlight + coveredHotel);
            clubOutOfPocket = Math.Max(0, totalCost - subsidyAmount);
        }

        public static CompetitionTravelEstimate BuildEstimate(
            CompetitionDefinition competition,
            string clubRegion,
            int rosterSize,
            int clubCash,
            bool preferComfort)
        {
            var estimate = new CompetitionTravelEstimate();
            if (!RequiresTravel(competition))
                return estimate;

            string venueRegion = competition.region ?? clubRegion ?? "Europe";
            estimate.subsidyTier = ResolveSubsidyTier(competition);
            estimate.flightHours = FlightHours(clubRegion ?? venueRegion, venueRegion);
            estimate.earlyArrivalDays = EarlyArrivalDays(competition, clubRegion, venueRegion);
            estimate.flightClass = ResolveFlightClass(
                competition, estimate.subsidyTier, clubCash, preferComfort);

            int eventDays = Math.Max(1,
                (competition.endHour - competition.startHour + CareerCalendar.HoursPerDay - 1) /
                CareerCalendar.HoursPerDay);
            int flightCost = FlightTicketCost(estimate.flightHours, estimate.flightClass, rosterSize);
            int hotelNights = estimate.earlyArrivalDays + eventDays;
            int hotelCost = HotelCost(hotelNights, rosterSize);
            int returnFlight = FlightTicketCost(estimate.flightHours, estimate.flightClass, rosterSize);
            estimate.totalCost = flightCost + hotelCost + returnFlight;

            SplitCost(
                estimate.totalCost,
                estimate.subsidyTier,
                estimate.flightHours,
                estimate.flightClass,
                estimate.earlyArrivalDays,
                eventDays,
                rosterSize,
                out estimate.subsidyAmount,
                out estimate.clubOutOfPocket);

            estimate.onSiteHour = competition.startHour - estimate.earlyArrivalDays * CareerCalendar.HoursPerDay;
            estimate.arriveHour = estimate.onSiteHour;
            estimate.departHour = Math.Max(0, estimate.arriveHour - estimate.flightHours - 3);
            estimate.returnDepartHour = competition.endHour + 8;
            estimate.returnArriveHour = estimate.returnDepartHour + estimate.flightHours + 2;
            return estimate;
        }

        public static void ApplyPlanToEntry(CompetitionEntry entry, CompetitionTravelEstimate estimate)
        {
            if (entry == null || estimate == null)
                return;
            entry.travelDepartHour = estimate.departHour;
            entry.travelArriveHour = estimate.arriveHour;
            entry.travelOnSiteHour = estimate.onSiteHour;
            entry.travelReturnDepartHour = estimate.returnDepartHour;
            entry.travelReturnArriveHour = estimate.returnArriveHour;
            entry.travelFlightClass = estimate.flightClass;
            entry.travelClubCost = estimate.clubOutOfPocket;
            entry.travelSubsidyAmount = estimate.subsidyAmount;
            entry.travelCostsPaid = false;
        }

        public static bool HasTravelPlan(CompetitionEntry entry) =>
            entry != null && entry.travelDepartHour > 0 && entry.travelReturnArriveHour > entry.travelDepartHour;

        public static bool IsInOutboundFlight(CompetitionEntry entry, int clockHour) =>
            HasTravelPlan(entry) &&
            clockHour >= entry.travelDepartHour &&
            clockHour < entry.travelArriveHour;

        public static bool IsOnSiteEarly(CompetitionEntry entry, CompetitionDefinition competition, int clockHour) =>
            HasTravelPlan(entry) &&
            competition != null &&
            clockHour >= entry.travelOnSiteHour &&
            clockHour < competition.startHour;

        public static bool IsInReturnFlight(CompetitionEntry entry, int clockHour) =>
            HasTravelPlan(entry) &&
            clockHour >= entry.travelReturnDepartHour &&
            clockHour < entry.travelReturnArriveHour;

        public static bool IsAwayForLan(CompetitionEntry entry, CompetitionDefinition competition, int clockHour) =>
            HasTravelPlan(entry) &&
            competition != null &&
            clockHour >= entry.travelDepartHour &&
            clockHour < entry.travelReturnArriveHour &&
            !entry.withdrawn &&
            entry.registered;

        public static int InFlightEnergyDelta(FlightClass flightClass, int hours)
        {
            if (hours <= 0)
                return 0;

            double perHour = flightClass switch
            {
                FlightClass.Business => 2.4d,
                FlightClass.PremiumEconomy => 0.8d,
                _ => -1.2d
            };
            return (int)Math.Round(perHour * hours);
        }

        public static double EarlySiteEnergyMultiplier(CompetitionEntry entry, CompetitionDefinition competition, int clockHour)
        {
            if (!IsOnSiteEarly(entry, competition, clockHour))
                return 1d;
            int hoursEarly = competition.startHour - Math.Max(clockHour, entry.travelOnSiteHour);
            if (hoursEarly >= 4 * CareerCalendar.HoursPerDay)
                return 1.35d;
            if (hoursEarly >= 2 * CareerCalendar.HoursPerDay)
                return 1.18d;
            return 1.08d;
        }

        public static string SubsidyLabel(CompetitionTravelSubsidy tier)
        {
            switch (tier)
            {
                case CompetitionTravelSubsidy.FlightEconomy: return "機票 經濟艙";
                case CompetitionTravelSubsidy.FlightBusiness: return "機票 商務艙";
                case CompetitionTravelSubsidy.FlightAndHotelEconomy: return "機票與住宿 經濟艙";
                case CompetitionTravelSubsidy.FlightAndHotelBusiness: return "機票與住宿 商務艙";
                case CompetitionTravelSubsidy.None: return "無補貼";
                default: return "無補貼";
            }
        }

        public static string FlightClassLabel(FlightClass flightClass)
        {
            switch (flightClass)
            {
                case FlightClass.PremiumEconomy: return "豪華經濟艙";
                case FlightClass.Business: return "商務艙";
                default: return "經濟艙";
            }
        }

        public static string BuildTravelSummary(
            CompetitionDefinition competition,
            CompetitionEntry entry,
            CareerBalanceDefinition balance,
            int clockHour)
        {
            if (!RequiresTravel(competition) || entry == null)
                return null;

            if (!HasTravelPlan(entry))
                return "差旅排程將於報名後建立。";

            var lines = new List<string>();
            if (entry.travelDepartHour > clockHour)
            {
                lines.Add($"出發 {CareerCalendar.FormatShort(balance, entry.travelDepartHour)}");
                lines.Add($"抵達賽地 {CareerCalendar.FormatShort(balance, entry.travelOnSiteHour)}");
            }
            else if (IsInOutboundFlight(entry, clockHour))
                lines.Add("選手途中：去程航班");
            else if (IsOnSiteEarly(entry, competition, clockHour))
                lines.Add("選手已抵達：適應與賽前訓練");
            else if (clockHour >= competition.startHour && clockHour <= competition.endHour)
                lines.Add("選手在線下賽現場");
            else if (IsInReturnFlight(entry, clockHour))
                lines.Add("選手途中：回程航班");

            lines.Add($"艙等 {FlightClassLabel(entry.travelFlightClass)}");
            if (entry.travelCostsPaid)
            {
                lines.Add($"差旅自付 ${entry.travelClubCost:N0} · 補貼 ${entry.travelSubsidyAmount:N0}");
            }
            else if (entry.travelClubCost > 0 || entry.travelSubsidyAmount > 0)
            {
                lines.Add($"預估自付 ${entry.travelClubCost:N0} · 補貼 ${entry.travelSubsidyAmount:N0}");
            }

            return string.Join("\n", lines);
        }
    }

    public static class FacilityRules
    {
        public static int UpgradeCost(FacilityDefinition definition, int targetLevel) =>
            CareerMath.RoundMoney(definition.baseUpgradeCost * Math.Pow(targetLevel, 1.7d));

        public static int UpgradeHours(FacilityDefinition definition, int targetLevel)
        {
            if (definition.upgradeHours != null && targetLevel - 1 < definition.upgradeHours.Length)
                return Math.Max(1, definition.upgradeHours[targetLevel - 1]);
            if (definition.upgradeWeeks != null && targetLevel - 1 < definition.upgradeWeeks.Length)
                return Math.Max(1, definition.upgradeWeeks[targetLevel - 1] * CareerCalendar.HoursPerWeek);
            return (4 + targetLevel * 3) * CareerCalendar.HoursPerWeek;
        }
    }
}
