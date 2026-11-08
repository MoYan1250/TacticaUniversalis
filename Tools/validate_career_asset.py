"""Validate CareerContentDatabase.asset like Unity CareerContentValidator."""
import re
from pathlib import Path

asset = Path(__file__).resolve().parents[1] / "Assets/_Project/Resources/Generated/CareerContentDatabase.asset"
text = asset.read_text(encoding="utf-8")

players_block = text.split("  players:\n", 1)[1].split("\n  clubs:\n", 1)[0]
clubs_block = text.split("\n  clubs:\n", 1)[1].split("\n  sponsors:\n", 1)[0]

errors = []
player_ids = []
for m in re.finditer(r"^  - id: (.+)$", players_block, re.M):
    pid = m.group(1).strip()
    if pid in player_ids:
        errors.append(f"duplicate player id: {pid}")
    player_ids.append(pid)

player_id_set = set(player_ids)
for m in re.finditer(
    r"^  - id: (.+?)\n(?:(?:    .+\n)*?)    power: (-?\d+)\n(?:(?:    .+\n)*?)    potential: (-?\d+)",
    players_block,
    re.M,
):
    pid, power, pot = m.group(1), int(m.group(2)), int(m.group(3))
    if power < 0 or power > 100:
        errors.append(f"{pid}: power {power}")
    if pot < 0 or pot > 100:
        errors.append(f"{pid}: potential {pot}")

clubs = []
for cm in re.finditer(
    r"^  - id: (.+?)\n(?:(?:    .+\n)*?)    playerIds:\n((?:    - .+\n)+)",
    clubs_block,
    re.M,
):
    cid, roster = cm.group(1), cm.group(2)
    pids = [ln.strip()[2:] for ln in roster.strip().split("\n")]
    clubs.append((cid, pids))
    if len(pids) != 5:
        errors.append(f"{cid}: expected 5 roster, got {len(pids)}")
    for pid in pids:
        if pid not in player_id_set:
            errors.append(f"{cid}: unknown player '{pid}'")

if len(player_ids) < 40:
    errors.append(f"players count {len(player_ids)} < 40")
if len(clubs) < 8:
    errors.append(f"clubs count {len(clubs)} < 8")

sponsors_block = text.split("\n  sponsors:\n", 1)[1].split("\n  competitions:\n", 1)[0]
comps_block = text.split("\n  competitions:\n", 1)[1].split("\n  facilities:\n", 1)[0]
sponsor_count = len(re.findall(r"^  - id: ", sponsors_block, re.M))
comp_count = len(re.findall(r"^  - id: ", comps_block, re.M))
if sponsor_count < 4:
    errors.append(f"sponsors count {sponsor_count} < 4")
if comp_count < 6:
    errors.append(f"competitions count {comp_count} < 6")

print("\n".join(errors) if errors else "OK")
print(f"players={len(player_ids)} clubs={len(clubs)} sponsors={sponsor_count} comps={comp_count}")
