# -*- coding: utf-8 -*-
"""Fix GenYoMin2 rename + use Anton on battle scoreboard cards/headers."""
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
import backup_quickmatch as bak

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing:\n{old[:220]!r}")
    return text.replace(old, new, 1)


def main() -> None:
    bak.backup("before_anton_scoreboard_font")
    text = TARGET.read_text(encoding="utf-8")
    if "擊殺" not in text or "跳過結算中" not in text:
        raise RuntimeError("UTF-8 corrupted")

    # GenYoMin2 rename (was 700.otf)
    if 'LoadProjectFont("700.otf")' in text:
        text = must_replace(
            text,
            'LoadProjectFont("700.otf")',
            'LoadProjectFont("GenYoMin2.otf")',
        )

    # Add Anton font field + load
    if "_scoreboardFont" not in text:
        text = must_replace(
            text,
            "        private Font _uiFont;\n"
            "        private Font _uiFont700;\n"
            "        private Font _uiFontBlack;\n"
            "        private Font _playerIdFont;\n",
            "        private Font _uiFont;\n"
            "        private Font _uiFont700;\n"
            "        private Font _uiFontBlack;\n"
            "        private Font _playerIdFont;\n"
            "        private Font _scoreboardFont;\n",
        )

    old_load = """            _uiFont = LoadProjectFont("NotoSansTC-Bold.ttf") ?? ResolveOsFallbackFont();
            _uiFont700 = LoadProjectFont("GenYoMin2.otf") ?? _uiFont;
            _uiFontBlack = LoadProjectFont("NotoSansTC-Black.ttf") ?? _uiFont;
            _playerIdFont = LoadProjectFont("Anuschka-Demo.ttf") ?? _uiFont;"""
    new_load = """            _uiFont = LoadProjectFont("NotoSansTC-Bold.ttf") ?? ResolveOsFallbackFont();
            _uiFont700 = LoadProjectFont("GenYoMin2.otf") ?? _uiFont;
            _uiFontBlack = LoadProjectFont("NotoSansTC-Black.ttf") ?? _uiFont;
            _playerIdFont = LoadProjectFont("Anuschka-Demo.ttf") ?? _uiFont;
            _scoreboardFont = LoadProjectFont("Anton.ttf") ?? _uiFont;"""
    if "_scoreboardFont = LoadProjectFont" not in text:
        # tolerate still-broken 700 path if rename step already applied
        if old_load not in text:
            old_load_alt = old_load.replace("GenYoMin2.otf", "700.otf")
            if old_load_alt in text:
                text = must_replace(
                    text,
                    old_load_alt,
                    new_load,
                )
            else:
                raise RuntimeError("font load block missing")
        else:
            text = must_replace(text, old_load, new_load)

    # Team header texts use Anton
    text = text.replace(
        'CreateText("LeftHeader", _leftTeamHeaderBlock.transform, 15, TextAnchor.MiddleLeft, TextPrimary);',
        'CreateText("LeftHeader", _leftTeamHeaderBlock.transform, 15, TextAnchor.MiddleLeft, TextPrimary, _scoreboardFont);',
    )
    text = text.replace(
        'CreateText("LeftMoney", _leftTeamHeaderBlock.transform, 11, TextAnchor.MiddleRight, TextPrimary);',
        'CreateText("LeftMoney", _leftTeamHeaderBlock.transform, 11, TextAnchor.MiddleRight, TextPrimary, _scoreboardFont);',
    )
    text = text.replace(
        'CreateText("RightHeader", _rightTeamHeaderBlock.transform, 15, TextAnchor.MiddleLeft, TextPrimary);',
        'CreateText("RightHeader", _rightTeamHeaderBlock.transform, 15, TextAnchor.MiddleLeft, TextPrimary, _scoreboardFont);',
    )
    text = text.replace(
        'CreateText("RightMoney", _rightTeamHeaderBlock.transform, 11, TextAnchor.MiddleRight, TextPrimary);',
        'CreateText("RightMoney", _rightTeamHeaderBlock.transform, 11, TextAnchor.MiddleRight, TextPrimary, _scoreboardFont);',
    )

    # Player cards use Anton
    old_card_font = """                BattlePlayerCardView card = BattleStatPanelPresentation.CreateCard(
                    parent,
                    leftSide,
                    i,
                    _uiFont,"""
    new_card_font = """                BattlePlayerCardView card = BattleStatPanelPresentation.CreateCard(
                    parent,
                    leftSide,
                    i,
                    _scoreboardFont ?? _uiFont,"""
    if old_card_font in text:
        text = must_replace(text, old_card_font, new_card_font)
    elif "_scoreboardFont ?? _uiFont," not in text:
        raise RuntimeError("CreateCard font arg missing")

    TARGET.write_text(text, encoding="utf-8")
    verify = TARGET.read_text(encoding="utf-8")
    if "擊殺" not in verify or "跳過結算中" not in verify:
        raise RuntimeError("UTF-8 corrupted after write")
    if 'LoadProjectFont("700.otf")' in verify:
        raise RuntimeError("still loading 700.otf")
    if "_scoreboardFont = LoadProjectFont(\"Anton.ttf\")" not in verify:
        raise RuntimeError("Anton not wired")
    print("patched GenYoMin2 + Anton scoreboard OK")


if __name__ == "__main__":
    main()
