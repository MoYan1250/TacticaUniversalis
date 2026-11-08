# -*- coding: utf-8 -*-
from pathlib import Path

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"
OLD = 'GameObject coneMeshRoot = new GameObject("ViewConeMesh", typeof(RectTransform));'
NEW = 'GameObject coneMeshRoot = new GameObject("ViewConeMesh", typeof(RectTransform), typeof(CanvasRenderer));'


def main() -> int:
    text = TARGET.read_text(encoding="utf-8")
    if NEW in text:
        print("already patched")
        return 0
    if OLD not in text:
        print("MISSING anchor")
        return 1
    TARGET.write_text(text.replace(OLD, NEW, 1), encoding="utf-8")
    verify = TARGET.read_text(encoding="utf-8")
    assert NEW in verify
    assert "叫暫停" in verify
    assert "擊殺" in verify
    print("patched ok")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
