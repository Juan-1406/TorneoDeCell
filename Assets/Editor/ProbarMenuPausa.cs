using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ProbarMenuPausa
{
    private static string carpeta;
    private static Func<IEnumerator> recorrido;
    private static readonly List<string> Resultados = new List<string>();
    private static double limite;
    private static bool terminado;
    private static Keyboard teclado;
    private static Gamepad mando;
    private static Mouse raton;
    private static InputSettings.BackgroundBehavior focoEditor;
    private static InputSettings.EditorInputBehaviorInPlayMode entradaEditor;

    public static void Run() => Ejecutar("Logs/MenuPausa", Pruebas);

    internal static void Ejecutar(string carpetaDeCapturas, Func<IEnumerator> prueba)
    {
        if (!Application.isBatchMode)
            throw new InvalidOperationException("Ejecutar la prueba de interfaz en batchmode.");

        carpeta = carpetaDeCapturas;
        recorrido = prueba;
        Directory.CreateDirectory(carpeta);
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/LevelSelect.unity");
        Application.logMessageReceived += VigilarErrores;
        EditorApplication.playModeStateChanged += Iniciar;
        EditorApplication.update += VigilarTiempo;
        limite = EditorApplication.timeSinceStartup + 160;
        EditorApplication.isPlaying = true;
    }

    private static void Iniciar(PlayModeStateChange estado)
    {
        if (estado != PlayModeStateChange.EnteredPlayMode) return;

        var anfitrion = new GameObject("Pruebas de pausa");
        UnityEngine.Object.DontDestroyOnLoad(anfitrion);
        anfitrion.AddComponent<Health>().StartCoroutine(EjecutarRecorrido());
    }

    private static IEnumerator EjecutarRecorrido()
    {
        focoEditor = InputSystem.settings.backgroundBehavior;
        entradaEditor = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode =
            InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        teclado = InputSystem.AddDevice<Keyboard>();
        mando = InputSystem.AddDevice<Gamepad>();
        raton = InputSystem.AddDevice<Mouse>();
        yield return null;
        yield return recorrido();
        Terminar(null);
    }

    internal static IEnumerator Pruebas()
    {
        PermitirReintento("Level1_Renato");
        SceneManager.LoadScene("Level1_Renato");
        yield return null;
        yield return new WaitForSeconds(2.3f);
        yield return Capturar("Renato-HUD", 1080, 2340);

        // Un control detrás de la pausa detecta fugas de selección y de clics.
        Button controlPrevio = CrearControlDeFondo();
        int clicsDeFondo = 0;
        controlPrevio.onClick.AddListener(() => clicsDeFondo++);
        EventSystem.current.SetSelectedGameObject(controlPrevio.gameObject);
        yield return Tecla(Key.Escape);
        var botones = BotonesDePausa();
        Check(SesionJuego.EnPausa && Time.timeScale == 0f && botones.Length == 3,
            "Escape abre la pausa y detiene el combate con las tres opciones.");
        ComprobarFoco(botones[0], "Continuar recibe el foco con un solo rombo.");
        yield return Capturar("Renato-Continuar", 1280, 720, () => ComprobarTamano(1280, 720));
        yield return Capturar("Renato-Continuar", 1920, 1080, () => ComprobarTamano(1920, 1080));

        yield return Tecla(Key.DownArrow);
        ComprobarFoco(botones[1], "Flecha abajo pasa a Reintentar.");
        yield return Capturar("Renato-Reintentar", 2560, 1080, () => ComprobarTamano(2560, 1080));
        yield return Tecla(Key.S);
        ComprobarFoco(botones[2], "S pasa a Volver al menú principal.");
        yield return Capturar("Renato-Volver", 1080, 2340, () => ComprobarTamano(1080, 2340));
        yield return Tecla(Key.DownArrow);
        yield return Tecla(Key.LeftArrow);
        ComprobarFoco(botones[2], "La navegación se detiene en los límites de la pausa.");

        yield return Clic(new Vector2(8f, 8f));
        ComprobarFoco(botones[2], "El clic en el fondo conserva el foco.");
        Check(clicsDeFondo == 0, "El panel bloquea clics sobre controles detrás de la pausa.");
        Vector2 centro = RectTransformUtility.WorldToScreenPoint(null, botones[0].transform.position);
        InputSystem.QueueStateEvent(raton, new MouseState { position = centro });
        yield return null;
        yield return null;
        ComprobarFoco(botones[2], "Pasar el ratón por Continuar conserva la selección de teclado.");
        Check(botones[0].targetGraphic.canvasRenderer.GetColor() == botones[0].colors.highlightedColor,
            "El resaltado del puntero tiene un estado propio.");

        yield return Tecla(Key.W);
        ComprobarFoco(botones[1], "W sube a Reintentar.");
        yield return EstadoMando(new GamepadState().WithButton(GamepadButton.DpadUp));
        ComprobarFoco(botones[0], "La cruceta sube a Continuar.");
        yield return EstadoMando(new GamepadState { leftStick = Vector2.down });
        ComprobarFoco(botones[1], "La palanca baja a Reintentar.");
        yield return EstadoMando(new GamepadState().WithButton(GamepadButton.DpadUp));
        yield return EstadoMando(new GamepadState().WithButton(GamepadButton.South));
        Check(!SesionJuego.EnPausa && Time.timeScale == 1f &&
            EventSystem.current.currentSelectedGameObject == controlPrevio.gameObject,
            "El botón sur confirma Continuar y devuelve el foco anterior.");
        UnityEngine.Object.Destroy(controlPrevio.gameObject);
        yield return null;

        yield return EstadoMando(new GamepadState().WithButton(GamepadButton.East));
        botones = BotonesDePausa();
        ComprobarFoco(botones[0], "El botón este abre la pausa y vuelve a enfocar Continuar.");
        yield return EstadoMando(new GamepadState().WithButton(GamepadButton.East));
        Check(!SesionJuego.EnPausa && Time.timeScale == 1f, "El botón este también reanuda.");
        yield return Tecla(Key.Escape);
        botones = BotonesDePausa();
        yield return Clic(RectTransformUtility.WorldToScreenPoint(null, botones[0].transform.position));
        Check(!SesionJuego.EnPausa && Time.timeScale == 1f, "Un clic en Continuar reanuda.");
        yield return Tecla(Key.Escape);
        yield return Tecla(Key.Escape);
        Check(!SesionJuego.EnPausa && Time.timeScale == 1f, "Escape también reanuda.");

        var pausa = UnityEngine.Object.FindFirstObjectByType<PauseManager>();
        PermitirReintento("Level1_Renato");
        yield return Tecla(Key.Escape);
        yield return Tecla(Key.S);
        yield return Tecla(Key.Enter);
        Check(pausa == null && SceneManager.GetActiveScene().name == "Level1_Renato" && Time.timeScale == 1f,
            "Enter sobre Reintentar vuelve a cargar Renato.");
        yield return Tecla(Key.Escape);
        yield return Tecla(Key.S);
        yield return Tecla(Key.S);
        yield return Tecla(Key.Enter);
        Check(SceneManager.GetActiveScene().name == "MainMenu" && !SesionJuego.EnPausa,
            "Volver al menú principal carga MainMenu.");

        PermitirReintento("Level2_PadrePasta");
        SceneManager.LoadScene("Level2_PadrePasta");
        yield return null;
        yield return null;
        yield return EstadoMando(new GamepadState().WithButton(GamepadButton.East));
        botones = BotonesDePausa();
        ComprobarFoco(botones[0], "Padre Pasta abre con Continuar seleccionado.");
        pausa = UnityEngine.Object.FindFirstObjectByType<PauseManager>();
        yield return EstadoMando(new GamepadState().WithButton(GamepadButton.DpadDown));
        yield return EstadoMando(new GamepadState().WithButton(GamepadButton.South));
        Check(pausa == null && SceneManager.GetActiveScene().name == "Level2_PadrePasta" && Time.timeScale == 1f,
            "Reintentar desde el mando vuelve a cargar Padre Pasta.");
        yield return EstadoMando(new GamepadState().WithButton(GamepadButton.East));
        botones = BotonesDePausa();
        yield return EstadoMando(new GamepadState().WithButton(GamepadButton.DpadDown));
        yield return EstadoMando(new GamepadState().WithButton(GamepadButton.DpadDown));
        ComprobarFoco(botones[2], "Padre Pasta conserva su opción Volver a la selección.");
        yield return Capturar("PadrePasta-Volver", 1920, 1080, () => ComprobarTamano(1920, 1080));
        yield return EstadoMando(new GamepadState().WithButton(GamepadButton.South));
        Check(SceneManager.GetActiveScene().name == "LevelSelect" && !SesionJuego.EnPausa,
            "Confirmar Volver a la selección carga LevelSelect.");
    }

    private static Button CrearControlDeFondo()
    {
        var canvas = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
            .Single(c => c.isRootCanvas && c.GetComponent<GraphicRaycaster>() != null);
        var control = new GameObject("Control detrás de pausa", typeof(RectTransform), typeof(Image), typeof(Button));
        var rect = (RectTransform)control.transform;
        rect.SetParent(canvas.transform, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
        rect.sizeDelta = new Vector2(64f, 64f);
        control.GetComponent<Image>().color = Color.clear;
        return control.GetComponent<Button>();
    }

    private static Button[] BotonesDePausa() => GameObject.Find("PanelPausa")?.GetComponentsInChildren<Button>()
        ?? throw new InvalidOperationException("La pausa no está visible.");

    private static void ComprobarFoco(Button esperado, string mensaje)
    {
        bool RomboVisible(Button boton) => boton.transform.Find("Indicador de selección").GetComponent<Image>().enabled;
        Check(EventSystem.current.currentSelectedGameObject == esperado.gameObject &&
            RomboVisible(esperado) && BotonesDePausa().Count(RomboVisible) == 1, mensaje);
    }

    internal static IEnumerator Tecla(Key tecla)
    {
        InputSystem.QueueStateEvent(teclado, new KeyboardState(tecla));
        yield return null;
        yield return null;
        InputSystem.QueueStateEvent(teclado, new KeyboardState());
        yield return null;
        yield return null;
    }

    internal static IEnumerator EstadoMando(GamepadState estado)
    {
        InputSystem.QueueStateEvent(mando, estado);
        yield return null;
        yield return null;
        InputSystem.QueueStateEvent(mando, new GamepadState());
        yield return null;
        yield return null;
    }

    internal static IEnumerator Clic(Vector2 posicion)
    {
        InputSystem.QueueStateEvent(raton, new MouseState { position = posicion }.WithButton(MouseButton.Left));
        yield return null;
        yield return null;
        InputSystem.QueueStateEvent(raton, new MouseState { position = posicion });
        yield return null;
        yield return null;
    }

    internal static void PermitirReintento(string escena)
    {
        if (ProgresoJuego.PuedeCargarEscena(escena)) return;
        // Usa el permiso de repetición de la sesión sin modificar el progreso guardado.
        typeof(SesionJuego).GetMethod("HabilitarRepeticion", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { escena });
    }

    private static void ComprobarTamano(int ancho, int alto)
    {
        Transform panel = GameObject.Find("PanelPausa").transform;
        var marco = (RectTransform)panel.Find("Área segura de pausa/Contenido/Marco de pausa");
        ComprobarMenu(marco, ancho, alto);
    }

    internal static void ComprobarMenu(RectTransform marco, int ancho, int alto)
    {
        Button[] botones = marco.GetComponentsInChildren<Button>();
        Camera camara = marco.GetComponentInParent<Canvas>().worldCamera;
        var pantalla = RectEnPantalla(marco, camara);
        Check(pantalla.xMin > 0 && pantalla.yMin > 0 && pantalla.xMax < ancho && pantalla.yMax < alto &&
            Vector2.Distance(pantalla.center, new Vector2(ancho, alto) * .5f) < 2f,
            $"{ancho}×{alto} mantiene el marco centrado dentro de la pantalla.");
        float filaAnterior = pantalla.yMax;
        foreach (Button boton in botones)
        {
            var fila = RectEnPantalla((RectTransform)boton.transform, camara);
            var texto = boton.GetComponentInChildren<TMP_Text>();
            texto.ForceMeshUpdate();
            float fuenteEnPixeles = texto.fontSize * fila.height / ((RectTransform)boton.transform).rect.height;
            Check(fila.xMin > pantalla.xMin && fila.xMax < pantalla.xMax &&
                fila.yMin > pantalla.yMin && fila.yMax < filaAnterior &&
                !texto.isTextOverflowing && fuenteEnPixeles >= (alto == 1080 ? 26f : 18f),
                $"{ancho}×{alto} deja «{texto.text}» legible y sin solaparse.");
            filaAnterior = fila.yMin;
        }
    }

    internal static Rect RectEnPantalla(RectTransform rect, Camera camara)
    {
        var esquinas = new Vector3[4];
        rect.GetWorldCorners(esquinas);
        Vector2 min = RectTransformUtility.WorldToScreenPoint(camara, esquinas[0]);
        Vector2 max = RectTransformUtility.WorldToScreenPoint(camara, esquinas[2]);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    internal static void Check(bool condicion, string mensaje)
    {
        if (!condicion) throw new InvalidOperationException(mensaje);
        Resultados.Add("PASS " + mensaje);
    }

    internal static IEnumerator Capturar(string nombre, int ancho, int alto, Action comprobar = null)
    {
        Camera camara = Camera.main;
        if (camara == null || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            throw new InvalidOperationException("La captura necesita una cámara y un renderizador activo.");

        Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
            .Where(c => c.renderMode == RenderMode.ScreenSpaceOverlay && c.isRootCanvas).ToArray();
        int[] capas = canvases.Select(c => c.sortingLayerID).ToArray();
        int[] ordenes = canvases.Select(c => c.sortingOrder).ToArray();
        RenderTexture destino = RenderTexture.GetTemporary(ancho, alto, 24);
        RenderTexture texturaPrevia = camara.targetTexture;
        RenderTexture activo = RenderTexture.active;
        float aspecto = camara.aspect;

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

            yield return null;
            Canvas.ForceUpdateCanvases();
            comprobar?.Invoke();
            RenderPipeline.SubmitRenderRequest(camara,
                new UniversalRenderPipeline.SingleCameraRequest { destination = destino });
            RenderTexture.active = destino;
            var imagen = new Texture2D(ancho, alto, TextureFormat.RGB24, false);
            imagen.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0);
            imagen.Apply();
            File.WriteAllBytes($"{carpeta}/{nombre}-{ancho}x{alto}.png", imagen.EncodeToPNG());
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

            camara.targetTexture = texturaPrevia;
            camara.aspect = aspecto;
            RenderTexture.active = activo;
            RenderTexture.ReleaseTemporary(destino);
        }
    }

    private static void VigilarTiempo()
    {
        if (EditorApplication.timeSinceStartup > limite) Terminar("Tiempo agotado en la prueba de interfaz.");
    }

    private static void VigilarErrores(string mensaje, string traza, LogType tipo)
    {
        if (tipo == LogType.Exception) Terminar(mensaje + "\n" + traza);
    }

    private static void Terminar(string error)
    {
        if (terminado) return;
        terminado = true;
        Time.timeScale = 1f;
        if (teclado != null) InputSystem.RemoveDevice(teclado);
        if (mando != null) InputSystem.RemoveDevice(mando);
        if (raton != null) InputSystem.RemoveDevice(raton);
        InputSystem.settings.backgroundBehavior = focoEditor;
        InputSystem.settings.editorInputBehaviorInPlayMode = entradaEditor;
        Application.logMessageReceived -= VigilarErrores;
        EditorApplication.playModeStateChanged -= Iniciar;
        EditorApplication.update -= VigilarTiempo;
        Resultados.Add(error == null ? $"OK {Resultados.Count} comprobaciones." : $"FAIL {error}");
        File.WriteAllLines(carpeta + "/Pruebas.txt", Resultados);
        if (error != null) Debug.LogError(error);
        EditorApplication.isPlaying = false;
        EditorApplication.delayCall += () => EditorApplication.Exit(error == null ? 0 : 1);
    }
}
