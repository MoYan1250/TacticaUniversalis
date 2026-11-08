# -*- coding: utf-8 -*-
from pathlib import Path

p = Path(__file__).resolve().parents[1] / "AGENTS.md"
text = p.read_text(encoding="utf-8")

old = """## 驗證

- 改 C# 後執行：`dotnet build TacticaUniversalis.Presentation.csproj`（或對應 csproj）。
- 生涯邏輯可跑：`dotnet test TacticaUniversalis.Tests.EditMode.csproj`（需本機 Unity 測試環境時以 Unity Test Runner 為準）。
- **QuickMatch UI**：若 `QuickMatchDemoController.cs` 中文變成 `?`，用 `python Tools/recover_quickmatch.py` 從 Local History 備份還原並重套 patch；**不要**用 Cursor StrReplace / PowerShell 整檔改含 CJK 的該檔。

## 相關文件

- 戰術／教練卡：`Assets/_Project/Docs/TacticalCards.md`、`FocusCoachCards.md`
- 俱樂部管理設計：`Assets/_Project/Docs/ClubManagementDesign.md`

本文更新時請同步檢查 `.cursor/rules/tactica-core.mdc` 摘要是否仍一致。
"""

new = """## 驗證

- 改 C# 後執行：`dotnet build TacticaUniversalis.Presentation.csproj`（或對應 csproj）。
- 生涯邏輯可跑：`dotnet test TacticaUniversalis.Tests.EditMode.csproj`（需本機 Unity 測試環境時以 Unity Test Runner 為準）。
- **QuickMatch UI**：若 `QuickMatchDemoController.cs` 中文變成 `?`，用 `python Tools/recover_quickmatch.py` 從 Local History 備份還原並重套 patch；**不要**用 Cursor StrReplace / PowerShell 整檔改含 CJK 的該檔。含中文的大改請優先用 `Tools/patch_*.py` UTF-8 腳本，或 `python Tools/fix_quickmatch_compile.py` 整串還原＋重套。

## 除錯與已知陷阱

### 編碼／檔案損壞

- UI 字串變 `???`、或檔案無法以 UTF-8 讀取：先停手，用 Local History / `Tools/recover_quickmatch.py` 還原，**不要**在已損檔上繼續 patch。
- Cursor 對含大量 CJK 的 `.cs` 做大範圍 StrReplace 也曾弄壞編碼；`QuickMatchDemoController.cs` 一律當高風險檔。

### 正式賽戰鬥 UI（`QuickMatchDemoController`）

- 正式賽／練習底欄切換：`ApplyControlBarLayout(simulation)`。`simulation == true` 為練習寬欄；`false` 為正式賽短置中欄。**勿寫成 `!simulation`**，否則正式賽會一直套用寬底欄，按鈕看起來像貼在右側。
- 正式賽跳過：`Skip()` 會停掉 `MatchLoop`，必須再 `StartCoroutine(ShowOfficialResultFlow())`，否則卡在戰鬥場景、結果不回傳生涯、對手帳不更新。
- 底欄／結果／meta 相關修復腳本在 `Tools/patch_official_*.py`；改完用 Python 驗證仍含「擊殺廣播」等中文錨點。

### 正式賽回生涯後抬頭空白／LOGO 消失

- 症狀：確認比賽結束回 `Demo_Career` 後，現金／粉絲等像 null、俱樂部 LOGO 不見，推進時間後又正常。
- 常見根因：`TryApplyOfficialMatchResult` → `AdvanceInteractiveCompetition` → `ApplyMatch` 呼叫 `CareerState.Club(id)`（`First`）時 id 不在存檔 clubs，丟 `InvalidOperationException`，`Start()` 裡後續的 `RefreshAll()` 沒跑到。
- 排查：Unity Console 或 MCP `read_console` 找 `Sequence contains no matching element` 與 `CareerState.Club`。
- 慣例：`ApplyMatch` 用 `TryGetClub`；回生涯路徑即使套用失敗也要 `RefreshAll()`（可加一幀 deferred 再刷一次）。

### 組織滿意度／俱樂部確認無反應

- `ClubEstateSatisfactionRules.EnsureNormalized` ↔ `SyncBoardLegacy` ↔ `Get` 曾造成 StackOverflow，表現為標題畫面「確認選擇」沒反應。
- `SyncBoardLegacy` 必須直接讀 `estateSatisfied` 條目，**不可**再呼叫 `Get()`。

### 一般除錯順序

1. `dotnet build` 對應 csproj（0 error）。
2. Unity Console 或 MCP `read_console`（error／exception＋stack）。
3. 編碼可疑時先查檔案能否 `encoding=\"utf-8\"` 讀出中文。
4. 場景流程問題：確認 `CareerMatchLaunchContext` 的 launch／completed result 是否有被 take 掉卻未套用。

## 相關文件

- 戰術／教練卡：`Assets/_Project/Docs/TacticalCards.md`、`FocusCoachCards.md`
- 俱樂部管理設計：`Assets/_Project/Docs/ClubManagementDesign.md`

本文更新時請同步檢查 `.cursor/rules/tactica-core.mdc` 摘要是否仍一致。
"""

# fix escaped quotes in new for actual file content
new = new.replace('encoding=\\"utf-8\\"', 'encoding="utf-8"')

if old not in text:
    raise SystemExit("AGENTS.md anchor missing")

p.write_text(text.replace(old, new, 1), encoding="utf-8", newline="\n")
print("updated", p)
