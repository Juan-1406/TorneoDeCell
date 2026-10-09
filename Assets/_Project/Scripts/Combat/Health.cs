using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private int vidaMaxima = 100;
    [SerializeField] private bool destruirAlMorir = false;

    public int VidaActual { get; private set; }
    public int VidaMaxima => vidaMaxima;

    public event Action<int, int> OnVidaCambiada;
    public event Action OnMuerte;

    private void Awake()
    {
        VidaActual = vidaMaxima;
    }

    private void Start()
    {
        OnVidaCambiada?.Invoke(VidaActual, vidaMaxima);
    }

    public void RecibirDanio(int cantidad)
    {
        if (VidaActual <= 0) return;

        VidaActual = Mathf.Max(VidaActual - cantidad, 0);
        OnVidaCambiada?.Invoke(VidaActual, vidaMaxima);

        if (VidaActual == 0)
        {
            OnMuerte?.Invoke();
            if (destruirAlMorir) Destroy(gameObject);
        }
    }
}