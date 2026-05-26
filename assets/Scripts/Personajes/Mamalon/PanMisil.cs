using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PanMisil : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float velocidad = 9f;
    [SerializeField] private float velocidadGiro = 140f;
    [SerializeField] private float tiempoVida = 7f;
    [Tooltip("Offset en Y sobre la posicion del objetivo para apuntar al centro del cuerpo.")]
    [SerializeField] private float offsetApuntadoY = 0.8f;

    [Header("Rotacion del modelo")]
    [Tooltip("Rota el mesh respecto a la direccion de vuelo. X=90 tumba el pan.")]
    [SerializeField] private Vector3 offsetRotacionModelo = new Vector3(90f, 0f, 0f);

    [Header("Hitstun al impactar")]
    [SerializeField] private float duracionHitstun = 0.4f;
    [SerializeField] private float impulsoY = 3f;
    [SerializeField] private float impulsoZ = 5f;

    private Transform _objetivo;
    private PersonajeBase _dueno;
    private float _dano;
    private bool _golpeo;
    private Rigidbody _rb;

    // direccion de vuelo real, separada de la rotacion visual
    private Vector3 _dirVuelo;

    public void Inicializar(Transform objetivo, PersonajeBase dueno, float dano)
    {
        _objetivo = objetivo;
        _dueno = dueno;
        _dano = dano;
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.freezeRotation = true;

        // forzar collider como trigger para no tener colision fisica
        foreach (var col in GetComponentsInChildren<Collider>())
            col.isTrigger = true;

        if (_objetivo != null)
        {
            _dirVuelo = (_objetivo.position + Vector3.up * offsetApuntadoY
                        - transform.position).normalized;
            if (_dirVuelo != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(_dirVuelo)
                                   * Quaternion.Euler(offsetRotacionModelo);
        }
        else
        {
            _dirVuelo = transform.forward;
        }

        Destroy(gameObject, tiempoVida);
    }

    private void FixedUpdate()
    {
        if (_golpeo) return;

        if (_objetivo != null)
        {
            Vector3 posObjetivo = _objetivo.position + Vector3.up * offsetApuntadoY;
            Vector3 nuevaDir = (posObjetivo - transform.position).normalized;

            if (nuevaDir != Vector3.zero)
            {
                // girar la rotacion visual suavemente
                Quaternion rotObjetivo = Quaternion.LookRotation(nuevaDir)
                                       * Quaternion.Euler(offsetRotacionModelo);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, rotObjetivo,
                    velocidadGiro * Time.fixedDeltaTime);

                // actualizar la direccion de vuelo real (independiente del offset visual)
                _dirVuelo = Vector3.RotateTowards(
                    _dirVuelo, nuevaDir,
                    velocidadGiro * Mathf.Deg2Rad * Time.fixedDeltaTime,
                    0f);
            }
        }

        // mover siempre en la direccion de vuelo real, no en transform.forward
        _rb.linearVelocity = _dirVuelo * velocidad;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_golpeo) return;
        // solo reacciona a hurtboxes (non-trigger) del enemigo
        if (other.isTrigger) return;

        PersonajeBase pb = other.GetComponentInParent<PersonajeBase>();
        if (pb == null || pb == _dueno) return;
        if (pb.EstadoActualPublico == EstadoJugador.Agarrado) return;

        _golpeo = true;
        _rb.linearVelocity = Vector3.zero;
        pb.RecibirGolpe(duracionHitstun, DireccionGolpe.Frente, TipoGolpe.Normal,
                        impulsoY, impulsoZ, _dano, transform.position);

        if (_dueno != null)
            _dueno.NotificarGolpeConectado(_dano);

        Destroy(gameObject);
    }
}