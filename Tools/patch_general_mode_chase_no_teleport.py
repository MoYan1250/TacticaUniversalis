# -*- coding: utf-8 -*-
"""General mode: stop chase teleports — path from visual pos, no short-path snap."""
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"anchor missing:\n{old[:280]!r}")
    return text.replace(old, new, 1)


def main() -> int:
    subprocess.check_call(
        [sys.executable, str(ROOT / "Tools" / "backup_quickmatch.py"), "chase_no_teleport"]
    )
    text = TARGET.read_text(encoding="utf-8")

    if "Path starts at the marker's current visual position" in text:
        print("already patched")
        return 0

    text = must_replace(
        text,
        "        private const float GeneralChaseSpeed = 32f;",
        "        private const float GeneralChaseSpeed = 88f;",
    )

    # After starting a coalesced move, yield at least one frame so Update can tick chases
    # before the event loop floods the next batch. Also wait out this actor's prior chase
    # only when retargeting (handled in SetGeneralMoveTarget via visual start).
    text = must_replace(
        text,
        """                        previousTime = chainEnd;
                        ApplyEvent(evt, true, silentMoves, coalesceDest);
                        SetRoundHud(evt.roundNumber, FormatClock(chainEnd));
                        continue;
                    }
""",
        """                        previousTime = chainEnd;
                        ApplyEvent(evt, true, silentMoves, coalesceDest);
                        SetRoundHud(evt.roundNumber, FormatClock(chainEnd));
                        // Let Update tick chase at least once; avoid same-frame retarget storms.
                        yield return null;
                        continue;
                    }
""",
    )

    # Wait for remaining chases before round end so markers finish walking instead of
    # freezing mid-path until ResetMarkers snaps them.
    text = must_replace(
        text,
        """                else
                    previousTime = evt.roundTime;

                ApplyEvent(evt, animate, silentMoves, coalesceDest);
            }
        }
""",
        """                else
                    previousTime = evt.roundTime;

                if (animate &&
                    IsGeneralBattleMode() &&
                    (evt.type == SimulationEventType.RoundEnded ||
                     evt.type == SimulationEventType.BombExploded ||
                     evt.type == SimulationEventType.BombDefused))
                {
                    float settleCap = 8f;
                    float settleElapsed = 0f;
                    while (_generalChases.Count > 0 && settleElapsed < settleCap)
                    {
                        if (!_paused)
                            settleElapsed += Time.unscaledDeltaTime;
                        yield return null;
                    }
                }

                ApplyEvent(evt, animate, silentMoves, coalesceDest);
            }
        }
""",
    )

    old_set = """        private void SetGeneralMoveTarget(string playerId, string zoneId, bool animate)
        {
            if (string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(zoneId))
                return;
            if (!_markers.TryGetValue(playerId, out RectTransform marker))
                return;
            if (_deadMarkers.Contains(playerId))
                return;

            string from = _playerZones.TryGetValue(playerId, out string current) ? current : zoneId;
            _playerZones[playerId] = zoneId;
            _moveDestinations[playerId] = zoneId;

            if (!animate)
            {
                marker.anchoredPosition = ResolveGeneralPosition(
                    playerId, ClampToMapCollision(LocalZonePosition(zoneId)));
                _generalChases.Remove(playerId);
                _moveDestinations.Remove(playerId);
                RefreshFloorVisibility();
                return;
            }

            if (_generalChases.TryGetValue(playerId, out GeneralChaseState ongoing) &&
                string.Equals(ongoing.TargetZone, zoneId, StringComparison.OrdinalIgnoreCase) &&
                ongoing.Points != null && ongoing.Points.Count > 0)
                return;

            IReadOnlyList<string> path = _pathfinder != null
                ? _pathfinder.FindPath(from, zoneId)
                : Array.Empty<string>();
            if (path == null || path.Count == 0)
                path = new[] { zoneId };

            var points = new List<Vector2>(path.Count);
            for (int i = 0; i < path.Count; i++)
                points.Add(ClampToMapCollision(LocalZonePosition(path[i])));
            points = DensifyPathPoints(points, 18f);

            if (points.Count <= 1)
            {
                marker.anchoredPosition = ResolveGeneralPosition(playerId, points.Count == 1
                    ? points[0]
                    : ClampToMapCollision(LocalZonePosition(zoneId)));
                _generalChases.Remove(playerId);
                _moveDestinations.Remove(playerId);
                RefreshFloorVisibility();
                return;
            }

            float[] seg = new float[points.Count - 1];
            float total = 0f;
            for (int i = 0; i < seg.Length; i++)
            {
                seg[i] = Mathf.Max(0.001f, Vector2.Distance(points[i], points[i + 1]));
                total += seg[i];
            }

            float progress = ProjectOntoPolyline(marker.anchoredPosition, points, seg, total);
            var pathZones = new string[path.Count];
            for (int i = 0; i < path.Count; i++)
                pathZones[i] = path[i];

            _generalChases[playerId] = new GeneralChaseState
            {
                TargetZone = zoneId,
                Points = points,
                SegmentLengths = seg,
                TotalLength = total,
                Progress = progress,
                PathZones = pathZones
            };
        }
"""

    new_set = """        private void SetGeneralMoveTarget(string playerId, string zoneId, bool animate)
        {
            if (string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(zoneId))
                return;
            if (!_markers.TryGetValue(playerId, out RectTransform marker))
                return;
            if (_deadMarkers.Contains(playerId))
                return;

            string from = _playerZones.TryGetValue(playerId, out string current) ? current : zoneId;
            _playerZones[playerId] = zoneId;
            _moveDestinations[playerId] = zoneId;

            // Even silent / non-animated logical updates must not snap the marker —
            // keep walking toward the latest destination at chase speed.
            if (!animate)
            {
                // Fall through to chase setup below (same visual path rules).
            }

            if (_generalChases.TryGetValue(playerId, out GeneralChaseState ongoing) &&
                string.Equals(ongoing.TargetZone, zoneId, StringComparison.OrdinalIgnoreCase) &&
                ongoing.Points != null && ongoing.Points.Count > 0)
                return;

            IReadOnlyList<string> path = _pathfinder != null
                ? _pathfinder.FindPath(from, zoneId)
                : Array.Empty<string>();
            if (path == null || path.Count == 0)
                path = new[] { zoneId };

            // Path starts at the marker's current visual position so retargeting never
            // teleports onto a logical zone center far from where the marker actually is.
            Vector2 visualStart = ClampToMapCollision(marker.anchoredPosition);
            var points = new List<Vector2>(path.Count + 1) { visualStart };
            for (int i = 0; i < path.Count; i++)
            {
                Vector2 p = ClampToMapCollision(LocalZonePosition(path[i]));
                if (Vector2.Distance(points[points.Count - 1], p) < 2.5f)
                    continue;
                points.Add(p);
            }
            points = DensifyPathPoints(points, 18f);

            Vector2 destPos = ClampToMapCollision(LocalZonePosition(zoneId));
            if (points.Count <= 1)
            {
                if (Vector2.Distance(visualStart, destPos) < 3f)
                {
                    marker.anchoredPosition = ResolveGeneralPosition(playerId, destPos);
                    _generalChases.Remove(playerId);
                    _moveDestinations.Remove(playerId);
                    RefreshFloorVisibility();
                    return;
                }
                points = new List<Vector2> { visualStart, destPos };
            }
            else if (Vector2.Distance(points[points.Count - 1], destPos) > 2.5f)
            {
                points.Add(destPos);
            }

            float[] seg = new float[points.Count - 1];
            float total = 0f;
            for (int i = 0; i < seg.Length; i++)
            {
                seg[i] = Mathf.Max(0.001f, Vector2.Distance(points[i], points[i + 1]));
                total += seg[i];
            }

            // Always begin at the visual start we prepended (progress 0) — never project
            // onto a path that begins at a distant logical zone (that caused teleports).
            var pathZones = new string[path.Count];
            for (int i = 0; i < path.Count; i++)
                pathZones[i] = path[i];

            _generalChases[playerId] = new GeneralChaseState
            {
                TargetZone = zoneId,
                Points = points,
                SegmentLengths = seg,
                TotalLength = total,
                Progress = 0f,
                PathZones = pathZones
            };
        }
"""

    text = must_replace(text, old_set, new_set)

    # Cap per-frame chase step so high playbackSpeed cannot cover a whole route in one frame.
    text = must_replace(
        text,
        """            float step = GeneralChaseSpeed * Time.unscaledDeltaTime * Mathf.Max(0.25f, playbackSpeed);
""",
        """            float speedMul = Mathf.Clamp(playbackSpeed, 0.25f, 3f);
            float step = Mathf.Min(
                GeneralChaseSpeed * Time.unscaledDeltaTime * speedMul,
                48f);
""",
    )

    if "擊殺廣播" not in text and "叫暫停" not in text:
        raise RuntimeError("UTF-8 anchors missing after patch")

    TARGET.write_text(text, encoding="utf-8")
    print("patched", TARGET)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
