# -*- coding: utf-8 -*-
"""Wire bomb carrier / dropped bomb HUD + general gear bind in QuickMatch."""
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"anchor missing: {old[:200]!r}")
    return text.replace(old, new, 1)


def main() -> int:
    subprocess.check_call(
        [sys.executable, str(ROOT / "Tools" / "backup_quickmatch.py"), "bomb_loot_ui"]
    )
    text = TARGET.read_text(encoding="utf-8")
    if "_bombCarrierId" in text and "HasBomb =" in text:
        print("already patched")
        return 0

    text = must_replace(
        text,
        "        private string _bombZoneId;\n",
        "        private string _bombZoneId;\n"
        "        private string _bombCarrierId;\n"
        "        private string _droppedBombZoneId;\n",
    )

    text = must_replace(
        text,
        """                case SimulationEventType.RoundStarted:
                    ApplyMapOrientation(evt.roundNumber, animate);
                    ResetMarkers(evt.roundNumber, false);
                    HideBomb();
""",
        """                case SimulationEventType.RoundStarted:
                    ApplyMapOrientation(evt.roundNumber, animate);
                    ResetMarkers(evt.roundNumber, false);
                    _bombCarrierId = null;
                    _droppedBombZoneId = null;
                    HideBomb();
""",
    )

    text = must_replace(
        text,
        """        private void ApplyStatusUpdate(SimulationEvent evt)
        {
            if (string.IsNullOrEmpty(evt.actorId))
                return;
            if (string.IsNullOrEmpty(evt.message))
                return;
            if (evt.message.StartsWith("clock:", StringComparison.OrdinalIgnoreCase))
                return;
            if (evt.message.StartsWith("hp:", StringComparison.OrdinalIgnoreCase))
""",
        """        private void ApplyStatusUpdate(SimulationEvent evt)
        {
            if (!string.IsNullOrEmpty(evt.message) &&
                evt.message.StartsWith("clock:", StringComparison.OrdinalIgnoreCase))
                return;

            if (ApplyBombLootStatus(evt))
                return;

            if (string.IsNullOrEmpty(evt.actorId))
                return;
            if (string.IsNullOrEmpty(evt.message))
                return;
            if (evt.message.StartsWith("hp:", StringComparison.OrdinalIgnoreCase))
""",
    )

    # Insert ApplyBombLootStatus before ApplyStatusUpdate
    text = must_replace(
        text,
        "        private void ApplyStatusUpdate(SimulationEvent evt)\n",
        '''        private bool ApplyBombLootStatus(SimulationEvent evt)
        {
            if (evt == null || string.IsNullOrEmpty(evt.message))
                return false;

            string msg = evt.message;
            if (msg.StartsWith("bomb_drop:", StringComparison.OrdinalIgnoreCase) ||
                (evt.reasonTags != null && Array.Exists(evt.reasonTags, t =>
                    string.Equals(t, "bomb_dropped", StringComparison.OrdinalIgnoreCase))))
            {
                string zone = msg.StartsWith("bomb_drop:", StringComparison.OrdinalIgnoreCase)
                    ? msg.Substring("bomb_drop:".Length)
                    : evt.zoneId;
                _bombCarrierId = null;
                _droppedBombZoneId = zone;
                if (!string.IsNullOrEmpty(zone))
                    ShowBomb(zone);
                RefreshStatPanels();
                return true;
            }

            if (string.Equals(msg, "bomb:1", StringComparison.OrdinalIgnoreCase) ||
                (evt.reasonTags != null && (
                    Array.Exists(evt.reasonTags, t => string.Equals(t, "bomb_carrier", StringComparison.OrdinalIgnoreCase)) ||
                    Array.Exists(evt.reasonTags, t => string.Equals(t, "bomb_pickup", StringComparison.OrdinalIgnoreCase)))))
            {
                _bombCarrierId = evt.actorId;
                _droppedBombZoneId = null;
                if (!IsBombPlantedVisible())
                    HideBomb();
                RefreshStatPanels();
                return true;
            }

            if (msg.StartsWith("kit:", StringComparison.OrdinalIgnoreCase) ||
                msg.StartsWith("gun:", StringComparison.OrdinalIgnoreCase) ||
                msg.StartsWith("gun_drop:", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(msg, "kit_drop", StringComparison.OrdinalIgnoreCase))
            {
                RefreshStatPanels();
                return true;
            }

            return false;
        }

        private bool IsBombPlantedVisible() =>
            !string.IsNullOrEmpty(_bombZoneId) &&
            string.IsNullOrEmpty(_droppedBombZoneId);

        private void ApplyStatusUpdate(SimulationEvent evt)
''',
    )

    # BombPlanted should clear dropped state
    text = must_replace(
        text,
        """                case SimulationEventType.BombPlanted:
                    MoveMarkerAlongPath(evt.actorId, evt.zoneId, animate);
                    ShowBomb(evt.zoneId);
                    break;
""",
        """                case SimulationEventType.BombPlanted:
                    _droppedBombZoneId = null;
                    _bombCarrierId = evt.actorId;
                    MoveMarkerAlongPath(evt.actorId, evt.zoneId, animate);
                    ShowBomb(evt.zoneId);
                    RefreshStatPanels();
                    break;
""",
    )

    text = must_replace(
        text,
        """                bool hasKit = false;
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
""",
        """                bool hasKit = false;
                bool hasBomb = false;
                bool hasFlash = false, hasSmoke = false, hasHe = false, hasFire = false;
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
                        hasKit = !teamIsT && runtime.Loadout.HasDefuseKit;
                        hasFlash = runtime.Loadout.HasFlash;
                        hasSmoke = runtime.Loadout.HasSmoke;
                        hasHe = runtime.Loadout.HasHe;
                        hasFire = runtime.Loadout.HasFire;
                        if (runtime.Loadout.HasArmor && runtime.Loadout.HasHelmet)
                            armor = BattleArmorHudState.Full;
                        else if (runtime.Loadout.HasArmor)
                            armor = BattleArmorHudState.Half;
                    }
                    if (!stat.Alive)
                        health = 0;
                }
                hasBomb = teamIsT && stat.Alive &&
                    !string.IsNullOrEmpty(_bombCarrierId) &&
                    string.Equals(_bombCarrierId, id, StringComparison.OrdinalIgnoreCase);
""",
    )

    text = must_replace(
        text,
        """                    SecondaryWeaponName = secondaryName,
                    HasDefuseKit = hasKit,
                    Armor = armor,
""",
        """                    SecondaryWeaponName = secondaryName,
                    HasFlash = hasFlash,
                    HasSmoke = hasSmoke,
                    HasHe = hasHe,
                    HasFire = hasFire,
                    HasDefuseKit = hasKit,
                    HasBomb = hasBomb,
                    Armor = armor,
""",
    )

    if "叫暫停" not in text:
        raise RuntimeError("UTF-8 corruption")
    TARGET.write_text(text, encoding="utf-8")
    print("patched", TARGET)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
