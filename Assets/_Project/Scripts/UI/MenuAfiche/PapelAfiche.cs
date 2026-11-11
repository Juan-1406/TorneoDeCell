using UnityEngine;
using UnityEngine.UI;

/// <summary>Hoja con borde rasgado para cubrir y descubrir una escena.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class PapelAfiche : MaskableGraphic
{
    public float Izquierda { get; set; }
    public float Derecha { get; set; }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = rectTransform.rect;
        const int tiras = 44;
        for (int i = 0; i <= tiras; i++)
        {
            float y = Mathf.Lerp(r.yMin, r.yMax, i / (float)tiras);
            float diente = Mathf.Sin(i * 7.13f) * 10 + Mathf.Sin(i * 2.31f) * 5;
            float a = Mathf.Lerp(r.xMin - 35, r.xMax + 35, Izquierda) + diente;
            float b = Mathf.Lerp(r.xMin - 35, r.xMax + 35, Derecha) + diente;
            Color c = color * (1 - .012f * Mathf.Sin(i * 1.3f)); c.a = color.a;
            vh.AddVert(new Vector3(a, y), c, Vector2.zero);
            vh.AddVert(new Vector3(b, y), c, Vector2.zero);
            if (i == 0) continue;
            int n = i * 2;
            vh.AddTriangle(n - 2, n, n - 1); vh.AddTriangle(n - 1, n, n + 1);
        }
    }
    public void Actualizar(float izquierda, float derecha)
    {
        Izquierda = izquierda; Derecha = derecha; SetVerticesDirty();
    }
}
