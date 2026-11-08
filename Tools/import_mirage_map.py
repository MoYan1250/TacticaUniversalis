# -*- coding: utf-8 -*-
"""Import Mirage assets, place zones, write mirage.json."""
from pathlib import Path
from PIL import Image, ImageDraw
import numpy as np
import json
import uuid
import re

SRC = Path(r"C:\Users\approlu\.cursor\projects\e-TacticaUniversalis\assets")
MAPS = Path(r"E:\TacticaUniversalis\Assets\_Project\Resources\Maps")
BGS = Path(r"E:\TacticaUniversalis\Assets\_Project\Resources\UI\Backgrounds")
JSON = Path(r"E:\TacticaUniversalis\Assets\_Project\Data\JSON")
BK = Path(r"E:\TacticaUniversalis\Tools\backups")
BK.mkdir(exist_ok=True)

OV_SRC = SRC / "c__Users_approlu_AppData_Roaming_Cursor_User_workspaceStorage_9d73533b536d22ebdcc9a9d7ab3cd46f_images_image-4952f7a3-ff1e-4ec8-ace3-fd338593c417.png"
CALL_SRC = SRC / "c__Users_approlu_AppData_Roaming_Cursor_User_workspaceStorage_9d73533b536d22ebdcc9a9d7ab3cd46f_images_image-790cfe80-1c88-4793-b0b4-ce500375edb5.png"
BG_SRCS = [
    SRC / "c__Users_approlu_AppData_Roaming_Cursor_User_workspaceStorage_9d73533b536d22ebdcc9a9d7ab3cd46f_images_image-a2451766-7b06-4873-a133-ed472073d4a8.png",
    SRC / "c__Users_approlu_AppData_Roaming_Cursor_User_workspaceStorage_9d73533b536d22ebdcc9a9d7ab3cd46f_images_image-6f7aaa35-cd83-4cef-b5fb-7f7a6ed5c15f.png",
    SRC / "c__Users_approlu_AppData_Roaming_Cursor_User_workspaceStorage_9d73533b536d22ebdcc9a9d7ab3cd46f_images_image-cbafb320-b898-42a4-8fab-4f20d1aa02db.png",
    SRC / "c__Users_approlu_AppData_Roaming_Cursor_User_workspaceStorage_9d73533b536d22ebdcc9a9d7ab3cd46f_images_image-e240cd44-d7c2-481f-9a6a-009c75471008.png",
]


def clone_meta(src_meta, dest_meta, sprite_name):
    t = Path(src_meta).read_text(encoding="utf-8")
    guid = uuid.uuid4().hex
    t = re.sub(r"guid: [a-f0-9]+", "guid: " + guid, t, count=1)
    t = re.sub(r"second: \w+", "second: " + sprite_name, t, count=1)
    Path(dest_meta).write_text(t, encoding="utf-8")
    print("meta", dest_meta.name, guid)


def make_overview():
    ov = Image.open(OV_SRC).convert("RGBA")
    w, h = ov.size
    side = max(w, h)
    canvas = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    canvas.paste(ov, ((side - w) // 2, (side - h) // 2))
    arr = np.array(canvas)
    mask = (arr[:, :, 0] < 18) & (arr[:, :, 1] < 18) & (arr[:, :, 2] < 18)
    arr[mask, 3] = 0
    out = Image.fromarray(arr).resize((1024, 1024), Image.Resampling.LANCZOS)
    arr2 = np.array(out)
    mask2 = (arr2[:, :, 0] < 12) & (arr2[:, :, 1] < 12) & (arr2[:, :, 2] < 12)
    arr2[mask2, 3] = 0
    out = Image.fromarray(arr2)
    out.save(MAPS / "mirage_overview.png")
    print("overview", out.size, "opaque", int((arr2[:, :, 3] > 0).sum()))
    ys, xs = np.where(arr2[:, :, 3] > 0)
    print("floor bbox", int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max()))
    r, g, b, a = arr2[:, :, 0].astype(int), arr2[:, :, 1].astype(int), arr2[:, :, 2].astype(int), arr2[:, :, 3]
    orange = (r > 140) & (g > 60) & (g < 180) & (b < 90) & (r > g + 15) & (a > 0)
    print("orange px", int(orange.sum()))
    if orange.any():
        oys, oxs = np.where(orange)
        for name, m in [
            ("TL", (oxs < 512) & (oys < 512)),
            ("TR", (oxs >= 512) & (oys < 512)),
            ("BL", (oxs < 512) & (oys >= 512)),
            ("BR", (oxs >= 512) & (oys >= 512)),
        ]:
            if m.any():
                print(name, round(oxs[m].mean() / 1024, 3), round(1 - oys[m].mean() / 1024, 3), int(m.sum()))
    Image.open(CALL_SRC).convert("RGB").save(BK / "mirage_callout_ref.png")
    return arr2


def make_bgs():
    for i, p in enumerate(BG_SRCS):
        im = Image.open(p).convert("RGB")
        dest = BGS / f"mirage_bg_{i}.png"
        im.save(dest, quality=95)
        print("bg", i, dest.name, im.size)
        clone_meta(BGS / f"train_bg_{i}.png.meta", BGS / f"mirage_bg_{i}.png.meta", f"mirage_bg_{i}_0")


def place_zones(arr2):
    # Overview orientation: T bottom-left, CT mid-right, B top-left, A bottom-center/right.
    landmarks = {
        "t_spawn": (180, 780),
        "ct_spawn": (780, 480),
        "b_site": (280, 220),
        "a_site": (620, 780),
        "mid": (480, 480),
        "top_mid": (420, 560),
        "underpass": (500, 560),
        "connector": (600, 620),
        "catwalk": (400, 360),
        "palace": (700, 860),
        "a_ramp": (560, 820),
        "jungle": (680, 680),
        "ticket": (720, 720),
        "b_apps": (160, 320),
        "market": (320, 320),
        "window": (620, 420),
        "balcony": (660, 820),
        "sandwich": (580, 760),
        "van": (220, 180),
        "kitchen": (200, 280),
        "b_short": (340, 280),
    }

    r, g, b, a = arr2[:, :, 0].astype(int), arr2[:, :, 1].astype(int), arr2[:, :, 2].astype(int), arr2[:, :, 3]
    orange = (r > 140) & (g > 60) & (g < 180) & (b < 90) & (r > g + 15) & (a > 0)
    if orange.any():
        oys, oxs = np.where(orange)
        b_m = (oxs < 500) & (oys < 450)
        a_m = (oxs >= 450) & (oys >= 500)
        if b_m.any():
            landmarks["b_site"] = (int(oxs[b_m].mean()), int(oys[b_m].mean()))
        if a_m.any():
            landmarks["a_site"] = (int(oxs[a_m].mean()), int(oys[a_m].mean()))

    ys, xs = np.where(arr2[:, :, 3] > 0)
    xmin, xmax, ymin, ymax = int(xs.min()), int(xs.max()), int(ys.min()), int(ys.max())
    for k, (x, y) in list(landmarks.items()):
        landmarks[k] = (
            int(np.clip(x, xmin + 8, xmax - 8)),
            int(np.clip(y, ymin + 8, ymax - 8)),
        )

    display = {
        "t_spawn": "T Spawn", "ct_spawn": "CT Spawn", "a_site": "A Site", "b_site": "B Site",
        "mid": "Mid", "top_mid": "Top Mid", "underpass": "Underpass", "connector": "Connector",
        "catwalk": "Catwalk", "palace": "Palace", "a_ramp": "A Ramp", "jungle": "Jungle",
        "ticket": "Ticket Booth", "b_apps": "B Apartments", "market": "Market",
        "window": "Window", "balcony": "Palace Balcony", "sandwich": "Sandwich",
        "van": "Van", "kitchen": "Kitchen", "b_short": "B Short",
    }
    bonuses = {
        "a_site": {"attackerTeamworkBonus": 0.10, "defenderAwarenessBonus": 0.05},
        "b_site": {"attackerTeamworkBonus": 0.05, "defenderAwarenessBonus": 0.10},
        "catwalk": {"defenderReactionBonus": 0.10, "attackerAimBonus": 0.05},
        "palace": {"defenderReactionBonus": 0.10},
        "window": {"attackerAimBonus": 0.05, "defenderReactionBonus": 0.05},
        "a_ramp": {"defenderReactionBonus": 0.05},
        "jungle": {"defenderReactionBonus": 0.05},
        "b_apps": {"defenderReactionBonus": 0.05},
        "mid": {"defenderReactionBonus": 0.05},
        "connector": {"defenderReactionBonus": 0.05},
    }

    img = Image.fromarray(arr2)
    d = ImageDraw.Draw(img)
    zones = []
    for zid, (x, y) in landmarks.items():
        d.ellipse([x - 5, y - 5, x + 5, y + 5], fill=(255, 40, 40, 255), outline=(255, 255, 0))
        d.text((x + 7, y - 8), zid, fill=(255, 255, 80))
        z = {
            "id": zid,
            "displayName": display[zid],
            "x": round(x / 1024, 3),
            "y": round(1 - y / 1024, 3),
            "attackerAimBonus": 0.0,
            "defenderReactionBonus": 0.0,
            "attackerTeamworkBonus": 0.0,
            "defenderAwarenessBonus": 0.0,
        }
        z.update(bonuses.get(zid, {}))
        zones.append(z)
        print(f"{zid}: u={z['x']:.3f} v={z['y']:.3f}")
    img.save(BK / "mirage_zones_preview.png")

    routes = [
        {"id": "t_to_a_ramp", "from": "t_spawn", "to": "a_site",
         "waypoints": ["t_spawn", "top_mid", "a_ramp", "sandwich", "a_site"]},
        {"id": "t_to_a_palace", "from": "t_spawn", "to": "a_site",
         "waypoints": ["t_spawn", "palace", "balcony", "a_site"]},
        {"id": "t_to_b_apps", "from": "t_spawn", "to": "b_site",
         "waypoints": ["t_spawn", "kitchen", "b_apps", "b_site"]},
        {"id": "t_to_b_cat", "from": "t_spawn", "to": "b_site",
         "waypoints": ["t_spawn", "top_mid", "mid", "catwalk", "b_short", "b_site"]},
        {"id": "ct_to_a", "from": "ct_spawn", "to": "a_site",
         "waypoints": ["ct_spawn", "ticket", "jungle", "a_site"]},
        {"id": "ct_to_b", "from": "ct_spawn", "to": "b_site",
         "waypoints": ["ct_spawn", "market", "van", "b_site"]},
        {"id": "mid_links", "from": "mid", "to": "connector",
         "waypoints": ["mid", "underpass", "connector", "jungle"]},
        {"id": "window_links", "from": "window", "to": "mid",
         "waypoints": ["window", "mid", "catwalk"]},
        {"id": "a_links", "from": "a_site", "to": "jungle",
         "waypoints": ["a_site", "sandwich", "jungle", "connector"]},
        {"id": "b_links", "from": "b_site", "to": "market",
         "waypoints": ["b_site", "van", "market", "b_short"]},
    ]
    root = {
        "schemaVersion": 1,
        "map": {"id": "mirage", "displayName": "Mirage", "zones": zones, "routes": routes},
    }
    out = JSON / "mirage.json"
    out.write_text(json.dumps(root, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    meta = (JSON / "dust2.json.meta").read_text(encoding="utf-8")
    guid = uuid.uuid4().hex
    (JSON / "mirage.json.meta").write_text(
        re.sub(r"guid: [a-f0-9]+", "guid: " + guid, meta, 1), encoding="utf-8"
    )
    print("wrote", out, "zones", len(zones), "meta", guid)


def main():
    arr = make_overview()
    clone_meta(MAPS / "train_overview.png.meta", MAPS / "mirage_overview.png.meta", "mirage_overview_0")
    make_bgs()
    place_zones(arr)


if __name__ == "__main__":
    main()
