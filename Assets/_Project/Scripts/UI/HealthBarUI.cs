using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Health objetivo;
    [SerializeField] private Slider slider;

    private TMP_Text cantidad;
    private TMP_Text estado;
    private RectTransform rastro;
    private Image destello;
    private Image relleno;
    private Image ilustracion;
    private SegundaFaseRenato faseRenato;
    private bool esJugador;
    private float proporcion;
    private float inicioRastro;
    private Coroutine impacto;

    internal Sprite RetratoActual => ilustracion != null ? ilustracion.sprite : null;

    internal void Componer(Transform padre, string nombre, Sprite retrato, TMP_FontAsset fuente, TMP_Text vidas)
    {
        esJugador = vidas != null;
        var rect = (RectTransform)transform;
        rect.SetParent(padre, false);
        // El director de oleadas sigue mostrando u ocultando el mismo objeto de barra.
        foreach (Transform hijo in rect) hijo.gameObject.SetActive(false);
        slider.transition = Selectable.Transition.None;
        slider.interactable = false;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };

        Color acento = esJugador ? EstiloTorneo.Energia : EstiloTorneo.Coral;
        var placa = PlacaCombate.Crear(rect, "Placa de vida", EstiloTorneo.Tinta, EstiloTorneo.Acero, 2.5f, 16);
        var foto = EstiloTorneo.Imagen(placa.transform, "Medallón", esJugador
            ? new Color32(31, 81, 85, 255) : new Color32(104, 53, 51, 255), 0, 0, 1, 1);
        EstiloTorneo.Anclar(foto.rectTransform, new Vector2(esJugador ? 0 : 1, .5f),
            new Vector2(esJugador ? 13 : -13, 0), new Vector2(esJugador ? 80 : 94, 94));
        ilustracion = EstiloTorneo.Retrato(foto.rectTransform, retrato, 1.38f);
        if (!esJugador) objetivo.TryGetComponent(out faseRenato);
        destello = EstiloTorneo.Imagen(foto.transform, "Impacto", Color.clear, 0, 0, 1, 1);
        EstiloTorneo.Imagen(foto.transform, "Pie del retrato", acento, 0, 0, 1, .045f);

        var cuerpo = EstiloTorneo.Caja(placa.transform, "Salud");
        cuerpo.offsetMin = new Vector2(esJugador ? 108 : 18, 3);
        cuerpo.offsetMax = new Vector2(esJugador ? -18 : -122, -3);
        var identidad = EstiloTorneo.Texto(cuerpo, nombre.ToUpperInvariant(), fuente, esJugador ? 36 : 39,
            0, .53f, .625f, .43f, EstiloTorneo.Papel);
        identidad.fontStyle |= FontStyles.Italic;
        identidad.alignment = TextAlignmentOptions.MidlineLeft;
        cantidad = EstiloTorneo.Texto(cuerpo, "", fuente, 29, .665f, .53f, .335f, .41f, EstiloTorneo.Papel);
        cantidad.name = "Vida numérica";
        cantidad.alignment = TextAlignmentOptions.MidlineRight;

        var pista = EstiloTorneo.Imagen(cuerpo, "Medidor", new Color32(55, 65, 83, 255), 0, .34f, 1, .17f);
        rastro = EstiloTorneo.Imagen(pista.transform, "Daño reciente", EstiloTorneo.Papel, 0, 0, 1, 1).rectTransform;
        relleno = EstiloTorneo.Imagen(pista.transform, "Vida actual", acento, 0, 0, 1, 1);
        slider.fillRect = relleno.rectTransform;
        int tramos = esJugador ? 3 : 5;
        for (int i = 1; i < tramos; i++)
        {
            var division = EstiloTorneo.Imagen(pista.transform, "División", EstiloTorneo.Tinta,
                (float)i / tramos, 0, 0, 1);
            division.rectTransform.sizeDelta = new Vector2(esJugador ? 4 : 2, 0);
        }

        estado = EstiloTorneo.Texto(cuerpo, esJugador ? "JUGADOR" : "JEFE", fuente, 26,
            0, .015f, .43f, .30f, acento);
        estado.name = "Estado";
        estado.alignment = TextAlignmentOptions.MidlineLeft;
        if (esJugador)
        {
            vidas.rectTransform.SetParent(cuerpo, false);
            EstiloTorneo.Ubicar(vidas.rectTransform, .46f, .015f, .54f, .30f);
            vidas.color = EstiloTorneo.Papel;
            vidas.fontStyle = FontStyles.Bold;
            vidas.alignment = TextAlignmentOptions.MidlineRight;
            vidas.enableAutoSizing = true;
            vidas.fontSizeMin = 22f;
            vidas.fontSizeMax = 26f;
            vidas.textWrappingMode = TextWrappingModes.NoWrap;
            vidas.raycastTarget = false;
        }
        Actualizar(objetivo.VidaActual, objetivo.VidaMaxima);
    }

    private void OnEnable()
    {
        objetivo.OnVidaCambiada += Actualizar;
        objetivo.OnDanioRecibido += MostrarImpacto;
        Actualizar(objetivo.VidaActual, objetivo.VidaMaxima);
    }

    private void OnDisable()
    {
        if (objetivo != null)
        {
            objetivo.OnVidaCambiada -= Actualizar;
            objetivo.OnDanioRecibido -= MostrarImpacto;
        }
        DetenerImpacto();
    }

    private void Actualizar(int actual, int maxima)
    {
        float anterior = proporcion;
        proporcion = maxima > 0 ? Mathf.Clamp01((float)actual / maxima) : 0f;
        slider.normalizedValue = proporcion;
        if (rastro == null) return;

        inicioRastro = Mathf.Max(anterior, rastro.anchorMax.x);
        DetenerImpacto();
        rastro.anchorMax = new Vector2(proporcion, 1);
        cantidad.SetText("{0}/{1}", actual, maxima);
        if (esJugador)
        {
            bool critica = actual > 0 && actual <= Mathf.Max(1, maxima / 3);
            estado.text = critica ? "¡CRÍTICA!" : "JUGADOR";
            estado.color = critica ? EstiloTorneo.Oro : EstiloTorneo.Energia;
            relleno.color = critica ? EstiloTorneo.Oro : EstiloTorneo.Energia;
        }
        else if (faseRenato != null)
            EstiloTorneo.MostrarPrimerPlano(ilustracion, faseRenato.RetratoEnSegundaFase);
    }

    private void MostrarImpacto()
    {
        if (rastro != null) impacto = StartCoroutine(AnimarImpacto());
    }

    private IEnumerator AnimarImpacto()
    {
        const float duracion = .48f;
        for (float t = 0f; t < duracion; t += Time.deltaTime)
        {
            float avance = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.10f, duracion, t));
            rastro.anchorMax = new Vector2(Mathf.Lerp(inicioRastro, proporcion, avance), 1f);
            var luz = EstiloTorneo.Papel;
            luz.a = .55f * (1 - Mathf.Clamp01(t / .18f));
            destello.color = luz;
            yield return null;
        }
        rastro.anchorMax = new Vector2(proporcion, 1);
        destello.color = Color.clear;
        impacto = null;
    }

    private void DetenerImpacto()
    {
        if (impacto != null) StopCoroutine(impacto);
        impacto = null;
        if (destello != null) destello.color = Color.clear;
    }
}
