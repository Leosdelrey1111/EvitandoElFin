using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Correcciones automáticas en runtime para las escenas horneadas (Nivel 1/2/3).
/// Corre una vez por cada escena cargada, sin tener que regenerar los niveles desde el editor:
///  - Partículas rosas (magenta): "NubeFatuo"/"NubeOro" se crearon sin material y en URP salían como cuadros rosas.
///  - Arena del General Momia (Nivel 2): plataforma con collider de esfera gigante, sepulcro-cubo con el jefe
///    metido dentro (solo se veía la cabeza), jefe sin altura de suelo.
///  - Arena del Atrio (Nivel 3): se asegura que exista el cierre de arena y que el Inquisidor pise el suelo.
/// </summary>
public class ArreglosEscenaRuntime : MonoBehaviour
{
    static ArreglosEscenaRuntime instancia;
    static Material matParticula;
    static Texture2D texSuave;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Crear()
    {
        if (instancia != null) return;
        GameObject go = new GameObject("ArreglosEscenaRuntime");
        DontDestroyOnLoad(go);
        instancia = go.AddComponent<ArreglosEscenaRuntime>();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void IniciarEnEscenaActual()
    {
        Crear();
        EjecutarAhora(SceneManager.GetActiveScene().name);
    }

    int framesPanteonRestantes = 0;

    void Awake()
    {
        if (instancia == null) { instancia = this; DontDestroyOnLoad(gameObject); }
        else if (instancia != this) { Destroy(gameObject); return; }
    }

    void Start()
    {
        framesPanteonRestantes = 120;
        EjecutarAhora(SceneManager.GetActiveScene().name);
    }

    void Update()
    {
        if (framesPanteonRestantes > 0)
        {
            framesPanteonRestantes--;
            DespejarSenderoPanteon();
        }
    }

    void OnEnable() { SceneManager.sceneLoaded += AlCargarEscena; }
    void OnDisable() { SceneManager.sceneLoaded -= AlCargarEscena; }

    void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        framesPanteonRestantes = 120;
        StartCoroutine(AplicarTrasEspera(escena.name));
    }

    IEnumerator AplicarTrasEspera(string nombre)
    {
        EjecutarAhora(nombre);
        yield return null;
        yield return new WaitForSecondsRealtime(0.2f);
        EjecutarAhora(nombre);
        yield return new WaitForSecondsRealtime(0.8f);
        EjecutarAhora(nombre);
    }

    public static void EjecutarAhora(string nombre)
    {
        if (string.IsNullOrEmpty(nombre)) nombre = SceneManager.GetActiveScene().name;
        ArreglarParticulas();
        DesactivarColisionesVegetacion();
        if (string.IsNullOrEmpty(nombre) || nombre.Contains("Nivel2") || nombre.Contains("Panteon"))
        {
            DespejarSenderoPanteon();
            ArreglarArenaGeneral();
        }
        if (nombre.Contains("Nivel3") || nombre.Contains("Ciudad")) ArreglarArenaInquisidor();
        if (nombre.Contains("Nivel1") || nombre.Contains("Bosque") || nombre.Contains("Nivel2") || nombre.Contains("Panteon") ||
            nombre.Contains("Nivel3") || nombre.Contains("Ciudad"))
        {
            AcomodoTotems.Acomodar(nombre);
            AcomodoEntorno.Acomodar(nombre);
            SepararDonChuyDeTotems();
            AcomodarPortales();
        }
    }

    // ================================================================== PARTÍCULAS ROSAS

    public static Material MaterialParticula()
    {
        if (matParticula != null) return matParticula;

        if (texSuave == null)
        {
            const int n = 64;
            texSuave = new Texture2D(n, n, TextureFormat.RGBA32, false);
            texSuave.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f;
                    float dy = (y + 0.5f) / n * 2f - 1f;
                    float d = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    float a = d * d * (3f - 2f * d); // smoothstep: bola suave, no un cuadrado
                    texSuave.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            texSuave.Apply();
        }

        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        matParticula = new Material(sh);
        matParticula.mainTexture = texSuave;
        matParticula.name = "MatParticulaSuave";
        return matParticula;
    }

    static bool MaterialRoto(Material m)
    {
        if (m == null || m.shader == null) return true;
        string s = m.shader.name;
        if (!m.shader.isSupported) return true;
        if (s.IndexOf("Error", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
        if (s.StartsWith("Particles/") || s.StartsWith("Legacy Shaders/") || s.StartsWith("Mobile/")) return true;
        if (m.name.Contains("Default") || m.name.Contains("Particle")) return true;
        return false;
    }

    /// <summary>Deja una nube de luciérnagas/polvo flotando suave en la zona (en vez de chorro de cuadros).</summary>
    public static void AjustarNube(ParticleSystem ps)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.45f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.42f);
        main.gravityModifier = -0.01f;
        main.maxParticles = 45;

        var em = ps.emission;
        em.enabled = true;
        em.rateOverTime = 6f;

        var sh = ps.shape;
        sh.enabled = true;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale = new Vector3(18f, 3f, 18f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(g);

        ps.Play();
    }

    public static void ArreglarParticulas()
    {
        int arregladas = 0;
        Material matS = MaterialParticula();
        foreach (ParticleSystem ps in FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include))
        {
            ParticleSystemRenderer r = ps.GetComponent<ParticleSystemRenderer>();
            if (r == null) continue;

            if (MaterialRoto(r.sharedMaterial))
            {
                r.sharedMaterial = matS;
                arregladas++;
            }
            if (ps.gameObject.name.StartsWith("Nube") || ps.gameObject.name.Contains("Fatuo"))
            {
                AjustarNube(ps);
            }
        }
        if (arregladas > 0) Debug.Log("[ArreglosEscena] Partículas con material roto corregidas: " + arregladas);
    }

    // ================================================================== SUELO Y COLLIDERS

    /// <summary>
    /// Un Cylinder de Unity trae CapsuleCollider; con escala ancha (32 m) y poca altura la cápsula se vuelve
    /// una ESFERA de radio 16: muro invisible que impedía entrar a la arena. Lo reemplaza por BoxCollider plano
    /// y una rampa de entrada frontal para que el Chofer suba sin chocar.
    /// </summary>
    public static void PlataformaSolida(GameObject plataforma)
    {
        foreach (CapsuleCollider cap in plataforma.GetComponents<CapsuleCollider>())
        {
            cap.enabled = false;
            if (Application.isPlaying) Destroy(cap); else DestroyImmediate(cap);
        }

        BoxCollider bc = plataforma.GetComponent<BoxCollider>();
        if (bc == null) bc = plataforma.AddComponent<BoxCollider>();
        bc.size = new Vector3(32f, 0.5f, 32f);
        bc.center = Vector3.zero;

        // Rampa suave en la entrada para que el CharacterController suba suavemente
        Transform rampa = plataforma.transform.Find("RampaAccesoEntrada");
        if (rampa == null)
        {
            GameObject rampaGo = new GameObject("RampaAccesoEntrada");
            rampaGo.transform.SetParent(plataforma.transform, false);
            rampaGo.transform.localPosition = new Vector3(0f, -0.15f, -15.5f);
            rampaGo.transform.localRotation = Quaternion.Euler(8.5f, 0f, 0f);
            BoxCollider rampaBc = rampaGo.AddComponent<BoxCollider>();
            rampaBc.size = new Vector3(14f, 0.6f, 6f);
        }
    }

    /// <summary>Apoya el pivote del personaje (pies) sobre el suelo bajo él, ignorando sus propios colliders.</summary>
    public static void AjustarASuelo(Transform t, float extraArriba)
    {
        CharacterController cc = t.GetComponent<CharacterController>();
        Vector3 origen = t.position + Vector3.up * 3f;
        RaycastHit[] hits = Physics.RaycastAll(origen, Vector3.down, 14f, ~0, QueryTriggerInteraction.Ignore);
        float mejor = float.MinValue;
        bool hay = false;
        foreach (RaycastHit h in hits)
        {
            if (h.collider.transform.IsChildOf(t)) continue;
            if (h.point.y > mejor) { mejor = h.point.y; hay = true; }
        }
        if (!hay) return;

        if (cc != null) cc.enabled = false;
        t.position = new Vector3(t.position.x, mejor + extraArriba, t.position.z);
        if (cc != null) cc.enabled = true;
    }

    // ================================================================== NIVEL 2: ARENA DEL GENERAL

    const string MarcaArenaAsentada = "_ArenaAsentadaEnTerreno";

    /// <summary>Plataforma = el disco ancho de la arena (NO las columnas, que también son cilindros).</summary>
    static Transform BuscarPlataformaArena(Transform arena)
    {
        Transform mejor = null;
        foreach (Transform hijo in arena)
        {
            if (hijo.GetComponent<MeshFilter>() == null) continue;
            Vector3 s = hijo.lossyScale;
            if (s.x >= 15f && s.z >= 15f && s.y < s.x * 0.5f)
            {
                if (mejor == null || s.x > mejor.lossyScale.x) mejor = hijo;
            }
        }
        return mejor;
    }

    /// <summary>
    /// Deja la plataforma como un zócalo macizo de 6 m de alto (su parte de abajo queda enterrada en el cerro: ya no se ve
    /// flotando aunque el terreno sea irregular) con un MeshCollider redondo exacto (sin esquinas invisibles).
    /// </summary>
    static void PrepararPlataformaArena(Transform plat)
    {
        foreach (Collider c in plat.GetComponents<Collider>())
        {
            if (c is MeshCollider) continue;
            c.enabled = false;
            Destroy(c);
        }
        MeshCollider mc = plat.GetComponent<MeshCollider>();
        if (mc == null) mc = plat.gameObject.AddComponent<MeshCollider>();
        MeshFilter mf = plat.GetComponent<MeshFilter>();
        if (mf != null) mc.sharedMesh = mf.sharedMesh;
        mc.convex = false;
        mc.enabled = true;

        // Rampa vieja: era hija de la plataforma (escala 32x) y se volvía una pared inclinada invisible enorme
        Transform rampaVieja = plat.Find("RampaAccesoEntrada");
        if (rampaVieja != null) Destroy(rampaVieja.gameObject);

        // Cilindro de Unity: alto local 2 (de -1 a 1). Conservamos la parte de arriba y alargamos hacia abajo.
        Transform padre = plat.parent;
        float escPadreY = padre != null ? Mathf.Max(0.0001f, padre.lossyScale.y) : 1f;
        float techoLocal = plat.localPosition.y + plat.localScale.y;
        float nuevaEscY = 3f / escPadreY; // 6 m de alto en el mundo
        if (plat.localScale.y < nuevaEscY)
        {
            plat.localScale = new Vector3(plat.localScale.x, nuevaEscY, plat.localScale.z);
            plat.localPosition = new Vector3(plat.localPosition.x, techoLocal - nuevaEscY, plat.localPosition.z);
        }
    }

    /// <summary>Baja/sube toda la arena para que la cima de la plataforma quede apenas sobre el terreno del cerro.</summary>
    static void AsentarArenaEnTerreno(Transform arena, Transform plat)
    {
        if (arena.Find(MarcaArenaAsentada) != null) return;

        List<float> alturas = new List<float>();
        float[] radios = { 0f, 4f, 8f, 11f };
        foreach (float r in radios)
        {
            int n = r < 0.1f ? 1 : 10;
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                Vector3 p = arena.position + new Vector3(Mathf.Sin(a) * r, 0f, Mathf.Cos(a) * r);
                float h;
                if (AcomodoTotems.AlturaTerreno(p, out h)) alturas.Add(h);
            }
        }
        if (alturas.Count == 0) return; // sin terreno debajo: no se mueve

        alturas.Sort();
        float sueloObjetivo = alturas[Mathf.Clamp(Mathf.RoundToInt((alturas.Count - 1) * 0.8f), 0, alturas.Count - 1)];

        Renderer rp = plat.GetComponent<Renderer>();
        float techoActual = rp != null ? rp.bounds.max.y : arena.position.y + 0.3f;
        float delta = (sueloObjetivo + 0.15f) - techoActual;

        if (Mathf.Abs(delta) > 0.05f)
        {
            arena.position += Vector3.up * delta;

            // Cosas de la arena que no son hijas de ella
            GameObject portal = GameObject.Find("PortalSalidaPanteon");
            if (portal != null && !portal.transform.IsChildOf(arena)) portal.transform.position += Vector3.up * delta;

            Debug.Log("[ArreglosEscena] Arena del General asentada en el terreno (" + delta.ToString("0.00") + " m).");
        }

        GameObject marca = new GameObject(MarcaArenaAsentada);
        marca.transform.SetParent(arena, false);
        Physics.SyncTransforms();
    }

    /// <summary>Rampa de cantera en la entrada (lado -Z) solo si la plataforma queda más alta que el sendero.</summary>
    static void RampaEntradaArena(Transform arena, Transform plat, float techo)
    {
        if (arena.Find("RampaEntradaGeneral") != null) return;

        Vector3 fuera = arena.TransformPoint(new Vector3(0f, 0f, -22f));
        float hFuera;
        if (!AcomodoTotems.AlturaTerreno(fuera, out hFuera)) return;
        float desnivel = techo - hFuera;
        if (desnivel < 0.8f) return; // el CharacterController sube solo (stepOffset 1.2)

        float largo = Mathf.Max(6f, desnivel * 3.2f);
        float hip = Mathf.Sqrt(largo * largo + desnivel * desnivel);
        float ang = Mathf.Atan2(desnivel, largo) * Mathf.Rad2Deg;

        GameObject rampa = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rampa.name = "RampaEntradaGeneral";
        rampa.transform.SetParent(arena, true);
        Vector3 arriba = arena.TransformPoint(new Vector3(0f, 0f, -15.6f)); arriba.y = techo;
        Vector3 abajo = arriba - arena.forward * largo; abajo.y = hFuera;
        rampa.transform.position = (arriba + abajo) * 0.5f - Vector3.up * 0.2f;
        rampa.transform.rotation = Quaternion.LookRotation(arena.forward) * Quaternion.Euler(-ang, 0f, 0f);
        Vector3 ls = arena.lossyScale;
        rampa.transform.localScale = new Vector3(7f / ls.x, 0.4f / ls.y, (hip + 0.6f) / ls.z);
        Renderer rp = plat.GetComponent<Renderer>();
        if (rp != null) rampa.GetComponent<Renderer>().sharedMaterial = rp.sharedMaterial;
    }

    public static void ArreglarArenaGeneral()
    {
        GameObject arenaGo = GameObject.Find("ArenaMausoleoGeneral");
        if (arenaGo == null) return;
        Transform arena = arenaGo.transform;

        // 1) Plataforma: zócalo macizo, collider redondo exacto y asentada sobre el terreno del cerro
        float techoPlataforma = arena.position.y + 0.3f;
        Transform plat = BuscarPlataformaArena(arena);
        if (plat != null)
        {
            PrepararPlataformaArena(plat);
            AsentarArenaEnTerreno(arena, plat);
            Renderer rend = plat.GetComponent<Renderer>();
            if (rend != null) techoPlataforma = rend.bounds.max.y;
            RampaEntradaArena(arena, plat, techoPlataforma);
        }
        else
        {
            float h;
            if (AcomodoTotems.AlturaSuelo(arena.position, null, out h)) techoPlataforma = h;
        }

        // Limpieza de colliders gigantes que versiones anteriores pusieron por error en las columnas
        foreach (Transform hijo in arena)
        {
            if (hijo == plat) continue;
            Transform rampaVieja = hijo.Find("RampaAccesoEntrada");
            if (rampaVieja != null) Destroy(rampaVieja.gameObject);
            BoxCollider bc = hijo.GetComponent<BoxCollider>();
            if (bc != null && hijo.GetComponent<MeshFilter>() != null && bc.size.x >= 30f) { bc.enabled = false; Destroy(bc); }
        }

        // 2) Sarcófago apoyado en la cima de la plataforma (ni flotando ni enterrado)
        float techoLocal = arena.InverseTransformPoint(new Vector3(arena.position.x, techoPlataforma, arena.position.z)).y;
        Transform viejo = arena.Find(SarcofagoGeneral.Nombre);
        bool esCuboViejo = viejo != null && viejo.childCount == 0;
        if (esCuboViejo)
        {
            viejo.gameObject.SetActive(false);
            viejo.name = "SepulcroGeneral_Cubo_Descartado";
            if (Application.isPlaying) Destroy(viejo.gameObject); else DestroyImmediate(viejo.gameObject);
        }
        if (viejo == null || esCuboViejo)
        {
            Material cantera = null;
            Renderer rv = esCuboViejo ? viejo.GetComponent<Renderer>() : null;
            if (rv != null) cantera = rv.sharedMaterial;
            SarcofagoGeneral.Construir(arena, new Vector3(0f, techoLocal, 10f), cantera);
        }
        else
        {
            viejo.localPosition = new Vector3(viejo.localPosition.x, techoLocal, viejo.localPosition.z);
        }

        // 3) El General de pie delante de su tumba (solo mientras duerme: no teletransportarlo en pleno combate)
        JefeGeneralMomia jefe = FindAnyObjectByType<JefeGeneralMomia>();
        bool enCombate = GameManager.Instancia != null &&
                         (GameManager.Instancia.estadoJefe == GameManager.EstadoJefe.Combate ||
                          GameManager.Instancia.estadoJefe == GameManager.EstadoJefe.Derrotado);
        if (jefe != null && !enCombate)
        {
            jefe.gameObject.tag = "Enemigo";
            CharacterController cc = jefe.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.height = 2.6f; cc.radius = 0.6f; cc.center = new Vector3(0f, 1.3f, 0f);
            }
            Physics.SyncTransforms();
            Transform t = jefe.transform;
            if (cc != null) cc.enabled = false;
            Vector3 frenteTumba = arena.TransformPoint(new Vector3(0f, 0f, 3f));
            t.position = new Vector3(frenteTumba.x, techoPlataforma + 0.05f, frenteTumba.z);
            if (cc != null) cc.enabled = true;

            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                Vector3 haciaJugador = p.transform.position - t.position; haciaJugador.y = 0f;
                if (haciaJugador.sqrMagnitude > 0.01f) t.rotation = Quaternion.LookRotation(haciaJugador.normalized);
            }
            AjustarASuelo(t, 0.03f);
        }
    }

    // ================================================================== NIVEL 3: ARENA DEL ATRIO

    static void ArreglarArenaInquisidor()
    {
        JefeInquisidor jefe = FindAnyObjectByType<JefeInquisidor>();
        if (jefe == null) return;
        jefe.gameObject.tag = "Enemigo";

        CharacterController cc = jefe.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.height = 2.6f; cc.radius = 0.6f; cc.center = new Vector3(0f, 1.3f, 0f);
        }
        Physics.SyncTransforms();
        AjustarASuelo(jefe.transform, 0.03f);

        // Cierre de arena (la escena horneada no lo traía)
        if (FindAnyObjectByType<ArenaAtrioBasilica>() == null)
        {
            GameObject atrio = GameObject.Find("AtrioBasílica_ArenaFinal");
            if (atrio == null) atrio = GameObject.Find("AtrioBasilica_ArenaFinal");

            GameObject sitio = new GameObject("CierreArena_Atrio");
            sitio.transform.position = atrio != null ? atrio.transform.position : jefe.transform.position;
            ArenaAtrioBasilica a = sitio.AddComponent<ArenaAtrioBasilica>();
            a.jefe = jefe.transform;
        }
    }

    static void SepararDonChuyDeTotems()
    {
        GameObject chuy = GameObject.Find("Comerciante_DonChuy") ?? GameObject.Find("Puesto_DonChuy_Ciudad") ?? GameObject.Find("Puesto_DonChuy_Panteon");
        if (chuy == null) return;

        foreach (TotemColeccionable tc in FindObjectsByType<TotemColeccionable>(FindObjectsInactive.Exclude))
        {
            if (tc == null) continue;
            Vector3 diff = chuy.transform.position - tc.transform.position;
            diff.y = 0f;
            if (diff.magnitude < 4.5f)
            {
                Vector3 dir = diff.sqrMagnitude > 0.01f ? diff.normalized : new Vector3(1f, 0f, 0.35f).normalized;
                chuy.transform.position = tc.transform.position + dir * 6.0f;
                AjustarASuelo(chuy.transform, 0f);
                break;
            }
        }
    }

    static void AcomodarPortales()
    {
        foreach (PortalVictoria pv in FindObjectsByType<PortalVictoria>(FindObjectsInactive.Include))
        {
            if (pv == null) continue;
            float suelo;
            if (AcomodoTotems.AlturaSuelo(pv.transform.position, pv.transform, out suelo))
            {
                if (Mathf.Abs(pv.transform.position.y - suelo) > 0.5f)
                    pv.transform.position = new Vector3(pv.transform.position.x, suelo + 0.1f, pv.transform.position.z);
            }
            BoxCollider bc = pv.GetComponent<BoxCollider>();
            if (bc != null)
            {
                bc.isTrigger = true;
                bc.center = new Vector3(0f, 2.0f, 0f);
                bc.size = new Vector3(4.5f, 5.0f, 4.5f);
            }
        }
    }

    // ================================================================== DESPEJE DE SENDEROS Y COLISIONES

    static void DesactivarColisionesVegetacion()
    {
        ChoferController chofer = Object.FindAnyObjectByType<ChoferController>();
        if (chofer != null)
        {
            chofer.controlBloqueado = false;
            CharacterController cc = chofer.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.height = 2f;
                cc.radius = 0.45f;
                cc.center = Vector3.zero;
                cc.stepOffset = Mathf.Max(cc.stepOffset, 1.2f);
                cc.slopeLimit = Mathf.Max(cc.slopeLimit, 85f);
                cc.skinWidth = 0.05f;
            }
            chofer.alturaSalto = 1.7f; // salto natural (~1.7 m): ya no "vuela"
        }
    }

    static readonly Vector3[] SenderoPanteonMundo = new Vector3[]
    {
        new Vector3(-210, 0, -210), // 0: Inicio
        new Vector3(-165, 0, -190),
        new Vector3(-110, 0, -200),
        new Vector3(-60, 0, -175),
        new Vector3(-15, 0, -190),
        new Vector3(40, 0, -165),
        new Vector3(85, 0, -180),
        new Vector3(135, 0, -155),
        new Vector3(170, 0, -110), // 8: Cirio 1
        new Vector3(190, 0, -60),
        new Vector3(165, 0, -15),
        new Vector3(115, 0, 5),
        new Vector3(60, 0, -10),
        new Vector3(5, 0, 10),
        new Vector3(-50, 0, -5),
        new Vector3(-95, 0, 20),   // 15: Cirio 2
        new Vector3(-140, 0, 50),  // 16
        new Vector3(-175, 0, 95),  // 17
        new Vector3(-155, 0, 140), // 18
        new Vector3(-100, 0, 155), // 19
        new Vector3(-45, 0, 140),  // 20: Cirio 3 & Don Chuy
        new Vector3(5, 0, 160),    // 21
        new Vector3(60, 0, 175),   // 22
        new Vector3(115, 0, 155),  // 23
        new Vector3(160, 0, 180)   // 24: Gran Mausoleo
    };

    static void DespejarSenderoPanteon()
    {
        // 1) Separar a Don Chuy de Cirio 3 si están en el mismo punto exacto
        GameObject chuy = GameObject.Find("Comerciante_DonChuy");
        GameObject cirio3 = GameObject.Find("Totem_Cirio_3");
        if (chuy != null && cirio3 != null)
        {
            Vector3 diff = chuy.transform.position - cirio3.transform.position;
            diff.y = 0f;
            if (diff.magnitude < 3f)
            {
                chuy.transform.position = cirio3.transform.position + new Vector3(6f, 0f, 2f);
                AjustarASuelo(chuy.transform, 0f);
            }
        }

        // 2) Retirar árboles/rocas procedurales que hayan caído dentro de 8 metros del sendero
        foreach (Terrain t in Terrain.activeTerrains)
        {
            if (t == null || t.terrainData == null) continue;
            TerrainData td = t.terrainData;
            TreeInstance[] viejos = td.treeInstances;
            if (viejos == null || viejos.Length == 0) continue;

            Vector3 tPos = t.transform.position;
            Vector3 tSize = td.size;
            List<TreeInstance> nuevos = new List<TreeInstance>(viejos.Length);
            int removidos = 0;

            for (int i = 0; i < viejos.Length; i++)
            {
                TreeInstance ti = viejos[i];
                Vector3 wpos = new Vector3(tPos.x + ti.position.x * tSize.x, 0f, tPos.z + ti.position.z * tSize.z);
                if (DistanciaASenderoXZ(wpos, SenderoPanteonMundo) < 8f)
                {
                    removidos++;
                    continue;
                }
                nuevos.Add(ti);
            }

            if (removidos > 0)
            {
                td.treeInstances = nuevos.ToArray();
            }
        }

        // 3) Desactivar cualquier collider decorativo/obstáculo que obstruya el sendero
        Collider[] colliders = Object.FindObjectsByType<Collider>(FindObjectsInactive.Include);
        foreach (Collider c in colliders)
        {
            if (c == null || !c.enabled || c.isTrigger) continue;
            if (c.CompareTag("Player") || c.CompareTag("Enemigo")) continue;
            if (c is TerrainCollider) continue;

            string n = c.gameObject.name;
            string pName = c.transform.parent != null ? c.transform.parent.name : "";

            if (n.Contains("Cirio") || n.Contains("Totem") || n.Contains("Gema") || n.Contains("Chuy") || 
                n.Contains("Arena") || n.Contains("Puerta") || n.Contains("Portal") || n.Contains("Sarcofago")) continue;
            if (pName.Contains("Cirio") || pName.Contains("Totem") || pName.Contains("Chuy") || 
                pName.Contains("Arena") || pName.Contains("Puerta") || pName.Contains("Portal") || pName.Contains("Sarcofago")) continue;

            Vector3 centro = c.bounds.center;
            centro.y = 0f;
            float dist = DistanciaASenderoXZ(centro, SenderoPanteonMundo);
            float radioBounds = Mathf.Max(c.bounds.extents.x, c.bounds.extents.z);
            if (dist < 6.5f || (dist - radioBounds) < 4.0f)
            {
                c.enabled = false;
            }
            else if (n.Contains("PilaPiedras") || n.Contains("Pena_") || n.Contains("Bloque") || 
                     n.Contains("Gavetas") || n.Contains("Mausoleo") || pName.Contains("Cruz_Camino"))
            {
                if (dist < 10f)
                {
                    c.enabled = false;
                }
            }
        }
    }

    public static float DistanciaASenderoXZ(Vector3 p, Vector3[] sendero)
    {
        if (sendero == null || sendero.Length < 2) return float.MaxValue;
        float minD = float.MaxValue;
        for (int i = 0; i < sendero.Length - 1; i++)
        {
            Vector3 a = sendero[i]; a.y = 0f;
            Vector3 b = sendero[i + 1]; b.y = 0f;
            Vector3 ab = b - a;
            float magSq = ab.sqrMagnitude;
            float t = magSq > 0.0001f ? Mathf.Clamp01(Vector3.Dot(p - a, ab) / magSq) : 0f;
            float d = Vector3.Distance(p, a + ab * t);
            if (d < minD) minD = d;
        }
        return minD;
    }
}
