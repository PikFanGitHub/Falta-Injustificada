using UnityEngine;
using System.Collections;

public class MinaCamus : MonoBehaviour
{
    // campos asignados por Camus.cs
    [HideInInspector] public Camus dueno;
    [HideInInspector] public float danio = 25f;
    [HideInInspector] public float hitstunDuracion = 0.5f;
    [HideInInspector] public float impulsoY = 8f;
    [HideInInspector] public float impulsoZ = 5f;
    [HideInInspector] public float tiempoExplosion = 1f; // segundos desde activacion hasta explotar

    // configuracion editable en el Inspector del prefab
    [Tooltip("nombre del estado de animacion de activacion en el Animator del prefab")]
    [SerializeField] private string animActivacion = "MinaActivar";

    [Tooltip("duracion de la animacion de activacion en segundos (fallback si no hay Animator)")]
    [SerializeField] private float duracionAnimActivacion = 0.8f;

    [Tooltip("nombre del GameObject hijo con el ParticleSystem de explosion, desactivado por defecto")]
    [SerializeField] private string nombreHijoExplosion = "ExplosionVFX";

    // internos
    private bool _activada = false;
    private Animator _animator;
    private Collider _collider;
    private GameObject _explosionVFX;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _collider = GetComponent<Collider>();

        Transform hijo = transform.Find(nombreHijoExplosion);
        if (hijo != null)
            _explosionVFX = hijo.gameObject;
        else
            Debug.LogWarning($"[MinaCamus] no se encontro el hijo '{nombreHijoExplosion}' en el prefab.");
    }

    // deteccion de pisada
    private void OnTriggerEnter(Collider other)
    {
        if (_activada) return;
        if (!other.CompareTag("Jugador")) return;

        PersonajeBase personaje = other.GetComponentInParent<PersonajeBase>();
        if (personaje == null) return;
        if (dueno != null && (object)personaje == (object)dueno) return;

        _activada = true;
        if (_collider != null) _collider.enabled = false;

        StartCoroutine(SecuenciaExplosion(personaje));
    }

    // animacion de activacion -> espera tiempoExplosion -> VFX + dano
    private IEnumerator SecuenciaExplosion(PersonajeBase objetivo)
    {
        // animacion de activacion
        float tiempoAnim = duracionAnimActivacion;

        if (_animator != null)
        {
            _animator.Play(animActivacion);
            yield return null;

            AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName(animActivacion) && info.length > 0f)
                tiempoAnim = info.length;
        }

        yield return new WaitForSeconds(tiempoAnim);

        // espera adicional antes de explotar (configurable desde CamusData)
        yield return new WaitForSeconds(tiempoExplosion);

        // activar VFX de explosion
        if (_explosionVFX != null)
        {
            _explosionVFX.transform.SetParent(null);
            _explosionVFX.SetActive(true);

            ParticleSystem ps = _explosionVFX.GetComponent<ParticleSystem>();
            float tiempoVFX = (ps != null)
                ? ps.main.duration + ps.main.startLifetime.constantMax
                : 2f;
            Destroy(_explosionVFX, tiempoVFX);
        }

        // aplicar dano y lanzamiento
        if (objetivo != null && objetivo.EstadoActualPublico != EstadoJugador.Agarrado)
        {
            objetivo.RecibirGolpe(
                hitstunDuracion,
                DireccionGolpe.Arriba,
                TipoGolpe.LanzarArriba,
                impulsoY,
                impulsoZ,
                danio
            );
        }

        gameObject.SetActive(false);
        Destroy(gameObject, 0.05f);
    }

    // llamado por Camus.cs al reiniciar ronda o colocar nueva mina
    public void Desaparecer()
    {
        StopAllCoroutines();
        if (_explosionVFX != null && _explosionVFX.transform.parent == transform)
            _explosionVFX.SetActive(false);
        gameObject.SetActive(false);
        Destroy(gameObject, 0.05f);
    }
}