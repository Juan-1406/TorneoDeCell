using UnityEngine;

[RequireComponent(typeof(CircleCollider2D), typeof(SpriteRenderer))]
public class RecogibleCombate : MonoBehaviour
{
    public enum TipoRecompensa { Curacion, Cadencia }
    [SerializeField] private TipoRecompensa tipo;
    [SerializeField, Min(1)] private int puntosCuracion = 1;
    [SerializeField, Range(0.1f, 0.99f)] private float multiplicadorCadencia = 0.65f;
    [SerializeField, Min(0.1f)] private float duracionMejora = 8f;
    [SerializeField, Min(0.1f)] private float tiempoEnSuelo = 15f;
    [SerializeField, Min(0f)] private float demoraRecogida = 0.35f;
    private static readonly Sprite[] figuras = new Sprite[2];
    private Health jugador;
    private RecompensasCombate director;
    private CircleCollider2D zona;
    private SpriteRenderer imagen;
    private Color colorBase;
    private float restante;
    private float demora;
    private bool consumido;
    public TipoRecompensa Tipo => tipo;
    public float SegundosRestantes => restante;

    private void Awake()
    {
        zona = GetComponent<CircleCollider2D>();
        zona.isTrigger = true;
        imagen = GetComponent<SpriteRenderer>();
        colorBase = imagen.color;
        if (imagen.sprite == null) imagen.sprite = Figura(tipo);
        restante = Mathf.Max(1f, tiempoEnSuelo);
        demora = demoraRecogida;
    }

    public void Configurar(Health objetivo, RecompensasCombate propietario)
    {
        jugador = objetivo;
        director = propietario;
    }

    private void Start()
    {
        if (jugador == null)
        {
            var objetivo = GameObject.FindGameObjectWithTag("Player");
            if (objetivo != null) jugador = objetivo.GetComponent<Health>();
        }
    }

    private void Update()
    {
        if (consumido || SesionJuego.EnPausa || Time.timeScale == 0f) return;
        if (director != null && director.Finalizado) return;
        // La pausa entre rondas permite recoger sin gastar la duración del objeto.
        if (director == null || !director.EnDescanso) restante -= Time.deltaTime;
        demora = Mathf.Max(0f, demora - Time.deltaTime);
        if (restante <= 0f) { Destroy(gameObject); return; }
        Color color = colorBase;
        if (restante < 3f) color.a *= 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(Time.time * 9f));
        imagen.color = color;
        if (jugador == null || jugador.VidaActual <= 0 || demora > 0f) return;
        var colliderJugador = jugador.GetComponent<Collider2D>();
        if (colliderJugador == null || !colliderJugador.enabled) return;
        float radio = zona.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        Vector2 centro = transform.TransformPoint(zona.offset);
        if (((Vector2)colliderJugador.ClosestPoint(centro) - centro).sqrMagnitude <= radio * radio)
            IntentarRecoger();
    }

    public bool IntentarRecoger()
    {
        if (consumido || demora > 0f || SesionJuego.EnPausa || Time.timeScale == 0f ||
            jugador == null || jugador.VidaActual <= 0 || (director != null && director.Finalizado)) return false;
        bool recogido;
        if (tipo == TipoRecompensa.Curacion) recogido = jugador.Curar(puntosCuracion);
        else
        {
            var disparo = jugador.GetComponent<PlayerShooting>();
            recogido = disparo != null && disparo.isActiveAndEnabled;
            if (recogido) disparo.MejorarCadenciaTemporal(multiplicadorCadencia, duracionMejora);
        }
        if (!recogido) return false;
        consumido = true;
        if (director != null) director.AlRecoger(this);
        gameObject.SetActive(false);
        Destroy(gameObject);
        return true;
    }

    private void OnDestroy()
    {
        if (director != null) director.Retirar(this);
    }

    private static Sprite Figura(TipoRecompensa tipo)
    {
        int indice = (int)tipo;
        if (figuras[indice] != null) return figuras[indice];
        var textura = new Texture2D(16, 16, TextureFormat.RGBA32, false)
            { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var pixeles = new Color[256];
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                bool visible = tipo == TipoRecompensa.Curacion
                    ? (x >= 6 && x <= 9 && y >= 2 && y <= 13) || (y >= 6 && y <= 9 && x >= 2 && x <= 13)
                    : (x >= 4 && x <= 7 && y >= 3 && y <= 12) || (x >= 9 && x <= 12 && y >= 3 && y <= 12);
                pixeles[y * 16 + x] = visible ? Color.white : Color.clear;
            }
        textura.SetPixels(pixeles);
        textura.Apply();
        return figuras[indice] = Sprite.Create(textura, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
    }
}

