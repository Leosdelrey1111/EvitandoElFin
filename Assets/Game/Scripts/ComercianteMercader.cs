using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Puesto del Comerciante (Issue #15): Don Chuy, un mercader de ánimas, cambia ALMAS por mejoras.
///  - Acércate y pulsa E para abrir la tienda (el juego se pausa mientras compras; E o Esc la cierran).
///  - Mejoras: Atole (cura), Filo de Obsidiana (+daño), Rebozo Espiritual (+poder máximo), Copal del Camino (+velocidad).
///  - Se paga con almas (energía espiritual) y NO con tótems: así nunca se te acaban los 3 tótems que abren la Cueva Vieja.
/// El modelo, la tienda y los avisos se construyen por código.
/// </summary>
public class ComercianteMercader : MonoBehaviour
{
    public static bool TiendaAbierta { get; private set; }
    static int frameCierre = -10;
    /// <summary>La pausa del GameManager no debe abrirse mientras la tienda está abierta ni en el frame en que se cierra con Esc.</summary>
    public static bool BloqueaPausa { get { return TiendaAbierta || Time.frameCount - frameCierre <= 1; } }

    public KeyCode teclaInteractuar = KeyCode.E;
    public float radioInteraccion = 4f;

    // ------------------------------------------------------------------ mejoras
    class Mejora
    {
        public string id, nombre, descripcion;
        public int[] costos;
        public int nivel;
        public bool repetible;
        public UIMexicana.Icono icono;
        public Color color;
        public int Max { get { return costos.Length; } }
        public bool Agotada { get { return !repetible && nivel >= Max; } }
        public int CostoActual { get { return repetible ? costos[0] : costos[Mathf.Min(nivel, Max - 1)]; } }

        // UI
        public TextMeshProUGUI txtCosto, txtBoton, txtNivel;
        public Button boton;
        public Image fondoBoton;
        public Image[] pips;
    }
    readonly List<Mejora> mejoras = new List<Mejora>();

    // ------------------------------------------------------------------ referencias
    Transform jugador;
    PoderEspiritual vida;
    InventarioEspiritual inventario;
    SistemaCombateChofer combate;
    ChoferController chofer;

    // UI
    GameObject panelTienda;
    CanvasGroup grupoPrompt;
    TextMeshProUGUI txtAlmas, txtDialogo;
    float cursorGuardado;
    bool cerca;
    Transform cuerpo;

    // ================================================================== CICLO DE VIDA

    void Awake()
    {
        TiendaAbierta = false;
        DefinirMejoras();
        ConstruirModelo();
        ConstruirUI();
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) return;
        jugador = p.transform;
        vida = p.GetComponent<PoderEspiritual>();
        inventario = p.GetComponent<InventarioEspiritual>();
        combate = p.GetComponent<SistemaCombateChofer>();
        chofer = p.GetComponent<ChoferController>();
    }

    void OnDestroy()
    {
        if (TiendaAbierta) { Time.timeScale = 1f; }
        TiendaAbierta = false;
    }

    void Update()
    {
        if (jugador == null) return;
        if (CodiceHistoria.Abierto) return;   // con el libro abierto no se comercia

        if (TiendaAbierta)
        {
            if (Input.GetKeyDown(teclaInteractuar) || Input.GetKeyDown(KeyCode.Escape)) CerrarTienda();
            return;
        }

        Vector3 d = jugador.position - transform.position; d.y = 0f;
        cerca = d.magnitude <= radioInteraccion && EstadoNivel.EnJuego && (vida == null || !vida.EstaMuerto);

        // el mercader mira al Chofer cuando está cerca
        if (cerca && cuerpo != null && d.sqrMagnitude > 0.01f)
            cuerpo.rotation = Quaternion.Slerp(cuerpo.rotation, Quaternion.LookRotation(-d.normalized), 4f * Time.deltaTime);

        grupoPrompt.alpha = Mathf.MoveTowards(grupoPrompt.alpha, cerca ? 1f : 0f, 5f * Time.deltaTime);

        if (cerca && Input.GetKeyDown(teclaInteractuar)) AbrirTienda();
    }

    // ================================================================== TIENDA

    void AbrirTienda()
    {
        TiendaAbierta = true;
        panelTienda.SetActive(true);
        grupoPrompt.alpha = 0f;
        Hablar(HistoriaNivel1.ChuySaludo, "chuy_saludo");
        Refrescar();
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        UIMexicana.Sonar("click");
    }

    void CerrarTienda()
    {
        TiendaAbierta = false;
        frameCierre = Time.frameCount;
        panelTienda.SetActive(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        VozNivel.Detener(CanalVoz.Interfaz);
        UIMexicana.Sonar("atras");
    }

    /// <summary>Línea de diálogo del mercader: texto en la tienda + su voz.</summary>
    void Hablar(string texto, string claveVoz)
    {
        txtDialogo.text = texto;
        VozNivel.Reproducir(claveVoz, CanalVoz.Interfaz);
    }

    void Comprar(Mejora m)
    {
        if (m.Agotada) { Hablar(HistoriaNivel1.ChuyAgotado, "chuy_agotado"); return; }
        if (inventario == null || vida == null) return;

        int costo = m.CostoActual;
        if (inventario.energiaEspiritual < costo)
        {
            Hablar(HistoriaNivel1.ChuySinAlmas, "chuy_sinalmas");
            UIMexicana.Sonar("atras");
            return;
        }
        if (m.id == "atole" && vida.actualPoder >= vida.maxPoder - 0.5f)
        {
            Hablar(HistoriaNivel1.ChuyLleno, "chuy_lleno");
            return;
        }

        inventario.AgregarEnergia(-costo);
        Aplicar(m);
        if (!m.repetible) m.nivel++;
        int g = Random.Range(0, HistoriaNivel1.ChuyGracias.Length);
        Hablar(HistoriaNivel1.ChuyGracias[g], "chuy_gracias_" + g);
        UIMexicana.Sonar("click");
        Refrescar();
    }

    void Aplicar(Mejora m)
    {
        switch (m.id)
        {
            case "atole":
                vida.Curar(60f);
                AvisosJuego.Mostrar("Atole caliente: +60 de Poder Espiritual", 2.5f);
                break;
            case "obsidiana":
                if (combate != null)
                {
                    combate.danioAtaque1 *= 1.25f; combate.danioAtaque2 *= 1.25f; combate.danioAtaque3 *= 1.25f; combate.danioArea *= 1.25f;
                }
                AvisosJuego.Mostrar("Filo de Obsidiana: +25% de daño", 2.5f);
                break;
            case "rebozo":
                vida.maxPoder += 30f;
                vida.Curar(30f);
                AvisosJuego.Mostrar("Rebozo Espiritual: +30 de Poder Espiritual máximo", 2.5f);
                break;
            case "copal":
                if (chofer != null) { chofer.velocidadCaminar *= 1.1f; chofer.velocidadCorrer *= 1.1f; }
                AvisosJuego.Mostrar("Copal del Camino: +10% de velocidad", 2.5f);
                break;
        }
    }

    void DefinirMejoras()
    {
        mejoras.Clear();
        mejoras.Add(new Mejora
        {
            id = "atole", nombre = "Atole de Ánimas", descripcion = "Restaura 60 de Poder Espiritual.",
            costos = new[] { 15 }, repetible = true, icono = UIMexicana.Icono.Corazon, color = UIMexicana.Rosa
        });
        mejoras.Add(new Mejora
        {
            id = "obsidiana", nombre = "Filo de Obsidiana", descripcion = "+25% de daño en tus golpes y en la onda.",
            costos = new[] { 40, 70, 100 }, icono = UIMexicana.Icono.Golpe, color = UIMexicana.Cempasuchil
        });
        mejoras.Add(new Mejora
        {
            id = "rebozo", nombre = "Rebozo Espiritual", descripcion = "+30 de Poder Espiritual máximo (y te cura esa cantidad).",
            costos = new[] { 35, 60 }, icono = UIMexicana.Icono.Alma, color = UIMexicana.Turquesa
        });
        mejoras.Add(new Mejora
        {
            id = "copal", nombre = "Copal del Camino", descripcion = "+10% de velocidad al caminar y correr.",
            costos = new[] { 30, 55 }, icono = UIMexicana.Icono.Esquive, color = UIMexicana.Violeta
        });
    }

    // ================================================================== UI

    void ConstruirUI()
    {
        Canvas canvas = UIMexicana.CrearCanvas("Tienda_Comerciante", 70);
        canvas.transform.SetParent(transform, false);
        RectTransform raiz = (RectTransform)canvas.transform;

        // ---------- aviso "E · Comerciar"
        RectTransform prompt = UIMexicana.Nuevo("Prompt", raiz);
        UIMexicana.AnclarCentro(prompt, new Vector2(0.5f, 0f), new Vector2(0f, 330f), new Vector2(430f, 56f));
        grupoPrompt = prompt.gameObject.AddComponent<CanvasGroup>();
        grupoPrompt.alpha = 0f; grupoPrompt.blocksRaycasts = false;
        Image pf = UIMexicana.Imagen("Fondo", prompt, UIMexicana.Redondeado(28), new Color(0.05f, 0.07f, 0.18f, 0.85f), true);
        UIMexicana.Estirar(pf.rectTransform);
        Image pb = UIMexicana.Imagen("Borde", prompt, UIMexicana.RedondeadoBorde(28, 1.8f), UIMexicana.ConAlfa(UIMexicana.Dorado, 0.8f), true);
        UIMexicana.Estirar(pb.rectTransform);
        RectTransform tecla = UIMexicana.Tecla(prompt, "E", 38f, 22f);
        tecla.anchorMin = tecla.anchorMax = new Vector2(0f, 0.5f); tecla.pivot = new Vector2(0f, 0.5f); tecla.anchoredPosition = new Vector2(14f, 0f);
        TextMeshProUGUI pt = UIMexicana.Texto("Texto", prompt, "Comerciar con Don Chuy", 24, UIMexicana.Crema, TextAlignmentOptions.Left, FontStyles.Bold, 1f);
        UIMexicana.Estirar(pt.rectTransform);
        pt.rectTransform.offsetMin = new Vector2(70f, 0f); pt.rectTransform.offsetMax = new Vector2(-14f, 0f);

        // ---------- panel de la tienda
        RectTransform panel = UIMexicana.Nuevo("PanelTienda", raiz);
        UIMexicana.Estirar(panel);
        panelTienda = panel.gameObject;

        Image velo = UIMexicana.Imagen("Velo", panel, UIMexicana.Blanco(), new Color(0.02f, 0.03f, 0.09f, 0.72f), false, true);
        UIMexicana.Estirar(velo.rectTransform);

        RectTransform tarjeta = UIMexicana.Nuevo("Tarjeta", panel);
        UIMexicana.AnclarCentro(tarjeta, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, 760f));
        Image ts = UIMexicana.Imagen("Sombra", tarjeta, UIMexicana.Redondeado(34), new Color(0f, 0f, 0f, 0.5f), true);
        UIMexicana.Estirar(ts.rectTransform); ts.rectTransform.offsetMin = new Vector2(-4f, -10f); ts.rectTransform.offsetMax = new Vector2(4f, -2f);
        Image tf = UIMexicana.Imagen("Fondo", tarjeta, UIMexicana.Redondeado(34), new Color(0.07f, 0.09f, 0.22f, 0.97f), true);
        UIMexicana.Estirar(tf.rectTransform);
        Image tb = UIMexicana.Imagen("Borde", tarjeta, UIMexicana.RedondeadoBorde(34, 3f), UIMexicana.Dorado, true);
        UIMexicana.Estirar(tb.rectTransform);

        // papel picado decorativo arriba
        for (int i = 0; i < 9; i++)
        {
            Image b = UIMexicana.Imagen("Papel" + i, tarjeta, UIMexicana.BanderaPapelPicado(i % 4), UIMexicana.ColoresPapelPicado[i % UIMexicana.ColoresPapelPicado.Length]);
            UIMexicana.AnclarCentro(b.rectTransform, new Vector2(0.5f, 1f), new Vector2((i - 4) * 104f, -30f), new Vector2(92f, 62f));
        }

        TextMeshProUGUI titulo = UIMexicana.Texto("Titulo", tarjeta, "DON CHUY", 46, UIMexicana.Dorado, TextAlignmentOptions.Center, FontStyles.Bold, 8f);
        UIMexicana.AnclarCentro(titulo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(800f, 56f));
        TextMeshProUGUI sub = UIMexicana.Texto("Sub", tarjeta, "MERCADER DE ÁNIMAS", 20, UIMexicana.Cempasuchil, TextAlignmentOptions.Center, FontStyles.Bold, 6f);
        UIMexicana.AnclarCentro(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(800f, 28f));

        txtDialogo = UIMexicana.Texto("Dialogo", tarjeta, "", 22, UIMexicana.Crema, TextAlignmentOptions.Center, FontStyles.Italic, 0.5f);
        UIMexicana.AnclarCentro(txtDialogo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -184f), new Vector2(860f, 34f));

        // almas disponibles
        RectTransform chip = UIMexicana.Nuevo("ChipAlmas", tarjeta);
        UIMexicana.AnclarCentro(chip, new Vector2(1f, 1f), new Vector2(-120f, -100f), new Vector2(190f, 48f));
        Image cf = UIMexicana.Imagen("Fondo", chip, UIMexicana.Redondeado(24), new Color(0.04f, 0.06f, 0.16f, 0.9f), true);
        UIMexicana.Estirar(cf.rectTransform);
        Image cb = UIMexicana.Imagen("Borde", chip, UIMexicana.RedondeadoBorde(24, 1.6f), UIMexicana.ConAlfa(UIMexicana.H("9FE7FF"), 0.7f), true);
        UIMexicana.Estirar(cb.rectTransform);
        Image ci = UIMexicana.Imagen("Icono", chip, UIMexicana.IconoSprite(UIMexicana.Icono.Alma), UIMexicana.H("9FE7FF"));
        UIMexicana.AnclarCentro(ci.rectTransform, new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(30f, 30f));
        txtAlmas = UIMexicana.Texto("Valor", chip, "0", 28, UIMexicana.Crema, TextAlignmentOptions.Left, FontStyles.Bold, 1f);
        UIMexicana.Estirar(txtAlmas.rectTransform);
        txtAlmas.rectTransform.offsetMin = new Vector2(56f, 0f);

        // filas de mejoras
        for (int i = 0; i < mejoras.Count; i++) CrearFila(tarjeta, mejoras[i], -250f - i * 120f);

        // pie
        RectTransform tecla2 = UIMexicana.Tecla(tarjeta, "E / ESC", 34f, 16f);
        tecla2.anchorMin = tecla2.anchorMax = new Vector2(0.5f, 0f); tecla2.pivot = new Vector2(1f, 0f); tecla2.anchoredPosition = new Vector2(-10f, 26f);
        TextMeshProUGUI cerrar = UIMexicana.Texto("Cerrar", tarjeta, "cerrar tienda", 20, UIMexicana.ConAlfa(UIMexicana.Crema, 0.75f), TextAlignmentOptions.Left, FontStyles.Bold, 2f);
        UIMexicana.AnclarCentro(cerrar.rectTransform, new Vector2(0.5f, 0f), new Vector2(110f, 42f), new Vector2(240f, 30f));

        panelTienda.SetActive(false);
    }

    void CrearFila(RectTransform tarjeta, Mejora m, float y)
    {
        RectTransform fila = UIMexicana.Nuevo("Fila_" + m.id, tarjeta);
        UIMexicana.AnclarCentro(fila, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(900f, 104f));
        Image ff = UIMexicana.Imagen("Fondo", fila, UIMexicana.Redondeado(22), new Color(0.11f, 0.14f, 0.32f, 0.9f), true);
        UIMexicana.Estirar(ff.rectTransform);
        Image fb = UIMexicana.Imagen("Borde", fila, UIMexicana.RedondeadoBorde(22, 1.6f), UIMexicana.ConAlfa(m.color, 0.75f), true);
        UIMexicana.Estirar(fb.rectTransform);

        // ícono
        Image disco = UIMexicana.Imagen("Disco", fila, UIMexicana.Circulo(), UIMexicana.ConAlfa(m.color, 0.22f));
        UIMexicana.AnclarCentro(disco.rectTransform, new Vector2(0f, 0.5f), new Vector2(64f, 0f), new Vector2(76f, 76f));
        Image ic = UIMexicana.Imagen("Icono", fila, UIMexicana.IconoSprite(m.icono), m.color);
        UIMexicana.AnclarCentro(ic.rectTransform, new Vector2(0f, 0.5f), new Vector2(64f, 0f), new Vector2(48f, 48f));

        TextMeshProUGUI nom = UIMexicana.Texto("Nombre", fila, m.nombre.ToUpper(), 24, UIMexicana.Crema, TextAlignmentOptions.Left, FontStyles.Bold, 2f);
        UIMexicana.Anclar(nom.rectTransform, new Vector2(0f, 1f), new Vector2(120f, -12f), new Vector2(430f, 32f));
        TextMeshProUGUI des = UIMexicana.Texto("Desc", fila, m.descripcion, 18, UIMexicana.ConAlfa(UIMexicana.Crema, 0.8f), TextAlignmentOptions.TopLeft, FontStyles.Normal, 0.3f);
        UIMexicana.Anclar(des.rectTransform, new Vector2(0f, 1f), new Vector2(120f, -46f), new Vector2(440f, 52f));

        // niveles (puntos)
        if (!m.repetible)
        {
            m.pips = new Image[m.Max];
            for (int i = 0; i < m.Max; i++)
            {
                Image p = UIMexicana.Imagen("Pip" + i, fila, UIMexicana.Circulo(), new Color(1f, 1f, 1f, 0.2f));
                UIMexicana.AnclarCentro(p.rectTransform, new Vector2(1f, 0.5f), new Vector2(-290f + i * 22f, 24f), new Vector2(14f, 14f));
                m.pips[i] = p;
            }
        }
        m.txtCosto = UIMexicana.Texto("Costo", fila, "", 24, UIMexicana.H("9FE7FF"), TextAlignmentOptions.Right, FontStyles.Bold, 1f);
        UIMexicana.AnclarCentro(m.txtCosto.rectTransform, new Vector2(1f, 0.5f), new Vector2(-300f, -14f), new Vector2(110f, 30f));
        m.txtCosto.rectTransform.pivot = new Vector2(1f, 0.5f);

        // botón comprar
        RectTransform rb = UIMexicana.Nuevo("BtnComprar", fila);
        UIMexicana.AnclarCentro(rb, new Vector2(1f, 0.5f), new Vector2(-110f, 0f), new Vector2(190f, 56f));
        m.fondoBoton = UIMexicana.Imagen("Fondo", rb, UIMexicana.Redondeado(26), UIMexicana.ConAlfa(m.color, 0.9f), true, true);
        UIMexicana.Estirar(m.fondoBoton.rectTransform);
        m.boton = rb.gameObject.AddComponent<Button>();
        m.boton.targetGraphic = m.fondoBoton;
        ColorBlock cb = m.boton.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        cb.disabledColor = new Color(0.45f, 0.45f, 0.5f, 0.8f);
        m.boton.colors = cb;
        Mejora capturada = m;
        m.boton.onClick.AddListener(() => Comprar(capturada));
        m.txtBoton = UIMexicana.Texto("Texto", rb, "COMPRAR", 22, Color.white, TextAlignmentOptions.Center, FontStyles.Bold, 2f);
        UIMexicana.Estirar(m.txtBoton.rectTransform);
    }

    void Refrescar()
    {
        float almas = inventario != null ? inventario.energiaEspiritual : 0f;
        txtAlmas.text = Mathf.RoundToInt(almas).ToString();

        foreach (Mejora m in mejoras)
        {
            if (m.pips != null)
                for (int i = 0; i < m.pips.Length; i++)
                    m.pips[i].color = i < m.nivel ? UIMexicana.Dorado : new Color(1f, 1f, 1f, 0.2f);

            if (m.Agotada)
            {
                m.txtCosto.text = "";
                m.txtBoton.text = "AL MÁXIMO";
                m.boton.interactable = false;
            }
            else
            {
                int costo = m.CostoActual;
                m.txtCosto.text = costo + " almas";
                bool alcanza = almas >= costo;
                m.txtBoton.text = alcanza ? "COMPRAR" : "FALTAN ALMAS";
                m.txtBoton.fontSize = alcanza ? 22 : 16;
                m.boton.interactable = alcanza;
            }
        }
    }

    // ================================================================== MODELO

    static Material Mat(Color c, float suavidad = 0.2f)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        Material m = new Material(sh);
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", suavidad);
        return m;
    }

    static Material MatEmisivo(Color c, Color e)
    {
        Material m = Mat(c, 0.5f);
        m.EnableKeyword("_EMISSION");
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", e);
        return m;
    }

    static GameObject Parte(PrimitiveType t, string nombre, Transform padre, Vector3 pos, Vector3 esc, Material mat, bool collider = false)
    {
        GameObject g = GameObject.CreatePrimitive(t);
        g.name = nombre;
        g.transform.SetParent(padre, false);
        g.transform.localPosition = pos;
        g.transform.localScale = esc;
        if (!collider) { Collider c = g.GetComponent<Collider>(); if (c != null) Destroy(c); }
        g.GetComponent<Renderer>().sharedMaterial = mat;
        return g;
    }

    /// <summary>Anciano con sombrero y sarape, linterna de ánimas y un puesto con mercancía.</summary>
    void ConstruirModelo()
    {
        Material tunica = Mat(new Color(0.30f, 0.14f, 0.12f));
        Material sarape = Mat(new Color(0.85f, 0.25f, 0.30f));
        Material franja1 = Mat(new Color(0.96f, 0.78f, 0.25f));
        Material franja2 = Mat(new Color(0.18f, 0.72f, 0.66f));
        Material piel = Mat(new Color(0.78f, 0.58f, 0.44f));
        Material sombrero = Mat(new Color(0.72f, 0.60f, 0.38f));
        Material bigote = Mat(new Color(0.9f, 0.9f, 0.88f));
        Material madera = Mat(new Color(0.34f, 0.22f, 0.12f));
        Material luzMat = MatEmisivo(new Color(1f, 0.8f, 0.4f), new Color(3f, 1.8f, 0.6f));
        Material cian = MatEmisivo(new Color(0.4f, 1f, 0.95f), new Color(0.6f, 2.6f, 2.4f));

        GameObject cuerpoGO = new GameObject("Cuerpo");
        cuerpoGO.transform.SetParent(transform, false);
        cuerpo = cuerpoGO.transform;

        Parte(PrimitiveType.Cylinder, "Tunica", cuerpo, new Vector3(0f, 0.8f, 0f), new Vector3(0.9f, 0.8f, 0.9f), tunica);
        Parte(PrimitiveType.Sphere, "Torso", cuerpo, new Vector3(0f, 1.45f, 0f), new Vector3(0.85f, 0.8f, 0.6f), tunica);
        Parte(PrimitiveType.Cube, "Sarape", cuerpo, new Vector3(0f, 1.42f, 0.02f), new Vector3(1.05f, 0.55f, 0.72f), sarape);
        Parte(PrimitiveType.Cube, "Franja1", cuerpo, new Vector3(0f, 1.42f, 0.02f), new Vector3(1.07f, 0.09f, 0.74f), franja1);
        Parte(PrimitiveType.Cube, "Franja2", cuerpo, new Vector3(0f, 1.25f, 0.02f), new Vector3(1.07f, 0.06f, 0.74f), franja2);
        GameObject cabeza = Parte(PrimitiveType.Sphere, "Cabeza", cuerpo, new Vector3(0f, 1.95f, 0.02f), Vector3.one * 0.42f, piel);
        Parte(PrimitiveType.Sphere, "Nariz", cabeza.transform, new Vector3(0f, -0.05f, 0.5f), new Vector3(0.2f, 0.25f, 0.25f), piel);
        Parte(PrimitiveType.Sphere, "BigoteI", cabeza.transform, new Vector3(-0.2f, -0.28f, 0.45f), new Vector3(0.36f, 0.14f, 0.2f), bigote);
        Parte(PrimitiveType.Sphere, "BigoteD", cabeza.transform, new Vector3(0.2f, -0.28f, 0.45f), new Vector3(0.36f, 0.14f, 0.2f), bigote);
        Parte(PrimitiveType.Sphere, "OjoI", cabeza.transform, new Vector3(-0.24f, 0.12f, 0.42f), new Vector3(0.12f, 0.1f, 0.1f), cian);
        Parte(PrimitiveType.Sphere, "OjoD", cabeza.transform, new Vector3(0.24f, 0.12f, 0.42f), new Vector3(0.12f, 0.1f, 0.1f), cian);
        Parte(PrimitiveType.Cylinder, "AlaSombrero", cuerpo, new Vector3(0f, 2.15f, 0f), new Vector3(1.0f, 0.03f, 1.0f), sombrero);
        Parte(PrimitiveType.Cylinder, "CopaSombrero", cuerpo, new Vector3(0f, 2.3f, 0f), new Vector3(0.5f, 0.15f, 0.5f), sombrero);
        // bastón con linterna de ánimas
        Parte(PrimitiveType.Cylinder, "Baston", cuerpo, new Vector3(0.65f, 1.1f, 0.2f), new Vector3(0.07f, 1.1f, 0.07f), madera);
        GameObject linterna = Parte(PrimitiveType.Sphere, "Linterna", cuerpo, new Vector3(0.65f, 2.3f, 0.2f), Vector3.one * 0.32f, luzMat);
        GameObject lg = new GameObject("LuzLinterna");
        lg.transform.SetParent(linterna.transform, false);
        Light l = lg.AddComponent<Light>();
        l.type = LightType.Point; l.color = new Color(1f, 0.75f, 0.4f); l.range = 12f; l.intensity = 3.2f; l.shadows = LightShadows.None;

        // puesto: mesa con tela y mercancía (se queda fijo, no gira con el mercader)
        GameObject puesto = new GameObject("Puesto");
        puesto.transform.SetParent(transform, false);
        puesto.transform.localPosition = new Vector3(-1.6f, 0f, 0.3f);
        Parte(PrimitiveType.Cube, "Mesa", puesto.transform, new Vector3(0f, 0.55f, 0f), new Vector3(1.8f, 0.1f, 0.9f), madera, true);
        Parte(PrimitiveType.Cube, "PataA", puesto.transform, new Vector3(-0.8f, 0.27f, -0.35f), new Vector3(0.1f, 0.54f, 0.1f), madera);
        Parte(PrimitiveType.Cube, "PataB", puesto.transform, new Vector3(0.8f, 0.27f, -0.35f), new Vector3(0.1f, 0.54f, 0.1f), madera);
        Parte(PrimitiveType.Cube, "PataC", puesto.transform, new Vector3(-0.8f, 0.27f, 0.35f), new Vector3(0.1f, 0.54f, 0.1f), madera);
        Parte(PrimitiveType.Cube, "PataD", puesto.transform, new Vector3(0.8f, 0.27f, 0.35f), new Vector3(0.1f, 0.54f, 0.1f), madera);
        Parte(PrimitiveType.Cube, "Mantel", puesto.transform, new Vector3(0f, 0.61f, 0f), new Vector3(1.9f, 0.02f, 1.0f), sarape);
        Parte(PrimitiveType.Sphere, "Vela1", puesto.transform, new Vector3(-0.5f, 0.78f, 0f), Vector3.one * 0.18f, luzMat);
        Parte(PrimitiveType.Sphere, "Cristal", puesto.transform, new Vector3(0.1f, 0.78f, 0.1f), new Vector3(0.22f, 0.3f, 0.22f), cian);
        Parte(PrimitiveType.Cylinder, "Jarra", puesto.transform, new Vector3(0.55f, 0.78f, -0.1f), new Vector3(0.22f, 0.18f, 0.22f), Mat(new Color(0.55f, 0.3f, 0.18f)));
        // papel picado colgando del puesto
        for (int i = 0; i < 5; i++)
        {
            Material pm = Mat(UIMexicana.ColoresPapelPicado[i % UIMexicana.ColoresPapelPicado.Length]);
            Parte(PrimitiveType.Cube, "Papel" + i, puesto.transform, new Vector3(-0.8f + i * 0.4f, 1.9f - Mathf.Abs(i - 2) * 0.06f, 0f), new Vector3(0.3f, 0.34f, 0.01f), pm);
        }
        Parte(PrimitiveType.Cylinder, "Hilo", puesto.transform, new Vector3(0f, 2.1f, 0f), new Vector3(0.015f, 0.9f, 0.015f), madera).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        Parte(PrimitiveType.Cylinder, "PostePuestoI", puesto.transform, new Vector3(-0.92f, 1.1f, 0f), new Vector3(0.06f, 1.1f, 0.06f), madera);
        Parte(PrimitiveType.Cylinder, "PostePuestoD", puesto.transform, new Vector3(0.92f, 1.1f, 0f), new Vector3(0.06f, 1.1f, 0.06f), madera);
        Parte(PrimitiveType.Cylinder, "HiloPostes", puesto.transform, new Vector3(0f, 2.2f, 0f), new Vector3(0.02f, 0.92f, 0.02f), madera).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

        // aura tenue para que se distinga de lejos
        GameObject aura = new GameObject("AuraMercader");
        aura.transform.SetParent(transform, false);
        aura.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        Light la = aura.AddComponent<Light>();
        la.type = LightType.Point; la.color = new Color(0.4f, 1f, 0.9f); la.range = 7f; la.intensity = 1.2f; la.shadows = LightShadows.None;

        // colisión para que no se atraviese al mercader
        CapsuleCollider cc = gameObject.AddComponent<CapsuleCollider>();
        cc.center = new Vector3(0f, 1.1f, 0f); cc.radius = 0.5f; cc.height = 2.2f;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 1f, 0.9f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, radioInteraccion);
    }
}
