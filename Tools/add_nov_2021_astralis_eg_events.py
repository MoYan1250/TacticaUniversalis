# -*- coding: utf-8 -*-
"""Append Astralis rebuild + EG roster overhaul events (Nov 2021)."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
events_path = root / "Assets/_Project/Data/JSON/career_events.json"
data = json.loads(events_path.read_text(encoding="utf-8"))
existing = {e.get("id") for e in data.get("events", [])}


def opt(oid, label):
    return {"id": oid, "label": label, "tooltip": "", "effects": []}


new_events = [
    {
        "id": "hist_astralis_blamef_k0nfig_2021_11_04",
        "title": "Astralis簽下blameF與k0nfig，宣告王朝陣容徹底解體",
        "tier": 3,  # A
        "isMajor": True,
        "fireHour": 7368,  # 2021-11-04
        "mtthDays": 99999,
        "publicVisible": True,
        "subjectClubId": "astralis_2021",
        "onFireEffects": [
            {"type": "add_exposure", "value": -5},
            {"type": "clear_flag", "key": "dynasty_internal_fracture", "durationDays": 0},
        ],
        "onFireRosterOps": [
            {
                "type": "join_club",
                "playerId": "blamef",
                "clubId": "astralis_2021",
                "squadStatus": "starter",
            },
            {
                "type": "join_club",
                "playerId": "k0nfig",
                "clubId": "astralis_2021",
                "squadStatus": "starter",
            },
        ],
        "description": (
            "Astralis正式簽下blameF與k0nfig，同時前North教練ave接替zonic成為新任主教練。"
            "這項人事調整意味著dupreeh、Magisk與zonic將在明年一月合約到期後離開俱樂部，"
            "CSGO歷史上最成功的長期陣容核心就此不復存在。"
            "Astralis在PGL斯德哥爾摩Major小組賽中以一勝三負的成績爆冷出局，"
            "這是隊伍成立以來首次無緣Major淘汰賽。"
            "競技總監Kasper Hvidt表示，這是一支全新的Astralis，過去的一切已成歷史。"
            "blameF表示與gla1ve合作讓他能夠放下指揮重擔，更專注於個人操作。"
            "k0nfig則表示，在Complexity的兩年讓他更加懷念純丹麥隊伍的氣氛。"
        ),
        "firstPersonDescription": (
            "我們正式宣布簽下前Complexity的blameF與k0nfig，"
            "同時前North教練ave將接任zonic成為新任主教練。"
            "這項人事調整意味著dupreeh、Magisk與zonic將在明年一月合約到期後離開俱樂部。"
            "我們感謝這三位傳奇人物為俱樂部贏得的四座Major冠軍與無數榮耀，"
            "當後人記錄我們的故事時，他們將永遠位於第一頁。"
            "我們在PGL斯德哥爾摩Major中以令人失望的一比三成績小組出局，"
            "這是隊伍成立以來首次無緣淘汰賽。"
            "競技總監Kasper Hvidt表示，過去的一切已不願再提及，這是一支全新的Astralis，未來掌握在我們自己手中。"
            "blameF將與gla1ve共同承擔指揮責任，這讓他能更專注於個人表現。"
            "k0nfig則表示，在Complexity待了兩年之後，他懷念純丹麥隊伍中那種獨特的氛圍與感覺。"
        ),
        "triggers": [{"type": "always"}],
        "options": [
            opt("remnants", "丹麥傳奇五人組只剩gla1ve與Xyp9x還留在隊中。"),
            opt("history", "A隊全丹麥王朝陣容正式走入歷史。"),
            opt("rebuild", "這或許是Astralis從巔峰走向重建的關鍵轉折點。"),
        ],
        "firstPersonOptions": [
            opt("thanks", "感謝傳奇三人組的所有貢獻，但我們必須迎接新的時代。"),
            opt("new", "這是一支全新的Astralis，我們將帶著過去的榮耀繼續前進。"),
            opt("chapter", "王朝的終結令人惋惜，但新的篇章已經展開。"),
        ],
    },
    {
        "id": "hist_eg_roster_overhaul_2021_11_08",
        "title": "EG北美陣容大換血",
        "tier": 4,  # B
        "isMajor": True,
        "fireHour": 7464,  # 2021-11-08
        "mtthDays": 99999,
        "publicVisible": True,
        "subjectClubId": "eg_2021",
        "onFireEffects": [
            {"type": "add_exposure", "value": -3},
        ],
        "onFireRosterOps": [
            # oBo / MICHU are not in career_players yet; only stanislaw can be mutated.
            {
                "type": "release",
                "playerId": "stanislaw",
                "squadStatus": "bench",
                "clubId": "",
            },
        ],
        "description": (
            "Evil Geniuses在PGL斯德哥爾摩Major以零勝三敗的成績在小組賽階段墊底出局後，"
            "宣布對陣容進行大幅度調整。"
            "stanislaw、oBo與MICHU已率先離開隊伍，目前陣容中僅剩Brezhe與CeRq兩名選手。"
            "自從Ethan於今年一月轉戰Valorant後，EG的陣容就一直處於不穩定狀態，"
            "簽入MICHU與將daps轉為教練等調整均未能改善成績。"
            "他們晉級Major之路亦備受質疑，僅靠擊敗00Nation與Trumiph棄權等情況才勉強取得傳奇組名額。"
            "EG表示將組建更強大的陣容，預計將圍繞現有核心進行重建。"
        ),
        "firstPersonDescription": "",
        "triggers": [{"type": "always"}],
        "options": [
            opt("quake", "北美CS的地震還在持續。"),
            opt("inevitable", "他們在Major的表現令人失望，這次換血是必然的結果。"),
            opt("region", "北美賽區的競爭力正在受到嚴峻考驗。"),
        ],
        "firstPersonOptions": [],
    },
]

added = 0
for event in new_events:
    if event["id"] in existing:
        print(f"skip: {event['id']}")
        continue
    data.setdefault("events", []).append(event)
    added += 1
    print(f"added: {event['id']}")

if added:
    events_path.write_text(
        json.dumps(data, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
print(f"done, added={added}")
