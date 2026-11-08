using System;
using System.Collections.Generic;
using System.Linq;

namespace TacticaUniversalis.Domain
{
    public enum GroundLootKind
    {
        DefuseKit = 0,
        Primary = 1,
        Pistol = 2,
        Flash = 3,
        Smoke = 4,
        He = 5,
        Fire = 6
    }

    public sealed class GroundLootItem
    {
        public GroundLootKind Kind { get; set; }
        public string ZoneId { get; set; }
        public string WeaponId { get; set; }
        /// <summary>Set when Kind is Fire so pickup keeps molotov vs incendiary silhouette.</summary>
        public FireNadeKind FireKind { get; set; }
    }

    /// <summary>
    /// Drop / pickup for bomb, kit, utility, and weapons. Bomb uses world DroppedBombZoneId.
    /// </summary>
    public static class GroundLootRules
    {
        public static int WeaponPickupScore(string weaponId, string primaryRole)
        {
            if (string.IsNullOrEmpty(weaponId))
                return 0;
            bool awper = string.Equals(primaryRole, "Awper", StringComparison.OrdinalIgnoreCase);
            switch (weaponId.ToLowerInvariant())
            {
                case "ak47":
                    return 1000;
                case "awp":
                    // Awpers strongly prefer AWP; others still may take it below rifles.
                    return awper ? 1050 : 860;
                case "m4a1s":
                case "m4a4":
                    return 900;
                case "ssg08":
                    // Only awpers pick SSG, and it ranks below M4.
                    return awper ? 850 : 0;
                case "galil":
                case "galil_ar":
                case "famas":
                    return 720;
                case "mp7":
                case "ump45":
                case "mac10":
                case "mp9":
                    return 520;
                case "deagle":
                    return 280;
                case "p250":
                    return 220;
                case "dual_berettas":
                case "elite":
                    return 210;
                case "usp_s":
                case "glock":
                    return 180;
                default:
                    return 100;
            }
        }

        public static bool PreferWeapon(string candidateId, string currentId, string primaryRole)
        {
            if (string.IsNullOrEmpty(candidateId))
                return false;
            bool awper = string.Equals(primaryRole, "Awper", StringComparison.OrdinalIgnoreCase);
            // Scout is awper-only — riflers never pick it up.
            if (!awper &&
                string.Equals(candidateId, "ssg08", StringComparison.OrdinalIgnoreCase))
                return false;
            if (string.IsNullOrEmpty(currentId))
                return WeaponPickupScore(candidateId, primaryRole) > 0;
            if (string.Equals(candidateId, currentId, StringComparison.OrdinalIgnoreCase))
                return false;
            return WeaponPickupScore(candidateId, primaryRole) > WeaponPickupScore(currentId, primaryRole);
        }

        public static bool IsPrimaryCategory(WeaponCatalog catalog, string weaponId)
        {
            if (catalog == null || !catalog.TryGet(weaponId, out WeaponDefinition w) || w == null)
                return !string.IsNullOrEmpty(weaponId);
            string cat = w.category ?? string.Empty;
            return cat.IndexOf("rifle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   cat.IndexOf("sniper", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   cat.IndexOf("smg", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static void DropFromVictim(
            BehaviorTreeRoundWorld world,
            PlayerRuntime victim,
            TeamRuntime victimTeam,
            float time)
        {
            if (world == null || victim == null || string.IsNullOrEmpty(victim.ZoneId))
                return;

            string zone = victim.ZoneId;
            string teamId = victimTeam?.Definition?.id;

            if (!world.BombPlanted &&
                !string.IsNullOrEmpty(world.BombCarrierId) &&
                string.Equals(world.BombCarrierId, victim.Definition.id, StringComparison.OrdinalIgnoreCase))
            {
                world.BombCarrierId = null;
                world.DroppedBombZoneId = zone;
                world.Add(time + 0.11f, EventType.StatusUpdated, victim.Definition.id, null, zone,
                    $"bomb_drop:{zone}", teamId, null, "bomb_dropped");
            }

            PlayerLoadout loadout = victim.Loadout;
            if (loadout == null)
                return;

            if (loadout.HasDefuseKit)
            {
                loadout.HasDefuseKit = false;
                world.GroundLoot.Add(new GroundLootItem
                {
                    Kind = GroundLootKind.DefuseKit,
                    ZoneId = zone
                });
                world.Add(time + 0.112f, EventType.StatusUpdated, victim.Definition.id, null, zone,
                    "kit_drop", teamId, null, "kit_dropped");
            }

            DropUtility(world, victim, zone, teamId, time, loadout);

            if (!string.IsNullOrEmpty(loadout.PrimaryWeaponId))
            {
                string wid = loadout.PrimaryWeaponId;
                loadout.PrimaryWeaponId = null;
                world.GroundLoot.Add(new GroundLootItem
                {
                    Kind = GroundLootKind.Primary,
                    ZoneId = zone,
                    WeaponId = wid
                });
                world.Add(time + 0.114f, EventType.StatusUpdated, victim.Definition.id, null, zone,
                    $"gun_drop:{wid}", teamId, wid, "gun_dropped");
            }

            if (!string.IsNullOrEmpty(loadout.PistolWeaponId))
            {
                string wid = loadout.PistolWeaponId;
                // Keep a default pistol so they are never unarmed if revived mid-round — but they are dead.
                loadout.PistolWeaponId = null;
                world.GroundLoot.Add(new GroundLootItem
                {
                    Kind = GroundLootKind.Pistol,
                    ZoneId = zone,
                    WeaponId = wid
                });
            }
        }

        private static void DropUtility(
            BehaviorTreeRoundWorld world, PlayerRuntime victim, string zone, string teamId,
            float time, PlayerLoadout loadout)
        {
            void DropOne(GroundLootKind kind, string tag, FireNadeKind fireKind = FireNadeKind.None)
            {
                world.GroundLoot.Add(new GroundLootItem
                {
                    Kind = kind,
                    ZoneId = zone,
                    FireKind = kind == GroundLootKind.Fire ? fireKind : FireNadeKind.None
                });
                world.Add(time + 0.113f, EventType.StatusUpdated, victim.Definition.id, null, zone,
                    tag, teamId, null, tag);
            }

            int flashes = loadout.FlashCount;
            loadout.FlashCount = 0;
            for (int i = 0; i < flashes; i++)
                DropOne(GroundLootKind.Flash, "util_drop_flash");

            if (loadout.HasSmoke)
            {
                loadout.HasSmoke = false;
                DropOne(GroundLootKind.Smoke, "util_drop_smoke");
            }

            if (loadout.HasHe)
            {
                loadout.HasHe = false;
                DropOne(GroundLootKind.He, "util_drop_he");
            }

            if (loadout.HasFire)
            {
                FireNadeKind fireKind = loadout.FireKind != FireNadeKind.None
                    ? loadout.FireKind
                    : PlayerLoadout.DefaultFireKind(ResolveVictimSide(world, victim));
                loadout.HasFire = false;
                loadout.FireKind = FireNadeKind.None;
                DropOne(GroundLootKind.Fire, "util_drop_fire", fireKind);
            }
        }

        public static void TryPickupInZone(
            BehaviorTreeRoundWorld world,
            PlayerRuntime player,
            TeamRuntime team,
            WeaponCatalog catalog,
            float time)
        {
            if (world == null || player == null || !player.Alive || string.IsNullOrEmpty(player.ZoneId))
                return;
            if (player.Loadout == null)
                player.Loadout = ItemizedEconomyRules.DefaultPistolLoadout(team.Side);

            string zone = player.ZoneId;
            string teamId = team.Definition.id;
            string role = player.Definition.primaryRole;

            // Bomb first (T only).
            if (team.Side == Side.Terrorist &&
                !world.BombPlanted &&
                !string.IsNullOrEmpty(world.DroppedBombZoneId) &&
                string.Equals(world.DroppedBombZoneId, zone, StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrEmpty(world.BombCarrierId))
            {
                world.BombCarrierId = player.Definition.id;
                world.DroppedBombZoneId = null;
                world.Add(time + 0.02f, EventType.StatusUpdated, player.Definition.id, null, zone,
                    "bomb:1", teamId, null, "bomb_pickup");
            }

            // Process ground loot at this zone: util fill order then guns.
            var here = world.GroundLoot
                .Where(l => l != null && string.Equals(l.ZoneId, zone, StringComparison.OrdinalIgnoreCase))
                .OrderBy(l => UtilityPickupPriority(l.Kind))
                .ThenBy(l => (int)l.Kind)
                .ToList();

            foreach (GroundLootItem item in here)
            {
                if (IsUtilityLoot(item.Kind) && ShouldSkipUtilityPickup(player, role))
                    continue;
                if (!TryTakeItem(world, player, team, catalog, item, role, time, teamId))
                    continue;
                world.GroundLoot.Remove(item);
            }
        }

        private static bool IsUtilityLoot(GroundLootKind kind) =>
            kind == GroundLootKind.Flash ||
            kind == GroundLootKind.Smoke ||
            kind == GroundLootKind.He ||
            kind == GroundLootKind.Fire;

        /// <summary>
        /// Entry with any util already carried skips util loot so thrower can restock.
        /// Support / high-utility always pick.
        /// </summary>
        public static bool ShouldSkipUtilityPickup(PlayerRuntime player, string role)
        {
            if (player?.Loadout == null)
                return false;
            if (string.Equals(role, "Support", StringComparison.OrdinalIgnoreCase))
                return false;
            if (player.Definition != null && player.Definition.attributes.utility >= 85)
                return false;
            if (!string.Equals(role, "Entry", StringComparison.OrdinalIgnoreCase))
                return false;
            PlayerLoadout l = player.Loadout;
            return l.FlashCount > 0 || l.HasSmoke || l.HasHe || l.HasFire;
        }

        /// <summary>Smoke → flash → HE → fire → kit → guns. Lower = earlier.</summary>
        public static int UtilityPickupPriority(GroundLootKind kind)
        {
            switch (kind)
            {
                case GroundLootKind.Smoke: return 0;
                case GroundLootKind.Flash: return 1;
                case GroundLootKind.He: return 2;
                case GroundLootKind.Fire: return 3;
                case GroundLootKind.DefuseKit: return 4;
                case GroundLootKind.Primary: return 10;
                case GroundLootKind.Pistol: return 11;
                default: return 20;
            }
        }

        private static bool TryTakeItem(
            BehaviorTreeRoundWorld world,
            PlayerRuntime player,
            TeamRuntime team,
            WeaponCatalog catalog,
            GroundLootItem item,
            string role,
            float time,
            string teamId)
        {
            PlayerLoadout loadout = player.Loadout;
            switch (item.Kind)
            {
                case GroundLootKind.DefuseKit:
                    if (team.Side != Side.CounterTerrorist || loadout.HasDefuseKit)
                        return false;
                    loadout.HasDefuseKit = true;
                    world.Add(time + 0.03f, EventType.StatusUpdated, player.Definition.id, null, player.ZoneId,
                        "kit:1", teamId, null, "kit_pickup");
                    return true;

                case GroundLootKind.Flash:
                    if (!loadout.CanTakeFlash)
                        return false;
                    loadout.FlashCount++;
                    return true;
                case GroundLootKind.Smoke:
                    if (loadout.HasSmoke) return false;
                    loadout.HasSmoke = true;
                    return true;
                case GroundLootKind.He:
                    if (loadout.HasHe) return false;
                    loadout.HasHe = true;
                    return true;
                case GroundLootKind.Fire:
                    if (loadout.HasFire) return false;
                    loadout.HasFire = true;
                    loadout.FireKind = item.FireKind != FireNadeKind.None
                        ? item.FireKind
                        : PlayerLoadout.DefaultFireKind(team.Side);
                    return true;

                case GroundLootKind.Primary:
                    if (string.IsNullOrEmpty(item.WeaponId))
                        return false;
                    if (!PreferWeapon(item.WeaponId, loadout.PrimaryWeaponId, role))
                        return false;
                    // Drop current primary back to ground if upgrading.
                    if (!string.IsNullOrEmpty(loadout.PrimaryWeaponId))
                    {
                        world.GroundLoot.Add(new GroundLootItem
                        {
                            Kind = GroundLootKind.Primary,
                            ZoneId = player.ZoneId,
                            WeaponId = loadout.PrimaryWeaponId
                        });
                    }
                    loadout.PrimaryWeaponId = item.WeaponId;
                    WeaponAmmoRules.FillSlot(player, catalog, primary: true);
                    world.Add(time + 0.04f, EventType.StatusUpdated, player.Definition.id, null, player.ZoneId,
                        $"gun:{item.WeaponId}", teamId, item.WeaponId, "gun_pickup");
                    return true;

                case GroundLootKind.Pistol:
                    if (string.IsNullOrEmpty(item.WeaponId))
                        return false;
                    if (BehaviorTreeBuyRules.IsDualBerettas(item.WeaponId) &&
                        !CanTakeDualBerettas(world, team))
                        return false;
                    if (!PreferWeapon(item.WeaponId, loadout.PistolWeaponId, role))
                        return false;
                    if (!string.IsNullOrEmpty(loadout.PistolWeaponId))
                    {
                        world.GroundLoot.Add(new GroundLootItem
                        {
                            Kind = GroundLootKind.Pistol,
                            ZoneId = player.ZoneId,
                            WeaponId = loadout.PistolWeaponId
                        });
                    }
                    loadout.PistolWeaponId = item.WeaponId;
                    WeaponAmmoRules.FillSlot(player, catalog, primary: false);
                    return true;
            }

            return false;
        }

        private static Side ResolveVictimSide(BehaviorTreeRoundWorld world, PlayerRuntime victim)
        {
            if (world?.Request == null || victim?.Definition == null)
                return Side.Terrorist;
            string id = victim.Definition.id;
            if (world.Request.attackers?.Players != null &&
                world.Request.attackers.Players.Any(p =>
                    p?.Definition != null &&
                    string.Equals(p.Definition.id, id, StringComparison.OrdinalIgnoreCase)))
                return world.Request.attackers.Side;
            if (world.Request.defenders?.Players != null &&
                world.Request.defenders.Players.Any(p =>
                    p?.Definition != null &&
                    string.Equals(p.Definition.id, id, StringComparison.OrdinalIgnoreCase)))
                return world.Request.defenders.Side;
            return Side.Terrorist;
        }

        private static bool CanTakeDualBerettas(BehaviorTreeRoundWorld world, TeamRuntime team)
        {
            if (world?.Request == null || team == null)
                return false;
            if (!MatchRules.IsPistolRound(Math.Max(0, world.Request.roundNumber - 1)))
                return false;
            return team.Players.All(p =>
                p == null || !p.Alive || !BehaviorTreeBuyRules.IsDualBerettas(p.Loadout?.PistolWeaponId));
        }

        /// <summary>T should try to recover a dropped bomb unless desperately saving alone.</summary>
        public static bool ShouldRetrieveDroppedBomb(
            Side side, bool bombPlanted, string droppedZone, int aliveSelf, int aliveEnemy)
        {
            if (side != Side.Terrorist || bombPlanted || string.IsNullOrEmpty(droppedZone))
                return false;
            // 1v3+ : prefer save/hide over suicide retrieve.
            if (aliveSelf == 1 && aliveEnemy >= 3)
                return false;
            return true;
        }

        public static bool ShouldGuardDroppedBomb(
            Side side, bool bombPlanted, string droppedZone)
        {
            return side == Side.CounterTerrorist &&
                   !bombPlanted &&
                   !string.IsNullOrEmpty(droppedZone);
        }
    }
}
