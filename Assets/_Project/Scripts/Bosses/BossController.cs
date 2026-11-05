using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossController : MonoBehaviour
{
    [SerializeField] private Health vida;
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private float retardoInicial = 1.5f;
    [SerializeField] private FaseJefe[] fases;

    private Transform objetivo;
    private SeguirCamaraX movimiento;
    private int faseActual = -1;
    private BossAttack ataqueActual;
    private BossAttack ultimoAtaque;
    private readonly List<BossAttack> candidatos = new List<BossAttack>();
    private bool iniciado;
    private PresentacionRenato presentacion;
    private int faseVisualPendiente = -1;
    public int FaseActual => faseActual;

    private void Awake()
    {
        movimiento = GetComponent<SeguirCamaraX>();
        presentacion = GetComponent<PresentacionRenato>();
    }

    private void OnEnable()
    {
        if (vida != null)
        {
            vida.OnVidaCambiada += RevisarFase;
            vida.OnMuerte += DetenerCombate;
        }

        if (iniciado) IniciarCombate();
    }

    private void OnDisable()
    {
        DetenerCombate();
        if (vida != null)
        {
            vida.OnVidaCambiada -= RevisarFase;
            vida.OnMuerte -= DetenerCombate;
        }
    }

    private void Start()
    {
        iniciado = true;
        if (vida == null || fases == null || fases.Length == 0)
        {
            Debug.LogWarning("El jefe necesita vida y fases configuradas para atacar.", this);
            return;
        }

        IniciarCombate();
    }

    private void IniciarCombate()
    {
        if (vida == null || vida.VidaActual <= 0) return;

        if (movimiento != null) movimiento.enabled = true;

        GameObject jugador = GameObject.FindGameObjectWithTag("Player");
        if (jugador != null) objetivo = jugador.transform;

        RevisarFase(vida.VidaActual, vida.VidaMaxima);
        StartCoroutine(Combate());
    }

    private void RevisarFase(int actual, int maxima)
    {
        if (actual <= 0 || maxima <= 0 || fases == null) return;

        float porcentaje = (float)actual / maxima;
        int nueva = -1;

        for (int i = 0; i < fases.Length; i++)
        {
            if (fases[i] == null) continue;
            if (nueva < 0) nueva = i;
            if (porcentaje <= fases[i].activaBajoVida) nueva = i;
        }

        if (nueva < 0 || nueva == faseActual) return;

        faseActual = nueva;
        if (movimiento != null) movimiento.EstablecerFase(nueva);
        if (presentacion != null)
        {
            if (nueva == 0) presentacion.EstablecerFase(nueva);
            else faseVisualPendiente = nueva;
        }
        if (sprite != null) sprite.color = fases[nueva].color;
        if (movimiento != null)
            movimiento.ConfigurarOscilacion(fases[nueva].amplitudHorizontal,
                fases[nueva].amplitudVertical, fases[nueva].ciclosPorSegundo);
        Debug.Log("Jefe en fase: " + fases[nueva].nombre);
    }

    private IEnumerator Combate()
    {
        if (presentacion != null) yield return presentacion.Entrada();
        yield return new WaitForSeconds(Mathf.Max(0f, retardoInicial - (presentacion != null ? 0.85f : 0f)));

        while (isActiveAndEnabled && vida != null && vida.VidaActual > 0)
        {
            if (presentacion != null && faseVisualPendiente >= 0)
            {
                int pendiente = faseVisualPendiente; faseVisualPendiente = -1;
                yield return presentacion.CambioFase(pendiente);
            }
            FaseJefe fase = fases != null && faseActual >= 0 && faseActual < fases.Length
                ? fases[faseActual] : null;
            BossAttack ataque = SeleccionarAtaque(fase);

            if (objetivo == null || ataque == null)
            {
                yield return null;
                continue;
            }

            ultimoAtaque = ataque;
            if (presentacion != null)
            {
                if (movimiento != null) yield return movimiento.ColocarParaAtaque(ataque, objetivo);
                yield return new WaitForSeconds(presentacion.Preparar(ataque));
                presentacion.IniciarAtaque(ataque);
                // Permite al Animator aplicar la pose y alinear nariz e hitbox antes de disparar.
                yield return null;
            }
            ataqueActual = ataque;
            yield return StartCoroutine(ataque.Ejecutar(objetivo));
            if (ataqueActual != null) ataqueActual.Cancelar();
            ataqueActual = null;

            // La fase nueva se utiliza en la siguiente seleccion, sin solapar ataques.
            if (presentacion != null)
            {
                presentacion.Recuperar();
                yield return new WaitForSeconds(0.4f);
                presentacion.Reposo();
            }
            yield return new WaitForSeconds(Mathf.Max(0f, fase.pausaEntreAtaques - (presentacion != null ? 0.4f : 0f)));
        }
    }

    private BossAttack SeleccionarAtaque(FaseJefe fase)
    {
        candidatos.Clear();
        if (fase == null || fase.ataques == null) return null;

        bool puedeRepetir = false;
        foreach (BossAttack ataque in fase.ataques)
        {
            if (ataque == null || !ataque.isActiveAndEnabled) continue;
            if (ataque == ultimoAtaque)
            {
                puedeRepetir = true;
                continue;
            }
            if (!candidatos.Contains(ataque)) candidatos.Add(ataque);
        }

        if (candidatos.Count > 0) return candidatos[Random.Range(0, candidatos.Count)];
        return puedeRepetir ? ultimoAtaque : null;
    }

    private void DetenerCombate()
    {
        StopAllCoroutines();
        // Cancelar la corutina no garantiza que se retiren sus avisos visuales.
        if (ataqueActual != null) ataqueActual.Cancelar();
        ataqueActual = null;
        if (movimiento != null) movimiento.enabled = false;
    }
}
