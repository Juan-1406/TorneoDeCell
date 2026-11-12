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
        public Sprite brazoFrente;
        public bool palmaEnSuelo;
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
    private float escala, objetivoEscala = 1f, golpeHasta;
    private SpriteRenderer areaManotazo;
    private SpriteRenderer brazoManotazo;
    private Vector3 posicionOrientacion;
    private bool palmaAlineada;
    private bool muriendo;
    private float derrotaInicio;
    private SegundaFaseRenato segundaFase;
    private SpriteRenderer auraDorada;
    private Texture2D texturaAura;
    private float inicioAura;
    internal const float InstanteDisparoDorado = 0.48f;
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
    public Vector2 CentroManotazo => new Vector2(transform.position.x,
        medioCuerpo ? bordePrimerPlanoY : movimiento != null ? movimiento.SueloY : transform.position.y);

    private void Awake()
    {
        escala = escalaBase;
        if (vida == null) vida = GetComponent<Health>();
        if (controlador == null) controlador = GetComponent<BossController>();
        segundaFase = GetComponent<SegundaFaseRenato>();
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
        OcultarAreaManotazo();
        if (brazoManotazo != null) brazoManotazo.enabled = false;
        RestaurarApoyoManotazo();
        ApagarAuraDorada();
        StopAllCoroutines();
    }
    private void Animar(string nombre, EstadoVisual nuevo)
    {
        estado = nuevo; animacion = nombre;
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
    public IEnumerator ManoAlSuelo(Vector2 centro, float radio, float objetivoX, System.Action impacto)
    {
        const string nombre = "Manotazo";
        // RenatoManotazo_Contacto entra en el segundo 0.70 del clip de 1.15 segundos.
        const float puntoImpacto = 0.70f / 1.15f;
        int estadoManotazo = Animator.StringToHash(nombre);
        if (!Mathf.Approximately(objetivoX, centro.x)) signo = objetivoX > centro.x ? -1 : 1;
        Vector3 posicionImpacto = new Vector3(centro.x, centro.y, transform.position.z);
        MostrarAreaManotazo(posicionImpacto, radio);
        Animar(nombre, EstadoVisual.Ataque);
        yield return null;

        bool impactoAplicado = false;
        AnimatorStateInfo cuadro = animador.GetCurrentAnimatorStateInfo(0);
        while (cuadro.shortNameHash == estadoManotazo && !muriendo && isActiveAndEnabled)
        {
            if (!impactoAplicado)
            {
                AjustarAreaManotazo(posicionImpacto, radio);
                float avance = Mathf.Clamp01(cuadro.normalizedTime / puntoImpacto);
                areaManotazo.color = new Color(1f, Mathf.Lerp(0.65f, 0.28f, avance), 0.14f,
                    Mathf.Lerp(0.72f, 1f, avance));
            }
            if (!impactoAplicado && cuadro.normalizedTime >= puntoImpacto)
            {
                impactoAplicado = true;
                OcultarAreaManotazo();
                FeedbackCombate.Emitir(areaManotazo, new Color(1f, 0.62f, 0.2f), 16, 5f, 0.1f);
                impacto();
            }

            if (cuadro.normalizedTime >= 1f) break;
            yield return null;
            cuadro = animador.GetCurrentAnimatorStateInfo(0);
        }

        OcultarAreaManotazo();
        if (!muriendo && isActiveAndEnabled) Reposo();
    }

    private void MostrarAreaManotazo(Vector3 posicion, float radio)
    {
        if (areaManotazo == null)
        {
            var area = new GameObject("Área del manotazo");
            area.transform.SetParent(transform, false);
            areaManotazo = area.AddComponent<SpriteRenderer>();
            areaManotazo.sprite = FeedbackCombate.Aro;
            areaManotazo.sortingOrder = 24;
            areaManotazo.sortingLayerID = cuerpo.sortingLayerID;
        }
        AjustarAreaManotazo(posicion, radio);
        areaManotazo.color = new Color(1f, 0.65f, 0.14f, 0.72f);
        areaManotazo.enabled = true;
    }

    private void AjustarAreaManotazo(Vector3 posicion, float radio)
    {
        areaManotazo.transform.position = posicion;
        // El borde queda a 30 píxeles del centro de la textura de 64.
        float escala = radio * 64f / 30f;
        Vector3 escalaJefe = transform.lossyScale;
        areaManotazo.transform.localScale = new Vector3(
            escala / Mathf.Abs(escalaJefe.x), escala / Mathf.Abs(escalaJefe.y), 1f);
    }

    public void OcultarAreaManotazo()
    {
        if (areaManotazo != null) areaManotazo.enabled = false;
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
        if (controlador != null && controlador.EscudoActivo)
        {
            FeedbackCombate.Emitir(puntoDisparo, Color.cyan, 3, 2f);
            return;
        }

        golpeHasta = Time.time + 0.12f;
        reaccion.localPosition = new Vector3(0.1f * signo, 0.04f, 0f);
        FeedbackCombate.Emitir(puntoDisparo, new Color(1f, 0.65f, 0.45f), 4, 2f);
    }
    private void Morir()
    {
        if (segundaFase != null && segundaFase.ConservarAbatido) return;
        muriendo = true; derrotaInicio = Time.time;
        if (hitbox != null) hitbox.enabled = false;
        if (movimiento != null) movimiento.enabled = false;
        Animar("Derrota", EstadoVisual.Derrota);
        FeedbackCombate.Emitir(puntoDisparo, new Color(1f, 0.4f, 0.25f), 16, 4f, 0.1f);
        Destroy(gameObject, 0.7f);
    }
    private void LateUpdate()
    {
        bool visible = muriendo || (segundaFase != null && segundaFase.DurantePresentacion) ||
            (controlador != null && controlador.enabled && vida.VidaActual > 0);
        cuerpo.enabled = visible;
        sombra.enabled = visible;
        animador.enabled = visible;
        if (!visible)
        {
            if (brazoManotazo != null) brazoManotazo.enabled = false;
            RestaurarApoyoManotazo();
        }
        if (!visible || SesionJuego.EnPausa || Time.timeScale == 0f) return;
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
        if (!muriendo && controlador != null && controlador.EscudoActivo)
            cuerpo.color = Color.Lerp(new Color(0.2f, 0.85f, 1f), Color.white, 0.2f + 0.15f * Mathf.Sin(Time.time * 14f));
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
        if (auraDorada != null && auraDorada.enabled)
        {
            Bounds contorno = cuerpo.bounds;
            float pulso = 1f + 0.05f * Mathf.Sin(Time.time * 19f);
            auraDorada.transform.position = contorno.center + Vector3.up * 0.16f;
            auraDorada.transform.localScale = new Vector3((contorno.size.x * 1.2f + 0.65f) * pulso / 0.75f,
                (contorno.size.y * 1.15f + 0.7f) * pulso, 1f);
            auraDorada.color = new Color(1f, 1f, 1f,
                Mathf.Lerp(0.2f, 0.88f, Mathf.Clamp01((Time.time - inicioAura) / 1.4f)));
        }
    }

    internal void Transformarse(float duracion)
    {
        objetivoEscala = 1f;
        if (auraDorada == null) CrearAuraDorada();
        inicioAura = Time.time;
        auraDorada.enabled = true;
        animador.speed = 4.4f / duracion;
        Animar("TransformacionRenato", EstadoVisual.CambioFase);
    }

    internal void ReposoDorado()
    {
        animador.speed = 1f;
        Animar("RenatoDoradoReposo", EstadoVisual.Reposo);
    }

    internal void PrepararDisparoDorado(float xObjetivo)
    {
        if (!Mathf.Approximately(xObjetivo, transform.position.x))
            signo = xObjetivo > transform.position.x ? -1 : 1;
        Animar("RenatoDoradoDisparo", EstadoVisual.Ataque);
    }

    internal void MostrarAbatido()
    {
        ApagarAuraDorada();
        objetivoEscala = 0.96f;
        animador.speed = 1f;
        Animar("RenatoAbatido", EstadoVisual.Derrota);
    }

    internal void ApagarAuraDorada()
    {
        if (auraDorada != null) auraDorada.enabled = false;
    }

    private void CrearAuraDorada()
    {
        const int ancho = 96, alto = 128;
        texturaAura = new Texture2D(ancho, alto, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        var pixeles = new Color[ancho * alto];
        for (int y = 0; y < alto; y++)
        for (int x = 0; x < ancho; x++)
        {
            var p = new Vector2((x + 0.5f) * 2f / ancho - 1f, (y + 0.5f) * 2f / alto - 1f);
            float angulo = Mathf.Atan2(p.y, p.x);
            float filo = 0.81f + 0.085f * Mathf.Cos(11f * angulo) + 0.065f * Mathf.Cos(17f * angulo + 1f);
            float distancia = p.magnitude - filo;
            float llama = Mathf.Clamp01(1f - Mathf.Abs(distancia + 0.025f) * 8f);
            float relleno = distancia < 0f ? 0.24f : 0f;
            pixeles[y * ancho + x] = new Color(1f, 0.57f + 0.41f * llama, 0.045f + 0.55f * llama,
                Mathf.Max(relleno, llama));
        }
        texturaAura.SetPixels(pixeles);
        texturaAura.Apply();
        var objeto = new GameObject("Aura dorada de René");
        objeto.transform.SetParent(transform, false);
        auraDorada = objeto.AddComponent<SpriteRenderer>();
        auraDorada.sprite = Sprite.Create(texturaAura, new Rect(0, 0, ancho, alto), Vector2.one * 0.5f, alto);
        auraDorada.sharedMaterial = cuerpo.sharedMaterial;
        auraDorada.sortingLayerID = cuerpo.sortingLayerID;
        auraDorada.sortingOrder = cuerpo.sortingOrder - 1;
    }

    private void OnDestroy()
    {
        if (auraDorada != null) Destroy(auraDorada.sprite);
        if (texturaAura != null) Destroy(texturaAura);
    }
    private void FixedUpdate() { if (cuerpo != null) ActualizarGeometria(); }
    private void ActualizarGeometria()
    {
        if (poses == null || poses.Length < 9 || cuerpo.sprite == null) return;
        PoseRenato pose = null;
        foreach (var configuracion in poses)
            if (configuracion != null && configuracion.sprite == cuerpo.sprite) { pose = configuracion; break; }
        if (pose == null) return;
        ActualizarBrazoManotazo(pose);
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

    private void ActualizarBrazoManotazo(PoseRenato pose)
    {
        if (!pose.palmaEnSuelo || !cuerpo.enabled) RestaurarApoyoManotazo();
        if (pose.brazoFrente == null || !cuerpo.enabled)
        {
            if (brazoManotazo != null) brazoManotazo.enabled = false;
            return;
        }

        if (pose.palmaEnSuelo)
        {
            // El pivote está en la palma. Ajusta el padre para dejar el Modelo bajo control del Animator.
            if (!palmaAlineada) posicionOrientacion = orientacion.localPosition;
            float piso = movimiento != null ? movimiento.SueloY - 0.25f : CentroManotazo.y;
            orientacion.position += Vector3.up * (piso - cuerpo.transform.position.y);
            palmaAlineada = true;
        }

        if (brazoManotazo == null)
        {
            var brazo = new GameObject("Brazos en primer plano");
            brazo.transform.SetParent(cuerpo.transform, false);
            brazoManotazo = brazo.AddComponent<SpriteRenderer>();
            brazoManotazo.sharedMaterial = cuerpo.sharedMaterial;
            brazoManotazo.sortingLayerID = cuerpo.sortingLayerID;
            // Dibuja la mano delante de la cancha, con el torso detrás y el jugador por delante.
            brazoManotazo.sortingOrder = cuerpo.sortingOrder + 2;
        }

        brazoManotazo.sprite = pose.brazoFrente;
        brazoManotazo.color = cuerpo.color;
        brazoManotazo.enabled = true;
    }

    private void RestaurarApoyoManotazo()
    {
        if (!palmaAlineada) return;
        orientacion.localPosition = posicionOrientacion;
        palmaAlineada = false;
    }
}

