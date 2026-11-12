using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-10)]
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
    private float direccionDash;
    private bool dashDisponible = true;
    private float empujeHasta;
    private float velocidadEmpujeX;

    public float DireccionMirada => direccionMirada;
    internal bool ApuntadoFijo { get; private set; }
    internal bool HaciendoDash => haciendoDash;
    internal bool EnSueloVisual => enSuelo && rb.linearVelocity.y >= -0.05f && rb.linearVelocity.y <= 0.2f;
    internal float InstanteSalto { get; private set; } = float.NegativeInfinity;
    public Vector2 DireccionDisparo { get; private set; } = Vector2.right;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        vida = GetComponent<Health>();
        sprite = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (SesionJuego.EnPausa || Time.timeScale == 0f) return;
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = ((Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) ? 1f : 0f) -
            (Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);
        ApuntadoFijo = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        inputHorizontal = ApuntadoFijo ? 0f : horizontal;
        float lado = Mathf.Sign(horizontal);
        if (horizontal != 0f) direccionMirada = lado;
        DireccionDisparo = vertical == 0f
            ? new Vector2(direccionMirada, 0f)
            : new Vector2(horizontal == 0f ? 0f : lado, vertical);

        if (haciendoDash || Time.time < empujeHasta) return;

        // Las teclas de apuntado vertical quedan disponibles durante el salto.
        if (Input.GetKeyDown(KeyCode.Space)) Saltar();

        // Una pulsación del botón derecho inicia el dash.
        if (Input.GetMouseButtonDown(1) && dashDisponible)
        {
            StartCoroutine(Dash());
        }
    }

    private void Saltar()
    {
        if (!enSuelo) return;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaSalto);
        InstanteSalto = Time.time;
    }

    private void FixedUpdate()
    {
        Collider2D suelo = Physics2D.OverlapCircle(groundCheck.position, radioSuelo, capaSuelo);
        enSuelo = suelo != null;
        if (enSuelo && suelo.usedByEffector && suelo.TryGetComponent<ParlanteFlotante>(out _))
            enSuelo = rb.linearVelocity.y <= 0.05f && groundCheck.position.y >= suelo.bounds.max.y - 0.05f;

        if (haciendoDash)
        {
            rb.linearVelocity = new Vector2(direccionDash * velocidadDash, 0f);
            return;
        }

        if (Time.time < empujeHasta)
        {
            rb.linearVelocity = new Vector2(velocidadEmpujeX, rb.linearVelocity.y);
            return;
        }

        rb.linearVelocity = new Vector2(inputHorizontal * velocidad, rb.linearVelocity.y);
    }

    public void RecibirEmpuje(Vector2 impulso)
    {
        if (impulso == Vector2.zero) return;
        rb.linearVelocity = impulso;
        velocidadEmpujeX = impulso.x;
        empujeHasta = Time.time + 0.22f;
    }

    private IEnumerator Dash()
    {
        dashDisponible = false;
        haciendoDash = true;
        direccionDash = direccionMirada;

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
