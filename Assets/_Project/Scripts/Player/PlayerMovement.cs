using System.Collections;
using UnityEngine;

// Controles:
//   A / D o flechas      -> moverse
//   W o flecha arriba    -> saltar (una segunda pulsación en el aire = doble salto)
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

    [Header("Agacharse")]
    [Tooltip("Altura del collider al agacharse, en proporción a la altura de pie.")]
    [SerializeField, Range(0.3f, 1f)] private float proporcionAlturaAgachado = 0.6f;

    [Header("Apuntado con mouse")]
    [Tooltip("Distancia mínima del mouse al personaje para cambiar la dirección de apuntado.")]
    [SerializeField, Min(0f)] private float zonaMuertaMouse = 0.35f;

    [Header("Dash")]
    [SerializeField] private float velocidadDash = 18f;
    [SerializeField] private float duracionDash = 0.15f;
    [SerializeField] private float recargaDash = 1f;

    private Rigidbody2D rb;
    private Health vida;
    private SpriteRenderer sprite;
    private BoxCollider2D caja;
    private Vector2 tamanoCajaDePie;
    private Vector2 offsetCajaDePie;
    private Camera camara;

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
        vida = GetComponent<Health>();
        sprite = GetComponent<SpriteRenderer>();
        caja = GetComponent<BoxCollider2D>();
        if (caja != null)
        {
            tamanoCajaDePie = caja.size;
            offsetCajaDePie = caja.offset;
        }
        saltosAereosRestantes = saltosAereos;
    }

    private void OnDisable()
    {
        // Las cinemáticas y el fin del combate desactivan este script: el personaje debe quedar de pie.
        CambiarAgachado(false);
        ApuntadoFijo = false;
        inputHorizontal = 0f;
    }

    private void Update()
    {
        if (SesionJuego.EnPausa || Time.timeScale == 0f) return;

        float horizontal = Input.GetAxisRaw("Horizontal");
        bool abajo = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        ApuntadoFijo = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        CambiarAgachado(abajo && enSuelo && !haciendoDash);
        inputHorizontal = ApuntadoFijo || Agachado ? 0f : horizontal;

        ActualizarApuntado();

        if (haciendoDash || Time.time < empujeHasta) return;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) Saltar();

        // Espacio inicia el dash. Se ignora justo después de pulsar un botón del menú de pausa.
        if (!SesionJuego.SoltarEntradaDeMenu && Input.GetKeyDown(KeyCode.Space) && dashDisponible)
        {
            CambiarAgachado(false);
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
        if (enSuelo)
        {
            CambiarAgachado(false);
            AplicarSalto(fuerzaSalto);
            return;
        }

        if (saltosAereosRestantes <= 0) return;
        saltosAereosRestantes--;
        AplicarSalto(fuerzaSalto * multiplicadorSaltoAereo);
    }

    private void AplicarSalto(float fuerza)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerza);
        InstanteSalto = Time.time;
    }

    private void CambiarAgachado(bool agachar)
    {
        if (Agachado == agachar) return;
        Agachado = agachar;
        if (caja == null) return;

        if (agachar)
        {
            // Se reduce la altura del collider manteniendo los pies en el mismo lugar.
            float alto = tamanoCajaDePie.y * proporcionAlturaAgachado;
            caja.size = new Vector2(tamanoCajaDePie.x, alto);
            caja.offset = new Vector2(offsetCajaDePie.x, offsetCajaDePie.y - (tamanoCajaDePie.y - alto) * 0.5f);
        }
        else
        {
            caja.size = tamanoCajaDePie;
            caja.offset = offsetCajaDePie;
        }
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
