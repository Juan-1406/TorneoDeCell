using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [SerializeField] private GameObject balaPrefab;
    [SerializeField] private float cadencia = 0.2f;

    private Camera cam;
    private float siguienteDisparo;
    private float mejoraHasta;
    private float multiplicadorCadencia = 1f;

    public void MejorarCadenciaTemporal(float multiplicador, float duracion)
    {
        if (multiplicador <= 0f || multiplicador >= 1f || duracion <= 0f) return;

        multiplicadorCadencia = Time.time < mejoraHasta
            ? Mathf.Min(multiplicadorCadencia, multiplicador) : multiplicador;
        mejoraHasta = Time.time + duracion;
        siguienteDisparo = Mathf.Min(siguienteDisparo, Time.time + cadencia * multiplicadorCadencia);
    }

    private void Awake()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        if (SesionJuego.EnPausa) return;
        if (Input.GetButton("Fire1") && Time.time >= siguienteDisparo)
        {
            Disparar();
            float factor = Time.time < mejoraHasta ? multiplicadorCadencia : 1f;
            siguienteDisparo = Time.time + cadencia * factor;
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