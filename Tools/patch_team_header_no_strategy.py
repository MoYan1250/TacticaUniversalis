# -*- coding: utf-8 -*-
"""Remove strategy card from team header; keep club / money / CP; 90% text height."""
from pathlib import Path
import backup_quickmatch as bak

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing:\n{old[:240]!r}")
    return text.replace(old, new, 1)


def main() -> None:
    bak.backup("before_team_header_no_strategy")
    text = TARGET.read_text(encoding="utf-8")
    if "跳過結算中" not in text:
        raise RuntimeError("UTF-8 anchor missing — abort")

    text = must_replace(
        text,
        """            if (_leftCardText != null)
                _leftCardText.gameObject.SetActive(general);
            if (_rightCardText != null)
                _rightCardText.gameObject.SetActive(general);

            if (general)
            {
                BattleStatPanelPresentation.LayoutTeamHeaderFields(
                    _leftTeamHeader, _leftMoneyText, _leftCpText, _leftCardText, leftSide: true);
                BattleStatPanelPresentation.LayoutTeamHeaderFields(
                    _rightTeamHeader, _rightMoneyText, _rightCpText, _rightCardText, leftSide: false);""",
        """            if (_leftCardText != null)
                _leftCardText.gameObject.SetActive(false);
            if (_rightCardText != null)
                _rightCardText.gameObject.SetActive(false);

            if (general)
            {
                BattleStatPanelPresentation.LayoutTeamHeaderFields(
                    _leftTeamHeader, _leftMoneyText, _leftCpText, leftSide: true);
                BattleStatPanelPresentation.LayoutTeamHeaderFields(
                    _rightTeamHeader, _rightMoneyText, _rightCpText, leftSide: false);""",
    )

    text = must_replace(
        text,
        """            _leftTeamHeader = CreateText("LeftHeader", _leftTeamHeaderBlock.transform, 14, TextAnchor.MiddleLeft, headerInk, antonFont);
            _leftMoneyText = CreateText("LeftMoney", _leftTeamHeaderBlock.transform, 13, TextAnchor.MiddleCenter, headerInk, antonFont);
            _leftCpText = CreateText("LeftCp", _leftTeamHeaderBlock.transform, 13, TextAnchor.MiddleCenter, headerInk, antonFont);
            _leftCardText = CreateText("LeftCard", _leftTeamHeaderBlock.transform, 12, TextAnchor.MiddleRight, headerInk, tacticsFont);

            _rightTeamHeaderBlock = CreateUiObject<Image>("RightTeamHeader", canvas.transform);
            SetRect(_rightTeamHeaderBlock.rectTransform, createRightHeaderMin, createRightHeaderMax);
            _rightTeamHeaderBlock.color = BattleStatPanelPresentation.TLight;
            _rightTeamHeaderBlock.raycastTarget = false;
            _rightCardText = CreateText("RightCard", _rightTeamHeaderBlock.transform, 12, TextAnchor.MiddleLeft, headerInk, tacticsFont);
            _rightCpText = CreateText("RightCp", _rightTeamHeaderBlock.transform, 13, TextAnchor.MiddleCenter, headerInk, antonFont);
            _rightMoneyText = CreateText("RightMoney", _rightTeamHeaderBlock.transform, 13, TextAnchor.MiddleCenter, headerInk, antonFont);
            _rightTeamHeader = CreateText("RightHeader", _rightTeamHeaderBlock.transform, 14, TextAnchor.MiddleRight, headerInk, antonFont);

            BattleStatPanelPresentation.LayoutTeamHeaderFields(
                _leftTeamHeader, _leftMoneyText, _leftCpText, _leftCardText, leftSide: true);
            BattleStatPanelPresentation.LayoutTeamHeaderFields(
                _rightTeamHeader, _rightMoneyText, _rightCpText, _rightCardText, leftSide: false);""",
        """            _leftTeamHeader = CreateText("LeftHeader", _leftTeamHeaderBlock.transform, 14, TextAnchor.MiddleLeft, headerInk, antonFont);
            _leftMoneyText = CreateText("LeftMoney", _leftTeamHeaderBlock.transform, 13, TextAnchor.MiddleCenter, headerInk, antonFont);
            _leftCpText = CreateText("LeftCp", _leftTeamHeaderBlock.transform, 13, TextAnchor.MiddleCenter, headerInk, antonFont);
            _leftCardText = CreateText("LeftCard", _leftTeamHeaderBlock.transform, 12, TextAnchor.MiddleRight, headerInk, tacticsFont);
            _leftCardText.gameObject.SetActive(false);

            _rightTeamHeaderBlock = CreateUiObject<Image>("RightTeamHeader", canvas.transform);
            SetRect(_rightTeamHeaderBlock.rectTransform, createRightHeaderMin, createRightHeaderMax);
            _rightTeamHeaderBlock.color = BattleStatPanelPresentation.TLight;
            _rightTeamHeaderBlock.raycastTarget = false;
            _rightCardText = CreateText("RightCard", _rightTeamHeaderBlock.transform, 12, TextAnchor.MiddleLeft, headerInk, tacticsFont);
            _rightCardText.gameObject.SetActive(false);
            _rightCpText = CreateText("RightCp", _rightTeamHeaderBlock.transform, 13, TextAnchor.MiddleCenter, headerInk, antonFont);
            _rightMoneyText = CreateText("RightMoney", _rightTeamHeaderBlock.transform, 13, TextAnchor.MiddleCenter, headerInk, antonFont);
            _rightTeamHeader = CreateText("RightHeader", _rightTeamHeaderBlock.transform, 14, TextAnchor.MiddleRight, headerInk, antonFont);

            BattleStatPanelPresentation.LayoutTeamHeaderFields(
                _leftTeamHeader, _leftMoneyText, _leftCpText, leftSide: true);
            BattleStatPanelPresentation.LayoutTeamHeaderFields(
                _rightTeamHeader, _rightMoneyText, _rightCpText, leftSide: false);""",
    )

    text = must_replace(
        text,
        """                BindTeamEconomyTexts(leftId, leftInk, _leftMoneyText, _leftCpText, _leftCardText);
                BindTeamEconomyTexts(rightId, rightInk, _rightMoneyText, _rightCpText, _rightCardText);
            }
            else
            {
                if (_leftMoneyText != null)
                {
                    _leftMoneyText.color = leftInk;
                    _leftMoneyText.text = FormatEconomyMoney(leftId) + " · " + FormatEconomyCp(leftId) + " · " + FormatEconomyCardName(leftId);
                }
                if (_rightMoneyText != null)
                {
                    _rightMoneyText.color = rightInk;
                    _rightMoneyText.text = FormatEconomyMoney(rightId) + " · " + FormatEconomyCp(rightId) + " · " + FormatEconomyCardName(rightId);
                }
            }""",
        """                BindTeamEconomyTexts(leftId, leftInk, _leftMoneyText, _leftCpText);
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
            }""",
    )

    text = must_replace(
        text,
        """        private void BindTeamEconomyTexts(
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
        }""",
        """        private void BindTeamEconomyTexts(
            string teamId, Color ink, Text moneyText, Text cpText)
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
        }""",
    )

    TARGET.write_text(text, encoding="utf-8")
    print("patched team header no strategy OK")


if __name__ == "__main__":
    main()
