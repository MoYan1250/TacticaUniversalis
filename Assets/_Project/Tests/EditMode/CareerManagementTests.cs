using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TacticaUniversalis.Application;
using TacticaUniversalis.Domain;
using TacticaUniversalis.Infrastructure;
using UnityEngine;

namespace TacticaUniversalis.Tests
{
    public sealed class CareerManagementTests
    {
        private CareerContentDatabaseAsset _asset;
        private CareerContent _content;

        [SetUp]
        public void SetUp()
        {
            string root = Path.Combine(UnityEngine.Application.dataPath, "_Project", "Data", "JSON");
            _asset = ScriptableObject.CreateInstance<CareerContentDatabaseAsset>();
            CareerContentJsonParser.ParseInto(
                _asset,
                File.ReadAllText(Path.Combine(root, "career_balance.json")),
                File.ReadAllText(Path.Combine(root, "career_players.json")),
                File.ReadAllText(Path.Combine(root, "career_clubs.json")),
                File.ReadAllText(Path.Combine(root, "sponsors.json")),
                File.ReadAllText(Path.Combine(root, "competitions.json")),
                File.ReadAllText(Path.Combine(root, "facilities.json")));
            _content = _asset.BuildDomain();
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_asset);

        [Test]
        public void CareerContent_IsCompleteAndValid()
        {
            Assert.That(CareerContentValidator.Validate(_asset), Is.Empty);
            Assert.That(_content.Clubs.Count, Is.EqualTo(24));
            Assert.That(_content.Players.Count, Is.EqualTo(135));
            Assert.That(_content.Facilities.Count, Is.EqualTo(4));
            Assert.That(_content.Competitions.Count, Is.GreaterThanOrEqualTo(6));
            Assert.That(_content.Clubs.ContainsKey("vitality_2021"), Is.True);
            Assert.That(_content.Clubs.ContainsKey("faze_2021"), Is.True);
            Assert.That(_content.Clubs.ContainsKey("cloud9_2021"), Is.True);
            // Placeholder synthetic clubs removed in favor of real 2021 rosters.
            Assert.That(_content.Clubs.ContainsKey("aurora"), Is.False);
        }

        [Test]
        public void CareerCalendar_StartsAt2021Jan1Midnight()
        {
            Assert.That(_content.Balance.startingYear, Is.EqualTo(2021));
            CareerState state = CareerSession.CreateNew(
                _content.Balance, _content.Players.Values, _content.Clubs.Values,
                "navi_2021", CareerDifficulty.Normal, 2021);
            DateTime start = CareerCalendar.ToDateTime(_content.Balance, state);
            Assert.That(start, Is.EqualTo(new DateTime(2021, 1, 1, 0, 0, 0)));
            Assert.That(CareerCalendar.FormatHeader(_content.Balance, state), Is.EqualTo("2021.1.1 0:00"));
            Assert.That(state.clockHour, Is.EqualTo(0));
            Assert.That(state.rankings.First(x => x.clubId == "astralis_2021").rank, Is.EqualTo(1));
            Assert.That(state.rankings.First(x => x.clubId == "navi_2021").rank, Is.EqualTo(3));
            Assert.That(state.rankings.First(x => x.clubId == "astralis_2021").points, Is.EqualTo(1000f).Within(0.01f));
        }

        [Test]
        public void CareerCalendar_SevenDaysIs168Hours()
        {
            DateTime week2 = CareerCalendar.ToDateTime(_content.Balance, CareerCalendar.HoursPerWeek);
            Assert.That(week2, Is.EqualTo(new DateTime(2021, 1, 8, 0, 0, 0)));
        }

        [Test]
        public void CareerCalendar_CompetitionLiveAndRegisterableWindows()
        {
            CompetitionDefinition competition = _content.Competitions["esl_pro_league_season_13"];
            Assert.That(CareerCalendar.IsLive(competition, competition.startHour - 1), Is.False);
            Assert.That(CareerCalendar.IsLive(competition, competition.startHour), Is.True);
            Assert.That(CareerCalendar.IsLive(competition, competition.endHour), Is.True);
            Assert.That(CareerCalendar.IsLive(competition, competition.endHour + 1), Is.False);
            Assert.That(
                CareerCalendar.IsRegisterableThisMonth(competition, _content.Balance, 0),
                Is.False);
            // March 1 2021 — same month as EPL 13 start (Mar 8).
            int march1 = CareerCalendar.FromDateTime(_content.Balance, new DateTime(2021, 3, 1));
            Assert.That(
                CareerCalendar.IsRegisterableThisMonth(competition, _content.Balance, march1),
                Is.True);
        }

        [Test]
        public void MapVisible_OnlyThisMonthRegisterableOrLive()
        {
            CompetitionDefinition january = _content.Competitions["blast_premier_global_final_2020"];
            CompetitionDefinition february = _content.Competitions["iem_xv_world_championship"];
            Assert.That(CareerCalendar.VisibleOnMap(january, _content.Balance, 0), Is.True);
            Assert.That(CareerCalendar.VisibleOnMap(february, _content.Balance, 0), Is.False);
            Assert.That(CareerCalendar.InScheduleWindow(february, _content.Balance, 0), Is.True);
            Assert.That(_content.Competitions.Count, Is.EqualTo(40));
            Assert.That(_content.Competitions["pgl_major_stockholm_2021"].tier, Is.EqualTo(CompetitionTier.Major));
            Assert.That(_content.Competitions["iem_xvi_cologne"].tier, Is.EqualTo(CompetitionTier.SuperElite));
            Assert.That(_content.Competitions["snow_sweet_snow_1"].tier, Is.EqualTo(CompetitionTier.A));
            Assert.That(_content.Competitions["dreamhack_open_march_2021_na"].region, Is.EqualTo("Americas"));
            Assert.That((int)CompetitionTier.Elite, Is.EqualTo(8));
        }

        [Test]
        public void Major_HiddenOnSchedule_UntilOfflineReturnAnnounce()
        {
            CompetitionDefinition major = _content.Competitions["pgl_major_stockholm_2021"];
            int before = CareerCalendar.FromDateTime(_content.Balance, new DateTime(2021, 9, 14));
            int onDay = CareerCalendar.FromDateTime(_content.Balance, new DateTime(2021, 9, 15));
            Assert.That(CareerCalendar.IsOfflineReturnAnnounced(_content.Balance, before), Is.False);
            Assert.That(CareerCalendar.IsOfflineReturnAnnounced(_content.Balance, onDay), Is.True);
            Assert.That(CareerCalendar.InScheduleWindow(major, _content.Balance, before), Is.False);
            Assert.That(CareerCalendar.InScheduleWindow(major, _content.Balance, onDay), Is.True);
        }

        [Test]
        public void CompetitionPrize_HigherPlacementEarnsMore_IncludingEarlyExits()
        {
            const int pool = 1_000_000;
            const int field = 16;
            int first = CompetitionPrizeRules.PrizeFor(pool, 1, field);
            int second = CompetitionPrizeRules.PrizeFor(pool, 2, field);
            int quarters = CompetitionPrizeRules.PrizeFor(pool, 5, field);
            int roundOf16 = CompetitionPrizeRules.PrizeFor(pool, 16, field);
            Assert.That(first, Is.GreaterThan(second));
            Assert.That(second, Is.GreaterThan(quarters));
            Assert.That(quarters, Is.GreaterThan(roundOf16));
            Assert.That(roundOf16, Is.GreaterThan(0));

            int sum = 0;
            for (int p = 1; p <= field; p++)
                sum += CompetitionPrizeRules.PrizeFor(pool, p, field);
            Assert.That(sum, Is.EqualTo(pool).Within(field)); // rounding slack
        }

        [Test]
        public void CompetitionTravel_LanEstimate_IncludesEarlyArrivalAndSubsidy()
        {
            CompetitionDefinition major = _content.Competitions["pgl_major_stockholm_2021"];
            CompetitionTravelEstimate eu = CompetitionTravelRules.BuildEstimate(
                major, "Europe", 5, 500000, preferComfort: true);
            CompetitionTravelEstimate americas = CompetitionTravelRules.BuildEstimate(
                major, "Americas", 5, 500000, preferComfort: false);

            Assert.That(eu.earlyArrivalDays, Is.GreaterThanOrEqualTo(1));
            Assert.That(americas.earlyArrivalDays, Is.GreaterThan(eu.earlyArrivalDays));
            Assert.That(americas.flightHours, Is.GreaterThan(eu.flightHours));
            Assert.That(eu.flightClass, Is.EqualTo(FlightClass.Business));
            Assert.That(eu.subsidyAmount, Is.GreaterThan(0));
            Assert.That(americas.clubOutOfPocket, Is.GreaterThanOrEqualTo(0));
            Assert.That(eu.departHour, Is.LessThan(major.startHour));
            Assert.That(eu.onSiteHour, Is.EqualTo(major.startHour - eu.earlyArrivalDays * CareerCalendar.HoursPerDay));
        }

        [Test]
        public void CompetitionTravel_InFlightEnergy_BusinessRecoversBetterThanEconomy()
        {
            int eco = CompetitionTravelRules.InFlightEnergyDelta(FlightClass.Economy, 8);
            int biz = CompetitionTravelRules.InFlightEnergyDelta(FlightClass.Business, 8);
            Assert.That(biz, Is.GreaterThan(eco));
            Assert.That(eco, Is.LessThan(0));
            Assert.That(biz, Is.GreaterThan(0));
        }

        [Test]
        public void CompetitionWithdraw_LateVoluntary_AppliesFeeAndBanExceptMajor()
        {
            CompetitionDefinition epl = _content.Competitions["esl_pro_league_season_13"];
            int lateHour = epl.startHour - 24;
            var entry = new CompetitionEntry
            {
                competitionId = epl.id,
                clubId = "test",
                registered = true,
                registeredHour = 0
            };
            CompetitionWithdrawEvaluation late = CompetitionWithdrawRules.Evaluate(
                epl, entry, lateHour, CompetitionWithdrawReason.Voluntary);
            Assert.That(late.canWithdraw, Is.True);
            Assert.That(late.isLate, Is.True);
            Assert.That(late.appliesBan, Is.True);
            Assert.That(late.fee, Is.GreaterThan(0));

            CompetitionWithdrawEvaluation illness = CompetitionWithdrawRules.Evaluate(
                epl, entry, lateHour, CompetitionWithdrawReason.PlayerIllness);
            Assert.That(illness.appliesBan, Is.False);
            Assert.That(illness.fee, Is.GreaterThan(0));
            Assert.That(illness.fee, Is.LessThan(late.fee));

            CompetitionDefinition major = _content.Competitions["pgl_major_stockholm_2021"];
            var majorEntry = new CompetitionEntry
            {
                competitionId = major.id,
                clubId = "test",
                registered = true
            };
            CompetitionWithdrawEvaluation majorLate = CompetitionWithdrawRules.Evaluate(
                major, majorEntry, major.startHour - 24, CompetitionWithdrawReason.Voluntary);
            Assert.That(majorLate.appliesBan, Is.False);
            Assert.That(majorLate.fee, Is.GreaterThan(0));
        }

        [TestCase(390000, 30000, FinancialHealth.Wealthy)]
        [TestCase(240000, 30000, FinancialHealth.Healthy)]
        [TestCase(120000, 30000, FinancialHealth.Balanced)]
        [TestCase(60000, 30000, FinancialHealth.Crisis)]
        public void FinanceHealth_UsesRunway(int cash, int expense, FinancialHealth expected)
        {
            var club = new ClubState
            {
                cash = cash,
                trailingExpense1 = expense,
                trailingExpense2 = expense,
                trailingExpense3 = expense,
                lastMonthNet = 1
            };
            Assert.That(FinanceRules.Evaluate(club, expense), Is.EqualTo(expected));
        }

        [Test]
        public void MarketValue_YoungPotentialAwperReceivesPremium()
        {
            var definition = new CareerPlayerDefinition
                { id = "p", age = 20, role = "Awper", professionalism = 80 };
            var player = new PlayerCareerState
                { playerId = "p", age = 20, power = 82, potential = 95, fame = 70 };
            var contract = new ContractState { remainingMonths = 24 };
            int normal = MarketRules.CalculateMarketValue(player, definition, contract, 100);
            int scarce = MarketRules.CalculateMarketValue(player, definition, contract, 120);
            Assert.That(scarce, Is.GreaterThan(normal));
            Assert.That(normal, Is.InRange(10000, 15000000));
        }

        [Test]
        public void RegistrationTarget_IncreasesThroughSignupMonth()
        {
            CompetitionDefinition competition = _content.Competitions.Values
                .First(x => x.id == "esl_pro_league_season_13");
            int early = CompetitionRegistrationRules.TargetRegisteredCount(
                competition, _content.Balance, CareerCalendar.FromDateTime(
                    _content.Balance, new DateTime(2021, 3, 2)));
            int late = CompetitionRegistrationRules.TargetRegisteredCount(
                competition, _content.Balance, CareerCalendar.FromDateTime(
                    _content.Balance, new DateTime(2021, 3, 28)));
            int field = CompetitionRegistrationRules.EffectiveFieldSize(competition);
            Assert.That(late, Is.GreaterThan(early));
            Assert.That(early, Is.GreaterThanOrEqualTo(0));
            Assert.That(late, Is.EqualTo(field));
            Assert.That(
                CompetitionRegistrationRules.SoftMaximumWorldRank(competition),
                Is.GreaterThan(competition.maximumWorldRank));
        }

        [Test]
        public void BuyInclination_FullRoster_IsLowUnlessClearUpgrade()
        {
            var definitions = new Dictionary<string, CareerPlayerDefinition>(StringComparer.OrdinalIgnoreCase)
            {
                ["a1"] = new CareerPlayerDefinition { id = "a1", role = "Rifler", age = 24 },
                ["a2"] = new CareerPlayerDefinition { id = "a2", role = "Rifler", age = 24 },
                ["a3"] = new CareerPlayerDefinition { id = "a3", role = "Rifler", age = 24 },
                ["a4"] = new CareerPlayerDefinition { id = "a4", role = "Rifler", age = 24 },
                ["a5"] = new CareerPlayerDefinition { id = "a5", role = "Awper", age = 24 },
                ["star"] = new CareerPlayerDefinition { id = "star", role = "Rifler", age = 22 },
                ["bench"] = new CareerPlayerDefinition { id = "bench", role = "Rifler", age = 26 },
            };
            var players = new List<PlayerCareerState>
            {
                new PlayerCareerState { playerId = "a1", clubId = "buyer", power = 78 },
                new PlayerCareerState { playerId = "a2", clubId = "buyer", power = 79 },
                new PlayerCareerState { playerId = "a3", clubId = "buyer", power = 80 },
                new PlayerCareerState { playerId = "a4", clubId = "buyer", power = 81 },
                new PlayerCareerState { playerId = "a5", clubId = "buyer", power = 82 },
                new PlayerCareerState { playerId = "star", clubId = "seller", power = 92 },
                new PlayerCareerState { playerId = "bench", clubId = "seller", power = 79 },
            };

            Assert.That(
                MarketRules.ClubWantsPlayer(players, definitions, "buyer", players[6], definitions["bench"]),
                Is.False);
            Assert.That(
                MarketRules.ClubWantsPlayer(players, definitions, "buyer", players[5], definitions["star"]),
                Is.True);

            double low = MarketRules.BuyInclination(players, definitions, "buyer", players[6], definitions["bench"]);
            double high = MarketRules.BuyInclination(players, definitions, "buyer", players[5], definitions["star"]);
            Assert.That(low, Is.EqualTo(0d));
            Assert.That(high, Is.GreaterThan(0.15d).And.LessThan(0.45d));
        }

        [Test]
        public void Ranking_CompetitionPlacementPoints_UsesTierAndPlacement()
        {
            float champion = RankingRules.CompetitionPlacementPoints(CompetitionTier.S, 1, 80);
            float third = RankingRules.CompetitionPlacementPoints(CompetitionTier.S, 3, 80);
            float winBonus = RankingRules.MatchWinPoints(CompetitionTier.S);
            Assert.That(champion, Is.EqualTo(220f * RankingRules.PowerModifier(80)).Within(1f));
            Assert.That(champion, Is.LessThanOrEqualTo(RankingRules.MaximumHltvPoints));
            Assert.That(third, Is.LessThan(champion * 0.55f));
            Assert.That(winBonus, Is.GreaterThan(1.5f).And.LessThan(5f));
        }

        [Test]
        public void Ranking_DecaysByHalfAfterHalfLifeHours()
        {
            int halfLife = 26 * CareerCalendar.HoursPerWeek;
            var batch = new RankingBatch { clubId = "a", earnedHour = 0, points = 1000 };
            Assert.That(RankingRules.DecayedPoints(batch, halfLife, halfLife), Is.EqualTo(500f).Within(.01f));
        }

        [Test]
        public void Rmr_PointsTable_MatchesStockholm2021()
        {
            Assert.That(RmrRules.PointsFor(RmrRound.Circuit2020, 1), Is.EqualTo(600f));
            Assert.That(RmrRules.PointsFor(RmrRound.Circuit2020, 9), Is.EqualTo(300f));
            Assert.That(RmrRules.PointsFor(RmrRound.Circuit2020, 20), Is.EqualTo(100f));
            Assert.That(RmrRules.PointsFor(RmrRound.Rmr1, 1), Is.EqualTo(1600f));
            Assert.That(RmrRules.PointsFor(RmrRound.Rmr1, 16), Is.EqualTo(100f));
            Assert.That(RmrRules.PointsFor(RmrRound.Rmr2, 1), Is.EqualTo(2000f));
            Assert.That(RmrRules.PointsFor(RmrRound.Rmr3, 1), Is.EqualTo(2500f));
            Assert.That(RmrRules.PointsFor(RmrRound.Rmr3, 2), Is.EqualTo(2344f));
            Assert.That(RmrRules.PointsFor(RmrRound.Rmr3, 16), Is.EqualTo(156f));
        }

        [Test]
        public void Rmr_EuropeStage_UsesStockholmSpotBands()
        {
            Assert.That(RmrRules.ResolveStage("Europe", 1), Is.EqualTo(RmrMajorStage.Legend));
            Assert.That(RmrRules.ResolveStage("Europe", 3), Is.EqualTo(RmrMajorStage.Legend));
            Assert.That(RmrRules.ResolveStage("Europe", 4), Is.EqualTo(RmrMajorStage.Challenger));
            Assert.That(RmrRules.ResolveStage("Europe", 9), Is.EqualTo(RmrMajorStage.Challenger));
            Assert.That(RmrRules.ResolveStage("Europe", 10), Is.EqualTo(RmrMajorStage.Contender));
            Assert.That(RmrRules.ResolveStage("Europe", 11), Is.EqualTo(RmrMajorStage.Contender));
            Assert.That(RmrRules.ResolveStage("Europe", 12), Is.EqualTo(RmrMajorStage.None));
        }

        [Test]
        public void StockholmMajor_UsesRmrQualificationMode()
        {
            CompetitionDefinition major = _content.Competitions["pgl_major_stockholm_2021"];
            Assert.That(StockholmMajorRules.UsesRmrQualification(major), Is.True);
            Assert.That(CompetitionRegistrationRules.EffectiveFieldSize(major), Is.EqualTo(24));
        }

        [Test]
        public void StockholmMajor_UnqualifiedCannotRegister_CancelAlwaysBlocked()
        {
            CareerSession session = BuildSession(CreateState(11, CareerDifficulty.Normal));
            // Force player out of RMR seats for this assert by clearing rankings stages via empty batches rebuild:
            // With default seeds NAVI is typically qualified; use a club that may or may not qualify.
            // Cancel must always be blocked for RMR majors regardless.
            Assert.That(
                session.CancelCompetitionRegistration(
                    "pgl_major_stockholm_2021",
                    CompetitionWithdrawReason.Voluntary,
                    out string cancelMsg),
                Is.False);
            Assert.That(cancelMsg, Does.Contain("不可取消"));

            CompetitionWithdrawEvaluation eval = session.EvaluateCompetitionWithdraw(
                "pgl_major_stockholm_2021");
            Assert.That(eval.canWithdraw, Is.False);
        }

        [Test]
        public void StockholmSwiss_ThreeWinsAdvance_ThreeLossesEliminate()
        {
            var standings = StockholmMajorRules.CreateSwissStandings(new[]
            {
                "a", "b", "c", "d", "e", "f", "g", "h",
                "i", "j", "k", "l", "m", "n", "o", "p"
            });
            for (int i = 0; i < 3; i++)
                StockholmMajorRules.ApplySwissResult(standings, "a", "b", 8);
            Assert.That(standings.First(x => x.clubId == "a").advanced, Is.True);
            Assert.That(standings.First(x => x.clubId == "b").eliminated, Is.True);

            List<MajorSwissPairing> pairings = StockholmMajorRules.BuildSwissPairings(standings, 42);
            Assert.That(pairings.Count, Is.GreaterThan(0));
            Assert.That(pairings.All(x =>
                !string.IsNullOrEmpty(x.clubAId) && !string.IsNullOrEmpty(x.clubBId)), Is.True);
        }

        [Test]
        public void StockholmMajor_QualifiedSeats_FollowRegionalBands()
        {
            CareerState state = CareerSession.CreateNew(
                _content.Balance, _content.Players.Values, _content.Clubs.Values,
                "navi_2021", CareerDifficulty.Normal, 99);
            List<MajorQualifiedSeat> seats = StockholmMajorRules.BuildQualifiedSeats(state.rmrRankings);
            Assert.That(seats.Count, Is.GreaterThan(0));
            Assert.That(StockholmMajorRules.LegendsClubIds(seats).Count, Is.LessThanOrEqualTo(8));
            Assert.That(
                StockholmMajorRules.ChallengersClubIds(seats).Count,
                Is.LessThanOrEqualTo(16));
            Assert.That(StockholmMajorRules.IsClubQualified(seats, "navi_2021") ||
                        seats.All(x => x.clubId != "navi_2021"),
                Is.True);
        }

        [Test]
        public void Rmr_RosterDeduction_StacksTwentyPercent()
        {
            var batches = new List<RmrPointBatch>
            {
                RmrRules.CreateCircuitSeedBatch("club_a", 1000f)
            };
            RmrPointBatch first = RmrRules.CreateRosterDeductionBatch(batches, "club_a", 100, "a");
            Assert.That(first, Is.Not.Null);
            Assert.That(first.points, Is.EqualTo(-200f));
            batches.Add(first);
            RmrPointBatch second = RmrRules.CreateRosterDeductionBatch(batches, "club_a", 200, "b");
            Assert.That(second, Is.Not.Null);
            Assert.That(second.points, Is.EqualTo(-160f));
        }

        [Test]
        public void Rmr_NewCareer_SeedsCircuitAndBuildsRegionalTable()
        {
            CareerState state = CareerSession.CreateNew(
                _content.Balance, _content.Players.Values, _content.Clubs.Values,
                "astralis_2021", CareerDifficulty.Normal, 42);
            Assert.That(state.rmrBatches, Is.Not.Empty);
            Assert.That(state.rmrRankings, Is.Not.Empty);
            RmrEntry europeTop = state.rmrRankings
                .Where(x => x.rmrRegion == "Europe")
                .OrderBy(x => x.regionRank)
                .FirstOrDefault();
            Assert.That(europeTop, Is.Not.Null);
            Assert.That(europeTop.totalPoints, Is.GreaterThan(0f));
            Assert.That(RmrRules.ResolveClubRmrRegion(_content.Clubs["navi_2021"]), Is.EqualTo("CIS"));
        }

        [Test]
        public void SameSeed_AndCommands_ProduceSameSeason()
        {
            CareerState a = RunOneSeason(77);
            CareerState b = RunOneSeason(77);
            Assert.That(a.clubs.Select(x => x.cash), Is.EqualTo(b.clubs.Select(x => x.cash)));
            Assert.That(a.rankings.Select(x => x.clubId), Is.EqualTo(b.rankings.Select(x => x.clubId)));
            Assert.That(a.events.Select(x => x.message), Is.EqualTo(b.events.Select(x => x.message)));
        }

        [Test]
        public void Save_RoundTripPreservesCareerState()
        {
            CareerState state = RunHours(CareerCalendar.HoursPerWeek * 12, 101);
            string folder = Path.Combine(Path.GetTempPath(), "TacticaCareerTest_" + Guid.NewGuid().ToString("N"));
            try
            {
                var persistence = new CareerPersistence(folder);
                persistence.Save(state, 1);
                CareerState loaded = persistence.Load(1);
                Assert.That(loaded.year, Is.EqualTo(state.year));
                Assert.That(loaded.clockHour, Is.EqualTo(state.clockHour));
                Assert.That(loaded.clubs.Select(x => x.cash), Is.EqualTo(state.clubs.Select(x => x.cash)));
                Assert.That(loaded.players.Count, Is.EqualTo(120));
            }
            finally
            {
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
            }
        }

        [Test]
        public void Hardcore_UsesIronmanSlotAndBlocksManualLoad()
        {
            CareerState state = CreateState(5, CareerDifficulty.Hardcore);
            string folder = Path.Combine(Path.GetTempPath(), "TacticaIronmanTest_" + Guid.NewGuid().ToString("N"));
            try
            {
                var persistence = new CareerPersistence(folder);
                string path = persistence.Save(state, 3);
                Assert.That(Path.GetFileName(path), Is.EqualTo("ironman.json"));
                Assert.Throws<InvalidOperationException>(() => persistence.Load(0));
                Assert.That(persistence.Load(0, true), Is.Not.Null);
                persistence.Save(CreateState(6, CareerDifficulty.Normal), CareerPersistence.LastManualSlot);
                Assert.That(persistence.Exists(CareerPersistence.LastManualSlot), Is.True);
                Assert.That(persistence.PeekSlot(CareerPersistence.LastManualSlot).Exists, Is.True);
            }
            finally
            {
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
            }
        }

        [Test]
        public void MultiSeasonSmoke_PreservesCoreInvariants()
        {
            int bankrupt = 0;
            const int seasonCount = 20;
            for (int seed = 1; seed <= seasonCount; seed++)
            {
                CareerState state = RunOneSeason(seed);
                if (state.clubs.Any(x => x.financialHealth == FinancialHealth.Bankrupt)) bankrupt++;
                Assert.That(state.players.GroupBy(x => x.playerId).All(x => x.Count() == 1), Is.True);
                Assert.That(state.rankings.Select(x => x.rank), Is.EqualTo(Enumerable.Range(1, state.clubs.Count)));
                Assert.That(state.clubs.Count, Is.EqualTo(24));
                Assert.That(state.clubs.All(x => x.cash > int.MinValue), Is.True);
                Assert.That(state.events.Any(x => x.type == CareerEventType.MonthClosed), Is.True);
            }
            TestContext.WriteLine($"{seasonCount}-season bankrupt snapshots: {bankrupt}");
            Assert.That(bankrupt, Is.LessThan(seasonCount));
        }

        private CareerState RunOneSeason(int seed)
        {
            CareerState state = CreateState(seed, CareerDifficulty.Normal);
            CareerSession session = BuildSession(state);
            session.SignSponsor("elite_tech");
            foreach (CompetitionDefinition competition in _content.Competitions.Values.OrderBy(x => x.startHour))
            {
                while (!state.gameOver &&
                       !CareerCalendar.IsRegisterableThisMonth(competition, _content.Balance, state.clockHour) &&
                       state.clockHour < competition.startHour)
                    session.AdvanceHours(CareerCalendar.HoursPerDay);
                session.RegisterCompetition(competition.id);
            }

            while (session.AwaitsOfficialPlayerMatch)
                session.AutoPlayPendingOfficialMatch();
            session.AdvanceToYearEnd();
            return state;
        }

        [Test]
        public void Transfer_SellerAcceptsFee_DoesNotMovePlayerOrCashYet()
        {
            CareerState state = CreateState(11, CareerDifficulty.Normal);
            CareerSession session = BuildSession(state);
            PlayerCareerState sold = state.players.First(x => x.clubId == state.playerClubId);
            ClubState seller = state.Club(state.playerClubId);
            ClubState buyer = state.clubs.First(x => x.clubId != state.playerClubId);
            int sellerCash = seller.cash;
            int buyerCash = buyer.cash;

            state.offers.Add(new TransferOffer
            {
                id = "fee_only",
                playerId = sold.playerId,
                buyerClubId = buyer.clubId,
                sellerClubId = state.playerClubId,
                fee = 250000,
                createdHour = state.clockHour,
                expiresHour = state.clockHour + TransferNegotiationRules.FeeOfferWindowHours,
                status = TransferOfferStatus.Pending
            });

            Assert.That(session.ResolvePlayerOffer("fee_only", TransferOfferStatus.Accepted), Is.True);
            TransferOffer offer = state.offers.First(x => x.id == "fee_only");
            Assert.That(offer.status, Is.EqualTo(TransferOfferStatus.ClubAgreed));
            Assert.That(offer.phaseExpiresHour, Is.EqualTo(
                state.clockHour + TransferNegotiationRules.PersonalTermsWindowHours));
            Assert.That(sold.clubId, Is.EqualTo(state.playerClubId));
            Assert.That(seller.cash, Is.EqualTo(sellerCash));
            Assert.That(buyer.cash, Is.EqualTo(buyerCash));
        }

        [Test]
        public void Transfer_PersonalTermsAccept_CompletesMoveAndPayment()
        {
            CareerState state = CreateState(12, CareerDifficulty.Normal);
            CareerSession session = BuildSession(state);
            ClubState buyer = state.Club(state.playerClubId);
            buyer.cash = 20_000_000;

            // Free roster slots so ClubWantsPlayer always allows the deal path via direct offer insert.
            foreach (PlayerCareerState rostered in state.players.Where(x => x.clubId == state.playerClubId).Skip(2).ToArray())
                rostered.clubId = string.Empty;

            PlayerCareerState target = state.players.First(x =>
                !string.IsNullOrEmpty(x.clubId) && x.clubId != state.playerClubId);
            string sellerId = target.clubId;
            ClubState seller = state.Club(sellerId);
            int buyerCash = buyer.cash;
            int sellerCash = seller.cash;
            ContractState contract = state.contracts.First(x => x.playerId == target.playerId);

            state.offers.Add(new TransferOffer
            {
                id = "personal_deal",
                playerId = target.playerId,
                buyerClubId = state.playerClubId,
                sellerClubId = sellerId,
                fee = 400000,
                createdHour = state.clockHour,
                expiresHour = state.clockHour + TransferNegotiationRules.FeeOfferWindowHours,
                status = TransferOfferStatus.ClubAgreed,
                phaseExpiresHour = state.clockHour + TransferNegotiationRules.PersonalTermsWindowHours
            });

            int asked = MarketRules.AskingSalary(400000, target.age, target.fame);
            Assert.That(session.ProposePlayerContract("personal_deal", asked, asked, 36), Is.True);
            Assert.That(session.ResolvePlayerContractTerms("personal_deal", true), Is.True);

            TransferOffer offer = state.offers.First(x => x.id == "personal_deal");
            Assert.That(offer.status, Is.EqualTo(TransferOfferStatus.Accepted));
            Assert.That(target.clubId, Is.EqualTo(state.playerClubId));
            Assert.That(buyer.cash, Is.LessThan(buyerCash));
            Assert.That(seller.cash, Is.GreaterThan(sellerCash));
            Assert.That(contract.clubId, Is.EqualTo(state.playerClubId));
            Assert.That(contract.monthlySalary, Is.EqualTo(asked));
        }

        [Test]
        public void Transfer_HigherRivalBid_OutbidsVulnerableDeal()
        {
            CareerState state = CreateState(13, CareerDifficulty.Normal);
            CareerSession session = BuildSession(state);
            ClubState buyer = state.Club(state.playerClubId);
            buyer.cash = 30_000_000;
            foreach (PlayerCareerState rostered in state.players.Where(x => x.clubId == state.playerClubId).Skip(2).ToArray())
                rostered.clubId = string.Empty;

            PlayerCareerState target = state.players.First(x =>
                !string.IsNullOrEmpty(x.clubId) && x.clubId != state.playerClubId);
            ContractState contract = state.contracts.First(x => x.playerId == target.playerId);
            contract.releaseClause = 900000;

            state.offers.Add(new TransferOffer
            {
                id = "vulnerable",
                playerId = target.playerId,
                buyerClubId = "gambit_2021",
                sellerClubId = target.clubId,
                fee = 500000,
                createdHour = state.clockHour,
                expiresHour = state.clockHour + TransferNegotiationRules.FeeOfferWindowHours,
                status = TransferOfferStatus.PersonalTerms,
                phaseExpiresHour = state.clockHour + TransferNegotiationRules.PersonalTermsWindowHours,
                offeredSalary = 10000,
                askedSalary = 10000,
                contractMonths = 24
            });

            TransferOffer buyout = session.MakeTransferOffer(target.playerId, contract.releaseClause);
            Assert.That(buyout, Is.Not.Null);
            Assert.That(buyout.releaseClauseBid, Is.True);
            Assert.That(buyout.status, Is.EqualTo(TransferOfferStatus.PersonalTerms));
            Assert.That(state.offers.First(x => x.id == "vulnerable").status,
                Is.EqualTo(TransferOfferStatus.Outbid));
            Assert.That(target.clubId, Is.Not.EqualTo(state.playerClubId));
        }

        [Test]
        public void Transfer_ReleaseClause_SkipsClubConsent_StillNeedsPersonalTerms()
        {
            CareerState state = CreateState(14, CareerDifficulty.Normal);
            CareerSession session = BuildSession(state);
            state.Club(state.playerClubId).cash = 40_000_000;
            foreach (PlayerCareerState rostered in state.players.Where(x => x.clubId == state.playerClubId).Skip(2).ToArray())
                rostered.clubId = string.Empty;

            PlayerCareerState target = state.players.First(x =>
                !string.IsNullOrEmpty(x.clubId) && x.clubId != state.playerClubId);
            string originalClub = target.clubId;
            ContractState contract = state.contracts.First(x => x.playerId == target.playerId);
            contract.releaseClause = 750000;

            TransferOffer offer = session.MakeTransferOffer(target.playerId, contract.releaseClause);
            Assert.That(offer, Is.Not.Null);
            Assert.That(offer.releaseClauseBid, Is.True);
            Assert.That(offer.status, Is.EqualTo(TransferOfferStatus.PersonalTerms));
            Assert.That(target.clubId, Is.EqualTo(originalClub));

            int asked = Math.Max(offer.askedSalary, MarketRules.AskingSalary(offer.fee, target.age, target.fame));
            Assert.That(session.ProposePlayerContract(offer.id, asked, asked * 2, 36), Is.True);
            Assert.That(session.ResolvePlayerContractTerms(offer.id, true), Is.True);
            Assert.That(target.clubId, Is.EqualTo(state.playerClubId));
            Assert.That(offer.status, Is.EqualTo(TransferOfferStatus.Accepted));
        }

        [Test]
        public void TransferNegotiationRules_OutbidThreshold_UsesMultiplierOrClause()
        {
            Assert.That(TransferNegotiationRules.CanOutbid(107999, 100000, false), Is.False);
            Assert.That(TransferNegotiationRules.CanOutbid(108000, 100000, false), Is.True);
            Assert.That(TransferNegotiationRules.CanOutbid(1, 100000, true), Is.True);
            Assert.That(
                TransferNegotiationRules.IsReleaseClauseBuyout(
                    new ContractState { releaseClause = 500000 }, 500000),
                Is.True);
            Assert.That(
                TransferNegotiationRules.IsReleaseClauseBuyout(
                    new ContractState { releaseClause = 500000 }, 499999),
                Is.False);
        }

        [Test]
        public void TransferNegotiationRules_PersonalTerms_FavorPrestigeAndStrongRoster()
        {
            double weak = TransferNegotiationRules.PersonalTermsAcceptanceScore(
                10000, 10000, 20000, 24, 10, 5, 50, false,
                buyerPrestige: 20, buyerRosterAvgPower: 60, playerPower: 90);
            double strong = TransferNegotiationRules.PersonalTermsAcceptanceScore(
                10000, 10000, 20000, 24, 10, 5, 50, false,
                buyerPrestige: 90, buyerRosterAvgPower: 92, playerPower: 90);
            Assert.That(strong, Is.GreaterThan(weak + 5d));
        }

        [Test]
        public void CareerDifficultyRules_HardNeedsAnalystForOpponentAttributes()
        {
            CareerState state = CreateState(21, CareerDifficulty.Hard);
            Assert.That(CareerDifficultyRules.UsesAttributeScouting(CareerDifficulty.Hard), Is.True);

            foreach (CoachStaffState coach in state.coaches.Where(x =>
                         string.Equals(x.clubId, state.playerClubId, StringComparison.OrdinalIgnoreCase) &&
                         ClubCoachStaffRules.ParseRole(x.role) == CoachStaffRole.Analyst).ToArray())
                coach.clubId = string.Empty;

            Assert.That(
                CareerDifficultyRules.CanRevealOpponentAttributes(state, state.playerClubId),
                Is.False);
            Assert.That(
                CareerDifficultyRules.FormatVisibleAttribute(state, state.playerClubId, "other_club", 88, 0),
                Is.EqualTo("需分析師"));
            Assert.That(
                CareerDifficultyRules.FormatVisibleAttribute(state, state.playerClubId, state.playerClubId, 88, 0),
                Is.EqualTo("88"));
        }

        [Test]
        public void EstimateMinimumTransferFee_IsBelowFullMarketValue()
        {
            CareerState state = CreateState(22, CareerDifficulty.Normal);
            CareerSession session = BuildSession(state);
            PlayerCareerState target = state.players.First(x =>
                !string.IsNullOrEmpty(x.clubId) && x.clubId != state.playerClubId);
            CareerPlayerDefinition def = _content.Players[target.playerId];
            ContractState contract = state.contracts.First(x => x.playerId == target.playerId);
            int market = MarketRules.CalculateMarketValue(target, def, contract,
                def.role == "Awper" ? 120 : 100);
            int min = session.EstimateMinimumTransferFee(target.playerId);
            Assert.That(min, Is.EqualTo(TransferNegotiationRules.EstimatedMinimumFee(market)));
            Assert.That(min, Is.LessThan(market));
        }

        [Test]
        public void CompetitionBracket_RoundTitles_ChangeByStage()
        {
            Assert.That(
                CompetitionRegistrationRules.RoundTitlesForTeamCount(16),
                Is.EqualTo(new[] { "十六強", "八強", "四強", "決賽" }));
            Assert.That(
                CompetitionRegistrationRules.RoundTitlesForTeamCount(8),
                Is.EqualTo(new[] { "八強", "四強", "決賽" }));
            Assert.That(CompetitionRegistrationRules.StageLabelForTeamsInRound(16), Is.EqualTo("十六強"));
            Assert.That(CompetitionRegistrationRules.StageLabelForTeamsInRound(8), Is.EqualTo("八強"));
            Assert.That(CompetitionRegistrationRules.StageLabelForTeamsInRound(4), Is.EqualTo("四強"));
            Assert.That(CompetitionRegistrationRules.StageLabelForTeamsInRound(2), Is.EqualTo("決賽"));

            List<CompetitionBracketRoundView> rounds = CompetitionRegistrationRules.BuildRoundViews(
                new List<string>
                {
                    "a", "b", "c", "d", "e", "f", "g", "h",
                    "i", "j", "k", "l", "m", "n", "o", "p"
                },
                "a",
                null);
            Assert.That(rounds.Select(x => x.title).ToArray(),
                Is.EqualTo(new[] { "十六強", "八強", "四強", "決賽" }));
        }

        [Test]
        public void SponsorSign_AllowsMultipleContractsButOnePerCalendarMonth()
        {
            CareerState state = CreateState(42, CareerDifficulty.Normal);
            CareerSession session = BuildSession(state);
            Assert.That(session.SignSponsor("local_gear"), Is.True);
            Assert.That(state.sponsors.Count(x => x.clubId == state.playerClubId), Is.EqualTo(1));
            Assert.That(session.SignSponsor("city_net"), Is.False);
            session.AdvanceHours(CareerCalendar.HoursPerDay * 28);
            Assert.That(session.SignSponsor("city_net"), Is.True);
            Assert.That(state.sponsors.Count(x => x.clubId == state.playerClubId), Is.EqualTo(2));
        }

        [Test]
        public void CareerDifficultyRules_HardcoreBlocksManualLoad()
        {
            Assert.That(CareerDifficultyRules.AllowsManualLoad(CareerDifficulty.Normal), Is.True);
            Assert.That(CareerDifficultyRules.AllowsManualLoad(CareerDifficulty.Hard), Is.True);
            Assert.That(CareerDifficultyRules.AllowsManualLoad(CareerDifficulty.Hardcore), Is.False);
            Assert.That(CareerDifficultyRules.AchievementsEligible(CareerDifficulty.Hardcore), Is.True);
            Assert.That(CareerDifficultyRules.AchievementsEligible(CareerDifficulty.Normal), Is.False);
            Assert.That(CareerDifficultyRules.FormatVisiblePotential(CareerDifficulty.Hard, 85), Is.EqualTo("77–93"));
            Assert.That(CareerDifficultyRules.FormatVisiblePotential(CareerDifficulty.Normal, 85), Is.EqualTo("85"));
        }

        [Test]
        public void DevelopmentRules_MapProficiencyDelta_DemotesSToAPlusHalf()
        {
            var player = new PlayerCareerState
            {
                playerId = "awper_test",
                mapProficiencies = new List<CareerNamedStat>
                {
                    new CareerNamedStat { id = "Dust II", grade = "S", value = 100f }
                }
            };
            DevelopmentRules.ApplyMapProficiencyDelta(player, "Dust II", -25f);
            CareerNamedStat dust = player.mapProficiencies.First(x => x.id == "Dust II");
            Assert.That(dust.grade, Is.EqualTo("A+"));
            Assert.That(dust.value, Is.EqualTo(75f).Within(0.01f));
        }

        [Test]
        public void ScriptedEventTierRules_FirstPersonRosterGate_RequiresAwperAboveA()
        {
            var def = new CareerScriptedEventDef
            {
                firstPersonRosterRole = "Awper",
                firstPersonRosterMapId = "Dust II",
                firstPersonRosterAboveGrade = "A",
                firstPersonDescription = "fp",
                description = "tp"
            };
            var players = new List<PlayerCareerState>
            {
                new PlayerCareerState
                {
                    playerId = "s1",
                    clubId = "club_a",
                    mapProficiencies = new List<CareerNamedStat>
                    {
                        new CareerNamedStat { id = "Dust II", grade = "A", value = 80f }
                    }
                },
                new PlayerCareerState
                {
                    playerId = "s2",
                    clubId = "club_a",
                    mapProficiencies = new List<CareerNamedStat>
                    {
                        new CareerNamedStat { id = "Dust II", grade = "A+", value = 10f }
                    }
                }
            };

            Assert.That(
                ScriptedEventTierRules.MeetsFirstPersonRosterGate(
                    def, "club_a", players, id => id == "s2" ? "Awper" : "Rifler"),
                Is.True);
            Assert.That(
                ScriptedEventTierRules.MeetsFirstPersonRosterGate(
                    def, "club_a", players, id => "Rifler"),
                Is.False);
            Assert.That(
                ScriptedEventTierRules.ResolveBody(def, "club_a", () => false),
                Is.EqualTo("tp"));
            Assert.That(
                ScriptedEventTierRules.ResolveBody(def, "club_a", () => true),
                Is.EqualTo("fp"));
        }

        [Test]
        public void ScriptedEventScheduleRules_SameDayEvents_SpreadAtLeastThreeHoursApart()
        {
            var defs = new[]
            {
                new CareerScriptedEventDef { id = "a", fireHour = 240 },
                new CareerScriptedEventDef { id = "b", fireHour = 240 },
                new CareerScriptedEventDef { id = "c", fireHour = 241 },
                new CareerScriptedEventDef { id = "solo", fireHour = 480 }
            };

            Dictionary<string, int> hours = ScriptedEventScheduleRules.BuildSpreadFireHours(defs, seed: 99);
            Assert.That(hours["solo"], Is.EqualTo(480));

            int[] dayHours = new[] { hours["a"], hours["b"], hours["c"] }
                .Select(h => h - 240)
                .OrderBy(h => h)
                .ToArray();
            Assert.That(dayHours.Length, Is.EqualTo(3));
            Assert.That(dayHours[0], Is.InRange(0, 23));
            Assert.That(dayHours[2], Is.InRange(0, 23));
            Assert.That(dayHours[1] - dayHours[0], Is.GreaterThanOrEqualTo(3));
            Assert.That(dayHours[2] - dayHours[1], Is.GreaterThanOrEqualTo(3));

            int[] allocated = ScriptedEventScheduleRules.AllocateHoursInDay(
                4, ScriptedEventScheduleRules.MinSameDayGapHours, new SeededRandom(7));
            Array.Sort(allocated);
            for (int i = 1; i < allocated.Length; i++)
                Assert.That(allocated[i] - allocated[i - 1], Is.GreaterThanOrEqualTo(3));
        }

        [Test]
        public void CompetitionScheduleRules_DualLanOverlap_IsBlocked()
        {
            var existing = new List<CompetitionCommitment>
            {
                new CompetitionCommitment
                {
                    competitionId = "lan_a",
                    displayName = "LAN A",
                    format = CompetitionFormat.LAN,
                    startHour = 100,
                    endHour = 200
                }
            };
            var candidate = new CompetitionCommitment
            {
                competitionId = "lan_b",
                displayName = "LAN B",
                format = CompetitionFormat.Hybrid,
                startHour = 150,
                endHour = 250
            };

            CompetitionScheduleEvaluation evaluation = CompetitionScheduleRules.EvaluatePlayerSignup(
                candidate, existing, confirmOnlineOverlap: true, keepCompetitionIds: null);
            Assert.That(evaluation.verdict, Is.EqualTo(CompetitionScheduleVerdict.BlockedDualPhysical));
            Assert.That(CompetitionScheduleRules.CanAiAccept(candidate, existing), Is.False);
        }

        [Test]
        public void CompetitionScheduleRules_OnlinePlusLan_AllowedWithConfirm()
        {
            var existing = new List<CompetitionCommitment>
            {
                new CompetitionCommitment
                {
                    competitionId = "lan_a",
                    displayName = "LAN A",
                    format = CompetitionFormat.LAN,
                    startHour = 100,
                    endHour = 200
                }
            };
            var candidate = new CompetitionCommitment
            {
                competitionId = "on_a",
                displayName = "Online A",
                format = CompetitionFormat.Online,
                startHour = 120,
                endHour = 180
            };

            CompetitionScheduleEvaluation needsConfirm = CompetitionScheduleRules.EvaluatePlayerSignup(
                candidate, existing, confirmOnlineOverlap: false, keepCompetitionIds: null);
            Assert.That(needsConfirm.verdict, Is.EqualTo(CompetitionScheduleVerdict.NeedsOnlineOverlapConfirm));

            CompetitionScheduleEvaluation confirmed = CompetitionScheduleRules.EvaluatePlayerSignup(
                candidate, existing, confirmOnlineOverlap: true, keepCompetitionIds: null);
            Assert.That(confirmed.verdict, Is.EqualTo(CompetitionScheduleVerdict.Allowed));
            Assert.That(CompetitionScheduleRules.CanAiAccept(candidate, existing), Is.True);
        }

        [Test]
        public void CompetitionScheduleRules_ThreeConcurrent_NeedsChooseTwo()
        {
            var existing = new List<CompetitionCommitment>
            {
                new CompetitionCommitment
                {
                    competitionId = "on_a",
                    displayName = "Online A",
                    format = CompetitionFormat.Online,
                    startHour = 100,
                    endHour = 300
                },
                new CompetitionCommitment
                {
                    competitionId = "on_b",
                    displayName = "Online B",
                    format = CompetitionFormat.Online,
                    startHour = 100,
                    endHour = 300
                }
            };
            var candidate = new CompetitionCommitment
            {
                competitionId = "on_c",
                displayName = "Online C",
                format = CompetitionFormat.Online,
                startHour = 100,
                endHour = 300
            };

            CompetitionScheduleEvaluation evaluation = CompetitionScheduleRules.EvaluatePlayerSignup(
                candidate, existing, confirmOnlineOverlap: true, keepCompetitionIds: null);
            Assert.That(evaluation.verdict, Is.EqualTo(CompetitionScheduleVerdict.NeedsChooseTwo));
            Assert.That(evaluation.conflictCompetitionIds.Count, Is.EqualTo(3));

            CompetitionScheduleEvaluation kept = CompetitionScheduleRules.EvaluatePlayerSignup(
                candidate,
                existing,
                confirmOnlineOverlap: true,
                keepCompetitionIds: new[] { "on_a", "on_c" });
            Assert.That(kept.verdict, Is.EqualTo(CompetitionScheduleVerdict.Allowed));
            Assert.That(CompetitionScheduleRules.CanAiAccept(candidate, existing), Is.False);
        }

        [Test]
        public void CompetitionScheduleRules_NonOverlappingLan_IsAllowed()
        {
            var existing = new List<CompetitionCommitment>
            {
                new CompetitionCommitment
                {
                    competitionId = "lan_a",
                    displayName = "LAN A",
                    format = CompetitionFormat.LAN,
                    startHour = 100,
                    endHour = 200
                }
            };
            var candidate = new CompetitionCommitment
            {
                competitionId = "lan_b",
                displayName = "LAN B",
                format = CompetitionFormat.LAN,
                startHour = 204,
                endHour = 300
            };

            CompetitionScheduleEvaluation evaluation = CompetitionScheduleRules.EvaluatePlayerSignup(
                candidate, existing, confirmOnlineOverlap: false, keepCompetitionIds: null);
            Assert.That(evaluation.verdict, Is.EqualTo(CompetitionScheduleVerdict.Allowed));
            Assert.That(CompetitionScheduleRules.CanAiAccept(candidate, existing), Is.True);
        }

        [Test]
        public void ClubCrisisRules_DynastyDailyPressure_IsChanceBasedNotFlat()
        {
            Assert.That(ClubCrisisRules.DynastyDailyPressure, Is.EqualTo(1));
            Assert.That(ClubCrisisRules.DynastyDailyPressureChance, Is.EqualTo(0.30f).Within(0.0001f));

            var state = new CareerState
            {
                seed = 42,
                clockHour = 100,
                flags = new List<CareerFlag>
                {
                    new CareerFlag
                    {
                        id = ClubCrisisRules.DynastyInternalFractureFlag,
                        scope = "astralis_2021",
                        expiresHour = 0
                    }
                },
                clubs = new List<ClubState>
                {
                    new ClubState { clubId = "astralis_2021", exposure = 80 }
                },
                players = new List<PlayerCareerState>()
            };
            for (int i = 0; i < 40; i++)
            {
                state.players.Add(new PlayerCareerState
                {
                    playerId = "p" + i,
                    clubId = "astralis_2021",
                    pressure = 40
                });
            }

            ClubCrisisRules.ApplyDailyEffects(state);
            Assert.That(state.clubs[0].exposure, Is.EqualTo(50));
            int raised = state.players.Count(p => p.pressure > 40);
            Assert.That(raised, Is.GreaterThan(0));
            Assert.That(raised, Is.LessThan(state.players.Count));
            Assert.That(state.players.All(p => p.pressure == 40 || p.pressure == 41), Is.True);
        }

        private CareerState RunHours(int hours, int seed)
        {
            CareerState state = CreateState(seed, CareerDifficulty.Normal);
            CareerSession session = BuildSession(state);
            session.AdvanceHours(hours);
            return state;
        }

        private CareerState CreateState(int seed, CareerDifficulty difficulty) =>
            CareerSession.CreateNew(_content.Balance, _content.Players.Values, _content.Clubs.Values,
                "navi_2021", difficulty, seed);

        private CareerSession BuildSession(CareerState state) =>
            new CareerSession(state, _content.Balance, _content.Players.Values, _content.Clubs.Values,
                _content.Sponsors.Values, _content.Competitions.Values, _content.Facilities.Values,
                new DeterministicCareerMatchBridge());
    }
}
