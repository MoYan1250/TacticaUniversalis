# -*- coding: utf-8 -*-
from pathlib import Path

p = Path(__file__).resolve().parents[1] / ".cursor/rules/tactica-core.mdc"
t = p.read_text(encoding="utf-8")
if "除錯與已知陷阱" in t:
    print("already synced")
else:
    needle = "6. **文本符號**：生成文案／JSON／UI 字串時禁止隨意使用「」()《》等；需要時先問用戶。詳見 `AGENTS.md`。\n"
    insert = (
        needle
        + "7. **QuickMatch／正式賽除錯**：底欄參數勿反轉；回生涯 null／LOGO 先查 Console `Club()`；"
        + "詳見 `AGENTS.md`「除錯與已知陷阱」。\n"
    )
    if needle not in t:
        raise SystemExit("needle missing in tactica-core.mdc")
    p.write_text(t.replace(needle, insert, 1), encoding="utf-8", newline="\n")
    print("synced", p)
