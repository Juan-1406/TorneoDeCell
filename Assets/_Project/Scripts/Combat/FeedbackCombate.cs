using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Efectos procedurales compartidos. El pool limita el coste de las bajas simultáneas.
public class FeedbackCombate : MonoBehaviour
{
    private class Chispa
    {
        public SpriteRenderer imagen;
        public Vector2 velocidad;
        public float restante, total, escala;
        public Color color;
        public bool anillo;
    }
    private static FeedbackCombate actual;
    private static Sprite punto, aro;
    private readonly List<Chispa> particulas = new List<Chispa>();
    private CinemachineImpulseSource impulso;
    private float siguienteShake;
    private bool climax;
    private GameObject marco;
    private readonly List<Image> bordes = new List<Image>();
    public static Sprite Punto => punto != null ? punto : punto = Sprite.Create(
        Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
    public static Sprite Aro
    {
        get
        {
            if (aro != null) return aro;
            var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var pixels = new Color[4096];
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                float r = Vector2.Distance(new Vector2(x, y), Vector2.one * 31.5f);
                pixels[y * 64 + x] = r >= 26f && r <= 30f ? Color.white : Color.clear;
            }
            tex.SetPixels(pixels); tex.Apply();
            return aro = Sprite.Create(tex, new Rect(0, 0, 64, 64), Vector2.one * 0.5f, 64f);
        }
    }
    public static FeedbackCombate Obtener(Scene escena)
    {
        if (actual != null && actual.gameObject.scene == escena) return actual;
        var go = new GameObject("Feedback de combate");
        SceneManager.MoveGameObjectToScene(go, escena);
        actual = go.AddComponent<FeedbackCombate>();
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) actual.impulso = player.GetComponent<CinemachineImpulseSource>();
        return actual;
    }
    public static void Emitir(Component origen, Color color, int cantidad = 8, float fuerza = 2f, float sacudida = 0f)
    {
        var host = Obtener(origen.gameObject.scene);
        for (int i = 0; i < cantidad; i++) host.Crear(origen.transform.position, color, fuerza, false);
        host.Crear(origen.transform.position, color, 0f, true);
        if (sacudida > 0f && Time.time >= host.siguienteShake && host.impulso != null)
        {
            host.siguienteShake = Time.time + 0.08f;
            host.impulso.GenerateImpulse(Mathf.Min(0.12f, sacudida));
        }
    }
    public static void Presion(Scene escena, bool activa)
    {
        if (activa) Obtener(escena).climax = true;
        else if (actual != null && actual.gameObject.scene == escena) actual.climax = false;
    }
    private void Crear(Vector3 posicion, Color color, float fuerza, bool anillo)
    {
        Chispa p = particulas.Find(x => x.restante <= 0f);
        if (p == null)
        {
            if (particulas.Count >= 120) return;
            var go = new GameObject("Chispa");
            go.transform.SetParent(transform);
            p = new Chispa { imagen = go.AddComponent<SpriteRenderer>() };
            p.imagen.sortingOrder = 24;
            particulas.Add(p);
        }
        p.imagen.gameObject.SetActive(true);
        p.imagen.sprite = anillo ? Aro : Punto;
        p.imagen.transform.position = posicion;
        p.velocidad = Random.insideUnitCircle.normalized * fuerza * Random.Range(0.5f, 1f);
        p.total = p.restante = anillo ? 0.32f : Random.Range(0.2f, 0.45f);
        p.escala = anillo ? 0.6f : Random.Range(0.07f, 0.15f);
        p.color = color; p.anillo = anillo;
        p.imagen.color = color;
        p.imagen.transform.localScale = Vector3.one * p.escala;
    }
    private void Update()
    {
        if (climax && marco == null) CrearMarco();
        if (marco != null)
        {
            marco.SetActive(climax && !SesionJuego.EnPausa);
            foreach (var borde in bordes) borde.color = new Color(1f, 0.18f, 0.12f,
                0.05f + 0.025f * Mathf.Sin(Time.time * 2.5f));
        }
        if (SesionJuego.EnPausa) return;
        foreach (var p in particulas)
        {
            if (p.restante <= 0f) continue;
            p.restante -= Time.deltaTime;
            float t = Mathf.Clamp01(p.restante / p.total);
            p.imagen.transform.position += (Vector3)(p.velocidad * Time.deltaTime);
            p.velocidad *= Mathf.Exp(-5f * Time.deltaTime);
            p.imagen.transform.localScale = Vector3.one * (p.anillo ? p.escala + (1f - t) * 1.5f : p.escala * t);
            Color c = p.color; c.a *= t; p.imagen.color = c;
            if (p.restante <= 0f) p.imagen.gameObject.SetActive(false);
        }
    }
    private void CrearMarco()
    {
        marco = new GameObject("Marco clímax", typeof(Canvas));
        marco.transform.SetParent(transform, false);
        var canvas = marco.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = -10;
        for (int i = 0; i < 4; i++)
        {
            var go = new GameObject("Borde", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(marco.transform, false);
            var r = go.GetComponent<RectTransform>();
            bool horizontal = i < 2;
            r.anchorMin = horizontal ? new Vector2(0, i) : new Vector2(i - 2, 0);
            r.anchorMax = horizontal ? new Vector2(1, i) : new Vector2(i - 2, 1);
            r.pivot = horizontal ? new Vector2(0.5f, i) : new Vector2(i - 2, 0.5f);
            r.anchoredPosition = Vector2.zero;
            r.sizeDelta = horizontal ? new Vector2(0, 12) : new Vector2(12, 0);
            var imagen = go.GetComponent<Image>(); imagen.raycastTarget = false; bordes.Add(imagen);
        }
    }
    private void OnDestroy() { if (actual == this) actual = null; }
}

