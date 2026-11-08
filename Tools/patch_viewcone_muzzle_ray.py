# -*- coding: utf-8 -*-
"""Map markers: view cone + muzzle tracer rays."""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "Tools"))
from backup_quickmatch import backup  # noqa: E402

SRC = ROOT / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str, label: str) -> str:
    if old not in text:
        raise RuntimeError("missing: " + label)
    return text.replace(old, new, 1)


def main() -> int:
    backup("viewcone_muzzle_ray")
    text = SRC.read_text(encoding="utf-8")
    if "KillFeed" not in text:
        raise RuntimeError("UTF-8 anchors missing")

    text = must_replace(
        text,
        """        private readonly Dictionary<string, Image> _markerDots = new Dictionary<string, Image>();
        private readonly Dictionary<string, Text> _markerLabels = new Dictionary<string, Text>();
        private readonly Dictionary<string, Image> _markerHpFills = new Dictionary<string, Image>();
""",
        """        private readonly Dictionary<string, Image> _markerDots = new Dictionary<string, Image>();
        private readonly Dictionary<string, Text> _markerLabels = new Dictionary<string, Text>();
        private readonly Dictionary<string, Image> _markerCones = new Dictionary<string, Image>();
        private readonly Dictionary<string, float> _markerFacingDeg =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        private readonly List<MuzzleRay> _muzzleRays = new List<MuzzleRay>();
        private readonly Dictionary<string, Image> _markerHpFills = new Dictionary<string, Image>();
""",
        "marker cone fields",
    )

    text = must_replace(
        text,
        """        /// <summary>General-mode marker dot diameter in map-local pixels.</summary>
        private const float MarkerDotDiameter = 16f;
""",
        """        /// <summary>General-mode marker dot diameter in map-local pixels.</summary>
        private const float MarkerDotDiameter = 16f;
        private const float ViewConeDegrees = 78f;
        private const float ViewConeLength = 92f;
        private const float MuzzleRayWidth = 3.2f;
        private const float MuzzleRaySeconds = 0.28f;
""",
        "view cone constants",
    )

    text = must_replace(
        text,
        """        private sealed class GeneralChaseState
        {
            public string TargetZone;
            public List<Vector2> Points;
            public float[] SegmentLengths;
            public float TotalLength;
            public float Progress;
            public string[] PathZones;
        }
""",
        """        private sealed class GeneralChaseState
        {
            public string TargetZone;
            public List<Vector2> Points;
            public float[] SegmentLengths;
            public float TotalLength;
            public float Progress;
            public string[] PathZones;
        }

        private sealed class MuzzleRay
        {
            public Image Image;
            public float ExpireAtUnscaled;
            public Color BaseColor;
        }
""",
        "MuzzleRay class",
    )

    text = must_replace(
        text,
                    """                    else if (later.type != SimulationEventType.StatusUpdated &&
                                         later.type != SimulationEventType.Contact &&
                                         later.type != SimulationEventType.TraitTriggered)
                                    break;
""",
        """                    else if (later.type != SimulationEventType.StatusUpdated &&
                                         later.type != SimulationEventType.Contact &&
                                         later.type != SimulationEventType.Shot &&
                                         later.type != SimulationEventType.TraitTriggered)
                                    break;
""",
        "coalesce wait skip Shot",
    )

    text = must_replace(
        text,
        """                    if (later.type == SimulationEventType.StatusUpdated ||
                        later.type == SimulationEventType.Contact ||
                        later.type == SimulationEventType.TraitTriggered)
                        continue;
""",
        """                    if (later.type == SimulationEventType.StatusUpdated ||
                        later.type == SimulationEventType.Contact ||
                        later.type == SimulationEventType.Shot ||
                        later.type == SimulationEventType.TraitTriggered)
                        continue;
""",
        "BuildGeneralMoveCoalesce Shot",
    )

    text = must_replace(
        text,
        """                case SimulationEventType.Kill:
                    if (!string.IsNullOrEmpty(evt.targetId))
""",
        """                case SimulationEventType.Shot:
                    ShowMuzzleRay(evt.actorId, evt.targetId);
                    break;
                case SimulationEventType.Kill:
                    if (!IsGeneralBattleMode())
                        ShowMuzzleRay(evt.actorId, evt.targetId);
                    if (!string.IsNullOrEmpty(evt.targetId))
""",
        "ApplyEvent Shot/Kill rays",
    )

    text = must_replace(
        text,
        """            StopAllMovement();
            _deadMarkers.Clear();
            if (_session == null) return;
""",
        """            StopAllMovement();
            _deadMarkers.Clear();
            ClearMuzzleRays();
            if (_session == null) return;
""",
        "ResetMarkers clear rays",
    )

    text = must_replace(
        text,
            """            RelayoutZone(spawn, animate, 0.25f);
            RefreshFloorVisibility();
        }
""",
        """            RelayoutZone(spawn, animate, 0.25f);
            InitTeamFacings(team, spawn);
            RefreshFloorVisibility();
        }
""",
        "PositionTeam init facings",
    )

    text = must_replace(
        text,
        """                marker.anchoredPosition = ClampToMap(LocalZonePosition(zoneId) + endOffset);
                _moveDestinations.Remove(playerId);
""",
        """                Vector2 instantEnd = ClampToMap(LocalZonePosition(zoneId) + endOffset);
                FaceMarkerToward(playerId, marker.anchoredPosition, instantEnd);
                marker.anchoredPosition = instantEnd;
                _moveDestinations.Remove(playerId);
""",
        "quick instant move facing",
    )

    text = must_replace(
        text,
        """                chase.Progress = Mathf.Min(chase.TotalLength, chase.Progress + step);
                // Stay on the authored path — no mid-chase collision shove (was stuttering).
                Vector2 pos = ClampToMapCollision(
                    SamplePolyline(chase.Points, chase.SegmentLengths, chase.Progress));
                marker.anchoredPosition = pos;
""",
        """                chase.Progress = Mathf.Min(chase.TotalLength, chase.Progress + step);
                // Stay on the authored path — no mid-chase collision shove (was stuttering).
                Vector2 pos = ClampToMapCollision(
                    SamplePolyline(chase.Points, chase.SegmentLengths, chase.Progress));
                FaceMarkerToward(playerId, marker.anchoredPosition, pos);
                marker.anchoredPosition = pos;
""",
        "chase facing",
    )

    text = must_replace(
        text,
        """                        Vector2 lerped = Vector2.Lerp(points[i], points[i + 1], local);
                        marker.anchoredPosition = IsGeneralBattleMode()
                            ? ClampToMapCollision(lerped)
                            : lerped;
""",
        """                        Vector2 lerped = Vector2.Lerp(points[i], points[i + 1], local);
                        Vector2 nextPos = IsGeneralBattleMode()
                            ? ClampToMapCollision(lerped)
                            : lerped;
                        FaceMarkerToward(playerId, marker.anchoredPosition, nextPos);
                        marker.anchoredPosition = nextPos;
""",
        "FollowPathRoutine facing",
    )

    text = must_replace(
        text,
        """                if (_markerDots.TryGetValue(pair.Key, out Image dot) && dot != null)
                    dot.rectTransform.sizeDelta = new Vector2(d, d);
""",
        """                if (_markerDots.TryGetValue(pair.Key, out Image dot) && dot != null)
                    dot.rectTransform.sizeDelta = new Vector2(d, d);
                if (_markerCones.TryGetValue(pair.Key, out Image cone) && cone != null)
                {
                    float coneLen = IsGeneralBattleMode() ? ViewConeLength : ViewConeLength * 1.15f;
                    cone.rectTransform.sizeDelta = new Vector2(coneLen * 2f, coneLen * 2f);
                }
""",
        "resize cones",
    )

    text = must_replace(
        text,
        """        private void RefreshMarkerSideColors()
        {
            foreach (KeyValuePair<string, Image> pair in _markerDots)
            {
                bool isT = PlayerIsTerrorist(pair.Key, _currentRound);
                pair.Value.color = isT ? TText : CtText;
            }
        }
""",
        """        private void RefreshMarkerSideColors()
        {
            foreach (KeyValuePair<string, Image> pair in _markerDots)
            {
                bool isT = PlayerIsTerrorist(pair.Key, _currentRound);
                pair.Value.color = isT ? TText : CtText;
                if (_markerCones.TryGetValue(pair.Key, out Image cone) && cone != null)
                    cone.color = ViewConeColor(isT);
            }
        }
""",
        "cone side colors",
    )

    text = must_replace(
        text,
        """                    Image ring = CreateUiObject<Image>("Ring", root.transform);
                    ring.sprite = _ringSprite;
                    ring.color = new Color(1f, 1f, 1f, 0.92f);
                    ring.rectTransform.anchorMin = ring.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    ring.rectTransform.sizeDelta = new Vector2(d + 4f, d + 4f);
""",
        """                    Image cone = CreateUiObject<Image>("ViewCone", root.transform);
                    cone.sprite = _circleSprite;
                    cone.type = Image.Type.Filled;
                    cone.fillMethod = Image.FillMethod.Radial360;
                    cone.fillOrigin = (int)Image.Origin360.Right;
                    cone.fillClockwise = false;
                    cone.fillAmount = ViewConeDegrees / 360f;
                    cone.color = ViewConeColor(false);
                    cone.raycastTarget = false;
                    cone.rectTransform.anchorMin = cone.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    cone.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    cone.rectTransform.sizeDelta = new Vector2(ViewConeLength * 2f, ViewConeLength * 2f);
                    cone.rectTransform.SetAsFirstSibling();

                    Image ring = CreateUiObject<Image>("Ring", root.transform);
                    ring.sprite = _ringSprite;
                    ring.color = new Color(1f, 1f, 1f, 0.92f);
                    ring.rectTransform.anchorMin = ring.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    ring.rectTransform.sizeDelta = new Vector2(d + 4f, d + 4f);
""",
        "create view cone",
    )

    text = must_replace(
        text,
        """                    _markers[id] = rootRect;
                    _markerDots[id] = marker;
                    _markerLabels[id] = label;
                    _playerZones[id] = "t_spawn";
""",
        """                    _markers[id] = rootRect;
                    _markerDots[id] = marker;
                    _markerLabels[id] = label;
                    _markerCones[id] = cone;
                    _markerFacingDeg[id] = 90f;
                    ApplyViewConeFacing(id, 90f);
                    _playerZones[id] = "t_spawn";
""",
        "register cone",
    )

    text = must_replace(
        text,
        """            TickMapCamera();
            TickGeneralChases();
            PruneExpiredKillFeed();
""",
        """            TickMapCamera();
            TickGeneralChases();
            TickMuzzleRays();
            PruneExpiredKillFeed();
""",
        "Update tick rays",
    )

    helpers = '''
        private static Color ViewConeColor(bool isTerrorist)
        {
            Color c = isTerrorist ? TText : CtText;
            c.a = 0.26f;
            return c;
        }

        private void InitTeamFacings(TeamDefinition team, string spawn)
        {
            if (team == null)
                return;
            Vector2 from = LocalZonePosition(spawn);
            string other = string.Equals(spawn, "t_spawn", StringComparison.OrdinalIgnoreCase)
                ? "ct_spawn"
                : "t_spawn";
            Vector2 to = LocalZonePosition(other);
            float facing = FacingDegrees(to - from);
            for (int i = 0; i < team.playerIds.Length; i++)
                ApplyViewConeFacing(team.playerIds[i], facing);
        }

        private static float FacingDegrees(Vector2 dir)
        {
            if (dir.sqrMagnitude < 0.01f)
                return 90f;
            return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        }

        private void FaceMarkerToward(string playerId, Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            if (delta.sqrMagnitude < 4f)
                return;
            ApplyViewConeFacing(playerId, FacingDegrees(delta));
        }

        private void ApplyViewConeFacing(string playerId, float facingDeg)
        {
            if (string.IsNullOrEmpty(playerId))
                return;
            _markerFacingDeg[playerId] = facingDeg;
            if (!_markerCones.TryGetValue(playerId, out Image cone) || cone == null)
                return;
            cone.rectTransform.localEulerAngles = new Vector3(0f, 0f, facingDeg - ViewConeDegrees * 0.5f);
        }

        private void ShowMuzzleRay(string shooterId, string targetId)
        {
            if (string.IsNullOrEmpty(shooterId) || string.IsNullOrEmpty(targetId))
                return;
            if (!_markers.TryGetValue(shooterId, out RectTransform fromRt) || fromRt == null)
                return;
            if (!_markers.TryGetValue(targetId, out RectTransform toRt) || toRt == null)
                return;
            if (!fromRt.gameObject.activeInHierarchy)
                return;

            Vector2 from = fromRt.anchoredPosition;
            Vector2 to = toRt.anchoredPosition;
            FaceMarkerToward(shooterId, from, to);
            Vector2 delta = to - from;
            float dist = delta.magnitude;
            if (dist < 4f)
                return;

            Image ray = null;
            MuzzleRay slot = null;
            for (int i = 0; i < _muzzleRays.Count; i++)
            {
                if (_muzzleRays[i].Image != null && !_muzzleRays[i].Image.gameObject.activeSelf)
                {
                    slot = _muzzleRays[i];
                    ray = slot.Image;
                    break;
                }
            }
            if (ray == null)
            {
                ray = CreateUiObject<Image>("MuzzleRay", _mapRect);
                ray.sprite = _circleSprite;
                ray.raycastTarget = false;
                ray.rectTransform.anchorMin = ray.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                ray.rectTransform.pivot = new Vector2(0f, 0.5f);
                slot = new MuzzleRay { Image = ray };
                _muzzleRays.Add(slot);
            }

            bool isT = PlayerIsTerrorist(shooterId, _currentRound);
            Color tint = isT ? TText : CtText;
            tint.a = 0.92f;
            ray.color = tint;
            ray.gameObject.SetActive(true);
            ray.rectTransform.SetAsLastSibling();
            ray.rectTransform.anchoredPosition = from;
            ray.rectTransform.sizeDelta = new Vector2(dist, MuzzleRayWidth);
            ray.rectTransform.localEulerAngles = new Vector3(0f, 0f, FacingDegrees(delta));
            slot.BaseColor = tint;
            slot.ExpireAtUnscaled = Time.unscaledTime + MuzzleRaySeconds;
        }

        private void TickMuzzleRays()
        {
            if (_muzzleRays.Count == 0)
                return;
            float now = Time.unscaledTime;
            for (int i = 0; i < _muzzleRays.Count; i++)
            {
                MuzzleRay ray = _muzzleRays[i];
                if (ray.Image == null || !ray.Image.gameObject.activeSelf)
                    continue;
                float left = ray.ExpireAtUnscaled - now;
                if (left <= 0f)
                {
                    ray.Image.gameObject.SetActive(false);
                    continue;
                }
                Color c = ray.BaseColor;
                c.a = ray.BaseColor.a * Mathf.Clamp01(left / MuzzleRaySeconds);
                ray.Image.color = c;
            }
        }

        private void ClearMuzzleRays()
        {
            for (int i = 0; i < _muzzleRays.Count; i++)
            {
                if (_muzzleRays[i].Image != null)
                    _muzzleRays[i].Image.gameObject.SetActive(false);
            }
        }

'''

    text = must_replace(
        text,
        "        private static Sprite CreateCircleSprite(bool filled)\n",
        helpers + "        private static Sprite CreateCircleSprite(bool filled)\n",
        "insert helpers",
    )

    SRC.write_text(text, encoding="utf-8")
    check = SRC.read_text(encoding="utf-8")
    for need in (
        "ViewCone",
        "ShowMuzzleRay",
        "EventType.Shot",
        "SimulationEventType.Shot",
        "TickMuzzleRays",
        "ApplyControlBarLayout(simulation)",
    ):
        if need not in check and need != "EventType.Shot":
            if need == "SimulationEventType.Shot" and "SimulationEventType.Shot" not in check:
                raise RuntimeError("missing " + need)
            if need != "EventType.Shot" and need not in check:
                raise RuntimeError("missing " + need)
    if "SimulationEventType.Shot" not in check:
        raise RuntimeError("Shot event handling missing")
    if "ApplyControlBarLayout(!simulation)" in check:
        raise RuntimeError("control bar polarity broken")
    if "擊殺廣播" not in check and "叫暫停" not in check and "KillFeed" not in check:
        raise RuntimeError("utf8 damaged")
    print("patched view cone + muzzle rays")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
