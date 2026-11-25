using UnityEngine;

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
/// Reproductor de música preparado para funcionar en bucle continuo (loop) sin cortes.
/// Puede colocarse como Prefab/Componente en cualquier escena o invocarse por código cuando se desee implementar.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class ReproductorMusicaLoop : MonoBehaviour
{
    public const string CarpetaResources = "Musica";

    [Header("Canción")]
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

    [Tooltip("Segundos de fundido de entrada al comenzar a sonar (0 = instantáneo).")]
    [Range(0f, 3f)]
    public float fadeEntradaSegundos = 0.35f;

    private AudioSource fuentePrincipal;
    private AudioSource fuenteSecundaria;
    private float factorFadeEntrada = 1f;
    private bool usandoCrossfade;

    void Awake()
    {
        ConfigurarFuentes();
    }

    void Start()
    {
        if (reproducirAlIniciar)
        {
            Reproducir();
        }
    }

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
        fuentePrincipal.priority = 0; // Máxima prioridad para que ningún SFX corte la música
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

        fuentePrincipal.clip = clip;
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
    }

    void Update()
    {
        if (fuentePrincipal == null || clip == null) return;

        if (factorFadeEntrada < 1f)
        {
            factorFadeEntrada = Mathf.MoveTowards(factorFadeEntrada, 1f, Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeEntradaSegundos));
        }

        float volBase = CalcularVolumenObjetivo() * factorFadeEntrada;

        // Modo 1: Loop exacto nativo por muestras (ideal para loops exportados al compás desde REAPER)
        if (!enLoop || crossfadeLoopSegundos <= 0.01f)
        {
            if (fuentePrincipal.loop != enLoop) fuentePrincipal.loop = enLoop;
            fuentePrincipal.volume = volBase;
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
        return Mathf.Clamp01(volumenBase * mult);
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
