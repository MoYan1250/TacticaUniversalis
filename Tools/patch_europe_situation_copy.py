# -*- coding: utf-8 -*-
from pathlib import Path

p = Path(r"Assets/_Project/Scripts/Presentation/CareerIntroCopy.cs")
t = p.read_text(encoding="utf-8")
old_start = "        internal const string EuropeSituation2021 ="
i = t.find(old_start)
if i < 0:
    raise SystemExit("EuropeSituation2021 not found")

end = t.find('";\n', i)
if end < 0:
    end = t.find('";\r\n', i)
if end < 0:
    raise SystemExit("end not found")
end = end + 2  # include ";

new_body = '''        internal const string EuropeSituation2021 =
            @"2020年的CS：GO賽場在動盪與混亂中走向尾聲。疫情迫使所有賽事轉為線上，傳統的地域壁壘與線下統治力被徹底打破。Astralis憑藉IEM全球挑戰賽的冠軍，在年底重返世界第一，但gla1ve與Xyp9x在年中因身心俱疲相繼休戰，讓這座丹麥王朝的根基不再穩固。Vitality曾在11月登上世界第一，卻在12月的IEM全球挑戰賽小組出局，將王座拱手讓回。NAVI以s1mple、electroNic與Perfecto為核心，穩居世界前三。G2在年底迎來NiKo加盟，與堂兄弟huNter-組成全新核心。Heroic完成從二線到頂尖的蛻變，站穩世界前列。FaZe則在陣容動盪中跌出前十。

而在西歐諸強各自摸索出路之時，一股來自東歐的暗流正悄然匯聚成浪潮。Gambit在2020年啟動青訓計畫，將nafany、sh1ro、interz、Ax1Le提拔至主隊，並召回2017年Major冠軍老將HObbit壓陣。這支二代紅星全年拿下11座賽事冠軍，以69.2%的勝率高居年度榜首，震驚了整個CS：GO世界。舊王未死，新王已至 !

2021年的歐洲，正見證一個時代的終結與另一個時代的降臨。";'''

p.write_text(t[:i] + new_body + t[end:], encoding="utf-8")
out = p.read_text(encoding="utf-8")
assert "新王已至" in out
assert "69.2%" in out
assert "正見證一個時代的終結" in out
print("ok")
