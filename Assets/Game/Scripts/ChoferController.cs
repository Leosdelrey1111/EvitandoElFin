using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class ChoferController : MonoBehaviour
{
    [Header("Velocidades")]
    public float velocidadCaminar = 4.5f;
    public float velocidadCorrer = 7.0f;
    public float gravedad = -15f;
    public float velocidadRotacion = 14f;

    public enum ModoControl { RelativoACamara, LibreMundo }
    [Header("Modo de Movimiento")]
    [Tooltip("RelativoACamara: El Chofer avanza hacia donde apunta la cámara con el mouse (natural de acción/RPG). LibreMundo: Ejes globales X/Z.")]
    public ModoControl modoControl = ModoControl.RelativoACamara;

    [Header("Salto")]
    public KeyCode teclaSalto = KeyCode.Space;
    [Tooltip("Altura del salto en metros")]
    public float alturaSalto = 1.25f;

    [Header("Esquive (Dodge Roll)")]
    public KeyCode teclaEsquive = KeyCode.LeftControl;
    public KeyCode teclaEsquiveAlterna = KeyCode.C;
    [Tooltip("Obsoleto (se conserva por compatibilidad con la escena). Ahora se usa 'distanciaEsquive' y 'tiempoEsquive'.")]
    public float fuerzaEsquive = 10f;
    [Tooltip("Obsoleto: ver 'tiempoEsquive'.")]
    public float duracionEsquive = 0.35f;
    [Tooltip("Obsoleto: ver 'recargaEsquive'.")]
    public float cooldownEsquive = 0.8f;
    [Tooltip("Metros que recorre el esquive (arranca rápido y frena suave).")]
    public float distanciaEsquive = 5.4f;
    [Tooltip("Duración del esquive (segundos). Durante todo este tiempo hay i-frames.")]
    public float tiempoEsquive = 0.48f;
    [Tooltip("Tiempo de recarga desde que termina el esquive.")]
    public float recargaEsquive = 0.55f;
    [Tooltip("Permite esquivar también en el aire (sale de un salto con un impulso).")]
    public bool permitirEsquiveEnAire = true;
    private bool puedeEsquivar = true;
    private bool estaEsquivando = false;

    // Rodada procedural (cuando el Animator no tiene un estado "Esquivar"): la aplica SistemaCombateChofer en su LateUpdate
    [System.NonSerialized] public bool rodando;
    [System.NonSerialized] public Quaternion rotacionRodada = Quaternion.identity;
    [System.NonSerialized] public Vector3 centroRodada = new Vector3(0f, -0.45f, 0f);

    // Entradas con "buffer": si se pulsa un poco antes de poder usarse, se ejecuta en cuanto se puede
    private float bufferSaltoHasta = -1f;
    private float bufferEsquiveHasta = -1f;
    private float ultimoTiempoSuelo = -10f;

    [Header("Estado Aturdido (Despertar del alma)")]
    [Tooltip("Si es true, el Chofer no responde al input (solo gravedad).")]
    public bool controlBloqueado = false;
    [Tooltip("0 = normal, 1 = muy confundido: camina más lento y se desvía como mareado.")]
    [Range(0f, 1f)]
    public float confusion = 0f;

    [Header("Animación")]
    public Animator animator;

    [Header("Combate")]
    [Tooltip("Multiplicador de velocidad de movimiento (el sistema de combate lo reduce mientras golpea).")]
    public float multiplicadorVelocidad = 1f;

    private CharacterController controller;
    private PoderEspiritual poder;
    private Vector3 velocidadVertical;
    private Transform camaraTransform;
    private SistemaCombateChofer combate;

    public bool EstaEsquivando { get { return estaEsquivando; } }
    /// <summary>
    /// ¿Está en el suelo? Usa el valor medido justo después de aplicar la gravedad (con 'isGrounded' directo daba falso
    /// mientras caminaba, porque el último Move era horizontal) y tolera 0.12 s tras dejar el borde ("coyote time").
    /// </summary>
    public bool EstaEnSuelo { get { return controller != null && Time.time - ultimoTiempoSuelo < 0.12f; } }
    private float inicioCooldownEsquive = -100f;
    /// <summary>0 = recién esquivado, 1 = listo para esquivar otra vez (para el HUD).</summary>
    public float ProgresoEsquive
    {
        get
        {
            if (estaEsquivando) return 0f;
            if (puedeEsquivar) return 1f;
            return Mathf.Clamp01((Time.time - inicioCooldownEsquive) / Mathf.Max(0.01f, recargaEsquive));
        }
    }

    private readonly System.Collections.Generic.HashSet<string> parametrosAnimator = new System.Collections.Generic.HashSet<string>();

    void Awake()
    {
        CentrarModeloVisual();
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        poder = GetComponent<PoderEspiritual>();
        combate = GetComponent<SistemaCombateChofer>();
        if (controller != null)
        {
            controller.center = Vector3.zero;
            controller.height = 2f;
            controller.radius = 0.5f;
        }

        CentrarModeloVisual();

        if (animator == null)
        {
            Animator[] anims = GetComponentsInChildren<Animator>();
            foreach (var a in anims)
            {
                if (a.gameObject != this.gameObject && a.runtimeAnimatorController != null)
                {
                    animator = a;
                    break;
                }
            }
            if (animator == null && anims.Length > 0)
            {
                animator = anims[anims.Length - 1];
            }
        }

        if (Camera.main != null)
        {
            camaraTransform = Camera.main.transform;
            CamaraTerceraPersona cam3ra = Camera.main.GetComponent<CamaraTerceraPersona>();
            if (cam3ra == null)
            {
                cam3ra = Camera.main.gameObject.AddComponent<CamaraTerceraPersona>();
            }
            cam3ra.objetivo = transform;
        }

        // Parámetros disponibles en el Animator (así no se lanzan avisos si falta alguna animación)
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (AnimatorControllerParameter p in animator.parameters) parametrosAnimator.Add(p.name);
        }

        if (poder != null)
        {
            poder.OnRecibioDanio += AlRecibirDanio;
            poder.OnMuerte += AlMorir;
        }
    }

    void OnDestroy()
    {
        if (poder != null)
        {
            poder.OnRecibioDanio -= AlRecibirDanio;
            poder.OnMuerte -= AlMorir;
        }
    }

    void AlRecibirDanio(float cantidad)
    {
        // El golpe mortal no dispara "Golpeado": competiría con la animación de muerte
        if (poder != null && poder.EstaMuerto) return;
        DispararTrigger("Golpeado");
    }

    void AlMorir()
    {
        controlBloqueado = true;
        if (animator == null) return;

        // Se cancelan los triggers pendientes (golpes, esquive...) para que nada interrumpa la caída
        foreach (AnimatorControllerParameter ap in animator.parameters)
            if (ap.type == AnimatorControllerParameterType.Trigger) animator.ResetTrigger(ap.name);

        animator.speed = 1f;
        // Se entra directo al estado de muerte (sin depender de transiciones); si no existe, se usa el trigger
        if (!ReproducirEstado("Muerte", 0.12f)) DispararTrigger("Muerte");
    }

    /// <summary>Reproduce un estado del Animator por nombre. Devuelve false si el estado no existe.</summary>
    public bool ReproducirEstado(string nombreEstado, float fundido)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        int hash = Animator.StringToHash(nombreEstado);
        if (!animator.HasState(0, hash)) return false;
        if (fundido > 0f) animator.CrossFadeInFixedTime(hash, fundido, 0, 0f);
        else animator.Play(hash, 0, 0f);
        return true;
    }

    /// <summary>Dispara un trigger del Animator solo si existe en el controlador.</summary>
    public void DispararTrigger(string nombre)
    {
        if (animator != null && parametrosAnimator.Contains(nombre)) animator.SetTrigger(nombre);
    }

    /// <summary>
    /// Elimina cualquier desfase en X o Z del modelo hijo para que rote sobre su propio cuerpo
    /// y no gire orbitando como satélite alrededor de un eje central.
    /// </summary>
    public void CentrarModeloVisual()
    {
        foreach (Transform hijo in transform)
        {
            if (hijo != transform && (hijo.GetComponentInChildren<Animator>() != null || hijo.name.ToLower().Contains("chofer")))
            {
                hijo.localPosition = new Vector3(0f, -1f, 0f);
                hijo.localRotation = Quaternion.identity;
            }
        }
    }

    void Update()
    {
        if (estaEsquivando)
        {
            // Durante el rodamiento no se mueve, pero las pulsaciones se guardan para ejecutarlas al terminar
            if (!controlBloqueado && Time.timeScale > 0.01f && Input.GetKeyDown(teclaSalto)) bufferSaltoHasta = Time.time + 0.25f;
            return;
        }

        AplicarGravedad();

        if (controlBloqueado)
        {
            if (animator != null) animator.SetFloat("Velocidad", 0f, 0.15f, Time.deltaTime);
            return;
        }

        ProcesarMovimiento();
        ProcesarSalto();
        ProcesarEsquive();

        if (animator != null && parametrosAnimator.Contains("EnSuelo")) animator.SetBool("EnSuelo", EstaEnSuelo);
    }

    void ProcesarSalto()
    {
        if (Time.timeScale <= 0.01f) return;
        if (Input.GetKeyDown(teclaSalto)) bufferSaltoHasta = Time.time + 0.2f;

        if (Time.time < bufferSaltoHasta && EstaEnSuelo)
        {
            bufferSaltoHasta = -1f;
            ultimoTiempoSuelo = -10f; // evita un segundo salto con el "coyote time"
            velocidadVertical.y = Mathf.Sqrt(alturaSalto * -2f * gravedad);
            DispararTrigger("Saltar");
        }
    }

    void ProcesarMovimiento()
    {
        // Lectura de entrada con soporte directo para WASD y flechas
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h = -1f;
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h = 1f;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v = 1f;
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v = -1f;

        Vector3 direccionEntrada = new Vector3(h, 0f, v);
        if (direccionEntrada.sqrMagnitude > 1f)
        {
            direccionEntrada.Normalize();
        }

        float objetivoVelocidad = 0f;

        if (direccionEntrada.sqrMagnitude >= 0.01f)
        {
            Vector3 direccionMovimiento;

            if (modoControl == ModoControl.LibreMundo)
            {
                // Movimiento libre en los ejes del mundo (W = +Z, S = -Z, A = -X, D = +X)
                direccionMovimiento = direccionEntrada;
            }
            else
            {
                // Moverse relativo hacia donde apunta la cámara
                Transform cam = camaraTransform != null ? camaraTransform : (Camera.main != null ? Camera.main.transform : null);
                if (cam != null)
                {
                    Vector3 forwardCam = cam.forward;
                    Vector3 rightCam = cam.right;
                    forwardCam.y = 0;
                    rightCam.y = 0;

                    if (forwardCam.sqrMagnitude < 0.001f) forwardCam = Vector3.forward;
                    if (rightCam.sqrMagnitude < 0.001f) rightCam = Vector3.right;

                    forwardCam.Normalize();
                    rightCam.Normalize();

                    direccionMovimiento = (forwardCam * v + rightCam * h).normalized;
                }
                else
                {
                    direccionMovimiento = direccionEntrada;
                }
            }

            if (direccionMovimiento.sqrMagnitude >= 0.001f)
            {
                // Aturdimiento: la dirección se desvía con una oscilación lenta (mareo)
                if (confusion > 0.01f)
                {
                    float desvio = Mathf.Sin(Time.time * 2.3f) * 38f * confusion;
                    direccionMovimiento = Quaternion.Euler(0f, desvio, 0f) * direccionMovimiento;
                }

                // Rotar suavemente hacia la dirección de movimiento
                Quaternion rotacionObjetivo = Quaternion.LookRotation(direccionMovimiento);
                transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, velocidadRotacion * Time.deltaTime);

                // Determinar si corre o camina (no se puede correr aturdido)
                bool corriendo = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && confusion < 0.3f;
                float velocidadActual = corriendo ? velocidadCorrer : velocidadCaminar;
                velocidadActual *= Mathf.Lerp(1f, 0.45f, confusion) * multiplicadorVelocidad;

                controller.Move(direccionMovimiento * velocidadActual * Time.deltaTime);
                objetivoVelocidad = corriendo ? 2f : 1f;
            }
        }

        if (animator != null)
        {
            // Amortiguación suave para transiciones de animación
            animator.SetFloat("Velocidad", objetivoVelocidad, 0.15f, Time.deltaTime);
        }
    }

    void AplicarGravedad()
    {
        if (controller.isGrounded && velocidadVertical.y < 0)
        {
            velocidadVertical.y = -2f;
        }

        velocidadVertical.y += gravedad * Time.deltaTime;
        controller.Move(velocidadVertical * Time.deltaTime);

        // Este Move tiene componente vertical, así que 'isGrounded' aquí sí es fiable
        if (controller.isGrounded) ultimoTiempoSuelo = Time.time;
    }

    void ProcesarEsquive()
    {
        if (Time.timeScale <= 0.01f) return;
        if (Input.GetKeyDown(teclaEsquive) || Input.GetKeyDown(teclaEsquiveAlterna)) bufferEsquiveHasta = Time.time + 0.25f;

        if (Time.time < bufferEsquiveHasta && puedeEsquivar && (permitirEsquiveEnAire || EstaEnSuelo))
        {
            bufferEsquiveHasta = -1f;
            StartCoroutine(RutinaEsquive());
        }
    }

    /// <summary>Dirección de la tecla de movimiento pulsada, relativa a la cámara (cero si no hay input).</summary>
    Vector3 DireccionDeEntrada()
    {
        float h = 0f, v = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v -= 1f;
        if (h == 0f && v == 0f) return Vector3.zero;

        Vector3 entrada = new Vector3(h, 0f, v).normalized;
        if (modoControl == ModoControl.LibreMundo) return entrada;

        Transform cam = camaraTransform != null ? camaraTransform : (Camera.main != null ? Camera.main.transform : null);
        if (cam == null) return entrada;
        Vector3 f = cam.forward; f.y = 0f;
        Vector3 r = cam.right; r.y = 0f;
        if (f.sqrMagnitude < 0.001f || r.sqrMagnitude < 0.001f) return entrada;
        return (f.normalized * v + r.normalized * h).normalized;
    }

    bool AnimatorTieneEstado(string nombre)
    {
        return animator != null && animator.runtimeAnimatorController != null && animator.HasState(0, Animator.StringToHash(nombre));
    }

    IEnumerator RutinaEsquive()
    {
        puedeEsquivar = false;
        estaEsquivando = true;
        if (poder != null) poder.invulnerable = true; // i-frames durante todo el esquive

        // El esquive corta lo que se esté haciendo (golpes, onda) para responder al instante
        if (combate != null) combate.CancelarAcciones();
        multiplicadorVelocidad = 1f;
        if (animator != null && animator.runtimeAnimatorController != null)
            foreach (AnimatorControllerParameter ap in animator.parameters)
                if (ap.type == AnimatorControllerParameterType.Trigger) animator.ResetTrigger(ap.name);

        // Hacia donde se pulsa; si no se pulsa nada, hacia donde mira
        Vector3 dir = DireccionDeEntrada();
        if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
        dir.y = 0f; dir.Normalize();
        transform.rotation = Quaternion.LookRotation(dir);

        bool animReal = AnimatorTieneEstado("Esquivar");
        if (animReal) DispararTrigger("Esquivar");
        else
        {
            // Sin clip de esquive: vuelve a locomoción (corta animaciones de golpe) y se hace una rodada procedural
            ReproducirEstado("Locomocion", 0.05f);
            if (animator != null) animator.SetFloat("Velocidad", 2f);
        }
        rodando = !animReal;

        float dur = Mathf.Max(0.1f, tiempoEsquive);
        float t = 0f;
        while (t < dur)
        {
            if (poder != null && poder.EstaMuerto) break; // si muere rodando, manda la animación de muerte
            float dt = Time.deltaTime;
            t += dt;
            float k = Mathf.Clamp01(t / dur);

            // Perfil de velocidad: arranca rápido y frena suave. Integral = distanciaEsquive.
            float v = (distanciaEsquive / dur) * (1.6f - 1.2f * k);

            if (controller.isGrounded && velocidadVertical.y < 0f) velocidadVertical.y = -2f;
            velocidadVertical.y += gravedad * dt;
            controller.Move((dir * v + Vector3.up * velocidadVertical.y) * dt);
            if (controller.isGrounded) ultimoTiempoSuelo = Time.time;

            if (rodando)
            {
                float ang = 360f * Mathf.SmoothStep(0f, 1f, (k - 0.04f) / 0.92f);
                rotacionRodada = Quaternion.AngleAxis(ang, Vector3.right);
            }
            yield return null;
        }

        rodando = false;
        rotacionRodada = Quaternion.identity;
        RestaurarModelo();

        estaEsquivando = false;
        inicioCooldownEsquive = Time.time;
        // Pequeño margen extra de invulnerabilidad al terminar el rodamiento
        yield return new WaitForSeconds(0.1f);
        if (poder != null) poder.invulnerable = false;
        yield return new WaitForSeconds(Mathf.Max(0f, recargaEsquive - 0.1f));
        puedeEsquivar = true;
    }

    // ---- rodada procedural: si SistemaCombateChofer no está, la aplica este script

    void LateUpdate()
    {
        if (!rodando || combate != null) return;
        Transform m = ModeloVisual();
        if (m == null) return;
        Vector3 c = centroRodada;
        m.localPosition = c + rotacionRodada * (new Vector3(0f, -1f, 0f) - c);
        m.localRotation = rotacionRodada;
    }

    Transform ModeloVisual()
    {
        return animator != null && animator.transform != transform ? animator.transform : null;
    }

    void RestaurarModelo()
    {
        if (combate != null) return; // SistemaCombateChofer vuelve a escribir la pose normal en su LateUpdate
        Transform m = ModeloVisual();
        if (m == null) return;
        m.localPosition = new Vector3(0f, -1f, 0f);
        m.localRotation = Quaternion.identity;
    }
}
