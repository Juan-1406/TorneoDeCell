using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class LevelSelectUI : MonoBehaviour
{
    [Serializable]
    public class NivelEntrada
    {
        public string nombre;
        public string escena;
        public Button boton;
        public TMP_Text etiqueta;
        public Sprite retrato;
        public Sprite escenario;
        [NonSerialized] public GameObject selloCandado;
        [NonSerialized] public Image imagenEscenario, imagenRetrato;
        [NonSerialized] public TMP_Text estado, requisito;
    }

    [SerializeField] private NivelEntrada[] niveles;
    [SerializeField] private SceneLoader cargador;
    [SerializeField] private Button botonVolver;
    [SerializeField] private Texture fondoAfiche;
    [SerializeField] private Sprite candado;
    [SerializeField] private TMP_FontAsset fuenteTitulo;
    [SerializeField] private TMP_FontAsset fuenteTexto;
    private Button botonProgreso;
    private static readonly Color AceroSuave = new Color32(106, 123, 146, 255);
    private static readonly Color Marfil = new Color32(247, 241, 226, 255);
    private static readonly Color Estado = new Color32(211, 222, 235, 255);
    private static readonly Color AceroPulido = new Color32(186, 210, 232, 255);
    private static readonly Color OroClaro = new Color32(255, 234, 153, 255);
    private static readonly Color OroOscuro = new Color32(189, 81, 16, 255);
    private static readonly Vector2 Formato = new Vector2(1920, 1080);

    private void Start()
    {
        Componer();
        foreach (NivelEntrada nivel in niveles)
        {
            string escena = nivel.escena;
            nivel.boton.onClick.AddListener(() => ElegirNivel(escena));
        }

        if (EventSystem.current != null &&
            EventSystem.current.TryGetComponent(out InputSystemUIInputModule entrada))
            entrada.deselectOnBackgroundClick = false;
        Actualizar();
    }

    private void Componer()
    {
        Transform raiz = botonVolver.transform.parent;
        botonProgreso = raiz.Find("BotonReiniciarProgreso").GetComponent<Button>();
        raiz.Find("Panel").gameObject.SetActive(false);
        var titulo = raiz.Find("Titulo").GetComponent<TMP_Text>();
        var fondo = EstiloTorneo.Imagen(raiz, "Fondo del selector", EstiloTorneo.Tinta, 0, 0, 1, 1);
        fondo.transform.SetAsFirstSibling();
        if (fondoAfiche != null)
        {
            var textura = EstiloTorneo.Caja(fondo.transform, "Fondo nocturno de Rivales").gameObject.AddComponent<RawImage>();
            textura.texture = fondoAfiche;
            textura.color = Color.white;
            textura.raycastTarget = false;
            fondo.gameObject.AddComponent<RectMask2D>();
            var encuadre = textura.gameObject.AddComponent<AspectRatioFitter>();
            encuadre.aspectRatio = (float)fondoAfiche.width / fondoAfiche.height;
            encuadre.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        }

        EstiloTorneo.Imagen(fondo.transform, "Velo nocturno", new Color32(7, 11, 19, 43), 0, 0, 1, 1);

        var area = AreaSeguraCombate.Crear(raiz, "Área segura del selector");
        var afiche = EstiloTorneo.Caja(area.Contenido, "Afiche de niveles");
        EstiloTorneo.Anclar(afiche, new Vector2(.5f, .5f), Vector2.zero, Formato);
        void Ajustar(Vector2 disponible) =>
            afiche.localScale = Vector3.one * Mathf.Min(disponible.x / Formato.x, disponible.y / Formato.y);
        area.CambioDeTamano += Ajustar;
        Ajustar(area.Contenido.rect.size);

        titulo.transform.SetParent(afiche, false);
        if (fuenteTitulo != null) titulo.font = fuenteTitulo;
        EstiloTorneo.Ubicar(titulo.rectTransform, .10f, .805f, .80f, .147f);
        Formatear(titulo, 126, fuenteTitulo != null ? FontStyles.Italic : FontStyles.Bold | FontStyles.Italic);
        titulo.color = Color.white;
        titulo.enableVertexGradient = true;
        var dorado = new Color32(248, 194, 80, 255);
        titulo.colorGradient = new VertexGradient(Marfil, Marfil, dorado, dorado);
        titulo.outlineColor = EstiloTorneo.Tinta;
        titulo.outlineWidth = .14f;
        var linea = PlacaCombate.Crear(afiche, "Remate del título", EstiloTorneo.Oro, Color.clear, 0, 3, true);
        EstiloTorneo.Ubicar(linea.rectTransform, .14f, .788f, .72f, .006f);

        for (int i = 0; i < niveles.Length; i++)
        {
            NivelEntrada nivel = niveles[i];
            float x = .065f + i * .223f;
            ComponerNivel(nivel, afiche, x);
        }

        ComponerAccion(botonVolver, afiche, .065f);
        ComponerAccion(botonProgreso, afiche, .735f);
    }

    private void ComponerNivel(NivelEntrada nivel, RectTransform afiche, float x)
    {
        Transform boton = nivel.boton.transform;
        var retratoAnterior = boton.Find("Retrato Padre Pasta");
        if (retratoAnterior != null) retratoAnterior.gameObject.SetActive(false);
        boton.SetParent(afiche, false);
        EstiloTorneo.Ubicar((RectTransform)boton, x, .205f, .202f, .541f);
        PrepararBorde(nivel.boton, 28, true);
        var baseTarjeta = Enmarcar(boton, "Base de la tarjeta", EstiloTorneo.Azul, AceroPulido, 4, 27, 2);
        baseTarjeta.Degradar(new Color32(78, 98, 124, 255), EstiloTorneo.Tinta);

        var realce = EstiloTorneo.Caja(boton, "Doble borde dorado");
        Enmarcar(realce, "Reflejo dorado", new Color32(255, 149, 30, 56), Color.clear, 0, 37, -12);
        var exterior = Enmarcar(realce, "Borde exterior", EstiloTorneo.Oro, OroClaro, 3, 34, -8);
        exterior.Degradar(EstiloTorneo.Oro, OroOscuro);
        Enmarcar(realce, "Separación", EstiloTorneo.Tinta, Color.clear, 0, 29, -1);
        var interior = Enmarcar(realce, "Borde interior", EstiloTorneo.Azul, OroClaro, 3, 26, 2);
        interior.Degradar(EstiloTorneo.Oro, EstiloTorneo.Tinta);

        var arte = EstiloTorneo.Caja(boton, "Ilustración del nivel");
        EstiloTorneo.Ubicar(arte, .038f, .358f, .924f, .613f);
        arte.gameObject.AddComponent<RectMask2D>();
        nivel.imagenEscenario = EstiloTorneo.Imagen(arte, "Escenario", EstiloTorneo.Azul, 0, 0, 1, 1);
        nivel.imagenEscenario.sprite = nivel.escenario;
        if (nivel.escenario != null)
        {
            var ajuste = nivel.imagenEscenario.gameObject.AddComponent<AspectRatioFitter>();
            ajuste.aspectRatio = nivel.escenario.rect.width / nivel.escenario.rect.height;
            ajuste.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        }
        nivel.imagenRetrato = EstiloTorneo.Imagen(arte, "Retrato", Color.white, .065f, .02f, .87f, .96f);
        nivel.imagenRetrato.sprite = nivel.retrato;
        nivel.imagenRetrato.enabled = nivel.retrato != null;
        nivel.imagenRetrato.preserveAspect = true;

        var sello = PlacaCombate.Crear(arte, "Sello de bloqueo", EstiloTorneo.Tinta, EstiloTorneo.Acero, 2, 9);
        EstiloTorneo.Anclar(sello.rectTransform, Vector2.one, new Vector2(-12, -12), new Vector2(55, 55));
        var icono = EstiloTorneo.Imagen(sello.transform, "Candado", EstiloTorneo.Papel, .23f, .19f, .54f, .62f);
        icono.sprite = candado;
        icono.preserveAspect = true;
        nivel.selloCandado = sello.gameObject;

        var franja = PlacaCombate.Crear(boton, "Placa del nombre", EstiloTorneo.Azul, AceroSuave, 2, 16, true);
        franja.Degradar(new Color32(54, 71, 93, 255), EstiloTorneo.Tinta);
        EstiloTorneo.Ubicar(franja.rectTransform, .038f, .237f, .924f, .137f);

        TMP_FontAsset fuenteBase = fuenteTexto != null ? fuenteTexto : nivel.etiqueta.font;
        nivel.etiqueta.transform.SetParent(franja.transform, false);
        nivel.etiqueta.text = nivel.nombre;
        nivel.etiqueta.font = fuenteBase;
        EstiloTorneo.Ubicar(nivel.etiqueta.rectTransform, .05f, .045f, .90f, .91f);
        Formatear(nivel.etiqueta, 48, fuenteTexto != null ? FontStyles.Normal : FontStyles.Bold);
        nivel.etiqueta.color = Marfil;
        nivel.estado = EstiloTorneo.Texto(boton, "", fuenteBase, 34, .06f, .112f, .88f, .092f, Estado);
        nivel.estado.name = "Estado del nivel";
        if (fuenteTexto != null) nivel.estado.fontStyle = FontStyles.Normal;
        nivel.requisito = EstiloTorneo.Texto(boton, "", fuenteBase, 26, .06f, .032f, .88f, .078f, Estado);
        nivel.requisito.name = "Requisito del nivel";
        nivel.requisito.fontStyle = FontStyles.Normal;
        var jugar = PlacaCombate.Crear(boton, "Acción Jugar", EstiloTorneo.Oro, OroClaro, 3, 16, true);
        jugar.Degradar(OroOscuro, new Color32(255, 190, 49, 255));
        EstiloTorneo.Ubicar(jugar.rectTransform, .08f, .064f, .84f, .145f);
        AgregarIconoJugar(jugar.transform);
        EstiloTorneo.Texto(jugar.transform, "Jugar", fuenteBase, 48, .29f, .02f, .60f, .96f, EstiloTorneo.Tinta);
        nivel.boton.gameObject.AddComponent<FocoOpcionCombate>().Configurar(seleccionado =>
        {
            realce.gameObject.SetActive(seleccionado);
            jugar.gameObject.SetActive(seleccionado);
            nivel.estado.enabled = !seleccionado;
        });
    }

    private static void AgregarIconoJugar(Transform padre)
    {
        var recorte = EstiloTorneo.Caja(padre, "Icono de jugar");
        EstiloTorneo.Ubicar(recorte, .21f, .24f, .12f, .52f);
        recorte.gameObject.AddComponent<RectMask2D>();
        var rombo = EstiloTorneo.Imagen(recorte, "Punta", EstiloTorneo.Tinta, 0, .5f, 0, 0);
        rombo.rectTransform.sizeDelta = new Vector2(25f, 25f);
        rombo.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
    }

    private static PlacaCombate Enmarcar(Transform padre, string nombre, Color fondo, Color borde,
        float grosor, float corte, float margen)
    {
        var marco = PlacaCombate.Crear(padre, nombre, fondo, borde, grosor, corte, true);
        marco.rectTransform.offsetMin = Vector2.one * margen;
        marco.rectTransform.offsetMax = -marco.rectTransform.offsetMin;
        return marco;
    }

    private void ComponerAccion(Button boton, RectTransform afiche, float x)
    {
        boton.transform.SetParent(afiche, false);
        EstiloTorneo.Ubicar((RectTransform)boton.transform, x, .076f, .20f, .072f);
        PrepararBorde(boton, 15);
        var interior = PlacaCombate.Crear(boton.transform, "Placa de la acción", EstiloTorneo.Azul, Color.clear, 0, 12);
        interior.rectTransform.offsetMin = Vector2.one * 3;
        interior.rectTransform.offsetMax = -Vector2.one * 3;
        var etiqueta = boton.GetComponentInChildren<TMP_Text>();
        etiqueta.transform.SetParent(interior.transform, false);
        if (fuenteTexto != null) etiqueta.font = fuenteTexto;
        EstiloTorneo.Ubicar(etiqueta.rectTransform, .13f, .04f, .8f, .92f);
        Formatear(etiqueta, 34, fuenteTexto != null ? FontStyles.Normal : FontStyles.Bold);
        ConfigurarRombo(boton, interior, etiqueta, 26);
    }

    private static void PrepararBorde(Button boton, float corte, bool cuatroEsquinas = false)
    {
        boton.GetComponent<Image>().color = Color.clear;
        var borde = PlacaCombate.Crear(boton.transform, "Marco del botón", Color.white, Color.clear, 0, corte, cuatroEsquinas);
        boton.targetGraphic = borde;
        boton.transition = Selectable.Transition.ColorTint;
        boton.colors = new ColorBlock
        {
            normalColor = AceroSuave,
            highlightedColor = EstiloTorneo.Foco,
            selectedColor = EstiloTorneo.Foco,
            pressedColor = EstiloTorneo.Papel,
            disabledColor = AceroSuave,
            colorMultiplier = 1
        };
    }

    private static void ConfigurarRombo(Button boton, PlacaCombate placa, TMP_Text etiqueta, float distancia)
    {
        var rombo = EstiloTorneo.Imagen(placa.transform, "Indicador de selección", EstiloTorneo.Tinta, 0, 0, 0, 0);
        rombo.rectTransform.anchorMin = rombo.rectTransform.anchorMax = new Vector2(0, .5f);
        rombo.rectTransform.sizeDelta = new Vector2(15, 15);
        rombo.rectTransform.anchoredPosition = new Vector2(distancia, 0);
        rombo.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
        boton.gameObject.AddComponent<FocoOpcionCombate>().Configurar(seleccionado =>
        {
            rombo.enabled = seleccionado;
            placa.color = seleccionado ? EstiloTorneo.Foco : EstiloTorneo.Azul;
            etiqueta.color = seleccionado ? EstiloTorneo.Tinta : Marfil;
        });
    }

    private static void Formatear(TMP_Text texto, float tamano, FontStyles estilo)
    {
        texto.color = EstiloTorneo.Papel;
        texto.alignment = TextAlignmentOptions.Center;
        texto.fontStyle = estilo;
        texto.margin = Vector4.zero;
        texto.textWrappingMode = TextWrappingModes.NoWrap;
        texto.enableAutoSizing = true;
        texto.fontSizeMin = tamano * .65f;
        texto.fontSizeMax = tamano;
        texto.raycastTarget = false;
    }

    private void ElegirNivel(string escena)
    {
        if (!cargador.IntentarCargarEscena(escena)) return;

        foreach (NivelEntrada nivel in niveles)
            nivel.boton.interactable = false;
        botonVolver.interactable = botonProgreso.interactable = false;
    }

    public void ReiniciarProgreso()
    {
        ProgresoJuego.Borrar();
        Actualizar();
    }

    private void Actualizar()
    {
        Button primero = null, anterior = null;
        for (int i = 0; i < niveles.Length; i++)
        {
            NivelEntrada nivel = niveles[i];
            bool existe = Application.CanStreamedLevelBeLoaded(nivel.escena);
            bool desbloqueado = ProgresoJuego.NivelDesbloqueado(i);
            bool completado = ProgresoJuego.NivelCompletado(i);
            bool disponible = existe && ProgresoJuego.PuedeJugarNivel(i);
            bool bloqueado = existe && !desbloqueado && !completado;
            nivel.boton.interactable = disponible;
            nivel.boton.GetComponent<Image>().raycastTarget = disponible;
            nivel.selloCandado.SetActive(bloqueado && candado != null);
            nivel.imagenEscenario.color = nivel.escenario == null ? EstiloTorneo.Azul :
                disponible ? Color.white : new Color32(181, 189, 203, 255);
            nivel.imagenRetrato.color = disponible ? Color.white : new Color32(210, 217, 227, 255);
            nivel.estado.text = completado
                ? (ProgresoJuego.JefeDerrotado(i) ? "COMPLETADO" : "FASE 1 SUPERADA")
                : bloqueado ? "Bloqueado" : existe ? "Disponible" : "Por estrenar";
            nivel.estado.color = bloqueado ? OroClaro : !existe && !completado ? EstiloTorneo.Foco : Estado;
            nivel.requisito.text = completado ? "Reinicia el progreso\npara repetir" : bloqueado
                ? (i == 1 ? "Derrota a Renato" : "Vence a los tres\njefes anteriores") : "";

            if (!disponible)
            {
                nivel.boton.navigation = new Navigation { mode = Navigation.Mode.None };
                continue;
            }

            Button boton = nivel.boton;
            boton.navigation = Enlaces(izquierda: anterior, abajo: i < niveles.Length / 2 ? botonVolver : botonProgreso);
            if (anterior != null)
            {
                Navigation navegacion = anterior.navigation;
                navegacion.selectOnRight = boton;
                anterior.navigation = navegacion;
            }
            primero ??= boton;
            anterior = boton;
        }
        botonVolver.navigation = Enlaces(arriba: primero, derecha: botonProgreso);
        botonProgreso.navigation = Enlaces(arriba: anterior, izquierda: botonVolver);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject((primero ?? botonVolver).gameObject);
    }

    private static Navigation Enlaces(Button izquierda = null, Button derecha = null,
        Button arriba = null, Button abajo = null) => new Navigation
    {
        mode = Navigation.Mode.Explicit,
        selectOnLeft = izquierda,
        selectOnRight = derecha,
        selectOnUp = arriba,
        selectOnDown = abajo
    };
}
