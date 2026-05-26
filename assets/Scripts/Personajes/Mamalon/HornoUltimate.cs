using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HornoUltimate : MonoBehaviour
{
    [Header("Disparo")]
    [SerializeField] private GameObject prefabPanMisil;
    [SerializeField] private Transform puntoDisparo;
    // desplazamiento extra sobre el punto de disparo para ajustar el spawn
    [SerializeField] private Vector3 offsetSpawnPan = new Vector3(0f, 1f, 0f);

    [Header("Animaciones")]
    [SerializeField] private string animSpawn = "Spawn";
    [SerializeField] private string animIdle = "Idle";
    [SerializeField] private string animProduce = "Produce";

    [Header("Dano recibido por golpe")]
    [SerializeField] private float danoRecibidoPorGolpe = 5f;

    [Header("Particulas")]
    [SerializeField] private GameObject prefabParticulasGolpe;
    [SerializeField] private GameObject prefabParticulasDestruccion;

    [Header("Sonidos")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sonidoGolpe;
    [SerializeField] private AudioClip sonidoProducir;
    [SerializeField] private AudioClip sonidoDestruccion;

    private float _vidaActual;
    private Transform _objetivo;
    private PersonajeBase _dueno;
    private float _danoMisil;
    private float _intervalo;
    private Coroutine _corrutinaDisparo;

    // guardados en Awake para restaurar la posicion al desactivar el GO
    private Transform _padreOriginal;
    private Vector3 _localPosOriginal;
    private Quaternion _localRotOriginal;

    private Animator _animator;

    // lista de misiles activos para destruirlos si el horno muere antes que ellos
    private readonly List<GameObject> _panesMisil = new List<GameObject>();

    private void Awake()
    {
        _padreOriginal = transform.parent;
        _localPosOriginal = transform.localPosition;
        _localRotOriginal = transform.localRotation;

        _animator = GetComponentInChildren<Animator>(true);

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        gameObject.SetActive(false);
    }

    // configura el horno y lo pone en marcha; llamado desde el personaje dueno
    public void Inicializar(PersonajeBase dueno, Transform objetivo,
                            float vida, float intervalo, float danoMisil)
    {
        _dueno = dueno;
        _objetivo = objetivo;
        _vidaActual = vida;
        _intervalo = intervalo;
        _danoMisil = danoMisil;

        // capturar la posicion mundial antes de desparentearse
        Vector3 posWorld = transform.position;
        Quaternion rotWorld = transform.rotation;

        transform.SetParent(null, false);
        transform.position = posWorld;
        transform.rotation = rotWorld;

        if (_animator != null) _animator.enabled = true;

        foreach (var col in GetComponentsInChildren<Collider>())
            col.isTrigger = true;

        gameObject.SetActive(true);

        PlayAnim(animSpawn);

        if (_corrutinaDisparo != null) StopCoroutine(_corrutinaDisparo);
        _corrutinaDisparo = StartCoroutine(CicloDisparo());
    }

    public void RecibirDano(float cantidad, Vector3 posicionGolpe = default)
    {
        SpawnParticulasGolpe(posicionGolpe == default ? transform.position : posicionGolpe);
        PlaySFX(sonidoGolpe);

        _vidaActual -= cantidad;
        if (_vidaActual <= 0f) Destruir();
    }

    public void Destruir()
    {
        SpawnParticulasDestruccion(transform.position);
        PlaySFX(sonidoDestruccion);

        if (_corrutinaDisparo != null)
        {
            StopCoroutine(_corrutinaDisparo);
            _corrutinaDisparo = null;
        }

        // limpiar todos los misiles que el horno haya generado
        foreach (var pan in _panesMisil)
            if (pan != null) Destroy(pan);
        _panesMisil.Clear();

        // volver a la jerarquia y posicion original antes de desactivarse
        transform.SetParent(_padreOriginal, false);
        transform.localPosition = _localPosOriginal;
        transform.localRotation = _localRotOriginal;

        gameObject.SetActive(false);
    }

    private void SpawnParticulasGolpe(Vector3 posicion)
    {
        if (prefabParticulasGolpe == null) return;
        GameObject go = Instantiate(prefabParticulasGolpe, posicion, Quaternion.identity);
        go.transform.localScale *= 3f;
        Destroy(go, 1f);
    }

    private void SpawnParticulasDestruccion(Vector3 posicion)
    {
        if (prefabParticulasDestruccion == null) return;
        GameObject go = Instantiate(prefabParticulasDestruccion, posicion, Quaternion.identity);
        go.transform.localScale *= 3f;

        // calcular el tiempo de vida real del sistema de particulas para el Destroy
        float tiempoVida = 2f;
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        if (ps == null) ps = go.GetComponentInChildren<ParticleSystem>();
        if (ps != null)
            tiempoVida = ps.main.duration + ps.main.startLifetime.constantMax;

        Destroy(go, tiempoVida);
    }

    private void PlaySFX(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        audioSource.PlayOneShot(clip);
    }

    private IEnumerator CicloDisparo()
    {
        // esperar a que termine la animacion de aparicion antes de comenzar a disparar
        float durSpawn = ObtenerDuracionClip(animSpawn);
        yield return new WaitForSeconds(durSpawn);

        while (true)
        {
            PlayAnim(animIdle);
            yield return new WaitForSeconds(_intervalo);

            if (_objetivo != null)
            {
                PlayAnim(animProduce);
                PlaySFX(sonidoProducir);
                float durProduce = ObtenerDuracionClip(animProduce);
                DispararPan();
                yield return new WaitForSeconds(durProduce);
            }
        }
    }

    private void DispararPan()
    {
        if (prefabPanMisil == null) return;

        Vector3 origen = puntoDisparo != null
            ? puntoDisparo.position
            : transform.position + Vector3.up * 0.5f;

        origen += offsetSpawnPan;

        // se instancia desactivado para poder configurarlo antes de que Update se ejecute
        GameObject panGO = Instantiate(prefabPanMisil, origen, Quaternion.identity);
        panGO.SetActive(false);

        _panesMisil.Add(panGO);

        PanMisil misil = panGO.GetComponent<PanMisil>();
        if (misil != null)
            misil.Inicializar(_objetivo, _dueno, _danoMisil);

        panGO.SetActive(true);
    }

    private void Update()
    {
        // eliminar referencias nulas a misiles que se hayan destruido por su cuenta
        _panesMisil.RemoveAll(p => p == null);
    }

    private void PlayAnim(string nombre)
    {
        if (_animator == null || !_animator.enabled) return;
        _animator.Play(nombre, 0, 0f);
    }

    // obtiene la duracion real de un clip del controlador; devuelve fallback si no lo encuentra
    private float ObtenerDuracionClip(string nombre, float fallback = 0.5f)
    {
        if (_animator == null || _animator.runtimeAnimatorController == null) return fallback;
        foreach (var clip in _animator.runtimeAnimatorController.animationClips)
            if (clip.name == nombre && clip.length > 0f) return clip.length;
        return fallback;
    }

    private void OnTriggerEnter(Collider other)
    {
        // solo reaccionar a hitboxes de personajes que no sean el dueno
        if (!other.isTrigger) return;
        Hitbox hb = other.GetComponent<Hitbox>();
        if (hb == null) return;
        PersonajeBase origen = other.GetComponentInParent<PersonajeBase>();
        if (origen == null || origen == _dueno) return;
        RecibirDano(danoRecibidoPorGolpe, other.bounds.center);
    }
}