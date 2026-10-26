using UnityEngine;
using UnityEngine.UI;

/// <summary>Marco de interfaz: contorno fino con punta recortada, sin textura estirada.</summary>
[AddComponentMenu("Jam3/Menu/Marco de boton")]
[RequireComponent(typeof(CanvasRenderer))]
public sealed class MENU_ButtonFrame : MaskableGraphic
{
    [SerializeField, Min(1f)] private float lineWidth = 1.7f;
    [SerializeField, Min(0f)] private float cutCorner = 20f;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        float cut = Mathf.Min(cutCorner, r.height * .45f);
        Vector2[] points = {
            new Vector2(r.xMin + 5f, r.yMax), new Vector2(r.xMax - cut, r.yMax),
            new Vector2(r.xMax, r.center.y), new Vector2(r.xMax - cut, r.yMin),
            new Vector2(r.xMin + 5f, r.yMin), new Vector2(r.xMin, r.yMin + 5f),
            new Vector2(r.xMin, r.yMax - 5f)
        };
        for (int i = 0; i < points.Length; i++)
        {
            Vector2 a = points[i], b = points[(i + 1) % points.Length];
            Vector2 n = new Vector2(-(b - a).y, (b - a).x).normalized;
            AddLine(vh, a, b, n, lineWidth * 4f, new Color(color.r, color.g, color.b, color.a * .10f));
            AddLine(vh, a, b, n, lineWidth, color);
        }
    }

    private static void AddLine(VertexHelper vh, Vector2 a, Vector2 b, Vector2 n, float width, Color tint)
    {
        int index = vh.currentVertCount;
        Vector2 v = n * width * .5f;
        vh.AddVert(a - v, tint, Vector2.zero); vh.AddVert(a + v, tint, Vector2.zero);
        vh.AddVert(b + v, tint, Vector2.zero); vh.AddVert(b - v, tint, Vector2.zero);
        vh.AddTriangle(index, index + 1, index + 2); vh.AddTriangle(index, index + 2, index + 3);
    }
}
