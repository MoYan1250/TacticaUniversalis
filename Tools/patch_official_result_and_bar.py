# UTF-8-safe: centered official bar + result overlay + skip/career flow
from pathlib import Path

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing anchor: {old[:100]!r}")
    return text.replace(old, new, 1)


def main() -> None:
    text = TARGET.read_text(encoding="utf-8")

    if "_officialResultRoot" not in text:
        text = must_replace(
            text,
            "        private RectTransform _skipButtonRect;\n        private GameObject _settingsRoot;",
            "        private RectTransform _skipButtonRect;\n"
            "        private GameObject _officialResultRoot;\n"
            "        private Text _officialResultBody;\n"
            "        private bool _awaitingOfficialResultConfirm;\n"
            "        private GameObject _settingsRoot;",
        )

    text = text.replace(
        "                else\n"
        "                    SetRect(_controlBarRect, new Vector2(0.34f, 0.028f), new Vector2(0.66f, 0.058f));",
        "                else\n"
        "                    SetRect(_controlBarRect, new Vector2(0.392f, 0.028f), new Vector2(0.608f, 0.054f));",
        1,
    )
    text = text.replace(
        "                else\n"
        "                    SetRect(_settingsButtonRect, new Vector2(0.664f, 0.028f), new Vector2(0.702f, 0.058f));",
        "                else\n"
        "                    SetRect(_settingsButtonRect, new Vector2(0.612f, 0.028f), new Vector2(0.642f, 0.054f));",
        1,
    )
    text = text.replace(
        "            if (_timeoutButtonRect != null)\n"
        "                SetRect(_timeoutButtonRect, new Vector2(0.06f, 0.18f), new Vector2(0.28f, 0.82f));\n"
        "            if (_pauseButtonRect != null)\n"
        "                SetRect(_pauseButtonRect, new Vector2(0.30f, 0.18f), new Vector2(0.42f, 0.82f));\n"
        "            if (_speedButtonRect != null)\n"
        "                SetRect(_speedButtonRect, new Vector2(0.44f, 0.18f), new Vector2(0.56f, 0.82f));\n"
        "            if (_skipButtonRect != null)\n"
        "                SetRect(_skipButtonRect, new Vector2(0.58f, 0.18f), new Vector2(0.70f, 0.82f));",
        "            if (_timeoutButtonRect != null)\n"
        "                SetRect(_timeoutButtonRect, new Vector2(0.04f, 0.20f), new Vector2(0.26f, 0.80f));\n"
        "            if (_pauseButtonRect != null)\n"
        "                SetRect(_pauseButtonRect, new Vector2(0.28f, 0.20f), new Vector2(0.44f, 0.80f));\n"
        "            if (_speedButtonRect != null)\n"
        "                SetRect(_speedButtonRect, new Vector2(0.46f, 0.20f), new Vector2(0.62f, 0.80f));\n"
        "            if (_skipButtonRect != null)\n"
        "                SetRect(_skipButtonRect, new Vector2(0.64f, 0.20f), new Vector2(0.80f, 0.80f));",
        1,
    )

    if "ShowOfficialResultFlow" not in text:
        text = must_replace(
            text,
            "            if (_careerOfficialMatch && _result != null)\n"
            "            {\n"
            "                CareerMatchLaunchContext.SetCompletedResult(_result);\n"
            "                UnityEngine.SceneManagement.SceneManager.LoadScene(\"Demo_Career\");\n"
            "            }\n"
            "        }\n\n"
            "        private IEnumerator PlayEventRange(int startInclusive, int endExclusive, bool animate)",
            "            if (_careerOfficialMatch && _result != null)\n"
            "                yield return ShowOfficialResultFlow();\n"
            "        }\n\n"
            "        private IEnumerator ShowOfficialResultFlow()\n"
            "        {\n"
            "            EnsureOfficialResultOverlay();\n"
            "            if (_officialResultBody != null)\n"
            "                _officialResultBody.text = FormatOfficialResultSummary();\n"
            "            if (_officialResultRoot != null)\n"
            "                _officialResultRoot.SetActive(true);\n"
            "            _awaitingOfficialResultConfirm = true;\n"
            "            while (_awaitingOfficialResultConfirm)\n"
            "                yield return null;\n"
            "        }\n\n"
            "        private string FormatOfficialResultSummary()\n"
            "        {\n"
            "            if (_result == null || _session == null)\n"
            "                return string.Empty;\n"
            "            string teamAId = _session.State.TeamA.Definition.id;\n"
            "            bool myTeamA = string.Equals(myTeamId, teamAId, StringComparison.OrdinalIgnoreCase);\n"
            "            int myScore = myTeamA ? _result.teamAScore : _result.teamBScore;\n"
            "            int oppScore = myTeamA ? _result.teamBScore : _result.teamAScore;\n"
            "            string myName = DemoZh.TeamName(myTeamId);\n"
            "            string oppName = DemoZh.TeamName(_opponentTeamId);\n"
            "            bool won = string.Equals(_result.winnerTeamId, myTeamId, StringComparison.OrdinalIgnoreCase);\n"
            "            string outcome = won ? \"勝利\" : \"落敗\";\n"
            "            return $\"{myName}  {myScore} : {oppScore}  {oppName}\\n\\n{outcome}\";\n"
            "        }\n\n"
            "        private void EnsureOfficialResultOverlay()\n"
            "        {\n"
            "            if (_officialResultRoot != null || _canvasRect == null)\n"
            "                return;\n"
            "            Image dim = CreateUiObject<Image>(\"OfficialResultDim\", _canvasRect);\n"
            "            Stretch(dim.rectTransform);\n"
            "            dim.color = new Color(0f, 0f, 0f, 0.62f);\n"
            "            dim.raycastTarget = true;\n"
            "            _officialResultRoot = dim.gameObject;\n"
            "\n"
            "            Image panel = CreateUiObject<Image>(\"OfficialResultPanel\", dim.transform);\n"
            "            SetRect(panel.rectTransform, new Vector2(0.36f, 0.40f), new Vector2(0.64f, 0.62f));\n"
            "            panel.color = new Color(Panel.r, Panel.g, Panel.b, 0.94f);\n"
            "\n"
            "            Text title = CreateText(\"Title\", panel.transform, 18, TextAnchor.MiddleCenter, Accent, _uiFont700);\n"
            "            SetRect(title.rectTransform, new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.94f));\n"
            "            title.text = \"比賽結束\";\n"
            "            title.fontStyle = FontStyle.Bold;\n"
            "\n"
            "            _officialResultBody = CreateText(\"Body\", panel.transform, 16, TextAnchor.MiddleCenter, TextPrimary);\n"
            "            SetRect(_officialResultBody.rectTransform, new Vector2(0.08f, 0.32f), new Vector2(0.92f, 0.72f));\n"
            "            _officialResultBody.lineSpacing = 1.15f;\n"
            "\n"
            "            CreateButton(\"ConfirmOfficialResult\", \"確認\", panel.transform,\n"
            "                new Vector2(0.28f, 0.08f), new Vector2(0.72f, 0.26f), OnConfirmOfficialResult);\n"
            "            _officialResultRoot.SetActive(false);\n"
            "        }\n\n"
            "        private void OnConfirmOfficialResult()\n"
            "        {\n"
            "            _awaitingOfficialResultConfirm = false;\n"
            "            if (_officialResultRoot != null)\n"
            "                _officialResultRoot.SetActive(false);\n"
            "            if (_result != null)\n"
            "                CareerMatchLaunchContext.SetCompletedResult(_result);\n"
            "            UnityEngine.SceneManagement.SceneManager.LoadScene(\"Demo_Career\");\n"
            "        }\n\n"
            "        private IEnumerator PlayEventRange(int startInclusive, int endExclusive, bool animate)",
        )

    if "StartCoroutine(ShowOfficialResultFlow())" not in text:
        text = must_replace(
            text,
            "            UpdateKillFeed();\n"
            "            UpdateTraitFeed();\n"
            "            UpdateAliveCountHud();\n"
            "            UpdateEconomyHud();\n"
            "        }\n\n"
            "        private void TogglePause()",
            "            UpdateKillFeed();\n"
            "            UpdateTraitFeed();\n"
            "            UpdateAliveCountHud();\n"
            "            UpdateEconomyHud();\n"
            "\n"
            "            if (_careerOfficialMatch && _result != null && _result.winnerTeamId != null)\n"
            "                StartCoroutine(ShowOfficialResultFlow());\n"
            "        }\n\n"
            "        private void TogglePause()",
        )

    old_skip_session = (
        "            if (_session != null && !_session.IsFinished)\n"
        "                _session.AutoPlayRemaining(myTeamId);\n"
        "            _result = _session?.Result;\n"
    )
    new_skip_session = (
        "            if (_session != null && !_session.IsFinished)\n"
        "            {\n"
        "                if (_session.IsComplete)\n"
        "                    _session.FinishMatch();\n"
        "                else\n"
        "                    _session.AutoPlayRemaining(myTeamId);\n"
        "            }\n"
        "            _result = _session?.Result;\n"
    )
    if old_skip_session in text:
        text = text.replace(old_skip_session, new_skip_session, 1)

    if "BuildOfficialResultOverlay" not in text:
        text = must_replace(
            text,
            "            BuildSettingsPanel(canvas.transform);\n"
            "            ApplyMatchModeUi();\n"
            "        }\n\n"
            "        private void BuildSettingsPanel(Transform canvas)",
            "            BuildSettingsPanel(canvas.transform);\n"
            "            EnsureOfficialResultOverlay();\n"
            "            ApplyMatchModeUi();\n"
            "        }\n\n"
            "        private void BuildSettingsPanel(Transform canvas)",
        )

    if "擊殺廣播" not in text:
        raise RuntimeError("UTF-8 corrupted")

    TARGET.write_text(text, encoding="utf-8", newline="\n")
    print("patched", TARGET)


if __name__ == "__main__":
    main()
