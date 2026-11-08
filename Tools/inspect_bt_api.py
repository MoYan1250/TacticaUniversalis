# -*- coding: utf-8 -*-
from pathlib import Path
t = Path("Assets/_Project/Scripts/Domain/BehaviorTreeRoundSimulator.cs").read_text(encoding="utf-8")
checks = [
    "HasHuntAdvantage", "HasHuntAdvantage",
    "ShouldAbortSave", "ShouldAbortSave",
    "BoardHasFreshTag", "BoardHasFreshTag",
    "TagDefusing", "TagDefusing",
    "AcousticTtlSeconds", "AcousticTtlSeconds",
    "HoldNearSite", "HoldNearSite",
    "EnemiesInZone", "EnemiesInZone",
    "GraphDistance", "GraphDistance",
    "FindPath", "IsPlayableHoldZone",
    "BombCarrierId", "BombCarrierId",
    "assigned", "PersonalTarget",
]
for s in checks:
    print(f"{s!r}: {t.count(s)}")
# print exact ShouldHunt
i = t.find("private static bool ShouldHunt")
print("HUNT:\n", t[i:i+280])
i = t.find("Carrier pushes site")
print("TARGET:\n", t[i-40:i+420])
i = t.find("TagDefusing")
print("POST:\n", t[i-200:i+420])
i = t.find("public static bool HasHuntAdvantage")
print("RULES:\n", t[i:i+420])
i = t.find("if (attacking && IsSpawnZone")
print("ASSIGN:\n", t[i:i+550])
i = t.find("score += 1.5f")
print("SCORE:\n", t[i-180:i+100])
# GraphDistance helper?
print("has GraphDistance method", "private static float GraphDistance" in t)
print("has EnemiesInZone", "private static int EnemiesInZone" in t)
print("has IsPlayableHoldZone", "IsPlayableHoldZone(" in t)
