using UnityEngine;

[RequireComponent(typeof(CircleCollider2D), typeof(SpriteRenderer))]
public class RecogibleCombate : MonoBehaviour
{
    public enum TipoRecompensa { Curacion, Cadencia }

    [SerializeField] private TipoRecompensa tipo;
    [SerializeField, Min(1)] private int puntosCuracion = 1;
    [SerializeField, Range(0.1f, 0.99f)] private float multiplicadorCadencia = 0.65f;
    [SerializeField, Min(0.1f)] private float duracionMejora = 8f;
    [SerializeField, Min(0.1f)] private float tiempoEnSuelo = 15f;

    private static Sprite figuraProvisional;
    private CircleCollider2D zona;
    private bool consumido;

    private void Awake()
    {
        zona = GetComponent<CircleCollider2D>();
        zona.isTrigger = true;
        var imagen = GetComponent<SpriteRenderer>();
        if (imagen.sprite == null) imagen.sprite = ObtenerFiguraProvisional();
        Destroy(gameObject, tiempoEnSuelo);
    }

    private void Update()
    {
        if (consumido) return;
        float radio = zona.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
        Collider2D jugador = Physics2D.OverlapCircle(transform.position, radio,
            1 << LayerMask.NameToLayer("Player"));
        if (jugador != null) IntentarRecoger(jugador);
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        IntentarRecoger(otro);
    }

    private void IntentarRecoger(Collider2D otro)
    {
        if (consumido || otro.gameObject.layer != LayerMask.NameToLayer("Player")) return;

        bool recogido = false;
        if (tipo == TipoRecompensa.Curacion)
        {
            Health vida = otro.GetComponent<Health>();
            if (vida != null) recogido = vida.Curar(puntosCuracion);
        }
        else
        {
            PlayerShooting disparo = otro.GetComponent<PlayerShooting>();
            if (disparo != null && disparo.isActiveAndEnabled)
            {
                disparo.MejorarCadenciaTemporal(multiplicadorCadencia, duracionMejora);
                recogido = true;
            }
        }

        if (recogido)
        {
            consumido = true;
            Destroy(gameObject);
        }
    }

    private static Sprite ObtenerFiguraProvisional()
    {
        if (figuraProvisional != null) return figuraProvisional;
        var textura = new Texture2D(16, 16, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = "Figura temporal de recompensa"
        };
        var pixeles = new Color[16 * 16];
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
                pixeles[y * 16 + x] = (x - 7.5f) * (x - 7.5f) +
                    (y - 7.5f) * (y - 7.5f) <= 49f ? Color.white : Color.clear;
        textura.SetPixels(pixeles);
        textura.Apply();
        figuraProvisional = Sprite.Create(textura, new Rect(0, 0, 16, 16),
            new Vector2(0.5f, 0.5f), 16f);
        figuraProvisional.name = "Figura temporal de recompensa";
        return figuraProvisional;
    }
}
