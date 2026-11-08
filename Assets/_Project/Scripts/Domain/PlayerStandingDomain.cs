using System;

namespace TacticaUniversalis.Domain
{
    /// <summary>
    /// Player historical / current standing tiers shown as square rank icons in career UI.
    /// Canonical ids are stable English keys; JSON may use Chinese or English display labels.
    /// </summary>
    public static class PlayerStandingRules
    {
        public const string MissingId = "missing";

        public static readonly string[] OrderedIds =
        {
            "goat",
            "crown_prince",
            "dominator",
            "superstar",
            "all_star",
            "elite",
            "pro",
            "semi_pro",
            "challenger",
            "contender",
            "apprentice",
            "amateur"
        };

        public static string NormalizeId(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return MissingId;

            string key = raw.Trim();
            switch (key)
            {
                case "GOAT":
                case "GOAT級":
                case "goat":
                    return "goat";
                case "王儲":
                case "Crown Prince":
                case "crown_prince":
                    return "crown_prince";
                case "高位明星":
                case "Dominator":
                case "dominator":
                    return "dominator";
                case "超巨星":
                case "Superstar":
                case "superstar":
                    return "superstar";
                case "全明星":
                case "All-Star":
                case "All Star":
                case "all_star":
                    return "all_star";
                case "精英":
                case "Elite":
                case "elite":
                    return "elite";
                case "一線":
                case "Pro":
                case "pro":
                    return "pro";
                case "準一線":
                case "Semi-Pro":
                case "Semi Pro":
                case "semi_pro":
                    return "semi_pro";
                case "二線":
                case "Challenger":
                case "challenger":
                    return "challenger";
                case "準二線":
                case "Contender":
                case "contender":
                    return "contender";
                case "學徒":
                case "Apprentice":
                case "apprentice":
                case "新銳":
                    return "apprentice";
                case "素人":
                case "Amateur":
                case "amateur":
                    return "amateur";
                case "缺資料":
                    return MissingId;
                default:
                    return MissingId;
            }
        }

        public static string DisplayName(string idOrRaw)
        {
            switch (NormalizeId(idOrRaw))
            {
                case "goat": return "GOAT";
                case "crown_prince": return "王儲";
                case "dominator": return "高位明星";
                case "superstar": return "超巨星";
                case "all_star": return "全明星";
                case "elite": return "精英";
                case "pro": return "一線";
                case "semi_pro": return "準一線";
                case "challenger": return "二線";
                case "contender": return "準二線";
                case "apprentice": return "學徒";
                case "amateur": return "素人";
                default: return "缺資料";
            }
        }

        public static string Description(string idOrRaw)
        {
            switch (NormalizeId(idOrRaw))
            {
                case "goat":
                    return "當代無可爭議的第一人。個人榮譽與大賽舞台的長期統治，讓整個場景都以他為標尺。";
                case "crown_prince":
                    return "緊咬王座的第二階梯。幾乎每場都能威脅冠軍熱門，只差臨門一腳的絕對統治。";
                case "dominator":
                    return "足以靠個人表現扭轉對局節奏的頂級火力。一流隊伍的決勝點，對手必須圍繞他佈局。";
                case "superstar":
                    return "穩定擠進世界前列的隊伍核心。無論數據或影響力，都是賽季討論的常客。";
                case "all_star":
                    return "一線主力中的佼佼者。能扛起一流陣容的關鍵輪次，但尚未穩定站上世界前排。";
                case "elite":
                    return "長時間維持高水準輸出的職業尖端。距離個人榜前列只差一點爆發與曝光。";
                case "pro":
                    return "一線賽事的正式輪換成員。以隊伍成績立足，個人尚未成為媒體焦點。";
                case "semi_pro":
                    return "準一線隊伍的主力支柱。能在區域與次級國際賽打出穩定貢獻。";
                case "challenger":
                    return "二線聯賽與區域強隊的常駐戰力。具備職業底子，但舞台與資源仍偏區域。";
                case "contender":
                    return "區域賽事的常客。正在累積職業節奏，尚未站穩更高一級的輪換席次。";
                case "apprentice":
                    return "青訓或三線體系中的培養對象。潛力可見，實戰履歷仍在堆疊。";
                case "amateur":
                    return "業餘或準業餘環境出身。職業賽場經驗極少，尚在尋找進入體系的入口。";
                default:
                    return "目前沒有足夠公開資料判定這位選手的地位。";
            }
        }

        public static string IconResourcePath(string idOrRaw)
        {
            string id = NormalizeId(idOrRaw);
            if (id == MissingId)
                return null;
            return "UI/Icons/standing/" + id;
        }

        public static string FromFame(int fame)
        {
            if (fame >= 95) return "goat";
            if (fame >= 90) return "crown_prince";
            if (fame >= 85) return "dominator";
            if (fame >= 80) return "superstar";
            if (fame >= 72) return "all_star";
            if (fame >= 64) return "elite";
            if (fame >= 55) return "pro";
            if (fame >= 48) return "semi_pro";
            if (fame >= 40) return "challenger";
            if (fame >= 32) return "contender";
            if (fame >= 20) return "apprentice";
            return "amateur";
        }

        public static string ResolveRaw(string primary, string fallback)
        {
            if (!string.IsNullOrWhiteSpace(primary))
                return primary.Trim();
            if (!string.IsNullOrWhiteSpace(fallback))
                return fallback.Trim();
            return null;
        }
    }
}
