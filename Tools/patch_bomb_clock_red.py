# -*- coding: utf-8 -*-
"""When bomb is planted, HUD clock turns red for the 40s countdown."""
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"
ANCHOR = "叫暫停"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"anchor missing:\n{old[:240]!r}")
    return text.replace(old, new, 1)


def main() -> int:
    subprocess.check_call(
        [sys.executable, str(ROOT / "Tools" / "backup_quickmatch.py"), "bomb_clock_red"]
    )
    text = TARGET.read_text(encoding="utf-8")
    if "BombClockRed" in text and "ApplyClockPlantedColor" in text:
        print("already patched")
        return 0

    text = must_replace(
        text,
        "        private static readonly Color OfficialLossUi = new Color(0.94f, 0.40f, 0.36f, 1f);\n",
        "        private static readonly Color OfficialLossUi = new Color(0.94f, 0.40f, 0.36f, 1f);\n"
        "        private static readonly Color BombClockRed = new Color(0.95f, 0.18f, 0.18f, 1f);\n",
    )

    text = must_replace(
        text,
        "        private void SetRoundHud(int roundNumber, string clockOrStatus)\n"
        "        {\n"
        "            if (_roundText != null)\n"
        "                _roundText.text = $\"ROUND {Mathf.Max(1, roundNumber):00}\";\n"
        "            if (_clockText != null)\n"
        "                _clockText.text = string.IsNullOrEmpty(clockOrStatus) ? string.Empty : clockOrStatus;\n"
        "        }\n",
        "        private void SetRoundHud(int roundNumber, string clockOrStatus)\n"
        "        {\n"
        "            if (_roundText != null)\n"
        "                _roundText.text = $\"ROUND {Mathf.Max(1, roundNumber):00}\";\n"
        "            if (_clockText != null)\n"
        "            {\n"
        "                _clockText.text = string.IsNullOrEmpty(clockOrStatus) ? string.Empty : clockOrStatus;\n"
        "                ApplyClockPlantedColor();\n"
        "            }\n"
        "        }\n"
        "\n"
        "        private void ApplyClockPlantedColor()\n"
        "        {\n"
        "            if (_clockText == null)\n"
        "                return;\n"
        "            _clockText.color = _bombPlantedAtRoundTime >= 0f ? BombClockRed : TextPrimary;\n"
        "        }\n",
    )

    # After plant cleared, restore white even if SetRoundHud already ran with planted=true.
    text = must_replace(
        text,
        "                    _bombPlantedAtRoundTime = -1f;\n"
        "                    HideBomb();\n"
        "                    _roundHasDeath = false;\n",
        "                    _bombPlantedAtRoundTime = -1f;\n"
        "                    ApplyClockPlantedColor();\n"
        "                    HideBomb();\n"
        "                    _roundHasDeath = false;\n",
    )

    # Defused / exploded: clear then restore color (may appear more than once — replace all).
    old_clear = (
        "                    _bombPlantedAtRoundTime = -1f;\n"
        "                    HideBomb();\n"
    )
    new_clear = (
        "                    _bombPlantedAtRoundTime = -1f;\n"
        "                    ApplyClockPlantedColor();\n"
        "                    HideBomb();\n"
    )
    if old_clear not in text:
        raise RuntimeError("defuse/explode clear anchor missing")
    text = text.replace(old_clear, new_clear)

    if ANCHOR not in text:
        raise RuntimeError("UTF-8 corruption")
    TARGET.write_text(text, encoding="utf-8")
    print("patched", TARGET)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
