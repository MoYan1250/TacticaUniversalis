# -*- coding: utf-8 -*-
from __future__ import annotations

import json
import math
from pathlib import Path

import numpy as np
from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
JSON_DIR = ROOT / "Assets/_Project/Data/JSON"
MAPS_DIR = ROOT / "Assets/_Project/Resources/Maps"
MAP_IDS = ("dust2", "train", "ancient", "overpass", "vertigo", "inferno", "mirage", "nuke")


def main() -> None:
    failures = []
    for map_id in MAP_IDS:
        data = json.loads((JSON_DIR / f"{map_id}.json").read_text(encoding="utf-8"))["map"]
        zones = {zone["id"]: zone for zone in data["zones"]}
        edges = {zone_id: set() for zone_id in zones}
        masks = {}
        for floor in data["floors"]:
            image_name = floor["overviewResource"].split("/")[-1] + ".png"
            masks[floor["id"]] = np.array(
                Image.open(MAPS_DIR / image_name).convert("RGBA")
            )[:, :, 3]

        transitions = 0
        for route in data["routes"]:
            points = route.get("waypoints") or [route["from"], route["to"]]
            for point in points:
                if point not in zones:
                    failures.append(f"{map_id}: {route['id']} references {point}")
            for start_id, goal_id in zip(points, points[1:]):
                if start_id not in zones or goal_id not in zones:
                    continue
                edges[start_id].add(goal_id)
                edges[goal_id].add(start_id)
                start, goal = zones[start_id], zones[goal_id]
                if start["floor"] != goal["floor"]:
                    transitions += 1
                    continue
                alpha = masks[start["floor"]]
                height, width = alpha.shape
                samples = max(
                    8,
                    int(math.hypot(
                        (start["x"] - goal["x"]) * width,
                        (start["y"] - goal["y"]) * height,
                    ) / 3),
                )
                xs = np.linspace(start["x"] * width, goal["x"] * width, samples)
                ys = np.linspace(
                    (1.0 - start["y"]) * height,
                    (1.0 - goal["y"]) * height,
                    samples,
                )
                xs = np.clip(xs, 0, width - 1).astype(int)
                ys = np.clip(ys, 0, height - 1).astype(int)
                inside = float((alpha[ys, xs] > 32).mean())
                # Low-res densify cells can sit on high-res alpha corners; require mostly walkable.
                if inside < 0.4:
                    failures.append(
                        f"{map_id}: {start_id}->{goal_id} leaves floor ({inside:.2f})"
                    )

        start_id = "t_spawn"
        seen = {start_id}
        queue = [start_id]
        while queue:
            current = queue.pop()
            unseen = edges[current] - seen
            seen.update(unseen)
            queue.extend(unseen)
        for zone in zones.values():
            if not zone.get("navigationOnly", False) and zone["id"] not in seen:
                failures.append(f"{map_id}: disconnected zone {zone['id']}")
        if len(data["floors"]) > 1 and transitions < 2:
            failures.append(f"{map_id}: only {transitions} floor transitions")
        print(
            map_id,
            f"zones={len(zones)}",
            f"routes={len(data['routes'])}",
            f"floorTransitions={transitions}",
        )

    if failures:
        raise SystemExit("\n".join(failures))
    print("all map navigation checks passed")


if __name__ == "__main__":
    main()
