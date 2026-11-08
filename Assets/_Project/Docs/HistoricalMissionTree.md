# 歷史任務樹設計備忘

本文記錄歷史轉會／歷史事件與任務樹的銜接。實作以本文與 `AGENTS.md` 慣例為準。

## 目標

- 歷史事件對旁觀俱樂部仍是第三人稱新聞；玩家為當事俱樂部時，關鍵抉擇改走任務樹。
- 玩家目前僅能扮演 NAVI 與 Gambit；本階段已實作 **NAVI 史實單線任務樹**。

## 本階段已實作

| 項目 | 說明 |
|------|------|
| 資料 | `Assets/_Project/Data/JSON/career_missions.json` |
| Domain | `CareerMissionTreeDef` / `CareerMissionNodeDef` / `CareerMissionRules` / `CareerMissionProgress` |
| 節點欄位 | `title`、`flavor`、`goal`；樹上只顯示銘題，懸停顯示背景與目標 |
| 條件 | `official_matches_min`、`competition_started`、`competition_champion` 等 |
| 存檔 | `CareerState.missionProgress`、`playerOfficialMatchesPlayed` |
| Application | `GetPlayerMissionTree`、`GetMissionNodeStatus`、`TryCompleteMissionNode` |
| UI | Hub 任務分頁開啟全螢幕 `CareerMissionTreePresentation` |
| 四月閘門 | `hist_b1t_replaces_flamie_navi_2021_04_11` 僅在 `not_player_club:navi_2021` 或 `has_flag:navi_path_b1t` 時觸發 |

### NAVI 2021 單線節點

| id | 銘題 | 完成條件 |
|----|------|----------|
| `navi_rise_day` | 崛起之日 | 開局 |
| `navi_third_gun` | 第三把槍 | 前置完成；設 `navi_path_b1t` |
| `navi_mic_to_youth` | 把麥克風遞給少年 | 前置完成 |
| `navi_triple_hold` | 三槍架點 | 正式賽 ≥5 場 |
| `navi_eight_years_due` | 八年之期已到 | Stockholm Major 開打且已報名 |
| `navi_major_finale` | Major之路的落幕 | 贏得 Stockholm Major |
| `navi_dynasty_arrives` | 黑金王朝的降臨 | 贏得 EPL S14，完成大滿貫拼圖 |

### NAVI 旗標

| Flag | 意義 |
|------|------|
| `navi_path_b1t` | 三核路線；四月升上主隊事件閘門 |
| `navi_stockholm_champion` | 任務樹記錄 Major 冠軍 |
| `navi_grand_slam_claimed` | 任務樹記錄大滿貫封頂 |

## 與現有結構的銜接

- 歷史事件的 `firstPersonDescription`／`firstPersonOptions` 仍供事件彈窗；任務樹用 `flavor`／`goal`。
- 文案符號：任務樹視為一般 UI，不用裝飾引號與設計比喻。
- 正式賽場次在 `CareerApplication.ApplyMatch` 累加 `playerOfficialMatchesPlayed`。

## 後續可擴

- Gambit 任務樹、更多 2021 下半年節點與戰鬥 modifier 掛接。
