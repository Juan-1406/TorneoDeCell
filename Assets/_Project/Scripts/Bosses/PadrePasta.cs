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

    [Header("Parlante suspendido")]
    [SerializeField, Min(0.7f)] private float avisoCaida = 1.1f;
    [SerializeField, Min(0.2f)] private float duracionCaida = 0.42f;
    [SerializeField, Min(1)] private int danioCaida = 1;

    [Header("Parlante del borde")]
    [SerializeField, Min(0.7f)] private float avisoBorde = 1.05f;
    [SerializeField, Min(4f)] private float velocidadBorde = 8f;
    [SerializeField, Min(1)] private int danioBorde = 1;

    private readonly EscudoPorDisparos escudo = new EscudoPorDisparos();
    private static readonly Color ParlanteEnEspera = new Color(0.75f, 0.75f, 0.8f);
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

    private IEnumerator RafagaMicrofono(bool alta)
    {
        Vector2 origen = bocaMicrofono.position;
        Vector2 blanco = cuerpoJugador.bounds.center;
        Vector2 rumbo = alta ? Vector2.left : new Vector2(
            Mathf.Min(-0.8f, blanco.x - origen.x), Mathf.Min(0f, blanco.y - origen.y)).normalized;
        float alcance = Mathf.Min((origen.x - limitesMuelle.x + 1f) / -rumbo.x,
            rumbo.y < -0.01f ? (alturaMuelle - origen.y) / rumbo.y : float.PositiveInfinity);
        float duracion = alcance / velocidadFrontal;

        pose = carga;
        GameObject aviso = new GameObject("Trayectoria del micrófono");
        aviso.transform.SetParent(efectos, false);
        for (float distancia = 0.8f; distancia < alcance; distancia += 1.35f)
        {
            var flecha = new GameObject("Flecha celeste").AddComponent<SpriteRenderer>();
            flecha.transform.SetParent(aviso.transform, false);
            flecha.transform.position = origen + rumbo * distancia;
            flecha.transform.rotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.left, rumbo));
            flecha.sprite = flechaFrontal;
            flecha.sharedMaterial = cuerpo.sharedMaterial;
            flecha.sortingOrder = 12;
        }
        yield return new WaitForSeconds(avisoMicrofono);
        aviso.SetActive(false);
        Destroy(aviso);

        for (int i = 0; i < notasPorRafaga; i++)
        {
            pose = disparo;
            Lanzar(notaCeleste, origen, rumbo, velocidadFrontal, danioFrontal, duracion);
            FeedbackCombate.Emitir(bocaMicrofono, new Color32(88, 240, 255, 255), 3, 1.2f);
            yield return new WaitForSeconds(intervaloRafaga * 0.6f);
            pose = carga;
            yield return new WaitForSeconds(intervaloRafaga * 0.4f);
        }

        pose = reposo;
        yield return new WaitForSeconds(duracion);
    }

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
        Vector2 origen = new Vector2(limitesMuelle.x + 0.85f, alturaMuelle + 0.36f);
        senalesHorizontales.transform.position = origen;
        MostrarAviso(senalesHorizontales, parlanteHorizontal, true);
        yield return new WaitForSeconds(avisoBorde);

        MostrarAviso(senalesHorizontales, parlanteHorizontal, false);
        pose = disparo;
        float viaje = (limitesMuelle.y - origen.x + 0.5f) / velocidadBorde;
        Bullet nota = Lanzar(notaNaranja, origen, Vector2.right, velocidadBorde, danioBorde, viaje);
        nota.transform.localScale = Vector3.one * 1.25f;
        nota.GetComponent<SpriteRenderer>().flipX = true;
        FeedbackCombate.Emitir(parlanteHorizontal, new Color32(255, 174, 74, 255), 4);
        yield return new WaitForSeconds(0.25f);
        pose = reposo;
        yield return new WaitForSeconds(Mathf.Max(0f, viaje - 0.25f));
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
