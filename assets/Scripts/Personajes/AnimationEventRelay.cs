using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimationEventRelay : MonoBehaviour
{
    // se usa PersonajeBase para que funcione con cualquier personaje (Cube, Montero, etc.)
    private PersonajeBase personaje;

    void Awake()
    {
        personaje = GetComponentInParent<PersonajeBase>();
        if (personaje == null)
            Debug.LogWarning("[AnimationEventRelay] No se encontro PersonajeBase en el padre.");
    }

    // sin parametro: actua sobre todas las hitboxes del array (indice -1)
    public void AnimEvent_AbrirHitbox() => personaje?.AnimEvent_AbrirHitbox(-1);
    public void AnimEvent_CerrarHitbox() => personaje?.AnimEvent_CerrarHitbox(-1);
    // con parametro int: actua solo sobre la hitbox en ese indice
    public void AnimEvent_AbrirHitbox(int indice) => personaje?.AnimEvent_AbrirHitbox(indice);
    public void AnimEvent_CerrarHitbox(int indice) => personaje?.AnimEvent_CerrarHitbox(indice);
    public void AnimEvent_FinAtaque() => personaje?.AnimEvent_FinAtaque();

    // indica el momento exacto en que se suelta el agarre de un comando (command2 de pani, etc.)
    public void AnimEvent_SoltarAgarreComando() => personaje?.AnimEvent_SoltarAgarreComando();

    // spawna el objeto de la ultimate en el fotograma configurado (ej. HornoUltimate de Cube)
    public void AnimEvent_SpawnHorno() => personaje?.AnimEvent_SpawnHorno();

    // receptores vacios para animation events del fbx que no necesitamos manejar
    public void OnPunchFinished() { }
    public void OnKickFinished() { }
    public void OnFootstep() { }
    public void OnAnimFinished() { }

    // events del fbx original de montero: abren y cierran la hitbox activa
    public void OnHitboxOpen() => personaje?.AnimEvent_AbrirHitbox(-1);
    public void OnHitboxClose() => personaje?.AnimEvent_CerrarHitbox(-1);
}