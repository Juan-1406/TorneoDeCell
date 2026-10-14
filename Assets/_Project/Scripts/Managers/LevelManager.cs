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
        SesionJuego.VidasExtra--;
        ActualizarTexto();

        jugador.GetComponent<PlayerMovement>().enabled = false;
        jugador.GetComponent<PlayerShooting>().enabled = false;
        jugador.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        jugador.GetComponent<PlayerParry>().enabled = false;

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
        ProgresoJuego.MarcarDerrotado(idJefe);
        Invoke(nameof(VolverASeleccion), retardoVictoria);
    }

    private void VolverASeleccion()
    {
        SceneManager.LoadScene("LevelSelect");
    }
}
