using System.Collections.Generic;
using TacticaUniversalis.Domain;

namespace TacticaUniversalis.Presentation
{
    public sealed class MapPathfinder
    {
        private readonly MapZoneGraph _graph;

        public MapPathfinder(MapDefinition map)
        {
            _graph = new MapZoneGraph(map);
        }

        public IReadOnlyList<string> FindPath(string from, string to) => _graph.FindPath(from, to);
    }
}
