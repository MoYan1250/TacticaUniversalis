# -*- coding: utf-8 -*-
"""Hide map marker HP/armor bars entirely (no longer shown on the map)."""
from pathlib import Path
import backup_quickmatch as bak

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing:\n{old[:240]!r}")
    return text.replace(old, new, 1)


def main() -> None:
    bak.backup("before_hide_map_vitality_bars")
    text = TARGET.read_text(encoding="utf-8")
    if "跳過結算中" not in text:
        raise RuntimeError("UTF-8 anchor missing — abort")

    # Stop creating HP/armor bars under map markers.
    old_bars = """                    Image hpBg = CreateUiObject<Image>("HpBg", root.transform);
                    hpBg.color = new Color(0.05f, 0.05f, 0.05f, 0.85f);
                    hpBg.rectTransform.anchorMin = new Vector2(0.1f, -0.28f);
                    hpBg.rectTransform.anchorMax = new Vector2(0.9f, -0.08f);
                    hpBg.rectTransform.offsetMin = Vector2.zero;
                    hpBg.rectTransform.offsetMax = Vector2.zero;
                    Image hpFill = CreateUiObject<Image>("HpFill", hpBg.transform);
                    hpFill.color = new Color(0.25f, 0.82f, 0.35f, 0.95f);
                    Stretch(hpFill.rectTransform);
                    hpFill.type = Image.Type.Filled;
                    hpFill.fillMethod = Image.FillMethod.Horizontal;
                    hpFill.fillAmount = 1f;
                    Image armorBg = CreateUiObject<Image>("ArmorBg", root.transform);
                    armorBg.color = new Color(0.05f, 0.05f, 0.05f, 0.75f);
                    armorBg.rectTransform.anchorMin = new Vector2(0.1f, -0.42f);
                    armorBg.rectTransform.anchorMax = new Vector2(0.9f, -0.30f);
                    armorBg.rectTransform.offsetMin = Vector2.zero;
                    armorBg.rectTransform.offsetMax = Vector2.zero;
                    Image armorFill = CreateUiObject<Image>("ArmorFill", armorBg.transform);
                    armorFill.color = new Color(0.35f, 0.55f, 0.95f, 0.95f);
                    Stretch(armorFill.rectTransform);
                    armorFill.type = Image.Type.Filled;
                    armorFill.fillMethod = Image.FillMethod.Horizontal;
                    armorFill.fillAmount = 0f;

                    _markers[id] = rootRect;
                    _markerDots[id] = marker;
                    _markerLabels[id] = label;
                    _markerHpFills[id] = hpFill;
                    _markerArmorFills[id] = armorFill;
                    _playerZones[id] = "t_spawn";"""

    new_bars = """                    // Map markers are number dots only; HP/armor live in the side scoreboard.
                    _markers[id] = rootRect;
                    _markerDots[id] = marker;
                    _markerLabels[id] = label;
                    _playerZones[id] = "t_spawn";"""

    text = must_replace(text, old_bars, new_bars)

    text = must_replace(
        text,
        """        private void RefreshMarkerHpBarsVisible()
        {
            bool show = IsGeneralBattleMode();
            foreach (Image fill in _markerHpFills.Values)
            {
                if (fill != null && fill.transform.parent != null)
                    fill.transform.parent.gameObject.SetActive(show);
            }
            foreach (Image fill in _markerArmorFills.Values)
            {
                if (fill != null && fill.transform.parent != null)
                    fill.transform.parent.gameObject.SetActive(show);
            }
        }""",
        """        private void RefreshMarkerHpBarsVisible()
        {
            // Intentionally unused: vitality bars were removed from map markers.
        }""",
    )

    TARGET.write_text(text, encoding="utf-8")
    print("patched hide map vitality bars OK")


if __name__ == "__main__":
    main()
