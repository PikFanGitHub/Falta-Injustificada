using UnityEngine;
using System.Collections;

public class Montero : PersonajeBase
{
    //hitboxes propias
    [Header("Hitboxes — Pelota (QCF aereo)")]
    [SerializeField] private GameObject[] hitboxesPelota;

    [Header("Hitboxes — Dash (QCB + Puno Fuerte)")]
    [SerializeField] private GameObject[] hitboxesDash;

    // acceso rapido al dato especifico de Montero (puede ser null si se asigna un asset incorrecto)
    private MonteroData DatosMontero => datosPersonaje as MonteroData;

    //  start - sobreescribir nombres de animacion con los del animator de Montero

    protected override void Start()
    {
        animHitstun = "Hit1";
        animHurt2 = "Hit2";
        animDead = "Death";
        animWin = "Win";
        animBlock = "BlockS";
        animBlockCrouch = "BlockCrouch";

        //montero no tiene Run ni Frenar
        animCorrer = "Walk";
        animFrenar = "Idle";

        //montero tiene animaciones de lanzado propias
        animLanzadoArriba = "Hit1";
        animLanzadoFrente = "Hit1";
        animLanzadoAbajo = "Hit1";
        animAterrizajeDuro = "StandUp";

        //montero no tiene Dizzy
        animDizzy = "Idle";

        base.Start();
    }

    //estado del rezo/buff
    private int _contadorRezos = 0;
    private bool _buffActivo = false;
    private Coroutine _corrutinaBuff = null;

    //  gate - la pelota solo se puede ejecutar en el aire

    protected override bool PuedeEjecutarComandoQCF(int boton)
    {
        return !enSuelo;
    }

    //  command 1 - QCB + Puno Flojo  (sistema de rezo + buff)

    protected override IEnumerator EjecutarCommand1()
    {
        MonteroData d = DatosMontero;
        int rezosNecesarios = d != null ? d.rezosNecesarios : 3;
        float duracionRezo = datosPersonaje != null ? datosPersonaje.duracionComando1 : 0.8f;
        string animRezo = d != null ? d.animRezo : "Command1";
        string animRezoFin = d != null ? d.animRezoTerminado : "Command1";

        IniciarComando();

        _contadorRezos++;

        if (_contadorRezos < rezosNecesarios)
        {
            //rezo parcial: animar y volver
            PlayAnim(animRezo);
            yield return new WaitForSeconds(duracionRezo);
            FinalizarComando();
        }
        else
        {
            //rezo completado: activar buff
            _contadorRezos = 0;

            PlayAnim(animRezoFin);
            yield return new WaitForSeconds(duracionRezo);
            FinalizarComando();

            //cancelar buff anterior si lo hubiera
            if (_corrutinaBuff != null)
                StopCoroutine(_corrutinaBuff);

            _corrutinaBuff = StartCoroutine(AplicarBuff());
        }
    }

    private IEnumerator AplicarBuff()
    {
        MonteroData d = DatosMontero;
        float duracion = d != null ? d.duracionBuff : 10f;
        float multVel = d != null ? d.multiplicadorVelocidad : 1.5f;
        float multSalto = d != null ? d.multiplicadorSalto : 1.3f;
        float multAnimacion = d != null ? d.multiplicadorVelocidadAnimacion : 1.5f;

        _buffActivo = true;
        buffMultVelocidad = multVel;
        buffMultSalto = multSalto;
        if (animator != null) animator.speed = multAnimacion;

        Debug.Log($"[Montero] Buff activado ({duracion}s) — vel x{multVel}, salto x{multSalto}, anim x{multAnimacion}");

        yield return new WaitForSeconds(duracion);

        QuitarBuff();
    }

    private void QuitarBuff()
    {
        _buffActivo = false;
        buffMultVelocidad = 1f;
        buffMultSalto = 1f;
        if (animator != null) animator.speed = 1f;
        _corrutinaBuff = null;
        Debug.Log("[Montero] Buff terminado.");
    }

    //al reiniciar la ronda se limpia el buff y se reactiva el animator
    protected override void OnReiniciarCallback()
    {
        _contadorRezos = 0;

        // restaurar animDead al valor correcto por si fue redirigido a animFail
        // durante una muerte por fallo de ultimate.
        animDead = "Death";

        QuitarBuff();
        if (_corrutinaBuff != null)
        {
            StopCoroutine(_corrutinaBuff);
            _corrutinaBuff = null;
        }
        if (animator != null) animator.enabled = true;
    }

    //  comando QCF - Pelota aerea, diagonal abajo-adelante

    protected override IEnumerator EjecutarComandoQCF(int boton)
    {
        MonteroData d = DatosMontero;
        float velZ = d != null ? d.velocidadPelota : 12f;
        float impulsoY = d != null ? d.impulsoYPelota : 8f;
        float dano = d != null ? d.danoPelota : 15f;
        float durPel = d != null ? d.duracionPelota : 1.4f;
        float hitstun = d != null ? d.hitstunPelota : 0.5f;
        float durStart = d != null ? d.duracionAnimPelotaStart : 0.5f;

        IniciarComando();
        command2GolpeoOponente = false;
        PrepararHitboxesPelota(dano, hitstun);

        //fase 1: animacion de transformacion
        PlayAnim("Command2-Start");
        yield return new WaitForSeconds(durStart);

        //fase 2: loop + movimiento diagonal abajo-adelante
        PlayAnim("Command2Loop");

        float dirZ = DireccionLanzado() < 0 ? 1f : -1f;

        //impulso diagonal: Y negativo (hacia abajo) + Z hacia el oponente
        rb.linearVelocity = new Vector3(0f, -Mathf.Abs(impulsoY), dirZ * velZ);

        SetHitboxes(hitboxesPelota, true);
        hitboxesActuales = hitboxesPelota;

        float t = 0f;
        while (t < durPel)
        {
            //mantener velocidad Z constante; Y la controla la fisica
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, dirZ * velZ);
            t += Time.deltaTime;
            if (enSuelo) break;
            //al hacer hit se para el movimiento horizontal y se cae libremente por gravedad
            if (command2GolpeoOponente)
            {
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
                break;
            }
            yield return null;
        }

        SetHitboxes(hitboxesPelota, false);
        //si no hizo hit ni llego al suelo, parar todo; si hizo hit dejar caer por gravedad
        if (!enSuelo && !command2GolpeoOponente)
            rb.linearVelocity = Vector3.zero;
        FinalizarComando();
    }

    private void PrepararHitboxesPelota(float dano, float hitstun)
    {
        if (hitboxesPelota == null) return;
        foreach (var hb in hitboxesPelota)
        {
            if (hb == null) continue;
            Hitbox h = hb.GetComponent<Hitbox>();
            if (h != null)
            {
                h.SetDano(dano);
                h.tipoGolpe = TipoGolpe.Derribar;
                h.direccion = DireccionGolpe.Abajo;
                h.hitstunDuracion = hitstun;
                h.SetImpulso(0f, 0f);
                //necesario para que Hitbox.cs llame a NotificarCommand2Impacto al hacer hit
                h.esCommand2 = true;
            }
        }
    }

    //  command 2 - QCB + Puno Fuerte  (Dash + golpe)
    //  animacion Command3 del animator de Montero

    protected override IEnumerator EjecutarCommand2()
    {
        MonteroData d = DatosMontero;
        float dano = datosPersonaje != null ? datosPersonaje.danoComando2 : 20f;
        float hitstun = datosPersonaje != null ? datosPersonaje.hitstunComando2 : 0.45f;
        float distancia = d != null ? d.distanciaDash : 3.6f;
        float velocidad = d != null ? d.velocidadDash : 18f;
        float durGolpe = d != null ? d.duracionGolpeDash : 0.35f;
        //la duracion del dash se calcula a partir de la distancia deseada
        float durDash = velocidad > 0f ? distancia / velocidad : 0f;
        float dirZ = DireccionLanzado() < 0 ? 1f : -1f;

        IniciarComando();

        //fase 1: dash
        PlayAnim("Command3");
        float t = 0f;
        while (t < durDash)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, dirZ * velocidad);
            t += Time.deltaTime;
            yield return null;
        }
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

        //fase 2: golpe
        PrepararHitboxesDash(dano, hitstun);
        hitboxesActuales = hitboxesDash;
        SetHitboxes(hitboxesDash, true);
        yield return new WaitForSeconds(durGolpe);
        SetHitboxes(hitboxesDash, false);

        FinalizarComando();
    }

    private void PrepararHitboxesDash(float dano, float hitstun)
    {
        if (hitboxesDash == null) return;
        foreach (var hb in hitboxesDash)
        {
            if (hb == null) continue;
            Hitbox h = hb.GetComponent<Hitbox>();
            if (h != null)
            {
                h.SetDano(dano);
                h.tipoGolpe = TipoGolpe.Normal;
                h.direccion = DireccionGolpe.Frente;
                h.hitstunDuracion = hitstun;
                h.SetImpulso(0f, 0f);
            }
            AjustarHitbox(hb);
        }
    }

    //  command 3 - QCB + Patada Fuerte  (Golpe hacia adelante)
    //  animacion Command4 del animator de Montero

    protected override IEnumerator EjecutarCommand3()
    {
        MonteroData d = DatosMontero;
        float dano = d != null ? d.danoGolpeAdelante : 48f;
        float hitstun = d != null ? d.hitstunGolpeAdelante : 0.4f;
        float distancia = d != null ? d.distanciaGolpeAdelante : 2.5f;
        float velocidad = d != null ? d.velocidadGolpeAdelante : 15f;
        // calcular cuanto tiempo dura el dash a partir de distancia y velocidad
        float durDash = velocidad > 0f ? distancia / velocidad : 0f;
        float dirZ = DireccionLanzado() < 0 ? 1f : -1f;

        IniciarComando();
        PrepararHitboxesGolpeAdelante(dano, hitstun);
        hitboxesActuales = hitboxesCommand4;

        PlayAnim("Command4");

        // fase de avance: recorrer la distancia configurada
        float t = 0f;
        while (t < durDash)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, dirZ * velocidad);
            t += Time.deltaTime;
            yield return null;
        }
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

        // esperar a que la animacion termine por completo antes de devolver el control
        // (ObtenerDuracionClip usa el tiempo real del clip; si durDash ya consumio
        //  mas tiempo del clip, el clamp a 0 evita un wait negativo)
        float durClip = ObtenerDuracionClip("Command4", 0.6f);
        float tiempoRestante = Mathf.Max(0f, durClip - durDash);
        if (tiempoRestante > 0f)
            yield return new WaitForSeconds(tiempoRestante);

        FinalizarComando();
    }

    private void PrepararHitboxesGolpeAdelante(float dano, float hitstun)
    {
        MonteroData d = DatosMontero;
        TipoGolpe tipo = d != null ? d.tipoGolpeAdelante : TipoGolpe.Normal;
        DireccionGolpe dir = d != null ? d.direccionGolpeAdelante : DireccionGolpe.Frente;
        float iy = d != null ? d.impulsoYGolpeAdelante : 0f;
        float iz = d != null ? d.impulsoZGolpeAdelante : 0f;

        if (hitboxesCommand4 == null) return;
        foreach (var hb in hitboxesCommand4)
        {
            if (hb == null) continue;
            Hitbox h = hb.GetComponent<Hitbox>();
            if (h != null)
            {
                h.SetDano(dano);
                h.tipoGolpe = tipo;
                h.direccion = dir;
                h.hitstunDuracion = hitstun;
                h.SetImpulso(iy, iz);
            }
            AjustarHitbox(hb);
        }
    }

    //  ultimate - Dash instakill
    //  secuencia:
    //    1. Gastar stocks (siempre, aunque el golpe no conecte).
    //    2. Fase Start : animacion de preparacion (duracion configurable).
    //    3. Fase Dash  : Montero se desplaza a alta velocidad hacia el oponente.
    //                    la hitbox se abre/cierra con AnimEvent_AbrirHitbox /
    //                    AnimEvent_CerrarHitbox del clip. Si el clip no tiene
    //                    esos events, se abre como fallback al inicio del dash
    //                    y se cierra al terminar el bucle.
    //    hit    -> el oponente recibe dano letal; Montero vuelve a idle.
    //    fallo  -> Montero reproduce animUltimateFail, el Animator se deshabilita
    //            para congelar la pose, y luego muere. Cualquier PlayAnim posterior
    //            (MorirDefinitivamente, etc.) no tiene efecto hasta la siguiente ronda.
    //  nota: _ultimateGolpeo se resetea en TriggerUltimate() (PersonajeBase)
    //  antes de llamar a esta corrutina.

    protected override IEnumerator EjecutarUltimate()
    {
        MonteroData d = DatosMontero;

        string animStart = d != null ? d.animUltimateStart : "UltimateStart";
        float durStart = d != null ? d.duracionUltimateStart : 1.2f;
        string animDashAnim = d != null ? d.animUltimateDash : "UltimateDash";
        float velDash = d != null ? d.velocidadUltimateDash : 28f;
        float durMaxDash = d != null ? d.duracionMaxUltimateDash : 0.9f;
        float danoUlt = d != null ? d.danoUltimate : 99999f;
        float hitstunUlt = d != null ? d.hitstunUltimate : 0.5f;
        string animFail = d != null ? d.animUltimateFail : "UltimateFail";
        float durFail = d != null ? d.duracionUltimateFail : 1.2f;

        // 1. Gastar stocks siempre, incluso si el golpe no conecta
        if (!GastarStocksUltimate()) yield break;

        // 2. Fase Start
        IniciarComando();
        PlayAnim(animStart);
        yield return new WaitForSeconds(durStart);

        // 3. Fase Dash con hitbox via animation events
        PrepararHitboxesUltimate(danoUlt, hitstunUlt);

        // asignar hitboxesActuales antes de PlayAnim para que los
        // AnimEvent_AbrirHitbox / AnimEvent_CerrarHitbox del clip puedan actuar
        hitboxesActuales = hitboxesUltimate;
        _ultimateGolpeo = false;

        float dirZ = DireccionLanzado() < 0 ? 1f : -1f;
        PlayAnim(animDashAnim);

        // fallback: dar un frame al animation event; si no abrio la hitbox, abrirla aqui
        yield return null;
        bool yaAbierta = hitboxesUltimate != null
                         && hitboxesUltimate.Length > 0
                         && hitboxesUltimate[0] != null
                         && hitboxesUltimate[0].activeSelf;
        if (!yaAbierta)
            SetHitboxes(hitboxesUltimate, true);

        // activar deteccion continua para evitar tunneling a alta velocidad
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        float t = 0f;
        while (t < durMaxDash)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, dirZ * velDash);
            t += Time.deltaTime;

            if (_ultimateGolpeo) break;
            if (enSuelo && t > 0.05f) break;

            yield return null;
        }

        // restaurar deteccion discreta antes de parar
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        SetHitboxes(hitboxesUltimate, false);
        hitboxesActuales = null;

        // resultado
        if (_ultimateGolpeo)
        {
            Debug.Log("[Montero] Ultimate conectada.");
            FinalizarComando();
        }
        else
        {
            Debug.Log("[Montero] Ultimate fallada. Montero muere.");
            PlayAnim(animFail);
            yield return new WaitForSeconds(durFail);

            // redirigir animDead a la animacion de fallo antes de matar al personaje.
            // de este modo MorirDefinitivamente() llamara PlayAnim(animFail) en lugar
            // de PlayAnim("Death"), y la corrutina MantenerAnimacionMuerte() de
            // PersonajeBase vigilara que el estado se quede en SuperFail cada frame.
            // se restaura a "Death" en OnReiniciarCallback al inicio de la siguiente ronda.
            animDead = animFail;

            AplicarDano(99999f);
        }
    }

    // prepara las hitboxesUltimate con dano letal y flags correctos.
    // esCommand2 = true hace que Hitbox.cs llame a NotificarCommand2Impacto,
    // que a su vez llama al override de abajo para activar _ultimateGolpeo.
    private void PrepararHitboxesUltimate(float dano, float hitstun)
    {
        if (hitboxesUltimate == null) return;
        foreach (var hb in hitboxesUltimate)
        {
            if (hb == null) continue;
            Hitbox h = hb.GetComponent<Hitbox>();
            if (h != null)
            {
                h.SetDano(dano);
                h.tipoGolpe = TipoGolpe.Derribar;
                h.direccion = DireccionGolpe.Frente;
                h.hitstunDuracion = hitstun;
                h.SetImpulso(0f, 0f);
                // reusamos esCommand2 para que Hitbox llame a
                // NotificarCommand2Impacto -> nuestra override -> _ultimateGolpeo
                h.esCommand2 = true;
                h.esCommand3 = false;
            }
            AjustarHitbox(hb);
        }
    }

    // override: cuando la hitbox de ultimate conecta, Hitbox.cs llama a
    // NotificarCommand2Impacto en el dueno. Lo interceptamos aqui para
    // activar _ultimateGolpeo en lugar de command2GolpeoOponente.
    public override void NotificarCommand2Impacto()
    {
        // si la ultimate esta en curso (hitboxesActuales == hitboxesUltimate),
        // lo contabilizamos como golpe de ultimate; si no, delegamos al base.
        if (hitboxesActuales == hitboxesUltimate)
            _ultimateGolpeo = true;
        else
            command2GolpeoOponente = true;
    }
}