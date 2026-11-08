# -*- coding: utf-8 -*-
"""Append NAVI BLAST World Final 2021 champion event (outcome-gated)."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
events_path = root / "Assets/_Project/Data/JSON/career_events.json"
data = json.loads(events_path.read_text(encoding="utf-8"))
event_id = "hist_navi_blast_world_final_2021_12_20"
existing = {e.get("id") for e in data.get("events", [])}
if event_id in existing:
    print(f"skip: {event_id}")
    raise SystemExit(0)


def opt(oid, label):
    return {"id": oid, "label": label, "tooltip": "", "effects": []}


event = {
    "id": event_id,
    "title": "NaVi奪得BLAST全球總決賽冠軍，s1mple單年8座MVP創歷史紀錄",
    "tier": 3,  # A
    "isMajor": True,
    "fireHour": 8472,  # 2021-12-20
    "mtthDays": 99999,
    "publicVisible": True,
    "subjectClubId": "navi_2021",
    "onFireEffects": [
        {"type": "add_exposure", "value": 3},
    ],
    # Outcome-gated: only fires if NAVI actually won BLAST World Final 2021.
    # Player mission-tree path that fails to win will not see this event.
    "description": (
        "NAVI在BLAST全球總決賽中以二比一擊敗Gambit，奪得2021年第八座冠軍獎盃。"
        "NAVI在首戰輸給Liquid後跌入敗者組，隨後連續擊敗Heroic、G2、Liquid、Vitality與Gambit完成了一穿五的壯舉。"
        "s1mple以全賽事平均1.42的rating獲評MVP，這是他今年的第八座MVP獎牌，"
        "打破了device在2018年創下的單年七座MVP紀錄。"
        "這是NAVI連續第四座線下大賽冠軍，黑金王朝以無可爭議的統治力為2021年畫下了完美句點。"
    ),
    "firstPersonDescription": (
        "我們在BLAST全球總決賽中以二比一擊敗Gambit，為2021年畫下了完美的句點。"
        "這是我們今年拿下的第八座冠軍，同時也是連續第四座線下大賽冠軍。"
        "總決賽開局雖在Mirage以十一比十六先失一圖，但隨後在Ancient與Inferno上我們以摧枯拉朽之勢接管了比賽。"
        "從敗者組第一輪開始，我們接連擊敗了Heroic、G2、Liquid、Vitality與Gambit，完成了不可思議的一穿五。"
        "s1mple以平均1.42的rating獲評賽事MVP，這是他今年的第八座MVP獎牌，"
        "打破了device在2018年創下的單年七座MVP紀錄。"
        "s1mple在賽後受訪時表示，如今他會說NAVI王朝已經到來。"
    ),
    "triggers": [
        {
            "type": "competition_champion",
            "key": "blast_premier_world_final_2021:navi_2021",
        }
    ],
    "options": [
        opt("record", "s1mple在這一年的表現堪稱CSGO歷史之最，八座MVP前無古人。"),
        opt("remembered", "黑金王朝完美封關，這支NAVI將被永遠銘記。"),
        opt("reign", "他們在2021年的統治力無人能及。"),
    ],
    "firstPersonOptions": [
        opt("perfect", "我們以最完美的方式結束了這一年，黑金王朝的統治無人能撼動。"),
        opt("mvp", "這是隊史最佳賽季，s1mple的八座MVP是對他巔峰統治力的最佳證明。"),
        opt("history", "連續四座線下大賽冠軍，我們正在書寫屬於自己的歷史。"),
    ],
}

data.setdefault("events", []).append(event)
events_path.write_text(
    json.dumps(data, ensure_ascii=False, indent=2) + "\n",
    encoding="utf-8",
)
print(f"added: {event_id}")
