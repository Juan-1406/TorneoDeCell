using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

internal sealed class FocoOpcionCombate : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    private Image rombo;
    private Graphic fondo;
    private TMP_Text etiqueta;
    private Color tintaNormal;

    internal void Configurar(Image indicador, Graphic interior, TMP_Text texto, Color tinta)
    {
        rombo = indicador;
        fondo = interior;
        etiqueta = texto;
        tintaNormal = tinta;
        Mostrar(false);
    }

    public void OnSelect(BaseEventData eventData) => Mostrar(true);
    public void OnDeselect(BaseEventData eventData) => Mostrar(false);
    private void OnEnable()
    {
        if (rombo != null)
            Mostrar(EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject);
    }

    private void OnDisable() => Mostrar(false);

    private void Mostrar(bool seleccionado)
    {
        rombo.enabled = seleccionado;
        fondo.color = seleccionado ? EstiloTorneo.Oro : EstiloTorneo.Azul;
        etiqueta.color = seleccionado ? EstiloTorneo.Tinta : tintaNormal;
    }
}
