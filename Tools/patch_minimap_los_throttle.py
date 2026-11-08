# -*- coding: utf-8 -*-
"""Optimize TickMarkerLos: dirty pos/facing + fewer ray samples + 25Hz throttle."""
from pathlib import Path
import shutil
from datetime import datetime

path = Path(r"Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs")
text = path.read_text(encoding="utf-8")

backup_dir = Path("Tools/backups")
backup_dir.mkdir(parents=True, exist_ok=True)
stamp = datetime.now().strftime("%Y%m%d_%H%M%S")
shutil.copy2(path, backup_dir / f"QuickMatchDemoController_{stamp}_minimap_los.cs")

old_const = "        private const int ViewConeRaySamples = 20;"
new_const = """        private const int ViewConeRaySamples = 10;
        private const float MarkerLosPosEpsilon = 0.5f;
        private const float MarkerLosFacingEpsilon = 0.5f;
        private const float MarkerLosMinInterval = 1f / 25f;"""
if old_const not in text:
    raise SystemExit("ViewConeRaySamples const not found")
text = text.replace(old_const, new_const, 1)

old_facing = """        private readonly Dictionary<string, float> _markerFacingDeg =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);"""
new_facing = """        private readonly Dictionary<string, float> _markerFacingDeg =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Vector2> _markerLosCachedPos =
            new Dictionary<string, Vector2>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, float> _markerLosCachedFacing =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        private float _markerLosTimeBudget;"""
if old_facing not in text:
    raise SystemExit("_markerFacingDeg field not found")
text = text.replace(old_facing, new_facing, 1)

old_block = """        private void TickMarkerLos()
        {
            if (!IsGeneralBattleMode())
                return;
            GetMapContentSize(out float width, out float height);
            if (!_mapCollision.IsReady ||
                Mathf.Abs(width - _collisionMapSize.x) > 1f ||
                Mathf.Abs(height - _collisionMapSize.y) > 1f)
                RebuildMapCollision();
            if (!_mapCollision.IsReady)
                return;
            foreach (KeyValuePair<string, RectTransform> pair in _markers)
            {
                if (pair.Value != null && pair.Value.gameObject.activeInHierarchy)
                    RefreshMarkerLos(pair.Key);
            }
        }

        private void RefreshAllMarkerLos()
        {
            foreach (KeyValuePair<string, RectTransform> pair in _markers)
                RefreshMarkerLos(pair.Key);
        }

        private void RefreshMarkerLos(string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
                return;
            if (!_markers.TryGetValue(playerId, out RectTransform marker) || marker == null)
                return;
            if (!_markerFacingDeg.TryGetValue(playerId, out float facingDeg))
                facingDeg = 90f;

            bool general = IsGeneralBattleMode();
            float coneLen = general ? ViewConeLength : ViewConeLength * 1.15f;
            int samples = ViewConeRaySamples;
            Vector2 origin = marker.anchoredPosition;"""

new_block = """        private void TickMarkerLos()
        {
            if (!IsGeneralBattleMode())
                return;
            GetMapContentSize(out float width, out float height);
            if (!_mapCollision.IsReady ||
                Mathf.Abs(width - _collisionMapSize.x) > 1f ||
                Mathf.Abs(height - _collisionMapSize.y) > 1f)
                RebuildMapCollision();
            if (!_mapCollision.IsReady)
                return;

            _markerLosTimeBudget += Time.unscaledDeltaTime;
            bool intervalDue = _markerLosTimeBudget >= MarkerLosMinInterval;
            if (intervalDue)
                _markerLosTimeBudget = 0f;

            foreach (KeyValuePair<string, RectTransform> pair in _markers)
            {
                if (pair.Value == null || !pair.Value.gameObject.activeInHierarchy)
                    continue;
                // Dirty markers refresh immediately; otherwise throttle to ~25Hz.
                if (!MarkerLosIsDirty(pair.Key, pair.Value) && !intervalDue)
                    continue;
                if (!intervalDue && !MarkerLosIsDirty(pair.Key, pair.Value))
                    continue;
                if (!MarkerLosIsDirty(pair.Key, pair.Value) && !intervalDue)
                    continue;
                RefreshMarkerLos(pair.Key, force: true);
            }
        }

        private bool MarkerLosIsDirty(string playerId, RectTransform marker)
        {
            if (marker == null)
                return true;
            if (!_markerFacingDeg.TryGetValue(playerId, out float facingDeg))
                facingDeg = 90f;
            Vector2 pos = marker.anchoredPosition;
            if (!_markerLosCachedPos.TryGetValue(playerId, out Vector2 cachedPos) ||
                !_markerLosCachedFacing.TryGetValue(playerId, out float cachedFacing))
                return true;
            if ((pos - cachedPos).sqrMagnitude > MarkerLosPosEpsilon * MarkerLosPosEpsilon)
                return true;
            if (Mathf.Abs(Mathf.DeltaAngle(cachedFacing, facingDeg)) > MarkerLosFacingEpsilon)
                return true;
            return false;
        }

        private void RefreshAllMarkerLos()
        {
            _markerLosCachedPos.Clear();
            _markerLosCachedFacing.Clear();
            foreach (KeyValuePair<string, RectTransform> pair in _markers)
                RefreshMarkerLos(pair.Key, force: true);
        }

        private void RefreshMarkerLos(string playerId, bool force = false)
        {
            if (string.IsNullOrEmpty(playerId))
                return;
            if (!_markers.TryGetValue(playerId, out RectTransform marker) || marker == null)
                return;
            if (!_markerFacingDeg.TryGetValue(playerId, out float facingDeg))
                facingDeg = 90f;
            if (!force && !MarkerLosIsDirty(playerId, marker))
                return;

            bool general = IsGeneralBattleMode();
            float coneLen = general ? ViewConeLength : ViewConeLength * 1.15f;
            int samples = ViewConeRaySamples;
            Vector2 origin = marker.anchoredPosition;"""

if old_block not in text:
    raise SystemExit("Tick/Refresh block not found")
text = text.replace(old_block, new_block, 1)

# Fix the botched triple continue logic I accidentally wrote - clean it up
botched = """            foreach (KeyValuePair<string, RectTransform> pair in _markers)
            {
                if (pair.Value == null || !pair.Value.gameObject.activeInHierarchy)
                    continue;
                // Dirty markers refresh immediately; otherwise throttle to ~25Hz.
                if (!MarkerLosIsDirty(pair.Key, pair.Value) && !intervalDue)
                    continue;
                if (!intervalDue && !MarkerLosIsDirty(pair.Key, pair.Value))
                    continue;
                if (!MarkerLosIsDirty(pair.Key, pair.Value) && !intervalDue)
                    continue;
                RefreshMarkerLos(pair.Key, force: true);
            }"""

clean = """            foreach (KeyValuePair<string, RectTransform> pair in _markers)
            {
                if (pair.Value == null || !pair.Value.gameObject.activeInHierarchy)
                    continue;
                // Dirty markers refresh immediately; clean ones only on ~25Hz tick.
                bool dirty = MarkerLosIsDirty(pair.Key, pair.Value);
                if (!dirty && !intervalDue)
                    continue;
                RefreshMarkerLos(pair.Key, force: true);
            }"""

if botched not in text:
    raise SystemExit("botched foreach not found for cleanup")
text = text.replace(botched, clean, 1)

old_end = """                sight.rectTransform.sizeDelta = new Vector2(Mathf.Max(2f, hit), SightRayWidth);
                sight.gameObject.SetActive(general && marker.gameObject.activeSelf);
            }
        }
"""

# Unique enough near ShowMuzzle - find after RefreshMarkerLos
idx = text.find("private void RefreshMarkerLos(string playerId, bool force = false)")
if idx < 0:
    raise SystemExit("RefreshMarkerLos signature not found after patch")
end_marker = """                sight.rectTransform.sizeDelta = new Vector2(Mathf.Max(2f, hit), SightRayWidth);
                sight.gameObject.SetActive(general && marker.gameObject.activeSelf);
            }
        }
"""
end_idx = text.find(end_marker, idx)
if end_idx < 0:
    raise SystemExit("RefreshMarkerLos end not found")
new_end = """                sight.rectTransform.sizeDelta = new Vector2(Mathf.Max(2f, hit), SightRayWidth);
                sight.gameObject.SetActive(general && marker.gameObject.activeSelf);
            }

            _markerLosCachedPos[playerId] = origin;
            _markerLosCachedFacing[playerId] = facingDeg;
        }
"""
text = text[:end_idx] + new_end + text[end_idx + len(end_marker) :]

path.write_text(text, encoding="utf-8")

# Sanity: Chinese anchors
for needle in ("擊殺廣播", "跳過結算中"):
    if needle not in text:
        print("WARN missing:", needle)
    else:
        print("anchor ok:", needle)
print("done")
