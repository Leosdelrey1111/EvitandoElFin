using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Menú principal de "Evitando el Fin": noche de Guanajuato (cerros, casitas de colores, la Basílica y luna),
/// papel picado, pétalos de cempasúchil, calavera, y paneles de Opciones, Controles y Créditos.
/// Se construye completo por código al iniciar la escena (basta con tener este componente en la escena).
/// </summary>
public class MenuMexicano : MonoBehaviour
{
    [Header("Escenas")]
    [Tooltip("Primera escena del juego (la cinemática del accidente).")]
    public string escenaJuego = "SampleScene";

    [Header("Textos")]
    public string titulo = "EVITANDO EL FIN";
    public string subtitulo = "UNA LEYENDA DE LA SIERRA DE GUANAJUATO";

    // --- UI
    Canvas canvas;
    RectTransform raizMenu;
    Image fundido;
    readonly List<RectTransform> capasParalaje = new List<RectTransform>();
    readonly List<float> factoresParalaje = new List<float>();
    readonly List<Vector2> basesParalaje = new List<Vector2>();
    Vector2 mouseSuave;

    // estrellas
    Image[] estrellas; float[] estFase, estVel, estBase;
    // pétalos
    RectTransform[] petalos; Vector2[] petPos; float[] petVel, petFase, petAmp, petGiro;
    // banderas
    RectTransform[] banderas; float[] banFase;
    // emblema
    RectTransform florEmblema;

    GameObject modalActual;
    readonly List<GameObject> modales = new List<GameObject>();
    bool cargando;
    AudioSource musica;
    Button primerBoton;

    void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        UIMexicana.AsegurarEventSystem();
        ConfigurarCamara();
        Construir();
        IniciarMusica();
        StartCoroutine(Intro());
    }

    void ConfigurarCamara()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = UIMexicana.NocheProfunda;
        cam.cullingMask = 0;
        foreach (Light l in Object.FindObjectsByType<Light>()) l.enabled = false;
    }

    // ================================================================== CONSTRUCCIÓN

    void Construir()
    {
        canvas = UIMexicana.CrearCanvas("Canvas_MenuPrincipal", 0);
        canvas.transform.SetParent(transform, false);
        RectTransform raiz = (RectTransform)canvas.transform;

        // ---------- cielo
        Image cielo = UIMexicana.Imagen("Cielo", raiz, UIMexicana.DegradadoVertical("cielo_menu", UIMexicana.H("05071A"), UIMexicana.H("151A42"), UIMexicana.H("5A1F55")), Color.white);
        UIMexicana.Estirar(cielo.rectTransform);

        CrearEstrellas(raiz);

        // luna con halo
        Image halo = UIMexicana.Imagen("HaloLuna", raiz, UIMexicana.Brillo(), new Color(1f, 0.85f, 0.6f, 0.35f));
        UIMexicana.Anclar(halo.rectTransform, new Vector2(1f, 1f), new Vector2(-190f, -120f), new Vector2(620f, 620f));
        halo.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        halo.rectTransform.anchoredPosition = new Vector2(-300f, -260f);
        Image luna = UIMexicana.Imagen("Luna", raiz, UIMexicana.Circulo(), UIMexicana.H("FFF1D0"));
        UIMexicana.AnclarCentro(luna.rectTransform, new Vector2(1f, 1f), new Vector2(-300f, -260f), new Vector2(150f, 150f));
        Image lunaSombra = UIMexicana.Imagen("LunaSombra", luna.transform, UIMexicana.Circulo(), new Color(0.9f, 0.8f, 0.62f, 0.55f));
        UIMexicana.AnclarCentro(lunaSombra.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(26f, -18f), new Vector2(46f, 46f));

        // ---------- paisaje en capas con paralaje
        AgregarCapa(raiz, "CerrosLejanos", PaisajeGuanajuato.Cerros(1920, 520, 3.1f, 150f, 290f), UIMexicana.H("2A2A63"), 520f, 8f);
        AgregarCapa(raiz, "CerrosMedios", PaisajeGuanajuato.Cerros(1920, 420, 11.7f, 100f, 230f), UIMexicana.H("1B2052"), 420f, 16f);
        AgregarCapa(raiz, "Ciudad", PaisajeGuanajuato.Ciudad(1920, 640, 70f, 1390f), Color.white, 640f, 26f);
        AgregarCapa(raiz, "PrimerPlano", PaisajeGuanajuato.PrimerPlano(1920, 300), Color.white, 300f, 44f);

        CrearPetalos(raiz);

        // viñeta general
        Image vineta = UIMexicana.Imagen("Vineta", raiz, UIMexicana.Vineta(), UIMexicana.ConAlfa(UIMexicana.NocheProfunda, 0.6f));
        UIMexicana.Estirar(vineta.rectTransform);
        // sombra lateral para dar contraste al menú
        Image lateral = UIMexicana.Imagen("SombraLateral", raiz, UIMexicana.DegradadoHorizontal("lateral", UIMexicana.ConAlfa(UIMexicana.NocheProfunda, 0.82f), UIMexicana.ConAlfa(UIMexicana.NocheProfunda, 0f)), Color.white);
        UIMexicana.Anclar(lateral.rectTransform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(1000f, 1080f));
        lateral.rectTransform.anchorMin = new Vector2(0f, 0f); lateral.rectTransform.anchorMax = new Vector2(0f, 1f);
        lateral.rectTransform.pivot = new Vector2(0f, 0.5f); lateral.rectTransform.sizeDelta = new Vector2(1000f, 0f);

        // ---------- menú principal
        raizMenu = UIMexicana.Nuevo("MenuPrincipal", raiz);
        UIMexicana.Estirar(raizMenu);
        ConstruirPanelPrincipal(raizMenu);

        CrearPapelPicado(raiz);

        // pie
        TextMeshProUGUI pie = UIMexicana.Texto("Pie", raiz, "UTNG  ·  Creación de Videojuegos  ·  10° cuatrimestre", 18, UIMexicana.ConAlfa(UIMexicana.Crema, 0.55f), TextAlignmentOptions.Left, FontStyles.Normal, 2f);
        UIMexicana.Anclar(pie.rectTransform, new Vector2(0f, 0f), new Vector2(60f, 28f), new Vector2(900f, 30f));
        TextMeshProUGUI ver = UIMexicana.Texto("Version", raiz, "v0.2  ·  prototipo", 18, UIMexicana.ConAlfa(UIMexicana.Crema, 0.45f), TextAlignmentOptions.Right, FontStyles.Normal, 2f);
        UIMexicana.Anclar(ver.rectTransform, new Vector2(1f, 0f), new Vector2(-60f, 28f), new Vector2(500f, 30f));

        // ---------- modales
        ConstruirOpciones(raiz);
        ConstruirControles(raiz);
        ConstruirCreditos(raiz);

        // fundido inicial
        fundido = UIMexicana.Imagen("Fundido", raiz, UIMexicana.Blanco(), Color.black, false, true);
        UIMexicana.Estirar(fundido.rectTransform);
    }

    void AgregarCapa(RectTransform padre, string nombre, Sprite s, Color color, float alto, float factor)
    {
        Image img = UIMexicana.Imagen(nombre, padre, s, color);
        RectTransform rt = img.rectTransform;
        rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(2 * factor + 40f, alto);
        rt.anchoredPosition = Vector2.zero;
        capasParalaje.Add(rt); factoresParalaje.Add(factor); basesParalaje.Add(Vector2.zero);
    }

    void CrearEstrellas(RectTransform padre)
    {
        int n = 130;
        estrellas = new Image[n]; estFase = new float[n]; estVel = new float[n]; estBase = new float[n];
        RectTransform cont = UIMexicana.Nuevo("Estrellas", padre);
        UIMexicana.Estirar(cont);
        System.Random rng = new System.Random(7);
        for (int i = 0; i < n; i++)
        {
            float s = 2f + (float)rng.NextDouble() * 3.2f;
            Image img = UIMexicana.Imagen("e" + i, cont, UIMexicana.Circulo(), Color.white);
            UIMexicana.Anclar(img.rectTransform, new Vector2(0f, 1f), new Vector2((float)rng.NextDouble() * 1920f, -(float)rng.NextDouble() * 620f), new Vector2(s, s));
            estrellas[i] = img;
            estFase[i] = (float)rng.NextDouble() * 6.28f;
            estVel[i] = 0.6f + (float)rng.NextDouble() * 2f;
            estBase[i] = 0.35f + (float)rng.NextDouble() * 0.6f;
        }
    }

    void CrearPetalos(RectTransform padre)
    {
        int n = 26;
        petalos = new RectTransform[n]; petPos = new Vector2[n];
        petVel = new float[n]; petFase = new float[n]; petAmp = new float[n]; petGiro = new float[n];
        RectTransform cont = UIMexicana.Nuevo("Petalos", padre);
        UIMexicana.Estirar(cont);
        System.Random rng = new System.Random(21);
        Color[] cols = { UIMexicana.Cempasuchil, UIMexicana.Dorado, UIMexicana.H("FF7A1C"), UIMexicana.Rosa };
        for (int i = 0; i < n; i++)
        {
            float s = 0.5f + (float)rng.NextDouble() * 0.9f;
            Color c = cols[rng.Next(cols.Length)]; c.a = 0.55f + (float)rng.NextDouble() * 0.35f;
            Image img = UIMexicana.Imagen("p" + i, cont, UIMexicana.Petalo(), c);
            RectTransform rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(22f * s, 34f * s);
            petalos[i] = rt;
            petPos[i] = new Vector2((float)rng.NextDouble() * 1920f, -(float)rng.NextDouble() * 1080f);
            petVel[i] = 30f + (float)rng.NextDouble() * 55f;
            petFase[i] = (float)rng.NextDouble() * 6.28f;
            petAmp[i] = 20f + (float)rng.NextDouble() * 50f;
            petGiro[i] = -80f + (float)rng.NextDouble() * 160f;
        }
    }

    void CrearPapelPicado(RectTransform padre)
    {
        RectTransform cont = UIMexicana.Nuevo("PapelPicado", padre);
        UIMexicana.Estirar(cont);

        int n = 17;
        float ancho = 1920f;
        float sag = 46f;
        Vector2 Punto(float x) { float t = x / ancho * 2f - 1f; return new Vector2(x, -18f - sag * (1f - t * t)); }

        // hilo (segmentos siguiendo la curva)
        int seg = 48;
        for (int i = 0; i < seg; i++)
        {
            Vector2 a = Punto(i * ancho / seg), b = Punto((i + 1) * ancho / seg);
            Image s = UIMexicana.Imagen("hilo" + i, cont, UIMexicana.Blanco(), UIMexicana.ConAlfa(UIMexicana.Crema, 0.55f));
            RectTransform rt = s.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            Vector2 d = b - a;
            rt.sizeDelta = new Vector2(d.magnitude + 1f, 2.5f);
            rt.anchoredPosition = a;
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        }

        banderas = new RectTransform[n]; banFase = new float[n];
        for (int i = 0; i < n; i++)
        {
            float x = (i + 0.5f) * ancho / n;
            Vector2 p = Punto(x);
            Color c = UIMexicana.ColoresPapelPicado[i % UIMexicana.ColoresPapelPicado.Length];
            c.a = 0.95f;
            Image f = UIMexicana.Imagen("bandera" + i, cont, UIMexicana.BanderaPapelPicado(i % 3), c);
            RectTransform rt = f.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(112f, 78f);
            rt.anchoredPosition = p + new Vector2(0f, 2f);
            banderas[i] = rt;
            banFase[i] = i * 0.8f;
        }
    }

    // ---------------------------------------------------------------- panel principal

    void ConstruirPanelPrincipal(RectTransform raiz)
    {
        const float x0 = 130f;

        // emblema: flor de cempasúchil + calavera
        florEmblema = UIMexicana.Imagen("EmblemaFlor", raiz, UIMexicana.IconoSprite(UIMexicana.Icono.Flor), UIMexicana.ConAlfa(UIMexicana.Cempasuchil, 0.95f)).rectTransform;
        UIMexicana.Anclar(florEmblema, new Vector2(0f, 1f), new Vector2(x0 - 10f, -150f), new Vector2(190f, 190f));
        florEmblema.pivot = new Vector2(0.5f, 0.5f);
        florEmblema.anchoredPosition = new Vector2(x0 + 85f, -250f);
        Image discoCal = UIMexicana.Imagen("DiscoCalavera", raiz, UIMexicana.Circulo(), UIMexicana.H("12183C"));
        UIMexicana.AnclarCentro(discoCal.rectTransform, new Vector2(0f, 1f), new Vector2(x0 + 85f, -250f), new Vector2(122f, 122f));
        Image calavera = UIMexicana.Imagen("Calavera", raiz, UIMexicana.IconoSprite(UIMexicana.Icono.Calavera), UIMexicana.Crema);
        UIMexicana.AnclarCentro(calavera.rectTransform, new Vector2(0f, 1f), new Vector2(x0 + 85f, -250f), new Vector2(100f, 100f));

        // título con "neón" rosa detrás
        TextMeshProUGUI sombraT = UIMexicana.Texto("TituloSombra", raiz, titulo, 84, UIMexicana.ConAlfa(UIMexicana.Rosa, 0.9f), TextAlignmentOptions.Left, FontStyles.Bold, 6f);
        UIMexicana.Anclar(sombraT.rectTransform, new Vector2(0f, 1f), new Vector2(x0 + 4f, -366f), new Vector2(1000f, 110f));
        TextMeshProUGUI t = UIMexicana.Texto("Titulo", raiz, titulo, 84, Color.white, TextAlignmentOptions.Left, FontStyles.Bold, 6f);
        UIMexicana.Anclar(t.rectTransform, new Vector2(0f, 1f), new Vector2(x0, -362f), new Vector2(1000f, 110f));
        t.enableVertexGradient = true;
        t.colorGradient = new VertexGradient(UIMexicana.Crema, UIMexicana.Crema, UIMexicana.Dorado, UIMexicana.Cempasuchil);

        TextMeshProUGUI sub = UIMexicana.Texto("Subtitulo", raiz, subtitulo, 22, UIMexicana.Turquesa, TextAlignmentOptions.Left, FontStyles.Bold, 7f);
        UIMexicana.Anclar(sub.rectTransform, new Vector2(0f, 1f), new Vector2(x0 + 4f, -462f), new Vector2(1000f, 36f));

        // divisor
        Image linea = UIMexicana.Imagen("Divisor", raiz, UIMexicana.DegradadoHorizontal("div", UIMexicana.ConAlfa(UIMexicana.Dorado, 0.9f), UIMexicana.ConAlfa(UIMexicana.Dorado, 0f)), Color.white);
        UIMexicana.Anclar(linea.rectTransform, new Vector2(0f, 1f), new Vector2(x0 + 4f, -508f), new Vector2(560f, 3f));
        Image florDiv = UIMexicana.Imagen("DivisorFlor", raiz, UIMexicana.IconoSprite(UIMexicana.Icono.Flor), UIMexicana.Cempasuchil);
        UIMexicana.AnclarCentro(florDiv.rectTransform, new Vector2(0f, 1f), new Vector2(x0 + 4f + 14f, -509f), new Vector2(26f, 26f));

        // botones
        RectTransform lista = UIMexicana.Nuevo("Botones", raiz);
        UIMexicana.Anclar(lista, new Vector2(0f, 1f), new Vector2(x0, -544f), new Vector2(520f, 440f));
        VerticalLayoutGroup v = lista.gameObject.AddComponent<VerticalLayoutGroup>();
        v.spacing = 14f;
        v.childAlignment = TextAnchor.UpperLeft;
        v.childControlWidth = false; v.childControlHeight = false;
        v.childForceExpandWidth = false; v.childForceExpandHeight = false;

        Vector2 tamBoton = new Vector2(500f, 66f);
        primerBoton = UIMexicana.Boton("BtnJugar", lista, "COMENZAR AVENTURA", tamBoton, UIMexicana.Rosa, Jugar);
        UIMexicana.Boton("BtnOpciones", lista, "OPCIONES", tamBoton, UIMexicana.Turquesa, () => AbrirModal("Opciones"));
        UIMexicana.Boton("BtnControles", lista, "CONTROLES", tamBoton, UIMexicana.Cempasuchil, () => AbrirModal("Controles"));
        UIMexicana.Boton("BtnCreditos", lista, "CRÉDITOS", tamBoton, UIMexicana.Violeta, () => AbrirModal("Creditos"));
        UIMexicana.Boton("BtnSalir", lista, "SALIR", tamBoton, UIMexicana.Rojo, Salir);
    }

    // ================================================================== MODALES

    RectTransform CrearModal(RectTransform padre, string clave, string tituloModal, Vector2 tam)
    {
        RectTransform raiz = UIMexicana.Nuevo("Modal_" + clave, padre);
        UIMexicana.Estirar(raiz);
        CanvasGroup cg = raiz.gameObject.AddComponent<CanvasGroup>();

        Image atenuar = UIMexicana.Imagen("Atenuar", raiz, UIMexicana.Blanco(), UIMexicana.ConAlfa(UIMexicana.NocheProfunda, 0.82f), false, true);
        UIMexicana.Estirar(atenuar.rectTransform);

        RectTransform tarjeta = UIMexicana.Nuevo("Tarjeta", raiz);
        UIMexicana.AnclarCentro(tarjeta, new Vector2(0.5f, 0.5f), Vector2.zero, tam);

        Image sombra = UIMexicana.Imagen("Sombra", tarjeta, UIMexicana.Redondeado(30), new Color(0f, 0f, 0f, 0.5f), true);
        UIMexicana.Estirar(sombra.rectTransform);
        sombra.rectTransform.offsetMin = new Vector2(-14f, -22f); sombra.rectTransform.offsetMax = new Vector2(14f, 6f);
        Image fondo = UIMexicana.Imagen("Fondo", tarjeta, UIMexicana.Redondeado(30), UIMexicana.ConAlfa(UIMexicana.Noche, 0.97f), true, true);
        UIMexicana.Estirar(fondo.rectTransform);
        Image borde = UIMexicana.Imagen("Borde", tarjeta, UIMexicana.RedondeadoBorde(30, 2.5f), UIMexicana.ConAlfa(UIMexicana.Dorado, 0.85f), true);
        UIMexicana.Estirar(borde.rectTransform);
        Image bordeInt = UIMexicana.Imagen("BordeInterior", tarjeta, UIMexicana.RedondeadoBorde(22, 1.2f), UIMexicana.ConAlfa(UIMexicana.Rosa, 0.55f), true);
        UIMexicana.Estirar(bordeInt.rectTransform);
        bordeInt.rectTransform.offsetMin = new Vector2(10f, 10f); bordeInt.rectTransform.offsetMax = new Vector2(-10f, -10f);

        // banda superior de papel picado en miniatura
        for (int i = 0; i < 9; i++)
        {
            Color c = UIMexicana.ColoresPapelPicado[i % UIMexicana.ColoresPapelPicado.Length]; c.a = 0.9f;
            Image f = UIMexicana.Imagen("mini" + i, tarjeta, UIMexicana.BanderaPapelPicado(i % 3), c);
            RectTransform rt = f.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(60f, 42f);
            rt.anchoredPosition = new Vector2((i - 4) * 66f, -14f);
        }

        // título
        TextMeshProUGUI tt = UIMexicana.Texto("Titulo", tarjeta, tituloModal, 46, UIMexicana.Dorado, TextAlignmentOptions.Center, FontStyles.Bold, 6f);
        UIMexicana.Anclar(tt.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -66f), new Vector2(tam.x - 100f, 64f));
        tt.rectTransform.pivot = new Vector2(0.5f, 1f);
        Image fl1 = UIMexicana.Imagen("FlorI", tarjeta, UIMexicana.IconoSprite(UIMexicana.Icono.Flor), UIMexicana.Cempasuchil);
        UIMexicana.AnclarCentro(fl1.rectTransform, new Vector2(0.5f, 1f), new Vector2(-260f, -98f), new Vector2(40f, 40f));
        Image fl2 = UIMexicana.Imagen("FlorD", tarjeta, UIMexicana.IconoSprite(UIMexicana.Icono.Flor), UIMexicana.Cempasuchil);
        UIMexicana.AnclarCentro(fl2.rectTransform, new Vector2(0.5f, 1f), new Vector2(260f, -98f), new Vector2(40f, 40f));

        // contenido
        RectTransform contenido = UIMexicana.Nuevo("Contenido", tarjeta);
        UIMexicana.Estirar(contenido);
        contenido.offsetMin = new Vector2(60f, 110f); contenido.offsetMax = new Vector2(-60f, -150f);

        // botón volver
        Button volver = UIMexicana.Boton("BtnVolver", tarjeta, "VOLVER", new Vector2(300f, 60f), UIMexicana.Turquesa, () => CerrarModal(), 26f);
        UIMexicana.Anclar(volver.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(300f, 60f));
        volver.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);

        raiz.gameObject.SetActive(false);
        modales.Add(raiz.gameObject);
        modalesPorClave[clave] = raiz.gameObject;
        return contenido;
    }

    readonly Dictionary<string, GameObject> modalesPorClave = new Dictionary<string, GameObject>();

    void AbrirModal(string clave)
    {
        GameObject m;
        if (!modalesPorClave.TryGetValue(clave, out m)) return;
        modalActual = m;
        StartCoroutine(AnimarModal(m, true));
    }

    void CerrarModal()
    {
        if (modalActual == null) return;
        UIMexicana.Sonar("atras");
        OpcionesJuego.Guardar();
        GameObject m = modalActual;
        modalActual = null;
        StartCoroutine(AnimarModal(m, false));
    }

    IEnumerator AnimarModal(GameObject m, bool abrir)
    {
        CanvasGroup cg = m.GetComponent<CanvasGroup>();
        RectTransform tarjeta = (RectTransform)m.transform.Find("Tarjeta");
        if (abrir) m.SetActive(true);
        float t = 0f, dur = 0.22f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            float e = abrir ? Mathf.SmoothStep(0f, 1f, k) : 1f - Mathf.SmoothStep(0f, 1f, k);
            cg.alpha = e;
            tarjeta.localScale = Vector3.one * Mathf.Lerp(0.93f, 1f, e);
            yield return null;
        }
        cg.alpha = abrir ? 1f : 0f;
        if (!abrir) m.SetActive(false);
        else
        {
            Button b = m.transform.Find("Tarjeta/BtnVolver").GetComponent<Button>();
            if (UnityEngine.EventSystems.EventSystem.current != null) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(b.gameObject);
        }
    }

    // ---------------------------------------------------------------- OPCIONES

    void ConstruirOpciones(RectTransform raiz)
    {
        RectTransform c = CrearModal(raiz, "Opciones", "OPCIONES", new Vector2(1000f, 780f));
        float y = 0f;
        const float paso = 86f;

        FilaSlider(c, ref y, "VOLUMEN GENERAL", OpcionesJuego.Master, UIMexicana.Rosa, v => OpcionesJuego.Master = v, v => Mathf.RoundToInt(v * 100) + "%");
        FilaSlider(c, ref y, "MÚSICA", OpcionesJuego.Musica, UIMexicana.Turquesa, v => OpcionesJuego.Musica = v, v => Mathf.RoundToInt(v * 100) + "%");
        FilaSlider(c, ref y, "EFECTOS", OpcionesJuego.Efectos, UIMexicana.Cempasuchil, v => OpcionesJuego.Efectos = v, v => Mathf.RoundToInt(v * 100) + "%");
        FilaSlider(c, ref y, "SENSIBILIDAD DE CÁMARA", (OpcionesJuego.Sensibilidad - 0.3f) / 1.7f, UIMexicana.Violeta,
            v => OpcionesJuego.Sensibilidad = 0.3f + 1.7f * v, v => "x" + (0.3f + 1.7f * v).ToString("0.0"));

        // Calidad (segmentado)
        RectTransform filaCal = Fila(c, y, "CALIDAD GRÁFICA");
        string[] nombres = { "BAJA", "MEDIA", "ALTA" };
        Button[] botones = new Button[3];
        Image[] fondos = new Image[3];
        System.Action refrescar = () =>
        {
            for (int i = 0; i < 3; i++)
            {
                bool sel = OpcionesJuego.Calidad == i;
                fondos[i].color = sel ? UIMexicana.ConAlfa(UIMexicana.Turquesa, 0.95f) : new Color(0.05f, 0.08f, 0.2f, 0.9f);
                botones[i].GetComponentInChildren<TextMeshProUGUI>().color = sel ? UIMexicana.NocheProfunda : UIMexicana.Crema;
            }
        };
        for (int i = 0; i < 3; i++)
        {
            int idx = i;
            botones[i] = Segmento(filaCal, nombres[i], -140f - (2 - i) * 150f, new Vector2(140f, 54f), () => { OpcionesJuego.Calidad = idx; UIMexicana.Sonar("click"); refrescar(); }, out fondos[i]);
        }
        refrescar();
        y += paso;

        // Pantalla completa (interruptor)
        RectTransform filaPan = Fila(c, y, "PANTALLA COMPLETA");
        Image fondoSw;
        Button sw = Segmento(filaPan, OpcionesJuego.PantallaCompleta ? "SÍ" : "NO", -140f, new Vector2(140f, 54f), null, out fondoSw);
        System.Action pintarSw = () =>
        {
            bool on = OpcionesJuego.PantallaCompleta;
            sw.GetComponentInChildren<TextMeshProUGUI>().text = on ? "SÍ" : "NO";
            fondoSw.color = on ? UIMexicana.ConAlfa(UIMexicana.Verde, 0.95f) : new Color(0.05f, 0.08f, 0.2f, 0.9f);
            sw.GetComponentInChildren<TextMeshProUGUI>().color = on ? UIMexicana.NocheProfunda : UIMexicana.Crema;
        };
        sw.onClick.AddListener(() => { OpcionesJuego.PantallaCompleta = !OpcionesJuego.PantallaCompleta; UIMexicana.Sonar("click"); pintarSw(); });
        pintarSw();
    }

    RectTransform Fila(RectTransform contenido, float y, string etiqueta)
    {
        RectTransform fila = UIMexicana.Nuevo("Fila_" + etiqueta, contenido);
        fila.anchorMin = new Vector2(0f, 1f); fila.anchorMax = new Vector2(1f, 1f);
        fila.pivot = new Vector2(0.5f, 1f);
        fila.anchoredPosition = new Vector2(0f, -y);
        fila.sizeDelta = new Vector2(0f, 70f);
        TextMeshProUGUI l = UIMexicana.Texto("Etiqueta", fila, etiqueta, 24, UIMexicana.Crema, TextAlignmentOptions.Left, FontStyles.Bold, 3f);
        UIMexicana.Anclar(l.rectTransform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(380f, 50f));
        l.rectTransform.pivot = new Vector2(0f, 0.5f);
        // línea tenue inferior
        Image ln = UIMexicana.Imagen("Linea", fila, UIMexicana.Blanco(), new Color(1f, 1f, 1f, 0.06f));
        ln.rectTransform.anchorMin = new Vector2(0f, 0f); ln.rectTransform.anchorMax = new Vector2(1f, 0f);
        ln.rectTransform.sizeDelta = new Vector2(0f, 2f); ln.rectTransform.anchoredPosition = new Vector2(0f, -8f);
        return fila;
    }

    void FilaSlider(RectTransform contenido, ref float y, string etiqueta, float valor, Color color, System.Action<float> alCambiar, System.Func<float, string> formato)
    {
        RectTransform fila = Fila(contenido, y, etiqueta);
        TextMeshProUGUI val = UIMexicana.Texto("Valor", fila, formato(valor), 24, UIMexicana.Dorado, TextAlignmentOptions.Right, FontStyles.Bold, 1f);
        UIMexicana.Anclar(val.rectTransform, new Vector2(1f, 0.5f), Vector2.zero, new Vector2(120f, 50f));
        val.rectTransform.pivot = new Vector2(1f, 0.5f);

        Slider s = UIMexicana.SliderMex(fila, "Slider", new Vector2(360f, 40f), valor, color, v => { alCambiar(v); val.text = formato(v); });
        RectTransform srt = (RectTransform)s.transform;
        srt.anchorMin = srt.anchorMax = new Vector2(1f, 0.5f);
        srt.pivot = new Vector2(1f, 0.5f);
        srt.anchoredPosition = new Vector2(-140f, 0f);
        y += 86f;
    }

    Button Segmento(RectTransform padre, string texto, float xDerecha, Vector2 tam, UnityEngine.Events.UnityAction alClick, out Image fondo)
    {
        RectTransform rt = UIMexicana.Nuevo("Seg_" + texto, padre);
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.anchoredPosition = new Vector2(xDerecha, 0f);
        rt.sizeDelta = tam;
        fondo = rt.gameObject.AddComponent<Image>();
        fondo.sprite = UIMexicana.Redondeado(12); fondo.type = Image.Type.Sliced;
        fondo.color = new Color(0.05f, 0.08f, 0.2f, 0.9f);
        Image borde = UIMexicana.Imagen("Borde", rt, UIMexicana.RedondeadoBorde(12, 1.6f), UIMexicana.ConAlfa(UIMexicana.Dorado, 0.55f), true);
        UIMexicana.Estirar(borde.rectTransform);
        Button b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = fondo; b.transition = Selectable.Transition.None;
        TextMeshProUGUI t = UIMexicana.Texto("Texto", rt, texto, 22, UIMexicana.Crema, TextAlignmentOptions.Center, FontStyles.Bold, 2f);
        UIMexicana.Estirar(t.rectTransform);
        if (alClick != null) b.onClick.AddListener(alClick);
        return b;
    }

    // ---------------------------------------------------------------- CONTROLES

    void ConstruirControles(RectTransform raiz)
    {
        RectTransform c = CrearModal(raiz, "Controles", "CONTROLES", new Vector2(1060f, 760f));

        string[][] filas =
        {
            new[] { "W A S D", "Moverte" },
            new[] { "SHIFT", "Correr" },
            new[] { "ESPACIO", "Saltar" },
            new[] { "CTRL  /  C", "Esquivar (ráfaga de invulnerabilidad)" },
            new[] { "CLICK  /  F", "Golpear: combo de 3 golpes" },
            new[] { "Q", "Onda espiritual (daño en área)" },
            new[] { "ESC", "Pausa" },
        };
        for (int i = 0; i < filas.Length; i++)
        {
            RectTransform fila = UIMexicana.Nuevo("Control" + i, c);
            fila.anchorMin = new Vector2(0f, 1f); fila.anchorMax = new Vector2(1f, 1f);
            fila.pivot = new Vector2(0.5f, 1f);
            fila.anchoredPosition = new Vector2(0f, -i * 70f);
            fila.sizeDelta = new Vector2(0f, 60f);

            string[] teclas = filas[i][0].Split(new[] { "  /  " }, System.StringSplitOptions.None);
            float x = 0f;
            for (int k = 0; k < teclas.Length; k++)
            {
                string[] partes = teclas[k].Split(' ');
                foreach (string p in partes)
                {
                    if (p.Length == 0) continue;
                    RectTransform tecla = UIMexicana.Tecla(fila, p, 50f, 22f);
                    tecla.anchorMin = tecla.anchorMax = new Vector2(0f, 0.5f);
                    tecla.pivot = new Vector2(0f, 0.5f);
                    tecla.anchoredPosition = new Vector2(x, 0f);
                    x += tecla.sizeDelta.x + 10f;
                }
                if (k < teclas.Length - 1)
                {
                    TextMeshProUGUI o = UIMexicana.Texto("o", fila, "o", 20, UIMexicana.ConAlfa(UIMexicana.Crema, 0.6f));
                    UIMexicana.Anclar(o.rectTransform, new Vector2(0f, 0.5f), new Vector2(x, 0f), new Vector2(30f, 30f));
                    o.rectTransform.pivot = new Vector2(0f, 0.5f);
                    x += 38f;
                }
            }
            TextMeshProUGUI d = UIMexicana.Texto("Descripcion", fila, filas[i][1], 26, UIMexicana.Crema, TextAlignmentOptions.Left, FontStyles.Normal, 1f);
            UIMexicana.Anclar(d.rectTransform, new Vector2(0f, 0.5f), new Vector2(380f, 0f), new Vector2(560f, 50f));
            d.rectTransform.pivot = new Vector2(0f, 0.5f);
        }

        TextMeshProUGUI meta = UIMexicana.Texto("Meta", c, "<color=#F4C95D>MISIÓN</color>   Reúne los 3 tótems de la sierra y cruza el portal espiritual.", 24,
            UIMexicana.ConAlfa(UIMexicana.Crema, 0.9f), TextAlignmentOptions.Center);
        UIMexicana.Anclar(meta.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(900f, 40f));
        meta.rectTransform.pivot = new Vector2(0.5f, 0f);
    }

    // ---------------------------------------------------------------- CRÉDITOS

    void ConstruirCreditos(RectTransform raiz)
    {
        RectTransform c = CrearModal(raiz, "Creditos", "CRÉDITOS", new Vector2(1060f, 760f));

        float y = 0f;
        System.Action<string, string, string, Color> persona = (nombre, rol, detalle, color) =>
        {
            RectTransform tarj = UIMexicana.Nuevo("Persona", c);
            tarj.anchorMin = new Vector2(0f, 1f); tarj.anchorMax = new Vector2(1f, 1f);
            tarj.pivot = new Vector2(0.5f, 1f);
            tarj.anchoredPosition = new Vector2(0f, -y);
            tarj.sizeDelta = new Vector2(0f, 128f);
            Image f = UIMexicana.Imagen("Fondo", tarj, UIMexicana.Redondeado(16), new Color(0.08f, 0.11f, 0.26f, 0.9f), true);
            UIMexicana.Estirar(f.rectTransform);
            Image barra = UIMexicana.Imagen("Barra", tarj, UIMexicana.Redondeado(4), color, true);
            UIMexicana.Anclar(barra.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(8f, 100f));
            barra.rectTransform.pivot = new Vector2(0f, 0.5f);
            TextMeshProUGUI n = UIMexicana.Texto("Nombre", tarj, nombre, 30, UIMexicana.Crema, TextAlignmentOptions.Left, FontStyles.Bold, 1f);
            UIMexicana.Anclar(n.rectTransform, new Vector2(0f, 1f), new Vector2(34f, -14f), new Vector2(900f, 40f));
            TextMeshProUGUI r = UIMexicana.Texto("Rol", tarj, rol, 22, color, TextAlignmentOptions.Left, FontStyles.Bold, 3f);
            UIMexicana.Anclar(r.rectTransform, new Vector2(0f, 1f), new Vector2(34f, -54f), new Vector2(900f, 32f));
            TextMeshProUGUI d = UIMexicana.Texto("Detalle", tarj, detalle, 20, UIMexicana.ConAlfa(UIMexicana.Crema, 0.75f), TextAlignmentOptions.Left);
            UIMexicana.Anclar(d.rectTransform, new Vector2(0f, 1f), new Vector2(34f, -88f), new Vector2(900f, 30f));
            y += 142f;
        };
        persona("Brayan Leonel Hernández Díaz", "PROGRAMACIÓN Y SISTEMAS", "Lógica de juego, IA, combate y compilación multiplataforma", UIMexicana.Rosa);
        persona("Celeste Estefanía Ramírez Matehuala", "ARTE, UI Y AUDIO", "Arte 3D, escenarios, texturizado, interfaz, audio y playtesting", UIMexicana.Turquesa);

        TextMeshProUGUI uni = UIMexicana.Texto("Universidad", c,
            "Universidad Tecnológica del Norte de Guanajuato\nCreación de Videojuegos  ·  10° cuatrimestre  ·  GIDS6101", 24, UIMexicana.Dorado, TextAlignmentOptions.Center, FontStyles.Bold, 1f);
        UIMexicana.Anclar(uni.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -y - 14f), new Vector2(940f, 80f));
        uni.rectTransform.pivot = new Vector2(0.5f, 1f);

        TextMeshProUGUI herr = UIMexicana.Texto("Herramientas", c,
            "Hecho con Unity  ·  Blender  ·  Meshy AI  ·  REAPER  ·  Inkscape  ·  Ibis Paint\nInspirado en las leyendas y la sierra de Guanajuato, México.", 20,
            UIMexicana.ConAlfa(UIMexicana.Crema, 0.7f), TextAlignmentOptions.Center);
        UIMexicana.Anclar(herr.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -y - 108f), new Vector2(940f, 70f));
        herr.rectTransform.pivot = new Vector2(0.5f, 1f);
    }

    // ================================================================== ACCIONES

    void Jugar()
    {
        if (cargando) return;
        cargando = true;
        StartCoroutine(CargarEscena());
    }

    IEnumerator CargarEscena()
    {
        float t = 0f, dur = 0.7f;
        float vol = musica != null ? musica.volume : 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            fundido.color = new Color(0f, 0f, 0f, k);
            if (musica != null) musica.volume = Mathf.Lerp(vol, 0f, k);
            yield return null;
        }
        if (Application.CanStreamedLevelBeLoaded(escenaJuego)) SceneManager.LoadScene(escenaJuego);
        else
        {
            Debug.LogError("[MenuMexicano] La escena '" + escenaJuego + "' no está en Build Settings.");
            cargando = false;
            fundido.color = new Color(0, 0, 0, 0);
        }
    }

    void Salir()
    {
        Debug.Log("[MenuMexicano] Saliendo del juego...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    IEnumerator Intro()
    {
        fundido.color = Color.black;
        yield return null;
        float t = 0f;
        // aparición escalonada de los elementos del menú
        CanvasGroup cg = raizMenu.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        RectTransform lista = (RectTransform)raizMenu.Find("Botones");
        Vector2 baseLista = lista.anchoredPosition;
        while (t < 1.4f)
        {
            t += Time.unscaledDeltaTime;
            fundido.color = new Color(0f, 0f, 0f, 1f - Mathf.Clamp01(t / 0.9f));
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.3f) / 0.9f));
            cg.alpha = k;
            lista.anchoredPosition = baseLista + new Vector2(-50f * (1f - k), 0f);
            yield return null;
        }
        fundido.color = new Color(0, 0, 0, 0);
        fundido.raycastTarget = false;
        lista.anchoredPosition = baseLista;
        if (primerBoton != null && UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(primerBoton.gameObject);
    }

    // ================================================================== ANIMACIÓN Y AUDIO

    void Update()
    {
        float t = Time.unscaledTime;

        // paralaje con el ratón
        Vector2 m = new Vector2(Input.mousePosition.x / Mathf.Max(1, Screen.width), Input.mousePosition.y / Mathf.Max(1, Screen.height)) - new Vector2(0.5f, 0.5f);
        mouseSuave = Vector2.Lerp(mouseSuave, m, 1f - Mathf.Exp(-3f * Time.unscaledDeltaTime));
        for (int i = 0; i < capasParalaje.Count; i++)
            capasParalaje[i].anchoredPosition = new Vector2(-mouseSuave.x * factoresParalaje[i] * 2f, -mouseSuave.y * factoresParalaje[i] * 0.6f);

        // estrellas
        for (int i = 0; i < estrellas.Length; i++)
        {
            float a = estBase[i] * (0.55f + 0.45f * Mathf.Sin(t * estVel[i] + estFase[i]));
            Color c = estrellas[i].color; c.a = a; estrellas[i].color = c;
        }

        // pétalos de cempasúchil
        for (int i = 0; i < petalos.Length; i++)
        {
            petPos[i].y -= petVel[i] * Time.unscaledDeltaTime;
            if (petPos[i].y < -1140f) { petPos[i].y = 60f; petPos[i].x = Random.value * 1920f; }
            float x = petPos[i].x + Mathf.Sin(t * 0.8f + petFase[i]) * petAmp[i];
            petalos[i].anchoredPosition = new Vector2(x, petPos[i].y);
            petalos[i].localRotation = Quaternion.Euler(0f, 0f, t * petGiro[i] + petFase[i] * 40f);
        }

        // papel picado meciéndose
        for (int i = 0; i < banderas.Length; i++)
            banderas[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.5f + banFase[i]) * 4.5f);

        // emblema
        if (florEmblema != null) florEmblema.localRotation = Quaternion.Euler(0f, 0f, t * 6f);

        // Esc cierra el modal
        if (Input.GetKeyDown(KeyCode.Escape) && modalActual != null) CerrarModal();

        // volumen de la música en vivo
        if (musica != null && !cargando) musica.volume = 0.5f * OpcionesJuego.Musica;
    }

    void IniciarMusica()
    {
        musica = gameObject.AddComponent<AudioSource>();
        musica.loop = true;
        musica.spatialBlend = 0f;
        musica.volume = 0.5f * OpcionesJuego.Musica;
        musica.clip = GenerarMusica();
        musica.Play();
    }

    /// <summary>Marimba pentatónica sobre un colchón grave: ambiente nocturno mexicano, sin audios externos.</summary>
    AudioClip GenerarMusica()
    {
        const int rate = 22050;
        const float beat = 0.46f;
        float[] melodia =
        {
            220f, 0f, 261.6f, 329.6f,  293.7f, 0f, 329.6f, 392f,
            440f, 0f, 392f, 329.6f,    293.7f, 261.6f, 329.6f, 0f,
        };
        int pasos = melodia.Length;
        int n = (int)(rate * beat * pasos);
        float[] d = new float[n];

        for (int p = 0; p < pasos; p++)
        {
            if (melodia[p] <= 0f) continue;
            int ini = (int)(rate * beat * p);
            float f = melodia[p];
            for (int i = 0; i < rate * 1.4f; i++)
            {
                float tt = i / (float)rate;
                float env = Mathf.Exp(-tt * 4.2f) * (1f - Mathf.Exp(-tt * 300f));
                float s = Mathf.Sin(2f * Mathf.PI * f * tt) + 0.3f * Mathf.Sin(2f * Mathf.PI * f * 4f * tt) * Mathf.Exp(-tt * 9f) + 0.12f * Mathf.Sin(2f * Mathf.PI * f * 2f * tt);
                d[(ini + i) % n] += s * env * 0.32f;
            }
        }
        // colchón: A2 + E3 con respiración lenta
        float total = n / (float)rate;
        for (int i = 0; i < n; i++)
        {
            float tt = i / (float)rate;
            float resp = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * (2f / total) * tt);
            d[i] += (Mathf.Sin(2f * Mathf.PI * 110f * tt) * 0.5f + Mathf.Sin(2f * Mathf.PI * 164.8f * tt) * 0.28f) * 0.16f * resp;
        }
        float max = 0.01f;
        for (int i = 0; i < n; i++) max = Mathf.Max(max, Mathf.Abs(d[i]));
        for (int i = 0; i < n; i++) d[i] = d[i] / max * 0.7f;

        AudioClip c = AudioClip.Create("MusicaMenu", n, 1, rate, false);
        c.SetData(d, 0);
        return c;
    }
}
