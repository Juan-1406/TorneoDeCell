using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(100)]
public class DirectorOleadas : MonoBehaviour
{
    [Serializable]
    private class Oleada
    {
        [Min(1)] public int enemigos = 2;
        [Min(0f)] public float intervaloAparicion = 0.7f;
    }

    [Header("Combate")]
    [SerializeField] private Health jugador;
    [SerializeField] private Health jefe;
    [SerializeField] private BossController controladorJefe;
    [SerializeField] private SeguirCamaraX movimientoJefe;
    [SerializeField] private SpriteRenderer imagenJefe;
    [SerializeField] private Collider2D colisionJefe;
    [SerializeField] private EnemigoMenor enemigoPrefab;

    [Header("Oleadas")]
    [SerializeField] private Oleada[] oleadas = { new Oleada(), new Oleada(), new Oleada() };
    [SerializeField, Min(0f)] private float esperaInicial = 1.5f;
    [SerializeField, Min(0f)] private float esperaEntreOleadas = 1.5f;
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

    public int IndiceOleada => indiceOleada;
    public int EnemigosActivos => activos.Count;
    public int EnemigosPorAparecer => porAparecer;
    public bool JefeActivo => jefeActivo;

    private void Awake()
    {
        if (controladorJefe != null) controladorJefe.enabled = false;
        if (movimientoJefe != null) movimientoJefe.enabled = false;
        if (imagenJefe != null) imagenJefe.enabled = false;
        if (colisionJefe != null) colisionJefe.enabled = false;
        if (jefe != null) jefe.Invulnerable = true;
        if (barraJefe != null) barraJefe.SetActive(false);
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
        StopAllCoroutines();
        foreach (EnemigoMenor enemigo in activos)
            if (enemigo != null) enemigo.Eliminado -= AlEliminarEnemigo;
        activos.Clear();
    }

    private void Start()
    {
        if (jugador == null || jefe == null || controladorJefe == null || enemigoPrefab == null ||
            oleadas == null || oleadas.Length == 0)
        {
            Debug.LogError("La fase de oleadas necesita jugador, jefe, enemigo y rondas configuradas.", this);
            return;
        }

        indiceOleada = Mathf.Clamp(
            SesionJuego.ProgresoOleadas(SceneManager.GetActiveScene().name), 0, oleadas.Length);
        if (indiceOleada == oleadas.Length) ActivarJefe();
        else
        {
            jefe.Invulnerable = true;
            StartCoroutine(EjecutarOleadas());
        }
    }

    private IEnumerator EjecutarOleadas()
    {
        if (esperaInicial > 0f) yield return new WaitForSeconds(esperaInicial);

        for (; indiceOleada < oleadas.Length && !detenido; indiceOleada++)
        {
            Oleada ronda = oleadas[indiceOleada];
            int cantidad = Mathf.Max(1, ronda.enemigos);
            porAparecer = cantidad;
            ActualizarTexto();

            for (int i = 0; i < cantidad && !detenido; i++)
            {
                CrearEnemigo(i);
                porAparecer--;
                ActualizarTexto();
                if (i < cantidad - 1 && ronda.intervaloAparicion > 0f)
                    yield return new WaitForSeconds(ronda.intervaloAparicion);
            }

            while (activos.Count > 0 && !detenido) yield return null;
            if (detenido) yield break;

            SesionJuego.GuardarProgresoOleadas(SceneManager.GetActiveScene().name, indiceOleada + 1);
            if (indiceOleada < oleadas.Length - 1)
            {
                if (textoOleadas != null) textoOleadas.text = "Oleada superada";
                if (esperaEntreOleadas > 0f) yield return new WaitForSeconds(esperaEntreOleadas);
            }
        }

        if (!detenido) ActivarJefe();
    }

    private void CrearEnemigo(int numero)
    {
        float lado = (numero + indiceOleada) % 2 == 0 ? -1f : 1f;
        float distancia = distanciaAparicion - (numero / 2) * 1.4f;
        float x = lado * distancia;
        if (Mathf.Abs(x - jugador.transform.position.x) < 2.5f) x = -x;

        var enemigo = Instantiate(enemigoPrefab, new Vector3(x, alturaAparicion, 0f),
            Quaternion.identity, transform);
        enemigo.Configurar(jugador);
        enemigo.Eliminado += AlEliminarEnemigo;
        activos.Add(enemigo);
    }

    private void AlEliminarEnemigo(EnemigoMenor enemigo)
    {
        enemigo.Eliminado -= AlEliminarEnemigo;
        activos.Remove(enemigo);
        ActualizarTexto();
    }

    private void ActualizarTexto()
    {
        if (textoOleadas == null) return;
        textoOleadas.text = "Oleada " + (indiceOleada + 1) + "/" + oleadas.Length +
            "  ·  Enemigos: " + (activos.Count + porAparecer);
    }

    private void ActivarJefe()
    {
        if (detenido || jefeActivo) return;
        jefeActivo = true;
        SesionJuego.GuardarProgresoOleadas(SceneManager.GetActiveScene().name, oleadas.Length);
        if (textoOleadas != null) textoOleadas.gameObject.SetActive(false);
        if (barraJefe != null) barraJefe.SetActive(true);
        jefe.Invulnerable = false;
        if (imagenJefe != null) imagenJefe.enabled = true;
        if (colisionJefe != null) colisionJefe.enabled = true;
        controladorJefe.enabled = true;
    }

    private void AlMorirJugador()
    {
        detenido = true;
        StopAllCoroutines();
        foreach (EnemigoMenor enemigo in activos)
            if (enemigo != null) enemigo.enabled = false;
    }

    private void AlVencerJefe()
    {
        SesionJuego.LimpiarProgresoOleadas();
    }
}
