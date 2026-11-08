"""Align callout map (img2) landmarks onto game overview (img1) and retarget zone UVs."""
from __future__ import annotations

import json
from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
# Prefer Cursor workspaceStorage copies; fall back to flat names under projects/assets.
_IMG_DIR = Path(
    r"C:\Users\approlu\AppData\Roaming\Cursor\User\workspaceStorage"
    r"\9d73533b536d22ebdcc9a9d7ab3cd46f\images"
)
_ASSETS = Path(r"C:\Users\approlu\.cursor\projects\e-TacticaUniversalis\assets")
IMG1 = _IMG_DIR / "image-c9ce5595-6ed2-44e8-85d2-15f7e017e0e2.png"
IMG2 = _IMG_DIR / "image-5ebdfb0d-1de4-4239-b662-19c34ebdfbae.png"
if not IMG1.exists():
    IMG1 = next(_ASSETS.glob("*image-c9ce5595*.png"))
if not IMG2.exists():
    IMG2 = next(_ASSETS.glob("*image-5ebdfb0d*.png"))
OVERVIEW = ROOT / "Assets/_Project/Resources/Maps/dust2_overview.png"
DUST2_JSON = ROOT / "Assets/_Project/Data/JSON/dust2.json"
OUT_DIR = ROOT / "Tools/backups"


def orange_mask(im: np.ndarray) -> np.ndarray:
    r, g, b = im[:, :, 0].astype(int), im[:, :, 1].astype(int), im[:, :, 2].astype(int)
    return (r > 160) & (g > 60) & (g < 200) & (b < 100) & (r > g + 30) & (r > b + 60)


def green_mask(im: np.ndarray) -> np.ndarray:
    r, g, b = im[:, :, 0].astype(int), im[:, :, 1].astype(int), im[:, :, 2].astype(int)
    return (g > 140) & (r < 140) & (b < 140) & (g > r + 30) & (g > b + 30)


def find_boxes(im: np.ndarray, mask_fn, min_area: int = 50) -> list[dict]:
    h, w = im.shape[:2]
    mask = mask_fn(im)
    visited = np.zeros((h, w), dtype=bool)
    boxes: list[dict] = []
    ys, xs = np.where(mask)
    for y, x in zip(ys, xs):
        if visited[y, x]:
            continue
        q = deque([(y, x)])
        visited[y, x] = True
        pts: list[tuple[int, int]] = []
        while q:
            cy, cx = q.popleft()
            pts.append((cx, cy))
            for ny, nx in ((cy - 1, cx), (cy + 1, cx), (cy, cx - 1), (cy, cx + 1)):
                if 0 <= ny < h and 0 <= nx < w and not visited[ny, nx] and mask[ny, nx]:
                    visited[ny, nx] = True
                    q.append((ny, nx))
        if len(pts) < min_area:
            continue
        xs2 = [p[0] for p in pts]
        ys2 = [p[1] for p in pts]
        x0, x1 = min(xs2), max(xs2)
        y0, y1 = min(ys2), max(ys2)
        bw, bh = x1 - x0 + 1, y1 - y0 + 1
        if bw < 20 or bh < 20:
            continue
        boxes.append(
            dict(
                x0=x0,
                y0=y0,
                x1=x1,
                y1=y1,
                cx=(x0 + x1) / 2,
                cy=(y0 + y1) / 2,
                n=len(pts),
                w=bw,
                h=bh,
            )
        )
    boxes.sort(key=lambda b: -b["n"])
    kept: list[dict] = []
    for b in boxes:
        ok = True
        for k in kept:
            if abs(b["cx"] - k["cx"]) < min(b["w"], k["w"]) * 0.5 and abs(b["cy"] - k["cy"]) < min(
                b["h"], k["h"]
            ) * 0.5:
                ok = False
                break
        if ok:
            kept.append(b)
    return kept


def label_landmarks(boxes_orange: list[dict], boxes_green: list[dict], w: int, h: int) -> dict[str, dict]:
    """Name the 4 highlight boxes by map topology (T bottom, CT mid-top, B left, A right)."""
    sites = sorted(boxes_orange, key=lambda b: b["cx"])
    if len(sites) < 2:
        raise RuntimeError(f"need 2 orange site boxes, got {len(sites)}")
    b_site, a_site = sites[0], sites[-1]

    greens = sorted(boxes_green, key=lambda b: b["cy"], reverse=True)
    if len(greens) < 2:
        raise RuntimeError(f"need 2 green spawn boxes, got {len(greens)}")
    # Bottom-most = T spawn; among remaining, pick the one nearer top-center (CT)
    t_spawn = greens[0]
    rest = greens[1:]
    ct_spawn = min(rest, key=lambda b: abs(b["cx"] - w * 0.55) + abs(b["cy"] - h * 0.28))
    return {"b_site": b_site, "a_site": a_site, "t_spawn": t_spawn, "ct_spawn": ct_spawn}


def fit_similarity(src: np.ndarray, dst: np.ndarray) -> tuple[float, float, float]:
    """Fit dst ≈ s * src + t  (uniform scale + translation). src/dst shape (N,2)."""
    src_c = src.mean(axis=0)
    dst_c = dst.mean(axis=0)
    src_d = src - src_c
    dst_d = dst - dst_c
    scale = np.sqrt((dst_d**2).sum() / max((src_d**2).sum(), 1e-12))
    tx, ty = dst_c - scale * src_c
    return float(scale), float(tx), float(ty)


def fit_affine(src: np.ndarray, dst: np.ndarray) -> np.ndarray:
    """Fit dst ≈ A @ [x,y,1]. Returns 2x3 matrix."""
    n = src.shape[0]
    X = np.column_stack([src[:, 0], src[:, 1], np.ones(n)])
    # least squares for x' and y'
    ax, _, _, _ = np.linalg.lstsq(X, dst[:, 0], rcond=None)
    ay, _, _, _ = np.linalg.lstsq(X, dst[:, 1], rcond=None)
    return np.vstack([ax, ay])


def apply_affine(M: np.ndarray, pts: np.ndarray) -> np.ndarray:
    ones = np.ones((pts.shape[0], 1))
    return (M @ np.hstack([pts, ones]).T).T


def pixel_to_uv(x: float, y: float, w: int, h: int) -> tuple[float, float]:
    # Game UV: x left→right, y bottom→top (matches dust2.json usage with (y-0.5)*height)
    return x / w, 1.0 - y / h


def main() -> None:
    im1 = np.array(Image.open(IMG1).convert("RGB"))
    im2 = np.array(Image.open(IMG2).convert("RGB"))
    h1, w1 = im1.shape[:2]
    h2, w2 = im2.shape[:2]
    print(f"img1 {w1}x{h1}  img2 {w2}x{h2}")

    lm1 = label_landmarks(find_boxes(im1, orange_mask), find_boxes(im1, green_mask), w1, h1)
    lm2 = label_landmarks(find_boxes(im2, orange_mask), find_boxes(im2, green_mask), w2, h2)

    names = ["t_spawn", "ct_spawn", "a_site", "b_site"]
    for n in names:
        b1, b2 = lm1[n], lm2[n]
        print(
            f"{n:10} img1=({b1['cx']:.1f},{b1['cy']:.1f}) {b1['w']}x{b1['h']}  "
            f"img2=({b2['cx']:.1f},{b2['cy']:.1f}) {b2['w']}x{b2['h']}"
        )

    # Use box corners as anchors for a more stable fit (4 boxes × 4 corners)
    src_pts = []
    dst_pts = []
    for n in names:
        b2, b1 = lm2[n], lm1[n]
        for corner in (
            (b2["x0"], b2["y0"]),
            (b2["x1"], b2["y0"]),
            (b2["x0"], b2["y1"]),
            (b2["x1"], b2["y1"]),
        ):
            # corresponding corner on img1 by relative position inside each box
            pass
        # Map each corner of img2 box → same relative corner of img1 box
        for (rx, ry) in ((0, 0), (1, 0), (0, 1), (1, 1)):
            sx = b2["x0"] + rx * (b2["x1"] - b2["x0"])
            sy = b2["y0"] + ry * (b2["y1"] - b2["y0"])
            dx = b1["x0"] + rx * (b1["x1"] - b1["x0"])
            dy = b1["y0"] + ry * (b1["y1"] - b1["y0"])
            src_pts.append((sx, sy))
            dst_pts.append((dx, dy))

    src = np.array(src_pts, dtype=float)
    dst = np.array(dst_pts, dtype=float)
    scale, tx, ty = fit_similarity(src, dst)
    M = fit_affine(src, dst)
    print(f"similarity: scale={scale:.6f} tx={tx:.2f} ty={ty:.2f}")
    print(f"affine:\n{M}")

    pred_s = scale * src + np.array([tx, ty])
    err_s = np.linalg.norm(pred_s - dst, axis=1)
    pred_a = apply_affine(M, src)
    err_a = np.linalg.norm(pred_a - dst, axis=1)
    print(f"sim RMSE={np.sqrt((err_s**2).mean()):.2f}px  max={err_s.max():.2f}")
    print(f"aff RMSE={np.sqrt((err_a**2).mean()):.2f}px  max={err_a.max():.2f}")

    # Naive overlay (stretch img2 to img1 size) vs proper affine warp for visual check
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    overview = Image.open(OVERVIEW).convert("RGBA")
    if overview.size != (w1, h1):
        print(f"WARN overview size {overview.size} != img1 {(w1,h1)}")

    # Warp img2 onto img1 with affine (PIL inverse map)
    # x1 = a*x2 + b*y2 + c ; y1 = d*x2 + e*y2 + f
    # We need mapping from output (img1) → input (img2): invert affine
    A = np.array([[M[0, 0], M[0, 1]], [M[1, 0], M[1, 1]]], dtype=float)
    t = np.array([M[0, 2], M[1, 2]], dtype=float)
    Ainv = np.linalg.inv(A)
    tinv = -Ainv @ t
    # PIL Affinity: coeffs = (a,b,c,d,e,f) for x' = ax+by+c
    coeffs = (
        float(Ainv[0, 0]),
        float(Ainv[0, 1]),
        float(tinv[0]),
        float(Ainv[1, 0]),
        float(Ainv[1, 1]),
        float(tinv[1]),
    )
    warped = Image.open(IMG2).convert("RGBA").transform(
        (w1, h1), Image.AFFINE, coeffs, resample=Image.Resampling.BILINEAR
    )
    base = Image.open(IMG1).convert("RGBA")
    blend = Image.blend(base, warped, 0.45)
    draw = ImageDraw.Draw(blend)
    for n in names:
        b = lm1[n]
        draw.rectangle([b["x0"], b["y0"], b["x1"], b["y1"]], outline=(255, 255, 0, 255), width=2)
        draw.text((b["cx"] - 20, b["cy"] - 8), n, fill=(255, 255, 0, 255))

    # Also plot current dust2.json zones
    data = json.loads(DUST2_JSON.read_text(encoding="utf-8"))
    for z in data["map"]["zones"]:
        px = z["x"] * w1
        py = (1.0 - z["y"]) * h1
        draw.ellipse([px - 4, py - 4, px + 4, py + 4], fill=(0, 200, 255, 255))
        draw.text((px + 6, py - 6), z["id"], fill=(0, 200, 255, 255))

    out_blend = OUT_DIR / "dust2_align_blend.png"
    blend.save(out_blend)
    print(f"wrote {out_blend}")

    # Naive size-only stretch for comparison
    naive = Image.open(IMG2).convert("RGBA").resize((w1, h1), Image.Resampling.BILINEAR)
    naive_blend = Image.blend(base, naive, 0.45)
    out_naive = OUT_DIR / "dust2_align_naive_stretch.png"
    naive_blend.save(out_naive)
    print(f"wrote {out_naive}")

    # Export transform for zone retargeting: callout-normalized UV → game UV
    # If a point is measured as UV on img2 (x right, y up), convert:
    #   px2 = u * w2, py2 = (1-v) * h2
    #   [px1,py1] = affine(px2,py2)
    #   u1 = px1/w1, v1 = 1 - py1/h1
    print("\n# Transform callout UV (img2) → game UV (img1):")
    print(f"# affine_img2_to_img1 = {M.tolist()}")
    print(f"# size_img1=({w1},{h1}) size_img2=({w2},{h2})")

    # Verify landmark centers transform
    for n in names:
        b2 = lm2[n]
        p = apply_affine(M, np.array([[b2["cx"], b2["cy"]]]))[0]
        u, v = pixel_to_uv(p[0], p[1], w1, h1)
        u1, v1 = pixel_to_uv(lm1[n]["cx"], lm1[n]["cy"], w1, h1)
        print(f"  {n}: transformed UV=({u:.3f},{v:.3f})  target UV=({u1:.3f},{v1:.3f})")


if __name__ == "__main__":
    main()
