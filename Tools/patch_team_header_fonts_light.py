# -*- coding: utf-8 -*-
"""Team header: Anton for name/money/CP, Noto for tactics card, light accent bg."""
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
    bak.backup("before_team_header_fonts_light")
    text = TARGET.read_text(encoding="utf-8")
    if "擊殺" not in text or "跳過結算中" not in text:
        raise RuntimeError("UTF-8 corrupted")

    if "_leftCardText" not in text:
        text = must_replace(
            text,
            "        private Text _leftMoneyText;\n"
            "        private Text _rightMoneyText;\n",
            "        private Text _leftMoneyText;\n"
            "        private Text _rightMoneyText;\n"
            "        private Text _leftCardText;\n"
            "        private Text _rightCardText;\n",
        )

    old_headers = """            // Independent team headers (strategy line uses GenYoMin Bold).
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

    new_headers = """            // Team headers: light accent bg; Anton for name/money/CP; Noto for tactics card Chinese.
            Font antonFont = _scoreboardFont ?? _uiFont;
            Font tacticsFont = _uiFont ?? _uiFont700;
            Color headerInk = BattleStatPanelPresentation.CtDark;
            _leftTeamHeaderBlock = CreateUiObject<Image>("LeftTeamHeader", canvas.transform);
            SetRect(_leftTeamHeaderBlock.rectTransform, new Vector2(0.008f, 0.782f), new Vector2(0.175f, 0.835f));
            _leftTeamHeaderBlock.color = BattleStatPanelPresentation.CtLight;
            _leftTeamHeaderBlock.raycastTarget = false;
            _leftTeamHeader = CreateText("LeftHeader", _leftTeamHeaderBlock.transform, 17, TextAnchor.MiddleLeft, headerInk, antonFont);
            SetRect(_leftTeamHeader.rectTransform, new Vector2(0.03f, 0.50f), new Vector2(0.97f, 0.96f));
            _leftTeamHeader.fontStyle = FontStyle.Bold;
            _leftMoneyText = CreateText("LeftMoney", _leftTeamHeaderBlock.transform, 13, TextAnchor.MiddleLeft, headerInk, antonFont);
            SetRect(_leftMoneyText.rectTransform, new Vector2(0.03f, 0.04f), new Vector2(0.58f, 0.52f));
            _leftCardText = CreateText("LeftCard", _leftTeamHeaderBlock.transform, 12, TextAnchor.MiddleRight, headerInk, tacticsFont);
            SetRect(_leftCardText.rectTransform, new Vector2(0.58f, 0.04f), new Vector2(0.97f, 0.52f));
            _leftCardText.fontStyle = FontStyle.Bold;

            _rightTeamHeaderBlock = CreateUiObject<Image>("RightTeamHeader", canvas.transform);
            SetRect(_rightTeamHeaderBlock.rectTransform, new Vector2(0.825f, 0.782f), new Vector2(0.992f, 0.835f));
            _rightTeamHeaderBlock.color = BattleStatPanelPresentation.TLight;
            _rightTeamHeaderBlock.raycastTarget = false;
            _rightTeamHeader = CreateText("RightHeader", _rightTeamHeaderBlock.transform, 17, TextAnchor.MiddleLeft, headerInk, antonFont);
            SetRect(_rightTeamHeader.rectTransform, new Vector2(0.03f, 0.50f), new Vector2(0.97f, 0.96f));
            _rightTeamHeader.fontStyle = FontStyle.Bold;
            _rightMoneyText = CreateText("RightMoney", _rightTeamHeaderBlock.transform, 13, TextAnchor.MiddleLeft, headerInk, antonFont);
            SetRect(_rightMoneyText.rectTransform, new Vector2(0.03f, 0.04f), new Vector2(0.58f, 0.52f));
            _rightCardText = CreateText("RightCard", _rightTeamHeaderBlock.transform, 12, TextAnchor.MiddleRight, headerInk, tacticsFont);
            SetRect(_rightCardText.rectTransform, new Vector2(0.58f, 0.04f), new Vector2(0.97f, 0.52f));
            _rightCardText.fontStyle = FontStyle.Bold;"""

    if "_leftCardText = CreateText" not in text:
        text = must_replace(text, old_headers, new_headers)

    old_economy = """        private void UpdateEconomyHud()
        {
            string leftId = myTeamId;
            string rightId = _opponentTeamId;
            if (_leftMoneyText != null)
            {
                _leftMoneyText.color = TextPrimary;
                _leftMoneyText.text = FormatEconomyLine(leftId);
            }
            if (_rightMoneyText != null)
            {
                _rightMoneyText.color = TextPrimary;
                _rightMoneyText.text = FormatEconomyLine(rightId);
            }
        }"""

    new_economy = """        private void UpdateEconomyHud()
        {
            string leftId = myTeamId;
            string rightId = _opponentTeamId;
            bool leftIsT = TeamIsTerrorist(leftId, _currentRound);
            bool rightIsT = TeamIsTerrorist(rightId, _currentRound);
            Color leftInk = BattleStatPanelPresentation.PanelColor(leftIsT);
            Color rightInk = BattleStatPanelPresentation.PanelColor(rightIsT);
            if (_leftMoneyText != null)
            {
                _leftMoneyText.color = leftInk;
                _leftMoneyText.text = FormatEconomyMoneyCp(leftId);
            }
            if (_leftCardText != null)
            {
                _leftCardText.color = leftInk;
                _leftCardText.text = FormatEconomyCardName(leftId);
            }
            if (_rightMoneyText != null)
            {
                _rightMoneyText.color = rightInk;
                _rightMoneyText.text = FormatEconomyMoneyCp(rightId);
            }
            if (_rightCardText != null)
            {
                _rightCardText.color = rightInk;
                _rightCardText.text = FormatEconomyCardName(rightId);
            }
        }"""

    if "FormatEconomyMoneyCp" not in text:
        text = must_replace(text, old_economy, new_economy)

    old_format = """        private string FormatEconomyLine(string teamId)
        {
            int money = _teamMoney.TryGetValue(teamId, out int value) ? value : EconomyRules.StartingTeamMoney;
            EquipmentLevel card = _teamCards.TryGetValue(teamId, out EquipmentLevel level) ? level : EquipmentLevel.Eco;
            int cp = _session != null ? _session.TeamById(teamId).CommandPoints : CommandPointRules.StartingPoints;
            return $"${money:N0} · CP {cp} · {EconomyRules.CardName(card)}";
        }"""

    new_format = """        private string FormatEconomyMoneyCp(string teamId)
        {
            int money = _teamMoney.TryGetValue(teamId, out int value) ? value : EconomyRules.StartingTeamMoney;
            int cp = _session != null ? _session.TeamById(teamId).CommandPoints : CommandPointRules.StartingPoints;
            return $"${money:N0}  CP {cp}";
        }

        private string FormatEconomyCardName(string teamId)
        {
            EquipmentLevel card = _teamCards.TryGetValue(teamId, out EquipmentLevel level) ? level : EquipmentLevel.Eco;
            return EconomyRules.CardName(card);
        }"""

    if "FormatEconomyMoneyCp" not in text or "private string FormatEconomyLine" in text:
        if "private string FormatEconomyLine" in text:
            text = must_replace(text, old_format, new_format)

    old_headers_update = """        private void UpdateTeamHeaders()
        {
            string leftId = myTeamId;
            string rightId = _opponentTeamId;
            if (_leftTeamHeader != null)
            {
                _leftTeamHeader.text = DemoZh.TeamName(leftId);
                _leftTeamHeader.color = TextPrimary;
            }
            if (_rightTeamHeader != null)
            {
                _rightTeamHeader.text = DemoZh.TeamName(rightId);
                _rightTeamHeader.color = TextPrimary;
            }
            UpdateEconomyHud();
        }"""

    new_headers_update = """        private void UpdateTeamHeaders()
        {
            string leftId = myTeamId;
            string rightId = _opponentTeamId;
            bool leftIsT = TeamIsTerrorist(leftId, _currentRound);
            bool rightIsT = TeamIsTerrorist(rightId, _currentRound);
            Color leftInk = BattleStatPanelPresentation.PanelColor(leftIsT);
            Color rightInk = BattleStatPanelPresentation.PanelColor(rightIsT);
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
        }"""

    if "leftInk = BattleStatPanelPresentation.PanelColor(leftIsT)" not in text.split("UpdateTeamHeaders")[1][:800]:
        text = must_replace(text, old_headers_update, new_headers_update)

    old_side = """            _leftTeamHeaderBlock.color = BattleStatPanelPresentation.PanelColor(leftIsT);
            _rightTeamHeaderBlock.color = BattleStatPanelPresentation.PanelColor(rightIsT);"""
    new_side = """            _leftTeamHeaderBlock.color = BattleStatPanelPresentation.AccentColor(leftIsT);
            _rightTeamHeaderBlock.color = BattleStatPanelPresentation.AccentColor(rightIsT);"""
    if "AccentColor(leftIsT)" not in text or old_side in text:
        if old_side in text:
            text = must_replace(text, old_side, new_side)

    TARGET.write_text(text, encoding="utf-8")
    verify = TARGET.read_text(encoding="utf-8")
    if "擊殺" not in verify or "跳過結算中" not in verify:
        raise RuntimeError("UTF-8 corrupted after write")
    if "_leftCardText" not in verify or "FormatEconomyMoneyCp" not in verify:
        raise RuntimeError("header split incomplete")
    if "BattleStatPanelPresentation.AccentColor(leftIsT)" not in verify:
        raise RuntimeError("light header bg not wired")
    print("patched team header fonts/light bg OK")


if __name__ == "__main__":
    main()
