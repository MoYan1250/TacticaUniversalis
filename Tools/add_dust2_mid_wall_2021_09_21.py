# -*- coding: utf-8 -*-
"""Append Dust II mid-doors wall historical event (2021-09-21)."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
events_path = root / "Assets/_Project/Data/JSON/career_events.json"
data = json.loads(events_path.read_text(encoding="utf-8"))

event_id = "hist_dust2_mid_doors_wall_2021_09_21"
existing = {e.get("id") for e in data.get("events", [])}
if event_id in existing:
    print(f"skip: {event_id} already present")
    raise SystemExit(0)


def opt(oid, label):
    return {"id": oid, "label": label, "tooltip": "", "effects": []}


event = {
    "id": event_id,
    "title": "Dust II中門新增牆壁，中門對狙走入歷史",
    "tier": 3,  # A
    "isMajor": True,
    "fireHour": 6312,  # 2021-09-21
    "mtthDays": 99999,
    "publicVisible": True,
    "subjectClubId": "",
    "firstPersonRosterRole": "Awper",
    "firstPersonRosterMapId": "Dust II",
    "firstPersonRosterAboveGrade": "A",
    "onFireEffects": [
        {
            "type": "add_role_map_proficiency",
            "key": "Awper:Dust II",
            "value": -50,
        }
    ],
    "description": (
        "Valve對Dust II進行了一次震撼性的改動，中門處新增了一道牆壁，"
        "徹底終結了這張地圖最經典的中門對狙玩法。"
        "自CS 1.6以來，中門對狙一直是Dust II的標誌性元素，無數傳奇狙擊手在此留下了永恆的畫面。"
        "如今這道牆壁將使T方出生點無法直接看見CT中路的動向，"
        "CT也無法再透過中門第一時間觀察T方的配槍配置。"
        "這項改動將影響所有層級的玩家與職業隊伍，"
        "狙擊手在Dust II上的影響力將被永久削弱，各隊必須重新制定中路控制策略與開局戰術。"
    ),
    "firstPersonDescription": (
        "Valve對Dust II進行了一次震撼性的改動，中門處新增了一道牆壁，"
        "徹底終結了這張地圖最經典的遊戲元素。"
        "自CS 1.6以來，中門對狙一直是Dust II的標誌性象徵，無數傳奇狙擊手在此留下了永恆的畫面。"
        "如今這道牆壁將使T方出生點無法直接看見CT中路的動向，"
        "CT也無法再透過中門第一時間觀察T方的配槍配置。"
        "這項改動將永久改變所有隊伍在Dust II上的戰術布置，"
        "狙擊手在這張地圖上的影響力將被大幅削弱。"
        "我們必須重新制定中路的控制策略與開局戰術。"
    ),
    "triggers": [{"type": "always"}],
    "options": [
        opt("advantage", "這對我們而言反而是好事，對手的狙擊手將失去原有的優勢。"),
        opt("unaffected", "我們本就不擅長這張地圖，這項改動對我們沒有影響。"),
    ],
    "firstPersonOptions": [
        opt("adapt", "我們接受這項改動，並將重新調整Dust II的戰術體系。"),
        opt("duty", "經典元素的消失令人惋惜，但適應新版本是職業隊伍的職責。"),
        opt("trial", "這對我們的狙擊手而言是一次嚴峻的考驗。"),
    ],
}

data.setdefault("events", []).append(event)
events_path.write_text(
    json.dumps(data, ensure_ascii=False, indent=2) + "\n",
    encoding="utf-8",
)
print(f"added: {event_id}")
