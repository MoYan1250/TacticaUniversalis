# -*- coding: utf-8 -*-
"""Manual re-anchor Overpass + Mirage zones onto overview floor."""
from pathlib import Path
from PIL import Image, ImageDraw
import numpy as np
import json

MAPS = Path(r"E:\TacticaUniversalis\Assets\_Project\Resources\Maps")
JSON = Path(r"E:\TacticaUniversalis\Assets\_Project\Data\JSON")
BK = Path(r"E:\TacticaUniversalis\Tools\backups")


def floor_bbox(arr):
    ys, xs = np.where(arr[:, :, 3] > 0)
    return int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())


def snap_to_floor(arr, x, y, max_r=40):
    """If (x,y) is transparent, walk to nearest opaque pixel."""
    h, w = arr.shape[:2]
    x, y = int(np.clip(x, 0, w - 1)), int(np.clip(y, 0, h - 1))
    if arr[y, x, 3] > 0:
        return x, y
    best = None
    best_d = 1e9
    for r in range(1, max_r + 1):
        for dy in range(-r, r + 1):
            for dx in range(-r, r + 1):
                if abs(dx) != r and abs(dy) != r:
                    continue
                nx, ny = x + dx, y + dy
                if 0 <= nx < w and 0 <= ny < h and arr[ny, nx, 3] > 0:
                    d = dx * dx + dy * dy
                    if d < best_d:
                        best_d = d
                        best = (nx, ny)
        if best is not None:
            return best
    # fallback clamp into bbox
    x0, y0, x1, y1 = floor_bbox(arr)
    return int(np.clip(x, x0 + 8, x1 - 8)), int(np.clip(y, y0 + 8, y1 - 8))


def orange_center(arr, prefer="top"):
    r, g, b, a = arr[:, :, 0].astype(int), arr[:, :, 1].astype(int), arr[:, :, 2].astype(int), arr[:, :, 3]
    m = (r > 140) & (g > 50) & (g < 180) & (b < 100) & (r > g + 15) & (a > 180)
    if not m.any():
        return None
    ys, xs = np.where(m)
    if prefer == "top":
        sel = ys <= np.percentile(ys, 45)
    elif prefer == "bottom":
        sel = ys >= np.percentile(ys, 55)
    elif prefer == "right":
        sel = xs >= np.percentile(xs, 55)
    else:
        sel = np.ones_like(ys, dtype=bool)
    if not sel.any():
        sel = np.ones_like(ys, dtype=bool)
    return int(xs[sel].mean()), int(ys[sel].mean())


def apply(name, landmarks):
    arr = np.array(Image.open(MAPS / f"{name}_overview.png").convert("RGBA"))
    path = JSON / f"{name}.json"
    root = json.loads(path.read_text(encoding="utf-8"))
    by = {z["id"]: z for z in root["map"]["zones"]}
    img = Image.fromarray(arr)
    d = ImageDraw.Draw(img)
    for zid, (x, y) in landmarks.items():
        x, y = snap_to_floor(arr, x, y)
        u, v = round(x / 1024, 3), round(1 - y / 1024, 3)
        by[zid]["x"], by[zid]["y"] = u, v
        d.ellipse([x - 5, y - 5, x + 5, y + 5], fill=(255, 40, 40), outline=(255, 255, 0))
        d.text((x + 7, y - 8), zid, fill=(255, 255, 80))
        print(f"{name:8} {zid:14} u={u:.3f} v={v:.3f} px=({x},{y})")
    path.write_text(json.dumps(root, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    img.save(BK / f"{name}_zones_preview.png")


def fix_overpass():
    arr = np.array(Image.open(MAPS / "overpass_overview.png").convert("RGBA"))
    x0, y0, x1, y1 = floor_bbox(arr)
    print("overpass floor", x0, y0, x1, y1)
    a = orange_center(arr, "top") or (465, 200)
    # B: right side of map in teal — search opaque teal-ish
    r, g, b, achan = arr[:, :, 0].astype(int), arr[:, :, 1].astype(int), arr[:, :, 2].astype(int), arr[:, :, 3]
    teal = (b > r + 10) & (g > 60) & (b > 70) & (achan > 0) & (np.arange(1024)[None, :] > 600)
    # fix: x > 600
    xx = np.arange(1024)[None, :]
    yy = np.arange(1024)[:, None]
    teal = (b > r + 5) & (g > 50) & (b > 60) & (achan > 0) & (xx > 620) & (yy > 350) & (yy < 750)
    if teal.any():
        tys, txs = np.where(teal)
        b_site = (int(np.median(txs)), int(np.median(tys)))
    else:
        b_site = (760, 520)
    print("A", a, "B", b_site)

    ax, ay = a
    bx, by = b_site
    landmarks = {
        "a_site": a,
        "b_site": b_site,
        # T bottom brown park; CT just above A bank area
        "t_spawn": (420, 960),
        "ct_spawn": (520, 70),
        "long": (200, 480),
        "toilets": (380, 420),
        "short": (470, 300),
        "mid": (430, 520),
        "fountain": (400, 680),
        "playground": (360, 820),
        "connector": (540, 480),
        "water": (700, 500),
        "monster": (860, 560),
        "canal": (720, 600),
        "b_short": (780, 580),
        "heaven": (620, 280),
        "bank": (400, 120),
        "truck": (540, 200),
        "squeaky": (580, 440),
        "upper_tunnels": (680, 780),
        "ramp": (480, 720),
    }
    apply("overpass", landmarks)


def fix_mirage():
    arr = np.array(Image.open(MAPS / "mirage_overview.png").convert("RGBA"))
    print("mirage floor", floor_bbox(arr))
    b = orange_center(arr, "top") or (170, 250)
    a = orange_center(arr, "bottom") or (540, 850)
    print("A", a, "B", b)
    ax, ay = a
    bx, by = b

    # Overview: T left (green mid-left), CT right. Use floor-relative.
    landmarks = {
        "b_site": b,
        "a_site": a,
        "t_spawn": (230, 760),
        "ct_spawn": (960, 380),
        "mid": (500, 480),
        "top_mid": (380, 560),
        "underpass": (460, 560),
        "connector": (600, 620),
        "catwalk": (400, 360),
        "palace": (700, 900),
        "a_ramp": (560, 860),
        "jungle": (660, 700),
        "ticket": (720, 740),
        "b_apps": (150, 360),
        "market": (300, 340),
        "window": (620, 420),
        "balcony": (660, 860),
        "sandwich": (560, 800),
        "van": (200, 200),
        "kitchen": (200, 300),
        "b_short": (320, 300),
    }
    apply("mirage", landmarks)


if __name__ == "__main__":
    fix_overpass()
    print()
    fix_mirage()
