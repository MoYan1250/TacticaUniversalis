# -*- coding: utf-8 -*-
"""Shrink general-mode body collision. Marker dots stay the same size."""
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"

OLD = (
    "        /// <summary>Collision radius = 50% of visual circle diameter / 2.</summary>\n"
    "        private const float GeneralCollisionRadius = MarkerDotDiameter * 0.25f;\n"
)
NEW = (
    "        /// <summary>Collision diameter is 20% of the visual circle. The drawn dot is unchanged.</summary>\n"
    "        private const float GeneralCollisionRadius = MarkerDotDiameter * 0.10f;\n"
)


def main() -> int:
    subprocess.check_call([sys.executable, str(ROOT / "Tools" / "backup_quickmatch.py"), "shrink_agent_collision"])
    text = SRC.read_text(encoding="utf-8")
    if "MarkerDotDiameter * 0.10f" in text and "0.25f" not in text.split("GeneralCollisionRadius", 1)[-1][:80]:
        print("already shrunk")
        return 0
    if OLD not in text:
        raise RuntimeError("collision radius anchor missing")
    text = text.replace(OLD, NEW, 1)
    if "叫暫停" not in text or "特性觸發" not in text:
        raise RuntimeError("UTF-8 anchors missing after patch")
    if "ApplyControlBarLayout(simulation)" not in text or "ApplyControlBarLayout(!simulation)" in text:
        raise RuntimeError("control bar polarity changed")
    SRC.write_text(text, encoding="utf-8")
    print("collision radius is now 10% of marker diameter")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
