using System;
using System.Collections.Generic;
using TacticaUniversalis.Domain;
using UnityEngine;

namespace TacticaUniversalis.Infrastructure
{
    public sealed class ContentDatabaseAsset : ScriptableObject
    {
        [SerializeField] private int schemaVersion = 1;
        [SerializeField] private PlayerDefinition[] players = Array.Empty<PlayerDefinition>();
        [SerializeField] private TeamDefinition[] teams = Array.Empty<TeamDefinition>();
        [SerializeField] private TraitDefinition[] traits = Array.Empty<TraitDefinition>();
        [SerializeField] private TacticalCardDefinition[] tacticalCards = Array.Empty<TacticalCardDefinition>();
        [SerializeField] private FocusCardDefinition[] focusCards = Array.Empty<FocusCardDefinition>();
        [SerializeField] private CoachCardDefinition[] coachCards = Array.Empty<CoachCardDefinition>();
        [SerializeField] private MapDefinition dust2;
        [SerializeField] private MapDefinition train;
        [SerializeField] private MapDefinition ancient;
        [SerializeField] private MapDefinition overpass;
        [SerializeField] private MapDefinition vertigo;
        [SerializeField] private MapDefinition inferno;
        [SerializeField] private MapDefinition mirage;
        [SerializeField] private MapDefinition nuke;

        public int SchemaVersion => schemaVersion;
        public IReadOnlyList<PlayerDefinition> Players => players;
        public IReadOnlyList<TeamDefinition> Teams => teams;
        public IReadOnlyList<TraitDefinition> Traits => traits;
        public IReadOnlyList<TacticalCardDefinition> TacticalCards => tacticalCards;
        public IReadOnlyList<FocusCardDefinition> FocusCards => focusCards;
        public IReadOnlyList<CoachCardDefinition> CoachCards => coachCards;
        public MapDefinition Dust2 => dust2;
        public MapDefinition Train => train;
        public MapDefinition Ancient => ancient;
        public MapDefinition Overpass => overpass;
        public MapDefinition Vertigo => vertigo;
        public MapDefinition Inferno => inferno;
        public MapDefinition Mirage => mirage;
        public MapDefinition Nuke => nuke;

        public void Replace(
            int version,
            PlayerDefinition[] playerData,
            TeamDefinition[] teamData,
            TraitDefinition[] traitData,
            MapDefinition mapData,
            TacticalCardDefinition[] tacticalCardData = null,
            FocusCardDefinition[] focusCardData = null,
            CoachCardDefinition[] coachCardData = null,
            MapDefinition trainData = null,
            MapDefinition ancientData = null,
            MapDefinition overpassData = null,
            MapDefinition vertigoData = null,
            MapDefinition infernoData = null,
            MapDefinition mirageData = null,
            MapDefinition nukeData = null)
        {
            schemaVersion = version;
            players = playerData ?? Array.Empty<PlayerDefinition>();
            teams = teamData ?? Array.Empty<TeamDefinition>();
            traits = traitData ?? Array.Empty<TraitDefinition>();
            tacticalCards = tacticalCardData ?? Array.Empty<TacticalCardDefinition>();
            focusCards = focusCardData ?? Array.Empty<FocusCardDefinition>();
            coachCards = coachCardData ?? Array.Empty<CoachCardDefinition>();
            dust2 = mapData;
            if (trainData != null)
                train = trainData;
            if (ancientData != null)
                ancient = ancientData;
            if (overpassData != null)
                overpass = overpassData;
            if (vertigoData != null)
                vertigo = vertigoData;
            if (infernoData != null)
                inferno = infernoData;
            if (mirageData != null)
                mirage = mirageData;
            if (nukeData != null)
                nuke = nukeData;
        }

        public void SetTrain(MapDefinition trainData) => train = trainData;
        public void SetAncient(MapDefinition ancientData) => ancient = ancientData;
        public void SetOverpass(MapDefinition overpassData) => overpass = overpassData;
        public void SetVertigo(MapDefinition vertigoData) => vertigo = vertigoData;
        public void SetInferno(MapDefinition infernoData) => inferno = infernoData;
        public void SetMirage(MapDefinition mirageData) => mirage = mirageData;
        public void SetNuke(MapDefinition nukeData) => nuke = nukeData;

        public ContentDatabase BuildDomain() =>
            new ContentDatabase(
                players, teams, traits, dust2, tacticalCards, focusCards, coachCards,
                train, ancient, overpass, vertigo, inferno, mirage, nuke);
    }
}
