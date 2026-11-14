using UnityEngine;

/// <summary>A continuous UV strip folded over a shelf edge, with its head on top.</summary>
[ExecuteAlways, RequireComponent(typeof(MeshFilter))]
public sealed class FoldedClimbStroke : MonoBehaviour
{
    public float width = 0.2f;
    public float length = 0.3287f;
    [Range(0.15f, 0.85f)] public float verticalFraction = 0.5f;
    public float lateralSlant = 0.12f;
    private Mesh generated;
    private Mesh original;

    private void OnEnable() { Rebuild(); }
    private void OnValidate() { if (isActiveAndEnabled) Rebuild(); }

    private void Rebuild()
    {
        var filter = GetComponent<MeshFilter>();
        if (!generated)
        {
            original = filter.sharedMesh;
            generated = new Mesh { name = "Shelf edge folded trace", hideFlags = HideFlags.HideAndDontSave };
        }
        float total = Mathf.Max(0.01f, length);
        float split = Mathf.Clamp(verticalFraction, 0.15f, 0.85f);
        float side = Mathf.Max(0.01f, width) * 0.5f;
        var vertices = new Vector3[6];
        var uv = new Vector2[6];
        for (int row = 0; row < 3; row++)
        {
            float v = row == 0 ? 0f : row == 1 ? split : 1f;
            float along = (v - split) * total;
            for (int col = 0; col < 2; col++)
            {
                int i = row * 2 + col;
                // Local +X points into the shelf; +Y is up; Z runs along its edge.
                vertices[i] = new Vector3(Mathf.Max(0f, along), Mathf.Min(0f, along),
                    (col == 0 ? -side : side) + along * lateralSlant);
                uv[i] = new Vector2(col, v);
            }
        }
        generated.Clear();
        generated.vertices = vertices;
        generated.uv = uv;
        generated.triangles = new[] { 0, 2, 1, 1, 2, 3, 2, 4, 3, 3, 4, 5 };
        generated.RecalculateNormals();
        generated.RecalculateBounds();
        filter.sharedMesh = generated;
    }

    private void OnDisable()
    {
        var filter = GetComponent<MeshFilter>();
        if (filter && filter.sharedMesh == generated) filter.sharedMesh = original;
        if (generated)
        {
            if (Application.isPlaying) Destroy(generated);
            else DestroyImmediate(generated);
        }
        generated = null;
    }
}
