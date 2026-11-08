# -*- coding: utf-8 -*-
"""Phase C static verification markers."""
from pathlib import Path

ROOT = Path(r"e:/TacticaUniversalis/Assets/_Project/Scripts/Presentation")


def main() -> int:
    checks = []
    hub = (ROOT / "CareerClubHubPresentation.cs").read_text(encoding="utf-8")
    for name, ok in [
        ("_clubHubHeaderLogo", "_clubHubHeaderLogo" in hub),
        ("ClubHubChromeBorder", "ClubHubChromeBorder" in hub),
        ("ClubHubChromeShadow", "ClubHubChromeShadow" in hub),
        ("CreateClubHubInsetWell", "CreateClubHubInsetWell" in hub),
        ("UiRoundedRectUtility", "UiRoundedRectUtility" in hub),
        ("Shadow", '"Shadow"' in hub),
        ("Face", '"Face"' in hub),
        ("Frame", '"Frame"' in hub),
        ("no_old_logo_find", 'Find("ClubHubPanel/Header/Logo")' not in hub),
    ]:
        checks.append(("B1", name, ok))

    ev = (ROOT / "CareerScriptedEventPresentation.cs").read_text(encoding="utf-8")
    for name, ok in [
        ("EventGlassFill", "EventGlassFill" in ev),
        ("BlurRunner", "UiBackdropBlurRunner" in ev),
        ("BeginShowScriptedEventWithBlur", "BeginShowScriptedEventWithBlur" in ev),
        ("ReleaseScriptedEventBlurTexture", "ReleaseScriptedEventBlurTexture" in ev),
        ("EventCard", "EventCard" in ev),
        ("resizeTextMaxSize15", "resizeTextMaxSize = 15" in ev),
        ("HideScriptedEventOverlay", "HideScriptedEventOverlay" in ev),
        ("SyncScriptedEventCardRect", "SyncScriptedEventCardRect" in ev),
    ]:
        checks.append(("B2", name, ok))

    mp = (ROOT / "CareerWorldMapPresentation.cs").read_text(encoding="utf-8")
    for name, ok in [
        ("OuterGlow", "OuterGlow" in mp),
        ("ApplyMapHubNodeVisual", "ApplyMapHubNodeVisual" in mp),
        ("GetRadialGlowSprite", "GetRadialGlowSprite" in mp),
        ("MapHubOuterGlowIdleAlpha", "MapHubOuterGlowIdleAlpha" in mp),
        ("_hoveredHubId", "_hoveredHubId" in mp),
        ("size64", "new Vector2(64f, 64f)" in mp),
    ]:
        checks.append(("B3", name, ok))

    mt = (ROOT / "CareerMissionTreePresentation.cs").read_text(encoding="utf-8")
    for name, ok in [
        ("BlurCapture", "UiBackdropBlurCapture" in mt),
        ("GlassFill", "GlassFill" in mt),
        ("CaptureBlurred", "CaptureBlurred" in mt),
        ("ReleaseBlurTexture", "ReleaseBlurTexture" in mt),
        ("Close", "public void Close" in mt),
        ("Open", "public void Open" in mt),
    ]:
        checks.append(("MT", name, ok))

    scripts = Path(r"e:/TacticaUniversalis/Assets/_Project/Scripts")
    tryget = any(
        "TryGetClub" in f.read_text(encoding="utf-8")
        for f in scripts.rglob("*CareerApplication*.cs")
    )
    checks.append(("C5", "TryGetClub", tryget))

    cd = (ROOT / "CareerDemoController.cs").read_text(encoding="utf-8")
    checks.append(("MT", "mission_tree_wired", "CareerMissionTreePresentation" in cd or "_missionTree" in cd))

    qm = (ROOT / "QuickMatchDemoController.cs").read_text(encoding="utf-8")
    for name, ok in [
        ("ApplyControlBarLayout_sim", "ApplyControlBarLayout(simulation)" in qm),
        ("not_negated", "ApplyControlBarLayout(!simulation)" not in qm),
        ("settings_br", "0.905f, 0.015f), new Vector2(0.985f, 0.100f)" in qm),
        ("official_bar", "0.36f, 0.018f), new Vector2(0.64f, 0.095f)" in qm),
        ("practice_bar", "0.12f, 0.015f), new Vector2(0.88f, 0.100f)" in qm),
        ("clock_sibling", "_clockText.transform.SetAsLastSibling()" in qm),
        ("skip_flow", "yield return ShowOfficialResultFlow()" in qm),
        ("kill_feed_zh", "擊殺" in qm),
    ]:
        checks.append(("QM", name, ok))

    fail = 0
    for group, name, ok in checks:
        print(("OK" if ok else "FAIL"), group, name)
        if not ok:
            fail += 1
    print("TOTAL_FAIL", fail)
    return 1 if fail else 0


if __name__ == "__main__":
    raise SystemExit(main())
