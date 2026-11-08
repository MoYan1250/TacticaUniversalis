using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TacticaUniversalis.Infrastructure.Editor
{
    public static class ContentImporter
    {
        private const string OutputFolder = "Assets/_Project/Resources/Generated";
        private const string OutputPath = OutputFolder + "/ContentDatabase.asset";
        private const string CareerOutputPath = OutputFolder + "/CareerContentDatabase.asset";

        [MenuItem("Tactica Universalis/Import JSON Content")]
        public static void Import()
        {
            string dataRoot = Path.Combine(UnityEngine.Application.dataPath, "_Project", "Data", "JSON");
            string players = Read(dataRoot, "players.json");
            string teams = Read(dataRoot, "teams.json");
            string traits = Read(dataRoot, "traits.json");
            string dust2 = Read(dataRoot, "dust2.json");
            string train = ReadOptional(dataRoot, "train.json");
            string ancient = ReadOptional(dataRoot, "ancient.json");
            string overpass = ReadOptional(dataRoot, "overpass.json");
            string vertigo = ReadOptional(dataRoot, "vertigo.json");
            string inferno = ReadOptional(dataRoot, "inferno.json");
            string mirage = ReadOptional(dataRoot, "mirage.json");
            string nuke = ReadOptional(dataRoot, "nuke.json");
            string tacticalCards = Read(dataRoot, "tactical_cards.json");
            string focusCards = Read(dataRoot, "focus_cards.json");
            string coachCards = Read(dataRoot, "coach_cards.json");

            EnsureFolder("Assets", "_Project");
            EnsureFolder("Assets/_Project", "Resources");
            EnsureFolder("Assets/_Project/Resources", "Generated");

            ContentDatabaseAsset asset = AssetDatabase.LoadAssetAtPath<ContentDatabaseAsset>(OutputPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ContentDatabaseAsset>();
                AssetDatabase.CreateAsset(asset, OutputPath);
            }

            ContentJsonParser.ParseInto(
                asset, players, teams, traits, dust2, tacticalCards, focusCards, coachCards,
                train, ancient, overpass, vertigo, inferno, mirage, nuke);
            var errors = ContentValidator.Validate(asset);
            if (errors.Count > 0)
                throw new InvalidDataException("Content import failed:\n" + string.Join("\n", errors));

            string utilityLineups = ReadOptional(dataRoot, "utility_lineups.json");
            if (!string.IsNullOrEmpty(utilityLineups))
                Domain.UtilityLineupRules.SetShared(
                    RuntimeContentLoader.ParseUtilityLineupsJson(utilityLineups));

            EditorUtility.SetDirty(asset);

            CareerContentDatabaseAsset careerAsset =
                AssetDatabase.LoadAssetAtPath<CareerContentDatabaseAsset>(CareerOutputPath);
            if (careerAsset == null)
            {
                careerAsset = ScriptableObject.CreateInstance<CareerContentDatabaseAsset>();
                AssetDatabase.CreateAsset(careerAsset, CareerOutputPath);
            }
            CareerContentJsonParser.ParseInto(
                careerAsset,
                Read(dataRoot, "career_balance.json"),
                Read(dataRoot, "career_players.json"),
                Read(dataRoot, "career_clubs.json"),
                Read(dataRoot, "sponsors.json"),
                Read(dataRoot, "competitions.json"),
                Read(dataRoot, "facilities.json"),
                ReadOptional(dataRoot, "career_events.json"),
                ReadOptional(dataRoot, "career_achievements.json"),
                ReadOptional(dataRoot, "career_coaches.json"),
                ReadOptional(dataRoot, "career_missions.json"));
            var careerErrors = CareerContentValidator.Validate(careerAsset);
            if (careerErrors.Count > 0)
                throw new InvalidDataException("Career content import failed:\n" + string.Join("\n", careerErrors));
            EditorUtility.SetDirty(careerAsset);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Imported {asset.Players.Count} players, {asset.Traits.Count} traits, " +
                      $"{asset.TacticalCards.Count} tactical / {asset.FocusCards.Count} focus / " +
                      $"{asset.CoachCards.Count} coach cards, Dust II" +
                      (asset.Train != null ? " + Train" : string.Empty) +
                      (asset.Ancient != null ? " + Ancient" : string.Empty) +
                      (asset.Overpass != null ? " + Overpass" : string.Empty) +
                      (asset.Vertigo != null ? " + Vertigo" : string.Empty) +
                      (asset.Inferno != null ? " + Inferno" : string.Empty) +
                      (asset.Mirage != null ? " + Mirage" : string.Empty) +
                      (asset.Nuke != null ? " + Nuke" : string.Empty) +
                      $" into {OutputPath}; " +
                      $"{careerAsset.Clubs.Count} career clubs, {careerAsset.Players.Count} career players, " +
                      $"{careerAsset.Coaches.Count} coaches, {careerAsset.ScriptedEvents.Count} events, " +
                      $"{careerAsset.Achievements.Count} achievements, {careerAsset.MissionTrees.Count} mission trees.");
            Selection.activeObject = asset;
        }

        [MenuItem("Tactica Universalis/Validate Generated Content")]
        public static void ValidateGenerated()
        {
            ContentDatabaseAsset asset = AssetDatabase.LoadAssetAtPath<ContentDatabaseAsset>(OutputPath);
            var errors = ContentValidator.Validate(asset);
            if (errors.Count > 0)
                throw new InvalidDataException(string.Join("\n", errors));
            CareerContentDatabaseAsset careerAsset =
                AssetDatabase.LoadAssetAtPath<CareerContentDatabaseAsset>(CareerOutputPath);
            var careerErrors = CareerContentValidator.Validate(careerAsset);
            if (careerErrors.Count > 0)
                throw new InvalidDataException(string.Join("\n", careerErrors));
            Debug.Log($"Content is valid: {asset.Players.Count} match players, " +
                      $"{careerAsset.Players.Count} career players, {careerAsset.Clubs.Count} career clubs.");
        }

        private static string Read(string root, string fileName)
        {
            string path = Path.Combine(root, fileName);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Canonical JSON file not found: {path}");
            return File.ReadAllText(path);
        }

        private static string ReadOptional(string root, string fileName)
        {
            string path = Path.Combine(root, fileName);
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}
