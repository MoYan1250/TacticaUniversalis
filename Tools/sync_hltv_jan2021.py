#!/usr/bin/env python3
"""Sync career/match JSON + CareerContentDatabase.asset to HLTV 2021-01-04 snapshot."""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "Assets" / "_Project" / "Data" / "JSON"
ASSET = ROOT / "Assets" / "_Project" / "Resources" / "Generated" / "CareerContentDatabase.asset"

# HLTV world ranking 2021-01-04 + Jan roster nicknames (5th may be incomplete on HLTV).
HLTV = {
    "astralis_2021": (1, 1000, ["Xyp9x", "dupreeh", "gla1ve", "device", "Magisk"]),
    "vitality_2021": (2, 900, ["shox", "RpK", "apEX", "ZywOo", "misutaaa"]),
    "navi_2021": (3, 604, ["flamie", "s1mple", "electroNic", "Boombl4", "Perfecto"]),
    "big_2021": (4, 580, ["tabseN", "tiziaN", "syrsoN", "XANTARES", "k1to"]),
    "heroic_2021": (5, 447, ["cadiaN", "b0RUP", "niko", "stavn", "TeSeS"]),
    "furia_2021": (6, 425, ["HEN1", "arT", "yuurih", "VINI", "KSCERATO"]),
    "g2_2021": (8, 316, ["NiKo", "huNter-", "kennyS", "amanek", "nexa"]),
    "liquid_2021": (9, 313, ["NAF", "EliGE", "Stewie2K", "Grim", "Twistzz"]),
    "mouz_2021": (10, 309, ["karrigan", "chrisJ", "frozen", "ropz", "Bymas"]),
    "virtuspro_2021": (11, 296, ["SANJI", "buster", "Qikert", "Jame", "YEKINDAR"]),
    "faze_2021": (13, 227, ["olofmeister", "rain", "Kjaerbye", "coldzera", "broky"]),
    "gambit_2021": (15, 190, ["HObbit", "interz", "Ax1Le", "sh1ro", "nafany"]),
    "nip_2021": (16, 187, ["twist", "REZ", "hampus", "nawwk", "Plopski"]),
    "eg_2021": (17, 187, ["stanislaw", "tarik", "Brehze", "Ethan", "CeRq"]),
    "godsent_2021": (18, 162, ["zehN", "emi", "STYKO", "Maden", "Farlig"]),
    "spirit_2021": (19, 148, ["chopper", "mir", "sdy", "iDISBALANCE", "magixx"]),
    "riders_2021": (40, 35, ["steel", "larsen", "alex", "mopoz", "shokz"]),
    "renegades_2021": (54, 25, ["Sico", "dexter", "INS", "Hatz", "malta"]),
    "cph_flames_2021": (60, 20, ["AcilioN", "Basso", "Daffu", "nicoodoz", "jabbi"]),
    "pain_2021": (65, 18, ["PKL", "NEKIZ", "hardzao", "biguzera", "saffee"]),
    "ence_2021": (66, 17, ["allu", "doto", "Jamppi", "Snappi", "Spinx"]),
    "tyloo_2021": (79, 13, ["Summer", "Attacker", "somebody", "SLOWLY", "DANK1NG"]),
    "sharks_2021": (117, 6, ["exit", "jnt", "pancc", "Lucaozy", "realzin"]),
    "entropiq_2021": (139, 4, ["El1an", "Lack1", "NickelBack", "krad", "forester"]),
}


def norm(s: str) -> str:
    return re.sub(r"[^a-z0-9]", "", s.lower())


def build_name_index(players: list[dict]) -> dict[str, str]:
    idx: dict[str, str] = {}
    for p in players:
        pid = p["id"]
        idx[norm(pid)] = pid
        dn = p.get("displayName") or ""
        if dn:
            idx[norm(dn)] = pid
    aliases = {
        "electronic": "electronic",
        "electronicn": "electronic",
        "apex": "apex",
        "zywoo": "zywoo",
        "art": "art",
        "vini": "vini",
        "kscerato": "kscerato",
        "hunter": "hunter",
        "niko": "niko",
        "cadian": "cadian",
        "teses": "teses",
        "rez": "rez",
        "cerq": "cerq",
        "ins": "ins",
        "hatz": "hatz",
        "pkl": "pkl",
        "nekiz": "nekiz",
        "lucaozy": "lucaozy",
        "summer": "summer",
        "attacker": "attacker",
        "slowly": "slowly",
        "dank1ng": "dank1ng",
        "el1an": "el1an",
        "lack1": "lack1",
        "nickelback": "nickelback",
    }
    for k, v in aliases.items():
        if v in {p["id"] for p in players}:
            idx[norm(k)] = v
    return idx


def resolve_roster(team_id: str, names: list[str], idx: dict[str, str], fallback: list[str]) -> list[str]:
    out: list[str] = []
    for n in names:
        pid = idx.get(norm(n))
        if pid and pid not in out:
            out.append(pid)
    for pid in fallback:
        if pid not in out:
            out.append(pid)
        if len(out) >= 5:
            break
    if len(out) > 5:
        out = out[:5]
    if len(out) < 5:
        raise SystemExit(f"{team_id}: need 5 players, got {len(out)}: {out}")
    return out


def patch_asset(clubs: list[dict]) -> None:
    text = ASSET.read_text(encoding="utf-8")
    start = text.index("  clubs:\n")
    end = text.index("\n  sponsors:", start)
    lines = ["  clubs:\n"]
    for c in sorted(clubs, key=lambda x: x["hltvRank"]):
        lines.append(f"  - id: {c['id']}\n")
        lines.append(f"    displayName: {c['displayName']}\n")
        lines.append(f"    region: {c['region']}\n")
        lines.append(f"    hltvRank: {c['hltvRank']}\n")
        lines.append(f"    hltvPoints: {c['hltvPoints']}\n")
        lines.append(f"    fame: {c['fame']}\n")
        lines.append(f"    ambition: {c['ambition']}\n")
        lines.append(f"    startingCash: {c['startingCash']}\n")
        lines.append("    playerIds:\n")
        for pid in c["playerIds"]:
            lines.append(f"    - {pid}\n")
    new_text = text[:start] + "".join(lines) + text[end:]
    ASSET.write_text(new_text, encoding="utf-8")
    print(f"patched asset clubs section ({len(clubs)} clubs)")


def main() -> None:
    players = json.loads((DATA / "players.json").read_text(encoding="utf-8"))["players"]
    idx = build_name_index(players)
    career_clubs = json.loads((DATA / "career_clubs.json").read_text(encoding="utf-8"))
    teams = json.loads((DATA / "teams.json").read_text(encoding="utf-8"))
    career_players = json.loads((DATA / "career_players.json").read_text(encoding="utf-8"))

    old_rosters = {c["id"]: c.get("playerIds", []) for c in career_clubs["clubs"]}
    team_by_id = {t["id"]: t for t in teams["teams"]}

    new_clubs = []
    for team_id, (rank, pts, jan_names) in sorted(HLTV.items(), key=lambda x: x[1][0]):
        old = next(c for c in career_clubs["clubs"] if c["id"] == team_id)
        fallback = old.get("playerIds") or team_by_id[team_id]["playerIds"]
        roster = resolve_roster(team_id, jan_names, idx, list(fallback))
        fame = max(4, min(100, round(pts / 10)))
        ambition = max(55, min(95, 50 + (100 - min(rank, 100)) // 2))
        cash = 1_000_000 + pts * 800
        new_clubs.append(
            {
                "id": team_id,
                "displayName": old["displayName"],
                "region": old["region"],
                "hltvRank": rank,
                "hltvPoints": pts,
                "fame": fame,
                "ambition": ambition,
                "startingCash": cash,
                "playerIds": roster,
            }
        )
        team_by_id[team_id]["playerIds"] = roster

    career_clubs["clubs"] = new_clubs
    teams["teams"] = [team_by_id[t["id"]] for t in teams["teams"]]

    # Reassign career player clubId from rosters.
    roster_club: dict[str, str] = {}
    for c in new_clubs:
        for pid in c["playerIds"]:
            roster_club[pid] = c["id"]
    for p in career_players["players"]:
        pid = p["id"]
        if pid in roster_club:
            p["clubId"] = roster_club[pid]

    (DATA / "career_clubs.json").write_text(
        json.dumps(career_clubs, indent=2, ensure_ascii=False) + "\n", encoding="utf-8"
    )
    (DATA / "teams.json").write_text(
        json.dumps(teams, indent=2, ensure_ascii=False) + "\n", encoding="utf-8"
    )
    (DATA / "career_players.json").write_text(
        json.dumps(career_players, indent=2, ensure_ascii=False) + "\n", encoding="utf-8"
    )

    patch_asset(new_clubs)
    print("done")


if __name__ == "__main__":
    main()
