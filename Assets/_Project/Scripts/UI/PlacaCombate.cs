using UnityEngine;
using UnityEngine.UI;

// Marco de acero del torneo, con los dos extremos recortados como los botones del afiche.
[RequireComponent(typeof(CanvasRenderer))]
internal sealed class PlacaCombate : MaskableGraphic
{
    private Color contorno;
    private Color? sombraContorno;
    private Color? sombraInterior;
    private float grosor;
    private float corte;
    private bool cuatroEsquinas;

    internal static PlacaCombate Crear(Transform padre, string nombre, Color interior, Color borde,
        float grosor = 2f, float corte = 14f, bool cuatroEsquinas = false)
    {
        RectTransform caja = EstiloTorneo.Caja(padre, nombre);
        var placa = caja.gameObject.AddComponent<PlacaCombate>();
        placa.raycastTarget = false;
        placa.color = interior;
        placa.contorno = borde;
        placa.grosor = grosor;
        placa.corte = corte;
        placa.cuatroEsquinas = cuatroEsquinas;
        return placa;
    }

    internal void Degradar(Color? bordeInferior, Color? rellenoInferior = null)
    {
        sombraContorno = bordeInferior;
        sombraInterior = rellenoInferior;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper malla)
    {
        malla.Clear();
        Rect rect = rectTransform.rect;
        if (rect.width <= 0 || rect.height <= 0) return;
        if (grosor > 0)
        {
            Poligono(malla, rect, corte, contorno, sombraContorno);
            rect = Rect.MinMaxRect(rect.xMin + grosor, rect.yMin + grosor,
                rect.xMax - grosor, rect.yMax - grosor);
        }
        if (rect.width > 0 && rect.height > 0)
            Poligono(malla, rect, Mathf.Max(0, corte - .6f * grosor), color, sombraInterior);
    }

    private void Poligono(VertexHelper malla, Rect r, float esquina, Color tinte, Color? sombra)
    {
        float c = Mathf.Min(esquina, .5f * Mathf.Min(r.width, r.height));
        int inicio = malla.currentVertCount;
        Vertice(r.xMin + c, r.yMax);
        Vertice(r.xMax - (cuatroEsquinas ? c : 0), r.yMax);
        if (cuatroEsquinas) Vertice(r.xMax, r.yMax - c);
        Vertice(r.xMax, r.yMin + c);
        Vertice(r.xMax - c, r.yMin);
        Vertice(r.xMin + (cuatroEsquinas ? c : 0), r.yMin);
        if (cuatroEsquinas) Vertice(r.xMin, r.yMin + c);
        Vertice(r.xMin, r.yMax - c);
        int triangulos = cuatroEsquinas ? 6 : 4;
        for (int i = 1; i <= triangulos; i++) malla.AddTriangle(inicio, inicio + i, inicio + i + 1);

        void Vertice(float x, float y)
        {
            Color tono = sombra.HasValue ? Color.Lerp(sombra.Value, tinte, (y - r.yMin) / r.height) : tinte;
            malla.AddVert(new Vector2(x, y), tono, Vector2.zero);
        }
    }
}
