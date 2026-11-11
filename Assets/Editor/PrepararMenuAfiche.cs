using System;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>Reconstrucción reproducible de la escena, sin modificar el resto del juego.</summary>
public static class PrepararMenuAfiche
{
    const string Arte = "Assets/_Project/Art/UI/MenuAfiche/";
    const string Escena = "Assets/_Project/Scenes/MainMenu.unity";

    [MenuItem("Torneo/Menú afiche/Reconstruir escena aprobada")]
    public static void Run()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory("Logs/MenuAfiche");
        if (!File.Exists("Logs/MenuAfiche/MainMenuAntes.unity.txt"))
            File.Copy(Escena, "Logs/MenuAfiche/MainMenuAntes.unity.txt");
        Importar("AficheElegido.png", false);
        Importar("MuroSinPersonajes.png", false);
        Importar("TituloPintado.png", true);
        GenerarAudioAfiche.Crear();
        var fuente = AssetDatabase.LoadAssetAtPath<Texture2D>(Arte + "AficheElegido.png");
        var muro = AssetDatabase.LoadAssetAtPath<Texture2D>(Arte + "MuroSinPersonajes.png");
        var logo = AssetDatabase.LoadAssetAtPath<Texture2D>(Arte + "TituloPintado.png");
        if (fuente == null || muro == null || logo == null) throw new InvalidOperationException("Faltan las imágenes aprobadas del afiche.");
        Material materialBoton = Material("BotonMetalico", "Torneo/UI/BotonAfiche");
        Material materialTitulo = Material("TituloPintado", "Torneo/UI/TituloAfiche");

        var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camara = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(UniversalAdditionalCameraData));
        camara.tag = "MainCamera";
        var cam = camara.GetComponent<Camera>(); cam.orthographic = true; cam.orthographicSize = 5;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.025f, .07f, .15f);
        camara.transform.position = new Vector3(0, 0, -10);

        var eventos = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventos.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        eventos.GetComponent<InputSystemUIInputModule>().deselectOnBackgroundClick = false;
        var sistema = eventos.GetComponent<EventSystem>(); sistema.sendNavigationEvents = true;
        var canvasGo = new GameObject("Menú · Afiche vivo", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1672, 941); scaler.matchWidthOrHeight = .5f;
        var menu = canvasGo.AddComponent<MenuAfiche>();

        var extension = Imagen("Muro extendido · otras pantallas", canvasGo.transform, muro);
        Estirar(extension.rectTransform); extension.color = new Color(.65f, .68f, .74f);
        extension.uvRect = new Rect(.08f, .20f, .84f, .52f);
        var marco = Rectangulo("Composición 16x9 · sin recortar retratos", canvasGo.transform, new Vector2(1672, 941), Vector2.zero);
        marco.gameObject.AddComponent<RectMask2D>();
        var fondo = Imagen("Muro de barrio · profundidad", marco, muro);
        fondo.rectTransform.sizeDelta = new Vector2(1706, 960);

        // Coordenadas del afiche elegido, de arriba hacia abajo; las UV mantienen los dibujos originales.
        var renato = Recorte("01 · René Puente", marco, fuente, P(
            706,97, 814,58, 885,87, 966,91, 1037,113, 1120,104, 1210,147,
            1174,181, 1158,236, 1144,282, 1128,323, 1110,366, 1100,416, 1083,460,
            1077,488, 1062,527, 1001,540, 950,543, 907,531, 855,540, 805,538,
            752,558, 699,572, 635,584, 635,540, 654,482, 665,433, 669,378,
            678,327, 681,281, 691,231, 700,185, 694,144));
        var padre = Recorte("02 · Padre Pasta", marco, fuente, P(
            1210,139, 1250,103, 1318,99, 1380,81, 1430,114, 1490,99, 1550,111,
            1590,87, 1672,83, 1684,83, 1684,416, 1672,424, 1625,444, 1570,448,
            1510,461, 1455,458, 1400,483, 1340,496, 1285,505, 1230,514,
            1165,526, 1100,540, 1053,544, 1065,499, 1089,459, 1108,414,
            1118,365, 1136,321, 1152,280, 1166,234, 1182,179));
        var cantante = Recorte("03 · JBL La Voz", marco, fuente, P(
            638,568, 710,548, 783,531, 840,526, 915,521, 970,525, 1035,518,
            1110,512, 1210,500, 1184,570, 1186,610, 1194,648, 1185,700,
            1160,756, 1145,802, 1129,858, 1112,903, 1098,953, 553,953,
            563,941, 590,903, 605,854, 616,820, 614,781, 629,736, 624,697,
            637,655, 631,610));
        var camarografo = Recorte("04 · Diddy González", marco, fuente, P(
            1202,492, 1285,489, 1340,480, 1400,467, 1455,442, 1510,445,
            1570,432, 1625,428, 1672,408, 1684,408, 1684,953, 1080,953,
            1096,902, 1113,856, 1129,800, 1144,754, 1169,698, 1178,648,
            1170,610, 1168,570));
        RectTransform[] retratos = { renato.rectTransform, padre.rectTransform, cantante.rectTransform, camarografo.rectTransform };
        foreach (var rt in retratos) rt.gameObject.AddComponent<CanvasGroup>();

        var luces = new[]
        {
            LuzEnRecorte("Parlante · aro grande", padre, new Vector2(1584, 302), new Vector2(110, 110), new Color(.1f,.55f,1,.35f)),
            LuzEnRecorte("Parlante · aro pequeño", padre, new Vector2(1436, 257), new Vector2(40, 40), new Color(.1f,.55f,1,.3f))
        };
        var camLuz = LuzEnRecorte("Cámara · luz cálida", camarografo, new Vector2(1572, 615), new Vector2(104, 100), new Color(1,.7f,.2f,.24f));
        var grabando = LuzEnRecorte("Cámara · indicador REC", camarografo, new Vector2(1498,706),
            new Vector2(16,10), new Color(1,.16f,.06f,.75f));
        grabando.IndicadorGrabacion = true;
        var calle = Luz("Farol · luz ambiente", marco, new Vector2(1590 - 836, 470.5f - 40), new Vector2(160, 90), new Color(1,.6f,.2f,.25f));
        foreach (Vector2 p in new[] { new Vector2(1100,82), new Vector2(1168,78), new Vector2(1261,74) })
            Luz("Ventana cálida", fondo.transform, new Vector2(p.x - 836, 470.5f - p.y), new Vector2(20, 24), new Color(1,.65f,.25f,.15f));

        var titulo = Imagen("Título · letras de muro aprobadas", marco, logo);
        titulo.rectTransform.sizeDelta = new Vector2(666, 426);
        titulo.rectTransform.anchoredPosition = new Vector2(350 - 836, 470.5f - 280);
        titulo.material = materialTitulo;
        var introTitulo = titulo.gameObject.AddComponent<CanvasGroup>();
        var botones = Rectangulo("Botones · mouse, teclado y mando", marco, new Vector2(1672,941), Vector2.zero);
        var introBotones = botones.gameObject.AddComponent<CanvasGroup>();
        var jugar = Boton("JUGAR", botones, fuente, materialBoton, menu, P(
            82,535, 106,502, 136,502, 147,512, 552,512, 565,503, 581,507,
            611,535, 611,566, 602,576, 611,586, 611,626, 581,660,
            144,660, 133,653, 109,653, 82,626, 82,588, 91,577, 82,566));
        var salir = Boton("SALIR", botones, fuente, materialBoton, menu, P(
            102,722, 132,693, 164,693, 174,701, 545,701, 554,694, 561,695,
            587,722, 587,792, 557,820, 132,820, 103,791));
        var nj = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = salir, selectOnDown = salir };
        var ns = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = jugar, selectOnDown = jugar };
        jugar.navigation = nj; salir.navigation = ns; sistema.firstSelectedGameObject = jugar.gameObject;

        var polvo = Rectangulo("Polvo · veinte motas suaves", marco, new Vector2(1672,941), Vector2.zero).gameObject.AddComponent<PolvoAfiche>();
        polvo.raycastTarget = false;
        menu.Configurar(marco, fondo.rectTransform, titulo.rectTransform, retratos, jugar, salir,
            introTitulo, introBotones, luces, camLuz, calle,
            Audio("Torneo_Nocturno"), Audio("Seleccion"), Audio("Confirmar"), Audio("Papel"));

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena, Escena);
        AssetDatabase.SaveAssets();
        File.WriteAllText("Logs/MenuAfiche/Preparacion.txt", "OK: MainMenu afiche vivo configurado. Cuatro retratos, título aprobado, dos botones navegables, capas, luces, audio y transición.\n");
        Debug.Log("Menú afiche preparado en la copia del proyecto.");
    }

    public static void PrepararYProbar()
    {
        Run();
        ProbarMenuAfiche.Run();
    }

    static void Importar(string archivo, bool alpha)
    {
        string path = Arte + archivo;
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var i = (TextureImporter)AssetImporter.GetAtPath(path);
        i.textureType = TextureImporterType.Default; i.alphaIsTransparency = alpha;
        i.alphaSource = TextureImporterAlphaSource.FromInput; i.mipmapEnabled = false;
        i.wrapMode = TextureWrapMode.Clamp; i.filterMode = FilterMode.Bilinear;
        i.maxTextureSize = 2048; i.textureCompression = TextureImporterCompression.Uncompressed;
        i.npotScale = TextureImporterNPOTScale.None; i.SaveAndReimport();
    }
    static Material Material(string nombre, string shader)
    {
        string path = Arte + nombre + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader s = Shader.Find(shader);
        if (s == null) throw new InvalidOperationException("No se encontró el shader " + shader);
        if (m == null) { m = new Material(s); AssetDatabase.CreateAsset(m, path); }
        m.shader = s; return m;
    }
    static AudioClip Audio(string nombre) => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/MenuAfiche/" + nombre + ".wav");
    static RectTransform Rectangulo(string nombre, Transform padre, Vector2 tam, Vector2 pos)
    {
        var rt = new GameObject(nombre, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(padre, false); rt.anchorMin = rt.anchorMax = new Vector2(.5f,.5f);
        rt.sizeDelta = tam; rt.anchoredPosition = pos; return rt;
    }
    static void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
    static RawImage Imagen(string nombre, Transform padre, Texture2D textura)
    {
        var rt = Rectangulo(nombre, padre, new Vector2(1672,941), Vector2.zero);
        var i = rt.gameObject.AddComponent<RawImage>(); i.texture = textura; i.raycastTarget = false; return i;
    }
    static Vector2[] P(params float[] xy)
    {
        var p = new Vector2[xy.Length / 2];
        for (int i = 0; i < p.Length; i++) p[i] = new Vector2(xy[i*2], xy[i*2+1]);
        return p;
    }
    static RecorteAfiche Recorte(string nombre, Transform padre, Texture2D fuente, Vector2[] puntos)
    {
        var rt = Rectangulo(nombre, padre, Vector2.one, Vector2.zero);
        var r = rt.gameObject.AddComponent<RecorteAfiche>(); r.Configurar(fuente, puntos); return r;
    }
    static LuzAfiche Luz(string nombre, Transform padre, Vector2 pos, Vector2 tam, Color color)
    {
        var rt = Rectangulo(nombre, padre, tam, pos);
        var luz = rt.gameObject.AddComponent<LuzAfiche>(); luz.color = color; luz.raycastTarget = false; return luz;
    }
    static LuzAfiche LuzEnRecorte(string nombre, RecorteAfiche recorte, Vector2 punto, Vector2 tam, Color color)
    {
        Vector2 centro = new Vector2(recorte.rectTransform.anchoredPosition.x + 836, 470.5f - recorte.rectTransform.anchoredPosition.y);
        return Luz(nombre, recorte.transform, new Vector2(punto.x - centro.x, centro.y - punto.y), tam, color);
    }
    static BotonAfiche Boton(string nombre, Transform padre, Texture2D fuente, Material material, MenuAfiche menu, Vector2[] puntos)
    {
        var r = Recorte(nombre + " · metal y letras", padre, fuente, puntos);
        var rt = Rectangulo(nombre, padre, r.rectTransform.sizeDelta, r.rectTransform.anchoredPosition);
        r.rectTransform.SetParent(rt, false); r.rectTransform.anchoredPosition = Vector2.zero; r.raycastTarget = true;
        var luz = Luz("Dorado de selección", rt, Vector2.zero, rt.sizeDelta + new Vector2(100,90), new Color(1,.6f,.1f,0));
        luz.transform.SetAsFirstSibling();
        var b = rt.gameObject.AddComponent<BotonAfiche>(); b.Configurar(r, luz, material, menu);
        return b;
    }
}
