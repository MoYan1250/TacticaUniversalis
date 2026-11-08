# -*- coding: utf-8 -*-
"""General-mode settings to center; lower scoreboard columns."""
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
import backup_quickmatch as bak

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing:\n{old[:240]!r}")
    return text.replace(old, new, 1)


def main() -> None:
    bak.backup("before_settings_center_stats_lower")
    text = TARGET.read_text(encoding="utf-8")
    if "擊殺" not in text or "跳過結算中" not in text:
        raise RuntimeError("UTF-8 corrupted")

    old_layout = """        private void ApplyControlBarLayout(bool practiceLayout)
        {
            // Settings always bottom-right (outside the compact official bar).
            if (_settingsButtonRect != null)
                SetRect(_settingsButtonRect, new Vector2(0.905f, 0.015f), new Vector2(0.985f, 0.100f));
"""

    new_layout = """        private void ApplyControlBarLayout(bool practiceLayout)
        {
            // General mode: settings sits in the center control UI so side scoreboards can use the bottom.
            // Other modes: keep settings bottom-right outside the compact official bar.
            if (_settingsButtonRect != null)
            {
                if (IsGeneralBattleMode())
                    SetRect(_settingsButtonRect, new Vector2(0.455f, 0.018f), new Vector2(0.545f, 0.095f));
                else
                    SetRect(_settingsButtonRect, new Vector2(0.905f, 0.015f), new Vector2(0.985f, 0.100f));
            }
"""

    if "IsGeneralBattleMode())\n                    SetRect(_settingsButtonRect" not in text:
        text = must_replace(text, old_layout, new_layout)

    # Lower team headers + scoreboard columns (free bottom for cards; settings moved center in general).
    old_left_header = """            SetRect(_leftTeamHeaderBlock.rectTransform, new Vector2(0.008f, 0.782f), new Vector2(0.195f, 0.835f));"""
    new_left_header = """            SetRect(_leftTeamHeaderBlock.rectTransform, new Vector2(0.006f, 0.708f), new Vector2(0.205f, 0.758f));"""
    if old_left_header in text:
        text = must_replace(text, old_left_header, new_left_header)

    old_right_header = """            SetRect(_rightTeamHeaderBlock.rectTransform, new Vector2(0.805f, 0.782f), new Vector2(0.992f, 0.835f));"""
    new_right_header = """            SetRect(_rightTeamHeaderBlock.rectTransform, new Vector2(0.795f, 0.708f), new Vector2(0.994f, 0.758f));"""
    if old_right_header in text:
        text = must_replace(text, old_right_header, new_right_header)

    old_left_stats = """            SetRect(leftPanel.rectTransform, new Vector2(0.006f, 0.112f), new Vector2(0.205f, 0.775f));"""
    new_left_stats = """            SetRect(leftPanel.rectTransform, new Vector2(0.006f, 0.018f), new Vector2(0.205f, 0.700f));"""
    if old_left_stats in text:
        text = must_replace(text, old_left_stats, new_left_stats)

    old_right_stats = """            SetRect(rightPanel.rectTransform, new Vector2(0.795f, 0.112f), new Vector2(0.994f, 0.775f));"""
    new_right_stats = """            SetRect(rightPanel.rectTransform, new Vector2(0.795f, 0.018f), new Vector2(0.994f, 0.700f));"""
    if old_right_stats in text:
        text = must_replace(text, old_right_stats, new_right_stats)

    TARGET.write_text(text, encoding="utf-8")
    verify = TARGET.read_text(encoding="utf-8")
    if "擊殺" not in verify or "跳過結算中" not in verify:
        raise RuntimeError("UTF-8 corrupted after write")
    if "0.455f, 0.018f" not in verify:
        raise RuntimeError("settings center not wired")
    if "0.018f), new Vector2(0.205f, 0.700f)" not in verify:
        raise RuntimeError("scoreboard not lowered")
    print("patched settings center + lowered scoreboard OK")


if __name__ == "__main__":
    main()
