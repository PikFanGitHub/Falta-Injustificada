using System.Collections.Generic;
using UnityEngine;

public class PersonajeDataBase : ScriptableObject
{
    // movimiento
    [Header("Movimiento")]
    public float velocidadMovimiento = 5f;
    public float velocidadCarrera = 9f;
    public float fuerzaSalto = 12f;

    // dano
    [Header("Dano Puno Flojo")]
    public float danoPunoFlojoSuelo = 3f;
    public float danoPunoFlojoAgachado = 2f;
    public float danoPunoFlojoAire = 3f;

    [Header("Dano Puno Fuerte")]
    public float danoPunoFuerteSuelo = 15f;
    public float danoPunoFuerteAgachado = 10f;
    public float danoPunoFuerteAire = 15f;

    [Header("Dano Patada Floja")]
    public float danoPatadaFlojaSuelo = 5f;
    public float danoPatadaFlojaAgachado = 7f;
    public float danoPatadaFlojaAire = 12f;

    [Header("Dano Patada Fuerte")]
    public float danoPatadaFuerteSuelo = 17f;
    public float danoPatadaFuerteAgachado = 15f;
    public float danoPatadaFuerteAire = 20f;

    // hitstun
    [Header("Hitstun Puno Flojo (segundos en stun)")]
    public float hitstunPunoFlojoSuelo = 0.4f;
    public float hitstunPunoFlojoAgachado = 0.4f;
    public float hitstunPunoFlojoAire = 0.4f;

    [Header("Hitstun Puno Fuerte")]
    public float hitstunPunoFuerteSuelo = 0.5f;
    public float hitstunPunoFuerteAgachado = 0.5f;
    public float hitstunPunoFuerteAire = 0.5f;

    [Header("Hitstun Patada Floja")]
    public float hitstunPatadaFlojaSuelo = 0.4f;
    public float hitstunPatadaFlojaAgachado = 0.4f;
    public float hitstunPatadaFlojaAire = 0.4f;

    [Header("Hitstun Patada Fuerte")]
    public float hitstunPatadaFuerteSuelo = 0.6f;
    public float hitstunPatadaFuerteAgachado = 0.6f;
    public float hitstunPatadaFuerteAire = 0.6f;

    [Header("Hitstun Comandos")]
    public float hitstunComando1 = 0.5f;
    public float hitstunComando2 = 0.4f;
    public float hitstunComando3 = 0.5f;

    // hitboxes - Escala y Offset
    [Header("Hitboxes - Escala y Offset Puno Flojo")]
    public Vector3 scalePunoFlojoSuelo = new Vector3(0.4f, 0.4f, 0.8f);
    public Vector3 offsetPunoFlojoSuelo = new Vector3(0f, 0.5f, 0.6f);
    public Vector3 scalePunoFlojoAgachado = new Vector3(0.4f, 0.4f, 0.8f);
    public Vector3 offsetPunoFlojoAgachado = new Vector3(0f, 0.1f, 0.6f);
    public Vector3 scalePunoFlojoAire = new Vector3(0.4f, 0.4f, 0.8f);
    public Vector3 offsetPunoFlojoAire = new Vector3(0f, 0.2f, 0.6f);

    [Header("Hitboxes - Escala y Offset Puno Fuerte")]
    public Vector3 scalePunoFuerteSuelo = new Vector3(0.4f, 0.4f, 1.1f);
    public Vector3 offsetPunoFuerteSuelo = new Vector3(0f, 0.5f, 0.8f);
    public Vector3 scalePunoFuerteAgachado = new Vector3(0.4f, 0.4f, 1.1f);
    public Vector3 offsetPunoFuerteAgachado = new Vector3(0f, 0.1f, 0.8f);
    public Vector3 scalePunoFuerteAire = new Vector3(0.4f, 0.4f, 1.1f);
    public Vector3 offsetPunoFuerteAire = new Vector3(0f, 0.2f, 0.8f);

    [Header("Hitboxes - Escala y Offset Patada Floja")]
    public Vector3 scalePatadaFlojaSuelo = new Vector3(0.4f, 0.35f, 1.0f);
    public Vector3 offsetPatadaFlojaSuelo = new Vector3(0f, 0.2f, 0.8f);
    public Vector3 scalePatadaFlojaAgachado = new Vector3(0.4f, 0.35f, 1.0f);
    public Vector3 offsetPatadaFlojaAgachado = new Vector3(0f, 0.1f, 0.8f);
    public Vector3 scalePatadaFlojaAire = new Vector3(0.4f, 0.35f, 1.0f);
    public Vector3 offsetPatadaFlojaAire = new Vector3(0f, -0.2f, 0.8f);

    [Header("Hitboxes - Escala y Offset Patada Fuerte")]
    public Vector3 scalePatadaFuerteSuelo = new Vector3(0.4f, 0.35f, 1.3f);
    public Vector3 offsetPatadaFuerteSuelo = new Vector3(0f, 0.2f, 1.0f);
    public Vector3 scalePatadaFuerteAgachado = new Vector3(0.4f, 0.35f, 1.3f);
    public Vector3 offsetPatadaFuerteAgachado = new Vector3(0f, 0.1f, 1.0f);
    public Vector3 scalePatadaFuerteAire = new Vector3(0.4f, 0.35f, 1.3f);
    public Vector3 offsetPatadaFuerteAire = new Vector3(0f, -0.2f, 1.0f);

    [Header("Hitboxes - Escala y Offset Comandos")]
    public Vector3 scaleCommand1 = new Vector3(0.4f, 0.5f, 1.0f);
    public Vector3 offsetCommand1 = new Vector3(0f, 0.5f, 0.8f);
    public Vector3 scaleCommand2 = new Vector3(0.4f, 0.5f, 0.5f);
    public Vector3 offsetCommand2 = new Vector3(0f, 1.0f, 0f);
    public Vector3 scaleCommand3 = new Vector3(0.4f, 0.35f, 1.3f);
    public Vector3 offsetCommand3 = new Vector3(0f, 0.5f, 1.0f);

    // tipo de golpe
    [Header("Tipo de golpe por ataque (reaccion que provoca)")]
    public TipoGolpe tipoGolpePunoFlojoSuelo = TipoGolpe.Normal;
    public TipoGolpe tipoGolpePunoFlojoAgachado = TipoGolpe.Normal;
    public TipoGolpe tipoGolpePunoFlojoAire = TipoGolpe.Normal;
    public TipoGolpe tipoGolpePunoFuerteSuelo = TipoGolpe.Normal;
    public TipoGolpe tipoGolpePunoFuerteAgachado = TipoGolpe.Derribar;
    public TipoGolpe tipoGolpePunoFuerteAire = TipoGolpe.LanzarArriba;
    public TipoGolpe tipoGolpePatadaFlojaSuelo = TipoGolpe.Normal;
    public TipoGolpe tipoGolpePatadaFlojaAgachado = TipoGolpe.HitDerribado;
    public TipoGolpe tipoGolpePatadaFlojaAire = TipoGolpe.LanzarFrente;
    public TipoGolpe tipoGolpePatadaFuerteSuelo = TipoGolpe.Derribar;
    public TipoGolpe tipoGolpePatadaFuerteAgachado = TipoGolpe.Normal;
    public TipoGolpe tipoGolpePatadaFuerteAire = TipoGolpe.Normal;

    [Header("Tipo de golpe por comando")]
    public TipoGolpe tipoGolpeComando1 = TipoGolpe.LanzarFrente;
    public TipoGolpe tipoGolpeComando2 = TipoGolpe.Normal;
    public TipoGolpe tipoGolpeComando3 = TipoGolpe.LanzarAbajo;

    // impulsos de lanzado
    [Header("Fuerza de lanzados Puno Flojo (Y = altura, Z = distancia)")]
    public float impulsoYPunoFlojoSuelo = 4f;
    public float impulsoZPunoFlojoSuelo = 4f;
    public float impulsoYPunoFlojoAgachado = 4f;
    public float impulsoZPunoFlojoAgachado = 4f;
    public float impulsoYPunoFlojoAire = 4f;
    public float impulsoZPunoFlojoAire = 4f;

    [Header("Fuerza de lanzados Puno Fuerte")]
    public float impulsoYPunoFuerteSuelo = 8f;
    public float impulsoZPunoFuerteSuelo = 3f;
    public float impulsoYPunoFuerteAgachado = 12f;
    public float impulsoZPunoFuerteAgachado = 2f;
    public float impulsoYPunoFuerteAire = 12f;
    public float impulsoZPunoFuerteAire = 3f;

    [Header("Fuerza de lanzados Patada Floja")]
    public float impulsoYPatadaFlojaSuelo = 4f;
    public float impulsoZPatadaFlojaSuelo = 8f;
    public float impulsoYPatadaFlojaAgachado = 4f;
    public float impulsoZPatadaFlojaAgachado = 8f;
    public float impulsoYPatadaFlojaAire = 4f;
    public float impulsoZPatadaFlojaAire = 10f;

    [Header("Fuerza de lanzados Patada Fuerte")]
    public float impulsoYPatadaFuerteSuelo = 4f;
    public float impulsoZPatadaFuerteSuelo = 6f;
    public float impulsoYPatadaFuerteAgachado = 4f;
    public float impulsoZPatadaFuerteAgachado = 6f;
    public float impulsoYPatadaFuerteAire = 4f;
    public float impulsoZPatadaFuerteAire = 6f;

    [Header("Fuerza de lanzados Comandos")]
    public float impulsoYComando1 = 4f;
    public float impulsoZComando1 = 10f;
    public float impulsoYComando2 = 4f;
    public float impulsoZComando2 = 6f;
    public float impulsoYComando3 = -10f;
    public float impulsoZComando3 = 5f;

    // direccion del golpe
    [Header("Direccion del golpe por ataque - configurable por personaje")]
    public DireccionGolpe direccionPunoFlojoSuelo = DireccionGolpe.Frente;
    public DireccionGolpe direccionPunoFlojoAgachado = DireccionGolpe.Abajo;
    public DireccionGolpe direccionPunoFlojoAire = DireccionGolpe.Arriba;
    public DireccionGolpe direccionPunoFuerteSuelo = DireccionGolpe.Frente;
    public DireccionGolpe direccionPunoFuerteAgachado = DireccionGolpe.Abajo;
    public DireccionGolpe direccionPunoFuerteAire = DireccionGolpe.Arriba;
    public DireccionGolpe direccionPatadaFlojaSuelo = DireccionGolpe.Frente;
    public DireccionGolpe direccionPatadaFlojaAgachado = DireccionGolpe.Abajo;
    public DireccionGolpe direccionPatadaFlojaAire = DireccionGolpe.Arriba;
    public DireccionGolpe direccionPatadaFuerteSuelo = DireccionGolpe.Arriba;
    public DireccionGolpe direccionPatadaFuerteAgachado = DireccionGolpe.Abajo;
    public DireccionGolpe direccionPatadaFuerteAire = DireccionGolpe.Arriba;

    // animaciones de ataques normales
    [Header("Puno Flojo (L)")]
    public string animPunoFlojoSuelo = "PunchLS";
    public string animPunoFlojoAgachado = "PunchLC";
    public string animPunoFlojoAire = "PunchLA";

    [Header("Puno Fuerte (M)")]
    public string animPunoFuerteSuelo = "PunchMS";
    public string animPunoFuerteAgachado = "PunchMC";
    public string animPunoFuerteAire = "PunchMA";

    [Header("Patada Floja (L)")]
    public string animPatadaFlojoSuelo = "KickLS";
    public string animPatadaFlojoAgachado = "KickLC";
    public string animPatadaFlojoAire = "KickLA";

    [Header("Patada Fuerte (M)")]
    public string animPatadaFuerteSuelo = "KickMS";
    public string animPatadaFuerteAgachado = "KickMC";
    public string animPatadaFuerteAire = "KickMA";

    // comandos - campos comunes
    [Header("Command1 - QCB + Puno Flojo")]
    public float danoComando1 = 15f;
    public float duracionComando1 = 0.8f;

    [Header("Command2 - QCB + Puno Fuerte")]
    public float danoComando2 = 12f;

    [Header("Command3 - QCB + Patada Fuerte")]
    public float danoComando3 = 20f;
    public float duracionComando3 = 1.0f;

    // retroceso al conectar un golpe
    [Header("Retroceso al golpear")]
    [Tooltip("Velocidad con la que el personaje retrocede al conectar un hit")]
    public float velocidadRetroceso = 3f;
    [Tooltip("Segundos que dura el impulso de retroceso")]
    public float duracionRetroceso = 0.08f;

    // agarre
    [Header("Agarre")]
    [Tooltip("Segundos de ventana para que la hitbox de agarre conecte antes de que falle")]
    public float duracionIntentaAgarre = 0.5f;
    [Tooltip("Dano que recibe el enemigo al ser lanzado")]
    public float danoAgarre = 10f;
    [Tooltip("Impulso vertical al soltar al enemigo")]
    public float impulsoYAgarre = 6f;
    [Tooltip("Impulso horizontal al soltar al enemigo")]
    public float impulsoZAgarre = 8f;
    [Tooltip("Segundos que el enemigo permanece en hitstun Dead al caer al suelo")]
    public float duracionHitstunAgarre = 2f;

    // reglas de Cancel
    [Header("Reglas de Cancel por Impacto")]
    [Tooltip("Define para cada ataque cuales otros se pueden encadenar al conectar.\n" +
             "Si un ataque no aparece aqui, no podra ser cancelado.\n\n" +
             "Ejemplo:\n" +
             "  Ataque Fuente   → PunoFlojoSuelo\n" +
             "  Canceles        → PatadaFlojaAgachado | PatadaFuerteSuelo")]
    public List<CancelRule> reglasCancel = new List<CancelRule>();

    // sonido de muerte de ronda
    [Header("Sonido de muerte de ronda")]
    [Tooltip("Clip que se reproduce cuando este personaje pierde una ronda (muere). Opcional.")]
    public AudioClip sonidoMuerteRonda;

    // ultimate
    [Header("Ultimate - Carga por golpe conectado")]
    [Tooltip("Cantidad de carga que suma cada golpe al medidor de ultimate del atacante.")]
    public float cargaUltimatePorGolpe = 10f;

    // devuelve la mascara de ataques destino permitidos para cancelar
    // el ataque indicado. Si no hay regla definida devuelve Ninguno.
    public AtaqueID ObtenerCanceles(AtaqueID fuente)
    {
        if (reglasCancel == null) return AtaqueID.Ninguno;
        foreach (var r in reglasCancel)
            if (r.ataqueFuente == fuente) return r.cancelesPermitidos;
        return AtaqueID.Ninguno;
    }

    // secuencias de Comandos
    // los defaults reproducen el comportamiento original.
    // cambia secuencia y/o botonActivacion en cada asset para personalizar.
    [Header("Secuencias de Comandos")]
    [Tooltip("Command1. Por defecto: QCB + Puño Flojo. Solo suelo.")]
    public ConfiguracionComando secuenciaCommand1 = new ConfiguracionComando
    { secuencia = TipoSecuencia.QCB, botonActivacion = 0 };

    [Tooltip("Command2. Por defecto: QCB + Puño Fuerte. Solo suelo.")]
    public ConfiguracionComando secuenciaCommand2 = new ConfiguracionComando
    { secuencia = TipoSecuencia.QCB, botonActivacion = 1 };

    [Tooltip("Command3. Por defecto: QCB + Patada Fuerte. Suelo y aire.")]
    public ConfiguracionComando secuenciaCommand3 = new ConfiguracionComando
    { secuencia = TipoSecuencia.QCB, botonActivacion = 3 };

    [Tooltip("Comando QCF generico (EjecutarComandoQCF). botonActivacion = -1 acepta cualquier boton.")]
    public ConfiguracionComando secuenciaCommandQCF = new ConfiguracionComando
    { secuencia = TipoSecuencia.QCF, botonActivacion = -1 };

    [Tooltip("Ultimate. Por defecto: Doble QCF + Puño Fuerte.\n" +
             "Si la subclase sobreescribe BotonActivadorUltimate, ese valor tiene prioridad sobre botonActivacion.")]
    public ConfiguracionComando secuenciaUltimate = new ConfiguracionComando
    { secuencia = TipoSecuencia.DobleQCF, botonActivacion = 1 };
}