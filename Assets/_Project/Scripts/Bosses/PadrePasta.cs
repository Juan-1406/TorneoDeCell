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
    [SerializeField] private Sprite notaCeleste, notaNaranja, notaRosada;
    [SerializeField] private Transform bocaMicrofono;
    [SerializeField] private GameObject senalesHorizontales, senalesVerticales;
    [SerializeField] private SpriteRenderer parlanteHorizontal, parlanteVertical;
    [SerializeField, Min(0.5f)] private float duracionAviso = 1.15f;
    [SerializeField, Min(0.1f)] private float velocidadHorizontal = 7f, velocidadVertical = 6.5f;
    [SerializeField, Min(0.1f)] private float velocidadTeledirigida = 4.8f;
    [SerializeField, Min(0f)] private float giroTeledirigida = 85f;

    private readonly EscudoPorDisparos escudo = new EscudoPorDisparos();
    private static readonly Color ParlanteEnEspera = new Color(0.75f, 0.75f, 0.8f);
    private Health vida;
    private SpriteRenderer cuerpo;
    private Transform objetivo;
    private Camera camara;
    private float semialtoArena;
    private Sprite pose;
    private float reaccionHasta;

    private void Awake()
    {
        vida = GetComponent<Health>();
        cuerpo = GetComponent<SpriteRenderer>();
    }

    private void OnEnable() => vida.OnDanioRecibido += Reaccionar;

    private void Start()
    {
        pose = reposo;
        GameObject jugador = GameObject.FindGameObjectWithTag("Player");
        if (jugador == null) return;
        objetivo = jugador.transform;
        camara = Camera.main;
        if (camara != null) semialtoArena = camara.orthographicSize;
        escudoVisual.sprite = FeedbackCombate.Aro;
        StartCoroutine(Combate());
    }

    private void LateUpdate()
    {
        cuerpo.sprite = Time.time < reaccionHasta ? danio : pose;
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
        yield return new WaitForSeconds(2f);
        while (true)
        {
            yield return Rafaga(senalesHorizontales, parlanteHorizontal, avisoHorizontal,
                notaNaranja, Vector2.left, velocidadHorizontal, true);
            yield return new WaitForSeconds(2.8f);
            yield return Rafaga(senalesVerticales, parlanteVertical, avisoVertical,
                notaRosada, Vector2.down, velocidadVertical, false);
            yield return new WaitForSeconds(1.15f);
            yield return NotaTeledirigida();
            yield return new WaitForSeconds(3.6f);
        }
    }

    private IEnumerator Rafaga(GameObject senales, SpriteRenderer parlante, Sprite gesto,
        Sprite nota, Vector2 direccion, float rapidez, bool escalonada)
    {
        pose = gesto;
        MostrarAviso(senales, parlante, true);
        yield return new WaitForSeconds(duracionAviso);

        senales.SetActive(false);
        pose = disparo;
        Transform carriles = senales.transform;
        int tandas = escalonada ? carriles.childCount : 1;
        for (int tanda = 0; tanda < tandas; tanda++)
        {
            int primero = escalonada ? tanda : 0;
            int fin = escalonada ? tanda + 1 : carriles.childCount;
            for (int i = 0; i < 3; i++)
            {
                for (int carril = primero; carril < fin; carril++)
                    Lanzar(nota, carriles.GetChild(carril).position, direccion, rapidez);
                yield return new WaitForSeconds(0.23f);
            }
        }
        MostrarAviso(senales, parlante, false);
        pose = reposo;
    }

    private IEnumerator NotaTeledirigida()
    {
        pose = carga;
        yield return new WaitForSeconds(0.7f);
        pose = disparo;
        Vector2 direccion = objetivo.position - bocaMicrofono.position;
        Lanzar(notaCeleste, bocaMicrofono.position, direccion, velocidadTeledirigida, objetivo, giroTeledirigida);
        yield return new WaitForSeconds(0.35f);
        pose = reposo;
    }

    private static void MostrarAviso(GameObject senales, SpriteRenderer parlante, bool activo)
    {
        senales.SetActive(activo);
        parlante.color = activo ? Color.white : ParlanteEnEspera;
    }

    private void Lanzar(Sprite nota, Vector2 origen, Vector2 direccion, float rapidez,
        Transform seguimiento = null, float giro = 0f)
    {
        Bullet proyectil = Instantiate(notaPrefab, origen, Quaternion.identity);
        proyectil.DispararNota(nota, direccion, rapidez, seguimiento, giro);
    }
}
