using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Determines how to show the gizmos of the graphs
/// </summary>
public enum ColorVue
{
    CONNECTIONS,
    PATH,
    NONE
}

/// <summary>
/// Determines the state of the nodes
/// </summary>
public enum nodeState
{
    NONE,
    PATH,
    SMOOTH_PATH,
    OPEN,
    START_END,
}

/// <summary>
/// Structure storing the distance to the neighbour as well as its gridPoint
/// </summary>
public struct Connection
{
    public float distance;
    public GridPoint neighbour;

    public Connection(float distance, GridPoint neighbour)
    {
        this.distance = distance;
        this.neighbour = neighbour;
    }
}

/// <summary>
/// Class containing validity information, all connections, the position, to which cluster it belongs, as well as the costSoFar, and prev for A*
/// </summary>
public class GridPoint
{
    public bool valid;
    public List<Connection> connections;
    public Vector3 pos = Vector3.zero;

    public float seen = 0;
    public float seenSoFar = 0;

    public float costSoFar;
    public GridPoint prev;

    public nodeState nodeState;

    public GridPoint(bool valid, Vector3 pos)
    {
        this.valid = valid;
        this.pos = pos;

        costSoFar = 0;
        nodeState = nodeState.NONE;

        if (valid)
            connections = new List<Connection>();
        else
            connections = null;
    }
}

/// <summary>
/// List of all GridPoints
/// </summary>
public class Graph
{
    public List<GridPoint> nodes;

    public Graph(GridPoint[,] grid)
    {
        nodes = new List<GridPoint>();

        for (int x = 0; x < grid.GetLength(0); x++)
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                if (grid[x, y].valid && grid[x, y].connections.Count > 0)
                {
                    nodes.Add(grid[x, y]);
                }
            }
    }

    public Graph()
    {
        nodes = new List<GridPoint>();
    }
}
