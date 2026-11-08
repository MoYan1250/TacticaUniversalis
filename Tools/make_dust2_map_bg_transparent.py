# -*- coding: utf-8 -*-
"""Make Dust2 overview black letterbox transparent so MapFrame frost shows through."""
from pathlib import Path
import shutil
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "Assets/_Project/Resources/Maps/dust2_overview.png"
BAK = ROOT / "Tools/backups/dust2_overview_pre_frost_border.png"


def main() -> None:
    BAK.parent.mkdir(parents=True, exist_ok=True)
    if not BAK.exists():
        shutil.copy2(SRC, BAK)

    im = Image.open(SRC).convert("RGBA")
    arr = np.array(im)
    rgb = arr[:, :, :3].astype(np.int16)
    lum = rgb.mean(axis=2)
    # Pure black letterbox only; walkable floor is ~60+ luminance.
    mask = lum <= 12
    arr[mask, 3] = 0
    Image.fromarray(arr).save(SRC, optimize=True)
    print(f"transparent pixels: {int(mask.sum())} / {mask.size} -> {SRC}")


if __name__ == "__main__":
    main()
