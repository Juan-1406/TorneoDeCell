public static class SesionJuego
{
    public const int VidasIniciales = 3;
    public static int VidasExtra = VidasIniciales;
    public static bool EnPausa;

    private static string escenaJefePendiente;
    private static int vidaJefePendiente = -1;

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
        VidasExtra = VidasIniciales;
        EnPausa = false;
        LimpiarVidaJefe();
    }
}
