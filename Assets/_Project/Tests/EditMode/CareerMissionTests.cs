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
    public sealed class CareerMissionTests
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
                File.ReadAllText(Path.Combine(root, "facilities.json")),
                File.ReadAllText(Path.Combine(root, "career_events.json")),
                File.ReadAllText(Path.Combine(root, "career_achievements.json")),
                null,
                File.ReadAllText(Path.Combine(root, "career_missions.json")));
            _content = _asset.BuildDomain();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_asset);

        private CareerSession BuildSession(string playerClubId = "navi_2021")
        {
            CareerState state = CareerSession.CreateNew(
                _content.Balance, _content.Players.Values, _content.Clubs.Values,
                playerClubId, CareerDifficulty.Normal, 2021);
            return new CareerSession(
                state, _content.Balance, _content.Players.Values, _content.Clubs.Values,
                _content.Sponsors.Values, _content.Competitions.Values, _content.Facilities.Values,
                new DeterministicCareerMatchBridge(), null,
                _content.ScriptedEvents.Values, _content.Achievements.Values,
                _content.Coaches.Values, _content.MissionTrees.Values);
        }

        [Test]
        public void MissionContent_LoadsNaviSingleLineTree()
        {
            Assert.That(_content.MissionTrees.Count, Is.GreaterThanOrEqualTo(1));
            CareerMissionTreeDef tree = CareerMissionRules.FindTreeForClub(
                _content.MissionTrees.Values, "navi_2021");
            Assert.That(tree, Is.Not.Null);
            Assert.That(tree.nodes.Length, Is.EqualTo(7));
            Assert.That(tree.nodes.Select(n => n.id).ToArray(), Does.Contain("navi_rise_day"));
            Assert.That(tree.nodes.First(n => n.id == "navi_rise_day").title, Is.EqualTo("崛起之日"));
            Assert.That(tree.nodes.First(n => n.id == "navi_triple_hold").flavor, Does.Contain("三核"));
        }

        [Test]
        public void Condition_Any_AndPlayerClub()
        {
            CareerSession session = BuildSession("navi_2021");
            ClubState club = session.State.Club("navi_2021");

            var anyOk = new CareerConditionDef
            {
                type = "any",
                conditions = new[]
                {
                    new CareerConditionDef { type = "not_player_club", key = "navi_2021" },
                    new CareerConditionDef { type = "has_flag", key = "navi_path_b1t" }
                }
            };
            Assert.That(
                CareerScriptedRules.Matches(anyOk, session.State, club, session.State.unlockedAchievements),
                Is.False);

            CareerFlagRules.SetFlag(
                session.State.flags, "navi_path_b1t", CareerFlagRules.GlobalScope, 0, 0);
            Assert.That(
                CareerScriptedRules.Matches(anyOk, session.State, club, session.State.unlockedAchievements),
                Is.True);

            CareerSession gambit = BuildSession("gambit_2021");
            ClubState gambitClub = gambit.State.Club("gambit_2021");
            Assert.That(
                CareerScriptedRules.Matches(anyOk, gambit.State, gambitClub, gambit.State.unlockedAchievements),
                Is.True);
        }

        [Test]
        public void Condition_OfficialMatchesMin()
        {
            CareerSession session = BuildSession();
            ClubState club = session.State.Club("navi_2021");
            var cond = new CareerConditionDef { type = "official_matches_min", value = 5 };
            session.State.playerOfficialMatchesPlayed = 4;
            Assert.That(
                CareerScriptedRules.Matches(cond, session.State, club, session.State.unlockedAchievements),
                Is.False);
            session.State.playerOfficialMatchesPlayed = 5;
            Assert.That(
                CareerScriptedRules.Matches(cond, session.State, club, session.State.unlockedAchievements),
                Is.True);
        }

        [Test]
        public void Condition_CompetitionStarted_UsesRegistrationAndClock()
        {
            CareerSession session = BuildSession();
            ClubState club = session.State.Club("navi_2021");
            const string majorId = "pgl_major_stockholm_2021";
            var cond = new CareerConditionDef { type = "competition_started", key = majorId };
            IReadOnlyDictionary<string, CompetitionDefinition> competitions = _content.Competitions;

            Assert.That(
                CareerScriptedRules.Matches(cond, session.State, club, session.State.unlockedAchievements, competitions),
                Is.False);

            CompetitionDefinition major = competitions[majorId];
            session.State.competitionEntries.Add(new CompetitionEntry
            {
                competitionId = majorId,
                clubId = "navi_2021",
                registered = true,
                registeredHour = major.startHour - 24
            });
            session.State.clockHour = major.startHour;
            Assert.That(
                CareerScriptedRules.Matches(cond, session.State, club, session.State.unlockedAchievements, competitions),
                Is.True);
        }

        [Test]
        public void MissionTree_LinearChainAndB1tFlag()
        {
            CareerSession session = BuildSession();
            Assert.That(session.TryCompleteMissionNode("navi_rise_day", out _), Is.True);
            Assert.That(session.TryCompleteMissionNode("navi_third_gun", out _), Is.True);
            Assert.That(
                CareerFlagRules.HasFlag(
                    session.State.flags, CareerMissionRules.NaviPathB1tFlag,
                    CareerFlagRules.GlobalScope, session.State.clockHour),
                Is.True);
            Assert.That(session.TryCompleteMissionNode("navi_mic_to_youth", out _), Is.True);
            Assert.That(
                session.GetMissionNodeStatus("navi_triple_hold"),
                Is.EqualTo(CareerMissionNodeStatus.Available));
            session.State.playerOfficialMatchesPlayed = 5;
            Assert.That(
                session.GetMissionNodeStatus("navi_triple_hold"),
                Is.EqualTo(CareerMissionNodeStatus.Completable));
        }

        [Test]
        public void AprilEvent_GatedByB1tPathForNaviPlayer()
        {
            Assert.That(_content.ScriptedEvents.ContainsKey("hist_b1t_replaces_flamie_navi_2021_04_11"), Is.True);
            CareerScriptedEventDef april = _content.ScriptedEvents["hist_b1t_replaces_flamie_navi_2021_04_11"];

            CareerSession navi = BuildSession("navi_2021");
            ClubState naviClub = navi.State.Club("navi_2021");
            Assert.That(
                CareerScriptedRules.MatchesAll(
                    april.triggers, navi.State, naviClub, navi.State.unlockedAchievements),
                Is.False);

            CareerFlagRules.SetFlag(
                navi.State.flags, CareerMissionRules.NaviPathB1tFlag,
                CareerFlagRules.GlobalScope, navi.State.clockHour, 0);
            Assert.That(
                CareerScriptedRules.MatchesAll(
                    april.triggers, navi.State, naviClub, navi.State.unlockedAchievements),
                Is.True);

            CareerSession gambit = BuildSession("gambit_2021");
            Assert.That(
                CareerScriptedRules.MatchesAll(
                    april.triggers, gambit.State, gambit.State.Club("gambit_2021"),
                    gambit.State.unlockedAchievements),
                Is.True);

            navi.State.clockHour = april.fireHour;
            foreach (CareerScriptedEventDef def in _content.ScriptedEvents.Values)
            {
                if (def == null || string.IsNullOrWhiteSpace(def.id))
                    continue;
                if (string.Equals(def.id, april.id, System.StringComparison.OrdinalIgnoreCase))
                    continue;
                if (def.fireHour <= 0 || def.fireHour > april.fireHour)
                    continue;
                CareerFlagRules.SetFlag(
                    navi.State.flags,
                    ScriptedEventTierRules.FiredFlagId(def.id),
                    CareerFlagRules.GlobalScope,
                    navi.State.clockHour,
                    0);
            }

            CareerScriptedEventDef due = CareerScriptedRules.PickDueScheduledEvent(
                _content.ScriptedEvents.Values, navi.State, naviClub);
            Assert.That(due, Is.Not.Null);
            Assert.That(due.id, Is.EqualTo("hist_b1t_replaces_flamie_navi_2021_04_11"));
        }
    }
}
