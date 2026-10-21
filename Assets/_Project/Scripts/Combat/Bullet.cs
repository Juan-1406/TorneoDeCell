using UnityEngine;

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

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
    }

    public void Disparar(Vector2 direccion)
    {
        rb.linearVelocity = direccion.normalized * velocidad;
        Orientar(direccion);
        Destroy(gameObject, tiempoVida);
    }

    public void Reflejar()
    {
        Vector2 direccion = -rb.linearVelocity;

        GameObject jefe = GameObject.FindGameObjectWithTag("Boss");
        if (jefe != null)
        {
            direccion = jefe.transform.position - transform.position;
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
        if (other.TryGetComponent(out PlayerParry parry) && parry.IntentarParry(this))
        {
            return;
        }

        if (other.TryGetComponent(out Health vida))
        {
            if (vida.RecibirDanio(danio)) Destroy(gameObject);
            return;
        }

        if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
    }
}
