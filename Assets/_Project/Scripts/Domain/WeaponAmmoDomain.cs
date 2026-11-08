using System;

namespace TacticaUniversalis.Domain
{
    /// <summary>
    /// Hidden mag / reserve / reload. No UI, no simulation events.
    /// Empty mag mid-fight forces a silent pause so the other side can catch the timing.
    /// </summary>
    public static class WeaponAmmoRules
    {
        public static void EnsureAmmoStats(WeaponDefinition weapon)
        {
            if (weapon == null)
                return;
            string id = weapon.id ?? string.Empty;
            string cat = weapon.category ?? string.Empty;
            bool knife = cat.IndexOf("knife", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         string.Equals(id, "knife", StringComparison.OrdinalIgnoreCase);
            if (knife)
            {
                weapon.magazineSize = 0;
                weapon.reserveAmmo = 0;
                weapon.reloadTime = 0f;
                return;
            }

            if (weapon.magazineSize > 0 && weapon.reloadTime > 0f)
                return;

            int mag;
            int reserve;
            float reload;
            if (IdEq(id, "glock"))
                Set(20, 120, 2.2f, out mag, out reserve, out reload);
            else if (IdEq(id, "usp_s"))
                Set(12, 24, 2.2f, out mag, out reserve, out reload);
            else if (IdEq(id, "p250"))
                Set(13, 26, 2.2f, out mag, out reserve, out reload);
            else if (IdEq(id, "Dual_Berettas"))
                Set(30, 120, 3.8f, out mag, out reserve, out reload);
            else if (IdEq(id, "deagle"))
                Set(7, 35, 2.2f, out mag, out reserve, out reload);
            else if (IdEq(id, "mac10"))
                Set(30, 100, 2.6f, out mag, out reserve, out reload);
            else if (IdEq(id, "mp9"))
                Set(30, 120, 2.1f, out mag, out reserve, out reload);
            else if (IdEq(id, "ump45"))
                Set(25, 100, 2.4f, out mag, out reserve, out reload);
            else if (IdEq(id, "mp7"))
                Set(30, 120, 3.1f, out mag, out reserve, out reload);
            else if (IdEq(id, "Galil_AR"))
                Set(35, 90, 2.5f, out mag, out reserve, out reload);
            else if (IdEq(id, "famas"))
                Set(25, 90, 3.3f, out mag, out reserve, out reload);
            else if (IdEq(id, "ak47"))
                Set(30, 90, 2.4f, out mag, out reserve, out reload);
            else if (IdEq(id, "m4a4"))
                Set(30, 90, 3.1f, out mag, out reserve, out reload);
            else if (IdEq(id, "m4a1s"))
                Set(20, 80, 3.1f, out mag, out reserve, out reload);
            else if (IdEq(id, "awp"))
                Set(5, 30, 3.7f, out mag, out reserve, out reload);
            else if (IdEq(id, "ssg08"))
                Set(10, 90, 3.7f, out mag, out reserve, out reload);
            else if (cat.IndexOf("pistol", StringComparison.OrdinalIgnoreCase) >= 0)
                Set(12, 24, 2.2f, out mag, out reserve, out reload);
            else if (cat.IndexOf("smg", StringComparison.OrdinalIgnoreCase) >= 0)
                Set(30, 100, 2.5f, out mag, out reserve, out reload);
            else if (cat.IndexOf("sniper", StringComparison.OrdinalIgnoreCase) >= 0)
                Set(5, 30, 3.7f, out mag, out reserve, out reload);
            else
                Set(30, 90, 2.5f, out mag, out reserve, out reload);

            if (weapon.magazineSize <= 0)
                weapon.magazineSize = mag;
            if (weapon.reserveAmmo <= 0)
                weapon.reserveAmmo = reserve;
            if (weapon.reloadTime <= 0f)
                weapon.reloadTime = reload;
        }

        public static bool IsMelee(WeaponDefinition weapon) =>
            weapon == null ||
            weapon.magazineSize <= 0 ||
            string.Equals(weapon.id, "knife", StringComparison.OrdinalIgnoreCase) ||
            (weapon.category != null &&
             weapon.category.IndexOf("knife", StringComparison.OrdinalIgnoreCase) >= 0);

        public static string ActiveWeaponId(PlayerRuntime player)
        {
            if (player?.Loadout == null)
                return "knife";
            string primary = player.Loadout.PrimaryWeaponId;
            string pistol = player.Loadout.PistolWeaponId;
            if (player.PreferPistol && HasPistolAmmo(player) && !string.IsNullOrEmpty(pistol))
                return pistol;
            if (!string.IsNullOrEmpty(primary))
                return primary;
            if (!string.IsNullOrEmpty(pistol))
                return pistol;
            return "knife";
        }

        public static bool HasPrimaryAmmo(PlayerRuntime player) =>
            player != null && (player.PrimaryMag > 0 || player.PrimaryReserve > 0);

        public static bool HasPistolAmmo(PlayerRuntime player) =>
            player != null && (player.PistolMag > 0 || player.PistolReserve > 0);

        public static bool IsReloading(PlayerRuntime player, float time) =>
            player != null && player.ReloadEndTime > time + 0.0001f;

        public static void FillForLoadout(PlayerRuntime player, WeaponCatalog catalog)
        {
            if (player == null || catalog == null)
                return;
            player.ReloadEndTime = -1f;
            player.ReloadingWeaponId = null;
            FillSlot(player, catalog, primary: true);
            FillSlot(player, catalog, primary: false);
            player.PreferPistol = false;
        }

        public static void FillSlot(PlayerRuntime player, WeaponCatalog catalog, bool primary)
        {
            if (player == null || catalog == null)
                return;
            string id = primary ? player.Loadout?.PrimaryWeaponId : player.Loadout?.PistolWeaponId;
            if (string.IsNullOrEmpty(id))
            {
                if (primary)
                {
                    player.PrimaryMag = 0;
                    player.PrimaryReserve = 0;
                }
                else
                {
                    player.PistolMag = 0;
                    player.PistolReserve = 0;
                }

                return;
            }

            WeaponDefinition weapon = catalog.GetOrKnife(id);
            EnsureAmmoStats(weapon);
            if (IsMelee(weapon))
            {
                if (primary)
                {
                    player.PrimaryMag = 0;
                    player.PrimaryReserve = 0;
                }

                return;
            }

            if (primary)
            {
                player.PrimaryMag = weapon.magazineSize;
                player.PrimaryReserve = weapon.reserveAmmo;
                player.PreferPistol = false;
            }
            else
            {
                player.PistolMag = weapon.magazineSize;
                player.PistolReserve = weapon.reserveAmmo;
            }
        }

        public static void TickReload(PlayerRuntime player, WeaponCatalog catalog, float time)
        {
            if (player == null || player.ReloadEndTime < 0f)
                return;
            if (time + 0.0001f < player.ReloadEndTime)
                return;
            FinishReload(player, catalog);
        }

        public static bool BeginReload(PlayerRuntime player, WeaponCatalog catalog, float time)
        {
            if (player == null || catalog == null)
                return false;
            TickReload(player, catalog, time);
            if (IsReloading(player, time))
                return true;

            string id = ActiveWeaponId(player);
            WeaponDefinition weapon = catalog.GetOrKnife(id);
            EnsureAmmoStats(weapon);
            if (IsMelee(weapon))
                return false;

            GetMagReserve(player, id, out int mag, out int reserve);
            if (reserve <= 0 || mag >= weapon.magazineSize)
                return false;

            player.ReloadingWeaponId = id;
            player.ReloadEndTime = time + Math.Max(0.4f, weapon.reloadTime);
            return true;
        }

        public static bool TryConsumeShot(
            PlayerRuntime player, WeaponCatalog catalog, float time, out string weaponId)
        {
            weaponId = ActiveWeaponId(player);
            if (player == null || catalog == null)
                return false;
            TickReload(player, catalog, time);
            if (IsReloading(player, time))
                return false;

            WeaponDefinition weapon = catalog.GetOrKnife(weaponId);
            EnsureAmmoStats(weapon);
            if (IsMelee(weapon))
                return true;

            GetMagReserve(player, weaponId, out int mag, out int reserve);
            if (mag <= 0)
            {
                if (reserve > 0)
                    BeginReload(player, catalog, time);
                else if (!IsPistolWeapon(player, weaponId) && HasPistolAmmo(player))
                    player.PreferPistol = true;
                return false;
            }

            SetMagReserve(player, weaponId, mag - 1, reserve);
            return true;
        }

        public static bool ShouldReload(
            PlayerRuntime player, WeaponCatalog catalog, float time, bool inContact)
        {
            if (player == null || catalog == null)
                return false;
            TickReload(player, catalog, time);
            if (IsReloading(player, time))
                return true;

            string id = ActiveWeaponId(player);
            WeaponDefinition weapon = catalog.GetOrKnife(id);
            EnsureAmmoStats(weapon);
            if (IsMelee(weapon))
                return false;

            GetMagReserve(player, id, out int mag, out int reserve);
            if (reserve <= 0)
                return false;
            if (mag <= 0)
                return true;
            if (!inContact && mag < weapon.magazineSize * 0.35f)
                return true;
            return false;
        }

        private static void FinishReload(PlayerRuntime player, WeaponCatalog catalog)
        {
            string id = player.ReloadingWeaponId ?? ActiveWeaponId(player);
            WeaponDefinition weapon = catalog != null ? catalog.GetOrKnife(id) : null;
            EnsureAmmoStats(weapon);
            player.ReloadEndTime = -1f;
            player.ReloadingWeaponId = null;
            if (IsMelee(weapon))
                return;

            GetMagReserve(player, id, out int mag, out int reserve);
            int need = Math.Max(0, weapon.magazineSize - mag);
            int take = Math.Min(need, reserve);
            SetMagReserve(player, id, mag + take, reserve - take);
        }

        private static bool IsPistolWeapon(PlayerRuntime player, string weaponId) =>
            player?.Loadout != null &&
            !string.IsNullOrEmpty(weaponId) &&
            string.Equals(player.Loadout.PistolWeaponId, weaponId, StringComparison.OrdinalIgnoreCase);

        private static void GetMagReserve(PlayerRuntime player, string weaponId, out int mag, out int reserve)
        {
            if (IsPistolWeapon(player, weaponId))
            {
                mag = player.PistolMag;
                reserve = player.PistolReserve;
                return;
            }

            mag = player.PrimaryMag;
            reserve = player.PrimaryReserve;
        }

        private static void SetMagReserve(PlayerRuntime player, string weaponId, int mag, int reserve)
        {
            if (IsPistolWeapon(player, weaponId))
            {
                player.PistolMag = mag;
                player.PistolReserve = reserve;
                return;
            }

            player.PrimaryMag = mag;
            player.PrimaryReserve = reserve;
        }

        private static void Set(int mag, int reserve, float reload, out int m, out int r, out float t)
        {
            m = mag;
            r = reserve;
            t = reload;
        }

        private static bool IdEq(string id, string expected) =>
            string.Equals(id, expected, StringComparison.OrdinalIgnoreCase);
    }
}
