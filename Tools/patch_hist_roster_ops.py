# -*- coding: utf-8 -*-
import json
from pathlib import Path

p = Path(__file__).resolve().parents[1] / "Assets/_Project/Data/JSON/career_events.json"
data = json.loads(p.read_text(encoding="utf-8"))


def squad(pid, status="bench"):
    return {"type": "set_squad", "playerId": pid, "squadStatus": status, "clubId": ""}


def join(pid, club, status="starter"):
    return {"type": "join_club", "playerId": pid, "clubId": club, "squadStatus": status}


def release(pid):
    return {"type": "release", "playerId": pid, "clubId": "", "squadStatus": "bench"}


def req(pid, status="bench"):
    return {"type": "request_transfer", "playerId": pid, "squadStatus": status, "clubId": ""}


ops_by_id = {
    # FaZe: olof returns as bench; coldzera already left historically.
    "hist_olofmeister_faze_bench_2021_01_04": [
        release("coldzera"),
        squad("olofmeister", "bench"),
    ],
    # Twistzz missing — only demote kjaerbye for now.
    "hist_twistzz_to_faze_2021_01_31": [
        squad("kjaerbye", "bench"),
        join("twistzz", "faze_2021", "starter"),  # no-op until career player exists
    ],
    # dexter missing — karrigan to bench on mouz.
    "hist_dexter_to_mouz_2021_02_11": [
        squad("karrigan", "bench"),
        join("dexter", "mouz_2021", "starter"),
    ],
    "hist_olofmeister_inactive_faze_2021_02_15": [
        squad("olofmeister", "bench"),
    ],
    "hist_karrigan_returns_faze_2021_02_15": [
        join("karrigan", "faze_2021", "starter"),
    ],
    # vester missing — no roster op
    "hist_astralis_seven_man_2021_02_17": [],
    "hist_g2_kennys_benched_2021_03_04": [
        squad("kennyS", "bench"),
        join("jackz", "g2_2021", "starter"),
    ],
    "hist_cloud9_colossus_fail_2021_03_26": [
        req("alex", "bench"),
        req("woxic", "bench"),
        req("es3tag", "bench"),
        req("floppy", "bench"),
        req("mezii", "bench"),
    ],
    # b1t missing — flamie to bench when AI (non-player NAVI)
    "hist_b1t_replaces_flamie_navi_2021_04_11": [
        squad("flamie", "bench"),
        join("b1t", "navi_2021", "starter"),
    ],
    # Bubzkji missing
    "hist_xyp9x_benched_astralis_2021_04_13": [
        squad("Xyp9x", "bench"),
        join("Bubzkji", "astralis_2021", "starter"),
    ],
}

found = set()
for e in data["events"]:
    eid = e.get("id")
    if eid not in ops_by_id:
        continue
    ops = ops_by_id[eid]
    if ops:
        e["onFireRosterOps"] = ops
    elif "onFireRosterOps" in e:
        del e["onFireRosterOps"]
    found.add(eid)

missing = set(ops_by_id) - found
if missing:
    raise SystemExit(f"missing events: {missing}")

p.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print("patched", len(found))
for eid in sorted(found):
    print(eid, len(ops_by_id[eid]), "ops")
