using System.Collections;
using UnityEngine;

public class BossController : MonoBehaviour
{
    [SerializeField] private Health vida;
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private float retardoInicial = 1.5f;
    [SerializeField] private FaseJefe[] fases;

    private Transform objetivo;
    private int faseActual = -1;

    private void OnEnable()
    {
        vida.OnVidaCambiada += RevisarFase;
    }

    private void OnDisable()
    {
        if (vida != null) vida.OnVidaCambiada -= RevisarFase;
    }

    private void Start()
    {
        GameObject jugador = GameObject.FindGameObjectWithTag("Player");
        if (jugador != null) objetivo = jugador.transform;

        RevisarFase(vida.VidaActual, vida.VidaMaxima);
        StartCoroutine(Combate());
    }

    private void RevisarFase(int actual, int maxima)
    {
        float porcentaje = (float)actual / maxima;
        int nueva = 0;

        for (int i = 0; i < fases.Length; i++)
        {
            if (porcentaje <= fases[i].activaBajoVida) nueva = i;
        }

        if (nueva == faseActual) return;

        faseActual = nueva;
        sprite.color = fases[nueva].color;
        Debug.Log("Jefe en fase: " + fases[nueva].nombre);
    }

    private IEnumerator Combate()
    {
        yield return new WaitForSeconds(retardoInicial);

        while (vida.VidaActual > 0)
        {
            FaseJefe fase = fases[faseActual];

            if (objetivo == null || fase.ataques.Length == 0)
            {
                yield return null;
                continue;
            }

            BossAttack ataque = fase.ataques[Random.Range(0, fase.ataques.Length)];
            yield return StartCoroutine(ataque.Ejecutar(objetivo));
            yield return new WaitForSeconds(fase.pausaEntreAtaques);
        }
    }
}