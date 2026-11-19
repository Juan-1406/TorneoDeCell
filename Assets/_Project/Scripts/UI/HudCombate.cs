using TMPro;
using UnityEngine;

public sealed class HudCombate : MonoBehaviour
{
    [SerializeField] private HealthBarUI barraJugador;
    [SerializeField] private HealthBarUI barraJefe;
    [SerializeField] private TMP_Text vidas;
    [SerializeField] private TMP_Text oleadas;
    [SerializeField] private TMP_Text recompensas;
    [SerializeField] private string nombreJefe;
    [SerializeField] private Sprite retratoJugador;
    [SerializeField] private Sprite retratoJefe;
    [SerializeField] private Texture muroAfiche;

    internal Sprite RetratoJugador => retratoJugador;
    internal Sprite RetratoJefe => barraJefe.RetratoActual != null ? barraJefe.RetratoActual : retratoJefe;
    internal Texture Muro => muroAfiche;

    private void Awake()
    {
        var area = AreaSeguraCombate.Crear(transform, "HUD del combate");
        area.transform.SetAsFirstSibling();
        TMP_FontAsset fuente = vidas.font;
        barraJugador.Componer(area.Contenido, "Mateo", retratoJugador, fuente, vidas);
        barraJefe.Componer(area.Contenido, nombreJefe, retratoJefe, fuente, null);
        PrepararAviso(oleadas, area.Contenido);
        PrepararAviso(recompensas, area.Contenido);
        area.CambioDeTamano += Distribuir;
        Distribuir(area.Contenido.rect.size);
    }

    private void PrepararAviso(TMP_Text aviso, RectTransform padre)
    {
        if (aviso == null) return;
        string nombre = aviso == oleadas ? "Panel de oleada" : "Panel de recompensa";
        var fondo = PlacaCombate.Crear(padre, nombre, EstiloTorneo.Tinta, EstiloTorneo.Acero, 2, 13);
        EstiloTorneo.Imagen(fondo.transform, "Acento", EstiloTorneo.Oro, .018f, .19f, .009f, .62f);
        aviso.rectTransform.SetParent(fondo.transform, false);
        EstiloTorneo.Ubicar(aviso.rectTransform, .055f, .04f, .91f, .92f);
        aviso.alignment = TextAlignmentOptions.MidlineLeft;
        aviso.color = EstiloTorneo.Papel;
        aviso.richText = true;
        aviso.text = "";
        fondo.gameObject.SetActive(false);
        aviso.raycastTarget = false;
        aviso.enableAutoSizing = true;
        aviso.fontSizeMin = 24f;
        aviso.fontSizeMax = 28f;
    }

    internal static void MostrarIndicador(TMP_Text texto, string mensaje)
    {
        if (texto == null) return;
        texto.text = mensaje;
        var fondo = texto.transform.parent;
        if (fondo != null && fondo.TryGetComponent<PlacaCombate>(out _))
            fondo.gameObject.SetActive(!string.IsNullOrEmpty(mensaje));
    }

    private void Distribuir(Vector2 tamano)
    {
        const float margen = 30f;
        float disponible = tamano.x - 2 * margen - 20f;
        if (disponible <= 0) return;
        float anchoJugador = Mathf.Min(476f, disponible * .445f);
        float anchoJefe = Mathf.Min(582f, disponible - anchoJugador);
        EstiloTorneo.Anclar((RectTransform)barraJugador.transform, new Vector2(0, 1),
            new Vector2(margen, -24), new Vector2(anchoJugador, 124));
        EstiloTorneo.Anclar((RectTransform)barraJefe.transform, Vector2.one,
            new Vector2(-margen, -24), new Vector2(anchoJefe, 124));

        // En pantallas estrechas, los avisos de oleada tienen una fila propia.
        float huecoCentral = tamano.x - 2 * (margen + Mathf.Max(anchoJugador, anchoJefe) + 12);
        bool estrecho = huecoCentral < 465;
        float y = estrecho ? -158f : -24f;
        float anchoAviso = Mathf.Min(590f, estrecho ? disponible : huecoCentral);
        var arriba = new Vector2(.5f, 1f);
        if (oleadas != null)
            EstiloTorneo.Anclar((RectTransform)oleadas.transform.parent, arriba, new Vector2(0, y), new Vector2(anchoAviso, 96));
        if (recompensas != null)
            EstiloTorneo.Anclar((RectTransform)recompensas.transform.parent, arriba, new Vector2(0, y - 102), new Vector2(anchoAviso, 74));
    }
}
