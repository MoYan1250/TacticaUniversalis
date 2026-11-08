# -*- coding: utf-8 -*-
"""Remove solid black backgrounds from kill-feed icon PNGs (UTF-8 safe)."""
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
ICON_DIR = ROOT / "Assets/_Project/Resources/UI/Icons"
FILES = ("kill_feed.png", "kill_skull.png", "kill_feed_body.png")
# Near-black keyed to alpha; soft edge for anti-aliased fringes.
BLACK_CUTOFF = 28
SOFT_RANGE = 36


def key_black_to_alpha(src: Path) -> None:
    im = Image.open(src).convert("RGBA")
    px = im.load()
    w, h = im.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            lum = (r + g + b) / 3.0
            if lum <= BLACK_CUTOFF:
                px[x, y] = (r, g, b, 0)
            elif lum < BLACK_CUTOFF + SOFT_RANGE:
                t = (lum - BLACK_CUTOFF) / SOFT_RANGE
                px[x, y] = (r, g, b, int(round(a * t)))
    im.save(src)
    print("wrote", src)


def main() -> None:
    for name in FILES:
        path = ICON_DIR / name
        if not path.is_file():
            print("skip missing", path)
            continue
        key_black_to_alpha(path)


if __name__ == "__main__":
    main()
