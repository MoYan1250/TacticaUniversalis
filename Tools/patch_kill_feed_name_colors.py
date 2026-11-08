# -*- coding: utf-8 -*-
"""Kill feed: CT/T name colors + white icon tint for transparent sprites."""
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing:\n{old[:240]!r}")
    return text.replace(old, new, 1)


def main() -> None:
    subprocess.check_call(
        [sys.executable, str(ROOT / "Tools" / "backup_quickmatch.py"), "kill_feed_name_colors"]
    )
    text = TARGET.read_text(encoding="utf-8")
    if "擊殺廣播" not in text and "KillFeed" not in text:
        raise RuntimeError("refusing: file looks corrupted")

    # Add hex constant before AddKillFeed uses it (do not gate on substring already present in body).
    if 'private const string KillFeedGrayHex = "9EA3AB";' not in text:
        text = must_replace(
            text,
            """        private static readonly Color KillFeedGray = new Color(0.62f, 0.64f, 0.67f, 1f);
        private static readonly Color KillFeedRowBg = new Color(0.06f, 0.07f, 0.08f, 0.72f);""",
            """        private static readonly Color KillFeedGray = new Color(0.62f, 0.64f, 0.67f, 1f);
        private const string KillFeedGrayHex = "9EA3AB";
        private static readonly Color KillFeedRowBg = new Color(0.06f, 0.07f, 0.08f, 0.72f);""",
        )

    text = must_replace(
        text,
        """        private void AddKillFeed(SimulationEvent evt)
        {
            int round = Mathf.Max(1, evt.roundNumber);
            string killer = GrayPlayerName(evt.actorId);
            string victim = GrayPlayerName(evt.targetId);
            var assists = new List<string>();
            if (evt.reasonTags != null)
            {
                foreach (string tag in evt.reasonTags)
                {
                    if (tag == null || !tag.StartsWith("assist:", StringComparison.OrdinalIgnoreCase))
                        continue;
                    string assistId = tag.Substring("assist:".Length);
                    if (!string.IsNullOrEmpty(assistId))
                        assists.Add(GrayPlayerName(assistId));
                }
            }

            string left = killer;
            if (assists.Count > 0)
                left += " +" + string.Join("+", assists);

            string right = victim;
            if (evt.reasonTags != null)
            {
                foreach (string tag in evt.reasonTags)
                {
                    if (tag != null && tag.StartsWith("kill_reward:", StringComparison.OrdinalIgnoreCase) &&
                        int.TryParse(tag.Substring("kill_reward:".Length), out int reward) && reward > 0)
                    {
                        right += $"  +${reward}";
                        break;
                    }
                }
            }

            bool headshot = !string.IsNullOrEmpty(evt.message) &&
                evt.message.IndexOf("headshot", StringComparison.OrdinalIgnoreCase) >= 0;
            _killFeed.Add(new KillFeedLine
            {
                Left = left,
                Right = right,
                Headshot = headshot,
                ExpireAtUnscaled = Time.unscaledTime + KillFeedVisibleSeconds
            });
            while (_killFeed.Count > 5)
                _killFeed.RemoveAt(0);
            UpdateKillFeed();
        }

        private static string GrayPlayerName(string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
                return string.Empty;
            return DemoZh.PlayerName(playerId);
        }

        private void PruneExpiredKillFeed()""",
        """        private void AddKillFeed(SimulationEvent evt)
        {
            int round = Mathf.Max(1, evt.roundNumber);
            string killer = ColoredPlayerName(evt.actorId, round);
            string victim = ColoredPlayerName(evt.targetId, round);
            var assists = new List<string>();
            if (evt.reasonTags != null)
            {
                foreach (string tag in evt.reasonTags)
                {
                    if (tag == null || !tag.StartsWith("assist:", StringComparison.OrdinalIgnoreCase))
                        continue;
                    string assistId = tag.Substring("assist:".Length);
                    if (!string.IsNullOrEmpty(assistId))
                        assists.Add(ColoredPlayerName(assistId, round));
                }
            }

            string left = killer;
            if (assists.Count > 0)
                left += $" <color=#{KillFeedGrayHex}>+</color>" + string.Join(
                    $"<color=#{KillFeedGrayHex}>+</color>", assists);

            string right = victim;
            if (evt.reasonTags != null)
            {
                foreach (string tag in evt.reasonTags)
                {
                    if (tag != null && tag.StartsWith("kill_reward:", StringComparison.OrdinalIgnoreCase) &&
                        int.TryParse(tag.Substring("kill_reward:".Length), out int reward) && reward > 0)
                    {
                        right += $"  <color=#C9A227>+${reward}</color>";
                        break;
                    }
                }
            }

            bool headshot = !string.IsNullOrEmpty(evt.message) &&
                evt.message.IndexOf("headshot", StringComparison.OrdinalIgnoreCase) >= 0;
            _killFeed.Add(new KillFeedLine
            {
                Left = left,
                Right = right,
                Headshot = headshot,
                ExpireAtUnscaled = Time.unscaledTime + KillFeedVisibleSeconds
            });
            while (_killFeed.Count > 5)
                _killFeed.RemoveAt(0);
            UpdateKillFeed();
        }

        private void PruneExpiredKillFeed()""",
    )

    text = must_replace(
        text,
        """                    if (row.Left != null)
                    {
                        row.Left.text = line.Left ?? string.Empty;
                        row.Left.color = KillFeedGray;
                    }
                    if (row.Right != null)
                    {
                        row.Right.text = line.Right ?? string.Empty;
                        row.Right.color = KillFeedGray;
                    }
                    Image rowBg = row.Root.GetComponent<Image>();
                    if (rowBg != null)
                        rowBg.color = KillFeedRowBg;
                    if (row.Icon != null)
                    {
                        row.Icon.enabled = true;
                        bool headshot = line.Headshot;
                        row.Icon.sprite = headshot ? _killFeedIconSprite : _killFeedSkullSprite;
                        row.Icon.color = KillFeedGray;
                        float iconSize = headshot ? 20f : 16f;
                        row.Icon.rectTransform.sizeDelta = new Vector2(iconSize, iconSize);
                    }""",
        """                    if (row.Left != null)
                    {
                        row.Left.text = line.Left ?? string.Empty;
                        // White base so rich-text CT/T colors are not gray-tinted.
                        row.Left.color = Color.white;
                    }
                    if (row.Right != null)
                    {
                        row.Right.text = line.Right ?? string.Empty;
                        row.Right.color = Color.white;
                    }
                    Image rowBg = row.Root.GetComponent<Image>();
                    if (rowBg != null)
                        rowBg.color = KillFeedRowBg;
                    if (row.Icon != null)
                    {
                        row.Icon.enabled = true;
                        bool headshot = line.Headshot;
                        row.Icon.sprite = headshot ? _killFeedIconSprite : _killFeedSkullSprite;
                        // Transparent-bg sprites: keep white so alpha is preserved.
                        row.Icon.color = Color.white;
                        float iconSize = headshot ? 20f : 16f;
                        row.Icon.rectTransform.sizeDelta = new Vector2(iconSize, iconSize);
                    }""",
    )

    text = must_replace(
        text,
        """                icon.sprite = _killFeedSkullSprite;
                icon.preserveAspect = true;
                icon.color = KillFeedGray;
                icon.raycastTarget = false;""",
        """                icon.sprite = _killFeedSkullSprite;
                icon.preserveAspect = true;
                icon.color = Color.white;
                icon.raycastTarget = false;""",
    )

    TARGET.write_text(text, encoding="utf-8")
    assert "ColoredPlayerName(evt.actorId, round)" in text
    assert "GrayPlayerName" not in text
    assert "擊殺廣播" in text or "KillFeed" in text
    print("patched", TARGET)


if __name__ == "__main__":
    main()
