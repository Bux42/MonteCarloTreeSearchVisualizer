using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class EdgeRenderer : MonoBehaviour
{
    public MctsNodeSphere a;
    public MctsNodeSphere b;

    LineRenderer lr;

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

    void LateUpdate()
    {
        if (!a || !b) return;
        lr.SetPosition(0, a.transform.position);
        lr.SetPosition(1, b.transform.position);
    }
}