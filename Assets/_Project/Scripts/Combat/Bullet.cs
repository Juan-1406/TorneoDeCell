using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float velocidad = 15f;
    [SerializeField] private float tiempoVida = 3f;
    [SerializeField] private int danio = 10;

    [Header("Al ser reflejada (parry)")]
    [SerializeField] private int danioReflejado = 25;
    [SerializeField] private float multiplicadorVelocidad = 1.5f;
    [SerializeField] private Color colorReflejada = Color.cyan;

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
        Destroy(gameObject, tiempoVida);
    }

    public void Reflejar()
    {
        rb.linearVelocity = -rb.linearVelocity * multiplicadorVelocidad;
        danio = danioReflejado;
        gameObject.layer = LayerMask.NameToLayer("PlayerBullet");
        gameObject.tag = "Untagged";
        sprite.color = colorReflejada;
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