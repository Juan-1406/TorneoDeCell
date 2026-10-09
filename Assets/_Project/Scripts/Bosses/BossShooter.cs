using UnityEngine;

public class BossShooter : MonoBehaviour
{
    [SerializeField] private GameObject balaPrefab;
    [SerializeField] private float intervalo = 1.5f;

    private Transform jugador;
    private float siguienteDisparo;

    private void Start()
    {
        GameObject objetoJugador = GameObject.FindGameObjectWithTag("Player");
        if (objetoJugador != null) jugador = objetoJugador.transform;

        siguienteDisparo = Time.time + intervalo;
    }

    private void Update()
    {
        if (jugador == null) return;

        if (Time.time >= siguienteDisparo)
        {
            Disparar();
            siguienteDisparo = Time.time + intervalo;
        }
    }

    private void Disparar()
    {
        Vector2 direccion = jugador.position - transform.position;

        GameObject bala = Instantiate(balaPrefab, transform.position, Quaternion.identity);
        bala.GetComponent<Bullet>().Disparar(direccion);
    }
}