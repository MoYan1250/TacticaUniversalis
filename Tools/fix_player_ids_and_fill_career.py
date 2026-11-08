# -*- coding: utf-8 -*-
"""Fix event playerId casing to match players.json; add missing career players."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
events_path = root / "Assets/_Project/Data/JSON/career_events.json"
players_path = root / "Assets/_Project/Data/JSON/career_players.json"
match_path = root / "Assets/_Project/Data/JSON/players.json"
docs_path = root / "Assets/_Project/Docs/HistoricalMissionTree.md"

events = json.loads(events_path.read_text(encoding="utf-8"))
career = json.loads(players_path.read_text(encoding="utf-8"))
match = json.loads(match_path.read_text(encoding="utf-8"))
match_by = {p["id"]: p for p in match["players"]}
existing = {p["id"] for p in career["players"]}

# Event rosterOp id renames → players.json canonical ids
ID_FIX = {
    "twistzz": "Twistzz",
    "jackz": "JACKZ",
    "obo": "oBo",
}

fixed_ops = 0
for e in events.get("events", []):
    for op in e.get("onFireRosterOps") or []:
        pid = op.get("playerId")
        if pid in ID_FIX:
            op["playerId"] = ID_FIX[pid]
            fixed_ops += 1
            print(f"event {e['id']}: {pid} -> {op['playerId']}")

# Seed career rows from match defs where possible.
def from_match(
    pid,
    *,
    club_id="",
    squad="bench",
    role=None,
    power=None,
    potential=None,
    salary=None,
    trajectory="standard",
    loyalty=50,
    professionalism=80,
):
    m = match_by.get(pid, {})
    fame = int(m.get("fame") or 40)
    mv = int(m.get("marketValue") or 100000)
    age = int(m.get("age") or 22)
    debut = int(m.get("careerStartYear") or 2018)
    primary = role or m.get("primaryRole") or "Rifler"
    # Rough career power from fame band if not overridden.
    pwr = power if power is not None else max(40, min(90, fame + 4))
    pot = potential if potential is not None else max(pwr, min(95, fame + 12))
    sal = salary if salary is not None else max(40000, mv // 4)
    return {
        "id": pid,
        "displayName": m.get("displayName") or pid,
        "age": age,
        "role": primary,
        "clubId": club_id,
        "power": pwr,
        "potential": pot,
        "fame": fame,
        "loyalty": loyalty,
        "professionalism": professionalism,
        "trajectory": trajectory,
        "monthlySalary": sal,
        "marketValue": mv,
        "debutYear": debut,
        "squadStatus": squad,
    }


new_players = [
    # Critical rosterOps
    from_match(
        "Twistzz",
        club_id="",
        squad="bench",
        power=84,
        potential=90,
        salary=280000,
        loyalty=55,
        professionalism=86,
    ),
    from_match(
        "dexter",
        club_id="",
        squad="bench",
        role="Igl",
        power=74,
        potential=80,
        salary=150000,
    ),
    from_match(
        "JACKZ",
        club_id="",
        squad="bench",
        power=76,
        potential=80,
        salary=180000,
    ),
    from_match(
        "b1t",
        club_id="navi_2021",
        squad="bench",
        role="Entry",
        power=78,
        potential=93,
        salary=120000,
        trajectory="rising",
        loyalty=70,
        professionalism=85,
    ),
    from_match(
        "Bubzkji",
        club_id="astralis_2021",
        squad="bench",
        power=72,
        potential=82,
        salary=100000,
        trajectory="rising",
    ),
    # Narrative / secondary
    from_match(
        "f0rest",
        club_id="",
        squad="bench",
        power=70,
        potential=68,
        salary=120000,
        trajectory="evergreen",
        professionalism=88,
    ),
    from_match(
        "autimatic",
        club_id="",
        squad="bench",
        power=72,
        potential=74,
        salary=140000,
    ),
    from_match(
        "oBo",
        club_id="eg_2021",
        squad="bench",
        power=74,
        potential=80,
        salary=160000,
    ),
    from_match(
        "michu",
        club_id="",
        squad="bench",
        power=70,
        potential=72,
        salary=100000,
    ),
    from_match(
        "vester",
        club_id="astralis_2021",
        squad="academy",
        power=58,
        potential=78,
        salary=40000,
        trajectory="rising",
    ),
    from_match(
        "vnG",
        club_id="",
        squad="bench",
        role="Igl",
        power=55,
        potential=60,
        salary=35000,
    ),
]

added = 0
for p in new_players:
    if p["id"] in existing:
        print(f"skip career player: {p['id']}")
        continue
    career.setdefault("players", []).append(p)
    existing.add(p["id"])
    added += 1
    print(f"added career player: {p['id']} club={p['clubId'] or '(free)'} squad={p['squadStatus']}")

events_path.write_text(
    json.dumps(events, ensure_ascii=False, indent=2) + "\n",
    encoding="utf-8",
)
players_path.write_text(
    json.dumps(career, ensure_ascii=False, indent=2) + "\n",
    encoding="utf-8",
)

# Docs refresh
doc = docs_path.read_text(encoding="utf-8")
old_block = (
    "- 選手（缺 `career_players.json`）：`twistzz`、`GeT_RiGhT`、`f0rest`、`autimatic`、`dexter`、`vester`、`vnG`、`Bubzkji`。"
    "\n- 已補：`gade`、`nitr0`、`osee`（2021年12月底 BIG／Liquid 歷史陣容異動用）。"
)
# Try several possible doc strings
replacements = [
    (
        "- 選手（缺 `career_players.json`）：`twistzz`、`GeT_RiGhT`、`f0rest`、`autimatic`、`dexter`、`vester`、`vnG`、`Bubzkji`。\n"
        "- 已補：`gade`、`nitr0`、`osee`（2021年12月底 BIG／Liquid 歷史陣容異動用）。",
        "- 選手（缺 `career_players.json`）：`GeT_RiGhT`（`players.json` 亦無）。\n"
        "- 已補生涯資料：`Twistzz`、`dexter`、`JACKZ`、`b1t`、`Bubzkji`、`f0rest`、`autimatic`、`oBo`、`michu`、`vester`、`vnG`、`gade`、`nitr0`、`osee`。\n"
        "- 事件 `playerId` 已與對戰庫大小寫對齊：`Twistzz`、`JACKZ`、`oBo`。",
    ),
]
for old, new in replacements:
    if old in doc:
        docs_path.write_text(doc.replace(old, new), encoding="utf-8")
        print("updated HistoricalMissionTree.md")
        break
else:
    # Fallback: append note under 依賴與前置工作 if marker exists
    marker = "## 依賴與前置工作"
    if marker in doc and "事件 `playerId` 已與對戰庫大小寫對齊" not in doc:
        insert = (
            marker
            + "\n\n"
            + "- 選手（缺 `career_players.json`）：`GeT_RiGhT`（`players.json` 亦無）。\n"
            + "- 已補生涯資料：`Twistzz`、`dexter`、`JACKZ`、`b1t`、`Bubzkji`、`f0rest`、`autimatic`、`oBo`、`michu`、`vester`、`vnG`、`gade`、`nitr0`、`osee`。\n"
            + "- 事件 `playerId` 已與對戰庫大小寫對齊：`Twistzz`、`JACKZ`、`oBo`。\n"
        )
        # Don't duplicate whole section — just print warning
        print("warn: docs block not exact-matched; please review HistoricalMissionTree.md manually")

print(f"done fixed_ops={fixed_ops} added_players={added} career_total={len(career['players'])}")
