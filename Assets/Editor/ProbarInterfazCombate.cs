using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static ProbarMenuPausa;

public static class ProbarInterfazCombate
{
    public static void Run() => Ejecutar("Logs/InterfazCombate", Revisar);

    private static IEnumerator Revisar()
    {
        yield return RevisarPadrePasta();
        yield return RevisarRenato();
        yield return RevisarSandbox();
        yield return Pruebas();
    }

    private static IEnumerator RevisarPadrePasta()
    {
        PermitirReintento("Level2_PadrePasta");
        SceneManager.LoadScene("Level2_PadrePasta");
        yield return null;
        yield return new WaitForSeconds(.35f);
        Health jugador = Jugador();
        Health jefe = Object.FindFirstObjectByType<PadrePasta>().GetComponent<Health>();
        Slider mateo = GameObject.Find("SliderJugador").GetComponent<Slider>();
        Slider pasta = GameObject.Find("Slider").GetComponent<Slider>();
        jugador.RestaurarVida(jugador.VidaMaxima);
        jugador.Invulnerable = true;
        Check(MuestraVida(mateo, jugador) && MuestraVida(pasta, jefe), "Ambas fichas comienzan con la vida real del personaje.");
        foreach (Vector2Int tamano in Formatos)
            yield return Capturar("HUD", tamano.x, tamano.y, () => ComprobarHud(mateo, pasta, tamano));

        // El golpe se aplica en el mismo fotograma que se renderiza, antes de que se disipe el rastro.
        yield return Capturar("Impacto", 1280, 720, () =>
        {
            jugador.Invulnerable = false;
            Check(jugador.RecibirDanio(1) && jefe.RecibirDanio(210), "Mateo y Padre Pasta reciben un golpe durante la partida.");
            Check(MuestraVida(mateo, jugador) && MuestraVida(pasta, jefe) && ImpactoCorrecto(mateo, true) && ImpactoCorrecto(pasta, true),
                "El golpe actualiza inmediatamente ambas cifras y deja un rastro y un destello en cada ficha.");
            jugador.Invulnerable = true;
            Canvas.ForceUpdateCanvases();
        });
        yield return new WaitForSeconds(1.1f);
        Check(ImpactoCorrecto(mateo, false) && ImpactoCorrecto(pasta, false), "Rastro y destello se extinguen en ambas fichas durante el combate.");
        jugador.Invulnerable = false;
        Check(jugador.RecibirDanio(1) && jugador.VidaActual == 1 && Texto(mateo, "Estado").text == "¡CRÍTICA!",
            "El último punto activa el aviso escrito de vida crítica.");
        jugador.Invulnerable = true;
        yield return new WaitForSeconds(.55f);
        yield return Capturar("Vida-critica", 1280, 720, () => ComprobarHud(mateo, pasta, new Vector2Int(1280, 720)));
        yield return Capturar("Vida-critica", 1080, 2340, () => ComprobarHud(mateo, pasta, new Vector2Int(1080, 2340)));
        Check(jugador.Curar(1) && Texto(mateo, "Estado").text == "JUGADOR" && ImpactoCorrecto(mateo, false),
            "Curar al jugador retira el aviso sin activar el efecto de daño.");
        yield return Tecla(Key.Escape);
        yield return Capturar("Pausa", 1280, 720);
        yield return Tecla(Key.Escape);

        // Perder una vida intermedia recarga el nivel y conserva la salud del jefe.
        SesionJuego.VidasExtra = 2;
        jugador.RestaurarVida(jugador.VidaActual);
        int vidaRestanteJefe = jefe.VidaActual;
        jugador.RecibirDanio(jugador.VidaMaxima);
        yield return new WaitUntil(() => jugador == null);
        yield return null;
        jugador = Jugador();
        jefe = Object.FindFirstObjectByType<PadrePasta>().GetComponent<Health>();
        pasta = GameObject.Find("Slider").GetComponent<Slider>();
        Check(SesionJuego.VidasExtra == 1 && Time.timeScale == 1 && jefe.VidaActual == vidaRestanteJefe && MuestraVida(pasta, jefe),
            "Con una vida de reserva se recarga el nivel y la ficha recupera la salud guardada del jefe.");
        jugador.RecibirDanio(jugador.VidaMaxima);
        yield return new WaitUntil(() => Time.timeScale == 0);
        RectTransform marco = MarcoDerrota();
        Button[] opciones = marco.GetComponentsInChildren<Button>();
        CanvasGroup cabecera = marco.GetComponentInChildren<CanvasGroup>();
        Check(cabecera.GetComponentInChildren<TMP_Text>().text == "DERROTA" &&
            opciones.Length == 2 && opciones[0].GetComponentInChildren<TMP_Text>().text == "Revancha" &&
            opciones[1].GetComponentInChildren<TMP_Text>().text == "Menú principal",
            "Derrota muestra Revancha y Menú principal en el orden esperado.");
        Check(EnFoco(opciones[0]), "Revancha recibe el foco y el rombo al abrir Game Over.");
        Check(opciones[0].IsInteractable() && cabecera.alpha < 1, "Revancha está disponible durante la entrada del afiche.");
        yield return new WaitForSecondsRealtime(.32f);
        Check(cabecera.alpha == 1f && ((RectTransform)cabecera.transform).anchoredPosition == Vector2.zero,
            "El afiche termina la transición con el combate detenido.");
        foreach (Vector2Int tamano in Formatos)
            yield return Capturar("GameOver", tamano.x, tamano.y, () => ComprobarMenu(marco, tamano.x, tamano.y));
        yield return Tecla(Key.Escape);
        Check(Time.timeScale == 0 && !SesionJuego.EnPausa && EnFoco(opciones[0]), "Escape durante la derrota mantiene el menú y su foco.");
        yield return EstadoMando(new GamepadState().WithButton(GamepadButton.DpadDown));
        yield return Clic(new Vector2(8, 8));
        Check(EnFoco(opciones[1]), "La cruceta alcanza Menú principal y un clic en el fondo mantiene la selección.");
        yield return EstadoMando(new GamepadState { leftStick = Vector2.up });
        Check(EnFoco(opciones[0]), "La palanca devuelve el foco a Revancha.");
        yield return EstadoMando(new GamepadState().WithButton(GamepadButton.South));
        Check(jugador == null && SceneManager.GetActiveScene().name == "Level2_PadrePasta" && Time.timeScale == 1 &&
            Jugador().VidaActual == Jugador().VidaMaxima && SesionJuego.VidasExtra == SesionJuego.VidasIniciales,
            "Revancha con el mando reinicia el combate, los puntos de vida y las vidas de reserva.");
    }

    private static IEnumerator RevisarRenato()
    {
        PermitirReintento("Level1_Renato");
        SesionJuego.GuardarProgresoOleadas("Level1_Renato", 3);
        SceneManager.LoadScene("Level1_Renato");
        yield return null;
        Check(GameObject.Find("Slider") == null && GameObject.Find("SliderJugador") != null,
            "La ficha de Mateo aparece antes que la de Renato mientras el director prepara al jefe.");
        yield return new WaitUntil(() => GameObject.Find("Slider") != null);
        Slider mateo = GameObject.Find("SliderJugador").GetComponent<Slider>();
        Slider rene = GameObject.Find("Slider").GetComponent<Slider>();
        Health jefe = Object.FindFirstObjectByType<SegundaFaseRenato>().GetComponent<Health>();
        Check(MuestraVida(rene, jefe) && Texto(rene, "RENATO PUENTE") != null && jefe.RecibirDanio(100) && ImpactoCorrecto(rene, true),
            "La entrada de Renato muestra su nombre, su vida y la reacción al golpe.");
        yield return new WaitForSeconds(.6f);
        yield return Capturar("Rene", 1920, 1080, () => ComprobarHud(mateo, rene, new Vector2Int(1920, 1080)));
        int primerMaximo = jefe.VidaMaxima;
        Image retrato = rene.GetComponentsInChildren<Image>().Single(i => i.name == "Retrato");
        Sprite faseInicial = retrato.sprite;
        Check(jefe.RecibirDanio(jefe.VidaActual) && MuestraVida(rene, jefe) && jefe.VidaActual == 0,
            "La barra de Renato llega a cero al agotar su primera fase.");
        yield return new WaitUntil(() => jefe.VidaMaxima != primerMaximo);
        Check(jefe.VidaActual == jefe.VidaMaxima && MuestraVida(rene, jefe) && ImpactoCorrecto(rene, false),
            $"La transformación rellena la barra con el nuevo máximo de {jefe.VidaMaxima} y sin destello de daño.");
        Check(retrato.sprite != null && retrato.sprite != faseInicial && PrimerPlanoCompleto(retrato),
            "El retrato dorado de Renato cabe completo en la ficha de su segunda fase.");
        SesionJuego.VidasExtra = 1;
        Health jugador = Jugador();
        jugador.RestaurarVida(jugador.VidaMaxima);
        jugador.RecibirDanio(jugador.VidaMaxima);
        yield return new WaitUntil(() => Time.timeScale == 0);
        RectTransform marco = MarcoDerrota();
        Button[] opciones = marco.GetComponentsInChildren<Button>();
        Check(opciones.Length == 2 && EnFoco(opciones[0]), "La derrota ante Renato también abre en Revancha.");
        yield return new WaitForSecondsRealtime(.32f);
        Image retratoDerrota = marco.GetComponentsInChildren<Image>().Single(i => i.name == "Retrato");
        Check(retratoDerrota.sprite == retrato.sprite && PrimerPlanoCompleto(retratoDerrota),
            "El afiche de derrota encuadra el rostro completo de la fase en que terminó el combate.");
        yield return Capturar("Rene-GameOver", 1280, 720);
        yield return Tecla(Key.DownArrow);
        yield return Tecla(Key.Enter);
        Check(SceneManager.GetActiveScene().name == "MainMenu", "Menú principal en Game Over abre el menú principal.");
    }

    private static IEnumerator RevisarSandbox()
    {
        SceneManager.LoadScene("SandBox");
        yield return null;
        yield return null;
        Health jugador = Jugador();
        SesionJuego.VidasExtra = 1;
        jugador.RecibirDanio(jugador.VidaMaxima);
        yield return new WaitUntil(() => Time.timeScale == 0);
        Button[] opciones = MarcoDerrota().GetComponentsInChildren<Button>();
        Check(opciones.Length == 1 && EnFoco(opciones[0]), "El escenario de pruebas conserva su única opción Revancha.");
        yield return Tecla(Key.DownArrow);
        yield return Tecla(Key.Enter);
        Check(jugador == null && SceneManager.GetActiveScene().name == "SandBox" && Time.timeScale == 1,
            "Revancha desde el escenario de pruebas también vuelve al combate.");
    }

    private static readonly Vector2Int[] Formatos =
    {
        new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(2560, 1080), new Vector2Int(1080, 2340)
    };

    private static Health Jugador() => GameObject.FindGameObjectWithTag("Player").GetComponent<Health>();

    private static RectTransform MarcoDerrota() => (RectTransform)Object.FindFirstObjectByType<HudCombate>()
        .transform.Find("Panel/Área segura de derrota/Contenido/Marco de derrota");

    private static bool EnFoco(Button boton) => EventSystem.current.currentSelectedGameObject == boton.gameObject &&
        boton.transform.Find("Indicador de selección").GetComponent<Image>().enabled;

    private static TMP_Text Texto(Slider barra, string nombre) =>
        barra.GetComponentsInChildren<TMP_Text>().Single(texto => texto.name == nombre);

    private static bool MuestraVida(Slider barra, Health vida) => Mathf.Approximately(barra.normalizedValue,
        (float)vida.VidaActual / vida.VidaMaxima) && Texto(barra, "Vida numérica").text == $"{vida.VidaActual}/{vida.VidaMaxima}";

    private static bool ImpactoCorrecto(Slider barra, bool visible)
    {
        var rastro = (RectTransform)barra.fillRect.parent.Find("Daño reciente");
        Image luz = barra.GetComponentsInChildren<Image>().Single(i => i.name == "Impacto");
        bool hayRastro = rastro.anchorMax.x > barra.normalizedValue + .001f;
        return hayRastro == visible && (luz.color.a > 0) == visible;
    }

    private static bool PrimerPlanoCompleto(Image imagen)
    {
        Canvas.ForceUpdateCanvases();
        var ventana = (RectTransform)imagen.transform.parent;
        Bounds limites = RectTransformUtility.CalculateRelativeRectTransformBounds(ventana, imagen.transform);
        return limites.size.x > 0 && limites.size.y > 0 && limites.min.x >= ventana.rect.xMin - .1f &&
            limites.max.x <= ventana.rect.xMax + .1f && limites.min.y >= ventana.rect.yMin - .1f &&
            limites.max.y <= ventana.rect.yMax + .1f;
    }

    private static void ComprobarHud(Slider jugador, Slider jefe, Vector2Int tamano)
    {
        Camera camara = Camera.main;
        Rect izquierda = RectEnPantalla((RectTransform)jugador.transform, camara);
        Rect derecha = RectEnPantalla((RectTransform)jefe.transform, camara);
        Check(izquierda.xMin > 0 && izquierda.xMax < derecha.xMin && derecha.xMax < tamano.x &&
            izquierda.yMax < tamano.y && derecha.yMax < tamano.y && Mathf.Min(izquierda.yMin, derecha.yMin) > tamano.y * .80f &&
            derecha.width > izquierda.width, $"{tamano.x}×{tamano.y} mantiene las fichas separadas en la franja superior y da más ancho al jefe.");
        foreach (Slider barra in new[] { jugador, jefe })
        {
            TMP_Text[] textos = barra.GetComponentsInChildren<TMP_Text>();
            foreach (TMP_Text texto in textos)
            {
                texto.ForceMeshUpdate();
                var rect = RectEnPantalla(texto.rectTransform, camara);
                float pixeles = texto.fontSize * rect.height / texto.rectTransform.rect.height;
                float minimo = tamano.y == 720 ? 14 : 18;
                Check(!texto.isTextOverflowing && pixeles >= minimo,
                    $"{tamano.x}×{tamano.y} muestra «{texto.text}» entero a {pixeles:F1} px.");
            }
            TMP_Text cantidad = Texto(barra, "Vida numérica");
            TMP_Text nombre = textos.Single(t => t.text == "MATEO" || t.text == "PADRE PASTA" || t.text == "RENATO PUENTE");
            ComprobarSeparacion(nombre, cantidad, camara, tamano);
            if (barra == jugador)
                ComprobarSeparacion(Texto(barra, "Estado"), textos.Single(t => t.text.StartsWith("Vidas extra")), camara, tamano);
        }
    }

    private static void ComprobarSeparacion(TMP_Text izquierda, TMP_Text derecha, Camera camara, Vector2Int tamano)
    {
        Vector3 fin = izquierda.transform.TransformPoint(izquierda.textBounds.max);
        Vector3 inicio = derecha.transform.TransformPoint(derecha.textBounds.min);
        float hueco = RectTransformUtility.WorldToScreenPoint(camara, inicio).x - RectTransformUtility.WorldToScreenPoint(camara, fin).x;
        Check(hueco >= 4, $"{tamano.x}×{tamano.y} separa «{izquierda.text}» y «{derecha.text}» por {hueco:F1} px.");
    }
}
