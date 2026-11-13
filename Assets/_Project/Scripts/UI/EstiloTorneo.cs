using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal static class EstiloTorneo
{
    internal static readonly Color Tinta = new Color32(10, 15, 27, 255);
    internal static readonly Color Azul = new Color32(24, 36, 59, 255);
    internal static readonly Color Oro = new Color32(250, 180, 67, 255);
    internal static readonly Color Papel = new Color32(255, 236, 194, 255);

    internal static void Ubicar(RectTransform rect, float x, float y, float ancho, float alto)
    {
        rect.anchorMin = new Vector2(x, y);
        rect.anchorMax = new Vector2(x + ancho, y + alto);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    internal static Image Imagen(Transform padre, string nombre, Color color,
        float x, float y, float ancho, float alto)
    {
        var imagen = new GameObject(nombre, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        imagen.transform.SetParent(padre, false);
        Ubicar(imagen.rectTransform, x, y, ancho, alto);
        imagen.color = color;
        imagen.raycastTarget = false;
        return imagen;
    }

    internal static TMP_Text Texto(Transform padre, string texto, TMP_FontAsset fuente, float tamano,
        float x, float y, float ancho, float alto, Color color)
    {
        var etiqueta = new GameObject(texto, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        etiqueta.transform.SetParent(padre, false);
        Ubicar(etiqueta.rectTransform, x, y, ancho, alto);
        etiqueta.font = fuente;
        etiqueta.text = texto;
        etiqueta.color = color;
        etiqueta.alignment = TextAlignmentOptions.Center;
        etiqueta.fontStyle = FontStyles.Bold;
        etiqueta.textWrappingMode = TextWrappingModes.NoWrap;
        etiqueta.fontSize = tamano;
        etiqueta.enableAutoSizing = true;
        etiqueta.fontSizeMin = tamano * .54f;
        etiqueta.fontSizeMax = tamano;
        etiqueta.raycastTarget = false;
        return etiqueta;
    }

    internal static Image Marco(Transform padre, string nombre, float x, float y, float ancho, float alto)
    {
        Image borde = Imagen(padre, nombre, Oro, x, y, ancho, alto);
        Imagen(borde.transform, "Tinta", Tinta, .009f, .013f, .982f, .974f);
        return Imagen(borde.transform, "Fondo", Azul, .019f, .028f, .962f, .944f);
    }

    internal static void Boton(Button boton, TMP_Text texto, bool principal)
    {
        var imagen = (Image)boton.targetGraphic;
        imagen.sprite = null;
        imagen.color = Color.white;
        boton.transition = Selectable.Transition.ColorTint;
        var colores = boton.colors;
        colores.normalColor = principal ? Oro : Azul;
        colores.highlightedColor = new Color32(255, 214, 137, 255);
        colores.selectedColor = new Color32(173, 222, 255, 255);
        colores.pressedColor = new Color32(220, 131, 48, 255);
        colores.disabledColor = new Color32(70, 77, 91, 255);
        colores.fadeDuration = .09f;
        boton.colors = colores;
        Image centro = Imagen(boton.transform, "Interior del botón", Tinta, .012f, .085f, .976f, .83f);
        centro.transform.SetAsFirstSibling();
        Imagen(boton.transform, "Acento", principal ? Oro : Papel, .035f, .26f, .018f, .48f);
        texto.transform.SetAsLastSibling();
        Ubicar(texto.rectTransform, .08f, .04f, .84f, .92f);
        texto.color = principal ? Oro : Papel;
        texto.fontStyle = FontStyles.Bold;
        texto.enableAutoSizing = true;
        texto.fontSizeMin = 16;
        texto.fontSizeMax = 35;
        texto.textWrappingMode = TextWrappingModes.NoWrap;
        texto.raycastTarget = false;
    }
}
