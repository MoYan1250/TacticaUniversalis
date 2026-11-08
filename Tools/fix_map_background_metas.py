# -*- coding: utf-8 -*-
"""Fix map background .meta: wrong train_bg_* names + oversized sprite rects -> black UI."""
from pathlib import Path
from PIL import Image
import re
import uuid

BGS = Path(r"E:\TacticaUniversalis\Assets\_Project\Resources\UI\Backgrounds")
TEMPLATE = (BGS / "dust2_bg_1.png.meta").read_text(encoding="utf-8")  # 1024x640, clean dust2 naming


def fix_one(png: Path):
    im = Image.open(png)
    w, h = im.size
    stem = png.stem  # e.g. mirage_bg_0
    sprite_name = stem + "_0"
    meta_path = Path(str(png) + ".meta")
    old = meta_path.read_text(encoding="utf-8") if meta_path.exists() else ""
    m = re.search(r"guid: ([a-f0-9]+)", old)
    guid = m.group(1) if m else uuid.uuid4().hex

    # Prefer dust2_bg_1 meta as base (correct Multiple sprite sheet shape), then rewrite.
    text = TEMPLATE
    text = re.sub(r"guid: [a-f0-9]+", "guid: " + guid, text, count=1)
    text = re.sub(r"second: \w+", "second: " + sprite_name, text, count=1)
    text = re.sub(r"name: dust2_bg_\d+_\d+", "name: " + sprite_name, text, count=1)
    # rect size
    text = re.sub(
        r"(rect:\n\s+serializedVersion: 2\n\s+x: 0\n\s+y: 0\n\s+width: )\d+(\n\s+height: )\d+",
        rf"\g<1>{w}\g<2>{h}",
        text,
        count=1,
    )
    # nameFileIdTable
    if "nameFileIdTable:" in text:
        text = re.sub(
            r"nameFileIdTable:\n(?:\s+\w+: \d+\n)*",
            f"nameFileIdTable:\n      {sprite_name}: 2586116875883752276\n",
            text,
            count=1,
        )
    else:
        text = text.replace(
            "nameFileIdTable: {}",
            f"nameFileIdTable:\n      {sprite_name}: 2586116875883752276",
        )

    meta_path.write_text(text, encoding="utf-8")
    print(f"fixed {png.name} -> {sprite_name} {w}x{h} guid={guid[:8]}…")


def main():
    for png in sorted(BGS.glob("*_bg_*.png")):
        # Always rewrite so train/ancient/etc. also get correct rects
        fix_one(png)


if __name__ == "__main__":
    main()
