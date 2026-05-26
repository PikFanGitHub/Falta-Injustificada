using UnityEngine;
using UnityEngine.UI;

public class BarraVida : MonoBehaviour
{
    [Header("Referencias")]
    public Slider slider;
    public SaludJugador saludJugador;

    void Awake()
    {
        // intentar obtener el Slider del propio GO si no esta asignado en el Inspector
        if (slider == null)
        {
            slider = GetComponent<Slider>();
            if (slider == null)
                Debug.LogError("[BarraVida] No hay Slider asignado en " + gameObject.name);
            return;
        }

        // rango normalizado para poder usar PorcentajeVida directamente sin conversion
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
    }

    void Update()
    {
        if (slider == null || saludJugador == null) return;
        slider.value = saludJugador.PorcentajeVida;
    }
}