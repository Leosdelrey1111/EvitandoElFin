using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Cinemáticas narrativas del Nivel 1 sincronizadas con el texto y la voz de Tonalli / La Bufa:
///  - Una toma por línea del narrador: el fundido o corte ocurre ANTES de que empiece a sonar la voz (`PrepararTomaSincronizada`),
///    y el reloj de la toma (`tToma` y `duracionTomaActual`) se sincroniza cuadro a cuadro con el audio real (`SincronizarRelojVoz`).
///  - Incluye cinemáticas para: "intro", "sierra" (cerro), "serpientes", "hombres_piedra", "comadres",
///    "corona", "tesoro" (bocamina), "arroyo", "cruces", "comerciante" (Don Chuy), "cueva", "jefe_despierta" y "jefe_derrotado".
///  - Las acciones de los enemigos en pantalla (enroscarse, voltear, golpear y agacharse, chismear y lanzar murmullos)
///    ocurren en el instante exacto en que la voz y el texto lo mencionan.
/// </summary>
[DefaultExecutionOrder(200)]
public class CinematicaNarrativa : MonoBehaviour
{
    public static CinematicaNarrativa Instancia { get; private set; }
    public static bool Activa { get { return Instancia != null && Instancia.enCurso; } }

    public bool habilitada = true;
    public KeyCode teclaSaltar = KeyCode.Return;
    [Tooltip("Si hay un enemigo vivo más cerca que esto, la cinemática se omite (la narración sigue normal).")]
    public float distanciaSeguridad = 24f;

    // ------------------------------------------------------------------ tomas
    struct Vista { public Vector3 pos, mira; public float fov, temblor, roll; }

    class Toma
    {
        public Func<float, Vista> vista;
        public Action alEmpezar;
        public Action<float> cadaCuadro;
        public string rotulo, subrotulo;
        public bool carta;
    }

    class Contexto
    {
        public Vector3 jug, adelante, derecha;
        public Func<float, Vector3> ruta;      // punto del camino a N metros por delante del Chofer (sobre el suelo)
    }

    class Mob
    {
        public EsbirroBaseIA ia; public CharacterController cc; public Transform visual;
        public EsbirroBaseIA.TipoEsbirro tipo; public float fase;
        public bool mover; public Vector3 destino; public float vel;
        public bool animPersonalizada;
    }

    readonly List<Mob> mobs = new List<Mob>();
    readonly List<GameObject> objetosEscena = new List<GameObject>();
    List<Toma> tomas;
    string idActual;
    int tomaIdx = -1, tomaPedida, totalLineas;
    float tToma;
    float duracionTomaActual = 5.5f;
    bool enCurso, iniciando, saliendo, preparado, cortando, tomado;
    bool controlLoBloqueamos;
    float fovOriginal;
    bool camTPEstabaActiva;

    // ------------------------------------------------------------------ referencias
    Transform jugador;
    ChoferController chofer;
    PoderEspiritual poder;
    Camera cam;
    CamaraTerceraPersona camTP;
    GuiaAlebrije guia;

    // ------------------------------------------------------------------ UI
    RectTransform barraSup, barraInf;
    CanvasGroup grupoFundido, grupoRotulo, grupoPista;
    TextMeshProUGUI txtRotulo, txtSub;
    Image lineaRotulo;
    float barraK, fadeAlpha, fadeObj, fadeVel = 3f;
    Coroutine rutinaRotulo;
    AudioSource fuente;
    AudioClip sonidoCarta;

    // ================================================================== CICLO DE VIDA

    void Awake()
    {
        Instancia = this;
        ConstruirUI();
        fuente = gameObject.AddComponent<AudioSource>();
        fuente.spatialBlend = 0f; fuente.playOnAwake = false; fuente.volume = 0.6f;
        sonidoCarta = UIMexicana.Pluck(new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.55f, 0.15f);
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            jugador = p.transform;
            chofer = p.GetComponent<ChoferController>();
            poder = p.GetComponent<PoderEspiritual>();
        }
        guia = GuiaAlebrije.Instancia != null ? GuiaAlebrije.Instancia : UnityEngine.Object.FindAnyObjectByType<GuiaAlebrije>();
        if (guia != null)
        {
            guia.OnLineaIniciada += AlIniciarLinea;
            guia.OnLineaTerminada += AlTerminarLinea;
        }
    }

    void OnDestroy()
    {
        if (guia != null)
        {
            guia.OnLineaIniciada -= AlIniciarLinea;
            guia.OnLineaTerminada -= AlTerminarLinea;
        }
        if (Instancia == this) Instancia = null;
    }

    // ================================================================== SINCRONIZACIÓN CON VOZ Y TEXTO

    /// <summary>
    /// Llamado por GuiaAlebrije ANTES de reproducir la voz y escribir el texto de una línea.
    /// Realiza el fundido de entrada o el corte de cámara previamente para que el segundo 0.0 de la toma
    /// coincida exactamente con el segundo 0.0 de la voz y del texto.
    /// </summary>
    public IEnumerator PrepararTomaSincronizada(string id, int i, float duracionLinea)
    {
        if (!habilitada || string.IsNullOrEmpty(id)) yield break;

        duracionTomaActual = Mathf.Max(1.8f, duracionLinea);
        int extra = Extra(id);
        int idxToma = i + extra;

        // Si ya estamos en otra cinemática distinta, cerrarla primero
        if (enCurso && id != idActual)
        {
            enCurso = false;
            yield return Salida();
        }

        // Si no ha arrancado esta cinemática y es su primera toma, iniciarla ahora antes de que suene la voz
        if (!enCurso)
        {
            if (idxToma != 0 || !TieneGuion(id)) yield break;
            yield return IniciarSincronizado(id, idxToma);
            yield break;
        }

        // Si ya está en curso y cambia de línea, hacer el corte de cámara antes de soltar la nueva línea de voz
        if (enCurso && preparado && tomas != null)
        {
            tomaPedida = Mathf.Clamp(idxToma, 0, tomas.Count - 1);
            if (tomaPedida != tomaIdx)
                yield return CorteSincronizado();
            else
                tToma = 0f;
        }
    }

    /// <summary>
    /// Sincroniza el reloj de la toma cinemática con el tiempo real de reproducción del AudioClip de voz.
    /// </summary>
    public void SincronizarRelojVoz(float tiempoVoz, float duracionClip)
    {
        if (!enCurso || !preparado) return;
        if (duracionClip > 0.5f)
            duracionTomaActual = Mathf.Max(duracionTomaActual, duracionClip + 0.25f);
        // Mantener tToma alineado al reloj del audio para evitar deriva por frame drops
        if (tiempoVoz > 0.01f)
            tToma = Mathf.Lerp(tToma, tiempoVoz, 0.5f);
    }

    /// <summary>Progreso suavizado (0..1) sincronizado con la duración exacta de la línea de voz actual.</summary>
    float Progreso(float t)
    {
        return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / Mathf.Max(1.5f, duracionTomaActual)));
    }

    /// <summary>Fracción lineal (0..1) de la línea de voz actual.</summary>
    float Frac(float t)
    {
        return Mathf.Clamp01(t / Mathf.Max(1.5f, duracionTomaActual));
    }

    void AlIniciarLinea(string id, int i)
    {
        if (!habilitada) return;
        // Respaldo por si algún evento externo dispara OnLineaIniciada sin pasar por PrepararTomaSincronizada
        int extra = Extra(id);
        if (enCurso)
        {
            if (id != idActual)
            {
                Terminar();
                if (i + extra == 0 && !iniciando && TieneGuion(id)) StartCoroutine(IniciarSincronizado(id, 0));
                return;
            }
            if (tomaIdx != Mathf.Clamp(i + extra, 0, tomas != null ? tomas.Count - 1 : 0))
                PedirToma(i + extra);
            return;
        }
        if (i + extra != 0 || iniciando || !TieneGuion(id)) return;
        StartCoroutine(IniciarSincronizado(id, 0));
    }

    static int Extra(string id) { return id == "jefe_despierta" || id == "jefe_derrotado" ? 1 : 0; }

    static bool EsDeJefe(string id) { return Extra(id) > 0; }

    void AlTerminarLinea(string id, int i)
    {
        if (enCurso && id == idActual && i >= totalLineas - 1) Terminar();
    }

    // ================================================================== ARRANQUE / SALIDA

    bool PuedeIniciar(string id)
    {
        if (jugador == null && GameObject.FindGameObjectWithTag("Player") != null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            jugador = p.transform;
            chofer = p.GetComponent<ChoferController>();
            poder = p.GetComponent<PoderEspiritual>();
        }
        if (jugador == null || (cam == null && Camera.main == null)) return false;
        if (EstadoNivel.Actual != EstadoDeNivel.Jugando || EstadoNivel.Terminado) return false;
        if (Time.timeScale < 0.01f || CodiceHistoria.Abierto) return false;
        if (poder != null && poder.EstaMuerto) return false;

        if (EsDeJefe(id) || id == "intro") return true;
        if (GameManager.Instancia != null && GameManager.Instancia.estadoJefe == GameManager.EstadoJefe.Combate) return false;

        foreach (EsbirroBaseIA e in UnityEngine.Object.FindObjectsByType<EsbirroBaseIA>())
        {
            if (e == null || e.EstaMuerto || !e.enabled) continue;
            Vector3 d = e.transform.position - jugador.position; d.y = 0f;
            if (d.magnitude < distanciaSeguridad) return false;
        }
        return true;
    }

    IEnumerator IniciarSincronizado(string id, int idxInicial)
    {
        if (iniciando) yield break;
        iniciando = true;
        while (saliendo) yield return null;

        cam = Camera.main;
        if (!PuedeIniciar(id)) { iniciando = false; yield break; }

        Contexto c = CrearContexto();
        List<Toma> guion = Guion(id, c);
        if (guion == null || guion.Count == 0) { iniciando = false; yield break; }

        tomas = guion;
        idActual = id;
        string[] lineas = HistoriaNivel1.Lineas(id);
        totalLineas = lineas != null ? lineas.Length : 1;
        tomaIdx = -1; tomaPedida = Mathf.Clamp(idxInicial, 0, tomas.Count - 1);
        cortando = false; preparado = false;
        enCurso = true;

        FadeA(1f, 0.25f);
        yield return EsperarFade();
        if (!enCurso) { iniciando = false; yield break; }

        Tomar();
        preparado = true;
        IrAToma(tomaPedida);
        // Posicionar la cámara inmediatamente en el fotograma 0 antes de aclarar
        if (tomas != null && tomaIdx >= 0 && tomaIdx < tomas.Count)
        {
            Toma toma = tomas[tomaIdx];
            if (toma.cadaCuadro != null) toma.cadaCuadro(0f);
            AplicarVista(toma.vista(0f));
        }
        FadeA(0f, 0.35f);
        iniciando = false;
    }

    IEnumerator CorteSincronizado()
    {
        cortando = true;
        FadeA(1f, 0.14f);
        yield return EsperarFade();
        if (enCurso && preparado && tomas != null)
        {
            IrAToma(tomaPedida);
            Toma toma = tomas[tomaIdx];
            if (toma.cadaCuadro != null) toma.cadaCuadro(0f);
            AplicarVista(toma.vista(0f));
        }
        if (enCurso) FadeA(0f, 0.24f);
        cortando = false;
    }

    void Terminar()
    {
        if (!enCurso) return;
        enCurso = false;
        StartCoroutine(Salida());
    }

    IEnumerator Salida()
    {
        saliendo = true;
        FadeA(1f, 0.25f);
        yield return EsperarFade();
        Restaurar();
        FadeA(0f, 0.4f);
        yield return EsperarFade();
        saliendo = false;
    }

    void Tomar()
    {
        if (tomado) return;
        tomado = true;
        if (cam == null) cam = Camera.main;
        fovOriginal = cam.fieldOfView;
        camTP = cam.GetComponent<CamaraTerceraPersona>();
        camTPEstabaActiva = camTP != null && camTP.enabled;
        if (camTP != null) camTP.enabled = false;

        if (chofer != null && !chofer.controlBloqueado) { chofer.controlBloqueado = true; controlLoBloqueamos = true; }
        if (poder != null) poder.invulnerable = true;
    }

    void Restaurar()
    {
        preparado = false;
        Liberar();
        foreach (GameObject lz in luces) if (lz != null) Destroy(lz);
        luces.Clear();
        foreach (GameObject obj in objetosEscena) if (obj != null) Destroy(obj);
        objetosEscena.Clear();
        if (rutinaRotulo != null) { StopCoroutine(rutinaRotulo); rutinaRotulo = null; }
        grupoRotulo.alpha = 0f;

        if (!tomado) return;
        tomado = false;
        if (cam != null) cam.fieldOfView = fovOriginal;
        if (camTP != null && camTPEstabaActiva) camTP.enabled = true;
        if (chofer != null && controlLoBloqueamos) chofer.controlBloqueado = false;
        controlLoBloqueamos = false;
        if (poder != null) poder.invulnerable = false;
        tomas = null; idActual = null; tomaIdx = -1;
    }

    // ================================================================== TOMAS

    void PedirToma(int i)
    {
        if (tomas == null) return;
        tomaPedida = Mathf.Clamp(i, 0, tomas.Count - 1);
        if (!preparado || cortando || tomaPedida == tomaIdx) return;
        StartCoroutine(CorteSincronizado());
    }

    void IrAToma(int i)
    {
        tomaIdx = i;
        tToma = 0f;
        Toma t = tomas[i];
        if (t.alEmpezar != null) t.alEmpezar();
        if (!string.IsNullOrEmpty(t.rotulo))
        {
            if (rutinaRotulo != null) StopCoroutine(rutinaRotulo);
            rutinaRotulo = StartCoroutine(MostrarRotulo(t.rotulo, t.subrotulo, t.carta));
        }
    }

    void Update()
    {
        if (enCurso && preparado && !iniciando && (Input.GetKeyDown(teclaSaltar) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            Terminar();

        if (enCurso && (EstadoNivel.Terminado || jugador == null || (poder != null && poder.EstaMuerto))) Terminar();
    }

    void LateUpdate()
    {
        // fundido
        fadeAlpha = Mathf.MoveTowards(fadeAlpha, fadeObj, fadeVel * Time.unscaledDeltaTime);
        grupoFundido.alpha = fadeAlpha;

        // barras de cine
        barraK = Mathf.MoveTowards(barraK, enCurso && preparado ? 1f : 0f, Time.unscaledDeltaTime / 0.55f);
        float h = Mathf.SmoothStep(0f, 1f, barraK) * 118f;
        barraSup.sizeDelta = new Vector2(0f, h);
        barraInf.sizeDelta = new Vector2(0f, h);
        grupoPista.alpha = Mathf.Clamp01((barraK - 0.6f) * 2.5f) * 0.75f;

        if (!enCurso || !preparado || tomas == null || tomaIdx < 0 || cam == null) return;

        tToma += Time.deltaTime;
        if (poder != null) poder.invulnerable = true;
        if (chofer != null) chofer.controlBloqueado = true;

        Toma toma = tomas[tomaIdx];
        if (toma.cadaCuadro != null) toma.cadaCuadro(tToma);
        MoverMobs();
        AplicarVista(toma.vista(tToma));
    }

    void AplicarVista(Vista v)
    {
        float t = Time.time;
        float amp = 0.012f + v.temblor;
        Vector3 ruido = new Vector3(
            Mathf.PerlinNoise(t * 0.9f, 1.3f) - 0.5f,
            Mathf.PerlinNoise(2.1f, t * 1.1f) - 0.5f,
            Mathf.PerlinNoise(t * 0.7f, 5.5f) - 0.5f) * 2f * amp;

        Vector3 pos = v.pos + ruido;
        float suelo = Suelo(pos);
        if (pos.y < suelo + 0.45f) pos.y = suelo + 0.45f;

        Vector3 dir = v.mira - pos;
        if (dir.sqrMagnitude < 0.01f) dir = jugador != null ? jugador.forward : Vector3.forward;
        float giro = v.roll + (Mathf.PerlinNoise(t * 0.5f, 9.7f) - 0.5f) * amp * 40f;
        Quaternion rot = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(0f, 0f, giro);

        cam.transform.SetPositionAndRotation(pos, rot);
        cam.fieldOfView = v.fov;
    }

    // ================================================================== FUNDIDO Y ROTULOS

    void FadeA(float objetivo, float seg) { fadeObj = objetivo; fadeVel = 1f / Mathf.Max(0.02f, seg); }

    IEnumerator EsperarFade()
    {
        while (!Mathf.Approximately(fadeAlpha, fadeObj)) yield return null;
    }

    IEnumerator MostrarRotulo(string rotulo, string sub, bool carta)
    {
        txtRotulo.text = rotulo;
        txtSub.text = sub ?? "";
        txtRotulo.color = carta ? UIMexicana.Dorado : UIMexicana.Crema;
        txtSub.fontStyle = carta ? FontStyles.Italic : FontStyles.Normal;
        lineaRotulo.color = UIMexicana.ConAlfa(carta ? UIMexicana.Dorado : UIMexicana.Cempasuchil, 0.9f);
        if (carta && fuente != null) fuente.PlayOneShot(sonidoCarta, 0.8f);

        yield return new WaitForSecondsRealtime(0.18f);
        float t = 0f;
        while (t < 0.45f) { t += Time.unscaledDeltaTime; grupoRotulo.alpha = Mathf.Clamp01(t / 0.45f); yield return null; }
        grupoRotulo.alpha = 1f;

        // Duración sincronizada con la línea de voz actual para no cortarse ni quedarse colgado
        float espera = Mathf.Clamp(duracionTomaActual - 1.15f, 2.2f, 6.2f);
        yield return new WaitForSecondsRealtime(espera);

        t = 0f;
        while (t < 0.55f) { t += Time.unscaledDeltaTime; grupoRotulo.alpha = 1f - Mathf.Clamp01(t / 0.55f); yield return null; }
        grupoRotulo.alpha = 0f;
        rutinaRotulo = null;
    }

    // ================================================================== ENEMIGOS DE ESCAPARATE

    Mob Presentar(EsbirroBaseIA.TipoEsbirro tipo, Vector3 pos, Vector3 mirarA)
    {
        pos = EnSuelo(pos, 0.05f);
        EsbirroBaseIA ia = FabricaEsbirros.Crear(tipo, pos);
        ia.enabled = false;                                   // sin IA: queda como estatua hasta que se libere
        CharacterController cc = ia.GetComponent<CharacterController>();
        cc.enabled = false;
        ia.transform.position = pos;
        Vector3 d = mirarA - pos; d.y = 0f;
        if (d.sqrMagnitude > 0.01f) ia.transform.rotation = Quaternion.LookRotation(d);

        Mob m = new Mob { ia = ia, cc = cc, tipo = tipo, visual = ia.transform.Find("Visual"), fase = UnityEngine.Random.value * 6f };
        mobs.Add(m);

        switch (tipo)
        {
            case EsbirroBaseIA.TipoEsbirro.Serpiente: CodiceHistoria.Registrar("bestia_serpiente"); break;
            case EsbirroBaseIA.TipoEsbirro.HombrePiedra: CodiceHistoria.Registrar("bestia_hombre_piedra"); break;
            case EsbirroBaseIA.TipoEsbirro.Comadre: CodiceHistoria.Registrar("bestia_comadre"); break;
        }
        return m;
    }

    void MoverMobs()
    {
        float t = Time.time;
        for (int i = 0; i < mobs.Count; i++)
        {
            Mob m = mobs[i];
            if (m == null || m.ia == null) continue;
            Transform tr = m.ia.transform;

            if (m.mover)
            {
                Vector3 p = Vector3.MoveTowards(tr.position, new Vector3(m.destino.x, tr.position.y, m.destino.z), m.vel * Time.deltaTime);
                p.y = Suelo(p) + 0.05f;
                Vector3 dir = p - tr.position; dir.y = 0f;
                if (dir.sqrMagnitude > 0.00001f) tr.rotation = Quaternion.Slerp(tr.rotation, Quaternion.LookRotation(dir), 8f * Time.deltaTime);
                tr.position = p;
                if ((new Vector3(m.destino.x, 0f, m.destino.z) - new Vector3(p.x, 0f, p.z)).sqrMagnitude < 0.04f) m.mover = false;
            }

            if (m.visual == null || m.animPersonalizada) continue;
            switch (m.tipo)
            {
                case EsbirroBaseIA.TipoEsbirro.Serpiente:
                    m.visual.localRotation = Quaternion.Euler(0f, Mathf.Sin(t * 2.2f + m.fase) * (m.mover ? 16f : 10f), 0f);
                    break;
                case EsbirroBaseIA.TipoEsbirro.Comadre:
                    m.visual.localRotation = Quaternion.Euler(Mathf.Sin(t * 5.8f + m.fase) * 2.5f, Mathf.Sin(t * 1.6f + m.fase) * 7f, 0f);
                    break;
            }
        }
    }

    void Girar(Mob m, Vector3 haciaPunto, float velocidad)
    {
        if (m == null || m.ia == null) return;
        Vector3 d = haciaPunto - m.ia.transform.position; d.y = 0f;
        if (d.sqrMagnitude < 0.01f) return;
        m.ia.transform.rotation = Quaternion.Slerp(m.ia.transform.rotation, Quaternion.LookRotation(d), velocidad * Time.deltaTime);
    }

    void Liberar()
    {
        foreach (Mob m in mobs)
        {
            if (m == null || m.ia == null) continue;
            if (m.visual != null)
            {
                m.visual.localPosition = Vector3.zero;
                m.visual.localRotation = Quaternion.identity;
                m.visual.localScale = Vector3.one;
            }
            if (m.ia.EstaMuerto) continue;
            m.cc.enabled = true;
            m.ia.enabled = true;
        }
        mobs.Clear();
    }

    // ================================================================== UTILIDADES

    static float Suelo(Vector3 p)
    {
        Terrain t = Terrain.activeTerrain;
        return t != null ? t.SampleHeight(p) + t.transform.position.y : p.y;
    }

    static Vector3 EnSuelo(Vector3 p, float altura) { p.y = Suelo(p) + altura; return p; }

    static Vector3 Plano(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude < 0.0001f ? Vector3.forward : v.normalized;
    }

    static float Ease(float t, float dur) { return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / Mathf.Max(0.01f, dur))); }

    static Vector3 Orb(Vector3 centro, float radio, float ang, float altura)
    {
        Vector3 p = centro + new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang)) * radio;
        p.y = Suelo(p) + altura;
        return p;
    }

    static float AngDe(Vector3 desdeCentroHacia) { return Mathf.Atan2(desdeCentroHacia.x, desdeCentroHacia.z); }

    static Vista V(Vector3 pos, Vector3 mira, float fov, float temblor = 0f, float roll = 0f)
    {
        return new Vista { pos = pos, mira = mira, fov = fov, temblor = temblor, roll = roll };
    }

    static Toma T(Func<float, Vista> vista, Action ini = null, string rotulo = null, string sub = null, bool carta = false, Action<float> cuadro = null)
    {
        return new Toma { vista = vista, alEmpezar = ini, rotulo = rotulo, subrotulo = sub, carta = carta, cadaCuadro = cuadro };
    }

    static Transform Buscar(string nombre)
    {
        GameObject g = GameObject.Find(nombre);
        return g != null ? g.transform : null;
    }

    static HistoriaNivel1.Bestia FichaDe(string id)
    {
        foreach (HistoriaNivel1.Bestia b in HistoriaNivel1.Bestiario) if (b.id == id) return b;
        return default(HistoriaNivel1.Bestia);
    }

    static string TextoCarta(string id, out string verso)
    {
        HistoriaNivel1.Bestia b = FichaDe(id);
        verso = b.verso != null ? b.verso.Replace("\n", "  ·  ") : "";
        return "CARTA No. " + b.numero + "   ·   " + (b.nombre ?? id).ToUpper();
    }

    Contexto CrearContexto()
    {
        Contexto c = new Contexto();
        c.jug = jugador.position;
        SenderoNivel s = SenderoNivel.Instancia;
        if (s != null && s.Valido)
        {
            Vector3 dir;
            s.Adelante(c.jug, 6f, out dir);
            c.adelante = Plano(dir);
            Vector3 origen = c.jug;
            c.ruta = m => { Vector3 d; return EnSuelo(s.Adelante(origen, m, out d), 0f); };
        }
        else
        {
            c.adelante = Plano(jugador.forward);
            Vector3 origen = c.jug, f = c.adelante;
            c.ruta = m => EnSuelo(origen + f * m, 0f);
        }
        c.derecha = Vector3.Cross(Vector3.up, c.adelante).normalized;
        return c;
    }

    // ================================================================== LUCES Y ESCENARIOS

    readonly List<GameObject> luces = new List<GameObject>();

    void NuevaLuz(Vector3 pos, Color color, float intensidad, float rango, float anguloSpot = 0f, Vector3? hacia = null)
    {
        GameObject g = new GameObject("LuzCine");
        g.transform.position = pos;
        Light l = g.AddComponent<Light>();
        l.color = color; l.intensity = intensidad; l.range = rango; l.shadows = LightShadows.None;
        if (anguloSpot > 0f && hacia.HasValue)
        {
            l.type = LightType.Spot; l.spotAngle = anguloSpot;
            g.transform.rotation = Quaternion.LookRotation(hacia.Value - pos);
        }
        else l.type = LightType.Point;
        luces.Add(g);
    }

    GameObject CrearEsferaEnergia(string nombre, Vector3 pos, float escala, Color color, Color emision)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        g.name = nombre;
        Collider col = g.GetComponent<Collider>();
        if (col != null) Destroy(col);
        g.transform.position = pos;
        g.transform.localScale = Vector3.one * escala;
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        Material m = new Material(sh);
        m.color = color;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        m.EnableKeyword("_EMISSION");
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", emision);
        g.GetComponent<Renderer>().sharedMaterial = m;
        objetosEscena.Add(g);
        return g;
    }

    bool Despejado(Vector3 a, Vector3 b)
    {
        RaycastHit h;
        if (!Physics.Linecast(a, b, out h, ~0, QueryTriggerInteraction.Ignore)) return true;
        return jugador != null && h.collider != null && h.collider.transform.root == jugador.root;
    }

    Vector3 ElegirEscenario(Contexto c, float[] distancias, float distCam, float alturaCam)
    {
        Vector3 mejor = c.ruta(distancias[0]);
        int mejorPts = -1;
        float[] laterales = { 0f, 4f, -4f, 8f, -8f };
        foreach (float m in distancias)
        {
            foreach (float lat in laterales)
            {
                Vector3 cen = EnSuelo(c.ruta(m) + c.derecha * lat, 0f);
                Vector3 hj = Plano(c.jug - cen);
                Vector3 camP = EnSuelo(cen + hj * distCam, alturaCam);
                int pts = 0;
                if (Despejado(camP, cen + Vector3.up * 1.0f)) pts += 3;
                if (Despejado(camP, cen + c.derecha * 3.2f + Vector3.up * 1.0f)) pts += 2;
                if (Despejado(camP, cen - c.derecha * 3.2f + Vector3.up * 1.0f)) pts += 2;
                Vector3 centroAlto = cen + Vector3.up * 1.0f;
                int libres = 0;
                Vector3[] dirs = { c.derecha, -c.derecha, c.adelante, -c.adelante };
                foreach (Vector3 d in dirs) if (!Physics.Raycast(centroAlto, d, 3.4f, ~0, QueryTriggerInteraction.Ignore)) libres++;
                pts += libres;
                pts -= Mathf.RoundToInt(Mathf.Abs(lat) * 0.15f);
                if (pts > mejorPts) { mejorPts = pts; mejor = cen; }
                if (pts >= 11) return mejor;
            }
        }
        return mejor;
    }

    // ================================================================== GUIONES SINCRONIZADOS (una toma por línea)

    static readonly HashSet<string> conGuion = new HashSet<string>
    {
        "intro", "sierra", "serpientes", "hombres_piedra", "comadres",
        "corona", "tesoro", "arroyo", "cruces", "comerciante", "cueva",
        "jefe_despierta", "jefe_derrotado"
    };

    bool TieneGuion(string id) { return conGuion.Contains(id); }

    List<Toma> Guion(string id, Contexto c)
    {
        var l = new List<Toma>();
        Vector3 j = c.jug, f = c.adelante, r = c.derecha, arriba = Vector3.up;

        switch (id)
        {
            // ---------------------------------------------------------------- INTRO (4 líneas: Tonalli, Sierra, Joven de Piedra, Tótems)
            case "intro":
            {
                CriaturaGuia criatura = UnityEngine.Object.FindAnyObjectByType<CriaturaGuia>();
                Transform cerro = Buscar("Cerro_Mirador") ?? Buscar("Corona_La_Bufa");
                Transform corona = Buscar("Corona_La_Bufa") ?? cerro;
                Transform cueva = Buscar("Cueva_La_Bufa") ?? (JefeLaBufa.Instancia != null ? JefeLaBufa.Instancia.transform : null);
                TotemColeccionable totem = UnityEngine.Object.FindAnyObjectByType<TotemColeccionable>();

                // Toma 0 (intro_0): "¡Por fin despiertas! Soy Tonalli, un alebrije..." -> Chofer recién levantado junto a Tonalli brillando
                l.Add(T(t =>
                {
                    float e = Progreso(t);
                    Vector3 posTonalli = criatura != null ? criatura.transform.position : (j + f * 2.2f + arriba * 1.6f);
                    Vector3 centro = Vector3.Lerp(j + arriba * 1.35f, posTonalli, 0.55f);
                    Vector3 p = j - f * Mathf.Lerp(2.4f, 3.4f, e) + r * Mathf.Lerp(1.6f, 0.95f, e);
                    p.y = Suelo(p) + Mathf.Lerp(1.45f, 1.75f, e);
                    return V(p, centro, Mathf.Lerp(50f, 44f, e), 0.006f);
                }, () =>
                {
                    NuevaLuz(j + f * 1.8f + arriba * 3.5f, new Color(1f, 0.72f, 0.28f), 8f, 14f);
                    NuevaLuz(j - f * 1.5f + arriba * 2.5f, new Color(0.3f, 0.9f, 1f), 6f, 12f);
                }, "CLARO DE LA OFRENDA", "«Tu alma se soltó del cuerpo… vine a guiarte»", false));

                // Toma 1 (intro_1): "Esta es la Sierra de Guanajuato. Bajo el Cerro de La Bufa se cuenta una leyenda..."
                Vector3 posCerro = corona != null ? corona.position : (j + f * 180f + arriba * 35f);
                Vector3 dirCerro = Plano(posCerro - j);
                l.Add(T(t =>
                {
                    float e = Progreso(t);
                    Vector3 p = j - dirCerro * Mathf.Lerp(6f, 12f, e) + arriba * Mathf.Lerp(4f, 14f, e);
                    p.y = Mathf.Max(p.y, Suelo(p) + 4f);
                    Vector3 mira = Vector3.Lerp(j + dirCerro * 40f + arriba * 8f, posCerro + arriba * 16f, e);
                    return V(p, mira, Mathf.Lerp(56f, 44f, e), 0.005f);
                }, () =>
                {
                    if (corona != null)
                        NuevaLuz(posCerro - dirCerro * 25f + arriba * 28f, new Color(1f, 0.78f, 0.42f), 28f, 95f, 80f, posCerro + arriba * 12f);
                }, "SIERRA DE GUANAJUATO", "«Bajo el Cerro de La Bufa se cuenta una leyenda…»", false));

                // Toma 2 (intro_2): "Dicen que una joven fue convertida en piedra junto a la Cueva Vieja..."
                Vector3 posCueva = cueva != null ? cueva.position : posCerro;
                Vector3 hc = Plano(j - posCueva);
                l.Add(T(t =>
                {
                    float e = Progreso(t);
                    Vector3 p = posCueva + hc * Mathf.Lerp(22f, 13f, e) + Vector3.Cross(arriba, hc) * Mathf.Lerp(-4f, 2f, e);
                    p.y = Suelo(p) + Mathf.Lerp(3.2f, 2.2f, e);
                    return V(p, posCueva + arriba * 3.0f, Mathf.Lerp(50f, 38f, e), 0.008f);
                }, () =>
                {
                    NuevaLuz(posCueva + arriba * 8f + hc * 5f, new Color(0.85f, 0.55f, 1f), 16f, 32f, 75f, posCueva + arriba * 2.5f);
                }, "LA CUEVA VIEJA", "«Una joven fue convertida en piedra…»", false));

                // Toma 3 (intro_3): "Reúne tres tótems: son los pedazos de ese encanto... Si te pierdes, pulsa R"
                Vector3 posTotem = totem != null ? totem.transform.position : c.ruta(28f);
                Vector3 ht = Plano(j - posTotem);
                l.Add(T(t =>
                {
                    float e = Progreso(t);
                    if (e < 0.58f)
                    {
                        float k = Mathf.SmoothStep(0f, 1f, e / 0.58f);
                        Vector3 p = Orb(posTotem, Mathf.Lerp(7.5f, 4.8f, k), AngDe(ht) + k * 0.7f, Mathf.Lerp(2.4f, 1.6f, k));
                        return V(p, posTotem + arriba * 1.2f, Mathf.Lerp(48f, 40f, k), 0.006f);
                    }
                    else
                    {
                        float k = Mathf.SmoothStep(0f, 1f, (e - 0.58f) / 0.42f);
                        Vector3 p = j - f * Mathf.Lerp(5.5f, 4.2f, k) + r * Mathf.Lerp(1.2f, 0.5f, k);
                        p.y = Suelo(p) + 1.75f;
                        return V(p, j + f * 12f + arriba * 1.4f, Mathf.Lerp(44f, 54f, k), 0.005f);
                    }
                }, () =>
                {
                    NuevaLuz(posTotem + arriba * 4f, new Color(0.3f, 1f, 0.9f), 12f, 16f);
                }, "LOS TRES TÓTEMS", "Pulsa R cuando quieras que Tonalli marque el camino", false));
                break;
            }

            // ---------------------------------------------------------------- SIERRA / CERRO (2 líneas: Cerros de cantera + Guardianes)
            case "sierra":
            {
                Transform mirador = Buscar("Cerro_Mirador") ?? Buscar("Cerro_Rocoso_5") ?? Buscar("Corona_La_Bufa");
                Transform corona = Buscar("Corona_La_Bufa") ?? mirador;
                Vector3 posCerro = mirador != null ? mirador.position : (c.ruta(35f) + r * 45f);
                Vector3 haciaJugador = Plano(j - posCerro);
                Vector3 latCerro = Vector3.Cross(arriba, haciaJugador).normalized;

                // Toma 0 (sierra_0): "Mira esos cerros de cantera. Dicen que La Bufa guarda en sus entrañas paredes de oro y pisos de plata."
                l.Add(T(t =>
                {
                    float e = Progreso(t);
                    Vector3 p = posCerro + haciaJugador * Mathf.Lerp(42f, 28f, e) + latCerro * Mathf.Lerp(-16f, 12f, e);
                    p.y = Suelo(p) + Mathf.Lerp(6f, 11f, e);
                    Vector3 mira = posCerro + arriba * Mathf.Lerp(8f, 15f, e);
                    return V(p, mira, Mathf.Lerp(54f, 42f, e), 0.006f);
                }, () =>
                {
                    // Bañar el cerro con reflejos de oro y plata como dice Tonalli
                    NuevaLuz(posCerro + haciaJugador * 22f + arriba * 24f - latCerro * 10f, new Color(1f, 0.78f, 0.25f), 32f, 80f, 80f, posCerro + arriba * 10f);
                    NuevaLuz(posCerro + haciaJugador * 18f + arriba * 14f + latCerro * 12f, new Color(0.82f, 0.92f, 1f), 24f, 65f, 75f, posCerro + arriba * 8f);
                    if (corona != null && corona != mirador)
                        NuevaLuz(corona.position - Plano(corona.position - j) * 25f + arriba * 26f, new Color(1f, 0.72f, 0.3f), 28f, 90f);
                }, "CERROS DE CANTERA · LA BUFA", "«Paredes de oro y pisos de plata»", false));

                // Toma 1 (sierra_1): "Por eso tantos subieron a buscarla… y por eso la montaña tiene guardianes."
                Vector3 puntoSendero = ElegirEscenario(c, new[] { 20f, 16f, 25f }, 10f, 2.2f);
                Vector3 hs = Plano(j - puntoSendero);
                l.Add(T(t =>
                {
                    float e = Progreso(t);
                    // La cámara baja desde la vista del cerro hacia el sendero custodiado entre las peñas
                    Vector3 p = puntoSendero + hs * Mathf.Lerp(16f, 8.5f, e) + r * Mathf.Lerp(6f, 1.5f, e);
                    p.y = Suelo(p) + Mathf.Lerp(7.5f, 2.0f, e);
                    Vector3 mira = Vector3.Lerp(posCerro + arriba * 10f, puntoSendero + arriba * 1.4f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(e * 1.35f)));
                    return V(p, mira, Mathf.Lerp(50f, 40f, e), 0.008f);
                }, () =>
                {
                    NuevaLuz(puntoSendero + arriba * 7f, new Color(0.65f, 0.88f, 1f), 12f, 20f, 75f, puntoSendero);
                }, "EL SENDERO DE LA SIERRA", "«Por eso la montaña tiene guardianes»", false));
                break;
            }

            // ---------------------------------------------------------------- SERPIENTES (2 líneas sincronizadas con movimiento + enroscarse)
            case "serpientes":
            {
                Vector3 centro = ElegirEscenario(c, new[] { 15f, 12f, 18f, 21f, 10f }, 8.5f, 1.8f);
                Vector3 hj = Plano(j - centro);
                string verso; string rot = TextoCarta("serpiente", out verso);
                Mob[] serpientes = new Mob[3];
                Vector3[] posBase = new Vector3[3];
                GameObject luzAviso = null;

                // Toma 0 (serpientes_0): "¡Cuidado, serpientes! No son comunes: nacen del hechizo. La leyenda dice que la joven, al cargarla, se vuelve serpiente."
                l.Add(T(t =>
                {
                    float e = Progreso(t);
                    Vector3 p = centro + hj * Mathf.Lerp(9.2f, 5.2f, e) + r * Mathf.Lerp(-1.8f, 0.6f, e);
                    p.y = Suelo(p) + Mathf.Lerp(2.1f, 1.15f, e);
                    return V(p, centro + arriba * 0.4f, Mathf.Lerp(52f, 38f, e), 0.008f);
                }, () =>
                {
                    NuevaLuz(centro + arriba * 6f + hj * 1.5f, new Color(0.55f, 1f, 0.82f), 16f, 16f, 75f, centro);
                    NuevaLuz(centro + hj * 2.8f + arriba * 1.3f, new Color(1f, 0.85f, 0.45f), 5f, 9f);
                    for (int k = 0; k < 3; k++)
                    {
                        posBase[k] = EnSuelo(centro + r * ((k - 1) * 2.7f) + f * (k == 1 ? -1.1f : 0.9f), 0.05f);
                        Vector3 posIni = EnSuelo(posBase[k] - hj * 2.6f + r * ((k - 1) * 1.1f), 0.05f);
                        serpientes[k] = Presentar(EsbirroBaseIA.TipoEsbirro.Serpiente, posIni, j);
                        serpientes[k].destino = posBase[k];
                        serpientes[k].vel = 2.4f;
                        serpientes[k].mover = true;
                    }
                }, rot, verso, true));

                // Toma 1 (serpientes_1): "Son rápidas pero frágiles. Golpea con combo y esquiva cuando se enrosquen."
                float a0 = AngDe(hj);
                l.Add(T(t =>
                {
                    float e = Progreso(t);
                    float fr = Frac(t);
                    // En la segunda mitad ("esquiva cuando se enrosquen") la cámara se acerca al enrosque y tiembla con la mordida
                    float radio = fr < 0.52f ? Mathf.Lerp(6.0f, 5.2f, fr / 0.52f) : Mathf.Lerp(5.2f, 4.3f, (fr - 0.52f) / 0.48f);
                    float temblor = fr > 0.82f ? 0.035f : 0.007f;
                    return V(Orb(centro, radio, a0 - 0.35f + 0.95f * e, Mathf.Lerp(1.75f, 1.25f, e)), centro + arriba * 0.45f, Mathf.Lerp(45f, 38f, e), temblor);
                }, () =>
                {
                    luzAviso = new GameObject("LuzEnrosqueCine");
                    luzAviso.transform.position = centro + arriba * 1.1f;
                    Light l = luzAviso.AddComponent<Light>();
                    l.type = LightType.Point; l.color = new Color(1f, 0.65f, 0.1f); l.range = 8f; l.intensity = 0f;
                    luces.Add(luzAviso);
                }, null, null, false, t =>
                {
                    float fr = Frac(t);
                    for (int k = 0; k < 3; k++)
                    {
                        Mob m = serpientes[k];
                        if (m == null || m.ia == null || m.visual == null) continue;
                        m.animPersonalizada = true;

                        if (fr < 0.50f)
                        {
                            // "Son rápidas pero frágiles": zigzaguean velozmente de lado a lado
                            m.mover = false;
                            float onda = Mathf.Sin(t * 7.5f + k * 2.1f);
                            Vector3 p = posBase[k] + r * (onda * 1.15f);
                            p.y = Suelo(p) + 0.05f;
                            m.ia.transform.position = p;
                            Girar(m, j, 10f);
                            m.visual.localPosition = Vector3.zero;
                            m.visual.localRotation = Quaternion.Euler(0f, onda * 28f, 0f);
                            m.visual.localScale = Vector3.one;
                        }
                        else if (fr < 0.84f)
                        {
                            // "...y esquiva cuando se enrosquen": se comprimen, levantan la cabeza y vibran con brillo de aviso
                            float kEnrosque = Mathf.SmoothStep(0f, 1f, (fr - 0.50f) / 0.34f);
                            Girar(m, j, 12f);
                            m.visual.localScale = new Vector3(1f + 0.25f * kEnrosque, 1f + 0.35f * kEnrosque, 1f - 0.42f * kEnrosque);
                            m.visual.localPosition = new Vector3(Mathf.Sin(t * 38f + k) * 0.05f * kEnrosque, 0.22f * kEnrosque, -0.25f * kEnrosque);
                            m.visual.localRotation = Quaternion.Euler(-24f * kEnrosque, Mathf.Sin(t * 25f + k) * 8f, 0f);
                            if (luzAviso != null) luzAviso.GetComponent<Light>().intensity = 7f * kEnrosque;
                        }
                        else
                        {
                            // Lanzan la tarascada hacia adelante al final de la frase
                            float kSalto = Mathf.SmoothStep(0f, 1f, (fr - 0.84f) / 0.16f);
                            m.visual.localScale = new Vector3(0.9f, 0.95f, 1.35f);
                            m.visual.localPosition = new Vector3(0f, 0.1f * (1f - kSalto), Mathf.Lerp(-0.25f, 0.85f, kSalto));
                            m.visual.localRotation = Quaternion.Euler(10f, 0f, 0f);
                            if (luzAviso != null) luzAviso.GetComponent<Light>().intensity = Mathf.Lerp(7f, 0f, kSalto);
                        }
                    }
                }));
                break;
            }

            // ---------------------------------------------------------------- HOMBRES DE PIEDRA (3 líneas: Figuras -> Voltean -> Puño y Agacharse)
            case "hombres_piedra":
            {
                Vector3 cen = ElegirEscenario(c, new[] { 17f, 14f, 20f, 24f }, 12f, 1.8f);
                Vector3 hj = Plano(j - cen);
                Vector3 pa = EnSuelo(cen - r * 3.1f, 0f), pb = EnSuelo(cen + r * 3.1f + f * 1.8f, 0f);
                string verso; string rot = TextoCarta("hombre_piedra", out verso);
                Mob[] colosos = new Mob[2];
                GameObject luzImpacto = null;

                // Toma 0 (hombres_piedra_0): "Allá adelante… ¿ves esas figuras? Son hombres de piedra. Dicen que son los jóvenes que intentaron salvarla."
                // Empiezan de espaldas (mirando camino arriba/adelante), congelados como estatuas en la sierra
                l.Add(T(t =>
                {
                    float e = Progreso(t);
                    Vector3 p = cen + hj * Mathf.Lerp(13.5f, 8.2f, e) + r * Mathf.Lerp(-1.5f, 0.5f, e);
                    p.y = Suelo(p) + Mathf.Lerp(2.1f, 1.65f, e);
                    return V(p, cen + arriba * 1.55f, Mathf.Lerp(46f, 34f, e), 0.006f);
                }, () =>
                {
                    NuevaLuz(pa + arriba * 6.5f + hj * 3f, new Color(0.7f, 0.9f, 1f), 16f, 16f, 65f, pa + arriba * 1.2f);
                    NuevaLuz(pb + arriba * 6.5f + hj * 3f, new Color(0.7f, 0.9f, 1f), 16f, 16f, 65f, pb + arriba * 1.2f);
                    NuevaLuz(cen + hj * 5f + arriba * 1.8f, new Color(1f, 0.8f, 0.5f), 4.5f, 11f);
                    // Empiezan mirando hacia adelante (de espaldas al Chofer), tal como dice la leyenda antes de voltear
                    colosos[0] = Presentar(EsbirroBaseIA.TipoEsbirro.HombrePiedra, pa, pa + f * 10f);
                    colosos[1] = Presentar(EsbirroBaseIA.TipoEsbirro.HombrePiedra, pb, pb + f * 10f);
                }, rot, verso, true));

                // Toma 1 (hombres_piedra_1): "Cargaron a la joven rumbo a la basílica sin mirar atrás… pero todos voltearon. Ahora vigilan el sendero, atrapados como ella."
                float a0 = AngDe(Plano(j - pa));
                l.Add(T(t =>
                {
                    float e = Progreso(t);
                    float fr = Frac(t);
                    float temblor = (fr > 0.38f && fr < 0.72f) ? 0.022f : 0.007f;
                    Vector3 p = Orb(pa, Mathf.Lerp(5.2f, 4.4f, e), a0 + 0.15f + 0.65f * e, Mathf.Lerp(1.45f, 1.25f, e));
                    return V(p, EnSuelo(Vector3.Lerp(pa, cen, 0.35f), 2.15f), Mathf.Lerp(44f, 38f, e), temblor);
                }, null, null, null, false, t =>
                {
                    float fr = Frac(t);
                    // Justo cuando Tonalli dice "...pero todos voltearon. Ahora vigilan el sendero", giran pesadamente hacia el Chofer
                    if (fr >= 0.36f)
                    {
                        Girar(colosos[0], j, 3.8f);
                        Girar(colosos[1], j, 3.4f);
                    }
                }));

                // Toma 2 (hombres_piedra_2): "Son lentos, pero su puño duele. Esquiva cuando se agachen."
                l.Add(T(t =>
                {
                    float e = Progreso(t);
                    float fr = Frac(t);
                    // Temblor fuerte justo cuando golpean el suelo ("su puño duele")
                    float impacto = (fr > 0.28f && fr < 0.50f) ? 0.075f * Mathf.Exp(-(fr - 0.28f) * 14f) : 0.008f;
                    Vector3 p = cen + hj * Mathf.Lerp(7.8f, 6.2f, e) + r * Mathf.Lerp(2.2f, 0.8f, e);
                    p.y = Suelo(p) + Mathf.Lerp(1.65f, 1.35f, e);
                    return V(p, cen + arriba * 1.45f, Mathf.Lerp(44f, 36f, e), impacto);
                }, () =>
                {
                    luzImpacto = new GameObject("LuzGolpePiedra");
                    luzImpacto.transform.position = pa + hj * 1.2f + arriba * 0.6f;
                    Light l = luzImpacto.AddComponent<Light>();
                    l.type = LightType.Point; l.color = new Color(1f, 0.6f, 0.2f); l.range = 10f; l.intensity = 0f;
                    luces.Add(luzImpacto);
                }, null, null, false, t =>
                {
                    float fr = Frac(t);
                    for (int k = 0; k < 2; k++)
                    {
                        Mob m = colosos[k];
                        if (m == null || m.ia == null || m.visual == null) continue;
                        m.animPersonalizada = true;
                        Girar(m, j, 6f);

                        if (fr < 0.28f)
                        {
                            // "Son lentos...": levantan el torso y los puños pesadamente
                            float kSub = Mathf.SmoothStep(0f, 1f, fr / 0.28f);
                            m.visual.localPosition = new Vector3(0f, 0.18f * kSub, 0f);
                            m.visual.localRotation = Quaternion.Euler(-16f * kSub, 0f, 0f);
                            m.visual.localScale = Vector3.one;
                        }
                        else if (fr < 0.54f)
                        {
                            // "...pero su puño duele": descargan el puñetazo contra el suelo
                            float kGolpe = Mathf.SmoothStep(0f, 1f, (fr - 0.28f) / 0.12f);
                            m.visual.localPosition = new Vector3(0f, Mathf.Lerp(0.18f, -0.15f, kGolpe), Mathf.Lerp(0f, 0.35f, kGolpe));
                            m.visual.localRotation = Quaternion.Euler(Mathf.Lerp(-16f, 22f, kGolpe), 0f, 0f);
                            if (luzImpacto != null)
                                luzImpacto.GetComponent<Light>().intensity = Mathf.Sin(Mathf.Clamp01((fr - 0.28f) / 0.24f) * Mathf.PI) * 12f;
                        }
                        else
                        {
                            // "Esquiva cuando se agachen": se agachan comprimiendo el cuerpo (telegrafiado real del enemigo)
                            float kAgachar = Mathf.SmoothStep(0f, 1f, (fr - 0.54f) / 0.30f);
                            m.visual.localScale = Vector3.Lerp(Vector3.one, new Vector3(1.18f, 0.68f, 1.18f), kAgachar);
                            m.visual.localPosition = new Vector3(Mathf.Sin(t * 32f) * 0.04f * kAgachar, -0.22f * kAgachar, 0f);
                            m.visual.localRotation = Quaternion.Euler(14f * kAgachar, 0f, 0f);
                            if (luzImpacto != null)
                                luzImpacto.GetComponent<Light>().intensity = 4.5f * kAgachar;
                        }
                    }
                }));
                break;
            }

            // ---------------------------------------------------------------- LAS DOS COMADRES (2 líneas: Cuchicheo iluminado -> Voltean y disparan murmullo)
            case "comadres":
            {
                Vector3 mid = ElegirEscenario(c, new[] { 18f, 15f, 21f, 24f }, 10f, 1.6f);
                Vector3 ca = EnSuelo(mid - r * 2.2f, 0f), cb = EnSuelo(mid + r * 2.2f, 0f);
                Mob[] cm = new Mob[2];
                Vector3 hj = Plano(j - mid);
                string verso; string rot = TextoCarta("comadre", out verso);
                GameObject orbeChisme = null;
                GameObject proyectilMurmullo = null;

                // Toma 0 (comadres_0): "Escucha… ¿murmullos? Son las Comadres: dos viejas que se pelearon por un hombre y quedaron hechas roca."
                l.Add(T(t =>
                {
                    float e = Progreso(t);
                    Vector3 p = mid + hj * Mathf.Lerp(11.5f, 6.8f, e) - r * Mathf.Lerp(2.4f, 0.4f, e);
                    p.y = Suelo(p) + Mathf.Lerp(1.55f, 1.35f, e);
                    return V(p, mid + arriba * 1.45f, Mathf.Lerp(44f, 34f, e), 0.012f);
                }, () =>
                {
                    // Iluminación teatral magenta y cálida para que las Comadres se vean con total claridad de noche
                    NuevaLuz(mid + arriba * 6f + hj * 2.5f, new Color(0.95f, 0.55f, 1f), 16f, 16f, 72f, mid + arriba * 1.3f);
                    NuevaLuz(ca + hj * 3f + arriba * 2.2f, new Color(1f, 0.78f, 0.55f), 6f, 10f);
                    NuevaLuz(cb + hj * 3f + arriba * 2.2f, new Color(1f, 0.78f, 0.55f), 6f, 10f);

                    cm[0] = Presentar(EsbirroBaseIA.TipoEsbirro.Comadre, ca, cb);
                    cm[1] = Presentar(EsbirroBaseIA.TipoEsbirro.Comadre, cb, ca);

                    // Orbe vibrante de murmullos entre sus dos bocas mientras chismeaban
                    orbeChisme = CrearEsferaEnergia("OrbeMurmulloCine", mid + arriba * 1.55f, 0.28f,
                        new Color(0.92f, 0.4f, 1f, 0.9f), new Color(2.4f, 0.7f, 2.8f));
                }, rot, verso, true, t =>
                {
                    // Se inclinan una hacia la otra cuchicheando y el murmullo pulsa entre ellas
                    if (orbeChisme != null)
                    {
                        float s = 0.24f + Mathf.Sin(t * 14f) * 0.09f;
                        orbeChisme.transform.localScale = Vector3.one * s;
                        orbeChisme.transform.position = mid + arriba * (1.55f + Mathf.Sin(t * 6f) * 0.06f);
                    }
                    for (int k = 0; k < 2; k++)
                    {
                        if (cm[k] == null || cm[k].visual == null) continue;
                        cm[k].animPersonalizada = true;
                        float cabezeo = Mathf.Sin(t * 9f + k * 2.2f) * 6f;
                        cm[k].visual.localRotation = Quaternion.Euler(10f + cabezeo, Mathf.Sin(t * 3.5f + k) * 8f, 0f);
                    }
                }));

                // Toma 1 (comadres_1): "Desde entonces solo hacen una cosa: chismear. Y sus murmullos hieren como piedras. ¡Muévete y acércate para callarlas!"
                float a0 = AngDe(hj);
                l.Add(T(t =>
                {
                    float e = Progreso(t);
                    float fr = Frac(t);
                    Vector3 camPos = Orb(mid, Mathf.Lerp(5.8f, 5.0f, e), a0 - 0.35f + 0.65f * e, Mathf.Lerp(1.55f, 1.45f, e));
                    float temblor = (fr > 0.42f && fr < 0.75f) ? 0.028f : 0.008f;
                    return V(camPos, mid + arriba * 1.45f, Mathf.Lerp(44f, 38f, e), temblor);
                }, () =>
                {
                    proyectilMurmullo = CrearEsferaEnergia("ProyectilMurmulloCine", mid + arriba * 1.55f, 0.01f,
                        new Color(0.95f, 0.35f, 1f), new Color(3.0f, 0.9f, 3.4f));
                }, null, null, false, t =>
                {
                    float fr = Frac(t);
                    Vector3 camPos = Orb(mid, Mathf.Lerp(5.8f, 5.0f, Progreso(t)), a0 - 0.35f + 0.65f * Progreso(t), 1.5f);

                    if (fr < 0.30f)
                    {
                        // "Desde entonces solo hacen una cosa: chismear." -> siguen cuchicheando entre sí
                        if (orbeChisme != null)
                            orbeChisme.transform.localScale = Vector3.one * (0.28f + Mathf.Sin(t * 16f) * 0.1f);
                    }
                    else
                    {
                        // "Y sus murmullos hieren como piedras. ¡Muévete y acércate para callarlas!"
                        // Voltean bruscamente hacia la cámara/Chofer y disparan una onda de murmullo púrpura
                        Girar(cm[0], camPos, 6.5f);
                        Girar(cm[1], camPos, 6.5f);

                        if (orbeChisme != null)
                            orbeChisme.transform.localScale = Vector3.Lerp(orbeChisme.transform.localScale, Vector3.zero, 10f * Time.deltaTime);

                        float kDisp = Mathf.Clamp01((fr - 0.34f) / 0.38f);
                        for (int k = 0; k < 2; k++)
                        {
                            if (cm[k] == null || cm[k].visual == null) continue;
                            cm[k].animPersonalizada = true;
                            cm[k].visual.localRotation = Quaternion.Euler(-12f * Mathf.Sin(kDisp * Mathf.PI), 0f, 0f);
                        }

                        if (proyectilMurmullo != null)
                        {
                            Vector3 origen = Vector3.Lerp(ca, cb, 0.5f) + arriba * 1.55f;
                            Vector3 destino = camPos - arriba * 0.25f + r * 0.6f;
                            proyectilMurmullo.transform.position = Vector3.Lerp(origen, destino, kDisp);
                            float esc = kDisp < 0.95f ? (0.38f + Mathf.Sin(t * 25f) * 0.08f) : 0f;
                            proyectilMurmullo.transform.localScale = Vector3.one * esc;
                        }
                    }
                }));
                break;
            }

            // ---------------------------------------------------------------- CORONA DE LA BUFA (2 líneas)
            case "corona":
            {
                Transform corona = Buscar("Corona_La_Bufa") ?? Buscar("Cueva_La_Bufa");
                Vector3 posCorona = corona != null ? corona.position : c.ruta(45f);
                Vector3 hj = Plano(j - posCorona);
                Vector3 lat = Vector3.Cross(arriba, hj).normalized;

                l.Add(T(t =>
                {
                    float e = Progreso(t);
                    Vector3 p = posCorona + hj * Mathf.Lerp(45f, 30f, e) + lat * Mathf.Lerp(-14f, 10f, e);
                    p.y = Suelo(p) + Mathf.Lerp(8f, 14f, e);
                    return V(p, posCorona + arriba * Mathf.Lerp(10f, 16f, e), Mathf.Lerp(52f, 40f, e), 0.006f);
                }, () =>
                {
                    NuevaLuz(posCorona + hj * 20f + arriba * 26f, new Color(1f, 0.68f, 0.28f), 32f, 95f, 80f, posCorona + arriba * 10f);
                }, "LA CORONA DE LA BUFA", "«Esas rocas guardan la Cueva Vieja»", false));

                l.Add(T(t =>
                {
                    float e = Progreso(t);
                    Vector3 p = j - f * Mathf.Lerp(5.5f, 3.8f, e) + r * Mathf.Lerp(1.6f, 0.8f, e);
                    p.y = Suelo(p) + 1.8f;
                    return V(p, posCorona + arriba * 8f, Mathf.Lerp(48f, 42f, e), 0.006f);
                }, null, "EL ÚLTIMO TRAMO", "«Respira, afina el esquive… y no mires atrás»", false));
                break;
            }

            // ---------------------------------------------------------------- BOCAMINA / TESORO (1 línea)
            case "tesoro":
            {
                Transform mina = Buscar("Bocamina_Abandonada");
                Vector3 posMina = mina != null ? mina.position : (c.ruta(18f) + r * 18f);
                Vector3 frente = mina != null ? mina.forward : Plano(j - posMina);
                Vector3 der = Vector3.Cross(arriba, frente).normalized;

                l.Add(T(t =>
                {
                    float e = Progreso(t);
                    Vector3 p = posMina + frente * Mathf.Lerp(13f, 7.5f, e) + der * Mathf.Lerp(-3.5f, 1.5f, e);
                    p.y = Suelo(p) + Mathf.Lerp(2.4f, 1.7f, e);
                    return V(p, posMina + arriba * 1.8f, Mathf.Lerp(50f, 38f, e), 0.006f);
                }, () =>
                {
                    NuevaLuz(posMina + frente * 4f + arriba * 4.5f, new Color(1f, 0.72f, 0.32f), 14f, 20f, 70f, posMina + arriba * 1.5f);
                }, "BOCAMINA ABANDONADA", "«Muchos entraron por la plata y ninguno volvió a salir»", false));
                break;
            }

            // ---------------------------------------------------------------- ARROYO SECO (2 líneas)
            case "arroyo":
            {
                Transform arroyo = Buscar("Cauce_Arroyo_Seco");
                Vector3 cen = arroyo != null ? EnSuelo(j + f * 14f, 0f) : ElegirEscenario(c, new[] { 14f, 18f }, 9f, 1.8f);
                Vector3 hj = Plano(j - cen);

                l.Add(T(t =>
                {
                    float e = Progreso(t);
                    Vector3 p = cen + hj * Mathf.Lerp(11f, 6.5f, e) - r * Mathf.Lerp(3f, 0f, e);
                    p.y = Suelo(p) + Mathf.Lerp(2.8f, 1.5f, e);
                    return V(p, cen + arriba * 0.5f, Mathf.Lerp(52f, 42f, e), 0.006f);
                }, () =>
                {
                    NuevaLuz(cen + arriba * 6f, new Color(0.65f, 0.85f, 1f), 14f, 18f);
                }, "EL ARROYO SECO", "«Bajaba agua clara desde La Bufa, hasta que cayó el hechizo»", false));

                l.Add(T(t =>
                {
                    float e = Progreso(t);
                    Vector3 p = cen + hj * 5.5f + r * Mathf.Lerp(-2.5f, 2.5f, e);
                    p.y = Suelo(p) + 1.1f;
                    return V(p, cen + f * 3f + arriba * 0.35f, Mathf.Lerp(44f, 36f, e), 0.008f);
                }));
                break;
            }

            // ---------------------------------------------------------------- CRUCES CON VELADORAS (2 líneas)
            case "cruces":
            {
                Transform cruzCercana = null;
                float mejorD = float.MaxValue;
                for (int k = 0; k < 8; k++)
                {
                    Transform tr = Buscar("Cruz_Camino_" + k);
                    if (tr == null) continue;
                    float d = (tr.position - j).sqrMagnitude;
                    if (d < mejorD) { mejorD = d; cruzCercana = tr; }
                }
                Vector3 posCruz = cruzCercana != null ? cruzCercana.position : (c.ruta(10f) + r * 3.4f);
                Vector3 hj = Plano(j - posCruz);

                l.Add(T(t =>
                {
                    float e = Progreso(t);
                    Vector3 p = posCruz + hj * Mathf.Lerp(5.5f, 3.2f, e) + r * Mathf.Lerp(-1.5f, 0.8f, e);
                    p.y = Suelo(p) + Mathf.Lerp(1.6f, 1.05f, e);
                    return V(p, posCruz + arriba * 0.95f, Mathf.Lerp(48f, 36f, e), 0.005f);
                }, () =>
                {
                    NuevaLuz(posCruz + arriba * 2.2f + hj * 1.2f, new Color(1f, 0.68f, 0.28f), 8f, 10f);
                }, "CRUCES DEL SENDERO", "«Una veladora le muestra el camino a las ánimas perdidas»", false));

                l.Add(T(t =>
                {
                    float e = Progreso(t);
                    Vector3 p = Orb(posCruz, 3.6f, AngDe(hj) + e * 0.85f, 1.25f);
                    return V(p, posCruz + arriba * 0.9f, 40f, 0.005f);
                }));
                break;
            }

            // ---------------------------------------------------------------- COMERCIANTE DON CHUY (2 líneas)
            case "comerciante":
            {
                Transform chuy = Buscar("Puesto_DonChuy") ?? Buscar("Comerciante_DonChuy") ?? Buscar("DonChuy");
                Vector3 posChuy = chuy != null ? chuy.position : (c.ruta(16f) - r * 5f);
                Vector3 hj = Plano(j - posChuy);

                l.Add(T(t =>
                {
                    float e = Progreso(t);
                    Vector3 p = posChuy + hj * Mathf.Lerp(8.5f, 4.6f, e) + r * Mathf.Lerp(-2f, 0.8f, e);
                    p.y = Suelo(p) + Mathf.Lerp(2.1f, 1.55f, e);
                    return V(p, posChuy + arriba * 1.35f, Mathf.Lerp(48f, 36f, e), 0.005f);
                }, () =>
                {
                    NuevaLuz(posChuy + arriba * 4f + hj * 2f, new Color(1f, 0.75f, 0.32f), 12f, 14f);
                }, "DON CHUY · MERCADER DE ÁNIMAS", "«Cambia tus almas por ayuda»", false));

                l.Add(T(t =>
                {
                    float e = Progreso(t);
                    Vector3 p = Orb(posChuy, 4.4f, AngDe(hj) + 0.2f + e * 0.65f, 1.55f);
                    return V(p, posChuy + arriba * 1.35f, 40f, 0.005f);
                }));
                break;
            }

            // ---------------------------------------------------------------- LA CUEVA VIEJA (2 líneas antes de despertar al jefe)
            case "cueva":
            {
                Transform cueva = Buscar("Cueva_La_Bufa") ?? (JefeLaBufa.Instancia != null ? JefeLaBufa.Instancia.transform : null);
                Vector3 posCueva = cueva != null ? cueva.position : c.ruta(25f);
                Vector3 hj = Plano(j - posCueva);

                l.Add(T(t =>
                {
                    float e = Progreso(t);
                    Vector3 p = posCueva + hj * Mathf.Lerp(18f, 10.5f, e);
                    p.y = Suelo(p) + Mathf.Lerp(3.0f, 1.9f, e);
                    return V(p, posCueva + arriba * 2.8f, Mathf.Lerp(50f, 38f, e), 0.008f);
                }, () =>
                {
                    NuevaLuz(posCueva + arriba * 6f + hj * 4f, new Color(1f, 0.62f, 0.25f), 16f, 26f, 75f, posCueva + arriba * 2.5f);
                }, "LA CUEVA VIEJA", "«Una joven que durmió siglos hecha piedra…»", false));

                l.Add(T(t =>
                {
                    float e = Progreso(t);
                    Vector3 p = Orb(posCueva, 11f, AngDe(hj) - 0.3f + e * 0.6f, 2.4f);
                    return V(p, posCueva + arriba * 2.6f, 44f, 0.008f);
                }));
                break;
            }

            // ---------------------------------------------------------------- LA BUFA DESPIERTA
            case "jefe_despierta":
            {
                JefeLaBufa jefe = JefeLaBufa.Instancia;
                if (jefe == null) return null;
                Vector3 bp = jefe.transform.position;
                Vector3 hj = Plano(j - bp);
                float a0 = AngDe(hj);
                string verso; string rot = TextoCarta("bufa", out verso);

                l.Add(T(t =>
                {
                    float e = Progreso(t);
                    Vector3 p = bp + hj * Mathf.Lerp(11f, 7.6f, e);
                    p.y = Suelo(p) + Mathf.Lerp(0.8f, 1.5f, e);
                    return V(p, bp + arriba * Mathf.Lerp(2.8f, 4.2f, e), Mathf.Lerp(58f, 44f, e), 0.014f + 0.07f * Mathf.Exp(-t * 0.5f));
                }, () =>
                {
                    NuevaLuz(bp + arriba * 5f + hj * 3f, new Color(1f, 0.65f, 0.25f), 14f, 24f, 80f, bp + arriba * 2.5f);
                    NuevaLuz(bp + hj * 5.5f + arriba * 1.2f, new Color(1f, 0.5f, 0.15f), 6f, 12f);
                }, rot, verso, true));

                l.Add(T(t =>
                {
                    float e = Progreso(t);
                    return V(Orb(bp, 11.5f, a0 - 0.35f + 0.9f * e, 2.2f + 3.6f * e), bp + arriba * Mathf.Lerp(3.2f, 4f, e), 50f, 0.012f);
                }));

                Vector3 lado = Vector3.Cross(Vector3.up, hj);
                l.Add(T(t =>
                {
                    float e = Progreso(t);
                    Vector3 p = j + hj * (3.4f - 1.6f * e) + lado * 0.9f;
                    p.y = Suelo(j) + 1.95f;
                    return V(p, bp + arriba * 3.2f, Mathf.Lerp(54f, 47f, e), 0.01f);
                }));
                break;
            }

            // ---------------------------------------------------------------- EL HECHIZO SE ROMPE
            case "jefe_derrotado":
            {
                JefeLaBufa jefe = JefeLaBufa.Instancia;
                if (jefe == null) return null;
                Vector3 bp = jefe.transform.position;
                Vector3 hj = Plano(j - bp);
                float a0 = AngDe(hj);
                Transform portal = Buscar("Portal_Victoria_Nivel1");

                l.Add(T(t =>
                {
                    float e = Progreso(t);
                    Vector3 p = bp + hj * Mathf.Lerp(9.5f, 7f, e);
                    p.y = Suelo(p) + 1.5f;
                    return V(p, bp + arriba * 2.8f, Mathf.Lerp(50f, 41f, e), 0.03f);
                }, () =>
                {
                    NuevaLuz(bp + arriba * 5f + hj * 2f, new Color(0.7f, 0.95f, 1f), 12f, 22f, 80f, bp + arriba * 2.5f);
                }, "EL HECHIZO SE ROMPE", "«Gracias… por no mirar atrás…»", false));

                l.Add(T(t =>
                {
                    float e = Progreso(t);
                    return V(Orb(bp, 9f + 3f * e, a0 + 1.2f * e, 2f + 9f * e), bp + arriba * (3f + 3f * e), Mathf.Lerp(56f, 46f, e), 0.008f);
                }));

                Vector3 meta = portal != null ? portal.position : bp;
                Vector3 hm = Plano(meta - j);
                Vector3 ladoM = Vector3.Cross(Vector3.up, hm);
                l.Add(T(t =>
                {
                    float e = Progreso(t);
                    Vector3 p = j - hm * 3.2f + ladoM * 1.0f + hm * (4.2f * e);
                    p.y = Suelo(j) + 1.9f;
                    return V(p, meta + arriba * 3f, Mathf.Lerp(54f, 44f, e));
                }));
                break;
            }
        }
        return l;
    }

    // ================================================================== UI

    void ConstruirUI()
    {
        Canvas canvas = UIMexicana.CrearCanvas("Cinematica_Narrativa", 54);
        canvas.transform.SetParent(transform, false);
        RectTransform raiz = (RectTransform)canvas.transform;

        barraSup = Barra("BarraSuperior", raiz, true);
        barraInf = Barra("BarraInferior", raiz, false);

        RectTransform rt = UIMexicana.Nuevo("Rotulo", raiz);
        UIMexicana.Anclar(rt, new Vector2(0f, 1f), new Vector2(90f, -150f), new Vector2(1100f, 150f));
        grupoRotulo = rt.gameObject.AddComponent<CanvasGroup>();
        grupoRotulo.alpha = 0f; grupoRotulo.blocksRaycasts = false; grupoRotulo.interactable = false;

        lineaRotulo = UIMexicana.Imagen("Linea", rt, UIMexicana.Blanco(), UIMexicana.ConAlfa(UIMexicana.Cempasuchil, 0.9f));
        UIMexicana.Anclar(lineaRotulo.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(6f, 112f));
        lineaRotulo.rectTransform.pivot = new Vector2(0f, 1f);

        txtRotulo = UIMexicana.Texto("Titulo", rt, "", 40, UIMexicana.Crema, TextAlignmentOptions.TopLeft, FontStyles.Bold, 5f);
        UIMexicana.Anclar(txtRotulo.rectTransform, new Vector2(0f, 1f), new Vector2(26f, 0f), new Vector2(1060f, 54f));
        txtRotulo.rectTransform.pivot = new Vector2(0f, 1f);
        txtRotulo.textWrappingMode = TextWrappingModes.NoWrap;

        txtSub = UIMexicana.Texto("Subtitulo", rt, "", 24, UIMexicana.ConAlfa(UIMexicana.Crema, 0.9f), TextAlignmentOptions.TopLeft, FontStyles.Normal, 1.5f);
        UIMexicana.Anclar(txtSub.rectTransform, new Vector2(0f, 1f), new Vector2(26f, -60f), new Vector2(1060f, 52f));
        txtSub.rectTransform.pivot = new Vector2(0f, 1f);
        txtSub.textWrappingMode = TextWrappingModes.NoWrap;

        RectTransform pista = UIMexicana.Nuevo("PistaSaltar", raiz);
        UIMexicana.Anclar(pista, new Vector2(1f, 0f), new Vector2(-34f, 40f), new Vector2(420f, 30f));
        pista.pivot = new Vector2(1f, 0f);
        grupoPista = pista.gameObject.AddComponent<CanvasGroup>();
        grupoPista.alpha = 0f; grupoPista.blocksRaycasts = false;
        TextMeshProUGUI tp = UIMexicana.Texto("Texto", pista, "ENTER  ·  saltar cinemática", 18, UIMexicana.Crema, TextAlignmentOptions.Right, FontStyles.Bold, 3f);
        UIMexicana.Estirar(tp.rectTransform);

        RectTransform fundido = UIMexicana.Nuevo("Fundido", raiz);
        UIMexicana.Estirar(fundido);
        Image negro = fundido.gameObject.AddComponent<Image>();
        negro.color = Color.black;
        negro.raycastTarget = false;
        grupoFundido = fundido.gameObject.AddComponent<CanvasGroup>();
        grupoFundido.alpha = 0f; grupoFundido.blocksRaycasts = false; grupoFundido.interactable = false;
        fundido.SetAsLastSibling();
    }

    static RectTransform Barra(string nombre, RectTransform padre, bool arriba)
    {
        RectTransform b = UIMexicana.Nuevo(nombre, padre);
        b.anchorMin = new Vector2(0f, arriba ? 1f : 0f);
        b.anchorMax = new Vector2(1f, arriba ? 1f : 0f);
        b.pivot = new Vector2(0.5f, arriba ? 1f : 0f);
        b.anchoredPosition = Vector2.zero;
        b.sizeDelta = new Vector2(0f, 0f);
        Image im = b.gameObject.AddComponent<Image>();
        im.color = new Color(0.01f, 0.01f, 0.03f, 1f);
        im.raycastTarget = false;

        RectTransform fil = UIMexicana.Nuevo("Filete", b);
        fil.anchorMin = new Vector2(0f, arriba ? 0f : 1f);
        fil.anchorMax = new Vector2(1f, arriba ? 0f : 1f);
        fil.pivot = new Vector2(0.5f, arriba ? 1f : 0f);
        fil.anchoredPosition = Vector2.zero;
        fil.sizeDelta = new Vector2(0f, 2f);
        Image fi = fil.gameObject.AddComponent<Image>();
        fi.color = UIMexicana.ConAlfa(UIMexicana.Dorado, 0.55f);
        fi.raycastTarget = false;
        return b;
    }
}
