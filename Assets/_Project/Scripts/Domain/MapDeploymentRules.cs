using System;
using System.Collections.Generic;
using System.Linq;

namespace TacticaUniversalis.Domain
{
    /// <summary>Shared site / approach assignment for quick and general-mode rounds.</summary>
    public static class MapDeploymentRules
    {
        private static bool IsTrain(string mapId) =>
            string.Equals(mapId, "train", StringComparison.OrdinalIgnoreCase);

        private static bool IsAncient(string mapId) =>
            string.Equals(mapId, "ancient", StringComparison.OrdinalIgnoreCase);

        private static bool IsOverpass(string mapId) =>
            string.Equals(mapId, "overpass", StringComparison.OrdinalIgnoreCase);

        private static bool IsVertigo(string mapId) =>
            string.Equals(mapId, "vertigo", StringComparison.OrdinalIgnoreCase);

        private static bool IsInferno(string mapId) =>
            string.Equals(mapId, "inferno", StringComparison.OrdinalIgnoreCase);

        private static bool IsMirage(string mapId) =>
            string.Equals(mapId, "mirage", StringComparison.OrdinalIgnoreCase);

        private static bool IsNuke(string mapId) =>
            string.Equals(mapId, "nuke", StringComparison.OrdinalIgnoreCase);

        public static string[] AttackDeploymentZones(string mapId, string primary)
        {
            if (IsTrain(mapId))
            {
                switch (primary)
                {
                    case "b_site":
                    case "b_halls":
                    case "b_ramp":
                    case "popdog":
                    case "underpass":
                    case "sidewalk":
                        return new[] { "b_halls", "popdog", "connector", "b_site", "underpass" };
                    case "ivy":
                    case "old_bomb":
                    case "pigeons":
                        return new[] { "ivy", "pigeons", "old_bomb", "a_site", "alley" };
                    case "connector":
                    case "kitchen":
                    case "a_main":
                        return new[] { "a_main", "kitchen", "connector", "a_site", "brown_halls" };
                    default:
                        return new[] { "alley", "a_main", "connector", "a_site", "ivy" };
                }
            }

            if (IsAncient(mapId))
            {
                switch (primary)
                {
                    case "b_site":
                    case "ramp":
                    case "water":
                    case "ruins":
                    case "cave":
                    case "tunnel":
                    case "catwalk":
                        return new[] { "tunnel", "water", "ruins", "ramp", "b_site" };
                    case "donut":
                    case "mid":
                    case "bottom_mid":
                    case "top_mid":
                    case "xbox":
                        return new[] { "bottom_mid", "mid", "donut", "xbox", "catwalk" };
                    case "a_main":
                    case "a_long":
                    case "temple":
                        return new[] { "a_main", "a_long", "donut", "a_site", "temple" };
                    default:
                        return new[] { "t_stairs", "a_main", "mid", "a_site", "donut" };
                }
            }

            if (IsOverpass(mapId))
            {
                switch (primary)
                {
                    case "b_site":
                    case "monster":
                    case "water":
                    case "canal":
                    case "b_short":
                    case "upper_tunnels":
                        return new[] { "upper_tunnels", "monster", "water", "canal", "b_site" };
                    case "long":
                    case "toilets":
                    case "short":
                    case "bank":
                    case "truck":
                        return new[] { "long", "toilets", "short", "a_site", "truck" };
                    case "mid":
                    case "fountain":
                    case "connector":
                    case "squeaky":
                        return new[] { "fountain", "mid", "connector", "squeaky", "short" };
                    default:
                        return new[] { "playground", "fountain", "mid", "a_site", "connector" };
                }
            }

            if (IsVertigo(mapId))
            {
                switch (primary)
                {
                    case "b_site":
                    case "b_stairs":
                    case "generator":
                    case "rafters":
                    case "quad":
                    case "pillar_b":
                        return new[] { "b_stairs", "generator", "quad", "b_site", "rafters" };
                    case "a_site":
                    case "a_ramp":
                    case "top_ramp":
                    case "scaffold":
                    case "a_catwalk":
                    case "sandbag":
                    case "a_sidewalk":
                        return new[] { "a_ramp", "top_ramp", "scaffold", "a_site", "sandbag" };
                    case "mid":
                    case "elevator":
                    case "connector":
                    case "ladder":
                    case "boost":
                        return new[] { "mid", "elevator", "connector", "ladder", "boost" };
                    default:
                        return new[] { "t_stairs", "mid", "connector", "a_site", "elevator" };
                }
            }

            if (IsInferno(mapId))
            {
                switch (primary)
                {
                    case "b_site":
                    case "banana":
                    case "banana_car":
                    case "logs":
                    case "construction":
                    case "coffins":
                        return new[] { "banana", "logs", "banana_car", "b_site", "construction" };
                    case "a_site":
                    case "arch":
                    case "boiler":
                    case "apartments":
                    case "balcony":
                    case "pit":
                    case "library":
                    case "truck":
                        return new[] { "arch", "boiler", "apartments", "a_site", "pit" };
                    case "mid":
                    case "alt_mid":
                    case "underpass":
                    case "top_mid":
                    case "speedway":
                        return new[] { "alt_mid", "mid", "underpass", "top_mid", "arch" };
                    default:
                        return new[] { "banana", "mid", "banana", "a_site", "top_mid" };
                }
            }

            if (IsMirage(mapId))
            {
                switch (primary)
                {
                    case "b_site":
                    case "b_apps":
                    case "market":
                    case "kitchen":
                    case "van":
                    case "catwalk":
                    case "b_short":
                        return new[] { "b_apps", "kitchen", "catwalk", "b_site", "b_short" };
                    case "a_site":
                    case "palace":
                    case "a_ramp":
                    case "balcony":
                    case "jungle":
                    case "ticket":
                    case "sandwich":
                        return new[] { "a_ramp", "palace", "sandwich", "a_site", "jungle" };
                    case "mid":
                    case "top_mid":
                    case "underpass":
                    case "connector":
                    case "window":
                        return new[] { "top_mid", "mid", "underpass", "connector", "window" };
                    default:
                        return new[] { "mid", "top_mid", "a_ramp", "a_site", "connector" };
                }
            }

            if (IsNuke(mapId))
            {
                switch (primary)
                {
                    case "b_site":
                    case "ramp":
                    case "secret":
                    case "garage":
                        return new[] { "ramp", "secret", "garage", "b_site", "outside" };
                    case "a_site":
                    case "hut":
                    case "squeaky":
                    case "mini":
                    case "main":
                    case "heaven":
                    case "hell":
                    case "a_rafters":
                        return new[] { "lobby", "hut", "squeaky", "a_site", "mini" };
                    case "lobby":
                    case "radio":
                    case "trophy":
                    case "silo":
                    case "truck":
                        return new[] { "truck", "silo", "lobby", "radio", "trophy" };
                    default:
                        return new[] { "outside", "lobby", "hut", "a_site", "ramp" };
                }
            }

            switch (primary)
            {
                case "b_tunnels":
                case "b_site":
                    return new[] { "b_tunnels", "mid", "b_doors", "b_site", "xbox" };
                case "a_short":
                case "cat":
                    return new[] { "a_short", "cat", "mid", "a_site", "b_tunnels" };
                case "mid":
                case "xbox":
                    return new[] { "mid", "xbox", "a_short", "b_tunnels", "top_mid" };
                default:
                    return new[] { "a_long", "mid", "a_short", "outside_long", "b_tunnels" };
            }
        }

        public static string[] DefendDeploymentZones(string mapId, string primary)
        {
            if (IsTrain(mapId))
            {
                switch (primary)
                {
                    case "b_site":
                    case "b_halls":
                    case "b_ramp":
                    case "popdog":
                    case "underpass":
                    case "sidewalk":
                        return new[] { "b_site", "b_halls", "ct_spawn", "connector", "sidewalk" };
                    case "ivy":
                    case "old_bomb":
                    case "pigeons":
                        return new[] { "old_bomb", "a_site", "ct_spawn", "heaven", "ivy" };
                    case "connector":
                    case "kitchen":
                    case "a_main":
                        return new[] { "heaven", "a_site", "ct_spawn", "connector", "kitchen" };
                    default:
                        return new[] { "a_site", "heaven", "ct_spawn", "old_bomb", "connector" };
                }
            }

            if (IsAncient(mapId))
            {
                switch (primary)
                {
                    case "b_site":
                    case "ramp":
                    case "water":
                    case "ruins":
                    case "cave":
                    case "tunnel":
                    case "catwalk":
                        return new[] { "b_site", "ramp", "ct_spawn", "cave", "a_site" };
                    case "donut":
                    case "mid":
                    case "bottom_mid":
                    case "top_mid":
                    case "xbox":
                        return new[] { "house", "top_mid", "ct_spawn", "donut", "mid" };
                    case "a_main":
                    case "a_long":
                    case "temple":
                        return new[] { "temple", "a_site", "ct_spawn", "donut", "b_site" };
                    default:
                        return new[] { "a_site", "temple", "ct_spawn", "house", "b_site" };
                }
            }

            if (IsOverpass(mapId))
            {
                switch (primary)
                {
                    case "b_site":
                    case "monster":
                    case "water":
                    case "canal":
                    case "b_short":
                    case "upper_tunnels":
                        return new[] { "b_site", "heaven", "ct_spawn", "b_short", "a_site" };
                    case "long":
                    case "toilets":
                    case "short":
                    case "bank":
                    case "truck":
                        return new[] { "bank", "a_site", "ct_spawn", "truck", "b_site" };
                    case "mid":
                    case "fountain":
                    case "connector":
                    case "squeaky":
                        return new[] { "squeaky", "connector", "ct_spawn", "short", "mid" };
                    default:
                        return new[] { "a_site", "bank", "ct_spawn", "heaven", "truck" };
                }
            }

            if (IsVertigo(mapId))
            {
                switch (primary)
                {
                    case "b_site":
                    case "b_stairs":
                    case "generator":
                    case "rafters":
                    case "quad":
                    case "pillar_b":
                        return new[] { "b_site", "generator", "ct_spawn", "quad", "a_site" };
                    case "a_site":
                    case "a_ramp":
                    case "top_ramp":
                    case "scaffold":
                    case "a_catwalk":
                    case "sandbag":
                    case "a_sidewalk":
                        return new[] { "a_site", "top_ramp", "ct_spawn", "scaffold", "b_site" };
                    case "mid":
                    case "elevator":
                    case "connector":
                    case "ladder":
                    case "boost":
                        return new[] { "elevator", "connector", "ct_spawn", "mid", "ladder" };
                    default:
                        return new[] { "a_site", "elevator", "ct_spawn", "b_site", "connector" };
                }
            }

            if (IsInferno(mapId))
            {
                switch (primary)
                {
                    case "b_site":
                    case "banana":
                    case "banana_car":
                    case "logs":
                    case "construction":
                    case "coffins":
                        return new[] { "b_site", "coffins", "ct_spawn", "construction", "a_site" };
                    case "a_site":
                    case "arch":
                    case "boiler":
                    case "apartments":
                    case "balcony":
                    case "pit":
                    case "library":
                    case "truck":
                        return new[] { "a_site", "pit", "ct_spawn", "library", "b_site" };
                    case "mid":
                    case "alt_mid":
                    case "underpass":
                    case "top_mid":
                    case "speedway":
                        return new[] { "top_mid", "arch", "ct_spawn", "speedway", "mid" };
                    default:
                        return new[] { "a_site", "library", "ct_spawn", "b_site", "speedway" };
                }
            }

            if (IsMirage(mapId))
            {
                switch (primary)
                {
                    case "b_site":
                    case "b_apps":
                    case "market":
                    case "kitchen":
                    case "van":
                    case "catwalk":
                    case "b_short":
                        return new[] { "b_site", "van", "ct_spawn", "market", "a_site" };
                    case "a_site":
                    case "palace":
                    case "a_ramp":
                    case "balcony":
                    case "jungle":
                    case "ticket":
                    case "sandwich":
                        return new[] { "a_site", "ticket", "ct_spawn", "jungle", "b_site" };
                    case "mid":
                    case "top_mid":
                    case "underpass":
                    case "connector":
                    case "window":
                        return new[] { "window", "connector", "ct_spawn", "mid", "jungle" };
                    default:
                        return new[] { "a_site", "ticket", "ct_spawn", "b_site", "window" };
                }
            }

            if (IsNuke(mapId))
            {
                switch (primary)
                {
                    case "b_site":
                    case "ramp":
                    case "secret":
                    case "garage":
                        return new[] { "b_site", "ramp", "ct_spawn", "secret", "a_site" };
                    case "a_site":
                    case "hut":
                    case "squeaky":
                    case "mini":
                    case "main":
                    case "heaven":
                    case "hell":
                    case "a_rafters":
                        return new[] { "a_site", "heaven", "ct_spawn", "hut", "b_site" };
                    case "lobby":
                    case "radio":
                    case "trophy":
                    case "silo":
                    case "truck":
                        return new[] { "lockers", "heaven", "ct_spawn", "lobby", "outside" };
                    default:
                        return new[] { "a_site", "heaven", "ct_spawn", "b_site", "lockers" };
                }
            }

            switch (primary)
            {
                case "b_tunnels":
                case "b_site":
                    return new[] { "b_site", "b_doors", "mid", "a_site", "xbox" };
                case "a_short":
                case "cat":
                    return new[] { "cat", "a_site", "mid", "b_site", "a_short" };
                case "mid":
                case "xbox":
                    return new[] { "xbox", "mid", "a_short", "b_site", "a_long" };
                default:
                    return new[] { "a_long", "a_site", "mid", "b_site", "cat" };
            }
        }

        public static string AssignDeployZone(
            string mapId, string role, string primary, string[] zones, bool attacking, int index)
        {
            if (IsTrain(mapId))
            {
                bool bSide = primary == "b_site" || primary == "b_halls" || primary == "b_ramp" ||
                             primary == "popdog" || primary == "underpass" || primary == "sidewalk";
                if (role == "Entry") return attacking ? primary : zones[0];
                if (role == "Awper")
                    return bSide ? "connector" : "ivy";
                if (role == "Support")
                    return attacking
                        ? (bSide ? "popdog" : "a_main")
                        : zones[Math.Min(1, zones.Length - 1)];
                if (role == "Igl") return attacking ? "connector" : "ct_spawn";
                return zones[Math.Min(index, zones.Length - 1)];
            }

            if (IsAncient(mapId))
            {
                bool bSide = primary == "b_site" || primary == "ramp" || primary == "water" ||
                             primary == "ruins" || primary == "cave" || primary == "tunnel" ||
                             primary == "catwalk";
                if (role == "Entry") return attacking ? primary : zones[0];
                if (role == "Awper")
                    return bSide ? "catwalk" : "a_long";
                if (role == "Support")
                    return attacking
                        ? (bSide ? "ruins" : "donut")
                        : zones[Math.Min(1, zones.Length - 1)];
                if (role == "Igl") return attacking ? "mid" : "ct_spawn";
                return zones[Math.Min(index, zones.Length - 1)];
            }

            if (IsOverpass(mapId))
            {
                bool bSide = primary == "b_site" || primary == "monster" || primary == "water" ||
                             primary == "canal" || primary == "b_short" || primary == "upper_tunnels";
                if (role == "Entry") return attacking ? primary : zones[0];
                if (role == "Awper")
                    return bSide ? "heaven" : "long";
                if (role == "Support")
                    return attacking
                        ? (bSide ? "water" : "toilets")
                        : zones[Math.Min(1, zones.Length - 1)];
                if (role == "Igl") return attacking ? "connector" : "ct_spawn";
                return zones[Math.Min(index, zones.Length - 1)];
            }

            if (IsVertigo(mapId))
            {
                bool bSide = primary == "b_site" || primary == "b_stairs" || primary == "generator" ||
                             primary == "rafters" || primary == "quad" || primary == "pillar_b";
                if (role == "Entry") return attacking ? primary : zones[0];
                if (role == "Awper")
                    return bSide ? "rafters" : "scaffold";
                if (role == "Support")
                    return attacking
                        ? (bSide ? "generator" : "a_ramp")
                        : zones[Math.Min(1, zones.Length - 1)];
                if (role == "Igl") return attacking ? "mid" : "ct_spawn";
                return zones[Math.Min(index, zones.Length - 1)];
            }

            if (IsInferno(mapId))
            {
                bool bSide = primary == "b_site" || primary == "banana" || primary == "banana_car" ||
                             primary == "logs" || primary == "construction" || primary == "coffins";
                if (role == "Entry") return attacking ? primary : zones[0];
                if (role == "Awper")
                    return bSide ? "coffins" : "arch";
                if (role == "Support")
                    return attacking
                        ? (bSide ? "banana" : "boiler")
                        : zones[Math.Min(1, zones.Length - 1)];
                if (role == "Igl") return attacking ? "mid" : "ct_spawn";
                return zones[Math.Min(index, zones.Length - 1)];
            }

            if (IsMirage(mapId))
            {
                bool bSide = primary == "b_site" || primary == "b_apps" || primary == "market" ||
                             primary == "kitchen" || primary == "van" || primary == "catwalk" ||
                             primary == "b_short";
                if (role == "Entry") return attacking ? primary : zones[0];
                if (role == "Awper")
                    return bSide ? "catwalk" : "window";
                if (role == "Support")
                    return attacking
                        ? (bSide ? "b_apps" : "a_ramp")
                        : zones[Math.Min(1, zones.Length - 1)];
                if (role == "Igl") return attacking ? "mid" : "ct_spawn";
                return zones[Math.Min(index, zones.Length - 1)];
            }

            if (IsNuke(mapId))
            {
                bool bSide = primary == "b_site" || primary == "ramp" || primary == "secret" ||
                             primary == "garage";
                if (role == "Entry") return attacking ? primary : zones[0];
                if (role == "Awper")
                    return bSide ? "ramp" : "heaven";
                if (role == "Support")
                    return attacking
                        ? (bSide ? "secret" : "hut")
                        : zones[Math.Min(1, zones.Length - 1)];
                if (role == "Igl") return attacking ? "outside" : "ct_spawn";
                return zones[Math.Min(index, zones.Length - 1)];
            }

            if (role == "Entry") return attacking ? primary : zones[0];
            if (role == "Awper")
                return primary == "b_tunnels" || primary == "b_site" ? "xbox" : "a_long";
            if (role == "Support")
                return attacking
                    ? (primary.StartsWith("b") ? "b_tunnels" : "a_short")
                    : zones[Math.Min(1, zones.Length - 1)];
            if (role == "Igl") return attacking ? "mid" : "ct_spawn";
            return zones[Math.Min(index, zones.Length - 1)];
        }

        public enum DeploySide
        {
            Mid,
            A,
            B
        }

        public static DeploySide ClassifyDeploySide(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId))
                return DeploySide.Mid;
            if (IsMidishZone(zoneId))
                return DeploySide.Mid;
            return string.Equals(
                MapZones.ResolvePlantSite(zoneId), "b_site", StringComparison.OrdinalIgnoreCase)
                ? DeploySide.B
                : DeploySide.A;
        }

        public static bool IsMidishZone(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId))
                return true;
            if (string.Equals(zoneId, "mid", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(zoneId, "top_mid", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(zoneId, "bottom_mid", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(zoneId, "xbox", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(zoneId, "ct_mid", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(zoneId, "ct_spawn", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(zoneId, "connector", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(zoneId, "donut", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(zoneId, "elevator", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(zoneId, "window", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(zoneId, "squeaky", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(zoneId, "fountain", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(zoneId, "alt_mid", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(zoneId, "speedway", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(zoneId, "underpass", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(zoneId, "outside", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(zoneId, "lobby", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(zoneId, "ladder", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(zoneId, "boost", StringComparison.OrdinalIgnoreCase))
                return true;
            return zoneId.IndexOf("mid", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static string OppositePlantSite(string primary)
        {
            return string.Equals(
                MapZones.ResolvePlantSite(primary), "b_site", StringComparison.OrdinalIgnoreCase)
                ? "a_site"
                : "b_site";
        }

        /// <summary>Attack: up to 4 primary-side approaches + 1 opposite lurk, unique slots.</summary>
        public static string[] BuildAttackSpreadPool(string mapId, string primary)
        {
            string[] seed = AttackDeploymentZones(mapId, primary);
            return EnforceSpread(seed, primary, attacking: true, mapId);
        }

        /// <summary>Defend: 3 primary-side + 1 mid + 1 opposite, unique slots.</summary>
        public static string[] BuildDefendSpreadPool(string mapId, string primary)
        {
            string[] seed = DefendDeploymentZones(mapId, primary);
            return EnforceSpread(seed, primary, attacking: false, mapId);
        }

        public static string[] EnforceSpread(
            string[] seed, string primary, bool attacking, string mapId = null)
        {
            string opposite = OppositePlantSite(primary);
            DeploySide primarySide = ClassifyDeploySide(primary);
            if (primarySide == DeploySide.Mid)
                primarySide = DeploySide.A;

            int maxPrimary = attacking ? 4 : 3;
            var result = new List<string>(5);
            int primaryCount = 0;
            bool hasMid = false;
            bool hasOpposite = false;

            void TryAdd(string zone)
            {
                if (string.IsNullOrEmpty(zone) || result.Count >= 5)
                    return;
                if (result.Exists(z => string.Equals(z, zone, StringComparison.OrdinalIgnoreCase)))
                    return;

                DeploySide side = ClassifyDeploySide(zone);
                if (side == primarySide)
                {
                    if (primaryCount >= maxPrimary)
                        return;
                    primaryCount++;
                }
                else if (side == DeploySide.Mid)
                {
                    if (attacking)
                    {
                        // Attack mid slots count toward the 4 primary approaches budget.
                        if (primaryCount >= maxPrimary)
                            return;
                        primaryCount++;
                    }
                    else
                    {
                        if (hasMid)
                            return;
                        hasMid = true;
                    }
                }
                else
                {
                    if (hasOpposite)
                        return;
                    hasOpposite = true;
                }

                result.Add(zone);
            }

            if (seed != null)
            {
                for (int i = 0; i < seed.Length; i++)
                    TryAdd(seed[i]);
            }

            string[] fillers = DefaultFillers(mapId, primarySide, opposite);
            for (int i = 0; i < fillers.Length && result.Count < 5; i++)
                TryAdd(fillers[i]);

            // Force opposite lurk / hold if still missing.
            if (!hasOpposite)
                TryAdd(opposite);
            if (!attacking && !hasMid)
                TryAdd(DefaultMidZone(mapId));

            for (int i = 0; i < fillers.Length && result.Count < 5; i++)
            {
                string pad = fillers[i];
                if (result.Exists(z => string.Equals(z, pad, StringComparison.OrdinalIgnoreCase)))
                    continue;
                result.Add(pad);
            }

            return result.Take(5).ToArray();
        }

        private static string DefaultMidZone(string mapId)
        {
            if (IsTrain(mapId)) return "connector";
            if (IsAncient(mapId)) return "donut";
            if (IsOverpass(mapId)) return "connector";
            if (IsVertigo(mapId)) return "elevator";
            if (IsInferno(mapId)) return "top_mid";
            if (IsMirage(mapId)) return "window";
            if (IsNuke(mapId)) return "lobby";
            return "mid";
        }

        private static string[] DefaultFillers(string mapId, DeploySide primarySide, string opposite)
        {
            string mid = DefaultMidZone(mapId);
            string aSite = "a_site";
            string bSite = "b_site";
            if (IsTrain(mapId))
                return new[] { "a_site", "heaven", mid, "b_site", "connector", "ivy", "old_bomb" };
            if (IsAncient(mapId))
                return new[] { "a_site", "temple", mid, "b_site", "ramp", "house", "donut" };
            if (IsOverpass(mapId))
                return new[] { "a_site", "bank", mid, "b_site", "heaven", "short", "truck" };
            if (IsVertigo(mapId))
                return new[] { "a_site", "scaffold", mid, "b_site", "generator", "connector" };
            if (IsInferno(mapId))
                return new[] { "a_site", "arch", mid, "b_site", "banana", "library", "speedway" };
            if (IsMirage(mapId))
                return new[] { "a_site", "palace", mid, "b_site", "jungle", "window", "ticket" };
            if (IsNuke(mapId))
                return new[] { "a_site", "hut", mid, "b_site", "ramp", "heaven", "outside" };

            // Dust2
            if (primarySide == DeploySide.B)
                return new[] { "b_site", "b_doors", "b_tunnels", mid, "xbox", aSite, "a_long", "cat" };
            return new[] { "a_long", "a_site", "a_short", "cat", mid, "xbox", bSite, "b_doors" };
        }

    }
}
