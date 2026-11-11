using UnityEngine;
using UnityEngine.UI;

/// <summary>Luz suave en una sola malla de UI; sin partículas físicas ni texturas por fotograma.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class LuzAfiche : MaskableGraphic
{
    [SerializeField] private bool indicadorGrabacion;
    public bool IndicadorGrabacion { get => indicadorGrabacion; set => indicadorGrabacion = value; }
    private void Update()
    {
        if (Application.isPlaying && indicadorGrabacion)
            color = new Color(1, .16f, .06f, Mathf.Sin(Time.unscaledTime * 4.5f) > 0 ? .75f : .12f);
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = rectTransform.rect;
        vh.AddVert(r.center, color, Vector2.zero);
        Color borde = color; borde.a = 0;
        const int lados = 32;
        for (int i = 0; i <= lados; i++)
        {
            float a = i * Mathf.PI * 2 / lados;
            vh.AddVert(r.center + new Vector2(Mathf.Cos(a) * r.width * .5f, Mathf.Sin(a) * r.height * .5f), borde, Vector2.zero);
            if (i > 0) vh.AddTriangle(0, i, i + 1);
        }
    }
}
