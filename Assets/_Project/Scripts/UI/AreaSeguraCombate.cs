using System;
using UnityEngine;
using UnityEngine.EventSystems;

// Un mismo espacio de diseño para las esquinas del HUD y los paneles centrados.
internal sealed class AreaSeguraCombate : UIBehaviour
{
    private RectTransform raiz;
    private Canvas lienzo;
    internal RectTransform Contenido { get; private set; }
    internal event Action<Vector2> CambioDeTamano;

    internal static AreaSeguraCombate Crear(Transform padre, string nombre)
    {
        var objeto = new GameObject(nombre, typeof(RectTransform));
        var area = objeto.AddComponent<AreaSeguraCombate>();
        area.raiz = (RectTransform)objeto.transform;
        area.raiz.SetParent(padre, false);
        EstiloTorneo.Ubicar(area.raiz, 0, 0, 1, 1);
        area.lienzo = padre.GetComponentInParent<Canvas>(true);
        area.Contenido = EstiloTorneo.Caja(area.raiz, "Contenido");
        area.Ajustar();
        return area;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        Ajustar();
    }

    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        Ajustar();
    }

    private void Ajustar()
    {
        if (Contenido == null) return;
        var segura = new Rect(0, 0, 1, 1);
        if (lienzo.renderMode == RenderMode.ScreenSpaceOverlay && Screen.width > 0 && Screen.height > 0)
        {
            Rect pixeles = Screen.safeArea;
            if (pixeles.width > 0 && pixeles.height > 0)
                segura = new Rect(pixeles.x / Screen.width, pixeles.y / Screen.height,
                    pixeles.width / Screen.width, pixeles.height / Screen.height);
        }

        Vector2 disponible = raiz.rect.size * segura.size;
        // Incluso en vertical caben dos retratos, sus nombres y las cifras en una misma fila.
        float escala = Mathf.Min(disponible.y / 1080f, disponible.x / 1120f);
        if (escala <= 0) return;
        Vector2 tamano = disponible / escala;
        Contenido.anchorMin = Contenido.anchorMax = segura.center;
        Contenido.anchoredPosition = Vector2.zero;
        Contenido.localScale = new Vector3(escala, escala, 1);
        if (Contenido.sizeDelta == tamano) return;
        Contenido.sizeDelta = tamano;
        CambioDeTamano?.Invoke(tamano);
    }
}
