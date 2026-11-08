# -*- coding: utf-8 -*-
"""Center battle control buttons; keep settings original; frosted chrome on controls."""
from pathlib import Path

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing anchor:\n{old[:160]!r}")
    return text.replace(old, new, 1)


def main() -> None:
    text = TARGET.read_text(encoding="utf-8")
    if "擊殺廣播" not in text:
        raise RuntimeError("UTF-8 corrupted")

    # --- fields for frost refs ---
    if "_timeoutButtonEdge" not in text:
        text = must_replace(
            text,
            "        private RectTransform _timeoutButtonRect;\n"
            "        private RectTransform _pauseButtonRect;\n"
            "        private RectTransform _speedButtonRect;\n"
            "        private RectTransform _skipButtonRect;\n",
            "        private RectTransform _timeoutButtonRect;\n"
            "        private RectTransform _pauseButtonRect;\n"
            "        private RectTransform _speedButtonRect;\n"
            "        private RectTransform _skipButtonRect;\n"
            "        private Image _controlBarImage;\n"
            "        private Image _timeoutButtonEdge;\n"
            "        private Image _timeoutButtonFace;\n"
            "        private Image _pauseButtonEdge;\n"
            "        private Image _pauseButtonFace;\n"
            "        private Image _speedButtonEdge;\n"
            "        private Image _speedButtonFace;\n"
            "        private Image _skipButtonEdge;\n"
            "        private Image _skipButtonFace;\n",
        )

    # --- layout: centered 4 buttons; settings stay bottom-right ---
    old_layout = """        private void ApplyControlBarLayout(bool practiceLayout)
        {
            if (_controlBarRect != null)
            {
                if (practiceLayout)
                    SetRect(_controlBarRect, new Vector2(0.12f, 0.015f), new Vector2(0.88f, 0.100f));
                else
                    SetRect(_controlBarRect, new Vector2(0.34f, 0.028f), new Vector2(0.66f, 0.058f));
            }

            if (_settingsButtonRect != null)
            {
                if (practiceLayout)
                    SetRect(_settingsButtonRect, new Vector2(0.905f, 0.015f), new Vector2(0.985f, 0.100f));
                else
                    SetRect(_settingsButtonRect, new Vector2(0.664f, 0.028f), new Vector2(0.702f, 0.058f));
            }

            if (practiceLayout)
            {
                if (_matchText != null)
                    SetRect(_matchText.rectTransform, new Vector2(0.43f, 0.15f), new Vector2(0.495f, 0.85f));
                if (_timeoutButtonRect != null)
                    SetRect(_timeoutButtonRect, new Vector2(0.50f, 0.15f), new Vector2(0.64f, 0.85f));
                if (_pauseButtonRect != null)
                    SetRect(_pauseButtonRect, new Vector2(0.65f, 0.15f), new Vector2(0.715f, 0.85f));
                if (_speedButtonRect != null)
                    SetRect(_speedButtonRect, new Vector2(0.72f, 0.15f), new Vector2(0.785f, 0.85f));
                if (_skipButtonRect != null)
                    SetRect(_skipButtonRect, new Vector2(0.79f, 0.15f), new Vector2(0.855f, 0.85f));
                return;
            }

            if (_timeoutButtonRect != null)
                SetRect(_timeoutButtonRect, new Vector2(0.595f, 0.22f), new Vector2(0.688f, 0.78f));
            if (_pauseButtonRect != null)
                SetRect(_pauseButtonRect, new Vector2(0.694f, 0.22f), new Vector2(0.742f, 0.78f));
            if (_speedButtonRect != null)
                SetRect(_speedButtonRect, new Vector2(0.748f, 0.22f), new Vector2(0.796f, 0.78f));
            if (_skipButtonRect != null)
                SetRect(_skipButtonRect, new Vector2(0.802f, 0.22f), new Vector2(0.850f, 0.78f));
        }"""

    # Also accept alternate official rects from earlier patches
    if old_layout not in text:
        # try looser: find method start to RefreshOfficialMatchMeta or ApplySettings
        start = text.find("        private void ApplyControlBarLayout(bool practiceLayout)")
        end = text.find("        private void RefreshOfficialMatchMeta()", start)
        if start < 0 or end < 0:
            end = text.find("        private void ApplySettingsFrostedChrome(bool enabled)", start)
        if start < 0 or end < 0:
            raise RuntimeError("cannot locate ApplyControlBarLayout block")
        old_layout = text[start:end]

    new_layout = """        private void ApplyControlBarLayout(bool practiceLayout)
        {
            // Settings always stay bottom-right (original slot).
            if (_settingsButtonRect != null)
                SetRect(_settingsButtonRect, new Vector2(0.905f, 0.015f), new Vector2(0.985f, 0.100f));

            if (_controlBarRect != null)
            {
                if (practiceLayout)
                    SetRect(_controlBarRect, new Vector2(0.12f, 0.015f), new Vector2(0.88f, 0.100f));
                else
                    // Centered strip for: 叫暫停 | 暫停 | 倍速 | 跳過
                    SetRect(_controlBarRect, new Vector2(0.30f, 0.018f), new Vector2(0.70f, 0.092f));
            }

            if (practiceLayout)
            {
                if (_matchText != null)
                    SetRect(_matchText.rectTransform, new Vector2(0.43f, 0.15f), new Vector2(0.495f, 0.85f));
                if (_timeoutButtonRect != null)
                    SetRect(_timeoutButtonRect, new Vector2(0.50f, 0.15f), new Vector2(0.64f, 0.85f));
                if (_pauseButtonRect != null)
                    SetRect(_pauseButtonRect, new Vector2(0.65f, 0.15f), new Vector2(0.715f, 0.85f));
                if (_speedButtonRect != null)
                    SetRect(_speedButtonRect, new Vector2(0.72f, 0.15f), new Vector2(0.785f, 0.85f));
                if (_skipButtonRect != null)
                    SetRect(_skipButtonRect, new Vector2(0.79f, 0.15f), new Vector2(0.855f, 0.85f));
                return;
            }

            // Equal slots L→R inside centered bar.
            if (_timeoutButtonRect != null)
                SetRect(_timeoutButtonRect, new Vector2(0.03f, 0.14f), new Vector2(0.27f, 0.86f));
            if (_pauseButtonRect != null)
                SetRect(_pauseButtonRect, new Vector2(0.29f, 0.14f), new Vector2(0.48f, 0.86f));
            if (_speedButtonRect != null)
                SetRect(_speedButtonRect, new Vector2(0.50f, 0.14f), new Vector2(0.69f, 0.86f));
            if (_skipButtonRect != null)
                SetRect(_skipButtonRect, new Vector2(0.71f, 0.14f), new Vector2(0.97f, 0.86f));
        }

"""

    text = text.replace(old_layout, new_layout, 1)

    # --- expand frost helper ---
    old_frost = """        private void ApplySettingsFrostedChrome(bool enabled)
        {
            if (!enabled)
                return;
            Color edge = new Color(Panel.r, Panel.g, Panel.b, 0.72f);
            Color face = new Color(Panel.r, Panel.g, Panel.b, 0.42f);
            if (_settingsButtonEdge != null)
                _settingsButtonEdge.color = edge;
            if (_settingsButtonFace != null)
                _settingsButtonFace.color = face;
        }"""

    new_frost = """        private void ApplySettingsFrostedChrome(bool enabled)
        {
            if (!enabled)
                return;

            Color bar = new Color(Panel.r, Panel.g, Panel.b, 0.38f);
            Color edge = new Color(Panel.r, Panel.g, Panel.b, 0.55f);
            Color face = new Color(Panel.r, Panel.g, Panel.b, 0.28f);
            Color highlight = new Color(0.55f, 0.58f, 0.64f, 0.42f);
            Color pressed = new Color(0.22f, 0.24f, 0.28f, 0.55f);

            if (_controlBarImage != null)
                _controlBarImage.color = bar;

            ApplyFrostedButtonChrome(_settingsButtonEdge, _settingsButtonFace, edge, face, highlight, pressed);
            ApplyFrostedButtonChrome(_timeoutButtonEdge, _timeoutButtonFace, edge, face, highlight, pressed);
            ApplyFrostedButtonChrome(_pauseButtonEdge, _pauseButtonFace, edge, face, highlight, pressed);
            ApplyFrostedButtonChrome(_speedButtonEdge, _speedButtonFace, edge, face, highlight, pressed);
            ApplyFrostedButtonChrome(_skipButtonEdge, _skipButtonFace, edge, face, highlight, pressed);
        }

        private static void ApplyFrostedButtonChrome(
            Image edgeImage, Image faceImage, Color edge, Color face, Color highlight, Color pressed)
        {
            if (edgeImage != null)
                edgeImage.color = edge;
            if (faceImage != null)
                faceImage.color = face;

            Button button = edgeImage != null
                ? edgeImage.GetComponent<Button>()
                : faceImage != null ? faceImage.GetComponentInParent<Button>() : null;
            if (button == null)
                return;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = highlight;
            colors.pressedColor = pressed;
            colors.selectedColor = highlight;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.35f);
            button.colors = colors;
        }

        private static void CaptureButtonChrome(Button button, out Image edge, out Image face)
        {
            edge = button != null ? button.GetComponent<Image>() : null;
            face = null;
            if (button == null)
                return;
            Transform faceTransform = button.transform.Find("Face");
            if (faceTransform != null)
                face = faceTransform.GetComponent<Image>();
        }"""

    if "ApplyFrostedButtonChrome" not in text:
        text = must_replace(text, old_frost, new_frost)

    # --- wire chrome refs at creation ---
    if "_controlBarImage = controlBar" not in text:
        text = must_replace(
            text,
            "            controlBar.color = new Color(Panel.r, Panel.g, Panel.b, 0.78f);\n"
            "            _controlBarRect = controlBar.rectTransform;\n",
            "            controlBar.color = new Color(Panel.r, Panel.g, Panel.b, 0.38f);\n"
            "            _controlBarRect = controlBar.rectTransform;\n"
            "            _controlBarImage = controlBar;\n",
        )

    if "CaptureButtonChrome(callTimeout" not in text:
        text = must_replace(
            text,
            "            _timeoutButtonRect = callTimeout.GetComponent<RectTransform>();\n"
            "            _timeoutCallLabel = callTimeout.GetComponentInChildren<Text>();\n",
            "            _timeoutButtonRect = callTimeout.GetComponent<RectTransform>();\n"
            "            CaptureButtonChrome(callTimeout, out _timeoutButtonEdge, out _timeoutButtonFace);\n"
            "            _timeoutCallLabel = callTimeout.GetComponentInChildren<Text>();\n",
        )

    if "CaptureButtonChrome(pauseBtn" not in text:
        text = must_replace(
            text,
            "            _pauseButtonRect = pauseBtn.GetComponent<RectTransform>();\n"
            "            RefreshPauseIcon();\n",
            "            _pauseButtonRect = pauseBtn.GetComponent<RectTransform>();\n"
            "            CaptureButtonChrome(pauseBtn, out _pauseButtonEdge, out _pauseButtonFace);\n"
            "            RefreshPauseIcon();\n",
        )

    if "CaptureButtonChrome(speed" not in text:
        text = must_replace(
            text,
            "            _speedButtonRect = speed.GetComponent<RectTransform>();\n"
            "            _speedText = speed.GetComponentInChildren<Text>();\n",
            "            _speedButtonRect = speed.GetComponent<RectTransform>();\n"
            "            CaptureButtonChrome(speed, out _speedButtonEdge, out _speedButtonFace);\n"
            "            _speedText = speed.GetComponentInChildren<Text>();\n",
        )

    if "CaptureButtonChrome(skipBtn" not in text:
        text = must_replace(
            text,
            "            _skipButtonRect = skipBtn.GetComponent<RectTransform>();\n",
            "            _skipButtonRect = skipBtn.GetComponent<RectTransform>();\n"
            "            CaptureButtonChrome(skipBtn, out _skipButtonEdge, out _skipButtonFace);\n",
        )

    # settings already has edge/face; ensure frost applied after build
    if "ApplySettingsFrostedChrome(true);" not in text.split("CreateZoneLabels()")[0][-400:]:
        # after settings button wire
        if "_settingsButtonFace = settingsFaceTransform.GetComponent<Image>();" in text:
            text = must_replace(
                text,
                "                _settingsButtonFace = settingsFaceTransform.GetComponent<Image>();\n"
                "            ApplySettingsFrostedChrome(true);\n",
                "                _settingsButtonFace = settingsFaceTransform.GetComponent<Image>();\n"
                "            ApplySettingsFrostedChrome(true);\n",
            )
        elif "_settingsButtonRect = settingsBtn.GetComponent<RectTransform>();" in text and \
                "CaptureButtonChrome(settingsBtn" not in text:
            # ensure settings chrome captured if only rect wired
            if "_settingsButtonEdge = settingsBtn.GetComponent<Image>();" not in text:
                text = must_replace(
                    text,
                    "            _settingsButtonRect = settingsBtn.GetComponent<RectTransform>();\n",
                    "            _settingsButtonRect = settingsBtn.GetComponent<RectTransform>();\n"
                    "            CaptureButtonChrome(settingsBtn, out _settingsButtonEdge, out _settingsButtonFace);\n"
                    "            ApplySettingsFrostedChrome(true);\n",
                )

    for needle in (
        "ApplyFrostedButtonChrome",
        "CaptureButtonChrome(callTimeout",
        "SetRect(_controlBarRect, new Vector2(0.30f, 0.018f), new Vector2(0.70f, 0.092f))",
        "SetRect(_settingsButtonRect, new Vector2(0.905f, 0.015f), new Vector2(0.985f, 0.100f))",
        "擊殺廣播",
    ):
        if needle not in text:
            raise RuntimeError(f"incomplete: {needle}")

    TARGET.write_text(text, encoding="utf-8", newline="\n")
    print("patched", TARGET)


if __name__ == "__main__":
    main()
