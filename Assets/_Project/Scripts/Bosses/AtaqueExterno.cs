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

    private GameObject aviso;
    private Material materialAviso;

    public override IEnumerator Ejecutar(Transform objetivo)
    {
        if (objetivo == null || balaPrefab == null) yield break;

        // Se fija el objetivo antes del aviso: el jugador puede esquivar la trayectoria.
        Vector2 destino = objetivo.position;
        Vector2 origen = CalcularOrigen(destino);
        Vector2 direccion = (destino - origen).normalized;
        if (direccion.sqrMagnitude < 0.001f) yield break;

        MostrarAviso(origen, destino);
        yield return new WaitForSeconds(Mathf.Max(0f, tiempoAviso));
        Cancelar();

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

    private void MostrarAviso(Vector2 origen, Vector2 destino)
    {
        Cancelar();
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            Debug.LogError("No se pudo mostrar el aviso del ataque externo.", this);
            return;
        }

        aviso = new GameObject("Aviso de ataque externo");
        aviso.transform.SetParent(transform, true);
        materialAviso = new Material(shader);
        LineRenderer linea = aviso.AddComponent<LineRenderer>();
        linea.material = materialAviso;
        linea.useWorldSpace = true;
        linea.positionCount = 2;
        linea.SetPosition(0, origen);
        linea.SetPosition(1, destino);
        linea.startWidth = 0.12f;
        linea.endWidth = 0.06f;
        linea.startColor = new Color(1f, 0.85f, 0f, 0.9f);
        linea.endColor = new Color(1f, 0.4f, 0f, 0.7f);
        linea.sortingOrder = 30;
    }

    public override void Cancelar()
    {
        if (aviso != null)
        {
            aviso.SetActive(false);
            Destroy(aviso);
            aviso = null;
        }
        if (materialAviso != null)
        {
            Destroy(materialAviso);
            materialAviso = null;
        }
    }

    private void OnDisable()
    {
        Cancelar();
    }
}
