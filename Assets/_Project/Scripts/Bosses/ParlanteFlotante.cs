using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public sealed class ParlanteFlotante : MonoBehaviour
{
    [SerializeField] private Sprite encendido;
    [SerializeField] private Vector2 desplazamientoEntrada = Vector2.left * 12f;
    [SerializeField, Min(0f)] private float retraso = 2f;
    [SerializeField, Min(0.1f)] private float duracionEntrada = 1.2f;

    private IEnumerator Start()
    {
        Vector3 destino = transform.position;
        Vector3 entrada = destino + (Vector3)desplazamientoEntrada;
        SpriteRenderer imagen = GetComponent<SpriteRenderer>();
        BoxCollider2D superficie = GetComponent<BoxCollider2D>();
        transform.position = entrada;
        imagen.enabled = false;
        superficie.enabled = false;
        yield return new WaitForSeconds(retraso);

        imagen.enabled = true;
        float inicio = Time.time;
        while (Time.time - inicio < duracionEntrada)
        {
            float avance = Mathf.SmoothStep(0f, 1f, (Time.time - inicio) / duracionEntrada);
            transform.position = Vector3.Lerp(entrada, destino, avance);
            yield return null;
        }
        // La superficie queda fija desde el primer momento en que admite pasajeros.
        transform.position = destino;
        superficie.enabled = true;
        imagen.sprite = encendido;
    }
}
