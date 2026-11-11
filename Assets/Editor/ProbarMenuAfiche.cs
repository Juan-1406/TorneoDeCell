using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

public static class ProbarMenuAfiche
{
    static readonly List<string> checks = new List<string>();
    static bool terminado;
    static double inicio;
    static string etapa;
    static Keyboard teclado;
    static Gamepad mando;
    static Mouse mouse;
    static InputSettings.BackgroundBehavior comportamientoFoco;
    static InputSettings.EditorInputBehaviorInPlayMode entradaEditor;

    public static void Run()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Ejecutar estas pruebas en batchmode.");
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/MainMenu.unity");
        inicio = EditorApplication.timeSinceStartup; etapa = "abrir menú";
        comportamientoFoco = InputSystem.settings.backgroundBehavior;
        entradaEditor = InputSystem.settings.editorInputBehaviorInPlayMode;
        EditorApplication.playModeStateChanged += Iniciar;
        EditorApplication.update += Vigilar;
        Application.logMessageReceived += Log;
        EditorApplication.isPlaying = true;
    }
    static void Iniciar(PlayModeStateChange estado)
    {
        if (estado != PlayModeStateChange.EnteredPlayMode) return;
        var go = new GameObject("Comprobaciones de menú");
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<Health>().StartCoroutine(Pruebas());
    }
    static IEnumerator Pruebas()
    {
        yield return null;
        var menu = UnityEngine.Object.FindFirstObjectByType<MenuAfiche>();
        Check(menu != null, "MainMenu arranca con el afiche animado");
        Check(UnityEngine.Object.FindObjectsByType<BotonAfiche>(FindObjectsSortMode.None).Length == 2,
            "Solo existen las dos acciones reales: Jugar y Salir");
        Check(menu.GetComponentsInChildren<RecorteAfiche>().Length == 6,
            "Cuatro retratos humanos independientes y dos botones; sin jugador ni Cell adicionales");
        Check(menu.GetComponentsInChildren<RecorteAfiche>().All(r => r.canvasRenderer != null),
            "Todos los recortes cuentan con su representación gráfica de UI");
        Check(!ShaderUtil.ShaderHasError(Shader.Find("Torneo/UI/BotonAfiche")) &&
            !ShaderUtil.ShaderHasError(Shader.Find("Torneo/UI/TituloAfiche")),
            "Los materiales del título y la selección compilan sin errores");
        Check(EventSystem.current.currentSelectedGameObject == menu.Jugar.gameObject,
            "Jugar recibe el foco inicial");
        Check(menu.Jugar.navigation.selectOnDown == menu.Salir && menu.Salir.navigation.selectOnUp == menu.Jugar,
            "La navegación conecta ambas acciones en las dos direcciones");
        yield return new WaitForSecondsRealtime(.13f);
        Capturar("01-entrada", 1280,720);
        yield return new WaitForSecondsRealtime(1.05f);
        Check(menu.Jugar.IntensidadSeleccion > .98f && menu.Salir.IntensidadSeleccion < .02f,
            "Solo Jugar tiene el destaque dorado al iniciar");
        Check(menu.GetComponents<AudioSource>().Any(a => a.loop && a.isPlaying && a.clip != null),
            "La base instrumental comienza y está configurada para repetirse");
        Capturar("02-jugar-seleccionado", 1920,1080);

        etapa = "navegación con teclado y mando";
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        teclado = InputSystem.AddDevice<Keyboard>(); mando = InputSystem.AddDevice<Gamepad>();
        yield return null;
        InputSystem.QueueStateEvent(teclado,new KeyboardState(Key.DownArrow));
        yield return null; yield return null;
        InputSystem.QueueStateEvent(teclado,new KeyboardState());
        yield return new WaitForSecondsRealtime(.24f);
        Check(EventSystem.current.currentSelectedGameObject == menu.Salir.gameObject,
            "Flecha abajo de un teclado de Input System selecciona Salir");
        Check(menu.Salir.IntensidadSeleccion > .98f && menu.Jugar.IntensidadSeleccion < .02f,
            "El dorado se traslada a Salir y Jugar vuelve al gris");
        Capturar("03-salir-seleccionado",1920,1080);
        InputSystem.QueueStateEvent(mando,new GamepadState().WithButton(GamepadButton.DpadUp));
        yield return null; yield return null;
        InputSystem.QueueStateEvent(mando,new GamepadState());
        yield return new WaitForSecondsRealtime(.24f);
        Check(EventSystem.current.currentSelectedGameObject == menu.Jugar.gameObject,
            "La cruceta de un mando de Input System vuelve a Jugar");

        etapa = "mouse, tiempo independiente y formatos";
        mouse = InputSystem.AddDevice<Mouse>();
        InputSystem.QueueStateEvent(mouse,new MouseState { position=new Vector2(8,8) }.WithButton(MouseButton.Left));
        yield return null; yield return null;
        InputSystem.QueueStateEvent(mouse,new MouseState { position=new Vector2(8,8) });
        yield return null;
        Check(EventSystem.current.currentSelectedGameObject == menu.Jugar.gameObject,
            "Un clic en el muro conserva la selección y permite seguir con teclado o mando");
        InputSystem.RemoveDevice(mouse); mouse = null;
        var pointer = new PointerEventData(EventSystem.current);
        ExecuteEvents.Execute(menu.Salir.gameObject,pointer,ExecuteEvents.pointerEnterHandler);
        yield return new WaitForSecondsRealtime(.2f);
        Check(menu.Salir.Seleccionado && !menu.Jugar.Seleccionado,
            "El mouse cambia la selección sin dejar dos acciones activas");
        ExecuteEvents.Execute(menu.Jugar.gameObject,pointer,ExecuteEvents.pointerEnterHandler);
        yield return new WaitForSecondsRealtime(.2f);
        Vector2 centro = RectTransformUtility.WorldToScreenPoint(null,menu.Jugar.transform.position);
        pointer.position = centro;
        var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer,hits);
        Check(hits.Any(h => h.gameObject.GetComponentInParent<BotonAfiche>() == menu.Jugar),
            "La zona visible del botón recibe los clics; el arte no bloquea el mouse");
        RectTransform retrato = menu.GetComponentsInChildren<RecorteAfiche>().First(r => r.name.StartsWith("01")).rectTransform;
        Vector2 antes = retrato.anchoredPosition;
        Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(.3f);
        Check(Vector2.Distance(antes,retrato.anchoredPosition) > .03f,
            "El afiche continúa animado aunque el tiempo de combate esté detenido");
        Time.timeScale = 1;
        Capturar("04-pantalla-4x3",1024,768);
        Capturar("05-pantalla-ultrapanoramica",2560,1080);
        Capturar("06-pantalla-16x10",1440,900);

        etapa = "confirmación y transición";
        // La confirmación recorre la misma acción que Enter y el botón sur del mando.
        InputSystem.QueueStateEvent(mando,new GamepadState().WithButton(GamepadButton.South));
        yield return null; yield return null;
        InputSystem.QueueStateEvent(mando,new GamepadState());
        Check(menu.EnTransicion && !menu.Jugar.interactable && !menu.Salir.interactable,
            "Confirmar desde el mando bloquea nuevas pulsaciones y comienza la transición");
        menu.JugarAhora();
        yield return new WaitForSecondsRealtime(.34f);
        Check(TransicionPapelAfiche.EnCurso,"La transición de papel cubre la salida del afiche");
        Capturar("07-transicion-papel",1280,720);
        float limite = Time.realtimeSinceStartup+15;
        while ((SceneManager.GetActiveScene().name != "LevelSelect" || TransicionPapelAfiche.EnCurso) && Time.realtimeSinceStartup < limite)
            yield return null;
        Check(SceneManager.GetActiveScene().name == "LevelSelect" && !TransicionPapelAfiche.EnCurso,
            "Jugar descubre LevelSelect y retira la transición sin duplicarla");
        Check(UnityEngine.Object.FindFirstObjectByType<MenuAfiche>() == null,
            "El audio y los elementos del menú no persisten sobre los niveles");
        Capturar("08-seleccion-de-nivel",1280,720);
        SceneManager.LoadScene("MainMenu");
        yield return null; yield return new WaitForSecondsRealtime(.85f);
        menu = UnityEngine.Object.FindFirstObjectByType<MenuAfiche>();
        Check(menu != null && menu.Jugar.Seleccionado && !TransicionPapelAfiche.EnCurso,
            "Volver al inicio reconstruye un menú usable con Jugar seleccionado");
        Check(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 1,
            "No quedan cámaras o escuchas de audio duplicadas al volver");
        Terminar(null);
    }

    static void Capturar(string nombre,int ancho,int alto)
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) throw new InvalidOperationException("La verificación visual necesita GPU.");
        var cam = Camera.main;
        var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
            .Where(c => c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
        var rt = RenderTexture.GetTemporary(ancho,alto,24);
        var activo = RenderTexture.active; var target = cam.targetTexture;
        float aspect = cam.aspect;
        try
        {
            cam.targetTexture = rt; cam.aspect = ancho/(float)alto;
            foreach (var c in canvases) { c.renderMode=RenderMode.ScreenSpaceCamera; c.worldCamera=cam; c.planeDistance=1; }
            Canvas.ForceUpdateCanvases();
            foreach (var m in UnityEngine.Object.FindObjectsByType<MenuAfiche>(FindObjectsSortMode.None)) m.ActualizarComposicion();
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest { destination=rt });
            RenderTexture.active=rt;
            var tex = new Texture2D(ancho,alto,TextureFormat.RGB24,false);
            tex.ReadPixels(new Rect(0,0,ancho,alto),0,0); tex.Apply();
            File.WriteAllBytes("Logs/MenuAfiche/"+nombre+".png",tex.EncodeToPNG());
            UnityEngine.Object.Destroy(tex);
        }
        finally
        {
            foreach (var c in canvases) { c.renderMode=RenderMode.ScreenSpaceOverlay; c.worldCamera=null; }
            cam.targetTexture=target; cam.aspect=aspect; RenderTexture.active=activo; RenderTexture.ReleaseTemporary(rt);
            Canvas.ForceUpdateCanvases();
        }
    }
    static void Check(bool valor,string texto)
    {
        if (!valor) throw new Exception(texto);
        checks.Add("PASS: "+texto);
        File.WriteAllLines("Logs/MenuAfiche/Pruebas.txt",checks);
    }
    static void Vigilar()
    {
        if (!terminado && EditorApplication.timeSinceStartup-inicio > 150) Terminar("Tiempo excedido en "+etapa);
    }
    static void Log(string mensaje,string stack,LogType tipo)
    {
        if (!terminado && (tipo == LogType.Exception || tipo == LogType.Error)) Terminar(etapa+": "+mensaje+"\n"+stack);
    }
    static void Terminar(string error)
    {
        if (terminado) return; terminado=true;
        if (teclado != null) InputSystem.RemoveDevice(teclado);
        if (mando != null) InputSystem.RemoveDevice(mando);
        if (mouse != null) InputSystem.RemoveDevice(mouse);
        InputSystem.settings.backgroundBehavior = comportamientoFoco;
        InputSystem.settings.editorInputBehaviorInPlayMode = entradaEditor;
        Time.timeScale=1;
        checks.Add(error == null ? "OK: "+checks.Count+" comprobaciones del afiche animado." : "FAIL: "+error);
        File.WriteAllLines("Logs/MenuAfiche/Pruebas.txt",checks);
        Application.logMessageReceived-=Log;
        EditorApplication.update-=Vigilar;
        EditorApplication.playModeStateChanged-=Iniciar;
        EditorApplication.isPlaying=false;
        EditorApplication.delayCall+=()=>EditorApplication.Exit(error == null ? 0 : 1);
    }
}
