# -*- coding: utf-8 -*-
"""Wire EventType.Utility smoke/fire markers on QuickMatch map."""
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
        [sys.executable, str(ROOT / "Tools" / "backup_quickmatch.py"), "utility_smoke"]
    )
    text = TARGET.read_text(encoding="utf-8")
    if "ShowUtilityEffect" in text and "_utilityMarkers" in text:
        print("already patched")
        return 0

    text = must_replace(
        text,
        "        private string _droppedBombZoneId;\n",
        "        private string _droppedBombZoneId;\n"
        "        private readonly List<UtilityMapMarker> _utilityMarkers = new List<UtilityMapMarker>();\n"
        "        private Transform _utilityMarkerRoot;\n",
    )

    text = must_replace(
        text,
        """                case SimulationEventType.RoundStarted:
                    ApplyMapOrientation(evt.roundNumber, animate);
                    ResetMarkers(evt.roundNumber, false);
                    _bombCarrierId = null;
                    _droppedBombZoneId = null;
                    _bombPlantedAtRoundTime = -1f;
                    ApplyClockPlantedColor();
                    HideBomb();
""",
        """                case SimulationEventType.RoundStarted:
                    ApplyMapOrientation(evt.roundNumber, animate);
                    ResetMarkers(evt.roundNumber, false);
                    _bombCarrierId = null;
                    _droppedBombZoneId = null;
                    _bombPlantedAtRoundTime = -1f;
                    ApplyClockPlantedColor();
                    HideBomb();
                    ClearUtilityMarkers();
""",
    )

    text = must_replace(
        text,
        """                case SimulationEventType.Shot:
                    ShowMuzzleRay(evt.actorId, evt.targetId);
                    break;
""",
        """                case SimulationEventType.Utility:
                    ExpireUtilityMarkers(evt.roundTime);
                    ShowUtilityEffect(evt);
                    RefreshStatPanels();
                    break;
                case SimulationEventType.Shot:
                    ShowMuzzleRay(evt.actorId, evt.targetId);
                    break;
""",
    )

    text = must_replace(
        text,
                """                case SimulationEventType.StatusUpdated:
                    ApplyStatusUpdate(evt);
                    break;
                case SimulationEventType.RoundEnded:
                    HideBomb();
""",
        """                case SimulationEventType.StatusUpdated:
                    ExpireUtilityMarkers(evt.roundTime);
                    ApplyStatusUpdate(evt);
                    break;
                case SimulationEventType.RoundEnded:
                    HideBomb();
                    ClearUtilityMarkers();
""",
    )

    # Insert helper types + methods before HideBomb
    text = must_replace(
        text,
        "        private void HideBomb()\n"
        "        {\n"
        "            _bombZoneId = null;\n"
        "            if (_bombMarker != null)\n"
        "                _bombMarker.gameObject.SetActive(false);\n"
        "        }\n",
        "        private void HideBomb()\n"
        "        {\n"
        "            _bombZoneId = null;\n"
        "            if (_bombMarker != null)\n"
        "                _bombMarker.gameObject.SetActive(false);\n"
        "        }\n"
        "\n"
        "        private sealed class UtilityMapMarker\n"
        "        {\n"
        "            public RectTransform Root;\n"
        "            public Image Fill;\n"
        "            public string ZoneId;\n"
        "            public string Kind;\n"
        "            public float ExpireAtRoundTime;\n"
        "        }\n"
        "\n"
        "        private void ClearUtilityMarkers()\n"
        "        {\n"
        "            for (int i = 0; i < _utilityMarkers.Count; i++)\n"
        "            {\n"
        "                UtilityMapMarker m = _utilityMarkers[i];\n"
        "                if (m?.Root != null)\n"
        "                    Destroy(m.Root.gameObject);\n"
        "            }\n"
        "            _utilityMarkers.Clear();\n"
        "        }\n"
        "\n"
        "        private void ExpireUtilityMarkers(float roundTime)\n"
        "        {\n"
        "            for (int i = _utilityMarkers.Count - 1; i >= 0; i--)\n"
        "            {\n"
        "                UtilityMapMarker m = _utilityMarkers[i];\n"
        "                if (m == null || roundTime + 0.001f < m.ExpireAtRoundTime)\n"
        "                    continue;\n"
        "                if (m.Root != null)\n"
        "                    Destroy(m.Root.gameObject);\n"
        "                _utilityMarkers.RemoveAt(i);\n"
        "            }\n"
        "        }\n"
        "\n"
        "        private void ShowUtilityEffect(SimulationEvent evt)\n"
        "        {\n"
        "            if (evt == null || string.IsNullOrEmpty(evt.zoneId))\n"
        "                return;\n"
        "            string kind = (evt.weaponId ?? string.Empty).ToLowerInvariant();\n"
        "            if (kind == \"he\" || kind == \"hegrenade\")\n"
        "            {\n"
        "                SpawnUtilityBurst(evt.zoneId, new Color(0.95f, 0.55f, 0.12f, 0.55f), 22f);\n"
        "                return;\n"
        "            }\n"
        "\n"
        "            bool smoke = kind == \"smoke\" || kind == \"smoke_grenade\";\n"
        "            bool fire = kind == \"fire\" || kind == \"molotov\" || kind == \"incendiary\";\n"
        "            if (!smoke && !fire)\n"
        "                smoke = true;\n"
        "\n"
        "            float duration = smoke ? 15f : 7f;\n"
        "            Color fill = smoke\n"
        "                ? new Color(0.72f, 0.74f, 0.78f, 0.42f)\n"
        "                : new Color(0.95f, 0.28f, 0.08f, 0.38f);\n"
        "            EnsureUtilityMarkerRoot();\n"
        "            Image disc = CreateUiObject<Image>(\"Util_\" + kind + \"_\" + evt.zoneId, _utilityMarkerRoot);\n"
        "            disc.raycastTarget = false;\n"
        "            disc.color = fill;\n"
        "            RectTransform rt = disc.rectTransform;\n"
        "            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);\n"
        "            rt.pivot = new Vector2(0.5f, 0.5f);\n"
        "            float diameter = smoke ? 54f : 42f;\n"
        "            rt.sizeDelta = new Vector2(diameter, diameter);\n"
        "            rt.anchoredPosition = ClampToMap(LocalZonePosition(evt.zoneId));\n"
        "            disc.transform.SetAsFirstSibling();\n"
        "            _utilityMarkers.Add(new UtilityMapMarker\n"
        "            {\n"
        "                Root = rt,\n"
        "                Fill = disc,\n"
        "                ZoneId = evt.zoneId,\n"
        "                Kind = smoke ? \"smoke\" : \"fire\",\n"
        "                ExpireAtRoundTime = evt.roundTime + duration\n"
        "            });\n"
        "            RefreshFloorVisibility();\n"
        "        }\n"
        "\n"
        "        private void SpawnUtilityBurst(string zoneId, Color color, float size)\n"
        "        {\n"
        "            EnsureUtilityMarkerRoot();\n"
        "            Image disc = CreateUiObject<Image>(\"UtilBurst\", _utilityMarkerRoot);\n"
        "            disc.raycastTarget = false;\n"
        "            disc.color = color;\n"
        "            RectTransform rt = disc.rectTransform;\n"
        "            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);\n"
        "            rt.pivot = new Vector2(0.5f, 0.5f);\n"
        "            rt.sizeDelta = new Vector2(size, size);\n"
        "            rt.anchoredPosition = ClampToMap(LocalZonePosition(zoneId));\n"
        "            StartCoroutine(DestroyUtilityBurstNextFrame(rt));\n"
        "        }\n"
        "\n"
        "        private System.Collections.IEnumerator DestroyUtilityBurstNextFrame(RectTransform rt)\n"
        "        {\n"
        "            yield return new WaitForSecondsRealtime(0.45f);\n"
        "            if (rt != null)\n"
        "                Destroy(rt.gameObject);\n"
        "        }\n"
        "\n"
        "        private void EnsureUtilityMarkerRoot()\n"
        "        {\n"
        "            if (_utilityMarkerRoot != null || _mapRect == null)\n"
        "                return;\n"
        "            GameObject go = new GameObject(\"UtilityMarkers\", typeof(RectTransform));\n"
        "            _utilityMarkerRoot = go.transform;\n"
        "            _utilityMarkerRoot.SetParent(_mapRect, false);\n"
        "            RectTransform rt = go.GetComponent<RectTransform>();\n"
        "            rt.anchorMin = Vector2.zero;\n"
        "            rt.anchorMax = Vector2.one;\n"
        "            rt.offsetMin = Vector2.zero;\n"
        "            rt.offsetMax = Vector2.zero;\n"
        "            rt.SetAsFirstSibling();\n"
        "        }\n",
    )

    # Also expire on Move so smoke fades with clock during playback
    text = must_replace(
        text,
        """                case SimulationEventType.Move:
                    if (silentMoves != null && silentMoves.Contains(evt.sequence))
""",
        """                case SimulationEventType.Move:
                    ExpireUtilityMarkers(evt.roundTime);
                    if (silentMoves != null && silentMoves.Contains(evt.sequence))
""",
    )

    TARGET.write_text(text, encoding="utf-8")
    # Verify Chinese anchors still present
    check = TARGET.read_text(encoding="utf-8")
    if "擊殺廣播" not in check and "叫暫停" not in check:
        raise RuntimeError("UTF-8 anchors missing after patch")
    if "ShowUtilityEffect" not in check:
        raise RuntimeError("ShowUtilityEffect not inserted")
    print("patched ok")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
