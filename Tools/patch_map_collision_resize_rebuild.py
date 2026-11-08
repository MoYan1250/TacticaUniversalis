# -*- coding: utf-8 -*-
"""Rebuild collision grid when map rect size becomes valid / changes."""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "Tools"))
from backup_quickmatch import backup  # noqa: E402

SRC = ROOT / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str, label: str) -> str:
    if old not in text:
        raise RuntimeError("missing: " + label)
    return text.replace(old, new, 1)


def main() -> int:
    backup("map_collision_resize")
    text = SRC.read_text(encoding="utf-8")
    text = must_replace(
        text,
        """        private readonly MapCollisionGrid _mapCollision = new MapCollisionGrid();
        private readonly Vector2[] _coneRimScratch = new Vector2[24];
""",
        """        private readonly MapCollisionGrid _mapCollision = new MapCollisionGrid();
        private readonly Vector2[] _coneRimScratch = new Vector2[24];
        private Vector2 _collisionMapSize;
""",
        "collision map size field",
    )
    text = must_replace(
        text,
        """        private void RebuildMapCollision()
        {
            GetMapContentSize(out float width, out float height);
            _mapCollision.Rebuild(_mapSprite, width, height);
            RefreshAllMarkerLos();
        }

        private void TickMarkerLos()
        {
            if (!IsGeneralBattleMode() || !_mapCollision.IsReady)
                return;
""",
        """        private void RebuildMapCollision()
        {
            GetMapContentSize(out float width, out float height);
            if (width < 8f || height < 8f)
                return;
            _collisionMapSize = new Vector2(width, height);
            _mapCollision.Rebuild(_mapSprite, width, height);
            RefreshAllMarkerLos();
        }

        private void TickMarkerLos()
        {
            if (!IsGeneralBattleMode())
                return;
            GetMapContentSize(out float width, out float height);
            if (!_mapCollision.IsReady ||
                Mathf.Abs(width - _collisionMapSize.x) > 1f ||
                Mathf.Abs(height - _collisionMapSize.y) > 1f)
                RebuildMapCollision();
            if (!_mapCollision.IsReady)
                return;
""",
        "rebuild when map size ready",
    )
    SRC.write_text(text, encoding="utf-8")
    check = SRC.read_text(encoding="utf-8")
    if "_collisionMapSize" not in check or "ApplyControlBarLayout(!simulation)" in check:
        raise RuntimeError("patch failed or polarity broken")
    if "叫暫停" not in check:
        raise RuntimeError("utf8 damaged")
    print("patched collision resize rebuild")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
