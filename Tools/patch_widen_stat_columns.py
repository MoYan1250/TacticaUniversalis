# -*- coding: utf-8 -*-
"""Widen scoreboard columns so mock card layout has room."""
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
import backup_quickmatch as bak

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing:\n{old[:200]!r}")
    return text.replace(old, new, 1)


def main() -> None:
    bak.backup("before_widen_stat_columns")
    text = TARGET.read_text(encoding="utf-8")
    if "擊殺" not in text or "跳過結算中" not in text:
        raise RuntimeError("UTF-8 corrupted")

    old = """            Image leftPanel = CreateUiObject<Image>("LeftStats", canvas.transform);
            SetRect(leftPanel.rectTransform, new Vector2(0.008f, 0.112f), new Vector2(0.175f, 0.775f));
            leftPanel.color = Color.clear;
            leftPanel.raycastTarget = false;
            _leftStatsPanel = leftPanel;

            Image rightPanel = CreateUiObject<Image>("RightStats", canvas.transform);
            SetRect(rightPanel.rectTransform, new Vector2(0.825f, 0.112f), new Vector2(0.992f, 0.775f));"""

    new = """            Image leftPanel = CreateUiObject<Image>("LeftStats", canvas.transform);
            SetRect(leftPanel.rectTransform, new Vector2(0.006f, 0.112f), new Vector2(0.205f, 0.775f));
            leftPanel.color = Color.clear;
            leftPanel.raycastTarget = false;
            _leftStatsPanel = leftPanel;

            Image rightPanel = CreateUiObject<Image>("RightStats", canvas.transform);
            SetRect(rightPanel.rectTransform, new Vector2(0.795f, 0.112f), new Vector2(0.994f, 0.775f));"""

    if "0.205f, 0.775f" not in text:
        text = must_replace(text, old, new)

    TARGET.write_text(text, encoding="utf-8")
    verify = TARGET.read_text(encoding="utf-8")
    if "擊殺" not in verify:
        raise RuntimeError("UTF-8 corrupted after write")
    print("widened scoreboard columns OK")


if __name__ == "__main__":
    main()
