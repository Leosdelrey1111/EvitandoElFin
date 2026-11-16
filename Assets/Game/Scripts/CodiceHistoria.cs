using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// CÓDICE DE LA BUFA: el libro de la historia (Issue #24). Se abre con TAB.
///  - HISTORIA: prólogo con la leyenda y un capítulo por cada cosa que Tonalli ha contado (con sus diálogos, en orden, agrupados por acto).
///    Cada capítulo se puede volver a escuchar con la voz de quien lo cuenta.
///  - BESTIARIO: cada enemigo es una carta de LOTERÍA ("El Hombre de Piedra") con su número, su emblema, su versito, su historia y un consejo.
///  - RECUERDOS: los fragmentos de memoria del Chofer que se recuperan al vencer a cada jefe.
/// Estética: tapa índigo con greca de talavera, papel picado colgando, veladoras y cempasúchil a los lados, páginas de papel amate.
/// Las páginas descubiertas se guardan (PlayerPrefs). Se construye por código; GuiaAlebrije lo crea solo si no existe.
/// </summary>
[DefaultExecutionOrder(-30)]
public class CodiceHistoria : MonoBehaviour
{
    public static CodiceHistoria Instancia { get; private set; }
    public static bool Abierto { get; private set; }
    static int frameCierre = -10;
    /// <summary>La pausa del GameManager no debe abrirse con el libro abierto ni en el frame en que se cierra con Esc.</summary>
    public static bool BloqueaPausa { get { return Abierto || Time.frameCount - frameCierre <= 1; } }

    public KeyCode tecla = KeyCode.Tab;

    // ------------------------------------------------------------------ persistencia
    const string ClavePrefs = "Codice_v1";
    static HashSet<string> descubiertas;

    static void Cargar()
    {
        if (descubiertas != null) return;
        descubiertas = new HashSet<string>();
        foreach (string p in PlayerPrefs.GetString(ClavePrefs, "").Split(','))
            if (p.Length > 0) descubiertas.Add(p);
    }

    public static bool Tiene(string id) { Cargar(); return descubiertas.Contains(id); }

    /// <summary>Desbloquea una página: id de capítulo ("intro"), "bestia_serpiente"... o "recuerdo1".</summary>
    public static void Registrar(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        Cargar();
        if (!descubiertas.Add(id)) return;
        PlayerPrefs.SetString(ClavePrefs, string.Join(",", new List<string>(descubiertas).ToArray()));
        AvisosJuego.Mostrar("Nueva página en el Códice  ·  TAB", 3.5f);
    }

    // ------------------------------------------------------------------ datos de las entradas
    class Entrada
    {
        public string id, etiqueta, titulo, subtitulo, cuerpo, verso;
        public bool desbloqueada;
        public string[] clavesVoz;
        // carta de lotería
        public bool carta; public int numero; public UIMexicana.Icono icono; public Color colorCarta;
    }

    readonly string[] nombresTab = { "HISTORIA", "BESTIARIO", "RECUERDOS" };
    Color[] coloresTab;
    int tab, pagina, seleccion;
    List<Entrada> entradas = new List<Entrada>();

    // ------------------------------------------------------------------ UI
    GameObject panel;
    const int PorPagina = 10;
    class FilaUI { public Button boton; public Image fondo; public TextMeshProUGUI numero, titulo; }
    readonly List<FilaUI> filas = new List<FilaUI>();
    readonly List<Image> fondosTab = new List<Image>();
    readonly List<Image> rayasTab = new List<Image>();
    readonly List<TextMeshProUGUI> textosTab = new List<TextMeshProUGUI>();
    TextMeshProUGUI txtEtiqueta, txtTitulo, txtSub, txtCuerpo, txtPaginador, txtContador;
    Button btnEscuchar, btnAnterior, btnSiguiente;
    Coroutine rutinaEscucha;

    // carta de lotería
    RectTransform cartaRaiz;
    Image cartaCampo, cartaEmblema;
    TextMeshProUGUI cartaNumero, cartaNombre, txtVerso, txtCuerpoCarta;

    // paleta
    static readonly Color Papel = UIMexicana.H("EFE0BA");
    static readonly Color Papel2 = UIMexicana.H("E2CE9F");
    static readonly Color Tinta = UIMexicana.H("3B2616");
    static readonly Color Carmesi = UIMexicana.H("A3123F");
    static readonly Color Cobalto = UIMexicana.H("1F4E9E");
    static readonly Color Indigo = UIMexicana.H("171C45");
    static readonly Color Naranja = UIMexicana.H("C2570C");

    // ================================================================== CICLO DE VIDA

    void Awake()
    {
        Instancia = this;
        Abierto = false;
        Cargar();
        coloresTab = new[] { UIMexicana.Rosa, UIMexicana.Cempasuchil, UIMexicana.Turquesa };
        ConstruirUI();
        EsbirroBaseIA.OnEsbirroDerrotado += AlDerrotarEsbirro;
    }

    void OnDestroy()
    {
        EsbirroBaseIA.OnEsbirroDerrotado -= AlDerrotarEsbirro;
        if (Abierto) Time.timeScale = 1f;
        Abierto = false;
        if (Instancia == this) Instancia = null;
    }

    void AlDerrotarEsbirro(EsbirroBaseIA e)
    {
        switch (e.tipo)
        {
            case EsbirroBaseIA.TipoEsbirro.Serpiente: Registrar("bestia_serpiente"); break;
            case EsbirroBaseIA.TipoEsbirro.HombrePiedra: Registrar("bestia_hombre_piedra"); break;
            case EsbirroBaseIA.TipoEsbirro.Comadre: Registrar("bestia_comadre"); break;
        }
    }

    void Update()
    {
        if (Abierto)
        {
            if (Input.GetKeyDown(tecla) || Input.GetKeyDown(KeyCode.Escape)) Cerrar();
            return;
        }

        if (Input.GetKeyDown(tecla) && PuedeAbrir()) Abrir();
    }

    bool PuedeAbrir()
    {
        if (!EstadoNivel.EnJuego || Time.timeScale <= 0.01f) return false;
        if (ComercianteMercader.TiendaAbierta) return false;
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        PoderEspiritual v = p != null ? p.GetComponent<PoderEspiritual>() : null;
        return v == null || !v.EstaMuerto;
    }

    void Abrir()
    {
        Abierto = true;
        panel.SetActive(true);
        tab = 0;
        ReconstruirEntradas();
        SeleccionarUltimaDesbloqueada();
        Refrescar();
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        UIMexicana.Sonar("click");
    }

    void Cerrar()
    {
        Abierto = false;
        frameCierre = Time.frameCount;
        DetenerEscucha();
        panel.SetActive(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        UIMexicana.Sonar("atras");
    }

    void SeleccionarUltimaDesbloqueada()
    {
        seleccion = 0;
        for (int i = entradas.Count - 1; i >= 0; i--) if (entradas[i].desbloqueada) { seleccion = i; break; }
        pagina = seleccion / PorPagina;
    }

    // ================================================================== CONTENIDO

    void ReconstruirEntradas()
    {
        entradas = new List<Entrada>();

        if (tab == 0)
        {
            entradas.Add(new Entrada
            {
                id = "prologo", etiqueta = "PRÓLOGO", titulo = "La leyenda de La Bufa", subtitulo = "Lo que cuentan en la sierra",
                cuerpo = ConLetraCapital(HistoriaNivel1.TextoPrologo, false), desbloqueada = true
            });
            for (int i = 0; i < HistoriaNivel1.Capitulos.GetLength(0); i++)
            {
                string id = HistoriaNivel1.Capitulos[i, 0];
                string[] lineas = HistoriaNivel1.Lineas(id);
                bool ok = Tiene(id);
                var claves = new string[lineas != null ? lineas.Length : 0];
                for (int k = 0; k < claves.Length; k++) claves[k] = HistoriaNivel1.ClaveLinea(id, k);

                string cuerpo = "";
                if (lineas != null)
                    for (int k = 0; k < lineas.Length; k++) cuerpo += (k > 0 ? "\n\n" : "") + "— " + lineas[k];

                int acto = HistoriaNivel1.ActoDe(id);
                entradas.Add(new Entrada
                {
                    id = id,
                    etiqueta = "CAPÍTULO " + Romano(i + 1),
                    titulo = HistoriaNivel1.Capitulos[i, 1],
                    subtitulo = (acto > 0 ? "Acto " + Romano(acto) + "  ·  " + HistoriaNivel1.TituloActo(acto) : "") ,
                    cuerpo = ConLetraCapital(cuerpo, true), desbloqueada = ok, clavesVoz = claves
                });
            }
        }
        else if (tab == 1)
        {
            Color[] colores = { UIMexicana.H("2A9D6F"), UIMexicana.H("5B7DB1"), UIMexicana.H("C2399A"), UIMexicana.H("E8892B") };
            UIMexicana.Icono[] iconos = { UIMexicana.Icono.Serpiente, UIMexicana.Icono.Totem, UIMexicana.Icono.Onda, UIMexicana.Icono.Calavera };
            for (int i = 0; i < HistoriaNivel1.Bestiario.Length; i++)
            {
                HistoriaNivel1.Bestia b = HistoriaNivel1.Bestiario[i];
                entradas.Add(new Entrada
                {
                    id = "bestia_" + b.id, etiqueta = "CARTA No. " + b.numero, titulo = b.nombre,
                    subtitulo = b.id == "bufa" ? "Jefe del Nivel 1" : "Enemigo del Nivel 1", verso = b.verso,
                    cuerpo = b.historia + "\n\n<b><color=#A3123F>Consejo:</color></b> " + b.consejo,
                    desbloqueada = Tiene("bestia_" + b.id),
                    carta = true, numero = b.numero, icono = iconos[i % iconos.Length], colorCarta = colores[i % colores.Length]
                });
            }
        }
        else
        {
            entradas.Add(new Entrada
            {
                id = "recuerdo1", etiqueta = "RECUERDO 1", titulo = "La promesa", subtitulo = "Recuperado al vencer a La Bufa",
                cuerpo = ConLetraCapital(HistoriaNivel1.TextoRecuerdo, false), desbloqueada = Tiene("recuerdo1")
            });
            string[] jefes = { "el General Momia", "el Clérigo Inquisidor", "Marcela la Bruja" };
            for (int i = 0; i < jefes.Length; i++)
                entradas.Add(new Entrada { id = "recuerdo" + (i + 2), etiqueta = "RECUERDO " + (i + 2), titulo = "Recuerdo " + (i + 2), subtitulo = "Se recupera al vencer a " + jefes[i], cuerpo = "", desbloqueada = false });
        }
    }

    /// <summary>Pone la primera letra del texto grande y en carmesí (letra capital de libro antiguo).</summary>
    static string ConLetraCapital(string texto, bool conGuion)
    {
        if (string.IsNullOrEmpty(texto)) return texto;
        for (int i = 0; i < texto.Length; i++)
        {
            if (!char.IsLetter(texto[i])) continue;
            return texto.Substring(0, i) + "<size=190%><color=#A3123F><b>" + texto[i] + "</b></color></size>" + texto.Substring(i + 1);
        }
        return texto;
    }

    static string Romano(int n)
    {
        string[] r = { "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X", "XI", "XII", "XIII", "XIV", "XV", "XVI", "XVII", "XVIII", "XIX", "XX" };
        return n >= 0 && n < r.Length ? r[n] : n.ToString();
    }

    // ================================================================== INTERFAZ (lógica)

    void CambiarTab(int nuevo)
    {
        if (tab == nuevo) return;
        DetenerEscucha();
        tab = nuevo;
        ReconstruirEntradas();
        SeleccionarUltimaDesbloqueada();
        UIMexicana.Sonar("tick");
        Refrescar();
    }

    void Seleccionar(int indiceGlobal)
    {
        if (indiceGlobal < 0 || indiceGlobal >= entradas.Count) return;
        DetenerEscucha();
        seleccion = indiceGlobal;
        UIMexicana.Sonar("tick");
        Refrescar();
    }

    void CambiarPagina(int delta)
    {
        int total = Mathf.Max(1, Mathf.CeilToInt(entradas.Count / (float)PorPagina));
        pagina = Mathf.Clamp(pagina + delta, 0, total - 1);
        UIMexicana.Sonar("tick");
        Refrescar();
    }

    void Refrescar()
    {
        // pestañas
        for (int i = 0; i < fondosTab.Count; i++)
        {
            bool activa = i == tab;
            fondosTab[i].color = activa ? Papel : UIMexicana.ConAlfa(Indigo, 0.95f);
            rayasTab[i].color = coloresTab[i];
            rayasTab[i].rectTransform.sizeDelta = new Vector2(rayasTab[i].rectTransform.sizeDelta.x, activa ? 8f : 4f);
            textosTab[i].color = activa ? Carmesi : UIMexicana.ConAlfa(UIMexicana.Crema, 0.85f);
        }

        // índice
        int total = Mathf.Max(1, Mathf.CeilToInt(entradas.Count / (float)PorPagina));
        pagina = Mathf.Clamp(pagina, 0, total - 1);
        for (int i = 0; i < filas.Count; i++)
        {
            int idx = pagina * PorPagina + i;
            FilaUI f = filas[i];
            if (idx >= entradas.Count) { f.boton.gameObject.SetActive(false); continue; }
            f.boton.gameObject.SetActive(true);
            Entrada e = entradas[idx];
            bool sel = idx == seleccion;
            f.fondo.color = sel ? UIMexicana.ConAlfa(Carmesi, 0.24f) : (e.desbloqueada ? Papel2 : UIMexicana.ConAlfa(Papel2, 0.45f));
            f.numero.text = (idx + 1).ToString();
            f.titulo.text = e.desbloqueada ? e.titulo : "· · ·   página por descubrir";
            f.titulo.color = e.desbloqueada ? Tinta : UIMexicana.ConAlfa(Tinta, 0.4f);
            f.titulo.fontStyle = sel ? FontStyles.Bold : FontStyles.Normal;
        }
        txtPaginador.text = (pagina + 1) + " / " + total;
        btnAnterior.interactable = pagina > 0;
        btnSiguiente.interactable = pagina < total - 1;

        // página derecha
        Entrada actual = seleccion >= 0 && seleccion < entradas.Count ? entradas[seleccion] : null;
        bool tieneVoz = false;
        bool mostrarCarta = actual != null && actual.carta;

        cartaRaiz.gameObject.SetActive(mostrarCarta);
        txtVerso.gameObject.SetActive(mostrarCarta);
        txtCuerpoCarta.gameObject.SetActive(mostrarCarta);
        txtCuerpo.gameObject.SetActive(!mostrarCarta);

        if (actual == null)
        {
            txtEtiqueta.text = ""; txtTitulo.text = ""; txtSub.text = ""; txtCuerpo.text = "";
        }
        else if (mostrarCarta)
        {
            txtEtiqueta.text = actual.etiqueta;
            txtTitulo.text = actual.desbloqueada ? actual.titulo : "Carta por descubrir";
            txtSub.text = actual.desbloqueada ? actual.subtitulo : "";

            cartaNumero.text = actual.numero.ToString();
            cartaCampo.color = actual.desbloqueada ? actual.colorCarta : UIMexicana.H("5A5560");
            cartaEmblema.sprite = UIMexicana.IconoSprite(actual.desbloqueada ? actual.icono : UIMexicana.Icono.Calavera);
            cartaEmblema.color = actual.desbloqueada ? UIMexicana.Crema : UIMexicana.ConAlfa(Color.black, 0.45f);
            cartaNombre.text = actual.desbloqueada ? actual.titulo.ToUpper() : "¿?";

            txtVerso.text = actual.desbloqueada ? actual.verso : "";
            txtCuerpoCarta.text = actual.desbloqueada ? actual.cuerpo : "Derrota a este enemigo para descubrir su carta.";
            txtCuerpoCarta.color = actual.desbloqueada ? Tinta : UIMexicana.ConAlfa(Tinta, 0.55f);
        }
        else if (!actual.desbloqueada)
        {
            txtEtiqueta.text = actual.etiqueta;
            txtTitulo.text = tab == 2 ? actual.titulo : "Página por descubrir";
            txtSub.text = tab == 2 ? actual.subtitulo : "";
            txtCuerpo.text = tab == 2 ? "Aún no has recuperado este recuerdo."
                           : "Sigue avanzando por la sierra: Tonalli irá escribiendo esta página.";
            txtCuerpo.color = UIMexicana.ConAlfa(Tinta, 0.55f);
        }
        else
        {
            txtEtiqueta.text = actual.etiqueta;
            txtTitulo.text = actual.titulo;
            txtSub.text = actual.subtitulo;
            txtCuerpo.text = actual.cuerpo;
            txtCuerpo.color = Tinta;
            if (actual.clavesVoz != null)
                foreach (string c in actual.clavesVoz) if (VozNivel.Obtener(c) != null) { tieneVoz = true; break; }
        }
        btnEscuchar.gameObject.SetActive(tieneVoz);

        int ok = 0, tot = 0;
        foreach (Entrada e in entradas) { tot++; if (e.desbloqueada) ok++; }
        txtContador.text = nombresTab[tab] + ":  " + ok + " / " + tot + " páginas";
    }

    void Escuchar()
    {
        if (seleccion < 0 || seleccion >= entradas.Count) return;
        Entrada e = entradas[seleccion];
        if (e.clavesVoz == null || !e.desbloqueada) return;
        DetenerEscucha();
        rutinaEscucha = StartCoroutine(RutinaEscucha(e.clavesVoz));
    }

    IEnumerator RutinaEscucha(string[] claves)
    {
        foreach (string c in claves)
        {
            AudioClip clip = VozNivel.Reproducir(c, CanalVoz.Interfaz);
            if (clip == null) continue;
            yield return new WaitForSecondsRealtime(clip.length + 0.3f);
        }
        rutinaEscucha = null;
    }

    void DetenerEscucha()
    {
        if (rutinaEscucha != null) StopCoroutine(rutinaEscucha);
        rutinaEscucha = null;
        VozNivel.Detener(CanalVoz.Interfaz);
    }

    // ================================================================== GENERACIÓN DE TEXTURAS DECORATIVAS

    static Sprite spriteTalavera, spriteAmate;

    /// <summary>Mosaico de talavera poblana: flor de cuatro pétalos cobalto sobre fondo crema, con esquinas y marco.</summary>
    static Sprite Talavera()
    {
        if (spriteTalavera != null) return spriteTalavera;
        const int n = 72;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;
        Color crema = UIMexicana.H("F4EBD3"), azul = UIMexicana.H("1F4E9E"), azulClaro = UIMexicana.H("6F9BDB");
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f;
                float r = Mathf.Sqrt(u * u + v * v);
                float a = Mathf.Atan2(v, u);
                Color c = crema;
                float petalo = 0.66f * (0.5f + 0.5f * Mathf.Abs(Mathf.Cos(2f * a)));
                if (r < petalo) c = azul;
                if (r < petalo * 0.62f && r > petalo * 0.5f) c = azulClaro;
                if (r < 0.15f) c = crema;
                if (r < 0.07f) c = azul;
                float esq = Mathf.Min(Mathf.Min(Mathf.Sqrt((u - 1f) * (u - 1f) + (v - 1f) * (v - 1f)), Mathf.Sqrt((u + 1f) * (u + 1f) + (v - 1f) * (v - 1f))),
                                      Mathf.Min(Mathf.Sqrt((u - 1f) * (u - 1f) + (v + 1f) * (v + 1f)), Mathf.Sqrt((u + 1f) * (u + 1f) + (v + 1f) * (v + 1f))));
                if (esq < 0.42f) c = azul;
                if (esq < 0.2f) c = crema;
                if (Mathf.Max(Mathf.Abs(u), Mathf.Abs(v)) > 0.94f) c = azulClaro;
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        // 72 px a 200 ppu = mosaicos de 36 unidades de canvas
        spriteTalavera = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 200f, 0, SpriteMeshType.FullRect);
        return spriteTalavera;
    }

    /// <summary>Papel amate: fibras suaves en tonos cálidos.</summary>
    static Sprite Amate()
    {
        if (spriteAmate != null) return spriteAmate;
        const int n = 128;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Color claro = UIMexicana.H("F3E7C6"), oscuro = UIMexicana.H("E0CB98");
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float fibra = Mathf.PerlinNoise(x * 0.09f, y * 0.45f) * 0.55f + Mathf.PerlinNoise(x * 0.35f, y * 0.12f) * 0.3f + Mathf.PerlinNoise(x * 0.8f, y * 0.8f) * 0.15f;
                tex.SetPixel(x, y, Color.Lerp(claro, oscuro, Mathf.SmoothStep(0.25f, 0.8f, fibra)));
            }
        tex.Apply();
        spriteAmate = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        return spriteAmate;
    }

    // ================================================================== CONSTRUCCIÓN DE LA UI

    void ConstruirUI()
    {
        UIMexicana.AsegurarEventSystem();
        Canvas canvas = UIMexicana.CrearCanvas("Codice_Historia", 80);
        canvas.transform.SetParent(transform, false);
        RectTransform raiz = (RectTransform)canvas.transform;

        RectTransform p = UIMexicana.Nuevo("PanelCodice", raiz);
        UIMexicana.Estirar(p);
        panel = p.gameObject;

        Image velo = UIMexicana.Imagen("Velo", p, UIMexicana.Blanco(), new Color(0.02f, 0.03f, 0.09f, 0.82f), false, true);
        UIMexicana.Estirar(velo.rectTransform);

        ConstruirPapelPicado(p);
        ConstruirOfrendas(p);

        // ---------- tapa del libro
        RectTransform libro = UIMexicana.Nuevo("Libro", p);
        UIMexicana.AnclarCentro(libro, new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(1640f, 940f));
        Image sombra = UIMexicana.Imagen("Sombra", libro, UIMexicana.Redondeado(16), new Color(0f, 0f, 0f, 0.55f), true);
        UIMexicana.Estirar(sombra.rectTransform); sombra.rectTransform.offsetMin = new Vector2(-6f, -14f); sombra.rectTransform.offsetMax = new Vector2(6f, -2f);
        Image tapa = UIMexicana.Imagen("Tapa", libro, UIMexicana.Redondeado(16), Indigo, true);
        UIMexicana.Estirar(tapa.rectTransform);

        // greca de talavera alrededor
        CrearBanda(libro, "TalaveraArriba", new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(1612f, 28f));
        CrearBanda(libro, "TalaveraAbajo", new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(1612f, 28f));
        CrearBanda(libro, "TalaveraIzq", new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(28f, 884f));
        CrearBanda(libro, "TalaveraDer", new Vector2(1f, 0.5f), new Vector2(-14f, 0f), new Vector2(28f, 884f));

        Image tapaBorde = UIMexicana.Imagen("Borde", libro, UIMexicana.RedondeadoBorde(16, 4f), UIMexicana.Dorado, true);
        UIMexicana.Estirar(tapaBorde.rectTransform);

        // título con calaveritas de azúcar
        Image calI = UIMexicana.Imagen("CalaveraI", libro, UIMexicana.IconoSprite(UIMexicana.Icono.Calavera), UIMexicana.Rosa);
        UIMexicana.AnclarCentro(calI.rectTransform, new Vector2(0.5f, 1f), new Vector2(-770f, -78f), new Vector2(58f, 58f));
        TextMeshProUGUI titulo = UIMexicana.Texto("Titulo", libro, "CÓDICE DE LA BUFA", 34, UIMexicana.Dorado, TextAlignmentOptions.Left, FontStyles.Bold, 5f);
        UIMexicana.AnclarCentro(titulo.rectTransform, new Vector2(0.5f, 1f), new Vector2(-470f, -68f), new Vector2(520f, 46f));
        txtContador = UIMexicana.Texto("Contador", libro, "", 17, UIMexicana.ConAlfa(UIMexicana.Crema, 0.75f), TextAlignmentOptions.Left, FontStyles.Bold, 2f);
        UIMexicana.AnclarCentro(txtContador.rectTransform, new Vector2(0.5f, 1f), new Vector2(-470f, -102f), new Vector2(520f, 24f));

        // pestañas con colores de papel picado
        for (int i = 0; i < nombresTab.Length; i++)
        {
            int cap = i;
            RectTransform t = UIMexicana.Nuevo("Tab" + i, libro);
            UIMexicana.AnclarCentro(t, new Vector2(0.5f, 1f), new Vector2(150f + i * 250f, -88f), new Vector2(236f, 54f));
            Image f = UIMexicana.Imagen("Fondo", t, UIMexicana.Redondeado(12), Indigo, true, true);
            UIMexicana.Estirar(f.rectTransform);
            Button b = t.gameObject.AddComponent<Button>();
            b.targetGraphic = f;
            ColorBlock cb = b.colors; cb.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f); cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f); b.colors = cb;
            b.onClick.AddListener(() => CambiarTab(cap));
            Image raya = UIMexicana.Imagen("Raya", t, UIMexicana.Blanco(), coloresTab[i]);
            raya.rectTransform.anchorMin = new Vector2(0f, 1f); raya.rectTransform.anchorMax = new Vector2(1f, 1f);
            raya.rectTransform.pivot = new Vector2(0.5f, 1f);
            raya.rectTransform.anchoredPosition = Vector2.zero; raya.rectTransform.sizeDelta = new Vector2(0f, 6f);
            Image bd = UIMexicana.Imagen("Borde", t, UIMexicana.RedondeadoBorde(12, 1.6f), UIMexicana.ConAlfa(coloresTab[i], 0.8f), true);
            UIMexicana.Estirar(bd.rectTransform);
            TextMeshProUGUI tx = UIMexicana.Texto("Texto", t, nombresTab[i], 22, UIMexicana.Crema, TextAlignmentOptions.Center, FontStyles.Bold, 4f);
            UIMexicana.Estirar(tx.rectTransform);
            fondosTab.Add(f);
            rayasTab.Add(raya);
            textosTab.Add(tx);
        }

        // páginas de papel amate
        RectTransform izq = CrearPagina(libro, "PaginaIzq", -395f);
        RectTransform der = CrearPagina(libro, "PaginaDer", 395f);
        Image lomo = UIMexicana.Imagen("Lomo", libro, UIMexicana.Blanco(), new Color(0.25f, 0.15f, 0.08f, 0.55f));
        UIMexicana.AnclarCentro(lomo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -45f), new Vector2(8f, 770f));

        ConstruirPaginaIzquierda(izq);
        ConstruirPaginaDerecha(der);

        panel.SetActive(false);
    }

    void CrearBanda(RectTransform padre, string nombre, Vector2 ancla, Vector2 pos, Vector2 tam)
    {
        Image b = UIMexicana.Imagen(nombre, padre, Talavera(), Color.white);
        b.type = Image.Type.Tiled;
        UIMexicana.AnclarCentro(b.rectTransform, ancla, pos, tam);
    }

    /// <summary>Papel picado colgando de un hilo, a lo ancho de la pantalla, meciéndose.</summary>
    void ConstruirPapelPicado(RectTransform padre)
    {
        Image hilo = UIMexicana.Imagen("Hilo", padre, UIMexicana.Blanco(), UIMexicana.ConAlfa(UIMexicana.Crema, 0.55f));
        UIMexicana.AnclarCentro(hilo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(1900f, 2f));
        int n = 15;
        for (int i = 0; i < n; i++)
        {
            Image f = UIMexicana.Imagen("Bandera" + i, padre, UIMexicana.BanderaPapelPicado(i % 3), UIMexicana.ColoresPapelPicado[i % UIMexicana.ColoresPapelPicado.Length]);
            RectTransform rt = f.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(-910f + i * 130f, -10f);
            rt.sizeDelta = new Vector2(104f, 70f);
            VaivenUI v = f.gameObject.AddComponent<VaivenUI>();
            v.amplitud = 3.5f; v.velocidad = 1.3f; v.fase = i * 0.8f;
        }
    }

    /// <summary>Veladoras, cempasúchil y calaveritas de azúcar a los lados del libro: una ofrenda.</summary>
    void ConstruirOfrendas(RectTransform padre)
    {
        for (int lado = -1; lado <= 1; lado += 2)
        {
            float x = lado * 893f;

            // cempasúchil
            Color[] naranjas = { UIMexicana.H("FF9F1C"), UIMexicana.H("FFB72B"), UIMexicana.H("F07B0B") };
            float[] tam = { 84f, 66f, 74f };
            float[] dx = { 0f, -26f * lado, 24f * lado };
            float[] dy = { 0f, 40f, 52f };
            for (int i = 0; i < 3; i++)
            {
                Image fl = UIMexicana.Imagen("Cempasuchil", padre, UIMexicana.IconoSprite(UIMexicana.Icono.Flor), naranjas[i]);
                UIMexicana.AnclarCentro(fl.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x + dx[i], -400f + dy[i]), new Vector2(tam[i], tam[i]));
            }

            // veladora
            RectTransform vela = UIMexicana.Nuevo("Veladora", padre);
            UIMexicana.AnclarCentro(vela, new Vector2(0.5f, 0.5f), new Vector2(x, -255f), new Vector2(60f, 100f));
            Image resplandor = UIMexicana.Imagen("Resplandor", vela, UIMexicana.Brillo(), new Color(1f, 0.8f, 0.35f, 0.6f));
            UIMexicana.AnclarCentro(resplandor.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 10f), new Vector2(190f, 190f));
            Image vaso = UIMexicana.Imagen("Vaso", vela, UIMexicana.Redondeado(10), UIMexicana.ConAlfa(UIMexicana.H("B3203A"), 0.92f), true);
            UIMexicana.AnclarCentro(vaso.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 42f), new Vector2(56f, 82f));
            Image vasoBorde = UIMexicana.Imagen("VasoBorde", vela, UIMexicana.RedondeadoBorde(10, 2f), UIMexicana.ConAlfa(UIMexicana.Dorado, 0.85f), true);
            UIMexicana.AnclarCentro(vasoBorde.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 42f), new Vector2(56f, 82f));
            Image llama = UIMexicana.Imagen("Llama", vela, UIMexicana.Circulo(), UIMexicana.H("FFD34D"));
            UIMexicana.AnclarCentro(llama.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(18f, 34f));
            ParpadeoUI pa = resplandor.gameObject.AddComponent<ParpadeoUI>();
            pa.imagen = resplandor; pa.fase = lado * 1.7f; pa.minAlfa = 0.38f; pa.maxAlfa = 0.7f;
            ParpadeoUI pb = llama.gameObject.AddComponent<ParpadeoUI>();
            pb.imagen = llama; pb.fase = lado * 0.9f; pb.minAlfa = 0.75f; pb.maxAlfa = 1f;

            // calavera de azúcar con florecita
            Image cal = UIMexicana.Imagen("CalaveraAzucar", padre, UIMexicana.IconoSprite(UIMexicana.Icono.Calavera), UIMexicana.Crema);
            UIMexicana.AnclarCentro(cal.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, -80f), new Vector2(96f, 96f));
            Image rosa = UIMexicana.Imagen("FlorCalavera", padre, UIMexicana.IconoSprite(UIMexicana.Icono.Flor), UIMexicana.Rosa);
            UIMexicana.AnclarCentro(rosa.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x + lado * 28f, -40f), new Vector2(34f, 34f));
        }
    }

    RectTransform CrearPagina(RectTransform libro, string nombre, float x)
    {
        RectTransform pg = UIMexicana.Nuevo(nombre, libro);
        UIMexicana.AnclarCentro(pg, new Vector2(0.5f, 0.5f), new Vector2(x, -45f), new Vector2(770f, 770f));
        Image sombra = UIMexicana.Imagen("Sombra", pg, UIMexicana.Redondeado(8), new Color(0f, 0f, 0f, 0.35f), true);
        UIMexicana.Estirar(sombra.rectTransform); sombra.rectTransform.offsetMin = new Vector2(-2f, -6f); sombra.rectTransform.offsetMax = new Vector2(2f, -1f);
        Image fondo = UIMexicana.Imagen("Amate", pg, Amate(), Color.white);
        UIMexicana.Estirar(fondo.rectTransform);
        Image borde = UIMexicana.Imagen("Borde", pg, UIMexicana.RedondeadoBorde(8, 2f), UIMexicana.ConAlfa(UIMexicana.H("8A6A3A"), 0.8f), true);
        UIMexicana.Estirar(borde.rectTransform);
        Image marco = UIMexicana.Imagen("MarcoInterior", pg, UIMexicana.RedondeadoBorde(8, 1.2f), UIMexicana.ConAlfa(Carmesi, 0.45f), true);
        UIMexicana.Estirar(marco.rectTransform); marco.rectTransform.offsetMin = new Vector2(12f, 12f); marco.rectTransform.offsetMax = new Vector2(-12f, -12f);

        // cempasúchil en las esquinas
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sy = -1; sy <= 1; sy += 2)
            {
                Image fl = UIMexicana.Imagen("EsquinaFlor", pg, UIMexicana.IconoSprite(UIMexicana.Icono.Flor), UIMexicana.ConAlfa(UIMexicana.Cempasuchil, 0.9f));
                UIMexicana.AnclarCentro(fl.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(sx * 366f, sy * 366f), new Vector2(40f, 40f));
            }
        return pg;
    }

    void ConstruirPaginaIzquierda(RectTransform izq)
    {
        TextMeshProUGUI cab = UIMexicana.Texto("Cabecera", izq, "ÍNDICE", 24, Carmesi, TextAlignmentOptions.Center, FontStyles.Bold, 12f);
        UIMexicana.AnclarCentro(cab.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(500f, 36f));

        for (int i = 0; i < PorPagina; i++)
        {
            int cap = i;
            FilaUI fila = new FilaUI();
            RectTransform r = UIMexicana.Nuevo("Fila" + i, izq);
            UIMexicana.AnclarCentro(r, new Vector2(0.5f, 1f), new Vector2(0f, -100f - i * 59f), new Vector2(660f, 52f));
            fila.fondo = UIMexicana.Imagen("Fondo", r, UIMexicana.Redondeado(14), Papel2, true, true);
            UIMexicana.Estirar(fila.fondo.rectTransform);
            fila.boton = r.gameObject.AddComponent<Button>();
            fila.boton.targetGraphic = fila.fondo;
            ColorBlock cb = fila.boton.colors; cb.highlightedColor = new Color(1.07f, 1.04f, 0.95f, 1f); cb.pressedColor = new Color(0.85f, 0.85f, 0.8f, 1f); fila.boton.colors = cb;
            fila.boton.onClick.AddListener(() => Seleccionar(pagina * PorPagina + cap));

            Image medallon = UIMexicana.Imagen("Medallon", r, UIMexicana.Circulo(), UIMexicana.ConAlfa(Carmesi, 0.9f));
            UIMexicana.AnclarCentro(medallon.rectTransform, new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(34f, 34f));
            fila.numero = UIMexicana.Texto("Numero", r, "", 18, UIMexicana.Crema, TextAlignmentOptions.Center, FontStyles.Bold, 0f);
            UIMexicana.AnclarCentro(fila.numero.rectTransform, new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(44f, 34f));
            fila.titulo = UIMexicana.Texto("Titulo", r, "", 21, Tinta, TextAlignmentOptions.Left, FontStyles.Normal, 0.3f);
            UIMexicana.Estirar(fila.titulo.rectTransform);
            fila.titulo.rectTransform.offsetMin = new Vector2(62f, 0f); fila.titulo.rectTransform.offsetMax = new Vector2(-14f, 0f);
            fila.titulo.textWrappingMode = TextWrappingModes.NoWrap;
            fila.titulo.overflowMode = TextOverflowModes.Ellipsis;
            filas.Add(fila);
        }

        btnAnterior = CrearBotonPagina(izq, "Anterior", "<", -120f, () => CambiarPagina(-1));
        btnSiguiente = CrearBotonPagina(izq, "Siguiente", ">", 120f, () => CambiarPagina(1));
        txtPaginador = UIMexicana.Texto("Paginador", izq, "1 / 1", 20, Tinta, TextAlignmentOptions.Center, FontStyles.Bold, 2f);
        UIMexicana.AnclarCentro(txtPaginador.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 46f), new Vector2(160f, 30f));
    }

    void ConstruirPaginaDerecha(RectTransform der)
    {
        txtEtiqueta = UIMexicana.Texto("Etiqueta", der, "", 18, Naranja, TextAlignmentOptions.Center, FontStyles.Bold, 10f);
        UIMexicana.AnclarCentro(txtEtiqueta.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(660f, 26f));

        txtTitulo = UIMexicana.Texto("TituloEntrada", der, "", 36, Carmesi, TextAlignmentOptions.Center, FontStyles.Bold, 1f);
        UIMexicana.AnclarCentro(txtTitulo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(660f, 70f));
        txtTitulo.enableAutoSizing = true; txtTitulo.fontSizeMin = 20; txtTitulo.fontSizeMax = 36;

        Image izqLinea = UIMexicana.Imagen("AdornoI", der, UIMexicana.Blanco(), UIMexicana.ConAlfa(Carmesi, 0.5f));
        UIMexicana.AnclarCentro(izqLinea.rectTransform, new Vector2(0.5f, 1f), new Vector2(-130f, -136f), new Vector2(200f, 3f));
        Image derLinea = UIMexicana.Imagen("AdornoD", der, UIMexicana.Blanco(), UIMexicana.ConAlfa(Carmesi, 0.5f));
        UIMexicana.AnclarCentro(derLinea.rectTransform, new Vector2(0.5f, 1f), new Vector2(130f, -136f), new Vector2(200f, 3f));
        Image flor = UIMexicana.Imagen("Flor", der, UIMexicana.IconoSprite(UIMexicana.Icono.Flor), UIMexicana.Cempasuchil);
        UIMexicana.AnclarCentro(flor.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -136f), new Vector2(34f, 34f));

        txtSub = UIMexicana.Texto("SubEntrada", der, "", 19, UIMexicana.ConAlfa(Tinta, 0.75f), TextAlignmentOptions.Center, FontStyles.Italic, 1f);
        UIMexicana.AnclarCentro(txtSub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(660f, 28f));

        // texto normal (capítulos, prólogo, recuerdos)
        txtCuerpo = UIMexicana.Texto("Cuerpo", der, "", 25, Tinta, TextAlignmentOptions.TopLeft, FontStyles.Normal, 0.3f);
        UIMexicana.AnclarCentro(txtCuerpo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -420f), new Vector2(660f, 410f));
        txtCuerpo.enableAutoSizing = true; txtCuerpo.fontSizeMin = 17; txtCuerpo.fontSizeMax = 27;
        txtCuerpo.lineSpacing = 6f;

        // ---------- carta de lotería (bestiario)
        cartaRaiz = UIMexicana.Nuevo("Carta", der);
        UIMexicana.AnclarCentro(cartaRaiz, new Vector2(0.5f, 1f), new Vector2(-205f, -435f), new Vector2(300f, 440f));
        Image csombra = UIMexicana.Imagen("Sombra", cartaRaiz, UIMexicana.Redondeado(14), new Color(0f, 0f, 0f, 0.35f), true);
        UIMexicana.Estirar(csombra.rectTransform); csombra.rectTransform.offsetMin = new Vector2(-3f, -8f); csombra.rectTransform.offsetMax = new Vector2(3f, -2f);
        Image cmarco = UIMexicana.Imagen("Marco", cartaRaiz, UIMexicana.Redondeado(14), Cobalto, true);
        UIMexicana.Estirar(cmarco.rectTransform);
        Image cinterior = UIMexicana.Imagen("Interior", cartaRaiz, UIMexicana.Redondeado(10), UIMexicana.H("F6ECD2"), true);
        UIMexicana.Estirar(cinterior.rectTransform); cinterior.rectTransform.offsetMin = new Vector2(9f, 9f); cinterior.rectTransform.offsetMax = new Vector2(-9f, -9f);

        cartaCampo = UIMexicana.Imagen("Campo", cartaRaiz, UIMexicana.Redondeado(8), UIMexicana.H("2A9D6F"), true);
        UIMexicana.AnclarCentro(cartaCampo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(262f, 310f));
        Image ccampoBorde = UIMexicana.Imagen("CampoBorde", cartaRaiz, UIMexicana.RedondeadoBorde(8, 2.4f), UIMexicana.ConAlfa(Cobalto, 0.9f), true);
        UIMexicana.AnclarCentro(ccampoBorde.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(262f, 310f));
        Image cbrillo = UIMexicana.Imagen("Halo", cartaRaiz, UIMexicana.Brillo(), new Color(1f, 1f, 1f, 0.35f));
        UIMexicana.AnclarCentro(cbrillo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(250f, 250f));
        cartaEmblema = UIMexicana.Imagen("Emblema", cartaRaiz, UIMexicana.IconoSprite(UIMexicana.Icono.Serpiente), UIMexicana.Crema);
        UIMexicana.AnclarCentro(cartaEmblema.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -172f), new Vector2(200f, 200f));

        Image cnum = UIMexicana.Imagen("Medallon", cartaRaiz, UIMexicana.Circulo(), Carmesi);
        UIMexicana.AnclarCentro(cnum.rectTransform, new Vector2(0f, 1f), new Vector2(46f, -46f), new Vector2(52f, 52f));
        cartaNumero = UIMexicana.Texto("Numero", cartaRaiz, "1", 30, UIMexicana.Crema, TextAlignmentOptions.Center, FontStyles.Bold, 0f);
        UIMexicana.AnclarCentro(cartaNumero.rectTransform, new Vector2(0f, 1f), new Vector2(46f, -46f), new Vector2(52f, 52f));
        for (int s = -1; s <= 1; s += 2)
        {
            Image fl = UIMexicana.Imagen("FlorCarta", cartaRaiz, UIMexicana.IconoSprite(UIMexicana.Icono.Flor), UIMexicana.ConAlfa(UIMexicana.Cempasuchil, 0.95f));
            UIMexicana.AnclarCentro(fl.rectTransform, new Vector2(0.5f, 1f), new Vector2(s * 108f, -305f), new Vector2(34f, 34f));
        }

        Image banner = UIMexicana.Imagen("Banner", cartaRaiz, UIMexicana.Redondeado(8), UIMexicana.H("FBF3DC"), true);
        UIMexicana.AnclarCentro(banner.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 52f), new Vector2(262f, 64f));
        Image bannerBorde = UIMexicana.Imagen("BannerBorde", cartaRaiz, UIMexicana.RedondeadoBorde(8, 2.4f), Carmesi, true);
        UIMexicana.AnclarCentro(bannerBorde.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 52f), new Vector2(262f, 64f));
        cartaNombre = UIMexicana.Texto("Nombre", cartaRaiz, "", 26, Carmesi, TextAlignmentOptions.Center, FontStyles.Bold, 2f);
        UIMexicana.AnclarCentro(cartaNombre.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 52f), new Vector2(246f, 58f));
        cartaNombre.enableAutoSizing = true; cartaNombre.fontSizeMin = 14; cartaNombre.fontSizeMax = 26;

        // versito y texto al lado de la carta
        txtVerso = UIMexicana.Texto("Verso", der, "", 24, Carmesi, TextAlignmentOptions.Center, FontStyles.Italic, 0.5f);
        UIMexicana.AnclarCentro(txtVerso.rectTransform, new Vector2(0.5f, 1f), new Vector2(185f, -255f), new Vector2(330f, 110f));
        txtVerso.enableAutoSizing = true; txtVerso.fontSizeMin = 16; txtVerso.fontSizeMax = 25;
        txtCuerpoCarta = UIMexicana.Texto("CuerpoCarta", der, "", 22, Tinta, TextAlignmentOptions.TopLeft, FontStyles.Normal, 0.2f);
        UIMexicana.AnclarCentro(txtCuerpoCarta.rectTransform, new Vector2(0.5f, 1f), new Vector2(185f, -495f), new Vector2(330f, 330f));
        txtCuerpoCarta.enableAutoSizing = true; txtCuerpoCarta.fontSizeMin = 15; txtCuerpoCarta.fontSizeMax = 23;
        txtCuerpoCarta.lineSpacing = 4f;

        // escuchar de nuevo
        RectTransform rb = UIMexicana.Nuevo("BtnEscuchar", der);
        UIMexicana.AnclarCentro(rb, new Vector2(0.5f, 0f), new Vector2(0f, 56f), new Vector2(340f, 56f));
        Image fb = UIMexicana.Imagen("Fondo", rb, UIMexicana.Redondeado(26), Carmesi, true, true);
        UIMexicana.Estirar(fb.rectTransform);
        btnEscuchar = rb.gameObject.AddComponent<Button>();
        btnEscuchar.targetGraphic = fb;
        ColorBlock cbe = btnEscuchar.colors; cbe.highlightedColor = new Color(1.18f, 1.18f, 1.18f, 1f); cbe.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f); btnEscuchar.colors = cbe;
        btnEscuchar.onClick.AddListener(Escuchar);
        Image bbd = UIMexicana.Imagen("Borde", rb, UIMexicana.RedondeadoBorde(26, 2f), UIMexicana.Dorado, true);
        UIMexicana.Estirar(bbd.rectTransform);
        Image ib = UIMexicana.Imagen("Icono", rb, UIMexicana.IconoSprite(UIMexicana.Icono.Onda), UIMexicana.Crema);
        UIMexicana.AnclarCentro(ib.rectTransform, new Vector2(0f, 0.5f), new Vector2(34f, 0f), new Vector2(30f, 30f));
        TextMeshProUGUI tb = UIMexicana.Texto("Texto", rb, "ESCUCHAR ESTA PÁGINA", 20, UIMexicana.Crema, TextAlignmentOptions.Center, FontStyles.Bold, 2f);
        UIMexicana.Estirar(tb.rectTransform);
        tb.rectTransform.offsetMin = new Vector2(30f, 0f);

        TextMeshProUGUI cerrar = UIMexicana.Texto("Cerrar", der, "TAB / ESC  ·  cerrar el códice", 15, UIMexicana.ConAlfa(Tinta, 0.6f), TextAlignmentOptions.Center, FontStyles.Bold, 2f);
        UIMexicana.AnclarCentro(cerrar.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(500f, 22f));

        cartaRaiz.gameObject.SetActive(false);
        txtVerso.gameObject.SetActive(false);
        txtCuerpoCarta.gameObject.SetActive(false);
    }

    Button CrearBotonPagina(RectTransform pagina, string nombre, string simbolo, float x, UnityAction click)
    {
        RectTransform r = UIMexicana.Nuevo("Btn" + nombre, pagina);
        UIMexicana.AnclarCentro(r, new Vector2(0.5f, 0f), new Vector2(x, 46f), new Vector2(64f, 44f));
        Image f = UIMexicana.Imagen("Fondo", r, UIMexicana.Redondeado(18), Carmesi, true, true);
        UIMexicana.Estirar(f.rectTransform);
        Button b = r.gameObject.AddComponent<Button>();
        b.targetGraphic = f;
        ColorBlock cb = b.colors;
        cb.highlightedColor = new Color(1.18f, 1.18f, 1.18f, 1f); cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        cb.disabledColor = new Color(0.6f, 0.55f, 0.5f, 0.45f);
        b.colors = cb;
        b.onClick.AddListener(click);
        // flecha dibujada (la fuente por defecto no trae ◀ ▶)
        Image flecha = UIMexicana.Imagen("Flecha", r, UIMexicana.IconoSprite(UIMexicana.Icono.Flecha), UIMexicana.Crema);
        UIMexicana.AnclarCentro(flecha.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30f, 30f));
        if (simbolo == "<") flecha.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f);
        return b;
    }
}

/// <summary>Mece un elemento de la interfaz como papel picado con el viento (funciona con el juego en pausa).</summary>
public class VaivenUI : MonoBehaviour
{
    public float amplitud = 4f, velocidad = 1.3f, fase = 0f;

    void Update()
    {
        transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * velocidad + fase) * amplitud);
    }
}

/// <summary>Parpadeo de llama de veladora (alfa que oscila); funciona con el juego en pausa.</summary>
public class ParpadeoUI : MonoBehaviour
{
    public Image imagen;
    public float minAlfa = 0.4f, maxAlfa = 0.8f, fase = 0f;

    void Update()
    {
        if (imagen == null) return;
        float t = Time.unscaledTime * 7f + fase;
        float k = 0.5f + 0.5f * (Mathf.Sin(t) * 0.5f + Mathf.Sin(t * 2.3f + 1f) * 0.3f + Mathf.PerlinNoise(t * 0.5f, fase) * 0.4f);
        Color c = imagen.color; c.a = Mathf.Lerp(minAlfa, maxAlfa, Mathf.Clamp01(k)); imagen.color = c;
        transform.localScale = Vector3.one * (0.94f + 0.1f * k);
    }
}
