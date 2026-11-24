using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health), typeof(SpriteRenderer))]
public sealed class PadrePasta : MonoBehaviour
{
    [Header("Poses")]
    [SerializeField] private Sprite reposo, carga, disparo, avisoHorizontal, avisoVertical, danio;
    [SerializeField] private SpriteRenderer escudoVisual;

    [Header("Escudo")]
    [SerializeField, Min(1)] private int disparosParaEscudo = 4;
    [SerializeField, Min(0.1f)] private float ventanaDisparos = 1f;
    [SerializeField, Min(0.1f)] private float duracionEscudo = 2.5f;
    [SerializeField, Range(0f, 0.95f)] private float reduccionDanioEscudo = 0.65f;
    [SerializeField, Min(0f)] private float recargaEscudo = 5f;

    [Header("Ataques")]
    [SerializeField] private Bullet notaPrefab;
    [SerializeField] private Sprite notaCeleste, notaNaranja;
    [Tooltip("Sprite de la nota que se puede devolver con parry. Si se deja vacío, se pinta la celeste de rosado.")]
    [SerializeField] private Sprite notaRosada;
    [SerializeField] private Transform bocaMicrofono;
    [SerializeField] private GameObject senalesHorizontales, senalesVerticales;
    [SerializeField] private SpriteRenderer parlanteHorizontal, parlanteVertical;
    [SerializeField] private Sprite flechaFrontal;
    [SerializeField] private Vector2 limitesMuelle = new Vector2(-9.75f, 4.65f);
    [SerializeField] private float alturaMuelle = -4.15f;
    [SerializeField, Range(0.1f, 0.9f)] private float umbralContratiempo = 0.5f;
    [SerializeField, Min(0.25f)] private float descanso = 1.1f, descansoFinal = 0.7f;

    [Header("Micrófono")]
    [SerializeField, Min(0.6f)] private float avisoMicrofono = 0.9f;
    [SerializeField, Min(0.12f)] private float intervaloRafaga = 0.24f;
    [SerializeField, Range(1, 6)] private int notasPorRafaga = 3;
    [SerializeField, Min(4f)] private float velocidadFrontal = 10f;
    [SerializeField, Min(1)] private int danioFrontal = 1;
    [Tooltip("Ángulo entre las notas del abanico. La del centro va directo a Mateo y es la rosada.")]
    [SerializeField, Range(8f, 35f)] private float aperturaAbanico = 20f;

    [Header("Parlante suspendido")]
    [SerializeField, Min(0.7f)] private float avisoCaida = 1.1f;
    [SerializeField, Min(0.2f)] private float duracionCaida = 0.42f;
    [SerializeField, Min(1)] private int danioCaida = 1;
    [Tooltip("Al aterrizar, el parlante suelta una nota por el suelo hacia cada lado.")]
    [SerializeField] private bool ondasAlAterrizar = true;
    [SerializeField, Min(1f)] private float velocidadOndas = 6f;
    [SerializeField, Range(0.5f, 2f)] private float escalaOndas = 1.2f;

    [Header("Parlante del borde")]
    [SerializeField, Min(0.7f)] private float avisoBorde = 1.05f;
    [SerializeField, Min(4f)] private float velocidadBorde = 8f;
    [SerializeField, Min(1)] private int danioBorde = 1;
    [Tooltip("Alterna notas bajas (se saltan) y altas (se esquivan agachado).")]
    [SerializeField] private bool alternarNotaAlta = true;

    private readonly EscudoPorDisparos escudo = new EscudoPorDisparos();
    private static readonly Color ParlanteEnEspera = new Color(0.75f, 0.75f, 0.8f);
    private static readonly Color RosadoParry = new Color(1f, 0.32f, 0.72f);
    private Health vida;
    private Health jugador;
    private Collider2D cuerpoJugador;
    private SpriteRenderer cuerpo;
    private Transform efectos;
    private Camera camara;
    private float semialtoArena;
    private Sprite pose;
    private float reaccionHasta;
    private Vector3 posicionParlanteVertical;
    private Vector3 posicionParlanteHorizontal;
    private LineRenderer cable;
    private PlayerMovement movimientoJugador;
    private int notasDelBorde;
    private int rafagas;

    private void Awake()
    {
        vida = GetComponent<Health>();
        cuerpo = GetComponent<SpriteRenderer>();
        pose = reposo;
        posicionParlanteVertical = parlanteVertical.transform.localPosition;
        posicionParlanteHorizontal = parlanteHorizontal.transform.localPosition;
    }

    private void OnEnable() => vida.OnDanioRecibido += Reaccionar;

    private void Start()
    {
        GameObject mateo = GameObject.FindGameObjectWithTag("Player");
        if (mateo == null || !mateo.TryGetComponent(out jugador) || !mateo.TryGetComponent(out cuerpoJugador))
            return;
        movimientoJugador = mateo.GetComponent<PlayerMovement>();

        efectos = new GameObject("Ataques de Padre Pasta").transform;
        efectos.SetParent(transform, false);
        cable = new GameObject("Cable del parlante").AddComponent<LineRenderer>();
        cable.transform.SetParent(efectos, false);
        cable.sharedMaterial = cuerpo.sharedMaterial;
        cable.sortingOrder = 10;
        cable.positionCount = 2;
        cable.widthMultiplier = 0.065f;
        cable.startColor = cable.endColor = new Color32(38, 24, 54, 255);
        cable.enabled = false;
        camara = Camera.main;
        if (camara != null) semialtoArena = camara.orthographicSize;
        escudoVisual.sprite = FeedbackCombate.Aro;
        MostrarAviso(senalesHorizontales, parlanteHorizontal, false);
        MostrarAviso(senalesVerticales, parlanteVertical, false);
        parlanteVertical.enabled = false;
        parlanteHorizontal.transform.position = new Vector2(limitesMuelle.x + 0.43f, alturaMuelle);
        StartCoroutine(Combate());
    }

    private void LateUpdate()
    {
        cuerpo.sprite = pose == reposo && Time.time < reaccionHasta ? danio : pose;
        escudoVisual.enabled = escudo.Activo;
        if (camara != null)
            camara.orthographicSize = Mathf.Max(semialtoArena, semialtoArena * (16f / 9f) / camara.aspect);
    }

    private void OnDisable()
    {
        vida.OnDanioRecibido -= Reaccionar;
        StopAllCoroutines();
        escudo.Reiniciar();
        escudoVisual.enabled = false;
        cuerpo.sprite = vida.VidaActual == 0 ? danio : reposo;
        MostrarAviso(senalesHorizontales, parlanteHorizontal, false);
        MostrarAviso(senalesVerticales, parlanteVertical, false);
        parlanteVertical.enabled = false;
        parlanteVertical.transform.localPosition = posicionParlanteVertical;
        parlanteHorizontal.transform.localPosition = posicionParlanteHorizontal;
        if (efectos != null)
        {
            efectos.gameObject.SetActive(false);
            Destroy(efectos.gameObject);
        }
    }

    internal bool RecibirDisparo(int cantidad)
    {
        if (cantidad <= 0 || vida.VidaActual <= 0 || vida.EsInvulnerable) return false;
        escudo.Registrar(disparosParaEscudo, ventanaDisparos, duracionEscudo, recargaEscudo);
        return vida.RecibirDanio(escudo.AjustarDanio(cantidad, reduccionDanioEscudo));
    }

    private void Reaccionar() => reaccionHasta = Time.time + 0.11f;

    private IEnumerator Combate()
    {
        yield return new WaitForSeconds(1.2f);
        yield return PresentarPatrones();

        while (true)
        {
            if (vida.VidaActual > vida.VidaMaxima * umbralContratiempo)
            {
                yield return PresentarPatrones();
                continue;
            }

            Coroutine caida = StartCoroutine(CaidaParlante());
            yield return RafagaMicrofono(alta: true);
            yield return caida;
            yield return new WaitForSeconds(descansoFinal);
            yield return NotaDelBorde();
            yield return new WaitForSeconds(descansoFinal);
        }
    }

    private IEnumerator PresentarPatrones()
    {
        yield return RafagaMicrofono(alta: false);
        yield return new WaitForSeconds(descanso);
        pose = avisoVertical;
        yield return CaidaParlante();
        pose = reposo;
        yield return new WaitForSeconds(descanso);
        yield return NotaDelBorde();
        yield return new WaitForSeconds(descanso);
    }

    // Alterna dos versiones del micrófono:
    //  - Línea: notas celestes seguidas, ninguna se puede devolver (se esquivan).
    //  - Abanico: 3 notas a la vez; la del centro va directo a Mateo y es la rosada (se devuelve con parry),
    //    las de los lados pasan por arriba y por abajo.
    // Con menos de la mitad de vida (alta = true) siempre es la línea horizontal.
    private IEnumerator RafagaMicrofono(bool alta)
    {
        Vector2 origen = bocaMicrofono.position;
        Vector2 blanco = cuerpoJugador.bounds.center;
        bool abanico = !alta && rafagas++ % 2 == 1;
        Vector2 rumbo = alta ? Vector2.left : abanico
            ? (blanco - origen).normalized
            : new Vector2(Mathf.Min(-0.8f, blanco.x - origen.x), Mathf.Min(0f, blanco.y - origen.y)).normalized;
        if (rumbo.x > -0.2f) rumbo = new Vector2(-0.2f, rumbo.y).normalized;

        Vector2[] rumbos = abanico
            ? new[] { Rotar(rumbo, aperturaAbanico), rumbo, Rotar(rumbo, -aperturaAbanico) }
            : new[] { rumbo };

        pose = carga;
        GameObject aviso = new GameObject("Trayectoria del micrófono");
        aviso.transform.SetParent(efectos, false);
        foreach (Vector2 r in rumbos)
            DibujarFlechas(aviso.transform, origen, r, Alcance(origen, r), abanico && r == rumbo);
        yield return new WaitForSeconds(avisoMicrofono);
        aviso.SetActive(false);
        Destroy(aviso);

        float duracion = 0f;
        if (abanico)
        {
            pose = disparo;
            foreach (Vector2 r in rumbos)
            {
                float viaje = Alcance(origen, r) / velocidadFrontal;
                duracion = Mathf.Max(duracion, viaje);
                Bullet nota = Lanzar(notaCeleste, origen, r, velocidadFrontal, danioFrontal, viaje);
                bool rosada = r == rumbo;
                nota.PermiteParry = rosada;
                if (rosada) PintarRosada(nota);
            }
            FeedbackCombate.Emitir(bocaMicrofono, RosadoParry, 5, 1.6f);
            yield return new WaitForSeconds(intervaloRafaga);
        }
        else
        {
            duracion = Alcance(origen, rumbo) / velocidadFrontal;
            for (int i = 0; i < notasPorRafaga; i++)
            {
                pose = disparo;
                Lanzar(notaCeleste, origen, rumbo, velocidadFrontal, danioFrontal, duracion).PermiteParry = false;
                FeedbackCombate.Emitir(bocaMicrofono, new Color32(88, 240, 255, 255), 3, 1.2f);
                yield return new WaitForSeconds(intervaloRafaga * 0.6f);
                pose = carga;
                yield return new WaitForSeconds(intervaloRafaga * 0.4f);
            }
        }

        pose = reposo;
        yield return new WaitForSeconds(duracion);
    }

    // Distancia que recorre una nota hasta salir por la izquierda del muelle o tocar el suelo.
    private float Alcance(Vector2 origen, Vector2 rumbo)
    {
        float horizontal = rumbo.x < -0.01f ? (origen.x - limitesMuelle.x + 1f) / -rumbo.x : 30f;
        float vertical = rumbo.y < -0.01f ? (alturaMuelle - origen.y) / rumbo.y : float.PositiveInfinity;
        return Mathf.Min(horizontal, vertical, 30f);
    }

    private void DibujarFlechas(Transform padre, Vector2 origen, Vector2 rumbo, float alcance, bool rosada)
    {
        for (float distancia = 0.8f; distancia < alcance; distancia += 1.35f)
        {
            var flecha = new GameObject("Flecha del micrófono").AddComponent<SpriteRenderer>();
            flecha.transform.SetParent(padre, false);
            flecha.transform.position = origen + rumbo * distancia;
            flecha.transform.rotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.left, rumbo));
            flecha.sprite = flechaFrontal;
            flecha.sharedMaterial = cuerpo.sharedMaterial;
            flecha.sortingOrder = 12;
            if (rosada) flecha.color = RosadoParry;
        }
    }

    private void PintarRosada(Bullet nota)
    {
        var imagen = nota.GetComponent<SpriteRenderer>();
        if (notaRosada != null) imagen.sprite = notaRosada;
        else imagen.color = RosadoParry;
    }

    private static Vector2 Rotar(Vector2 v, float grados) => Quaternion.Euler(0f, 0f, grados) * v;

    private IEnumerator CaidaParlante()
    {
        float x = Mathf.Clamp(cuerpoJugador.bounds.center.x, limitesMuelle.x + 1.1f, limitesMuelle.y - 1.1f);
        Vector3 origen = new Vector3(x, 3.22f, 0f);
        Transform soporte = parlanteVertical.transform;
        soporte.position = origen;
        parlanteVertical.enabled = true;
        senalesVerticales.transform.position = new Vector3(x, alturaMuelle, 0f);
        MostrarAviso(senalesVerticales, parlanteVertical, true);
        cable.enabled = true;
        ActualizarCable();
        yield return new WaitForSeconds(avisoCaida);

        bool golpeado = false;
        for (float t = 0f; t < duracionCaida;)
        {
            yield return new WaitForFixedUpdate();
            t += Time.fixedDeltaTime;
            float avance = Mathf.Clamp01(t / duracionCaida);
            Bounds recorrido = parlanteVertical.bounds;
            soporte.position = new Vector3(x, Mathf.Lerp(origen.y, alturaMuelle, avance * avance), 0f);
            ActualizarCable();
            recorrido.Encapsulate(parlanteVertical.bounds);
            recorrido.Expand(new Vector3(-0.24f, -0.08f, 0f));
            if (!golpeado && recorrido.Intersects(cuerpoJugador.bounds))
                golpeado = jugador.RecibirDanio(danioCaida);
        }

        FeedbackCombate.Emitir(soporte, new Color32(245, 91, 207, 255), 6, 2f);
        if (ondasAlAterrizar) LanzarOndas(x);
        MostrarAviso(senalesVerticales, parlanteVertical, false);
        for (float t = 0f; t < 0.46f;)
        {
            yield return null;
            t += Time.deltaTime;
            soporte.position = Vector3.Lerp(new Vector3(x, alturaMuelle, 0f), origen, Mathf.SmoothStep(0f, 1f, t / 0.46f));
            ActualizarCable();
        }
        parlanteVertical.enabled = false;
        cable.enabled = false;
    }

    // Dos notas pequeñas salen por el suelo desde donde cayó el parlante: hay que saltarlas.
    private void LanzarOndas(float x)
    {
        Vector2 origen = new Vector2(x, alturaMuelle + 0.3f);
        foreach (float lado in new[] { -1f, 1f })
        {
            float borde = lado < 0f ? limitesMuelle.x - 0.5f : limitesMuelle.y + 0.5f;
            float viaje = Mathf.Abs(borde - x) / velocidadOndas;
            if (viaje < 0.05f) continue;
            Bullet onda = Lanzar(notaNaranja, origen, new Vector2(lado, 0f), velocidadOndas, danioCaida, viaje);
            onda.PermiteParry = false;
            onda.transform.localScale = Vector3.one * escalaOndas;
            onda.GetComponent<SpriteRenderer>().flipX = lado > 0f;
        }
    }

    private void ActualizarCable()
    {
        Vector3 punta = parlanteVertical.bounds.center;
        punta.y = parlanteVertical.bounds.max.y - 0.06f;
        cable.SetPosition(0, new Vector3(punta.x, 5.16f, 0f));
        cable.SetPosition(1, punta);
    }

    private IEnumerator NotaDelBorde()
    {
        pose = avisoHorizontal;
        // Primero baja (se salta) y la siguiente alta (se esquiva agachado), y así sucesivamente.
        bool alta = alternarNotaAlta && notasDelBorde++ % 2 == 1;
        Vector2 origen = new Vector2(limitesMuelle.x + 0.85f, alta ? AlturaNotaAlta() : alturaMuelle + 0.36f);
        senalesHorizontales.transform.position = origen;
        MostrarAviso(senalesHorizontales, parlanteHorizontal, true);
        yield return new WaitForSeconds(avisoBorde);

        MostrarAviso(senalesHorizontales, parlanteHorizontal, false);
        pose = disparo;
        float viaje = (limitesMuelle.y - origen.x + 0.5f) / velocidadBorde;
        Bullet nota = Lanzar(notaNaranja, origen, Vector2.right, velocidadBorde, danioBorde, viaje);
        nota.PermiteParry = false;
        nota.transform.localScale = Vector3.one * (alta ? EscalaNotaAlta(nota) : 1.25f);
        nota.GetComponent<SpriteRenderer>().flipX = true;
        FeedbackCombate.Emitir(parlanteHorizontal, new Color32(255, 174, 74, 255), 4);
        yield return new WaitForSeconds(0.25f);
        pose = reposo;
        yield return new WaitForSeconds(Mathf.Max(0f, viaje - 0.25f));
    }

    // Altura de pie de Mateo, aunque en este momento esté agachado.
    private float AlturaDePie()
    {
        float alto = cuerpoJugador.bounds.size.y;
        return movimientoJugador != null && movimientoJugador.Agachado ? alto / PlayerMovement.AlturaAgachado : alto;
    }

    // Entre la cabeza de Mateo agachado y la cabeza de Mateo de pie.
    private float AlturaNotaAlta()
    {
        float alto = AlturaDePie();
        return alturaMuelle + alto * (PlayerMovement.AlturaAgachado + 1f) * 0.5f;
    }

    // La nota alta se achica si hace falta para que pase sobre Mateo agachado sin rozarlo.
    private float EscalaNotaAlta(Bullet nota)
    {
        if (!nota.TryGetComponent(out CircleCollider2D circulo) || circulo.radius <= 0f) return 1f;
        float radioMaximo = AlturaDePie() * (1f - PlayerMovement.AlturaAgachado) * 0.35f;
        return Mathf.Min(1.25f, radioMaximo / circulo.radius);
    }

    private static void MostrarAviso(GameObject senales, SpriteRenderer parlante, bool activo)
    {
        senales.SetActive(activo);
        parlante.color = activo ? Color.white : ParlanteEnEspera;
    }

    private Bullet Lanzar(Sprite nota, Vector2 origen, Vector2 direccion, float rapidez, int impacto, float duracion)
    {
        Bullet proyectil = Instantiate(notaPrefab, origen, Quaternion.identity, efectos);
        proyectil.DispararNota(nota, direccion, rapidez, impacto, duracion);
        return proyectil;
    }
}
