using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    private bool cargando;

    public void CargarEscena(string nombre)
    {
        IntentarCargarEscena(nombre);
    }

    public bool IntentarCargarEscena(string nombre)
    {
        if (cargando || !Application.CanStreamedLevelBeLoaded(nombre) ||
            !ProgresoJuego.PuedeCargarEscena(nombre)) return false;

        cargando = true;
        Time.timeScale = 1f;
        if (SesionJuego.EscenaRepeticion == nombre) SesionJuego.ReiniciarIntento();
        else SesionJuego.Reiniciar();
        SceneManager.LoadScene(nombre);
        return true;
    }

    public void ReiniciarEscena()
    {
        CargarEscena(SceneManager.GetActiveScene().name);
    }

    public void Salir()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
