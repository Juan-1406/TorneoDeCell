using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Recorta por geometría el afiche aprobado: no vuelve a dibujar sus personajes.</summary>
[AddComponentMenu("Torneo/Menu/Recorte de afiche")]
[RequireComponent(typeof(CanvasRenderer))]
public sealed class RecorteAfiche : MaskableGraphic
{
    [SerializeField] private Texture2D afiche;
    [SerializeField] private Vector2[] contorno;
    [SerializeField] private Rect region;
    private int[] triangulos;
    public override Texture mainTexture => afiche != null ? afiche : Texture2D.whiteTexture;

    public void Configurar(Texture2D imagen, Vector2[] puntos)
    {
        afiche = imagen;
        contorno = puntos;
        Vector2 min = puntos[0], max = puntos[0];
        foreach (Vector2 p in puntos) { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
        region = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(.5f, .5f);
        rectTransform.sizeDelta = region.size;
        rectTransform.anchoredPosition = new Vector2(region.center.x - 836f, 470.5f - region.center.y);
        raycastTarget = false;
        triangulos = null;
        SetAllDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (afiche == null || contorno == null || contorno.Length < 3) return;
        Rect r = rectTransform.rect;
        foreach (Vector2 p in contorno)
        {
            float x = (p.x - region.x) / region.width;
            float y = 1f - (p.y - region.y) / region.height;
            vh.AddVert(new Vector3(r.x + x * r.width, r.y + y * r.height), color,
                new Vector2(p.x / 1672f, 1f - p.y / 941f));
        }
        if (triangulos == null) triangulos = Triangular(contorno);
        for (int i = 0; i < triangulos.Length; i += 3)
            vh.AddTriangle(triangulos[i], triangulos[i + 1], triangulos[i + 2]);
    }

    // Las líneas de papel son cóncavas; un abanico de triángulos atravesaría otros retratos.
    private static int[] Triangular(Vector2[] puntos)
    {
        var indices = new List<int>();
        var resultado = new List<int>();
        float area = 0;
        for (int i = 0; i < puntos.Length; i++)
            area += Cruz(puntos[i], puntos[(i + 1) % puntos.Length]);
        for (int i = 0; i < puntos.Length; i++) indices.Add(area > 0 ? i : puntos.Length - 1 - i);
        int limite = puntos.Length * puntos.Length;
        while (indices.Count > 2 && limite-- > 0)
        {
            bool encontrado = false;
            for (int i = 0; i < indices.Count; i++)
            {
                int a = indices[(i + indices.Count - 1) % indices.Count];
                int b = indices[i], c = indices[(i + 1) % indices.Count];
                if (Cruz(puntos[b] - puntos[a], puntos[c] - puntos[b]) <= .001f) continue;
                bool contiene = false;
                foreach (int j in indices)
                {
                    if (j == a || j == b || j == c) continue;
                    Vector2 p = puntos[j];
                    if (Cruz(puntos[b] - puntos[a], p - puntos[a]) >= 0 &&
                        Cruz(puntos[c] - puntos[b], p - puntos[b]) >= 0 &&
                        Cruz(puntos[a] - puntos[c], p - puntos[c]) >= 0) { contiene = true; break; }
                }
                if (contiene) continue;
                resultado.Add(a); resultado.Add(b); resultado.Add(c);
                indices.RemoveAt(i); encontrado = true; break;
            }
            if (!encontrado) break;
        }
        return resultado.ToArray();
    }
    private static float Cruz(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
}
