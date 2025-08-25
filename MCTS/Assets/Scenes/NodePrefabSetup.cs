using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MctsNodeSphere))]
public class NodePrefabSetup : MonoBehaviour
{
    void Reset()
    {
        var r = GetComponent<Renderer>();
        if (!r)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(transform, false);
            sphere.transform.localScale = Vector3.one * 0.4f;
            DestroyImmediate(sphere.GetComponent<SphereCollider>());
        }

        if (!GetComponent<SphereCollider>())
        {
            var col = gameObject.AddComponent<SphereCollider>();
            col.radius = 0.3f;
        }

        var rend = GetComponentInChildren<Renderer>();
        if (rend && rend.sharedMaterial == null)
        {
            rend.sharedMaterial = new Material(Shader.Find("Standard"));
        }
    }
}