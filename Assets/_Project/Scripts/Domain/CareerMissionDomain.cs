using System;
using System.Collections.Generic;
using System.Linq;

namespace TacticaUniversalis.Domain
{
    public enum CareerMissionNodeStatus
    {
        Locked = 0,
        Available = 1,
        Completable = 2,
        Completed = 3,
        Failed = 4,
        BlockedByExclusive = 5
    }

    [Serializable]
    public sealed class CareerMissionNodeDef
    {
        public string id;
        public string title;
        public string description;
        /// <summary>Hover brief: story context.</summary>
        public string flavor;
        /// <summary>Hover brief: objective and unlock hint.</summary>
        public string goal;
        public string[] prerequisites = Array.Empty<string>();
        /// <summary>Mutually exclusive group id. Completing one blocks siblings.</summary>
        public string exclusiveGroup;
        public CareerConditionDef[] completeTriggers = Array.Empty<CareerConditionDef>();
        public CareerEffectDef[] effects = Array.Empty<CareerEffectDef>();
        public CareerRosterOpDef[] rosterOps = Array.Empty<CareerRosterOpDef>();
        public int uiColumn;
        public int uiRow;
    }

    [Serializable]
    public sealed class CareerMissionTreeDef
    {
        public string id;
        public string clubId;
        public string title;
        public CareerMissionNodeDef[] nodes = Array.Empty<CareerMissionNodeDef>();
    }

    [Serializable]
    public sealed class CareerMissionProgress
    {
        public List<string> completedNodeIds = new List<string>();
        public List<string> failedNodeIds = new List<string>();
    }

    public static class CareerMissionRules
    {
        public const string NaviPathB1tFlag = "navi_path_b1t";
        public const string NaviStockholmChampionFlag = "navi_stockholm_champion";
        public const string NaviGrandSlamClaimedFlag = "navi_grand_slam_claimed";

        public static CareerMissionTreeDef FindTreeForClub(
            IEnumerable<CareerMissionTreeDef> trees, string clubId)
        {
            if (trees == null || string.IsNullOrWhiteSpace(clubId))
                return null;
            return trees.FirstOrDefault(t =>
                t != null &&
                string.Equals(t.clubId, clubId, StringComparison.OrdinalIgnoreCase));
        }

        public static CareerMissionNodeDef FindNode(CareerMissionTreeDef tree, string nodeId)
        {
            if (tree?.nodes == null || string.IsNullOrWhiteSpace(nodeId))
                return null;
            return tree.nodes.FirstOrDefault(n =>
                n != null && string.Equals(n.id, nodeId, StringComparison.OrdinalIgnoreCase));
        }

        public static bool IsCompleted(CareerMissionProgress progress, string nodeId)
        {
            if (progress?.completedNodeIds == null || string.IsNullOrWhiteSpace(nodeId))
                return false;
            return progress.completedNodeIds.Any(id =>
                string.Equals(id, nodeId, StringComparison.OrdinalIgnoreCase));
        }

        public static bool IsFailed(CareerMissionProgress progress, string nodeId)
        {
            if (progress?.failedNodeIds == null || string.IsNullOrWhiteSpace(nodeId))
                return false;
            return progress.failedNodeIds.Any(id =>
                string.Equals(id, nodeId, StringComparison.OrdinalIgnoreCase));
        }

        public static CareerMissionNodeStatus GetStatus(
            CareerMissionTreeDef tree,
            CareerMissionNodeDef node,
            CareerMissionProgress progress,
            CareerState state,
            ClubState club,
            IReadOnlyDictionary<string, CompetitionDefinition> competitions = null)
        {
            if (node == null)
                return CareerMissionNodeStatus.Locked;
            if (IsCompleted(progress, node.id))
                return CareerMissionNodeStatus.Completed;
            if (IsFailed(progress, node.id))
                return CareerMissionNodeStatus.Failed;
            if (IsBlockedByExclusive(tree, node, progress))
                return CareerMissionNodeStatus.BlockedByExclusive;
            if (!PrerequisitesMet(node, progress))
                return CareerMissionNodeStatus.Locked;

            var unlocked = state?.unlockedAchievements ?? (IReadOnlyCollection<string>)Array.Empty<string>();
            if (CareerScriptedRules.MatchesAll(node.completeTriggers, state, club, unlocked, competitions))
                return CareerMissionNodeStatus.Completable;
            return CareerMissionNodeStatus.Available;
        }

        public static bool PrerequisitesMet(CareerMissionNodeDef node, CareerMissionProgress progress)
        {
            if (node?.prerequisites == null || node.prerequisites.Length == 0)
                return true;
            foreach (string prereq in node.prerequisites)
            {
                if (string.IsNullOrWhiteSpace(prereq))
                    continue;
                if (!IsCompleted(progress, prereq))
                    return false;
            }
            return true;
        }

        public static bool IsBlockedByExclusive(
            CareerMissionTreeDef tree, CareerMissionNodeDef node, CareerMissionProgress progress)
        {
            if (tree?.nodes == null || node == null || string.IsNullOrWhiteSpace(node.exclusiveGroup))
                return false;
            foreach (CareerMissionNodeDef other in tree.nodes)
            {
                if (other == null || string.IsNullOrWhiteSpace(other.id))
                    continue;
                if (string.Equals(other.id, node.id, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.Equals(other.exclusiveGroup, node.exclusiveGroup, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (IsCompleted(progress, other.id))
                    return true;
            }
            return false;
        }

        public static string StatusLabel(CareerMissionNodeStatus status)
        {
            switch (status)
            {
                case CareerMissionNodeStatus.Locked: return "未解鎖";
                case CareerMissionNodeStatus.Available: return "進行中";
                case CareerMissionNodeStatus.Completable: return "可完成";
                case CareerMissionNodeStatus.Completed: return "已完成";
                case CareerMissionNodeStatus.Failed: return "失敗";
                case CareerMissionNodeStatus.BlockedByExclusive: return "路線已關閉";
                default: return "未知";
            }
        }

        public static void EnsureProgress(CareerState state)
        {
            if (state == null)
                return;
            state.missionProgress = state.missionProgress ?? new CareerMissionProgress();
            state.missionProgress.completedNodeIds =
                state.missionProgress.completedNodeIds ?? new List<string>();
            state.missionProgress.failedNodeIds =
                state.missionProgress.failedNodeIds ?? new List<string>();
        }

        public static void MarkCompleted(CareerMissionProgress progress, string nodeId)
        {
            if (progress == null || string.IsNullOrWhiteSpace(nodeId))
                return;
            progress.completedNodeIds = progress.completedNodeIds ?? new List<string>();
            if (!IsCompleted(progress, nodeId))
                progress.completedNodeIds.Add(nodeId);
            progress.failedNodeIds = progress.failedNodeIds ?? new List<string>();
            progress.failedNodeIds.RemoveAll(id =>
                string.Equals(id, nodeId, StringComparison.OrdinalIgnoreCase));
        }
    }
}
