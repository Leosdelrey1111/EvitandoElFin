using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Catálogo de las pistas musicales del proyecto en Assets/Game/Resources/Musica/.
/// </summary>
public enum PistaMusica
{
    MenuEvitandoElFin = 0,
    CaminoEntornoBase = 1,
    JefeNivel1Pelea = 2,
    JefeNivel2 = 3,
    JefeNivel3Pelea = 4
}

/// <summary>
/// Gestor y reproductor de música en bucle continuo (loop):
/// - En el Menú Principal reproduce MenuEvitandoElFin en loop.
/// - Durante el juego (fuera de combate de jefe) reproduce CaminoEntornoBase en loop.
/// - Al enfrentar a cada jefe cambia a su canción correspondiente (JefeNivel1Pelea, JefeNivel2, JefeNivel3Pelea) en loop.
/// - Al derrotar al jefe detiene la música de jefe y vuelve a reproducir CaminoEntornoBase en loop.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class ReproductorMusicaLoop : MonoBehaviour
{
    public const string CarpetaResources = "Musica";

    public static ReproductorMusicaLoop Instancia { get; private set; }

    [Header("Canción Actual")]
    [Tooltip("Pista del catálogo en Assets/Game/Resources/Musica.")]
    public PistaMusica pista = PistaMusica.CaminoEntornoBase;

    [Tooltip("Clip asignado directamente. Si está vacío, se carga automáticamente según 'pista'.")]
    public AudioClip clip;

    [Header("Configuración de Loop")]
    [Tooltip("Mantiene la canción repitiéndose infinitamente.")]
    public bool enLoop = true;

    [Tooltip("Inicia la reproducción automáticamente al activarse el objeto.")]
    public bool reproducirAlIniciar = true;

    [Tooltip("Si es mayor a 0, funde suavemente el final de la canción con el inicio al repetir el loop (en segundos). En 0 hace loop exacto por muestra.")]
    [Range(0f, 2.5f)]
    public float crossfadeLoopSegundos = 0f;

    [Header("Volumen")]
    [Range(0f, 1f)]
    public float volumenBase = 0.65f;

    [Tooltip("Ajusta el volumen automáticamente con el slider de Música de OpcionesJuego.")]
    public bool sincronizarConOpciones = true;

    [Tooltip("Segundos de fundido de entrada al cambiar de canción (0 = instantáneo).")]
    [Range(0f, 3f)]
    public float fadeEntradaSegundos = 0.35f;

    /// <summary>Multiplicador temporal usado por transiciones de escena (1 = volumen normal, 0 = silencio).</summary>
    [System.NonSerialized]
    public float multiplicadorFadeExterno = 1f;

    public bool EnCombateJefe { get { return enCombateJefe; } }
    public AudioSource FuenteActiva { get { return fuentePrincipal; } }

    private AudioSource fuentePrincipal;
    private AudioSource fuenteSecundaria;
    private float factorFadeEntrada = 1f;
    private bool usandoCrossfade;
    private bool enCombateJefe;
    private bool esGestorGlobal;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InicializarAutomaticamente()
    {
        AsegurarInstancia();
        if (Instancia != null)
        {
            Instancia.SincronizarConEscena(SceneManager.GetActiveScene().name);
        }
    }

    public static ReproductorMusicaLoop AsegurarInstancia()
    {
        if (Instancia != null) return Instancia;

        ReproductorMusicaLoop existente = Object.FindAnyObjectByType<ReproductorMusicaLoop>();
        if (existente != null)
        {
            Instancia = existente;
            return Instancia;
        }

        GameObject go = new GameObject("GestorMusicaLoop");
        Object.DontDestroyOnLoad(go);
        AudioSource src = go.AddComponent<AudioSource>();
        src.loop = true;
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        src.priority = 0;

        ReproductorMusicaLoop rep = go.AddComponent<ReproductorMusicaLoop>();
        rep.esGestorGlobal = true;
        rep.reproducirAlIniciar = false;
        rep.enLoop = true;
        Instancia = rep;
        return Instancia;
    }

    void Awake()
    {
        if (Instancia == null)
        {
            Instancia = this;
            if (transform.parent == null)
            {
                Object.DontDestroyOnLoad(gameObject);
                esGestorGlobal = true;
            }
        }
        else if (Instancia != this)
        {
            // Si había un prefab local en la escena, delega su pista al gestor global para no duplicar música
            Instancia.CambiarPista(pista);
            Destroy(gameObject);
            return;
        }

        ConfigurarFuentes();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    void Start()
    {
        if (reproducirAlIniciar && (fuentePrincipal == null || !fuentePrincipal.isPlaying))
        {
            Reproducir();
        }
    }

    void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        if (Instancia != this) return;
        multiplicadorFadeExterno = 1f;
        SincronizarConEscena(escena.name);
    }

    /// <summary>
    /// Selecciona automáticamente MenuEvitandoElFin en el menú o CaminoEntornoBase al entrar a un nivel.
    /// </summary>
    public void SincronizarConEscena(string nombreEscena)
    {
        multiplicadorFadeExterno = 1f;
        enCombateJefe = false;

        bool esMenu = EsEscenaDeMenu(nombreEscena);
        PistaMusica objetivo = esMenu ? PistaMusica.MenuEvitandoElFin : PistaMusica.CaminoEntornoBase;

        if (fuentePrincipal != null && fuentePrincipal.isPlaying && pista == objetivo && clip != null)
        {
            fuentePrincipal.loop = true;
            return;
        }

        CambiarPista(objetivo);
    }

    static bool EsEscenaDeMenu(string nombreEscena)
    {
        if (!string.IsNullOrEmpty(nombreEscena))
        {
            string n = nombreEscena.ToLowerInvariant();
            if (n.Contains("menu")) return true;
        }
        return Object.FindAnyObjectByType<MenuMexicano>() != null || Object.FindAnyObjectByType<MenuPrincipal>() != null;
    }

    // ================================================================== API ESTÁTICA PARA MENÚ, EXPLORACIÓN Y JEFES

    /// <summary>
    /// Reproduce la canción del menú (MenuEvitandoElFin) en loop.
    /// </summary>
    public static void ReproducirMenu()
    {
        ReproductorMusicaLoop inst = AsegurarInstancia();
        inst.enCombateJefe = false;
        inst.multiplicadorFadeExterno = 1f;
        if (inst.pista != PistaMusica.MenuEvitandoElFin || inst.fuentePrincipal == null || !inst.fuentePrincipal.isPlaying)
        {
            inst.CambiarPista(PistaMusica.MenuEvitandoElFin);
        }
    }

    /// <summary>
    /// Reproduce la canción base de exploración (CaminoEntornoBase) en loop cuando no se está peleando contra un jefe.
    /// </summary>
    public static void ReproducirExploracion()
    {
        ReproductorMusicaLoop inst = AsegurarInstancia();
        if (inst.enCombateJefe) return;
        inst.multiplicadorFadeExterno = 1f;
        if (inst.pista != PistaMusica.CaminoEntornoBase || inst.fuentePrincipal == null || !inst.fuentePrincipal.isPlaying)
        {
            inst.CambiarPista(PistaMusica.CaminoEntornoBase);
        }
    }

    /// <summary>
    /// Cambia la música a la canción del jefe correspondiente en loop durante todo el enfrentamiento.
    /// </summary>
    public static void IniciarCombateJefe(PistaMusica pistaJefe)
    {
        ReproductorMusicaLoop inst = AsegurarInstancia();
        inst.enCombateJefe = true;
        inst.multiplicadorFadeExterno = 1f;
        if (inst.pista != pistaJefe || inst.fuentePrincipal == null || !inst.fuentePrincipal.isPlaying)
        {
            inst.CambiarPista(pistaJefe);
        }
    }

    /// <summary>
    /// Determina la canción del jefe según el nivel actual e inicia la música de pelea en loop.
    /// </summary>
    public static void IniciarCombateJefeNivelActual()
    {
        IniciarCombateJefe(ObtenerPistaJefePorEscena());
    }

    /// <summary>
    /// Detiene la canción del jefe al ser derrotado y vuelve a reproducir CaminoEntornoBase en loop.
    /// </summary>
    public static void FinalizarCombateJefe()
    {
        ReproductorMusicaLoop inst = AsegurarInstancia();
        inst.enCombateJefe = false;
        inst.multiplicadorFadeExterno = 1f;
        inst.CambiarPista(PistaMusica.CaminoEntornoBase);
    }

    public static PistaMusica ObtenerPistaJefePorEscena()
    {
        string escena = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(escena))
        {
            string n = escena.ToLowerInvariant();
            if (n.Contains("nivel2") || n.Contains("panteon")) return PistaMusica.JefeNivel2;
            if (n.Contains("nivel3") || n.Contains("ciudad")) return PistaMusica.JefeNivel3Pelea;
            if (n.Contains("nivel1") || n.Contains("bosque")) return PistaMusica.JefeNivel1Pelea;
        }

        if (Object.FindAnyObjectByType<JefeGeneralMomia>() != null) return PistaMusica.JefeNivel2;
        if (Object.FindAnyObjectByType<JefeInquisidor>() != null) return PistaMusica.JefeNivel3Pelea;
        if (Object.FindAnyObjectByType<JefeLaBufa>() != null) return PistaMusica.JefeNivel1Pelea;

        return PistaMusica.JefeNivel1Pelea;
    }

    // ================================================================== REPRODUCCIÓN EN LOOP

    void OnValidate()
    {
        AudioSource src = GetComponent<AudioSource>();
        if (src != null)
        {
            src.loop = enLoop;
            src.spatialBlend = 0f;
            if (clip != null) src.clip = clip;
        }
    }

    void ConfigurarFuentes()
    {
        fuentePrincipal = GetComponent<AudioSource>();
        if (fuentePrincipal == null) fuentePrincipal = gameObject.AddComponent<AudioSource>();

        if (clip == null)
        {
            clip = ObtenerClip(pista);
        }

        if (clip != null && clip.loadState == AudioDataLoadState.Unloaded)
        {
            clip.LoadAudioData();
        }

        fuentePrincipal.clip = clip;
        fuentePrincipal.loop = enLoop;
        fuentePrincipal.playOnAwake = false;
        fuentePrincipal.spatialBlend = 0f;
        fuentePrincipal.priority = 0;
        fuentePrincipal.volume = CalcularVolumenObjetivo();
    }

    /// <summary>
    /// Inicia o reinicia la canción asegurándose de que quede en loop.
    /// </summary>
    public void Reproducir()
    {
        if (fuentePrincipal == null) ConfigurarFuentes();
        if (clip == null) clip = ObtenerClip(pista);
        if (clip == null)
        {
            Debug.LogWarning("[ReproductorMusicaLoop] No se encontró el clip para la pista: " + pista);
            return;
        }

        if (clip.loadState == AudioDataLoadState.Unloaded)
        {
            clip.LoadAudioData();
        }

        if (fuenteSecundaria != null)
        {
            fuenteSecundaria.Stop();
            usandoCrossfade = false;
        }

        enLoop = true;
        fuentePrincipal.Stop();
        fuentePrincipal.clip = clip;
        fuentePrincipal.time = 0f;
        fuentePrincipal.loop = enLoop && crossfadeLoopSegundos <= 0.01f;
        factorFadeEntrada = fadeEntradaSegundos > 0.01f ? 0f : 1f;
        fuentePrincipal.volume = CalcularVolumenObjetivo() * factorFadeEntrada;
        fuentePrincipal.Play();
    }

    /// <summary>
    /// Cambia a otra pista del catálogo y la reproduce en loop.
    /// </summary>
    public void CambiarPista(PistaMusica nuevaPista)
    {
        pista = nuevaPista;
        clip = ObtenerClip(nuevaPista);
        Reproducir();
    }

    public void Detener()
    {
        if (fuentePrincipal != null) fuentePrincipal.Stop();
        if (fuenteSecundaria != null) fuenteSecundaria.Stop();
        usandoCrossfade = false;
    }

    void Update()
    {
        if (fuentePrincipal == null || clip == null) return;

        if (factorFadeEntrada < 1f)
        {
            factorFadeEntrada = Mathf.MoveTowards(factorFadeEntrada, 1f, Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeEntradaSegundos));
        }

        float volBase = CalcularVolumenObjetivo() * factorFadeEntrada;

        // Modo 1: Loop exacto nativo por muestras (ideal para los loops exportados de REAPER)
        if (!enLoop || crossfadeLoopSegundos <= 0.01f)
        {
            if (fuentePrincipal.loop != enLoop) fuentePrincipal.loop = enLoop;
            fuentePrincipal.volume = volBase;
            if (enLoop && !fuentePrincipal.isPlaying && Application.isFocused)
            {
                fuentePrincipal.Play();
            }
            return;
        }

        // Modo 2: Loop con crossfade suave entre el final y el inicio de la pista
        ActualizarCrossfadeLoop(volBase);
    }

    void ActualizarCrossfadeLoop(float volBase)
    {
        if (fuenteSecundaria == null)
        {
            GameObject goSec = new GameObject("AudioLoop_Crossfade");
            goSec.transform.SetParent(transform, false);
            fuenteSecundaria = goSec.AddComponent<AudioSource>();
            fuenteSecundaria.clip = clip;
            fuenteSecundaria.loop = false;
            fuenteSecundaria.playOnAwake = false;
            fuenteSecundaria.spatialBlend = 0f;
            fuenteSecundaria.priority = 0;
            fuenteSecundaria.volume = 0f;
        }

        fuentePrincipal.loop = false;
        fuenteSecundaria.loop = false;

        float dur = clip.length;
        float cf = Mathf.Min(crossfadeLoopSegundos, dur * 0.45f);

        if (fuentePrincipal.isPlaying && dur - fuentePrincipal.time <= cf && !usandoCrossfade)
        {
            usandoCrossfade = true;
            fuenteSecundaria.clip = clip;
            fuenteSecundaria.time = 0f;
            fuenteSecundaria.volume = 0f;
            fuenteSecundaria.Play();
        }

        if (usandoCrossfade)
        {
            float tSec = fuenteSecundaria.isPlaying ? fuenteSecundaria.time : cf;
            float k = Mathf.Clamp01(tSec / Mathf.Max(0.01f, cf));
            fuentePrincipal.volume = volBase * (1f - k);
            fuenteSecundaria.volume = volBase * k;

            if (k >= 1f || !fuentePrincipal.isPlaying)
            {
                fuentePrincipal.Stop();
                AudioSource tmp = fuentePrincipal;
                fuentePrincipal = fuenteSecundaria;
                fuenteSecundaria = tmp;
                fuentePrincipal.volume = volBase;
                usandoCrossfade = false;
            }
        }
        else
        {
            fuentePrincipal.volume = volBase;
            if (!fuentePrincipal.isPlaying)
            {
                fuentePrincipal.time = 0f;
                fuentePrincipal.Play();
            }
        }
    }

    float CalcularVolumenObjetivo()
    {
        float mult = sincronizarConOpciones ? OpcionesJuego.Musica : 1f;
        return Mathf.Clamp01(volumenBase * mult * Mathf.Clamp01(multiplicadorFadeExterno));
    }

    /// <summary>
    /// Carga cualquiera de las 5 canciones guardadas en Assets/Game/Resources/Musica/.
    /// </summary>
    public static AudioClip ObtenerClip(PistaMusica pista)
    {
        return Resources.Load<AudioClip>(CarpetaResources + "/" + NombreArchivo(pista));
    }

    /// <summary>
    /// Carga una canción por su nombre de archivo (sin extensión) desde Assets/Game/Resources/Musica/.
    /// </summary>
    public static AudioClip ObtenerClip(string nombreArchivo)
    {
        return Resources.Load<AudioClip>(CarpetaResources + "/" + nombreArchivo);
    }

    public static string NombreArchivo(PistaMusica pista)
    {
        switch (pista)
        {
            case PistaMusica.MenuEvitandoElFin: return "MenuEvitandoElFin";
            case PistaMusica.CaminoEntornoBase: return "CaminoEntornoBase";
            case PistaMusica.JefeNivel1Pelea:   return "JefeNivel1Pelea";
            case PistaMusica.JefeNivel2:        return "JefeNivel2";
            case PistaMusica.JefeNivel3Pelea:   return "JefeNivel3Pelea";
            default:                            return "CaminoEntornoBase";
        }
    }
}
