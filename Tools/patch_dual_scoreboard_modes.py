# -*- coding: utf-8 -*-
"""Dual scoreboard: classic (quick) vs general cards; settings only centered in general."""
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
import backup_quickmatch as bak

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing:\n{old[:240]!r}")
    return text.replace(old, new, 1)


def main() -> None:
    bak.backup("before_dual_scoreboard_modes")
    text = TARGET.read_text(encoding="utf-8")
    if "擊殺" not in text or "跳過結算中" not in text:
        raise RuntimeError("UTF-8 corrupted")

    # --- fields for classic/general roots ---
    if "_leftClassicRoot" not in text:
        text = must_replace(
            text,
            "        private readonly List<BattlePlayerCardView> _leftBattleCards = new List<BattlePlayerCardView>();\n"
            "        private readonly List<BattlePlayerCardView> _rightBattleCards = new List<BattlePlayerCardView>();\n",
            "        private readonly List<BattlePlayerCardView> _leftBattleCards = new List<BattlePlayerCardView>();\n"
            "        private readonly List<BattlePlayerCardView> _rightBattleCards = new List<BattlePlayerCardView>();\n"
            "        private RectTransform _leftClassicRoot;\n"
            "        private RectTransform _rightClassicRoot;\n"
            "        private RectTransform _leftGeneralRoot;\n"
            "        private RectTransform _rightGeneralRoot;\n",
        )

    # --- CreateStatSlots: build classic + general ---
    old_create = """        private void CreateStatSlots(Transform parent, bool leftSide)
        {
            List<Text> slots = leftSide ? _leftStatSlots : _rightStatSlots;
            List<Text> roundKills = leftSide ? _leftRoundKillSlots : _rightRoundKillSlots;
            List<StatMetricSlot[]> statuses = leftSide ? _leftStatusRows : _rightStatusRows;
            List<Image> jerseys = leftSide ? _leftJerseyBadges : _rightJerseyBadges;
            List<Text> jerseyNums = leftSide ? _leftJerseyNumbers : _rightJerseyNumbers;
            List<Image> rows = leftSide ? _leftStatRows : _rightStatRows;
            List<string> playerIds = leftSide ? _leftStatPlayerIds : _rightStatPlayerIds;
            List<BattlePlayerCardView> cards = leftSide ? _leftBattleCards : _rightBattleCards;

            Sprite killIcon = _statIcons != null && _statIcons.Length > 0 ? _statIcons[0] : null;
            Sprite deathIcon = _statIcons != null && _statIcons.Length > 1 ? _statIcons[1] : null;
            Sprite assistIcon = _statIcons != null && _statIcons.Length > 2 ? _statIcons[2] : null;
            Sprite staminaIcon = _statIcons != null && _statIcons.Length > 3 ? _statIcons[3] : null;
            Sprite mentalityIcon = _statIcons != null && _statIcons.Length > 4 ? _statIcons[4] : null;
            Sprite formIcon = _statIcons != null && _statIcons.Length > 5 ? _statIcons[5] : null;

            for (int i = 0; i < 5; i++)
            {
                BattlePlayerCardView card = BattleStatPanelPresentation.CreateCard(
                    parent,
                    leftSide,
                    i,
                    _scoreboardFont ?? _uiFont,
                    killIcon,
                    deathIcon,
                    assistIcon,
                    mentalityIcon,
                    formIcon,
                    staminaIcon,
                    OnStatRowPointerEnter,
                    OnStatRowPointerExit);
                BattleStatPanelPresentation.LayoutCardInColumn(card, i);
                cards.Add(card);

                rows.Add(card.Root);
                playerIds.Add(string.Empty);
                jerseys.Add(card.NumberBar);
                jerseyNums.Add(card.NumberText);
                slots.Add(card.PlayerName);
                roundKills.Add(null);
                statuses.Add(null);
            }
        }"""

    new_create = """        private void CreateStatSlots(Transform parent, bool leftSide)
        {
            Image classicHost = CreateUiObject<Image>("ClassicRoot", parent);
            Stretch(classicHost.rectTransform);
            classicHost.color = Color.clear;
            classicHost.raycastTarget = false;
            Image generalHost = CreateUiObject<Image>("GeneralRoot", parent);
            Stretch(generalHost.rectTransform);
            generalHost.color = Color.clear;
            generalHost.raycastTarget = false;
            if (leftSide)
            {
                _leftClassicRoot = classicHost.rectTransform;
                _leftGeneralRoot = generalHost.rectTransform;
            }
            else
            {
                _rightClassicRoot = classicHost.rectTransform;
                _rightGeneralRoot = generalHost.rectTransform;
            }

            CreateClassicStatSlots(classicHost.transform, leftSide);
            CreateGeneralStatSlots(generalHost.transform, leftSide);
            classicHost.gameObject.SetActive(true);
            generalHost.gameObject.SetActive(false);
        }

        private void CreateClassicStatSlots(Transform parent, bool leftSide)
        {
            List<Text> slots = leftSide ? _leftStatSlots : _rightStatSlots;
            List<Text> roundKills = leftSide ? _leftRoundKillSlots : _rightRoundKillSlots;
            List<StatMetricSlot[]> statuses = leftSide ? _leftStatusRows : _rightStatusRows;
            List<Image> jerseys = leftSide ? _leftJerseyBadges : _rightJerseyBadges;
            List<Text> jerseyNums = leftSide ? _leftJerseyNumbers : _rightJerseyNumbers;
            List<Image> rows = leftSide ? _leftStatRows : _rightStatRows;
            List<string> playerIds = leftSide ? _leftStatPlayerIds : _rightStatPlayerIds;
            const float topStart = 0.91f;
            const float bottomPad = 0.02f;
            const float rowGap = 0.01f;
            float rowHeight = (topStart - bottomPad - 4f * rowGap) / 5f;
            float top = topStart;
            for (int i = 0; i < 5; i++)
            {
                float bottom = top - rowHeight;
                Image row = CreateUiObject<Image>("StatRow" + i, parent);
                SetRect(row.rectTransform, new Vector2(0.05f, bottom), new Vector2(0.95f, top));
                row.color = AliveRowBg;
                row.raycastTarget = true;
                rows.Add(row);
                playerIds.Add(string.Empty);

                int slotIndex = i;
                bool sideIsLeft = leftSide;
                EventTrigger trigger = row.gameObject.AddComponent<EventTrigger>();
                AddPointerTrigger(trigger, EventTriggerType.PointerEnter, _ => OnStatRowPointerEnter(sideIsLeft, slotIndex));
                AddPointerTrigger(trigger, EventTriggerType.PointerExit, _ => OnStatRowPointerExit(sideIsLeft, slotIndex));

                Image badge = CreateUiObject<Image>("Jersey", row.transform);
                badge.sprite = _circleSprite;
                badge.color = CtText;
                badge.raycastTarget = false;
                badge.rectTransform.anchorMin = badge.rectTransform.anchorMax =
                    leftSide ? new Vector2(0.10f, 0.70f) : new Vector2(0.90f, 0.70f);
                badge.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                badge.rectTransform.sizeDelta = new Vector2(18f, 18f);
                badge.rectTransform.anchoredPosition = Vector2.zero;
                jerseys.Add(badge);

                Text jerseyNum = CreateText("JerseyNum", badge.transform, 11, TextAnchor.MiddleCenter, TextPrimary);
                Stretch(jerseyNum.rectTransform);
                jerseyNum.fontStyle = FontStyle.Bold;
                jerseyNum.raycastTarget = false;
                jerseyNum.text = (i + 1).ToString();
                jerseyNums.Add(jerseyNum);

                Text line = CreateText("Line", row.transform, 16, leftSide ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight, TextPrimary);
                SetRect(line.rectTransform,
                    leftSide ? new Vector2(0.18f, 0.34f) : new Vector2(0.28f, 0.34f),
                    leftSide ? new Vector2(0.68f, 1f) : new Vector2(0.80f, 1f));
                line.rectTransform.offsetMin = new Vector2(leftSide ? 2f : 4f, 0f);
                line.rectTransform.offsetMax = new Vector2(leftSide ? -4f : -2f, -2f);
                line.raycastTarget = false;
                line.text = "—";
                slots.Add(line);

                Text rk = CreateText("RoundKills", row.transform, 20, TextAnchor.MiddleCenter, CtText);
                SetRect(rk.rectTransform,
                    leftSide ? new Vector2(0.70f, 0.42f) : new Vector2(0.02f, 0.42f),
                    leftSide ? new Vector2(0.98f, 0.95f) : new Vector2(0.26f, 0.95f));
                rk.fontStyle = FontStyle.Bold;
                rk.raycastTarget = false;
                rk.text = string.Empty;
                roundKills.Add(rk);

                statuses.Add(CreateStatusMetricRow(row.transform));
                top = bottom - rowGap;
            }
        }

        private void CreateGeneralStatSlots(Transform parent, bool leftSide)
        {
            List<BattlePlayerCardView> cards = leftSide ? _leftBattleCards : _rightBattleCards;
            Sprite killIcon = _statIcons != null && _statIcons.Length > 0 ? _statIcons[0] : null;
            Sprite deathIcon = _statIcons != null && _statIcons.Length > 1 ? _statIcons[1] : null;
            Sprite assistIcon = _statIcons != null && _statIcons.Length > 2 ? _statIcons[2] : null;
            Sprite staminaIcon = _statIcons != null && _statIcons.Length > 3 ? _statIcons[3] : null;
            Sprite mentalityIcon = _statIcons != null && _statIcons.Length > 4 ? _statIcons[4] : null;
            Sprite formIcon = _statIcons != null && _statIcons.Length > 5 ? _statIcons[5] : null;

            for (int i = 0; i < 5; i++)
            {
                BattlePlayerCardView card = BattleStatPanelPresentation.CreateCard(
                    parent,
                    leftSide,
                    i,
                    _scoreboardFont ?? _uiFont,
                    killIcon,
                    deathIcon,
                    assistIcon,
                    mentalityIcon,
                    formIcon,
                    staminaIcon,
                    OnStatRowPointerEnter,
                    OnStatRowPointerExit);
                BattleStatPanelPresentation.LayoutCardInColumn(card, i);
                cards.Add(card);
            }
        }"""

    if "CreateClassicStatSlots" not in text:
        text = must_replace(text, old_create, new_create)

    # --- ApplyScoreboardModeChrome after ApplyControlBarLayout ---
    if "ApplyScoreboardModeChrome" not in text:
        text = must_replace(
            text,
            "            ApplyControlBarLayout(simulation);\n"
            "            ApplySettingsFrostedChrome(true);\n",
            "            ApplyControlBarLayout(simulation);\n"
            "            ApplyScoreboardModeChrome();\n"
            "            ApplySettingsFrostedChrome(true);\n",
        )

    chrome_method = """
        private void ApplyScoreboardModeChrome()
        {
            bool general = IsGeneralBattleMode();
            if (_leftClassicRoot != null)
                _leftClassicRoot.gameObject.SetActive(!general);
            if (_rightClassicRoot != null)
                _rightClassicRoot.gameObject.SetActive(!general);
            if (_leftGeneralRoot != null)
                _leftGeneralRoot.gameObject.SetActive(general);
            if (_rightGeneralRoot != null)
                _rightGeneralRoot.gameObject.SetActive(general);

            if (_leftCpText != null)
                _leftCpText.gameObject.SetActive(general);
            if (_rightCpText != null)
                _rightCpText.gameObject.SetActive(general);
            if (_leftCardText != null)
                _leftCardText.gameObject.SetActive(general);
            if (_rightCardText != null)
                _rightCardText.gameObject.SetActive(general);

            if (general)
            {
                if (_leftTeamHeader != null)
                    SetRect(_leftTeamHeader.rectTransform, new Vector2(0.02f, 0.08f), new Vector2(0.30f, 0.92f));
                if (_leftMoneyText != null)
                    SetRect(_leftMoneyText.rectTransform, new Vector2(0.30f, 0.08f), new Vector2(0.50f, 0.92f));
                if (_rightTeamHeader != null)
                    SetRect(_rightTeamHeader.rectTransform, new Vector2(0.70f, 0.08f), new Vector2(0.98f, 0.92f));
                if (_rightMoneyText != null)
                    SetRect(_rightMoneyText.rectTransform, new Vector2(0.50f, 0.08f), new Vector2(0.70f, 0.92f));
                if (_leftTeamHeaderBlock != null)
                    SetRect(_leftTeamHeaderBlock.rectTransform, new Vector2(0.006f, 0.708f), new Vector2(0.205f, 0.758f));
                if (_rightTeamHeaderBlock != null)
                    SetRect(_rightTeamHeaderBlock.rectTransform, new Vector2(0.795f, 0.708f), new Vector2(0.994f, 0.758f));
                if (_leftStatsPanel != null)
                {
                    SetRect(_leftStatsPanel.rectTransform, new Vector2(0.006f, 0.018f), new Vector2(0.205f, 0.700f));
                    _leftStatsPanel.color = Color.clear;
                }
                if (_rightStatsPanel != null)
                {
                    SetRect(_rightStatsPanel.rectTransform, new Vector2(0.795f, 0.018f), new Vector2(0.994f, 0.700f));
                    _rightStatsPanel.color = Color.clear;
                }
            }
            else
            {
                // Quick mode: original monolithic side panels (figure 1).
                if (_leftTeamHeader != null)
                    SetRect(_leftTeamHeader.rectTransform, new Vector2(0.04f, 0.08f), new Vector2(0.40f, 0.92f));
                if (_leftMoneyText != null)
                    SetRect(_leftMoneyText.rectTransform, new Vector2(0.40f, 0.08f), new Vector2(0.96f, 0.92f));
                if (_rightTeamHeader != null)
                    SetRect(_rightTeamHeader.rectTransform, new Vector2(0.04f, 0.08f), new Vector2(0.40f, 0.92f));
                if (_rightMoneyText != null)
                    SetRect(_rightMoneyText.rectTransform, new Vector2(0.40f, 0.08f), new Vector2(0.96f, 0.92f));
                if (_leftTeamHeaderBlock != null)
                    SetRect(_leftTeamHeaderBlock.rectTransform, new Vector2(0.008f, 0.720f), new Vector2(0.165f, 0.775f));
                if (_rightTeamHeaderBlock != null)
                    SetRect(_rightTeamHeaderBlock.rectTransform, new Vector2(0.835f, 0.720f), new Vector2(0.992f, 0.775f));
                if (_leftStatsPanel != null)
                    SetRect(_leftStatsPanel.rectTransform, new Vector2(0.008f, 0.112f), new Vector2(0.165f, 0.775f));
                if (_rightStatsPanel != null)
                    SetRect(_rightStatsPanel.rectTransform, new Vector2(0.835f, 0.112f), new Vector2(0.992f, 0.775f));
            }

            UpdateSidePanelColors();
            UpdateTeamHeaders();
        }
"""

    if "private void ApplyScoreboardModeChrome()" not in text:
        text = must_replace(
            text,
            "        private void RefreshOfficialMatchMeta()\n",
            chrome_method + "\n        private void RefreshOfficialMatchMeta()\n",
        )

    # --- UpdateSidePanelColors: mode-aware ---
    old_colors = """        private void UpdateSidePanelColors()
        {
            if (_leftTeamHeaderBlock == null || _rightTeamHeaderBlock == null)
                return;

            string leftId = myTeamId;
            string rightId = _opponentTeamId;
            bool leftIsT = TeamIsTerrorist(leftId, _currentRound);
            bool rightIsT = TeamIsTerrorist(rightId, _currentRound);
            _leftTeamHeaderBlock.color = BattleStatPanelPresentation.AccentColor(leftIsT);
            _rightTeamHeaderBlock.color = BattleStatPanelPresentation.AccentColor(rightIsT);
            if (_leftStatsPanel != null)
                _leftStatsPanel.color = Color.clear;
            if (_rightStatsPanel != null)
                _rightStatsPanel.color = Color.clear;
            RefreshMarkerSideColors();
            RefreshStatPanels();
            UpdateAliveCountHud();
        }"""

    new_colors = """        private void UpdateSidePanelColors()
        {
            if (_leftStatsPanel == null || _rightStatsPanel == null)
                return;

            string leftId = myTeamId;
            string rightId = _opponentTeamId;
            bool leftIsT = TeamIsTerrorist(leftId, _currentRound);
            bool rightIsT = TeamIsTerrorist(rightId, _currentRound);
            bool general = IsGeneralBattleMode();
            if (general)
            {
                if (_leftTeamHeaderBlock != null)
                    _leftTeamHeaderBlock.color = BattleStatPanelPresentation.AccentColor(leftIsT);
                if (_rightTeamHeaderBlock != null)
                    _rightTeamHeaderBlock.color = BattleStatPanelPresentation.AccentColor(rightIsT);
                _leftStatsPanel.color = Color.clear;
                _rightStatsPanel.color = Color.clear;
            }
            else
            {
                Color leftPanel = leftIsT ? TPanel : CtPanel;
                Color rightPanel = rightIsT ? TPanel : CtPanel;
                _leftStatsPanel.color = leftPanel;
                _rightStatsPanel.color = rightPanel;
                if (_leftTeamHeaderBlock != null)
                    _leftTeamHeaderBlock.color = leftPanel;
                if (_rightTeamHeaderBlock != null)
                    _rightTeamHeaderBlock.color = rightPanel;
            }
            RefreshMarkerSideColors();
            RefreshStatPanels();
            UpdateAliveCountHud();
        }"""

    if "bool general = IsGeneralBattleMode();\n            if (general)" not in text.split("UpdateSidePanelColors")[1][:900]:
        text = must_replace(text, old_colors, new_colors)

    # --- UpdateTeamHeaders text color ---
    old_th = """            Color leftInk = BattleStatPanelPresentation.PanelColor(leftIsT);
            Color rightInk = BattleStatPanelPresentation.PanelColor(rightIsT);
            if (_leftTeamHeader != null)
            {
                _leftTeamHeader.text = DemoZh.TeamName(leftId);
                _leftTeamHeader.color = leftInk;
            }
            if (_rightTeamHeader != null)
            {
                _rightTeamHeader.text = DemoZh.TeamName(rightId);
                _rightTeamHeader.color = rightInk;
            }"""

    new_th = """            bool general = IsGeneralBattleMode();
            Color leftInk = general ? BattleStatPanelPresentation.PanelColor(leftIsT) : TextPrimary;
            Color rightInk = general ? BattleStatPanelPresentation.PanelColor(rightIsT) : TextPrimary;
            if (_leftTeamHeader != null)
            {
                _leftTeamHeader.text = DemoZh.TeamName(leftId);
                _leftTeamHeader.color = leftInk;
            }
            if (_rightTeamHeader != null)
            {
                _rightTeamHeader.text = DemoZh.TeamName(rightId);
                _rightTeamHeader.color = rightInk;
            }"""

    if "bool general = IsGeneralBattleMode();\n            Color leftInk = general" not in text:
        text = must_replace(text, old_th, new_th)

    # --- UpdateEconomyHud: quick combines line ---
    old_eco = """            Color leftInk = BattleStatPanelPresentation.PanelColor(leftIsT);
            Color rightInk = BattleStatPanelPresentation.PanelColor(rightIsT);
            BindTeamEconomyTexts(leftId, leftInk, _leftMoneyText, _leftCpText, _leftCardText);
            BindTeamEconomyTexts(rightId, rightInk, _rightMoneyText, _rightCpText, _rightCardText);
        }"""

    new_eco = """            bool general = IsGeneralBattleMode();
            Color leftInk = general ? BattleStatPanelPresentation.PanelColor(leftIsT) : TextPrimary;
            Color rightInk = general ? BattleStatPanelPresentation.PanelColor(rightIsT) : TextPrimary;
            if (general)
            {
                BindTeamEconomyTexts(leftId, leftInk, _leftMoneyText, _leftCpText, _leftCardText);
                BindTeamEconomyTexts(rightId, rightInk, _rightMoneyText, _rightCpText, _rightCardText);
            }
            else
            {
                if (_leftMoneyText != null)
                {
                    _leftMoneyText.color = leftInk;
                    _leftMoneyText.text = FormatEconomyMoney(leftId) + " · " + FormatEconomyCp(leftId) + " · " + FormatEconomyCardName(leftId);
                }
                if (_rightMoneyText != null)
                {
                    _rightMoneyText.color = rightInk;
                    _rightMoneyText.text = FormatEconomyMoney(rightId) + " · " + FormatEconomyCp(rightId) + " · " + FormatEconomyCardName(rightId);
                }
            }
        }"""

    if "FormatEconomyMoney(leftId) + \" · \"" not in text:
        text = must_replace(text, old_eco, new_eco)

    # --- BindStatSlots: branch classic vs general ---
    old_bind = """        private void BindStatSlots(
            List<Text> slots, List<Text> roundKillSlots, List<StatMetricSlot[]> statusRows, TeamDefinition team)
        {
            bool leftSide = ReferenceEquals(slots, _leftStatSlots);
            List<BattlePlayerCardView> cards = leftSide ? _leftBattleCards : _rightBattleCards;
            List<string> rowPlayerIds = leftSide ? _leftStatPlayerIds : _rightStatPlayerIds;
            bool teamIsT = TeamIsTerrorist(team.id, _currentRound);
            Color panelDark = BattleStatPanelPresentation.PanelColor(teamIsT);
            Color accentLight = BattleStatPanelPresentation.AccentColor(teamIsT);
            int teamMoney = _teamMoney.TryGetValue(team.id, out int purse) ? purse : EconomyRules.StartingTeamMoney;

            for (int i = 0; i < cards.Count; i++)
"""

    # Find the full BindStatSlots method and replace with dual version - read current end
    bind_start = text.find("        private void BindStatSlots(\n            List<Text> slots")
    bind_end = text.find("        private string ResolveWeaponDisplayName")
    if bind_start < 0 or bind_end < 0:
        raise RuntimeError("BindStatSlots bounds missing")

    new_bind = """        private void BindStatSlots(
            List<Text> slots, List<Text> roundKillSlots, List<StatMetricSlot[]> statusRows, TeamDefinition team)
        {
            if (IsGeneralBattleMode())
                BindGeneralStatSlots(slots, team);
            else
                BindClassicStatSlots(slots, roundKillSlots, statusRows, team);
        }

        private void BindClassicStatSlots(
            List<Text> slots, List<Text> roundKillSlots, List<StatMetricSlot[]> statusRows, TeamDefinition team)
        {
            bool leftSide = ReferenceEquals(slots, _leftStatSlots);
            List<Image> jerseys = leftSide ? _leftJerseyBadges : _rightJerseyBadges;
            List<Text> jerseyNums = leftSide ? _leftJerseyNumbers : _rightJerseyNumbers;
            List<Image> rows = leftSide ? _leftStatRows : _rightStatRows;
            List<string> rowPlayerIds = leftSide ? _leftStatPlayerIds : _rightStatPlayerIds;
            bool teamIsT = TeamIsTerrorist(team.id, _currentRound);
            Color roundKillColor = teamIsT ? TText : CtText;
            Color sideColor = SideBadgeColor(teamIsT);
            for (int i = 0; i < slots.Count; i++)
            {
                if (i >= team.playerIds.Length)
                {
                    if (slots[i] != null)
                        slots[i].text = string.Empty;
                    if (i < roundKillSlots.Count && roundKillSlots[i] != null)
                        roundKillSlots[i].text = string.Empty;
                    if (i < statusRows.Count)
                        ClearStatusMetrics(statusRows[i]);
                    if (i < jerseys.Count && jerseys[i] != null)
                        jerseys[i].gameObject.SetActive(false);
                    if (i < rows.Count && rows[i] != null)
                        rows[i].color = AliveRowBg;
                    if (i < rowPlayerIds.Count)
                        rowPlayerIds[i] = string.Empty;
                    continue;
                }

                string id = team.playerIds[i];
                int number = PlayerNumber(id);
                PlayerStat stat = _stats.TryGetValue(id, out PlayerStat found) ? found : new PlayerStat();
                bool alive = stat.Alive;
                Color nameColor = alive ? TextPrimary : DeadRowText;
                Color jerseyColor = alive ? sideColor : DeadRowBadge;
                Color jerseyNumColor = alive ? TextPrimary : DeadRowMuted;
                Color rkColor = alive ? roundKillColor : DeadRowMuted;
                slots[i].text = DemoZh.PlayerName(id);
                slots[i].color = nameColor;
                _statLines[id] = slots[i];
                if (i < rowPlayerIds.Count)
                    rowPlayerIds[i] = id;
                if (i < rows.Count && rows[i] != null)
                    rows[i].color = alive ? AliveRowBg : DeadRowBg;
                if (i < jerseys.Count && jerseys[i] != null)
                {
                    jerseys[i].gameObject.SetActive(true);
                    jerseys[i].color = jerseyColor;
                }
                if (i < jerseyNums.Count && jerseyNums[i] != null)
                {
                    jerseyNums[i].text = number > 0 ? number.ToString() : string.Empty;
                    jerseyNums[i].color = jerseyNumColor;
                }
                if (i < roundKillSlots.Count && roundKillSlots[i] != null)
                {
                    roundKillSlots[i].color = rkColor;
                    roundKillSlots[i].text = stat.RoundKills > 0 ? stat.RoundKills.ToString() : string.Empty;
                }
                if (i < statusRows.Count)
                    BindStatusMetrics(statusRows[i], stat, alive);
            }
        }

        private void BindGeneralStatSlots(List<Text> slots, TeamDefinition team)
        {
            bool leftSide = ReferenceEquals(slots, _leftStatSlots);
            List<BattlePlayerCardView> cards = leftSide ? _leftBattleCards : _rightBattleCards;
            List<string> rowPlayerIds = leftSide ? _leftStatPlayerIds : _rightStatPlayerIds;
            bool teamIsT = TeamIsTerrorist(team.id, _currentRound);
            Color panelDark = BattleStatPanelPresentation.PanelColor(teamIsT);
            Color accentLight = BattleStatPanelPresentation.AccentColor(teamIsT);
            int teamMoney = _teamMoney.TryGetValue(team.id, out int purse) ? purse : EconomyRules.StartingTeamMoney;

            for (int i = 0; i < cards.Count; i++)
            {
                BattlePlayerCardView card = cards[i];
                if (i >= team.playerIds.Length)
                {
                    BattleStatPanelPresentation.Clear(card);
                    if (i < rowPlayerIds.Count)
                        rowPlayerIds[i] = string.Empty;
                    continue;
                }

                string id = team.playerIds[i];
                PlayerStat stat = _stats.TryGetValue(id, out PlayerStat found) ? found : new PlayerStat();
                PlayerRuntime runtime = FindPlayerRuntime(id);
                int health = stat.Health;
                int money = teamMoney;
                string primaryName = string.Empty;
                string secondaryName = string.Empty;
                bool hasKit = false;
                BattleArmorHudState armor = BattleArmorHudState.None;
                if (runtime != null)
                {
                    if (runtime.Health > 0 || !stat.Alive)
                        health = runtime.Health;
                    money = runtime.Money;
                    if (runtime.Loadout != null)
                    {
                        primaryName = ResolveWeaponDisplayName(runtime.Loadout.PrimaryWeaponId);
                        secondaryName = ResolveWeaponDisplayName(runtime.Loadout.PistolWeaponId);
                        hasKit = runtime.Loadout.HasDefuseKit;
                        if (runtime.Loadout.HasArmor && runtime.Loadout.HasHelmet)
                            armor = BattleArmorHudState.Full;
                        else if (runtime.Loadout.HasArmor)
                            armor = BattleArmorHudState.Half;
                    }
                    if (!stat.Alive)
                        health = 0;
                }
                else if (!stat.Alive)
                {
                    health = 0;
                }

                var data = new BattlePlayerCardData
                {
                    PlayerId = id,
                    DisplayName = DemoZh.PlayerName(id),
                    JerseyNumber = PlayerNumber(id),
                    Health = health,
                    Kills = stat.Kills,
                    Deaths = stat.Deaths,
                    RoundKills = stat.RoundKills,
                    Money = money,
                    PrimaryWeaponName = primaryName,
                    SecondaryWeaponName = secondaryName,
                    HasDefuseKit = hasKit,
                    Armor = armor,
                    Mentality = stat.Mentality,
                    Form = stat.Form,
                    Stamina = stat.Stamina,
                    Alive = stat.Alive
                };
                BattleStatPanelPresentation.Bind(card, data, panelDark, accentLight);
                if (i < rowPlayerIds.Count)
                    rowPlayerIds[i] = id;
            }
        }

        private string ResolveWeaponDisplayName"""

    text = text[:bind_start] + new_bind + text[bind_end + len("        private string ResolveWeaponDisplayName"):]

    # GetStatRowPlayerId: classic uses _leftStatPlayerIds which classic bind fills;
    # general also fills rowPlayerIds in BindGeneralStatSlots. Good.
    # But classic and general share _leftStatPlayerIds - only one mode active. OK.

    # Hover for general: OnStatRowPointerEnter uses slot index - classic and general both use same indices.
    # General cards wire OnStatRowPointerEnter; classic rows too. When classic hidden, only general receives. OK.

    TARGET.write_text(text, encoding="utf-8")
    verify = TARGET.read_text(encoding="utf-8")
    if "擊殺" not in verify or "跳過結算中" not in verify:
        raise RuntimeError("UTF-8 corrupted after write")
    if "CreateClassicStatSlots" not in verify or "ApplyScoreboardModeChrome" not in verify:
        raise RuntimeError("dual mode incomplete")
    if "BindClassicStatSlots" not in verify or "BindGeneralStatSlots" not in verify:
        raise RuntimeError("bind branch incomplete")
    print("patched dual scoreboard modes OK")


if __name__ == "__main__":
    main()
