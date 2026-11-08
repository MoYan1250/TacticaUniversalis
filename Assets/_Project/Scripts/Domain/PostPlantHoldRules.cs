namespace TacticaUniversalis.Domain
{
    /// <summary>
    /// Post-plant hold vs egress: stay until ~8s before boom unless clearing out
    /// when the site is empty and nobody is defusing.
    /// </summary>
    public static class PostPlantHoldRules
    {
        public const float EgressLeadSeconds = 8f;

        public static float BombRemaining(float plantTime, float bombTimer, float time)
        {
            if (plantTime < 0f || bombTimer <= 0f)
                return float.MaxValue;
            return plantTime + bombTimer - time;
        }

        /// <summary>
        /// Evacuate when the fuse is short, no defuse pressure, and no enemies on/near site.
        /// </summary>
        public static bool ShouldEvacuate(float remainingSeconds, bool hearsDefuse, bool enemiesOnOrNearSite)
        {
            if (hearsDefuse || enemiesOnOrNearSite)
                return false;
            return remainingSeconds <= EgressLeadSeconds;
        }
    }
}
