using System;
using System.IO;
using TacticaUniversalis.Domain;
using UnityEngine;

namespace TacticaUniversalis.Infrastructure
{
    public sealed class CareerSaveSlotInfo
    {
        public int Slot;
        public bool Exists;
        public string PlayerClubId;
        public CareerDifficulty Difficulty;
        public int ClockHour;
        public int Year;
        public string SaveId;
    }

    public sealed class CareerPersistence
    {
        private const string SaveFolder = "CareerSaves";

        /// <summary>Ironman / hardcore auto-save file.</summary>
        public const int IronmanSlot = 0;

        /// <summary>First selectable manual slot.</summary>
        public const int FirstManualSlot = 1;

        /// <summary>Number of player-facing save slots currently open.</summary>
        public const int ManualSlotCount = 4;

        /// <summary>Last selectable manual slot inclusive.</summary>
        public const int LastManualSlot = FirstManualSlot + ManualSlotCount - 1;

        public string RootPath { get; }

        public CareerPersistence(string rootPath = null)
        {
            RootPath = string.IsNullOrWhiteSpace(rootPath)
                ? Path.Combine(UnityEngine.Application.persistentDataPath, SaveFolder)
                : rootPath;
        }

        public static bool IsManualSlot(int slot) =>
            slot >= FirstManualSlot && slot <= LastManualSlot;

        public string Save(CareerState state, int slot = 1)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (state.difficulty == CareerDifficulty.Hardcore) slot = IronmanSlot;
            ValidateSlot(slot);
            Directory.CreateDirectory(RootPath);
            string path = PathFor(slot);
            string temporary = path + ".tmp";
            string backup = path + ".bak";
            File.WriteAllText(temporary, JsonUtility.ToJson(state, true));
            if (File.Exists(path))
            {
                if (File.Exists(backup)) File.Delete(backup);
                File.Replace(temporary, path, backup);
                if (File.Exists(backup)) File.Delete(backup);
            }
            else File.Move(temporary, path);
            return path;
        }

        public CareerState Load(int slot = 1, bool allowHardcore = false)
        {
            ValidateSlot(slot);
            if (slot == IronmanSlot && !allowHardcore)
                throw new InvalidOperationException("Ironman saves cannot be manually loaded in-game.");
            string path = PathFor(slot);
            if (!File.Exists(path)) return null;
            CareerState state = JsonUtility.FromJson<CareerState>(File.ReadAllText(path));
            if (state == null) throw new InvalidDataException("Career save could not be parsed.");
            if (state.schemaVersion != 1)
                throw new InvalidDataException($"Unsupported career save schemaVersion {state.schemaVersion}.");
            Normalize(state);
            if (state.clockHour < 0)
                state.clockHour = 0;
            return state;
        }

        public bool Exists(int slot = 1)
        {
            ValidateSlot(slot);
            return File.Exists(PathFor(slot));
        }

        /// <summary>Deletes a manual slot save. Ironman slot cannot be deleted from UI.</summary>
        public bool Delete(int slot)
        {
            if (!IsManualSlot(slot))
                throw new ArgumentOutOfRangeException(nameof(slot), "Only manual slots can be deleted.");
            string path = PathFor(slot);
            bool removed = false;
            if (File.Exists(path))
            {
                File.Delete(path);
                removed = true;
            }

            string backup = path + ".bak";
            if (File.Exists(backup))
            {
                File.Delete(backup);
                removed = true;
            }

            string temporary = path + ".tmp";
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
                removed = true;
            }

            return removed;
        }

        public string PathFor(int slot)
        {
            ValidateSlot(slot);
            return Path.Combine(RootPath, slot == IronmanSlot ? "ironman.json" : $"career_{slot}.json");
        }

        /// <summary>Lightweight peek for slot UI; returns Exists=false when empty or unreadable.</summary>
        public CareerSaveSlotInfo PeekSlot(int slot)
        {
            ValidateSlot(slot);
            var info = new CareerSaveSlotInfo { Slot = slot, Exists = false };
            string path = PathFor(slot);
            if (!File.Exists(path))
                return info;

            try
            {
                CareerState state = JsonUtility.FromJson<CareerState>(File.ReadAllText(path));
                if (state == null)
                    return info;
                info.Exists = true;
                info.PlayerClubId = state.playerClubId;
                info.Difficulty = state.difficulty;
                info.ClockHour = Math.Max(0, state.clockHour);
                info.Year = state.year;
                info.SaveId = state.saveId;
            }
            catch
            {
                info.Exists = false;
            }

            return info;
        }

        private static void ValidateSlot(int slot)
        {
            if (slot < IronmanSlot || slot > LastManualSlot)
                throw new ArgumentOutOfRangeException(
                    nameof(slot),
                    $"Save slot must be {IronmanSlot}..{LastManualSlot}.");
        }

        private static void Normalize(CareerState state)
        {
            state.clubs = state.clubs ?? new System.Collections.Generic.List<ClubState>();
            state.players = state.players ?? new System.Collections.Generic.List<PlayerCareerState>();
            state.contracts = state.contracts ?? new System.Collections.Generic.List<ContractState>();
            state.sponsors = state.sponsors ?? new System.Collections.Generic.List<SponsorContract>();
            state.offers = state.offers ?? new System.Collections.Generic.List<TransferOffer>();
            foreach (TransferOffer offer in state.offers)
            {
                if (offer == null) continue;
                if (offer.contractMonths < 0) offer.contractMonths = 0;
                if (offer.negotiationRound < 0) offer.negotiationRound = 0;
                if (offer.phaseExpiresHour < 0) offer.phaseExpiresHour = 0;
            }
            state.competitionEntries = state.competitionEntries ??
                new System.Collections.Generic.List<CompetitionEntry>();
            state.competitionSignupBans = state.competitionSignupBans ??
                new System.Collections.Generic.List<CompetitionSignupBan>();
            state.rankingBatches = state.rankingBatches ??
                new System.Collections.Generic.List<RankingBatch>();
            state.rankings = state.rankings ?? new System.Collections.Generic.List<RankingEntry>();
            state.rmrBatches = state.rmrBatches ??
                new System.Collections.Generic.List<RmrPointBatch>();
            state.rmrRankings = state.rmrRankings ?? new System.Collections.Generic.List<RmrEntry>();
            state.ledger = state.ledger ?? new System.Collections.Generic.List<LedgerEntry>();
            state.events = state.events ?? new System.Collections.Generic.List<CareerEvent>();
            state.competitionBrackets = state.competitionBrackets ??
                new System.Collections.Generic.List<CompetitionBracketArchive>();
            state.queuedInteractiveCompetitionIds = state.queuedInteractiveCompetitionIds ??
                new System.Collections.Generic.List<string>();
            state.flags = state.flags ?? new System.Collections.Generic.List<CareerFlag>();
            state.pendingEvents = state.pendingEvents ??
                new System.Collections.Generic.List<CareerPendingEvent>();
            state.unlockedAchievements = state.unlockedAchievements ??
                new System.Collections.Generic.List<string>();
            CareerMissionRules.EnsureProgress(state);
            if (state.playerOfficialMatchesPlayed < 0)
                state.playerOfficialMatchesPlayed = 0;
            state.communityPosts = state.communityPosts ??
                new System.Collections.Generic.List<CommunityPostLogEntry>();
            state.coaches = state.coaches ?? new System.Collections.Generic.List<CoachStaffState>();
            foreach (CoachStaffState coach in state.coaches)
            {
                if (coach == null)
                    continue;
                coach.opponentIntel = coach.opponentIntel ??
                    new System.Collections.Generic.List<CoachOpponentIntelEntry>();
            }
            foreach (ClubState club in state.clubs)
                club.facilities = club.facilities ?? new System.Collections.Generic.List<FacilityState>();
            foreach (ClubState club in state.clubs)
                ClubEstateSatisfactionRules.EnsureNormalized(club);
            foreach (PlayerCareerState player in state.players)
            {
                player.attributes = player.attributes ?? new System.Collections.Generic.List<CareerNamedStat>();
                player.roleProficiencies = player.roleProficiencies ??
                    new System.Collections.Generic.List<CareerNamedStat>();
                player.mapProficiencies = player.mapProficiencies ??
                    new System.Collections.Generic.List<CareerNamedStat>();
                if (string.IsNullOrWhiteSpace(player.squadStatus))
                    player.squadStatus = ClubEstateRules.SquadStarter;
                else
                    player.squadStatus = ClubEstateRules.NormalizeSquadStatus(player.squadStatus);
                if (player.pressure <= 0)
                    player.pressure = 40;
                DevelopmentRules.EnsureProgressLists(player, null);
            }
            foreach (CompetitionBracketArchive archive in state.competitionBrackets)
            {
                archive.seedOrder = archive.seedOrder ?? new System.Collections.Generic.List<string>();
                archive.matches = archive.matches ??
                    new System.Collections.Generic.List<CompetitionBracketMatchRecord>();
            }

            if (state.competitionRun != null)
            {
                if (state.competitionRun.mapSeries == null)
                    state.competitionRun.mapSeries = new CareerMapSeriesState();
                MapVetoRules.EnsureLists(state.competitionRun.mapSeries);
            }
        }
    }
}
