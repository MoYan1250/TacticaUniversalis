# -*- coding: utf-8 -*-
"""Wire QuickMatch to active map (dust2 / train): overview, zones, backgrounds, planner."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def main():
    text = TARGET.read_text(encoding="utf-8")
    assert "戰術控制室" in text, "UTF-8 Chinese anchor missing"

    reps = [
        (
            """            _content = RuntimeContentLoader.Load();
            DemoZh.ConfigurePlayerNames(_content.Players.Values);
            _pathfinder = new MapPathfinder(_content.Dust2);
            foreach (MapZoneDefinition zone in _content.Dust2.zones)
                _zones[zone.id] = zone;
            _circleSprite = CreateCircleSprite(true);
            _ringSprite = CreateCircleSprite(false);""",
            """            _content = RuntimeContentLoader.Load();
            DemoZh.ConfigurePlayerNames(_content.Players.Values);
            _circleSprite = CreateCircleSprite(true);
            _ringSprite = CreateCircleSprite(false);""",
        ),
        (
            """                _seriesScoreOpponent = Mathf.Max(0, launch.seriesScoreOpponent);
            }

            BuildPlayerNumbers();""",
            """                _seriesScoreOpponent = Mathf.Max(0, launch.seriesScoreOpponent);
            }
            else
            {
                _careerMapId = "dust2";
                _careerMapDisplayName = "Dust II";
            }

            BindActiveMap();
            BuildPlayerNumbers();""",
        ),
        (
            """            _session = new InteractiveQuickMatchSession(
                _content, seed, new RuleBasedTacticalPlanner(), new FastRoundSimulator(),
                myTeamId, _opponentTeamId);""",
            """            string activeMapId = string.IsNullOrEmpty(_careerMapId) ? "dust2" : _careerMapId;
            string defaultZone = string.Equals(activeMapId, "train", StringComparison.OrdinalIgnoreCase)
                ? "connector"
                : "mid";
            _session = new InteractiveQuickMatchSession(
                _content, seed, new RuleBasedTacticalPlanner(defaultZone), new FastRoundSimulator(),
                myTeamId, _opponentTeamId, activeMapId);""",
        ),
        (
            """            Sprite sprite = Resources.Load<Sprite>("UI/Backgrounds/dust2_bg_" + idx);""",
            """            string bgPrefix = string.Equals(_careerMapId, "train", StringComparison.OrdinalIgnoreCase)
                ? "UI/Backgrounds/train_bg_"
                : "UI/Backgrounds/dust2_bg_";
            Sprite sprite = Resources.Load<Sprite>(bgPrefix + idx);""",
        ),
        (
            """            Image map = CreateUiObject<Image>("Dust2Map", _mapRotator);
            _mapRect = map.rectTransform;
            Stretch(_mapRect);
            _mapRect.pivot = new Vector2(0.5f, 0.5f);
            _mapSprite = Resources.Load<Sprite>("Maps/dust2_overview");
            map.sprite = _mapSprite;""",
            """            Image map = CreateUiObject<Image>("ActiveMap", _mapRotator);
            _mapRect = map.rectTransform;
            Stretch(_mapRect);
            _mapRect.pivot = new Vector2(0.5f, 0.5f);
            _mapSprite = LoadOverviewSprite(_careerMapId);
            map.sprite = _mapSprite;""",
        ),
        (
            """            title.text = "戰術控制室 · Dust II";
            _controlBarTitle = title;""",
            """            title.text = "戰術控制室 · " + (string.IsNullOrEmpty(_careerMapDisplayName) ? "Dust II" : _careerMapDisplayName);
            _controlBarTitle = title;""",
        ),
        (
            """            foreach (MapZoneDefinition zone in _content.Dust2.zones)
            {
                Image hit = CreateUiObject<Image>("ZoneHit_" + zone.id, _mapRect);""",
            """            foreach (MapZoneDefinition zone in ActiveMapZones())
            {
                Image hit = CreateUiObject<Image>("ZoneHit_" + zone.id, _mapRect);""",
        ),
        (
            """            if (_content?.Dust2?.zones == null)
                return;
            for (int i = 0; i < _content.Dust2.zones.Length; i++)
            {
                MapZoneDefinition zone = _content.Dust2.zones[i];""",
            """            MapZoneDefinition[] zones = ActiveMapZones();
            if (zones == null)
                return;
            for (int i = 0; i < zones.Length; i++)
            {
                MapZoneDefinition zone = zones[i];""",
        ),
        (
            """            if (!_zones.TryGetValue(zoneId, out MapZoneDefinition zone))
                zone = _zones["mid"];""",
            """            if (!_zones.TryGetValue(zoneId, out MapZoneDefinition zone))
            {
                if (!_zones.TryGetValue("connector", out zone) &&
                    !_zones.TryGetValue("mid", out zone) &&
                    !_zones.TryGetValue("a_site", out zone))
                    zone = _zones.Values.First();
            }""",
        ),
    ]

    for old, new in reps:
        if old not in text:
            raise SystemExit("anchor missing:\n" + old[:160])
        text = text.replace(old, new, 1)

    helper = '''
        private MapDefinition ActiveMapDefinition()
        {
            string id = string.IsNullOrEmpty(_careerMapId) ? "dust2" : _careerMapId;
            return _content != null ? _content.GetMap(id) : null;
        }

        private MapZoneDefinition[] ActiveMapZones() =>
            ActiveMapDefinition()?.zones ?? Array.Empty<MapZoneDefinition>();

        private void BindActiveMap()
        {
            _zones.Clear();
            MapDefinition map = ActiveMapDefinition();
            if (map == null)
                return;
            _pathfinder = new MapPathfinder(map);
            foreach (MapZoneDefinition zone in map.zones ?? Array.Empty<MapZoneDefinition>())
                _zones[zone.id] = zone;
        }

        private static Sprite LoadOverviewSprite(string mapId)
        {
            if (string.Equals(mapId, "train", StringComparison.OrdinalIgnoreCase))
            {
                Sprite train = Resources.Load<Sprite>("Maps/train_overview");
                if (train != null)
                    return train;
            }
            return Resources.Load<Sprite>("Maps/dust2_overview");
        }
'''

    if "BindActiveMap()" not in text or "private void BindActiveMap()" not in text:
        anchor = "        private void GetMapContentSize(out float width, out float height)"
        if anchor not in text:
            raise SystemExit("GetMapContentSize anchor missing")
        # Avoid double-insert if call site already present from partial run
        if "private void BindActiveMap()" not in text:
            text = text.replace(anchor, helper + "\n" + anchor, 1)

    if "using System.Linq;" not in text:
        text = text.replace(
            "using System.Collections.Generic;",
            "using System.Collections.Generic;\nusing System.Linq;",
            1,
        )

    TARGET.write_text(text, encoding="utf-8")
    verify = TARGET.read_text(encoding="utf-8")
    assert "戰術控制室" in verify
    assert "private void BindActiveMap()" in verify
    assert "train_bg_" in verify
    assert "LoadOverviewSprite" in verify
    assert "ApplyControlBarLayout(simulation)" in verify
    assert "_content.Dust2" not in verify or verify.count("_content.Dust2") == 0
    print("patched", TARGET)
    print("remaining Dust2 refs", verify.count("_content.Dust2"))


if __name__ == "__main__":
    main()
