using System;
using System.Collections.Generic;

namespace TacticaUniversalis.Domain
{
    public enum BtStatus
    {
        Success,
        Failure,
        Running
    }

    public abstract class BtNode
    {
        public abstract BtStatus Tick(BtContext context);
    }

    public sealed class BtContext
    {
        public BehaviorTreeRoundWorld World { get; set; }
        public PlayerRuntime Self { get; set; }
        public TeamRuntime SelfTeam { get; set; }
        public TeamRuntime EnemyTeam { get; set; }
        public TacticalPlan SelfPlan { get; set; }
        public float Time { get; set; }
        public IRandomSource Random { get; set; }

        public object Blackboard { get; set; }
    }

    public sealed class BtSequence : BtNode
    {
        private readonly BtNode[] _children;
        public BtSequence(params BtNode[] children) => _children = children ?? Array.Empty<BtNode>();

        public override BtStatus Tick(BtContext context)
        {
            foreach (BtNode child in _children)
            {
                BtStatus status = child.Tick(context);
                if (status != BtStatus.Success)
                    return status;
            }
            return BtStatus.Success;
        }
    }

    public sealed class BtSelector : BtNode
    {
        private readonly BtNode[] _children;
        public BtSelector(params BtNode[] children) => _children = children ?? Array.Empty<BtNode>();

        public override BtStatus Tick(BtContext context)
        {
            foreach (BtNode child in _children)
            {
                BtStatus status = child.Tick(context);
                if (status != BtStatus.Failure)
                    return status;
            }
            return BtStatus.Failure;
        }
    }

    public sealed class BtCondition : BtNode
    {
        private readonly Func<BtContext, bool> _predicate;
        public BtCondition(Func<BtContext, bool> predicate) => _predicate = predicate;

        public override BtStatus Tick(BtContext context) =>
            _predicate != null && _predicate(context) ? BtStatus.Success : BtStatus.Failure;
    }

    public sealed class BtAction : BtNode
    {
        private readonly Func<BtContext, BtStatus> _action;
        public BtAction(Func<BtContext, BtStatus> action) => _action = action;

        public override BtStatus Tick(BtContext context) =>
            _action != null ? _action(context) : BtStatus.Failure;
    }

    /// <summary>Undirected zone graph for general-mode pathing (Domain, no Unity).</summary>
    public sealed class MapZoneGraph
    {
        private readonly Dictionary<string, HashSet<string>> _edges =
            new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        public MapZoneGraph(MapDefinition map)
        {
            foreach (MapZoneDefinition zone in map?.zones ?? Array.Empty<MapZoneDefinition>())
                Ensure(zone.id);

            foreach (MapRouteDefinition route in map?.routes ?? Array.Empty<MapRouteDefinition>())
            {
                string[] points = route.waypoints;
                if (points == null || points.Length == 0)
                {
                    Connect(route.from, route.to);
                    continue;
                }

                for (int i = 0; i < points.Length - 1; i++)
                    Connect(points[i], points[i + 1]);
            }

            if (string.Equals(map?.id, "dust2", StringComparison.OrdinalIgnoreCase))
                AddDust2TacticalLinks();
        }

        private void AddDust2TacticalLinks()
        {
            Connect("t_spawn", "suicide");
            Connect("suicide", "top_mid");
            Connect("top_mid", "mid");
            Connect("mid", "mid_doors");
            Connect("mid_doors", "xbox");
            Connect("t_spawn", "outside_long");
            Connect("outside_long", "long_doors");
            Connect("outside_long", "pit");
            Connect("long_doors", "a_long");
            Connect("pit", "a_long");
            Connect("a_long", "a_ramp");
            Connect("a_ramp", "a_site");
            Connect("a_long", "a_site");
            Connect("a_short", "cat");
            Connect("cat", "a_site");
            Connect("a_short", "a_site");
            Connect("a_site", "ct_spawn");
            Connect("b_site", "ct_spawn");
            Connect("xbox", "ct_mid");
            Connect("ct_mid", "ct_spawn");
            Connect("xbox", "lower_tunnels");
            Connect("lower_tunnels", "b_tunnels");
            Connect("t_spawn", "outside_tunnels");
            Connect("outside_tunnels", "b_tunnels");
            Connect("b_tunnels", "b_site");
            Connect("b_site", "b_doors");
            Connect("b_doors", "scaffolding");
            Connect("scaffolding", "ct_mid");
            Connect("b_doors", "ct_mid");
            Connect("mid_doors", "cat");
            Connect("mid", "cat");
            Connect("cat", "a_short");
            Connect("a_short", "a_ramp");
        }

        public IReadOnlyList<string> FindPath(string from, string to) =>
            FindPath(from, to, null);

        /// <summary>
        /// Shortest path. When <paramref name="zoneCost"/> is set, uses Dijkstra with
        /// non-negative costs (entry cost of each node except the start).
        /// </summary>
        public IReadOnlyList<string> FindPath(string from, string to, Func<string, float> zoneCost)
        {
            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
                return Array.Empty<string>();
            if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
                return new[] { to };

            Ensure(from);
            Ensure(to);

            if (zoneCost == null)
                return FindPathBfs(from, to);
            return FindPathDijkstra(from, to, zoneCost);
        }

        private IReadOnlyList<string> FindPathBfs(string from, string to)
        {
            var queue = new Queue<string>();
            var prev = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            queue.Enqueue(from);
            prev[from] = null;
            while (queue.Count > 0)
            {
                string cur = queue.Dequeue();
                if (string.Equals(cur, to, StringComparison.OrdinalIgnoreCase))
                    break;
                foreach (string next in _edges[cur])
                {
                    if (prev.ContainsKey(next))
                        continue;
                    prev[next] = cur;
                    queue.Enqueue(next);
                }
            }

            return Reconstruct(prev, from, to);
        }

        private IReadOnlyList<string> FindPathDijkstra(
            string from, string to, Func<string, float> zoneCost)
        {
            var dist = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
            var prev = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var open = new List<string>();
            dist[from] = 0f;
            prev[from] = null;
            open.Add(from);

            while (open.Count > 0)
            {
                int bestIdx = 0;
                float bestDist = dist[open[0]];
                for (int i = 1; i < open.Count; i++)
                {
                    float d = dist[open[i]];
                    if (d < bestDist)
                    {
                        bestDist = d;
                        bestIdx = i;
                    }
                }

                string cur = open[bestIdx];
                open.RemoveAt(bestIdx);
                if (string.Equals(cur, to, StringComparison.OrdinalIgnoreCase))
                    break;

                foreach (string next in _edges[cur])
                {
                    float step = zoneCost(next);
                    if (step < 0.01f)
                        step = 0.01f;
                    float candidate = bestDist + step;
                    if (dist.TryGetValue(next, out float known) && candidate >= known - 1e-5f)
                        continue;
                    dist[next] = candidate;
                    prev[next] = cur;
                    if (!open.Exists(z => string.Equals(z, next, StringComparison.OrdinalIgnoreCase)))
                        open.Add(next);
                }
            }

            return Reconstruct(prev, from, to);
        }

        private static IReadOnlyList<string> Reconstruct(
            Dictionary<string, string> prev, string from, string to)
        {
            if (prev == null || !prev.ContainsKey(to))
                return Array.Empty<string>();

            var path = new List<string>();
            for (string at = to; at != null; at = prev[at])
                path.Add(at);
            path.Reverse();
            if (path.Count == 0 ||
                !string.Equals(path[0], from, StringComparison.OrdinalIgnoreCase))
                return Array.Empty<string>();
            return path;
        }

        public IEnumerable<string> Neighbors(string zoneId)
        {
            Ensure(zoneId);
            return _edges[zoneId];
        }

        private void Connect(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
                return;
            Ensure(a);
            Ensure(b);
            _edges[a].Add(b);
            _edges[b].Add(a);
        }

        private void Ensure(string id)
        {
            if (string.IsNullOrEmpty(id))
                return;
            if (!_edges.ContainsKey(id))
                _edges[id] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
