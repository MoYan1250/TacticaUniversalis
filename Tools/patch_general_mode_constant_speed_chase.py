# -*- coding: utf-8 -*-
"""General mode: constant-speed chase movement (decouple from event-driven duration)."""
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"anchor missing:\n{old[:220]!r}")
    return text.replace(old, new, 1)


def main() -> int:
    subprocess.check_call(
        [sys.executable, str(ROOT / "Tools" / "backup_quickmatch.py"), "constant_speed_chase"]
    )
    text = TARGET.read_text(encoding="utf-8")
    if "TickGeneralChases" in text and "SetGeneralMoveTarget" in text:
        print("already patched")
        return 0

    # --- fields ---
    text = must_replace(
        text,
        """        /// <summary>Visual marker dot diameter in map-local pixels.</summary>
        private const float MarkerDotDiameter = 28f;
        /// <summary>Collision radius = 50% of visual circle diameter / 2.</summary>
        private const float GeneralCollisionRadius = MarkerDotDiameter * 0.25f;
""",
        """        /// <summary>Visual marker dot diameter in map-local pixels.</summary>
        private const float MarkerDotDiameter = 28f;
        /// <summary>Collision radius = 50% of visual circle diameter / 2.</summary>
        private const float GeneralCollisionRadius = MarkerDotDiameter * 0.25f;
        /// <summary>General-mode visual travel speed in map-local pixels per second.</summary>
        private const float GeneralChaseSpeed = 140f;
        private readonly Dictionary<string, GeneralChaseState> _generalChases =
            new Dictionary<string, GeneralChaseState>(StringComparer.OrdinalIgnoreCase);

        private sealed class GeneralChaseState
        {
            public string TargetZone;
            public List<Vector2> Points;
            public float[] SegmentLengths;
            public float TotalLength;
            public float Progress;
            public string[] PathZones;
        }
""",
    )

    # --- PlayEventRange: Move never blocks clock ---
    text = must_replace(
        text,
        """                if (animate)
                {
                    float gap = Mathf.Max(0f, evt.roundTime - previousTime);
                    bool coalescedMove = IsGeneralBattleMode() &&
                        evt.type == SimulationEventType.Move &&
                        coalesceDest != null &&
                        coalesceDest.ContainsKey(evt.sequence) &&
                        silentMoves != null;

                    if (coalescedMove)
                    {
                        float chainEnd = evt.roundTime;
                        for (int k = 0; k < slice.Count; k++)
                        {
                            SimulationEvent later = slice[k];
                            if (later.sequence <= evt.sequence) continue;
                            if (later.type == SimulationEventType.Move &&
                                string.Equals(later.actorId, evt.actorId, StringComparison.OrdinalIgnoreCase) &&
                                silentMoves.Contains(later.sequence))
                                chainEnd = later.roundTime;
                            else if (later.type == SimulationEventType.Move &&
                                     string.Equals(later.actorId, evt.actorId, StringComparison.OrdinalIgnoreCase))
                                break;
                            else if (later.type != SimulationEventType.StatusUpdated &&
                                     later.type != SimulationEventType.Contact &&
                                     later.type != SimulationEventType.TraitTriggered)
                                break;
                        }
                        gap = Mathf.Max(0f, chainEnd - previousTime);
                        previousTime = chainEnd;

                        string dest = evt.zoneId;
                        if (coalesceDest.TryGetValue(evt.sequence, out string coalesced) &&
                            !string.IsNullOrEmpty(coalesced))
                            dest = coalesced;

                        // Long path tween runs in parallel; only stagger briefly so other agents start too.
                        float duration = EstimateGeneralMoveDuration(evt.actorId, dest, gap);
                        _currentRound = Mathf.Max(1, evt.roundNumber);
                        MoveMarkerAlongPath(evt.actorId, dest, true, duration);
                        float stagger = Mathf.Clamp(
                            gap / Mathf.Max(0.25f, playbackSpeed) * 0.1f, 0.02f, 0.14f);
                        float elapsed = 0f;
                        float shown = chainEnd - gap;
                        while (elapsed < stagger)
                        {
                            if (!_paused)
                            {
                                elapsed += Time.unscaledDeltaTime;
                                float u = Mathf.Clamp01(elapsed / Mathf.Max(0.001f, stagger));
                                SetRoundHud(evt.roundNumber, FormatClock(shown + gap * u));
                            }
                            yield return null;
                        }
                        SetRoundHud(evt.roundNumber, FormatClock(chainEnd));
                        while (_paused)
                            yield return null;
                        continue;
                    }

                    previousTime = evt.roundTime;
                    float idleWait = GeneralModePlaybackWait(gap);
                    float idleElapsed = 0f;
                    float idleShown = evt.roundTime - gap;
                    while (idleElapsed < idleWait)
                    {
                        if (!_paused)
                        {
                            idleElapsed += Time.unscaledDeltaTime;
                            if (IsGeneralBattleMode() && idleWait > 0.001f && gap > 0.001f)
                            {
                                float u = Mathf.Clamp01(idleElapsed / idleWait);
                                SetRoundHud(evt.roundNumber, FormatClock(idleShown + gap * u));
                            }
                        }
                        yield return null;
                    }
                    while (_paused)
                        yield return null;
                }
                else
                    previousTime = evt.roundTime;

                ApplyEvent(evt, animate, silentMoves, coalesceDest);
""",
        """                if (animate)
                {
                    float gap = Mathf.Max(0f, evt.roundTime - previousTime);

                    // General mode moves: set chase target immediately, never wait on travel duration.
                    if (IsGeneralBattleMode() && evt.type == SimulationEventType.Move)
                    {
                        float chainEnd = evt.roundTime;
                        if (coalesceDest != null &&
                            coalesceDest.ContainsKey(evt.sequence) &&
                            silentMoves != null)
                        {
                            for (int k = 0; k < slice.Count; k++)
                            {
                                SimulationEvent later = slice[k];
                                if (later.sequence <= evt.sequence) continue;
                                if (later.type == SimulationEventType.Move &&
                                    string.Equals(later.actorId, evt.actorId, StringComparison.OrdinalIgnoreCase) &&
                                    silentMoves.Contains(later.sequence))
                                    chainEnd = later.roundTime;
                                else if (later.type == SimulationEventType.Move &&
                                         string.Equals(later.actorId, evt.actorId, StringComparison.OrdinalIgnoreCase))
                                    break;
                                else if (later.type != SimulationEventType.StatusUpdated &&
                                         later.type != SimulationEventType.Contact &&
                                         later.type != SimulationEventType.TraitTriggered)
                                    break;
                            }
                        }
                        previousTime = chainEnd;
                        ApplyEvent(evt, true, silentMoves, coalesceDest);
                        SetRoundHud(evt.roundNumber, FormatClock(chainEnd));
                        continue;
                    }

                    previousTime = evt.roundTime;
                    float idleWait = GeneralModePlaybackWait(gap);
                    float idleElapsed = 0f;
                    float idleShown = evt.roundTime - gap;
                    while (idleElapsed < idleWait)
                    {
                        if (!_paused)
                        {
                            idleElapsed += Time.unscaledDeltaTime;
                            if (IsGeneralBattleMode() && idleWait > 0.001f && gap > 0.001f)
                            {
                                float u = Mathf.Clamp01(idleElapsed / idleWait);
                                SetRoundHud(evt.roundNumber, FormatClock(idleShown + gap * u));
                            }
                        }
                        yield return null;
                    }
                    while (_paused)
                        yield return null;
                }
                else
                    previousTime = evt.roundTime;

                ApplyEvent(evt, animate, silentMoves, coalesceDest);
""",
    )

    # --- MoveMarkerAlongPath: general mode → chase ---
    text = must_replace(
        text,
        """        private void MoveMarkerAlongPath(string playerId, string zoneId, bool animate)
        {
            MoveMarkerAlongPath(playerId, zoneId, animate, -1f);
        }

        private void MoveMarkerAlongPath(string playerId, string zoneId, bool animate, float durationOverride)
        {
            if (string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(zoneId))
                return;
            if (!_markers.TryGetValue(playerId, out RectTransform marker))
                return;

            string from = _playerZones.TryGetValue(playerId, out string current) ? current : zoneId;
            if (string.Equals(from, zoneId, StringComparison.OrdinalIgnoreCase))
                return;

            // Already tweening to this zone — do not restart (avoids hop jitter).
            if (IsGeneralBattleMode() &&
                _moveDestinations.TryGetValue(playerId, out string pending) &&
                string.Equals(pending, zoneId, StringComparison.OrdinalIgnoreCase) &&
                _moveRoutines.TryGetValue(playerId, out Coroutine busy) && busy != null)
            {
                _playerZones[playerId] = zoneId;
                return;
            }

            IReadOnlyList<string> path = _pathfinder.FindPath(from, zoneId);
            _playerZones[playerId] = zoneId;

            Vector2 endOffset = ZoneOccupantOffset(playerId, zoneId);
            if (!animate || path.Count == 0)
            {
                Vector2 landed = LocalZonePosition(zoneId) + endOffset;
                marker.anchoredPosition = IsGeneralBattleMode()
                    ? ResolveGeneralPosition(playerId, landed)
                    : ClampToMap(landed);
                _moveDestinations.Remove(playerId);
                if (!IsGeneralBattleMode())
                {
                    if (!string.Equals(from, zoneId, StringComparison.OrdinalIgnoreCase))
                        RelayoutZone(from, false, 0f);
                    RelayoutZone(zoneId, false, 0f);
                }
                return;
            }

            float duration = durationOverride > 0.01f
                ? durationOverride
                : (IsGeneralBattleMode()
                    ? GeneralModeMoveDuration(path.Count)
                    : Mathf.Clamp(0.28f * Mathf.Max(1, path.Count - 1), 0.35f, 1.6f));
            // General mode: never shove teammates mid-path; quick mode keeps pack layout.
            if (!IsGeneralBattleMode() &&
                !string.Equals(from, zoneId, StringComparison.OrdinalIgnoreCase))
                RelayoutZone(from, true, 0.18f);
            _moveDestinations[playerId] = zoneId;
            StartMoveRoutine(playerId, FollowPathRoutine(playerId, path, endOffset, duration));
        }

        private float GeneralModeMoveDuration(int pathCount, float simGapSeconds = -1f)
        {
            int hops = Mathf.Max(1, pathCount - 1);
            float perHop = BehaviorTreeRoundSimulator.MoveIntervalSeconds;
            float fromHops = hops * perHop;
            // Prefer the coalesced sim span so doorway→exit fills the whole travel window.
            float raw = simGapSeconds > 0.05f ? Mathf.Max(fromHops, simGapSeconds * 0.92f) : fromHops;
            float speed = Mathf.Max(0.25f, playbackSpeed);
            return Mathf.Clamp(raw / speed, 0.14f, 16f);
        }

        private float EstimateGeneralMoveDuration(string playerId, string zoneId, float simGapSeconds)
        {
            if (string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(zoneId) || _pathfinder == null)
                return Mathf.Clamp(Mathf.Max(0.14f, simGapSeconds) / Mathf.Max(0.25f, playbackSpeed), 0.14f, 16f);
            string from = _playerZones.TryGetValue(playerId, out string current) ? current : zoneId;
            IReadOnlyList<string> path = _pathfinder.FindPath(from, zoneId);
            int count = path != null && path.Count > 0 ? path.Count : 2;
            return GeneralModeMoveDuration(count, simGapSeconds);
        }
""",
        """        private void MoveMarkerAlongPath(string playerId, string zoneId, bool animate)
        {
            if (IsGeneralBattleMode())
            {
                SetGeneralMoveTarget(playerId, zoneId, animate);
                return;
            }

            if (string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(zoneId))
                return;
            if (!_markers.TryGetValue(playerId, out RectTransform marker))
                return;

            string from = _playerZones.TryGetValue(playerId, out string current) ? current : zoneId;
            if (string.Equals(from, zoneId, StringComparison.OrdinalIgnoreCase))
                return;

            IReadOnlyList<string> path = _pathfinder.FindPath(from, zoneId);
            _playerZones[playerId] = zoneId;

            Vector2 endOffset = ZoneOccupantOffset(playerId, zoneId);
            if (!animate || path.Count == 0)
            {
                marker.anchoredPosition = ClampToMap(LocalZonePosition(zoneId) + endOffset);
                _moveDestinations.Remove(playerId);
                if (!string.Equals(from, zoneId, StringComparison.OrdinalIgnoreCase))
                    RelayoutZone(from, false, 0f);
                RelayoutZone(zoneId, false, 0f);
                return;
            }

            float duration = Mathf.Clamp(0.28f * Mathf.Max(1, path.Count - 1), 0.35f, 1.6f);
            if (!string.Equals(from, zoneId, StringComparison.OrdinalIgnoreCase))
                RelayoutZone(from, true, 0.18f);
            _moveDestinations[playerId] = zoneId;
            StartMoveRoutine(playerId, FollowPathRoutine(playerId, path, endOffset, duration));
        }

        private void SetGeneralMoveTarget(string playerId, string zoneId, bool animate)
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

        private static float ProjectOntoPolyline(
            Vector2 pos, List<Vector2> points, float[] seg, float total)
        {
            float bestDist = float.MaxValue;
            float bestProgress = 0f;
            float walked = 0f;
            for (int i = 0; i < seg.Length; i++)
            {
                Vector2 a = points[i];
                Vector2 b = points[i + 1];
                Vector2 ab = b - a;
                float abLenSq = ab.sqrMagnitude;
                float t = abLenSq < 0.0001f ? 0f : Mathf.Clamp01(Vector2.Dot(pos - a, ab) / abLenSq);
                Vector2 proj = a + ab * t;
                float d = Vector2.Distance(pos, proj);
                if (d < bestDist)
                {
                    bestDist = d;
                    bestProgress = walked + seg[i] * t;
                }
                walked += seg[i];
            }
            return Mathf.Clamp(bestProgress, 0f, total);
        }

        private void TickGeneralChases()
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

        private static Vector2 SamplePolyline(List<Vector2> points, float[] seg, float distance)
        {
            float walked = 0f;
            for (int i = 0; i < seg.Length; i++)
            {
                float len = seg[i];
                if (walked + len >= distance || i == seg.Length - 1)
                {
                    float local = Mathf.Clamp01((distance - walked) / Mathf.Max(0.001f, len));
                    return Vector2.Lerp(points[i], points[i + 1], local);
                }
                walked += len;
            }
            return points[points.Count - 1];
        }
""",
    )

    # --- StopAllMovement clears chases ---
    text = must_replace(
        text,
        """            _moveRoutines.Clear();
            _moveDestinations.Clear();
        }
""",
        """            _moveRoutines.Clear();
            _moveDestinations.Clear();
            _generalChases.Clear();
        }
""",
    )

    # --- Kill clears chase ---
    text = must_replace(
        text,
        """                    if (!string.IsNullOrEmpty(evt.targetId))
                    {
                        _deadMarkers.Add(evt.targetId);
                        SetMarkerVitality(evt.targetId, 0, 0);
                    }
                    if (!string.IsNullOrEmpty(evt.targetId) && _markers.TryGetValue(evt.targetId, out RectTransform victim))
                    {
                        victim.gameObject.SetActive(false);
""",
        """                    if (!string.IsNullOrEmpty(evt.targetId))
                    {
                        _deadMarkers.Add(evt.targetId);
                        SetMarkerVitality(evt.targetId, 0, 0);
                        _generalChases.Remove(evt.targetId);
                        _moveDestinations.Remove(evt.targetId);
                    }
                    if (!string.IsNullOrEmpty(evt.targetId) && _markers.TryGetValue(evt.targetId, out RectTransform victim))
                    {
                        victim.gameObject.SetActive(false);
""",
    )

    # --- Update ticks chases ---
    text = must_replace(
        text,
        """        private void Update()
        {
            PruneExpiredKillFeed();

            if (Input.GetKeyDown(KeyCode.Space) &&
""",
        """        private void Update()
        {
            TickGeneralChases();
            PruneExpiredKillFeed();

            if (Input.GetKeyDown(KeyCode.Space) &&
""",
    )

    # Soften idle wait comment — moves no longer use Estimate*
    text = must_replace(
        text,
        """            // Only squash long idle/timeout gaps. Move chains use EstimateGeneralMoveDuration.
""",
        """            // Only squash long idle/timeout gaps. Moves chase independently at GeneralChaseSpeed.
""",
    )

    if "叫暫停" not in text:
        raise RuntimeError("UTF-8 corruption suspected")
    if "TickGeneralChases" not in text or "SetGeneralMoveTarget" not in text:
        raise RuntimeError("chase APIs missing after patch")

    TARGET.write_text(text, encoding="utf-8")
    print("patched", TARGET)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
