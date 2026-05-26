using UnityEngine;

[CreateAssetMenu(fileName = "MonteroData", menuName = "Scriptable Objects/MonteroData")]
public class MonteroData : PersonajeDataBase
{
    //pelota - QCF aereo
    [Header("Pelota — QCF (solo aereo, diagonal abajo-adelante)")]
    public float danoPelota = 15f;
    public float duracionPelota = 1.4f;
    public float velocidadPelota = 12f;
    public float impulsoYPelota = 8f;   // se aplica negativo en codigo (hacia abajo)
    public float hitstunPelota = 0.5f;
    [Tooltip("Segundos que dura la animacion Command2-Start antes de empezar a moverse")]
    public float duracionAnimPelotaStart = 0.5f;

    //dash - Command3
    [Header("Dash — Command3 (QCB + Puno Fuerte)")]
    [Tooltip("Distancia en unidades que recorre el dash hacia el oponente")]
    public float distanciaDash = 3.6f;
    [Tooltip("Velocidad de desplazamiento durante el dash")]
    public float velocidadDash = 18f;
    [Tooltip("Segundos que la hitbox del golpe permanece activa tras el dash")]
    public float duracionGolpeDash = 0.35f;

    //golpe Adelante - Command4
    [Header("Golpe Adelante — Command4 (QCB + Patada Fuerte)")]
    [Tooltip("Distancia en unidades que recorre el dash antes del golpe")]
    public float distanciaGolpeAdelante = 2.5f;
    [Tooltip("Velocidad de desplazamiento durante el dash del golpe adelante")]
    public float velocidadGolpeAdelante = 15f;

    //rezo (Command1)
    [Header("Rezo — Command1")]
    [Tooltip("Cuántas veces hay que rezar para activar el buff")]
    public int rezosNecesarios = 3;
    [Tooltip("Nombre del estado de animación de un rezo individual")]
    public string animRezo = "Command1";
    [Tooltip("Nombre del estado de animación al completar todos los rezos (buff activado)")]
    public string animRezoTerminado = "Command1";

    //buff
    [Header("Buff — valores al activarse")]
    [Tooltip("Duración del buff en segundos")]
    public float duracionBuff = 10f;
    [Tooltip("Multiplicador de velocidad de movimiento y carrera")]
    public float multiplicadorVelocidad = 1.5f;
    [Tooltip("Multiplicador de fuerza de salto")]
    public float multiplicadorSalto = 1.3f;
    [Tooltip("Multiplicador de velocidad del Animator (hace todas las animaciones más rápidas)")]
    public float multiplicadorVelocidadAnimacion = 1.5f;

    // ultimate
    [Header("Ultimate — Dash instakill")]
    [Tooltip("Nombre del estado de animacion de Start (preparacion).")]
    public string animUltimateStart = "UltimateStart";
    [Tooltip("Segundos que Montero permanece en la animacion de Start antes de lanzarse.")]
    public float duracionUltimateStart = 1.2f;

    [Tooltip("Nombre del estado de animacion del Dash (contiene los animation events de hitbox).")]
    public string animUltimateDash = "UltimateDash";
    [Tooltip("Velocidad del dash hacia el oponente durante la ultimate.")]
    public float velocidadUltimateDash = 28f;
    [Tooltip("Tiempo maximo del dash antes de considerarlo fallado (segundos).")]
    public float duracionMaxUltimateDash = 0.9f;
    [Tooltip("Dano absurdo que se aplica al enemigo si conecta (mata instantaneamente con cualquier valor > vida maxima del oponente).")]
    public float danoUltimate = 99999f;
    [Tooltip("Hitstun del golpe de ultimate (segundos).")]
    public float hitstunUltimate = 0.5f;

    [Tooltip("Nombre del estado de animacion cuando el dash falla (no conecta).")]
    public string animUltimateFail = "UltimateFail";
    [Tooltip("Segundos que dura la animacion de fallo antes de que Montero muera.")]
    public float duracionUltimateFail = 1.2f;

    //golpe Adelante - Command4
    [Header("Golpe Adelante — Command4 (QCB + Patada Fuerte)")]
    public float danoGolpeAdelante = 48f;
    public float hitstunGolpeAdelante = 0.4f;
    public TipoGolpe tipoGolpeAdelante = TipoGolpe.Normal;
    [Tooltip("Direccion del golpe: determina que bloqueos lo detienen y la animacion de impacto del receptor")]
    public DireccionGolpe direccionGolpeAdelante = DireccionGolpe.Frente;
    [Tooltip("Impulso vertical aplicado al receptor (positivo = arriba)")]
    public float impulsoYGolpeAdelante = 0f;
    [Tooltip("Impulso horizontal aplicado al receptor (positivo = alejarse del atacante)")]
    public float impulsoZGolpeAdelante = 0f;
}