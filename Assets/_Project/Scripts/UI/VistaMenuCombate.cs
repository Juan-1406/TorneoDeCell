using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

internal sealed class VistaMenuCombate : MonoBehaviour
{
    private GameObject principal;
    private bool derrota;
    private CanvasGroup cabecera;
    private HudCombate arte;
    private Image ilustracion;
    private EventSystem eventos;
    private InputSystemUIInputModule entrada;
    private GameObject focoAnterior;
    private bool deseleccionAnterior;

    internal void Componer(Button[] botones, GameObject primerBoton, bool esDerrota)
    {
        derrota = esDerrota;
        principal = primerBoton;
        var velo = GetComponent<Image>();
        velo.sprite = null;
        velo.type = Image.Type.Simple;
        velo.color = new Color32(10, 15, 27, derrota ? (byte)194 : (byte)156);
        velo.raycastTarget = true;

        TMP_Text titulo = null;
        foreach (Transform hijo in transform)
            if (hijo.TryGetComponent(out titulo)) break;

        arte = GetComponentInParent<HudCombate>(true);
        var area = AreaSeguraCombate.Crear(transform, derrota ? "Área segura de derrota" : "Área segura de pausa");
        var marco = EstiloTorneo.Caja(area.Contenido, derrota ? "Marco de derrota" : "Marco de pausa");
        float alto = 46 + 154 + botones.Length * 12 + 100 + (botones.Length - 1) * 76;
        EstiloTorneo.Anclar(marco, new Vector2(.5f, .5f), Vector2.zero, new Vector2(792, alto));
        var sombra = PlacaCombate.Crear(marco, "Sombra", new Color32(3, 6, 12, 205), Color.clear, 0, 25);
        sombra.rectTransform.anchoredPosition = new Vector2(0, -10);
        Color acento = derrota ? EstiloTorneo.Coral : EstiloTorneo.Oro;
        var placa = PlacaCombate.Crear(marco, "Cartel", EstiloTorneo.Tinta, acento, 4, 26);
        var columna = EstiloTorneo.Caja(placa.transform, "Opciones");
        var lista = columna.gameObject.AddComponent<VerticalLayoutGroup>();
        lista.padding = new RectOffset(24, 24, 22, 24);
        lista.spacing = 12;
        lista.childAlignment = TextAnchor.MiddleCenter;
        lista.childControlWidth = lista.childControlHeight = true;
        lista.childForceExpandHeight = false;

        ComponerCabecera(columna, titulo, acento);
        for (int i = 0; i < botones.Length; i++)
        {
            Button boton = botones[i];
            boton.transform.SetParent(columna, false);
            ComponerOpcion(boton, boton.gameObject == principal);
            boton.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = i > 0 ? botones[i - 1] : null,
                selectOnDown = i + 1 < botones.Length ? botones[i + 1] : null
            };
        }
    }

    private void ComponerCabecera(RectTransform columna, TMP_Text titulo, Color acento)
    {
        var recorte = EstiloTorneo.Caja(columna, "Encabezado");
        recorte.gameObject.AddComponent<LayoutElement>().preferredHeight = 154;
        recorte.gameObject.AddComponent<RectMask2D>();
        var banda = EstiloTorneo.Caja(recorte, "Afiche");
        cabecera = banda.gameObject.AddComponent<CanvasGroup>();
        var pared = banda.gameObject.AddComponent<RawImage>();
        pared.texture = arte.Muro;
        pared.uvRect = new Rect(.19f, .70f, .81f, .30f);
        pared.color = derrota ? new Color32(200, 175, 192, 255) : new Color32(211, 221, 236, 255);
        pared.raycastTarget = false;
        EstiloTorneo.Imagen(banda, "Tinta del afiche", new Color32(8, 16, 34, 72), 0, 0, 1, 1);
        var franja = EstiloTorneo.Imagen(banda, "Franja del retrato",
            derrota ? new Color32(130, 51, 46, 255) : new Color32(30, 80, 88, 255), .73f, -.5f, .45f, 2);
        franja.rectTransform.localRotation = Quaternion.Euler(0, 0, -16);
        EstiloTorneo.Imagen(franja.transform, "Filete", acento, .015f, 0, .018f, 1);
        var foto = EstiloTorneo.Caja(banda, "Ilustración");
        EstiloTorneo.Ubicar(foto, .73f, 0, .27f, 1);
        ilustracion = EstiloTorneo.Retrato(foto, derrota ? arte.RetratoJefe : arte.RetratoJugador, derrota ? 1.05f : 1.02f, !derrota);

        var marca = EstiloTorneo.Imagen(banda, "Marca izquierda", acento, .037f, .34f, .014f, .32f);
        var pareja = EstiloTorneo.Imagen(banda, "Marca derecha", acento, .063f, .34f, .014f, .32f);
        if (derrota)
        {
            marca.rectTransform.localRotation = Quaternion.Euler(0, 0, -24);
            pareja.rectTransform.localRotation = marca.rectTransform.localRotation;
        }
        titulo.transform.SetParent(banda, false);
        EstiloTorneo.Ubicar(titulo.rectTransform, .115f, .08f, derrota ? .58f : .61f, .86f);
        titulo.color = EstiloTorneo.Papel;
        titulo.fontStyle = FontStyles.Bold | FontStyles.Italic;
        titulo.alignment = TextAlignmentOptions.MidlineLeft;
        titulo.enableAutoSizing = true;
        titulo.fontSizeMin = 50;
        titulo.fontSizeMax = derrota ? 79 : 86;
        titulo.textWrappingMode = TextWrappingModes.NoWrap;
        titulo.raycastTarget = false;
        EstiloTorneo.Imagen(banda, "Pie del afiche", acento, 0, 0, 1, .035f);
    }

    private static void ComponerOpcion(Button boton, bool primera)
    {
        boton.gameObject.AddComponent<LayoutElement>().preferredHeight = primera ? 100 : 76;
        // El Image original sigue siendo la superficie que recibe el clic.
        boton.GetComponent<Image>().color = Color.clear;
        var borde = PlacaCombate.Crear(boton.transform, "Marco del botón", Color.white, Color.clear, 0, 14);
        boton.targetGraphic = borde;
        boton.transition = Selectable.Transition.ColorTint;
        ColorBlock colores = boton.colors;
        colores.normalColor = primera ? EstiloTorneo.Oro : EstiloTorneo.Acero;
        colores.highlightedColor = EstiloTorneo.Foco;
        colores.selectedColor = EstiloTorneo.Papel;
        colores.pressedColor = EstiloTorneo.Coral;
        colores.fadeDuration = 0;
        boton.colors = colores;
        var interior = PlacaCombate.Crear(boton.transform, "Interior del botón", EstiloTorneo.Azul,
            Color.clear, 0, 12);
        interior.rectTransform.offsetMin = Vector2.one * 3;
        interior.rectTransform.offsetMax = -interior.rectTransform.offsetMin;
        var indicador = EstiloTorneo.Imagen(boton.transform, "Indicador de selección", EstiloTorneo.Tinta, 0, .5f, 0, 0);
        indicador.rectTransform.sizeDelta = new Vector2(18, 18);
        indicador.rectTransform.anchoredPosition = new Vector2(33, 0);
        indicador.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);

        TMP_Text etiqueta = boton.GetComponentInChildren<TMP_Text>(true);
        if (etiqueta.text == "Volver al menú") etiqueta.text = "Volver al menú principal";
        etiqueta.transform.SetAsLastSibling();
        EstiloTorneo.Ubicar(etiqueta.rectTransform, 0, 0, 1, 1);
        etiqueta.rectTransform.offsetMin = new Vector2(63, 4);
        etiqueta.rectTransform.offsetMax = new Vector2(-21, -4);
        etiqueta.fontStyle = FontStyles.Bold;
        etiqueta.alignment = TextAlignmentOptions.MidlineLeft;
        etiqueta.enableAutoSizing = true;
        etiqueta.fontSizeMin = 26;
        etiqueta.fontSizeMax = primera ? 46 : 36;
        etiqueta.textWrappingMode = TextWrappingModes.NoWrap;
        etiqueta.raycastTarget = false;
        boton.gameObject.AddComponent<FocoOpcionCombate>().Configurar(indicador, interior, etiqueta,
            primera ? EstiloTorneo.Oro : EstiloTorneo.Papel);
    }

    private void OnEnable()
    {
        if (principal == null) return;
        transform.SetAsLastSibling();
        eventos = EventSystem.current;
        if (eventos != null)
        {
            focoAnterior = eventos.currentSelectedGameObject;
            entrada = eventos.GetComponent<InputSystemUIInputModule>();
            if (entrada != null)
            {
                deseleccionAnterior = entrada.deselectOnBackgroundClick;
                entrada.deselectOnBackgroundClick = false;
            }
            eventos.SetSelectedGameObject(principal);
        }
        if (derrota)
        {
            EstiloTorneo.MostrarPrimerPlano(ilustracion, arte.RetratoJefe);
            StartCoroutine(EntradaDerrota());
        }
    }

    private IEnumerator EntradaDerrota()
    {
        var banda = (RectTransform)cabecera.transform;
        const float duracion = .24f;
        for (float t = 0; t < duracion; t += Time.unscaledDeltaTime)
        {
            float avance = Mathf.SmoothStep(0, 1, t / duracion);
            cabecera.alpha = Mathf.Lerp(.4f, 1, avance);
            banda.anchoredPosition = new Vector2(-30 * (1 - avance), 0);
            yield return null;
        }
        cabecera.alpha = 1;
        banda.anchoredPosition = Vector2.zero;
    }

    private void OnDisable()
    {
        if (entrada != null) entrada.deselectOnBackgroundClick = deseleccionAnterior;
        if (eventos != null && eventos == EventSystem.current)
        {
            var actual = eventos.currentSelectedGameObject;
            if (actual == null || actual.transform.IsChildOf(transform))
            {
                bool disponible = focoAnterior != null && focoAnterior.activeInHierarchy &&
                    (!focoAnterior.TryGetComponent(out Selectable control) || (control.isActiveAndEnabled && control.IsInteractable()));
                eventos.SetSelectedGameObject(disponible ? focoAnterior : null);
            }
        }
        eventos = null;
        focoAnterior = null;
        entrada = null;
    }
}
