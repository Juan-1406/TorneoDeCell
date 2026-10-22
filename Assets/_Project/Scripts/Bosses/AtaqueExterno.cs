using System.Collections;
using UnityEngine;

public class AtaqueExterno : BossAttack
{
    public enum PatronExterno { Lateral, Diagonal }

    [SerializeField] private PatronExterno patron;
    [SerializeField] private float tiempoAviso = 0.8f;
    [SerializeField] private int cantidad = 2;
    [SerializeField] private float intervalo = 0.4f;
    [SerializeField] private float recuperacion = 0.3f;
    [SerializeField] private float alturaDiagonal = 3.5f;
    [SerializeField] private float limiteHorizontal = 8.1f;
    [SerializeField] private float alturaMinima = -2.8f;
    [SerializeField] private float margenCamara = 0.7f;

    public override IEnumerator Ejecutar(Transform objetivo)
    {
        if (objetivo == null || balaPrefab == null) yield break;

        // Se fija el objetivo antes de la preparacion: el jugador puede esquivar la trayectoria.
        Vector2 destino = objetivo.position;
        Vector2 origen = CalcularOrigen(destino);
        Vector2 direccion = (destino - origen).normalized;
        if (direccion.sqrMagnitude < 0.001f) yield break;

        yield return new WaitForSeconds(Mathf.Max(0f, tiempoAviso));

        for (int i = 0; i < Mathf.Max(1, cantidad); i++)
        {
            DispararDesde(origen, direccion);
            if (i < cantidad - 1) yield return new WaitForSeconds(Mathf.Max(0f, intervalo));
        }

        if (recuperacion > 0f) yield return new WaitForSeconds(recuperacion);
    }

    private Vector2 CalcularOrigen(Vector2 destino)
    {
        float izquierda = -limiteHorizontal;
        float derecha = limiteHorizontal;
        float abajo = alturaMinima;
        float arriba = destino.y + alturaDiagonal;

        Camera camara = Camera.main;
        if (camara != null && camara.orthographic)
        {
            float profundidad = Mathf.Abs(camara.transform.position.z - transform.position.z);
            Vector3 esquinaInferior = camara.ViewportToWorldPoint(new Vector3(0f, 0f, profundidad));
            Vector3 esquinaSuperior = camara.ViewportToWorldPoint(new Vector3(1f, 1f, profundidad));
            izquierda = Mathf.Max(izquierda, esquinaInferior.x + margenCamara);
            derecha = Mathf.Min(derecha, esquinaSuperior.x - margenCamara);
            abajo = Mathf.Max(abajo, esquinaInferior.y + margenCamara);
            arriba = esquinaSuperior.y - margenCamara;
        }

        if (derecha <= izquierda)
        {
            izquierda = -limiteHorizontal;
            derecha = limiteHorizontal;
        }
        if (arriba < abajo) arriba = abajo;

        bool desdeIzquierda = Random.value < 0.5f;
        float x = desdeIzquierda ? izquierda : derecha;
        float otroX = desdeIzquierda ? derecha : izquierda;
        if (Mathf.Abs(x - destino.x) < 2.5f && Mathf.Abs(otroX - destino.x) > Mathf.Abs(x - destino.x))
            x = otroX;

        float y = patron == PatronExterno.Diagonal
            ? Mathf.Clamp(destino.y + alturaDiagonal, abajo, arriba)
            : Mathf.Clamp(destino.y, abajo, arriba);

        return new Vector2(x, y);
    }

}
