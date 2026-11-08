# -*- coding: utf-8 -*-
"""Fix general-mode hang: compress long idle waits and keep clock moving."""
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str, count: int = 1) -> str:
    if old not in text:
        raise RuntimeError(f"patch anchor missing: {old[:120]!r}")
    return text.replace(old, new, count)


def main() -> int:
    subprocess.check_call([sys.executable, str(ROOT / "Tools" / "backup_quickmatch.py"), "general_mode_hang"])
    text = TARGET.read_text(encoding="utf-8")
    if "GeneralModePlaybackWait" in text:
        print("already patched")
        return 0

    text = must_replace(
        text,
        "                if (animate)\n"
        "                {\n"
        "                    float wait = Mathf.Max(0f, evt.roundTime - previousTime) / Mathf.Max(1f, playbackSpeed);\n"
        "                    previousTime = evt.roundTime;\n"
        "                    float elapsed = 0f;\n"
        "                    while (elapsed < wait)\n"
        "                    {\n"
        "                        if (!_paused)\n"
        "                            elapsed += Time.unscaledDeltaTime;\n"
        "                        yield return null;\n"
        "                    }\n"
        "                    while (_paused)\n"
        "                        yield return null;\n"
        "                }\n"
        "                else\n"
        "                    previousTime = evt.roundTime;\n"
        "\n"
        "                ApplyEvent(evt, animate);\n",
        "                if (animate)\n"
        "                {\n"
        "                    float gap = Mathf.Max(0f, evt.roundTime - previousTime);\n"
        "                    float wait = GeneralModePlaybackWait(gap);\n"
        "                    previousTime = evt.roundTime;\n"
        "                    float elapsed = 0f;\n"
        "                    float shown = evt.roundTime - gap;\n"
        "                    while (elapsed < wait)\n"
        "                    {\n"
        "                        if (!_paused)\n"
        "                        {\n"
        "                            elapsed += Time.unscaledDeltaTime;\n"
        "                            if (IsGeneralBattleMode() && wait > 0.001f && gap > 0.001f)\n"
        "                            {\n"
        "                                float u = Mathf.Clamp01(elapsed / wait);\n"
        "                                SetRoundHud(evt.roundNumber, FormatClock(shown + gap * u));\n"
        "                            }\n"
        "                        }\n"
        "                        yield return null;\n"
        "                    }\n"
        "                    while (_paused)\n"
        "                        yield return null;\n"
        "                }\n"
        "                else\n"
        "                    previousTime = evt.roundTime;\n"
        "\n"
        "                ApplyEvent(evt, animate);\n",
    )

    text = must_replace(
        text,
        "        private bool IsGeneralBattleMode() =>\n"
        "            _activeBattleSimMode == BattleSimMode.BehaviorTree ||\n"
        "            (_session != null && _session.BattleSimMode == BattleSimMode.BehaviorTree);\n",
        "        private bool IsGeneralBattleMode() =>\n"
        "            _activeBattleSimMode == BattleSimMode.BehaviorTree ||\n"
        "            (_session != null && _session.BattleSimMode == BattleSimMode.BehaviorTree);\n"
        "\n"
        "        /// <summary>\n"
        "        /// General mode often has long idle gaps before timeout; cap waits so playback\n"
        "        /// does not freeze the UI for ~100s with no events.\n"
        "        /// </summary>\n"
        "        private float GeneralModePlaybackWait(float roundTimeGap)\n"
        "        {\n"
        "            float speed = Mathf.Max(0.25f, playbackSpeed);\n"
        "            float wait = Mathf.Max(0f, roundTimeGap) / speed;\n"
        "            if (!IsGeneralBattleMode())\n"
        "                return wait;\n"
        "            // Keep some pacing, but never stall longer than this on empty time.\n"
        "            return Mathf.Min(wait, 0.45f);\n"
        "        }\n",
    )

    # Ignore clock heartbeat in vitality parser
    text = must_replace(
        text,
        "            if (evt.message.StartsWith(\"hp:\", StringComparison.OrdinalIgnoreCase))\n"
        "            {\n",
        "            if (evt.message.StartsWith(\"clock:\", StringComparison.OrdinalIgnoreCase))\n"
        "                return;\n"
        "            if (evt.message.StartsWith(\"hp:\", StringComparison.OrdinalIgnoreCase))\n"
        "            {\n",
    )

    if "叫暫停" not in text and "KillFeed" not in text:
        raise RuntimeError("UTF-8 corruption suspected after patch")

    TARGET.write_text(text, encoding="utf-8")
    print("patched", TARGET)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
