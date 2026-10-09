using System.Collections;
using UnityEngine;

public class ParpadeoDanio : MonoBehaviour
{
    [SerializeField] private Health vida;
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private float duracion = 1f;
    [SerializeField] private float intervalo = 0.1f;

    private void OnEnable()
    {
        vida.OnDanioRecibido += Iniciar;
    }

    private void OnDisable()
    {
        if (vida != null) vida.OnDanioRecibido -= Iniciar;
    }

    private void Iniciar()
    {
        StopAllCoroutines();
        StartCoroutine(Parpadear());
    }

    private IEnumerator Parpadear()
    {
        float fin = Time.time + duracion;
        while (Time.time < fin)
        {
            sprite.enabled = !sprite.enabled;
            yield return new WaitForSeconds(intervalo);
        }
        sprite.enabled = true;
    }
}