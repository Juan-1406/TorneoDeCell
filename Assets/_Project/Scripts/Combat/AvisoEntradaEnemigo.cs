using UnityEngine;

public class AvisoEntradaEnemigo : MonoBehaviour
{
    private SpriteRenderer aro, sombra, centro;
    private Color color;
    private float duracion, edad;
    private Transform grafica;
    private Camera cam;
    public void Configurar(Color tinte, float segundos)
    {
        color = tinte; duracion = Mathf.Max(0.1f, segundos);
        grafica = new GameObject("Mira visible").transform;
        grafica.SetParent(transform, false); cam = Camera.main;
        aro = Crear("Mira", FeedbackCombate.Aro, Vector3.zero, Vector3.one * 1.6f);
        sombra = Crear("Sombra", FeedbackCombate.Punto, Vector3.down * 0.55f, new Vector3(1.25f, 0.14f, 1));
        centro = Crear("Núcleo", FeedbackCombate.Punto, Vector3.zero, Vector3.one * 0.16f);
        for (int i = 0; i < 4; i++)
        {
            float a = i * Mathf.PI * 0.5f;
            var marca = Crear("Guía", FeedbackCombate.Punto, new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * 0.65f,
                i % 2 == 0 ? new Vector3(0.25f, 0.06f, 1) : new Vector3(0.06f, 0.25f, 1));
            marca.color = color;
        }
        Colocar();
    }
    private SpriteRenderer Crear(string nombre, Sprite sprite, Vector3 posicion, Vector3 escala)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(grafica, false);
        go.transform.localPosition = posicion; go.transform.localScale = escala;
        var r = go.AddComponent<SpriteRenderer>(); r.sprite = sprite; r.sortingOrder = 18; r.color = color;
        return r;
    }
    private void Update()
    {
        if (SesionJuego.EnPausa || aro == null) return;
        Colocar();
        edad += Time.deltaTime;
        float t = Mathf.Clamp01(edad / duracion);
        aro.transform.localScale = Vector3.one * Mathf.Lerp(1.9f, 0.95f, t);
        Color c = color; c.a = 0.45f + 0.5f * Mathf.Abs(Mathf.Sin(t * Mathf.PI * 3f));
        aro.color = c; centro.color = c;
        sombra.color = new Color(color.r * 0.25f, color.g * 0.25f, color.b * 0.25f, 0.6f);
    }
    private void Colocar()
    {
        if (cam == null || !cam.orthographic) return;
        float alto = cam.orthographicSize, ancho = alto * cam.aspect;
        Vector3 p = transform.position, c = cam.transform.position;
        p.x = Mathf.Clamp(p.x, c.x - ancho + 1f, c.x + ancho - 1f);
        p.y = Mathf.Clamp(p.y, c.y - alto + 1f, c.y + alto - 1f);
        grafica.position = p;
    }
}

