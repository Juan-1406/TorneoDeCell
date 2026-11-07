using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal sealed class DialogoRenato
{
    private static readonly Color Madera = new Color32(75, 39, 29, 255);
    private static readonly Color Borde = new Color32(225, 151, 75, 255);
    private static readonly Color Papel = new Color32(255, 224, 170, 255);
    private static readonly Color Tinta = new Color32(61, 30, 28, 255);
    private readonly Canvas canvas;
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

        var lienzo = new GameObject("Diálogo de René", typeof(RectTransform), typeof(Canvas));
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
        var caja = Imagen(panel, "Marco", Madera, 0.08f, 0.035f, 0.92f, 0.27f);
        var filete = Imagen(caja.rectTransform, "Borde dorado", Borde, 0.008f, 0.035f, 0.992f, 0.965f);
        var interior = Imagen(filete.rectTransform, "Papel", Papel, 0.008f, 0.038f, 0.992f, 0.962f);
        var zona = interior.rectTransform;
        Imagen(zona, "Separador", Borde, 0.715f, 0.06f, 0.721f, 0.94f);
        var marcoRetrato = Imagen(zona, "Marco del retrato", Madera, 0.742f, 0.078f, 0.974f, 0.922f);
        var fondoRetrato = Imagen(marcoRetrato.rectTransform, "Fondo del retrato", Borde, 0.037f, 0.038f, 0.963f, 0.962f);
        retrato = Imagen(fondoRetrato.rectTransform, "Retrato", Color.white, 0.03f, 0.03f, 0.97f, 0.97f);
        retrato.preserveAspect = true;
        var etiqueta = Imagen(zona, "Placa del nombre", Madera, 0.027f, 0.73f, 0.365f, 0.924f);
        TMP_Text nombre = Etiqueta(etiqueta.rectTransform, "Nombre", fuente, 0.04f, 0f, 0.96f, 1f);
        nombre.color = Papel;
        nombre.fontStyle = FontStyles.Bold;
        nombre.alignment = TextAlignmentOptions.MidlineLeft;
        nombre.text = "René Puente";
        texto = Etiqueta(zona, "Mensaje", fuente, 0.03f, 0.22f, 0.689f, 0.707f);
        pie = Etiqueta(zona, "Indicador", fuente, 0.36f, 0.04f, 0.689f, 0.195f);
        pie.alignment = TextAlignmentOptions.BottomRight;
        pie.fontSizeMax = 19f;
        areaClic = caja.rectTransform;
    }

    private readonly RectTransform areaClic;
    private int cuadroApertura;

    internal void Abrir(Sprite cara, string mensaje, string indicacion)
    {
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

    internal bool ClicDentro() => Time.frameCount > cuadroApertura && Input.GetMouseButtonDown(0) &&
        RectTransformUtility.RectangleContainsScreenPoint(areaClic, Input.mousePosition,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera);

    internal void Cerrar()
    {
        if (panel != null) panel.gameObject.SetActive(false);
    }

    internal void Destruir()
    {
        if (panel != null) Object.Destroy(panel.gameObject);
    }

    private static Image Imagen(RectTransform padre, string nombre, Color color, float x, float y, float maxX, float maxY)
    {
        var imagen = new GameObject(nombre, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        imagen.rectTransform.SetParent(padre, false);
        Colocar(imagen.rectTransform, x, y, maxX, maxY);
        imagen.color = color;
        imagen.raycastTarget = false;
        return imagen;
    }

    private static TMP_Text Etiqueta(RectTransform padre, string nombre, TMP_FontAsset fuente,
        float x, float y, float maxX, float maxY)
    {
        var etiqueta = new GameObject(nombre, typeof(RectTransform), typeof(TextMeshProUGUI))
            .GetComponent<TextMeshProUGUI>();
        etiqueta.rectTransform.SetParent(padre, false);
        Colocar(etiqueta.rectTransform, x, y, maxX, maxY);
        if (fuente != null) etiqueta.font = fuente;
        etiqueta.color = Tinta;
        etiqueta.fontSizeMin = 12f;
        etiqueta.fontSizeMax = 44f;
        etiqueta.enableAutoSizing = true;
        etiqueta.alignment = TextAlignmentOptions.TopLeft;
        etiqueta.raycastTarget = false;
        return etiqueta;
    }

    private static void Colocar(RectTransform rect, float x, float y, float maxX, float maxY)
    {
        rect.anchorMin = new Vector2(x, y);
        rect.anchorMax = new Vector2(maxX, maxY);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
