# -*- coding: utf-8 -*-
from pathlib import Path
from PIL import Image, ImageDraw
import numpy as np
import json

OV = Path(r"E:\TacticaUniversalis\Assets\_Project\Resources\Maps\ancient_overview.png")
OUT_PREV = Path(r"E:\TacticaUniversalis\Tools\backups\ancient_zones_preview.png")
OUT_JSON = Path(r"E:\TacticaUniversalis\Assets\_Project\Data\JSON\ancient.json")

ov = np.array(Image.open(OV).convert("RGBA"))
h, w = ov.shape[:2]
r, g, b, a = ov[:, :, 0].astype(int), ov[:, :, 1].astype(int), ov[:, :, 2].astype(int), ov[:, :, 3]
floor = a > 0
orange = (r > 140) & (g > 60) & (g < 160) & (b < 80) & (r > g + 20) & floor
print("orange", int(orange.sum()))
ys, xs = np.where(orange)
if len(xs):
    left = xs < w * 0.5
    if left.any():
        print("A-ish", xs[left].mean() / w, 1 - ys[left].mean() / h, int(left.sum()))
    if (~left).any():
        print("B-ish", xs[~left].mean() / w, 1 - ys[~left].mean() / h, int((~left).sum()))

# Image px top-left; T bottom / CT top on Ancient overview
landmarks = {
    "t_spawn": (512, 940),
    "ct_spawn": (512, 70),
    "a_site": (220, 220),
    "b_site": (780, 420),
    "mid": (520, 520),
    "bottom_mid": (520, 700),
    "top_mid": (520, 320),
    "donut": (340, 420),
    "a_main": (200, 380),
    "a_long": (160, 300),
    "temple": (340, 180),
    "water": (780, 820),
    "ruins": (820, 650),
    "ramp": (820, 500),
    "cave": (680, 480),
    "catwalk": (640, 560),
    "house": (560, 220),
    "connector": (560, 140),
    "t_stairs": (512, 850),
    "tunnel": (620, 880),
    "xbox": (500, 640),
}

img = Image.fromarray(ov)
d = ImageDraw.Draw(img)
for k, (x, y) in landmarks.items():
    d.ellipse([x - 5, y - 5, x + 5, y + 5], fill=(255, 40, 40, 255), outline=(255, 255, 0))
    d.text((x + 7, y - 8), k, fill=(255, 255, 80))
img.save(OUT_PREV)
print("preview", OUT_PREV)

bonuses = {
    "a_site": {"attackerTeamworkBonus": 0.10, "defenderAwarenessBonus": 0.05},
    "b_site": {"attackerTeamworkBonus": 0.05, "defenderAwarenessBonus": 0.10},
    "mid": {"defenderReactionBonus": 0.05},
    "donut": {"defenderReactionBonus": 0.10},
    "ramp": {"defenderReactionBonus": 0.05},
    "cave": {"defenderReactionBonus": 0.05},
    "catwalk": {"attackerAimBonus": 0.05, "defenderReactionBonus": 0.05},
    "temple": {"defenderReactionBonus": 0.05},
    "water": {"defenderReactionBonus": 0.05},
    "a_main": {"defenderReactionBonus": 0.05},
    "a_long": {"attackerAimBonus": 0.05},
}

display = {
    "t_spawn": "T Spawn",
    "ct_spawn": "CT Spawn",
    "a_site": "A Site",
    "b_site": "B Site",
    "mid": "Mid",
    "bottom_mid": "Bottom Mid",
    "top_mid": "Top Mid",
    "donut": "Donut",
    "a_main": "A Main",
    "a_long": "A Long",
    "temple": "Temple",
    "water": "Water",
    "ruins": "Ruins",
    "ramp": "B Ramp",
    "cave": "Cave",
    "catwalk": "Catwalk",
    "house": "House",
    "connector": "Connector",
    "t_stairs": "T Stairs",
    "tunnel": "Tunnel",
    "xbox": "Xbox",
}

zones = []
for zid, (x, y) in landmarks.items():
    z = {
        "id": zid,
        "displayName": display[zid],
        "x": round(x / 1024, 3),
        "y": round(1 - y / 1024, 3),
        "attackerAimBonus": 0.0,
        "defenderReactionBonus": 0.0,
        "attackerTeamworkBonus": 0.0,
        "defenderAwarenessBonus": 0.0,
    }
    z.update(bonuses.get(zid, {}))
    zones.append(z)
    print(f"{zid}: u={z['x']:.3f} v={z['y']:.3f}")

routes = [
    {"id": "t_to_a_main", "from": "t_spawn", "to": "a_site",
     "waypoints": ["t_spawn", "t_stairs", "a_main", "a_long", "a_site"]},
    {"id": "t_to_a_mid", "from": "t_spawn", "to": "a_site",
     "waypoints": ["t_spawn", "t_stairs", "bottom_mid", "donut", "a_site"]},
    {"id": "t_to_b_water", "from": "t_spawn", "to": "b_site",
     "waypoints": ["t_spawn", "tunnel", "water", "ruins", "ramp", "b_site"]},
    {"id": "t_to_b_mid", "from": "t_spawn", "to": "b_site",
     "waypoints": ["t_spawn", "t_stairs", "bottom_mid", "xbox", "catwalk", "cave", "b_site"]},
    {"id": "ct_to_a", "from": "ct_spawn", "to": "a_site",
     "waypoints": ["ct_spawn", "temple", "a_site"]},
    {"id": "ct_to_b", "from": "ct_spawn", "to": "b_site",
     "waypoints": ["ct_spawn", "connector", "house", "b_site"]},
    {"id": "ct_to_mid", "from": "ct_spawn", "to": "mid",
     "waypoints": ["ct_spawn", "connector", "house", "top_mid", "mid"]},
    {"id": "mid_to_a", "from": "mid", "to": "a_site",
     "waypoints": ["mid", "donut", "a_site"]},
    {"id": "mid_to_b", "from": "mid", "to": "b_site",
     "waypoints": ["mid", "catwalk", "cave", "b_site"]},
    {"id": "a_links", "from": "a_main", "to": "temple",
     "waypoints": ["a_main", "a_site", "temple"]},
]

root = {"schemaVersion": 1, "map": {"id": "ancient", "displayName": "Ancient", "zones": zones, "routes": routes}}
OUT_JSON.write_text(json.dumps(root, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print("wrote", OUT_JSON, "zones", len(zones))
