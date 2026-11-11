using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class PolvoAfiche : MaskableGraphic
{
    private void Update() => SetVerticesDirty();
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        float t = Application.isPlaying ? Time.unscaledTime : 0;
        Rect r = rectTransform.rect;
        const int cantidad = 20;
        for (int i = 0; i < cantidad; i++)
        {
            float semilla = i * 2.39996f;
            float u = Mathf.Repeat(i * .618034f + t * (.003f + i * .00009f), 1f);
            float v = Mathf.Repeat(i * .41421f + t * .009f, 1f);
            float x = r.x + u * r.width;
            float y = r.y + v * r.height + Mathf.Sin(t * .35f + semilla) * 6;
            // No pasar partículas sobre las etiquetas de los botones.
            if (u < .38f && v < .48f) continue;
            float tam = 1.2f + (i % 3) * .5f;
            Color c = new Color(1, .84f, .58f, .07f + .12f * Mathf.Pow(Mathf.Sin(t * .4f + semilla), 2));
            int n = vh.currentVertCount;
            vh.AddVert(new Vector3(x - tam, y - tam), c, Vector2.zero);
            vh.AddVert(new Vector3(x - tam, y + tam), c, Vector2.zero);
            vh.AddVert(new Vector3(x + tam, y + tam), c, Vector2.zero);
            vh.AddVert(new Vector3(x + tam, y - tam), c, Vector2.zero);
            vh.AddTriangle(n, n + 1, n + 2); vh.AddTriangle(n, n + 2, n + 3);
        }
    }
}
