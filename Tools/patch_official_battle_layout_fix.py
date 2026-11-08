# -*- coding: utf-8 -*-
"""
Official battle layout: larger centered control buttons, settings bottom-right,
ROUND countdown restored under ROUND. Always backs up first.
"""
from pathlib import Path
import backup_quickmatch as bak

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing:\n{old[:160]!r}")
    return text.replace(old, new, 1)


def replace_apply_control_bar(text: str) -> str:
    start = text.find("        private void ApplyControlBarLayout(bool practiceLayout)")
    if start < 0:
        raise RuntimeError("ApplyControlBarLayout missing")
    end = text.find("        private void RefreshOfficialMatchMeta()", start)
    if end < 0:
        end = text.find("        private void ApplySettingsFrostedChrome(bool enabled)", start)
    if end < 0:
        raise RuntimeError("end anchor missing for ApplyControlBarLayout")

    new_block = """        private void ApplyControlBarLayout(bool practiceLayout)
        {
            // Settings always bottom-right (outside the compact official bar).
            if (_settingsButtonRect != null)
                SetRect(_settingsButtonRect, new Vector2(0.905f, 0.015f), new Vector2(0.985f, 0.100f));

            if (_controlBarRect != null)
            {
                if (practiceLayout)
                    SetRect(_controlBarRect, new Vector2(0.12f, 0.015f), new Vector2(0.88f, 0.100f));
                else
                    // Official: four controls, screen-centered, tall enough to click.
                    SetRect(_controlBarRect, new Vector2(0.36f, 0.018f), new Vector2(0.64f, 0.095f));
            }

            if (practiceLayout)
            {
                if (_matchText != null)
                    SetRect(_matchText.rectTransform, new Vector2(0.43f, 0.15f), new Vector2(0.495f, 0.85f));
                if (_timeoutButtonRect != null)
                    SetRect(_timeoutButtonRect, new Vector2(0.50f, 0.15f), new Vector2(0.64f, 0.85f));
                if (_pauseButtonRect != null)
                    SetRect(_pauseButtonRect, new Vector2(0.65f, 0.15f), new Vector2(0.715f, 0.85f));
                if (_speedButtonRect != null)
                    SetRect(_speedButtonRect, new Vector2(0.72f, 0.15f), new Vector2(0.785f, 0.85f));
                if (_skipButtonRect != null)
                    SetRect(_skipButtonRect, new Vector2(0.79f, 0.15f), new Vector2(0.855f, 0.85f));
                return;
            }

            // L→R centered pack: 叫暫停 | 暫停 | 倍速 | 跳過
            if (_timeoutButtonRect != null)
                SetRect(_timeoutButtonRect, new Vector2(0.03f, 0.14f), new Vector2(0.35f, 0.86f));
            if (_pauseButtonRect != null)
                SetRect(_pauseButtonRect, new Vector2(0.37f, 0.14f), new Vector2(0.55f, 0.86f));
            if (_speedButtonRect != null)
                SetRect(_speedButtonRect, new Vector2(0.57f, 0.14f), new Vector2(0.75f, 0.86f));
            if (_skipButtonRect != null)
                SetRect(_skipButtonRect, new Vector2(0.77f, 0.14f), new Vector2(0.97f, 0.86f));
        }

"""
    return text[:start] + new_block + text[end:]


def restore_top_hud_clock(text: str) -> str:
    if "SetRect(topHud.rectTransform, new Vector2(0.32f, 0.886f)" not in text:
        text = must_replace(
            text,
            "            SetRect(topHud.rectTransform, new Vector2(0.32f, 0.900f), new Vector2(0.68f, 0.992f));",
            "            SetRect(topHud.rectTransform, new Vector2(0.32f, 0.886f), new Vector2(0.68f, 0.992f));",
        )

    if "0.862f), new Vector2(0.60f, 0.898f)" in text:
        text = must_replace(
            text,
            "            SetRect(root.rectTransform, new Vector2(0.40f, 0.862f), new Vector2(0.60f, 0.898f));",
            "            SetRect(root.rectTransform, new Vector2(0.40f, 0.848f), new Vector2(0.60f, 0.882f));",
        )

    old_hud = """            _roundText = CreateText("Round", topHud.transform, 18, TextAnchor.MiddleCenter, Accent);
            SetRect(_roundText.rectTransform, new Vector2(0.37f, 0.58f), new Vector2(0.63f, 0.95f));
            _roundText.fontStyle = FontStyle.Bold;
            _roundText.text = "ROUND 01";

            _clockText = CreateText("Clock", topHud.transform, 16, TextAnchor.MiddleCenter, TextPrimary);
            SetRect(_clockText.rectTransform, new Vector2(0.37f, 0.36f), new Vector2(0.63f, 0.58f));
            _clockText.text = "1:55";

            _careerMatchMetaText = CreateText("CareerMeta", topHud.transform, 11, TextAnchor.MiddleCenter, TextMuted);
            SetRect(_careerMatchMetaText.rectTransform, new Vector2(0.34f, 0.06f), new Vector2(0.66f, 0.28f));
            _careerMatchMetaText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _careerMatchMetaText.gameObject.SetActive(false);"""

    new_hud = """            _roundText = CreateText("Round", topHud.transform, 17, TextAnchor.MiddleCenter, Accent);
            SetRect(_roundText.rectTransform, new Vector2(0.37f, 0.66f), new Vector2(0.63f, 0.96f));
            _roundText.fontStyle = FontStyle.Bold;
            _roundText.verticalOverflow = VerticalWrapMode.Truncate;
            _roundText.text = "ROUND 01";

            // Countdown sits directly under ROUND; keep a dedicated band so meta cannot cover it.
            _clockText = CreateText("Clock", topHud.transform, 15, TextAnchor.MiddleCenter, TextPrimary);
            SetRect(_clockText.rectTransform, new Vector2(0.37f, 0.38f), new Vector2(0.63f, 0.64f));
            _clockText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _clockText.verticalOverflow = VerticalWrapMode.Truncate;
            _clockText.raycastTarget = false;
            _clockText.text = "1:55";

            _careerMatchMetaText = CreateText("CareerMeta", topHud.transform, 10, TextAnchor.MiddleCenter, TextMuted);
            SetRect(_careerMatchMetaText.rectTransform, new Vector2(0.30f, 0.04f), new Vector2(0.70f, 0.34f));
            _careerMatchMetaText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _careerMatchMetaText.verticalOverflow = VerticalWrapMode.Truncate;
            _careerMatchMetaText.resizeTextForBestFit = true;
            _careerMatchMetaText.resizeTextMinSize = 8;
            _careerMatchMetaText.resizeTextMaxSize = 10;
            _careerMatchMetaText.raycastTarget = false;
            _careerMatchMetaText.gameObject.SetActive(false);"""

    if "0.38f), new Vector2(0.63f, 0.64f)" not in text:
        text = must_replace(text, old_hud, new_hud)

    if "_clockText.transform.SetAsLastSibling()" not in text:
        old_mode = """            if (_careerMatchMetaText != null)
            {
                _careerMatchMetaText.gameObject.SetActive(!simulation);
                if (!simulation)
                    RefreshOfficialMatchMeta();
            }

            ApplyControlBarLayout(simulation);"""
        new_mode = """            if (_careerMatchMetaText != null)
            {
                _careerMatchMetaText.gameObject.SetActive(!simulation);
                if (!simulation)
                    RefreshOfficialMatchMeta();
            }
            if (_clockText != null)
                _clockText.transform.SetAsLastSibling();

            ApplyControlBarLayout(simulation);"""
        if old_mode in text:
            text = must_replace(text, old_mode, new_mode)
        else:
            # Already has a comment line before ApplyControlBarLayout
            old_mode2 = """            if (_careerMatchMetaText != null)
            {
                _careerMatchMetaText.gameObject.SetActive(!simulation);
                if (!simulation)
                    RefreshOfficialMatchMeta();
            }

            // simulation=true → practice (wide bar); false → official (compact centered)
            ApplyControlBarLayout(simulation);"""
            new_mode2 = """            if (_careerMatchMetaText != null)
            {
                _careerMatchMetaText.gameObject.SetActive(!simulation);
                if (!simulation)
                    RefreshOfficialMatchMeta();
            }
            if (_clockText != null)
                _clockText.transform.SetAsLastSibling();

            // simulation=true → practice (wide bar); false → official (compact centered)
            ApplyControlBarLayout(simulation);"""
            if old_mode2 in text:
                text = must_replace(text, old_mode2, new_mode2)

    return text


def main() -> None:
    bak.backup("before_battle_layout")
    text = TARGET.read_text(encoding="utf-8")
    if "擊殺廣播" not in text:
        raise RuntimeError("UTF-8 corrupted")

    text = replace_apply_control_bar(text)
    text = restore_top_hud_clock(text)

    if "擊殺廣播" not in text:
        raise RuntimeError("UTF-8 corrupted after patch")
    if "ApplyControlBarLayout(!simulation)" in text:
        raise RuntimeError("inverted control bar call present")
    if "0.905f, 0.015f), new Vector2(0.985f, 0.100f)" not in text:
        raise RuntimeError("settings bottom-right missing")
    if "0.38f), new Vector2(0.63f, 0.64f)" not in text:
        raise RuntimeError("clock under ROUND missing")
    if "0.36f, 0.018f), new Vector2(0.64f, 0.095f)" not in text:
        raise RuntimeError("official centered bar missing")

    TARGET.write_text(text, encoding="utf-8", newline="\n")
    print("patched", TARGET)


if __name__ == "__main__":
    main()
