using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class MctsNodeSphere : MonoBehaviour
{

    [Header("Graph")]
    public MctsNodeSphere parent;
    public List<MctsNodeSphere> children = new List<MctsNodeSphere>();

    [Header("Physics (runtime)")]
    [HideInInspector] public Vector3 velocity;

    UnityEngine.MeshRenderer rendered;


    private void Start()
    {
        ////rendered.material.color = color;
        //rendered = GetComponent<MeshRenderer>();
        //rendered.materials[0].SetColor("_Color", Color.blue);
    }
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

    public void SetColor(Color color)
    {
        rendered = GetComponent<MeshRenderer>();
        if (rendered != null)
        {
            //StandardMaterial
            //rendered.material.color = color;
            rendered.materials[0].SetColor("_EmissionColor", color);
            rendered.materials[0].SetColor("_Color", color);
            rendered.materials[0].SetColor("_Albedo", color);
        }
        else
        {
            Debug.LogWarning("Renderer is null, cannot set color");
        }
    }
}