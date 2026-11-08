using System;
using System.Collections.Generic;
using System.Linq;
using TacticaUniversalis.Domain;

namespace TacticaUniversalis.Application
{
    public static class TimeoutCoachRules
    {
        /// <summary>Max coach cards that can be played in a single timeout window.</summary>
        public const int MaximumCoachCardsPerTimeout = 3;

        public static void ApplyFormPenaltyToBoth(MatchState state)
        {
            foreach (PlayerRuntime player in state.TeamA.Players.Concat(state.TeamB.Players))
                player.Form = FormRules.Clamp(player.Form - TimeoutRules.BothTeamsFormPenalty);
        }

        public static bool TryCallTimeout(TeamRuntime caller, MatchState state)
        {
            if (caller == null || caller.TimeoutsRemaining <= 0)
                return false;
            caller.TimeoutsRemaining--;
            caller.CoachUsedThisTimeout.Clear();
            caller.PaidCoachUsedThisTimeout = false;
            TeamRuntime other = caller == state.TeamA ? state.TeamB : state.TeamA;
            other.CoachUsedThisTimeout.Clear();
            other.PaidCoachUsedThisTimeout = false;
            ApplyFormPenaltyToBoth(state);
            return true;
        }

        public static bool CanUseCoach(TeamRuntime team, CoachCardDefinition card)
        {
            if (team == null || card == null)
                return false;
            if (!string.Equals(card.timing, "timeout", StringComparison.OrdinalIgnoreCase))
                return false;
            if (card.teamScope != "universal" &&
                !string.Equals(card.teamScope, team.Definition.id, StringComparison.OrdinalIgnoreCase))
                return false;
            if (team.CoachUsedThisTimeout.Count >= MaximumCoachCardsPerTimeout)
                return false;
            if (card.isBaseCoach)
                return !team.CoachUsedThisTimeout.Contains(card.id);
            if (card.maxUsesPerMatch > 0 &&
                team.SpecialUses.TryGetValue("coach:" + card.id, out int uses) &&
                uses >= card.maxUsesPerMatch)
                return false;
            return team.CommandPoints >= card.commandCost;
        }

        public static bool TryApplyCoach(
            TeamRuntime team,
            CoachCardDefinition card,
            string targetPlayerId,
            MatchState state)
        {
            if (!CanUseCoach(team, card))
                return false;
            if (!card.isBaseCoach && !CommandPointRules.TrySpend(team, card.commandCost))
                return false;

            IEnumerable<PlayerRuntime> targets = ResolveTargets(team, card.targetMode, targetPlayerId);
            foreach (PlayerRuntime player in targets)
            {
                if (card.mentalityDelta != 0f)
                    player.Mentality = Clamp100(player.Mentality + card.mentalityDelta);
                if (card.staminaDelta != 0f)
                    player.Stamina = Clamp100(player.Stamina + card.staminaDelta);
                if (card.formDelta != 0f)
                    player.Form = FormRules.Clamp(player.Form + card.formDelta);
                if (card.sideEffectFormDelta != 0f)
                    player.Form = FormRules.Clamp(player.Form + card.sideEffectFormDelta);
                if (card.specialRuleKey == "groove_cleanse_teamwork" ||
                    card.effectType == "cleanse")
                {
                    List<string> remove = player.States
                        .Where(s =>
                            s.StartsWith("debuff:", StringComparison.OrdinalIgnoreCase) ||
                            s.StartsWith("neg:", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    foreach (string s in remove)
                        player.States.Remove(s);
                    player.SpecialState = false;
                }
            }

            if (card.teamworkBonus > 0f)
                team.SpecialUses["coach_teamwork_bonus"] =
                    (int)Math.Round(card.teamworkBonus * 1000f);

            team.CoachUsedThisTimeout.Add(card.id);
            if (!card.isBaseCoach && card.maxUsesPerMatch > 0)
            {
                string key = "coach:" + card.id;
                team.SpecialUses.TryGetValue(key, out int uses);
                team.SpecialUses[key] = uses + 1;
            }

            // Opponent may act on caller timeout; state retained for future UI.
            _ = state;
            return true;
        }

        public static void GrantOvertimeTimeouts(MatchState state)
        {
            state.TeamA.TimeoutsRemaining += TimeoutRules.OvertimeTimeoutsPerTeam;
            state.TeamB.TimeoutsRemaining += TimeoutRules.OvertimeTimeoutsPerTeam;
        }

        private static IEnumerable<PlayerRuntime> ResolveTargets(
            TeamRuntime team, string targetMode, string targetPlayerId)
        {
            if (string.Equals(targetMode, "team", StringComparison.OrdinalIgnoreCase))
                return team.Players;
            PlayerRuntime target = team.Players.FirstOrDefault(p =>
                string.Equals(p.Definition.id, targetPlayerId, StringComparison.OrdinalIgnoreCase));
            return target != null ? new[] { target } : Array.Empty<PlayerRuntime>();
        }

        private static float Clamp100(float value) => Math.Max(0f, Math.Min(100f, value));
    }

    public static class FocusCardRules
    {
        public static IEnumerable<FocusCardDefinition> CatalogFor(
            TeamRuntime team, IReadOnlyDictionary<string, FocusCardDefinition> all) =>
            all.Values.Where(c =>
                string.Equals(c.teamScope, team.Definition.id, StringComparison.OrdinalIgnoreCase));

        public static int SelectedTacticalCost(
            TeamRuntime team, IReadOnlyDictionary<string, TacticalCardDefinition> tactical) =>
            team.TacticalDeck.Selected
                .Where(tactical.ContainsKey)
                .Sum(id => tactical[id].commandCost);

        public static bool CanSelect(
            TeamRuntime team,
            FocusCardDefinition card,
            IReadOnlyDictionary<string, TacticalCardDefinition> tactical,
            bool tacticalAlreadyCommitted = false)
        {
            if (team == null || card == null)
                return false;
            if (!string.Equals(card.teamScope, team.Definition.id, StringComparison.OrdinalIgnoreCase))
                return false;
            int spent = tacticalAlreadyCommitted ? 0 : SelectedTacticalCost(team, tactical);
            return spent + card.commandCost <= team.CommandPoints;
        }

        public static float UseScale(TeamRuntime team, FocusCardDefinition card)
        {
            if (card.specialRuleKey != "b1t_growth_diminishing")
                return 1f;
            team.SpecialUses.TryGetValue("focus_uses:" + card.id, out int uses);
            int n = uses + 1;
            return Math.Max(0.4f, 1f - 0.2f * (n - 1));
        }

        public static bool TryCommit(
            TeamRuntime team,
            FocusCardDefinition card,
            string manualTargetId,
            TacticalPlan plan,
            IReadOnlyDictionary<string, TacticalCardDefinition> tactical)
        {
            if (card == null)
                return true;
            if (!CanSelect(team, card, tactical, tacticalAlreadyCommitted: true))
                return false;
            if (!CommandPointRules.TrySpend(team, card.commandCost))
                return false;

            float scale = UseScale(team, card);
            team.SpecialUses.TryGetValue("focus_uses:" + card.id, out int uses);
            team.SpecialUses["focus_uses:" + card.id] = uses + 1;

            IEnumerable<PlayerRuntime> targets = ResolveFocusTargets(team, card, manualTargetId);
            foreach (PlayerRuntime player in targets)
                ApplyPlayerBonuses(player, card, scale);

            if (card.teamworkBonus > 0f && string.Equals(card.targetMode, "team", StringComparison.OrdinalIgnoreCase))
                plan.teamworkBonus = Math.Min(0.30f, plan.teamworkBonus + card.teamworkBonus * scale);
            if (card.openingBonus > 0f && string.Equals(card.targetMode, "team", StringComparison.OrdinalIgnoreCase))
                plan.openingBonus = Math.Min(0.40f, plan.openingBonus + card.openingBonus * scale);
            if (card.clutchBonus > 0f && string.Equals(card.targetMode, "team", StringComparison.OrdinalIgnoreCase))
                plan.clutchBonus = Math.Min(0.30f, plan.clutchBonus + card.clutchBonus * scale);

            if (card.specialRuleKey == "gemini_same_zone_link" && card.duoTeamworkBonus > 0f)
            {
                PlayerRuntime a = team.Players.FirstOrDefault(p => p.Definition.id == "s1mple");
                PlayerRuntime b = team.Players.FirstOrDefault(p => p.Definition.id == "electronic");
                if (a != null && b != null &&
                    string.Equals(a.ZoneId, b.ZoneId, StringComparison.OrdinalIgnoreCase))
                    plan.teamworkBonus = Math.Min(0.35f, plan.teamworkBonus + card.duoTeamworkBonus * scale);
                else
                {
                    // Mark for mid-round same-zone pulse via state flag.
                    a?.States.Add("focus:gemini_link");
                    b?.States.Add("focus:gemini_link");
                    team.SpecialUses["gemini_duo_bonus"] = (int)Math.Round(card.duoTeamworkBonus * scale * 1000f);
                }
            }

            if (!string.IsNullOrEmpty(card.specialRuleKey))
            {
                foreach (PlayerRuntime player in targets)
                    player.States.Add("focus:" + card.specialRuleKey);
            }

            if (card.specialRuleKey == "perfecto_cleanse_clutch")
            {
                foreach (PlayerRuntime player in targets)
                {
                    List<string> remove = player.States
                        .Where(s => s.StartsWith("debuff:", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    foreach (string s in remove)
                        player.States.Remove(s);
                    player.Mentality = Math.Max(0f, Math.Min(100f,
                        player.Mentality + card.mentalityRestore * scale));
                }
            }
            else if (card.mentalityRestore > 0f)
            {
                foreach (PlayerRuntime player in targets)
                    player.Mentality = Math.Max(0f, Math.Min(100f,
                        player.Mentality + card.mentalityRestore * scale));
            }

            if (card.specialRuleKey == "sh1ro_fog_of_war")
                team.SpecialUses["fog:sh1ro"] = 1;

            team.SelectedFocusCardId = card.id;
            team.SelectedFocusTargetId = manualTargetId ?? string.Empty;
            return true;
        }

        public static string ChooseAi(
            TeamRuntime team,
            MatchState state,
            IReadOnlyDictionary<string, FocusCardDefinition> catalog,
            IReadOnlyDictionary<string, TacticalCardDefinition> tactical,
            IRandomSource random)
        {
            var options = CatalogFor(team, catalog)
                .Where(c => CanSelect(team, c, tactical, tacticalAlreadyCommitted: true))
                .OrderByDescending(c => c.commandCost + random.Next01() * 0.5f)
                .ToList();
            if (options.Count == 0)
                return null;
            // Prefer spending leftover CP; sometimes skip.
            if (random.Next01() < 0.25f)
                return null;
            FocusCardDefinition pick = options[0];
            team.SelectedFocusCardId = pick.id;
            if (string.Equals(pick.targetMode, "manual", StringComparison.OrdinalIgnoreCase) &&
                pick.targetPlayerIds != null && pick.targetPlayerIds.Length > 0)
                team.SelectedFocusTargetId = pick.targetPlayerIds[random.Range(0, pick.targetPlayerIds.Length)];
            else
                team.SelectedFocusTargetId = pick.targetPlayerIds?.FirstOrDefault() ?? string.Empty;
            return pick.id;
        }

        private static IEnumerable<PlayerRuntime> ResolveFocusTargets(
            TeamRuntime team, FocusCardDefinition card, string manualTargetId)
        {
            if (string.Equals(card.targetMode, "team", StringComparison.OrdinalIgnoreCase))
                return team.Players;
            if (string.Equals(card.targetMode, "manual", StringComparison.OrdinalIgnoreCase))
            {
                PlayerRuntime manual = team.Players.FirstOrDefault(p =>
                    string.Equals(p.Definition.id, manualTargetId, StringComparison.OrdinalIgnoreCase));
                if (manual != null)
                    return new[] { manual };
                if (card.targetPlayerIds != null)
                {
                    foreach (string id in card.targetPlayerIds)
                    {
                        PlayerRuntime p = team.Players.FirstOrDefault(x =>
                            string.Equals(x.Definition.id, id, StringComparison.OrdinalIgnoreCase));
                        if (p != null)
                            return new[] { p };
                    }
                }
                return Array.Empty<PlayerRuntime>();
            }
            return (card.targetPlayerIds ?? Array.Empty<string>())
                .Select(id => team.Players.FirstOrDefault(p =>
                    string.Equals(p.Definition.id, id, StringComparison.OrdinalIgnoreCase)))
                .Where(p => p != null);
        }

        private static void ApplyPlayerBonuses(PlayerRuntime player, FocusCardDefinition card, float scale)
        {
            if (card.allAttributeBonus > 0f)
                player.FocusAllMult *= 1f + card.allAttributeBonus * scale;
            player.FocusAim += AttrPoints(card.aimBonus, scale);
            player.FocusReaction += AttrPoints(card.reactionBonus, scale);
            player.FocusAwareness += AttrPoints(card.awarenessBonus, scale);
            player.FocusUtility += AttrPoints(card.utilityBonus, scale);
            player.FocusTeamwork += AttrPoints(card.teamworkBonus, scale);
            player.FocusCommunication += AttrPoints(card.communicationBonus, scale);
            player.FocusOpening += AttrPoints(card.openingBonus, scale);
            player.FocusClutch += AttrPoints(card.clutchBonus, scale);
            if (card.specialRuleKey == "interz_support_aura")
                player.FocusTeamwork += AttrPoints(card.teamworkBonus, scale); // 翻倍配合
        }

        private static float AttrPoints(float fractionBonus, float scale) =>
            fractionBonus <= 0f ? 0f : 80f * fractionBonus * scale;
    }
}
