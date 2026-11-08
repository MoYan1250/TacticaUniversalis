# -*- coding: utf-8 -*-
"""Load kit/bomb utility icons; shrink kill-feed weapon icons to 80%."""
from pathlib import Path
import backup_quickmatch as bak

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing:\n{old[:240]!r}")
    return text.replace(old, new, 1)


def main() -> None:
    bak.backup("before_kit_bomb_weapon_scale")
    text = TARGET.read_text(encoding="utf-8")
    if "跳過結算中" not in text:
        raise RuntimeError("UTF-8 anchor missing — abort")

    text = must_replace(
        text,
        """            _statIcons = LoadStatIcons();
            _killFeedIconSprite = Resources.Load<Sprite>("UI/Icons/kill_feed");""",
        """            _statIcons = LoadStatIcons();
            BattleStatPanelPresentation.SetKitAndBombIcons(
                Resources.Load<Sprite>("UI/Icons/Weapons/defuse_kit"),
                Resources.Load<Sprite>("UI/Icons/Weapons/bomb"));
            _killFeedIconSprite = Resources.Load<Sprite>("UI/Icons/kill_feed");""",
    )

    text = must_replace(
        text,
        """                        float iconW = weaponSprite != null ? 42f : 16f;
                        float iconH = weaponSprite != null ? 18f : 16f;
                        row.Icon.rectTransform.sizeDelta = new Vector2(iconW, iconH);""",
        """                        float iconW = weaponSprite != null ? 42f * BattleStatPanelPresentation.WeaponIconScale : 16f;
                        float iconH = weaponSprite != null ? 18f * BattleStatPanelPresentation.WeaponIconScale : 16f;
                        row.Icon.rectTransform.sizeDelta = new Vector2(iconW, iconH);""",
    )

    text = must_replace(
        text,
        """                icon.rectTransform.sizeDelta = new Vector2(42f, 18f);""",
        """                icon.rectTransform.sizeDelta = new Vector2(
                    42f * BattleStatPanelPresentation.WeaponIconScale,
                    18f * BattleStatPanelPresentation.WeaponIconScale);""",
    )

    TARGET.write_text(text, encoding="utf-8")
    print("patched kit/bomb + weapon scale OK")


if __name__ == "__main__":
    main()
