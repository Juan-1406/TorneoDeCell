using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [SerializeField] private GameObject balaPrefab;
    [SerializeField] private float cadencia = 0.2f;

    internal static bool EntradaDisparoActiva => Input.GetMouseButton(0) || Input.GetButton("Fire1");

    private PlayerMovement movimiento;
    private MateoSpriteAnimator visual;
    private float siguienteDisparo;
    private float mejoraRestante;
    private float multiplicadorCadencia = 1f;
    public bool SuspenderMejora { get; set; }
    internal bool BloqueadoPorDialogo { get; set; }
    public float SegundosMejora => mejoraRestante;
    public float CadenciaActual => cadencia * (mejoraRestante > 0f ? multiplicadorCadencia : 1f);

    public void LimpiarMejora()
    {
        mejoraRestante = 0f;
        multiplicadorCadencia = 1f;
    }

    private void OnDisable() => LimpiarMejora();

    public void MejorarCadenciaTemporal(float multiplicador, float duracion)
    {
        if (multiplicador <= 0f || multiplicador >= 1f || duracion <= 0f) return;

        multiplicadorCadencia = mejoraRestante > 0f
            ? Mathf.Min(multiplicadorCadencia, multiplicador) : multiplicador;
        mejoraRestante = Mathf.Max(mejoraRestante, duracion);
        siguienteDisparo = Mathf.Min(siguienteDisparo, Time.time + cadencia * multiplicadorCadencia);
    }

    private void Awake()
    {
        movimiento = GetComponent<PlayerMovement>();
        visual = GetComponent<MateoSpriteAnimator>();
    }

    private void Update()
    {
        if (SesionJuego.EnPausa || Time.timeScale == 0f || BloqueadoPorDialogo || DialogoRenato.ConsumioClic) return;
        if (!SuspenderMejora) mejoraRestante = Mathf.Max(0f, mejoraRestante - Time.deltaTime);
        if (EntradaDisparoActiva && Time.time >= siguienteDisparo)
        {
            Disparar();
            siguienteDisparo = Time.time + CadenciaActual;
        }
    }

    private void Disparar()
    {
        Vector2 direccion = movimiento != null ? movimiento.DireccionDisparo : Vector2.right;

        float lado = movimiento != null ? movimiento.DireccionMirada : 1f;
        Vector3 origen = visual != null ? visual.PuntaCanon(direccion, lado) : transform.position;
        GameObject bala = Instantiate(balaPrefab, origen, Quaternion.identity);
        bala.GetComponent<Bullet>().Disparar(direccion);
        if (visual != null) visual.MostrarDisparo(origen, direccion, lado);
    }
}
