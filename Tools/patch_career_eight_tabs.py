# -*- coding: utf-8 -*-
"""Patch CareerDemoController + CareerWorldMapPresentation for 8-tab career rail."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CAREER = ROOT / "Assets/_Project/Scripts/Presentation/CareerDemoController.cs"
MAP = ROOT / "Assets/_Project/Scripts/Presentation/CareerWorldMapPresentation.cs"


def replace_once(text: str, old: str, new: str, label: str) -> str:
    if old not in text:
        raise SystemExit(f"MISSING [{label}]: {old[:80]!r}...")
    return text.replace(old, new, 1)


def patch_career(text: str) -> str:
    text = replace_once(
        text,
        '        private static readonly string[] Tabs =\n            { "地圖", "選手管理", "經濟", "戰術" };',
        '        private static readonly string[] Tabs =\n        {\n            "地圖", "選手", "經濟", "談判", "基地", "戰術", "社群", "圖鑑"\n        };',
        "Tabs",
    )

    text = replace_once(
        text,
        "        private int _tab;\n",
        "        private int _tab;\n        private bool _mapEventLogFocus;\n",
        "_mapEventLogFocus field",
    )

    text = replace_once(
        text,
        "        private void SelectTab(int index)\n        {\n            if (_tab == index) return;\n            ClosePlayerDetail();\n            _tab = index;",
        "        private void SelectTab(int index)\n        {\n            if (_tab == index && !(index == 0 && _mapEventLogFocus)) return;\n            ClosePlayerDetail();\n            if (index != 0 || _tab != index)\n                _mapEventLogFocus = false;\n            _tab = index;",
        "SelectTab",
    )

    old_nav = """                case CareerUiDestination.TransferMarket:
                    NavigateToTab(1);
                    _sectionExpanded["market_offers"] = true;
                    break;"""
    new_nav = """                case CareerUiDestination.TransferMarket:
                    NavigateToTab(3);
                    _sectionExpanded["market_offers"] = true;
                    break;"""
    text = replace_once(text, old_nav, new_nav, "TransferMarket nav")

    old_tactics = """                case CareerUiDestination.Tactics:
                    NavigateToTab(3);
                    break;"""
    new_tactics = """                case CareerUiDestination.Tactics:
                    NavigateToTab(5);
                    break;"""
    text = replace_once(text, old_tactics, new_tactics, "Tactics nav")

    old_fac = """                case CareerUiDestination.Facilities:
                    NavigateToTab(2);
                    _sectionExpanded["base_facilities"] = true;
                    break;
                case CareerUiDestination.EventLog:
                    NavigateToTab(2);
                    _sectionExpanded["base_events"] = true;
                    break;"""
    new_fac = """                case CareerUiDestination.Facilities:
                    NavigateToTab(4);
                    _sectionExpanded["base_facilities"] = true;
                    break;
                case CareerUiDestination.EventLog:
                    NavigateToTab(0);
                    _mapEventLogFocus = true;
                    break;"""
    text = replace_once(text, old_fac, new_fac, "Facilities/EventLog nav")

    old_rebuild = """        private void RebuildContent()
        {
            if (_session == null) return;

            bool mapTab = _tab == 0;
            if (_mapHost != null) _mapHost.SetActive(mapTab);
            if (_scrollHost != null) _scrollHost.SetActive(!mapTab);

            if (mapTab)
            {
                RefreshWorldMap();
                RefreshOfficialMatchBanner();
                RefreshMapSchedule();
                FocusPendingMapCompetitionRow();
                return;
            }

            if (_officialMatchBanner != null)
                _officialMatchBanner.SetActive(false);

            if (_contentRoot == null) return;

            float mainScrollPos = _scroll != null ? _scroll.verticalNormalizedPosition : 1f;

            for (int i = _contentRoot.childCount - 1; i >= 0; i--)
                DestroyImmediate(_contentRoot.GetChild(i).gameObject);

            switch (_tab)
            {
                case 1: DrawPlayerManagement(); break;
                case 2: DrawBase(); break;
                default: DrawTacticsLaunch(); break;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRoot);
            if (_scroll != null)
                _scroll.verticalNormalizedPosition = mainScrollPos;
        }"""
    new_rebuild = """        private void RebuildContent()
        {
            if (_session == null) return;

            bool mapVisible = _tab == 0 && !_mapEventLogFocus;
            if (_mapHost != null) _mapHost.SetActive(mapVisible);
            if (_scrollHost != null) _scrollHost.SetActive(_tab != 0 || _mapEventLogFocus);

            if (mapVisible)
            {
                RefreshWorldMap();
                RefreshOfficialMatchBanner();
                RefreshMapSchedule();
                FocusPendingMapCompetitionRow();
                return;
            }

            if (_tab == 0 && _mapEventLogFocus)
            {
                if (_officialMatchBanner != null)
                    _officialMatchBanner.SetActive(false);
                RebuildScrollTab(DrawMapEventLog);
                return;
            }

            if (_officialMatchBanner != null)
                _officialMatchBanner.SetActive(false);

            switch (_tab)
            {
                case 1: RebuildScrollTab(DrawPlayerTab); break;
                case 2: RebuildScrollTab(DrawEconomyTab); break;
                case 3: RebuildScrollTab(DrawNegotiationTab); break;
                case 4: RebuildScrollTab(DrawBaseFacilitiesTab); break;
                case 5: RebuildScrollTab(DrawTacticsTab); break;
                case 6: RebuildScrollTab(DrawCommunityTab); break;
                case 7: RebuildScrollTab(DrawCodexTab); break;
            }
        }

        private void RebuildScrollTab(Action draw)
        {
            if (_contentRoot == null || draw == null) return;

            float mainScrollPos = _scroll != null ? _scroll.verticalNormalizedPosition : 1f;

            for (int i = _contentRoot.childCount - 1; i >= 0; i--)
                DestroyImmediate(_contentRoot.GetChild(i).gameObject);

            draw();

            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRoot);
            if (_scroll != null)
                _scroll.verticalNormalizedPosition = mainScrollPos;
        }

        private void DrawTabIntro(string title, string subtitle, params string[] bullets)
        {
            AddSectionTitle(title);
            AddMutedLine(subtitle);
            if (bullets == null) return;
            foreach (string line in bullets)
            {
                if (string.IsNullOrEmpty(line)) continue;
                AddMutedLine("· " + line);
            }
        }

        private void DrawPlaceholderSection(string key, string heading, string note)
        {
            if (!AddCollapsibleSection(key, heading)) return;
            AddMutedLine(string.IsNullOrEmpty(note) ? "此功能開發中。" : note);
        }

        private void ShowMapEventLog()
        {
            _mapEventLogFocus = true;
            NavigateToTab(0);
            RebuildContent();
        }

        private void DrawMapEventLog()
        {
            DrawTabIntro("賽事與事件", "事件檢視", "賽事報名", "賽程日曆", "世界排名", "事件檢視");
            RectTransform backRow = AddRow(44f);
            AddRowButton(backRow, 0.02f, 0.32f, "返回地圖", () =>
            {
                _mapEventLogFocus = false;
                RebuildContent();
            });
            DrawEvents();
        }

        private void DrawPlayerTab()
        {
            DrawTabIntro("選手管理", "陣容調整、訓練分配、狀態檢視、青訓營");
            DrawRoster();
            DrawPlaceholderSection("player_youth", "青訓營", "青訓學院設施升級後，梯隊選手將在此管理。");
        }

        private void DrawEconomyTab()
        {
            DrawTabIntro("財務管理", "營收報表、支出明細、贊助商合約、商品銷售");
            DrawEconomyFinance();
            DrawPlaceholderSection("econ_merch", "商品銷售", null);
        }

        private void DrawNegotiationTab()
        {
            DrawTabIntro("轉會與合約", "轉會市場、合約談判、AI 報價、租借管理");
            DrawMarket();
            DrawPlaceholderSection("nego_contracts", "合約談判", null);
            DrawPlaceholderSection("nego_loans", "租借管理", null);
        }

        private void DrawBaseFacilitiesTab()
        {
            DrawTabIntro("設施與教練組", "設施升級、教練組管理、醫療中心、數據中心");
            DrawFacilities();
            DrawPlaceholderSection("base_coaches", "教練組管理", null);
            DrawPlaceholderSection("base_medical", "醫療中心", null);
            DrawPlaceholderSection("base_analytics", "數據中心", null);
        }

        private void DrawTacticsTab()
        {
            DrawTabIntro("戰術與數據", "牌組編輯、對手分析、Meta 趨勢、數據顯示");
            DrawPlaceholderSection("tactics_deck", "牌組編輯", null);
            DrawPlaceholderSection("tactics_scout", "對手分析", null);
            DrawPlaceholderSection("tactics_meta", "Meta 趨勢", null);
            DrawPlaceholderSection("tactics_data", "數據顯示", null);
            DrawTacticsLaunch();
        }

        private void DrawCommunityTab()
        {
            DrawTabIntro("粉絲與公關", "粉絲趨勢、社群事件、公關危機、粉絲活動");
            DrawPlaceholderSection("comm_fans", "粉絲趨勢", null);
            DrawPlaceholderSection("comm_social", "社群事件", null);
            DrawPlaceholderSection("comm_pr", "公關危機", null);
            DrawPlaceholderSection("comm_events", "粉絲活動", null);
        }

        private void DrawCodexTab()
        {
            DrawTabIntro("資料庫與教學", "選手圖鑑、俱樂部圖鑑、遊戲教學、術語辭典");
            DrawPlaceholderSection("codex_players", "選手圖鑑", null);
            DrawPlaceholderSection("codex_clubs", "俱樂部圖鑑", null);
            DrawPlaceholderSection("codex_tutorial", "遊戲教學", null);
            DrawPlaceholderSection("codex_glossary", "術語辭典", null);
        }"""
    text = replace_once(text, old_rebuild, new_rebuild, "RebuildContent")

    old_pm = """        private void DrawPlayerManagement()
        {
            DrawRoster();
            DrawMarket();
        }

        private void DrawBase()
        {
            CareerState state = _session.State;
            ClubState club = state.Club(state.playerClubId);
            MonthlyBudgetPreview budget = _session.GetMonthlyBudgetPreview(club.clubId);

            if (AddCollapsibleSection("base_finance", "每月收支"))
                DrawMonthlyBudgetPanel(budget);

            if (AddCollapsibleSection("base_ledger", "最近現金流"))
            {
                LedgerEntry[] ledger = state.ledger.Where(x => x.clubId == club.clubId)
                    .Reverse<LedgerEntry>().Take(8).ToArray();
                if (ledger.Length == 0)
                    AddMutedLine("尚無帳目紀錄。");
                foreach (LedgerEntry entry in ledger)
                {
                    RectTransform row = AddRow(36f);
                    AddRowLabel(row, 0.02f, 0.28f,
                        CareerCalendar.FormatShort(_content.Balance, entry.clockHour),
                        TextMuted, 13);
                    AddRowLabel(row, 0.30f, 0.62f, entry.category, TextPrimary, 13);
                    Color amtColor = entry.amount >= 0 ? Positive : Danger;
                    AddRowLabel(row, 0.64f, 0.82f, $"{entry.amount:+#,0;-#,0;0}", amtColor, 13, TextAnchor.MiddleRight);
                    AddRowLabel(row, 0.84f, 0.98f, entry.note ?? string.Empty, TextMuted, 12);
                }
            }

            if (AddCollapsibleSection("base_sponsors", "贊助"))
            {
                SponsorContract[] active = state.sponsors
                    .Where(x => x.clubId == club.clubId)
                    .OrderByDescending(x => x.tier)
                    .ThenBy(x => x.sponsorId)
                    .ToArray();
                int monthKey = SponsorRules.CalendarMonthKey(_content.Balance, state.clockHour);
                bool signedThisMonth = state.lastSponsorSignMonthKey == monthKey;
                AddMutedLine(
                    $"合作中 {active.Length}/{SponsorRules.MaxActiveSponsorsPerClub} · " +
                    (signedThisMonth ? "本月已簽約，下月可再洽談一家" : "本月尚可簽訂一家新贊助"));

                if (active.Length == 0)
                    AddMutedLine("目前無贊助合約。");
                foreach (SponsorContract contract in active)
                {
                    if (!_content.Sponsors.TryGetValue(contract.sponsorId, out SponsorDefinition def))
                        continue;
                    RectTransform row = AddRow(40f);
                    AddRowLabel(row, 0.02f, 0.72f,
                        $"{def.displayName} · 滿意度 {contract.satisfaction} · 剩餘 {contract.remainingMonths} 月",
                        TextPrimary, 14);
                    AddRowLabel(row, 0.74f, 0.98f,
                        $"${contract.monthlyFee:N0}/月",
                        TextMuted, 13, TextAnchor.MiddleRight);
                }

                if (active.Length < SponsorRules.MaxActiveSponsorsPerClub)
                {
                    AddMutedLine("可洽談贊助：");
                    foreach (SponsorDefinition definition in _content.Sponsors.Values.OrderBy(x => x.tier))
                    {
                        if (active.Any(x => string.Equals(x.sponsorId, definition.id, StringComparison.OrdinalIgnoreCase)))
                            continue;
                        RectTransform row = AddRow(42f);
                        AddRowLabel(row, 0.02f, 0.72f,
                            $"{definition.displayName} · {definition.tier} · ${definition.baseMonthlyFee:N0}/月",
                            TextPrimary, 14);
                        string sponsorId = definition.id;
                        AddRowButton(row, 0.76f, 0.98f, "洽談", () => ShowSponsorNegotiationPanel(sponsorId));
                    }
                }
            }

            DrawFacilities();

            if (AddCollapsibleSection("base_events", "近期事件"))
            {
                CareerEvent[] events = state.events.Reverse<CareerEvent>().Take(12).ToArray();
                if (events.Length == 0)
                    AddMutedLine("尚無事件紀錄。");
                foreach (CareerEvent evt in events)
                    AddCareerEventRow(evt);
            }
        }"""
    new_pm = """        private void DrawEconomyFinance()
        {
            CareerState state = _session.State;
            ClubState club = state.Club(state.playerClubId);
            MonthlyBudgetPreview budget = _session.GetMonthlyBudgetPreview(club.clubId);

            if (AddCollapsibleSection("base_finance", "每月收支"))
                DrawMonthlyBudgetPanel(budget);

            if (AddCollapsibleSection("base_ledger", "最近現金流"))
            {
                LedgerEntry[] ledger = state.ledger.Where(x => x.clubId == club.clubId)
                    .Reverse<LedgerEntry>().Take(8).ToArray();
                if (ledger.Length == 0)
                    AddMutedLine("尚無帳目紀錄。");
                foreach (LedgerEntry entry in ledger)
                {
                    RectTransform row = AddRow(36f);
                    AddRowLabel(row, 0.02f, 0.28f,
                        CareerCalendar.FormatShort(_content.Balance, entry.clockHour),
                        TextMuted, 13);
                    AddRowLabel(row, 0.30f, 0.62f, entry.category, TextPrimary, 13);
                    Color amtColor = entry.amount >= 0 ? Positive : Danger;
                    AddRowLabel(row, 0.64f, 0.82f, $"{entry.amount:+#,0;-#,0;0}", amtColor, 13, TextAnchor.MiddleRight);
                    AddRowLabel(row, 0.84f, 0.98f, entry.note ?? string.Empty, TextMuted, 12);
                }
            }

            if (AddCollapsibleSection("base_sponsors", "贊助"))
            {
                SponsorContract[] active = state.sponsors
                    .Where(x => x.clubId == club.clubId)
                    .OrderByDescending(x => x.tier)
                    .ThenBy(x => x.sponsorId)
                    .ToArray();
                int monthKey = SponsorRules.CalendarMonthKey(_content.Balance, state.clockHour);
                bool signedThisMonth = state.lastSponsorSignMonthKey == monthKey;
                AddMutedLine(
                    $"合作中 {active.Length}/{SponsorRules.MaxActiveSponsorsPerClub} · " +
                    (signedThisMonth ? "本月已簽約，下月可再洽談一家" : "本月尚可簽訂一家新贊助"));

                if (active.Length == 0)
                    AddMutedLine("目前無贊助合約。");
                foreach (SponsorContract contract in active)
                {
                    if (!_content.Sponsors.TryGetValue(contract.sponsorId, out SponsorDefinition def))
                        continue;
                    RectTransform row = AddRow(40f);
                    AddRowLabel(row, 0.02f, 0.72f,
                        $"{def.displayName} · 滿意度 {contract.satisfaction} · 剩餘 {contract.remainingMonths} 月",
                        TextPrimary, 14);
                    AddRowLabel(row, 0.74f, 0.98f,
                        $"${contract.monthlyFee:N0}/月",
                        TextMuted, 13, TextAnchor.MiddleRight);
                }

                if (active.Length < SponsorRules.MaxActiveSponsorsPerClub)
                {
                    AddMutedLine("可洽談贊助：");
                    foreach (SponsorDefinition definition in _content.Sponsors.Values.OrderBy(x => x.tier))
                    {
                        if (active.Any(x => string.Equals(x.sponsorId, definition.id, StringComparison.OrdinalIgnoreCase)))
                            continue;
                        RectTransform row = AddRow(42f);
                        AddRowLabel(row, 0.02f, 0.72f,
                            $"{definition.displayName} · {definition.tier} · ${definition.baseMonthlyFee:N0}/月",
                            TextPrimary, 14);
                        string sponsorId = definition.id;
                        AddRowButton(row, 0.76f, 0.98f, "洽談", () => ShowSponsorNegotiationPanel(sponsorId));
                    }
                }
            }
        }"""
    text = replace_once(text, old_pm, new_pm, "DrawBase split")

    text = replace_once(
        text,
        '            AddSectionTitle("戰術板");',
        '            AddSectionTitle("練習模擬");',
        "TacticsLaunch title",
    )

    return text


def patch_map(text: str) -> str:
    old = """            Text title = CreateText("ScheduleTitle", panel.transform, 19, TextAnchor.MiddleLeft, Accent, _uiFont700);
            SetRect(title.rectTransform, new Vector2(0.05f, 0.90f), new Vector2(0.95f, 0.98f));
            title.text = "賽事行程 · 報名";

            Image scrollHost = CreateUiObject<Image>("ScheduleScroll", panel.transform);
            SetRect(scrollHost.rectTransform, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.88f));"""
    new = """            Text title = CreateText("ScheduleTitle", panel.transform, 17, TextAnchor.MiddleLeft, Accent, _uiFont700);
            SetRect(title.rectTransform, new Vector2(0.05f, 0.90f), new Vector2(0.95f, 0.98f));
            title.text = "賽事與事件";

            CreateActionButton(
                "MapRankBtn", "排名", panel.transform,
                new Vector2(0.05f, 0.82f), new Vector2(0.24f, 0.89f),
                ButtonEdge, ButtonFace, OpenWorldRankingPanel, _uiFont700);
            CreateActionButton(
                "MapEventsBtn", "事件", panel.transform,
                new Vector2(0.26f, 0.82f), new Vector2(0.45f, 0.89f),
                ButtonEdge, ButtonFace, ShowMapEventLog, _uiFont700);

            Image scrollHost = CreateUiObject<Image>("ScheduleScroll", panel.transform);
            SetRect(scrollHost.rectTransform, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.80f));"""
    return replace_once(text, old, new, "MapSchedulePanel")


def main():
    career = CAREER.read_text(encoding="utf-8")
    career = patch_career(career)
    CAREER.write_text(career, encoding="utf-8", newline="\n")
    print("Patched", CAREER)

    map_text = MAP.read_text(encoding="utf-8")
    map_text = patch_map(map_text)
    MAP.write_text(map_text, encoding="utf-8", newline="\n")
    print("Patched", MAP)


if __name__ == "__main__":
    main()
