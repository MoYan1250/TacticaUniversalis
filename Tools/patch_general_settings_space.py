# -*- coding: utf-8 -*-
"""
General mode bottom UI:
- Settings back to bottom-right (no overlap with control bar / scoreboards)
- Raise side scoreboards above the bottom bar
- Space toggles pause
"""
from pathlib import Path
import backup_quickmatch as bak

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError("missing:\n" + old[:300])
    return text.replace(old, new, 1)


def main() -> None:
    bak.backup("before_general_settings_space")
    text = TARGET.read_text(encoding="utf-8")
    if "特性觸發" not in text:
        raise RuntimeError("UTF-8 corrupted")

    text = must_replace(
        text,
        """        private void ApplyControlBarLayout(bool practiceLayout)
        {
            // General mode: settings sits in the center control UI so side scoreboards can use the bottom.
            // Other modes: keep settings bottom-right outside the compact official bar.
            if (_settingsButtonRect != null)
            {
                if (IsGeneralBattleMode())
                    SetRect(_settingsButtonRect, new Vector2(0.455f, 0.018f), new Vector2(0.545f, 0.095f));
                else
                    SetRect(_settingsButtonRect, new Vector2(0.905f, 0.015f), new Vector2(0.985f, 0.100f));
            }
""",
        """        private void ApplyControlBarLayout(bool practiceLayout)
        {
            // Settings always bottom-right, clear of the control pack and side scoreboards.
            if (_settingsButtonRect != null)
                SetRect(_settingsButtonRect, new Vector2(0.905f, 0.015f), new Vector2(0.985f, 0.100f));
""",
    )

    text = must_replace(
        text,
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
    )

    text = must_replace(
        text,
        """        private void Update()
        {
            PruneExpiredKillFeed();

            if (!Input.GetKeyDown(KeyCode.Escape))
                return;

            if (_settingsPanel != null && _settingsPanel.IsVisible)
            {
                HideSettingsPanel();
                return;
            }
""",
        """        private void Update()
        {
            PruneExpiredKillFeed();

            if (Input.GetKeyDown(KeyCode.Space) &&
                (_settingsPanel == null || !_settingsPanel.IsVisible))
            {
                TogglePause();
            }

            if (!Input.GetKeyDown(KeyCode.Escape))
                return;

            if (_settingsPanel != null && _settingsPanel.IsVisible)
            {
                HideSettingsPanel();
                return;
            }
""",
    )

    TARGET.write_text(text, encoding="utf-8")
    verify = TARGET.read_text(encoding="utf-8")
    if "特性觸發" not in verify:
        raise RuntimeError("UTF-8 lost")
    if "0.455f, 0.018f" in verify:
        raise RuntimeError("centered settings still present")
    if "KeyCode.Space" not in verify:
        raise RuntimeError("space pause missing")
    if "0.006f, 0.112f" not in verify:
        raise RuntimeError("general scoreboard bottom clearance missing")
    print("patched general settings + space pause OK")


if __name__ == "__main__":
    main()
