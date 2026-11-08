# -*- coding: utf-8 -*-
"""Append Complexity transfer-list / roster breakup event (2021-11-03)."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
events_path = root / "Assets/_Project/Data/JSON/career_events.json"
data = json.loads(events_path.read_text(encoding="utf-8"))
event_id = "hist_complexity_jks_es3tag_transfer_list_2021_11_03"
existing = {e.get("id") for e in data.get("events", [])}
if event_id in existing:
    print(f"skip: {event_id}")
    raise SystemExit(0)


def opt(oid, label):
    return {"id": oid, "label": label, "tooltip": "", "effects": []}


event = {
    "id": event_id,
    "title": "Complexity宣布jks、es3tag進入交易名單，陣容解體",
    "tier": 4,  # B
    "isMajor": True,
    "fireHour": 7344,  # 2021-11-03
    "mtthDays": 99999,
    "publicVisible": True,
    "subjectClubId": "complexity_2021",
    "onFireEffects": [
        {"type": "add_exposure", "value": -5},
    ],
    "onFireRosterOps": [
        # Ensure es3tag is on Complexity for the AI timeline before listing
        # (career start places him on Cloud9; he joined Complexity historically).
        {
            "type": "join_club",
            "playerId": "es3tag",
            "clubId": "complexity_2021",
            "squadStatus": "starter",
        },
        {
            "type": "request_transfer",
            "playerId": "es3tag",
            "squadStatus": "bench",
            "clubId": "",
        },
        {
            "type": "request_transfer",
            "playerId": "jks",
            "squadStatus": "bench",
            "clubId": "",
        },
    ],
    "description": (
        "Complexity正式將jks與es3tag列入轉會名單，這支曾被稱為巨像的歐洲混編陣容正式宣告解體。"
        "blameF與poizon已於此前進入自由市場，k0nfig因手腕受傷離隊，coldzera的短期合約也將在年底到期。"
        "隊伍在IEM Fall歐洲區僅獲得第十七至二十名，未能取得Major參賽資格，這成為壓垮駱駝的最後一根稻草。"
        "教練peacemaker表示隊伍內部在IEM Fall期間發生了不和諧的事件，裂痕已無法修補。"
        "Complexity表示將繼續留在CS領域，並在合適的時間重新組建隊伍。"
    ),
    "firstPersonDescription": (
        "我們正式將jks與es3tag列入轉會名單，"
        "這標誌著我們過去兩年圍繞歐洲陣容打造計畫的徹底終結。"
        "此前blameF、poizon與k0nfig已相繼離隊，coldzera的短期合約也將在年底到期。"
        "未能打入PGL斯德哥爾摩Major是壓垮這項計畫的最後一根稻草，"
        "我們在IEM Fall歐洲區以第十七至二十名作收，這項結果直接導致管理層決定重建隊伍。"
        "隊伍內部在IEM Fall期間發生了不和諧的事件，選手間的裂痕已無法修補。"
        "我們將繼續留在CS領域，並在合適的時間重新組建隊伍。"
    ),
    "triggers": [{"type": "always"}],
    "options": [
        opt("collapse", "未能打入Major的代價，一支強隊就此崩塌。"),
        opt("fail", "他們的歐洲實驗最終以失敗告終。"),
        opt("market", "這些選手流入市場，或許我們可以從中獲益。"),
    ],
    "firstPersonOptions": [
        opt("thanks", "感謝這幾位選手為隊伍所做的一切，我們必須向前看。"),
        opt("pain", "這是一次痛苦的決定，但為了未來的發展，我們別無選擇。"),
    ],
}

data.setdefault("events", []).append(event)
events_path.write_text(
    json.dumps(data, ensure_ascii=False, indent=2) + "\n",
    encoding="utf-8",
)
print(f"added: {event_id}")
