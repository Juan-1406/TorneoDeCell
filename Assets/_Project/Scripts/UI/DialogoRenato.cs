using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

internal sealed class DialogoRenato
{
    private readonly Canvas canvas;
    private readonly TMP_Text titulo;
    private readonly Image acento;
    private readonly RectTransform panel;
    private readonly Image retrato;
    private readonly TMP_Text texto, pie;

    internal DialogoRenato(Component propietario, TMP_FontAsset fuente)
    {
        foreach (Canvas candidato in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (candidato.gameObject.scene != propietario.gameObject.scene || !candidato.isRootCanvas ||
                candidato.renderMode == RenderMode.WorldSpace) continue;
            canvas = candidato;
            break;
        }

        var lienzo = new GameObject("Diálogo de Renato", typeof(RectTransform), typeof(Canvas));
        if (canvas != null)
        {
            lienzo.transform.SetParent(canvas.transform, false);
            Colocar((RectTransform)lienzo.transform, 0f, 0f, 1f, 1f);
            lienzo.GetComponent<Canvas>().overrideSorting = true;
            lienzo.GetComponent<Canvas>().sortingOrder = canvas.sortingOrder + 20;
        }
        else
        {
            lienzo.transform.SetParent(propietario.transform, false);
            canvas = lienzo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var escala = lienzo.AddComponent<CanvasScaler>();
            escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escala.referenceResolution = new Vector2(1280, 720);
        }

        panel = (RectTransform)lienzo.transform;
        var area = AreaSeguraCombate.Crear(panel, "Área segura del aviso");
        var marco = EstiloTorneo.Caja(area.Contenido, "Marco del aviso");
        EstiloTorneo.Anclar(marco, new Vector2(.5f, 0), new Vector2(0, 34), new Vector2(890, 172));
        var placa = PlacaCombate.Crear(marco, "Afiche", EstiloTorneo.Tinta, EstiloTorneo.Acero, 3, 20);
        var zona = placa.rectTransform;
        acento = EstiloTorneo.Imagen(zona, "Banda de fase", EstiloTorneo.Energia, .018f, .12f, .009f, .76f);
        var fondo = EstiloTorneo.Imagen(zona, "Fondo del retrato", EstiloTorneo.Azul, .83f, .05f, .15f, .90f);
        retrato = EstiloTorneo.Imagen(fondo.transform, "Retrato", Color.white, 0, 0, 1, 1);
        retrato.preserveAspect = true;
        titulo = EstiloTorneo.Texto(zona, "Título de fase", fuente, 46, .051f, .58f, .76f, .37f, EstiloTorneo.Oro);
        titulo.alignment = TextAlignmentOptions.MidlineLeft;
        titulo.fontStyle |= FontStyles.Italic;
        texto = EstiloTorneo.Texto(zona, "Mensaje", fuente, 29, .051f, .28f, .76f, .28f, EstiloTorneo.Papel);
        texto.alignment = TextAlignmentOptions.MidlineLeft;
        pie = EstiloTorneo.Texto(zona, "Indicador", fuente, 25, .051f, .055f, .76f, .21f, EstiloTorneo.Foco);
        pie.alignment = TextAlignmentOptions.MidlineLeft;
        areaClic = marco;
    }

    private readonly RectTransform areaClic;
    private int cuadroApertura;
    private static int cuadroClic = -1;
    internal static bool ConsumioClic => cuadroClic == Time.frameCount;

    internal void Abrir(Sprite cara, bool victoria, string mensaje, string indicacion)
    {
        titulo.text = victoria ? "¡VICTORIA DEFINITIVA!" : "FASE 2";
        titulo.fontSizeMax = victoria ? 54 : 44;
        acento.color = titulo.color = victoria ? EstiloTorneo.Oro : EstiloTorneo.Energia;
        retrato.sprite = cara;
        Mostrar(mensaje, indicacion);
        cuadroApertura = Time.frameCount;
        panel.gameObject.SetActive(true);
        panel.SetAsLastSibling();
    }

    internal void Mostrar(string mensaje, string indicacion)
    {
        texto.text = mensaje;
        pie.text = indicacion;
    }

    internal bool ClicDentro()
    {
        if (Time.frameCount <= cuadroApertura) return false;
        bool clic = Input.GetMouseButtonDown(0) && RectTransformUtility.RectangleContainsScreenPoint(
            areaClic, Input.mousePosition, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera);
        var modulo = EventSystem.current != null ? EventSystem.current.currentInputModule as InputSystemUIInputModule : null;
        if (!clic && modulo?.submit?.action?.WasPressedThisFrame() != true) return false;

        cuadroClic = Time.frameCount;
        return true;
    }


    internal void Cerrar()
    {
        if (panel != null) panel.gameObject.SetActive(false);
    }

    internal void Destruir()
    {
        if (panel != null) Object.Destroy(panel.gameObject);
    }

    private static void Colocar(RectTransform rect, float x, float y, float maxX, float maxY)
    {
        rect.anchorMin = new Vector2(x, y);
        rect.anchorMax = new Vector2(maxX, maxY);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
