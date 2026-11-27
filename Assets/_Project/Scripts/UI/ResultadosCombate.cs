using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

internal static class ResultadosCombate
{
    internal static GameObject Mostrar(Canvas canvas, TMP_FontAsset fuente, Sprite mateo,
        string jefe, string logro, EstadisticasNivel estadisticas, ResultadoNivel puntos,
        int record, string clasificacion, LevelManager nivel)
    {
        var fondo = EstiloTorneo.Imagen(canvas.transform, "Resultados del combate", EstiloTorneo.Tinta, 0, 0, 1, 1);
        fondo.raycastTarget = true;
        var area = AreaSeguraCombate.Crear(fondo.transform, "Área segura de resultados");
        var cartel = EstiloTorneo.Caja(area.Contenido, "Cartel de victoria");
        EstiloTorneo.Anclar(cartel, new Vector2(.5f, .5f), Vector2.zero, new Vector2(1060, 900));
        EstiloTorneo.Imagen(cartel, "Franja superior", EstiloTorneo.Oro, .06f, .972f, .88f, .005f);
        EstiloTorneo.Texto(cartel, "¡VICTORIA!", fuente, 94, .10f, .84f, .80f, .12f, EstiloTorneo.Papel);
        EstiloTorneo.Texto(cartel, jefe, fuente, 41, .10f, .78f, .80f, .055f, EstiloTorneo.Oro);

        var medallon = EstiloTorneo.Marco(cartel, "Fondo de Mateo", .06f, .24f, .30f, .51f);
        var retrato = EstiloTorneo.Imagen(medallon.transform, "Mateo", Color.white, .07f, .25f, .86f, .72f);
        retrato.sprite = mateo;
        retrato.preserveAspect = true;
        retrato.enabled = mateo != null;
        EstiloTorneo.Imagen(medallon.transform, "Base de clasificación", EstiloTorneo.Tinta, .035f, .025f, .93f, .22f);
        EstiloTorneo.Texto(medallon.transform, "Clasificación", fuente, 31, .06f, .045f, .65f, .18f, EstiloTorneo.Papel);
        EstiloTorneo.Texto(medallon.transform, clasificacion, fuente, 64, .71f, .03f, .23f, .21f, EstiloTorneo.Oro);
        var cinta = EstiloTorneo.Imagen(cartel, "Cinta", EstiloTorneo.Oro, .06f, .145f, .30f, .069f);
        EstiloTorneo.Texto(cinta.transform, logro, fuente, 34, .025f, .03f, .95f, .94f, EstiloTorneo.Tinta);

        var panel = EstiloTorneo.Marco(cartel, "Marcador", .385f, .24f, .555f, .51f).transform;
        EstiloTorneo.Texto(panel, "Puntos", fuente, 28, .77f, .865f, .16f, .10f, EstiloTorneo.Oro)
            .alignment = TextAlignmentOptions.MidlineRight;
        int centesimas = Mathf.FloorToInt(estadisticas.Segundos * 100f);
        string tiempo = $"{centesimas / 6000:00}:{centesimas / 100 % 60:00}.{centesimas % 100:00}";
        Fila(panel, fuente, "Parrys perfectos", estadisticas.ParrysPerfectos.ToString(), puntos.Parry, .705f);
        Fila(panel, fuente, "Daño recibido", estadisticas.DanioRecibido.ToString(), puntos.Danio, .543f);
        Fila(panel, fuente, "Tiempo", tiempo, puntos.Tiempo, .381f);
        Fila(panel, fuente, "Puntuación final", "", puntos.Total, .202f, true);
        Fila(panel, fuente, "Récord personal", "", record, .04f);

        Button revancha = Accion(cartel, fuente, "Revancha", .385f, true);
        Button volver = Accion(cartel, fuente, "Elegir nivel", .67f, false);
        revancha.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = volver };
        volver.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = revancha };
        revancha.onClick.AddListener(nivel.ReintentarVictoria);
        volver.onClick.AddListener(nivel.VolverASeleccion);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(revancha.gameObject);
        return fondo.gameObject;
    }

    private static void Fila(Transform panel, TMP_FontAsset fuente, string nombre, string dato, int puntos,
        float y, bool destacada = false)
    {
        const float alto = .145f;
        EstiloTorneo.Imagen(panel, nombre, destacada ? EstiloTorneo.Oro : EstiloTorneo.Tinta, .035f, y, .93f, alto);
        EstiloTorneo.Texto(panel, nombre, fuente, 27, .065f, y, .43f, alto, destacada ? EstiloTorneo.Tinta : EstiloTorneo.Papel)
            .alignment = TextAlignmentOptions.MidlineLeft;
        if (dato.Length > 0)
            EstiloTorneo.Texto(panel, dato, fuente, 31, .50f, y, .24f, alto, EstiloTorneo.Papel)
                .alignment = TextAlignmentOptions.MidlineRight;
        EstiloTorneo.Texto(panel, puntos.ToString("N0"), fuente, destacada ? 46 : 36, .77f, y, .16f, alto,
            destacada ? EstiloTorneo.Tinta : EstiloTorneo.Oro)
            .alignment = TextAlignmentOptions.MidlineRight;
    }

    private static Button Accion(Transform padre, TMP_FontAsset fuente, string nombre, float x, bool principal)
    {
        Image baseBoton = EstiloTorneo.Imagen(padre, nombre, Color.white, x, .135f, .27f, .089f);
        baseBoton.raycastTarget = true;
        Button boton = baseBoton.gameObject.AddComponent<Button>();
        boton.targetGraphic = baseBoton;
        var texto = EstiloTorneo.Texto(boton.transform, nombre, fuente, 34, 0, 0, 1, 1, EstiloTorneo.Papel);
        EstiloTorneo.Boton(boton, texto, principal);
        return boton;
    }
}
