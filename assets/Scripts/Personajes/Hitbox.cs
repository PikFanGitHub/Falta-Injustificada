using UnityEngine;

public enum DireccionGolpe { Frente, Arriba, Abajo }

public enum TipoGolpe
{
    Normal,
    LanzarArriba,
    LanzarFrente,
    LanzarAbajo,
    Derribar,
    SiempreHit,
    HitDerribado
}

public class Hitbox : MonoBehaviour
{
    [Tooltip("Dano base. PersonajeBase lo sobreescribe con el valor del ScriptableObject.")]
    public float dano = 10f;

    [Tooltip("Tag del objeto que puede recibir dano (normalmente 'Jugador')")]
    public string tagEnemigo = "Jugador";

    [Tooltip("Desde que direccion viene este golpe")]
    public DireccionGolpe direccion = DireccionGolpe.Frente;

    [Tooltip("Tipo de reaccion que provoca en el enemigo")]
    public TipoGolpe tipoGolpe = TipoGolpe.Normal;

    [HideInInspector] public float impulsoY = 0f;
    [HideInInspector] public float impulsoZ = 0f;
    [HideInInspector] public float hitstunDuracion = 0.4f;
    [HideInInspector] public bool esCommand3 = false;
    [HideInInspector] public bool esCommand2 = false;
    [HideInInspector] public bool esPalaAtaquePrincipal = false;
    [HideInInspector] public bool esCancelable = false;
    // cuando es true, al conectar se llama NotificarUltimateImpacto() en el atacante
    // para que la corrutina EjecutarUltimate() sepa que el golpe conecto
    [HideInInspector] public bool esUltimate = false;
    // si true, esta hitbox pertenece a un intento de agarre y llama a RecibirAgarre en vez de RecibirGolpe
    [HideInInspector] public bool esAgarre = false;

    [Tooltip("Si true, esta hitbox solo se activa en contexto Suelo (Hitbox_Pala)")]
    [SerializeField] public bool soloSuelo = false;

    [Tooltip("Si true, esta hitbox no se activa en contexto Suelo (HitboxPatadaFuerte normal)")]
    [SerializeField] public bool ignorarEnSuelo = false;

    [Tooltip("Si true, no se aplica AjustarHitbox desde FighterData (hitboxes hijo de hueso del rig)")]
    [SerializeField] public bool ignorarAjusteTransform = false;

    private PersonajeBase personajePadre;
    private bool yaGolpeo = false;

    void Awake()
    {
        personajePadre = GetComponentInParent<PersonajeBase>();
    }

    void OnEnable() { yaGolpeo = false; }

    private void OnTriggerEnter(Collider other)
    {
        // notificar contacto con el suelo (logica de pala)
        if (esPalaAtaquePrincipal && other.CompareTag("Suelo"))
        {
            if (personajePadre != null)
            {
                // closestPoint da el punto exacto del collider suelo mas cercano al hitbox
                Vector3 posContacto = other.ClosestPoint(transform.position);
                personajePadre.NotificarPalaSuelo(posContacto);
            }
            return;
        }

        if (!other.CompareTag(tagEnemigo)) return;
        if (yaGolpeo) return;
        yaGolpeo = true;

        if (esCancelable && personajePadre != null)
            personajePadre.NotificarImpactoCancelable();

        PersonajeBase personajeEnemigo = other.GetComponentInParent<PersonajeBase>();
        if (personajeEnemigo != null)
        {
            if (esAgarre)
            {
                personajeEnemigo.RecibirAgarre(personajePadre);
            }
            else
            {
                Debug.Log($"[Hitbox] '{gameObject.name}' golpea TipoGolpe={tipoGolpe} direccion={direccion} hitstun={hitstunDuracion}");

                Vector3 puntoContacto = other.ClosestPoint(transform.position);

                ResultadoGolpe resultado = personajeEnemigo.RecibirGolpe(
                    hitstunDuracion,
                    direccion,
                    tipoGolpe,
                    impulsoY,
                    impulsoZ,
                    dano,
                    puntoContacto
                );

                if (resultado == ResultadoGolpe.Conectado && personajePadre != null)
                {
                    personajePadre.NotificarGolpeConectado(dano);
                    // hitbox de ultimate: avisar al atacante para que aplique el estado WinRar
                    if (esUltimate)
                        personajePadre.NotificarUltimateImpacto();
                }
                else if (resultado == ResultadoGolpe.Bloqueado && personajePadre != null)
                {
                    personajePadre.NotificarGolpeBloqueado();
                }
            }
        }

        if (esCommand3 && personajePadre != null) personajePadre.NotificarCommand3Impacto();
        if (esCommand2 && personajePadre != null) personajePadre.NotificarCommand2Impacto();
        if (esPalaAtaquePrincipal && personajePadre != null) personajePadre.NotificarPalaImpacto();
    }

    public void SetDano(float nuevoDano) => dano = nuevoDano;
    public void SetImpulso(float iy, float iz) { impulsoY = iy; impulsoZ = iz; }
}