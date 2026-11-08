# -*- coding: utf-8 -*-
"""Gate map pan/zoom + small markers to general mode only; quick mode stays classic."""
from pathlib import Path
import backup_quickmatch as bak

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing:\n{old[:280]!r}")
    return text.replace(old, new, 1)


def main() -> None:
    bak.backup("before_map_cam_general_only")
    text = TARGET.read_text(encoding="utf-8")
    if "叫暫停" not in text and "特性觸發" not in text:
        raise RuntimeError("UTF-8 corrupted — abort")

    text = must_replace(
        text,
        """        /// <summary>Visual marker dot diameter in map-local pixels.</summary>
        private const float MarkerDotDiameter = 16f;
        /// <summary>Collision radius = 50% of visual circle diameter / 2.</summary>
        private const float GeneralCollisionRadius = MarkerDotDiameter * 0.25f;""",
        """        /// <summary>General-mode marker dot diameter in map-local pixels.</summary>
        private const float MarkerDotDiameter = 16f;
        /// <summary>Quick-mode marker dot diameter (unchanged classic size).</summary>
        private const float QuickMarkerDotDiameter = 28f;
        /// <summary>Collision radius = 50% of visual circle diameter / 2.</summary>
        private const float GeneralCollisionRadius = MarkerDotDiameter * 0.25f;""",
    )

    text = must_replace(
        text,
        """        private float _mapZoom = MapZoomDefault;
        private Vector2 _mapPan = Vector2.zero;""",
        """        private float _mapZoom = 1f;
        private Vector2 _mapPan = Vector2.zero;""",
    )

    # Slot ring uses active diameter
    text = must_replace(
        text,
        """            float radius = Mathf.Clamp(
                Mathf.Min(width, height) * 0.022f,
                MarkerDotDiameter * 0.55f,
                MarkerDotDiameter * 1.15f);""",
        """            float dot = ActiveMarkerDotDiameter();
            float radius = Mathf.Clamp(
                Mathf.Min(width, height) * (IsGeneralBattleMode() ? 0.022f : 0.032f),
                dot * 0.55f,
                IsGeneralBattleMode() ? dot * 1.15f : 26f);""",
    )

    # Camera apply + tick gated; add helpers
    text = must_replace(
        text,
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
                return;""",
        """        private float ActiveMarkerDotDiameter() =>
            IsGeneralBattleMode() ? MarkerDotDiameter : QuickMarkerDotDiameter;

        private void ApplyMapPanZoom()
        {
            if (_mapRotator == null)
                return;
            if (!IsGeneralBattleMode())
            {
                _mapZoom = 1f;
                _mapPan = Vector2.zero;
            }
            else
                _mapZoom = Mathf.Clamp(_mapZoom, MapZoomMin, MapZoomMax);
            _mapRotator.localScale = new Vector3(_mapZoom, _mapZoom, 1f);
            _mapRotator.anchoredPosition = _mapPan;
        }

        private void ApplyMapCameraForMode()
        {
            if (IsGeneralBattleMode())
            {
                _mapZoom = MapZoomDefault;
                _mapPan = Vector2.zero;
            }
            else
            {
                _mapZoom = 1f;
                _mapPan = Vector2.zero;
            }
            ApplyMapPanZoom();
            ApplyMarkerVisualSizes();
        }

        private void ApplyMarkerVisualSizes()
        {
            float d = ActiveMarkerDotDiameter();
            float rootSize = d + 6f;
            foreach (KeyValuePair<string, RectTransform> pair in _markers)
            {
                RectTransform root = pair.Value;
                if (root == null)
                    continue;
                root.sizeDelta = new Vector2(rootSize, rootSize);
                Transform ringT = root.Find("Ring");
                if (ringT is RectTransform ring)
                    ring.sizeDelta = new Vector2(d + 4f, d + 4f);
                if (_markerDots.TryGetValue(pair.Key, out Image dot) && dot != null)
                    dot.rectTransform.sizeDelta = new Vector2(d, d);
                if (_markerLabels.TryGetValue(pair.Key, out Text label) && label != null)
                {
                    label.fontSize = IsGeneralBattleMode()
                        ? Mathf.Clamp(Mathf.RoundToInt(d * 0.7f), 9, 14)
                        : 15;
                    label.rectTransform.sizeDelta = new Vector2(d, d);
                }
            }

            if (_bombMarker != null)
            {
                float bombSize = d + 8f;
                _bombMarker.sizeDelta = new Vector2(bombSize, bombSize);
                if (_bombMarkerGlow != null)
                    _bombMarkerGlow.rectTransform.sizeDelta = new Vector2(bombSize, bombSize);
                if (_bombMarkerCore != null)
                {
                    float core = IsGeneralBattleMode() ? d * 0.65f : 18f;
                    _bombMarkerCore.rectTransform.sizeDelta = new Vector2(core, core);
                }
                Text bombLabel = _bombMarker.GetComponentInChildren<Text>();
                if (bombLabel != null)
                    bombLabel.fontSize = IsGeneralBattleMode() ? 11 : 14;
            }
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
            if (!IsGeneralBattleMode())
                return;
            if (_mapFrame == null || _mapRotator == null)
                return;
            if (_settingsPanel != null && _settingsPanel.IsVisible)
                return;
            if (_officialResultRoot != null && _officialResultRoot.activeSelf)
                return;
            if (!TryMapPointerLocal(out Vector2 local))
                return;""",
    )

    # Hook ApplyMatchModeUi
    text = must_replace(
        text,
        """            ApplyControlBarLayout(simulation);
            ApplyScoreboardModeChrome();
            ApplySettingsFrostedChrome(true);
            bool floorSwitch = MapSupportsFloors();""",
        """            ApplyControlBarLayout(simulation);
            ApplyScoreboardModeChrome();
            ApplySettingsFrostedChrome(true);
            ApplyMapCameraForMode();
            bool floorSwitch = MapSupportsFloors();""",
    )

    # Build UI: don't force general zoom before mode is known
    text = must_replace(
        text,
        """            FitMapFrameToSprite(
                new Vector2(0.175f, 0.115f),
                new Vector2(0.825f, 0.885f));
            _mapZoom = MapZoomDefault;
            _mapPan = Vector2.zero;
            ApplyMapPanZoom();

            // Bottom controls — wider than map so “叫暫停(n)” has room""",
        """            FitMapFrameToSprite(
                new Vector2(0.175f, 0.115f),
                new Vector2(0.825f, 0.885f));
            _mapZoom = 1f;
            _mapPan = Vector2.zero;
            ApplyMapPanZoom();

            // Bottom controls — wider than map so “叫暫停(n)” has room""",
    )

    # CreateMarkers: start at quick classic size; ApplyMatchModeUi resizes for general
    text = must_replace(
        text,
        """                    rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0.5f);
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
        """                    rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0.5f);
                    float d = QuickMarkerDotDiameter;
                    float rootSize = d + 6f;
                    rootRect.sizeDelta = new Vector2(rootSize, rootSize);

                    Image ring = CreateUiObject<Image>("Ring", root.transform);
                    ring.sprite = _ringSprite;
                    ring.color = new Color(1f, 1f, 1f, 0.92f);
                    ring.rectTransform.anchorMin = ring.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    ring.rectTransform.sizeDelta = new Vector2(d + 4f, d + 4f);

                    Image marker = CreateUiObject<Image>("Dot", root.transform);
                    marker.sprite = _circleSprite;
                    marker.color = CtText;
                    marker.rectTransform.anchorMin = marker.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    marker.rectTransform.sizeDelta = new Vector2(d, d);

                    Text label = CreateText("Label", root.transform, 15, TextAnchor.MiddleCenter, TextPrimary);
                    label.text = number.ToString();
                    label.fontStyle = FontStyle.Bold;
                    label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    label.rectTransform.sizeDelta = new Vector2(d, d);
                    label.rectTransform.anchoredPosition = Vector2.zero;""",
    )

    # Bomb create: classic sizes by default
    text = must_replace(
        text,
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
    )

    TARGET.write_text(text, encoding="utf-8")
    verify = TARGET.read_text(encoding="utf-8")
    if "叫暫停" not in verify and "特性觸發" not in verify:
        raise RuntimeError("UTF-8 lost after write")
    for needle in (
        "QuickMarkerDotDiameter",
        "ApplyMapCameraForMode",
        "ApplyMarkerVisualSizes",
        "if (!IsGeneralBattleMode())\n                return;",
        "ActiveMarkerDotDiameter",
    ):
        if needle not in verify:
            raise RuntimeError(f"missing after patch: {needle}")
    print("patched map camera general-only OK")


if __name__ == "__main__":
    main()
