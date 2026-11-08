# -*- coding: utf-8 -*-
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
events_path = root / "Assets/_Project/Data/JSON/career_events.json"
players_path = root / "Assets/_Project/Data/JSON/career_players.json"

data = json.loads(events_path.read_text(encoding="utf-8"))
players_data = json.loads(players_path.read_text(encoding="utf-8"))


def opt(oid, label):
    return {"id": oid, "label": label, "tooltip": "", "effects": []}


# Lucky is required for Astralis August roster op.
if not any(p.get("id") == "lucky" for p in players_data.get("players", [])):
    players_data["players"].append(
        {
            "id": "lucky",
            "displayName": "Lucky",
            "age": 18,
            "role": "Awper",
            "clubId": "",
            "power": 52,
            "potential": 72,
            "fame": 35,
            "loyalty": 50,
            "professionalism": 80,
            "trajectory": "rising",
            "monthlySalary": 50000,
            "marketValue": 80000,
            "debutYear": 2018,
            "squadStatus": "bench",
        }
    )
    players_path.write_text(
        json.dumps(players_data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    print("added career player lucky")

new_events = [
    {
        "id": "hist_big_lists_xantares_2021_08_01",
        "title": "BIG掛牌XANTARES",
        "tier": 5,  # C
        "isMajor": False,
        "fireHour": 5088,  # 2021-08-01
        "mtthDays": 99999,
        "publicVisible": True,
        "subjectClubId": "big_2021",
        "onFireEffects": [
            {"type": "add_exposure", "value": -1},
            {"type": "add_estate", "key": "star_players", "value": -2},
            {"type": "add_estate", "key": "public_opinion", "value": -1},
        ],
        "onFireRosterOps": [
            {
                "type": "request_transfer",
                "playerId": "xantares",
                "squadStatus": "bench",
                "clubId": "",
            }
        ],
        "description": (
            "BIG正式將XANTARES掛牌，這位土耳其選手將結束在德國的兩年生涯。"
            "XANTARES自2019年從Space Soldiers加入BIG以來，幫助隊伍贏得DreamHack萊比錫公開賽冠軍，"
            "並在2020年ESL One科隆歐洲區獲得亞軍。"
            "據傳BIG已開始尋找替代人選，而XANTARES預計將返回土耳其加入Eternal Fire。"
        ),
        "firstPersonDescription": (
            "我們正式將XANTARES列入轉會名單，這位土耳其選手將結束在BIG為期兩年的生涯。"
            "XANTARES自2019年從Space Soldiers加入以來，為我們贏得了DreamHack萊比錫公開賽冠軍，"
            "並在2020年打入ESL One科隆歐洲區決賽。"
            "管理層與選手本人共同做出了這項決定，我們感謝他為隊伍做出的貢獻。"
        ),
        "triggers": [{"type": "always"}],
        "options": [
            opt("europe_end", "他在歐洲的旅程就此告一段落。"),
            opt("home", "他將回到土耳其，或許那才是屬於他的舞台。"),
        ],
        "firstPersonOptions": [
            opt("thanks", "感謝XANTARES的付出，祝福他未來一切順利。"),
            opt("fair", "這對雙方都是合理的選擇。"),
        ],
    },
    {
        "id": "hist_fnatic_rebuild_alex_mezii_2021_08_07",
        "title": "fnatic正式啟動重建，下放Golden與JW，簽下ALEX與mezii轉為國際陣容",
        "tier": 4,  # B
        "isMajor": True,
        "fireHour": 5232,  # 2021-08-07
        "mtthDays": 99999,
        "publicVisible": True,
        "subjectClubId": "fnatic_2021",
        "onFireEffects": [
            {"type": "add_exposure", "value": -3},
            {"type": "add_fame", "value": -2},
            {"type": "add_board", "value": -2},
            {"type": "add_estate", "key": "veterans", "value": -5},
            {"type": "add_estate", "key": "public_opinion", "value": -3},
            {"type": "add_estate", "key": "star_players", "value": -3},
        ],
        "onFireRosterOps": [
            {
                "type": "set_squad",
                "playerId": "golden",
                "squadStatus": "bench",
                "clubId": "",
            },
            {
                "type": "set_squad",
                "playerId": "jw",
                "squadStatus": "bench",
                "clubId": "",
            },
            {
                "type": "join_club",
                "playerId": "alex",
                "clubId": "fnatic_2021",
                "squadStatus": "starter",
            },
            {
                "type": "join_club",
                "playerId": "mezii",
                "clubId": "fnatic_2021",
                "squadStatus": "starter",
            },
        ],
        "description": (
            "fnatic正式啟動重建，將指揮Golden與傳奇選手JW下放至替補席，同時簽下英國選手mezii與法國指揮ALEX。"
            "這標誌著這支瑞典CS的圖騰隊伍正式放棄純瑞典陣容，轉向國際化路線。"
            "JW自2013年加入fnatic以來，為隊伍贏得三座Major冠軍，是CSGO歷史上最偉大的狙擊手之一。"
            "ALEX在Cloud9解散後成為自由選手，mezii則從Endpoint轉會而來。"
        ),
        "firstPersonDescription": (
            "我們正式啟動CSGO部門的重建計畫，將Golden與JW下放至替補席，同時簽下ALEX與mezii加入先發陣容。"
            "這項決定標誌著fnatic將放棄純瑞典陣容，轉向國際化路線。"
            "Golden自2020年回歸以來擔任指揮，而JW則是fnatic自2013年以來的標誌性人物，三屆Major冠軍得主。"
            "管理層感謝兩位選手多年來的貢獻，但相信新的方向能為隊伍帶來新的活力。"
        ),
        "triggers": [{"type": "always"}],
        "options": [
            opt("sweden", "瑞典CS的象徵正在消逝。"),
            opt("era", "JW的離開標誌著一個時代的結束。"),
            opt("watch", "國際陣容能否為fnatic帶來新生，仍有待觀察。"),
        ],
        "firstPersonOptions": [
            opt("thanks", "感謝Golden與JW為fnatic所做的一切，但我們必須向前看。"),
            opt("trend", "國際陣容是未來的趨勢，我們必須跟上時代。"),
        ],
    },
    {
        "id": "hist_astralis_lucky_starts_xyp9x_bench_2021_08_18",
        "title": "Astralis將Xyp9x列為替補，啟用Lucky作為首發狙擊手",
        "tier": 4,  # B
        "isMajor": True,
        "fireHour": 5496,  # 2021-08-18
        "mtthDays": 99999,
        "publicVisible": True,
        "subjectClubId": "astralis_2021",
        "onFireEffects": [
            {"type": "add_exposure", "value": -2},
            {"type": "add_board", "value": -2},
            {"type": "add_estate", "key": "veterans", "value": -3},
            {"type": "add_estate", "key": "star_players", "value": -2},
            {"type": "add_estate", "key": "public_opinion", "value": -2},
            {"type": "add_roster_pressure", "value": 3},
        ],
        "onFireRosterOps": [
            {
                "type": "set_squad",
                "playerId": "Xyp9x",
                "squadStatus": "bench",
                "clubId": "",
            },
            {
                "type": "join_club",
                "playerId": "lucky",
                "clubId": "astralis_2021",
                "squadStatus": "starter",
            },
        ],
        "description": (
            "Astralis在EPL S14的賽事名單中將Xyp9x列為替補，同時啟用十八歲的年輕狙擊手Lucky作為先發。"
            "這是device離隊後，Astralis首次啟用專職狙擊手參加正式賽事。此前狙擊手職責由dupreeh與gla1ve分擔。"
            "gla1ve曾明確表示沒有專職狙擊手，重返巔峰將是癡人說夢。"
            "Lucky於七月底從Tricked簽約加入Astralis作為第六人，他將在EPL S14首戰對陣Team Spirit的比賽中首次亮相。"
            "Xyp9x在接受採訪時表示自己正在評估未來方向，不排除離開Astralis加入其他隊伍的可能性。"
            "目前陣容中僅gla1ve已續約至2024年，dupreeh、Magisk與Xyp9x的合約都將在年底到期。"
        ),
        "firstPersonDescription": (
            "我們在向ESL官方提交的EPL S14賽事名單中，將Xyp9x列為替補，並啟用年輕狙擊手Lucky作為先發。"
            "Xyp9x自2020年中因職業倦怠申請休假後，狀態一直未能完全恢復。"
            "同時，在device離隊後，我們缺乏專職狙擊手的問題日益嚴重，"
            "gla1ve曾明確表示沒有專職狙擊手，重返巔峰將是癡人說夢。"
            "我們於七月底從Tricked簽下Lucky作為第六人，他將在本屆賽事中承擔狙擊手職責。"
            "Xyp9x在接受採訪時表示，他正在評估未來方向，也不排除離開Astralis加入其他隊伍的可能性。"
            "gla1ve已與俱樂部續約三年，而dupreeh、Magisk與Xyp9x的合約將在年底到期。"
        ),
        "triggers": [{"type": "always"}],
        "options": [
            opt("clutch_era", "殘局大師的時代或許已經結束。"),
            opt("collapse", "這是Astralis王朝全面解體的信號。"),
            opt("gap", "Lucky能否扛起device留下的空缺，仍是未知數。"),
        ],
        "firstPersonOptions": [
            opt("needed", "這是為了隊伍競爭力所做的必要調整。"),
            opt("respect", "感謝Xyp9x多年來的付出，我們尊重他的選擇。"),
        ],
    },
]

existing = {e.get("id") for e in data["events"]}
for e in new_events:
    if e["id"] in existing:
        data["events"] = [x for x in data["events"] if x.get("id") != e["id"]]
    data["events"].append(e)

events_path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
text = events_path.read_text(encoding="utf-8")
assert "BIG掛牌XANTARES" in text
assert "fnatic正式啟動重建" in text
assert "啟用Lucky作為首發狙擊手" in text
assert any(p.get("id") == "lucky" for p in players_data["players"])
print("total", len(data["events"]), "ok")
