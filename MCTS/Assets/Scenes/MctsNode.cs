
using System;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class Edge
{
    public int source;
    public int target;
}

[System.Serializable]
public class Meta
{
    public int rootId;
    public DateTime generatedAt;
    public int nodeCount;
    public int edgeCount;
    public object maxDepth;
    public object maxNodes;
}

[System.Serializable]
public class Node
{
    public int id;
    public int parentId;
    public int depth;
    public int move;
    public double critic;
    public int visits;
    public double wins;
    public bool terminated;
    public bool actionTerminated;
    public int iteration;
    public bool fightWon;
    public List<int> children;
}

[System.Serializable]
public class MctcTree
{
    public Meta meta;
    public List<Node> nodes;
    public List<Edge> edges;
    public double minCriticScore;
    public double maxCriticScore;
    public double minDepth;
    public double maxDepth;

    public void SetMinMaxValues()
    {
        minCriticScore = nodes.Min(x => x.critic);
        maxCriticScore = nodes.Max(x => x.critic);

        minDepth = nodes.Min(x => x.depth);
        maxDepth = nodes.Max(x => x.depth);
    }

    public double NormalizeCriticScore(double score)
    {
        return (score - minCriticScore) / (maxCriticScore - minCriticScore);
    }

    public double NormalizeDepth(double depth)
    {
        return (depth - minDepth) / (maxDepth - minDepth);
    }
}

public class MctsNode
{

}
