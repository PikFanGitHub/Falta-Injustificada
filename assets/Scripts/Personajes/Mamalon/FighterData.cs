using UnityEngine;

[CreateAssetMenu(fileName = "FighterData", menuName = "Scriptable Objects/FighterData")]
public class FighterData : PersonajeDataBase
{
    // command2 multifase (especifico de Cube)
    [Header("Command2 — Fases (QCB + Puño Fuerte)")]
    public float duracionComando2_1 = 0.4f;
    public float duracionComando2_2 = 0.4f;
    public float duracionComando2_3 = 0.3f;

    // command4 - Pan (hitbox extra de Cube)
    [Header("Hitboxes - Escala y Offset Command4")]
    public Vector3 scaleCommand4 = new Vector3(0.4f, 0.35f, 1.0f);
    public Vector3 offsetCommand4 = new Vector3(0f, 0.2f, 0.8f);

    // pan
    [Header("Command4 — Pan (QCF + boton 1 o 2)")]
    public float danoPan = 14f;
    [Tooltip("Distancia desde la que emerge el pan hasta su posicion final")]
    public float distanciaEreccionPan = 3f;

    [Header("Pan Espalda — posicion objetivo")]
    [Tooltip("Offset en Y sobre la posicion del oponente donde apunta el pan (0 = pies, 1 = centro cuerpo)")]
    public float alturaObjetivoPanEspalda = 1f;

    [Header("Pan Espalda — impulso al golpear (Y = altura, Z = distancia)")]
    public float impulsoYPanEspalda = 5f;
    public float impulsoZPanEspalda = 8f;

    [Header("Pan Suelo — impulso al golpear (Y = altura hacia arriba)")]
    public float impulsoYPanSuelo = 10f;
    public float impulsoZPanSuelo = 2f;

    // ultimate - Horno
    [Header("Ultimate — Horno de Mamalon")]
    [Tooltip("Nombre del estado de animacion del ultimate en el Animator de Cube.")]
    public string animUltimate = "Ultimate";
    [Tooltip("Duracion de la animacion del ultimate (fallback si el clip no se encuentra).")]
    public float duracionAnimUltimate = 2f;
    [Tooltip("Vida total del horno.")]
    public float vidaHornoUltimate = 60f;
    [Tooltip("Segundos entre cada disparo del horno.")]
    public float intervaloPanUltimate = 2f;
    [Tooltip("Dano de cada pan misil al impactar.")]
    public float danoPanUltimate = 8f;
}