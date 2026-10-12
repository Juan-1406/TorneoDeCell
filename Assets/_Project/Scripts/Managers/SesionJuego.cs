public static class SesionJuego
{
    public const int VidasIniciales = 3;
    public static int VidasExtra = VidasIniciales;
    public static bool EnPausa;

    public static void Reiniciar()
    {
        VidasExtra = VidasIniciales;
        EnPausa = false;
    }
}