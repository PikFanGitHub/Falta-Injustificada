using UnityEngine;
using System.Collections;


public class TorretaCamus : MonoBehaviour
{
    // campos asignados por Camus.cs
    [HideInInspector] public Camus dueno;
    [HideInInspector] public Transform objetivo;
    public GameObject prefabProyectil;

    [HideInInspector] public float danioProyectil = 10f;
    [HideInInspector] public float hitstunProyectil = 0.3f;
    [HideInInspector] public float intervaloDisparo = 0.3f;

    // estado interno
    private Coroutine _corrutinaDisparo;
    private bool _activa = false;

    // inicia la torreta de rafaga fija (N disparos)
    public void IniciarTorreta5(int numProyectiles = 5)
    {
        if (_activa) return;
        _activa = true;
        _corrutinaDisparo = StartCoroutine(SecuenciaRafaga(numProyectiles));
    }

    // inicia la torreta infinita, se detiene llamando a Desaparecer() desde Camus.cs
    public void IniciarTorretaInfinita()
    {
        if (_activa) return;
        _activa = true;
        _corrutinaDisparo = StartCoroutine(SecuenciaInfinita());
    }

    // detiene el disparo sin destruir el objeto
    public void DetenerTorreta()
    {
        _activa = false;
        if (_corrutinaDisparo != null)
        {
            StopCoroutine(_corrutinaDisparo);
            _corrutinaDisparo = null;
        }
    }

    // detiene y destruye la torreta, llamado desde Camus.cs
    public void Desaparecer()
    {
        DetenerTorreta();
        dueno?.NotificarTorretaDestruida(this);
        gameObject.SetActive(false);
        Destroy(gameObject, 0.05f);
    }

    // corrutinas de disparo

    private IEnumerator SecuenciaRafaga(int cantidad)
    {
        for (int i = 0; i < cantidad && _activa; i++)
        {
            Disparar();
            yield return new WaitForSeconds(intervaloDisparo);
        }
        // rafaga terminada, la torreta desaparece
        Desaparecer();
    }

    private IEnumerator SecuenciaInfinita()
    {
        while (_activa)
        {
            Disparar();
            yield return new WaitForSeconds(intervaloDisparo);
        }
    }

    private void Disparar()
    {
        if (prefabProyectil == null || objetivo == null) return;

        GameObject go = Instantiate(prefabProyectil, transform.position + Vector3.up * 0.3f, Quaternion.identity);

        ProyectilCamus proj = go.GetComponent<ProyectilCamus>();
        if (proj != null)
        {
            proj.dueno = dueno;
            proj.danio = danioProyectil;
            proj.hitstunDuracion = hitstunProyectil;
            proj.objetivo = objetivo;
        }
    }
}