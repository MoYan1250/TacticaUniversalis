# -*- coding: utf-8 -*-
"""Import Inferno assets, place zones, write inferno.json."""
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

OV_SRC = SRC / "c__Users_approlu_AppData_Roaming_Cursor_User_workspaceStorage_9d73533b536d22ebdcc9a9d7ab3cd46f_images_image-186bad01-48fe-4e5c-8f6b-1eea04bd9d10.png"
CALL_SRC = SRC / "c__Users_approlu_AppData_Roaming_Cursor_User_workspaceStorage_9d73533b536d22ebdcc9a9d7ab3cd46f_images_image-276fe288-d876-4787-bcfa-42f958bfcb80.png"
BG_SRCS = [
    SRC / "c__Users_approlu_AppData_Roaming_Cursor_User_workspaceStorage_9d73533b536d22ebdcc9a9d7ab3cd46f_images_image-103917d0-1bc6-446c-822a-1b3beba13a04.png",
    SRC / "c__Users_approlu_AppData_Roaming_Cursor_User_workspaceStorage_9d73533b536d22ebdcc9a9d7ab3cd46f_images_image-a53ddb85-c16b-46a4-8009-f6f667a794a2.png",
    SRC / "c__Users_approlu_AppData_Roaming_Cursor_User_workspaceStorage_9d73533b536d22ebdcc9a9d7ab3cd46f_images_image-23e76d31-67e2-43e2-a3dd-92b51aba904c.png",
    SRC / "c__Users_approlu_AppData_Roaming_Cursor_User_workspaceStorage_9d73533b536d22ebdcc9a9d7ab3cd46f_images_image-212c62dc-e76f-48bf-bb42-88c46857748c.png",
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
    out.save(MAPS / "inferno_overview.png")
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
    Image.open(CALL_SRC).convert("RGB").save(BK / "inferno_callout_ref.png")
    return arr2


def make_bgs():
    for i, p in enumerate(BG_SRCS):
        im = Image.open(p).convert("RGB")
        dest = BGS / f"inferno_bg_{i}.png"
        im.save(dest, quality=95)
        print("bg", i, dest.name, im.size)
        clone_meta(BGS / f"train_bg_{i}.png.meta", BGS / f"inferno_bg_{i}.png.meta", f"inferno_bg_{i}_0")


def place_zones(arr2):
    # Callout: T left, CT right, B top-left, A bottom-right, Banana lower-left curve, Mid center.
    landmarks = {
        "t_spawn": (120, 480),
        "ct_spawn": (900, 420),
        "b_site": (380, 220),
        "a_site": (780, 780),
        "banana": (280, 620),
        "mid": (480, 520),
        "alt_mid": (400, 560),
        "underpass": (450, 600),
        "top_mid": (560, 420),
        "arch": (620, 480),
        "boiler": (640, 560),
        "apartments": (820, 560),
        "balcony": (800, 680),
        "pit": (760, 860),
        "library": (860, 620),
        "speedway": (700, 320),
        "construction": (500, 240),
        "coffins": (420, 180),
        "truck": (720, 720),
        "banana_car": (240, 700),
        "logs": (300, 540),
    }

    r, g, b, a = arr2[:, :, 0].astype(int), arr2[:, :, 1].astype(int), arr2[:, :, 2].astype(int), arr2[:, :, 3]
    orange = (r > 140) & (g > 60) & (g < 180) & (b < 90) & (r > g + 15) & (a > 0)
    if orange.any():
        oys, oxs = np.where(orange)
        # B = top-left-ish orange, A = bottom-right orange
        b_m = (oxs < 600) & (oys < 450)
        a_m = (oxs >= 500) & (oys >= 500)
        if b_m.any():
            landmarks["b_site"] = (int(oxs[b_m].mean()), int(oys[b_m].mean()))
        if a_m.any():
            landmarks["a_site"] = (int(oxs[a_m].mean()), int(oys[a_m].mean()))

    # clamp into floor
    ys, xs = np.where(arr2[:, :, 3] > 0)
    xmin, xmax, ymin, ymax = int(xs.min()), int(xs.max()), int(ys.min()), int(ys.max())
    for k, (x, y) in list(landmarks.items()):
        landmarks[k] = (
            int(np.clip(x, xmin + 8, xmax - 8)),
            int(np.clip(y, ymin + 8, ymax - 8)),
        )

    display = {
        "t_spawn": "T Spawn", "ct_spawn": "CT Spawn", "a_site": "A Site", "b_site": "B Site",
        "banana": "Banana", "mid": "Mid", "alt_mid": "Alt Mid", "underpass": "Underpass",
        "top_mid": "Top Mid", "arch": "Arch", "boiler": "Boiler", "apartments": "Apartments",
        "balcony": "Balcony", "pit": "Pit", "library": "Library", "speedway": "Speedway",
        "construction": "Construction", "coffins": "Coffins", "truck": "Truck",
        "banana_car": "Banana Car", "logs": "Logs",
    }
    bonuses = {
        "a_site": {"attackerTeamworkBonus": 0.10, "defenderAwarenessBonus": 0.05},
        "b_site": {"attackerTeamworkBonus": 0.05, "defenderAwarenessBonus": 0.10},
        "banana": {"defenderReactionBonus": 0.10, "attackerAimBonus": 0.05},
        "apartments": {"defenderReactionBonus": 0.10},
        "arch": {"attackerAimBonus": 0.05, "defenderReactionBonus": 0.05},
        "pit": {"defenderReactionBonus": 0.05},
        "boiler": {"defenderReactionBonus": 0.05},
        "construction": {"defenderReactionBonus": 0.05},
        "mid": {"defenderReactionBonus": 0.05},
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
    img.save(BK / "inferno_zones_preview.png")

    routes = [
        {"id": "t_to_b_banana", "from": "t_spawn", "to": "b_site",
         "waypoints": ["t_spawn", "banana_car", "banana", "logs", "b_site"]},
        {"id": "t_to_a_mid", "from": "t_spawn", "to": "a_site",
         "waypoints": ["t_spawn", "alt_mid", "mid", "top_mid", "boiler", "a_site"]},
        {"id": "t_to_a_apps", "from": "t_spawn", "to": "a_site",
         "waypoints": ["t_spawn", "mid", "apartments", "balcony", "a_site"]},
        {"id": "t_to_a_arch", "from": "t_spawn", "to": "a_site",
         "waypoints": ["t_spawn", "mid", "top_mid", "arch", "truck", "a_site"]},
        {"id": "ct_to_a", "from": "ct_spawn", "to": "a_site",
         "waypoints": ["ct_spawn", "library", "pit", "a_site"]},
        {"id": "ct_to_b", "from": "ct_spawn", "to": "b_site",
         "waypoints": ["ct_spawn", "speedway", "construction", "coffins", "b_site"]},
        {"id": "mid_links", "from": "mid", "to": "arch",
         "waypoints": ["mid", "underpass", "top_mid", "arch"]},
        {"id": "a_links", "from": "a_site", "to": "apartments",
         "waypoints": ["a_site", "pit", "balcony", "apartments"]},
        {"id": "b_links", "from": "banana", "to": "construction",
         "waypoints": ["banana", "b_site", "coffins", "construction"]},
    ]
    root = {
        "schemaVersion": 1,
        "map": {"id": "inferno", "displayName": "Inferno", "zones": zones, "routes": routes},
    }
    out = JSON / "inferno.json"
    out.write_text(json.dumps(root, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    meta = (JSON / "dust2.json.meta").read_text(encoding="utf-8")
    guid = uuid.uuid4().hex
    (JSON / "inferno.json.meta").write_text(
        re.sub(r"guid: [a-f0-9]+", "guid: " + guid, meta, 1), encoding="utf-8"
    )
    print("wrote", out, "zones", len(zones), "meta", guid)


def main():
    arr = make_overview()
    clone_meta(MAPS / "train_overview.png.meta", MAPS / "inferno_overview.png.meta", "inferno_overview_0")
    make_bgs()
    place_zones(arr)


if __name__ == "__main__":
    main()
