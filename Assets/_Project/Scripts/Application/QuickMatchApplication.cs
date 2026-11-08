using System;
using System.Collections.Generic;
using System.Linq;
using TacticaUniversalis.Domain;

namespace TacticaUniversalis.Application
{
    public sealed class RuleBasedTacticalPlanner : ITacticalPlanner
    {
        private readonly string _defaultZone;

        public RuleBasedTacticalPlanner(string defaultZone = "mid")
        {
            _defaultZone = string.IsNullOrEmpty(defaultZone) ? "mid" : defaultZone;
        }

        public TacticalPlan CreatePlan(MatchState state, TeamRuntime team, Side side, IRandomSource random)
        {
            bool attacking = side == Side.Terrorist;
            bool hasAwper = team.Players.Any(x => x.Definition.primaryRole == "Awper");
            string zone = _defaultZone;

            var plan = new TacticalPlan
            {
                name = "Default Map Control",
                primaryZoneId = zone,
                aggression = attacking ? 0.55f : 0.40f,
                utilityCommitment = 0.70f,
                teamworkBonus = Math.Max(0f, (float)team.Players.Average(x => x.Definition.attributes.teamwork) - 75f) / 100f,
                openingBonus = 0f,
                plantPriority = attacking ? 0.75f : 0f,
                equipmentLevel = EconomyRules.BestAffordable(team.Money, hasAwper)
            };

            foreach (PlayerRuntime player in team.Players)
                plan.playerAssignments[player.Definition.id] = AssignmentFor(player.Definition.primaryRole, zone, attacking);
            return plan;
        }

        private static string AssignmentFor(string role, string zone, bool attacking)
        {
            if (role == "Entry") return attacking ? "first_contact:" + zone : "aggressive_hold:" + zone;
            if (role == "Awper") return "long_sightline:" + zone;
            if (role == "Lurker") return attacking ? "late_flank:" + zone : "rotate:ct_spawn";
            if (role == "Support") return "utility_support:" + zone;
            if (role == "Igl") return "command:" + zone;
            return "trade:" + zone;
        }
    }

    /// <summary>
    /// Round-by-round match session. Opponent always auto-picks BestAffordable;
    /// the player team can override with a chosen economy card.
    /// </summary>
    public sealed class InteractiveQuickMatchSession
    {
        private readonly ContentDatabase _content;
        private readonly MapDefinition _map;
        private readonly ITacticalPlanner _planner;
        private readonly IRoundSimulator _roundSimulator;
        private readonly SeededRandom _random;
        private readonly MatchState _state;
        private readonly MatchResult _output;
        private readonly TacticalCardAi _tacticalAi = new TacticalCardAi();
        private readonly BattleSimMode _battleSimMode;
        private readonly WeaponCatalog _weaponCatalog;
        private int _globalSequence;
        private bool _roundPrepared;
        private bool _finished;

        public InteractiveQuickMatchSession(
            ContentDatabase content,
            int seed,
            ITacticalPlanner planner,
            IRoundSimulator roundSimulator,
            string teamAId = "navi_2021",
            string teamBId = "gambit_2021",
            string mapId = null,
            BattleSimMode battleSimMode = BattleSimMode.Quick,
            WeaponCatalog weaponCatalog = null)
        {
            _content = content;
            _map = content.GetMap(mapId);
            _planner = planner;
            _roundSimulator = roundSimulator;
            _battleSimMode = battleSimMode;
            _weaponCatalog = weaponCatalog ?? WeaponCatalog.CreateDefault();
            _random = new SeededRandom(seed);
            _state = new MatchState(
                CreateTeam(content, teamAId, _random),
                CreateTeam(content, teamBId, _random));
            if (UsesItemizedEconomy)
                ItemizedEconomyRules.InitializeMatch(_state);
            CommandPointRules.Initialize(_state.TeamA);
            CommandPointRules.Initialize(_state.TeamB);
            _state.TeamA.TimeoutsRemaining = TimeoutRules.RegularTimeoutsPerTeam;
            _state.TeamB.TimeoutsRemaining = TimeoutRules.RegularTimeoutsPerTeam;
            TacticalDeckRules.Initialize(_state.TeamA, content.TacticalCards.Values, _random);
            TacticalDeckRules.Initialize(_state.TeamB, content.TacticalCards.Values, _random);
            _output = new MatchResult { seed = seed };
        }

        public bool UsesItemizedEconomy => _battleSimMode == BattleSimMode.BehaviorTree;
        public BattleSimMode BattleSimMode => _battleSimMode;
        public WeaponCatalog Weapons => _weaponCatalog;

        public MatchState State => _state;
        public MatchResult Result => _output;
        public ContentDatabase Content => _content;
        public MapDefinition Map => _map;
        public bool IsComplete => MatchRules.HasWinner(_state);
        public bool IsFinished => _finished;
        public int NextRoundNumber => _state.CompletedRounds + 1;
        public bool RoundPrepared => _roundPrepared;
        public bool TimeoutActive { get; private set; }
        public bool TimeoutPending { get; private set; }
        public string TimeoutPendingTeamId { get; private set; }
        /// <summary>Optional overview LOS for BehaviorTree rounds; null = fail-open.</summary>
        public ICombatLosQuery CombatLos { get; set; }

        public TeamRuntime TeamById(string teamId) =>
            _state.TeamA.Definition.id == teamId ? _state.TeamA : _state.TeamB;

        /// <summary>對戰中申請暫停：消耗 1 次額度，於下回合開局進入教練窗。</summary>
        public bool TryRequestTimeoutForNextRound(string teamId)
        {
            if (TimeoutPending || TimeoutActive || IsComplete)
                return false;
            TeamRuntime team = TeamById(teamId);
            if (team.TimeoutsRemaining <= 0)
                return false;
            team.TimeoutsRemaining--;
            TimeoutPending = true;
            TimeoutPendingTeamId = team.Definition.id;
            _output.events.Add(MetaEvent(
                _globalSequence++, Math.Max(1, _state.CompletedRounds), EventType.Timeout,
                $"{team.Definition.id} requested timeout for next round ({team.TimeoutsRemaining} left)"));
            return true;
        }

        /// <summary>若有待處理暫停，開局套用全體手感 −5 並開啟教練窗。回傳是否進入暫停窗。</summary>
        public bool TryBeginPendingTimeoutWindow()
        {
            if (!TimeoutPending || IsComplete)
                return false;
            string teamId = TimeoutPendingTeamId;
            TimeoutPending = false;
            TimeoutPendingTeamId = null;
            TeamRuntime team = TeamById(teamId);
            team.CoachUsedThisTimeout.Clear();
            team.PaidCoachUsedThisTimeout = false;
            TeamRuntime opponent = team == _state.TeamA ? _state.TeamB : _state.TeamA;
            opponent.CoachUsedThisTimeout.Clear();
            opponent.PaidCoachUsedThisTimeout = false;
            TimeoutCoachRules.ApplyFormPenaltyToBoth(_state);
            TimeoutActive = true;
            _output.events.Add(MetaEvent(
                _globalSequence++, NextRoundNumber, EventType.Timeout,
                $"{team.Definition.id} timeout window opened (form -5 both teams)"));
            CoachCardDefinition shout = _content.CoachCards.Values
                .FirstOrDefault(c => c.isBaseCoach && c.id == "coach_base_shout");
            if (shout != null)
                TimeoutCoachRules.TryApplyCoach(opponent, shout, null, _state);
            return true;
        }

        public void EndTimeoutWindow() => TimeoutActive = false;

        public bool TryApplyCoachCard(string teamId, string cardId, string targetPlayerId = null)
        {
            if (!TimeoutActive || !_content.CoachCards.TryGetValue(cardId, out CoachCardDefinition card))
                return false;
            return TimeoutCoachRules.TryApplyCoach(TeamById(teamId), card, targetPlayerId, _state);
        }

        public void SetFocusSelection(string teamId, string cardId, string targetPlayerId = null)
        {
            TeamRuntime team = TeamById(teamId);
            team.SelectedFocusCardId = cardId ?? string.Empty;
            team.SelectedFocusTargetId = targetPlayerId ?? string.Empty;
        }

        public void PrepareRound()
        {
            if (IsComplete || _finished)
                return;
            if (_roundPrepared)
                return;

            ConfigureSidesAndBreaks(_state, _output.events, ref _globalSequence);
            TacticalDeckRules.BeginRound(_state.TeamA, _random);
            TacticalDeckRules.BeginRound(_state.TeamB, _random);
            if (_state.CompletedRounds > 0 && CommandPointRules.PerRoundRefill > 0)
            {
                CommandPointRules.Add(_state.TeamA, CommandPointRules.PerRoundRefill);
                CommandPointRules.Add(_state.TeamB, CommandPointRules.PerRoundRefill);
            }
            _state.TeamA.SelectedFocusCardId = string.Empty;
            _state.TeamA.SelectedFocusTargetId = string.Empty;
            _state.TeamB.SelectedFocusCardId = string.Empty;
            _state.TeamB.SelectedFocusTargetId = string.Empty;
            TimeoutActive = false;
            _roundPrepared = true;
        }

        public IReadOnlyList<string> HandFor(string teamId) => TeamById(teamId).TacticalDeck.Hand;

        public bool ToggleTacticalCard(string teamId, string cardId)
        {
            TeamRuntime team = TeamById(teamId);
            if (_content.TacticalCards.TryGetValue(cardId, out TacticalCardDefinition limitedCard) &&
                limitedCard.specialRuleKey == "hobbit_reveal_limited" &&
                team.SpecialUses.TryGetValue(cardId, out int uses) && uses >= 2)
                return false;
            bool selected = TacticalDeckRules.ToggleSelection(team, cardId, _content.TacticalCards);
            if (selected && team.TacticalDeck.Selected.Contains(cardId) &&
                _content.TacticalCards.TryGetValue(cardId, out TacticalCardDefinition card) &&
                card.specialRuleKey == "boombl4_draw_four")
            {
                string key = "boombl4_draw_round";
                int round = NextRoundNumber;
                if (!team.SpecialUses.TryGetValue(key, out int usedRound) || usedRound != round)
                {
                    TacticalDeckRules.Draw(team, 4, _random);
                    team.SpecialUses[key] = round;
                }
            }
            return selected;
        }

        public bool DiscardTacticalCard(string teamId, string cardId) =>
            TacticalDeckRules.DiscardFromHand(TeamById(teamId), cardId);

        public RoundResult PlayRound(
            string playerTeamId,
            EquipmentLevel playerCard,
            IReadOnlyList<string> playerTacticalCards = null,
            string playerFocusCardId = null,
            string playerFocusTargetId = null,
            IReadOnlyList<string> heroPlayerIds = null)
        {
            if (IsComplete || _finished)
                throw new InvalidOperationException("Match already complete.");
            if (!_roundPrepared)
                PrepareRound();
            TimeoutActive = false;

            TeamRuntime attackers = _state.TeamA.Side == Side.Terrorist ? _state.TeamA : _state.TeamB;
            TeamRuntime defenders = attackers == _state.TeamA ? _state.TeamB : _state.TeamA;
            TacticalPlan attackPlan = _planner.CreatePlan(_state, attackers, Side.Terrorist, _random);
            TacticalPlan defendPlan = _planner.CreatePlan(_state, defenders, Side.CounterTerrorist, _random);

            bool pistolRound = MatchRules.IsPistolRound(_state.CompletedRounds);
            if (UsesItemizedEconomy && !pistolRound)
            {
                attackPlan.equipmentLevel = EconomyCardLoadoutRules.BestAffordable(
                    attackers, _weaponCatalog,
                    attackers.Players.Any(x => x.Definition.primaryRole == "Awper"));
                defendPlan.equipmentLevel = EconomyCardLoadoutRules.BestAffordable(
                    defenders, _weaponCatalog,
                    defenders.Players.Any(x => x.Definition.primaryRole == "Awper"));
            }
            else if (!UsesItemizedEconomy)
            {
                // quick mode planner already set levels via CreatePlan
            }

            if (pistolRound)
            {
                attackPlan.equipmentLevel = EquipmentLevel.Eco;
                defendPlan.equipmentLevel = EquipmentLevel.Eco;
                attackers.HeroBuyPlayerIds.Clear();
                defenders.HeroBuyPlayerIds.Clear();
            }
            else
            {
                ApplyPlayerCard(attackers, attackPlan, playerTeamId, playerCard, heroPlayerIds);
                ApplyPlayerCard(defenders, defendPlan, playerTeamId, playerCard, heroPlayerIds);
            }

            TeamRuntime playerTeam = TeamById(playerTeamId);
            TeamRuntime opponentTeam = playerTeam == _state.TeamA ? _state.TeamB : _state.TeamA;
            IReadOnlyList<string> playerChoices = playerTacticalCards ??
                (playerTeam.TacticalDeck.Selected.Count > 0
                    ? playerTeam.TacticalDeck.Selected.ToArray()
                    : _tacticalAi.Choose(playerTeam, _state, _content.TacticalCards, _random));
            IReadOnlyList<string> opponentChoices =
                _tacticalAi.Choose(opponentTeam, _state, _content.TacticalCards, _random);
            SetSelection(playerTeam, playerChoices);
            SetSelection(opponentTeam, opponentChoices);
            ApplyDrawSpecial(playerTeam);
            ApplyDrawSpecial(opponentTeam);
            if (playerTacticalCards == null)
                _tacticalAi.DiscardOverflow(playerTeam, _content.TacticalCards);
            _tacticalAi.DiscardOverflow(opponentTeam, _content.TacticalCards);

            TacticalCardDefinition[] attackCards = CardsFor(attackers);
            TacticalCardDefinition[] defendCards = CardsFor(defenders);
            TacticalPlanFactory.ApplyCards(attackPlan, attackers, attackCards);
            TacticalPlanFactory.ApplyCards(defendPlan, defenders, defendCards);
            ApplyRouteSpecial(attackPlan);
            ApplyRouteSpecial(defendPlan);
            TacticalPlanFactory.ApplyCounters(attackPlan, attackCards, defendCards);
            TacticalPlanFactory.ApplyCounters(defendPlan, defendCards, attackCards);
            TacticalPlanFactory.ApplyHobbitSuppression(attackPlan, defendCards);
            TacticalPlanFactory.ApplyHobbitSuppression(defendPlan, attackCards);
            ConsumeLimitedSpecials(attackers, attackCards);
            ConsumeLimitedSpecials(defenders, defendCards);

            if (!TacticalDeckRules.Commit(playerTeam, _content.TacticalCards))
                throw new InvalidOperationException("Player tactical selection is not legal.");
            if (!TacticalDeckRules.Commit(opponentTeam, _content.TacticalCards))
                throw new InvalidOperationException("Opponent tactical selection is not legal.");

            // Focus cards after tactical CP commit.
            string focusId = playerFocusCardId ?? playerTeam.SelectedFocusCardId;
            string focusTarget = playerFocusTargetId ?? playerTeam.SelectedFocusTargetId;
            if (!string.IsNullOrEmpty(focusId) &&
                _content.FocusCards.TryGetValue(focusId, out FocusCardDefinition playerFocus))
            {
                FocusCardRules.TryCommit(
                    playerTeam, playerFocus, focusTarget,
                    playerTeam == attackers ? attackPlan : defendPlan,
                    _content.TacticalCards);
            }

            FocusCardRules.ChooseAi(
                opponentTeam, _state, _content.FocusCards, _content.TacticalCards, _random);
            if (!string.IsNullOrEmpty(opponentTeam.SelectedFocusCardId) &&
                _content.FocusCards.TryGetValue(opponentTeam.SelectedFocusCardId, out FocusCardDefinition oppFocus))
            {
                FocusCardRules.TryCommit(
                    opponentTeam, oppFocus, opponentTeam.SelectedFocusTargetId,
                    opponentTeam == attackers ? attackPlan : defendPlan,
                    _content.TacticalCards);
            }

            // Apply deferred coach teamwork bump if any.
            ApplyCoachTeamwork(attackers, attackPlan);
            ApplyCoachTeamwork(defenders, defendPlan);

            RoundResult round = _roundSimulator.Simulate(new RoundSimulationRequest
            {
                match = _state,
                attackers = attackers,
                defenders = defenders,
                attackerPlan = attackPlan,
                defenderPlan = defendPlan,
                map = _map,
                roundNumber = _state.CompletedRounds + 1,
                Los = CombatLos
            }, _random);

            foreach (SimulationEvent evt in round.events)
            {
                evt.sequence = _globalSequence++;
                _output.events.Add(evt);
            }

            if (round.winnerTeamId == _state.TeamA.Definition.id) _state.TeamAScore++;
            else _state.TeamBScore++;
            _state.CompletedRounds++;
            _roundPrepared = false;

            _output.teamAScore = _state.TeamAScore;
            _output.teamBScore = _state.TeamBScore;
            if (IsComplete)
                FinishMatch();
            return round;
        }

        private static void ApplyCoachTeamwork(TeamRuntime team, TacticalPlan plan)
        {
            if (!team.SpecialUses.TryGetValue("coach_teamwork_bonus", out int milli) || milli <= 0)
                return;
            plan.teamworkBonus = Math.Min(0.30f, plan.teamworkBonus + milli / 1000f);
            team.SpecialUses.Remove("coach_teamwork_bonus");
        }

        private TacticalCardDefinition[] CardsFor(TeamRuntime team) =>
            team.TacticalDeck.Selected
                .Where(_content.TacticalCards.ContainsKey)
                .Select(id => _content.TacticalCards[id])
                .ToArray();

        private void ApplyRouteSpecial(TacticalPlan plan)
        {
            if (plan.specialRuleKeys.Contains("decoy_rotate") && NextRoundNumber % 2 == 0)
                plan.primaryZoneId = "b_tunnels";
        }

        private void SetSelection(TeamRuntime team, IEnumerable<string> choices)
        {
            team.TacticalDeck.Selected.Clear();
            foreach (string id in choices ?? Enumerable.Empty<string>())
            {
                if (team.TacticalDeck.Selected.Count >= TacticalDeckRules.MaximumCardsPerRound)
                    break;
                if (_content.TacticalCards.TryGetValue(id, out TacticalCardDefinition card) &&
                    card.specialRuleKey == "hobbit_reveal_limited" &&
                    team.SpecialUses.TryGetValue(id, out int uses) && uses >= 2)
                    continue;
                if (TacticalDeckRules.CanSelect(team, id, _content.TacticalCards))
                    team.TacticalDeck.Selected.Add(id);
            }
        }

        private void ApplyDrawSpecial(TeamRuntime team)
        {
            if (!team.TacticalDeck.Selected.Any(id =>
                    _content.TacticalCards.TryGetValue(id, out TacticalCardDefinition card) &&
                    card.specialRuleKey == "boombl4_draw_four"))
                return;
            string key = "boombl4_draw_round";
            int round = NextRoundNumber;
            if (team.SpecialUses.TryGetValue(key, out int usedRound) && usedRound == round)
                return;
            TacticalDeckRules.Draw(team, 4, _random);
            team.SpecialUses[key] = round;
        }

        private static void ConsumeLimitedSpecials(
            TeamRuntime team,
            IEnumerable<TacticalCardDefinition> cards)
        {
            foreach (TacticalCardDefinition card in cards)
            {
                if (card.specialRuleKey != "hobbit_reveal_limited")
                    continue;
                team.SpecialUses.TryGetValue(card.id, out int uses);
                team.SpecialUses[card.id] = uses + 1;
            }
        }

        public void AutoPlayRemaining(string playerTeamId)
        {
            while (AutoPlayOneRound(playerTeamId))
            {
            }
        }

        /// <summary>
        /// Plays at most one remaining round. Returns true if the match still needs more rounds.
        /// </summary>
        public bool AutoPlayOneRound(string playerTeamId)
        {
            if (_finished)
                return false;

            if (IsComplete)
            {
                FinishMatch();
                return false;
            }

            PrepareRound();
            TeamRuntime player = TeamById(playerTeamId);
            bool hasAwper = player.Players.Any(x => x.Definition.primaryRole == "Awper");
            EquipmentLevel card = UsesItemizedEconomy
                ? EconomyCardLoadoutRules.BestAffordable(player, _weaponCatalog, hasAwper)
                : EconomyRules.BestAffordable(player.Money, hasAwper);
            PlayRound(playerTeamId, card);

            if (IsComplete && !_finished)
                FinishMatch();

            return !_finished;
        }

        public void FinishMatch()
        {
            if (_finished)
                return;
            if (!IsComplete)
                throw new InvalidOperationException($"Match exceeded safety limit of {MatchRules.MaximumRounds} rounds.");

            _output.winnerTeamId = _state.TeamAScore > _state.TeamBScore
                ? _state.TeamA.Definition.id
                : _state.TeamB.Definition.id;
            _output.teamAScore = _state.TeamAScore;
            _output.teamBScore = _state.TeamBScore;
            _output.events.Add(new SimulationEvent
            {
                sequence = _globalSequence++,
                roundNumber = _state.CompletedRounds,
                roundTime = 140f,
                type = EventType.MatchEnded,
                teamId = _output.winnerTeamId,
                message = $"{_output.winnerTeamId} wins {_output.teamAScore}-{_output.teamBScore}",
                reasonTags = Array.Empty<string>()
            });
            _output.playerSummaries = _state.TeamA.Players
                .Select(x => BuildSummary(_state.TeamA.Definition.id, x))
                .Concat(_state.TeamB.Players.Select(x => BuildSummary(_state.TeamB.Definition.id, x)))
                .ToList();
            _finished = true;
        }

        private static PlayerMatchSummary BuildSummary(string teamId, PlayerRuntime player)
        {
            int rounds = Math.Max(1, player.Kills + player.Deaths);
            return new PlayerMatchSummary
            {
                playerId = player.Definition.id,
                teamId = teamId,
                kills = player.Kills,
                deaths = player.Deaths,
                assists = player.Assists,
                rating = Math.Max(.25f, Math.Min(2f,
                    .8f + player.Kills / (float)rounds * .7f +
                    player.Assists / (float)rounds * .25f -
                    player.Deaths / (float)rounds * .25f))
            };
        }

        private void ApplyPlayerCard(
            TeamRuntime team,
            TacticalPlan plan,
            string playerTeamId,
            EquipmentLevel playerCard,
            IReadOnlyList<string> heroPlayerIds)
        {
            if (!string.Equals(team.Definition.id, playerTeamId, StringComparison.OrdinalIgnoreCase))
                return;

            EquipmentLevel actual = playerCard;
            if (UsesItemizedEconomy)
            {
                if (actual == EquipmentLevel.Hero)
                {
                    BehaviorTreeBuyRules.SetHeroTargets(
                        team,
                        heroPlayerIds != null && heroPlayerIds.Count > 0
                            ? heroPlayerIds
                            : BehaviorTreeBuyRules.SuggestHeroTargets(team));
                    if (!EconomyCardLoadoutRules.CanAffordCard(team, actual, _weaponCatalog))
                        actual = EconomyCardLoadoutRules.BestAffordable(team, _weaponCatalog, false);
                }
                else
                {
                    team.HeroBuyPlayerIds.Clear();
                    while (actual > EquipmentLevel.Eco &&
                           !EconomyCardLoadoutRules.CanAffordCard(team, actual, _weaponCatalog))
                        actual--;
                }
            }
            else
            {
                team.HeroBuyPlayerIds.Clear();
                if (actual == EquipmentLevel.Hero)
                    actual = EquipmentLevel.FullBuy;
                while (actual > EquipmentLevel.Eco && !EconomyRules.CanAfford(team.Money, actual))
                    actual--;
            }
            plan.equipmentLevel = actual;
        }

        private static TeamRuntime CreateTeam(ContentDatabase content, string teamId, IRandomSource random)
        {
            TeamDefinition definition = content.Teams[teamId];
            var players = definition.playerIds.Select(id =>
                new PlayerRuntime(content.Players[id], 50f + random.Next01() * 20f));
            return new TeamRuntime(definition, players);
        }

        private void ConfigureSidesAndBreaks(
            MatchState state, List<SimulationEvent> events, ref int sequence)
        {
            bool teamAIsT = MatchRules.TeamAIsTerrorist(state.CompletedRounds);
            state.TeamA.Side = teamAIsT ? Side.Terrorist : Side.CounterTerrorist;
            state.TeamB.Side = teamAIsT ? Side.CounterTerrorist : Side.Terrorist;

            if (state.CompletedRounds == MatchRules.RegulationHalfRounds)
            {
                CommandPointRules.Add(state.TeamA, CommandPointRules.HalfTimeRefill);
                CommandPointRules.Add(state.TeamB, CommandPointRules.HalfTimeRefill);
                RestoreStamina(state, 20f);
                events.Add(MetaEvent(sequence++, state.CompletedRounds + 1, EventType.HalfTime, "Half-time: sides switched"));
            }

            if (state.CompletedRounds >= MatchRules.RegulationRounds &&
                (state.CompletedRounds - MatchRules.RegulationRounds) % MatchRules.OvertimeHalfRounds == 0)
            {
                CommandPointRules.Add(state.TeamA, CommandPointRules.OvertimeRefill);
                CommandPointRules.Add(state.TeamB, CommandPointRules.OvertimeRefill);
                if (UsesItemizedEconomy)
                {
                    ItemizedEconomyRules.ApplyOvertimeReset(state.TeamA);
                    ItemizedEconomyRules.ApplyOvertimeReset(state.TeamB);
                }
                else
                {
                    state.TeamA.Money = EconomyRules.OvertimeTeamMoney;
                    state.TeamB.Money = EconomyRules.OvertimeTeamMoney;
                }
                RestoreStamina(state, 8f);
                if (state.CompletedRounds == MatchRules.RegulationRounds)
                {
                    TimeoutCoachRules.GrantOvertimeTimeouts(state);
                    events.Add(MetaEvent(sequence++, state.CompletedRounds + 1, EventType.OvertimeStarted, "Overtime MR3 started"));
                }
                else
                    events.Add(MetaEvent(sequence++, state.CompletedRounds + 1, EventType.HalfTime, "Overtime sides switched"));
            }
        }

        private static void RestoreStamina(MatchState state, float amount)
        {
            foreach (PlayerRuntime player in state.TeamA.Players.Concat(state.TeamB.Players))
                player.Stamina = Math.Min(100f, player.Stamina + amount);
        }

        private static SimulationEvent MetaEvent(int sequence, int round, EventType type, string message) =>
            new SimulationEvent
            {
                sequence = sequence,
                roundNumber = round,
                roundTime = 0f,
                type = type,
                message = message,
                reasonTags = Array.Empty<string>()
            };
    }

    public sealed class QuickMatchSimulator : IMatchSimulator
    {
        private readonly ITacticalPlanner _planner;
        private readonly IRoundSimulator _roundSimulator;

        public QuickMatchSimulator(ITacticalPlanner planner, IRoundSimulator roundSimulator)
        {
            _planner = planner;
            _roundSimulator = roundSimulator;
        }

        public MatchResult Simulate(ContentDatabase content, string teamAId, string teamBId, int seed)
        {
            // Full auto: both sides use BestAffordable via the planner (player override unused).
            var session = new InteractiveQuickMatchSession(
                content, seed, _planner, _roundSimulator, teamAId, teamBId);
            while (!session.IsComplete)
            {
                session.PrepareRound();
                TeamRuntime playerTeam = session.TeamById(teamAId);
                bool hasAwper = playerTeam.Players.Any(x => x.Definition.primaryRole == "Awper");
                session.PlayRound(teamAId, EconomyRules.BestAffordable(playerTeam.Money, hasAwper));
            }
            if (!session.IsFinished)
                session.FinishMatch();
            return session.Result;
        }
    }

    public sealed class QuickMatchUseCase
    {
        private readonly IMatchSimulator _simulator;

        public QuickMatchUseCase(IMatchSimulator simulator) => _simulator = simulator;

        public MatchResult Run(ContentDatabase content, int seed) =>
            _simulator.Simulate(content, "navi_2021", "gambit_2021", seed);
    }
}
