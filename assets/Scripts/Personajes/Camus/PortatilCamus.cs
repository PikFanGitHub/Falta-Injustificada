using UnityEngine;

public class PortatilCamus : MonoBehaviour
{
    // referencia al dueno para ignorar su propio collider si hace falta
    [HideInInspector] public Camus dueno;

    // el collider del propio prefab define el area de efecto
    private Collider areaEfecto;

    private void Awake()
    {
        if (areaEfecto == null)
            areaEfecto = GetComponent<Collider>();
    }

    // devuelve true si el transform dado esta dentro del area de efecto del portatil.
    public bool EstaEnArea(Transform objetivo)
    {
        if (objetivo == null || areaEfecto == null) return false;
        return areaEfecto.bounds.Contains(objetivo.position);
    }

    // oculta y desactiva el portatil (el antiguo cuando se saca uno nuevo).
    public void Desaparecer()
    {
        gameObject.SetActive(false);
        Destroy(gameObject, 0.1f);
    }
}