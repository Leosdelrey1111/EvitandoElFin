using UnityEngine;

/// <summary>
/// Alma errante (energía espiritual): flota, es atraída hacia el Chofer cuando se acerca y al tomarla
/// suma energía al inventario y cura un poco el Poder Espiritual.
/// Los esbirros las sueltan al morir (Regla 2) y el nivel las reparte por el bosque.
/// Todo el aspecto se construye en runtime con AlmaRecolectable.Crear(...), no necesita prefab.
/// </summary>
public class AlmaRecolectable : MonoBehaviour
{
    [Header("Recompensa")]
    public float energia = 1f;
    public float curacion = 6f;

    [Header("Comportamiento")]
    public float radioAtraccion = 4.5f;
    public float velocidadAtraccion = 10f;
    public float radioRecoleccion = 0.8f;
    public float amplitudFlotacion = 0.18f;
    public float frecuenciaFlotacion = 2.2f;
    [Tooltip("Segundos antes de poder ser atraída (para que al soltarse del enemigo se vea salir).")]
    public float retrasoAtraccion = 0.5f;
    [Tooltip("0 = no desaparece")]
    public float tiempoVida = 0f;

    private Transform jugador;
    private Vector3 posBase;
    private float fase;
    private float nacio;
    private Vector3 velocidadInicial;
    private bool atraida;
    private float velocidadActual;

    private static Material matAlma;
    private static AudioClip clipAlma;
    private static Sprite spriteBrillo;

    // ---------------------------------------------------------------- fábrica

    public static AlmaRecolectable Crear(Vector3 posicion, bool conLuz, bool salpicar)
    {
        GameObject go = new GameObject("Alma_Errante");
        go.transform.position = posicion;

        // Núcleo luminoso
        GameObject nucleo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        nucleo.name = "Nucleo";
        nucleo.transform.SetParent(go.transform, false);
        nucleo.transform.localScale = Vector3.one * 0.26f;
        Collider colNucleo = nucleo.GetComponent<Collider>();
        if (Application.isPlaying) Destroy(colNucleo); else DestroyImmediate(colNucleo);
        Renderer rn = nucleo.GetComponent<Renderer>();
        rn.sharedMaterial = MaterialAlma(new Color(0.65f, 1f, 0.95f, 1f));
        rn.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // Halo (billboard suave)
        GameObject halo = new GameObject("Halo");
        halo.transform.SetParent(go.transform, false);
        SpriteRenderer sr = halo.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteBrillo();
        sr.color = new Color(0.4f, 1f, 0.9f, 0.55f);
        halo.transform.localScale = Vector3.one * 1.1f;
        halo.AddComponent<MirarCamara>();

        // Estela fantasmal
        TrailRenderer tr = go.AddComponent<TrailRenderer>();
        tr.time = 0.5f;
        tr.widthCurve = new AnimationCurve(new Keyframe(0f, 0.22f), new Keyframe(1f, 0f));
        tr.sharedMaterial = MaterialAlma(Color.white);
        tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(new Color(0.7f, 1f, 0.95f), 0f), new GradientColorKey(new Color(0.2f, 0.7f, 1f), 1f) },
            new[] { new GradientAlphaKey(0.7f, 0f), new GradientAlphaKey(0f, 1f) });
        tr.colorGradient = g;

        if (conLuz)
        {
            Light l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.4f, 1f, 0.9f);
            l.range = 3.5f;
            l.intensity = 1.2f;
            l.shadows = LightShadows.None;
        }

        AlmaRecolectable a = go.AddComponent<AlmaRecolectable>();
        if (salpicar)
        {
            Vector2 d = Random.insideUnitCircle.normalized * Random.Range(1.5f, 3.5f);
            a.velocidadInicial = new Vector3(d.x, Random.Range(2.5f, 4.5f), d.y);
        }
        return a;
    }

    public static void CrearGrupo(Vector3 centro, int cantidad, bool conLuz)
    {
        for (int i = 0; i < cantidad; i++)
        {
            AlmaRecolectable a = Crear(centro, conLuz && i == 0, true);
            a.energia = 1f;
        }
    }

    // ---------------------------------------------------------------- vida

    void Start()
    {
        nacio = Time.time;
        posBase = transform.position;
        fase = Random.value * 10f;
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) jugador = p.transform;
        if (tiempoVida > 0f) Destroy(gameObject, tiempoVida);
    }

    void Update()
    {
        if (jugador == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) jugador = p.transform;
            return;
        }

        float edad = Time.time - nacio;
        Vector3 objetivoCentro = jugador.position + Vector3.up * 0.9f;
        float dist = Vector3.Distance(transform.position, objetivoCentro);

        // Salida inicial (salpicadura al soltarse del enemigo), con gravedad suave y flotación al asentarse
        if (velocidadInicial.sqrMagnitude > 0.01f && !atraida)
        {
            transform.position += velocidadInicial * Time.deltaTime;
            velocidadInicial = Vector3.Lerp(velocidadInicial, Vector3.zero, 1f - Mathf.Exp(-3.2f * Time.deltaTime));
            posBase = transform.position;
        }

        if (!atraida && edad >= retrasoAtraccion && dist <= radioAtraccion) atraida = true;

        if (atraida)
        {
            velocidadActual = Mathf.Min(velocidadAtraccion * 2f, velocidadActual + velocidadAtraccion * 2.5f * Time.deltaTime);
            transform.position = Vector3.MoveTowards(transform.position, objetivoCentro, (velocidadAtraccion * 0.4f + velocidadActual) * Time.deltaTime);
            if (dist <= radioRecoleccion) Recolectar();
        }
        else
        {
            // flotación suave
            float y = Mathf.Sin((Time.time + fase) * frecuenciaFlotacion) * amplitudFlotacion;
            Vector3 p = transform.position;
            if (velocidadInicial.sqrMagnitude <= 0.01f)
            {
                p.y = posBase.y + y;
                transform.position = p;
            }
        }
    }

    void Recolectar()
    {
        if (InventarioEspiritual.Instancia != null) InventarioEspiritual.Instancia.AgregarEnergia(energia);

        PoderEspiritual v = jugador != null ? jugador.GetComponent<PoderEspiritual>() : null;
        if (v != null && curacion > 0f) v.Curar(curacion);

        AudioSource.PlayClipAtPoint(ClipAlma(), transform.position, 0.5f);
        Destroy(gameObject);
    }

    // ---------------------------------------------------------------- recursos compartidos

    static Material MaterialAlma(Color c)
    {
        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");
        Material m = new Material(sh);
        m.color = c;
        return m;
    }

    static Sprite SpriteBrillo()
    {
        if (spriteBrillo != null) return spriteBrillo;
        const int n = 64;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x / (n - 1f) - 0.5f) * 2f, dy = (y / (n - 1f) - 0.5f) * 2f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Pow(1f - Mathf.Clamp01(d), 2.2f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        spriteBrillo = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 64f);
        return spriteBrillo;
    }

    static AudioClip ClipAlma()
    {
        if (clipAlma != null) return clipAlma;
        int rate = 22050, n = (int)(rate * 0.28f);
        float[] d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate;
            float env = Mathf.Exp(-t * 9f);
            d[i] = (Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * 1320f * t) * 0.35f) * env * 0.5f;
        }
        clipAlma = AudioClip.Create("AlmaTomada", n, 1, rate, false);
        clipAlma.SetData(d, 0);
        return clipAlma;
    }
}
