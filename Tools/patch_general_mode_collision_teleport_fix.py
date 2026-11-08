# -*- coding: utf-8 -*-
"""Fix general-mode collision teleport (unit/0.001 blow-up) and soften mid-path pushes."""
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
    subprocess.check_call([sys.executable, str(ROOT / "Tools" / "backup_quickmatch.py"), "collision_teleport_fix"])
    text = TARGET.read_text(encoding="utf-8")

    # Fix ResolveGeneralPosition: normalize properly; cap push; ignore far agents
    old_resolve = '''        private Vector2 ResolveGeneralPosition(string playerId, Vector2 desired)
        {
            desired = ClampToMapCollision(desired);
            float minDist = GeneralCollisionRadius * 2f;
            for (int iter = 0; iter < 5; iter++)
            {
                foreach (KeyValuePair<string, RectTransform> pair in _markers)
                {
                    if (string.Equals(pair.Key, playerId, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (_deadMarkers.Contains(pair.Key))
                        continue;
                    RectTransform other = pair.Value;
                    if (other == null || !other.gameObject.activeSelf)
                        continue;

                    Vector2 delta = desired - other.anchoredPosition;
                    float dist = delta.magnitude;
                    if (dist >= minDist)
                        continue;
                    if (dist < 0.001f)
                    {
                        float ang = PlayerNumber(playerId) * 1.2566f;
                        delta = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                        dist = 0.001f;
                    }
                    desired += delta / dist * (minDist - dist);
                }
                desired = ClampToMapCollision(desired);
            }
            return desired;
        }
'''

    new_resolve = '''        private Vector2 ResolveGeneralPosition(string playerId, Vector2 desired)
        {
            desired = ClampToMapCollision(desired);
            float minDist = GeneralCollisionRadius * 2f;
            // Only separate against nearby agents — far teammates must not shove across the map.
            float consider = minDist * 3f;
            for (int iter = 0; iter < 5; iter++)
            {
                foreach (KeyValuePair<string, RectTransform> pair in _markers)
                {
                    if (string.Equals(pair.Key, playerId, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (_deadMarkers.Contains(pair.Key))
                        continue;
                    RectTransform other = pair.Value;
                    if (other == null || !other.gameObject.activeSelf)
                        continue;

                    Vector2 delta = desired - other.anchoredPosition;
                    float dist = delta.magnitude;
                    if (dist >= minDist || dist > consider)
                        continue;
                    Vector2 dir;
                    if (dist < 0.001f)
                    {
                        float ang = PlayerNumber(playerId) * 1.2566f;
                        dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                    }
                    else
                        dir = delta / dist;
                    // Never scale a unit vector by 1/dist (old bug teleported markers to corners).
                    desired += dir * (minDist - dist);
                }
                desired = ClampToMapCollision(desired);
            }
            return desired;
        }
'''

    if old_resolve not in text:
        raise RuntimeError("ResolveGeneralPosition block missing")
    text = text.replace(old_resolve, new_resolve, 1)

    # Mid-path: clamp only — collision resolve on settle/spawn prevents corner teleport flicker
    text = must_replace(
        text,
        '''                        Vector2 lerped = Vector2.Lerp(points[i], points[i + 1], local);
                        marker.anchoredPosition = IsGeneralBattleMode()
                            ? ResolveGeneralPosition(playerId, lerped)
                            : lerped;
''',
        '''                        Vector2 lerped = Vector2.Lerp(points[i], points[i + 1], local);
                        marker.anchoredPosition = IsGeneralBattleMode()
                            ? ClampToMapCollision(lerped)
                            : lerped;
''',
    )

    text = must_replace(
        text,
        '''            if (points.Count == 1)
            {
                marker.anchoredPosition = IsGeneralBattleMode()
                    ? ResolveGeneralPosition(playerId, points[0])
                    : points[0];
                SettlePlayerInZone(playerId, destination);
''',
        '''            if (points.Count == 1)
            {
                marker.anchoredPosition = IsGeneralBattleMode()
                    ? ClampToMapCollision(points[0])
                    : points[0];
                SettlePlayerInZone(playerId, destination);
''',
    )

    if "叫暫停" not in text:
        raise RuntimeError("UTF-8 corruption suspected")

    TARGET.write_text(text, encoding="utf-8")
    print("patched", TARGET)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
