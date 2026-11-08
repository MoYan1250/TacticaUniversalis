using System;
using System.Collections.Generic;
using System.Linq;

namespace TacticaUniversalis.Domain
{
    public enum ClubEstateId
    {
        Board,
        Management,
        CoachingStaff,
        StarPlayers,
        Veterans,
        SupportStaff,
        Reserves,
        PublicOpinion
    }

    public sealed class ClubEstateDef
    {
        public ClubEstateId Id;
        public string Title;
        public string ShortLabel;
        public string Description;
        public string UnlockHint;
    }

    public sealed class ClubEstateStatus
    {
        public ClubEstateDef Def;
        public bool Formed;
        public string StatusLabel;
        public int Satisfaction;
        public ClubEstateSatisfactionStance Stance;
    }

    [Serializable]
    public sealed class ClubEstateSatisfactionState
    {
        public string estateId;
        public int value = 50;
    }

    public enum ClubEstateSatisfactionStance
    {
        Neutral,
        Angry,
        Loyal
    }

    public readonly struct ClubEstateActiveModifier
    {
        public ClubEstateActiveModifier(bool buff, string id, string title, string description)
        {
            Buff = buff;
            Id = id;
            Title = title;
            Description = description;
        }

        public bool Buff { get; }
        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
    }

    public static class ClubEstateSatisfactionRules
    {
        public const int LowThreshold = 30;
        public const int HighThreshold = 70;

        public static ClubEstateSatisfactionStance GetStance(int satisfaction)
        {
            if (satisfaction < LowThreshold)
                return ClubEstateSatisfactionStance.Angry;
            if (satisfaction > HighThreshold)
                return ClubEstateSatisfactionStance.Loyal;
            return ClubEstateSatisfactionStance.Neutral;
        }

        public static bool TryParseEstateId(string key, out ClubEstateId id)
        {
            id = default;
            if (string.IsNullOrWhiteSpace(key))
                return false;
            string normalized = key.Trim().ToLowerInvariant().Replace("-", "_");
            switch (normalized)
            {
                case "board":
                    id = ClubEstateId.Board;
                    return true;
                case "management":
                    id = ClubEstateId.Management;
                    return true;
                case "coaching_staff":
                case "coaching":
                    id = ClubEstateId.CoachingStaff;
                    return true;
                case "star_players":
                case "stars":
                    id = ClubEstateId.StarPlayers;
                    return true;
                case "veterans":
                    id = ClubEstateId.Veterans;
                    return true;
                case "support_staff":
                case "support":
                    id = ClubEstateId.SupportStaff;
                    return true;
                case "reserves":
                    id = ClubEstateId.Reserves;
                    return true;
                case "public_opinion":
                case "public":
                case "fans":
                    id = ClubEstateId.PublicOpinion;
                    return true;
                default:
                    return Enum.TryParse(key, true, out id);
            }
        }

        public static string ToStorageId(ClubEstateId id) =>
            id switch
            {
                ClubEstateId.CoachingStaff => "coaching_staff",
                ClubEstateId.StarPlayers => "star_players",
                ClubEstateId.SupportStaff => "support_staff",
                ClubEstateId.PublicOpinion => "public_opinion",
                _ => id.ToString().ToLowerInvariant()
            };

        public static int DefaultValue(ClubEstateId id) =>
            id switch
            {
                ClubEstateId.Board => 60,
                ClubEstateId.Management => 55,
                ClubEstateId.CoachingStaff => 52,
                ClubEstateId.StarPlayers => 58,
                ClubEstateId.Veterans => 50,
                ClubEstateId.SupportStaff => 50,
                ClubEstateId.Reserves => 48,
                ClubEstateId.PublicOpinion => 50,
                _ => 50
            };

        public static int Clamp(int value) => Math.Max(0, Math.Min(100, value));

        public static void EnsureNormalized(ClubState club)
        {
            if (club == null)
                return;
            club.estateSatisfactions = club.estateSatisfactions ?? new List<ClubEstateSatisfactionState>();

            int boardLegacy = Clamp(club.boardSatisfaction);
            ClubEstateSatisfactionState boardEntry = FindEntry(club, ClubEstateId.Board);
            if (boardEntry == null)
            {
                club.estateSatisfactions.Add(new ClubEstateSatisfactionState
                {
                    estateId = ToStorageId(ClubEstateId.Board),
                    value = boardLegacy
                });
            }
            else
            {
                boardEntry.value = Clamp(boardEntry.value);
                if (boardEntry.value == 50 && boardLegacy != 60)
                    boardEntry.value = boardLegacy;
            }

            foreach (ClubEstateDef def in ClubEstateRules.All)
            {
                if (FindEntry(club, def.Id) != null)
                    continue;
                club.estateSatisfactions.Add(new ClubEstateSatisfactionState
                {
                    estateId = ToStorageId(def.Id),
                    value = DefaultValue(def.Id)
                });
            }

            SyncBoardLegacy(club);
        }

        public static int Get(ClubState club, ClubEstateId id)
        {
            if (club == null)
                return DefaultValue(id);
            EnsureNormalized(club);
            ClubEstateSatisfactionState entry = FindEntry(club, id);
            return entry == null ? DefaultValue(id) : Clamp(entry.value);
        }

        public static void Set(ClubState club, ClubEstateId id, int value)
        {
            if (club == null)
                return;
            EnsureNormalized(club);
            value = Clamp(value);
            ClubEstateSatisfactionState entry = FindEntry(club, id);
            if (entry == null)
            {
                club.estateSatisfactions.Add(new ClubEstateSatisfactionState
                {
                    estateId = ToStorageId(id),
                    value = value
                });
            }
            else
                entry.value = value;

            SyncBoardLegacy(club);
        }

        public static void ApplyDelta(ClubState club, ClubEstateId id, int delta)
        {
            if (delta == 0 || club == null)
                return;
            Set(club, id, Get(club, id) + delta);
        }

        public static void SyncBoardLegacy(ClubState club)
        {
            if (club == null)
                return;
            ClubEstateSatisfactionState entry = FindEntry(club, ClubEstateId.Board);
            club.boardSatisfaction = entry == null
                ? Clamp(club.boardSatisfaction)
                : Clamp(entry.value);
        }

        public static bool TryGetActiveModifier(
            ClubEstateId id,
            bool formed,
            ClubEstateSatisfactionStance stance,
            out ClubEstateActiveModifier modifier)
        {
            modifier = default;
            if (!formed || stance == ClubEstateSatisfactionStance.Neutral)
                return false;

            bool buff = stance == ClubEstateSatisfactionStance.Loyal;
            switch (id)
            {
                case ClubEstateId.Board:
                    modifier = buff
                        ? new ClubEstateActiveModifier(true, "est_board_loyal", "董事會背書",
                            "長期投資與設施升級容忍度提高。")
                        : new ClubEstateActiveModifier(false, "est_board_angry", "董事會掣肘",
                            "重大引援與高薪決策更易遭質疑。");
                    return true;
                case ClubEstateId.Management:
                    modifier = buff
                        ? new ClubEstateActiveModifier(true, "est_mgmt_loyal", "管理協同",
                            "贊助與轉會窗口的資源調度較順暢。")
                        : new ClubEstateActiveModifier(false, "est_mgmt_angry", "管理內耗",
                            "部門協調成本上升，決策延宕。");
                    return true;
                case ClubEstateId.CoachingStaff:
                    modifier = buff
                        ? new ClubEstateActiveModifier(true, "est_coach_loyal", "教練體系穩定",
                            "訓練效率 +8%。")
                        : new ClubEstateActiveModifier(false, "est_coach_angry", "教練離心",
                            "訓練效率 −8%。");
                    return true;
                case ClubEstateId.StarPlayers:
                    modifier = buff
                        ? new ClubEstateActiveModifier(true, "est_star_loyal", "核心帶頭",
                            "商業與士氣加成；關鍵場面穩定度提升。")
                        : new ClubEstateActiveModifier(false, "est_star_angry", "核心不滿",
                            "更衣室風險上升，媒體易放大矛盾。");
                    return true;
                case ClubEstateId.Veterans:
                    modifier = buff
                        ? new ClubEstateActiveModifier(true, "est_vet_loyal", "老將定海神針",
                            "隊伍紀律與年輕選手心態較穩。")
                        : new ClubEstateActiveModifier(false, "est_vet_angry", "老將離心",
                            "更衣室話語權衝突，紀律事件機率上升。");
                    return true;
                case ClubEstateId.SupportStaff:
                    modifier = buff
                        ? new ClubEstateActiveModifier(true, "est_support_loyal", "後勤到位",
                            "選手能量恢復 +10%。")
                        : new ClubEstateActiveModifier(false, "est_support_angry", "後勤失序",
                            "選手能量恢復 −10%。");
                    return true;
                case ClubEstateId.Reserves:
                    modifier = buff
                        ? new ClubEstateActiveModifier(true, "est_reserve_loyal", "梯隊暢通",
                            "青訓提拔與替補心態積極。")
                        : new ClubEstateActiveModifier(false, "est_reserve_angry", "梯隊積壓",
                            "替補與青訓選手離心，輪換意願下降。");
                    return true;
                case ClubEstateId.PublicOpinion:
                    modifier = buff
                        ? new ClubEstateActiveModifier(true, "est_public_loyal", "輿論順風",
                            "粉絲與贊助曝光收益 +5%。")
                        : new ClubEstateActiveModifier(false, "est_public_angry", "輿論逆風",
                            "贊助滿意度更易下滑，負面事件放大。");
                    return true;
                default:
                    return false;
            }
        }

        public static void AppendModifierHints(
            ClubState club,
            CareerState state,
            CareerBalanceDefinition balance,
            CareerClubDefinition clubDef,
            IEnumerable<PlayerCareerState> roster,
            List<(bool buff, string id, string title, string description)> hints)
        {
            if (club == null || hints == null)
                return;

            IReadOnlyList<ClubEstateStatus> estates = ClubEstateRules.Evaluate(
                balance, state, club, clubDef, roster);
            foreach (ClubEstateStatus estate in estates)
            {
                if (estate == null || !estate.Formed)
                    continue;
                int sat = Get(club, estate.Def.Id);
                var stance = GetStance(sat);
                if (!TryGetActiveModifier(estate.Def.Id, true, stance, out ClubEstateActiveModifier mod))
                    continue;
                hints.Add((mod.Buff, mod.Id, $"{estate.Def.Title} · {sat}%", mod.Description));
            }
        }

        public static double GetTrainingEfficiencyMultiplier(
            CareerState state,
            ClubState club,
            CareerBalanceDefinition balance,
            CareerClubDefinition clubDef,
            IEnumerable<PlayerCareerState> roster)
        {
            if (club == null || state == null)
                return 1d;
            IReadOnlyList<ClubEstateStatus> estates = ClubEstateRules.Evaluate(
                balance, state, club, clubDef, roster);
            ClubEstateStatus coaching = estates.FirstOrDefault(x => x.Def.Id == ClubEstateId.CoachingStaff);
            if (coaching == null || !coaching.Formed)
                return 1d;
            return GetStance(Get(club, ClubEstateId.CoachingStaff)) switch
            {
                ClubEstateSatisfactionStance.Loyal => 1.08d,
                ClubEstateSatisfactionStance.Angry => 0.92d,
                _ => 1d
            };
        }

        public static double GetMerchandiseMultiplier(
            CareerState state,
            ClubState club,
            CareerBalanceDefinition balance,
            CareerClubDefinition clubDef,
            IEnumerable<PlayerCareerState> roster)
        {
            if (club == null || state == null)
                return 1d;
            IReadOnlyList<ClubEstateStatus> estates = ClubEstateRules.Evaluate(
                balance, state, club, clubDef, roster);
            ClubEstateStatus opinion = estates.FirstOrDefault(x => x.Def.Id == ClubEstateId.PublicOpinion);
            if (opinion == null || !opinion.Formed)
                return 1d;
            return GetStance(Get(club, ClubEstateId.PublicOpinion)) switch
            {
                ClubEstateSatisfactionStance.Loyal => 1.05d,
                ClubEstateSatisfactionStance.Angry => 0.95d,
                _ => 1d
            };
        }

        public static double GetEnergyRecoveryMultiplier(
            CareerState state,
            ClubState club,
            CareerBalanceDefinition balance,
            CareerClubDefinition clubDef,
            IEnumerable<PlayerCareerState> roster)
        {
            if (club == null || state == null)
                return 1d;
            IReadOnlyList<ClubEstateStatus> estates = ClubEstateRules.Evaluate(
                balance, state, club, clubDef, roster);
            ClubEstateStatus support = estates.FirstOrDefault(x => x.Def.Id == ClubEstateId.SupportStaff);
            if (support == null || !support.Formed)
                return 1d;
            return GetStance(Get(club, ClubEstateId.SupportStaff)) switch
            {
                ClubEstateSatisfactionStance.Loyal => 1.10d,
                ClubEstateSatisfactionStance.Angry => 0.90d,
                _ => 1d
            };
        }

        public static void ApplyMonthlyFinancialPulse(ClubState club, FinancialHealth health)
        {
            if (club == null)
                return;
            if (health == FinancialHealth.Wealthy)
                ApplyDelta(club, ClubEstateId.Board, 2);
            if (health >= FinancialHealth.Deficit)
            {
                ApplyDelta(club, ClubEstateId.Board, -4);
                ApplyDelta(club, ClubEstateId.Management, -2);
            }
            if (health == FinancialHealth.Crisis)
                ApplyDelta(club, ClubEstateId.PublicOpinion, -2);
        }

        public static void OnFacilityUpgraded(ClubState club, FacilityType type, int newLevel)
        {
            if (club == null || newLevel <= 1)
                return;
            switch (type)
            {
                case FacilityType.Training:
                    ApplyDelta(club, ClubEstateId.CoachingStaff, 5);
                    break;
                case FacilityType.Medical:
                    ApplyDelta(club, ClubEstateId.SupportStaff, 5);
                    break;
                case FacilityType.Academy:
                    ApplyDelta(club, ClubEstateId.Reserves, 4);
                    break;
            }
        }

        private static ClubEstateSatisfactionState FindEntry(ClubState club, ClubEstateId id)
        {
            string want = ToStorageId(id);
            return club.estateSatisfactions?.FirstOrDefault(x =>
                x != null && string.Equals(x.estateId, want, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// EU4-style club estates / interest groups. Presence is conditional;
    /// NAVI and Gambit always have the full set for demo balance.
    /// </summary>
    public static class ClubEstateRules
    {
        public const string SquadStarter = "starter";
        public const string SquadBench = "bench";
        public const string SquadAcademy = "academy";
        public const string SquadPromoted = "promoted";

        public const int VeteranYearsSinceDebut = 7;
        public const int CoachFacilityMinLevel = 2;
        public const int SupportFacilityMinLevel = 2;
        public const int AcademyReserveMinLevel = 2;
        public const int LargeClubFameMin = 55;
        public const int LargeClubRankMax = 12;
        public const int StarFameGap = 15;
        public const int StarPowerGap = 12;

        private static readonly string[] FullEstateClubIds =
        {
            "navi_2021",
            "gambit_2021"
        };

        private static readonly ClubEstateDef[] Catalog =
        {
            new ClubEstateDef
            {
                Id = ClubEstateId.Board,
                Title = "董事會",
                ShortLabel = "最高決策",
                Description =
                    "這並非你的同僚，而是你必須匯報的對象。他們掌握著預算審批與重大人事任免的權力，大型俱樂部或上市公司體系中的最高決策機構。",
                UnlockHint = "需為大型俱樂部（高名氣／高排名）或上市公司。"
            },
            new ClubEstateDef
            {
                Id = ClubEstateId.Management,
                Title = "管理層",
                ShortLabel = "資源中樞",
                Description =
                    "涵蓋你本人、總經理、營運總監、商務總監等。他們是資源調度的中樞，負責將各部門的需求轉化為可執行的計畫與預算。",
                UnlockHint = "所有俱樂部皆有。"
            },
            new ClubEstateDef
            {
                Id = ClubEstateId.CoachingStaff,
                Title = "教練組",
                ShortLabel = "作戰參謀",
                Description =
                    "他們是作戰參謀部，負責將選手的個人能力轉化為體系戰鬥力。涵蓋主教練、助理教練、戰術分析師，一切戰術紀律的制定者與監督者。",
                UnlockHint = "於基地將「訓練／教練組」升至 Lv.2，或聘用主教練／助教／分析師。"
            },
            new ClubEstateDef
            {
                Id = ClubEstateId.StarPlayers,
                Title = "明星選手",
                ShortLabel = "核心資產",
                Description =
                    "他們是俱樂部的重要資產，戰績與商業價值的雙重引擎。擁有改寫比賽走向的能力，也是賽場上最具威懾力的存在。",
                UnlockHint = "隊內需有一人名氣或實力明顯高於其餘隊友。"
            },
            new ClubEstateDef
            {
                Id = ClubEstateId.Veterans,
                Title = "老將",
                ShortLabel = "更衣室權威",
                Description =
                    "操作或許不在巔峰，但比賽閱讀能力與穩定性極強。他們具備更衣室內的權威性，是隊伍秩序的維護者，也是年輕選手的導師。",
                UnlockHint = "隊內需有出道滿七年的選手。"
            },
            new ClubEstateDef
            {
                Id = ClubEstateId.SupportStaff,
                Title = "後勤組",
                ShortLabel = "狀態放大",
                Description =
                    "他們是選手個體效率的放大器。不直接產出戰術，但確保選手能以百分之百的狀態執行戰術。涵蓋心理師、營養師、物理治療師等專業人員。",
                UnlockHint = "於基地將「醫療／後勤組」升至 Lv.2（代表已聘用後勤團隊）。"
            },
            new ClubEstateDef
            {
                Id = ClubEstateId.Reserves,
                Title = "儲備力量",
                ShortLabel = "未來期權",
                Description =
                    "他們是俱樂部的未來期權。成本低廉、潛力高昂，但即戰力尚不確定。涵蓋替補選手、青訓隊員、剛從青訓提拔的新人。",
                UnlockHint = "需有 Lv.2 青訓，或選手頁存在替補／青訓／剛提拔新人。"
            },
            new ClubEstateDef
            {
                Id = ClubEstateId.PublicOpinion,
                Title = "輿論",
                ShortLabel = "粉絲監督",
                Description =
                    "在現代大型俱樂部中，粉絲早已不是單純的觀眾。他們透過超話、論壇、直播彈幕，構成了一股強大的輿論監督力量——足以影響贊助商觀感，甚至動搖管理層的決策。",
                UnlockHint = "所有俱樂部皆有。"
            }
        };

        public static IReadOnlyList<ClubEstateDef> All => Catalog;

        public static string DisplayTitle(string estateId)
        {
            if (!ClubEstateSatisfactionRules.TryParseEstateId(estateId, out ClubEstateId id))
                return string.IsNullOrWhiteSpace(estateId) ? "組織" : estateId;
            foreach (ClubEstateDef def in Catalog)
            {
                if (def.Id == id)
                    return def.Title;
            }

            return estateId;
        }

        public static bool IsFullEstateClub(string clubId) =>
            !string.IsNullOrWhiteSpace(clubId) &&
            FullEstateClubIds.Any(id => string.Equals(id, clubId, StringComparison.OrdinalIgnoreCase));

        public static bool IsLargeClub(CareerClubDefinition def, ClubState club)
        {
            if (club == null && def == null)
                return false;
            int fame = club?.fame ?? def?.fame ?? 0;
            if (fame >= LargeClubFameMin)
                return true;
            int rank = def?.hltvRank ?? 0;
            return rank > 0 && rank <= LargeClubRankMax;
        }

        public static bool IsPublicCompany(CareerClubDefinition def) =>
            def != null && def.isPublicCompany;

        public static int YearsSinceDebut(PlayerCareerState player, int currentYear)
        {
            if (player == null)
                return 0;
            if (player.debutYear > 0)
                return Math.Max(0, currentYear - player.debutYear);
            // Legacy saves: experience may already encode pro years.
            return Math.Max(0, player.experience);
        }

        public static bool IsReserveSquadStatus(string squadStatus)
        {
            if (string.IsNullOrWhiteSpace(squadStatus))
                return false;
            return string.Equals(squadStatus, SquadBench, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(squadStatus, SquadAcademy, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(squadStatus, SquadPromoted, StringComparison.OrdinalIgnoreCase);
        }

        public static int FacilityLevel(ClubState club, FacilityType type)
        {
            if (club?.facilities == null)
                return 0;
            FacilityState facility = club.facilities.FirstOrDefault(x => x != null && x.type == type);
            return facility == null || facility.disabled ? 0 : Math.Max(0, facility.level);
        }

        public static IReadOnlyList<ClubEstateStatus> Evaluate(
            CareerBalanceDefinition balance,
            CareerState state,
            ClubState club,
            CareerClubDefinition clubDef,
            IEnumerable<PlayerCareerState> roster)
        {
            var list = new List<ClubEstateStatus>(Catalog.Length);
            bool forceAll = IsFullEstateClub(club?.clubId ?? clubDef?.id);
            PlayerCareerState[] players = (roster ?? Array.Empty<PlayerCareerState>())
                .Where(p => p != null)
                .ToArray();
            int year = balance != null && state != null
                ? CareerCalendar.ToDateTime(balance, state).Year
                : state?.year ?? balance?.startingYear ?? 2021;

            foreach (ClubEstateDef def in Catalog)
            {
                bool formed = forceAll || IsFormed(def.Id, state, club, clubDef, players, year);
                int satisfaction = club != null
                    ? ClubEstateSatisfactionRules.Get(club, def.Id)
                    : ClubEstateSatisfactionRules.DefaultValue(def.Id);
                list.Add(new ClubEstateStatus
                {
                    Def = def,
                    Formed = formed,
                    StatusLabel = formed ? def.ShortLabel : "未形成",
                    Satisfaction = satisfaction,
                    Stance = ClubEstateSatisfactionRules.GetStance(satisfaction)
                });
            }
            return list;
        }

        private static bool IsFormed(
            ClubEstateId id,
            CareerState state,
            ClubState club,
            CareerClubDefinition clubDef,
            PlayerCareerState[] roster,
            int currentYear)
        {
            switch (id)
            {
                case ClubEstateId.Management:
                case ClubEstateId.PublicOpinion:
                    return true;
                case ClubEstateId.Board:
                    return IsPublicCompany(clubDef) || IsLargeClub(clubDef, club);
                case ClubEstateId.CoachingStaff:
                    return FacilityLevel(club, FacilityType.Training) >= CoachFacilityMinLevel ||
                           ClubCoachStaffRules.HasActiveCoachOnClub(state, club?.clubId);
                case ClubEstateId.SupportStaff:
                    return FacilityLevel(club, FacilityType.Medical) >= SupportFacilityMinLevel;
                case ClubEstateId.Veterans:
                    return roster.Any(p => YearsSinceDebut(p, currentYear) >= VeteranYearsSinceDebut);
                case ClubEstateId.StarPlayers:
                    return HasStarPlayer(roster);
                case ClubEstateId.Reserves:
                    return FacilityLevel(club, FacilityType.Academy) >= AcademyReserveMinLevel ||
                           roster.Any(p => IsReserveSquadStatus(p.squadStatus));
                default:
                    return false;
            }
        }

        private static bool HasStarPlayer(PlayerCareerState[] roster)
        {
            if (roster == null || roster.Length < 2)
                return roster != null && roster.Length == 1 &&
                       (roster[0].fame >= 70 || roster[0].power >= 80);

            int[] fame = roster.Select(p => p.fame).OrderByDescending(x => x).ToArray();
            int[] power = roster.Select(p => p.power).OrderByDescending(x => x).ToArray();
            if (fame[0] - fame[1] >= StarFameGap)
                return true;
            if (power[0] - power[1] >= StarPowerGap)
                return true;
            return false;
        }

        public static int ResolveDebutYear(CareerPlayerDefinition definition, CareerBalanceDefinition balance)
        {
            if (definition == null)
                return 0;
            if (definition.debutYear > 0)
                return definition.debutYear;
            int startYear = balance?.startingYear ?? 2021;
            // Estimate: most pros debut around age 17 if not authored.
            return startYear - Math.Max(1, definition.age - 17);
        }

        public static string NormalizeSquadStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return SquadStarter;
            string s = status.Trim().ToLowerInvariant();
            if (s == SquadBench || s == SquadAcademy || s == SquadPromoted || s == SquadStarter)
                return s;
            return SquadStarter;
        }
    }

    [Serializable]
    public sealed class ClubEstateResolutionDef
    {
        public string id;
        public string title;
        public string description;
        public int cashCost;
        public string cooldownFlag;
        public int cooldownDays = 120;
        public ClubEstateResolutionEffect[] effects = Array.Empty<ClubEstateResolutionEffect>();
    }

    [Serializable]
    public sealed class ClubEstateResolutionEffect
    {
        public string estateId;
        public int satisfactionDelta;
    }

    public sealed class ClubEstateResolutionStatus
    {
        public ClubEstateResolutionDef Def;
        public bool CanEnact;
        public string BlockReason;
    }

    public static class ClubEstateResolutionRules
    {
        private static readonly ClubEstateResolutionDef[] Catalog =
        {
            new ClubEstateResolutionDef
            {
                id = "invest_coaching",
                title = "加強教練預算",
                description = "董事會批准額外訓練資源；教練組士氣上升，但短期現金壓力增加。",
                cashCost = 35000,
                cooldownFlag = "cd_res_invest_coaching",
                cooldownDays = 150,
                effects = new[]
                {
                    new ClubEstateResolutionEffect { estateId = "coaching_staff", satisfactionDelta = 12 },
                    new ClubEstateResolutionEffect { estateId = "board", satisfactionDelta = -3 }
                }
            },
            new ClubEstateResolutionDef
            {
                id = "fan_engagement",
                title = "球迷見面週",
                description = "舉辦公開活動與直播互動，提升輿論與明星選手曝光，管理層需額外協調。",
                cashCost = 18000,
                cooldownFlag = "cd_res_fan_engagement",
                cooldownDays = 120,
                effects = new[]
                {
                    new ClubEstateResolutionEffect { estateId = "public_opinion", satisfactionDelta = 10 },
                    new ClubEstateResolutionEffect { estateId = "star_players", satisfactionDelta = 5 },
                    new ClubEstateResolutionEffect { estateId = "management", satisfactionDelta = -4 }
                }
            },
            new ClubEstateResolutionDef
            {
                id = "salary_discipline",
                title = "薪資紀律方案",
                description = "凍結非核心加薪並審視合約；董事會滿意，但更衣室壓力上升。",
                cashCost = 0,
                cooldownFlag = "cd_res_salary_discipline",
                cooldownDays = 180,
                effects = new[]
                {
                    new ClubEstateResolutionEffect { estateId = "board", satisfactionDelta = 8 },
                    new ClubEstateResolutionEffect { estateId = "star_players", satisfactionDelta = -10 },
                    new ClubEstateResolutionEffect { estateId = "veterans", satisfactionDelta = -5 }
                }
            },
            new ClubEstateResolutionDef
            {
                id = "youth_push",
                title = "押注青訓提拔",
                description = "公開承諾給予梯隊機會；儲備與輿論振奮，老將與當打核心可能疑慮。",
                cashCost = 12000,
                cooldownFlag = "cd_res_youth_push",
                cooldownDays = 160,
                effects = new[]
                {
                    new ClubEstateResolutionEffect { estateId = "reserves", satisfactionDelta = 14 },
                    new ClubEstateResolutionEffect { estateId = "public_opinion", satisfactionDelta = 4 },
                    new ClubEstateResolutionEffect { estateId = "veterans", satisfactionDelta = -6 },
                    new ClubEstateResolutionEffect { estateId = "star_players", satisfactionDelta = -4 }
                }
            }
        };

        public static IReadOnlyList<ClubEstateResolutionDef> All => Catalog;

        public static IReadOnlyList<ClubEstateResolutionStatus> Evaluate(
            CareerState state,
            ClubState club,
            CareerBalanceDefinition balance,
            CareerClubDefinition clubDef,
            IEnumerable<PlayerCareerState> roster)
        {
            var list = new List<ClubEstateResolutionStatus>(Catalog.Length);
            if (club == null || state == null)
                return list;

            IReadOnlyList<ClubEstateStatus> estates = ClubEstateRules.Evaluate(
                balance, state, club, clubDef, roster);

            foreach (ClubEstateResolutionDef def in Catalog)
            {
                string block = null;
                if (def.cashCost > 0 && club.cash < def.cashCost)
                    block = $"現金不足（需 ${def.cashCost:N0}）";
                else if (!string.IsNullOrWhiteSpace(def.cooldownFlag) &&
                         CareerFlagRules.HasFlag(
                             state.flags, def.cooldownFlag, club.clubId, state.clockHour))
                    block = "冷卻中";
                else if (def.effects != null)
                {
                    foreach (ClubEstateResolutionEffect effect in def.effects)
                    {
                        if (effect == null || effect.satisfactionDelta >= 0)
                            continue;
                        if (!ClubEstateSatisfactionRules.TryParseEstateId(
                                effect.estateId, out ClubEstateId estateId))
                            continue;
                        ClubEstateStatus estate = estates.FirstOrDefault(x => x.Def.Id == estateId);
                        if (estate != null && !estate.Formed)
                        {
                            block = "相關組織尚未形成";
                            break;
                        }
                    }
                }

                list.Add(new ClubEstateResolutionStatus
                {
                    Def = def,
                    CanEnact = block == null,
                    BlockReason = block
                });
            }

            return list;
        }

        public static bool TryEnact(
            CareerState state,
            ClubState club,
            string resolutionId,
            out string failure)
        {
            failure = null;
            if (state == null || club == null)
            {
                failure = "無效狀態";
                return false;
            }

            ClubEstateResolutionDef def = Catalog.FirstOrDefault(x =>
                string.Equals(x.id, resolutionId, StringComparison.OrdinalIgnoreCase));
            if (def == null)
            {
                failure = "未知決議";
                return false;
            }

            if (def.cashCost > 0 && club.cash < def.cashCost)
            {
                failure = "現金不足";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(def.cooldownFlag) &&
                CareerFlagRules.HasFlag(state.flags, def.cooldownFlag, club.clubId, state.clockHour))
            {
                failure = "冷卻中";
                return false;
            }

            if (def.cashCost > 0)
                club.cash -= def.cashCost;

            if (def.effects != null)
            {
                foreach (ClubEstateResolutionEffect effect in def.effects)
                {
                    if (effect == null || effect.satisfactionDelta == 0)
                        continue;
                    if (!ClubEstateSatisfactionRules.TryParseEstateId(
                            effect.estateId, out ClubEstateId estateId))
                        continue;
                    ClubEstateSatisfactionRules.ApplyDelta(club, estateId, effect.satisfactionDelta);
                }
            }

            if (!string.IsNullOrWhiteSpace(def.cooldownFlag))
            {
                CareerFlagRules.SetFlag(
                    state.flags, def.cooldownFlag, club.clubId, state.clockHour, def.cooldownDays);
            }

            return true;
        }
    }
}
