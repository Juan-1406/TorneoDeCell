using UnityEngine;

// Consume los estados de la IA. No decide ataques, trayectorias, daño ni desplazamiento físico.
[DisallowMultipleComponent]
public class PresentacionEnemigoMenor : MonoBehaviour
{
    [SerializeField] private Transform visualEntrada;
    [SerializeField] private Transform orientacion;
    [SerializeField] private SpriteRenderer imagen;
    [SerializeField] private Animator animador;
    private EnemigoMenor enemigo;
    private Health vida;
    private EnemigoMenor.EstadoEnemigo estadoAnterior;
    private float inicioEstado, danioHasta, pulsoHasta;
    private string clipActual;
    private SpriteRenderer energia;
    private Vector2 salida;
    private bool vinculado;
    public Transform VisualEntrada => visualEntrada;
    public SpriteRenderer Imagen => imagen;
    public Animator Animador => animador;
    public int Orientacion => orientacion.localScale.x < 0f ? -1 : 1;
    public string AnimacionActual => clipActual;

    public void Vincular(EnemigoMenor comportamiento, Health salud)
    {
        enemigo = comportamiento; vida = salud;
        if (!vinculado) { vida.OnDanioRecibido += RecibirDanio; vinculado = true; }
        estadoAnterior = enemigo.EstadoActual; inicioEstado = Time.time;
        if (enemigo.Tipo == EnemigoMenor.TipoAtaque.Proyectil)
        {
            var go = new GameObject("Carga de palma", typeof(SpriteRenderer));
            go.transform.SetParent(transform, false);
            energia = go.GetComponent<SpriteRenderer>();
            energia.sprite = FeedbackCombate.Aro;
            energia.sortingLayerID = imagen.sortingLayerID; energia.sortingOrder = imagen.sortingOrder + 1;
            energia.enabled = false;
        }
        Reproducir("Reposo");
    }

    private void RecibirDanio()
    {
        if (vida.VidaActual <= 0) return;
        danioHasta = Time.time + 0.14f;
        Reproducir("Danio", true);
    }

    public void PulsoDisparo(Vector2 direccion)
    {
        salida = direccion;
        pulsoHasta = Time.time + 0.1f;
        // El gesto de disparo tiene prioridad durante la emisión, incluso si ya comienza el repliegue.
        Reproducir("Disparar", true);
    }

    private void Reproducir(string nombre, bool reiniciar = false)
    {
        if (!reiniciar && clipActual == nombre) return;
        clipActual = nombre;
        animador.Play(nombre, 0, 0f);
        animador.Update(0f);
    }

    private void LateUpdate()
    {
        if (!vinculado || enemigo == null || !enemigo.isActiveAndEnabled || SesionJuego.EnPausa || Time.timeScale == 0f) return;
        var estado = enemigo.EstadoActual;
        if (estado != estadoAnterior) { estadoAnterior = estado; inicioEstado = Time.time; }
        float sentido = Orientacion;
        if (estado == EnemigoMenor.EstadoEnemigo.Amago || estado == EnemigoMenor.EstadoEnemigo.Carga)
            sentido = enemigo.SentidoCarga;
        else if (estado == EnemigoMenor.EstadoEnemigo.PreparandoRafaga || estado == EnemigoMenor.EstadoEnemigo.Disparando)
            sentido = enemigo.PuntoApuntado.x - transform.position.x;
        else if (enemigo.ObjetivoVisual != null) sentido = enemigo.ObjetivoVisual.position.x - transform.position.x;
        if (Mathf.Abs(sentido) > 0.01f)
            orientacion.localScale = new Vector3(Mathf.Sign(sentido), 1f, 1f);

        bool tirador = enemigo.Tipo == EnemigoMenor.TipoAtaque.Proyectil;
        string nombre = "Reposo";
        switch (estado)
        {
            case EnemigoMenor.EstadoEnemigo.Amago: nombre = "PrepararGolpe"; break;
            case EnemigoMenor.EstadoEnemigo.Carga: nombre = "Golpear"; break;
            case EnemigoMenor.EstadoEnemigo.Recuperacion: nombre = "Recuperar"; break;
            case EnemigoMenor.EstadoEnemigo.PreparandoRafaga:
                nombre = Time.time - inicioEstado < 0.18f ? "Apuntar" : "PrepararProyectil"; break;
            case EnemigoMenor.EstadoEnemigo.Disparando: nombre = "Disparar"; break;
            case EnemigoMenor.EstadoEnemigo.Reposicion:
                nombre = tirador && Time.time - inicioEstado < 0.2f ? "Recuperar" : "Desplazar"; break;
            case EnemigoMenor.EstadoEnemigo.Buscando:
                nombre = Mathf.Abs(enemigo.VelocidadHorizontal) > 0.2f ? "Desplazar" : "Reposo"; break;
        }
        if (Time.time < danioHasta) nombre = "Danio";
        else if (tirador && Time.time < pulsoHasta) nombre = "Disparar";
        Reproducir(nombre);
        imagen.color = Time.time < danioHasta ? new Color(1f, 0.8f, 0.8f) : Color.white;
        if (energia != null)
        {
            bool preparando = estado == EnemigoMenor.EstadoEnemigo.PreparandoRafaga;
            energia.enabled = !enemigo.Entrando && (preparando || Time.time < pulsoHasta);
            if (energia.enabled)
            {
                Vector2 dir = preparando ? (enemigo.PuntoApuntado - (Vector2)transform.position).normalized : salida;
                energia.transform.position = transform.position + (Vector3)(dir * 0.7f);
                float t = preparando ? Mathf.Clamp01((Time.time - inicioEstado) / 0.55f) : 1f;
                energia.transform.localScale = Vector3.one * (preparando ? Mathf.Lerp(0.13f, 0.28f, t) : 0.38f);
                energia.color = new Color(0.9f, 0.55f, 1f, preparando ? 0.6f + t * 0.3f : 0.85f);
            }
        }
    }

    public void MostrarDerrota()
    {
        if (energia != null) energia.enabled = false;
        // Copia estrictamente gráfica: la baja, el loot y el cupo de oleada se resuelven de inmediato.
        GameObject salidaVisual = Instantiate(visualEntrada.gameObject, visualEntrada.position, visualEntrada.rotation);
        salidaVisual.name = "Derrota visual Cell Junior";
        salidaVisual.AddComponent<RetirarVisualEnemigo>().Preparar();
    }

    private void OnDestroy()
    {
        if (vida != null && vinculado) vida.OnDanioRecibido -= RecibirDanio;
    }
}
