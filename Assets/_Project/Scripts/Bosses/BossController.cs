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

    private void Awake()
    {
        movimiento = GetComponent<SeguirCamaraX>();
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
        if (sprite != null) sprite.color = fases[nueva].color;
        if (movimiento != null)
            movimiento.ConfigurarOscilacion(fases[nueva].amplitudHorizontal,
                fases[nueva].amplitudVertical, fases[nueva].ciclosPorSegundo);
        Debug.Log("Jefe en fase: " + fases[nueva].nombre);
    }

    private IEnumerator Combate()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, retardoInicial));

        while (isActiveAndEnabled && vida != null && vida.VidaActual > 0)
        {
            FaseJefe fase = fases != null && faseActual >= 0 && faseActual < fases.Length
                ? fases[faseActual] : null;
            BossAttack ataque = SeleccionarAtaque(fase);

            if (objetivo == null || ataque == null)
            {
                yield return null;
                continue;
            }

            ultimoAtaque = ataque;
            ataqueActual = ataque;
            yield return StartCoroutine(ataque.Ejecutar(objetivo));
            if (ataqueActual != null) ataqueActual.Cancelar();
            ataqueActual = null;

            // La fase nueva se utiliza en la siguiente seleccion, sin solapar ataques.
            yield return new WaitForSeconds(Mathf.Max(0f, fase.pausaEntreAtaques));
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
