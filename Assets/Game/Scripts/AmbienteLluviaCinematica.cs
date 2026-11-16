using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Noche de tormenta para la cinemática del accidente. Todo se genera por código al empezar:
///  - CARRETERA MOJADA: el asfalto se oscurece y se vuelve brillante (mapa de brillo con charcos), con reflejos en tiempo real de faros y sirenas.
///  - Lluvia que repica: anillos de salpicadura sobre el pavimento y rocío que levantan las llantas.
///  - Tormenta: relámpagos con trueno, gotas resbalando por el lente, haz de luz de los faros del camión bajo la lluvia.
///  - Look de cine (post-procesado): contraste frío, grano, bloom, aberración cromática y desenfoque de movimiento que sube en las escenas de acción.
///  - Marcas de derrape sobre el asfalto, bengalas rojas de los paramédicos y vehículos húmedos.
/// Lo agrega CinematicaAccidenteCamion automáticamente; no necesita configuración en la escena.
/// </summary>
public class AmbienteLluviaCinematica : MonoBehaviour
{
    [Header("Carretera mojada")]
    public bool carreteraMojada = true;
    [Range(0f, 0.9f)] public float oscurecimientoAsfalto = 0.5f;
    [Range(0.3f, 1f)] public float brilloAsfalto = 0.88f;
    public bool reflejosEnTiempoReal = true;

    [Header("Tormenta")]
    public bool relampagos = true;
    public Vector2 intervaloRelampagos = new Vector2(8f, 16f);
    public bool gotasEnLente = true;
    public bool hazDeFaros = true;

    [Header("Imagen")]
    public bool posProcesado = true;

    Camera camara;
    Transform camion, ambulancia;

    // carretera
    ReflectionProbe sonda;
    float proximaSonda;

    // salpicaduras
    ParticleSystem anillos;

    // tormenta
    Light luzLuna;
    AudioSource audioTrueno;
    AudioClip clipTrueno;
    bool relampagoEnCurso;

    // lente
    class Gota { public RectTransform rt; public Image img, brillo; public float vida, duracion, tam, vel, deriva; }
    readonly List<Gota> gotas = new List<Gota>();
    RectTransform capaGotas;
    float proximaGota;

    // post-procesado
    VolumeProfile perfil;
    ChromaticAberration cromatica;
    Vignette vineta;
    MotionBlur desenfoque;
    float accionActual, accionObjetivo;

    // derrape
    readonly List<TrailRenderer> huellas = new List<TrailRenderer>();

    // ================================================================== PREPARAR (lo llama la cinemática)

    public void Preparar(Camera cam, Transform camionT, Transform ambulanciaT)
    {
        camara = cam; camion = camionT; ambulancia = ambulanciaT;

        if (carreteraMojada) MojarCarretera();
        MojarVehiculo(camion);
        MojarVehiculo(ambulancia);
        CrearSondaDeReflejos();
        CrearAnillosDeLluvia();
        CrearTrueno();
        if (posProcesado) CrearPosProcesado();
        if (gotasEnLente) CrearCapaDeGotas();

        luzLuna = null;
        foreach (Light l in Object.FindObjectsByType<Light>())
            if (l.type == LightType.Directional) { luzLuna = l; break; }

        if (camion != null)
        {
            CrearRocio(camion, new[] { new Vector3(-1.35f, 0.25f, -3.8f), new Vector3(1.35f, 0.25f, -3.8f) });
            CrearHuellasDerrape(camion);
            if (hazDeFaros) CrearHazFaros(camion);
        }
        if (ambulancia != null)
            CrearRocio(ambulancia, new[] { new Vector3(-1.2f, 0.25f, -2.0f), new Vector3(1.2f, 0.25f, -2.0f) });

        if (relampagos) StartCoroutine(BucleRelampagos());
    }

    void Update()
    {
        if (camara == null) return;

        ActualizarAnillos();
        ActualizarSonda();
        ActualizarGotas();
        ActualizarImagen();
    }

    // ================================================================== CARRETERA MOJADA

    void MojarCarretera()
    {
        Transform pista = null;
        GeneradorCurvasGuanajuato gen = Object.FindAnyObjectByType<GeneradorCurvasGuanajuato>();
        if (gen != null) pista = gen.transform.Find("Pista_Asfalto_Guanajuato");
        if (pista == null) { GameObject g = GameObject.Find("Pista_Asfalto_Guanajuato"); if (g != null) pista = g.transform; }
        if (pista == null) return;

        MeshRenderer mr = pista.GetComponent<MeshRenderer>();
        if (mr == null || mr.sharedMaterial == null) return;

        Material orig = mr.sharedMaterial;
        Material m = new Material(orig);
        m.name = orig.name + "_Mojado";

        Texture baseTex = orig.HasProperty("_BaseMap") ? orig.GetTexture("_BaseMap") : null;
        Texture2D copia = baseTex != null ? LeerTextura(baseTex) : null;

        if (copia != null)
        {
            int w = copia.width, h = copia.height;
            Color[] px = copia.GetPixels();
            Color[] gloss = new Color[px.Length];

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    Color c = px[i];
                    float u = x / (float)w;
                    bool asfalto = u > 0.10f && u < 0.90f;

                    // charcos: manchas de ruido que enlazan sin costura en el sentido de la carretera (eje y)
                    float n = RuidoCiclicoY(x, y, h);
                    float charco = asfalto ? Mathf.SmoothStep(0.55f, 0.67f, n) : 0f;

                    float oscuro = 1f - oscurecimientoAsfalto * (asfalto ? 1f : 0.45f);
                    oscuro *= 1f - 0.35f * charco;
                    px[i] = new Color(c.r * oscuro, c.g * oscuro, c.b * oscuro, 1f);

                    float suave = asfalto ? Mathf.Lerp(brilloAsfalto * 0.82f, 0.98f, charco) : 0.5f;
                    gloss[i] = new Color(0f, 0f, 0f, suave);
                }

            Texture2D baseMojada = new Texture2D(w, h, TextureFormat.RGBA32, true);
            baseMojada.wrapMode = TextureWrapMode.Repeat;
            baseMojada.SetPixels(px);
            baseMojada.Apply();

            Texture2D brillo = new Texture2D(w, h, TextureFormat.RGBA32, true, true);
            brillo.wrapMode = TextureWrapMode.Repeat;
            brillo.SetPixels(gloss);
            brillo.Apply();

            m.SetTexture("_BaseMap", baseMojada);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", baseMojada);
            m.SetTexture("_MetallicGlossMap", brillo);
            m.EnableKeyword("_METALLICSPECGLOSSMAP");
            m.SetFloat("_Smoothness", 1f);
            m.SetFloat("_Metallic", 0f);
        }
        else
        {
            Color baseC = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.gray;
            m.SetColor("_BaseColor", baseC * (1f - oscurecimientoAsfalto));
            m.SetFloat("_Smoothness", brilloAsfalto);
        }

        // la emisión amarillenta del asfalto original ya no hace falta: con la lluvia brillan los reflejos, no el piso
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", new Color(0.03f, 0.03f, 0.04f));

        mr.sharedMaterial = m;
    }

    static float RuidoCiclicoY(int x, int y, int alto)
    {
        float a = Mathf.PerlinNoise(x * 0.018f + 11.3f, y * 0.018f + 4.7f) * 0.65f + Mathf.PerlinNoise(x * 0.05f + 3.1f, y * 0.05f) * 0.35f;
        float b = Mathf.PerlinNoise(x * 0.018f + 11.3f, (y - alto) * 0.018f + 4.7f) * 0.65f + Mathf.PerlinNoise(x * 0.05f + 3.1f, (y - alto) * 0.05f) * 0.35f;
        return Mathf.Lerp(a, b, y / (float)alto);
    }

    static Texture2D LeerTextura(Texture t)
    {
        try
        {
            RenderTexture rt = RenderTexture.GetTemporary(t.width, t.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            Graphics.Blit(t, rt);
            RenderTexture previo = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D copia = new Texture2D(t.width, t.height, TextureFormat.RGBA32, false);
            copia.ReadPixels(new Rect(0, 0, t.width, t.height), 0, 0);
            copia.Apply();
            RenderTexture.active = previo;
            RenderTexture.ReleaseTemporary(rt);
            return copia;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Lluvia] No se pudo leer la textura del asfalto: " + e.Message);
            return null;
        }
    }

    /// <summary>Carrocerías con más brillo (agua sobre la pintura).</summary>
    void MojarVehiculo(Transform v)
    {
        if (v == null) return;
        foreach (Renderer r in v.GetComponentsInChildren<Renderer>(true))
        {
            if (r is ParticleSystemRenderer || r is TrailRenderer) continue;
            Material[] mats = r.materials;
            foreach (Material m in mats)
            {
                if (m == null || !m.HasProperty("_Smoothness")) continue;
                m.SetFloat("_Smoothness", Mathf.Max(m.GetFloat("_Smoothness"), 0.72f));
            }
        }
    }

    // ================================================================== REFLEJOS

    void CrearSondaDeReflejos()
    {
        if (!reflejosEnTiempoReal) return;
        GameObject go = new GameObject("Sonda_Reflejos_Lluvia");
        go.transform.SetParent(transform, false);
        sonda = go.AddComponent<ReflectionProbe>();
        sonda.mode = ReflectionProbeMode.Realtime;
        sonda.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
        sonda.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
        sonda.resolution = 128;
        sonda.hdr = true;
        sonda.size = new Vector3(140f, 50f, 140f);
        sonda.intensity = 1f;
        sonda.importance = 10;
        sonda.boxProjection = false;
        sonda.cullingMask = ~0;
        proximaSonda = 0f;
    }

    void ActualizarSonda()
    {
        if (sonda == null) return;
        // sigue a la cámara; se refresca unas 2 veces por segundo (suficiente para faros y sirenas)
        sonda.transform.position = camara.transform.position + Vector3.up * 1.5f;
        if (Time.unscaledTime >= proximaSonda)
        {
            proximaSonda = Time.unscaledTime + 0.45f;
            sonda.RenderProbe();
        }
    }

    // ================================================================== SALPICADURAS (anillos en el pavimento)

    Texture2D TexturaAnillo()
    {
        const int n = 64;
        Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f;
                float r = Mathf.Sqrt(u * u + v * v);
                float a = Mathf.Clamp01(1f - Mathf.Abs(r - 0.78f) / 0.10f) * Mathf.Clamp01((1f - r) * 6f);
                t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        t.Apply();
        return t;
    }

    void CrearAnillosDeLluvia()
    {
        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) return;
        Material mat = new Material(sh);
        mat.mainTexture = TexturaAnillo();
        mat.color = Color.white;

        GameObject go = new GameObject("Salpicaduras_Lluvia");
        go.transform.SetParent(transform, false);
        anillos = go.AddComponent<ParticleSystem>();
        anillos.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = anillos.main;
        main.loop = true; main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
        main.startColor = new Color(0.85f, 0.92f, 1f, 0.5f);
        main.maxParticles = 1400;

        var em = anillos.emission; em.rateOverTime = 420f;
        var sp = anillos.shape;
        sp.shapeType = ParticleSystemShapeType.Box;
        sp.scale = new Vector3(34f, 0.1f, 34f);

        var tam = anillos.sizeOverLifetime; tam.enabled = true;
        tam.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.25f), new Keyframe(1f, 2.2f)));
        var col = anillos.colorOverLifetime; col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 0.12f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(g);

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
        r.shadowCastingMode = ShadowCastingMode.Off;
        anillos.Play();
    }

    void ActualizarAnillos()
    {
        if (anillos == null) return;
        // las salpicaduras se concentran donde mira la cámara, pegadas al suelo
        Vector3 foco = camara.transform.position + camara.transform.forward * 13f;
        float y = foco.y;
        RaycastHit hit;
        if (Physics.Raycast(foco + Vector3.up * 30f, Vector3.down, out hit, 80f, ~0, QueryTriggerInteraction.Ignore)) y = hit.point.y;
        anillos.transform.position = new Vector3(foco.x, y + 0.07f, foco.z);
    }

    // ================================================================== ROCÍO DE LLANTAS Y MARCAS DE DERRAPE

    void CrearRocio(Transform vehiculo, Vector3[] posiciones)
    {
        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) return;
        Material mat = new Material(sh);
        mat.mainTexture = UIMexicana.Brillo().texture;

        foreach (Vector3 p in posiciones)
        {
            GameObject go = new GameObject("Rocio_Llanta");
            go.transform.SetParent(vehiculo, false);
            go.transform.localPosition = p;
            go.transform.localRotation = Quaternion.Euler(-120f, 0f, 0f);   // hacia atrás y arriba
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true; main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 1.1f);
            main.startColor = new Color(0.8f, 0.86f, 0.95f, 0.18f);
            main.gravityModifier = 0.15f;
            main.maxParticles = 160;
            var em = ps.emission; em.rateOverTime = 0f;
            var sp = ps.shape; sp.shapeType = ParticleSystemShapeType.Cone; sp.angle = 22f; sp.radius = 0.25f;
            var col = ps.colorOverLifetime; col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
            var tam = ps.sizeOverLifetime; tam.enabled = true;
            tam.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(1f, 1.8f)));
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;

            RocioVehiculo rv = go.AddComponent<RocioVehiculo>();
            rv.sistema = ps; rv.vehiculo = vehiculo;
        }
    }

    void CrearHuellasDerrape(Transform vehiculo)
    {
        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) return;
        Material mat = new Material(sh);
        mat.color = new Color(0.02f, 0.02f, 0.025f, 0.85f);

        Vector3[] pos =
        {
            new Vector3(-1.35f, 0.05f, -1.3f), new Vector3(1.35f, 0.05f, -1.3f),
            new Vector3(-1.35f, 0.05f, -3.6f), new Vector3(1.35f, 0.05f, -3.6f)
        };
        foreach (Vector3 p in pos)
        {
            GameObject go = new GameObject("Huella_Derrape");
            go.transform.SetParent(vehiculo, false);
            go.transform.localPosition = p;
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);   // el ribete queda plano sobre el asfalto
            TrailRenderer tr = go.AddComponent<TrailRenderer>();
            tr.sharedMaterial = mat;
            tr.alignment = LineAlignment.TransformZ;
            tr.time = 9f;
            tr.minVertexDistance = 0.15f;
            tr.widthMultiplier = 0.5f;
            tr.shadowCastingMode = ShadowCastingMode.Off;
            tr.emitting = false;
            huellas.Add(tr);
        }
    }

    /// <summary>Activa/desactiva las marcas negras de las llantas sobre el asfalto (durante el derrape).</summary>
    public void Derrape(bool activo)
    {
        foreach (TrailRenderer t in huellas) if (t != null) t.emitting = activo;
    }

    // ================================================================== HAZ DE LOS FAROS BAJO LA LLUVIA

    void CrearHazFaros(Transform vehiculo)
    {
        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) return;
        Material mat = new Material(sh);
        mat.color = Color.white;

        foreach (float x in new[] { -0.85f, 0.85f })
        {
            GameObject go = new GameObject("Haz_Faro");
            go.transform.SetParent(vehiculo, false);
            go.transform.localPosition = new Vector3(x, 1.1f, 4.7f);
            go.transform.localRotation = Quaternion.Euler(5f, 0f, 0f);

            const int lados = 14;
            const float largo = 26f, radio = 5.2f;
            var vertices = new List<Vector3> { Vector3.zero };
            var colores = new List<Color> { new Color(1f, 0.93f, 0.75f, 0.20f) };
            for (int i = 0; i < lados; i++)
            {
                float a = i / (float)lados * Mathf.PI * 2f;
                vertices.Add(new Vector3(Mathf.Cos(a) * radio, Mathf.Sin(a) * radio * 0.7f, largo));
                colores.Add(new Color(1f, 0.93f, 0.75f, 0f));
            }
            var tris = new List<int>();
            for (int i = 0; i < lados; i++)
            {
                tris.Add(0); tris.Add(1 + i); tris.Add(1 + (i + 1) % lados);
            }
            Mesh mesh = new Mesh { name = "HazFaro" };
            mesh.SetVertices(vertices);
            mesh.SetColors(colores);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }
    }

    // ================================================================== BENGALAS DE EMERGENCIA

    /// <summary>Bengala roja sobre el asfalto: luz parpadeante, chispas y humo. El pavimento mojado refleja el resplandor.</summary>
    public GameObject CrearBengala(Vector3 posicion)
    {
        GameObject root = new GameObject("Bengala_Emergencia");
        root.transform.position = posicion;

        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) lit = Shader.Find("Standard");
        Material tubo = new Material(lit);
        tubo.color = new Color(0.75f, 0.08f, 0.08f);
        GameObject cuerpo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cuerpo.transform.SetParent(root.transform, false);
        cuerpo.transform.localPosition = new Vector3(0f, 0.17f, 0f);
        cuerpo.transform.localScale = new Vector3(0.05f, 0.17f, 0.05f);
        cuerpo.GetComponent<Renderer>().sharedMaterial = tubo;
        Destroy(cuerpo.GetComponent<Collider>());

        GameObject luzGo = new GameObject("LuzBengala");
        luzGo.transform.SetParent(root.transform, false);
        luzGo.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        Light l = luzGo.AddComponent<Light>();
        l.type = LightType.Point; l.color = new Color(1f, 0.15f, 0.08f); l.range = 11f; l.intensity = 5f; l.shadows = LightShadows.None;
        ParpadeoLuz p = luzGo.AddComponent<ParpadeoLuz>();
        p.luz = l; p.intensidadBase = 5f;

        Shader sh = Shader.Find("Sprites/Default");
        if (sh != null)
        {
            Material chispa = new Material(sh); chispa.mainTexture = UIMexicana.Circulo().texture;
            GameObject chGo = new GameObject("Chispas");
            chGo.transform.SetParent(root.transform, false);
            chGo.transform.localPosition = new Vector3(0f, 0.36f, 0f);
            chGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            ParticleSystem ps = chGo.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
            main.startColor = new Color(1f, 0.55f, 0.25f, 1f);
            main.gravityModifier = 0.7f;
            var em = ps.emission; em.rateOverTime = 38f;
            var sp = ps.shape; sp.shapeType = ParticleSystemShapeType.Cone; sp.angle = 28f; sp.radius = 0.02f;
            chGo.GetComponent<ParticleSystemRenderer>().sharedMaterial = chispa;
            ps.Play();

            Material humoMat = new Material(sh); humoMat.mainTexture = UIMexicana.Brillo().texture;
            GameObject huGo = new GameObject("Humo");
            huGo.transform.SetParent(root.transform, false);
            huGo.transform.localPosition = new Vector3(0f, 0.38f, 0f);
            huGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            ParticleSystem hu = huGo.AddComponent<ParticleSystem>();
            hu.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var hm = hu.main;
            hm.loop = true; hm.simulationSpace = ParticleSystemSimulationSpace.World;
            hm.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.6f);
            hm.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            hm.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
            hm.startColor = new Color(1f, 0.35f, 0.3f, 0.22f);
            hm.gravityModifier = -0.05f;
            var he = hu.emission; he.rateOverTime = 9f;
            var hs = hu.shape; hs.shapeType = ParticleSystemShapeType.Cone; hs.angle = 14f; hs.radius = 0.03f;
            var hc = hu.colorOverLifetime; hc.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
            hc.color = new ParticleSystem.MinMaxGradient(g);
            huGo.GetComponent<ParticleSystemRenderer>().sharedMaterial = humoMat;
            hu.Play();
        }
        return root;
    }

    // ================================================================== TORMENTA: RELÁMPAGOS Y TRUENO

    void CrearTrueno()
    {
        audioTrueno = gameObject.AddComponent<AudioSource>();
        audioTrueno.playOnAwake = false;
        audioTrueno.spatialBlend = 0f;
        audioTrueno.volume = 0.7f;

        int rate = 22050, n = (int)(rate * 4.2f);
        float[] d = new float[n];
        System.Random rng = new System.Random(21);
        float lp1 = 0f, lp2 = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate;
            float ruido = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp1 += (ruido - lp1) * (t < 0.18f ? 0.35f : 0.035f);       // chasquido seco al inicio, retumbo grave después
            lp2 += (lp1 - lp2) * 0.12f;
            float env = t < 0.18f ? (t / 0.18f) : Mathf.Exp(-(t - 0.18f) * 0.95f);
            float oleada = 0.75f + 0.25f * Mathf.Sin(t * 5.5f) * Mathf.Sin(t * 2.1f + 1f);
            d[i] = Mathf.Clamp(lp2 * 5.5f * env * oleada, -1f, 1f);
        }
        clipTrueno = AudioClip.Create("Trueno", n, 1, rate, false);
        clipTrueno.SetData(d, 0);
    }

    IEnumerator BucleRelampagos()
    {
        yield return new WaitForSecondsRealtime(5f);
        while (true)
        {
            RelampagoAhora(Random.Range(0.5f, 2.2f));
            yield return new WaitForSecondsRealtime(Random.Range(intervaloRelampagos.x, intervaloRelampagos.y));
        }
    }

    /// <summary>Cae un rayo: destellos en el cielo y el trueno llega después (más lejos = más tarde).</summary>
    public void RelampagoAhora(float retrasoTrueno)
    {
        if (relampagoEnCurso) return;
        StartCoroutine(RutinaRelampago(retrasoTrueno));
    }

    IEnumerator RutinaRelampago(float retrasoTrueno)
    {
        relampagoEnCurso = true;
        Color ambienteOrig = RenderSettings.ambientLight;
        Color nieblaOrig = RenderSettings.fogColor;
        Color luzColor = luzLuna != null ? luzLuna.color : Color.white;
        float luzInt = luzLuna != null ? luzLuna.intensity : 0f;

        float[] pulsos = { 0.07f, 0.05f, 0.14f };
        float[] huecos = { 0.06f, 0.09f, 0f };
        for (int i = 0; i < pulsos.Length; i++)
        {
            if (luzLuna != null) { luzLuna.color = new Color(0.8f, 0.87f, 1f); luzLuna.intensity = Random.Range(2.2f, 3.4f); }
            RenderSettings.ambientLight = new Color(0.30f, 0.34f, 0.46f);
            RenderSettings.fogColor = new Color(0.20f, 0.24f, 0.34f);
            yield return new WaitForSecondsRealtime(pulsos[i]);
            if (luzLuna != null) { luzLuna.color = luzColor; luzLuna.intensity = luzInt; }
            RenderSettings.ambientLight = ambienteOrig;
            RenderSettings.fogColor = nieblaOrig;
            if (huecos[i] > 0f) yield return new WaitForSecondsRealtime(huecos[i]);
        }

        yield return new WaitForSecondsRealtime(retrasoTrueno);
        if (audioTrueno != null && clipTrueno != null)
        {
            audioTrueno.pitch = Random.Range(0.85f, 1.1f);
            audioTrueno.PlayOneShot(clipTrueno, Random.Range(0.55f, 0.9f));
        }
        relampagoEnCurso = false;
    }

    // ================================================================== POST-PROCESADO (look de cine)

    void CrearPosProcesado()
    {
        perfil = ScriptableObject.CreateInstance<VolumeProfile>();

        var bloom = perfil.Add<Bloom>(true);
        bloom.threshold.Override(0.85f);
        bloom.intensity.Override(1.15f);
        bloom.scatter.Override(0.72f);

        vineta = perfil.Add<Vignette>(true);
        vineta.intensity.Override(0.36f);
        vineta.smoothness.Override(0.55f);

        var color = perfil.Add<ColorAdjustments>(true);
        color.postExposure.Override(0.15f);
        color.contrast.Override(14f);
        color.saturation.Override(-12f);
        color.colorFilter.Override(new Color(0.84f, 0.93f, 1f));

        var grano = perfil.Add<FilmGrain>(true);
        grano.type.Override(FilmGrainLookup.Thin1);
        grano.intensity.Override(0.3f);
        grano.response.Override(0.8f);

        cromatica = perfil.Add<ChromaticAberration>(true);
        cromatica.intensity.Override(0.06f);

        desenfoque = perfil.Add<MotionBlur>(true);
        desenfoque.intensity.Override(0.15f);
        desenfoque.quality.Override(MotionBlurQuality.Low);

        var tono = perfil.Add<Tonemapping>(true);
        tono.mode.Override(TonemappingMode.ACES);

        GameObject go = new GameObject("Volumen_Cinematica_Lluvia");
        go.transform.SetParent(transform, false);
        Volume v = go.AddComponent<Volume>();
        v.isGlobal = true;
        v.priority = 20f;
        v.weight = 1f;
        v.sharedProfile = perfil;

        if (camara != null)
        {
            var datos = camara.GetUniversalAdditionalCameraData();
            if (datos != null) datos.renderPostProcessing = true;
        }
    }

    /// <summary>0 = calma, 1 = acción máxima (derrape, impacto): sube la aberración cromática, la viñeta y el desenfoque.</summary>
    public void Accion(float k) { accionObjetivo = Mathf.Clamp01(k); }

    void ActualizarImagen()
    {
        if (perfil == null) return;
        accionActual = Mathf.MoveTowards(accionActual, accionObjetivo, 2.2f * Time.unscaledDeltaTime);
        if (cromatica != null) cromatica.intensity.value = Mathf.Lerp(0.06f, 0.5f, accionActual);
        if (vineta != null) vineta.intensity.value = Mathf.Lerp(0.36f, 0.55f, accionActual);
        if (desenfoque != null) desenfoque.intensity.value = Mathf.Lerp(0.15f, 0.65f, accionActual);
    }

    // ================================================================== GOTAS EN EL LENTE

    void CrearCapaDeGotas()
    {
        GameObject cgo = new GameObject("Lente_Gotas");
        cgo.transform.SetParent(transform, false);
        Canvas canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 450;      // debajo de las barras y los subtítulos de la cinemática (500)
        CanvasScaler cs = cgo.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        capaGotas = (RectTransform)cgo.transform;
    }

    /// <summary>Salpicón de gotas en el lente (por ejemplo al chocar o al pasar un charco).</summary>
    public void GotasExtra(int cantidad)
    {
        for (int i = 0; i < cantidad; i++) NuevaGota(true);
    }

    void NuevaGota(bool grande = false)
    {
        if (capaGotas == null || gotas.Count >= 42) return;

        GameObject go = new GameObject("Gota", typeof(RectTransform));
        go.transform.SetParent(capaGotas, false);
        Gota g = new Gota();
        g.rt = (RectTransform)go.transform;
        g.tam = Random.Range(10f, grande ? 46f : 30f);
        g.duracion = Random.Range(2.5f, 6f);
        g.vel = Random.Range(8f, 30f) * (g.tam / 20f);
        g.deriva = Random.Range(-6f, 6f);
        g.rt.anchorMin = g.rt.anchorMax = Vector2.zero;
        g.rt.pivot = new Vector2(0.5f, 0.5f);
        g.rt.anchoredPosition = new Vector2(Random.Range(0f, 1920f), Random.Range(120f, 1000f));
        g.rt.sizeDelta = new Vector2(g.tam, g.tam * 1.25f);

        g.img = go.AddComponent<Image>();
        g.img.sprite = UIMexicana.Circulo();
        g.img.color = new Color(0.82f, 0.9f, 1f, 0.10f);
        g.img.raycastTarget = false;

        GameObject bgo = new GameObject("Brillo", typeof(RectTransform));
        bgo.transform.SetParent(go.transform, false);
        g.brillo = bgo.AddComponent<Image>();
        g.brillo.sprite = UIMexicana.Circulo();
        g.brillo.color = new Color(1f, 1f, 1f, 0.35f);
        g.brillo.raycastTarget = false;
        RectTransform br = (RectTransform)bgo.transform;
        br.anchorMin = br.anchorMax = new Vector2(0.5f, 0.5f);
        br.sizeDelta = new Vector2(g.tam * 0.28f, g.tam * 0.28f);
        br.anchoredPosition = new Vector2(-g.tam * 0.16f, g.tam * 0.24f);

        gotas.Add(g);
    }

    void ActualizarGotas()
    {
        if (capaGotas == null) return;
        float dt = Time.unscaledDeltaTime;

        proximaGota -= dt;
        if (proximaGota <= 0f) { NuevaGota(); proximaGota = Random.Range(0.12f, 0.35f); }

        for (int i = gotas.Count - 1; i >= 0; i--)
        {
            Gota g = gotas[i];
            g.vida += dt;
            float k = g.vida / g.duracion;
            if (k >= 1f) { Destroy(g.rt.gameObject); gotas.RemoveAt(i); continue; }

            // aparece, se queda un rato y luego resbala hacia abajo
            float aparece = Mathf.Clamp01(g.vida / 0.35f);
            float resbala = Mathf.Clamp01((k - 0.45f) / 0.55f);
            Vector2 p = g.rt.anchoredPosition;
            p.y -= g.vel * resbala * resbala * 6f * dt;
            p.x += g.deriva * resbala * dt;
            g.rt.anchoredPosition = p;
            float alfa = aparece * (1f - Mathf.SmoothStep(0.75f, 1f, k));
            Color c = g.img.color; c.a = 0.10f * alfa * 1.6f; g.img.color = c;
            Color b = g.brillo.color; b.a = 0.4f * alfa; g.brillo.color = b;
            g.rt.sizeDelta = new Vector2(g.tam, g.tam * (1.2f + 0.5f * resbala));
        }
    }
}

/// <summary>Las llantas levantan rocío de agua según la velocidad del vehículo.</summary>
public class RocioVehiculo : MonoBehaviour
{
    public ParticleSystem sistema;
    public Transform vehiculo;
    Vector3 ultima;

    void Start() { if (vehiculo != null) ultima = vehiculo.position; }

    void Update()
    {
        if (sistema == null || vehiculo == null) return;
        float dt = Time.unscaledDeltaTime;
        if (dt <= 0.0001f) return;
        float v = (vehiculo.position - ultima).magnitude / dt;
        ultima = vehiculo.position;

        var em = sistema.emission;
        em.rateOverTime = Mathf.Clamp((v - 3f) * 6f, 0f, 110f);
        if (!sistema.isPlaying) sistema.Play();
    }
}

/// <summary>Parpadeo irregular de una luz (bengalas, fuego).</summary>
public class ParpadeoLuz : MonoBehaviour
{
    public Light luz;
    public float intensidadBase = 5f;
    float semilla;

    void Start() { semilla = Random.value * 100f; }

    void Update()
    {
        if (luz == null) return;
        float t = Time.unscaledTime * 11f + semilla;
        luz.intensity = intensidadBase * (0.7f + 0.3f * Mathf.PerlinNoise(t, semilla) + 0.18f * Mathf.Sin(t * 2.7f));
    }
}
