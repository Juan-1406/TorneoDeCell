using System;
using UnityEngine;

[Serializable]
internal sealed class PuntuacionNivel
{
    [SerializeField, Min(0)] private int puntosMaximosParry = 300;
    [SerializeField, Min(1)] private int parrysParaMaximo = 8;
    [SerializeField, Min(0)] private int puntosMaximosDanio = 400;
    [SerializeField, Min(1), Tooltip("Daño acumulado a partir del cual esta categoría vale cero.")]
    private int danioParaCero = 6;
    [SerializeField, Min(0)] private int puntosMaximosTiempo = 300;
    [SerializeField, Min(0), Tooltip("Segundos para obtener todos los puntos de tiempo.")]
    private float tiempoObjetivo = 180f;
    [SerializeField, Min(1), Tooltip("Segundos para perder todos los puntos de tiempo. Debe superar el objetivo.")]
    private float tiempoParaCero = 360f;

    [Header("Clasificación (% del máximo)")]
    [SerializeField, Range(0, 100)] private int minimoS = 90;
    [SerializeField, Range(0, 100)] private int minimoA = 80;
    [SerializeField, Range(0, 100)] private int minimoB = 65;
    [SerializeField, Range(0, 100)] private int minimoC = 50;

    internal ResultadoNivel Calcular(EstadisticasNivel estadisticas)
    {
        int parry = Mathf.Max(0, puntosMaximosParry);
        int danio = Mathf.Max(0, puntosMaximosDanio);
        int tiempo = Mathf.Max(0, puntosMaximosTiempo);
        float objetivo = Mathf.Max(0f, tiempoObjetivo);
        float limite = Mathf.Max(objetivo + 1f, tiempoParaCero);
        float proporcionParry = Mathf.Clamp01((float)estadisticas.ParrysPerfectos / Mathf.Max(1, parrysParaMaximo));
        float proporcionDanio = 1f - Mathf.Clamp01((float)estadisticas.DanioRecibido / Mathf.Max(1, danioParaCero));
        float proporcionTiempo = 1f - Mathf.InverseLerp(objetivo, limite, estadisticas.Segundos);
        return new ResultadoNivel(
            Mathf.RoundToInt(parry * proporcionParry),
            Mathf.RoundToInt(danio * proporcionDanio),
            Mathf.RoundToInt(tiempo * proporcionTiempo));
    }

    internal string Clasificar(int total)
    {
        float maximo = (float)Mathf.Max(0, puntosMaximosParry) + Mathf.Max(0, puntosMaximosDanio) + Mathf.Max(0, puntosMaximosTiempo);
        if (maximo == 0f) return "D";

        float porcentaje = 100f * total / maximo;
        if (porcentaje >= minimoS) return "S";
        if (porcentaje >= minimoA) return "A";
        if (porcentaje >= minimoB) return "B";
        return porcentaje >= minimoC ? "C" : "D";
    }
}

internal sealed class EstadisticasNivel
{
    internal int ParrysPerfectos { get; set; }
    internal int DanioRecibido { get; set; }
    internal float Segundos { get; set; }
}

internal readonly struct ResultadoNivel
{
    internal readonly int Parry;
    internal readonly int Danio;
    internal readonly int Tiempo;
    internal int Total => Parry + Danio + Tiempo;

    internal ResultadoNivel(int parry, int danio, int tiempo)
    {
        Parry = parry;
        Danio = danio;
        Tiempo = tiempo;
    }
}
