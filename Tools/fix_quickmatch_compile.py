# -*- coding: utf-8 -*-
"""Restore QuickMatchDemoController UTF-8 and re-apply official + coach patches."""
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
scripts = [
    "recover_quickmatch.py",
    "patch_coach_timeout_ui.py",
    "patch_official_control_bar.py",
    "patch_official_opponent_ui.py",
    "patch_official_result_and_bar.py",
    "patch_official_hud_meta.py",
    "patch_official_result_rich.py",
    "patch_official_result_logo_fix.py",
    "patch_official_battle_cleanup.py",
    # Final layout: centered official bar + settings BR + ROUND clock (backs up itself).
    "patch_official_battle_layout_fix.py",
    "patch_battle_frost_hud_result.py",
    "patch_official_result_name_align.py",
    "patch_kill_feed_gray_expire.py",
]

def main() -> int:
    for name in scripts:
        path = ROOT / "Tools" / name
        if not path.is_file():
            print("missing", path, file=sys.stderr)
            return 1
        print("run", name)
        r = subprocess.run([sys.executable, str(path)], cwd=str(ROOT))
        if r.returncode != 0:
            return r.returncode

    target = ROOT / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"
    text = target.read_text(encoding="utf-8")
    if "_careerBarInfoText != null" in text and "_careerMatchMetaText" in text:
        old = """            if (_careerBarInfoText != null)
            {
                SetRect(_careerBarInfoText.rectTransform, new Vector2(0.02f, 0.18f), new Vector2(0.58f, 0.82f));
                if (_careerBarInfoText.fontSize > 12)
                    _careerBarInfoText.fontSize = 12;
                _careerBarInfoText.horizontalOverflow = HorizontalWrapMode.Overflow;
            }
            if (_timeoutButtonRect != null)"""
        new = """            if (_timeoutButtonRect != null)"""
        if old in text:
            text = text.replace(old, new, 1)
            target.write_text(text, encoding="utf-8", newline="\n")
            print("removed stale _careerBarInfoText layout block")
    if "???" in text[:50000]:
        print("WARN: file may still contain ???", file=sys.stderr)
    if "ApplyControlBarLayout(!simulation)" in text:
        text = text.replace(
            "ApplyControlBarLayout(!simulation)",
            "ApplyControlBarLayout(simulation)",
        )
        target.write_text(text, encoding="utf-8", newline="\n")
        print("fixed inverted ApplyControlBarLayout(!simulation)")
        text = target.read_text(encoding="utf-8")
    if "ApplyControlBarLayout(simulation)" not in text:
        print("WARN: ApplyControlBarLayout(simulation) missing", file=sys.stderr)
        return 1
    if "ApplyControlBarLayout(!simulation)" in text:
        print("WARN: ApplyControlBarLayout still inverted", file=sys.stderr)
        return 1
    if "OfficialWinUi" not in text:
        print("WARN: OfficialWinUi missing", file=sys.stderr)
        return 1
    if "private static readonly Color OfficialWinUi" not in text:
        needle = "        private static readonly Color AliveRowBg = new Color(PanelInner.r, PanelInner.g, PanelInner.b, 0.72f);\n"
        ins = needle + (
            "        private static readonly Color OfficialWinUi = new Color(0.45f, 0.78f, 0.52f, 1f);\n"
            "        private static readonly Color OfficialLossUi = new Color(0.94f, 0.40f, 0.36f, 1f);\n"
        )
        if needle in text:
            text = text.replace(needle, ins, 1)
            target.write_text(text, encoding="utf-8", newline="\n")
            print("added OfficialWinUi color fields")
        else:
            print("WARN: AliveRowBg anchor missing", file=sys.stderr)
            return 1
    print("OK", target)
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
