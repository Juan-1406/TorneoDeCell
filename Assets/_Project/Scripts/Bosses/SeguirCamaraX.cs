using UnityEngine;
using System.Collections;

public class SeguirCamaraX : MonoBehaviour
{
    [SerializeField] private float desfaseX = 5f;
    [SerializeField] private float suavidad = 2f;
    [SerializeField] private BoxCollider2D limitesArena;
    [Header("Puesta en escena de Renato")]
    [SerializeField] private bool movimientoEscenico;
    [SerializeField] private Collider2D sueloEscenario;
    private PresentacionRenato presentacion;
    private Vector3 ancla;
    private bool trasladando;
    private int faseEscenica, movimientos;
    public bool MovimientoEscenico => movimientoEscenico;
    public bool Trasladando => trasladando;
    public float SueloY => sueloEscenario != null ? sueloEscenario.bounds.max.y + 0.25f : alturaInicial;
    public float PlanoPosteriorY => presentacion != null && presentacion.MedioCuerpo ? presentacion.BordePrimerPlanoY - 0.7f : SueloY;

    private Camera cam;
    private Collider2D colision;
    private float alturaInicial;
    private float amplitudHorizontal;
    private float amplitudVertical;
    private float frecuencia;
    private float objetivoHorizontal;
    private float objetivoVertical;
    private float objetivoFrecuencia;
    private float faseOscilacion;

    private void Awake()
    {
        cam = Camera.main;
        colision = GetComponent<Collider2D>();
        alturaInicial = transform.position.y;
        ancla = transform.position;
        presentacion = GetComponent<PresentacionRenato>();
    }

    private void OnDisable() { trasladando = false; }
    public void EstablecerFase(int fase) => faseEscenica = Mathf.Clamp(fase, 0, 2);

    public IEnumerator ColocarParaAtaque(BossAttack ataque, Transform jugador)
    {
        if (!movimientoEscenico || presentacion == null || jugador == null) yield break;
        movimientos++;
        float centro = sueloEscenario != null ? sueloEscenario.bounds.center.x : 0f;
        float semiancho = sueloEscenario != null ? sueloEscenario.bounds.extents.x : 12f;
        float lado = transform.position.x >= centro ? 1f : -1f;
        if (ataque is AtaqueExterno && faseEscenica > 0) lado = -lado;
        float x = centro + lado * semiancho * (movimientos % 2 == 0 ? 0.52f : 0.64f);
        float y = PlanoPosteriorY;
        if (ataque is AtaqueAbanico)
        {
            x = centro + (jugador.position.x >= centro ? -1.8f : 1.8f);
            y += presentacion.MedioCuerpo ? -0.12f : 0.65f;
        }
        else if (Mathf.Abs(x - jugador.position.x) < 4.3f)
            x = centro - Mathf.Sign(jugador.position.x - centro) * semiancho * 0.64f;
        Vector3 destino = Acotar(new Vector3(x, y, transform.position.z));
        Vector3 origen = transform.position;
        if (Mathf.Abs(destino.x - origen.x) > 0.6f)
            yield return presentacion.GirarHacia(destino.x);
        presentacion.Traslado(); trasladando = true;
        float distancia = Vector2.Distance(origen, destino);
        float duracion = Mathf.Clamp(distancia / (9f + faseEscenica), 0.5f, 1.5f);
        // El busto se desplaza detrás de la cancha; no salta dejando una cintura flotante.
        float elevacion = presentacion.MedioCuerpo ? 0.035f : distancia > 4f ? 1f + faseEscenica * 0.15f : 0.18f;
        float tiempo = 0f;
        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float t = Mathf.Clamp01(tiempo / duracion);
            Vector3 punto = Vector3.Lerp(origen, destino, Mathf.SmoothStep(0f, 1f, t));
            punto.y += Mathf.Sin(t * Mathf.PI) * elevacion;
            transform.position = Acotar(punto);
            yield return null;
        }
        ancla = Acotar(destino); transform.position = ancla; trasladando = false;
        yield return presentacion.GirarHacia(jugador.position.x);
        presentacion.Reposo();
    }

    private Vector3 Acotar(Vector3 posicion)
    {
        float radio = presentacion != null ? Mathf.Max(2.5f, presentacion.RadioVisual) : 2.8f;
        float izquierda = -12f + radio, derecha = 12f - radio;
        if (sueloEscenario != null)
        { izquierda = sueloEscenario.bounds.min.x + radio; derecha = sueloEscenario.bounds.max.x - radio; }
        if (cam != null && cam.orthographic)
        {
            float borde = cam.orthographicSize * cam.aspect;
            izquierda = Mathf.Max(izquierda, cam.transform.position.x - borde + radio);
            derecha = Mathf.Min(derecha, cam.transform.position.x + borde - radio);
            float alto = presentacion != null ? presentacion.AlturaVisual : 5f;
            posicion.y = Mathf.Min(posicion.y, cam.transform.position.y + cam.orthographicSize - alto - 0.15f);
        }
        posicion.x = izquierda < derecha ? Mathf.Clamp(posicion.x, izquierda, derecha) : (izquierda + derecha) * 0.5f;
        posicion.y = Mathf.Max(presentacion != null && presentacion.MedioCuerpo ? PlanoPosteriorY - 0.15f : SueloY, posicion.y);
        if (presentacion != null && presentacion.MedioCuerpo)
            posicion.y = Mathf.Min(posicion.y, presentacion.BordePrimerPlanoY - 0.6f);
        return posicion;
    }

    public void ConfigurarOscilacion(float horizontal, float vertical, float ciclosPorSegundo)
    {
        objetivoHorizontal = Mathf.Max(0f, horizontal);
        objetivoVertical = Mathf.Max(0f, vertical);
        objetivoFrecuencia = Mathf.Max(0f, ciclosPorSegundo);
    }

    private void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        if (movimientoEscenico)
        {
            if (!trasladando)
            {
                float tiempo = Time.time * (1f + faseEscenica * 0.3f);
                Vector3 reposo = ancla + new Vector3(Mathf.Sin(tiempo) * 0.1f, Mathf.Sin(tiempo * 0.7f) * 0.035f, 0f);
                transform.position = Acotar(reposo);
            }
            return;
        }

        float paso = Time.deltaTime;
        amplitudHorizontal = Mathf.MoveTowards(amplitudHorizontal, objetivoHorizontal, 1.5f * paso);
        amplitudVertical = Mathf.MoveTowards(amplitudVertical, objetivoVertical, 1.5f * paso);
        frecuencia = Mathf.MoveTowards(frecuencia, objetivoFrecuencia, 0.5f * paso);
        faseOscilacion += frecuencia * 2f * Mathf.PI * paso;

        float objetivoX = cam.transform.position.x + desfaseX
            + amplitudHorizontal * Mathf.Sin(faseOscilacion);
        if (cam.orthographic && colision != null)
        {
            float bordeDerecho = cam.transform.position.x + cam.orthographicSize * cam.aspect;
            objetivoX = Mathf.Min(objetivoX, bordeDerecho - colision.bounds.extents.x - 0.25f);
        }
        if (limitesArena != null && colision != null)
        {
            float margen = colision.bounds.extents.x + 0.4f;
            float centro = limitesArena.transform.TransformPoint(limitesArena.offset).x;
            float mitad = limitesArena.size.x * Mathf.Abs(limitesArena.transform.lossyScale.x) * 0.5f;
            objetivoX = Mathf.Clamp(objetivoX,
                centro - mitad + margen,
                centro + mitad - margen);
        }

        // La oscilacion vertical solo sube desde la altura original del jefe.
        float objetivoY = alturaInicial + amplitudVertical * (1f - Mathf.Cos(2f * faseOscilacion)) * 0.5f;
        float interpolacion = Mathf.Clamp01(suavidad * paso);
        float nuevaX = Mathf.Lerp(transform.position.x, objetivoX, interpolacion);
        float nuevaY = Mathf.Lerp(transform.position.y, objetivoY, interpolacion);
        transform.position = new Vector3(nuevaX, nuevaY, transform.position.z);
    }
}
