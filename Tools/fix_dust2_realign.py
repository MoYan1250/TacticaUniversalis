# -*- coding: utf-8 -*-
"""
Fix Dust2 map alignment:
1) Rebuild overview from source RGBA: opaque floor only (drop ghost AA / bad overlays).
2) Strip baked orange/green guide boxes (they were naively stamped from callout).
3) Register callout -> overview with anisotropic scale + translation via floor-mask ECC.
4) Retarget zone UVs from callout landmarks through that transform.
"""
from __future__ import annotations

import json
import shutil
from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
SRC = Path(
    r"C:\Users\approlu\AppData\Roaming\Cursor\User\workspaceStorage"
    r"\9d73533b536d22ebdcc9a9d7ab3cd46f\images"
    r"\image-1d64a656-2de0-4de6-8c7d-6d423dd8964f.png"
)
CALLOUT = Path(
    r"C:\Users\approlu\AppData\Roaming\Cursor\User\workspaceStorage"
    r"\9d73533b536d22ebdcc9a9d7ab3cd46f\images"
    r"\image-5143ac6a-6b71-47f4-9299-f526bf7fa49d.png"
)
OVERVIEW = ROOT / "Assets/_Project/Resources/Maps/dust2_overview.png"
DUST2_JSON = ROOT / "Assets/_Project/Data/JSON/dust2.json"
OUT = ROOT / "Tools/backups"

# Target overview size (keep current in-game resolution)
OW, OH = 958, 1024


def orange_mask(rgb: np.ndarray) -> np.ndarray:
    r, g, b = rgb[:, :, 0].astype(int), rgb[:, :, 1].astype(int), rgb[:, :, 2].astype(int)
    return (r > 150) & (g > 50) & (g < 210) & (b < 110) & (r > g + 25) & (r > b + 50)


def green_mask(rgb: np.ndarray) -> np.ndarray:
    r, g, b = rgb[:, :, 0].astype(int), rgb[:, :, 1].astype(int), rgb[:, :, 2].astype(int)
    return (g > 130) & (r < 150) & (b < 150) & (g > r + 25) & (g > b + 25)


def floor_mask(rgb: np.ndarray, alpha: np.ndarray | None = None) -> np.ndarray:
    lum = rgb.mean(axis=2)
    m = lum > 28
    if alpha is not None:
        m &= alpha >= 250
    # exclude guide strokes
    m &= ~orange_mask(rgb)
    # green floor is OK; only strip neon guide outlines (thin + very green)
    neon = (rgb[:, :, 1].astype(int) > 200) & (rgb[:, :, 0].astype(int) < 120) & (
        rgb[:, :, 2].astype(int) < 120
    )
    m &= ~neon
    return m


def rebuild_overview() -> tuple[np.ndarray, np.ndarray]:
    """Return clean RGBA overview (OW x OH) and its floor mask."""
    src = np.array(Image.open(SRC).convert("RGBA"))
    # Keep fully opaque pixels only — drops misaligned semi-transparent ghost edges.
    keep = src[:, :, 3] >= 250
    clean = np.zeros_like(src)
    clean[keep] = src[keep]
    clean[:, :, 3] = np.where(keep, 255, 0).astype(np.uint8)

    rgb = clean[:, :, :3]
    # Remove orange/green guide boxes by inpainting from nearby floor (simple dilate fill).
    kill = orange_mask(rgb) | (
        (rgb[:, :, 1].astype(int) > 180)
        & (rgb[:, :, 0].astype(int) < 100)
        & (rgb[:, :, 2].astype(int) < 120)
        & (clean[:, :, 3] > 0)
    )
    # Also kill striped CT fill (yellow-green diagonals) — restore underlying by neighbor avg later
    stripe = (
        (rgb[:, :, 1].astype(int) > 140)
        & (rgb[:, :, 0].astype(int) > 100)
        & (rgb[:, :, 2].astype(int) < 80)
        & (clean[:, :, 3] > 0)
    )
    kill |= stripe

    # For killed pixels that were on floor, replace with local median of non-kill floor
    ys, xs = np.where(kill)
    for y, x in zip(ys, xs):
        y0, y1 = max(0, y - 4), min(src.shape[0], y + 5)
        x0, x1 = max(0, x - 4), min(src.shape[1], x + 5)
        patch = clean[y0:y1, x0:x1]
        prgb = patch[:, :, :3]
        pa = patch[:, :, 3]
        pk = kill[y0:y1, x0:x1]
        valid = (pa >= 250) & (~pk) & (prgb.mean(axis=2) > 28)
        if valid.any():
            clean[y, x, :3] = np.median(prgb[valid], axis=0).astype(np.uint8)
            clean[y, x, 3] = 255
        else:
            clean[y, x] = (0, 0, 0, 0)

    # Letterbox: anything near-black -> transparent
    lum = clean[:, :, :3].astype(np.int16).mean(axis=2)
    clean[lum <= 12, 3] = 0

    im = Image.fromarray(clean).resize((OW, OH), Image.Resampling.LANCZOS)
    arr = np.array(im)
    # Re-threshold alpha after resize
    arr[arr[:, :, 3] < 128, 3] = 0
    arr[arr[:, :, 3] >= 128, 3] = 255
    mask = floor_mask(arr[:, :, :3], arr[:, :, 3])
    return arr, mask


def callout_floor(w: int, h: int) -> tuple[np.ndarray, np.ndarray]:
    im = np.array(Image.open(CALLOUT).convert("RGB").resize((w, h), Image.Resampling.LANCZOS))
    # Callout has title bar — mask black + near-black
    m = floor_mask(im)
    return im, m


def score_overlap(dst: np.ndarray, src: np.ndarray, sx: float, sy: float, tx: float, ty: float) -> float:
    """Overlap of warped src floor onto dst floor. src/dst bool masks same size."""
    h, w = dst.shape
    ys, xs = np.where(src)
    if len(xs) == 0:
        return -1e9
    # subsample for speed
    step = max(1, len(xs) // 8000)
    xs = xs[::step].astype(np.float64)
    ys = ys[::step].astype(np.float64)
    xd = sx * xs + tx
    yd = sy * ys + ty
    xi = np.rint(xd).astype(np.int32)
    yi = np.rint(yd).astype(np.int32)
    ok = (xi >= 0) & (xi < w) & (yi >= 0) & (yi < h)
    if not ok.any():
        return -1e9
    hit = dst[yi[ok], xi[ok]].mean()
    # prefer transforms that cover similar area
    return float(hit)


def register_masks(dst: np.ndarray, src: np.ndarray) -> tuple[float, float, float, float]:
    """Find sx,sy,tx,ty maximizing floor overlap. src (callout) -> dst (overview)."""
    # Initial guess from bbox centers/sizes
    def bbox(m):
        ys, xs = np.where(m)
        return xs.min(), ys.min(), xs.max(), ys.max()

    x0s, y0s, x1s, y1s = bbox(src)
    x0d, y0d, x1d, y1d = bbox(dst)
    sx0 = (x1d - x0d) / max(1, (x1s - x0s))
    sy0 = (y1d - y0d) / max(1, (y1s - y0s))
    tx0 = x0d - sx0 * x0s
    ty0 = y0d - sy0 * y0s
    print(f"bbox init sx={sx0:.4f} sy={sy0:.4f} tx={tx0:.1f} ty={ty0:.1f}")

    best = (-1e9, sx0, sy0, tx0, ty0)
    # Coarse search
    for dsx in np.linspace(-0.08, 0.08, 9):
        for dsy in np.linspace(-0.08, 0.08, 9):
            sx, sy = sx0 + dsx, sy0 + dsy
            for dtx in np.linspace(-40, 40, 11):
                for dty in np.linspace(-40, 40, 11):
                    sc = score_overlap(dst, src, sx, sy, tx0 + dtx, ty0 + dty)
                    if sc > best[0]:
                        best = (sc, sx, sy, tx0 + dtx, ty0 + dty)
    print(f"coarse best score={best[0]:.4f} sx={best[1]:.4f} sy={best[2]:.4f} tx={best[3]:.1f} ty={best[4]:.1f}")

    # Fine search around best
    _, sx1, sy1, tx1, ty1 = best
    for dsx in np.linspace(-0.02, 0.02, 9):
        for dsy in np.linspace(-0.02, 0.02, 9):
            sx, sy = sx1 + dsx, sy1 + dsy
            for dtx in np.linspace(-12, 12, 13):
                for dty in np.linspace(-12, 12, 13):
                    sc = score_overlap(dst, src, sx, sy, tx1 + dtx, ty1 + dty)
                    if sc > best[0]:
                        best = (sc, sx, sy, tx1 + dtx, ty1 + dty)
    print(f"fine best score={best[0]:.4f} sx={best[1]:.4f} sy={best[2]:.4f} tx={best[3]:.1f} ty={best[4]:.1f}")
    return best[1], best[2], best[3], best[4]


# Callout pixel positions on ORIGINAL 916x1024 callout (not resized)
CALLOUT_PX_ORIG: dict[str, tuple[float, float]] = {
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


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    bak = OUT / "dust2_overview_before_realign.png"
    if not bak.exists():
        shutil.copy2(OVERVIEW, bak)

    overview, dst_mask = rebuild_overview()
    Image.fromarray(overview).save(OUT / "dust2_overview_clean_preview.png")
    print("clean overview built", overview.shape)

    # Register callout resized to overview size
    call_rgb, src_mask = callout_floor(OW, OH)
    Image.fromarray((src_mask.astype(np.uint8) * 255)).save(OUT / "mask_callout.png")
    Image.fromarray((dst_mask.astype(np.uint8) * 255)).save(OUT / "mask_overview.png")

    sx, sy, tx, ty = register_masks(dst_mask, src_mask)

    # Scale factors from original callout (916x1024) -> registered overview space:
    # first resize callout to OW,OH then apply sx,sy,tx,ty
    # px_ov = sx * (px_c * OW/916) + tx
    rx, ry = OW / 916.0, OH / 1024.0
    sx_o, sy_o = sx * rx, sy * ry
    tx_o, ty_o = tx, ty
    print(f"orig-callout->overview: sx={sx_o:.5f} sy={sy_o:.5f} tx={tx_o:.2f} ty={ty_o:.2f}")

    # Visual: warp callout onto overview
    # Inverse map for each overview pixel
    yy, xx = np.mgrid[0:OH, 0:OW]
    # xx = sx * x_src + tx => x_src = (xx - tx) / sx
    xs = (xx - tx) / sx
    ys = (yy - ty) / sy
    # src is callout resized to OW,OH
    xsi = np.clip(np.rint(xs), 0, OW - 1).astype(np.int32)
    ysi = np.clip(np.rint(ys), 0, OH - 1).astype(np.int32)
    warped = call_rgb[ysi, xsi]
    base = overview[:, :, :3].copy()
    # only show warped where overview has floor
    blend = base.copy()
    alpha = 0.45
    m = dst_mask
    blend[m] = (base[m] * (1 - alpha) + warped[m] * alpha).astype(np.uint8)
    Image.fromarray(blend).save(OUT / "dust2_realign_blend.png")

    # Zone UVs
    new_uv = {}
    for zid, (cx, cy) in CALLOUT_PX_ORIG.items():
        px = sx_o * cx + tx_o
        py = sy_o * cy + ty_o
        u = float(np.clip(px / OW, 0, 1))
        v = float(np.clip(1.0 - py / OH, 0, 1))
        new_uv[zid] = (round(u, 3), round(v, 3))
        print(f"{zid:18} -> UV ({u:.3f},{v:.3f})")

    # Draw zones on blend
    draw = ImageDraw.Draw(Image.fromarray(blend))
    vis = Image.fromarray(blend)
    draw = ImageDraw.Draw(vis)
    for zid, (u, v) in new_uv.items():
        px, py = u * OW, (1 - v) * OH
        draw.ellipse([px - 4, py - 4, px + 4, py + 4], fill=(255, 40, 40))
        draw.text((px + 6, py - 6), zid, fill=(255, 220, 80))
    vis.save(OUT / "dust2_realign_zones.png")

    # Write overview asset
    Image.fromarray(overview).save(OVERVIEW, optimize=True)
    print(f"wrote {OVERVIEW}")

    # Update JSON
    jbak = OUT / "dust2.json.before_realign.json"
    shutil.copy2(DUST2_JSON, jbak)
    data = json.loads(DUST2_JSON.read_text(encoding="utf-8"))
    for z in data["map"]["zones"]:
        if z["id"] in new_uv:
            z["x"], z["y"] = new_uv[z["id"]]
    DUST2_JSON.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"wrote {DUST2_JSON}")


if __name__ == "__main__":
    main()
