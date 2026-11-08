# -*- coding: utf-8 -*-
"""Remove empty !animate stub in SetGeneralMoveTarget."""
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def main() -> int:
    subprocess.check_call(
        [sys.executable, str(ROOT / "Tools" / "backup_quickmatch.py"), "chase_cleanup_animate"]
    )
    text = TARGET.read_text(encoding="utf-8")
    old = """            _moveDestinations[playerId] = zoneId;

            // Even silent / non-animated logical updates must not snap the marker —
            // keep walking toward the latest destination at chase speed.
            if (!animate)
            {
                // Fall through to chase setup below (same visual path rules).
            }

            if (_generalChases.TryGetValue(playerId, out GeneralChaseState ongoing) &&
"""
    new = """            _moveDestinations[playerId] = zoneId;
            // Never snap markers for general mode — always chase from current visual position.
            _ = animate;

            if (_generalChases.TryGetValue(playerId, out GeneralChaseState ongoing) &&
"""
    if old not in text:
        raise RuntimeError("anchor missing")
    text = text.replace(old, new, 1)
    if "叫暫停" not in text:
        raise RuntimeError("UTF-8 broken")
    TARGET.write_text(text, encoding="utf-8")
    print("cleaned")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
