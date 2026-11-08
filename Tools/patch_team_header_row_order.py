# -*- coding: utf-8 -*-
"""Team header single-row order: L name/money/CP/card; R mirrored."""
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
    bak.backup("before_team_header_row_order")
    text = TARGET.read_text(encoding="utf-8")
    if "擊殺" not in text or "跳過結算中" not in text:
        raise RuntimeError("UTF-8 corrupted")

    if "_leftCpText" not in text:
        text = must_replace(
            text,
            "        private Text _leftCardText;\n"
            "        private Text _rightCardText;\n",
            "        private Text _leftCpText;\n"
            "        private Text _rightCpText;\n"
            "        private Text _leftCardText;\n"
            "        private Text _rightCardText;\n",
        )

    old_headers = """            // Team headers: light accent bg; Anton for name/money/CP; Noto for tactics card Chinese.
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

    new_headers = """            // Team headers one row — L: club/money/CP/strategy; R mirrored.
            Font antonFont = _scoreboardFont ?? _uiFont;
            Font tacticsFont = _uiFont ?? _uiFont700;
            Color headerInk = BattleStatPanelPresentation.CtDark;
            _leftTeamHeaderBlock = CreateUiObject<Image>("LeftTeamHeader", canvas.transform);
            SetRect(_leftTeamHeaderBlock.rectTransform, new Vector2(0.008f, 0.782f), new Vector2(0.195f, 0.835f));
            _leftTeamHeaderBlock.color = BattleStatPanelPresentation.CtLight;
            _leftTeamHeaderBlock.raycastTarget = false;
            _leftTeamHeader = CreateText("LeftHeader", _leftTeamHeaderBlock.transform, 14, TextAnchor.MiddleLeft, headerInk, antonFont);
            SetRect(_leftTeamHeader.rectTransform, new Vector2(0.02f, 0.08f), new Vector2(0.30f, 0.92f));
            _leftTeamHeader.fontStyle = FontStyle.Bold;
            _leftTeamHeader.horizontalOverflow = HorizontalWrapMode.Overflow;
            _leftMoneyText = CreateText("LeftMoney", _leftTeamHeaderBlock.transform, 13, TextAnchor.MiddleCenter, headerInk, antonFont);
            SetRect(_leftMoneyText.rectTransform, new Vector2(0.30f, 0.08f), new Vector2(0.50f, 0.92f));
            _leftCpText = CreateText("LeftCp", _leftTeamHeaderBlock.transform, 13, TextAnchor.MiddleCenter, headerInk, antonFont);
            SetRect(_leftCpText.rectTransform, new Vector2(0.50f, 0.08f), new Vector2(0.66f, 0.92f));
            _leftCardText = CreateText("LeftCard", _leftTeamHeaderBlock.transform, 12, TextAnchor.MiddleRight, headerInk, tacticsFont);
            SetRect(_leftCardText.rectTransform, new Vector2(0.66f, 0.08f), new Vector2(0.98f, 0.92f));
            _leftCardText.fontStyle = FontStyle.Bold;
            _leftCardText.horizontalOverflow = HorizontalWrapMode.Overflow;

            _rightTeamHeaderBlock = CreateUiObject<Image>("RightTeamHeader", canvas.transform);
            SetRect(_rightTeamHeaderBlock.rectTransform, new Vector2(0.805f, 0.782f), new Vector2(0.992f, 0.835f));
            _rightTeamHeaderBlock.color = BattleStatPanelPresentation.TLight;
            _rightTeamHeaderBlock.raycastTarget = false;
            _rightCardText = CreateText("RightCard", _rightTeamHeaderBlock.transform, 12, TextAnchor.MiddleLeft, headerInk, tacticsFont);
            SetRect(_rightCardText.rectTransform, new Vector2(0.02f, 0.08f), new Vector2(0.34f, 0.92f));
            _rightCardText.fontStyle = FontStyle.Bold;
            _rightCardText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _rightCpText = CreateText("RightCp", _rightTeamHeaderBlock.transform, 13, TextAnchor.MiddleCenter, headerInk, antonFont);
            SetRect(_rightCpText.rectTransform, new Vector2(0.34f, 0.08f), new Vector2(0.50f, 0.92f));
            _rightMoneyText = CreateText("RightMoney", _rightTeamHeaderBlock.transform, 13, TextAnchor.MiddleCenter, headerInk, antonFont);
            SetRect(_rightMoneyText.rectTransform, new Vector2(0.50f, 0.08f), new Vector2(0.70f, 0.92f));
            _rightTeamHeader = CreateText("RightHeader", _rightTeamHeaderBlock.transform, 14, TextAnchor.MiddleRight, headerInk, antonFont);
            SetRect(_rightTeamHeader.rectTransform, new Vector2(0.70f, 0.08f), new Vector2(0.98f, 0.92f));
            _rightTeamHeader.fontStyle = FontStyle.Bold;
            _rightTeamHeader.horizontalOverflow = HorizontalWrapMode.Overflow;"""

    if "_leftCpText = CreateText" not in text:
        text = must_replace(text, old_headers, new_headers)

    old_economy = """        private void UpdateEconomyHud()
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

    new_economy = """        private void UpdateEconomyHud()
        {
            string leftId = myTeamId;
            string rightId = _opponentTeamId;
            bool leftIsT = TeamIsTerrorist(leftId, _currentRound);
            bool rightIsT = TeamIsTerrorist(rightId, _currentRound);
            Color leftInk = BattleStatPanelPresentation.PanelColor(leftIsT);
            Color rightInk = BattleStatPanelPresentation.PanelColor(rightIsT);
            BindTeamEconomyTexts(leftId, leftInk, _leftMoneyText, _leftCpText, _leftCardText);
            BindTeamEconomyTexts(rightId, rightInk, _rightMoneyText, _rightCpText, _rightCardText);
        }

        private void BindTeamEconomyTexts(
            string teamId, Color ink, Text moneyText, Text cpText, Text cardText)
        {
            if (moneyText != null)
            {
                moneyText.color = ink;
                moneyText.text = FormatEconomyMoney(teamId);
            }
            if (cpText != null)
            {
                cpText.color = ink;
                cpText.text = FormatEconomyCp(teamId);
            }
            if (cardText != null)
            {
                cardText.color = ink;
                cardText.text = FormatEconomyCardName(teamId);
            }
        }"""

    if "BindTeamEconomyTexts" not in text:
        text = must_replace(text, old_economy, new_economy)

    old_format = """        private string FormatEconomyMoneyCp(string teamId)
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

    new_format = """        private string FormatEconomyMoney(string teamId)
        {
            int money = _teamMoney.TryGetValue(teamId, out int value) ? value : EconomyRules.StartingTeamMoney;
            return "$" + money.ToString("N0");
        }

        private string FormatEconomyCp(string teamId)
        {
            int cp = _session != null ? _session.TeamById(teamId).CommandPoints : CommandPointRules.StartingPoints;
            return "CP " + cp;
        }

        private string FormatEconomyCardName(string teamId)
        {
            EquipmentLevel card = _teamCards.TryGetValue(teamId, out EquipmentLevel level) ? level : EquipmentLevel.Eco;
            return EconomyRules.CardName(card);
        }"""

    if "FormatEconomyMoneyCp" in text:
        text = must_replace(text, old_format, new_format)
    elif "FormatEconomyMoney(" not in text:
        raise RuntimeError("economy format helpers missing")

    TARGET.write_text(text, encoding="utf-8")
    verify = TARGET.read_text(encoding="utf-8")
    if "擊殺" not in verify or "跳過結算中" not in verify:
        raise RuntimeError("UTF-8 corrupted after write")
    if "_leftCpText = CreateText" not in verify or "FormatEconomyMoneyCp" in verify:
        raise RuntimeError("row order patch incomplete")
    print("patched team header row order OK")


if __name__ == "__main__":
    main()
