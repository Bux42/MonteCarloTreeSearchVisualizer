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

    bool emissive = false;
    static float initialEmissionIntensity = 10.0f;
    float currentEmissionIntensity = initialEmissionIntensity;
    public Color color;

    private void Start()
    {
        ////rendered.material.color = color;
        //rendered = GetComponent<MeshRenderer>();
        //rendered.materials[0].SetColor("_Color", Color.blue);

        rendered = GetComponent<MeshRenderer>();
    }

    private void Update()
    {
        // early return 
        if (currentEmissionIntensity <= .2f)
        {
            return;
        }

        rendered = GetComponent<MeshRenderer>();

        if (emissive && rendered != null)
        {
            // lower emission to 0, disable emission if close to 0

            if (currentEmissionIntensity > .2f)
            {
                currentEmissionIntensity *= 0.98f;
                rendered.materials[0].SetColor("_EmissionColor", this.color * currentEmissionIntensity);
            }
        }

        if (rendered == null)
        {
            Debug.LogWarning("Renderer is null, cannot update emission color");
        }
    }

    // Optional: tint the node by depth for readability
    public void SetDepthColor(int depth)
    {
        var r = GetComponent<Renderer>();
        if (!r) return;
        float t = Mathf.InverseLerp(0, 8, depth);
        r.material.color = Color.Lerp(Color.white, new Color(0.7f, 0.9f, 1f), t);
    }

    public Color GetColor()
    {
        return this.color;
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
        this.color = color;

        rendered = GetComponent<MeshRenderer>();
        if (rendered != null)
        {
            rendered.materials[0].SetColor("_Color", color);
        }
        else
        {
            Debug.LogWarning("Renderer is null, cannot set color");
        }
    }

    public void SetEmissionColor(Color color)
    {
        rendered = GetComponent<MeshRenderer>();
        if (rendered != null)
        {
            currentEmissionIntensity = initialEmissionIntensity;
            rendered.materials[0].EnableKeyword("_EMISSION");
            rendered.materials[0].SetColor("_EmissionColor", color * currentEmissionIntensity);

            emissive = true;
        }
        else
        {
            Debug.LogWarning("Renderer is null, cannot set emission color");
        }
    }
}