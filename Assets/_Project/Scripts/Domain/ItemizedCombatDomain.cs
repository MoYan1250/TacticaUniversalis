using System;
using System.Collections.Generic;
using System.Linq;

namespace TacticaUniversalis.Domain
{
    [Serializable]
    public sealed class WeaponDefinition
    {
        public string id;
        public string displayName;
        public string category;
        public int price;
        public int killReward;
        public int damageBody;
        public int damageBodyArmored;
        public int damageHead;
        public int damageHeadArmored;
        public float armorPenetration = 0.5f;
        public float fireInterval = 0.1f;
        /// <summary>UV distance penalty slope. Higher drops hit chance faster with range.</summary>
        public float rangeFalloff;
        /// <summary>UV distance of peak accuracy.</summary>
        public float optimalRange;
        /// <summary>Unused by hit formula. Kept for JSON; close shots use hip-fire stance instead.</summary>
        public float closeFalloff;
        /// <summary>UV distance beyond which the weapon cannot engage.</summary>
        public float maxEngageRange;
        /// <summary>True for weapons that normally scope; too close they hip-fire instead.</summary>
        public bool requiresScope;
        /// <summary>UV distance below which a scoped weapon switches to hip-fire.</summary>
        public float hipFireRange;
        /// <summary>Hip-fire accuracy multiplier versus scoped peak, typically slightly below 1.</summary>
        public float hipFireAccuracy;
        public int magazineSize;
        public int reserveAmmo;
        public float reloadTime;
        /// <summary>CS:GO flinch_velocity_modifier_large. Lower tags harder on first hit.</summary>
        public float flinchLarge;
        /// <summary>CS:GO flinch_velocity_modifier_small. Stacks extra slowdown on follow-up hits.</summary>
        public float flinchSmall;
        /// <summary>CS:GO max_player_speed while holding this weapon.</summary>
        public float moveSpeed;
    }

    [Serializable]
    public sealed class Weapons2021JsonRoot
    {
        public int schemaVersion = 1;
        public int armorVestPrice = 650;
        public int armorHelmetPrice = 1000;
        public int helmetUpgradePrice = 350;
        public int defuseKitPrice = 400;
        public WeaponDefinition[] weapons;
    }

    /// <summary>Incendiary (CT) vs molotov (T). Preserved on drop/pickup for HUD silhouette.</summary>
    public enum FireNadeKind
    {
        None = 0,
        Molotov = 1,
        Incendiary = 2
    }

    public sealed class PlayerLoadout
    {
        public const int MaxFlashCount = 2;

        public string PrimaryWeaponId { get; set; }
        public string PistolWeaponId { get; set; }
        public bool HasArmor { get; set; }
        public bool HasHelmet { get; set; }
        public bool HasDefuseKit { get; set; }
        private int _flashCount;
        /// <summary>0–2 flashbangs carried.</summary>
        public int FlashCount
        {
            get => _flashCount;
            set => _flashCount = value < 0 ? 0 : (value > MaxFlashCount ? MaxFlashCount : value);
        }
        public bool HasFlash
        {
            get => FlashCount > 0;
            set
            {
                if (value)
                {
                    if (FlashCount < 1)
                        FlashCount = 1;
                }
                else
                    FlashCount = 0;
            }
        }
        public bool HasSmoke { get; set; }
        public bool HasHe { get; set; }
        public bool HasFire { get; set; }
        /// <summary>Which fire nade silhouette to show; tint still follows holder side.</summary>
        public FireNadeKind FireKind { get; set; }

        public string ActiveWeaponId =>
            !string.IsNullOrEmpty(PrimaryWeaponId) ? PrimaryWeaponId : PistolWeaponId;

        public bool CanTakeFlash => FlashCount < MaxFlashCount;

        public bool TryAddFlash()
        {
            if (!CanTakeFlash)
                return false;
            FlashCount++;
            return true;
        }

        public PlayerLoadout Clone() =>
            new PlayerLoadout
            {
                PrimaryWeaponId = PrimaryWeaponId,
                PistolWeaponId = PistolWeaponId,
                HasArmor = HasArmor,
                HasHelmet = HasHelmet,
                HasDefuseKit = HasDefuseKit,
                FlashCount = FlashCount,
                HasSmoke = HasSmoke,
                HasHe = HasHe,
                HasFire = HasFire,
                FireKind = FireKind
            };

        public static FireNadeKind DefaultFireKind(Side side) =>
            side == Side.Terrorist ? FireNadeKind.Molotov : FireNadeKind.Incendiary;

        public FireNadeKind ResolvedFireKind(Side holderSide) =>
            FireKind != FireNadeKind.None ? FireKind : DefaultFireKind(holderSide);
    }

    public sealed class WeaponCatalog
    {
        public int ArmorVestPrice { get; }
        public int ArmorHelmetPrice { get; }
        public int HelmetUpgradePrice { get; }
        public int DefuseKitPrice { get; }
        public IReadOnlyDictionary<string, WeaponDefinition> Weapons { get; }

        public WeaponCatalog(
            IEnumerable<WeaponDefinition> weapons,
            int armorVestPrice = 650,
            int armorHelmetPrice = 1000,
            int helmetUpgradePrice = 350,
            int defuseKitPrice = 400)
        {
            Weapons = (weapons ?? Enumerable.Empty<WeaponDefinition>())
                .Where(w => w != null && !string.IsNullOrWhiteSpace(w.id))
                .ToDictionary(w => w.id, StringComparer.OrdinalIgnoreCase);
            foreach (WeaponDefinition weapon in Weapons.Values)
            {
                CombatSightRules.EnsureRangeStats(weapon);
                WeaponAmmoRules.EnsureAmmoStats(weapon);
                CombatTaggingRules.EnsureTaggingStats(weapon);
            }
            ArmorVestPrice = armorVestPrice;
            ArmorHelmetPrice = armorHelmetPrice;
            HelmetUpgradePrice = helmetUpgradePrice;
            DefuseKitPrice = defuseKitPrice;
        }

        public static WeaponCatalog FromRoot(Weapons2021JsonRoot root)
        {
            if (root == null || root.weapons == null || root.weapons.Length == 0)
                return CreateDefault();
            return new WeaponCatalog(
                root.weapons,
                root.armorVestPrice > 0 ? root.armorVestPrice : 650,
                root.armorHelmetPrice > 0 ? root.armorHelmetPrice : 1000,
                root.helmetUpgradePrice > 0 ? root.helmetUpgradePrice : 350,
                root.defuseKitPrice > 0 ? root.defuseKitPrice : 400);
        }

        public static WeaponCatalog CreateDefault() => FromRoot(DefaultRoot());

        public static Weapons2021JsonRoot DefaultRoot() =>
            new Weapons2021JsonRoot
            {
                schemaVersion = 1,
                armorVestPrice = 650,
                armorHelmetPrice = 1000,
                helmetUpgradePrice = 350,
                defuseKitPrice = 400,
                weapons = new[]
                {
                    W("glock", "Glock-18", "pistol", 200, 300, 28, 21, 119, 56, 0.47f, 0.15f),
                    W("usp_s", "USP-S", "pistol", 200, 300, 35, 26, 140, 70, 0.505f, 0.17f),
                    W("p250", "P250", "pistol", 300, 300, 38, 28, 151, 96, 0.64f, 0.15f),
                    W("Dual_Berettas", "Dual Berettas", "pistol", 300, 300, 38, 22, 152, 87, 0.575f, 0.12f),
                    W("deagle", "Desert Eagle", "pistol", 700, 300, 53, 47, 235, 178, 0.932f, 0.225f),
                    W("mac10", "MAC-10", "smg", 1050, 600, 29, 16, 114, 49, 0.575f, 0.075f),
                    W("mp9", "MP9", "smg", 1250, 600, 26, 15, 104, 46, 0.60f, 0.07f),
                    W("ump45", "UMP-45", "smg", 1200, 600, 35, 22, 140, 69, 0.65f, 0.09f),
                    W("mp7", "MP7", "smg", 1500, 600, 29, 18, 116, 57, 0.625f, 0.08f),
                    W("Galil_AR", "Galil AR", "rifle", 1800, 300, 30, 23, 120, 92, 0.775f, 0.09f),
                    W("famas", "FAMAS", "rifle", 2050, 300, 30, 20, 120, 84, 0.70f, 0.09f),
                    W("ak47", "AK-47", "rifle", 2700, 300, 36, 27, 143, 111, 0.775f, 0.1f),
                    W("m4a4", "M4A4", "rifle", 3100, 300, 33, 23, 131, 92, 0.70f, 0.09f),
                    W("m4a1s", "M4A1-S", "rifle", 2900, 300, 38, 26, 132, 99, 0.70f, 0.1f),
                    W("awp", "AWP", "sniper", 4750, 100, 115, 110, 459, 448, 0.975f, 1.455f),
                    W("ssg08", "SSG 08", "sniper", 1700, 300, 88, 72, 352, 211, 0.85f, 1.25f),
                    W("knife", "Knife", "knife", 0, 1500, 40, 34, 40, 34, 0.85f, 0.4f)
                }
            };

        private static WeaponDefinition W(
            string id, string name, string category, int price, int killReward,
            int body, int bodyArmored, int head, int headArmored,
            float pen, float interval) =>
            new WeaponDefinition
            {
                id = id,
                displayName = name,
                category = category,
                price = price,
                killReward = killReward,
                damageBody = body,
                damageBodyArmored = bodyArmored,
                damageHead = head,
                damageHeadArmored = headArmored,
                armorPenetration = pen,
                fireInterval = interval
            };

        public bool TryGet(string weaponId, out WeaponDefinition weapon)
        {
            if (Weapons.TryGetValue(weaponId ?? string.Empty, out weapon))
                return true;
            if (string.Equals(weaponId, "galil", StringComparison.OrdinalIgnoreCase))
                return Weapons.TryGetValue("Galil_AR", out weapon);
            if (string.Equals(weaponId, "elite", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(weaponId, "dual_beretta", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(weaponId, "dualberettas", StringComparison.OrdinalIgnoreCase))
                return Weapons.TryGetValue("Dual_Berettas", out weapon);
            weapon = null;
            return false;
        }

        public WeaponDefinition GetOrKnife(string weaponId) =>
            TryGet(weaponId, out WeaponDefinition w) ? w : Weapons["knife"];
    }

    public readonly struct HitResult
    {
        public readonly int DamageToHealth;
        public readonly int DamageToArmor;
        public readonly bool Headshot;
        public readonly bool Killed;

        public HitResult(int damageToHealth, int damageToArmor, bool headshot, bool killed)
        {
            DamageToHealth = damageToHealth;
            DamageToArmor = damageToArmor;
            Headshot = headshot;
            Killed = killed;
        }
    }

    public static class HitResolver
    {
        public static float HeadshotChance(PlayerRuntime attacker)
        {
            float aim = attacker.Definition.attributes.aim + attacker.FocusAim;
            float chance = 0.12f + aim / 500f;
            if (attacker.Definition.traitIds != null &&
                attacker.Definition.traitIds.Any(t =>
                    string.Equals(t, "headshot_machine", StringComparison.OrdinalIgnoreCase)))
                chance += 0.08f;
            return Math.Min(0.55f, chance * attacker.FocusAllMult);
        }

        public static HitResult Apply(
            PlayerRuntime attacker,
            PlayerRuntime victim,
            WeaponCatalog catalog,
            IRandomSource random,
            string weaponId = null)
        {
            string id = weaponId ?? attacker.Loadout?.ActiveWeaponId;
            WeaponDefinition weapon = catalog.GetOrKnife(id);
            bool headshot = random.Next01() < HeadshotChance(attacker);
            bool armored = victim.ArmorHp > 0;
            bool helmetBlocks = armored && victim.Loadout != null && victim.Loadout.HasHelmet;

            int damage;
            if (headshot)
            {
                if (armored && helmetBlocks)
                    damage = weapon.damageHeadArmored;
                else if (armored && !helmetBlocks)
                    damage = weapon.damageHead;
                else
                    damage = weapon.damageHead;
            }
            else
            {
                damage = armored ? weapon.damageBodyArmored : weapon.damageBody;
            }

            int toHealth;
            int toArmor = 0;
            if (!armored)
            {
                toHealth = damage;
            }
            else
            {
                // CS-like split: armored body/head tables already bake pen; armor loses ~half of mitigated.
                int unarmored = headshot ? weapon.damageHead : weapon.damageBody;
                toHealth = damage;
                toArmor = Math.Max(0, (unarmored - damage + 1) / 2);
                if (toArmor > victim.ArmorHp)
                    toArmor = victim.ArmorHp;
            }

            victim.ArmorHp = Math.Max(0, victim.ArmorHp - toArmor);
            if (victim.ArmorHp <= 0 && victim.Loadout != null)
            {
                victim.Loadout.HasArmor = false;
                victim.Loadout.HasHelmet = false;
            }

            victim.Health = Math.Max(0, victim.Health - toHealth);
            bool killed = victim.Health <= 0;
            if (killed)
                victim.Alive = false;
            return new HitResult(toHealth, toArmor, headshot, killed);
        }
    }

    /// <summary>CS:GO 2021-style per-player wallets for general / behavior-tree mode.</summary>
    public static class ItemizedEconomyRules
    {
        public const int StartingPlayerMoney = 800;
        public const int MaximumPlayerMoney = 16000;
        public const int OvertimePlayerMoney = 8000;
        public const int WinReward = 3250;
        public const int BombWinReward = 3500;
        public const int LossBase = 1400;
        public const int LossStreakBonus = 500;
        public const int LossCap = 3400;

        public static void InitializeMatch(TeamRuntime team)
        {
            foreach (PlayerRuntime player in team.Players)
            {
                player.Money = StartingPlayerMoney;
                player.Health = 100;
                player.ArmorHp = 0;
                player.Loadout = DefaultPistolLoadout(team.Side);
                player.KeptWeaponFromPriorRound = false;
            }
            SyncTeamMoney(team);
        }

        public static void InitializeMatch(MatchState state)
        {
            InitializeMatch(state.TeamA);
            InitializeMatch(state.TeamB);
        }

        public static PlayerLoadout DefaultPistolLoadout(Side side) =>
            new PlayerLoadout
            {
                PistolWeaponId = side == Side.Terrorist ? "glock" : "usp_s",
                PrimaryWeaponId = null,
                HasArmor = false,
                HasHelmet = false,
                HasDefuseKit = false
            };

        public static void ResetRoundCombat(
            PlayerRuntime player, string spawn, Side side, WeaponCatalog catalog = null)
        {
            player.ResetRound(spawn);
            player.Health = 100;
            if (player.Loadout == null)
                player.Loadout = DefaultPistolLoadout(side);
            if (!player.KeptWeaponFromPriorRound)
            {
                // Weapons persist only when kept; otherwise start from pistol + surviving armor.
            }
            if (!player.Loadout.HasArmor)
                player.ArmorHp = 0;
            else if (player.ArmorHp <= 0)
                player.ArmorHp = 100;
            if (catalog != null)
                WeaponAmmoRules.FillForLoadout(player, catalog);
        }

        public static void SyncTeamMoney(TeamRuntime team)
        {
            if (team.Players.Count == 0)
            {
                team.Money = 0;
                return;
            }
            // General mode: team economy is the sum of all player wallets.
            team.Money = team.Players.Sum(p => p.Money);
        }

        public static int ClampMoney(int money) =>
            Math.Max(0, Math.Min(MaximumPlayerMoney, money));

        public static void ApplyKillReward(PlayerRuntime killer, WeaponCatalog catalog, string weaponId)
        {
            WeaponDefinition weapon = catalog.GetOrKnife(weaponId);
            killer.Money = ClampMoney(killer.Money + Math.Max(0, weapon.killReward));
        }

        public static void Settle(TeamRuntime winner, TeamRuntime loser, string reason)
        {
            int winIncome = reason == "bomb_exploded" || reason == "bomb_defused"
                ? BombWinReward
                : WinReward;
            foreach (PlayerRuntime p in winner.Players)
                p.Money = ClampMoney(p.Money + winIncome);
            winner.LossStreak = 0;

            loser.LossStreak = Math.Min(4, loser.LossStreak + 1);
            int lossIncome = Math.Min(LossCap, LossBase + LossStreakBonus * loser.LossStreak);
            foreach (PlayerRuntime p in loser.Players)
                p.Money = ClampMoney(p.Money + lossIncome);

            SyncTeamMoney(winner);
            SyncTeamMoney(loser);
        }

        public static void ApplyOvertimeReset(TeamRuntime team)
        {
            foreach (PlayerRuntime p in team.Players)
                p.Money = OvertimePlayerMoney;
            SyncTeamMoney(team);
        }

        public static bool TrySpend(PlayerRuntime player, int cost)
        {
            if (cost <= 0)
                return true;
            if (player.Money < cost)
                return false;
            player.Money -= cost;
            return true;
        }
    }

    /// <summary>
    /// Facade for economy-card affordability / estimates. Actual loadout picks live in
    /// <see cref="BehaviorTreeBuyRules"/> (general mode).
    /// </summary>
    public static class EconomyCardLoadoutRules
    {
        public static int EstimateTeamCardCost(
            TeamRuntime team, EquipmentLevel level, WeaponCatalog catalog) =>
            BehaviorTreeBuyRules.EstimateTeamCardCost(team, level, catalog);

        /// <summary>Ideal spend for one player at the card tier (used by tests / helpers).</summary>
        public static int EstimateCardCost(
            EquipmentLevel level, Side side, WeaponCatalog catalog, bool isAwper) =>
            BehaviorTreeBuyRules.EstimateIdealCostForRole(level, side, catalog, isAwper, null);

        public static bool CanAffordCard(TeamRuntime team, EquipmentLevel level, WeaponCatalog catalog) =>
            BehaviorTreeBuyRules.CanAffordCard(team, level, catalog);

        public static EquipmentLevel BestAffordable(
            TeamRuntime team, WeaponCatalog catalog, bool hasAwper) =>
            BehaviorTreeBuyRules.BestAffordable(team, catalog, hasAwper);

        public static void BuyForTeam(
            TeamRuntime team, EquipmentLevel desired, WeaponCatalog catalog) =>
            BehaviorTreeBuyRules.BuyForTeam(team, desired, catalog);

        public static void BuyForPlayer(
            PlayerRuntime player, Side side, EquipmentLevel desired, WeaponCatalog catalog) =>
            BehaviorTreeBuyRules.BuyForPlayer(player, side, desired, catalog);
    }

    /// <summary>Pistol-round $800 spend focus. Team must include at least two distinct roles.</summary>
    public enum PistolRoundBuyRole
    {
        Armor = 0,
        BetterPistol = 1,
        Utility = 2
    }

    /// <summary>Full/force buy utility density for team roles.</summary>
    public enum UtilBuyProfile
    {
        None = 0,
        Light = 1,
        Entry = 2,
        Stack = 3
    }

    /// <summary>
    /// General-mode buy phase: economy card is team strategy; each agent buys within that intent.
    /// Hero targets are player-designated on <see cref="TeamRuntime.HeroBuyPlayerIds"/>.
    /// </summary>
    public static class BehaviorTreeBuyRules
    {
        private const float PistolRoundDualBerettasChanceT = 0.40f;
        private const float PistolRoundDualBerettasChanceCt = 0.18f;
        public const float HelpGunBuyChance = 0.40f;
        private const int FlashPrice = 200;
        private const int SmokePrice = 300;
        private const int HePrice = 300;
        private const int MolotovPrice = 400;
        private const int IncendiaryPrice = 600;

        private sealed class BuyIntent
        {
            public string[] PrimaryPool;
            public bool WantVest;
            public bool WantHelmet;
            public bool WantDefuseKit;
            public bool AllowPistolUpgrade;
            public bool PistolOnlyUpgrade;
            public bool ArmorWithLeftoverOnly;
        }

        public static bool IsDualBerettas(string weaponId) =>
            !string.IsNullOrEmpty(weaponId) &&
            (string.Equals(weaponId, "Dual_Berettas", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(weaponId, "elite", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(weaponId, "dual_beretta", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(weaponId, "dualberettas", StringComparison.OrdinalIgnoreCase));

        public static int TeamDualBerettasCount(TeamRuntime team) =>
            team?.Players?.Count(p => IsDualBerettas(p?.Loadout?.PistolWeaponId)) ?? 0;

        public static int EstimateTeamCardCost(
            TeamRuntime team, EquipmentLevel level, WeaponCatalog catalog)
        {
            if (level == EquipmentLevel.Eco || team == null || team.Players.Count == 0)
                return 0;

            if (level == EquipmentLevel.Hero)
            {
                IReadOnlyList<string> heroes = ResolveHeroIds(team);
                int total = 0;
                foreach (PlayerRuntime player in team.Players)
                {
                    if (!IsHeroTarget(player, heroes))
                        continue;
                    total += EstimatePlayerIdealCost(player, team.Side, EquipmentLevel.Hero, catalog);
                }
                return total;
            }

            int sum = 0;
            foreach (PlayerRuntime player in team.Players)
                sum += EstimatePlayerIdealCost(player, team.Side, level, catalog);
            return sum;
        }

        public static bool CanAffordCard(TeamRuntime team, EquipmentLevel level, WeaponCatalog catalog)
        {
            if (level == EquipmentLevel.Eco)
                return true;
            ItemizedEconomyRules.SyncTeamMoney(team);
            return team.Money >= EstimateTeamCardCost(team, level, catalog);
        }

        public static EquipmentLevel BestAffordable(
            TeamRuntime team, WeaponCatalog catalog, bool hasAwper)
        {
            // AI never auto-picks Hero — that card is player-designated.
            EquipmentLevel desired = EquipmentLevel.FullBuy;
            while (desired > EquipmentLevel.Eco && !CanAffordCard(team, desired, catalog))
                desired--;
            return desired;
        }

        public static void SetHeroTargets(TeamRuntime team, IEnumerable<string> playerIds)
        {
            team.HeroBuyPlayerIds.Clear();
            if (playerIds == null)
                return;
            foreach (string id in playerIds)
            {
                if (string.IsNullOrEmpty(id))
                    continue;
                if (team.HeroBuyPlayerIds.Count >= 2)
                    break;
                if (team.Players.Any(p =>
                        string.Equals(p.Definition.id, id, StringComparison.OrdinalIgnoreCase)))
                    team.HeroBuyPlayerIds.Add(id);
            }
        }

        public static List<string> SuggestHeroTargets(TeamRuntime team)
        {
            var picks = new List<string>();
            PlayerRuntime awper = team.Players.FirstOrDefault(IsAwper);
            if (awper != null)
                picks.Add(awper.Definition.id);
            foreach (PlayerRuntime p in team.Players
                         .OrderByDescending(x => x.Definition.attributes.aim)
                         .ThenByDescending(x => x.Money))
            {
                if (picks.Count >= 2)
                    break;
                if (picks.Any(id =>
                        string.Equals(id, p.Definition.id, StringComparison.OrdinalIgnoreCase)))
                    continue;
                picks.Add(p.Definition.id);
            }
            return picks;
        }

        public static void BuyForTeam(
            TeamRuntime team, EquipmentLevel desired, WeaponCatalog catalog) =>
            BuyForTeam(team, desired, catalog, null, null, null);

        public static void BuyForTeam(
            TeamRuntime team,
            EquipmentLevel desired,
            WeaponCatalog catalog,
            List<GroundLootItem> dropBag,
            string dropZoneId,
            IRandomSource random)
        {
            team.Equipment = desired;
            if (desired == EquipmentLevel.Hero && team.HeroBuyPlayerIds.Count == 0)
                SetHeroTargets(team, SuggestHeroTargets(team));

            IReadOnlyList<string> heroes = ResolveHeroIds(team);
            string utilityAnchorId = null;
            string entryAnchorId = null;
            bool buyUtil = desired == EquipmentLevel.FullBuy || desired == EquipmentLevel.ForceBuy;
            if (buyUtil)
                ResolveUtilityAnchors(team, out utilityAnchorId, out entryAnchorId);

            foreach (PlayerRuntime player in team.Players)
            {
                UtilBuyProfile utilProfile = UtilBuyProfile.None;
                if (buyUtil && player?.Definition != null)
                {
                    if (string.Equals(player.Definition.id, utilityAnchorId, StringComparison.OrdinalIgnoreCase))
                        utilProfile = UtilBuyProfile.Stack;
                    else if (string.Equals(player.Definition.id, entryAnchorId, StringComparison.OrdinalIgnoreCase))
                        utilProfile = UtilBuyProfile.Entry;
                    // Others: no personal Light util — extras go to spawn drops.
                }

                BuyForPlayer(player, team.Side, desired, catalog, heroes, utilProfile);
            }

            if (buyUtil &&
                dropBag != null &&
                !string.IsNullOrEmpty(dropZoneId))
            {
                ApplyUtilitySpawnDrops(
                    team, dropBag, dropZoneId, utilityAnchorId, entryAnchorId);
            }

            if (desired == EquipmentLevel.FullBuy)
                ApplyHelpGunBuys(team, team.Side, catalog, random);

            ItemizedEconomyRules.SyncTeamMoney(team);
        }

        /// <summary>
        /// Entry + one rifler buy flash/smoke into spawn GroundLoot (not inventory).
        /// Caps ~2 flash + 1 smoke across donors.
        /// </summary>
        public static void ApplyUtilitySpawnDrops(
            TeamRuntime team,
            List<GroundLootItem> dropBag,
            string dropZoneId,
            string throwerId,
            string entryId)
        {
            if (team?.Players == null || dropBag == null || string.IsNullOrEmpty(dropZoneId))
                return;

            int flashDrops = 0;
            int smokeDrops = 0;
            const int maxFlashDrops = 2;
            const int maxSmokeDrops = 1;

            var donors = new List<PlayerRuntime>();
            PlayerRuntime entry = team.Players.FirstOrDefault(p =>
                p?.Definition != null &&
                string.Equals(p.Definition.id, entryId, StringComparison.OrdinalIgnoreCase));
            if (entry != null)
                donors.Add(entry);

            PlayerRuntime riflerDonor = team.Players
                .Where(p => p?.Definition != null &&
                            !string.Equals(p.Definition.id, throwerId, StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(p.Definition.id, entryId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(p => p.Money)
                .ThenBy(p => p.Definition.id, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
            if (riflerDonor != null)
                donors.Add(riflerDonor);

            foreach (PlayerRuntime donor in donors)
            {
                while (flashDrops < maxFlashDrops &&
                       donor.Money >= FlashPrice &&
                       ItemizedEconomyRules.TrySpend(donor, FlashPrice))
                {
                    dropBag.Add(new GroundLootItem
                    {
                        Kind = GroundLootKind.Flash,
                        ZoneId = dropZoneId
                    });
                    flashDrops++;
                }

                if (smokeDrops < maxSmokeDrops &&
                    donor.Money >= SmokePrice &&
                    ItemizedEconomyRules.TrySpend(donor, SmokePrice))
                {
                    dropBag.Add(new GroundLootItem
                    {
                        Kind = GroundLootKind.Smoke,
                        ZoneId = dropZoneId
                    });
                    smokeDrops++;
                }
            }
        }

        /// <summary>
        /// ~40% chance: richest leftover donor buys a side rifle onto a teammate with no primary.
        /// </summary>
        public static void ApplyHelpGunBuys(
            TeamRuntime team, Side side, WeaponCatalog catalog, IRandomSource random)
        {
            if (team?.Players == null || catalog == null)
                return;

            string rifleId = side == Side.Terrorist ? "ak47" : "m4a4";
            if (!catalog.TryGet(rifleId, out WeaponDefinition rifle) || rifle == null)
                return;

            var needGun = team.Players
                .Where(p => p?.Definition != null &&
                            p.Loadout != null &&
                            string.IsNullOrEmpty(p.Loadout.PrimaryWeaponId))
                .OrderBy(p => p.Definition.id, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (needGun.Count == 0)
                return;

            if (random != null && random.Next01() >= HelpGunBuyChance)
                return;
            // null random → always attempt (tests without RNG still deterministic via donor pick)

            foreach (PlayerRuntime recipient in needGun)
            {
                PlayerRuntime donor = team.Players
                    .Where(p => p?.Definition != null &&
                                p.Loadout != null &&
                                !string.Equals(p.Definition.id, recipient.Definition.id,
                                    StringComparison.OrdinalIgnoreCase) &&
                                p.Money >= rifle.price)
                    .OrderByDescending(p => p.Money)
                    .ThenBy(p => p.Definition.id, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();
                if (donor == null)
                    continue;
                if (!ItemizedEconomyRules.TrySpend(donor, rifle.price))
                    continue;
                recipient.Loadout.PrimaryWeaponId = rifle.id;
                WeaponAmmoRules.FillSlot(recipient, catalog, primary: true);
            }
        }

        public static void ResolveUtilityAnchors(
            TeamRuntime team, out string utilityAnchorId, out string entryAnchorId)
        {
            utilityAnchorId = null;
            entryAnchorId = null;
            if (team?.Players == null || team.Players.Count == 0)
                return;

            var alive = team.Players.Where(p => p?.Definition != null).ToList();
            if (alive.Count == 0)
                return;

            PlayerRuntime util = alive
                .OrderByDescending(p =>
                    string.Equals(p.Definition.primaryRole, "Support", StringComparison.OrdinalIgnoreCase)
                        ? 1_000_000
                        : 0)
                .ThenByDescending(p => p.Definition.attributes.utility)
                .ThenBy(p => p.Definition.id, StringComparer.OrdinalIgnoreCase)
                .First();
            utilityAnchorId = util.Definition.id;
            string utilId = utilityAnchorId;

            PlayerRuntime entry = alive
                .Where(p => !string.Equals(p.Definition.id, utilId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(p =>
                    string.Equals(p.Definition.primaryRole, "Entry", StringComparison.OrdinalIgnoreCase)
                        ? 1_000_000
                        : 0)
                .ThenByDescending(p => p.Definition.attributes.opening)
                .ThenBy(p => p.Definition.id, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
            entryAnchorId = entry?.Definition.id;
        }

        public static void BuyPistolRound(
            TeamRuntime team, WeaponCatalog catalog, IRandomSource random = null)
        {
            // Pistol round ($800): split team across armor / better pistol / utility.
            // At least two of those three must appear. Dual Berettas only on pistol-upgrade buyers.
            team.Equipment = EquipmentLevel.Eco;
            team.HeroBuyPlayerIds.Clear();
            var buyers = new List<PlayerRuntime>();
            foreach (PlayerRuntime player in team.Players)
            {
                if (player == null)
                    continue;
                if (player.Loadout == null)
                    player.Loadout = ItemizedEconomyRules.DefaultPistolLoadout(team.Side);
                EnsurePistol(player, team.Side);
                buyers.Add(player);
            }

            Dictionary<string, PistolRoundBuyRole> roles = AssignPistolRoundRoles(buyers, random);

            string dualiesBuyerId = null;
            float chance = team.Side == Side.Terrorist
                ? PistolRoundDualBerettasChanceT
                : PistolRoundDualBerettasChanceCt;
            var pistolBuyers = buyers
                .Where(p => roles.TryGetValue(p.Definition.id, out PistolRoundBuyRole role) &&
                            role == PistolRoundBuyRole.BetterPistol)
                .ToList();
            if (random != null &&
                pistolBuyers.Count > 0 &&
                TeamDualBerettasCount(team) == 0 &&
                random.Next01() < chance)
            {
                PlayerRuntime buyer = PickDualBerettasBuyer(pistolBuyers, catalog);
                if (buyer != null && TryBuyDualBerettas(buyer, catalog))
                    dualiesBuyerId = buyer.Definition.id;
            }

            foreach (PlayerRuntime player in buyers)
            {
                roles.TryGetValue(player.Definition.id, out PistolRoundBuyRole role);
                bool dualiesOwner = !string.IsNullOrEmpty(dualiesBuyerId) &&
                    string.Equals(player.Definition.id, dualiesBuyerId, StringComparison.OrdinalIgnoreCase);
                ApplyPistolRoundRoleBuy(player, team.Side, role, dualiesOwner, catalog);
                player.KeptWeaponFromPriorRound = false;
                WeaponAmmoRules.FillForLoadout(player, catalog);
            }
            ItemizedEconomyRules.SyncTeamMoney(team);
        }

        /// <summary>Classifies a finished pistol-round loadout into one of the three spend styles.</summary>
        public static bool TryClassifyPistolRoundBuy(PlayerRuntime player, Side side, out PistolRoundBuyRole role)
        {
            role = PistolRoundBuyRole.Armor;
            if (player?.Loadout == null)
                return false;

            bool upgradedPistol = IsUpgradedPistol(player.Loadout.PistolWeaponId, side);
            bool hasUtil = player.Loadout.HasFlash || player.Loadout.HasSmoke || player.Loadout.HasHe ||
                           player.Loadout.HasFire || player.Loadout.HasDefuseKit;
            bool hasArmor = player.Loadout.HasArmor;

            if (upgradedPistol && !hasArmor)
            {
                role = PistolRoundBuyRole.BetterPistol;
                return true;
            }

            if (hasUtil && !hasArmor && !upgradedPistol)
            {
                role = PistolRoundBuyRole.Utility;
                return true;
            }

            if (hasArmor && !upgradedPistol)
            {
                role = PistolRoundBuyRole.Armor;
                return true;
            }

            return false;
        }

        public static int CountDistinctPistolRoundBuyRoles(TeamRuntime team)
        {
            if (team?.Players == null || team.Players.Count == 0)
                return 0;
            var seen = new HashSet<PistolRoundBuyRole>();
            foreach (PlayerRuntime player in team.Players)
            {
                if (TryClassifyPistolRoundBuy(player, team.Side, out PistolRoundBuyRole role))
                    seen.Add(role);
            }
            return seen.Count;
        }

        private static void ApplyPistolRoundRoleBuy(
            PlayerRuntime player,
            Side side,
            PistolRoundBuyRole role,
            bool dualiesOwner,
            WeaponCatalog catalog)
        {
            switch (role)
            {
                case PistolRoundBuyRole.Armor:
                    ApplyBuy(player, side, new BuyIntent
                    {
                        PrimaryPool = Array.Empty<string>(),
                        WantVest = true,
                        WantHelmet = false,
                        WantDefuseKit = false,
                        AllowPistolUpgrade = false,
                        PistolOnlyUpgrade = false
                    }, catalog);
                    break;
                case PistolRoundBuyRole.BetterPistol:
                    if (dualiesOwner)
                        break;
                    ApplyBuy(player, side, new BuyIntent
                    {
                        PrimaryPool = Array.Empty<string>(),
                        WantVest = false,
                        WantHelmet = false,
                        WantDefuseKit = false,
                        AllowPistolUpgrade = true,
                        PistolOnlyUpgrade = true
                    }, catalog);
                    break;
                case PistolRoundBuyRole.Utility:
                    BuyPistolRoundUtility(player, side, catalog);
                    break;
            }
        }

        private static void BuyPistolRoundUtility(
            PlayerRuntime player, Side side, WeaponCatalog catalog)
        {
            EnsurePistol(player, side);
            // Prefer nades that fit in $800: smoke + flash + HE = 800.
            if (!player.Loadout.HasSmoke && ItemizedEconomyRules.TrySpend(player, SmokePrice))
                player.Loadout.HasSmoke = true;
            if (player.Loadout.FlashCount < PlayerLoadout.MaxFlashCount &&
                ItemizedEconomyRules.TrySpend(player, FlashPrice))
                player.Loadout.FlashCount++;
            if (!player.Loadout.HasHe && ItemizedEconomyRules.TrySpend(player, HePrice))
                player.Loadout.HasHe = true;

            int firePrice = side == Side.Terrorist ? MolotovPrice : IncendiaryPrice;
            if (!player.Loadout.HasFire && ItemizedEconomyRules.TrySpend(player, firePrice))
            {
                player.Loadout.HasFire = true;
                player.Loadout.FireKind = PlayerLoadout.DefaultFireKind(side);
            }

            if (side == Side.CounterTerrorist &&
                !player.Loadout.HasDefuseKit &&
                ItemizedEconomyRules.TrySpend(player, catalog.DefuseKitPrice))
                player.Loadout.HasDefuseKit = true;
        }

        private static Dictionary<string, PistolRoundBuyRole> AssignPistolRoundRoles(
            List<PlayerRuntime> players, IRandomSource random)
        {
            var map = new Dictionary<string, PistolRoundBuyRole>(StringComparer.OrdinalIgnoreCase);
            if (players == null || players.Count == 0)
                return map;

            if (players.Count == 1)
            {
                map[players[0].Definition.id] = PistolRoundBuyRole.BetterPistol;
                return map;
            }

            var roster = new List<PlayerRuntime>(players);
            ShuffleInPlace(roster, random);

            PistolRoundBuyRole[] pool = BuildPistolRoundRolePool(roster.Count, random);
            for (int i = 0; i < roster.Count; i++)
                map[roster[i].Definition.id] = pool[i % pool.Length];

            // Guarantee every chosen role appears at least once (pool already sized for that).
            EnsureMinimumRoleCoverage(map, roster, pool);
            return map;
        }

        private static PistolRoundBuyRole[] BuildPistolRoundRolePool(int playerCount, IRandomSource random)
        {
            var all = new List<PistolRoundBuyRole>
            {
                PistolRoundBuyRole.Armor,
                PistolRoundBuyRole.BetterPistol,
                PistolRoundBuyRole.Utility
            };
            ShuffleInPlace(all, random);

            int distinct = playerCount >= 3 && (random == null || random.Next01() < 0.55f)
                ? 3
                : 2;
            if (distinct > playerCount)
                distinct = playerCount;
            if (distinct < 2)
                distinct = Math.Min(2, playerCount);

            return all.Take(distinct).ToArray();
        }

        private static void EnsureMinimumRoleCoverage(
            Dictionary<string, PistolRoundBuyRole> map,
            List<PlayerRuntime> roster,
            PistolRoundBuyRole[] pool)
        {
            if (map == null || roster == null || pool == null || pool.Length == 0)
                return;

            foreach (PistolRoundBuyRole needed in pool)
            {
                if (map.Values.Any(r => r == needed))
                    continue;
                // Reassign the last player that currently duplicates another covered role.
                for (int i = roster.Count - 1; i >= 0; i--)
                {
                    string id = roster[i].Definition.id;
                    PistolRoundBuyRole current = map[id];
                    int copies = map.Values.Count(r => r == current);
                    if (copies <= 1)
                        continue;
                    map[id] = needed;
                    break;
                }
            }
        }

        private static void ShuffleInPlace<T>(IList<T> list, IRandomSource random)
        {
            if (list == null || list.Count <= 1)
                return;
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j;
                if (random != null)
                    j = random.Range(0, i + 1);
                else
                    j = i; // keep order when no RNG
                if (j < 0 || j > i)
                    j = 0;
                T tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }

        private static bool IsUpgradedPistol(string pistolId, Side side)
        {
            if (string.IsNullOrEmpty(pistolId))
                return false;
            string starter = side == Side.Terrorist ? "glock" : "usp_s";
            if (string.Equals(pistolId, starter, StringComparison.OrdinalIgnoreCase))
                return false;
            return true;
        }

        private static PlayerRuntime PickDualBerettasBuyer(
            IEnumerable<PlayerRuntime> candidates, WeaponCatalog catalog)
        {
            if (candidates == null || !catalog.TryGet("Dual_Berettas", out WeaponDefinition dualies))
                return null;
            return candidates
                .Where(p => p?.Loadout != null && p.Money >= dualies.price)
                .OrderBy(p => DualBerettasBuyerPriority(p.Definition.primaryRole))
                .ThenBy(p => p.Definition.id, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
        }

        public static void BuyForPlayer(
            PlayerRuntime player, Side side, EquipmentLevel desired, WeaponCatalog catalog) =>
            BuyForPlayer(player, side, desired, catalog, null, UtilBuyProfile.None);

        public static void BuyForPlayer(
            PlayerRuntime player,
            Side side,
            EquipmentLevel desired,
            WeaponCatalog catalog,
            IReadOnlyList<string> heroIds) =>
            BuyForPlayer(player, side, desired, catalog, heroIds, UtilBuyProfile.None);

        public static void BuyForPlayer(
            PlayerRuntime player,
            Side side,
            EquipmentLevel desired,
            WeaponCatalog catalog,
            IReadOnlyList<string> heroIds,
            UtilBuyProfile utilProfile)
        {
            if (player.Loadout == null)
                player.Loadout = ItemizedEconomyRules.DefaultPistolLoadout(side);

            if (desired == EquipmentLevel.Eco)
            {
                player.KeptWeaponFromPriorRound = !string.IsNullOrEmpty(player.Loadout.PrimaryWeaponId);
                EnsurePistol(player, side);
                WeaponAmmoRules.FillForLoadout(player, catalog);
                return;
            }

            if (desired == EquipmentLevel.Hero)
            {
                if (!IsHeroTarget(player, heroIds ?? Array.Empty<string>()))
                {
                    EnsurePistol(player, side);
                    player.KeptWeaponFromPriorRound = !string.IsNullOrEmpty(player.Loadout.PrimaryWeaponId);
                    WeaponAmmoRules.FillForLoadout(player, catalog);
                    return;
                }

                BuyIntent heroIntent = BuildIntent(EquipmentLevel.Hero, side, IsAwper(player));
                ApplyBuy(player, side, heroIntent, catalog);
                player.KeptWeaponFromPriorRound = !string.IsNullOrEmpty(player.Loadout.PrimaryWeaponId);
                WeaponAmmoRules.FillForLoadout(player, catalog);
                return;
            }

            BuyIntent intent = BuildIntent(desired, side, IsAwper(player));
            ApplyBuy(player, side, intent, catalog);
            if (utilProfile != UtilBuyProfile.None)
                BuyUtilityForRole(player, side, utilProfile);
            player.KeptWeaponFromPriorRound = !string.IsNullOrEmpty(player.Loadout.PrimaryWeaponId);
            WeaponAmmoRules.FillForLoadout(player, catalog);
        }

        public static void BuyUtilityForRole(PlayerRuntime player, Side side, UtilBuyProfile profile)
        {
            if (player?.Loadout == null || profile == UtilBuyProfile.None)
                return;

            int firePrice = side == Side.Terrorist ? MolotovPrice : IncendiaryPrice;

            switch (profile)
            {
                case UtilBuyProfile.Stack:
                    TryBuySmoke(player);
                    TryBuyHe(player);
                    TryBuyFire(player, firePrice, side);
                    TryBuyFlash(player);
                    TryBuyFlash(player);
                    break;
                case UtilBuyProfile.Entry:
                    TryBuyFlash(player);
                    break;
                case UtilBuyProfile.Light:
                    if (player.Loadout.FlashCount < 1)
                        TryBuyFlash(player);
                    else if (!player.Loadout.HasSmoke)
                        TryBuySmoke(player);
                    break;
            }
        }

        private static void TryBuyFlash(PlayerRuntime player)
        {
            if (!player.Loadout.CanTakeFlash)
                return;
            if (ItemizedEconomyRules.TrySpend(player, FlashPrice))
                player.Loadout.FlashCount++;
        }

        private static void TryBuySmoke(PlayerRuntime player)
        {
            if (player.Loadout.HasSmoke)
                return;
            if (ItemizedEconomyRules.TrySpend(player, SmokePrice))
                player.Loadout.HasSmoke = true;
        }

        private static void TryBuyHe(PlayerRuntime player)
        {
            if (player.Loadout.HasHe)
                return;
            if (ItemizedEconomyRules.TrySpend(player, HePrice))
                player.Loadout.HasHe = true;
        }

        private static void TryBuyFire(PlayerRuntime player, int firePrice, Side side)
        {
            if (player.Loadout.HasFire)
                return;
            if (!ItemizedEconomyRules.TrySpend(player, firePrice))
                return;
            player.Loadout.HasFire = true;
            player.Loadout.FireKind = PlayerLoadout.DefaultFireKind(side);
        }

        public static int EstimatePlayerIdealCost(
            PlayerRuntime player, Side side, EquipmentLevel level, WeaponCatalog catalog)
        {
            if (level == EquipmentLevel.Eco)
                return 0;
            string ownedPrimary = player.Loadout?.PrimaryWeaponId;
            bool hasArmor = player.Loadout != null && player.Loadout.HasArmor;
            bool hasHelmet = player.Loadout != null && player.Loadout.HasHelmet;
            bool hasKit = player.Loadout != null && player.Loadout.HasDefuseKit;
            string ownedPistol = player.Loadout?.PistolWeaponId;
            return EstimateIdealCostForRole(
                level, side, catalog, IsAwper(player), ownedPrimary, hasArmor, hasHelmet, hasKit, ownedPistol);
        }

        public static int EstimateIdealCostForRole(
            EquipmentLevel level, Side side, WeaponCatalog catalog, bool isAwper, string ownedPrimary,
            bool hasArmor = false, bool hasHelmet = false, bool hasKit = false, string ownedPistol = null)
        {
            if (level == EquipmentLevel.Eco)
                return 0;
            BuyIntent intent = BuildIntent(level, side, isAwper);
            int cost = 0;

            if (intent.PistolOnlyUpgrade ||
                (intent.AllowPistolUpgrade && (intent.PrimaryPool == null || intent.PrimaryPool.Length == 0)))
            {
                if (catalog.TryGet("deagle", out WeaponDefinition deagle) &&
                    !string.Equals(ownedPistol, "deagle", StringComparison.OrdinalIgnoreCase))
                    cost += deagle.price;
            }
            else
            {
                string preferred = PreferredPrimary(intent.PrimaryPool, catalog, isAwper ? "Awper" : "Rifler");
                if (!string.IsNullOrEmpty(preferred) &&
                    !string.Equals(ownedPrimary, preferred, StringComparison.OrdinalIgnoreCase) &&
                    catalog.TryGet(preferred, out WeaponDefinition primary))
                    cost += primary.price;
            }

            if (!intent.ArmorWithLeftoverOnly)
            {
                if (intent.WantHelmet)
                {
                    if (!(hasArmor && hasHelmet))
                    {
                        if (hasArmor && !hasHelmet)
                            cost += catalog.HelmetUpgradePrice;
                        else
                            cost += catalog.ArmorHelmetPrice;
                    }
                }
                else if (intent.WantVest && !hasArmor)
                    cost += catalog.ArmorVestPrice;
            }

            if (intent.WantDefuseKit && side == Side.CounterTerrorist && !hasKit)
                cost += catalog.DefuseKitPrice;

            return cost;
        }

        private static void ApplyBuy(
            PlayerRuntime player, Side side, BuyIntent intent, WeaponCatalog catalog)
        {
            EnsurePistol(player, side);
            string role = player.Definition.primaryRole ?? "Rifler";

            if (intent.PistolOnlyUpgrade)
            {
                TryUpgradePistol(player, catalog);
            }
            else
            {
                bool boughtOrKeptPrimary = TryBuyPrimary(player, intent.PrimaryPool, catalog, role);
                if (!boughtOrKeptPrimary && intent.AllowPistolUpgrade)
                    TryUpgradePistol(player, catalog);
            }

            if (intent.ArmorWithLeftoverOnly)
            {
                var leftover = new BuyIntent
                {
                    WantVest = true,
                    WantHelmet = player.Money >= catalog.ArmorHelmetPrice || player.Loadout.HasArmor
                };
                BuyArmor(player, leftover, catalog);
            }
            else
            {
                BuyArmor(player, intent, catalog);
            }

            if (intent.WantDefuseKit && side == Side.CounterTerrorist && !player.Loadout.HasDefuseKit &&
                ItemizedEconomyRules.TrySpend(player, catalog.DefuseKitPrice))
                player.Loadout.HasDefuseKit = true;
        }

        private static void TryUpgradePistol(PlayerRuntime player, WeaponCatalog catalog)
        {
            if (string.Equals(player.Loadout.PistolWeaponId, "deagle", StringComparison.OrdinalIgnoreCase))
                return;
            if (IsDualBerettas(player.Loadout.PistolWeaponId))
                return;
            if (catalog.TryGet("deagle", out WeaponDefinition deagle) &&
                ItemizedEconomyRules.TrySpend(player, deagle.price))
            {
                player.Loadout.PistolWeaponId = deagle.id;
                return;
            }
            if (catalog.TryGet("p250", out WeaponDefinition p250) &&
                !string.Equals(player.Loadout.PistolWeaponId, "p250", StringComparison.OrdinalIgnoreCase) &&
                ItemizedEconomyRules.TrySpend(player, p250.price))
            {
                player.Loadout.PistolWeaponId = p250.id;
            }
        }

        private static bool TryBuyDualBerettas(PlayerRuntime player, WeaponCatalog catalog)
        {
            if (player?.Loadout == null || IsDualBerettas(player.Loadout.PistolWeaponId))
                return false;
            if (!catalog.TryGet("Dual_Berettas", out WeaponDefinition dualies))
                return false;
            if (!ItemizedEconomyRules.TrySpend(player, dualies.price))
                return false;
            player.Loadout.PistolWeaponId = dualies.id;
            return true;
        }

        private static int DualBerettasBuyerPriority(string role)
        {
            if (string.Equals(role, "Entry", StringComparison.OrdinalIgnoreCase))
                return 0;
            if (string.Equals(role, "Igl", StringComparison.OrdinalIgnoreCase))
                return 1;
            if (string.Equals(role, "Rifler", StringComparison.OrdinalIgnoreCase))
                return 2;
            return 3;
        }

        private static bool TryBuyPrimary(
            PlayerRuntime player, string[] pool, WeaponCatalog catalog, string role)
        {
            if (pool == null || pool.Length == 0)
                return !string.IsNullOrEmpty(player.Loadout.PrimaryWeaponId);

            string[] ranked = pool
                .Where(id => catalog.TryGet(id, out _))
                .OrderByDescending(id => GroundLootRules.WeaponPickupScore(id, role))
                .ToArray();

            foreach (string candidate in ranked)
            {
                if (!catalog.TryGet(candidate, out WeaponDefinition weapon))
                    continue;
                if (string.Equals(player.Loadout.PrimaryWeaponId, candidate, StringComparison.OrdinalIgnoreCase))
                    return true;
                if (!GroundLootRules.PreferWeapon(candidate, player.Loadout.PrimaryWeaponId, role))
                    continue;
                if (!ItemizedEconomyRules.TrySpend(player, weapon.price))
                    continue;
                player.Loadout.PrimaryWeaponId = weapon.id;
                return true;
            }

            return !string.IsNullOrEmpty(player.Loadout.PrimaryWeaponId);
        }

        private static string PreferredPrimary(string[] pool, WeaponCatalog catalog, string role)
        {
            if (pool == null || pool.Length == 0)
                return null;
            string best = null;
            int bestScore = int.MinValue;
            foreach (string id in pool)
            {
                if (!catalog.TryGet(id, out _))
                    continue;
                int score = GroundLootRules.WeaponPickupScore(id, role);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = id;
                }
            }
            return best;
        }

        private static void BuyArmor(PlayerRuntime player, BuyIntent intent, WeaponCatalog catalog)
        {
            if (!intent.WantVest && !intent.WantHelmet)
                return;

            if (intent.WantHelmet)
            {
                if (player.Loadout.HasArmor && player.Loadout.HasHelmet)
                    return;
                if (player.Loadout.HasArmor && !player.Loadout.HasHelmet)
                {
                    if (ItemizedEconomyRules.TrySpend(player, catalog.HelmetUpgradePrice))
                    {
                        player.Loadout.HasHelmet = true;
                        player.ArmorHp = Math.Max(player.ArmorHp, 100);
                    }
                    return;
                }
                if (ItemizedEconomyRules.TrySpend(player, catalog.ArmorHelmetPrice))
                {
                    player.Loadout.HasArmor = true;
                    player.Loadout.HasHelmet = true;
                    player.ArmorHp = 100;
                    return;
                }
                if (ItemizedEconomyRules.TrySpend(player, catalog.ArmorVestPrice))
                {
                    player.Loadout.HasArmor = true;
                    player.Loadout.HasHelmet = false;
                    player.ArmorHp = 100;
                }
                return;
            }

            if (intent.WantVest && !player.Loadout.HasArmor &&
                ItemizedEconomyRules.TrySpend(player, catalog.ArmorVestPrice))
            {
                player.Loadout.HasArmor = true;
                player.ArmorHp = 100;
            }
        }

        private static BuyIntent BuildIntent(EquipmentLevel level, Side side, bool isAwper)
        {
            string sideSmg = side == Side.Terrorist ? "mac10" : "mp9";
            string[] forcePool = side == Side.Terrorist
                ? new[] { "ak47", "Galil_AR", sideSmg, "ump45", "mp7" }
                : new[] { "m4a4", "m4a1s", "famas", sideSmg, "ump45", "mp7" };
            string[] fullPool = side == Side.Terrorist
                ? new[] { "awp", "ak47", "Galil_AR", sideSmg, "ump45", "mp7" }
                : new[] { "awp", "m4a4", "m4a1s", "famas", sideSmg, "ump45", "mp7" };
            string[] fullRiflerPool = side == Side.Terrorist
                ? new[] { "ak47", "Galil_AR", sideSmg, "ump45", "mp7" }
                : new[] { "m4a4", "m4a1s", "famas", sideSmg, "ump45", "mp7" };
            string[] heroPool = isAwper
                ? (side == Side.Terrorist
                    ? new[] { "awp", "ssg08", "ak47", "Galil_AR" }
                    : new[] { "awp", "ssg08", "m4a4", "m4a1s", "famas" })
                : fullRiflerPool;

            switch (level)
            {
                case EquipmentLevel.HalfBuy:
                    return new BuyIntent
                    {
                        PrimaryPool = Array.Empty<string>(),
                        WantVest = true,
                        WantHelmet = true,
                        WantDefuseKit = false,
                        AllowPistolUpgrade = true,
                        PistolOnlyUpgrade = true
                    };
                case EquipmentLevel.ForceBuy:
                    return new BuyIntent
                    {
                        PrimaryPool = forcePool,
                        WantVest = false,
                        WantHelmet = false,
                        WantDefuseKit = false,
                        AllowPistolUpgrade = false,
                        ArmorWithLeftoverOnly = true
                    };
                case EquipmentLevel.FullBuy:
                    return new BuyIntent
                    {
                        PrimaryPool = isAwper ? fullPool : fullRiflerPool,
                        WantVest = true,
                        WantHelmet = true,
                        WantDefuseKit = side == Side.CounterTerrorist,
                        AllowPistolUpgrade = true
                    };
                case EquipmentLevel.Hero:
                    return new BuyIntent
                    {
                        PrimaryPool = heroPool,
                        WantVest = false,
                        WantHelmet = false,
                        WantDefuseKit = false,
                        AllowPistolUpgrade = false,
                        ArmorWithLeftoverOnly = true
                    };
                default:
                    return new BuyIntent
                    {
                        PrimaryPool = Array.Empty<string>(),
                        WantVest = false,
                        WantHelmet = false,
                        WantDefuseKit = false
                    };
            }
        }

        private static IReadOnlyList<string> ResolveHeroIds(TeamRuntime team)
        {
            if (team.HeroBuyPlayerIds.Count > 0)
                return team.HeroBuyPlayerIds;
            return SuggestHeroTargets(team);
        }

        private static bool IsHeroTarget(PlayerRuntime player, IReadOnlyList<string> heroIds)
        {
            if (player == null || heroIds == null || heroIds.Count == 0)
                return false;
            string id = player.Definition.id;
            for (int i = 0; i < heroIds.Count; i++)
            {
                if (string.Equals(heroIds[i], id, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static void EnsurePistol(PlayerRuntime player, Side side)
        {
            if (string.IsNullOrEmpty(player.Loadout.PistolWeaponId))
                player.Loadout.PistolWeaponId = side == Side.Terrorist ? "glock" : "usp_s";
        }

        private static bool IsAwper(PlayerRuntime player) =>
            player?.Definition != null &&
            string.Equals(player.Definition.primaryRole, "Awper", StringComparison.OrdinalIgnoreCase);
    }
}
