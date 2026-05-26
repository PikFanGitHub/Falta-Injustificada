using UnityEngine;

// scriptable object de pani, hereda todos los campos de PersonajeDataBase
[CreateAssetMenu(fileName = "PaniData", menuName = "Scriptable Objects/PaniData")]
public class PaniData : PersonajeDataBase
{
    [Header("Command1 - Boost de velocidad")]
    [Tooltip("Multiplicador que se aplica a velocidadMovimiento durante el hover.")]
    public float multVelocidadBoost = 2.5f;

    [Tooltip("Duracion de la animacion de start antes de entrar al loop (segundos).")]
    public float duracionCommand1Start = 0.4f;

    [Tooltip("Tiempo que dura la fase de hover loop (segundos).")]
    public float duracionCommand1Hover = 2.0f;

    [Tooltip("Duracion de la animacion de land antes de volver a idle (segundos).")]
    public float duracionCommand1Land = 0.4f;

    [Tooltip("Cuanto sube el modelo durante el hover (unidades locales).")]
    public float elevacionHover = 0.5f;

    [Tooltip("Velocidad a la que sube y baja el modelo (unidades por segundo).")]
    public float velocidadElevacion = 4f;

    [Header("Command2 - Dash agarre")]

    [Tooltip("Impulso vertical al soltar al enemigo tras el agarre del command2.")]
    public float impulsoYLanzadoCommand2 = 10f;

    [Tooltip("Impulso horizontal al soltar al enemigo tras el agarre del command2.")]
    public float impulsoZLanzadoCommand2 = 5f;

    [Tooltip("Duracion de la animacion de start del dash.")]
    public float duracionCommand2Start = 0.3f;

    [Tooltip("Velocidad del dash hacia el oponente.")]
    public float velocidadCommand2Dash = 14f;

    [Tooltip("Tiempo maximo del runcycle antes de que expire sin agarrar.")]
    public float duracionCommand2Runcycle = 1.2f;

    [Tooltip("Duracion de la animacion de success (agarre conectado).")]
    public float duracionCommand2Success = 0.8f;

    [Tooltip("Duracion de la animacion de fail (no conecto).")]
    public float duracionCommand2Fail = 0.5f;

    [Header("Command3 - Agarre directo")]
    [Tooltip("Duracion de la ventana de la hitbox de agarre en el start.")]
    public float duracionCommand3Hitbox = 0.4f;

    [Tooltip("Duracion de la animacion de success (agarre conectado).")]
    public float duracionCommand3Success = 0.8f;

    [Tooltip("Duracion de la animacion de fail (no conecto).")]
    public float duracionCommand3Fail = 0.5f;

    [Header("Command4 - Proyectil (QCF)")]
    [Tooltip("Velocidad horizontal del proyectil.")]
    public float velocidadProyectil = 12f;

    [Tooltip("Tiempo de vida del proyectil antes de volver a su posicion original.")]
    public float tiempoVidaProyectil = 3f;

    [Tooltip("Duracion de la fase diagonal al inicio del lanzamiento (segundos). 0 = solo horizontal.")]
    public float tiempoDiagonal = 0.3f;

    [Tooltip("Velocidad de caida durante la fase diagonal (unidades por segundo). Valores positivos bajan el proyectil.")]
    public float inclinacionVertical = 8f;

    [Tooltip("Duracion de la animacion de disparo de pani antes de volver a idle.")]
    public float duracionCommand4Anim = 0.5f;

    [Header("Patada floja agachado - desplazamiento")]
    [Tooltip("Impulso hacia adelante al golpear con patada floja agachado.")]
    public float impulsoPatadaFlojaAgachado = 4f;

    [Tooltip("Segundos que dura el impulso de la patada floja agachado.")]
    public float duracionImpulsoPatadaAgachado = 0.15f;

    [Header("Ultimate - Trampa (QCF+QCF+Puno Flojo)")]
    [Tooltip("Nombre del estado de animacion de la ultimate de Pani.")]
    public string animUltimate = "Ultimate";

    [Tooltip("Duracion de la animacion de la ultimate antes de que empiece el contador (segundos).")]
    public float duracionUltimateAnim = 0.8f;

    [Tooltip("Duracion total del contador en segundos (el sprite va de ese valor hasta 1).")]
    public float duracionTrampa = 5f;

    [Tooltip("Duracion en segundos de la fase de captura tras el contador: " +
             "si el enemigo se mueve en este periodo recibe el golpe.")]
    public float duracionCaptura = 2f;

    [Tooltip("Dano infligido al enemigo si se mueve o actua durante la trampa.")]
    public float danoTrampa = 30f;

    [Tooltip("Duracion del hitstun al recibir el golpe de trampa (segundos).")]
    public float hitstunTrampa = 0.5f;

    [Tooltip("Tipo de golpe que recibe el enemigo al activarse la trampa.")]
    public TipoGolpe tipoGolpeTrampa = TipoGolpe.LanzarArriba;

    [Tooltip("Impulso vertical al recibir el golpe de trampa.")]
    public float impulsoYTrampa = 8f;

    [Tooltip("Impulso horizontal al recibir el golpe de trampa.")]
    public float impulsoZTrampa = 4f;
}