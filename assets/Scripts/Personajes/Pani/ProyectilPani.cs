using UnityEngine;

public class ProyectilPani : MonoBehaviour
{
    [Tooltip("Transform hijo de pani que marca la posicion de reposo del proyectil.")]
    [SerializeField] private Transform puntoReposo;

    [Tooltip("Transform raiz del modelo de pani, usado para leer la escala y voltear el proyectil.")]
    [SerializeField] private Transform modeloPani;

    [Tooltip("Tag del personaje enemigo que puede recibir el impacto.")]
    [SerializeField] private string tagEnemigo = "Jugador";

    [Tooltip("Dano que aplica al impactar.")]
    [SerializeField] private float dano = 10f;

    [Tooltip("Duracion del hitstun al impactar (segundos).")]
    [SerializeField] private float hitstunDuracion = 0.35f;

    [Tooltip("Nombre del estado idle en el animator (cuando el proyectil esta en reposo).")]
    [SerializeField] private string animIdle = "Idle";

    [Tooltip("Nombre del estado de ataque en el animator (cuando el proyectil esta volando).")]
    [SerializeField] private string animAtaque = "Ataque";

    private enum Estado { Reposo, Avanzando }
    private Estado _estado = Estado.Reposo;

    private Collider _collider;
    private Animator _animator;
    private PersonajeBase _dueno;
    private Collider _duenoColl;

    // parent original guardado en Awake para poder reparentar al volver a reposo
    private Transform _parentOriginal;

    // parametros de vuelo guardados al llamar a Lanzar()
    private float _dirZ;
    private float _velocidad;
    private float _tiempoRestante;
    private float _tiempoDiagonalRestante;
    private float _inclinacionVertical;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        _animator = GetComponent<Animator>();

        // guardar el parent antes de cualquier cambio
        _parentOriginal = transform.parent;

        // el collider se mantiene desactivado: se usa solo como referencia de forma
        // para el overlap manual. si estuviera activo Unity podria generar eventos
        // de fisica propios que interfieran con la logica del proyectil.
        if (_collider != null)
            _collider.enabled = false;

        Hitbox hitboxComp = GetComponent<Hitbox>();
        if (hitboxComp != null) hitboxComp.enabled = false;
    }

    private void Start()
    {
        if (puntoReposo != null)
            transform.position = puntoReposo.position;

        // arrancar en idle desde el primer frame
        PlayAnimSiExiste(animIdle);
    }

    private void Update()
    {
        switch (_estado)
        {
            case Estado.Reposo: ActualizarReposo(); break;
            case Estado.Avanzando: ActualizarAvanzando(); break;
        }
    }

    private void ActualizarReposo()
    {
        // seguir al punto de reposo exactamente cada frame
        if (puntoReposo != null)
            transform.position = puntoReposo.position;

        // sincronizar volteo horizontal con el modelo de pani
        if (modeloPani != null)
        {
            float signo = Mathf.Sign(modeloPani.localScale.x);
            if (signo == 0f) signo = 1f;
            Vector3 s = transform.localScale;
            s.x = Mathf.Abs(s.x) * signo;
            transform.localScale = s;
        }
    }

    private void ActualizarAvanzando()
    {
        _tiempoRestante -= Time.deltaTime;
        if (_tiempoRestante <= 0f)
        {
            EntrarReposo();
            return;
        }

        // componente vertical: solo activa durante la fase diagonal inicial
        float velY = 0f;
        if (_tiempoDiagonalRestante > 0f)
        {
            _tiempoDiagonalRestante -= Time.deltaTime;
            velY = -_inclinacionVertical; // valores positivos en el data = bajar
        }

        transform.position += new Vector3(
            0f,
            velY * Time.deltaTime,
            _dirZ * _velocidad * Time.deltaTime
        );

        // deteccion manual de impacto despues de mover
        VerificarImpactoManual();
    }

    // comprueba cada frame si hay un enemigo en el area del proyectil.
    // mas confiable que OnTriggerEnter cuando el objeto se mueve via transform sin Rigidbody.
    private void VerificarImpactoManual()
    {
        Collider[] hits = ObtenerOverlaps();
        if (hits == null) return;

        foreach (Collider hit in hits)
        {
            // ignorar el collider principal del dueno
            if (_duenoColl != null && hit == _duenoColl) continue;

            bool tagValido = hit.CompareTag(tagEnemigo)
                          || hit.transform.root.CompareTag(tagEnemigo);
            if (!tagValido) continue;

            PersonajeBase enemigo = hit.GetComponentInParent<PersonajeBase>();
            if (enemigo == null) continue;

            // segunda verificacion por si el dueno tiene varios colliders
            if (_dueno != null && (object)enemigo == (object)_dueno) continue;

            // no danar a personajes agarrados
            if (enemigo.EstadoActualPublico == EstadoJugador.Agarrado) continue;

            enemigo.RecibirGolpe(hitstunDuracion, DireccionGolpe.Frente, TipoGolpe.Normal, 0f, 0f, dano);
            _dueno?.NotificarGolpeConectado();
            EntrarReposo();
            return;
        }
    }

    // calcula los overlaps usando la forma del collider adjunto como referencia
    private Collider[] ObtenerOverlaps()
    {
        if (_collider == null) return null;

        if (_collider is BoxCollider box)
        {
            Vector3 center = transform.TransformPoint(box.center);
            Vector3 half = new Vector3(
                box.size.x * 0.5f * Mathf.Abs(transform.lossyScale.x),
                box.size.y * 0.5f * Mathf.Abs(transform.lossyScale.y),
                box.size.z * 0.5f * Mathf.Abs(transform.lossyScale.z)
            );
            return Physics.OverlapBox(center, half, transform.rotation);
        }

        if (_collider is SphereCollider sphere)
        {
            Vector3 center = transform.TransformPoint(sphere.center);
            float radius = sphere.radius * Mathf.Max(
                Mathf.Abs(transform.lossyScale.x),
                Mathf.Abs(transform.lossyScale.y),
                Mathf.Abs(transform.lossyScale.z)
            );
            return Physics.OverlapSphere(center, radius);
        }

        return null;
    }

    private void EntrarReposo()
    {
        _estado = Estado.Reposo;

        // reparentar al parent original para que puntoReposo vuelva a ser
        // una referencia valida y el proyectil siga al personaje en reposo
        transform.SetParent(_parentOriginal);

        // volver directamente al punto de reposo
        if (puntoReposo != null)
            transform.position = puntoReposo.position;

        // volver a la animacion idle
        PlayAnimSiExiste(animIdle);
    }

    // registra al dueno para ignorarlo en los overlaps manuales
    public void SetDueno(PersonajeBase dueno)
    {
        _dueno = dueno;
        _duenoColl = dueno != null ? dueno.GetComponent<Collider>() : null;
    }

    // llamado por pani cuando ejecuta el command4.
    // si el proyectil ya esta en vuelo, ignora la llamada.
    public void Lanzar(float direccionZ, float velocidad, float tiempoVida, float tiempoDiagonal, float inclinacionVertical)
    {
        if (_estado != Estado.Reposo)
        {
            Debug.Log("[ProyectilPani] Lanzar() ignorado: el proyectil no esta en reposo.");
            return;
        }

        _dirZ = direccionZ;
        _velocidad = velocidad;
        _tiempoRestante = tiempoVida;
        _tiempoDiagonalRestante = tiempoDiagonal;
        _inclinacionVertical = inclinacionVertical;

        // desparentar antes de cambiar de estado para que el movimiento posterior
        // sea en espacio de mundo y no arrastre el movimiento del personaje padre
        transform.SetParent(null);

        _estado = Estado.Avanzando;
        PlayAnimSiExiste(animAtaque);
    }

    // reproduce una animacion solo si el nombre no esta vacio
    private void PlayAnimSiExiste(string nombreAnim)
    {
        if (_animator != null && !string.IsNullOrEmpty(nombreAnim))
            _animator.Play(nombreAnim, -1, 0f);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (puntoReposo != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, puntoReposo.position);
            Gizmos.DrawWireSphere(puntoReposo.position, 0.08f);
        }
    }
#endif
}