using System.Collections;
using UnityEngine;

public abstract class BossAttack : MonoBehaviour
{
    [SerializeField] protected GameObject balaPrefab;

    public abstract IEnumerator Ejecutar(Transform objetivo);

    public virtual void Cancelar() { }

    protected void Disparar(Vector2 direccion)
    {
        DispararDesde(transform.position, direccion);
    }

    protected void DispararDesde(Vector2 origen, Vector2 direccion)
    {
        if (balaPrefab == null || direccion.sqrMagnitude < 0.001f) return;

        GameObject bala = Instantiate(balaPrefab, origen, Quaternion.identity);
        bala.GetComponent<Bullet>().Disparar(direccion);
    }
}
