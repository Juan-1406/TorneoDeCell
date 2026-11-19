using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine.InputSystem.LowLevel;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using static ProbarMenuPausa;

public static class ProbarNivelRenato
{
    public static void Run() => Ejecutar("Logs/NivelRenato", Partida);

    private static IEnumerator Partida()
    {
        yield return Persecucion();
        yield return FuegoDesdeUnFlanco();
        yield return Esquivar();
        yield return Fases();
    }

    private static IEnumerator Cargar()
    {
        SesionJuego.Reiniciar();
        PermitirReintento("Level1_Renato");
        SceneManager.LoadScene("Level1_Renato");
        yield return null;
        yield return null;
    }

    private static Health Mateo() => GameObject.FindGameObjectWithTag("Player").GetComponent<Health>();

    private static IEnumerator Persecucion()
    {
        yield return Cargar();
        var director = Object.FindFirstObjectByType<DirectorOleadas>();
        yield return new WaitUntil(() => director.EnemigosActivos == 1);
        var enemigo = Object.FindFirstObjectByType<EnemigoMenor>();
        Health jugador = Mateo();
        float inicio = Time.time;
        float distancia = Vector2.Distance(enemigo.transform.position, jugador.transform.position);
        yield return new WaitUntil(() => enemigo.EstadoActual == EnemigoMenor.EstadoEnemigo.Amago);
        float amago = Vector2.Distance(enemigo.transform.position, jugador.transform.position);
        int vida = jugador.VidaActual;
        enemigo.Vida.RecibirDanio(1);
        yield return null;
        Check(enemigo.GetComponent<PresentacionEnemigoMenor>().AnimacionActual == "PrepararGolpe",
            "Un impacto durante el amago deja visible la preparación del puño.");
        yield return new WaitUntil(() => enemigo.EstadoActual == EnemigoMenor.EstadoEnemigo.Recuperacion);
        Check(jugador.VidaActual == vida - 1,
            $"Primera carga desde aparición a {distancia:F1} unidades, amago a {amago:F1}, tiempo {Time.time-inicio:F2} s, salud de Mateo {vida} a {jugador.VidaActual}.");
        jugador.Invulnerable = true;
        int pendientes = director.EnemigosActivos + director.EnemigosPorAparecer;
        enemigo.Vida.RecibirDanio(999);
        Check(director.EnemigosActivos + director.EnemigosPorAparecer == pendientes - 1 &&
            Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Any(t => t.text.Contains($"·  {pendientes - 1} ENEMIGO")),
            "La baja actualiza el HUD y cuenta tanto enemigos activos como pendientes.");
        foreach (Vector2Int formato in new[] {new Vector2Int(1280,720),new Vector2Int(2560,1080),new Vector2Int(1080,2340)})
            yield return Capturar("Oleada", formato.x, formato.y, () => RevisarPanel("Panel de oleada"));
    }

    private static IEnumerator FuegoDesdeUnFlanco()
    {
        yield return Cargar();
        Health jugador = Mateo();
        var tirador = jugador.GetComponent<PlayerShooting>();
        var movimiento = jugador.GetComponent<PlayerMovement>();
        var director = Object.FindFirstObjectByType<DirectorOleadas>();
        yield return new WaitUntil(() => director.EnemigosActivos == 1);
        var primero = Object.FindFirstObjectByType<EnemigoMenor>();
        movimiento.enabled = false;
        typeof(PlayerMovement).GetField("direccionMirada", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(movimiento, -1f);
        typeof(PlayerMovement).GetProperty("DireccionDisparo").SetValue(movimiento, Vector2.left);
        float limite = Time.time + 8f;
        while (Time.time < limite && jugador.VidaActual == jugador.VidaMaxima)
        {
            tirador.SendMessage("Disparar");
            yield return new WaitForSeconds(tirador.CadenciaActual);
        }
        Check(primero == null && jugador.VidaActual < jugador.VidaMaxima,
            "El fuego sostenido derriba al atacante de la izquierda, pero Mateo quieto recibe la carga del otro flanco.");
    }

    private static IEnumerator Esquivar()
    {
        yield return Cargar();
        yield return new WaitUntil(() => Object.FindObjectsByType<EnemigoMenor>(FindObjectsSortMode.None).Any(e => e.PreparandoAtaque));
        var enemigo = Object.FindObjectsByType<EnemigoMenor>(FindObjectsSortMode.None).First(e => e.PreparandoAtaque);
        var jugador = Mateo();
        // Mateo usa el mismo salto físico que inicia la barra espaciadora.
        yield return new WaitForSeconds(.16f);
        jugador.GetComponent<PlayerMovement>().SendMessage("Saltar");
        float altura = jugador.transform.position.y;
        yield return new WaitForSeconds(.22f);
        Check(jugador.transform.position.y > altura + .5f, "Mateo salta durante el aviso.");
        yield return new WaitUntil(() => enemigo.EstadoActual == EnemigoMenor.EstadoEnemigo.Recuperacion);
        Check(jugador.VidaActual == jugador.VidaMaxima, "El salto esquiva la primera carga.");
    }

    private static IEnumerator Fases()
    {
        var director = Object.FindFirstObjectByType<DirectorOleadas>();
        Mateo().Invulnerable = true;
        while (!director.JefeActivo)
        {
            foreach (var e in Object.FindObjectsByType<EnemigoMenor>(FindObjectsSortMode.None))
                if (!e.Entrando) e.Vida.RecibirDanio(999);
            yield return null;
        }
        var fase = Object.FindFirstObjectByType<SegundaFaseRenato>();
        var vida = fase.GetComponent<Health>();
        vida.RecibirDanio(vida.VidaMaxima);
        var aviso = GameObject.Find("Diálogo de Renato");
        var titulo = aviso.GetComponentsInChildren<TMP_Text>().Single(t => t.name == "Título de fase");
        Check(titulo.text == "FASE 2" && !vida.RecibirDanio(1) &&
            aviso.GetComponentsInChildren<TMP_Text>().Any(t => t.text == "¡La pelea sigue!"), "El primer agotamiento abre una sola transición y rechaza impactos repetidos.");
        float inicioAviso = Time.time;
        yield return Capturar("Fase-2",1280,720, () => RevisarPanel("Marco del aviso"));
        yield return Capturar("Fase-2",1080,2340, () => RevisarPanel("Marco del aviso"));
        yield return new WaitUntil(() => !aviso.activeSelf);
        Check(Time.time - inicioAviso >= 3.35f && vida.VidaActual == 0,
            "El aviso de fase 2 se lee durante 3,4 s y se retira antes de reanudar el combate.");
        yield return new WaitUntil(() => vida.VidaActual == 375);
        Check(!aviso.activeSelf && GameObject.Find("Panel de oleada").GetComponentInChildren<TMP_Text>().text.Contains("Devuelve"),
            "El aviso se retira antes del combate dorado y el HUD actualiza el objetivo.");
        var proyectil = UnityEditor.AssetDatabase.LoadAssetAtPath<Bullet>(
            "Assets/_Project/Prefabs/Proyectiles/Renato Puente/BalaEnemigaAbanico.prefab");
        var disparar = typeof(Bullet).GetMethod("DispararEnergia", BindingFlags.Instance | BindingFlags.NonPublic);
        var cuerpo = fase.GetComponent<BoxCollider2D>().bounds;
        Vector2 lado = cuerpo.center.x > 0 ? Vector2.left : Vector2.right;
        Vector2 origen = (Vector2)cuerpo.center + lado * (cuerpo.extents.x + .6f);
        for (int i = 0; i < 5; i++)
        {
            var energia = Object.Instantiate(proyectil, origen, Quaternion.identity);
            disparar.Invoke(energia, new object[] { fase, lado, 7f, 75, 12, 2.2f });
            energia.Reflejar();
        }
        yield return new WaitUntil(() => vida.VidaActual == 0);
        var devolver = typeof(SegundaFaseRenato).GetMethod("RecibirEnergiaDevuelta", BindingFlags.Instance | BindingFlags.NonPublic);
        devolver.Invoke(fase, new object[] { 75 });
        Check(vida.VidaActual == 0 && aviso.activeSelf && titulo.text == "¡VICTORIA DEFINITIVA!" &&
            Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Count(t => t.name == "Título de fase") == 1,
            "Al agotar la fase dorada aparece una sola victoria en el mismo panel.");
        yield return Capturar("Victoria",1080,2340, () => RevisarPanel("Marco del aviso"));
        yield return Capturar("Victoria",1920,1080);
        yield return new WaitForSecondsRealtime(1.25f);
        yield return Tecla(UnityEngine.InputSystem.Key.Enter);
        Check(aviso.GetComponentsInChildren<TMP_Text>().Any(t => t.text.Contains("2/3")), "Confirmar avanza una página y mantiene la victoria.");
        yield return Tecla(UnityEngine.InputSystem.Key.Enter);
        const string clave = "jefe_derrotado_0";
        bool existia = PlayerPrefs.HasKey(clave);
        int valor = PlayerPrefs.GetInt(clave);
        int victorias = 0;
        vida.OnMuerte += () => victorias++;
        try
        {
            yield return EstadoMando(new UnityEngine.InputSystem.LowLevel.GamepadState().WithButton(
                UnityEngine.InputSystem.LowLevel.GamepadButton.South));
        }
        finally
        {
            if (existia) PlayerPrefs.SetInt(clave, valor);
            else PlayerPrefs.DeleteKey(clave);
            PlayerPrefs.Save();
        }
        GameObject resultados = GameObject.Find("Resultados del combate");
        Check(victorias == 1 && !aviso.activeSelf && resultados != null,
            "La confirmación final con mando conduce una sola vez a resultados.");
        var textos = resultados.GetComponentsInChildren<TMP_Text>();
        Check(textos.Any(t => t.text == "Renato Puente") && textos.Any(t => t.text == "Elegir nivel"),
            "Resultados muestra el nombre corregido y la misma acción que la pausa.");
        foreach (var formato in new[] { new Vector2Int(1280, 720), new Vector2Int(2560, 1080), new Vector2Int(1080, 2340) })
            yield return Capturar("Resultados", formato.x, formato.y, () =>
                Check(textos.All(t =>
                {
                    t.ForceMeshUpdate();
                    return !t.isTextOverflowing;
                }), $"Los textos de resultados caben a {formato.x}×{formato.y}."));
        yield return EstadoMando(new GamepadState().WithButton(GamepadButton.South));
        Check(SceneManager.GetActiveScene().name == "LevelSelect", "Elegir nivel abre el selector desde resultados.");

    }

    private static void RevisarPanel(string nombre)
    {
        Canvas.ForceUpdateCanvases();
        var panel = (RectTransform)GameObject.Find(nombre).transform;
        var rect = RectEnPantalla(panel, Camera.main);
        foreach (var barra in Object.FindObjectsByType<HealthBarUI>(FindObjectsSortMode.None))
            Check(!rect.Overlaps(RectEnPantalla((RectTransform)barra.transform, Camera.main)), $"{nombre} separado de {barra.name}.");
        foreach (var texto in panel.GetComponentsInChildren<TMP_Text>())
        {
            texto.ForceMeshUpdate();
            Check(!texto.isTextOverflowing, $"{texto.name} cabe en su panel.");
        }
    }
}
