# -*- coding: utf-8 -*-
"""General mode: no flower packing; circle collision (~50% of marker) + map bounds."""
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str, count: int = 1) -> str:
    if old not in text:
        raise RuntimeError(f"patch anchor missing: {old[:160]!r}")
    return text.replace(old, new, count)


def main() -> int:
    subprocess.check_call([sys.executable, str(ROOT / "Tools" / "backup_quickmatch.py"), "general_collision"])
    text = TARGET.read_text(encoding="utf-8")
    if "GeneralCollisionRadius" in text and "ResolveGeneralPosition" in text:
        print("already patched")
        return 0

    # Constants near other private consts if possible — add after KillFeed constants or near fields
    if "GeneralCollisionRadius" not in text:
        text = must_replace(
            text,
            "        private readonly Dictionary<string, string> _moveDestinations =\n"
            "            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);\n",
            "        private readonly Dictionary<string, string> _moveDestinations =\n"
            "            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);\n"
            "\n"
            "        /// <summary>Visual marker dot diameter in map-local pixels.</summary>\n"
            "        private const float MarkerDotDiameter = 28f;\n"
            "        /// <summary>Collision radius = 50% of visual circle diameter / 2.</summary>\n"
            "        private const float GeneralCollisionRadius = MarkerDotDiameter * 0.25f;\n",
        )

    # PositionTeam: separate with collision instead of RelayoutZone flower
    text = must_replace(
        text,
        "            // Pack all teammates visually around the shared spawn zone.\n"
        "            RelayoutZone(spawn, animate, 0.25f);\n"
        "            RefreshFloorVisibility();\n"
        "        }\n",
        "            if (IsGeneralBattleMode())\n"
        "            {\n"
        "                // Place on zone center then push apart via collision — no flower ring.\n"
        "                Vector2 center = ClampToMapCollision(LocalZonePosition(spawn));\n"
        "                for (int i = 0; i < team.playerIds.Length; i++)\n"
        "                {\n"
        "                    string id = team.playerIds[i];\n"
        "                    if (!_markers.TryGetValue(id, out RectTransform marker))\n"
        "                        continue;\n"
        "                    // Tiny deterministic seed so identical starts do not stack perfectly.\n"
        "                    float seed = (PlayerNumber(id) - 1) * 0.01f;\n"
        "                    marker.anchoredPosition = center + new Vector2(seed, -seed);\n"
        "                }\n"
        "                SeparateGeneralAgentsNear(spawn);\n"
        "            }\n"
        "            else\n"
        "                RelayoutZone(spawn, animate, 0.25f);\n"
        "            RefreshFloorVisibility();\n"
        "        }\n",
    )

    # MoveMarkerAlongPath: zero offset in general mode (already via ZoneOccupantOffset)
    text = must_replace(
        text,
        "                marker.anchoredPosition = ClampToMap(LocalZonePosition(zoneId) + endOffset);\n"
        "                _moveDestinations.Remove(playerId);\n"
        "                if (!IsGeneralBattleMode())\n"
        "                {\n"
        "                    if (!string.Equals(from, zoneId, StringComparison.OrdinalIgnoreCase))\n"
        "                        RelayoutZone(from, false, 0f);\n"
        "                    RelayoutZone(zoneId, false, 0f);\n"
        "                }\n"
        "                return;\n"
        "            }\n",
        "                Vector2 landed = LocalZonePosition(zoneId) + endOffset;\n"
        "                marker.anchoredPosition = IsGeneralBattleMode()\n"
        "                    ? ResolveGeneralPosition(playerId, landed)\n"
        "                    : ClampToMap(landed);\n"
        "                _moveDestinations.Remove(playerId);\n"
        "                if (!IsGeneralBattleMode())\n"
        "                {\n"
        "                    if (!string.Equals(from, zoneId, StringComparison.OrdinalIgnoreCase))\n"
        "                        RelayoutZone(from, false, 0f);\n"
        "                    RelayoutZone(zoneId, false, 0f);\n"
        "                }\n"
        "                return;\n"
        "            }\n",
    )

    # FollowPathRoutine: clamp+collision each frame; path points use ClampToMapCollision
    text = must_replace(
        text,
        "            var points = new List<Vector2>();\n"
        "            for (int i = 0; i < path.Count; i++)\n"
        "            {\n"
        "                Vector2 p = LocalZonePosition(path[i]);\n"
        "                if (i == path.Count - 1)\n"
        "                    p += endOffset;\n"
        "                points.Add(ClampToMap(p));\n"
        "            }\n",
        "            var points = new List<Vector2>();\n"
        "            for (int i = 0; i < path.Count; i++)\n"
        "            {\n"
        "                Vector2 p = LocalZonePosition(path[i]);\n"
        "                if (i == path.Count - 1)\n"
        "                    p += endOffset;\n"
        "                points.Add(IsGeneralBattleMode() ? ClampToMapCollision(p) : ClampToMap(p));\n"
        "            }\n",
    )

    text = must_replace(
        text,
        "            if (points.Count == 1)\n"
        "            {\n"
        "                marker.anchoredPosition = points[0];\n"
        "                SettlePlayerInZone(playerId, destination);\n"
        "                _moveRoutines[playerId] = null;\n"
        "                yield break;\n"
        "            }\n",
        "            if (points.Count == 1)\n"
        "            {\n"
        "                marker.anchoredPosition = IsGeneralBattleMode()\n"
        "                    ? ResolveGeneralPosition(playerId, points[0])\n"
        "                    : points[0];\n"
        "                SettlePlayerInZone(playerId, destination);\n"
        "                _moveRoutines[playerId] = null;\n"
        "                yield break;\n"
        "            }\n",
    )

    text = must_replace(
        text,
        "                        marker.gameObject.SetActive(\n"
        "                            !_deadMarkers.Contains(playerId) &&\n"
        "                            string.Equals(segmentFloor, _currentMapFloor, StringComparison.OrdinalIgnoreCase));\n"
        "                        marker.anchoredPosition = Vector2.Lerp(points[i], points[i + 1], local);\n"
        "                        break;\n",
        "                        marker.gameObject.SetActive(\n"
        "                            !_deadMarkers.Contains(playerId) &&\n"
        "                            string.Equals(segmentFloor, _currentMapFloor, StringComparison.OrdinalIgnoreCase));\n"
        "                        Vector2 lerped = Vector2.Lerp(points[i], points[i + 1], local);\n"
        "                        marker.anchoredPosition = IsGeneralBattleMode()\n"
        "                            ? ResolveGeneralPosition(playerId, lerped)\n"
        "                            : lerped;\n"
        "                        break;\n",
    )

    # SettlePlayerInZone
    text = must_replace(
        text,
        "        private void SettlePlayerInZone(string playerId, string zoneId)\n"
        "        {\n"
        "            if (string.IsNullOrEmpty(zoneId) || !_markers.TryGetValue(playerId, out RectTransform marker))\n"
        "                return;\n"
        "            marker.anchoredPosition = ClampToMap(LocalZonePosition(zoneId) + ZoneOccupantOffset(playerId, zoneId));\n"
        "            _moveDestinations.Remove(playerId);\n"
        "            if (!IsGeneralBattleMode())\n"
        "                RelayoutZone(zoneId, false, 0f);\n"
        "            RefreshFloorVisibility();\n"
        "        }\n",
        "        private void SettlePlayerInZone(string playerId, string zoneId)\n"
        "        {\n"
        "            if (string.IsNullOrEmpty(zoneId) || !_markers.TryGetValue(playerId, out RectTransform marker))\n"
        "                return;\n"
        "            Vector2 desired = LocalZonePosition(zoneId) + ZoneOccupantOffset(playerId, zoneId);\n"
        "            marker.anchoredPosition = IsGeneralBattleMode()\n"
        "                ? ResolveGeneralPosition(playerId, desired)\n"
        "                : ClampToMap(desired);\n"
        "            _moveDestinations.Remove(playerId);\n"
        "            if (IsGeneralBattleMode())\n"
        "                SeparateGeneralAgentsNear(zoneId);\n"
        "            else\n"
        "                RelayoutZone(zoneId, false, 0f);\n"
        "            RefreshFloorVisibility();\n"
        "        }\n",
    )

    # ZoneOccupantOffset: no flower in general mode
    text = must_replace(
        text,
        "        private Vector2 ZoneOccupantOffset(string playerId, string zoneId)\n"
        "        {\n"
        "            // Stable general-mode ring: live occupant reindex makes markers jump when\n"
        "            // teammates arrive/leave the same zone.\n"
        "            if (IsGeneralBattleMode())\n"
        "            {\n"
        "                int slot = Mathf.Max(0, PlayerNumber(playerId) - 1);\n"
        "                return SlotRingOffset(slot, 5);\n"
        "            }\n"
        "\n"
        "            List<string> occupants = ActiveOccupants(zoneId);\n",
        "        private Vector2 ZoneOccupantOffset(string playerId, string zoneId)\n"
        "        {\n"
        "            // General mode uses physical collision instead of flower packing.\n"
        "            if (IsGeneralBattleMode())\n"
        "                return Vector2.zero;\n"
        "\n"
        "            List<string> occupants = ActiveOccupants(zoneId);\n",
    )

    # ClampToMap + new collision helpers after ClampToMap
    text = must_replace(
        text,
        "        private Vector2 ClampToMap(Vector2 local)\n"
        "        {\n"
        "            GetMapContentSize(out float width, out float height);\n"
        "            float pad = Mathf.Min(28f, Mathf.Min(width, height) * 0.04f);\n"
        "            return new Vector2(\n"
        "                Mathf.Clamp(local.x, -width * 0.5f + pad, width * 0.5f - pad),\n"
        "                Mathf.Clamp(local.y, -height * 0.5f + pad, height * 0.5f - pad));\n"
        "        }\n",
        "        private Vector2 ClampToMap(Vector2 local)\n"
        "        {\n"
        "            GetMapContentSize(out float width, out float height);\n"
        "            float pad = Mathf.Min(28f, Mathf.Min(width, height) * 0.04f);\n"
        "            return new Vector2(\n"
        "                Mathf.Clamp(local.x, -width * 0.5f + pad, width * 0.5f - pad),\n"
        "                Mathf.Clamp(local.y, -height * 0.5f + pad, height * 0.5f - pad));\n"
        "        }\n"
        "\n"
        "        /// <summary>Map bounds inset by agent collision radius so bodies stay inside.</summary>\n"
        "        private Vector2 ClampToMapCollision(Vector2 local)\n"
        "        {\n"
        "            GetMapContentSize(out float width, out float height);\n"
        "            float pad = Mathf.Max(GeneralCollisionRadius + 1f,\n"
        "                Mathf.Min(28f, Mathf.Min(width, height) * 0.04f));\n"
        "            return new Vector2(\n"
        "                Mathf.Clamp(local.x, -width * 0.5f + pad, width * 0.5f - pad),\n"
        "                Mathf.Clamp(local.y, -height * 0.5f + pad, height * 0.5f - pad));\n"
        "        }\n"
        "\n"
        "        private Vector2 ResolveGeneralPosition(string playerId, Vector2 desired)\n"
        "        {\n"
        "            desired = ClampToMapCollision(desired);\n"
        "            float minDist = GeneralCollisionRadius * 2f;\n"
        "            for (int iter = 0; iter < 5; iter++)\n"
        "            {\n"
        "                foreach (KeyValuePair<string, RectTransform> pair in _markers)\n"
        "                {\n"
        "                    if (string.Equals(pair.Key, playerId, StringComparison.OrdinalIgnoreCase))\n"
        "                        continue;\n"
        "                    if (_deadMarkers.Contains(pair.Key))\n"
        "                        continue;\n"
        "                    RectTransform other = pair.Value;\n"
        "                    if (other == null || !other.gameObject.activeSelf)\n"
        "                        continue;\n"
        "\n"
        "                    Vector2 delta = desired - other.anchoredPosition;\n"
        "                    float dist = delta.magnitude;\n"
        "                    if (dist >= minDist)\n"
        "                        continue;\n"
        "                    if (dist < 0.001f)\n"
        "                    {\n"
        "                        float ang = PlayerNumber(playerId) * 1.2566f;\n"
        "                        delta = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));\n"
        "                        dist = 0.001f;\n"
        "                    }\n"
        "                    desired += delta / dist * (minDist - dist);\n"
        "                }\n"
        "                desired = ClampToMapCollision(desired);\n"
        "            }\n"
        "            return desired;\n"
        "        }\n"
        "\n"
        "        private void SeparateGeneralAgentsNear(string zoneId)\n"
        "        {\n"
        "            if (!IsGeneralBattleMode())\n"
        "                return;\n"
        "            // Relax nearby agents so stacks at a zone break apart without flower layout.\n"
        "            for (int iter = 0; iter < 8; iter++)\n"
        "            {\n"
        "                foreach (KeyValuePair<string, RectTransform> pair in _markers)\n"
        "                {\n"
        "                    if (_deadMarkers.Contains(pair.Key))\n"
        "                        continue;\n"
        "                    RectTransform marker = pair.Value;\n"
        "                    if (marker == null || !marker.gameObject.activeSelf)\n"
        "                        continue;\n"
        "                    if (!string.IsNullOrEmpty(zoneId) &&\n"
        "                        _playerZones.TryGetValue(pair.Key, out string z) &&\n"
        "                        !string.Equals(z, zoneId, StringComparison.OrdinalIgnoreCase))\n"
        "                        continue;\n"
        "                    if (_moveRoutines.TryGetValue(pair.Key, out Coroutine moving) && moving != null)\n"
        "                        continue;\n"
        "                    marker.anchoredPosition = ResolveGeneralPosition(pair.Key, marker.anchoredPosition);\n"
        "                }\n"
        "            }\n"
        "        }\n",
    )

    if "叫暫停" not in text and "KillFeed" not in text:
        raise RuntimeError("UTF-8 corruption suspected")

    TARGET.write_text(text, encoding="utf-8")
    print("patched", TARGET)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
