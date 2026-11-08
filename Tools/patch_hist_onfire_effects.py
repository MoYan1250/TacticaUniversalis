# -*- coding: utf-8 -*-
import json
from pathlib import Path

p = Path(__file__).resolve().parents[1] / "Assets/_Project/Data/JSON/career_events.json"
data = json.loads(p.read_text(encoding="utf-8"))

effects_by_id = {
    "hist_twistzz_to_faze_2021_01_31": [
        {"type": "add_exposure", "value": 3},
        {"type": "add_fame", "value": 2},
        {"type": "add_fans", "value": 1500},
        {"type": "add_board", "value": 2},
        {"type": "add_estate", "key": "public_opinion", "value": 3},
        {"type": "add_estate", "key": "star_players", "value": 2},
    ],
    "hist_karrigan_returns_faze_2021_02_15": [
        {"type": "add_exposure", "value": 4},
        {"type": "add_fame", "value": 2},
        {"type": "add_board", "value": 3},
        {"type": "add_estate", "key": "public_opinion", "value": 3},
        {"type": "add_estate", "key": "coaching_staff", "value": 2},
    ],
    "hist_olofmeister_inactive_faze_2021_02_15": [
        {"type": "add_exposure", "value": -2},
        {"type": "add_estate", "key": "public_opinion", "value": -2},
        {"type": "add_estate", "key": "veterans", "value": -2},
    ],
    "hist_g2_kennys_benched_2021_03_04": [
        {"type": "add_exposure", "value": -3},
        {"type": "add_fame", "value": -1},
        {"type": "add_board", "value": -2},
        {"type": "add_estate", "key": "public_opinion", "value": -2},
        {"type": "add_estate", "key": "star_players", "value": -3},
    ],
    "hist_dexter_to_mouz_2021_02_11": [
        {"type": "add_exposure", "value": 2},
        {"type": "add_board", "value": 1},
        {"type": "add_estate", "key": "coaching_staff", "value": 2},
    ],
    "hist_astralis_seven_man_2021_02_17": [
        {"type": "add_fame", "value": 1},
        {"type": "add_board", "value": -1},
        {"type": "add_estate", "key": "reserves", "value": 3},
        {"type": "add_estate", "key": "coaching_staff", "value": 1},
    ],
    "hist_gambit_iem_katowice_2021_02_21": [
        {"type": "add_exposure", "value": 7},
        {"type": "add_fame", "value": 5},
        {"type": "add_fans", "value": 8000},
        {"type": "add_board", "value": 5},
        {"type": "add_estate", "key": "public_opinion", "value": 6},
        {"type": "add_estate", "key": "star_players", "value": 4},
    ],
    "hist_cloud9_colossus_fail_2021_03_26": [
        {"type": "add_exposure", "value": -8},
        {"type": "add_fame", "value": -5},
        {"type": "add_fans", "value": -3000},
        {"type": "add_board", "value": -8},
        {"type": "add_estate", "key": "public_opinion", "value": -6},
        {"type": "add_estate", "key": "management", "value": -4},
    ],
}

found = set()
for e in data["events"]:
    eid = e.get("id")
    if eid in effects_by_id:
        e["onFireEffects"] = effects_by_id[eid]
        found.add(eid)

missing = set(effects_by_id) - found
if missing:
    raise SystemExit(f"missing events: {missing}")

p.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print("patched", len(found))
for eid in sorted(found):
    print(eid, len(effects_by_id[eid]), "effects")
