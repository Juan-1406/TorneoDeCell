using UnityEngine;

[DisallowMultipleComponent]
public sealed class MateoSpriteAnimator : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField] private Sprite reposo;
    [SerializeField] private Sprite[] carrera;
    [SerializeField] private Sprite[] poses;
    [SerializeField] private Sprite[] destellos;
    [SerializeField] private Sprite[] salto;
    [SerializeField] private Sprite[] disparoAereo;
    [SerializeField, Min(1f)] private float runFramesPerSecond = 8f;
    [SerializeField, Min(0f)] private float moveThreshold = 0.1f;
    [SerializeField, Min(0f)] private float dashThreshold = 12f;

    private const float CuadrosDestelloPorSegundo = 30f;
    // Punta del cañón respecto al pivote de cada pose, en píxeles de la hoja.
    private static readonly Vector2[] Puntas =
    {
        new Vector2(143f, 53f),
        new Vector2(104f, 130f),
        new Vector2(49f, 188f),
        new Vector2(111f, -40f),
        new Vector2(4f, -146f)
    };

    private SpriteRenderer spriteRenderer;
    private SpriteRenderer destello;
    private SpriteRenderer respiracion;
    private float inicioReposo;
    private Rigidbody2D body;
    private PlayerMovement movimiento;
    private float inicioDestello;
    private Sprite poseDisparo;
    private PlayerParry parry;
    private bool estabaEnSuelo = true;
    private float aterrizajeHasta;
    private float ultimoSaltoVisto = float.NegativeInfinity;
    private bool saltoDesdeSuelo;
    private bool TieneSalto => movimiento != null && salto != null && salto.Length == 5;
    private bool PrioridadDefensiva => (movimiento != null && movimiento.HaciendoDash) ||
        (parry != null && parry.EscudoActivo);
    private float ladoDisparo;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        body = GetComponent<Rigidbody2D>();
        movimiento = GetComponent<PlayerMovement>();
        parry = GetComponent<PlayerParry>();
    }

    private static int IndicePose(Vector2 direccion)
    {
        if (direccion.y > 0f) return direccion.x == 0f ? 2 : 1;
        if (direccion.y < 0f) return direccion.x == 0f ? 4 : 3;
        return 0;
    }

    private int FaseSalto(float vertical)
    {
        if (Time.time - movimiento.InstanteSalto < 0.055f) return 0;
        if (vertical > 1f) return 1;
        if (!saltoDesdeSuelo) return 3;
        return vertical >= -1f ? 2 : 3;
    }

    private bool Agachado => TieneSalto && movimiento.Agachado;

    private Sprite SeleccionarPose(Vector2 direccion, out Vector2 punta)
    {
        // Agachado se reutiliza el cuadro de aterrizaje (rodillas flexionadas) y se dispara de frente.
        if (Agachado)
        {
            punta = PuntaAgachado;
            return salto[4];
        }

        int indice = IndicePose(direccion);
        punta = Puntas[indice];
        if (TieneSalto && !movimiento.EnSueloVisual && disparoAereo != null && disparoAereo.Length == 3)
        {
            if (Time.time - movimiento.InstanteSalto < 0.055f || (parry != null && parry.EscudoActivo))
            {
                int fase = FaseSalto(body.linearVelocity.y);
                punta = PuntasSalto[fase];
                return salto[fase];
            }

            if (indice <= 2)
            {
                punta = PuntasAire[indice];
                return disparoAereo[indice];
            }
        }

        if (TieneSalto && movimiento.EnSueloVisual && (!estabaEnSuelo || Time.time < aterrizajeHasta))
        {
            punta = PuntaAgachado;
            return salto[4];
        }

        return poses != null && indice < poses.Length ? poses[indice] : null;
    }

    private static readonly Vector2 PuntaAgachado = new Vector2(144f, 21f);

    private static readonly Vector2[] PuntasAire =
    {
        new Vector2(126f, 57f),
        new Vector2(104f, 145f),
        new Vector2(58f, 219f)
    };

    private static readonly Vector2[] PuntasSalto =
    {
        new Vector2(142f, 0f),
        new Vector2(131f, 49f),
        new Vector2(125f, 54f),
        new Vector2(134f, 57f)
    };

    internal Vector3 PuntaCanon(Vector2 direccion, float lado)
    {
        Sprite pose = SeleccionarPose(direccion, out Vector2 punta);
        if (pose == null) return transform.position;
        punta /= pose.pixelsPerUnit;
        punta.x *= lado;
        return transform.TransformPoint(punta);
    }

    internal void MostrarDisparo(Vector3 origen, Vector2 direccion, float lado)
    {
        if (spriteRenderer == null || destellos == null || destellos.Length == 0) return;
        if (destello == null)
        {
            destello = new GameObject("Destello de Mateo").AddComponent<SpriteRenderer>();
            destello.transform.SetParent(transform, false);
            destello.transform.localScale = Vector3.one * 0.45f;
            destello.sortingLayerID = spriteRenderer.sortingLayerID;
            destello.sortingOrder = spriteRenderer.sortingOrder + 1;
        }

        inicioDestello = Time.time;
        poseDisparo = SeleccionarPose(direccion, out _);
        ladoDisparo = lado;
        destello.transform.SetPositionAndRotation(origen,
            Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.right, direccion)));
        destello.sprite = destellos[0];
        destello.enabled = true;
    }

    private void OnDisable()
    {
        if (destello != null) destello.enabled = false;
        MostrarReposo(false);
    }

    private void LateUpdate()
    {
        if (spriteRenderer == null || frames == null || frames.Length == 0) return;

        float horizontalSpeed = body != null ? body.linearVelocity.x : 0f;
        float verticalSpeed = body != null ? body.linearVelocity.y : 0f;
        float lado = movimiento != null ? movimiento.DireccionMirada :
            Mathf.Abs(horizontalSpeed) > moveThreshold ? Mathf.Sign(horizontalSpeed) : spriteRenderer.flipX ? -1f : 1f;
        Vector2 direccion = movimiento != null ? movimiento.DireccionDisparo : Vector2.right;
        bool dash = Mathf.Abs(horizontalSpeed) >= dashThreshold || (movimiento != null && movimiento.HaciendoDash);
        bool enSuelo = !TieneSalto || movimiento.EnSueloVisual;
        bool impulso = TieneSalto && Time.time - movimiento.InstanteSalto < 0.055f;
        if (TieneSalto && movimiento.InstanteSalto != ultimoSaltoVisto)
        {
            ultimoSaltoVisto = movimiento.InstanteSalto;
            saltoDesdeSuelo = true;
        }
        if (enSuelo && !impulso) saltoDesdeSuelo = false;
        if (TieneSalto && enSuelo && !estabaEnSuelo && !dash)
            aterrizajeHasta = Time.time + 0.095f;
        estabaEnSuelo = enSuelo;
        if (!enSuelo || impulso) aterrizajeHasta = 0f;
        bool aterrizando = TieneSalto && Time.time < aterrizajeHasta;
        Sprite cuadro = frames[0];

        bool agachado = !dash && Agachado;

        if (dash)
            cuadro = frames[Mathf.Min(3, frames.Length - 1)];
        else if (agachado)
            cuadro = salto[4];
        else if (TieneSalto && (!enSuelo || impulso))
            cuadro = salto[FaseSalto(verticalSpeed)];
        else if (aterrizando)
            cuadro = salto[4];
        else if (verticalSpeed > 0.2f)
            cuadro = frames[Mathf.Min(3, frames.Length - 1)];
        else if (Mathf.Abs(horizontalSpeed) > moveThreshold && carrera != null && carrera.Length > 0)
            cuadro = carrera[Mathf.FloorToInt(Time.time * runFramesPerSecond) % carrera.Length];
        else if (Mathf.Abs(horizontalSpeed) > moveThreshold && frames.Length >= 3)
            cuadro = frames[1 + Mathf.FloorToInt(Time.time * runFramesPerSecond) % 2];

        bool mostrarPose = !dash && !PrioridadDefensiva && !impulso && !aterrizando &&
            (direccion.y != 0f || (movimiento != null && movimiento.ApuntadoFijo));
        if (mostrarPose) cuadro = SeleccionarPose(direccion, out _) ?? cuadro;
        if (destello != null && destello.enabled)
        {
            int cuadroDestello = Mathf.FloorToInt((Time.time - inicioDestello) * CuadrosDestelloPorSegundo);
            destello.enabled = cuadroDestello < destellos.Length;
            if (destello.enabled)
            {
                destello.sprite = destellos[cuadroDestello];
                if (!dash && !PrioridadDefensiva && !impulso && !aterrizando && poseDisparo != null)
                {
                    cuadro = poseDisparo;
                    lado = ladoDisparo;
                    mostrarPose = true;
                }
            }
        }

        spriteRenderer.sprite = cuadro;
        spriteRenderer.flipX = lado < 0f;
        MostrarReposo(reposo != null && enSuelo && !agachado && !aterrizando && !impulso && !mostrarPose && !dash &&
            Mathf.Abs(horizontalSpeed) <= moveThreshold && Mathf.Abs(verticalSpeed) <= 0.2f);
    }

    private void MostrarReposo(bool activo)
    {
        if (!activo)
        {
            if (respiracion != null) respiracion.enabled = false;
            if (spriteRenderer != null) spriteRenderer.forceRenderingOff = false;
            return;
        }

        if (respiracion == null)
        {
            respiracion = new GameObject("Respiración de Mateo").AddComponent<SpriteRenderer>();
            respiracion.transform.SetParent(transform, false);
            respiracion.sharedMaterial = spriteRenderer.sharedMaterial;
            respiracion.sortingLayerID = spriteRenderer.sortingLayerID;
            respiracion.sortingOrder = spriteRenderer.sortingOrder;
            respiracion.sprite = reposo;
            respiracion.enabled = false;
        }

        if (!respiracion.enabled) inicioReposo = Time.time;
        float altura = 1f + 0.012f * (1f - Mathf.Cos((Time.time - inicioReposo) * 3.5f));
        respiracion.transform.localScale = new Vector3(1f, altura, 1f);
        respiracion.transform.localPosition = Vector3.up * (reposo.bounds.min.y * (1f - altura));
        respiracion.flipX = spriteRenderer.flipX;
        respiracion.color = spriteRenderer.color;
        respiracion.enabled = spriteRenderer.enabled;
        spriteRenderer.forceRenderingOff = true;
    }
}
