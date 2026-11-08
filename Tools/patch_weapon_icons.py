# -*- coding: utf-8 -*-
"""Wire weapon icons into general scoreboard cards + kill feed (weapon + headshot)."""
from pathlib import Path
import backup_quickmatch as bak

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"
CSPROJ = Path(__file__).resolve().parents[1] / "TacticaUniversalis.Presentation.csproj"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing:\n{old[:280]!r}")
    return text.replace(old, new, 1)


def main() -> None:
    bak.backup("before_weapon_icons")
    text = TARGET.read_text(encoding="utf-8")
    if "跳過結算中" not in text:
        raise RuntimeError("UTF-8 anchor missing — abort")

    text = must_replace(
        text,
        """        private sealed class KillFeedLine
        {
            public string Left;
            public string Right;
            public bool Headshot;
            public float ExpireAtUnscaled;
        }""",
        """        private sealed class KillFeedLine
        {
            public string Left;
            public string Right;
            public string WeaponId;
            public bool Headshot;
            public float ExpireAtUnscaled;
        }""",
    )

    text = must_replace(
        text,
        """        private sealed class KillFeedRow
        {
            public GameObject Root;
            public Text Left;
            public Image Icon;
            public Text Right;
        }""",
        """        private sealed class KillFeedRow
        {
            public GameObject Root;
            public Text Left;
            public Image Icon;
            public Image HeadshotIcon;
            public Text Right;
        }""",
    )

    text = must_replace(
        text,
        """            bool headshot = !string.IsNullOrEmpty(evt.message) &&
                evt.message.IndexOf("headshot", StringComparison.OrdinalIgnoreCase) >= 0;
            _killFeed.Add(new KillFeedLine
            {
                Left = left,
                Right = right,
                Headshot = headshot,
                ExpireAtUnscaled = Time.unscaledTime + KillFeedVisibleSeconds
            });""",
        """            bool headshot = !string.IsNullOrEmpty(evt.message) &&
                evt.message.IndexOf("headshot", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!headshot && evt.reasonTags != null)
            {
                foreach (string tag in evt.reasonTags)
                {
                    if (tag != null && tag.IndexOf("headshot", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        headshot = true;
                        break;
                    }
                }
            }
            _killFeed.Add(new KillFeedLine
            {
                Left = left,
                Right = right,
                WeaponId = evt.weaponId,
                Headshot = headshot,
                ExpireAtUnscaled = Time.unscaledTime + KillFeedVisibleSeconds
            });""",
    )

    text = must_replace(
        text,
        """                    if (row.Icon != null)
                    {
                        row.Icon.enabled = true;
                        bool headshot = line.Headshot;
                        row.Icon.sprite = headshot ? _killFeedIconSprite : _killFeedSkullSprite;
                        // Transparent-bg sprites: keep white so alpha is preserved.
                        row.Icon.color = Color.white;
                        float iconSize = headshot ? 20f : 16f;
                        row.Icon.rectTransform.sizeDelta = new Vector2(iconSize, iconSize);
                    }""",
        """                    if (row.Icon != null)
                    {
                        Sprite weaponSprite = WeaponIconAtlas.Get(line.WeaponId);
                        row.Icon.enabled = true;
                        row.Icon.sprite = weaponSprite != null ? weaponSprite : _killFeedSkullSprite;
                        row.Icon.color = Color.white;
                        row.Icon.preserveAspect = true;
                        float iconW = weaponSprite != null ? 42f : 16f;
                        float iconH = weaponSprite != null ? 18f : 16f;
                        row.Icon.rectTransform.sizeDelta = new Vector2(iconW, iconH);
                    }
                    if (row.HeadshotIcon != null)
                    {
                        bool headshot = line.Headshot;
                        row.HeadshotIcon.enabled = headshot;
                        if (headshot)
                        {
                            row.HeadshotIcon.sprite = _killFeedIconSprite ?? _killFeedSkullSprite;
                            row.HeadshotIcon.color = Color.white;
                            row.HeadshotIcon.rectTransform.sizeDelta = new Vector2(18f, 18f);
                        }
                    }""",
    )

    text = must_replace(
        text,
                """                Image icon = CreateUiObject<Image>("Icon", rowRoot.transform);
                icon.sprite = _killFeedSkullSprite;
                icon.preserveAspect = true;
                icon.color = Color.white;
                icon.raycastTarget = false;
                icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0.55f, 0.5f);
                icon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                icon.rectTransform.sizeDelta = new Vector2(18f, 18f);
                icon.rectTransform.anchoredPosition = Vector2.zero;

                Text right = CreateText("Right", rowRoot.transform, 12, TextAnchor.MiddleLeft, KillFeedGray);
                SetRect(right.rectTransform, new Vector2(0.60f, 0f), new Vector2(0.98f, 1f));""",
                """                Image icon = CreateUiObject<Image>("Icon", rowRoot.transform);
                icon.sprite = _killFeedSkullSprite;
                icon.preserveAspect = true;
                icon.color = Color.white;
                icon.raycastTarget = false;
                icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0.50f, 0.5f);
                icon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                icon.rectTransform.sizeDelta = new Vector2(42f, 18f);
                icon.rectTransform.anchoredPosition = Vector2.zero;

                Image headshotIcon = CreateUiObject<Image>("HeadshotIcon", rowRoot.transform);
                headshotIcon.sprite = _killFeedIconSprite ?? _killFeedSkullSprite;
                headshotIcon.preserveAspect = true;
                headshotIcon.color = Color.white;
                headshotIcon.raycastTarget = false;
                headshotIcon.enabled = false;
                headshotIcon.rectTransform.anchorMin = headshotIcon.rectTransform.anchorMax = new Vector2(0.62f, 0.5f);
                headshotIcon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                headshotIcon.rectTransform.sizeDelta = new Vector2(18f, 18f);
                headshotIcon.rectTransform.anchoredPosition = Vector2.zero;

                Text right = CreateText("Right", rowRoot.transform, 12, TextAnchor.MiddleLeft, KillFeedGray);
                SetRect(right.rectTransform, new Vector2(0.68f, 0f), new Vector2(0.98f, 1f));""",
    )

    text = must_replace(
        text,
        """                SetRect(left.rectTransform, new Vector2(0.08f, 0f), new Vector2(0.50f, 1f));""",
        """                SetRect(left.rectTransform, new Vector2(0.02f, 0f), new Vector2(0.42f, 1f));""",
    )

    text = must_replace(
        text,
        """                _killFeedRows.Add(new KillFeedRow
                {
                    Root = rowRoot.gameObject,
                    Left = left,
                    Icon = icon,
                    Right = right,
                });""",
        """                _killFeedRows.Add(new KillFeedRow
                {
                    Root = rowRoot.gameObject,
                    Left = left,
                    Icon = icon,
                    HeadshotIcon = headshotIcon,
                    Right = right,
                });""",
    )

    # Pass weapon ids into general card bind data
    text = must_replace(
        text,
        """                string primaryName = string.Empty;
                string secondaryName = string.Empty;
                bool hasKit = false;""",
        """                string primaryName = string.Empty;
                string secondaryName = string.Empty;
                string primaryWeaponId = string.Empty;
                string secondaryWeaponId = string.Empty;
                bool hasKit = false;""",
    )

    text = must_replace(
        text,
        """                        primaryName = ResolveWeaponDisplayName(runtime.Loadout.PrimaryWeaponId);
                        secondaryName = ResolveWeaponDisplayName(runtime.Loadout.PistolWeaponId);
                        hasKit = !teamIsT && runtime.Loadout.HasDefuseKit;""",
        """                        primaryWeaponId = runtime.Loadout.PrimaryWeaponId ?? string.Empty;
                        secondaryWeaponId = runtime.Loadout.PistolWeaponId ?? string.Empty;
                        primaryName = ResolveWeaponDisplayName(primaryWeaponId);
                        secondaryName = ResolveWeaponDisplayName(secondaryWeaponId);
                        hasKit = !teamIsT && runtime.Loadout.HasDefuseKit;""",
    )

    # Find PrimaryWeaponName = primaryName assignment in card data and add ids
    text = must_replace(
        text,
        """                    PrimaryWeaponName = primaryName,
                    SecondaryWeaponName = secondaryName,""",
        """                    PrimaryWeaponName = primaryName,
                    SecondaryWeaponName = secondaryName,
                    PrimaryWeaponId = primaryWeaponId,
                    SecondaryWeaponId = secondaryWeaponId,""",
    )

    TARGET.write_text(text, encoding="utf-8")

    cs = CSPROJ.read_text(encoding="utf-8")
    if "WeaponIconAtlas.cs" not in cs:
        cs = cs.replace(
            '    <Compile Include="Assets\\_Project\\Scripts\\Presentation\\BattleStatPanelPresentation.cs" />\n',
            '    <Compile Include="Assets\\_Project\\Scripts\\Presentation\\BattleStatPanelPresentation.cs" />\n'
            '    <Compile Include="Assets\\_Project\\Scripts\\Presentation\\WeaponIconAtlas.cs" />\n',
        )
        CSPROJ.write_text(cs, encoding="utf-8")
        print("csproj updated")

    print("patched weapon icons OK")


if __name__ == "__main__":
    main()
