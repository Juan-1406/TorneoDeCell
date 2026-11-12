using System.Collections.Generic;
using UnityEngine;

// El barrido permite colisionar con enemigos sin cambiar la matriz de capas del juego.
public sealed class ProyectilRenato : MonoBehaviour
{
    private static readonly Color Oro = new Color(1f, 0.7f, 0.12f);
    private static readonly Color RosadoParry = new Color(1f, 0.32f, 0.72f);
    private static readonly Color Devuelto = new Color(0.25f, 1f, 1f);
    private readonly List<RaycastHit2D> impactos = new List<RaycastHit2D>();
    private readonly List<Collider2D> vecinos = new List<Collider2D>();
    private readonly HashSet<Health> alcanzados = new HashSet<Health>();
    private SegundaFaseRenato origen;
    private SpriteRenderer imagen;
    private ContactFilter2D filtro;
    private Vector2 velocidad;
    private float vence, radio;
    private int danio, danioDevuelto, danioEnemigos;
    private bool devuelto, resuelto;

    internal void Configurar(SegundaFaseRenato jefe, Rigidbody2D rb, SpriteRenderer sprite,
        Vector2 direccion, float rapidez, int danioBase, int retorno, int aEnemigos, float radioExplosion)
    {
        origen = jefe;
        imagen = sprite;
        rb.simulated = false;
        imagen.sprite = FeedbackCombate.Aro;
        imagen.color = GetComponent<Bullet>().EsReflejable ? RosadoParry : Oro;
        filtro.SetLayerMask(LayerMask.GetMask("Player", "Enemy", "Ground"));
        filtro.useTriggers = true;
        velocidad = direccion.normalized * rapidez;
        danio = danioBase;
        danioDevuelto = retorno;
        danioEnemigos = aEnemigos;
        radio = radioExplosion;
        vence = Time.time + 6f;
    }

    internal void Devolver(Vector2 direccion)
    {
        devuelto = true;
        if (direccion.sqrMagnitude < 0.001f) direccion = -velocidad;
        velocidad = direccion.normalized * velocidad.magnitude * 1.45f;
        imagen.color = Devuelto;
        vence = Time.time + 6f;
        FeedbackCombate.Emitir(this, Devuelto, 10, 3f);
    }

    private void FixedUpdate()
    {
        if (resuelto) return;
        if (Time.time >= vence || origen == null || !origen.CombateTransformado)
        {
            Retirar();
            return;
        }

        Vector2 salida = transform.position;
        if (origen.FueraDeArena(salida))
        {
            Retirar();
            return;
        }
        Vector2 avance = velocidad * Time.fixedDeltaTime;
        Physics2D.CircleCast(salida, 0.26f, avance.normalized, filtro, impactos, avance.magnitude);
        foreach (RaycastHit2D impacto in impactos)
        {
            Collider2D colision = impacto.collider;
            Health vida = colision.GetComponentInParent<Health>();
            bool esJefe = vida == origen.Vida;
            bool jugador = vida != null && vida.CompareTag("Player");
            bool enemigo = vida != null && vida.TryGetComponent<EnemigoMenor>(out _);
            bool suelo = colision.gameObject.layer == LayerMask.NameToLayer("Ground") && !colision.isTrigger;
            if (esJefe && !devuelto || jugador && devuelto) continue;
            if (!esJefe && !jugador && !enemigo && !suelo) continue;
            if (vida != null && vida.VidaActual <= 0) continue;

            transform.position = impacto.centroid;
            if (jugador && vida.TryGetComponent(out PlayerParry parry) &&
                parry.IntentarParry(GetComponent<Bullet>())) return;

            Vector2 punto = impacto.distance > 0f ? impacto.point : colision.ClosestPoint(salida);
            Explotar(punto, vida);
            return;
        }

        transform.position = salida + avance;
    }

    private void Explotar(Vector2 punto, Health directo)
    {
        resuelto = true;
        transform.position = punto;
        FeedbackCombate.Emitir(this, devuelto ? Devuelto : Oro, 16, 5f, 0.08f);
        if (directo != null)
        {
            alcanzados.Add(directo);
            if (directo == origen.Vida) origen.RecibirEnergiaDevuelta(danioDevuelto);
            else directo.RecibirDanio(directo.CompareTag("Player") ? danio : danioEnemigos);
        }

        if (origen.CombateTransformado)
        {
            Physics2D.OverlapCircle(punto, radio, filtro, vecinos);
            foreach (Collider2D colision in vecinos)
            {
                Health vida = colision.GetComponentInParent<Health>();
                if (vida == null || vida == origen.Vida || !alcanzados.Add(vida)) continue;
                if (vida.TryGetComponent<EnemigoMenor>(out _)) vida.RecibirDanio(danioEnemigos);
                else if (!devuelto && vida.CompareTag("Player")) vida.RecibirDanio(danio);
                if (!origen.CombateTransformado) break;
            }
        }
        Retirar();
    }

    private void Retirar()
    {
        resuelto = true;
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
