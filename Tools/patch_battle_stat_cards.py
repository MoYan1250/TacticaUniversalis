# -*- coding: utf-8 -*-
"""Redesign battle side scoreboard into independent CS-style player cards."""
from pathlib import Path
import backup_quickmatch as bak

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"
CSPROJ = ROOT / "TacticaUniversalis.Presentation.csproj"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing:\n{old[:220]!r}")
    return text.replace(old, new, 1)


def main() -> None:
    bak.backup("before_battle_stat_cards")
    text = TARGET.read_text(encoding="utf-8")
    if "擊殺" not in text or "跳過結算中" not in text:
        raise RuntimeError("UTF-8 corrupted (missing Chinese anchors)")

    # --- fields: battle card lists ---
    if "_leftBattleCards" not in text:
        text = must_replace(
            text,
            "        private readonly List<string> _leftStatPlayerIds = new List<string>();\n"
            "        private readonly List<string> _rightStatPlayerIds = new List<string>();\n",
            "        private readonly List<string> _leftStatPlayerIds = new List<string>();\n"
            "        private readonly List<string> _rightStatPlayerIds = new List<string>();\n"
            "        private readonly List<BattlePlayerCardView> _leftBattleCards = new List<BattlePlayerCardView>();\n"
            "        private readonly List<BattlePlayerCardView> _rightBattleCards = new List<BattlePlayerCardView>();\n"
            "        private Image _leftTeamHeaderBlock;\n"
            "        private Image _rightTeamHeaderBlock;\n",
        )

    # --- PlayerStat Health ---
    if "public int Health = 100;" not in text:
        text = must_replace(
            text,
            "        private sealed class PlayerStat\n"
            "        {\n"
            "            public int Kills;\n"
            "            public int Deaths;\n"
            "            public int Assists;\n"
            "            public int RoundKills;\n"
            "            public bool Alive = true;\n"
            "            public float Stamina = 100f;\n"
            "            public float Mentality = 50f;\n"
            "            public float Form = 50f;\n"
            "        }\n",
            "        private sealed class PlayerStat\n"
            "        {\n"
            "            public int Kills;\n"
            "            public int Deaths;\n"
            "            public int Assists;\n"
            "            public int RoundKills;\n"
            "            public int Health = 100;\n"
            "            public bool Alive = true;\n"
            "            public float Stamina = 100f;\n"
            "            public float Mentality = 50f;\n"
            "            public float Form = 50f;\n"
            "        }\n",
        )

    # --- BuildInterface: transparent columns + independent team headers ---
    old_panels = """            // Side KDA panels end above the bottom control bar (no empty overlap strip)
            Image leftPanel = CreateUiObject<Image>("LeftStats", canvas.transform);
            SetRect(leftPanel.rectTransform, new Vector2(0.008f, 0.112f), new Vector2(0.165f, 0.775f));
            leftPanel.color = new Color(CtPanel.r, CtPanel.g, CtPanel.b, 0.78f);
            _leftStatsPanel = leftPanel;
            _leftTeamHeader = CreateText("LeftHeader", leftPanel.transform, 16, TextAnchor.MiddleLeft, TextPrimary);
            SetRect(_leftTeamHeader.rectTransform, new Vector2(0.04f, 0.93f), new Vector2(0.40f, 0.995f));
            _leftTeamHeader.fontStyle = FontStyle.Bold;
            _leftMoneyText = CreateText("LeftMoney", leftPanel.transform, 12, TextAnchor.MiddleRight, TextPrimary);
            SetRect(_leftMoneyText.rectTransform, new Vector2(0.40f, 0.93f), new Vector2(0.96f, 0.995f));

            Image rightPanel = CreateUiObject<Image>("RightStats", canvas.transform);
            SetRect(rightPanel.rectTransform, new Vector2(0.835f, 0.112f), new Vector2(0.992f, 0.775f));
            rightPanel.color = new Color(TPanel.r, TPanel.g, TPanel.b, 0.90f);
            _rightStatsPanel = rightPanel;
            _rightTeamHeader = CreateText("RightHeader", rightPanel.transform, 16, TextAnchor.MiddleLeft, TextPrimary);
            SetRect(_rightTeamHeader.rectTransform, new Vector2(0.04f, 0.93f), new Vector2(0.40f, 0.995f));
            _rightTeamHeader.fontStyle = FontStyle.Bold;
            _rightMoneyText = CreateText("RightMoney", rightPanel.transform, 12, TextAnchor.MiddleRight, TextPrimary);
            SetRect(_rightMoneyText.rectTransform, new Vector2(0.40f, 0.93f), new Vector2(0.96f, 0.995f));

            CreateStatSlots(leftPanel.transform, true);
            CreateStatSlots(rightPanel.transform, false);"""

    # Fix TPanel alpha - actual file has 0.78f for both
    old_panels = """            // Side KDA panels end above the bottom control bar (no empty overlap strip)
            Image leftPanel = CreateUiObject<Image>("LeftStats", canvas.transform);
            SetRect(leftPanel.rectTransform, new Vector2(0.008f, 0.112f), new Vector2(0.165f, 0.775f));
            leftPanel.color = new Color(CtPanel.r, CtPanel.g, CtPanel.b, 0.78f);
            _leftStatsPanel = leftPanel;
            _leftTeamHeader = CreateText("LeftHeader", leftPanel.transform, 16, TextAnchor.MiddleLeft, TextPrimary);
            SetRect(_leftTeamHeader.rectTransform, new Vector2(0.04f, 0.93f), new Vector2(0.40f, 0.995f));
            _leftTeamHeader.fontStyle = FontStyle.Bold;
            _leftMoneyText = CreateText("LeftMoney", leftPanel.transform, 12, TextAnchor.MiddleRight, TextPrimary);
            SetRect(_leftMoneyText.rectTransform, new Vector2(0.40f, 0.93f), new Vector2(0.96f, 0.995f));

            Image rightPanel = CreateUiObject<Image>("RightStats", canvas.transform);
            SetRect(rightPanel.rectTransform, new Vector2(0.835f, 0.112f), new Vector2(0.992f, 0.775f));
            rightPanel.color = new Color(TPanel.r, TPanel.g, TPanel.b, 0.78f);
            _rightStatsPanel = rightPanel;
            _rightTeamHeader = CreateText("RightHeader", rightPanel.transform, 16, TextAnchor.MiddleLeft, TextPrimary);
            SetRect(_rightTeamHeader.rectTransform, new Vector2(0.04f, 0.93f), new Vector2(0.40f, 0.995f));
            _rightTeamHeader.fontStyle = FontStyle.Bold;
            _rightMoneyText = CreateText("RightMoney", rightPanel.transform, 12, TextAnchor.MiddleRight, TextPrimary);
            SetRect(_rightMoneyText.rectTransform, new Vector2(0.40f, 0.93f), new Vector2(0.96f, 0.995f));

            CreateStatSlots(leftPanel.transform, true);
            CreateStatSlots(rightPanel.transform, false);"""

    new_panels = """            // Independent team headers (not part of a shared side block).
            _leftTeamHeaderBlock = CreateUiObject<Image>("LeftTeamHeader", canvas.transform);
            SetRect(_leftTeamHeaderBlock.rectTransform, new Vector2(0.008f, 0.782f), new Vector2(0.165f, 0.828f));
            _leftTeamHeaderBlock.color = new Color(CtPanel.r, CtPanel.g, CtPanel.b, 0.82f);
            _leftTeamHeaderBlock.raycastTarget = false;
            _leftTeamHeader = CreateText("LeftHeader", _leftTeamHeaderBlock.transform, 15, TextAnchor.MiddleLeft, TextPrimary);
            SetRect(_leftTeamHeader.rectTransform, new Vector2(0.04f, 0.08f), new Vector2(0.42f, 0.92f));
            _leftTeamHeader.fontStyle = FontStyle.Bold;
            _leftMoneyText = CreateText("LeftMoney", _leftTeamHeaderBlock.transform, 11, TextAnchor.MiddleRight, TextPrimary);
            SetRect(_leftMoneyText.rectTransform, new Vector2(0.42f, 0.08f), new Vector2(0.96f, 0.92f));

            _rightTeamHeaderBlock = CreateUiObject<Image>("RightTeamHeader", canvas.transform);
            SetRect(_rightTeamHeaderBlock.rectTransform, new Vector2(0.835f, 0.782f), new Vector2(0.992f, 0.828f));
            _rightTeamHeaderBlock.color = new Color(TPanel.r, TPanel.g, TPanel.b, 0.82f);
            _rightTeamHeaderBlock.raycastTarget = false;
            _rightTeamHeader = CreateText("RightHeader", _rightTeamHeaderBlock.transform, 15, TextAnchor.MiddleLeft, TextPrimary);
            SetRect(_rightTeamHeader.rectTransform, new Vector2(0.04f, 0.08f), new Vector2(0.42f, 0.92f));
            _rightTeamHeader.fontStyle = FontStyle.Bold;
            _rightMoneyText = CreateText("RightMoney", _rightTeamHeaderBlock.transform, 11, TextAnchor.MiddleRight, TextPrimary);
            SetRect(_rightMoneyText.rectTransform, new Vector2(0.42f, 0.08f), new Vector2(0.96f, 0.92f));

            // Transparent columns so gaps between player cards show the match background.
            Image leftPanel = CreateUiObject<Image>("LeftStats", canvas.transform);
            SetRect(leftPanel.rectTransform, new Vector2(0.008f, 0.112f), new Vector2(0.175f, 0.775f));
            leftPanel.color = Color.clear;
            leftPanel.raycastTarget = false;
            _leftStatsPanel = leftPanel;

            Image rightPanel = CreateUiObject<Image>("RightStats", canvas.transform);
            SetRect(rightPanel.rectTransform, new Vector2(0.825f, 0.112f), new Vector2(0.992f, 0.775f));
            rightPanel.color = Color.clear;
            rightPanel.raycastTarget = false;
            _rightStatsPanel = rightPanel;

            CreateStatSlots(leftPanel.transform, true);
            CreateStatSlots(rightPanel.transform, false);"""

    if "LeftTeamHeader" not in text or "_leftTeamHeaderBlock" not in text or "CreateUiObject<Image>(\"LeftTeamHeader\"" not in text:
        if old_panels not in text:
            raise RuntimeError("side panel build block missing/changed")
        text = must_replace(text, old_panels, new_panels)

    # --- CreateStatSlots rewrite ---
    old_create = """        private void CreateStatSlots(Transform parent, bool leftSide)
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

                // Small perfect circle jersey badge (side color applied in BindStatSlots).
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
        }"""

    new_create = """        private void CreateStatSlots(Transform parent, bool leftSide)
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
                    _uiFont,
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

    if "BattleStatPanelPresentation.CreateCard" not in text:
        text = must_replace(text, old_create, new_create)

    # --- UpdateSidePanelColors: tint team header blocks instead of column ---
    old_colors = """        private void UpdateSidePanelColors()
        {
            if (_leftStatsPanel == null || _rightStatsPanel == null)
                return;

            string leftId = myTeamId;
            string rightId = _opponentTeamId;
            bool leftIsT = TeamIsTerrorist(leftId, _currentRound);
            bool rightIsT = TeamIsTerrorist(rightId, _currentRound);
            _leftStatsPanel.color = leftIsT ? TPanel : CtPanel;
            _rightStatsPanel.color = rightIsT ? TPanel : CtPanel;
            RefreshMarkerSideColors();
            RefreshStatPanels();
            UpdateAliveCountHud();
        }"""

    new_colors = """        private void UpdateSidePanelColors()
        {
            if (_leftTeamHeaderBlock == null || _rightTeamHeaderBlock == null)
                return;

            string leftId = myTeamId;
            string rightId = _opponentTeamId;
            bool leftIsT = TeamIsTerrorist(leftId, _currentRound);
            bool rightIsT = TeamIsTerrorist(rightId, _currentRound);
            Color leftTint = leftIsT ? TPanel : CtPanel;
            Color rightTint = rightIsT ? TPanel : CtPanel;
            _leftTeamHeaderBlock.color = new Color(leftTint.r, leftTint.g, leftTint.b, 0.82f);
            _rightTeamHeaderBlock.color = new Color(rightTint.r, rightTint.g, rightTint.b, 0.82f);
            if (_leftStatsPanel != null)
                _leftStatsPanel.color = Color.clear;
            if (_rightStatsPanel != null)
                _rightStatsPanel.color = Color.clear;
            RefreshMarkerSideColors();
            RefreshStatPanels();
            UpdateAliveCountHud();
        }"""

    if "_leftTeamHeaderBlock.color" not in text:
        text = must_replace(text, old_colors, new_colors)

    # --- BindStatSlots rewrite ---
    old_bind = """        private void BindStatSlots(
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
                    slots[i].text = string.Empty;
                    if (i < roundKillSlots.Count)
                        roundKillSlots[i].text = string.Empty;
                    if (i < statusRows.Count)
                        ClearStatusMetrics(statusRows[i]);
                    if (i < jerseys.Count)
                        jerseys[i].gameObject.SetActive(false);
                    if (i < rows.Count)
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
                if (i < rows.Count)
                    rows[i].color = alive ? AliveRowBg : DeadRowBg;
                if (i < jerseys.Count)
                {
                    jerseys[i].gameObject.SetActive(true);
                    jerseys[i].color = jerseyColor;
                }
                if (i < jerseyNums.Count)
                {
                    jerseyNums[i].text = number > 0 ? number.ToString() : string.Empty;
                    jerseyNums[i].color = jerseyNumColor;
                }
                if (i < roundKillSlots.Count)
                {
                    roundKillSlots[i].color = rkColor;
                    roundKillSlots[i].text = stat.RoundKills > 0 ? stat.RoundKills.ToString() : string.Empty;
                }
                if (i < statusRows.Count)
                    BindStatusMetrics(statusRows[i], stat, alive);
            }
        }"""

    new_bind = """        private void BindStatSlots(
            List<Text> slots, List<Text> roundKillSlots, List<StatMetricSlot[]> statusRows, TeamDefinition team)
        {
            bool leftSide = ReferenceEquals(slots, _leftStatSlots);
            List<BattlePlayerCardView> cards = leftSide ? _leftBattleCards : _rightBattleCards;
            List<string> rowPlayerIds = leftSide ? _leftStatPlayerIds : _rightStatPlayerIds;
            bool teamIsT = TeamIsTerrorist(team.id, _currentRound);
            Color sideColor = SideBadgeColor(teamIsT);
            bool showAssists = _result != null || (_session != null && _session.IsFinished);
            int teamMoney = _teamMoney.TryGetValue(team.id, out int purse) ? purse : EconomyRules.StartingTeamMoney;

            for (int i = 0; i < cards.Count; i++)
            {
                BattlePlayerCardView card = cards[i];
                if (i >= team.playerIds.Length)
                {
                    BattleStatPanelPresentation.Clear(card);
                    if (i < rowPlayerIds.Count)
                        rowPlayerIds[i] = string.Empty;
                    if (i < slots.Count && slots[i] != null)
                        slots[i].text = string.Empty;
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
                    if (IsGeneralBattleMode())
                    {
                        if (runtime.Health > 0 || !stat.Alive)
                            health = runtime.Health;
                        money = runtime.Money;
                    }
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
                    Assists = stat.Assists,
                    RoundKills = stat.RoundKills,
                    Money = money,
                    PrimaryWeaponName = primaryName,
                    SecondaryWeaponName = secondaryName,
                    HasDefuseKit = hasKit,
                    Armor = armor,
                    Mentality = stat.Mentality,
                    Form = stat.Form,
                    Stamina = stat.Stamina,
                    Alive = stat.Alive,
                    ShowAssists = showAssists
                };
                BattleStatPanelPresentation.Bind(card, data, sideColor);
                if (i < rowPlayerIds.Count)
                    rowPlayerIds[i] = id;
                if (i < slots.Count && slots[i] != null)
                {
                    slots[i].text = data.DisplayName;
                    _statLines[id] = slots[i];
                }
            }
        }

        private string ResolveWeaponDisplayName(string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId) || _weaponCatalog == null)
                return string.Empty;
            return _weaponCatalog.TryGet(weaponId, out WeaponDefinition def) && def != null
                ? def.displayName
                : weaponId;
        }"""

    if "BattleStatPanelPresentation.Bind" not in text:
        text = must_replace(text, old_bind, new_bind)

    # --- ApplyStatusUpdate: store HP on PlayerStat ---
    old_hp = """                SetMarkerVitality(evt.actorId, hp, armor);
                return;
            }"""
    new_hp = """                SetMarkerVitality(evt.actorId, hp, armor);
                if (_stats.TryGetValue(evt.actorId, out PlayerStat hpStat))
                {
                    hpStat.Health = Mathf.Clamp(hp, 0, 100);
                    if (hp <= 0)
                        hpStat.Alive = false;
                    RefreshStatPanels();
                }
                return;
            }"""
    if "hpStat.Health" not in text:
        text = must_replace(text, old_hp, new_hp)

    # --- ResetRoundKills: restore Health ---
    old_reset_rk = """            foreach (PlayerStat stat in _stats.Values)
            {
                stat.RoundKills = 0;
                stat.Alive = true;
            }"""
    new_reset_rk = """            foreach (PlayerStat stat in _stats.Values)
            {
                stat.RoundKills = 0;
                stat.Alive = true;
                stat.Health = 100;
            }"""
    if "stat.Health = 100;" not in text:
        text = must_replace(text, old_reset_rk, new_reset_rk)

    # --- ApplyKillStats: zero health on death ---
    old_victim = """            if (!string.IsNullOrEmpty(evt.targetId) && _stats.TryGetValue(evt.targetId, out PlayerStat victim))
            {
                victim.Deaths++;
                victim.Alive = false;
            }"""
    new_victim = """            if (!string.IsNullOrEmpty(evt.targetId) && _stats.TryGetValue(evt.targetId, out PlayerStat victim))
            {
                victim.Deaths++;
                victim.Alive = false;
                victim.Health = 0;
            }"""
    if "victim.Health = 0;" not in text:
        text = must_replace(text, old_victim, new_victim)

    # --- After map finish, refresh so assists appear ---
    old_finish = """            if (_session != null && !_session.IsFinished)
                _session.FinishMatch();
            _result = _session?.Result;
            HideCardPicker();"""
    new_finish = """            if (_session != null && !_session.IsFinished)
                _session.FinishMatch();
            _result = _session?.Result;
            RefreshStatPanels();
            HideCardPicker();"""
    if "RefreshStatPanels();\n            HideCardPicker();" not in text:
        text = must_replace(text, old_finish, new_finish)

    TARGET.write_text(text, encoding="utf-8")
    verify = TARGET.read_text(encoding="utf-8")
    if "擊殺" not in verify or "跳過結算中" not in verify:
        raise RuntimeError("UTF-8 corrupted after write")

    # csproj
    cs = CSPROJ.read_text(encoding="utf-8")
    if "BattleStatPanelPresentation.cs" not in cs:
        cs = cs.replace(
            '    <Compile Include="Assets\\_Project\\Scripts\\Presentation\\QuickMatchDemoController.cs" />\n',
            '    <Compile Include="Assets\\_Project\\Scripts\\Presentation\\BattleStatPanelPresentation.cs" />\n'
            '    <Compile Include="Assets\\_Project\\Scripts\\Presentation\\QuickMatchDemoController.cs" />\n',
        )
        CSPROJ.write_text(cs, encoding="utf-8")

    print("patched battle stat cards OK")


if __name__ == "__main__":
    main()
