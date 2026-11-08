# -*- coding: utf-8 -*-
"""
Battle map frame: frosted glass chrome matching control buttons.
- MapFrame uses Panel frost colors (edge + inset face)
- Map content inset so a thin frost rim is visible
Requires prior transparent letterbox on dust2_overview.png for open areas.
"""
from pathlib import Path
import backup_quickmatch as bak

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing:\n{old[:200]!r}")
    return text.replace(old, new, 1)


def main() -> None:
    bak.backup("before_map_frost_frame")
    text = TARGET.read_text(encoding="utf-8")
    if "特性觸發" not in text or "0.12f, 0.13f, 0.145f, 0.82f" not in text:
        raise RuntimeError("UTF-8 corrupted or MapFrame color missing")

    if "_mapFrameImage" not in text:
        text = must_replace(
            text,
            "        private RectTransform _mapFrame;\n"
            "        private RectTransform _mapRotator;\n",
            "        private RectTransform _mapFrame;\n"
            "        private Image _mapFrameImage;\n"
            "        private Image _mapFrameFace;\n"
            "        private RectTransform _mapRotator;\n",
        )

    # Apply frost to map frame when chrome is applied
    text = must_replace(
        text,
        "            if (_controlBarImage != null)\n"
        "                _controlBarImage.color = bar;\n\n"
        "            ApplyFrostedButtonChrome(_settingsButtonEdge, _settingsButtonFace, edge, face, highlight, pressed);\n",
        "            if (_controlBarImage != null)\n"
        "                _controlBarImage.color = bar;\n"
        "            if (_mapFrameImage != null)\n"
        "                _mapFrameImage.color = edge;\n"
        "            if (_mapFrameFace != null)\n"
        "                _mapFrameFace.color = face;\n\n"
        "            ApplyFrostedButtonChrome(_settingsButtonEdge, _settingsButtonFace, edge, face, highlight, pressed);\n",
    )

    # Build frosted map chrome instead of opaque dark MapFrame
    old_frame = (
        "            // Map slot (actual frame is fitted to sprite aspect to remove side letterbox)\n"
        '            Image mapFrame = CreateUiObject<Image>("MapFrame", canvas.transform);\n'
        "            _mapFrame = mapFrame.rectTransform;\n"
        "            mapFrame.color = new Color(0.12f, 0.13f, 0.145f, 0.82f);\n"
    )
    new_frame = (
        "            // Map slot — frosted chrome matching control buttons (transparent map letterbox shows through)\n"
        '            Image mapFrame = CreateUiObject<Image>("MapFrame", canvas.transform);\n'
        "            _mapFrame = mapFrame.rectTransform;\n"
        "            _mapFrameImage = mapFrame;\n"
        "            mapFrame.color = new Color(Panel.r, Panel.g, Panel.b, 0.55f);\n"
        "            mapFrame.raycastTarget = false;\n"
        "\n"
        '            Image mapFace = CreateUiObject<Image>("MapFace", mapFrame.transform);\n'
        "            Stretch(mapFace.rectTransform);\n"
        "            mapFace.rectTransform.offsetMin = new Vector2(3f, 3f);\n"
        "            mapFace.rectTransform.offsetMax = new Vector2(-3f, -3f);\n"
        "            mapFace.color = new Color(Panel.r, Panel.g, Panel.b, 0.28f);\n"
        "            mapFace.raycastTarget = false;\n"
        "            _mapFrameFace = mapFace;\n"
    )
    text = must_replace(text, old_frame, new_frame)

    # Parent rotator under MapFace so map sits inside frosted face
    text = must_replace(
        text,
        '            var rotatorGo = new GameObject("MapRotator", typeof(RectTransform));\n'
        "            rotatorGo.transform.SetParent(_mapFrame, false);\n"
        "            _mapRotator = rotatorGo.GetComponent<RectTransform>();\n"
        "            Stretch(_mapRotator);\n"
        "            _mapRotator.pivot = new Vector2(0.5f, 0.5f);\n",
        '            var rotatorGo = new GameObject("MapRotator", typeof(RectTransform));\n'
        "            rotatorGo.transform.SetParent(_mapFrameFace != null ? _mapFrameFace.transform : _mapFrame, false);\n"
        "            _mapRotator = rotatorGo.GetComponent<RectTransform>();\n"
        "            Stretch(_mapRotator);\n"
        "            _mapRotator.pivot = new Vector2(0.5f, 0.5f);\n",
    )

    TARGET.write_text(text, encoding="utf-8")
    verify = TARGET.read_text(encoding="utf-8")
    if "特性觸發" not in verify:
        raise RuntimeError("UTF-8 lost after write")
    if "_mapFrameFace" not in verify or "MapFace" not in verify:
        raise RuntimeError("map frost frame not applied")
    if "0.12f, 0.13f, 0.145f, 0.82f" in verify:
        raise RuntimeError("old opaque MapFrame color still present")
    print("patched map frosted frame OK")


if __name__ == "__main__":
    main()
