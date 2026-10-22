using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health), typeof(Rigidbody2D), typeof(CircleCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class EnemigoMenor : MonoBehaviour
{
    public enum TipoAtaque { Cercano, Proyectil }

    [Header("Movimiento lateral")]
    [SerializeField] private TipoAtaque tipoAtaque;
    [SerializeField, Min(0f)] private float velocidad = 2.4f;
    [SerializeField, Min(0f)] private float distanciaMinima = 2.8f;

    [Header("Ataque")]
    [SerializeField, Min(0.1f)] private float distanciaAtaque = 1.3f;
    [SerializeField, Min(0.1f)] private float intervaloAtaque = 1.4f;
    [SerializeField, Min(0f)] private float avisoAtaque = 0.35f;
    [SerializeField, Min(0f)] private float esperaTrasAparecer = 0.8f;
    [SerializeField, Min(1)] private int danio = 1;
    [SerializeField] private Bullet proyectilPrefab;

    private static Sprite figuraProvisional;
    private Health vida;
    private Health jugador;
    private Rigidbody2D cuerpo;
    private SpriteRenderer imagen;
    private Color colorBase;
    private float siguienteAtaque;
    private float limiteIzquierdo = float.NegativeInfinity;
    private float limiteDerecho = float.PositiveInfinity;
    private bool preparandoAtaque;
    private RecogibleCombate recompensaPrefab;
    private float probabilidadRecompensa;

    public event Action<EnemigoMenor> Eliminado;
    public Health Vida => vida;

    private void Awake()
    {
        vida = GetComponent<Health>();
        cuerpo = GetComponent<Rigidbody2D>();
        imagen = GetComponent<SpriteRenderer>();
        colorBase = imagen.color;
        vida.OnMuerte += AlMorir;
        if (imagen.sprite == null) imagen.sprite = ObtenerFiguraProvisional();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        preparandoAtaque = false;
        if (imagen != null) imagen.color = colorBase;
    }

    private void OnDestroy()
    {
        if (vida != null) vida.OnMuerte -= AlMorir;
    }

    public void Configurar(Health objetivo)
    {
        Configurar(objetivo, float.NegativeInfinity, float.PositiveInfinity);
    }

    public void Configurar(Health objetivo, float izquierda, float derecha)
    {
        jugador = objetivo;
        limiteIzquierdo = izquierda;
        limiteDerecho = derecha;
        siguienteAtaque = Time.time + esperaTrasAparecer;
    }

    public void ConfigurarRecompensa(RecogibleCombate prefab, float probabilidad)
    {
        recompensaPrefab = prefab;
        probabilidadRecompensa = Mathf.Clamp01(probabilidad);
    }

    private void FixedUpdate()
    {
        if (jugador == null || jugador.VidaActual <= 0 || vida.VidaActual <= 0 || preparandoAtaque) return;

        float distanciaHorizontal = jugador.transform.position.x - cuerpo.position.x;
        float distancia = Mathf.Abs(distanciaHorizontal);
        float direccion = 0f;
        if (tipoAtaque == TipoAtaque.Cercano)
        {
            if (distancia > distanciaAtaque * 0.85f) direccion = Mathf.Sign(distanciaHorizontal);
        }
        else if (distancia > distanciaAtaque * 0.8f)
            direccion = Mathf.Sign(distanciaHorizontal);
        else if (distancia < distanciaMinima)
            direccion = -Mathf.Sign(distanciaHorizontal);

        if (direccion == 0f) return;
        float x = Mathf.Clamp(cuerpo.position.x + direccion * velocidad * Time.fixedDeltaTime,
            limiteIzquierdo, limiteDerecho);
        cuerpo.MovePosition(new Vector2(x, cuerpo.position.y));
    }

    private void Update()
    {
        if (jugador == null || jugador.VidaActual <= 0 || vida.VidaActual <= 0 ||
            preparandoAtaque || Time.time < siguienteAtaque) return;
        if (tipoAtaque == TipoAtaque.Proyectil && proyectilPrefab == null) return;
        if (Vector2.Distance(transform.position, jugador.transform.position) > distanciaAtaque) return;

        StartCoroutine(PrepararAtaque());
    }

    private IEnumerator PrepararAtaque()
    {
        preparandoAtaque = true;
        imagen.color = Color.Lerp(colorBase, Color.white, 0.65f);
        if (avisoAtaque > 0f) yield return new WaitForSeconds(avisoAtaque);
        imagen.color = colorBase;

        if (jugador != null && jugador.VidaActual > 0 && vida.VidaActual > 0)
        {
            Vector2 direccion = jugador.transform.position - transform.position;
            if (direccion.magnitude <= distanciaAtaque)
            {
                if (tipoAtaque == TipoAtaque.Cercano)
                    jugador.RecibirDanio(danio);
                else if (direccion.sqrMagnitude > 0.01f)
                {
                    Vector3 origen = transform.position + (Vector3)(direccion.normalized * 0.65f);
                    Bullet bala = Instantiate(proyectilPrefab, origen, Quaternion.identity);
                    bala.Disparar(direccion);
                }
            }
        }

        siguienteAtaque = Time.time + intervaloAtaque;
        preparandoAtaque = false;
    }

    private void AlMorir()
    {
        if (recompensaPrefab != null && UnityEngine.Random.value < probabilidadRecompensa)
            Instantiate(recompensaPrefab, transform.position + Vector3.up * 0.4f,
                Quaternion.identity);
        Eliminado?.Invoke(this);
    }

    private static Sprite ObtenerFiguraProvisional()
    {
        if (figuraProvisional != null) return figuraProvisional;

        var textura = new Texture2D(16, 16, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = "Figura temporal de enemigo menor"
        };
        var pixeles = new Color[16 * 16];
        for (int i = 0; i < pixeles.Length; i++) pixeles[i] = Color.white;
        textura.SetPixels(pixeles);
        textura.Apply();
        figuraProvisional = Sprite.Create(textura, new Rect(0, 0, 16, 16),
            new Vector2(0.5f, 0.5f), 16f);
        figuraProvisional.name = "Figura temporal de enemigo menor";
        return figuraProvisional;
    }
}
