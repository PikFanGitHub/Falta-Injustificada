using UnityEngine;

public class ProyectilCamus : MonoBehaviour
{
    [HideInInspector] public Camus dueno;
    [HideInInspector] public Transform objetivo;
    [HideInInspector] public float danio = 10f;
    [HideInInspector] public float hitstunDuracion = 0.3f;
    [HideInInspector] public float velocidad = 12f;

    private bool _yaImpacto = false;
    private Vector3 _direccion = Vector3.zero;

    private void Start()
    {
        if (dueno != null)
        {
            Collider propioColl = GetComponent<Collider>();
            Collider duenoColl = dueno.GetComponent<Collider>();
            if (propioColl != null && duenoColl != null)
                Physics.IgnoreCollision(propioColl, duenoColl);
        }

        // calcular direccion hacia el enemigo en el momento del disparo
        if (objetivo != null)
        {
            _direccion = (objetivo.position - transform.position).normalized;
            if (_direccion == Vector3.zero) _direccion = transform.forward;
        }

        Destroy(gameObject, 6f);
    }

    private void Update()
    {
        if (_yaImpacto || _direccion == Vector3.zero) return;
        transform.position += _direccion * velocidad * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_yaImpacto) return;
        if (!other.CompareTag("Jugador")) return;

        PersonajeBase p = other.GetComponentInParent<PersonajeBase>();
        if (p == null) return;
        if (dueno != null && (object)p == (object)dueno) return;
        if (p.EstadoActualPublico == EstadoJugador.Agarrado) return;

        _yaImpacto = true;
        p.RecibirGolpe(hitstunDuracion, DireccionGolpe.Frente, TipoGolpe.Normal, 0f, 0f, danio);

        Destroy(gameObject);
    }
}