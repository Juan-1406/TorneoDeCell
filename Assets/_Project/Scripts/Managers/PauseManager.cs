using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private GameObject panelPausa;
    [SerializeField] private GameObject primerBoton;
    [SerializeField] private SceneLoader cargador;

    private Button[] botones;
    private bool saliendo;
    private InputSystemUIInputModule entradaUI;

    private void Awake()
    {
        panelPausa.SetActive(false);
        botones = panelPausa.GetComponentsInChildren<Button>(true);
        panelPausa.AddComponent<VistaMenuCombate>().Componer(botones, primerBoton, false);
        SesionJuego.EnPausa = false;
    }

    private void Start()
    {
        if (EventSystem.current != null)
            entradaUI = EventSystem.current.GetComponent<InputSystemUIInputModule>();
    }

    private void Update()
    {
        bool cancelarUI = entradaUI?.cancel?.action?.WasPerformedThisFrame() == true;
        if (!cancelarUI && !Input.GetButtonDown("Cancel")) return;

        if (SesionJuego.EnPausa) Reanudar();
        else Pausar();
    }

    public void Pausar()
    {
        if (saliendo || !isActiveAndEnabled || Time.timeScale == 0f) return;

        SesionJuego.EnPausa = true;
        Time.timeScale = 0f;
        panelPausa.SetActive(true);
    }

    public void Reanudar()
    {
        if (saliendo || !SesionJuego.EnPausa) return;

        SesionJuego.EnPausa = false;
        SesionJuego.SoltarEntradaDeMenu = true;
        Time.timeScale = 1f;
        panelPausa.SetActive(false);
    }

    public void Reintentar()
    {
        SalirDeLaPausa(SceneManager.GetActiveScene().name);
    }

    public void VolverASeleccion()
    {
        SalirDeLaPausa("LevelSelect");
    }

    private void SalirDeLaPausa(string escena)
    {
        if (saliendo || !isActiveAndEnabled || !SesionJuego.EnPausa) return;
        if (!cargador.IntentarCargarEscena(escena)) return;

        saliendo = true;
        foreach (Button boton in botones)
            boton.interactable = false;
    }

    private void OnDisable()
    {
        if (SesionJuego.EnPausa && panelPausa != null && panelPausa.activeSelf)
            Reanudar();
    }
}
