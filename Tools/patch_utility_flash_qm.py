# -*- coding: utf-8 -*-
"""Wire flash bang white burst + blinded marker glow on QuickMatch map."""
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
        [sys.executable, str(ROOT / "Tools" / "backup_quickmatch.py"), "flash_blind"]
    )
    text = TARGET.read_text(encoding="utf-8")
    if "ApplyBlindedStatus" in text and 'kind == "flash"' in text:
        print("already patched")
        return 0

    text = must_replace(
        text,
        "        private readonly List<UtilityMapMarker> _utilityMarkers = new List<UtilityMapMarker>();\n"
        "        private Transform _utilityMarkerRoot;\n",
        "        private readonly List<UtilityMapMarker> _utilityMarkers = new List<UtilityMapMarker>();\n"
        "        private Transform _utilityMarkerRoot;\n"
        "        private readonly Dictionary<string, float> _blindedUntilByPlayer =\n"
        "            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);\n"
        "        private readonly Dictionary<string, Image> _markerBlindGlows =\n"
        "            new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);\n",
    )

    text = must_replace(
        text,
        "                    HideBomb();\n"
        "                    ClearUtilityMarkers();\n"
        "                    _roundHasDeath = false;\n",
        "                    HideBomb();\n"
        "                    ClearUtilityMarkers();\n"
        "                    ClearBlindedGlows();\n"
        "                    _roundHasDeath = false;\n",
    )

    text = must_replace(
        text,
        """                case SimulationEventType.Move:
                    ExpireUtilityMarkers(evt.roundTime);
""",
        """                case SimulationEventType.Move:
                    ExpireUtilityMarkers(evt.roundTime);
                    ExpireBlindedGlows(evt.roundTime);
""",
    )

    text = must_replace(
        text,
        """                case SimulationEventType.StatusUpdated:
                    ExpireUtilityMarkers(evt.roundTime);
                    ApplyStatusUpdate(evt);
""",
        """                case SimulationEventType.StatusUpdated:
                    ExpireUtilityMarkers(evt.roundTime);
                    ExpireBlindedGlows(evt.roundTime);
                    ApplyStatusUpdate(evt);
""",
    )

    text = must_replace(
        text,
        """                case SimulationEventType.RoundEnded:
                    HideBomb();
                    ClearUtilityMarkers();
""",
        """                case SimulationEventType.RoundEnded:
                    HideBomb();
                    ClearUtilityMarkers();
                    ClearBlindedGlows();
""",
    )

    text = must_replace(
        text,
        """            string kind = (evt.weaponId ?? string.Empty).ToLowerInvariant();
            if (kind == "he" || kind == "hegrenade")
            {
                SpawnUtilityBurst(evt.zoneId, new Color(0.95f, 0.55f, 0.12f, 0.55f), 22f);
                return;
            }

            bool smoke = kind == "smoke" || kind == "smoke_grenade";
""",
        """            string kind = (evt.weaponId ?? string.Empty).ToLowerInvariant();
            if (kind == "he" || kind == "hegrenade")
            {
                SpawnUtilityBurst(evt.zoneId, new Color(0.95f, 0.55f, 0.12f, 0.55f), 22f);
                return;
            }
            if (kind == "flash" || kind == "flashbang")
            {
                SpawnUtilityBurst(evt.zoneId, new Color(1f, 1f, 1f, 0.72f), 36f);
                return;
            }

            bool smoke = kind == "smoke" || kind == "smoke_grenade";
""",
    )

    text = must_replace(
        text,
        """            if (ApplyBombLootStatus(evt))
                return;

            if (string.IsNullOrEmpty(evt.actorId))
                return;
            if (string.IsNullOrEmpty(evt.message))
                return;
            if (evt.message.StartsWith("hp:", StringComparison.OrdinalIgnoreCase))
""",
        """            if (ApplyBombLootStatus(evt))
                return;

            if (ApplyBlindedStatus(evt))
                return;

            if (string.IsNullOrEmpty(evt.actorId))
                return;
            if (string.IsNullOrEmpty(evt.message))
                return;
            if (evt.message.StartsWith("hp:", StringComparison.OrdinalIgnoreCase))
""",
    )

    text = must_replace(
        text,
        "        private void SetMarkerVitality(string playerId, int hp, int armor)\n"
        "        {\n"
        "            if (_markerHpFills.TryGetValue(playerId, out Image hpFill) && hpFill != null)\n"
        "                hpFill.fillAmount = Mathf.Clamp01(hp / 100f);\n"
        "            if (_markerArmorFills.TryGetValue(playerId, out Image armorFill) && armorFill != null)\n"
        "                armorFill.fillAmount = Mathf.Clamp01(armor / 100f);\n"
        "        }\n",
        "        private void SetMarkerVitality(string playerId, int hp, int armor)\n"
        "        {\n"
        "            if (_markerHpFills.TryGetValue(playerId, out Image hpFill) && hpFill != null)\n"
        "                hpFill.fillAmount = Mathf.Clamp01(hp / 100f);\n"
        "            if (_markerArmorFills.TryGetValue(playerId, out Image armorFill) && armorFill != null)\n"
        "                armorFill.fillAmount = Mathf.Clamp01(armor / 100f);\n"
        "        }\n"
        "\n"
        "        private bool ApplyBlindedStatus(SimulationEvent evt)\n"
        "        {\n"
        "            if (evt == null || string.IsNullOrEmpty(evt.actorId) || string.IsNullOrEmpty(evt.message))\n"
        "                return false;\n"
        "            bool tagged = evt.reasonTags != null &&\n"
        "                Array.Exists(evt.reasonTags, t =>\n"
        "                    string.Equals(t, \"utility_blinded\", StringComparison.OrdinalIgnoreCase));\n"
        "            if (!evt.message.StartsWith(\"blinded:\", StringComparison.OrdinalIgnoreCase) && !tagged)\n"
        "                return false;\n"
        "            string payload = evt.message;\n"
        "            int colon = payload.IndexOf(':');\n"
        "            if (colon < 0 ||\n"
        "                !float.TryParse(payload.Substring(colon + 1),\n"
        "                    System.Globalization.NumberStyles.Float,\n"
        "                    System.Globalization.CultureInfo.InvariantCulture,\n"
        "                    out float until))\n"
        "                return tagged;\n"
        "            _blindedUntilByPlayer[evt.actorId] = until;\n"
        "            EnsureBlindGlow(evt.actorId, true);\n"
        "            RefreshStatPanels();\n"
        "            return true;\n"
        "        }\n"
        "\n"
        "        private void EnsureBlindGlow(string playerId, bool on)\n"
        "        {\n"
        "            if (string.IsNullOrEmpty(playerId) || !_markers.TryGetValue(playerId, out RectTransform root) || root == null)\n"
        "                return;\n"
        "            if (!on)\n"
        "            {\n"
        "                if (_markerBlindGlows.TryGetValue(playerId, out Image existing) && existing != null)\n"
        "                    existing.enabled = false;\n"
        "                return;\n"
        "            }\n"
        "            if (!_markerBlindGlows.TryGetValue(playerId, out Image glow) || glow == null)\n"
        "            {\n"
        "                glow = CreateUiObject<Image>(\"BlindGlow\", root);\n"
        "                glow.sprite = _circleSprite;\n"
        "                glow.raycastTarget = false;\n"
        "                glow.color = new Color(1f, 1f, 1f, 0.55f);\n"
        "                RectTransform rt = glow.rectTransform;\n"
        "                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);\n"
        "                rt.pivot = new Vector2(0.5f, 0.5f);\n"
        "                float d = QuickMarkerDotDiameter + 14f;\n"
        "                rt.sizeDelta = new Vector2(d, d);\n"
        "                rt.SetAsFirstSibling();\n"
        "                _markerBlindGlows[playerId] = glow;\n"
        "            }\n"
        "            glow.enabled = true;\n"
        "            glow.color = new Color(1f, 1f, 1f, 0.55f);\n"
        "        }\n"
        "\n"
        "        private void ExpireBlindedGlows(float roundTime)\n"
        "        {\n"
        "            if (_blindedUntilByPlayer.Count == 0)\n"
        "                return;\n"
        "            var expired = new List<string>();\n"
        "            foreach (KeyValuePair<string, float> pair in _blindedUntilByPlayer)\n"
        "            {\n"
        "                if (roundTime + 0.001f >= pair.Value)\n"
        "                    expired.Add(pair.Key);\n"
        "            }\n"
        "            for (int i = 0; i < expired.Count; i++)\n"
        "            {\n"
        "                string id = expired[i];\n"
        "                _blindedUntilByPlayer.Remove(id);\n"
        "                EnsureBlindGlow(id, false);\n"
        "            }\n"
        "        }\n"
        "\n"
        "        private void ClearBlindedGlows()\n"
        "        {\n"
        "            foreach (KeyValuePair<string, Image> pair in _markerBlindGlows)\n"
        "            {\n"
        "                if (pair.Value != null)\n"
        "                    Destroy(pair.Value.gameObject);\n"
        "            }\n"
        "            _markerBlindGlows.Clear();\n"
        "            _blindedUntilByPlayer.Clear();\n"
        "        }\n",
    )

    TARGET.write_text(text, encoding="utf-8")
    check = TARGET.read_text(encoding="utf-8")
    if "擊殺廣播" not in check and "叫暫停" not in check:
        raise RuntimeError("UTF-8 anchors missing after patch")
    if "ApplyBlindedStatus" not in check:
        raise RuntimeError("ApplyBlindedStatus not inserted")
    if 'kind == "flash"' not in check:
        raise RuntimeError("flash branch missing in ShowUtilityEffect")
    print("patched ok")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
