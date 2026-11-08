# -*- coding: utf-8 -*-
"""
General mode fixes:
1) Playback wait: compress idle sim-time first, then divide by playbackSpeed
   (old Min(wait, fixedCap) made speed feel stuck near 2x)
2) Keep CycleGeneralSpeed as-is
"""
from pathlib import Path
import backup_quickmatch as bak

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError("missing:\n" + old[:300])
    return text.replace(old, new, 1)


def main() -> None:
    bak.backup("before_general_speed_trait")
    text = TARGET.read_text(encoding="utf-8")
    if "特性觸發" not in text:
        raise RuntimeError("UTF-8 corrupted")

    old = """        private float GeneralModePlaybackWait(float roundTimeGap)
        {
            float speed = Mathf.Max(0.25f, playbackSpeed);
            float wait = Mathf.Max(0f, roundTimeGap) / speed;
            if (!IsGeneralBattleMode())
                return wait;
            // Heartbeat / timeout gaps are ~1s+; compress hard so rounds do not freeze.
            // Short combat gaps keep more of their natural pacing.
            // Compress long idle gaps; keep short nav steps snappy under denser grids.
            float cap = roundTimeGap >= 0.9f ? 0.1f : roundTimeGap <= 0.12f ? 0.12f : 0.35f;
            return Mathf.Min(wait, cap);
        }
"""

    new = """        private float GeneralModePlaybackWait(float roundTimeGap)
        {
            float speed = Mathf.Max(0.25f, playbackSpeed);
            float gap = Mathf.Max(0f, roundTimeGap);
            if (!IsGeneralBattleMode())
                return gap / speed;

            // Compress empty sim-time first, then honor playbackSpeed so 0.5× / 1× / 2×
            // actually change wall-clock pacing (a fixed Min(wait, cap) made speed feel stuck).
            if (gap >= 0.9f)
                gap = 0.22f;
            else if (gap > 0.40f)
                gap = 0.40f;
            return gap / speed;
        }
"""

    text = must_replace(text, old, new)
    TARGET.write_text(text, encoding="utf-8")
    verify = TARGET.read_text(encoding="utf-8")
    if "特性觸發" not in verify:
        raise RuntimeError("UTF-8 lost")
    if "return gap / speed;" not in verify:
        raise RuntimeError("speed fix missing")
    if "float cap = roundTimeGap >= 0.9f ? 0.1f" in verify:
        raise RuntimeError("old fixed cap still present")
    print("patched general speed OK")


if __name__ == "__main__":
    main()
