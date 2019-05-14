using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageCompare.Classes.Directions.AStar
{
    class PathFinder
    {
        public Map Map { get; set; }
        public Node Start { get; set; }
        public Node End { get; set; }
        public int NodeVisits { get; private set; }
        public double ShortestPathLength { get; set; }
        public double ShortestPathCost { get; private set; }

        public List<Node> GetShortestPathAstar()
        {
            foreach ( var node in Map.Nodes )
                node.StraightLineDistanceToEnd = node.StraightLineDistanceTo(End);
            AstarSearch();
            var shortestPath = new List<Node>();
            shortestPath.Add(End);
            BuildShortestPath(shortestPath , End);
            shortestPath.Reverse();
            return shortestPath;
        }

        private void BuildShortestPath(List<Node> list , Node node)
        {
            if ( node.NearestToStart == null )
                return;
            list.Add(node.NearestToStart);
            ShortestPathLength += node.Connections.Single(x => x.ConnectedNode == node.NearestToStart).Length;
            ShortestPathCost += node.Connections.Single(x => x.ConnectedNode == node.NearestToStart).Cost;
            BuildShortestPath(list , node.NearestToStart);
        }

        private void AstarSearch()
        {
            Start.MinCostToStart = 0;
            var prioQueue = new List<Node>();
            prioQueue.Add(Start);
            do
            {
                prioQueue = prioQueue.OrderBy(x => x.MinCostToStart + x.StraightLineDistanceToEnd).ToList();
                var node = prioQueue.First();
                prioQueue.Remove(node);
                NodeVisits++;
                foreach ( var cnn in node.Connections.OrderBy(x => x.Cost) )
                {
                    var childNode = cnn.ConnectedNode;
                    if ( childNode.Visited )
                        continue;
                    if ( childNode.MinCostToStart == null ||
                        node.MinCostToStart + cnn.Cost < childNode.MinCostToStart )
                    {
                        childNode.MinCostToStart = node.MinCostToStart + cnn.Cost;
                        childNode.NearestToStart = node;
                        if ( !prioQueue.Contains(childNode) )
                            prioQueue.Add(childNode);
                    }
                }
                node.Visited = true;
                if ( node == End )
                    return;
            } while ( prioQueue.Any() );
        }
    }
}
