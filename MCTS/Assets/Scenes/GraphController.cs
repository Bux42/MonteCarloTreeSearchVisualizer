using System.Collections.Generic;
using UnityEngine;

public class GraphController : MonoBehaviour
{
    [Header("Prefabs")]
    public MctsNode nodePrefab;
    public EdgeRenderer edgePrefab;

    [Header("Physics (layout)")]
    public float repulsion = 100.0f;       // higher = stronger push between all nodes
    public float springK = 10.0f;        // edge spring strength
    public float restLength = 1.5f;      // preferred edge length
    public float damping = 0.99f;         // 0..1 velocity damping each frame
    public float maxSpeed = 10f;         // clamp for stability
    public float centerPull = 0.2f;      // gentle pull toward origin

    [Header("Camera")]
    public Camera mainCamera;

    List<MctsNode> nodes = new List<MctsNode>();
    List<EdgeRenderer> edges = new List<EdgeRenderer>();

    GameObject lookAtFocus = null; // optional focus object for camera

    // Start is called before the first frame update
    void Start()
    {
        // Create root
        var root = Instantiate(nodePrefab, Vector3.zero, Quaternion.identity, transform);
        root.gameObject.name = "Root";
        root.SetDepthColor(0);
        nodes.Add(root);

        //mainCamera.transform.LookAt(root.transform.position);
    }

    // Update is called once per frame
    void Update()
    {
        RunLayout(Time.deltaTime);
        HandleClickToAddChild();

        //// look at each node, switch to next node when tab is pressed
        //if (Input.GetKeyDown(KeyCode.Tab))
        //{
        //    Debug.Log($"Tab pressed, {nodes.Count} nodes");
        //    if (lookAtFocus == null)
        //    {
        //        lookAtFocus = nodes[0].gameObject; // start with first node
        //        Debug.Log($"Current focus: {lookAtFocus.name}");

        //    }
        //    else
        //    {
        //        int currentIndex = nodes.IndexOf(lookAtFocus.GetComponent<MctsNode>());

        //        Debug.Log($"Current focus: {lookAtFocus.name} at index {currentIndex}");

        //        if (currentIndex < nodes.Count - 1)
        //        {
        //            lookAtFocus = nodes[currentIndex + 1].gameObject; // next node
        //        }
        //        else
        //        {
        //            Debug.Log($"Circle back to root node");
        //            lookAtFocus = nodes[0].gameObject; // reset focus to root node
        //        }
        //    }
        //    mainCamera.transform.LookAt(lookAtFocus ? lookAtFocus.transform.position : Vector3.zero);
        //    Debug.Log($"Switched focus to: {(lookAtFocus ? lookAtFocus.name : "None")} at position {(lookAtFocus ? lookAtFocus.transform.position : null)}");
        //}
    }

    void RunLayout(float dt)
    {
        // Pairwise repulsion (O(n^2), fine for a few hundred nodes)
        for (int i = 0; i < nodes.Count; i++)
        {
            var ni = nodes[i];
            for (int j = i + 1; j < nodes.Count; j++)
            {
                var nj = nodes[j];
                Vector3 delta = ni.transform.position - nj.transform.position;
                float d2 = delta.sqrMagnitude + 1e-6f;
                Vector3 dir = delta.normalized;
                float force = repulsion / d2; // 1/r^2
                Vector3 f = dir * force;
                ni.velocity += f * dt;
                nj.velocity -= f * dt;
            }
        }

        // Springs on edges
        foreach (var e in edges)
        {
            if (!e.a || !e.b) continue;
            Vector3 delta = e.b.transform.position - e.a.transform.position;
            float dist = delta.magnitude + 1e-6f;
            Vector3 dir = delta / dist;
            float ext = dist - restLength;             // positive if stretched
            Vector3 f = springK * ext * dir;           // Hooke’s law
            e.a.velocity += f * dt;
            e.b.velocity -= f * dt;
        }

        // Gentle pull to center + integrate
        foreach (var n in nodes)
        {
            n.velocity += -centerPull * n.transform.position * dt;

            // damping & clamp
            n.velocity *= damping;
            n.velocity = Vector3.ClampMagnitude(n.velocity, maxSpeed);

            n.transform.position += n.velocity * dt;
        }
    }

    void HandleClickToAddChild()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out var hit, 1000f)) return;

        var node = hit.collider.GetComponentInParent<MctsNode>();
        if (!node) return;

        Debug.Log($"Add child to {node.name}");

        AddChild(node);
    }

    public void AddChild(MctsNode parent)
    {
        // Spawn near parent with slight random offset
        Vector3 spawnPos = parent.transform.position + Random.onUnitSphere * 0.8f;
        var child = Instantiate(nodePrefab, spawnPos, Quaternion.identity, transform);
        child.gameObject.name = $"Node_{nodes.Count}";
        child.parent = parent;
        parent.children.Add(child);
        child.SetDepthColor(child.GetDepth());

        nodes.Add(child);

        var edge = Instantiate(edgePrefab, transform);
        edge.a = parent;
        edge.b = child;
        edges.Add(edge);
    }
}
