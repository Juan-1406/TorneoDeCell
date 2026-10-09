using System.Collections;
using UnityEngine;

public class AtaqueRafaga : BossAttack
{
    [SerializeField] private int cantidad = 3;
    [SerializeField] private float intervalo = 0.3f;

    public override IEnumerator Ejecutar(Transform objetivo)
    {
        for (int i = 0; i < cantidad; i++)
        {
            if (objetivo == null) yield break;

            Disparar(objetivo.position - transform.position);
            yield return new WaitForSeconds(intervalo);
        }
    }
}