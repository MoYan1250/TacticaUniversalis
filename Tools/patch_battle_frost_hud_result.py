# -*- coding: utf-8 -*-
"""
Battle UI polish (UTF-8 only):
- Frosted glass on bottom control buttons + bar
- Larger ROUND clock / match meta
- Logos farther from scores without widening TopHud
- Larger official result logos / names / scores
"""
from pathlib import Path
import backup_quickmatch as bak

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing:\n{old[:180]!r}")
    return text.replace(old, new, 1)


def main() -> None:
    bak.backup("before_frost_hud_result")
    text = TARGET.read_text(encoding="utf-8")
    if "擊殺廣播" not in text:
        raise RuntimeError("UTF-8 corrupted")

    # --- fields ---
    if "_timeoutButtonEdge" not in text:
        text = must_replace(
            text,
            "        private RectTransform _controlBarRect;\n"
            "        private Image _settingsButtonEdge;\n"
            "        private Image _settingsButtonFace;\n"
            "        private RectTransform _settingsButtonRect;\n"
            "        private RectTransform _timeoutButtonRect;\n"
            "        private RectTransform _pauseButtonRect;\n"
            "        private RectTransform _speedButtonRect;\n"
            "        private RectTransform _skipButtonRect;\n",
            "        private RectTransform _controlBarRect;\n"
            "        private Image _controlBarImage;\n"
            "        private Image _settingsButtonEdge;\n"
            "        private Image _settingsButtonFace;\n"
            "        private RectTransform _settingsButtonRect;\n"
            "        private RectTransform _timeoutButtonRect;\n"
            "        private Image _timeoutButtonEdge;\n"
            "        private Image _timeoutButtonFace;\n"
            "        private RectTransform _pauseButtonRect;\n"
            "        private Image _pauseButtonEdge;\n"
            "        private Image _pauseButtonFace;\n"
            "        private RectTransform _speedButtonRect;\n"
            "        private Image _speedButtonEdge;\n"
            "        private Image _speedButtonFace;\n"
            "        private RectTransform _skipButtonRect;\n"
            "        private Image _skipButtonEdge;\n"
            "        private Image _skipButtonFace;\n",
        )

    # --- frost helpers ---
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

    # --- wire control bar + buttons ---
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

    # --- TopHud: larger clock/meta; logos farther from scores (same TopHud width) ---
    text = must_replace(
        text,
        "            _leftTeamLogo = CreateTeamLogo(\"LeftLogo\", topHud.transform, 0.20f);\n"
        "            _leftScoreText = CreateText(\"LeftScore\", topHud.transform, 42, TextAnchor.MiddleRight, TextPrimary);\n"
        "            SetRect(_leftScoreText.rectTransform, new Vector2(0.27f, 0.28f), new Vector2(0.37f, 0.95f));",
        "            _leftTeamLogo = CreateTeamLogo(\"LeftLogo\", topHud.transform, 0.11f);\n"
        "            _leftScoreText = CreateText(\"LeftScore\", topHud.transform, 42, TextAnchor.MiddleRight, TextPrimary);\n"
        "            SetRect(_leftScoreText.rectTransform, new Vector2(0.27f, 0.28f), new Vector2(0.37f, 0.95f));",
    )
    text = must_replace(
        text,
        "            _rightTeamLogo = CreateTeamLogo(\"RightLogo\", topHud.transform, 0.80f);",
        "            _rightTeamLogo = CreateTeamLogo(\"RightLogo\", topHud.transform, 0.89f);",
    )

    text = must_replace(
        text,
        "            _clockText = CreateText(\"Clock\", topHud.transform, 15, TextAnchor.MiddleCenter, TextPrimary);\n"
        "            SetRect(_clockText.rectTransform, new Vector2(0.37f, 0.38f), new Vector2(0.63f, 0.64f));\n"
        "            _clockText.horizontalOverflow = HorizontalWrapMode.Overflow;\n"
        "            _clockText.verticalOverflow = VerticalWrapMode.Truncate;\n"
        "            _clockText.raycastTarget = false;\n"
        "            _clockText.text = \"1:55\";\n"
        "\n"
        "            _careerMatchMetaText = CreateText(\"CareerMeta\", topHud.transform, 10, TextAnchor.MiddleCenter, TextMuted);\n"
        "            SetRect(_careerMatchMetaText.rectTransform, new Vector2(0.30f, 0.04f), new Vector2(0.70f, 0.34f));\n"
        "            _careerMatchMetaText.horizontalOverflow = HorizontalWrapMode.Overflow;\n"
        "            _careerMatchMetaText.verticalOverflow = VerticalWrapMode.Truncate;\n"
        "            _careerMatchMetaText.resizeTextForBestFit = true;\n"
        "            _careerMatchMetaText.resizeTextMinSize = 8;\n"
        "            _careerMatchMetaText.resizeTextMaxSize = 10;\n",
        "            _clockText = CreateText(\"Clock\", topHud.transform, 20, TextAnchor.MiddleCenter, TextPrimary);\n"
        "            SetRect(_clockText.rectTransform, new Vector2(0.37f, 0.36f), new Vector2(0.63f, 0.64f));\n"
        "            _clockText.horizontalOverflow = HorizontalWrapMode.Overflow;\n"
        "            _clockText.verticalOverflow = VerticalWrapMode.Truncate;\n"
        "            _clockText.fontStyle = FontStyle.Bold;\n"
        "            _clockText.raycastTarget = false;\n"
        "            _clockText.text = \"1:55\";\n"
        "\n"
        "            _careerMatchMetaText = CreateText(\"CareerMeta\", topHud.transform, 13, TextAnchor.MiddleCenter, TextMuted);\n"
        "            SetRect(_careerMatchMetaText.rectTransform, new Vector2(0.28f, 0.02f), new Vector2(0.72f, 0.34f));\n"
        "            _careerMatchMetaText.horizontalOverflow = HorizontalWrapMode.Overflow;\n"
        "            _careerMatchMetaText.verticalOverflow = VerticalWrapMode.Truncate;\n"
        "            _careerMatchMetaText.resizeTextForBestFit = true;\n"
        "            _careerMatchMetaText.resizeTextMinSize = 11;\n"
        "            _careerMatchMetaText.resizeTextMaxSize = 14;\n",
    )

    # --- Official result overlay: larger logos / names / scores (panel width unchanged) ---
    text = must_replace(
        text,
        "            SetRect(panel.rectTransform, new Vector2(0.30f, 0.34f), new Vector2(0.70f, 0.66f));\n"
        "            panel.color = new Color(Panel.r, Panel.g, Panel.b, 0.94f);\n"
        "\n"
        "            Text title = CreateText(\"Title\", panel.transform, 18, TextAnchor.MiddleCenter, Accent, _uiFont700);\n"
        "            SetRect(title.rectTransform, new Vector2(0.08f, 0.82f), new Vector2(0.92f, 0.96f));\n"
        "            title.text = \"比賽結束\";\n"
        "            title.fontStyle = FontStyle.Bold;\n"
        "\n"
        "            _officialResultLeftLogo = CreateResultTeamLogo(\"LeftLogo\", panel.transform, 0.16f);\n"
        "            _officialResultRightLogo = CreateResultTeamLogo(\"RightLogo\", panel.transform, 0.84f);\n"
        "\n"
        "            _officialResultLeftName = CreateText(\"LeftName\", panel.transform, 12, TextAnchor.MiddleCenter, TextMuted);\n"
        "            SetRect(_officialResultLeftName.rectTransform, new Vector2(0.06f, 0.38f), new Vector2(0.26f, 0.48f));\n"
        "\n"
        "            _officialResultRightName = CreateText(\"RightName\", panel.transform, 12, TextAnchor.MiddleCenter, TextMuted);\n"
        "            SetRect(_officialResultRightName.rectTransform, new Vector2(0.74f, 0.38f), new Vector2(0.94f, 0.48f));\n"
        "\n"
        "            _officialResultLeftScore = CreateText(\"LeftScore\", panel.transform, 36, TextAnchor.MiddleRight, TextPrimary, _uiFont700);\n"
        "            SetRect(_officialResultLeftScore.rectTransform, new Vector2(0.26f, 0.50f), new Vector2(0.42f, 0.74f));\n"
        "            _officialResultLeftScore.fontStyle = FontStyle.Bold;\n"
        "\n"
        "            Text colon = CreateText(\"Colon\", panel.transform, 28, TextAnchor.MiddleCenter, TextMuted);\n"
        "            SetRect(colon.rectTransform, new Vector2(0.44f, 0.50f), new Vector2(0.56f, 0.74f));\n"
        "            colon.text = \":\";\n"
        "            colon.fontStyle = FontStyle.Bold;\n"
        "\n"
        "            _officialResultRightScore = CreateText(\"RightScore\", panel.transform, 36, TextAnchor.MiddleLeft, TextPrimary, _uiFont700);\n"
        "            SetRect(_officialResultRightScore.rectTransform, new Vector2(0.58f, 0.50f), new Vector2(0.74f, 0.74f));\n"
        "            _officialResultRightScore.fontStyle = FontStyle.Bold;\n"
        "\n"
        "            _officialResultOutcome = CreateText(\"Outcome\", panel.transform, 22, TextAnchor.MiddleCenter, OfficialWinUi, _uiFont700);\n"
        "            SetRect(_officialResultOutcome.rectTransform, new Vector2(0.28f, 0.28f), new Vector2(0.72f, 0.42f));\n"
        "            _officialResultOutcome.fontStyle = FontStyle.Bold;\n",
        "            SetRect(panel.rectTransform, new Vector2(0.30f, 0.30f), new Vector2(0.70f, 0.70f));\n"
        "            panel.color = new Color(Panel.r, Panel.g, Panel.b, 0.94f);\n"
        "\n"
        "            Text title = CreateText(\"Title\", panel.transform, 22, TextAnchor.MiddleCenter, Accent, _uiFont700);\n"
        "            SetRect(title.rectTransform, new Vector2(0.08f, 0.86f), new Vector2(0.92f, 0.97f));\n"
        "            title.text = \"比賽結束\";\n"
        "            title.fontStyle = FontStyle.Bold;\n"
        "\n"
        "            _officialResultLeftLogo = CreateResultTeamLogo(\"LeftLogo\", panel.transform, 0.13f);\n"
        "            _officialResultRightLogo = CreateResultTeamLogo(\"RightLogo\", panel.transform, 0.87f);\n"
        "\n"
        "            _officialResultLeftName = CreateText(\"LeftName\", panel.transform, 16, TextAnchor.MiddleCenter, TextMuted);\n"
        "            SetRect(_officialResultLeftName.rectTransform, new Vector2(0.04f, 0.34f), new Vector2(0.28f, 0.46f));\n"
        "\n"
        "            _officialResultRightName = CreateText(\"RightName\", panel.transform, 16, TextAnchor.MiddleCenter, TextMuted);\n"
        "            SetRect(_officialResultRightName.rectTransform, new Vector2(0.72f, 0.34f), new Vector2(0.96f, 0.46f));\n"
        "\n"
        "            _officialResultLeftScore = CreateText(\"LeftScore\", panel.transform, 48, TextAnchor.MiddleRight, TextPrimary, _uiFont700);\n"
        "            SetRect(_officialResultLeftScore.rectTransform, new Vector2(0.28f, 0.48f), new Vector2(0.44f, 0.78f));\n"
        "            _officialResultLeftScore.fontStyle = FontStyle.Bold;\n"
        "\n"
        "            Text colon = CreateText(\"Colon\", panel.transform, 36, TextAnchor.MiddleCenter, TextMuted);\n"
        "            SetRect(colon.rectTransform, new Vector2(0.44f, 0.48f), new Vector2(0.56f, 0.78f));\n"
        "            colon.text = \":\";\n"
        "            colon.fontStyle = FontStyle.Bold;\n"
        "\n"
        "            _officialResultRightScore = CreateText(\"RightScore\", panel.transform, 48, TextAnchor.MiddleLeft, TextPrimary, _uiFont700);\n"
        "            SetRect(_officialResultRightScore.rectTransform, new Vector2(0.56f, 0.48f), new Vector2(0.72f, 0.78f));\n"
        "            _officialResultRightScore.fontStyle = FontStyle.Bold;\n"
        "\n"
        "            _officialResultOutcome = CreateText(\"Outcome\", panel.transform, 26, TextAnchor.MiddleCenter, OfficialWinUi, _uiFont700);\n"
        "            SetRect(_officialResultOutcome.rectTransform, new Vector2(0.28f, 0.24f), new Vector2(0.72f, 0.40f));\n"
        "            _officialResultOutcome.fontStyle = FontStyle.Bold;\n",
    )

    text = must_replace(
        text,
        "            // Same pattern as TopHud logos: fixed X, height-driven square beside the score.\n"
        "            rt.anchorMin = new Vector2(anchorX, 0.48f);\n"
        "            rt.anchorMax = new Vector2(anchorX, 0.78f);\n",
        "            // Same pattern as TopHud logos: fixed X, height-driven square beside the score.\n"
        "            rt.anchorMin = new Vector2(anchorX, 0.44f);\n"
        "            rt.anchorMax = new Vector2(anchorX, 0.82f);\n",
    )

    if "擊殺廣播" not in text:
        raise RuntimeError("UTF-8 corrupted after patch")
    for needle in (
        "ApplyFrostedButtonChrome",
        "CaptureButtonChrome(callTimeout",
        "CreateTeamLogo(\"LeftLogo\", topHud.transform, 0.11f)",
        "CreateText(\"Clock\", topHud.transform, 20,",
        "CreateText(\"LeftScore\", panel.transform, 48,",
        "CreateResultTeamLogo(\"LeftLogo\", panel.transform, 0.13f)",
    ):
        if needle not in text:
            raise RuntimeError(f"missing after patch: {needle}")

    TARGET.write_text(text, encoding="utf-8", newline="\n")
    print("patched", TARGET)


if __name__ == "__main__":
    main()
