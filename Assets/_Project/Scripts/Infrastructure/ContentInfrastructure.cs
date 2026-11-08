using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TacticaUniversalis.Domain;
using UnityEngine;

namespace TacticaUniversalis.Infrastructure
{
    [Serializable]
    public sealed class PlayersJsonRoot
    {
        public int schemaVersion;
        public PlayerDefinition[] players;
    }

    [Serializable]
    public sealed class TeamsJsonRoot
    {
        public int schemaVersion;
        public TeamDefinition[] teams;
    }

    [Serializable]
    public sealed class TraitsJsonRoot
    {
        public int schemaVersion;
        public TraitDefinition[] traits;
    }

    [Serializable]
    public sealed class MapJsonRoot
    {
        public int schemaVersion;
        public MapDefinition map;
    }

    [Serializable]
    public sealed class TacticalCardsJsonRoot
    {
        public int schemaVersion;
        public TacticalCardDefinition[] cards;
    }

    [Serializable]
    public sealed class FocusCardsJsonRoot
    {
        public int schemaVersion;
        public FocusCardDefinition[] cards;
    }

    [Serializable]
    public sealed class CoachCardsJsonRoot
    {
        public int schemaVersion;
        public CoachCardDefinition[] cards;
    }

    [Serializable]
    public sealed class UtilityLineupJsonRoot
    {
        public int schemaVersion = 1;
        public UtilityLineupEntry[] lineups;
    }

    public static class ContentJsonParser
    {
        public static ContentDatabaseAsset ParseInto(
            ContentDatabaseAsset asset,
            string playersJson,
            string teamsJson,
            string traitsJson,
            string mapJson,
            string tacticalCardsJson = null,
            string focusCardsJson = null,
            string coachCardsJson = null,
            string trainMapJson = null,
            string ancientMapJson = null,
            string overpassMapJson = null,
            string vertigoMapJson = null,
            string infernoMapJson = null,
            string mirageMapJson = null,
            string nukeMapJson = null)
        {
            PlayersJsonRoot players = JsonUtility.FromJson<PlayersJsonRoot>(playersJson);
            TeamsJsonRoot teams = JsonUtility.FromJson<TeamsJsonRoot>(teamsJson);
            TraitsJsonRoot traits = JsonUtility.FromJson<TraitsJsonRoot>(traitsJson);
            MapJsonRoot map = JsonUtility.FromJson<MapJsonRoot>(mapJson);
            TacticalCardsJsonRoot tacticalCards = string.IsNullOrWhiteSpace(tacticalCardsJson)
                ? new TacticalCardsJsonRoot { schemaVersion = map.schemaVersion, cards = Array.Empty<TacticalCardDefinition>() }
                : JsonUtility.FromJson<TacticalCardsJsonRoot>(tacticalCardsJson);
            FocusCardsJsonRoot focusCards = string.IsNullOrWhiteSpace(focusCardsJson)
                ? new FocusCardsJsonRoot { schemaVersion = map.schemaVersion, cards = Array.Empty<FocusCardDefinition>() }
                : JsonUtility.FromJson<FocusCardsJsonRoot>(focusCardsJson);
            CoachCardsJsonRoot coachCards = string.IsNullOrWhiteSpace(coachCardsJson)
                ? new CoachCardsJsonRoot { schemaVersion = map.schemaVersion, cards = Array.Empty<CoachCardDefinition>() }
                : JsonUtility.FromJson<CoachCardsJsonRoot>(coachCardsJson);
            MapDefinition trainMap = null;
            if (!string.IsNullOrWhiteSpace(trainMapJson))
            {
                MapJsonRoot trainRoot = JsonUtility.FromJson<MapJsonRoot>(trainMapJson);
                trainMap = trainRoot?.map;
            }
            MapDefinition ancientMap = null;
            if (!string.IsNullOrWhiteSpace(ancientMapJson))
            {
                MapJsonRoot ancientRoot = JsonUtility.FromJson<MapJsonRoot>(ancientMapJson);
                ancientMap = ancientRoot?.map;
            }
            MapDefinition overpassMap = null;
            if (!string.IsNullOrWhiteSpace(overpassMapJson))
            {
                MapJsonRoot overpassRoot = JsonUtility.FromJson<MapJsonRoot>(overpassMapJson);
                overpassMap = overpassRoot?.map;
            }
            MapDefinition vertigoMap = null;
            if (!string.IsNullOrWhiteSpace(vertigoMapJson))
            {
                MapJsonRoot vertigoRoot = JsonUtility.FromJson<MapJsonRoot>(vertigoMapJson);
                vertigoMap = vertigoRoot?.map;
            }
            MapDefinition infernoMap = null;
            if (!string.IsNullOrWhiteSpace(infernoMapJson))
            {
                MapJsonRoot infernoRoot = JsonUtility.FromJson<MapJsonRoot>(infernoMapJson);
                infernoMap = infernoRoot?.map;
            }
            MapDefinition mirageMap = null;
            if (!string.IsNullOrWhiteSpace(mirageMapJson))
            {
                MapJsonRoot mirageRoot = JsonUtility.FromJson<MapJsonRoot>(mirageMapJson);
                mirageMap = mirageRoot?.map;
            }
            MapDefinition nukeMap = null;
            if (!string.IsNullOrWhiteSpace(nukeMapJson))
            {
                MapJsonRoot nukeRoot = JsonUtility.FromJson<MapJsonRoot>(nukeMapJson);
                nukeMap = nukeRoot?.map;
            }
            int version = new[]
            {
                players.schemaVersion, teams.schemaVersion, traits.schemaVersion,
                map.schemaVersion, tacticalCards.schemaVersion,
                focusCards.schemaVersion, coachCards.schemaVersion
            }.Min();
            asset.Replace(
                version, players.players, teams.teams, traits.traits, map.map,
                tacticalCards.cards, focusCards.cards, coachCards.cards,
                trainMap, ancientMap, overpassMap, vertigoMap, infernoMap, mirageMap, nukeMap);
            return asset;
        }
    }

    public static class ContentValidator
    {
        public static IReadOnlyList<string> Validate(ContentDatabaseAsset asset)
        {
            var errors = new List<string>();
            if (asset == null)
                return new[] { "Content database asset is null." };
            if (asset.SchemaVersion != 1)
                errors.Add($"Unsupported schemaVersion {asset.SchemaVersion}; expected 1.");
            if (asset.Players.Count < 10)
                errors.Add($"Expected at least 10 players, found {asset.Players.Count}.");
            if (asset.Teams.Count < 2)
                errors.Add($"Expected at least 2 teams, found {asset.Teams.Count}.");
            if (asset.Dust2 == null || asset.Dust2.id != "dust2")
                errors.Add("Dust II map definition is missing.");

            HashSet<string> playerIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (PlayerDefinition player in asset.Players)
            {
                if (string.IsNullOrWhiteSpace(player.id) || !playerIds.Add(player.id))
                    errors.Add($"Player ID is missing or duplicated: '{player.id}'.");
                if (player.attributes == null)
                    errors.Add($"{player.id}: combat attributes are missing.");
                else
                    ValidateAttributes(player, errors);
                if (string.IsNullOrWhiteSpace(player.dust2Grade))
                    errors.Add($"{player.id}: Dust II grade is missing.");
                foreach (string traitId in player.traitIds ?? Array.Empty<string>())
                    if (!asset.Traits.Any(x => x.id == traitId))
                        errors.Add($"{player.id}: unknown trait '{traitId}'.");
            }

            foreach (TeamDefinition team in asset.Teams)
            {
                int n = team.playerIds?.Length ?? 0;
                // Opening-day rankings can list 3–4 confirmed players; do not invent fillers.
                if (n < 1 || n > 5)
                    errors.Add($"{team.id}: expected 1..5 players, found {n}.");
                foreach (string playerId in team.playerIds ?? Array.Empty<string>())
                    if (!playerIds.Contains(playerId))
                        errors.Add($"{team.id}: unknown player '{playerId}'.");
            }

            if (asset.Dust2 != null)
            {
                HashSet<string> zoneIds = new HashSet<string>(
                    (asset.Dust2.zones ?? Array.Empty<MapZoneDefinition>()).Select(x => x.id),
                    StringComparer.OrdinalIgnoreCase);
                foreach (string required in new[]
                         {
                             "t_spawn", "ct_spawn", "a_long", "a_short", "cat", "a_site",
                             "mid", "xbox", "b_site", "b_tunnels"
                         })
                    if (!zoneIds.Contains(required))
                        errors.Add($"Dust II is missing zone '{required}'.");
            }

            if (asset.Train != null)
            {
                if (asset.Train.id != "train")
                    errors.Add("Train map definition id must be 'train'.");
                HashSet<string> trainZones = new HashSet<string>(
                    (asset.Train.zones ?? Array.Empty<MapZoneDefinition>()).Select(x => x.id),
                    StringComparer.OrdinalIgnoreCase);
                foreach (string required in new[]
                         {
                             "t_spawn", "ct_spawn", "a_site", "b_site", "alley", "ivy",
                             "popdog", "connector", "brown_halls"
                         })
                    if (!trainZones.Contains(required))
                        errors.Add($"Train is missing zone '{required}'.");
            }

            if (asset.Ancient != null)
            {
                if (asset.Ancient.id != "ancient")
                    errors.Add("Ancient map definition id must be 'ancient'.");
                HashSet<string> ancientZones = new HashSet<string>(
                    (asset.Ancient.zones ?? Array.Empty<MapZoneDefinition>()).Select(x => x.id),
                    StringComparer.OrdinalIgnoreCase);
                foreach (string required in new[]
                         {
                             "t_spawn", "ct_spawn", "a_site", "b_site", "mid", "donut",
                             "water", "ramp", "temple", "cave"
                         })
                    if (!ancientZones.Contains(required))
                        errors.Add($"Ancient is missing zone '{required}'.");
            }

            if (asset.Overpass != null)
            {
                if (asset.Overpass.id != "overpass")
                    errors.Add("Overpass map definition id must be 'overpass'.");
                HashSet<string> overpassZones = new HashSet<string>(
                    (asset.Overpass.zones ?? Array.Empty<MapZoneDefinition>()).Select(x => x.id),
                    StringComparer.OrdinalIgnoreCase);
                foreach (string required in new[]
                         {
                             "t_spawn", "ct_spawn", "a_site", "b_site", "long", "monster",
                             "water", "connector", "fountain", "toilets"
                         })
                    if (!overpassZones.Contains(required))
                        errors.Add($"Overpass is missing zone '{required}'.");
            }

            if (asset.Vertigo != null)
            {
                if (asset.Vertigo.id != "vertigo")
                    errors.Add("Vertigo map definition id must be 'vertigo'.");
                HashSet<string> vertigoZones = new HashSet<string>(
                    (asset.Vertigo.zones ?? Array.Empty<MapZoneDefinition>()).Select(x => x.id),
                    StringComparer.OrdinalIgnoreCase);
                foreach (string required in new[]
                         {
                             "t_spawn", "ct_spawn", "a_site", "b_site", "mid", "elevator",
                             "connector", "a_ramp", "generator", "t_stairs"
                         })
                    if (!vertigoZones.Contains(required))
                        errors.Add($"Vertigo is missing zone '{required}'.");
            }

            if (asset.Inferno != null)
            {
                if (asset.Inferno.id != "inferno")
                    errors.Add("Inferno map definition id must be 'inferno'.");
                HashSet<string> infernoZones = new HashSet<string>(
                    (asset.Inferno.zones ?? Array.Empty<MapZoneDefinition>()).Select(x => x.id),
                    StringComparer.OrdinalIgnoreCase);
                foreach (string required in new[]
                         {
                             "t_spawn", "ct_spawn", "a_site", "b_site", "banana", "mid",
                             "arch", "apartments", "construction", "pit"
                         })
                    if (!infernoZones.Contains(required))
                        errors.Add($"Inferno is missing zone '{required}'.");
            }

            if (asset.Mirage != null)
            {
                if (asset.Mirage.id != "mirage")
                    errors.Add("Mirage map definition id must be 'mirage'.");
                HashSet<string> mirageZones = new HashSet<string>(
                    (asset.Mirage.zones ?? Array.Empty<MapZoneDefinition>()).Select(x => x.id),
                    StringComparer.OrdinalIgnoreCase);
                foreach (string required in new[]
                         {
                             "t_spawn", "ct_spawn", "a_site", "b_site", "mid", "palace",
                             "a_ramp", "catwalk", "b_apps", "connector"
                         })
                    if (!mirageZones.Contains(required))
                        errors.Add($"Mirage is missing zone '{required}'.");
            }

            if (asset.Nuke != null)
            {
                if (asset.Nuke.id != "nuke")
                    errors.Add("Nuke map definition id must be 'nuke'.");
                HashSet<string> nukeZones = new HashSet<string>(
                    (asset.Nuke.zones ?? Array.Empty<MapZoneDefinition>()).Select(x => x.id),
                    StringComparer.OrdinalIgnoreCase);
                foreach (string required in new[]
                         {
                             "t_spawn", "ct_spawn", "a_site", "b_site", "lobby", "hut",
                             "outside", "ramp", "heaven", "secret"
                         })
                    if (!nukeZones.Contains(required))
                        errors.Add($"Nuke is missing zone '{required}'.");
            }
            foreach (MapDefinition map in new[]
                     {
                         asset.Dust2, asset.Train, asset.Ancient, asset.Overpass,
                         asset.Vertigo, asset.Inferno, asset.Mirage, asset.Nuke
                     })
                ValidateMapNavigation(map, errors);
            ValidateTacticalCards(asset, errors);
            ValidateFocusCards(asset, errors);
            ValidateCoachCards(asset, errors);
            return errors;
        }

        private static void ValidateMapNavigation(MapDefinition map, ICollection<string> errors)
        {
            if (map == null)
                return;
            var zoneIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var zones = new Dictionary<string, MapZoneDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (MapZoneDefinition zone in map.zones ?? Array.Empty<MapZoneDefinition>())
            {
                if (zone == null || string.IsNullOrWhiteSpace(zone.id) || !zoneIds.Add(zone.id))
                {
                    errors.Add($"{map.id}: duplicate or empty navigation zone id.");
                    continue;
                }
                zones[zone.id] = zone;
                if (zone.x < 0f || zone.x > 1f || zone.y < 0f || zone.y > 1f)
                    errors.Add($"{map.id}: zone '{zone.id}' lies outside the overview.");
            }

            var floorIds = new HashSet<string>(
                (map.floors ?? Array.Empty<MapFloorDefinition>())
                    .Where(x => x != null && !string.IsNullOrWhiteSpace(x.id))
                    .Select(x => x.id),
                StringComparer.OrdinalIgnoreCase);
            if (floorIds.Count == 0)
                errors.Add($"{map.id}: no map floor is defined.");
            if (!string.IsNullOrEmpty(map.defaultFloor) && !floorIds.Contains(map.defaultFloor))
                errors.Add($"{map.id}: default floor '{map.defaultFloor}' is missing.");
            foreach (MapZoneDefinition zone in zones.Values)
                if (string.IsNullOrWhiteSpace(zone.floor) || !floorIds.Contains(zone.floor))
                    errors.Add($"{map.id}: zone '{zone.id}' has an invalid floor.");

            int floorTransitions = 0;
            foreach (MapRouteDefinition route in map.routes ?? Array.Empty<MapRouteDefinition>())
            {
                string[] points = route?.waypoints;
                if (points == null || points.Length == 0)
                    points = new[] { route?.from, route?.to };
                for (int i = 0; i < points.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(points[i]) || !zones.ContainsKey(points[i]))
                        errors.Add($"{map.id}: route '{route?.id}' references unknown zone '{points[i]}'.");
                    if (i > 0 && zones.TryGetValue(points[i - 1], out MapZoneDefinition from) &&
                        zones.TryGetValue(points[i], out MapZoneDefinition to) &&
                        !string.Equals(from.floor, to.floor, StringComparison.OrdinalIgnoreCase))
                        floorTransitions++;
                }
            }

            if (floorIds.Count > 1 && floorTransitions < 2)
                errors.Add($"{map.id}: multi-floor map needs at least two explicit floor connections.");

            MapZoneDefinition start = zones.Values.FirstOrDefault(x => !x.navigationOnly);
            if (start == null)
                return;
            var graph = new MapZoneGraph(map);
            foreach (MapZoneDefinition zone in zones.Values.Where(x => !x.navigationOnly))
                if (graph.FindPath(start.id, zone.id).Count == 0)
                    errors.Add($"{map.id}: zone '{zone.id}' is disconnected from '{start.id}'.");
        }

        private static void ValidateTacticalCards(ContentDatabaseAsset asset, ICollection<string> errors)
        {
            if (asset.TacticalCards.Count == 0)
                return; // Legacy tests/content can still parse without the optional file.

            int teamsWithScopedCards = asset.Teams.Count(team =>
                asset.TacticalCards.Any(x =>
                    string.Equals(x.teamScope, team.id, StringComparison.OrdinalIgnoreCase)));
            int expectedCards = 6 + teamsWithScopedCards * 6;
            if (asset.TacticalCards.Count != expectedCards)
                errors.Add(
                    $"Expected {expectedCards} tactical cards " +
                    $"(6 universal + 6×{teamsWithScopedCards} scoped), found {asset.TacticalCards.Count}.");

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> zones = new HashSet<string>(
                (asset.Dust2?.zones ?? Array.Empty<MapZoneDefinition>()).Select(x => x.id),
                StringComparer.OrdinalIgnoreCase);
            foreach (TacticalCardDefinition card in asset.TacticalCards)
            {
                if (string.IsNullOrWhiteSpace(card.id) || !ids.Add(card.id))
                    errors.Add($"Tactical card ID is missing or duplicated: '{card.id}'.");
                if (card.commandCost < 0 || card.commandCost > 3)
                    errors.Add($"{card.id}: commandCost must be 0..3.");
                if (card.initiativeBias < 0 || card.initiativeBias > 2)
                    errors.Add($"{card.id}: initiativeBias must be 0..2.");
                if (card.risk < 0f || card.risk > 1f ||
                    card.openingBonus < 0f || card.openingBonus > 0.30f ||
                    card.firepowerBonus < 0f || card.firepowerBonus > 0.20f ||
                    card.teamworkBonus < 0f || card.teamworkBonus > 0.15f ||
                    card.clutchBonus < 0f || card.clutchBonus > 0.15f)
                    errors.Add($"{card.id}: tactical bonus is outside its allowed range.");
                if (!zones.Contains(card.primaryZoneId))
                    errors.Add($"{card.id}: unknown primary zone '{card.primaryZoneId}'.");
                if (card.teamScope != "universal" &&
                    !asset.Teams.Any(x => string.Equals(x.id, card.teamScope, StringComparison.OrdinalIgnoreCase)))
                    errors.Add($"{card.id}: unknown team scope '{card.teamScope}'.");
                if (!string.IsNullOrEmpty(card.targetPlayerId) &&
                    !asset.Players.Any(x => string.Equals(x.id, card.targetPlayerId, StringComparison.OrdinalIgnoreCase)))
                    errors.Add($"{card.id}: unknown target player '{card.targetPlayerId}'.");
            }

            if (asset.TacticalCards.Count(x => x.teamScope == "universal") != 6)
                errors.Add("Expected exactly six universal tactical cards.");
            foreach (TeamDefinition team in asset.Teams)
            {
                int scoped = asset.TacticalCards.Count(x =>
                    string.Equals(x.teamScope, team.id, StringComparison.OrdinalIgnoreCase));
                if (scoped == 0)
                    continue; // Auto-expanded teams may not have unique tactical cards yet.
                if (scoped != 6)
                    errors.Add($"Expected exactly six tactical cards for {team.id}, found {scoped}.");
            }
        }

        private static void ValidateFocusCards(ContentDatabaseAsset asset, ICollection<string> errors)
        {
            if (asset.FocusCards.Count == 0)
                return;
            if (asset.FocusCards.Count != 8)
                errors.Add($"Expected 8 focus cards, found {asset.FocusCards.Count}.");

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var allowedTargetModes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { "auto", "manual", "duo", "team" };
            var allowedDurations = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { "round", "cross_round" };
            foreach (FocusCardDefinition card in asset.FocusCards)
            {
                if (string.IsNullOrWhiteSpace(card.id) || !ids.Add(card.id))
                    errors.Add($"Focus card ID is missing or duplicated: '{card.id}'.");
                if (card.commandCost < 1 || card.commandCost > 3)
                    errors.Add($"{card.id}: focus commandCost must be 1..3.");
                if (!allowedTargetModes.Contains(card.targetMode ?? string.Empty))
                    errors.Add($"{card.id}: unknown targetMode '{card.targetMode}'.");
                if (!allowedDurations.Contains(card.duration ?? string.Empty))
                    errors.Add($"{card.id}: unknown duration '{card.duration}'.");
                if (card.allAttributeBonus < 0f || card.allAttributeBonus > 0.30f)
                    errors.Add($"{card.id}: allAttributeBonus must be 0..0.30.");
                if (card.teamScope != "universal" &&
                    !asset.Teams.Any(x => string.Equals(x.id, card.teamScope, StringComparison.OrdinalIgnoreCase)))
                    errors.Add($"{card.id}: unknown team scope '{card.teamScope}'.");
                foreach (string playerId in card.targetPlayerIds ?? Array.Empty<string>())
                    if (!asset.Players.Any(x => string.Equals(x.id, playerId, StringComparison.OrdinalIgnoreCase)))
                        errors.Add($"{card.id}: unknown target player '{playerId}'.");
                foreach (string tacticalId in card.synergyTacticalCardIds ?? Array.Empty<string>())
                    if (asset.TacticalCards.Count > 0 &&
                        !asset.TacticalCards.Any(x => string.Equals(x.id, tacticalId, StringComparison.OrdinalIgnoreCase)))
                        errors.Add($"{card.id}: unknown synergy tactical card '{tacticalId}'.");
            }

            if (asset.FocusCards.Count > 0)
            {
                if (asset.FocusCards.Count(x => x.teamScope == "navi_2021") != 4)
                    errors.Add("Expected exactly four NAVI focus cards.");
                if (asset.FocusCards.Count(x => x.teamScope == "gambit_2021") != 4)
                    errors.Add("Expected exactly four Gambit focus cards.");
            }
        }

        private static void ValidateCoachCards(ContentDatabaseAsset asset, ICollection<string> errors)
        {
            if (asset.CoachCards.Count == 0)
                return;
            if (asset.CoachCards.Count != 9)
                errors.Add($"Expected 9 coach cards, found {asset.CoachCards.Count}.");

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (CoachCardDefinition card in asset.CoachCards)
            {
                if (string.IsNullOrWhiteSpace(card.id) || !ids.Add(card.id))
                    errors.Add($"Coach card ID is missing or duplicated: '{card.id}'.");
                if (card.commandCost < 0 || card.commandCost > 4)
                    errors.Add($"{card.id}: coach commandCost must be 0..4.");
                if (!string.Equals(card.timing, "timeout", StringComparison.OrdinalIgnoreCase))
                    errors.Add($"{card.id}: coach timing must be 'timeout'.");
                if (card.isBaseCoach && card.commandCost != 0)
                    errors.Add($"{card.id}: base coach cards must cost 0.");
                if (!card.isBaseCoach && card.commandCost < 2)
                    errors.Add($"{card.id}: paid coach cards should cost at least 2.");
                if (card.teamScope != "universal" &&
                    !asset.Teams.Any(x => string.Equals(x.id, card.teamScope, StringComparison.OrdinalIgnoreCase)))
                    errors.Add($"{card.id}: unknown team scope '{card.teamScope}'.");
            }

            if (asset.CoachCards.Count > 0)
            {
                if (asset.CoachCards.Count(x => x.isBaseCoach) != 3)
                    errors.Add("Expected exactly three base coach cards.");
                if (asset.CoachCards.Count(x => x.teamScope == "universal" && !x.isBaseCoach) != 4)
                    errors.Add("Expected exactly four paid universal coach cards.");
                if (asset.CoachCards.Count(x => x.teamScope == "navi_2021") != 1)
                    errors.Add("Expected exactly one NAVI exclusive coach card.");
                if (asset.CoachCards.Count(x => x.teamScope == "gambit_2021") != 1)
                    errors.Add("Expected exactly one Gambit exclusive coach card.");
            }
        }

        private static void ValidateAttributes(PlayerDefinition player, ICollection<string> errors)
        {
            foreach (string key in new[] { "aim", "reaction", "awareness", "utility", "teamwork", "communication", "opening", "clutch" })
            {
                int value = player.attributes.Get(key);
                if (value < 0 || value > 100)
                    errors.Add($"{player.id}: {key}={value} is outside 0..100.");
            }
        }
    }

    public static class RuntimeContentLoader
    {
        public static ContentDatabase Load()
        {
            ContentDatabaseAsset asset = Resources.Load<ContentDatabaseAsset>("Generated/ContentDatabase");
            if (asset == null)
                throw new InvalidOperationException(
                    "Generated content database not found. Run Tactica Universalis/Import JSON Content.");
            if (asset.Train == null || asset.Train.zones == null || asset.Train.zones.Length == 0)
            {
                MapDefinition train = TryLoadMapFromDisk("train.json");
                if (train != null)
                    asset.SetTrain(train);
            }

            if (asset.Ancient == null || asset.Ancient.zones == null || asset.Ancient.zones.Length == 0)
            {
                MapDefinition ancient = TryLoadMapFromDisk("ancient.json");
                if (ancient != null)
                    asset.SetAncient(ancient);
            }

            if (asset.Overpass == null || asset.Overpass.zones == null || asset.Overpass.zones.Length == 0)
            {
                MapDefinition overpass = TryLoadMapFromDisk("overpass.json");
                if (overpass != null)
                    asset.SetOverpass(overpass);
            }

            if (asset.Vertigo == null || asset.Vertigo.zones == null || asset.Vertigo.zones.Length == 0)
            {
                MapDefinition vertigo = TryLoadMapFromDisk("vertigo.json");
                if (vertigo != null)
                    asset.SetVertigo(vertigo);
            }

            if (asset.Inferno == null || asset.Inferno.zones == null || asset.Inferno.zones.Length == 0)
            {
                MapDefinition inferno = TryLoadMapFromDisk("inferno.json");
                if (inferno != null)
                    asset.SetInferno(inferno);
            }

            if (asset.Mirage == null || asset.Mirage.zones == null || asset.Mirage.zones.Length == 0)
            {
                MapDefinition mirage = TryLoadMapFromDisk("mirage.json");
                if (mirage != null)
                    asset.SetMirage(mirage);
            }

            if (asset.Nuke == null || asset.Nuke.zones == null || asset.Nuke.zones.Length == 0)
            {
                MapDefinition nuke = TryLoadMapFromDisk("nuke.json");
                if (nuke != null)
                    asset.SetNuke(nuke);
            }

            IReadOnlyList<string> errors = ContentValidator.Validate(asset);
            if (errors.Count > 0)
                throw new InvalidOperationException("Content validation failed:\n" + string.Join("\n", errors));
            UtilityLineupRules.SetShared(LoadUtilityLineups());
            return asset.BuildDomain();
        }

        public static UtilityLineupCatalog LoadUtilityLineups()
        {
            try
            {
                string path = Path.Combine(
                    UnityEngine.Application.dataPath, "_Project", "Data", "JSON", "utility_lineups.json");
                if (!File.Exists(path))
                    return UtilityLineupCatalog.FromEntries(UtilityLineupRules.CreateDefaultDust2Entries());
                return ParseUtilityLineupsJson(File.ReadAllText(path));
            }
            catch
            {
                return UtilityLineupCatalog.FromEntries(UtilityLineupRules.CreateDefaultDust2Entries());
            }
        }

        public static UtilityLineupCatalog ParseUtilityLineupsJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return UtilityLineupCatalog.FromEntries(Array.Empty<UtilityLineupEntry>());
            UtilityLineupJsonRoot root = JsonUtility.FromJson<UtilityLineupJsonRoot>(json);
            return UtilityLineupCatalog.FromEntries(root?.lineups ?? Array.Empty<UtilityLineupEntry>());
        }

        public static WeaponCatalog LoadWeaponCatalog()
        {
            try
            {
                string path = Path.Combine(
                    UnityEngine.Application.dataPath, "_Project", "Data", "JSON", "weapons_2021.json");
                if (!File.Exists(path))
                    return WeaponCatalog.CreateDefault();
                Weapons2021JsonRoot root = JsonUtility.FromJson<Weapons2021JsonRoot>(File.ReadAllText(path));
                return WeaponCatalog.FromRoot(root);
            }
            catch
            {
                return WeaponCatalog.CreateDefault();
            }
        }

        public static MapDefinition TryLoadMapFromDisk(string fileName)
        {
            try
            {
                string path = Path.Combine(UnityEngine.Application.dataPath, "_Project", "Data", "JSON", fileName);
                if (!File.Exists(path))
                    return null;
                MapJsonRoot root = JsonUtility.FromJson<MapJsonRoot>(File.ReadAllText(path));
                return root?.map;
            }
            catch
            {
                return null;
            }
        }
    }
}
