using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelSelectUI : MonoBehaviour
{
    [Serializable]
    public class NivelEntrada
    {
        public string nombre;
        public string escena;
        public Button boton;
        public TMP_Text etiqueta;
    }

    [SerializeField] private NivelEntrada[] niveles;
    [SerializeField] private SceneLoader cargador;

    private void Start()
    {
        for (int i = 0; i < niveles.Length; i++)
        {
            string escena = niveles[i].escena;
            niveles[i].boton.onClick.AddListener(() => cargador.CargarEscena(escena));
        }

        Actualizar();
    }

    public void ReiniciarProgreso()
    {
        ProgresoJuego.Borrar();
        Actualizar();
    }

    private void Actualizar()
    {
        for (int i = 0; i < niveles.Length; i++)
        {
            NivelEntrada n = niveles[i];

            bool existe = Application.CanStreamedLevelBeLoaded(n.escena);
            bool desbloqueado = ProgresoJuego.NivelDesbloqueado(i);
            bool completado = ProgresoJuego.NivelCompletado(i);

            n.boton.interactable = existe && ProgresoJuego.PuedeJugarNivel(i);

            if (!existe) n.etiqueta.text = n.nombre + "\nPRÓXIMAMENTE";
            else if (completado) n.etiqueta.text = n.nombre +
                (i == 1 && !ProgresoJuego.JefeDerrotado(i) ? "\nFase 1 superada" : "\nCOMPLETADO") +
                "\n<size=14>Reinicia el progreso para repetir</size>";
            else if (i == 1 && !desbloqueado) n.etiqueta.text = n.nombre + "\nDerrota a René Puente para desbloquear";
            else if (!desbloqueado) n.etiqueta.text = "BLOQUEADO";
            else n.etiqueta.text = n.nombre;
        }
    }
}
