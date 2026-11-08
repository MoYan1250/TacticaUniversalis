# -*- coding: utf-8 -*-
"""
Skip: restore fast path — sync AutoPlayRemaining + one-shot HUD.
Do NOT replay every event (that is what made skip crawl),
and do NOT yield one-round-per-frame (that made wall-clock slower).
"""
from pathlib import Path
import backup_quickmatch as bak

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError("missing:\n" + old[:320])
    return text.replace(old, new, 1)


def main() -> None:
    bak.backup("before_skip_fast_batch")
    text = TARGET.read_text(encoding="utf-8")
    if "特性觸發" not in text or "SkipMatchRoutine" not in text:
        raise RuntimeError("UTF-8 corrupted or skip routine missing")

    old = """        private IEnumerator SkipMatchRoutine()
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
                                "跳過結算中");
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
"""

    new = """        private IEnumerator SkipMatchRoutine()
        {
            _skippingMatch = true;
            try
            {
                // One frame so the HUD can show "skipping", then finish remaining
                // rounds in one batch. Replaying every event via ApplyEvent was the
                // real hitch; one-round-per-frame only stretched wall-clock time.
                SetRoundHud(Mathf.Max(1, _currentRound), "跳過結算中");
                yield return null;

                if (_session != null && !_session.IsFinished)
                {
                    if (_session.IsComplete)
                        _session.FinishMatch();
                    else
                        _session.AutoPlayRemaining(myTeamId);
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
"""

    text = must_replace(text, old, new)
    TARGET.write_text(text, encoding="utf-8")
    verify = TARGET.read_text(encoding="utf-8")
    if "特性觸發" not in verify:
        raise RuntimeError("UTF-8 lost")
    if "AutoPlayRemaining(myTeamId)" not in verify:
        raise RuntimeError("AutoPlayRemaining missing")
    if "yield return null;" in verify.split("private IEnumerator SkipMatchRoutine()")[1].split("private void ApplySkippedMatchPresentation()")[0].split("SetRoundHud")[1]:
        # should have exactly one yield before AutoPlay, and ShowOfficialResultFlow yield later
        pass
    if "PlayRound(myTeamId, card)" in verify.split("SkipMatchRoutine()")[1].split("ApplySkippedMatchPresentation()")[0]:
        raise RuntimeError("per-round loop still present")
    print("patched skip fast batch OK")


if __name__ == "__main__":
    main()
