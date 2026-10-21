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
    [SerializeField] private float retardoVictoria = 2f;

    private bool combateFinalizado;

    private void OnEnable()
    {
        if (jugador != null) jugador.OnMuerte += AlMorirJugador;
        if (jefe != null) jefe.OnMuerte += AlVencerJefe;
    }

    private void OnDisable()
    {
        if (jugador != null) jugador.OnMuerte -= AlMorirJugador;
        if (jefe != null) jefe.OnMuerte -= AlVencerJefe;
    }

    private void Start()
    {
        Time.timeScale = 1f;
        if (panelGameOver != null) panelGameOver.SetActive(false);
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
            if (jefe != null && jefe.VidaActual > 0)
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
        SesionJuego.Reiniciar();
        RecargarNivel();
    }

    private void RecargarNivel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void ActualizarTexto()
    {
        if (textoVidas != null) textoVidas.text = "Vidas extra: " + SesionJuego.VidasExtra;
    }

    private void AlVencerJefe()
    {
        if (!FinalizarCombate()) return;

        ProgresoJuego.MarcarDerrotado(idJefe);
        Invoke(nameof(VolverASeleccion), retardoVictoria);
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

    private void VolverASeleccion()
    {
        SceneManager.LoadScene("LevelSelect");
    }
}
