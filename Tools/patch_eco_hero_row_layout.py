# -*- coding: utf-8 -*-
"""Economy card / hero-pick row: lift cards, align confirm with player buttons."""
from pathlib import Path

TARGET = Path(__file__).resolve().parents[1] / (
    "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"
)


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError("missing:\n" + repr(old[:200]))
    return text.replace(old, new, 1)


def main() -> None:
    text = TARGET.read_text(encoding="utf-8")

    text = must_replace(
        text,
        "SetRect(edge.rectTransform, new Vector2(x0, 0.08f), new Vector2(x1, 0.74f));",
        "SetRect(edge.rectTransform, new Vector2(x0, 0.22f), new Vector2(x1, 0.74f));",
    )

    text = must_replace(
        text,
        "SetRect(rowRt, new Vector2(0.06f, 0.02f), new Vector2(0.94f, 0.16f));",
        "SetRect(rowRt, new Vector2(0.06f, 0.02f), new Vector2(0.94f, 0.20f));",
    )

    text = must_replace(
        text,
        "SetRect(_heroPickHint.rectTransform, new Vector2(0f, 0.55f), new Vector2(0.70f, 1f));",
        "SetRect(_heroPickHint.rectTransform, new Vector2(0f, 0.62f), new Vector2(0.70f, 1f));",
    )

    text = must_replace(
        text,
        "SetRect(edge.rectTransform, new Vector2(x0, 0.05f), new Vector2(x0 + 0.13f, 0.50f));",
        "SetRect(edge.rectTransform, new Vector2(x0, 0.06f), new Vector2(x0 + 0.13f, 0.56f));",
    )

    text = must_replace(
        text,
        "SetRect(confEdge.rectTransform, new Vector2(0.74f, 0.55f), new Vector2(0.98f, 0.95f));",
        "SetRect(confEdge.rectTransform, new Vector2(0.74f, 0.06f), new Vector2(0.88f, 0.56f));",
    )

    for anchor in ("選擇經濟卡", "英雄槍"):
        if anchor not in text:
            raise RuntimeError(f"Chinese anchor lost: {anchor}")

    TARGET.write_text(text, encoding="utf-8")
    print("patched", TARGET)


if __name__ == "__main__":
    main()
