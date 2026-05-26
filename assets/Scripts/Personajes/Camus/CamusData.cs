using UnityEngine;

[CreateAssetMenu(fileName = "CamusData", menuName = "Scriptable Objects/CamusData")]
public class CamusData : PersonajeDataBase
{
    // command1 - portatil
    [Header("Command1 - portatil")]
    [Tooltip("Prefab del portatil que se instancia en el suelo")]
    public GameObject prefabPortatil;
    [Tooltip("Segundos que dura la animacion de sacar el portatil")]
    public float duracionAnimPortatil = 0.6f;
    [Tooltip("Offset en Y respecto al suelo donde se coloca el portatil")]
    public float alturaPortatil = 0.05f;

    // command2 y QCF - comparten la misma animacion (animCommand2_1)
    [Header("Command2 y QCF - animacion compartida")]
    [Tooltip("Segundos que dura la animacion del comando 2 y de todos los QCF")]
    public float duracionComando2 = 0.8f;

    // command3 - desplazamiento hacia adelante
    [Header("Command3 - desplazamiento")]
    [Tooltip("Nombre del estado de animacion de inicio del desplazamiento")]
    public string animDesplazamientoStart = "CrawlStart";
    [Tooltip("Nombre del estado de animacion loop durante el desplazamiento")]
    public string animDesplazamientoLoop = "Crawl";
    [Tooltip("Nombre del estado de animacion de fin del desplazamiento")]
    public string animDesplazamientoEnd = "CrawlEnd";
    [Tooltip("Duracion en segundos de la animacion de inicio")]
    public float duracionAnimStart = 0.3f;
    [Tooltip("Duracion en segundos de la animacion de fin")]
    public float duracionAnimEnd = 0.3f;
    [Tooltip("Velocidad de desplazamiento hacia adelante")]
    public float velocidadDesplazamiento = 8f;
    [Tooltip("Segundos que dura el desplazamiento (sin contar start y end)")]
    public float duracionDesplazamiento = 0.5f;

    // patada floja y fuerte suelo - desplazamiento
    [Header("Patada suelo - desplazamiento")]
    [Tooltip("Impulso hacia adelante al golpear con patada floja en suelo")]
    public float impulsoPatadaFlojaSuelo = 4f;
    [Tooltip("Impulso hacia adelante al golpear con patada fuerte en suelo")]
    public float impulsoPatadaFuerteSuelo = 6f;
    [Tooltip("Segundos que dura el impulso de la patada")]
    public float duracionImpulsoPatada = 0.15f;

    // QCF + Patada Floja -> mina
    [Header("QCF + Patada Floja - mina")]
    [Tooltip("Prefab de la mina. Debe tener el componente MinaCamus.")]
    public GameObject prefabMina;
    [Tooltip("Dano que hace la mina al explotar")]
    public float danioMina = 25f;
    [Tooltip("Hitstun en segundos que aplica la explosion de la mina")]
    public float hitstunMina = 0.5f;
    [Tooltip("Impulso vertical al lanzar con la mina")]
    public float impulsoYMina = 8f;
    [Tooltip("Impulso horizontal al lanzar con la mina")]
    public float impulsoZMina = 5f;
    [Tooltip("Segundos que tarda en explotar despues de activarse")]
    public float tiempoExplosionMina = 1f;

    // QCF + Patada Fuerte -> torreta 5 proyectiles
    [Header("QCF - torretas")]
    [Tooltip("Metros por encima de la cabeza del personaje donde aparece la torreta")]
    public float alturaTorreta = 1f;

    [Header("QCF + Patada Fuerte - torreta 5 proyectiles")]
    [Tooltip("Prefab de la torreta de 5 proyectiles. Debe tener el componente TorretaCamus.")]
    public GameObject prefabTorreta5;
    [Tooltip("Dano de cada proyectil de la torreta de 5")]
    public float danioproyectilTorreta5 = 10f;
    [Tooltip("Hitstun que aplica cada proyectil")]
    public float hitstunTorreta5 = 0.3f;
    [Tooltip("Segundos entre proyectil y proyectil")]
    public float intervaloTorreta5 = 0.3f;

    // QCF + Puno Flojo -> torreta infinita (hasta recibir golpe)
    [Header("QCF + Puno Flojo - torreta infinita")]
    [Tooltip("Prefab de la torreta infinita. Debe tener el componente TorretaCamus.")]
    public GameObject prefabTorretaInfinita;
    [Tooltip("Dano de cada proyectil de la torreta infinita")]
    public float danioProyectilTorretaInfinita = 8f;
    [Tooltip("Hitstun que aplica cada proyectil de la torreta infinita")]
    public float hitstunTorretaInfinita = 0.25f;
    [Tooltip("Segundos entre proyectil y proyectil de la torreta infinita")]
    public float intervaloTorretaInfinita = 0.4f;

    // ultimate - golpe con WinRAR
    [Header("Ultimate - golpe con WinRAR")]
    [Tooltip("Nombre del estado Animator de la animacion de la ultimate")]
    public string animUltimate = "Ultimate";
    [Tooltip("Segundos de startup antes de que se abra la hitbox (desde inicio de animacion)")]
    public float duracionStartupUltimate = 0.3f;
    [Tooltip("Segundos que la hitbox permanece activa buscando al enemigo")]
    public float duracionActivaUltimate = 0.2f;
    [Tooltip("Segundos de recovery tras cerrar la hitbox antes de volver a Idle")]
    public float duracionRecoveryUltimate = 0.5f;
    [Tooltip("Dano del golpe de la ultimate")]
    public float danoUltimate = 30f;
    [Tooltip("Hitstun brevísimo del golpe de la ultimate (el estado WinRar se aplica por encima)")]
    public float hitstunUltimate = 0.15f;
    [Tooltip("Impulso vertical del golpe de la ultimate")]
    public float impulsoYUltimate = 1f;
    [Tooltip("Impulso horizontal del golpe de la ultimate")]
    public float impulsoZUltimate = 0.5f;
    [Tooltip("Segundos que el enemigo permanece en estado WinRar tras recibir el golpe")]
    public float duracionEstadoWinRar = 8f;
    [Tooltip("Prefab de partículas que se instancia en la posición del rival al conectar la ultimate")]
    public GameObject prefabParticulasUltimate;
}