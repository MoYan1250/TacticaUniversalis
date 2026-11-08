# -*- coding: utf-8 -*-
"""Clip view cones and sight rays against overview collision cells."""
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
    backup("map_collision_los")
    text = SRC.read_text(encoding="utf-8")
    if "SightRay" not in text or "ShowMuzzleRay" not in text:
        raise RuntimeError("sight/muzzle anchors missing")
    if "ApplyControlBarLayout(simulation)" not in text:
        raise RuntimeError("control bar polarity missing")

    text = must_replace(
        text,
        """        private readonly Dictionary<string, Image> _markerCones = new Dictionary<string, Image>();
        private readonly Dictionary<string, Image> _markerSightRays = new Dictionary<string, Image>();
""",
        """        private readonly Dictionary<string, Image> _markerCones = new Dictionary<string, Image>();
        private readonly Dictionary<string, ViewConeMeshGraphic> _markerConeMeshes =
            new Dictionary<string, ViewConeMeshGraphic>();
        private readonly Dictionary<string, Image> _markerSightRays = new Dictionary<string, Image>();
        private readonly MapCollisionGrid _mapCollision = new MapCollisionGrid();
        private readonly Vector2[] _coneRimScratch = new Vector2[24];
""",
        "collision + cone mesh fields",
    )

    text = must_replace(
        text,
        """        private const float ViewConeDegrees = 78f;
        private const float ViewConeLength = 92f;
""",
        """        private const float ViewConeDegrees = 78f;
        private const float ViewConeLength = 92f;
        private const int ViewConeRaySamples = 20;
""",
        "cone sample count",
    )

    text = must_replace(
        text,
            """            TickMapCamera();
            TickGeneralChases();
            TickMuzzleRays();
            PruneExpiredKillFeed();
""",
        """            TickMapCamera();
            TickGeneralChases();
            TickMarkerLos();
            TickMuzzleRays();
            PruneExpiredKillFeed();
""",
        "Update tick LOS",
    )

    text = must_replace(
        text,
        """            FitMapFrameToSprite(new Vector2(0.175f, 0.115f), new Vector2(0.825f, 0.885f));
            ApplyMapPanZoom();
            RelayoutZoneLabels();
            RefreshFloorButton();
            RefreshFloorVisibility();
        }
""",
        """            FitMapFrameToSprite(new Vector2(0.175f, 0.115f), new Vector2(0.825f, 0.885f));
            ApplyMapPanZoom();
            RebuildMapCollision();
            RelayoutZoneLabels();
            RefreshFloorButton();
            RefreshFloorVisibility();
        }
""",
        "rebuild collision on floor change",
    )

    text = must_replace(
        text,
        """            FitMapFrameToSprite(
                new Vector2(0.175f, 0.115f),
                new Vector2(0.825f, 0.885f));
            _mapZoom = 1f;
            _mapPan = Vector2.zero;
            ApplyMapPanZoom();
""",
        """            FitMapFrameToSprite(
                new Vector2(0.175f, 0.115f),
                new Vector2(0.825f, 0.885f));
            _mapZoom = 1f;
            _mapPan = Vector2.zero;
            ApplyMapPanZoom();
            RebuildMapCollision();
""",
        "rebuild collision on map create",
    )

    text = must_replace(
        text,
        """                SeparateGeneralAgentsNear(spawn);
            }
            else
                RelayoutZone(spawn, animate, 0.25f);
            InitTeamFacings(team, spawn);
""",
        """                SeparateGeneralAgentsNear(spawn);
            }
            else
                RelayoutZone(spawn, animate, 0.25f);
            InitTeamFacings(team, spawn);
            RefreshAllMarkerLos();
""",
        "refresh LOS after spawn layout",
    )

    text = must_replace(
        text,
        """                    cone.rectTransform.sizeDelta = new Vector2(ViewConeLength * 2f, ViewConeLength * 2f);
                    cone.rectTransform.SetAsFirstSibling();

                    Image sight = CreateUiObject<Image>("SightRay", root.transform);
""",
        """                    cone.rectTransform.sizeDelta = new Vector2(ViewConeLength * 2f, ViewConeLength * 2f);
                    cone.rectTransform.SetAsFirstSibling();
                    cone.enabled = false;
                    cone.raycastTarget = false;
                    ViewConeMeshGraphic coneMesh = cone.gameObject.AddComponent<ViewConeMeshGraphic>();
                    coneMesh.raycastTarget = false;
                    coneMesh.color = ViewConeColor(false);

                    Image sight = CreateUiObject<Image>("SightRay", root.transform);
""",
        "add clipped cone mesh",
    )

    text = must_replace(
        text,
        """                    _markerCones[id] = cone;
                    _markerSightRays[id] = sight;
""",
        """                    _markerCones[id] = cone;
                    _markerConeMeshes[id] = coneMesh;
                    _markerSightRays[id] = sight;
""",
        "register cone mesh",
    )

    text = must_replace(
        text,
        """                if (_markerCones.TryGetValue(pair.Key, out Image cone) && cone != null)
                    cone.color = ViewConeColor(isT);
""",
        """                if (_markerCones.TryGetValue(pair.Key, out Image cone) && cone != null)
                    cone.color = ViewConeColor(isT);
                if (_markerConeMeshes.TryGetValue(pair.Key, out ViewConeMeshGraphic mesh) && mesh != null)
                    mesh.color = ViewConeColor(isT);
""",
        "cone mesh side color",
    )

    text = must_replace(
        text,
        """        private void ApplyViewConeFacing(string playerId, float facingDeg)
        {
            if (string.IsNullOrEmpty(playerId))
                return;
            _markerFacingDeg[playerId] = facingDeg;
            if (_markerCones.TryGetValue(playerId, out Image cone) && cone != null)
                cone.rectTransform.localEulerAngles = new Vector3(0f, 0f, facingDeg - ViewConeDegrees * 0.5f);
            if (_markerSightRays.TryGetValue(playerId, out Image sight) && sight != null)
            {
                float rad = facingDeg * Mathf.Deg2Rad;
                float muzzle = ActiveMarkerDotDiameter() * 0.48f;
                sight.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * muzzle;
                sight.rectTransform.localEulerAngles = new Vector3(0f, 0f, facingDeg);
            }
        }
""",
        """        private void ApplyViewConeFacing(string playerId, float facingDeg)
        {
            if (string.IsNullOrEmpty(playerId))
                return;
            _markerFacingDeg[playerId] = facingDeg;
            RefreshMarkerLos(playerId);
        }

        private void RebuildMapCollision()
        {
            GetMapContentSize(out float width, out float height);
            _mapCollision.Rebuild(_mapSprite, width, height);
            RefreshAllMarkerLos();
        }

        private void TickMarkerLos()
        {
            if (!IsGeneralBattleMode() || !_mapCollision.IsReady)
                return;
            foreach (KeyValuePair<string, RectTransform> pair in _markers)
            {
                if (pair.Value != null && pair.Value.gameObject.activeInHierarchy)
                    RefreshMarkerLos(pair.Key);
            }
        }

        private void RefreshAllMarkerLos()
        {
            foreach (KeyValuePair<string, RectTransform> pair in _markers)
                RefreshMarkerLos(pair.Key);
        }

        private void RefreshMarkerLos(string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
                return;
            if (!_markers.TryGetValue(playerId, out RectTransform marker) || marker == null)
                return;
            if (!_markerFacingDeg.TryGetValue(playerId, out float facingDeg))
                facingDeg = 90f;

            bool general = IsGeneralBattleMode();
            float coneLen = general ? ViewConeLength : ViewConeLength * 1.15f;
            int samples = ViewConeRaySamples;
            Vector2 origin = marker.anchoredPosition;
            float half = ViewConeDegrees * 0.5f;
            for (int i = 0; i < samples; i++)
            {
                float t = samples <= 1 ? 0.5f : i / (float)(samples - 1);
                float ang = (facingDeg - half + ViewConeDegrees * t) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                float dist = coneLen;
                if (general && _mapCollision.IsReady)
                    dist = _mapCollision.Raycast(origin, dir, coneLen);
                _coneRimScratch[i] = dir * dist;
            }

            if (_markerConeMeshes.TryGetValue(playerId, out ViewConeMeshGraphic mesh) && mesh != null)
            {
                bool isT = PlayerIsTerrorist(playerId, _currentRound);
                mesh.rectTransform.localEulerAngles = Vector3.zero;
                mesh.SetFan(ViewConeColor(isT), _coneRimScratch, samples);
            }
            else if (_markerCones.TryGetValue(playerId, out Image cone) && cone != null)
                cone.rectTransform.localEulerAngles = new Vector3(0f, 0f, facingDeg - ViewConeDegrees * 0.5f);

            if (_markerSightRays.TryGetValue(playerId, out Image sight) && sight != null)
            {
                float rad = facingDeg * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                float muzzle = ActiveMarkerDotDiameter() * 0.48f;
                sight.rectTransform.anchoredPosition = dir * muzzle;
                sight.rectTransform.localEulerAngles = new Vector3(0f, 0f, facingDeg);
                float maxLen = general ? SightRayLength : SightRayLength * 1.08f;
                float hit = maxLen;
                if (general && _mapCollision.IsReady)
                    hit = _mapCollision.Raycast(origin + dir * muzzle, dir, maxLen);
                sight.rectTransform.sizeDelta = new Vector2(Mathf.Max(2f, hit), SightRayWidth);
                sight.gameObject.SetActive(general && marker.gameObject.activeSelf);
            }
        }
""",
        "LOS clip cone + sight ray",
    )

    text = must_replace(
        text,
        """            Vector2 from = fromRt.anchoredPosition;
            Vector2 to = toRt.anchoredPosition;
            FaceMarkerToward(shooterId, from, to);
            Vector2 delta = to - from;
""",
        """            Vector2 from = fromRt.anchoredPosition;
            Vector2 to = toRt.anchoredPosition;
            FaceMarkerToward(shooterId, from, to);
            if (IsGeneralBattleMode() && _mapCollision.IsReady)
                _mapCollision.TryClipSegment(from, to, out to);
            Vector2 delta = to - from;
""",
        "clip fire tracer by walls",
    )

    SRC.write_text(text, encoding="utf-8")
    check = SRC.read_text(encoding="utf-8")
    for need in (
        "RebuildMapCollision",
        "RefreshMarkerLos",
        "ViewConeMeshGraphic",
        "MapCollisionGrid",
        "TickMarkerLos",
        "TryClipSegment",
        "ApplyControlBarLayout(simulation)",
        "叫暫停",
    ):
        if need not in check:
            raise RuntimeError("missing after patch: " + need)
    if "ApplyControlBarLayout(!simulation)" in check:
        raise RuntimeError("control bar polarity broken")
    print("patched map collision LOS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
