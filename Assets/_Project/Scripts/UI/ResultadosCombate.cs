using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

internal static class ResultadosCombate
{
    internal static GameObject Mostrar(Canvas canvas, TMP_FontAsset fuente, Sprite mateo,
        string jefe, string logro, float segundos, Health vida, int parries, LevelManager nivel)
    {
        var fondo = EstiloTorneo.Imagen(canvas.transform, "Resultados del combate", EstiloTorneo.Tinta, 0, 0, 1, 1);
        fondo.raycastTarget = true;
        EstiloTorneo.Imagen(fondo.transform, "Franja superior", EstiloTorneo.Oro, .07f, .89f, .86f, .005f);
        EstiloTorneo.Texto(fondo.transform, "¡VICTORIA!", fuente, 94, .12f, .72f, .76f, .16f, EstiloTorneo.Papel);
        EstiloTorneo.Texto(fondo.transform, jefe, fuente, 41, .12f, .65f, .76f, .065f, EstiloTorneo.Oro);

        var medallon = EstiloTorneo.Marco(fondo.transform, "Fondo de Mateo", .10f, .25f, .32f, .39f);
        var retrato = EstiloTorneo.Imagen(medallon.transform, "Mateo", Color.white, .07f, .02f, .86f, .95f);
        retrato.sprite = mateo;
        retrato.preserveAspect = true;
        retrato.enabled = mateo != null;
        var cinta = EstiloTorneo.Imagen(fondo.transform, "Cinta", EstiloTorneo.Oro, .075f, .185f, .37f, .071f);
        EstiloTorneo.Texto(cinta.transform, logro, fuente, 35, .025f, .03f, .95f, .94f, EstiloTorneo.Tinta);

        var panel = EstiloTorneo.Marco(fondo.transform, "Marcador", .48f, .25f, .44f, .37f);
        int tiempo = Mathf.FloorToInt(segundos);
        Fila(panel.transform, fuente, "Tiempo activo", $"{tiempo / 60:00}:{tiempo % 60:00}", .68f);
        Fila(panel.transform, fuente, "Vida restante", $"{vida.VidaActual} / {vida.VidaMaxima}", .38f);
        Fila(panel.transform, fuente, "Parries exitosos", parries.ToString(), .08f);
        Button reintentar = Accion(fondo.transform, fuente, "Reintentar", .48f, false);
        Button volver = Accion(fondo.transform, fuente, "Volver a la selección", .705f, true);
        reintentar.onClick.AddListener(nivel.ReintentarVictoria);
        volver.onClick.AddListener(nivel.VolverASeleccion);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(volver.gameObject);
        return fondo.gameObject;
    }

    private static void Fila(Transform panel, TMP_FontAsset fuente, string nombre, string dato, float y)
    {
        EstiloTorneo.Imagen(panel, nombre, EstiloTorneo.Tinta, .035f, y, .93f, .25f);
        EstiloTorneo.Texto(panel, nombre, fuente, 33, .06f, y, .57f, .25f, EstiloTorneo.Papel)
            .alignment = TextAlignmentOptions.MidlineLeft;
        EstiloTorneo.Texto(panel, dato, fuente, 49, .66f, y, .27f, .25f, EstiloTorneo.Oro)
            .alignment = TextAlignmentOptions.MidlineRight;
    }

    private static Button Accion(Transform padre, TMP_FontAsset fuente, string nombre, float x, bool principal)
    {
        Image baseBoton = EstiloTorneo.Imagen(padre, nombre, Color.white, x, .13f, .215f, .088f);
        baseBoton.raycastTarget = true;
        Button boton = baseBoton.gameObject.AddComponent<Button>();
        boton.targetGraphic = baseBoton;
        var texto = EstiloTorneo.Texto(boton.transform, nombre, fuente, 34, 0, 0, 1, 1, EstiloTorneo.Papel);
        EstiloTorneo.Boton(boton, texto, principal);
        return boton;
    }
}
