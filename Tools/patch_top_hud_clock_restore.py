# -*- coding: utf-8 -*-
"""Restore battle TopHud countdown under ROUND; keep meta below without covering clock."""
from pathlib import Path

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing:\n{old[:140]!r}")
    return text.replace(old, new, 1)


def main() -> None:
    text = TARGET.read_text(encoding="utf-8")
    if "擊殺廣播" not in text:
        raise RuntimeError("UTF-8 corrupted")

    # Slightly taller TopHud so ROUND + clock + meta all fit.
    text = must_replace(
        text,
        "            SetRect(topHud.rectTransform, new Vector2(0.32f, 0.900f), new Vector2(0.68f, 0.992f));",
        "            SetRect(topHud.rectTransform, new Vector2(0.32f, 0.886f), new Vector2(0.68f, 0.992f));",
    )

    # Alive strip sits just under TopHud.
    if "0.862f), new Vector2(0.60f, 0.898f)" in text:
        text = must_replace(
            text,
            "            SetRect(root.rectTransform, new Vector2(0.40f, 0.862f), new Vector2(0.60f, 0.898f));",
            "            SetRect(root.rectTransform, new Vector2(0.40f, 0.848f), new Vector2(0.60f, 0.882f));",
        )

    old_hud = """            _roundText = CreateText("Round", topHud.transform, 18, TextAnchor.MiddleCenter, Accent);
            SetRect(_roundText.rectTransform, new Vector2(0.37f, 0.58f), new Vector2(0.63f, 0.95f));
            _roundText.fontStyle = FontStyle.Bold;
            _roundText.text = "ROUND 01";

            _clockText = CreateText("Clock", topHud.transform, 16, TextAnchor.MiddleCenter, TextPrimary);
            SetRect(_clockText.rectTransform, new Vector2(0.37f, 0.36f), new Vector2(0.63f, 0.58f));
            _clockText.text = "1:55";

            _careerMatchMetaText = CreateText("CareerMeta", topHud.transform, 11, TextAnchor.MiddleCenter, TextMuted);
            SetRect(_careerMatchMetaText.rectTransform, new Vector2(0.34f, 0.06f), new Vector2(0.66f, 0.28f));
            _careerMatchMetaText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _careerMatchMetaText.gameObject.SetActive(false);"""

    new_hud = """            _roundText = CreateText("Round", topHud.transform, 17, TextAnchor.MiddleCenter, Accent);
            SetRect(_roundText.rectTransform, new Vector2(0.37f, 0.66f), new Vector2(0.63f, 0.96f));
            _roundText.fontStyle = FontStyle.Bold;
            _roundText.verticalOverflow = VerticalWrapMode.Truncate;
            _roundText.text = "ROUND 01";

            // Countdown sits directly under ROUND; keep a dedicated band so meta cannot cover it.
            _clockText = CreateText("Clock", topHud.transform, 15, TextAnchor.MiddleCenter, TextPrimary);
            SetRect(_clockText.rectTransform, new Vector2(0.37f, 0.38f), new Vector2(0.63f, 0.64f));
            _clockText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _clockText.verticalOverflow = VerticalWrapMode.Truncate;
            _clockText.raycastTarget = false;
            _clockText.text = "1:55";

            _careerMatchMetaText = CreateText("CareerMeta", topHud.transform, 10, TextAnchor.MiddleCenter, TextMuted);
            SetRect(_careerMatchMetaText.rectTransform, new Vector2(0.30f, 0.04f), new Vector2(0.70f, 0.34f));
            _careerMatchMetaText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _careerMatchMetaText.verticalOverflow = VerticalWrapMode.Truncate;
            _careerMatchMetaText.resizeTextForBestFit = true;
            _careerMatchMetaText.resizeTextMinSize = 8;
            _careerMatchMetaText.resizeTextMaxSize = 10;
            _careerMatchMetaText.raycastTarget = false;
            _careerMatchMetaText.gameObject.SetActive(false);"""

    text = must_replace(text, old_hud, new_hud)

    # Ensure clock stays above meta when refreshing official UI.
    if "_clockText.transform.SetAsLastSibling()" not in text:
        text = must_replace(
            text,
            """            if (_careerMatchMetaText != null)
            {
                _careerMatchMetaText.gameObject.SetActive(!simulation);
                if (!simulation)
                    RefreshOfficialMatchMeta();
            }

            // simulation=true → practice (wide bar); false → official (compact centered)
            ApplyControlBarLayout(simulation);""",
            """            if (_careerMatchMetaText != null)
            {
                _careerMatchMetaText.gameObject.SetActive(!simulation);
                if (!simulation)
                    RefreshOfficialMatchMeta();
            }
            if (_clockText != null)
                _clockText.transform.SetAsLastSibling();

            // simulation=true → practice (wide bar); false → official (compact centered)
            ApplyControlBarLayout(simulation);""",
        )

    if "擊殺廣播" not in text:
        raise RuntimeError("UTF-8 corrupted after patch")
    if "0.38f), new Vector2(0.63f, 0.64f)" not in text:
        raise RuntimeError("clock rect not applied")

    TARGET.write_text(text, encoding="utf-8", newline="\n")
    print("patched", TARGET)


if __name__ == "__main__":
    main()
