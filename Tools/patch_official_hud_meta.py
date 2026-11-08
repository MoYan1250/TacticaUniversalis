# UTF-8-safe: official meta under clock, compact bottom bar, frosted settings
from pathlib import Path

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing anchor: {old[:120]!r}")
    return text.replace(old, new, 1)


def main() -> None:
    text = TARGET.read_text(encoding="utf-8")

    if "_careerMatchMetaText" not in text:
        text = must_replace(
            text,
            "        private Text _careerBarInfoText;\n        private RectTransform _controlBarRect;",
            "        private Text _careerMatchMetaText;\n"
            "        private RectTransform _controlBarRect;\n"
            "        private Image _settingsButtonEdge;\n"
            "        private Image _settingsButtonFace;",
        )

    if "RefreshOfficialMatchMeta" not in text:
        text = must_replace(
            text,
            "            if (_careerBarInfoText != null)\n"
            "            {\n"
            "                _careerBarInfoText.gameObject.SetActive(!simulation);\n"
            "                if (!simulation)\n"
            "                    RefreshOfficialControlBarInfo();\n"
            "            }\n\n"
            "            ApplyControlBarLayout(simulation);",
            "            if (_careerMatchMetaText != null)\n"
            "            {\n"
            "                _careerMatchMetaText.gameObject.SetActive(!simulation);\n"
            "                if (!simulation)\n"
            "                    RefreshOfficialMatchMeta();\n"
            "            }\n\n"
            "            ApplyControlBarLayout(simulation);\n"
            "            ApplySettingsFrostedChrome(true);",
        )

    if "private void RefreshOfficialMatchMeta()" not in text:
        text = must_replace(
            text,
            "        private void RefreshOfficialControlBarInfo()\n"
            "        {\n"
            "            if (_careerBarInfoText == null)\n"
            "                return;\n"
            "            string stage = string.IsNullOrEmpty(_careerBracketStageLabel)\n"
            "                ? string.Empty\n"
            "                : $\" · {_careerBracketStageLabel}\";\n"
            "            string competition = string.IsNullOrEmpty(_careerCompetitionTitle)\n"
            "                ? \"正式賽\"\n"
            "                : _careerCompetitionTitle;\n"
            "            string mapName = string.IsNullOrEmpty(_careerMapDisplayName) ? \"Dust II\" : _careerMapDisplayName;\n"
            "            string opponent = DemoZh.TeamName(_opponentTeamId);\n"
            "            _careerBarInfoText.text = $\"正式賽 · {mapName} · vs {opponent} · {competition}{stage}\";\n"
            "        }",
            "        private void RefreshOfficialMatchMeta()\n"
            "        {\n"
            "            if (_careerMatchMetaText == null)\n"
            "                return;\n"
            "            string mapName = string.IsNullOrEmpty(_careerMapDisplayName) ? \"Dust II\" : _careerMapDisplayName;\n"
            "            string competition = _careerCompetitionTitle ?? string.Empty;\n"
            "            string stage = _careerBracketStageLabel ?? string.Empty;\n"
            "            var parts = new List<string>();\n"
            "            if (!string.IsNullOrEmpty(mapName))\n"
            "                parts.Add(mapName);\n"
            "            if (!string.IsNullOrEmpty(competition))\n"
            "                parts.Add(competition);\n"
            "            if (!string.IsNullOrEmpty(stage))\n"
            "                parts.Add(stage);\n"
            "            _careerMatchMetaText.text = parts.Count > 0 ? string.Join(\" · \", parts) : string.Empty;\n"
            "        }\n\n"
            "        private void ApplySettingsFrostedChrome(bool enabled)\n"
            "        {\n"
            "            if (!enabled)\n"
            "                return;\n"
            "            Color edge = new Color(Panel.r, Panel.g, Panel.b, 0.72f);\n"
            "            Color face = new Color(Panel.r, Panel.g, Panel.b, 0.42f);\n"
            "            if (_settingsButtonEdge != null)\n"
            "                _settingsButtonEdge.color = edge;\n"
            "            if (_settingsButtonFace != null)\n"
            "                _settingsButtonFace.color = face;\n"
            "        }",
        )

    # ApplyControlBarLayout: remove career bar blocks, compact official row
    old_official = (
        "            if (_careerBarInfoText != null)\n"
        "            {\n"
        "                SetRect(_careerBarInfoText.rectTransform, new Vector2(0.02f, 0.18f), new Vector2(0.58f, 0.82f));\n"
        "                if (_careerBarInfoText.fontSize > 12)\n"
        "                    _careerBarInfoText.fontSize = 12;\n"
        "                _careerBarInfoText.horizontalOverflow = HorizontalWrapMode.Overflow;\n"
        "            }\n"
        "            if (_timeoutButtonRect != null)\n"
        "                SetRect(_timeoutButtonRect, new Vector2(0.595f, 0.22f), new Vector2(0.688f, 0.80f));\n"
        "            if (_pauseButtonRect != null)\n"
        "                SetRect(_pauseButtonRect, new Vector2(0.694f, 0.22f), new Vector2(0.742f, 0.78f));\n"
        "            if (_speedButtonRect != null)\n"
        "                SetRect(_speedButtonRect, new Vector2(0.748f, 0.22f), new Vector2(0.796f, 0.80f));\n"
        "            if (_skipButtonRect != null)\n"
        "                SetRect(_skipButtonRect, new Vector2(0.802f, 0.22f), new Vector2(0.850f, 0.80f));"
    )
    new_official = (
        "            if (_timeoutButtonRect != null)\n"
        "                SetRect(_timeoutButtonRect, new Vector2(0.06f, 0.18f), new Vector2(0.28f, 0.82f));\n"
        "            if (_pauseButtonRect != null)\n"
        "                SetRect(_pauseButtonRect, new Vector2(0.30f, 0.18f), new Vector2(0.42f, 0.82f));\n"
        "            if (_speedButtonRect != null)\n"
        "                SetRect(_speedButtonRect, new Vector2(0.44f, 0.18f), new Vector2(0.56f, 0.82f));\n"
        "            if (_skipButtonRect != null)\n"
        "                SetRect(_skipButtonRect, new Vector2(0.58f, 0.18f), new Vector2(0.70f, 0.82f));"
    )
    if old_official in text:
        text = text.replace(old_official, new_official, 1)

    text = text.replace(
        "                else\n"
        "                    SetRect(_controlBarRect, new Vector2(0.16f, 0.022f), new Vector2(0.895f, 0.078f));",
        "                else\n"
        "                    SetRect(_controlBarRect, new Vector2(0.34f, 0.028f), new Vector2(0.66f, 0.058f));",
        1,
    )
    text = text.replace(
        "                else\n"
        "                    SetRect(_settingsButtonRect, new Vector2(0.902f, 0.022f), new Vector2(0.982f, 0.078f));",
        "                else\n"
        "                    SetRect(_settingsButtonRect, new Vector2(0.664f, 0.028f), new Vector2(0.702f, 0.058f));",
        1,
    )

    if "_careerMatchMetaText = CreateText" not in text:
        text = must_replace(
            text,
            "            _clockText = CreateText(\"Clock\", topHud.transform, 16, TextAnchor.MiddleCenter, TextPrimary);\n"
            "            SetRect(_clockText.rectTransform, new Vector2(0.37f, 0.30f), new Vector2(0.63f, 0.58f));\n"
            "            _clockText.text = \"1:55\";\n\n"
            "            _rightScoreText = CreateText(\"RightScore\", topHud.transform, 42, TextAnchor.MiddleLeft, TextPrimary);",
            "            _clockText = CreateText(\"Clock\", topHud.transform, 16, TextAnchor.MiddleCenter, TextPrimary);\n"
            "            SetRect(_clockText.rectTransform, new Vector2(0.37f, 0.36f), new Vector2(0.63f, 0.58f));\n"
            "            _clockText.text = \"1:55\";\n\n"
            "            _careerMatchMetaText = CreateText(\"CareerMeta\", topHud.transform, 11, TextAnchor.MiddleCenter, TextMuted);\n"
            "            SetRect(_careerMatchMetaText.rectTransform, new Vector2(0.34f, 0.06f), new Vector2(0.66f, 0.28f));\n"
            "            _careerMatchMetaText.horizontalOverflow = HorizontalWrapMode.Overflow;\n"
            "            _careerMatchMetaText.gameObject.SetActive(false);\n\n"
            "            _rightScoreText = CreateText(\"RightScore\", topHud.transform, 42, TextAnchor.MiddleLeft, TextPrimary);",
        )

    if "_careerBarInfoText = CreateText" in text:
        text = must_replace(
            text,
            "            _careerBarInfoText = CreateText(\"CareerInfo\", controlBar.transform, 13, TextAnchor.MiddleLeft, TextPrimary);\n"
            "            SetRect(_careerBarInfoText.rectTransform, new Vector2(0.02f, 0.12f), new Vector2(0.42f, 0.88f));\n"
            "            _careerBarInfoText.gameObject.SetActive(false);\n\n"
            "            _sideLabel = CreateText(\"SideLabel\", controlBar.transform, 13, TextAnchor.MiddleLeft, TextMuted);",
            "            _sideLabel = CreateText(\"SideLabel\", controlBar.transform, 13, TextAnchor.MiddleLeft, TextMuted);",
        )

    if "_settingsButtonEdge = settingsBtn" not in text:
        text = must_replace(
            text,
            "            _settingsButtonRect = settingsBtn.GetComponent<RectTransform>();\n\n"
            "            CreateZoneLabels();",
            "            _settingsButtonRect = settingsBtn.GetComponent<RectTransform>();\n"
            "            _settingsButtonEdge = settingsBtn.GetComponent<Image>();\n"
            "            Transform settingsFaceTransform = settingsBtn.transform.Find(\"Face\");\n"
            "            if (settingsFaceTransform != null)\n"
            "                _settingsButtonFace = settingsFaceTransform.GetComponent<Image>();\n"
            "            ApplySettingsFrostedChrome(true);\n\n"
            "            CreateZoneLabels();",
        )

    if "if (_careerBarInfoText != null)" in text and "SetRect(_careerBarInfoText" in text:
        text = text.replace(
            "                if (_careerBarInfoText != null)\n"
            "                    SetRect(_careerBarInfoText.rectTransform, new Vector2(0.02f, 0.12f), new Vector2(0.42f, 0.88f));\n",
            "",
            1,
        )

    if "private Text _careerBarInfoText;" in text:
        text = text.replace("        private Text _careerBarInfoText;\n", "", 1)

    if "擊殺廣播" not in text:
        raise RuntimeError("UTF-8 corrupted")

    TARGET.write_text(text, encoding="utf-8", newline="\n")
    print("patched", TARGET)


if __name__ == "__main__":
    main()
