# -*- coding: utf-8 -*-
"""Import Nuke using main upper overview only; ignore top-left / hatched lower B layer."""
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

OV_SRC = SRC / "c__Users_approlu_AppData_Roaming_Cursor_User_workspaceStorage_9d73533b536d22ebdcc9a9d7ab3cd46f_images_image-3f9c340c-8d81-46fc-b04f-3dc22c948d65.png"
CALL_SRC = SRC / "c__Users_approlu_AppData_Roaming_Cursor_User_workspaceStorage_9d73533b536d22ebdcc9a9d7ab3cd46f_images_image-45b8526f-23f0-41d9-ab43-7d2537ea1d9c.png"
BG_SRCS = [
    SRC / "c__Users_approlu_AppData_Roaming_Cursor_User_workspaceStorage_9d73533b536d22ebdcc9a9d7ab3cd46f_images_image-6a6f3363-62a2-49ac-b2f8-56b28ee7421b.png",
    SRC / "c__Users_approlu_AppData_Roaming_Cursor_User_workspaceStorage_9d73533b536d22ebdcc9a9d7ab3cd46f_images_image-d9af0c69-c17e-4e29-a4d7-f41846d63303.png",
    SRC / "c__Users_approlu_AppData_Roaming_Cursor_User_workspaceStorage_9d73533b536d22ebdcc9a9d7ab3cd46f_images_image-787febf5-5ebd-4d56-95b3-d7931a723c6a.png",
    SRC / "c__Users_approlu_AppData_Roaming_Cursor_User_workspaceStorage_9d73533b536d22ebdcc9a9d7ab3cd46f_images_image-7035aedf-3e43-4918-b09e-d003b9f74248.png",
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
    arr = np.array(ov)
    r, g, b, a = arr[:, :, 0].astype(int), arr[:, :, 1].astype(int), arr[:, :, 2].astype(int), arr[:, :, 3]
    # Drop hatched/teal lower-layer overlay (top inset) — keep main upper floor.
    teal = (g > b) & (b > r) & ((g - r) > 18) & (b > 35) & (b < 190) & (a > 0)
    # Also drop diagonal-hatch-looking mid blues that are only in northern protrusion
    hatch = teal & (np.arange(arr.shape[0])[:, None] < int(arr.shape[0] * 0.55))
    # Only clear contiguous northern teal band, not yard blues: require near top third OR strong teal
    ys = np.arange(arr.shape[0])[:, None]
    clear = teal & (ys < int(arr.shape[0] * 0.62))
    arr[clear, 3] = 0
    print("cleared lower-overlay px", int(clear.sum()))

    # black -> transparent
    dark = (r < 18) & (g < 18) & (b < 18)
    arr[dark, 3] = 0

    ov2 = Image.fromarray(arr)
    w, h = ov2.size
    side = max(w, h)
    canvas = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    canvas.paste(ov2, ((side - w) // 2, (side - h) // 2), ov2)
    out = canvas.resize((1024, 1024), Image.Resampling.LANCZOS)
    arr2 = np.array(out)
    mask2 = (arr2[:, :, 0] < 12) & (arr2[:, :, 1] < 12) & (arr2[:, :, 2] < 12)
    arr2[mask2, 3] = 0
    out = Image.fromarray(arr2)
    out.save(MAPS / "nuke_overview.png")
    ys2, xs2 = np.where(arr2[:, :, 3] > 0)
    print("overview", out.size, "opaque", int((arr2[:, :, 3] > 0).sum()),
          "bbox", int(xs2.min()), int(ys2.min()), int(xs2.max()), int(ys2.max()))

    r2, g2, b2, a2 = arr2[:, :, 0].astype(int), arr2[:, :, 1].astype(int), arr2[:, :, 2].astype(int), arr2[:, :, 3]
    orange = (r2 > 140) & (g2 > 50) & (g2 < 180) & (b2 < 90) & (r2 > g2 + 15) & (a2 > 0)
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

    # Callout: blank top-left lower inset, keep main upper
    call = Image.open(CALL_SRC).convert("RGB")
    ca = np.array(call)
    ch, cw = ca.shape[:2]
    # Zero top-left inset roughly (small lower-floor panel)
    ca[: int(ch * 0.38), : int(cw * 0.42)] = 0
    Image.fromarray(ca).save(BK / "nuke_callout_upper_ref.png")
    return arr2


def make_bgs():
    for i, p in enumerate(BG_SRCS):
        im = Image.open(p).convert("RGB")
        dest = BGS / f"nuke_bg_{i}.png"
        im.save(dest, quality=95)
        print("bg", i, dest.name, im.size)
        clone_meta(BGS / f"train_bg_{i}.png.meta", BGS / f"nuke_bg_{i}.png.meta", f"nuke_bg_{i}_0")


def place_zones(arr2):
    # Upper orientation: T left, CT right, A center silos, Outside/Yard south of A toward CT,
    # Ramp north of A (B approach stand-in until lower layer). Ignore lower inset.
    landmarks = {
        "t_spawn": (100, 520),
        "ct_spawn": (920, 520),
        "a_site": (520, 480),
        "b_site": (560, 280),  # ramp / B approach on upper for now
        "lobby": (320, 480),
        "hut": (440, 480),
        "squeaky": (400, 520),
        "outside": (700, 620),
        "garage": (780, 700),
        "ramp": (620, 300),
        "heaven": (600, 400),
        "mini": (620, 520),
        "silo": (260, 560),
        "radio": (280, 420),
        "lockers": (720, 440),
        "secret": (800, 640),
        "a_rafters": (500, 400),
        "trophy": (360, 440),
        "truck": (180, 560),
        "hell": (640, 460),
        "main": (580, 560),
    }

    r, g, b, a = arr2[:, :, 0].astype(int), arr2[:, :, 1].astype(int), arr2[:, :, 2].astype(int), arr2[:, :, 3]
    orange = (r > 140) & (g > 50) & (g < 180) & (b < 90) & (r > g + 15) & (a > 0)
    if orange.any():
        oys, oxs = np.where(orange)
        # A silos / site often center-ish orange
        a_m = (oxs > 350) & (oxs < 700) & (oys > 350) & (oys < 650)
        if a_m.any():
            landmarks["a_site"] = (int(oxs[a_m].mean()), int(oys[a_m].mean()))
        elif orange.any():
            landmarks["a_site"] = (int(oxs.mean()), int(oys.mean()))

    ys, xs = np.where(arr2[:, :, 3] > 0)
    xmin, xmax, ymin, ymax = int(xs.min()), int(xs.max()), int(ys.min()), int(ys.max())
    for k, (x, y) in list(landmarks.items()):
        landmarks[k] = (
            int(np.clip(x, xmin + 8, xmax - 8)),
            int(np.clip(y, ymin + 8, ymax - 8)),
        )

    display = {
        "t_spawn": "T Spawn", "ct_spawn": "CT Spawn", "a_site": "A Site", "b_site": "B Site",
        "lobby": "Lobby", "hut": "Hut", "squeaky": "Squeaky", "outside": "Outside",
        "garage": "Garage", "ramp": "Ramp", "heaven": "Heaven", "mini": "Mini",
        "silo": "Silo", "radio": "Radio", "lockers": "Lockers", "secret": "Secret",
        "a_rafters": "Rafters", "trophy": "Trophy", "truck": "Truck", "hell": "Hell",
        "main": "Main",
    }
    bonuses = {
        "a_site": {"attackerTeamworkBonus": 0.10, "defenderAwarenessBonus": 0.05},
        "b_site": {"attackerTeamworkBonus": 0.05, "defenderAwarenessBonus": 0.10},
        "ramp": {"defenderReactionBonus": 0.10, "attackerAimBonus": 0.05},
        "heaven": {"attackerAimBonus": 0.05, "defenderReactionBonus": 0.05},
        "hut": {"defenderReactionBonus": 0.10},
        "outside": {"defenderReactionBonus": 0.05},
        "squeaky": {"defenderReactionBonus": 0.05},
        "garage": {"defenderReactionBonus": 0.05},
        "secret": {"defenderReactionBonus": 0.05},
        "mini": {"defenderReactionBonus": 0.05},
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
    img.save(BK / "nuke_zones_preview.png")

    routes = [
        {"id": "t_to_a_lobby", "from": "t_spawn", "to": "a_site",
         "waypoints": ["t_spawn", "truck", "silo", "lobby", "hut", "a_site"]},
        {"id": "t_to_a_outside", "from": "t_spawn", "to": "a_site",
         "waypoints": ["t_spawn", "outside", "main", "mini", "a_site"]},
        {"id": "t_to_b_ramp", "from": "t_spawn", "to": "b_site",
         "waypoints": ["t_spawn", "lobby", "trophy", "ramp", "b_site"]},
        {"id": "t_to_b_secret", "from": "t_spawn", "to": "b_site",
         "waypoints": ["t_spawn", "outside", "garage", "secret", "b_site"]},
        {"id": "ct_to_a", "from": "ct_spawn", "to": "a_site",
         "waypoints": ["ct_spawn", "lockers", "heaven", "hell", "a_site"]},
        {"id": "ct_to_b", "from": "ct_spawn", "to": "b_site",
         "waypoints": ["ct_spawn", "ramp", "b_site"]},
        {"id": "a_links", "from": "a_site", "to": "heaven",
         "waypoints": ["a_site", "a_rafters", "heaven", "hell"]},
        {"id": "outside_links", "from": "outside", "to": "mini",
         "waypoints": ["outside", "garage", "main", "mini"]},
        {"id": "lobby_links", "from": "lobby", "to": "squeaky",
         "waypoints": ["lobby", "radio", "squeaky", "hut"]},
    ]
    root = {
        "schemaVersion": 1,
        "map": {"id": "nuke", "displayName": "Nuke", "zones": zones, "routes": routes},
    }
    out = JSON / "nuke.json"
    out.write_text(json.dumps(root, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    meta = (JSON / "dust2.json.meta").read_text(encoding="utf-8")
    guid = uuid.uuid4().hex
    (JSON / "nuke.json.meta").write_text(
        re.sub(r"guid: [a-f0-9]+", "guid: " + guid, meta, 1), encoding="utf-8"
    )
    print("wrote", out, "zones", len(zones), "meta", guid)


def main():
    arr = make_overview()
    clone_meta(MAPS / "train_overview.png.meta", MAPS / "nuke_overview.png.meta", "nuke_overview_0")
    make_bgs()
    place_zones(arr)


if __name__ == "__main__":
    main()
