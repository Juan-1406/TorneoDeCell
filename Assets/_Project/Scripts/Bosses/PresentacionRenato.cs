using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PresentacionRenato : MonoBehaviour
{
    [System.Serializable]
    public class PoseRenato
    {
        public Sprite sprite;
        public Vector2 origenNasal;
        public Vector2 centroVulnerable;
        public Vector2 tamanoVulnerable;
    }
    public enum EstadoVisual { Reposo, Movimiento, Preparacion, Ataque, Recuperacion, CambioFase, Derrota }
    [SerializeField] private Health vida;
    [SerializeField] private BossController controlador;
    [SerializeField] private SeguirCamaraX movimiento;
    [SerializeField] private SpriteRenderer cuerpo;
    [SerializeField] private Animator animador;
    [SerializeField] private Transform orientacion;
    [SerializeField] private Transform reaccion;
    [SerializeField] private Transform puntoDisparo;
    [SerializeField] private SpriteRenderer sombra;
    [SerializeField] private BoxCollider2D hitbox;
    [SerializeField] private PoseRenato[] poses;
    [SerializeField, Min(0.5f)] private float escalaBase = 2f;
    [SerializeField] private bool medioCuerpo;
    [SerializeField] private float bordePrimerPlanoY = -2.376f;
    private EstadoVisual estado;
    private string animacion = "Reposo";
    private int fase, signo = 1;
    private float escala, objetivoEscala = 1f, golpeHasta, restaurarAnimacion;
    private bool muriendo;
    private float derrotaInicio;
    public EstadoVisual Estado => estado;
    public SpriteRenderer Cuerpo => cuerpo;
    public Animator Animador => animador;
    public int Orientacion => signo;
    public Vector2 OrigenDisparo => puntoDisparo.position;
    public float RadioVisual => 2.15f * Mathf.Max(1f, escala);
    public float AlturaVisual => 3.75f * Mathf.Max(1f, escala);
    public bool MedioCuerpo => medioCuerpo;
    public float BordePrimerPlanoY => bordePrimerPlanoY;
    public bool Visible => cuerpo != null && cuerpo.enabled;

    private void Awake()
    {
        escala = escalaBase;
        if (vida == null) vida = GetComponent<Health>();
        if (controlador == null) controlador = GetComponent<BossController>();
        if (movimiento == null) movimiento = GetComponent<SeguirCamaraX>();
        if (hitbox == null) hitbox = GetComponent<BoxCollider2D>();
        if (sombra != null && sombra.sprite == null)
        {
            var textura = new Texture2D(32, 32, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var pixeles = new Color[1024];
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
                pixeles[y * 32 + x] = Vector2.Distance(new Vector2(x, y), Vector2.one * 15.5f) < 15f ? Color.white : Color.clear;
            textura.SetPixels(pixeles); textura.Apply();
            sombra.sprite = Sprite.Create(textura, new Rect(0, 0, 32, 32), Vector2.one * 0.5f, 32f);
        }
    }
    private void OnEnable()
    {
        vida.OnDanioRecibido += ReaccionarDanio;
        vida.OnMuerte += Morir;
    }
    private void OnDisable()
    {
        if (vida != null) { vida.OnDanioRecibido -= ReaccionarDanio; vida.OnMuerte -= Morir; }
        StopAllCoroutines();
    }
    private void Animar(string nombre, EstadoVisual nuevo)
    {
        estado = nuevo; animacion = nombre;
        restaurarAnimacion = 0f;
        if (animador != null) { animador.enabled = true; animador.Play(nombre, 0, 0f); }
    }
    public IEnumerator Entrada()
    {
        Animar("Entrada", EstadoVisual.CambioFase);
        yield return new WaitForSeconds(0.85f);
        Reposo();
    }
    public IEnumerator GirarHacia(float x)
    {
        int nuevo = x > transform.position.x ? -1 : 1;
        if (nuevo == signo) yield break;
        Animar("Giro", EstadoVisual.Movimiento);
        yield return new WaitForSeconds(0.12f);
        signo = nuevo;
        yield return new WaitForSeconds(0.16f);
    }
    public void Traslado() { objetivoEscala = 0.96f; Animar("Traslado", EstadoVisual.Movimiento); }
    public void Reposo() { objetivoEscala = 1f; Animar("Reposo", EstadoVisual.Reposo); }
    public float Preparar(BossAttack ataque)
    {
        bool abanico = ataque is AtaqueAbanico;
        objetivoEscala = abanico ? 1.14f : ataque is AtaqueRafaga ? 0.98f : 0.94f;
        Animar(abanico ? "PrepararAbanico" : ataque is AtaqueRafaga ? "PrepararRafaga" : "PrepararExterno",
            EstadoVisual.Preparacion);
        return abanico ? 0.8f : ataque is AtaqueRafaga ? 0.65f : 0.5f;
    }
    public void IniciarAtaque(BossAttack ataque)
    {
        Animar(ataque is AtaqueAbanico ? "DisparoAbanico" : ataque is AtaqueRafaga ? "DisparoRafaga" : "DisparoExterno", EstadoVisual.Ataque);
    }
    public void PulsoDisparo(bool nasal)
    {
        if (muriendo) return;
        if (nasal && estado == EstadoVisual.Ataque)
            animador.Play(animacion, 0, 0f);
        // El retroceso no desplaza la raíz ni altera la dirección del patrón.
        reaccion.localPosition = new Vector3(0.07f * signo, 0.02f, 0f);
    }
    public void Recuperar()
    {
        objetivoEscala = 1f;
        Animar("Recuperacion", EstadoVisual.Recuperacion);
    }
    public void EstablecerFase(int indice) { fase = Mathf.Clamp(indice, 0, 2); }
    public IEnumerator CambioFase(int indice)
    {
        EstablecerFase(indice);
        objetivoEscala = 1.08f;
        Animar("CambioFase", EstadoVisual.CambioFase);
        FeedbackCombate.Emitir(puntoDisparo, new Color(1f, 0.4f, 0.2f), 12, 2.5f, 0.08f);
        yield return new WaitForSeconds(0.85f);
        Reposo();
    }
    private void ReaccionarDanio()
    {
        golpeHasta = Time.time + 0.12f;
        reaccion.localPosition = new Vector3(0.1f * signo, 0.04f, 0f);
        if (estado == EstadoVisual.Reposo || estado == EstadoVisual.Movimiento)
        {
            animador.Play("Danio", 0, 0);
            restaurarAnimacion = Time.time + 0.2f;
        }
        FeedbackCombate.Emitir(puntoDisparo, new Color(1f, 0.65f, 0.45f), 4, 2f);
    }
    private void Morir()
    {
        muriendo = true; derrotaInicio = Time.time;
        if (hitbox != null) hitbox.enabled = false;
        if (movimiento != null) movimiento.enabled = false;
        Animar("Derrota", EstadoVisual.Derrota);
        FeedbackCombate.Emitir(puntoDisparo, new Color(1f, 0.4f, 0.25f), 16, 4f, 0.1f);
        Destroy(gameObject, 0.7f);
    }
    private void LateUpdate()
    {
        bool visible = muriendo || (controlador != null && controlador.enabled && vida.VidaActual > 0);
        cuerpo.enabled = visible;
        sombra.enabled = visible;
        animador.enabled = visible;
        if (!visible || SesionJuego.EnPausa || Time.timeScale == 0f) return;
        if (restaurarAnimacion > 0f && Time.time >= restaurarAnimacion)
        {
            restaurarAnimacion = 0;
            animador.Play(animacion, 0, 0);
        }
        float deseada = escalaBase * (1f + fase * 0.035f) * objetivoEscala;
        Camera cam = Camera.main;
        if (cam != null && cam.orthographic)
        {
            float espacio = cam.transform.position.y + cam.orthographicSize - transform.position.y - 0.65f;
            deseada = Mathf.Min(deseada, espacio / 4.05f);
        }
        escala = Mathf.MoveTowards(escala, deseada, Time.deltaTime * 1.1f);
        orientacion.localScale = new Vector3(signo * escala, escala, 1f);
        reaccion.localPosition = Vector3.Lerp(reaccion.localPosition, Vector3.zero, 15f * Time.deltaTime);
        Color tono = Color.Lerp(Color.white, new Color(1f, 0.8f, 0.72f), fase * 0.2f);
        cuerpo.color = Time.time < golpeHasta ? new Color(1f, 0.92f, 0.82f) : tono;
        if (muriendo)
        {
            Color c = cuerpo.color; c.a = 1f - Mathf.Clamp01((Time.time - derrotaInicio) / 0.7f); cuerpo.color = c;
            sombra.color = new Color(0, 0, 0, c.a * 0.25f);
        }
        if (movimiento != null)
        {
            Vector3 p = sombra.transform.position;
            p.y = movimiento.SueloY - 0.1f; sombra.transform.position = p;
            sombra.transform.localScale = new Vector3(3.3f * escala, 0.18f, 1f);
        }
        ActualizarGeometria();
    }
    private void FixedUpdate() { if (cuerpo != null) ActualizarGeometria(); }
    private void ActualizarGeometria()
    {
        if (poses == null || poses.Length < 9 || cuerpo.sprite == null) return;
        PoseRenato pose = null;
        foreach (var configuracion in poses)
            if (configuracion != null && configuracion.sprite == cuerpo.sprite) { pose = configuracion; break; }
        if (pose == null) return;
        puntoDisparo.position = cuerpo.transform.TransformPoint(pose.origenNasal);
        // Caja vulnerable del torso, alineada con la pose, independiente de brazos y píxeles vacíos.
        Vector3 c = transform.InverseTransformPoint(cuerpo.transform.TransformPoint(pose.centroVulnerable));
        Vector3 ejeX = transform.InverseTransformVector(cuerpo.transform.TransformVector(Vector3.right * pose.tamanoVulnerable.x));
        Vector3 ejeY = transform.InverseTransformVector(cuerpo.transform.TransformVector(Vector3.up * pose.tamanoVulnerable.y));
        Vector2 tamano = new Vector2(Mathf.Abs(ejeX.x) + Mathf.Abs(ejeY.x), Mathf.Abs(ejeX.y) + Mathf.Abs(ejeY.y));
        // El torso oculto detrás del borde de la cancha no recibe impactos invisibles.
        if (medioCuerpo)
        {
            float inferior = Mathf.Max(c.y - tamano.y * 0.5f, transform.InverseTransformPoint(new Vector3(0f, bordePrimerPlanoY + 0.02f, 0f)).y);
            float superior = c.y + tamano.y * 0.5f;
            tamano.y = Mathf.Max(0.01f, superior - inferior);
            c.y = inferior + tamano.y * 0.5f;
        }
        hitbox.offset = new Vector2(c.x, c.y);
        hitbox.size = tamano;
    }
}

