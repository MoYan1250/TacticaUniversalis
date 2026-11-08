# -*- coding: utf-8 -*-
"""Import Vertigo using LEFT overview/callout only (ignore right lower layer)."""
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

OV_SRC = SRC / "c__Users_approlu_AppData_Roaming_Cursor_User_workspaceStorage_9d73533b536d22ebdcc9a9d7ab3cd46f_images_image-c763b72d-00d2-4ee8-b025-40bce821c553.png"
CALL_SRC = SRC / "c__Users_approlu_AppData_Roaming_Cursor_User_workspaceStorage_9d73533b536d22ebdcc9a9d7ab3cd46f_images_image-d65d8e68-818a-45cd-a9fb-4dfac986942a.png"
BG_SRCS = [
    SRC / "c__Users_approlu_AppData_Roaming_Cursor_User_workspaceStorage_9d73533b536d22ebdcc9a9d7ab3cd46f_images_image-b6170804-70a5-437a-9562-7e78cd785e8b.png",
    SRC / "c__Users_approlu_AppData_Roaming_Cursor_User_workspaceStorage_9d73533b536d22ebdcc9a9d7ab3cd46f_images_image-63765da1-a6f3-40e4-94a0-a14283db6763.png",
    SRC / "c__Users_approlu_AppData_Roaming_Cursor_User_workspaceStorage_9d73533b536d22ebdcc9a9d7ab3cd46f_images_image-c75b8d5f-4669-4a5b-92f4-52065baf75c4.png",
    SRC / "c__Users_approlu_AppData_Roaming_Cursor_User_workspaceStorage_9d73533b536d22ebdcc9a9d7ab3cd46f_images_image-f6bac2dc-0772-4a53-abf2-aa0b729bd7f4.png",
]


def clone_meta(src_meta, dest_meta, sprite_name):
    t = Path(src_meta).read_text(encoding="utf-8")
    guid = uuid.uuid4().hex
    t = re.sub(r"guid: [a-f0-9]+", "guid: " + guid, t, count=1)
    t = re.sub(r"second: \w+", "second: " + sprite_name, t, count=1)
    Path(dest_meta).write_text(t, encoding="utf-8")
    print("meta", dest_meta.name, guid)


def crop_left(im: Image.Image) -> Image.Image:
    w, h = im.size
    # Find split: middle of black gap between two floors
    arr = np.array(im.convert("RGB"))
    # column opacity / non-black density
    dark = (arr[:, :, 0] < 20) & (arr[:, :, 1] < 20) & (arr[:, :, 2] < 20)
    col_floor = (~dark).mean(axis=0)
    # find widest near-black vertical band around center
    mid = w // 2
    best = mid
    best_score = -1
    for x in range(int(w * 0.35), int(w * 0.65)):
        # score: dark column + neighbors
        score = 1.0 - col_floor[max(0, x - 2):x + 3].mean()
        if score > best_score:
            best_score = score
            best = x
    # also try simple half if floor content is clearly left-heavy
    left_cut = best
    # ensure left has content
    if col_floor[:left_cut].sum() < 5:
        left_cut = w // 2
    print("left_cut", left_cut, "of", w, "score", round(best_score, 3))
    return im.crop((0, 0, left_cut, h))


def make_overview():
    full = Image.open(OV_SRC).convert("RGBA")
    left = crop_left(full)
    # black -> transparent then pad square
    arr = np.array(left)
    mask = (arr[:, :, 0] < 18) & (arr[:, :, 1] < 18) & (arr[:, :, 2] < 18)
    arr[mask, 3] = 0
    left = Image.fromarray(arr)
    w, h = left.size
    side = max(w, h)
    canvas = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    canvas.paste(left, ((side - w) // 2, (side - h) // 2), left)
    out = canvas.resize((1024, 1024), Image.Resampling.LANCZOS)
    arr2 = np.array(out)
    mask2 = (arr2[:, :, 0] < 12) & (arr2[:, :, 1] < 12) & (arr2[:, :, 2] < 12)
    arr2[mask2, 3] = 0
    out = Image.fromarray(arr2)
    out.save(MAPS / "vertigo_overview.png")
    ys, xs = np.where(arr2[:, :, 3] > 0)
    print("overview", out.size, "opaque", int((arr2[:, :, 3] > 0).sum()),
          "bbox", int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max()))

    # orange sites
    r, g, b, a = arr2[:, :, 0].astype(int), arr2[:, :, 1].astype(int), arr2[:, :, 2].astype(int), arr2[:, :, 3]
    orange = (r > 140) & (g > 50) & (g < 160) & (b < 80) & (r > g + 20) & (a > 0)
    print("orange", int(orange.sum()))
    if orange.any():
        oys, oxs = np.where(orange)
        # cluster by position
        for name, m in [("TL", (oxs < 512) & (oys < 512)), ("TR", (oxs >= 512) & (oys < 512)),
                        ("BL", (oxs < 512) & (oys >= 512)), ("BR", (oxs >= 512) & (oys >= 512))]:
            if m.any():
                print(name, oxs[m].mean() / 1024, 1 - oys[m].mean() / 1024, int(m.sum()))

    # save left callout ref
    call = Image.open(CALL_SRC).convert("RGB")
    call_left = crop_left(call)
    call_left.save(BK / "vertigo_callout_left_ref.png")
    return arr2


def make_bgs():
    for i, p in enumerate(BG_SRCS):
        im = Image.open(p).convert("RGB")
        dest = BGS / f"vertigo_bg_{i}.png"
        im.save(dest, quality=95)
        print("bg", i, dest.name)
        clone_meta(BGS / f"train_bg_{i}.png.meta", BGS / f"vertigo_bg_{i}.png.meta", f"vertigo_bg_{i}_0")


def place_zones(arr2):
    # Left-layer callout orientation: B top-left, CT top-center, A bottom-right,
    # T stairs/spawn bottom-left, Mid center, Elevator center-right.
    landmarks = {
        "t_spawn": (220, 880),
        "ct_spawn": (520, 120),
        "b_site": (220, 160),
        "a_site": (780, 780),
        "mid": (480, 450),
        "elevator": (620, 480),
        "connector": (520, 280),
        "t_stairs": (260, 780),
        "b_stairs": (280, 320),
        "ramp": (700, 650),
        "top_ramp": (720, 580),
        "scaffold": (860, 700),
        "catwalk": (900, 620),
        "generator": (300, 200),
        "rafters": (180, 220),
        "ladder": (580, 560),
        "boost": (420, 500),
        "sandbag": (760, 700),
        "sidewalk": (840, 760),
        "quad": (160, 140),
        "pillar_b": (250, 180),
    }

    r, g, b, a = arr2[:, :, 0].astype(int), arr2[:, :, 1].astype(int), arr2[:, :, 2].astype(int), arr2[:, :, 3]
    orange = (r > 140) & (g > 50) & (g < 160) & (b < 80) & (r > g + 20) & (a > 0)
    if orange.any():
        oys, oxs = np.where(orange)
        # top-left orange -> B, bottom-right-ish -> A
        tl = (oxs < 512) & (oys < 450)
        br = (oxs >= 450) & (oys >= 500)
        if tl.any():
            landmarks["b_site"] = (int(oxs[tl].mean()), int(oys[tl].mean()))
        if br.any():
            landmarks["a_site"] = (int(oxs[br].mean()), int(oys[br].mean()))
        elif ((oxs >= 512) & (oys >= 400)).any():
            m = (oxs >= 512) & (oys >= 400)
            landmarks["a_site"] = (int(oxs[m].mean()), int(oys[m].mean()))

    display = {
        "t_spawn": "T Spawn", "ct_spawn": "CT Spawn", "b_site": "B Site", "a_site": "A Site",
        "mid": "Mid", "elevator": "Elevator", "connector": "Connector",
        "t_stairs": "T Stairs", "b_stairs": "B Stairs", "ramp": "Ramp", "top_ramp": "Top Ramp",
        "scaffold": "Scaffold", "catwalk": "Catwalk", "generator": "Generator",
        "rafters": "Rafters", "ladder": "Ladder", "boost": "Boost", "sandbag": "Sandbag",
        "sidewalk": "Sidewalk", "quad": "Quad", "pillar_b": "B Pillar",
    }
    bonuses = {
        "a_site": {"attackerTeamworkBonus": 0.10, "defenderAwarenessBonus": 0.05},
        "b_site": {"attackerTeamworkBonus": 0.05, "defenderAwarenessBonus": 0.10},
        "mid": {"defenderReactionBonus": 0.05},
        "elevator": {"defenderReactionBonus": 0.10},
        "ramp": {"defenderReactionBonus": 0.05, "attackerAimBonus": 0.05},
        "scaffold": {"attackerAimBonus": 0.05},
        "connector": {"defenderReactionBonus": 0.05},
        "generator": {"defenderReactionBonus": 0.05},
    }

    img = Image.fromarray(arr2)
    d = ImageDraw.Draw(img)
    zones = []
    for zid, (x, y) in landmarks.items():
        x = int(np.clip(x, 20, 1004))
        y = int(np.clip(y, 20, 1004))
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
    img.save(BK / "vertigo_zones_preview.png")

    routes = [
        {"id": "t_to_b", "from": "t_spawn", "to": "b_site",
         "waypoints": ["t_spawn", "t_stairs", "mid", "b_stairs", "generator", "b_site"]},
        {"id": "t_to_a_ramp", "from": "t_spawn", "to": "a_site",
         "waypoints": ["t_spawn", "t_stairs", "mid", "ramp", "top_ramp", "a_site"]},
        {"id": "t_to_a_scaffold", "from": "t_spawn", "to": "a_site",
         "waypoints": ["t_spawn", "mid", "elevator", "scaffold", "catwalk", "a_site"]},
        {"id": "ct_to_b", "from": "ct_spawn", "to": "b_site",
         "waypoints": ["ct_spawn", "connector", "b_site"]},
        {"id": "ct_to_a", "from": "ct_spawn", "to": "a_site",
         "waypoints": ["ct_spawn", "connector", "elevator", "a_site"]},
        {"id": "mid_to_b", "from": "mid", "to": "b_site",
         "waypoints": ["mid", "boost", "b_stairs", "b_site"]},
        {"id": "mid_to_a", "from": "mid", "to": "a_site",
         "waypoints": ["mid", "ladder", "ramp", "sandbag", "a_site"]},
        {"id": "b_links", "from": "b_site", "to": "rafters",
         "waypoints": ["b_site", "quad", "pillar_b", "rafters", "generator"]},
        {"id": "a_links", "from": "a_site", "to": "sidewalk",
         "waypoints": ["a_site", "sandbag", "sidewalk", "scaffold"]},
        {"id": "elevator_links", "from": "elevator", "to": "connector",
         "waypoints": ["elevator", "mid", "connector"]},
    ]
    root = {"schemaVersion": 1, "map": {"id": "vertigo", "displayName": "Vertigo", "zones": zones, "routes": routes}}
    out = JSON / "vertigo.json"
    out.write_text(json.dumps(root, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    meta = (JSON / "dust2.json.meta").read_text(encoding="utf-8")
    guid = uuid.uuid4().hex
    (JSON / "vertigo.json.meta").write_text(re.sub(r"guid: [a-f0-9]+", "guid: " + guid, meta, 1), encoding="utf-8")
    print("wrote", out, "zones", len(zones))


def main():
    arr = make_overview()
    clone_meta(MAPS / "train_overview.png.meta", MAPS / "vertigo_overview.png.meta", "vertigo_overview_0")
    make_bgs()
    place_zones(arr)


if __name__ == "__main__":
    main()
