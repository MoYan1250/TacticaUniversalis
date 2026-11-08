# -*- coding: utf-8 -*-
"""Fix map overview .meta sprite names/rects (cloned train_* leftovers)."""
from pathlib import Path
from PIL import Image
import re
import uuid

MAPS = Path(r"E:\TacticaUniversalis\Assets\_Project\Resources\Maps")
TEMPLATE = (MAPS / "dust2_overview.png.meta").read_text(encoding="utf-8")


def fix_one(png: Path):
    im = Image.open(png)
    w, h = im.size
    stem = png.stem
    sprite_name = stem + "_0"
    meta_path = Path(str(png) + ".meta")
    old = meta_path.read_text(encoding="utf-8") if meta_path.exists() else ""
    m = re.search(r"guid: ([a-f0-9]+)", old)
    guid = m.group(1) if m else uuid.uuid4().hex

    text = TEMPLATE
    text = re.sub(r"guid: [a-f0-9]+", "guid: " + guid, text, count=1)
    text = re.sub(r"second: \w+", "second: " + sprite_name, text, count=1)
    text = re.sub(r"name: dust2_overview_\d+", "name: " + sprite_name, text)
    text = re.sub(
        r"(rect:\n\s+serializedVersion: 2\n\s+x: 0\n\s+y: 0\n\s+width: )\d+(\n\s+height: )\d+",
        rf"\g<1>{w}\g<2>{h}",
        text,
        count=1,
    )
    if "nameFileIdTable:" in text:
        text = re.sub(
            r"nameFileIdTable:\n(?:\s+\S+: -?\d+\n)*",
            f"nameFileIdTable:\n      {sprite_name}: 2586116875883752276\n",
            text,
            count=1,
        )
        # dust2 may use different internal id — keep whatever is in second/first
        mid = re.search(r"internalID: (-?\d+)", text)
        if mid and "2586116875883752276" in text:
            # try keep original dust2 sprite internal id from template's sprites section
            pass
        # Better: extract internalID from template sprite
        iid = re.search(r"internalID: (-?\d+)", TEMPLATE.split("sprites:")[1] if "sprites:" in TEMPLATE else TEMPLATE)
        if iid:
            text = re.sub(
                rf"nameFileIdTable:\n\s+{re.escape(sprite_name)}: \d+",
                f"nameFileIdTable:\n      {sprite_name}: {iid.group(1)}",
                text,
                count=1,
            )

    meta_path.write_text(text, encoding="utf-8")
    print(f"fixed {png.name} -> {sprite_name} {w}x{h}")


def main():
    for png in sorted(MAPS.glob("*_overview.png")):
        fix_one(png)


if __name__ == "__main__":
    main()
