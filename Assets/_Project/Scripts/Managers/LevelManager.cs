using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private Health jugador;
    [SerializeField] private TMP_Text textoVidas;
    [SerializeField] private GameObject panelGameOver;
    [SerializeField] private float retardo = 1.5f;
    [Header("Victoria")]
    [SerializeField] private Health jefe;
    [SerializeField] private int idJefe = 0;
    [SerializeField] private Sprite poseVictoriaMateo;

    private bool combateFinalizado;
    private bool cambiandoEscena;
    private bool victoria;
    private float segundosActivos;
    private int parries;
    private PlayerParry parry;
    private SegundaFaseRenato faseRenato;
    private GameObject resultados;

    private void Awake()
    {
        if (jugador != null) parry = jugador.GetComponent<PlayerParry>();
        if (jefe != null) faseRenato = jefe.GetComponent<SegundaFaseRenato>();
    }

    private void OnEnable()
    {
        if (jugador != null) jugador.OnMuerte += AlMorirJugador;
        if (jefe != null) jefe.OnMuerte += AlVencerJefe;
        if (parry != null) parry.ProyectilDevuelto += ContarParry;
    }

    private void OnDisable()
    {
        if (jugador != null) jugador.OnMuerte -= AlMorirJugador;
        if (jefe != null) jefe.OnMuerte -= AlVencerJefe;
        if (parry != null) parry.ProyectilDevuelto -= ContarParry;
    }

    private void ContarParry() => parries++;

    private void Update()
    {
        if (!combateFinalizado && !SesionJuego.EnPausa && (faseRenato == null || !faseRenato.EnDialogo))
            segundosActivos += Time.deltaTime;
    }

    private void Start()
    {
        Time.timeScale = 1f;
        if (!ProgresoJuego.PuedeCargarEscena(gameObject.scene.name))
        {
            VolverASeleccion();
            return;
        }

        if (panelGameOver != null) panelGameOver.SetActive(false);
        if (textoVidas != null)
        {
            textoVidas.color = EstiloTorneo.Papel;
            textoVidas.fontStyle = FontStyles.Bold;
            var fondo = EstiloTorneo.Imagen(textoVidas.transform, "Fondo de vidas", EstiloTorneo.Tinta, -.06f, 0, 1.12f, 1);
            fondo.transform.SetParent(textoVidas.transform.parent, true);
            fondo.transform.SetSiblingIndex(textoVidas.transform.GetSiblingIndex());
        }
        ActualizarTexto();

        if (jefe != null && SesionJuego.IntentarRestaurarVidaJefe(SceneManager.GetActiveScene().name, out int vidaJefe))
            jefe.RestaurarVida(vidaJefe);
    }

    private void AlMorirJugador()
    {
        if (!FinalizarCombate()) return;

        SesionJuego.VidasExtra--;
        ActualizarTexto();

        if (SesionJuego.VidasExtra > 0)
        {
            DirectorOleadas director = FindFirstObjectByType<DirectorOleadas>();
            if (jefe != null && jefe.VidaActual > 0 &&
                (!jefe.TryGetComponent(out SegundaFaseRenato segundaFase) || !segundaFase.FueraDePrimeraFase) &&
                (director == null || director.FaseJefeAlcanzada))
                SesionJuego.GuardarVidaJefe(SceneManager.GetActiveScene().name, jefe.VidaActual);
            Invoke(nameof(RecargarNivel), retardo);
        }
        else
        {
            Invoke(nameof(MostrarGameOver), retardo);
        }
    }

    private void MostrarGameOver()
    {
        if (panelGameOver != null) panelGameOver.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ReintentarJuego()
    {
        if (cambiandoEscena) return;
        SesionJuego.ReiniciarIntento();
        RecargarNivel();
    }

    public void ReintentarVictoria()
    {
        if (!victoria || cambiandoEscena) return;
        SesionJuego.HabilitarRepeticion(gameObject.scene.name);
        ReintentarJuego();
    }

    private void RecargarNivel()
    {
        if (cambiandoEscena) return;
        if (!ProgresoJuego.PuedeCargarEscena(gameObject.scene.name))
        {
            VolverASeleccion();
            return;
        }

        cambiandoEscena = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void ActualizarTexto()
    {
        if (textoVidas != null) textoVidas.text = "Vidas extra: " + SesionJuego.VidasExtra;
    }

    private void AlVencerJefe()
    {
        if (!FinalizarCombate()) return;

        victoria = true;
        bool padrePasta = idJefe == 1 && jefe.TryGetComponent<PadrePasta>(out _);
        if (padrePasta)
        {
            if (!ProgresoJuego.PadrePastaFaseUnoSuperada) ProgresoJuego.MarcarPadrePastaFaseUnoSuperada();
        }
        else if (!ProgresoJuego.JefeDerrotado(idJefe)) ProgresoJuego.MarcarDerrotado(idJefe);

        Canvas canvas = textoVidas != null ? textoVidas.GetComponentInParent<Canvas>() : null;
        if (canvas == null || jugador == null)
        {
            VolverASeleccion();
            return;
        }
        string nombre = padrePasta ? "Padre Pasta" : "René Puente";
        resultados = ResultadosCombate.Mostrar(canvas, textoVidas.font, poseVictoriaMateo, nombre,
            padrePasta ? "Fase 1 superada" : "René Puente derrotado", segundosActivos, jugador, parries, this);
    }

    private bool FinalizarCombate()
    {
        // El primer resultado cierra la ronda; se ignoran impactos posteriores.
        if (combateFinalizado) return false;
        combateFinalizado = true;

        if (jugador != null)
        {
            DetenerComponente(jugador.GetComponent<PlayerMovement>());
            DetenerComponente(jugador.GetComponent<PlayerShooting>());
            DetenerComponente(jugador.GetComponent<PlayerParry>());
            jugador.Invulnerable = true;

            if (jugador.TryGetComponent(out Rigidbody2D cuerpo))
            {
                cuerpo.linearVelocity = Vector2.zero;
                cuerpo.simulated = false;
            }
        }

        if (jefe != null) jefe.Invulnerable = true;
        if (jefe != null && jefe.TryGetComponent(out PadrePasta padrePasta))
            DetenerComponente(padrePasta);

        // Tambien cubre escenas de pruebas sin un jefe asignado al LevelManager.
        foreach (BossController controlador in FindObjectsByType<BossController>(FindObjectsSortMode.None))
        {
            if (controlador.gameObject.scene == gameObject.scene)
                DetenerComponente(controlador);
        }

        foreach (Bullet bala in FindObjectsByType<Bullet>(FindObjectsSortMode.None))
        {
            if (bala.gameObject.scene != gameObject.scene) continue;

            // Destroy se aplica al final del frame: desactivar evita mas colisiones ahora.
            bala.gameObject.SetActive(false);
            Destroy(bala.gameObject);
        }

        // La pausa no debe congelar la espera hacia el reintento o la seleccion.
        foreach (PauseManager pausa in FindObjectsByType<PauseManager>(FindObjectsSortMode.None))
        {
            if (pausa.gameObject.scene == gameObject.scene)
                pausa.enabled = false;
        }

        return true;
    }

    private static void DetenerComponente(MonoBehaviour componente)
    {
        if (componente == null) return;

        // Deshabilitar un MonoBehaviour por si solo no cancela sus corutinas.
        componente.StopAllCoroutines();
        componente.enabled = false;
    }

    public void VolverASeleccion()
    {
        if (cambiandoEscena) return;
        cambiandoEscena = true;
        if (resultados != null) resultados.SetActive(false);
        Time.timeScale = 1f;
        SesionJuego.Reiniciar();
        SceneManager.LoadScene("LevelSelect");
    }
}
