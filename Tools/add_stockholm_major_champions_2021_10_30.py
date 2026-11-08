# -*- coding: utf-8 -*-
"""Append NAVI / Gambit PGL Major Stockholm champion events (2021-10-30)."""
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
        "id": "hist_navi_stockholm_major_champion_2021_10_30",
        "title": "NAVI贏得PGL Major斯德哥爾摩",
        "tier": 2,  # S
        "isMajor": True,
        "fireHour": 7248,  # 2021-10-30
        "mtthDays": 99999,
        "publicVisible": True,
        "subjectClubId": "navi_2021",
        "tokenCompetitionId": "pgl_major_stockholm_2021",
        "onFireEffects": [
            {"type": "add_exposure", "value": 30},
            {"type": "add_roster_fame", "value": 30},
        ],
        "description": (
            "NAVI在PGL Major斯德哥爾摩總決賽中以二比零擊敗{決賽對手}，奪得隊史首座Major冠軍。"
            "這不僅是一場勝利，更是一項前無古人的成就，"
            "NAVI成為CSGO歷史上唯一一支以全局皆為二比零的完美戰績奪冠的隊伍。"
            "從傳奇組階段開始，他們先後擊敗各個強隊，七場比賽未失一圖。"
            "s1mple以1.47的rating獲評賽事MVP，這是Major歷史上最高的個人評分紀錄。"
            "歷經五年多的職業生涯與無數次的遺憾，s1mple終於捧起了那座象徵至高榮耀的Major獎盃，"
            "黑金王朝在瑞典的漫天歡呼中達到了巔峰。"
        ),
        "firstPersonDescription": (
            "我們做到了。在Avicii Arena的滿場觀眾面前，我們以二比零擊敗對手，"
            "贏得了PGL Major斯德哥爾摩的冠軍。"
            "這不僅是我們隊史第一座Major冠軍，更是CSGO歷史上唯一一次以全局皆為二比零的完美戰績奪冠的紀錄。"
            "從入圍賽到總決賽，我們未失一圖。"
            "s1mple在長達五年多的職業生涯後終於捧起了那座屬於他的Major獎盃，"
            "他以高達1.47的rating獲評賽事MVP，這是Major歷史上最高的個人評分紀錄。"
            "黑金王朝在這一刻達到了巔峰。"
        ),
        "triggers": [
            {
                "type": "competition_champion",
                "key": "pgl_major_stockholm_2021:navi_2021",
            }
        ],
        "options": [
            opt("moved", "s1mple終於圓夢，這是CSGO歷史上最令人動容的時刻之一。"),
            opt("remembered", "完美奪冠，這支NAVI將永遠被銘記。"),
            opt("peak", "黑金王朝的統治力在這一刻達到了頂點。"),
        ],
        "firstPersonOptions": [
            opt("perfect", "我們是冠軍，這是屬於黑金王朝的完美賽季。"),
            opt("s1mple", "s1mple終於圓夢，這座Major獎盃是對他職業生涯最好的肯定。"),
            opt("history", "這支隊伍將永遠被載入CS的史冊。"),
        ],
    },
    {
        "id": "hist_gambit_stockholm_major_champion_2021_10_30",
        "title": "Gambit贏得PGL Major斯德哥爾摩",
        "tier": 2,  # S
        "isMajor": True,
        "fireHour": 7248,  # 2021-10-30
        "mtthDays": 99999,
        "publicVisible": True,
        "subjectClubId": "gambit_2021",
        "tokenCompetitionId": "pgl_major_stockholm_2021",
        "onFireEffects": [
            {"type": "add_exposure", "value": 30},
            {"type": "add_roster_fame", "value": 30},
        ],
        "description": (
            "Gambit在PGL Major斯德哥爾摩總決賽中奪得隊史首座Major冠軍。"
            "這支獨聯體黑馬在賽前世界排名僅第十九位，卻在一年之內完成了從默默無聞到世界之巔的驚人躍升。"
            "他們的每一場成績都向世界證明了他們並非曇花一現。"
            "在Avicii Arena的舞台上，他們面對剛拿下大滿貫、氣勢如虹的NAVI，也展現出了超乎年齡的冷靜與團隊紀律。"
            "sh1ro以穩定的狙擊火力壓制了對面，Ax1Le與Hobbit在關鍵回合的發揮更徹底摧毀了對手的防線。"
            "這支被視為CS史上最強青訓隊的年輕陣容，在眾人驚嘆聲中登上了CS的頂峰，"
            "證明了獨聯體的CS人才儲備遠超所有人的想像。"
        ),
        "firstPersonDescription": (
            "我們做到了。在Avicii Arena的滿場觀眾面前，我們擊敗了{決賽對手}，"
            "贏得了PGL Major斯德哥爾摩的冠軍。"
            "從一年前世界排名第十九位、從入圍賽一路打進IEM卡托維茲決賽並奪冠，"
            "到如今在Major舞台上擊敗剛拿下大滿貫的NAVI，"
            "這支由青訓體系培養出來的年輕隊伍用兩年的時間完成了所有選手一輩子的夢想。"
            "sh1ro在總決賽中以穩定的狙擊火力壓制了對面，"
            "Ax1Le與Hobbit在關鍵回合的發揮更徹底摧毀了對手的防線。"
            "獨聯體雙雄的時代正式來臨，而我們站在了頂峰。"
            "從Groove教練打造的青訓體系到如今的世界冠軍，這一刻屬於所有相信我們的人。"
        ),
        "triggers": [
            {
                "type": "competition_champion",
                "key": "pgl_major_stockholm_2021:gambit_2021",
            }
        ],
        "options": [
            opt("era", "獨聯體雙雄的時代正式來臨，這支青訓隊的成長令人難以置信。"),
            opt("generation", "sh1ro與Ax1Le在Major舞台上的表現，標誌著新一代獨聯體天才的誕生。"),
        ],
        "firstPersonOptions": [
            opt("academy", "我們是冠軍，這座Major獎盃是對我們青訓體系最好的證明。"),
            opt("era", "獨聯體已如閃電般歸來，而我們就是這個時代的開創者。"),
            opt("script", "擊敗剛拿下大滿貫的NAVI並且圓夢Major，沒有比這更完美的劇本了。"),
        ],
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
