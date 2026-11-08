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
    if "CycleMapFloor" in text:
        print("floor navigation already patched")
        return
    if ("擊殺廣播" not in text and "特性觸發" not in text and "叫暫停" not in text) or \
            "ApplyControlBarLayout(simulation)" not in text:
        raise RuntimeError("QuickMatch UTF-8 or official layout invariant failed")

    text = replace_once(
        text,
        """        private readonly Dictionary<string, string> _playerZones = new Dictionary<string, string>();
        private readonly Dictionary<string, Coroutine> _moveRoutines = new Dictionary<string, Coroutine>();""",
        """        private readonly Dictionary<string, string> _playerZones = new Dictionary<string, string>();
        private readonly HashSet<string> _deadMarkers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Coroutine> _moveRoutines = new Dictionary<string, Coroutine>();""",
        "dead marker field",
    )
    text = replace_once(
        text,
        """        private RectTransform _mapRect;
        private Image _bgImage;
        private int _matchBackgroundIndex = -1;
        private Sprite _mapSprite;
        private RectTransform _bombMarker;""",
        """        private RectTransform _mapRect;
        private Image _mapImage;
        private Image _bgImage;
        private int _matchBackgroundIndex = -1;
        private Sprite _mapSprite;
        private string _currentMapFloor;
        private string _bombZoneId;
        private RectTransform _bombMarker;""",
        "map floor fields",
    )
    text = replace_once(
        text,
        """        private RectTransform _skipButtonRect;
        private Image _skipButtonEdge;
        private Image _skipButtonFace;
        private GameObject _officialResultRoot;""",
        """        private RectTransform _skipButtonRect;
        private Image _skipButtonEdge;
        private Image _skipButtonFace;
        private RectTransform _floorButtonRect;
        private Text _floorButtonText;
        private GameObject _officialResultRoot;""",
        "floor button fields",
    )
    text = replace_once(
        text,
        """            if (_skipButtonRect != null)
                _skipButtonRect.gameObject.SetActive(!IsGeneralBattleMode());
            RefreshMarkerHpBarsVisible();""",
        """            bool floorSwitch = MapSupportsFloors();
            if (_skipButtonRect != null)
                _skipButtonRect.gameObject.SetActive(!floorSwitch && !IsGeneralBattleMode());
            if (_floorButtonRect != null)
                _floorButtonRect.gameObject.SetActive(floorSwitch);
            RefreshFloorButton();
            RefreshFloorVisibility();
            RefreshMarkerHpBarsVisible();""",
        "mode floor visibility",
    )
    text = replace_once(
        text,
        """                if (_skipButtonRect != null)
                    SetRect(_skipButtonRect, new Vector2(0.79f, 0.15f), new Vector2(0.855f, 0.85f));
                return;""",
        """                if (_skipButtonRect != null)
                    SetRect(_skipButtonRect, new Vector2(0.79f, 0.15f), new Vector2(0.855f, 0.85f));
                if (_floorButtonRect != null)
                    SetRect(_floorButtonRect, new Vector2(0.79f, 0.15f), new Vector2(0.855f, 0.85f));
                return;""",
        "practice floor layout",
    )
    text = replace_once(
        text,
        """            if (_skipButtonRect != null)
                SetRect(_skipButtonRect, new Vector2(0.77f, 0.14f), new Vector2(0.97f, 0.86f));
        }""",
        """            if (_skipButtonRect != null)
                SetRect(_skipButtonRect, new Vector2(0.77f, 0.14f), new Vector2(0.97f, 0.86f));
            if (_floorButtonRect != null)
                SetRect(_floorButtonRect, new Vector2(0.77f, 0.14f), new Vector2(0.97f, 0.86f));
        }""",
        "official floor layout",
    )
    text = replace_once(
        text,
        """                case SimulationEventType.Kill:
                    if (!string.IsNullOrEmpty(evt.targetId))
                        SetMarkerVitality(evt.targetId, 0, 0);""",
        """                case SimulationEventType.Kill:
                    if (!string.IsNullOrEmpty(evt.targetId))
                    {
                        _deadMarkers.Add(evt.targetId);
                        SetMarkerVitality(evt.targetId, 0, 0);
                    }""",
        "dead marker tracking",
    )
    text = replace_once(
        text,
        """        private void ResetMarkers(int roundNumber, bool animate)
        {
            StopAllMovement();
            if (_session == null) return;""",
        """        private void ResetMarkers(int roundNumber, bool animate)
        {
            StopAllMovement();
            _deadMarkers.Clear();
            if (_session == null) return;""",
        "reset dead markers",
    )
    text = replace_once(
        text,
        """            // Pack all teammates visually around the shared spawn zone.
            RelayoutZone(spawn, animate, 0.25f);""",
        """            // Pack all teammates visually around the shared spawn zone.
            RelayoutZone(spawn, animate, 0.25f);
            RefreshFloorVisibility();""",
        "spawn floor visibility",
    )
    text = replace_once(
        text,
        """            if (!_markers.TryGetValue(playerId, out RectTransform marker) || !marker.gameObject.activeSelf)
                return;

            string from = _playerZones.TryGetValue(playerId, out string current) ? current : zoneId;""",
        """            if (!_markers.TryGetValue(playerId, out RectTransform marker))
                return;

            string from = _playerZones.TryGetValue(playerId, out string current) ? current : zoneId;""",
        "hidden floor movement",
    )
    text = replace_once(
        text,
        """                    if (walked + seg >= distance || i == segmentLengths.Length - 1)
                    {
                        float local = Mathf.Clamp01((distance - walked) / seg);
                        marker.anchoredPosition = Vector2.Lerp(points[i], points[i + 1], local);
                        break;
                    }""",
        """                    if (walked + seg >= distance || i == segmentLengths.Length - 1)
                    {
                        float local = Mathf.Clamp01((distance - walked) / seg);
                        string segmentFloor = ZoneFloor(path[Mathf.Min(i, path.Count - 1)]);
                        marker.gameObject.SetActive(
                            !_deadMarkers.Contains(playerId) &&
                            string.Equals(segmentFloor, _currentMapFloor, StringComparison.OrdinalIgnoreCase));
                        marker.anchoredPosition = Vector2.Lerp(points[i], points[i + 1], local);
                        break;
                    }""",
        "floor aware movement",
    )
    text = replace_once(
        text,
        """            if (string.IsNullOrEmpty(zoneId) || !_markers.TryGetValue(playerId, out RectTransform marker))
                return;
            if (!marker.gameObject.activeSelf)
                return;
            marker.anchoredPosition = ClampToMap(LocalZonePosition(zoneId) + ZoneOccupantOffset(playerId, zoneId));
            RelayoutZone(zoneId, false, 0f);""",
        """            if (string.IsNullOrEmpty(zoneId) || !_markers.TryGetValue(playerId, out RectTransform marker))
                return;
            marker.anchoredPosition = ClampToMap(LocalZonePosition(zoneId) + ZoneOccupantOffset(playerId, zoneId));
            RelayoutZone(zoneId, false, 0f);
            RefreshFloorVisibility();""",
        "hidden settle",
    )
    text = replace_once(
        text,
        """        private MapZoneDefinition[] ActiveMapZones() =>
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

        private static Sprite LoadOverviewSprite(string mapId)""",
        """        private MapZoneDefinition[] ActiveMapZones() =>
            ActiveMapDefinition()?.zones ?? Array.Empty<MapZoneDefinition>();

        private bool MapSupportsFloors()
        {
            MapDefinition map = ActiveMapDefinition();
            return map?.floors != null && map.floors.Length > 1;
        }

        private string ZoneFloor(string zoneId)
        {
            if (!string.IsNullOrEmpty(zoneId) && _zones.TryGetValue(zoneId, out MapZoneDefinition zone) &&
                !string.IsNullOrEmpty(zone.floor))
                return zone.floor;
            return string.IsNullOrEmpty(_currentMapFloor) ? "main" : _currentMapFloor;
        }

        private void BindActiveMap()
        {
            _zones.Clear();
            MapDefinition map = ActiveMapDefinition();
            if (map == null)
                return;
            _pathfinder = new MapPathfinder(map);
            foreach (MapZoneDefinition zone in map.zones ?? Array.Empty<MapZoneDefinition>())
                _zones[zone.id] = zone;
            _currentMapFloor = !string.IsNullOrEmpty(map.defaultFloor)
                ? map.defaultFloor
                : map.floors != null && map.floors.Length > 0 ? map.floors[0].id : "main";
        }

        private Sprite LoadFloorOverviewSprite()
        {
            MapDefinition map = ActiveMapDefinition();
            MapFloorDefinition floor = map?.floors?.FirstOrDefault(x =>
                string.Equals(x.id, _currentMapFloor, StringComparison.OrdinalIgnoreCase));
            if (floor != null && !string.IsNullOrEmpty(floor.overviewResource))
            {
                Sprite sprite = Resources.Load<Sprite>(floor.overviewResource);
                if (sprite != null)
                    return sprite;
            }
            return LoadOverviewSprite(string.IsNullOrEmpty(_careerMapId) ? "dust2" : _careerMapId);
        }

        private void CycleMapFloor()
        {
            MapDefinition map = ActiveMapDefinition();
            if (map?.floors == null || map.floors.Length < 2)
                return;
            int current = Array.FindIndex(map.floors, x =>
                string.Equals(x.id, _currentMapFloor, StringComparison.OrdinalIgnoreCase));
            _currentMapFloor = map.floors[(current + 1 + map.floors.Length) % map.floors.Length].id;
            _mapSprite = LoadFloorOverviewSprite();
            if (_mapImage != null)
                _mapImage.sprite = _mapSprite;
            FitMapFrameToSprite(new Vector2(0.175f, 0.115f), new Vector2(0.825f, 0.885f));
            RelayoutZoneLabels();
            RefreshFloorButton();
            RefreshFloorVisibility();
        }

        private void RefreshFloorButton()
        {
            if (_floorButtonText == null)
                return;
            MapDefinition map = ActiveMapDefinition();
            MapFloorDefinition floor = map?.floors?.FirstOrDefault(x =>
                string.Equals(x.id, _currentMapFloor, StringComparison.OrdinalIgnoreCase));
            _floorButtonText.text = floor != null && !string.IsNullOrEmpty(floor.displayName)
                ? floor.displayName
                : _currentMapFloor;
        }

        private bool IsCurrentMatchPlayer(string playerId)
        {
            if (_session == null || string.IsNullOrEmpty(playerId))
                return false;
            return _session.State.TeamA.Definition.playerIds.Contains(playerId) ||
                   _session.State.TeamB.Definition.playerIds.Contains(playerId);
        }

        private void RefreshFloorVisibility()
        {
            MapZoneDefinition[] zones = ActiveMapZones();
            for (int i = 0; i < zones.Length && i < _zoneHitPads.Count; i++)
            {
                MapZoneDefinition zone = zones[i];
                bool visible = !zone.navigationOnly &&
                    string.Equals(ZoneFloor(zone.id), _currentMapFloor, StringComparison.OrdinalIgnoreCase);
                if (_zoneHitPads[i] != null)
                    _zoneHitPads[i].gameObject.SetActive(visible);
            }
            foreach (KeyValuePair<string, RectTransform> pair in _markers)
            {
                if (!IsCurrentMatchPlayer(pair.Key))
                    continue;
                string zoneId = _playerZones.TryGetValue(pair.Key, out string current) ? current : "t_spawn";
                bool visible = !_deadMarkers.Contains(pair.Key) &&
                    string.Equals(ZoneFloor(zoneId), _currentMapFloor, StringComparison.OrdinalIgnoreCase);
                pair.Value.gameObject.SetActive(visible);
            }
            if (_bombMarker != null && !string.IsNullOrEmpty(_bombZoneId))
                _bombMarker.gameObject.SetActive(
                    string.Equals(ZoneFloor(_bombZoneId), _currentMapFloor, StringComparison.OrdinalIgnoreCase));
        }

        private static Sprite LoadOverviewSprite(string mapId)""",
        "floor methods",
    )
    text = replace_once(
        text,
        """        private void ShowBomb(string zoneId)
        {
            if (_bombMarker == null)
                return;
            _bombMarker.gameObject.SetActive(true);
            _bombMarker.anchoredPosition = ClampToMap(LocalZonePosition(zoneId));
            _bombMarker.SetAsLastSibling();
        }

        private void HideBomb()
        {
            if (_bombMarker != null)
                _bombMarker.gameObject.SetActive(false);
        }""",
        """        private void ShowBomb(string zoneId)
        {
            if (_bombMarker == null)
                return;
            _bombZoneId = zoneId;
            _bombMarker.anchoredPosition = ClampToMap(LocalZonePosition(zoneId));
            _bombMarker.SetAsLastSibling();
            RefreshFloorVisibility();
        }

        private void HideBomb()
        {
            _bombZoneId = null;
            if (_bombMarker != null)
                _bombMarker.gameObject.SetActive(false);
        }""",
        "bomb floor visibility",
    )
    text = replace_once(
        text,
        """            foreach (KeyValuePair<string, RectTransform> pair in _markers)
                pair.Value.gameObject.SetActive(active.Contains(pair.Key));
        }""",
        """            foreach (KeyValuePair<string, RectTransform> pair in _markers)
                pair.Value.gameObject.SetActive(active.Contains(pair.Key));
            RefreshFloorVisibility();
        }""",
        "sync floor visibility",
    )
    text = replace_once(
        text,
        """            Image map = CreateUiObject<Image>("ActiveMap", _mapRotator);
            _mapRect = map.rectTransform;
            Stretch(_mapRect);
            _mapRect.pivot = new Vector2(0.5f, 0.5f);
            _mapSprite = LoadOverviewSprite(_careerMapId);
            map.sprite = _mapSprite;""",
        """            Image map = CreateUiObject<Image>("ActiveMap", _mapRotator);
            _mapImage = map;
            _mapRect = map.rectTransform;
            Stretch(_mapRect);
            _mapRect.pivot = new Vector2(0.5f, 0.5f);
            _mapSprite = LoadFloorOverviewSprite();
            map.sprite = _mapSprite;""",
        "floor overview",
    )
    text = replace_once(
        text,
        """            _skipButtonRect = skipBtn.GetComponent<RectTransform>();
            CaptureButtonChrome(skipBtn, out _skipButtonEdge, out _skipButtonFace);
            Button newSeedBtn = CreateButton("NewSeed", "下一場", controlBar.transform,""",
        """            _skipButtonRect = skipBtn.GetComponent<RectTransform>();
            CaptureButtonChrome(skipBtn, out _skipButtonEdge, out _skipButtonFace);
            Button floorBtn = CreateButton("Floor", "", controlBar.transform,
                new Vector2(0.79f, 0.15f), new Vector2(0.855f, 0.85f), CycleMapFloor);
            _floorButtonRect = floorBtn.GetComponent<RectTransform>();
            _floorButtonText = floorBtn.GetComponentInChildren<Text>();
            _floorButtonRect.gameObject.SetActive(false);
            Button newSeedBtn = CreateButton("NewSeed", "下一場", controlBar.transform,""",
        "floor button creation",
    )
    text = replace_once(
        text,
        """                _zoneHitPads.Add(hit);
                _zoneLabels.Add(label);
            }
        }""",
        """                _zoneHitPads.Add(hit);
                _zoneLabels.Add(label);
            }
            RefreshFloorVisibility();
        }""",
        "zone floor visibility",
    )

    for needle in (
        "CycleMapFloor", "LoadFloorOverviewSprite", "RefreshFloorVisibility",
        "特性觸發", "ApplyControlBarLayout(simulation)",
    ):
        if needle not in text:
            raise RuntimeError(f"missing verification needle: {needle}")
    TARGET.write_text(text, encoding="utf-8", newline="\n")
    print("patched", TARGET)


if __name__ == "__main__":
    main()
