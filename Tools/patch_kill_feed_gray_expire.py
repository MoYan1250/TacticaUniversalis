# -*- coding: utf-8 -*-
"""Kill feed: gray text, separated rows, expire after 5 seconds. UTF-8 safe."""
from pathlib import Path
from datetime import datetime
import shutil

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"
BACKUP_DIR = ROOT / "Tools" / "backups"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing:\n{old[:200]!r}")
    return text.replace(old, new, 1)


def backup(text: str) -> None:
    # Prefer kill-feed / battle Chinese anchors; fall back to structural markers.
    if "特性觸發" not in text and "叫暫停" not in text and "KillFeed" not in text:
        raise RuntimeError("refusing to backup: file looks corrupted")
    BACKUP_DIR.mkdir(parents=True, exist_ok=True)
    stamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    dest = BACKUP_DIR / f"QuickMatchDemoController_{stamp}_before_kill_feed_fade.cs"
    shutil.copy2(TARGET, dest)
    print("backed up", dest)


def main() -> None:
    text = TARGET.read_text(encoding="utf-8")
    backup(text)

    # --- KillFeedLine: track spawn time ---
    text = must_replace(
        text,
        """        private sealed class KillFeedLine
        {
            public string Left;
            public string Right;
            public bool Headshot;
        }""",
        """        private sealed class KillFeedLine
        {
            public string Left;
            public string Right;
            public bool Headshot;
            public float ExpireAtUnscaled;
        }

        private const float KillFeedVisibleSeconds = 5f;
        private static readonly Color KillFeedGray = new Color(0.62f, 0.64f, 0.67f, 1f);
        private static readonly Color KillFeedRowBg = new Color(0.06f, 0.07f, 0.08f, 0.72f);""",
    )

    # --- Update: prune expired kill feed ---
    if "PruneExpiredKillFeed" not in text:
        text = must_replace(
            text,
            """        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape))
                return;""",
            """        private void Update()
        {
            PruneExpiredKillFeed();

            if (!Input.GetKeyDown(KeyCode.Escape))
                return;""",
        )

    # --- AddKillFeed: gray names + expire time ---
    text = must_replace(
        text,
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
                left += " +" + string.Join("+", assists);

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
            _killFeed.Add(new KillFeedLine { Left = left, Right = right, Headshot = headshot });
            while (_killFeed.Count > 5)
                _killFeed.RemoveAt(0);
            UpdateKillFeed();
        }""",
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

        private void PruneExpiredKillFeed()
        {
            if (_killFeed.Count == 0)
                return;
            float now = Time.unscaledTime;
            int removed = _killFeed.RemoveAll(x => x == null || now >= x.ExpireAtUnscaled);
            if (removed > 0)
                UpdateKillFeed();
        }""",
    )

    # --- UpdateKillFeed: gray text + separate row chrome ---
    text = must_replace(
        text,
        """        private void UpdateKillFeed()
        {
            for (int i = 0; i < _killFeedRows.Count; i++)
            {
                KillFeedRow row = _killFeedRows[i];
                if (row?.Root == null)
                    continue;
                if (i < _killFeed.Count)
                {
                    KillFeedLine line = _killFeed[i];
                    row.Root.SetActive(true);
                    if (row.Left != null)
                        row.Left.text = line.Left ?? string.Empty;
                    if (row.Right != null)
                        row.Right.text = line.Right ?? string.Empty;
                    Image rowBg = row.Root != null ? row.Root.GetComponent<Image>() : null;
                    if (rowBg != null)
                        rowBg.color = new Color(0f, 0f, 0f, 1f);
                    if (row.Icon != null)
                    {
                        row.Icon.enabled = true;
                        bool headshot = line.Headshot;
                        row.Icon.sprite = headshot ? _killFeedIconSprite : _killFeedSkullSprite;
                        float iconSize = headshot ? 22f : 18f;
                        row.Icon.rectTransform.sizeDelta = new Vector2(iconSize, iconSize);
                    }
                }
                else
                {
                    row.Root.SetActive(false);
                }
            }
        }""",
        """        private void UpdateKillFeed()
        {
            for (int i = 0; i < _killFeedRows.Count; i++)
            {
                KillFeedRow row = _killFeedRows[i];
                if (row?.Root == null)
                    continue;
                if (i < _killFeed.Count)
                {
                    KillFeedLine line = _killFeed[i];
                    row.Root.SetActive(true);
                    if (row.Left != null)
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
                    }
                }
                else
                {
                    row.Root.SetActive(false);
                }
            }
        }""",
    )

    # --- CreateKillFeedRows: gaps between rows ---
    text = must_replace(
        text,
        """        private void CreateKillFeedRows(Transform parent)
        {
            _killFeedRows.Clear();
            const int maxLines = 5;
            const float top = 0.96f;
            const float bottom = 0.04f;
            float rowH = (top - bottom) / maxLines;
            for (int i = 0; i < maxLines; i++)
            {
                float y1 = top - i * rowH;
                float y0 = y1 - rowH;
                Image rowRoot = CreateUiObject<Image>("KillRow" + i, parent);
                rowRoot.color = Color.clear;
                rowRoot.raycastTarget = false;
                SetRect(rowRoot.rectTransform, new Vector2(0.03f, y0), new Vector2(0.97f, y1));

                Text left = CreateText("Left", rowRoot.transform, 12, TextAnchor.MiddleRight, TextPrimary);""",
        """        private void CreateKillFeedRows(Transform parent)
        {
            _killFeedRows.Clear();
            const int maxLines = 5;
            const float top = 0.98f;
            const float bottom = 0.02f;
            const float gap = 0.018f;
            float rowH = (top - bottom - gap * (maxLines - 1)) / maxLines;
            for (int i = 0; i < maxLines; i++)
            {
                float y1 = top - i * (rowH + gap);
                float y0 = y1 - rowH;
                Image rowRoot = CreateUiObject<Image>("KillRow" + i, parent);
                rowRoot.color = KillFeedRowBg;
                rowRoot.raycastTarget = false;
                SetRect(rowRoot.rectTransform, new Vector2(0.03f, y0), new Vector2(0.97f, y1));

                Text left = CreateText("Left", rowRoot.transform, 12, TextAnchor.MiddleRight, KillFeedGray);""",
    )

    text = must_replace(
        text,
        """                Text right = CreateText("Right", rowRoot.transform, 12, TextAnchor.MiddleLeft, TextPrimary);
                SetRect(right.rectTransform, new Vector2(0.60f, 0f), new Vector2(0.98f, 1f));
                right.supportRichText = true;
                right.horizontalOverflow = HorizontalWrapMode.Overflow;
                right.verticalOverflow = VerticalWrapMode.Truncate;
                right.resizeTextForBestFit = true;
                right.resizeTextMinSize = 9;
                right.resizeTextMaxSize = 12;
                right.raycastTarget = false;
                right.text = string.Empty;

                rowRoot.gameObject.SetActive(false);""",
        """                Text right = CreateText("Right", rowRoot.transform, 12, TextAnchor.MiddleLeft, KillFeedGray);
                SetRect(right.rectTransform, new Vector2(0.60f, 0f), new Vector2(0.98f, 1f));
                right.supportRichText = true;
                right.horizontalOverflow = HorizontalWrapMode.Overflow;
                right.verticalOverflow = VerticalWrapMode.Truncate;
                right.resizeTextForBestFit = true;
                right.resizeTextMinSize = 9;
                right.resizeTextMaxSize = 12;
                right.raycastTarget = false;
                right.text = string.Empty;

                rowRoot.gameObject.SetActive(false);""",
    )

    # Icon default gray
    text = must_replace(
        text,
        """                icon.sprite = _killFeedSkullSprite;
                icon.preserveAspect = true;
                icon.color = Color.white;
                icon.raycastTarget = false;""",
        """                icon.sprite = _killFeedSkullSprite;
                icon.preserveAspect = true;
                icon.color = KillFeedGray;
                icon.raycastTarget = false;""",
    )

    if "PruneExpiredKillFeed" not in text:
        raise RuntimeError("prune helper missing")
    if "KillFeedVisibleSeconds" not in text:
        raise RuntimeError("ttl const missing")
    if "GrayPlayerName" not in text:
        raise RuntimeError("gray name helper missing")
    if "特性觸發" not in text and "叫暫停" not in text:
        raise RuntimeError("UTF-8 corrupted after patch")

    TARGET.write_text(text, encoding="utf-8", newline="\n")
    print("patched", TARGET)


if __name__ == "__main__":
    main()
