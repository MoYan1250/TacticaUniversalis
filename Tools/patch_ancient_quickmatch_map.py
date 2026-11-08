# -*- coding: utf-8 -*-
"""Extend QuickMatch map resources for Ancient (and generic mapId prefixes)."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def main():
    text = TARGET.read_text(encoding="utf-8")
    assert "戰術控制室" in text

    reps = [
        (
            """            string activeMapId = string.IsNullOrEmpty(_careerMapId) ? "dust2" : _careerMapId;
            string defaultZone = string.Equals(activeMapId, "train", StringComparison.OrdinalIgnoreCase)
                ? "connector"
                : "mid";""",
            """            string activeMapId = string.IsNullOrEmpty(_careerMapId) ? "dust2" : _careerMapId;
            string defaultZone = "mid";
            if (string.Equals(activeMapId, "train", StringComparison.OrdinalIgnoreCase))
                defaultZone = "connector";
            else if (string.Equals(activeMapId, "ancient", StringComparison.OrdinalIgnoreCase))
                defaultZone = "mid";""",
        ),
        (
            """            string bgPrefix = string.Equals(_careerMapId, "train", StringComparison.OrdinalIgnoreCase)
                ? "UI/Backgrounds/train_bg_"
                : "UI/Backgrounds/dust2_bg_";
            Sprite sprite = Resources.Load<Sprite>(bgPrefix + idx);""",
            """            string bgPrefix = BackgroundResourcePrefix(_careerMapId);
            Sprite sprite = Resources.Load<Sprite>(bgPrefix + idx);""",
        ),
        (
            """        private static Sprite LoadOverviewSprite(string mapId)
        {
            if (string.Equals(mapId, "train", StringComparison.OrdinalIgnoreCase))
            {
                Sprite train = Resources.Load<Sprite>("Maps/train_overview");
                if (train != null)
                    return train;
            }
            return Resources.Load<Sprite>("Maps/dust2_overview");
        }""",
            """        private static Sprite LoadOverviewSprite(string mapId)
        {
            if (!string.IsNullOrEmpty(mapId) &&
                !string.Equals(mapId, "dust2", StringComparison.OrdinalIgnoreCase))
            {
                Sprite named = Resources.Load<Sprite>("Maps/" + mapId + "_overview");
                if (named != null)
                    return named;
            }
            return Resources.Load<Sprite>("Maps/dust2_overview");
        }

        private static string BackgroundResourcePrefix(string mapId)
        {
            if (string.Equals(mapId, "train", StringComparison.OrdinalIgnoreCase))
                return "UI/Backgrounds/train_bg_";
            if (string.Equals(mapId, "ancient", StringComparison.OrdinalIgnoreCase))
                return "UI/Backgrounds/ancient_bg_";
            return "UI/Backgrounds/dust2_bg_";
        }""",
        ),
    ]

    for old, new in reps:
        if old not in text:
            raise SystemExit("anchor missing:\n" + old[:180])
        text = text.replace(old, new, 1)

    TARGET.write_text(text, encoding="utf-8")
    v = TARGET.read_text(encoding="utf-8")
    assert "ancient_bg_" in v
    assert "BackgroundResourcePrefix" in v
    assert "戰術控制室" in v
    assert "ApplyControlBarLayout(simulation)" in v
    print("ok")


if __name__ == "__main__":
    main()
