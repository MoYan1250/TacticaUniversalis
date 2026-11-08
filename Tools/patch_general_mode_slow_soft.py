# -*- coding: utf-8 -*-
"""Slow general-mode pace + no hard collision shove while chasing."""
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"anchor missing: {old[:180]!r}")
    return text.replace(old, new, 1)


def main() -> int:
    subprocess.check_call(
        [sys.executable, str(ROOT / "Tools" / "backup_quickmatch.py"), "slow_chase_soft_collide"]
    )
    text = TARGET.read_text(encoding="utf-8")

    text = must_replace(
        text,
        "        /// <summary>General-mode visual travel speed in map-local pixels per second.</summary>\n"
        "        private const float GeneralChaseSpeed = 140f;\n",
        "        /// <summary>General-mode visual travel speed in map-local pixels per second.</summary>\n"
        "        private const float GeneralChaseSpeed = 58f;\n",
    )

    old_tick = '''        private void TickGeneralChases()
        {
            if (!IsGeneralBattleMode() || _generalChases.Count == 0)
                return;
            if (_paused)
                return;

            float step = GeneralChaseSpeed * Time.unscaledDeltaTime * Mathf.Max(0.25f, playbackSpeed);
            var arrived = new List<string>();
            foreach (KeyValuePair<string, GeneralChaseState> pair in _generalChases)
            {
                string playerId = pair.Key;
                GeneralChaseState chase = pair.Value;
                if (!_markers.TryGetValue(playerId, out RectTransform marker) || marker == null)
                {
                    arrived.Add(playerId);
                    continue;
                }
                if (_deadMarkers.Contains(playerId))
                {
                    arrived.Add(playerId);
                    continue;
                }

                chase.Progress = Mathf.Min(chase.TotalLength, chase.Progress + step);
                Vector2 pos = SamplePolyline(chase.Points, chase.SegmentLengths, chase.Progress);
                pos = ClampToMapCollision(pos);
                // Soft overlap nudge while moving — no hard shove.
                pos = ResolveGeneralPosition(playerId, pos);
                marker.anchoredPosition = pos;

                int zoneIndex = 0;
                float walked = 0f;
                for (int i = 0; i < chase.SegmentLengths.Length; i++)
                {
                    if (walked + chase.SegmentLengths[i] >= chase.Progress ||
                        i == chase.SegmentLengths.Length - 1)
                    {
                        zoneIndex = i;
                        break;
                    }
                    walked += chase.SegmentLengths[i];
                }
                if (chase.PathZones != null && chase.PathZones.Length > 0)
                {
                    string segmentFloor = ZoneFloor(
                        chase.PathZones[Mathf.Min(zoneIndex, chase.PathZones.Length - 1)]);
                    marker.gameObject.SetActive(
                        string.Equals(segmentFloor, _currentMapFloor, StringComparison.OrdinalIgnoreCase));
                }

                if (chase.Progress >= chase.TotalLength - 0.5f)
                    arrived.Add(playerId);
            }

            for (int i = 0; i < arrived.Count; i++)
            {
                string id = arrived[i];
                if (_generalChases.TryGetValue(id, out GeneralChaseState done))
                {
                    SettlePlayerInZone(id, done.TargetZone);
                    _generalChases.Remove(id);
                }
            }
        }
'''

    new_tick = '''        private void TickGeneralChases()
        {
            if (!IsGeneralBattleMode() || _generalChases.Count == 0)
                return;
            if (_paused)
                return;

            float step = GeneralChaseSpeed * Time.unscaledDeltaTime * Mathf.Max(0.25f, playbackSpeed);
            var arrived = new List<string>();
            foreach (KeyValuePair<string, GeneralChaseState> pair in _generalChases)
            {
                string playerId = pair.Key;
                GeneralChaseState chase = pair.Value;
                if (!_markers.TryGetValue(playerId, out RectTransform marker) || marker == null)
                {
                    arrived.Add(playerId);
                    continue;
                }
                if (_deadMarkers.Contains(playerId))
                {
                    arrived.Add(playerId);
                    continue;
                }

                chase.Progress = Mathf.Min(chase.TotalLength, chase.Progress + step);
                // Stay on the authored path — no mid-chase collision shove (was stuttering).
                Vector2 pos = ClampToMapCollision(
                    SamplePolyline(chase.Points, chase.SegmentLengths, chase.Progress));
                marker.anchoredPosition = pos;

                int zoneIndex = 0;
                float walked = 0f;
                for (int i = 0; i < chase.SegmentLengths.Length; i++)
                {
                    if (walked + chase.SegmentLengths[i] >= chase.Progress ||
                        i == chase.SegmentLengths.Length - 1)
                    {
                        zoneIndex = i;
                        break;
                    }
                    walked += chase.SegmentLengths[i];
                }
                if (chase.PathZones != null && chase.PathZones.Length > 0)
                {
                    string segmentFloor = ZoneFloor(
                        chase.PathZones[Mathf.Min(zoneIndex, chase.PathZones.Length - 1)]);
                    marker.gameObject.SetActive(
                        string.Equals(segmentFloor, _currentMapFloor, StringComparison.OrdinalIgnoreCase));
                }

                if (chase.Progress >= chase.TotalLength - 0.5f)
                    arrived.Add(playerId);
            }

            for (int i = 0; i < arrived.Count; i++)
            {
                string id = arrived[i];
                if (_generalChases.TryGetValue(id, out GeneralChaseState done))
                {
                    SettlePlayerInZone(id, done.TargetZone);
                    _generalChases.Remove(id);
                }
            }
        }
'''

    text = must_replace(text, old_tick, new_tick)

    # Softer settle: only light resolve, never snap far
    text = must_replace(
        text,
        '''            if (IsGeneralBattleMode())
            {
                // Soft land: keep near path end, only nudge for overlap — no zone-wide shove.
                Vector2 current = marker.anchoredPosition;
                if (Vector2.Distance(current, desired) < GeneralCollisionRadius * 3f)
                    desired = current;
                marker.anchoredPosition = ResolveGeneralPosition(playerId, desired);
            }
''',
        '''            if (IsGeneralBattleMode())
            {
                // Soft land: keep path-end position; tiny overlap nudge only.
                Vector2 current = marker.anchoredPosition;
                if (Vector2.Distance(current, desired) < GeneralCollisionRadius * 4f)
                    desired = current;
                marker.anchoredPosition = SoftSeparateOnce(playerId, ClampToMapCollision(desired));
            }
''',
    )

    # Add SoftSeparateOnce after ResolveGeneralPosition block — find a good insert point
    if "SoftSeparateOnce" not in text:
        text = must_replace(
            text,
            '''            return desired;
        }

        private void SeparateGeneralAgentsNear(string zoneId)
''',
            '''            return desired;
        }

        /// <summary>One light push for overlap at rest — weaker than ResolveGeneralPosition.</summary>
        private Vector2 SoftSeparateOnce(string playerId, Vector2 desired)
        {
            float minDist = GeneralCollisionRadius * 2f;
            foreach (KeyValuePair<string, RectTransform> pair in _markers)
            {
                if (string.Equals(pair.Key, playerId, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (_deadMarkers.Contains(pair.Key) || _generalChases.ContainsKey(pair.Key))
                    continue;
                RectTransform other = pair.Value;
                if (other == null || !other.gameObject.activeSelf)
                    continue;
                Vector2 delta = desired - other.anchoredPosition;
                float dist = delta.magnitude;
                if (dist >= minDist || dist > minDist * 2f)
                    continue;
                Vector2 dir = dist < 0.001f
                    ? new Vector2(Mathf.Cos(PlayerNumber(playerId) * 1.2566f), Mathf.Sin(PlayerNumber(playerId) * 1.2566f))
                    : delta / dist;
                desired += dir * ((minDist - dist) * 0.35f);
            }
            return ClampToMapCollision(desired);
        }

        private void SeparateGeneralAgentsNear(string zoneId)
''',
        )

    if "叫暫停" not in text:
        raise RuntimeError("UTF-8 corruption")
    TARGET.write_text(text, encoding="utf-8")
    print("patched", TARGET)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
