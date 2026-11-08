using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TacticaUniversalis.Domain;

namespace TacticaUniversalis.Tests.EditMode
{
    public sealed class MapVetoRulesTests
    {
        [Test]
        public void BeginVeto_UsesSevenMaps_AndCoinFlipIsDeterministic()
        {
            var series = new CareerMapSeriesState();
            MapVetoRules.BeginVeto(series, seed: 2);
            Assert.That(series.poolRemaining.Count, Is.EqualTo(7));
            Assert.That(series.playerIsVetoA, Is.EqualTo(MapVetoRules.PlayerIsVetoSideA(2)));
            Assert.That(series.vetoComplete, Is.False);
        }

        [Test]
        public void SevenStepVeto_ProducesThreeDistinctMaps()
        {
            var series = new CareerMapSeriesState();
            MapVetoRules.AutoCompleteVeto(series, seed: 41);
            Assert.That(series.vetoComplete, Is.True);
            Assert.That(series.seriesMapIds[0], Is.Not.Null.And.Not.Empty);
            Assert.That(series.seriesMapIds[1], Is.Not.Null.And.Not.Empty);
            Assert.That(series.seriesMapIds[2], Is.Not.Null.And.Not.Empty);
            Assert.That(series.seriesMapIds[0], Is.Not.EqualTo(series.seriesMapIds[1]));
            Assert.That(series.seriesMapIds[0], Is.Not.EqualTo(series.seriesMapIds[2]));
            Assert.That(series.seriesMapIds[1], Is.Not.EqualTo(series.seriesMapIds[2]));
            Assert.That(series.bannedIds.Count, Is.EqualTo(4));
            Assert.That(series.poolRemaining.Count, Is.EqualTo(0));
        }

        [Test]
        public void PlayerBanThenAiCompletes_Works()
        {
            var series = new CareerMapSeriesState();
            MapVetoRules.BeginVeto(series, seed: 0); // player is A
            Assert.That(series.playerIsVetoA, Is.True);
            Assert.That(MapVetoRules.IsPlayerTurn(0, true), Is.True);
            Assert.That(MapVetoRules.TryApplyChoice(series, "dust2", out _), Is.True);
            Assert.That(series.bannedIds, Does.Contain("dust2"));

            int guard = 0;
            while (!series.vetoComplete && guard++ < 12)
            {
                if (series.vetoStep == (int)MapVetoStepKind.Decider)
                {
                    Assert.That(MapVetoRules.TryResolveDecider(series, out _), Is.True);
                    break;
                }

                if (MapVetoRules.IsPlayerTurn(series.vetoStep, series.playerIsVetoA))
                {
                    string pick = series.poolRemaining[0];
                    Assert.That(MapVetoRules.TryApplyChoice(series, pick, out _), Is.True);
                }
                else
                    Assert.That(MapVetoRules.TryApplyAiChoice(series, 0, out _, out _), Is.True);
            }

            Assert.That(series.vetoComplete, Is.True);
        }

        [Test]
        public void SeriesIsComplete_RequiresTwoMapWins()
        {
            var series = new CareerMapSeriesState { vetoComplete = true, mapWinsPlayer = 1, mapWinsOpponent = 1 };
            Assert.That(MapVetoRules.SeriesIsComplete(series), Is.False);
            series.mapWinsPlayer = 2;
            Assert.That(MapVetoRules.SeriesIsComplete(series), Is.True);
        }

        [Test]
        public void ResolvePlayableMapId_FallsBackToDust2()
        {
            Assert.That(MapVetoRules.ResolvePlayableMapId("mirage", _ => false), Is.EqualTo("dust2"));
            Assert.That(MapVetoRules.ResolvePlayableMapId("dust2", id => id == "dust2"), Is.EqualTo("dust2"));
            Assert.That(MapVetoRules.ResolvePlayableMapId("train", id => id == "dust2" || id == "train"), Is.EqualTo("train"));
            Assert.That(MapVetoRules.ResolvePlayableMapId("ancient", id => id == "dust2" || id == "ancient"), Is.EqualTo("ancient"));
            Assert.That(MapVetoRules.DefaultPoolIds, Does.Contain("train"));
            Assert.That(MapVetoRules.DefaultPoolIds, Does.Not.Contain("ancient"));
        }

        [Test]
        public void MapPoolRules_SwapsTrainForAncientWhenRemoved()
        {
            string[] before = MapPoolRules.ResolveActivePoolIds(trainRemoved: false);
            Assert.That(before, Does.Contain("train"));
            Assert.That(before, Does.Not.Contain("ancient"));
            Assert.That(MapPoolRules.IsPlayable("train", false), Is.True);
            Assert.That(MapPoolRules.IsPlayable("ancient", false), Is.False);

            string[] after = MapPoolRules.ResolveActivePoolIds(trainRemoved: true);
            Assert.That(after, Does.Contain("ancient"));
            Assert.That(after, Does.Not.Contain("train"));
            Assert.That(after.Length, Is.EqualTo(7));
            Assert.That(MapPoolRules.IsPlayable("train", true), Is.False);
            Assert.That(MapPoolRules.IsPlayable("ancient", true), Is.True);
            Assert.That(MapPoolRules.IsPlayable("dust2", true), Is.True);

            MapPoolEntry[] entries = MapPoolRules.ResolveActivePool(MapVetoRules.CreateDefaultPool(), true);
            Assert.That(entries.Any(x => x.id == "ancient" && x.playable), Is.True);
            Assert.That(entries.Any(x => x.id == "train"), Is.False);
        }

        [Test]
        public void MapPoolRules_IsTrainRemoved_UsesFlagOrFiredEvent()
        {
            var state = new CareerState { clockHour = 3000, flags = new List<CareerFlag>() };
            Assert.That(MapPoolRules.IsTrainRemoved(state), Is.False);

            CareerFlagRules.SetFlag(
                state.flags, MapPoolRules.TrainPoolRemovedFlag, CareerFlagRules.GlobalScope, state.clockHour, 0);
            Assert.That(MapPoolRules.IsTrainRemoved(state), Is.True);

            state.flags.Clear();
            CareerFlagRules.SetFlag(
                state.flags,
                ScriptedEventTierRules.FiredFlagId(MapPoolRules.TrainOutAncientInEventId),
                CareerFlagRules.GlobalScope,
                state.clockHour,
                0);
            Assert.That(MapPoolRules.IsTrainRemoved(state), Is.True);
        }
    }
}
