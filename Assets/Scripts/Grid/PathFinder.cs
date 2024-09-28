using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class PathFinder
{
    private HexGrid grid;
    
    public PathFinder(int Width, int Height)
    {
        
    }

    public static List<HexCell> FindPath(HexCell origin, HexCell destination)
    {
        Dictionary<HexCell, PathNode> nodesNotEvaluated = new Dictionary<HexCell, PathNode>();
        Dictionary<HexCell, PathNode> nodesAlreadyEvaluated = new Dictionary<HexCell, PathNode>();

        PathNode startNode = new PathNode(origin, origin, destination, 0);
        nodesNotEvaluated.Add(origin, startNode);

        bool gotPath = EvaluateNextNode(nodesNotEvaluated, nodesAlreadyEvaluated, origin, destination,
            out List<HexCell> path);

        while (!gotPath)
        {
            gotPath = EvaluateNextNode(nodesNotEvaluated, nodesAlreadyEvaluated, origin, destination, out path);
        }

        return path;
    }

    private static bool EvaluateNextNode(Dictionary<HexCell, PathNode> nodesNotEvaluated,
        Dictionary<HexCell, PathNode> nodesEvaluated, HexCell origin, HexCell destination, out List<HexCell> path)
    {
        PathNode currentNode = GetCheapestNode(nodesNotEvaluated.Values.ToArray());

        if (currentNode == null)
        {
            path = new List<HexCell>();
            return false;
        }

        nodesNotEvaluated.Remove(currentNode.target);
        nodesEvaluated.Add(currentNode.target, currentNode);

        path = new List<HexCell>();

        if (currentNode.target == destination)
        {
            path.Add(currentNode.target);
            while (currentNode.target != origin)
            {
                path.Add(currentNode.parent.target);
                currentNode = currentNode.parent;
            }

            return true;
        }

        List<PathNode> neighbours = new List<PathNode>();

        foreach (HexCell tile in currentNode.target.Neighbours)
        {
            PathNode node = new PathNode(tile, origin, destination, currentNode.GetCost());

            if (tile.IsNotLand())
            {
                node.baseCost = 999999;
                // continue;
            }
            
            neighbours.Add(node);
        }

        foreach (PathNode neighbour in neighbours)
        {
            if (nodesEvaluated.Keys.Contains(neighbour.target)) continue;

            if (neighbour.GetCost() < currentNode.GetCost() || !nodesNotEvaluated.Keys.Contains(neighbour.target))
            {
                neighbour.SetParent(currentNode);
                if (!nodesNotEvaluated.Keys.Contains(neighbour.target))
                {
                    nodesNotEvaluated.Add(neighbour.target, neighbour);
                }
            }
        }

        return false;
    }

    // public void OnDrawGizmos()
    // {
    //     if (path != null)
    //     {
    //         foreach (HexCell tile in path)
    //         {
    //             Gizmos.DrawCube(tile);
    //         }
    //     }
    // }

    private static PathNode GetCheapestNode(PathNode[] nodesNotEvaluated)
    {
        if (nodesNotEvaluated.Length == 0) { return null; }

        PathNode selectedNode = nodesNotEvaluated[0];

        for (int i = 1; i < nodesNotEvaluated.Length; i++)
        {
            var currentNode = nodesNotEvaluated[i];
            if (currentNode.GetCost() < selectedNode.GetCost())
            {
                selectedNode = currentNode;
            }
            
            else if (currentNode.GetCost() == selectedNode.GetCost() &&
                currentNode.costToDestination < selectedNode.costToDestination)
            {
                selectedNode = currentNode;
            }
        }

        return selectedNode;
    }
}
