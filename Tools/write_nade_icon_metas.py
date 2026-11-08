# -*- coding: utf-8 -*-
from pathlib import Path
import uuid

out = Path("Assets/_Project/Resources/UI/Icons/Weapons")
template = (out / "defuse_kit.png.meta").read_text(encoding="utf-8")
names = ["flashbang", "he_grenade", "incendiary", "molotov", "smoke_grenade"]
for name in names:
    meta = out / f"{name}.png.meta"
    if meta.exists():
        print("meta exists", name)
        continue
    g = uuid.uuid4().hex
    # keep 32 hex like unity guids
    text = template
    # replace both guid occurrences
    old_guid = "6dd1c4cbfbd9cff27523117f34810426"
    text = text.replace(old_guid, g)
    meta.write_text(text, encoding="utf-8")
    print("wrote meta", name, g)
