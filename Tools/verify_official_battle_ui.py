# -*- coding: utf-8 -*-
"""Verify official battle UI restore markers (UTF-8)."""
from pathlib import Path

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def main() -> int:
    text = TARGET.read_text(encoding="utf-8")
    required = [
        "特性觸發",
        "叫暫停",
        "比賽結束",
        "RefreshOfficialMatchMeta",
        "_careerMatchMetaText",
        "ApplyControlBarLayout(simulation)",
        # Skip stops MatchLoop then opens official result via SkipMatchRoutine.
        "yield return ShowOfficialResultFlow()",
        "HeightControlsWidth",
        "string rightId = _opponentTeamId;",
        "SetRect(_settingsButtonRect, new Vector2(0.905f, 0.015f), new Vector2(0.985f, 0.100f))",
        # Official short bar: centered 0.36-0.64 X, tall clickable 0.018-0.095 Y
        "SetRect(_controlBarRect, new Vector2(0.36f, 0.018f), new Vector2(0.64f, 0.095f))",
        # Practice wide bar
        "SetRect(_controlBarRect, new Vector2(0.12f, 0.015f), new Vector2(0.88f, 0.100f))",
        "_clockText.transform.SetAsLastSibling()",
        "ApplyFrostedButtonChrome",
        "KillFeedVisibleSeconds",
        "PruneExpiredKillFeed",
        "private IEnumerator SkipMatchRoutine()",
    ]
    forbidden = [
        "正式賽 · {mapName}",
        "正式賽 · {_careerCompetitionTitle}",
        'string rightId = myTeamId == "navi_2021" ? "gambit_2021" : "navi_2021";',
        "ApplyControlBarLayout(!simulation)",
    ]
    ok = True
    for item in required:
        if item not in text:
            print("MISSING", item)
            ok = False
        else:
            print("OK", item)
    for item in forbidden:
        if item in text:
            print("FORBIDDEN still present:", item)
            ok = False
    # Meta should not include 正式賽 prefix
    if "正式賽 ·" in text:
        print("WARN: leftover 正式賽 · still in file")
        # allow in comments/status elsewhere? search contexts
        for i, line in enumerate(text.splitlines(), 1):
            if "正式賽 ·" in line:
                print(f"  L{i}: {line.strip()[:100]}")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
