"""Retarget dust2.json zone UVs from callout map via affine (img2 -> img1)."""
from __future__ import annotations

import json
import shutil
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
_IMG_DIR = Path(
    r"C:\Users\approlu\AppData\Roaming\Cursor\User\workspaceStorage"
    r"\9d73533b536d22ebdcc9a9d7ab3cd46f\images"
)
IMG1 = _IMG_DIR / "image-c9ce5595-6ed2-44e8-85d2-15f7e017e0e2.png"
IMG2 = _IMG_DIR / "image-5ebdfb0d-1de4-4239-b662-19c34ebdfbae.png"
OVERVIEW = ROOT / "Assets/_Project/Resources/Maps/dust2_overview.png"
DUST2_JSON = ROOT / "Assets/_Project/Data/JSON/dust2.json"
OUT_DIR = ROOT / "Tools/backups"

# Affine: img2 pixel -> img1 pixel (4 site/spawn box corners, RMSE ~1.4px)
M = np.array(
    [
        [1.785307822202588, -0.00047370648032096785, -58.71331615147471],
        [-0.0011719401334577976, 1.788893420889894, -154.5252749497],
    ],
    dtype=float,
)

# Callout pixel centers on img2 (916x1024), top-left origin.
# Sites/spawns = highlight box / icon centers from detection; others from grid read.
CALLOUT_PX: dict[str, tuple[float, float]] = {
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


def apply_affine(pts: np.ndarray) -> np.ndarray:
    ones = np.ones((pts.shape[0], 1))
    return (M @ np.hstack([pts, ones]).T).T


def main() -> None:
    im1 = Image.open(IMG1).convert("RGBA")
    w1, h1 = im1.size
    print(f"img1 {w1}x{h1}")

    keys = list(CALLOUT_PX.keys())
    mapped = apply_affine(np.array([CALLOUT_PX[k] for k in keys], dtype=float))
    new_uv: dict[str, tuple[float, float]] = {}
    for k, (px, py) in zip(keys, mapped):
        u = float(np.clip(px / w1, 0.0, 1.0))
        v = float(np.clip(1.0 - py / h1, 0.0, 1.0))
        new_uv[k] = (round(u, 3), round(v, 3))
        print(f"{k:18} ({CALLOUT_PX[k][0]:.0f},{CALLOUT_PX[k][1]:.0f}) -> UV ({u:.3f},{v:.3f})")

    OUT_DIR.mkdir(parents=True, exist_ok=True)
    bak = OUT_DIR / "dust2.json.before_align.json"
    if not bak.exists():
        shutil.copy2(DUST2_JSON, bak)

    data = json.loads(DUST2_JSON.read_text(encoding="utf-8"))
    print("\nDelta:")
    for z in data["map"]["zones"]:
        zid = z["id"]
        if zid not in new_uv:
            print(f"  SKIP {zid}")
            continue
        ox, oy = z["x"], z["y"]
        nx, ny = new_uv[zid]
        z["x"], z["y"] = nx, ny
        print(f"  {zid:18} ({ox:.3f},{oy:.3f}) -> ({nx:.3f},{ny:.3f})  d=({nx-ox:+.3f},{ny-oy:+.3f})")

    # Keep formatting readable: indent 2, trailing newline
    DUST2_JSON.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"wrote {DUST2_JSON}")

    # Overview verification
    ov = Image.open(OVERVIEW).convert("RGBA")
    ow, oh = ov.size
    draw = ImageDraw.Draw(ov)
    for z in data["map"]["zones"]:
        px = z["x"] * ow
        py = (1.0 - z["y"]) * oh
        draw.ellipse([px - 5, py - 5, px + 5, py + 5], fill=(0, 255, 100, 255), outline=(0, 0, 0, 255))
        draw.text((px + 7, py - 7), z["id"], fill=(180, 255, 200, 255))
    out_ov = OUT_DIR / "dust2_zones_aligned_overview.png"
    ov.save(out_ov)
    print(f"wrote {out_ov}")

    # img1 + warped callout blend with new points
    A = M[:, :2]
    t = M[:, 2]
    Ainv = np.linalg.inv(A)
    tinv = -Ainv @ t
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
    blend = Image.blend(im1, warped, 0.40)
    draw = ImageDraw.Draw(blend)
    for zid, (u, v) in new_uv.items():
        px = u * w1
        py = (1.0 - v) * h1
        draw.ellipse([px - 6, py - 6, px + 6, py + 6], fill=(255, 40, 40, 255))
        draw.text((px + 8, py - 8), zid, fill=(255, 220, 80, 255))
    out_b = OUT_DIR / "dust2_zones_aligned_blend.png"
    blend.save(out_b)
    print(f"wrote {out_b}")


if __name__ == "__main__":
    main()
