using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [SerializeField] private GameObject balaPrefab;
    [SerializeField] private float cadencia = 0.2f;

    private Camera cam;
    private float siguienteDisparo;
    private float mejoraRestante;
    private float multiplicadorCadencia = 1f;
    public bool SuspenderMejora { get; set; }
    public float SegundosMejora => mejoraRestante;
    public float CadenciaActual => cadencia * (mejoraRestante > 0f ? multiplicadorCadencia : 1f);

    public void LimpiarMejora()
    {
        mejoraRestante = 0f;
        multiplicadorCadencia = 1f;
    }

    private void OnDisable() => LimpiarMejora();

    public void MejorarCadenciaTemporal(float multiplicador, float duracion)
    {
        if (multiplicador <= 0f || multiplicador >= 1f || duracion <= 0f) return;

        multiplicadorCadencia = mejoraRestante > 0f
            ? Mathf.Min(multiplicadorCadencia, multiplicador) : multiplicador;
        mejoraRestante = Mathf.Max(mejoraRestante, duracion);
        siguienteDisparo = Mathf.Min(siguienteDisparo, Time.time + cadencia * multiplicadorCadencia);
    }

    private void Awake()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        if (SesionJuego.EnPausa || Time.timeScale == 0f) return;
        if (!SuspenderMejora) mejoraRestante = Mathf.Max(0f, mejoraRestante - Time.deltaTime);
        if (Input.GetButton("Fire1") && Time.time >= siguienteDisparo)
        {
            Disparar();
            siguienteDisparo = Time.time + CadenciaActual;
        }
    }

    private void Disparar()
    {
        Vector3 mouseMundo = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseMundo.z = 0f;

        Vector2 direccion = mouseMundo - transform.position;

        GameObject bala = Instantiate(balaPrefab, transform.position, Quaternion.identity);
        bala.GetComponent<Bullet>().Disparar(direccion);
    }
}
