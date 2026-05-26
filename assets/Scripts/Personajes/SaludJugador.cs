using UnityEngine;
using UnityEngine.Events;

public class SaludJugador : MonoBehaviour
{
    [Header("Vida")]
    public float vidaMaxima = 100f;
    public float vidaActual = 100f;

    [Header("Evento al morir (opcional)")]
    public UnityEvent onMuerte;

    // valor normalizado [0, 1] para alimentar directamente a la barra de vida
    public float PorcentajeVida => vidaActual / vidaMaxima;

    public void RecibirDano(float cantidad)
    {
        // ignorar dano cuando el personaje ya esta muerto
        if (vidaActual <= 0f) return;

        vidaActual -= cantidad;
        vidaActual = Mathf.Max(vidaActual, 0f);

        Debug.Log(name + " recibio " + cantidad + " de dano. Vida restante: " + vidaActual);

        if (vidaActual <= 0f)
        {
            Debug.Log(name + " ha sido derrotado!");
            onMuerte?.Invoke();
        }
    }

    public void CurarVida(float cantidad)
    {
        vidaActual = Mathf.Min(vidaActual + cantidad, vidaMaxima);
    }
}