# -*- coding: utf-8 -*-
"""
General mode: merge settings into centered bottom control bar (not free-floating).
Keep Space pause. Raise scoreboards only enough to clear the centered bar.
"""
from pathlib import Path
import backup_quickmatch as bak

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError("missing:\n" + old[:320])
    return text.replace(old, new, 1)


def main() -> None:
    bak.backup("before_general_bar_center_settings")
    text = TARGET.read_text(encoding="utf-8")
    if "特性觸發" not in text or "KeyCode.Space" not in text:
        raise RuntimeError("UTF-8 corrupted or space pause missing")

    text = must_replace(
        text,
        """        private void ApplyControlBarLayout(bool practiceLayout)
        {
            // Settings always bottom-right, clear of the control pack and side scoreboards.
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
                if (_floorButtonRect != null)
                    SetRect(_floorButtonRect, new Vector2(0.79f, 0.15f), new Vector2(0.855f, 0.85f));
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
            if (_floorButtonRect != null)
                SetRect(_floorButtonRect, new Vector2(0.77f, 0.14f), new Vector2(0.97f, 0.86f));
        }
""",
        """        private void ApplyControlBarLayout(bool practiceLayout)
        {
            bool general = IsGeneralBattleMode();

            if (general)
            {
                // Merge settings into the bottom bar and keep the whole pack screen-centered.
                if (_settingsButtonRect != null && _controlBarRect != null)
                    _settingsButtonRect.SetParent(_controlBarRect, false);
                if (_controlBarRect != null)
                    SetRect(_controlBarRect, new Vector2(0.30f, 0.018f), new Vector2(0.70f, 0.095f));

                // L→R inside bar: 叫暫停 | 暫停 | 倍速 | 跳過/樓層 | 設定
                if (_matchText != null)
                    _matchText.gameObject.SetActive(false);
                if (_timeoutButtonRect != null)
                    SetRect(_timeoutButtonRect, new Vector2(0.02f, 0.14f), new Vector2(0.28f, 0.86f));
                if (_pauseButtonRect != null)
                    SetRect(_pauseButtonRect, new Vector2(0.30f, 0.14f), new Vector2(0.44f, 0.86f));
                if (_speedButtonRect != null)
                    SetRect(_speedButtonRect, new Vector2(0.46f, 0.14f), new Vector2(0.60f, 0.86f));
                if (_skipButtonRect != null)
                    SetRect(_skipButtonRect, new Vector2(0.62f, 0.14f), new Vector2(0.76f, 0.86f));
                if (_floorButtonRect != null)
                    SetRect(_floorButtonRect, new Vector2(0.62f, 0.14f), new Vector2(0.76f, 0.86f));
                if (_settingsButtonRect != null)
                    SetRect(_settingsButtonRect, new Vector2(0.78f, 0.14f), new Vector2(0.98f, 0.86f));
                return;
            }

            // Quick / official: settings stays outside bottom-right on the canvas.
            if (_settingsButtonRect != null && _canvasRect != null)
                _settingsButtonRect.SetParent(_canvasRect, false);
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
                if (_floorButtonRect != null)
                    SetRect(_floorButtonRect, new Vector2(0.79f, 0.15f), new Vector2(0.855f, 0.85f));
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
            if (_floorButtonRect != null)
                SetRect(_floorButtonRect, new Vector2(0.77f, 0.14f), new Vector2(0.97f, 0.86f));
        }
""",
    )

    # General scoreboards can extend lower again; centered bar no longer sits in the corners.
    text = must_replace(
        text,
        """                if (_leftStatsPanel != null)
                {
                    // Stay above bottom control bar + settings (same clearance as classic).
                    SetRect(_leftStatsPanel.rectTransform, new Vector2(0.006f, 0.112f), new Vector2(0.205f, 0.700f));
                    _leftStatsPanel.color = Color.clear;
                }
                if (_rightStatsPanel != null)
                {
                    SetRect(_rightStatsPanel.rectTransform, new Vector2(0.795f, 0.112f), new Vector2(0.994f, 0.700f));
                    _rightStatsPanel.color = Color.clear;
                }
""",
        """                if (_leftStatsPanel != null)
                {
                    SetRect(_leftStatsPanel.rectTransform, new Vector2(0.006f, 0.018f), new Vector2(0.205f, 0.700f));
                    _leftStatsPanel.color = Color.clear;
                }
                if (_rightStatsPanel != null)
                {
                    SetRect(_rightStatsPanel.rectTransform, new Vector2(0.795f, 0.018f), new Vector2(0.994f, 0.700f));
                    _rightStatsPanel.color = Color.clear;
                }
""",
    )

    TARGET.write_text(text, encoding="utf-8")
    verify = TARGET.read_text(encoding="utf-8")
    if "特性觸發" not in verify or "KeyCode.Space" not in verify:
        raise RuntimeError("UTF-8 lost or space pause removed")
    if "SetParent(_controlBarRect, false)" not in verify:
        raise RuntimeError("settings not merged into control bar")
    if "0.30f, 0.018f), new Vector2(0.70f, 0.095f)" not in verify:
        raise RuntimeError("centered general bar missing")
    print("patched general centered bar + merged settings OK")


if __name__ == "__main__":
    main()
