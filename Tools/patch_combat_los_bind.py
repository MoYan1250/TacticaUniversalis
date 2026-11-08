# -*- coding: utf-8 -*-
"""Bind MapCollisionGrid to InteractiveQuickMatchSession.CombatLos after RebuildMapCollision."""
from pathlib import Path
import backup_quickmatch as bak

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing:\n{old[:220]!r}")
    return text.replace(old, new, 1)


def apply(text: str) -> str:
    text = must_replace(
        text,
        """        private void RebuildMapCollision()
        {
            GetMapContentSize(out float width, out float height);
            if (width < 8f || height < 8f)
                return;
            _collisionMapSize = new Vector2(width, height);
            _mapCollision.Rebuild(_mapSprite, width, height);
            RefreshAllMarkerLos();
        }
""",
        """        private void RebuildMapCollision()
        {
            GetMapContentSize(out float width, out float height);
            if (width < 8f || height < 8f)
                return;
            _collisionMapSize = new Vector2(width, height);
            _mapCollision.Rebuild(_mapSprite, width, height);
            if (_session != null)
                _session.CombatLos = _mapCollision.IsReady ? _mapCollision : null;
            RefreshAllMarkerLos();
        }
""",
    )
    return text


def main() -> None:
    bak.backup("combat_los_bind")
    original = TARGET.read_text(encoding="utf-8")
    updated = apply(original)
    if "擊殺" not in updated or "跳過" not in updated:
        raise RuntimeError("Chinese anchors lost")
    if "ApplyControlBarLayout(simulation)" not in updated:
        raise RuntimeError("control bar polarity lost")
    if "_session.CombatLos" not in updated:
        raise RuntimeError("CombatLos bind missing")
    TARGET.write_text(updated, encoding="utf-8")
    print("patched", TARGET)


if __name__ == "__main__":
    main()
