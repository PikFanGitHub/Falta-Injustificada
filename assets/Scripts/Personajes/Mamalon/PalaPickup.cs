using UnityEngine;

public class PalaPickup : MonoBehaviour
{
    [HideInInspector] public Cube dueno;

    [Tooltip("Segundos que deben pasar tras activarse antes de poder ser recogida")]
    [SerializeField] private float cooldownPickup = 0.4f;

    // tiempo en que el GO fue activado; se inicializa con un valor negativo muy bajo
    // para que el cooldown ya haya pasado si el objeto arranca activo en escena
    private float _tiempoActivacion = -999f;

    private void OnEnable()
    {
        // registrar el momento exacto en que se activo el GO para calcular el cooldown
        _tiempoActivacion = Time.time;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (dueno == null) return;

        // evitar que la pala se recoja en el mismo frame en que se lanza
        if (Time.time - _tiempoActivacion < cooldownPickup) return;

        // solo el dueno puede recoger la pala
        Cube cube = other.GetComponentInParent<Cube>();
        if (cube != null && cube == dueno)
            cube.RecogerPala();
    }
}