using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class GeneradorCurvasGuanajuato : MonoBehaviour
{
    [Header("Dimensiones de la Carretera")]
    [Tooltip("Ancho del asfalto jugable (en metros). 12m da espacio perfecto para combate y esquivar con Space)")]
    public float anchoCarretera = 12f;
    [Tooltip("Ancho del acotamiento/tierra lateral de la sierra a los costados")]
    public float anchoAcotamiento = 2.2f;
    [Tooltip("Profundidad de la cuneta de desagüe contra la montaña")]
    public float anchoCuneta = 1.0f;
    [Tooltip("Distancia entre muestras a lo largo del trazado")]
    public float pasoSegmento = 1.5f;

    [Header("Peralte y Realismo")]
    [Tooltip("Inclinación automática en curvas (peralte de sierra)")]
    [Range(0f, 8f)]
    public float factorPeralte = 3.5f;

    [Header("Barreras de Contención (Guardarraíles)")]
    [Tooltip("Genera defensa metálica en el flanco del voladero/cañón")]
    public bool generarBarreras = true;
    public float alturaBarrera = 1.15f;
    public float grosorBarrera = 0.28f;
    public float espacioEntrePostes = 3.5f;

    [Header("Materiales y Shaders")]
    public Material materialAsfalto;
    public Material materialBarrera;
    public Material materialMontania;

    [Header("Efectos Semi-Animados y Ambiente")]
    public bool agregarNieblaAnimada = true;
    public bool agregarSenalesChevrons = true;
    public bool agregarPulsacionEspiritual = true;

    [Header("Sierra y Paisaje de Guanajuato")]
    [Tooltip("Genera cortes de cantera, cañón profundo, túnel colonial, picachos y casitas de Guanajuato")]
    public bool generarMontaniasYVegetacion = true;

    [Header("Puntos de Control de la Sierra (Curvas de Guanajuato)")]
    public List<Vector3> puntosDeControl = new List<Vector3>();

    // Contenedores internos
    private GameObject contenedorCarretera;
    private GameObject contenedorBarreras;
    private GameObject contenedorDecoracion;

    void Awake()
    {
        if (puntosDeControl == null || puntosDeControl.Count < 4)
        {
            InicializarCurvasDeGuanajuato();
        }
    }

    void Start()
    {
        if (Application.isPlaying && transform.childCount == 0)
        {
            Generar();
        }
    }

    [ContextMenu("Inicializar Puntos de Curvas de Guanajuato")]
    public void InicializarCurvasDeGuanajuato()
    {
        puntosDeControl = new List<Vector3>()
        {
            new Vector3(0, 0, -15),      // Tramo inicial (frente al Chofer)
            new Vector3(0, 0, 10),       // Recta de aceleración
            new Vector3(10, 1.2f, 38),   // Entrada a la primera curva suave a la derecha
            new Vector3(22, 2.5f, 70),   // Pico de curva sobre el cerro
            new Vector3(5, 4.0f, 105),   // Curva cerrada en S a la izquierda
            new Vector3(-20, 5.5f, 140), // La "Curva del Diablo" (curva cerrada con voladero)
            new Vector3(-12, 6.5f, 180), // Recta panorámica de altura
            new Vector3(15, 7.8f, 220),  // Curva en herradura (horquilla) a la derecha
            new Vector3(25, 7.0f, 260),  // Salida de la herradura hacia el mirador
            new Vector3(8, 5.5f, 300),   // Curva de bajada
            new Vector3(-5, 4.0f, 340),  // Curva amplia final
            new Vector3(0, 3.5f, 380)    // Meta / Explanada de la sierra
        };
    }

    [ContextMenu("Generar Carretera de Guanajuato")]
    public void Generar()
    {
        Limpiar();

        // Desactivar planos genéricos de prueba (por ejemplo, Plane inicial que corta la carretera)
        DesactivarPlanoDePrueba();

        if (puntosDeControl == null || puntosDeControl.Count < 4)
        {
            InicializarCurvasDeGuanajuato();
        }

        AsegurarMateriales();

        // 1. Muestrear la curva Catmull-Rom con peralte real
        List<PuntoRuta> ruta = MuestrearCurvaCatmullRom();
        if (ruta.Count < 2) return;

        // 2. Construir la Malla Maestra de Asfalto, Acotamientos y Cuneta (Superficie Única Continua)
        ConstruirMallaCarretera(ruta);

        // 3. Construir Barreras Metálicas de Contención en el Voladero (Cañón)
        if (generarBarreras)
        {
            ConstruirBarrerasContencionVoladero(ruta);
        }

        // 4. Agregar Efectos Semi-Animados (Flechas reflectantes de curva, niebla baja)
        ConstruirEfectosSemiAnimados(ruta);

        // 5. Construir Paisaje Auténtico de Guanajuato (Taludes de corte, cañón, casitas, túnel y picachos)
        if (generarMontaniasYVegetacion)
        {
            GeneradorMontaniasYVegetacion genMontanias = GetComponent<GeneradorMontaniasYVegetacion>();
            if (genMontanias == null)
            {
                genMontanias = gameObject.AddComponent<GeneradorMontaniasYVegetacion>();
            }

            float wAsfalto = anchoCarretera * 0.5f;
            float bordeIzquierdoMontana = wAsfalto + anchoAcotamiento + anchoCuneta;
            float bordeDerechoCanon = wAsfalto + anchoAcotamiento + 0.35f;

            genMontanias.GenerarTodo(ruta, bordeIzquierdoMontana, bordeDerechoCanon);
        }

        Debug.Log("<color=#00FFAA><b>[Evitando el Fin]</b> ¡Carretera de Guanajuato generada con éxito (Sin bordes invisibles, paisaje colonial y serrano auténtico)!</color>");
    }

    [ContextMenu("Limpiar Mapa Generado")]
    public void Limpiar()
    {
        while (transform.childCount > 0)
        {
            Transform child = transform.GetChild(0);
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }
    }

    private void DesactivarPlanoDePrueba()
    {
        // Si existe un Plane en la raíz de la escena que se usó como suelo de pruebas, desactivarlo
        GameObject planeObj = GameObject.Find("Plane");
        if (planeObj != null && planeObj.transform.parent == null)
        {
            planeObj.SetActive(false);
            Debug.Log("<color=#FFAA00><b>[Evitando el Fin]</b> Se desactivó el 'Plane' de prueba para evitar colisiones invisibles que corten la carretera.</color>");
        }
    }

    public struct PuntoRuta
    {
        public Vector3 posicion;
        public Vector3 adelante;
        public Vector3 derecha;
        public Vector3 arriba;
        public float distanciaAcumulada;
        public float curvatura;
    }

    private List<PuntoRuta> MuestrearCurvaCatmullRom()
    {
        List<PuntoRuta> puntos = new List<PuntoRuta>();
        int numTramos = puntosDeControl.Count - 3;
        float distanciaAcum = 0f;
        Vector3 anterior = ObtenerPuntoCatmullRom(0, 0f);

        for (int i = 0; i < numTramos; i++)
        {
            Vector3 p1 = puntosDeControl[i + 1];
            Vector3 p2 = puntosDeControl[i + 2];

            float distAprox = Vector3.Distance(p1, p2);
            int pasos = Mathf.Max(4, Mathf.CeilToInt(distAprox / pasoSegmento));

            for (int s = 0; s < pasos; s++)
            {
                float t = (float)s / pasos;
                Vector3 pos = ObtenerPuntoCatmullRom(i, t);
                Vector3 tan = ObtenerTangenteCatmullRom(i, t).normalized;

                Vector3 sigTan = ObtenerTangenteCatmullRom(i, Mathf.Clamp01(t + 0.05f)).normalized;
                Vector3 cruz = Vector3.Cross(tan, sigTan);
                float curvatura = cruz.y * 20f;

                Vector3 up = Vector3.up;
                Vector3 right = Vector3.Cross(up, tan).normalized;
                if (factorPeralte > 0)
                {
                    float anguloPeralte = Mathf.Clamp(curvatura * factorPeralte, -12f, 12f);
                    Quaternion rotPeralte = Quaternion.AngleAxis(anguloPeralte, tan);
                    right = rotPeralte * right;
                    up = rotPeralte * up;
                }

                distanciaAcum += Vector3.Distance(anterior, pos);
                anterior = pos;

                puntos.Add(new PuntoRuta
                {
                    posicion = pos,
                    adelante = tan,
                    derecha = right,
                    arriba = up,
                    distanciaAcumulada = distanciaAcum,
                    curvatura = curvatura
                });
            }
        }

        Vector3 pFinal = puntosDeControl[puntosDeControl.Count - 2];
        Vector3 tanFinal = ObtenerTangenteCatmullRom(numTramos - 1, 1f).normalized;
        Vector3 rFinal = Vector3.Cross(Vector3.up, tanFinal).normalized;
        distanciaAcum += Vector3.Distance(anterior, pFinal);

        puntos.Add(new PuntoRuta
        {
            posicion = pFinal,
            adelante = tanFinal,
            derecha = rFinal,
            arriba = Vector3.up,
            distanciaAcumulada = distanciaAcum,
            curvatura = 0f
        });

        return puntos;
    }

    private Vector3 ObtenerPuntoCatmullRom(int tramo, float t)
    {
        Vector3 p0 = puntosDeControl[tramo];
        Vector3 p1 = puntosDeControl[tramo + 1];
        Vector3 p2 = puntosDeControl[tramo + 2];
        Vector3 p3 = puntosDeControl[tramo + 3];

        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    private Vector3 ObtenerTangenteCatmullRom(int tramo, float t)
    {
        Vector3 p0 = puntosDeControl[tramo];
        Vector3 p1 = puntosDeControl[tramo + 1];
        Vector3 p2 = puntosDeControl[tramo + 2];
        Vector3 p3 = puntosDeControl[tramo + 3];

        float t2 = t * t;

        return 0.5f * (
            (-p0 + p2) +
            (2f * (2f * p0 - 5f * p1 + 4f * p2 - p3) * t) +
            (3f * (-p0 + 3f * p1 - 3f * p2 + p3) * t2)
        );
    }

    /// <summary>
    /// Construye una malla maestra continua con asfalto, acotamientos y cuneta de desagüe.
    /// Cuenta con un único MeshCollider continuo, garantizando cero atascos o paredes invisibles.
    /// </summary>
    private void ConstruirMallaCarretera(List<PuntoRuta> ruta)
    {
        contenedorCarretera = new GameObject("Pista_Asfalto_Guanajuato");
        contenedorCarretera.transform.SetParent(transform, false);

        MeshFilter mf = contenedorCarretera.AddComponent<MeshFilter>();
        MeshRenderer mr = contenedorCarretera.AddComponent<MeshRenderer>();
        mr.sharedMaterial = materialAsfalto;

        int numAnillos = ruta.Count;
        int vertsPorAnillo = 8;
        Vector3[] vertices = new Vector3[numAnillos * vertsPorAnillo];
        Vector2[] uvs = new Vector2[numAnillos * vertsPorAnillo];
        List<int> triangulos = new List<int>();

        float wAsfalto = anchoCarretera * 0.5f;

        for (int i = 0; i < numAnillos; i++)
        {
            PuntoRuta pr = ruta[i];
            Vector3 pos = pr.posicion;
            Vector3 r = pr.derecha;
            Vector3 up = pr.arriba;
            float distV = pr.distanciaAcumulada * 0.18f;

            int bIdx = i * vertsPorAnillo;

            // 0: Fondo de Cuneta (Pared de la Sierra)
            vertices[bIdx + 0] = pos - r * (wAsfalto + anchoAcotamiento + anchoCuneta) - up * 0.16f;
            uvs[bIdx + 0] = new Vector2(-0.15f, distV);

            // 1: Borde Cuneta de concreto
            vertices[bIdx + 1] = pos - r * (wAsfalto + anchoAcotamiento + 0.35f) + up * 0.02f;
            uvs[bIdx + 1] = new Vector2(-0.04f, distV);

            // 2: Acotamiento Izquierdo (Grava serrana)
            vertices[bIdx + 2] = pos - r * (wAsfalto + anchoAcotamiento) + up * 0.03f;
            uvs[bIdx + 2] = new Vector2(0.0f, distV);

            // 3: Borde Asfalto Izquierdo (Línea blanca)
            vertices[bIdx + 3] = pos - r * wAsfalto + up * 0.05f;
            uvs[bIdx + 3] = new Vector2(0.08f, distV);

            // 4: Centro Asfalto (Doble línea amarilla)
            vertices[bIdx + 4] = pos + up * 0.05f;
            uvs[bIdx + 4] = new Vector2(0.5f, distV);

            // 5: Borde Asfalto Derecho (Línea blanca)
            vertices[bIdx + 5] = pos + r * wAsfalto + up * 0.05f;
            uvs[bIdx + 5] = new Vector2(0.92f, distV);

            // 6: Acotamiento Derecho (Grava serrana)
            vertices[bIdx + 6] = pos + r * (wAsfalto + anchoAcotamiento) + up * 0.03f;
            uvs[bIdx + 6] = new Vector2(1.0f, distV);

            // 7: Borde Exterior del Voladero (bajo la defensa metálica)
            vertices[bIdx + 7] = pos + r * (wAsfalto + anchoAcotamiento + 0.35f) - up * 0.02f;
            uvs[bIdx + 7] = new Vector2(1.05f, distV);

            if (i < numAnillos - 1)
            {
                int nIdx = (i + 1) * vertsPorAnillo;

                for (int s = 0; s < vertsPorAnillo - 1; s++)
                {
                    int v0 = bIdx + s;
                    int v1 = bIdx + s + 1;
                    int v2 = nIdx + s;
                    int v3 = nIdx + s + 1;

                    // Triángulos con normales mirando hacia ARRIBA
                    triangulos.Add(v0);
                    triangulos.Add(v2);
                    triangulos.Add(v1);

                    triangulos.Add(v1);
                    triangulos.Add(v2);
                    triangulos.Add(v3);
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "Malla_Carretera_Guanajuato";
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangulos.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        mf.sharedMesh = mesh;

        // MeshCollider único y continuo: asegura 100% que no existan micro-escalones
        MeshCollider mc = contenedorCarretera.AddComponent<MeshCollider>();
        mc.sharedMesh = mesh;

        if (agregarPulsacionEspiritual)
        {
            contenedorCarretera.AddComponent<EfectoCarreteraAnimada>();
        }
    }

    /// <summary>
    /// Construye las barreras metálicas de contención (Defensas W) ÚNICAMENTE en el lado del precipicio/cañón,
    /// como en las carreteras reales de montaña (el lado de la pared rocosa queda libre con su cuneta).
    /// </summary>
    private void ConstruirBarrerasContencionVoladero(List<PuntoRuta> ruta)
    {
        contenedorBarreras = new GameObject("Barreras_Contencion_Voladero");
        contenedorBarreras.transform.SetParent(transform, false);

        float wAsfalto = anchoCarretera * 0.5f;
        float offsetLateral = wAsfalto + anchoAcotamiento + 0.15f;

        GameObject goBaranda = new GameObject("Defensa_Metalica_Canon");
        goBaranda.transform.SetParent(contenedorBarreras.transform, false);

        MeshFilter mf = goBaranda.AddComponent<MeshFilter>();
        MeshRenderer mr = goBaranda.AddComponent<MeshRenderer>();
        mr.sharedMaterial = materialBarrera;

        int numAnillos = ruta.Count;
        // Perfil cerrado de 4 vértices para formar un volumen sólido libre de clipping
        int perfiles = 4;
        Vector3[] verts = new Vector3[numAnillos * perfiles];
        Vector2[] uvs = new Vector2[numAnillos * perfiles];
        List<int> tris = new List<int>();

        float yBase = 0.25f;
        float yAlto = yBase + alturaBarrera;

        for (int i = 0; i < numAnillos; i++)
        {
            PuntoRuta pr = ruta[i];
            Vector3 centro = pr.posicion + pr.derecha * offsetLateral;
            Vector3 r = pr.derecha;
            Vector3 up = pr.arriba;
            float distV = pr.distanciaAcumulada * 0.5f;

            int bIdx = i * perfiles;

            // 0: Parte inferior frontal
            verts[bIdx + 0] = centro + up * yBase - r * (grosorBarrera * 0.5f);
            // 1: Onda media saliente (defensa corrugada tipo W)
            verts[bIdx + 1] = centro + up * (yBase + alturaBarrera * 0.5f) - r * (grosorBarrera * 1.0f);
            // 2: Parte superior frontal
            verts[bIdx + 2] = centro + up * yAlto - r * (grosorBarrera * 0.5f);
            // 3: Parte trasera (cierra el volumen)
            verts[bIdx + 3] = centro + up * (yBase + alturaBarrera * 0.5f) + r * (grosorBarrera * 0.5f);

            uvs[bIdx + 0] = new Vector2(0f, distV);
            uvs[bIdx + 1] = new Vector2(0.4f, distV);
            uvs[bIdx + 2] = new Vector2(0.8f, distV);
            uvs[bIdx + 3] = new Vector2(1.2f, distV);

            if (i < numAnillos - 1)
            {
                int nIdx = (i + 1) * perfiles;

                // Cara frontal (curva corrugada)
                for (int s = 0; s < 2; s++)
                {
                    int v0 = bIdx + s;
                    int v1 = bIdx + s + 1;
                    int v2 = nIdx + s;
                    int v3 = nIdx + s + 1;

                    tris.Add(v0); tris.Add(v2); tris.Add(v1);
                    tris.Add(v1); tris.Add(v2); tris.Add(v3);
                }

                // Cierre superior/trasero
                tris.Add(bIdx + 2); tris.Add(nIdx + 2); tris.Add(bIdx + 3);
                tris.Add(bIdx + 3); tris.Add(nIdx + 2); tris.Add(nIdx + 3);

                // Cierre inferior/trasero
                tris.Add(bIdx + 3); tris.Add(nIdx + 3); tris.Add(bIdx + 0);
                tris.Add(bIdx + 0); tris.Add(nIdx + 3); tris.Add(nIdx + 0);
            }

            // Postes de acero clavados al suelo cada X distancia
            if (i % Mathf.Max(1, Mathf.RoundToInt(espacioEntrePostes / pasoSegmento)) == 0)
            {
                CrearPosteSoporte(centro, up, goBaranda.transform);
            }
        }

        Mesh m = new Mesh();
        m.name = "Defensa_Metalica_Mesh";
        m.vertices = verts;
        m.uv = uvs;
        m.triangles = tris.ToArray();
        m.RecalculateNormals();

        mf.sharedMesh = m;

        // MeshCollider cerrado para que el choque sea suave y no se traspase
        MeshCollider mc = goBaranda.AddComponent<MeshCollider>();
        mc.sharedMesh = m;
    }

    private void CrearPosteSoporte(Vector3 posBase, Vector3 up, Transform padre)
    {
        GameObject poste = GameObject.CreatePrimitive(PrimitiveType.Cube);
        poste.name = "Poste_Acero";
        poste.transform.SetParent(padre, false);
        poste.transform.position = posBase + up * (alturaBarrera * 0.4f) - up * 0.2f;
        poste.transform.localScale = new Vector3(0.2f, alturaBarrera * 1.4f, 0.2f);

        // Destruir el collider individual para evitar interferencias
        Collider c = poste.GetComponent<Collider>();
        if (c != null)
        {
            if (Application.isPlaying) Destroy(c);
            else DestroyImmediate(c);
        }

        MeshRenderer mr = poste.GetComponent<MeshRenderer>();
        if (mr != null) mr.sharedMaterial = materialBarrera;
    }

    private void ConstruirEfectosSemiAnimados(List<PuntoRuta> ruta)
    {
        contenedorDecoracion = new GameObject("Ambiente_SemiAnimado_Sierra");
        contenedorDecoracion.transform.SetParent(transform, false);

        float wAsfalto = anchoCarretera * 0.5f;

        // Flechas reflectantes en curvas peligrosas (del lado exterior del voladero)
        if (agregarSenalesChevrons)
        {
            for (int i = 5; i < ruta.Count - 8; i += 12)
            {
                PuntoRuta pr = ruta[i];
                if (Mathf.Abs(pr.curvatura) > 0.07f)
                {
                    float lado = pr.curvatura > 0 ? -(wAsfalto + anchoAcotamiento + 1.2f) : (wAsfalto + anchoAcotamiento + 1.2f);
                    Vector3 posSenal = pr.posicion + pr.derecha * lado + Vector3.up * 1.6f;

                    GameObject senal = new GameObject("Senal_Chevron_" + i);
                    senal.transform.SetParent(contenedorDecoracion.transform, false);
                    senal.transform.position = posSenal;
                    senal.transform.rotation = Quaternion.LookRotation(-pr.adelante);

                    SenalCurvaAnimada scriptSenal = senal.AddComponent<SenalCurvaAnimada>();
                    scriptSenal.apuntarHaciaInterior = pr.curvatura < 0;
                }
            }
        }

        // Niebla atmosférica natural y suave en el horizonte (sin cuadros planos ni círculos en el piso)
        if (agregarNieblaAnimada)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.0035f;
            RenderSettings.fogColor = new Color(0.82f, 0.85f, 0.90f);
        }
    }

    private void AsegurarMateriales()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");

        // 1. Material Asfalto con Doble Línea Amarilla Guanajuatense
        if (materialAsfalto == null)
        {
            materialAsfalto = new Material(urpLit);
            materialAsfalto.name = "Mat_Asfalto_Guanajuato";
            materialAsfalto.SetColor("_BaseColor", new Color(0.24f, 0.24f, 0.27f));
            materialAsfalto.SetFloat("_Smoothness", 0.35f);

            Texture2D texAsfalto = GenerarTexturaCarretera();
            materialAsfalto.SetTexture("_BaseMap", texAsfalto);
            materialAsfalto.EnableKeyword("_EMISSION");
            materialAsfalto.SetColor("_EmissionColor", new Color(0.12f, 0.10f, 0.02f));
        }

        // 2. Material Metálico para Barreras de Contención
        if (materialBarrera == null)
        {
            materialBarrera = new Material(urpLit);
            materialBarrera.name = "Mat_Guardarriel_Guanajuato";
            materialBarrera.SetColor("_BaseColor", new Color(0.85f, 0.85f, 0.88f));
            materialBarrera.SetFloat("_Metallic", 0.85f);
            materialBarrera.SetFloat("_Smoothness", 0.65f);
        }

        // 3. Material Roca de la Sierra
        if (materialMontania == null)
        {
            materialMontania = new Material(urpLit);
            materialMontania.name = "Mat_Sierra_Guanajuato";
            materialMontania.SetColor("_BaseColor", new Color(0.48f, 0.38f, 0.30f));
            materialMontania.SetFloat("_Smoothness", 0.1f);
        }
    }

    private Texture2D GenerarTexturaCarretera()
    {
        int ancho = 512;
        int alto = 512;
        Texture2D tex = new Texture2D(ancho, alto, TextureFormat.RGBA32, true);
        tex.name = "Tex_Asfalto_Carretera_Guanajuato";
        tex.wrapMode = TextureWrapMode.Repeat;

        Color colAsfalto = new Color(0.20f, 0.20f, 0.22f);
        Color colGravaSerrana = new Color(0.46f, 0.38f, 0.30f); // Tepetate / Grava rojiza de sierra
        Color colCuneta = new Color(0.38f, 0.36f, 0.35f);       // Concreto / mampostería de cuneta
        Color colLineaAmarilla = new Color(0.98f, 0.76f, 0.05f); // Amarillo vial SCT mexicano
        Color colLineaBlanca = new Color(0.92f, 0.92f, 0.95f);

        for (int y = 0; y < alto; y++)
        {
            for (int x = 0; x < ancho; x++)
            {
                float u = (float)x / ancho;
                float ruido = (Mathf.PerlinNoise(x * 0.15f, y * 0.15f) - 0.5f) * 0.08f;
                Color pixel = colAsfalto + new Color(ruido, ruido, ruido);

                // Cuneta de montaña (extremo izquierdo: u < 0.04)
                if (u < 0.04f)
                {
                    pixel = colCuneta + new Color(ruido * 0.5f, ruido * 0.5f, ruido * 0.5f);
                }
                // Acotamiento izquierdo (0.04 a 0.08)
                else if (u >= 0.04f && u < 0.08f)
                {
                    float ruidoGrava = Mathf.PerlinNoise(x * 0.2f, y * 0.2f) * 0.15f;
                    pixel = colGravaSerrana + new Color(ruidoGrava, ruidoGrava, ruidoGrava);
                }
                // Acotamiento derecho (0.92 a 1.0)
                else if (u > 0.92f)
                {
                    float ruidoGrava = Mathf.PerlinNoise(x * 0.2f, y * 0.2f) * 0.15f;
                    pixel = colGravaSerrana + new Color(ruidoGrava, ruidoGrava, ruidoGrava);
                }

                // Líneas blancas continuas laterales de acotamiento
                if ((u >= 0.082f && u <= 0.098f) || (u >= 0.902f && u <= 0.918f))
                {
                    pixel = colLineaBlanca;
                }

                // Doble raya continua amarilla en el centro (México SCT: curva peligrosa prohibido rebasar)
                if ((u >= 0.485f && u <= 0.495f) || (u >= 0.505f && u <= 0.515f))
                {
                    pixel = colLineaAmarilla;
                }

                tex.SetPixel(x, y, pixel);
            }
        }

        tex.Apply();
        return tex;
    }
}
