# -*- coding: utf-8 -*-
"""Wire general mode (BehaviorTree): session factory, HP bars, speed 0.5-2x, no skip."""
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str, count: int = 1) -> str:
    if old not in text:
        raise RuntimeError(f"patch anchor missing: {old[:120]!r}")
    return text.replace(old, new, count)


def main() -> int:
    subprocess.check_call([sys.executable, str(ROOT / "Tools" / "backup_quickmatch.py"), "general_mode_bt"])
    text = TARGET.read_text(encoding="utf-8")
    if "IsGeneralBattleMode" in text and "CycleGeneralSpeed" in text:
        print("already patched")
        return 0

    # Fields for HP bars + weapon catalog cache
    text = must_replace(
        text,
        "        private readonly Dictionary<string, Image> _markerDots = new Dictionary<string, Image>();\n"
        "        private readonly Dictionary<string, Text> _markerLabels = new Dictionary<string, Text>();\n",
        "        private readonly Dictionary<string, Image> _markerDots = new Dictionary<string, Image>();\n"
        "        private readonly Dictionary<string, Text> _markerLabels = new Dictionary<string, Text>();\n"
        "        private readonly Dictionary<string, Image> _markerHpFills = new Dictionary<string, Image>();\n"
        "        private readonly Dictionary<string, Image> _markerArmorFills = new Dictionary<string, Image>();\n"
        "        private WeaponCatalog _weaponCatalog;\n"
        "        private BattleSimMode _activeBattleSimMode = BattleSimMode.Quick;\n",
    )

    # GenerateMatch: pick simulator from settings
    text = must_replace(
        text,
        "            _session = new InteractiveQuickMatchSession(\n"
        "                _content, seed, new RuleBasedTacticalPlanner(defaultZone), new FastRoundSimulator(),\n"
        "                myTeamId, _opponentTeamId, activeMapId);\n",
        "            _activeBattleSimMode = GameSettingsRuntime.EffectiveBattleSimMode();\n"
        "            if (_weaponCatalog == null)\n"
        "                _weaponCatalog = RuntimeContentLoader.LoadWeaponCatalog();\n"
        "            IRoundSimulator roundSim = BattleSimFactory.CreateRoundSimulator(\n"
        "                _activeBattleSimMode, _weaponCatalog);\n"
        "            _session = new InteractiveQuickMatchSession(\n"
        "                _content, seed, new RuleBasedTacticalPlanner(defaultZone), roundSim,\n"
        "                myTeamId, _opponentTeamId, activeMapId,\n"
        "                _activeBattleSimMode, _weaponCatalog);\n"
        "            if (IsGeneralBattleMode())\n"
        "            {\n"
        "                playbackSpeed = 1f;\n"
        "                if (_speedText != null)\n"
        "                    _speedText.text = \"1.0×\";\n"
        "            }\n",
    )

    # ApplyMatchModeUi: hide skip in general mode
    text = must_replace(
        text,
        "            ApplyControlBarLayout(simulation);\n"
        "            ApplySettingsFrostedChrome(true);\n"
        "        }\n",
        "            ApplyControlBarLayout(simulation);\n"
        "            ApplySettingsFrostedChrome(true);\n"
        "            if (_skipButtonRect != null)\n"
        "                _skipButtonRect.gameObject.SetActive(!IsGeneralBattleMode());\n"
        "            RefreshMarkerHpBarsVisible();\n"
        "        }\n",
    )

    # Skip early-out
    text = must_replace(
        text,
        "        private void Skip()\n"
        "        {\n"
        "            if (_skippingMatch)\n"
        "                return;\n",
        "        private void Skip()\n"
        "        {\n"
        "            if (IsGeneralBattleMode())\n"
        "                return;\n"
        "            if (_skippingMatch)\n"
        "                return;\n",
    )

    # CycleSpeed: branch for general mode
    text = must_replace(
        text,
        "        private void CycleSpeed()\n"
        "        {\n"
        "            playbackSpeed = playbackSpeed < 4f ? 4f : playbackSpeed < 16f ? 16f : playbackSpeed < 64f ? 64f : 1f;\n"
        "            _speedText.text = $\"{playbackSpeed:0}×\";\n"
        "        }\n",
        "        private void CycleSpeed()\n"
        "        {\n"
        "            if (IsGeneralBattleMode())\n"
        "            {\n"
        "                CycleGeneralSpeed();\n"
        "                return;\n"
        "            }\n"
        "            playbackSpeed = playbackSpeed < 4f ? 4f : playbackSpeed < 16f ? 16f : playbackSpeed < 64f ? 64f : 1f;\n"
        "            _speedText.text = $\"{playbackSpeed:0}×\";\n"
        "        }\n"
        "\n"
        "        private bool IsGeneralBattleMode() =>\n"
        "            _activeBattleSimMode == BattleSimMode.BehaviorTree ||\n"
        "            (_session != null && _session.BattleSimMode == BattleSimMode.BehaviorTree);\n"
        "\n"
        "        private void CycleGeneralSpeed()\n"
        "        {\n"
        "            if (playbackSpeed < 0.75f)\n"
        "                playbackSpeed = 1f;\n"
        "            else if (playbackSpeed < 1.25f)\n"
        "                playbackSpeed = 1.5f;\n"
        "            else if (playbackSpeed < 1.75f)\n"
        "                playbackSpeed = 2f;\n"
        "            else\n"
        "                playbackSpeed = 0.5f;\n"
        "            if (_speedText != null)\n"
        "                _speedText.text = $\"{playbackSpeed:0.0}×\";\n"
        "        }\n",
    )

    # Economy card picker affordability for itemized mode
    text = must_replace(
        text,
            "            for (int i = 0; i < levels.Length && i < _cardButtons.Count; i++)\n"
            "            {\n"
            "                EquipmentLevel level = levels[i];\n"
            "                int cost = EconomyRules.EquipmentCost(level);\n"
            "                bool awpBlocked = level == EquipmentLevel.AwpFullBuy && !hasAwper;\n"
            "                bool affordable = EconomyRules.CanAfford(money, level) && !awpBlocked;\n",
            "            for (int i = 0; i < levels.Length && i < _cardButtons.Count; i++)\n"
            "            {\n"
            "                EquipmentLevel level = levels[i];\n"
            "                bool awpBlocked = level == EquipmentLevel.AwpFullBuy && !hasAwper;\n"
            "                int cost;\n"
            "                bool affordable;\n"
            "                if (IsGeneralBattleMode() && _weaponCatalog != null)\n"
            "                {\n"
            "                    cost = EconomyCardLoadoutRules.EstimateCardCost(\n"
            "                        level, player.Side, _weaponCatalog, hasAwper);\n"
            "                    affordable = EconomyCardLoadoutRules.CanAffordCard(\n"
            "                        player, level, _weaponCatalog) && !awpBlocked;\n"
            "                }\n"
            "                else\n"
            "                {\n"
            "                    cost = EconomyRules.EquipmentCost(level);\n"
            "                    affordable = EconomyRules.CanAfford(money, level) && !awpBlocked;\n"
            "                }\n",
    )

    text = must_replace(
        text,
        "            if (!EconomyRules.CanAfford(player.Money, level))\n"
        "                return;\n"
        "            _selectedCard = level;\n",
        "            if (IsGeneralBattleMode() && _weaponCatalog != null)\n"
        "            {\n"
        "                if (!EconomyCardLoadoutRules.CanAffordCard(player, level, _weaponCatalog))\n"
        "                    return;\n"
        "            }\n"
        "            else if (!EconomyRules.CanAfford(player.Money, level))\n"
        "                return;\n"
        "            _selectedCard = level;\n",
    )

    # CreateMarkers: HP / armor bars
    text = must_replace(
        text,
        "                    Outline outline = label.gameObject.AddComponent<Outline>();\n"
        "                    outline.effectColor = new Color(0f, 0f, 0f, 0.85f);\n"
        "                    outline.effectDistance = new Vector2(1.1f, -1.1f);\n"
        "\n"
        "                    _markers[id] = rootRect;\n"
        "                    _markerDots[id] = marker;\n"
        "                    _markerLabels[id] = label;\n"
        "                    _playerZones[id] = \"t_spawn\";\n",
        "                    Outline outline = label.gameObject.AddComponent<Outline>();\n"
        "                    outline.effectColor = new Color(0f, 0f, 0f, 0.85f);\n"
        "                    outline.effectDistance = new Vector2(1.1f, -1.1f);\n"
        "\n"
        "                    Image hpBg = CreateUiObject<Image>(\"HpBg\", root.transform);\n"
        "                    hpBg.color = new Color(0.05f, 0.05f, 0.05f, 0.85f);\n"
        "                    hpBg.rectTransform.anchorMin = new Vector2(0.1f, -0.28f);\n"
        "                    hpBg.rectTransform.anchorMax = new Vector2(0.9f, -0.08f);\n"
        "                    hpBg.rectTransform.offsetMin = Vector2.zero;\n"
        "                    hpBg.rectTransform.offsetMax = Vector2.zero;\n"
        "                    Image hpFill = CreateUiObject<Image>(\"HpFill\", hpBg.transform);\n"
        "                    hpFill.color = new Color(0.25f, 0.82f, 0.35f, 0.95f);\n"
        "                    Stretch(hpFill.rectTransform);\n"
        "                    hpFill.type = Image.Type.Filled;\n"
        "                    hpFill.fillMethod = Image.FillMethod.Horizontal;\n"
        "                    hpFill.fillAmount = 1f;\n"
        "                    Image armorBg = CreateUiObject<Image>(\"ArmorBg\", root.transform);\n"
        "                    armorBg.color = new Color(0.05f, 0.05f, 0.05f, 0.75f);\n"
        "                    armorBg.rectTransform.anchorMin = new Vector2(0.1f, -0.42f);\n"
        "                    armorBg.rectTransform.anchorMax = new Vector2(0.9f, -0.30f);\n"
        "                    armorBg.rectTransform.offsetMin = Vector2.zero;\n"
        "                    armorBg.rectTransform.offsetMax = Vector2.zero;\n"
        "                    Image armorFill = CreateUiObject<Image>(\"ArmorFill\", armorBg.transform);\n"
        "                    armorFill.color = new Color(0.35f, 0.55f, 0.95f, 0.95f);\n"
        "                    Stretch(armorFill.rectTransform);\n"
        "                    armorFill.type = Image.Type.Filled;\n"
        "                    armorFill.fillMethod = Image.FillMethod.Horizontal;\n"
        "                    armorFill.fillAmount = 0f;\n"
        "\n"
        "                    _markers[id] = rootRect;\n"
        "                    _markerDots[id] = marker;\n"
        "                    _markerLabels[id] = label;\n"
        "                    _markerHpFills[id] = hpFill;\n"
        "                    _markerArmorFills[id] = armorFill;\n"
        "                    _playerZones[id] = \"t_spawn\";\n",
    )

    # ApplyStatusUpdate: support hp:|armor: and update bars
    text = must_replace(
        text,
        "        private void ApplyStatusUpdate(SimulationEvent evt)\n"
        "        {\n"
        "            if (string.IsNullOrEmpty(evt.actorId) || !_stats.TryGetValue(evt.actorId, out PlayerStat stat))\n"
        "                return;\n"
        "            if (string.IsNullOrEmpty(evt.message))\n"
        "                return;\n"
        "            string[] parts = evt.message.Split('|');\n"
        "            if (parts.Length < 3)\n"
        "                return;\n"
        "            if (float.TryParse(parts[0], out float stamina))\n"
        "                stat.Stamina = stamina;\n"
        "            if (float.TryParse(parts[1], out float mentality))\n"
        "                stat.Mentality = mentality;\n"
        "            if (float.TryParse(parts[2], out float form))\n"
        "                stat.Form = form;\n"
        "            RefreshStatPanels();\n"
        "        }\n",
        "        private void ApplyStatusUpdate(SimulationEvent evt)\n"
        "        {\n"
        "            if (string.IsNullOrEmpty(evt.actorId))\n"
        "                return;\n"
        "            if (string.IsNullOrEmpty(evt.message))\n"
        "                return;\n"
        "            if (evt.message.StartsWith(\"hp:\", StringComparison.OrdinalIgnoreCase))\n"
        "            {\n"
        "                int hp = 0;\n"
        "                int armor = 0;\n"
        "                string[] parts = evt.message.Split('|');\n"
        "                foreach (string part in parts)\n"
        "                {\n"
        "                    if (part.StartsWith(\"hp:\", StringComparison.OrdinalIgnoreCase))\n"
        "                        int.TryParse(part.Substring(3), out hp);\n"
        "                    else if (part.StartsWith(\"armor:\", StringComparison.OrdinalIgnoreCase))\n"
        "                        int.TryParse(part.Substring(6), out armor);\n"
        "                }\n"
        "                SetMarkerVitality(evt.actorId, hp, armor);\n"
        "                return;\n"
        "            }\n"
        "            if (!_stats.TryGetValue(evt.actorId, out PlayerStat stat))\n"
        "                return;\n"
        "            string[] legacy = evt.message.Split('|');\n"
        "            if (legacy.Length < 3)\n"
        "                return;\n"
        "            if (float.TryParse(legacy[0], out float stamina))\n"
        "                stat.Stamina = stamina;\n"
        "            if (float.TryParse(legacy[1], out float mentality))\n"
        "                stat.Mentality = mentality;\n"
        "            if (float.TryParse(legacy[2], out float form))\n"
        "                stat.Form = form;\n"
        "            RefreshStatPanels();\n"
        "        }\n"
        "\n"
        "        private void SetMarkerVitality(string playerId, int hp, int armor)\n"
        "        {\n"
        "            if (_markerHpFills.TryGetValue(playerId, out Image hpFill) && hpFill != null)\n"
        "                hpFill.fillAmount = Mathf.Clamp01(hp / 100f);\n"
        "            if (_markerArmorFills.TryGetValue(playerId, out Image armorFill) && armorFill != null)\n"
        "                armorFill.fillAmount = Mathf.Clamp01(armor / 100f);\n"
        "        }\n"
        "\n"
        "        private void RefreshMarkerHpBarsVisible()\n"
        "        {\n"
        "            bool show = IsGeneralBattleMode();\n"
        "            foreach (Image fill in _markerHpFills.Values)\n"
        "            {\n"
        "                if (fill != null && fill.transform.parent != null)\n"
        "                    fill.transform.parent.gameObject.SetActive(show);\n"
        "            }\n"
        "            foreach (Image fill in _markerArmorFills.Values)\n"
        "            {\n"
        "                if (fill != null && fill.transform.parent != null)\n"
        "                    fill.transform.parent.gameObject.SetActive(show);\n"
        "            }\n"
        "        }\n",
    )

    # On Kill also zero HP bar
    text = must_replace(
        text,
        "                case SimulationEventType.Kill:\n"
        "                    if (!string.IsNullOrEmpty(evt.targetId) && _markers.TryGetValue(evt.targetId, out RectTransform victim))\n"
        "                    {\n"
        "                        string deathZone = _playerZones.TryGetValue(evt.targetId, out string z) ? z : evt.zoneId;\n"
        "                        victim.gameObject.SetActive(false);\n"
        "                        RelayoutZone(deathZone, animate, 0.18f);\n"
        "                    }\n",
        "                case SimulationEventType.Kill:\n"
        "                    if (!string.IsNullOrEmpty(evt.targetId))\n"
        "                        SetMarkerVitality(evt.targetId, 0, 0);\n"
        "                    if (!string.IsNullOrEmpty(evt.targetId) && _markers.TryGetValue(evt.targetId, out RectTransform victim))\n"
        "                    {\n"
        "                        string deathZone = _playerZones.TryGetValue(evt.targetId, out string z) ? z : evt.zoneId;\n"
        "                        victim.gameObject.SetActive(false);\n"
        "                        RelayoutZone(deathZone, animate, 0.18f);\n"
        "                    }\n",
    )

    # ResetMarkers: restore HP to full when round starts in general mode
    text = must_replace(
        text,
        "                _playerZones[id] = spawn;\n",
        "                _playerZones[id] = spawn;\n"
        "                if (IsGeneralBattleMode())\n"
        "                {\n"
        "                    PlayerRuntime live = null;\n"
        "                    foreach (PlayerRuntime p in _session.State.TeamA.Players.Concat(_session.State.TeamB.Players))\n"
        "                    {\n"
        "                        if (p.Definition.id == id) { live = p; break; }\n"
        "                    }\n"
        "                    if (live != null)\n"
        "                        SetMarkerVitality(id, live.Alive ? live.Health : 0, live.ArmorHp);\n"
        "                    else\n"
        "                        SetMarkerVitality(id, 100, 0);\n"
        "                }\n",
    )

    if "擊殺廣播" not in text and "叫暫停" not in text:
        raise RuntimeError("UTF-8 corruption suspected after patch")

    TARGET.write_text(text, encoding="utf-8")
    print("patched", TARGET)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
