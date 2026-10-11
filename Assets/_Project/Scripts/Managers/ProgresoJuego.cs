using UnityEngine;

public static class ProgresoJuego
{
    private const string Prefijo = "jefe_derrotado_";
    private const int TotalJefes = 4;

    public static bool JefeDerrotado(int id)
    {
        return PlayerPrefs.GetInt(Prefijo + id, 0) == 1;
    }

    public static void MarcarDerrotado(int id)
    {
        PlayerPrefs.SetInt(Prefijo + id, 1);
        PlayerPrefs.Save();
    }

    public static bool NivelDesbloqueado(int id)
    {
        if (id < TotalJefes - 1) return true;

        for (int i = 0; i < TotalJefes - 1; i++)
        {
            if (!JefeDerrotado(i)) return false;
        }
        return true;
    }

    public static void Borrar()
    {
        for (int i = 0; i < TotalJefes; i++)
        {
            PlayerPrefs.DeleteKey(Prefijo + i);
        }
        PlayerPrefs.Save();
    }
}