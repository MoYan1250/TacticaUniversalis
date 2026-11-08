# -*- coding: utf-8 -*-
"""Team header fields: equal slot widths/gaps, Best Fit at 90% bar height."""
from pathlib import Path
import backup_quickmatch as bak

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing:\n{old[:240]!r}")
    return text.replace(old, new, 1)


def main() -> None:
    bak.backup("before_team_header_fit")
    text = TARGET.read_text(encoding="utf-8")
    if "跳過結算中" not in text:
        raise RuntimeError("UTF-8 anchor missing — abort")

    old_create = """            _leftTeamHeader = CreateText("LeftHeader", _leftTeamHeaderBlock.transform, 14, TextAnchor.MiddleLeft, headerInk, antonFont);
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
            SetRect(_rightTeamHeaderBlock.rectTransform, createRightHeaderMin, createRightHeaderMax);
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

    new_create = """            _leftTeamHeader = CreateText("LeftHeader", _leftTeamHeaderBlock.transform, 14, TextAnchor.MiddleLeft, headerInk, antonFont);
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
                _rightTeamHeader, _rightMoneyText, _rightCpText, _rightCardText, leftSide: false);"""

    text = must_replace(text, old_create, new_create)

    old_chrome = """            if (general)
            {
                if (_leftTeamHeader != null)
                    SetRect(_leftTeamHeader.rectTransform, new Vector2(0.02f, 0.08f), new Vector2(0.30f, 0.92f));
                if (_leftMoneyText != null)
                    SetRect(_leftMoneyText.rectTransform, new Vector2(0.30f, 0.08f), new Vector2(0.50f, 0.92f));
                if (_rightTeamHeader != null)
                    SetRect(_rightTeamHeader.rectTransform, new Vector2(0.70f, 0.08f), new Vector2(0.98f, 0.92f));
                if (_rightMoneyText != null)
                    SetRect(_rightMoneyText.rectTransform, new Vector2(0.50f, 0.08f), new Vector2(0.70f, 0.92f));

                BattleStatPanelPresentation.GetGeneralColumnScreenRects("""

    new_chrome = """            if (general)
            {
                BattleStatPanelPresentation.LayoutTeamHeaderFields(
                    _leftTeamHeader, _leftMoneyText, _leftCpText, _leftCardText, leftSide: true);
                BattleStatPanelPresentation.LayoutTeamHeaderFields(
                    _rightTeamHeader, _rightMoneyText, _rightCpText, _rightCardText, leftSide: false);

                BattleStatPanelPresentation.GetGeneralColumnScreenRects("""

    text = must_replace(text, old_chrome, new_chrome)

    TARGET.write_text(text, encoding="utf-8")
    print("patched team header fit OK")


if __name__ == "__main__":
    main()
