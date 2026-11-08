using System;
using System.Collections.Generic;
using System.Linq;

namespace TacticaUniversalis.Domain
{
    public enum CoachStaffRole
    {
        HeadCoach = 0,
        Assistant = 1,
        Analyst = 2
    }

    /// <summary>Hidden outcome for reading opponent tactics. Only Failure is shown to the player.</summary>
    public enum OpponentReadOutcome
    {
        Success = 0,
        Failure = 1,
        Misread = 2
    }

    [Serializable]
    public sealed class CareerCoachDefinition
    {
        public string id;
        public string displayName;
        public string role = "head_coach";
        public string clubId;
        public int age = 35;
        public string bio;

        // Head coach — pause attributes
        public int tactics = 50;
        public int motivation = 50;
        public int analysis = 50;
        public int observation = 50;

        // Head coach — bench attributes
        public int research = 50;
        public int lineup = 50;
        public int training = 50;
        public int management = 50;

        // Assistant-specific
        public int trainingExecution = 50;
        public int tacticalDesign = 50;
        public int vodReview = 50;
        public int coachSynergy = 60;

        // Analyst-specific
        public int dataInvestigation = 50;
        public int trendAnalysis = 50;
        public int patchAdaptation = 50;
        public int dataAccumulation = 40;

        // Shared ratings / status seeds
        public int prestige = 50;
        public int fame = 40;
        public int experience = 40;
        public int monthlySalary = 8000;
        public int loyalty = 60;
    }

    [Serializable]
    public sealed class CoachOpponentIntelEntry
    {
        public string opponentClubId;
        /// <summary>Times faced while this analyst was on staff.</summary>
        public int meetings;
        /// <summary>0–100 familiarity depth for this opponent.</summary>
        public int depth;
    }

    [Serializable]
    public sealed class CoachStaffState
    {
        public string coachId;
        public string clubId;
        public string role = "head_coach";
        public int age;

        // Head coach attrs
        public int tactics;
        public int motivation;
        public int analysis;
        public int observation;
        public int research;
        public int lineup;
        public int training;
        public int management;

        // Assistant attrs
        public int trainingExecution;
        public int tacticalDesign;
        public int vodReview;
        public int coachSynergy;

        // Analyst attrs
        public int dataInvestigation;
        public int trendAnalysis;
        public int patchAdaptation;
        public int dataAccumulation;
        public List<CoachOpponentIntelEntry> opponentIntel = new List<CoachOpponentIntelEntry>();

        // Shared
        public int prestige;
        public int fame;
        public int experience;
        public int energy = 100;
        public int confidence = 60;
        public int pressure = 40;
        public int health = 100;
        public int loyalty = 60;
        public int monthlySalary;
        public int remainingMonths = 24;
        /// <summary>Calendar months served at current club (tenure).</summary>
        public int monthsAtClub;
    }

    public static class ClubCoachStaffRules
    {
        public const int LowPrestigeTrustThreshold = 30;
        public const int LowManagementLoyaltyThreshold = 40;
        public const int AssistantPromoteExperience = 75;
        public const int VersionPatchAccumulationLoss = 8;
        public const int LeaveAccumulationDecay = 25;

        public static CoachStaffRole ParseRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return CoachStaffRole.HeadCoach;
            switch (role.Trim().ToLowerInvariant())
            {
                case "assistant":
                case "assistant_coach":
                case "助教":
                    return CoachStaffRole.Assistant;
                case "analyst":
                case "分析師":
                    return CoachStaffRole.Analyst;
                default:
                    return CoachStaffRole.HeadCoach;
            }
        }

        public static string ToStorageRole(CoachStaffRole role) =>
            role switch
            {
                CoachStaffRole.Assistant => "assistant",
                CoachStaffRole.Analyst => "analyst",
                _ => "head_coach"
            };

        public static string RoleLabel(CoachStaffRole role) =>
            role switch
            {
                CoachStaffRole.Assistant => "助教",
                CoachStaffRole.Analyst => "分析師",
                _ => "主教練"
            };

        public static string RoleLabel(string role) => RoleLabel(ParseRole(role));

        public static int Clamp(int value) => Math.Max(0, Math.Min(100, value));

        public static string FormatRatingGrade(int value)
        {
            value = Clamp(value);
            if (value >= 95) return "S";
            if (value >= 90) return "A+";
            if (value >= 85) return "A";
            if (value >= 80) return "B+";
            if (value >= 70) return "B";
            if (value >= 60) return "C";
            if (value >= 45) return "D";
            return "E";
        }

        public static string FormatRatedStat(int value) =>
            $"{Clamp(value)}（{FormatRatingGrade(value)}）";

        public static bool IsLowPrestige(CoachStaffState coach) =>
            coach != null && Clamp(coach.prestige) < LowPrestigeTrustThreshold;

        public static CoachStaffState CreateFromDefinition(CareerCoachDefinition def)
        {
            if (def == null)
                return null;
            CoachStaffRole role = ParseRole(def.role);
            var state = new CoachStaffState
            {
                coachId = def.id,
                clubId = def.clubId ?? string.Empty,
                role = ToStorageRole(role),
                age = Math.Max(18, def.age),
                tactics = Clamp(def.tactics),
                motivation = Clamp(def.motivation),
                analysis = Clamp(def.analysis),
                observation = Clamp(def.observation),
                research = Clamp(def.research),
                lineup = Clamp(def.lineup),
                training = Clamp(def.training),
                management = Clamp(def.management),
                trainingExecution = Clamp(def.trainingExecution > 0 ? def.trainingExecution : def.training),
                tacticalDesign = Clamp(def.tacticalDesign > 0 ? def.tacticalDesign : def.research),
                vodReview = Clamp(def.vodReview > 0 ? def.vodReview : def.observation),
                coachSynergy = Clamp(def.coachSynergy > 0 ? def.coachSynergy : 60),
                dataInvestigation = Clamp(def.dataInvestigation > 0 ? def.dataInvestigation : def.analysis),
                trendAnalysis = Clamp(def.trendAnalysis > 0 ? def.trendAnalysis : def.analysis),
                patchAdaptation = Clamp(def.patchAdaptation > 0 ? def.patchAdaptation : 50),
                dataAccumulation = Clamp(def.dataAccumulation > 0 ? def.dataAccumulation : 40),
                opponentIntel = new List<CoachOpponentIntelEntry>(),
                prestige = Clamp(def.prestige),
                fame = Clamp(def.fame),
                experience = Clamp(def.experience),
                energy = 100,
                confidence = 60,
                pressure = 40,
                health = 100,
                loyalty = Clamp(def.loyalty > 0 ? def.loyalty : 60),
                monthlySalary = Math.Max(0, def.monthlySalary),
                remainingMonths = 24,
                monthsAtClub = 0
            };
            return state;
        }

        public static bool HasActiveCoachOnClub(CareerState state, string clubId)
        {
            if (state?.coaches == null || string.IsNullOrWhiteSpace(clubId))
                return false;
            return state.coaches.Any(c =>
                c != null &&
                string.Equals(c.clubId, clubId, StringComparison.OrdinalIgnoreCase));
        }

        public static IEnumerable<CoachStaffState> ClubCoaches(CareerState state, string clubId) =>
            (state?.coaches ?? Enumerable.Empty<CoachStaffState>())
                .Where(c => c != null &&
                            string.Equals(c.clubId, clubId, StringComparison.OrdinalIgnoreCase));

        public static CoachStaffState FindByRole(CareerState state, string clubId, CoachStaffRole role)
        {
            string want = ToStorageRole(role);
            return ClubCoaches(state, clubId).FirstOrDefault(c =>
                string.Equals(c.role, want, StringComparison.OrdinalIgnoreCase));
        }

        public static int BestTrainingExecution(CareerState state, string clubId)
        {
            int best = 0;
            foreach (CoachStaffState coach in ClubCoaches(state, clubId))
            {
                CoachStaffRole role = ParseRole(coach.role);
                int value = role == CoachStaffRole.Assistant
                    ? coach.trainingExecution
                    : role == CoachStaffRole.HeadCoach
                        ? coach.training
                        : 0;
                best = Math.Max(best, Clamp(value));
            }
            return best;
        }

        /// <summary>Training plan execution quality: 0.85 .. 1.20.</summary>
        public static double GetStaffTrainingMultiplier(CareerState state, string clubId)
        {
            if (!HasActiveCoachOnClub(state, clubId))
                return 1d;
            return 0.85d + BestTrainingExecution(state, clubId) * 0.0035d;
        }

        /// <summary>
        /// Head coach tactics bonus, reduced when assistant synergy is low vs their tactical design.
        /// </summary>
        public static double GetTacticalCardBonusMultiplier(CareerState state, string clubId)
        {
            CoachStaffState head = FindByRole(state, clubId, CoachStaffRole.HeadCoach);
            CoachStaffState assistant = FindByRole(state, clubId, CoachStaffRole.Assistant);
            double baseBonus = head != null ? Clamp(head.tactics) / 400d : 0d;
            if (assistant != null)
            {
                double design = Clamp(assistant.tacticalDesign) / 100d;
                double synergy = Clamp(assistant.coachSynergy) / 100d;
                // Low synergy discounts assistant design contribution (and can drag head bonus).
                double assistShare = design * 0.15d * synergy;
                double mismatchPenalty = (1d - synergy) * design * 0.12d;
                return Math.Max(0.85d, 1d + baseBonus + assistShare - mismatchPenalty);
            }
            return 1d + baseBonus;
        }

        public static double GetMotivationRecoveryMultiplier(CareerState state, string clubId)
        {
            CoachStaffState head = FindByRole(state, clubId, CoachStaffRole.HeadCoach);
            if (head == null)
                return 1d;
            return 0.9d + Clamp(head.motivation) / 250d;
        }

        public static double GetVodReviewQualityMultiplier(CareerState state, string clubId)
        {
            CoachStaffState assistant = FindByRole(state, clubId, CoachStaffRole.Assistant);
            if (assistant == null)
                return 1d;
            return 0.85d + Clamp(assistant.vodReview) * 0.003d;
        }

        public static double GetPreMatchReportDepth(CareerState state, string clubId, string opponentClubId)
        {
            CoachStaffState analyst = FindByRole(state, clubId, CoachStaffRole.Analyst);
            if (analyst == null)
                return 0.4d;
            double baseDepth = Clamp(analyst.dataInvestigation) / 100d;
            double accum = Clamp(analyst.dataAccumulation) / 100d;
            double familiar = GetOpponentFamiliarity(analyst, opponentClubId) / 100d;
            return Math.Max(0.15d, Math.Min(1d, 0.35d * baseDepth + 0.35d * accum + 0.30d * familiar));
        }

        public static double GetTimeoutInsightAccuracy(CareerState state, string clubId)
        {
            CoachStaffState analyst = FindByRole(state, clubId, CoachStaffRole.Analyst);
            if (analyst == null)
                return 0.45d;
            return Math.Max(0.1d, Math.Min(0.95d,
                0.30d + Clamp(analyst.trendAnalysis) / 100d * 0.45d +
                Clamp(analyst.dataAccumulation) / 100d * 0.20d));
        }

        public static int GetOpponentFamiliarity(CoachStaffState analyst, string opponentClubId)
        {
            if (analyst == null || string.IsNullOrWhiteSpace(opponentClubId))
                return 0;
            CoachOpponentIntelEntry entry = analyst.opponentIntel?.FirstOrDefault(x =>
                x != null && string.Equals(x.opponentClubId, opponentClubId, StringComparison.OrdinalIgnoreCase));
            return entry == null ? 0 : Clamp(entry.depth);
        }

        public static void RecordOpponentMeeting(CoachStaffState analyst, string opponentClubId)
        {
            if (analyst == null || string.IsNullOrWhiteSpace(opponentClubId))
                return;
            analyst.opponentIntel = analyst.opponentIntel ?? new List<CoachOpponentIntelEntry>();
            CoachOpponentIntelEntry entry = analyst.opponentIntel.FirstOrDefault(x =>
                x != null && string.Equals(x.opponentClubId, opponentClubId, StringComparison.OrdinalIgnoreCase));
            if (entry == null)
            {
                entry = new CoachOpponentIntelEntry
                {
                    opponentClubId = opponentClubId,
                    meetings = 0,
                    depth = 0
                };
                analyst.opponentIntel.Add(entry);
            }

            entry.meetings++;
            int gain = 12 + Clamp(analyst.dataInvestigation) / 10;
            if (entry.meetings >= 2)
                gain += 8;
            entry.depth = Clamp(entry.depth + gain);
            analyst.dataAccumulation = Clamp(analyst.dataAccumulation + 2);
        }

        /// <summary>Game version / patch update: analyst intel decays (mitigated by patchAdaptation).</summary>
        public static void ApplyGameVersionUpdate(CareerState state)
        {
            if (state?.coaches == null)
                return;
            foreach (CoachStaffState coach in state.coaches)
            {
                if (coach == null || ParseRole(coach.role) != CoachStaffRole.Analyst)
                    continue;
                int resist = Clamp(coach.patchAdaptation) / 5;
                int loss = Math.Max(2, VersionPatchAccumulationLoss - resist);
                coach.dataAccumulation = Clamp(coach.dataAccumulation - loss);
                if (coach.opponentIntel == null)
                    continue;
                foreach (CoachOpponentIntelEntry entry in coach.opponentIntel)
                {
                    if (entry == null) continue;
                    entry.depth = Clamp(entry.depth - loss);
                }
            }
        }

        /// <summary>When an analyst leaves, club-wide familiarity drops.</summary>
        public static void OnAnalystLeftClub(CoachStaffState analyst)
        {
            if (analyst == null)
                return;
            analyst.dataAccumulation = Clamp(analyst.dataAccumulation - LeaveAccumulationDecay);
            if (analyst.opponentIntel == null)
                return;
            foreach (CoachOpponentIntelEntry entry in analyst.opponentIntel)
            {
                if (entry == null) continue;
                entry.depth = Clamp(entry.depth - LeaveAccumulationDecay);
            }
            analyst.clubId = string.Empty;
        }

        /// <summary>
        /// Hard mode: fraction of opponent player stats revealed by scouting / data investigation (0–1).
        /// </summary>
        public static double HardModeScoutRevealFraction(CareerState state, string clubId)
        {
            if (state == null || state.difficulty == CareerDifficulty.Normal)
                return 1d;
            CoachStaffState analyst = FindByRole(state, clubId, CoachStaffRole.Analyst);
            int skill = analyst != null ? Clamp(analyst.dataInvestigation) : 25;
            return Math.Max(0.15d, Math.Min(0.85d, 0.20d + skill / 140d));
        }

        public static bool CanPromoteAssistantToHeadCoach(CoachStaffState assistant) =>
            assistant != null &&
            ParseRole(assistant.role) == CoachStaffRole.Assistant &&
            Clamp(assistant.experience) >= AssistantPromoteExperience;

        /// <summary>
        /// Promote assistant → head coach. Attribute distribution follows assistant archetype.
        /// </summary>
        public static bool TryPromoteAssistantToHeadCoach(
            CareerState state, string clubId, out string failure)
        {
            failure = null;
            CoachStaffState assistant = FindByRole(state, clubId, CoachStaffRole.Assistant);
            if (assistant == null)
            {
                failure = "沒有助教";
                return false;
            }
            if (!CanPromoteAssistantToHeadCoach(assistant))
            {
                failure = $"經驗不足（需 {AssistantPromoteExperience}）";
                return false;
            }

            CoachStaffState existingHead = FindByRole(state, clubId, CoachStaffRole.HeadCoach);
            if (existingHead != null)
                existingHead.clubId = string.Empty;

            // Archetype: training / design / review weights redistribute into head coach attrs.
            int exec = Clamp(assistant.trainingExecution);
            int design = Clamp(assistant.tacticalDesign);
            int review = Clamp(assistant.vodReview);
            int syn = Clamp(assistant.coachSynergy);

            assistant.role = ToStorageRole(CoachStaffRole.HeadCoach);
            assistant.tactics = Clamp((design * 2 + syn) / 3);
            assistant.motivation = Clamp((syn * 2 + review) / 3);
            assistant.analysis = Clamp((design + review) / 2);
            assistant.observation = Clamp((review * 2 + exec) / 3);
            assistant.research = Clamp(design);
            assistant.lineup = Clamp((exec + design) / 2);
            assistant.training = Clamp(exec);
            assistant.management = Clamp((syn * 2 + exec) / 3);
            assistant.prestige = Clamp(assistant.prestige + 8);
            assistant.fame = Clamp(assistant.fame + 4);
            assistant.monthlySalary = Math.Max(assistant.monthlySalary, assistant.monthlySalary + 4000);
            return true;
        }

        public static double OpponentReadSuccessChance(int analysisOrTrend, int iglAwareness)
        {
            double a = Clamp(analysisOrTrend) / 100d;
            double awareness = Clamp(iglAwareness) / 100d;
            return Math.Max(0.08d, Math.Min(0.92d, 0.25d + a * 0.45d + awareness * 0.30d));
        }

        public static OpponentReadOutcome RollOpponentRead(
            int analysisOrTrend, int iglAwareness, SeededRandom random)
        {
            double pSuccess = OpponentReadSuccessChance(analysisOrTrend, iglAwareness);
            double roll = random != null ? random.Next01() : 0.5d;
            if (roll < pSuccess)
                return OpponentReadOutcome.Success;
            double failShare = 0.55d;
            if (roll < pSuccess + (1d - pSuccess) * failShare)
                return OpponentReadOutcome.Failure;
            return OpponentReadOutcome.Misread;
        }

        public static string PlayerFacingOpponentReadMessage(OpponentReadOutcome outcome) =>
            outcome == OpponentReadOutcome.Failure
                ? "解讀對手戰術失敗。"
                : null;

        public static void ApplyHourlyEnergyRecovery(CareerState state, int hours)
        {
            if (state?.coaches == null || hours <= 0)
                return;
            foreach (CoachStaffState coach in state.coaches)
            {
                if (coach != null)
                    coach.energy = EnergyRecoveryRules.Recover(coach.energy, hours);
            }
        }

        public static void ApplyMonthlyStaffPulse(CareerState state, string clubId)
        {
            foreach (CoachStaffState coach in ClubCoaches(state, clubId).ToArray())
            {
                CoachStaffRole role = ParseRole(coach.role);
                coach.monthsAtClub = Math.Max(0, coach.monthsAtClub) + 1;
                coach.pressure = Clamp(coach.pressure - 2);
                if (coach.remainingMonths > 0)
                    coach.remainingMonths--;

                if (role == CoachStaffRole.HeadCoach)
                {
                    if (Clamp(coach.management) < LowManagementLoyaltyThreshold)
                        coach.loyalty = Clamp(coach.loyalty - 1);
                    else if (Clamp(coach.management) >= 70)
                        coach.loyalty = Clamp(coach.loyalty + 1);
                }
                else if (role == CoachStaffRole.Assistant)
                {
                    // Tenure builds succession experience.
                    if (coach.monthsAtClub % 2 == 0)
                        coach.experience = Clamp(coach.experience + 1);
                    if (Clamp(coach.coachSynergy) < 35)
                        coach.loyalty = Clamp(coach.loyalty - 1);
                }
                else if (role == CoachStaffRole.Analyst)
                {
                    // Longer tenure deepens general accumulation (patch adaptation slows loss elsewhere).
                    if (coach.monthsAtClub % 3 == 0)
                        coach.dataAccumulation = Clamp(coach.dataAccumulation + 1 + Clamp(coach.patchAdaptation) / 50);
                }
            }
        }

        public static int ResolveIglAwareness(
            CareerState state,
            IReadOnlyDictionary<string, CareerPlayerDefinition> playerDefs,
            string clubId)
        {
            if (state?.players == null || string.IsNullOrWhiteSpace(clubId))
                return 50;

            int bestAwareness = 0;
            int iglAwareness = 0;
            bool hasIgl = false;

            foreach (PlayerCareerState player in state.players)
            {
                if (player == null ||
                    !string.Equals(player.clubId, clubId, StringComparison.OrdinalIgnoreCase))
                    continue;
                int awareness = ReadAttribute(player, "awareness");
                if (awareness > bestAwareness)
                    bestAwareness = awareness;

                if (playerDefs != null &&
                    playerDefs.TryGetValue(player.playerId, out CareerPlayerDefinition def) &&
                    def != null &&
                    !string.IsNullOrWhiteSpace(def.role) &&
                    (string.Equals(def.role, "Igl", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(def.role, "IGL", StringComparison.OrdinalIgnoreCase)))
                {
                    hasIgl = true;
                    iglAwareness = awareness;
                }
            }

            if (hasIgl)
                return iglAwareness > 0 ? iglAwareness : 50;
            return bestAwareness > 0 ? bestAwareness : 50;
        }

        private static int ReadAttribute(PlayerCareerState player, string attributeId)
        {
            if (player?.attributes == null)
                return 0;
            CareerNamedStat stat = player.attributes.FirstOrDefault(x =>
                x != null && string.Equals(x.id, attributeId, StringComparison.OrdinalIgnoreCase));
            return stat == null ? 0 : Clamp((int)Math.Round(stat.value));
        }

        public static int MonthlyStaffSalary(CareerState state, string clubId) =>
            ClubCoaches(state, clubId).Sum(c => Math.Max(0, c.monthlySalary));

        public static double GetResearchOpenBuffMultiplier(CareerState state, string clubId)
        {
            CoachStaffState assistant = FindByRole(state, clubId, CoachStaffRole.Assistant);
            CoachStaffState head = FindByRole(state, clubId, CoachStaffRole.HeadCoach);
            double design = assistant != null ? Clamp(assistant.tacticalDesign) / 500d : 0d;
            double headResearch = head != null ? Clamp(head.research) / 500d : 0d;
            return 1d + design + headResearch;
        }

        public static double GetLineupMismatchDebuffReduction(CareerState state, string clubId)
        {
            CoachStaffState head = FindByRole(state, clubId, CoachStaffRole.HeadCoach);
            if (head == null)
                return 0d;
            return Clamp(head.lineup) / 200d;
        }

        public static double GetObservationTempBoostFraction(CareerState state, string clubId)
        {
            CoachStaffState head = FindByRole(state, clubId, CoachStaffRole.HeadCoach);
            if (head == null)
                return 0d;
            return Clamp(head.observation) / 850d;
        }
    }
}
