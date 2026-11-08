# -*- coding: utf-8 -*-
"""Shrink official bottom control bar to fit only 4 centered buttons."""
from pathlib import Path

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def main() -> None:
    text = TARGET.read_text(encoding="utf-8")
    if "擊殺廣播" not in text:
        raise RuntimeError("UTF-8 corrupted")

    start = text.find("        private void ApplyControlBarLayout(bool practiceLayout)")
    end = text.find("        private void RefreshOfficialMatchMeta()", start)
    if start < 0:
        raise RuntimeError("ApplyControlBarLayout missing")
    if end < 0:
        end = text.find("        private void ApplySettingsFrostedChrome(bool enabled)", start)
    if end < 0:
        raise RuntimeError("end anchor missing")

    new_block = """        private void ApplyControlBarLayout(bool practiceLayout)
        {
            // Settings always stay bottom-right (original slot).
            if (_settingsButtonRect != null)
                SetRect(_settingsButtonRect, new Vector2(0.905f, 0.015f), new Vector2(0.985f, 0.100f));

            if (_controlBarRect != null)
            {
                if (practiceLayout)
                    SetRect(_controlBarRect, new Vector2(0.12f, 0.015f), new Vector2(0.88f, 0.100f));
                else
                    // Official: only 4 buttons — short bar, screen-centered.
                    SetRect(_controlBarRect, new Vector2(0.38f, 0.020f), new Vector2(0.62f, 0.088f));
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

            // L→R: 叫暫停 | 暫停 | 倍速 | 跳過 (tight pack, no empty wings)
            if (_timeoutButtonRect != null)
                SetRect(_timeoutButtonRect, new Vector2(0.02f, 0.12f), new Vector2(0.34f, 0.88f));
            if (_pauseButtonRect != null)
                SetRect(_pauseButtonRect, new Vector2(0.36f, 0.12f), new Vector2(0.54f, 0.88f));
            if (_speedButtonRect != null)
                SetRect(_speedButtonRect, new Vector2(0.56f, 0.12f), new Vector2(0.74f, 0.88f));
            if (_skipButtonRect != null)
                SetRect(_skipButtonRect, new Vector2(0.76f, 0.12f), new Vector2(0.98f, 0.88f));
        }

"""

    text = text[:start] + new_block + text[end:]
    TARGET.write_text(text, encoding="utf-8", newline="\n")
    print("patched", TARGET)


if __name__ == "__main__":
    main()
