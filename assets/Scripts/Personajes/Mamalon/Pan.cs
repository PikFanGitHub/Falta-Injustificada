using UnityEngine;
using System.Collections;

public enum TipoPan { Espalda, Suelo }

public class Pan : MonoBehaviour
{
    [HideInInspector] public float dano = 10f;
    [HideInInspector] public string tagEnemigo = "Jugador";
    [HideInInspector] public TipoPan tipo;

    [Header("Tipo predeterminado de este Pan (para identificarlo en el array)")]
    [Tooltip("Asigna aqui si este GameObject es el PanEspalda o el PanSuelo. " +
             "Cube.cs lo usa para encontrar el pan correcto independientemente del orden en el Inspector.")]
    [SerializeField] public TipoPan tipoPredeterminado = TipoPan.Espalda;

    [Header("Ereccion y retraccion")]
    [Tooltip("Segundos que tarda en erguirse desde su posicion de origen hasta la final.")]
    [SerializeField] private float tiempoEreccion = 0.3f;

    [Tooltip("Segundos que permanece totalmente erguido antes de retraerse (si no ha golpeado).")]
    [SerializeField] private float tiempoActivo = 1.2f;

    [Tooltip("Segundos que tarda en retraerse.")]
    [SerializeField] private float tiempoRetraccion = 0.25f;

    private Cube personajePadre;
    private Transform padreOriginal;
    private bool yaGolpeo = false;
    private bool retraido = false;
    private float _impulsoY;
    private float _impulsoZ;
    private Vector3 posOrigen;
    private Vector3 posFinal;

    // inicializa el pan y arranca la secuencia de ereccion / permanencia / retraccion
    public void Erigir(Cube padre, float nuevoDano, string tag, TipoPan t,
                       Vector3 posicionFinal, Vector3 desplazamientoOrigen,
                       float impulsoY = 0f, float impulsoZ = 0f)
    {
        personajePadre = padre;
        dano = nuevoDano;
        tagEnemigo = tag;
        tipo = t;
        yaGolpeo = false;
        retraido = false;
        _impulsoY = impulsoY;
        _impulsoZ = impulsoZ;
        posFinal = posicionFinal;
        posOrigen = posicionFinal + desplazamientoOrigen;

        // se desparentea para moverse en espacio mundial sin heredar transforms del padre
        padreOriginal = transform.parent;
        transform.SetParent(null, true);
        transform.position = posOrigen;
        gameObject.SetActive(true);

        StopAllCoroutines();
        StartCoroutine(SecuenciaTotem());
    }

    IEnumerator SecuenciaTotem()
    {
        // subida
        yield return StartCoroutine(MoverHacia(posOrigen, posFinal, tiempoEreccion));

        // permanece activo hasta agotar el tiempo o golpear al enemigo
        float t = 0f;
        while (t < tiempoActivo && !yaGolpeo)
        {
            t += Time.deltaTime;
            yield return null;
        }

        // retraccion
        yield return StartCoroutine(Retraerse());
    }

    IEnumerator MoverHacia(Vector3 desde, Vector3 hasta, float duracion)
    {
        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / duracion);
            transform.position = Vector3.LerpUnclamped(desde, hasta, p);
            yield return null;
        }
        transform.position = hasta;
    }

    IEnumerator Retraerse()
    {
        // guarda para que OnTriggerEnter no relance la retraccion mientras se ejecuta
        if (retraido) yield break;
        retraido = true;
        yield return StartCoroutine(MoverHacia(posFinal, posOrigen, tiempoRetraccion));
        gameObject.SetActive(false);
        if (padreOriginal != null)
            transform.SetParent(padreOriginal, true);
    }

    void OnTriggerEnter(Collider other)
    {
        if (yaGolpeo) return;
        if (retraido) return;
        if (!other.CompareTag(tagEnemigo)) return;

        // no golpear a un personaje que ya esta siendo agarrado
        PersonajeBase personajeEnemigo = other.GetComponentInParent<PersonajeBase>();
        if (personajeEnemigo != null && personajeEnemigo.EstadoActualPublico == EstadoJugador.Agarrado) return;

        yaGolpeo = true;

        SaludJugador salud = other.GetComponentInParent<SaludJugador>();
        if (salud != null) salud.RecibirDano(dano);

        if (personajeEnemigo != null)
        {
            if (tipo == TipoPan.Suelo)
            {
                // el pan de suelo lanza hacia arriba
                personajeEnemigo.RecibirGolpe(0.4f, DireccionGolpe.Arriba, TipoGolpe.LanzarArriba,
                    _impulsoY, _impulsoZ);
            }
            else
            {
                // el pan de espalda determina la direccion lateral segun la posicion relativa del Cube
                float dirZ = (personajePadre != null && personajePadre.transform.position.z < transform.position.z)
                    ? 1f : -1f;
                personajeEnemigo.RecibirGolpe(0.4f, DireccionGolpe.Frente, TipoGolpe.LanzarFrente,
                    _impulsoY, _impulsoZ * dirZ);
            }
        }
    }

    // forzar desactivacion desde fuera sin esperar la secuencia
    public void Desactivar()
    {
        StopAllCoroutines();
        gameObject.SetActive(false);
    }
}