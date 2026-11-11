using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// IA base de esbirros: Patrulla -> Persecución -> Ataque melé con telegrafiado -> Muerte (suelta almas).
/// El daño al Chofer se aplica por distancia al final del telegrafiado (no depende de triggers),
/// y el Chofer puede esquivarlo (i-frames) o alejarse durante la preparación.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PoderEspiritual))]
public class EsbirroBaseIA : MonoBehaviour
{
    public enum TipoEsbirro { Serpiente, Momia, HombrePiedra, Monje, Cuervo, Catrin, Comadre }
    public enum Estado { Patrulla, Persecucion, Preparando, Atacando, Muerto }

    [Header("Identidad del Enemigo")]
    public TipoEsbirro tipo = TipoEsbirro.Serpiente;

    [Header("Movimiento e IA")]
    public float velocidadPersecucion = 3.2f;
    public float velocidadPatrulla = 1.5f;
    public float rangoDeteccion = 9f;
    [Tooltip("Si el jugador se aleja más que esto (x rangoDeteccion) el enemigo deja de perseguir.")]
    public float factorPerdida = 1.6f;
    public float rangoAtaque = 1.6f;
    public float radioPatrulla = 6f;
    public float gravedad = -15f;

    [Header("Combate")]
    public float danio = 15f;
    public float cooldownAtaque = 1.8f;
    [Tooltip("Tiempo de aviso antes del golpe (el jugador puede esquivar).")]
    public float tiempoTelegrafiado = 0.55f;
    [Tooltip("Obsoleto: el daño ahora se aplica por distancia. Se conserva por compatibilidad.")]
    public GameObject hitboxAtaqueEnemigo;
    public float resistenciaEmpuje = 1f;

    [Header("Ataque a distancia (Comadres)")]
    [Tooltip("Si es true, en vez de golpear de cerca lanza un proyectil (rangoAtaque pasa a ser la distancia de tiro).")]
    public bool ataqueADistancia = false;
    public float velocidadProyectil = 11f;
    public Color colorProyectil = new Color(0.9f, 0.4f, 1f);

    [Header("Recompensa al Morir (Regla 2 GDD)")]
    public GameObject prefabTotemDrop;
    public int cantidadTotemsADropear = 0;
    [Tooltip("Almas (energía espiritual) que suelta al ser derrotado.")]
    public int cantidadAlmasADropear = 3;

    [Header("Feedback")]
    public float alturaBarraVida = 2.4f;

    public bool EstaMuerto { get { return estado == Estado.Muerto; } }
    public Estado EstadoActual { get { return estado; } }

    /// <summary>Se dispara cuando cualquier esbirro es derrotado.</summary>
    public static event Action<EsbirroBaseIA> OnEsbirroDerrotado;

    private Transform playerTransform;
    private PoderEspiritual vidaJugador;
    private CharacterController controller;
    private PoderEspiritual vida;
    private Vector3 velocidadVertical;
    private Vector3 empuje;
    private float tiempoProximoAtaque = 0f;
    private float aturdidoHasta = 0f;
    private Estado estado = Estado.Patrulla;

    // patrulla
    private Vector3 puntoOrigen;
    private Vector3 destinoPatrulla;
    private float esperaPatrulla = 0f;

    // visual
    private Transform visual;
    private Vector3 visualEscalaBase;
    private Vector3 visualPosBase;
    private Quaternion visualRotBase;
    private Renderer[] renderers;
    private MaterialPropertyBlock mpb;
    private float flashHasta = 0f;
    private bool telegrafiando = false;
    private float faseAnim;
    private bool seMovio;

    // barra de vida
    private Transform barraRaiz;
    private Transform barraRelleno;
    private float barraVisibleHasta = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        vida = GetComponent<PoderEspiritual>();
        vida.OnMuerte += Morir;
        vida.OnRecibioDanio += AlRecibirDanio;

        puntoOrigen = transform.position;
        destinoPatrulla = puntoOrigen;

        BuscarJugador();

        if (hitboxAtaqueEnemigo != null) hitboxAtaqueEnemigo.SetActive(false);

        // Visual procedural (hijo "Visual") o cualquier hijo con renderers
        visual = transform.Find("Visual");
        if (visual == null && transform.childCount > 0) visual = transform.GetChild(0);
        if (visual != null)
        {
            visualEscalaBase = visual.localScale;
            visualPosBase = visual.localPosition;
            visualRotBase = visual.localRotation;
        }
        renderers = GetComponentsInChildren<Renderer>();
        mpb = new MaterialPropertyBlock();

        CrearBarraVida();
    }

    void BuscarJugador()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            vidaJugador = playerObj.GetComponent<PoderEspiritual>();
        }
    }

    void Update()
    {
        if (estado == Estado.Muerto || !controller.enabled) return;

        AplicarGravedadYEmpuje();

        if (playerTransform == null) { BuscarJugador(); return; }
        if (Time.time < aturdidoHasta) return;

        bool jugadorVivo = vidaJugador == null || !vidaJugador.EstaMuerto;
        Vector3 haciaJugador = playerTransform.position - transform.position;
        haciaJugador.y = 0f;
        float dist = haciaJugador.magnitude;

        seMovio = false;

        switch (estado)
        {
            case Estado.Patrulla:
                if (jugadorVivo && dist <= rangoDeteccion) { estado = Estado.Persecucion; break; }
                Patrullar();
                break;

            case Estado.Persecucion:
                if (!jugadorVivo || dist > rangoDeteccion * factorPerdida) { estado = Estado.Patrulla; puntoOrigen = transform.position; break; }
                MirarHacia(haciaJugador, 9f);
                if (ataqueADistancia && dist < rangoAtaque * 0.4f)
                {
                    // las tiradoras guardan la distancia
                    controller.Move(-haciaJugador.normalized * velocidadPersecucion * 0.8f * Time.deltaTime);
                    seMovio = true;
                    if (Time.time >= tiempoProximoAtaque) StartCoroutine(RutinaAtaque());
                }
                else if (dist > rangoAtaque)
                {
                    controller.Move(haciaJugador.normalized * velocidadPersecucion * Time.deltaTime);
                    seMovio = true;
                }
                else if (Time.time >= tiempoProximoAtaque)
                {
                    StartCoroutine(RutinaAtaque());
                }
                break;
        }
    }

    void Patrullar()
    {
        if (esperaPatrulla > 0f) { esperaPatrulla -= Time.deltaTime; return; }

        Vector3 hacia = destinoPatrulla - transform.position;
        hacia.y = 0f;
        if (hacia.magnitude < 0.6f)
        {
            // elegir nuevo punto cerca del origen y esperar un poco
            Vector2 r = UnityEngine.Random.insideUnitCircle * radioPatrulla;
            destinoPatrulla = puntoOrigen + new Vector3(r.x, 0f, r.y);
            esperaPatrulla = UnityEngine.Random.Range(1f, 3f);
            return;
        }
        MirarHacia(hacia, 5f);
        controller.Move(hacia.normalized * velocidadPatrulla * Time.deltaTime);
        seMovio = true;
    }

    void MirarHacia(Vector3 dir, float velocidad)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir.normalized), velocidad * Time.deltaTime);
    }

    IEnumerator RutinaAtaque()
    {
        estado = Estado.Preparando;
        tiempoProximoAtaque = Time.time + cooldownAtaque + tiempoTelegrafiado;

        // 1) Telegrafiado: se encoge/enrojece para avisar
        telegrafiando = true;
        float t = 0f;
        while (t < tiempoTelegrafiado)
        {
            if (estado == Estado.Muerto) yield break;
            if (Time.time < aturdidoHasta) { telegrafiando = false; estado = Estado.Persecucion; yield break; } // golpeado: se cancela
            t += Time.deltaTime;
            if (playerTransform != null)
            {
                Vector3 h = playerTransform.position - transform.position;
                MirarHacia(h, 6f);
            }
            yield return null;
        }
        telegrafiando = false;
        if (estado == Estado.Muerto) yield break;   // murió justo al terminar el aviso: no revivirlo

        // 2a) Ataque a distancia: lanza un murmullo hacia el Chofer
        if (ataqueADistancia)
        {
            estado = Estado.Atacando;
            if (playerTransform != null && vidaJugador != null && !vidaJugador.EstaMuerto)
            {
                Vector3 origen = transform.position + Vector3.up * 1.4f + transform.forward * 0.5f;
                Vector3 objetivo = playerTransform.position + Vector3.up * 0.9f;
                ProyectilEspiritual.Crear(origen, objetivo - origen, velocidadProyectil, danio, colorProyectil, 0.5f, 3.5f);
            }
            yield return new WaitForSeconds(0.35f);
            if (estado != Estado.Muerto) estado = Estado.Persecucion;
            yield break;
        }

        // 2b) Golpe: pequeña embestida y comprobación de impacto
        estado = Estado.Atacando;
        float dur = 0.18f; t = 0f;
        while (t < dur)
        {
            if (estado == Estado.Muerto) yield break;
            t += Time.deltaTime;
            controller.Move(transform.forward * (rangoAtaque * 0.7f) * Time.deltaTime / dur);
            yield return null;
        }

        if (estado == Estado.Muerto) yield break;

        if (playerTransform != null && vidaJugador != null && !vidaJugador.EstaMuerto)
        {
            Vector3 h = playerTransform.position - transform.position; h.y = 0f;
            bool enRango = h.magnitude <= rangoAtaque * 1.35f;
            bool enFrente = Vector3.Angle(transform.forward, h) < 80f;
            if (enRango && enFrente)
            {
                vidaJugador.RecibirDanio(danio); // si el Chofer está esquivando (i-frames) no recibe daño
            }
        }

        yield return new WaitForSeconds(0.25f);
        if (estado != Estado.Muerto) estado = Estado.Persecucion;
    }

    void AplicarGravedadYEmpuje()
    {
        if (controller == null || !controller.enabled) return;

        if (controller.isGrounded && velocidadVertical.y < 0) velocidadVertical.y = -2f;
        velocidadVertical.y += gravedad * Time.deltaTime;
        controller.Move(velocidadVertical * Time.deltaTime);

        if (empuje.sqrMagnitude > 0.01f)
        {
            controller.Move(empuje * Time.deltaTime);
            empuje = Vector3.Lerp(empuje, Vector3.zero, 1f - Mathf.Exp(-9f * Time.deltaTime));
        }
    }

    /// <summary>Empuja al esbirro (retroceso al ser golpeado).</summary>
    public void AplicarEmpuje(Vector3 direccion, float fuerza)
    {
        if (estado == Estado.Muerto) return;
        direccion.y = 0f;
        empuje = direccion.normalized * fuerza / Mathf.Max(0.2f, resistenciaEmpuje);
    }

    void AlRecibirDanio(float cantidad)
    {
        if (estado == Estado.Muerto) return;
        flashHasta = Time.time + 0.12f;
        barraVisibleHasta = Time.time + 4f;
        aturdidoHasta = Time.time + Mathf.Min(0.45f, 0.12f + cantidad * 0.006f);
        if (estado == Estado.Patrulla) estado = Estado.Persecucion; // al ser golpeado, aunque no haya visto al Chofer
    }

    void Morir()
    {
        if (estado == Estado.Muerto) return;
        estado = Estado.Muerto;
        Debug.Log("[" + gameObject.name + "] Esbirro derrotado.");

        gameObject.tag = "Untagged"; // deja de ser objetivo
        controller.enabled = false;
        if (barraRaiz != null) barraRaiz.gameObject.SetActive(false);

        Vector3 centro = transform.position + Vector3.up * 0.7f;

        // Almas (energía espiritual)
        if (cantidadAlmasADropear > 0) AlmaRecolectable.CrearGrupo(centro, cantidadAlmasADropear, true);

        // Tótems (solo si se configuró)
        if (prefabTotemDrop != null)
        {
            for (int i = 0; i < cantidadTotemsADropear; i++)
            {
                Vector3 dispersion = new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 0.5f, UnityEngine.Random.Range(-0.5f, 0.5f));
                Instantiate(prefabTotemDrop, transform.position + dispersion, Quaternion.identity);
            }
        }
        else if (cantidadTotemsADropear > 0 && InventarioEspiritual.Instancia != null)
        {
            InventarioEspiritual.Instancia.AgregarTotems(cantidadTotemsADropear);
        }

        if (OnEsbirroDerrotado != null) OnEsbirroDerrotado(this);
        StartCoroutine(AnimacionMuerte());
    }

    IEnumerator AnimacionMuerte()
    {
        // Se desvanece hundiéndose y encogiéndose
        float dur = 0.7f, t = 0f;
        Vector3 escalaInicial = transform.localScale;
        Vector3 posInicial = transform.position;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = t / dur;
            transform.localScale = escalaInicial * Mathf.Lerp(1f, 0.05f, k * k);
            transform.position = posInicial + Vector3.down * (k * 0.6f);
            transform.Rotate(0f, 360f * Time.deltaTime, 0f);
            yield return null;
        }
        Destroy(gameObject);
    }

    // ============================================================ VISUAL / BARRA DE VIDA

    void LateUpdate()
    {
        if (estado == Estado.Muerto) return;

        AplicarTinte();
        AnimarVisualProcedural();
        ActualizarBarraVida();
    }

    void AplicarTinte()
    {
        if (renderers == null) return;
        bool flash = Time.time < flashHasta;
        if (!flash && !telegrafiando && !mpbSucio) return;

        Color c = Color.white;
        bool usar = false;
        if (flash) { c = new Color(1f, 1f, 1f, 1f); usar = true; }
        else if (telegrafiando)
        {
            float p = 0.5f + 0.5f * Mathf.Sin(Time.time * 28f);
            c = Color.Lerp(new Color(1f, 0.25f, 0.2f), new Color(1f, 0.8f, 0.2f), p);
            usar = true;
        }

        foreach (Renderer r in renderers)
        {
            if (r == null || r is TrailRenderer || r is ParticleSystemRenderer || r is LineRenderer) continue;
            if (usar)
            {
                r.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", c);
                mpb.SetColor("_Color", c);
                r.SetPropertyBlock(mpb);
            }
            else
            {
                r.SetPropertyBlock(null); // vuelve al color original del material
            }
        }
        mpbSucio = usar;
    }
    private bool mpbSucio = false;

    void AnimarVisualProcedural()
    {
        if (visual == null) return;

        faseAnim += Time.deltaTime * (seMovio ? 9f : 2.5f);

        Vector3 pos = visualPosBase;
        Quaternion rot = visualRotBase;
        Vector3 esc = visualEscalaBase;

        if (tipo == TipoEsbirro.Serpiente)
        {
            // ondulación lateral
            rot = visualRotBase * Quaternion.Euler(0f, Mathf.Sin(faseAnim) * (seMovio ? 14f : 4f), 0f);
        }
        else
        {
            // pisadas pesadas
            pos += Vector3.up * Mathf.Abs(Mathf.Sin(faseAnim * 0.5f)) * (seMovio ? 0.10f : 0.02f);
            rot = visualRotBase * Quaternion.Euler(0f, 0f, Mathf.Sin(faseAnim * 0.5f) * (seMovio ? 3f : 0.6f));
        }

        if (telegrafiando)
        {
            // se agazapa y crece antes de golpear
            esc = Vector3.Scale(visualEscalaBase, new Vector3(1.12f, 0.88f, 1.12f));
            pos += transform.forward * -0.12f;
        }

        visual.localPosition = pos;
        visual.localRotation = rot;
        visual.localScale = esc;
    }

    void CrearBarraVida()
    {
        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) return;

        GameObject raiz = new GameObject("BarraVida");
        raiz.transform.SetParent(transform, false);
        raiz.transform.localPosition = new Vector3(0f, alturaBarraVida, 0f);
        barraRaiz = raiz.transform;

        GameObject fondo = GameObject.CreatePrimitive(PrimitiveType.Quad);
        fondo.name = "Fondo";
        fondo.transform.SetParent(raiz.transform, false);
        fondo.transform.localScale = new Vector3(1.1f, 0.12f, 1f);
        Destroy(fondo.GetComponent<Collider>());
        Material mFondo = new Material(sh); mFondo.color = new Color(0f, 0f, 0f, 0.7f);
        fondo.GetComponent<Renderer>().sharedMaterial = mFondo;
        fondo.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        GameObject relleno = GameObject.CreatePrimitive(PrimitiveType.Quad);
        relleno.name = "Relleno";
        relleno.transform.SetParent(raiz.transform, false);
        relleno.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        Destroy(relleno.GetComponent<Collider>());
        Material mRel = new Material(sh); mRel.color = new Color(0.95f, 0.25f, 0.25f, 1f);
        relleno.GetComponent<Renderer>().sharedMaterial = mRel;
        relleno.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        barraRelleno = relleno.transform;

        raiz.SetActive(false);
    }

    void ActualizarBarraVida()
    {
        if (barraRaiz == null) return;

        bool visible = Time.time < barraVisibleHasta && vida != null && !vida.EstaMuerto;
        if (barraRaiz.gameObject.activeSelf != visible) barraRaiz.gameObject.SetActive(visible);
        if (!visible) return;

        float p = vida.Porcentaje;
        const float ancho = 1.04f;
        barraRelleno.localScale = new Vector3(Mathf.Max(0.001f, ancho * p), 0.08f, 1f);
        barraRelleno.localPosition = new Vector3(-(ancho * (1f - p)) * 0.5f, 0f, -0.01f);

        // billboard hacia la cámara (compensando la escala del enemigo)
        if (Camera.main != null)
        {
            barraRaiz.rotation = Quaternion.LookRotation(barraRaiz.position - Camera.main.transform.position);
        }
    }

    void OnDestroy()
    {
        if (vida != null)
        {
            vida.OnMuerte -= Morir;
            vida.OnRecibioDanio -= AlRecibirDanio;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangoAtaque);
    }
}
