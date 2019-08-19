using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TacticalPoint
{
    public Vector3 pos;
    public bool used = false;
    public float coverValue = 0;
    public float coverPoint = 0;

    public TacticalPoint(Vector3 pos)
    {
        this.pos = pos;
    }
}

public class TacticalGraph : MonoBehaviour
{
    public static List<TacticalPoint> graph;

    private GameObject[] players = null;

    void Awake()
    {
        CreateGraph();
    }

    /// <summary>
    /// Creates a graph from all of the pov nodes
    /// </summary>
    private void CreateGraph()
    {
        graph = new List<TacticalPoint>();

        foreach (Transform t in GetComponentsInChildren<Transform>()) //retrieves all the cubes that are child of this object
        {
            if (t == transform || t.localPosition == Vector3.zero)
                continue;

            graph.Add(new TacticalPoint(t.position));  //add it to the graph
        }
    }

    private void Update()
    {
        players = GameObject.FindGameObjectsWithTag("Player");
        if (players.Length == 0)
            return;

        LayerMask mask = 1 << 0;

        foreach (TacticalPoint p in graph)
        {
            p.coverValue = 0f;
            p.coverPoint = 0f;
            foreach (GameObject t in players)
            {
                Vector3 tmp = t.transform.position + new Vector3(0, 1, 0);
                Vector3 dest = p.pos + new Vector3(0, 2, 0);

                if (Physics.Raycast(p.pos, tmp - p.pos, out RaycastHit hitHid, Vector3.Distance(p.pos, tmp), mask))
                {
                    p.coverValue += 1f / (float)players.Length;
                    p.coverPoint += 1f / (float)players.Length;
                }
                if (Physics.Raycast(dest, tmp - dest, out RaycastHit hitVis, Vector3.Distance(dest, tmp), mask))
                {
                    p.coverValue -= (1f / (float)players.Length) / 2f;
                }
            }
        }
    }

    public static TacticalPoint GetClosestCoverPoint(Vector3 position)
    {
        TacticalPoint tmp = null;
        float minDist = Mathf.Infinity;
        float cover = -1f;

        foreach (TacticalPoint p in graph)
        {
            if (p.used)
                continue;
            float dist = Vector3.Distance(position, p.pos);
            if (p.coverPoint > cover || (p.coverPoint == cover && dist < minDist))
            {
                cover = p.coverPoint;
                minDist = dist;
                tmp = p;
            }
        }
        if (tmp != null)
            return tmp;
        return null;
    }

    public static TacticalPoint GetClosestAndBestCoverPoint(Vector3 position)
    {
        TacticalPoint tmp = null;
        float minDist = Mathf.Infinity;
        float cover = -1f;

        foreach (TacticalPoint p in graph)
        {
            if (p.used)
                continue;
            float dist = Vector3.Distance(position, p.pos);
            if (p.coverValue > cover || (p.coverValue == cover && dist < minDist))
            {
                cover = p.coverValue;
                minDist = dist;
                tmp = p;
            }
        }
        if (tmp != null)
            return tmp;
        return null;
    }

    public static float HighestTacticalValue()
    {
        float cover = -1f;

        foreach (TacticalPoint p in graph)
        {
            if (p.used)
                continue;
            if (p.coverValue > cover)
            {
                cover = p.coverValue;
            }
        }
        return cover;
    }

    /// <summary>
    /// Draws the graph on the screen when in editor view and pressing on the povGraph object
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (graph == null || graph.Count == 0)
            CreateGraph();
        Vector3 size = new Vector3(0.4f, 0.2f, 0.4f);

        Gizmos.color = Color.red;
        foreach (TacticalPoint point in graph)
        {
            Gizmos.DrawCube(point.pos, size);
        }
    }
}
