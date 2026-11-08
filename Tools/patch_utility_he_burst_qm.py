# -*- coding: utf-8 -*-
"""HE grenade multi-layer explosion burst on QuickMatch map."""
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
        [sys.executable, str(ROOT / "Tools" / "backup_quickmatch.py"), "he_burst"]
    )
    text = TARGET.read_text(encoding="utf-8")
    if "SpawnHeExplosionBurst" in text:
        print("already patched")
        return 0

    text = must_replace(
        text,
        """            if (kind == "he" || kind == "hegrenade")
            {
                SpawnUtilityBurst(evt.zoneId, new Color(0.95f, 0.55f, 0.12f, 0.55f), 22f);
                return;
            }
""",
        """            if (kind == "he" || kind == "hegrenade")
            {
                SpawnHeExplosionBurst(evt.zoneId);
                return;
            }
""",
    )

    text = must_replace(
        text,
        """        private void SpawnUtilityBurst(string zoneId, Color color, float size)
        {
            EnsureUtilityMarkerRoot();
            Image disc = CreateUiObject<Image>("UtilBurst", _utilityMarkerRoot);
            disc.raycastTarget = false;
            disc.color = color;
            RectTransform rt = disc.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = ClampToMap(LocalZonePosition(zoneId));
            StartCoroutine(DestroyUtilityBurstNextFrame(rt));
        }

        private System.Collections.IEnumerator DestroyUtilityBurstNextFrame(RectTransform rt)
        {
            yield return new WaitForSecondsRealtime(0.45f);
            if (rt != null)
                Destroy(rt.gameObject);
        }
""",
        """        private void SpawnUtilityBurst(string zoneId, Color color, float size)
        {
            SpawnUtilityBurst(zoneId, color, size, 0.45f);
        }

        private void SpawnUtilityBurst(string zoneId, Color color, float size, float lifetimeSeconds)
        {
            EnsureUtilityMarkerRoot();
            Image disc = CreateUiObject<Image>("UtilBurst", _utilityMarkerRoot);
            disc.raycastTarget = false;
            disc.color = color;
            RectTransform rt = disc.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = ClampToMap(LocalZonePosition(zoneId));
            StartCoroutine(DestroyUtilityBurstAfter(rt, lifetimeSeconds));
        }

        private void SpawnHeExplosionBurst(string zoneId)
        {
            SpawnUtilityBurst(zoneId, new Color(1f, 0.92f, 0.35f, 0.75f), 18f, 0.28f);
            SpawnUtilityBurst(zoneId, new Color(0.98f, 0.45f, 0.08f, 0.55f), 34f, 0.45f);
            SpawnUtilityBurst(zoneId, new Color(0.85f, 0.22f, 0.05f, 0.32f), 52f, 0.62f);
        }

        private System.Collections.IEnumerator DestroyUtilityBurstNextFrame(RectTransform rt)
        {
            yield return DestroyUtilityBurstAfter(rt, 0.45f);
        }

        private System.Collections.IEnumerator DestroyUtilityBurstAfter(RectTransform rt, float seconds)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0.05f, seconds));
            if (rt != null)
                Destroy(rt.gameObject);
        }
""",
    )

    TARGET.write_text(text, encoding="utf-8")
    check = TARGET.read_text(encoding="utf-8")
    if "\u64ca\u6bba\u5ee3\u64ad" not in check and "\u53eb\u66ab\u505c" not in check:
        raise RuntimeError("UTF-8 anchors missing after patch")
    if "SpawnHeExplosionBurst" not in check:
        raise RuntimeError("SpawnHeExplosionBurst missing")
    print("patched ok")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
