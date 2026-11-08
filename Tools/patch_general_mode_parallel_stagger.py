# -*- coding: utf-8 -*-
"""Parallel agent path tweens: full glide duration, short stagger wait only."""
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def main() -> int:
    subprocess.check_call(
        [sys.executable, str(ROOT / "Tools" / "backup_quickmatch.py"), "parallel_move_stagger"]
    )
    text = TARGET.read_text(encoding="utf-8")
    old = '''                        // Start move first so the marker travels during the wait (no idle then zip).
                        float wait = EstimateGeneralMoveDuration(evt.actorId, dest, gap);
                        // Apply move with the same duration used for the wait clock.
                        _currentRound = Mathf.Max(1, evt.roundNumber);
                        SetRoundHud(evt.roundNumber, FormatClock(evt.roundTime));
                        MoveMarkerAlongPath(evt.actorId, dest, true, wait);
                        float elapsed = 0f;
                        float shown = chainEnd - gap;
                        while (elapsed < wait)
                        {
                            if (!_paused)
                            {
                                elapsed += Time.unscaledDeltaTime;
                                if (wait > 0.001f && gap > 0.001f)
                                {
                                    float u = Mathf.Clamp01(elapsed / wait);
                                    SetRoundHud(evt.roundNumber, FormatClock(shown + gap * u));
                                }
                            }
                            yield return null;
                        }
                        while (_paused)
                            yield return null;
                        continue;
'''
    new = '''                        // Long path tween runs in parallel; only stagger briefly so other agents start too.
                        float duration = EstimateGeneralMoveDuration(evt.actorId, dest, gap);
                        _currentRound = Mathf.Max(1, evt.roundNumber);
                        MoveMarkerAlongPath(evt.actorId, dest, true, duration);
                        float stagger = Mathf.Clamp(
                            gap / Mathf.Max(0.25f, playbackSpeed) * 0.1f, 0.02f, 0.14f);
                        float elapsed = 0f;
                        float shown = chainEnd - gap;
                        while (elapsed < stagger)
                        {
                            if (!_paused)
                            {
                                elapsed += Time.unscaledDeltaTime;
                                float u = Mathf.Clamp01(elapsed / Mathf.Max(0.001f, stagger));
                                SetRoundHud(evt.roundNumber, FormatClock(shown + gap * u));
                            }
                            yield return null;
                        }
                        SetRoundHud(evt.roundNumber, FormatClock(chainEnd));
                        while (_paused)
                            yield return null;
                        continue;
'''
    if old not in text:
        raise RuntimeError("coalesced move wait block missing")
    text = text.replace(old, new, 1)
    if "叫暫停" not in text:
        raise RuntimeError("UTF-8 corruption")
    TARGET.write_text(text, encoding="utf-8")
    print("patched parallel stagger")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
