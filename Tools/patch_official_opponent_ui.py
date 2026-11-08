# UTF-8-safe: fix opponent team UI + tighter official bar + hide duplicate match label
from pathlib import Path

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing anchor: {old[:100]!r}")
    return text.replace(old, new, 1)


def main() -> None:
    text = TARGET.read_text(encoding="utf-8")

    old_right = 'string rightId = myTeamId == "navi_2021" ? "gambit_2021" : "navi_2021";'
    new_right = "string rightId = _opponentTeamId;"
    while old_right in text:
        text = text.replace(old_right, new_right, 1)

    text = must_replace(
        text,
        """        private void UpdateScore()
        {
            if (_leftScoreText == null || _rightScoreText == null)
                return;

            string leftId = myTeamId;
            string rightId = _opponentTeamId;
            int leftScore = leftId == "navi_2021" ? _naviScore : _gambitScore;
            int rightScore = rightId == "navi_2021" ? _naviScore : _gambitScore;""",
        """        private void UpdateScore()
        {
            if (_leftScoreText == null || _rightScoreText == null)
                return;

            string teamAId = _session?.State.TeamA.Definition.id ?? myTeamId;
            string leftId = myTeamId;
            string rightId = _opponentTeamId;
            int teamAScore = _naviScore;
            int teamBScore = _gambitScore;
            int leftScore = string.Equals(leftId, teamAId, System.StringComparison.OrdinalIgnoreCase)
                ? teamAScore : teamBScore;
            int rightScore = string.Equals(rightId, teamAId, System.StringComparison.OrdinalIgnoreCase)
                ? teamAScore : teamBScore;""",
    )

    text = must_replace(
        text,
        """            bool naviIsT = MatchRules.TeamAIsTerrorist(Mathf.Max(0, _currentRound - 1));
            bool leftIsNavi = myTeamId == "navi_2021";
            bool leftIsT = leftIsNavi ? naviIsT : !naviIsT;
            _leftStatsPanel.color = leftIsT ? TPanel : CtPanel;
            _rightStatsPanel.color = leftIsT ? CtPanel : TPanel;""",
        """            string leftId = myTeamId;
            string rightId = _opponentTeamId;
            bool leftIsT = TeamIsTerrorist(leftId, _currentRound);
            bool rightIsT = TeamIsTerrorist(rightId, _currentRound);
            _leftStatsPanel.color = leftIsT ? TPanel : CtPanel;
            _rightStatsPanel.color = rightIsT ? TPanel : CtPanel;""",
    )

    text = must_replace(
        text,
        """        private void ResetEconomyDisplay()
        {
            _teamMoney["navi_2021"] = EconomyRules.StartingTeamMoney;
            _teamMoney["gambit_2021"] = EconomyRules.StartingTeamMoney;
            _teamCards["navi_2021"] = EquipmentLevel.Eco;
            _teamCards["gambit_2021"] = EquipmentLevel.Eco;
            UpdateEconomyHud();
        }""",
        """        private void ResetEconomyDisplay()
        {
            string teamA = _session?.State.TeamA.Definition.id ?? myTeamId;
            string teamB = _session?.State.TeamB.Definition.id ?? _opponentTeamId;
            foreach (string teamId in new[] { teamA, teamB })
            {
                if (string.IsNullOrEmpty(teamId))
                    continue;
                _teamMoney[teamId] = EconomyRules.StartingTeamMoney;
                _teamCards[teamId] = EquipmentLevel.Eco;
            }
            UpdateEconomyHud();
        }""",
    )

    if "_matchText.gameObject.SetActive(simulation)" not in text:
        text = must_replace(
            text,
            "            if (_sideLabel != null)\n"
            "                _sideLabel.gameObject.SetActive(simulation);\n"
            "            if (_careerBarInfoText != null)",
            "            if (_sideLabel != null)\n"
            "                _sideLabel.gameObject.SetActive(simulation);\n"
            "            if (_matchText != null)\n"
            "                _matchText.gameObject.SetActive(simulation);\n"
            "            if (_careerBarInfoText != null)",
        )

    text = must_replace(
        text,
        "                else\n"
        "                    SetRect(_controlBarRect, new Vector2(0.14f, 0.020f), new Vector2(0.88f, 0.082f));",
        "                else\n"
        "                    SetRect(_controlBarRect, new Vector2(0.16f, 0.022f), new Vector2(0.895f, 0.078f));",
    )

    text = must_replace(
        text,
        "                else\n"
        "                    SetRect(_settingsButtonRect, new Vector2(0.905f, 0.020f), new Vector2(0.985f, 0.082f));",
        "                else\n"
        "                    SetRect(_settingsButtonRect, new Vector2(0.902f, 0.022f), new Vector2(0.982f, 0.078f));",
    )

    text = must_replace(
        text,
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
        "                SetRect(_skipButtonRect, new Vector2(0.822f, 0.20f), new Vector2(0.875f, 0.80f));",
        "            if (_careerBarInfoText != null)\n"
        "            {\n"
        "                SetRect(_careerBarInfoText.rectTransform, new Vector2(0.02f, 0.18f), new Vector2(0.58f, 0.82f));\n"
        "                if (_careerBarInfoText.fontSize > 12)\n"
        "                    _careerBarInfoText.fontSize = 12;\n"
        "                _careerBarInfoText.horizontalOverflow = HorizontalWrapMode.Overflow;\n"
        "            }\n"
        "            if (_timeoutButtonRect != null)\n"
        "                SetRect(_timeoutButtonRect, new Vector2(0.595f, 0.22f), new Vector2(0.688f, 0.78f));\n"
        "            if (_pauseButtonRect != null)\n"
        "                SetRect(_pauseButtonRect, new Vector2(0.694f, 0.22f), new Vector2(0.742f, 0.78f));\n"
        "            if (_speedButtonRect != null)\n"
        "                SetRect(_speedButtonRect, new Vector2(0.748f, 0.22f), new Vector2(0.796f, 0.78f));\n"
        "            if (_skipButtonRect != null)\n"
        "                SetRect(_skipButtonRect, new Vector2(0.802f, 0.22f), new Vector2(0.850f, 0.78f));",
    )

    text = must_replace(
        text,
        '            _careerBarInfoText.text = $"正式賽 · {mapName} · {competition}{stage}";',
        '            string opponent = DemoZh.TeamName(_opponentTeamId);\n'
        '            _careerBarInfoText.text = $"正式賽 · {mapName} · vs {opponent} · {competition}{stage}";',
    )

    text = must_replace(
        text,
        """            bool leftIsT = TeamIsTerrorist(leftId, _currentRound);
            if (_leftAliveBadge != null)
                _leftAliveBadge.color = leftIsT ? TAliveBg : CtAliveBg;
            if (_rightAliveBadge != null)
                _rightAliveBadge.color = leftIsT ? CtAliveBg : TAliveBg;""",
        """            bool leftIsT = TeamIsTerrorist(leftId, _currentRound);
            bool rightIsT = TeamIsTerrorist(rightId, _currentRound);
            if (_leftAliveBadge != null)
                _leftAliveBadge.color = leftIsT ? TAliveBg : CtAliveBg;
            if (_rightAliveBadge != null)
                _rightAliveBadge.color = rightIsT ? TAliveBg : CtAliveBg;""",
    )

    if "擊殺廣播" not in text:
        raise RuntimeError("UTF-8 corrupted")

    TARGET.write_text(text, encoding="utf-8", newline="\n")
    print("patched", TARGET)


if __name__ == "__main__":
    main()
