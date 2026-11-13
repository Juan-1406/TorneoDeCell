public static class SesionJuego
{
    public const int VidasIniciales = 3;
    public static int VidasExtra = VidasIniciales;
    public static bool EnPausa;
    internal static bool SoltarEntradaDeMenu;
    internal static string EscenaRepeticion { get; private set; }
    internal static void HabilitarRepeticion(string escena) => EscenaRepeticion = escena;

    private static string escenaJefePendiente;
    private static int vidaJefePendiente = -1;
    private static string escenaOleadasPendiente;
    private static int siguienteOleada;

    public static int ProgresoOleadas(string escena)
    {
        return escenaOleadasPendiente == escena ? siguienteOleada : 0;
    }

    public static void GuardarProgresoOleadas(string escena, int oleadaSiguiente)
    {
        escenaOleadasPendiente = escena;
        siguienteOleada = oleadaSiguiente;
    }

    public static void LimpiarProgresoOleadas()
    {
        escenaOleadasPendiente = null;
        siguienteOleada = 0;
    }

    public static void GuardarVidaJefe(string escena, int vida)
    {
        escenaJefePendiente = escena;
        vidaJefePendiente = vida;
    }

    public static bool IntentarRestaurarVidaJefe(string escena, out int vida)
    {
        if (vidaJefePendiente >= 0 && escenaJefePendiente == escena)
        {
            vida = vidaJefePendiente;
            LimpiarVidaJefe();
            return true;
        }

        vida = 0;
        return false;
    }

    private static void LimpiarVidaJefe()
    {
        escenaJefePendiente = null;
        vidaJefePendiente = -1;
    }

    public static void Reiniciar()
    {
        EscenaRepeticion = null;
        ReiniciarIntento();
    }

    internal static void ReiniciarIntento()
    {
        SoltarEntradaDeMenu = false;
        VidasExtra = VidasIniciales;
        EnPausa = false;
        LimpiarVidaJefe();
        LimpiarProgresoOleadas();
    }
}
