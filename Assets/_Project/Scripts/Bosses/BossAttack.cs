using System.Collections;
using UnityEngine;

public abstract class BossAttack : MonoBehaviour
{
    [SerializeField] protected GameObject balaPrefab;

    public abstract IEnumerator Ejecutar(Transform objetivo);

    protected void Disparar(Vector2 direccion)
    {
        GameObject bala = Instantiate(balaPrefab, transform.position, Quaternion.identity);
        bala.GetComponent<Bullet>().Disparar(direccion);
    }
}