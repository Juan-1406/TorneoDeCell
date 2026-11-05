using System.Collections;
using UnityEngine;

public abstract class BossAttack : MonoBehaviour
{
    [SerializeField] protected GameObject balaPrefab;
    public event System.Action<Bullet> ProyectilEmitido;
    private PresentacionRenato presentacion;
    protected PresentacionRenato Presentacion => presentacion != null ? presentacion : presentacion = GetComponentInParent<PresentacionRenato>();
    protected Vector2 OrigenDisparo => Presentacion != null ? Presentacion.OrigenDisparo : (Vector2)transform.position;

    public abstract IEnumerator Ejecutar(Transform objetivo);

    public virtual void Cancelar() { }

    protected void Disparar(Vector2 direccion)
    {
        DispararDesde(OrigenDisparo, direccion);
    }

    protected void DispararDesde(Vector2 origen, Vector2 direccion)
    {
        if (balaPrefab == null || direccion.sqrMagnitude < 0.001f) return;

        GameObject bala = Instantiate(balaPrefab, origen, Quaternion.identity);
        var proyectil = bala.GetComponent<Bullet>();
        proyectil.Disparar(direccion);
        if (Presentacion != null) Presentacion.PulsoDisparo(!(this is AtaqueExterno));
        ProyectilEmitido?.Invoke(proyectil);
    }
}
