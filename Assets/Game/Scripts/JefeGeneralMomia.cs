using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Jefe del Nivel 2: EL GENERAL MOMIA, Centinela del Panteón de Santa Paula.
/// 
/// Leyenda: Oficial militar incorrupto sepultado en las gavetas de Guanajuato.
/// Su alma no descansa protegiendo los sepulcros y blande su viejo sable de mando.
/// 
/// Mecánicas de Combate:
///  - FASE 1: Estocada de sable telegrafiada · Giro de espada 360° · Orden de marcha (invoca momias).
///  - FASE 2 (al 50% de vida): Envuelto en fuego fatuo esmeralda · Carga con sable de viento · Hendidura de lápidas.
/// </summary>
[DefaultExecutionOrder(10)]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PoderEspiritual))]
public class JefeGeneralMomia : MonoBehaviour
{
    public static JefeGeneralMomia Instancia { get; private set; }

    public enum Estado { Dormido, Despertando, Combate, Furia, Muriendo, Derrotado }

    [Header("Arena y Distancias")]
    public float radioArena = 15f;
    public float radioDespertar = 12f;

    [Header("Vida")]
    public float vidaMax = 850f;

    [Header("Daño de Ataques")]
    public float danioEstocada = 24f;
    public float danioGiro = 28f;
    public float danioCarga = 34f;
    public float danioSepulcro = 22f;

    [Header("Tiempos")]
    public float tiempoTelegrafiado = 1.1f;

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
    private bool invocoFase1 = false, invocoFase2 = false;

    // Visuales procedurales
    private GameObject visualRoot;
    private GameObject sableObj;
    private Light luzGeneral;
    private Material matUniforme, matVendas, matFuegoFatuo;

    // UI Barra de Jefe
    private GameObject canvasBarra;
    private Slider sliderVida;
    private TextMeshProUGUI txtNombre, txtSubtitulo;

    private readonly List<EsbirroBaseIA> esbirrosInvocados = new List<EsbirroBaseIA>();

    static readonly Color VerdeEsmeralda = new Color(0.15f, 0.95f, 0.45f);
    static readonly Color OroViejo = new Color(0.85f, 0.7f, 0.2f);

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

        // Comprobación de vida
        if (sliderVida != null && canvasBarra.activeSelf)
        {
            sliderVida.value = Mathf.Clamp01(vida.actualPoder / vidaMax);
        }

        switch (estado)
        {
            case Estado.Dormido:
                float d = Vector3.Distance(transform.position, jugador.position);
                if (d <= radioDespertar)
                {
                    StartCoroutine(RutinaDespertar());
                }
                break;

            case Estado.Combate:
                if (!telegrafiando && !aturdido)
                {
                    MirarHacia(jugador.position);
                    MoverHacia(jugador.position, 3.2f);

                    float dist = Vector3.Distance(transform.position, jugador.position);
                    if (dist <= 3.0f)
                    {
                        StartCoroutine(EjecutarAtaque());
                    }
                }

                // Transición a fase 2
                if (!fase2 && vida.actualPoder <= vidaMax * 0.5f)
                {
                    StartCoroutine(RutinaTransicionFase2());
                }

                // Invocaciones automáticas por salud
                if (!invocoFase1 && vida.actualPoder <= vidaMax * 0.75f)
                {
                    invocoFase1 = true;
                    InvocarGuardias(2);
                }
                else if (!invocoFase2 && vida.actualPoder <= vidaMax * 0.25f)
                {
                    invocoFase2 = true;
                    InvocarGuardias(2);
                }
                break;

            case Estado.Muriendo:
                // Animando muerte
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
            GuiaAlebrije.Instancia.Decir(HistoriaNivel1.VozPerfil.GeneralMomia, "«¡Nadie profana el Panteón de Santa Paula sin pagar con su alma!»", "general_despierta", true);
        else
            AvisosJuego.Mostrar("«¡Nadie profana el Panteón de Santa Paula sin pagar con su alma!»", 4f);

        // Brillo ascendente de fuego fatuo
        float t = 0f;
        while (t < 2.5f)
        {
            t += Time.deltaTime;
            if (luzGeneral != null) luzGeneral.intensity = Mathf.Lerp(0.5f, 4.0f, t / 2.5f);
            visualRoot.transform.localScale = Vector3.one * Mathf.Lerp(0.9f, 1.15f, t / 2.5f);
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
            // Ataque 1: Estocada de sable
            yield return StartCoroutine(AtaqueEstocada());
        }
        else if (eleccion == 1)
        {
            // Ataque 2: Giro de sable en 360°
            yield return StartCoroutine(AtaqueGiro());
        }
        else if (eleccion == 2)
        {
            // Ataque 3: Carga con sable de viento espectral
            yield return StartCoroutine(AtaqueCargaEspectral());
        }
        else
        {
            // Ataque 4: Clavar sable en sepulcro
            yield return StartCoroutine(AtaqueSepulcro());
        }

        yield return new WaitForSeconds(fase2 ? 0.8f : 1.4f);
        telegrafiando = false;
    }

    IEnumerator AtaqueEstocada()
    {
        // Telegrafiado rectangular frontal
        GameObject marca = CrearMarcaSuelo(transform.position + transform.forward * 2.2f, new Vector3(1.6f, 0.05f, 4.5f), VerdeEsmeralda);
        yield return new WaitForSeconds(tiempoTelegrafiado);
        Destroy(marca);

        // Animación de estocada
        Vector3 posDestino = transform.position + transform.forward * 4.0f;
        float t = 0f;
        while (t < 0.25f)
        {
            t += Time.deltaTime;
            cc.Move(transform.forward * 18f * Time.deltaTime);
            yield return null;
        }

        // Chequeo de daño
        if (jugador != null && Vector3.Distance(transform.position, jugador.position) <= 2.8f)
        {
            vidaJugador?.RecibirDanio(danioEstocada);
        }
    }

    IEnumerator AtaqueGiro()
    {
        // Telegrafiado circular
        GameObject marca = CrearMarcaSuelo(transform.position, new Vector3(5.5f, 0.05f, 5.5f), OroViejo);
        yield return new WaitForSeconds(tiempoTelegrafiado);
        Destroy(marca);

        // Giro visual
        float rotInicial = transform.eulerAngles.y;
        float t = 0f;
        while (t < 0.35f)
        {
            t += Time.deltaTime;
            transform.Rotate(Vector3.up, 720f * Time.deltaTime);
            yield return null;
        }

        if (jugador != null && Vector3.Distance(transform.position, jugador.position) <= 3.8f)
        {
            vidaJugador?.RecibirDanio(danioGiro);
        }
    }

    IEnumerator AtaqueCargaEspectral()
    {
        if (GuiaAlebrije.Instancia != null)
            GuiaAlebrije.Instancia.Decir(HistoriaNivel1.VozPerfil.GeneralMomia, "«¡A la carga, batallón de las ánimas!»", "general_carga", true);
        else
            AvisosJuego.Mostrar("«¡A la carga, batallón de las ánimas!»", 2f);
        GameObject marca = CrearMarcaSuelo(transform.position + transform.forward * 4f, new Vector3(2.5f, 0.05f, 8f), VerdeEsmeralda);
        yield return new WaitForSeconds(tiempoTelegrafiado * 0.8f);
        Destroy(marca);

        // Carga súper veloz
        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            cc.Move(transform.forward * 22f * Time.deltaTime);
            if (jugador != null && Vector3.Distance(transform.position, jugador.position) <= 2.5f)
            {
                vidaJugador?.RecibirDanio(danioCarga);
                break;
            }
            yield return null;
        }
    }

    IEnumerator AtaqueSepulcro()
    {
        // Salto y clavar espada
        GameObject marca = CrearMarcaSuelo(transform.position, new Vector3(7.0f, 0.05f, 7.0f), Color.red);
        yield return new WaitForSeconds(tiempoTelegrafiado);
        Destroy(marca);

        // Efecto expansivo
        if (jugador != null && Vector3.Distance(transform.position, jugador.position) <= 4.8f)
        {
            vidaJugador?.RecibirDanio(danioSepulcro);
        }
    }

    IEnumerator RutinaTransicionFase2()
    {
        fase2 = true;
        aturdido = true;
        if (GuiaAlebrije.Instancia != null)
            GuiaAlebrije.Instancia.Decir(HistoriaNivel1.VozPerfil.GeneralMomia, "«¡El batallón nunca se rinde! ¡Ardan en fuego fatuo!»", "general_furia", true);
        else
            AvisosJuego.Mostrar("✦ ¡LA FURIA DEL GENERAL MOMIA SE DESATA! ✦\n«¡El batallón nunca se rinde!»", 3.5f);

        if (matFuegoFatuo != null)
        {
            matFuegoFatuo.SetColor("_EmissionColor", VerdeEsmeralda * 3.5f);
        }

        yield return new WaitForSeconds(2.0f);
        aturdido = false;
        InvocarGuardias(3);
    }

    void InvocarGuardias(int cantidad)
    {
        for (int i = 0; i < cantidad; i++)
        {
            Vector3 offset = Quaternion.Euler(0, i * (360f / cantidad), 0) * (Vector3.forward * 4f);
            Vector3 pos = transform.position + offset;
            EsbirroBaseIA e = FabricaEsbirros.Crear(EsbirroBaseIA.TipoEsbirro.Momia, pos);
            if (e != null) esbirrosInvocados.Add(e);
        }
    }

    IEnumerator RutinaDerrota()
    {
        estado = Estado.Muriendo;
        if (GuiaAlebrije.Instancia != null)
            GuiaAlebrije.Instancia.Decir(HistoriaNivel1.VozPerfil.GeneralMomia, "«Rindo honores a tu valor, Chofer... por fin descanso en mi cripta...»", "general_derrota", true);
        else
            AvisosJuego.Mostrar("«Gracias... por fin descanso en mi cripta...»", 4f);

        // Eliminar esbirros restantes
        foreach (var e in esbirrosInvocados)
        {
            if (e != null) Destroy(e.gameObject);
        }

        // Soltar almas masivas
        for (int i = 0; i < 15; i++)
        {
            Vector3 offset = Random.insideUnitSphere * 2.5f;
            offset.y = Mathf.Abs(offset.y) + 0.5f;
            AlmaRecolectable.Crear(transform.position + offset, true, true);
        }

        // Desbloquear Recuerdo 2 en el Códice
        CodiceHistoria.Registrar("recuerdo2");

        if (GameManager.Instancia != null)
        {
            GameManager.Instancia.CambiarEstadoJefe(GameManager.EstadoJefe.Derrotado);
        }

        // Desvanecer visual
        float t = 0f;
        while (t < 3f)
        {
            t += Time.deltaTime;
            visualRoot.transform.localScale = Vector3.Lerp(Vector3.one * 1.15f, Vector3.zero, t / 3f);
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
        m.SetColor("_EmissionColor", col * 1.5f);
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
        visualRoot = new GameObject("VisualGeneral");
        visualRoot.transform.SetParent(transform, false);

        matUniforme = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        matUniforme.color = new Color(0.2f, 0.28f, 0.22f); // Verde militar desgastado

        matVendas = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        matVendas.color = new Color(0.65f, 0.6f, 0.5f);

        matFuegoFatuo = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        matFuegoFatuo.color = VerdeEsmeralda;
        matFuegoFatuo.EnableKeyword("_EMISSION");
        matFuegoFatuo.SetColor("_EmissionColor", VerdeEsmeralda * 1.8f);

        // Intentar instanciar el modelo 3D real del Comandante Momia
        GameObject prefabGeneral = Resources.Load<GameObject>("Prefabs/Esbirro_GuerreroIndigena");
        #if UNITY_EDITOR
        if (prefabGeneral == null)
            prefabGeneral = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Esbirro_GuerreroIndigena.prefab");
        #endif

        if (prefabGeneral != null)
        {
            GameObject inst = Instantiate(prefabGeneral, visualRoot.transform, false);
            inst.name = "ModeloGeneral3D";
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one * 1.35f;

            var ia = inst.GetComponent<EsbirroBaseIA>(); if (ia != null) Destroy(ia);
            var col = inst.GetComponent<Collider>(); if (col != null) Destroy(col);
            var poder = inst.GetComponent<PoderEspiritual>(); if (poder != null) Destroy(poder);
        }
        else
        {
            // Torso con pechera y hombreras doradas
            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            torso.transform.SetParent(visualRoot.transform, false);
            torso.transform.localPosition = new Vector3(0, 1.4f, 0);
            torso.transform.localScale = new Vector3(0.9f, 1.1f, 0.55f);
            torso.GetComponent<Renderer>().material = matUniforme;
            Destroy(torso.GetComponent<Collider>());

            // Hombreras militares
            GameObject hombroI = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hombroI.transform.SetParent(visualRoot.transform, false);
            hombroI.transform.localPosition = new Vector3(-0.6f, 1.85f, 0);
            hombroI.transform.localScale = new Vector3(0.4f, 0.15f, 0.55f);
            hombroI.GetComponent<Renderer>().material = matFuegoFatuo;
            Destroy(hombroI.GetComponent<Collider>());

            GameObject hombroD = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hombroD.transform.SetParent(visualRoot.transform, false);
            hombroD.transform.localPosition = new Vector3(0.6f, 1.85f, 0);
            hombroD.transform.localScale = new Vector3(0.4f, 0.15f, 0.55f);
            hombroD.GetComponent<Renderer>().material = matFuegoFatuo;
            Destroy(hombroD.GetComponent<Collider>());

            // Cabeza con sombrero bicornio militar
            GameObject cabeza = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabeza.transform.SetParent(visualRoot.transform, false);
            cabeza.transform.localPosition = new Vector3(0, 2.15f, 0);
            cabeza.transform.localScale = new Vector3(0.45f, 0.55f, 0.45f);
            cabeza.GetComponent<Renderer>().material = matVendas;
            Destroy(cabeza.GetComponent<Collider>());

            // Sombrero bicornio
            GameObject bicornio = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bicornio.transform.SetParent(cabeza.transform, false);
            bicornio.transform.localPosition = new Vector3(0, 0.65f, 0);
            bicornio.transform.localScale = new Vector3(1.7f, 0.5f, 0.65f);
            bicornio.GetComponent<Renderer>().material = matUniforme;
            Destroy(bicornio.GetComponent<Collider>());

            // Ojos de fuego fatuo
            GameObject ojoI = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ojoI.transform.SetParent(cabeza.transform, false);
            ojoI.transform.localPosition = new Vector3(-0.25f, 0.15f, 0.5f);
            ojoI.transform.localScale = Vector3.one * 0.25f;
            ojoI.GetComponent<Renderer>().material = matFuegoFatuo;
            Destroy(ojoI.GetComponent<Collider>());

            GameObject ojoD = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ojoD.transform.SetParent(cabeza.transform, false);
            ojoD.transform.localPosition = new Vector3(0.25f, 0.15f, 0.5f);
            ojoD.transform.localScale = Vector3.one * 0.25f;
            ojoD.GetComponent<Renderer>().material = matFuegoFatuo;
            Destroy(ojoD.GetComponent<Collider>());
        }

        // Sable de mando
        sableObj = new GameObject("SableGeneral");
        sableObj.transform.SetParent(visualRoot.transform, false);
        sableObj.transform.localPosition = new Vector3(0.65f, 1.1f, 0.4f);
        sableObj.transform.localRotation = Quaternion.Euler(30, 0, 15);

        GameObject hoja = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hoja.transform.SetParent(sableObj.transform, false);
        hoja.transform.localPosition = new Vector3(0, 0.8f, 0);
        hoja.transform.localScale = new Vector3(0.08f, 1.8f, 0.18f);
        hoja.GetComponent<Renderer>().material = matFuegoFatuo;
        Destroy(hoja.GetComponent<Collider>());

        // Luz de fuego fatuo
        GameObject luzGo = new GameObject("LuzFuegoFatuo");
        luzGo.transform.SetParent(visualRoot.transform, false);
        luzGo.transform.localPosition = new Vector3(0, 1.8f, 0);
        luzGeneral = luzGo.AddComponent<Light>();
        luzGeneral.type = LightType.Point;
        luzGeneral.color = VerdeEsmeralda;
        luzGeneral.range = 10f;
        luzGeneral.intensity = 2.5f;
    }

    void CrearBarraVida()
    {
        canvasBarra = new GameObject("Canvas_BarraJefeGeneral");
        Canvas c = canvasBarra.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasBarra.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasBarra.AddComponent<GraphicRaycaster>();

        RectTransform panel = UIMexicana.Nuevo("PanelJefe", canvasBarra.transform);
        UIMexicana.AnclarCentro(panel, new Vector2(0.5f, 1f), new Vector2(0, -50f), new Vector2(650f, 65f));

        sliderVida = UIMexicana.SliderMex(panel, "BarraVidaJefe", new Vector2(650f, 26f), 1f, VerdeEsmeralda, null);

        txtNombre = UIMexicana.Texto("NombreJefe", panel, "EL GENERAL MOMIA", 24, OroViejo, TextAlignmentOptions.Center, FontStyles.Bold, 2f);
        UIMexicana.AnclarCentro(txtNombre.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, 18f), new Vector2(400f, 30f));

        txtSubtitulo = UIMexicana.Texto("SubJefe", panel, "Centinela del Panteón de Santa Paula", 15, UIMexicana.Crema, TextAlignmentOptions.Center, FontStyles.Italic, 1f);
        UIMexicana.AnclarCentro(txtSubtitulo.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, -18f), new Vector2(400f, 25f));

        canvasBarra.SetActive(false);
    }
}
