# -*- coding: utf-8 -*-
"""Official result: series pips outside logos (same order as TopHud)."""
from pathlib import Path
import backup_quickmatch as bak

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError("missing:\n" + old[:280])
    return text.replace(old, new, 1)


def main() -> None:
    bak.backup("before_result_series_outer")
    text = TARGET.read_text(encoding="utf-8")
    if "特性觸發" not in text:
        raise RuntimeError("UTF-8 corrupted")

    if "_officialResultUsesOuterSeriesPips" not in text:
        text = must_replace(
            text,
            "        private Image[] _resultLeftSeriesPips;\n"
            "        private Image[] _resultRightSeriesPips;\n",
            "        private Image[] _resultLeftSeriesPips;\n"
            "        private Image[] _resultRightSeriesPips;\n"
            "        private bool _officialResultUsesOuterSeriesPips;\n",
        )

    text = must_replace(
        text,
        """            if (_officialResultRoot != null && _officialResultLeftLogo == null)
            {
                Destroy(_officialResultRoot);
                _officialResultRoot = null;
            }
            if (_officialResultRoot != null)
                return;
""",
        """            if (_officialResultRoot != null &&
                (_officialResultLeftLogo == null || !_officialResultUsesOuterSeriesPips))
            {
                Destroy(_officialResultRoot);
                _officialResultRoot = null;
                _officialResultLeftLogo = null;
                _officialResultRightLogo = null;
                _resultLeftSeriesPips = null;
                _resultRightSeriesPips = null;
            }
            if (_officialResultRoot != null)
                return;
""",
    )

    text = must_replace(
        text,
        """            // Logo X centers: 0.18 / 0.82 — names use the same column centers underneath.
            _officialResultLeftLogo = CreateResultTeamLogo("LeftLogo", panel.transform, 0.18f);
            _resultLeftSeriesPips = CreateSeriesPips("ResultLeftPips", panel.transform, 0.26f, true);
            _officialResultRightLogo = CreateResultTeamLogo("RightLogo", panel.transform, 0.82f);
            _resultRightSeriesPips = CreateSeriesPips("ResultRightPips", panel.transform, 0.74f, false);
""",
        """            // Same order as TopHud: series | logo | score … score | logo | series
            _resultLeftSeriesPips = CreateSeriesPips("ResultLeftPips", panel.transform, 0.08f, true);
            _officialResultLeftLogo = CreateResultTeamLogo("LeftLogo", panel.transform, 0.18f);
            _officialResultRightLogo = CreateResultTeamLogo("RightLogo", panel.transform, 0.82f);
            _resultRightSeriesPips = CreateSeriesPips("ResultRightPips", panel.transform, 0.92f, false);
            _officialResultUsesOuterSeriesPips = true;
""",
    )

    TARGET.write_text(text, encoding="utf-8")
    verify = TARGET.read_text(encoding="utf-8")
    if "特性觸發" not in verify:
        raise RuntimeError("UTF-8 lost")
    if "0.08f, true)" not in verify or "0.92f, false)" not in verify:
        raise RuntimeError("outer pip anchors missing")
    if "0.26f, true)" in verify.split("EnsureOfficialResultOverlay")[1].split("CreateResultTeamLogo")[0]:
        raise RuntimeError("old inner left pip still present")
    print("patched result series outer OK")


if __name__ == "__main__":
    main()
