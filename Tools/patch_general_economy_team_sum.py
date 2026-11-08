# -*- coding: utf-8 -*-
"""General mode: card cost shows team total spend; softer card copy."""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "Tools"))
from backup_quickmatch import backup  # noqa: E402

SRC = ROOT / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def main() -> int:
    backup("general_economy_team_sum")
    text = SRC.read_text(encoding="utf-8")
    if "KillFeed" not in text and "ApplyControlBarLayout(simulation)" not in text:
        raise RuntimeError("UTF-8 / structure anchors missing")

    old_cost = (
        "cost = EconomyCardLoadoutRules.EstimateCardCost(\n"
        "                        level, player.Side, _weaponCatalog, hasAwper);"
    )
    new_cost = (
        "cost = EconomyCardLoadoutRules.EstimateTeamCardCost(\n"
        "                        player, level, _weaponCatalog);"
    )
    if old_cost not in text:
        raise RuntimeError("EstimateCardCost call not found")
    text = text.replace(old_cost, new_cost, 1)

    old_reset = "_teamMoney[teamId] = EconomyRules.StartingTeamMoney;"
    new_reset = (
        "_teamMoney[teamId] = IsGeneralBattleMode()\n"
        "                    ? ItemizedEconomyRules.StartingPlayerMoney * 5\n"
        "                    : EconomyRules.StartingTeamMoney;"
    )
    if old_reset not in text:
        raise RuntimeError("ResetEconomyDisplay money init not found")
    text = text.replace(old_reset, new_reset, 1)

    # Replace CardDescription body: keep quick-mode mult copy; general mode uses intent copy.
    start = text.find("private static string CardDescription(EquipmentLevel level)")
    if start < 0:
        raise RuntimeError("CardDescription not found")
    end = text.find("private void ShowCardPicker()", start)
    if end < 0:
        raise RuntimeError("ShowCardPicker after CardDescription not found")

    new_method = (
        "private string CardDescription(EquipmentLevel level)\n"
        "        {\n"
        "            if (IsGeneralBattleMode())\n"
        "            {\n"
        "                switch (level)\n"
        "                {\n"
        "                    case EquipmentLevel.Eco:\n"
        "                        return \"全隊不購裝\\n保留既有槍械\";\n"
        "                    case EquipmentLevel.Light:\n"
        "                        return \"全隊輕裝意圖\\n槍械由選手自行決定\";\n"
        "                    case EquipmentLevel.Standard:\n"
        "                        return \"全隊標準裝備意圖\\n槍械由選手自行決定\";\n"
        "                    case EquipmentLevel.FullBuy:\n"
        "                        return \"全隊滿配意圖\\n槍械由選手自行決定\";\n"
        "                    case EquipmentLevel.AwpFullBuy:\n"
        "                        return \"全隊狙擊配置意圖\\n需狙擊手\\n槍械由選手自行決定\";\n"
        "                    default:\n"
        "                        return \"全隊經濟意圖\";\n"
        "                }\n"
        "            }\n"
        "            float mult = EconomyRules.EquipmentMultiplier(level);\n"
        "            switch (level)\n"
        "            {\n"
        "                case EquipmentLevel.Eco:\n"
        "                    return $\"火力 ×{mult:0.00}\\n不購裝，存活 +30%\";\n"
        "                case EquipmentLevel.Light:\n"
        "                    return $\"火力 ×{mult:0.00}\\n衝鋒槍 + 小甲\";\n"
        "                case EquipmentLevel.Standard:\n"
        "                    return $\"火力 ×{mult:0.00}\\n步槍 + 全甲\";\n"
        "                case EquipmentLevel.FullBuy:\n"
        "                    return $\"火力 ×{mult:0.00}\\n步槍 + 道具 + 全甲\";\n"
        "                case EquipmentLevel.AwpFullBuy:\n"
        "                    return $\"火力 ×{mult:0.00}\\nAWP + 全配\\n需狙擊手\";\n"
        "                default:\n"
        "                    return $\"火力 ×{mult:0.00}\";\n"
        "            }\n"
        "        }\n\n        "
    )
    text = text[:start] + new_method + text[end:]

    SRC.write_text(text, encoding="utf-8")
    check = SRC.read_text(encoding="utf-8")
    if "EstimateTeamCardCost" not in check:
        raise RuntimeError("EstimateTeamCardCost missing after patch")
    if "全隊滿配意圖" not in check:
        raise RuntimeError("general card copy missing after patch")
    if "ApplyControlBarLayout(simulation)" not in check and "ApplyControlBarLayout(!simulation)" in check:
        raise RuntimeError("control bar polarity broken")
    print("patched", SRC)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
