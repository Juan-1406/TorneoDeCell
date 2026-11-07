using UnityEngine;
using System.Collections.Generic;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float velocidad = 15f;
    [SerializeField] private float tiempoVida = 3f;
    [SerializeField] private int danio = 10;

    [Header("Visual")]
    [SerializeField] private float orientacionSpriteGrados;

    [Header("Al ser reflejada (parry)")]
    [SerializeField] private int danioReflejado = 25;
    [SerializeField] private float multiplicadorVelocidad = 1.5f;
    [SerializeField] private Color colorReflejada = Color.cyan;
    [SerializeField] private Sprite spriteReflejado;

    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private float expira;
    private static readonly HashSet<Bullet> activas = new HashSet<Bullet>();
    public static IEnumerable<Bullet> Activas => activas;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarRegistro() => activas.Clear();
    private void OnEnable() => activas.Add(this);
    private void OnDisable() => activas.Remove(this);
    public bool AmenazaA(Vector2 posicion)
    {
        if (gameObject.layer != LayerMask.NameToLayer("PlayerBullet") || rb == null) return false;
        Vector2 delta = posicion - rb.position;
        float rapidez = rb.linearVelocity.magnitude;
        if (rapidez < 0.1f || delta.sqrMagnitude > 25f) return false;
        float adelante = Vector2.Dot(delta, rb.linearVelocity / rapidez);
        return adelante > 0f && adelante < rapidez * 0.5f &&
            Mathf.Abs(delta.x * rb.linearVelocity.y - delta.y * rb.linearVelocity.x) / rapidez < 0.8f;
    }
    public bool EsReflejable => isActiveAndEnabled && gameObject.layer == LayerMask.NameToLayer("EnemyBullet");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
    }

    public void Disparar(Vector2 direccion)
    {
        rb.linearVelocity = direccion.normalized * velocidad;
        Orientar(direccion);
        expira = Time.time + Mathf.Max(0.1f, tiempoVida);
    }

    private void Update()
    {
        if (expira > 0f && Time.time >= expira) Destroy(gameObject);
    }

    public void Reflejar()
    {
        if (!EsReflejable) return;
        expira = Time.time + Mathf.Max(0.1f, tiempoVida);
        Vector2 direccion = -rb.linearVelocity;

        GameObject jefe = GameObject.FindGameObjectWithTag("Boss");
        Collider2D colisionJefe = jefe != null ? jefe.GetComponent<Collider2D>() : null;
        if (colisionJefe != null && colisionJefe.enabled)
        {
            direccion = colisionJefe.bounds.center - transform.position;
        }
        else
        {
            float distanciaMenor = float.PositiveInfinity;
            foreach (EnemigoMenor enemigo in FindObjectsByType<EnemigoMenor>(FindObjectsSortMode.None))
            {
                if (enemigo.gameObject.scene != gameObject.scene || enemigo.Vida == null || enemigo.Vida.VidaActual <= 0) continue;
                float distancia = (enemigo.transform.position - transform.position).sqrMagnitude;
                if (distancia >= distanciaMenor) continue;
                distanciaMenor = distancia;
                direccion = enemigo.transform.position - transform.position;
            }
        }

        float rapidez = rb.linearVelocity.magnitude * multiplicadorVelocidad;
        rb.linearVelocity = direccion.normalized * rapidez;

        danio = danioReflejado;
        gameObject.layer = LayerMask.NameToLayer("PlayerBullet");
        gameObject.tag = "Untagged";
        if (spriteReflejado != null)
        {
            sprite.sprite = spriteReflejado;
            orientacionSpriteGrados = 0f;
        }
        sprite.color = colorReflejada;
        Orientar(direccion);
    }

    private void Orientar(Vector2 direccion)
    {
        if (direccion.sqrMagnitude < 0.001f) return;
        float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angulo - orientacionSpriteGrados);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        int capaEnemiga = LayerMask.NameToLayer("EnemyBullet");
        int capaJugador = LayerMask.NameToLayer("PlayerBullet");
        if (gameObject.layer == capaEnemiga &&
            other.TryGetComponent(out PlayerParry parry) && parry.IntentarParry(this))
        {
            return;
        }

        if (other.TryGetComponent(out Health vida))
        {
            if (gameObject.layer == capaEnemiga && other.gameObject.layer != LayerMask.NameToLayer("Player")) return;
            if (gameObject.layer == capaJugador && other.gameObject.layer == LayerMask.NameToLayer("Player")) return;
            bool recibioDanio = gameObject.layer == capaJugador &&
                other.TryGetComponent(out PresentacionRenato renato) && renato.isActiveAndEnabled &&
                other.TryGetComponent(out BossController jefe) && jefe.isActiveAndEnabled
                ? jefe.RecibirDisparoRenato(danio)
                : vida.RecibirDanio(danio);
            if (recibioDanio) Destroy(gameObject);
            return;
        }

        if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
    }
}
