# -*- coding: utf-8 -*-
"""Import Overpass assets, place zones, write overpass.json."""
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

OV_SRC = SRC / "c__Users_approlu_AppData_Roaming_Cursor_User_workspaceStorage_9d73533b536d22ebdcc9a9d7ab3cd46f_images_image-4fc8ed25-2ca0-4f9e-b02c-8e3eb73a8087.png"
CALL_SRC = SRC / "c__Users_approlu_AppData_Roaming_Cursor_User_workspaceStorage_9d73533b536d22ebdcc9a9d7ab3cd46f_images_image-0bfb0c3e-6671-472c-9eb6-7edaf21dc1d1.png"
BG_SRCS = [
    SRC / "c__Users_approlu_AppData_Roaming_Cursor_User_workspaceStorage_9d73533b536d22ebdcc9a9d7ab3cd46f_images_image-0be8b2ee-910b-4213-80a1-b09e7071129f.png",
    SRC / "c__Users_approlu_AppData_Roaming_Cursor_User_workspaceStorage_9d73533b536d22ebdcc9a9d7ab3cd46f_images_image-c2a7527f-c15c-4a8b-aebd-9dfea0f3a2ff.png",
    SRC / "c__Users_approlu_AppData_Roaming_Cursor_User_workspaceStorage_9d73533b536d22ebdcc9a9d7ab3cd46f_images_image-d708fe7f-7899-4f22-afcb-0d364ecfa7ce.png",
    SRC / "c__Users_approlu_AppData_Roaming_Cursor_User_workspaceStorage_9d73533b536d22ebdcc9a9d7ab3cd46f_images_image-784eb0ed-58cb-4965-8359-67cb93f4bfbb.png",
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
    out.save(MAPS / "overpass_overview.png")
    print("overview", out.size, "opaque", int((arr2[:, :, 3] > 0).sum()))
    ys, xs = np.where(arr2[:, :, 3] > 0)
    print("floor bbox", int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max()))
    # orange markers
    r, g, b, a = arr2[:, :, 0].astype(int), arr2[:, :, 1].astype(int), arr2[:, :, 2].astype(int), arr2[:, :, 3]
    orange = (r > 140) & (g > 60) & (g < 180) & (b < 90) & (r > g + 15) & (a > 0)
    print("orange px", int(orange.sum()))
    if orange.any():
        oys, oxs = np.where(orange)
        # cluster by y
        top = oys < 512
        bot = ~top
        if top.any():
            print("orange top", oxs[top].mean() / 1024, 1 - oys[top].mean() / 1024, int(top.sum()))
        if bot.any():
            print("orange bot", oxs[bot].mean() / 1024, 1 - oys[bot].mean() / 1024, int(bot.sum()))
        # left/right
        left = oxs < 512
        if left.any():
            print("orange L", oxs[left].mean() / 1024, 1 - oys[left].mean() / 1024, int(left.sum()))
        if (~left).any():
            print("orange R", oxs[~left].mean() / 1024, 1 - oys[~left].mean() / 1024, int((~left).sum()))
    Image.open(CALL_SRC).convert("RGB").save(BK / "overpass_callout_ref.png")
    return arr2


def make_bgs():
    for i, p in enumerate(BG_SRCS):
        im = Image.open(p).convert("RGB")
        dest = BGS / f"overpass_bg_{i}.png"
        im.save(dest, quality=95)
        print("bg", i, dest.name, im.size)
        clone_meta(BGS / f"train_bg_{i}.png.meta", BGS / f"overpass_bg_{i}.png.meta", f"overpass_bg_{i}_0")


def place_zones(arr2):
    # Overview orientation (inspected): A near top, B lower-right teal, T bottom, CT above A.
    # Align to this overview UV; callout labels for naming.
    # Pixel coords top-left origin on 1024 overview.
    landmarks = {
        "t_spawn": (420, 920),
        "ct_spawn": (520, 90),
        "a_site": (480, 180),
        "b_site": (780, 720),
        "long": (180, 450),
        "toilets": (400, 420),
        "short": (480, 320),
        "mid": (450, 560),
        "fountain": (420, 700),
        "playground": (380, 820),
        "connector": (560, 520),
        "water": (720, 520),
        "monster": (820, 580),
        "canal": (700, 620),
        "b_short": (720, 650),
        "heaven": (620, 280),
        "bank": (420, 120),
        "truck": (540, 200),
        "squeaky": (580, 450),
        "upper_tunnels": (650, 780),
        "ramp": (500, 750),
    }

    # Snap A/B to orange if present
    r, g, b, a = arr2[:, :, 0].astype(int), arr2[:, :, 1].astype(int), arr2[:, :, 2].astype(int), arr2[:, :, 3]
    orange = (r > 140) & (g > 60) & (g < 180) & (b < 90) & (r > g + 15) & (a > 0)
    if orange.any():
        oys, oxs = np.where(orange)
        # A = uppermost orange cluster, B = lower/right
        order = np.argsort(oys)
        # split by median y of orange
        med = np.median(oys)
        top = oys <= med
        bot = oys > med
        if top.any():
            landmarks["a_site"] = (int(oxs[top].mean()), int(oys[top].mean()))
        if bot.any():
            landmarks["b_site"] = (int(oxs[bot].mean()), int(oys[bot].mean()))
        elif (~top).any():
            right = oxs > oxs.mean()
            landmarks["b_site"] = (int(oxs[right].mean()), int(oys[right].mean()))

    display = {
        "t_spawn": "T Spawn", "ct_spawn": "CT Spawn", "a_site": "A Site", "b_site": "B Site",
        "long": "Long", "toilets": "Toilets", "short": "Short", "mid": "Mid",
        "fountain": "Fountain", "playground": "Playground", "connector": "Connector",
        "water": "Water", "monster": "Monster", "canal": "Canal", "b_short": "B Short",
        "heaven": "Heaven", "bank": "Bank", "truck": "Truck", "squeaky": "Squeaky",
        "upper_tunnels": "Upper Tunnels", "ramp": "Ramp",
    }
    bonuses = {
        "a_site": {"attackerTeamworkBonus": 0.10, "defenderAwarenessBonus": 0.05},
        "b_site": {"attackerTeamworkBonus": 0.05, "defenderAwarenessBonus": 0.10},
        "monster": {"defenderReactionBonus": 0.10, "attackerAimBonus": 0.05},
        "connector": {"defenderReactionBonus": 0.10},
        "long": {"attackerAimBonus": 0.05, "defenderReactionBonus": 0.05},
        "heaven": {"attackerAimBonus": 0.05, "defenderReactionBonus": 0.05},
        "water": {"defenderReactionBonus": 0.05},
        "short": {"defenderReactionBonus": 0.05},
        "toilets": {"defenderReactionBonus": 0.05},
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
    img.save(BK / "overpass_zones_preview.png")

    routes = [
        {"id": "t_to_a_long", "from": "t_spawn", "to": "a_site",
         "waypoints": ["t_spawn", "playground", "fountain", "long", "a_site"]},
        {"id": "t_to_a_short", "from": "t_spawn", "to": "a_site",
         "waypoints": ["t_spawn", "ramp", "fountain", "mid", "toilets", "short", "a_site"]},
        {"id": "t_to_b_monster", "from": "t_spawn", "to": "b_site",
         "waypoints": ["t_spawn", "upper_tunnels", "monster", "b_site"]},
        {"id": "t_to_b_water", "from": "t_spawn", "to": "b_site",
         "waypoints": ["t_spawn", "ramp", "connector", "water", "canal", "b_short", "b_site"]},
        {"id": "ct_to_a", "from": "ct_spawn", "to": "a_site",
         "waypoints": ["ct_spawn", "bank", "truck", "a_site"]},
        {"id": "ct_to_b", "from": "ct_spawn", "to": "b_site",
         "waypoints": ["ct_spawn", "heaven", "b_site"]},
        {"id": "mid_to_b", "from": "mid", "to": "b_site",
         "waypoints": ["mid", "connector", "squeaky", "water", "b_site"]},
        {"id": "connector_links", "from": "connector", "to": "short",
         "waypoints": ["connector", "squeaky", "short"]},
        {"id": "a_links", "from": "a_site", "to": "heaven",
         "waypoints": ["a_site", "truck", "heaven"]},
        {"id": "b_links", "from": "monster", "to": "b_short",
         "waypoints": ["monster", "canal", "b_short", "b_site"]},
    ]
    root = {
        "schemaVersion": 1,
        "map": {"id": "overpass", "displayName": "Overpass", "zones": zones, "routes": routes},
    }
    out = JSON / "overpass.json"
    out.write_text(json.dumps(root, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    meta = (JSON / "dust2.json.meta").read_text(encoding="utf-8")
    guid = uuid.uuid4().hex
    (JSON / "overpass.json.meta").write_text(
        re.sub(r"guid: [a-f0-9]+", "guid: " + guid, meta, 1), encoding="utf-8"
    )
    print("wrote", out, "zones", len(zones), "meta", guid)


def main():
    arr = make_overview()
    clone_meta(MAPS / "train_overview.png.meta", MAPS / "overpass_overview.png.meta", "overpass_overview_0")
    make_bgs()
    place_zones(arr)


if __name__ == "__main__":
    main()
