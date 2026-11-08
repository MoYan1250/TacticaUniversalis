using System;
using System.Collections.Generic;
using System.Linq;

namespace TacticaUniversalis.Domain
{
    [Serializable]
    public sealed class CommunityPostLogEntry
    {
        public string templateId;
        public int clockHour;
        public int fameDelta;
        public int exposureDelta;
        public int fansDelta;
        public string featuredPlayerId;
    }

    public sealed class CommunityPostTemplate
    {
        public string id;
        public string title;
        public string description;
        public int cashCost;
        public int fameDelta;
        public int exposureDelta;
        public int fansDelta;
        /// <summary>Energy deducted from each targeted roster player (0 = none).</summary>
        public int playerEnergyCost;
        /// <summary>How many players pay energy (spotlight=1, behind scenes=up to 3 starters).</summary>
        public int playerEnergyTargetCount;
        public int minStarterTargets;
    }

    public static class CommunityOperationsRules
    {
        public const int MaxPostsPerDay = 2;
        public const string DailyUpdateTemplateId = "daily_update";

        private static readonly CommunityPostTemplate[] Templates =
        {
            new CommunityPostTemplate
            {
                id = "daily_update",
                title = "日常動態",
                description = "發布訓練與幕後動態，維持社群關注。",
                cashCost = 0,
                fameDelta = 1,
                exposureDelta = 1,
                fansDelta = 120
            },
            new CommunityPostTemplate
            {
                id = "player_spotlight",
                title = "選手專訪",
                description = "以選手專訪擴大個人與俱樂部話題；受訪選手將消耗精力。",
                cashCost = 2000,
                fameDelta = 3,
                exposureDelta = 1,
                fansDelta = 280,
                playerEnergyCost = 18,
                playerEnergyTargetCount = 1,
                minStarterTargets = 0
            },
            new CommunityPostTemplate
            {
                id = "behind_scenes",
                title = "幕後紀實",
                description = "呈現專業運營與團隊文化；需正選選手配合拍攝並消耗精力。",
                cashCost = 1500,
                fameDelta = 1,
                exposureDelta = 3,
                fansDelta = 150,
                playerEnergyCost = 12,
                playerEnergyTargetCount = 3,
                minStarterTargets = 2
            },
            new CommunityPostTemplate
            {
                id = "fan_engagement",
                title = "粉絲互動",
                description = "透過問答、投票或抽獎拉近與粉絲距離，並擴散聲量。",
                cashCost = 1000,
                fameDelta = 2,
                exposureDelta = 2,
                fansDelta = 400
            },
            new CommunityPostTemplate
            {
                id = "pr_clarification",
                title = "公關澄清",
                description = "針對傳聞或爭議發布說明，以修復外界觀感；可能略損短期名氣。",
                cashCost = 3500,
                fameDelta = -1,
                exposureDelta = 4,
                fansDelta = -80
            }
        };

        public static IReadOnlyList<CommunityPostTemplate> AllTemplates => Templates;

        public static CommunityPostTemplate FindTemplate(string templateId)
        {
            if (string.IsNullOrWhiteSpace(templateId))
                return null;
            return Templates.FirstOrDefault(t =>
                string.Equals(t.id, templateId, StringComparison.OrdinalIgnoreCase));
        }

        public static int DayIndex(int clockHour) =>
            Math.Max(0, clockHour / CareerCalendar.HoursPerDay);

        public static int PostsOnDay(IEnumerable<CommunityPostLogEntry> log, int dayIndex) =>
            log == null ? 0 : log.Count(x => DayIndex(x.clockHour) == dayIndex);

        public static int PostsOnDayForTemplate(
            IEnumerable<CommunityPostLogEntry> log, int dayIndex, string templateId) =>
            log == null ? 0 : log.Count(x =>
                DayIndex(x.clockHour) == dayIndex &&
                string.Equals(x.templateId, templateId, StringComparison.OrdinalIgnoreCase));

        public static bool CanPublish(
            CareerState state,
            ClubState club,
            CommunityPostTemplate template,
            out string reason)
        {
            reason = null;
            if (state == null || club == null || template == null)
            {
                reason = "資料不完整";
                return false;
            }

            if (state.gameOver)
            {
                reason = "生涯已結束";
                return false;
            }

            if (!string.Equals(club.clubId, state.playerClubId, StringComparison.OrdinalIgnoreCase))
            {
                reason = "僅玩家俱樂部可發文";
                return false;
            }

            state.communityPosts = state.communityPosts ?? new List<CommunityPostLogEntry>();
            int day = DayIndex(state.clockHour);
            if (PostsOnDay(state.communityPosts, day) >= MaxPostsPerDay)
            {
                reason = $"今日已發 {MaxPostsPerDay} 則（明日 0:00 後可再發）";
                return false;
            }

            if (PostsOnDayForTemplate(state.communityPosts, day, template.id) > 0)
            {
                reason = "此類型今日已發過";
                return false;
            }

            if (template.id == "pr_clarification" && club.exposure >= 55)
            {
                reason = "目前威望尚可，暫不需要公關澄清";
                return false;
            }

            if (club.cash < template.cashCost)
            {
                reason = $"現金不足（需 ${template.cashCost:N0}）";
                return false;
            }

            if (!TrySelectEnergyTargets(state, template, out _, out reason))
                return false;

            return true;
        }

        public static bool TrySelectEnergyTargets(
            CareerState state,
            CommunityPostTemplate template,
            out List<PlayerCareerState> targets,
            out string reason)
        {
            targets = new List<PlayerCareerState>();
            reason = null;
            if (template == null || template.playerEnergyCost <= 0)
                return true;

            List<PlayerCareerState> roster = state.players
                .Where(p => string.Equals(p.clubId, state.playerClubId, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (roster.Count == 0)
            {
                reason = "沒有可派出的選手";
                return false;
            }

            if (string.Equals(template.id, "player_spotlight", StringComparison.OrdinalIgnoreCase))
            {
                PlayerCareerState featured = PickFeaturedPlayer(roster);
                if (featured == null || featured.energy < template.playerEnergyCost)
                {
                    reason = $"選手精力不足（專訪需 ≥ {template.playerEnergyCost}）";
                    return false;
                }

                targets.Add(featured);
                return true;
            }

            if (string.Equals(template.id, "behind_scenes", StringComparison.OrdinalIgnoreCase))
            {
                List<PlayerCareerState> starters = roster
                    .Where(IsStarter)
                    .OrderByDescending(p => p.fame)
                    .Take(Math.Max(1, template.playerEnergyTargetCount))
                    .ToList();
                if (starters.Count < Math.Max(1, template.minStarterTargets))
                {
                    reason = $"正選不足（幕後至少 {template.minStarterTargets} 人）";
                    return false;
                }

                if (starters.Any(p => p.energy < template.playerEnergyCost))
                {
                    reason = $"部分正選精力不足（每人需 ≥ {template.playerEnergyCost}）";
                    return false;
                }

                targets.AddRange(starters);
                return true;
            }

            return true;
        }

        public static void ApplyEnergyCost(IEnumerable<PlayerCareerState> targets, int costPerPlayer)
        {
            if (targets == null || costPerPlayer <= 0)
                return;
            foreach (PlayerCareerState player in targets)
                player.energy = Math.Max(0, player.energy - costPerPlayer);
        }

        private static PlayerCareerState PickFeaturedPlayer(List<PlayerCareerState> roster)
        {
            PlayerCareerState starter = roster
                .Where(IsStarter)
                .OrderByDescending(p => p.fame)
                .ThenByDescending(p => p.energy)
                .FirstOrDefault();
            if (starter != null)
                return starter;
            return roster.OrderByDescending(p => p.fame).ThenByDescending(p => p.energy).FirstOrDefault();
        }

        private static bool IsStarter(PlayerCareerState player) =>
            string.Equals(
                ClubEstateRules.NormalizeSquadStatus(player.squadStatus),
                ClubEstateRules.SquadStarter,
                StringComparison.OrdinalIgnoreCase);

        public static string FormatEffectPreview(CommunityPostTemplate template)
        {
            if (template == null) return string.Empty;
            var parts = new List<string>();
            if (template.fameDelta != 0)
                parts.Add($"名氣 {FormatSigned(template.fameDelta)}");
            if (template.exposureDelta != 0)
                parts.Add($"威望 {FormatSigned(template.exposureDelta)}");
            if (template.fansDelta != 0)
                parts.Add($"粉絲 {FormatSigned(template.fansDelta)}");
            if (template.playerEnergyCost > 0)
            {
                if (string.Equals(template.id, "behind_scenes", StringComparison.OrdinalIgnoreCase))
                    parts.Add($"正選精力 −{template.playerEnergyCost}（最多 {template.playerEnergyTargetCount} 人）");
                else
                    parts.Add($"選手精力 −{template.playerEnergyCost}");
            }
            if (template.cashCost > 0)
                parts.Add($"花費 ${template.cashCost:N0}");
            else
                parts.Add("免費");
            return string.Join(" · ", parts);
        }

        private static string FormatSigned(int value) =>
            value > 0 ? $"+{value}" : value.ToString();
    }
}
