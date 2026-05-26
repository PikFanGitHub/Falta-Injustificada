using UnityEngine;
using System.Collections;

public class Camus : PersonajeBase
{
    // acceso tipado al ScriptableObject propio
    private CamusData DatosCamus => datosPersonaje as CamusData;

    // portatil (Command1)
    private PortatilCamus _portatilActivo = null;

    // desplazamiento (Command3)
    private bool _enDesplazamiento = false;
    private Coroutine _corrutinaDesplazamiento = null;

    // impulso de ataque (patada floja/fuerte suelo)
    private Coroutine _corrutinaImpulsoAtaque = null;

    // gadgets QCF  (solo uno activo a la vez)
    [SerializeField] private GameObject visualEscudo;
    [SerializeField] private GameObject hijoLaptopComando1;

    // prop WinRAR que Camus sostiene durante la animacion de la ultimate.
    // es un GO hijo de Camus (no del enemigo). Empieza desactivado y
    // solo esta visible mientras dura la animacion de la ultimate.
    [SerializeField] private GameObject propWinRarUltimate;
    private MinaCamus _minaActiva = null;
    private TorretaCamus _torreta5Activa = null;
    private TorretaCamus _torretaInfActiva = null;
    private bool _escudoActivo = false;

    // gates de comandos

    protected override bool PuedeEjecutarCommand2()
        => PorlatilActivoYEnArea();

    protected override bool PuedeEjecutarCommand3()
        => !_enDesplazamiento;

    protected override bool PuedeEjecutarComandoQCF(int boton)
        => PorlatilActivoYEnArea();

    private bool PorlatilActivoYEnArea()
        => _portatilActivo != null
        && _portatilActivo.gameObject.activeSelf
        && _portatilActivo.EstaEnArea(transform);

    // start
    protected override void Start()
    {
        base.Start();
        if (hijoLaptopComando1 != null) hijoLaptopComando1.SetActive(false);
        if (propWinRarUltimate != null) propWinRarUltimate.SetActive(false);
    }

    // reinicio de ronda
    protected override void OnReiniciarCallback()
    {
        if (_corrutinaImpulsoAtaque != null)
        {
            StopCoroutine(_corrutinaImpulsoAtaque);
            _corrutinaImpulsoAtaque = null;
        }

        if (_portatilActivo != null)
        {
            _portatilActivo.Desaparecer();
            _portatilActivo = null;
        }
        if (hijoLaptopComando1 != null) hijoLaptopComando1.SetActive(false);

        if (_corrutinaDesplazamiento != null)
        {
            StopCoroutine(_corrutinaDesplazamiento);
            _corrutinaDesplazamiento = null;
        }
        _enDesplazamiento = false;

        DestruirGadgetActivo();
        if (propWinRarUltimate != null) propWinRarUltimate.SetActive(false);
    }

    // recibir golpe
    protected override void OnRecibirGolpeCallback()
    {
        if (_corrutinaImpulsoAtaque != null)
        {
            StopCoroutine(_corrutinaImpulsoAtaque);
            _corrutinaImpulsoAtaque = null;
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }

        if (hijoLaptopComando1 != null) hijoLaptopComando1.SetActive(false);
        if (propWinRarUltimate != null) propWinRarUltimate.SetActive(false);

        if (_enDesplazamiento)
        {
            if (_corrutinaDesplazamiento != null)
            {
                StopCoroutine(_corrutinaDesplazamiento);
                _corrutinaDesplazamiento = null;
            }
            _enDesplazamiento = false;
        }

        if (_torretaInfActiva != null)
        {
            _torretaInfActiva.Desaparecer();
            _torretaInfActiva = null;
        }
    }

    // escudo automatico
    protected override bool OnComprobarEscudo()
    {
        if (!_escudoActivo) return false;
        _escudoActivo = false;
        if (visualEscudo != null) visualEscudo.SetActive(false);
        return true;
    }

    // los ataques normales interrumpen el desplazamiento si estaba activo
    protected override void EjecutarAtaqueNormal(int boton)
    {
        if (_enDesplazamiento)
        {
            if (_corrutinaDesplazamiento != null)
            {
                StopCoroutine(_corrutinaDesplazamiento);
                _corrutinaDesplazamiento = null;
            }
            _enDesplazamiento = false;
        }

        base.EjecutarAtaqueNormal(boton);

        CamusData d = DatosCamus;
        if (d == null || contextoActual != ContextoPersonaje.Suelo) return;

        float impulso = 0f;
        if (boton == 2) impulso = d.impulsoPatadaFlojaSuelo;
        if (boton == 3) impulso = d.impulsoPatadaFuerteSuelo;

        if (impulso > 0f)
        {
            if (_corrutinaImpulsoAtaque != null)
            {
                StopCoroutine(_corrutinaImpulsoAtaque);
                _corrutinaImpulsoAtaque = null;
            }
            _corrutinaImpulsoAtaque = StartCoroutine(
                ImpulsoAtaque(-DireccionLanzado() * impulso, d.duracionImpulsoPatada));
        }
    }

    private IEnumerator ImpulsoAtaque(float velocidadZ, float duracion)
    {
        float t = 0f;
        while (t < duracion)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, velocidadZ);
            t += Time.deltaTime;
            yield return null;
        }
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        _corrutinaImpulsoAtaque = null;
    }

    // command1 - Portatil (QCB + Puno Flojo)
    protected override IEnumerator EjecutarCommand1()
    {
        CamusData d = DatosCamus;
        if (d == null)
        {
            Debug.LogError("[Camus] datosPersonaje no es CamusData. Asigna el asset correcto en el Inspector.");
            yield break;
        }
        if (d.prefabPortatil == null)
        {
            Debug.LogError("[Camus] prefabPortatil no asignado en CamusData.");
            yield break;
        }

        CancelarDesplazamientoSiActivo();
        IniciarComando();
        if (hijoLaptopComando1 != null) hijoLaptopComando1.SetActive(true);
        PlayAnim(animCommand1);

        yield return new WaitForSeconds(d.duracionAnimPortatil);

        if (hijoLaptopComando1 != null) hijoLaptopComando1.SetActive(false);

        if (_portatilActivo != null)
        {
            _portatilActivo.Desaparecer();
            _portatilActivo = null;
        }

        Vector3 posPortatil = transform.position;
        posPortatil.y += d.alturaPortatil;

        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down,
                            out RaycastHit hit, 2f, ~0, QueryTriggerInteraction.Ignore))
            posPortatil.y = hit.point.y + d.alturaPortatil;

        GameObject go = Object.Instantiate(d.prefabPortatil, posPortatil, Quaternion.identity);
        _portatilActivo = go.GetComponent<PortatilCamus>();

        if (_portatilActivo == null)
        {
            Debug.LogWarning("[Camus] El prefab del portatil no tiene componente PortatilCamus.");
            _portatilActivo = go.AddComponent<PortatilCamus>();
        }

        _portatilActivo.dueno = this;

        FinalizarComando();
    }

    // command2 - Animacion (QCB + Puno Fuerte, solo dentro del area del portatil)
    protected override IEnumerator EjecutarCommand2()
    {
        CamusData d = DatosCamus;
        float duracion = d != null ? d.duracionComando2 : 0.8f;

        CancelarDesplazamientoSiActivo();
        IniciarComando();
        yield return null;
        if (animator != null) animator.Play(animCommand2_1, -1, 0f);

        yield return new WaitForSeconds(duracion);

        FinalizarComando();
    }

    private void CancelarDesplazamientoSiActivo()
    {
        if (!_enDesplazamiento) return;
        if (_corrutinaDesplazamiento != null)
        {
            StopCoroutine(_corrutinaDesplazamiento);
            _corrutinaDesplazamiento = null;
        }
        _enDesplazamiento = false;
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }

    // command3 - desplazamiento hacia adelante (QCB + Patada Fuerte)
    protected override IEnumerator EjecutarCommand3()
    {
        CamusData d = DatosCamus;
        float durStart = d != null ? d.duracionAnimStart : 0.3f;
        float durLoop = d != null ? d.duracionDesplazamiento : 0.5f;
        float durEnd = d != null ? d.duracionAnimEnd : 0.3f;
        float vel = d != null ? d.velocidadDesplazamiento : 8f;
        string animStart = d != null ? d.animDesplazamientoStart : "CrawlStart";
        string animLoop = d != null ? d.animDesplazamientoLoop : "Crawl";
        string animEnd = d != null ? d.animDesplazamientoEnd : "CrawlEnd";

        IniciarComando();
        _enDesplazamiento = true;

        PlayAnim(animStart);
        yield return new WaitForSeconds(durStart);

        if (!_enDesplazamiento) { FinalizarComando(); yield break; }

        PlayAnim(animLoop);
        float dir = -DireccionLanzado();
        float t = 0f;
        while (t < durLoop && _enDesplazamiento)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, dir * vel);
            t += Time.deltaTime;
            yield return null;
        }
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

        if (!_enDesplazamiento) { FinalizarComando(); yield break; }

        PlayAnim(animEnd);
        yield return new WaitForSeconds(durEnd);

        _enDesplazamiento = false;
        FinalizarComando();
    }

    // comandos QCF
    protected override IEnumerator EjecutarComandoQCF(int boton)
    {
        CamusData d = DatosCamus;

        CancelarDesplazamientoSiActivo();
        DestruirGadgetActivo();

        IniciarComando();
        yield return null;
        if (animator != null) animator.Play(animCommand2_1, -1, 0f);
        yield return new WaitForSeconds(d != null ? d.duracionComando2 : 0.8f);

        switch (boton)
        {
            case 2: SpawnMina(d); break;
            case 3: SpawnTorreta5(d); break;
            case 0: SpawnTorretaInfinita(d); break;
            case 1:
                _escudoActivo = true;
                if (visualEscudo != null)
                    visualEscudo.SetActive(true);
                else
                    Debug.LogWarning("[Camus] visualEscudo no asignado en el Inspector.");
                break;
        }

        FinalizarComando();
    }

    // ultimate - QCF + QCF + Puno Fuerte
    protected override IEnumerator EjecutarUltimate()
    {
        CamusData d = DatosCamus;
        if (d == null) yield break;

        // gastar los stocks configurados; GastarStocksUltimate() usa stocksNecesariosUltimate
        if (!GastarStocksUltimate()) yield break;

        _ultimateGolpeo = false;
        CancelarDesplazamientoSiActivo();

        // mostrar el prop WinRAR de Camus durante la animacion
        if (propWinRarUltimate != null) propWinRarUltimate.SetActive(true);

        // configurar datos de las hitboxes sin activarlas todavia
        if (hitboxesUltimate != null)
        {
            foreach (var hb in hitboxesUltimate)
            {
                if (hb == null) continue;
                Hitbox h = hb.GetComponent<Hitbox>();
                if (h == null) continue;
                h.SetDano(d.danoUltimate);
                h.direccion = DireccionGolpe.Frente;
                h.tipoGolpe = TipoGolpe.SiempreHit;
                h.esCancelable = false;
                h.hitstunDuracion = d.hitstunUltimate;
                h.esUltimate = true;
                h.SetImpulso(d.impulsoYUltimate, d.impulsoZUltimate);
                hb.SetActive(false);
            }
        }

        IniciarComando();

        // asignar hitboxesActuales antes de reproducir la animacion para que
        // animEvent_AbrirHitbox / AnimEvent_CerrarHitbox del clip puedan actuar
        hitboxesActuales = hitboxesUltimate;

        PlayAnim(d.animUltimate);

        // startup (fallback por temporizador)
        // si el clip tiene AnimEvent_AbrirHitbox se lo salta porque la hitbox
        // ya estara abierta cuando llegue aqui el temporizador.
        yield return new WaitForSeconds(d.duracionStartupUltimate);

        if (estadoActual != EstadoJugador.Comando) yield break;

        // abrir solo si ningun animation event lo hizo ya
        bool yaAbierta = hitboxesUltimate != null
                         && hitboxesUltimate.Length > 0
                         && hitboxesUltimate[0] != null
                         && hitboxesUltimate[0].activeSelf;
        if (!yaAbierta)
        {
            AjustarHitboxes(hitboxesUltimate);   // <-- orienta la hitbox segun el flip del personaje
            SetHitboxes(hitboxesUltimate, true);
        }

        // frames activos (fallback por temporizador)
        // si el clip tiene AnimEvent_CerrarHitbox, cerrara la hitbox antes de
        // que expire este bucle. El bucle solo sirve de seguridad.
        float tActivo = 0f;
        while (tActivo < d.duracionActivaUltimate)
        {
            if (_ultimateGolpeo) break;
            if (estadoActual != EstadoJugador.Comando) break;
            tActivo += Time.deltaTime;
            yield return null;
        }

        // cerrar hitboxes (si el anim event ya las cerro, SetActive(false) es inocuo)
        SetHitboxes(hitboxesUltimate, false);
        hitboxesActuales = null;

        // ocultar el prop de Camus
        if (propWinRarUltimate != null) propWinRarUltimate.SetActive(false);

        // aplicar WinRar al oponente si golpeo
        if (_ultimateGolpeo && oponente != null)
        {
            // particulas en la posicion del rival
            if (d.prefabParticulasUltimate != null)
                Object.Instantiate(d.prefabParticulasUltimate,
                                   oponente.position + Vector3.up * 0.8f,
                                   Quaternion.identity);

            PersonajeBase enemigoBase = oponente.GetComponent<PersonajeBase>();
            // playHitAnim: true  el rival reproducira Hurt en lugar de Idle al entrar en WinRar
            enemigoBase?.EntrarEstadoWinRar(d.duracionEstadoWinRar, playHitAnim: true);
        }

        // recovery
        yield return new WaitForSeconds(d.duracionRecoveryUltimate);

        FinalizarComando();
    }

    // QCF + Patada Floja (boton 2) -> Mina
    private void SpawnMina(CamusData d)
    {
        if (d == null || d.prefabMina == null)
        {
            Debug.LogError("[Camus] prefabMina no asignado en CamusData.");
            return;
        }

        Vector3 pos = ObtenerPosicionEntreJugadorYEnemigo();
        GameObject go = Object.Instantiate(d.prefabMina, pos, Quaternion.identity);
        _minaActiva = go.GetComponent<MinaCamus>();
        if (_minaActiva == null) _minaActiva = go.AddComponent<MinaCamus>();

        _minaActiva.dueno = this;
        _minaActiva.danio = d.danioMina;
        _minaActiva.hitstunDuracion = d.hitstunMina;
        _minaActiva.impulsoY = d.impulsoYMina;
        _minaActiva.impulsoZ = d.impulsoZMina;
        _minaActiva.tiempoExplosion = d.tiempoExplosionMina;
    }

    // QCF + Patada Fuerte (boton 3) -> Torreta 5 proyectiles
    private void SpawnTorreta5(CamusData d)
    {
        if (d == null || d.prefabTorreta5 == null)
        {
            Debug.LogError("[Camus] prefabTorreta5 no asignado en CamusData.");
            return;
        }

        Vector3 pos = ObtenerPosicionEncimaPersonaje();
        GameObject go = Object.Instantiate(d.prefabTorreta5, pos, Quaternion.identity);
        _torreta5Activa = go.GetComponent<TorretaCamus>();
        if (_torreta5Activa == null) _torreta5Activa = go.AddComponent<TorretaCamus>();

        _torreta5Activa.dueno = this;
        _torreta5Activa.objetivo = oponente;
        _torreta5Activa.danioProyectil = d.danioproyectilTorreta5;
        _torreta5Activa.hitstunProyectil = d.hitstunTorreta5;
        _torreta5Activa.intervaloDisparo = d.intervaloTorreta5;
        _torreta5Activa.prefabProyectil = ObtenerPrefabProyectilDeTorreta(go);

        _torreta5Activa.IniciarTorreta5(5);
    }

    // QCF + Puno Flojo (boton 0) -> Torreta infinita
    private void SpawnTorretaInfinita(CamusData d)
    {
        if (d == null || d.prefabTorretaInfinita == null)
        {
            Debug.LogError("[Camus] prefabTorretaInfinita no asignado en CamusData.");
            return;
        }

        Vector3 pos = ObtenerPosicionEncimaPersonaje();
        GameObject go = Object.Instantiate(d.prefabTorretaInfinita, pos, Quaternion.identity);
        _torretaInfActiva = go.GetComponent<TorretaCamus>();
        if (_torretaInfActiva == null) _torretaInfActiva = go.AddComponent<TorretaCamus>();

        _torretaInfActiva.dueno = this;
        _torretaInfActiva.objetivo = oponente;
        _torretaInfActiva.danioProyectil = d.danioProyectilTorretaInfinita;
        _torretaInfActiva.hitstunProyectil = d.hitstunTorretaInfinita;
        _torretaInfActiva.intervaloDisparo = d.intervaloTorretaInfinita;
        _torretaInfActiva.prefabProyectil = ObtenerPrefabProyectilDeTorreta(go);

        _torretaInfActiva.IniciarTorretaInfinita();
    }

    // utilidades

    private void DestruirGadgetActivo()
    {
        if (_minaActiva != null)
        {
            _minaActiva.Desaparecer();
            _minaActiva = null;
        }
        // comprobacion extra: referencia sucia (objeto ya destruido por Unity)
        if (_torreta5Activa != null)
        {
            _torreta5Activa.Desaparecer();   // desaparecer ya llama NotificarTorretaDestruida
            _torreta5Activa = null;
        }
        if (_torretaInfActiva != null)
        {
            _torretaInfActiva.Desaparecer();
            _torretaInfActiva = null;
        }
        _escudoActivo = false;
        if (visualEscudo != null) visualEscudo.SetActive(false);
    }

    private Vector3 ObtenerPosicionSuelo()
    {
        Vector3 pos = transform.position;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down,
                            out RaycastHit hit, 2f, ~0, QueryTriggerInteraction.Ignore))
            pos.y = hit.point.y + 0.05f;
        return pos;
    }

    private Vector3 ObtenerPosicionEntreJugadorYEnemigo()
    {
        if (oponente == null) return ObtenerPosicionSuelo();

        Vector3 medio = (transform.position + oponente.position) * 0.5f;

        if (Physics.Raycast(medio + Vector3.up * 1f, Vector3.down,
                            out RaycastHit hit, 3f, ~0, QueryTriggerInteraction.Ignore))
            medio.y = hit.point.y + 0.05f;
        else
            medio.y = transform.position.y;

        return medio;
    }

    private Vector3 ObtenerPosicionEncimaPersonaje()
    {
        CamusData d = DatosCamus;
        float offsetExtra = d != null ? d.alturaTorreta : 1f;
        Collider col = GetComponent<Collider>();
        float alturaPersonaje = col != null ? col.bounds.size.y : 1.8f;
        return transform.position + Vector3.up * (alturaPersonaje + offsetExtra);
    }

    private GameObject ObtenerPrefabProyectilDeTorreta(GameObject torretaGO)
    {
        ProyectilCamus proj = torretaGO.GetComponentInChildren<ProyectilCamus>(true);
        if (proj != null) return proj.gameObject;

        TorretaCamus t = torretaGO.GetComponent<TorretaCamus>();
        if (t != null && t.prefabProyectil != null) return t.prefabProyectil;

        Debug.LogWarning("[Camus] No se encontro prefabProyectil en la torreta. Asignalo manualmente en TorretaCamus o como hijo del prefab.");
        return null;
    }

    // callback que llaman las torretas al autodestruirse
    public void NotificarTorretaDestruida(TorretaCamus torreta)
    {
        if (_torreta5Activa == torreta) _torreta5Activa = null;
        if (_torretaInfActiva == torreta) _torretaInfActiva = null;
    }

}