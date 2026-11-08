# -*- coding: utf-8 -*-
"""Battle profile: show combat attribute sum; result names prefer display without year."""
from pathlib import Path
import backup_quickmatch as bak

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing:\n{old[:240]!r}")
    return text.replace(old, new, 1)


def main() -> None:
    bak.backup("before_ability_sum_and_result_name")
    text = TARGET.read_text(encoding="utf-8")
    if "擊殺廣播" not in text:
        raise RuntimeError("UTF-8 corrupted before patch")

    old_profile = """        private string BuildProfilePopupText(PlayerDefinition player)
        {
            var sb = new StringBuilder(512);
            sb.AppendLine("<color=#EBB438>戰鬥屬性</color>");
            CombatAttributes a = player.attributes ?? new CombatAttributes();
            sb.AppendLine($"瞄準  {a.aim}    反應  {a.reaction}");"""

    new_profile = """        private string BuildProfilePopupText(PlayerDefinition player)
        {
            var sb = new StringBuilder(512);
            CombatAttributes a = player.attributes ?? new CombatAttributes();
            int combatTotal = DevelopmentRules.SumCombatAttributes(a);
            sb.AppendLine(combatTotal > 0
                ? $"<color=#EBB438>戰鬥屬性 {combatTotal}</color>"
                : "<color=#EBB438>戰鬥屬性</color>");
            sb.AppendLine($"瞄準  {a.aim}    反應  {a.reaction}");"""

    text = must_replace(text, old_profile, new_profile)

    old_names = """            BindTeamLogo(_officialResultLeftLogo, myTeamId);
            BindTeamLogo(_officialResultRightLogo, _opponentTeamId);
            if (_officialResultLeftName != null)
                _officialResultLeftName.text = DemoZh.TeamName(myTeamId);
            if (_officialResultRightName != null)
                _officialResultRightName.text = DemoZh.TeamName(_opponentTeamId);"""

    new_names = """            BindTeamLogo(_officialResultLeftLogo, myTeamId);
            BindTeamLogo(_officialResultRightLogo, _opponentTeamId);
            if (_officialResultLeftName != null)
                _officialResultLeftName.text = ResolveBattleTeamDisplayName(myTeamId);
            if (_officialResultRightName != null)
                _officialResultRightName.text = ResolveBattleTeamDisplayName(_opponentTeamId);"""

    text = must_replace(text, old_names, new_names)

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
    if "private string ResolveBattleTeamDisplayName" not in text:
        if anchor not in text:
            raise RuntimeError("missing EnsureOfficialResultOverlay anchor")
        text = text.replace(anchor, helper + anchor, 1)

    if "擊殺廣播" not in text:
        raise RuntimeError("UTF-8 corrupted after patch")
    if "DevelopmentRules.SumCombatAttributes" not in text:
        raise RuntimeError("ability sum not applied")
    if "ResolveBattleTeamDisplayName" not in text:
        raise RuntimeError("result name helper not applied")

    TARGET.write_text(text, encoding="utf-8")
    print("patched", TARGET)


if __name__ == "__main__":
    main()
