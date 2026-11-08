# -*- coding: utf-8 -*-
"""Fix general-mode marker jitter: stable slots, no mid-move relayout, hop duration match."""
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str, count: int = 1) -> str:
    if old not in text:
        raise RuntimeError(f"patch anchor missing: {old[:140]!r}")
    return text.replace(old, new, count)


def main() -> int:
    subprocess.check_call([sys.executable, str(ROOT / "Tools" / "backup_quickmatch.py"), "general_move_jitter"])
    text = TARGET.read_text(encoding="utf-8")
    if "GeneralModeMoveDuration" in text and "stable general-mode ring" in text:
        print("already patched")
        return 0

    # Field for current move destination (skip redundant restarts)
    if "_moveDestinations" not in text:
        text = must_replace(
            text,
            "        private readonly Dictionary<string, Coroutine> _moveRoutines = new Dictionary<string, Coroutine>();\n",
            "        private readonly Dictionary<string, Coroutine> _moveRoutines = new Dictionary<string, Coroutine>();\n"
            "        private readonly Dictionary<string, string> _moveDestinations =\n"
            "            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);\n",
        )

    # Replace MoveMarkerAlongPath body for smoother general mode
    text = must_replace(
        text,
        "        private void MoveMarkerAlongPath(string playerId, string zoneId, bool animate)\n"
        "        {\n"
        "            if (string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(zoneId))\n"
        "                return;\n"
        "            if (!_markers.TryGetValue(playerId, out RectTransform marker))\n"
        "                return;\n"
        "\n"
        "            string from = _playerZones.TryGetValue(playerId, out string current) ? current : zoneId;\n"
        "            IReadOnlyList<string> path = _pathfinder.FindPath(from, zoneId);\n"
        "            _playerZones[playerId] = zoneId;\n"
        "\n"
        "            // Logical zone is shared; visual slot is unique among current occupants.\n"
        "            Vector2 endOffset = ZoneOccupantOffset(playerId, zoneId);\n"
        "            if (!animate || path.Count == 0)\n"
        "            {\n"
        "                marker.anchoredPosition = ClampToMap(LocalZonePosition(zoneId) + endOffset);\n"
        "                if (!string.Equals(from, zoneId, StringComparison.OrdinalIgnoreCase))\n"
        "                    RelayoutZone(from, false, 0f);\n"
        "                RelayoutZone(zoneId, false, 0f);\n"
        "                return;\n"
        "            }\n"
        "\n"
        "            float duration = IsGeneralBattleMode()\n"
        "                ? Mathf.Clamp(0.08f * Mathf.Max(1, path.Count - 1), 0.05f, 0.40f)\n"
        "                : Mathf.Clamp(0.28f * Mathf.Max(1, path.Count - 1), 0.35f, 1.6f);\n"
        "            if (!string.Equals(from, zoneId, StringComparison.OrdinalIgnoreCase))\n"
        "                RelayoutZone(from, true, 0.18f);\n"
        "            StartMoveRoutine(playerId, FollowPathRoutine(playerId, path, endOffset, duration));\n"
        "        }\n",
        "        private void MoveMarkerAlongPath(string playerId, string zoneId, bool animate)\n"
        "        {\n"
        "            if (string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(zoneId))\n"
        "                return;\n"
        "            if (!_markers.TryGetValue(playerId, out RectTransform marker))\n"
        "                return;\n"
        "\n"
        "            string from = _playerZones.TryGetValue(playerId, out string current) ? current : zoneId;\n"
        "            if (string.Equals(from, zoneId, StringComparison.OrdinalIgnoreCase))\n"
        "                return;\n"
        "\n"
        "            // Already tweening to this zone — do not restart (avoids hop jitter).\n"
        "            if (IsGeneralBattleMode() &&\n"
        "                _moveDestinations.TryGetValue(playerId, out string pending) &&\n"
        "                string.Equals(pending, zoneId, StringComparison.OrdinalIgnoreCase) &&\n"
        "                _moveRoutines.TryGetValue(playerId, out Coroutine busy) && busy != null)\n"
        "            {\n"
        "                _playerZones[playerId] = zoneId;\n"
        "                return;\n"
        "            }\n"
        "\n"
        "            IReadOnlyList<string> path = _pathfinder.FindPath(from, zoneId);\n"
        "            _playerZones[playerId] = zoneId;\n"
        "\n"
        "            Vector2 endOffset = ZoneOccupantOffset(playerId, zoneId);\n"
        "            if (!animate || path.Count == 0)\n"
        "            {\n"
        "                marker.anchoredPosition = ClampToMap(LocalZonePosition(zoneId) + endOffset);\n"
        "                _moveDestinations.Remove(playerId);\n"
        "                if (!IsGeneralBattleMode())\n"
        "                {\n"
        "                    if (!string.Equals(from, zoneId, StringComparison.OrdinalIgnoreCase))\n"
        "                        RelayoutZone(from, false, 0f);\n"
        "                    RelayoutZone(zoneId, false, 0f);\n"
        "                }\n"
        "                return;\n"
        "            }\n"
        "\n"
        "            float duration = IsGeneralBattleMode()\n"
        "                ? GeneralModeMoveDuration(path.Count)\n"
        "                : Mathf.Clamp(0.28f * Mathf.Max(1, path.Count - 1), 0.35f, 1.6f);\n"
        "            // General mode: never shove teammates mid-path; quick mode keeps pack layout.\n"
        "            if (!IsGeneralBattleMode() &&\n"
        "                !string.Equals(from, zoneId, StringComparison.OrdinalIgnoreCase))\n"
        "                RelayoutZone(from, true, 0.18f);\n"
        "            _moveDestinations[playerId] = zoneId;\n"
        "            StartMoveRoutine(playerId, FollowPathRoutine(playerId, path, endOffset, duration));\n"
        "        }\n"
        "\n"
        "        private static float GeneralModeMoveDuration(int pathCount)\n"
        "        {\n"
        "            // Match Domain MoveIntervalSeconds (~0.45) so hops finish before the next.\n"
        "            int hops = Mathf.Max(1, pathCount - 1);\n"
        "            return Mathf.Clamp(0.42f * hops, 0.38f, 0.85f);\n"
        "        }\n",
    )

    # SettlePlayerInZone: don't RelayoutZone in general mode
    text = must_replace(
        text,
        "            marker.anchoredPosition = ClampToMap(LocalZonePosition(zoneId) + ZoneOccupantOffset(playerId, zoneId));\n"
        "            RelayoutZone(zoneId, false, 0f);\n"
        "            RefreshFloorVisibility();\n"
        "        }\n",
        "            marker.anchoredPosition = ClampToMap(LocalZonePosition(zoneId) + ZoneOccupantOffset(playerId, zoneId));\n"
        "            _moveDestinations.Remove(playerId);\n"
        "            if (!IsGeneralBattleMode())\n"
        "                RelayoutZone(zoneId, false, 0f);\n"
        "            RefreshFloorVisibility();\n"
        "        }\n",
    )

    # Stable occupant offset in general mode (player number ring, not live stack index)
    text = must_replace(
        text,
        "        private Vector2 ZoneOccupantOffset(string playerId, string zoneId)\n"
        "        {\n"
        "            List<string> occupants = ActiveOccupants(zoneId);\n"
        "            int index = occupants.IndexOf(playerId);\n"
        "            if (index < 0)\n"
        "                index = Mathf.Max(0, PlayerNumber(playerId) - 1);\n"
        "            return SlotRingOffset(index, Mathf.Max(1, occupants.Count));\n"
        "        }\n",
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
        "            List<string> occupants = ActiveOccupants(zoneId);\n"
        "            int index = occupants.IndexOf(playerId);\n"
        "            if (index < 0)\n"
        "                index = Mathf.Max(0, PlayerNumber(playerId) - 1);\n"
        "            return SlotRingOffset(index, Mathf.Max(1, occupants.Count));\n"
        "        }\n",
    )

    # Clear destinations on StopAllMovement
    if "StopAllMovement()" in text and "_moveDestinations.Clear()" not in text:
        text = must_replace(
            text,
            "            _moveRoutines.Clear();\n",
            "            _moveRoutines.Clear();\n"
            "            _moveDestinations.Clear();\n",
        )

    if "叫暫停" not in text and "KillFeed" not in text:
        raise RuntimeError("UTF-8 corruption suspected")

    TARGET.write_text(text, encoding="utf-8")
    print("patched", TARGET)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
