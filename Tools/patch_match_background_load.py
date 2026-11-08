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
                string.Equals(mapId, "inferno", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(mapId, "mirage", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(mapId, "nuke", StringComparison.OrdinalIgnoreCase))
                return "UI/Backgrounds/" + mapId + "_bg_";
            return "UI/Backgrounds/dust2_bg_";
        }"""
    new = """        private static string BackgroundResourcePrefix(string mapId)
        {
            if (string.IsNullOrEmpty(mapId) ||
                string.Equals(mapId, "dust2", StringComparison.OrdinalIgnoreCase))
                return "UI/Backgrounds/dust2_bg_";
            return "UI/Backgrounds/" + mapId + "_bg_";
        }"""
    if old not in text:
        raise SystemExit("bg prefix anchor missing")
    text = text.replace(old, new, 1)

    old2 = """        private void ApplyMatchBackground()
        {
            if (_bgImage == null)
                return;
            int idx = Mathf.Abs(seed) % 4;
            if (_matchBackgroundIndex == idx && _bgImage.sprite != null)
                return;
            _matchBackgroundIndex = idx;
            string bgPrefix = BackgroundResourcePrefix(_careerMapId);
            Sprite sprite = Resources.Load<Sprite>(bgPrefix + idx);
            _bgImage.sprite = sprite;
            _bgImage.enabled = sprite != null;
            _bgImage.color = sprite != null ? Color.white : Bg;
        }"""
    new2 = """        private void ApplyMatchBackground()
        {
            if (_bgImage == null)
                return;
            string mapId = string.IsNullOrEmpty(_careerMapId) ? "dust2" : _careerMapId;
            int idx = Mathf.Abs(seed) % 4;
            Sprite sprite = LoadMatchBackgroundSprite(mapId, idx);
            if (sprite == null && !string.Equals(mapId, "dust2", StringComparison.OrdinalIgnoreCase))
                sprite = LoadMatchBackgroundSprite("dust2", idx);
            _matchBackgroundIndex = idx;
            _bgImage.sprite = sprite;
            _bgImage.enabled = sprite != null;
            _bgImage.color = sprite != null ? Color.white : Bg;
        }

        private static Sprite LoadMatchBackgroundSprite(string mapId, int idx)
        {
            string path = BackgroundResourcePrefix(mapId) + idx;
            Sprite sprite = Resources.Load<Sprite>(path);
            if (sprite != null)
                return sprite;
            Sprite[] all = Resources.LoadAll<Sprite>(path);
            return all != null && all.Length > 0 ? all[0] : null;
        }"""
    if old2 not in text:
        raise SystemExit("ApplyMatchBackground anchor missing")
    text = text.replace(old2, new2, 1)

    TARGET.write_text(text, encoding="utf-8")
    v = TARGET.read_text(encoding="utf-8")
    assert "LoadMatchBackgroundSprite" in v and "戰術控制室" in v
    assert "ApplyControlBarLayout(simulation)" in v
    print("ok")


if __name__ == "__main__":
    main()
