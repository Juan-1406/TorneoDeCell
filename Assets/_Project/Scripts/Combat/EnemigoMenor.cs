using System;
using UnityEngine;

[RequireComponent(typeof(Health), typeof(Rigidbody2D), typeof(CircleCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class EnemigoMenor : MonoBehaviour
{
    [SerializeField, Min(0f)] private float velocidad = 2.4f;
    [SerializeField, Min(0.1f)] private float distanciaAtaque = 1.3f;
    [SerializeField, Min(0.1f)] private float intervaloAtaque = 1.4f;
    [SerializeField, Min(1)] private int danio = 1;

    private static Sprite figuraProvisional;
    private Health vida;
    private Health jugador;
    private Rigidbody2D cuerpo;
    private float siguienteAtaque;

    public event Action<EnemigoMenor> Eliminado;
    public Health Vida => vida;

    private void Awake()
    {
        vida = GetComponent<Health>();
        cuerpo = GetComponent<Rigidbody2D>();
        vida.OnMuerte += AlMorir;

        var imagen = GetComponent<SpriteRenderer>();
        if (imagen.sprite == null) imagen.sprite = ObtenerFiguraProvisional();
    }

    private void OnDestroy()
    {
        if (vida != null) vida.OnMuerte -= AlMorir;
    }

    public void Configurar(Health objetivo)
    {
        jugador = objetivo;
    }

    private void FixedUpdate()
    {
        if (jugador == null || jugador.VidaActual <= 0 || vida.VidaActual <= 0) return;

        Vector2 haciaJugador = (Vector2)jugador.transform.position - cuerpo.position;
        if (haciaJugador.sqrMagnitude > distanciaAtaque * distanciaAtaque)
        {
            cuerpo.MovePosition(Vector2.MoveTowards(cuerpo.position,
                jugador.transform.position, velocidad * Time.fixedDeltaTime));
            return;
        }

        if (Time.time < siguienteAtaque) return;
        siguienteAtaque = Time.time + intervaloAtaque;
        jugador.RecibirDanio(danio);
    }

    private void AlMorir()
    {
        Eliminado?.Invoke(this);
    }

    private static Sprite ObtenerFiguraProvisional()
    {
        if (figuraProvisional != null) return figuraProvisional;

        var textura = new Texture2D(16, 16, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = "Figura temporal de enemigo menor"
        };
        var pixeles = new Color[16 * 16];
        for (int i = 0; i < pixeles.Length; i++) pixeles[i] = Color.white;
        textura.SetPixels(pixeles);
        textura.Apply();
        figuraProvisional = Sprite.Create(textura, new Rect(0, 0, 16, 16),
            new Vector2(0.5f, 0.5f), 16f);
        figuraProvisional.name = "Figura temporal de enemigo menor";
        return figuraProvisional;
    }
}
