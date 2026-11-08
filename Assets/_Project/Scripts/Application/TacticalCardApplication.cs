using System;
using System.Collections.Generic;
using System.Linq;
using TacticaUniversalis.Domain;

namespace TacticaUniversalis.Application
{
    public static class TacticalPlanFactory
    {
        public static TacticalPlan ApplyCards(
            TacticalPlan plan,
            TeamRuntime team,
            IEnumerable<TacticalCardDefinition> cards)
        {
            TacticalCardDefinition[] selected = (cards ?? Enumerable.Empty<TacticalCardDefinition>()).ToArray();
            if (selected.Length == 0)
                return plan;

            TacticalCardDefinition primary = selected
                .OrderByDescending(x => x.commandCost)
                .ThenByDescending(x => x.initiativeBias)
                .First();
            plan.name = string.Join(" + ", selected.Select(x => x.displayName));
            plan.primaryZoneId = primary.primaryZoneId;
            plan.tacticalCardIds = selected.Select(x => x.id).ToArray();
            plan.specialRuleKeys = selected.Where(x => !string.IsNullOrEmpty(x.specialRuleKey))
                .Select(x => x.specialRuleKey).ToArray();
            plan.targetPlayerId = primary.targetPlayerId;
            plan.openingBonus = Clamp(selected.Sum(x => Effective(x, plan.equipmentLevel, x.openingBonus)), 0f, 0.30f);
            plan.firepowerBonus = Clamp(selected.Sum(x => Effective(x, plan.equipmentLevel, x.firepowerBonus)), 0f, 0.20f);
            plan.teamworkBonus = Clamp(
                plan.teamworkBonus + selected.Sum(x => Effective(x, plan.equipmentLevel, x.teamworkBonus)),
                0f, 0.15f);
            plan.clutchBonus = Clamp(selected.Sum(x => Effective(x, plan.equipmentLevel, x.clutchBonus)), 0f, 0.15f);
            plan.risk = Clamp(selected.Max(x => x.risk), 0f, 1f);
            plan.initiativeBias = selected.Max(x => x.initiativeBias);
            plan.aggression = Clamp(0.25f + plan.initiativeBias * 0.25f + plan.risk * 0.20f, 0f, 1f);
            plan.plantPriority = Clamp(plan.plantPriority + (primary.archetype == "deception" ? 0.10f : 0f), 0f, 1f);

            if (plan.specialRuleKeys.Contains("boombl4_draw_four"))
                plan.firepowerBonus -= 0.05f;
            if (team.NextRoundFirepowerPenalty > 0f)
            {
                plan.firepowerBonus -= team.NextRoundFirepowerPenalty;
                team.NextRoundFirepowerPenalty = 0f;
            }
            return plan;
        }

        public static void ApplyCounters(
            TacticalPlan plan,
            IEnumerable<TacticalCardDefinition> own,
            IEnumerable<TacticalCardDefinition> opponent)
        {
            plan.counterModifiers = TacticalCounterResolver.Resolve(own, opponent);
        }

        public static void ApplyHobbitSuppression(
            TacticalPlan plan,
            IEnumerable<TacticalCardDefinition> opponentCards)
        {
            if (!(opponentCards ?? Enumerable.Empty<TacticalCardDefinition>())
                .Any(x => x.specialRuleKey == "hobbit_reveal_limited"))
                return;
            plan.openingBonus *= 0.60f;
            plan.firepowerBonus *= 0.60f;
            plan.teamworkBonus *= 0.60f;
            plan.clutchBonus *= 0.60f;
        }

        private static float Effective(TacticalCardDefinition card, EquipmentLevel equipment, float value) =>
            card.specialRuleKey == "eco_only" &&
            equipment != EquipmentLevel.Eco && equipment != EquipmentLevel.HalfBuy
                ? 0f
                : value;

        private static float Clamp(float value, float min, float max) =>
            Math.Max(min, Math.Min(max, value));
    }

    public sealed class TacticalCardAi
    {
        public IReadOnlyList<string> Choose(
            TeamRuntime team,
            MatchState state,
            IReadOnlyDictionary<string, TacticalCardDefinition> catalog,
            IRandomSource random)
        {
            var candidates = team.TacticalDeck.Hand
                .Where(catalog.ContainsKey)
                .Select(id => catalog[id])
                .Where(card => IsUsable(card, team))
                .OrderByDescending(card => Score(card, team, state) + random.Next01() * 0.05f)
                .ToList();
            var selected = new List<string>();
            int spent = 0;
            foreach (TacticalCardDefinition card in candidates)
            {
                if (selected.Count >= TacticalDeckRules.MaximumCardsPerRound ||
                    spent + card.commandCost > team.CommandPoints)
                    continue;
                selected.Add(card.id);
                spent += card.commandCost;
                if (selected.Count >= 2 && spent >= Math.Max(1, team.CommandPoints - 1))
                    break;
            }
            if (selected.Count == 0)
            {
                TacticalCardDefinition free = candidates.FirstOrDefault(x => x.commandCost == 0);
                if (free != null)
                    selected.Add(free.id);
            }
            return selected;
        }

        public void DiscardOverflow(
            TeamRuntime team,
            IReadOnlyDictionary<string, TacticalCardDefinition> catalog)
        {
            while (team.TacticalDeck.Hand.Count > TacticalDeckRules.MaximumHandSize)
            {
                string discard = team.TacticalDeck.Hand
                    .Where(x => !team.TacticalDeck.Selected.Contains(x))
                    .OrderBy(x => catalog.TryGetValue(x, out TacticalCardDefinition card) ? card.commandCost : 0)
                    .FirstOrDefault();
                if (discard == null || !TacticalDeckRules.DiscardFromHand(team, discard))
                    break;
            }
        }

        private static bool IsUsable(TacticalCardDefinition card, TeamRuntime team)
        {
            if (card.specialRuleKey == "eco_only" &&
                team.Equipment != EquipmentLevel.Eco && team.Equipment != EquipmentLevel.HalfBuy)
                return false;
            if (card.specialRuleKey == "hobbit_reveal_limited" &&
                team.SpecialUses.TryGetValue(card.id, out int uses) && uses >= 2)
                return false;
            return true;
        }

        private static float Score(TacticalCardDefinition card, TeamRuntime team, MatchState state)
        {
            float score = card.openingBonus + card.firepowerBonus + card.teamworkBonus + card.clutchBonus;
            score -= card.commandCost * 0.025f;
            if (team.Side == Side.CounterTerrorist &&
                (card.archetype == "discipline" || card.archetype == "stack"))
                score += 0.12f;
            if (team.Side == Side.Terrorist &&
                (card.archetype == "rush" || card.archetype == "aggressive" || card.archetype == "deception"))
                score += 0.10f;
            if (state.ScoreAgainst(team.Definition.id) - state.ScoreFor(team.Definition.id) >= 3)
                score += card.risk * 0.12f;
            if (team.Equipment == EquipmentLevel.Eco && card.specialRuleKey == "eco_only")
                score += 0.30f;
            return score;
        }
    }
}
