using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class ProbarRenatoVisual
{
    static readonly List<string> checks = new List<string>();
    static double inicio;
    static bool terminado, teniaProgreso;
    static int progreso;
    static string etapa="inicio";
    static DirectorOleadas director;
    static Health player,boss;
    static BossController controlador;
    static SeguirCamaraX movimiento;
    static PresentacionRenato vista;
    static MonoBehaviour runner;
    static readonly HashSet<string> posesAutomaticas=new HashSet<string>();
    public static void Run()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Usar batchmode.");
        teniaProgreso=PlayerPrefs.HasKey("jefe_derrotado_0");
        progreso=PlayerPrefs.GetInt("jefe_derrotado_0",0);
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Level1_Renato.unity");
        SesionJuego.Reiniciar(); SesionJuego.GuardarProgresoOleadas("Level1_Renato",3);
        inicio=EditorApplication.timeSinceStartup;
        Application.logMessageReceived+=Log; EditorApplication.update+=Vigilar;
        EditorApplication.playModeStateChanged+=Iniciar;
        EditorApplication.isPlaying=true;
    }
    static void Iniciar(PlayModeStateChange cambio)
    {
        if(cambio!=PlayModeStateChange.EnteredPlayMode)return;
        var go=new GameObject("Pruebas visuales Renato");UnityEngine.Object.DontDestroyOnLoad(go);
        runner=go.AddComponent<Health>();runner.StartCoroutine(Pruebas());
    }
    static void Vincular()
    {
        director=UnityEngine.Object.FindFirstObjectByType<DirectorOleadas>();
        player=GameObject.FindGameObjectWithTag("Player").GetComponent<Health>();
        boss=GameObject.FindGameObjectWithTag("Boss").GetComponent<Health>();
        controlador=boss.GetComponent<BossController>();
        movimiento=boss.GetComponent<SeguirCamaraX>();
        vista=boss.GetComponent<PresentacionRenato>();
        player.Invulnerable=true; player.GetComponent<PlayerMovement>().enabled=false;
        player.GetComponent<PlayerShooting>().enabled=false;
        Time.timeScale=3f;
    }
    static IEnumerator Pruebas()
    {
        yield return null;Vincular();
        Check(vista!=null && vista.Animador!=null,"Presentación y Animator conectados");
        Check(!vista.Visible && !controlador.enabled,"Renato no aparece durante la preparación de la fase");
        Check(vista.Animador.runtimeAnimatorController.animationClips.Length==14,"Catorce clips editables disponibles");
        Check(boss.GetComponent<SpriteRenderer>().sprite==null,"El sprite anterior no se duplica detrás del nuevo");
        Check(boss.transform.localScale==Vector3.one,"La raíz de combate no escala proyectiles ni hijos");
        yield return new WaitUntil(()=>director.JefeActivo);
        yield return new WaitForSeconds(0.25f);
        Capturar("01-entrada");
        yield return new WaitForSeconds(1f);
        controlador.StopAllCoroutines();movimiento.enabled=false;movimiento.enabled=true;vista.Reposo();
        yield return new WaitForSeconds(0.25f);
        Check(vista.Visible && vista.Animador.enabled,"El jefe se muestra al habilitar su combate");
        float ratio=vista.Cuerpo.bounds.size.y/player.GetComponent<SpriteRenderer>().bounds.size.y;
        Check(ratio>3.1f && ratio<4.2f,"Presencia de busto imponente con escala acotada: "+ratio.ToString("F2"));
        Check(vista.MedioCuerpo && vista.Cuerpo.sprite.name.StartsWith("RenatoBusto_"),"La presentación utiliza las poses de medio cuerpo");
        var plano=UnityEngine.Object.FindFirstObjectByType<PrimerPlanoArenaRenato>();
        Check(plano!=null&&plano.Franja!=null,"La cancha original tiene su capa de primer plano");
        Check(Mathf.Abs(plano.BordeY-vista.BordePrimerPlanoY)<0.025f,"La cintura y el recorte de hitbox comparten el borde de cancha");
        Check(plano.Franja.sortingOrder>vista.Cuerpo.sortingOrder&&plano.Franja.sortingOrder<player.GetComponent<SpriteRenderer>().sortingOrder,"El jugador conserva prioridad visual delante de Renato y la cancha");
        Check(plano.Franja.sprite.texture==GameObject.Find("FondoMapaRenato").GetComponent<SpriteRenderer>().sprite.texture,"El primer plano reutiliza exactamente el fondo establecido");
        Check(Mathf.Abs(GameObject.Find("Ground").GetComponent<Collider2D>().bounds.size.x-26.1f)<0.01f,"Se conserva el ancho de combate de la arena");
        Check(boss.VidaMaxima==500,"Se conserva la salud máxima del jefe");
        var box=boss.GetComponent<BoxCollider2D>();
        Check(box.enabled && box.isTrigger && box.GetComponent<Health>()==boss,"La hitbox conserva salud y capa del jefe");
        Capturar("02-reposo");
        var rafaga=boss.GetComponentsInChildren<AtaqueRafaga>().First(x=>x.name=="Rafaga3");
        var abanico=boss.GetComponentInChildren<AtaqueAbanico>();
        var externo=boss.GetComponentsInChildren<AtaqueExterno>().First();
        etapa="rafaga";
        yield return movimiento.ColocarParaAtaque(rafaga,player.transform);
        float preparacion=vista.Preparar(rafaga);
        yield return new WaitForSeconds(preparacion*0.6f);
        Check(vista.Cuerpo.sprite.name.Contains("Inhalacion"),"La ráfaga tiene inhalación reconocible");
        Capturar("03-inhalacion");
        yield return new WaitForSeconds(preparacion*0.4f);
        vista.IniciarAtaque(rafaga);yield return null;yield return null;
        LimpiarBalas();Vector2 nariz=vista.OrigenDisparo;
        int emisionesRafaga=0;
        Action<Bullet> contarRafaga=b=>emisionesRafaga++;
        rafaga.ProyectilEmitido+=contarRafaga;
        runner.StartCoroutine(rafaga.Ejecutar(player.transform));yield return null;
        var primera=Balas().FirstOrDefault(x=>x.EsReflejable);
        Check(primera!=null,"La ráfaga dispara normalmente");
        Check(Vector2.Distance(primera.transform.position,nariz)<1.3f,"El disparo nace junto a la nariz animada");
        Check(Vector2.Dot(primera.GetComponent<Rigidbody2D>().linearVelocity.normalized,((Vector2)player.transform.position-nariz).normalized)>0.96f,"El disparo sigue dirigido al jugador");
        Check(vista.Cuerpo.sprite.name.Contains("Estornudo"),"El estornudo coincide con la salida del proyectil");
        Capturar("04-rafaga");
        yield return new WaitForSeconds(0.95f);
        Check(emisionesRafaga==3,"La ráfaga base conserva sus tres proyectiles");
        rafaga.ProyectilEmitido-=contarRafaga;
        vista.Recuperar();yield return new WaitForSeconds(0.2f);
        Check(vista.Cuerpo.sprite.name.Contains("Recuperacion"),"La recuperación muestra la limpieza de la nariz");
        Capturar("05-recuperacion");
        yield return new WaitForSeconds(0.25f);vista.Reposo();LimpiarBalas();
        etapa="abanico";
        yield return movimiento.ColocarParaAtaque(abanico,player.transform);
        yield return new WaitForSeconds(vista.Preparar(abanico));
        vista.IniciarAtaque(abanico);yield return null;yield return null;
        runner.StartCoroutine(abanico.Ejecutar(player.transform));yield return null;
        Check(Balas().Count(x=>x.EsReflejable)==5,"El abanico conserva sus cinco proyectiles simultáneos");
        Check(vista.Cuerpo.sprite.name.Contains("Abanico"),"El abanico usa una postura frontal distinta");
        float ratioFan=vista.Cuerpo.bounds.size.y/player.GetComponent<SpriteRenderer>().bounds.size.y;
        Check(ratioFan>3.1f && ratioFan<4.6f,"El gesto amplio sigue dentro de una escala jugable");
        Capturar("06-abanico-frontal");
        yield return new WaitForSeconds(0.45f);LimpiarBalas();
        etapa="poses y hitboxes";
        string[] estados={"Reposo","Traslado","DisparoExterno","PrepararRafaga","DisparoRafaga","DisparoAbanico","Recuperacion","CambioFase","Danio"};
        var sprites=new HashSet<string>();
        foreach(string estado in estados)
        {
            vista.Animador.Play(estado,0,estado=="PrepararRafaga"?0.5f:0f);
            yield return null;yield return null;
            sprites.Add(vista.Cuerpo.sprite.name);
            var bounds=vista.Cuerpo.bounds;bounds.Expand(0.25f);
            Check(bounds.Contains(box.bounds.min)&&bounds.Contains(box.bounds.max),"Hitbox contenida en la silueta: "+estado);
            Check(bounds.Contains(vista.OrigenDisparo),"Origen de disparo dentro de la pose: "+estado);
            Check(box.bounds.min.y>=vista.BordePrimerPlanoY-0.01f,"La parte oculta del busto no recibe impactos: "+estado);
        }
        Check(sprites.Count==9,"Nueve perspectivas y expresiones distintas");
        etapa="fases y orientaciones";
        boss.RestaurarVida(300);Check(controlador.FaseActual==1,"La segunda fase conserva su umbral de salud");
        runner.StartCoroutine(vista.CambioFase(1));yield return new WaitForSeconds(0.1f);Capturar("07-cambio-fase");
        yield return new WaitForSeconds(0.8f);
        yield return movimiento.ColocarParaAtaque(externo,player.transform);
        if(boss.transform.position.x>player.transform.position.x)
            yield return movimiento.ColocarParaAtaque(externo,player.transform);
        Check(vista.Orientacion==-1,"El jefe situado a la izquierda mira hacia la derecha");
        Capturar("08-flanco-izquierdo");
        yield return new WaitForSeconds(vista.Preparar(rafaga));
        vista.IniciarAtaque(rafaga);yield return null;yield return null;
        Check(vista.OrigenDisparo.x>boss.transform.position.x,"La nariz cambia de lado con la orientación");
        LimpiarBalas();
        runner.StartCoroutine(rafaga.Ejecutar(player.transform));yield return null;
        Check(Balas().First(x=>x.EsReflejable).GetComponent<Rigidbody2D>().linearVelocity.x>0,"La ráfaga desde la izquierda viaja hacia el jugador");
        Capturar("09-rafaga-derecha");
        yield return new WaitForSeconds(1f);LimpiarBalas();
        boss.RestaurarVida(150);Check(controlador.FaseActual==2,"La fase furiosa conserva su umbral de salud");
        yield return vista.CambioFase(2);
        etapa="combate automatico";
        controlador.enabled=false;controlador.enabled=true;
        float fin=Time.time+20f;float minX=boss.transform.position.x,maxX=minX;
        while(Time.time<fin)
        {
            posesAutomaticas.Add(vista.Cuerpo.sprite.name);
            minX=Mathf.Min(minX,boss.transform.position.x);maxX=Mathf.Max(maxX,boss.transform.position.x);
            var suelo=GameObject.Find("Ground").GetComponent<Collider2D>().bounds;
            CheckContinuo(boss.GetComponent<BoxCollider2D>().bounds.min.x>=suelo.min.x-0.05f&&box.bounds.max.x<=suelo.max.x+0.05f,"El jefe salió de la arena");
            CheckContinuo(vista.Cuerpo.bounds.max.y<Camera.main.transform.position.y+Camera.main.orthographicSize-0.1f,"La cabeza del jefe salió del encuadre");
            CheckContinuo(boss.transform.position.y<=vista.BordePrimerPlanoY-0.55f,"La cintura del busto quedó flotando sobre la cancha");
            yield return null;
        }
        Check(posesAutomaticas.Count>=4,"El ciclo automático alterna preparación, ataque, orientación y recuperación");
        Check(maxX-minX>3f,"Los ataques producen cambios reales de sector");
        Capturar("10-fase-furiosa");
        Vector3 lugar=boss.transform.position;
        Time.timeScale=0;SesionJuego.EnPausa=true;yield return new WaitForSecondsRealtime(0.25f);
        Check(Vector3.Distance(lugar,boss.transform.position)<0.01f,"La pausa congela el desplazamiento del jefe");
        SesionJuego.EnPausa=false;Time.timeScale=3;
        etapa="parry y daño";
        LimpiarBalas();
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Proyectiles/Renato Puente/BalaEnemigoMenor.prefab");
        Vector2 inicioBala=(Vector2)box.bounds.center+Vector2.left*1f;
        var bala=UnityEngine.Object.Instantiate(prefab,inicioBala,Quaternion.identity).GetComponent<Bullet>();
        bala.Disparar(Vector2.left);bala.Reflejar();
        Check(Vector2.Dot(bala.GetComponent<Rigidbody2D>().linearVelocity.normalized,((Vector2)box.bounds.center-inicioBala).normalized)>0.99f,"El parry apunta al torso visible del busto");
        controlador.StopAllCoroutines();movimiento.enabled=false;
        int salud=boss.VidaActual;yield return new WaitForSeconds(0.2f);
        Check(boss.VidaActual<salud,"La bala reflejada alcanza la hitbox animada");
        etapa="reintento";
        player.RestaurarVida(3);player.RecibirDanio(999);yield return null;
        Check(!vista.Visible,"La presentación se detiene al cerrar el combate");
        yield return new WaitForSeconds(2f);Vincular();
        yield return new WaitUntil(()=>director.JefeActivo);yield return new WaitForSeconds(0.2f);
        Check(vista.Visible&&director.IndiceOleada==3,"El reintento restaura la presentación en la fase del jefe");
        etapa="derrota";
        box=boss.GetComponent<BoxCollider2D>();
        boss.RestaurarVida(1);boss.RecibirDanio(1);yield return null;
        Check(vista.Estado==PresentacionRenato.EstadoVisual.Derrota&&!box.enabled,"La derrota conserva su animación y desactiva la hitbox");
        Capturar("11-derrota");
        yield return new WaitForSeconds(0.8f);
        Check(boss==null,"La presentación de derrota termina y retira al jefe");
        Terminar(null);
    }
    static Bullet[] Balas()=>UnityEngine.Object.FindObjectsByType<Bullet>(FindObjectsSortMode.None);
    static void LimpiarBalas(){foreach(var b in Balas()){b.gameObject.SetActive(false);UnityEngine.Object.Destroy(b.gameObject);}}
    static void Check(bool value,string text){if(!value)throw new Exception(text);checks.Add("PASS: "+text);}
    static void CheckContinuo(bool value,string text){if(!value)throw new Exception(text);}
    static void Vigilar(){if(!terminado&&EditorApplication.timeSinceStartup-inicio>180)Terminar("Timeout: "+etapa);}
    static void Log(string mensaje,string trace,LogType tipo){if(!terminado&&(tipo==LogType.Error||tipo==LogType.Exception))Terminar(etapa+": "+mensaje+"\n"+trace);}
    static void Terminar(string error)
    {
        if(terminado)return;terminado=true;
        checks.Add(error==null?"OK: "+checks.Count+" comprobaciones de Renato.":"FAIL: "+error);
        File.WriteAllLines("Logs/PruebasRenatoVisual.txt",checks);
        if(teniaProgreso)PlayerPrefs.SetInt("jefe_derrotado_0",progreso);else PlayerPrefs.DeleteKey("jefe_derrotado_0");PlayerPrefs.Save();
        Application.logMessageReceived-=Log;EditorApplication.update-=Vigilar;EditorApplication.playModeStateChanged-=Iniciar;
        EditorApplication.isPlaying=false;EditorApplication.delayCall+=()=>EditorApplication.Exit(error==null?0:1);
    }
    static void Capturar(string nombre)
    {
        if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)return;
        Directory.CreateDirectory("Logs/RenatoVisual");
        var cam=Camera.main;float oldAspect=cam.aspect;cam.aspect=16f/9f;
        var rt=RenderTexture.GetTemporary(1280,720,24);var anterior=RenderTexture.active;
        var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(x=>x.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
        try
        {
            foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=cam;c.planeDistance=1;}
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
            RenderTexture.active=rt;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);
            tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();
            File.WriteAllBytes("Logs/RenatoVisual/"+nombre+".png",tex.EncodeToPNG());UnityEngine.Object.Destroy(tex);
        }
        finally
        {
            foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceOverlay;c.worldCamera=null;}
            RenderTexture.active=anterior;RenderTexture.ReleaseTemporary(rt);cam.aspect=oldAspect;
        }
    }
}

