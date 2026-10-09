using System.Collections;
using UnityEngine;

public class AtaqueAbanico : BossAttack
{
    [SerializeField] private int cantidad = 5;
    [SerializeField] private float anguloTotal = 50f;
    [SerializeField] private float tiempoRecuperacion = 0.4f;

    public override IEnumerator Ejecutar(Transform objetivo)
    {
        if (objetivo == null) yield break;

        Vector2 direccionBase = objetivo.position - transform.position;

        for (int i = 0; i < cantidad; i++)
        {
            float t = cantidad == 1 ? 0.5f : i / (float)(cantidad - 1);
            float angulo = Mathf.Lerp(-anguloTotal / 2f, anguloTotal / 2f, t);
            Vector2 direccion = Quaternion.Euler(0f, 0f, angulo) * direccionBase;
            Disparar(direccion);
        }

        yield return new WaitForSeconds(tiempoRecuperacion);
    }
}