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
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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
        EditorSceneManager.OpenScene(persistencia ? "Assets/_Project/Scenes/LevelSelect.unity" :
            "Assets/_Project/Scenes/MainMenu.unity");
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
        UnityEngine.Object.FindFirstObjectByType<MenuAfiche>().Jugar.onClick.Invoke();
        yield return Esperar(() => Selector() != null && !TransicionPapelAfiche.EnCurso, "selector desde Jugar");
        Check(SceneManager.GetActiveScene().name == "LevelSelect", "Jugar abre el selector desde el afiche inicial");
        CapturarInterfaz("Selector");
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
        var pausa = UnityEngine.Object.FindFirstObjectByType<PauseManager>();
        pausa.Pausar();
        yield return new WaitForSecondsRealtime(.2f);
        CapturarInterfaz("Pausa");
        Check(SesionJuego.EnPausa && Time.timeScale == 0f &&
            UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.name == "BotonContinuar",
            "La pausa congela el juego y enfoca Continuar");
        pausa.Reanudar();
        pausa.Reanudar();
        Check(!SesionJuego.EnPausa && Time.timeScale == 1f, "Continuar restaura el tiempo");

        jugador.Invulnerable = true;
        PrepararSesion();
        pausa.Pausar();
        pausa.Reintentar();
        pausa.VolverAlMenu();
        yield return null;
        yield return null;
        Check(SceneManager.GetActiveScene().name == "Level1_Renato" &&
            !ProgresoJuego.JefeDerrotado(0) && SesionJuego.ProgresoOleadas("Level1_Renato") == 0 &&
            Time.timeScale == 1f && !SesionJuego.EnPausa && SesionJuego.VidasExtra == SesionJuego.VidasIniciales &&
            jugador == null && VidaJefe().VidaActual == VidaJefe().VidaMaxima,
            "Reintentar desde pausa repone jugador, jefe, vidas y oleadas e ignora una segunda accion");

        // La muerte definitiva publica OnMuerte despues de las dos fases y el dialogo.
        Health jefe = VidaJefe();
        typeof(Health).GetProperty("AplazarMuerte", Privado).SetValue(jefe, null);
        jefe.Invulnerable = false;
        jefe.RecibirDanio(jefe.VidaMaxima);
        Check(ProgresoJuego.JefeDerrotado(0), "La muerte definitiva del jefe guarda la victoria inmediatamente");
        Check(GameObject.Find("Resultados del combate") != null, "La victoria de Renato muestra el marcador");
        CapturarInterfaz("Resultados");
        Gestor().VolverASeleccion();
        yield return null;
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
        yield return ProbarMuellePadrePasta();
        pausa = UnityEngine.Object.FindFirstObjectByType<PauseManager>();
        pausa.Pausar();
        pausa.VolverAlMenu();
        yield return null;
        yield return null;
        Check(SceneManager.GetActiveScene().name == "MainMenu" && Time.timeScale == 1f &&
            !SesionJuego.EnPausa && ProgresoJuego.JefeDerrotado(0),
            "Volver al menu desde pausa conserva la victoria y restaura el tiempo");
        TransicionPapelAfiche.Cargar("LevelSelect");
        yield return new WaitForSeconds(1.2f);
        Entradas()[1].boton.onClick.Invoke();
        yield return null;
        yield return null;
        var ataquesPasta = UnityEngine.Object.FindFirstObjectByType<PadrePasta>()
            .transform.Find("Ataques de Padre Pasta");
        yield return Esperar(() => ataquesPasta.GetComponentInChildren<Bullet>() != null, "nota activa antes de la victoria");
        var notaActiva = ataquesPasta.GetComponentInChildren<Bullet>();
        jefe = VidaJefe();
        jefe.Invulnerable = false;
        jefe.RecibirDanio(jefe.VidaMaxima);
        Check(!ataquesPasta.gameObject.activeInHierarchy && !notaActiva.gameObject.activeInHierarchy,
            "La victoria desactiva las notas y los avisos al instante");
        yield return null;
        Check(ataquesPasta == null && notaActiva == null, "La victoria elimina los ataques desactivados");
        Check(ProgresoJuego.PadrePastaFaseUnoSuperada && !ProgresoJuego.JefeDerrotado(1),
            "Padre Pasta conserva la victoria de su fase disponible sin inventar una segunda fase");
        Gestor().ReintentarVictoria();
        yield return null;
        yield return null;
        Check(SceneManager.GetActiveScene().name == "Level2_PadrePasta" &&
            VidaJefe().VidaActual == VidaJefe().VidaMaxima && ProgresoJuego.PadrePastaFaseUnoSuperada,
            "Reintentar desde resultados abre un intento nuevo y conserva el logro");
        Gestor().VolverASeleccion();
        yield return null;
        yield return null;
        Check(SceneManager.GetActiveScene().name == "LevelSelect" && !Entradas()[1].boton.interactable,
            "La fase disponible de Padre Pasta completada tambien queda bloqueada");
        Check(Entradas()[1].etiqueta.text.Contains("Fase 1 superada"), "El selector conserva el estado de fase de Padre Pasta");
        Terminar(null);
    }

    static IEnumerator ProbarMuellePadrePasta()
    {
        var jefe = UnityEngine.Object.FindFirstObjectByType<PadrePasta>();
        var pausa = UnityEngine.Object.FindFirstObjectByType<PauseManager>();
        var mateo = GameObject.FindGameObjectWithTag("Player");
        var salud = mateo.GetComponent<Health>();
        var cuerpo = mateo.GetComponent<Rigidbody2D>();
        var movimiento = mateo.GetComponent<PlayerMovement>();
        var disparos = mateo.GetComponent<PlayerShooting>();
        var senalBorde = (GameObject)typeof(PadrePasta).GetField("senalesHorizontales", Privado).GetValue(jefe);
        var senalCaida = (GameObject)typeof(PadrePasta).GetField("senalesVerticales", Privado).GetValue(jefe);
        var efectos = jefe.transform.Find("Ataques de Padre Pasta");
        Check(efectos != null && jefe.transform.position.x > cuerpo.position.x, "Padre Pasta arranca fijo a la derecha");

        // Invoca el disparo normal en suelo y con la física del salto activa.
        cuerpo.position = new Vector2(2.25f, -3.14f);
        yield return new WaitForFixedUpdate();
        int vidaInicial = VidaJefe().VidaActual;
        var disparar = typeof(PlayerShooting).GetMethod("Disparar", Privado);
        disparar.Invoke(disparos, null);
        yield return new WaitForSeconds(0.35f);
        Check(VidaJefe().VidaActual < vidaInicial, "Mateo alcanza al jefe con el disparo horizontal desde el muelle");
        vidaInicial = VidaJefe().VidaActual;
        typeof(PlayerMovement).GetMethod("Saltar", Privado).Invoke(movimiento, null);
        yield return new WaitForFixedUpdate();
        Check(cuerpo.linearVelocity.y > 0f, "Mateo puede saltar desde la superficie del muelle");
        disparar.Invoke(disparos, null);
        yield return new WaitForSeconds(0.36f);
        Check(VidaJefe().VidaActual < vidaInicial, "Mateo puede acertar mientras está saltando");

        yield return Esperar(() => efectos.Find("Trayectoria del micrófono") != null, "aviso del micrófono");
        Check(!senalBorde.activeSelf && !senalCaida.activeSelf, "El primer aviso frontal aparece solo");
        pausa.Pausar();
        yield return new WaitForSecondsRealtime(0.55f);
        Check(efectos.Find("Trayectoria del micrófono") != null && !efectos.GetComponentInChildren<Bullet>(),
            "Pausar congela el aviso y la salida de notas");
        pausa.Reanudar();
        int vidaMateo = salud.VidaActual;
        cuerpo.position += Vector2.left * 4.25f;
        yield return Esperar(() => efectos.GetComponentInChildren<Bullet>() != null, "nota del micrófono");
        yield return Esperar(() => senalCaida.activeSelf, "aviso del parlante suspendido");
        Check(!senalBorde.activeSelf && efectos.Find("Trayectoria del micrófono") == null,
            "La caída se presenta por separado");
        Check(salud.VidaActual == vidaMateo, "Salir de la trayectoria anunciada esquiva la ráfaga frontal");
        cuerpo.position += Vector2.left * 2.5f;
        yield return Esperar(() => !senalCaida.activeSelf, "impacto del parlante");
        Check(salud.VidaActual == vidaMateo, "Salir de la columna evita el impacto");
        yield return Esperar(() => senalBorde.activeSelf, "aviso del borde");
        Check(!senalCaida.activeSelf, "El parlante del borde anuncia la nota baja por separado");
        // Entra justo después del lanzamiento, para cruzar la nota cerca del
        // ápice del salto real de Mateo.
        cuerpo.position = new Vector2(-7f, -3.14f);
        yield return new WaitForFixedUpdate();
        yield return Esperar(() => !senalBorde.activeSelf, "salida de la nota baja");
        typeof(PlayerMovement).GetMethod("Saltar", Privado).Invoke(movimiento, null);
        yield return new WaitForSeconds(0.5f);
        Check(salud.VidaActual == vidaMateo, "El salto normal esquiva la nota baja");

        VidaJefe().RestaurarVida(VidaJefe().VidaMaxima / 2);
        yield return Esperar(() => senalCaida.activeSelf && efectos.Find("Trayectoria del micrófono") != null,
            "combinación de caída y ráfaga alta");
        cuerpo.position += Vector2.right * 2.5f;
        yield return Esperar(() => !senalCaida.activeSelf, "impacto durante la combinación");
        Check(salud.VidaActual == vidaMateo, "La combinación deja una salida horizontal sobre el muelle");

        pausa.Pausar();
        pausa.Reintentar();
        yield return null;
        yield return null;
        Check(jefe == null && efectos == null && VidaJefe().VidaActual == VidaJefe().VidaMaxima &&
            !SesionJuego.EnPausa && Time.timeScale == 1f, "Reintentar limpia los ataques y restaura la arena");
        pausa = UnityEngine.Object.FindFirstObjectByType<PauseManager>();
        pausa.Pausar();
        pausa.VolverASeleccion();
        yield return null;
        yield return null;
        Check(SceneManager.GetActiveScene().name == "LevelSelect" && Entradas()[1].boton.interactable,
            "Volver a la selección mantiene desbloqueado a Padre Pasta");
        Entradas()[1].boton.onClick.Invoke();
        yield return null;
        yield return null;
    }

    static IEnumerator Esperar(Func<bool> condicion, string descripcion)
    {
        float limite = Time.realtimeSinceStartup + 10f;
        while (!condicion() && Time.realtimeSinceStartup < limite) yield return null;
        if (!condicion()) throw new InvalidOperationException("Tiempo agotado esperando " + descripcion);
    }

    static void CapturarInterfaz(string nombre)
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null || Camera.main == null) return;
        Camera camara = Camera.main;
        Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
            .Where(c => c.renderMode == RenderMode.ScreenSpaceOverlay && c.isRootCanvas).ToArray();
        var capas = canvases.Select(c => c.sortingLayerID).ToArray();
        var ordenes = canvases.Select(c => c.sortingOrder).ToArray();
        var previo = camara.targetTexture;
        var activo = RenderTexture.active;
        float aspecto = camara.aspect;
        foreach (int ancho in new[] {1920, 1280})
        {
            int alto = ancho * 9 / 16;
            var destino = RenderTexture.GetTemporary(ancho, alto, 24);
            try
            {
                camara.targetTexture = destino;
                camara.aspect = ancho / (float)alto;
                for (int i = 0; i < canvases.Length; i++)
                {
                    var canvas = canvases[i];
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = camara;
                    canvas.planeDistance = camara.nearClipPlane + .1f;
                    canvas.sortingLayerID = SortingLayer.layers.Last().id;
                    canvas.sortingOrder = 1000 + ordenes[i];
                }
                Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camara, new UniversalRenderPipeline.SingleCameraRequest { destination = destino });
                RenderTexture.active = destino;
                var imagen = new Texture2D(ancho, alto, TextureFormat.RGB24, false);
                imagen.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0);
                imagen.Apply();
                File.WriteAllBytes($"{Carpeta}/{nombre}-{ancho}x{alto}.png", imagen.EncodeToPNG());
                UnityEngine.Object.Destroy(imagen);
            }
            finally
            {
                for (int i = 0; i < canvases.Length; i++)
                {
                    var canvas = canvases[i];
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.worldCamera = null;
                    canvas.sortingLayerID = capas[i];
                    canvas.sortingOrder = ordenes[i];
                }
                camara.targetTexture = previo;
                camara.aspect = aspecto;
                RenderTexture.active = activo;
                RenderTexture.ReleaseTemporary(destino);
                Canvas.ForceUpdateCanvases();
            }
        }
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
        // El índice de búsqueda del Editor puede fallar en copias recién importadas.
        if (traza.Contains("UnityEditor.Search.SearchInit.IndexationOnStartup")) return;
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
