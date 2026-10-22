using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Health objetivo;
    [SerializeField] private Slider slider;

    private void OnEnable()
    {
        objetivo.OnVidaCambiada += Actualizar;
        Actualizar(objetivo.VidaActual, objetivo.VidaMaxima);
    }

    private void OnDisable()
    {
        if (objetivo != null) objetivo.OnVidaCambiada -= Actualizar;
    }

    private void Actualizar(int actual, int maxima)
    {
        slider.value = (float)actual / maxima;
    }
}
