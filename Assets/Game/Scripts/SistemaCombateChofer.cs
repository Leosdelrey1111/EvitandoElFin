using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Combate melé del Chofer: combo de 3 golpes (click izquierdo / F) + Onda Espiritual de área (Q).
///
/// - La detección de impactos se hace con OverlapSphere + cono frontal en el instante del golpe
///   (no depende de triggers ni de Rigidbody, así no hay "golpes fantasma" ni golpes que no registran).
/// - Mientras no exista un clip de ataque en el Animator, el golpe se anima de forma procedural
///   (torsión/inclinación del modelo, impulso hacia adelante, estela de energía, partículas y sonido).
///   Si el Animator tiene un parámetro Trigger llamado "Atacar" (o "Atacar2", "Atacar3", "AtaqueArea") se dispara también.
/// - Hit-freeze breve y golpe de FOV para dar peso a cada impacto.
/// </summary>
public class SistemaCombateChofer : MonoBehaviour
{
    [Header("Configuración de Ataques Melé (Combo)")]
    [Tooltip("Obsoleto: se conserva por compatibilidad con escenas anteriores. El daño ahora se calcula por área, no por trigger.")]
    public GameObject hitboxMele;
    public float danioAtaque1 = 20f;
    public float danioAtaque2 = 30f;
    public float danioAtaque3 = 45f;

    [Header("Alcance del golpe")]
    public float alcanceGolpe = 2.4f;
    [Range(40f, 220f)] public float anguloGolpe = 150f;

    [Header("Tiempos de Combo")]
    [Tooltip("Tiempo máximo entre golpes para seguir el combo")]
    public float tiempoVentanaCombo = 0.9f;
    public float duracionGolpe = 0.34f;
    public float duracionGolpeFinal = 0.52f;
    public float cooldownEntreGolpes = 0.06f;
    [Tooltip("Impulso hacia adelante (m/s) durante el golpe")]
    public float impulsoAdelante = 4.5f;
    [Range(0.1f, 1f)] public float velocidadMientrasAtaca = 0.45f;

    [Header("Ataque de Área (Onda Espiritual)")]
    public KeyCode teclaAtaqueArea = KeyCode.Q;
    public float radioArea = 5f;
    public float danioArea = 50f;
    public float cooldownArea = 5f;

    [Header("Feedback")]
    public Color colorEspiritual = new Color(0.35f, 0.95f, 1f, 1f);
    [Range(0f, 1f)] public float volumenSfx = 0.8f;

    // ---------- estado público para el HUD ----------
    public int PasoCombo { get { return comboPaso; } }
    /// <summary>0 = recién usada, 1 = lista para usar de nuevo.</summary>
    public float ProgresoAtaqueArea { get { return cooldownArea <= 0f ? 1f : Mathf.Clamp01(1f - (tiempoProximoAtaqueArea - Time.time) / cooldownArea); } }
    public bool Atacando { get { return atacando; } }

    // ---------- interno ----------
    private ChoferController chofer;
    private CharacterController controller;
    private Animator animator;
    private Transform modelo;
    private Vector3 modeloPosBase;
    private Quaternion modeloRotBase;
    private bool modeloInicializado;

    private int comboPaso = 0;
    private float tiempoUltimoGolpe = -10f;
    private float tiempoProximoAtaqueArea = 0f;
    private bool atacando = false;
    private float congelarHasta = 0f;

    // Buffer de entrada: si se pulsa una acción mientras otra termina, se ejecuta en cuanto se puede
    private enum Pendiente { Nada, Golpe, Onda }
    private Pendiente pendiente = Pendiente.Nada;
    private float pendienteHasta = 0f;
    private bool enRecuperacion = false;   // true cuando el golpe actual ya conectó y solo falta recuperarse (se puede cancelar)

    // pose procedural
    private Vector3 poseRot;
    private float poseAdelante;
    private bool usarPoseProcedural = true;

    // VFX
    private Transform puntaEstela;
    private TrailRenderer estela;
    private ParticleSystem chispas;
    private LineRenderer anilloArea;
    private Material matSprite;
    private AudioSource audioFx;
    private AudioClip clipSwoosh, clipImpacto, clipArea;
    private CamaraTerceraPersona camara;
    private float golpeFov = 0f;
    private bool golpeFovActivo = false;
    private readonly HashSet<PoderEspiritual> golpeados = new HashSet<PoderEspiritual>();

    void Start()
    {
        chofer = GetComponent<ChoferController>();
        controller = GetComponent<CharacterController>();
        animator = chofer != null && chofer.animator != null ? chofer.animator : GetComponentInChildren<Animator>();
        modelo = animator != null && animator.transform != transform ? animator.transform : null;

        if (hitboxMele != null) hitboxMele.SetActive(false); // ya no se usa

        // ¿El Animator tiene animación de ataque real? (trigger "Atacar")
        usarPoseProcedural = true;
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (AnimatorControllerParameter p in animator.parameters)
                if (p.type == AnimatorControllerParameterType.Trigger && p.name == "Atacar") usarPoseProcedural = false;
        }

        if (Camera.main != null) camara = Camera.main.GetComponent<CamaraTerceraPersona>();

        CrearEfectos();
    }

    void Update()
    {
        // Sin input durante pausa o secuencias (intro) con el control bloqueado
        if (Time.timeScale <= 0.01f) return;
        if (chofer != null && chofer.controlBloqueado) return;

        // El combo se reinicia si pasa mucho tiempo sin golpear
        if (!atacando && comboPaso > 0 && Time.time - tiempoUltimoGolpe > tiempoVentanaCombo)
        {
            comboPaso = 0;
        }

        // --- entradas: se guardan 0.4 s para que no se pierdan si se pulsan un instante antes de poder usarse
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.F))
        {
            pendiente = Pendiente.Golpe;
            pendienteHasta = Time.time + 0.4f;
        }
        if (Input.GetKeyDown(teclaAtaqueArea) && tiempoProximoAtaqueArea - Time.time < 0.4f)
        {
            pendiente = Pendiente.Onda;
            pendienteHasta = Time.time + 0.4f;
        }

        if (pendiente == Pendiente.Nada) return;
        if (Time.time > pendienteHasta) { pendiente = Pendiente.Nada; return; }

        // Durante el esquive no se ataca (el esquive ya cortó cualquier ataque); la acción espera a que termine
        if (chofer != null && chofer.EstaEsquivando) return;

        if (atacando)
        {
            // Si el golpe ya conectó y solo está recuperándose, la siguiente acción lo cancela: se siente ágil.
            // (RutinaGolpe sale sola de la recuperación al ver 'pendiente'; la onda corta el golpe de inmediato.)
            if (pendiente == Pendiente.Onda && enRecuperacion && Time.time >= tiempoProximoAtaqueArea)
            {
                CancelarAcciones();
                pendiente = Pendiente.Onda;   // CancelarAcciones borra el buffer; la onda sí se ejecuta
            }
            else return;
        }

        if (pendiente == Pendiente.Golpe)
        {
            pendiente = Pendiente.Nada;
            IniciarGolpe();
        }
        else if (pendiente == Pendiente.Onda)
        {
            if (Time.time >= tiempoProximoAtaqueArea)
            {
                pendiente = Pendiente.Nada;
                StartCoroutine(RutinaAtaqueArea());
            }
        }
    }

    /// <summary>Corta golpes y onda en curso (lo usa el esquive) y deja al personaje libre al instante.</summary>
    public void CancelarAcciones()
    {
        StopAllCoroutines();
        atacando = false;
        enRecuperacion = false;
        pendiente = Pendiente.Nada;
        poseRot = Vector3.zero;
        poseAdelante = 0f;
        golpeados.Clear();
        if (estela != null) estela.emitting = false;
        if (anilloArea != null) anilloArea.gameObject.SetActive(false);
        if (chofer != null) chofer.multiplicadorVelocidad = 1f;
    }

    void IniciarGolpe()
    {
        int siguiente = comboPaso + 1;
        if (siguiente > 3 || Time.time - tiempoUltimoGolpe > tiempoVentanaCombo) siguiente = 1;
        comboPaso = siguiente;
        StartCoroutine(RutinaGolpe(comboPaso));
    }

    // ============================================================ GOLPE MELÉ

    IEnumerator RutinaGolpe(int paso)
    {
        atacando = true;
        enRecuperacion = false;
        bool finalCombo = paso == 3;
        float dur = finalCombo ? duracionGolpeFinal : duracionGolpe;
        float danio = paso == 1 ? danioAtaque1 : (paso == 2 ? danioAtaque2 : danioAtaque3);

        DispararTrigger(paso == 1 ? "Atacar" : (paso == 2 ? "Atacar2" : "Atacar3"));
        Reproducir(clipSwoosh, finalCombo ? 0.9f : 1.0f, finalCombo ? 0.85f : 1f + 0.1f * paso);
        if (chofer != null) chofer.multiplicadorVelocidad = velocidadMientrasAtaca;

        // Orientar el golpe hacia el enemigo más cercano al frente (asistencia de puntería suave)
        OrientarAlObjetivoCercano();

        float t = 0f;
        bool aplicado = false;
        if (estela != null) { estela.Clear(); }

        while (t < dur)
        {
            if (Time.time < congelarHasta) { yield return null; continue; } // hit-freeze

            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);

            CalcularPose(paso, k);
            MoverEstela(paso, k);

            // Impulso de avance durante la fase de golpe
            if (controller != null && k > 0.28f && k < 0.58f)
            {
                controller.Move(transform.forward * impulsoAdelante * (finalCombo ? 1.3f : 1f) * Time.deltaTime);
            }

            if (!aplicado && k >= 0.46f)
            {
                aplicado = true;
                AplicarGolpe(danio, finalCombo);
            }

            // Ya conectó: desde aquí solo es recuperación. Si hay otra acción esperando, se corta y se encadena.
            if (aplicado && k >= 0.62f)
            {
                enRecuperacion = true;
                if (pendiente != Pendiente.Nada && Time.time <= pendienteHasta) break;
            }
            yield return null;
        }

        if (estela != null) estela.emitting = false;
        if (chofer != null) chofer.multiplicadorVelocidad = 1f;
        tiempoUltimoGolpe = Time.time;

        // Si hay una acción en espera no se hace la pausa extra: se encadena de inmediato
        if (pendiente == Pendiente.Nada) yield return new WaitForSeconds(cooldownEntreGolpes);
        enRecuperacion = false;
        atacando = false;
    }

    void CalcularPose(int paso, float k)
    {
        Vector3 viento, golpe;
        float avance;
        switch (paso)
        {
            case 1: viento = new Vector3(-6f, -38f, 8f); golpe = new Vector3(14f, 48f, -10f); avance = 0.22f; break;
            case 2: viento = new Vector3(-6f, 38f, -8f); golpe = new Vector3(14f, -48f, 10f); avance = 0.22f; break;
            case 3: viento = new Vector3(-30f, 0f, 0f); golpe = new Vector3(40f, 0f, 0f); avance = 0.40f; break;
            default: viento = new Vector3(-28f, 0f, 0f); golpe = new Vector3(32f, 0f, 0f); avance = 0.15f; break; // onda de área
        }

        float w = Mathf.SmoothStep(0f, 1f, k / 0.30f);            // preparación
        float s = Mathf.SmoothStep(0f, 1f, (k - 0.30f) / 0.20f);  // golpe (rápido)
        float r = Mathf.SmoothStep(0f, 1f, (k - 0.58f) / 0.42f);  // recuperación

        Vector3 p = Vector3.Lerp(Vector3.zero, viento, w);
        p = Vector3.Lerp(p, golpe, s);
        p *= (1f - r);
        poseRot = p;
        poseAdelante = avance * s * (1f - r);

        // Si el Animator ya tiene animaciones de ataque reales, no se superpone la pose procedural
        if (!usarPoseProcedural) { poseRot = Vector3.zero; poseAdelante = 0f; }
    }

    void MoverEstela(int paso, float k)
    {
        if (puntaEstela == null || estela == null) return;

        float u = Mathf.SmoothStep(0f, 1f, (k - 0.26f) / 0.30f);
        bool activa = k > 0.26f && k < 0.60f;
        if (activa && !estela.emitting) { estela.Clear(); estela.emitting = true; }
        if (!activa) estela.emitting = false;

        const float R = 1.7f;
        Vector3 pos;
        if (paso == 1)
        {
            float a = Mathf.Lerp(80f, -80f, u) * Mathf.Deg2Rad;
            pos = new Vector3(Mathf.Sin(a) * R, 1.1f, Mathf.Cos(a) * R * 0.9f);
        }
        else if (paso == 2)
        {
            float a = Mathf.Lerp(-80f, 80f, u) * Mathf.Deg2Rad;
            pos = new Vector3(Mathf.Sin(a) * R, 0.8f, Mathf.Cos(a) * R * 0.9f);
        }
        else
        {
            pos = new Vector3(0f, Mathf.Lerp(2.4f, 0.2f, u), Mathf.Lerp(0.5f, 1.9f, u));
        }
        puntaEstela.localPosition = pos;
    }

    void OrientarAlObjetivoCercano()
    {
        PoderEspiritual mejor = null;
        float mejorDist = 3.2f;
        foreach (Collider c in Physics.OverlapSphere(transform.position, 3.2f, ~0, QueryTriggerInteraction.Ignore))
        {
            PoderEspiritual pe = c.GetComponentInParent<PoderEspiritual>();
            if (pe == null || pe.gameObject == gameObject || pe.EstaMuerto || !pe.CompareTag("Enemigo")) continue;
            Vector3 d = pe.transform.position - transform.position; d.y = 0f;
            float dist = d.magnitude;
            if (dist < mejorDist && dist > 0.05f && Vector3.Angle(transform.forward, d) < 80f)
            {
                mejorDist = dist; mejor = pe;
            }
        }
        if (mejor != null)
        {
            Vector3 d = mejor.transform.position - transform.position; d.y = 0f;
            transform.rotation = Quaternion.LookRotation(d.normalized);
        }
    }

    void AplicarGolpe(float danio, bool finalCombo)
    {
        golpeados.Clear();
        Vector3 origen = transform.position + Vector3.up * 0.9f;
        float radio = alcanceGolpe + (finalCombo ? 0.5f : 0f);
        bool algunImpacto = false;

        foreach (Collider c in Physics.OverlapSphere(origen + transform.forward * (radio * 0.35f), radio, ~0, QueryTriggerInteraction.Ignore))
        {
            PoderEspiritual pe = c.GetComponentInParent<PoderEspiritual>();
            if (pe == null || pe.gameObject == gameObject || pe.EstaMuerto || !pe.CompareTag("Enemigo")) continue;
            if (!golpeados.Add(pe)) continue;

            Vector3 hacia = pe.transform.position - transform.position; hacia.y = 0f;
            if (hacia.sqrMagnitude > 0.01f && Vector3.Angle(transform.forward, hacia) > anguloGolpe * 0.5f) { golpeados.Remove(pe); continue; }

            pe.RecibirDanio(danio);
            algunImpacto = true;

            EsbirroBaseIA ia = pe.GetComponent<EsbirroBaseIA>();
            if (ia != null) ia.AplicarEmpuje(hacia.sqrMagnitude > 0.001f ? hacia.normalized : transform.forward, finalCombo ? 9f : 5f);

            EmitirChispas(c.ClosestPoint(origen) + Vector3.up * 0.2f, finalCombo ? 26 : 14);
            Debug.Log("[Chofer] Golpe a " + pe.name + " (" + danio + " de daño espiritual)");
        }

        if (algunImpacto)
        {
            Reproducir(clipImpacto, finalCombo ? 1f : 0.8f, finalCombo ? 0.8f : 1f);
            congelarHasta = Time.time + (finalCombo ? 0.09f : 0.05f);
            golpeFov = finalCombo ? 5f : 2.5f;
        }
    }

    // ============================================================ ONDA ESPIRITUAL (ÁREA)

    IEnumerator RutinaAtaqueArea()
    {
        atacando = true;
        tiempoProximoAtaqueArea = Time.time + cooldownArea;
        DispararTrigger("AtaqueArea");
        Reproducir(clipArea, 1f, 1f);
        if (chofer != null) chofer.multiplicadorVelocidad = 0.2f;

        float dur = 0.5f, t = 0f;
        bool aplicado = false;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            CalcularPose(4, k);

            if (!aplicado && k >= 0.45f)
            {
                aplicado = true;
                golpeFov = 6f;
                StartCoroutine(AnimarAnilloArea());

                foreach (Collider c in Physics.OverlapSphere(transform.position, radioArea, ~0, QueryTriggerInteraction.Ignore))
                {
                    PoderEspiritual pe = c.GetComponentInParent<PoderEspiritual>();
                    if (pe == null || pe.gameObject == gameObject || pe.EstaMuerto || !pe.CompareTag("Enemigo")) continue;
                    if (!golpeados.Add(pe)) continue;

                    pe.RecibirDanio(danioArea);
                    Vector3 fuera = pe.transform.position - transform.position; fuera.y = 0f;
                    EsbirroBaseIA ia = pe.GetComponent<EsbirroBaseIA>();
                    if (ia != null) ia.AplicarEmpuje(fuera.sqrMagnitude > 0.001f ? fuera.normalized : transform.forward, 11f);
                    EmitirChispas(pe.transform.position + Vector3.up, 18);
                }
                golpeados.Clear();
                Debug.Log("[Chofer] ¡Onda de Energía Espiritual liberada en área!");
            }
            yield return null;
        }

        if (chofer != null) chofer.multiplicadorVelocidad = 1f;
        yield return new WaitForSeconds(0.1f);
        atacando = false;
    }

    IEnumerator AnimarAnilloArea()
    {
        if (anilloArea == null) yield break;
        anilloArea.gameObject.SetActive(true);
        float dur = 0.45f, t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = t / dur;
            float radio = Mathf.Lerp(0.5f, radioArea, 1f - (1f - k) * (1f - k));
            anilloArea.transform.localScale = new Vector3(radio, 1f, radio);
            anilloArea.widthMultiplier = Mathf.Lerp(0.45f, 0.06f, k);
            Color c = colorEspiritual; c.a = 1f - k;
            anilloArea.startColor = c; anilloArea.endColor = c;
            yield return null;
        }
        anilloArea.gameObject.SetActive(false);
    }

    // ============================================================ ANIMACIÓN PROCEDURAL / CÁMARA

    void LateUpdate()
    {
        if (modelo != null)
        {
            if (!modeloInicializado)
            {
                modeloPosBase = modelo.localPosition;
                modeloRotBase = modelo.localRotation;
                modeloInicializado = true;
            }

            // Relajar la pose cuando no se ataca
            if (!atacando)
            {
                poseRot = Vector3.Lerp(poseRot, Vector3.zero, 1f - Mathf.Exp(-14f * Time.deltaTime));
                poseAdelante = Mathf.Lerp(poseAdelante, 0f, 1f - Mathf.Exp(-14f * Time.deltaTime));
            }

            Quaternion rot = modeloRotBase * Quaternion.Euler(poseRot);
            Vector3 pos = modeloPosBase + Vector3.forward * poseAdelante;

            // Rodada del esquive (procedural, cuando no hay clip de esquive): gira el cuerpo alrededor de un punto bajo
            if (chofer != null && chofer.rodando)
            {
                Vector3 c = chofer.centroRodada;
                pos = c + chofer.rotacionRodada * (pos - c);
                rot = chofer.rotacionRodada * rot;
            }

            modelo.localRotation = rot;
            modelo.localPosition = pos;
        }

        // Golpe de FOV (se amortigua solo)
        if (camara != null)
        {
            if (golpeFov > 0.01f)
            {
                golpeFov = Mathf.MoveTowards(golpeFov, 0f, 22f * Time.deltaTime);
                camara.fovExtra = golpeFov;
                golpeFovActivo = true;
            }
            else if (golpeFovActivo)
            {
                camara.fovExtra = 0f;
                golpeFovActivo = false;
            }
        }
    }

    void DispararTrigger(string nombre)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == nombre)
            {
                animator.SetTrigger(nombre);
                return;
            }
        }
    }

    // ============================================================ EFECTOS (estela, chispas, anillo, sonido)

    void CrearEfectos()
    {
        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh != null) { matSprite = new Material(sh); matSprite.color = Color.white; }

        // Estela del golpe
        GameObject punta = new GameObject("PuntaEstelaGolpe");
        punta.transform.SetParent(transform, false);
        puntaEstela = punta.transform;
        if (matSprite != null)
        {
            estela = punta.AddComponent<TrailRenderer>();
            estela.sharedMaterial = matSprite;
            estela.time = 0.16f;
            estela.minVertexDistance = 0.04f;
            estela.emitting = false;
            estela.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            estela.widthCurve = new AnimationCurve(new Keyframe(0f, 0.55f), new Keyframe(1f, 0f));
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(colorEspiritual, 0.4f), new GradientColorKey(colorEspiritual, 1f) },
                new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0.6f, 0.5f), new GradientAlphaKey(0f, 1f) });
            estela.colorGradient = g;
        }

        // Chispas de impacto
        GameObject goChispas = new GameObject("ChispasGolpe");
        goChispas.transform.SetParent(transform, false);
        chispas = goChispas.AddComponent<ParticleSystem>();
        chispas.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = chispas.main;
        main.loop = false; main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 9f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white, colorEspiritual);
        main.gravityModifier = 0.6f;
        main.maxParticles = 120;
        var em = chispas.emission; em.rateOverTime = 0f;
        var shp = chispas.shape; shp.shapeType = ParticleSystemShapeType.Sphere; shp.radius = 0.15f;
        var rend = chispas.GetComponent<ParticleSystemRenderer>();
        if (matSprite != null) rend.sharedMaterial = matSprite;
        rend.renderMode = ParticleSystemRenderMode.Stretch;
        rend.velocityScale = 0.05f; rend.lengthScale = 1.5f;

        // Anillo de la onda espiritual
        if (matSprite != null)
        {
            GameObject goAnillo = new GameObject("AnilloOndaEspiritual");
            goAnillo.transform.SetParent(transform, false);
            goAnillo.transform.localPosition = new Vector3(0f, -0.9f, 0f);
            anilloArea = goAnillo.AddComponent<LineRenderer>();
            anilloArea.useWorldSpace = false;
            anilloArea.loop = true;
            anilloArea.sharedMaterial = matSprite;
            anilloArea.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            const int n = 56;
            anilloArea.positionCount = n;
            for (int i = 0; i < n; i++)
            {
                float a = (i / (float)n) * Mathf.PI * 2f;
                anilloArea.SetPosition(i, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)));
            }
            goAnillo.SetActive(false);
        }

        // Sonido procedural
        audioFx = gameObject.AddComponent<AudioSource>();
        audioFx.playOnAwake = false;
        audioFx.spatialBlend = 0f;
        clipSwoosh = GenerarSwoosh();
        clipImpacto = GenerarImpacto();
        clipArea = GenerarOnda();
    }

    void EmitirChispas(Vector3 pos, int cantidad)
    {
        if (chispas == null) return;
        chispas.transform.position = pos;
        chispas.Play();
        chispas.Emit(cantidad);
    }

    void Reproducir(AudioClip clip, float volumen, float pitch)
    {
        if (audioFx == null || clip == null) return;
        audioFx.pitch = pitch;
        audioFx.PlayOneShot(clip, volumen * volumenSfx * OpcionesJuego.Efectos);
    }

    AudioClip GenerarSwoosh()
    {
        int rate = 22050, n = (int)(rate * 0.28f);
        float[] d = new float[n];
        System.Random rng = new System.Random(7);
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n;
            float ruido = (float)(rng.NextDouble() * 2.0 - 1.0);
            float coef = Mathf.Lerp(0.08f, 0.55f, t);          // el filtro se abre: efecto "swish"
            lp += (ruido - lp) * coef;
            float env = Mathf.Sin(t * Mathf.PI);
            d[i] = lp * env * 0.9f;
        }
        AudioClip c = AudioClip.Create("Swoosh", n, 1, rate, false);
        c.SetData(d, 0);
        return c;
    }

    AudioClip GenerarImpacto()
    {
        int rate = 22050, n = (int)(rate * 0.22f);
        float[] d = new float[n];
        System.Random rng = new System.Random(11);
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate;
            float tono = Mathf.Sin(2f * Mathf.PI * (140f - t * 280f) * t) * Mathf.Exp(-t * 22f);
            float ruido = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-t * 45f) * 0.6f;
            d[i] = (tono + ruido) * 0.9f;
        }
        AudioClip c = AudioClip.Create("ImpactoGolpe", n, 1, rate, false);
        c.SetData(d, 0);
        return c;
    }

    AudioClip GenerarOnda()
    {
        int rate = 22050, n = (int)(rate * 0.8f);
        float[] d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate;
            float k = t / 0.8f;
            float freq = Mathf.Lerp(180f, 620f, k * k);
            float env = Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI) * (1f - k * 0.4f);
            float boom = Mathf.Sin(2f * Mathf.PI * 60f * t) * Mathf.Exp(-Mathf.Abs(t - 0.38f) * 12f);
            d[i] = (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.35f * env + boom * 0.6f);
        }
        AudioClip c = AudioClip.Create("OndaEspiritual", n, 1, rate, false);
        c.SetData(d, 0);
        return c;
    }

    void OnDisable()
    {
        if (chofer != null) chofer.multiplicadorVelocidad = 1f;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, radioArea);
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.5f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.9f + transform.forward * (alcanceGolpe * 0.35f), alcanceGolpe);
    }
}
