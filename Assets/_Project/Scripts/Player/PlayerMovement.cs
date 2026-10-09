using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float velocidad = 6f;

    [Header("Salto")]
    [SerializeField] private float fuerzaSalto = 10f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float radioSuelo = 0.2f;
    [SerializeField] private LayerMask capaSuelo;

    [Header("Dash")]
    [SerializeField] private float velocidadDash = 18f;
    [SerializeField] private float duracionDash = 0.15f;
    [SerializeField] private float recargaDash = 1f;

    private Rigidbody2D rb;
    private Health vida;
    private SpriteRenderer sprite;

    private float inputHorizontal;
    private float direccionMirada = 1f;
    private bool enSuelo;
    private bool haciendoDash;
    private bool dashDisponible = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        vida = GetComponent<Health>();
        sprite = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        inputHorizontal = Input.GetAxisRaw("Horizontal");
        if (inputHorizontal != 0f) direccionMirada = Mathf.Sign(inputHorizontal);

        if (haciendoDash) return;

        // Salto con la tecla W (también puedes agregar flecha arriba si quieres)
        if (Input.GetKeyDown(KeyCode.W) && enSuelo)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaSalto);
        }

        // Dash con la barra espaciadora
        if (Input.GetKeyDown(KeyCode.Space) && dashDisponible)
        {
            StartCoroutine(Dash());
        }
    }

    private void FixedUpdate()
    {
        enSuelo = Physics2D.OverlapCircle(groundCheck.position, radioSuelo, capaSuelo);

        if (haciendoDash)
        {
            rb.linearVelocity = new Vector2(direccionMirada * velocidadDash, 0f);
            return;
        }

        rb.linearVelocity = new Vector2(inputHorizontal * velocidad, rb.linearVelocity.y);
    }

    private IEnumerator Dash()
    {
        dashDisponible = false;
        haciendoDash = true;

        float gravedadOriginal = rb.gravityScale;
        rb.gravityScale = 0f;
        vida.Invulnerable = true;
        sprite.color = new Color(sprite.color.r, sprite.color.g, sprite.color.b, 0.5f);

        yield return new WaitForSeconds(duracionDash);

        rb.gravityScale = gravedadOriginal;
        vida.Invulnerable = false;
        sprite.color = new Color(sprite.color.r, sprite.color.g, sprite.color.b, 1f);
        haciendoDash = false;

        yield return new WaitForSeconds(recargaDash);
        dashDisponible = true;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, radioSuelo);
    }
}