using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void CargarEscena(string nombre)
    {
        Time.timeScale = 1f;
        SesionJuego.Reiniciar();
        SceneManager.LoadScene(nombre);
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