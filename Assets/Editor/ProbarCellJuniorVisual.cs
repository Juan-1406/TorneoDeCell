using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class ProbarCellJuniorVisual
{
    static readonly List<string> checks=new List<string>();
    static double inicio;
    static bool terminado;
    static string etapa="inicio";
    static Health jugador;
    static readonly BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
    public static void Run()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Usar batchmode.");
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Level1_Renato.unity");
        SesionJuego.Reiniciar();inicio=EditorApplication.timeSinceStartup;
        Application.logMessageReceived+=Log;EditorApplication.update+=Vigilar;
        EditorApplication.playModeStateChanged+=Iniciar;EditorApplication.isPlaying=true;
    }
    static void Iniciar(PlayModeStateChange e)
    {
        if(e!=PlayModeStateChange.EnteredPlayMode)return;
        var go=new GameObject("Pruebas Cell Junior");UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<Health>().StartCoroutine(Pruebas());
    }
    static EnemigoMenor Crear(bool ranged,float x)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemigos/EnemigoMenor"+(ranged?"Distancia":"")+".prefab");
        var e=UnityEngine.Object.Instantiate(prefab,new Vector3(x,-2.7f,0),Quaternion.identity).GetComponent<EnemigoMenor>();
        e.Configurar(jugador,-12.25f,12.25f);Set(e,"siguienteAtaque",Time.time+1000);return e;
    }
    static IEnumerator Pruebas()
    {
        yield return null;
        UnityEngine.Object.FindFirstObjectByType<DirectorOleadas>().enabled=false;
        jugador=GameObject.FindGameObjectWithTag("Player").GetComponent<Health>();
        jugador.Invulnerable=true;jugador.GetComponent<PlayerMovement>().enabled=false;jugador.GetComponent<PlayerShooting>().enabled=false;
        Time.timeScale=2;
        var melee=Crear(false,-3.5f);var ranged=Crear(true,4.5f);
        var m=melee.GetComponent<PresentacionEnemigoMenor>();var r=ranged.GetComponent<PresentacionEnemigoMenor>();
        Check(m!=null&&r!=null,"Los dos prefabs incluyen presentación de Cell Junior");
        Check(melee.Entrando&&ranged.Entrando&&!melee.GetComponent<Collider2D>().enabled,"La nueva gráfica conserva la entrada sin colisión");
        yield return new WaitUntil(()=>!melee.Entrando&&!ranged.Entrando);
        Check(!melee.Entrando&&!ranged.Entrando&&melee.GetComponent<Collider2D>().enabled,"La entrada termina con los tiempos y colisiones existentes");
        Check(melee.Vida.VidaMaxima==20&&ranged.Vida.VidaMaxima==15,"Se conservan 20 de salud melee y 15 ranged");
        Check((float)Get(melee,"velocidad")==2.3f&&(float)Get(ranged,"velocidad")==1.5f,"Se conservan velocidades de persecución y reposicionamiento");
        Check((float)Get(melee,"velocidadCarga")==7.5f&&(float)Get(melee,"duracionCarga")==.32f,"Se conserva velocidad y duración de la carga");
        Check((int)Get(ranged,"disparosRafaga")==2&&(int)Get(ranged,"proyectilesAbanico")==3,"Se conservan dos ráfagas de tres proyectiles");
        Check(m.Orientacion==1&&r.Orientacion==-1,"Ambas variantes miran al jugador desde flancos opuestos");
        Check(!melee.GetComponent<SpriteRenderer>().enabled&&!ranged.GetComponent<SpriteRenderer>().enabled,"Los cuadrados anteriores no se dibujan");
        foreach(var e in new[]{melee,ranged})
        {
            var p=e.GetComponent<PresentacionEnemigoMenor>();
            Check(e.transform.localScale==Vector3.one&&Mathf.Approximately(e.GetComponent<CircleCollider2D>().radius,.55f),"La gráfica no escala ni sustituye el collider: "+e.Tipo);
            Check(e.GetComponent<Rigidbody2D>().bodyType==RigidbodyType2D.Kinematic&&e.GetComponent<Rigidbody2D>().gravityScale==0,"La física lateral conserva su configuración: "+e.Tipo);
            Check(p.Imagen.color==Color.white,"El cuerpo azul se dibuja sin tinte de polera: "+e.Tipo);
            Check(p.Imagen.bounds.size.y>1.4f&&p.Imagen.bounds.size.y<1.8f,"La proporción se mantiene legible junto al jugador: "+e.Tipo);
            Check(p.Imagen.sprite.name.Contains(e.Tipo==EnemigoMenor.TipoAtaque.Cercano?"Melee":"Ranged"),"Cada prefab utiliza su propio atlas: "+e.Tipo);
            e.enabled=false;
            var clips=p.Animador.runtimeAnimatorController.animationClips;
            Check(clips.Length==(e.Tipo==EnemigoMenor.TipoAtaque.Cercano?7:8),"Conjunto completo de estados visuales: "+e.Tipo);
            var sprites=new HashSet<string>();
            foreach(var clip in clips)
            {
                foreach(var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                    foreach(var k in AnimationUtility.GetObjectReferenceCurve(clip,binding))
                        if(k.value is Sprite s)sprites.Add(s.name);
                p.Animador.Play(clip.name,0,0);p.Animador.Update(0);
                Check(e.transform.localScale==Vector3.one&&Mathf.Approximately(e.GetComponent<CircleCollider2D>().radius,.55f),"El clip conserva la colisión: "+e.Tipo+"/"+clip.name);
            }
            Check(sprites.Count==9,"Nueve poses distintas conectadas: "+e.Tipo);
            p.Animador.Play("Reposo",0,0);p.Animador.Update(0);
        }
        Capturar("01-variantes-en-arena");
        etapa="melee";
        melee.GetComponent<Rigidbody2D>().position=new Vector2(-3.2f,-2.7f);melee.enabled=true;
        Set(melee,"siguienteAtaque",Time.time);
        yield return new WaitUntil(()=>melee.EstadoActual==EnemigoMenor.EstadoEnemigo.Amago);
        yield return null;
        Check(m.AnimacionActual=="PrepararGolpe"&&m.Imagen.sprite.name.Contains("Preparar"),"El amago utiliza anticipación con puño recogido");
        Capturar("02-melee-preparacion");
        yield return new WaitUntil(()=>melee.EstadoActual==EnemigoMenor.EstadoEnemigo.Carga);
        yield return null;
        Check(m.AnimacionActual=="Golpear"&&m.Imagen.sprite.name.Contains("Golpear"),"El puñetazo coincide con la carga física existente");
        Capturar("03-melee-golpe");
        yield return new WaitUntil(()=>melee.EstadoActual==EnemigoMenor.EstadoEnemigo.Recuperacion);
        yield return null;
        Check(m.AnimacionActual=="Recuperar","La recuperación respeta el estado de la IA");
        melee.enabled=false;
        etapa="ranged";
        ranged.enabled=true;Set(ranged,"siguienteAtaque",Time.time);
        yield return new WaitUntil(()=>ranged.EstadoActual==EnemigoMenor.EstadoEnemigo.PreparandoRafaga);
        yield return null;
        Check(r.AnimacionActual=="Apuntar","El tirador apunta antes de cargar");
        yield return new WaitForSeconds(.23f);
        Check(r.AnimacionActual=="PrepararProyectil"&&r.Imagen.sprite.name.Contains("Preparar"),"La carga tiene una postura distinta del disparo");
        Capturar("04-ranged-preparacion");
        Vector2 anunciada=ranged.PuntoApuntado-(Vector2)ranged.transform.position;
        yield return new WaitUntil(()=>ranged.EstadoActual==EnemigoMenor.EstadoEnemigo.Disparando);
        yield return null;
        Check(r.AnimacionActual=="Disparar"&&r.Imagen.sprite.name.Contains("Disparar"),"La palma dispara en sincronía con la ráfaga");
        var balas=Balas();
        Check(balas.Length==3,"La primera emisión conserva tres proyectiles");
        Check(balas.Any(b=>Vector2.Dot(b.GetComponent<Rigidbody2D>().linearVelocity.normalized,anunciada.normalized)>.99f),"La animación no cambia la dirección preparada");
        Capturar("05-ranged-disparo");
        yield return new WaitForSeconds(.30f);
        Check(Balas().Length==6,"La segunda emisión conserva el intervalo y seis proyectiles totales");
        Check(ranged.EstadoActual==EnemigoMenor.EstadoEnemigo.Reposicion,"El disparo conserva el repliegue inmediato de la IA");
        yield return new WaitForSeconds(.22f);
        Check(r.AnimacionActual=="Desplazar","El repliegue usa animación de pasos");
        ranged.enabled=false;
        foreach(var b in Balas())UnityEngine.Object.Destroy(b.gameObject);
        etapa="daño y pausa";
        melee.Vida.RecibirDanio(1);
        Check(m.AnimacionActual=="Danio"&&melee.Vida.VidaActual==19,"El impacto muestra reacción y aplica el daño normal");
        Capturar("06-danio");
        Time.timeScale=0;SesionJuego.EnPausa=true;yield return null;
        Vector3 pos=ranged.transform.position;float t=r.Animador.GetCurrentAnimatorStateInfo(0).normalizedTime;
        yield return new WaitForSecondsRealtime(.2f);
        Check(pos==ranged.transform.position&&Mathf.Abs(t-r.Animador.GetCurrentAnimatorStateInfo(0).normalizedTime)<.001f,"La pausa congela física y Animator");
        SesionJuego.EnPausa=false;Time.timeScale=2;
        etapa="derrota";
        int bajas=0;melee.Eliminado+=e=>bajas++;
        melee.Vida.RecibirDanio(999);yield return null;
        var salida=UnityEngine.Object.FindFirstObjectByType<RetirarVisualEnemigo>();
        Check(bajas==1&&melee==null,"La baja se contabiliza una sola vez y retira inmediatamente al enemigo");
        Check(salida!=null&&salida.GetComponentInChildren<Collider2D>()==null&&salida.GetComponentInChildren<Health>()==null,"La caída visual no conserva hitbox, salud ni IA");
        Check(salida.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Muerte"),"La caída utiliza su clip de muerte");
        Capturar("07-caida");
        Time.timeScale=0;SesionJuego.EnPausa=true;yield return new WaitForSecondsRealtime(.2f);
        Check(salida!=null,"La pausa conserva la caída sin consumir su tiempo");
        SesionJuego.EnPausa=false;Time.timeScale=2;
        yield return new WaitForSeconds(.45f);
        Check(salida==null,"La caída se desvanece y no deja objetos gráficos");
        ranged.Vida.RecibirDanio(999);yield return new WaitForSeconds(.45f);
        Check(UnityEngine.Object.FindFirstObjectByType<RetirarVisualEnemigo>()==null,"Ambas variantes completan y retiran su muerte visual");
        Terminar(null);
    }
    static Bullet[] Balas()=>UnityEngine.Object.FindObjectsByType<Bullet>(FindObjectsSortMode.None).Where(x=>x.EsReflejable).ToArray();
    static object Get(object o,string n)=>o.GetType().GetField(n,flags).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,flags).SetValue(o,v);
    static void Check(bool ok,string s){if(!ok)throw new Exception(s);checks.Add("PASS: "+s);}
    static void Vigilar(){if(!terminado&&EditorApplication.timeSinceStartup-inicio>180)Terminar("Timeout: "+etapa);}
    static void Log(string m,string stack,LogType t){if(!terminado&&(t==LogType.Error||t==LogType.Exception))Terminar(etapa+": "+m+"\n"+stack);}
    static void Terminar(string error)
    {
        if(terminado)return;terminado=true;
        checks.Add(error==null?"OK: "+checks.Count+" comprobaciones de Cell Junior.":"FAIL: "+error);
        File.WriteAllLines("Logs/PruebasCellJuniorVisual.txt",checks);
        Application.logMessageReceived-=Log;EditorApplication.update-=Vigilar;EditorApplication.playModeStateChanged-=Iniciar;
        EditorApplication.isPlaying=false;EditorApplication.delayCall+=()=>EditorApplication.Exit(error==null?0:1);
    }
    static void Capturar(string nombre)
    {
        if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)return;
        Directory.CreateDirectory("Logs/CellJuniorVisual");
        var cam=Camera.main;float aspect=cam.aspect;cam.aspect=16f/9;
        var rt=RenderTexture.GetTemporary(1280,720,24);var antes=RenderTexture.active;
        var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(x=>x.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
        try
        {
            foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=cam;c.planeDistance=1;}
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
            RenderTexture.active=rt;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);
            tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes("Logs/CellJuniorVisual/"+nombre+".png",tex.EncodeToPNG());UnityEngine.Object.Destroy(tex);
        }
        finally
        {
            foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceOverlay;c.worldCamera=null;}
            RenderTexture.active=antes;RenderTexture.ReleaseTemporary(rt);cam.aspect=aspect;
        }
    }
}
