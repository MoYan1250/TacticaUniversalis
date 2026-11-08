# -*- coding: utf-8 -*-
"""Skip settlement: busy spinner + elapsed wall time while AutoPlayOneRound yields."""
from pathlib import Path
import backup_quickmatch as bak

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing:\n{old[:200]!r}")
    return text.replace(old, new, 1)


def build_new_skip() -> str:
    return (
        "        private IEnumerator SkipMatchRoutine()\n"
        "        {\n"
        "            _skippingMatch = true;\n"
        "            _skipBusyStartedAt = Time.unscaledTime;\n"
        "            SetSkipBusyVisible(true);\n"
        "            RefreshSkipBusyHud();\n"
        "            try\n"
        "            {\n"
        "                // Yield each round so the busy spinner and elapsed clock can update.\n"
        "                yield return null;\n"
        "\n"
        "                if (_session != null)\n"
        "                {\n"
        "                    while (!_session.IsFinished)\n"
        "                    {\n"
        "                        if (!_session.AutoPlayOneRound(myTeamId))\n"
        "                            break;\n"
        "                        RefreshSkipBusyHud();\n"
        "                        yield return null;\n"
        "                    }\n"
        "                }\n"
        "\n"
        "                _result = _session?.Result;\n"
        "                ApplySkippedMatchPresentation();\n"
        "\n"
        "                if (_careerOfficialMatch && _result != null && _result.winnerTeamId != null)\n"
        "                    yield return ShowOfficialResultFlow();\n"
        "            }\n"
        "            finally\n"
        "            {\n"
        "                SetSkipBusyVisible(false);\n"
        "                _skippingMatch = false;\n"
        "                _playback = null;\n"
        "            }\n"
        "        }\n"
        "\n"
        "        private void TickSkipBusySpinner()\n"
        "        {\n"
        "            if (!_skippingMatch || _skipBusySpinner == null || !_skipBusySpinner.gameObject.activeSelf)\n"
        "                return;\n"
        "            _skipBusySpinner.rectTransform.Rotate(0f, 0f, -360f * Time.unscaledDeltaTime);\n"
        "            RefreshSkipBusyHud();\n"
        "        }\n"
        "\n"
        "        private void SetSkipBusyVisible(bool visible)\n"
        "        {\n"
        "            if (_skipBusySpinner != null)\n"
        "                _skipBusySpinner.gameObject.SetActive(visible);\n"
        "        }\n"
        "\n"
        "        private void RefreshSkipBusyHud()\n"
        "        {\n"
        "            float elapsed = Mathf.Max(0f, Time.unscaledTime - _skipBusyStartedAt);\n"
        "            int totalSec = Mathf.FloorToInt(elapsed);\n"
        "            int mm = totalSec / 60;\n"
        "            int ss = totalSec % 60;\n"
        "            int round = _session != null\n"
        "                ? Mathf.Max(1, _session.NextRoundNumber)\n"
        "                : Mathf.Max(1, _currentRound);\n"
        '            SetRoundHud(round, $"跳過結算中 {mm}:{ss:00}");\n'
        "        }\n"
        "\n"
    )


def apply(text: str) -> str:
    text = must_replace(
        text,
        """        private bool _careerOfficialMatch;
        private bool _skippingMatch;
        private string _careerCompetitionTitle;
""",
        """        private bool _careerOfficialMatch;
        private bool _skippingMatch;
        private Image _skipBusySpinner;
        private float _skipBusyStartedAt;
        private string _careerCompetitionTitle;
""",
    )

    text = must_replace(
        text,
        """            _clockText = CreateText("Clock", topHud.transform, 20, TextAnchor.MiddleCenter, TextPrimary);
            SetRect(_clockText.rectTransform, new Vector2(0.34f, 0.36f), new Vector2(0.66f, 0.64f));
            _clockText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _clockText.verticalOverflow = VerticalWrapMode.Truncate;
            _clockText.fontStyle = FontStyle.Bold;
            _clockText.raycastTarget = false;
            _clockText.text = "1:55";
""",
        """            _clockText = CreateText("Clock", topHud.transform, 20, TextAnchor.MiddleCenter, TextPrimary);
            SetRect(_clockText.rectTransform, new Vector2(0.34f, 0.36f), new Vector2(0.66f, 0.64f));
            _clockText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _clockText.verticalOverflow = VerticalWrapMode.Truncate;
            _clockText.fontStyle = FontStyle.Bold;
            _clockText.raycastTarget = false;
            _clockText.text = "1:55";

            _skipBusySpinner = CreateUiObject<Image>("SkipBusySpinner", topHud.transform);
            SetRect(_skipBusySpinner.rectTransform, new Vector2(0.285f, 0.38f), new Vector2(0.335f, 0.62f));
            _skipBusySpinner.sprite = _playbackSkipSprite;
            _skipBusySpinner.preserveAspect = true;
            _skipBusySpinner.color = TextPrimary;
            _skipBusySpinner.raycastTarget = false;
            _skipBusySpinner.gameObject.SetActive(false);
""",
    )

    text = must_replace(
        text,
        """        private void Update()
        {
            TickMapCamera();
            TickGeneralChases();
            TickMarkerLos();
            TickMuzzleRays();
            PruneExpiredKillFeed();
""",
        """        private void Update()
        {
            TickSkipBusySpinner();
            TickMapCamera();
            TickGeneralChases();
            TickMarkerLos();
            TickMuzzleRays();
            PruneExpiredKillFeed();
""",
    )

    start = text.find("        private IEnumerator SkipMatchRoutine()")
    if start < 0:
        raise RuntimeError("SkipMatchRoutine missing")
    end = text.find("        private void ApplySkippedMatchPresentation()", start)
    if end < 0:
        raise RuntimeError("ApplySkippedMatchPresentation missing")
    return text[:start] + build_new_skip() + text[end:]


def main() -> None:
    bak.backup("skip_busy_spinner")
    original = TARGET.read_text(encoding="utf-8")
    updated = apply(original)
    if "跳過結算中" not in updated:
        raise RuntimeError("Chinese anchor lost")
    if "擊殺" not in updated or "特性觸發" not in updated:
        raise RuntimeError("Chinese HUD anchors lost")
    if "AutoPlayOneRound" not in updated:
        raise RuntimeError("AutoPlayOneRound not wired")
    if "TickSkipBusySpinner" not in updated:
        raise RuntimeError("spinner tick missing")
    # TopHud clock creation runs after sprites load in Awake — verify file order.
    sprite_load = updated.find('_playbackSkipSprite = Resources.Load')
    spinner_create = updated.find('_skipBusySpinner = CreateUiObject')
    if sprite_load < 0 or spinner_create < 0 or sprite_load > spinner_create:
        raise RuntimeError("playback_skip sprite must load before SkipBusySpinner creation")
    TARGET.write_text(updated, encoding="utf-8")
    print("patched", TARGET)


if __name__ == "__main__":
    main()
