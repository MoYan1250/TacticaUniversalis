# -*- coding: utf-8 -*-
"""Tighten general-mode idle wait compression (long heartbeat/timeout gaps)."""
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str, count: int = 1) -> str:
    if old not in text:
        raise RuntimeError(f"patch anchor missing: {old[:120]!r}")
    return text.replace(old, new, count)


def main() -> int:
    subprocess.check_call([sys.executable, str(ROOT / "Tools" / "backup_quickmatch.py"), "hang_wait_tighten"])
    text = TARGET.read_text(encoding="utf-8")

    old = (
        "            if (!IsGeneralBattleMode())\n"
        "                return wait;\n"
        "            // Keep some pacing, but never stall longer than this on empty time.\n"
        "            return Mathf.Min(wait, 0.45f);\n"
    )
    new = (
        "            if (!IsGeneralBattleMode())\n"
        "                return wait;\n"
        "            // Heartbeat / timeout gaps are ~1s+; compress hard so rounds do not freeze.\n"
        "            // Short combat gaps keep more of their natural pacing.\n"
        "            float cap = roundTimeGap >= 0.9f ? 0.1f : 0.35f;\n"
        "            return Mathf.Min(wait, cap);\n"
    )
    if "roundTimeGap >= 0.9f" in text:
        print("already tightened")
        return 0
    text = must_replace(text, old, new)

    if "叫暫停" not in text and "KillFeed" not in text:
        raise RuntimeError("UTF-8 corruption suspected")
    TARGET.write_text(text, encoding="utf-8")
    print("patched", TARGET)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
