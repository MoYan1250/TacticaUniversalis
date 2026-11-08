# -*- coding: utf-8 -*-
from pathlib import Path

p = Path(r"Assets/_Project/Scripts/Presentation/CareerDemoController.cs")
t = p.read_text(encoding="utf-8")

old = '''        private static string ClubCurrentBlurb(string clubId)
        {
            switch ((clubId ?? string.Empty).ToLowerInvariant())
            {
                case "gambit_2021":
                    return "2021 年初，Gambit 被視為 CIS 狂潮最鋒利的矛。sh1ro 與 Ax1Le 的上限驚人，nafany 的指揮風格激進而年輕。機會很大，但經驗與穩定性仍是通往真正王朝的最後關卡。";
                case "navi_2021":
                default:
                    return "2021 年初的 NAVI 仍圍繞 s1mple 運轉，先發步槍線由 flamie 與 electroNic 共同撐起，青訓新星 b1t 則在替補席積蓄能量。壓力來自內部與外部：外界要冠軍，更衣室要證明這套主力加上儲備的配置，能把個人天賦轉成集體獎盃。";
            }
        }'''

# Verbatim strings: double any " inside. User used curly “ ” around B1T and 「」 for quote.
navi = (
    "2020年對NAVI而言是充滿矛盾的一年。年初以新陣容：s1mple、electroNic、Boombl4、Perfecto與flamie，"
    "在IEM卡托維茲線下賽以3：0橫掃G2奪冠，展現了絕對的統治力。然而隨著疫情爆發、賽事全面轉為線上，"
    "NAVI的狀態急速下滑，不僅在IEM紐約接連輸給Virtus.pro與forZe，更在EPL S12決賽與IEM北京海淀決賽中，"
    "兩度在2：0領先的大好局面下被Astralis與Vitality讓二追三逆轉。下半年顆粒無收的NAVI，帶著起伏與遺憾結束了這一年。\n\n"
    "2020年12月，NAVI做出了改變未來的關鍵決定—將青訓隊的17歲天才Valeriy “B1T” Vakhovskiy提拔至主隊，"
    "正式啟用六人輪換陣容。教練B1ad3對此表示：「在正確的操作下，擴大陣容的模型非常成功，長遠來看這絕對會強化我們的陣容。」"
    "此時NAVI以HLTV世界第三的排名結束2020年，是前十名中唯一的獨聯體代表。全隊在年末賽事結束後進入休假，"
    "預計2021年1月中旬歸隊，迎接即將到來的BLAST Premier全球總決賽—這場賽事將決定他們能否以一座冠軍為動盪的2020年畫下句點，"
    "並揭開黑金王朝的序幕。"
)

gambit = (
    "2020年對Gambit而言是浴火重生的一年。10月，俱樂部正式宣布將青訓隊Gambit Youngsters整體提拔為主隊。"
    "陣容由nafany、sh1ro、interz、Ax1Le與2017年Major冠軍老將HObbit組成，教練則由groove擔任。"
    "這支二代紅星在2020年以穩定的陣容征戰全年，成為HLTV年度前二十名隊伍中唯一一支全年未曾更換陣容的隊伍。\n\n"
    "這批從青訓體系中殺出的年輕人以驚人的統治力震驚了整個世界。他們全年拿下11座賽事冠軍，"
    "在368張地圖上打出68.2%的勝率，高居HLTV年度戰隊勝率榜榜首。Gambit以HLTV世界排名第15名結束2020年，"
    "靜靜等待著2021年的全面爆發。"
)

# Escape for C# @"..." : only " -> ""
def to_verbatim(s: str) -> str:
    return '@"' + s.replace('"', '""') + '"'

new = f'''        private static string ClubCurrentBlurb(string clubId)
        {{
            switch ((clubId ?? string.Empty).ToLowerInvariant())
            {{
                case "gambit_2021":
                    return {to_verbatim(gambit)};
                case "navi_2021":
                default:
                    return {to_verbatim(navi)};
            }}
        }}'''

if old not in t:
    raise SystemExit("ClubCurrentBlurb block not found")
p.write_text(t.replace(old, new, 1), encoding="utf-8")
out = p.read_text(encoding="utf-8")
assert "黑金王朝的序幕" in out
assert "68.2%" in out
assert "Gambit Youngsters" in out
print("ok")
