# UTF-8-safe: official match compact bottom bar + default speed 1x
from pathlib import Path

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str, count: int = 1) -> str:
    if old not in text:
        raise RuntimeError(f"patch anchor missing: {old[:80]!r}")
    return text.replace(old, new, count)


def main() -> None:
    text = TARGET.read_text(encoding="utf-8")

    text = must_replace(
        text,
        "        [SerializeField] private float playbackSpeed = 16f;",
        "        [SerializeField] private float playbackSpeed = 1f;",
    )

    if "_controlBarRect" not in text:
        text = must_replace(
            text,
            "        private Text _careerBarInfoText;\n        private GameObject _settingsRoot;",
            "        private Text _careerBarInfoText;\n"
            "        private RectTransform _controlBarRect;\n"
            "        private RectTransform _settingsButtonRect;\n"
            "        private RectTransform _timeoutButtonRect;\n"
            "        private RectTransform _pauseButtonRect;\n"
            "        private RectTransform _speedButtonRect;\n"
            "        private RectTransform _skipButtonRect;\n"
            "        private GameObject _settingsRoot;",
        )

    if "ApplyControlBarLayout" not in text:
        text = must_replace(
            text,
            "            if (_careerBarInfoText != null)\n"
            "            {\n"
            "                _careerBarInfoText.gameObject.SetActive(!simulation);\n"
            "                if (!simulation)\n"
            "                    RefreshOfficialControlBarInfo();\n"
            "            }\n"
            "        }\n\n"
            "        private void RefreshOfficialControlBarInfo()",
            "            if (_careerBarInfoText != null)\n"
            "            {\n"
            "                _careerBarInfoText.gameObject.SetActive(!simulation);\n"
            "                if (!simulation)\n"
            "                    RefreshOfficialControlBarInfo();\n"
            "            }\n\n"
            "            ApplyControlBarLayout(simulation);\n"
            "        }\n\n"
            "        private void ApplyControlBarLayout(bool practiceLayout)\n"
            "        {\n"
            "            if (_controlBarRect != null)\n"
            "            {\n"
            "                if (practiceLayout)\n"
            "                    SetRect(_controlBarRect, new Vector2(0.12f, 0.015f), new Vector2(0.88f, 0.100f));\n"
            "                else\n"
            "                    SetRect(_controlBarRect, new Vector2(0.14f, 0.020f), new Vector2(0.88f, 0.082f));\n"
            "            }\n\n"
            "            if (_settingsButtonRect != null)\n"
            "            {\n"
            "                if (practiceLayout)\n"
            "                    SetRect(_settingsButtonRect, new Vector2(0.905f, 0.015f), new Vector2(0.985f, 0.100f));\n"
            "                else\n"
            "                    SetRect(_settingsButtonRect, new Vector2(0.905f, 0.020f), new Vector2(0.985f, 0.082f));\n"
            "            }\n\n"
            "            if (practiceLayout)\n"
            "            {\n"
            "                if (_careerBarInfoText != null)\n"
            "                    SetRect(_careerBarInfoText.rectTransform, new Vector2(0.02f, 0.12f), new Vector2(0.42f, 0.88f));\n"
            "                if (_matchText != null)\n"
            "                    SetRect(_matchText.rectTransform, new Vector2(0.43f, 0.15f), new Vector2(0.495f, 0.85f));\n"
            "                if (_timeoutButtonRect != null)\n"
            "                    SetRect(_timeoutButtonRect, new Vector2(0.50f, 0.15f), new Vector2(0.64f, 0.85f));\n"
            "                if (_pauseButtonRect != null)\n"
            "                    SetRect(_pauseButtonRect, new Vector2(0.65f, 0.15f), new Vector2(0.715f, 0.85f));\n"
            "                if (_speedButtonRect != null)\n"
            "                    SetRect(_speedButtonRect, new Vector2(0.72f, 0.15f), new Vector2(0.785f, 0.85f));\n"
            "                if (_skipButtonRect != null)\n"
            "                    SetRect(_skipButtonRect, new Vector2(0.79f, 0.15f), new Vector2(0.855f, 0.85f));\n"
            "                return;\n"
            "            }\n\n"
            "            if (_careerBarInfoText != null)\n"
            "                SetRect(_careerBarInfoText.rectTransform, new Vector2(0.02f, 0.14f), new Vector2(0.48f, 0.86f));\n"
            "            if (_matchText != null)\n"
            "                SetRect(_matchText.rectTransform, new Vector2(0.485f, 0.14f), new Vector2(0.58f, 0.86f));\n"
            "            if (_timeoutButtonRect != null)\n"
            "                SetRect(_timeoutButtonRect, new Vector2(0.585f, 0.20f), new Vector2(0.695f, 0.80f));\n"
            "            if (_pauseButtonRect != null)\n"
            "                SetRect(_pauseButtonRect, new Vector2(0.702f, 0.20f), new Vector2(0.755f, 0.80f));\n"
            "            if (_speedButtonRect != null)\n"
            "                SetRect(_speedButtonRect, new Vector2(0.762f, 0.20f), new Vector2(0.815f, 0.80f));\n"
            "            if (_skipButtonRect != null)\n"
            "                SetRect(_skipButtonRect, new Vector2(0.822f, 0.20f), new Vector2(0.875f, 0.80f));\n"
            "        }\n\n"
            "        private void RefreshOfficialControlBarInfo()",
        )

    if "playbackSpeed = 1f;" not in text.split("BuildPlayerNumbers();")[0]:
        text = must_replace(
            text,
            "            BuildPlayerNumbers();\n            BuildInterface();",
            "            playbackSpeed = 1f;\n            BuildPlayerNumbers();\n            BuildInterface();",
        )

    if "_controlBarRect = controlBar.rectTransform" not in text:
        text = must_replace(
            text,
            "            controlBar.color = new Color(Panel.r, Panel.g, Panel.b, 0.78f);\n\n"
            "            Text title = CreateText(\"Title\", controlBar.transform, 14, TextAnchor.MiddleLeft, TextMuted);",
            "            controlBar.color = new Color(Panel.r, Panel.g, Panel.b, 0.78f);\n"
            "            _controlBarRect = controlBar.rectTransform;\n\n"
            "            Text title = CreateText(\"Title\", controlBar.transform, 14, TextAnchor.MiddleLeft, TextMuted);",
        )

    text = must_replace(
        text,
        '            Button callTimeout = CreateButton("CallTimeout", "叫暫停", controlBar.transform,\n'
        "                new Vector2(0.50f, 0.15f), new Vector2(0.64f, 0.85f), OnRequestTimeoutClicked);",
        '            Button callTimeout = CreateButton("CallTimeout", "叫暫停", controlBar.transform,\n'
        "                new Vector2(0.50f, 0.15f), new Vector2(0.64f, 0.85f), OnRequestTimeoutClicked);\n"
        "            _timeoutButtonRect = callTimeout.GetComponent<RectTransform>();",
    )

    text = must_replace(
        text,
        '            CreateIconButton("Pause", controlBar.transform,\n'
        "                new Vector2(0.65f, 0.15f), new Vector2(0.715f, 0.85f),\n"
        "                _playbackPauseSprite, TogglePause, out _pauseIcon);",
        '            Button pauseBtn = CreateIconButton("Pause", controlBar.transform,\n'
        "                new Vector2(0.65f, 0.15f), new Vector2(0.715f, 0.85f),\n"
        "                _playbackPauseSprite, TogglePause, out _pauseIcon);\n"
        "            _pauseButtonRect = pauseBtn.GetComponent<RectTransform>();",
    )

    text = must_replace(
        text,
        '            Button speed = CreateButton("Speed", "16×", controlBar.transform,\n'
        "                new Vector2(0.72f, 0.15f), new Vector2(0.785f, 0.85f), CycleSpeed);\n"
        "            _speedText = speed.GetComponentInChildren<Text>();",
        '            Button speed = CreateButton("Speed", "1×", controlBar.transform,\n'
        "                new Vector2(0.72f, 0.15f), new Vector2(0.785f, 0.85f), CycleSpeed);\n"
        "            _speedButtonRect = speed.GetComponent<RectTransform>();\n"
        "            _speedText = speed.GetComponentInChildren<Text>();\n"
        "            if (_speedText != null)\n"
        "                _speedText.text = $\"{playbackSpeed:0}×\";",
    )

    text = must_replace(
        text,
        '            CreateIconButton("Skip", controlBar.transform,\n'
        "                new Vector2(0.79f, 0.15f), new Vector2(0.855f, 0.85f),\n"
        "                _playbackSkipSprite, Skip, out _);",
        '            Button skipBtn = CreateIconButton("Skip", controlBar.transform,\n'
        "                new Vector2(0.79f, 0.15f), new Vector2(0.855f, 0.85f),\n"
        "                _playbackSkipSprite, Skip, out _);\n"
        "            _skipButtonRect = skipBtn.GetComponent<RectTransform>();",
    )

    text = must_replace(
        text,
        "            CreateIconButton(\n"
        "                \"Settings\",\n"
        "                canvas.transform,\n"
        "                new Vector2(0.905f, 0.015f),\n"
        "                new Vector2(0.985f, 0.100f),\n"
        "                settingsSprite,\n"
        "                ToggleSettingsPanel,\n"
        "                out _);",
        "            Button settingsBtn = CreateIconButton(\n"
        "                \"Settings\",\n"
        "                canvas.transform,\n"
        "                new Vector2(0.905f, 0.015f),\n"
        "                new Vector2(0.985f, 0.100f),\n"
        "                settingsSprite,\n"
        "                ToggleSettingsPanel,\n"
        "                out _);\n"
        "            _settingsButtonRect = settingsBtn.GetComponent<RectTransform>();",
    )

    if "ApplyControlBarLayout" not in text or "playbackSpeed = 1f;" not in text:
        raise RuntimeError("patch incomplete")

    TARGET.write_text(text, encoding="utf-8", newline="\n")
    print("patched", TARGET)


if __name__ == "__main__":
    main()
