using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TacticaUniversalis.Domain;

namespace TacticaUniversalis.Presentation
{
    public static class DemoZh
    {
        private static readonly Dictionary<string, string> Zones = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["t_spawn"] = "T 出生點",
            ["ct_spawn"] = "CT 出生點",
            ["a_long"] = "A 大道",
            ["a_short"] = "A 小路",
            ["a_ramp"] = "A 斜坡",
            ["long_doors"] = "大道門",
            ["cat"] = "貓橋",
            ["a_site"] = "A 包點",
            ["suicide"] = "自殺道",
            ["top_mid"] = "上中路",
            ["mid"] = "中路",
            ["mid_doors"] = "中路門",
            ["xbox"] = "Xbox",
            ["ct_mid"] = "CT 中路",
            ["b_site"] = "B 包點",
            ["b_tunnels"] = "B 通道",
            ["b_doors"] = "B 門",
            ["outside_tunnels"] = "外通道",
            ["lower_tunnels"] = "下通道",
            ["outside_long"] = "外大道",
            ["pit"] = "深坑",
            ["scaffolding"] = "鷹架",
            ["alley"] = "巷道",
            ["pigeons"] = "鴿子位",
            ["ivy"] = "常春藤",
            ["old_bomb"] = "舊包點",
            ["brown_halls"] = "棕廳",
            ["a_main"] = "A 主路",
            ["kitchen"] = "廚房",
            ["popdog"] = "Popdog",
            ["underpass"] = "地下通道",
            ["connector"] = "連接道",
            ["heaven"] = "天堂",
            ["b_halls"] = "B 廳",
            ["b_ramp"] = "B 斜坡",
            ["sidewalk"] = "人行道",
            ["ct_short"] = "CT 小路",
            ["ct_long"] = "CT 大道",
            ["ct_stairs"] = "CT 樓梯",
            ["bottom_mid"] = "下中路",
            ["donut"] = "甜甜圈",
            ["temple"] = "神殿",
            ["water"] = "水道",
            ["ruins"] = "遺跡",
            ["ramp"] = "B 斜坡",
            ["cave"] = "洞穴",
            ["catwalk"] = "貓道",
            ["house"] = "紅屋",
            ["t_stairs"] = "T 樓梯",
            ["tunnel"] = "隧道",
            ["long"] = "大道",
            ["toilets"] = "廁所",
            ["short"] = "小路",
            ["fountain"] = "噴泉",
            ["playground"] = "遊樂場",
            ["monster"] = "怪獸道",
            ["canal"] = "運河",
            ["b_short"] = "B 小路",
            ["bank"] = "銀行",
            ["truck"] = "卡車",
            ["squeaky"] = "吱呀門",
            ["upper_tunnels"] = "上通道",
            ["elevator"] = "電梯井",
            ["generator"] = "發電機",
            ["rafters"] = "樑架",
            ["ladder"] = "梯子",
            ["boost"] = "墊人位",
            ["sandbag"] = "沙袋",
            ["quad"] = "四連箱",
            ["pillar_b"] = "B 柱",
            ["b_stairs"] = "B 樓梯",
            ["top_ramp"] = "斜坡頂",
            ["scaffold"] = "鷹架區",
            ["a_sidewalk"] = "A 人行道",
            ["a_catwalk"] = "A 貓道",
            ["banana"] = "香蕉道",
            ["alt_mid"] = "側中路",
            ["arch"] = "拱門",
            ["boiler"] = "鍋爐房",
            ["apartments"] = "公寓",
            ["balcony"] = "陽台",
            ["library"] = "圖書館",
            ["speedway"] = "快速道",
            ["construction"] = "工地",
            ["coffins"] = "棺木位",
            ["banana_car"] = "香蕉車",
            ["logs"] = "原木",
            ["palace"] = "宮殿",
            ["jungle"] = "叢林",
            ["ticket"] = "售票亭",
            ["b_apps"] = "B 公寓",
            ["market"] = "市場",
            ["window"] = "窗戶",
            ["sandwich"] = "三明治位",
            ["van"] = "廂型車",
            ["lobby"] = "大廳",
            ["hut"] = "小木屋",
            ["outside"] = "外場",
            ["garage"] = "車庫",
            ["mini"] = "迷你門",
            ["silo"] = "筒倉",
            ["radio"] = "無線電室",
            ["lockers"] = "更衣室",
            ["secret"] = "密道",
            ["a_rafters"] = "樑架",
            ["trophy"] = "獎盃室",
            ["hell"] = "地獄",
            ["main"] = "主門",
        };

        private static readonly Dictionary<string, string> Teams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["navi_2021"] = "NAVI",
            ["gambit_2021"] = "Gambit",
            ["NAVI 2021"] = "NAVI",
            ["Gambit 2021"] = "Gambit",
        };

        private static readonly Dictionary<string, string> Plans = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["B Execute"] = "B 點強攻",
            ["Mid Control"] = "中路控制",
            ["Short Take"] = "小路切入",
            ["Long Control"] = "大道控制",
            ["B Anchor"] = "B 點固守",
            ["Mid Contest"] = "中路對峙",
            ["A Crossfire"] = "A 點交叉火力",
            ["Default Map Control"] = "默認控圖",
        };

        private static readonly Dictionary<string, string> WinReasons = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["elimination"] = "殲滅獲勝",
            ["bomb_exploded"] = "炸彈爆炸獲勝",
            ["bomb_defused"] = "拆除炸彈獲勝",
            ["timeout"] = "時間結束獲勝",
        };

        private static readonly Dictionary<string, string> Traits = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["clutch_master"] = "關鍵先生",
            ["competitive_spirit"] = "好勝心",
            ["forest_intimidation"] = "恐森症",
            ["all_weapon_arsenal"] = "全能武器庫",
            ["pyramid_solution"] = "金字塔方程式",
            ["electronic_tyrant"] = "電子暴君",
            ["unlucky"] = "倒楣蛋",
            ["adversity_awakened"] = "逆境覺醒",
            ["forest_electronic"] = "森電",
            ["headshot_machine"] = "爆頭機器",
            ["silent_assassin"] = "沈默刺客",
            ["tactical_brain"] = "戰術大腦",
            ["cis_charge"] = "獨聯體大衝鋒",
            ["system_core"] = "體系核心",
            ["versatile"] = "萬金油",
            ["utility_master"] = "道具大師",
            ["trade_king"] = "補槍王",
            ["silent_leader"] = "無聲領袖",
            ["undying"] = "不死者",
            ["clutch_equation"] = "殘局方程式",
            ["red_star_child"] = "紅星之子",
            ["tailwind_expert"] = "順風專家",
            ["human_emperor"] = "人皇降臨",
            ["front_firepower"] = "正面火力",
            ["young_blood"] = "年輕氣盛",
            ["veteran"] = "老將",
            ["experience_intuition"] = "經驗直覺",
            ["precise_entry"] = "精準突破",
            ["calm_analysis"] = "冷靜剖析",
            ["battle_igl"] = "剛槍指揮",
            ["system_architect"] = "體系建築師",
            ["red_star_academy"] = "紅星青訓",
            ["unsung_hero"] = "無名英雄",
            ["trade_specialist"] = "補槍專家",
        };

        /// <summary>Attribute-focused effect text for trait trigger feed.</summary>
        public static string FormatTraitEffect(string traitId, float bonus = 0f, string extra = null)
        {
            if (!string.IsNullOrEmpty(extra))
                return extra;

            int stacks = bonus > 0f ? Math.Max(1, (int)Math.Round(bonus / 2f)) : 0;
            switch ((traitId ?? string.Empty).ToLowerInvariant())
            {
                case "precise_entry":
                    return "在該回合結束前突破 +5、瞄準 +5";
                case "clutch_master":
                    return "殘局期間意識 +15、瞄準 +10";
                case "competitive_spirit":
                    return "落後 3 分以上時全屬性 +5";
                case "forest_intimidation":
                    return "低威望對手心態／手感下降；擊殺≥20且K/D>1.3時對手全隊心態、手感 -5（僅一次）";
                case "all_weapon_arsenal":
                    return "使用非狙擊武器時懲罰減半（約等同瞄準懲罰 -3）";
                case "pyramid_solution":
                    return "本回合全屬性 +15%";
                case "electronic_tyrant":
                    return "連續擊殺後瞄準 +5、反應 +5";
                case "unlucky":
                    return "開局首殺階段對槍 -6";
                case "adversity_awakened":
                    return "逆境覺醒，全屬性 +3";
                case "forest_electronic":
                    return bonus >= 2.5f
                        ? "與 s1mple 默契發動，配合 +3"
                        : "與 s1mple 默契轉化，配合提升";
                case "headshot_machine":
                    return "本槍爆頭率 +20%";
                case "silent_assassin":
                    return "溝通偏低時的配合懲罰減半，對槍加成生效";
                case "tactical_brain":
                    return "指揮光環：隊友配合 +2.5";
                case "cis_charge":
                    return "隊友突破屬性對槍 +3";
                case "system_core":
                    return "在場光環：全隊配合 +2.5";
                case "system_architect":
                    return "在場光環：全隊配合 +4";
                case "versatile":
                    return "輔助位全屬性 +2%，位置懲罰減半";
                case "utility_master":
                    return "道具／回防階段對槍 +5（一般交火 +2）";
                case "trade_king":
                    return "補槍階段瞄準 +10、反應提升";
                case "trade_specialist":
                    return "補槍階段瞄準 +15%、反應 +15%";
                case "silent_leader":
                    return "配合屬性轉化為對槍加成";
                case "undying":
                    return "殘局時生存與對槍 +6（一般階段 +4）";
                case "clutch_equation":
                    return "殘局全屬性 +10%";
                case "red_star_child":
                    return stacks > 0
                        ? $"層數 x{stacks}：瞄準 +{stacks * 2}、意識 +{stacks * 2}"
                        : "存活擊殺後累積瞄準、意識加成";
                case "tailwind_expert":
                    return bonus >= 0f
                        ? "領先 3 分以上：全屬性 +3"
                        : "落後 3 分以上：全屬性 -5";
                case "human_emperor":
                    return "本回合首次擊殺後瞄準 +10、反應 +10";
                case "front_firepower":
                    return "正面交火瞄準／反應對槍 +5";
                case "young_blood":
                    return bonus >= 0f
                        ? "前 10 回合反應 +2.5"
                        : "第 20 回合後反應 -1.5";
                case "veteran":
                    return "前三回合團隊配合 +3";
                case "experience_intuition":
                    return "20% 機率提早預警，奪取交火主動權";
                case "calm_analysis":
                    return "指揮光環：隊友配合／意識 +2.5";
                case "battle_igl":
                    return "第 28 回合後瞄準 +5";
                case "red_star_academy":
                    return "青訓隊友溝通／配合微幅提升（+0.5）";
                case "unsung_hero":
                    return "輔助位全屬性 +2%，位置懲罰減半";
                case "big_stage_heart":
                    return "大賽壓力對指揮懲罰減半";
                case "champion_pedigree":
                    return "重要比賽降低全隊壓力負面影響";
                case "kazakh_eagle":
                    return "指定地圖／大賽獲得額外加成";
                case "future_star":
                    return "長期成長：意識、配合、殘局提升";
                case "defense_theory":
                    return "降低首殺頻率，提高首殺成功率";
                default:
                    return bonus != 0f
                        ? (bonus > 0f ? $"對槍加成 +{bonus:0.#}" : $"對槍懲罰 {bonus:0.#}")
                        : "效果生效";
            }
        }

        private static readonly Dictionary<string, string> PlayerNames =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static void ConfigurePlayerNames(IEnumerable<PlayerDefinition> players)
        {
            PlayerNames.Clear();
            if (players == null)
                return;
            foreach (PlayerDefinition player in players)
            {
                if (player == null || string.IsNullOrEmpty(player.id))
                    continue;
                string label = string.IsNullOrWhiteSpace(player.displayName) ? player.id : player.displayName;
                PlayerNames[player.id] = label;
            }
        }

        public static string PlayerName(string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
                return string.Empty;
            return PlayerNames.TryGetValue(playerId, out string name) ? name : playerId;
        }

        public static string TraitName(string traitId)
        {
            if (string.IsNullOrEmpty(traitId))
                return string.Empty;
            return Traits.TryGetValue(traitId, out string name) ? name : traitId;
        }

        /// <summary>Longer display names for the career/match player profile panel.</summary>
        public static string ProfileTraitName(string traitId)
        {
            switch ((traitId ?? string.Empty).ToLowerInvariant())
            {
                case "forest_intimidation": return "恐森症威壓";
                case "pyramid_solution": return "金字塔的尖，方程式的解";
                default: return TraitName(traitId);
            }
        }

        /// <summary>Full profile blurbs for the personal-info panel.</summary>
        public static string ProfileTraitDescription(string traitId)
        {
            switch ((traitId ?? string.Empty).ToLowerInvariant())
            {
                case "clutch_master":
                    return "殘局時意識+15、瞄準+10";
                case "competitive_spirit":
                    return "落後時（比分差距≥3）全屬性+5%；領先時全屬性無加成";
                case "forest_intimidation":
                    return "對位選手若威望低於80，該選手每回合額外心態-2，若該選手心態或手感不高於75，該選手每回合額外手感-2；當擊殺大於等於20且K/D大於1.3時，對手全隊手感跟心態-5";
                case "all_weapon_arsenal":
                    return "使用步槍時，瞄準和反應的懲罰-50%（原本非狙擊武器會有-15%懲罰，現在只剩-7.5%）";
                case "pyramid_solution":
                    return "每回合有30%機率進入狀態，狀態期間，所有屬性暫時+15%（乘法加成，突破面板上限），但如果在該狀態打輸，則會心態-5，手感-5";
                default:
                    return FormatTraitEffect(traitId);
            }
        }

        public static string TeamName(string idOrDisplay)
        {
            if (string.IsNullOrEmpty(idOrDisplay))
                return string.Empty;
            if (Teams.TryGetValue(idOrDisplay, out string name))
                return name;
            return StripSeasonYearSuffix(idOrDisplay);
        }

        /// <summary>
        /// Removes trailing season year from ids/display names, e.g. astralis_2021 or Astralis 2021.
        /// </summary>
        public static string StripSeasonYearSuffix(string idOrDisplay)
        {
            if (string.IsNullOrEmpty(idOrDisplay))
                return string.Empty;
            Match match = Regex.Match(idOrDisplay, @"^(.*?)(?:[_\s]|-)((?:19|20)\d{2})$");
            if (match.Success)
            {
                string baseName = match.Groups[1].Value.TrimEnd();
                if (!string.IsNullOrEmpty(baseName))
                    return baseName;
            }
            return idOrDisplay;
        }

        public static string ZoneName(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId))
                return string.Empty;
            return Zones.TryGetValue(zoneId, out string name) ? name : zoneId;
        }

        public static string FormatEvent(SimulationEvent evt)
        {
            if (evt == null)
                return string.Empty;

            switch (evt.type)
            {
                case EventType.RoundStarted:
                    return $"第 {evt.roundNumber} 回合：攻向 {ZoneName(evt.zoneId)}";
                case EventType.Setup:
                    return TranslateSetup(evt.message);
                case EventType.Kill:
                {
                    string line = $"{PlayerName(evt.actorId)} 擊殺 {PlayerName(evt.targetId)}";
                    if (!string.IsNullOrEmpty(evt.message) && evt.message.IndexOf("headshot", StringComparison.OrdinalIgnoreCase) >= 0)
                        line += "（爆頭）";
                    return line;
                }
                case EventType.BombPlanted:
                    return $"{PlayerName(evt.actorId)} 在 {ZoneName(evt.zoneId)} 安裝炸彈";
                case EventType.BombDefused:
                    return $"{PlayerName(evt.actorId)} 拆除炸彈";
                case EventType.BombExploded:
                    return "炸彈爆炸";
                case EventType.Timeout:
                    return "回合時間結束";
                case EventType.RoundEnded:
                    return $"{TeamName(evt.teamId)} {TranslateWinReason(evt.message)}";
                case EventType.HalfTime:
                    return "中場休息，雙方換邊";
                case EventType.OvertimeStarted:
                    return "進入加時賽（MR3）";
                case EventType.MatchEnded:
                    return $"比賽結束：{TeamName(evt.teamId)} 獲勝";
                case EventType.Contact:
                case EventType.Shot:
                case EventType.Move:
                    return string.Empty;
                case EventType.StatusUpdated:
                    if (IsCallout(evt))
                        return TranslateCallout(evt);
                    return TranslateFallback(evt.message);
                default:
                    return TranslateFallback(evt.message);
            }
        }

        private static bool IsCallout(SimulationEvent evt)
        {
            if (evt?.message != null &&
                evt.message.StartsWith("callout|", StringComparison.OrdinalIgnoreCase))
                return true;
            if (evt?.reasonTags == null)
                return false;
            for (int i = 0; i < evt.reasonTags.Length; i++)
            {
                if (string.Equals(evt.reasonTags[i], "callout", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static string TranslateCallout(SimulationEvent evt)
        {
            string[] parts = (evt.message ?? string.Empty).Split('|');
            int count = 0;
            string zone = evt.zoneId;
            string weapons = string.Empty;
            if (parts.Length >= 3)
            {
                int.TryParse(parts[1], out count);
                if (!string.IsNullOrEmpty(parts[2]))
                    zone = parts[2];
                if (parts.Length >= 4)
                    weapons = parts[3].Replace(",", " ");
            }

            if (count <= 0)
                count = 1;
            while (weapons.IndexOf("  ", StringComparison.Ordinal) >= 0)
                weapons = weapons.Replace("  ", " ");
            weapons = weapons.Trim();
            string who = PlayerName(evt.actorId);
            string loc = ZoneName(zone);
            if (string.IsNullOrEmpty(weapons))
                return $"{who} 報點 {loc} {count}人";
            return $"{who} 報點 {loc} {count}人 含 {weapons}";
        }

        private static string TranslateSetup(string message)
        {
            if (string.IsNullOrEmpty(message))
                return "戰術佈署完成";
            string[] parts = message.Split(new[] { " vs " }, StringSplitOptions.None);
            if (parts.Length == 2)
            {
                string a = Plans.TryGetValue(parts[0].Trim(), out string pa) ? pa : parts[0].Trim();
                string b = Plans.TryGetValue(parts[1].Trim(), out string pb) ? pb : parts[1].Trim();
                return $"{a} 對  {b}";
            }
            return TranslateFallback(message);
        }

        private static string TranslateWinReason(string message)
        {
            if (string.IsNullOrEmpty(message))
                return "獲勝";
            Match match = Regex.Match(message, @"wins by (\w+)");
            if (match.Success && WinReasons.TryGetValue(match.Groups[1].Value, out string reason))
                return reason;
            foreach (KeyValuePair<string, string> pair in WinReasons)
            {
                if (message.IndexOf(pair.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                    return pair.Value;
            }
            return "獲勝";
        }

        private static string TranslateFallback(string message)
        {
            if (string.IsNullOrEmpty(message))
                return string.Empty;
            string text = message;
            foreach (KeyValuePair<string, string> pair in Teams)
                text = text.Replace(pair.Key, pair.Value);
            foreach (KeyValuePair<string, string> pair in Zones)
                text = text.Replace(pair.Key, pair.Value);
            text = text.Replace("attacks", "攻向");
            text = text.Replace("eliminated", "擊殺");
            text = text.Replace("headshot", "爆頭");
            text = text.Replace("Bomb planted", "安裝炸彈");
            text = text.Replace("Bomb defused", "拆除炸彈");
            text = text.Replace("Bomb exploded", "炸彈爆炸");
            text = text.Replace("Round timer expired", "回合時間結束");
            text = text.Replace("Replay skipped; authoritative event result applied.", "已跳過重播，直接套用模擬結果。");
            text = text.Replace("Paused", "已暫停");
            text = text.Replace("Resumed", "繼續播放");
            return text;
        }
    }
}
