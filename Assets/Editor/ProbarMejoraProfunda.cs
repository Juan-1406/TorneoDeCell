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

public static class ProbarMejoraProfunda
{
    static string Output => Path.GetFullPath("Logs/PruebasOleadas.txt");
    static readonly List<string> checks = new List<string>();
    static double inicio;
    static bool terminado;
    static DirectorOleadas d;
    static Health jugador;
    static RecompensasCombate premios;
    static PlayerShooting disparo;
    static string etapa;
    static BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    public static void Run()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Ejecutar estas pruebas con Unity en batchmode.");
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Level1_Renato.unity");
        SesionJuego.Reiniciar();
        inicio = EditorApplication.timeSinceStartup;
        EditorApplication.playModeStateChanged += Iniciar;
        EditorApplication.update += Vigilar;
        Application.logMessageReceived += Log;
        EditorApplication.isPlaying = true;
    }

    static void Iniciar(PlayModeStateChange cambio)
    {
        if (cambio != PlayModeStateChange.EnteredPlayMode) return;
        var go = new GameObject("Pruebas de integración");
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<Health>().StartCoroutine(Pruebas());
    }

    static void Vincular()
    {
        d = UnityEngine.Object.FindFirstObjectByType<DirectorOleadas>();
        premios = UnityEngine.Object.FindFirstObjectByType<RecompensasCombate>();
        jugador = GameObject.Find("Player").GetComponent<Health>();
        disparo = jugador.GetComponent<PlayerShooting>();
        jugador.GetComponent<PlayerMovement>().enabled = false;
        jugador.Invulnerable = true;
        Time.timeScale = 4;
    }

    static IEnumerator Pruebas()
    {
        yield return null;
        Vincular();
        etapa = "inicio";
        Check(d != null && premios != null, "Directores y referencias cargan");
        Check(!d.JefeActivo && GameObject.FindGameObjectWithTag("Boss").GetComponent<Health>().Invulnerable, "Jefe protegido durante oleadas");
        yield return new WaitUntil(() => GameObject.Find("Aviso de entrada") != null);
        Check(d.EnemigosActivos == 0 && d.EnemigosPorAparecer == 3, "El aviso precede a la aparición");
        Check(GameObject.Find("Aviso de entrada").GetComponent<AvisoEntradaEnemigo>() != null, "La alerta usa una mira animada");
        Capturar("01-alerta");
        Vector3 posicionAviso = GameObject.Find("Aviso de entrada").transform.position;
        jugador.GetComponent<Rigidbody2D>().position = posicionAviso;
        yield return new WaitUntil(() => d.EnemigosActivos == 1);
        var enemigo = Enemigos()[0];
        Check(enemigo.Entrando && !enemigo.GetComponent<Collider2D>().enabled, "La entrada animada no tiene contacto ofensivo");
        Check(Mathf.Abs(enemigo.transform.position.x - jugador.transform.position.x) >= 4f, "Aparición alejada del jugador");
        Check(Mathf.Abs(enemigo.transform.position.x - posicionAviso.x) >= 4f, "Entrar en el aviso obliga a buscar otra posición segura");
        yield return new WaitUntil(() => !enemigo.Entrando);
        Check(enemigo.GetComponent<Collider2D>().enabled && !enemigo.Vida.Invulnerable, "La entrada termina con colisión y salud normales");
        jugador.GetComponent<Rigidbody2D>().position = new Vector2(0, -2.4f);
        enemigo.GetComponent<Rigidbody2D>().position = new Vector2(-7f, -2.7f);
        var pared = new GameObject("Obstáculo de prueba");
        pared.layer = LayerMask.NameToLayer("Ground");
        pared.transform.position = new Vector3(-6f, -2.7f, 0f);
        pared.AddComponent<BoxCollider2D>().size = new Vector2(0.4f, 2f);
        Physics2D.SyncTransforms();
        yield return new WaitForSeconds(0.8f);
        Check(enemigo.transform.position.x < -6.6f, "El enemigo no atraviesa un obstáculo");
        UnityEngine.Object.Destroy(pared);
        enemigo.GetComponent<Rigidbody2D>().position = new Vector2(-1f, -2.7f);
        Set(enemigo, "siguienteAtaque", Time.time);
        jugador.RestaurarVida(3);
        yield return new WaitUntil(() => enemigo.PreparandoAtaque);
        Check(enemigo.EstadoActual == EnemigoMenor.EstadoEnemigo.Amago, "El melee anticipa su carga con un amago");
        jugador.GetComponent<Rigidbody2D>().position = new Vector2(-2f, -2.4f);
        yield return new WaitForSeconds(0.55f);
        Check(jugador.VidaActual == 3, "Cambiar de lado esquiva el golpe anunciado");
        jugador.Invulnerable = true;
        jugador.GetComponent<Rigidbody2D>().position = new Vector2(0, -2.4f);
        enemigo.enabled = false;
        yield return new WaitForSeconds(2);
        Check(d.EnemigosActivos == 2 && d.EnemigosPorAparecer == 1, "La primera oleada respeta el cupo y conserva pendientes");
        foreach (var e in Enemigos())
        {
            while (e.Entrando) yield return null;
            e.enabled = false;
        }

        etapa = "recompensas";
        var curacion = Prefab<RecogibleCombate>("Recompensas/RecompensaCuracion.prefab");
        var cadencia = Prefab<RecogibleCombate>("Recompensas/RecompensaCadencia.prefab");
        var item = premios.IntentarCrear(curacion, 1f, jugador.transform.position);
        Check(item != null && !item.IntentarRecoger(), "El objeto tiene tiempo de aparición antes de recogerse");
        Check(item.EnVuelo, "El loot sale impulsado al aparecer");
        float alturaInicial = item.transform.position.y;
        yield return new WaitForSeconds(0.18f);
        Check(item.transform.position.y > alturaInicial, "El impulso del loot produce un arco ascendente");
        Capturar("02-loot");
        yield return new WaitForSeconds(0.5f);
        Check(item != null && !item.IntentarRecoger(), "La curación no se desperdicia con salud completa");
        jugador.Invulnerable = false;
        jugador.RecibirDanio(1);
        jugador.Invulnerable = true;
        Time.timeScale = 0; SesionJuego.EnPausa = true;
        Check(!item.IntentarRecoger(), "No se recogen ítems durante la pausa");
        float vidaItem = item.SegundosRestantes;
        yield return new WaitForSecondsRealtime(0.2f);
        Check(Mathf.Approximately(item.SegundosRestantes, vidaItem), "La pausa congela la caducidad");
        SesionJuego.EnPausa = false; Time.timeScale = 4;
        Check(item.IntentarRecoger() && jugador.VidaActual == 3 && jugador.Invulnerable, "Curación respeta máximo e invulnerabilidad");
        Check(!item.IntentarRecoger(), "Una recompensa no se aplica dos veces");
        Check(SesionJuego.VidasExtra == 3, "Curar no regala vidas extra");
        yield return null;

        var a = premios.IntentarCrear(curacion, 1f, new Vector3(8, -2, 0));
        var b = premios.IntentarCrear(curacion, 1f, new Vector3(9, -2, 0));
        Check(a != null && b != null && premios.IntentarCrear(curacion, 1f, Vector3.zero) == null, "Máximo de dos curaciones en la arena");
        var buff = premios.IntentarCrear(cadencia, 1f, new Vector3(-8, -2, 0));
        Check(buff != null && premios.ObjetosActivos == 3 && premios.IntentarCrear(cadencia, 1f, Vector3.zero) == null, "Límite global y de potenciadores");
        UnityEngine.Object.Destroy(a.gameObject); UnityEngine.Object.Destroy(b.gameObject);
        buff.transform.position = jugador.transform.position;
        yield return new WaitForSeconds(0.5f);
        Check(disparo.SegundosMejora > 0 && disparo.CadenciaActual < 0.2f, "Recoger cadencia modifica el disparo real");
        float restante = disparo.SegundosMejora;
        disparo.MejorarCadenciaTemporal(0.8f, 1f);
        Check(disparo.SegundosMejora >= restante && disparo.CadenciaActual <= 0.131f, "Otra mejora no acorta ni multiplica la existente");
        disparo.SuspenderMejora = true;
        restante = disparo.SegundosMejora;
        yield return new WaitForSeconds(0.5f);
        Check(Mathf.Approximately(restante, disparo.SegundosMejora), "El descanso conserva la duración del potenciador");
        disparo.SuspenderMejora = false;
        disparo.LimpiarMejora();
        disparo.MejorarCadenciaTemporal(0.65f, 0.2f);
        yield return new WaitForSeconds(0.4f);
        Check(disparo.SegundosMejora == 0 && Mathf.Approximately(disparo.CadenciaActual, 0.2f), "La cadencia base se restaura al expirar");

        item = premios.IntentarCrear(curacion, 1f, new Vector3(9, -2, 0));
        Set(item, "restante", 0.2f);
        premios.EnDescanso = true;
        yield return new WaitForSeconds(0.4f);
        Check(item != null && item.SegundosRestantes > 0, "Los descansos también conservan ítems pendientes");
        premios.EnDescanso = false;
        yield return new WaitForSeconds(0.4f);
        Check(item == null, "Los ítems caducan durante el combate");

        etapa = "parry";
        var parry = jugador.GetComponent<PlayerParry>();
        var balaPrefab = Prefab<Bullet>("Proyectiles/Renato Puente/BalaEnemigoMenor.prefab");
        Set(premios, "probabilidadParry", 1f);
        parry.StartCoroutine("Parry");
        var bala = UnityEngine.Object.Instantiate(balaPrefab, new Vector3(0, 10, 0), Quaternion.identity);
        bala.Disparar(Vector2.left);
        Check(parry.IntentarParry(bala) && !bala.EsReflejable, "Parry devuelve el proyectil y lo marca una sola vez");
        Check(premios.ObjetosActivos == 1, "Un parry perfecto puede generar una recompensa");
        Check(!parry.IntentarParry(bala) && premios.ObjetosActivos == 1, "No se recompensa dos veces el mismo proyectil");
        UnityEngine.Object.Destroy(bala.gameObject);
        foreach (var r in UnityEngine.Object.FindObjectsByType<RecogibleCombate>(FindObjectsSortMode.None)) UnityEngine.Object.Destroy(r.gameObject);
        yield return new WaitForSeconds(0.7f);
        parry.StartCoroutine("Parry");
        bala = UnityEngine.Object.Instantiate(balaPrefab, new Vector3(0, 10, 0), Quaternion.identity);
        bala.Disparar(Vector2.left); parry.IntentarParry(bala);
        Check(premios.ObjetosActivos == 0, "La espera entre premios limita la acumulación por parry");
        UnityEngine.Object.Destroy(bala.gameObject);
        Set(premios, "probabilidadParry", 0.35f);

        etapa = "final primera oleada";
        yield return LimpiarHasta(1);
        Check(SesionJuego.ProgresoOleadas("Level1_Renato") == 1, "Completar la oleada guarda el avance");
        Check(d.Estado == DirectorOleadas.EstadoOleadas.Descanso, "Hay descanso entre rondas");
        yield return new WaitUntil(() => d.IndiceOleada == 1 && d.EnemigosActivos == 2);
        Check(Enemigos().Any(e => e.name.Contains("Distancia")), "La segunda oleada introduce al tirador");
        Check(d.EnemigosActivos == 2 && d.EnemigosPorAparecer == 4, "La segunda oleada abre con un grupo mixto simultáneo");
        var tirador = Enemigos().First(e => e.name.Contains("Distancia"));
        yield return new WaitUntil(() => tirador.PreparandoAtaque);
        Vector2 direccionAnunciada = jugador.transform.position - tirador.transform.position;
        var cuerpoJugador = jugador.GetComponent<Rigidbody2D>();
        cuerpoJugador.gravityScale = 0f;
        cuerpoJugador.position += Vector2.up * 3f;
        yield return new WaitUntil(() => UnityEngine.Object.FindObjectsByType<Bullet>(FindObjectsSortMode.None).Any(x => x.EsReflejable));
        Check(true, "El tirador produce proyectiles durante la partida");
        var balas = UnityEngine.Object.FindObjectsByType<Bullet>(FindObjectsSortMode.None).Where(x => x.EsReflejable).ToArray();
        Check(balas.Length == 3, "La ráfaga abre con tres proyectiles separados");
        Check(balas.Any(x => Vector2.Dot(x.GetComponent<Rigidbody2D>().linearVelocity.normalized, direccionAnunciada.normalized) > 0.99f), "El tirador respeta la dirección preparada aunque el jugador se mueva");
        yield return new WaitForSeconds(0.3f);
        Check(UnityEngine.Object.FindObjectsByType<Bullet>(FindObjectsSortMode.None).Count(x => x.EsReflejable) >= 6, "La ráfaga tiene una segunda descarga escalonada");
        yield return new WaitUntil(() => tirador.EstadoActual == EnemigoMenor.EstadoEnemigo.Reposicion);
        Check(true, "El tirador se reposiciona después de disparar");
        Capturar("03-conflicto");
        cuerpoJugador.gravityScale = 3f;
        cuerpoJugador.position = new Vector2(0, -2.4f);
        yield return new WaitUntil(() => tirador.EstadoActual == EnemigoMenor.EstadoEnemigo.Buscando);
        Set(tirador, "siguienteAtaque", Time.time + 5f);
        Set(tirador, "revisarAmenaza", Time.time);
        Set(tirador, "siguienteEvasion", Time.time);
        var balaJugador = UnityEngine.Object.Instantiate(Prefab<Bullet>("Proyectiles/Bala.prefab"),
            tirador.transform.position + Vector3.left * 3f, Quaternion.identity);
        balaJugador.Disparar(Vector2.right);
        // Aislar la percepción: otro enemigo no debe interceptar el disparo de esta prueba.
        balaJugador.GetComponent<Rigidbody2D>().simulated = false;
        Check(Bullet.Activas.Contains(balaJugador), "La percepción registra el proyectil nuevo");
        Check(balaJugador.AmenazaA(tirador.transform.position), "El disparo entrante se detecta como amenaza");
        yield return new WaitForSeconds(0.2f);
        Check(tirador.EstadoActual == EnemigoMenor.EstadoEnemigo.Reposicion,
            "El azul reacciona al disparo antes del impacto: " + tirador.EstadoActual);
        yield return new WaitForSeconds(0.55f);
        Check(tirador.EstadoActual == EnemigoMenor.EstadoEnemigo.Buscando,
            "La recarga de evasión impide que el azul huya continuamente");
        UnityEngine.Object.Destroy(balaJugador.gameObject);

        etapa = "reintento oleada";
        disparo.MejorarCadenciaTemporal(0.65f, 8f);
        jugador.RestaurarVida(3); jugador.RecibirDanio(999);
        Check(premios.Finalizado && premios.ObjetosActivos == 0 && disparo.SegundosMejora == 0, "La muerte cancela premios y efectos");
        yield return new WaitForSeconds(2);
        Vincular();
        Check(SesionJuego.VidasExtra == 2 && d.IndiceOleada == 1, "Reintento conserva solo las oleadas completadas");
        Check(disparo.SegundosMejora == 0, "Las mejoras no pasan a la vida siguiente");
        yield return LimpiarHasta(2);
        Check(!d.JefeActivo, "El jefe sigue bloqueado tras la segunda oleada");
        yield return new WaitUntil(() => d.IndiceOleada == 2 && d.EnemigosActivos == 4);
        foreach(var e in Enemigos()) while(e.Entrando) yield return null;
        Check(d.CupoActual == 4 && d.EnemigosPorAparecer == 4, "El clímax mantiene cuatro activos y una reserva continua");
        Capturar("04-horda");
        Enemigos()[0].Vida.RecibirDanio(999);
        Check(UnityEngine.Object.FindFirstObjectByType<FeedbackCombate>() != null, "Las bajas generan feedback compartido");
        yield return new WaitUntil(() => d.EnemigosActivos == 4);
        Check(SesionJuego.ProgresoOleadas("Level1_Renato") == 2, "La horda repone una baja sin pasar al jefe");
        yield return LimpiarHasta(3);
        yield return new WaitUntil(() => d.Estado == DirectorOleadas.EstadoOleadas.EntradaJefe);
        Check(!d.JefeActivo && d.EnemigosActivos == 0 && d.EnemigosPorAparecer == 0, "Entrada del jefe con todos los enemigos eliminados");
        yield return new WaitUntil(() => d.JefeActivo);
        var boss = GameObject.FindGameObjectWithTag("Boss").GetComponent<Health>();
        Check(!boss.Invulnerable && boss.GetComponent<BossController>().enabled, "Renato queda activo y vulnerable");
        boss.RecibirDanio(10);
        int saludJefe = boss.VidaActual;
        etapa = "reintento jefe";
        jugador.RestaurarVida(3); jugador.RecibirDanio(999);
        yield return new WaitForSeconds(2);
        Vincular();
        Check(!d.JefeActivo && GameObject.FindGameObjectWithTag("Boss").GetComponent<Health>().Invulnerable, "La entrada del jefe protege su salud restaurada");
        SesionJuego.VidasExtra = 2;
        jugador.RestaurarVida(3); jugador.RecibirDanio(999);
        yield return new WaitForSeconds(2);
        Vincular();
        Check(GameObject.FindGameObjectWithTag("Boss").GetComponent<Health>().VidaActual == saludJefe, "Morir durante la entrada tampoco pierde la salud guardada del jefe");
        yield return new WaitUntil(() => d.JefeActivo);
        Check(d.IndiceOleada == 3 && Enemigos().Length == 0, "Reintento del jefe no repite oleadas");
        Check(GameObject.FindGameObjectWithTag("Boss").GetComponent<Health>().VidaActual == saludJefe, "Reintento conserva la salud del jefe");
        etapa = "game over";
        SesionJuego.VidasExtra = 1;
        jugador.RestaurarVida(3); jugador.RecibirDanio(999);
        yield return new WaitUntil(() => Time.timeScale == 0f);
        Check(Time.timeScale == 0 && premios.Finalizado, "Game Over detiene combate y recompensas");
        UnityEngine.Object.FindFirstObjectByType<LevelManager>().ReintentarJuego();
        yield return null; yield return null;
        Vincular();
        Check(SesionJuego.VidasExtra == 3 && SesionJuego.ProgresoOleadas("Level1_Renato") == 0, "Reiniciar desde Game Over borra el avance de la partida");
        Terminar(null);
    }

    static IEnumerator LimpiarHasta(int progreso)
    {
        while (SesionJuego.ProgresoOleadas("Level1_Renato") < progreso)
        {
            foreach (var e in Enemigos()) e.Vida.RecibirDanio(999);
            yield return new WaitForSeconds(0.15f);
        }
    }
    static EnemigoMenor[] Enemigos() => UnityEngine.Object.FindObjectsByType<EnemigoMenor>(FindObjectsSortMode.None);
    static T Prefab<T>(string path) where T : Component => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/" + path).GetComponent<T>();
    static void Set(object o, string name, object value) => o.GetType().GetField(name, Flags).SetValue(o, value);
    static void Check(bool ok, string mensaje)
    {
        if (!ok) throw new Exception(mensaje);
        checks.Add("PASS: " + mensaje);
    }
    static void Vigilar()
    {
        if (terminado) return;
        if (EditorApplication.timeSinceStartup - inicio > 180) Terminar("Tiempo agotado: " + etapa);
        if (d != null && d.EnemigosActivos + d.EntradasEnCurso > d.CupoActual) Terminar("Cupo excedido incluyendo entradas");
    }
    static void Log(string mensaje, string trace, LogType tipo)
    {
        if (!terminado && (tipo == LogType.Error || tipo == LogType.Exception)) Terminar(etapa + ": " + mensaje);
    }
    static void Capturar(string nombre)
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
        Directory.CreateDirectory("Logs/Juice");
        var cam = Camera.main;
        float aspectoAnterior = cam.aspect;
        cam.aspect = 1280f / 720f;
        foreach (var aviso in UnityEngine.Object.FindObjectsByType<AvisoEntradaEnemigo>(FindObjectsSortMode.None))
            aviso.SendMessage("Colocar");
        var rt = RenderTexture.GetTemporary(1280, 720, 24);
        var anterior = RenderTexture.active;
        var lienzos = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
            .Where(x => x.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
        try
        {
            foreach (var lienzo in lienzos) { lienzo.renderMode = RenderMode.ScreenSpaceCamera; lienzo.worldCamera = cam; lienzo.planeDistance = 1f; }
            Canvas.ForceUpdateCanvases();
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(cam,
                new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest { destination = rt });
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply();
            File.WriteAllBytes(Path.GetFullPath("Logs/Juice/" + nombre + ".png"), tex.EncodeToPNG());
            UnityEngine.Object.Destroy(tex);
        }
        finally
        {
            foreach (var lienzo in lienzos) { lienzo.renderMode = RenderMode.ScreenSpaceOverlay; lienzo.worldCamera = null; }
            RenderTexture.active = anterior; RenderTexture.ReleaseTemporary(rt);
            cam.aspect = aspectoAnterior;
        }
    }
    static void Terminar(string error)
    {
        if (terminado) return;
        terminado = true;
        checks.Add(error == null ? "OK: " + checks.Count + " comprobaciones." : "FAIL: " + error);
        File.WriteAllLines(Output, checks);
        Application.logMessageReceived -= Log;
        EditorApplication.update -= Vigilar;
        EditorApplication.playModeStateChanged -= Iniciar;
        EditorApplication.isPlaying = false;
        EditorApplication.delayCall += () => EditorApplication.Exit(error == null ? 0 : 1);
    }
}

