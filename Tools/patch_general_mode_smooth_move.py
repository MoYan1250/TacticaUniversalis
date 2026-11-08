# -*- coding: utf-8 -*-
"""General mode: continuous path playback synced to hop duration; no settle shove jitter."""
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"anchor missing: {old[:200]!r}")
    return text.replace(old, new, 1)


def main() -> int:
    subprocess.check_call(
        [sys.executable, str(ROOT / "Tools" / "backup_quickmatch.py"), "smooth_continuous_move"]
    )
    text = TARGET.read_text(encoding="utf-8")
    if "EstimateGeneralMoveDuration" in text and "Start move first" in text:
        print("already patched")
        return 0

    # PlayEventRange: for coalesced moves, start tween then wait for its duration (no idle + zip)
    text = must_replace(
        text,
        '''                if (animate)
                {
                    float gap = Mathf.Max(0f, evt.roundTime - previousTime);
                    if (IsGeneralBattleMode() &&
                        evt.type == SimulationEventType.Move &&
                        coalesceDest != null &&
                        coalesceDest.ContainsKey(evt.sequence) &&
                        silentMoves != null)
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
                    }
                    else
                        previousTime = evt.roundTime;
                    float wait = GeneralModePlaybackWait(gap);
                    float elapsed = 0f;
                    float shown = evt.roundTime - gap;
                    while (elapsed < wait)
                    {
                        if (!_paused)
                        {
                            elapsed += Time.unscaledDeltaTime;
                            if (IsGeneralBattleMode() && wait > 0.001f && gap > 0.001f)
                            {
                                float u = Mathf.Clamp01(elapsed / wait);
                                SetRoundHud(evt.roundNumber, FormatClock(shown + gap * u));
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
''',
        '''                if (animate)
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

                        // Start move first so the marker travels during the wait (no idle then zip).
                        float wait = EstimateGeneralMoveDuration(evt.actorId, dest, gap);
                        // Apply move with the same duration used for the wait clock.
                        _currentRound = Mathf.Max(1, evt.roundNumber);
                        SetRoundHud(evt.roundNumber, FormatClock(evt.roundTime));
                        MoveMarkerAlongPath(evt.actorId, dest, true, wait);
                        float elapsed = 0f;
                        float shown = chainEnd - gap;
                        while (elapsed < wait)
                        {
                            if (!_paused)
                            {
                                elapsed += Time.unscaledDeltaTime;
                                if (wait > 0.001f && gap > 0.001f)
                                {
                                    float u = Mathf.Clamp01(elapsed / wait);
                                    SetRoundHud(evt.roundNumber, FormatClock(shown + gap * u));
                                }
                            }
                            yield return null;
                        }
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
''',
    )

    # Duration: match sim hop pacing; insert visual midpoints for long segments
    text = must_replace(
        text,
        '''        private static float GeneralModeMoveDuration(int pathCount)
        {
            int hops = Mathf.Max(1, pathCount - 1);
            return Mathf.Clamp(0.16f * hops, 0.20f, 1.35f);
        }
''',
        '''        private float GeneralModeMoveDuration(int pathCount, float simGapSeconds = -1f)
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
''',
    )

    # MoveMarkerAlongPath: add durationOverride overload
    text = must_replace(
        text,
        '''        private void MoveMarkerAlongPath(string playerId, string zoneId, bool animate)
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

            float duration = IsGeneralBattleMode()
                ? GeneralModeMoveDuration(path.Count)
                : Mathf.Clamp(0.28f * Mathf.Max(1, path.Count - 1), 0.35f, 1.6f);
            // General mode: never shove teammates mid-path; quick mode keeps pack layout.
            if (!IsGeneralBattleMode() &&
                !string.Equals(from, zoneId, StringComparison.OrdinalIgnoreCase))
                RelayoutZone(from, true, 0.18f);
            _moveDestinations[playerId] = zoneId;
            StartMoveRoutine(playerId, FollowPathRoutine(playerId, path, endOffset, duration));
        }
''',
        '''        private void MoveMarkerAlongPath(string playerId, string zoneId, bool animate)
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
''',
    )

    # FollowPathRoutine: densify visual points along long segments
    text = must_replace(
        text,
        '''            var points = new List<Vector2>();
            for (int i = 0; i < path.Count; i++)
            {
                Vector2 p = LocalZonePosition(path[i]);
                if (i == path.Count - 1)
                    p += endOffset;
                points.Add(IsGeneralBattleMode() ? ClampToMapCollision(p) : ClampToMap(p));
            }
''',
        '''            var points = new List<Vector2>();
            for (int i = 0; i < path.Count; i++)
            {
                Vector2 p = LocalZonePosition(path[i]);
                if (i == path.Count - 1)
                    p += endOffset;
                points.Add(IsGeneralBattleMode() ? ClampToMapCollision(p) : ClampToMap(p));
            }
            if (IsGeneralBattleMode())
                points = DensifyPathPoints(points, 18f);
''',
    )

    # Soft settle: resolve self only, no zone-wide shove (was causing end-of-path jitter)
    text = must_replace(
        text,
        '''        private void SettlePlayerInZone(string playerId, string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId) || !_markers.TryGetValue(playerId, out RectTransform marker))
                return;
            Vector2 desired = LocalZonePosition(zoneId) + ZoneOccupantOffset(playerId, zoneId);
            marker.anchoredPosition = IsGeneralBattleMode()
                ? ResolveGeneralPosition(playerId, desired)
                : ClampToMap(desired);
            _moveDestinations.Remove(playerId);
            if (IsGeneralBattleMode())
                SeparateGeneralAgentsNear(zoneId);
            else
                RelayoutZone(zoneId, false, 0f);
            RefreshFloorVisibility();
        }
''',
        '''        private void SettlePlayerInZone(string playerId, string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId) || !_markers.TryGetValue(playerId, out RectTransform marker))
                return;
            Vector2 desired = LocalZonePosition(zoneId) + ZoneOccupantOffset(playerId, zoneId);
            if (IsGeneralBattleMode())
            {
                // Soft land: keep near path end, only nudge for overlap — no zone-wide shove.
                Vector2 current = marker.anchoredPosition;
                if (Vector2.Distance(current, desired) < GeneralCollisionRadius * 3f)
                    desired = current;
                marker.anchoredPosition = ResolveGeneralPosition(playerId, desired);
            }
            else
            {
                marker.anchoredPosition = ClampToMap(desired);
                RelayoutZone(zoneId, false, 0f);
            }
            _moveDestinations.Remove(playerId);
            RefreshFloorVisibility();
        }

        /// <summary>Insert samples so long nav segments glide instead of leaping.</summary>
        private static List<Vector2> DensifyPathPoints(List<Vector2> source, float maxSegment)
        {
            if (source == null || source.Count < 2 || maxSegment < 4f)
                return source;
            var dense = new List<Vector2>(source.Count * 2);
            dense.Add(source[0]);
            for (int i = 0; i < source.Count - 1; i++)
            {
                Vector2 a = source[i];
                Vector2 b = source[i + 1];
                float dist = Vector2.Distance(a, b);
                int steps = Mathf.Max(1, Mathf.CeilToInt(dist / maxSegment));
                for (int s = 1; s <= steps; s++)
                    dense.Add(Vector2.Lerp(a, b, s / (float)steps));
            }
            return dense;
        }
''',
    )

    # Idle wait: only compress true idle gaps (timeout), keep short gaps for non-move events
    text = must_replace(
        text,
        '''        private float GeneralModePlaybackWait(float roundTimeGap)
        {
            float speed = Mathf.Max(0.25f, playbackSpeed);
            float gap = Mathf.Max(0f, roundTimeGap);
            if (!IsGeneralBattleMode())
                return gap / speed;

            // Compress long idle gaps (timeout / heartbeat), keep mid gaps for move chains.
            if (gap >= 2.5f)
                gap = 0.18f;
            else if (gap >= 0.9f)
                gap = Mathf.Lerp(0.55f, 0.22f, Mathf.InverseLerp(0.9f, 2.5f, gap));
            return gap / speed;
        }
''',
        '''        private float GeneralModePlaybackWait(float roundTimeGap)
        {
            float speed = Mathf.Max(0.25f, playbackSpeed);
            float gap = Mathf.Max(0f, roundTimeGap);
            if (!IsGeneralBattleMode())
                return gap / speed;

            // Only squash long idle/timeout gaps. Move chains use EstimateGeneralMoveDuration.
            if (gap >= 4f)
                gap = 0.35f;
            else if (gap >= 1.5f)
                gap = Mathf.Lerp(0.85f, 0.35f, Mathf.InverseLerp(1.5f, 4f, gap));
            return gap / speed;
        }
''',
    )

    if "叫暫停" not in text:
        raise RuntimeError("UTF-8 corruption suspected")

    TARGET.write_text(text, encoding="utf-8")
    print("patched", TARGET)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
