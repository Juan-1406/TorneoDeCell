using UnityEngine;
using UnityEngine.UI;

// Marco de acero del torneo, con los dos extremos recortados como los botones del afiche.
[RequireComponent(typeof(CanvasRenderer))]
internal sealed class PlacaCombate : MaskableGraphic
{
    private Color contorno;
    private float grosor;
    private float corte;

    internal static PlacaCombate Crear(Transform padre, string nombre, Color interior, Color borde,
        float grosor = 2f, float corte = 14f)
    {
        RectTransform caja = EstiloTorneo.Caja(padre, nombre);
        var placa = caja.gameObject.AddComponent<PlacaCombate>();
        placa.raycastTarget = false;
        placa.color = interior;
        placa.contorno = borde;
        placa.grosor = grosor;
        placa.corte = corte;
        return placa;
    }

    protected override void OnPopulateMesh(VertexHelper malla)
    {
        malla.Clear();
        Rect rect = rectTransform.rect;
        if (rect.width <= 0 || rect.height <= 0) return;
        if (grosor > 0)
        {
            Poligono(malla, rect, corte, contorno);
            rect = Rect.MinMaxRect(rect.xMin + grosor, rect.yMin + grosor,
                rect.xMax - grosor, rect.yMax - grosor);
        }
        if (rect.width > 0 && rect.height > 0)
            Poligono(malla, rect, Mathf.Max(0, corte - .6f * grosor), color);
    }

    private static void Poligono(VertexHelper malla, Rect r, float esquina, Color32 tinte)
    {
        float c = Mathf.Min(esquina, .5f * Mathf.Min(r.width, r.height));
        int inicio = malla.currentVertCount;
        malla.AddVert(new Vector2(r.xMin + c, r.yMax), tinte, Vector2.zero);
        malla.AddVert(new Vector2(r.xMax, r.yMax), tinte, Vector2.zero);
        malla.AddVert(new Vector2(r.xMax, r.yMin + c), tinte, Vector2.zero);
        malla.AddVert(new Vector2(r.xMax - c, r.yMin), tinte, Vector2.zero);
        malla.AddVert(new Vector2(r.xMin, r.yMin), tinte, Vector2.zero);
        malla.AddVert(new Vector2(r.xMin, r.yMax - c), tinte, Vector2.zero);
        for (int i = 1; i < 5; i++) malla.AddTriangle(inicio, inicio + i, inicio + i + 1);
    }
}
