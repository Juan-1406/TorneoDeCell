using UnityEngine;

public class SeguirCamaraX : MonoBehaviour
{
    [SerializeField] private float desfaseX = 5f;
    [SerializeField] private float suavidad = 2f;

    private Camera cam;
    private Collider2D colision;
    private float alturaInicial;
    private float amplitudHorizontal;
    private float amplitudVertical;
    private float frecuencia;
    private float objetivoHorizontal;
    private float objetivoVertical;
    private float objetivoFrecuencia;
    private float faseOscilacion;

    private void Awake()
    {
        cam = Camera.main;
        colision = GetComponent<Collider2D>();
        alturaInicial = transform.position.y;
    }

    public void ConfigurarOscilacion(float horizontal, float vertical, float ciclosPorSegundo)
    {
        objetivoHorizontal = Mathf.Max(0f, horizontal);
        objetivoVertical = Mathf.Max(0f, vertical);
        objetivoFrecuencia = Mathf.Max(0f, ciclosPorSegundo);
    }

    private void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        float paso = Time.deltaTime;
        amplitudHorizontal = Mathf.MoveTowards(amplitudHorizontal, objetivoHorizontal, 1.5f * paso);
        amplitudVertical = Mathf.MoveTowards(amplitudVertical, objetivoVertical, 1.5f * paso);
        frecuencia = Mathf.MoveTowards(frecuencia, objetivoFrecuencia, 0.5f * paso);
        faseOscilacion += frecuencia * 2f * Mathf.PI * paso;

        float objetivoX = cam.transform.position.x + desfaseX
            + amplitudHorizontal * Mathf.Sin(faseOscilacion);
        if (cam.orthographic && colision != null)
        {
            float bordeDerecho = cam.transform.position.x + cam.orthographicSize * cam.aspect;
            objetivoX = Mathf.Min(objetivoX, bordeDerecho - colision.bounds.extents.x - 0.25f);
        }

        // La oscilacion vertical solo sube desde la altura original del jefe.
        float objetivoY = alturaInicial + amplitudVertical * (1f - Mathf.Cos(2f * faseOscilacion)) * 0.5f;
        float interpolacion = Mathf.Clamp01(suavidad * paso);
        float nuevaX = Mathf.Lerp(transform.position.x, objetivoX, interpolacion);
        float nuevaY = Mathf.Lerp(transform.position.y, objetivoY, interpolacion);
        transform.position = new Vector3(nuevaX, nuevaY, transform.position.z);
    }
}
