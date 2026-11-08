# -*- coding: utf-8 -*-
"""Re-apply coach 3-card timeout UI on QuickMatchDemoController (UTF-8 safe)."""
from pathlib import Path

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def main() -> None:
    text = TARGET.read_text(encoding="utf-8")

    old_sub = (
        '            _timeoutSubtitle.text =\n'
        '                $"剩餘暫停 {team.TimeoutsRemaining} · CP {team.CommandPoints}（已選佔 {selectedCost}） · " +\n'
        '                $"已選 {_selectedCoachIds.Count} · 全體手感 −5";'
    )
    new_sub = (
        '            _timeoutSubtitle.text =\n'
        '                $"剩餘暫停 {team.TimeoutsRemaining} · CP {team.CommandPoints}（已選佔 {selectedCost}） · " +\n'
        '                $"已選 {_selectedCoachIds.Count}/{TimeoutCoachRules.MaximumCoachCardsPerTimeout} · 全體手感 −5";'
    )
    if old_sub not in text:
        if new_sub.split("\n")[1] in text:
            print("subtitle already patched")
        else:
            raise SystemExit("timeout subtitle block not found")
    else:
        text = text.replace(old_sub, new_sub, 1)

    old_can = """        private bool CanSelectCoachCard(TeamRuntime team, CoachCardDefinition card)
        {
            if (!TimeoutCoachRules.CanUseCoach(team, card))
                return false;
            if (card.isBaseCoach)
                return true;

            int otherPaidCost = 0;
            foreach (string id in _selectedCoachIds)
            {
                if (!_content.CoachCards.TryGetValue(id, out CoachCardDefinition selected))
                    continue;
                if (!selected.isBaseCoach)
                    otherPaidCost += selected.commandCost;
            }
            return team.CommandPoints - otherPaidCost >= card.commandCost;
        }"""
    new_can = """        private bool CanSelectCoachCard(TeamRuntime team, CoachCardDefinition card)
        {
            if (_selectedCoachIds.Count >= TimeoutCoachRules.MaximumCoachCardsPerTimeout)
                return false;
            if (!TimeoutCoachRules.CanUseCoach(team, card))
                return false;

            int otherPaidCost = 0;
            foreach (string id in _selectedCoachIds)
            {
                if (!_content.CoachCards.TryGetValue(id, out CoachCardDefinition selected))
                    continue;
                if (!selected.isBaseCoach)
                    otherPaidCost += selected.commandCost;
            }
            if (card.isBaseCoach)
                return true;
            return team.CommandPoints - otherPaidCost >= card.commandCost;
        }"""
    if old_can in text:
        text = text.replace(old_can, new_can, 1)
    elif "MaximumCoachCardsPerTimeout" in text and "_selectedCoachIds.Count >=" in text:
        print("CanSelectCoachCard already patched")
    else:
        raise SystemExit("CanSelectCoachCard block not found")

    old_reason = """        private static string CoachUnavailableReason(TeamRuntime team, CoachCardDefinition card)
        {
            if (card == null || team == null)
                return "無法使用";
            if (card.isBaseCoach && team.CoachUsedThisTimeout.Contains(card.id))
                return "本暫停已套用";
            if (!card.isBaseCoach && team.PaidCoachUsedThisTimeout)
                return "本暫停已使用付費教練卡";
            if (card.maxUsesPerMatch > 0 &&
                team.SpecialUses.TryGetValue("coach:" + card.id, out int uses) &&
                uses >= card.maxUsesPerMatch)
                return "已達本場次使用上限";
            if (team.CommandPoints < card.commandCost)
                return $"指揮點不足（需 {card.commandCost}）";
            return card.description;
        }"""
    new_reason = """        private static string CoachUnavailableReason(TeamRuntime team, CoachCardDefinition card)
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
    if old_reason in text:
        text = text.replace(old_reason, new_reason, 1)
    elif "本暫停最多" in text:
        print("CoachUnavailableReason already patched")
    else:
        raise SystemExit("CoachUnavailableReason block not found")

    old_click = """            if (!CanSelectCoachCard(team, card))
            {
                RefreshTimeoutPicker();
                return;
            }

            if (!card.isBaseCoach)
            {
                List<string> remove = _selectedCoachIds
                    .Where(selectedId =>
                        _content.CoachCards.TryGetValue(selectedId, out CoachCardDefinition selected) &&
                        !selected.isBaseCoach)
                    .ToList();
                foreach (string selectedId in remove)
                {
                    _selectedCoachIds.Remove(selectedId);
                    _coachPickTargets.Remove(selectedId);
                }
            }

            _selectedCoachIds.Add(id);"""
    new_click = """            if (!CanSelectCoachCard(team, card))
            {
                RefreshTimeoutPicker();
                return;
            }

            if (_selectedCoachIds.Count >= TimeoutCoachRules.MaximumCoachCardsPerTimeout)
            {
                RefreshTimeoutPicker();
                return;
            }

            _selectedCoachIds.Add(id);"""
    if old_click in text:
        text = text.replace(old_click, new_click, 1)
    elif "_selectedCoachIds.Count >= TimeoutCoachRules.MaximumCoachCardsPerTimeout" in text:
        print("OnCoachCardClicked already patched")
    else:
        raise SystemExit("OnCoachCardClicked block not found")

    TARGET.write_text(text, encoding="utf-8", newline="\n")
    print("Patched coach timeout UI in", TARGET)


if __name__ == "__main__":
    main()
