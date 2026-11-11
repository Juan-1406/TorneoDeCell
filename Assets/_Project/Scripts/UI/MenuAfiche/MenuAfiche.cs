using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Presentación exclusiva de MainMenu; no interviene en la lógica de combate.</summary>
public sealed class MenuAfiche : MonoBehaviour
{
    [Header("Composición aprobada · 1672 × 941")]
    [SerializeField] private RectTransform marco;
    [SerializeField] private RectTransform fondo;
    [SerializeField] private RectTransform titulo;
    [SerializeField] private RectTransform[] recortes;
    [SerializeField] private BotonAfiche jugar, salir;
    [SerializeField] private CanvasGroup entradaTitulo, entradaBotones;
    [SerializeField] private LuzAfiche[] lucesParlante;
    [SerializeField] private LuzAfiche luzCamara, luzCalle;
    [Header("Sonido original del menú")]
    [SerializeField] private AudioClip musica, seleccion, confirmar, papel;
    [SerializeField, Range(0, 1)] private float volumenMusica = .24f;
    [SerializeField] private string escenaJugar = "LevelSelect";

    private Vector2[] origenes;
    private CanvasGroup[] entradas;
    private Vector2 origenFondo, origenTitulo, mirada, velocidadMirada;
    private AudioSource pista, efectos;
    private float inicio, ultimoSonido = -1;
    private bool foco = true, cambiando;
    private Material materialTitulo;
    public bool EnTransicion => cambiando;
    public BotonAfiche Jugar => jugar;
    public BotonAfiche Salir => salir;

    public void Configurar(RectTransform contenedor, RectTransform pared, RectTransform logo,
        RectTransform[] personajes, BotonAfiche botonJugar, BotonAfiche botonSalir,
        CanvasGroup logoEntrada, CanvasGroup botonesEntrada, LuzAfiche[] parlante,
        LuzAfiche camara, LuzAfiche calle, AudioClip loop, AudioClip hover, AudioClip aceptar, AudioClip rasgar)
    {
        marco = contenedor; fondo = pared; titulo = logo; recortes = personajes;
        jugar = botonJugar; salir = botonSalir; entradaTitulo = logoEntrada; entradaBotones = botonesEntrada;
        lucesParlante = parlante; luzCamara = camara; luzCalle = calle;
        musica = loop; seleccion = hover; confirmar = aceptar; papel = rasgar;
    }

    private void Awake()
    {
        Time.timeScale = 1;
        SesionJuego.Reiniciar();
        inicio = Time.unscaledTime;
        origenes = new Vector2[recortes.Length];
        entradas = new CanvasGroup[recortes.Length];
        for (int i = 0; i < recortes.Length; i++)
        {
            origenes[i] = recortes[i].anchoredPosition;
            entradas[i] = recortes[i].GetComponent<CanvasGroup>();
        }
        origenFondo = fondo.anchoredPosition;
        origenTitulo = titulo.anchoredPosition;
        var imagenTitulo = titulo.GetComponent<RawImage>();
        if (imagenTitulo.material != null)
        {
            materialTitulo = new Material(imagenTitulo.material);
            imagenTitulo.material = materialTitulo;
        }
        pista = gameObject.AddComponent<AudioSource>();
        pista.playOnAwake = false; pista.clip = musica; pista.loop = true;
        pista.volume = 0; pista.spatialBlend = 0;
        efectos = gameObject.AddComponent<AudioSource>();
        efectos.playOnAwake = false; efectos.spatialBlend = 0; efectos.volume = .42f;
        if (musica != null) pista.Play();
        if (papel != null) efectos.PlayOneShot(papel, .18f);
        jugar.onClick.AddListener(JugarAhora);
        salir.onClick.AddListener(SalirAhora);
        ActualizarComposicion();
    }

    private void Start()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.firstSelectedGameObject = jugar.gameObject;
            EventSystem.current.SetSelectedGameObject(jugar.gameObject);
        }
    }

    private void LateUpdate() => ActualizarComposicion();

    // También se usa al verificar otras relaciones de aspecto en el editor.
    public void ActualizarComposicion()
    {
        if (marco == null || !Application.isPlaying || origenes == null) return;
        var area = (RectTransform)marco.parent;
        float escala = Mathf.Min(area.rect.width / 1672f, area.rect.height / 941f);
        marco.localScale = Vector3.one * escala;
        float t = Time.unscaledTime - inicio;
        Vector2 objetivo = Vector2.zero;
        if (foco && Mouse.current != null && Screen.width > 0 && Screen.height > 0)
        {
            Vector2 p = Mouse.current.position.ReadValue();
            objetivo = new Vector2(Mathf.Clamp(p.x / Screen.width * 2 - 1, -1, 1),
                Mathf.Clamp(p.y / Screen.height * 2 - 1, -1, 1));
        }
        mirada = Vector2.SmoothDamp(mirada, objetivo, ref velocidadMirada, .18f, 100, Time.unscaledDeltaTime);
        fondo.anchoredPosition = origenFondo + mirada * 1.3f;
        for (int i = 0; i < recortes.Length; i++)
        {
            float entrada = Suave((t - .045f - i * .085f) / .50f);
            float fase = i * 1.73f;
            float x = Mathf.Sin(t * .48f + fase) * .8f;
            float y = Mathf.Sin(t * .79f + fase) * 1.25f;
            Vector2 paralaje = mirada * (2.4f + i * .42f);
            recortes[i].anchoredPosition = origenes[i] + new Vector2(x, y) + paralaje +
                new Vector2((i % 2 == 0 ? 14 : -14) * (1 - entrada), -19 * (1 - entrada));
            recortes[i].localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * .53f + fase) * .085f);
            entradas[i].alpha = entrada;
        }
        float entradaLogo = Suave((t - .08f) / .55f);
        entradaTitulo.alpha = entradaLogo;
        titulo.anchoredPosition = origenTitulo + new Vector2(0, (1 - entradaLogo) * 12);
        titulo.localScale = Vector3.one * Mathf.Lerp(.96f, 1, entradaLogo);
        float botones = Suave(t / .42f);
        entradaBotones.alpha = botones;
        jugar.Entrada(botones); salir.Entrada(botones);
        if (materialTitulo != null) materialTitulo.SetFloat("_Reloj", t);

        float pulso = Mathf.Exp(-Mathf.Repeat(t, 60f / 110f) * 9f);
        for (int i = 0; i < lucesParlante.Length; i++)
        {
            lucesParlante[i].color = new Color(.15f, .58f, 1f, .25f + pulso * .28f);
            lucesParlante[i].transform.localScale = Vector3.one * (1 + pulso * .12f);
        }
        if (luzCamara != null)
            luzCamara.color = new Color(1, .72f, .25f, .20f + .04f * Mathf.Sin(t * .8f));
        if (luzCalle != null)
            luzCalle.color = new Color(1, .60f, .19f, .28f + .025f * Mathf.Sin(t * .67f) + .015f * Mathf.Sin(t * 1.91f));
        if (pista != null)
        {
            float volumen = cambiando ? 0 : volumenMusica * (foco ? 1 : .25f) * Suave(t / 1.2f);
            pista.volume = Mathf.MoveTowards(pista.volume, volumen, Time.unscaledDeltaTime * .7f);
        }
    }

    public void SonarSeleccion()
    {
        if (cambiando || Time.unscaledTime - inicio < .4f || Time.unscaledTime - ultimoSonido < .07f) return;
        ultimoSonido = Time.unscaledTime;
        if (efectos != null && seleccion != null) efectos.PlayOneShot(seleccion, .38f);
    }

    public void SeleccionarSalir()
    {
        if (!cambiando && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(salir.gameObject);
    }

    public void JugarAhora()
    {
        if (!Bloquear(jugar)) return;
        StartCoroutine(EntrarAlTorneo());
    }
    public void SalirAhora()
    {
        if (!Bloquear(salir)) return;
        StartCoroutine(Cerrar());
    }
    private bool Bloquear(BotonAfiche elegido)
    {
        if (cambiando) return false;
        cambiando = true;
        elegido.ConfirmarVisual();
        jugar.interactable = salir.interactable = false;
        if (confirmar != null) efectos.PlayOneShot(confirmar, .65f);
        return true;
    }
    private IEnumerator EntrarAlTorneo()
    {
        yield return new WaitForSecondsRealtime(.14f);
        if (papel != null) efectos.PlayOneShot(papel, .5f);
        TransicionPapelAfiche.Cargar(escenaJugar);
    }
    private IEnumerator Cerrar()
    {
        yield return new WaitForSecondsRealtime(.26f);
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
    private static float Suave(float x) => Mathf.SmoothStep(0, 1, Mathf.Clamp01(x));
    private void OnApplicationFocus(bool valor) => foco = valor;
    private void OnDestroy()
    {
        if (jugar != null) jugar.onClick.RemoveListener(JugarAhora);
        if (salir != null) salir.onClick.RemoveListener(SalirAhora);
        if (materialTitulo != null)
        {
            if (Application.isPlaying) Destroy(materialTitulo);
            else DestroyImmediate(materialTitulo);
        }
    }
}
