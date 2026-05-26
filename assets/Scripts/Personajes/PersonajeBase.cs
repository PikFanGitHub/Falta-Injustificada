using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;

// Enums compartidos por todos los personajes
public enum EstadoJugador
{
    Idle,
    Caminar,
    Corriendo,
    Saltar,
    Agachado,
    Atacando,
    Comando,
    Dizzy,
    Hitstun,
    Derribado,
    Agarrado,
    Dasheando
}

public enum ContextoPersonaje
{
    Suelo,
    Agachado,
    Aire
}

// Resultado de RecibirGolpe: permite a Hitbox.cs distinguir entre
// golpe conectado, bloqueado (recoil doble al atacante) e ignorado (derribado, escudo).
public enum ResultadoGolpe
{
    Conectado,
    Bloqueado,
    Ignorado
}

// PersonajeBase.cs
// Clase base abstracta para todos los personajes del juego.
// Contiene toda la logica comun: movimiento, fisica, input,
// ataques normales, bloqueo, hitstun, lanzados, tech, agarres, gizmos, dash.
//
// Agacharse / hurtboxes: sistema de doble collider (hurtboxCollider + hurtboxAgachadoCollider).
// Dashes: sistema completo con estado Dasheando, 4 tipos, cancelabilidad y buffer.
// Efectos de golpe: SpawnParticulasGolpe con prefabParticulasGolpe / prefabParticulasBloqueo.
//
// Lo que varia por personaje (sobreescribir en subclase):
//   EjecutarCommand1() -> QCB + Puno Flojo
//   EjecutarCommand2() -> QCB + Puno Fuerte
//   EjecutarCommand3() -> QCB + Patada Fuerte
//   EjecutarComandoQCF() -> QCF + cualquier boton
//   EjecutarUltimate()  -> se activa con doble QCF + BotonActivadorUltimate
//   BotonActivadorUltimate -> boton que completa el doble QCF (por defecto 1 = Puno Fuerte)
//   PuedeEjecutarCommandX() -> gates opcionales
//   OnFinAtaqueCallback() -> hook post-ataque especifico
public abstract class PersonajeBase : MonoBehaviour
{
    // ScriptableObject
    [Header("ScriptableObject del personaje")]
    [SerializeField] protected PersonajeDataBase datosPersonaje;

    [Header("Oponente")]
    [SerializeField] protected Transform oponente;

    [Header("Modelo (hijo con el Animator, se busca automaticamente)")]
    [SerializeField] protected Transform modeloHijo;

    // Hitboxes normales
    [Header("Hitboxes - Ataques normales")]
    [SerializeField] protected GameObject[] hitboxesPunoFlojo;
    [SerializeField] protected GameObject[] hitboxesPunoFuerte;
    [SerializeField] protected GameObject[] hitboxesPatadaFloja;
    [SerializeField] protected GameObject[] hitboxesPatadaFuerte;

    // Hitboxes de comandos (usadas por las subclases)
    [Header("Hitboxes - Comandos (asignar en subclase / Inspector)")]
    [SerializeField] protected GameObject[] hitboxesCommand1;
    [SerializeField] protected GameObject[] hitboxesCommand2;
    [SerializeField] protected GameObject[] hitboxesCommand3;
    [SerializeField] protected GameObject[] hitboxesCommand4;

    // Hitbox de ultimate
    [Header("Hitboxes - Ultimate")]
    [SerializeField] protected GameObject[] hitboxesUltimate;

    // Hitboxes de agarre
    [Header("Hitboxes - Agarre")]
    [SerializeField] protected GameObject[] hitboxesAgarre;
    [Tooltip("Punto del rig donde se coloca al enemigo mientras esta agarrado. Opcional.")]
    [SerializeField] protected Transform puntoAgarre;

    [Header("Clash de agarre")]
    [SerializeField] private float velocidadBaseClash = 18f;
    [SerializeField] private float duracionClash = 0.25f;
    [SerializeField] private float multiplicadorClashSobreRetroceso = 12f;

    // Animator
    [Header("Animator (se busca automaticamente)")]
    [SerializeField] protected Animator animator;

    // Animaciones de movimiento
    [Header("Nombres de estados - Movimiento")]
    [SerializeField] protected string animIdle = "Idle";
    [SerializeField] protected string animCaminar = "Walk";
    [SerializeField] protected string animCaminarAtras = "Walk Backwards";
    [SerializeField] protected string animCorrer = "Run";
    [SerializeField] protected string animFrenar = "Frenar";
    [SerializeField] protected string animSaltar = "Jump";
    [SerializeField] protected string animAire = "Air";
    [SerializeField] protected string animAgachado = "Crouch";
    [SerializeField] protected string animAgachadoIdle = "Crouching";
    [SerializeField] protected string animLevantarse = "StandUp";
    [SerializeField] protected string animHitstun = "Hurt";
    [SerializeField] protected string animHurt2 = "Hurt2";
    [SerializeField] protected string animDead = "Dead";
    [SerializeField] protected string animDizzy = "Dizzy";
    [SerializeField] protected float duracionDerribado = 2.5f;
    [SerializeField] protected string animWin = "Victory";
    [SerializeField] protected string animBlock = "Block";
    [SerializeField] protected string animBlockCrouch = "BlockCrouch";
    [SerializeField] protected string animHitstunWinRar = "hit";

    // Animaciones de agarre
    [Header("Nombres de estados - Agarre")]
    [SerializeField] protected string animAgarre = "Grab";
    [SerializeField] protected string animAgarreHold = "GrabHold";

    // Animaciones de lanzado
    [Header("Nombres de estados - Lanzados")]
    [SerializeField] protected string animLanzadoArriba = "LaunchUp";
    [SerializeField] protected string animLanzadoFrente = "LaunchFront";
    [SerializeField] protected string animLanzadoAbajo = "LaunchDown";
    [SerializeField] protected string animAterrizajeDuro = "LandHard";

    // Animaciones de comandos (disponibles para subclases)
    [Header("Nombres de estados - Comandos")]
    [SerializeField] protected string animCommand1 = "Command1";
    [SerializeField] protected string animCommand2_1 = "Command2-1";
    [SerializeField] protected string animCommand2_2 = "Command2-2";
    [SerializeField] protected string animCommand2_3 = "Command2-3";
    [SerializeField] protected string animCommand3 = "Command3";
    [SerializeField] protected string animCommand4 = "Command4";

    // Dash
    [Header("Dash - Configuracion general")]
    [SerializeField] protected bool puedeDashAdelante = false;
    [Tooltip("Si true, el doble-tap adelante hace dash en lugar de correr. " +
             "El personaje NO puede correr si esto esta activo.")]
    [SerializeField] protected bool puedeDashAire = false;

    [Header("Dash - Adelante Suelo")]
    [SerializeField] protected string animDashAdelanteSuelo = "DashForward";
    [SerializeField] protected float velocidadDashAdelanteSuelo = 15f;
    [SerializeField] protected float distanciaDashAdelanteSuelo = 3f;
    [SerializeField] protected float lagDashAdelanteSuelo = 0.1f;

    [Header("Dash - Atras Suelo")]
    [SerializeField] protected string animDashAtrasSuelo = "DashBack";
    [SerializeField] protected float velocidadDashAtrasSuelo = 12f;
    [SerializeField] protected float distanciaDashAtrasSuelo = 2.5f;
    [SerializeField] protected float lagDashAtrasSuelo = 0.2f;

    [Header("Dash - Adelante Aire")]
    [SerializeField] protected string animDashAdelanteAire = "AirDashForward";
    [SerializeField] protected float velocidadDashAdelanteAire = 12f;
    [SerializeField] protected float distanciaDashAdelanteAire = 3f;
    [SerializeField] protected float lagDashAdelanteAire = 0.1f;

    [Header("Dash - Atras Aire")]
    [SerializeField] protected string animDashAtrasAire = "AirDashBack";
    [SerializeField] protected float velocidadDashAtrasAire = 10f;
    [SerializeField] protected float distanciaDashAtrasAire = 2.5f;
    [SerializeField] protected float lagDashAtrasAire = 0.15f;

    [Header("Salto desde carrera / dash adelante")]
    [Tooltip("Fuerza vertical del salto cuando se viene de correr o de un dash adelante.")]
    [SerializeField] protected float fuerzaSaltoCorridaDash = 10f;
    [Tooltip("Velocidad horizontal del salto al venir de correr o de un dash adelante.")]
    [SerializeField] protected float velocidadHorizontalSaltoCorridaDash = 5f;

    // Fisica del salto
    [Header("Fisica del salto")]
    [SerializeField] protected float gravedadExtra = 20f;
    [SerializeField] protected float velocidadSaltoHorizontal = 3f;
    [Tooltip("Capas que se consideran suelo en el raycast de aterrizaje. " +
             "EXCLUIR la capa del oponente para evitar aterrizar sobre su cabeza.")]
    [SerializeField] private LayerMask maskaSuelo = ~0;
    [SerializeField] protected bool permitirDobleSalto = false;
    protected bool _dobleSaltoUsado = false;

    [Header("Carrera")]
    [SerializeField] protected float ventanaDoubleTap = 0.3f;

    [Header("Comandos QCF / QCB")]
    [SerializeField] protected float ventanaQCF = 2.0f;

    [Header("Rebote tras lanzamiento")]
    protected bool _enVueloLanzado = false;
    protected Vector3 _velocidadAnteColision = Vector3.zero;
    [SerializeField] protected float multiplicadorImpulsoLanzado = 1.5f;
    protected bool _reboteSuelo = false;

    // Signo de orientacion: 1 = derecha, -1 = izquierda
    protected float _signoOrientacion = 1f;
    private float _tiempoUltimoFlip = -999f;

    [Header("Bloqueo de proximidad")]
    [SerializeField] protected GameObject colliderBloqueoProximidad;

    [Header("Colision del cuerpo (hurtboxes)")]
    [Tooltip("Collider principal del GO hijo Hurtbox (BoxCollider o CapsuleCollider). " +
             "Se desactiva cuando el personaje esta agachado.")]
    [SerializeField] private Collider hurtboxCollider;

    [Tooltip("Collider alternativo que se activa SOLO cuando el personaje esta agachado. " +
             "Debe ser un GO hijo independiente. Se desactiva en cualquier otro estado.")]
    [SerializeField] private Collider hurtboxAgachadoCollider;

    [SerializeField] protected float graciaAbajo = 0.15f;
    [SerializeField] protected float duracionDizzy = 3f;

    // Tech
    [Header("Tech (escape de combo en el aire)")]
    [SerializeField] protected float techDelayInicio = 0.8f;
    [SerializeField] protected float techVentana = 0.6f;
    [SerializeField] protected float techImpulsoZ = 10f;
    [SerializeField] protected float techImpulsoY = 6f;
    [SerializeField] protected float techTiempoMantener = 0.08f;
    [SerializeField] protected string animTech = "Jump";

    protected bool enVentanaTech = false;
    protected int _techBotonesPresionados = 0;
    protected float _techDosBotonesTiempo = -999f;

    // Hitstop
    [Header("Hitstop (freeze frame al impactar)")]
    [SerializeField] protected float duracionHitstop = 0.06f;
    [SerializeField] protected float duracionHitstopBloqueo = 0.04f;
    private Coroutine _corrutinaHitstop;
    private Coroutine _corrutinaHitstopWinRar;

    // Combo Counter UI
    [Header("Combo Counter UI")]
    [SerializeField] private Image _imagenDecenas;
    [SerializeField] private Image _imagenUnidades;
    [SerializeField] private Sprite[] _spritesNumeros;
    [SerializeField] private float duracionDesvanecerCombo = 1.5f;

    // Escalado de dano por combo
    [Header("Escalado de dano por combo")]
    [SerializeField] private float comboEscaladoPorGolpe = 0.1f;
    [SerializeField] private float comboEscaladoMinimo = 0.1f;

    [Header("Efectos VFX")]
    [SerializeField] protected GameObject prefabParticulasGolpe;
    [SerializeField] protected GameObject prefabParticulasBloqueo;
    [Tooltip("VFX que se spawnea en el suelo al saltar. Se reproduce 0.8 s y luego se destruye.")]
    [SerializeField] protected GameObject prefabVFXSalto;
    [Tooltip("VFX (Visual Effect Graph) que se spawnea en los pies al hacer dash adelante en suelo.")]
    [SerializeField] protected GameObject prefabVFXDashAdelante;
    [Tooltip("Sistema de particulas que se spawnea en los pies mientras el jugador corre.")]
    [SerializeField] protected GameObject prefabParticulasCorrer;
    [Tooltip("Sistema de particulas que se spawnea en los pies al aterrizar.")]
    [SerializeField] protected GameObject prefabParticulasAterrizaje;

    [Header("Ultimate")]
    [SerializeField] protected BarraUltimate barraUltimate;
    [Tooltip("Porcentaje del dano infligido que se convierte en carga de ultimate (0 = nada, 1 = 100% del dano)")]
    [SerializeField][Range(0f, 2f)] protected float porcentajeCargaUltimate = 0.2f;
    [Tooltip("Stocks de barra necesarios para poder lanzar la ultimate (1, 2 o 3)")]
    [SerializeField][Range(1, 3)] protected int stocksNecesariosUltimate = 3;

    // Deteccion de doble QCF para el ultimate
    private float _tPrimerQCFUltimate = -999f;
    private float _tSegundoQCFUltimate = -999f;
    [Tooltip("Ventana en segundos para completar el segundo QCF tras el primero")]
    [SerializeField] private float ventanaDobleQCFUltimate = 1.5f;

    // Ultimate — boton que completa el doble QCF (por defecto Puno Fuerte = 1).
    // Sobreescribir en la subclase para usar otro boton (ej. Pani usa Puno Flojo = 0).
    // Lee el boton del data asset. Las subclases pueden sobreescribir
    // para mantener compatibilidad (p.ej. Pani => 0 sigue funcionando).
    protected virtual int BotonActivadorUltimate =>
        datosPersonaje != null ? datosPersonaje.secuenciaUltimate.botonActivacion : 1;

    // Estado WinRar
    [Header("Estado WinRar (victima de la ultimate de Camus)")]
    [Tooltip("AnimatorController que reemplaza al original mientras el personaje esta en estado WinRar. " +
             "Debe tener los mismos nombres de estado: Idle, Walk, Walk Backwards, Crouch, Crouching, " +
             "StandUp, Hurt, Hurt2, Dead. Opcional si el Animator del modelo WinRar ya tiene su propio controller.")]
    [SerializeField] private RuntimeAnimatorController controladorWinRar;

    [Tooltip("GO hijo del personaje con el modelo 3D del WinRAR y su propio Animator. " +
             "Debe estar desactivado por defecto. Al entrar en estado WinRar se activa " +
             "y se oculta el modelo original.")]
    [SerializeField] private GameObject modeloWinRar;

    // Estado interno WinRar
    protected bool _enEstadoWinRar = false;
    private float _tiempoFinWinRar = -1f;
    private RuntimeAnimatorController _controladorOriginalWinRar;
    private Animator _animatorOriginalWinRar;

    // Multiplicadores de buff
    protected float buffMultVelocidad = 1f;
    protected float buffMultSalto = 1f;

    // Componentes internos
    protected PlayerInput playerInput;
    protected Rigidbody rb;
    protected SaludJugador salud;
    protected bool _ataqueHaConectado = false;

    // Estado interno
    protected float movimientoHorizontal = 0f;
    protected bool enSuelo = true;
    protected bool recienSaltado = false;
    protected float tiempoSalto = -999f;
    protected const float minTiempoEnAire = 0.15f;
    protected EstadoJugador estadoActual = EstadoJugador.Idle;
    protected EstadoJugador estadoAnterior = EstadoJugador.Idle;
    protected ContextoPersonaje contextoActual = ContextoPersonaje.Suelo;
    protected GameObject[] hitboxesActuales = null;
    protected bool ultimaOrientacionAdelante = true;
    protected Vector3 escalaOriginalModelo;
    protected bool _juegoTerminado = false;
    private bool _enClash = false;

    // Acceso publico de solo lectura al estado actual.
    // Util para sistemas externos (p.ej. la trampa de la ultimate de Pani).
    public EstadoJugador EstadoActualPublico => estadoActual;

    // Correr
    protected float ultimoPulsoAdelante = -999f;
    protected float ultimoPulsoAtras = -999f;
    protected bool corriendo = false;

    // Cancel on hit / buffer
    protected bool ataqueEsCancelable = false;
    protected bool _command1Cancelado = false;
    protected bool yaAtacoEnAire = false;

    protected AtaqueID _ataqueActivoID = AtaqueID.Ninguno;

    protected int bufferAtaque = -1;
    protected float bufferAtaqueTiempo = -999f;
    protected bool bufferSalto = false;
    protected float bufferSaltoTiempo = -999f;

    [Header("Buffer de input")]
    [SerializeField] protected float bufferAtaqueVentana = 0.5f;

    protected bool bufferAgacharse = false;

    // Derribado
    protected bool estaDerribado = false;

    // Gracia de dirección para cancel a salto diagonal
    // Guarda la última dirección horizontal pulsada y su timestamp, de modo que
    // si el jugador aprieta dirección+salto simultáneamente (y el evento llega en
    // distinto orden) o mantiene la dirección justo antes del cancel, el salto
    // salga en diagonal igualmente.
    private float _ultimaDireccionHorizontal = 0f;
    private float _tUltimaDireccionHorizontal = -999f;
    [Header("Cancel a salto diagonal")]
    [Tooltip("Ventana de tiempo (s) en la que se recuerda la última dirección pulsada " +
             "para facilitar el salto diagonal al cancelar un ataque. " +
             "Valores entre 0.10 y 0.18 son recomendados.")]
    [SerializeField] private float graciaHorizontalCancelSalto = 0.14f;

    // Generacion de ataque
    protected int _ataqueGeneracion = 0;

    // QCF / QCB timestamps
    protected float tAbajoOn = -999f;
    protected float tAbajoOff = -999f;
    protected float tAdelanteOn = -999f;
    protected float tAbajoOnQCB = -999f;
    protected float tAbajoOffQCB = -999f;
    protected float tAtrasOn = -999f;

    // Timestamps para secuencias adicionales
    private float _tAtrasParaMCF = -999f;             // MCA: cuando se pulsó atrás
    private float _tAdelanteParaMCB = -999f;          // MCAtras: cuando se pulsó adelante
    private float _tUltimoAbajoSoltado = -999f;       // Doble-abajo: último release de abajo
    private float _tDobleAbajoCompleto = -999f;       // Doble-abajo: marca de secuencia completa
    private float _tQCFCompletadoParaInverso = -999f; // QCF+QCFInverso: cuándo se completó el QCF

    // Flags de teclas
    protected bool abajoPresionado = false;
    protected bool adelantePresionado = false;

    // Command2
    protected bool command2GolpeoOponente = false;
    protected bool enVentanaCancelCommand2 = false;
    protected bool command2CancelUsado = false;
    protected int command2BotonCancel = -1;
    [SerializeField] protected float ventanaCancelCommand2 = 0.4f;

    // Command3
    protected bool command3GolpeoOponente = false;

    // Ultimate
    protected bool _ultimateGolpeo = false;

    // Bloqueo
    private bool _enBlockstun = false;
    private Coroutine _corrutinaBloqueo = null;

    // Eventos de animacion (anti-duplicado)
    private int frameUltimoAbrirHitbox = -1;
    private int frameUltimoCerrarHitbox = -1;

    // Acciones de Input
    private InputAction moverDerecha;
    private InputAction moverIzquierda;
    private InputAction saltarAction;
    private InputAction agacharseAction;
    private InputAction punoFlojoAction;
    private InputAction punoFuerteAction;
    private InputAction patadaFlojaAction;
    private InputAction patadaFuerteAction;
    private InputAction agarreAction;

    // Estado de agarre (lado agarrador)
    protected bool _agarreConectado = false;
    protected bool _esperandoSoltarAgarre = false;
    protected PersonajeBase _personajeAgarrado = null;
    private bool _esHaciaAtrasAgarre = false;
    private float _dirZLanzamientoAgarre = 1f;
    private bool _intentandoAgarrar = false;

    // Estado de agarre (lado agarrado)
    private PersonajeBase _agarradorActual = null;

    // Colision dinamica suelo/aire
    private Collider[] _collidersCuerpoPropio;
    private Collider[] _collidersCuerpoOponente;
    private PersonajeBase _oponenteBase;
    private bool _colisionActivaActual = true;
    private float _tiempoUltimoRebotePared = -999f;
    private const float _cooldownRebotePared = 0.25f;

    private Vector3 _centroColisionOriginal;
    private Vector3 _hurtboxSizeOriginal;
    private Vector3 _proxBloqueoLocalPosOriginal;

    // Combo counter
    private int _contadorCombo = 0;
    private int _ultimoComboMostrado = 0;
    private Coroutine _corrutinaVigilarCombo = null;
    private Coroutine _corrutinaDesvanecerCombo = null;

    // Dash - estado interno
    protected bool _dashAereoUsado = false;
    protected bool _dasheandoAdelante = false;
    private Coroutine _corrutinaDash = null;
    private bool _comandoEnEjecucion = false;
    private bool _dashActualCancelableAAtaque = false;
    private bool _bufferDash = false;
    private bool _bufferDashEsAdelante = false;
    private bool _bufferDashEsAereo = false;
    private float _bufferDashTiempo = -999f;

    // Api publica
    private AnimationSoundPlayer _soundPlayer;

    public void SetOponente(Transform t)
    {
        oponente = t;
        InicializarColisionesConOponente();
    }

    public void SetBarraUltimate(BarraUltimate barra)
    {
        barraUltimate = barra;
    }

    public virtual void NotificarCommand3Impacto() { command3GolpeoOponente = true; }
    public virtual void NotificarCommand2Impacto() { command2GolpeoOponente = true; }
    public virtual void NotificarPalaImpacto() { }

    // La hitbox de ultimate llama a este metodo cuando conecta con el enemigo.
    public virtual void NotificarUltimateImpacto() { _ultimateGolpeo = true; }

    public virtual void NotificarGolpeConectado(float danoConectado = 0f)
    {
        if (datosPersonaje == null) return;
        StartCoroutine(AplicarRetroceso());
        IniciarHitstop(duracionHitstop);
        IncrementarCombo();
        float carga = danoConectado * porcentajeCargaUltimate;
        if (carga > 0f) barraUltimate?.AgregarCarga(carga);
    }

    public virtual void NotificarGolpeBloqueado()
    {
        StartCoroutine(AplicarRetrocesoDoble());
        IniciarHitstop(duracionHitstopBloqueo);
    }

    IEnumerator AplicarRetrocesoDoble()
    {
        if (datosPersonaje == null) yield break;
        float vel = datosPersonaje.velocidadRetroceso * 2f;
        float dur = datosPersonaje.duracionRetroceso;
        float dir = DireccionLanzado();
        float t = 0f;
        while (t < dur)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, dir * vel);
            t += Time.deltaTime;
            yield return null;
        }
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }

    public virtual void NotificarImpactoCancelable()
    {
        if (!ataqueEsCancelable) return;
        // NOTA: se eliminaron los guards "if (_comandoEnEjecucion) return" y
        // "if (estadoActual == Comando) return" porque bloqueaban el cancel de
        // Command1: IniciarComando() pone _comandoEnEjecucion=true, por lo que
        // NotificarImpactoCancelable salía antes de llamar a CancelarAtaqueActual(),
        // impidiendo que _command1Cancelado se pusiera a true.

        _ataqueHaConectado = true;

        if (EjecutarCancelDash()) return;

        if (datosPersonaje != null)
        {
            AtaqueID cancelTargets = datosPersonaje.ObtenerCanceles(_ataqueActivoID);
            if (cancelTargets == AtaqueID.Ninguno) return;

            if (bufferSalto && (cancelTargets & AtaqueID.Salto) != 0
                && Time.time - bufferSaltoTiempo <= bufferAtaqueVentana)
            {
                bufferSalto = false;
                ataqueEsCancelable = false;
                Saltar();
                return;
            }

            if (bufferAtaque < 0) return;
            // FIX cancel a comando: igual que en OnBotonAtaque, usar el ID enriquecido
            // para que el cancel check permita buffers que van a disparar un comando.
            AtaqueID ataqueDestino = BotonConSecuenciaAAtaqueID(bufferAtaque, contextoActual);
            if ((cancelTargets & ataqueDestino) == 0) return;
        }
        if (bufferSalto && Time.time - bufferSaltoTiempo <= bufferAtaqueVentana)
        {
            bufferSalto = false;
            ataqueEsCancelable = false;
            Saltar();
            return;
        }

        if (bufferAtaque < 0) return;

        ataqueEsCancelable = false;
        CancelarAtaqueActual();
    }

    public virtual void NotificarPalaSuelo(Vector3 posContacto) { }
    public virtual void RecogerPala() { }

    public void AplicarDano(float cantidad)
    {
        SaludJugador s = GetComponent<SaludJugador>();
        if (s != null) s.RecibirDano(cantidad);
    }

    public void RecibirHitstun(float duracion, DireccionGolpe dir = DireccionGolpe.Frente)
    {
        RecibirGolpe(duracion, dir, TipoGolpe.Normal);
    }

    public void SetDatosPersonaje(FighterData d) => datosPersonaje = d;

    // Estado WinRar - API publica

    private void OcultarRenderers(Transform raiz, bool ocultar)
    {
        foreach (var smr in raiz.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            smr.enabled = !ocultar;
        foreach (var mr in raiz.GetComponentsInChildren<MeshRenderer>(true))
            mr.enabled = !ocultar;
    }

    public void EntrarEstadoWinRar(float duracion, bool playHitAnim = false)
    {
        _tiempoFinWinRar = Time.time + duracion;
        if (_enEstadoWinRar) return;

        StopAllCoroutines();
        _corrutinaDash = null;
        SetAllHitboxes(false);
        hitboxesActuales = null;
        corriendo = false;
        bufferAgacharse = false;
        bufferSalto = false;
        bufferAtaque = -1;
        _dashAereoUsado = false;
        _bufferDash = false;
        LimpiarEstadoAgarrador();
        LimpiarEstadoAgarrado();
        ResetearBloqueo();
        LimpiarQCF();
        LimpiarQCB();
        _ataqueGeneracion++;
        rb.linearVelocity = Vector3.zero;
        enSuelo = true;
        estaDerribado = false;
        contextoActual = ContextoPersonaje.Suelo;
        _enEstadoWinRar = true;

        if (modeloWinRar != null)
        {
            _animatorOriginalWinRar = animator;
            if (_animatorOriginalWinRar != null)
                _animatorOriginalWinRar.enabled = false;

            if (modeloHijo != null)
                OcultarRenderers(modeloHijo, true);

            modeloWinRar.SetActive(true);

            Animator animWR = modeloWinRar.GetComponent<Animator>();
            if (animWR == null) animWR = modeloWinRar.GetComponentInChildren<Animator>();
            if (animWR != null)
            {
                animator = animWR;
                animator.applyRootMotion = false;
                if (controladorWinRar != null)
                    animator.runtimeAnimatorController = controladorWinRar;
            }
        }
        else if (controladorWinRar != null && animator != null)
        {
            _controladorOriginalWinRar = animator.runtimeAnimatorController;
            animator.runtimeAnimatorController = controladorWinRar;
        }

        CambiarEstado(EstadoJugador.Idle);
        PlayAnim(playHitAnim ? animHitstun : animIdle);
    }

    private void SalirEstadoWinRar()
    {
        _enEstadoWinRar = false;
        _tiempoFinWinRar = -1f;

        if (modeloWinRar != null)
        {
            modeloWinRar.SetActive(false);
            if (modeloHijo != null) OcultarRenderers(modeloHijo, false);
            if (_animatorOriginalWinRar != null)
            {
                animator = _animatorOriginalWinRar;
                animator.enabled = true;
            }
            _animatorOriginalWinRar = null;
        }
        else
        {
            if (_controladorOriginalWinRar != null && animator != null)
                animator.runtimeAnimatorController = _controladorOriginalWinRar;
            _controladorOriginalWinRar = null;
        }

        // Si está recibiendo un golpe, no interrumpir la secuencia.
        // Las corrutinas activas (LevantarseTrasHitstun, SecuenciaLanzado, etc.)
        // ya usarán el animator restaurado en sus siguientes PlayAnim.
        if (estadoActual == EstadoJugador.Hitstun ||
            estadoActual == EstadoJugador.Derribado ||
            estadoActual == EstadoJugador.Agarrado)
            return;

        CambiarEstado(EstadoJugador.Idle);
        contextoActual = ContextoPersonaje.Suelo;
        PlayAnim(animIdle);
    }

    // Llamadas desde RoundManager

    public void BloquearInputRonda()
    {
        _juegoTerminado = true;
        StopAllCoroutines();
        _comandoEnEjecucion = false;
        _corrutinaDash = null;
        SetAllHitboxes(false);
        rb.linearVelocity = Vector3.zero;
    }

    public void MorirDefinitivamente()
    {
        _juegoTerminado = true;

        // Restaurar forma normal siempre, independientemente de si hay modeloWinRar o no,
        // para que animDead se ejecute sobre el modelo original.
        if (_enEstadoWinRar)
        {
            if (modeloWinRar != null)
            {
                modeloWinRar.SetActive(false);
                if (modeloHijo != null) OcultarRenderers(modeloHijo, false);
                if (_animatorOriginalWinRar != null)
                {
                    animator = _animatorOriginalWinRar;
                    animator.enabled = true;
                }
                _animatorOriginalWinRar = null;
            }
            else if (_controladorOriginalWinRar != null && animator != null)
            {
                animator.runtimeAnimatorController = _controladorOriginalWinRar;
            }
        }

        _enEstadoWinRar = false;
        _tiempoFinWinRar = -1f;
        _controladorOriginalWinRar = null;

        ResetearContadorComboSilencioso();
        StopAllCoroutines();
        _corrutinaHitstop = null;

        // Si habia un hitstop activo, el animator quedo congelado a speed=0.
        // Hay que descongelarlo ANTES de reproducir la animacion de muerte.
        if (animator != null) animator.speed = 1f;

        _comandoEnEjecucion = false;
        _corrutinaDash = null;
        SetAllHitboxes(false);
        LimpiarEstadoAgarrador();
        LimpiarEstadoAgarrado();
        rb.linearVelocity = Vector3.zero;
        estaDerribado = true;
        CambiarEstado(EstadoJugador.Derribado);
        PlayAnim(animDead);

        // Forzar un Update del Animator en este mismo frame para asegurarnos de
        // que el estado Dead se aplica incluso si habia una transicion pendiente.
        if (animator != null)
            animator.Update(0f);

        // Reproducir sonido de muerte de ronda asignado al personaje
        if (datosPersonaje != null && datosPersonaje.sonidoMuerteRonda != null && _soundPlayer != null)
            _soundPlayer.audioSource?.PlayOneShot(datosPersonaje.sonidoMuerteRonda);

        // Corrutina de seguridad: si por alguna razon el animator vuelve a speed=0
        // (p.ej. interferencia externa), lo restaura al siguiente frame.
        StartCoroutine(MantenerAnimacionMuerte());
    }

    private IEnumerator MantenerAnimacionMuerte()
    {
        yield return null;

        while (_juegoTerminado)
        {
            if (animator != null)
            {
                if (animator.speed == 0f) animator.speed = 1f;
                AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
                if (!info.IsName(animDead))
                    PlayAnim(animDead);
            }
            yield return null;
        }
    }


    public void AnimarVictoria()
    {
        _juegoTerminado = true;
        ResetearContadorComboSilencioso();
        StopAllCoroutines();
        _corrutinaDash = null;
        SetAllHitboxes(false);
        LimpiarEstadoAgarrador();
        LimpiarEstadoAgarrado();
        rb.linearVelocity = Vector3.zero;
        CambiarEstado(EstadoJugador.Idle);
        PlayAnim(animWin);
    }

    public void AnimarVictoriaFinal(Transform spawnPoint)
    {
        _juegoTerminado = true;
        ResetearContadorComboSilencioso();
        StopAllCoroutines();
        _corrutinaDash = null;
        SetAllHitboxes(false);
        LimpiarEstadoAgarrador();
        LimpiarEstadoAgarrado();
        rb.linearVelocity = Vector3.zero;
        CambiarEstado(EstadoJugador.Idle);
        PlayAnim(animWin);

        var data = CharacterSelectionData.Instance;
        if (data == null) return;

        int playerNumber = 0;
        if (gameObject.name.StartsWith("Player1")) playerNumber = 1;
        else if (gameObject.name.StartsWith("Player2")) playerNumber = 2;
        if (playerNumber == 0) return;

        // BUG FIX #2: guardar el ganador para que VictoryScreenManager lo lea correctamente.
        // Sin esto winnerPlayerNumber queda en 0 y la pantalla siempre muestra "J2".
        data.winnerPlayerNumber = playerNumber;

        GameObject prefab = data.GetPrefabVictoriaFinal(playerNumber);
        if (prefab != null)
        {
            Vector3 pos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
            Quaternion rot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

            // BUG FIX #1: guardar la instancia y pasarle el número de jugador al VictorySkinApplier
            // ANTES de que su Start() se ejecute. Sin esto, cuando ambos jugadores eligen el mismo
            // personaje la auto-detección falla (devuelve 0) y la skin nunca se aplica.
            GameObject instance = Instantiate(prefab, pos, rot);
            var skinApplier = instance.GetComponent<VictorySkinApplier>();
            if (skinApplier != null)
                skinApplier.SetPlayerNumber(playerNumber);
        }
    }

    public void ReiniciarParaNuevaRonda()
    {
        _juegoTerminado = false;
        _dobleSaltoUsado = false;
        estaDerribado = false;
        _enVueloLanzado = false;
        _reboteSuelo = false;
        corriendo = false;
        yaAtacoEnAire = false;
        enSuelo = true;
        hitboxesActuales = null;
        bufferAgacharse = false;
        bufferSalto = false;
        _enClash = false;

        _dashAereoUsado = false;
        _comandoEnEjecucion = false;
        _dasheandoAdelante = false;
        _dashActualCancelableAAtaque = false;
        _bufferDash = false;
        ultimoPulsoAtras = -999f;

        if (_enEstadoWinRar)
        {
            if (modeloWinRar != null)
            {
                modeloWinRar.SetActive(false);
                if (modeloHijo != null) OcultarRenderers(modeloHijo, false);
                if (_animatorOriginalWinRar != null)
                {
                    animator = _animatorOriginalWinRar;
                    animator.enabled = true;
                }
                _animatorOriginalWinRar = null;
            }
            else if (_controladorOriginalWinRar != null && animator != null)
                animator.runtimeAnimatorController = _controladorOriginalWinRar;
        }
        _enEstadoWinRar = false;
        _tiempoFinWinRar = -1f;
        _controladorOriginalWinRar = null;

        _tPrimerQCFUltimate = -999f;
        _tSegundoQCFUltimate = -999f;

        ResetearContadorComboSilencioso();
        StopAllCoroutines();
        if (animator != null) animator.speed = 1f;
        _corrutinaDash = null;
        SetAllHitboxes(false);
        LimpiarEstadoAgarrador();
        LimpiarEstadoAgarrado();
        ResetearBloqueo();
        LimpiarQCF();
        LimpiarQCB();
        _tAtrasParaMCF = -999f;
        _tAdelanteParaMCB = -999f;
        _tUltimoAbajoSoltado = -999f;
        _tDobleAbajoCompleto = -999f;
        _tQCFCompletadoParaInverso = -999f;
        _ataqueActivoID = AtaqueID.Ninguno;
        if (rb != null) rb.linearVelocity = Vector3.zero;
        CambiarEstado(EstadoJugador.Idle);
        contextoActual = ContextoPersonaje.Suelo;
        PlayAnim(animIdle);
        OnReiniciarCallback();
    }

    public void ReiniciarUltimatePartida()
    {
        barraUltimate?.ReiniciarUltimate();
    }

    public void IntentarBloqueoProximidad() { }

    // Metodos virtuales para sobreescribir en hijos

    protected virtual IEnumerator EjecutarCommand1()
    {
        IniciarComando(); FinalizarComando(); yield break;
    }

    protected virtual IEnumerator EjecutarCommand2()
    {
        IniciarComando(); FinalizarComando(); yield break;
    }

    protected virtual IEnumerator EjecutarCommand3()
    {
        IniciarComando(); FinalizarComando(); yield break;
    }

    protected virtual IEnumerator EjecutarComandoQCF(int boton)
    {
        IniciarComando(); FinalizarComando(); yield break;
    }

    protected virtual IEnumerator EjecutarUltimate()
    {
        IniciarComando(); FinalizarComando(); yield break;
    }

    public void TriggerUltimate()
    {
        if (_juegoTerminado) return;
        if (_enEstadoWinRar) return;
        if (_enBlockstun) return;
        if (barraUltimate == null || barraUltimate.NivelActual < stocksNecesariosUltimate) return;
        if (estadoActual != EstadoJugador.Idle
         && estadoActual != EstadoJugador.Caminar
         && estadoActual != EstadoJugador.Corriendo) return;

        _ultimateGolpeo = false;
        StartCoroutine(EjecutarUltimate());
    }

    protected bool GastarStocksUltimate()
    {
        if (barraUltimate == null) return false;
        return barraUltimate.GastarStocks(stocksNecesariosUltimate);
    }

    protected virtual bool PuedeEjecutarCommand1() => true;
    protected virtual bool PuedeEjecutarCommand2() => true;
    protected virtual bool PuedeEjecutarCommand3() => true;
    protected virtual bool PuedeEjecutarComandoQCF(int b) => true;
    protected virtual bool PuedeEjecutarAtaqueNormal(int b) => true;

    protected virtual void OnFinAtaqueCallback() { }
    protected virtual void OnVolverTrasAtaqueCallback() { }
    protected virtual void OnRecibirGolpeCallback() { }
    protected virtual bool OnComprobarEscudo() { return false; }
    protected virtual void OnReiniciarCallback() { }

    // Ciclo de vida de Unity

    protected virtual void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            Debug.LogError("[PersonajeBase] PlayerInput no encontrado en " + gameObject.name +
                ". Debe estar en el MISMO GameObject. El personaje no respondera a inputs.");
            return;
        }

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (modeloHijo == null && animator != null) modeloHijo = animator.transform;

        playerInput.actions.FindActionMap("Gameplay")?.Enable();

        try
        {
            moverDerecha = playerInput.actions["MoverDerecha"];
            moverIzquierda = playerInput.actions["MoverIzquierda"];
            saltarAction = playerInput.actions["Saltar"];
            agacharseAction = playerInput.actions["Agacharse"];
            punoFlojoAction = playerInput.actions["PunetazoFlojo"];
            punoFuerteAction = playerInput.actions["PunetazoFuerte"];
            patadaFlojaAction = playerInput.actions["PatadaFlojo"];
            patadaFuerteAction = playerInput.actions["PatadaFuerte"];
            agarreAction = playerInput.actions["Agarre"];
        }
        catch (System.Exception e)
        {
            Debug.LogError("[PersonajeBase] Error al buscar acciones de input: " + e.Message);
            return;
        }

        moverDerecha.performed += ctx => { movimientoHorizontal = 1f; _ultimaDireccionHorizontal = 1f; _tUltimaDireccionHorizontal = Time.time; adelantePresionado = EsAdelante(1f); OnDireccionPulsada(1f); };
        moverDerecha.canceled += ctx => { movimientoHorizontal = 0f; adelantePresionado = false; PararCorrer(); OnDireccionSoltada(); };
        moverIzquierda.performed += ctx => { movimientoHorizontal = -1f; _ultimaDireccionHorizontal = -1f; _tUltimaDireccionHorizontal = Time.time; adelantePresionado = EsAdelante(-1f); OnDireccionPulsada(-1f); };
        moverIzquierda.canceled += ctx => { movimientoHorizontal = 0f; adelantePresionado = false; PararCorrer(); OnDireccionSoltada(); };

        agacharseAction.performed += ctx => { abajoPresionado = true; OnAbajoPulsado(); };
        agacharseAction.canceled += ctx => { abajoPresionado = false; OnAbajoSoltado(); };

        saltarAction.performed += ctx => Saltar();
        punoFlojoAction.performed += ctx => OnBotonAtaque(0);
        punoFuerteAction.performed += ctx => OnBotonAtaque(1);
        patadaFlojaAction.performed += ctx => OnBotonAtaque(2);
        patadaFuerteAction.performed += ctx => OnBotonAtaque(3);

        punoFlojoAction.canceled += ctx => OnBotonAtaqueSoltado(0);
        punoFuerteAction.canceled += ctx => OnBotonAtaqueSoltado(1);
        patadaFlojaAction.canceled += ctx => OnBotonAtaqueSoltado(2);
        patadaFuerteAction.canceled += ctx => OnBotonAtaqueSoltado(3);

        agarreAction.performed += ctx => IniciarAgarre();

        salud = GetComponent<SaludJugador>();
        if (salud == null)
            Debug.LogWarning("[PersonajeBase] No se encontro SaludJugador en " + gameObject.name);
    }

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        SetAllHitboxes(false);
        _soundPlayer = GetComponentInChildren<AnimationSoundPlayer>();

        if (datosPersonaje == null)
            Debug.LogError("[PersonajeBase] FALTA FighterData en " + gameObject.name);

        if (modeloHijo != null)
            escalaOriginalModelo = modeloHijo.localScale;

        foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            smr.updateWhenOffscreen = true;

        if (oponente != null)
            InicializarColisionesConOponente();

        if (hurtboxCollider != null)
        {
            if (hurtboxCollider is BoxCollider hbBox)
            {
                _hurtboxSizeOriginal = hbBox.size;
                _centroColisionOriginal = hbBox.center;
            }
            else if (hurtboxCollider is CapsuleCollider hbCap)
            {
                _hurtboxSizeOriginal = new Vector3(hbCap.radius, hbCap.height, hbCap.radius);
                _centroColisionOriginal = hbCap.center;
            }
        }

        if (hurtboxAgachadoCollider != null)
            hurtboxAgachadoCollider.enabled = false;

        if (modeloWinRar != null)
            modeloWinRar.SetActive(false);

        if (colliderBloqueoProximidad != null)
            _proxBloqueoLocalPosOriginal = colliderBloqueoProximidad.transform.localPosition;

        OcultarUIComboInmediatamente();
    }

    // Colision dinamica suelo/aire

    void InicializarColisionesConOponente()
    {
        Collider[] propios = GetComponentsInChildren<Collider>();
        Collider[] ajenos = oponente.GetComponentsInChildren<Collider>();
        var propiosCuerpo = new System.Collections.Generic.List<Collider>();
        var ajenosCuerpo = new System.Collections.Generic.List<Collider>();

        foreach (var c in propios) if (!c.isTrigger) propiosCuerpo.Add(c);
        foreach (var c in ajenos) if (!c.isTrigger) ajenosCuerpo.Add(c);

        _collidersCuerpoPropio = propiosCuerpo.ToArray();
        _collidersCuerpoOponente = ajenosCuerpo.ToArray();
        _oponenteBase = oponente.GetComponent<PersonajeBase>();

        foreach (var propio in propios)
            foreach (var ajeno in ajenos)
                if (propio.isTrigger || ajeno.isTrigger)
                    Physics.IgnoreCollision(propio, ajeno, true);

        _colisionActivaActual = true;
        AplicarColisionCuerpo(true);
    }

    void AplicarColisionCuerpo(bool colisionar)
    {
        if (_collidersCuerpoPropio == null || _collidersCuerpoOponente == null) return;
        foreach (var propio in _collidersCuerpoPropio)
            foreach (var ajeno in _collidersCuerpoOponente)
                if (propio != null && ajeno != null)
                    Physics.IgnoreCollision(propio, ajeno, !colisionar);
    }

    void ActualizarColisionConOponente()
    {
        if (_collidersCuerpoPropio == null || _collidersCuerpoOponente == null) return;

        bool oponenteEnAire = _oponenteBase != null && !_oponenteBase.enSuelo;
        bool debeColisionar = enSuelo && !oponenteEnAire;

        if (debeColisionar == _colisionActivaActual) return;
        _colisionActivaActual = debeColisionar;
        AplicarColisionCuerpo(debeColisionar);
    }

    void FixedUpdate()
    {
        if (_juegoTerminado) return;
        if (datosPersonaje == null) return;
        AplicarGravedadExtra();
        MoverPersonaje();
        ActualizarOrientacion();
        ActualizarAnimacionMovimiento();
        ActualizarColisionConOponente();
        if (!enSuelo) ComprobarSueloPorRaycast();
    }

    void ComprobarSueloPorRaycast()
    {
        if (Time.time - tiempoSalto < minTiempoEnAire) return;
        if (rb.linearVelocity.y > 0.5f) return;

        bool tocaSuelo = Physics.Raycast(
            transform.position + Vector3.up * 0.15f,
            Vector3.down,
            0.45f,
            maskaSuelo,
            QueryTriggerInteraction.Ignore);

        if (tocaSuelo) Aterrizar();
    }

    void Update()
    {
        if (_juegoTerminado) return;   //ningún sistema interfiere tras la muerte
        if (animator == null) return;
        CorregirAnimEscapada();

        if (_enEstadoWinRar && Time.time >= _tiempoFinWinRar)
            SalirEstadoWinRar();

        ComprobarAtascadoEnAire();
    }

    void ComprobarAtascadoEnAire()
    {
        if (enSuelo) return;
        if (estadoActual != EstadoJugador.Saltar && estadoActual != EstadoJugador.Hitstun) return;
        if (Time.time - tiempoSalto < 0.6f) return;
        if (Mathf.Abs(rb.linearVelocity.y) > 0.8f) return;

        // Raycast largo como red de seguridad
        bool tocaSuelo = Physics.Raycast(
            transform.position + Vector3.up * 0.3f,
            Vector3.down,
            1.0f,
            maskaSuelo,
            QueryTriggerInteraction.Ignore);

        if (tocaSuelo) Aterrizar();
    }

    // Comandos y deteccion de input

    void OnAbajoPulsado()
    {
        // Doble-abajo (↓↓): confirmar si se pulsó abajo recientemente tras soltar
        if (_tUltimoAbajoSoltado > -999f && Time.time - _tUltimoAbajoSoltado <= ventanaQCF)
            _tDobleAbajoCompleto = Time.time;

        if (!adelantePresionado)
        {
            tAbajoOn = Time.time;
            tAdelanteOn = -999f;
        }
        tAbajoOnQCB = Time.time;
        tAtrasOn = -999f;

        if (estadoActual == EstadoJugador.Atacando || estadoActual == EstadoJugador.Comando)
        {
            bufferAgacharse = true;
            return;
        }

        if (_enBlockstun) return;
        IniciarAgacharse();
    }

    void OnAbajoSoltado()
    {
        _tUltimoAbajoSoltado = Time.time;
        tAbajoOff = Time.time;
        tAbajoOffQCB = Time.time;
        bufferAgacharse = false;
        if (_enBlockstun) return;
        SoltarAgacharse();
    }

    void OnDireccionPulsada(float dir)
    {
        if (_juegoTerminado) return;

        if (EsAdelante(dir))
        {
            if (!enSuelo && puedeDashAire && !_dashAereoUsado && !_enEstadoWinRar)
            {
                float ahora = Time.time;
                if (ultimoPulsoAdelante > -999f && ahora - ultimoPulsoAdelante <= ventanaDoubleTap)
                {
                    ultimoPulsoAdelante = -999f;
                    TriggerDash(adelante: true, aereo: true);
                }
                else
                {
                    ultimoPulsoAdelante = ahora;
                }
            }

            if (enSuelo)
                ComprobarDoubleTap(dir);

            // Tracking para MCAtras: registrar cuándo se pulsó adelante
            _tAdelanteParaMCB = Time.time;

            bool abajoReciente = abajoPresionado ||
                (tAbajoOff > -999f && Time.time - tAbajoOff <= graciaAbajo);
            if (abajoReciente && tAbajoOn > -999f && Time.time - tAbajoOn <= ventanaQCF)
            {
                tAdelanteOn = Time.time;
                // QCF+QCFInverso: registrar que QCF se completó
                _tQCFCompletadoParaInverso = Time.time;
                if (_tPrimerQCFUltimate > -999f && Time.time - _tPrimerQCFUltimate <= ventanaDobleQCFUltimate)
                    _tSegundoQCFUltimate = Time.time;
                else
                    _tPrimerQCFUltimate = Time.time;
            }

            tAbajoOnQCB = -999f;
            tAtrasOn = -999f;
            tAbajoOffQCB = -999f;
        }
        else
        {
            // Tracking para MCF: registrar cuándo se pulsó atrás
            _tAtrasParaMCF = Time.time;
            if (!_enEstadoWinRar)
            {
                float ahora = Time.time;
                if (ultimoPulsoAtras > -999f && ahora - ultimoPulsoAtras <= ventanaDoubleTap)
                {
                    ultimoPulsoAtras = -999f;
                    if (enSuelo)
                        TriggerDash(adelante: false, aereo: false);
                    else if (puedeDashAire && !_dashAereoUsado)
                        TriggerDash(adelante: false, aereo: true);
                }
                else
                {
                    ultimoPulsoAtras = ahora;
                }
            }

            bool abajoRecienteQCB = abajoPresionado ||
                (tAbajoOffQCB > -999f && Time.time - tAbajoOffQCB <= graciaAbajo);
            if (abajoRecienteQCB && tAbajoOnQCB > -999f && Time.time - tAbajoOnQCB <= ventanaQCF)
                tAtrasOn = Time.time;

            tAbajoOn = -999f;
            tAdelanteOn = -999f;
            tAbajoOff = -999f;
            PararCorrer();
        }
    }

    void OnDireccionSoltada() { }

    bool QCFCompleto()
    {
        float ahora = Time.time;
        return tAbajoOn > -999f
            && tAdelanteOn > tAbajoOn
            && ahora - tAbajoOn <= ventanaQCF
            && ahora - tAdelanteOn <= ventanaQCF;
    }

    bool QCBCompleto()
    {
        float ahora = Time.time;
        return tAbajoOnQCB > -999f
            && tAtrasOn > tAbajoOnQCB
            && ahora - tAbajoOnQCB <= ventanaQCF
            && ahora - tAtrasOn <= ventanaQCF;
    }

    // Tech

    void RegistrarBotonParaTech(int boton)
    {
        if (!enVentanaTech) return;
        int bit = 1 << boton;
        if ((_techBotonesPresionados & bit) != 0) return;
        _techBotonesPresionados |= bit;
        int cuenta = ContarBits(_techBotonesPresionados);
        if (cuenta >= 2 && _techDosBotonesTiempo < 0f)
            _techDosBotonesTiempo = Time.time;
    }

    void OnBotonAtaqueSoltado(int boton)
    {
        int bit = 1 << boton;
        _techBotonesPresionados &= ~bit;
        if (ContarBits(_techBotonesPresionados) < 2)
            _techDosBotonesTiempo = -1f;
    }

    static int ContarBits(int v)
    {
        int c = 0;
        while (v != 0) { c += v & 1; v >>= 1; }
        return c;
    }

    bool TechConfirmado()
    {
        if (!enVentanaTech) return false;
        if (_techDosBotonesTiempo < 0f) return false;
        if (Time.time - _techDosBotonesTiempo < techTiempoMantener) return false;
        if (movimientoHorizontal == 0f) return false;
        return true;
    }

    protected void ResetearTech()
    {
        enVentanaTech = false;
        _techBotonesPresionados = 0;
        _techDosBotonesTiempo = -1f;
    }

    // Input de boton de ataque

    void OnBotonAtaque(int boton)
    {
        if (_juegoTerminado) return;
        if (_enEstadoWinRar) return;
        if (enVentanaTech)
        {
            RegistrarBotonParaTech(boton);
            return;
        }
        if (_enBlockstun) return;

        // ── Garantía: en Idle nunca deben quedar flags residuales que bloqueen ataques ──
        if (estadoActual == EstadoJugador.Idle)
        {
            _comandoEnEjecucion = false;
            ataqueEsCancelable = false;
            _ataqueHaConectado = false;
            bufferAtaque = -1;
            _bufferDash = false;
        }

        // ... resto del método sin cambios

        if (estadoActual == EstadoJugador.Dasheando)
        {
            if (_dashActualCancelableAAtaque)
            {
                // Guardar la velocidad horizontal del dash ANTES de que Atacar() la ponga a cero,
                // para reinyectarla después y que el personaje siga con el momentum del dash.
                float momentumDash = rb.linearVelocity.z;

                if (_corrutinaDash != null) { StopCoroutine(_corrutinaDash); _corrutinaDash = null; }
                if (!enSuelo)
                {
                    CambiarEstado(EstadoJugador.Saltar);
                    contextoActual = ContextoPersonaje.Aire;
                }
                else
                {
                    CambiarEstado(EstadoJugador.Idle);
                    contextoActual = ContextoPersonaje.Suelo;
                }
                EjecutarAtaqueNormal(boton);

                // Restaurar el momentum: Atacar() ya zeroeó la velocidad Z en suelo,
                // así que lo sobreescribimos aquí con el valor guardado.
                if (estadoActual == EstadoJugador.Atacando)
                    rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, momentumDash);
            }
            else
            {
                bufferAtaque = boton;
                bufferAtaqueTiempo = Time.time;
            }
            return;
        }

        if (estadoActual == EstadoJugador.Atacando)
        {
            if (_comandoEnEjecucion)
            {
                bufferAtaque = boton;
                bufferAtaqueTiempo = Time.time;
                return;
            }
            if (_ataqueHaConectado && ataqueEsCancelable)
            {
                if (datosPersonaje != null)
                {
                    AtaqueID cancelTargets = datosPersonaje.ObtenerCanceles(_ataqueActivoID);
                    if (cancelTargets != AtaqueID.Ninguno)
                    {
                        // FIX cancel a comando: enriquecer el ID destino con el comando que
                        // correspondería a la secuencia activa, para que el cancel check no
                        // bloquee inputs que van a disparar un comando (p.ej. QCB+PuñoFlojo).
                        AtaqueID ataqueDestino = BotonConSecuenciaAAtaqueID(boton, contextoActual);
                        if ((cancelTargets & ataqueDestino) == 0)
                        {
                            bufferAtaque = boton;
                            bufferAtaqueTiempo = Time.time;
                            return;
                        }
                    }
                }
                bufferAtaque = boton;
                bufferAtaqueTiempo = Time.time;
                ataqueEsCancelable = false;
                CancelarAtaqueActual();
                return;
            }

            bufferAtaque = boton;
            bufferAtaqueTiempo = Time.time;
            return;
        }

        // la ventana de cancel del Command2 tiene prioridad sobre el buffer de Comando
        // para que el boton pulsado llegue aqui incluso estando en estado Comando
        if (enVentanaCancelCommand2)
        {
            command2BotonCancel = boton;
            command2CancelUsado = true;
            return;
        }

        if (estadoActual == EstadoJugador.Comando)
        {
            bufferAtaque = boton;
            bufferAtaqueTiempo = Time.time;
            return;
        }

        // ── Deteccion de comandos (data-driven) ────────────────────────────────
        // Si datosPersonaje esta asignado, usa ConfiguracionComando del asset.
        // En caso contrario, fallback al comportamiento original hardcodeado.
        if (datosPersonaje != null)
        {
            // Command3 — suelo y aire
            var c3 = datosPersonaje.secuenciaCommand3;
            if (SecuenciaCompleta(c3.secuencia)
                && (c3.botonActivacion < 0 || c3.botonActivacion == boton)
                && PuedeEjecutarCommand3()
                && (estadoActual == EstadoJugador.Idle || estadoActual == EstadoJugador.Corriendo
                 || estadoActual == EstadoJugador.Agachado || estadoActual == EstadoJugador.Saltar))
            {
                LimpiarSecuencia(c3.secuencia);
                StartCoroutine(EjecutarCommand3());
                return;
            }

            // Command2 — solo suelo
            var c2 = datosPersonaje.secuenciaCommand2;
            if (enSuelo && SecuenciaCompleta(c2.secuencia)
                && (c2.botonActivacion < 0 || c2.botonActivacion == boton)
                && PuedeEjecutarCommand2()
                && (estadoActual == EstadoJugador.Idle || estadoActual == EstadoJugador.Corriendo
                 || estadoActual == EstadoJugador.Agachado))
            {
                LimpiarSecuencia(c2.secuencia);
                StartCoroutine(EjecutarCommand2());
                return;
            }

            // Command1 — solo suelo
            var c1 = datosPersonaje.secuenciaCommand1;
            if (enSuelo && SecuenciaCompleta(c1.secuencia)
                && (c1.botonActivacion < 0 || c1.botonActivacion == boton)
                && PuedeEjecutarCommand1()
                && (estadoActual == EstadoJugador.Idle || estadoActual == EstadoJugador.Corriendo
                 || estadoActual == EstadoJugador.Agachado))
            {
                LimpiarSecuencia(c1.secuencia);
                StartCoroutine(EjecutarCommand1());
                return;
            }

            // Ultimate — BotonActivadorUltimate mantiene compatibilidad con overrides virtuales
            if (boton == BotonActivadorUltimate
                && SecuenciaCompleta(datosPersonaje.secuenciaUltimate.secuencia)
                && !_enEstadoWinRar
                && barraUltimate != null && barraUltimate.NivelActual >= stocksNecesariosUltimate
                && (estadoActual == EstadoJugador.Idle || estadoActual == EstadoJugador.Caminar
                 || estadoActual == EstadoJugador.Corriendo))
            {
                LimpiarSecuencia(datosPersonaje.secuenciaUltimate.secuencia);
                TriggerUltimate();
                return;
            }

            // Comando QCF generico
            var cqcf = datosPersonaje.secuenciaCommandQCF;
            if (SecuenciaCompleta(cqcf.secuencia)
                && (cqcf.botonActivacion < 0 || cqcf.botonActivacion == boton)
                && PuedeEjecutarComandoQCF(boton)
                && (estadoActual == EstadoJugador.Idle || estadoActual == EstadoJugador.Corriendo
                 || estadoActual == EstadoJugador.Agachado || estadoActual == EstadoJugador.Saltar))
            {
                LimpiarSecuencia(cqcf.secuencia);
                StartCoroutine(EjecutarComandoQCF(boton));
                return;
            }
        }
        else
        {
            // Fallback hardcodeado (comportamiento original) cuando no hay data asset
            if (QCBCompleto() && boton == 3
                && PuedeEjecutarCommand3()
                && (estadoActual == EstadoJugador.Idle || estadoActual == EstadoJugador.Corriendo
                 || estadoActual == EstadoJugador.Agachado || estadoActual == EstadoJugador.Saltar))
            {
                LimpiarQCB(); StartCoroutine(EjecutarCommand3()); return;
            }
            if (QCBCompleto() && enSuelo
                && (estadoActual == EstadoJugador.Idle || estadoActual == EstadoJugador.Corriendo
                 || estadoActual == EstadoJugador.Agachado))
            {
                switch (boton)
                {
                    case 0: if (!PuedeEjecutarCommand1()) return; LimpiarQCB(); StartCoroutine(EjecutarCommand1()); return;
                    case 1: if (!PuedeEjecutarCommand2()) return; LimpiarQCB(); StartCoroutine(EjecutarCommand2()); return;
                }
            }
            if (boton == BotonActivadorUltimate && _tSegundoQCFUltimate > -999f
                && Time.time - _tSegundoQCFUltimate <= ventanaDobleQCFUltimate
                && !_enEstadoWinRar
                && barraUltimate != null && barraUltimate.NivelActual >= stocksNecesariosUltimate
                && (estadoActual == EstadoJugador.Idle || estadoActual == EstadoJugador.Caminar
                 || estadoActual == EstadoJugador.Corriendo))
            {
                _tPrimerQCFUltimate = -999f; _tSegundoQCFUltimate = -999f; LimpiarQCF();
                TriggerUltimate(); return;
            }
            if (QCFCompleto() && PuedeEjecutarComandoQCF(boton)
                && (estadoActual == EstadoJugador.Idle || estadoActual == EstadoJugador.Corriendo
                 || estadoActual == EstadoJugador.Agachado || estadoActual == EstadoJugador.Saltar))
            {
                LimpiarQCF(); StartCoroutine(EjecutarComandoQCF(boton)); return;
            }
        }
        EjecutarAtaqueNormal(boton);
    }

    // Ataques normales
    protected virtual void EjecutarAtaqueNormal(int boton)
    {
        if (datosPersonaje == null) return;
        switch (boton)
        {
            case 0: Atacar(hitboxesPunoFlojo, ObtenerAnimAtaque(datosPersonaje.animPunoFlojoSuelo, datosPersonaje.animPunoFlojoAgachado, datosPersonaje.animPunoFlojoAire)); break;
            case 1: Atacar(hitboxesPunoFuerte, ObtenerAnimAtaque(datosPersonaje.animPunoFuerteSuelo, datosPersonaje.animPunoFuerteAgachado, datosPersonaje.animPunoFuerteAire)); break;
            case 2: Atacar(hitboxesPatadaFloja, ObtenerAnimAtaque(datosPersonaje.animPatadaFlojoSuelo, datosPersonaje.animPatadaFlojoAgachado, datosPersonaje.animPatadaFlojoAire)); break;
            case 3: Atacar(hitboxesPatadaFuerte, ObtenerAnimAtaque(datosPersonaje.animPatadaFuerteSuelo, datosPersonaje.animPatadaFuerteAgachado, datosPersonaje.animPatadaFuerteAire)); break;
        }
    }

    protected void LimpiarQCF()
    {
        tAbajoOn = -999f; tAdelanteOn = -999f; tAbajoOff = -999f;
        _tPrimerQCFUltimate = -999f; _tSegundoQCFUltimate = -999f;
        _tQCFCompletadoParaInverso = -999f;
    }
    protected void LimpiarQCB()
    {
        tAbajoOnQCB = -999f; tAtrasOn = -999f; tAbajoOffQCB = -999f;
    }

    protected bool SecuenciaCompleta(TipoSecuencia tipo)
    {
        switch (tipo)
        {
            case TipoSecuencia.QCF: return QCFCompleto();
            case TipoSecuencia.QCB: return QCBCompleto();
            case TipoSecuencia.DobleAbajo: return DobleAbajoCompleto();
            case TipoSecuencia.MedioCirculoAdelante: return MedioCirculoAdelanteCompleto();
            case TipoSecuencia.MedioCirculoAtras: return MedioCirculoAtrasCompleto();
            case TipoSecuencia.DobleQCF:
                return _tSegundoQCFUltimate > -999f
                    && Time.time - _tSegundoQCFUltimate <= ventanaDobleQCFUltimate;
            case TipoSecuencia.QCFmasQCFInverso: return QCFmasQCFInversoCompleto();
            default: return false;
        }
    }

    protected void LimpiarSecuencia(TipoSecuencia tipo)
    {
        switch (tipo)
        {
            case TipoSecuencia.QCF:
                LimpiarQCF(); break;
            case TipoSecuencia.QCB:
                LimpiarQCB(); break;
            case TipoSecuencia.DobleAbajo:
                _tDobleAbajoCompleto = -999f; break;
            case TipoSecuencia.MedioCirculoAdelante:
                _tAtrasParaMCF = -999f; LimpiarQCF(); break;
            case TipoSecuencia.MedioCirculoAtras:
                _tAdelanteParaMCB = -999f; LimpiarQCB(); break;
            case TipoSecuencia.DobleQCF:
                _tPrimerQCFUltimate = -999f; _tSegundoQCFUltimate = -999f; LimpiarQCF(); break;
            case TipoSecuencia.QCFmasQCFInverso:
                _tQCFCompletadoParaInverso = -999f; LimpiarQCF(); LimpiarQCB(); break;
        }
    }

    bool DobleAbajoCompleto()
    {
        return _tDobleAbajoCompleto > -999f
            && Time.time - _tDobleAbajoCompleto <= ventanaQCF;
    }

    // ← ↙ ↓ ↘ → : atrás pulsado, luego abajo, luego adelante
    // Reutiliza _tAtrasParaMCF + los timestamps QCF existentes (tAbajoOn, tAdelanteOn).
    bool MedioCirculoAdelanteCompleto()
    {
        float ahora = Time.time;
        return _tAtrasParaMCF > -999f
            && tAbajoOn > _tAtrasParaMCF       // abajo pulsado DESPUES de atrás
            && tAdelanteOn > tAbajoOn           // adelante pulsado DESPUES de abajo
            && ahora - _tAtrasParaMCF <= ventanaQCF * 2f
            && ahora - tAdelanteOn <= ventanaQCF;
    }

    // → ↘ ↓ ↙ ← : adelante pulsado, luego abajo, luego atrás
    // Reutiliza _tAdelanteParaMCB + los timestamps QCB existentes (tAbajoOnQCB, tAtrasOn).
    bool MedioCirculoAtrasCompleto()
    {
        float ahora = Time.time;
        return _tAdelanteParaMCB > -999f
            && tAbajoOnQCB > _tAdelanteParaMCB  // abajo pulsado DESPUES de adelante
            && tAtrasOn > tAbajoOnQCB            // atrás pulsado DESPUES de abajo
            && ahora - _tAdelanteParaMCB <= ventanaQCF * 2f
            && ahora - tAtrasOn <= ventanaQCF;
    }

    // QCF completado, luego QCB completado dentro de la ventana
    bool QCFmasQCFInversoCompleto()
    {
        return _tQCFCompletadoParaInverso > -999f
            && QCBCompleto()
            && Time.time - _tQCFCompletadoParaInverso <= ventanaQCF * 2f;
    }

    // Infraestructura de comandos

    protected void IniciarComando()
    {
        _comandoEnEjecucion = true;
        _ataqueGeneracion++;
        ResetearBloqueo();
        corriendo = false;
        hitboxesActuales = null;
        ataqueEsCancelable = false;
        _ataqueHaConectado = false;
        CambiarEstado(EstadoJugador.Comando);
        contextoActual = ContextoPersonaje.Suelo;
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }

    protected void FinalizarComando()
    {
        _comandoEnEjecucion = false;
        hitboxesActuales = null;
        if (!enSuelo)
        {
            CambiarEstado(EstadoJugador.Saltar);
            PlayAnim(animAire);
        }
        else
        {
            CambiarEstado(EstadoJugador.Idle);
            PlayAnim(animIdle);
            EjecutarBufferAtaque();
            EjecutarBufferAgacharse();
            EjecutarBufferSalto();
        }
    }

    protected IEnumerator ActivarHitboxesConDano(GameObject[] hitboxes, float dano, float duracion,
        Vector3? escala = null, Vector3? offset = null)
    {
        if (hitboxes == null) yield break;
        foreach (var hb in hitboxes)
        {
            if (hb == null) continue;
            Hitbox h = hb.GetComponent<Hitbox>();
            if (h != null) h.SetDano(dano);
            AjustarHitbox(hb, escala, offset);
            hb.SetActive(true);
        }
        yield return new WaitForSeconds(duracion);
        foreach (var hb in hitboxes)
            if (hb != null) hb.SetActive(false);
    }

    // Fisica y animacion

    void CorregirAnimEscapada()
    {
        if (_juegoTerminado) return;
        if (_enBlockstun) return;
        if (_enEstadoWinRar) return;

        if (estadoActual == EstadoJugador.Atacando
         || estadoActual == EstadoJugador.Hitstun
         || estadoActual == EstadoJugador.Comando
         || estadoActual == EstadoJugador.Dizzy
         || estadoActual == EstadoJugador.Derribado
         || estadoActual == EstadoJugador.Agarrado
         || estadoActual == EstadoJugador.Dasheando) return;

        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
        AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);
        bool enTransicion = animator.IsInTransition(0);

        string animCorrecto = null;
        switch (estadoActual)
        {
            case EstadoJugador.Idle:
                if (!current.IsName(animIdle) || (enTransicion && !next.IsName(animIdle)))
                    animCorrecto = animIdle;
                break;
            case EstadoJugador.Caminar:
                string animWalk = EsMovimientoHaciaAdelante() ? animCaminar : animCaminarAtras;
                bool currentOk = current.IsName(animCaminar) || current.IsName(animCaminarAtras);
                bool nextOk = !enTransicion || next.IsName(animCaminar) || next.IsName(animCaminarAtras);
                if (!currentOk || !nextOk) animCorrecto = animWalk;
                break;
            case EstadoJugador.Corriendo:
                if (!current.IsName(animCorrer) || (enTransicion && !next.IsName(animCorrer)))
                    animCorrecto = animCorrer;
                break;
            case EstadoJugador.Agachado:
                bool curAgOk = current.IsName(animAgachado) || current.IsName(animAgachadoIdle);
                bool nxtAgOk = !enTransicion || next.IsName(animAgachado) || next.IsName(animAgachadoIdle);
                if (!curAgOk || !nxtAgOk) animCorrecto = animAgachadoIdle;
                break;
        }

        if (animCorrecto != null) PlayAnim(animCorrecto);
    }

    void AplicarGravedadExtra()
    {
        if (estadoActual == EstadoJugador.Dasheando && !enSuelo) return;

        if (rb.linearVelocity.y < 0f)
            rb.AddForce(Vector3.down * gravedadExtra, ForceMode.Acceleration);
    }

    void MoverPersonaje()
    {
        if (_enBlockstun)
        {
            rb.linearVelocity = new Vector3(0f, Mathf.Min(rb.linearVelocity.y, 0f), 0f);
            return;
        }

        if (estadoActual == EstadoJugador.Dasheando) return;

        float velocidad = 0f;

        if (estadoActual == EstadoJugador.Idle || estadoActual == EstadoJugador.Caminar)
        {
            velocidad = movimientoHorizontal * datosPersonaje.velocidadMovimiento * buffMultVelocidad;
            estadoActual = movimientoHorizontal != 0f ? EstadoJugador.Caminar : EstadoJugador.Idle;
        }
        else if (estadoActual == EstadoJugador.Corriendo)
        {
            if (_enEstadoWinRar) { corriendo = false; estadoActual = EstadoJugador.Idle; return; }
            float dirAdelante = oponente != null
                ? Mathf.Sign(oponente.position.z - transform.position.z)
                : Mathf.Sign(movimientoHorizontal);
            velocidad = dirAdelante * datosPersonaje.velocidadCarrera * buffMultVelocidad;
        }
        else return;

        float vy = enSuelo ? Mathf.Min(rb.linearVelocity.y, 0f) : rb.linearVelocity.y;
        rb.linearVelocity = new Vector3(0f, vy, velocidad);
    }

    void ActualizarOrientacion()
    {
        if (oponente == null || modeloHijo == null) return;
        if (_personajeAgarrado != null) return;
        if (estadoActual == EstadoJugador.Agarrado) return;

        float d = oponente.position.z - transform.position.z;
        const float umbral = 0.40f;
        const float cooldownFlip = 0.20f;

        float nuevoSigno;
        if (d > umbral) nuevoSigno = 1f;
        else if (d < -umbral) nuevoSigno = -1f;
        else nuevoSigno = _signoOrientacion;

        if (nuevoSigno == _signoOrientacion) return;
        if (Time.time - _tiempoUltimoFlip < cooldownFlip) return;

        _tiempoUltimoFlip = Time.time;
        _signoOrientacion = nuevoSigno;

        if (nuevoSigno > 0f)
            modeloHijo.localScale = new Vector3(escalaOriginalModelo.x, escalaOriginalModelo.y, Mathf.Abs(escalaOriginalModelo.z));
        else
            modeloHijo.localScale = new Vector3(escalaOriginalModelo.x, escalaOriginalModelo.y, -Mathf.Abs(escalaOriginalModelo.z));

        AplicarOrientacionAColisiones();
    }

    void AplicarOrientacionAColisiones()
    {
        if (hurtboxCollider != null)
        {
            float centroZ = _centroColisionOriginal.z * _signoOrientacion;
            if (hurtboxCollider is BoxCollider bc)
                bc.center = new Vector3(bc.center.x, bc.center.y, centroZ);
            else if (hurtboxCollider is CapsuleCollider cc)
                cc.center = new Vector3(cc.center.x, cc.center.y, centroZ);
        }

        if (hurtboxAgachadoCollider != null)
        {
            if (hurtboxAgachadoCollider is BoxCollider bcAg)
                bcAg.center = new Vector3(bcAg.center.x, bcAg.center.y,
                    Mathf.Abs(bcAg.center.z) * _signoOrientacion);
            else if (hurtboxAgachadoCollider is CapsuleCollider ccAg)
                ccAg.center = new Vector3(ccAg.center.x, ccAg.center.y,
                    Mathf.Abs(ccAg.center.z) * _signoOrientacion);
        }

        if (colliderBloqueoProximidad != null)
        {
            Vector3 lp = _proxBloqueoLocalPosOriginal;
            colliderBloqueoProximidad.transform.localPosition = new Vector3(lp.x, lp.y, lp.z * _signoOrientacion);
        }
    }

    void ActualizarAnimacionMovimiento()
    {
        if (_enBlockstun) return;
        if (estadoActual == EstadoJugador.Dasheando) return;

        bool haciaAdelante = EsMovimientoHaciaAdelante();
        bool estadoCambio = estadoActual != estadoAnterior;
        bool orientCambio = estadoActual == EstadoJugador.Caminar && haciaAdelante != ultimaOrientacionAdelante;

        if (!estadoCambio && !orientCambio) return;

        estadoAnterior = estadoActual;
        ultimaOrientacionAdelante = haciaAdelante;

        switch (estadoActual)
        {
            case EstadoJugador.Idle: PlayAnim(animIdle); break;
            case EstadoJugador.Caminar: PlayAnim(haciaAdelante ? animCaminar : animCaminarAtras); break;
            case EstadoJugador.Corriendo: PlayAnim(animCorrer); break;
        }
    }

    protected bool EsMovimientoHaciaAdelante()
    {
        if (movimientoHorizontal == 0f) return true;
        if (oponente == null) return movimientoHorizontal > 0f;
        float d = oponente.position.z - transform.position.z;
        if (Mathf.Abs(d) < 0.01f) return true;
        return Mathf.Sign(movimientoHorizontal) == Mathf.Sign(d);
    }

    protected bool EsAdelante(float dir)
    {
        if (oponente == null) return dir > 0f;
        float d = oponente.position.z - transform.position.z;
        if (Mathf.Abs(d) < 0.01f) return true;
        return Mathf.Sign(dir) == Mathf.Sign(d);
    }

    // Acciones normales

    protected void CambiarEstado(EstadoJugador nuevo)
    {
        estadoActual = nuevo;

        bool esAgachado = nuevo == EstadoJugador.Agachado
            || ((nuevo == EstadoJugador.Atacando || nuevo == EstadoJugador.Comando)
                && contextoActual == ContextoPersonaje.Agachado);

        if (hurtboxCollider != null)
            hurtboxCollider.enabled = !esAgachado;
        if (hurtboxAgachadoCollider != null)
            hurtboxAgachadoCollider.enabled = esAgachado;

        if (!esAgachado && hurtboxCollider != null)
        {
            float centroZ = _centroColisionOriginal.z * _signoOrientacion;

            if (hurtboxCollider is BoxCollider hbBox)
            {
                hbBox.size = _hurtboxSizeOriginal;
                hbBox.center = new Vector3(_centroColisionOriginal.x, _centroColisionOriginal.y, centroZ);
            }
            else if (hurtboxCollider is CapsuleCollider hbCap)
            {
                hbCap.height = _hurtboxSizeOriginal.y;
                hbCap.center = new Vector3(_centroColisionOriginal.x, _centroColisionOriginal.y, centroZ);
            }
        }
    }

    void Saltar()
    {
        if (_juegoTerminado) return;
        if (_enEstadoWinRar) return;
        if (_enBlockstun) return;

        // FIX: calcular esCancelASalto ANTES del guard de _comandoEnEjecucion,
        // para que el cancel a salto desde un comando pueda ejecutarse inmediatamente
        // en vez de bufferizarse y dispararse cuando el comando ya terminó.
        bool esCancelASalto = false;
        if ((estadoActual == EstadoJugador.Atacando || estadoActual == EstadoJugador.Comando)
            && datosPersonaje != null && _ataqueHaConectado)
        {
            AtaqueID targets = datosPersonaje.ObtenerCanceles(_ataqueActivoID);
            esCancelASalto = (targets & AtaqueID.Salto) != 0;
        }

        // Solo bufferizar si NO hay un cancel a salto válido
        if (_comandoEnEjecucion && !esCancelASalto)
        {
            bufferSalto = true;
            bufferSaltoTiempo = Time.time;
            return;
        }

        if (estadoActual == EstadoJugador.Dasheando)
        {
            if (!_dasheandoAdelante) return;

            bool enAire = !enSuelo;
            if (enAire)
            {
                if (!permitirDobleSalto || _dobleSaltoUsado || yaAtacoEnAire) return;
                _dobleSaltoUsado = true;
            }

            if (_corrutinaDash != null) { StopCoroutine(_corrutinaDash); _corrutinaDash = null; }
            _dashAereoUsado = false;

            corriendo = false;
            yaAtacoEnAire = false;
            CambiarEstado(EstadoJugador.Saltar);
            contextoActual = ContextoPersonaje.Aire;
            enSuelo = false;
            tiempoSalto = Time.time;

            rb.linearVelocity = new Vector3(
                0f,
                fuerzaSaltoCorridaDash * buffMultSalto,
                movimientoHorizontal * velocidadHorizontalSaltoCorridaDash);

            StartCoroutine(SpawnVFXSalto(transform.position));
            StartCoroutine(TransicionSalto());
            return;
        }

        if (estadoActual == EstadoJugador.Agachado
         || estadoActual == EstadoJugador.Hitstun
         || estadoActual == EstadoJugador.Dizzy
         || estadoActual == EstadoJugador.Agarrado) return;

        if ((estadoActual == EstadoJugador.Atacando || estadoActual == EstadoJugador.Comando)
            && !esCancelASalto)
        {
            bufferSalto = true;
            bufferSaltoTiempo = Time.time;
            return;
        }

        bool estaEnAire = estadoActual == EstadoJugador.Saltar && !enSuelo;
        if (estaEnAire)
        {
            if (!permitirDobleSalto || _dobleSaltoUsado || yaAtacoEnAire) return;
            _dobleSaltoUsado = true;
            rb.linearVelocity = new Vector3(0f, datosPersonaje.fuerzaSalto * buffMultSalto,
                                            movimientoHorizontal * velocidadSaltoHorizontal);
            StartCoroutine(TransicionSalto());
            return;
        }

        if (esCancelASalto)
        {
            _ataqueGeneracion++;
            ataqueEsCancelable = false;
            _command1Cancelado = true;
            _comandoEnEjecucion = false;   // FIX: liberar el flag de comando al cancelar
            SetAllHitboxes(false);
            hitboxesActuales = null;
            _ataqueActivoID = AtaqueID.Ninguno;
            if (!enSuelo) _dobleSaltoUsado = true;
        }

        corriendo = false;
        bufferAgacharse = false;
        yaAtacoEnAire = false;
        LimpiarQCF(); LimpiarQCB();
        CambiarEstado(EstadoJugador.Saltar);
        contextoActual = ContextoPersonaje.Aire;
        enSuelo = false;
        recienSaltado = false;
        tiempoSalto = Time.time;

        bool veniaDeCorriendo = estadoAnterior == EstadoJugador.Corriendo;

        float vy = veniaDeCorriendo
            ? fuerzaSaltoCorridaDash * buffMultSalto
            : datosPersonaje.fuerzaSalto * buffMultSalto;

        // FIX diagonal: los cancels a salto (desde ataque o comando) usan la velocidad
        // horizontal de salto desde carrera para que el jugador pueda saltar en diagonal
        // manteniendo pulsada una dirección en el momento del cancel.
        //
        // Para cubrir el caso de pulsación simultánea (dirección+salto en el mismo frame),
        // el InputSystem puede procesar el evento de salto antes de actualizar
        // movimientoHorizontal. Por eso:
        //   1. Leemos el InputAction directamente (estado hardware real).
        //   2. Si tampoco está pulsado ahora, usamos la última dirección registrada
        //      dentro de la ventana graciaHorizontalCancelSalto.
        float dirParaSalto = movimientoHorizontal;
        if ((esCancelASalto || veniaDeCorriendo) && dirParaSalto == 0f)
        {
            // 1) Estado hardware directo del InputAction
            float dr = moverDerecha != null ? moverDerecha.ReadValue<float>() : 0f;
            float dl = moverIzquierda != null ? moverIzquierda.ReadValue<float>() : 0f;
            if (dr > 0.5f) dirParaSalto = 1f;
            else if (dl > 0.5f) dirParaSalto = -1f;

            // 2) Gracia: si la dirección fue pulsada muy recientemente (< graciaHorizontalCancelSalto s)
            if (dirParaSalto == 0f && Time.time - _tUltimaDireccionHorizontal <= graciaHorizontalCancelSalto)
                dirParaSalto = _ultimaDireccionHorizontal;
        }

        float vz = (esCancelASalto || veniaDeCorriendo)
            ? dirParaSalto * velocidadHorizontalSaltoCorridaDash
            : movimientoHorizontal * velocidadSaltoHorizontal;

        rb.linearVelocity = new Vector3(0f, vy, vz);
        StartCoroutine(SpawnVFXSalto(transform.position));
        StartCoroutine(TransicionSalto());
    }

    void EjecutarBufferSalto()
    {
        if (!bufferSalto) return;
        bufferSalto = false;
        if (Time.time - bufferSaltoTiempo > bufferAtaqueVentana) return;
        Saltar();
    }

    IEnumerator TransicionSalto()
    {
        PlayAnim(animSaltar);
        yield return new WaitForSeconds(0.3f);
        if (estadoActual == EstadoJugador.Saltar) PlayAnim(animAire);
    }

    // --- VFX / Particulas de movimiento ---

    IEnumerator SpawnVFXDashAdelante(Vector3 posicionPies)
    {
        if (prefabVFXDashAdelante == null) yield break;

        // La rotacion respeta _signoOrientacion para que el VFX se espeje con el personaje.
        Quaternion rotVFXDash = Quaternion.Euler(0f, _signoOrientacion > 0f ? 0f : 180f, 0f);
        GameObject vfx = Instantiate(prefabVFXDashAdelante, posicionPies, rotVFXDash);
        yield return new WaitForSeconds(0.05f);

        if (vfx == null) yield break;

        UnityEngine.VFX.VisualEffect ve = vfx.GetComponent<UnityEngine.VFX.VisualEffect>();
        if (ve != null) ve.SendEvent("OnStop");

        yield return new WaitForSeconds(0.4f);
        if (vfx != null) Destroy(vfx);
    }

    // Referencia al GO de particulas de carrera activo (para poder detenerlo al parar)
    private GameObject _particulasCorrerActivo;

    void IniciarParticulasCorrer()
    {
        if (prefabParticulasCorrer == null) return;
        DetenerParticulasCorrer();
        // Instanciamos como hijo del jugador para que sigan los pies mientras corre
        _particulasCorrerActivo = Instantiate(prefabParticulasCorrer, transform.position, Quaternion.identity, transform);
    }

    void DetenerParticulasCorrer()
    {
        if (_particulasCorrerActivo == null) return;

        // Desanclar del padre para que las particulas ya emitidas no se muevan con el jugador
        _particulasCorrerActivo.transform.SetParent(null);

        ParticleSystem[] sistemas = _particulasCorrerActivo.GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem ps in sistemas)
        {
            var emision = ps.emission;
            emision.enabled = false;
        }

        float tiempoMaxVida = 0f;
        foreach (ParticleSystem ps in sistemas)
            tiempoMaxVida = Mathf.Max(tiempoMaxVida, ps.main.startLifetime.constantMax);

        Destroy(_particulasCorrerActivo, tiempoMaxVida);
        _particulasCorrerActivo = null;
    }

    void SpawnParticulasSimple(GameObject prefab, Vector3 posicion)
    {
        if (prefab == null) return;

        GameObject go = Instantiate(prefab, posicion, Quaternion.identity);

        ParticleSystem[] sistemas = go.GetComponentsInChildren<ParticleSystem>(true);
        float tiempoMaxVida = 0f;
        foreach (ParticleSystem ps in sistemas)
            tiempoMaxVida = Mathf.Max(tiempoMaxVida, ps.main.startLifetime.constantMax + ps.main.duration);

        // Si no hay ParticleSystem (p.ej. VFX Graph), destruir tras un tiempo fijo
        if (tiempoMaxVida <= 0f) tiempoMaxVida = 2f;
        Destroy(go, tiempoMaxVida);
    }

    IEnumerator SpawnVFXSalto(Vector3 posicionSuelo)
    {
        if (prefabVFXSalto == null) yield break;

        // Se instancia en la posicion del suelo sin padre: no sigue al jugador.
        // La rotacion respeta _signoOrientacion para que el VFX se espeje con el personaje.
        Quaternion rotVFXSalto = Quaternion.Euler(0f, _signoOrientacion > 0f ? 0f : 180f, 0f);
        GameObject vfx = Instantiate(prefabVFXSalto, posicionSuelo, rotVFXSalto);
        yield return new WaitForSeconds(0.05f);

        if (vfx == null) yield break;

        // Visual Effect Graph: enviar OnStop para que el spawner deje de emitir.
        // Las particulas ya vivas terminan su ciclo de vida de forma natural (sin corte brusco).
        UnityEngine.VFX.VisualEffect ve = vfx.GetComponent<UnityEngine.VFX.VisualEffect>();
        if (ve != null) ve.SendEvent("OnStop");

        // El lifetime maximo del VFX es 0.4 s; esperamos ese margen para que no quede nada flotando.
        yield return new WaitForSeconds(0.4f);
        if (vfx != null) Destroy(vfx);
    }

    void IniciarAgacharse()
    {
        if (_juegoTerminado) return;
        if (_enBlockstun) return;
        if (estadoActual != EstadoJugador.Idle
         && estadoActual != EstadoJugador.Caminar
         && estadoActual != EstadoJugador.Corriendo) return;

        corriendo = false;
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        CambiarEstado(EstadoJugador.Agachado);
        contextoActual = ContextoPersonaje.Agachado;
        StartCoroutine(TransicionAgachado());
    }

    IEnumerator TransicionAgachado()
    {
        PlayAnim(animAgachado);
        yield return new WaitForSeconds(0.3f);
        if (estadoActual == EstadoJugador.Agachado) PlayAnim(animAgachadoIdle);
    }

    void SoltarAgacharse()
    {
        if (_enBlockstun) return;

        if (estadoActual == EstadoJugador.Agachado)
        {
            CambiarEstado(EstadoJugador.Idle);
            contextoActual = ContextoPersonaje.Suelo;
            PlayAnim(animIdle);
        }
        else if (estadoActual == EstadoJugador.Atacando)
            contextoActual = ContextoPersonaje.Suelo;
    }

    // Atacar

    protected void Atacar(GameObject[] hitboxes, string nombreAnimacion)
    {
        bool enAire = estadoActual == EstadoJugador.Saltar;
        bool puedeAtacar = estadoActual == EstadoJugador.Idle
                        || estadoActual == EstadoJugador.Caminar
                        || estadoActual == EstadoJugador.Corriendo
                        || estadoActual == EstadoJugador.Agachado
                        || (enAire && !yaAtacoEnAire);
        if (!puedeAtacar) return;
        if (enAire) yaAtacoEnAire = true;

        corriendo = false;
        rb.linearVelocity = enAire
            ? new Vector3(0f, rb.linearVelocity.y, rb.linearVelocity.z)
            : new Vector3(0f, rb.linearVelocity.y, 0f);

        CambiarEstado(EstadoJugador.Atacando);
        hitboxesActuales = hitboxes;

        if (nombreAnimacion != null) PlayAnim(nombreAnimacion);
        if (hitboxes != null)
        {
            _ataqueActivoID = HitboxesAAtaqueID(hitboxes);
            PrepararHitboxes(hitboxes);
        }
        _ataqueHaConectado = false;
        _ataqueGeneracion++;
        float duracionFallback = nombreAnimacion != null ? ObtenerDuracionClip(nombreAnimacion) : 0.5f;
        StartCoroutine(VolverEstadoTrasAtaque(duracionFallback, _ataqueGeneracion));
    }

    protected void PrepararHitboxes(GameObject[] hitboxes)
    {
        if (hitboxes == null) return;

        float dano = 0f;
        Vector3? escala = null;
        Vector3? offset = null;

        if (hitboxes == hitboxesPunoFlojo) ResolverDatosPunoFlojo(ref dano, ref escala, ref offset);
        else if (hitboxes == hitboxesPunoFuerte) ResolverDatosPunoFuerte(ref dano, ref escala, ref offset);
        else if (hitboxes == hitboxesPatadaFloja) ResolverDatosPatadaFloja(ref dano, ref escala, ref offset);
        else if (hitboxes == hitboxesPatadaFuerte) ResolverDatosPatadaFuerte(ref dano, ref escala, ref offset);
        else if (hitboxes == hitboxesAgarre)
        {
            if (datosPersonaje != null) dano = datosPersonaje.danoAgarre;
        }

        DireccionGolpe dir = ResolverDireccion(hitboxes);
        TipoGolpe tipoGolpe = ResolverTipoGolpe(hitboxes);
        float iy, iz;
        ResolverImpulsos(hitboxes, out iy, out iz);

        float escalaCombo = CalcularEscalaDanio();
        dano = Mathf.Max(1f, dano * escalaCombo);
        iy *= escalaCombo;
        iz *= escalaCombo;

        ataqueEsCancelable = datosPersonaje != null
            && datosPersonaje.ObtenerCanceles(_ataqueActivoID) != AtaqueID.Ninguno;

        float hitstun = ResolverHitstun(hitboxes);

        foreach (var hb in hitboxes)
        {
            if (hb == null) continue;
            Hitbox h = hb.GetComponent<Hitbox>();
            if (h != null && h.soloSuelo && contextoActual != ContextoPersonaje.Suelo) continue;
            if (h != null && h.ignorarEnSuelo && contextoActual == ContextoPersonaje.Suelo) continue;
            if (h != null)
            {
                h.SetDano(dano);
                h.direccion = dir;
                h.tipoGolpe = tipoGolpe;
                h.esCancelable = ataqueEsCancelable;
                h.hitstunDuracion = hitstun;
                h.SetImpulso(iy, iz);
            }
            if (h == null || !h.ignorarAjusteTransform)
                AjustarHitbox(hb, escala, offset);
        }
    }
    // Dash

    void TriggerDash(bool adelante, bool aereo)
    {
        if (_juegoTerminado) return;
        if (_enBlockstun) return;
        if (_enEstadoWinRar) return;
        if (enVentanaCancelCommand2) return;

        if (estadoActual == EstadoJugador.Atacando || estadoActual == EstadoJugador.Comando)
        {
            _bufferDash = true;
            _bufferDashEsAdelante = adelante;
            _bufferDashEsAereo = aereo;
            _bufferDashTiempo = Time.time;

            if (_ataqueHaConectado && ataqueEsCancelable)
                EjecutarCancelDash();

            return;
        }

        EjecutarDashDirecto(adelante, aereo);
    }

    bool EjecutarCancelDash()
    {
        if (!_bufferDash) return false;
        if (Time.time - _bufferDashTiempo > bufferAtaqueVentana)
        {
            _bufferDash = false;
            return false;
        }

        if (datosPersonaje != null)
        {
            AtaqueID cancelTargets = datosPersonaje.ObtenerCanceles(_ataqueActivoID);
            if (cancelTargets != AtaqueID.Ninguno)
            {
                AtaqueID dashID = ObtenerIDDash(_bufferDashEsAdelante, _bufferDashEsAereo);
                if ((cancelTargets & dashID) == 0) return false;
            }
        }

        _bufferDash = false;
        ataqueEsCancelable = false;
        _ataqueGeneracion++;
        SetAllHitboxes(false);
        hitboxesActuales = null;
        if (enSuelo)
        {
            CambiarEstado(EstadoJugador.Idle);
            contextoActual = ContextoPersonaje.Suelo;
        }
        else
        {
            CambiarEstado(EstadoJugador.Saltar);
            contextoActual = ContextoPersonaje.Aire;
        }

        EjecutarDashDirecto(_bufferDashEsAdelante, _bufferDashEsAereo);
        return true;
    }

    void EjecutarDashDirecto(bool adelante, bool aereo)
    {
        if (aereo)
        {
            if (enSuelo) return;
            if (!puedeDashAire) return;
            if (_dashAereoUsado) return;
            if (estadoActual != EstadoJugador.Saltar
             && estadoActual != EstadoJugador.Idle) return;
        }
        else if (adelante)
        {
            if (!puedeDashAdelante) return;
            if (!enSuelo) return;
            if (estadoActual != EstadoJugador.Idle
             && estadoActual != EstadoJugador.Caminar
             && estadoActual != EstadoJugador.Corriendo) return;
        }
        else
        {
            if (!enSuelo) return;
            if (estadoActual != EstadoJugador.Idle
             && estadoActual != EstadoJugador.Caminar
             && estadoActual != EstadoJugador.Corriendo) return;
        }

        if (aereo) _dashAereoUsado = true;

        // Al hacer dash aéreo se resetea yaAtacoEnAire para que el personaje
        // pueda atacar al instante al cancelar un ataque aéreo -> dash aéreo.
        if (aereo) yaAtacoEnAire = false;

        if (_corrutinaDash != null) { StopCoroutine(_corrutinaDash); _corrutinaDash = null; }
        _corrutinaDash = StartCoroutine(CorrutinaDash(adelante, aereo));
    }

    IEnumerator CorrutinaDash(bool adelante, bool aereo)
    {
        float velocidad, distancia, lag;
        string animDash;

        if (!aereo && adelante)
        {
            velocidad = velocidadDashAdelanteSuelo;
            distancia = distanciaDashAdelanteSuelo;
            lag = lagDashAdelanteSuelo;
            animDash = animDashAdelanteSuelo;
        }
        else if (!aereo)
        {
            velocidad = velocidadDashAtrasSuelo;
            distancia = distanciaDashAtrasSuelo;
            lag = lagDashAtrasSuelo;
            animDash = animDashAtrasSuelo;
        }
        else if (adelante)
        {
            velocidad = velocidadDashAdelanteAire;
            distancia = distanciaDashAdelanteAire;
            lag = lagDashAdelanteAire;
            animDash = animDashAdelanteAire;
        }
        else
        {
            velocidad = velocidadDashAtrasAire;
            distancia = distanciaDashAtrasAire;
            lag = lagDashAtrasAire;
            animDash = animDashAtrasAire;
        }

        float dirHaciaOponente = oponente != null
            ? Mathf.Sign(oponente.position.z - transform.position.z)
            : _signoOrientacion;
        float dirDash = adelante ? dirHaciaOponente : -dirHaciaOponente;

        _dashActualCancelableAAtaque = true; // Todos los dash son cancelables con ataque de forma instantánea
        _dasheandoAdelante = adelante;

        corriendo = false;
        ResetearBloqueo();
        LimpiarQCF();
        LimpiarQCB();
        CambiarEstado(EstadoJugador.Dasheando);
        contextoActual = aereo ? ContextoPersonaje.Aire : ContextoPersonaje.Suelo;
        rb.linearVelocity = new Vector3(0f, aereo ? 0f : rb.linearVelocity.y, dirDash * velocidad);
        PlayAnim(animDash);

        // VFX en los pies al hacer dash adelante en suelo
        if (!aereo && adelante)
            StartCoroutine(SpawnVFXDashAdelante(transform.position));

        float posInicialZ = transform.position.z;
        float distRecorrida = 0f;
        float tiempoMaxDash = (velocidad > 0f) ? (distancia / velocidad) * 2.5f : 1f;
        float tDash = 0f;
        float zUltimo = posInicialZ;
        float tSinAvance = 0f;

        while (distRecorrida < distancia)
        {
            if (estadoActual != EstadoJugador.Dasheando) yield break;
            // FIX: dash aereo totalmente horizontal — Y forzada a 0 para evitar caida durante el dash
            float velY = aereo ? 0f : rb.linearVelocity.y;
            rb.linearVelocity = new Vector3(0f, velY, dirDash * velocidad);
            if (aereo && enSuelo) break;
            distRecorrida = Mathf.Abs(transform.position.z - posInicialZ);

            // detectar atasco: si la posicion z no avanza, salir del dash
            float avance = Mathf.Abs(transform.position.z - zUltimo);
            zUltimo = transform.position.z;
            tSinAvance = avance < 0.001f ? tSinAvance + Time.deltaTime : 0f;

            tDash += Time.deltaTime;
            if (tSinAvance > 0.1f || tDash > tiempoMaxDash) break;

            yield return null;
        }

        if (estadoActual != EstadoJugador.Dasheando) yield break;

        if (aereo && !enSuelo)
            rb.linearVelocity = new Vector3(0f, 0f, dirDash * velocidad * 0.5f);
        else
            rb.linearVelocity = new Vector3(0f, 0f, 0f);

        float tLag = 0f;
        while (tLag < lag)
        {
            if (estadoActual != EstadoJugador.Dasheando) yield break;
            if (aereo && enSuelo) break;
            tLag += Time.deltaTime;
            yield return null;
        }

        if (estadoActual != EstadoJugador.Dasheando) yield break;

        _corrutinaDash = null;

        if (!enSuelo)
        {
            CambiarEstado(EstadoJugador.Saltar);
            contextoActual = ContextoPersonaje.Aire;
            PlayAnim(animAire);
        }
        else
        {
            CambiarEstado(EstadoJugador.Idle);
            contextoActual = ContextoPersonaje.Suelo;
            PlayAnim(animIdle);
            EjecutarBufferAtaque();
            EjecutarBufferAgacharse();
            EjecutarBufferSalto();
        }
    }

    protected AtaqueID ObtenerIDDash(bool adelante, bool aereo)
    {
        if (!aereo && adelante) return AtaqueID.DashAdelanteTerrestre;
        if (!aereo) return AtaqueID.DashAtrasTerrestre;
        if (adelante) return AtaqueID.DashAdelanteAereo;
        return AtaqueID.DashAtrasAereo;
    }

    // Helpers de resolucion de datos por tipo de hitbox

    void ResolverDatosPunoFlojo(ref float dano, ref Vector3? escala, ref Vector3? offset)
    {
        if (contextoActual == ContextoPersonaje.Aire) { dano = datosPersonaje.danoPunoFlojoAire; escala = datosPersonaje.scalePunoFlojoAire; offset = datosPersonaje.offsetPunoFlojoAire; }
        else if (contextoActual == ContextoPersonaje.Agachado) { dano = datosPersonaje.danoPunoFlojoAgachado; escala = datosPersonaje.scalePunoFlojoAgachado; offset = datosPersonaje.offsetPunoFlojoAgachado; }
        else { dano = datosPersonaje.danoPunoFlojoSuelo; escala = datosPersonaje.scalePunoFlojoSuelo; offset = datosPersonaje.offsetPunoFlojoSuelo; }
    }

    void ResolverDatosPunoFuerte(ref float dano, ref Vector3? escala, ref Vector3? offset)
    {
        if (contextoActual == ContextoPersonaje.Aire) { dano = datosPersonaje.danoPunoFuerteAire; escala = datosPersonaje.scalePunoFuerteAire; offset = datosPersonaje.offsetPunoFuerteAire; }
        else if (contextoActual == ContextoPersonaje.Agachado) { dano = datosPersonaje.danoPunoFuerteAgachado; escala = datosPersonaje.scalePunoFuerteAgachado; offset = datosPersonaje.offsetPunoFuerteAgachado; }
        else { dano = datosPersonaje.danoPunoFuerteSuelo; escala = datosPersonaje.scalePunoFuerteSuelo; offset = datosPersonaje.offsetPunoFuerteSuelo; }
    }

    void ResolverDatosPatadaFloja(ref float dano, ref Vector3? escala, ref Vector3? offset)
    {
        if (contextoActual == ContextoPersonaje.Aire) { dano = datosPersonaje.danoPatadaFlojaAire; escala = datosPersonaje.scalePatadaFlojaAire; offset = datosPersonaje.offsetPatadaFlojaAire; }
        else if (contextoActual == ContextoPersonaje.Agachado) { dano = datosPersonaje.danoPatadaFlojaAgachado; escala = datosPersonaje.scalePatadaFlojaAgachado; offset = datosPersonaje.offsetPatadaFlojaAgachado; }
        else { dano = datosPersonaje.danoPatadaFlojaSuelo; escala = datosPersonaje.scalePatadaFlojaSuelo; offset = datosPersonaje.offsetPatadaFlojaSuelo; }
    }

    void ResolverDatosPatadaFuerte(ref float dano, ref Vector3? escala, ref Vector3? offset)
    {
        if (contextoActual == ContextoPersonaje.Aire) { dano = datosPersonaje.danoPatadaFuerteAire; escala = datosPersonaje.scalePatadaFuerteAire; offset = datosPersonaje.offsetPatadaFuerteAire; }
        else if (contextoActual == ContextoPersonaje.Agachado) { dano = datosPersonaje.danoPatadaFuerteAgachado; escala = datosPersonaje.scalePatadaFuerteAgachado; offset = datosPersonaje.offsetPatadaFuerteAgachado; }
        else { dano = datosPersonaje.danoPatadaFuerteSuelo; escala = datosPersonaje.scalePatadaFuerteSuelo; offset = datosPersonaje.offsetPatadaFuerteSuelo; }
    }

    DireccionGolpe ResolverDireccion(GameObject[] hitboxes)
    {
        if (hitboxes == hitboxesPunoFlojo)
        {
            if (contextoActual == ContextoPersonaje.Aire) return datosPersonaje.direccionPunoFlojoAire;
            if (contextoActual == ContextoPersonaje.Agachado) return datosPersonaje.direccionPunoFlojoAgachado;
            return datosPersonaje.direccionPunoFlojoSuelo;
        }
        if (hitboxes == hitboxesPunoFuerte)
        {
            if (contextoActual == ContextoPersonaje.Aire) return datosPersonaje.direccionPunoFuerteAire;
            if (contextoActual == ContextoPersonaje.Agachado) return datosPersonaje.direccionPunoFuerteAgachado;
            return datosPersonaje.direccionPunoFuerteSuelo;
        }
        if (hitboxes == hitboxesPatadaFloja)
        {
            if (contextoActual == ContextoPersonaje.Aire) return datosPersonaje.direccionPatadaFlojaAire;
            if (contextoActual == ContextoPersonaje.Agachado) return datosPersonaje.direccionPatadaFlojaAgachado;
            return datosPersonaje.direccionPatadaFlojaSuelo;
        }
        if (hitboxes == hitboxesPatadaFuerte)
        {
            if (contextoActual == ContextoPersonaje.Aire) return datosPersonaje.direccionPatadaFuerteAire;
            if (contextoActual == ContextoPersonaje.Agachado) return datosPersonaje.direccionPatadaFuerteAgachado;
            return datosPersonaje.direccionPatadaFuerteSuelo;
        }
        return DireccionGolpe.Frente;
    }

    TipoGolpe ResolverTipoGolpe(GameObject[] hitboxes)
    {
        if (hitboxes == hitboxesPunoFlojo)
        {
            if (contextoActual == ContextoPersonaje.Aire) return datosPersonaje.tipoGolpePunoFlojoAire;
            if (contextoActual == ContextoPersonaje.Agachado) return datosPersonaje.tipoGolpePunoFlojoAgachado;
            return datosPersonaje.tipoGolpePunoFlojoSuelo;
        }
        if (hitboxes == hitboxesPunoFuerte)
        {
            if (contextoActual == ContextoPersonaje.Aire) return datosPersonaje.tipoGolpePunoFuerteAire;
            if (contextoActual == ContextoPersonaje.Agachado) return datosPersonaje.tipoGolpePunoFuerteAgachado;
            return datosPersonaje.tipoGolpePunoFuerteSuelo;
        }
        if (hitboxes == hitboxesPatadaFloja)
        {
            if (contextoActual == ContextoPersonaje.Aire) return datosPersonaje.tipoGolpePatadaFlojaAire;
            if (contextoActual == ContextoPersonaje.Agachado) return datosPersonaje.tipoGolpePatadaFlojaAgachado;
            return datosPersonaje.tipoGolpePatadaFlojaSuelo;
        }
        if (hitboxes == hitboxesPatadaFuerte)
        {
            if (contextoActual == ContextoPersonaje.Aire) return datosPersonaje.tipoGolpePatadaFuerteAire;
            if (contextoActual == ContextoPersonaje.Agachado) return datosPersonaje.tipoGolpePatadaFuerteAgachado;
            return datosPersonaje.tipoGolpePatadaFuerteSuelo;
        }
        if (hitboxes == hitboxesCommand1) return datosPersonaje.tipoGolpeComando1;
        if (hitboxes == hitboxesCommand2) return datosPersonaje.tipoGolpeComando2;
        if (hitboxes == hitboxesCommand3) return datosPersonaje.tipoGolpeComando3;
        return TipoGolpe.Normal;
    }

    void ResolverImpulsos(GameObject[] hitboxes, out float iy, out float iz)
    {
        iy = 0f; iz = 0f;
        if (hitboxes == hitboxesPunoFlojo)
        {
            if (contextoActual == ContextoPersonaje.Aire) { iy = datosPersonaje.impulsoYPunoFlojoAire; iz = datosPersonaje.impulsoZPunoFlojoAire; }
            else if (contextoActual == ContextoPersonaje.Agachado) { iy = datosPersonaje.impulsoYPunoFlojoAgachado; iz = datosPersonaje.impulsoZPunoFlojoAgachado; }
            else { iy = datosPersonaje.impulsoYPunoFlojoSuelo; iz = datosPersonaje.impulsoZPunoFlojoSuelo; }
        }
        else if (hitboxes == hitboxesPunoFuerte)
        {
            if (contextoActual == ContextoPersonaje.Aire) { iy = datosPersonaje.impulsoYPunoFuerteAire; iz = datosPersonaje.impulsoZPunoFuerteAire; }
            else if (contextoActual == ContextoPersonaje.Agachado) { iy = datosPersonaje.impulsoYPunoFuerteAgachado; iz = datosPersonaje.impulsoZPunoFuerteAgachado; }
            else { iy = datosPersonaje.impulsoYPunoFuerteSuelo; iz = datosPersonaje.impulsoZPunoFuerteSuelo; }
        }
        else if (hitboxes == hitboxesPatadaFloja)
        {
            if (contextoActual == ContextoPersonaje.Aire) { iy = datosPersonaje.impulsoYPatadaFlojaAire; iz = datosPersonaje.impulsoZPatadaFlojaAire; }
            else if (contextoActual == ContextoPersonaje.Agachado) { iy = datosPersonaje.impulsoYPatadaFlojaAgachado; iz = datosPersonaje.impulsoZPatadaFlojaAgachado; }
            else { iy = datosPersonaje.impulsoYPatadaFlojaSuelo; iz = datosPersonaje.impulsoZPatadaFlojaSuelo; }
        }
        else if (hitboxes == hitboxesPatadaFuerte)
        {
            if (contextoActual == ContextoPersonaje.Aire) { iy = datosPersonaje.impulsoYPatadaFuerteAire; iz = datosPersonaje.impulsoZPatadaFuerteAire; }
            else if (contextoActual == ContextoPersonaje.Agachado) { iy = datosPersonaje.impulsoYPatadaFuerteAgachado; iz = datosPersonaje.impulsoZPatadaFuerteAgachado; }
            else { iy = datosPersonaje.impulsoYPatadaFuerteSuelo; iz = datosPersonaje.impulsoZPatadaFuerteSuelo; }
        }
        else if (hitboxes == hitboxesCommand1) { iy = datosPersonaje.impulsoYComando1; iz = datosPersonaje.impulsoZComando1; }
        else if (hitboxes == hitboxesCommand2) { iy = datosPersonaje.impulsoYComando2; iz = datosPersonaje.impulsoZComando2; }
        else if (hitboxes == hitboxesCommand3) { iy = datosPersonaje.impulsoYComando3; iz = datosPersonaje.impulsoZComando3; }
    }

    float ResolverHitstun(GameObject[] hitboxes)
    {
        if (hitboxes == hitboxesPunoFlojo)
        {
            if (contextoActual == ContextoPersonaje.Aire) return datosPersonaje.hitstunPunoFlojoAire;
            if (contextoActual == ContextoPersonaje.Agachado) return datosPersonaje.hitstunPunoFlojoAgachado;
            return datosPersonaje.hitstunPunoFlojoSuelo;
        }
        if (hitboxes == hitboxesPunoFuerte)
        {
            if (contextoActual == ContextoPersonaje.Aire) return datosPersonaje.hitstunPunoFuerteAire;
            if (contextoActual == ContextoPersonaje.Agachado) return datosPersonaje.hitstunPunoFuerteAgachado;
            return datosPersonaje.hitstunPunoFuerteSuelo;
        }
        if (hitboxes == hitboxesPatadaFloja)
        {
            if (contextoActual == ContextoPersonaje.Aire) return datosPersonaje.hitstunPatadaFlojaAire;
            if (contextoActual == ContextoPersonaje.Agachado) return datosPersonaje.hitstunPatadaFlojaAgachado;
            return datosPersonaje.hitstunPatadaFlojaSuelo;
        }
        if (hitboxes == hitboxesPatadaFuerte)
        {
            if (contextoActual == ContextoPersonaje.Aire) return datosPersonaje.hitstunPatadaFuerteAire;
            if (contextoActual == ContextoPersonaje.Agachado) return datosPersonaje.hitstunPatadaFuerteAgachado;
            return datosPersonaje.hitstunPatadaFuerteSuelo;
        }
        if (hitboxes == hitboxesCommand1) return datosPersonaje.hitstunComando1;
        if (hitboxes == hitboxesCommand2) return datosPersonaje.hitstunComando2;
        if (hitboxes == hitboxesCommand3) return datosPersonaje.hitstunComando3;
        return 0.4f;
    }

    // Animation events

    public void AnimEvent_AbrirHitbox(int indice = -1)
    {
        if (Time.frameCount == frameUltimoAbrirHitbox) return;
        frameUltimoAbrirHitbox = Time.frameCount;
        if (hitboxesActuales == null) return;
        if (indice < 0)
        {
            AjustarHitboxes(hitboxesActuales);
            SetHitboxes(hitboxesActuales, true, true);
        }
        else if (indice < hitboxesActuales.Length && hitboxesActuales[indice] != null)
        {
            Hitbox h = hitboxesActuales[indice].GetComponent<Hitbox>();
            if (h != null && h.soloSuelo && contextoActual != ContextoPersonaje.Suelo) return;
            AjustarHitbox(hitboxesActuales[indice]);
            hitboxesActuales[indice].SetActive(true);
        }
    }

    public void AnimEvent_CerrarHitbox(int indice = -1)
    {
        if (Time.frameCount == frameUltimoCerrarHitbox) return;
        frameUltimoCerrarHitbox = Time.frameCount;
        if (hitboxesActuales == null) return;
        if (indice < 0)
            SetHitboxes(hitboxesActuales, false);
        else if (indice < hitboxesActuales.Length && hitboxesActuales[indice] != null)
            hitboxesActuales[indice].SetActive(false);
    }

    // FIX: guarda _juegoTerminado para que la animacion de muerte no sea
    // sobreescrita por AnimEvent_FinAtaque si el personaje muere mientras ataca.
    public void AnimEvent_FinAtaque()
    {
        if (_juegoTerminado) return;
        if (_comandoEnEjecucion) return;
        SetAllHitboxes(false);
        hitboxesActuales = null;

        OnFinAtaqueCallback();

        if (!enSuelo) { CambiarEstado(EstadoJugador.Saltar); return; }

        if (abajoPresionado || bufferAgacharse)
        {
            bufferAgacharse = false;
            CambiarEstado(EstadoJugador.Agachado);
            contextoActual = ContextoPersonaje.Agachado;
            PlayAnim(animAgachadoIdle);
            EjecutarBufferAtaque();
            return;
        }

        if (contextoActual == ContextoPersonaje.Agachado)
        {
            CambiarEstado(EstadoJugador.Agachado);
            PlayAnim(animAgachadoIdle);
            EjecutarBufferAtaque();
            return;
        }
        CambiarEstado(EstadoJugador.Idle);
        PlayAnim(animIdle);
        EjecutarBufferAtaque();
        EjecutarBufferAgacharse();
        EjecutarBufferSalto();
    }

    public void AnimEvent_SoltarAgarre()
    {
        if (!_esperandoSoltarAgarre || _personajeAgarrado == null) return;

        float impulsoY = datosPersonaje != null ? datosPersonaje.impulsoYAgarre : 6f;
        float impulsoZ = datosPersonaje != null ? datosPersonaje.impulsoZAgarre : 8f;
        float durHitstun = datosPersonaje != null ? datosPersonaje.duracionHitstunAgarre : 2f;
        float dano = datosPersonaje != null ? datosPersonaje.danoAgarre : 10f;

        _personajeAgarrado.LiberarDeAgarre(impulsoY, impulsoZ, _dirZLanzamientoAgarre, durHitstun, dano);
        _personajeAgarrado = null;
        _esperandoSoltarAgarre = false;
    }

    public virtual void AnimEvent_SoltarAgarreComando() { }

    // override en personajes con ultimate que spawna un objeto (ej. HornoUltimate de Cube)
    public virtual void AnimEvent_SpawnHorno() { }

    // Efectos VFX de golpe

    protected void SpawnParticulasGolpe(GameObject prefab, Vector3 posicion)
    {
        if (prefab == null) return;
        Vector3 pos = posicion == default ? transform.position + Vector3.up : posicion;
        GameObject vfxGolpe = Instantiate(prefab, pos, Quaternion.identity);

        // Destruir automaticamente: usar la duracion del ParticleSystem si existe,
        // si no, un tiempo fijo de seguridad.
        float tiempoVida = 3f;
        ParticleSystem ps = vfxGolpe.GetComponent<ParticleSystem>();
        if (ps == null) ps = vfxGolpe.GetComponentInChildren<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            tiempoVida = main.duration + main.startLifetime.constantMax;
        }
        Destroy(vfxGolpe, tiempoVida);
    }

    // Recibir golpe

    string AnimHitstunAleatorio()
    {
        if (_enEstadoWinRar) return animHitstunWinRar;
        return Random.value < 0.5f ? animHitstun : animHurt2;
    }

    public ResultadoGolpe RecibirGolpe(float duracion, DireccionGolpe dir, TipoGolpe tipo,
                         float impulsoY = 0f, float impulsoZ = 0f, float dano = 0f,
                         Vector3 posicionGolpe = default, bool ignorarBloqueo = false)
    {
        if (estaDerribado) return ResultadoGolpe.Ignorado;

        ignorarBloqueo = ignorarBloqueo || (tipo == TipoGolpe.SiempreHit);

        if (!ignorarBloqueo && EsBloqueado(dir))
        {
            AplicarDano(dano * 0.1f);
            SpawnParticulasGolpe(prefabParticulasBloqueo, posicionGolpe);
            if (_corrutinaBloqueo != null) StopCoroutine(_corrutinaBloqueo);
            _corrutinaBloqueo = StartCoroutine(AnimarBloqueo(duracion));
            IniciarHitstop(duracionHitstopBloqueo);
            return ResultadoGolpe.Bloqueado;
        }

        if (!ignorarBloqueo && OnComprobarEscudo())
        {
            if (_corrutinaBloqueo != null) StopCoroutine(_corrutinaBloqueo);
            _corrutinaBloqueo = StartCoroutine(AnimarBloqueo(0.3f));
            IniciarHitstop(duracionHitstopBloqueo);
            return ResultadoGolpe.Ignorado;
        }

        SpawnParticulasGolpe(prefabParticulasGolpe, posicionGolpe);
        AplicarDano(dano);
        _soundPlayer?.SonidoGolpeAleatorio();

        if (_juegoTerminado) return ResultadoGolpe.Conectado;

        OnRecibirGolpeCallback();

        if (_enEstadoWinRar)
        {
            SpawnParticulasGolpe(prefabParticulasGolpe, posicionGolpe);
            _soundPlayer?.SonidoGolpeAleatorio();
            IniciarHitstop(duracionHitstop);
        }

        LimpiarEstadoAgarrador();
        LimpiarEstadoAgarrado();
        ResetearBloqueo();
        ResetearContadorComboSilencioso();

        StopAllCoroutines();
        _corrutinaDash = null;
        _enVueloLanzado = false;
        _dashAereoUsado = false;
        _bufferDash = false;

        _ataqueHaConectado = false;
        _comandoEnEjecucion = false;
        SetAllHitboxes(false);
        hitboxesActuales = null;
        corriendo = false;
        bufferAgacharse = false;
        bufferSalto = false;
        LimpiarQCF(); LimpiarQCB();

        switch (tipo)
        {
            case TipoGolpe.Derribar:
                StartCoroutine(SecuenciaDerribado());
                break;
            case TipoGolpe.LanzarArriba:
                StartCoroutine(SecuenciaLanzado(
                    new Vector3(0f, impulsoY, DireccionLanzado() * impulsoZ),
                    animLanzadoArriba, duracion));
                break;
            case TipoGolpe.LanzarFrente:
                if (!enSuelo)
                {
                    _enVueloLanzado = true;
                    StartCoroutine(SecuenciaLanzado(
                        new Vector3(0f, impulsoY, DireccionLanzado() * impulsoZ),
                        animLanzadoFrente, duracion));
                }
                else
                {
                    CambiarEstado(EstadoJugador.Hitstun);
                    contextoActual = ContextoPersonaje.Suelo;
                    PlayAnim(AnimHitstunAleatorio());
                    if (impulsoZ > 0f)
                        rb.AddForce(new Vector3(0f, 0f, DireccionLanzado() * impulsoZ), ForceMode.VelocityChange);
                    StartCoroutine(LevantarseTrasHitstun(duracion));
                }
                break;
            case TipoGolpe.LanzarAbajo:
                _reboteSuelo = true;
                if (!enSuelo)
                {
                    StartCoroutine(SecuenciaLanzado(
                        new Vector3(0f, -Mathf.Abs(impulsoY), DireccionLanzado() * impulsoZ),
                        animLanzadoAbajo, duracion));
                }
                else
                {
                    StartCoroutine(SecuenciaLanzado(
                        new Vector3(0f, Mathf.Abs(impulsoY), DireccionLanzado() * impulsoZ),
                        animLanzadoAbajo, duracion));
                }
                break;
            default:
                CambiarEstado(EstadoJugador.Hitstun);
                contextoActual = ContextoPersonaje.Suelo;
                PlayAnim(AnimHitstunAleatorio());
                StartCoroutine(LevantarseTrasHitstun(duracion));
                break;
        }

        IniciarHitstop(duracionHitstop);

        return ResultadoGolpe.Conectado;
    }



    IEnumerator SecuenciaDerribado()
    {
        CambiarEstado(EstadoJugador.Derribado);
        contextoActual = ContextoPersonaje.Suelo;
        estaDerribado = true;
        PlayAnim(animDead);
        if (_juegoTerminado) yield break;
        yield return new WaitForSeconds(duracionDerribado);
        estaDerribado = false;
        if (estadoActual != EstadoJugador.Derribado) yield break;
        estadoActual = EstadoJugador.Idle;
        contextoActual = ContextoPersonaje.Suelo;
        PlayAnim(animLevantarse);
        yield return new WaitForSeconds(0.5f);
        CambiarEstado(EstadoJugador.Idle);
        PlayAnim(animIdle);
    }

    protected float DireccionLanzado()
    {
        if (oponente == null)
            return (modeloHijo != null && modeloHijo.localScale.z > 0f) ? -1f : 1f;
        return oponente.position.z > transform.position.z ? -1f : 1f;
    }

    IEnumerator SecuenciaLanzado(Vector3 impulso, string animVuelo, float duracionHitstun = 0f)
    {
        CambiarEstado(EstadoJugador.Hitstun);
        contextoActual = ContextoPersonaje.Aire;
        enSuelo = false;
        _enVueloLanzado = true;

        rb.linearVelocity = Vector3.zero;
        rb.AddForce(impulso * multiplicadorImpulsoLanzado, ForceMode.VelocityChange);
        PlayAnim(animVuelo);

        // El tech solo puede activarse a partir del 70 % del hitstun.
        // Si no se recibio duracion valida, se usa techDelayInicio como fallback.
        float delayTech = duracionHitstun > 0f ? duracionHitstun * 0.5f : techDelayInicio;

        // umbral y tiempo para detectar que el personaje quedo atascado contra una pared
        const float velocidadMinLanzado = 0.4f;
        const float tiempoMaxAtascado = 0.15f;
        float tAtascado = 0f;

        float t = 0f;
        while (t < delayTech && !enSuelo)
        {
            _velocidadAnteColision = rb.linearVelocity;
            t += Time.deltaTime;

            if (rb.linearVelocity.magnitude < velocidadMinLanzado)
                tAtascado += Time.deltaTime;
            else
                tAtascado = 0f;

            if (tAtascado >= tiempoMaxAtascado) { enSuelo = true; break; }
            yield return null;
        }

        if (enSuelo)
        {
            _enVueloLanzado = false;
            yield return FinalizarLanzado();
            yield break;
        }

        ResetearTech();
        enVentanaTech = true;
        tAtascado = 0f;
        PlayAnim(animAire);

        float tVentana = 0f;
        bool techEjecutado = false;

        while (tVentana < techVentana && !enSuelo)
        {
            _velocidadAnteColision = rb.linearVelocity;
            if (TechConfirmado()) { techEjecutado = true; break; }
            tVentana += Time.deltaTime;

            if (rb.linearVelocity.magnitude < velocidadMinLanzado)
                tAtascado += Time.deltaTime;
            else
                tAtascado = 0f;

            if (tAtascado >= tiempoMaxAtascado) { enSuelo = true; break; }
            yield return null;
        }

        ResetearTech();

        if (techEjecutado)
        {
            _enVueloLanzado = false;
            float dirTech = Mathf.Sign(movimientoHorizontal);
            rb.linearVelocity = Vector3.zero;
            rb.AddForce(new Vector3(0f, techImpulsoY, dirTech * techImpulsoZ), ForceMode.VelocityChange);
            CambiarEstado(EstadoJugador.Saltar);
            PlayAnim(animTech);
            while (!enSuelo) yield return null;
            CambiarEstado(EstadoJugador.Idle);
            contextoActual = ContextoPersonaje.Suelo;
            PlayAnim(animIdle);
            yield break;
        }

        tAtascado = 0f;
        while (!enSuelo)
        {
            _velocidadAnteColision = rb.linearVelocity;

            if (rb.linearVelocity.magnitude < velocidadMinLanzado)
                tAtascado += Time.deltaTime;
            else
                tAtascado = 0f;

            if (tAtascado >= tiempoMaxAtascado) { enSuelo = true; break; }
            yield return null;
        }

        _enVueloLanzado = false;
        yield return FinalizarLanzado();
    }

    IEnumerator FinalizarLanzado()
    {
        PlayAnim(animAterrizajeDuro);
        yield return new WaitForSeconds(0.5f);
        CambiarEstado(EstadoJugador.Idle);
        contextoActual = ContextoPersonaje.Suelo;
        PlayAnim(animIdle);
    }

    IEnumerator HitstunEnWinRar()
    {
        PlayAnim(animHitstun);
        yield return new WaitForSeconds(0.5f);
        if (_enEstadoWinRar) PlayAnim(animIdle);
        _corrutinaHitstopWinRar = null;
    }

    // Corrutinas auxiliares

    void EjecutarBufferAtaque()
    {
        if (_enEstadoWinRar) return;
        if (bufferAtaque < 0) return;
        if (Time.time - bufferAtaqueTiempo > bufferAtaqueVentana) { bufferAtaque = -1; return; }
        int btn = bufferAtaque;
        bufferAtaque = -1;
        OnBotonAtaque(btn);
    }

    void EjecutarBufferAgacharse()
    {
        if (!bufferAgacharse && !abajoPresionado) return;
        bufferAgacharse = false;
        if (bufferAtaque >= 0) return;
        if (estadoActual != EstadoJugador.Idle) return;
        IniciarAgacharse();
    }

    // Bloqueo

    bool EsBloqueado(DireccionGolpe dir)
    {
        if (_enEstadoWinRar) return false;

        if (estadoActual == EstadoJugador.Atacando
         || estadoActual == EstadoJugador.Hitstun
         || estadoActual == EstadoJugador.Agarrado
         || estadoActual == EstadoJugador.Derribado
         || estadoActual == EstadoJugador.Dizzy
         || estadoActual == EstadoJugador.Dasheando) return false;

        if (movimientoHorizontal == 0f) return false;
        if (EsAdelante(movimientoHorizontal)) return false;

        if (contextoActual == ContextoPersonaje.Suelo)
            return dir == DireccionGolpe.Frente || dir == DireccionGolpe.Arriba;
        if (contextoActual == ContextoPersonaje.Agachado)
            return dir == DireccionGolpe.Frente || dir == DireccionGolpe.Abajo;

        return false;
    }

    IEnumerator AnimarBloqueo(float duracionBlockstun)
    {
        _enBlockstun = true;

        rb.linearVelocity = new Vector3(0f, Mathf.Min(rb.linearVelocity.y, 0f), 0f);
        PlayAnim(contextoActual == ContextoPersonaje.Agachado ? animBlockCrouch : animBlock);

        float t = 0f;
        while (t < duracionBlockstun)
        {
            rb.linearVelocity = new Vector3(0f, Mathf.Min(rb.linearVelocity.y, 0f), 0f);

            bool debeAgacharse = abajoPresionado && contextoActual != ContextoPersonaje.Agachado;
            bool debeLevantarse = !abajoPresionado && contextoActual == ContextoPersonaje.Agachado;

            if (debeAgacharse)
            {
                contextoActual = ContextoPersonaje.Agachado;
                CambiarEstado(EstadoJugador.Agachado);
                PlayAnim(animBlockCrouch);
            }
            else if (debeLevantarse)
            {
                contextoActual = ContextoPersonaje.Suelo;
                CambiarEstado(EstadoJugador.Idle);
                PlayAnim(animBlock);
            }

            t += Time.deltaTime;
            yield return null;
        }

        _enBlockstun = false;
        _corrutinaBloqueo = null;

        if (abajoPresionado)
        {
            CambiarEstado(EstadoJugador.Agachado);
            contextoActual = ContextoPersonaje.Agachado;
            PlayAnim(animAgachadoIdle);
        }
        else
        {
            CambiarEstado(EstadoJugador.Idle);
            contextoActual = ContextoPersonaje.Suelo;
            PlayAnim(animIdle);
        }
    }

    void CancelarAtaqueActual()
    {
        SetAllHitboxes(false);
        hitboxesActuales = null;

        if (estadoActual == EstadoJugador.Atacando)
        {
            _ataqueGeneracion++;

            if (!enSuelo)
            {
                CambiarEstado(EstadoJugador.Saltar);
                yaAtacoEnAire = false;
                EjecutarBufferAtaque();
                return;
            }

            if (abajoPresionado || bufferAgacharse)
            {
                bufferAgacharse = false;
                CambiarEstado(EstadoJugador.Agachado);
                contextoActual = ContextoPersonaje.Agachado;
                PlayAnim(animAgachadoIdle);
                EjecutarBufferAtaque();
                return;
            }

            if (contextoActual == ContextoPersonaje.Agachado)
            {
                CambiarEstado(EstadoJugador.Agachado);
                PlayAnim(animAgachadoIdle);
                EjecutarBufferAtaque();
                return;
            }
            CambiarEstado(EstadoJugador.Idle);
            PlayAnim(animIdle);
            EjecutarBufferAtaque();
            EjecutarBufferAgacharse();
        }
        else if (estadoActual == EstadoJugador.Comando)
        {
            _command1Cancelado = true;
            _comandoEnEjecucion = false;   // FIX: liberar flag para que el estado quede limpio
            CambiarEstado(EstadoJugador.Idle);
            PlayAnim(animIdle);
            EjecutarBufferAtaque();
            EjecutarBufferAgacharse();
        }
    }

    protected void ResetearBloqueo()
    {
        _enBlockstun = false;
        if (_corrutinaBloqueo != null)
        {
            StopCoroutine(_corrutinaBloqueo);
            _corrutinaBloqueo = null;
        }
        if (animator != null) animator.speed = 1f;
    }

    IEnumerator AplicarRetroceso()
    {
        if (datosPersonaje == null) yield break;
        float vel = datosPersonaje.velocidadRetroceso;
        float dur = datosPersonaje.duracionRetroceso;
        float dirRetroceso = DireccionLanzado();
        float t = 0f;
        while (t < dur)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, dirRetroceso * vel);
            t += Time.deltaTime;
            yield return null;
        }
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }

    IEnumerator VolverEstadoTrasAtaque(float tiempo, int gen)
    {
        yield return new WaitForSeconds(tiempo);
        if (estadoActual != EstadoJugador.Atacando) yield break;
        if (_ataqueGeneracion != gen) yield break;

        SetAllHitboxes(false);
        hitboxesActuales = null;

        OnVolverTrasAtaqueCallback();

        if (!enSuelo) { CambiarEstado(EstadoJugador.Saltar); yield break; }

        if (abajoPresionado || bufferAgacharse)
        {
            bufferAgacharse = false;
            CambiarEstado(EstadoJugador.Agachado);
            contextoActual = ContextoPersonaje.Agachado;
            PlayAnim(animAgachadoIdle);
            EjecutarBufferAtaque();
            yield break;
        }

        if (contextoActual == ContextoPersonaje.Agachado)
        {
            CambiarEstado(EstadoJugador.Agachado);
            PlayAnim(animAgachadoIdle);
            EjecutarBufferAtaque();
            yield break;
        }
        CambiarEstado(EstadoJugador.Idle);
        PlayAnim(animIdle);
        EjecutarBufferAtaque();
        EjecutarBufferAgacharse();
        EjecutarBufferSalto();
    }

    IEnumerator LevantarseTrasHitstun(float duracion)
    {
        yield return new WaitForSeconds(duracion);
        if (_juegoTerminado) yield break;
        CambiarEstado(EstadoJugador.Idle);
        contextoActual = ContextoPersonaje.Suelo;
        PlayAnim(animLevantarse);
        yield return new WaitForSeconds(0.5f);
        if (_juegoTerminado) yield break;
        if (estadoActual == EstadoJugador.Idle) PlayAnim(animIdle);
    }

    // Carrera

    void ComprobarDoubleTap(float dirPulsada)
    {
        if (_enEstadoWinRar) return;
        if (!EsAdelante(dirPulsada)) { PararCorrer(); return; }
        if (corriendo) return;
        float ahora = Time.time;
        if (ahora - ultimoPulsoAdelante <= ventanaDoubleTap)
        {
            if (puedeDashAdelante)
                TriggerDash(adelante: true, aereo: false);
            else
                IniciarCorrer();
        }
        else
            ultimoPulsoAdelante = ahora;
    }

    void IniciarCorrer()
    {
        if (estadoActual != EstadoJugador.Idle && estadoActual != EstadoJugador.Caminar) return;
        corriendo = true;
        CambiarEstado(EstadoJugador.Corriendo);
        PlayAnim(animCorrer);
        IniciarParticulasCorrer();
    }

    void PararCorrer()
    {
        if (!corriendo) return;
        corriendo = false;
        DetenerParticulasCorrer();
        if (estadoActual == EstadoJugador.Corriendo)
            StartCoroutine(TransicionFrenar());
    }

    IEnumerator TransicionFrenar()
    {
        CambiarEstado(EstadoJugador.Idle);
        PlayAnim(animFrenar);
        yield return new WaitForSeconds(0.25f);
        if (estadoActual == EstadoJugador.Idle) PlayAnim(animIdle);
    }

    // Hitboxes

    protected void AjustarHitbox(GameObject hitbox, Vector3? escala = null, Vector3? offset = null)
    {
        if (escala.HasValue && escala.Value != Vector3.zero)
            hitbox.transform.localScale = escala.Value;
        if (offset.HasValue && offset.Value != Vector3.zero)
            hitbox.transform.localPosition = offset.Value;

        float dir = (modeloHijo != null && modeloHijo.localScale.z > 0f) ? 1f : -1f;
        hitbox.transform.localPosition = new Vector3(
            hitbox.transform.localPosition.x,
            hitbox.transform.localPosition.y,
            dir * Mathf.Abs(hitbox.transform.localPosition.z));
    }

    protected void AjustarHitboxes(GameObject[] hitboxes, Vector3? escala = null, Vector3? offset = null)
    {
        if (hitboxes == null) return;
        foreach (var hb in hitboxes)
            if (hb != null) AjustarHitbox(hb, escala, offset);
    }

    protected void SetAllHitboxes(bool estado)
    {
        SetHitboxes(hitboxesPunoFlojo, estado);
        SetHitboxes(hitboxesPunoFuerte, estado);
        SetHitboxes(hitboxesPatadaFloja, estado);
        SetHitboxes(hitboxesPatadaFuerte, estado);
        SetHitboxes(hitboxesCommand1, estado);
        SetHitboxes(hitboxesCommand2, estado);
        SetHitboxes(hitboxesCommand3, estado);
        SetHitboxes(hitboxesCommand4, estado);
        SetHitboxes(hitboxesUltimate, estado);
        SetHitboxes(hitboxesAgarre, estado);
    }

    protected void SetHitboxes(GameObject[] hitboxes, bool estado, bool respetarSoloSuelo = false)
    {
        if (hitboxes == null) return;
        foreach (var hb in hitboxes)
        {
            if (hb == null) continue;
            if (estado && respetarSoloSuelo)
            {
                Hitbox h = hb.GetComponent<Hitbox>();
                if (h != null && h.soloSuelo && contextoActual != ContextoPersonaje.Suelo) continue;
                if (h != null && h.ignorarEnSuelo && contextoActual == ContextoPersonaje.Suelo) continue;
            }
            hb.SetActive(estado);
        }
    }

    protected void SetHitboxFlag(GameObject[] hitboxes, bool esCmd2, bool esCmd3)
    {
        if (hitboxes == null) return;
        foreach (var hb in hitboxes)
        {
            if (hb == null) continue;
            Hitbox h = hb.GetComponent<Hitbox>();
            if (h != null) { h.esCommand2 = esCmd2; h.esCommand3 = esCmd3; }
        }
    }

    bool EsBloqueoPosible()
    {
        if (movimientoHorizontal == 0f) return false;
        if (EsAdelante(movimientoHorizontal)) return false;
        return estadoActual == EstadoJugador.Idle
            || estadoActual == EstadoJugador.Caminar
            || estadoActual == EstadoJugador.Agachado
            || estadoActual == EstadoJugador.Corriendo;
    }

    // Utilidades

    protected AtaqueID HitboxesAAtaqueID(GameObject[] hitboxes)
    {
        if (hitboxes == hitboxesPunoFlojo)
        {
            if (contextoActual == ContextoPersonaje.Aire) return AtaqueID.PunoFlojoAire;
            if (contextoActual == ContextoPersonaje.Agachado) return AtaqueID.PunoFlojoAgachado;
            return AtaqueID.PunoFlojoSuelo;
        }
        if (hitboxes == hitboxesPunoFuerte)
        {
            if (contextoActual == ContextoPersonaje.Aire) return AtaqueID.PunoFuerteAire;
            if (contextoActual == ContextoPersonaje.Agachado) return AtaqueID.PunoFuerteAgachado;
            return AtaqueID.PunoFuerteSuelo;
        }
        if (hitboxes == hitboxesPatadaFloja)
        {
            if (contextoActual == ContextoPersonaje.Aire) return AtaqueID.PatadaFlojaAire;
            if (contextoActual == ContextoPersonaje.Agachado) return AtaqueID.PatadaFlojaAgachado;
            return AtaqueID.PatadaFlojaSuelo;
        }
        if (hitboxes == hitboxesPatadaFuerte)
        {
            if (contextoActual == ContextoPersonaje.Aire) return AtaqueID.PatadaFuerteAire;
            if (contextoActual == ContextoPersonaje.Agachado) return AtaqueID.PatadaFuerteAgachado;
            return AtaqueID.PatadaFuerteSuelo;
        }
        if (hitboxes == hitboxesCommand1) return AtaqueID.Comando1;
        if (hitboxes == hitboxesCommand2) return AtaqueID.Comando2;
        if (hitboxes == hitboxesCommand3) return AtaqueID.Comando3;
        if (hitboxes == hitboxesCommand4) return AtaqueID.Comando4;
        return AtaqueID.Ninguno;
    }

    protected void EstablecerAtaqueActivo(AtaqueID id)
    {
        _ataqueActivoID = id;
        if (datosPersonaje != null)
        {
            AtaqueID targets = datosPersonaje.ObtenerCanceles(id);
            ataqueEsCancelable = targets != AtaqueID.Ninguno;
        }
    }

    protected AtaqueID BotonYContextoAAtaqueID(int boton, ContextoPersonaje ctx)
    {
        switch (boton)
        {
            case 0:
                if (ctx == ContextoPersonaje.Aire) return AtaqueID.PunoFlojoAire;
                if (ctx == ContextoPersonaje.Agachado) return AtaqueID.PunoFlojoAgachado;
                return AtaqueID.PunoFlojoSuelo;
            case 1:
                if (ctx == ContextoPersonaje.Aire) return AtaqueID.PunoFuerteAire;
                if (ctx == ContextoPersonaje.Agachado) return AtaqueID.PunoFuerteAgachado;
                return AtaqueID.PunoFuerteSuelo;
            case 2:
                if (ctx == ContextoPersonaje.Aire) return AtaqueID.PatadaFlojaAire;
                if (ctx == ContextoPersonaje.Agachado) return AtaqueID.PatadaFlojaAgachado;
                return AtaqueID.PatadaFlojaSuelo;
            case 3:
                if (ctx == ContextoPersonaje.Aire) return AtaqueID.PatadaFuerteAire;
                if (ctx == ContextoPersonaje.Agachado) return AtaqueID.PatadaFuerteAgachado;
                return AtaqueID.PatadaFuerteSuelo;
        }
        return AtaqueID.Ninguno;
    }

    // Versión enriquecida: además del ID normal de ataque, suma el ID del comando
    // que correspondería a la secuencia activa para este botón.
    // Necesario para que el cancel check en OnBotonAtaque y NotificarImpactoCancelable
    // no rechace inputs que van a disparar un comando (p.ej. QCB completado + PuñoFlojo).
    protected AtaqueID BotonConSecuenciaAAtaqueID(int boton, ContextoPersonaje ctx)
    {
        AtaqueID destino = BotonYContextoAAtaqueID(boton, ctx);
        if (datosPersonaje == null) return destino;

        var c1 = datosPersonaje.secuenciaCommand1;
        if (enSuelo && SecuenciaCompleta(c1.secuencia)
            && (c1.botonActivacion < 0 || c1.botonActivacion == boton)
            && PuedeEjecutarCommand1())
            destino |= AtaqueID.Comando1;

        var c2 = datosPersonaje.secuenciaCommand2;
        if (enSuelo && SecuenciaCompleta(c2.secuencia)
            && (c2.botonActivacion < 0 || c2.botonActivacion == boton)
            && PuedeEjecutarCommand2())
            destino |= AtaqueID.Comando2;

        var c3 = datosPersonaje.secuenciaCommand3;
        if (SecuenciaCompleta(c3.secuencia)
            && (c3.botonActivacion < 0 || c3.botonActivacion == boton)
            && PuedeEjecutarCommand3())
            destino |= AtaqueID.Comando3;

        var cqcf = datosPersonaje.secuenciaCommandQCF;
        if (SecuenciaCompleta(cqcf.secuencia)
            && (cqcf.botonActivacion < 0 || cqcf.botonActivacion == boton)
            && PuedeEjecutarComandoQCF(boton))
            destino |= AtaqueID.Comando4;

        return destino;
    }

    protected string ObtenerAnimAtaque(string suelo, string agachado, string aire)
    {
        switch (contextoActual)
        {
            case ContextoPersonaje.Agachado: return agachado;
            case ContextoPersonaje.Aire: return aire;
            default: return suelo;
        }
    }

    protected void PlayAnim(string nombre)
    {
        if (animator == null) return;
        animator.Play(nombre, 0, 0f);
    }

    protected float ObtenerDuracionClip(string nombreAnim, float fallback = 0.5f)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return fallback;
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            if (clip.name == nombreAnim && clip.length > 0f) return clip.length;
        return fallback;
    }

    // Colisiones

    private void OnCollisionEnter(Collision other)
    {
        bool esLanzado = (estadoActual == EstadoJugador.Hitstun && !enSuelo);

        if (_enVueloLanzado && esLanzado)
        {
            foreach (ContactPoint cp in other.contacts)
            {
                if (_reboteSuelo && cp.normal.y > 0.7f)
                {
                    _reboteSuelo = false;
                    Vector3 rebote = Vector3.Reflect(_velocidadAnteColision, cp.normal);
                    rb.linearVelocity = new Vector3(0f, rebote.y * 0.6f, rebote.z * 0.6f);
                    return;
                }
                if (Mathf.Abs(cp.normal.y) < 0.5f)
                {
                    // si el personaje ya va muy lento, ignorar el cooldown para que
                    // siempre reciba el empujon que lo saca de la pared
                    bool casiParado = _velocidadAnteColision.magnitude < 1.5f;
                    if (!casiParado && Time.time - _tiempoUltimoRebotePared < _cooldownRebotePared) return;
                    _tiempoUltimoRebotePared = Time.time;

                    if (casiParado)
                    {
                        rb.linearVelocity = new Vector3(0f, 1f, cp.normal.z * 4f);
                        return;
                    }
                    Vector3 rebote = Vector3.Reflect(_velocidadAnteColision, cp.normal);
                    rb.linearVelocity = new Vector3(0f, rebote.y * 0.6f, rebote.z * 0.6f);
                    return;
                }
            }
        }

        if (!esLanzado)
        {
            if (recienSaltado) return;
            if (Time.time - tiempoSalto < minTiempoEnAire) return;
        }
        // Red de seguridad: ignorar colisiones con el cuerpo del oponente
        // para que pisar su cabeza no cuente como suelo.
        if (oponente != null && other.transform.IsChildOf(oponente)) return;
        foreach (ContactPoint cp in other.contacts)
            if (cp.normal.y > 0.7f) { Aterrizar(); return; }
    }

    private void OnCollisionExit(Collision other) => recienSaltado = false;

    private void OnCollisionStay(Collision other)
    {
        if (recienSaltado && rb.linearVelocity.y < 0f) recienSaltado = false;
    }

    void Aterrizar()
    {
        enSuelo = true;
        recienSaltado = false;
        yaAtacoEnAire = false;
        _dobleSaltoUsado = false;
        _dashAereoUsado = false;

        // Particulas de aterrizaje en los pies
        SpawnParticulasSimple(prefabParticulasAterrizaje, transform.position);
        if (estadoActual != EstadoJugador.Agachado)
            contextoActual = ContextoPersonaje.Suelo;

        // no interrumpir la animacion de victoria al tocar el suelo
        if (_juegoTerminado) return;

        // EstadoJugador.Comando se elimina de aqui: los comandos gestionan
        // su propio estado al terminar (FinalizarComando / Dizzy).
        // Asi Command3 de Cube sigue ejecutandose al tocar suelo en el aire.
        if (estadoActual == EstadoJugador.Saltar
         || estadoActual == EstadoJugador.Atacando
         || estadoActual == EstadoJugador.Idle
         || estadoActual == EstadoJugador.Dasheando
         || estadoActual == EstadoJugador.Hitstun)
        {
            // FIX: si el jugador mantiene abajo (o hay buffer de agacharse)
            // al aterrizar, pasar directamente a Agachado en lugar de Idle.
            if (abajoPresionado || bufferAgacharse)
            {
                bufferAgacharse = false;
                CambiarEstado(EstadoJugador.Agachado);
                contextoActual = ContextoPersonaje.Agachado;
                PlayAnim(animAgachadoIdle);
            }
            else
            {
                CambiarEstado(EstadoJugador.Idle);
                PlayAnim(animIdle);
            }
        }
    }

    // Sistema de agarre

    void IniciarAgarre()
    {
        if (_juegoTerminado) return;
        if (_enEstadoWinRar) return;
        if (_enBlockstun) return;
        if (estadoActual != EstadoJugador.Idle
         && estadoActual != EstadoJugador.Caminar
         && estadoActual != EstadoJugador.Corriendo) return;

        _esHaciaAtrasAgarre = movimientoHorizontal != 0f && !EsAdelante(movimientoHorizontal);
        float dirHaciaOponente = (oponente != null)
            ? Mathf.Sign(oponente.position.z - transform.position.z)
            : _signoOrientacion;
        _dirZLanzamientoAgarre = _esHaciaAtrasAgarre ? -dirHaciaOponente : dirHaciaOponente;

        _agarreConectado = false;
        _esperandoSoltarAgarre = false;
        _personajeAgarrado = null;

        StartCoroutine(CorrutinaAgarre());
    }

    IEnumerator CorrutinaAgarre()
    {
        _intentandoAgarrar = true;
        corriendo = false;
        ResetearBloqueo();
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        CambiarEstado(EstadoJugador.Comando);
        contextoActual = ContextoPersonaje.Suelo;

        if (hitboxesAgarre != null)
        {
            foreach (var hb in hitboxesAgarre)
            {
                if (hb == null) continue;
                Hitbox h = hb.GetComponent<Hitbox>();
                if (h != null) h.esAgarre = true;
            }
        }

        hitboxesActuales = hitboxesAgarre;
        PlayAnim(animAgarre);
        PrepararHitboxes(hitboxesAgarre);

        float ventana = datosPersonaje != null ? datosPersonaje.duracionIntentaAgarre : 0.5f;
        float t = 0f;
        while (t < ventana && !_agarreConectado)
        {
            t += Time.deltaTime;
            yield return null;
        }

        _intentandoAgarrar = false;
        SetHitboxes(hitboxesAgarre, false);
        hitboxesActuales = null;

        if (!_agarreConectado)
        {
            CambiarEstado(EstadoJugador.Idle);
            PlayAnim(animIdle);
            yield break;
        }

        PlayAnim(animAgarreHold);
        _esperandoSoltarAgarre = true;

        if (_esHaciaAtrasAgarre && modeloHijo != null)
        {
            _signoOrientacion = -_signoOrientacion;
            _tiempoUltimoFlip = Time.time;
            if (_signoOrientacion > 0f)
                modeloHijo.localScale = new Vector3(escalaOriginalModelo.x, escalaOriginalModelo.y, Mathf.Abs(escalaOriginalModelo.z));
            else
                modeloHijo.localScale = new Vector3(escalaOriginalModelo.x, escalaOriginalModelo.y, -Mathf.Abs(escalaOriginalModelo.z));
            AplicarOrientacionAColisiones();
        }

        yield return null;

        while (_esperandoSoltarAgarre && _personajeAgarrado != null)
        {
            Vector3 posAgarre = puntoAgarre != null
                ? puntoAgarre.position
                : transform.position + new Vector3(0f, 0.5f, _dirZLanzamientoAgarre * 0.6f);

            _personajeAgarrado.FijarPosicionAgarre(posAgarre);

            if (animator != null)
            {
                AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
                if (info.IsName(animAgarreHold) && info.normalizedTime >= 0.95f && !animator.IsInTransition(0))
                    AnimEvent_SoltarAgarre();
            }

            yield return null;
        }

        _personajeAgarrado = null;
        _agarreConectado = false;
        _esperandoSoltarAgarre = false;
        CambiarEstado(EstadoJugador.Idle);
        PlayAnim(animIdle);
    }

    private void ClashDeAgarre()
    {
        if (_enClash) return;
        _enClash = true;
        _intentandoAgarrar = false;
        StopAllCoroutines();
        _corrutinaDash = null;
        _comandoEnEjecucion = false;  // reset manual porque StopAllCoroutines mató FinalizarComando
        _ataqueGeneracion++;           // invalida VolverEstadoTrasAtaque pendientes
        SetAllHitboxes(false);
        hitboxesActuales = null;
        LimpiarEstadoAgarrador();
        LimpiarEstadoAgarrado();
        ResetearBloqueo();
        corriendo = false;

        CambiarEstado(EstadoJugador.Hitstun);
        contextoActual = ContextoPersonaje.Suelo;
        PlayAnim(animHitstun);
        StartCoroutine(ImpulsoClashAgarre());
    }

    IEnumerator ImpulsoClashAgarre()
    {
        float velocidadBase = datosPersonaje != null
            ? datosPersonaje.velocidadRetroceso * multiplicadorClashSobreRetroceso
            : velocidadBaseClash;
        float dirRetroceso = DireccionLanzado();

        float t = 0f;
        while (t < duracionClash)
        {
            float factor = 1f - (t / duracionClash);
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, dirRetroceso * velocidadBase * factor);
            t += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

        float timeout = 1.5f;
        while (!enSuelo && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        yield return LevantarseTrasHitstun(0.15f);
        _enClash = false;
    }

    public void NotificarAgarreImpacto(PersonajeBase agarrado)
    {
        if (_agarreConectado) return;
        _agarreConectado = true;
        _personajeAgarrado = agarrado;
    }

    public void RecibirAgarre(PersonajeBase agarrador)
    {
        if (_enClash) return;
        if (estadoActual == EstadoJugador.Agarrado) return;
        if (estaDerribado) return;
        if (_juegoTerminado) return;
        if (agarrador == null) return;

        if (_intentandoAgarrar)
        {
            ClashDeAgarre();
            agarrador.ClashDeAgarre();
            return;
        }
        LimpiarEstadoAgarrador();

        StopAllCoroutines();
        _corrutinaDash = null;
        SetAllHitboxes(false);
        ResetearBloqueo();
        corriendo = false;

        _agarradorActual = agarrador;
        CambiarEstado(EstadoJugador.Agarrado);
        contextoActual = ContextoPersonaje.Suelo;

        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;

        PlayAnim(animHurt2);

        agarrador.NotificarAgarreImpacto(this);
    }

    public void FijarPosicionAgarre(Vector3 posAgarre)
    {
        transform.position = new Vector3(transform.position.x, posAgarre.y, posAgarre.z);
    }

    public void LiberarDeAgarre(float impulsoY, float impulsoZ, float dirZ,
                            float duracionHitstun, float dano,
                            bool permitirJuggle = false)
    {
        if (estadoActual != EstadoJugador.Agarrado) return;

        _agarradorActual = null;
        rb.isKinematic = false;
        enSuelo = false;
        recienSaltado = false;
        tiempoSalto = Time.time - minTiempoEnAire;

        if (dano > 0f) AplicarDano(dano);

        rb.linearVelocity = new Vector3(0f, impulsoY, dirZ * impulsoZ);

        if (permitirJuggle)
        {
            estaDerribado = false;
            CambiarEstado(EstadoJugador.Hitstun);
            contextoActual = ContextoPersonaje.Aire;
            StartCoroutine(SecuenciaLanzado(new Vector3(0f, impulsoY, dirZ * impulsoZ), animLanzadoArriba, duracionHitstun));
        }
        else
        {
            estaDerribado = true;
            CambiarEstado(EstadoJugador.Derribado);
            contextoActual = ContextoPersonaje.Aire;
            StartCoroutine(SecuenciaCaidaAgarre(duracionHitstun));
        }
    }

    IEnumerator SecuenciaCaidaAgarre(float duracionHitstun)
    {
        try
        {
            PlayAnim(animDead);

            float tiempoEspera = 0f;
            while (!enSuelo && tiempoEspera < 5f)
            {
                tiempoEspera += Time.deltaTime;
                yield return null;
            }

            enSuelo = true;
            contextoActual = ContextoPersonaje.Suelo;
            PlayAnim(animDead);
            yield return new WaitForSeconds(duracionHitstun);

            estaDerribado = false;
            CambiarEstado(EstadoJugador.Idle);
            PlayAnim(animLevantarse);
            yield return new WaitForSeconds(0.5f);
            PlayAnim(animIdle);
        }
        finally
        {
            // garantiza reset aunque la corrutina sea interrumpida
            estaDerribado = false;
            if (rb != null) rb.isKinematic = false;
        }
    }

    private void LimpiarEstadoAgarrador()
    {
        if (_personajeAgarrado != null)
        {
            _personajeAgarrado.LiberarDeAgarre(2f, 2f, _signoOrientacion, 0.5f, 0f);
            _personajeAgarrado = null;
        }
        _agarreConectado = false;
        _esperandoSoltarAgarre = false;
    }

    private void LimpiarEstadoAgarrado()
    {
        if (estadoActual == EstadoJugador.Agarrado)
        {
            _agarradorActual = null;
            rb.isKinematic = false;
            estaDerribado = false;   // añadido
            CambiarEstado(EstadoJugador.Idle);
            contextoActual = ContextoPersonaje.Suelo;
            PlayAnim(animIdle);
        }
    }

    // Hitstop

    public void IniciarHitstop(float duracion)
    {
        if (animator == null || !gameObject.activeInHierarchy) return;
        if (_corrutinaHitstop != null) StopCoroutine(_corrutinaHitstop);
        _corrutinaHitstop = StartCoroutine(CorrutinaHitstop(duracion));
    }

    IEnumerator CorrutinaHitstop(float duracion)
    {
        if (animator != null) animator.speed = 0f;
        yield return new WaitForSecondsRealtime(duracion);
        if (animator != null && animator.speed == 0f) animator.speed = 1f;
        _corrutinaHitstop = null;
    }

    // Combo Counter

    public bool EstaEnEstadoCombo()
    {
        return estadoActual == EstadoJugador.Hitstun
            || estadoActual == EstadoJugador.Derribado
            || estadoActual == EstadoJugador.Agarrado;
    }

    float CalcularEscalaDanio()
    {
        if (_contadorCombo <= 0) return 1f;
        return Mathf.Max(comboEscaladoMinimo, 1f - _contadorCombo * comboEscaladoPorGolpe);
    }

    void IncrementarCombo()
    {
        if (_corrutinaDesvanecerCombo != null)
        {
            StopCoroutine(_corrutinaDesvanecerCombo);
            _corrutinaDesvanecerCombo = null;
        }

        _contadorCombo++;
        _ultimoComboMostrado = _contadorCombo;
        ActualizarUICombo();

        if (_corrutinaVigilarCombo != null) StopCoroutine(_corrutinaVigilarCombo);
        _corrutinaVigilarCombo = StartCoroutine(VigilarFinCombo());
    }

    IEnumerator VigilarFinCombo()
    {
        yield return null;

        PersonajeBase objetivo = _oponenteBase;
        if (objetivo == null)
        {
            _corrutinaVigilarCombo = null;
            yield break;
        }

        while (objetivo.EstaEnEstadoCombo())
            yield return null;

        _corrutinaVigilarCombo = null;
        int comboFinal = _ultimoComboMostrado;
        _contadorCombo = 0;
        _corrutinaDesvanecerCombo = StartCoroutine(DesvanecerCombo(comboFinal >= 10));
    }

    void ActualizarUICombo()
    {
        if (_spritesNumeros == null || _spritesNumeros.Length < 10) return;

        int unidades = _contadorCombo % 10;
        int decenas = (_contadorCombo / 10) % 10;

        if (_imagenUnidades != null)
        {
            _imagenUnidades.sprite = _spritesNumeros[unidades];
            Color c = _imagenUnidades.color;
            _imagenUnidades.color = new Color(c.r, c.g, c.b, 1f);
        }

        if (_imagenDecenas != null)
        {
            _imagenDecenas.sprite = _spritesNumeros[decenas];
            Color c = _imagenDecenas.color;
            _imagenDecenas.color = new Color(c.r, c.g, c.b, _contadorCombo >= 10 ? 1f : 0f);
        }
    }

    IEnumerator DesvanecerCombo(bool mostrarDecenas)
    {
        float t = 0f;
        while (t < duracionDesvanecerCombo)
        {
            float alpha = 1f - (t / duracionDesvanecerCombo);

            if (_imagenUnidades != null)
            {
                Color c = _imagenUnidades.color;
                _imagenUnidades.color = new Color(c.r, c.g, c.b, alpha);
            }
            if (_imagenDecenas != null && mostrarDecenas)
            {
                Color c = _imagenDecenas.color;
                _imagenDecenas.color = new Color(c.r, c.g, c.b, alpha);
            }

            t += Time.deltaTime;
            yield return null;
        }

        OcultarUIComboInmediatamente();
        _corrutinaDesvanecerCombo = null;
    }

    void OcultarUIComboInmediatamente()
    {
        if (_imagenDecenas != null)
        {
            Color c = _imagenDecenas.color;
            _imagenDecenas.color = new Color(c.r, c.g, c.b, 0f);
        }
        if (_imagenUnidades != null)
        {
            Color c = _imagenUnidades.color;
            _imagenUnidades.color = new Color(c.r, c.g, c.b, 0f);
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("instakill")) return;
        if (_juegoTerminado) return;
        if (estaDerribado) return;

        // Vaciar la vida directamente y dejar que SaludJugador gestione la muerte normal
        if (salud != null)
            salud.RecibirDano(999999f);
    }
    void ResetearContadorComboSilencioso()
    {
        if (_contadorCombo == 0 && _corrutinaVigilarCombo == null && _corrutinaDesvanecerCombo == null)
            return;

        if (_corrutinaVigilarCombo != null)
        {
            StopCoroutine(_corrutinaVigilarCombo);
            _corrutinaVigilarCombo = null;
        }
        if (_corrutinaDesvanecerCombo != null)
        {
            StopCoroutine(_corrutinaDesvanecerCombo);
            _corrutinaDesvanecerCombo = null;
        }

        _contadorCombo = 0;
        OcultarUIComboInmediatamente();
    }

    // Gizmos

    void OnDrawGizmos()
    {
        DrawHitboxesGizmo(hitboxesPunoFlojo, Color.red);
        DrawHitboxesGizmo(hitboxesPunoFuerte, Color.blue);
        DrawHitboxesGizmo(hitboxesPatadaFloja, Color.green);
        DrawHitboxesGizmo(hitboxesPatadaFuerte, Color.yellow);
        DrawHitboxesGizmo(hitboxesCommand1, Color.cyan);
        DrawHitboxesGizmo(hitboxesCommand2, Color.magenta);
        DrawHitboxesGizmo(hitboxesCommand3, new Color(1f, 0.5f, 0f));
        DrawHitboxesGizmo(hitboxesCommand4, new Color(0f, 0.8f, 0.4f));
        DrawHitboxesGizmo(hitboxesUltimate, new Color(1f, 0.84f, 0f));
        DrawHitboxesGizmo(hitboxesAgarre, new Color(1f, 0f, 1f));
    }

    void DrawHitboxesGizmo(GameObject[] hitboxes, Color color)
    {
        if (hitboxes == null) return;
        foreach (var hb in hitboxes)
        {
            if (hb == null || !hb.activeInHierarchy) continue;
            Gizmos.color = color;
            BoxCollider bc = hb.GetComponent<BoxCollider>();
            if (bc != null)
            {
                Matrix4x4 prev = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(
                    hb.transform.TransformPoint(bc.center),
                    hb.transform.rotation,
                    hb.transform.lossyScale);
                Gizmos.DrawWireCube(Vector3.zero, bc.size);
                Gizmos.matrix = prev;
            }
            else
            {
                Gizmos.DrawWireCube(hb.transform.position, hb.transform.localScale);
            }
        }
    }
}