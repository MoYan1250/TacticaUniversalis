import json
from collections import Counter
from pathlib import Path

root = Path(__file__).resolve().parents[1] / "Assets" / "_Project" / "Data" / "JSON"
clubs = json.loads((root / "career_clubs.json").read_text(encoding="utf-8"))["clubs"]
cp = json.loads((root / "career_players.json").read_text(encoding="utf-8"))["players"]
players = {p["id"] for p in cp}
player_club = {p["id"]: p.get("clubId") for p in cp}
errors = []
club_ids = set()
for club in clubs:
    cid = club["id"]
    if cid in club_ids:
        errors.append(f"dup club {cid}")
    club_ids.add(cid)
    pids = club.get("playerIds") or []
    if len(pids) != 5:
        errors.append(f"{cid}: expected 5 players got {len(pids)}")
    for pid in pids:
        if pid not in players:
            errors.append(f"{cid}: unknown player '{pid}'")
        elif player_club.get(pid) != cid:
            errors.append(f"{cid}: player '{pid}' clubId is '{player_club.get(pid)}'")
allp = [p for c in clubs for p in c["playerIds"]]
for p, n in Counter(allp).items():
    if n > 1:
        errors.append(f"player '{p}' on {n} club rosters")
for pid, cid in player_club.items():
    if cid and cid not in club_ids:
        errors.append(f"{pid}: unknown club '{cid}'")
print("\n".join(errors) if errors else "OK")
print(f"total: {len(errors)}")
