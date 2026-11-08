# -*- coding: utf-8 -*-
from __future__ import annotations

import json
import heapq
import math
import re
import uuid
from pathlib import Path

from PIL import Image, ImageFilter
import numpy as np


ROOT = Path(__file__).resolve().parents[1]
JSON_DIR = ROOT / "Assets/_Project/Data/JSON"
MAPS_DIR = ROOT / "Assets/_Project/Resources/Maps"
SOURCE_DIR = Path(
    r"C:\Users\approlu\.cursor\projects\e-TacticaUniversalis\assets"
)

NUKE_B1 = SOURCE_DIR / (
    "c__Users_approlu_AppData_Roaming_Cursor_User_workspaceStorage_"
    "9d73533b536d22ebdcc9a9d7ab3cd46f_images_image-cc469557-e02e-4b02-b42b-81744676d90f.png"
)
NUKE_F1 = SOURCE_DIR / (
    "c__Users_approlu_AppData_Roaming_Cursor_User_workspaceStorage_"
    "9d73533b536d22ebdcc9a9d7ab3cd46f_images_image-f261b557-5b3d-4873-a850-f7515e33573f.png"
)
VERTIGO_B2 = SOURCE_DIR / (
    "c__Users_approlu_AppData_Roaming_Cursor_User_workspaceStorage_"
    "9d73533b536d22ebdcc9a9d7ab3cd46f_images_image-15f324e5-b88f-4206-b383-e74230d739b1.png"
)
VERTIGO_B1 = SOURCE_DIR / (
    "c__Users_approlu_AppData_Roaming_Cursor_User_workspaceStorage_"
    "9d73533b536d22ebdcc9a9d7ab3cd46f_images_image-ece6c278-8d85-4134-8682-635743211ecb.png"
)


POINTS = {
    "train": {
        "t_spawn": (135, 180), "ct_spawn": (905, 812),
        "a_site": (648, 528), "b_site": (550, 812),
        "alley": (520, 122), "pigeons": (806, 112), "ivy": (855, 390),
        "old_bomb": (842, 535), "brown_halls": (323, 445),
        "a_main": (390, 380), "kitchen": (410, 330), "popdog": (465, 590),
        "underpass": (342, 583), "connector": (640, 650), "heaven": (708, 585),
        "b_halls": (315, 795), "b_ramp": (395, 704), "sidewalk": (520, 712),
        "ct_short": (790, 713), "ct_long": (806, 918), "ct_stairs": (974, 640),
    },
    "ancient": {
        "t_spawn": (510, 936), "ct_spawn": (532, 116),
        "a_site": (300, 246), "b_site": (800, 420), "mid": (516, 480),
        "bottom_mid": (518, 610), "top_mid": (516, 360), "donut": (310, 470),
        "a_main": (238, 360), "a_long": (180, 270), "temple": (360, 210),
        "water": (788, 720), "ruins": (840, 650), "ramp": (760, 560),
        "cave": (690, 520), "catwalk": (620, 540), "house": (580, 250),
        "connector": (610, 185), "t_stairs": (510, 850), "tunnel": (760, 780),
        "xbox": (510, 610),
    },
    "inferno": {
        "t_spawn": (58, 752), "ct_spawn": (630, 215),
        "b_site": (482, 210), "a_site": (850, 735), "banana": (466, 470),
        "mid": (585, 715), "alt_mid": (555, 835), "underpass": (645, 800),
        "top_mid": (748, 670), "arch": (800, 580), "boiler": (685, 800),
        "apartments": (478, 875), "balcony": (465, 785), "pit": (955, 835),
        "library": (934, 580), "speedway": (786, 322),
        "construction": (632, 135), "coffins": (580, 135), "truck": (844, 322),
        "banana_car": (528, 503), "logs": (425, 528),
    },
    "mirage": {
        "t_spawn": (947, 333), "ct_spawn": (250, 700),
        "b_site": (184, 190), "a_site": (550, 745), "mid": (520, 400),
        "top_mid": (700, 330), "underpass": (402, 355), "connector": (480, 500),
        "catwalk": (520, 350), "palace": (735, 760), "a_ramp": (760, 625),
        "jungle": (390, 560), "ticket": (430, 820), "b_apps": (250, 75),
        "market": (150, 370), "window": (330, 330), "balcony": (675, 705),
        "sandwich": (590, 600), "van": (130, 95), "kitchen": (390, 150),
        "b_short": (370, 230),
    },
}

NUKE_POINTS = {
    "t_spawn": ("B1", 75, 320), "ct_spawn": ("B1", 850, 260),
    "a_site": ("B1", 570, 290), "b_site": ("F1", 570, 330),
    "lobby": ("B1", 470, 300), "hut": ("B1", 530, 320),
    "squeaky": ("B1", 520, 345), "outside": ("B1", 660, 420),
    "garage": ("B1", 740, 470), "ramp": ("B1", 565, 155),
    "heaven": ("B1", 620, 270), "mini": ("B1", 620, 330),
    "silo": ("B1", 480, 390), "radio": ("B1", 460, 280),
    "lockers": ("B1", 680, 300), "secret": ("B1", 700, 500),
    "a_rafters": ("B1", 590, 250), "trophy": ("B1", 500, 280),
    "truck": ("B1", 300, 320), "hell": ("B1", 640, 300),
    "main": ("B1", 600, 340),
    "ramp_lower": ("F1", 565, 155), "secret_lower": ("F1", 700, 500),
    "vents": ("B1", 555, 345), "vents_lower": ("F1", 555, 370),
}

VERTIGO_POINTS = {
    "t_spawn": ("B2", 260, 850), "ct_spawn": ("B2", 620, 150),
    "b_site": ("B2", 175, 145), "a_site": ("B2", 820, 650),
    "mid": ("B2", 460, 370), "elevator": ("B2", 560, 390),
    "connector": ("B2", 420, 165), "t_stairs": ("B2", 190, 410),
    "b_stairs": ("B2", 205, 235), "a_ramp": ("B2", 700, 620),
    "top_ramp": ("B2", 740, 560), "scaffold": ("B2", 850, 570),
    "a_catwalk": ("B2", 875, 470), "generator": ("B2", 280, 130),
    "rafters": ("B2", 120, 130), "ladder": ("B2", 440, 520),
    "boost": ("B2", 430, 330), "sandbag": ("B2", 735, 610),
    "a_sidewalk": ("B2", 800, 700), "quad": ("B2", 110, 95),
    "pillar_b": ("B2", 210, 115),
    "lower_t_stairs": ("B1", 190, 410), "lower_b_stairs": ("B1", 205, 235),
    "lower_ladder": ("B1", 440, 520), "lower_lobby": ("B1", 310, 420),
    "lower_bridge": ("B1", 525, 405), "lower_ramp": ("B1", 720, 520),
}


def non_black_bbox(images: list[Image.Image]) -> tuple[int, int, int, int]:
    mask = None
    for image in images:
        rgb = np.array(image.convert("RGB"))
        current = np.max(rgb, axis=2) > 18
        mask = current if mask is None else mask | current
    ys, xs = np.where(mask)
    return int(xs.min()), int(ys.min()), int(xs.max() + 1), int(ys.max() + 1)


def convert_floor_images(
    sources: list[Path], outputs: list[str]
) -> tuple[tuple[int, int, int, int], int, int]:
    images = [Image.open(path).convert("RGBA") for path in sources]
    bbox = non_black_bbox(images)
    left, top, right, bottom = bbox
    width, height = right - left, bottom - top
    side = max(width, height)
    offset_x = (side - width) // 2
    offset_y = (side - height) // 2
    for image, output in zip(images, outputs):
        crop = image.crop(bbox)
        arr = np.array(crop)
        dark = np.max(arr[:, :, :3], axis=2) < 18
        arr[dark, 3] = 0
        canvas = Image.new("RGBA", (side, side), (0, 0, 0, 0))
        canvas.paste(Image.fromarray(arr), (offset_x, offset_y))
        canvas.resize((1024, 1024), Image.Resampling.LANCZOS).save(MAPS_DIR / output)
    return bbox, offset_x, offset_y


def transformed_uv(
    x: float, y: float, bbox: tuple[int, int, int, int], offset_x: int, offset_y: int
) -> tuple[float, float]:
    left, top, right, bottom = bbox
    side = max(right - left, bottom - top)
    px = (x - left + offset_x) / side
    py = (y - top + offset_y) / side
    return round(px, 4), round(1.0 - py, 4)


def floor_meta(source_name: str, output_name: str) -> None:
    source = (MAPS_DIR / f"{source_name}.png.meta").read_text(encoding="utf-8")
    source = re.sub(r"guid: [a-f0-9]+", "guid: " + uuid.uuid4().hex, source, count=1)
    source = source.replace(source_name + "_0", output_name + "_0")
    source = source.replace("name: " + source_name, "name: " + output_name)
    (MAPS_DIR / f"{output_name}.png.meta").write_text(source, encoding="utf-8")


def load_map(map_id: str) -> tuple[Path, dict]:
    path = JSON_DIR / f"{map_id}.json"
    return path, json.loads(path.read_text(encoding="utf-8"))


def apply_points(map_id: str, points: dict[str, tuple[int, int]]) -> None:
    path, root = load_map(map_id)
    map_data = root["map"]
    map_data["zones"] = [
        zone for zone in map_data["zones"] if not zone["id"].startswith("nav_")
    ]
    map_data["defaultFloor"] = "main"
    map_data["floors"] = [{
        "id": "main",
        "displayName": "主樓層",
        "overviewResource": f"Maps/{map_id}_overview",
    }]
    for zone in map_data["zones"]:
        if zone["id"] not in points:
            raise RuntimeError(f"{map_id}: missing point for {zone['id']}")
        x, y = points[zone["id"]]
        zone["x"] = round(x / 1024.0, 4)
        zone["y"] = round(1.0 - y / 1024.0, 4)
        zone["floor"] = "main"
        zone["navigationOnly"] = False
    path.write_text(json.dumps(root, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def add_zone(map_data: dict, zone_id: str, floor: str, uv: tuple[float, float]) -> None:
    map_data["zones"].append({
        "id": zone_id,
        "displayName": zone_id.replace("_", " ").title(),
        "x": uv[0],
        "y": uv[1],
        "floor": floor,
        "navigationOnly": True,
        "attackerAimBonus": 0.0,
        "defenderReactionBonus": 0.0,
        "attackerTeamworkBonus": 0.0,
        "defenderAwarenessBonus": 0.0,
    })


def apply_multifloor(
    map_id: str,
    points: dict[str, tuple[str, int, int]],
    bbox: tuple[int, int, int, int],
    offset_x: int,
    offset_y: int,
    default_floor: str,
    floors: list[dict],
) -> dict:
    path, root = load_map(map_id)
    map_data = root["map"]
    map_data["zones"] = [
        zone for zone in map_data["zones"] if not zone["id"].startswith("nav_")
    ]
    map_data["defaultFloor"] = default_floor
    map_data["floors"] = floors
    existing = {zone["id"]: zone for zone in map_data["zones"]}
    for zone_id, (floor, x, y) in points.items():
        uv = transformed_uv(x, y, bbox, offset_x, offset_y)
        if zone_id in existing:
            zone = existing[zone_id]
            zone["x"], zone["y"] = uv
            zone["floor"] = floor
            zone["navigationOnly"] = False
        else:
            add_zone(map_data, zone_id, floor, uv)
    path.write_text(json.dumps(root, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    return root


def patch_nuke_routes(root: dict) -> None:
    routes = [
        route for route in root["map"]["routes"] if route["id"] != "a_to_b_vents"
    ]
    root["map"]["routes"] = routes
    for route in routes:
        if route["id"] == "t_to_b_ramp":
            route["waypoints"] = route["logicalWaypoints"] = [
                "t_spawn", "lobby", "trophy", "ramp", "ramp_lower", "b_site"
            ]
        elif route["id"] == "t_to_b_secret":
            route["waypoints"] = route["logicalWaypoints"] = [
                "t_spawn", "outside", "garage", "secret", "secret_lower", "b_site"
            ]
        elif route["id"] == "ct_to_b":
            route["waypoints"] = route["logicalWaypoints"] = [
                "ct_spawn", "lockers", "ramp", "ramp_lower", "b_site"
            ]
    routes.append({
        "id": "a_to_b_vents",
        "from": "a_site",
        "to": "b_site",
        "waypoints": ["a_site", "vents", "vents_lower", "b_site"],
    })
    path = JSON_DIR / "nuke.json"
    path.write_text(json.dumps(root, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def patch_vertigo_routes(root: dict) -> None:
    lower_ids = {"lower_t_route", "lower_b_route", "lower_a_route"}
    root["map"]["routes"] = [
        route for route in root["map"]["routes"] if route["id"] not in lower_ids
    ]
    for route in root["map"]["routes"]:
        route["waypoints"] = [
            {"ramp": "a_ramp", "catwalk": "a_catwalk", "sidewalk": "a_sidewalk"}.get(p, p)
            for p in route["waypoints"]
        ]
        if route.get("logicalWaypoints"):
            route["logicalWaypoints"] = [
                {"ramp": "a_ramp", "catwalk": "a_catwalk", "sidewalk": "a_sidewalk"}.get(p, p)
                for p in route["logicalWaypoints"]
            ]
    root["map"]["routes"].extend([
        {
            "id": "lower_t_route",
            "from": "t_spawn",
            "to": "mid",
            "waypoints": [
                "t_spawn", "t_stairs", "lower_t_stairs", "lower_lobby",
                "lower_bridge", "lower_ladder", "ladder", "mid",
            ],
        },
        {
            "id": "lower_b_route",
            "from": "t_spawn",
            "to": "b_site",
            "waypoints": [
                "t_spawn", "t_stairs", "lower_t_stairs", "lower_lobby",
                "lower_b_stairs", "b_stairs", "generator", "b_site",
            ],
        },
        {
            "id": "lower_a_route",
            "from": "t_spawn",
            "to": "a_site",
            "waypoints": [
                "t_spawn", "t_stairs", "lower_t_stairs", "lower_bridge",
                "lower_ramp", "a_ramp", "top_ramp", "a_site",
            ],
        },
    ])
    path = JSON_DIR / "vertigo.json"
    path.write_text(json.dumps(root, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def add_single_floor_metadata(map_id: str) -> None:
    path, root = load_map(map_id)
    map_data = root["map"]
    map_data["defaultFloor"] = "main"
    map_data["floors"] = [{
        "id": "main",
        "displayName": "主樓層",
        "overviewResource": f"Maps/{map_id}_overview",
    }]
    for zone in map_data["zones"]:
        zone["floor"] = "main"
        zone["navigationOnly"] = False
    path.write_text(json.dumps(root, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def nearest_walkable(mask: np.ndarray, point: tuple[int, int]) -> tuple[int, int]:
    x, y = point
    if 0 <= y < mask.shape[0] and 0 <= x < mask.shape[1] and mask[y, x]:
        return x, y
    for radius in range(1, max(mask.shape)):
        for yy in range(max(0, y - radius), min(mask.shape[0], y + radius + 1)):
            for xx in (x - radius, x + radius):
                if 0 <= xx < mask.shape[1] and mask[yy, xx]:
                    return xx, yy
        for xx in range(max(0, x - radius), min(mask.shape[1], x + radius + 1)):
            for yy in (y - radius, y + radius):
                if 0 <= yy < mask.shape[0] and mask[yy, xx]:
                    return xx, yy
    raise RuntimeError("floor has no walkable pixels")


def astar(mask: np.ndarray, start: tuple[int, int], goal: tuple[int, int]) -> list[tuple[int, int]]:
    start = nearest_walkable(mask, start)
    goal = nearest_walkable(mask, goal)
    queue = [(0.0, start)]
    came_from = {start: None}
    cost = {start: 0.0}
    moves = [
        (-1, 0, 1.0), (1, 0, 1.0), (0, -1, 1.0), (0, 1, 1.0),
        (-1, -1, 1.414), (-1, 1, 1.414), (1, -1, 1.414), (1, 1, 1.414),
    ]
    while queue:
        _, current = heapq.heappop(queue)
        if current == goal:
            break
        for dx, dy, move_cost in moves:
            nxt = current[0] + dx, current[1] + dy
            if not (0 <= nxt[0] < mask.shape[1] and 0 <= nxt[1] < mask.shape[0]):
                continue
            if not mask[nxt[1], nxt[0]]:
                continue
            new_cost = cost[current] + move_cost
            if nxt in cost and new_cost >= cost[nxt]:
                continue
            cost[nxt] = new_cost
            came_from[nxt] = current
            heuristic = math.hypot(goal[0] - nxt[0], goal[1] - nxt[1])
            heapq.heappush(queue, (new_cost + heuristic, nxt))
    if goal not in came_from:
        return [start, goal]
    result = []
    current = goal
    while current is not None:
        result.append(current)
        current = came_from[current]
    result.reverse()
    return result


def clear_line(mask: np.ndarray, a: tuple[int, int], b: tuple[int, int]) -> bool:
    distance = max(abs(b[0] - a[0]), abs(b[1] - a[1]))
    if distance == 0:
        return True
    for i in range(distance + 1):
        t = i / distance
        x = int(round(a[0] + (b[0] - a[0]) * t))
        y = int(round(a[1] + (b[1] - a[1]) * t))
        if not mask[y, x]:
            return False
    return True


def simplify_path(
    mask: np.ndarray, path: list[tuple[int, int]], max_step: int = 4
) -> list[tuple[int, int]]:
    if len(path) <= 2:
        return path
    result = [path[0]]
    current = 0
    while current < len(path) - 1:
        target = min(len(path) - 1, current + max_step)
        while target > current + 1 and not clear_line(mask, path[current], path[target]):
            target -= 1
        result.append(path[target])
        current = target
    return result


def floor_masks(map_data: dict, size: int = 192) -> dict[str, np.ndarray]:
    masks = {}
    for floor in map_data["floors"]:
        path = MAPS_DIR / (floor["overviewResource"].split("/")[-1] + ".png")
        alpha = Image.open(path).convert("RGBA").getchannel("A")
        small = alpha.resize((size, size), Image.Resampling.NEAREST)
        # Erode one grid cell so routes stay away from void edges.
        eroded = small.filter(ImageFilter.MinFilter(3))
        mask = np.array(eroded) > 32
        if not mask.any():
            mask = np.array(small) > 32
        masks[floor["id"]] = mask
    return masks


def densify_routes(map_id: str) -> None:
    path, root = load_map(map_id)
    map_data = root["map"]
    map_data["zones"] = [
        zone for zone in map_data["zones"] if not zone["id"].startswith("nav_")
    ]
    zones = {zone["id"]: zone for zone in map_data["zones"]}
    masks = floor_masks(map_data)
    serial = 0
    for route in map_data["routes"]:
        logical = route.get("logicalWaypoints")
        if not logical:
            logical = [
                point for point in (route.get("waypoints") or [route["from"], route["to"]])
                if point in zones and not zones[point].get("navigationOnly", False)
            ]
            # Keep authored connector zones even though they are navigation-only.
            authored = route.get("waypoints") or []
            logical = [point for point in authored if point in zones and not point.startswith("nav_")]
        route["logicalWaypoints"] = logical
        expanded = [logical[0]]
        for start_id, goal_id in zip(logical, logical[1:]):
            start, goal = zones[start_id], zones[goal_id]
            if start["floor"] != goal["floor"]:
                expanded.append(goal_id)
                continue
            mask = masks[start["floor"]]
            width, height = mask.shape[1], mask.shape[0]
            start_cell = (
                int(np.clip(start["x"] * width, 0, width - 1)),
                int(np.clip((1.0 - start["y"]) * height, 0, height - 1)),
            )
            goal_cell = (
                int(np.clip(goal["x"] * width, 0, width - 1)),
                int(np.clip((1.0 - goal["y"]) * height, 0, height - 1)),
            )
            detailed = simplify_path(
                mask, astar(mask, start_cell, goal_cell), max_step=2
            )
            snapped_start = detailed[0]
            snapped_goal = detailed[-1]
            start["x"] = round((snapped_start[0] + 0.5) / width, 4)
            start["y"] = round(1.0 - (snapped_start[1] + 0.5) / height, 4)
            goal["x"] = round((snapped_goal[0] + 0.5) / width, 4)
            goal["y"] = round(1.0 - (snapped_goal[1] + 0.5) / height, 4)
            for cell in detailed[1:-1]:
                serial += 1
                node_id = f"nav_{route['id']}_{serial}"
                uv = (
                    round((cell[0] + 0.5) / width, 4),
                    round(1.0 - (cell[1] + 0.5) / height, 4),
                )
                add_zone(map_data, node_id, start["floor"], uv)
                zones[node_id] = map_data["zones"][-1]
                expanded.append(node_id)
            expanded.append(goal_id)
        route["waypoints"] = expanded
    path.write_text(json.dumps(root, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print("densified", map_id, "navigation nodes", serial)


def main() -> None:
    for map_id, points in POINTS.items():
        apply_points(map_id, points)
        print("aligned", map_id, len(points))

    nuke_bbox, nuke_ox, nuke_oy = convert_floor_images(
        [NUKE_B1, NUKE_F1], ["nuke_overview_b1.png", "nuke_overview_f1.png"]
    )
    floor_meta("nuke_overview", "nuke_overview_b1")
    floor_meta("nuke_overview", "nuke_overview_f1")
    nuke_root = apply_multifloor(
        "nuke", NUKE_POINTS, nuke_bbox, nuke_ox, nuke_oy, "B1",
        [
            {"id": "B1", "displayName": "B1", "overviewResource": "Maps/nuke_overview_b1"},
            {"id": "F1", "displayName": "F1", "overviewResource": "Maps/nuke_overview_f1"},
        ],
    )
    patch_nuke_routes(nuke_root)

    vertigo_bbox, vertigo_ox, vertigo_oy = convert_floor_images(
        [VERTIGO_B2, VERTIGO_B1],
        ["vertigo_overview_b2.png", "vertigo_overview_b1.png"],
    )
    floor_meta("vertigo_overview", "vertigo_overview_b2")
    floor_meta("vertigo_overview", "vertigo_overview_b1")
    vertigo_root = apply_multifloor(
        "vertigo", VERTIGO_POINTS, vertigo_bbox, vertigo_ox, vertigo_oy, "B2",
        [
            {"id": "B2", "displayName": "B2", "overviewResource": "Maps/vertigo_overview_b2"},
            {"id": "B1", "displayName": "B1", "overviewResource": "Maps/vertigo_overview_b1"},
        ],
    )
    patch_vertigo_routes(vertigo_root)

    for map_id in ("dust2", "overpass"):
        add_single_floor_metadata(map_id)
    for map_id in (
        "dust2", "train", "ancient", "overpass",
        "vertigo", "inferno", "mirage", "nuke",
    ):
        densify_routes(map_id)
    print("rebuilt floor maps and navigation data")


if __name__ == "__main__":
    import sys
    if len(sys.argv) > 1 and sys.argv[1] == "densify":
        for map_id in (
            "dust2", "train", "ancient", "overpass",
            "vertigo", "inferno", "mirage", "nuke",
        ):
            densify_routes(map_id)
            print("densified", map_id)
    else:
        main()
