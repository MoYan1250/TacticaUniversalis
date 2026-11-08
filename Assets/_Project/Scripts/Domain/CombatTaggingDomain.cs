using System;

namespace TacticaUniversalis.Domain
{
    /// <summary>
    /// CS:GO gun tagging: hit slows movement via flinch stack and velocity modifier.
    /// Recovery is +1/2.5 per second on the modifier; stack approaches 1 at 0.35/s.
    /// </summary>
    public static class CombatTaggingRules
    {
        public const float KnifeSpeed = 250f;
        public const float VelocityRecoverPerSecond = 1f / 2.5f;
        public const float FlinchStackRecoverPerSecond = 0.35f;

        public static void EnsureTaggingStats(WeaponDefinition weapon)
        {
            if (weapon == null)
                return;
            if (weapon.flinchLarge > 0f && weapon.flinchSmall > 0f && weapon.moveSpeed > 0f)
                return;

            string id = weapon.id ?? string.Empty;
            string cat = weapon.category ?? string.Empty;
            float large;
            float small;
            float speed;
            if (IdEq(id, "glock") || IdEq(id, "usp_s") || IdEq(id, "p250") || IdEq(id, "Dual_Berettas"))
                Set(0.50f, 0.65f, 240f, out large, out small, out speed);
            else if (IdEq(id, "deagle"))
                Set(0.50f, 0.65f, 230f, out large, out small, out speed);
            else if (IdEq(id, "mac10"))
                Set(0.45f, 0.60f, 240f, out large, out small, out speed);
            else if (IdEq(id, "mp9"))
                Set(0.45f, 0.60f, 240f, out large, out small, out speed);
            else if (IdEq(id, "ump45"))
                Set(0.45f, 0.60f, 230f, out large, out small, out speed);
            else if (IdEq(id, "mp7"))
                Set(0.45f, 0.60f, 220f, out large, out small, out speed);
            else if (IdEq(id, "Galil_AR"))
                Set(0.40f, 0.55f, 215f, out large, out small, out speed);
            else if (IdEq(id, "famas"))
                Set(0.40f, 0.55f, 220f, out large, out small, out speed);
            else if (IdEq(id, "ak47"))
                Set(0.40f, 0.55f, 215f, out large, out small, out speed);
            else if (IdEq(id, "m4a4") || IdEq(id, "m4a1s"))
                Set(0.40f, 0.55f, 225f, out large, out small, out speed);
            else if (IdEq(id, "awp"))
                Set(0.35f, 0.40f, 200f, out large, out small, out speed);
            else if (IdEq(id, "ssg08"))
                Set(0.40f, 0.55f, 230f, out large, out small, out speed);
            else if (cat.IndexOf("knife", StringComparison.OrdinalIgnoreCase) >= 0)
                Set(0.60f, 0.70f, 250f, out large, out small, out speed);
            else if (cat.IndexOf("pistol", StringComparison.OrdinalIgnoreCase) >= 0)
                Set(0.50f, 0.65f, 240f, out large, out small, out speed);
            else if (cat.IndexOf("smg", StringComparison.OrdinalIgnoreCase) >= 0)
                Set(0.45f, 0.60f, 230f, out large, out small, out speed);
            else if (cat.IndexOf("sniper", StringComparison.OrdinalIgnoreCase) >= 0)
                Set(0.35f, 0.40f, 200f, out large, out small, out speed);
            else
                Set(0.40f, 0.55f, 215f, out large, out small, out speed);

            if (weapon.flinchLarge <= 0f)
                weapon.flinchLarge = large;
            if (weapon.flinchSmall <= 0f)
                weapon.flinchSmall = small;
            if (weapon.moveSpeed <= 0f)
                weapon.moveSpeed = speed;
        }

        public static void ApplyHit(
            PlayerRuntime victim, WeaponDefinition attackerWeapon, WeaponDefinition heldWeapon)
        {
            if (victim == null || !victim.Alive)
                return;
            EnsureTaggingStats(attackerWeapon);
            EnsureTaggingStats(heldWeapon);
            float large = attackerWeapon != null && attackerWeapon.flinchLarge > 0f
                ? attackerWeapon.flinchLarge
                : 0.40f;
            float small = attackerWeapon != null && attackerWeapon.flinchSmall > 0f
                ? attackerWeapon.flinchSmall
                : 0.55f;
            float stack = victim.FlinchStack <= 0f ? 1f : victim.FlinchStack;
            float next = Math.Min(stack, Math.Min(large, large - (1f - stack) * small));
            victim.FlinchStack = Clamp(next, 0.1f, 1f);

            float heldSpeed = heldWeapon != null && heldWeapon.moveSpeed > 0f
                ? heldWeapon.moveSpeed
                : KnifeSpeed;
            float scaler = Math.Max(0.15f, (heldSpeed - 120f) / (KnifeSpeed - 120f)) * 0.8f + 0.08f;
            float modifier = victim.FlinchStack * scaler;
            float ratio = Math.Min(1f, (heldSpeed - 80f) / (KnifeSpeed - 80f)) * 1.2f - 0.08f;
            float clampMin = Math.Max(0.2f, ratio / 4f);
            float clampMax = large > 0.65f ? large : 0.65f;
            victim.VelocityModifier = Clamp(modifier, clampMin, clampMax);
        }

        public static void Tick(PlayerRuntime player, float deltaSeconds)
        {
            if (player == null || deltaSeconds <= 0f)
                return;
            if (player.FlinchStack < 1f)
                player.FlinchStack = Math.Min(1f, player.FlinchStack + deltaSeconds * FlinchStackRecoverPerSecond);
            player.FlinchStack = Clamp(player.FlinchStack, 0.1f, 1f);
            if (player.VelocityModifier < 1f)
                player.VelocityModifier = Math.Min(
                    1f, player.VelocityModifier + deltaSeconds * VelocityRecoverPerSecond);
            if (player.VelocityModifier <= 0f)
                player.VelocityModifier = 1f;
        }

        public static float ScaledMoveInterval(float baseInterval, PlayerRuntime player)
        {
            float modifier = player != null && player.VelocityModifier > 0.05f
                ? player.VelocityModifier
                : 1f;
            modifier = Clamp(modifier, 0.2f, 1f);
            return baseInterval / modifier;
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        private static void Set(float large, float small, float speed, out float l, out float s, out float v)
        {
            l = large;
            s = small;
            v = speed;
        }

        private static bool IdEq(string id, string expected) =>
            string.Equals(id, expected, StringComparison.OrdinalIgnoreCase);
    }
}
