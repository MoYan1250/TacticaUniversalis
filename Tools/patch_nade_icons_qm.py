# -*- coding: utf-8 -*-
from pathlib import Path

p = Path("Assets/_Project/Scripts/Presentation/QuickMatchDemoController.cs")
src = p.read_text(encoding="utf-8")
orig = src

def req(label, old, new):
    global src
    if old not in src:
        raise SystemExit("MISSING " + label + "\n" + old[:120])
    src = src.replace(old, new, 1)
    print("ok", label)

req(
    "load-nade-icons",
    """            BattleStatPanelPresentation.SetKitAndBombIcons(
                Resources.Load<Sprite>("UI/Icons/Weapons/defuse_kit"),
                Resources.Load<Sprite>("UI/Icons/Weapons/bomb"));
            _killFeedIconSprite = Resources.Load<Sprite>("UI/Ic""",
    """            BattleStatPanelPresentation.SetKitAndBombIcons(
                Resources.Load<Sprite>("UI/Icons/Weapons/defuse_kit"),
                Resources.Load<Sprite>("UI/Icons/Weapons/bomb"));
            BattleStatPanelPresentation.SetNadeIcons(
                Resources.Load<Sprite>("UI/Icons/Weapons/flashbang"),
                Resources.Load<Sprite>("UI/Icons/Weapons/smoke_grenade"),
                Resources.Load<Sprite>("UI/Icons/Weapons/he_grenade"),
                Resources.Load<Sprite>("UI/Icons/Weapons/molotov"),
                Resources.Load<Sprite>("UI/Icons/Weapons/incendiary"));
            _killFeedIconSprite = Resources.Load<Sprite>("UI/Ic""",
)

req(
    "declare-fire-flag",
    "bool hasFlash = false, hasSmoke = false, hasHe = false, hasFire = false;\n                BattleArmorHudState armor = Bat",
    "bool hasFlash = false, hasSmoke = false, hasHe = false, hasFire = false;\n                bool fireIsIncendiary = !teamIsT;\n                BattleArmorHudState armor = Bat",
)

req(
    "read-fire-kind",
    """                        hasFire = runtime.Loadout.HasFire;
                        if (runtime.Loadout.HasArmor && runtime.Loadout.HasHelmet)""",
    """                        hasFire = runtime.Loadout.HasFire;
                        fireIsIncendiary = runtime.Loadout.ResolvedFireKind(
                            teamIsT ? Side.Terrorist : Side.CounterTerrorist) == FireNadeKind.Incendiary;
                        if (runtime.Loadout.HasArmor && runtime.Loadout.HasHelmet)""",
)

req(
    "card-fire-field",
    """                    HasFire = hasFire,
                    HasDefuseKit = hasKit,""",
    """                    HasFire = hasFire,
                    FireIsIncendiary = fireIsIncendiary,
                    HasDefuseKit = hasKit,""",
)

if src == orig:
    raise SystemExit("no changes")
p.write_text(src, encoding="utf-8")
print("DONE", len(src) - len(orig))
