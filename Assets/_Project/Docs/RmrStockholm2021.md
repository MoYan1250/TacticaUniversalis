# Stockholm 2021 Regional Major Rankings

設計參考：Liquipedia Regional Major Rankings / PGL Major Stockholm 2021。

## 與世界排名的關係

- **世界排名**：HLTV 風格積分，有半衰期與視窗，見 `RankingRules`。
- **地區 Major 排名**：獨立的 RMR 積分，**不衰減**，僅累加各輪名次分與陣容扣分，見 `RmrRules`。
- **Major 入場**：`pgl_major_stockholm_2021` 的 `qualificationMode: Rmr` 已啟用。未取得地區席位時不可報名；取得席位後可確認入場，但**不可取消報名**。開賽前系統仍會依 RMR 名單為 AI 俱樂部自動鎖定入場。

## 六區與 Stockholm 名額

| 區域 | 傳奇 | 挑戰者 | 爭奪者 | 合計 |
|------|------|--------|--------|------|
| Europe | 3 | 6 | 2 | 11 |
| CIS | 2 | 2 | 1 | 5 |
| NorthAmerica | 3 | 1 | 1 | 5 |
| SouthAmerica | 0 | 0 | 1 | 1 |
| Asia | 0 | 0 | 1 | 1 |
| Oceania | 0 | 0 | 1 | 1 |

## Major 賽制

- 挑戰者階段：爭奪者＋挑戰者共十六隊，瑞士制三勝晉級／三敗淘汰
- 傳奇階段：八席傳奇種子＋八席挑戰者晉級，同樣瑞士制
- 冠軍階段：八隊單淘汰
- 實作見 `StockholmMajorRules` 與 `CareerMajorApplication`

## 積分輪次

- Circuit 2020：開檔 seed（`rmrCircuitSeed` 或由 hltvPoints 推估）
- RMR 1 / RMR 2 / RMR 3：賽事 `rmrRound` + `rmrRegion` 結算寫入
- 陣容變動：每次核心替換扣當下總分 20%，可疊加（轉會完成、歷史 join_club）

內容對照見 `career_rmr_balance.json`；點數表實作在 `RmrRules`。

## UI

世界排名面板可切換 **世界排名** / **地區 Major 排名**，後者可循環切換六區，顯示區內名次、總分、各輪小計與資格預覽。行程表點開 Major 詳情可見三階段規則與瑞士制戰績。

## 已知缺口

亞／大洋／南美若缺 RMR 賽事或俱樂部，該區 Contender 席位可能由世界排名較高的其他俱樂部補位，以確保挑戰者十六隊可開打。
