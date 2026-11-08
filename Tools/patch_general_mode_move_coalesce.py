# -*- coding: utf-8 -*-
"""General mode: coalesce hop Moves into one smooth path; skip kill Relayout shove."""
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
    subprocess.check_call([sys.executable, str(ROOT / "Tools" / "backup_quickmatch.py"), "general_move_coalesce"])
    text = TARGET.read_text(encoding="utf-8")
    if "BuildGeneralMoveCoalesce" in text:
        print("already patched")
        return 0

    text = must_replace(
        text,
        "            int currentRound = slice.Count > 0 ? slice[0].roundNumber : 0;\n"
        "            float previousTime = 0f;\n"
        "            foreach (SimulationEvent evt in slice)\n"
        "            {\n"
        "                if (evt.roundNumber != currentRound)\n"
        "                {\n"
        "                    currentRound = evt.roundNumber;\n"
        "                    previousTime = 0f;\n"
        "                }\n"
        "\n"
        "                if (animate)\n"
        "                {\n"
        "                    float gap = Mathf.Max(0f, evt.roundTime - previousTime);\n"
        "                    float wait = GeneralModePlaybackWait(gap);\n"
        "                    previousTime = evt.roundTime;\n",
        "            int currentRound = slice.Count > 0 ? slice[0].roundNumber : 0;\n"
        "            float previousTime = 0f;\n"
        "            HashSet<int> silentMoves = null;\n"
        "            Dictionary<int, string> coalesceDest = null;\n"
        "            if (IsGeneralBattleMode())\n"
        "                BuildGeneralMoveCoalesce(slice, out silentMoves, out coalesceDest);\n"
        "            foreach (SimulationEvent evt in slice)\n"
        "            {\n"
        "                if (evt.roundNumber != currentRound)\n"
        "                {\n"
        "                    currentRound = evt.roundNumber;\n"
        "                    previousTime = 0f;\n"
        "                }\n"
        "\n"
        "                if (IsGeneralBattleMode() &&\n"
        "                    silentMoves != null &&\n"
        "                    evt.type == SimulationEventType.Move &&\n"
        "                    silentMoves.Contains(evt.sequence))\n"
        "                {\n"
        "                    previousTime = Mathf.Max(previousTime, evt.roundTime);\n"
        "                    ApplyEvent(evt, false, silentMoves, coalesceDest);\n"
        "                    continue;\n"
        "                }\n"
        "\n"
        "                if (animate)\n"
        "                {\n"
        "                    float gap = Mathf.Max(0f, evt.roundTime - previousTime);\n"
        "                    if (IsGeneralBattleMode() &&\n"
        "                        evt.type == SimulationEventType.Move &&\n"
        "                        coalesceDest != null &&\n"
        "                        coalesceDest.ContainsKey(evt.sequence) &&\n"
        "                        silentMoves != null)\n"
        "                    {\n"
        "                        float chainEnd = evt.roundTime;\n"
        "                        for (int k = 0; k < slice.Count; k++)\n"
        "                        {\n"
        "                            SimulationEvent later = slice[k];\n"
        "                            if (later.sequence <= evt.sequence) continue;\n"
        "                            if (later.type == SimulationEventType.Move &&\n"
        "                                string.Equals(later.actorId, evt.actorId, StringComparison.OrdinalIgnoreCase) &&\n"
        "                                silentMoves.Contains(later.sequence))\n"
        "                                chainEnd = later.roundTime;\n"
        "                            else if (later.type == SimulationEventType.Move &&\n"
        "                                     string.Equals(later.actorId, evt.actorId, StringComparison.OrdinalIgnoreCase))\n"
        "                                break;\n"
        "                            else if (later.type != SimulationEventType.StatusUpdated &&\n"
        "                                     later.type != SimulationEventType.Contact &&\n"
        "                                     later.type != SimulationEventType.TraitTriggered)\n"
        "                                break;\n"
        "                        }\n"
        "                        gap = Mathf.Max(0f, chainEnd - previousTime);\n"
        "                        previousTime = chainEnd;\n"
        "                    }\n"
        "                    else\n"
        "                        previousTime = evt.roundTime;\n"
        "                    float wait = GeneralModePlaybackWait(gap);\n",
    )

    # Remove duplicate previousTime assignment if left in wait block - the old code had
    # previousTime = evt.roundTime after wait=; we replaced up to wait= line. Check remaining.

    text = must_replace(
        text,
        "                ApplyEvent(evt, animate);\n"
        "            }\n"
        "        }\n"
        "\n"
        "        private void SyncEconomyFromState()\n",
        "                ApplyEvent(evt, animate, silentMoves, coalesceDest);\n"
        "            }\n"
        "        }\n"
        "\n"
        "        private static void BuildGeneralMoveCoalesce(\n"
        "            List<SimulationEvent> slice,\n"
        "            out HashSet<int> silentMoves,\n"
        "            out Dictionary<int, string> coalesceDest)\n"
        "        {\n"
        "            silentMoves = new HashSet<int>();\n"
        "            coalesceDest = new Dictionary<int, string>();\n"
        "            for (int i = 0; i < slice.Count; i++)\n"
        "            {\n"
        "                SimulationEvent evt = slice[i];\n"
        "                if (evt.type != SimulationEventType.Move || string.IsNullOrEmpty(evt.actorId))\n"
        "                    continue;\n"
        "                if (silentMoves.Contains(evt.sequence))\n"
        "                    continue;\n"
        "\n"
        "                string finalZone = evt.zoneId;\n"
        "                var intermediates = new List<int>();\n"
        "                for (int j = i + 1; j < slice.Count; j++)\n"
        "                {\n"
        "                    SimulationEvent later = slice[j];\n"
        "                    if (later.type == SimulationEventType.Move &&\n"
        "                        string.Equals(later.actorId, evt.actorId, StringComparison.OrdinalIgnoreCase))\n"
        "                    {\n"
        "                        finalZone = later.zoneId;\n"
        "                        intermediates.Add(later.sequence);\n"
        "                        continue;\n"
        "                    }\n"
        "                    if (later.type == SimulationEventType.StatusUpdated ||\n"
        "                        later.type == SimulationEventType.Contact ||\n"
        "                        later.type == SimulationEventType.TraitTriggered)\n"
        "                        continue;\n"
        "                    break;\n"
        "                }\n"
        "\n"
        "                coalesceDest[evt.sequence] = finalZone;\n"
        "                foreach (int seq in intermediates)\n"
        "                    silentMoves.Add(seq);\n"
        "            }\n"
        "        }\n"
        "\n"
        "        private void SyncEconomyFromState()\n",
    )

    text = must_replace(
        text,
        "        private void ApplyEvent(SimulationEvent evt, bool animate)\n"
        "        {\n",
        "        private void ApplyEvent(\n"
        "            SimulationEvent evt,\n"
        "            bool animate,\n"
        "            HashSet<int> silentMoves = null,\n"
        "            Dictionary<int, string> coalesceDest = null)\n"
        "        {\n",
    )

    text = must_replace(
        text,
        "                case SimulationEventType.Move:\n"
        "                    // Non-blocking: multiple players can path at once.\n"
        "                    MoveMarkerAlongPath(evt.actorId, evt.zoneId, animate);\n"
        "                    break;\n",
        "                case SimulationEventType.Move:\n"
        "                    if (silentMoves != null && silentMoves.Contains(evt.sequence))\n"
        "                    {\n"
        "                        if (!string.IsNullOrEmpty(evt.actorId) && !string.IsNullOrEmpty(evt.zoneId))\n"
        "                            _playerZones[evt.actorId] = evt.zoneId;\n"
        "                        break;\n"
        "                    }\n"
        "                    {\n"
        "                        string dest = evt.zoneId;\n"
        "                        if (coalesceDest != null &&\n"
        "                            coalesceDest.TryGetValue(evt.sequence, out string coalesced) &&\n"
        "                            !string.IsNullOrEmpty(coalesced))\n"
        "                            dest = coalesced;\n"
        "                        MoveMarkerAlongPath(evt.actorId, dest, animate);\n"
        "                    }\n"
        "                    break;\n",
    )

    text = must_replace(
        text,
        "                        string deathZone = _playerZones.TryGetValue(evt.targetId, out string z) ? z : evt.zoneId;\n"
        "                        victim.gameObject.SetActive(false);\n"
        "                        RelayoutZone(deathZone, animate, 0.18f);\n"
        "                    }\n",
        "                        victim.gameObject.SetActive(false);\n"
        "                        if (!IsGeneralBattleMode())\n"
        "                        {\n"
        "                            string deathZone = _playerZones.TryGetValue(evt.targetId, out string z) ? z : evt.zoneId;\n"
        "                            RelayoutZone(deathZone, animate, 0.18f);\n"
        "                        }\n"
        "                    }\n",
    )

    text = must_replace(
        text,
        "        private static float GeneralModeMoveDuration(int pathCount)\n"
        "        {\n"
        "            // Match Domain MoveIntervalSeconds (~0.45) so hops finish before the next.\n"
        "            int hops = Mathf.Max(1, pathCount - 1);\n"
        "            return Mathf.Clamp(0.42f * hops, 0.38f, 0.85f);\n"
        "        }\n",
        "        private static float GeneralModeMoveDuration(int pathCount)\n"
        "        {\n"
        "            int hops = Mathf.Max(1, pathCount - 1);\n"
        "            return Mathf.Clamp(0.16f * hops, 0.20f, 1.35f);\n"
        "        }\n",
    )

    if "叫暫停" not in text and "KillFeed" not in text:
        raise RuntimeError("UTF-8 corruption suspected")

    TARGET.write_text(text, encoding="utf-8")
    print("patched", TARGET)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
