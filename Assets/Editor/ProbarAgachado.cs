using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using static ProbarMenuPausa;

public static class ProbarAgachado
{
    public static void Run() => Ejecutar("Logs/Agachado", Partidas);

    private static IEnumerator Partidas()
    {
        yield return Partida("Level1_Renato");
        yield return Partida("Level2_PadrePasta");
    }

    private static IEnumerator Partida(string escena)
    {
        SesionJuego.Reiniciar();
        PermitirReintento(escena);
        SceneManager.LoadScene(escena);
        yield return null;
        yield return new WaitForSeconds(.4f);

        // Aísla la trayectoria de la nota para comparar las mismas alturas en ambas posturas.
        if (Object.FindFirstObjectByType<DirectorOleadas>() is DirectorOleadas oleadas)
            oleadas.SendMessage("Detener");
        if (Object.FindFirstObjectByType<PadrePasta>() is PadrePasta padre)
            padre.enabled = false;

        var mateo = GameObject.FindGameObjectWithTag("Player");
        var cuerpo = mateo.GetComponent<BoxCollider2D>();
        var movimiento = mateo.GetComponent<PlayerMovement>();
        var vida = mateo.GetComponent<Health>();
        var pose = mateo.transform.Find("Pose anclada de Mateo").GetComponent<SpriteRenderer>();
        var dePie = cuerpo.bounds;
        float piesPose = pose.bounds.min.y;
        float alturaDePie = cuerpo.size.y;

        MantenerTeclas(Key.S);
        yield return new WaitForSeconds(.12f);
        Check(cuerpo.size.y < alturaDePie && Mathf.Abs(cuerpo.bounds.min.y - dePie.min.y) < .025f &&
            pose.enabled && pose.sprite.name == "Mateo_Aterrizaje" && Mathf.Abs(pose.bounds.min.y - piesPose) < .025f,
            $"{escena} mantiene los pies y reduce la altura de la pose y la colisión al mantener S.");
        float alturaBaja = cuerpo.size.y;
        MantenerTeclas();
        yield return null;
        yield return null;
        Check(cuerpo.size.y == alturaDePie, $"{escena} recupera la colisión de pie al soltar S.");

        // Usa la misma rutina de salto que llama Espacio, con S mantenida por Input System.
        MantenerTeclas(Key.S);
        yield return null;
        yield return null;
        movimiento.SendMessage("Saltar");
        yield return new WaitForSeconds(.13f);
        Check(cuerpo.size.y == alturaDePie && cuerpo.bounds.min.y > dePie.min.y + .25f,
            $"{escena} permite saltar desde el agachado y se estira en el aire.");
        yield return new WaitUntil(() => cuerpo.size.y == alturaBaja);
        Check(Mathf.Abs(cuerpo.bounds.min.y - dePie.min.y) < .025f, $"{escena} vuelve a agacharse al aterrizar con S.");

        MantenerTeclas(Key.S, Key.Escape);
        yield return null;
        yield return null;
        MantenerTeclas();
        yield return null;
        yield return null;
        Check(SesionJuego.EnPausa && cuerpo.size.y == alturaBaja, $"{escena} congela la postura durante la pausa.");
        yield return Tecla(Key.Escape);
        Check(!SesionJuego.EnPausa && cuerpo.size.y == alturaDePie, $"{escena} aplica la liberación de S al continuar.");

        float alto = dePie.max.y - .14f;
        float bajo = dePie.min.y + .48f;
        int vidaInicial = vida.VidaActual;
        yield return LanzarNota(cuerpo, alto);
        Check(vida.VidaActual == vidaInicial - 1, $"{escena} recibe la nota alta si Mateo está de pie.");
        vida.RestaurarVida(vidaInicial);
        MantenerTeclas(Key.S);
        yield return new WaitUntil(() => cuerpo.size.y == alturaBaja);
        yield return Capturar(escena + "-Agachado", 1280, 720);
        yield return LanzarNota(cuerpo, alto);
        Check(vida.VidaActual == vidaInicial, $"{escena} esquiva la misma nota alta agachado.");
        yield return LanzarNota(cuerpo, bajo);
        Check(vida.VidaActual == vidaInicial - 1 && cuerpo.size.y == alturaBaja,
            $"{escena} recibe la nota baja y conserva la postura al mantener S.");
        MantenerTeclas();
    }

    private static IEnumerator LanzarNota(BoxCollider2D cuerpo, float altura)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<Bullet>(
            "Assets/_Project/Prefabs/Proyectiles/Padre Pasta/NotaMusical.prefab");
        Vector2 salida = new Vector2(cuerpo.bounds.center.x + 3f, altura);
        Bullet nota = Object.Instantiate(prefab, salida, Quaternion.identity);
        nota.Disparar(Vector2.left);
        yield return new WaitForSeconds(.65f);
        if (nota != null) Object.Destroy(nota.gameObject);
    }
}
