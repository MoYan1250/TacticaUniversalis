# -*- coding: utf-8 -*-
"""Align official result logos with team names; enlarge name text."""
from pathlib import Path
import backup_quickmatch as bak

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing:\n{old[:200]!r}")
    return text.replace(old, new, 1)


def main() -> None:
    bak.backup("before_result_name_align")
    text = TARGET.read_text(encoding="utf-8")
    if "擊殺廣播" not in text:
        raise RuntimeError("UTF-8 corrupted")

    # Force rebuild of result overlay if an old instance was cached in fields during edit;
    # creation path already recreates when left logo missing — keep layout anchors consistent.

    old_block = """            _officialResultLeftLogo = CreateResultTeamLogo("LeftLogo", panel.transform, 0.13f);
            _officialResultRightLogo = CreateResultTeamLogo("RightLogo", panel.transform, 0.87f);

            _officialResultLeftName = CreateText("LeftName", panel.transform, 16, TextAnchor.MiddleCenter, TextMuted);
            SetRect(_officialResultLeftName.rectTransform, new Vector2(0.04f, 0.34f), new Vector2(0.28f, 0.46f));

            _officialResultRightName = CreateText("RightName", panel.transform, 16, TextAnchor.MiddleCenter, TextMuted);
            SetRect(_officialResultRightName.rectTransform, new Vector2(0.72f, 0.34f), new Vector2(0.96f, 0.46f));

            _officialResultLeftScore = CreateText("LeftScore", panel.transform, 48, TextAnchor.MiddleRight, TextPrimary, _uiFont700);
            SetRect(_officialResultLeftScore.rectTransform, new Vector2(0.28f, 0.48f), new Vector2(0.44f, 0.78f));
            _officialResultLeftScore.fontStyle = FontStyle.Bold;

            Text colon = CreateText("Colon", panel.transform, 36, TextAnchor.MiddleCenter, TextMuted);
            SetRect(colon.rectTransform, new Vector2(0.44f, 0.48f), new Vector2(0.56f, 0.78f));
            colon.text = ":";
            colon.fontStyle = FontStyle.Bold;

            _officialResultRightScore = CreateText("RightScore", panel.transform, 48, TextAnchor.MiddleLeft, TextPrimary, _uiFont700);
            SetRect(_officialResultRightScore.rectTransform, new Vector2(0.56f, 0.48f), new Vector2(0.72f, 0.78f));
            _officialResultRightScore.fontStyle = FontStyle.Bold;

            _officialResultOutcome = CreateText("Outcome", panel.transform, 26, TextAnchor.MiddleCenter, OfficialWinUi, _uiFont700);
            SetRect(_officialResultOutcome.rectTransform, new Vector2(0.28f, 0.24f), new Vector2(0.72f, 0.40f));
            _officialResultOutcome.fontStyle = FontStyle.Bold;"""

    new_block = """            // Logo X centers: 0.18 / 0.82 — names use the same column centers underneath.
            _officialResultLeftLogo = CreateResultTeamLogo("LeftLogo", panel.transform, 0.18f);
            _officialResultRightLogo = CreateResultTeamLogo("RightLogo", panel.transform, 0.82f);

            _officialResultLeftName = CreateText("LeftName", panel.transform, 22, TextAnchor.MiddleCenter, TextPrimary);
            SetRect(_officialResultLeftName.rectTransform, new Vector2(0.06f, 0.36f), new Vector2(0.30f, 0.50f));
            _officialResultLeftName.fontStyle = FontStyle.Bold;
            _officialResultLeftName.resizeTextForBestFit = true;
            _officialResultLeftName.resizeTextMinSize = 16;
            _officialResultLeftName.resizeTextMaxSize = 24;
            _officialResultLeftName.horizontalOverflow = HorizontalWrapMode.Overflow;

            _officialResultRightName = CreateText("RightName", panel.transform, 22, TextAnchor.MiddleCenter, TextPrimary);
            SetRect(_officialResultRightName.rectTransform, new Vector2(0.70f, 0.36f), new Vector2(0.94f, 0.50f));
            _officialResultRightName.fontStyle = FontStyle.Bold;
            _officialResultRightName.resizeTextForBestFit = true;
            _officialResultRightName.resizeTextMinSize = 16;
            _officialResultRightName.resizeTextMaxSize = 24;
            _officialResultRightName.horizontalOverflow = HorizontalWrapMode.Overflow;

            _officialResultLeftScore = CreateText("LeftScore", panel.transform, 48, TextAnchor.MiddleRight, TextPrimary, _uiFont700);
            SetRect(_officialResultLeftScore.rectTransform, new Vector2(0.30f, 0.52f), new Vector2(0.44f, 0.82f));
            _officialResultLeftScore.fontStyle = FontStyle.Bold;

            Text colon = CreateText("Colon", panel.transform, 36, TextAnchor.MiddleCenter, TextMuted);
            SetRect(colon.rectTransform, new Vector2(0.44f, 0.52f), new Vector2(0.56f, 0.82f));
            colon.text = ":";
            colon.fontStyle = FontStyle.Bold;

            _officialResultRightScore = CreateText("RightScore", panel.transform, 48, TextAnchor.MiddleLeft, TextPrimary, _uiFont700);
            SetRect(_officialResultRightScore.rectTransform, new Vector2(0.56f, 0.52f), new Vector2(0.70f, 0.82f));
            _officialResultRightScore.fontStyle = FontStyle.Bold;

            _officialResultOutcome = CreateText("Outcome", panel.transform, 26, TextAnchor.MiddleCenter, OfficialWinUi, _uiFont700);
            SetRect(_officialResultOutcome.rectTransform, new Vector2(0.28f, 0.22f), new Vector2(0.72f, 0.34f));
            _officialResultOutcome.fontStyle = FontStyle.Bold;"""

    text = must_replace(text, old_block, new_block)

    # Logos share a vertical band with scores; names sit in a dedicated band under that row.
    text = must_replace(
        text,
        "            // Same pattern as TopHud logos: fixed X, height-driven square beside the score.\n"
        "            rt.anchorMin = new Vector2(anchorX, 0.44f);\n"
        "            rt.anchorMax = new Vector2(anchorX, 0.82f);\n",
        "            // Column-centered with team name (same anchorX). Height matches score row.\n"
        "            rt.anchorMin = new Vector2(anchorX, 0.52f);\n"
        "            rt.anchorMax = new Vector2(anchorX, 0.82f);\n",
    )

    if "CreateResultTeamLogo(\"LeftLogo\", panel.transform, 0.18f)" not in text:
        raise RuntimeError("left logo center not applied")
    if "CreateText(\"LeftName\", panel.transform, 22," not in text:
        raise RuntimeError("left name size not applied")
    if "擊殺廣播" not in text:
        raise RuntimeError("UTF-8 corrupted after patch")

    TARGET.write_text(text, encoding="utf-8", newline="\n")
    print("patched", TARGET)


if __name__ == "__main__":
    main()
