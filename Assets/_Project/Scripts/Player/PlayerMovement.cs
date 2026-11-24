using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Controles:
//   A / D o flechas      -> moverse
//   W o flecha arriba    -> saltar (otra pulsación en el aire = doble salto)
//   S o flecha abajo     -> agacharse (solo en el suelo)
//   Espacio              -> dash
//   Mouse                -> apuntar (se ajusta a 8 direcciones)
//   Shift                -> quedarse quieto mientras se apunta
[DefaultExecutionOrder(-10)]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float velocidad = 6f;

    [Header("Salto")]
    [SerializeField] private float fuerzaSalto = 10f;
    [Tooltip("Saltos adicionales permitidos en el aire. 1 = doble salto.")]
    [SerializeField, Min(0)] private int saltosAereos = 1;
    [Tooltip("Fuerza del salto en el aire en relación al salto normal.")]
    [SerializeField, Range(0.3f, 1.5f)] private float multiplicadorSaltoAereo = 0.9f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float radioSuelo = 0.2f;
    [SerializeField] private LayerMask capaSuelo;

    [Header("Apuntado con mouse")]
    [Tooltip("Distancia mínima del mouse al personaje para cambiar la dirección de apuntado.")]
    [SerializeField, Min(0f)] private float zonaMuertaMouse = 0.35f;

    [Header("Dash")]
    [SerializeField] private float velocidadDash = 18f;
    [SerializeField] private float duracionDash = 0.15f;
    [SerializeField] private float recargaDash = 1f;

    internal const float AlturaAgachado = .72f;

    private Rigidbody2D rb;
    private BoxCollider2D cuerpo;
    private Vector2 tamanoDePie, centroDePie;
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
    private int saltosAereosRestantes;
    private Vector2 apuntado = Vector2.right;
    private Camera camara;

    public float DireccionMirada => direccionMirada;
    internal bool ApuntadoFijo { get; private set; }
    internal bool HaciendoDash => haciendoDash;
    internal bool Agachado { get; private set; }
    internal bool EnSueloVisual => enSuelo && rb.linearVelocity.y >= -0.05f && rb.linearVelocity.y <= 0.2f;
    internal float InstanteSalto { get; private set; } = float.NegativeInfinity;
    public Vector2 DireccionDisparo { get; private set; } = Vector2.right;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cuerpo = GetComponent<BoxCollider2D>();
        if (cuerpo != null)
        {
            tamanoDePie = cuerpo.size;
            centroDePie = cuerpo.offset;
        }
        vida = GetComponent<Health>();
        sprite = GetComponent<SpriteRenderer>();
        saltosAereosRestantes = saltosAereos;
    }

    private void Update()
    {
        if (SesionJuego.EnPausa || Time.timeScale == 0f) return;
        bool ocupado = haciendoDash || Time.time < empujeHasta;
        bool abajo = Keyboard.current != null &&
            (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed);
        CambiarPostura(!ocupado && EnSueloVisual && abajo);
        float horizontal = Input.GetAxisRaw("Horizontal");
        ApuntadoFijo = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        inputHorizontal = ApuntadoFijo ? 0f : horizontal;

        ActualizarApuntado();

        if (ocupado) return;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) Saltar();

        // Espacio inicia el dash. Se ignora justo después de pulsar un botón del menú de pausa.
        if (!SesionJuego.SoltarEntradaDeMenu && Input.GetKeyDown(KeyCode.Space) && dashDisponible)
        {
            // El dash sigue la tecla de movimiento pulsada; sin tecla, va hacia donde mira el personaje.
            direccionDash = horizontal != 0f ? Mathf.Sign(horizontal) : direccionMirada;
            StartCoroutine(Dash());
        }
    }

    // Convierte la posición del mouse en una de las 8 direcciones (cada 45 grados).
    private void ActualizarApuntado()
    {
        if (camara == null) camara = Camera.main;
        if (camara != null)
        {
            Vector2 mouse = camara.ScreenToWorldPoint(Input.mousePosition);
            Vector2 centro = sprite != null ? (Vector2)sprite.bounds.center : (Vector2)transform.position;
            Vector2 delta = mouse - centro;
            if (delta.sqrMagnitude >= zonaMuertaMouse * zonaMuertaMouse)
            {
                float angulo = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                float ajustado = Mathf.Round(angulo / 45f) * 45f * Mathf.Deg2Rad;
                apuntado = new Vector2(Mathf.Round(Mathf.Cos(ajustado)), Mathf.Round(Mathf.Sin(ajustado)));
            }
        }

        // El personaje mira hacia el lado del mouse; apuntando recto arriba o abajo conserva su lado.
        if (apuntado.x != 0f) direccionMirada = Mathf.Sign(apuntado.x);

        // Agachado solo dispara de frente.
        DireccionDisparo = Agachado ? new Vector2(direccionMirada, 0f) : apuntado;
    }

    private void Saltar()
    {
        float fuerza;
        if (enSuelo) fuerza = fuerzaSalto;
        else if (saltosAereosRestantes > 0)
        {
            saltosAereosRestantes--;
            fuerza = fuerzaSalto * multiplicadorSaltoAereo;
        }
        else return;

        CambiarPostura(false);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerza);
        InstanteSalto = Time.time;
    }

    private void FixedUpdate()
    {
        Collider2D suelo = Physics2D.OverlapCircle(groundCheck.position, radioSuelo, capaSuelo);
        enSuelo = suelo != null;
        if (enSuelo && suelo.usedByEffector && suelo.TryGetComponent<ParlanteFlotante>(out _))
            enSuelo = rb.linearVelocity.y <= 0.05f && groundCheck.position.y >= suelo.bounds.max.y - 0.05f;

        // Al tocar el suelo (sin estar subiendo) se recupera el doble salto.
        if (enSuelo && rb.linearVelocity.y <= 0.05f) saltosAereosRestantes = saltosAereos;

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

        rb.linearVelocity = new Vector2(Agachado ? 0f : inputHorizontal * velocidad, rb.linearVelocity.y);
    }

    public void RecibirEmpuje(Vector2 impulso)
    {
        if (impulso == Vector2.zero) return;
        CambiarPostura(false);
        rb.linearVelocity = impulso;
        velocidadEmpujeX = impulso.x;
        empujeHasta = Time.time + 0.22f;
    }

    private IEnumerator Dash()
    {
        dashDisponible = false;
        haciendoDash = true;
        CambiarPostura(false);

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

    private void CambiarPostura(bool agachado)
    {
        if (cuerpo == null || agachado == Agachado) return;

        Agachado = agachado;
        float altura = agachado ? tamanoDePie.y * AlturaAgachado : tamanoDePie.y;
        cuerpo.size = new Vector2(tamanoDePie.x, altura);
        // Baja el centro junto con la parte superior para conservar el contacto de los pies.
        cuerpo.offset = centroDePie + Vector2.up * ((altura - tamanoDePie.y) * .5f);
    }

    private void OnDisable() => CambiarPostura(false);

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, radioSuelo);
    }
}
