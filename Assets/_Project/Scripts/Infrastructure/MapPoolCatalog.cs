using System;
using System.IO;
using System.Linq;
using TacticaUniversalis.Domain;
using UnityEngine;

namespace TacticaUniversalis.Infrastructure
{
    [Serializable]
    public sealed class MapPoolJsonRoot
    {
        public int schemaVersion = 1;
        public MapPoolEntry[] maps;
    }

    public static class MapPoolCatalog
    {
        private static MapPoolEntry[] _cached;

        public static MapPoolEntry[] GetEntries()
        {
            if (_cached != null)
                return _cached;
            _cached = TryLoadFromDisk() ?? MapVetoRules.CreateDefaultPool();
            return _cached;
        }

        public static void ResetCacheForTests() => _cached = null;

        public static MapPoolEntry Find(string mapId)
        {
            if (string.IsNullOrEmpty(mapId))
                return null;
            return GetEntries().FirstOrDefault(x =>
                string.Equals(x.id, mapId, StringComparison.OrdinalIgnoreCase));
        }

        public static string DisplayName(string mapId)
        {
            MapPoolEntry entry = Find(mapId);
            if (entry != null && !string.IsNullOrWhiteSpace(entry.displayName))
                return entry.displayName;
            return mapId ?? "Dust II";
        }

        public static bool IsPlayable(string mapId)
        {
            MapPoolEntry entry = Find(mapId);
            return entry != null && entry.playable;
        }

        private static MapPoolEntry[] TryLoadFromDisk()
        {
            try
            {
                string path = Path.Combine(UnityEngine.Application.dataPath, "_Project", "Data", "JSON", "map_pool.json");
                if (!File.Exists(path))
                    return null;
                string json = File.ReadAllText(path);
                MapPoolJsonRoot root = JsonUtility.FromJson<MapPoolJsonRoot>(json);
                if (root?.maps == null || root.maps.Length != MapVetoRules.PoolSize)
                    return null;
                return root.maps;
            }
            catch
            {
                return null;
            }
        }
    }
}
