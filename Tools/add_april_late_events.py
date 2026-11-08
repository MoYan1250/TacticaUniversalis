# -*- coding: utf-8 -*-
import json
from pathlib import Path

p = Path(__file__).resolve().parents[1] / "Assets/_Project/Data/JSON/career_events.json"
data = json.loads(p.read_text(encoding="utf-8"))


def opt(oid, label):
    return {"id": oid, "label": label, "tooltip": "", "effects": []}


new_events = [
    {
        "id": "hist_astralis_internal_fracture_2021_04_26",
        "title": "Astralis內部矛盾浮現",
        "tier": 4,  # B
        "isMajor": True,
        "fireHour": 2760,  # 2021-04-26
        "mtthDays": 99999,
        "publicVisible": True,
        "subjectClubId": "astralis_2021",
        "onFireEffects": [
            {"type": "add_exposure", "value": -20},
            {"type": "add_roster_pressure", "value": 10},
            {"type": "add_board", "value": -4},
            {"type": "add_estate", "key": "public_opinion", "value": -5},
            {"type": "add_estate", "key": "star_players", "value": -4},
            {
                "type": "set_flag",
                "key": "dynasty_internal_fracture",
                "durationDays": 0,
            },
        ],
        "description": "Astralis內部的矛盾逐漸浮上檯面。自2020年初以來，管理層與選手之間在休假、薪資與訓練安排等問題上產生了嚴重分歧。體育總監Kasper Hvidt的管理風格被認為過於刻板，選手們希望獲得更多休假時間以緩解職業倦怠，但管理層以賽程與投資者利益為由拒絕了這些請求。疫情期間俱樂部更要求全體員工減薪百分之三十，進一步加劇了雙方的緊張關係。CSPPA已介入協調，但裂痕似乎難以修補。",
        "firstPersonDescription": "我們必須承認，隊伍內部正面臨著嚴峻的考驗。自2020年初以來，管理層與選手之間在休假、薪資與訓練安排等問題上產生了嚴重分歧。競技總監Kasper Hvidt的管理風格被認為過於刻板，選手們希望獲得更多休假時間以緩解職業倦怠，但管理層以賽程與投資者利益為由拒絕了這些請求。疫情期間俱樂部要求全體員工減薪百分之三十，進一步加劇了雙方的緊張關係。CSPPA已介入協調。gla1ve在2020年5月因職業倦怠與健康問題申請休假三個月，Xyp9x隨後也跟進。這些裂痕正在侵蝕我們的核心。",
        "triggers": [{"type": "always"}],
        "options": [
            opt("collapse", "王朝的崩潰往往從內部開始。"),
            opt("erode", "管理層與選手的矛盾正在侵蝕這支隊伍的根基。"),
            opt("turning", "這或許是Astralis走向衰落的轉折點。"),
        ],
        "firstPersonOptions": [
            opt("face", "我們必須正視這些問題，否則王朝將從內部崩塌。"),
            opt("talk", "管理層與選手需要坐下來好好溝通。"),
        ],
    },
    {
        "id": "hist_heroic_epl_s13_2021_04_30",
        "title": "Heroic奪得EPL S13冠軍，cadiaN打出載入史冊的1v4殘局",
        "tier": 3,  # A
        "isMajor": True,
        "fireHour": 2856,  # 2021-04-30
        "mtthDays": 99999,
        "publicVisible": True,
        "subjectClubId": "heroic_2021",
        "onFireEffects": [
            {"type": "add_exposure", "value": 7},
            {"type": "add_fame", "value": 5},
            {"type": "add_fans", "value": 7000},
            {"type": "add_board", "value": 5},
            {"type": "add_estate", "key": "public_opinion", "value": 6},
            {"type": "add_estate", "key": "star_players", "value": 4},
        ],
        "description": "Heroic在EPL第十三賽季總決賽中以三比零擊敗Gambit，以不敗戰績奪得冠軍。這是丹麥隊伍繼Astralis之後再次登上世界之巔。決賽中，cadiaN在關鍵回合打出了一記載入史冊的1v4殘局，以一己之力逆轉劣勢，徹底摧毀了對手的士氣。這支丹麥黑馬在整個賽事中展現了驚人的團隊紀律與戰術執行力，證明了他們並非曇花一現。",
        "firstPersonDescription": "",
        "triggers": [{"type": "always"}],
        "options": [
            opt("congrats", "恭喜他們，這是一場值得銘記的勝利。"),
            opt("clutch", "cadiaN的殘局將永遠留在CSGO的歷史中。"),
            opt("denmark", "丹麥CS正在迎來新的統治者。"),
        ],
        "firstPersonOptions": [],
    },
]

existing = {e.get("id") for e in data["events"]}
for e in new_events:
    if e["id"] in existing:
        data["events"] = [x for x in data["events"] if x.get("id") != e["id"]]
    data["events"].append(e)

p.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
text = p.read_text(encoding="utf-8")
assert "Astralis內部矛盾浮現" in text
assert "Heroic奪得EPL S13冠軍" in text
assert "dynasty_internal_fracture" in text
assert "add_roster_pressure" in text
print("total", len(data["events"]), "ok")
