# -*- coding: utf-8 -*-
from pathlib import Path

p = Path(r"Assets/_Project/Scripts/Presentation/CareerIntroCopy.cs")
t = p.read_text(encoding="utf-8")
old_start = "        internal const string EraCisSurge2021 ="
i = t.find(old_start)
if i < 0:
    raise SystemExit("EraCisSurge2021 not found")

# End at start of EuropeSituation2021
j = t.find("        internal const string EuropeSituation2021 =", i)
if j < 0:
    raise SystemExit("EuropeSituation2021 marker not found")

new_era = '''        internal const string EraCisSurge2021 =
            @"2021年初，獨聯體的烽火已在歐陸邊境點燃。基輔的NAVI集結了s1mple、electroNic與Perfecto的鐵三角，並在年末將青訓天才B1T提拔至主隊。這支黑金軍團在2020年雖僅有一座冠軍入帳，卻已將Astralis與Vitality拉下王座，展現出足以撼動整個歐洲秩序的恐怖潛力。他們的賽場風格融合了s1mple的絕對火力與electronic的鋼鐵紀律，等待的只是一個徹底爆發的契機。

與此同時，喀山的Gambit正以另一種方式改寫歷史。2017年Major冠軍老將HObbit率領nafany、sh1ro、interz與Ax1Le四名自家青訓體系培育的少年，在2020年以69.2%的勝率高居年度榜首。他們證明了冠軍不必重金購入，可以從零培養。這支二代紅星的崛起，讓賽區不再是NAVI獨霸，而是兩股並行的風暴，獨聯體即將二度崛起，靜靜改寫整個歷史。";

'''

p.write_text(t[:i] + new_era + t[j:], encoding="utf-8")
out = p.read_text(encoding="utf-8")
assert "獨聯體的烽火已在歐陸邊境點燃" in out
assert "喀山的Gambit" in out
assert "69.2%" in out
print("ok")
