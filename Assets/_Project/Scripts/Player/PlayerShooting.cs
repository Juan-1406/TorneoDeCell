using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [SerializeField] private GameObject balaPrefab;
    [SerializeField] private float cadencia = 0.2f;

    private Camera cam;
    private float siguienteDisparo;

    private void Awake()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        if (Input.GetButton("Fire1") && Time.time >= siguienteDisparo)
        {
            Disparar();
            siguienteDisparo = Time.time + cadencia;
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