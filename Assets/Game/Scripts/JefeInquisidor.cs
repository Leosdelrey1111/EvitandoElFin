using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Jefe Final del Nivel 3: EL CLÉRIGO INQUISIDOR, Sombra del Callejón del Beso y la Basílica de Guanajuato.
/// 
/// Mecánicas de Combate:
///  - FASE 1: Pilares de fuego sagrado · Cadenas inquisitoriales · Invocación de penitentes.
///  - FASE 2 (al 50% de vida): Alas de cantera y fuego negro · Lluvia de campanas de bronce · Estallido en cruz.
/// Al ser derrotado, entrega el Recuerdo 3 ("El Amanecer") y abre el Portal Final de Retorno al mundo de los vivos.
/// </summary>
[DefaultExecutionOrder(10)]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PoderEspiritual))]
public class JefeInquisidor : MonoBehaviour
{
    public static JefeInquisidor Instancia { get; private set; }

    public enum Estado { Dormido, Despertando, Combate, Furia, Muriendo, Derrotado }

    [Header("Arena y Distancias")]
    public float radioArena = 16f;
    public float radioDespertar = 13f;

    [Header("Vida")]
    public float vidaMax = 1000f;

    [Header("Daño de Ataques")]
    public float danioPilares = 28f;
    public float danioCadenas = 22f;
    public float danioCampanas = 32f;
    public float danioEstallidoCruz = 36f;

    [Header("Tiempos")]
    public float tiempoTelegrafiado = 1.0f;

    public Estado EstadoActual { get { return estado; } }
    public bool Derrotado { get { return estado == Estado.Derrotado; } }

    private Estado estado = Estado.Dormido;
    private Transform jugador;
    private PoderEspiritual vidaJugador;
    private CharacterController cc;
    private PoderEspiritual vida;

    private bool fase2 = false;
    private bool telegrafiando = false;
    private bool aturdido = false;
    private bool invocoMonjes = false;

    // Visuales
    private GameObject visualRoot;
    private GameObject libroObj;
    private GameObject alasObj;
    private Light luzInquisidor;
    private Material matSotana, matDorado, matFuegoSagrado, matFuegoNegro;

    // UI Barra de Jefe
    private GameObject canvasBarra;
    private Slider sliderVida;
    private TextMeshProUGUI txtNombre, txtSubtitulo;

    private readonly List<EsbirroBaseIA> esbirrosInvocados = new List<EsbirroBaseIA>();

    static readonly Color DoradoSagrado = new Color(1.0f, 0.78f, 0.15f);
    static readonly Color FuegoCarmesí = new Color(0.9f, 0.15f, 0.1f);
    static readonly Color PurpuraHerejia = new Color(0.6f, 0.1f, 0.85f);

    void Awake()
    {
        Instancia = this;
        cc = GetComponent<CharacterController>();
        vida = GetComponent<PoderEspiritual>();
        vida.maxPoder = vidaMax;
        vida.actualPoder = vidaMax;

        ConstruirModeloVisual();
        CrearBarraVida();
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            jugador = p.transform;
            vidaJugador = p.GetComponent<PoderEspiritual>();
        }

        if (GameManager.Instancia != null)
        {
            GameManager.Instancia.jefeRequerido = true;
            GameManager.Instancia.CambiarEstadoJefe(GameManager.EstadoJefe.Dormido);
        }
    }

    void Update()
    {
        if (jugador == null) return;

        if (sliderVida != null && canvasBarra.activeSelf)
        {
            sliderVida.value = Mathf.Clamp01(vida.actualPoder / vidaMax);
        }

        switch (estado)
        {
            case Estado.Dormido:
                if (Vector3.Distance(transform.position, jugador.position) <= radioDespertar)
                {
                    StartCoroutine(RutinaDespertar());
                }
                break;

            case Estado.Combate:
                if (!telegrafiando && !aturdido)
                {
                    MirarHacia(jugador.position);
                    MoverHacia(jugador.position, fase2 ? 4.0f : 2.8f);

                    float dist = Vector3.Distance(transform.position, jugador.position);
                    if (dist <= 6.0f)
                    {
                        StartCoroutine(EjecutarAtaque());
                    }
                }

                if (!fase2 && vida.actualPoder <= vidaMax * 0.5f)
                {
                    StartCoroutine(RutinaTransicionFase2());
                }

                if (!invocoMonjes && vida.actualPoder <= vidaMax * 0.75f)
                {
                    invocoMonjes = true;
                    InvocarPenitentes(2);
                }
                break;
        }

        if (vida.actualPoder <= 0f && estado != Estado.Muriendo && estado != Estado.Derrotado)
        {
            StartCoroutine(RutinaDerrota());
        }
    }

    IEnumerator RutinaDespertar()
    {
        estado = Estado.Despertando;
        canvasBarra.SetActive(true);
        if (GuiaAlebrije.Instancia != null)
            GuiaAlebrije.Instancia.Decir(HistoriaNivel1.VozPerfil.Inquisidor, "«¡En este sagrado suelo nadie escapa al juicio de la Inquisición!»", "inquisidor_despierta", true);
        else
            AvisosJuego.Mostrar("«¡En este sagrado suelo nadie escapa al juicio de la Inquisición!»", 4f);

        float t = 0f;
        while (t < 2.5f)
        {
            t += Time.deltaTime;
            if (luzInquisidor != null) luzInquisidor.intensity = Mathf.Lerp(0.8f, 5.0f, t / 2.5f);
            visualRoot.transform.localScale = Vector3.one * Mathf.Lerp(0.9f, 1.25f, t / 2.5f);
            yield return null;
        }

        if (GameManager.Instancia != null)
        {
            GameManager.Instancia.CambiarEstadoJefe(GameManager.EstadoJefe.Combate);
        }
        estado = Estado.Combate;
    }

    IEnumerator EjecutarAtaque()
    {
        telegrafiando = true;
        int eleccion = Random.Range(0, fase2 ? 4 : 2);

        if (eleccion == 0)
        {
            yield return StartCoroutine(AtaquePilaresFuego());
        }
        else if (eleccion == 1)
        {
            yield return StartCoroutine(AtaqueCadenasInquisicion());
        }
        else if (eleccion == 2)
        {
            yield return StartCoroutine(AtaqueLluviaCampanas());
        }
        else
        {
            yield return StartCoroutine(AtaqueEstallidoCruz());
        }

        yield return new WaitForSeconds(fase2 ? 0.7f : 1.3f);
        telegrafiando = false;
    }

    IEnumerator AtaquePilaresFuego()
    {
        if (GuiaAlebrije.Instancia != null)
            GuiaAlebrije.Instancia.Decir(HistoriaNivel1.VozPerfil.Inquisidor, "«¡Que el fuego sagrado purifique tu alma impura!»", "inquisidor_pilares", true);
        else
            AvisosJuego.Mostrar("«¡Que el fuego purifique tu alma impura!»", 2f);

        // 3 marcas doradas telegrafiadas alrededor del jugador
        Vector3 posCentro = jugador.position;
        List<GameObject> marcas = new List<GameObject>();
        for (int i = 0; i < 3; i++)
        {
            Vector3 offset = Quaternion.Euler(0, i * 120f, 0) * (Vector3.forward * 3f);
            marcas.Add(CrearMarcaSuelo(posCentro + offset, new Vector3(3f, 0.05f, 3f), DoradoSagrado));
        }

        yield return new WaitForSeconds(tiempoTelegrafiado);

        foreach (var m in marcas)
        {
            if (m != null)
            {
                // Checar si el jugador sigue en el pilar
                if (jugador != null && Vector3.Distance(m.transform.position, jugador.position) <= 2.2f)
                {
                    vidaJugador?.RecibirDanio(danioPilares);
                }
                Destroy(m);
            }
        }
    }

    IEnumerator AtaqueCadenasInquisicion()
    {
        GameObject marca = CrearMarcaSuelo(transform.position + transform.forward * 3.5f, new Vector3(2.5f, 0.05f, 7f), FuegoCarmesí);
        yield return new WaitForSeconds(tiempoTelegrafiado);
        Destroy(marca);

        // Golpe directo con el báculo
        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            cc.Move(transform.forward * 16f * Time.deltaTime);
            yield return null;
        }

        if (jugador != null && Vector3.Distance(transform.position, jugador.position) <= 3.2f)
        {
            vidaJugador?.RecibirDanio(danioCadenas);
        }
    }

    IEnumerator AtaqueLluviaCampanas()
    {
        AvisosJuego.Mostrar("✦ ¡DOBLAN LAS CAMPANAS DEL JUICIO! ✦\n¡Salta para evitar la onda de choque!", 3f);

        GameObject marca = CrearMarcaSuelo(transform.position, new Vector3(8.5f, 0.05f, 8.5f), PurpuraHerejia);
        yield return new WaitForSeconds(tiempoTelegrafiado * 0.9f);
        Destroy(marca);

        // Si el jugador no está saltando en el aire (y > 0.4f), recibe el daño de onda de choque
        if (jugador != null && Vector3.Distance(transform.position, jugador.position) <= 6.5f)
        {
            CharacterController ccJ = jugador.GetComponent<CharacterController>();
            bool saltando = (ccJ != null && !ccJ.isGrounded);
            if (!saltando)
            {
                vidaJugador?.RecibirDanio(danioCampanas);
            }
        }
    }

    IEnumerator AtaqueEstallidoCruz()
    {
        // Dos rectángulos cruzados formando una cruz inquisitorial en el suelo
        GameObject barraH = CrearMarcaSuelo(transform.position, new Vector3(12f, 0.05f, 2.5f), FuegoCarmesí);
        GameObject barraV = CrearMarcaSuelo(transform.position, new Vector3(2.5f, 0.05f, 12f), FuegoCarmesí);

        yield return new WaitForSeconds(tiempoTelegrafiado * 0.85f);
        Destroy(barraH);
        Destroy(barraV);

        if (jugador != null)
        {
            Vector3 diff = jugador.position - transform.position;
            if (Mathf.Abs(diff.x) <= 6f && Mathf.Abs(diff.z) <= 1.4f || Mathf.Abs(diff.z) <= 6f && Mathf.Abs(diff.x) <= 1.4f)
            {
                vidaJugador?.RecibirDanio(danioEstallidoCruz);
            }
        }
    }

    IEnumerator RutinaTransicionFase2()
    {
        fase2 = true;
        aturdido = true;
        if (GuiaAlebrije.Instancia != null)
            GuiaAlebrije.Instancia.Decir(HistoriaNivel1.VozPerfil.Inquisidor, "«¡La máscara ha caído! ¡Guanajuato entero arderá conmigo!»", "inquisidor_fase2", true);
        else
            AvisosJuego.Mostrar("✦ ¡LA SOMBRA DE LA HEREJÍA SE DESATA! ✦\n«¡Guanajuato entero arderá conmigo!»", 4f);

        if (alasObj != null) alasObj.SetActive(true);
        if (matFuegoNegro != null) matFuegoNegro.SetColor("_EmissionColor", PurpuraHerejia * 3.5f);

        yield return new WaitForSeconds(2.0f);
        aturdido = false;
        InvocarPenitentes(3);
    }

    void InvocarPenitentes(int cantidad)
    {
        for (int i = 0; i < cantidad; i++)
        {
            Vector3 offset = Quaternion.Euler(0, i * (360f / cantidad), 0) * (Vector3.forward * 5f);
            EsbirroBaseIA e = FabricaEsbirros.Crear(EsbirroBaseIA.TipoEsbirro.Monje, transform.position + offset);
            if (e != null) esbirrosInvocados.Add(e);
        }
    }

    IEnumerator RutinaDerrota()
    {
        estado = Estado.Muriendo;
        if (GuiaAlebrije.Instancia != null)
            GuiaAlebrije.Instancia.Decir(HistoriaNivel1.VozPerfil.Inquisidor, "«La condena del tribunal... se ha roto... ¡Vuelve a la vida, Chofer!»", "inquisidor_derrota", true);
        else
            AvisosJuego.Mostrar("«La condena... se ha roto... ¡Vuelve a la vida, Chofer!»", 4.5f);

        foreach (var e in esbirrosInvocados)
        {
            if (e != null) Destroy(e.gameObject);
        }

        // Gran lluvia de almas
        for (int i = 0; i < 20; i++)
        {
            Vector3 offset = Random.insideUnitSphere * 3.5f;
            offset.y = Mathf.Abs(offset.y) + 0.5f;
            AlmaRecolectable.Crear(transform.position + offset, true, true);
        }

        // Desbloquear Recuerdo 3 definitivo
        CodiceHistoria.Registrar("recuerdo3");

        if (GameManager.Instancia != null)
        {
            GameManager.Instancia.CambiarEstadoJefe(GameManager.EstadoJefe.Derrotado);
        }

        float t = 0f;
        while (t < 3.5f)
        {
            t += Time.deltaTime;
            visualRoot.transform.localScale = Vector3.Lerp(Vector3.one * 1.25f, Vector3.zero, t / 3.5f);
            yield return null;
        }

        if (canvasBarra != null) canvasBarra.SetActive(false);
        estado = Estado.Derrotado;
        gameObject.SetActive(false);
    }

    GameObject CrearMarcaSuelo(Vector3 pos, Vector3 escala, Color col)
    {
        GameObject marca = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marca.name = "MarcaTelegrafiado";
        marca.transform.position = pos + Vector3.up * 0.05f;
        marca.transform.rotation = transform.rotation;
        marca.transform.localScale = escala;
        Collider c = marca.GetComponent<Collider>();
        if (c != null) Destroy(c);

        Material m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        m.color = col;
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", col * 1.6f);
        marca.GetComponent<Renderer>().material = m;
        return marca;
    }

    void MirarHacia(Vector3 punto)
    {
        Vector3 dir = (punto - transform.position);
        dir.y = 0;
        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion rot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, 7f * Time.deltaTime);
        }
    }

    void MoverHacia(Vector3 punto, float vel)
    {
        Vector3 dir = (punto - transform.position).normalized;
        dir.y = -9.8f;
        cc.Move(dir * vel * Time.deltaTime);
    }

    void ConstruirModeloVisual()
    {
        visualRoot = new GameObject("VisualInquisidor");
        visualRoot.transform.SetParent(transform, false);

        matSotana = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        matSotana.color = new Color(0.18f, 0.12f, 0.16f); // Hábito purpúreo oscuro

        matDorado = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        matDorado.color = DoradoSagrado;
        matDorado.EnableKeyword("_EMISSION");
        matDorado.SetColor("_EmissionColor", DoradoSagrado * 1.5f);

        matFuegoSagrado = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        matFuegoSagrado.color = FuegoCarmesí;
        matFuegoSagrado.EnableKeyword("_EMISSION");
        matFuegoSagrado.SetColor("_EmissionColor", FuegoCarmesí * 2.0f);

        matFuegoNegro = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        matFuegoNegro.color = PurpuraHerejia;
        matFuegoNegro.EnableKeyword("_EMISSION");
        matFuegoNegro.SetColor("_EmissionColor", PurpuraHerejia * 2.0f);

        // Intentar instanciar el modelo 3D real del Clérigo Inquisidor
        GameObject prefabClerigo = Resources.Load<GameObject>("Prefabs/Jefe_ClerigoInquisidor");
        #if UNITY_EDITOR
        if (prefabClerigo == null)
            prefabClerigo = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Jefe_ClerigoInquisidor.prefab");
        #endif

        if (prefabClerigo != null)
        {
            GameObject inst = Instantiate(prefabClerigo, visualRoot.transform, false);
            inst.name = "ModeloClerigo3D";
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one * 1.3f;

            var clerigoScript = inst.GetComponent<JefeClerigoInquisidor>(); if (clerigoScript != null) Destroy(clerigoScript);
            var col = inst.GetComponent<Collider>(); if (col != null) Destroy(col);
            var poder = inst.GetComponent<PoderEspiritual>(); if (poder != null) Destroy(poder);
        }
        else
        {
            // Cuerpo con túnica ceremonial
            GameObject sotana = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            sotana.transform.SetParent(visualRoot.transform, false);
            sotana.transform.localPosition = new Vector3(0, 1.1f, 0);
            sotana.transform.localScale = new Vector3(0.85f, 1.1f, 0.85f);
            sotana.GetComponent<Renderer>().material = matSotana;
            Destroy(sotana.GetComponent<Collider>());

            // Estola dorada de juicio
            GameObject estola = GameObject.CreatePrimitive(PrimitiveType.Cube);
            estola.transform.SetParent(visualRoot.transform, false);
            estola.transform.localPosition = new Vector3(0, 1.4f, 0.38f);
            estola.transform.localScale = new Vector3(0.35f, 1.3f, 0.1f);
            estola.GetComponent<Renderer>().material = matDorado;
            Destroy(estola.GetComponent<Collider>());

            // Cabeza con mitra episcopal inquisitorial
            GameObject cabeza = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabeza.transform.SetParent(visualRoot.transform, false);
            cabeza.transform.localPosition = new Vector3(0, 2.3f, 0);
            cabeza.transform.localScale = new Vector3(0.42f, 0.5f, 0.42f);
            cabeza.GetComponent<Renderer>().material = matSotana;
            Destroy(cabeza.GetComponent<Collider>());

            // Mitra alta
            GameObject mitra = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mitra.transform.SetParent(cabeza.transform, false);
            mitra.transform.localPosition = new Vector3(0, 0.7f, 0);
            mitra.transform.localScale = new Vector3(1.1f, 1.4f, 0.7f);
            mitra.GetComponent<Renderer>().material = matDorado;
            Destroy(mitra.GetComponent<Collider>());

            // Ojos de fuego sagrado
            GameObject ojoI = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ojoI.transform.SetParent(cabeza.transform, false);
            ojoI.transform.localPosition = new Vector3(-0.25f, 0.1f, 0.5f);
            ojoI.transform.localScale = Vector3.one * 0.22f;
            ojoI.GetComponent<Renderer>().material = matFuegoSagrado;
            Destroy(ojoI.GetComponent<Collider>());

            GameObject ojoD = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ojoD.transform.SetParent(cabeza.transform, false);
            ojoD.transform.localPosition = new Vector3(0.25f, 0.1f, 0.5f);
            ojoD.transform.localScale = Vector3.one * 0.22f;
            ojoD.GetComponent<Renderer>().material = matFuegoSagrado;
            Destroy(ojoD.GetComponent<Collider>());
        }

        // Libro Inquisitorial flotando en la mano izquierda
        libroObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        libroObj.name = "LibroSentencias";
        libroObj.transform.SetParent(visualRoot.transform, false);
        libroObj.transform.localPosition = new Vector3(-0.65f, 1.4f, 0.45f);
        libroObj.transform.localScale = new Vector3(0.45f, 0.55f, 0.15f);
        libroObj.transform.localRotation = Quaternion.Euler(20, 35, 10);
        libroObj.GetComponent<Renderer>().material = matDorado;
        Destroy(libroObj.GetComponent<Collider>());

        // Alas de Cantera y Fuego Negro (se activan en Fase 2)
        alasObj = new GameObject("AlasHerejia");
        alasObj.transform.SetParent(visualRoot.transform, false);
        alasObj.transform.localPosition = new Vector3(0, 1.8f, -0.3f);

        GameObject alaI = GameObject.CreatePrimitive(PrimitiveType.Cube);
        alaI.transform.SetParent(alasObj.transform, false);
        alaI.transform.localPosition = new Vector3(-1.1f, 0.4f, 0);
        alaI.transform.localScale = new Vector3(1.6f, 0.8f, 0.1f);
        alaI.transform.localRotation = Quaternion.Euler(0, 25, 20);
        alaI.GetComponent<Renderer>().material = matFuegoNegro;
        Destroy(alaI.GetComponent<Collider>());

        GameObject alaD = GameObject.CreatePrimitive(PrimitiveType.Cube);
        alaD.transform.SetParent(alasObj.transform, false);
        alaD.transform.localPosition = new Vector3(1.1f, 0.4f, 0);
        alaD.transform.localScale = new Vector3(1.6f, 0.8f, 0.1f);
        alaD.transform.localRotation = Quaternion.Euler(0, -25, -20);
        alaD.GetComponent<Renderer>().material = matFuegoNegro;
        Destroy(alaD.GetComponent<Collider>());

        alasObj.SetActive(false);

        // Luz de juicio
        GameObject luzGo = new GameObject("LuzInquisidor");
        luzGo.transform.SetParent(visualRoot.transform, false);
        luzGo.transform.localPosition = new Vector3(0, 2f, 0);
        luzInquisidor = luzGo.AddComponent<Light>();
        luzInquisidor.type = LightType.Point;
        luzInquisidor.color = DoradoSagrado;
        luzInquisidor.range = 12f;
        luzInquisidor.intensity = 3.0f;
    }

    void CrearBarraVida()
    {
        canvasBarra = new GameObject("Canvas_BarraJefeInquisidor");
        Canvas c = canvasBarra.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasBarra.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasBarra.AddComponent<GraphicRaycaster>();

        RectTransform panel = UIMexicana.Nuevo("PanelJefe", canvasBarra.transform);
        UIMexicana.AnclarCentro(panel, new Vector2(0.5f, 1f), new Vector2(0, -50f), new Vector2(650f, 65f));

        sliderVida = UIMexicana.SliderMex(panel, "BarraVidaJefe", new Vector2(650f, 26f), 1f, DoradoSagrado, null);

        txtNombre = UIMexicana.Texto("NombreJefe", panel, "EL CLÉRIGO INQUISIDOR", 24, FuegoCarmesí, TextAlignmentOptions.Center, FontStyles.Bold, 2f);
        UIMexicana.AnclarCentro(txtNombre.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, 18f), new Vector2(450f, 30f));

        txtSubtitulo = UIMexicana.Texto("SubJefe", panel, "La Sombra del Callejón del Beso", 15, UIMexicana.Crema, TextAlignmentOptions.Center, FontStyles.Italic, 1f);
        UIMexicana.AnclarCentro(txtSubtitulo.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, -18f), new Vector2(400f, 25f));

        canvasBarra.SetActive(false);
    }
}
