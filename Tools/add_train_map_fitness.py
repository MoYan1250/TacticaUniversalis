# -*- coding: utf-8 -*-
import json
import math
from pathlib import Path

ordered = ["E", "D", "C", "B", "B+", "A", "A+", "S"]
idx = {g: i for i, g in enumerate(ordered)}


def normalize(g):
    if not g:
        return "C"
    g = str(g).strip().upper()
    aliases = {"A-": "A", "B-": "B", "C+": "C"}
    g = aliases.get(g, g)
    return g if g in idx else "C"


def avg_grade(grades):
    if not grades:
        return "C"
    vals = [idx[normalize(g)] for g in grades]
    mean = sum(vals) / len(vals)
    nearest = int(math.floor(mean + 0.5))
    nearest = max(0, min(len(ordered) - 1, nearest))
    return ordered[nearest]


def main():
    p = Path(__file__).resolve().parents[1] / "Assets/_Project/Data/JSON/players.json"
    data = json.loads(p.read_text(encoding="utf-8"))
    players = data["players"]
    samples = []
    for pl in players:
        mf = pl.get("mapFitness") or []
        mf = [m for m in mf if (m.get("map") or "").lower() != "train"]
        grades = [m.get("grade") for m in mf if m.get("grade")]
        train_grade = avg_grade(grades)
        mf.append({"map": "Train", "grade": train_grade, "proficiency": 0})
        pl["mapFitness"] = mf
        if len(samples) < 5:
            samples.append((pl.get("id"), train_grade, [(m["map"], m["grade"]) for m in mf]))

    p.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    data2 = json.loads(p.read_text(encoding="utf-8"))
    assert all(any(m.get("map") == "Train" for m in pl["mapFitness"]) for pl in data2["players"])
    print("updated", len(players))
    for pid, grade, rows in samples:
        print(pid, "Train=" + grade, rows)


if __name__ == "__main__":
    main()
