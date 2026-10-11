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
            bool derrotado = ProgresoJuego.JefeDerrotado(i);

            n.boton.interactable = existe && desbloqueado;

            if (!existe) n.etiqueta.text = n.nombre + "\nPRÓXIMAMENTE";
            else if (!desbloqueado) n.etiqueta.text = "BLOQUEADO";
            else if (derrotado) n.etiqueta.text = n.nombre + "\n(DERROTADO)";
            else n.etiqueta.text = n.nombre;
        }
    }
}