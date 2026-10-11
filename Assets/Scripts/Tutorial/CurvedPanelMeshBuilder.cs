using UnityEngine;

// Generates a horizontally curved panel from physical radius and design-space dimensions.
public static class CurvedPanelMeshBuilder
{
    public static void Rebuild(Mesh mesh, float width, float height, float radius,
        float canvasScale, int columns, int rows)
    {
        columns = Mathf.Max(1, columns);
        rows = Mathf.Max(1, rows);
        int vertexColumns = columns + 1;
        Vector3[] vertices = new Vector3[vertexColumns * (rows + 1)];
        Vector2[] uv = new Vector2[vertices.Length];
        Color[] colors = new Color[vertices.Length];
        int[] triangles = new int[columns * rows * 6];
        for (int y = 0; y <= rows; y++)
        {
            float v = y / (float)rows;
            for (int x = 0; x <= columns; x++)
            {
                float u = x / (float)columns;
                float angle = (u - 0.5f) * width * canvasScale / radius;
                int index = y * vertexColumns + x;
                vertices[index] = new Vector3(radius * Mathf.Sin(angle) / canvasScale,
                    (v - 0.5f) * height,
                    radius * (Mathf.Cos(angle) - 1f) / canvasScale);
                uv[index] = new Vector2(u, v);
                colors[index] = Color.white;
            }
        }
        int triangle = 0;
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                int a = y * vertexColumns + x;
                int b = a + vertexColumns;
                int c = a + 1;
                int d = b + 1;
                triangles[triangle++] = a;
                triangles[triangle++] = b;
                triangles[triangle++] = c;
                triangles[triangle++] = b;
                triangles[triangle++] = d;
                triangles[triangle++] = c;
            }
        }
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.colors = colors;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }
}
