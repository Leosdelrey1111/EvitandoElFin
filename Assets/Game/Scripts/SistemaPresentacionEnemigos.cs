using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Sistema de presentación cinematográfica de enemigos al encontrarlos por primera vez.
/// Pausa el juego, hace zoom al enemigo mostrando una acción característica,
/// y despliega información del bestiario (nombre, historia, consejos).
/// Solo se activa una vez por tipo de enemigo en toda la partida.
/// </summary>
public class SistemaPresentacionEnemigos : MonoBehaviour
{
    public static SistemaPresentacionEnemigos Instancia { get; private set; }
    public static bool EnPresentacion { get; private set; }

    [Header("Configuración")]
    public float distanciaActivacion = 15f;
    public float duracionPresentacion = 5f;
    public KeyCode teclaSaltar = KeyCode.Return;

    // Estado de enemigos vistos (se guarda en SistemaGuardado)
    private HashSet<EsbirroBaseIA.TipoEsbirro> enemigosVistos = new HashSet<EsbirroBaseIA.TipoEsbirro>();
    private bool presentacionEnCurso;

    // Referencias
    private Camera camara;
    private Transform jugador;

    // UI
    private GameObject panelPresentacion;
    private CanvasGroup grupoPresentacion;
    private TextMeshProUGUI txtNombre, txtNumero, txtHistoria, txtConsejo;
    private Image imgFondo, imgIcono;
    private RectTransform barraSup, barraInf;

    // Estado de cámara
    private Vector3 posicionCamaraOriginal;
    private Quaternion rotacionCamaraOriginal;
    private float fovOriginal;
    private CamaraTerceraPersona camaraTP;

    // ================================================================== CICLO DE VIDA

    void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        Instancia = this;
        DontDestroyOnLoad(gameObject);

        ConstruirUI();
        CargarEnemigosVistos();
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) jugador = p.transform;

        camara = Camera.main;
        camaraTP = FindAnyObjectByType<CamaraTerceraPersona>();
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    // ================================================================== PRESENTACIÓN

    /// <summary>
    /// Intenta mostrar la presentación de un enemigo si es la primera vez que se ve.
    /// </summary>
    public void IntentarMostrarPresentacion(EsbirroBaseIA enemigo)
    {
        // Las presentaciones de enemigos se realizan con las cinemáticas 3D de CinematicaNarrativa
        // (una sola vez por tipo de enemigo en toda la partida), evitando tarjetas planas amarillas.
        return;
    }

    bool EsVisible(Transform objetivo)
    {
        Vector3 direccion = objetivo.position - camara.transform.position;
        RaycastHit hit;
        if (Physics.Raycast(camara.transform.position, direccion.normalized, out hit, distanciaActivacion))
        {
            if (hit.transform == objetivo || hit.transform.IsChildOf(objetivo))
                return true;
        }
        return false;
    }

    IEnumerator CinematicaPresentacion(EsbirroBaseIA enemigo)
    {
        presentacionEnCurso = true;
        EnPresentacion = true;

        // Marcar enemigo como visto
        enemigosVistos.Add(enemigo.tipo);
        GuardarEnemigoVisto(enemigo.tipo);

        // Pausar el juego
        float timeScaleOriginal = Time.timeScale;
        Time.timeScale = 0f;

        // Bloquear controles
        if (jugador != null)
        {
            ChoferController chofer = jugador.GetComponent<ChoferController>();
            if (chofer != null) chofer.controlBloqueado = true;
        }

        // Guardar estado de cámara
        GuardarEstadoCamara();

        // Desactivar cámara de tercera persona temporalmente
        if (camaraTP != null) camaraTP.enabled = false;

        // Mostrar barras de cine
        yield return MostrarBarrasCine(true);

        // Zoom a enemigo
        yield return ZoomAEnemigo(enemigo.transform);

        // Mostrar información del enemigo
        MostrarInformacionEnemigo(enemigo.tipo);
        yield return FadeInPanel();

        // Esperar duración o salto
        float tiempo = 0f;
        while (tiempo < duracionPresentacion && Time.unscaledTime >= 0f)
        {
            if (Input.GetKeyDown(teclaSaltar) || Input.GetKeyDown(KeyCode.Escape))
                break;
            tiempo += Time.unscaledDeltaTime;
            yield return null;
        }

        // Ocultar panel
        yield return FadeOutPanel();

        // Restaurar cámara
        yield return RestaurarCamara();

        // Ocultar barras de cine
        yield return MostrarBarrasCine(false);

        // Reactivar cámara de tercera persona
        if (camaraTP != null) camaraTP.enabled = true;

        // Reanudar el juego
        Time.timeScale = timeScaleOriginal;

        // Desbloquear controles
        if (jugador != null)
        {
            ChoferController chofer = jugador.GetComponent<ChoferController>();
            if (chofer != null) chofer.controlBloqueado = false;
        }

        EnPresentacion = false;
        presentacionEnCurso = false;
    }

    void GuardarEstadoCamara()
    {
        if (camara != null)
        {
            posicionCamaraOriginal = camara.transform.position;
            rotacionCamaraOriginal = camara.transform.rotation;
            fovOriginal = camara.fieldOfView;
        }
    }

    IEnumerator ZoomAEnemigo(Transform objetivo)
    {
        if (camara == null || objetivo == null) yield break;

        Vector3 posicionObjetivo = objetivo.position + Vector3.up * 1.5f;
        Vector3 direccionCamara = (objetivo.position - posicionObjetivo).normalized;
        Vector3 posicionCamara = posicionObjetivo - direccionCamara * 5f + Vector3.up * 1f;

        float duracion = 1f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duracion;
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            camara.transform.position = Vector3.Lerp(posicionCamaraOriginal, posicionCamara, smooth);
            camara.transform.rotation = Quaternion.Slerp(rotacionCamaraOriginal, 
                Quaternion.LookRotation(posicionObjetivo - camara.transform.position), smooth);
            camara.fieldOfView = Mathf.Lerp(fovOriginal, 40f, smooth);

            yield return null;
        }
    }

    IEnumerator RestaurarCamara()
    {
        if (camara == null) yield break;

        Vector3 posInicial = camara.transform.position;
        Quaternion rotInicial = camara.transform.rotation;
        float fovInicial = camara.fieldOfView;

        float duracion = 0.6f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duracion;
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            camara.transform.position = Vector3.Lerp(posInicial, posicionCamaraOriginal, smooth);
            camara.transform.rotation = Quaternion.Slerp(rotInicial, rotacionCamaraOriginal, smooth);
            camara.fieldOfView = Mathf.Lerp(fovInicial, fovOriginal, smooth);

            yield return null;
        }
    }

    IEnumerator MostrarBarrasCine(bool mostrar)
    {
        if (barraSup == null || barraInf == null) yield break;

        if (mostrar)
        {
            barraSup.gameObject.SetActive(true);
            barraInf.gameObject.SetActive(true);
        }

        float objetivo = mostrar ? 1f : 0f;
        float actual = barraSup.anchorMax.y - 1f;
        float duracion = 0.4f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duracion;
            float k = Mathf.Lerp(actual, objetivo, Mathf.SmoothStep(0f, 1f, t));

            barraSup.anchorMin = new Vector2(0f, 1f - k * 0.12f);
            barraSup.anchorMax = new Vector2(1f, 1f);
            barraInf.anchorMin = new Vector2(0f, 0f);
            barraInf.anchorMax = new Vector2(1f, k * 0.12f);

            yield return null;
        }

        if (!mostrar)
        {
            barraSup.gameObject.SetActive(false);
            barraInf.gameObject.SetActive(false);
        }
    }

    IEnumerator FadeInPanel()
    {
        if (grupoPresentacion == null) yield break;

        panelPresentacion.SetActive(true);
        grupoPresentacion.alpha = 0f;

        float duracion = 0.5f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duracion;
            grupoPresentacion.alpha = Mathf.SmoothStep(0f, 1f, t);
            yield return null;
        }
    }

    IEnumerator FadeOutPanel()
    {
        if (grupoPresentacion == null) yield break;

        float duracion = 0.4f;
        float t = 0f;
        float inicial = grupoPresentacion.alpha;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duracion;
            grupoPresentacion.alpha = Mathf.Lerp(inicial, 0f, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        panelPresentacion.SetActive(false);
    }

    // ================================================================== INFORMACIÓN DE ENEMIGOS

    void MostrarInformacionEnemigo(EsbirroBaseIA.TipoEsbirro tipo)
    {
        HistoriaNivel1.Bestia bestia = ObtenerBestiario(tipo);

        if (txtNumero != null) txtNumero.text = "No. " + bestia.numero;
        if (txtNombre != null) txtNombre.text = bestia.nombre.ToUpper();
        if (txtHistoria != null) txtHistoria.text = bestia.historia;
        if (txtConsejo != null) txtConsejo.text = "✦ " + bestia.consejo;

        // Color según tipo
        Color colorTipo = ObtenerColorTipo(tipo);
        if (imgFondo != null) imgFondo.color = new Color(colorTipo.r, colorTipo.g, colorTipo.b, 0.15f);
        if (txtNombre != null) txtNombre.color = colorTipo;

        // Reproducir sonido
        AudioSource audio = GetComponent<AudioSource>();
        if (audio != null)
        {
            AudioClip clip = UIMexicana.Pluck(new[] { 523.25f, 659.25f, 783.99f }, 0.6f, 0.12f);
            audio.PlayOneShot(clip);
        }
    }

    HistoriaNivel1.Bestia ObtenerBestiario(EsbirroBaseIA.TipoEsbirro tipo)
    {
        string id = "";
        switch (tipo)
        {
            case EsbirroBaseIA.TipoEsbirro.Serpiente: id = "serpiente"; break;
            case EsbirroBaseIA.TipoEsbirro.HombrePiedra: id = "hombre_piedra"; break;
            case EsbirroBaseIA.TipoEsbirro.Comadre: id = "comadre"; break;
            case EsbirroBaseIA.TipoEsbirro.Momia: 
                return new HistoriaNivel1.Bestia(5, "momia", "La Momia de Santa Paula",
                    "La momia del panteón,\nvelocidad y zarpazos son.",
                    "Víctima del cólera de 1833, momificada en la cantera rosa de la Sierra de Guanajuato. Ataca en jauría.",
                    "Veloces y agresivas. Esquiva sus zarpazos laterales y golpéalas con combo rápido.");
            case EsbirroBaseIA.TipoEsbirro.Catrin:
                return new HistoriaNivel1.Bestia(6, "catrin", "El Catrín",
                    "El catrín con su bastón,\nfuego fatuo dispara sin razón.",
                    "Espectro de época virreinal con levita, bombín y bastón. Su bastón dispara proyectiles de fuego fatuo turquesa.",
                    "Ataca a distancia. Acércate en zigzag para esquivar proyectiles y golpéalo en cuerpo a cuerpo.");
            case EsbirroBaseIA.TipoEsbirro.Cuervo:
                return new HistoriaNivel1.Bestia(7, "cuervo", "El Cuervo de Cripta",
                    "El cuervo que graznará,\nen picada te atacará.",
                    "Cuervo espectral que habita las cruces altas del camposanto. Desciende en picada cuando menos lo esperas.",
                    "Espera su picada y esquiva justo antes del impacto, luego contragolpea.");
            case EsbirroBaseIA.TipoEsbirro.Monje:
                return new HistoriaNivel1.Bestia(8, "monje", "El Monje Penitente",
                    "El monje con su candil,\nfuego purificador hasta el fin.",
                    "Monje de la Inquisición con hábito oscuro. Blande un candelabro de plata candente que estalla en fuego purificador.",
                    "Espera a que golpee el suelo con el candelabro y esquiva hacia atrás para evitar la explosión.");
        }

        // Buscar en bestiario del Nivel 1
        foreach (HistoriaNivel1.Bestia b in HistoriaNivel1.Bestiario)
            if (b.id == id) return b;

        // Fallback
        return new HistoriaNivel1.Bestia(0, "desconocido", "Enemigo Desconocido", "", "Un enemigo misterioso.", "Ten cuidado.");
    }

    Color ObtenerColorTipo(EsbirroBaseIA.TipoEsbirro tipo)
    {
        switch (tipo)
        {
            case EsbirroBaseIA.TipoEsbirro.Serpiente: return new Color(0.8f, 0.3f, 0.2f); // Rojo
            case EsbirroBaseIA.TipoEsbirro.HombrePiedra: return new Color(0.6f, 0.5f, 0.4f); // Gris piedra
            case EsbirroBaseIA.TipoEsbirro.Comadre: return new Color(0.7f, 0.3f, 0.7f); // Púrpura
            case EsbirroBaseIA.TipoEsbirro.Momia: return new Color(0.2f, 0.9f, 0.5f); // Verde esmeralda
            case EsbirroBaseIA.TipoEsbirro.Catrin: return new Color(0.3f, 0.9f, 0.9f); // Turquesa
            case EsbirroBaseIA.TipoEsbirro.Cuervo: return new Color(0.3f, 0.3f, 0.4f); // Gris oscuro
            case EsbirroBaseIA.TipoEsbirro.Monje: return new Color(1f, 0.8f, 0.2f); // Dorado
            default: return Color.white;
        }
    }

    // ================================================================== PERSISTENCIA

    void CargarEnemigosVistos()
    {
        if (SistemaGuardado.Datos != null && !string.IsNullOrEmpty(SistemaGuardado.Datos.enemigosVistos))
        {
            string[] vistos = SistemaGuardado.Datos.enemigosVistos.Split(',');
            foreach (string s in vistos)
            {
                if (System.Enum.TryParse(s, out EsbirroBaseIA.TipoEsbirro tipo))
                    enemigosVistos.Add(tipo);
            }
        }
    }

    void GuardarEnemigoVisto(EsbirroBaseIA.TipoEsbirro tipo)
    {
        List<string> lista = new List<string>(enemigosVistos.Count);
        foreach (EsbirroBaseIA.TipoEsbirro t in enemigosVistos)
            lista.Add(t.ToString());

        if (SistemaGuardado.Datos != null)
        {
            SistemaGuardado.Datos.enemigosVistos = string.Join(",", lista.ToArray());
            SistemaGuardado.Guardar();
        }
    }

    // ================================================================== UI

    void ConstruirUI()
    {
        GameObject canvas = new GameObject("CanvasPresentacionEnemigos");
        Canvas c = canvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 100;
        canvas.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvas.AddComponent<GraphicRaycaster>();
        canvas.transform.SetParent(transform);

        // Barras de cine
        barraSup = UIMexicana.Nuevo("BarraCineSup", canvas.transform);
        Image imgSup = UIMexicana.Imagen("Fondo", barraSup, null, Color.black, false, false);
        UIMexicana.Estirar(imgSup.rectTransform);
        UIMexicana.AnclarCentro(barraSup, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1920f, 0f));
        barraSup.gameObject.SetActive(false);

        barraInf = UIMexicana.Nuevo("BarraCineInf", canvas.transform);
        Image imgInf = UIMexicana.Imagen("Fondo", barraInf, null, Color.black, false, false);
        UIMexicana.Estirar(imgInf.rectTransform);
        UIMexicana.AnclarCentro(barraInf, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(1920f, 0f));
        barraInf.gameObject.SetActive(false);

        // Panel de presentación
        panelPresentacion = UIMexicana.Nuevo("PanelPresentacion", canvas.transform).gameObject;
        UIMexicana.Imagen("Fondo", panelPresentacion.transform, null, new Color(0.05f, 0.05f, 0.08f, 0.95f), false, false);
        RectTransform rtPanel = panelPresentacion.GetComponent<RectTransform>();
        UIMexicana.AnclarCentro(rtPanel, new Vector2(0.5f, 0.3f), Vector2.zero, new Vector2(900f, 320f));

        grupoPresentacion = panelPresentacion.AddComponent<CanvasGroup>();
        panelPresentacion.SetActive(false);

        // Fondo decorativo
        imgFondo = UIMexicana.Imagen("Fondo", panelPresentacion.transform, null, Color.white, false, false);
        UIMexicana.AnclarCentro(imgFondo.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(880f, 300f));

        // Borde
        Image borde = UIMexicana.Imagen("Borde", panelPresentacion.transform, null, UIMexicana.Dorado, false, false);
        UIMexicana.AnclarCentro(borde.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 320f));
        Outline outline = borde.gameObject.AddComponent<Outline>();
        outline.effectDistance = new Vector2(3f, -3f);

        // Número
        txtNumero = UIMexicana.Texto("Numero", panelPresentacion.transform, "No. 1", 20, UIMexicana.Dorado, TextAlignmentOptions.Left);
        UIMexicana.AnclarCentro(txtNumero.rectTransform, new Vector2(0f, 1f), new Vector2(30f, -20f), new Vector2(200f, 30f));

        // Nombre
        txtNombre = UIMexicana.Texto("Nombre", panelPresentacion.transform, "ENEMIGO", 32, UIMexicana.Dorado, TextAlignmentOptions.Center, FontStyles.Bold, 2f);
        UIMexicana.AnclarCentro(txtNombre.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(800f, 40f));

        // Historia
        txtHistoria = UIMexicana.Texto("Historia", panelPresentacion.transform, "Historia del enemigo...", 18, Color.white, TextAlignmentOptions.Left);
        UIMexicana.AnclarCentro(txtHistoria.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(820f, 100f));

        // Consejo
        txtConsejo = UIMexicana.Texto("Consejo", panelPresentacion.transform, "✦ Consejo de combate", 20, UIMexicana.Verde, TextAlignmentOptions.Left, FontStyles.Bold);
        UIMexicana.AnclarCentro(txtConsejo.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 25f), new Vector2(820f, 60f));

        // Pista para saltar
        TextMeshProUGUI txtPista = UIMexicana.Texto("Pista", panelPresentacion.transform, "[ ENTER para continuar ]", 16, new Color(0.7f, 0.7f, 0.7f), TextAlignmentOptions.Center);
        UIMexicana.AnclarCentro(txtPista.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, -35f), new Vector2(400f, 25f));

        // AudioSource para efectos
        AudioSource audio = gameObject.AddComponent<AudioSource>();
        audio.spatialBlend = 0f;
        audio.playOnAwake = false;
        audio.volume = 0.6f;
    }
}
