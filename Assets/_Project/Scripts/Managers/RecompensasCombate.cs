using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RecompensasCombate : MonoBehaviour
{
    [SerializeField] private Health jugador;
    [SerializeField] private Health jefe;
    [SerializeField] private Collider2D sueloArena;
    [SerializeField] private RecogibleCombate curacionPrefab;
    [SerializeField] private RecogibleCombate cadenciaPrefab;
    [SerializeField] private TMP_Text textoRecompensas;
    [SerializeField, Range(0f, 1f)] private float probabilidadParry = 0.35f;
    [SerializeField, Min(0f)] private float recargaPremioParry = 6f;
    [SerializeField, Min(1)] private int maximoObjetos = 3;
    private readonly HashSet<RecogibleCombate> objetos = new HashSet<RecogibleCombate>();
    private PlayerParry parry;
    private PlayerShooting disparo;
    private float siguientePremio;
    private float mensajeHasta;
    private string mensaje;
    public bool Finalizado { get; private set; }
    public bool EnDescanso { get; set; }
    public int ObjetosActivos => objetos.Count;

    private void Awake()
    {
        if (jugador != null)
        {
            parry = jugador.GetComponent<PlayerParry>();
            disparo = jugador.GetComponent<PlayerShooting>();
        }
    }

    private void OnEnable()
    {
        if (parry != null) parry.ParryPerfecto += AlParryPerfecto;
        if (jugador != null) jugador.OnMuerte += Terminar;
        if (jefe != null) jefe.OnMuerte += Terminar;
    }

    private void OnDisable()
    {
        if (parry != null) parry.ParryPerfecto -= AlParryPerfecto;
        if (jugador != null) jugador.OnMuerte -= Terminar;
        if (jefe != null) jefe.OnMuerte -= Terminar;
        Terminar();
    }

    public RecogibleCombate IntentarCrear(RecogibleCombate prefab, float probabilidad, Vector3 posicion)
    {
        if (Finalizado || jugador == null || jugador.VidaActual <= 0 || prefab == null) return null;
        objetos.RemoveWhere(item => item == null);
        int delTipo = 0;
        foreach (var item in objetos) if (item.Tipo == prefab.Tipo) delTipo++;
        int limiteTipo = prefab.Tipo == RecogibleCombate.TipoRecompensa.Curacion ? 2 : 1;
        if (objetos.Count >= Mathf.Max(1, maximoObjetos) || delTipo >= limiteTipo ||
            probabilidad <= 0f || Random.value >= Mathf.Clamp01(probabilidad)) return null;

        if (sueloArena != null)
        {
            Bounds b = sueloArena.bounds;
            posicion.x = Mathf.Clamp(posicion.x, b.min.x + 1f, b.max.x - 1f);
            posicion.y = b.max.y + 0.65f;
        }
        posicion.z = 0f;
        var itemNuevo = Instantiate(prefab, posicion, Quaternion.identity, transform);
        itemNuevo.Configurar(jugador, this);
        objetos.Add(itemNuevo);
        return itemNuevo;
    }

    private void AlParryPerfecto()
    {
        if (Finalizado || SesionJuego.EnPausa || Time.time < siguientePremio) return;
        siguientePremio = Time.time + Mathf.Max(0.1f, recargaPremioParry);
        MostrarMensaje("Parry perfecto");
        var prefab = jugador.VidaActual < jugador.VidaMaxima ? curacionPrefab : cadenciaPrefab;
        IntentarCrear(prefab, probabilidadParry, jugador.transform.position);
    }

    public void AlRecoger(RecogibleCombate item)
    {
        objetos.Remove(item);
        MostrarMensaje(item.Tipo == RecogibleCombate.TipoRecompensa.Curacion
            ? "Salud recuperada" : "Disparo rápido activado");
    }

    public void Retirar(RecogibleCombate item) => objetos.Remove(item);

    private void MostrarMensaje(string texto)
    {
        mensaje = texto;
        mensajeHasta = Time.time + 2f;
    }

    private void Update()
    {
        if (textoRecompensas == null) return;
        if (Finalizado) { textoRecompensas.text = ""; return; }
        string buff = disparo != null && disparo.SegundosMejora > 0f
            ? "Disparo rápido: " + Mathf.CeilToInt(disparo.SegundosMejora) + " s" : "";
        textoRecompensas.text = Time.time < mensajeHasta ? mensaje + (buff == "" ? "" : "\n" + buff) : buff;
    }

    public void Terminar()
    {
        if (Finalizado) return;
        Finalizado = true;
        if (disparo != null) disparo.LimpiarMejora();
        foreach (var item in new List<RecogibleCombate>(objetos))
            if (item != null) { item.gameObject.SetActive(false); Destroy(item.gameObject); }
        objetos.Clear();
        if (textoRecompensas != null) textoRecompensas.text = "";
    }
}

