using System.Collections.Generic;
using UnityEngine;

internal sealed class EscudoPorDisparos
{
    private readonly Queue<float> impactos = new Queue<float>();
    private float escudoHasta;
    private float siguienteEscudo;

    public bool Activo => Time.time < escudoHasta;

    public bool Registrar(int umbral, float ventana, float duracion, float recarga)
    {
        float ahora = Time.time;
        if (ahora < siguienteEscudo) return false;

        while (impactos.Count > 0 && ahora - impactos.Peek() > ventana)
            impactos.Dequeue();
        impactos.Enqueue(ahora);
        if (impactos.Count < umbral) return false;

        impactos.Clear();
        escudoHasta = ahora + duracion;
        siguienteEscudo = escudoHasta + recarga;
        return true;
    }

    public int AjustarDanio(int danio, float reduccion)
    {
        return Activo ? Mathf.Max(1, Mathf.RoundToInt(danio * (1f - reduccion))) : danio;
    }

    public void Reiniciar()
    {
        impactos.Clear();
        escudoHasta = siguienteEscudo = 0f;
    }
}
