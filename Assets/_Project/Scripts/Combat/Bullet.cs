using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float velocidad = 15f;
    [SerializeField] private float tiempoVida = 3f;
    [SerializeField] private int danio = 10;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Disparar(Vector2 direccion)
    {
        rb.linearVelocity = direccion.normalized * velocidad;
        Destroy(gameObject, tiempoVida);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out Health vida))
        {
            vida.RecibirDanio(danio);
            Destroy(gameObject);
            return;
        }

        if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
    }
}