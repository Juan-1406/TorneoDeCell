using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private int vidaMaxima = 100;
    [SerializeField] private bool destruirAlMorir = false;
    [SerializeField] private float tiempoInvulnerable = 0f;

    private float invulnerableHasta;

    public int VidaActual { get; private set; }
    public int VidaMaxima => vidaMaxima;
    public bool Invulnerable { get; set; }
    public bool EsInvulnerable => Invulnerable || Time.time < invulnerableHasta;

    public event Action<int, int> OnVidaCambiada;
    public event Action OnDanioRecibido;
    public event Action OnMuerte;

    private void Awake()
    {
        VidaActual = vidaMaxima;
    }

    private void Start()
    {
        OnVidaCambiada?.Invoke(VidaActual, vidaMaxima);
    }

    public bool RecibirDanio(int cantidad)
    {
        if (VidaActual <= 0 || EsInvulnerable) return false;

        VidaActual = Mathf.Max(VidaActual - cantidad, 0);
        invulnerableHasta = Time.time + tiempoInvulnerable;

        OnVidaCambiada?.Invoke(VidaActual, vidaMaxima);
        OnDanioRecibido?.Invoke();

        if (VidaActual == 0)
        {
            OnMuerte?.Invoke();
            if (destruirAlMorir) Destroy(gameObject);
        }

        return true;
    }
}