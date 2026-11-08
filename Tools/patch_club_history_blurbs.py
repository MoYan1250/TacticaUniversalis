# -*- coding: utf-8 -*-
from pathlib import Path

p = Path(r"Assets/_Project/Scripts/Presentation/CareerDemoController.cs")
t = p.read_text(encoding="utf-8")

old = '''        private static string ClubHistoryBlurb(string clubId)
        {
            switch ((clubId ?? string.Empty).ToLowerInvariant())
            {
                case "gambit_2021":
                    return "Gambit 以年輕 CIS 班底迅速崛起，用高上限的雙核火力與大膽戰術，在短時間內成為國際賽常客。他們代表的是新世代的上升曲線：不靠舊王朝餘溫，而是用成績硬擠進第一梯隊。";
                case "navi_2021":
                default:
                    return "Natus Vincere 長年是東歐與獨聯體的旗艦俱樂部。從巨星個人英雄主義到體系重建，NAVI 的歷史幾乎與現代 CS 的榮光與焦慮綁在一起——球迷期待的不只是勝利，而是「屬於這個時代的王朝」。";
            }
        }'''

new = '''        private static string ClubHistoryBlurb(string clubId)
        {
            switch ((clubId ?? string.Empty).ToLowerInvariant())
            {
                case "gambit_2021":
                    return @"2017年對Gambit而言是隊史最輝煌的一年。由Dosia、AdreN、mou、Zeus與HObbit組成的陣容，在PGL克拉科夫Major上演灰姑娘童話。這支平均年齡27歲的「老年戰隊」先是在半決賽擊敗丹麥霸主Astralis，決賽再以讓一追二的戲碼逆轉Immortals，為獨聯體賽區贏得隊史首座，也是該賽區第一座Major冠軍獎盃。然而輝煌僅維持17天，奪冠後Zeus與教練Kane離隊重返NAVI，Gambit從此一蹶不振。

2018年至2019年，Gambit陷入漫長的陣痛期。2018年初的ELEAGUE Boston Major，Gambit在小組賽即遭俄羅斯戰隊QBF擊敗。此後陣容持續動盪，2019年5月俱樂部將主隊陣容暫時凍結，同年9月將Dosia、mou等選手掛上轉會名單。與此同時，Gambit於2019年4月啟動青訓計畫Gambit Youngsters，選拔過程甚至邀請了現任NAVI教練B1ad3參與。這支青訓隊在2019年底的MSI MGA總決賽中擊敗ViCi與ex Epsilon奪冠，首次闖入世界前30名。

2020年，青訓隊在年初以來已贏下7場線上賽事，並擊敗過NAVI、MIBR、forZe與Virtus.pro等強隊。7月，俱樂部迎來2017年Major冠軍老將HObbit回歸，以六人陣容帶領這批少年征戰。10月，Gambit正式宣布將青訓隊整體提拔為主隊，陣容為nafany、sh1ro、interz、Ax1Le、HObbit與教練groove。這支二代紅星在2020年以69.2%的勝率高居年度榜首，靜靜等待著2021年的全面爆發。";
                case "navi_2021":
                default:
                    return @"2018年的NAVI迎來了真正的蛻變。由Zeus、Edward、flamie、s1mple與electroNic組成的陣容，在年初ELEAGUE Boston Major止步四強後，接連拿下SL-i群星聯賽S5、CS:GO亞洲邀請賽與ESL One科隆站等五座冠軍。s1mple以1.33的Rating獲封HLTV年度TOP 1，打破了歷屆紀錄。然而在FACEIT倫敦Major決賽中，NAVI再次敗於Astralis，成為丹麥王朝崛起的墊腳石。

2019年對NAVI而言是動盪的一年。年初他們在StarSeries S7以3：0橫掃fnatic奪冠，但在IEM卡托維茲Major與StarLadder柏林Major分別止步四強與八強。5月，俱樂部迎來重大變革，老將Edward被下放，Boombl4入隊。9月的BLAST Pro Series莫斯科站結束後，Zeus宣布退役，教練Kane離隊，Boombl4正式接過指揮職責。GuardiaN短暫回歸、B1ad3接任教練，但隊伍陷入長期低潮，12月HLTV排名跌至隊史最低的14名。

2020年初，NAVI再度重組，下放GuardiaN、簽下Perfecto。新陣容迅速展現威力，在ICE Challenge 2020拿下亞軍後，NAVI於IEM卡托維茲以3：0橫掃G2奪冠，時隔十年再次捧起這座獎盃，並於3月重返世界第一。然而疫情迫使賽事轉為線上後，NAVI的統治力急轉直下。全年經歷了ESL One科隆歐洲站的慘淡出局、EPL S12決賽遭Astralis讓二追三，以及IEM北京海淀決賽再度被Vitality讓二追三的連串打擊。下半年顆粒無收的NAVI，帶著起伏與遺憾走向2021年。等待他們的，將是BLAST全球總決賽的救贖與黑金王朝的序幕。";
            }
        }'''

if old not in t:
    raise SystemExit("ClubHistoryBlurb block not found")
p.write_text(t.replace(old, new, 1), encoding="utf-8")
out = p.read_text(encoding="utf-8")
assert "黑金王朝的序幕" in out
assert "Gambit Youngsters" in out
assert "69.2%" in out
print("ok")
