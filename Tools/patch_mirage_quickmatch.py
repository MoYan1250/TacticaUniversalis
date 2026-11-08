# -*- coding: utf-8 -*-
from pathlib import Path

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def main():
    text = TARGET.read_text(encoding="utf-8")
    assert "戰術控制室" in text

    old = """        private static string BackgroundResourcePrefix(string mapId)
        {
            if (string.Equals(mapId, "train", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(mapId, "ancient", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(mapId, "overpass", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(mapId, "vertigo", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(mapId, "inferno", StringComparison.OrdinalIgnoreCase))
                return "UI/Backgrounds/" + mapId + "_bg_";
            return "UI/Backgrounds/dust2_bg_";
        }"""
    new = """        private static string BackgroundResourcePrefix(string mapId)
        {
            if (string.Equals(mapId, "train", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(mapId, "ancient", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(mapId, "overpass", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(mapId, "vertigo", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(mapId, "inferno", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(mapId, "mirage", StringComparison.OrdinalIgnoreCase))
                return "UI/Backgrounds/" + mapId + "_bg_";
            return "UI/Backgrounds/dust2_bg_";
        }"""
    if old not in text:
        raise SystemExit("bg prefix anchor missing")
    text = text.replace(old, new, 1)

    old2 = """            else if (string.Equals(activeMapId, "inferno", StringComparison.OrdinalIgnoreCase))
                defaultZone = "mid";"""
    new2 = """            else if (string.Equals(activeMapId, "inferno", StringComparison.OrdinalIgnoreCase))
                defaultZone = "mid";
            else if (string.Equals(activeMapId, "mirage", StringComparison.OrdinalIgnoreCase))
                defaultZone = "mid";"""
    if old2 not in text:
        raise SystemExit("default zone anchor missing")
    text = text.replace(old2, new2, 1)

    TARGET.write_text(text, encoding="utf-8")
    v = TARGET.read_text(encoding="utf-8")
    assert "mirage" in v and "戰術控制室" in v
    assert "ApplyControlBarLayout(simulation)" in v
    print("ok")


if __name__ == "__main__":
    main()
