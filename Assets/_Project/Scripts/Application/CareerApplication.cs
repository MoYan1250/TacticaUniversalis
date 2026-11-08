using System;
using System.Collections.Generic;
using System.Linq;
using TacticaUniversalis.Domain;

namespace TacticaUniversalis.Application
{
    public sealed class CareerMatchSummary
    {
        public string winnerClubId;
        public string loserClubId;
        public int winnerScore;
        public int loserScore;
        public float winnerRating;
        public float loserRating;
        public int exposure;
    }

    /// <summary>Result of advancing the career clock; used for Paradox-style auto-pause.</summary>
    public sealed class CareerAdvanceResult
    {
        public int hoursAdvanced;
        public bool playerCompetitionResolved;
        public bool playerMatchRequired;
        public bool newTransferOfferForPlayer;
        public bool transferDealInterrupted;
        public bool personalTermsDue;
        public bool monthClosed;
        public bool gameOver;
        public bool scriptedEventPending;
        /// <summary>Interactive scripted popup should pause the clock (tier prefs / force-pause).</summary>
        public bool scriptedEventShouldPause;
        /// <summary>C/D notification-bar event fired this advance.</summary>
        public bool eventToastPending;
        public string eventToastTitle;
        public string eventToastBody;
        public ScriptedEventTier eventToastTier;
        public bool eventToastShouldPause;

        public bool ShouldAutoPause =>
            playerMatchRequired || playerCompetitionResolved || newTransferOfferForPlayer ||
            transferDealInterrupted || personalTermsDue ||
            scriptedEventShouldPause || eventToastShouldPause || gameOver;
    }

    public interface ICareerMatchBridge
    {
        CareerMatchSummary Simulate(
            CareerState state,
            IReadOnlyDictionary<string, CareerPlayerDefinition> players,
            string clubAId, string clubBId, CompetitionTier tier, int seed);
    }

    public sealed class DeterministicCareerMatchBridge : ICareerMatchBridge
    {
        public CareerMatchSummary Simulate(
            CareerState state,
            IReadOnlyDictionary<string, CareerPlayerDefinition> definitions,
            string clubAId, string clubBId, CompetitionTier tier, int seed)
        {
            var random = new SeededRandom(seed);
            double a = ClubPower(state, clubAId) + (random.Next01() - .5f) * 12f;
            double b = ClubPower(state, clubBId) + (random.Next01() - .5f) * 12f;
            double chanceA = 1d / (1d + Math.Exp(-(a - b) / 8d));
            bool aWins = random.Next01() < chanceA;
            string winner = aWins ? clubAId : clubBId;
            string loser = aWins ? clubBId : clubAId;
            int losingScore = random.Range(5, 15);
            int exposure = 20 + (int)tier * 14;
            return new CareerMatchSummary
            {
                winnerClubId = winner,
                loserClubId = loser,
                winnerScore = 16,
                loserScore = losingScore,
                winnerRating = 1.08f + random.Next01() * .2f,
                loserRating = .75f + random.Next01() * .2f,
                exposure = exposure
            };
        }

        private static double ClubPower(CareerState state, string clubId)
        {
            PlayerCareerState[] roster = state.players.Where(x => x.clubId == clubId).ToArray();
            return roster.Length == 0 ? 50d : roster.Average(x => x.power + (x.form - 50) * .08);
        }
    }

    public sealed class QuickMatchCareerBridge : ICareerMatchBridge
    {
        private readonly ContentDatabase _matchContent;
        private readonly IMatchSimulator _simulator;
        private readonly ICareerMatchBridge _fallback = new DeterministicCareerMatchBridge();

        public QuickMatchCareerBridge(ContentDatabase matchContent, IMatchSimulator simulator = null)
        {
            _matchContent = matchContent;
            _simulator = simulator ?? new QuickMatchSimulator(
                new RuleBasedTacticalPlanner(), new FastRoundSimulator());
        }

        public CareerMatchSummary Simulate(
            CareerState state,
            IReadOnlyDictionary<string, CareerPlayerDefinition> players,
            string clubAId, string clubBId, CompetitionTier tier, int seed)
        {
            if (_matchContent == null ||
                !_matchContent.Teams.ContainsKey(clubAId) ||
                !_matchContent.Teams.ContainsKey(clubBId))
                return _fallback.Simulate(state, players, clubAId, clubBId, tier, seed);

            MatchResult result = _simulator.Simulate(_matchContent, clubAId, clubBId, seed);
            bool teamAWon = string.Equals(
                result.winnerTeamId, clubAId, StringComparison.OrdinalIgnoreCase);
            string winner = teamAWon ? clubAId : clubBId;
            string loser = teamAWon ? clubBId : clubAId;
            PlayerMatchSummary[] winnerPlayers = result.playerSummaries
                .Where(x => string.Equals(x.teamId, result.winnerTeamId, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            PlayerMatchSummary[] loserPlayers = result.playerSummaries
                .Where(x => !string.Equals(x.teamId, result.winnerTeamId, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            return new CareerMatchSummary
            {
                winnerClubId = winner,
                loserClubId = loser,
                winnerScore = teamAWon ? result.teamAScore : result.teamBScore,
                loserScore = teamAWon ? result.teamBScore : result.teamAScore,
                winnerRating = winnerPlayers.Length == 0 ? 1.1f : winnerPlayers.Average(x => x.rating),
                loserRating = loserPlayers.Length == 0 ? .9f : loserPlayers.Average(x => x.rating),
                exposure = 20 + (int)tier * 14
            };
        }
    }

    public sealed partial class CareerSession
    {
        private readonly CareerBalanceDefinition _balance;
        private readonly IReadOnlyDictionary<string, CareerPlayerDefinition> _playerDefinitions;
        private readonly IReadOnlyDictionary<string, CareerClubDefinition> _clubDefinitions;
        private readonly IReadOnlyDictionary<string, SponsorDefinition> _sponsorDefinitions;
        private readonly IReadOnlyDictionary<string, CompetitionDefinition> _competitions;
        private readonly IReadOnlyDictionary<FacilityType, FacilityDefinition> _facilityDefinitions;
        private readonly IReadOnlyDictionary<string, CareerScriptedEventDef> _scriptedEvents;
        private readonly IReadOnlyDictionary<string, int> _scriptedFireHours;
        private readonly IReadOnlyDictionary<string, CareerAchievementDef> _achievements;
        private readonly IReadOnlyDictionary<string, CareerCoachDefinition> _coachDefinitions;
        private readonly IReadOnlyDictionary<string, CareerMissionTreeDef> _missionTrees;
        private readonly ICareerMatchBridge _matches;
        private readonly ContentDatabase _interactiveMatchContent;
        private int _sequence;

        public CareerState State { get; }

        /// <summary>
        /// Presentation supplies pause preference per tier from game settings.
        /// Null → use domain defaults S+/S/A/B pause, C/D do not.
        /// </summary>
        public System.Func<ScriptedEventTier, bool> ShouldPauseOnEventTier { get; set; }

        public CareerSession(
            CareerState state,
            CareerBalanceDefinition balance,
            IEnumerable<CareerPlayerDefinition> players,
            IEnumerable<CareerClubDefinition> clubs,
            IEnumerable<SponsorDefinition> sponsors,
            IEnumerable<CompetitionDefinition> competitions,
            IEnumerable<FacilityDefinition> facilities,
            ICareerMatchBridge matchBridge = null,
            ContentDatabase interactiveMatchContent = null,
            IEnumerable<CareerScriptedEventDef> scriptedEvents = null,
            IEnumerable<CareerAchievementDef> achievements = null,
            IEnumerable<CareerCoachDefinition> coaches = null,
            IEnumerable<CareerMissionTreeDef> missionTrees = null)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _playerDefinitions = players.ToDictionary(x => x.id, StringComparer.Ordinal);
            _clubDefinitions = clubs.ToDictionary(x => x.id, StringComparer.OrdinalIgnoreCase);
            _sponsorDefinitions = sponsors.ToDictionary(x => x.id, StringComparer.OrdinalIgnoreCase);
            _competitions = competitions.ToDictionary(x => x.id, StringComparer.OrdinalIgnoreCase);
            _facilityDefinitions = facilities.ToDictionary(x => x.type);
            _scriptedEvents = (scriptedEvents ?? Array.Empty<CareerScriptedEventDef>())
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.id))
                .ToDictionary(x => x.id, StringComparer.OrdinalIgnoreCase);
            _scriptedFireHours = ScriptedEventScheduleRules.BuildSpreadFireHours(
                _scriptedEvents.Values, State.seed);
            _achievements = (achievements ?? Array.Empty<CareerAchievementDef>())
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.id))
                .ToDictionary(x => x.id, StringComparer.OrdinalIgnoreCase);
            _coachDefinitions = (coaches ?? Array.Empty<CareerCoachDefinition>())
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.id))
                .ToDictionary(x => x.id, StringComparer.OrdinalIgnoreCase);
            _missionTrees = (missionTrees ?? Array.Empty<CareerMissionTreeDef>())
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.id))
                .ToDictionary(x => x.id, StringComparer.OrdinalIgnoreCase);
            _matches = matchBridge ?? new DeterministicCareerMatchBridge();
            _interactiveMatchContent = interactiveMatchContent;
            _sequence = State.events.Count;
            State.flags = State.flags ?? new List<CareerFlag>();
            State.pendingEvents = State.pendingEvents ?? new List<CareerPendingEvent>();
            State.unlockedAchievements = State.unlockedAchievements ?? new List<string>();
            State.coaches = State.coaches ?? new List<CoachStaffState>();
            State.competitionSignupBans = State.competitionSignupBans ?? new List<CompetitionSignupBan>();
            CareerMissionRules.EnsureProgress(State);
            SyncYearFromClock();
            ProcessGradualCompetitionRegistration();
        }

        public static CareerState CreateNew(
            CareerBalanceDefinition balance,
            IEnumerable<CareerPlayerDefinition> playerDefinitions,
            IEnumerable<CareerClubDefinition> clubDefinitions,
            string playerClubId, CareerDifficulty difficulty, int seed,
            IEnumerable<CareerCoachDefinition> coachDefinitions = null)
        {
            var state = new CareerState
            {
                saveId = Guid.NewGuid().ToString("N"),
                difficulty = difficulty,
                seed = seed,
                year = balance.startingYear,
                clockHour = 0,
                playerClubId = playerClubId
            };
            foreach (CareerClubDefinition club in clubDefinitions
                         .OrderBy(c => c.hltvRank > 0 ? c.hltvRank : int.MaxValue)
                         .ThenBy(c => c.id, StringComparer.OrdinalIgnoreCase))
            {
                var clubState = new ClubState
                {
                    clubId = club.id,
                    cash = club.startingCash > 0 ? club.startingCash : balance.startingCash,
                    fame = club.fame,
                    fans = 15000 + club.fame * 1000,
                    exposure = club.fame
                };
                foreach (FacilityType type in Enum.GetValues(typeof(FacilityType)))
                    clubState.facilities.Add(new FacilityState { type = type, level = 1, targetLevel = 1 });
                ClubEstateSatisfactionRules.EnsureNormalized(clubState);
                state.clubs.Add(clubState);
            }

            foreach (CareerPlayerDefinition definition in playerDefinitions)
            {
                int debutYear = ClubEstateRules.ResolveDebutYear(definition, balance);
                string squadStatus = ClubEstateRules.NormalizeSquadStatus(definition.squadStatus);
                state.players.Add(new PlayerCareerState
                {
                    playerId = definition.id,
                    clubId = definition.clubId,
                    age = definition.age,
                    power = definition.power,
                    potential = definition.potential,
                    fame = definition.fame,
                    loyalty = definition.loyalty,
                    pressure = 40,
                    trajectory = definition.trajectory,
                    debutYear = debutYear,
                    squadStatus = squadStatus,
                    experience = Math.Max(0, balance.startingYear - debutYear),
                    roleProficiency = 70,
                    mapProficiency = 60,
                    trainingFocus = TrainingPlanRules.Serialize(new CareerTrainingPlan
                    {
                        attributes = new List<string> { "aim" },
                        maps = new List<string> { "Dust II" },
                        role = string.IsNullOrEmpty(definition.role) ? "Rifler" : definition.role
                    }),
                    unknownGenius = definition.age <= 20 && definition.potential >= 88 &&
                        StablePercent(seed, definition.id) < 20
                });
                DevelopmentRules.EnsureProgressLists(state.players[state.players.Count - 1], definition.role);
                if (!string.IsNullOrEmpty(definition.clubId))
                {
                    state.contracts.Add(new ContractState
                    {
                        id = "contract_" + definition.id,
                        playerId = definition.id,
                        clubId = definition.clubId,
                        startYear = balance.startingYear,
                        startHour = 0,
                        remainingMonths = 24,
                        monthlySalary = definition.monthlySalary,
                        releaseClause = Math.Max(definition.marketValue * 2, 100000),
                        prizeShare = .08f,
                        appearanceGuarantee = .5f
                    });
                }
            }

            state.coaches = state.coaches ?? new List<CoachStaffState>();
            foreach (CareerCoachDefinition coachDef in coachDefinitions ?? Array.Empty<CareerCoachDefinition>())
            {
                if (coachDef == null || string.IsNullOrWhiteSpace(coachDef.id))
                    continue;
                CoachStaffState hired = ClubCoachStaffRules.CreateFromDefinition(coachDef);
                if (hired != null)
                    state.coaches.Add(hired);
            }

            var clubById = clubDefinitions.ToDictionary(x => x.id, StringComparer.OrdinalIgnoreCase);
            foreach (ClubState clubState in state.clubs)
            {
                if (!clubById.TryGetValue(clubState.clubId, out CareerClubDefinition definition))
                    continue;
                if (definition.hltvPoints <= 0)
                    continue;
                state.rankingBatches.Add(new RankingBatch
                {
                    id = "seed_" + clubState.clubId,
                    clubId = clubState.clubId,
                    earnedHour = 0,
                    points = definition.hltvPoints,
                    seedSnapshot = true
                });
            }

            state.rankings = RankingRules.Build(
                state.rankingBatches,
                state.clubs.Select(x => x.clubId),
                state.clockHour,
                CareerCalendar.ResolveRankingHalfLifeHours(balance),
                CareerCalendar.ResolveRankingWindowHours(balance));

            state.rmrBatches = state.rmrBatches ?? new List<RmrPointBatch>();
            foreach (ClubState clubState in state.clubs)
            {
                if (!clubById.TryGetValue(clubState.clubId, out CareerClubDefinition clubDef))
                    continue;
                int circuit = RmrRules.CircuitSeedFromClub(clubDef);
                if (circuit <= 0)
                    continue;
                state.rmrBatches.Add(RmrRules.CreateCircuitSeedBatch(clubState.clubId, circuit));
            }

            state.rmrRankings = RmrRules.Build(state.rmrBatches, clubDefinitions);
            return state;
        }

        public CompetitionTravelEstimate GetCompetitionTravelEstimate(string competitionId)
        {
            if (!_competitions.TryGetValue(competitionId, out CompetitionDefinition competition))
                return new CompetitionTravelEstimate();
            ClubState club = State.Club(State.playerClubId);
            return BuildTravelEstimateForClub(competition, club.clubId, club.cash, preferComfort: true);
        }

        private CompetitionTravelEstimate BuildTravelEstimateForClub(
            CompetitionDefinition competition,
            string clubId,
            int clubCash,
            bool preferComfort)
        {
            _clubDefinitions.TryGetValue(clubId, out CareerClubDefinition clubDef);
            string region = clubDef?.region ?? competition.region ?? "Europe";
            int rosterSize = Math.Max(5, State.players.Count(x =>
                string.Equals(x.clubId, clubId, StringComparison.OrdinalIgnoreCase)));
            if (!preferComfort && clubDef != null)
                preferComfort = clubDef.ambition >= 70;
            return CompetitionTravelRules.BuildEstimate(
                competition, region, rosterSize, clubCash, preferComfort);
        }

        private void EnsureCompetitionTravelPlan(string clubId, CompetitionDefinition competition, CompetitionEntry entry)
        {
            if (!CompetitionTravelRules.RequiresTravel(competition) || entry == null)
                return;

            ClubState club = State.TryGetClub(clubId, out ClubState resolved) ? resolved : null;
            int cash = club?.cash ?? 0;
            bool preferComfort = string.Equals(clubId, State.playerClubId, StringComparison.OrdinalIgnoreCase) ||
                                 (_clubDefinitions.TryGetValue(clubId, out CareerClubDefinition def) && def.ambition >= 75);
            CompetitionTravelEstimate estimate = BuildTravelEstimateForClub(
                competition, clubId, cash, preferComfort);
            CompetitionTravelRules.ApplyPlanToEntry(entry, estimate);
        }

        private void ProcessCompetitionTravel(int fromHour, int toHour)
        {
            if (toHour <= fromHour)
                return;

            foreach (CompetitionEntry entry in State.competitionEntries)
            {
                if (entry == null || !entry.registered || entry.withdrawn || entry.completed)
                    continue;
                if (!_competitions.TryGetValue(entry.competitionId, out CompetitionDefinition competition))
                    continue;
                if (!CompetitionTravelRules.RequiresTravel(competition))
                    continue;
                if (!CompetitionTravelRules.HasTravelPlan(entry))
                    EnsureCompetitionTravelPlan(entry.clubId, competition, entry);

                if (!entry.travelCostsPaid &&
                    entry.travelDepartHour > 0 &&
                    entry.travelDepartHour <= toHour)
                {
                    SettleCompetitionTravelCosts(entry, competition);
                }

                int flightHours = Math.Max(0, Math.Min(toHour, entry.travelArriveHour) -
                                             Math.Max(fromHour, entry.travelDepartHour));
                if (flightHours > 0)
                    ApplyInFlightEnergy(entry.clubId, entry.travelFlightClass, flightHours);

                int returnHours = Math.Max(0, Math.Min(toHour, entry.travelReturnArriveHour) -
                                              Math.Max(fromHour, entry.travelReturnDepartHour));
                if (returnHours > 0)
                    ApplyInFlightEnergy(entry.clubId, entry.travelFlightClass, returnHours);
            }
        }

        private void SettleCompetitionTravelCosts(CompetitionEntry entry, CompetitionDefinition competition)
        {
            if (entry == null || entry.travelCostsPaid || !State.TryGetClub(entry.clubId, out ClubState club))
                return;

            int due = Math.Max(0, entry.travelClubCost);
            if (due > 0)
            {
                club.cash -= due;
                AddLedger(club.clubId, "travel", -due, competition.id, "線下賽差旅自付");
            }

            entry.travelCostsPaid = true;
            if (string.Equals(entry.clubId, State.playerClubId, StringComparison.OrdinalIgnoreCase))
            {
                AddEvent(CareerEventType.FinancialStatusChanged, club.clubId, competition.id,
                    $"已支付差旅 ${due:N0}，補貼 ${entry.travelSubsidyAmount:N0}");
            }
        }

        private void ApplyInFlightEnergy(string clubId, FlightClass flightClass, int hours)
        {
            if (hours <= 0)
                return;

            int delta = CompetitionTravelRules.InFlightEnergyDelta(flightClass, hours);
            foreach (PlayerCareerState player in State.players.Where(x =>
                         string.Equals(x.clubId, clubId, StringComparison.OrdinalIgnoreCase)))
                player.energy = Math.Max(0, Math.Min(100, player.energy + delta));
        }

        private bool IsClubRosterInFlight(string clubId, int hour)
        {
            foreach (CompetitionEntry entry in State.competitionEntries)
            {
                if (!entry.registered || entry.withdrawn)
                    continue;
                if (!string.Equals(entry.clubId, clubId, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (CompetitionTravelRules.IsInOutboundFlight(entry, hour) ||
                    CompetitionTravelRules.IsInReturnFlight(entry, hour))
                    return true;
            }

            return false;
        }

        private double TravelEarlySiteEnergyMultiplier(string clubId)
        {
            foreach (CompetitionEntry entry in State.competitionEntries)
            {
                if (!entry.registered || entry.withdrawn)
                    continue;
                if (!string.Equals(entry.clubId, clubId, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!_competitions.TryGetValue(entry.competitionId, out CompetitionDefinition competition))
                    continue;
                double mul = CompetitionTravelRules.EarlySiteEnergyMultiplier(
                    entry, competition, State.clockHour);
                if (mul > 1d)
                    return mul;
            }

            return 1d;
        }

        public bool SetTrainingFocus(string playerId, string focus)
        {
            PlayerCareerState player = State.players.FirstOrDefault(x => x.playerId == playerId);
            if (player == null || player.clubId != State.playerClubId) return false;
            CareerTrainingPlan plan = TrainingPlanRules.Parse(focus);
            player.trainingFocus = TrainingPlanRules.Serialize(plan);
            return true;
        }

        public bool RegisterCompetition(string competitionId) =>
            TryRegisterCompetition(competitionId, null, out _);

        public bool TryRegisterCompetition(
            string competitionId,
            CompetitionRegisterOptions options,
            out RegisterCompetitionResult result)
        {
            result = new RegisterCompetitionResult();
            options = options ?? new CompetitionRegisterOptions();

            if (!_competitions.TryGetValue(competitionId, out CompetitionDefinition competition))
            {
                result.failureReason = "找不到賽事";
                return false;
            }

            if (CareerCalendar.IsCancelled(competition))
            {
                result.failureReason = "賽事已取消";
                return false;
            }

            bool rmrMajor = StockholmMajorRules.UsesRmrQualification(competition);
            if (rmrMajor)
            {
                List<MajorQualifiedSeat> seats = StockholmMajorRules.BuildQualifiedSeats(State.rmrRankings);
                if (!StockholmMajorRules.IsClubQualified(seats, State.playerClubId))
                {
                    result.failureReason = "尚未取得地區 Major 排名席位";
                    return false;
                }
            }

            RankingEntry rank = State.rankings.FirstOrDefault(x => x.clubId == State.playerClubId);
            ClubState club = State.Club(State.playerClubId);
            int softRankCap = CompetitionRegistrationRules.SoftMaximumWorldRank(competition);
            if (!CareerCalendar.IsRegisterableThisMonth(competition, _balance, State.clockHour))
            {
                result.failureReason = "目前不在報名期間";
                return false;
            }

            if (!rmrMajor &&
                softRankCap < int.MaxValue &&
                rank != null &&
                rank.rank > softRankCap)
            {
                result.failureReason = "世界排名未達報名門檻";
                return false;
            }

            int travelOutOfPocket = 0;
            if (CompetitionTravelRules.RequiresTravel(competition))
            {
                CompetitionTravelEstimate travel = BuildTravelEstimateForClub(
                    competition, club.clubId, club.cash, club.clubId == State.playerClubId);
                travelOutOfPocket = travel.clubOutOfPocket;
            }

            if (club.cash < competition.entryFee + travelOutOfPocket)
            {
                result.failureReason = "現金不足以支付報名費或差旅";
                return false;
            }

            if (!string.IsNullOrEmpty(competition.region) &&
                competition.format != CompetitionFormat.LAN &&
                competition.format != CompetitionFormat.Hybrid &&
                !_clubDefinitions[club.clubId].region.Equals(competition.region, StringComparison.OrdinalIgnoreCase))
            {
                result.failureReason = "線上賽事地區不符";
                return false;
            }

            if (CompetitionWithdrawRules.HasActiveSignupBan(
                    State.competitionSignupBans, club.clubId, competition))
            {
                result.failureReason = "目前受到該賽事主辦或級別的報名限制";
                return false;
            }

            if (IsClubRegistered(competitionId, club.clubId))
            {
                result.failureReason = "已報名此賽事";
                return false;
            }

            CompetitionCommitment candidate = ToCommitment(competition, isLockedSeat: rmrMajor);
            List<CompetitionCommitment> existing = BuildClubCommitments(club.clubId);
            CompetitionScheduleEvaluation schedule = CompetitionScheduleRules.EvaluatePlayerSignup(
                candidate,
                existing,
                options.confirmOnlineOverlap,
                options.keepCompetitionIds);

            result.scheduleVerdict = schedule.verdict;
            result.conflictCompetitionIds = schedule.conflictCompetitionIds ?? new List<string>();
            result.conflictDisplayNames = schedule.conflictDisplayNames ?? new List<string>();

            if (schedule.verdict == CompetitionScheduleVerdict.NeedsOnlineOverlapConfirm ||
                schedule.verdict == CompetitionScheduleVerdict.NeedsChooseTwo ||
                schedule.verdict == CompetitionScheduleVerdict.BlockedDualPhysical ||
                schedule.verdict == CompetitionScheduleVerdict.BlockedChooseTwoLocked)
            {
                result.failureReason = schedule.message;
                return false;
            }

            if (options.keepCompetitionIds != null && options.keepCompetitionIds.Length > 0)
            {
                HashSet<string> keep = new HashSet<string>(
                    options.keepCompetitionIds.Where(x => !string.IsNullOrWhiteSpace(x)),
                    StringComparer.OrdinalIgnoreCase);
                foreach (string conflictId in result.conflictCompetitionIds)
                {
                    if (keep.Contains(conflictId))
                        continue;
                    if (string.Equals(conflictId, competitionId, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (!CancelCompetitionRegistration(
                            conflictId, CompetitionWithdrawReason.Voluntary, out string cancelMsg))
                    {
                        result.failureReason = cancelMsg ?? "無法取消衝突賽事報名";
                        return false;
                    }
                }

                if (!keep.Contains(competitionId))
                {
                    result.success = true;
                    result.failureReason = "已依選擇取消報名，未加入新賽事";
                    return true;
                }
            }

            RegisterClubForCompetition(competitionId, club.clubId);
            if (competition.entryFee > 0)
            {
                AddLedger(club.clubId, "entry_fee", -competition.entryFee, competition.id, "賽事報名費");
                club.cash -= competition.entryFee;
            }

            ProcessGradualCompetitionRegistration();
            result.success = true;
            return true;
        }

        private List<CompetitionCommitment> BuildClubCommitments(string clubId)
        {
            var list = new List<CompetitionCommitment>();
            foreach (CompetitionEntry entry in State.competitionEntries)
            {
                if (entry == null || !entry.registered || entry.withdrawn || entry.completed)
                    continue;
                if (!string.Equals(entry.clubId, clubId, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!_competitions.TryGetValue(entry.competitionId, out CompetitionDefinition def))
                    continue;
                if (CareerCalendar.IsCancelled(def))
                    continue;
                list.Add(ToCommitment(def, StockholmMajorRules.UsesRmrQualification(def)));
            }

            return list;
        }

        private CompetitionCommitment ToCommitment(CompetitionDefinition competition, bool isLockedSeat)
        {
            string name = competition.displayName;
            if (string.IsNullOrWhiteSpace(name))
                name = competition.id;
            return new CompetitionCommitment
            {
                competitionId = competition.id,
                displayName = name,
                format = competition.format,
                startHour = competition.startHour,
                endHour = competition.endHour,
                isLockedSeat = isLockedSeat
            };
        }

        public CompetitionWithdrawEvaluation EvaluateCompetitionWithdraw(
            string competitionId, CompetitionWithdrawReason reason = CompetitionWithdrawReason.Voluntary)
        {
            if (!_competitions.TryGetValue(competitionId, out CompetitionDefinition competition))
            {
                return new CompetitionWithdrawEvaluation { blockReason = "找不到賽事" };
            }

            if (StockholmMajorRules.UsesRmrQualification(competition))
            {
                return new CompetitionWithdrawEvaluation
                {
                    canWithdraw = false,
                    blockReason = "地區 Major 排名取得的席位不可取消報名"
                };
            }

            CompetitionEntry entry = State.competitionEntries.FirstOrDefault(x =>
                string.Equals(x.competitionId, competitionId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.clubId, State.playerClubId, StringComparison.OrdinalIgnoreCase));
            return CompetitionWithdrawRules.Evaluate(competition, entry, State.clockHour, reason);
        }

        public bool CancelCompetitionRegistration(
            string competitionId,
            CompetitionWithdrawReason reason,
            out string message)
        {
            message = null;
            if (!_competitions.TryGetValue(competitionId, out CompetitionDefinition competition))
            {
                message = "找不到賽事";
                return false;
            }

            if (StockholmMajorRules.UsesRmrQualification(competition))
            {
                message = "地區 Major 排名取得的席位不可取消報名";
                return false;
            }

            CompetitionEntry entry = State.competitionEntries.FirstOrDefault(x =>
                string.Equals(x.competitionId, competitionId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.clubId, State.playerClubId, StringComparison.OrdinalIgnoreCase));
            CompetitionWithdrawEvaluation evaluation =
                CompetitionWithdrawRules.Evaluate(competition, entry, State.clockHour, reason);
            if (!evaluation.canWithdraw)
            {
                message = evaluation.blockReason;
                return false;
            }

            ApplyCompetitionWithdrawal(competition, entry, State.playerClubId, reason, evaluation);
            message = evaluation.summary;
            ProcessGradualCompetitionRegistration();
            return true;
        }

        public bool SignSponsor(string sponsorId) =>
            TrySignSponsor(sponsorId, out _);

        public SponsorSignEvaluation EvaluateSponsorSign(string sponsorId)
        {
            var evaluation = new SponsorSignEvaluation();
            if (!_sponsorDefinitions.TryGetValue(sponsorId, out SponsorDefinition definition))
            {
                evaluation.blockReason = "找不到贊助商";
                return evaluation;
            }

            evaluation.definition = definition;
            ClubState club = State.Club(State.playerClubId);
            evaluation.currentFame = club.fame;
            evaluation.requiredFame = definition.minimumFame;
            evaluation.currentExposure = club.exposure;
            evaluation.requiredExposure = definition.minimumExposure;
            evaluation.currentRankPercentile = PlayerClubRankPercentile(club.clubId);
            evaluation.requiredRankPercentile = definition.minimumRankPercentile;
            evaluation.meetsFame = club.fame >= definition.minimumFame;
            evaluation.meetsExposure = club.exposure >= definition.minimumExposure;
            evaluation.meetsRank = evaluation.currentRankPercentile >= definition.minimumRankPercentile;

            SponsorContract[] active = State.sponsors
                .Where(x => x.clubId == club.clubId).ToArray();
            evaluation.activeSponsorCount = active.Length;
            evaluation.alreadySigned = active.Any(x =>
                string.Equals(x.sponsorId, sponsorId, StringComparison.OrdinalIgnoreCase));
            evaluation.rosterFull = active.Length >= SponsorRules.MaxActiveSponsorsPerClub;

            int monthKey = SponsorRules.CalendarMonthKey(_balance, State.clockHour);
            evaluation.monthlyLimitReached = State.lastSponsorSignMonthKey == monthKey;

            if (evaluation.alreadySigned)
                evaluation.blockReason = "已與此贊助商簽約";
            else if (evaluation.monthlyLimitReached)
                evaluation.blockReason = "本月已簽訂贊助，下個月才能再簽一家";
            else if (evaluation.rosterFull)
                evaluation.blockReason = $"已達 {SponsorRules.MaxActiveSponsorsPerClub} 家贊助上限";
            else if (!evaluation.meetsFame || !evaluation.meetsExposure || !evaluation.meetsRank)
                evaluation.blockReason = "未達贊助商要求";

            evaluation.canSign = string.IsNullOrEmpty(evaluation.blockReason);
            return evaluation;
        }

        public bool TrySignSponsor(string sponsorId, out string failureReason)
        {
            failureReason = null;
            SponsorSignEvaluation evaluation = EvaluateSponsorSign(sponsorId);
            if (!evaluation.canSign)
            {
                failureReason = evaluation.blockReason;
                return false;
            }

            SponsorDefinition definition = evaluation.definition;
            ClubState club = State.Club(State.playerClubId);
            State.sponsors.Add(new SponsorContract
            {
                id = $"sponsor_{sponsorId}_{State.clockHour}",
                sponsorId = sponsorId,
                clubId = club.clubId,
                tier = definition.tier,
                remainingMonths = SponsorRules.DefaultContractMonths,
                monthlyFee = definition.baseMonthlyFee,
                resultBonus = definition.baseResultBonus,
                minimumExposure = definition.minimumExposure,
                satisfaction = 60
            });
            State.lastSponsorSignMonthKey = SponsorRules.CalendarMonthKey(_balance, State.clockHour);
            AddEvent(CareerEventType.SponsorChanged, club.clubId, sponsorId, "簽訂贊助合約");
            return true;
        }

        private int PlayerClubRankPercentile(string clubId)
        {
            RankingEntry rank = State.rankings.FirstOrDefault(x => x.clubId == clubId);
            if (rank == null || State.rankings.Count == 0)
                return 0;
            return CareerMath.RoundMoney((State.rankings.Count - rank.rank + 1d) / State.rankings.Count * 100d);
        }

        public bool StartFacilityUpgrade(FacilityType type)
        {
            _ = type;
            // Facility / equipment upgrades are disabled pending redesign.
            return false;
        }

        private void ProcessFacilities(int hours)
        {
            _ = hours;
            // Facility upgrade progression disabled; cancel any in-progress upgrades without leveling.
            foreach (ClubState club in State.clubs)
            {
                if (club?.facilities == null) continue;
                foreach (FacilityState facility in club.facilities)
                {
                    if (facility == null || facility.upgradeHoursRemaining <= 0) continue;
                    facility.upgradeHoursRemaining = 0;
                    facility.targetLevel = facility.level;
                }
            }
        }

        public TransferOffer MakeTransferOffer(string playerId, int fee)
        {
            if (!TransferNegotiationRules.QuoteSystemEnabled)
                return null;
            PlayerCareerState player = State.players.FirstOrDefault(x => x.playerId == playerId);
            ClubState buyer = State.Club(State.playerClubId);
            if (player == null || player.clubId == buyer.clubId || fee <= 0 ||
                !TransferWindowOpen() || buyer.cash - fee < EstimatedMonthlyExpense(buyer.clubId) * 3)
                return null;
            if (!_playerDefinitions.TryGetValue(playerId, out CareerPlayerDefinition definition))
                return null;
            if (!MarketRules.ClubWantsPlayer(State.players, _playerDefinitions, buyer.clubId, player, definition))
                return null;

            ContractState contract = State.contracts.FirstOrDefault(x => x.playerId == playerId);
            bool buyout = TransferNegotiationRules.IsReleaseClauseBuyout(contract, fee);
            var offer = new TransferOffer
            {
                id = "offer_" + State.year + "_" + State.clockHour + "_" + (++_sequence),
                playerId = playerId,
                buyerClubId = buyer.clubId,
                sellerClubId = player.clubId,
                fee = fee,
                createdHour = State.clockHour,
                expiresHour = State.clockHour + TransferNegotiationRules.FeeOfferWindowHours,
                releaseClauseBid = buyout,
                status = TransferOfferStatus.Pending
            };

            if (buyout)
            {
                int marketValue = MarketRules.CalculateMarketValue(player, definition, contract,
                    definition.role == "Awper" ? 120 : 100);
                int asked = TransferNegotiationRules.DefaultAskedSalary(player, marketValue);
                TransferNegotiationRules.ApplyPersonalTermsPhase(offer, State.clockHour, asked);
                OutbidVulnerableDealsForPlayer(playerId, offer.id, fee, releaseClausePath: true);
                AddEvent(CareerEventType.ContractChanged, buyer.clubId, playerId,
                    "觸發違約金買斷，進入個人條款談判");
            }

            State.offers.Add(offer);
            return offer;
        }

        public bool ClubInterestedInSigning(string playerId)
        {
            PlayerCareerState player = State.players.FirstOrDefault(x => x.playerId == playerId);
            if (player == null || !_playerDefinitions.TryGetValue(player.playerId, out CareerPlayerDefinition definition))
                return false;
            return MarketRules.ClubWantsPlayer(
                State.players, _playerDefinitions, State.playerClubId, player, definition);
        }

        public bool ResolvePlayerOffer(string offerId, TransferOfferStatus response, int counterFee = 0)
        {
            if (!TransferNegotiationRules.QuoteSystemEnabled)
                return false;
            TransferOffer offer = State.offers.FirstOrDefault(x => x.id == offerId);
            if (offer == null ||
                (offer.status != TransferOfferStatus.Pending && offer.status != TransferOfferStatus.Countered) ||
                offer.sellerClubId != State.playerClubId)
                return false;
            if (response == TransferOfferStatus.Countered && counterFee > offer.fee)
            {
                offer.fee = counterFee;
                offer.status = TransferOfferStatus.Countered;
                return true;
            }
            if (response == TransferOfferStatus.Rejected)
            {
                offer.status = TransferOfferStatus.Rejected;
                return true;
            }
            if (response == TransferOfferStatus.Accepted)
            {
                TransferNegotiationRules.ApplyClubAgreedPhase(offer, State.clockHour);
                AddEvent(CareerEventType.ContractChanged, offer.sellerClubId, offer.playerId,
                    "暫允轉會費，進入個人條款短窗");
                return true;
            }
            offer.status = response;
            return true;
        }

        /// <summary>Buyer proposes personal contract terms during ClubAgreed / PersonalTerms.</summary>
        public bool ProposePlayerContract(
            string offerId, int monthlySalary, int signingBonus = 0, int contractMonths = 0)
        {
            if (!TransferNegotiationRules.QuoteSystemEnabled)
                return false;
            TransferOffer offer = State.offers.FirstOrDefault(x => x.id == offerId);
            if (offer == null ||
                !string.Equals(offer.buyerClubId, State.playerClubId, StringComparison.OrdinalIgnoreCase))
                return false;
            if (offer.status != TransferOfferStatus.ClubAgreed &&
                offer.status != TransferOfferStatus.PersonalTerms)
                return false;

            PlayerCareerState player = State.players.FirstOrDefault(x => x.playerId == offer.playerId);
            if (player == null || !_playerDefinitions.TryGetValue(player.playerId, out CareerPlayerDefinition definition))
                return false;

            ContractState contract = State.contracts.FirstOrDefault(x => x.playerId == player.playerId);
            int marketValue = MarketRules.CalculateMarketValue(player, definition, contract,
                definition.role == "Awper" ? 120 : 100);
            int asked = offer.askedSalary > 0
                ? offer.askedSalary
                : TransferNegotiationRules.DefaultAskedSalary(player, marketValue);
            int months = contractMonths > 0
                ? contractMonths
                : TransferNegotiationRules.DefaultContractMonths;
            TransferNegotiationRules.ApplyPersonalTermsPhase(
                offer, State.clockHour, asked, monthlySalary, signingBonus, months);
            offer.negotiationRound++;
            return true;
        }

        /// <summary>Resolve personal terms for a deal where the player club is the buyer.</summary>
        public bool ResolvePlayerContractTerms(string offerId, bool accept)
        {
            if (!TransferNegotiationRules.QuoteSystemEnabled)
                return false;
            TransferOffer offer = State.offers.FirstOrDefault(x => x.id == offerId);
            if (offer == null || offer.status != TransferOfferStatus.PersonalTerms ||
                !string.Equals(offer.buyerClubId, State.playerClubId, StringComparison.OrdinalIgnoreCase))
                return false;

            if (!accept)
            {
                if (offer.negotiationRound >= 3)
                {
                    offer.status = TransferOfferStatus.Failed;
                    PlayerCareerState player = State.Player(offer.playerId);
                    player.loyalty = Math.Max(0, player.loyalty - 2);
                    return true;
                }
                return true;
            }

            PlayerCareerState target = State.Player(offer.playerId);
            double score = BuildPersonalTermsScore(offer, target);
            var random = new SeededRandom(State.seed + State.clockHour * 104729 + offer.fee);
            bool accepted = score >= 90d ||
                            TransferNegotiationRules.PlayerAcceptsPersonalTerms(score, random);
            if (!accepted)
            {
                offer.negotiationRound++;
                if (offer.negotiationRound >= 3)
                {
                    offer.status = TransferOfferStatus.Failed;
                    target.loyalty = Math.Max(0, target.loyalty - 2);
                }
                return false;
            }

            CompleteTransfer(offer);
            return true;
        }

        /// <summary>True when the club can trigger a release-clause buyout at the listed fee.</summary>
        public bool CanTriggerReleaseClause(string playerId, int fee)
        {
            ContractState contract = State.contracts.FirstOrDefault(x => x.playerId == playerId);
            return TransferNegotiationRules.IsReleaseClauseBuyout(contract, fee);
        }

        public int EstimateMinimumTransferFee(string playerId)
        {
            PlayerCareerState player = State.players.FirstOrDefault(x => x.playerId == playerId);
            if (player == null || !_playerDefinitions.TryGetValue(playerId, out CareerPlayerDefinition definition))
                return 10000;
            ContractState contract = State.contracts.FirstOrDefault(x => x.playerId == playerId);
            int value = MarketRules.CalculateMarketValue(player, definition, contract,
                definition.role == "Awper" ? 120 : 100);
            return TransferNegotiationRules.EstimatedMinimumFee(value);
        }

        public double ClubAveragePower(string clubId)
        {
            PlayerCareerState[] roster = State.players
                .Where(x => string.Equals(x.clubId, clubId, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            return roster.Length == 0 ? 50d : roster.Average(x => x.power);
        }

        public bool TryPublishCommunityPost(string templateId, out string message)
        {
            message = null;
            CommunityPostTemplate template = CommunityOperationsRules.FindTemplate(templateId);
            if (template == null)
            {
                message = "未知的發文類型";
                return false;
            }

            ClubState club = State.Club(State.playerClubId);
            if (!CommunityOperationsRules.CanPublish(State, club, template, out message))
                return false;

            if (!CommunityOperationsRules.TrySelectEnergyTargets(
                    State, template, out List<PlayerCareerState> energyTargets, out message))
                return false;

            if (template.cashCost > 0)
            {
                club.cash -= template.cashCost;
                AddLedger(club.clubId, "community_ops", -template.cashCost, template.id, template.title);
            }

            club.fame = Math.Max(0, Math.Min(100, club.fame + template.fameDelta));
            club.exposure = Math.Max(0, Math.Min(100, club.exposure + template.exposureDelta));
            club.fans = Math.Max(0, club.fans + template.fansDelta);
            CommunityOperationsRules.ApplyEnergyCost(energyTargets, template.playerEnergyCost);

            string featuredId = energyTargets != null && energyTargets.Count > 0
                ? energyTargets[0].playerId
                : null;

            State.communityPosts = State.communityPosts ?? new List<CommunityPostLogEntry>();
            State.communityPosts.Add(new CommunityPostLogEntry
            {
                templateId = template.id,
                clockHour = State.clockHour,
                fameDelta = template.fameDelta,
                exposureDelta = template.exposureDelta,
                fansDelta = template.fansDelta,
                featuredPlayerId = featuredId
            });
            const int maxLog = 48;
            if (State.communityPosts.Count > maxLog)
                State.communityPosts.RemoveRange(0, State.communityPosts.Count - maxLog);

            message = $"已發布「{template.title}」· {CommunityOperationsRules.FormatEffectPreview(template)}";
            if (energyTargets != null && energyTargets.Count > 0 && template.playerEnergyCost > 0)
                message += $" · {energyTargets.Count} 名選手精力 −{template.playerEnergyCost}";
            // Auto daily updates stay in community log only — not the news feed.
            if (!string.Equals(template.id, CommunityOperationsRules.DailyUpdateTemplateId,
                    StringComparison.OrdinalIgnoreCase))
                AddEvent(CareerEventType.CommunityPost, club.clubId, template.id, message);
            return true;
        }

        public bool SetCommunityAutoDailyUpdate(bool enabled, out string message)
        {
            message = null;
            State.communityAutoDailyUpdate = enabled;
            if (!enabled)
            {
                message = "已關閉自動日常動態";
                return true;
            }

            message = "已開啟自動日常動態（每日 0:00 後首次推進時間時發布，並立即嘗試今日一則）";
            TryPublishCommunityPost(CommunityOperationsRules.DailyUpdateTemplateId, out string publishMessage);
            if (!string.IsNullOrEmpty(publishMessage) &&
                publishMessage.StartsWith("已發布", StringComparison.Ordinal))
                message = publishMessage + " · 自動日常動態已開啟";
            return true;
        }

        private void TryAutoCommunityDailyUpdate()
        {
            if (!State.communityAutoDailyUpdate || State.gameOver)
                return;
            TryPublishCommunityPost(CommunityOperationsRules.DailyUpdateTemplateId, out _);
        }

        /// <summary>Advances the career clock by the given number of hours.</summary>
        public CareerAdvanceResult AdvanceHours(int hours)
        {
            var result = new CareerAdvanceResult();
            if (State.gameOver || hours <= 0)
            {
                result.gameOver = State.gameOver;
                return result;
            }

            TryPromoteScheduledPlayerMatch(result);
            if (State.competitionRun?.awaitingPlayerMatch == true)
            {
                result.playerMatchRequired = true;
                return result;
            }
            if (State.pendingEvents != null && State.pendingEvents.Count > 0)
            {
                CareerScriptedEventDef pendingDef = GetPendingEventDefinition();
                if (pendingDef != null && ScriptedEventTierRules.UsesInteractivePopup(pendingDef))
                    result.scriptedEventPending = true;
                if (PendingEventBlocksClock())
                    result.scriptedEventShouldPause = true;
                return result;
            }

            int startedHour = State.clockHour;
            int pendingOffersBefore = CountPendingOffersForPlayer();
            int activePlayerDealsBefore = CountActivePlayerBuyerDeals();
            int tick = CareerCalendar.ResolveTrainingTickHours(_balance);
            int remaining = hours;
            while (remaining > 0 && !State.gameOver)
            {
                int step = Math.Min(remaining, NextBoundaryDelta());
                if (step <= 0) step = remaining;

                int before = State.clockHour;
                DateTime beforeDt = CareerCalendar.ToDateTime(_balance, before);
                State.clockHour += step;
                SyncYearFromClock();
                DateTime afterDt = CareerCalendar.ToDateTime(_balance, State.clockHour);

                TryPromoteScheduledPlayerMatch(result);
                if (result.playerMatchRequired)
                    break;

                ProcessFacilities(step);
                ProcessCompetitionTravel(before, State.clockHour);
                ProcessTrainingHours(step);
                ClubCoachStaffRules.ApplyHourlyEnergyRecovery(State, step);

                int dayBefore = CommunityOperationsRules.DayIndex(before);
                int dayAfter = CommunityOperationsRules.DayIndex(State.clockHour);
                if (dayAfter > dayBefore)
                {
                    TryAutoCommunityDailyUpdate();
                    ProcessGradualCompetitionRegistration();
                    ClubCrisisRules.ApplyDailyEffects(State);
                }

                // Scheduled historical events may fire mid-day after same-day spread.
                if (dayAfter > dayBefore || HasDueScheduledScriptedEvent())
                {
                    ProcessScriptedPulse(result, scheduledOnly: true);
                    if (result.scriptedEventPending || result.scriptedEventShouldPause ||
                        result.eventToastShouldPause)
                        break;
                }

                if (ProcessCompetitionsStartedInRange(before, State.clockHour))
                {
                    if (State.competitionRun?.awaitingPlayerMatch == true)
                        result.playerMatchRequired = true;
                    else
                        result.playerCompetitionResolved = true;
                }
                ExpireOffers();

                int ticksBefore = before / tick;
                int ticksAfter = State.clockHour / tick;
                for (int t = ticksBefore; t < ticksAfter && !State.gameOver; t++)
                {
                    ProcessWeeklyTrainingSideEffects();
                    ProcessGradualCompetitionRegistration();
                    ProcessAiMarket(result);
                    ProcessScriptedPulse(result);
                    if (result.scriptedEventPending || result.scriptedEventShouldPause ||
                        result.eventToastShouldPause)
                        break;
                }

                int rankingPeriod = CareerCalendar.ResolveRankingUpdateHours(_balance);
                int rankWeeksBefore = before / rankingPeriod;
                int rankWeeksAfter = State.clockHour / rankingPeriod;
                if (rankWeeksAfter > rankWeeksBefore)
                    RefreshRankings();

                if (result.scriptedEventPending || result.scriptedEventShouldPause ||
                    result.eventToastShouldPause)
                    break;

                if (afterDt.Year > beforeDt.Year || afterDt.Month > beforeDt.Month)
                {
                    CloseMonth();
                    result.monthClosed = true;
                }

                if (afterDt.Year > beforeDt.Year)
                {
                    foreach (PlayerCareerState player in State.players)
                        player.age++;
                }

                remaining -= step;
            }

            result.hoursAdvanced = State.clockHour - startedHour;
            result.newTransferOfferForPlayer = CountPendingOffersForPlayer() > pendingOffersBefore;
            result.personalTermsDue = HasPersonalTermsDueForPlayer();
            if (CountActivePlayerBuyerDeals() < activePlayerDealsBefore)
                result.transferDealInterrupted = true;
            result.gameOver = State.gameOver;
            if (result.hoursAdvanced >= CareerCalendar.HoursPerDay)
            {
                AddEvent(CareerEventType.HoursAdvanced, State.playerClubId, null,
                    $"推進至 {CareerCalendar.FormatHeader(_balance, State)}");
            }

            return result;
        }

        /// <summary>Compatibility wrapper: advances one training-tick (default 168h).</summary>
        public CareerAdvanceResult AdvanceWeek() =>
            AdvanceHours(CareerCalendar.ResolveTrainingTickHours(_balance));

        public CareerAdvanceResult AdvanceDay() => AdvanceHours(CareerCalendar.HoursPerDay);

        public void AdvanceToYearEnd()
        {
            int year = State.year;
            while (!State.gameOver && State.year == year)
            {
                while (AwaitsOfficialPlayerMatch)
                    AutoPlayPendingOfficialMatch();
                AdvanceHours(CareerCalendar.HoursPerDay);
            }
        }

        private int CountPendingOffersForPlayer() =>
            State.offers.Count(x =>
                string.Equals(x.sellerClubId, State.playerClubId, StringComparison.OrdinalIgnoreCase) &&
                (x.status == TransferOfferStatus.Pending || x.status == TransferOfferStatus.Countered));

        private int CountActivePlayerBuyerDeals() =>
            State.offers.Count(x =>
                string.Equals(x.buyerClubId, State.playerClubId, StringComparison.OrdinalIgnoreCase) &&
                TransferNegotiationRules.IsVulnerableToOutbid(x.status));

        private bool HasPersonalTermsDueForPlayer() =>
            State.offers.Any(x =>
                string.Equals(x.buyerClubId, State.playerClubId, StringComparison.OrdinalIgnoreCase) &&
                (x.status == TransferOfferStatus.ClubAgreed ||
                 x.status == TransferOfferStatus.PersonalTerms));

        private int ClubWorldRank(string clubId)
        {
            RankingEntry entry = State.rankings.FirstOrDefault(x =>
                string.Equals(x.clubId, clubId, StringComparison.OrdinalIgnoreCase));
            return entry?.rank ?? 50;
        }

        private double BuildPersonalTermsScore(TransferOffer offer, PlayerCareerState player)
        {
            int asked = offer.askedSalary > 0
                ? offer.askedSalary
                : MarketRules.AskingSalary(offer.fee, player.age, player.fame);
            ClubState buyer = State.clubs.FirstOrDefault(x =>
                string.Equals(x.clubId, offer.buyerClubId, StringComparison.OrdinalIgnoreCase));
            int prestige = buyer?.exposure ?? buyer?.fame ?? 50;
            return TransferNegotiationRules.PersonalTermsAcceptanceScore(
                offer.offeredSalary,
                asked,
                offer.signingBonus,
                offer.contractMonths > 0
                    ? offer.contractMonths
                    : TransferNegotiationRules.DefaultContractMonths,
                ClubWorldRank(offer.buyerClubId),
                ClubWorldRank(offer.sellerClubId),
                player.loyalty,
                player.transferRequested,
                prestige,
                ClubAveragePower(offer.buyerClubId),
                player.power);
        }

        private void OutbidVulnerableDealsForPlayer(
            string playerId, string rivalOfferId, int rivalFee, bool releaseClausePath)
        {
            foreach (TransferOffer existing in State.offers.Where(x =>
                         string.Equals(x.playerId, playerId, StringComparison.OrdinalIgnoreCase) &&
                         TransferNegotiationRules.IsVulnerableToOutbid(x.status) &&
                         !string.Equals(x.id, rivalOfferId, StringComparison.OrdinalIgnoreCase)).ToArray())
            {
                if (!TransferNegotiationRules.CanOutbid(rivalFee, existing.fee, releaseClausePath))
                    continue;
                TransferNegotiationRules.MarkOutbid(existing, rivalOfferId);
                AddEvent(CareerEventType.ContractChanged, existing.buyerClubId, playerId,
                    "進行中的轉會談判被更高報價或違約金打斷");
            }
        }

        private int NextBoundaryDelta()
        {
            int tick = CareerCalendar.ResolveTrainingTickHours(_balance);
            int toTick = tick - State.clockHour % tick;
            if (toTick <= 0) toTick = tick;

            DateTime now = CareerCalendar.ToDateTime(_balance, State.clockHour);
            DateTime monthEnd = new DateTime(now.Year, now.Month,
                DateTime.DaysInMonth(now.Year, now.Month), 23, 0, 0, DateTimeKind.Unspecified);
            int toMonth = CareerCalendar.FromDateTime(_balance, monthEnd) - State.clockHour + 1;
            if (toMonth <= 0) toMonth = int.MaxValue;

            int toCompetition = int.MaxValue;
            foreach (CompetitionDefinition competition in _competitions.Values)
            {
                if (CareerCalendar.IsCancelled(competition)) continue;
                if (competition.startHour > State.clockHour)
                    toCompetition = Math.Min(toCompetition, competition.startHour - State.clockHour);
            }

            int toTravel = int.MaxValue;
            foreach (CompetitionEntry entry in State.competitionEntries)
            {
                if (entry == null || !entry.registered || entry.withdrawn || entry.travelCostsPaid)
                    continue;
                if (entry.travelDepartHour > State.clockHour)
                    toTravel = Math.Min(toTravel, entry.travelDepartHour - State.clockHour);
            }

            int toScripted = HoursUntilNextScheduledScriptedEvent();

            return Math.Max(1, Math.Min(toTick, Math.Min(toMonth, Math.Min(toCompetition, Math.Min(toTravel, toScripted)))));
        }

        private int ResolveScriptedFireHour(CareerScriptedEventDef def) =>
            ScriptedEventScheduleRules.ResolveFireHour(def, _scriptedFireHours);

        private bool HasDueScheduledScriptedEvent()
        {
            if (_scriptedEvents.Count == 0 || !State.TryGetClub(State.playerClubId, out ClubState club))
                return false;
            return CareerScriptedRules.PickDueScheduledEvent(
                       _scriptedEvents.Values, State, club, _scriptedFireHours) !=
                   null;
        }

        private int HoursUntilNextScheduledScriptedEvent()
        {
            int best = int.MaxValue;
            foreach (CareerScriptedEventDef def in _scriptedEvents.Values)
            {
                int fireHour = ResolveScriptedFireHour(def);
                if (fireHour <= State.clockHour)
                    continue;
                if (CareerFlagRules.HasFlag(
                        State.flags,
                        ScriptedEventTierRules.FiredFlagId(def.id),
                        CareerFlagRules.GlobalScope,
                        State.clockHour))
                    continue;
                if (State.pendingEvents != null &&
                    State.pendingEvents.Any(x =>
                        string.Equals(x.eventId, def.id, StringComparison.OrdinalIgnoreCase)))
                    continue;
                best = Math.Min(best, fireHour - State.clockHour);
            }

            return best;
        }

        private void SyncYearFromClock()
        {
            State.year = CareerCalendar.ToDateTime(_balance, State).Year;
        }

        private void ProcessTrainingHours(int hours)
        {
            if (hours <= 0)
                return;

            // Energy multipliers are club-wide; evaluate once per club instead of per player-hour.
            var energyMultipliers = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            foreach (PlayerCareerState player in State.players)
            {
                if (player == null ||
                    string.IsNullOrWhiteSpace(player.clubId) ||
                    !State.TryGetClub(player.clubId, out ClubState club) ||
                    IsClubRosterInFlight(player.clubId, State.clockHour))
                    continue;

                if (!_playerDefinitions.TryGetValue(player.playerId, out CareerPlayerDefinition definition) ||
                    definition == null)
                    continue;

                _clubDefinitions.TryGetValue(club.clubId, out CareerClubDefinition clubDef);
                FacilityState facility = club.facilities?.FirstOrDefault(x => x.type == FacilityType.Training);
                int facilityLevel = facility == null || facility.disabled
                    ? 1
                    : Math.Max(1, facility.level);

                PlayerDefinition match = null;
                _interactiveMatchContent?.Players.TryGetValue(player.playerId, out match);
                DevelopmentRules.EnsureProgressLists(
                    player,
                    definition.role,
                    match?.attributes,
                    match?.roleFitness,
                    match?.mapFitness,
                    match?.dust2Grade);

                CareerTrainingPlan plan = TrainingPlanRules.Parse(player.trainingFocus);
                double weeklyPoints = DevelopmentRules.WeeklyTrainingPoints(
                    player, definition, 70, facilityLevel, false);
                weeklyPoints *= ClubEstateSatisfactionRules.GetTrainingEfficiencyMultiplier(
                    State, club, _balance, clubDef, State.players.Where(p => p.clubId == club.clubId));
                weeklyPoints *= ClubCoachStaffRules.GetStaffTrainingMultiplier(State, club.clubId);

                DevelopmentRules.ApplyHourlyTraining(
                    player, definition, plan, weeklyPoints, hours, _balance);

                if (!energyMultipliers.TryGetValue(club.clubId, out double energyMultiplier))
                {
                    energyMultiplier = ClubEstateSatisfactionRules.GetEnergyRecoveryMultiplier(
                                           State, club, _balance, clubDef,
                                           State.players.Where(p => p.clubId == club.clubId)) *
                                       TravelEarlySiteEnergyMultiplier(club.clubId);
                    energyMultipliers[club.clubId] = energyMultiplier;
                }

                player.energy = EnergyRecoveryRules.Recover(player.energy, hours, energyMultiplier);

                player.hoursWithoutMatch += hours;
                player.power = Math.Min(player.power, DevelopmentRules.AbilityCeiling(player));
            }
        }

        private void ProcessWeeklyTrainingSideEffects()
        {
            bool anyRust = false;
            foreach (PlayerCareerState player in State.players)
            {
                if (player == null ||
                    !_playerDefinitions.TryGetValue(player.playerId, out CareerPlayerDefinition definition) ||
                    definition == null)
                    continue;

                if (player.hoursWithoutMatch >= 8 * CareerCalendar.HoursPerWeek)
                    player.form = Math.Max(30, player.form - 2);

                if (DevelopmentRules.ApplyInactivityRust(player, definition, _balance))
                    anyRust = true;
            }

            if (anyRust)
            {
                // Keep as status-only; rust is not news-feed material.
            }
        }

        private bool ProcessCompetitionsStartedInRange(int exclusiveStartHour, int inclusiveEndHour)
        {
            bool playerInvolved = false;
            foreach (CompetitionDefinition competition in _competitions.Values
                         .Where(x => !CareerCalendar.IsCancelled(x) &&
                                     x.startHour > exclusiveStartHour &&
                                     x.startHour <= inclusiveEndHour)
                         .OrderBy(x => x.startHour))
            {
                CompetitionWithdrawRules.ConsumeMatchingBans(State.competitionSignupBans, competition);
                if (StockholmMajorRules.UsesRmrQualification(competition))
                    EnsureRmrQualifiedMajorField(competition);
                else
                    EnsureCompetitionFieldFull(competition, allowSitOut: false);
                List<string> entrants = BuildEntrantList(competition);
                if (entrants.Count < 2) continue;
                bool playerEntered = entrants.Contains(State.playerClubId, StringComparer.OrdinalIgnoreCase);
                if (StockholmMajorRules.IsStockholmMajorStructure(competition))
                {
                    BeginStockholmMajor(competition, entrants);
                    if (playerEntered)
                        playerInvolved = true;
                }
                else if (playerEntered)
                {
                    BeginInteractiveCompetition(competition, entrants);
                    playerInvolved = true;
                }
                else
                    RunCompetition(competition, entrants);
            }

            return playerInvolved;
        }

        public CompetitionBracketSnapshot GetCompetitionBracketSnapshot(string competitionId)
        {
            if (!_competitions.TryGetValue(competitionId, out CompetitionDefinition competition))
                return new CompetitionBracketSnapshot();
            return CompetitionRegistrationRules.BuildSnapshot(
                State,
                _balance,
                competition,
                State.playerClubId,
                ResolveClubDisplayName);
        }

        private string ResolveClubDisplayName(string clubId)
        {
            if (string.IsNullOrEmpty(clubId))
                return "待定";
            return _clubDefinitions.TryGetValue(clubId, out CareerClubDefinition club)
                ? club.displayName
                : clubId;
        }

        private List<string> BuildEntrantList(CompetitionDefinition competition)
        {
            int field = CompetitionRegistrationRules.EffectiveFieldSize(competition);
            List<string> entrants = CompetitionRegistrationRules.RegisteredClubIds(State, competition.id)
                .ToList();

            if (entrants.Count < field)
                EnsureCompetitionFieldFull(competition, allowSitOut: false);

            entrants = CompetitionRegistrationRules.RegisteredClubIds(State, competition.id).ToList();
            return OrderEntrantsByRank(entrants).Take(field).ToList();
        }

        private List<string> OrderEntrantsByRank(List<string> entrants) =>
            entrants
                .OrderBy(id =>
                {
                    RankingEntry rank = State.rankings.FirstOrDefault(x =>
                        string.Equals(x.clubId, id, StringComparison.OrdinalIgnoreCase));
                    return rank?.rank ?? 999;
                })
                .ThenBy(id => id, StringComparer.OrdinalIgnoreCase)
                .ToList();

        private IEnumerable<(string clubId, bool sameRegion, double desire)> EnumerateSignupCandidates(
            CompetitionDefinition competition)
        {
            int softRank = CompetitionRegistrationRules.SoftMaximumWorldRank(competition);
            var ranked = State.rankings
                .OrderBy(x => x.rank)
                .Select(x =>
                {
                    _clubDefinitions.TryGetValue(x.clubId, out CareerClubDefinition def);
                    bool sameRegion = string.IsNullOrEmpty(competition.region) ||
                                      (def != null &&
                                       def.region.Equals(competition.region, StringComparison.OrdinalIgnoreCase));
                    bool rankOk = softRank >= int.MaxValue || x.rank <= softRank;
                    return new { ranked = x, def, sameRegion, rankOk };
                })
                .Where(x => x.rankOk && x.def != null)
                .Where(x => !string.Equals(x.ranked.clubId, State.playerClubId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // 同賽區優先，不足再開放跨區補滿。玩家俱樂部不進 AI 自動補人，須自行報名。
            foreach (var item in ranked.Where(x => x.sameRegion).Concat(ranked.Where(x => !x.sameRegion)))
            {
                int ambition = item.def.ambition;
                ClubRosterCondition(item.ranked.clubId, out double energy, out double health, out double form);
                int overlaps = CountOverlappingCommitments(item.ranked.clubId, competition.id);
                double desire = CompetitionRegistrationRules.AiSignupDesire(
                    competition,
                    item.ranked.rank,
                    ambition,
                    energy,
                    health,
                    form,
                    overlaps,
                    item.sameRegion);
                if (CompetitionTravelRules.RequiresTravel(competition) &&
                    State.TryGetClub(item.ranked.clubId, out ClubState rankedClub))
                {
                    CompetitionTravelEstimate travel = BuildTravelEstimateForClub(
                        competition, item.ranked.clubId,
                        rankedClub.cash,
                        preferComfort: false);
                    if (travel.clubOutOfPocket >= 50000)
                        desire *= 0.35d;
                    else if (travel.clubOutOfPocket >= 25000)
                        desire *= 0.55d;
                    else if (travel.clubOutOfPocket >= 10000)
                        desire *= 0.75d;
                    if (!item.sameRegion && travel.earlyArrivalDays >= 4)
                        desire *= 0.82d;
                }
                yield return (item.ranked.clubId, item.sameRegion, desire);
            }
        }

        private void ClubRosterCondition(
            string clubId, out double averageEnergy, out double averageHealth, out double averageForm)
        {
            PlayerCareerState[] roster = State.players
                .Where(x => string.Equals(x.clubId, clubId, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (roster.Length == 0)
            {
                averageEnergy = 70d;
                averageHealth = 80d;
                averageForm = 50d;
                return;
            }

            averageEnergy = roster.Average(x => x.energy);
            averageHealth = roster.Average(x => x.health);
            averageForm = roster.Average(x => x.form);
        }

        private int CountOverlappingCommitments(string clubId, string excludingCompetitionId)
        {
            if (!_competitions.TryGetValue(excludingCompetitionId, out CompetitionDefinition candidateDef))
                return 0;
            CompetitionCommitment candidate = ToCommitment(candidateDef, false);
            List<CompetitionCommitment> existing = BuildClubCommitments(clubId)
                .Where(x => !string.Equals(x.competitionId, excludingCompetitionId, StringComparison.OrdinalIgnoreCase))
                .ToList();
            return CompetitionScheduleRules.CountOverlappingWith(candidate, existing);
        }

        private bool ClubCanAcceptSchedule(string clubId, CompetitionDefinition competition)
        {
            if (competition == null)
                return false;
            CompetitionCommitment candidate = ToCommitment(
                competition, StockholmMajorRules.UsesRmrQualification(competition));
            List<CompetitionCommitment> existing = BuildClubCommitments(clubId);
            return CompetitionScheduleRules.CanAiAccept(candidate, existing);
        }

        private bool IsClubRegistered(string competitionId, string clubId) =>
            State.competitionEntries.Any(x =>
                string.Equals(x.competitionId, competitionId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.clubId, clubId, StringComparison.OrdinalIgnoreCase) &&
                x.registered && !x.withdrawn);

        private void RegisterClubForCompetition(string competitionId, string clubId)
        {
            if (IsClubRegistered(competitionId, clubId))
                return;
            if (!_competitions.TryGetValue(competitionId, out CompetitionDefinition competition))
                return;
            if (CompetitionWithdrawRules.HasActiveSignupBan(State.competitionSignupBans, clubId, competition))
                return;
            if (!State.TryGetClub(clubId, out ClubState club))
                return;
            if (!ClubCanAcceptSchedule(clubId, competition))
                return;

            if (CompetitionTravelRules.RequiresTravel(competition))
            {
                CompetitionTravelEstimate travel = BuildTravelEstimateForClub(
                    competition, clubId, club.cash, preferComfort: false);
                if (club.cash < competition.entryFee + travel.clubOutOfPocket)
                    return;
            }
            else if (club.cash < competition.entryFee)
            {
                return;
            }

            CompetitionEntry existing = State.competitionEntries.FirstOrDefault(x =>
                string.Equals(x.competitionId, competitionId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.clubId, clubId, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.registered = true;
                existing.withdrawn = false;
                existing.registeredHour = State.clockHour;
                existing.completed = false;
                existing.placement = 0;
                EnsureCompetitionTravelPlan(clubId, competition, existing);
                return;
            }

            var entry = new CompetitionEntry
            {
                competitionId = competitionId,
                clubId = clubId,
                registered = true,
                registeredHour = State.clockHour
            };
            State.competitionEntries.Add(entry);
            EnsureCompetitionTravelPlan(clubId, competition, entry);
        }

        private void ApplyCompetitionWithdrawal(
            CompetitionDefinition competition,
            CompetitionEntry entry,
            string clubId,
            CompetitionWithdrawReason reason,
            CompetitionWithdrawEvaluation evaluation)
        {
            entry.registered = false;
            entry.withdrawn = true;
            entry.withdrawnHour = State.clockHour;
            entry.withdrawReason = reason;

            if (evaluation.fee > 0 && State.TryGetClub(clubId, out ClubState club) && club != null)
            {
                club.cash -= evaluation.fee;
                AddLedger(club.clubId, "withdraw_fee", -evaluation.fee, competition.id,
                    evaluation.excusedFromBan ? "賽事替補協調費" : "臨時退賽替補費用");
            }

            if (evaluation.appliesBan)
            {
                if (State.competitionSignupBans == null)
                    State.competitionSignupBans = new List<CompetitionSignupBan>();
                State.competitionSignupBans.Add(new CompetitionSignupBan
                {
                    clubId = clubId,
                    organizerId = CompetitionWithdrawRules.ResolveOrganizerId(competition),
                    tier = competition.tier,
                    sourceCompetitionId = competition.id,
                    issuedHour = State.clockHour,
                    consumed = false
                });
            }

            // Withdrawal is reflected in competition UI; skip news-feed spam.
            // 退賽後盡量補替補隊，但極少數情況仍可能補不滿。
            if (State.clockHour < competition.startHour)
                EnsureCompetitionFieldFull(competition, allowSitOut: false);
        }

        /// <summary>
        /// 依意願為賽事補人。allowSitOut 時 AI 可休賽；強制滿額時忽略休賽，必要時跨區補滿。
        /// </summary>
        private void EnsureCompetitionFieldFull(CompetitionDefinition competition, bool allowSitOut)
        {
            if (StockholmMajorRules.UsesRmrQualification(competition))
            {
                EnsureRmrQualifiedMajorField(competition);
                return;
            }

            int field = CompetitionRegistrationRules.EffectiveFieldSize(competition);
            int registered = CompetitionRegistrationRules.RegisteredClubIds(State, competition.id).Count;
            if (registered >= field)
                return;

            int need = field - registered;
            var random = new SeededRandom(
                State.seed + State.clockHour * 31337 + competition.id.GetHashCode() + (allowSitOut ? 1 : 17));

            List<(string clubId, bool sameRegion, double desire)> candidates =
                EnumerateSignupCandidates(competition)
                    .Where(x => !IsClubRegistered(competition.id, x.clubId))
                    .Where(x => !CompetitionWithdrawRules.HasActiveSignupBan(
                        State.competitionSignupBans, x.clubId, competition))
                    .OrderByDescending(x => x.desire)
                    .ThenBy(x => x.sameRegion ? 0 : 1)
                    .ToList();

            foreach ((string clubId, bool _, double desire) in candidates)
            {
                if (need <= 0)
                    break;
                if (allowSitOut && CompetitionRegistrationRules.AiChoosesRest(desire, random))
                    continue;
                RegisterClubForCompetition(competition.id, clubId);
                need--;
            }

            if (need <= 0)
                return;

            // 極少數：強制補人也找不到人時留下空籤，對戰時以不戰而勝處理。
            foreach ((string clubId, bool _, double _) in candidates)
            {
                if (need <= 0)
                    break;
                if (IsClubRegistered(competition.id, clubId))
                    continue;
                RegisterClubForCompetition(competition.id, clubId);
                need--;
            }
        }

        private void ProcessGradualCompetitionRegistration()
        {
            foreach (CompetitionDefinition competition in _competitions.Values
                         .Where(x => !CareerCalendar.IsCancelled(x)))
            {
                if (!CompetitionRegistrationRules.IsOpenForAiRegistration(
                        competition, _balance, State.clockHour, State.flags))
                    continue;

                if (StockholmMajorRules.UsesRmrQualification(competition))
                {
                    // RMR majors lock seats near kickoff / force-fill window, not gradual world-rank drip.
                    if (CompetitionRegistrationRules.IsForceFillWindow(competition, _balance, State.clockHour) ||
                        State.clockHour >= competition.startHour - CareerCalendar.HoursPerDay * 3)
                        EnsureRmrQualifiedMajorField(competition);
                    continue;
                }

                int field = CompetitionRegistrationRules.EffectiveFieldSize(competition);
                int registered = CompetitionRegistrationRules.RegisteredClubIds(State, competition.id).Count;
                if (registered >= field)
                    continue;

                bool forceFill = CompetitionRegistrationRules.IsForceFillWindow(
                    competition, _balance, State.clockHour);
                if (forceFill)
                {
                    EnsureCompetitionFieldFull(competition, allowSitOut: false);
                    continue;
                }

                int target = CompetitionRegistrationRules.TargetRegisteredCount(
                    competition, _balance, State.clockHour);
                int toAdd = Math.Max(0, target - registered);
                if (toAdd <= 0)
                    continue;

                var random = new SeededRandom(
                    State.seed + State.clockHour * 31337 + competition.id.GetHashCode());
                toAdd = Math.Min(toAdd, 1 + (random.Next01() > 0.55f ? 1 : 0));
                toAdd = Math.Min(toAdd, field - registered);

                List<(string clubId, bool sameRegion, double desire)> candidates =
                    EnumerateSignupCandidates(competition)
                        .Where(x => !IsClubRegistered(competition.id, x.clubId))
                        .Where(x => !CompetitionWithdrawRules.HasActiveSignupBan(
                            State.competitionSignupBans, x.clubId, competition))
                        .OrderByDescending(x => x.desire)
                        .ThenBy(x => x.sameRegion ? 0 : 1)
                        .ToList();

                foreach ((string clubId, bool _, double desire) in candidates)
                {
                    if (toAdd <= 0)
                        break;
                    if (CompetitionRegistrationRules.AiChoosesRest(desire, random))
                        continue;
                    RegisterClubForCompetition(competition.id, clubId);
                    toAdd--;
                }

                // AI 偶爾因狀態不佳提前退賽，讓報名表持續流動。
                MaybeProcessAiEarlyWithdrawals(competition, random);
            }
        }

        private void MaybeProcessAiEarlyWithdrawals(CompetitionDefinition competition, SeededRandom random)
        {
            if (competition == null || random == null)
                return;
            if (CompetitionWithdrawRules.IsLateWithdraw(competition, State.clockHour))
                return;
            if (random.Next01() > 0.08f)
                return;

            List<CompetitionEntry> registered = State.competitionEntries
                .Where(x => string.Equals(x.competitionId, competition.id, StringComparison.OrdinalIgnoreCase) &&
                            x.registered && !x.withdrawn && !x.completed &&
                            !string.Equals(x.clubId, State.playerClubId, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (registered.Count == 0)
                return;

            CompetitionEntry pick = registered[random.Range(0, registered.Count)];
            ClubRosterCondition(pick.clubId, out double energy, out double health, out _);
            CompetitionWithdrawReason reason = CompetitionWithdrawReason.Voluntary;
            if (health < 45d && random.Next01() < 0.45f)
                reason = CompetitionWithdrawReason.PlayerIllness;
            else if (energy > 55d && random.Next01() > 0.35f)
                return;

            CompetitionWithdrawEvaluation evaluation =
                CompetitionWithdrawRules.Evaluate(competition, pick, State.clockHour, reason);
            if (!evaluation.canWithdraw)
                return;
            ApplyCompetitionWithdrawal(competition, pick, pick.clubId, reason, evaluation);
        }

        public bool AwaitsOfficialPlayerMatch =>
            State.competitionRun != null && State.competitionRun.awaitingPlayerMatch;

        public bool HasScheduledOfficialPlayerMatch =>
            State.competitionRun != null &&
            !State.competitionRun.awaitingPlayerMatch &&
            State.competitionRun.playerMatchDueHour > State.clockHour &&
            !string.IsNullOrEmpty(State.competitionRun.pendingOpponentClubId);

        public int HoursUntilScheduledOfficialPlayerMatch =>
            HasScheduledOfficialPlayerMatch
                ? Math.Max(0, State.competitionRun.playerMatchDueHour - State.clockHour)
                : 0;

        public bool TryBuildOfficialMatchLaunch(
            ContentDatabase matchContent, out CareerMatchLaunchContext.LaunchRequest launch)
        {
            launch = null;
            if (!AwaitsOfficialPlayerMatch ||
                !_competitions.TryGetValue(State.competitionRun.competitionId, out CompetitionDefinition competition))
                return false;

            CareerCompetitionRunState run = State.competitionRun;
            CareerMapSeriesState series = GetOfficialMapSeries();
            if (!series.vetoComplete)
                return false;

            string opponent = run.pendingOpponentClubId;
            if (!CareerMatchTeams.TryResolveSimTeams(
                    matchContent, State.playerClubId, opponent,
                    out string playerSim, out string opponentSim))
                return false;

            run.pendingPlayerSimTeamId = playerSim;
            run.pendingOpponentSimTeamId = opponentSim;

            string seriesMapId = series.CurrentMapId;
            if (string.IsNullOrEmpty(seriesMapId))
                seriesMapId = "dust2";
            string combatMapId = MapVetoRules.ResolvePlayableMapId(seriesMapId, MapPoolIsPlayable);
            string displayName = ResolveMapDisplayName(seriesMapId);

            launch = new CareerMatchLaunchContext.LaunchRequest
            {
                playerCareerClubId = State.playerClubId,
                opponentCareerClubId = opponent,
                playerSimTeamId = playerSim,
                opponentSimTeamId = opponentSim,
                seed = run.pendingMatchSeed + series.nextMapIndex * 97,
                competitionId = competition.id,
                competitionDisplayName = competition.displayName,
                mapId = combatMapId,
                mapDisplayName = displayName,
                bracketStageLabel = FormatKnockoutStageLabel(run),
                bestOf = MapVetoRules.BestOf,
                seriesMapIndex = series.nextMapIndex,
                seriesScorePlayer = series.mapWinsPlayer,
                seriesScoreOpponent = series.mapWinsOpponent
            };
            return true;
        }

        public bool NeedsOfficialMapVeto =>
            AwaitsOfficialPlayerMatch &&
            (State.competitionRun.mapSeries == null || !State.competitionRun.mapSeries.vetoComplete);

        public CareerMapSeriesState GetOfficialMapSeries()
        {
            if (State.competitionRun == null)
                return null;
            if (State.competitionRun.mapSeries == null)
                State.competitionRun.mapSeries = new CareerMapSeriesState();
            MapVetoRules.EnsureLists(State.competitionRun.mapSeries);
            return State.competitionRun.mapSeries;
        }

        public void BeginOfficialMapVeto()
        {
            CareerCompetitionRunState run = State.competitionRun;
            if (run == null)
                return;
            CareerMapSeriesState series = GetOfficialMapSeries();
            MapVetoRules.BeginVeto(series, run.pendingMatchSeed, GetActiveOfficialPoolIds());
        }

        public bool IsTrainPoolRemoved() => MapPoolRules.IsTrainRemoved(State);

        public string[] GetActiveOfficialPoolIds() =>
            MapPoolRules.ResolveActivePoolIds(IsTrainPoolRemoved());

        public MapPoolEntry[] ResolveActiveOfficialMapPool(MapPoolEntry[] catalogEntries) =>
            MapPoolRules.ResolveActivePool(catalogEntries, IsTrainPoolRemoved());

        public bool TryApplyOfficialMapVetoChoice(string mapId, out string error)
        {
            error = null;
            CareerMapSeriesState series = GetOfficialMapSeries();
            if (series == null || !AwaitsOfficialPlayerMatch)
            {
                error = "no pending match";
                return false;
            }

            if (series.vetoComplete)
            {
                error = "veto complete";
                return false;
            }

            if (!MapVetoRules.IsPlayerTurn(series.vetoStep, series.playerIsVetoA))
            {
                error = "not player turn";
                return false;
            }

            return MapVetoRules.TryApplyChoice(series, mapId, out error);
        }

        public bool TryAdvanceOfficialMapVetoAi(out string chosenId, out string error)
        {
            chosenId = null;
            error = null;
            CareerMapSeriesState series = GetOfficialMapSeries();
            if (series == null || series.vetoComplete)
            {
                error = "veto inactive";
                return false;
            }

            if (series.vetoStep == (int)MapVetoStepKind.Decider)
                return MapVetoRules.TryResolveDecider(series, out error);

            if (MapVetoRules.IsPlayerTurn(series.vetoStep, series.playerIsVetoA))
            {
                error = "player turn";
                return false;
            }

            return MapVetoRules.TryApplyAiChoice(
                series, State.competitionRun.pendingMatchSeed, out chosenId, out error);
        }

        public bool OfficialSeriesNeedsNextMap =>
            State.competitionRun?.awaitingPlayerMatch == true &&
            State.competitionRun.mapSeries != null &&
            State.competitionRun.mapSeries.vetoComplete &&
            State.competitionRun.mapSeries.seriesActive &&
            !MapVetoRules.SeriesIsComplete(State.competitionRun.mapSeries);

        private static string FormatKnockoutStageLabel(CareerCompetitionRunState run)
        {
            if (run?.currentRoundClubs == null || run.currentRoundClubs.Count == 0)
                return string.Empty;
            return CompetitionRegistrationRules.StageLabelForTeamsInRound(run.currentRoundClubs.Count);
        }

        private bool MapPoolIsPlayable(string mapId) =>
            MapPoolRules.IsPlayable(mapId, IsTrainPoolRemoved());

        private static string ResolveMapDisplayName(string mapId) =>
            MapPoolRules.DisplayName(mapId);

        public bool TryApplyOfficialMatchResult(MatchResult result, ContentDatabase matchContent)
        {
            if (result == null || State.competitionRun == null)
                return false;
            bool pendingInteractive = State.competitionRun.awaitingPlayerMatch;
            if (!pendingInteractive &&
                !string.IsNullOrEmpty(State.competitionRun.pendingOpponentClubId) &&
                !string.IsNullOrEmpty(State.competitionRun.pendingPlayerSimTeamId))
                pendingInteractive = true;
            if (!pendingInteractive)
                return false;
            if (!_competitions.TryGetValue(State.competitionRun.competitionId, out CompetitionDefinition competition))
                return false;

            string playerSim = State.competitionRun.pendingPlayerSimTeamId;
            string opponentSim = State.competitionRun.pendingOpponentSimTeamId;
            if (string.IsNullOrEmpty(playerSim) || string.IsNullOrEmpty(opponentSim))
            {
                if (!CareerMatchTeams.TryResolveSimTeams(
                        matchContent, State.playerClubId, State.competitionRun.pendingOpponentClubId,
                        out playerSim, out opponentSim))
                    return false;
            }

            CareerMapSeriesState series = GetOfficialMapSeries();
            if (!series.vetoComplete)
                MapVetoRules.AutoCompleteVeto(
                    series, State.competitionRun.pendingMatchSeed, GetActiveOfficialPoolIds());

            CareerMatchSummary mapSummary = BuildSummaryFromMatchResult(
                result, playerSim, opponentSim, State.playerClubId,
                State.competitionRun.pendingOpponentClubId, competition.tier);

            bool playerWonMap = string.Equals(
                mapSummary.winnerClubId, State.playerClubId, StringComparison.OrdinalIgnoreCase);
            bool playerIsTeamA = PlayerSimTeamIsTeamA(result, playerSim, opponentSim);
            int playerRoundScore = playerIsTeamA ? result.teamAScore : result.teamBScore;
            int opponentRoundScore = playerIsTeamA ? result.teamBScore : result.teamAScore;
            series.mapScorePlayer.Add(playerRoundScore);
            series.mapScoreOpponent.Add(opponentRoundScore);
            if (playerWonMap)
                series.mapWinsPlayer++;
            else
                series.mapWinsOpponent++;

            if (!MapVetoRules.SeriesIsComplete(series))
            {
                series.nextMapIndex++;
                series.seriesActive = true;
                State.competitionRun.awaitingPlayerMatch = true;
                return false;
            }

            CareerMatchSummary seriesSummary = new CareerMatchSummary
            {
                winnerClubId = series.mapWinsPlayer >= MapVetoRules.WinsNeeded
                    ? State.playerClubId
                    : State.competitionRun.pendingOpponentClubId,
                loserClubId = series.mapWinsPlayer >= MapVetoRules.WinsNeeded
                    ? State.competitionRun.pendingOpponentClubId
                    : State.playerClubId,
                winnerScore = Math.Max(series.mapWinsPlayer, series.mapWinsOpponent),
                loserScore = Math.Min(series.mapWinsPlayer, series.mapWinsOpponent),
                winnerRating = mapSummary.winnerRating,
                loserRating = mapSummary.loserRating,
                exposure = mapSummary.exposure
            };

            ApplyMatch(seriesSummary);
            RecordBracketMatchFromCurrentPair(competition, seriesSummary);
            State.competitionRun.awaitingPlayerMatch = false;
            series.seriesActive = false;
            return ContinueAfterOfficialPlayerMatch(competition, seriesSummary);
        }

        public bool AutoPlayPendingOfficialMatch()
        {
            if (State.competitionRun == null || !State.competitionRun.awaitingPlayerMatch)
                return false;
            if (!_competitions.TryGetValue(State.competitionRun.competitionId, out CompetitionDefinition competition))
                return false;

            CareerMapSeriesState series = GetOfficialMapSeries();
            if (!series.vetoComplete)
                MapVetoRules.AutoCompleteVeto(
                    series, State.competitionRun.pendingMatchSeed, GetActiveOfficialPoolIds());

            string opponent = State.competitionRun.pendingOpponentClubId;
            int matchSeed = State.competitionRun.pendingMatchSeed;

            while (!MapVetoRules.SeriesIsComplete(series))
            {
                CareerMatchSummary mapSummary = _matches.Simulate(
                    State, _playerDefinitions, State.playerClubId, opponent, competition.tier,
                    matchSeed + series.nextMapIndex * 97);
                bool playerWon = string.Equals(
                    mapSummary.winnerClubId, State.playerClubId, StringComparison.OrdinalIgnoreCase);
                series.mapScorePlayer.Add(playerWon ? mapSummary.winnerScore : mapSummary.loserScore);
                series.mapScoreOpponent.Add(playerWon ? mapSummary.loserScore : mapSummary.winnerScore);
                if (playerWon)
                    series.mapWinsPlayer++;
                else
                    series.mapWinsOpponent++;
                if (MapVetoRules.SeriesIsComplete(series))
                    break;
                series.nextMapIndex++;
            }

            CareerMatchSummary seriesSummary = new CareerMatchSummary
            {
                winnerClubId = series.mapWinsPlayer >= MapVetoRules.WinsNeeded
                    ? State.playerClubId
                    : opponent,
                loserClubId = series.mapWinsPlayer >= MapVetoRules.WinsNeeded
                    ? opponent
                    : State.playerClubId,
                winnerScore = Math.Max(series.mapWinsPlayer, series.mapWinsOpponent),
                loserScore = Math.Min(series.mapWinsPlayer, series.mapWinsOpponent),
                winnerRating = 1.1f,
                loserRating = 0.9f,
                exposure = 20 + (int)competition.tier * 14
            };

            ApplyMatch(seriesSummary);
            RecordBracketMatchFromCurrentPair(competition, seriesSummary);
            State.competitionRun.awaitingPlayerMatch = false;
            series.seriesActive = false;
            return ContinueAfterOfficialPlayerMatch(competition, seriesSummary);
        }

        private bool ContinueAfterOfficialPlayerMatch(CompetitionDefinition competition, CareerMatchSummary summary)
        {
            CareerCompetitionRunState run = State.competitionRun;
            if (run != null &&
                string.Equals(run.structureId, StockholmMajorRules.StructureId, StringComparison.OrdinalIgnoreCase))
                return ContinueAfterOfficialMajorMatch(competition, summary);

            return ContinueAfterOfficialPlayerMatchCore(competition, summary);
        }

        private void TryPromoteScheduledPlayerMatch(CareerAdvanceResult result)
        {
            CareerCompetitionRunState run = State.competitionRun;
            if (run == null || run.awaitingPlayerMatch)
                return;
            if (string.IsNullOrEmpty(run.pendingOpponentClubId))
                return;
            if (run.playerMatchDueHour <= 0 || State.clockHour < run.playerMatchDueHour)
                return;

            run.awaitingPlayerMatch = true;
            run.playerMatchDueHour = 0;
            result.playerMatchRequired = true;
        }

        private void ScheduleInteractivePlayerMatch(CareerCompetitionRunState run)
        {
            if (run.mapSeries == null)
                run.mapSeries = new CareerMapSeriesState();
            run.mapSeries.ResetVeto();

            if (run.playerInteractiveMatchesResolved > 0)
            {
                run.playerMatchDueHour = State.clockHour + CareerCalendar.HoursPerDay;
                run.awaitingPlayerMatch = false;
            }
            else
            {
                run.playerMatchDueHour = 0;
                run.awaitingPlayerMatch = true;
            }
        }

        public static CareerMatchSummary BuildSummaryFromMatchResult(
            MatchResult result,
            string playerSimTeamId,
            string opponentSimTeamId,
            string playerCareerClubId,
            string opponentCareerClubId,
            CompetitionTier tier)
        {
            bool playerWon = string.Equals(result.winnerTeamId, playerSimTeamId, StringComparison.OrdinalIgnoreCase);
            string winnerCareer = playerWon ? playerCareerClubId : opponentCareerClubId;
            string loserCareer = playerWon ? opponentCareerClubId : playerCareerClubId;
            bool playerIsTeamA = PlayerSimTeamIsTeamA(result, playerSimTeamId, opponentSimTeamId);
            int playerScore = playerIsTeamA ? result.teamAScore : result.teamBScore;
            int opponentScore = playerIsTeamA ? result.teamBScore : result.teamAScore;
            int winnerScore = playerWon ? playerScore : opponentScore;
            int loserScore = playerWon ? opponentScore : playerScore;

            PlayerMatchSummary[] winnerPlayers = result.playerSummaries?
                .Where(x => string.Equals(x.teamId, result.winnerTeamId, StringComparison.OrdinalIgnoreCase)).ToArray()
                ?? Array.Empty<PlayerMatchSummary>();
            PlayerMatchSummary[] loserPlayers = result.playerSummaries?
                .Where(x => !string.Equals(x.teamId, result.winnerTeamId, StringComparison.OrdinalIgnoreCase)).ToArray()
                ?? Array.Empty<PlayerMatchSummary>();

            return new CareerMatchSummary
            {
                winnerClubId = winnerCareer,
                loserClubId = loserCareer,
                winnerScore = winnerScore,
                loserScore = loserScore,
                winnerRating = winnerPlayers.Length == 0 ? 1.1f : winnerPlayers.Average(x => x.rating),
                loserRating = loserPlayers.Length == 0 ? .9f : loserPlayers.Average(x => x.rating),
                exposure = 20 + (int)tier * 14
            };
        }

        private static bool PlayerSimTeamIsTeamA(
            MatchResult result, string playerSimTeamId, string opponentSimTeamId)
        {
            if (result?.playerSummaries != null && result.playerSummaries.Count > 0)
            {
                if (result.playerSummaries.Any(x =>
                        string.Equals(x.teamId, playerSimTeamId, StringComparison.OrdinalIgnoreCase)))
                    return true;
                if (result.playerSummaries.Any(x =>
                        string.Equals(x.teamId, opponentSimTeamId, StringComparison.OrdinalIgnoreCase)))
                    return false;
            }
            return true;
        }

        private void BeginInteractiveCompetition(CompetitionDefinition competition, List<string> entrants)
        {
            if (competition == null)
                return;

            if (State.competitionRun != null &&
                !string.IsNullOrWhiteSpace(State.competitionRun.competitionId) &&
                !string.Equals(
                    State.competitionRun.competitionId,
                    competition.id,
                    StringComparison.OrdinalIgnoreCase))
            {
                QueueInteractiveCompetition(competition.id);
                return;
            }

            List<string> bracket = BuildKnockoutBracket(entrants, State.playerClubId);
            EnsureBracketArchive(competition.id, bracket);
            State.competitionRun = new CareerCompetitionRunState
            {
                competitionId = competition.id,
                currentRoundClubs = bracket,
                roundWinners = new List<string>(),
                pairIndex = 0,
                roundIndex = 0
            };
            AdvanceInteractiveCompetition(competition);
        }

        private void QueueInteractiveCompetition(string competitionId)
        {
            if (string.IsNullOrWhiteSpace(competitionId))
                return;
            State.queuedInteractiveCompetitionIds =
                State.queuedInteractiveCompetitionIds ?? new List<string>();
            if (State.queuedInteractiveCompetitionIds.Any(x =>
                    string.Equals(x, competitionId, StringComparison.OrdinalIgnoreCase)))
                return;
            State.queuedInteractiveCompetitionIds.Add(competitionId);
        }

        private void TryStartNextQueuedInteractiveCompetition()
        {
            if (State.competitionRun != null)
                return;
            State.queuedInteractiveCompetitionIds =
                State.queuedInteractiveCompetitionIds ?? new List<string>();
            while (State.queuedInteractiveCompetitionIds.Count > 0)
            {
                string nextId = State.queuedInteractiveCompetitionIds[0];
                State.queuedInteractiveCompetitionIds.RemoveAt(0);
                if (!_competitions.TryGetValue(nextId, out CompetitionDefinition competition) ||
                    CareerCalendar.IsCancelled(competition))
                    continue;
                if (!IsClubRegistered(nextId, State.playerClubId))
                    continue;

                List<string> entrants = BuildEntrantList(competition);
                if (entrants.Count < 2)
                    continue;
                if (!entrants.Contains(State.playerClubId, StringComparer.OrdinalIgnoreCase))
                    continue;

                if (StockholmMajorRules.IsStockholmMajorStructure(competition))
                    BeginStockholmMajor(competition, entrants);
                else
                    BeginInteractiveCompetition(competition, entrants);
                return;
            }
        }

        private void EnsureBracketArchive(string competitionId, List<string> seedOrder)
        {
            if (State.competitionBrackets == null)
                State.competitionBrackets = new List<CompetitionBracketArchive>();

            CompetitionBracketArchive archive = State.competitionBrackets.FirstOrDefault(x =>
                string.Equals(x.competitionId, competitionId, StringComparison.OrdinalIgnoreCase));
            if (archive == null)
            {
                archive = new CompetitionBracketArchive { competitionId = competitionId };
                State.competitionBrackets.Add(archive);
            }

            archive.seedOrder = new List<string>(seedOrder ?? new List<string>());
            archive.matches = new List<CompetitionBracketMatchRecord>();
        }

        private void RecordBracketMatch(
            CompetitionDefinition competition,
            string clubA,
            string clubB,
            CareerMatchSummary summary)
        {
            if (competition == null || summary == null || State.competitionRun == null)
                return;

            CareerCompetitionRunState run = State.competitionRun;
            if (State.competitionBrackets == null)
                State.competitionBrackets = new List<CompetitionBracketArchive>();

            CompetitionBracketArchive archive = State.competitionBrackets.FirstOrDefault(x =>
                string.Equals(x.competitionId, competition.id, StringComparison.OrdinalIgnoreCase));
            if (archive == null)
            {
                archive = new CompetitionBracketArchive
                {
                    competitionId = competition.id,
                    seedOrder = new List<string>(run.currentRoundClubs ?? new List<string>())
                };
                State.competitionBrackets.Add(archive);
            }

            int scoreA = string.Equals(summary.winnerClubId, clubA, StringComparison.OrdinalIgnoreCase)
                ? summary.winnerScore
                : summary.loserScore;
            int scoreB = string.Equals(summary.winnerClubId, clubB, StringComparison.OrdinalIgnoreCase)
                ? summary.winnerScore
                : summary.loserScore;

            archive.matches.RemoveAll(x =>
                x.roundIndex == run.roundIndex && x.pairIndex == run.pairIndex);
            archive.matches.Add(new CompetitionBracketMatchRecord
            {
                roundIndex = run.roundIndex,
                pairIndex = run.pairIndex,
                clubAId = clubA,
                clubBId = clubB,
                winnerClubId = summary.winnerClubId,
                scoreA = scoreA,
                scoreB = scoreB
            });
        }

        private void AdvanceInteractiveCompetition(CompetitionDefinition competition)
        {
            CareerCompetitionRunState run = State.competitionRun;
            if (run == null) return;

            while (true)
            {
                if (run.currentRoundClubs.Count <= 1)
                {
                    if (run.playerPlacement <= 0)
                        run.playerPlacement = 1;
                    if (string.Equals(run.structureId, StockholmMajorRules.StructureId, StringComparison.OrdinalIgnoreCase))
                    {
                        SetMajorPlacement(run, State.playerClubId, run.playerPlacement);
                        if (run.currentRoundClubs.Count == 1 && !string.IsNullOrEmpty(run.currentRoundClubs[0]))
                            SetMajorPlacement(run, run.currentRoundClubs[0], 1);
                        CareerCompetitionRunState finished = run;
                        State.competitionRun = null;
                        FinalizeStockholmMajorAwards(competition, finished);
                        TryStartNextQueuedInteractiveCompetition();
                        return;
                    }

                    CompleteInteractiveCompetitionAwards(competition);
                    State.competitionRun = null;
                    TryStartNextQueuedInteractiveCompetition();
                    return;
                }

                if (TryCompleteRoundTransition(run))
                    continue;

                int pairStart = run.pairIndex * 2;
                if (pairStart + 1 >= run.currentRoundClubs.Count)
                {
                    if (pairStart < run.currentRoundClubs.Count)
                        run.roundWinners.Add(run.currentRoundClubs[pairStart]);
                    run.pairIndex++;
                    continue;
                }

                string clubA = run.currentRoundClubs[pairStart];
                string clubB = run.currentRoundClubs[pairStart + 1];

                // 極罕見：空籤或對手缺席 → 不戰而勝晉級。
                if (string.IsNullOrEmpty(clubA) && string.IsNullOrEmpty(clubB))
                {
                    run.pairIndex++;
                    continue;
                }

                if (string.IsNullOrEmpty(clubA) || string.IsNullOrEmpty(clubB))
                {
                    string winner = string.IsNullOrEmpty(clubA) ? clubB : clubA;
                    RecordWalkoverBracketMatch(competition, clubA ?? string.Empty, clubB ?? string.Empty, winner);
                    run.roundWinners.Add(winner);
                    run.pairIndex++;
                    continue;
                }

                bool playerMatch = string.Equals(clubA, State.playerClubId, StringComparison.OrdinalIgnoreCase) ||
                                   string.Equals(clubB, State.playerClubId, StringComparison.OrdinalIgnoreCase);

                if (playerMatch)
                {
                    string opponentClubId = string.Equals(clubA, State.playerClubId, StringComparison.OrdinalIgnoreCase)
                        ? clubB : clubA;
                    if (CanPlayInteractiveOfficialMatch(opponentClubId))
                    {
                        run.pendingOpponentClubId = opponentClubId;
                        run.pendingMatchSeed = StablePercent(
                            State.seed + State.clockHour, competition.id + clubA + clubB + run.roundIndex) * 7919;
                        ScheduleInteractivePlayerMatch(run);
                        return;
                    }

                    run.roundWinners.Add(SimulateKnockoutPair(competition, clubA, clubB));
                    TryRecordStockholmFinalPlacements(run, clubA, clubB, run.roundWinners[run.roundWinners.Count - 1]);
                    run.pairIndex++;
                    continue;
                }

                string aiWinner = SimulateKnockoutPair(competition, clubA, clubB);
                run.roundWinners.Add(aiWinner);
                TryRecordStockholmFinalPlacements(run, clubA, clubB, aiWinner);
                run.pairIndex++;
            }
        }

        private static void TryRecordStockholmFinalPlacements(
            CareerCompetitionRunState run, string clubA, string clubB, string winnerClubId)
        {
            if (run == null ||
                !string.Equals(run.structureId, StockholmMajorRules.StructureId, StringComparison.OrdinalIgnoreCase) ||
                run.currentRoundClubs == null ||
                run.currentRoundClubs.Count != 2 ||
                string.IsNullOrWhiteSpace(winnerClubId))
                return;

            string loser = string.Equals(winnerClubId, clubA, StringComparison.OrdinalIgnoreCase) ? clubB : clubA;
            if (!string.IsNullOrWhiteSpace(loser))
                SetMajorPlacement(run, loser, 2);
            SetMajorPlacement(run, winnerClubId, 1);
        }

        private void RecordWalkoverBracketMatch(
            CompetitionDefinition competition, string clubA, string clubB, string winnerClubId)
        {
            RecordBracketMatch(competition, clubA, clubB, new CareerMatchSummary
            {
                winnerClubId = winnerClubId,
                loserClubId = string.Equals(winnerClubId, clubA, StringComparison.OrdinalIgnoreCase) ? clubB : clubA,
                winnerScore = 1,
                loserScore = 0,
                winnerRating = 1f,
                loserRating = 0f,
                exposure = 0
            });
        }

        private static bool TryCompleteRoundTransition(CareerCompetitionRunState run)
        {
            int pairsInRound = (run.currentRoundClubs.Count + 1) / 2;
            if (run.pairIndex < pairsInRound) return false;
            run.currentRoundClubs = new List<string>(run.roundWinners);
            run.roundWinners = new List<string>();
            run.pairIndex = 0;
            run.roundIndex++;
            return true;
        }

        private void CompleteInteractiveCompetitionAwards(CompetitionDefinition competition)
        {
            CareerCompetitionRunState run = State.competitionRun;
            if (run == null) return;

            List<string> entrants = BuildEntrantList(competition);
            foreach (string clubId in entrants)
            {
                CompetitionEntry existing = State.competitionEntries.FirstOrDefault(x =>
                    x.competitionId == competition.id && x.clubId == clubId);
                if (existing != null && existing.completed) continue;

                int placement;
                if (string.Equals(clubId, State.playerClubId, StringComparison.OrdinalIgnoreCase) &&
                    run.playerPlacement > 0)
                    placement = run.playerPlacement;
                else
                    placement = SimulatePlacementForClub(competition, clubId, entrants);

                AwardCompetitionPlacement(competition, clubId, placement, entrants.Count);
            }

            // Placement / prize already awarded above; no match-complete news spam.
        }

        private int SimulatePlacementForClub(
            CompetitionDefinition competition, string clubId, List<string> entrants)
        {
            var random = new SeededRandom(StablePercent(State.seed, competition.id + clubId) * 9973);
            List<string> ordered = entrants
                .OrderByDescending(x => ClubPower(x) + (string.Equals(x, clubId, StringComparison.OrdinalIgnoreCase) ? 0 : random.Next01() * 8))
                .ToList();
            return Math.Max(1, ordered.IndexOf(clubId) + 1);
        }

        private void AwardCompetitionPlacement(
            CompetitionDefinition competition, string clubId, int placement, int fieldSize)
        {
            float points = RankingRules.CompetitionPlacementPoints(
                competition.tier, placement, ClubPower(clubId));
            State.rankingBatches.Add(new RankingBatch
            {
                id = competition.id + "_" + State.year + "_" + clubId + "_" + placement,
                clubId = clubId,
                competitionId = competition.id,
                earnedHour = State.clockHour,
                points = points,
                placement = placement,
                tier = competition.tier
            });
            // Batches land immediately; published HLTV table rebuilds every 7 days.
            TryAwardRmrPlacement(competition, clubId, placement);
            int prize = CompetitionPrizeRules.PrizeFor(competition.prizePool, placement, fieldSize);
            if (prize > 0 && State.TryGetClub(clubId, out ClubState club))
            {
                club.cash += prize;
                AddLedger(club.clubId, "prize", prize, competition.id, $"第 {placement} 名獎金");
            }

            CompetitionEntry entry = State.competitionEntries.FirstOrDefault(x =>
                x.competitionId == competition.id && x.clubId == clubId);
            if (entry != null)
            {
                entry.placement = placement;
                entry.completed = true;
            }

            if (placement == 1 &&
                competition.tier == CompetitionTier.Major &&
                string.Equals(clubId, State.playerClubId, StringComparison.OrdinalIgnoreCase) &&
                State.TryGetClub(clubId, out ClubState majorClub))
            {
                CareerFlagRules.SetFlag(State.flags, "won_major", clubId, State.clockHour, 0);
                CareerFlagRules.SetFlag(State.flags, "won_major", CareerFlagRules.GlobalScope, State.clockHour, 0);
                EvaluateAchievements(majorClub);
            }
        }

        private string SimulateKnockoutPair(CompetitionDefinition competition, string clubA, string clubB)
        {
            int matchSeed = StablePercent(State.seed + State.clockHour, clubA + clubB + competition.id) * 7919;
            CareerMatchSummary summary = _matches.Simulate(
                State, _playerDefinitions, clubA, clubB, competition.tier, matchSeed);
            ApplyMatch(summary);
            RecordBracketMatch(competition, clubA, clubB, summary);
            return summary.winnerClubId;
        }

        private void RecordBracketMatchFromCurrentPair(CompetitionDefinition competition, CareerMatchSummary summary)
        {
            CareerCompetitionRunState run = State.competitionRun;
            if (run == null || summary == null)
                return;
            int pairStart = run.pairIndex * 2;
            if (pairStart + 1 >= run.currentRoundClubs.Count)
                return;
            RecordBracketMatch(
                competition,
                run.currentRoundClubs[pairStart],
                run.currentRoundClubs[pairStart + 1],
                summary);
        }

        private static List<string> BuildKnockoutBracket(List<string> entrants, string playerClubId)
        {
            int need = Math.Min(16, Math.Max(2, entrants.Count));
            int size = 2;
            while (size < need)
                size *= 2;
            size = Math.Min(16, size);

            var bracket = new List<string>(size);
            for (int i = 0; i < size; i++)
                bracket.Add(i < entrants.Count ? entrants[i] : null);
            return bracket;
        }

        private static int PlacementForRoundLoss(int roundIndex, int teamsInRound)
        {
            if (teamsInRound <= 2) return 2;
            if (teamsInRound <= 4) return 3;
            if (teamsInRound <= 8) return 5;
            if (teamsInRound <= 16) return 9;
            return Math.Min(teamsInRound, 17);
        }

        private bool CanPlayInteractiveOfficialMatch(string opponentCareerClubId) =>
            _interactiveMatchContent != null &&
            CareerMatchTeams.TryResolveSimTeams(
                _interactiveMatchContent,
                State.playerClubId,
                opponentCareerClubId,
                out _,
                out _);

        private void RunCompetition(CompetitionDefinition competition, List<string> entrants)
        {
            var standings = entrants.ToDictionary(x => x, x => 0);
            for (int i = 0; i < entrants.Count; i++)
            for (int j = i + 1; j < entrants.Count; j++)
            {
                int matchSeed = StablePercent(State.seed + State.clockHour, entrants[i] + entrants[j]) * 7919 + i * 101 + j;
                CareerMatchSummary summary = _matches.Simulate(
                    State, _playerDefinitions, entrants[i], entrants[j], competition.tier, matchSeed);
                standings[summary.winnerClubId]++;
                ApplyMatch(summary);
            }
            string[] ordered = standings.OrderByDescending(x => x.Value)
                .ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase).Select(x => x.Key).ToArray();
            for (int i = 0; i < ordered.Length; i++)
            {
                int placement = i + 1;
                float points = RankingRules.CompetitionPlacementPoints(
                    competition.tier, placement, ClubPower(ordered[i]));
                State.rankingBatches.Add(new RankingBatch
                {
                    id = competition.id + "_" + State.year + "_" + ordered[i] + "_" + placement,
                    clubId = ordered[i],
                    competitionId = competition.id,
                    earnedHour = State.clockHour,
                    points = points,
                    placement = placement,
                    tier = competition.tier
                });
                int prize = CompetitionPrizeRules.PrizeFor(competition.prizePool, placement, ordered.Length);
                if (prize > 0 && State.TryGetClub(ordered[i], out ClubState club))
                {
                    club.cash += prize;
                    AddLedger(club.clubId, "prize", prize, competition.id, $"第 {placement} 名獎金");
                }
                CompetitionEntry entry = State.competitionEntries.FirstOrDefault(x =>
                    x.competitionId == competition.id && x.clubId == ordered[i]);
                if (entry != null) { entry.placement = placement; entry.completed = true; }
            }

            // Batches land immediately; published HLTV table rebuilds every 7 days.
            for (int i = 0; i < ordered.Length; i++)
                TryAwardRmrPlacement(competition, ordered[i], i + 1);
        }

        private void ApplyMatch(CareerMatchSummary summary)
        {
            if (summary == null ||
                string.IsNullOrWhiteSpace(summary.winnerClubId) ||
                string.IsNullOrWhiteSpace(summary.loserClubId))
                return;

            foreach (PlayerCareerState player in State.players.Where(x =>
                         string.Equals(x.clubId, summary.winnerClubId, StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(x.clubId, summary.loserClubId, StringComparison.OrdinalIgnoreCase)))
            {
                bool won = string.Equals(player.clubId, summary.winnerClubId, StringComparison.OrdinalIgnoreCase);
                player.careerMatches++;
                player.hoursWithoutMatch = 0;
                player.recentRating = won ? summary.winnerRating : summary.loserRating;
                player.ratingTrend = (player.recentRating - 1f) * 10f;
                player.form = Math.Max(0, Math.Min(100, player.form + (won ? 4 : -2)));
                player.energy = Math.Max(0, player.energy - 12);
                player.experience += 3;
            }

            // Bracket may reference clubs missing from older saves — never throw here.
            if (State.TryGetClub(summary.winnerClubId, out ClubState winnerClub))
            {
                winnerClub.exposure = Math.Min(100, winnerClub.exposure + summary.exposure / 5);
                ClubCrisisRules.ClampExposureIfCapped(State, winnerClub);
            }
            if (State.TryGetClub(summary.loserClubId, out ClubState loserClub))
            {
                loserClub.exposure = Math.Min(100, loserClub.exposure + summary.exposure / 8);
                ClubCrisisRules.ClampExposureIfCapped(State, loserClub);
            }

            TryAwardMatchRankingPoints(summary);
            RecordAnalystOpponentMeeting(summary);

            if (!string.IsNullOrWhiteSpace(State.playerClubId) &&
                State.competitionRun != null &&
                (string.Equals(summary.winnerClubId, State.playerClubId, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(summary.loserClubId, State.playerClubId, StringComparison.OrdinalIgnoreCase)))
                State.playerOfficialMatchesPlayed++;
        }

        private void RecordAnalystOpponentMeeting(CareerMatchSummary summary)
        {
            if (summary == null || string.IsNullOrWhiteSpace(State.playerClubId))
                return;
            string playerClub = State.playerClubId;
            string opponent;
            if (string.Equals(summary.winnerClubId, playerClub, StringComparison.OrdinalIgnoreCase))
                opponent = summary.loserClubId;
            else if (string.Equals(summary.loserClubId, playerClub, StringComparison.OrdinalIgnoreCase))
                opponent = summary.winnerClubId;
            else
                return;

            CoachStaffState analyst = ClubCoachStaffRules.FindByRole(State, playerClub, CoachStaffRole.Analyst);
            if (analyst != null)
                ClubCoachStaffRules.RecordOpponentMeeting(analyst, opponent);
        }

        private void TryAwardMatchRankingPoints(CareerMatchSummary summary)
        {
            CareerCompetitionRunState run = State.competitionRun;
            if (run == null || string.IsNullOrEmpty(run.competitionId))
                return;
            if (!_competitions.TryGetValue(run.competitionId, out CompetitionDefinition competition))
                return;
            if (!CareerCalendar.IsLive(competition, State.clockHour))
                return;

            float points = RankingRules.MatchWinPoints(competition.tier);
            if (points <= 0f)
                return;

            State.rankingBatches.Add(new RankingBatch
            {
                id = "match_" + State.clockHour + "_" + summary.winnerClubId + "_" + summary.loserClubId,
                clubId = summary.winnerClubId,
                competitionId = competition.id,
                earnedHour = State.clockHour,
                points = points,
                placement = 0,
                tier = competition.tier
            });

            // Batches land immediately; published HLTV table rebuilds every 7 days.
        }

        private void ProcessAiMarket(CareerAdvanceResult advanceResult = null)
        {
            if (!TransferNegotiationRules.QuoteSystemEnabled || !TransferWindowOpen())
                return;
            var random = new SeededRandom(State.seed + State.clockHour * 104729);
            ProcessAiSellerResponses(random);
            ProcessAiBuyerPersonalTerms(random);
            TryAiRivalSnipes(random, advanceResult);

            const double baseOfferChance = .12d;
            foreach (ClubState buyer in State.clubs.Where(x => x.clubId != State.playerClubId))
            {
                if (State.offers.Count(x =>
                        string.Equals(x.buyerClubId, buyer.clubId, StringComparison.OrdinalIgnoreCase) &&
                        TransferNegotiationRules.IsActiveNegotiation(x.status)) >= 2)
                    continue;

                PlayerCareerState target = State.players
                    .Where(x => x.clubId == State.playerClubId)
                    .OrderByDescending(x => x.recentRating + x.potential / 100f)
                    .FirstOrDefault();
                if (target == null || !_playerDefinitions.TryGetValue(target.playerId, out CareerPlayerDefinition targetDef))
                    continue;

                double inclination = MarketRules.BuyInclination(
                    State.players, _playerDefinitions, buyer.clubId, target, targetDef);
                if (inclination <= 0 || random.Next01() > baseOfferChance * inclination)
                    continue;

                if (State.offers.Any(x =>
                        string.Equals(x.buyerClubId, buyer.clubId, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(x.playerId, target.playerId, StringComparison.OrdinalIgnoreCase) &&
                        TransferNegotiationRules.IsActiveNegotiation(x.status)))
                    continue;

                ContractState contract = State.contracts.FirstOrDefault(x => x.playerId == target.playerId);
                int value = MarketRules.CalculateMarketValue(target, targetDef, contract,
                    targetDef.role == "Awper" ? 120 : 100);
                int fee = CareerMath.RoundMoney(value * (.9d + random.Next01() * .35d));
                if (buyer.cash - fee < EstimatedMonthlyExpense(buyer.clubId) * 3) continue;
                State.offers.Add(new TransferOffer
                {
                    id = "ai_offer_" + State.clockHour + "_" + buyer.clubId + "_" + (++_sequence),
                    playerId = target.playerId,
                    buyerClubId = buyer.clubId,
                    sellerClubId = State.playerClubId,
                    fee = fee,
                    createdHour = State.clockHour,
                    expiresHour = State.clockHour + TransferNegotiationRules.FeeOfferWindowHours,
                    status = TransferOfferStatus.Pending
                });
            }
        }

        private void ProcessAiSellerResponses(SeededRandom random)
        {
            foreach (TransferOffer offer in State.offers.Where(x =>
                         string.Equals(x.buyerClubId, State.playerClubId, StringComparison.OrdinalIgnoreCase) &&
                         (x.status == TransferOfferStatus.Pending || x.status == TransferOfferStatus.Countered) &&
                         !string.Equals(x.sellerClubId, State.playerClubId, StringComparison.OrdinalIgnoreCase))
                     .ToArray())
            {
                PlayerCareerState player = State.players.FirstOrDefault(x => x.playerId == offer.playerId);
                if (player == null ||
                    !_playerDefinitions.TryGetValue(player.playerId, out CareerPlayerDefinition definition))
                    continue;

                ContractState contract = State.contracts.FirstOrDefault(x => x.playerId == player.playerId);
                int value = MarketRules.CalculateMarketValue(player, definition, contract,
                    definition.role == "Awper" ? 120 : 100);
                float importance = player.transferRequested ? 0.1f : TransferNegotiationRules.ImportanceDefault;
                int reserve = TransferNegotiationRules.SellerReservePrice(value, importance);
                if (TransferNegotiationRules.SellerAcceptsFee(offer.fee, reserve) ||
                    (random.Next01() < 0.15d && offer.fee >= CareerMath.RoundMoney(reserve * 0.92d)))
                {
                    TransferNegotiationRules.ApplyClubAgreedPhase(offer, State.clockHour);
                    AddEvent(CareerEventType.ContractChanged, offer.sellerClubId, offer.playerId,
                        "對方俱樂部暫允轉會費，請盡快談妥個人條款");
                }
                else if (random.Next01() < 0.35d)
                {
                    offer.status = TransferOfferStatus.Rejected;
                }
            }
        }

        private void ProcessAiBuyerPersonalTerms(SeededRandom random)
        {
            foreach (TransferOffer offer in State.offers.Where(x =>
                         !string.Equals(x.buyerClubId, State.playerClubId, StringComparison.OrdinalIgnoreCase) &&
                         (x.status == TransferOfferStatus.ClubAgreed ||
                          x.status == TransferOfferStatus.PersonalTerms)).ToArray())
            {
                PlayerCareerState player = State.players.FirstOrDefault(x => x.playerId == offer.playerId);
                if (player == null ||
                    !_playerDefinitions.TryGetValue(player.playerId, out CareerPlayerDefinition definition))
                    continue;

                ContractState contract = State.contracts.FirstOrDefault(x => x.playerId == player.playerId);
                int value = MarketRules.CalculateMarketValue(player, definition, contract,
                    definition.role == "Awper" ? 120 : 100);
                int asked = TransferNegotiationRules.DefaultAskedSalary(player, value);
                if (offer.status == TransferOfferStatus.ClubAgreed)
                {
                    int offered = CareerMath.RoundMoney(asked * (0.95d + random.Next01() * 0.2d));
                    TransferNegotiationRules.ApplyPersonalTermsPhase(
                        offer, State.clockHour, asked, offered, CareerMath.RoundMoney(asked * 0.5d),
                        TransferNegotiationRules.DefaultContractMonths);
                }

                double score = BuildPersonalTermsScore(offer, player);
                if (score >= 90d || TransferNegotiationRules.PlayerAcceptsPersonalTerms(score, random))
                    CompleteTransfer(offer);
                else
                {
                    offer.negotiationRound++;
                    if (offer.negotiationRound >= 3)
                        offer.status = TransferOfferStatus.Failed;
                }
            }
        }

        private void TryAiRivalSnipes(SeededRandom random, CareerAdvanceResult advanceResult)
        {
            TransferOffer[] vulnerable = State.offers.Where(x =>
                TransferNegotiationRules.IsVulnerableToOutbid(x.status) &&
                (string.Equals(x.buyerClubId, State.playerClubId, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(x.sellerClubId, State.playerClubId, StringComparison.OrdinalIgnoreCase)))
                .ToArray();
            if (vulnerable.Length == 0) return;

            foreach (TransferOffer deal in vulnerable)
            {
                PlayerCareerState target = State.players.FirstOrDefault(x => x.playerId == deal.playerId);
                if (target == null ||
                    !_playerDefinitions.TryGetValue(target.playerId, out CareerPlayerDefinition targetDef))
                    continue;

                ContractState contract = State.contracts.FirstOrDefault(x => x.playerId == target.playerId);
                foreach (ClubState rival in State.clubs.Where(x =>
                             x.clubId != State.playerClubId &&
                             x.clubId != deal.buyerClubId &&
                             x.clubId != deal.sellerClubId))
                {
                    double inclination = MarketRules.BuyInclination(
                        State.players, _playerDefinitions, rival.clubId, target, targetDef);
                    if (inclination <= 0 || random.Next01() > 0.08d * inclination)
                        continue;

                    int value = MarketRules.CalculateMarketValue(target, targetDef, contract,
                        targetDef.role == "Awper" ? 120 : 100);
                    bool tryClause = contract != null && contract.releaseClause > 0 &&
                                     random.Next01() < 0.35d;
                    int fee = tryClause
                        ? contract.releaseClause
                        : CareerMath.RoundMoney(Math.Max(
                            deal.fee * TransferNegotiationRules.OutbidFeeMultiplier,
                            value * (1.05d + random.Next01() * 0.2d)));
                    if (!TransferNegotiationRules.CanOutbid(fee, deal.fee, tryClause))
                        continue;
                    if (rival.cash - fee < EstimatedMonthlyExpense(rival.clubId) * 3)
                        continue;

                    var sniping = new TransferOffer
                    {
                        id = "snipe_" + State.clockHour + "_" + rival.clubId + "_" + (++_sequence),
                        playerId = deal.playerId,
                        buyerClubId = rival.clubId,
                        sellerClubId = deal.sellerClubId,
                        fee = fee,
                        createdHour = State.clockHour,
                        expiresHour = State.clockHour + TransferNegotiationRules.FeeOfferWindowHours,
                        releaseClauseBid = tryClause,
                        status = TransferOfferStatus.Pending
                    };
                    if (tryClause)
                    {
                        int asked = TransferNegotiationRules.DefaultAskedSalary(target, value);
                        TransferNegotiationRules.ApplyPersonalTermsPhase(sniping, State.clockHour, asked);
                    }
                    else
                    {
                        TransferNegotiationRules.ApplyClubAgreedPhase(sniping, State.clockHour);
                    }

                    TransferNegotiationRules.MarkOutbid(deal, sniping.id);
                    State.offers.Add(sniping);
                    AddEvent(CareerEventType.ContractChanged, deal.buyerClubId, deal.playerId,
                        $"{rival.clubId} 介入搶人，原談判被打斷");
                    if (advanceResult != null &&
                        (string.Equals(deal.buyerClubId, State.playerClubId, StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(deal.sellerClubId, State.playerClubId, StringComparison.OrdinalIgnoreCase)))
                        advanceResult.transferDealInterrupted = true;
                    break;
                }
            }
        }

        private void CloseMonth()
        {
            foreach (ClubState club in State.clubs)
            {
                int income = 0;
                foreach (SponsorContract sponsor in State.sponsors
                             .Where(x => x.clubId == club.clubId).ToList())
                {
                    int sponsorIncome = FinanceRules.SponsorMonthly(sponsor, club, State.difficulty);
                    club.cash += sponsorIncome;
                    income += sponsorIncome;
                    AddLedger(club.clubId, "sponsor", sponsorIncome, sponsor.sponsorId, "贊助月費");
                    sponsor.remainingMonths--;
                    sponsor.satisfaction = Math.Max(0, Math.Min(100,
                        sponsor.satisfaction + (club.exposure >= sponsor.minimumExposure ? 3 : -8)));
                    sponsor.consecutiveLowMonths = sponsor.satisfaction < 25
                        ? sponsor.consecutiveLowMonths + 1 : 0;
                    if (sponsor.remainingMonths <= 0 || sponsor.consecutiveLowMonths >= 2)
                    {
                        State.sponsors.Remove(sponsor);
                        AddEvent(CareerEventType.SponsorChanged, club.clubId, sponsor.sponsorId, "贊助合約終止");
                    }
                }
                int stars = State.players.Count(x => x.clubId == club.clubId && x.fame >= 80);
                _clubDefinitions.TryGetValue(club.clubId, out CareerClubDefinition clubDef);
                int merchBase = FinanceRules.Merchandise(club, stars, State.difficulty);
                int merch = CareerMath.RoundMoney(merchBase *
                    ClubEstateSatisfactionRules.GetMerchandiseMultiplier(
                        State, club, _balance, clubDef,
                        State.players.Where(p => p.clubId == club.clubId)));
                club.cash += merch;
                income += merch;
                AddLedger(club.clubId, "merchandise", merch, null, "商品銷售");

                int salaries = State.contracts.Where(x => x.clubId == club.clubId).Sum(x => x.monthlySalary);
                int staffSalaries = ClubCoachStaffRules.MonthlyStaffSalary(State, club.clubId);
                // Facility operating cost temporarily disabled.
                int maintenance = 0;
                int expense = salaries + staffSalaries + maintenance;
                club.cash -= expense;
                AddLedger(club.clubId, "salary", -salaries, null, "選手薪資");
                if (staffSalaries > 0)
                    AddLedger(club.clubId, "staff_salary", -staffSalaries, null, "教練組薪資");
                ClubCoachStaffRules.ApplyMonthlyStaffPulse(State, club.clubId);
                club.lastMonthNet = income - expense;
                club.consecutiveLossMonths = club.lastMonthNet < 0 ? club.consecutiveLossMonths + 1 : 0;
                FinanceRules.PushExpenseHistory(club, expense);
                FinancialHealth previous = club.financialHealth;
                club.financialHealth = FinanceRules.Evaluate(club, expense);
                club.consecutiveCrisisMonths = club.financialHealth == FinancialHealth.Crisis
                    ? club.consecutiveCrisisMonths + 1 : 0;
                ClubEstateSatisfactionRules.ApplyMonthlyFinancialPulse(club, club.financialHealth);
                if (previous != club.financialHealth)
                    AddEvent(CareerEventType.FinancialStatusChanged, club.clubId, null,
                        $"{previous} → {club.financialHealth}");
                if (club.financialHealth == FinancialHealth.Bankrupt && club.clubId == State.playerClubId)
                    State.gameOver = State.difficulty == CareerDifficulty.Hardcore;
            }
            foreach (ContractState contract in State.contracts) contract.remainingMonths--;
            ReleaseExpiredContracts();
            CareerFlagRules.SetFlag(State.flags, "month_closed_once", CareerFlagRules.GlobalScope, State.clockHour, 0);
            EvaluateAchievements(State.Club(State.playerClubId));
            AddEvent(CareerEventType.MonthClosed, State.playerClubId, null, "完成月結");
        }

        private void ReleaseExpiredContracts()
        {
            foreach (ContractState contract in State.contracts.Where(x => x.remainingMonths <= 0).ToArray())
            {
                PlayerCareerState player = State.Player(contract.playerId);
                player.clubId = string.Empty;
                State.contracts.Remove(contract);
                AddEvent(CareerEventType.ContractChanged, contract.clubId, player.playerId, "合約到期成為自由球員");
            }
        }

        private void CompleteTransfer(TransferOffer offer)
        {
            ClubState buyer = State.Club(offer.buyerClubId);
            ClubState seller = State.Club(offer.sellerClubId);
            if (buyer.cash < offer.fee)
            {
                offer.status = TransferOfferStatus.Failed;
                return;
            }
            buyer.cash -= offer.fee;
            seller.cash += CareerMath.RoundMoney(offer.fee * .97d);
            AddLedger(buyer.clubId, "transfer", -offer.fee, offer.playerId, "買入選手");
            AddLedger(seller.clubId, "transfer", CareerMath.RoundMoney(offer.fee * .97d), offer.playerId, "出售選手");
            if (offer.signingBonus > 0)
            {
                if (buyer.cash < offer.signingBonus)
                    offer.signingBonus = 0;
                else
                {
                    buyer.cash -= offer.signingBonus;
                    AddLedger(buyer.clubId, "transfer", -offer.signingBonus, offer.playerId, "簽字費");
                }
            }

            PlayerCareerState player = State.Player(offer.playerId);
            player.clubId = buyer.clubId;
            player.transferRequested = false;
            ContractState contract = State.contracts.FirstOrDefault(x => x.playerId == player.playerId);
            int months = offer.contractMonths > 0
                ? offer.contractMonths
                : TransferNegotiationRules.DefaultContractMonths;
            int salary = offer.offeredSalary > 0
                ? offer.offeredSalary
                : MarketRules.AskingSalary(offer.fee, player.age, player.fame);
            if (contract != null)
            {
                contract.clubId = buyer.clubId;
                contract.remainingMonths = months;
                contract.monthlySalary = salary;
                contract.signingBonus = offer.signingBonus;
            }
            else
            {
                State.contracts.Add(new ContractState
                {
                    id = "contract_" + player.playerId + "_" + State.clockHour,
                    playerId = player.playerId,
                    clubId = buyer.clubId,
                    startYear = State.year,
                    startHour = State.clockHour,
                    remainingMonths = months,
                    monthlySalary = salary,
                    signingBonus = offer.signingBonus
                });
            }

            offer.status = TransferOfferStatus.Accepted;
            foreach (TransferOffer rival in State.offers.Where(x =>
                         string.Equals(x.playerId, offer.playerId, StringComparison.OrdinalIgnoreCase) &&
                         TransferNegotiationRules.IsActiveNegotiation(x.status) &&
                         !string.Equals(x.id, offer.id, StringComparison.OrdinalIgnoreCase)).ToArray())
            {
                TransferNegotiationRules.MarkOutbid(rival, offer.id);
            }

            AddEvent(CareerEventType.TransferCompleted, buyer.clubId, player.playerId,
                offer.releaseClauseBid
                    ? $"以違約金 ${offer.fee:N0} 完成轉會"
                    : $"以 ${offer.fee:N0} 完成轉會");
            ApplyRmrRosterDeduction(buyer.clubId, "transfer_" + offer.playerId);
            ApplyRmrRosterDeduction(seller.clubId, "transfer_out_" + offer.playerId);
        }

        private void ExpireOffers()
        {
            foreach (TransferOffer offer in State.offers.Where(x =>
                         TransferNegotiationRules.IsActiveNegotiation(x.status)).ToArray())
            {
                int deadline = offer.phaseExpiresHour > 0
                    ? Math.Min(
                        offer.expiresHour <= 0 ? int.MaxValue : offer.expiresHour,
                        offer.phaseExpiresHour)
                    : offer.expiresHour;
                if (deadline <= 0 || deadline >= State.clockHour)
                    continue;

                if (offer.status == TransferOfferStatus.ClubAgreed ||
                    offer.status == TransferOfferStatus.PersonalTerms)
                    offer.status = TransferOfferStatus.Failed;
                else
                    offer.status = TransferOfferStatus.Expired;
            }
        }

        private void RefreshRankings(bool notifyPlayer = false)
        {
            RankingEntry previous = State.rankings.FirstOrDefault(x =>
                string.Equals(x.clubId, State.playerClubId, StringComparison.OrdinalIgnoreCase));
            int previousRank = previous?.rank ?? 0;
            float previousPoints = previous?.points ?? 0f;

            State.rankings = RankingRules.Build(
                State.rankingBatches, State.clubs.Select(x => x.clubId),
                State.clockHour,
                CareerCalendar.ResolveRankingHalfLifeHours(_balance),
                CareerCalendar.ResolveRankingWindowHours(_balance));

            if (!notifyPlayer)
                return;

            RankingEntry current = State.rankings.FirstOrDefault(x =>
                string.Equals(x.clubId, State.playerClubId, StringComparison.OrdinalIgnoreCase));
            if (current == null)
                return;
            if (current.rank == previousRank && Math.Abs(current.points - previousPoints) < 0.5f)
                return;

            // Ranking changes are visible on the world-rank panel; not news-feed material.
        }

        private void TryAwardRmrPlacement(CompetitionDefinition competition, string clubId, int placement)
        {
            if (competition == null || string.IsNullOrWhiteSpace(clubId) || placement <= 0)
                return;
            if (!RmrRules.TryResolveCompetitionRmr(competition, out RmrRound round, out string eventRegion))
                return;
            if (!_clubDefinitions.TryGetValue(clubId, out CareerClubDefinition clubDef))
                return;
            string clubRegion = RmrRules.ResolveClubRmrRegion(clubDef);
            if (!string.Equals(clubRegion, eventRegion, StringComparison.OrdinalIgnoreCase))
                return;

            float points = RmrRules.PointsFor(round, placement);
            if (points <= 0f)
                return;

            State.rmrBatches = State.rmrBatches ?? new List<RmrPointBatch>();
            string batchId = $"rmr_{competition.id}_{State.year}_{clubId}_{placement}";
            if (State.rmrBatches.Any(x =>
                    x != null && string.Equals(x.id, batchId, StringComparison.OrdinalIgnoreCase)))
                return;

            RmrPointBatch batch = RmrRules.CreatePlacementBatch(
                clubId, competition.id, round, placement, State.clockHour);
            batch.id = batchId;
            State.rmrBatches.Add(batch);
            RefreshRmrRankings();
        }

        private void ApplyRmrRosterDeduction(string clubId, string reasonId)
        {
            if (string.IsNullOrWhiteSpace(clubId))
                return;
            State.rmrBatches = State.rmrBatches ?? new List<RmrPointBatch>();
            RmrPointBatch deduct = RmrRules.CreateRosterDeductionBatch(
                State.rmrBatches, clubId, State.clockHour, reasonId);
            if (deduct == null)
                return;
            State.rmrBatches.Add(deduct);
            RefreshRmrRankings();
        }

        private void RefreshRmrRankings()
        {
            State.rmrBatches = State.rmrBatches ?? new List<RmrPointBatch>();
            State.rmrRankings = RmrRules.Build(State.rmrBatches, _clubDefinitions.Values);
        }

        private int EstimatedMonthlyExpense(string clubId) =>
            State.contracts.Where(x => x.clubId == clubId).Sum(x => x.monthlySalary) +
            ClubCoachStaffRules.MonthlyStaffSalary(State, clubId);

        public MonthlyBudgetPreview GetMonthlyBudgetPreview(string clubId)
        {
            ClubState club = State.Club(clubId);
            int stars = State.players.Count(x => x.clubId == clubId && x.fame >= 80);
            ContractState[] contracts = State.contracts
                .Where(x => string.Equals(x.clubId, clubId, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            CoachStaffState[] coaches = ClubCoachStaffRules.ClubCoaches(State, clubId).ToArray();

            MonthlyBudgetPreview preview = FinanceRules.BuildMonthlyPreview(
                club,
                State.sponsors,
                contracts,
                coaches,
                State.ledger,
                _balance,
                State.clockHour,
                _facilityDefinitions,
                stars,
                State.difficulty);

            foreach (SponsorContract contract in State.sponsors
                         .Where(x => string.Equals(x.clubId, clubId, StringComparison.OrdinalIgnoreCase)))
            {
                string name = contract.sponsorId;
                if (_sponsorDefinitions != null && _sponsorDefinitions.TryGetValue(contract.sponsorId, out SponsorDefinition def) &&
                    !string.IsNullOrWhiteSpace(def.displayName))
                    name = def.displayName;
                preview.sponsorDetails.Add(new BudgetBreakdownLine
                {
                    label = name,
                    amount = FinanceRules.SponsorMonthly(contract, club, State.difficulty)
                });
            }

            preview.merchandiseDetails.Add(new BudgetBreakdownLine
            {
                label = "商品銷售預估",
                amount = preview.merchandiseIncome
            });

            foreach (ContractState contract in contracts.OrderByDescending(x => x.monthlySalary))
            {
                string name = contract.playerId;
                if (_playerDefinitions != null && _playerDefinitions.TryGetValue(contract.playerId, out CareerPlayerDefinition player) &&
                    !string.IsNullOrWhiteSpace(player.displayName))
                    name = player.displayName;
                preview.salaryDetails.Add(new BudgetBreakdownLine
                {
                    label = name,
                    amount = contract.monthlySalary
                });
            }

            foreach (CoachStaffState coach in coaches
                         .OrderBy(c => (int)ClubCoachStaffRules.ParseRole(c.role))
                         .ThenByDescending(c => c.monthlySalary))
            {
                string name = coach.coachId;
                if (_coachDefinitions != null &&
                    _coachDefinitions.TryGetValue(coach.coachId, out CareerCoachDefinition def) &&
                    !string.IsNullOrWhiteSpace(def.displayName))
                    name = def.displayName;
                string role = ClubCoachStaffRules.RoleLabel(coach.role);
                preview.coachSalaryDetails.Add(new BudgetBreakdownLine
                {
                    label = $"{role} · {name}",
                    amount = Math.Max(0, coach.monthlySalary)
                });
            }

            return preview;
        }

        private double ClubPower(string clubId)
        {
            PlayerCareerState[] roster = State.players.Where(x => x.clubId == clubId).ToArray();
            return roster.Length == 0 ? 50 : roster.Average(x => x.power);
        }

        private bool TransferWindowOpen()
        {
            DateTime now = CareerCalendar.ToDateTime(_balance, State);
            bool winter = now.Month == 1 && now.Day <= 28;
            bool summer = now.Month == 7 && now.Day <= 28;
            return winter || summer;
        }

        private void AddLedger(string clubId, string category, int amount, string sourceId, string note)
        {
            var entry = new LedgerEntry
            {
                id = "ledger_" + State.year + "_" + State.clockHour + "_" + (++_sequence),
                year = State.year,
                clockHour = State.clockHour,
                clubId = clubId,
                category = category,
                amount = amount,
                sourceId = sourceId,
                note = note
            };
            State.ledger.Add(entry);
        }

        public bool PendingEventBlocksClock()
        {
            CareerScriptedEventDef def = GetPendingEventDefinition();
            if (def == null)
                return false;
            // C with choices uses interactive popup but is not S/A/B "modal" — still honor pause prefs.
            if (!ScriptedEventTierRules.UsesInteractivePopup(def))
                return false;
            ScriptedEventTier tier = ScriptedEventTierRules.Resolve(def);
            return EvaluatePauseForTier(tier);
        }

        public bool EvaluatePauseForTier(ScriptedEventTier tier)
        {
            if (ScriptedEventTierRules.ForcePause(tier))
                return true;
            if (ShouldPauseOnEventTier != null)
                return ShouldPauseOnEventTier(tier);
            return ScriptedEventTierRules.ShouldPause(tier, true, true, true, false);
        }

        private void ProcessScriptedPulse(CareerAdvanceResult result, bool scheduledOnly = false)
        {
            CareerFlagRules.PurgeExpired(State.flags, State.clockHour);
            ClubState club = State.Club(State.playerClubId);
            EvaluateAchievements(club);

            if (State.pendingEvents.Count > 0)
            {
                CareerScriptedEventDef pendingDef = GetPendingEventDefinition();
                if (pendingDef != null && ScriptedEventTierRules.UsesInteractivePopup(pendingDef))
                    result.scriptedEventPending = true;
                if (PendingEventBlocksClock())
                    result.scriptedEventShouldPause = true;
                return;
            }

            if (_scriptedEvents.Count == 0)
                return;

            CareerScriptedEventDef fired;
            if (scheduledOnly)
            {
                fired = CareerScriptedRules.PickDueScheduledEvent(
                    _scriptedEvents.Values, State, club, _scriptedFireHours);
            }
            else
            {
                var random = new SeededRandom(State.seed + State.clockHour * 7919 + 17);
                fired = CareerScriptedRules.PickEligibleEvent(
                    _scriptedEvents.Values, State, club, random, daysInStep: 7, _scriptedFireHours);
            }

            if (fired == null)
                return;

            ScriptedEventTier tier = ScriptedEventTierRules.Resolve(fired);
            string newsClubId = !string.IsNullOrWhiteSpace(fired.subjectClubId)
                ? fired.subjectClubId
                : club.clubId;

            CareerFlagRules.SetFlag(
                State.flags,
                ScriptedEventTierRules.FiredFlagId(fired.id),
                CareerFlagRules.GlobalScope,
                State.clockHour,
                0);

            ApplyOnFireEffects(fired);
            ApplyOnFireRosterOps(fired);

            if (ScriptedEventTierRules.UsesNotificationOnly(fired))
            {
                ApplyNotificationEvent(fired, club, result, tier, newsClubId);
                return;
            }

            AddEvent(
                CareerEventType.ScriptedEvent,
                newsClubId,
                fired.id,
                ResolveScriptedEventBody(fired),
                tier,
                fired.publicVisible || ScriptedEventTierRules.UsesNotificationBar(tier),
                title: fired.title);

            State.pendingEvents.Add(new CareerPendingEvent
            {
                instanceId = "pending_" + State.clockHour + "_" + (++_sequence),
                eventId = fired.id,
                clubId = club.clubId,
                firedHour = State.clockHour
            });
            // Always ask UI to open the popup; pause is controlled separately by tier prefs.
            result.scriptedEventPending = true;
            result.scriptedEventShouldPause = EvaluatePauseForTier(tier);
        }

        private void ApplyOnFireEffects(CareerScriptedEventDef def)
        {
            if (def?.onFireEffects == null || def.onFireEffects.Length == 0)
                return;

            CareerEffectDef[] globalEffects = def.onFireEffects
                .Where(IsGlobalScriptedEffect)
                .ToArray();
            if (globalEffects.Length > 0)
                ApplyGlobalEffects(globalEffects);

            CareerEffectDef[] clubEffects = def.onFireEffects
                .Where(x => !IsGlobalScriptedEffect(x))
                .ToArray();
            if (clubEffects.Length == 0)
                return;

            string targetId = !string.IsNullOrWhiteSpace(def.subjectClubId)
                ? def.subjectClubId
                : State.playerClubId;
            if (!State.TryGetClub(targetId, out ClubState target))
                return;

            if (!string.IsNullOrWhiteSpace(def.onFireRosterMapId) &&
                !string.IsNullOrWhiteSpace(def.onFireRosterMinGrade) &&
                !RosterHasMapGrade(target, def.onFireRosterMapId, def.onFireRosterMinGrade))
                return;

            ApplyEffects(clubEffects, target);
        }

        private static bool IsGlobalScriptedEffect(CareerEffectDef effect)
        {
            if (effect == null || string.IsNullOrWhiteSpace(effect.type))
                return false;
            string type = effect.type.Trim();
            if (string.Equals(type, "add_role_map_proficiency", StringComparison.OrdinalIgnoreCase))
                return true;
            // World map-pool flag must apply even when club roster gates skip other onFire effects.
            if (string.Equals(type, "set_flag", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(type, "clear_flag", StringComparison.OrdinalIgnoreCase))
            {
                return string.Equals(
                    effect.key, MapPoolRules.TrainPoolRemovedFlag, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        /// <summary>
        /// Apply scripted historical roster changes.
        /// Join/release/transfer never touch the player club; set_squad may, so planned
        /// starter/bench swaps like flamie ↔ b1t still run when playing as NAVI.
        /// </summary>
        private void ApplyOnFireRosterOps(CareerScriptedEventDef def)
        {
            if (def?.onFireRosterOps == null || def.onFireRosterOps.Length == 0)
                return;

            bool subjectIsPlayer =
                !string.IsNullOrWhiteSpace(def.subjectClubId) &&
                string.Equals(def.subjectClubId, State.playerClubId, StringComparison.OrdinalIgnoreCase);

            foreach (CareerRosterOpDef op in def.onFireRosterOps)
            {
                if (op == null || string.IsNullOrWhiteSpace(op.type))
                    continue;
                string type = op.type.Trim().ToLowerInvariant();
                if (subjectIsPlayer && type != "set_squad")
                    continue;
                TryApplyHistoricalRosterOp(op, def, allowSetSquadOnPlayerClub: subjectIsPlayer);
            }
        }

        private void TryApplyHistoricalRosterOp(
            CareerRosterOpDef op, CareerScriptedEventDef def, bool allowSetSquadOnPlayerClub = false)
        {
            if (op == null || string.IsNullOrWhiteSpace(op.type) || string.IsNullOrWhiteSpace(op.playerId))
                return;

            PlayerCareerState player = State.players?.FirstOrDefault(x =>
                x != null && string.Equals(x.playerId, op.playerId, StringComparison.OrdinalIgnoreCase));
            if (player == null)
                return;

            bool onPlayerClub = !string.IsNullOrWhiteSpace(State.playerClubId) &&
                string.Equals(player.clubId, State.playerClubId, StringComparison.OrdinalIgnoreCase);

            string type = op.type.Trim().ToLowerInvariant();
            if (onPlayerClub && !(allowSetSquadOnPlayerClub && type == "set_squad"))
                return;

            switch (type)
            {
                case "set_squad":
                    player.squadStatus = ClubEstateRules.NormalizeSquadStatus(
                        string.IsNullOrWhiteSpace(op.squadStatus)
                            ? ClubEstateRules.SquadBench
                            : op.squadStatus);
                    break;
                case "join_club":
                    if (string.IsNullOrWhiteSpace(op.clubId) || !State.TryGetClub(op.clubId, out _))
                        return;
                    if (string.Equals(op.clubId, State.playerClubId, StringComparison.OrdinalIgnoreCase))
                        return;
                    ApplyHistoricalJoinClub(player, op.clubId, op.squadStatus);
                    AddEvent(
                        CareerEventType.TransferCompleted,
                        op.clubId,
                        player.playerId,
                        def != null ? "歷史陣容：" + def.title : "歷史陣容調整");
                    break;
                case "release":
                    ApplyHistoricalRelease(player);
                    break;
                case "request_transfer":
                    player.transferRequested = true;
                    if (!string.IsNullOrWhiteSpace(op.squadStatus))
                    {
                        player.squadStatus = ClubEstateRules.NormalizeSquadStatus(op.squadStatus);
                    }
                    break;
            }
        }

        private void ApplyHistoricalJoinClub(PlayerCareerState player, string clubId, string squadStatus)
        {
            CancelActiveOffersForPlayer(player.playerId);
            player.clubId = clubId;
            player.transferRequested = false;
            player.squadStatus = ClubEstateRules.NormalizeSquadStatus(
                string.IsNullOrWhiteSpace(squadStatus)
                    ? ClubEstateRules.SquadStarter
                    : squadStatus);

            ContractState contract = State.contracts?.FirstOrDefault(x =>
                string.Equals(x.playerId, player.playerId, StringComparison.OrdinalIgnoreCase));
            if (contract != null)
            {
                contract.clubId = clubId;
                if (contract.remainingMonths <= 0)
                    contract.remainingMonths = TransferNegotiationRules.DefaultContractMonths;
            }
            else
            {
                State.contracts.Add(new ContractState
                {
                    id = "contract_" + player.playerId + "_" + State.clockHour,
                    playerId = player.playerId,
                    clubId = clubId,
                    startYear = State.year,
                    startHour = State.clockHour,
                    remainingMonths = TransferNegotiationRules.DefaultContractMonths,
                    monthlySalary = MarketRules.AskingSalary(
                        Math.Max(100000, player.power * 5000), player.age, player.fame)
                });
            }

            ApplyRmrRosterDeduction(clubId, "historical_join_" + player.playerId);
        }

        private void ApplyHistoricalRelease(PlayerCareerState player)
        {
            CancelActiveOffersForPlayer(player.playerId);
            player.clubId = string.Empty;
            player.transferRequested = true;
            player.squadStatus = ClubEstateRules.SquadBench;
            ContractState contract = State.contracts?.FirstOrDefault(x =>
                string.Equals(x.playerId, player.playerId, StringComparison.OrdinalIgnoreCase));
            if (contract != null)
                State.contracts.Remove(contract);
        }

        private void CancelActiveOffersForPlayer(string playerId)
        {
            if (State.offers == null || string.IsNullOrWhiteSpace(playerId))
                return;
            foreach (TransferOffer offer in State.offers.Where(x =>
                         x != null &&
                         string.Equals(x.playerId, playerId, StringComparison.OrdinalIgnoreCase) &&
                         TransferNegotiationRules.IsActiveNegotiation(x.status)).ToArray())
            {
                offer.status = TransferOfferStatus.Failed;
            }
        }

        private bool RosterHasMapGrade(ClubState club, string mapId, string minGrade)
        {
            if (club == null || State.players == null)
                return false;
            foreach (PlayerCareerState player in State.players)
            {
                if (player == null ||
                    !string.Equals(player.clubId, club.clubId, StringComparison.OrdinalIgnoreCase))
                    continue;
                CareerNamedStat stat = player.mapProficiencies?.FirstOrDefault(x =>
                    string.Equals(x.id, mapId, StringComparison.OrdinalIgnoreCase));
                if (stat != null && !string.IsNullOrWhiteSpace(stat.grade) &&
                    GradeScale.AtLeast(stat.grade, minGrade))
                    return true;
            }

            return false;
        }

        public bool MeetsScriptedEventFirstPersonRosterGate(CareerScriptedEventDef def) =>
            ScriptedEventTierRules.MeetsFirstPersonRosterGate(
                def,
                State.playerClubId,
                State.players,
                ResolvePlayerPrimaryRole);

        public string ResolveScriptedEventBody(CareerScriptedEventDef def)
        {
            string body = ScriptedEventTierRules.ResolveBody(
                def,
                State.playerClubId,
                () => MeetsScriptedEventFirstPersonRosterGate(def));
            if (def == null || string.IsNullOrWhiteSpace(def.tokenCompetitionId))
                return body;
            return CareerScriptedRules.ApplyBodyTokens(
                body,
                ResolveFinalOpponentDisplayName(def.tokenCompetitionId, def.subjectClubId));
        }

        public CareerEventOptionDef[] ResolveScriptedEventOptions(CareerScriptedEventDef def) =>
            ScriptedEventTierRules.ResolveOptions(
                def,
                State.playerClubId,
                () => MeetsScriptedEventFirstPersonRosterGate(def));

        private string ResolveFinalOpponentDisplayName(string competitionId, string championClubId)
        {
            if (string.IsNullOrWhiteSpace(competitionId))
                return null;

            if (!string.IsNullOrWhiteSpace(championClubId))
            {
                CompetitionEntry runnerUp = State.competitionEntries?.FirstOrDefault(x =>
                    x != null &&
                    x.completed &&
                    x.placement == 2 &&
                    string.Equals(x.competitionId, competitionId, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(x.clubId, championClubId, StringComparison.OrdinalIgnoreCase));
                if (runnerUp != null)
                    return ClubDisplayName(runnerUp.clubId);
            }
            else
            {
                CompetitionEntry runnerUp = State.competitionEntries?.FirstOrDefault(x =>
                    x != null &&
                    x.completed &&
                    x.placement == 2 &&
                    string.Equals(x.competitionId, competitionId, StringComparison.OrdinalIgnoreCase));
                if (runnerUp != null)
                    return ClubDisplayName(runnerUp.clubId);
            }

            CompetitionBracketArchive archive = State.competitionBrackets?.FirstOrDefault(x =>
                string.Equals(x.competitionId, competitionId, StringComparison.OrdinalIgnoreCase));
            CompetitionBracketMatchRecord finalMatch = archive?.matches?
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.winnerClubId))
                .OrderByDescending(x => x.roundIndex)
                .ThenByDescending(x => x.pairIndex)
                .FirstOrDefault();
            if (finalMatch == null)
                return null;

            string loserId = string.Equals(
                    finalMatch.winnerClubId, finalMatch.clubAId, StringComparison.OrdinalIgnoreCase)
                ? finalMatch.clubBId
                : finalMatch.clubAId;
            if (!string.IsNullOrWhiteSpace(championClubId) &&
                !string.Equals(finalMatch.winnerClubId, championClubId, StringComparison.OrdinalIgnoreCase))
            {
                // Prefer a match the authored champion actually won.
                CompetitionBracketMatchRecord championFinal = archive.matches
                    .Where(x =>
                        x != null &&
                        string.Equals(x.winnerClubId, championClubId, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(x => x.roundIndex)
                    .ThenByDescending(x => x.pairIndex)
                    .FirstOrDefault();
                if (championFinal != null)
                {
                    loserId = string.Equals(
                            championFinal.winnerClubId, championFinal.clubAId, StringComparison.OrdinalIgnoreCase)
                        ? championFinal.clubBId
                        : championFinal.clubAId;
                }
            }

            return ClubDisplayName(loserId);
        }

        private string ClubDisplayName(string clubId)
        {
            if (string.IsNullOrWhiteSpace(clubId))
                return null;
            return _clubDefinitions.TryGetValue(clubId, out CareerClubDefinition club) &&
                   !string.IsNullOrWhiteSpace(club.displayName)
                ? club.displayName
                : clubId;
        }

        private string ResolvePlayerPrimaryRole(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId) ||
                _playerDefinitions == null ||
                !_playerDefinitions.TryGetValue(playerId, out CareerPlayerDefinition definition))
                return null;
            return definition.role;
        }

        private void ApplyNotificationEvent(
            CareerScriptedEventDef def,
            ClubState club,
            CareerAdvanceResult result,
            ScriptedEventTier tier,
            string newsClubId)
        {
            CareerEventOptionDef[] options = ResolveScriptedEventOptions(def);
            CareerEventOptionDef option = options.Length > 0 ? options[0] : null;
            string body = ResolveScriptedEventBody(def);
            string message = body;
            if (option != null)
            {
                ApplyEffects(option.effects, club);
                if (!string.IsNullOrWhiteSpace(option.label))
                    message = string.IsNullOrWhiteSpace(body)
                        ? option.label
                        : body + "\n\n" + option.label;
            }

            AddEvent(
                CareerEventType.ScriptedEvent,
                newsClubId,
                def.id,
                message,
                tier,
                def.publicVisible || ScriptedEventTierRules.Resolve(def) != ScriptedEventTier.D,
                title: def.title);

            result.eventToastPending = true;
            result.eventToastTitle = def.title ?? "通知";
            result.eventToastBody = body;
            result.eventToastTier = tier;
            result.eventToastShouldPause = EvaluatePauseForTier(tier);
            EvaluateAchievements(club);
        }

        public CareerPendingEvent GetActivePendingEvent() =>
            State.pendingEvents?.FirstOrDefault();

        public CareerScriptedEventDef GetPendingEventDefinition()
        {
            CareerPendingEvent pending = GetActivePendingEvent();
            if (pending == null)
                return null;
            _scriptedEvents.TryGetValue(pending.eventId, out CareerScriptedEventDef def);
            return def;
        }

        public bool TryGetScriptedEventDefinition(string eventId, out CareerScriptedEventDef def) =>
            _scriptedEvents.TryGetValue(eventId ?? string.Empty, out def);

        public bool TryEnactClubResolution(string resolutionId, out string failure)
        {
            failure = null;
            if (string.IsNullOrWhiteSpace(resolutionId))
            {
                failure = "未知決議";
                return false;
            }

            ClubState club = State.Club(State.playerClubId);
            if (!ClubEstateResolutionRules.TryEnact(State, club, resolutionId, out failure))
                return false;

            ClubEstateResolutionDef def = ClubEstateResolutionRules.All.FirstOrDefault(x =>
                string.Equals(x.id, resolutionId, StringComparison.OrdinalIgnoreCase));
            if (def.cashCost > 0)
                AddLedger(club.clubId, "resolution", -def.cashCost, resolutionId, "俱樂部決議");
            AddEvent(CareerEventType.ScriptedEvent, club.clubId, resolutionId,
                def != null ? "決議：" + def.title : "俱樂部決議");
            EvaluateAchievements(club);
            return true;
        }

        public bool TryResolvePendingEvent(string optionId, out string failure)
        {
            failure = null;
            CareerPendingEvent pending = GetActivePendingEvent();
            if (pending == null)
            {
                failure = "沒有待處理事件";
                return false;
            }
            if (!_scriptedEvents.TryGetValue(pending.eventId, out CareerScriptedEventDef def))
            {
                State.pendingEvents.Remove(pending);
                failure = "事件定義缺失";
                return false;
            }

            CareerEventOptionDef[] resolvedOptions = ResolveScriptedEventOptions(def);
            CareerEventOptionDef option = resolvedOptions.FirstOrDefault(x =>
                string.Equals(x.id, optionId, StringComparison.OrdinalIgnoreCase));
            if (option == null && resolvedOptions.Length > 0)
                option = resolvedOptions[0];
            if (option == null)
            {
                State.pendingEvents.Remove(pending);
                failure = "事件沒有可選項";
                return false;
            }

            ClubState club = State.Club(pending.clubId);
            ApplyEffects(option.effects, club);
            State.pendingEvents.Remove(pending);
            EvaluateAchievements(club);
            // Same-day / overdue scheduled events should not wait for the next clock day.
            ProcessScriptedPulse(new CareerAdvanceResult(), scheduledOnly: true);
            return true;
        }

        private void EvaluateAchievements(ClubState club)
        {
            if (!CareerDifficultyRules.AchievementsEligible(State.difficulty))
                return;
            if (club == null || _achievements.Count == 0)
                return;

            foreach (CareerAchievementDef achievement in _achievements.Values)
            {
                if (achievement.hardcoreOnly && State.difficulty != CareerDifficulty.Hardcore)
                    continue;
                if (State.unlockedAchievements.Contains(achievement.id, StringComparer.OrdinalIgnoreCase))
                    continue;
                if (!CareerScriptedRules.MatchesAll(
                        achievement.triggers, State, club, State.unlockedAchievements))
                    continue;

                State.unlockedAchievements.Add(achievement.id);
                ApplyEffects(achievement.rewards, club);
            }
        }

        public CareerMissionTreeDef GetPlayerMissionTree() =>
            CareerMissionRules.FindTreeForClub(_missionTrees.Values, State.playerClubId);

        public CareerMissionNodeStatus GetMissionNodeStatus(string nodeId)
        {
            CareerMissionTreeDef tree = GetPlayerMissionTree();
            CareerMissionNodeDef node = CareerMissionRules.FindNode(tree, nodeId);
            if (tree == null || node == null || !State.TryGetClub(State.playerClubId, out ClubState club))
                return CareerMissionNodeStatus.Locked;
            CareerMissionRules.EnsureProgress(State);
            return CareerMissionRules.GetStatus(tree, node, State.missionProgress, State, club, _competitions);
        }

        public bool TryCompleteMissionNode(string nodeId, out string failReason)
        {
            failReason = null;
            CareerMissionTreeDef tree = GetPlayerMissionTree();
            if (tree == null)
            {
                failReason = "此俱樂部尚無任務樹。";
                return false;
            }

            CareerMissionNodeDef node = CareerMissionRules.FindNode(tree, nodeId);
            if (node == null)
            {
                failReason = "找不到任務節點。";
                return false;
            }

            if (!State.TryGetClub(State.playerClubId, out ClubState club))
            {
                failReason = "找不到俱樂部狀態。";
                return false;
            }

            CareerMissionRules.EnsureProgress(State);
            CareerMissionNodeStatus status = CareerMissionRules.GetStatus(
                tree, node, State.missionProgress, State, club, _competitions);
            if (status != CareerMissionNodeStatus.Completable)
            {
                failReason = status == CareerMissionNodeStatus.BlockedByExclusive
                    ? "此路線已關閉。"
                    : status == CareerMissionNodeStatus.Completed
                        ? "任務已完成。"
                        : status == CareerMissionNodeStatus.Locked
                            ? "尚未解鎖。"
                            : "條件尚未達成。";
                return false;
            }

            ApplyEffects(node.effects, club);
            ApplyMissionRosterOps(node.rosterOps);
            CareerMissionRules.MarkCompleted(State.missionProgress, node.id);
            return true;
        }

        private void ApplyMissionRosterOps(CareerRosterOpDef[] ops)
        {
            if (ops == null || ops.Length == 0)
                return;
            foreach (CareerRosterOpDef op in ops)
            {
                if (op == null || string.IsNullOrWhiteSpace(op.type))
                    continue;
                TryApplyHistoricalRosterOp(op, null, allowSetSquadOnPlayerClub: true);
            }
        }

        private void ApplyEffects(CareerEffectDef[] effects, ClubState club)
        {
            if (effects == null || club == null)
                return;

            foreach (CareerEffectDef effect in effects)
            {
                if (effect == null || string.IsNullOrWhiteSpace(effect.type))
                    continue;
                switch (effect.type.Trim().ToLowerInvariant())
                {
                    case "set_flag":
                        CareerFlagRules.SetFlag(
                            State.flags, effect.key, club.clubId, State.clockHour, effect.durationDays);
                        // Club-scoped crises must not also stamp a global flag.
                        if (effect.durationDays == 0 &&
                            !string.Equals(
                                effect.key,
                                ClubCrisisRules.DynastyInternalFractureFlag,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            CareerFlagRules.SetFlag(
                                State.flags, effect.key, CareerFlagRules.GlobalScope, State.clockHour, 0);
                        }
                        break;
                    case "clear_flag":
                        CareerFlagRules.ClearFlag(State.flags, effect.key, club.clubId);
                        CareerFlagRules.ClearFlag(State.flags, effect.key, CareerFlagRules.GlobalScope);
                        break;
                    case "add_cash":
                        club.cash += effect.value;
                        if (effect.value != 0)
                            AddLedger(club.clubId, "event", effect.value, effect.key ?? "scripted", "事件效果");
                        break;
                    case "add_fame":
                        club.fame = Math.Max(0, Math.Min(100, club.fame + effect.value));
                        break;
                    case "add_exposure":
                        club.exposure = Math.Max(0, Math.Min(100, club.exposure + effect.value));
                        ClubCrisisRules.ClampExposureIfCapped(State, club);
                        break;
                    case "add_roster_pressure":
                        ApplyRosterPressure(club.clubId, effect.value);
                        break;
                    case "add_roster_fame":
                        ApplyRosterFame(club.clubId, effect.value);
                        break;
                    case "add_role_map_proficiency":
                        ApplyRoleMapProficiency(effect.key, effect.value);
                        break;
                    case "add_board":
                        ClubEstateSatisfactionRules.ApplyDelta(
                            club, ClubEstateId.Board, effect.value);
                        break;
                    case "add_estate":
                        if (ClubEstateSatisfactionRules.TryParseEstateId(effect.key, out ClubEstateId estateId))
                            ClubEstateSatisfactionRules.ApplyDelta(club, estateId, effect.value);
                        break;
                    case "add_fans":
                        club.fans = Math.Max(0, club.fans + effect.value);
                        break;
                    case "unlock_achievement":
                        if (CareerDifficultyRules.AchievementsEligible(State.difficulty) &&
                            !string.IsNullOrWhiteSpace(effect.key) &&
                            !State.unlockedAchievements.Contains(effect.key, StringComparer.OrdinalIgnoreCase))
                            State.unlockedAchievements.Add(effect.key);
                        break;
                    case "log":
                        AddEvent(CareerEventType.ScriptedEvent, club.clubId, effect.key,
                            string.IsNullOrWhiteSpace(effect.key) ? "事件紀錄" : effect.key);
                        break;
                    case "game_version_update":
                        ClubCoachStaffRules.ApplyGameVersionUpdate(State);
                        AddEvent(CareerEventType.ScriptedEvent, club.clubId, "game_version_update",
                            "遊戲版本更新：分析師數據積累下降");
                        break;
                }
            }
        }

        private void ApplyRosterPressure(string clubId, int delta)
        {
            if (State.players == null || string.IsNullOrWhiteSpace(clubId) || delta == 0)
                return;
            foreach (PlayerCareerState player in State.players)
            {
                if (player == null ||
                    !string.Equals(player.clubId, clubId, StringComparison.OrdinalIgnoreCase))
                    continue;
                player.pressure = Math.Max(0, Math.Min(100, player.pressure + delta));
            }
        }

        private void ApplyRosterFame(string clubId, int delta)
        {
            if (State.players == null || string.IsNullOrWhiteSpace(clubId) || delta == 0)
                return;
            foreach (PlayerCareerState player in State.players)
            {
                if (player == null ||
                    !string.Equals(player.clubId, clubId, StringComparison.OrdinalIgnoreCase))
                    continue;
                player.fame = Math.Max(0, Math.Min(100, player.fame + delta));
            }
        }

        private void ApplyGlobalEffects(CareerEffectDef[] effects)
        {
            if (effects == null)
                return;
            foreach (CareerEffectDef effect in effects)
            {
                if (effect == null || string.IsNullOrWhiteSpace(effect.type))
                    continue;
                string type = effect.type.Trim();
                if (string.Equals(type, "add_role_map_proficiency", StringComparison.OrdinalIgnoreCase))
                    ApplyRoleMapProficiency(effect.key, effect.value);
                else if (string.Equals(type, "set_flag", StringComparison.OrdinalIgnoreCase))
                {
                    CareerFlagRules.SetFlag(
                        State.flags, effect.key, CareerFlagRules.GlobalScope, State.clockHour,
                        effect.durationDays);
                }
                else if (string.Equals(type, "clear_flag", StringComparison.OrdinalIgnoreCase))
                {
                    CareerFlagRules.ClearFlag(State.flags, effect.key, CareerFlagRules.GlobalScope);
                }
            }
        }

        /// <summary>
        /// key format: Role:MapId (e.g. Awper:Dust II). Applies signed map proficiency delta
        /// to every player whose primary role matches.
        /// </summary>
        private void ApplyRoleMapProficiency(string key, int delta)
        {
            if (State.players == null || string.IsNullOrWhiteSpace(key) || delta == 0)
                return;

            int sep = key.IndexOf(':');
            if (sep <= 0 || sep >= key.Length - 1)
                return;

            string role = key.Substring(0, sep).Trim();
            string mapId = key.Substring(sep + 1).Trim();
            if (string.IsNullOrWhiteSpace(role) || string.IsNullOrWhiteSpace(mapId))
                return;

            foreach (PlayerCareerState player in State.players)
            {
                if (player == null || string.IsNullOrWhiteSpace(player.playerId))
                    continue;
                if (_playerDefinitions == null ||
                    !_playerDefinitions.TryGetValue(player.playerId, out CareerPlayerDefinition definition))
                    continue;
                if (!string.Equals(definition.role, role, StringComparison.OrdinalIgnoreCase))
                    continue;
                DevelopmentRules.ApplyMapProficiencyDelta(player, mapId, delta);
            }
        }

        private void AddEvent(
            CareerEventType type,
            string clubId,
            string subjectId,
            string message,
            ScriptedEventTier eventTier = ScriptedEventTier.Unset,
            bool publicVisible = false,
            string title = null)
        {
            string headline = !string.IsNullOrWhiteSpace(title) ? title : message;
            State.events.Add(new CareerEvent
            {
                id = "event_" + State.year + "_" + State.clockHour + "_" + (++_sequence),
                type = type,
                year = State.year,
                clockHour = State.clockHour,
                clubId = clubId,
                subjectId = subjectId,
                title = headline,
                message = message ?? string.Empty,
                eventTier = eventTier,
                publicVisible = publicVisible
            });
        }

        private static int StablePercent(int seed, string text)
        {
            unchecked
            {
                int hash = seed;
                foreach (char c in text ?? string.Empty) hash = hash * 31 + c;
                return Math.Abs(hash % 100);
            }
        }
    }
}
