using UnityEngine;
using UnityEngine.EventSystems;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private GameObject panelPausa;
    [SerializeField] private GameObject primerBoton;

    private void Start()
    {
        SesionJuego.EnPausa = false;
        panelPausa.SetActive(false);
    }

    private void Update()
    {
        if (!Input.GetButtonDown("Cancel")) return;

        if (SesionJuego.EnPausa) Reanudar();
        else Pausar();
    }

    public void Pausar()
    {
        if (Time.timeScale == 0f) return;

        SesionJuego.EnPausa = true;
        Time.timeScale = 0f;
        panelPausa.SetActive(true);
        EventSystem.current.SetSelectedGameObject(primerBoton);
    }

    public void Reanudar()
    {
        SesionJuego.EnPausa = false;
        Time.timeScale = 1f;
        panelPausa.SetActive(false);
    }
}