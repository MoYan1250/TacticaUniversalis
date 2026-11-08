# -*- coding: utf-8 -*-
"""Align general-mode team header width with player cards; tighten card gaps;
match header↔card gap to card↔card gap via BattleStatPanelPresentation.GetGeneralColumnScreenRects.
"""
from pathlib import Path
import backup_quickmatch as bak

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing:\n{old[:240]!r}")
    return text.replace(old, new, 1)


def main() -> None:
    bak.backup("before_scoreboard_column_gap")
    text = TARGET.read_text(encoding="utf-8")
    if "擊殺" not in text and "跳過結算中" not in text:
        raise RuntimeError("UTF-8 anchor missing — abort")

    # ApplyScoreboardModeChrome general branch: use shared column rects
    old_general = """            if (general)
            {
                if (_leftTeamHeader != null)
                    SetRect(_leftTeamHeader.rectTransform, new Vector2(0.02f, 0.08f), new Vector2(0.30f, 0.92f));
                if (_leftMoneyText != null)
                    SetRect(_leftMoneyText.rectTransform, new Vector2(0.30f, 0.08f), new Vector2(0.50f, 0.92f));
                if (_rightTeamHeader != null)
                    SetRect(_rightTeamHeader.rectTransform, new Vector2(0.70f, 0.08f), new Vector2(0.98f, 0.92f));
                if (_rightMoneyText != null)
                    SetRect(_rightMoneyText.rectTransform, new Vector2(0.50f, 0.08f), new Vector2(0.70f, 0.92f));
                if (_leftTeamHeaderBlock != null)
                    SetRect(_leftTeamHeaderBlock.rectTransform, new Vector2(0.006f, 0.708f), new Vector2(0.205f, 0.758f));
                if (_rightTeamHeaderBlock != null)
                    SetRect(_rightTeamHeaderBlock.rectTransform, new Vector2(0.795f, 0.708f), new Vector2(0.994f, 0.758f));
                if (_leftStatsPanel != null)
                {
                    SetRect(_leftStatsPanel.rectTransform, new Vector2(0.006f, 0.018f), new Vector2(0.205f, 0.700f));
                    _leftStatsPanel.color = Color.clear;
                }
                if (_rightStatsPanel != null)
                {
                    SetRect(_rightStatsPanel.rectTransform, new Vector2(0.795f, 0.018f), new Vector2(0.994f, 0.700f));
                    _rightStatsPanel.color = Color.clear;
                }
            }"""

    new_general = """            if (general)
            {
                if (_leftTeamHeader != null)
                    SetRect(_leftTeamHeader.rectTransform, new Vector2(0.02f, 0.08f), new Vector2(0.30f, 0.92f));
                if (_leftMoneyText != null)
                    SetRect(_leftMoneyText.rectTransform, new Vector2(0.30f, 0.08f), new Vector2(0.50f, 0.92f));
                if (_rightTeamHeader != null)
                    SetRect(_rightTeamHeader.rectTransform, new Vector2(0.70f, 0.08f), new Vector2(0.98f, 0.92f));
                if (_rightMoneyText != null)
                    SetRect(_rightMoneyText.rectTransform, new Vector2(0.50f, 0.08f), new Vector2(0.70f, 0.92f));

                BattleStatPanelPresentation.GetGeneralColumnScreenRects(
                    true, out var leftHeaderMin, out var leftHeaderMax, out var leftPanelMin, out var leftPanelMax);
                BattleStatPanelPresentation.GetGeneralColumnScreenRects(
                    false, out var rightHeaderMin, out var rightHeaderMax, out var rightPanelMin, out var rightPanelMax);

                if (_leftTeamHeaderBlock != null)
                    SetRect(_leftTeamHeaderBlock.rectTransform, leftHeaderMin, leftHeaderMax);
                if (_rightTeamHeaderBlock != null)
                    SetRect(_rightTeamHeaderBlock.rectTransform, rightHeaderMin, rightHeaderMax);
                if (_leftStatsPanel != null)
                {
                    SetRect(_leftStatsPanel.rectTransform, leftPanelMin, leftPanelMax);
                    _leftStatsPanel.color = Color.clear;
                }
                if (_rightStatsPanel != null)
                {
                    SetRect(_rightStatsPanel.rectTransform, rightPanelMin, rightPanelMax);
                    _rightStatsPanel.color = Color.clear;
                }
            }"""

    text = must_replace(text, old_general, new_general)

    # Initial CreateUI positions for headers/panels — match shared layout
    old_create_left = """            _leftTeamHeaderBlock = CreateUiObject<Image>("LeftTeamHeader", canvas.transform);
            SetRect(_leftTeamHeaderBlock.rectTransform, new Vector2(0.006f, 0.708f), new Vector2(0.205f, 0.758f));"""
    new_create_left = """            BattleStatPanelPresentation.GetGeneralColumnScreenRects(
                true, out var createLeftHeaderMin, out var createLeftHeaderMax, out var createLeftPanelMin, out var createLeftPanelMax);
            BattleStatPanelPresentation.GetGeneralColumnScreenRects(
                false, out var createRightHeaderMin, out var createRightHeaderMax, out var createRightPanelMin, out var createRightPanelMax);

            _leftTeamHeaderBlock = CreateUiObject<Image>("LeftTeamHeader", canvas.transform);
            SetRect(_leftTeamHeaderBlock.rectTransform, createLeftHeaderMin, createLeftHeaderMax);"""
    text = must_replace(text, old_create_left, new_create_left)

    old_create_right = """            _rightTeamHeaderBlock = CreateUiObject<Image>("RightTeamHeader", canvas.transform);
            SetRect(_rightTeamHeaderBlock.rectTransform, new Vector2(0.795f, 0.708f), new Vector2(0.994f, 0.758f));"""
    new_create_right = """            _rightTeamHeaderBlock = CreateUiObject<Image>("RightTeamHeader", canvas.transform);
            SetRect(_rightTeamHeaderBlock.rectTransform, createRightHeaderMin, createRightHeaderMax);"""
    text = must_replace(text, old_create_right, new_create_right)

    old_left_panel = """            SetRect(leftPanel.rectTransform, new Vector2(0.006f, 0.018f), new Vector2(0.205f, 0.700f));"""
    new_left_panel = """            SetRect(leftPanel.rectTransform, createLeftPanelMin, createLeftPanelMax);"""
    text = must_replace(text, old_left_panel, new_left_panel)

    # right panel initial rect — find nearby
    old_right_panel = None
    for candidate in (
        'SetRect(rightPanel.rectTransform, new Vector2(0.795f, 0.018f), new Vector2(0.994f, 0.700f));',
        'SetRect(_rightStatsPanel.rectTransform, new Vector2(0.795f, 0.018f), new Vector2(0.994f, 0.700f));',
    ):
        if candidate in text:
            old_right_panel = candidate
            break
    if old_right_panel is None:
        # fuzzy search
        idx = text.find("RightStats")
        raise RuntimeError(f"right panel SetRect missing near RightStats ctx={text[idx:idx+400]!r}")

    if "rightPanel.rectTransform" in old_right_panel:
        text = must_replace(
            text,
            old_right_panel,
            "SetRect(rightPanel.rectTransform, createRightPanelMin, createRightPanelMax);",
        )
    else:
        text = must_replace(
            text,
            old_right_panel,
            "SetRect(_rightStatsPanel.rectTransform, createRightPanelMin, createRightPanelMax);",
        )

    TARGET.write_text(text, encoding="utf-8")
    print("patched scoreboard column gap OK")


if __name__ == "__main__":
    main()
