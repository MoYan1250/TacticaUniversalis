# -*- coding: utf-8 -*-
"""
Fix Skip() hitch: do not AutoPlayRemaining + ApplyEvent(all) on one frame.
- Spread remaining rounds across frames
- Apply final HUD from result summaries / session state once
"""
from pathlib import Path
import backup_quickmatch as bak

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError("missing:\n" + old[:300])
    return text.replace(old, new, 1)


def main() -> None:
    bak.backup("before_skip_perf")
    text = TARGET.read_text(encoding="utf-8")
    if "特性觸發" not in text:
        raise RuntimeError("UTF-8 corrupted")

    if "_skippingMatch" not in text:
        text = must_replace(
            text,
            "        private bool _careerOfficialMatch;\n",
            "        private bool _careerOfficialMatch;\n"
            "        private bool _skippingMatch;\n",
        )

    old_skip = """        private void Skip()
        {
            StopAllMovement();
            if (_playback != null)
            {
                StopCoroutine(_playback);
                _playback = null;
            }
            _awaitingCard = false;
            _awaitingTacticalCards = false;
            _awaitingTimeout = false;
            _awaitingFocus = false;
            _selectedCard = null;
            HideCardPicker();
            HideTacticalCardPicker();
            HideTimeoutPicker();
            HideFocusPicker();
            HideRoundWinBanner();

            if (_session != null && !_session.IsFinished)
            {
                if (_session.IsComplete)
                    _session.FinishMatch();
                else
                    _session.AutoPlayRemaining(myTeamId);
            }
            _result = _session?.Result;

            _naviScore = 0;
            _gambitScore = 0;
            _roundHasDeath = false;
            _killFeed.Clear();
            _traitFeed.Clear();
            ResetEconomyDisplay();
            ResetStats();
            if (_result != null)
            {
                foreach (SimulationEvent evt in _result.events.OrderBy(x => x.roundNumber).ThenBy(x => x.roundTime).ThenBy(x => x.sequence))
                    ApplyEvent(evt, false);
            }
            UpdateKillFeed();
            UpdateTraitFeed();
            UpdateAliveCountHud();
            UpdateEconomyHud();

            if (_careerOfficialMatch && _result != null && _result.winnerTeamId != null)
                StartCoroutine(ShowOfficialResultFlow());
        }
"""

    new_skip = """        private void Skip()
        {
            if (_skippingMatch)
                return;

            StopAllMovement();
            if (_playback != null)
            {
                StopCoroutine(_playback);
                _playback = null;
            }
            _awaitingCard = false;
            _awaitingTacticalCards = false;
            _awaitingTimeout = false;
            _awaitingFocus = false;
            _selectedCard = null;
            HideCardPicker();
            HideTacticalCardPicker();
            HideTimeoutPicker();
            HideFocusPicker();
            HideRoundWinBanner();

            _playback = StartCoroutine(SkipMatchRoutine());
        }

        private IEnumerator SkipMatchRoutine()
        {
            _skippingMatch = true;
            try
            {
                // Spread remaining simulation across frames — AutoPlayRemaining + full
                // ApplyEvent replay on one frame freezes the battle UI for a long time.
                if (_session != null && !_session.IsFinished)
                {
                    if (_session.IsComplete)
                        _session.FinishMatch();
                    else
                    {
                        while (!_session.IsComplete && !_session.IsFinished)
                        {
                            _session.PrepareRound();
                            TeamRuntime player = _session.TeamById(myTeamId);
                            bool hasAwper = player.Players.Any(x => x.Definition.primaryRole == "Awper");
                            EquipmentLevel card = EconomyRules.BestAffordable(player.Money, hasAwper);
                            _session.PlayRound(myTeamId, card);
                            SetRoundHud(
                                Mathf.Max(1, _session.State.CompletedRounds),
                                "跳過結算中…");
                            yield return null;
                        }
                        if (!_session.IsFinished)
                            _session.FinishMatch();
                    }
                }

                _result = _session?.Result;
                ApplySkippedMatchPresentation();

                if (_careerOfficialMatch && _result != null && _result.winnerTeamId != null)
                    yield return ShowOfficialResultFlow();
            }
            finally
            {
                _skippingMatch = false;
                _playback = null;
            }
        }

        private void ApplySkippedMatchPresentation()
        {
            _naviScore = 0;
            _gambitScore = 0;
            _roundHasDeath = false;
            _killFeed.Clear();
            _traitFeed.Clear();
            ResetEconomyDisplay();
            ResetStats();

            if (_result == null)
            {
                UpdateKillFeed();
                UpdateTraitFeed();
                UpdateAliveCountHud();
                UpdateEconomyHud();
                return;
            }

            _naviScore = _result.teamAScore;
            _gambitScore = _result.teamBScore;

            if (_result.playerSummaries != null)
            {
                foreach (PlayerMatchSummary summary in _result.playerSummaries)
                {
                    if (!_stats.TryGetValue(summary.playerId, out PlayerStat stat))
                        continue;
                    stat.Kills = summary.kills;
                    stat.Deaths = summary.deaths;
                    stat.Assists = summary.assists;
                    stat.Alive = true;
                    stat.RoundKills = 0;
                }
            }

            SyncEconomyFromState();
            if (_session != null)
            {
                _teamCards[_session.State.TeamA.Definition.id] = _session.State.TeamA.Equipment;
                _teamCards[_session.State.TeamB.Definition.id] = _session.State.TeamB.Equipment;
            }

            int lastRound = Mathf.Max(
                1,
                _session != null ? _session.State.CompletedRounds : 1);
            _currentRound = lastRound;
            SetRoundHud(lastRound, "比賽結束");
            ApplyMapOrientation(lastRound, false);
            ResetMarkers(lastRound, false);
            HideBomb();
            HideRoundWinBanner();
            UpdateScore();
            RefreshStatPanels();
            UpdateKillFeed();
            UpdateTraitFeed();
            UpdateAliveCountHud();
            UpdateEconomyHud();
            UpdateSidePanelColors();
            UpdateTeamHeaders();
        }
"""

    text = must_replace(text, old_skip, new_skip)

    # Avoid CJK ellipsis if user rules dislike weird chars - "跳過結算中…" uses … 
    # AGENTS says no 「」()《》 - ellipsis … should be fine. Use ASCII "..." to be safer.
    text = text.replace('SetRoundHud(\n                                Mathf.Max(1, _session.State.CompletedRounds),\n                                "跳過結算中…");',
                        'SetRoundHud(\n                                Mathf.Max(1, _session.State.CompletedRounds),\n                                "跳過結算中");')

    TARGET.write_text(text, encoding="utf-8")
    verify = TARGET.read_text(encoding="utf-8")
    if "特性觸發" not in verify:
        raise RuntimeError("UTF-8 lost")
    if "SkipMatchRoutine" not in verify or "ApplySkippedMatchPresentation" not in verify:
        raise RuntimeError("skip perf patch incomplete")
    if "AutoPlayRemaining(myTeamId)" in verify.split("private void Skip()")[1].split("private void TogglePause()")[0]:
        raise RuntimeError("old sync Skip path still present")
    print("patched skip perf OK")


if __name__ == "__main__":
    main()
