using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void CargarEscena(string nombre)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(nombre);
    }

    public void Salir()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}