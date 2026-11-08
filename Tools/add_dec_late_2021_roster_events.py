# -*- coding: utf-8 -*-
"""Append late-December 2021 roster events + missing career players."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
events_path = root / "Assets/_Project/Data/JSON/career_events.json"
players_path = root / "Assets/_Project/Data/JSON/career_players.json"

events = json.loads(events_path.read_text(encoding="utf-8"))
players = json.loads(players_path.read_text(encoding="utf-8"))

existing_player_ids = {p.get("id") for p in players.get("players", [])}
existing_event_ids = {e.get("id") for e in events.get("events", [])}


def opt(oid, label):
    return {"id": oid, "label": label, "tooltip": "", "effects": []}


new_players = [
    {
        "id": "gade",
        "displayName": "gade",
        "age": 26,
        "role": "Igl",
        "clubId": "",
        "power": 62,
        "potential": 60,
        "fame": 38,
        "loyalty": 50,
        "professionalism": 82,
        "trajectory": "standard",
        "monthlySalary": 120000,
        "marketValue": 180000,
        "debutYear": 2016,
        "squadStatus": "bench",
    },
    {
        "id": "nitr0",
        "displayName": "nitr0",
        "age": 26,
        "role": "Igl",
        "clubId": "",
        "power": 70,
        "potential": 68,
        "fame": 72,
        "loyalty": 55,
        "professionalism": 86,
        "trajectory": "standard",
        "monthlySalary": 200000,
        "marketValue": 350000,
        "debutYear": 2015,
        "squadStatus": "bench",
    },
    {
        "id": "osee",
        "displayName": "oSee",
        "age": 22,
        "role": "Awper",
        "clubId": "",
        "power": 68,
        "potential": 78,
        "fame": 48,
        "loyalty": 50,
        "professionalism": 80,
        "trajectory": "rising",
        "monthlySalary": 100000,
        "marketValue": 200000,
        "debutYear": 2018,
        "squadStatus": "bench",
    },
]

added_players = 0
for p in new_players:
    if p["id"] in existing_player_ids:
        print(f"skip player: {p['id']}")
        continue
    players.setdefault("players", []).append(p)
    added_players += 1
    print(f"added player: {p['id']}")

new_events = [
    {
        "id": "hist_big_gade_german_roster_2021_12_28",
        "title": "BIG宣布新賽季選手名單，簽下丹麥指揮gade重回全德語陣容",
        "tier": 5,  # C
        "isMajor": False,
        "fireHour": 8664,  # 2021-12-28
        "mtthDays": 99999,
        "publicVisible": True,
        "subjectClubId": "big_2021",
        "onFireEffects": [],
        "onFireRosterOps": [
            {
                "type": "join_club",
                "playerId": "gade",
                "clubId": "big_2021",
                "squadStatus": "starter",
            }
        ],
        "description": (
            "BIG宣布新賽季選手名單，丹麥指揮gade將加入隊伍，BIG將重回全德語陣容。"
            "gade此前曾效力於North與Heroic，他的加入將填補隊伍在指揮位置上的空缺。"
            "BIG在2021年表現起伏不定，管理層希望透過這次調整為隊伍帶來更穩定的戰術執行力與溝通效率。"
        ),
        "firstPersonDescription": (
            "我們正式宣布新賽季的選手名單，丹麥指揮gade將加入隊伍，BIG將重回全德語陣容。"
            "gade此前曾效力於North與Heroic，他的指揮風格與德國選手的紀律性相輔相成。"
            "管理層相信這項調整能為隊伍帶來更穩定的戰術執行力。"
            "我們將以這套全新陣容備戰2022年的賽事。"
        ),
        "triggers": [{"type": "always"}],
        "options": [
            opt("comms", "德語陣容的溝通優勢或許能讓他們走得更遠。"),
            opt("watch", "丹麥指揮登陸德國，這筆交易值得觀察。"),
        ],
        "firstPersonOptions": [
            opt("welcome", "歡迎gade加入，這是指揮位置的關鍵補強。"),
            opt("german", "重回全德語陣容將有助於提升團隊溝通效率。"),
        ],
    },
    {
        "id": "hist_ropz_leaves_mouz_2021_12_29",
        "title": "ropz告別mousesports，預計加盟FaZe",
        "tier": 4,  # B
        "isMajor": True,
        "fireHour": 8688,  # 2021-12-29
        "mtthDays": 99999,
        "publicVisible": True,
        "subjectClubId": "mouz_2021",
        "onFireEffects": [],
        "onFireRosterOps": [
            {
                "type": "release",
                "playerId": "ropz",
                "squadStatus": "bench",
                "clubId": "",
            }
        ],
        "description": (
            "ropz正式告別mousesports，結束他近五年的MOUZ生涯。"
            "他在告別聲明中表示，從FPL被發掘並獲得證明自己的機會，從那之後他就一直在實現夢想。"
            "ropz在MOUZ期間贏得九座冠軍，並在2018年至2020年間連續三年入選HLTV年度TOP20，最高排名第七位。"
            "根據多方報導，ropz預計將加盟FaZe Clan，與前指揮karrigan重聚。"
        ),
        "firstPersonDescription": (
            "我們正式宣布ropz將在年底合約到期後離開隊伍，結束他近五年的MOUZ生涯。"
            "ropz在告別聲明中表示，這段旅程從一開始就令他難以置信，"
            "他被從FPL中發掘並獲得證明自己的機會，從那之後他就一直在實現夢想。"
            "ropz在MOUZ期間幫助隊伍贏得九座冠軍獎盃，並在2018年至2020年間連續三年入選HLTV年度TOP20，最高排名第七位。"
            "根據多方報導，ropz預計將加盟FaZe Clan，與前指揮karrigan重聚。"
            "我們感謝ropz多年來的貢獻，祝福他在新的篇章中一切順利。"
        ),
        "triggers": [{"type": "always"}],
        "options": [
            opt("chapter", "愛沙尼亞天才結束了他在MOUZ的傳奇篇章。"),
            opt("faze", "FaZe銀河艦隊的最後一塊拼圖即將就位。"),
            opt("reunion", "他與karrigan的重聚令人期待。"),
        ],
        "firstPersonOptions": [
            opt("thanks", "感謝ropz五年來的付出，他將永遠是MOUZ歷史的一部分。"),
            opt("respect", "他的離開令人惋惜，但我們尊重他的選擇，祝福他前程似錦。"),
        ],
    },
    {
        "id": "hist_liquid_rebuild_2021_12_30",
        "title": "Liquid宣布FalleN、Grim、Stewie2K離隊，nitr0回歸、shox與oSee加盟",
        "tier": 3,  # A
        "isMajor": True,
        "fireHour": 8712,  # 2021-12-30
        "mtthDays": 99999,
        "publicVisible": True,
        "subjectClubId": "liquid_2021",
        "onFireEffects": [],
        "onFireRosterOps": [
            {"type": "release", "playerId": "fallen", "squadStatus": "bench", "clubId": ""},
            {"type": "release", "playerId": "grim", "squadStatus": "bench", "clubId": ""},
            {"type": "release", "playerId": "stewie2k", "squadStatus": "bench", "clubId": ""},
            {
                "type": "join_club",
                "playerId": "nitr0",
                "clubId": "liquid_2021",
                "squadStatus": "starter",
            },
            {
                "type": "join_club",
                "playerId": "shox",
                "clubId": "liquid_2021",
                "squadStatus": "starter",
            },
            {
                "type": "join_club",
                "playerId": "osee",
                "clubId": "liquid_2021",
                "squadStatus": "starter",
            },
        ],
        "description": (
            "Liquid正式確認FalleN、Stewie2K與Grim離隊，"
            "同時宣布nitr0將從Valorant回歸CSGO賽場，"
            "shox首次離開法國CS環境加盟Liquid，oSee從Extra Salt加入擔任專職狙擊手。"
            "Liquid在2021年表現不佳，世界排名一度跌至第十六位，"
            "管理層決定進行全面重組，僅保留EliGE與NAF兩名核心選手。"
            "這次重組是北美CS近年來規模最大的陣容變動之一。"
        ),
        "firstPersonDescription": (
            "我們正式宣布2022賽季的陣容重組計畫。"
            "Gabriel FalleN Toledo、Jake Stewie2K Yip與Michael Grim Wince將離開隊伍。"
            "同時我們歡迎Nick nitr0 Cannella的回歸，他將從Valorant重返CSGO賽場。"
            "Richard shox Papillon將首次離開法國CS環境，加入我們的國際陣容。"
            "Josh oSee Ohm將從Extra Salt加盟，擔任隊伍的專職狙擊手。"
            "這是一次徹底的重建，我們的目標是重返世界頂級行列。"
        ),
        "triggers": [{"type": "always"}],
        "options": [
            opt("quake", "北美CS最大規模的重組正在進行中。"),
            opt("shox", "shox首次離開法國環境，這筆交易意義深遠。"),
            opt("nitr0", "nitr0的回歸將為Liquid帶來急需的指揮能力。"),
        ],
        "firstPersonOptions": [
            opt("thanks", "感謝離隊選手們的付出，我們將帶著新的陣容迎接新的挑戰。"),
            opt("welcome", "歡迎nitr0回歸，shox與oSee的加入將為我們注入新的活力。"),
            opt("confidence", "這是北美CS最大規模的重組，我們對未來充滿信心。"),
        ],
    },
]

added_events = 0
for event in new_events:
    if event["id"] in existing_event_ids:
        print(f"skip event: {event['id']}")
        continue
    events.setdefault("events", []).append(event)
    added_events += 1
    print(f"added event: {event['id']}")

if added_players:
    players_path.write_text(
        json.dumps(players, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
if added_events:
    events_path.write_text(
        json.dumps(events, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )

# Update HistoricalMissionTree missing-player note
docs = root / "Assets/_Project/Docs/HistoricalMissionTree.md"
doc_text = docs.read_text(encoding="utf-8")
old = "- 選手（缺 `career_players.json`）：`twistzz`、`GeT_RiGhT`、`f0rest`、`autimatic`、`dexter`、`vester`、`vnG`、`Bubzkji`。"
new = (
    "- 選手（缺 `career_players.json`）：`twistzz`、`GeT_RiGhT`、`f0rest`、`autimatic`、`dexter`、`vester`、`vnG`、`Bubzkji`。"
    "\n- 已補：`gade`、`nitr0`、`osee`（2021年12月底 BIG／Liquid 歷史陣容異動用）。"
)
if old in doc_text and "已補：`gade`" not in doc_text:
    docs.write_text(doc_text.replace(old, new), encoding="utf-8")
    print("updated HistoricalMissionTree.md")

print(f"done players={added_players} events={added_events}")
