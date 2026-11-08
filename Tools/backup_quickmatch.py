# -*- coding: utf-8 -*-
"""Copy QuickMatchDemoController.cs to Tools/backups/ before any battle-UI edit."""
from datetime import datetime
from pathlib import Path
import shutil
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"
BACKUP_DIR = ROOT / "Tools" / "backups"


def backup(reason: str = "") -> Path:
    if not SRC.is_file():
        raise FileNotFoundError(SRC)
    text = SRC.read_text(encoding="utf-8")
    # Label text has changed over time; accept several battle-UI Chinese anchors.
    if ("擊殺廣播" not in text and "特性觸發" not in text and "叫暫停" not in text and
            "KillFeed" not in text):
        raise RuntimeError("refusing to backup: UTF-8 looks corrupted (missing battle UI anchors)")
    BACKUP_DIR.mkdir(parents=True, exist_ok=True)
    stamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    suffix = f"_{reason}" if reason else ""
    dest = BACKUP_DIR / f"QuickMatchDemoController_{stamp}{suffix}.cs"
    shutil.copy2(SRC, dest)
    print("backed up", dest)
    return dest


def main() -> int:
    reason = sys.argv[1] if len(sys.argv) > 1 else "manual"
    backup(reason)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
