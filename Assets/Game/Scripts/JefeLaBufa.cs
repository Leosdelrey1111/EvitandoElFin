using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Jefe del Nivel 1: LA BUFA, la joven convertida en piedra junto a la Cueva Vieja del Cerro de La Bufa.
///
/// Leyenda: quien la cargue cuesta abajo sin mirar atrás podría romper el hechizo, pero a mitad del camino ella se
/// vuelve serpiente y todos voltean. De ahí salen sus dos fases:
///  - FASE 1 (piedra):  Lluvia de rocas · Embestida pétrea (queda aturdida al chocar) · invoca a sus pretendientes de piedra.
///  - FASE 2 (serpiente, al 50%): Mordida en salto · Onda de choque (se esquiva SALTANDO) · invoca serpientes.
/// Todos los ataques tienen telegrafiado de 1.2 s (1.0 s en fase 2) con marcas rojas en el suelo.
///
/// Todo se construye por código (modelo, arena, barra de vida, sonidos): basta con colocar este componente en el centro de la arena.
/// Duerme como una estatua hasta que el Chofer reúne los tótems y se acerca.
/// </summary>
[DefaultExecutionOrder(10)]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PoderEspiritual))]
public class JefeLaBufa : MonoBehaviour
{
    public static JefeLaBufa Instancia { get; private set; }

    public enum Estado { Dormida, Despertando, Combate, Transformando, Muriendo, Derrotada }

    [Header("Arena")]
    public float radioArena = 14f;
    public float radioDespertar = 11f;
    public float alturaPilares = 7f;

    [Header("Vida")]
    public float vidaMax = 700f;

    [Header("Daño")]
    public float danioRocas = 18f;
    public float danioEmbestida = 28f;
    public float danioMordida = 26f;
    public float danioOnda = 20f;

    [Header("Telegrafiado")]
    public float tiempoTelegrafiado = 1.2f;

    public Estado EstadoActual { get { return estado; } }
    public bool Derrotada { get { return estado == Estado.Derrotada; } }

    // ------------------------------------------------------------------ referencias y estado
    Estado estado = Estado.Dormida;
    Vector3 centro;
    Transform jugador;
    PoderEspiritual vidaJugador;
    CharacterController ccJugador;
    CharacterController cc;
    PoderEspiritual vida;
    float velY;
    bool fase2, telegrafiando, aturdida, tocoBorde;
    bool invoco75, invoco25, avisoFinal;
    float despertarK = 0f;         // 0 = dormida, 1 = despierta (brillo de grietas y ojos)
    float esperaNarrador = 0f;     // segundos que lleva esperando a que Tonalli termine de hablar antes de despertar
    float flashHasta;
    float escalaVisual = 1f;
    readonly List<GameObject> temporales = new List<GameObject>();
    readonly List<EsbirroBaseIA> invocados = new List<EsbirroBaseIA>();

    // ------------------------------------------------------------------ visual
    Transform visualRoot;
    GameObject piedraRoot, serpRoot;
    Material matGrieta, matOjos;
    Light luzCuerpo;
    Transform serpCabeza;
    Transform[] serpSegs;
    float[] serpRadio;
    GameObject raizPilares;
    readonly List<Transform> pilares = new List<Transform>();
    readonly List<float> pilaresYFinal = new List<float>();
    readonly List<float> pilaresYOculto = new List<float>();

    static readonly Color Ambar = new Color(1f, 0.62f, 0.11f);
    static readonly Color Cian = new Color(0.3f, 0.95f, 1f);
    static readonly Color Rojo = new Color(1f, 0.15f, 0.1f);
    static Shader shaderLit;
    static readonly Dictionary<string, Material> cacheMat = new Dictionary<string, Material>();

    // ------------------------------------------------------------------ UI y audio
    CanvasGroup grupoUI;
    RectTransform rellenoVida, rellenoRetraso;
    Image imgRelleno;
    TextMeshProUGUI txtNombre;
    float pMostrada = 1f, pRetraso = 1f;
    AudioSource sfx, musica;
    float volumenMusica;

    // ================================================================== CICLO DE VIDA

    void Awake()
    {
        Instancia = this;
        cc = GetComponent<CharacterController>();
        cc.height = 4.4f; cc.radius = 1.3f; cc.center = new Vector3(0f, 2.2f, 0f); cc.stepOffset = 0.5f;

        vida = GetComponent<PoderEspiritual>();
        vida.maxPoder = vidaMax;
        vida.actualPoder = vidaMax;
        vida.invulnerable = true;
        vida.OnMuerte += AlMorir;
        vida.OnRecibioDanio += AlRecibirDanio;

        ConstruirVisual();
        ConstruirUI();

        sfx = gameObject.AddComponent<AudioSource>();
        sfx.spatialBlend = 0.5f; sfx.maxDistance = 70f; sfx.rolloffMode = AudioRolloffMode.Linear; sfx.playOnAwake = false;
        musica = gameObject.AddComponent<AudioSource>();
        musica.spatialBlend = 0f; musica.loop = true; musica.playOnAwake = false; musica.volume = 0f;
    }

    void Start()
    {
        // Se posa sobre el suelo del centro de la arena
        Vector3 s;
        cc.enabled = false;
        if (UtilTerreno.SueloEn(transform.position.x, transform.position.z, out s)) transform.position = s;
        cc.enabled = true;
        centro = transform.position;

        BuscarJugador();

        gameObject.tag = "Untagged"; // dormida: no es blanco de los golpes
        if (GameManager.Instancia != null)
        {
            GameManager.Instancia.jefeRequerido = true;
            GameManager.Instancia.CambiarEstadoJefe(GameManager.EstadoJefe.Dormido);
        }
    }

    void BuscarJugador()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) return;
        jugador = p.transform;
        vidaJugador = p.GetComponent<PoderEspiritual>();
        ccJugador = p.GetComponent<CharacterController>();
        // el jefe no choca con el Chofer (si no, la embestida se frenaría contra él)
        if (ccJugador != null && cc != null) Physics.IgnoreCollision(cc, ccJugador);
    }

    void OnDestroy()
    {
        if (vida != null) { vida.OnMuerte -= AlMorir; vida.OnRecibioDanio -= AlRecibirDanio; }
        if (Instancia == this) Instancia = null;
    }

    void Update()
    {
        if (estado == Estado.Muriendo || estado == Estado.Derrotada) { ActualizarMusica(); return; }

        AplicarGravedad();
        ActualizarMusica();

        if (jugador == null) { BuscarJugador(); return; }
        if (estado == Estado.Dormida) ComprobarDespertar();
    }

    void AplicarGravedad()
    {
        if (!cc.enabled) return;
        if (cc.isGrounded && velY < 0f) velY = -2f;
        velY += -20f * Time.deltaTime;
        cc.Move(Vector3.up * velY * Time.deltaTime);
    }

    void ComprobarDespertar()
    {
        if (GameManager.Instancia == null || !GameManager.Instancia.TotemsCompletos()) return;
        if (DistPlano(jugador.position, centro) >= radioDespertar) return;

        // la historia manda: La Bufa espera a que Tonalli termine de contar lo de la Cueva Vieja (máximo 15 s)
        if (GuiaAlebrije.Instancia != null && GuiaAlebrije.Instancia.Ocupado && esperaNarrador < 15f)
        {
            esperaNarrador += Time.deltaTime;
            return;
        }
        StartCoroutine(Despertar());
    }

    void LateUpdate()
    {
        AnimarVisual();
        ActualizarCola();
        ActualizarUI();
    }

    // ================================================================== DESPERTAR

    IEnumerator Despertar()
    {
        estado = Estado.Despertando;
        if (GameManager.Instancia != null) GameManager.Instancia.CambiarEstadoJefe(GameManager.EstadoJefe.Combate);

        CodiceHistoria.Registrar("bestia_bufa");
        Sonar(ClipRugido(), 1f);
        Decir(HistoriaNivel1.BufaDespierta, "bufa_despierta", 4f, false, "jefe_despierta");
        StartCoroutine(TonalliDespues("despierta", 4.2f));
        CamaraTerceraPersona.Sacudir(0.3f);
        StartCoroutine(LevantarPilares(true));
        IniciarMusica();

        float t = 0f, dur = 3.2f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = t / dur;
            despertarK = k;
            escalaVisual = Mathf.Lerp(0.93f, 1f, k);
            if (Random.value < 0.06f) { CamaraTerceraPersona.Sacudir(0.12f); Polvo(centro + Random.insideUnitSphere * 2f, 1.5f); }
            yield return null;
        }
        despertarK = 1f;
        escalaVisual = 1f;

        // la cinemática de su despertar manda: el combate empieza cuando termina (o si se salta con ENTER)
        float espera = 0f;
        while (CinematicaNarrativa.Activa && espera < 45f) { espera += Time.deltaTime; yield return null; }

        gameObject.tag = "Enemigo";
        vida.invulnerable = false;
        estado = Estado.Combate;
        grupoUI.alpha = Mathf.Max(grupoUI.alpha, 0.01f);
        StartCoroutine(BucleCombate());
    }

    // ================================================================== BUCLE DE COMBATE

    IEnumerator BucleCombate()
    {
        int ultimo = -1;
        while (estado == Estado.Combate)
        {
            if (vidaJugador != null && vidaJugador.EstaMuerto) { yield return null; continue; }

            float p = vida.Porcentaje;
            if (!fase2 && p <= 0.5f) { yield return Transformar(); continue; }
            if (!invoco75 && p <= 0.75f) { invoco75 = true; yield return InvocarPiedra(); continue; }
            if (fase2 && !invoco25 && p <= 0.25f) { invoco25 = true; yield return InvocarSerpientes(3); continue; }
            if (!avisoFinal && p <= 0.2f)
            {
                avisoFinal = true;
                if (GuiaAlebrije.Instancia != null) GuiaAlebrije.Instancia.Jefe("final");
            }

            yield return Perseguir(fase2 ? 0.8f : 1.5f);
            if (estado != Estado.Combate) yield break;

            int a = ElegirAtaque(ultimo);
            ultimo = a;
            switch (a)
            {
                case 0: yield return LluviaRocas(); break;
                case 1: yield return Embestida(); break;
                case 2: yield return Mordida(); break;
                default: yield return OndaChoque(); break;
            }
        }
    }

    int ElegirAtaque(int ultimo)
    {
        float[] w = fase2 ? new float[] { 2f, 2f, 3f, 2f } : new float[] { 3f, 3f, 0f, 0f };
        if (ultimo >= 0 && ultimo < w.Length) w[ultimo] = 0f;
        float total = 0f;
        foreach (float x in w) total += x;
        float r = Random.value * total;
        for (int i = 0; i < w.Length; i++)
        {
            if (r < w[i]) return i;
            r -= w[i];
        }
        return 0;
    }

    IEnumerator Perseguir(float segundos)
    {
        float t = 0f;
        float vel = fase2 ? 4.8f : 2.8f;
        float distDeseada = fase2 ? 4f : 5f;
        while (t < segundos && estado == Estado.Combate)
        {
            t += Time.deltaTime;
            Vector3 h = Plano(jugador.position - transform.position);
            MirarHacia(h, 5f);
            if (h.magnitude > distDeseada) Mover(h.normalized * vel * Time.deltaTime);
            yield return null;
        }
    }

    // ================================================================== ATAQUE 1: LLUVIA DE ROCAS

    IEnumerator LluviaRocas()
    {
        int n = fase2 ? 9 : 6;
        float espera = fase2 ? 1.0f : tiempoTelegrafiado;
        Sonar(ClipAviso(), 0.7f);

        var marcas = new Marcador[n];
        var puntos = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            Vector2 off = i == 0 ? Vector2.zero : Random.insideUnitCircle * 7f;
            Vector3 p = jugador.position + new Vector3(off.x, 0f, off.y);
            p = LimitarAArena(p);
            puntos[i] = new Vector3(p.x, SueloY(p), p.z);
            marcas[i] = CrearMarcador(puntos[i], 2.4f, Rojo);
        }

        telegrafiando = true;
        float t = 0f;
        while (t < espera && estado == Estado.Combate)
        {
            t += Time.deltaTime;
            for (int i = 0; i < n; i++) marcas[i].Progreso(t / espera);
            MirarHacia(Plano(jugador.position - transform.position), 4f);
            yield return null;
        }
        telegrafiando = false;

        for (int i = 0; i < n; i++) StartCoroutine(CaerRoca(puntos[i], marcas[i], i * 0.08f));
        yield return new WaitForSeconds(0.2f + n * 0.08f + 0.9f);
    }

    IEnumerator CaerRoca(Vector3 pos, Marcador marca, float retraso)
    {
        yield return new WaitForSeconds(retraso);

        GameObject roca = GameObject.CreatePrimitive(Random.value < 0.5f ? PrimitiveType.Cube : PrimitiveType.Sphere);
        roca.name = "RocaJefe";
        QuitarCollider(roca);
        roca.transform.localScale = new Vector3(Random.Range(1.4f, 2.2f), Random.Range(1.4f, 2.2f), Random.Range(1.4f, 2.2f));
        roca.transform.rotation = Random.rotation;
        roca.GetComponent<Renderer>().sharedMaterial = Mat("roca_jefe", new Color(0.36f, 0.34f, 0.33f), 0.1f);
        Temp(roca);

        float dur = 0.28f, t = 0f, altura = 24f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = t / dur;
            roca.transform.position = pos + Vector3.up * (altura * (1f - k * k) + 0.8f);
            roca.transform.Rotate(260f * Time.deltaTime, 180f * Time.deltaTime, 0f);
            yield return null;
        }
        roca.transform.position = pos + Vector3.up * 0.6f;
        if (marca != null) marca.Destruir();

        GolpeEnRadio(pos, 2.4f, danioRocas);
        Polvo(pos, 2.2f);
        Sonar(ClipImpacto(), 0.8f);
        CamaraTerceraPersona.Sacudir(0.1f);

        // la roca se queda un momento y se hunde
        yield return new WaitForSeconds(3f);
        float h = 0f;
        while (h < 1f && roca != null)
        {
            h += Time.deltaTime * 1.2f;
            roca.transform.position += Vector3.down * 1.4f * Time.deltaTime;
            yield return null;
        }
        if (roca != null) Destroy(roca);
    }

    // ================================================================== ATAQUE 2: EMBESTIDA PÉTREA

    IEnumerator Embestida()
    {
        float espera = fase2 ? 1.0f : tiempoTelegrafiado;
        Vector3 dir = Plano(jugador.position - transform.position).normalized;
        float largo = Mathf.Clamp(DistPlano(jugador.position, transform.position) + 6f, 12f, radioArena * 1.7f);

        Indicador ind = CrearIndicador(new Vector3(transform.position.x, SueloY(transform.position), transform.position.z), dir, largo, 3.2f, Rojo);
        Sonar(ClipAviso(), 0.8f);
        telegrafiando = true;

        float t = 0f;
        while (t < espera && estado == Estado.Combate)
        {
            t += Time.deltaTime;
            if (t < espera * 0.65f)
            {
                Vector3 h = Plano(jugador.position - transform.position);
                if (h.sqrMagnitude > 0.01f) dir = Vector3.Slerp(dir, h.normalized, 6f * Time.deltaTime);
                ind.Orientar(dir);
            }
            MirarHacia(dir, 12f);
            ind.Progreso(t / espera);
            yield return null;
        }
        telegrafiando = false;
        ind.Destruir();
        if (estado != Estado.Combate) yield break;

        // arremetida
        Sonar(ClipRugido(), 0.6f);
        float recorrido = 0f, vel = 26f;
        bool golpe = false;
        int bloqueado = 0;
        tocoBorde = false;
        while (recorrido < largo && !tocoBorde && bloqueado < 3 && estado == Estado.Combate)
        {
            float paso = vel * Time.deltaTime;
            Vector3 antes = transform.position;
            MirarHacia(dir, 30f);
            Mover(dir * paso);
            float avanzado = Plano(transform.position - antes).magnitude;
            recorrido += paso;
            bloqueado = avanzado < paso * 0.25f ? bloqueado + 1 : 0;

            if (!golpe && DistPlano(jugador.position, transform.position) < 2.7f && Mathf.Abs(jugador.position.y - transform.position.y) < 3.5f)
            {
                golpe = true;
                vidaJugador.RecibirDanio(danioEmbestida);
                CamaraTerceraPersona.Sacudir(0.25f);
            }
            if (Random.value < 0.5f) Polvo(transform.position + Random.insideUnitSphere * 0.6f, 0.9f);
            yield return null;
        }

        // choque: queda aturdida (ventana para golpearla)
        CamaraTerceraPersona.Sacudir(0.35f);
        Polvo(transform.position + transform.forward * 1.4f, 3f);
        Sonar(ClipImpacto(), 1f);
        yield return Aturdir(2.2f);
    }

    IEnumerator Aturdir(float segundos)
    {
        aturdida = true;
        AvisosJuego.Mostrar("¡Está aturdida! ¡Golpéala!", 1.8f);
        float t = 0f;
        while (t < segundos && estado == Estado.Combate)
        {
            t += Time.deltaTime;
            if (Random.value < 0.15f) Chispas(transform.position + Vector3.up * Random.Range(1f, 4f) + Random.insideUnitSphere);
            yield return null;
        }
        aturdida = false;
    }

    // ================================================================== ATAQUE 3 (FASE 2): MORDIDA EN SALTO

    IEnumerator Mordida()
    {
        float espera = 1.0f;
        Vector3 objetivo = jugador.position;
        Marcador marca = CrearMarcador(new Vector3(objetivo.x, SueloY(objetivo), objetivo.z), 3.6f, Rojo);
        Sonar(ClipAviso(), 0.8f);
        telegrafiando = true;

        float t = 0f;
        while (t < espera && estado == Estado.Combate)
        {
            t += Time.deltaTime;
            if (t < espera * 0.6f)
            {
                objetivo = LimitarAArena(jugador.position);
                marca.Mover(new Vector3(objetivo.x, SueloY(objetivo), objetivo.z));
            }
            marca.Progreso(t / espera);
            MirarHacia(Plano(objetivo - transform.position), 8f);
            yield return null;
        }
        telegrafiando = false;
        if (estado != Estado.Combate) { marca.Destruir(); yield break; }

        // salto en arco
        Vector3 ini = transform.position;
        Vector3 fin = new Vector3(objetivo.x, SueloY(objetivo), objetivo.z);
        cc.enabled = false;
        float dur = 0.65f, k = 0f;
        Sonar(ClipRugido(), 0.5f);
        while (k < 1f && estado == Estado.Combate)
        {
            k += Time.deltaTime / dur;
            Vector3 p = Vector3.Lerp(ini, fin, Mathf.Clamp01(k));
            p.y += Mathf.Sin(Mathf.PI * Mathf.Clamp01(k)) * 4f;
            transform.position = p;
            MirarHacia(Plano(fin - ini), 20f);
            yield return null;
        }
        transform.position = fin;
        cc.enabled = true;
        velY = 0f;

        marca.Destruir();
        GolpeEnRadio(fin, 3.6f, danioMordida);
        Polvo(fin, 3.4f);
        Sonar(ClipImpacto(), 1f);
        CamaraTerceraPersona.Sacudir(0.3f);
        yield return new WaitForSeconds(0.7f);
    }

    // ================================================================== ATAQUE 4 (FASE 2): ONDA DE CHOQUE

    IEnumerator OndaChoque()
    {
        float espera = 1.0f;
        Marcador marca = CrearMarcador(new Vector3(transform.position.x, SueloY(transform.position), transform.position.z), 4f, Rojo);
        Sonar(ClipAviso(), 0.9f);
        AvisosJuego.Mostrar("¡SALTA cuando llegue la onda!", 1.8f);
        telegrafiando = true;

        float t = 0f;
        while (t < espera && estado == Estado.Combate)
        {
            t += Time.deltaTime;
            marca.Progreso(t / espera);
            marca.Mover(new Vector3(transform.position.x, SueloY(transform.position), transform.position.z));
            yield return null;
        }
        telegrafiando = false;
        marca.Destruir();
        if (estado != Estado.Combate) yield break;

        // el golpe al suelo
        CamaraTerceraPersona.Sacudir(0.4f);
        Sonar(ClipImpacto(), 1f);
        Polvo(transform.position, 4f);

        LineRenderer anillo = CrearAnillo();
        Vector3 origen = transform.position;
        float suelo = SueloY(origen);
        float radio = 2f, maxRadio = radioArena + 3f, vel = 12f;
        bool golpeado = false;
        while (radio < maxRadio && estado == Estado.Combate)
        {
            radio += vel * Time.deltaTime;
            DibujarAnillo(anillo, origen, suelo + 0.3f, radio);

            if (!golpeado && vidaJugador != null && !vidaJugador.EstaMuerto)
            {
                float d = DistPlano(jugador.position, origen);
                float pies = (jugador.position.y - 1f) - SueloY(jugador.position);   // el pivote del Chofer está a la altura de la cintura
                if (Mathf.Abs(d - radio) < 0.9f && pies < 0.45f)
                {
                    golpeado = true;
                    vidaJugador.RecibirDanio(danioOnda);
                    CamaraTerceraPersona.Sacudir(0.2f);
                }
            }
            yield return null;
        }
        if (anillo != null) Destroy(anillo.gameObject);
        yield return new WaitForSeconds(0.5f);
    }

    // ================================================================== INVOCACIONES

    IEnumerator InvocarPiedra()
    {
        bool narradorLibre = GuiaAlebrije.Instancia == null || !GuiaAlebrije.Instancia.Ocupado;
        Decir(HistoriaNivel1.BufaInvocaPiedra, "bufa_invoca_piedra", 3f, true);
        if (narradorLibre) StartCoroutine(TonalliDespues("invoca", 3.2f));   // si estaba ocupada, llegaría tarde: se omite
        Sonar(ClipRugido(), 0.8f);
        CamaraTerceraPersona.Sacudir(0.2f);
        telegrafiando = true;
        yield return new WaitForSeconds(0.8f);
        telegrafiando = false;

        for (int i = 0; i < 2; i++)
        {
            Vector3 p = PuntoEnArena(i * Mathf.PI + Random.Range(-0.5f, 0.5f), Random.Range(6f, 9f));
            Polvo(p, 3f);
            EsbirroBaseIA e = FabricaEsbirros.Crear(EsbirroBaseIA.TipoEsbirro.HombrePiedra, p + Vector3.up * 0.2f);
            e.rangoDeteccion = 80f;
            invocados.Add(e);
            yield return new WaitForSeconds(0.5f);
        }
        yield return new WaitForSeconds(0.8f);
    }

    IEnumerator InvocarSerpientes(int cantidad)
    {
        Decir(HistoriaNivel1.BufaInvocaSerpientes, "bufa_invoca_serpientes", 3f, true);
        Sonar(ClipRugido(), 0.7f);
        telegrafiando = true;
        yield return new WaitForSeconds(0.7f);
        telegrafiando = false;

        for (int i = 0; i < cantidad; i++)
        {
            Vector3 p = PuntoEnArena(i * (Mathf.PI * 2f / cantidad) + Random.Range(-0.3f, 0.3f), Random.Range(6f, 10f));
            Polvo(p, 2f);
            EsbirroBaseIA e = FabricaEsbirros.Crear(EsbirroBaseIA.TipoEsbirro.Serpiente, p + Vector3.up * 0.2f);
            e.rangoDeteccion = 80f;
            invocados.Add(e);
            yield return new WaitForSeconds(0.35f);
        }
        yield return new WaitForSeconds(0.6f);
    }

    // ================================================================== TRANSFORMACIÓN (FASE 2)

    IEnumerator Transformar()
    {
        estado = Estado.Transformando;
        vida.invulnerable = true;
        Sonar(ClipRugido(), 1f);
        Decir(HistoriaNivel1.BufaFase2, "bufa_fase2", 3.5f);
        StartCoroutine(TonalliDespues("fase2", 3.6f));

        telegrafiando = true;
        float t = 0f, dur = 2.4f;
        while (t < dur)
        {
            t += Time.deltaTime;
            CamaraTerceraPersona.Sacudir(0.12f);
            if (Random.value < 0.35f) { Chispas(transform.position + Vector3.up * Random.Range(0.5f, 4.5f) + Random.insideUnitSphere); Polvo(transform.position + Random.insideUnitSphere * 1.5f, 1.2f); }
            yield return null;
        }
        telegrafiando = false;

        // la piedra estalla y aparece la serpiente
        Romper(piedraRoot.transform, 14);
        piedraRoot.SetActive(false);
        serpRoot.SetActive(true);
        ReiniciarCola();
        fase2 = true;
        matGrieta.color = Ambar;
        if (luzCuerpo != null) luzCuerpo.color = new Color(0.4f, 1f, 0.5f);
        CamaraTerceraPersona.Sacudir(0.45f);
        Polvo(transform.position, 5f);
        Sonar(ClipImpacto(), 1f);
        StartCoroutine(Destello(1.0f));
        ActualizarNombre();

        float pop = 0f;
        while (pop < 0.5f)
        {
            pop += Time.deltaTime;
            escalaVisual = Mathf.Lerp(0.5f, 1f, pop / 0.5f);
            yield return null;
        }
        escalaVisual = 1f;

        vida.invulnerable = false;
        estado = Estado.Combate;
        yield return InvocarSerpientes(3);
    }

    IEnumerator Destello(float seg)
    {
        if (luzCuerpo == null) yield break;
        float baseI = luzCuerpo.intensity;
        luzCuerpo.intensity = baseI + 14f;
        float t = 0f;
        while (t < seg)
        {
            t += Time.deltaTime;
            luzCuerpo.intensity = Mathf.Lerp(baseI + 14f, baseI, t / seg);
            yield return null;
        }
        luzCuerpo.intensity = baseI;
    }

    // ================================================================== DAÑO RECIBIDO Y MUERTE

    void AlRecibirDanio(float cantidad)
    {
        flashHasta = Time.time + 0.12f;
    }

    void AlMorir()
    {
        if (estado == Estado.Muriendo || estado == Estado.Derrotada) return;
        StopAllCoroutines();
        StartCoroutine(Muerte());
    }

    IEnumerator Muerte()
    {
        estado = Estado.Muriendo;
        gameObject.tag = "Untagged";
        vida.invulnerable = true;
        telegrafiando = false; aturdida = false;
        if (cc != null && !cc.enabled) cc.enabled = true;

        foreach (GameObject g in temporales) if (g != null) Destroy(g);
        temporales.Clear();
        foreach (EsbirroBaseIA e in invocados)
            if (e != null && !e.EstaMuerto) { PoderEspiritual pe = e.GetComponent<PoderEspiritual>(); if (pe != null) pe.RecibirDanio(99999f); }
        invocados.Clear();

        Sonar(ClipRugido(), 1f);
        Decir(HistoriaNivel1.BufaDerrotada, "bufa_derrotada", 5f, false, "jefe_derrotado");
        StartCoroutine(TonalliDespues("derrotado", 4.2f));

        // colapso: se agrieta, tiembla y brilla
        float t = 0f, dur = 3.2f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = t / dur;
            despertarK = 1f + k * 2.5f;
            escalaVisual = 1f + Mathf.Sin(t * 40f) * 0.015f * (0.4f + k);
            CamaraTerceraPersona.Sacudir(0.08f + 0.2f * k);
            if (Random.value < 0.4f) Chispas(transform.position + Vector3.up * Random.Range(0.5f, 4.5f) + Random.insideUnitSphere * 1.2f);
            yield return null;
        }

        // estallido
        Transform activo = fase2 ? serpRoot.transform : piedraRoot.transform;
        Romper(activo, 26);
        activo.gameObject.SetActive(false);
        if (luzCuerpo != null) luzCuerpo.enabled = false;
        CamaraTerceraPersona.Sacudir(0.5f);
        Polvo(transform.position + Vector3.up, 6f);
        Sonar(ClipImpacto(), 1f);
        StartCoroutine(FlashLuz(transform.position + Vector3.up * 2.5f, 1.4f));

        AlmaRecolectable.CrearGrupo(transform.position + Vector3.up * 1.2f, 30, true);
        StartCoroutine(LiberarJoven());
        StartCoroutine(LevantarPilares(false));

        // el hechizo se rompe: se abre el portal
        if (GameManager.Instancia != null) GameManager.Instancia.CambiarEstadoJefe(GameManager.EstadoJefe.Derrotado);
        AvisosJuego.Mostrar("¡El hechizo se rompió! El portal espiritual se ha abierto.", 5f);

        yield return new WaitForSeconds(4.5f);
        if (GuiaAlebrije.Instancia != null) GuiaAlebrije.Instancia.MostrarRecuerdo(HistoriaNivel1.TituloRecuerdo, HistoriaNivel1.TextoRecuerdo);

        estado = Estado.Derrotada;
        cc.enabled = false;
    }

    IEnumerator FlashLuz(Vector3 pos, float seg)
    {
        GameObject go = new GameObject("FlashJefe");
        go.transform.position = pos;
        Light l = go.AddComponent<Light>();
        l.type = LightType.Point; l.color = new Color(1f, 0.95f, 0.8f); l.range = 40f; l.intensity = 30f; l.shadows = LightShadows.None;
        float t = 0f;
        while (t < seg)
        {
            t += Time.deltaTime;
            l.intensity = Mathf.Lerp(30f, 0f, t / seg);
            yield return null;
        }
        Destroy(go);
    }

    /// <summary>El alma de la joven, ya libre, sube al cielo entre pétalos de cempasúchil.</summary>
    IEnumerator LiberarJoven()
    {
        GameObject orbe = new GameObject("AlmaDeLaJoven");
        orbe.transform.position = centro + Vector3.up * 1.5f;
        Shader spr = Shader.Find("Sprites/Default");

        GameObject halo = new GameObject("Halo");
        halo.transform.SetParent(orbe.transform, false);
        SpriteRenderer sr = halo.AddComponent<SpriteRenderer>();
        sr.sprite = UIMexicana.Brillo();
        sr.color = new Color(1f, 0.95f, 0.75f, 0.9f);
        halo.transform.localScale = Vector3.one * 5f;
        MirarCamara mc = halo.AddComponent<MirarCamara>();

        Light l = orbe.AddComponent<Light>();
        l.type = LightType.Point; l.color = new Color(1f, 0.9f, 0.6f); l.range = 22f; l.intensity = 6f; l.shadows = LightShadows.None;

        ParticleSystem ps = orbe.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true; main.startLifetime = 3.5f; main.startSpeed = 1.2f; main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.62f, 0.11f), new Color(1f, 0.85f, 0.3f));
        main.gravityModifier = -0.05f; main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission; em.rateOverTime = 28f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 1.2f;
        var pr = orbe.GetComponent<ParticleSystemRenderer>();
        if (spr != null)
        {
            Material m = new Material(spr);
            m.mainTexture = UIMexicana.Petalo().texture;
            pr.sharedMaterial = m;
        }
        ps.Play();

        float t = 0f, dur = 9f;
        Vector3 ini = orbe.transform.position;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = t / dur;
            orbe.transform.position = ini + Vector3.up * (k * k * 22f + Mathf.Sin(t * 2f) * 0.2f);
            float a = k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f;
            sr.color = new Color(1f, 0.95f, 0.75f, 0.9f * a);
            l.intensity = 6f * a;
            yield return null;
        }
        Destroy(orbe);
    }

    // ================================================================== ARENA (pilares)

    IEnumerator LevantarPilares(bool subir)
    {
        if (subir && pilares.Count == 0) CrearPilares();
        if (pilares.Count == 0) yield break;

        float dur = 2.2f, t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            for (int i = 0; i < pilares.Count; i++)
            {
                if (pilares[i] == null) continue;
                float retraso = (i % 12) * 0.05f;
                float k = Mathf.Clamp01((t - retraso) / (dur - 0.6f));
                k = k * k * (3f - 2f * k);
                float desde = subir ? pilaresYOculto[i] : pilaresYFinal[i];
                float hasta = subir ? pilaresYFinal[i] : pilaresYOculto[i];
                Vector3 p = pilares[i].position; p.y = Mathf.Lerp(desde, hasta, k);
                pilares[i].position = p;
            }
            if (subir) CamaraTerceraPersona.Sacudir(0.07f);
            yield return null;
        }

        if (!subir)
        {
            foreach (Transform p in pilares) if (p != null) Destroy(p.gameObject);
            pilares.Clear(); pilaresYFinal.Clear(); pilaresYOculto.Clear();
            if (raizPilares != null) Destroy(raizPilares);
        }
    }

    void CrearPilares()
    {
        Material roca = Mat("pilar_arena", new Color(0.30f, 0.29f, 0.30f), 0.1f);
        Material runa = MatEmisivo(new Color(0.3f, 0.95f, 1f), new Color(0.4f, 2.2f, 2.6f));
        GameObject raiz = new GameObject("PilaresArena");
        raizPilares = raiz;

        int n = 38;
        for (int i = 0; i < n; i++)
        {
            float ang = (i / (float)n) * Mathf.PI * 2f;
            Vector3 dirRad = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
            Vector3 pos = centro + dirRad * (radioArena + 1.6f);
            float suelo = SueloY(pos);
            float alto = alturaPilares * Random.Range(0.8f, 1.25f);

            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = "PilarArena" + i;
            g.transform.SetParent(raiz.transform, true);
            g.transform.localScale = new Vector3(3.6f, alto, 2.4f);
            g.transform.rotation = Quaternion.LookRotation(-dirRad) * Quaternion.Euler(0f, Random.Range(-8f, 8f), 0f);
            float yOculto = suelo - alto * 0.5f - 0.3f;
            float yFinal = suelo + alto * 0.5f - 0.9f;
            g.transform.position = new Vector3(pos.x, yOculto, pos.z);
            g.GetComponent<Renderer>().sharedMaterial = roca;

            GameObject rn = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rn.name = "Runa";
            QuitarCollider(rn);
            rn.transform.SetParent(g.transform, false);
            rn.transform.localPosition = new Vector3(0f, 0.28f, 0.505f);
            rn.transform.localScale = new Vector3(0.28f, 0.06f, 0.03f);
            rn.GetComponent<Renderer>().sharedMaterial = runa;

            pilares.Add(g.transform);
            pilaresYFinal.Add(yFinal);
            pilaresYOculto.Add(yOculto);
        }
    }

    // ================================================================== MOVIMIENTO Y UTILIDADES DE JUEGO

    void Mover(Vector3 delta)
    {
        delta.y = 0f;
        if (!cc.enabled) return;
        Vector3 np = transform.position + delta;
        Vector3 off = np - centro; off.y = 0f;
        float lim = radioArena - 1.8f;
        if (off.magnitude > lim)
        {
            off = off.normalized * lim;
            delta = new Vector3(centro.x + off.x, transform.position.y, centro.z + off.z) - transform.position;
            tocoBorde = true;
        }
        cc.Move(delta);
    }

    void MirarHacia(Vector3 dir, float velocidad)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir.normalized), velocidad * Time.deltaTime);
    }

    static Vector3 Plano(Vector3 v) { v.y = 0f; return v; }
    static float DistPlano(Vector3 a, Vector3 b) { a.y = 0f; b.y = 0f; return Vector3.Distance(a, b); }

    float SueloY(Vector3 p)
    {
        Vector3 s;
        return UtilTerreno.SueloEn(p.x, p.z, out s) ? s.y : centro.y;
    }

    Vector3 LimitarAArena(Vector3 p)
    {
        Vector3 off = p - centro; off.y = 0f;
        float lim = radioArena - 1f;
        if (off.magnitude > lim) off = off.normalized * lim;
        return new Vector3(centro.x + off.x, p.y, centro.z + off.z);
    }

    Vector3 PuntoEnArena(float ang, float radio)
    {
        Vector3 p = centro + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * radio;
        return new Vector3(p.x, SueloY(p), p.z);
    }

    void GolpeEnRadio(Vector3 punto, float radio, float danio)
    {
        if (jugador == null || vidaJugador == null || vidaJugador.EstaMuerto) return;
        if (DistPlano(jugador.position, punto) > radio) return;
        if (Mathf.Abs(jugador.position.y - 1f - punto.y) > 3f) return;
        vidaJugador.RecibirDanio(danio);
        CamaraTerceraPersona.Sacudir(0.18f);
    }

    void Temp(GameObject g) { temporales.Add(g); }

    static void QuitarCollider(GameObject g)
    {
        Collider c = g.GetComponent<Collider>();
        if (c != null) Destroy(c);
    }

    // ================================================================== INDICADORES (marcas rojas en el suelo)

    class Marcador
    {
        public GameObject raiz; Transform relleno; float tam;
        public void Progreso(float k)
        {
            if (raiz == null) return;
            float e = Mathf.Clamp01(k);
            relleno.localScale = new Vector3(tam * e, tam * e, 1f);
            raiz.transform.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(Time.time * 18f) * e);
        }
        public void Mover(Vector3 p) { if (raiz != null) raiz.transform.position = p + Vector3.up * 0.12f; }
        public void Destruir() { if (raiz != null) Object.Destroy(raiz); }
        public static Marcador Crear(Vector3 pos, float radio, Color c)
        {
            Marcador m = new Marcador();
            m.tam = radio * 2f;
            m.raiz = new GameObject("MarcaPeligro");
            m.raiz.transform.position = pos + Vector3.up * 0.12f;
            Quad(m.raiz.transform, "Base", radio * 2f, new Color(c.r, c.g, c.b, 0.28f));
            m.relleno = Quad(m.raiz.transform, "Relleno", radio * 2f, new Color(c.r, c.g, c.b, 0.55f)).transform;
            m.relleno.localScale = new Vector3(0f, 0f, 1f);
            return m;
        }
        static GameObject Quad(Transform padre, string nombre, float tam, Color color)
        {
            GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = nombre;
            Object.Destroy(q.GetComponent<Collider>());
            q.transform.SetParent(padre, false);
            q.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            q.transform.localScale = new Vector3(tam, tam, 1f);
            Renderer r = q.GetComponent<Renderer>();
            Material mt = new Material(Shader.Find("Sprites/Default"));
            mt.mainTexture = UIMexicana.Circulo().texture;
            mt.color = color;
            r.sharedMaterial = mt;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return q;
        }
    }

    class Indicador
    {
        public GameObject raiz; Transform relleno; float largo;
        public void Orientar(Vector3 dir) { if (raiz != null && dir.sqrMagnitude > 0.001f) raiz.transform.rotation = Quaternion.LookRotation(dir); }
        public void Progreso(float k)
        {
            if (raiz == null) return;
            float e = Mathf.Clamp01(k);
            relleno.localScale = new Vector3(relleno.localScale.x, largo * e, 1f);
            relleno.localPosition = new Vector3(0f, 0f, largo * e * 0.5f);
        }
        public void Destruir() { if (raiz != null) Object.Destroy(raiz); }
        public static Indicador Crear(Vector3 origen, Vector3 dir, float largo, float ancho, Color c)
        {
            Indicador ind = new Indicador { largo = largo };
            ind.raiz = new GameObject("IndicadorEmbestida");
            ind.raiz.transform.position = origen + Vector3.up * 0.12f;
            ind.Orientar(dir);
            Transform baseQ = Quad(ind.raiz.transform, "Base", ancho, largo, new Color(c.r, c.g, c.b, 0.25f)).transform;
            baseQ.localPosition = new Vector3(0f, 0f, largo * 0.5f);
            ind.relleno = Quad(ind.raiz.transform, "Relleno", ancho, 0.01f, new Color(c.r, c.g, c.b, 0.55f)).transform;
            return ind;
        }
        static GameObject Quad(Transform padre, string nombre, float ancho, float largo, Color color)
        {
            GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = nombre;
            Object.Destroy(q.GetComponent<Collider>());
            q.transform.SetParent(padre, false);
            q.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            q.transform.localScale = new Vector3(ancho, largo, 1f);
            Renderer r = q.GetComponent<Renderer>();
            Material mt = new Material(Shader.Find("Sprites/Default"));
            mt.mainTexture = UIMexicana.Blanco().texture;
            mt.color = color;
            r.sharedMaterial = mt;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return q;
        }
    }

    Marcador CrearMarcador(Vector3 pos, float radio, Color c)
    {
        Marcador m = Marcador.Crear(pos, radio, c);
        Temp(m.raiz);
        return m;
    }

    Indicador CrearIndicador(Vector3 origen, Vector3 dir, float largo, float ancho, Color c)
    {
        Indicador i = Indicador.Crear(origen, dir, largo, ancho, c);
        Temp(i.raiz);
        return i;
    }

    // ---- anillo de la onda de choque (se redibuja en coordenadas de mundo)

    LineRenderer CrearAnillo()
    {
        GameObject go = new GameObject("AnilloOnda");
        Temp(go);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.positionCount = 72;
        lr.widthMultiplier = 0.8f;
        lr.numCapVertices = 2;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Material m = new Material(Shader.Find("Sprites/Default"));
        m.color = new Color(1f, 0.7f, 0.2f, 0.95f);
        lr.sharedMaterial = m;
        lr.startColor = lr.endColor = new Color(1f, 0.75f, 0.25f, 0.95f);
        return lr;
    }

    void DibujarAnillo(LineRenderer lr, Vector3 origen, float y, float radio)
    {
        if (lr == null) return;
        for (int i = 0; i < lr.positionCount; i++)
        {
            float a = (i / (float)lr.positionCount) * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(origen.x + Mathf.Cos(a) * radio, y, origen.z + Mathf.Sin(a) * radio));
        }
    }

    // ================================================================== EFECTOS

    void Polvo(Vector3 pos, float tam)
    {
        Rafaga(pos, tam, new Color(0.55f, 0.5f, 0.42f, 0.7f), 22, 0.9f, 0.1f);
    }

    void Chispas(Vector3 pos)
    {
        Rafaga(pos, 0.5f, new Color(1f, 0.7f, 0.2f, 1f), 8, 0.5f, 0.3f);
    }

    void Rafaga(Vector3 pos, float tam, Color color, int cuenta, float vida, float gravedad)
    {
        Shader spr = Shader.Find("Sprites/Default");
        if (spr == null) return;
        GameObject go = new GameObject("FxJefe");
        go.transform.position = pos;
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = 0.5f; main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(vida * 0.6f, vida * 1.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(tam * 0.6f, tam * 2.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(tam * 0.18f, tam * 0.45f);
        main.startColor = color;
        main.gravityModifier = gravedad;
        var em = ps.emission; em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)cuenta) });
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = tam * 0.25f;
        var col = ps.colorOverLifetime; col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(g);
        Material m = new Material(spr);
        m.mainTexture = UIMexicana.Circulo().texture;
        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = m;
        ps.Play();
        Destroy(go, 2.5f);
    }

    /// <summary>Convierte las piezas del modelo en escombros con física que desaparecen solos.</summary>
    void Romper(Transform raizVisual, int maxPiezas)
    {
        var renderers = raizVisual.GetComponentsInChildren<MeshRenderer>();
        int hechas = 0;
        foreach (MeshRenderer r in renderers)
        {
            if (hechas >= maxPiezas) break;
            if (r.sharedMaterial == matGrieta || r.sharedMaterial == matOjos) continue;
            GameObject copia = GameObject.CreatePrimitive(PrimitiveType.Cube);
            copia.name = "Escombro";
            copia.transform.position = r.transform.position;
            copia.transform.rotation = r.transform.rotation;
            copia.transform.localScale = Vector3.ClampMagnitude(r.transform.lossyScale, 1.3f) * 0.9f;
            copia.GetComponent<Renderer>().sharedMaterial = r.sharedMaterial;
            Rigidbody rb = copia.AddComponent<Rigidbody>();
            rb.mass = 20f;
            rb.AddExplosionForce(900f, transform.position + Vector3.up * 1.5f, 8f, 1.5f);
            rb.AddTorque(Random.insideUnitSphere * 200f);
            Destroy(copia, 6f);
            hechas++;
        }
    }

    // ================================================================== ANIMACIÓN DEL MODELO

    void AnimarVisual()
    {
        if (visualRoot == null) return;
        float t = Time.time;

        Vector3 pos = new Vector3(0f, Mathf.Sin(t * 1.6f) * 0.05f, 0f);
        Quaternion rot = Quaternion.identity;
        Vector3 esc = Vector3.one * escalaVisual;

        if (telegrafiando)
        {
            pos += Random.insideUnitSphere * 0.05f;
            esc = Vector3.Scale(esc, new Vector3(1.06f, 0.93f, 1.06f));
        }
        if (aturdida) rot = Quaternion.Euler(16f + Mathf.Sin(t * 14f) * 2f, 0f, Mathf.Sin(t * 9f) * 3f);
        if (estado == Estado.Dormida) pos.y = 0f;

        visualRoot.localPosition = pos;
        visualRoot.localRotation = rot;
        visualRoot.localScale = esc;

        // brillo de grietas y ojos
        bool flash = Time.time < flashHasta;
        float brillo = estado == Estado.Dormida ? 0.15f
            : aturdida ? 0.25f
            : telegrafiando ? 1.8f + Mathf.Sin(t * 30f) * 0.7f
            : 0.9f + Mathf.Sin(t * 2f) * 0.2f;
        brillo *= Mathf.Max(0.05f, despertarK);

        Color cg = flash ? Color.white : (telegrafiando ? Rojo : Ambar);
        SetEmision(matGrieta, cg * (flash ? 3f : brillo * 2.2f));
        SetEmision(matOjos, (telegrafiando ? Rojo : (fase2 ? Ambar : Cian)) * Mathf.Max(0.05f, despertarK) * (aturdida ? 0.3f : 2.4f));
        if (luzCuerpo != null && estado != Estado.Muriendo)
        {
            luzCuerpo.intensity = Mathf.Lerp(luzCuerpo.intensity, 1.5f + 3f * Mathf.Clamp01(despertarK) * (telegrafiando ? 1.6f : 1f), 6f * Time.deltaTime);
        }
    }

    static void SetEmision(Material m, Color c)
    {
        if (m == null) return;
        m.EnableKeyword("_EMISSION");
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c);
    }

    void ReiniciarCola()
    {
        Vector3 atras = -transform.forward;
        Vector3 anterior = serpCabeza.position;
        for (int i = 0; i < serpSegs.Length; i++)
        {
            Vector3 p = anterior + atras * (serpRadio[i] * 1.5f + (i == 0 ? 1.2f : 0f));
            p.y = transform.position.y + serpRadio[i];
            serpSegs[i].position = p;
            anterior = p;
        }
    }

    /// <summary>La cola de la serpiente sigue a la cabeza como una cadena.</summary>
    void ActualizarCola()
    {
        if (!fase2 || serpSegs == null || serpRoot == null || !serpRoot.activeSelf) return;
        Vector3 anterior = serpCabeza.position;
        float suelo = transform.position.y;
        for (int i = 0; i < serpSegs.Length; i++)
        {
            Transform s = serpSegs[i];
            Vector3 dir = s.position - anterior;
            if (dir.sqrMagnitude < 0.0001f) dir = -transform.forward;
            dir.Normalize();
            float sep = serpRadio[i] * 1.5f + (i == 0 ? 1.2f : 0f);
            Vector3 p = anterior + dir * sep;
            p.y = Mathf.Lerp(p.y, suelo + serpRadio[i], 0.25f);
            s.position = p;
            anterior = p;
        }
    }

    // ================================================================== CONSTRUCCIÓN DEL MODELO

    static Shader Lit()
    {
        if (shaderLit == null)
        {
            shaderLit = Shader.Find("Universal Render Pipeline/Lit");
            if (shaderLit == null) shaderLit = Shader.Find("Standard");
        }
        return shaderLit;
    }

    static Material Mat(string clave, Color c, float suavidad)
    {
        Material m;
        if (cacheMat.TryGetValue(clave, out m) && m != null) return m;
        m = new Material(Lit());
        m.name = clave;
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", suavidad);
        cacheMat[clave] = m;
        return m;
    }

    static Material MatEmisivo(Color c, Color emision)
    {
        Material m = new Material(Lit());
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        m.EnableKeyword("_EMISSION");
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", emision);
        return m;
    }

    static GameObject Parte(PrimitiveType tipo, string nombre, Transform padre, Vector3 pos, Vector3 escala, Material mat)
    {
        GameObject g = GameObject.CreatePrimitive(tipo);
        g.name = nombre;
        g.transform.SetParent(padre, false);
        g.transform.localPosition = pos;
        g.transform.localScale = escala;
        Collider c = g.GetComponent<Collider>();
        if (c != null) Destroy(c);
        g.GetComponent<Renderer>().sharedMaterial = mat;
        return g;
    }

    void ConstruirVisual()
    {
        Material roca = Mat("jefe_roca", new Color(0.38f, 0.36f, 0.35f), 0.15f);
        Material rocaClara = Mat("jefe_roca_clara", new Color(0.50f, 0.47f, 0.43f), 0.2f);
        Material musgo = Mat("jefe_musgo", new Color(0.22f, 0.33f, 0.20f), 0.1f);
        matGrieta = MatEmisivo(Ambar, Ambar * 2f);
        matOjos = MatEmisivo(Cian, Cian * 2.4f);

        visualRoot = new GameObject("Visual").transform;
        visualRoot.SetParent(transform, false);

        // ---------------- fase 1: la joven de piedra (monolito)
        piedraRoot = new GameObject("JovenDePiedra");
        piedraRoot.transform.SetParent(visualRoot, false);
        Transform p = piedraRoot.transform;

        Parte(PrimitiveType.Cylinder, "Falda", p, new Vector3(0f, 1.3f, 0f), new Vector3(2.9f, 1.3f, 2.9f), roca);
        Parte(PrimitiveType.Sphere, "FaldaBorde", p, new Vector3(0f, 0.45f, 0f), new Vector3(3.2f, 0.9f, 3.2f), musgo);
        Parte(PrimitiveType.Sphere, "Cintura", p, new Vector3(0f, 2.75f, 0f), new Vector3(1.8f, 1.4f, 1.4f), rocaClara);
        Parte(PrimitiveType.Sphere, "Torso", p, new Vector3(0f, 3.45f, 0.05f), new Vector3(2.0f, 1.6f, 1.45f), roca);
        Parte(PrimitiveType.Sphere, "HombroI", p, new Vector3(-1.05f, 3.85f, 0f), Vector3.one * 0.75f, rocaClara);
        Parte(PrimitiveType.Sphere, "HombroD", p, new Vector3(1.05f, 3.85f, 0f), Vector3.one * 0.75f, rocaClara);
        GameObject cabeza = Parte(PrimitiveType.Sphere, "Cabeza", p, new Vector3(0f, 4.55f, 0.1f), new Vector3(1.0f, 1.1f, 1.0f), rocaClara);
        Parte(PrimitiveType.Sphere, "OjoI", cabeza.transform, new Vector3(-0.22f, 0.08f, 0.45f), new Vector3(0.2f, 0.12f, 0.12f), matOjos);
        Parte(PrimitiveType.Sphere, "OjoD", cabeza.transform, new Vector3(0.22f, 0.08f, 0.45f), new Vector3(0.2f, 0.12f, 0.12f), matOjos);
        // cabello de lajas
        for (int i = 0; i < 7; i++)
        {
            float x = (i - 3) * 0.28f;
            GameObject laja = Parte(PrimitiveType.Cube, "Laja" + i, p, new Vector3(x, 4.2f - Mathf.Abs(i - 3) * 0.12f, -0.55f), new Vector3(0.3f, 1.9f - Mathf.Abs(i - 3) * 0.2f, 0.22f), roca);
            laja.transform.localRotation = Quaternion.Euler(Random.Range(-8f, 8f), 0f, (i - 3) * 4f);
        }
        // brazos extendidos hacia delante (pidiendo ayuda)
        GameObject brI = Parte(PrimitiveType.Capsule, "BrazoI", p, new Vector3(-1.35f, 3.1f, 0.35f), new Vector3(0.55f, 1.1f, 0.55f), roca);
        brI.transform.localRotation = Quaternion.Euler(-20f, 0f, 12f);
        GameObject brD = Parte(PrimitiveType.Capsule, "BrazoD", p, new Vector3(1.35f, 3.1f, 0.35f), new Vector3(0.55f, 1.1f, 0.55f), roca);
        brD.transform.localRotation = Quaternion.Euler(-20f, 0f, -12f);
        Parte(PrimitiveType.Sphere, "ManoI", p, new Vector3(-1.5f, 2.1f, 0.7f), Vector3.one * 0.65f, rocaClara);
        Parte(PrimitiveType.Sphere, "ManoD", p, new Vector3(1.5f, 2.1f, 0.7f), Vector3.one * 0.65f, rocaClara);
        // grietas de ámbar (el hechizo)
        Grieta(p, new Vector3(0.35f, 3.5f, 0.66f), new Vector3(0.07f, 0.95f, 0.07f), 15f);
        Grieta(p, new Vector3(-0.55f, 3.2f, 0.62f), new Vector3(0.06f, 0.75f, 0.06f), -25f);
        Grieta(p, new Vector3(0.25f, 2.5f, 0.72f), new Vector3(0.06f, 0.85f, 0.06f), 8f);
        Grieta(p, new Vector3(0f, 1.6f, 1.42f), new Vector3(0.07f, 1.3f, 0.07f), 0f);
        Grieta(p, new Vector3(-0.9f, 1.2f, 1.0f), new Vector3(0.06f, 1.0f, 0.06f), -12f);
        Grieta(p, new Vector3(0.95f, 1.0f, 0.95f), new Vector3(0.06f, 1.0f, 0.06f), 14f);
        Grieta(cabeza.transform, new Vector3(0.1f, 0.42f, 0.4f), new Vector3(0.05f, 0.3f, 0.05f), 20f);

        GameObject luz = new GameObject("LuzCuerpo");
        luz.transform.SetParent(visualRoot, false);
        luz.transform.localPosition = new Vector3(0f, 3.4f, 1.0f);
        luzCuerpo = luz.AddComponent<Light>();
        luzCuerpo.type = LightType.Point; luzCuerpo.color = Ambar; luzCuerpo.range = 14f; luzCuerpo.intensity = 2f; luzCuerpo.shadows = LightShadows.None;

        // ---------------- fase 2: la serpiente de la cueva (se activa al transformarse)
        serpRoot = new GameObject("SerpienteDeLaCueva");
        serpRoot.transform.SetParent(visualRoot, false);
        Material piel = Mat("jefe_piel", new Color(0.26f, 0.34f, 0.20f), 0.35f);
        Material vientre = Mat("jefe_vientre", new Color(0.72f, 0.66f, 0.42f), 0.3f);
        Material colmillo = Mat("jefe_colmillo", new Color(0.92f, 0.9f, 0.8f), 0.3f);

        GameObject cab = Parte(PrimitiveType.Sphere, "Cabeza", serpRoot.transform, new Vector3(0f, 1.6f, 1.0f), new Vector3(2.0f, 1.3f, 2.6f), piel);
        serpCabeza = cab.transform;
        Parte(PrimitiveType.Sphere, "OjoI", cab.transform, new Vector3(-0.38f, 0.28f, 0.34f), new Vector3(0.22f, 0.2f, 0.2f), matOjos);
        Parte(PrimitiveType.Sphere, "OjoD", cab.transform, new Vector3(0.38f, 0.28f, 0.34f), new Vector3(0.22f, 0.2f, 0.2f), matOjos);
        Parte(PrimitiveType.Capsule, "ColmilloI", serpRoot.transform, new Vector3(-0.38f, 1.0f, 2.05f), new Vector3(0.14f, 0.3f, 0.14f), colmillo);
        Parte(PrimitiveType.Capsule, "ColmilloD", serpRoot.transform, new Vector3(0.38f, 1.0f, 2.05f), new Vector3(0.14f, 0.3f, 0.14f), colmillo);
        Parte(PrimitiveType.Sphere, "Cuello", serpRoot.transform, new Vector3(0f, 1.6f, -0.2f), new Vector3(1.9f, 1.8f, 2.0f), vientre);

        int n = 14;
        serpSegs = new Transform[n];
        serpRadio = new float[n];
        for (int i = 0; i < n; i++)
        {
            float k = i / (float)(n - 1);
            float r = Mathf.Lerp(1.05f, 0.28f, k);
            serpRadio[i] = r;
            GameObject seg = Parte(PrimitiveType.Sphere, "Seg" + i, serpRoot.transform, new Vector3(0f, r, -1.8f - i * r * 1.5f), Vector3.one * r * 2f, i % 2 == 0 ? piel : vientre);
            serpSegs[i] = seg.transform;
            if (i % 2 == 0 && i < n - 2)
                Parte(PrimitiveType.Sphere, "Rombo", seg.transform, new Vector3(0f, 0.46f, 0f), new Vector3(0.35f, 0.18f, 0.5f), matGrieta);
        }
        serpRoot.SetActive(false);
    }

    void Grieta(Transform padre, Vector3 pos, Vector3 escala, float rotZ)
    {
        GameObject g = Parte(PrimitiveType.Cube, "Grieta", padre, pos, escala, matGrieta);
        g.transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
    }

    // ================================================================== UI: BARRA DE VIDA DEL JEFE

    void ConstruirUI()
    {
        Canvas canvas = UIMexicana.CrearCanvas("BarraJefe", 52);
        canvas.transform.SetParent(transform, false);
        RectTransform raiz = (RectTransform)canvas.transform;

        RectTransform caja = UIMexicana.Nuevo("CajaJefe", raiz);
        UIMexicana.AnclarCentro(caja, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(920f, 84f));
        grupoUI = caja.gameObject.AddComponent<CanvasGroup>();
        grupoUI.alpha = 0f; grupoUI.blocksRaycasts = false; grupoUI.interactable = false;

        txtNombre = UIMexicana.Texto("Nombre", caja, "", 28, UIMexicana.Crema, TextAlignmentOptions.Center, FontStyles.Bold, 6f);
        UIMexicana.AnclarCentro(txtNombre.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(900f, 36f));
        ActualizarNombre();

        RectTransform marco = UIMexicana.Nuevo("Marco", caja);
        UIMexicana.AnclarCentro(marco, new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(880f, 28f));
        Image sombra = UIMexicana.Imagen("Sombra", marco, UIMexicana.Redondeado(14), new Color(0f, 0f, 0f, 0.45f), true);
        UIMexicana.Estirar(sombra.rectTransform); sombra.rectTransform.offsetMin = new Vector2(-2f, -4f); sombra.rectTransform.offsetMax = new Vector2(2f, -2f);
        Image fondo = UIMexicana.Imagen("Fondo", marco, UIMexicana.Redondeado(14), new Color(0.05f, 0.04f, 0.1f, 0.92f), true);
        UIMexicana.Estirar(fondo.rectTransform);

        RectTransform interior = UIMexicana.Nuevo("Interior", marco);
        UIMexicana.Estirar(interior);
        interior.offsetMin = new Vector2(4f, 4f); interior.offsetMax = new Vector2(-4f, -4f);

        Image retraso = UIMexicana.Imagen("Retraso", interior, UIMexicana.Redondeado(10), UIMexicana.H("FFB36B"), true);
        rellenoRetraso = retraso.rectTransform;
        rellenoRetraso.anchorMin = Vector2.zero; rellenoRetraso.anchorMax = Vector2.one;
        rellenoRetraso.offsetMin = Vector2.zero; rellenoRetraso.offsetMax = Vector2.zero;

        imgRelleno = UIMexicana.Imagen("Relleno", interior, UIMexicana.Redondeado(10), UIMexicana.Rojo, true);
        rellenoVida = imgRelleno.rectTransform;
        rellenoVida.anchorMin = Vector2.zero; rellenoVida.anchorMax = Vector2.one;
        rellenoVida.offsetMin = Vector2.zero; rellenoVida.offsetMax = Vector2.zero;

        // marca del 50% (aquí se transforma en serpiente)
        Image muesca = UIMexicana.Imagen("Muesca50", interior, UIMexicana.Blanco(), UIMexicana.ConAlfa(UIMexicana.Dorado, 0.9f));
        muesca.rectTransform.anchorMin = new Vector2(0.5f, 0f); muesca.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        muesca.rectTransform.sizeDelta = new Vector2(3f, 0f);

        Image borde = UIMexicana.Imagen("Borde", marco, UIMexicana.RedondeadoBorde(14, 2f), UIMexicana.ConAlfa(UIMexicana.Dorado, 0.85f), true);
        UIMexicana.Estirar(borde.rectTransform);

        Image calavera = UIMexicana.Imagen("Calavera", caja, UIMexicana.IconoSprite(UIMexicana.Icono.Calavera), UIMexicana.Crema);
        UIMexicana.AnclarCentro(calavera.rectTransform, new Vector2(0f, 0f), new Vector2(-8f, 20f), new Vector2(40f, 40f));
    }

    void ActualizarNombre()
    {
        if (txtNombre == null) return;
        string sub = fase2 ? "La Serpiente de la Cueva" : HistoriaNivel1.SubtituloJefe;
        txtNombre.text = "<color=#FF9F1C>" + HistoriaNivel1.NombreJefe + "</color>   <size=65%><color=#F7EBD0>" + sub.ToUpper() + "</color></size>";
        if (imgRelleno != null) imgRelleno.color = fase2 ? UIMexicana.Verde : UIMexicana.Rojo;
    }

    void ActualizarUI()
    {
        if (grupoUI == null || vida == null) return;
        bool visible = estado == Estado.Combate || estado == Estado.Transformando || estado == Estado.Despertando;
        float meta = visible ? 1f : 0f;
        if (estado == Estado.Despertando) meta = despertarK > 0.6f ? 1f : 0f;
        grupoUI.alpha = Mathf.MoveTowards(grupoUI.alpha, meta, 2.5f * Time.unscaledDeltaTime);

        float p = vida.Porcentaje;
        pMostrada = Mathf.MoveTowards(pMostrada, p, 1.5f * Time.deltaTime);
        if (p > pMostrada) pMostrada = p;
        pRetraso = Mathf.MoveTowards(pRetraso, pMostrada, 0.35f * Time.deltaTime);
        if (pRetraso < pMostrada) pRetraso = pMostrada;

        rellenoVida.anchorMax = new Vector2(Mathf.Clamp01(pMostrada), 1f);
        rellenoRetraso.anchorMax = new Vector2(Mathf.Clamp01(pRetraso), 1f);
    }

    // ================================================================== AUDIO PROCEDURAL

    /// <summary>
    /// Frase del jefe. Entra a la cola de diálogo (la caja muestra a La Bufa hablando) y no se pisa con Tonalli.
    /// Sin narrador en la escena, cae a un aviso en pantalla y su voz directa.
    /// </summary>
    void Decir(string frase, string claveVoz, float segundos, bool opcional = false, string historia = null)
    {
        GuiaAlebrije g = GuiaAlebrije.Instancia;
        // las frases "opcionales" (invocaciones) se omiten si la cola aún está ocupada: llegarían tarde y ya no concordarían con la acción
        if (g != null && opcional && g.Ocupado) return;
        // 'historia' enlaza la frase con su capítulo (indice -1 = antes de sus líneas) para que la cinemática arranque con la voz del jefe
        if (g != null) g.Decir(HistoriaNivel1.VozPerfil.LaBufa, frase, claveVoz, true, historia, -1);
        else { AvisosJuego.Mostrar(frase, segundos); VozNivel.Reproducir(claveVoz, CanalVoz.Personaje); }
    }

    /// <summary>
    /// Tonalli comenta lo que acaba de decir/hacer el jefe. Entra a la cola justo detrás de la frase del jefe,
    /// así que se escucha DESPUÉS de ella, sin pisarse (el parámetro de espera se conserva por compatibilidad).
    /// </summary>
    IEnumerator TonalliDespues(string sufijo, float segundos)
    {
        yield return null;
        GuiaAlebrije g = GuiaAlebrije.Instancia;
        if (g == null) yield break;
        g.Jefe(sufijo);
    }

    void Sonar(AudioClip clip, float volumen)
    {
        if (sfx != null && clip != null) sfx.PlayOneShot(clip, volumen);
    }

    void IniciarMusica()
    {
        musica.clip = ClipMusica();
        musica.volume = 0f;
        musica.Play();
    }

    void ActualizarMusica()
    {
        if (musica == null || musica.clip == null) return;
        bool sonando = estado == Estado.Despertando || estado == Estado.Combate || estado == Estado.Transformando;
        volumenMusica = Mathf.MoveTowards(volumenMusica, sonando ? 0.32f : 0f, (sonando ? 0.15f : 0.12f) * Time.unscaledDeltaTime);
        musica.volume = volumenMusica;
        if (!sonando && volumenMusica <= 0f && musica.isPlaying) musica.Stop();
    }

    static AudioClip clipImpacto, clipRugido, clipAviso, clipMusica;

    static AudioClip ClipImpacto()
    {
        if (clipImpacto != null) return clipImpacto;
        int rate = 22050, n = (int)(rate * 0.8f);
        float[] d = new float[n];
        float lp = 0f;
        System.Random rng = new System.Random(5);
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate;
            float ruido = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp += (ruido - lp) * 0.12f;
            d[i] = (lp * 2.2f * Mathf.Exp(-t * 7f) + Mathf.Sin(2f * Mathf.PI * (70f - 30f * t) * t) * Mathf.Exp(-t * 5f)) * 0.8f;
        }
        clipImpacto = AudioClip.Create("JefeImpacto", n, 1, rate, false);
        clipImpacto.SetData(d, 0);
        return clipImpacto;
    }

    static AudioClip ClipRugido()
    {
        if (clipRugido != null) return clipRugido;
        int rate = 22050, n = (int)(rate * 1.7f);
        float[] d = new float[n];
        System.Random rng = new System.Random(9);
        float fase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate;
            float f = Mathf.Lerp(150f, 52f, t / 1.7f);
            fase += 2f * Mathf.PI * f / rate;
            float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 1.7f));
            float saw = (fase / (2f * Mathf.PI)) % 1f * 2f - 1f;
            float ruido = (float)(rng.NextDouble() * 2.0 - 1.0);
            d[i] = (saw * 0.55f + ruido * 0.25f + Mathf.Sin(fase * 0.5f) * 0.5f) * env * 0.7f;
        }
        clipRugido = AudioClip.Create("JefeRugido", n, 1, rate, false);
        clipRugido.SetData(d, 0);
        return clipRugido;
    }

    static AudioClip ClipAviso()
    {
        if (clipAviso != null) return clipAviso;
        int rate = 22050, n = (int)(rate * 0.4f);
        float[] d = new float[n];
        float fase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate;
            fase += 2f * Mathf.PI * Mathf.Lerp(280f, 900f, t / 0.4f) / rate;
            d[i] = Mathf.Sin(fase) * Mathf.Sin(Mathf.PI * i / n) * 0.5f;
        }
        clipAviso = AudioClip.Create("JefeAviso", n, 1, rate, false);
        clipAviso.SetData(d, 0);
        return clipAviso;
    }

    /// <summary>Tema de jefe: bordón grave + tambor + motivo menor (12 s en bucle).</summary>
    static AudioClip ClipMusica()
    {
        if (clipMusica != null) return clipMusica;
        int rate = 22050, seg = 12, n = rate * seg;
        float[] d = new float[n];
        float beat = 0.6f;
        float[] motivo = { 220f, 261.67f, 220f, 196f, 220f, 293.33f, 261.67f, 196f };
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate;
            float v = 0f;
            // bordón (ciclos enteros en 12 s para que el bucle no haga clic)
            float trem = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * 0.25f * t);
            v += (Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.35f + Mathf.Sin(2f * Mathf.PI * 82.5f * t) * 0.18f) * trem;

            // bombo cada pulso
            float tb = t % beat;
            v += Mathf.Sin(2f * Mathf.PI * (110f * Mathf.Exp(-tb * 14f) + 38f) * tb) * Mathf.Exp(-tb * 9f) * 0.8f;
            // tom en el contratiempo
            float tt = (t + beat * 0.5f) % (beat * 2f);
            if (tt < 0.25f) v += Mathf.Sin(2f * Mathf.PI * (90f - 120f * tt) * tt) * Mathf.Exp(-tt * 12f) * 0.35f;

            // motivo menor, una nota cada dos pulsos
            float ln = beat * 2f;
            int idx = Mathf.FloorToInt(t / ln) % motivo.Length;
            float tn = t % ln;
            v += Mathf.Sin(2f * Mathf.PI * motivo[idx] * tn) * Mathf.Exp(-tn * 3.5f) * 0.12f;

            d[i] = Mathf.Clamp(v * 0.55f, -1f, 1f);
        }
        clipMusica = AudioClip.Create("TemaJefe", n, 1, rate, false);
        clipMusica.SetData(d, 0);
        return clipMusica;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, radioArena);
        Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, radioDespertar);
    }
}
