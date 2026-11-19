using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

internal sealed class FocoOpcionCombate : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    private Action<bool> mostrar;

    internal void Configurar(Image indicador, Graphic interior, TMP_Text texto, Color tinta)
    {
        Configurar(seleccionado =>
        {
            indicador.enabled = seleccionado;
            interior.color = seleccionado ? EstiloTorneo.Oro : EstiloTorneo.Azul;
            texto.color = seleccionado ? EstiloTorneo.Tinta : tinta;
        });
    }

    internal void Configurar(Action<bool> mostrarFoco)
    {
        mostrar = mostrarFoco;
        ActualizarFoco();
    }

    public void OnSelect(BaseEventData eventData) => mostrar?.Invoke(true);
    public void OnDeselect(BaseEventData eventData) => mostrar?.Invoke(false);
    private void OnEnable() => ActualizarFoco();
    private void OnDisable() => mostrar?.Invoke(false);

    private void ActualizarFoco() =>
        mostrar?.Invoke(EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject);
}
