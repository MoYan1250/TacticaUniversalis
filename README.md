# Tactica Universalis

CSGO 電競俱樂部模擬遊戲。時間停在 2021 年開季，你經營一支俱樂部，也可以走進地圖看這一分怎麼打。Unity 6 的個人作品，仍在開發中。

A CSGO esports club simulation set in the opening of the 2021 season.

<img src="logo.png" alt="Tactica Universalis" width="180">

## 流程簡介

開局選擇 NAVI 或 Gambit。其他俱樂部由 bot 扮演。日曆從 2021 年 1 月 1 日開始。遊戲內部以一週為一步，畫面上看到的是日期和小時。

每一週照這個順序結算：你下的命令，合約與轉會期限，訓練與恢復，賽事，排名與曝光，其他俱樂部的決定，月底結算，董事會，然後自動存檔。一年有休賽期、春季、夏季窗口、秋季，以及年末的總決賽。

比賽日可以離開經營畫面，進入戰術控制室。選手在地圖上移動、對槍、下包。你可以暫停、加快速度，或跳過這場。結果回到生涯，改寫現金、名次和任務進度。

地圖是當年的圖池 Dust II、Mirage、Inferno、Nuke、Overpass、Train、Vertigo。

生涯分成八頁：地圖、選手、經濟、談判、基地、戰術、社群、圖鑑。

## 已經定下來的機制

選手有瞄準、反應、意識、道具、配合、溝通、首殺、殘局。八項合成戰力，再換成 S 到 E 的字母級。訓練可以成長，但受年齡、潛力、教練、基地和疲勞限制。年紀到了，反應和瞄準會先退。

收入來自賽事獎金、贊助、商品、門票和轉會。支出是薪資、基地、差旅和醫療。現金能撐幾個月，決定俱樂部是富裕、健康、緊繃還是危機。贊助從本地到頂級，名氣和世界排名不夠就簽不到更高一檔。

難度分普通、困難、硬核。普通看得到所有能力，也可以自由存讀。困難和硬核要靠分析師，才會逐步看到對手的能力。硬核不能隨意讀檔，只能沿著自動存檔繼續同一條生涯。

對局裡，戰術卡決定這一回合全隊怎麼打。每隊有通用卡和自己的專屬卡，用指揮點打出，卡與卡之間有克制。聚焦卡再決定這一回合把資源交給誰。教練可以在回合裡叫暫停。

NAVI 有一條已經接上的史實任務，從開局走到斯德哥爾摩 Major，再到後面的冠軍目標。Gambit 的任務樹還沒有做完。

畫面和數值仍在修。上面寫的是已經進遊戲、而且規則對得上的部分。

![戰術控制室](Assets/_Project/Screenshots/dust2_ui_zh.png)

戰術控制室。NAVI VS Gambit on Dust II。

## 程式架構

規則在 `Assets/_Project/Scripts/Domain`，不依賴 Unity。一週和一場比賽的流程在 `Application`。讀 JSON 與存檔在 `Infrastructure`。生涯和對局畫面在 `Presentation`。編輯模式測試在 `Assets/_Project/Tests/EditMode`。

## 如何 Unity 裡打開

安裝 Unity `6000.0.48f1`，用 Unity Hub 開啟這個資料夾。生涯場景是 `Assets/_Project/Scenes/Demo_Career.unity`。對局場景是 `Assets/_Project/Scenes/Demo_Dust2_QuickSim.unity`。

Unity 會自己重新產生解決方案。那些檔帶有這台電腦的路徑，所以沒有放進倉庫。地圖碰撞 harness 與選手爬蟲是另外兩個倉庫，下載快取也不在這裡。

- 地圖碰撞設計：[TacticaUniversalis-map-harness](https://github.com/MoYan1250/TacticaUniversalis-map-harness)
- 選手資料：[TacticaUniversalis-player-scraper](https://github.com/MoYan1250/TacticaUniversalis-player-scraper)

## 名稱與授權

俱樂部、選手與地圖名稱屬於各自的權利人。這是非官方的 CSGO 俱樂部模擬，與 Valve、賽事主辦方和任何俱樂部無關。

這個倉庫沒有開放原始碼授權。公開是為了展示與閱讀程式。

Shang-Jing Lyu · [MoYan1250](https://github.com/MoYan1250)
