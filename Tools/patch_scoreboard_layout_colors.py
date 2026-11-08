# -*- coding: utf-8 -*-
"""Scoreboard polish: CT/T colors, team header Bold, Bind panel/accent colors."""
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
    bak.backup("before_scoreboard_layout_colors")
    text = TARGET.read_text(encoding="utf-8")
    if "擊殺" not in text or "跳過結算中" not in text:
        raise RuntimeError("UTF-8 corrupted")

    # Team header: GenYoMin Bold + larger sizes + dark panel tint defaults
    old_headers = """            // Independent team headers (not part of a shared side block).
            _leftTeamHeaderBlock = CreateUiObject<Image>("LeftTeamHeader", canvas.transform);
            SetRect(_leftTeamHeaderBlock.rectTransform, new Vector2(0.008f, 0.782f), new Vector2(0.165f, 0.828f));
            _leftTeamHeaderBlock.color = new Color(CtPanel.r, CtPanel.g, CtPanel.b, 0.82f);
            _leftTeamHeaderBlock.raycastTarget = false;
            _leftTeamHeader = CreateText("LeftHeader", _leftTeamHeaderBlock.transform, 15, TextAnchor.MiddleLeft, TextPrimary, _scoreboardFont);
            SetRect(_leftTeamHeader.rectTransform, new Vector2(0.04f, 0.08f), new Vector2(0.42f, 0.92f));
            _leftTeamHeader.fontStyle = FontStyle.Bold;
            _leftMoneyText = CreateText("LeftMoney", _leftTeamHeaderBlock.transform, 11, TextAnchor.MiddleRight, TextPrimary, _scoreboardFont);
            SetRect(_leftMoneyText.rectTransform, new Vector2(0.42f, 0.08f), new Vector2(0.96f, 0.92f));

            _rightTeamHeaderBlock = CreateUiObject<Image>("RightTeamHeader", canvas.transform);
            SetRect(_rightTeamHeaderBlock.rectTransform, new Vector2(0.835f, 0.782f), new Vector2(0.992f, 0.828f));
            _rightTeamHeaderBlock.color = new Color(TPanel.r, TPanel.g, TPanel.b, 0.82f);
            _rightTeamHeaderBlock.raycastTarget = false;
            _rightTeamHeader = CreateText("RightHeader", _rightTeamHeaderBlock.transform, 15, TextAnchor.MiddleLeft, TextPrimary, _scoreboardFont);
            SetRect(_rightTeamHeader.rectTransform, new Vector2(0.04f, 0.08f), new Vector2(0.42f, 0.92f));
            _rightTeamHeader.fontStyle = FontStyle.Bold;
            _rightMoneyText = CreateText("RightMoney", _rightTeamHeaderBlock.transform, 11, TextAnchor.MiddleRight, TextPrimary, _scoreboardFont);
            SetRect(_rightMoneyText.rectTransform, new Vector2(0.42f, 0.08f), new Vector2(0.96f, 0.92f));"""

    new_headers = """            // Independent team headers (strategy line uses GenYoMin Bold).
            Font strategyFont = _uiFont700 ?? _uiFont;
            _leftTeamHeaderBlock = CreateUiObject<Image>("LeftTeamHeader", canvas.transform);
            SetRect(_leftTeamHeaderBlock.rectTransform, new Vector2(0.008f, 0.782f), new Vector2(0.175f, 0.835f));
            _leftTeamHeaderBlock.color = BattleStatPanelPresentation.CtDark;
            _leftTeamHeaderBlock.raycastTarget = false;
            _leftTeamHeader = CreateText("LeftHeader", _leftTeamHeaderBlock.transform, 18, TextAnchor.MiddleLeft, TextPrimary, strategyFont);
            SetRect(_leftTeamHeader.rectTransform, new Vector2(0.04f, 0.08f), new Vector2(0.40f, 0.92f));
            _leftTeamHeader.fontStyle = FontStyle.Bold;
            _leftMoneyText = CreateText("LeftMoney", _leftTeamHeaderBlock.transform, 14, TextAnchor.MiddleRight, TextPrimary, strategyFont);
            SetRect(_leftMoneyText.rectTransform, new Vector2(0.40f, 0.08f), new Vector2(0.96f, 0.92f));
            _leftMoneyText.fontStyle = FontStyle.Bold;

            _rightTeamHeaderBlock = CreateUiObject<Image>("RightTeamHeader", canvas.transform);
            SetRect(_rightTeamHeaderBlock.rectTransform, new Vector2(0.825f, 0.782f), new Vector2(0.992f, 0.835f));
            _rightTeamHeaderBlock.color = BattleStatPanelPresentation.TDark;
            _rightTeamHeaderBlock.raycastTarget = false;
            _rightTeamHeader = CreateText("RightHeader", _rightTeamHeaderBlock.transform, 18, TextAnchor.MiddleLeft, TextPrimary, strategyFont);
            SetRect(_rightTeamHeader.rectTransform, new Vector2(0.04f, 0.08f), new Vector2(0.40f, 0.92f));
            _rightTeamHeader.fontStyle = FontStyle.Bold;
            _rightMoneyText = CreateText("RightMoney", _rightTeamHeaderBlock.transform, 14, TextAnchor.MiddleRight, TextPrimary, strategyFont);
            SetRect(_rightMoneyText.rectTransform, new Vector2(0.40f, 0.08f), new Vector2(0.96f, 0.92f));
            _rightMoneyText.fontStyle = FontStyle.Bold;"""

    if "strategyFont" not in text:
        text = must_replace(text, old_headers, new_headers)

    # UpdateSidePanelColors → tint independent headers with side dark colors
    old_colors = """        private void UpdateSidePanelColors()
        {
            if (_leftStatsPanel == null || _rightStatsPanel == null)
                return;

            string leftId = myTeamId;
            string rightId = _opponentTeamId;
            bool leftIsT = TeamIsTerrorist(leftId, _currentRound);
            bool rightIsT = TeamIsTerrorist(rightId, _currentRound);
            _leftStatsPanel.color = leftIsT ? TPanel : CtPanel;
            _rightStatsPanel.color = rightIsT ? TPanel : CtPanel;
            RefreshMarkerSideColors();
            RefreshStatPanels();
            UpdateAliveCountHud();
        }"""

    new_colors = """        private void UpdateSidePanelColors()
        {
            if (_leftTeamHeaderBlock == null || _rightTeamHeaderBlock == null)
                return;

            string leftId = myTeamId;
            string rightId = _opponentTeamId;
            bool leftIsT = TeamIsTerrorist(leftId, _currentRound);
            bool rightIsT = TeamIsTerrorist(rightId, _currentRound);
            _leftTeamHeaderBlock.color = BattleStatPanelPresentation.PanelColor(leftIsT);
            _rightTeamHeaderBlock.color = BattleStatPanelPresentation.PanelColor(rightIsT);
            if (_leftStatsPanel != null)
                _leftStatsPanel.color = Color.clear;
            if (_rightStatsPanel != null)
                _rightStatsPanel.color = Color.clear;
            RefreshMarkerSideColors();
            RefreshStatPanels();
            UpdateAliveCountHud();
        }"""

    if "BattleStatPanelPresentation.PanelColor" not in text:
        text = must_replace(text, old_colors, new_colors)

    # Bind with panel + accent colors
    old_bind_colors = """            bool teamIsT = TeamIsTerrorist(team.id, _currentRound);
            Color sideColor = SideBadgeColor(teamIsT);
            bool showAssists = _result != null || (_session != null && _session.IsFinished);"""
    new_bind_colors = """            bool teamIsT = TeamIsTerrorist(team.id, _currentRound);
            Color panelDark = BattleStatPanelPresentation.PanelColor(teamIsT);
            Color accentLight = BattleStatPanelPresentation.AccentColor(teamIsT);
            bool showAssists = _result != null || (_session != null && _session.IsFinished);"""
    if "Color panelDark = BattleStatPanelPresentation.PanelColor" not in text:
        text = must_replace(text, old_bind_colors, new_bind_colors)

    if "BattleStatPanelPresentation.Bind(card, data, sideColor);" in text:
        text = must_replace(
            text,
            "BattleStatPanelPresentation.Bind(card, data, sideColor);",
            "BattleStatPanelPresentation.Bind(card, data, panelDark, accentLight);",
        )
    elif "BattleStatPanelPresentation.Bind(card, data, panelDark, accentLight);" not in text:
        raise RuntimeError("Bind call missing")

    TARGET.write_text(text, encoding="utf-8")
    verify = TARGET.read_text(encoding="utf-8")
    if "擊殺" not in verify or "跳過結算中" not in verify:
        raise RuntimeError("UTF-8 corrupted after write")
    print("patched scoreboard layout/colors OK")


if __name__ == "__main__":
    main()
