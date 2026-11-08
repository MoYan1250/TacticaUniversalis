using System;
using System.Collections.Generic;
using System.Linq;
using TacticaUniversalis.Domain;

namespace TacticaUniversalis.Application
{
    public sealed partial class CareerSession
    {
        public bool IsRmrQualifiedForCompetition(string competitionId)
        {
            if (!_competitions.TryGetValue(competitionId, out CompetitionDefinition competition) ||
                !StockholmMajorRules.UsesRmrQualification(competition))
                return false;
            List<MajorQualifiedSeat> seats = StockholmMajorRules.BuildQualifiedSeats(State.rmrRankings);
            return StockholmMajorRules.IsClubQualified(seats, State.playerClubId);
        }

        public RmrMajorStage GetPlayerMajorSeatStage(string competitionId)
        {
            if (!_competitions.TryGetValue(competitionId, out CompetitionDefinition competition) ||
                !StockholmMajorRules.UsesRmrQualification(competition))
                return RmrMajorStage.None;
            List<MajorQualifiedSeat> seats = StockholmMajorRules.BuildQualifiedSeats(State.rmrRankings);
            return StockholmMajorRules.SeatStage(seats, State.playerClubId);
        }

        private void EnsureRmrQualifiedMajorField(CompetitionDefinition competition)
        {
            if (competition == null)
                return;

            RefreshRmrRankings();
            List<MajorQualifiedSeat> seats = StockholmMajorRules.BuildQualifiedSeats(State.rmrRankings);
            List<string> legends = StockholmMajorRules.LegendsClubIds(seats);
            List<string> challengers = StockholmMajorRules.ChallengersClubIds(seats);

            FillMajorStageSlots(legends, 8, competition, preferStronger: true);
            FillMajorStageSlots(challengers, 16, competition, preferStronger: true);

            HashSet<string> desired = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string id in legends.Take(8))
                desired.Add(id);
            foreach (string id in challengers.Take(16))
                desired.Add(id);

            foreach (string clubId in desired)
            {
                // 玩家即使取得 RMR 席位也須自行報名，不自動鎖定入場。
                if (string.Equals(clubId, State.playerClubId, StringComparison.OrdinalIgnoreCase))
                    continue;
                RegisterClubForCompetition(competition.id, clubId);
            }

            // Drop accidental world-rank registrations that are not RMR-qualified once lock happens.
            foreach (CompetitionEntry entry in State.competitionEntries
                         .Where(x => string.Equals(x.competitionId, competition.id, StringComparison.OrdinalIgnoreCase) &&
                                     x.registered && !x.withdrawn)
                         .ToList())
            {
                if (!desired.Contains(entry.clubId))
                {
                    entry.registered = false;
                    entry.withdrawn = true;
                    entry.withdrawnHour = State.clockHour;
                    entry.withdrawReason = CompetitionWithdrawReason.Voluntary;
                }
            }
        }

        private void FillMajorStageSlots(
            List<string> stageClubs, int target, CompetitionDefinition competition, bool preferStronger)
        {
            if (stageClubs.Count >= target)
                return;

            HashSet<string> taken = new HashSet<string>(stageClubs, StringComparer.OrdinalIgnoreCase);
            IEnumerable<string> fillers = State.rankings
                .OrderBy(x => x.rank)
                .Select(x => x.clubId)
                .Where(id => !string.IsNullOrWhiteSpace(id) &&
                             !taken.Contains(id) &&
                             !string.Equals(id, State.playerClubId, StringComparison.OrdinalIgnoreCase));
            if (!preferStronger)
                fillers = fillers.Reverse();

            foreach (string clubId in fillers)
            {
                if (stageClubs.Count >= target)
                    break;
                if (CompetitionTravelRules.RequiresTravel(competition) &&
                    State.TryGetClub(clubId, out ClubState club) &&
                    club != null)
                {
                    CompetitionTravelEstimate travel = BuildTravelEstimateForClub(
                        competition, clubId, club.cash, preferComfort: false);
                    if (club.cash < competition.entryFee + travel.clubOutOfPocket)
                        continue;
                }

                stageClubs.Add(clubId);
                taken.Add(clubId);
            }
        }

        private void BeginStockholmMajor(CompetitionDefinition competition, List<string> entrants)
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

            RefreshRmrRankings();
            List<MajorQualifiedSeat> seats = StockholmMajorRules.BuildQualifiedSeats(State.rmrRankings);
            List<string> legends = StockholmMajorRules.LegendsClubIds(seats)
                .Where(id => entrants.Contains(id, StringComparer.OrdinalIgnoreCase))
                .ToList();
            List<string> challengers = StockholmMajorRules.ChallengersClubIds(seats)
                .Where(id => entrants.Contains(id, StringComparer.OrdinalIgnoreCase))
                .ToList();

            // Entrants registered via RMR lock may include fallback fillers already counted in lists.
            foreach (string id in entrants)
            {
                if (legends.Contains(id, StringComparer.OrdinalIgnoreCase) ||
                    challengers.Contains(id, StringComparer.OrdinalIgnoreCase))
                    continue;
                if (legends.Count < 8)
                    legends.Add(id);
                else
                    challengers.Add(id);
            }

            FillMajorStageSlots(legends, 8, competition, preferStronger: true);
            FillMajorStageSlots(challengers, 16, competition, preferStronger: true);
            legends = legends.Take(8).ToList();
            challengers = challengers.Take(16).ToList();

            State.competitionRun = new CareerCompetitionRunState
            {
                competitionId = competition.id,
                structureId = StockholmMajorRules.StructureId,
                majorStage = MajorStageKind.ChallengersSwiss,
                legendsSeedClubs = legends,
                swissStandings = StockholmMajorRules.CreateSwissStandings(challengers),
                swissPairings = new List<MajorSwissPairing>(),
                swissPairIndex = 0,
                swissRoundIndex = 0,
                majorPlacements = new List<MajorPlacementRecord>(),
                currentRoundClubs = new List<string>(),
                roundWinners = new List<string>(),
                pairIndex = 0,
                roundIndex = 0
            };

            EnsureBracketArchive(competition.id, legends.Concat(challengers).ToList());
            AdvanceStockholmMajor(competition);
        }

        private void AdvanceStockholmMajor(CompetitionDefinition competition)
        {
            CareerCompetitionRunState run = State.competitionRun;
            if (run == null ||
                !string.Equals(run.structureId, StockholmMajorRules.StructureId, StringComparison.OrdinalIgnoreCase))
                return;

            while (true)
            {
                if (run.majorStage == MajorStageKind.ChallengersSwiss ||
                    run.majorStage == MajorStageKind.LegendsSwiss)
                {
                    if (AdvanceStockholmSwiss(competition, run))
                        return;
                    continue;
                }

                if (run.majorStage == MajorStageKind.ChampionsKnockout)
                {
                    AdvanceInteractiveCompetition(competition);
                    return;
                }

                return;
            }
        }

        /// <summary>Returns true when waiting on a player match.</summary>
        private bool AdvanceStockholmSwiss(CompetitionDefinition competition, CareerCompetitionRunState run)
        {
            int advanceTarget = StockholmMajorRules.ChampionsField;
            while (true)
            {
                if (StockholmMajorRules.SwissStageComplete(run.swissStandings, advanceTarget))
                {
                    FinishStockholmSwissStage(competition, run);
                    return false;
                }

                if (run.swissPairings == null || run.swissPairings.Count == 0)
                {
                    int seed = State.seed + State.clockHour * 17 + run.swissRoundIndex * 131 +
                               (int)run.majorStage * 997;
                    run.swissPairings = StockholmMajorRules.BuildSwissPairings(run.swissStandings, seed);
                    run.swissPairIndex = 0;
                    if (run.swissPairings.Count == 0)
                    {
                        FinishStockholmSwissStage(competition, run);
                        return false;
                    }
                }

                while (run.swissPairIndex < run.swissPairings.Count)
                {
                    MajorSwissPairing pairing = run.swissPairings[run.swissPairIndex];
                    if (pairing.completed)
                    {
                        run.swissPairIndex++;
                        continue;
                    }

                    string clubA = pairing.clubAId;
                    string clubB = pairing.clubBId;
                    bool playerMatch =
                        string.Equals(clubA, State.playerClubId, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(clubB, State.playerClubId, StringComparison.OrdinalIgnoreCase);

                    if (playerMatch && CanPlayInteractiveOfficialMatch(
                            string.Equals(clubA, State.playerClubId, StringComparison.OrdinalIgnoreCase) ? clubB : clubA))
                    {
                        run.pendingOpponentClubId =
                            string.Equals(clubA, State.playerClubId, StringComparison.OrdinalIgnoreCase) ? clubB : clubA;
                        run.pendingMatchSeed = StablePercent(
                            State.seed + State.clockHour,
                            competition.id + clubA + clubB + run.swissRoundIndex + run.majorStage) * 7919;
                        // Map currentRoundClubs so RecordBracketMatchFromCurrentPair still works.
                        run.currentRoundClubs = new List<string> { clubA, clubB };
                        run.pairIndex = 0;
                        ScheduleInteractivePlayerMatch(run);
                        return true;
                    }

                    string winner = SimulateKnockoutPair(competition, clubA, clubB);
                    string loser = string.Equals(winner, clubA, StringComparison.OrdinalIgnoreCase) ? clubB : clubA;
                    pairing.completed = true;
                    pairing.winnerClubId = winner;
                    pairing.scoreA = string.Equals(winner, clubA, StringComparison.OrdinalIgnoreCase) ? 1 : 0;
                    pairing.scoreB = string.Equals(winner, clubB, StringComparison.OrdinalIgnoreCase) ? 1 : 0;
                    StockholmMajorRules.ApplySwissResult(run.swissStandings, winner, loser, advanceTarget);
                    run.swissPairIndex++;
                }

                run.swissRoundIndex++;
                run.swissPairings = new List<MajorSwissPairing>();
                run.swissPairIndex = 0;
            }
        }

        private void FinishStockholmSwissStage(CompetitionDefinition competition, CareerCompetitionRunState run)
        {
            List<string> advanced = StockholmMajorRules.AdvancedClubs(run.swissStandings);
            List<string> eliminated = StockholmMajorRules.EliminatedClubs(run.swissStandings);

            if (run.majorStage == MajorStageKind.ChallengersSwiss)
            {
                for (int i = 0; i < eliminated.Count; i++)
                    SetMajorPlacement(run, eliminated[i], StockholmMajorRules.ChallengersEliminationPlacement(i));

                // If player was eliminated here, record placement but keep simulating the Major.
                if (run.playerPlacement <= 0)
                {
                    MajorPlacementRecord playerPlace = run.majorPlacements.FirstOrDefault(x =>
                        string.Equals(x.clubId, State.playerClubId, StringComparison.OrdinalIgnoreCase));
                    if (playerPlace != null)
                        run.playerPlacement = playerPlace.placement;
                }

                List<string> legendsField = run.legendsSeedClubs
                    .Concat(advanced)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(16)
                    .ToList();
                while (legendsField.Count < 16)
                {
                    string filler = State.rankings
                        .Select(x => x.clubId)
                        .FirstOrDefault(id =>
                            !string.Equals(id, State.playerClubId, StringComparison.OrdinalIgnoreCase) &&
                            !legendsField.Contains(id, StringComparer.OrdinalIgnoreCase));
                    if (string.IsNullOrEmpty(filler))
                        break;
                    legendsField.Add(filler);
                }

                run.majorStage = MajorStageKind.LegendsSwiss;
                run.swissStandings = StockholmMajorRules.CreateSwissStandings(legendsField);
                run.swissPairings = new List<MajorSwissPairing>();
                run.swissPairIndex = 0;
                run.swissRoundIndex = 0;
                return;
            }

            if (run.majorStage == MajorStageKind.LegendsSwiss)
            {
                for (int i = 0; i < eliminated.Count; i++)
                    SetMajorPlacement(run, eliminated[i], StockholmMajorRules.LegendsEliminationPlacement(i));

                if (run.playerPlacement <= 0)
                {
                    MajorPlacementRecord playerPlace = run.majorPlacements.FirstOrDefault(x =>
                        string.Equals(x.clubId, State.playerClubId, StringComparison.OrdinalIgnoreCase));
                    if (playerPlace != null)
                        run.playerPlacement = playerPlace.placement;
                }

                List<string> champions = advanced.Take(8).ToList();
                while (champions.Count < 2 && eliminated.Count > 0)
                {
                    champions.Add(eliminated[0]);
                    eliminated.RemoveAt(0);
                }

                run.majorStage = MajorStageKind.ChampionsKnockout;
                run.swissStandings = new List<MajorSwissStanding>();
                run.swissPairings = new List<MajorSwissPairing>();
                List<string> bracket = BuildKnockoutBracket(champions, State.playerClubId);
                EnsureBracketArchive(competition.id, bracket);
                run.currentRoundClubs = bracket;
                run.roundWinners = new List<string>();
                run.pairIndex = 0;
                run.roundIndex = 0;
            }
        }

        private static void SetMajorPlacement(CareerCompetitionRunState run, string clubId, int placement)
        {
            if (run == null || string.IsNullOrWhiteSpace(clubId) || placement <= 0)
                return;
            MajorPlacementRecord existing = run.majorPlacements.FirstOrDefault(x =>
                string.Equals(x.clubId, clubId, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.placement = placement;
                return;
            }

            run.majorPlacements.Add(new MajorPlacementRecord { clubId = clubId, placement = placement });
        }

        private bool ContinueAfterOfficialMajorMatch(CompetitionDefinition competition, CareerMatchSummary summary)
        {
            CareerCompetitionRunState run = State.competitionRun;
            if (run == null)
                return true;

            if (run.majorStage == MajorStageKind.ChampionsKnockout)
                return ContinueAfterOfficialPlayerMatchCore(competition, summary);

            bool playerWon = string.Equals(summary.winnerClubId, State.playerClubId, StringComparison.OrdinalIgnoreCase);
            string winner = summary.winnerClubId;
            string loser = summary.loserClubId;
            if (run.swissPairIndex < run.swissPairings.Count)
            {
                MajorSwissPairing pairing = run.swissPairings[run.swissPairIndex];
                pairing.completed = true;
                pairing.winnerClubId = winner;
                pairing.scoreA = string.Equals(winner, pairing.clubAId, StringComparison.OrdinalIgnoreCase)
                    ? summary.winnerScore
                    : summary.loserScore;
                pairing.scoreB = string.Equals(winner, pairing.clubBId, StringComparison.OrdinalIgnoreCase)
                    ? summary.winnerScore
                    : summary.loserScore;
            }

            StockholmMajorRules.ApplySwissResult(
                run.swissStandings, winner, loser, StockholmMajorRules.ChampionsField);
            run.swissPairIndex++;
            run.playerInteractiveMatchesResolved++;

            if (!playerWon)
            {
                MajorSwissStanding standing = run.swissStandings.FirstOrDefault(x =>
                    string.Equals(x.clubId, State.playerClubId, StringComparison.OrdinalIgnoreCase));
                if (standing != null && standing.eliminated && run.playerPlacement <= 0)
                    run.playerPlacement = run.majorStage == MajorStageKind.ChallengersSwiss ? 20 : 12;
            }

            AdvanceStockholmMajor(competition);
            return true;
        }

        private bool ContinueAfterOfficialPlayerMatchCore(CompetitionDefinition competition, CareerMatchSummary summary)
        {
            bool playerWon = string.Equals(summary.winnerClubId, State.playerClubId, StringComparison.OrdinalIgnoreCase);
            State.competitionRun.roundWinners.Add(playerWon ? State.playerClubId : summary.winnerClubId);
            State.competitionRun.pairIndex++;

            bool stockholm = string.Equals(
                State.competitionRun.structureId,
                StockholmMajorRules.StructureId,
                StringComparison.OrdinalIgnoreCase);

            if (!playerWon)
            {
                State.competitionRun.playerPlacement = PlacementForRoundLoss(
                    State.competitionRun.roundIndex, State.competitionRun.currentRoundClubs.Count);
                if (stockholm)
                {
                    int teams = State.competitionRun.currentRoundClubs.Count;
                    if (teams <= 2) State.competitionRun.playerPlacement = 2;
                    else if (teams <= 4) State.competitionRun.playerPlacement = 3;
                    else State.competitionRun.playerPlacement = 5;
                    SetMajorPlacement(
                        State.competitionRun, State.playerClubId, State.competitionRun.playerPlacement);
                    // Keep run alive so remaining AI champions matches finish.
                    AdvanceInteractiveCompetition(competition);
                    return true;
                }

                CompleteInteractiveCompetitionAwards(competition);
                State.competitionRun = null;
                TryStartNextQueuedInteractiveCompetition();
                return true;
            }

            State.competitionRun.playerInteractiveMatchesResolved++;
            AdvanceInteractiveCompetition(competition);
            return true;
        }

        private void FinalizeStockholmMajorAwards(CompetitionDefinition competition, CareerCompetitionRunState run)
        {
            if (competition == null || run == null)
                return;

            // Champion / runner-up from knockout archive if available.
            if (run.playerPlacement == 1)
                SetMajorPlacement(run, State.playerClubId, 1);

            List<string> entrants = CompetitionRegistrationRules.RegisteredClubIds(State, competition.id).ToList();
            if (entrants.Count == 0)
                entrants = run.legendsSeedClubs
                    .Concat(run.swissStandings.Select(x => x.clubId))
                    .Concat(run.currentRoundClubs)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

            // Fill missing champion-stage placements from remaining incomplete entries.
            foreach (string clubId in entrants)
            {
                if (run.majorPlacements.Any(x =>
                        string.Equals(x.clubId, clubId, StringComparison.OrdinalIgnoreCase)))
                    continue;

                int placement;
                if (string.Equals(clubId, State.playerClubId, StringComparison.OrdinalIgnoreCase) &&
                    run.playerPlacement > 0)
                    placement = run.playerPlacement;
                else
                    placement = SimulatePlacementForClub(competition, clubId, entrants);

                placement = Math.Max(1, Math.Min(24, placement));
                SetMajorPlacement(run, clubId, placement);
            }

            foreach (MajorPlacementRecord record in run.majorPlacements)
            {
                CompetitionEntry existing = State.competitionEntries.FirstOrDefault(x =>
                    x.competitionId == competition.id &&
                    string.Equals(x.clubId, record.clubId, StringComparison.OrdinalIgnoreCase));
                if (existing != null && existing.completed)
                    continue;
                AwardCompetitionPlacement(competition, record.clubId, record.placement, 24);
            }
        }
    }
}
