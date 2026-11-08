# -*- coding: utf-8 -*-
"""Remove assist display from battle scoreboard bind data."""
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
import backup_quickmatch as bak

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing:\n{old[:240]!r}")
    return text.replace(old, new, 1)


def main() -> None:
    bak.backup("before_scoreboard_mock_layout")
    text = TARGET.read_text(encoding="utf-8")
    if "擊殺" not in text or "跳過結算中" not in text:
        raise RuntimeError("UTF-8 corrupted")

    old = """            Color panelDark = BattleStatPanelPresentation.PanelColor(teamIsT);
            Color accentLight = BattleStatPanelPresentation.AccentColor(teamIsT);
            bool showAssists = _result != null || (_session != null && _session.IsFinished);
            int teamMoney = _teamMoney.TryGetValue(team.id, out int purse) ? purse : EconomyRules.StartingTeamMoney;"""
    new = """            Color panelDark = BattleStatPanelPresentation.PanelColor(teamIsT);
            Color accentLight = BattleStatPanelPresentation.AccentColor(teamIsT);
            int teamMoney = _teamMoney.TryGetValue(team.id, out int purse) ? purse : EconomyRules.StartingTeamMoney;"""
    if "bool showAssists" in text:
        text = must_replace(text, old, new)

    old_data = """                    Kills = stat.Kills,
                    Deaths = stat.Deaths,
                    Assists = stat.Assists,
                    RoundKills = stat.RoundKills,
                    Money = money,
                    PrimaryWeaponName = primaryName,
                    SecondaryWeaponName = secondaryName,
                    HasDefuseKit = hasKit,
                    Armor = armor,
                    Mentality = stat.Mentality,
                    Form = stat.Form,
                    Stamina = stat.Stamina,
                    Alive = stat.Alive,
                    ShowAssists = showAssists
                };"""
    new_data = """                    Kills = stat.Kills,
                    Deaths = stat.Deaths,
                    RoundKills = stat.RoundKills,
                    Money = money,
                    PrimaryWeaponName = primaryName,
                    SecondaryWeaponName = secondaryName,
                    HasDefuseKit = hasKit,
                    Armor = armor,
                    Mentality = stat.Mentality,
                    Form = stat.Form,
                    Stamina = stat.Stamina,
                    Alive = stat.Alive
                };"""
    if "ShowAssists = showAssists" in text:
        text = must_replace(text, old_data, new_data)

    TARGET.write_text(text, encoding="utf-8")
    verify = TARGET.read_text(encoding="utf-8")
    if "擊殺" not in verify or "跳過結算中" not in verify:
        raise RuntimeError("UTF-8 corrupted after write")
    if "ShowAssists" in verify:
        raise RuntimeError("ShowAssists still present")
    print("patched scoreboard mock layout OK")


if __name__ == "__main__":
    main()
