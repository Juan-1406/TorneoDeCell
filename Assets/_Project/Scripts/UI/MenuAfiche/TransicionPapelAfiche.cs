using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class TransicionPapelAfiche : MonoBehaviour
{
    private static TransicionPapelAfiche activa;
    private PapelAfiche hoja, sombra;
    public static bool EnCurso => activa != null;

    public static void Cargar(string escena)
    {
        if (activa != null) return;
        if (!Application.CanStreamedLevelBeLoaded(escena))
        {
            Debug.LogError("La escena de destino del menú no está incluida: " + escena);
            return;
        }
        var go = new GameObject("Transición de papel", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(go);
        var c = go.GetComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 32000;
        var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1672, 941); scaler.matchWidthOrHeight = .5f;
        activa = go.AddComponent<TransicionPapelAfiche>();
        activa.StartCoroutine(activa.Ejecutar(escena));
    }
    private IEnumerator Ejecutar(string escena)
    {
        sombra = Crear("Sombra del borde", new Color(.015f, .025f, .055f, .7f));
        hoja = Crear("Papel cálido", new Color(.96f, .89f, .73f));
        float t = 0;
        while (t < .46f)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / .46f));
            sombra.Actualizar(0, Mathf.Min(1, p + .016f)); hoja.Actualizar(0, p);
            yield return null;
        }
        hoja.Actualizar(0, 1); sombra.Actualizar(0, 1);
        Time.timeScale = 1; SesionJuego.Reiniciar();
        AsyncOperation carga = SceneManager.LoadSceneAsync(escena);
        while (carga != null && !carga.isDone) yield return null;
        yield return null;
        t = 0;
        while (t < .40f)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / .40f));
            hoja.Actualizar(p, 1); sombra.Actualizar(Mathf.Max(0, p - .016f), 1);
            yield return null;
        }
        Destroy(gameObject);
    }
    private PapelAfiche Crear(string nombre, Color color)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(PapelAfiche));
        var rt = go.GetComponent<RectTransform>(); rt.SetParent(transform, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        var papel = go.GetComponent<PapelAfiche>(); papel.color = color; papel.raycastTarget = true;
        return papel;
    }
    private void OnDestroy() { if (activa == this) activa = null; }
}
