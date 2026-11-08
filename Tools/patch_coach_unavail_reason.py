# -*- coding: utf-8 -*-
from pathlib import Path

path = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"
text = path.read_text(encoding="utf-8")

old = """        private static string CoachUnavailableReason(TeamRuntime team, CoachCardDefinition card)
        {
            if (card == null || team == null)
                return "????";
            if (card.isBaseCoach && team.CoachUsedThisTimeout.Contains(card.id))
                return "??????";
            if (!card.isBaseCoach && team.PaidCoachUsedThisTimeout)
                return "???????????";
            if (card.maxUsesPerMatch > 0 &&
                team.SpecialUses.TryGetValue("coach:" + card.id, out int uses) &&
                uses >= card.maxUsesPerMatch)
                return "?????????";
            if (team.CommandPoints < card.commandCost)
                return $"??????? {card.commandCost}?";
            return card.description;
        }"""

new = """        private static string CoachUnavailableReason(TeamRuntime team, CoachCardDefinition card)
        {
            if (card == null || team == null)
                return "無法使用";
            if (team.CoachUsedThisTimeout.Count >= TimeoutCoachRules.MaximumCoachCardsPerTimeout)
                return $"本暫停最多 {TimeoutCoachRules.MaximumCoachCardsPerTimeout} 張教練卡";
            if (card.isBaseCoach && team.CoachUsedThisTimeout.Contains(card.id))
                return "本暫停已套用";
            if (card.maxUsesPerMatch > 0 &&
                team.SpecialUses.TryGetValue("coach:" + card.id, out int uses) &&
                uses >= card.maxUsesPerMatch)
                return "已達本場次使用上限";
            if (team.CommandPoints < card.commandCost)
                return $"指揮點不足（需 {card.commandCost}）";
            return card.description;
        }"""

if old not in text:
    raise SystemExit("CoachUnavailableReason block not found (encoding drift?)")
path.write_text(text.replace(old, new, 1), encoding="utf-8", newline="\n")
print("Patched CoachUnavailableReason")
