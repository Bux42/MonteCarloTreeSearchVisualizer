using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class EdgeRenderer : MonoBehaviour
{
    public MctsNodeSphere a;
    public MctsNodeSphere b;

    LineRenderer lr;

    bool emissive = false;
    static float defaultLineWidth = 0.02f;
    float currentLineWidth = 0.02f;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.widthMultiplier = 0.02f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = Color.gray;
        lr.endColor = Color.gray;
    }

    private void Update()
    {
        if (emissive)
        {
            // lower emission to 0, disable emission if close to 0
            Color c = lr.materials[0].GetColor("_EmissionColor");
            c *= 0.992f;

            if (c.maxColorComponent < 0.01f)
            {
                lr.materials[0].DisableKeyword("_EMISSION");
                emissive = false;
            }
            else
            {
                lr.materials[0].SetColor("_EmissionColor", c);
            }
        }

        if (currentLineWidth > defaultLineWidth)
        {
            currentLineWidth *= 0.99f;
            lr.startWidth = currentLineWidth;
            lr.endWidth = currentLineWidth;
        }
    }

    void LateUpdate()
    {
        if (!a || !b) return;
        lr.SetPosition(0, a.transform.position);
        lr.SetPosition(1, b.transform.position);
    }

    public void SetColor(Color color)
    {
        lr.materials[0].SetColor("_Color", color);
    }

    public void SetLineWidth(float width)
    {
        currentLineWidth = width;

        lr.startWidth = currentLineWidth;
        lr.endWidth = currentLineWidth;
    }

    public Color GetColor()
    {
        return lr.materials[0].GetColor("_Color");
    }

    public void SetEmissionColor(Color color)
    {
        if (lr != null)
        {
            lr.materials[0].EnableKeyword("_EMISSION");
            lr.materials[0].SetColor("_EmissionColor", color * 4);

            emissive = true;
        }
        else
        {
            Debug.LogWarning("Renderer is null, cannot set emission color");
        }
    }
}