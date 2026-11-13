using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Dos ejecuciones separadas verifican que el progreso sobreviva al cierre de Unity.
public static class ProbarProgresoJuego
{
    const string Carpeta = "Logs/ProgresoJuego";
    const string Respaldo = Carpeta + "/PreferenciasOriginales.json";
    const string AjusteAjeno = "prueba_progreso_ajuste_ajeno";
    static readonly string[] Claves =
    {
        "jefe_derrotado_0", "jefe_derrotado_1", "jefe_derrotado_2", "jefe_derrotado_3",
        "padre_pasta_fase_1_superada", AjusteAjeno
    };
    static readonly List<string> checks = new List<string>();
    static readonly BindingFlags Privado = BindingFlags.Instance | BindingFlags.NonPublic;
    static bool terminado, verificarPersistencia;
    static double inicio;

    [Serializable]
    public class Preferencias
    {
        public bool[] existen;
        public int[] valores;
    }

    public static void Run()
    {
        ExigirBatch();
        Directory.CreateDirectory(Carpeta);
        if (File.Exists(Respaldo))
            throw new InvalidOperationException("Hay un respaldo pendiente. Ejecutar VerificarPersistencia o RestaurarPreferencias.");
        var copia = new Preferencias { existen = new bool[Claves.Length], valores = new int[Claves.Length] };
        for (int i = 0; i < Claves.Length; i++)
        {
            copia.existen[i] = PlayerPrefs.HasKey(Claves[i]);
            copia.valores[i] = PlayerPrefs.GetInt(Claves[i]);
        }
        File.WriteAllText(Respaldo, JsonUtility.ToJson(copia, true));
        ProgresoJuego.Borrar();
        PlayerPrefs.SetInt(AjusteAjeno, 73);
        PlayerPrefs.Save();
        Iniciar(false);
    }

    public static void VerificarPersistencia()
    {
        ExigirBatch();
        if (!File.Exists(Respaldo)) throw new InvalidOperationException("Ejecutar Run primero.");
        Iniciar(true);
    }

    public static void RestaurarPreferencias()
    {
        ExigirBatch();
        Restaurar();
        EditorApplication.Exit(0);
    }

    static void ExigirBatch()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Usar Unity en batchmode.");
    }

    static void Iniciar(bool persistencia)
    {
        verificarPersistencia = persistencia;
        checks.Clear();
        terminado = false;
        inicio = EditorApplication.timeSinceStartup;
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/LevelSelect.unity");
        Application.logMessageReceived += Log;
        EditorApplication.update += Vigilar;
        EditorApplication.playModeStateChanged += AlEntrar;
        EditorApplication.isPlaying = true;
    }

    static void AlEntrar(PlayModeStateChange cambio)
    {
        if (cambio != PlayModeStateChange.EnteredPlayMode) return;
        var go = new GameObject("Pruebas de progreso");
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<Health>().StartCoroutine(verificarPersistencia ? Persistencia() : Partida());
    }

    static LevelSelectUI Selector() => UnityEngine.Object.FindFirstObjectByType<LevelSelectUI>();
    static LevelSelectUI.NivelEntrada[] Entradas() =>
        (LevelSelectUI.NivelEntrada[])typeof(LevelSelectUI).GetField("niveles", Privado).GetValue(Selector());
    static SceneLoader Cargador() => UnityEngine.Object.FindFirstObjectByType<SceneLoader>();
    static LevelManager Gestor() => UnityEngine.Object.FindFirstObjectByType<LevelManager>();
    static Health VidaJefe() => (Health)typeof(LevelManager).GetField("jefe", Privado).GetValue(Gestor());
    static void PulsarReinicio()
    {
        Button boton = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
            .Single(b => Enumerable.Range(0, b.onClick.GetPersistentEventCount())
                .Any(i => b.onClick.GetPersistentMethodName(i) == "ReiniciarProgreso"));
        boton.onClick.Invoke();
    }
    static void PrepararSesion()
    {
        SesionJuego.VidasExtra = 1;
        SesionJuego.EnPausa = true;
        SesionJuego.GuardarProgresoOleadas("Level1_Renato", 2);
        SesionJuego.GuardarVidaJefe("Level1_Renato", 55);
    }
    static void ComprobarReinicio()
    {
        Check(Enumerable.Range(0, 4).All(i => !ProgresoJuego.JefeDerrotado(i)) &&
            !ProgresoJuego.PadrePastaFaseUnoSuperada, "Reiniciar borra todos los jefes y la fase de Padre Pasta");
        Check(SesionJuego.VidasExtra == SesionJuego.VidasIniciales && !SesionJuego.EnPausa,
            "Reiniciar restaura las vidas y limpia la pausa");
        Check(SesionJuego.ProgresoOleadas("Level1_Renato") == 0 &&
            !SesionJuego.IntentarRestaurarVidaJefe("Level1_Renato", out _),
            "Reiniciar elimina las oleadas y la vida pendiente del jefe");
        Check(Entradas()[0].boton.interactable && !Entradas()[1].boton.interactable,
            "Reiniciar habilita a Renato y vuelve a bloquear Padre Pasta");
        Check(PlayerPrefs.GetInt(AjusteAjeno) == 73, "Reiniciar conserva preferencias ajenas al progreso");
    }

    static IEnumerator Partida()
    {
        yield return null;
        Check(Entradas()[0].boton.interactable && !Entradas()[1].boton.interactable,
            "Una partida nueva permite Renato y exige su victoria para Padre Pasta");
        Check(!Entradas()[2].boton.interactable && !Entradas()[3].boton.interactable,
            "Los niveles no disponibles siguen bloqueados");
        PrepararSesion();
        Cargador().CargarEscena("Level2_PadrePasta");
        yield return null;
        Check(SceneManager.GetActiveScene().name == "LevelSelect" && SesionJuego.VidasExtra == 1,
            "El cargador rechaza niveles bloqueados sin alterar la partida");
        PulsarReinicio();
        ComprobarReinicio();

        // Comprueba tambien los jefes futuros sin requerir sus escenas aun inexistentes.
        for (int i = 0; i < 4; i++)
        {
            ProgresoJuego.MarcarDerrotado(i);
            Check(!ProgresoJuego.PuedeJugarNivel(i), "El jefe " + i + " completado no se puede repetir");
        }
        ProgresoJuego.MarcarPadrePastaFaseUnoSuperada();
        PulsarReinicio();
        ComprobarReinicio();

        Entradas()[0].boton.onClick.Invoke();
        yield return null;
        yield return null;
        Check(SceneManager.GetActiveScene().name == "Level1_Renato", "El boton de Renato abre una partida nueva");
        var jugador = GameObject.FindGameObjectWithTag("Player").GetComponent<Health>();
        jugador.Invulnerable = true;
        SesionJuego.GuardarProgresoOleadas("Level1_Renato", 2);
        Gestor().ReintentarJuego();
        yield return null;
        yield return null;
        Check(SceneManager.GetActiveScene().name == "Level1_Renato" &&
            !ProgresoJuego.JefeDerrotado(0) && SesionJuego.ProgresoOleadas("Level1_Renato") == 0,
            "Un nivel sin completar se puede reintentar");

        // La muerte definitiva publica OnMuerte despues de las dos fases y el dialogo.
        Health jefe = VidaJefe();
        typeof(Health).GetProperty("AplazarMuerte", Privado).SetValue(jefe, null);
        jefe.Invulnerable = false;
        jefe.RecibirDanio(jefe.VidaMaxima);
        Check(ProgresoJuego.JefeDerrotado(0), "La muerte definitiva del jefe guarda la victoria inmediatamente");
        yield return new WaitForSeconds(2.2f);
        yield return null;
        Check(SceneManager.GetActiveScene().name == "LevelSelect", "La victoria regresa al selector");
        Check(!Entradas()[0].boton.interactable && Entradas()[1].boton.interactable,
            "La victoria bloquea Renato y desbloquea Padre Pasta");
        Check(Entradas()[0].etiqueta.text.Contains("COMPLETADO") &&
            Entradas()[0].etiqueta.text.Contains("Reinicia"), "El nivel completado explica como volver a jugar");
        Cargador().CargarEscena("Level1_Renato");
        Entradas()[0].boton.onClick.Invoke();
        yield return null;
        Check(SceneManager.GetActiveScene().name == "LevelSelect", "Ni el cargador ni un evento de boton repiten Renato completado");
        SceneManager.LoadScene("Level1_Renato");
        yield return null;
        yield return null;
        Check(SceneManager.GetActiveScene().name == "LevelSelect", "La entrada directa a un nivel completado vuelve al selector");

        Cargador().CargarEscena("MainMenu");
        yield return null;
        yield return null;
        Check(ProgresoJuego.JefeDerrotado(0), "Volver al menu conserva la victoria");
        TransicionPapelAfiche.Cargar("LevelSelect");
        yield return new WaitForSeconds(1.2f);
        Check(!Entradas()[0].boton.interactable, "Volver del menu conserva el bloqueo");

        Entradas()[1].boton.onClick.Invoke();
        yield return null;
        yield return null;
        Check(SceneManager.GetActiveScene().name == "Level2_PadrePasta", "Padre Pasta se puede jugar tras vencer a Renato");
        jefe = VidaJefe();
        jefe.Invulnerable = false;
        jefe.RecibirDanio(jefe.VidaMaxima);
        Check(ProgresoJuego.PadrePastaFaseUnoSuperada && !ProgresoJuego.JefeDerrotado(1),
            "Padre Pasta conserva la victoria de su fase disponible sin inventar una segunda fase");
        Gestor().ReintentarJuego();
        yield return null;
        yield return null;
        Check(SceneManager.GetActiveScene().name == "LevelSelect" && !Entradas()[1].boton.interactable,
            "La fase disponible de Padre Pasta completada tambien queda bloqueada");
        Check(Entradas()[1].etiqueta.text.Contains("Fase 1 superada"), "El selector conserva el estado de fase de Padre Pasta");
        Terminar(null);
    }

    static IEnumerator Persistencia()
    {
        yield return null;
        Check(ProgresoJuego.JefeDerrotado(0) && ProgresoJuego.PadrePastaFaseUnoSuperada,
            "Las victorias de ambos niveles sobreviven al cierre y reapertura de Unity");
        Check(!Entradas()[0].boton.interactable && !Entradas()[1].boton.interactable,
            "Al abrir el juego de nuevo ambos niveles siguen bloqueados");
        SceneManager.LoadScene("Level2_PadrePasta");
        yield return null;
        yield return null;
        Check(SceneManager.GetActiveScene().name == "LevelSelect", "La entrada directa a Padre Pasta completado tambien queda bloqueada");
        PrepararSesion();
        PulsarReinicio();
        ComprobarReinicio();
        Entradas()[0].boton.onClick.Invoke();
        yield return null;
        yield return null;
        Check(SceneManager.GetActiveScene().name == "Level1_Renato" && VidaJefe().VidaActual == VidaJefe().VidaMaxima,
            "Tras reiniciar se puede volver a jugar desde cero con el jefe a vida completa");
        Terminar(null);
    }

    static void Check(bool condicion, string mensaje)
    {
        if (!condicion) throw new Exception(mensaje);
        checks.Add("PASS: " + mensaje);
    }
    static void Vigilar()
    {
        if (!terminado && EditorApplication.timeSinceStartup - inicio > 100) Terminar("Tiempo agotado");
    }
    static void Log(string mensaje, string traza, LogType tipo)
    {
        if (!terminado && (tipo == LogType.Error || tipo == LogType.Exception)) Terminar(mensaje + "\n" + traza);
    }
    static void Restaurar()
    {
        if (!File.Exists(Respaldo)) return;
        var copia = JsonUtility.FromJson<Preferencias>(File.ReadAllText(Respaldo));
        for (int i = 0; i < Claves.Length; i++)
        {
            if (copia.existen[i]) PlayerPrefs.SetInt(Claves[i], copia.valores[i]);
            else PlayerPrefs.DeleteKey(Claves[i]);
        }
        PlayerPrefs.Save();
        File.Delete(Respaldo);
    }
    static void Terminar(string error)
    {
        if (terminado) return;
        terminado = true;
        checks.Add(error == null ? "OK: " + checks.Count + " comprobaciones." : "FAIL: " + error);
        File.WriteAllLines(Carpeta + (verificarPersistencia ? "/Persistencia.txt" : "/Partida.txt"), checks);
        Application.logMessageReceived -= Log;
        EditorApplication.update -= Vigilar;
        EditorApplication.playModeStateChanged -= AlEntrar;
        if (error != null || verificarPersistencia) Restaurar();
        EditorApplication.isPlaying = false;
        EditorApplication.delayCall += () => EditorApplication.Exit(error == null ? 0 : 1);
    }
}
