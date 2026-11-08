using System.Collections.Generic;
using UnityEngine;

namespace TacticaUniversalis.Presentation
{
    /// <summary>White weapon silhouette sprites under Resources/UI/Icons/Weapons.</summary>
    public static class WeaponIconAtlas
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        private static bool _loaded;

        public static Sprite Get(string weaponId)
        {
            EnsureLoaded();
            if (string.IsNullOrWhiteSpace(weaponId))
                return null;
            return Cache.TryGetValue(Normalize(weaponId), out Sprite sprite) ? sprite : null;
        }

        private static void EnsureLoaded()
        {
            if (_loaded)
                return;
            _loaded = true;
            TryAdd("mac10");
            TryAdd("ssg08");
            TryAdd("Galil_AR");
            TryAdd("m4a1s");
            TryAdd("ak47");
            TryAdd("awp");
            TryAdd("deagle");
            TryAdd("knife");
            TryAdd("glock");
            TryAdd("p250");
            TryAdd("usp_s");
            TryAdd("m4a4");
            TryAdd("mp7");
            TryAdd("mp9");
            TryAdd("FAMAS");
            TryAdd("Ump45");
            TryAdd("Dual_Berettas");
            Alias("ssg08", "ssg");
            Alias("m4a1s", "m4a1_s", "m4a1-s");
            Alias("usp_s", "usps", "usp-s");
            Alias("glock", "glock18");
            Alias("deagle", "desert_eagle", "deserteagle");
            Alias("knife", "melee", "bayonet");
            Alias("galil_ar", "galil");
            Alias("dual_berettas", "elite", "dualies", "dual_beretta");
        }

        private static void TryAdd(string resourceName)
        {
            Sprite sprite = Resources.Load<Sprite>("UI/Icons/Weapons/" + resourceName);
            if (sprite == null)
                return;
            Cache[Normalize(resourceName)] = sprite;
        }

        private static void Alias(string canonicalId, params string[] extraIds)
        {
            if (!Cache.TryGetValue(Normalize(canonicalId), out Sprite sprite) || sprite == null)
                return;
            foreach (string extra in extraIds)
            {
                if (!string.IsNullOrWhiteSpace(extra))
                    Cache[Normalize(extra)] = sprite;
            }
        }

        private static string Normalize(string weaponId) =>
            weaponId.Trim().ToLowerInvariant().Replace(' ', '_');
    }
}
