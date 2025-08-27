using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class GraphController : MonoBehaviour
{
    [Header("Prefabs")]
    public MctsNodeSphere nodePrefab;
    public EdgeRenderer edgePrefab;

    [Header("Physics (layout)")]
    public float repulsion = 50.0f;       // higher = stronger push between all nodes
    public float springK = 400.0f;        // edge spring strength
    public float restLength = 1.5f;      // preferred edge length
    public float damping = 0.99f;         // 0..1 velocity damping each frame
    public float maxSpeed = 2f;         // clamp for stability
    public float centerPull = 0.2f;      // gentle pull toward origin

    [Header("Camera")]
    public Camera mainCamera;

    List<MctsNodeSphere> nodes = new List<MctsNodeSphere>();
    List<EdgeRenderer> edges = new List<EdgeRenderer>();

    MctcTree tree = null;
    Gradient nodeColorGradient = new Gradient();

    TMPro.TextMeshProUGUI totalNodesText = null;

    Vector3[] pos, vel; // caches
    // Uniform grid (spatial hash): cell -> list of indices
    readonly Dictionary<Vector3Int, List<int>> grid = new Dictionary<Vector3Int, List<int>>(1024);
    float cellSize;
    public List<EdgeIdx> edgesIdxs = new List<EdgeIdx>(); // integer index edges
    public float cutoff = 10f; // neighbors beyond this distance ignored
    float cutoff2;

    public struct EdgeIdx { public int a, b; }

    void Awake()
    {
        cutoff2 = cutoff * cutoff;
        cellSize = cutoff;
    }

    // Start is called before the first frame update
    void Start()
    {
        //// Create root
        //var root = Instantiate(nodePrefab, Vector3.zero, Quaternion.identity, transform);
        //root.gameObject.name = "Root";
        //root.SetDepthColor(0);
        //nodes.Add(root);

        var colors = new GradientColorKey[3];

        colors[0] = new GradientColorKey(Color.red, 0.0f);
        colors[1] = new GradientColorKey(Color.yellow, 0.5f);
        colors[2] = new GradientColorKey(Color.green, 1.0f);

        var alphas = new GradientAlphaKey[3];
        alphas[0] = new GradientAlphaKey(1.0f, 1.0f);
        alphas[1] = new GradientAlphaKey(1.0f, 1.0f);
        alphas[2] = new GradientAlphaKey(1.0f, 1.0f);

        nodeColorGradient.SetKeys(colors, alphas);

        LoadTreeFile();


        var totalNodesGo = GameObject.Find("TotalNodesText");
        if (totalNodesGo != null)
        {
            totalNodesText = totalNodesGo.GetComponent<TMPro.TextMeshProUGUI>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        UpdateTreeNodePositions(Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            AddNextNode();
        }
        if (Input.GetKey(KeyCode.Space))
        {
            AddNextNode();
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            // Reset
            nodes.Clear();
            edges.Clear();
            edgesIdxs.Clear();
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }
        if (Input.GetKey(KeyCode.H))
        {
            ShowControls();
        }
        else
        {
            HideControls();
        }
    }

    void ShowControls()
    {
        // ControlsText textmeshpro set visible / active

        var go = GameObject.Find("ControlsText");
        if (go != null)
        {
            var text = go.GetComponent<TMPro.TextMeshProUGUI>();
            if (text != null)
            {
                text.enabled = true;
            }
        }
    }

    void HideControls()
    {
        // ControlsText textmeshpro set visible / active

        var go = GameObject.Find("ControlsText");
        if (go != null)
        {
            var text = go.GetComponent<TMPro.TextMeshProUGUI>();
            if (text != null)
            {
                text.enabled = false;
            }
        }
    }

    void AddNextNode()
    {
        if (tree == null || tree.nodes.Count == nodes.Count) return;

        DateTime before = DateTime.Now;

        if (nodes.Count == 0)
        {
            var rootTreeNode = tree.nodes.Find(n => n.parentId == -1);
            var newNode = AddTreeNode(null, null, transform, rootTreeNode);
        }
        else
        {
            var tn = tree.nodes[nodes.Count];
            // Find parent node in scene
            var parentNode = nodes.Find(n => n.name == $"Node_id_{tn.parentId}");

            if (parentNode == null)
            {
                Debug.LogWarning($"Parent node with id {tn.parentId} not found for node id {tn.id}");
            }
            else
            {
                // Create new node
                Vector3 spawnPos = parentNode.transform.position + UnityEngine.Random.onUnitSphere * 0.8f;

                Debug.Log($"Add node at index: {nodes.Count} Total nodes: {nodes.Count} / {tree.nodes.Count}");
                AddTreeNode(parentNode, spawnPos, transform, tn);
            }
        }

        totalNodesText.text = $"Total Nodes: {nodes.Count} / {tree.nodes.Count}";

        DateTime after = DateTime.Now;
        TimeSpan duration = after.Subtract(before);
        Debug.Log("AddNextNode ms: " + duration.Milliseconds);
    }

    List<EdgeRenderer> GetEdgesToRoot(MctsNodeSphere currentNode)
    {
        List<EdgeRenderer> edgesToRoot = new List<EdgeRenderer>();

        while (currentNode.parent != null)
        {
            edgesToRoot.Add(edges.Find(x => x.b == currentNode));
            currentNode = currentNode.parent;
        }

        return edgesToRoot;
    }

    MctsNodeSphere AddTreeNode(MctsNodeSphere? parentNode, Vector3? position, Transform transform, Node node)
    {
        DateTime before = DateTime.Now;

        Vector3 pos = position ?? Vector3.zero;

        var newNode = Instantiate(nodePrefab, pos, Quaternion.identity, transform);

        newNode.gameObject.name = $"Node_id_{node.id}";
        newNode.parent = parentNode;

        if (parentNode != null)
        {
            parentNode.children.Add(newNode);
        }

        //newNode.SetDepthColor(newNode.GetDepth());

        //Color sphereColor = CriticToColor(tn.critic, tree.minCriticScore, tree.maxCriticScore);
        Color sphereColor = nodeColorGradient.Evaluate((float)tree.NormalizeCriticScore(node.critic));

        //Debug.Log($"Node id {node.id} critic {node.critic} color {sphereColor} normalized critic score: {tree.NormalizeCriticScore(node.critic)}");
        //Debug.Log($"blue {Color.blue}");

        Color nodeColor = sphereColor;

        if (parentNode == null)
        {
            nodeColor = Color.magenta; // root node
        }

        if (node.actionTerminated)
        {
            nodeColor = Color.cyan; // terminal node
        }

        newNode.SetColor(nodeColor);

        nodes.Add(newNode);

        if (parentNode != null)
        {
            // Create edge
            var edge = Instantiate(edgePrefab, transform);
            edge.a = parentNode;
            edge.b = newNode;
            edges.Add(edge);

            edgesIdxs.Add(new EdgeIdx { a = nodes.IndexOf(parentNode), b = nodes.IndexOf(newNode) });

            edge.SetColor(parentNode.color, newNode.color);
            edge.SetEmissionColor(newNode.color);
            edge.SetLineWidth(0.2f);
        }

        // make it glow
        newNode.SetEmissionColor(sphereColor);
        var tmpNode = newNode;

        while (tmpNode.parent != null)
        {
            tmpNode = tmpNode.parent;
            tmpNode.SetEmissionColor(tmpNode.GetColor());
        }

        List<EdgeRenderer> edgesToRoot = GetEdgesToRoot(newNode);

        foreach (var e in edgesToRoot)
        {
            e.SetEmissionColor(e.GetColor());
            e.SetLineWidth(0.1f);
        }

        DateTime after = DateTime.Now;
        TimeSpan duration = after.Subtract(before);
        Debug.Log("AddTreeNode ms: " + duration.Milliseconds);

        return newNode;
    }

    Color CriticToColor(double value, double vmin = -200, double vmax = 200)
    {
        // clamp
        double v = Math.Max(Math.Min(value, vmax), vmin);
        // normalize [0..1]
        double norm = (v - vmin) / (vmax - vmin);

        if (norm < 0.5)
        {
            // red -> white
            float t = (float)(norm / 0.5);
            return new Color(1f, t, t);
        }
        else
        {
            // white -> green
            float t = (float)((norm - 0.5) / 0.5);
            return new Color(1f - t, 1f, 1f - t);
        }
    }

    void LoadTreeFile()
    {
        // Deserialize Assets/Data/mcts_tree.json to MctcTree class

        TextAsset textFile = (TextAsset)Resources.Load("mcts_tree");
        var all = Resources.LoadAll(".");

        tree = JsonUtility.FromJson<MctcTree>(textFile.text);

        Debug.Log($"Loaded tree with {tree.nodes.Count} nodes and {tree.edges.Count} edges");
        tree.SetMinMaxValues();
        // json doesn't like nulls, so we use -1 instead of nullable ints
    }

    void UpdateTreeNodePositions(float dt)
    {
        int n = nodes.Count;
        if (n == 0 || edgesIdxs.Count == 0) return;

        // resize caches
        if (pos == null || pos.Length < n)
        {
            pos = new Vector3[n];
            vel = new Vector3[n];
        }

        // snapshot positions/velocities once
        for (int i = 0; i < n; i++)
        {
            pos[i] = nodes[i].transform.position;
            vel[i] = nodes[i].velocity;
        }

        // rebuild grid
        grid.Clear();
        for (int i = 0; i < n; i++)
        {
            var c = ToCell(pos[i]);
            if (!grid.TryGetValue(c, out var list))
            {
                list = new List<int>(8);
                grid[c] = list;
            }
            list.Add(i);
        }

        // pairwise repulsion using neighbor cells only
        foreach (var kv in grid)
        {
            var cell = kv.Key;
            var listA = kv.Value;

            // iterate current cell vs itself (i<j)
            for (int ii = 0; ii < listA.Count; ii++)
            {
                int i = listA[ii];
                for (int jj = ii + 1; jj < listA.Count; jj++)
                {
                    int j = listA[jj];
                    ApplyRepulsion(i, j, dt);
                }
            }

            // iterate against 26 neighbor cells
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        if (dx == 0 && dy == 0 && dz == 0) continue;
                        var nb = new Vector3Int(cell.x + dx, cell.y + dy, cell.z + dz);
                        if (!grid.TryGetValue(nb, out var listB)) continue;

                        // cross pairs (all of A vs all of B)
                        for (int ii = 0; ii < listA.Count; ii++)
                        {
                            int i = listA[ii];
                            for (int jj = 0; jj < listB.Count; jj++)
                            {
                                int j = listB[jj];
                                if (j <= i) continue; // avoid duplicates & self
                                ApplyRepulsion(i, j, dt);
                            }
                        }
                    }
        }

        // springs along edgesIdxs (already sparse)
        for (int e = 0; e < edgesIdxs.Count; e++)
        {
            int a = edgesIdxs[e].a;
            int b = edgesIdxs[e].b;

            Vector3 d = pos[b] - pos[a];
            float d2 = d.sqrMagnitude;
            if (d2 > cutoff2) continue; // skip far springs (optional)

            float dist = Mathf.Sqrt(d2) + 1e-6f;
            Vector3 dir = d / dist;
            float ext = dist - restLength;
            Vector3 f = springK * ext * dir;
            vel[a] += f * dt;
            vel[b] -= f * dt;
        }

        // center pull, damping, integrate, write back once
        for (int i = 0; i < n; i++)
        {
            vel[i] += -centerPull * pos[i] * dt;
            vel[i] *= damping;
            if (vel[i].sqrMagnitude > maxSpeed * maxSpeed)
                vel[i] = vel[i].normalized * maxSpeed;

            pos[i] += vel[i] * dt;

            nodes[i].velocity = vel[i];
            nodes[i].transform.position = pos[i];
        }
    }

    [MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    void ApplyRepulsion(int i, int j, float dt)
    {
        Vector3 d = pos[i] - pos[j];
        float d2 = d.sqrMagnitude + 1e-6f;
        if (d2 > cutoff2) return;                // cheap reject

        // 1/r^2, soften at very small r
        float invDist = Mathf.Sqrt(1f / d2);     // 1/sqrt(d2)
        Vector3 dir = d * invDist;               // normalized delta
        float force = repulsion / d2;
        Vector3 f = dir * force;
        vel[i] += f * dt;
        vel[j] -= f * dt;
    }

    [MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    Vector3Int ToCell(Vector3 p)
    {
        return new Vector3Int(
            Mathf.FloorToInt(p.x / cellSize),
            Mathf.FloorToInt(p.y / cellSize),
            Mathf.FloorToInt(p.z / cellSize)
        );
    }


    void HandleClickToAddChild()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out var hit, 1000f)) return;

        var node = hit.collider.GetComponentInParent<MctsNodeSphere>();
        if (!node) return;

        Debug.Log($"Add child to {node.name}");

        AddChild(node);
    }

    public void AddChild(MctsNodeSphere parent)
    {
        // Spawn near parent with slight random offset
        Vector3 spawnPos = parent.transform.position + UnityEngine.Random.onUnitSphere * 0.8f;
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
