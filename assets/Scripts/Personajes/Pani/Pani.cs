using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Pani : PersonajeBase
{
    // cast de conveniencia para acceder a los datos especificos sin hacer cast manual cada vez
    private PaniData DatosPani => datosPersonaje as PaniData;

    // puno flojo (0) en lugar del 1 por defecto de PersonajeBase
    protected override int BotonActivadorUltimate => 0;

    [Header("Command1 - Animaciones")]
    [SerializeField] private string animC1Start = "Command1Start";
    [SerializeField] private string animC1Loop = "Command1Loop";
    [SerializeField] private string animC1Land = "Command1Land";

    [Header("Command2 - Animaciones")]
    [SerializeField] private string animC2Start = "Command2Start";
    [SerializeField] private string animC2Run = "Command2Run";
    [SerializeField] private string animC2Success = "Command2Success";
    [SerializeField] private string animC2Fail = "Command2Fail";

    [Header("Command3 - Animaciones")]
    [SerializeField] private string animC3Start = "Command3Start";
    [SerializeField] private string animC3Success = "Command3Success";
    [SerializeField] private string animC3Fail = "Command3Fail";

    [Header("Command4 - Animaciones")]
    [SerializeField] private string animC4Disparo = "Command4";

    [Header("Command1 - Efectos")]
    [SerializeField] private GameObject fuegoPiernaIzquierda;
    [SerializeField] private GameObject fuegoPiernaDerecha;

    [Header("Command2 - Agarre")]
    [SerializeField] private Transform puntoAgarreCommand2;

    [Header("Command3 - Agarre")]
    [SerializeField] private Transform puntoAgarreCommand3;

    [Header("Command4 - Proyectil")]
    [SerializeField] private ProyectilPani proyectil;

    [Header("Ultimate - Contador")]
    [Tooltip("Image de UI que muestra la cuenta atras. El GO debe estar DESACTIVADO por defecto.")]
    [SerializeField] private Image imagenContadorUltimate;

    [Tooltip("Sprites numericos para la cuenta atras. Indice 0 = sprite del 1, indice 4 = sprite del 5.")]
    [SerializeField] private Sprite[] spritesContadorUltimate;

    [Header("Ultimate - Flash")]
    [Tooltip("Imagen UI blanca full-screen para el flash. Debe tener alfa 0 y estar DESACTIVADA por defecto.")]
    [SerializeField] private Image imagenFlash;

    [Tooltip("AudioSource desde el que se reproduce el sonido del flash.")]
    [SerializeField] private AudioSource fuenteAudioUltimate;

    [Tooltip("Clip de audio que suena al dispararse el flash.")]
    [SerializeField] private AudioClip sonidoFlashUltimate;

    // posicion local del modelo en reposo, guardada para restaurarla tras el hover del command1
    private Vector3 _posLocalOriginalModelo;
    private bool _fuegoActivo;
    // flag que la animacion de command2/3 activa cuando debe soltar al agarrado
    private bool _soltarAgarreComando;
    private Coroutine _corrutinaImpulsoAtaque;
    private Coroutine _corrutinaFlash;
    // corrutina de la fase de trampa de la ultimate; se guarda para poder cancelarla
    private Coroutine _corrutinaTrampaPani;

    protected override void Start()
    {
        base.Start();
        ActivarFuego(false);
        if (modeloHijo != null)
            _posLocalOriginalModelo = modeloHijo.localPosition;
    }

    void LateUpdate()
    {
        // si el fuego sigue activo pero el personaje vuelve a Idle (p.ej. fue golpeado),
        // se apaga y se restauran los valores de movimiento y posicion del modelo
        if (_fuegoActivo && estadoActual == EstadoJugador.Idle)
        {
            ActivarFuego(false);
            buffMultVelocidad = 1f;
            if (modeloHijo != null)
                modeloHijo.localPosition = _posLocalOriginalModelo;
        }
    }

    // callbacks de PersonajeBase

    // disparado por animation event en las animaciones de command2 y command3
    public override void AnimEvent_SoltarAgarreComando()
    {
        _soltarAgarreComando = true;
    }

    protected override void OnReiniciarCallback()
    {
        // cancelar la trampa de la ultimate si el personaje se reinicia
        if (_corrutinaTrampaPani != null)
        {
            StopCoroutine(_corrutinaTrampaPani);
            _corrutinaTrampaPani = null;
        }
        OcultarFlash();
        OcultarContador();
    }

    protected override void OnRecibirGolpeCallback()
    {
        // detener el impulso de movimiento si Pani recibe un golpe durante un command
        if (_corrutinaImpulsoAtaque != null)
        {
            StopCoroutine(_corrutinaImpulsoAtaque);
            _corrutinaImpulsoAtaque = null;
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }

        // interrumpir la trampa de la ultimate si Pani es golpeada
        if (_corrutinaTrampaPani != null)
        {
            StopCoroutine(_corrutinaTrampaPani);
            _corrutinaTrampaPani = null;
        }

        OcultarFlash();
        OcultarContador();
    }

    // impulso de patada agachada

    protected override void EjecutarAtaqueNormal(int boton)
    {
        base.EjecutarAtaqueNormal(boton);

        // solo la patada floja agachada (boton 2 en contexto agachado) tiene impulso de deslizamiento
        if (boton == 2 && contextoActual == ContextoPersonaje.Agachado && DatosPani != null)
        {
            float impulso = DatosPani.impulsoPatadaFlojaAgachado;
            float duracion = DatosPani.duracionImpulsoPatadaAgachado;
            if (impulso > 0f && duracion > 0f)
                StartCoroutine(AplicarImpulsoPatadaAgachado(impulso, duracion));
        }
    }

    private IEnumerator AplicarImpulsoPatadaAgachado(float impulso, float duracion)
    {
        float dirZ = oponente != null
            ? Mathf.Sign(oponente.position.z - transform.position.z)
            : 1f;

        float t = 0f;
        while (t < duracion && estadoActual == EstadoJugador.Atacando)
        {
            // preservar la velocidad vertical (caida/gravedad) mientras se aplica el impulso lateral
            float vy = enSuelo ? Mathf.Min(rb.linearVelocity.y, 0f) : rb.linearVelocity.y;
            rb.linearVelocity = new Vector3(0f, vy, dirZ * impulso);
            t += Time.deltaTime;
            yield return null;
        }

        // frenar el deslizamiento al terminar si el personaje sigue en estado atacando
        if (estadoActual == EstadoJugador.Atacando)
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }

    // command1: jet dash con fuego en piernas y fase de hover

    protected override IEnumerator EjecutarCommand1()
    {
        float durStart = DatosPani != null ? DatosPani.duracionCommand1Start : 0.4f;
        float durHover = DatosPani != null ? DatosPani.duracionCommand1Hover : 2.0f;
        float durLand = DatosPani != null ? DatosPani.duracionCommand1Land : 0.4f;
        float multVel = DatosPani != null ? DatosPani.multVelocidadBoost : 2.5f;
        float elevacion = DatosPani != null ? DatosPani.elevacionHover : 0.5f;
        float velElev = DatosPani != null ? DatosPani.velocidadElevacion : 4f;

        float ultimaDir = oponente != null
            ? Mathf.Sign(oponente.position.z - transform.position.z)
            : 1f;
        float elevacionActual = 0f;

        IniciarComando();
        PlayAnim(animC1Start);
        ActivarFuego(true);

        try
        {
            // fase start: impulso inicial hacia el oponente
            float tiempoStart = durStart;
            while (tiempoStart > 0f)
            {
                AplicarMovimientoManual(ultimaDir, multVel);
                tiempoStart -= Time.deltaTime;
                yield return null;
            }

            // fase hover: se puede redirigir con el stick y cancelar pulsando abajo
            PlayAnim(animC1Loop);
            bool abajoAlEntrar = abajoPresionado;

            float tiempoHover = durHover;
            while (tiempoHover > 0f)
            {
                // evitar que mantener abajo al entrar cancele la fase inmediatamente
                if (!abajoPresionado) abajoAlEntrar = false;
                if (abajoPresionado && !abajoAlEntrar) break;

                if (movimientoHorizontal != 0f)
                    ultimaDir = Mathf.Sign(movimientoHorizontal);

                AplicarMovimientoManual(ultimaDir, multVel);
                elevacionActual = Mathf.MoveTowards(elevacionActual, elevacion, velElev * Time.deltaTime);
                if (modeloHijo != null)
                    modeloHijo.localPosition = _posLocalOriginalModelo + Vector3.up * elevacionActual;

                tiempoHover -= Time.deltaTime;
                yield return null;
            }

            // fase land: frenar y bajar el modelo a su posicion original
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            PlayAnim(animC1Land);
            ActivarFuego(false);

            float tiempoLand = durLand;
            while (tiempoLand > 0f)
            {
                elevacionActual = Mathf.MoveTowards(elevacionActual, 0f, velElev * Time.deltaTime);
                if (modeloHijo != null)
                    modeloHijo.localPosition = _posLocalOriginalModelo + Vector3.up * elevacionActual;
                tiempoLand -= Time.deltaTime;
                yield return null;
            }

            if (modeloHijo != null)
                modeloHijo.localPosition = _posLocalOriginalModelo;
        }
        finally
        {
            // garantizar limpieza aunque la corrutina sea interrumpida desde fuera
            buffMultVelocidad = 1f;
            ActivarFuego(false);
            if (modeloHijo != null)
                modeloHijo.localPosition = _posLocalOriginalModelo;
        }

        FinalizarComando();
    }

    // command2: dash de agarre con animacion de exito o fallo segun contacto

    protected override IEnumerator EjecutarCommand2()
    {
        float durStart = DatosPani != null ? DatosPani.duracionCommand2Start : 0.3f;
        float velDash = DatosPani != null ? DatosPani.velocidadCommand2Dash : 14f;
        float durRun = DatosPani != null ? DatosPani.duracionCommand2Runcycle : 1.2f;
        float durFail = DatosPani != null ? DatosPani.duracionCommand2Fail : 0.5f;
        float durSuccessMax = DatosPani != null ? DatosPani.duracionCommand2Success : 0.8f;

        IniciarComando();
        _agarreConectado = false;
        _personajeAgarrado = null;
        _soltarAgarreComando = false;

        // las hitboxes se preparan antes de PlayAnim para que AnimEvent_AbrirHitbox
        // las encuentre ya asignadas en hitboxesActuales cuando se dispare el evento
        PrepararHitboxesAgarre(hitboxesCommand2);
        hitboxesActuales = hitboxesCommand2;

        PlayAnim(animC2Start);
        SetHitboxes(hitboxesCommand2, true);
        yield return new WaitForSeconds(durStart);

        float dirZ = oponente != null
            ? Mathf.Sign(oponente.position.z - transform.position.z)
            : 1f;

        PlayAnim(animC2Run);

        float tiempoRun = durRun;
        while (tiempoRun > 0f && !_agarreConectado)
        {
            float vy = enSuelo ? Mathf.Min(rb.linearVelocity.y, 0f) : rb.linearVelocity.y;
            rb.linearVelocity = new Vector3(0f, vy, dirZ * velDash);
            tiempoRun -= Time.deltaTime;
            yield return null;
        }

        // fallback por si el anim event de cierre no llego a dispararse
        SetHitboxes(hitboxesCommand2, false);
        hitboxesActuales = null;
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

        if (_agarreConectado && _personajeAgarrado != null)
        {
            yield return null;
            _soltarAgarreComando = false;
            PlayAnim(animC2Success);

            float elapsed = 0f;
            while (elapsed < durSuccessMax)
            {
                // mantener al agarrado sobre el punto de agarre hasta que el anim event lo suelte
                if (!_soltarAgarreComando && _personajeAgarrado != null && puntoAgarreCommand2 != null)
                    _personajeAgarrado.FijarPosicionAgarre(puntoAgarreCommand2.position);

                if (_soltarAgarreComando && _personajeAgarrado != null)
                {
                    LiberarAgarradoCommand2();
                    _soltarAgarreComando = false;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // seguridad por si el anim event no disparo antes de que terminara el tiempo
            if (_personajeAgarrado != null) LiberarAgarradoCommand2();
        }
        else
        {
            yield return null;
            PlayAnim(animC2Fail);
            float esperaFail = durFail > 0.05f ? durFail : ObtenerDuracionClip(animC2Fail, 0.5f);
            yield return new WaitForSeconds(esperaFail);
        }

        _agarreConectado = false;
        _personajeAgarrado = null;
        _soltarAgarreComando = false;
        FinalizarComando();
    }

    // command3: agarre de punto fijo con ventana de hitbox controlada por anim events

    protected override IEnumerator EjecutarCommand3()
    {
        float durHitbox = DatosPani != null ? DatosPani.duracionCommand3Hitbox : 0.4f;
        float durSuccess = DatosPani != null ? DatosPani.duracionCommand3Success : 0.8f;
        float durFail = DatosPani != null ? DatosPani.duracionCommand3Fail : 0.5f;

        IniciarComando();
        _agarreConectado = false;
        _personajeAgarrado = null;

        // igual que command2: asignar antes de PlayAnim para que el anim event no falle
        PrepararHitboxesAgarre(hitboxesCommand3);
        hitboxesActuales = hitboxesCommand3;

        PlayAnim(animC3Start);

        float tiempoHitbox = durHitbox;
        while (tiempoHitbox > 0f && !_agarreConectado)
        {
            tiempoHitbox -= Time.deltaTime;
            yield return null;
        }

        // fallback por si el anim event de cierre no llego a dispararse
        SetHitboxes(hitboxesCommand3, false);
        hitboxesActuales = null;

        if (_agarreConectado && _personajeAgarrado != null)
        {
            yield return null;
            _soltarAgarreComando = false;
            PlayAnim(animC3Success);

            float elapsed = 0f;
            while (elapsed < durSuccess)
            {
                if (!_soltarAgarreComando && _personajeAgarrado != null && puntoAgarreCommand3 != null)
                    _personajeAgarrado.FijarPosicionAgarre(puntoAgarreCommand3.position);

                if (_soltarAgarreComando && _personajeAgarrado != null)
                {
                    LiberarAgarradoCommand3();
                    _soltarAgarreComando = false;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (_personajeAgarrado != null) LiberarAgarradoCommand3();
        }
        else
        {
            yield return null;
            PlayAnim(animC3Fail);
            float esperaFail = durFail > 0.05f ? durFail : ObtenerDuracionClip(animC3Fail, 0.5f);
            yield return new WaitForSeconds(esperaFail);
        }

        _agarreConectado = false;
        _personajeAgarrado = null;
        _soltarAgarreComando = false;
        FinalizarComando();
    }

    // command4 (QCF): disparo de proyectil diagonal

    protected override IEnumerator EjecutarComandoQCF(int boton)
    {
        float velProyectil = DatosPani != null ? DatosPani.velocidadProyectil : 12f;
        float tiempoVida = DatosPani != null ? DatosPani.tiempoVidaProyectil : 3f;
        float tiempoDiag = DatosPani != null ? DatosPani.tiempoDiagonal : 0.3f;
        float inclinacion = DatosPani != null ? DatosPani.inclinacionVertical : 8f;
        float durAnim = DatosPani != null ? DatosPani.duracionCommand4Anim : 0.5f;

        IniciarComando();
        PlayAnim(animC4Disparo);

        if (proyectil != null)
        {
            float dirZ = oponente != null
                ? Mathf.Sign(oponente.position.z - transform.position.z)
                : 1f;
            proyectil.SetDueno(this);
            proyectil.Lanzar(dirZ, velProyectil, tiempoVida, tiempoDiag, inclinacion);
        }

        yield return new WaitForSeconds(durAnim);
        FinalizarComando();
    }

    // ultimate: pone una trampa de flash y penaliza al oponente si se mueve

    protected override IEnumerator EjecutarUltimate()
    {
        PaniData d = DatosPani;
        if (d == null) yield break;

        if (!GastarStocksUltimate()) yield break;

        // bloquear el control solo durante la animacion de activacion
        IniciarComando();
        PlayAnim(d.animUltimate);

        float tAnim = d.duracionUltimateAnim;
        while (tAnim > 0f)
        {
            tAnim -= Time.deltaTime;
            yield return null;
        }

        // devolver el control al jugador; la trampa sigue ejecutandose en segundo plano
        FinalizarComando();

        if (_corrutinaTrampaPani != null) StopCoroutine(_corrutinaTrampaPani);
        _corrutinaTrampaPani = StartCoroutine(FaseTrampaUltimate(d));
    }

    private IEnumerator FaseTrampaUltimate(PaniData d)
    {
        PersonajeBase enemigo = oponente != null
            ? oponente.GetComponent<PersonajeBase>()
            : null;

        MostrarContador();

        // cuenta atras visible para el oponente
        float tTrampa = d.duracionTrampa;
        while (tTrampa > 0f)
        {
            ActualizarContador(Mathf.CeilToInt(tTrampa));
            tTrampa -= Time.deltaTime;
            yield return null;
        }

        OcultarContador();

        // el flash se lanza como corrutina independiente para no bloquear la fase de captura
        _corrutinaFlash = StartCoroutine(EfectoFlashFoto());

        // ventana de captura: si el enemigo no esta en Idle recibe el golpe de trampa
        float tCaptura = d.duracionCaptura;
        while (tCaptura > 0f)
        {
            if (enemigo != null && enemigo.EstadoActualPublico != EstadoJugador.Idle)
            {
                enemigo.RecibirGolpe(
                    d.hitstunTrampa,
                    DireccionGolpe.Frente,
                    d.tipoGolpeTrampa,
                    d.impulsoYTrampa,
                    d.impulsoZTrampa,
                    d.danoTrampa,
                    default,
                    true);
                break;
            }

            tCaptura -= Time.deltaTime;
            yield return null;
        }

        _corrutinaTrampaPani = null;
    }

    // helpers de cuenta atras

    private void MostrarContador()
    {
        if (imagenContadorUltimate != null)
            imagenContadorUltimate.gameObject.SetActive(true);
    }

    private void OcultarContador()
    {
        if (imagenContadorUltimate != null)
            imagenContadorUltimate.gameObject.SetActive(false);
    }

    private void ActualizarContador(int numero)
    {
        if (imagenContadorUltimate == null
            || spritesContadorUltimate == null
            || spritesContadorUltimate.Length == 0) return;

        // el numero va de 5 a 1; los sprites estan en indices 0-4, por eso se resta 1
        int idx = Mathf.Clamp(numero - 1, 0, spritesContadorUltimate.Length - 1);
        imagenContadorUltimate.sprite = spritesContadorUltimate[idx];
    }

    // helpers de flash

    private IEnumerator EfectoFlashFoto()
    {
        if (imagenFlash == null) yield break;

        if (fuenteAudioUltimate != null && sonidoFlashUltimate != null)
            fuenteAudioUltimate.PlayOneShot(sonidoFlashUltimate);

        // tres destellos: subida rapida de alfa y bajada suave
        const int parpadeos = 3;
        const float durSubida = 0.05f;
        const float durBajada = 0.18f;

        Color c = imagenFlash.color;
        imagenFlash.gameObject.SetActive(true);

        for (int i = 0; i < parpadeos; i++)
        {
            float t = 0f;
            while (t < durSubida)
            {
                c.a = Mathf.Lerp(0f, 1f, t / durSubida);
                imagenFlash.color = c;
                t += Time.deltaTime;
                yield return null;
            }

            t = 0f;
            while (t < durBajada)
            {
                c.a = Mathf.Lerp(1f, 0f, t / durBajada);
                imagenFlash.color = c;
                t += Time.deltaTime;
                yield return null;
            }
        }

        OcultarFlash();
        _corrutinaFlash = null;
    }

    private void OcultarFlash()
    {
        // detener la corrutina si sigue en curso antes de forzar alfa 0
        if (_corrutinaFlash != null)
        {
            StopCoroutine(_corrutinaFlash);
            _corrutinaFlash = null;
        }

        if (imagenFlash == null) return;
        Color c = imagenFlash.color;
        c.a = 0f;
        imagenFlash.color = c;
        imagenFlash.gameObject.SetActive(false);
    }

    // helpers internos

    // configura las hitboxes de agarre y les aplica escala/offset segun el comando
    private void PrepararHitboxesAgarre(GameObject[] hitboxes)
    {
        if (hitboxes == null || datosPersonaje == null) return;
        foreach (var hb in hitboxes)
        {
            if (hb == null) continue;
            Hitbox h = hb.GetComponent<Hitbox>();
            if (h == null) continue;
            h.esAgarre = true;
            h.SetDano(datosPersonaje.danoAgarre);
        }

        if (hitboxes == hitboxesCommand2)
            AjustarHitboxes(hitboxes, datosPersonaje.scaleCommand2, datosPersonaje.offsetCommand2);
        else if (hitboxes == hitboxesCommand3)
            AjustarHitboxes(hitboxes, datosPersonaje.scaleCommand3, datosPersonaje.offsetCommand3);
        else
            AjustarHitboxes(hitboxes);
    }

    private void LiberarAgarradoCommand2()
    {
        if (_personajeAgarrado == null) return;
        PaniData d = DatosPani;
        float iy = d != null ? d.impulsoYLanzadoCommand2 : 10f;
        float iz = d != null ? d.impulsoZLanzadoCommand2 : 5f;
        float hitstun = datosPersonaje != null ? datosPersonaje.hitstunComando2 : 1f;
        float dano = datosPersonaje != null ? datosPersonaje.danoComando2 : 12f;
        _personajeAgarrado.LiberarDeAgarre(iy, iz, _signoOrientacion, hitstun, dano, permitirJuggle: true);
        _personajeAgarrado = null;
    }

    private void LiberarAgarradoCommand3()
    {
        if (_personajeAgarrado == null) return;
        float iy = datosPersonaje != null ? datosPersonaje.impulsoYComando3 : 8f;
        float iz = datosPersonaje != null ? datosPersonaje.impulsoZComando3 : 0f;
        float hitstun = datosPersonaje != null ? datosPersonaje.hitstunComando3 : 1f;
        float dano = datosPersonaje != null ? datosPersonaje.danoComando3 : 15f;
        _personajeAgarrado.LiberarDeAgarre(iy, iz, _signoOrientacion, hitstun, dano, permitirJuggle: true);
        _personajeAgarrado = null;
    }

    // aplica velocidad manual sobre el eje Z respetando la velocidad vertical actual
    private void AplicarMovimientoManual(float direccion, float mult)
    {
        float vel = direccion * datosPersonaje.velocidadMovimiento * mult;
        float vy = enSuelo ? Mathf.Min(rb.linearVelocity.y, 0f) : rb.linearVelocity.y;
        rb.linearVelocity = new Vector3(0f, vy, vel);
    }

    private void ActivarFuego(bool activo)
    {
        _fuegoActivo = activo;
        if (fuegoPiernaIzquierda != null) fuegoPiernaIzquierda.SetActive(activo);
        if (fuegoPiernaDerecha != null) fuegoPiernaDerecha.SetActive(activo);
    }
}