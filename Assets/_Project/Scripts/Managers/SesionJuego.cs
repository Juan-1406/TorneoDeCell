public static class SesionJuego
{
    public const int VidasIniciales = 3;
    public static int VidasExtra = VidasIniciales;

    public static void Reiniciar()
    {
        VidasExtra = VidasIniciales;
    }
}