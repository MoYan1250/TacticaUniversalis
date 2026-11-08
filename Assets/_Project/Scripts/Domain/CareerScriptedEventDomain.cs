using System;
using System.Collections.Generic;
using System.Linq;

namespace TacticaUniversalis.Domain
{
    /// <summary>EU4-style persistent flag (global or club-scoped) with optional expiry.</summary>
    [Serializable]
    public sealed class CareerFlag
    {
        public string id;
        /// <summary>Empty / "global" = worldwide; otherwise clubId.</summary>
        public string scope = "global";
        /// <summary>0 = permanent until cleared.</summary>
        public int expiresHour;
    }

    [Serializable]
    public sealed class CareerPendingEvent
    {
        public string instanceId;
        public string eventId;
        public string clubId;
        public int firedHour;
    }

    [Serializable]
    public sealed class CareerConditionDef
    {
        /// <summary>
        /// always | has_flag | not_flag | fame_min | fame_max | exposure_min | exposure_max |
        /// cash_min | cash_max | board_min | board_max | estate_min | estate_max | fans_min | difficulty | hardcore |
        /// rank_max | has_achievement | not_achievement |
        /// competition_champion (key = competitionId:clubId) |
        /// official_matches_min (value = min official matches for player club) |
        /// competition_started (key = competitionId; player club entered and event live or past) |
        /// player_club | not_player_club (key = clubId) |
        /// clock_min | clock_max (value = absolute clockHour) |
        /// any (OR of nested <see cref="conditions"/>)
        /// </summary>
        public string type = "always";
        public string key;
        public int value;
        /// <summary>Nested conditions for type <c>any</c>.</summary>
        public CareerConditionDef[] conditions = Array.Empty<CareerConditionDef>();
    }

    [Serializable]
    public sealed class CareerEffectDef
    {
        /// <summary>
        /// set_flag | clear_flag | add_cash | add_fame | add_exposure | add_board | add_estate | add_fans |
        /// add_roster_pressure | add_roster_fame | add_role_map_proficiency | unlock_achievement | log |
        /// game_version_update.
        /// add_role_map_proficiency uses key "Role:MapId" (e.g. Awper:Dust II) and signed value delta.
        /// </summary>
        public string type;
        public string key;
        public int value;
        /// <summary>Days until flag expires; 0 = permanent.</summary>
        public int durationDays;
    }

    [Serializable]
    public sealed class CareerRosterOpDef
    {
        /// <summary>
        /// set_squad | join_club | release | request_transfer
        /// </summary>
        public string type;
        public string playerId;
        /// <summary>Target club for join_club.</summary>
        public string clubId;
        /// <summary>starter | bench | academy | promoted</summary>
        public string squadStatus;
    }

    [Serializable]
    public sealed class CareerEventOptionDef
    {
        public string id;
        public string label;
        public string tooltip;
        public CareerEffectDef[] effects = Array.Empty<CareerEffectDef>();
    }

    public enum ScriptedEventTier
    {
        /// <summary>JSON / legacy unset → infer from isMajor.</summary>
        Unset = 0,
        /// <summary>S+ 世界事件：超大邊框，強制暫停。</summary>
        SPlus = 1,
        /// <summary>S 變革事件：大彈窗黑金邊框。</summary>
        S = 2,
        /// <summary>A 歷史事件：大彈窗金色邊框；有俱樂部 LOGO 時顯示相關隊徽。</summary>
        A = 3,
        /// <summary>B 隊伍事件：中型一般彈窗。</summary>
        B = 4,
        /// <summary>C 場外事件：通知欄，預設不暫停。</summary>
        C = 5,
        /// <summary>D 通知事件：通知欄，不暫停；未標公開時僅我方可見。</summary>
        D = 6
    }

    [Serializable]
    public sealed class CareerScriptedEventDef
    {
        public string id;
        public string title;
        /// <summary>第三人稱正文。玩家非當事俱樂部時使用。</summary>
        public string description;
        /// <summary>第一人稱正文。僅當事俱樂部視角；目前可玩俱樂部未涵蓋時保留。</summary>
        public string firstPersonDescription;
        /// <summary>Mean time to happen in days. Ignored when <see cref="fireHour"/> &gt; 0.</summary>
        public int mtthDays = 120;
        /// <summary>Absolute career-epoch hour to fire once. 0 = MTTH random event.</summary>
        public int fireHour;
        /// <summary>Legacy: true maps to B when <see cref="tier"/> is Unset.</summary>
        public bool isMajor = true;
        /// <summary>Event tier ladder. Unset → infer from isMajor.</summary>
        public ScriptedEventTier tier = ScriptedEventTier.Unset;
        /// <summary>When true, D-tier events appear in global news for all clubs.</summary>
        public bool publicVisible;
        /// <summary>Club this news is about. Used for related logos, on-fire effects, and first-person POV.</summary>
        public string subjectClubId;
        /// <summary>
        /// Optional extra clubs featured in the event chrome. Logos only render when an asset exists.
        /// </summary>
        public string[] relatedClubIds = Array.Empty<string>();
        /// <summary>
        /// Applied once when the event fires, to <see cref="subjectClubId"/> when set;
        /// otherwise to the player club. Third-person option picks should not carry club buffs.
        /// </summary>
        public CareerEffectDef[] onFireEffects = Array.Empty<CareerEffectDef>();
        /// <summary>
        /// Historical AI roster mutations applied on fire when the subject club is not the player club.
        /// Player-side roster changes stay reserved for the mission tree.
        /// </summary>
        public CareerRosterOpDef[] onFireRosterOps = Array.Empty<CareerRosterOpDef>();
        /// <summary>
        /// When set, <see cref="onFireEffects"/> only apply if the target club roster has a player
        /// whose fitness grade on this map is at least <see cref="onFireRosterMinGrade"/>.
        /// </summary>
        public string onFireRosterMapId;
        /// <summary>Minimum inclusive grade for the roster gate (e.g. "S"). Empty = no gate.</summary>
        public string onFireRosterMinGrade;
        /// <summary>
        /// Keep first-person copy stored but never shown yet. Used when the first-person POV
        /// depends on future roster conditions we do not surface to the player.
        /// </summary>
        public bool firstPersonReserved;
        /// <summary>
        /// When set with <see cref="firstPersonRosterMapId"/> / grade fields, first-person copy
        /// only shows if the player club roster meets the gate (independent of subjectClubId).
        /// </summary>
        public string firstPersonRosterRole;
        /// <summary>Map id checked on player-club roster for first-person POV (e.g. Dust II).</summary>
        public string firstPersonRosterMapId;
        /// <summary>
        /// Inclusive minimum map grade for first-person POV. Ignored when
        /// <see cref="firstPersonRosterAboveGrade"/> is set.
        /// </summary>
        public string firstPersonRosterMinGrade;
        /// <summary>
        /// Exclusive floor: roster map grade must be strictly above this (e.g. A → A+ or S).
        /// </summary>
        public string firstPersonRosterAboveGrade;
        /// <summary>
        /// When set, replace <c>{決賽對手}</c> in body text with that competition's runner-up
        /// display name (final loser vs champion).
        /// </summary>
        public string tokenCompetitionId;
        public CareerConditionDef[] triggers = Array.Empty<CareerConditionDef>();
        /// <summary>第三人稱選項。</summary>
        public CareerEventOptionDef[] options = Array.Empty<CareerEventOptionDef>();
        /// <summary>第一人稱選項。僅當事俱樂部；目前保留。</summary>
        public CareerEventOptionDef[] firstPersonOptions = Array.Empty<CareerEventOptionDef>();
    }

    public static class ScriptedEventTierRules
    {
        public static ScriptedEventTier Resolve(CareerScriptedEventDef def)
        {
            if (def == null)
                return ScriptedEventTier.B;
            if (def.tier != ScriptedEventTier.Unset && Enum.IsDefined(typeof(ScriptedEventTier), def.tier))
                return def.tier;
            return def.isMajor ? ScriptedEventTier.B : ScriptedEventTier.D;
        }

        public static string TierCode(ScriptedEventTier tier)
        {
            switch (ResolveOrDefault(tier))
            {
                case ScriptedEventTier.SPlus: return "S+";
                case ScriptedEventTier.S: return "S";
                case ScriptedEventTier.A: return "A";
                case ScriptedEventTier.B: return "B";
                case ScriptedEventTier.C: return "C";
                case ScriptedEventTier.D: return "D";
                default: return "B";
            }
        }

        public static string TierLabel(ScriptedEventTier tier)
        {
            switch (ResolveOrDefault(tier))
            {
                case ScriptedEventTier.SPlus: return "世界事件";
                case ScriptedEventTier.S: return "變革事件";
                case ScriptedEventTier.A: return "歷史事件";
                case ScriptedEventTier.B: return "隊伍事件";
                case ScriptedEventTier.C: return "場外事件";
                case ScriptedEventTier.D: return "通知事件";
                default: return "隊伍事件";
            }
        }

        static ScriptedEventTier ResolveOrDefault(ScriptedEventTier tier) =>
            tier == ScriptedEventTier.Unset ? ScriptedEventTier.B : tier;

        public static bool UsesModalPopup(ScriptedEventTier tier)
        {
            ScriptedEventTier t = ResolveOrDefault(tier);
            return t == ScriptedEventTier.SPlus ||
                   t == ScriptedEventTier.S ||
                   t == ScriptedEventTier.A ||
                   t == ScriptedEventTier.B;
        }

        /// <summary>S+ stays fixed and modal; all other popup tiers may be dragged aside.</summary>
        public static bool AllowsWindowDrag(ScriptedEventTier tier) =>
            ResolveOrDefault(tier) != ScriptedEventTier.SPlus;

        public static bool UsesNotificationBar(ScriptedEventTier tier)
        {
            ScriptedEventTier t = ResolveOrDefault(tier);
            return t == ScriptedEventTier.C || t == ScriptedEventTier.D;
        }

        /// <summary>
        /// Toast-only path: D is always notification-only; C only when at most one option
        /// (across third-person and first-person option lists). Multi-choice C uses the popup.
        /// </summary>
        public static bool UsesNotificationOnly(CareerScriptedEventDef def)
        {
            ScriptedEventTier tier = Resolve(def);
            if (tier == ScriptedEventTier.D)
                return true;
            if (tier != ScriptedEventTier.C)
                return false;
            int third = def?.options?.Length ?? 0;
            int first = def?.firstPersonOptions?.Length ?? 0;
            return Math.Max(third, first) <= 1;
        }

        public static bool UsesInteractivePopup(CareerScriptedEventDef def) =>
            !UsesNotificationOnly(def);

        public static string FiredFlagId(string eventId) =>
            "evt_fired_" + (eventId ?? string.Empty);

        public static bool HasFirstPersonRosterGate(CareerScriptedEventDef def) =>
            def != null &&
            (!string.IsNullOrWhiteSpace(def.firstPersonRosterRole) ||
             !string.IsNullOrWhiteSpace(def.firstPersonRosterMapId) ||
             !string.IsNullOrWhiteSpace(def.firstPersonRosterMinGrade) ||
             !string.IsNullOrWhiteSpace(def.firstPersonRosterAboveGrade));

        /// <summary>
        /// Roster gate for first-person POV. When no roster fields are set, returns true.
        /// <paramref name="resolvePlayerRole"/> maps playerId → primary role (e.g. Awper).
        /// </summary>
        public static bool MeetsFirstPersonRosterGate(
            CareerScriptedEventDef def,
            string clubId,
            IEnumerable<PlayerCareerState> players,
            Func<string, string> resolvePlayerRole)
        {
            if (!HasFirstPersonRosterGate(def))
                return true;
            if (string.IsNullOrWhiteSpace(clubId) || players == null)
                return false;

            foreach (PlayerCareerState player in players)
            {
                if (player == null ||
                    !string.Equals(player.clubId, clubId, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.IsNullOrWhiteSpace(def.firstPersonRosterRole))
                {
                    string role = resolvePlayerRole != null ? resolvePlayerRole(player.playerId) : null;
                    if (!string.Equals(role, def.firstPersonRosterRole, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                if (!string.IsNullOrWhiteSpace(def.firstPersonRosterMapId) ||
                    !string.IsNullOrWhiteSpace(def.firstPersonRosterAboveGrade) ||
                    !string.IsNullOrWhiteSpace(def.firstPersonRosterMinGrade))
                {
                    CareerNamedStat stat = player.mapProficiencies?.FirstOrDefault(x =>
                        string.Equals(x.id, def.firstPersonRosterMapId, StringComparison.OrdinalIgnoreCase));
                    if (stat == null || string.IsNullOrWhiteSpace(stat.grade))
                        continue;
                    if (!string.IsNullOrWhiteSpace(def.firstPersonRosterAboveGrade))
                    {
                        if (!GradeScale.Above(stat.grade, def.firstPersonRosterAboveGrade))
                            continue;
                    }
                    else if (!string.IsNullOrWhiteSpace(def.firstPersonRosterMinGrade) &&
                             !GradeScale.AtLeast(stat.grade, def.firstPersonRosterMinGrade))
                    {
                        continue;
                    }
                }

                return true;
            }

            return false;
        }

        public static bool PreferFirstPerson(
            CareerScriptedEventDef def,
            string playerClubId,
            Func<bool> rosterGate = null)
        {
            if (def == null || def.firstPersonReserved)
                return false;

            bool hasBody = !string.IsNullOrWhiteSpace(def.firstPersonDescription);
            bool hasOptions = def.firstPersonOptions != null && def.firstPersonOptions.Length > 0;
            if (!hasBody && !hasOptions)
                return false;

            if (HasFirstPersonRosterGate(def))
                return rosterGate != null && rosterGate();

            // Empty subjectClubId = global / all clubs use first-person copy when authored.
            return string.IsNullOrWhiteSpace(def.subjectClubId) ||
                   string.Equals(def.subjectClubId, playerClubId, StringComparison.OrdinalIgnoreCase);
        }

        public static string ResolveBody(
            CareerScriptedEventDef def,
            string playerClubId,
            Func<bool> rosterGate = null)
        {
            if (def == null)
                return string.Empty;
            if (PreferFirstPerson(def, playerClubId, rosterGate) &&
                !string.IsNullOrWhiteSpace(def.firstPersonDescription))
                return def.firstPersonDescription;

            return def.description ?? string.Empty;
        }

        public static CareerEventOptionDef[] ResolveOptions(
            CareerScriptedEventDef def,
            string playerClubId,
            Func<bool> rosterGate = null)
        {
            if (def == null)
                return Array.Empty<CareerEventOptionDef>();
            if (PreferFirstPerson(def, playerClubId, rosterGate) &&
                def.firstPersonOptions != null &&
                def.firstPersonOptions.Length > 0)
                return def.firstPersonOptions;

            return def.options ?? Array.Empty<CareerEventOptionDef>();
        }

        public static bool ForcePause(ScriptedEventTier tier) =>
            ResolveOrDefault(tier) == ScriptedEventTier.SPlus;

        /// <summary>
        /// News feed visibility for scripted events.
        /// D is toast-only by default; set <paramref name="publicVisible"/> for specific historical
        /// D items that should still appear in the news column.
        /// C and above always report as world/club news.
        /// </summary>
        public static bool IsVisibleInNews(
            ScriptedEventTier tier,
            bool publicVisible,
            string eventClubId,
            string playerClubId)
        {
            ScriptedEventTier t = ResolveOrDefault(tier);
            if (t == ScriptedEventTier.D)
                return publicVisible;
            return true;
        }

        public static bool ShouldPause(
            ScriptedEventTier tier,
            bool pauseOnS,
            bool pauseOnA,
            bool pauseOnB,
            bool pauseOnC)
        {
            ScriptedEventTier t = ResolveOrDefault(tier);
            if (ForcePause(t))
                return true;
            switch (t)
            {
                case ScriptedEventTier.S: return pauseOnS;
                case ScriptedEventTier.A: return pauseOnA;
                case ScriptedEventTier.B: return pauseOnB;
                case ScriptedEventTier.C: return pauseOnC;
                default: return false;
            }
        }
    }

    [Serializable]
    public sealed class CareerAchievementDef
    {
        public string id;
        public string title;
        public string description;
        public bool hardcoreOnly;
        public CareerConditionDef[] triggers = Array.Empty<CareerConditionDef>();
        public CareerEffectDef[] rewards = Array.Empty<CareerEffectDef>();
    }

    public static class CareerFlagRules
    {
        public const string GlobalScope = "global";

        public static string NormalizeScope(string scope) =>
            string.IsNullOrWhiteSpace(scope) ? GlobalScope : scope.Trim();

        public static bool HasFlag(
            IEnumerable<CareerFlag> flags, string flagId, string scope, int clockHour)
        {
            if (flags == null || string.IsNullOrWhiteSpace(flagId))
                return false;
            string wantScope = NormalizeScope(scope);
            foreach (CareerFlag flag in flags)
            {
                if (flag == null || !string.Equals(flag.id, flagId, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.Equals(NormalizeScope(flag.scope), wantScope, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (flag.expiresHour > 0 && clockHour >= flag.expiresHour)
                    continue;
                return true;
            }
            return false;
        }

        public static void SetFlag(
            List<CareerFlag> flags, string flagId, string scope, int clockHour, int durationDays)
        {
            if (flags == null || string.IsNullOrWhiteSpace(flagId))
                return;
            string wantScope = NormalizeScope(scope);
            flags.RemoveAll(x =>
                x != null &&
                string.Equals(x.id, flagId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(NormalizeScope(x.scope), wantScope, StringComparison.OrdinalIgnoreCase));

            int expires = 0;
            if (durationDays > 0)
                expires = clockHour + durationDays * CareerCalendar.HoursPerDay;

            flags.Add(new CareerFlag
            {
                id = flagId,
                scope = wantScope,
                expiresHour = expires
            });
        }

        public static void ClearFlag(List<CareerFlag> flags, string flagId, string scope)
        {
            if (flags == null || string.IsNullOrWhiteSpace(flagId))
                return;
            string wantScope = NormalizeScope(scope);
            flags.RemoveAll(x =>
                x != null &&
                string.Equals(x.id, flagId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(NormalizeScope(x.scope), wantScope, StringComparison.OrdinalIgnoreCase));
        }

        public static void PurgeExpired(List<CareerFlag> flags, int clockHour)
        {
            if (flags == null) return;
            flags.RemoveAll(x => x != null && x.expiresHour > 0 && clockHour >= x.expiresHour);
        }
    }

    public static class CareerScriptedRules
    {
        public static bool MatchesAll(
            CareerConditionDef[] conditions,
            CareerState state,
            ClubState club,
            IReadOnlyCollection<string> unlockedAchievements,
            IReadOnlyDictionary<string, CompetitionDefinition> competitions = null)
        {
            if (conditions == null || conditions.Length == 0)
                return true;
            foreach (CareerConditionDef condition in conditions)
            {
                if (!Matches(condition, state, club, unlockedAchievements, competitions))
                    return false;
            }
            return true;
        }

        public static bool Matches(
            CareerConditionDef condition,
            CareerState state,
            ClubState club,
            IReadOnlyCollection<string> unlockedAchievements,
            IReadOnlyDictionary<string, CompetitionDefinition> competitions = null)
        {
            if (condition == null || string.IsNullOrWhiteSpace(condition.type))
                return true;
            if (club == null || state == null)
                return false;

            string type = condition.type.Trim().ToLowerInvariant();
            switch (type)
            {
                case "always":
                    return true;
                case "any":
                    return MatchesAny(condition.conditions, state, club, unlockedAchievements, competitions);
                case "has_flag":
                    return CareerFlagRules.HasFlag(state.flags, condition.key, club.clubId, state.clockHour) ||
                           CareerFlagRules.HasFlag(state.flags, condition.key, CareerFlagRules.GlobalScope, state.clockHour);
                case "not_flag":
                    return !CareerFlagRules.HasFlag(state.flags, condition.key, club.clubId, state.clockHour) &&
                           !CareerFlagRules.HasFlag(state.flags, condition.key, CareerFlagRules.GlobalScope, state.clockHour);
                case "fame_min":
                    return club.fame >= condition.value;
                case "fame_max":
                    return club.fame <= condition.value;
                case "exposure_min":
                    return club.exposure >= condition.value;
                case "exposure_max":
                    return club.exposure <= condition.value;
                case "cash_min":
                    return club.cash >= condition.value;
                case "cash_max":
                    return club.cash <= condition.value;
                case "board_min":
                    return ClubEstateSatisfactionRules.Get(club, ClubEstateId.Board) >= condition.value;
                case "board_max":
                    return ClubEstateSatisfactionRules.Get(club, ClubEstateId.Board) <= condition.value;
                case "estate_min":
                    return ClubEstateSatisfactionRules.TryParseEstateId(condition.key, out ClubEstateId estateMin) &&
                           ClubEstateSatisfactionRules.Get(club, estateMin) >= condition.value;
                case "estate_max":
                    return ClubEstateSatisfactionRules.TryParseEstateId(condition.key, out ClubEstateId estateMax) &&
                           ClubEstateSatisfactionRules.Get(club, estateMax) <= condition.value;
                case "fans_min":
                    return club.fans >= condition.value;
                case "difficulty":
                    return (int)state.difficulty == condition.value;
                case "hardcore":
                    return state.difficulty == CareerDifficulty.Hardcore;
                case "has_achievement":
                    return unlockedAchievements != null &&
                           unlockedAchievements.Contains(condition.key, StringComparer.OrdinalIgnoreCase);
                case "not_achievement":
                    return unlockedAchievements == null ||
                           !unlockedAchievements.Contains(condition.key, StringComparer.OrdinalIgnoreCase);
                case "rank_max":
                    RankingEntry rank = state.rankings?.FirstOrDefault(x =>
                        string.Equals(x.clubId, club.clubId, StringComparison.OrdinalIgnoreCase));
                    return rank != null && rank.rank > 0 && rank.rank <= Math.Max(1, condition.value);
                case "competition_champion":
                    return IsCompetitionChampion(state, condition.key);
                case "official_matches_min":
                    return state.playerOfficialMatchesPlayed >= Math.Max(0, condition.value);
                case "competition_started":
                    return IsCompetitionStarted(state, club, condition.key, competitions);
                case "player_club":
                    return !string.IsNullOrWhiteSpace(condition.key) &&
                           string.Equals(state.playerClubId, condition.key, StringComparison.OrdinalIgnoreCase);
                case "not_player_club":
                    return string.IsNullOrWhiteSpace(condition.key) ||
                           !string.Equals(state.playerClubId, condition.key, StringComparison.OrdinalIgnoreCase);
                case "clock_min":
                    return state.clockHour >= condition.value;
                case "clock_max":
                    return state.clockHour <= condition.value;
                default:
                    return false;
            }
        }

        public static bool MatchesAny(
            CareerConditionDef[] conditions,
            CareerState state,
            ClubState club,
            IReadOnlyCollection<string> unlockedAchievements,
            IReadOnlyDictionary<string, CompetitionDefinition> competitions = null)
        {
            if (conditions == null || conditions.Length == 0)
                return false;
            foreach (CareerConditionDef condition in conditions)
            {
                if (Matches(condition, state, club, unlockedAchievements, competitions))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// key format: competitionId:clubId — true when that club has completed placement 1.
        /// </summary>
        public static bool IsCompetitionChampion(CareerState state, string key)
        {
            if (state?.competitionEntries == null || string.IsNullOrWhiteSpace(key))
                return false;
            int sep = key.IndexOf(':');
            if (sep <= 0 || sep >= key.Length - 1)
                return false;
            string competitionId = key.Substring(0, sep).Trim();
            string clubId = key.Substring(sep + 1).Trim();
            if (string.IsNullOrWhiteSpace(competitionId) || string.IsNullOrWhiteSpace(clubId))
                return false;

            return state.competitionEntries.Any(x =>
                x != null &&
                x.completed &&
                x.placement == 1 &&
                string.Equals(x.competitionId, competitionId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.clubId, clubId, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>True when the competition has begun and the club is entered or has participated.</summary>
        public static bool IsCompetitionStarted(
            CareerState state,
            ClubState club,
            string competitionId,
            IReadOnlyDictionary<string, CompetitionDefinition> competitions)
        {
            if (state == null || club == null || string.IsNullOrWhiteSpace(competitionId))
                return false;

            string clubId = club.clubId;
            CareerCompetitionRunState run = state.competitionRun;
            if (run != null &&
                string.Equals(run.competitionId, competitionId, StringComparison.OrdinalIgnoreCase))
            {
                if (run.currentRoundClubs != null &&
                    run.currentRoundClubs.Any(id =>
                        string.Equals(id, clubId, StringComparison.OrdinalIgnoreCase)))
                    return true;
                if (run.swissStandings != null &&
                    run.swissStandings.Any(s =>
                        s != null &&
                        string.Equals(s.clubId, clubId, StringComparison.OrdinalIgnoreCase) &&
                        !s.eliminated))
                    return true;
            }

            CompetitionEntry entry = state.competitionEntries?.FirstOrDefault(x =>
                x != null &&
                string.Equals(x.competitionId, competitionId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.clubId, clubId, StringComparison.OrdinalIgnoreCase) &&
                x.registered &&
                !x.withdrawn);
            if (entry != null && competitions != null &&
                competitions.TryGetValue(competitionId, out CompetitionDefinition competition) &&
                competition != null &&
                state.clockHour >= competition.startHour)
                return true;

            return state.competitionBrackets?.Any(archive =>
                       archive != null &&
                       string.Equals(archive.competitionId, competitionId, StringComparison.OrdinalIgnoreCase) &&
                       archive.matches != null &&
                       archive.matches.Any(m =>
                           m != null &&
                           (string.Equals(m.clubAId, clubId, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(m.clubBId, clubId, StringComparison.OrdinalIgnoreCase)))) == true;
        }

        public const string FinalOpponentToken = "{決賽對手}";

        public static string ApplyBodyTokens(string body, string finalOpponentDisplayName)
        {
            if (string.IsNullOrEmpty(body) || body.IndexOf(FinalOpponentToken, StringComparison.Ordinal) < 0)
                return body ?? string.Empty;
            string name = string.IsNullOrWhiteSpace(finalOpponentDisplayName)
                ? "對手"
                : finalOpponentDisplayName.Trim();
            return body.Replace(FinalOpponentToken, name);
        }

        /// <summary>
        /// EU4-style daily chance ≈ 1 / mtthDays. Evaluated once per week for demo pacing.
        /// </summary>
        public static bool RollMtth(CareerScriptedEventDef def, SeededRandom random, int daysInStep)
        {
            if (def == null)
                return false;
            int mtth = Math.Max(1, def.mtthDays);
            double p = 1d - Math.Pow(1d - (1d / mtth), Math.Max(1, daysInStep));
            return random.Next01() < p;
        }

        public static CareerScriptedEventDef PickDueScheduledEvent(
            IEnumerable<CareerScriptedEventDef> defs,
            CareerState state,
            ClubState club,
            IReadOnlyDictionary<string, int> fireHourOverrides = null)
        {
            if (defs == null || state == null || club == null)
                return null;

            var unlocked = state.unlockedAchievements ?? new List<string>();
            CareerScriptedEventDef best = null;
            int bestHour = int.MaxValue;
            foreach (CareerScriptedEventDef def in defs)
            {
                if (def == null || string.IsNullOrWhiteSpace(def.id))
                    continue;
                int fireHour = ScriptedEventScheduleRules.ResolveFireHour(def, fireHourOverrides);
                if (fireHour <= 0)
                    continue;
                if (state.clockHour < fireHour)
                    continue;
                if (CareerFlagRules.HasFlag(
                        state.flags, ScriptedEventTierRules.FiredFlagId(def.id), CareerFlagRules.GlobalScope, state.clockHour))
                    continue;
                if (state.pendingEvents != null &&
                    state.pendingEvents.Any(x =>
                        string.Equals(x.eventId, def.id, StringComparison.OrdinalIgnoreCase)))
                    continue;
                if (!MatchesAll(def.triggers, state, club, unlocked))
                    continue;
                if (best == null || fireHour < bestHour ||
                    (fireHour == bestHour &&
                     string.Compare(def.id, best.id, StringComparison.OrdinalIgnoreCase) < 0))
                {
                    best = def;
                    bestHour = fireHour;
                }
            }

            return best;
        }

        public static CareerScriptedEventDef PickEligibleEvent(
            IEnumerable<CareerScriptedEventDef> defs,
            CareerState state,
            ClubState club,
            SeededRandom random,
            int daysInStep,
            IReadOnlyDictionary<string, int> fireHourOverrides = null)
        {
            if (defs == null || state == null || club == null)
                return null;

            CareerScriptedEventDef scheduled = PickDueScheduledEvent(defs, state, club, fireHourOverrides);
            if (scheduled != null)
                return scheduled;

            var unlocked = state.unlockedAchievements ?? new List<string>();
            var eligible = new List<CareerScriptedEventDef>();
            foreach (CareerScriptedEventDef def in defs)
            {
                if (def == null || string.IsNullOrWhiteSpace(def.id))
                    continue;
                if (ScriptedEventScheduleRules.ResolveFireHour(def, fireHourOverrides) > 0)
                    continue;
                if (state.pendingEvents != null &&
                    state.pendingEvents.Any(x =>
                        string.Equals(x.eventId, def.id, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(x.clubId, club.clubId, StringComparison.OrdinalIgnoreCase)))
                    continue;
                if (CareerFlagRules.HasFlag(
                        state.flags, ScriptedEventTierRules.FiredFlagId(def.id), CareerFlagRules.GlobalScope, state.clockHour))
                    continue;
                if (!MatchesAll(def.triggers, state, club, unlocked))
                    continue;
                if (!RollMtth(def, random, daysInStep))
                    continue;
                eligible.Add(def);
            }

            if (eligible.Count == 0)
                return null;
            int index = Math.Min(eligible.Count - 1, (int)(random.Next01() * eligible.Count));
            return eligible[index];
        }
    }

    /// <summary>
    /// Same-calendar-day scheduled events get randomized hours with a minimum gap.
    /// </summary>
    public static class ScriptedEventScheduleRules
    {
        public const int MinSameDayGapHours = 3;

        public static int ResolveFireHour(
            CareerScriptedEventDef def,
            IReadOnlyDictionary<string, int> fireHourOverrides)
        {
            if (def == null)
                return 0;
            if (fireHourOverrides != null &&
                !string.IsNullOrWhiteSpace(def.id) &&
                fireHourOverrides.TryGetValue(def.id, out int overrideHour))
                return overrideHour;
            return def.fireHour;
        }

        public static Dictionary<string, int> BuildSpreadFireHours(
            IEnumerable<CareerScriptedEventDef> defs,
            int seed)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (defs == null)
                return map;

            IEnumerable<IGrouping<int, CareerScriptedEventDef>> groups = defs
                .Where(d => d != null && !string.IsNullOrWhiteSpace(d.id) && d.fireHour > 0)
                .GroupBy(d => CommunityOperationsRules.DayIndex(d.fireHour));

            foreach (IGrouping<int, CareerScriptedEventDef> group in groups)
            {
                List<CareerScriptedEventDef> list = group
                    .OrderBy(d => d.id, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                int dayStart = group.Key * CareerCalendar.HoursPerDay;

                if (list.Count == 1)
                {
                    map[list[0].id] = list[0].fireHour;
                    continue;
                }

                var random = new SeededRandom(unchecked(seed + group.Key * 104729 + 31));
                int[] hoursOfDay = AllocateHoursInDay(list.Count, MinSameDayGapHours, random);
                Array.Sort(hoursOfDay);

                var order = new List<int>(list.Count);
                for (int i = 0; i < list.Count; i++)
                    order.Add(i);
                Shuffle(order, random);

                for (int i = 0; i < list.Count; i++)
                    map[list[order[i]].id] = dayStart + hoursOfDay[i];
            }

            return map;
        }

        /// <summary>
        /// Pick <paramref name="count"/> hour-of-day values in 0..23 with pairwise gaps
        /// of at least <paramref name="minGap"/> when possible.
        /// </summary>
        public static int[] AllocateHoursInDay(int count, int minGap, IRandomSource random)
        {
            if (count <= 0)
                return Array.Empty<int>();

            int dayHours = CareerCalendar.HoursPerDay;
            if (count == 1)
                return new[] { random != null ? random.Range(0, dayHours) : 0 };

            int gap = Math.Max(1, minGap);
            while (gap > 1 && dayHours - (count - 1) * gap < count)
                gap--;

            int span = dayHours - (count - 1) * gap;
            if (span < count || random == null)
            {
                var packed = new int[count];
                int packGap = Math.Max(1, Math.Min(gap, (dayHours - 1) / Math.Max(1, count - 1)));
                for (int i = 0; i < count; i++)
                    packed[i] = Math.Min(dayHours - 1, i * packGap);
                return packed;
            }

            var pool = new List<int>(span);
            for (int i = 0; i < span; i++)
                pool.Add(i);
            Shuffle(pool, random);

            int[] chosen = pool.Take(count).OrderBy(x => x).ToArray();
            var hours = new int[count];
            for (int i = 0; i < count; i++)
                hours[i] = chosen[i] + i * gap;
            return hours;
        }

        static void Shuffle(List<int> list, IRandomSource random)
        {
            if (list == null || random == null)
                return;
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Range(0, i + 1);
                int tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }
    }
}
