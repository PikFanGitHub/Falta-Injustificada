using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class Cube : PersonajeBase
{
    //acceso tipado a los datos especificos de Cube
    private FighterData DatosCube => datosPersonaje as FighterData;

    // la ultimate de Mamalon se activa con QCF+QCF+Patada Floja (boton 2)
    protected override int BotonActivadorUltimate => 2;

    //pala
    [Header("Pala — objeto en el rig y prefab caido en suelo")]
    [SerializeField] private GameObject pala;
    [SerializeField] private GameObject palaEnSuelo;

    private bool tienePala = true;
    private bool palaGolpeoEnAtaque = false;
    private bool palaGolpeoSuelo = false;
    private bool atacandoConPatadaFuerteSuelo = false;

    //panes
    [Header("Panes (QCF + boton) — 2 GameObjects con componente Pan")]
    [SerializeField] private GameObject[] panes;

    private int _usosComandoPan = 0;
    private float _tiempoCooldownPan = -999f;
    private const float cooldownPan = 5f;
    private const int usosPorCooldown = 2;

    // ultimate - Horno
    [Header("Ultimate — Horno")]
    [Tooltip("Referencia directa al HornoUltimate que vive en el rig del personaje.")]
    [SerializeField] private HornoUltimate hornoUltimate;

    //ciclo de vida de unity

    protected override void Awake()
    {
        base.Awake();

        //auto-asignacion de palaEnSuelo si no esta en Inspector
        if (palaEnSuelo == null)
        {
            PalaPickup pickup = GetComponentInChildren<PalaPickup>(true);
            if (pickup != null)
                palaEnSuelo = pickup.gameObject;
            else
                Debug.LogWarning("[Cube] No se encontró PalaPickup en los hijos. Asignalo manualmente.");
        }

        //auto-asignacion de panes si el array esta vacio
        if (panes == null || panes.Length == 0)
        {
            Pan[] encontrados = GetComponentsInChildren<Pan>(true);
            if (encontrados.Length > 0)
            {
                panes = new GameObject[encontrados.Length];
                for (int i = 0; i < encontrados.Length; i++)
                    panes[i] = encontrados[i].gameObject;
            }
            else
                Debug.LogWarning("[Cube] No se encontraron componentes Pan en los hijos. Asignalos manualmente.");
        }
    }

    protected override void Start()
    {
        base.Start();
    }

    //  gates - que botones/comandos requieren pala

    protected override bool PuedeEjecutarCommand1() => tienePala;
    protected override bool PuedeEjecutarCommand3() => tienePala;
    protected override bool PuedeEjecutarComandoQCF(int b) => tienePala;

    protected override bool PuedeEjecutarAtaqueNormal(int boton)
    {
        // puno Fuerte (1) y Patada Fuerte (3) requieren pala
        if (boton == 1 || boton == 3)
            return tienePala;
        return true;
    }

    //  ataque normal - override para Patada Fuerte (gestion de pala)
    protected override void EjecutarAtaqueNormal(int boton)
    {
        if (!PuedeEjecutarAtaqueNormal(boton)) return;

        if (boton == 3)
        {
            atacandoConPatadaFuerteSuelo = (contextoActual == ContextoPersonaje.Suelo);
            palaGolpeoEnAtaque = false;
            palaGolpeoSuelo = false;

            // fix: marcar el hitbox de suelo como principal para que Hitbox.OnTriggerEnter
            // pueda llamar a NotificarPalaSuelo con la posicion de contacto correcta.
            if (atacandoConPatadaFuerteSuelo && hitboxesPatadaFuerte != null)
            {
                foreach (var hb in hitboxesPatadaFuerte)
                {
                    if (hb == null) continue;
                    Hitbox h = hb.GetComponent<Hitbox>();
                    if (h != null && h.soloSuelo)
                        h.esPalaAtaquePrincipal = true;
                }
            }
        }

        base.EjecutarAtaqueNormal(boton);
    }

    //  hooks DE ciclo DE vida

    //llamado al final de AnimEvent_FinAtaque
    protected override void OnFinAtaqueCallback()
    {
        if (atacandoConPatadaFuerteSuelo && !palaGolpeoEnAtaque && !palaGolpeoSuelo)
            SoltarPala(ObtenerPosicionFallbackPala());
        atacandoConPatadaFuerteSuelo = false;
        palaGolpeoSuelo = false;
    }

    //llamado en la corrutina fallback VolverEstadoTrasAtaque
    protected override void OnVolverTrasAtaqueCallback()
    {
        if (atacandoConPatadaFuerteSuelo && !palaGolpeoEnAtaque && !palaGolpeoSuelo)
            SoltarPala(ObtenerPosicionFallbackPala());
        atacandoConPatadaFuerteSuelo = false;
        palaGolpeoSuelo = false;
    }

    //llamado al inicio de RecibirGolpe (antes de cancelar corutinas)
    protected override void OnRecibirGolpeCallback()
    {
        if (atacandoConPatadaFuerteSuelo && !palaGolpeoEnAtaque)
            SoltarPala();
        atacandoConPatadaFuerteSuelo = false;
        palaGolpeoEnAtaque = false;
        palaGolpeoSuelo = false;
        animator.speed = 1f;
    }

    //llamado al final de ReiniciarParaNuevaRonda
    protected override void OnReiniciarCallback()
    {
        atacandoConPatadaFuerteSuelo = false;
        palaGolpeoEnAtaque = false;
        palaGolpeoSuelo = false;
        animator.speed = 1f;
        hornoUltimate?.Destruir();
        RecogerPala();
    }

    //  notificaciones externas (Hitbox.cs / PalaPickup.cs)

    public override void NotificarPalaImpacto()
    {
        palaGolpeoEnAtaque = true;
    }
    public override void NotificarGolpeConectado(float danoConectado = 0f)
    {
        base.NotificarGolpeConectado(danoConectado);
        // si estabamos atacando con la pala, registrar el impacto en el enemigo
        if (atacandoConPatadaFuerteSuelo)
            palaGolpeoEnAtaque = true;
    }

    public override void NotificarPalaSuelo(Vector3 posContacto)
    {
        if (palaGolpeoSuelo) return;
        palaGolpeoSuelo = true;
        SetHitboxes(hitboxesPatadaFuerte, false);
        SoltarPala(posContacto);  // usar tal cual, ya es el punto correcto
    }

    public override void RecogerPala()
    {
        if (tienePala) return;
        tienePala = true;
        if (pala != null) pala.SetActive(true);
        if (palaEnSuelo != null) palaEnSuelo.SetActive(false);
    }

    // fix: calcula la posicion de caida de la pala usando el hitbox soloSuelo proyectado
    // al suelo mediante raycast. Si no hay hitbox o no hay suelo bajo el, usa la posicion
    // del jugador desplazada hacia el frente como antes, pero ahora proyectada al suelo.
    private Vector3 ObtenerPosicionFallbackPala()
    {
        // intentar usar el hitbox soloSuelo como origen del raycast
        if (hitboxesPatadaFuerte != null)
        {
            foreach (var hb in hitboxesPatadaFuerte)
            {
                if (hb == null) continue;
                Hitbox h = hb.GetComponent<Hitbox>();
                if (h == null || !h.soloSuelo) continue;

                Vector3 origen = hb.transform.position + Vector3.up * 0.5f;
                if (Physics.Raycast(origen, Vector3.down, out RaycastHit hit, 5f,
                                    ~0, QueryTriggerInteraction.Ignore))
                    return hit.point;

                // sin suelo bajo el hitbox: proyectar verticalmente desde su posicion XZ
                Vector3 pos = hb.transform.position;
                pos.y = transform.position.y;
                return pos;
            }
        }

        // fallback final: frente al jugador proyectado al suelo
        Vector3 basePos = transform.position + new Vector3(0f, 0.5f, _signoOrientacion * 1.5f);
        if (Physics.Raycast(basePos, Vector3.down, out RaycastHit groundHit, 5f,
                            ~0, QueryTriggerInteraction.Ignore))
            return groundHit.point;

        return transform.position + new Vector3(0f, 0f, _signoOrientacion * 1.5f);
    }

    //soltar pala
    private void SoltarPala(Vector3? posicion = null)
    {
        if (!tienePala) return;
        tienePala = false;
        if (pala != null) pala.SetActive(false);
        if (palaEnSuelo != null)
        {
            PalaPickup pickup = palaEnSuelo.GetComponent<PalaPickup>();
            if (pickup != null) pickup.dueno = this;
            palaEnSuelo.transform.SetParent(null, true);
            Vector3 pos = posicion ?? (transform.position
                + new Vector3(0f, 0f, _signoOrientacion * 1.5f));
            palaEnSuelo.transform.position = pos;
            palaEnSuelo.SetActive(true);
        }
    }

    //  command 1 - QCB + Puno Flojo  (ataque unico con pala)

    protected override IEnumerator EjecutarCommand1()
    {
        IniciarComando();
        _command1Cancelado = false;
        ataqueEsCancelable = true;

        EstablecerAtaqueActivo(AtaqueID.Comando1);
        bool cancelableOriginal = ataqueEsCancelable;
        if (!cancelableOriginal) ataqueEsCancelable = true;

        foreach (var hb in hitboxesCommand1)
        {
            if (hb == null) continue;
            Hitbox h = hb.GetComponent<Hitbox>();
            if (h != null) h.esCancelable = true;
        }

        foreach (var hb in hitboxesCommand1)
        {
            if (hb == null) continue;
            Hitbox h = hb.GetComponent<Hitbox>();
            if (h != null)
            {
                h.SetDano(datosPersonaje.danoComando1);
                h.tipoGolpe = datosPersonaje.tipoGolpeComando1;
                h.direccion = DireccionGolpe.Frente;
                h.esCancelable = true;
                h.hitstunDuracion = datosPersonaje.hitstunComando1;
                h.SetImpulso(datosPersonaje.impulsoYComando1, datosPersonaje.impulsoZComando1);
            }
            AjustarHitbox(hb, datosPersonaje.scaleCommand1, datosPersonaje.offsetCommand1);
        }

        PlayAnim(animCommand1);
        hitboxesActuales = hitboxesCommand1;
        yield return new WaitForSeconds(datosPersonaje.duracionComando1);
        SetHitboxes(hitboxesCommand1, false);
        if (_command1Cancelado) yield break;
        ataqueEsCancelable = false;
        FinalizarComando();
    }

    //  command 2 - QCB + Puno Fuerte  (emerge del suelo)

    protected override IEnumerator EjecutarCommand2()
    {
        IniciarComando();
        command2GolpeoOponente = false;
        command2CancelUsado = false;
        command2BotonCancel = -1;

        EstablecerAtaqueActivo(AtaqueID.Comando2);

        Vector3 posicionOriginal = transform.position;

        //fase 1: meterse en el suelo
        PlayAnim(animCommand2_1);
        yield return new WaitForSeconds(DatosCube?.duracionComando2_1 ?? 0.4f);

        //teleport bajo el oponente en Z
        if (oponente != null)
            transform.position = new Vector3(transform.position.x, transform.position.y, oponente.position.z);

        foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            smr.updateWhenOffscreen = true;

        yield return null;

        //fase 2: animacion de llegada + hitbox activa
        PlayAnim(animCommand2_2);
        hitboxesActuales = hitboxesCommand2;
        SetHitboxFlag(hitboxesCommand2, true, false);

        yield return StartCoroutine(ActivarHitboxesConDano(
            hitboxesCommand2, datosPersonaje.danoComando2, DatosCube?.duracionComando2_2 ?? 0.4f,
            datosPersonaje.scaleCommand2, datosPersonaje.offsetCommand2));

        SetHitboxFlag(hitboxesCommand2, false, false);

        //si golpeo: ventana de cancel con ataque normal
        if (command2GolpeoOponente)
        {
            // congelar el Animator para que no auto-transicione de 2-2 a 2-3
            // mientras esperamos el input del jugador
            animator.speed = 0f;
            enVentanaCancelCommand2 = true;
            float t = 0f;
            while (t < ventanaCancelCommand2 && !command2CancelUsado)
            {
                t += Time.deltaTime;
                yield return null;
            }
            enVentanaCancelCommand2 = false;
            animator.speed = 1f;

            if (command2CancelUsado)
            {
                hitboxesActuales = null;
                bufferAtaque = -1;
                FinalizarComando();
                EjecutarAtaqueNormal(command2BotonCancel);
                yield break;
            }
        }

        //sin cancel: volver al origen con animacion de vuelta
        transform.position = posicionOriginal;
        PlayAnim(animCommand2_3);
        yield return new WaitForSeconds(DatosCube?.duracionComando2_3 ?? 0.3f);
        FinalizarComando();
    }

    //command 3 - QCB + Patada Fuerte  (si no golpea = Dizzy)

    protected override IEnumerator EjecutarCommand3()
    {
        bool eraEnAire = !enSuelo;
        corriendo = false;
        hitboxesActuales = null;
        CambiarEstado(EstadoJugador.Comando);
        contextoActual = eraEnAire ? ContextoPersonaje.Aire : ContextoPersonaje.Suelo;

        EstablecerAtaqueActivo(AtaqueID.Comando3);

        command3GolpeoOponente = false;
        PlayAnim(animCommand3);
        hitboxesActuales = hitboxesCommand3;
        SetHitboxFlag(hitboxesCommand3, false, true);

        foreach (var hb in hitboxesCommand3)
        {
            if (hb == null) continue;
            Hitbox h = hb.GetComponent<Hitbox>();
            if (h != null) h.SetDano(datosPersonaje.danoComando3);
            AjustarHitbox(hb, datosPersonaje.scaleCommand3, datosPersonaje.offsetCommand3);
        }

        float duracionTotal = datosPersonaje.duracionComando3;
        float hitboxDuracion = duracionTotal * 0.5f;
        float t = 0f;
        bool hitboxActiva = false;

        while (t < duracionTotal)
        {
            if (!hitboxActiva && t >= (duracionTotal - hitboxDuracion) * 0.3f)
            {
                SetHitboxes(hitboxesCommand3, true);
                hitboxActiva = true;
            }
            if (hitboxActiva && t >= (duracionTotal - hitboxDuracion) * 0.3f + hitboxDuracion)
            {
                SetHitboxes(hitboxesCommand3, false);
                hitboxActiva = false;
            }

            if (movimientoHorizontal != 0f)
            {
                float vel = movimientoHorizontal * datosPersonaje.velocidadMovimiento;
                float vy = enSuelo ? Mathf.Min(rb.linearVelocity.y, 0f) : rb.linearVelocity.y;
                rb.linearVelocity = new Vector3(0f, vy, vel);
            }

            t += Time.deltaTime;
            yield return null;
        }

        SetHitboxes(hitboxesCommand3, false);
        SetHitboxFlag(hitboxesCommand3, false, false);

        if (command3GolpeoOponente)
        {
            FinalizarComando();
        }
        else
        {
            //no golpeo: Dizzy
            CambiarEstado(EstadoJugador.Dizzy);
            contextoActual = ContextoPersonaje.Suelo;
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            PlayAnim(animDizzy);
            yield return new WaitForSeconds(duracionDizzy);
            CambiarEstado(EstadoJugador.Idle);
            PlayAnim(animIdle);
        }
    }

    //comando QCF - Pan  (QCF + Puno Fuerte o Patada Floja)

    protected override IEnumerator EjecutarComandoQCF(int boton)
    {
        if (boton != 1 && boton != 2) yield break;

        EstablecerAtaqueActivo(AtaqueID.Comando4);

        //cooldown cada dos usos
        if (_usosComandoPan >= usosPorCooldown)
        {
            float restante = cooldownPan - (Time.time - _tiempoCooldownPan);
            if (restante > 0f) yield break;
            _usosComandoPan = 0;
        }

        IniciarComando();
        PlayAnim(animCommand4);
        yield return new WaitForSeconds(0.2f);

        if (datosPersonaje == null || panes == null || panes.Length == 0)
        {
            Debug.LogWarning("[Cube] No hay panes asignados.");
            FinalizarComando();
            yield break;
        }

        TipoPan tipoBuscado = (boton == 1) ? TipoPan.Espalda : TipoPan.Suelo;
        Pan pan = EncontrarPan(tipoBuscado);

        if (pan == null)
        {
            Debug.LogWarning($"[Cube] No se encontró Pan con tipoPredeterminado={tipoBuscado}.");
            FinalizarComando();
            yield break;
        }

        float dist = DatosCube?.distanciaEreccionPan ?? 3f;
        Vector3 posOp = oponente != null
            ? oponente.position
            : transform.position + new Vector3(0f, 0f, 2f);

        float dirZ = (oponente != null)
            ? Mathf.Sign(oponente.position.z - transform.position.z)
            : (modeloHijo != null && modeloHijo.localScale.z > 0f ? 1f : -1f);

        TipoPan tipo;
        Vector3 posFinal;
        Vector3 desplazamientoOrigen;

        if (tipoBuscado == TipoPan.Espalda)
        {
            tipo = TipoPan.Espalda;
            posFinal = new Vector3(posOp.x, posOp.y + (DatosCube?.alturaObjetivoPanEspalda ?? 1f), posOp.z);
            desplazamientoOrigen = new Vector3(0f, 0f, dirZ * dist);
        }
        else
        {
            tipo = TipoPan.Suelo;
            Vector3 groundPos = posOp;
            if (Physics.Raycast(posOp + Vector3.up * 2f, Vector3.down, out RaycastHit hitSuelo, 10f,
                                ~0, QueryTriggerInteraction.Ignore))
                groundPos.y = hitSuelo.point.y;

            posFinal = new Vector3(posOp.x, groundPos.y, posOp.z);
            desplazamientoOrigen = new Vector3(0f, -dist, 0f);
        }

        float iyPan = (tipoBuscado == TipoPan.Espalda) ? (DatosCube?.impulsoYPanEspalda ?? 5f) : (DatosCube?.impulsoYPanSuelo ?? 10f);
        float izPan = (tipoBuscado == TipoPan.Espalda) ? (DatosCube?.impulsoZPanEspalda ?? 8f) : (DatosCube?.impulsoZPanSuelo ?? 2f);
        pan.Erigir(this, DatosCube?.danoPan ?? 14f, "Jugador", tipo, posFinal, desplazamientoOrigen, iyPan, izPan);

        yield return new WaitForSeconds(0.3f);

        _usosComandoPan++;
        if (_usosComandoPan >= usosPorCooldown)
            _tiempoCooldownPan = Time.time;

        FinalizarComando();
    }

    // ultimate - Horno

    protected override IEnumerator EjecutarUltimate()
    {
        if (!GastarStocksUltimate()) yield break;

        IniciarComando();

        string nombreAnim = DatosCube?.animUltimate ?? "Ultimate";
        float durAnim = DatosCube != null
            ? ObtenerDuracionClip(DatosCube.animUltimate, DatosCube.duracionAnimUltimate)
            : 2f;

        PlayAnim(nombreAnim);

        // si hay un Animation Event AnimEvent_SpawnHorno en el clip, el horno se
        // spawnea desde ese evento en el fotograma exacto configurado. Si no existe,
        // este fallback lo lanza al inicio de la animacion para que no se pierda.
        if (hornoUltimate != null && !hornoUltimate.gameObject.activeSelf && DatosCube != null)
            SpawnHorno();

        yield return new WaitForSeconds(durAnim);
        FinalizarComando();
    }

    // llamado desde el Animation Event "AnimEvent_SpawnHorno" del clip de ultimate,
    // o desde el fallback de la corrutina si el evento no esta configurado.
    public override void AnimEvent_SpawnHorno()
    {
        if (hornoUltimate == null || hornoUltimate.gameObject.activeSelf || DatosCube == null) return;
        SpawnHorno();
    }

    private void SpawnHorno()
    {
        hornoUltimate.Inicializar(
            this,
            oponente,
            DatosCube.vidaHornoUltimate,
            DatosCube.intervaloPanUltimate,
            DatosCube.danoPanUltimate);
    }

    //busca el Pan correcto en el array
    private Pan EncontrarPan(TipoPan tipo)
    {
        if (panes == null) return null;
        foreach (var go in panes)
        {
            if (go == null) continue;
            Pan p = go.GetComponent<Pan>();
            if (p != null && p.tipoPredeterminado == tipo) return p;
        }
        return null;
    }
}