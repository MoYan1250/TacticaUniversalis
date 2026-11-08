# 俱樂部管理系統數值規格（Demo v1）

本文件是 Career 模式的數值權威。靜態設定由 `career_balance.json` 載入；存檔只保存狀態與規則版本。除特別標示外，屬性、名氣、忠誠、滿意度、健康、潛力與熟練度皆為 `0..100`，比例為 `0..1`，金額為整數 USD。中間值保留 `double`，入帳採四捨五入，最終結果均 clamp 至欄位範圍。

## 1. 時間、結算順序與共同尺度

- 開局日曆：`2021-01-01 00:00`（Demo）。內部最小推進仍為 1 週；狀態與 HUD 顯示精確到小時（`yyyy-MM-dd HH:00`）。
- 一年 52 週。月結週為 4、8、13、17、21、26、30、34、39、43、47、52。
- 賽季：休賽期 1–5；春季 6–26；夏季休整／主要窗口 27–30；秋季 31–47；年度總決賽 48；年終 49–52。
- 每週順序：處理玩家命令 → 合約／轉會期限 → 訓練與恢復 → 賽事 → Rating/排名/曝光 → AI 決策 → 月結 → 董事會／硬核限制 → 自動存檔。
- Demo 開局只能選擇 NAVI 或 Gambit；其他俱樂部為 AI 對手。
- 戰力 `Power = Σ(attribute × weight)`：瞄準 .20、反應 .15、意識 .15、道具 .10、配合 .12、溝通 .08、首殺 .10、殘局 .10。
- 字母級：S `95..100`、A+ `90..94`、A `85..89`、B+ `80..84`、B `70..79`、C `60..69`、D `45..59`、E `0..44`。

## 2. 俱樂部經濟

### 2.1 收入

所有單項收入先計算原值，再乘難度收入係數（普通 1、困難 .92、硬核 .85）。

- 賽事獎金：`Prize = Pool × PlacementShare × (1 - PlayerBonusShare)`；名次分配預設冠軍 .50、亞軍 .25、四強各 .10、其餘合計 .05。
- 贊助月費：`SponsorMonthly = BaseFee × (0.55 + Fame/200 + SponsorSatisfaction/400)`。
- 贊助成績獎金：`BaseBonus × TierWeight × PlacementWeight`，TierWeight 本地 .5、區域 1、國際 1.8、頂級 3。
- 轉會收入：實收報價減經紀／聯盟費 `TransferFee × .97`。
- 商品：`Merch = 4000 × (1 + Fans/50000) × (0.5 + Fame/100) × (1 + StarCount×.12)`。
- 門票分成：`Ticket = EventAttendance × TicketPrice × OrganizerShare × (0.55 + Fame/200 + Form/400)`。
- 青訓輸出：`AcademyValue × (0.65 + Scout/200 + AcademyLevel×.06)`。
- 直播／內容：`Media = 1500 × (0.4 + Fame/80) × (0.8 + Exposure/250)`。

### 2.2 支出

- 選手薪資：合約月薪總和；賽事獎金分成在獎金入帳時同步扣除。
- 教練薪資：`BaseStaffSalary × (0.6 + Ability/100 + Fame/250)`。
- 基地維護：各設施 `BaseMaintenance × Level^1.35 × RegionCost`。
- 差旅：`DistanceBandCost × Travelers × EventTierWeight`；Demo 距離帶本地 150、區域 500、洲際 1400。
- 轉會：轉會費、簽字費於成交日一次入帳；租借按月扣費。
- 青訓：`4000 × AcademyLevel^1.25 + ScoutNetworkCost`。
- 醫療：`Severity^1.4 × 800 × (1.2 - MedicalLevel×.06)`。

### 2.3 財務健康

`RunwayMonths = Cash / max(1, trailingThreeMonthExpenseAverage)`。狀態優先級由下而上：

- 破產：危機連續 12 月；普通模式進入強制接管，硬核模式結束生涯。
- 危機：Runway `<3`；凍結設施升級與高額轉會，允許董事會要求掛牌／降薪。
- 赤字：連續 3 個月淨現金流 `<0`；董事會警告，轉會預算上限降 40%。
- 收支平衡：Runway `3..6`；不追加預算。
- 健康：Runway `>6..12`；正常營運。
- 富裕：Runway `>12` 且近三月非虧損；董事會滿意度每月 +2，可追加 10% 預算。

董事會滿意度每月變化：`clamp(NetCashFlow / MonthlyExpense × 8, -8, 8) + RankingDeltaBonus + ObjectiveBonus`。

## 3. 贊助商

- 等級門檻：本地無門檻；區域名氣 35／排名前 75%；國際名氣 60／排名前 40%；頂級名氣 80／排名前 15%。
- 合約：6–24 月、月費、名次獎金、最低曝光、最低排名、可提前終止條款。
- 滿意度月變化：`0.35×PerformanceDelta + 0.30×ExposureDelta + 0.20×RequirementScore + 0.15×Relationship - 50`，縮放為 `-12..+8`。
- `<25` 連續 2 月可終止；`<40` 不續約；`>75` 續約報價 +10%。
- 談判成功率：`sigmoid((ClubLeverage - SponsorDemand)/12)`，最終 clamp `10%..90%`。賽季初或近 8 週排名提升 20% 時可重談。

## 4. 選手培養、潛力與衰退

每週每名選手獲得訓練點：

`TP = BaseTP × AgeCurve × PotentialSpace × Coach × Facility × Professionalism × Fatigue × Schedule`

- `BaseTP=1.2`；AgeCurve：18 以下 1.35、19–20 1.2、21–27 .75、28–30 .45、31+ .25。
- `PotentialSpace = clamp((Potential - Power)/35, .15, 1.25)`。
- Coach `=.7 + Training/200`；Facility `=.85 + Level×.05`；職業性格 1.15、一般 1、散漫 .82。
- Fatigue `=clamp(Energy/75,.55,1.1)`；當週兩場以上比賽 Schedule=.65。
- 指定屬性增量：`TP × FocusWeight / 10`；單週單屬性最多 +.35，能力不得超過 `min(100, Potential+8)`。
- 比賽經驗：上場 `2 + EventTier×1.5 + RatingBonus` XP；`LevelThreshold=100+Level×25`，升級轉為 1.5 TP。
- 位置／地圖熟練度：`Δ = TP × .55 × Coach × Facility`，存 `0..100`。

衰退於每 13 週結算：

- `Decline = Base × AgeExcess × Trajectory × Health × Inactivity`。
- 反應起點 27、Base .45；瞄準 29、Base .32；意識 32、Base .12；其餘 .18。
- 長青型 Trajectory=.65、標準 1、流星型 1.45；健康 `<70` 時 Health=`1+(70-Health)/100`。
- 連續 8 週未上場 Inactivity=1.25，並每週 Form -2 至最低 30。
- 重大傷病（severity ≥80）恢復時永久損失：反應／瞄準各 `0.5..2.0`，使用存檔 seed。
- 「未解的天才」：潛力 ≥88 且生涯前 10 場，20% 生成；前 5 場 Power 臨時 +4，不改永久屬性。

普通模式顯示真實潛力；困難顯示分析區間 `±(18-Analyst/8)`；硬核只顯示文字等級與信心。

## 5. 身價、轉會與合約

### 5.1 身價

`BaseValue = 25000 × exp((Power-55)/14)`。

`MarketValue = clamp(BaseValue × Age × Potential × Fame × Form × Contract × Scarcity, 10000, 15000000)`。

- Age：18–20 1.35、21–24 1.25、25–27 1、28–30 .78、31+ .55。
- Potential：`1 + max(0,Potential-Power)/80`（24 歲以下再 ×1.15）。
- Fame `=.75+Fame/200`；Form `=clamp(.85+(RatingTrend+10)/40,.75,1.25)`。
- Contract：剩餘 0–3 月 .55、4–12 .8、13–24 1、25+ 1.12。
- Scarcity：AWPer 1.2、IGL 1.15、Entry 1.05、其他 1。

### 5.2 交易

分階段談判：報價談妥轉會費**不會**立刻換隊。狀態流：`Pending`／`Countered` → 賣方暫允 → `ClubAgreed` → 買方開談個人條款 → `PersonalTerms` → 球員接受才 `CompleteTransfer`。進行中可被對手打斷為 `Outbid`；逾時為 `Expired`／`Failed`。

- 轉會費階段：玩家自行報價；市場列表只顯示可能最低價估計 `MarketValue × 0.85`，不保證對方接受。AI 首報 `MarketValue × (0.85..1.25)`；賣方保留價 `MarketValue × (1 + Importance×.35 + WindowPressure)`。報價有效約 2 週。
- 個人條款短窗：賣方暫允後 `phaseExpiresHour = clock + 5×24`。買方提出月薪／簽字費／合約月數。選手個人轉會意願除薪資條款外，另受買方威望與陣容平均戰力相對自身戰力影響。窗內選手仍屬原俱樂部，現金尚未扣轉會費。
- 對手打斷：對 `ClubAgreed`／`PersonalTerms` 交易，新轉會費 ≥ 暫允費 × `1.08`，或合法違約金路徑，可將原單標為 `Outbid` 並開立新談判。
- 違約金買斷：`fee >= releaseClause > 0` 跳過俱樂部同意，直接進入 `PersonalTerms` 且 `releaseClauseBid = true`；仍須球員個人條款同意後才成交。賣方實收仍為 `TransferFee × .97`。
- 市場情報：列表顯示歷史地位／當前地位與戰鬥屬性；困難／硬核需有分析師在編才能逐步揭露他隊屬性。
- 自由轉會免轉會費；租借 6–12 月與租借買斷選項為後續範圍。
- AI 需求分：`RoleGap×.4 + Upgrade×.3 + AgePlan×.15 + Commercial×.15`；預算後現金不得低於 3 個月固定支出。
- 窗口：第 1–4 週與第 27–30 週；重傷可補同位置自由球員；青訓與自由球員不受限制。
- 說服留隊：`Score = Management×.30 + Loyalty×.30 + TeamRankAppeal×.20 + WageParity×.20 - Unrest`；`roll 0..100 <= Score` 成功。

### 5.3 合約

- 長度 12–48 月；續約始於剩餘 6 月，其他俱樂部可於剩餘 3 月接觸。
- 要求薪資：`MarketValue/36 × AgeSecurity × FameDemand`，限制 `1000..250000/月`。
- 個人條款接受分：薪資 28%、簽字費 12%、長度 8%、世界排名吸引力 12%、忠誠／離隊意願 15%、買方威望 12%、買方陣容實力相對該選手 13%。
- 談判破裂：每次拒絕忠誠 -2、心情 -4；三次失敗冷卻 4 週並可能提出轉會。
- 出場保證採滾動 12 週；低於承諾 15 個百分點時滿意度每週 -3。

## 6. 賽事與排名

- 層級倍率：S 5、A 3、B 1.8、C 1、D .5、表演賽 0。
- 報名資格依排名、邀請、預選、地區、報名費逐條驗證；失敗回傳可顯示原因碼。
- 基礎排名分：`TierBase × PlacementWeight × FieldStrength + UpsetBonus`。TierBase：S 1000、A 600、B 350、C 180、D 80。
- PlacementWeight：冠軍 1、亞軍 .65、四強 .4、八強 .22、參賽 .08。
- `FieldStrength=.75+AverageOpponentPower/200`；爆冷分 `max(0,(OpponentRank-SelfRank)×2)`，單場上限 80。
- 時間衰減：每週 `BatchPoints × 0.5^(AgeWeeks/26)`；只計最近 52 週。
- 同分依 13 週積分、最高級賽事名次、對戰、club id 排序，確保決定性。
- 排名影響：贊助上限、邀請門檻、頂級選手加盟意願、粉絲週增長 `Fans×(.001 + RankPercentile×.004)`。

## 7. 設施

- 訓練基地：訓練效率與能量恢復；青訓設施：新人品質／成長；醫療中心：恢復與永久傷損機率；數據中心：分析區間與賽前情報。
- 等級 1–5；升級費 `BaseCost × Level^1.7`，工期 `4 + Level×3` 週；升級中仍按原級生效。
- 月維護 `BaseMaintenance × Level^1.35`；無法支付時設施停用一級效果，連續 3 月欠費降 1 級。
- 加盟吸引力：每個 4+ 級設施 +2，總上限 +8。

## 8. 跨系統事件與模組接口

所有 command 產生 append-only `CareerEvent`；財務異動必有同 id 的 `LedgerEntry`。必要事件：

`WeekAdvanced`、`MonthClosed`、`MatchCompleted`、`PrizeAwarded`、`SponsorChanged`、`TrainingApplied`、`PlayerInjured`、`ContractChanged`、`TransferCompleted`、`RankingUpdated`、`FacilityChanged`、`FinancialStatusChanged`。

Match bridge 輸入 club/team/player snapshot 與 seed，輸出勝負、比分、每名選手 K/D/A、Rating、出場、傷病抽樣與曝光；Career 不讀局內 money，QuickMatch 不讀俱樂部 cash。

## 9. 難度與硬核模式

- 普通：完整潛力、3 個手動存檔、收入 1、AI 積極度 1。
- 困難：潛力區間、收入 .92、AI 出價頻率 1.2、董事會耐心 .85。
- 硬核：潛力文字級、收入 .85、AI 1.35、單一原子自動存檔、command 後立即覆寫、破產結束生涯。遊戲內禁止回讀舊節點；無法阻止玩家在作業系統層複製檔案。

## 10. Demo MVP 邊界與驗收

- 8 俱樂部、40 名球員（含自由球員）、4 贊助商等級、6 個賽事範本、四類設施、52 週。
- 可操作：訓練、報名、贊助、續約、自由簽約、報價／反提案、設施升級、推進一週、存讀檔。
- Demo 使用兩隊完整 QuickMatch；其他 AI 對局用相同 seed 的摘要模擬。交換、完整青訓生成、經紀人與多人聯機只保留欄位。
- 驗收：同 seed 同命令得到同結果；全年可結束；存檔 round-trip 等價；AI 不突破現金安全線；所有財務可追溯；硬核無手動讀檔入口；100 季 smoke 無非有限值、重複球員所有權或無法結算賽季。
