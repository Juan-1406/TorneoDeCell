using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
        [NonSerialized] public Image icono;
        [NonSerialized] public RectTransform ilustracion;
    }

    [SerializeField] private NivelEntrada[] niveles;
    [SerializeField] private SceneLoader cargador;
    [SerializeField] private Button botonVolver;
    [SerializeField] private Texture fondoAfiche;
    [SerializeField] private Sprite candado;
    private TMP_Text accion;

    private void Start()
    {
        Componer();
        for (int i = 0; i < niveles.Length; i++)
        {
            string escena = niveles[i].escena;
            niveles[i].boton.onClick.AddListener(() => ElegirNivel(escena));
        }

        Actualizar();
    }

    private void Componer()
    {
        Canvas canvas = botonVolver.GetComponentInParent<Canvas>();
        Transform raiz = botonVolver.transform.parent;
        TMP_FontAsset fuente = niveles[0].etiqueta.font;
        var fondo = EstiloTorneo.Imagen(raiz, "Fondo del selector", EstiloTorneo.Tinta, 0, 0, 1, 1);
        var panelAnterior = raiz.Find("Panel");
        if (panelAnterior != null) panelAnterior.gameObject.SetActive(false);
        fondo.transform.SetAsFirstSibling();
        if (fondoAfiche != null)
        {
            var textura = new GameObject("Muro del afiche", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            textura.transform.SetParent(fondo.transform, false);
            EstiloTorneo.Ubicar(textura.rectTransform, 0, 0, 1, 1);
            textura.texture = fondoAfiche;
            textura.color = new Color(.42f, .46f, .58f);
            textura.raycastTarget = false;
        }
        var titulo = raiz.Find("Titulo");
        if (titulo != null) titulo.gameObject.SetActive(false);
        EstiloTorneo.Texto(raiz, "ELIGE EL COMBATE", fuente, 82, .08f, .79f, .84f, .13f, EstiloTorneo.Papel);
        EstiloTorneo.Imagen(raiz, "Línea de encabezado", EstiloTorneo.Oro, .07f, .775f, .86f, .005f);

        for (int i = 0; i < niveles.Length; i++)
        {
            NivelEntrada nivel = niveles[i];
            var retratoAnterior = nivel.boton.transform.Find("Retrato Padre Pasta");
            if (retratoAnterior != null) retratoAnterior.gameObject.SetActive(false);
            float x = .065f + i * .223f;
            EstiloTorneo.Ubicar((RectTransform)nivel.boton.transform, x, .25f, .202f, .48f);
            EstiloTorneo.Boton(nivel.boton, nivel.etiqueta, false);
            EstiloTorneo.Ubicar(nivel.etiqueta.rectTransform, .07f, .075f, .86f, .29f);
            nivel.etiqueta.fontSizeMax = 30;
            var arte = EstiloTorneo.Imagen(nivel.boton.transform, "Escenario", Color.white, .045f, .38f, .91f, .565f);
            nivel.ilustracion = arte.rectTransform;
            arte.sprite = nivel.escenario;
            if (nivel.escenario == null) arte.color = EstiloTorneo.Azul;
            else arte.color = new Color(.72f, .72f, .78f);
            var retrato = EstiloTorneo.Imagen(arte.transform, "Retrato", Color.white, .07f, .015f, .86f, .96f);
            retrato.sprite = nivel.retrato;
            retrato.preserveAspect = true;
            retrato.enabled = nivel.retrato != null;
            nivel.icono = EstiloTorneo.Imagen(arte.transform, "Candado", EstiloTorneo.Papel, .78f, .76f, .17f, .19f);
            nivel.icono.sprite = candado;
            nivel.icono.preserveAspect = true;
            EstiloTorneo.Imagen(nivel.boton.transform, "Separador", EstiloTorneo.Oro, .045f, .37f, .91f, .008f);
        }

        var volver = botonVolver.GetComponentInChildren<TMP_Text>();
        volver.text = "Volver al inicio";
        EstiloTorneo.Ubicar((RectTransform)botonVolver.transform, .065f, .075f, .20f, .07f);
        EstiloTorneo.Boton(botonVolver, volver, false);
        foreach (Button boton in canvas.GetComponentsInChildren<Button>(true))
        {
            if (boton.name != "BotonReiniciarProgreso") continue;
            EstiloTorneo.Ubicar((RectTransform)boton.transform, .735f, .075f, .20f, .07f);
            EstiloTorneo.Boton(boton, boton.GetComponentInChildren<TMP_Text>(), false);
        }
        accion = EstiloTorneo.Texto(raiz, "", fuente, 30, .26f, .13f, .48f, .07f, EstiloTorneo.Oro);
    }

    private void LateUpdate()
    {
        if (EventSystem.current == null) return;
        GameObject foco = EventSystem.current.currentSelectedGameObject;
        string texto = "";
        foreach (NivelEntrada nivel in niveles)
        {
            bool seleccionado = nivel.boton.gameObject == foco && nivel.boton.interactable;
            nivel.ilustracion.localScale = Vector3.one * (seleccionado ? 1.035f : 1f);
            if (seleccionado)
                texto = "Entrar al combate  ·  " + nivel.nombre;
        }
        if (accion.text != texto) accion.text = texto;
    }

    private void ElegirNivel(string escena)
    {
        if (!cargador.IntentarCargarEscena(escena)) return;

        foreach (NivelEntrada nivel in niveles)
            nivel.boton.interactable = false;
        botonVolver.interactable = false;
    }

    public void ReiniciarProgreso()
    {
        ProgresoJuego.Borrar();
        Actualizar();
    }

    private void Actualizar()
    {
        Button primerNivelDisponible = null;
        for (int i = 0; i < niveles.Length; i++)
        {
            NivelEntrada n = niveles[i];

            bool existe = Application.CanStreamedLevelBeLoaded(n.escena);
            bool desbloqueado = ProgresoJuego.NivelDesbloqueado(i);
            bool completado = ProgresoJuego.NivelCompletado(i);

            n.boton.interactable = existe && ProgresoJuego.PuedeJugarNivel(i);
            bool bloqueado = !desbloqueado && !completado;
            n.icono.sprite = bloqueado ? candado : null;
            n.icono.color = bloqueado ? EstiloTorneo.Papel : completado
                ? new Color32(143, 218, 155, 255) : EstiloTorneo.Oro;
            n.icono.rectTransform.localRotation = Quaternion.Euler(0, 0, bloqueado ? 0 : 45);
            n.icono.rectTransform.localScale = Vector3.one * (bloqueado ? 1f : .45f);
            n.icono.enabled = bloqueado ? candado != null : completado || existe;
            if (n.boton.interactable && primerNivelDisponible == null)
                primerNivelDisponible = n.boton;

            ColorBlock colores = n.boton.colors;
            colores.normalColor = EstiloTorneo.Azul;
            colores.disabledColor = completado
                ? new Color32(156, 210, 163, 255)
                : new Color32(157, 157, 166, 255);
            n.boton.colors = colores;

            string estado;
            if (completado)
                estado = (ProgresoJuego.JefeDerrotado(i) ? "COMPLETADO" : "Fase 1 superada") +
                    "\n<size=65%>Reinicia el progreso para repetir</size>";
            else if (!desbloqueado)
                estado = "BLOQUEADO\n<size=65%>" + (i == 1
                    ? "Derrota a René Puente"
                    : "Derrota a los tres jefes anteriores") + "</size>";
            else
                estado = existe ? "DISPONIBLE" : "PRÓXIMAMENTE";

            n.etiqueta.text = n.nombre + "\n" + estado;
        }

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(
                (primerNivelDisponible != null ? primerNivelDisponible : botonVolver).gameObject);
    }
}
