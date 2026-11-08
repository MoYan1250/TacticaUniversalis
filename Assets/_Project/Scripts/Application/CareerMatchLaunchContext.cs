using System;
using System.IO;
using System.Text;
using TacticaUniversalis.Domain;

namespace TacticaUniversalis.Application
{
    /// <summary>In-memory bridge between career scene and Demo_Dust2_QuickSim (not saved).</summary>
    public static class CareerMatchLaunchContext
    {
        private static readonly string LaunchBackupPath =
            Path.Combine(Path.GetTempPath(), "TacticaUniversalis.MatchLaunchContext.json");

        [Serializable]
        public sealed class LaunchRequest
        {
            public string playerCareerClubId;
            public string opponentCareerClubId;
            public string playerSimTeamId;
            public string opponentSimTeamId;
            public int seed;
            public string competitionId;
            public string competitionDisplayName;
            public string mapId;
            public string mapDisplayName;
            public string bracketStageLabel;
            public int bestOf = 3;
            public int seriesMapIndex;
            public int seriesScorePlayer;
            public int seriesScoreOpponent;
        }

        private static LaunchRequest _launch;
        private static MatchResult _completedResult;

        public static bool HasLaunch => _launch != null;
        public static bool HasCompletedResult => _completedResult != null;

        public static void SetLaunch(LaunchRequest request)
        {
            _launch = request;
            if (request == null)
            {
                DeleteLaunchBackup();
                return;
            }

            File.WriteAllText(LaunchBackupPath, SerializeLaunch(request), Encoding.UTF8);
        }

        public static bool TryTakeLaunch(out LaunchRequest request)
        {
            request = _launch;
            if (request == null)
                request = TryLoadLaunchBackup();
            if (request == null) return false;
            _launch = null;
            DeleteLaunchBackup();
            return true;
        }

        public static void SetCompletedResult(MatchResult result) => _completedResult = result;

        public static bool TryTakeCompletedResult(out MatchResult result)
        {
            result = _completedResult;
            if (result == null) return false;
            _completedResult = null;
            return true;
        }

        public static void Clear()
        {
            _launch = null;
            _completedResult = null;
            DeleteLaunchBackup();
        }

        private static LaunchRequest TryLoadLaunchBackup()
        {
            try
            {
                if (!File.Exists(LaunchBackupPath))
                    return null;
                string[] lines = File.ReadAllLines(LaunchBackupPath, Encoding.UTF8);
                if (lines.Length < 10)
                    return null;
                return new LaunchRequest
                {
                    playerCareerClubId = Restore(lines[0]),
                    opponentCareerClubId = Restore(lines[1]),
                    playerSimTeamId = Restore(lines[2]),
                    opponentSimTeamId = Restore(lines[3]),
                    seed = ParseInt(lines[4]),
                    competitionId = Restore(lines[5]),
                    competitionDisplayName = Restore(lines[6]),
                    mapId = Restore(lines[7]),
                    mapDisplayName = Restore(lines[8]),
                    bracketStageLabel = Restore(lines[9]),
                    bestOf = lines.Length > 10 ? ParseInt(lines[10], 3) : 3,
                    seriesMapIndex = lines.Length > 11 ? ParseInt(lines[11]) : 0,
                    seriesScorePlayer = lines.Length > 12 ? ParseInt(lines[12]) : 0,
                    seriesScoreOpponent = lines.Length > 13 ? ParseInt(lines[13]) : 0,
                };
            }
            catch
            {
                return null;
            }
        }

        private static string SerializeLaunch(LaunchRequest request)
        {
            string[] values =
            {
                Persist(request.playerCareerClubId),
                Persist(request.opponentCareerClubId),
                Persist(request.playerSimTeamId),
                Persist(request.opponentSimTeamId),
                request.seed.ToString(),
                Persist(request.competitionId),
                Persist(request.competitionDisplayName),
                Persist(request.mapId),
                Persist(request.mapDisplayName),
                Persist(request.bracketStageLabel),
                request.bestOf.ToString(),
                request.seriesMapIndex.ToString(),
                request.seriesScorePlayer.ToString(),
                request.seriesScoreOpponent.ToString(),
            };
            return string.Join("\n", values);
        }

        private static string Persist(string value) =>
            Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty));

        private static string Restore(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(value));
            }
            catch
            {
                return string.Empty;
            }
        }

        private static int ParseInt(string value, int fallback = 0) =>
            int.TryParse(value, out int parsed) ? parsed : fallback;

        private static void DeleteLaunchBackup()
        {
            try
            {
                if (File.Exists(LaunchBackupPath))
                    File.Delete(LaunchBackupPath);
            }
            catch
            {
            }
        }
    }

    public static class CareerMatchTeams
    {
        public static bool TryResolveSimTeams(
            ContentDatabase content,
            string playerCareerClubId,
            string opponentCareerClubId,
            out string playerSimTeamId,
            out string opponentSimTeamId)
        {
            playerSimTeamId = ResolveSimTeam(content, playerCareerClubId);
            opponentSimTeamId = ResolveSimTeam(content, opponentCareerClubId);
            if (content == null ||
                string.IsNullOrEmpty(playerSimTeamId) ||
                string.IsNullOrEmpty(opponentSimTeamId))
                return false;
            return content.Teams.ContainsKey(playerSimTeamId) &&
                   content.Teams.ContainsKey(opponentSimTeamId);
        }

        public static string ResolveSimTeam(ContentDatabase content, string careerClubId)
        {
            if (content == null || string.IsNullOrEmpty(careerClubId))
                return null;
            if (content.Teams.ContainsKey(careerClubId))
                return careerClubId;
            return null;
        }
    }
}
