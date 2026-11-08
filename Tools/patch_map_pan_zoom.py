# -*- coding: utf-8 -*-
"""Battle map: larger default zoom, edge pan, scroll zoom, smaller player dots."""
from pathlib import Path
import backup_quickmatch as bak

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"
CSPROJ = Path(__file__).resolve().parents[1] / "TacticaUniversalis.Presentation.csproj"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing:\n{old[:280]!r}")
    return text.replace(old, new, 1)


def main() -> None:
    bak.backup("before_map_pan_zoom_apply")
    text = TARGET.read_text(encoding="utf-8")
    if "擊殺廣播" not in text and "特性觸發" not in text:
        raise RuntimeError("UTF-8 corrupted — abort")

    # Smaller player dots + pan/zoom constants
    text = must_replace(
        text,
        """        /// <summary>Visual marker dot diameter in map-local pixels.</summary>
        private const float MarkerDotDiameter = 28f;
        /// <summary>Collision radius = 50% of visual circle diameter / 2.</summary>
        private const float GeneralCollisionRadius = MarkerDotDiameter * 0.25f;
        /// <summary>General-mode visual travel speed in map-local pixels per second.</summary>
        private const float GeneralChaseSpeed = 58f;""",
        """        /// <summary>Visual marker dot diameter in map-local pixels.</summary>
        private const float MarkerDotDiameter = 16f;
        /// <summary>Collision radius = 50% of visual circle diameter / 2.</summary>
        private const float GeneralCollisionRadius = MarkerDotDiameter * 0.25f;
        /// <summary>General-mode visual travel speed in map-local pixels per second.</summary>
        private const float GeneralChaseSpeed = 58f;
        private const float MapZoomMin = 0.75f;
        private const float MapZoomMax = 4.5f;
        private const float MapZoomDefault = 1.95f;
        private const float MapZoomScrollFactor = 0.14f;
        private const float MapEdgePanMargin = 0.09f;
        private const float MapEdgePanSpeed = 520f;""",
    )

    if "_mapZoom" not in text:
        text = must_replace(
            text,
            """        private RectTransform _mapRotator;
        private RectTransform _mapRect;
        private Image _mapImage;""",
            """        private RectTransform _mapRotator;
        private RectTransform _mapRect;
        private float _mapZoom = MapZoomDefault;
        private Vector2 _mapPan = Vector2.zero;
        private Image _mapImage;""",
        )

    # Tight zone ring for smaller dots
    text = must_replace(
        text,
        """            GetMapContentSize(out float width, out float height);
            float radius = Mathf.Clamp(Mathf.Min(width, height) * 0.032f, 14f, 26f);""",
        """            GetMapContentSize(out float width, out float height);
            float radius = Mathf.Clamp(
                Mathf.Min(width, height) * 0.022f,
                MarkerDotDiameter * 0.55f,
                MarkerDotDiameter * 1.15f);""",
    )

    # Mask overflow when zoomed/panned
    text = must_replace(
        text,
        """            Image mapFace = CreateUiObject<Image>("MapFace", mapFrame.transform);
            Stretch(mapFace.rectTransform);
            mapFace.rectTransform.offsetMin = new Vector2(3f, 3f);
            mapFace.rectTransform.offsetMax = new Vector2(-3f, -3f);
            mapFace.color = new Color(Panel.r, Panel.g, Panel.b, 0.28f);
            mapFace.raycastTarget = false;
            _mapFrameFace = mapFace;""",
        """            Image mapFace = CreateUiObject<Image>("MapFace", mapFrame.transform);
            Stretch(mapFace.rectTransform);
            mapFace.rectTransform.offsetMin = new Vector2(3f, 3f);
            mapFace.rectTransform.offsetMax = new Vector2(-3f, -3f);
            mapFace.color = new Color(Panel.r, Panel.g, Panel.b, 0.28f);
            mapFace.raycastTarget = false;
            if (mapFace.GetComponent<RectMask2D>() == null)
                mapFace.gameObject.AddComponent<RectMask2D>();
            _mapFrameFace = mapFace;""",
    )

    # Apply default pan/zoom after fitting frame
    text = must_replace(
        text,
        """            FitMapFrameToSprite(
                new Vector2(0.175f, 0.115f),
                new Vector2(0.825f, 0.885f));

            // Bottom controls — wider than map so “叫暫停(n)” has room""",
        """            FitMapFrameToSprite(
                new Vector2(0.175f, 0.115f),
                new Vector2(0.825f, 0.885f));
            _mapZoom = MapZoomDefault;
            _mapPan = Vector2.zero;
            ApplyMapPanZoom();

            // Bottom controls — wider than map so “叫暫停(n)” has room""",
    )

    # Keep view after floor swap
    text = must_replace(
        text,
        """            FitMapFrameToSprite(new Vector2(0.175f, 0.115f), new Vector2(0.825f, 0.885f));
            RelayoutZoneLabels();
            RefreshFloorButton();
            RefreshFloorVisibility();""",
        """            FitMapFrameToSprite(new Vector2(0.175f, 0.115f), new Vector2(0.825f, 0.885f));
            ApplyMapPanZoom();
            RelayoutZoneLabels();
            RefreshFloorButton();
            RefreshFloorVisibility();""",
    )

    # Insert pan/zoom helpers before GetMapContentSize
    if "private void ApplyMapPanZoom()" not in text:
        text = must_replace(
            text,
            """        private void GetMapContentSize(out float width, out float height)
        {
            Rect rect = _mapRect != null ? _mapRect.rect : new Rect(0f, 0f, 1f, 1f);
            width = rect.width;
            height = rect.height;
        }""",
            """        private void ApplyMapPanZoom()
        {
            if (_mapRotator == null)
                return;
            _mapZoom = Mathf.Clamp(_mapZoom, MapZoomMin, MapZoomMax);
            _mapRotator.localScale = new Vector3(_mapZoom, _mapZoom, 1f);
            _mapRotator.anchoredPosition = _mapPan;
        }

        private bool TryMapPointerLocal(out Vector2 local)
        {
            local = Vector2.zero;
            if (_mapFrame == null)
                return false;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _mapFrame, Input.mousePosition, null, out local))
                return false;
            return _mapFrame.rect.Contains(local);
        }

        private void TickMapCamera()
        {
            if (_mapFrame == null || _mapRotator == null)
                return;
            if (_settingsPanel != null && _settingsPanel.IsVisible)
                return;
            if (_officialResultRoot != null && _officialResultRoot.activeSelf)
                return;
            if (!TryMapPointerLocal(out Vector2 local))
                return;

            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                float factor = 1f + MapZoomScrollFactor;
                _mapZoom = scroll > 0f
                    ? _mapZoom * factor
                    : _mapZoom / factor;
                ApplyMapPanZoom();
            }

            Rect r = _mapFrame.rect;
            float nx = (local.x - r.xMin) / Mathf.Max(1f, r.width);
            float ny = (local.y - r.yMin) / Mathf.Max(1f, r.height);
            Vector2 dir = Vector2.zero;
            if (nx <= MapEdgePanMargin)
                dir.x -= 1f;
            if (nx >= 1f - MapEdgePanMargin)
                dir.x += 1f;
            if (ny <= MapEdgePanMargin)
                dir.y -= 1f;
            if (ny >= 1f - MapEdgePanMargin)
                dir.y += 1f;
            if (dir.sqrMagnitude < 0.0001f)
                return;

            dir.Normalize();
            // Mouse at an edge pans the view that way (map content slides opposite).
            float speed = MapEdgePanSpeed * Time.unscaledDeltaTime;
            _mapPan -= dir * speed;
            // Soft overscroll — allow leaving the map art, but not endlessly.
            GetMapContentSize(out float mapW, out float mapH);
            float viewW = Mathf.Max(1f, r.width);
            float viewH = Mathf.Max(1f, r.height);
            float halfX = (mapW * _mapZoom + viewW) * 0.55f;
            float halfY = (mapH * _mapZoom + viewH) * 0.55f;
            _mapPan.x = Mathf.Clamp(_mapPan.x, -halfX, halfX);
            _mapPan.y = Mathf.Clamp(_mapPan.y, -halfY, halfY);
            ApplyMapPanZoom();
        }

        private void GetMapContentSize(out float width, out float height)
        {
            Rect rect = _mapRect != null ? _mapRect.rect : new Rect(0f, 0f, 1f, 1f);
            width = rect.width;
            height = rect.height;
        }""",
        )

    # Hook Update
    text = must_replace(
        text,
        """        private void Update()
        {
            TickGeneralChases();
            PruneExpiredKillFeed();""",
        """        private void Update()
        {
            TickMapCamera();
            TickGeneralChases();
            PruneExpiredKillFeed();""",
    )

    # Smaller marker visuals driven by MarkerDotDiameter
    text = must_replace(
        text,
        """                    root.transform.SetParent(_mapRect, false);
                    RectTransform rootRect = root.GetComponent<RectTransform>();
                    rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0.5f);
                    rootRect.sizeDelta = new Vector2(36f, 36f);

                    Image ring = CreateUiObject<Image>("Ring", root.transform);
                    ring.sprite = _ringSprite;
                    ring.color = new Color(1f, 1f, 1f, 0.92f);
                    ring.rectTransform.anchorMin = ring.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    ring.rectTransform.sizeDelta = new Vector2(34f, 34f);

                    Image marker = CreateUiObject<Image>("Dot", root.transform);
                    marker.sprite = _circleSprite;
                    marker.color = CtText;
                    marker.rectTransform.anchorMin = marker.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    marker.rectTransform.sizeDelta = new Vector2(28f, 28f);

                    Text label = CreateText("Label", root.transform, 15, TextAnchor.MiddleCenter, TextPrimary);
                    label.text = number.ToString();
                    label.fontStyle = FontStyle.Bold;
                    label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    label.rectTransform.sizeDelta = new Vector2(28f, 28f);
                    label.rectTransform.anchoredPosition = Vector2.zero;""",
        """                    root.transform.SetParent(_mapRect, false);
                    RectTransform rootRect = root.GetComponent<RectTransform>();
                    rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0.5f);
                    float rootSize = MarkerDotDiameter + 6f;
                    rootRect.sizeDelta = new Vector2(rootSize, rootSize);

                    Image ring = CreateUiObject<Image>("Ring", root.transform);
                    ring.sprite = _ringSprite;
                    ring.color = new Color(1f, 1f, 1f, 0.92f);
                    ring.rectTransform.anchorMin = ring.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    ring.rectTransform.sizeDelta = new Vector2(MarkerDotDiameter + 4f, MarkerDotDiameter + 4f);

                    Image marker = CreateUiObject<Image>("Dot", root.transform);
                    marker.sprite = _circleSprite;
                    marker.color = CtText;
                    marker.rectTransform.anchorMin = marker.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    marker.rectTransform.sizeDelta = new Vector2(MarkerDotDiameter, MarkerDotDiameter);

                    int labelSize = Mathf.Clamp(Mathf.RoundToInt(MarkerDotDiameter * 0.7f), 9, 14);
                    Text label = CreateText("Label", root.transform, labelSize, TextAnchor.MiddleCenter, TextPrimary);
                    label.text = number.ToString();
                    label.fontStyle = FontStyle.Bold;
                    label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    label.rectTransform.sizeDelta = new Vector2(MarkerDotDiameter, MarkerDotDiameter);
                    label.rectTransform.anchoredPosition = Vector2.zero;""",
    )

    # Slightly smaller bomb marker to match
    text = must_replace(
        text,
        """            _bombMarker.anchorMin = _bombMarker.anchorMax = new Vector2(0.5f, 0.5f);
            _bombMarker.sizeDelta = new Vector2(36f, 36f);
            _bombMarker.gameObject.SetActive(false);

            Image glow = CreateUiObject<Image>("Glow", root.transform);
            glow.sprite = _circleSprite;
            glow.color = new Color(0.95f, 0.55f, 0.12f, 0.35f);
            glow.rectTransform.anchorMin = glow.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            glow.rectTransform.sizeDelta = new Vector2(36f, 36f);
            _bombMarkerGlow = glow;

            Image core = CreateUiObject<Image>("Core", root.transform);
            core.sprite = _circleSprite;
            core.color = new Color(0.98f, 0.72f, 0.18f, 1f);
            core.rectTransform.anchorMin = core.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            core.rectTransform.sizeDelta = new Vector2(18f, 18f);
            _bombMarkerCore = core;

            Text label = CreateText("Label", root.transform, 14, TextAnchor.MiddleCenter, TextPrimary);""",
        """            _bombMarker.anchorMin = _bombMarker.anchorMax = new Vector2(0.5f, 0.5f);
            float bombSize = MarkerDotDiameter + 8f;
            _bombMarker.sizeDelta = new Vector2(bombSize, bombSize);
            _bombMarker.gameObject.SetActive(false);

            Image glow = CreateUiObject<Image>("Glow", root.transform);
            glow.sprite = _circleSprite;
            glow.color = new Color(0.95f, 0.55f, 0.12f, 0.35f);
            glow.rectTransform.anchorMin = glow.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            glow.rectTransform.sizeDelta = new Vector2(bombSize, bombSize);
            _bombMarkerGlow = glow;

            Image core = CreateUiObject<Image>("Core", root.transform);
            core.sprite = _circleSprite;
            core.color = new Color(0.98f, 0.72f, 0.18f, 1f);
            core.rectTransform.anchorMin = core.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            core.rectTransform.sizeDelta = new Vector2(MarkerDotDiameter * 0.65f, MarkerDotDiameter * 0.65f);
            _bombMarkerCore = core;

            Text label = CreateText("Label", root.transform, 11, TextAnchor.MiddleCenter, TextPrimary);""",
    )

    TARGET.write_text(text, encoding="utf-8")
    verify = TARGET.read_text(encoding="utf-8")
    if "擊殺廣播" not in verify and "特性觸發" not in verify:
        raise RuntimeError("UTF-8 lost after write")
    for needle in (
        "TickMapCamera",
        "ApplyMapPanZoom",
        "MapZoomDefault",
        "MarkerDotDiameter = 16f",
        "RectMask2D",
    ):
        if needle not in verify:
            raise RuntimeError(f"missing after patch: {needle}")
    if "MarkerDotDiameter = 28f" in verify:
        raise RuntimeError("old marker diameter still present")

    print("patched map pan/zoom OK")


if __name__ == "__main__":
    main()
