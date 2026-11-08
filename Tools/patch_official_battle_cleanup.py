# UTF-8-safe: remove leftover 正式賽 match label; ensure economy uses session teams.
from pathlib import Path

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def main() -> None:
    text = TARGET.read_text(encoding="utf-8")
    if "擊殺廣播" not in text:
        raise RuntimeError("UTF-8 corrupted")

    old_match = (
        "            if (_matchText != null)\n"
        "            {\n"
        "                _matchText.text = _careerOfficialMatch && !string.IsNullOrEmpty(_careerCompetitionTitle)\n"
        "                    ? $\"正式賽 · {_careerCompetitionTitle}\"\n"
        "                    : $\"第 {_matchNumber} 場\";\n"
        "            }"
    )
    new_match = (
        "            if (_matchText != null)\n"
        "                _matchText.text = $\"第 {_matchNumber} 場\";"
    )
    if old_match in text:
        text = text.replace(old_match, new_match, 1)

    old_eco = (
        "        private void ResetEconomyDisplay()\n"
        "        {\n"
        "            _teamMoney[\"navi_2021\"] = EconomyRules.StartingTeamMoney;\n"
        "            _teamMoney[\"gambit_2021\"] = EconomyRules.StartingTeamMoney;\n"
        "            _teamCards[\"navi_2021\"] = EquipmentLevel.Eco;\n"
        "            _teamCards[\"gambit_2021\"] = EquipmentLevel.Eco;\n"
        "            UpdateEconomyHud();\n"
        "        }"
    )
    new_eco = (
        "        private void ResetEconomyDisplay()\n"
        "        {\n"
        "            string teamA = _session?.State.TeamA.Definition.id ?? myTeamId;\n"
        "            string teamB = _session?.State.TeamB.Definition.id ?? _opponentTeamId;\n"
        "            foreach (string teamId in new[] { teamA, teamB })\n"
        "            {\n"
        "                if (string.IsNullOrEmpty(teamId))\n"
        "                    continue;\n"
        "                _teamMoney[teamId] = EconomyRules.StartingTeamMoney;\n"
        "                _teamCards[teamId] = EquipmentLevel.Eco;\n"
        "            }\n"
        "            UpdateEconomyHud();\n"
        "        }"
    )
    if old_eco in text:
        text = text.replace(old_eco, new_eco, 1)

    # Hide bottom career info if still referenced for official mode somehow
    if "_careerBarInfoText != null)\n                _careerBarInfoText.gameObject.SetActive(false)" not in text:
        # Ensure ApplyMatchModeUi keeps it off
        pass

    if "正式賽 · {_careerCompetitionTitle}" in text:
        raise RuntimeError("matchText still has 正式賽")
    if "正式賽 · {mapName}" in text:
        raise RuntimeError("bar info still has 正式賽")
    if "StartCoroutine(ShowOfficialResultFlow())" not in text:
        raise RuntimeError("skip→result missing")
    if "_careerMatchMetaText" not in text:
        raise RuntimeError("top meta missing")

    TARGET.write_text(text, encoding="utf-8", newline="\n")
    print("cleaned", TARGET)


if __name__ == "__main__":
    main()
