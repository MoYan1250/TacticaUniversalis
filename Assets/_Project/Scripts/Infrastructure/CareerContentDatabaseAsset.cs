using System;

using System.Collections.Generic;

using TacticaUniversalis.Domain;

using UnityEngine;



namespace TacticaUniversalis.Infrastructure

{

    public sealed class CareerContentDatabaseAsset : ScriptableObject

    {

        [SerializeField] private CareerBalanceDefinition balance = new CareerBalanceDefinition();

        [SerializeField] private CareerPlayerDefinition[] players = Array.Empty<CareerPlayerDefinition>();

        [SerializeField] private CareerClubDefinition[] clubs = Array.Empty<CareerClubDefinition>();

        [SerializeField] private SponsorDefinition[] sponsors = Array.Empty<SponsorDefinition>();

        [SerializeField] private CompetitionDefinition[] competitions = Array.Empty<CompetitionDefinition>();

        [SerializeField] private FacilityDefinition[] facilities = Array.Empty<FacilityDefinition>();

        [SerializeField] private CareerScriptedEventDef[] scriptedEvents = Array.Empty<CareerScriptedEventDef>();

        [SerializeField] private CareerAchievementDef[] achievements = Array.Empty<CareerAchievementDef>();

        [SerializeField] private CareerCoachDefinition[] coaches = Array.Empty<CareerCoachDefinition>();

        [SerializeField] private CareerMissionTreeDef[] missionTrees = Array.Empty<CareerMissionTreeDef>();



        public CareerBalanceDefinition Balance => balance;

        public IReadOnlyList<CareerPlayerDefinition> Players => players;

        public IReadOnlyList<CareerClubDefinition> Clubs => clubs;

        public IReadOnlyList<SponsorDefinition> Sponsors => sponsors;

        public IReadOnlyList<CompetitionDefinition> Competitions => competitions;

        public IReadOnlyList<FacilityDefinition> Facilities => facilities;

        public IReadOnlyList<CareerScriptedEventDef> ScriptedEvents => scriptedEvents;

        public IReadOnlyList<CareerAchievementDef> Achievements => achievements;

        public IReadOnlyList<CareerCoachDefinition> Coaches => coaches;

        public IReadOnlyList<CareerMissionTreeDef> MissionTrees => missionTrees;



        public void Replace(

            CareerBalanceDefinition newBalance,

            CareerPlayerDefinition[] newPlayers,

            CareerClubDefinition[] newClubs,

            SponsorDefinition[] newSponsors,

            CompetitionDefinition[] newCompetitions,

            FacilityDefinition[] newFacilities,

            CareerScriptedEventDef[] newScriptedEvents = null,

            CareerAchievementDef[] newAchievements = null,

            CareerCoachDefinition[] newCoaches = null,

            CareerMissionTreeDef[] newMissionTrees = null)

        {

            balance = newBalance ?? new CareerBalanceDefinition();

            players = newPlayers ?? Array.Empty<CareerPlayerDefinition>();

            clubs = newClubs ?? Array.Empty<CareerClubDefinition>();

            sponsors = newSponsors ?? Array.Empty<SponsorDefinition>();

            competitions = newCompetitions ?? Array.Empty<CompetitionDefinition>();

            facilities = newFacilities ?? Array.Empty<FacilityDefinition>();

            scriptedEvents = newScriptedEvents ?? Array.Empty<CareerScriptedEventDef>();

            achievements = newAchievements ?? Array.Empty<CareerAchievementDef>();

            coaches = newCoaches ?? Array.Empty<CareerCoachDefinition>();

            missionTrees = newMissionTrees ?? Array.Empty<CareerMissionTreeDef>();

        }



        public CareerContent BuildDomain() =>

            new CareerContent(balance, players, clubs, sponsors, competitions, facilities,

                scriptedEvents, achievements, coaches, missionTrees);

    }

}


