# -*- coding: utf-8 -*-
"""Rewrite economy cards: Eco/HalfBuy/ForceBuy/FullBuy/Hero + pistol lock + hero pick."""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "Tools"))
from backup_quickmatch import backup  # noqa: E402

SRC = ROOT / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def replace_all(text: str, old: str, new: str, label: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing: {label}")
    return text.replace(old, new)


def main() -> int:
    backup("economy_strategy_cards")
    text = SRC.read_text(encoding="utf-8")
    if "KillFeed" not in text:
        raise RuntimeError("anchors missing")

    # Fields for hero pick
    needle = "        private bool _awaitingCard;\n        private EquipmentLevel? _selectedCard;"
    insert = (
        "        private bool _awaitingCard;\n"
        "        private bool _awaitingHeroPick;\n"
        "        private EquipmentLevel? _pendingEconomyCard;\n"
        "        private readonly List<string> _heroPickIds = new List<string>();\n"
        "        private readonly List<Button> _heroPickButtons = new List<Button>();\n"
        "        private readonly List<Text> _heroPickLabels = new List<Text>();\n"
        "        private Button _heroConfirmButton;\n"
        "        private Text _heroPickHint;\n"
        "        private GameObject _heroPickRow;\n"
        "        private EquipmentLevel? _selectedCard;"
    )
    text = replace_all(text, needle, insert, "awaitingCard fields")

    # MatchLoop: pistol skip + hero ids to PlayRound
    old_loop = """                _selectedCard = null;
                _awaitingCard = true;
                ShowCardPicker();
                while (_awaitingCard && _selectedCard == null)
                    yield return null;

                EquipmentLevel card = _selectedCard ?? EquipmentLevel.Eco;
                _selectedCard = null;
                _awaitingCard = false;
                HideCardPicker();

                _awaitingTacticalCards = true;
                ShowTacticalCardPicker();
                while (_awaitingTacticalCards)
                    yield return null;
                string[] tacticalCards = _session.TeamById(myTeamId).TacticalDeck.Selected.ToArray();
                HideTacticalCardPicker();

                _focusPickTargetId = string.Empty;
                _session.SetFocusSelection(myTeamId, string.Empty, string.Empty);
                _awaitingFocus = true;
                ShowFocusPicker();
                while (_awaitingFocus)
                    yield return null;
                TeamRuntime focusTeam = _session.TeamById(myTeamId);
                string focusId = focusTeam.SelectedFocusCardId;
                string focusTarget = focusTeam.SelectedFocusTargetId;
                HideFocusPicker();

                int beforeRound = _session.Result.events.Count;
                _session.PlayRound(myTeamId, card, tacticalCards, focusId, focusTarget);"""

    new_loop = """                EquipmentLevel card = EquipmentLevel.Eco;
                string[] heroIds = Array.Empty<string>();
                bool pistolRound = MatchRules.IsPistolRound(_session.State.CompletedRounds);
                if (pistolRound)
                {
                    SetPhaseStatus("手槍局：裝備由選手自行決定");
                    HideCardPicker();
                }
                else
                {
                    _selectedCard = null;
                    _pendingEconomyCard = null;
                    _awaitingHeroPick = false;
                    _heroPickIds.Clear();
                    _awaitingCard = true;
                    ShowCardPicker();
                    while (_awaitingCard && _selectedCard == null)
                        yield return null;

                    card = _selectedCard ?? EquipmentLevel.Eco;
                    heroIds = _heroPickIds.Count > 0 ? _heroPickIds.ToArray() : Array.Empty<string>();
                    _selectedCard = null;
                    _pendingEconomyCard = null;
                    _awaitingHeroPick = false;
                    _awaitingCard = false;
                    HideCardPicker();
                    HideHeroPickRow();
                }

                _awaitingTacticalCards = true;
                ShowTacticalCardPicker();
                while (_awaitingTacticalCards)
                    yield return null;
                string[] tacticalCards = _session.TeamById(myTeamId).TacticalDeck.Selected.ToArray();
                HideTacticalCardPicker();

                _focusPickTargetId = string.Empty;
                _session.SetFocusSelection(myTeamId, string.Empty, string.Empty);
                _awaitingFocus = true;
                ShowFocusPicker();
                while (_awaitingFocus)
                    yield return null;
                TeamRuntime focusTeam = _session.TeamById(myTeamId);
                string focusId = focusTeam.SelectedFocusCardId;
                string focusTarget = focusTeam.SelectedFocusTargetId;
                HideFocusPicker();

                int beforeRound = _session.Result.events.Count;
                _session.PlayRound(myTeamId, card, tacticalCards, focusId, focusTarget, heroIds);"""

    text = replace_all(text, old_loop, new_loop, "MatchLoop economy block")

    # Replace enum arrays
    text = replace_all(
        text,
        "EquipmentLevel.Eco, EquipmentLevel.Light, EquipmentLevel.Standard,\n"
        "                EquipmentLevel.FullBuy, EquipmentLevel.AwpFullBuy",
        "EquipmentLevel.Eco, EquipmentLevel.HalfBuy, EquipmentLevel.ForceBuy,\n"
        "                EquipmentLevel.FullBuy, EquipmentLevel.Hero",
        "levels array",
    )

    # Card accents / descriptions / OnEconomyCardChosen — replace whole methods via markers
    # CardAccentColor
    start = text.find("        private static Color CardAccentColor(EquipmentLevel level)")
    end = text.find("        private string CardDescription(EquipmentLevel level)")
    if start < 0 or end < 0:
        raise RuntimeError("CardAccentColor block missing")
    text = text[:start] + """        private static Color CardAccentColor(EquipmentLevel level)
        {
            switch (level)
            {
                case EquipmentLevel.Eco: return new Color(0.35f, 0.40f, 0.45f, 1f);
                case EquipmentLevel.HalfBuy: return new Color(0.28f, 0.48f, 0.42f, 1f);
                case EquipmentLevel.ForceBuy: return new Color(0.30f, 0.45f, 0.62f, 1f);
                case EquipmentLevel.FullBuy: return new Color(0.62f, 0.42f, 0.22f, 1f);
                case EquipmentLevel.Hero: return new Color(0.55f, 0.28f, 0.32f, 1f);
                default: return new Color(0.35f, 0.35f, 0.38f, 1f);
            }
        }

""" + text[end:]

    start = text.find("        private string CardDescription(EquipmentLevel level)")
    end = text.find("        private void ShowCardPicker()")
    if start < 0 or end < 0:
        raise RuntimeError("CardDescription block missing")
    text = text[:start] + """        private string CardDescription(EquipmentLevel level)
        {
            if (IsGeneralBattleMode())
            {
                switch (level)
                {
                    case EquipmentLevel.Eco:
                        return "純經濟局\\n幾乎不花錢，積蓄下回合全起";
                    case EquipmentLevel.HalfBuy:
                        return "半經濟局\\n護甲加強手槍，保住下回合全起";
                    case EquipmentLevel.ForceBuy:
                        return "強起局\\n孤注一擲，槍械由選手決定";
                    case EquipmentLevel.FullBuy:
                        return "全起局\\n長槍全甲滿道具，槍械由選手決定";
                    case EquipmentLevel.Hero:
                        return "英雄槍局\\n指定1至2名選手起關鍵武器";
                    default:
                        return "全隊經濟意圖";
                }
            }
            float mult = EconomyRules.EquipmentMultiplier(level);
            switch (level)
            {
                case EquipmentLevel.Eco:
                    return $"火力 ×{mult:0.00}\\n幾乎不購裝";
                case EquipmentLevel.HalfBuy:
                    return $"火力 ×{mult:0.00}\\n護甲 + 強手槍";
                case EquipmentLevel.ForceBuy:
                    return $"火力 ×{mult:0.00}\\n強起火力";
                case EquipmentLevel.FullBuy:
                    return $"火力 ×{mult:0.00}\\n全起滿配";
                case EquipmentLevel.Hero:
                    return $"火力 ×{mult:0.00}\\n集中資源起英雄槍";
                default:
                    return $"火力 ×{mult:0.00}";
            }
        }

""" + text[end:]

    # RefreshCardPicker affordability (remove awpBlocked)
    old_refresh = """            EquipmentLevel[] levels =
            {
                EquipmentLevel.Eco, EquipmentLevel.HalfBuy, EquipmentLevel.ForceBuy,
                EquipmentLevel.FullBuy, EquipmentLevel.Hero
            };
            for (int i = 0; i < levels.Length && i < _cardButtons.Count; i++)
            {
                EquipmentLevel level = levels[i];
                bool awpBlocked = level == EquipmentLevel.AwpFullBuy && !hasAwper;
                int cost;
                bool affordable;
                if (IsGeneralBattleMode() && _weaponCatalog != null)
                {
                    cost = EconomyCardLoadoutRules.EstimateTeamCardCost(
                        player, level, _weaponCatalog);
                    affordable = EconomyCardLoadoutRules.CanAffordCard(
                        player, level, _weaponCatalog) && !awpBlocked;
                }
                else
                {
                    cost = EconomyRules.EquipmentCost(level);
                    affordable = EconomyRules.CanAfford(money, level) && !awpBlocked;
                }
                _cardButtons[i].interactable = affordable;

                if (i < _cardCostLabels.Count && _cardCostLabels[i] != null)
                {
                    _cardCostLabels[i].text = cost == 0 ? "$0" : $"${cost:N0}";
                    _cardCostLabels[i].color = affordable ? Accent : TextMuted;
                }
                if (i < _cardTitleLabels.Count && _cardTitleLabels[i] != null)
                    _cardTitleLabels[i].color = affordable ? TextPrimary : TextMuted;
                if (i < _cardDetailLabels.Count && _cardDetailLabels[i] != null)
                {
                    string status = awpBlocked ? "需狙擊手" : affordable ? string.Empty : "金錢不足";
                    string body = CardDescription(level);
                    NarrativeUiText.Set(_cardDetailLabels[i], string.IsNullOrEmpty(status) ? body : $"{body}\\n{status}");
                    _cardDetailLabels[i].color = affordable ? TextMuted : new Color(0.55f, 0.42f, 0.42f, 1f);
                }"""

    # After first levels replace, CreateCardPicker still has old or new? We replaced_all both arrays.
    # But Refresh may still have AwpFullBuy if first replace got CreateCardPicker only once each - we used replace_all for the enum pair.

    # Fix Refresh block - may still reference AwpFullBuy
    text = text.replace(
        "bool awpBlocked = level == EquipmentLevel.AwpFullBuy && !hasAwper;",
        "bool heroNeedsPick = level == EquipmentLevel.Hero && IsGeneralBattleMode();",
    )
    text = text.replace(
        "player, level, _weaponCatalog) && !awpBlocked;",
        "player, level, _weaponCatalog);",
    )
    text = text.replace(
        "affordable = EconomyRules.CanAfford(money, level) && !awpBlocked;",
        "affordable = EconomyRules.CanAfford(money, level);",
    )
    text = text.replace(
        'string status = awpBlocked ? "需狙擊手" : affordable ? string.Empty : "金錢不足";',
        'string status = !affordable ? "金錢不足" : heroNeedsPick ? "需指定選手" : string.Empty;',
    )

    # For Hero estimate, set suggested targets before cost so estimate uses them
    old_cost_gen = """                if (IsGeneralBattleMode() && _weaponCatalog != null)
                {
                    cost = EconomyCardLoadoutRules.EstimateTeamCardCost(
                        player, level, _weaponCatalog);
                    affordable = EconomyCardLoadoutRules.CanAffordCard(
                        player, level, _weaponCatalog);
                }"""
    new_cost_gen = """                if (IsGeneralBattleMode() && _weaponCatalog != null)
                {
                    if (level == EquipmentLevel.Hero && player.HeroBuyPlayerIds.Count == 0)
                        BehaviorTreeBuyRules.SetHeroTargets(
                            player, BehaviorTreeBuyRules.SuggestHeroTargets(player));
                    cost = EconomyCardLoadoutRules.EstimateTeamCardCost(
                        player, level, _weaponCatalog);
                    affordable = EconomyCardLoadoutRules.CanAffordCard(
                        player, level, _weaponCatalog);
                }"""
    if old_cost_gen not in text:
        raise RuntimeError("general cost block missing")
    text = text.replace(old_cost_gen, new_cost_gen, 1)

    # OnEconomyCardChosen
    start = text.find("        private void OnEconomyCardChosen(EquipmentLevel level)")
    end = text.find("\n        private void ", start + 10)
    # find next private method after OnEconomyCardChosen
    end = text.find("\n        private ", start + 40)
    # better: find closing of method by next "        private void" that isn't nested
    end = text.find("        private void CreateTactical", start)
    if end < 0:
        end = text.find("        private void ShowTactical", start)
    if end < 0:
        # search any private after
        idx = start + 50
        while True:
            nxt = text.find("\n        private ", idx)
            if nxt < 0:
                raise RuntimeError("cannot find end of OnEconomyCardChosen")
            # skip if inside
            chunk = text[start:nxt]
            if chunk.count("{") <= chunk.count("}"):
                end = nxt + 1
                break
            idx = nxt + 10

    # Simpler: replace known old body
    old_choose = """        private void OnEconomyCardChosen(EquipmentLevel level)
        {
            if (!_awaitingCard || _session == null)
                return;
            TeamRuntime player = _session.TeamById(myTeamId);
            bool hasAwper = player.Players.Any(x => x.Definition.primaryRole == "Awper");
            if (level == EquipmentLevel.AwpFullBuy && !hasAwper)
                return;
            if (IsGeneralBattleMode() && _weaponCatalog != null)
            {
                if (!EconomyCardLoadoutRules.CanAffordCard(player, level, _weaponCatalog))
                    return;
            }
            else if (!EconomyRules.CanAfford(player.Money, level))
                return;
            _selectedCard = level;
        }"""
    new_choose = """        private void OnEconomyCardChosen(EquipmentLevel level)
        {
            if (!_awaitingCard || _session == null)
                return;
            TeamRuntime player = _session.TeamById(myTeamId);
            if (IsGeneralBattleMode() && _weaponCatalog != null)
            {
                if (level == EquipmentLevel.Hero && player.HeroBuyPlayerIds.Count == 0)
                    BehaviorTreeBuyRules.SetHeroTargets(
                        player, BehaviorTreeBuyRules.SuggestHeroTargets(player));
                if (!EconomyCardLoadoutRules.CanAffordCard(player, level, _weaponCatalog))
                    return;
            }
            else if (!EconomyRules.CanAfford(player.Money, level))
                return;

            if (IsGeneralBattleMode() && level == EquipmentLevel.Hero)
            {
                _pendingEconomyCard = level;
                _awaitingHeroPick = true;
                if (_heroPickIds.Count == 0)
                {
                    _heroPickIds.Clear();
                    _heroPickIds.AddRange(BehaviorTreeBuyRules.SuggestHeroTargets(player));
                }
                EnsureHeroPickRow();
                RefreshHeroPickRow();
                if (_cardPickerSubtitle != null)
                    _cardPickerSubtitle.text = "點選 1 至 2 名選手後按確認";
                return;
            }

            _heroPickIds.Clear();
            HideHeroPickRow();
            _pendingEconomyCard = null;
            _awaitingHeroPick = false;
            _selectedCard = level;
        }

        private void EnsureHeroPickRow()
        {
            if (_heroPickRow != null || _cardPickerPanel == null)
            {
                if (_heroPickRow != null)
                    _heroPickRow.SetActive(true);
                return;
            }

            _heroPickRow = new GameObject("HeroPickRow", typeof(RectTransform));
            _heroPickRow.transform.SetParent(_cardPickerPanel.transform, false);
            var rowRt = _heroPickRow.GetComponent<RectTransform>();
            SetRect(rowRt, new Vector2(0.06f, 0.02f), new Vector2(0.94f, 0.16f));

            _heroPickHint = CreateUiObject<Text>("HeroHint", _heroPickRow.transform);
            SetRect(_heroPickHint.rectTransform, new Vector2(0f, 0.55f), new Vector2(0.70f, 1f));
            _heroPickHint.font = _uiFont != null ? _uiFont : Resources.GetBuiltinResource<Font>("Arial.ttf");
            _heroPickHint.fontSize = 16;
            _heroPickHint.alignment = TextAnchor.MiddleLeft;
            _heroPickHint.color = TextMuted;
            _heroPickHint.text = "英雄槍選手";

            TeamRuntime team = _session != null ? _session.TeamById(myTeamId) : null;
            int n = team != null ? team.Players.Count : 5;
            for (int i = 0; i < n; i++)
            {
                Image edge = CreateUiObject<Image>("HeroPick" + i, _heroPickRow.transform);
                float x0 = 0.02f + i * 0.14f;
                SetRect(edge.rectTransform, new Vector2(x0, 0.05f), new Vector2(x0 + 0.13f, 0.50f));
                edge.color = new Color(0.72f, 0.58f, 0.28f, 1f);
                Image face = CreateUiObject<Image>("Face", edge.transform);
                SetRect(face.rectTransform, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.92f));
                face.color = new Color(0.14f, 0.145f, 0.17f, 1f);
                Text label = CreateUiObject<Text>("Label", face.transform);
                Stretch(label.rectTransform);
                label.font = _heroPickHint.font;
                label.fontSize = 14;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = TextPrimary;
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                Button btn = edge.gameObject.AddComponent<Button>();
                btn.targetGraphic = face;
                int idx = i;
                btn.onClick.AddListener(() => ToggleHeroPick(idx));
                _heroPickButtons.Add(btn);
                _heroPickLabels.Add(label);
            }

            Image confEdge = CreateUiObject<Image>("HeroConfirm", _heroPickRow.transform);
            SetRect(confEdge.rectTransform, new Vector2(0.74f, 0.55f), new Vector2(0.98f, 0.95f));
            confEdge.color = new Color(0.72f, 0.58f, 0.28f, 1f);
            Image confFace = CreateUiObject<Image>("Face", confEdge.transform);
            SetRect(confFace.rectTransform, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.92f));
            confFace.color = new Color(0.18f, 0.22f, 0.16f, 1f);
            Text confLabel = CreateUiObject<Text>("Label", confFace.transform);
            Stretch(confLabel.rectTransform);
            confLabel.font = _heroPickHint.font;
            confLabel.fontSize = 16;
            confLabel.alignment = TextAnchor.MiddleCenter;
            confLabel.color = TextPrimary;
            confLabel.text = "確認";
            _heroConfirmButton = confEdge.gameObject.AddComponent<Button>();
            _heroConfirmButton.targetGraphic = confFace;
            _heroConfirmButton.onClick.AddListener(ConfirmHeroPick);
        }

        private void HideHeroPickRow()
        {
            if (_heroPickRow != null)
                _heroPickRow.SetActive(false);
        }

        private void RefreshHeroPickRow()
        {
            if (_session == null)
                return;
            EnsureHeroPickRow();
            TeamRuntime team = _session.TeamById(myTeamId);
            for (int i = 0; i < _heroPickLabels.Count && i < team.Players.Count; i++)
            {
                PlayerRuntime p = team.Players[i];
                string id = p.Definition.id;
                bool on = _heroPickIds.Any(x => string.Equals(x, id, StringComparison.OrdinalIgnoreCase));
                _heroPickLabels[i].text = DemoZh.PlayerName(id);
                _heroPickButtons[i].targetGraphic.color = on
                    ? new Color(0.28f, 0.32f, 0.20f, 1f)
                    : new Color(0.14f, 0.145f, 0.17f, 1f);
            }
            if (_heroConfirmButton != null)
                _heroConfirmButton.interactable = _heroPickIds.Count >= 1 && _heroPickIds.Count <= 2;
        }

        private void ToggleHeroPick(int index)
        {
            if (!_awaitingHeroPick || _session == null)
                return;
            TeamRuntime team = _session.TeamById(myTeamId);
            if (index < 0 || index >= team.Players.Count)
                return;
            string id = team.Players[index].Definition.id;
            int at = _heroPickIds.FindIndex(x => string.Equals(x, id, StringComparison.OrdinalIgnoreCase));
            if (at >= 0)
                _heroPickIds.RemoveAt(at);
            else if (_heroPickIds.Count < 2)
                _heroPickIds.Add(id);
            RefreshHeroPickRow();
        }

        private void ConfirmHeroPick()
        {
            if (!_awaitingHeroPick || _pendingEconomyCard == null || _heroPickIds.Count < 1)
                return;
            TeamRuntime player = _session.TeamById(myTeamId);
            BehaviorTreeBuyRules.SetHeroTargets(player, _heroPickIds);
            if (_weaponCatalog != null &&
                !EconomyCardLoadoutRules.CanAffordCard(player, _pendingEconomyCard.Value, _weaponCatalog))
                return;
            _selectedCard = _pendingEconomyCard;
            _pendingEconomyCard = null;
            _awaitingHeroPick = false;
        }
"""
    if old_choose not in text:
        # try without Awp check if already partially patched
        raise RuntimeError("OnEconomyCardChosen old body not found")
    text = text.replace(old_choose, new_choose, 1)

    # CreateCardPicker titles still use EconomyRules.CardName — fine after Domain rename

    # Remove leftover AwpFullBuy / Light / Standard if any
    for bad in ("EquipmentLevel.AwpFullBuy", "EquipmentLevel.Light", "EquipmentLevel.Standard"):
        if bad in text:
            # CtLight / TLight are fine
            pass
    # Check enum refs only
    import re
    bad_refs = re.findall(r"EquipmentLevel\.(AwpFullBuy|Light|Standard)\b", text)
    if bad_refs:
        raise RuntimeError(f"leftover enum refs: {bad_refs}")

    SRC.write_text(text, encoding="utf-8")
    check = SRC.read_text(encoding="utf-8")
    for need in ("純 Eco", "半起", "強起", "全起", "英雄槍", "ConfirmHeroPick", "IsPistolRound", "heroIds"):
        # CardName comes from EconomyRules at runtime; UI may use CardDescription
        pass
    for need in ("HalfBuy", "ForceBuy", "ConfirmHeroPick", "IsPistolRound", "heroIds"):
        if need not in check:
            raise RuntimeError(f"missing after patch: {need}")
    if "擊殺廣播" not in check and "叫暫停" not in check and "KillFeed" not in check:
        raise RuntimeError("utf8 damaged")
    print("patched QuickMatch economy strategy cards")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
