using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PovGraph : MonoBehaviour
{
    public static Graph graph;

    public ColorVue colorVue;
    public bool debug = false;
    public bool stop = false;

    /// <summary>
    /// Creates a graph from all of the pov nodes
    /// </summary>
    private void CreateGraph()
    {
        graph = new Graph();

        foreach (Transform t in GetComponentsInChildren<Transform>()) //retrieves all the cubes that are child of this object
        {
            if (t == transform || t.localPosition == Vector3.zero)
                continue;

            GridPoint tmp = new GridPoint(true, t.position);  //creates a valid gridpoint

            graph.nodes.Add(tmp);  //add it to the graph
        }
        foreach (GridPoint p in graph.nodes)  //this will create connections
        {
            foreach (GridPoint tmp in graph.nodes)
            {
                if (p == tmp || p.pos.y != tmp.pos.y)  //if they are identical, no connection
                {
                    continue;
                }
                LayerMask mask = 1 << 0;
                if (!Physics.CapsuleCast(p.pos, new Vector3(p.pos.x, p.pos.y + 1f, p.pos.z), 0.2f, tmp.pos - p.pos, out RaycastHit hit, Vector3.Distance(p.pos, tmp.pos), mask)) //checks if there is a collider between both nodes
                {
                    if (debug)
                        Debug.DrawLine(p.pos, tmp.pos, Color.red, 100000);
                    p.connections.Add(new Connection(Vector3.Distance(p.pos, tmp.pos), tmp));                                //if there isn't create a connection
                }
            }
        }
    }

    void Awake()
    {
        CreateGraph();
    }

    /// <summary>
    /// Draws the graph on the screen when in editor view and pressing on the povGraph object
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (graph == null || graph.nodes == null)
            CreateGraph();
        Vector3 size = new Vector3(0.4f, 0.2f, 0.4f);

        foreach (GridPoint point in graph.nodes)
        {
            if (point.valid == true)
            {
                if (colorVue == ColorVue.CONNECTIONS)
                    Gizmos.color = Misc.GetColorWithVar(point.connections.Count);
                else if (colorVue == ColorVue.PATH)
                    Gizmos.color = Misc.GetColorWithVar((int)point.nodeState + 2);
                else if (colorVue == ColorVue.NONE)
                    Gizmos.color = Color.red;

                Gizmos.DrawCube(point.pos, size);
                if (point.prev != null && (point.nodeState == nodeState.PATH || point.nodeState == nodeState.START_END) && colorVue == ColorVue.PATH)
                    Gizmos.DrawLine(point.prev.pos, point.pos);
            }
        }
    }
}
