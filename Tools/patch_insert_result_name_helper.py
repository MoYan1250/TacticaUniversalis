# -*- coding: utf-8 -*-
"""Insert missing ResolveBattleTeamDisplayName helper after prior partial patch."""
from pathlib import Path
import backup_quickmatch as bak

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def main() -> None:
    bak.backup("before_insert_result_name_helper")
    text = TARGET.read_text(encoding="utf-8")
    if "擊殺廣播" not in text:
        raise RuntimeError("UTF-8 corrupted")

    if "private string ResolveBattleTeamDisplayName" in text:
        print("helper already present")
        return

    helper = """
        private string ResolveBattleTeamDisplayName(string teamId)
        {
            if (_content != null &&
                _content.Teams.TryGetValue(teamId, out TeamDefinition team) &&
                !string.IsNullOrWhiteSpace(team.displayName))
                return DemoZh.TeamName(team.displayName);
            return DemoZh.TeamName(teamId);
        }

"""

    anchor = "        private void EnsureOfficialResultOverlay()\n"
    if anchor not in text:
        raise RuntimeError("missing EnsureOfficialResultOverlay anchor")
    text = text.replace(anchor, helper + anchor, 1)

    if "擊殺廣播" not in text:
        raise RuntimeError("UTF-8 corrupted after insert")
    if "private string ResolveBattleTeamDisplayName" not in text:
        raise RuntimeError("helper still missing")

    TARGET.write_text(text, encoding="utf-8")
    print("inserted helper into", TARGET)


if __name__ == "__main__":
    main()
