# -*- coding: utf-8 -*-
"""Team header/economy/CP: opaque faction-dark ink; header bar opaque faction-light."""
from pathlib import Path
import backup_quickmatch as bak

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing:\n{old[:220]!r}")
    return text.replace(old, new, 1)


def apply(text: str) -> str:
    text = must_replace(
        text,
        """        private void UpdateEconomyHud()
        {
            string leftId = myTeamId;
            string rightId = _opponentTeamId;
            bool leftIsT = TeamIsTerrorist(leftId, _currentRound);
            bool rightIsT = TeamIsTerrorist(rightId, _currentRound);
            bool general = IsGeneralBattleMode();
            Color leftInk = general ? BattleStatPanelPresentation.PanelColor(leftIsT) : TextPrimary;
            Color rightInk = general ? BattleStatPanelPresentation.PanelColor(rightIsT) : TextPrimary;
            if (general)
            {
                BindTeamEconomyTexts(leftId, leftInk, _leftMoneyText, _leftCpText);
                BindTeamEconomyTexts(rightId, rightInk, _rightMoneyText, _rightCpText);
            }
            else
            {
                if (_leftMoneyText != null)
                {
                    _leftMoneyText.color = leftInk;
                    _leftMoneyText.text = FormatEconomyMoney(leftId) + " · " + FormatEconomyCp(leftId);
                }
                if (_rightMoneyText != null)
                {
                    _rightMoneyText.color = rightInk;
                    _rightMoneyText.text = FormatEconomyMoney(rightId) + " · " + FormatEconomyCp(rightId);
                }
            }
        }
""",
        """        private void UpdateEconomyHud()
        {
            string leftId = myTeamId;
            string rightId = _opponentTeamId;
            bool leftIsT = TeamIsTerrorist(leftId, _currentRound);
            bool rightIsT = TeamIsTerrorist(rightId, _currentRound);
            Color leftInk = BattleStatPanelPresentation.Opaque(
                BattleStatPanelPresentation.PanelColor(leftIsT));
            Color rightInk = BattleStatPanelPresentation.Opaque(
                BattleStatPanelPresentation.PanelColor(rightIsT));
            if (IsGeneralBattleMode())
            {
                BindTeamEconomyTexts(leftId, leftInk, _leftMoneyText, _leftCpText);
                BindTeamEconomyTexts(rightId, rightInk, _rightMoneyText, _rightCpText);
            }
            else
            {
                if (_leftMoneyText != null)
                {
                    _leftMoneyText.color = leftInk;
                    _leftMoneyText.text = FormatEconomyMoney(leftId) + " · " + FormatEconomyCp(leftId);
                }
                if (_rightMoneyText != null)
                {
                    _rightMoneyText.color = rightInk;
                    _rightMoneyText.text = FormatEconomyMoney(rightId) + " · " + FormatEconomyCp(rightId);
                }
            }
        }
""",
    )

    text = must_replace(
        text,
        """        private void UpdateTeamHeaders()
        {
            string leftId = myTeamId;
            string rightId = _opponentTeamId;
            bool leftIsT = TeamIsTerrorist(leftId, _currentRound);
            bool rightIsT = TeamIsTerrorist(rightId, _currentRound);
            bool general = IsGeneralBattleMode();
            Color leftInk = general ? BattleStatPanelPresentation.PanelColor(leftIsT) : TextPrimary;
            Color rightInk = general ? BattleStatPanelPresentation.PanelColor(rightIsT) : TextPrimary;
            if (_leftTeamHeader != null)
            {
                _leftTeamHeader.text = DemoZh.TeamName(leftId);
                _leftTeamHeader.color = leftInk;
            }
            if (_rightTeamHeader != null)
            {
                _rightTeamHeader.text = DemoZh.TeamName(rightId);
                _rightTeamHeader.color = rightInk;
            }
            UpdateEconomyHud();
        }
""",
        """        private void UpdateTeamHeaders()
        {
            string leftId = myTeamId;
            string rightId = _opponentTeamId;
            bool leftIsT = TeamIsTerrorist(leftId, _currentRound);
            bool rightIsT = TeamIsTerrorist(rightId, _currentRound);
            Color leftInk = BattleStatPanelPresentation.Opaque(
                BattleStatPanelPresentation.PanelColor(leftIsT));
            Color rightInk = BattleStatPanelPresentation.Opaque(
                BattleStatPanelPresentation.PanelColor(rightIsT));
            if (_leftTeamHeader != null)
            {
                _leftTeamHeader.text = DemoZh.TeamName(leftId);
                _leftTeamHeader.color = leftInk;
            }
            if (_rightTeamHeader != null)
            {
                _rightTeamHeader.text = DemoZh.TeamName(rightId);
                _rightTeamHeader.color = rightInk;
            }
            UpdateEconomyHud();
        }
""",
    )

    text = must_replace(
        text,
        """        private void UpdateSidePanelColors()
        {
            if (_leftStatsPanel == null || _rightStatsPanel == null)
                return;

            string leftId = myTeamId;
            string rightId = _opponentTeamId;
            bool leftIsT = TeamIsTerrorist(leftId, _currentRound);
            bool rightIsT = TeamIsTerrorist(rightId, _currentRound);
            bool general = IsGeneralBattleMode();
            if (general)
            {
                if (_leftTeamHeaderBlock != null)
                    _leftTeamHeaderBlock.color = BattleStatPanelPresentation.AccentColor(leftIsT);
                if (_rightTeamHeaderBlock != null)
                    _rightTeamHeaderBlock.color = BattleStatPanelPresentation.AccentColor(rightIsT);
                _leftStatsPanel.color = Color.clear;
                _rightStatsPanel.color = Color.clear;
            }
            else
            {
                Color leftPanel = leftIsT ? TPanel : CtPanel;
                Color rightPanel = rightIsT ? TPanel : CtPanel;
                _leftStatsPanel.color = leftPanel;
                _rightStatsPanel.color = rightPanel;
                if (_leftTeamHeaderBlock != null)
                    _leftTeamHeaderBlock.color = leftPanel;
                if (_rightTeamHeaderBlock != null)
                    _rightTeamHeaderBlock.color = rightPanel;
            }
            RefreshMarkerSideColors();
            RefreshStatPanels();
            UpdateAliveCountHud();
        }
""",
        """        private void UpdateSidePanelColors()
        {
            if (_leftStatsPanel == null || _rightStatsPanel == null)
                return;

            string leftId = myTeamId;
            string rightId = _opponentTeamId;
            bool leftIsT = TeamIsTerrorist(leftId, _currentRound);
            bool rightIsT = TeamIsTerrorist(rightId, _currentRound);
            // Header bar: opaque faction light so dark team/economy/CP ink stays readable.
            if (_leftTeamHeaderBlock != null)
            {
                BattleStatPanelPresentation.ApplyOpaqueFill(
                    _leftTeamHeaderBlock,
                    BattleStatPanelPresentation.AccentColor(leftIsT));
            }
            if (_rightTeamHeaderBlock != null)
            {
                BattleStatPanelPresentation.ApplyOpaqueFill(
                    _rightTeamHeaderBlock,
                    BattleStatPanelPresentation.AccentColor(rightIsT));
            }
            if (IsGeneralBattleMode())
            {
                _leftStatsPanel.color = Color.clear;
                _rightStatsPanel.color = Color.clear;
            }
            else
            {
                Color leftPanel = leftIsT ? TPanel : CtPanel;
                Color rightPanel = rightIsT ? TPanel : CtPanel;
                _leftStatsPanel.color = leftPanel;
                _rightStatsPanel.color = rightPanel;
            }
            RefreshMarkerSideColors();
            RefreshStatPanels();
            UpdateAliveCountHud();
        }
""",
    )

    # Initial create colors: use Opaque accents + dark ink (already CtDark via headerInk).
    text = must_replace(
        text,
        """            Color headerInk = BattleStatPanelPresentation.CtDark;
""",
        """            Color headerInk = BattleStatPanelPresentation.Opaque(BattleStatPanelPresentation.CtDark);
""",
    )
    text = must_replace(
        text,
        """            _leftTeamHeaderBlock.color = BattleStatPanelPresentation.CtLight;
""",
        """            _leftTeamHeaderBlock.color = BattleStatPanelPresentation.Opaque(BattleStatPanelPresentation.CtLight);
""",
    )
    text = must_replace(
        text,
        """            _rightTeamHeaderBlock.color = BattleStatPanelPresentation.TLight;
""",
        """            _rightTeamHeaderBlock.color = BattleStatPanelPresentation.Opaque(BattleStatPanelPresentation.TLight);
""",
    )
    return text


def main() -> None:
    bak.backup("team_header_opaque_dark_ink")
    original = TARGET.read_text(encoding="utf-8")
    updated = apply(original)
    if "擊殺" not in updated or "跳過" not in updated:
        raise RuntimeError("Chinese anchors lost")
    if "ApplyControlBarLayout(simulation)" not in updated:
        raise RuntimeError("control bar polarity lost")
    if "BattleStatPanelPresentation.Opaque" not in updated:
        raise RuntimeError("Opaque ink not applied")
    if "general ? BattleStatPanelPresentation.PanelColor(leftIsT) : TextPrimary" in updated:
        raise RuntimeError("official still uses TextPrimary for header ink")
    TARGET.write_text(updated, encoding="utf-8")
    print("patched", TARGET)


if __name__ == "__main__":
    main()
