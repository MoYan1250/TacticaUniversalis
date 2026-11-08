# -*- coding: utf-8 -*-
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def replace_once(text: str, old: str, new: str, label: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing anchor: {label}")
    return text.replace(old, new, 1)


def main() -> None:
    text = TARGET.read_text(encoding="utf-8")
    if "FloorToInt(seconds)" in text and "0.08f * Mathf.Max(1, path.Count - 1)" in text:
        print("already patched")
        return
    if ("特性觸發" not in text and "叫暫停" not in text) or "ApplyControlBarLayout(simulation)" not in text:
        raise RuntimeError("QuickMatch UTF-8 or layout invariant failed")

    text = replace_once(
        text,
        """        private static string FormatClock(float seconds)
        {
            int remaining = Mathf.Max(0, 115 - Mathf.RoundToInt(seconds));
            return $"{remaining / 60}:{remaining % 60:00}";
        }""",
        """        private static string FormatClock(float seconds)
        {
            // Whole seconds only; general-mode sim may advance at 0.01s.
            int remaining = Mathf.Max(0, 115 - Mathf.FloorToInt(seconds));
            return $"{remaining / 60}:{remaining % 60:00}";
        }""",
        "FormatClock",
    )
    text = replace_once(
        text,
        """            float cap = roundTimeGap >= 0.9f ? 0.1f : 0.35f;
            return Mathf.Min(wait, cap);""",
        """            // Compress long idle gaps; keep short nav steps snappy under denser grids.
            float cap = roundTimeGap >= 0.9f ? 0.1f : roundTimeGap <= 0.12f ? 0.12f : 0.35f;
            return Mathf.Min(wait, cap);""",
        "playback wait",
    )
    text = replace_once(
        text,
        """            float duration = Mathf.Clamp(0.28f * Mathf.Max(1, path.Count - 1), 0.35f, 1.6f);""",
        """            float duration = IsGeneralBattleMode()
                ? Mathf.Clamp(0.08f * Mathf.Max(1, path.Count - 1), 0.05f, 0.40f)
                : Mathf.Clamp(0.28f * Mathf.Max(1, path.Count - 1), 0.35f, 1.6f);""",
        "move duration",
    )
    for needle in (
        "FloorToInt(seconds)",
        "0.08f * Mathf.Max(1, path.Count - 1)",
        "特性觸發",
        "ApplyControlBarLayout(simulation)",
    ):
        if needle not in text:
            raise RuntimeError(f"missing verification needle: {needle}")
    TARGET.write_text(text, encoding="utf-8", newline="\n")
    print("patched", TARGET)


if __name__ == "__main__":
    main()
