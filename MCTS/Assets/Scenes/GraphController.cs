using System;
using System.Collections.Generic;
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
        RunLayout(Time.deltaTime);
        //HandleClickToAddChild();

        //if (Input.GetKeyDown(KeyCode.Space))
        //{
        //    // Deserialize Assets/Data/mcts_tree.json to MctcTree class

        //    TextAsset textFile = (TextAsset)Resources.Load("mcts_tree");
        //    var all = Resources.LoadAll(".");

        //    MctcTree tree = JsonUtility.FromJson<MctcTree>(textFile.text);

        //    Debug.Log($"Loaded tree with {tree.nodes.Count} nodes and {tree.edges.Count} edges");

        //    // json doesn't like nulls, so we use -1 instead of nullable ints
        //}

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            AddNextNode();
        }
        if (Input.GetKey(KeyCode.Space))
        {
            AddNextNode();
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
        if (tree == null) return;

        if (nodes.Count == 0)
        {
            var rootTreeNode = tree.nodes.Find(n => n.parentId == -1);
            var newNode = AddTreeNode(null, null, transform, rootTreeNode);
        }
        else
        {
            for (int i = 0; i < tree.nodes.Count; i++)
            {
                var tn = tree.nodes[i];
                // Check if this node is already created
                bool exists = nodes.Exists(n => n.name == $"Node_id_{tn.id}");
                if (exists) continue;

                // Find parent node in scene
                var parentNode = nodes.Find(n => n.name == $"Node_id_{tn.parentId}");
                if (parentNode == null)
                {
                    Debug.LogWarning($"Parent node with id {tn.parentId} not found for node id {tn.id}");
                    continue;
                }

                // Create new node
                Vector3 spawnPos = parentNode.transform.position + UnityEngine.Random.onUnitSphere * 0.8f;
                AddTreeNode(parentNode, spawnPos, transform, tn);
                break;
            }
        }

        totalNodesText.text = $"Total Nodes: {nodes.Count}";
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

        Debug.Log($"Node id {node.id} critic {node.critic} color {sphereColor} normalized critic score: {tree.NormalizeCriticScore(node.critic)}");
        Debug.Log($"blue {Color.blue}");

        newNode.SetColor(parentNode != null ? sphereColor : Color.magenta);

        nodes.Add(newNode);

        if (parentNode != null)
        {
            // Create edge
            var edge = Instantiate(edgePrefab, transform);
            edge.a = parentNode;
            edge.b = newNode;
            edges.Add(edge);

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
            Vector3 f = springK * ext * dir;           // Hooke�s law
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
