# UTF-8-safe QuickMatchDemoController patcher (official match UI only).
# Always reads the current TARGET file — never overwrites from Local History backup.
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def patch(text: str) -> str:
    if "ApplyMatchModeUi" in text and "_naviSideButtonRoot" in text:
        print("official UI patches already present; no changes")
        return text

    text = text.replace(
        "        private string _careerCompetitionTitle;\n\n        private ContentDatabase _content;",
        "        private string _careerCompetitionTitle;\n"
        "        private string _careerMapDisplayName;\n"
        "        private string _careerBracketStageLabel;\n\n"
        "        private ContentDatabase _content;",
        1,
    )
    text = text.replace(
        "        private Text _naviSideButton;\n        private GameObject _settingsRoot;",
        "        private Text _naviSideButton;\n"
        "        private GameObject _naviSideButtonRoot;\n"
        "        private GameObject _gambitSideButtonRoot;\n"
        "        private GameObject _newSeedButtonRoot;\n"
        "        private Text _controlBarTitle;\n"
        "        private Text _careerBarInfoText;\n"
        "        private GameObject _settingsRoot;",
        1,
    )
    text = text.replace(
        "                _careerCompetitionTitle = launch.competitionDisplayName;\n            }",
        "                _careerCompetitionTitle = launch.competitionDisplayName;\n"
        "                _careerMapDisplayName = string.IsNullOrEmpty(launch.mapDisplayName) ? \"Dust II\" : launch.mapDisplayName;\n"
        "                _careerBracketStageLabel = launch.bracketStageLabel;\n            }",
        1,
    )
    text = text.replace(
        "            GenerateMatch();\n        }\n\n        private void OnDestroy()",
        "            GenerateMatch();\n"
        "            ApplyMatchModeUi();\n"
        "        }\n\n"
        "        private void ApplyMatchModeUi()\n"
        "        {\n"
        "            bool simulation = !_careerOfficialMatch;\n"
        "            if (_naviSideButtonRoot != null)\n"
        "                _naviSideButtonRoot.SetActive(simulation);\n"
        "            if (_gambitSideButtonRoot != null)\n"
        "                _gambitSideButtonRoot.SetActive(simulation);\n"
        "            if (_newSeedButtonRoot != null)\n"
        "                _newSeedButtonRoot.SetActive(simulation);\n"
        "            if (_controlBarTitle != null)\n"
        "                _controlBarTitle.gameObject.SetActive(simulation);\n"
        "            if (_sideLabel != null)\n"
        "                _sideLabel.gameObject.SetActive(simulation);\n"
        "            if (_careerBarInfoText != null)\n"
        "            {\n"
        "                _careerBarInfoText.gameObject.SetActive(!simulation);\n"
        "                if (!simulation)\n"
        "                    RefreshOfficialControlBarInfo();\n"
        "            }\n"
        "        }\n\n"
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
        "            _careerBarInfoText.text = $\"正式賽 · {mapName} · {competition}{stage}\";\n"
        "        }\n\n"
        "        private void OnDestroy()",
        1,
    )
    text = text.replace(
        "            RefreshTimeoutCallButton();\n"
        "            _playback = StartCoroutine(MatchLoop());\n"
        "        }\n\n"
        "        private IEnumerator MatchLoop()",
        "            RefreshTimeoutCallButton();\n"
        "            ApplyMatchModeUi();\n"
        "            _playback = StartCoroutine(MatchLoop());\n"
        "        }\n\n"
        "        private IEnumerator MatchLoop()",
        1,
    )
    text = text.replace(
        "        private void SelectSide(string teamId)\n        {\n            if (myTeamId == teamId)",
        "        private void SelectSide(string teamId)\n        {\n            if (_careerOfficialMatch)\n                return;\n            if (myTeamId == teamId)",
        1,
    )
    text = text.replace(
        "        private void NewSeed()\n        {\n            seed++;",
        "        private void NewSeed()\n        {\n            if (_careerOfficialMatch)\n                return;\n            seed++;",
        1,
    )
    text = text.replace(
        '            title.text = "戰術控制室 · Dust II";\n\n            _sideLabel = CreateText("SideLabel", controlBar.transform, 13, TextAnchor.MiddleLeft, TextMuted);',
        '            title.text = "戰術控制室 · Dust II";\n'
        "            _controlBarTitle = title;\n\n"
        '            _careerBarInfoText = CreateText("CareerInfo", controlBar.transform, 13, TextAnchor.MiddleLeft, TextPrimary);\n'
        "            SetRect(_careerBarInfoText.rectTransform, new Vector2(0.02f, 0.12f), new Vector2(0.42f, 0.88f));\n"
        "            _careerBarInfoText.gameObject.SetActive(false);\n\n"
        '            _sideLabel = CreateText("SideLabel", controlBar.transform, 13, TextAnchor.MiddleLeft, TextMuted);',
        1,
    )
    text = text.replace(
        "            _naviSideButton = naviBtn.GetComponentInChildren<Text>();\n"
        '            Button gambitBtn = CreateButton("SideGambit", "Gambit", controlBar.transform,',
        "            _naviSideButtonRoot = naviBtn.gameObject;\n"
        "            _naviSideButton = naviBtn.GetComponentInChildren<Text>();\n"
        '            Button gambitBtn = CreateButton("SideGambit", "Gambit", controlBar.transform,',
        1,
    )
    text = text.replace(
        "            _gambitSideButton = gambitBtn.GetComponentInChildren<Text>();\n\n            _matchText = CreateText(\"Match\", controlBar.transform, 13, TextAnchor.MiddleCenter, TextMuted);",
        "            _gambitSideButtonRoot = gambitBtn.gameObject;\n"
        "            _gambitSideButton = gambitBtn.GetComponentInChildren<Text>();\n\n            _matchText = CreateText(\"Match\", controlBar.transform, 13, TextAnchor.MiddleCenter, TextMuted);",
        1,
    )
    text = text.replace(
        '            CreateButton("NewSeed", "下一場", controlBar.transform,\n'
        "                new Vector2(0.86f, 0.15f), new Vector2(0.985f, 0.85f), NewSeed);\n\n            Sprite settingsSprite",
        '            Button newSeedBtn = CreateButton("NewSeed", "下一場", controlBar.transform,\n'
        "                new Vector2(0.86f, 0.15f), new Vector2(0.985f, 0.85f), NewSeed);\n"
        "            _newSeedButtonRoot = newSeedBtn.gameObject;\n\n            Sprite settingsSprite",
        1,
    )
    text = text.replace(
        "            BuildSettingsPanel(canvas.transform);\n        }",
        "            BuildSettingsPanel(canvas.transform);\n            ApplyMatchModeUi();\n        }",
        1,
    )
    for needle in ("ApplyMatchModeUi", "RefreshOfficialControlBarInfo", "_naviSideButtonRoot", "擊殺廣播"):
        if needle not in text:
            raise RuntimeError(f"missing {needle}")
    return text


def main() -> None:
    text = TARGET.read_text(encoding="utf-8")
    patched = patch(text)
    if patched != text:
        TARGET.write_text(patched, encoding="utf-8", newline="\n")
        print("patched", TARGET)
    else:
        print("unchanged", TARGET)


if __name__ == "__main__":
    main()
