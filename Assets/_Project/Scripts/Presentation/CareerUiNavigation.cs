using TacticaUniversalis.Domain;

namespace TacticaUniversalis.Presentation
{
    internal enum CareerUiDestination
    {
        None,
        Map,
        MapCompetition,
        OfficialMatch,
        PlayerRoster,
        TransferMarket,
        Training,
        Finance,
        Sponsors,
        Facilities,
        EventLog,
        Tactics,
        WorldRanking,
        PlayerProfile,
    }

    internal readonly struct CareerUiNavLink
    {
        public CareerUiDestination Destination { get; }
        public string SubjectId { get; }

        public bool IsActive => Destination != CareerUiDestination.None;

        public CareerUiNavLink(CareerUiDestination destination, string subjectId = null)
        {
            Destination = destination;
            SubjectId = subjectId;
        }

        public static CareerUiNavLink None => new CareerUiNavLink(CareerUiDestination.None);

        public static CareerUiNavLink FromEvent(CareerEvent evt)
        {
            if (evt == null) return None;

            switch (evt.type)
            {
                case CareerEventType.MatchCompleted:
                case CareerEventType.PrizeAwarded:
                    return string.IsNullOrEmpty(evt.subjectId)
                        ? new CareerUiNavLink(CareerUiDestination.Map)
                        : new CareerUiNavLink(CareerUiDestination.MapCompetition, evt.subjectId);
                case CareerEventType.TransferCompleted:
                case CareerEventType.ContractChanged:
                    return string.IsNullOrEmpty(evt.subjectId)
                        ? new CareerUiNavLink(CareerUiDestination.TransferMarket)
                        : new CareerUiNavLink(CareerUiDestination.PlayerProfile, evt.subjectId);
                case CareerEventType.SponsorChanged:
                    return new CareerUiNavLink(CareerUiDestination.Sponsors);
                case CareerEventType.FacilityChanged:
                    return new CareerUiNavLink(CareerUiDestination.Facilities);
                case CareerEventType.TrainingApplied:
                    return new CareerUiNavLink(CareerUiDestination.Training);
                case CareerEventType.RankingUpdated:
                    return new CareerUiNavLink(CareerUiDestination.WorldRanking);
                case CareerEventType.FinancialStatusChanged:
                case CareerEventType.MonthClosed:
                    return new CareerUiNavLink(CareerUiDestination.Finance);
                case CareerEventType.HoursAdvanced:
                case CareerEventType.PlayerInjured:
                    return new CareerUiNavLink(CareerUiDestination.PlayerRoster);
                default:
                    return new CareerUiNavLink(CareerUiDestination.EventLog);
            }
        }
    }
}
