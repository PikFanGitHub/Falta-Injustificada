using UnityEngine;

// hace que este GameObject siga la posicion y rotacion de otro transform cada frame.
// usar en Hitbox_Pala para que siga al objeto armas sin ser hijo del hueso.
public class SeguirTransform : MonoBehaviour
{
    [SerializeField] private Transform objetivo;

    void LateUpdate()
    {
        if (objetivo == null) return;
        transform.position = objetivo.position;
        transform.rotation = objetivo.rotation;
    }
}