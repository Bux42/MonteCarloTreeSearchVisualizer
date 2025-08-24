using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class MctsNode : MonoBehaviour
{
    [Header("Graph")]
    public MctsNode parent;
    public List<MctsNode> children = new List<MctsNode>();

    [Header("Physics (runtime)")]
    [HideInInspector] public Vector3 velocity;

    // Optional: tint the node by depth for readability
    public void SetDepthColor(int depth)
    {
        var r = GetComponent<Renderer>();
        if (!r) return;
        float t = Mathf.InverseLerp(0, 8, depth);
        r.material.color = Color.Lerp(Color.white, new Color(0.7f, 0.9f, 1f), t);
    }

    public int GetDepth()
    {
        int d = 0;
        var cur = parent;
        while (cur != null) { d++; cur = cur.parent; }
        return d;
    }
}