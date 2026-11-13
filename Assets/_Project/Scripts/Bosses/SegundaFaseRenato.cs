using System.Collections;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SegundaFaseRenato : MonoBehaviour
{
    private enum Etapa { Primera, Transformacion, Segunda, Despedida, Victoria, Interrumpida }
    private static readonly string[] Despedida =
    {
        "No puede ser… ¿de verdad perdí?",
        "Pensé que con esta transformación me iba a alcanzar…",
        "Ya… ganaste. Solo déjame recuperar el aliento."
    };

    [Header("Transformación")]
    [SerializeField, Min(3f)] private float duracionTransformacion = 4.4f;
    [SerializeField] private Sprite retratoDorado;
    [SerializeField] private Sprite retratoAbatido;
    [Header("Fase 2")]
    [SerializeField, Min(1)] private int vidaTransformado = 375;
    [SerializeField] private Bullet proyectil;
    [SerializeField, Min(0.85f)] private float intervaloDisparo = 2.6f;
    [SerializeField, Min(1f)] private float rapidezProyectil = 7f;
    [SerializeField, Min(1)] private int danioDevuelto = 75;
    [SerializeField, Min(1)] private int danioEnemigos = 12;
    [SerializeField, Min(0.25f)] private float radioExplosion = 2.2f;
    [SerializeField, Min(0)] private int maximoRefuerzos = 2;
    [SerializeField, Min(0.2f)] private float intervaloRefuerzos = 5f;

    private Etapa etapa;
    private DirectorOleadas director;
    private Health jugador;
    private BossController controlador;
    private PresentacionRenato presentacion;
    private Collider2D hitbox;
    private TMP_FontAsset fuente;
    private DialogoRenato dialogo;
    private PlayerShooting disparo;
    private PlayerParry parry;
    private PlayerMovement movimiento;
    private Rigidbody2D cuerpoJugador;
    private PauseManager pausa;
    private bool bloqueado, parryPrevio, movimientoPrevio, simulacionPrevia, pausaPrevia, disparoPrevio;
    private bool rearmarDisparo;

    internal Health Vida { get; private set; }
    internal bool FueraDePrimeraFase => etapa != Etapa.Primera;
    internal bool CombateTransformado => etapa == Etapa.Segunda;
    internal bool DurantePresentacion => etapa == Etapa.Transformacion || etapa == Etapa.Despedida || etapa == Etapa.Victoria;
    internal bool ConservarAbatido => etapa == Etapa.Victoria;

    internal void Vincular(DirectorOleadas directorOleadas, Health objetivo, TMP_FontAsset fuenteUI)
    {
        if (director != null || objetivo == null) return;
        if (proyectil == null || retratoDorado == null || retratoAbatido == null)
        {
            Debug.LogError("La segunda fase de René necesita proyectil y ambos retratos.", this);
            return;
        }

        director = directorOleadas;
        jugador = objetivo;
        fuente = fuenteUI;
        Vida = GetComponent<Health>();
        controlador = GetComponent<BossController>();
        presentacion = GetComponent<PresentacionRenato>();
        hitbox = GetComponent<Collider2D>();
        disparo = jugador.GetComponent<PlayerShooting>();
        parry = jugador.GetComponent<PlayerParry>();
        movimiento = jugador.GetComponent<PlayerMovement>();
        cuerpoJugador = jugador.GetComponent<Rigidbody2D>();
        Vida.AplazarMuerte = AplazarDerrota;
        jugador.OnMuerte += Interrumpir;
    }

    private void OnDisable()
    {
        if (Vida != null && Vida.AplazarMuerte == AplazarDerrota) Vida.AplazarMuerte = null;
        if (jugador != null) jugador.OnMuerte -= Interrumpir;
        StopAllCoroutines();
        dialogo?.Destruir();
    }

    private bool AplazarDerrota()
    {
        if (etapa == Etapa.Primera)
        {
            etapa = Etapa.Transformacion;
            controlador.DetenerCombate();
            director.LimpiarProyectiles();
            hitbox.enabled = false;
            BloquearJugador();
            StartCoroutine(Transformar());
        }
        else if (etapa == Etapa.Segunda)
        {
            etapa = Etapa.Despedida;
            StopAllCoroutines();
            director.CerrarCombateRenato();
            hitbox.enabled = false;
            BloquearJugador();
            StartCoroutine(Despedirse());
        }
        return true;
    }

    private IEnumerator Transformar()
    {
        dialogo ??= new DialogoRenato(this, fuente);
        dialogo.Abrir(retratoDorado, "¡No me detendrás…! ¡Todavía me queda una última fase!", "");
        presentacion.Transformarse(duracionTransformacion);
        yield return new WaitForSeconds(duracionTransformacion);

        dialogo.Cerrar();
        etapa = Etapa.Segunda;
        Vida.IniciarEtapa(vidaTransformado);
        Vida.Invulnerable = true;
        hitbox.enabled = true;
        presentacion.ReposoDorado();
        DevolverControl();
        director.IniciarRefuerzosRenato(maximoRefuerzos, intervaloRefuerzos);
        yield return CombatirTransformado();
    }

    private IEnumerator CombatirTransformado()
    {
        yield return new WaitForSeconds(0.85f);
        while (etapa == Etapa.Segunda && jugador != null && jugador.VidaActual > 0)
        {
            presentacion.PrepararDisparoDorado(jugador.transform.position.x);
            yield return new WaitForSeconds(PresentacionRenato.InstanteDisparoDorado);
            // El Animator aplica primero la pose de lanzamiento y su punto de salida.
            yield return null;
            Vector2 origen = presentacion.OrigenDisparo;
            Vector2 direccion = (Vector2)jugador.transform.position - origen;
            if (direccion.sqrMagnitude < 0.001f) direccion = Vector2.left * presentacion.Orientacion;
            Bullet bala = Instantiate(proyectil, origen, Quaternion.identity);
            bala.DispararEnergia(this, direccion, rapidezProyectil, danioDevuelto, danioEnemigos, radioExplosion);
            yield return new WaitForSeconds(0.8f - PresentacionRenato.InstanteDisparoDorado);
            presentacion.ReposoDorado();
            yield return new WaitForSeconds(Mathf.Max(0.05f, intervaloDisparo - 0.8f));
        }
    }

    private IEnumerator Despedirse()
    {
        presentacion.MostrarAbatido();
        dialogo ??= new DialogoRenato(this, fuente);
        dialogo.Abrir(retratoAbatido, Despedida[0], "Clic para continuar  ·  1/3");
        int pagina = 0;
        while (pagina < Despedida.Length)
        {
            yield return null;
            if (!dialogo.ClicDentro()) continue;
            pagina++;
            if (pagina < Despedida.Length)
                dialogo.Mostrar(Despedida[pagina], pagina == Despedida.Length - 1
                    ? "Clic para cerrar  ·  3/3" : $"Clic para continuar  ·  {pagina + 1}/3");
        }
        dialogo.Cerrar();
        etapa = Etapa.Victoria;
        Vida.ConfirmarMuerte();
    }

    internal void RecibirEnergiaDevuelta(int cantidad)
    {
        if (!CombateTransformado) return;
        Vida.Invulnerable = false;
        try { Vida.RecibirDanio(cantidad); }
        finally { Vida.Invulnerable = true; }
    }

    internal bool FueraDeArena(Vector2 punto) => director.FueraDeArena(punto);

    private void Interrumpir()
    {
        if (!FueraDePrimeraFase) return;
        etapa = Etapa.Interrumpida;
        StopAllCoroutines();
        director.CerrarCombateRenato();
        dialogo?.Cerrar();
        presentacion.ApagarAuraDorada();
    }

    private void BloquearJugador()
    {
        if (bloqueado) return;
        bloqueado = true;
        pausa = FindFirstObjectByType<PauseManager>();
        if (pausa != null) { pausaPrevia = pausa.enabled; pausa.enabled = false; }
        if (movimiento != null) { movimientoPrevio = movimiento.enabled; movimiento.enabled = false; }
        if (parry != null) { parryPrevio = parry.enabled; parry.enabled = false; }
        if (disparo != null) { disparoPrevio = disparo.BloqueadoPorDialogo; disparo.BloqueadoPorDialogo = true; }
        if (cuerpoJugador != null) { simulacionPrevia = cuerpoJugador.simulated; cuerpoJugador.simulated = false; }
    }

    private void DevolverControl()
    {
        bloqueado = false;
        if (pausa != null) pausa.enabled = pausaPrevia;
        if (movimiento != null) movimiento.enabled = movimientoPrevio;
        if (parry != null) parry.enabled = parryPrevio;
        if (cuerpoJugador != null) cuerpoJugador.simulated = simulacionPrevia;
        rearmarDisparo = disparo != null;
    }

    private void Update()
    {
        // Una pulsación de disparo mantenida durante el diálogo debe soltarse para rearmar.
        if (!rearmarDisparo || PlayerShooting.EntradaDisparoActiva) return;
        rearmarDisparo = false;
        if (disparo != null && !bloqueado) disparo.BloqueadoPorDialogo = disparoPrevio;
    }
}
