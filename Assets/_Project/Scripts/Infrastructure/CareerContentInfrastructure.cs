using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TacticaUniversalis.Domain;
using UnityEngine;

namespace TacticaUniversalis.Infrastructure
{
    [Serializable] public sealed class CareerBalanceJsonRoot { public CareerBalanceDefinition balance; }
    [Serializable] public sealed class CareerPlayersJsonRoot { public int schemaVersion; public CareerPlayerDefinition[] players; }
    [Serializable] public sealed class CareerClubsJsonRoot { public int schemaVersion; public CareerClubDefinition[] clubs; }
    [Serializable] public sealed class SponsorsJsonRoot { public int schemaVersion; public SponsorDefinition[] sponsors; }
    [Serializable] public sealed class CompetitionsJsonRoot { public int schemaVersion; public CompetitionDefinition[] competitions; }
    [Serializable] public sealed class FacilitiesJsonRoot { public int schemaVersion; public FacilityDefinition[] facilities; }
    [Serializable] public sealed class CareerEventsJsonRoot { public int schemaVersion; public CareerScriptedEventDef[] events; }
    [Serializable] public sealed class CareerAchievementsJsonRoot { public int schemaVersion; public CareerAchievementDef[] achievements; }
    [Serializable] public sealed class CareerCoachesJsonRoot { public int schemaVersion; public CareerCoachDefinition[] coaches; }
    [Serializable] public sealed class CareerMissionsJsonRoot { public int schemaVersion; public CareerMissionTreeDef[] trees; }

    public sealed class CareerContent
    {
        public CareerBalanceDefinition Balance { get; }
        public IReadOnlyDictionary<string, CareerPlayerDefinition> Players { get; }
        public IReadOnlyDictionary<string, CareerClubDefinition> Clubs { get; }
        public IReadOnlyDictionary<string, SponsorDefinition> Sponsors { get; }
        public IReadOnlyDictionary<string, CompetitionDefinition> Competitions { get; }
        public IReadOnlyDictionary<FacilityType, FacilityDefinition> Facilities { get; }
        public IReadOnlyDictionary<string, CareerScriptedEventDef> ScriptedEvents { get; }
        public IReadOnlyDictionary<string, CareerAchievementDef> Achievements { get; }
        public IReadOnlyDictionary<string, CareerCoachDefinition> Coaches { get; }
        public IReadOnlyDictionary<string, CareerMissionTreeDef> MissionTrees { get; }

        public CareerContent(
            CareerBalanceDefinition balance,
            IEnumerable<CareerPlayerDefinition> players,
            IEnumerable<CareerClubDefinition> clubs,
            IEnumerable<SponsorDefinition> sponsors,
            IEnumerable<CompetitionDefinition> competitions,
            IEnumerable<FacilityDefinition> facilities,
            IEnumerable<CareerScriptedEventDef> scriptedEvents = null,
            IEnumerable<CareerAchievementDef> achievements = null,
            IEnumerable<CareerCoachDefinition> coaches = null,
            IEnumerable<CareerMissionTreeDef> missionTrees = null)
        {
            Balance = balance;
            Players = players.ToDictionary(x => x.id, StringComparer.Ordinal);
            Clubs = clubs.ToDictionary(x => x.id, StringComparer.OrdinalIgnoreCase);
            Sponsors = sponsors.ToDictionary(x => x.id, StringComparer.OrdinalIgnoreCase);
            Competitions = competitions.ToDictionary(x => x.id, StringComparer.OrdinalIgnoreCase);
            Facilities = facilities.ToDictionary(x => x.type);
            ScriptedEvents = (scriptedEvents ?? Array.Empty<CareerScriptedEventDef>())
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.id))
                .ToDictionary(x => x.id, StringComparer.OrdinalIgnoreCase);
            Achievements = (achievements ?? Array.Empty<CareerAchievementDef>())
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.id))
                .ToDictionary(x => x.id, StringComparer.OrdinalIgnoreCase);
            Coaches = (coaches ?? Array.Empty<CareerCoachDefinition>())
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.id))
                .ToDictionary(x => x.id, StringComparer.OrdinalIgnoreCase);
            MissionTrees = (missionTrees ?? Array.Empty<CareerMissionTreeDef>())
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.id))
                .ToDictionary(x => x.id, StringComparer.OrdinalIgnoreCase);
        }
    }

    public static class CareerContentJsonParser
    {
        public static CareerContentDatabaseAsset ParseInto(
            CareerContentDatabaseAsset asset,
            string balanceJson, string playersJson, string clubsJson,
            string sponsorsJson, string competitionsJson, string facilitiesJson,
            string eventsJson = null, string achievementsJson = null, string coachesJson = null,
            string missionsJson = null)
        {
            CareerBalanceJsonRoot balance = JsonUtility.FromJson<CareerBalanceJsonRoot>(balanceJson);
            CareerPlayersJsonRoot players = JsonUtility.FromJson<CareerPlayersJsonRoot>(playersJson);
            CareerClubsJsonRoot clubs = JsonUtility.FromJson<CareerClubsJsonRoot>(clubsJson);
            SponsorsJsonRoot sponsors = JsonUtility.FromJson<SponsorsJsonRoot>(sponsorsJson);
            CompetitionsJsonRoot competitions = JsonUtility.FromJson<CompetitionsJsonRoot>(competitionsJson);
            FacilitiesJsonRoot facilities = JsonUtility.FromJson<FacilitiesJsonRoot>(facilitiesJson);

            CareerScriptedEventDef[] events = Array.Empty<CareerScriptedEventDef>();
            CareerAchievementDef[] achievements = Array.Empty<CareerAchievementDef>();
            CareerCoachDefinition[] coaches = Array.Empty<CareerCoachDefinition>();
            CareerMissionTreeDef[] missions = Array.Empty<CareerMissionTreeDef>();
            if (!string.IsNullOrWhiteSpace(eventsJson))
            {
                CareerEventsJsonRoot root = JsonUtility.FromJson<CareerEventsJsonRoot>(eventsJson);
                events = root?.events ?? Array.Empty<CareerScriptedEventDef>();
            }
            if (!string.IsNullOrWhiteSpace(achievementsJson))
            {
                CareerAchievementsJsonRoot root = JsonUtility.FromJson<CareerAchievementsJsonRoot>(achievementsJson);
                achievements = root?.achievements ?? Array.Empty<CareerAchievementDef>();
            }
            if (!string.IsNullOrWhiteSpace(coachesJson))
            {
                CareerCoachesJsonRoot root = JsonUtility.FromJson<CareerCoachesJsonRoot>(coachesJson);
                coaches = root?.coaches ?? Array.Empty<CareerCoachDefinition>();
            }
            if (!string.IsNullOrWhiteSpace(missionsJson))
            {
                CareerMissionsJsonRoot root = JsonUtility.FromJson<CareerMissionsJsonRoot>(missionsJson);
                missions = root?.trees ?? Array.Empty<CareerMissionTreeDef>();
            }

            asset.Replace(balance.balance, players.players, clubs.clubs, sponsors.sponsors,
                competitions.competitions, facilities.facilities, events, achievements, coaches, missions);
            return asset;
        }
    }

    public static class CareerContentValidator
    {
        public static IReadOnlyList<string> Validate(CareerContentDatabaseAsset asset)
        {
            var errors = new List<string>();
            if (asset == null) return new[] { "Career content asset is null." };
            if (asset.Balance == null || asset.Balance.schemaVersion != 1)
                errors.Add("Career balance schemaVersion must be 1.");
            if (asset.Clubs.Count < 8) errors.Add("Career demo requires at least 8 clubs.");
            if (asset.Players.Count < 40) errors.Add("Career demo requires at least 40 players.");
            if (asset.Sponsors.Select(x => x.tier).Distinct().Count() < 4)
                errors.Add("Career demo requires all four sponsor tiers.");
            if (asset.Facilities.Select(x => x.type).Distinct().Count() != 4)
                errors.Add("Career demo requires four unique facility types.");
            if (asset.Competitions.Count < 6) errors.Add("Career demo requires at least 6 competitions.");

            var playerIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (CareerPlayerDefinition player in asset.Players)
            {
                if (string.IsNullOrWhiteSpace(player.id) || !playerIds.Add(player.id))
                    errors.Add($"Career player id is missing or duplicated: '{player.id}'.");
                if (player.power < 0 || player.power > 100 ||
                    player.potential < 0 || player.potential > 100)
                    errors.Add($"{player.id}: power and potential must be 0..100.");
            }

            var clubIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (CareerClubDefinition club in asset.Clubs)
            {
                if (string.IsNullOrWhiteSpace(club.id) || !clubIds.Add(club.id))
                    errors.Add($"Career club id is missing or duplicated: '{club.id}'.");
                if (club.playerIds == null || club.playerIds.Length != 5)
                    errors.Add($"{club.id}: expected exactly five roster players.");
                foreach (string id in club.playerIds ?? Array.Empty<string>())
                    if (!playerIds.Contains(id)) errors.Add($"{club.id}: unknown player '{id}'.");
            }
            foreach (CareerPlayerDefinition player in asset.Players)
                if (!string.IsNullOrEmpty(player.clubId) && !clubIds.Contains(player.clubId))
                    errors.Add($"{player.id}: unknown club '{player.clubId}'.");
            return errors;
        }
    }

    public static class RuntimeCareerContentLoader
    {
        public static CareerContent Load()
        {
            CareerContentDatabaseAsset asset =
                Resources.Load<CareerContentDatabaseAsset>("Generated/CareerContentDatabase");
            if (asset == null)
                throw new InvalidOperationException(
                    "Generated career content not found. Run Tactica Universalis/Import JSON Content.");
            IReadOnlyList<string> errors = CareerContentValidator.Validate(asset);
            if (errors.Count > 0)
                throw new InvalidOperationException("Career content validation failed:\n" + string.Join("\n", errors));

            // Always merge Data/JSON content when present so career_events / players / clubs
            // edits apply without requiring a ContentImporter re-run.
            TryMergeScriptedJson(asset);

            return asset.BuildDomain();
        }

        private static void TryMergeScriptedJson(CareerContentDatabaseAsset asset)
        {
            try
            {
                string dataRoot = Path.Combine(UnityEngine.Application.dataPath, "_Project", "Data", "JSON");
                string eventsPath = Path.Combine(dataRoot, "career_events.json");
                string achievementsPath = Path.Combine(dataRoot, "career_achievements.json");
                string coachesPath = Path.Combine(dataRoot, "career_coaches.json");
                string missionsPath = Path.Combine(dataRoot, "career_missions.json");
                string playersPath = Path.Combine(dataRoot, "career_players.json");
                string clubsPath = Path.Combine(dataRoot, "career_clubs.json");
                string eventsJson = File.Exists(eventsPath) ? File.ReadAllText(eventsPath) : null;
                string achievementsJson = File.Exists(achievementsPath) ? File.ReadAllText(achievementsPath) : null;
                string coachesJson = File.Exists(coachesPath) ? File.ReadAllText(coachesPath) : null;
                string missionsJson = File.Exists(missionsPath) ? File.ReadAllText(missionsPath) : null;
                string playersJson = File.Exists(playersPath) ? File.ReadAllText(playersPath) : null;
                string clubsJson = File.Exists(clubsPath) ? File.ReadAllText(clubsPath) : null;
                if (string.IsNullOrWhiteSpace(eventsJson) &&
                    string.IsNullOrWhiteSpace(achievementsJson) &&
                    string.IsNullOrWhiteSpace(coachesJson) &&
                    string.IsNullOrWhiteSpace(missionsJson) &&
                    string.IsNullOrWhiteSpace(playersJson) &&
                    string.IsNullOrWhiteSpace(clubsJson))
                    return;

                CareerScriptedEventDef[] events = asset.ScriptedEvents?.ToArray() ?? Array.Empty<CareerScriptedEventDef>();
                CareerAchievementDef[] achievements = asset.Achievements?.ToArray() ?? Array.Empty<CareerAchievementDef>();
                CareerCoachDefinition[] coaches = asset.Coaches?.ToArray() ?? Array.Empty<CareerCoachDefinition>();
                CareerMissionTreeDef[] missions = asset.MissionTrees?.ToArray() ?? Array.Empty<CareerMissionTreeDef>();
                CareerPlayerDefinition[] players = asset.Players?.ToArray() ?? Array.Empty<CareerPlayerDefinition>();
                CareerClubDefinition[] clubs = asset.Clubs?.ToArray() ?? Array.Empty<CareerClubDefinition>();
                if (!string.IsNullOrWhiteSpace(eventsJson))
                {
                    CareerEventsJsonRoot root = JsonUtility.FromJson<CareerEventsJsonRoot>(eventsJson);
                    if (root?.events != null && root.events.Length > 0)
                        events = root.events;
                }
                if (!string.IsNullOrWhiteSpace(achievementsJson))
                {
                    CareerAchievementsJsonRoot root = JsonUtility.FromJson<CareerAchievementsJsonRoot>(achievementsJson);
                    if (root?.achievements != null && root.achievements.Length > 0)
                        achievements = root.achievements;
                }
                if (!string.IsNullOrWhiteSpace(coachesJson))
                {
                    CareerCoachesJsonRoot root = JsonUtility.FromJson<CareerCoachesJsonRoot>(coachesJson);
                    if (root?.coaches != null && root.coaches.Length > 0)
                        coaches = root.coaches;
                }
                if (!string.IsNullOrWhiteSpace(missionsJson))
                {
                    CareerMissionsJsonRoot root = JsonUtility.FromJson<CareerMissionsJsonRoot>(missionsJson);
                    if (root?.trees != null && root.trees.Length > 0)
                        missions = root.trees;
                }
                if (!string.IsNullOrWhiteSpace(playersJson))
                {
                    CareerPlayersJsonRoot root = JsonUtility.FromJson<CareerPlayersJsonRoot>(playersJson);
                    if (root?.players != null && root.players.Length > 0)
                        players = root.players;
                }
                if (!string.IsNullOrWhiteSpace(clubsJson))
                {
                    CareerClubsJsonRoot root = JsonUtility.FromJson<CareerClubsJsonRoot>(clubsJson);
                    if (root?.clubs != null && root.clubs.Length > 0)
                        clubs = root.clubs;
                }

                asset.Replace(
                    asset.Balance,
                    players,
                    clubs,
                    asset.Sponsors.ToArray(),
                    asset.Competitions.ToArray(),
                    asset.Facilities.ToArray(),
                    events,
                    achievements,
                    coaches,
                    missions);
            }
            catch
            {
                // Optional content; keep base career asset if JSON merge fails.
            }
        }
    }
}
