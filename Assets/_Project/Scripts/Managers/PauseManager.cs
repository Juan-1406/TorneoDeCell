using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private GameObject panelPausa;
    [SerializeField] private GameObject primerBoton;
    [SerializeField] private SceneLoader cargador;

    private Button[] botones;
    private bool saliendo;
    private CanvasGroup grupo;

    private void Awake()
    {
        botones = panelPausa.GetComponentsInChildren<Button>(true);
        var raiz = (RectTransform)panelPausa.transform;
        var fondo = panelPausa.GetComponent<Image>();
        fondo.sprite = null;
        fondo.color = new Color(.025f, .037f, .065f, .76f);
        grupo = panelPausa.AddComponent<CanvasGroup>();
        var panelAnterior = raiz.Find("Panel");
        if (panelAnterior != null) panelAnterior.GetComponent<Image>().enabled = false;
        var titulo = panelPausa.GetComponentInChildren<TMP_Text>(true);
        foreach (TMP_Text etiqueta in panelPausa.GetComponentsInChildren<TMP_Text>(true))
            if (etiqueta.text == "PAUSA") titulo = etiqueta;
        var marco = EstiloTorneo.Marco(raiz, "Marco de pausa", .32f, .20f, .36f, .60f);
        marco.transform.parent.SetAsFirstSibling();
        titulo.transform.SetParent(marco.transform, false);
        EstiloTorneo.Ubicar(titulo.rectTransform, .1f, .70f, .8f, .25f);
        titulo.color = EstiloTorneo.Papel;
        titulo.fontStyle = FontStyles.Bold;
        titulo.fontSize = 68;
        titulo.alignment = TextAlignmentOptions.Center;
        EstiloTorneo.Imagen(marco.transform, "Línea de encabezado", EstiloTorneo.Oro, .10f, .68f, .8f, .005f);
        foreach (Button boton in botones)
        {
            bool principal = boton.gameObject == primerBoton;
            int fila = principal ? 0 : boton.onClick.GetPersistentMethodName(0) == nameof(Reintentar) ? 1 : 2;
            boton.transform.SetParent(marco.transform, false);
            EstiloTorneo.Ubicar((RectTransform)boton.transform, .12f, .49f - fila * .17f, .76f, .125f);
            var etiqueta = boton.GetComponentInChildren<TMP_Text>(true);
            if (etiqueta.text == "Volver al menú") etiqueta.text = "Volver al menú principal";
            EstiloTorneo.Boton(boton, etiqueta, principal);
        }
        SesionJuego.EnPausa = false;
        panelPausa.SetActive(false);
    }

    private void Update()
    {
        if (!Input.GetButtonDown("Cancel")) return;

        if (SesionJuego.EnPausa) Reanudar();
        else Pausar();
    }

    public void Pausar()
    {
        if (saliendo || !isActiveAndEnabled || Time.timeScale == 0f) return;

        SesionJuego.EnPausa = true;
        Time.timeScale = 0f;
        panelPausa.SetActive(true);
        StartCoroutine(Abrir());
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(primerBoton);
    }

    public void Reanudar()
    {
        if (saliendo || !SesionJuego.EnPausa) return;

        SesionJuego.EnPausa = false;
        SesionJuego.SoltarEntradaDeMenu = true;
        Time.timeScale = 1f;
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
        panelPausa.SetActive(false);
    }

    private IEnumerator Abrir()
    {
        grupo.alpha = 0f;
        for (float t = 0f; t < .15f && SesionJuego.EnPausa; t += Time.unscaledDeltaTime)
        {
            grupo.alpha = Mathf.SmoothStep(0f, 1f, t / .15f);
            yield return null;
        }
        grupo.alpha = 1f;
    }

    public void Reintentar()
    {
        SalirDeLaPausa(SceneManager.GetActiveScene().name);
    }

    public void VolverAlMenu()
    {
        SalirDeLaPausa("MainMenu");
    }

    public void VolverASeleccion()
    {
        SalirDeLaPausa("LevelSelect");
    }

    private void SalirDeLaPausa(string escena)
    {
        if (saliendo || !isActiveAndEnabled || !SesionJuego.EnPausa) return;
        if (!cargador.IntentarCargarEscena(escena)) return;

        saliendo = true;
        foreach (Button boton in botones)
            boton.interactable = false;
    }

    private void OnDisable()
    {
        if (SesionJuego.EnPausa && panelPausa != null && panelPausa.activeSelf)
            Reanudar();
    }
}
