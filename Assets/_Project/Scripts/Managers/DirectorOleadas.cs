using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(100)]
public class DirectorOleadas : MonoBehaviour
{
    public enum EstadoOleadas { Preparacion, Combate, Descanso, EntradaJefe, Jefe, Detenido }

    [Serializable]
    private class Aparicion
    {
        public EnemigoMenor prefab;
        public RecogibleCombate recompensa;
        [Range(0f, 1f)] public float probabilidadRecompensa;
    }

    [Serializable]
    private class Oleada
    {
        public string nombre;
        [Min(1)] public int enemigos = 2;
        public Aparicion[] secuencia;
        [Min(1)] public int maximoSimultaneos = 2;
        [Min(0f)] public float intervaloAparicion = 0.9f;
    }

    [Header("Combate")]
    [SerializeField] private Health jugador;
    [SerializeField] private Health jefe;
    [SerializeField] private BossController controladorJefe;
    [SerializeField] private SeguirCamaraX movimientoJefe;
    [SerializeField] private SpriteRenderer imagenJefe;
    [SerializeField] private Collider2D colisionJefe;
    [SerializeField] private EnemigoMenor enemigoPrefab;
    [SerializeField] private RecompensasCombate recompensas;

    [Header("Oleadas")]
    [SerializeField] private Oleada[] oleadas = { new Oleada(), new Oleada(), new Oleada() };
    [SerializeField, Min(0f)] private float esperaInicial = 1.5f;
    [SerializeField, Min(0f)] private float esperaEntreOleadas = 3f;
    [SerializeField, Min(0f)] private float esperaAntesJefe = 4f;
    [SerializeField, Min(0.1f)] private float avisoAparicion = 0.8f;
    [SerializeField, Min(1f)] private float distanciaSeguraJugador = 4f;
    [SerializeField] private Collider2D sueloArena;
    [SerializeField] private float distanciaAparicion = 10.8f;
    [SerializeField] private float alturaAparicion = -2.7f;

    [Header("Indicadores")]
    [SerializeField] private GameObject barraJefe;
    [SerializeField] private TMP_Text textoOleadas;

    private readonly HashSet<EnemigoMenor> activos = new HashSet<EnemigoMenor>();
    private int indiceOleada;
    private int porAparecer;
    private bool jefeActivo;
    private bool detenido;
    private GameObject aviso;
    private string ladoAviso;
    private PlayerShooting disparo;
    private static Sprite figuraAviso;
    public EstadoOleadas Estado { get; private set; }
    public int IndiceOleada => indiceOleada;
    public int EnemigosActivos => activos.Count;
    public int EnemigosPorAparecer => porAparecer;
    public bool JefeActivo => jefeActivo;
    public bool FaseJefeAlcanzada => jefeActivo || (oleadas != null && oleadas.Length > 0 &&
        SesionJuego.ProgresoOleadas(gameObject.scene.name) >= oleadas.Length);
    public event Action<int> OleadaSuperada;
    public event Action JefeHabilitado;

    private void Awake()
    {
        if (controladorJefe != null) controladorJefe.enabled = false;
        if (movimientoJefe != null) movimientoJefe.enabled = false;
        if (imagenJefe != null) imagenJefe.enabled = false;
        if (colisionJefe != null) colisionJefe.enabled = false;
        if (jefe != null) jefe.Invulnerable = true;
        if (barraJefe != null) barraJefe.SetActive(false);
        if (jugador != null) disparo = jugador.GetComponent<PlayerShooting>();
        CambiarEstado(EstadoOleadas.Preparacion);
    }

    private void OnEnable()
    {
        if (jugador != null) jugador.OnMuerte += AlMorirJugador;
        if (jefe != null) jefe.OnMuerte += AlVencerJefe;
    }

    private void OnDisable()
    {
        if (jugador != null) jugador.OnMuerte -= AlMorirJugador;
        if (jefe != null) jefe.OnMuerte -= AlVencerJefe;
        Detener();
        foreach (var enemigo in activos)
            if (enemigo != null)
            {
                enemigo.Eliminado -= AlEliminarEnemigo;
                enemigo.Retirado -= AlEliminarEnemigo;
            }
        activos.Clear();
    }

    private void Start()
    {
        if (jugador == null || jefe == null || controladorJefe == null ||
            oleadas == null || oleadas.Length == 0 || !OleadasConfiguradas())
        {
            Debug.LogError("La fase de oleadas necesita jugador, jefe y enemigos configurados.", this);
            Detener();
            return;
        }
        indiceOleada = Mathf.Clamp(SesionJuego.ProgresoOleadas(gameObject.scene.name), 0, oleadas.Length);
        if (indiceOleada == oleadas.Length) StartCoroutine(EntrarJefe());
        else StartCoroutine(EjecutarOleadas());
    }

    private bool OleadasConfiguradas()
    {
        foreach (var ronda in oleadas)
        {
            if (ronda == null) return false;
            if (ronda.secuencia == null || ronda.secuencia.Length == 0)
            {
                if (enemigoPrefab == null) return false;
                continue;
            }
            foreach (var entrada in ronda.secuencia)
                if (entrada == null || entrada.prefab == null) return false;
        }
        return true;
    }

    private IEnumerator EjecutarOleadas()
    {
        yield return Esperar(esperaInicial, "Prepárate", EstadoOleadas.Preparacion);
        for (; indiceOleada < oleadas.Length && !detenido; indiceOleada++)
        {
            var ronda = oleadas[indiceOleada];
            int cantidad = ronda.secuencia != null && ronda.secuencia.Length > 0
                ? ronda.secuencia.Length : Mathf.Max(1, ronda.enemigos);
            porAparecer = cantidad;
            CambiarEstado(EstadoOleadas.Combate);
            ActualizarTexto();

            for (int i = 0; i < cantidad && !detenido; i++)
            {
                while (activos.Count >= Mathf.Max(1, ronda.maximoSimultaneos) && !detenido) yield return null;
                if (detenido) yield break;
                var entrada = ronda.secuencia != null && ronda.secuencia.Length > 0 ? ronda.secuencia[i] : null;
                yield return AnunciarYCrear(entrada, i);
                if (detenido) yield break;
                porAparecer--;
                ActualizarTexto();
                if (i < cantidad - 1) yield return new WaitForSeconds(Mathf.Max(0f, ronda.intervaloAparicion));
            }

            while (activos.Count > 0 && !detenido) yield return null;
            if (detenido) yield break;
            LimpiarProyectiles();
            SesionJuego.GuardarProgresoOleadas(gameObject.scene.name, indiceOleada + 1);
            OleadaSuperada?.Invoke(indiceOleada + 1);
            if (indiceOleada < oleadas.Length - 1)
                yield return Esperar(esperaEntreOleadas, "Oleada superada · recoge los ítems", EstadoOleadas.Descanso);
        }
        if (!detenido) yield return EntrarJefe();
    }

    private IEnumerator EntrarJefe()
    {
        jefe.Invulnerable = true;
        LimpiarProyectiles();
        yield return Esperar(esperaAntesJefe, "Prepárate para el jefe", EstadoOleadas.EntradaJefe);
        if (!detenido) ActivarJefe();
    }

    private IEnumerator Esperar(float segundos, string mensaje, EstadoOleadas estado)
    {
        CambiarEstado(estado);
        float restante = Mathf.Max(0f, segundos);
        while (restante > 0f && !detenido)
        {
            if (textoOleadas != null) textoOleadas.text = mensaje + " · " + Mathf.CeilToInt(restante);
            restante -= Time.deltaTime;
            yield return null;
        }
    }

    private void Limites(out float izquierda, out float derecha)
    {
        izquierda = -Mathf.Abs(distanciaAparicion) - 1f;
        derecha = Mathf.Abs(distanciaAparicion) + 1f;
        if (sueloArena == null) return;
        izquierda = sueloArena.bounds.min.x + 0.8f;
        derecha = sueloArena.bounds.max.x - 0.8f;
    }

    private bool EsSeguro(Vector2 punto)
    {
        if (jugador == null || Mathf.Abs(punto.x - jugador.transform.position.x) < distanciaSeguraJugador) return false;
        foreach (var otro in activos)
            if (otro != null && Vector2.Distance(punto, otro.transform.position) < 1.7f) return false;
        var obstaculo = Physics2D.OverlapCircle(punto, 0.6f, LayerMask.GetMask("Ground"));
        return obstaculo == null || obstaculo.isTrigger;
    }

    private bool BuscarPosicion(int numero, out Vector2 posicion)
    {
        Limites(out float izquierda, out float derecha);
        bool desdeIzquierda = (numero + indiceOleada) % 2 == 0;
        for (int i = 0; i <= 20; i++)
        {
            float t = i / 20f;
            float x = Mathf.Lerp(izquierda + 0.2f, derecha - 0.2f, desdeIzquierda ? t : 1f - t);
            posicion = new Vector2(x, alturaAparicion);
            if (EsSeguro(posicion)) return true;
        }
        posicion = default;
        return false;
    }

    private IEnumerator AnunciarYCrear(Aparicion entrada, int numero)
    {
        var prefab = entrada != null ? entrada.prefab : enemigoPrefab;
        while (!detenido)
        {
            if (!BuscarPosicion(numero, out Vector2 posicion)) { yield return null; continue; }
            aviso = new GameObject("Aviso de entrada");
            aviso.transform.SetParent(transform);
            aviso.transform.position = posicion;
            aviso.transform.localScale = new Vector3(1.1f, 1.3f, 1f);
            var imagen = aviso.AddComponent<SpriteRenderer>();
            if (figuraAviso == null)
                figuraAviso = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
            imagen.sprite = figuraAviso;
            imagen.sortingOrder = 18;
            var color = prefab.GetComponent<SpriteRenderer>().color;
            color.a = 0.35f;
            imagen.color = color;
            ladoAviso = posicion.x < jugador.transform.position.x ? "Entrada por la izquierda" : "Entrada por la derecha";
            ActualizarTexto();
            yield return new WaitForSeconds(Mathf.Max(0.1f, avisoAparicion));
            RetirarAviso();
            // Se vuelve a comprobar: el jugador pudo acercarse durante el aviso.
            if (detenido) yield break;
            if (!EsSeguro(posicion)) continue;
            Limites(out float izquierda, out float derecha);
            var enemigo = Instantiate(prefab, posicion, Quaternion.identity, transform);
            enemigo.Configurar(jugador, izquierda, derecha);
            if (entrada != null)
                enemigo.ConfigurarRecompensa(entrada.recompensa, entrada.probabilidadRecompensa, recompensas);
            enemigo.Eliminado += AlEliminarEnemigo;
            enemigo.Retirado += AlEliminarEnemigo;
            activos.Add(enemigo);
            yield break;
        }
    }

    private void AlEliminarEnemigo(EnemigoMenor enemigo)
    {
        enemigo.Eliminado -= AlEliminarEnemigo;
        enemigo.Retirado -= AlEliminarEnemigo;
        if (activos.Remove(enemigo) && Estado == EstadoOleadas.Combate) ActualizarTexto();
    }

    private void ActualizarTexto()
    {
        if (textoOleadas == null) return;
        string nombre = oleadas[indiceOleada].nombre;
        if (aviso != null) nombre = ladoAviso;
        textoOleadas.text = "Oleada " + (indiceOleada + 1) + "/" + oleadas.Length +
            " · Restantes: " + (activos.Count + porAparecer) +
            (string.IsNullOrWhiteSpace(nombre) ? "" : "\n" + nombre);
    }

    private void CambiarEstado(EstadoOleadas estado)
    {
        Estado = estado;
        bool descanso = estado != EstadoOleadas.Combate && estado != EstadoOleadas.Jefe;
        if (disparo != null) disparo.SuspenderMejora = descanso;
        if (recompensas != null) recompensas.EnDescanso = descanso;
    }

    private void ActivarJefe()
    {
        if (detenido || jefeActivo || activos.Count != 0 || porAparecer != 0) return;
        LimpiarProyectiles();
        jefeActivo = true;
        CambiarEstado(EstadoOleadas.Jefe);
        SesionJuego.GuardarProgresoOleadas(gameObject.scene.name, oleadas.Length);
        if (textoOleadas != null) textoOleadas.gameObject.SetActive(false);
        if (barraJefe != null) barraJefe.SetActive(true);
        jefe.Invulnerable = false;
        if (imagenJefe != null) imagenJefe.enabled = true;
        if (colisionJefe != null) colisionJefe.enabled = true;
        controladorJefe.enabled = true;
        JefeHabilitado?.Invoke();
    }

    private void LimpiarProyectiles()
    {
        foreach (var bala in FindObjectsByType<Bullet>(FindObjectsSortMode.None))
            if (bala.gameObject.scene == gameObject.scene)
            {
                bala.gameObject.SetActive(false);
                Destroy(bala.gameObject);
            }
    }

    private void RetirarAviso()
    {
        if (aviso != null) { aviso.SetActive(false); Destroy(aviso); }
        aviso = null;
    }

    private void Detener()
    {
        detenido = true;
        StopAllCoroutines();
        RetirarAviso();
        CambiarEstado(EstadoOleadas.Detenido);
        foreach (var enemigo in activos) if (enemigo != null) enemigo.enabled = false;
    }

    private void AlMorirJugador() => Detener();
    private void AlVencerJefe()
    {
        Detener();
        SesionJuego.LimpiarProgresoOleadas();
    }
}

