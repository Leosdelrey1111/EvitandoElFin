using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Director de la historia del Nivel 1 (libre para explorar, pero contada en orden).
/// Tonalli, el alebrije guía, va contando la leyenda de La Bufa conforme el Chofer avanza, y La Bufa le contesta en la pelea:
///  - TODO lo que se dice pasa por UNA sola cola: las voces nunca se pisan ni se cortan; cada línea termina antes de que empiece la siguiente.
///  - Los momentos del jefe pasan al frente de la cola (en su orden), pero esperan a que termine la línea que ya se está diciendo.
///  - Cada línea muestra quién habla (retrato y color) y el texto se escribe al ritmo de la voz.
///  - Tarjetas de ACTO (ACTO I · El despertar...) abren cada parte de la historia.
///  - Hitos del mapa, tótems y eventos del jefe disparan los capítulos; cada uno queda escrito en el Códice (TAB).
///  - Tecla R: el alebrije se adelanta y marca el camino hacia el siguiente objetivo.
/// Se construye por código; basta con tener este componente en la escena.
/// </summary>
[DefaultExecutionOrder(-40)]
public class GuiaAlebrije : MonoBehaviour
{
    public static GuiaAlebrije Instancia { get; private set; }

    public KeyCode teclaPista = KeyCode.R;
    public float duracionPista = 5f;

    CriaturaGuia criatura;
    Transform jugador;
    InventarioEspiritual inventario;
    HitoNarrativo[] hitos;
    readonly HashSet<HitoNarrativo> hitosHechos = new HashSet<HitoNarrativo>();
    float proximoAvisoCueva;

    // ------------------------------------------------------------------ cola de diálogo
    enum TipoLinea { Dialogo, TarjetaActo }
    struct Linea
    {
        public TipoLinea tipo;
        public HistoriaNivel1.VozPerfil quien;
        public string texto, clave;
        public int acto; public string tituloActo;
        public string historia; public int indice;   // capítulo y número de línea (null si es una frase suelta)
    }

    /// <summary>Se dispara cuando EMPIEZA a decirse una línea de un capítulo (id, número de línea). Las cinemáticas cambian de toma con esto.</summary>
    public event System.Action<string, int> OnLineaIniciada;
    /// <summary>Se dispara cuando TERMINA de decirse esa línea (voz completa y respiro incluidos).</summary>
    public event System.Action<string, int> OnLineaTerminada;
    readonly List<Linea> prioritaria = new List<Linea>();
    readonly List<Linea> normal = new List<Linea>();
    readonly HashSet<string> contadas = new HashSet<string>();
    Coroutine rutina, rutinaPista;
    float proximaPista;

    /// <summary>true mientras quede algo por decir (el jefe espera a que Tonalli termine antes de despertar).</summary>
    public bool Ocupado { get { return rutina != null || prioritaria.Count > 0 || normal.Count > 0; } }

    // ------------------------------------------------------------------ UI
    CanvasGroup grupoPanel, grupoRecuerdo, grupoActo;
    TextMeshProUGUI txtTexto, txtNombre, txtRecTitulo, txtRecTexto, txtActoNumero, txtActoTitulo;
    Image imgBorde, imgBrillo, imgDisco, imgAro, imgIcono;

    // ------------------------------------------------------------------ voz de respaldo (blips si no hay audio)
    AudioSource fuente;
    AudioClip[] blips;
    AudioClip sonidoActo;

    void Awake()
    {
        Instancia = this;
        if (Object.FindAnyObjectByType<CodiceHistoria>() == null)
            new GameObject("Codice_Historia").AddComponent<CodiceHistoria>();
        ConstruirUI();
        fuente = gameObject.AddComponent<AudioSource>();
        fuente.spatialBlend = 0f;
        fuente.playOnAwake = false;
        fuente.volume = 0.22f;
        blips = new[] { CrearBlip(820f), CrearBlip(980f), CrearBlip(1160f) };
        sonidoActo = UIMexicana.Pluck(new[] { 392f, 523.25f, 659.25f, 784f }, 0.5f, 0.16f);
    }

    void Start()
    {
        criatura = Object.FindAnyObjectByType<CriaturaGuia>();
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            jugador = p.transform;
            inventario = p.GetComponent<InventarioEspiritual>();
            if (inventario != null) inventario.OnCambioTotems += AlCambiarTotems;
        }
        hitos = Object.FindObjectsByType<HitoNarrativo>();
        VozNivel.Precargar();   // todas las voces listas desde ya: así no hay tirón (y desfase) la primera vez que habla el jefe
    }

    void OnDestroy()
    {
        if (inventario != null) inventario.OnCambioTotems -= AlCambiarTotems;
        if (Instancia == this) Instancia = null;
    }

    // ================================================================== API

    /// <summary>
    /// Cuenta un capítulo (una sola vez) con la voz de Tonalli. Si abre un acto, antes se muestra su tarjeta de título.
    /// Si es urgente pasa al frente de la cola, pero NUNCA corta la línea que ya se está diciendo.
    /// </summary>
    public void ContarHistoria(string id, bool urgente = false)
    {
        if (contadas.Contains(id)) return;
        string[] lineas = HistoriaNivel1.Lineas(id);
        if (lineas == null) return;
        contadas.Add(id);
        if (HistoriaNivel1.IndiceCapitulo(id) >= 0) CodiceHistoria.Registrar(id);   // queda escrito en el libro

        List<Linea> destino = urgente ? prioritaria : normal;

        int acto; string tituloActo;
        if (HistoriaNivel1.AbreActo(id, out acto, out tituloActo))
            destino.Add(new Linea { tipo = TipoLinea.TarjetaActo, acto = acto, tituloActo = tituloActo });

        for (int i = 0; i < lineas.Length; i++)
            destino.Add(new Linea
            {
                tipo = TipoLinea.Dialogo, quien = HistoriaNivel1.VozPerfil.Tonalli,
                texto = lineas[i], clave = HistoriaNivel1.ClaveLinea(id, i),
                historia = id, indice = i
            });
        Arrancar();
    }

    /// <summary>
    /// Una frase suelta de un personaje (por ejemplo La Bufa). Si es urgente se dice ANTES de lo que ya esté esperando en la cola
    /// (para que la frase del jefe llegue junto con su acción y no detrás de comentarios viejos), pero nunca corta la línea en curso.
    /// 'historia' e 'indice' la enlazan a un capítulo para que las cinemáticas sepan que forma parte de él (indice -1 = antes de su primera línea).
    /// </summary>
    public void Decir(HistoriaNivel1.VozPerfil quien, string texto, string claveVoz, bool urgente = true, string historia = null, int indice = 0)
    {
        Linea l = new Linea { tipo = TipoLinea.Dialogo, quien = quien, texto = texto, clave = claveVoz, historia = historia, indice = indice };
        if (urgente)
        {
            int pos = 0;   // se conserva el orden entre varias frases sueltas del mismo personaje
            while (pos < prioritaria.Count && prioritaria[pos].tipo == TipoLinea.Dialogo && prioritaria[pos].quien == quien && (prioritaria[pos].historia == null || prioritaria[pos].indice == -1)) pos++;
            prioritaria.Insert(pos, l);
        }
        else normal.Add(l);
        Arrancar();
    }

    static Linea Sacar(List<Linea> cola)
    {
        Linea l = cola[0];
        cola.RemoveAt(0);
        return l;
    }

    /// <summary>Atajo para los eventos del jefe (ids "jefe_...").</summary>
    public void Jefe(string sufijo) { ContarHistoria("jefe_" + sufijo, true); }

    public void MostrarRecuerdo(string titulo, string texto)
    {
        if (inventario != null) inventario.DesbloquearRecuerdo(titulo);
        CodiceHistoria.Registrar("recuerdo1");
        StartCoroutine(RutinaRecuerdo(titulo, texto));
    }

    void Arrancar()
    {
        if (rutina == null) rutina = StartCoroutine(Procesar());
    }

    // ================================================================== HITOS Y EVENTOS

    void Update()
    {
        if (jugador == null) return;

        if (hitos != null && EstadoNivel.Actual != EstadoDeNivel.Intro)
        {
            for (int i = 0; i < hitos.Length; i++)
            {
                HitoNarrativo h = hitos[i];
                if (h == null || hitosHechos.Contains(h)) continue;
                Vector3 d = jugador.position - h.transform.position; d.y = 0f;
                if (d.magnitude > h.radio) continue;

                if (h.idHistoria == "cueva")
                {
                    int req = GameManager.Instancia != null ? GameManager.Instancia.totemsRequeridos : 3;
                    int tengo = inventario != null ? inventario.cantidadTotems : 0;
                    if (tengo >= req) { hitosHechos.Add(h); ContarHistoria("cueva"); }
                    else if (Time.time >= proximoAvisoCueva)
                    {
                        proximoAvisoCueva = Time.time + 45f;
                        contadas.Remove("cueva_cerrada");
                        ContarHistoria("cueva_cerrada");
                    }
                }
                else
                {
                    hitosHechos.Add(h);
                    ContarHistoria(h.idHistoria);
                }
            }
        }

        if (Input.GetKeyDown(teclaPista) && Time.timeScale > 0.01f && Time.time >= proximaPista)
        {
            proximaPista = Time.time + duracionPista + 1f;
            if (rutinaPista != null) StopCoroutine(rutinaPista);
            rutinaPista = StartCoroutine(RutinaPista());
        }
    }

    void AlCambiarTotems(int n)
    {
        if (n >= 1) ContarHistoria("totem1");
        if (n >= 2) ContarHistoria("totem2");
        if (n >= 3) ContarHistoria("totem3");
    }

    // ================================================================== PISTA (R)

    Transform ObjetivoPista()
    {
        int req = GameManager.Instancia != null ? GameManager.Instancia.totemsRequeridos : 3;
        int tengo = inventario != null ? inventario.cantidadTotems : 0;

        if (tengo < req)
        {
            TotemColeccionable mejor = null; float dMejor = float.MaxValue;
            foreach (TotemColeccionable t in Object.FindObjectsByType<TotemColeccionable>())
            {
                float d = (t.transform.position - jugador.position).sqrMagnitude;
                if (d < dMejor) { dMejor = d; mejor = t; }
            }
            if (mejor != null) return mejor.transform;
        }

        if (JefeLaBufa.Instancia != null && !JefeLaBufa.Instancia.Derrotada) return JefeLaBufa.Instancia.transform;
        PortalVictoria portal = Object.FindAnyObjectByType<PortalVictoria>();
        return portal != null ? portal.transform : null;
    }

    IEnumerator RutinaPista()
    {
        if (criatura == null || jugador == null) yield break;
        Transform obj = ObjetivoPista();
        if (obj == null) yield break;

        criatura.apuntarA = obj;
        criatura.hablando = true;
        AvisosJuego.Mostrar("Tonalli te marca el camino…", 3f);
        yield return new WaitForSeconds(duracionPista);
        criatura.apuntarA = null;
        if (rutina == null) criatura.hablando = false;
        rutinaPista = null;
    }

    // ================================================================== NARRACIÓN (una línea a la vez, en orden)

    IEnumerator Procesar()
    {
        bool primera = true;
        HistoriaNivel1.VozPerfil anterior = HistoriaNivel1.VozPerfil.Tonalli;

        while (prioritaria.Count > 0 || normal.Count > 0)
        {
            // si el Chofer cayó, la historia se detiene
            if (EstadoNivel.Terminado && EstadoNivel.Resultado == ResultadoNivel.Derrota) { prioritaria.Clear(); normal.Clear(); break; }

            Linea linea = Sacar(prioritaria.Count > 0 ? prioritaria : normal);

            if (linea.tipo == TipoLinea.TarjetaActo)
            {
                if (grupoPanel.alpha > 0f) yield return Fundido(grupoPanel, 0f, 0.3f);
                yield return MostrarTarjetaActo(linea);
                primera = true;
                continue;
            }

            // pausa breve entre líneas (más larga si cambia quien habla)
            if (!primera) yield return new WaitForSeconds(anterior != linea.quien ? 0.45f : 0.22f);
            primera = false;
            anterior = linea.quien;

            if (criatura != null) criatura.hablando = linea.quien == HistoriaNivel1.VozPerfil.Tonalli;

            // Calcular duración real del clip de voz para que la toma cinemática dure exactamente lo mismo que la voz y el texto
            CanalVoz canal = linea.quien == HistoriaNivel1.VozPerfil.LaBufa ? CanalVoz.Personaje : CanalVoz.Guia;
            AudioClip clipPre = VozNivel.Obtener(linea.clave);
            float duracionLinea = clipPre != null
                ? clipPre.length + (canal == CanalVoz.Personaje ? 0.55f : 0.35f)
                : Mathf.Max(3.0f, (linea.texto != null ? linea.texto.Length : 40) * 0.065f);

            // Preparar y encuadrar la toma de cámara ANTES de soltar la voz y el texto para que no haya desfase por el fundido
            if (linea.historia != null && CinematicaNarrativa.Instancia != null)
                yield return CinematicaNarrativa.Instancia.PrepararTomaSincronizada(linea.historia, linea.indice, duracionLinea);

            if (linea.historia != null && OnLineaIniciada != null) OnLineaIniciada(linea.historia, linea.indice);
            yield return Mostrar(linea);
            if (linea.historia != null && OnLineaTerminada != null) OnLineaTerminada(linea.historia, linea.indice);
        }

        yield return new WaitForSeconds(0.4f);
        if (prioritaria.Count == 0 && normal.Count == 0)
        {
            yield return Fundido(grupoPanel, 0f, 0.5f);
            if (criatura != null && rutinaPista == null) criatura.hablando = false;
        }
        rutina = null;
        if (prioritaria.Count > 0 || normal.Count > 0) Arrancar();   // llegó algo mientras se apagaba la caja
    }

    IEnumerator Mostrar(Linea linea)
    {
        AplicarEstilo(linea.quien);
        string texto = linea.texto;
        txtTexto.text = texto;
        txtTexto.maxVisibleCharacters = 0;
        txtTexto.ForceMeshUpdate();
        int total = txtTexto.textInfo.characterCount;

        if (grupoPanel.alpha < 1f) yield return Fundido(grupoPanel, 1f, 0.3f);

        // Con voz: el texto avanza según el TIEMPO REAL de reproducción del audio (no con un reloj propio), así texto y voz nunca se
        // desfasan aunque haya un tirón de frames, una pausa o un eco largo; la línea termina cuando la voz termina de sonar.
        // Sin voz (audio sin generar): máquina de escribir con "blips".
        CanalVoz canal = linea.quien == HistoriaNivel1.VozPerfil.LaBufa ? CanalVoz.Personaje : CanalVoz.Guia;
        AudioClip voz = VozNivel.Reproducir(linea.clave, canal);

        if (voz != null)
        {
            float tramoTexto = Mathf.Max(0.5f, voz.length * 0.92f);   // el texto acompaña toda la frase de voz
            float arranque = 0f;
            while (txtTexto.maxVisibleCharacters < total)
            {
                bool pausa = Time.timeScale <= 0.01f;                 // en pausa la voz se congela y el texto también
                float tv = VozNivel.Tiempo(canal);
                bool suena = pausa || VozNivel.Hablando(canal);
                if (!suena)
                {
                    arranque += Time.unscaledDeltaTime;               // la voz aún no arranca (carga del clip)
                    if (arranque > 0.5f) break;                       // no sonó o ya terminó: se muestra completo
                    yield return null;
                    continue;
                }
                if (CinematicaNarrativa.Instancia != null)
                    CinematicaNarrativa.Instancia.SincronizarRelojVoz(tv, voz.length);
                txtTexto.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(total * Mathf.Clamp01(tv / tramoTexto)));
                yield return null;
            }
            txtTexto.maxVisibleCharacters = total;

            // la línea no termina hasta que acaba la voz; La Bufa lleva eco de piedra, así que se deja respirar más
            float guardia = 0f;
            while ((VozNivel.Hablando(canal) || Time.timeScale <= 0.01f) && guardia < voz.length + 3f)
            {
                if (Time.timeScale > 0.01f)
                {
                    guardia += Time.deltaTime;
                    if (CinematicaNarrativa.Instancia != null && VozNivel.Hablando(canal))
                        CinematicaNarrativa.Instancia.SincronizarRelojVoz(VozNivel.Tiempo(canal), voz.length);
                }
                yield return null;
            }
            yield return new WaitForSeconds(canal == CanalVoz.Personaje ? 0.55f : 0.35f);
            yield break;
        }

        float cps = 46f;
        float t = 0f; int ultimoBlip = 0;
        while (txtTexto.maxVisibleCharacters < total)
        {
            t += Time.deltaTime;
            int visibles = Mathf.Min(total, Mathf.FloorToInt(t * cps));
            txtTexto.maxVisibleCharacters = visibles;
            if (visibles - ultimoBlip >= 3)
            {
                ultimoBlip = visibles;
                fuente.pitch = linea.quien == HistoriaNivel1.VozPerfil.LaBufa ? Random.Range(0.55f, 0.7f) : Random.Range(0.92f, 1.12f);
                fuente.PlayOneShot(blips[Random.Range(0, blips.Length)]);
            }
            yield return null;
        }
        txtTexto.maxVisibleCharacters = total;
        yield return new WaitForSeconds(Mathf.Max(2.4f, texto.Length * 0.045f));
    }

    IEnumerator MostrarTarjetaActo(Linea l)
    {
        txtActoNumero.text = "ACTO " + Romano(l.acto);
        txtActoTitulo.text = l.tituloActo;
        fuente.pitch = 1f;
        fuente.PlayOneShot(sonidoActo, 0.9f);
        yield return Fundido(grupoActo, 1f, 0.9f);
        yield return new WaitForSeconds(2.6f);
        yield return Fundido(grupoActo, 0f, 0.9f);
        yield return new WaitForSeconds(0.25f);
    }

    static string Romano(int n)
    {
        string[] r = { "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };
        return n >= 0 && n < r.Length ? r[n] : n.ToString();
    }

    IEnumerator Fundido(CanvasGroup g, float objetivo, float seg)
    {
        float ini = g.alpha, t = 0f;
        while (t < seg)
        {
            t += Time.unscaledDeltaTime;
            g.alpha = Mathf.Lerp(ini, objetivo, t / seg);
            yield return null;
        }
        g.alpha = objetivo;
    }

    IEnumerator RutinaRecuerdo(string titulo, string texto)
    {
        txtRecTitulo.text = titulo;
        txtRecTexto.text = texto;
        yield return Fundido(grupoRecuerdo, 1f, 0.8f);
        yield return new WaitForSeconds(9f);
        yield return Fundido(grupoRecuerdo, 0f, 1.2f);
    }

    // ================================================================== ESTILO POR PERSONAJE

    void AplicarEstilo(HistoriaNivel1.VozPerfil quien)
    {
        switch (quien)
        {
            case HistoriaNivel1.VozPerfil.LaBufa:
                txtNombre.text = HistoriaNivel1.NombreJefe + "  ·  " + HistoriaNivel1.SubtituloJefe.ToUpper();
                txtNombre.color = UIMexicana.H("FFA01C");
                imgBorde.color = UIMexicana.ConAlfa(UIMexicana.H("FFA01C"), 0.9f);
                imgBrillo.color = UIMexicana.ConAlfa(UIMexicana.H("FF7A00"), 0.8f);
                imgDisco.color = UIMexicana.H("3A3735");
                imgAro.color = UIMexicana.H("FFA01C");
                imgIcono.sprite = UIMexicana.IconoSprite(UIMexicana.Icono.Calavera);
                imgIcono.color = UIMexicana.Crema;
                txtTexto.fontStyle = FontStyles.Italic;
                break;
            case HistoriaNivel1.VozPerfil.DonChuy:
                txtNombre.text = "DON CHUY  ·  MERCADER DE ÁNIMAS";
                txtNombre.color = UIMexicana.Dorado;
                imgBorde.color = UIMexicana.ConAlfa(UIMexicana.Dorado, 0.9f);
                imgBrillo.color = UIMexicana.ConAlfa(UIMexicana.Dorado, 0.7f);
                imgDisco.color = UIMexicana.H("7A2E1A");
                imgAro.color = UIMexicana.Dorado;
                imgIcono.sprite = UIMexicana.IconoSprite(UIMexicana.Icono.Alma);
                imgIcono.color = UIMexicana.Crema;
                txtTexto.fontStyle = FontStyles.Normal;
                break;
            default:
                txtNombre.text = HistoriaNivel1.NombreGuia + "  ·  ALEBRIJE";
                txtNombre.color = UIMexicana.Cempasuchil;
                imgBorde.color = UIMexicana.ConAlfa(UIMexicana.Cempasuchil, 0.85f);
                imgBrillo.color = UIMexicana.ConAlfa(UIMexicana.Turquesa, 0.8f);
                imgDisco.color = UIMexicana.H("FF7A1A");
                imgAro.color = UIMexicana.H("E4007C");
                imgIcono.sprite = UIMexicana.IconoSprite(UIMexicana.Icono.Flor);
                imgIcono.color = UIMexicana.H("2EC4B6");
                txtTexto.fontStyle = FontStyles.Normal;
                break;
        }
    }

    // ================================================================== UI

    void ConstruirUI()
    {
        Canvas canvas = UIMexicana.CrearCanvas("Narrador_Alebrije", 55);
        canvas.transform.SetParent(transform, false);
        RectTransform raiz = (RectTransform)canvas.transform;

        // ---------- caja de diálogo (abajo a la izquierda)
        RectTransform panel = UIMexicana.Nuevo("Dialogo", raiz);
        UIMexicana.Anclar(panel, new Vector2(0f, 0f), new Vector2(36f, 36f), new Vector2(700f, 150f));
        grupoPanel = panel.gameObject.AddComponent<CanvasGroup>();
        grupoPanel.alpha = 0f;
        grupoPanel.blocksRaycasts = false;
        grupoPanel.interactable = false;

        Image sombra = UIMexicana.Imagen("Sombra", panel, UIMexicana.Redondeado(26), new Color(0f, 0f, 0f, 0.4f), true);
        UIMexicana.Estirar(sombra.rectTransform);
        sombra.rectTransform.offsetMin = new Vector2(-2f, -5f); sombra.rectTransform.offsetMax = new Vector2(2f, -2f);
        Image fondo = UIMexicana.Imagen("Fondo", panel, UIMexicana.Redondeado(26), new Color(0.05f, 0.07f, 0.19f, 0.88f), true);
        UIMexicana.Estirar(fondo.rectTransform);
        imgBorde = UIMexicana.Imagen("Borde", panel, UIMexicana.RedondeadoBorde(26, 2f), UIMexicana.ConAlfa(UIMexicana.Cempasuchil, 0.85f), true);
        UIMexicana.Estirar(imgBorde.rectTransform);

        // retrato (cambia según quien habla)
        RectTransform retrato = UIMexicana.Nuevo("Retrato", panel);
        UIMexicana.AnclarCentro(retrato, new Vector2(0f, 0.5f), new Vector2(72f, 0f), new Vector2(104f, 104f));
        imgBrillo = UIMexicana.Imagen("Brillo", retrato, UIMexicana.Brillo(), UIMexicana.ConAlfa(UIMexicana.Turquesa, 0.8f));
        UIMexicana.AnclarCentro(imgBrillo.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 150f));
        imgDisco = UIMexicana.Imagen("Disco", retrato, UIMexicana.Circulo(), UIMexicana.H("FF7A1A"));
        UIMexicana.AnclarCentro(imgDisco.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(88f, 88f));
        imgAro = UIMexicana.Imagen("Aro", retrato, UIMexicana.Circulo(), UIMexicana.H("E4007C"));
        UIMexicana.AnclarCentro(imgAro.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60f, 60f));
        imgIcono = UIMexicana.Imagen("Icono", retrato, UIMexicana.IconoSprite(UIMexicana.Icono.Flor), UIMexicana.H("2EC4B6"));
        UIMexicana.AnclarCentro(imgIcono.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(52f, 52f));

        txtNombre = UIMexicana.Texto("Nombre", panel, HistoriaNivel1.NombreGuia + "  ·  ALEBRIJE", 17, UIMexicana.Cempasuchil, TextAlignmentOptions.Left, FontStyles.Bold, 4f);
        UIMexicana.Anclar(txtNombre.rectTransform, new Vector2(0f, 1f), new Vector2(140f, -14f), new Vector2(540f, 26f));

        txtTexto = UIMexicana.Texto("Texto", panel, "", 23, UIMexicana.Crema, TextAlignmentOptions.TopLeft, FontStyles.Normal, 0.5f);
        UIMexicana.Anclar(txtTexto.rectTransform, new Vector2(0f, 1f), new Vector2(140f, -44f), new Vector2(540f, 98f));

        TextMeshProUGUI pista = UIMexicana.Texto("Pista", panel, "R · pista   ·   TAB · códice", 13, UIMexicana.ConAlfa(UIMexicana.Crema, 0.45f), TextAlignmentOptions.Right, FontStyles.Bold, 2f);
        UIMexicana.Anclar(pista.rectTransform, new Vector2(1f, 0f), new Vector2(-18f, 8f), new Vector2(300f, 20f));
        pista.rectTransform.pivot = new Vector2(1f, 0f);

        // ---------- tarjeta de ACTO (centro-arriba)
        RectTransform acto = UIMexicana.Nuevo("TarjetaActo", raiz);
        UIMexicana.AnclarCentro(acto, new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(1920f, 240f));
        grupoActo = acto.gameObject.AddComponent<CanvasGroup>();
        grupoActo.alpha = 0f; grupoActo.blocksRaycasts = false;
        Image banda = UIMexicana.Imagen("Banda", acto, UIMexicana.Blanco(), new Color(0.02f, 0.03f, 0.09f, 0.62f));
        UIMexicana.AnclarCentro(banda.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920f, 200f));
        Image lineaSup = UIMexicana.Imagen("LineaSup", acto, UIMexicana.Blanco(), UIMexicana.ConAlfa(UIMexicana.Dorado, 0.9f));
        UIMexicana.AnclarCentro(lineaSup.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 100f), new Vector2(1100f, 3f));
        Image lineaInf = UIMexicana.Imagen("LineaInf", acto, UIMexicana.Blanco(), UIMexicana.ConAlfa(UIMexicana.Dorado, 0.9f));
        UIMexicana.AnclarCentro(lineaInf.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -100f), new Vector2(1100f, 3f));
        for (int i = -1; i <= 1; i += 2)
        {
            Image fl = UIMexicana.Imagen("FlorActo", acto, UIMexicana.IconoSprite(UIMexicana.Icono.Flor), UIMexicana.Cempasuchil);
            UIMexicana.AnclarCentro(fl.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(i * 590f, 0f), new Vector2(56f, 56f));
        }
        txtActoNumero = UIMexicana.Texto("ActoNumero", acto, "ACTO I", 30, UIMexicana.Cempasuchil, TextAlignmentOptions.Center, FontStyles.Bold, 18f);
        UIMexicana.AnclarCentro(txtActoNumero.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 46f), new Vector2(1000f, 44f));
        txtActoTitulo = UIMexicana.Texto("ActoTitulo", acto, "", 64, UIMexicana.Crema, TextAlignmentOptions.Center, FontStyles.Bold, 4f);
        UIMexicana.AnclarCentro(txtActoTitulo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -24f), new Vector2(1100f, 84f));
        txtActoTitulo.enableAutoSizing = true; txtActoTitulo.fontSizeMin = 36; txtActoTitulo.fontSizeMax = 64;

        // ---------- tarjeta de recuerdo (centro)
        RectTransform rec = UIMexicana.Nuevo("Recuerdo", raiz);
        UIMexicana.AnclarCentro(rec, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(860f, 270f));
        grupoRecuerdo = rec.gameObject.AddComponent<CanvasGroup>();
        grupoRecuerdo.alpha = 0f;
        grupoRecuerdo.blocksRaycasts = false;

        Image rs = UIMexicana.Imagen("Sombra", rec, UIMexicana.Redondeado(30), new Color(0f, 0f, 0f, 0.5f), true);
        UIMexicana.Estirar(rs.rectTransform); rs.rectTransform.offsetMin = new Vector2(-4f, -8f); rs.rectTransform.offsetMax = new Vector2(4f, -2f);
        Image rf = UIMexicana.Imagen("Fondo", rec, UIMexicana.Redondeado(30), new Color(0.12f, 0.1f, 0.09f, 0.94f), true);
        UIMexicana.Estirar(rf.rectTransform);
        Image rb = UIMexicana.Imagen("Borde", rec, UIMexicana.RedondeadoBorde(30, 3f), UIMexicana.Dorado, true);
        UIMexicana.Estirar(rb.rectTransform);
        Image rb2 = UIMexicana.Imagen("Borde2", rec, UIMexicana.RedondeadoBorde(30, 1.2f), UIMexicana.ConAlfa(UIMexicana.Cempasuchil, 0.6f), true);
        UIMexicana.Estirar(rb2.rectTransform); rb2.rectTransform.offsetMin = new Vector2(10f, 10f); rb2.rectTransform.offsetMax = new Vector2(-10f, -10f);

        Image flo = UIMexicana.Imagen("Flor", rec, UIMexicana.IconoSprite(UIMexicana.Icono.Flor), UIMexicana.Cempasuchil);
        UIMexicana.AnclarCentro(flo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(46f, 46f));
        txtRecTitulo = UIMexicana.Texto("Titulo", rec, "", 30, UIMexicana.Dorado, TextAlignmentOptions.Center, FontStyles.Bold, 5f);
        UIMexicana.AnclarCentro(txtRecTitulo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -86f), new Vector2(780f, 40f));
        txtRecTexto = UIMexicana.Texto("Texto", rec, "", 25, UIMexicana.Crema, TextAlignmentOptions.Center, FontStyles.Italic, 0.5f);
        UIMexicana.AnclarCentro(txtRecTexto.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -32f), new Vector2(760f, 130f));
    }

    static AudioClip CrearBlip(float frecuencia)
    {
        int rate = 22050, n = (int)(rate * 0.05f);
        float[] d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate;
            float env = Mathf.Sin(Mathf.PI * i / n);
            d[i] = (Mathf.Sin(2f * Mathf.PI * frecuencia * t) * 0.7f + Mathf.Sin(2f * Mathf.PI * frecuencia * 2f * t) * 0.2f) * env;
        }
        AudioClip c = AudioClip.Create("BlipTonalli", n, 1, rate, false);
        c.SetData(d, 0);
        return c;
    }
}
