# -*- coding: utf-8 -*-
"""Final zone placement on clean overview floor geometry (pixel coords, top-left origin)."""
from __future__ import annotations

import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
OVERVIEW = ROOT / "Assets/_Project/Resources/Maps/dust2_overview.png"
DUST2_JSON = ROOT / "Assets/_Project/Data/JSON/dust2.json"
OUT = ROOT / "Tools/backups"

OW, OH = 958, 1024

# Placed on clean schematic floor (after measuring grid + region centroids).
# x right, y down in pixels -> UV y flipped.
PX = {
    "t_spawn": (405, 914),
    "ct_spawn": (564, 265),
    "b_site": (200, 185),
    "a_site": (805, 210),
    "b_doors": (310, 230),
    "scaffolding": (370, 250),
    "ct_mid": (450, 360),
    "a_short": (640, 330),
    "a_ramp": (804, 324),
    "cat": (560, 430),
    "xbox": (440, 470),
    "mid_doors": (470, 500),
    "mid": (470, 580),
    "lower_tunnels": (350, 434),
    "b_tunnels": (190, 470),
    "a_long": (860, 520),
    "top_mid": (435, 670),
    "suicide": (435, 760),
    "outside_tunnels": (190, 700),
    "long_doors": (780, 680),
    "outside_long": (650, 780),
    "pit": (855, 645),
}


def main() -> None:
    ov = np.array(Image.open(OVERVIEW).convert("RGBA"))
    mask = (ov[:, :, 3] >= 128) & (ov[:, :, :3].mean(axis=2) > 28)

    new_uv = {}
    for zid, (px, py) in PX.items():
        if not mask[int(py), int(px)]:
            # snap
            best = None
            for r in range(1, 60):
                y0, y1 = max(0, py - r), min(OH, py + r + 1)
                x0, x1 = max(0, px - r), min(OW, px + r + 1)
                yy, xx = np.where(mask[y0:y1, x0:x1])
                if len(xx):
                    xx, yy = xx + x0, yy + y0
                    i = int(np.argmin((xx - px) ** 2 + (yy - py) ** 2))
                    px, py = int(xx[i]), int(yy[i])
                    best = True
                    break
            print(f"snapped {zid} -> ({px},{py}) found={best}")
        u = round(px / OW, 3)
        v = round(1.0 - py / OH, 3)
        new_uv[zid] = (u, v)
        print(f"{zid:18} ({px},{py}) UV=({u:.3f},{v:.3f})")

    data = json.loads(DUST2_JSON.read_text(encoding="utf-8"))
    for z in data["map"]["zones"]:
        z["x"], z["y"] = new_uv[z["id"]]
    DUST2_JSON.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    vis = Image.fromarray(ov)
    draw = ImageDraw.Draw(vis)
    for zid, (u, v) in new_uv.items():
        px, py = u * OW, (1 - v) * OH
        draw.ellipse([px - 5, py - 5, px + 5, py + 5], fill=(255, 50, 50, 255), outline=(0, 0, 0, 255))
        draw.text((px + 7, py - 7), zid, fill=(255, 230, 80, 255))
    out = OUT / "dust2_zones_final.png"
    vis.save(out)
    print("wrote", DUST2_JSON)
    print("wrote", out)


if __name__ == "__main__":
    main()
