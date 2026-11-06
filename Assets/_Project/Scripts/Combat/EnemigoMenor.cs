using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Health), typeof(Rigidbody2D), typeof(CircleCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class EnemigoMenor : MonoBehaviour
{
    public enum TipoAtaque { Cercano, Proyectil }
    public enum EstadoEnemigo { Entrada, Buscando, Amago, Carga, Recuperacion, PreparandoRafaga, Disparando, Reposicion }
    [Header("Movimiento lateral")]
    [SerializeField] private TipoAtaque tipoAtaque;
    [SerializeField, Min(0f)] private float velocidad = 2.4f;
    [SerializeField, Min(0f)] private float distanciaMinima = 2.8f;
    [SerializeField, Min(0.1f)] private float aceleracion = 9f;
    [Header("Ataque")]
    [SerializeField, Min(0.1f)] private float distanciaAtaque = 1.3f;
    [SerializeField, Min(0.1f)] private float intervaloAtaque = 1.4f;
    [SerializeField, Min(0.1f)] private float avisoAtaque = 0.45f;
    [SerializeField, Min(0f)] private float esperaTrasAparecer = 0.8f;
    [SerializeField, Min(1)] private int danio = 1;
    [SerializeField] private Bullet proyectilPrefab;
    [Header("Carga melee")]
    [SerializeField, Min(1f)] private float alcanceCarga = 4.5f;
    [SerializeField, Min(1f)] private float velocidadCarga = 7f;
    [SerializeField, Min(0.1f)] private float duracionCarga = 0.32f;
    [Header("Ráfaga ranged")]
    [SerializeField, Range(1, 3)] private int disparosRafaga = 2;
    [SerializeField, Range(1, 5)] private int proyectilesAbanico = 3;
    [SerializeField, Range(0f, 25f)] private float dispersion = 11f;
    [SerializeField, Min(0.1f)] private float intervaloRafaga = 0.28f;
    [SerializeField, Min(0.7f)] private float recargaEvasion = 1.4f;

    private Health vida, jugador;
    private Rigidbody2D cuerpo;
    private CircleCollider2D colision;
    private SpriteRenderer imagen;
    private Transform visual;
    private PresentacionEnemigoMenor presentacion;
    private Color colorBase;
    private float siguienteAtaque, velocidadX, flashHasta, reubicarHasta, revisarAmenaza;
    private float siguienteEvasion;
    private float direccionCarga, direccionReubicar;
    private float limiteIzquierdo = float.NegativeInfinity, limiteDerecho = float.PositiveInfinity;
    private bool eliminado, golpeAplicado;
    private Vector2 objetivoAtaque;
    private RecogibleCombate recompensaPrefab;
    private float probabilidadRecompensa;
    private RecompensasCombate recompensas;
    private static readonly HashSet<EnemigoMenor> presentes = new HashSet<EnemigoMenor>();
    public EstadoEnemigo EstadoActual { get; private set; }
    public bool PreparandoAtaque => EstadoActual == EstadoEnemigo.Amago || EstadoActual == EstadoEnemigo.PreparandoRafaga;
    public bool Entrando => EstadoActual == EstadoEnemigo.Entrada;
    public TipoAtaque Tipo => tipoAtaque;
    public float VelocidadHorizontal => velocidadX;
    public Health Vida => vida;
    public Transform ObjetivoVisual => jugador != null ? jugador.transform : null;
    public Vector2 PuntoApuntado => objetivoAtaque;
    public float SentidoCarga => direccionCarga;
    public event Action<EnemigoMenor> Eliminado;
    public event Action<EnemigoMenor> Retirado;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarRegistro() => presentes.Clear();
    private void OnEnable() => presentes.Add(this);
    private void Awake()
    {
        vida = GetComponent<Health>(); cuerpo = GetComponent<Rigidbody2D>(); colision = GetComponent<CircleCollider2D>();
        var original = GetComponent<SpriteRenderer>();
        colorBase = original.color;
        presentacion = GetComponent<PresentacionEnemigoMenor>();
        if (presentacion != null)
        {
            visual = presentacion.VisualEntrada;
            imagen = presentacion.Imagen;
            presentacion.Vincular(this, vida);
        }
        else
        {
            var go = new GameObject("Visual enemigo");
            visual = go.transform; visual.SetParent(transform, false);
            imagen = go.AddComponent<SpriteRenderer>();
            imagen.sprite = original.sprite != null ? original.sprite : FeedbackCombate.Punto;
            imagen.sharedMaterial = original.sharedMaterial;
            imagen.sortingLayerID = original.sortingLayerID; imagen.sortingOrder = original.sortingOrder;
            imagen.color = colorBase;
        }
        original.enabled = false;
        vida.OnMuerte += AlMorir;
        vida.OnDanioRecibido += AlRecibirDanio;
    }
    private void OnDisable()
    {
        presentes.Remove(this); StopAllCoroutines(); velocidadX = 0;
        if (imagen != null) imagen.color = presentacion != null ? Color.white : colorBase;
    }
    private void OnDestroy()
    {
        if (vida != null) { vida.OnMuerte -= AlMorir; vida.OnDanioRecibido -= AlRecibirDanio; }
        Retirado?.Invoke(this);
    }
    public void Configurar(Health objetivo) => Configurar(objetivo, float.NegativeInfinity, float.PositiveInfinity);
    public void Configurar(Health objetivo, float izquierda, float derecha)
    {
        jugador = objetivo; limiteIzquierdo = izquierda; limiteDerecho = derecha;
        siguienteAtaque = Time.time + esperaTrasAparecer + UnityEngine.Random.Range(0f, 0.2f);
        StartCoroutine(Entrada());
    }
    public void ConfigurarRecompensa(RecogibleCombate prefab, float probabilidad, RecompensasCombate propietario = null)
    {
        recompensaPrefab = prefab; probabilidadRecompensa = Mathf.Clamp01(probabilidad); recompensas = propietario;
    }
    private IEnumerator Entrada()
    {
        EstadoActual = EstadoEnemigo.Entrada;
        colision.enabled = false; vida.Invulnerable = true;
        float tiempo = 0;
        while (tiempo < 0.38f)
        {
            tiempo += Time.deltaTime;
            float t = Mathf.Clamp01(tiempo / 0.38f);
            // Overshoot visual; la colisión conserva su tamaño durante todo el efecto.
            float escala = 1f + 2.70158f * Mathf.Pow(t - 1f, 3) + 1.70158f * Mathf.Pow(t - 1f, 2);
            visual.localScale = Vector3.one * Mathf.Max(0.05f, escala);
            visual.localPosition = Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.35f;
            yield return null;
        }
        visual.localScale = Vector3.one; visual.localPosition = Vector3.zero;
        if (jugador == null || jugador.VidaActual <= 0) yield break;
        colision.enabled = true; vida.Invulnerable = false; EstadoActual = EstadoEnemigo.Buscando;
        FeedbackCombate.Emitir(this, colorBase, 5, 1.7f);
    }
    private bool PuedeActuar => jugador != null && jugador.VidaActual > 0 && vida.VidaActual > 0 && !eliminado;
    private void FixedUpdate()
    {
        if (!PuedeActuar || SesionJuego.EnPausa || Entrando) return;
        float direccion = 0f;
        float dx = jugador.transform.position.x - cuerpo.position.x;
        if (EstadoActual == EstadoEnemigo.Carga)
        {
            Mover(direccionCarga * velocidadCarga * Time.fixedDeltaTime, false);
            Vector2 delta = jugador.transform.position - transform.position;
            if (!golpeAplicado && delta.magnitude <= distanciaAtaque &&
                delta.x * direccionCarga >= -0.3f && TieneLineaLibre(jugador.transform.position))
            { golpeAplicado = true; jugador.RecibirDanio(danio); }
            return;
        }
        if (EstadoActual == EstadoEnemigo.Amago) direccion = -direccionCarga * 0.25f;
        else if (EstadoActual == EstadoEnemigo.Reposicion) direccion = direccionReubicar * 1.45f;
        else if (EstadoActual == EstadoEnemigo.Buscando)
        {
            if (tipoAtaque == TipoAtaque.Cercano)
                direccion = Mathf.Abs(dx) > distanciaAtaque * 0.85f ? Mathf.Sign(dx) : 0f;
            else if (Mathf.Abs(dx) < distanciaMinima + 0.4f) direccion = -Mathf.Sign(dx);
            else if (Mathf.Abs(dx) > distanciaAtaque * 0.72f) direccion = Mathf.Sign(dx);
        }
        velocidadX = Mathf.MoveTowards(velocidadX, direccion * velocidad, aceleracion * Time.fixedDeltaTime);
        Mover(velocidadX * Time.fixedDeltaTime, true);
    }
    private void Mover(float paso, bool separar)
    {
        if (Mathf.Abs(paso) < 0.0001f) return;
        float x = Mathf.Clamp(cuerpo.position.x + paso, limiteIzquierdo, limiteDerecho);
        if (separar)
            foreach (var otro in presentes)
            {
                if (otro == this || otro == null || otro.vida.VidaActual <= 0 || otro.gameObject.scene != gameObject.scene) continue;
                float distancia = Mathf.Abs(otro.transform.position.x - cuerpo.position.x);
                if (Mathf.Abs(otro.transform.position.x - x) < 1.1f &&
                    Mathf.Abs(otro.transform.position.x - x) < distancia) { velocidadX = 0f; return; }
            }
        var pared = Physics2D.CircleCast(cuerpo.position, colision.radius * 0.95f,
            Vector2.right * Mathf.Sign(paso), Mathf.Abs(x - cuerpo.position.x) + 0.05f, LayerMask.GetMask("Ground"));
        if (pared.collider != null && !pared.collider.isTrigger) { velocidadX = 0f; return; }
        cuerpo.MovePosition(new Vector2(x, cuerpo.position.y));
    }
    private void Update()
    {
        if (!PuedeActuar || SesionJuego.EnPausa || Time.timeScale == 0f || Entrando) return;
        if (EstadoActual == EstadoEnemigo.Reposicion && Time.time >= reubicarHasta) EstadoActual = EstadoEnemigo.Buscando;
        if (tipoAtaque == TipoAtaque.Proyectil && EstadoActual == EstadoEnemigo.Buscando &&
            Time.time >= revisarAmenaza && Time.time >= siguienteEvasion)
        {
            revisarAmenaza = Time.time + 0.18f;
            foreach (var bala in Bullet.Activas)
                if (bala != null && bala.gameObject.scene == gameObject.scene && bala.AmenazaA(cuerpo.position))
                { Reposicionar(); break; }
        }
        if (EstadoActual != EstadoEnemigo.Buscando || Time.time < siguienteAtaque) return;
        float rango = tipoAtaque == TipoAtaque.Cercano ? alcanceCarga : distanciaAtaque;
        if (Vector2.Distance(transform.position, jugador.transform.position) > rango ||
            !TieneLineaLibre(jugador.transform.position)) return;
        if (tipoAtaque == TipoAtaque.Proyectil && proyectilPrefab == null) return;
        StartCoroutine(tipoAtaque == TipoAtaque.Cercano ? Cargar() : Rafaga());
    }
    private IEnumerator Cargar()
    {
        EstadoActual = EstadoEnemigo.Amago; golpeAplicado = false;
        objetivoAtaque = jugador.transform.position;
        direccionCarga = objetivoAtaque.x >= cuerpo.position.x ? 1f : -1f;
        yield return new WaitForSeconds(Mathf.Max(0.45f, avisoAtaque));
        if (!PuedeActuar) yield break;
        velocidadX = 0f; EstadoActual = EstadoEnemigo.Carga;
        FeedbackCombate.Emitir(this, colorBase, 4, 1f);
        yield return new WaitForSeconds(duracionCarga);
        EstadoActual = EstadoEnemigo.Recuperacion;
        yield return new WaitForSeconds(0.55f);
        siguienteAtaque = Time.time + intervaloAtaque * 0.65f;
        EstadoActual = EstadoEnemigo.Buscando;
    }
    private IEnumerator Rafaga()
    {
        EstadoActual = EstadoEnemigo.PreparandoRafaga;
        objetivoAtaque = jugador.transform.position;
        yield return new WaitForSeconds(Mathf.Max(0.55f, avisoAtaque));
        if (!PuedeActuar) yield break;
        EstadoActual = EstadoEnemigo.Disparando;
        Vector2 direccion = objetivoAtaque - (Vector2)transform.position;
        if (direccion.sqrMagnitude > 0.01f && TieneLineaLibre(objetivoAtaque))
            for (int ronda = 0; ronda < disparosRafaga; ronda++)
            {
                if (!PuedeActuar) yield break;
                for (int i = 0; i < proyectilesAbanico; i++)
                {
                    float angulo = (i - (proyectilesAbanico - 1) * 0.5f) * dispersion;
                    Vector2 vector = Quaternion.Euler(0, 0, angulo) * direccion.normalized;
                    var bala = Instantiate(proyectilPrefab, transform.position + (Vector3)(vector * 0.7f), Quaternion.identity);
                    bala.Disparar(vector);
                }
                flashHasta = Time.time + 0.08f;
                if (presentacion != null) presentacion.PulsoDisparo(direccion.normalized);
                if (ronda < disparosRafaga - 1) yield return new WaitForSeconds(intervaloRafaga);
            }
        siguienteAtaque = Time.time + intervaloAtaque;
        Reposicionar();
    }
    private void Reposicionar()
    {
        float lejos = cuerpo.position.x >= jugador.transform.position.x ? 1f : -1f;
        if (cuerpo.position.x + lejos * 1.5f > limiteDerecho || cuerpo.position.x + lejos * 1.5f < limiteIzquierdo) lejos = -lejos;
        direccionReubicar = lejos; reubicarHasta = Time.time + 0.65f;
        siguienteEvasion = Time.time + Mathf.Max(0.7f, recargaEvasion);
        EstadoActual = EstadoEnemigo.Reposicion;
    }
    private void LateUpdate()
    {
        if (presentacion != null || visual == null || Entrando || SesionJuego.EnPausa || Time.timeScale == 0f) return;
        Color tinte = PreparandoAtaque ? Color.Lerp(colorBase, Color.white, 0.55f + 0.2f * Mathf.Sin(Time.time * 18f)) : colorBase;
        imagen.color = Time.time < flashHasta ? Color.white : tinte;
        Vector3 escala = Vector3.one;
        if (EstadoActual == EstadoEnemigo.Amago) escala = new Vector3(0.8f, 1.18f, 1);
        else if (EstadoActual == EstadoEnemigo.Carga) escala = new Vector3(1.3f, 0.78f, 1);
        else if (EstadoActual == EstadoEnemigo.PreparandoRafaga) escala = new Vector3(1.1f, 0.9f, 1);
        visual.localScale = Vector3.Lerp(visual.localScale, escala, 18f * Time.deltaTime);
        visual.localPosition = Vector3.up * (Mathf.Abs(velocidadX) > 0.2f ? Mathf.Abs(Mathf.Sin(Time.time * 11f)) * 0.09f : 0f);
    }
    private void AlRecibirDanio()
    {
        flashHasta = Time.time + 0.1f;
        FeedbackCombate.Emitir(this, colorBase, 5, 2.5f);
        if (tipoAtaque == TipoAtaque.Proyectil && EstadoActual == EstadoEnemigo.Buscando &&
            jugador != null && Time.time >= siguienteEvasion) Reposicionar();
    }
    private void AlMorir()
    {
        if (eliminado) return;
        eliminado = true; StopAllCoroutines();
        if (presentacion != null) presentacion.MostrarDerrota();
        FeedbackCombate.Emitir(this, colorBase, 12, 4f, 0.06f);
        if (recompensas != null) recompensas.IntentarCrear(recompensaPrefab, probabilidadRecompensa, transform.position);
        Eliminado?.Invoke(this); gameObject.SetActive(false); Destroy(gameObject);
    }
    private bool TieneLineaLibre(Vector2 destino)
    {
        var impacto = Physics2D.Linecast(transform.position, destino, LayerMask.GetMask("Ground"));
        return impacto.collider == null || impacto.collider.isTrigger;
    }
}

