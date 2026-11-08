using System;
using System.Collections.Generic;
using System.Linq;

namespace TacticaUniversalis.Domain
{
    [Serializable]
    public sealed class TacticalCardDefinition
    {
        public string id;
        public string displayName;
        public int commandCost;
        public string teamScope;
        public string archetype;
        public string[] tags;
        public string description;
        public string useWhen;
        public string avoidWhen;
        public string primaryZoneId;
        public int initiativeBias;
        public float risk;
        public float openingBonus;
        public float firepowerBonus;
        public float teamworkBonus;
        public float clutchBonus;
        public string specialRuleKey;
        public string targetPlayerId;
    }

    public sealed class TacticalDeckState
    {
        public List<string> DrawPile { get; } = new List<string>();
        public List<string> Hand { get; } = new List<string>();
        public List<string> DiscardPile { get; } = new List<string>();
        public List<string> Selected { get; } = new List<string>();
        public bool RoundDrawn { get; set; }
    }

    public sealed class TacticalSelection
    {
        public List<string> cardIds = new List<string>();
        public List<string> discardedIds = new List<string>();
    }

    public sealed class CounterModifiers
    {
        public float firepower;
        public float opening;
        public string outcome = "even";
        public string sourceCardId;
        public string targetCardId;
    }

    public static class CommandPointRules
    {
        public const int StartingPoints = 8;
        public const int HalfTimeRefill = 5;
        public const int OvertimeRefill = 3;
        /// <summary>CP granted to both teams at the start of each round after the first.</summary>
        public const int PerRoundRefill = 1;
        public const int MaximumPoints = 14;

        public static void Initialize(TeamRuntime team) => team.CommandPoints = StartingPoints;

        public static void Add(TeamRuntime team, int amount) =>
            team.CommandPoints = Math.Min(MaximumPoints, Math.Max(0, team.CommandPoints + amount));

        public static bool TrySpend(TeamRuntime team, int amount)
        {
            if (amount < 0 || team.CommandPoints < amount)
                return false;
            team.CommandPoints -= amount;
            return true;
        }
    }

    public static class TacticalDeckRules
    {
        public const int NormalDrawCount = 2;
        public const int MaximumHandSize = 7;
        public const int MaximumCardsPerRound = 3;

        public static void Initialize(
            TeamRuntime team,
            IEnumerable<TacticalCardDefinition> cards,
            IRandomSource random)
        {
            TacticalDeckState deck = team.TacticalDeck;
            deck.DrawPile.Clear();
            deck.Hand.Clear();
            deck.DiscardPile.Clear();
            deck.Selected.Clear();
            deck.RoundDrawn = false;
            deck.DrawPile.AddRange(cards
                .Where(x => x.teamScope == "universal" ||
                            string.Equals(x.teamScope, team.Definition.id, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.id));
            Shuffle(deck.DrawPile, random);
        }

        public static IReadOnlyList<string> Draw(TeamRuntime team, int count, IRandomSource random)
        {
            var drawn = new List<string>();
            for (int i = 0; i < count; i++)
            {
                RefillIfNeeded(team.TacticalDeck, random);
                if (team.TacticalDeck.DrawPile.Count == 0)
                    break;
                int last = team.TacticalDeck.DrawPile.Count - 1;
                string id = team.TacticalDeck.DrawPile[last];
                team.TacticalDeck.DrawPile.RemoveAt(last);
                team.TacticalDeck.Hand.Add(id);
                drawn.Add(id);
            }
            return drawn;
        }

        public static void BeginRound(TeamRuntime team, IRandomSource random)
        {
            team.TacticalDeck.Selected.Clear();
            if (!team.TacticalDeck.RoundDrawn)
            {
                Draw(team, NormalDrawCount, random);
                team.TacticalDeck.RoundDrawn = true;
            }
        }

        public static bool CanSelect(
            TeamRuntime team,
            string cardId,
            IReadOnlyDictionary<string, TacticalCardDefinition> cards)
        {
            if (team.TacticalDeck.Selected.Count >= MaximumCardsPerRound ||
                !team.TacticalDeck.Hand.Contains(cardId) ||
                !cards.TryGetValue(cardId, out TacticalCardDefinition card))
                return false;
            int total = team.TacticalDeck.Selected
                .Where(cards.ContainsKey)
                .Sum(id => cards[id].commandCost);
            return total + card.commandCost <= team.CommandPoints;
        }

        public static bool ToggleSelection(
            TeamRuntime team,
            string cardId,
            IReadOnlyDictionary<string, TacticalCardDefinition> cards)
        {
            if (team.TacticalDeck.Selected.Remove(cardId))
                return true;
            if (!CanSelect(team, cardId, cards))
                return false;
            team.TacticalDeck.Selected.Add(cardId);
            return true;
        }

        public static bool DiscardFromHand(TeamRuntime team, string cardId)
        {
            if (team.TacticalDeck.Selected.Contains(cardId) ||
                !team.TacticalDeck.Hand.Remove(cardId))
                return false;
            team.TacticalDeck.DiscardPile.Add(cardId);
            return true;
        }

        public static bool Commit(
            TeamRuntime team,
            IReadOnlyDictionary<string, TacticalCardDefinition> cards)
        {
            if (team.TacticalDeck.Selected.Count > MaximumCardsPerRound ||
                team.TacticalDeck.Hand.Count > MaximumHandSize)
                return false;
            int cost = team.TacticalDeck.Selected.Where(cards.ContainsKey).Sum(id => cards[id].commandCost);
            if (!CommandPointRules.TrySpend(team, cost))
                return false;
            foreach (string id in team.TacticalDeck.Selected.ToArray())
            {
                team.TacticalDeck.Hand.Remove(id);
                team.TacticalDeck.DiscardPile.Add(id);
            }
            team.TacticalDeck.RoundDrawn = false;
            return true;
        }

        private static void RefillIfNeeded(TacticalDeckState deck, IRandomSource random)
        {
            if (deck.DrawPile.Count > 0 || deck.DiscardPile.Count == 0)
                return;
            deck.DrawPile.AddRange(deck.DiscardPile);
            deck.DiscardPile.Clear();
            Shuffle(deck.DrawPile, random);
        }

        private static void Shuffle(List<string> values, IRandomSource random)
        {
            for (int i = values.Count - 1; i > 0; i--)
            {
                int j = random.Range(0, i + 1);
                string tmp = values[i];
                values[i] = values[j];
                values[j] = tmp;
            }
        }
    }

    public static class TacticalCounterResolver
    {
        private static readonly Dictionary<string, Dictionary<string, int>> Matrix =
            new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase)
            {
                ["balanced"] = Row(("discipline", 1), ("rush", -2), ("aggressive", -2)),
                ["rush"] = Row(("balanced", 2), ("deception", 2), ("info", -2), ("stack", -2), ("discipline", -2)),
                ["aggressive"] = Row(("balanced", 2), ("info", -2), ("stack", -2), ("discipline", -2)),
                ["info"] = Row(("rush", 2), ("aggressive", 2)),
                ["deception"] = Row(("stack", 2), ("rush", -2), ("discipline", -2)),
                ["stack"] = Row(("rush", 2), ("aggressive", 2), ("deception", -2)),
                ["discipline"] = Row(("rush", 2), ("aggressive", 2), ("deception", 2), ("balanced", -1)),
                ["tactical"] = Row()
            };

        public static CounterModifiers Resolve(
            IEnumerable<TacticalCardDefinition> ownCards,
            IEnumerable<TacticalCardDefinition> opponentCards)
        {
            CounterModifiers best = new CounterModifiers();
            foreach (TacticalCardDefinition own in ownCards ?? Enumerable.Empty<TacticalCardDefinition>())
            foreach (TacticalCardDefinition opponent in opponentCards ?? Enumerable.Empty<TacticalCardDefinition>())
            {
                int strength = Strength(own, opponent);
                if (strength <= StrengthValue(best.outcome))
                    continue;
                best = FromStrength(strength, own.id, opponent.id);
            }
            return best;
        }

        private static int Strength(TacticalCardDefinition own, TacticalCardDefinition opponent)
        {
            if (own.id == "gambit_hobbit_veteran" && opponent.archetype == "deception")
                return 2;
            if (own.id == "gambit_ax1le_lurk" && opponent.archetype == "stack")
                return 2;
            if ((own.id == "navi_discipline_defense" || own.id == "gambit_iron_wall") &&
                (opponent.archetype == "rush" || opponent.archetype == "aggressive" ||
                 opponent.archetype == "deception"))
                return 2;
            return Matrix.TryGetValue(own.archetype ?? string.Empty, out Dictionary<string, int> row) &&
                   row.TryGetValue(opponent.archetype ?? string.Empty, out int value)
                ? value
                : 0;
        }

        private static CounterModifiers FromStrength(int strength, string own, string opponent)
        {
            if (strength >= 2)
                return new CounterModifiers
                {
                    firepower = 0.10f, opening = 0.10f, outcome = "advantage",
                    sourceCardId = own, targetCardId = opponent
                };
            if (strength == 1)
                return new CounterModifiers
                {
                    firepower = 0.05f, outcome = "slight_advantage",
                    sourceCardId = own, targetCardId = opponent
                };
            return new CounterModifiers();
        }

        private static int StrengthValue(string outcome) =>
            outcome == "advantage" ? 2 : outcome == "slight_advantage" ? 1 : 0;

        private static Dictionary<string, int> Row(params (string key, int value)[] entries)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach ((string key, int value) entry in entries)
                result[entry.key] = entry.value;
            return result;
        }
    }

    public static class TacticalSpecialRules
    {
        public static void ApplyDeath(TeamRuntime team, TacticalPlan plan, string victimPlayerId)
        {
            if (victimPlayerId == "b1t" && plan.specialRuleKeys.Contains("b1t_death_penalty"))
                team.NextRoundFirepowerPenalty = Math.Max(team.NextRoundFirepowerPenalty, 0.05f);
            if (victimPlayerId == "s1mple" && plan.specialRuleKeys.Contains("s1mple_anchor"))
                plan.firepowerBonus -= 0.08f;
            if (victimPlayerId == "nafany" && plan.specialRuleKeys.Contains("nafany_igl_risk"))
                plan.teamworkBonus = 0f;
        }
    }

    /// <summary>
    /// 聚焦卡：回答「這一回合把球傳給誰」。疊加在戰術卡全隊基底之上。
    /// teamScope 決定 UI 顏色：universal=鋼藍通用；隊伍 id=銅色專屬。
    /// </summary>
    [Serializable]
    public sealed class FocusCardDefinition
    {
        public string id;
        public string displayName;
        public int commandCost;
        public string teamScope;
        public string[] tags;
        public string description;
        public string useWhen;
        public string avoidWhen;
        /// <summary>auto | manual | duo | team</summary>
        public string targetMode;
        public string[] targetPlayerIds;
        public float allAttributeBonus;
        public float aimBonus;
        public float reactionBonus;
        public float awarenessBonus;
        public float utilityBonus;
        public float teamworkBonus;
        public float communicationBonus;
        public float openingBonus;
        public float clutchBonus;
        public float mentalityRestore;
        /// <summary>雙人同區時額外配合加成。</summary>
        public float duoTeamworkBonus;
        public string specialRuleKey;
        /// <summary>round | cross_round</summary>
        public string duration;
        /// <summary>建議疊加的戰術卡 id（協同表用）。</summary>
        public string[] synergyTacticalCardIds;
        public string synergyNote;
        public int maxUsesPerMatch;
    }

    /// <summary>
    /// 教練卡：僅在暫停期間使用。teamScope 決定 UI 顏色（同戰術卡）。
    /// </summary>
    [Serializable]
    public sealed class CoachCardDefinition
    {
        public string id;
        public string displayName;
        public int commandCost;
        public string teamScope;
        public string[] tags;
        public string description;
        public string useWhen;
        public string avoidWhen;
        /// <summary>固定為 timeout。</summary>
        public string timing;
        public bool isBaseCoach;
        /// <summary>mentality | stamina | form | shout | tactical_swap | cleanse | reveal | hybrid</summary>
        public string effectType;
        /// <summary>auto | manual | team</summary>
        public string targetMode;
        public float mentalityDelta;
        public float staminaDelta;
        public float formDelta;
        public float teamworkBonus;
        public float sideEffectFormDelta;
        public string specialRuleKey;
        public int maxUsesPerMatch;
    }

    /// <summary>手感（Form）完整數值規則。執行層尚未全部接入，以此為準。</summary>
    public static class FormRules
    {
        public const float MatchMin = 10f;
        public const float MatchMax = 95f;
        public const float MatchStartMin = 50f;
        public const float MatchStartMax = 70f;
        public const float CombatBaseline = 50f;

        public const float KillGainMin = 5f;
        public const float KillGainMax = 10f;
        public const float MultiKillBonusPerExtra = 3f;
        public const float ClutchWinGain = 15f;

        public const float DeathLossMin = 5f;
        public const float DeathLossMax = 10f;
        public const float OpenedDeathLoss = 15f;
        public const float EcoRoundTeamLoss = 5f;
        public const float TimeoutTeamLoss = 5f;

        public const float AimReactionBonusCap = 15f;
        public const float AimReactionPenaltyFloor = -10f;

        public static float Clamp(float form) =>
            Math.Max(MatchMin, Math.Min(MatchMax, form));

        /// <summary>手感相對 50 的差值，夾在 −10..+15，供瞄準／反應加減。</summary>
        public static float AimReactionDelta(float form)
        {
            float delta = form - CombatBaseline;
            if (delta > AimReactionBonusCap)
                return AimReactionBonusCap;
            if (delta < AimReactionPenaltyFloor)
                return AimReactionPenaltyFloor;
            return delta;
        }
    }

    /// <summary>暫停系統規則常數。執行層尚未全部接入，以此為準。</summary>
    public static class TimeoutRules
    {
        public const int RegularTimeoutsPerTeam = 3;
        public const int OvertimeTimeoutsPerTeam = 1;
        public const float BothTeamsFormPenalty = FormRules.TimeoutTeamLoss;
    }

    public static class FocusSpecialRules
    {
        public static void ApplyDeath(RoundSimulationRequest request, PlayerRuntime victim)
        {
            if (victim == null || request == null)
                return;
            TeamRuntime team = ResolveTeam(request, victim.Definition.id);
            if (team == null)
                return;
            if (!string.Equals(victim.Definition.id, "s1mple", StringComparison.OrdinalIgnoreCase))
                return;
            if (!team.Players.Any(p => p.States.Contains("focus:s1mple_focus_death_penalty")))
                return;
            foreach (PlayerRuntime mate in team.Players)
            {
                mate.Mentality = Math.Max(0f, Math.Min(100f, mate.Mentality - 12f));
                mate.Form = FormRules.Clamp(mate.Form - 10f);
            }
        }

        private static TeamRuntime ResolveTeam(RoundSimulationRequest request, string playerId)
        {
            if (request.attackers.Players.Any(p =>
                    string.Equals(p.Definition.id, playerId, StringComparison.OrdinalIgnoreCase)))
                return request.attackers;
            if (request.defenders.Players.Any(p =>
                    string.Equals(p.Definition.id, playerId, StringComparison.OrdinalIgnoreCase)))
                return request.defenders;
            return null;
        }
    }
}
