# -*- coding: utf-8 -*-
"""Kill feed: opaque per-row boxes, skull icon, enlarged headshot icon, no title panel."""
from pathlib import Path
import backup_quickmatch as bak

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing:\n{old[:200]!r}")
    return text.replace(old, new, 1)


def main() -> None:
    bak.backup("kill_feed_ui")
    text = TARGET.read_text(encoding="utf-8")
    if "擊殺廣播" not in text:
        raise RuntimeError("UTF-8 corrupted before patch")

    text = must_replace(
        text,
        """        private Sprite _killFeedIconSprite;
        private Sprite _naviLogoSprite;""",
        """        private Sprite _killFeedIconSprite;
        private Sprite _killFeedSkullSprite;
        private Sprite _naviLogoSprite;""",
    )

    text = must_replace(
        text,
        """        private sealed class KillFeedLine
        {
            public string Left;
            public string Right;
        }""",
        """        private sealed class KillFeedLine
        {
            public string Left;
            public string Right;
            public bool Headshot;
        }""",
    )

    text = must_replace(
        text,
        """            _killFeedIconSprite = Resources.Load<Sprite>("UI/Icons/kill_feed");
            _naviLogoSprite = Resources.Load<Sprite>("UI/Logos/navi");""",
        """            _killFeedIconSprite = Resources.Load<Sprite>("UI/Icons/kill_feed");
            _killFeedSkullSprite = Resources.Load<Sprite>("UI/Icons/kill_skull") ?? _killFeedIconSprite;
            _naviLogoSprite = Resources.Load<Sprite>("UI/Logos/navi");""",
    )

    text = must_replace(
        text,
        """            // Compact top-right kill feed (above equal-height side stats)
            Image killBlock = CreateUiObject<Image>("KillFeed", canvas.transform);
            SetRect(killBlock.rectTransform, new Vector2(0.835f, 0.795f), new Vector2(0.992f, 0.992f));
            killBlock.color = new Color(PanelInner.r, PanelInner.g, PanelInner.b, 0.72f);

            Text killLabel = CreateText("KillLabel", killBlock.transform, 12, TextAnchor.MiddleLeft, TextMuted);
            SetRect(killLabel.rectTransform, new Vector2(0.06f, 0.78f), new Vector2(0.94f, 0.96f));
            killLabel.text = "擊殺廣播";

            CreateKillFeedRows(killBlock.transform);""",
        """            // Compact top-right kill feed (above equal-height side stats)
            Image killBlock = CreateUiObject<Image>("KillFeed", canvas.transform);
            SetRect(killBlock.rectTransform, new Vector2(0.835f, 0.795f), new Vector2(0.992f, 0.992f));
            killBlock.color = Color.clear;
            killBlock.raycastTarget = false;

            CreateKillFeedRows(killBlock.transform);""",
    )

    text = must_replace(
        text,
        """            _killFeed.Add(new KillFeedLine { Left = left, Right = right });""",
        """            bool headshot = !string.IsNullOrEmpty(evt.message) &&
                evt.message.IndexOf("headshot", StringComparison.OrdinalIgnoreCase) >= 0;
            _killFeed.Add(new KillFeedLine { Left = left, Right = right, Headshot = headshot });""",
    )

    text = must_replace(
        text,
        """                    if (row.Icon != null)
                    {
                        row.Icon.enabled = true;
                        row.Icon.sprite = _killFeedIconSprite;
                    }""",
        """                    Image rowBg = row.Root != null ? row.Root.GetComponent<Image>() : null;
                    if (rowBg != null)
                        rowBg.color = new Color(0f, 0f, 0f, 1f);
                    if (row.Icon != null)
                    {
                        row.Icon.enabled = true;
                        bool headshot = line.Headshot;
                        row.Icon.sprite = headshot ? _killFeedIconSprite : _killFeedSkullSprite;
                        float iconSize = headshot ? 22f : 18f;
                        row.Icon.rectTransform.sizeDelta = new Vector2(iconSize, iconSize);
                    }""",
    )

    text = must_replace(
        text,
        """            const float top = 0.74f;
            const float bottom = 0.04f;""",
        """            const float top = 0.96f;
            const float bottom = 0.04f;""",
    )

    text = must_replace(
        text,
        """                icon.sprite = _killFeedIconSprite;
                icon.preserveAspect = true;
                icon.color = Color.white;
                icon.raycastTarget = false;
                icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0.55f, 0.5f);
                icon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                icon.rectTransform.sizeDelta = new Vector2(16f, 16f);""",
        """                icon.sprite = _killFeedSkullSprite;
                icon.preserveAspect = true;
                icon.color = Color.white;
                icon.raycastTarget = false;
                icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0.55f, 0.5f);
                icon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                icon.rectTransform.sizeDelta = new Vector2(18f, 18f);""",
    )

    TARGET.write_text(text, encoding="utf-8")
    after = TARGET.read_text(encoding="utf-8")
    if "kill_skull" not in after or "Headshot" not in after:
        raise RuntimeError("patch verification failed")
    print("patched kill feed UI")


if __name__ == "__main__":
    main()
