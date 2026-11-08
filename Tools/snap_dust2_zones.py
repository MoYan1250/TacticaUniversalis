# -*- coding: utf-8 -*-
"""Snap zone UVs onto overview floor; refine right-side landmarks on clean overview."""
from __future__ import annotations

import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
OVERVIEW = ROOT / "Assets/_Project/Resources/Maps/dust2_overview.png"
DUST2_JSON = ROOT / "Assets/_Project/Data/JSON/dust2.json"
OUT = ROOT / "Tools/backups"

# Transform from previous realign (orig callout 916x1024 -> overview 958x1024)
SX, SY, TX, TY = 1.12357, 1.05202, -36.12, -54.32
OW, OH = 958, 1024

CALLOUT_PX = {
    "t_spawn": (365, 905),
    "ct_spawn": (527, 298),
    "a_site": (725, 245),
    "b_site": (186, 210),
    "suicide": (455, 790),
    "top_mid": (455, 710),
    "mid": (455, 610),
    "mid_doors": (455, 520),
    "xbox": (425, 490),
    "ct_mid": (448, 390),
    "cat": (545, 480),
    "a_short": (605, 360),
    "a_ramp": (770, 350),
    "a_long": (800, 510),
    "long_doors": (745, 715),
    "pit": (805, 820),
    "outside_long": (620, 835),
    "outside_tunnels": (175, 710),
    "b_tunnels": (185, 480),
    "lower_tunnels": (335, 490),
    "b_doors": (295, 255),
    "scaffolding": (355, 275),
}

# Manual overrides in overview pixel space (top-left origin), after inspecting clean art.
# Prefer center of named floor regions on the schematic itself.
OV_OVERRIDE = {
    # Pit is the green alcove bottom-right of long
    "pit": (860, 780),
    "outside_long": (640, 800),
    "long_doors": (780, 700),
    "a_long": (850, 500),
    "a_ramp": (810, 330),
    "a_site": (780, 220),
    "t_spawn": (380, 900),
    "outside_tunnels": (180, 700),
}


def floor_mask(rgb: np.ndarray, alpha: np.ndarray) -> np.ndarray:
    return (alpha >= 128) & (rgb.mean(axis=2) > 28)


def snap_to_floor(px: float, py: float, mask: np.ndarray, max_r: int = 80) -> tuple[float, float]:
    h, w = mask.shape
    x = int(np.clip(round(px), 0, w - 1))
    y = int(np.clip(round(py), 0, h - 1))
    if mask[y, x]:
        return float(x), float(y)
    best = None
    for r in range(1, max_r + 1):
        y0, y1 = max(0, y - r), min(h, y + r + 1)
        x0, x1 = max(0, x - r), min(w, x + r + 1)
        yy, xx = np.where(mask[y0:y1, x0:x1])
        if len(xx) == 0:
            continue
        xx = xx + x0
        yy = yy + y0
        d2 = (xx - x) ** 2 + (yy - y) ** 2
        i = int(np.argmin(d2))
        best = (float(xx[i]), float(yy[i]))
        break
    return best if best else (float(x), float(y))


def main() -> None:
    ov = np.array(Image.open(OVERVIEW).convert("RGBA"))
    rgb, a = ov[:, :, :3], ov[:, :, 3]
    mask = floor_mask(rgb, a)
    print("floor px", int(mask.sum()))

    new_uv = {}
    for zid, (cx, cy) in CALLOUT_PX.items():
        if zid in OV_OVERRIDE:
            px, py = OV_OVERRIDE[zid]
        else:
            px = SX * cx + TX
            py = SY * cy + TY
        px2, py2 = snap_to_floor(px, py, mask)
        u = round(float(np.clip(px2 / OW, 0, 1)), 3)
        v = round(float(np.clip(1.0 - py2 / OH, 0, 1)), 3)
        new_uv[zid] = (u, v)
        moved = abs(px2 - px) + abs(py2 - py)
        print(f"{zid:18} ({px:.0f},{py:.0f}) -> ({px2:.0f},{py2:.0f}) UV=({u:.3f},{v:.3f}) snap={moved:.0f}")

    data = json.loads(DUST2_JSON.read_text(encoding="utf-8"))
    for z in data["map"]["zones"]:
        if z["id"] in new_uv:
            z["x"], z["y"] = new_uv[z["id"]]
    DUST2_JSON.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    vis = Image.fromarray(ov)
    draw = ImageDraw.Draw(vis)
    for zid, (u, v) in new_uv.items():
        px, py = u * OW, (1 - v) * OH
        draw.ellipse([px - 5, py - 5, px + 5, py + 5], fill=(255, 40, 40, 255), outline=(0, 0, 0, 255))
        draw.text((px + 7, py - 7), zid, fill=(255, 230, 80, 255))
    out = OUT / "dust2_zones_snapped.png"
    vis.save(out)
    print(f"wrote {DUST2_JSON}")
    print(f"wrote {out}")


if __name__ == "__main__":
    main()
