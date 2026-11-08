# Tactica Universalis

CSGO 電競俱樂部模擬遊戲。以 2021 賽季經營一支俱樂部，並進入地圖打戰術對局。這是 Unity 6 的個人作品，目前仍在開發。

A CSGO esports club simulation, set in the 2021 season. Work in progress.

<img src="logo.png" alt="Tactica Universalis" width="180">

## 現在玩得到什麼

- 生涯從選擇俱樂部開始，以週為單位推進，處理經濟、選手、教練、贊助與賽程。
- 比賽日可以進入戰術對局，在 Dust II 看這一分怎麼打，並能暫停、調整倍速或跳過。
- 規則、選手、賽事與地圖資料放在 JSON。存檔只記狀態，不把靜態設定寫死在存檔裡。

規則、介面與平衡都還在修。倉庫裡是目前的開發版本，不是完成品。

## 另外兩塊

遊戲本體在這個倉庫。另外兩塊各自獨立：

- 地圖碰撞設計 harness：[TacticaUniversalis-map-harness](https://github.com/MoYan1250/TacticaUniversalis-map-harness)
- 選手資料爬蟲：[TacticaUniversalis-player-scraper](https://github.com/MoYan1250/TacticaUniversalis-player-scraper)

## 程式怎麼分

| 層 | 路徑 | 做什麼 |
| --- | --- | --- |
| Domain | `Assets/_Project/Scripts/Domain` | 規則與資料，不依賴 Unity |
| Application | `Assets/_Project/Scripts/Application` | 把一週、一場比賽的流程串起來 |
| Infrastructure | `Assets/_Project/Scripts/Infrastructure` | 讀 JSON、存檔 |
| Presentation | `Assets/_Project/Scripts/Presentation` | 生涯與對局畫面 |

編輯模式測試在 `Assets/_Project/Tests/EditMode`。

## 在 Unity 裡打開

1. 安裝 Unity `6000.0.48f1`。
2. 用 Unity Hub 開啟這個資料夾。
3. 生涯場景是 `Assets/_Project/Scenes/Demo_Career.unity`。
4. 對局場景是 `Assets/_Project/Scenes/Demo_Dust2_QuickSim.unity`。

Unity 會自己重新產生解決方案與專案檔。那些檔帶有這台電腦的路徑，所以沒有放進倉庫。

## 沒有放進這個倉庫的東西

- `Library`、`Temp`、`Logs` 與其他編輯器快取。
- 地圖碰撞 harness、它的下載快取與預覽。
- 選手爬蟲與它的原始快取。
- 編輯器備份、本機代理筆記，以及示範字型 Anuschka Demo。遊戲會改用介面字型。

## 第三方名稱與素材

俱樂部、選手與地圖名稱屬於各自的權利人。這是非官方的 CSGO 俱樂部模擬，與 Valve、賽事主辦方和任何俱樂部無關。

請勿再散布倉庫裡的雷達圖、選手資料，或把這些名稱當成可以轉授權的內容。

隨專案提供的字型是 Noto Sans TC、Anton 與源樣明體，各自依原授權使用。

## 授權

這個倉庫沒有開放原始碼授權。公開是為了展示與閱讀程式，不表示可以拿去改作或再發布。

## 作者

Shang-Jing Lyu · [MoYan1250](https://github.com/MoYan1250)
