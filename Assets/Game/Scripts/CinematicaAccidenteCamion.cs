using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Cinemática de prólogo e introducción: Accidente en las Curvas de Guanajuato (3:12 AM).
/// Secuencia cinematográfica completa:
/// 1. Tensión nocturna / conducción del tráiler por la sierra en su carril derecho (+3m).
/// 2. Inclinación y peralte auténtico en curvas con rodaje de neumáticos y luces nocturnas.
/// 3. Falla de frenos, derrape centrífugo descontrolado e impacto violento contra la valla de contención.
/// 4. Valla destrozada, cámara lenta en el precipicio y caída aparatosa al fondo del valle.
/// 5. Llegada y rescate de la ambulancia con sirenas luminosas y acústicas.
/// 6. Paramedicos cargando la camilla con el chofer herido hacia las puertas traseras.
/// 7. Partida urgente de la ambulancia recorriendo las curvas de Guanajuato con cámara traveling de persecución.
/// 8. Desprendimiento del alma y transición a Nivel1_Limbo.
/// </summary>
public class CinematicaAccidenteCamion : MonoBehaviour
{
    [Header("Referencias de la Escena")]
    public Transform camionTransform;
    public GameObject choferJugador;
    public Camera camaraPrincipal;
    public CamaraTerceraPersona camaraJugadorScript;
    public GeneradorCurvasGuanajuato generadorCarretera;

    [Header("Ambulancia y Emergencias")]
    public GameObject ambulanciaObjeto;
    public Light sirenaAzul;
    public Light sirenaRoja;

    [Header("Cinemática y Ritmo")]
    public float velocidadAvance = 0.40f;
    [Tooltip("Punto de la carretera (en tramos de curva) donde el camión pierde el control y derrapa hacia el voladero")]
    public float puntoAccidente = 2.45f;

    [Tooltip("Multiplicador de la velocidad del camión durante la fase de conducción (menor = más tiempo para diálogo y planos).")]
    [Range(0.4f, 1.2f)] public float multiplicadorRitmoConduccion = 0.7f;

    [Header("Tormenta, personajes y detalles")]
    [Tooltip("Carretera mojada, charcos, relámpagos, gotas en el lente y look de cine. Se agrega solo si falta.")]
    public AmbienteLluviaCinematica ambienteLluvia;
    [Tooltip("Modelo del Chofer (el de Meshy). Se acuesta en la camilla. Si está vacío se usa una figura provisional.")]
    public GameObject prefabChoferHerido;
    [Tooltip("Posición y giro del Chofer sobre la camilla (ajústalos si ves que flota o se hunde).")]
    public Vector3 posChoferEnCamilla = new Vector3(0f, 0.2f, 0.9f);
    public Vector3 rotChoferEnCamilla = new Vector3(-90f, 0f, 0f);
    [Tooltip("Bengalas rojas que los paramédicos ponen en la carretera.")]
    public bool bengalasDeEmergencia = true;

    [Header("Atmósfera nocturna")]
    [Tooltip("Aplica en runtime cielo, luna, niebla y ambiente de noche (aunque la escena esté configurada de día).")]
    public bool forzarNoche = true;
    [Tooltip("Skybox nocturno (por ejemplo AllSkyFree/Night MoonBurst). Si está vacío se usa un fondo azul oscuro.")]
    public Material materialCieloNoche;

    [Header("Siguiente Escena")]
    public string escenaNivel1 = "Nivel1_Bosque";

    [Header("Producción")]
    [Tooltip("Permite saltar la cinemática manteniendo ESPACIO, ENTER o ESC (requisito del IntroState).")]
    public bool permitirSaltar = true;
    [Tooltip("Segundos que hay que mantener la tecla para saltar.")]
    public float tiempoParaSaltar = 0.9f;
    [Tooltip("Lluvia fina de noche alrededor de la cámara (atmósfera).")]
    public bool lluviaNocturna = true;
    public string tituloJuego = "EVITANDO EL FIN";
    public string subtituloPrologo = "PRÓLOGO";

    [Header("Guion (editable)")]
    [TextArea] public string textoLugar = "<size=120%><b>SIERRA DE GUANAJUATO</b></size>\n<color=#BBBBBB>Carretera a Dolores Hidalgo  ·  03:12 AM</color>";
    [TextArea] public string textoRadio = "<color=#8FD3FF><b>RADIO</b></color>  Unidad 7, ¿cómo va la sierra? Ya se te hizo tarde.\n<color=#FFD98F><b>CHOFER</b></color>  Todo en orden, jefe. Ya casi llego a Dolores.";
    [TextArea] public string textoFrenos = "<color=#FFD98F><b>CHOFER</b></color>\n<color=#FF4444><b>¡Los frenos no responden!</b></color>";
    [TextArea] public string textoMinutosDespues = "<size=90%><color=#BBBBBB>MINUTOS DESPUÉS</color></size>";
    [TextArea] public string textoParamedico1 = "<color=#9FFFC8><b>PARAMÉDICO</b></color>  ¡Aquí está el chofer! Pulso muy débil, a la camilla, rápido.";
    [TextArea] public string textoParamedico2 = "<color=#9FFFC8><b>PARAMÉDICO</b></color>  <color=#FF5555><b>¡Código rojo!</b></color> A urgencias, no tenemos mucho tiempo.";
    [TextArea] public string textoCuerpo = "<size=115%>Tu cuerpo va camino a urgencias…</size>";
    [TextArea] public string textoAlmaDespierta = "<size=130%><color=#66FFE0><b>Pero tú sigues aquí.</b></color></size>";

    // Overlay Cinemático (UI)
    private CanvasGroup fadeCanvas;
    private CanvasGroup flashCanvas;
    private CanvasGroup flashAlma;
    private RectTransform barraSup;
    private RectTransform barraInf;
    private TextMeshProUGUI textoSubtitulos;
    private CanvasGroup fondoSubtitulos;
    private TextMeshProUGUI textoTitulo;
    private TextMeshProUGUI textoSubTitulo;
    private TextMeshProUGUI textoSaltar;
    private RectTransform barraSaltar;
    private Image orbeAlma;
    private Coroutine rutinaSub;
    private Coroutine rutinaCola;
    private readonly Queue<KeyValuePair<string, float>> colaSubs = new Queue<KeyValuePair<string, float>>();
    private float progresoSalto;
    private bool saltando;
    private AsyncOperation cargaPendiente;

    // Audio Procedural Integrado
    private AudioSource audioSourceMotor;
    private AudioSource audioSourceFx;
    private AudioSource audioSourceSirena;
    private AudioSource audioSourceAmbiente;
    private AudioSource audioSourceMusica;
    private AudioSource audioSourceLatido;

    // Efectos de partículas
    private ParticleSystem lluvia;
    private ParticleSystem chispasImpacto;
    private ParticleSystem polvoImpacto;
    private ParticleSystem humoCamion;

    // Intermitentes de emergencia del camión siniestrado
    private bool intermitentesActivos;
    private readonly List<Light> lucesIntermitentes = new List<Light>();

    // Luces y Calaveras
    private Light luzBrakingCamion;
    private Renderer[] calaverasCamion;

    // Ruedas animadas
    private List<Transform> ruedasCamion = new List<Transform>();
    private List<Transform> ruedasDelanterasCamion = new List<Transform>();
    private List<Transform> ruedasAmbulancia = new List<Transform>();

    // Estado interno
    private float shakeCamara;
    private bool sirenasActivas;
    private float tiempoSirena;
    private Vector3 rutaInicioPos;
    private Vector3 rutaInicioFwd;
    private float rollCamara, rollObjetivo;        // inclinación de la cámara (plano holandés) en el derrape
    private bool corteBajoRescate;                 // ya se hizo el corte al plano bajo (reflejos en el asfalto)

    // Respaldo de puntos de sierra en caso de que falte GeneradorCurvasGuanajuato
    private readonly List<Vector3> puntosFallback = new List<Vector3>()
    {
        new Vector3(0, 0, -15),
        new Vector3(0, 0, 10),
        new Vector3(10, 1.2f, 38),
        new Vector3(22, 2.5f, 70),
        new Vector3(5, 4.0f, 105),
        new Vector3(-20, 5.5f, 140),
        new Vector3(-12, 6.5f, 180),
        new Vector3(15, 7.8f, 220),
        new Vector3(25, 7.0f, 260),
        new Vector3(8, 5.5f, 300),
        new Vector3(-5, 4.0f, 340),
        new Vector3(0, 3.5f, 380)
    };

    void Awake()
    {
        if (generadorCarretera == null) generadorCarretera = Object.FindAnyObjectByType<GeneradorCurvasGuanajuato>();
        if (camaraPrincipal == null) camaraPrincipal = Camera.main;
        if (camaraPrincipal != null && camaraJugadorScript == null) camaraJugadorScript = camaraPrincipal.GetComponent<CamaraTerceraPersona>();

        DesactivarUIResidual();
        ConstruirOverlay();
        ConfigurarAudioProcedural();
        ClasificarRuedasYLuces();
    }

    void Start()
    {
        var gm = Object.FindAnyObjectByType<GameManager>();
        if (gm != null) gm.enabled = false;

        if (camionTransform != null)
        {
            foreach (var c in camionTransform.GetComponentsInChildren<Collider>()) c.enabled = false;
        }

        if (ambulanciaObjeto != null)
        {
            foreach (var c in ambulanciaObjeto.GetComponentsInChildren<Collider>()) c.enabled = false;
        }

        if (camaraPrincipal != null)
        {
            camaraPrincipal.nearClipPlane = 0.1f;
            camaraPrincipal.farClipPlane = Mathf.Max(camaraPrincipal.farClipPlane, 1200f);
        }

        escenaNivel1 = ResolverEscenaNivel1();
        if (forzarNoche) AplicarAtmosferaNocturna();

        CrearEfectosParticulas();

        // Noche de tormenta: asfalto mojado, salpicaduras, relámpagos, gotas en el lente y look de cine
        if (lluviaNocturna)
        {
            if (ambienteLluvia == null) ambienteLluvia = GetComponent<AmbienteLluviaCinematica>();
            if (ambienteLluvia == null) ambienteLluvia = gameObject.AddComponent<AmbienteLluviaCinematica>();
            ambienteLluvia.Preparar(camaraPrincipal, camionTransform, ambulanciaObjeto != null ? ambulanciaObjeto.transform : null);
        }

        StartCoroutine(SecuenciaCinematicaCompleta());
    }

    void Update()
    {
        if (sirenasActivas)
        {
            tiempoSirena += Time.deltaTime * 14f;
            bool azulOn = Mathf.Sin(tiempoSirena) > 0f;
            if (sirenaAzul != null) sirenaAzul.enabled = azulOn;
            if (sirenaRoja != null) sirenaRoja.enabled = !azulOn;
        }

        if (intermitentesActivos)
        {
            bool on = Mathf.Repeat(Time.unscaledTime, 0.9f) < 0.45f;
            foreach (Light l in lucesIntermitentes) if (l != null) l.enabled = on;
        }

        ActualizarSaltar();
    }

    void LateUpdate()
    {
        // La lluvia acompaña a la cámara (caída por delante del lente)
        if (lluvia != null && camaraPrincipal != null)
        {
            Vector3 fwdPlano = camaraPrincipal.transform.forward;
            fwdPlano.y = 0f;
            if (fwdPlano.sqrMagnitude < 0.01f) fwdPlano = Vector3.forward;
            lluvia.transform.position = camaraPrincipal.transform.position + Vector3.up * 11f + fwdPlano.normalized * 9f;
        }
    }

    void OnDestroy()
    {
        // Nunca dejar el juego en cámara lenta si la escena se descarga a mitad de la cinemática
        Time.timeScale = 1f;
    }

    // ------------------------------------------------------------ ATMÓSFERA Y ESCENA SIGUIENTE

    /// <summary>El Nivel 1 es el bosque. Si la escena guardada aún apunta al antiguo Limbo, se usa Nivel1_Bosque cuando exista.</summary>
    string ResolverEscenaNivel1()
    {
        if (Application.CanStreamedLevelBeLoaded("Nivel1_Bosque") &&
            (string.IsNullOrEmpty(escenaNivel1) || escenaNivel1 == "Nivel1_Limbo"))
            return "Nivel1_Bosque";
        return escenaNivel1;
    }

    void AplicarAtmosferaNocturna()
    {
        Color niebla = new Color(0.035f, 0.055f, 0.10f);

        if (materialCieloNoche != null)
        {
            RenderSettings.skybox = materialCieloNoche;
            if (camaraPrincipal != null) camaraPrincipal.clearFlags = CameraClearFlags.Skybox;
        }
        else if (camaraPrincipal != null)
        {
            camaraPrincipal.clearFlags = CameraClearFlags.SolidColor;
            camaraPrincipal.backgroundColor = new Color(0.02f, 0.03f, 0.07f);
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.045f, 0.06f, 0.11f);
        RenderSettings.reflectionIntensity = 0.25f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0045f;
        RenderSettings.fogColor = niebla;

        // La luz direccional pasa a ser luz de luna fría y tenue
        foreach (Light l in Object.FindObjectsByType<Light>())
        {
            if (l.type != LightType.Directional) continue;
            l.color = new Color(0.45f, 0.58f, 0.95f);
            l.intensity = 0.35f;
            l.transform.rotation = Quaternion.Euler(38f, -32f, 0f);
            l.shadows = LightShadows.Soft;
        }
        DynamicGI.UpdateEnvironment();
    }

    // ------------------------------------------------------------ SALTAR CINEMÁTICA

    void ActualizarSaltar()
    {
        if (!permitirSaltar || saltando) return;

        bool presionando = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.Return)
                        || Input.GetKey(KeyCode.KeypadEnter) || Input.GetKey(KeyCode.Escape);

        progresoSalto = presionando
            ? progresoSalto + Time.unscaledDeltaTime / Mathf.Max(0.1f, tiempoParaSaltar)
            : Mathf.Max(0f, progresoSalto - Time.unscaledDeltaTime * 2.5f);

        if (barraSaltar != null) barraSaltar.sizeDelta = new Vector2(260f * Mathf.Clamp01(progresoSalto), 3f);
        if (textoSaltar != null)
        {
            Color c = textoSaltar.color;
            c.a = Mathf.Lerp(c.a, presionando ? 1f : 0.55f, Time.unscaledDeltaTime * 8f);
            textoSaltar.color = c;
        }

        if (progresoSalto >= 1f) SaltarCinematica();
    }

    public void SaltarCinematica()
    {
        if (saltando) return;
        saltando = true;

        StopAllCoroutines();
        rutinaSub = null;
        rutinaCola = null;
        colaSubs.Clear();

        Time.timeScale = 1f;
        shakeCamara = 0f;
        sirenasActivas = false;
        if (sirenaAzul != null) sirenaAzul.enabled = false;
        if (sirenaRoja != null) sirenaRoja.enabled = false;

        foreach (var a in new[] { audioSourceMotor, audioSourceSirena, audioSourceFx, audioSourceLatido })
            if (a != null) a.Stop();

        StartCoroutine(RutinaSalto());
    }

    IEnumerator RutinaSalto()
    {
        AplicarAlphaSub(0f);
        if (textoTitulo != null) textoTitulo.alpha = 0f;
        if (textoSubTitulo != null) textoSubTitulo.alpha = 0f;

        // Fundido a negro mientras el ambiente se apaga
        float vol = audioSourceAmbiente != null ? audioSourceAmbiente.volume : 0f;
        float volMus = audioSourceMusica != null ? audioSourceMusica.volume : 0f;
        float t = 0f;
        const float dur = 0.7f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            fadeCanvas.alpha = k;
            if (audioSourceAmbiente != null) audioSourceAmbiente.volume = Mathf.Lerp(vol, 0f, k);
            if (audioSourceMusica != null) audioSourceMusica.volume = Mathf.Lerp(volMus, 0f, k);
            yield return null;
        }
        fadeCanvas.alpha = 1f;

        CargarNivel1();
    }

    void CargarNivel1()
    {
        Time.timeScale = 1f;

        // Si ya se estaba precargando, solo hay que activarla (un LoadScene síncrono quedaría bloqueado)
        if (cargaPendiente != null)
        {
            cargaPendiente.allowSceneActivation = true;
            return;
        }

        if (Application.CanStreamedLevelBeLoaded(escenaNivel1))
        {
            SceneManager.LoadScene(escenaNivel1);
        }
        else
        {
            Debug.LogError("[Cinemática] No se encontró la escena '" + escenaNivel1 + "' en Build Settings.");
        }
    }

    private void DesactivarUIResidual()
    {
        foreach (var c in Object.FindObjectsByType<Canvas>())
        {
            if (c.gameObject.name.Contains("Gameplay") || c.gameObject.name.Contains("Pausa") || c.gameObject.name.Contains("HUD"))
            {
                c.gameObject.SetActive(false);
            }
        }
        var textoViejo = GameObject.Find("TextoNarrativo");
        if (textoViejo != null) textoViejo.SetActive(false);
        var panelViejo = GameObject.Find("PanelFadeNegro");
        if (panelViejo != null) panelViejo.SetActive(false);
    }

    private void ClasificarRuedasYLuces()
    {
        ruedasCamion.Clear();
        ruedasDelanterasCamion.Clear();
        ruedasAmbulancia.Clear();

        if (camionTransform != null)
        {
            foreach (Transform t in camionTransform.GetComponentsInChildren<Transform>())
            {
                if (t.name.StartsWith("Rueda_") || t.name.Contains("Rueda"))
                {
                    ruedasCamion.Add(t);
                    if (t.localPosition.z > 1.5f) ruedasDelanterasCamion.Add(t);
                }
            }

            // Intermitentes de emergencia (cuatro esquinas, apagados hasta que el camión queda detenido)
            lucesIntermitentes.Clear();
            Vector3[] esquinas = { new Vector3(-1.3f, 1.0f, 4.6f), new Vector3(1.3f, 1.0f, 4.6f), new Vector3(-1.3f, 0.9f, -5.6f), new Vector3(1.3f, 0.9f, -5.6f) };
            for (int i = 0; i < esquinas.Length; i++)
            {
                GameObject gi = new GameObject("Intermitente_" + i);
                gi.transform.SetParent(camionTransform, false);
                gi.transform.localPosition = esquinas[i];
                Light li = gi.AddComponent<Light>();
                li.type = LightType.Point;
                li.color = new Color(1f, 0.55f, 0.08f);
                li.range = 7f;
                li.intensity = 6f;
                li.shadows = LightShadows.None;
                li.enabled = false;
                lucesIntermitentes.Add(li);
            }

            // Luz de frenos adicional en la parte trasera
            GameObject goBrake = new GameObject("LuzFrenoEmergencia");
            goBrake.transform.SetParent(camionTransform, false);
            goBrake.transform.localPosition = new Vector3(0f, 0.9f, -5.7f);
            luzBrakingCamion = goBrake.AddComponent<Light>();
            luzBrakingCamion.type = LightType.Point;
            luzBrakingCamion.color = new Color(1f, 0.05f, 0.05f);
            luzBrakingCamion.range = 14f;
            luzBrakingCamion.intensity = 0f;

            List<Renderer> rendCalaveras = new List<Renderer>();
            foreach (var r in camionTransform.GetComponentsInChildren<Renderer>())
            {
                if (r.name.Contains("Calavera")) rendCalaveras.Add(r);
            }
            calaverasCamion = rendCalaveras.ToArray();
        }

        if (ambulanciaObjeto != null)
        {
            foreach (Transform t in ambulanciaObjeto.GetComponentsInChildren<Transform>())
            {
                if (t.name.StartsWith("Rueda_") || t.name.Contains("Rueda"))
                {
                    ruedasAmbulancia.Add(t);
                }
            }
        }
    }

    // ============================================================ SECUENCIA PRINCIPAL

    IEnumerator SecuenciaCinematicaCompleta()
    {
        if (choferJugador != null) choferJugador.SetActive(false);
        if (camaraJugadorScript != null) camaraJugadorScript.enabled = false;
        if (ambulanciaObjeto != null) ambulanciaObjeto.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Time.timeScale = 1f;

        fadeCanvas.alpha = 1f;

        // Arrancar motor diésel procedural suavemente en segundo plano
        if (audioSourceMotor != null)
        {
            audioSourceMotor.volume = 0f;
            audioSourceMotor.Play();
        }

        // Ambiente nocturno (viento, grillos, lluvia fina) y tensión musical desde el negro
        IniciarAmbiente();
        yield return new WaitForSeconds(0.6f);

        // Tarjeta de título sobre negro
        yield return StartCoroutine(TarjetaTitulo(tituloJuego, subtituloPrologo));
        yield return new WaitForSeconds(0.5f);

        // Entrada panorámica con barras negras (formato cine)
        StartCoroutine(AnimarBarras(110f, 1.2f));
        MostrarSub(textoLugar, 1.6f);
        MostrarSub(textoRadio, 1.3f);

        // Desvanecimiento suave desde negro
        yield return Fade(fadeCanvas, 1f, 0f, 1.8f);

        // 1. Fase de Conducción fluida en el carril derecho con planos dinámicos
        yield return StartCoroutine(FaseConduccion());

        // 2. Falla mecánica, derrape en la curva peligrosa, choque de valla y caída
        yield return StartCoroutine(FaseAccidente());

        // 3. Rescate: ambulancia, paramédicos con camilla y partida a toda velocidad
        yield return StartCoroutine(FaseRescate());

        // 4. Separación del alma y transición al Nivel 1
        yield return StartCoroutine(FaseDesprendimientoAlma());
    }

    // ------------------------------------------------------------ FASE 1: CONDUCCIÓN

    IEnumerator FaseConduccion()
    {
        float s = 0f;
        float limiteFrenos = puntoAccidente - 0.35f; // Comienzo de la falla mecánica antes de la curva
        float corte1 = 0.95f;
        float corte2 = 1.65f;
        bool avisoFrenosMostrado = false;
        Vector3 camOverlook = Vector3.zero;
        bool overlookPosicionada = false;

        float carrilDerecho = 3.0f; // Centro del carril derecho (carretera de 12m)

        while (s < limiteFrenos)
        {
            float dt = Time.deltaTime;
            float aceleracion = Mathf.Clamp01(s / 0.7f);
            float vel = Mathf.Lerp(0.24f, velocidadAvance, aceleracion) * multiplicadorRitmoConduccion;
            s += dt * vel;

            // Evaluar carretera con peralte real y orientación perfecta
            Vector3 posPista, fwdPista, upPista, rightPista;
            Quaternion rotPista;
            EvaluarRuta(s, carrilDerecho, out posPista, out rotPista, out fwdPista, out upPista, out rightPista);

            // Vibración sutil de motor pesado diésel
            float vibracion = Mathf.Sin(Time.time * 38f) * 0.012f;
            if (camionTransform != null)
            {
                camionTransform.position = posPista + upPista * (0.08f + vibracion);
                camionTransform.rotation = rotPista;
            }

            // Rodar ruedas según velocidad
            RotarRuedas(ruedasCamion, camionTransform, dt * vel * 520f);

            // Volumen del motor
            if (audioSourceMotor != null)
            {
                audioSourceMotor.volume = Mathf.Lerp(audioSourceMotor.volume, 0.45f, dt * 2f);
                audioSourceMotor.pitch = Mathf.Lerp(0.85f, 1.15f, aceleracion);
            }

            // La tensión musical crece conforme se acerca la curva fatal
            if (audioSourceMusica != null)
            {
                float objetivoMusica = Mathf.Lerp(0.10f, 0.42f, Mathf.Clamp01(s / limiteFrenos));
                audioSourceMusica.volume = Mathf.Lerp(audioSourceMusica.volume, objetivoMusica, dt * 1.5f);
                audioSourceMusica.pitch = Mathf.Lerp(1f, 1.12f, Mathf.Clamp01(s / limiteFrenos));
            }

            // COREOGRAFÍA CINEMATOGRÁFICA DE CÁMARAS
            if (s < corte1)
            {
                // PLANO 1: Travelling dinámico de persecución baja en ángulo tres cuartos
                Vector3 camDeseada = posPista - fwdPista * 12.0f + upPista * 3.4f - rightPista * 1.8f;
                PosicionarCamara(camDeseada, posPista + upPista * 1.5f + fwdPista * 1.5f, 5.5f);
                if (camaraPrincipal != null) camaraPrincipal.fieldOfView = Mathf.Lerp(camaraPrincipal.fieldOfView, 60f, dt * 2f);
            }
            else if (s >= corte1 && s < corte2)
            {
                // PLANO 2: Mirador panorámico desde un saliente de la montaña mientras el camión pasa
                if (!overlookPosicionada)
                {
                    Vector3 pMirador, fMirador, uMirador, rMirador;
                    Quaternion rotM;
                    EvaluarRuta(s + 0.35f, -5.5f, out pMirador, out rotM, out fMirador, out uMirador, out rMirador);
                    camOverlook = pMirador + Vector3.up * 3.8f;
                    overlookPosicionada = true;
                    if (camaraPrincipal != null) camaraPrincipal.transform.position = camOverlook;
                }
                PosicionarCamara(camOverlook, posPista + upPista * 1.6f, 60f);
                if (camaraPrincipal != null) camaraPrincipal.fieldOfView = Mathf.Lerp(camaraPrincipal.fieldOfView, 54f, dt * 2f);
            }
            else
            {
                // PLANO 3: Tensión extrema en plano corto sobre la cabina y cofre
                Vector3 camTension = posPista - fwdPista * 8.5f + upPista * 2.6f + rightPista * 1.4f;
                PosicionarCamara(camTension, posPista + upPista * 1.4f + fwdPista * 2.0f, 7.5f);
            }

            // AVISO DE FALLA MECÁNICA
            if (s >= 1.70f && !avisoFrenosMostrado)
            {
                avisoFrenosMostrado = true;
                MostrarSubUrgente(textoFrenos, 1.6f);
                EncenderLucesFreno(true);
                ReproducirAudioProcedural(TipoFx.FrenosChillando, 0.6f);
            }

            if (s >= 1.70f)
            {
                shakeCamara = Mathf.Lerp(shakeCamara, 0.25f, dt * 4f);
                if (camaraPrincipal != null)
                    camaraPrincipal.fieldOfView = Mathf.Lerp(camaraPrincipal.fieldOfView, 68f, dt * 3f);
            }

            yield return null;
        }
    }

    // ------------------------------------------------------------ FASE 2: ACCIDENTE Y CAÍDA

    IEnumerator FaseAccidente()
    {
        float sDerrape = puntoAccidente - 0.35f;
        float lateralOffset = 3.0f; // Inicia en el carril derecho
        float anguloDerrape = 0f;
        float tDerrape = 0f;

        Vector3 ultimoPunto = camionTransform.position;
        Vector3 ultimoFwd = camionTransform.forward;
        Vector3 ultimoUp = camionTransform.up;
        Vector3 ultimoRight = camionTransform.right;

        // Sonido de derrape de neumáticos sobre asfalto
        ReproducirAudioProcedural(TipoFx.DerrapeLlantas, 0.7f);
        if (ambienteLluvia != null)
        {
            ambienteLluvia.Derrape(true);      // marcas negras de las llantas sobre el asfalto mojado
            ambienteLluvia.Accion(1f);         // aberración cromática y desenfoque de movimiento
            ambienteLluvia.GotasExtra(6);
        }
        rollObjetivo = -9f;                    // plano holandés: la cámara se inclina con el derrape

        // Deslizamiento centrífugo hacia la barrera de contención (offset lateral > 8.2m)
        while (lateralOffset < 8.35f)
        {
            float dt = Time.deltaTime;
            tDerrape += dt;
            sDerrape += dt * 0.44f;
            lateralOffset += (3.2f + tDerrape * 7.5f) * dt;
            anguloDerrape += (35f + tDerrape * 65f) * dt;
            shakeCamara = Mathf.Min(0.65f, 0.2f + tDerrape * 0.5f);

            Vector3 posPista, fwdPista, upPista, rightPista;
            Quaternion rotPista;
            EvaluarRuta(sDerrape, lateralOffset, out posPista, out rotPista, out fwdPista, out upPista, out rightPista);

            ultimoPunto = posPista;
            ultimoFwd = fwdPista;
            ultimoUp = upPista;
            ultimoRight = rightPista;

            // El tráiler derrapa cruzado de costado
            Quaternion rotDerrape = rotPista * Quaternion.Euler(0f, anguloDerrape, Mathf.Min(tDerrape * 26f, 22f));

            if (camionTransform != null)
            {
                camionTransform.position = posPista + upPista * 0.10f;
                camionTransform.rotation = rotDerrape;
            }

            RotarRuedas(ruedasCamion, camionTransform, dt * 380f);

            // Cámara dramática lateral siguiendo el derrape descontrolado
            Vector3 posCamDerrape = posPista - fwdPista * 11.0f + upPista * 3.5f - rightPista * 3.8f;
            PosicionarCamara(posCamDerrape, camionTransform.position + upPista * 1.4f, 9.5f);

            if (camaraPrincipal != null)
                camaraPrincipal.fieldOfView = Mathf.Lerp(camaraPrincipal.fieldOfView, 75f, dt * 4f);

            yield return null;
        }

        // ==================== IMPACTO CONTRA LA VALLA METÁLICA (SLOW MOTION)
        Vector3 puntoImpacto = camionTransform.position;
        StartCoroutine(Flash(1f, 0.7f));
        shakeCamara = 1.2f;
        rollObjetivo = 7f;
        if (ambienteLluvia != null)
        {
            ambienteLluvia.Derrape(false);
            ambienteLluvia.GotasExtra(16);
            ambienteLluvia.RelampagoAhora(0.35f);   // un rayo ilumina el choque
            ambienteLluvia.Accion(0.7f);
        }

        // Sonido de impacto violento
        ReproducirAudioProcedural(TipoFx.ChoqueMetalValla, 1.0f);
        if (audioSourceMotor != null) audioSourceMotor.Stop();
        EmitirChispas(puntoImpacto + Vector3.up * 0.6f, ultimoRight);
        EmitirPolvo(puntoImpacto);
        if (audioSourceMusica != null) audioSourceMusica.volume = 0f; // el impacto corta la música en seco

        // Abre un hueco real en la defensa metálica y expulsa fragmentos
        RomperDefensa(puntoImpacto, 7.5f);
        LanzarFragmentosValla(puntoImpacto, ultimoFwd, ultimoRight);
        CrearVallaRota(puntoImpacto, ultimoFwd, ultimoRight);

        // Cámara en el borde de la carretera, observando cómo el camión sale del camino
        Vector3 camBorde = puntoImpacto - ultimoRight * 7.5f - ultimoFwd * 5.0f + Vector3.up * 3.5f;
        if (camaraPrincipal != null) camaraPrincipal.transform.position = camBorde;

        Time.timeScale = 0.25f; // Cámara lenta cinematográfica

        // ==================== EL CAMIÓN ATRAVIESA LA VALLA Y SE DESLIZA POR LA LADERA (SALE "UN POCO" DE LA CARRETERA)
        Vector3 vel = ultimoFwd * 9.5f + ultimoRight * 6.0f;
        float yaw = anguloDerrape;
        float roll = Mathf.Min(tDerrape * 26f, 22f);
        const float rollFinal = 26f;           // queda ladeado sobre la pendiente
        Vector3 pos = camionTransform.position;
        Vector3 normalSuave = ultimoUp;
        Vector3 fwdPlano = Vector3.ProjectOnPlane(ultimoFwd, Vector3.up).normalized;
        if (fwdPlano.sqrMagnitude < 0.01f) fwdPlano = Vector3.forward;
        float tDesliz = 0f;
        bool detenido = false;

        while (!detenido && tDesliz < 5.5f)
        {
            float dt = Time.deltaTime;
            tDesliz += dt;

            if (tDesliz > 0.2f) Time.timeScale = Mathf.MoveTowards(Time.timeScale, 1f, Time.unscaledDeltaTime * 0.8f);
            shakeCamara = Mathf.MoveTowards(shakeCamara, 0.12f, Time.unscaledDeltaTime * 0.6f);

            // Rozamiento creciente: se frena sobre el pasto
            float rozamiento = Mathf.Lerp(0.8f, 2.4f, Mathf.Clamp01(tDesliz / 2.5f));
            vel *= Mathf.Exp(-rozamiento * dt);
            pos += vel * dt;

            // Se apoya sobre el terreno de la ladera
            Vector3 suelo, normal;
            if (BuscarSuelo(pos + Vector3.up * 8f, 40f, out suelo, out normal))
            {
                pos.y = Mathf.Lerp(pos.y, suelo.y + 0.12f, 1f - Mathf.Exp(-14f * dt));
                normalSuave = Vector3.Slerp(normalSuave, normal, 1f - Mathf.Exp(-6f * dt));
            }
            else
            {
                pos.y -= 9f * dt;
            }

            // Sigue girando mientras pierde velocidad y termina ladeado
            yaw += Mathf.Lerp(30f, 4f, Mathf.Clamp01(tDesliz / 3f)) * dt * Mathf.Clamp01(vel.magnitude / 4f);
            roll = Mathf.MoveTowards(roll, rollFinal, 14f * dt);

            Quaternion rotBase = Quaternion.FromToRotation(Vector3.up, normalSuave) * Quaternion.LookRotation(fwdPlano, Vector3.up);
            camionTransform.SetPositionAndRotation(pos, rotBase * Quaternion.Euler(0f, yaw, roll));
            RotarRuedas(ruedasCamion, camionTransform, vel.magnitude * dt * 40f);

            PosicionarCamara(camBorde, pos + Vector3.up * 1.2f, 40f);

            if (tDesliz > 0.9f && vel.magnitude < 0.35f) detenido = true;
            yield return null;
        }

        // ==================== REPOSO: el camión queda ladeado fuera de la carretera, con humo
        Time.timeScale = 1f;
        StartCoroutine(Flash(0.6f, 0.7f));
        shakeCamara = 0.6f;
        rollObjetivo = 0f;
        if (ambienteLluvia != null) ambienteLluvia.Accion(0f);
        ReproducirAudioProcedural(TipoFx.GolpeFondoCanon, 0.8f);
        EncenderLucesFreno(false);
        EmitirPolvo(camionTransform.position);
        IniciarHumoCamion();
        intermitentesActivos = true;

        // Zumbido de oídos: el mundo se apaga (mismo zumbido con el que arranca el Nivel 1)
        ReproducirAudioProcedural(TipoFx.ZumbidoOidos, 0.5f);
        if (audioSourceAmbiente != null) audioSourceAmbiente.volume = 0.04f;

        // Plano lento de acercamiento al camión siniestrado
        Vector3 posReposo = camionTransform.position;
        Vector3 camInicio = camaraPrincipal != null ? camaraPrincipal.transform.position : camBorde;
        Vector3 haciaCamara = (camInicio - posReposo); haciaCamara.y = 0f;
        if (haciaCamara.sqrMagnitude < 0.01f) haciaCamara = -ultimoRight;
        Vector3 camFinal = posReposo + haciaCamara.normalized * 10f + Vector3.up * 2.4f;
        float tRep = 0f;
        const float durRep = 2.6f;
        while (tRep < durRep)
        {
            tRep += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, tRep / durRep);
            shakeCamara = Mathf.MoveTowards(shakeCamara, 0.03f, Time.deltaTime * 0.5f);
            PosicionarCamara(Vector3.Lerp(camInicio, camFinal, k), posReposo + Vector3.up * 1.6f, 8f);
            if (camaraPrincipal != null) camaraPrincipal.fieldOfView = Mathf.Lerp(camaraPrincipal.fieldOfView, 52f, Time.deltaTime * 2f);
            yield return null;
        }
        shakeCamara = 0f;

        // Desvanecimiento suave a negro
        yield return Fade(fadeCanvas, 0f, 1f, 1.0f);
    }

    // ------------------------------------------------------------ AYUDAS DE ESCENARIO (suelo, valla, humo)

    /// <summary>Busca el suelo bajo un punto, ignorando valla, fragmentos y objetos con Rigidbody.</summary>
    bool BuscarSuelo(Vector3 desde, float distancia, out Vector3 punto, out Vector3 normal)
    {
        punto = desde; normal = Vector3.up;
        RaycastHit[] hits = Physics.RaycastAll(desde, Vector3.down, distancia, ~0, QueryTriggerInteraction.Ignore);
        float mejorY = float.MinValue;
        bool ok = false;
        foreach (RaycastHit h in hits)
        {
            Collider c = h.collider;
            if (c.attachedRigidbody != null) continue;
            string n = c.name;
            if (n.Contains("Defensa") || n.Contains("Poste") || n.Contains("Fragmento") || n.Contains("Valla")) continue;
            if (camionTransform != null && c.transform.IsChildOf(camionTransform)) continue;
            if (ambulanciaObjeto != null && c.transform.IsChildOf(ambulanciaObjeto.transform)) continue;
            if (h.point.y > mejorY) { mejorY = h.point.y; punto = h.point; normal = h.normal; ok = true; }
        }
        return ok;
    }

    /// <summary>Proyecta un punto al suelo (terreno o asfalto) para que los personajes no floten ni se hundan.</summary>
    Vector3 AlSuelo(Vector3 p)
    {
        Vector3 suelo, normal;
        if (BuscarSuelo(p + Vector3.up * 6f, 24f, out suelo, out normal)) return suelo;
        return p;
    }

    /// <summary>Elimina de la malla de la defensa metálica el tramo del impacto (queda un hueco real por donde sale el camión).</summary>
    void RomperDefensa(Vector3 centroMundo, float radio)
    {
        if (generadorCarretera == null) return;
        Transform defensa = generadorCarretera.transform.Find("Barreras_Contencion_Voladero/Defensa_Metalica_Canon");
        if (defensa == null) return;

        MeshFilter mf = defensa.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null || !mf.sharedMesh.isReadable) return;

        Mesh original = mf.sharedMesh;
        Vector3[] v = original.vertices;
        int[] tris = original.triangles;
        List<int> conservados = new List<int>(tris.Length);
        for (int i = 0; i < tris.Length; i += 3)
        {
            Vector3 c = (v[tris[i]] + v[tris[i + 1]] + v[tris[i + 2]]) / 3f;
            if (Vector3.Distance(defensa.TransformPoint(c), centroMundo) > radio)
            {
                conservados.Add(tris[i]); conservados.Add(tris[i + 1]); conservados.Add(tris[i + 2]);
            }
        }

        Mesh rota = new Mesh();
        rota.name = original.name + "_Rota";
        rota.vertices = v;
        rota.uv = original.uv;
        rota.triangles = conservados.ToArray();
        rota.RecalculateNormals();
        rota.RecalculateBounds();
        mf.sharedMesh = rota;

        MeshCollider mc = defensa.GetComponent<MeshCollider>();
        if (mc != null) mc.sharedMesh = rota;

        foreach (Transform hijo in defensa)
        {
            if (hijo.name == "Poste_Acero" && Vector3.Distance(hijo.position, centroMundo) < radio) hijo.gameObject.SetActive(false);
        }
    }

    void IniciarHumoCamion()
    {
        if (humoCamion == null || camionTransform == null) return;
        humoCamion.transform.SetParent(camionTransform, false);
        humoCamion.transform.localPosition = new Vector3(0f, 1.9f, 3.9f);
        humoCamion.Play();
    }

    // ------------------------------------------------------------ FASE 3: RESCATE Y AMBULANCIA

    IEnumerator FaseRescate()
    {
        // La ambulancia se estaciona SOBRE la carretera, en el carril derecho (+3.2m), cerca del camión siniestrado
        float sRescate = puntoAccidente + 0.20f;
        Vector3 posAmbulancia, fwdAmbulancia, upAmbulancia, rightAmbulancia;
        Quaternion rotAmbulancia;
        EvaluarRuta(sRescate, 3.2f, out posAmbulancia, out rotAmbulancia, out fwdAmbulancia, out upAmbulancia, out rightAmbulancia);

        // Punto junto a la cabina del camión (del lado de la carretera) de donde salen los paramédicos con la camilla
        Vector3 haciaCarretera = posAmbulancia - camionTransform.position;
        haciaCarretera.y = 0f;
        if (haciaCarretera.sqrMagnitude < 0.01f) haciaCarretera = -rightAmbulancia;
        Vector3 bordeVallaRota = AlSuelo(camionTransform.position + camionTransform.forward * 3.0f + haciaCarretera.normalized * 2.5f);

        Vector3 traseraAmbulancia = posAmbulancia - fwdAmbulancia * 3.6f;

        if (ambulanciaObjeto != null)
        {
            ambulanciaObjeto.transform.position = posAmbulancia + upAmbulancia * 0.08f;
            ambulanciaObjeto.transform.rotation = rotAmbulancia;
            ambulanciaObjeto.SetActive(true);
        }

        sirenasActivas = true;
        if (audioSourceSirena != null)
        {
            audioSourceSirena.volume = 0.5f;
            audioSourceSirena.Play();
        }

        // Crear camilla (con el Chofer encima) y paramédicos
        GameObject camilla = CrearCamilla();
        GameObject med1 = CrearParamedico("Paramedico_1");   // va al frente, de espaldas a la marcha (agarra la camilla hacia atrás)
        GameObject med2 = CrearParamedico("Paramedico_2");
        ActorCaminante actor1 = med1.GetComponent<ActorCaminante>();
        if (actor1 != null) actor1.brazosAtras = true;
        corteBajoRescate = false;

        // Bengalas rojas sobre el asfalto mojado detrás de la ambulancia y una junto al camión
        if (bengalasDeEmergencia && ambienteLluvia != null)
        {
            for (int i = 0; i < 3; i++)
                ambienteLluvia.CrearBengala(AlSuelo(posAmbulancia - fwdAmbulancia * (7f + i * 5.5f) + rightAmbulancia * (0.8f - i * 0.8f)));
            ambienteLluvia.CrearBengala(AlSuelo(posAmbulancia + fwdAmbulancia * 9f + rightAmbulancia * 1.2f));
        }

        // Cámara de rescate encuadrando la ambulancia, el asfalto mojado y las sirenas
        Vector3 centroEscena = (posAmbulancia + bordeVallaRota) * 0.5f;
        Vector3 posCamRescate = centroEscena - rightAmbulancia * 12.5f - fwdAmbulancia * 2.5f + Vector3.up * 2.8f;
        Vector3 miraCamRescate = centroEscena + Vector3.up * 1.1f;

        if (camaraPrincipal != null)
        {
            camaraPrincipal.fieldOfView = 50f;
            camaraPrincipal.transform.position = posCamRescate;
            camaraPrincipal.transform.LookAt(miraCamRescate);
        }

        // Aparece la escena de emergencias
        // El mundo vuelve poco a poco: ambiente y un colchón musical triste
        StartCoroutine(RampaVolumen(audioSourceAmbiente, 0.30f, 3.0f));
        StartCoroutine(RampaVolumen(audioSourceMusica, 0.16f, 3.0f));
        if (audioSourceMusica != null) audioSourceMusica.pitch = 0.9f;

        MostrarSub(textoMinutosDespues, 1.0f);
        yield return Fade(fadeCanvas, 1f, 0f, 1.2f);
        MostrarSub(textoParamedico1, 2.4f);

        // Los paramédicos cargan la camilla caminando desde la valla hasta la puerta trasera de la ambulancia
        Vector3 dirCamilla = (traseraAmbulancia - bordeVallaRota);
        dirCamilla.y = 0f;
        dirCamilla.Normalize();

        float durCaminata = 4.2f;
        float tCam = 0f;

        while (tCam < durCaminata)
        {
            tCam += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, tCam / durCaminata);
            Vector3 posPaso = Vector3.Lerp(bordeVallaRota, traseraAmbulancia, k);
            float rebotePaso = Mathf.Sin(tCam * 8f) * 0.035f;

            // Todos caminan pegados al terreno (la ladera sube hacia la carretera)
            Vector3 pCamilla = AlSuelo(posPaso);
            Vector3 pMed1 = AlSuelo(posPaso + dirCamilla * 1.25f);
            Vector3 pMed2 = AlSuelo(posPaso - dirCamilla * 1.25f);

            camilla.transform.position = pCamilla + Vector3.up * (0.85f + rebotePaso);
            camilla.transform.rotation = Quaternion.LookRotation(dirCamilla, Vector3.up);

            med1.transform.position = pMed1 + Vector3.up * rebotePaso;
            med1.transform.rotation = Quaternion.LookRotation(dirCamilla, Vector3.up);

            med2.transform.position = pMed2 + Vector3.up * rebotePaso;
            med2.transform.rotation = Quaternion.LookRotation(dirCamilla, Vector3.up);

            if (tCam < 2.1f)
            {
                PosicionarCamara(posCamRescate + rightAmbulancia * Mathf.Sin(tCam * 0.5f) * 0.3f, miraCamRescate, 4f);
            }
            else
            {
                // CORTE a un plano bajo, casi al ras del asfalto mojado: se ven los reflejos de las sirenas y las bengalas
                Vector3 camBaja = AlSuelo(traseraAmbulancia - fwdAmbulancia * 6.5f - rightAmbulancia * 2.6f) + Vector3.up * 0.5f;
                Vector3 miraBaja = Vector3.Lerp(pCamilla, traseraAmbulancia, 0.5f) + Vector3.up * 1.0f;
                if (!corteBajoRescate)
                {
                    corteBajoRescate = true;
                    if (camaraPrincipal != null) { camaraPrincipal.transform.position = camBaja; camaraPrincipal.fieldOfView = 40f; }
                    if (ambienteLluvia != null) ambienteLluvia.GotasExtra(5);
                }
                PosicionarCamara(camBaja + fwdAmbulancia * Mathf.Min(tCam - 2.1f, 2.5f) * 0.35f, miraBaja, 5f);
            }
            yield return null;
        }

        MostrarSub(textoParamedico2, 2.0f);

        // Llegan a la puerta trasera: un respiro para verlos antes de subir
        yield return new WaitForSeconds(0.6f);

        // Guardan la camilla y suben a la ambulancia
        ReproducirAudioProcedural(TipoFx.PuertasAmbulancia, 0.8f);
        camilla.SetActive(false);
        med1.SetActive(false);
        med2.SetActive(false);
        yield return new WaitForSeconds(0.9f);

        // LA AMBULANCIA PARTE CON CÁMARA TRAVELING DE PERSECUCIÓN POR LA RUTA REAL
        float sAmbulancia = sRescate;
        float velAmbulancia = 0f;
        float tiempoViaje = 0f;
        float maxTiempoViaje = 5.6f;

        while (tiempoViaje < maxTiempoViaje && ambulanciaObjeto != null)
        {
            float dt = Time.deltaTime;
            tiempoViaje += dt;
            velAmbulancia = Mathf.Min(velAmbulancia + 0.16f * dt, 0.46f);
            sAmbulancia += dt * velAmbulancia;

            // Seguir rigurosamente el carril derecho (+3.0m) de la sierra con peralte
            Vector3 posPista, fwdPista, upPista, rightPista;
            Quaternion rotPista;
            EvaluarRuta(sAmbulancia, 3.0f, out posPista, out rotPista, out fwdPista, out upPista, out rightPista);

            ambulanciaObjeto.transform.position = posPista + upPista * 0.08f;
            ambulanciaObjeto.transform.rotation = rotPista;

            RotarRuedas(ruedasAmbulancia, ambulanciaObjeto != null ? ambulanciaObjeto.transform : null, dt * velAmbulancia * 520f);

            // CÁMARA TRAVELING CINEMATOGRÁFICA EN MOVIMIENTO SIGUIENDO LA AMBULANCIA
            Vector3 camSeguimiento = posPista - fwdPista * 11.5f + upPista * 3.8f - rightPista * 2.2f;
            Vector3 objetivoMirar = posPista + upPista * 1.5f + fwdPista * 2.0f;
            PosicionarCamara(camSeguimiento, objetivoMirar, 6.0f);

            // Fundido final a negro mientras se aleja en las curvas nocturnas
            if (tiempoViaje > 3.6f)
            {
                fadeCanvas.alpha = Mathf.Clamp01((tiempoViaje - 3.6f) / 1.8f);
                if (audioSourceSirena != null)
                {
                    audioSourceSirena.volume = Mathf.Lerp(audioSourceSirena.volume, 0f, dt * 1.5f);
                }
            }

            yield return null;
        }

        sirenasActivas = false;
        if (sirenaAzul != null) sirenaAzul.enabled = false;
        if (sirenaRoja != null) sirenaRoja.enabled = false;
        if (audioSourceSirena != null) audioSourceSirena.Stop();
        if (ambulanciaObjeto != null) ambulanciaObjeto.SetActive(false);
        fadeCanvas.alpha = 1f;
    }

    // ------------------------------------------------------------ FASE 4: DESPRENDIMIENTO DEL ALMA

    IEnumerator FaseDesprendimientoAlma()
    {
        // Precarga el Nivel 1 en segundo plano para que la transición no tenga tirones
        AsyncOperation carga = null;
        if (Application.CanStreamedLevelBeLoaded(escenaNivel1))
        {
            carga = SceneManager.LoadSceneAsync(escenaNivel1);
            carga.allowSceneActivation = false;
            cargaPendiente = carga;
        }
        else
        {
            Debug.LogError("[Cinemática] No se encontró la escena '" + escenaNivel1 + "' en Build Settings.");
        }

        StartCoroutine(AnimarBarras(0f, 1.2f));
        StartCoroutine(RampaVolumen(audioSourceMusica, 0f, 1.5f));
        StartCoroutine(RampaVolumen(audioSourceAmbiente, 0f, 1.5f));

        // Latido de corazón sobre negro
        if (audioSourceLatido != null)
        {
            audioSourceLatido.volume = 0f;
            audioSourceLatido.Play();
            StartCoroutine(RampaVolumen(audioSourceLatido, 0.55f, 2.0f));
        }
        yield return new WaitForSeconds(1.6f);

        yield return StartCoroutine(DecirSub(textoCuerpo, 2.2f));

        // Aparece el orbe de alma, pulsando al ritmo del latido
        ReproducirAudioProcedural(TipoFx.AlmaEterea, 0.7f);
        StartCoroutine(AnimarOrbeAlma());
        yield return new WaitForSeconds(1.2f);

        yield return StartCoroutine(DecirSub(textoAlmaDespierta, 2.0f));
        yield return new WaitForSeconds(0.4f);

        // Destello espiritual que se disuelve en negro (conecta con el despertar del Nivel 1)
        yield return Fade(flashAlma, 0f, 1f, 0.9f);
        fadeCanvas.alpha = 1f;
        if (orbeAlma != null) orbeAlma.color = new Color(1f, 1f, 1f, 0f);
        if (audioSourceLatido != null) audioSourceLatido.Stop();
        yield return Fade(flashAlma, 1f, 0f, 1.1f);

        Time.timeScale = 1f;
        if (carga != null)
        {
            while (carga.progress < 0.9f) yield return null;
            carga.allowSceneActivation = true;
        }
    }

    IEnumerator AnimarOrbeAlma()
    {
        if (orbeAlma == null) yield break;
        RectTransform rt = orbeAlma.rectTransform;
        float t = 0f;
        while (true)
        {
            t += Time.unscaledDeltaTime;
            float aparicion = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 2.0f));
            // Latido: doble pulso por segundo (lub-dub)
            float fase = t % 1f;
            float pulso = Mathf.Exp(-fase * 7f) + 0.6f * Mathf.Exp(-Mathf.Abs(fase - 0.28f) * 14f);
            float esc = Mathf.Lerp(0.15f, 1.0f, aparicion) * (1f + pulso * 0.12f);
            // Después de la aparición, el orbe crece lentamente (el alma se expande)
            esc *= 1f + Mathf.Max(0f, t - 3.2f) * 0.9f;
            rt.localScale = Vector3.one * esc;
            orbeAlma.color = new Color(0.55f, 1f, 0.95f, aparicion * 0.9f);
            yield return null;
        }
    }

    IEnumerator RampaVolumen(AudioSource fuente, float objetivo, float dur)
    {
        if (fuente == null) yield break;
        float inicio = fuente.volume;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            fuente.volume = Mathf.Lerp(inicio, objetivo, Mathf.Clamp01(t / dur));
            yield return null;
        }
        fuente.volume = objetivo;
    }

    // ------------------------------------------------------------ AMBIENTE Y TÍTULO

    void IniciarAmbiente()
    {
        if (audioSourceAmbiente != null)
        {
            audioSourceAmbiente.volume = 0f;
            audioSourceAmbiente.Play();
            StartCoroutine(RampaVolumen(audioSourceAmbiente, 0.35f, 3.0f));
        }
        if (audioSourceMusica != null)
        {
            audioSourceMusica.volume = 0f;
            audioSourceMusica.pitch = 1f;
            audioSourceMusica.Play();
            StartCoroutine(RampaVolumen(audioSourceMusica, 0.10f, 4.0f));
        }
        if (lluvia != null && lluviaNocturna) lluvia.Play();
    }

    IEnumerator TarjetaTitulo(string titulo, string sub)
    {
        if (textoTitulo == null) yield break;
        textoTitulo.text = titulo;
        textoSubTitulo.text = sub;
        textoTitulo.characterSpacing = 10f;

        // Entrada: el espaciado entre letras se cierra lentamente (efecto de título de cine)
        float t = 0f;
        const float dIn = 1.6f;
        while (t < dIn)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / dIn);
            textoTitulo.alpha = k;
            textoTitulo.characterSpacing = Mathf.Lerp(26f, 12f, k);
            textoSubTitulo.alpha = Mathf.Clamp01((k - 0.4f) / 0.6f) * 0.85f;
            yield return null;
        }

        yield return new WaitForSecondsRealtime(1.4f);

        t = 0f;
        const float dOut = 1.0f;
        while (t < dOut)
        {
            t += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Clamp01(t / dOut);
            textoTitulo.alpha = k;
            textoSubTitulo.alpha = k * 0.85f;
            yield return null;
        }
        textoTitulo.alpha = 0f;
        textoSubTitulo.alpha = 0f;
    }

    // ============================================================ EVALUACIÓN RIGUROSA DE RUTAS Y CURVAS

    public void EvaluarRuta(
        float s,
        float offsetLateral,
        out Vector3 posMundo,
        out Quaternion rotMundo,
        out Vector3 fwdMundo,
        out Vector3 upMundo,
        out Vector3 rightMundo)
    {
        if (generadorCarretera != null)
        {
            if (generadorCarretera.EvaluarRutaCompleta(s, offsetLateral, out posMundo, out rotMundo, out fwdMundo, out upMundo, out rightMundo))
            {
                return;
            }
        }

        // Respaldo matemático autónomo con idéntica spline Catmull-Rom y peralte
        List<Vector3> pts = puntosFallback;
        int numTramos = pts.Count - 3;
        float sClamped = Mathf.Clamp(s, 0f, (float)numTramos - 0.001f);
        int tramo = Mathf.FloorToInt(sClamped);
        float t = sClamped - tramo;

        Vector3 posLocal = CatmullRom(pts[tramo], pts[tramo + 1], pts[tramo + 2], pts[tramo + 3], t);
        Vector3 tanLocal = CatmullRomTangente(pts[tramo], pts[tramo + 1], pts[tramo + 2], pts[tramo + 3], t).normalized;

        Vector3 sigTan = CatmullRomTangente(pts[tramo], pts[tramo + 1], pts[tramo + 2], pts[tramo + 3], Mathf.Clamp01(t + 0.04f)).normalized;
        float curvatura = Vector3.Cross(tanLocal, sigTan).y * 20f;

        Vector3 upLocal = Vector3.up;
        Vector3 rightLocal = Vector3.Cross(upLocal, tanLocal).normalized;

        float peralte = Mathf.Clamp(curvatura * 3.5f, -12f, 12f);
        Quaternion rotPeralte = Quaternion.AngleAxis(peralte, tanLocal);
        rightLocal = rotPeralte * rightLocal;
        upLocal = rotPeralte * upLocal;

        Vector3 punto = posLocal + rightLocal * offsetLateral + upLocal * 0.05f;

        posMundo = punto;
        fwdMundo = tanLocal;
        upMundo = upLocal;
        rightMundo = rightLocal;
        rotMundo = Quaternion.LookRotation(fwdMundo, upMundo);
    }

    private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    private Vector3 CatmullRomTangente(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        return 0.5f * (
            (-p0 + p2) +
            (2f * (2f * p0 - 5f * p1 + 4f * p2 - p3) * t) +
            (3f * (-p0 + 3f * p1 - 3f * p2 + p3) * t2)
        );
    }

    private void RotarRuedas(List<Transform> ruedas, Transform vehiculo, float angulo)
    {
        if (ruedas == null || vehiculo == null) return;
        Vector3 eje = vehiculo.right;
        for (int i = 0; i < ruedas.Count; i++)
        {
            if (ruedas[i] != null)
            {
                ruedas[i].RotateAround(ruedas[i].position, eje, angulo);
            }
        }
    }

    private void EncenderLucesFreno(bool encendido)
    {
        if (luzBrakingCamion != null)
        {
            luzBrakingCamion.intensity = encendido ? 14f : 0f;
        }

        if (calaverasCamion != null)
        {
            Color c = encendido ? new Color(2.5f, 0.1f, 0.1f) : new Color(0.85f, 0.05f, 0.05f);
            foreach (var r in calaverasCamion)
            {
                if (r != null && r.sharedMaterial != null)
                {
                    r.sharedMaterial.color = encendido ? Color.red : new Color(0.6f, 0.05f, 0.05f);
                    if (r.sharedMaterial.HasProperty("_EmissionColor"))
                    {
                        r.sharedMaterial.SetColor("_EmissionColor", c);
                    }
                }
            }
        }
    }

    void PosicionarCamara(Vector3 posDeseada, Vector3 objetivoMirar, float suavizado)
    {
        if (camaraPrincipal == null) return;
        Transform ct = camaraPrincipal.transform;
        ct.position = Vector3.Lerp(ct.position, posDeseada, 1f - Mathf.Exp(-suavizado * Time.unscaledDeltaTime));

        // Cámara "en mano": ruido Perlin continuo (más natural que el ruido aleatorio por frame).
        // Siempre hay un temblor mínimo y se intensifica con shakeCamara (frenazo, impacto).
        float intensidad = 0.035f + shakeCamara;
        Vector3 ruido = RuidoPerlin3(Time.unscaledTime * (6f + shakeCamara * 14f));

        Vector3 miraConShake = objetivoMirar + ruido * intensidad * 0.45f;

        Vector3 dir = (miraConShake - ct.position).normalized;
        if (dir.sqrMagnitude > 0.001f)
        {
            rollCamara = Mathf.MoveTowards(rollCamara, rollObjetivo, 22f * Time.unscaledDeltaTime);
            Quaternion rotDeseada = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(0f, 0f, rollCamara);
            ct.rotation = Quaternion.Slerp(ct.rotation, rotDeseada, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
        }

        ct.position += ruido * intensidad * 0.15f;
    }

    /// <summary>Ruido Perlin suave en [-1, 1] por eje.</summary>
    static Vector3 RuidoPerlin3(float t)
    {
        return new Vector3(
            Mathf.PerlinNoise(t, 0.37f) * 2f - 1f,
            Mathf.PerlinNoise(0.71f, t) * 2f - 1f,
            Mathf.PerlinNoise(t + 13.1f, t * 0.5f) * 2f - 1f);
    }

    // ============================================================ PARTÍCULAS (LLUVIA, CHISPAS, POLVO)

    Material MaterialParticulas(Color color)
    {
        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) return null;
        Material m = new Material(sh);
        m.color = color;
        return m;
    }

    ParticleSystem NuevoSistema(string nombre, Material mat)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(transform, false);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (mat != null) go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
        return ps;
    }

    void CrearEfectosParticulas()
    {
        // ---- Lluvia fina
        Material matLluvia = MaterialParticulas(Color.white);
        if (matLluvia != null)
        {
            lluvia = NuevoSistema("Lluvia_Noche", matLluvia);
            lluvia.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // emite hacia abajo

            var main = lluvia.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 0.9f;
            main.startSpeed = 26f;
            main.startSize = 0.03f;
            main.startColor = new Color(0.78f, 0.86f, 1f, 0.30f);
            main.maxParticles = 1500;

            var em = lluvia.emission;
            em.rateOverTime = 650f;

            var sh = lluvia.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(36f, 36f, 0.5f);

            var r = lluvia.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.03f;
            r.lengthScale = 1f;
        }

        CrearHumoCamion();

        // ---- Chispas del impacto metálico
        Material matChispa = MaterialParticulas(new Color(1f, 0.72f, 0.25f, 1f));
        if (matChispa != null)
        {
            chispasImpacto = NuevoSistema("Chispas_Impacto", matChispa);
            var main = chispasImpacto.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 16f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startColor = new Color(1f, 0.78f, 0.3f, 1f);
            main.gravityModifier = 1.6f;
            main.maxParticles = 200;

            var em = chispasImpacto.emission;
            em.rateOverTime = 0f;


            var sh = chispasImpacto.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.6f;

            var r = chispasImpacto.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.06f;
            r.lengthScale = 1.5f;
        }

        // ---- Polvo / humo del impacto
        Material matPolvo = MaterialParticulas(new Color(0.55f, 0.52f, 0.48f, 1f));
        if (matPolvo != null)
        {
            polvoImpacto = NuevoSistema("Polvo_Impacto", matPolvo);
            var main = polvoImpacto.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(1.2f, 2.8f);
            main.startColor = new Color(0.55f, 0.52f, 0.48f, 0.35f);
            main.gravityModifier = -0.02f;
            main.maxParticles = 120;

            var em = polvoImpacto.emission;
            em.rateOverTime = 0f;


            var sh = polvoImpacto.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 1.5f;

            // Se disuelve y crece con el tiempo
            var col = polvoImpacto.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);

            var sol = polvoImpacto.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(1f, 1.8f)));
        }
    }

    void CrearHumoCamion()
    {
        Material matHumo = MaterialParticulas(new Color(0.35f, 0.35f, 0.38f, 1f));
        if (matHumo == null) return;

        humoCamion = NuevoSistema("Humo_Camion", matHumo);
        var main = humoCamion.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4.0f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.0f);
        main.startColor = new Color(0.3f, 0.3f, 0.33f, 0.35f);
        main.gravityModifier = -0.05f;
        main.maxParticles = 80;

        var em = humoCamion.emission;
        em.rateOverTime = 14f;

        var sh = humoCamion.shape;
        sh.shapeType = ParticleSystemShapeType.Cone;
        sh.angle = 18f;
        sh.radius = 0.35f;
        humoCamion.transform.rotation = Quaternion.Euler(-90f, 0f, 0f); // hacia arriba

        var col = humoCamion.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(g);

        var sol = humoCamion.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.4f), new Keyframe(1f, 2.4f)));
    }

    void EmitirChispas(Vector3 posicion, Vector3 direccion)
    {
        if (chispasImpacto == null) return;
        chispasImpacto.transform.position = posicion;
        chispasImpacto.Play();
        chispasImpacto.Emit(90);
    }

    void EmitirPolvo(Vector3 posicion)
    {
        if (polvoImpacto == null) return;
        polvoImpacto.transform.position = posicion;
        polvoImpacto.Play();
        polvoImpacto.Emit(35);
    }

    void LanzarFragmentosValla(Vector3 centro, Vector3 fwd, Vector3 right)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        Material matMetal = new Material(sh);
        matMetal.color = new Color(0.72f, 0.74f, 0.78f);
        if (matMetal.HasProperty("_Metallic")) matMetal.SetFloat("_Metallic", 0.9f);

        for (int i = 0; i < 18; i++)
        {
            GameObject fragmento = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fragmento.name = "Fragmento_Valla_Metal";
            fragmento.GetComponent<MeshRenderer>().sharedMaterial = matMetal;
            fragmento.transform.position = centro + right * Random.Range(-0.5f, 1.5f) + fwd * Random.Range(-2f, 2f) + Vector3.up * Random.Range(0.2f, 1.6f);
            fragmento.transform.rotation = Random.rotation;
            fragmento.transform.localScale = new Vector3(Random.Range(0.18f, 0.45f), Random.Range(0.08f, 0.22f), Random.Range(0.8f, 2.2f));

            Rigidbody rb = fragmento.AddComponent<Rigidbody>();
            rb.mass = 2.5f;
            rb.linearVelocity = right * Random.Range(6f, 14f) + fwd * Random.Range(-3f, 7f) + Vector3.up * Random.Range(3f, 8f);
            rb.angularVelocity = Random.insideUnitSphere * 14f;

            Destroy(fragmento, 9f);
        }
    }

    void CrearVallaRota(Vector3 punto, Vector3 fwd, Vector3 right)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        Material matMetal = new Material(sh);
        matMetal.color = new Color(0.65f, 0.67f, 0.70f);

        // Barandal doblado hacia el abismo
        GameObject rota = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rota.name = "Defensa_Metalica_Doblada";
        rota.transform.position = punto + right * 0.5f - Vector3.up * 0.4f;
        rota.transform.rotation = Quaternion.LookRotation(fwd, right) * Quaternion.Euler(35f, 25f, -40f);
        rota.transform.localScale = new Vector3(0.25f, 0.35f, 4.5f);
        rota.GetComponent<MeshRenderer>().sharedMaterial = matMetal;
        Destroy(rota.GetComponent<Collider>());
    }

    // ============================================================ MODELOS TEMPORALES (CAMILLA Y PARAMÉDICOS)

    GameObject CrearCamilla()
    {
        GameObject root = new GameObject("Camilla_Rescate");

        // Si hay una camilla real (Resources/Cinematica/Camilla) se usa; si no, la provisional de abajo
        GameObject real = Resources.Load<GameObject>("Cinematica/Camilla");
        if (real != null)
        {
            GameObject c = Instantiate(real, root.transform);
            c.transform.localPosition = new Vector3(0f, -0.85f, 0f);
            foreach (Collider col in c.GetComponentsInChildren<Collider>()) Destroy(col);
            ColocarChoferEnCamilla(root);
            return root;
        }

        Material metal = NuevoMaterial(new Color(0.78f, 0.8f, 0.84f), 0.8f, 0.6f);
        Material colchon = NuevoMaterial(new Color(0.1f, 0.24f, 0.5f), 0.5f, 0f);
        Material correa = NuevoMaterial(new Color(0.95f, 0.5f, 0.05f), 0.4f, 0f);
        Material negro = NuevoMaterial(new Color(0.04f, 0.04f, 0.05f), 0.3f, 0f);

        Parte(root.transform, PrimitiveType.Cube, "Colchon", new Vector3(0f, 0f, 0f), new Vector3(0.72f, 0.1f, 1.95f), colchon);
        Parte(root.transform, PrimitiveType.Cube, "Marco", new Vector3(0f, -0.08f, 0f), new Vector3(0.78f, 0.05f, 2.02f), metal);
        Parte(root.transform, PrimitiveType.Cube, "BarandalI", new Vector3(-0.4f, 0.04f, 0f), new Vector3(0.03f, 0.06f, 1.9f), metal);
        Parte(root.transform, PrimitiveType.Cube, "BarandalD", new Vector3(0.4f, 0.04f, 0f), new Vector3(0.03f, 0.06f, 1.9f), metal);
        foreach (float z in new[] { -0.7f, 0.7f })
            foreach (float x in new[] { -0.3f, 0.3f })
            {
                Parte(root.transform, PrimitiveType.Cylinder, "Pata", new Vector3(x, -0.47f, z), new Vector3(0.045f, 0.4f, 0.045f), metal);
                GameObject rueda = Parte(root.transform, PrimitiveType.Cylinder, "RuedaCamilla", new Vector3(x, -0.82f, z), new Vector3(0.16f, 0.035f, 0.16f), negro);
                rueda.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }

        ColocarChoferEnCamilla(root);

        // manta térmica plateada sobre las piernas y el torso, y correas naranjas
        Material manta = NuevoMaterial(new Color(0.86f, 0.8f, 0.55f), 0.85f, 0.9f);
        Parte(root.transform, PrimitiveType.Cube, "MantaPiernas", new Vector3(0f, 0.25f, 0.62f), new Vector3(0.64f, 0.05f, 0.66f), manta);
        Parte(root.transform, PrimitiveType.Cube, "MantaTorso", new Vector3(0f, 0.34f, 0.0f), new Vector3(0.68f, 0.05f, 0.5f), manta);
        Parte(root.transform, PrimitiveType.Cube, "Correa1", new Vector3(0f, 0.38f, 0.1f), new Vector3(0.76f, 0.02f, 0.07f), correa);
        Parte(root.transform, PrimitiveType.Cube, "Correa2", new Vector3(0f, 0.29f, 0.6f), new Vector3(0.7f, 0.02f, 0.07f), correa);
        return root;
    }

    /// <summary>Acuesta al Chofer (modelo de Meshy) boca arriba sobre la camilla, con la cabeza hacia atrás. Sin modelo, una figura provisional.</summary>
    void ColocarChoferEnCamilla(GameObject camilla)
    {
        if (prefabChoferHerido != null)
        {
            GameObject ch = Instantiate(prefabChoferHerido, camilla.transform);
            ch.name = "Chofer_Herido";
            ch.transform.localPosition = posChoferEnCamilla;
            ch.transform.localRotation = Quaternion.Euler(rotChoferEnCamilla);
            foreach (Animator an in ch.GetComponentsInChildren<Animator>(true)) an.enabled = false;   // se queda en su pose base
            foreach (Collider col in ch.GetComponentsInChildren<Collider>(true)) Destroy(col);
            CharacterController cc = ch.GetComponent<CharacterController>();
            if (cc != null) Destroy(cc);
            return;
        }

        Parte(camilla.transform, PrimitiveType.Capsule, "ChoferProvisional", new Vector3(0f, 0.2f, 0f), new Vector3(0.36f, 0.85f, 0.3f), NuevoMaterial(new Color(0.9f, 0.85f, 0.7f), 0.3f, 0f))
            .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Parte(camilla.transform, PrimitiveType.Sphere, "CabezaProvisional", new Vector3(0f, 0.22f, -0.95f), Vector3.one * 0.26f, NuevoMaterial(new Color(0.78f, 0.58f, 0.45f), 0.3f, 0f));
    }

    GameObject Parte(Transform padre, PrimitiveType tipo, string nombre, Vector3 pos, Vector3 escala, Material mat)
    {
        GameObject g = GameObject.CreatePrimitive(tipo);
        g.name = nombre;
        g.transform.SetParent(padre, false);
        g.transform.localPosition = pos;
        g.transform.localScale = escala;
        g.GetComponent<MeshRenderer>().sharedMaterial = mat;
        Destroy(g.GetComponent<Collider>());
        return g;
    }

    GameObject CrearParamedico(string nombre)
    {
        bool primero = nombre.EndsWith("1");
        return ActorCaminante.Crear(nombre, primero ? "Paramedico" : "Paramedica",
            primero ? new Color(0.12f, 0.38f, 0.28f) : new Color(0.1f, 0.3f, 0.42f), true);
    }

    Material NuevoMaterial(Color c, float suavidad = 0.4f, float metalico = 0f)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        Material m = new Material(sh);
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", suavidad);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metalico);
        return m;
    }

    // ============================================================ OVERLAY CINEMÁTICO (UI)

    void ConstruirOverlay()
    {
        GameObject cgo = new GameObject("Overlay_Cinematica_Guanajuato");
        Canvas canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        CanvasScaler sc = cgo.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);

        // Orden de dibujado (de atrás hacia adelante):
        // flash blanco -> fundido a negro -> barras de cine -> orbe de alma -> título -> subtítulos -> indicador de salto -> destello espiritual.
        // El fundido va DEBAJO de los textos para que subtítulos y título se lean incluso sobre negro.
        flashCanvas = CrearPantallaCompleta("FlashBlanco", cgo.transform, Color.white);
        flashCanvas.alpha = 0f;

        fadeCanvas = CrearPantallaCompleta("FadeNegro", cgo.transform, Color.black);
        fadeCanvas.alpha = 1f;

        barraSup = CrearBarraNegra("BarraSuperior", cgo.transform, true);
        barraInf = CrearBarraNegra("BarraInferior", cgo.transform, false);

        Sprite radial = CrearSpriteRadial(128);

        // Orbe de alma (fase final)
        GameObject ogo = new GameObject("OrbeAlma");
        ogo.transform.SetParent(cgo.transform, false);
        orbeAlma = ogo.AddComponent<Image>();
        orbeAlma.sprite = radial;
        orbeAlma.color = new Color(0.55f, 1f, 0.95f, 0f);
        orbeAlma.raycastTarget = false;
        RectTransform ort = orbeAlma.rectTransform;
        ort.anchorMin = ort.anchorMax = new Vector2(0.5f, 0.5f);
        ort.sizeDelta = new Vector2(620f, 620f);
        ort.localScale = Vector3.one * 0.15f;

        // Tarjeta de título (centrada)
        textoTitulo = CrearTextoUI("TituloJuego", cgo.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(1500f, 140f), 92, FontStyles.Bold);
        textoTitulo.color = new Color(0.95f, 0.88f, 0.70f, 1f);
        textoTitulo.alpha = 0f;
        textoSubTitulo = CrearTextoUI("SubtituloJuego", cgo.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -55f), new Vector2(1200f, 60f), 30, FontStyles.Normal);
        textoSubTitulo.characterSpacing = 18f;
        textoSubTitulo.color = new Color(0.75f, 0.80f, 0.85f, 1f);
        textoSubTitulo.alpha = 0f;

        // Subtítulos con fondo suave para legibilidad sobre cualquier plano
        GameObject fgo = new GameObject("FondoSubtitulos");
        fgo.transform.SetParent(cgo.transform, false);
        Image fondo = fgo.AddComponent<Image>();
        fondo.sprite = radial;
        fondo.color = new Color(0f, 0f, 0f, 0.62f);
        fondo.raycastTarget = false;
        RectTransform frt = fondo.rectTransform;
        frt.anchorMin = new Vector2(0.10f, 0f);
        frt.anchorMax = new Vector2(0.90f, 0f);
        frt.pivot = new Vector2(0.5f, 0f);
        frt.anchoredPosition = new Vector2(0f, 105f);
        frt.sizeDelta = new Vector2(0f, 190f);
        fondoSubtitulos = fgo.AddComponent<CanvasGroup>();
        fondoSubtitulos.alpha = 0f;
        fondoSubtitulos.blocksRaycasts = false;

        GameObject sgo = new GameObject("SubtitulosCinematica");
        sgo.transform.SetParent(cgo.transform, false);
        RectTransform srt = sgo.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.12f, 0f);
        srt.anchorMax = new Vector2(0.88f, 0f);
        srt.pivot = new Vector2(0.5f, 0f);
        srt.anchoredPosition = new Vector2(0f, 125f);
        srt.sizeDelta = new Vector2(0f, 150f);

        textoSubtitulos = sgo.AddComponent<TextMeshProUGUI>();
        textoSubtitulos.alignment = TextAlignmentOptions.Center;
        textoSubtitulos.fontSize = 34;
        textoSubtitulos.color = new Color(1f, 1f, 1f, 0f);
        textoSubtitulos.outlineWidth = 0.18f;
        textoSubtitulos.outlineColor = new Color32(0, 0, 0, 255);
        textoSubtitulos.raycastTarget = false;

        // Indicador "mantén para saltar" (esquina inferior derecha, sobre la barra de cine)
        if (permitirSaltar)
        {
            textoSaltar = CrearTextoUI("TextoSaltar", cgo.transform, new Vector2(1f, 0f), new Vector2(-40f, 52f), new Vector2(520f, 36f), 22, FontStyles.Normal);
            textoSaltar.rectTransform.pivot = new Vector2(1f, 0f);
            textoSaltar.alignment = TextAlignmentOptions.Right;
            textoSaltar.text = "Mantén  [ESPACIO]  para saltar";
            textoSaltar.color = new Color(0.8f, 0.8f, 0.8f, 0.55f);

            GameObject bgo = new GameObject("BarraSaltar");
            bgo.transform.SetParent(cgo.transform, false);
            Image bimg = bgo.AddComponent<Image>();
            bimg.color = new Color(0.95f, 0.88f, 0.70f, 0.95f);
            bimg.raycastTarget = false;
            barraSaltar = bimg.rectTransform;
            barraSaltar.anchorMin = barraSaltar.anchorMax = new Vector2(1f, 0f);
            barraSaltar.pivot = new Vector2(1f, 0f);
            barraSaltar.anchoredPosition = new Vector2(-40f, 44f);
            barraSaltar.sizeDelta = new Vector2(0f, 3f);
        }

        // Destello espiritual (cian/blanco) para la transición al Nivel 1; encima de todo
        flashAlma = CrearPantallaCompleta("DestelloAlma", cgo.transform, new Color(0.80f, 1f, 0.97f, 1f));
        flashAlma.alpha = 0f;
    }

    TextMeshProUGUI CrearTextoUI(string nombre, Transform padre, Vector2 ancla, Vector2 pos, Vector2 tam, float fontSize, FontStyles estilo)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = fontSize;
        tmp.fontStyle = estilo;
        tmp.raycastTarget = false;
        RectTransform rt = tmp.rectTransform;
        rt.anchorMin = rt.anchorMax = ancla;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
        return tmp;
    }

    /// <summary>Sprite circular con borde suave (alpha decreciente hacia el borde). Sirve para orbe y sombra de subtítulos.</summary>
    Sprite CrearSpriteRadial(int n)
    {
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float dx = (x / (n - 1f) - 0.5f) * 2f;
                float dy = (y / (n - 1f) - 0.5f) * 2f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = 1f - Mathf.SmoothStep(0.15f, 1f, d);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }

    CanvasGroup CrearPantallaCompleta(string nombre, Transform padre, Color color)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        CanvasGroup cg = go.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        return cg;
    }

    RectTransform CrearBarraNegra(string nombre, Transform padre, bool arriba)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        Image img = go.AddComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, arriba ? 1f : 0f);
        rt.anchorMax = new Vector2(1f, arriba ? 1f : 0f);
        rt.pivot = new Vector2(0.5f, arriba ? 1f : 0f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, 0f);
        return rt;
    }

    IEnumerator AnimarBarras(float alturaFinal, float duracion)
    {
        float inicio = barraSup.sizeDelta.y;
        float t = 0f;
        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            float h = Mathf.Lerp(inicio, alturaFinal, Mathf.SmoothStep(0f, 1f, t / duracion));
            barraSup.sizeDelta = new Vector2(0f, h);
            barraInf.sizeDelta = new Vector2(0f, h);
            yield return null;
        }
    }

    IEnumerator Fade(CanvasGroup cg, float desde, float hasta, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(desde, hasta, t / dur);
            yield return null;
        }
        cg.alpha = hasta;
    }

    IEnumerator Flash(float intensidad, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            flashCanvas.alpha = Mathf.Lerp(intensidad, 0f, t / dur);
            yield return null;
        }
        flashCanvas.alpha = 0f;
    }

    // ------------------------------------------------------------ SUBTÍTULOS (con cola)

    /// <summary>Encola un subtítulo: se muestra cuando termina el anterior (no se pisan líneas de diálogo).</summary>
    void MostrarSub(string texto, float duracion)
    {
        colaSubs.Enqueue(new KeyValuePair<string, float>(texto, duracion));
        if (rutinaCola == null) rutinaCola = StartCoroutine(ProcesarColaSubs());
    }

    /// <summary>Corta lo que se esté mostrando y muestra el subtítulo de inmediato (alertas críticas).</summary>
    void MostrarSubUrgente(string texto, float duracion)
    {
        colaSubs.Clear();
        if (rutinaCola != null) StopCoroutine(rutinaCola);
        if (rutinaSub != null) StopCoroutine(rutinaSub);
        rutinaSub = null;
        colaSubs.Enqueue(new KeyValuePair<string, float>(texto, duracion));
        rutinaCola = StartCoroutine(ProcesarColaSubs());
    }

    IEnumerator ProcesarColaSubs()
    {
        while (colaSubs.Count > 0)
        {
            var linea = colaSubs.Dequeue();
            rutinaSub = StartCoroutine(RutinaSub(linea.Key, linea.Value));
            yield return rutinaSub;
        }
        rutinaCola = null;
    }

    /// <summary>Muestra un subtítulo y espera a que termine (para secuencias que dependen del texto).</summary>
    IEnumerator DecirSub(string texto, float duracion)
    {
        while (rutinaCola != null) yield return null; // espera a que la cola se vacíe
        yield return StartCoroutine(RutinaSub(texto, duracion));
    }

    void AplicarAlphaSub(float a)
    {
        if (textoSubtitulos != null)
        {
            Color c = textoSubtitulos.color;
            c.a = a;
            textoSubtitulos.color = c;
        }
        if (fondoSubtitulos != null) fondoSubtitulos.alpha = a;
    }

    IEnumerator RutinaSub(string texto, float duracion)
    {
        textoSubtitulos.text = texto;
        textoSubtitulos.maxVisibleCharacters = 0;
        textoSubtitulos.color = Color.white;
        textoSubtitulos.ForceMeshUpdate();

        // Aparición del fondo suave y escritura tipo máquina
        int total = textoSubtitulos.textInfo.characterCount;
        float t = 0f;
        while (textoSubtitulos.maxVisibleCharacters < total)
        {
            t += Time.unscaledDeltaTime;
            if (fondoSubtitulos != null) fondoSubtitulos.alpha = Mathf.Clamp01(t * 6f);
            textoSubtitulos.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(t * 42f));
            yield return null;
        }
        if (fondoSubtitulos != null) fondoSubtitulos.alpha = 1f;

        yield return new WaitForSecondsRealtime(duracion);

        float a = 1f;
        while (a > 0f)
        {
            a -= Time.unscaledDeltaTime * 2.8f;
            AplicarAlphaSub(Mathf.Max(0f, a));
            yield return null;
        }
        rutinaSub = null;
    }

    // ============================================================ AUDIO PROCEDURAL PROFESIONAL

    private enum TipoFx
    {
        FrenosChillando,
        DerrapeLlantas,
        ChoqueMetalValla,
        GolpeFondoCanon,
        PuertasAmbulancia,
        AlmaEterea,
        ZumbidoOidos
    }

    private void ConfigurarAudioProcedural()
    {
        GameObject aObj = new GameObject("AudioCinematica");
        aObj.transform.SetParent(transform, false);

        audioSourceMotor = aObj.AddComponent<AudioSource>();
        audioSourceMotor.loop = true;
        audioSourceMotor.playOnAwake = false;
        audioSourceMotor.clip = GenerarClipMotorDiesel();

        audioSourceFx = aObj.AddComponent<AudioSource>();
        audioSourceFx.loop = false;
        audioSourceFx.playOnAwake = false;

        audioSourceSirena = aObj.AddComponent<AudioSource>();
        audioSourceSirena.loop = true;
        audioSourceSirena.playOnAwake = false;
        audioSourceSirena.clip = GenerarClipSirenaAmbulancia();

        // Ambiente nocturno: viento + grillos + lluvia fina (loop sin costura)
        audioSourceAmbiente = aObj.AddComponent<AudioSource>();
        audioSourceAmbiente.loop = true;
        audioSourceAmbiente.playOnAwake = false;
        audioSourceAmbiente.volume = 0f;
        audioSourceAmbiente.clip = GenerarClipAmbienteNocturno();

        // Colchón musical de tensión (drone grave con batimiento)
        audioSourceMusica = aObj.AddComponent<AudioSource>();
        audioSourceMusica.loop = true;
        audioSourceMusica.playOnAwake = false;
        audioSourceMusica.volume = 0f;
        audioSourceMusica.clip = GenerarClipDroneTension();

        // Latido del corazón para la separación del alma
        audioSourceLatido = aObj.AddComponent<AudioSource>();
        audioSourceLatido.loop = true;
        audioSourceLatido.playOnAwake = false;
        audioSourceLatido.volume = 0f;
        audioSourceLatido.clip = GenerarClipLatido();
    }

    // Todas las frecuencias usan ciclos enteros por clip para que el loop no tenga "clic" en la costura.

    private AudioClip GenerarClipAmbienteNocturno()
    {
        const int rate = 22050;
        const float dur = 8f;
        int samples = (int)(rate * dur);
        float[] d = new float[samples];
        System.Random rng = new System.Random(1223);
        float lp = 0f, lp2 = 0f, hp = 0f, prev = 0f;
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / rate;
            float ruido = (float)(rng.NextDouble() * 2.0 - 1.0);

            // Viento: ruido pasa-bajos con modulación lenta (1/8 Hz y 3/8 Hz = ciclos enteros en 8 s)
            lp += (ruido - lp) * 0.02f;
            lp2 += (lp - lp2) * 0.05f;
            float mod = 0.55f + 0.30f * Mathf.Sin(2f * Mathf.PI * 0.125f * t) + 0.15f * Mathf.Sin(2f * Mathf.PI * 0.375f * t + 1.2f);
            float viento = lp2 * 9f * mod;

            // Lluvia fina: ruido pasa-altos muy tenue
            hp = 0.92f * (hp + ruido - prev);
            prev = ruido;
            float lluviaFina = hp * 0.05f;

            // Grillos: tono agudo pulsado (≈ 4.3 kHz con compuerta de 12 Hz y vibrato)
            float compuerta = Mathf.Clamp01(Mathf.Sin(2f * Mathf.PI * 12f * t) * 3f);
            float grupo = Mathf.Clamp01(Mathf.Sin(2f * Mathf.PI * 0.5f * t) * 2f + 0.3f);
            float grillos = Mathf.Sin(2f * Mathf.PI * 4300f * t) * compuerta * grupo * 0.025f;

            d[i] = Mathf.Clamp(viento * 0.45f + lluviaFina + grillos, -1f, 1f);
        }
        AudioClip c = AudioClip.Create("AmbienteNocturno", samples, 1, rate, false);
        c.SetData(d, 0);
        return c;
    }

    private AudioClip GenerarClipDroneTension()
    {
        const int rate = 22050;
        const float dur = 4f;
        int samples = (int)(rate * dur);
        float[] d = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / rate;
            // 55 Hz + 55.5 Hz (batimiento de 0.5 Hz) + quinta 82.5 Hz + armónico agudo muy tenue
            float s = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.45f
                    + Mathf.Sin(2f * Mathf.PI * 55.5f * t) * 0.35f
                    + Mathf.Sin(2f * Mathf.PI * 82.5f * t) * 0.20f
                    + Mathf.Sin(2f * Mathf.PI * 220f * t + Mathf.Sin(2f * Mathf.PI * 0.25f * t) * 2f) * 0.05f;
            d[i] = s * 0.5f;
        }
        AudioClip c = AudioClip.Create("DroneTension", samples, 1, rate, false);
        c.SetData(d, 0);
        return c;
    }

    private AudioClip GenerarClipLatido()
    {
        const int rate = 22050;
        int samples = rate; // 1 s = 60 BPM
        float[] d = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / rate;
            // "lub" a 0 s y "dub" a 0.28 s
            float lub = Mathf.Sin(2f * Mathf.PI * 52f * t) * Mathf.Exp(-t * 22f);
            float t2 = Mathf.Max(0f, t - 0.28f);
            float dub = t >= 0.28f ? Mathf.Sin(2f * Mathf.PI * 62f * t2) * Mathf.Exp(-t2 * 26f) * 0.75f : 0f;
            d[i] = (lub + dub) * 0.9f;
        }
        AudioClip c = AudioClip.Create("Latido", samples, 1, rate, false);
        c.SetData(d, 0);
        return c;
    }

    private AudioClip GenerarClipZumbidoOidos()
    {
        const int rate = 22050;
        const float dur = 3.2f;
        int samples = (int)(rate * dur);
        float[] d = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / rate;
            float env = Mathf.Clamp01(t * 20f) * Mathf.Exp(-t * 0.9f);
            d[i] = (Mathf.Sin(2f * Mathf.PI * 3800f * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * 3815f * t) * 0.4f) * env * 0.35f;
        }
        AudioClip c = AudioClip.Create("ZumbidoOidosCinematica", samples, 1, rate, false);
        c.SetData(d, 0);
        return c;
    }

    private void ReproducirAudioProcedural(TipoFx tipo, float volumen)
    {
        if (audioSourceFx == null) return;
        AudioClip c = null;

        switch (tipo)
        {
            case TipoFx.FrenosChillando: c = GenerarClipFrenos(); break;
            case TipoFx.DerrapeLlantas: c = GenerarClipDerrape(); break;
            case TipoFx.ChoqueMetalValla: c = GenerarClipChoqueMetal(); break;
            case TipoFx.GolpeFondoCanon: c = GenerarClipImpactoFondo(); break;
            case TipoFx.PuertasAmbulancia: c = GenerarClipPuertas(); break;
            case TipoFx.AlmaEterea: c = GenerarClipAlma(); break;
            case TipoFx.ZumbidoOidos: c = GenerarClipZumbidoOidos(); break;
        }

        if (c != null)
        {
            audioSourceFx.PlayOneShot(c, volumen);
        }
    }

    private AudioClip GenerarClipMotorDiesel()
    {
        int rate = 22050;
        int samples = rate * 2;
        float[] d = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / rate;
            float f1 = Mathf.Sin(2f * Mathf.PI * 48f * t) * 0.4f;
            float f2 = Mathf.Sin(2f * Mathf.PI * 96f * t) * 0.25f;
            float ruido = (Random.value * 2f - 1f) * 0.08f;
            d[i] = (f1 + f2 + ruido) * 0.7f;
        }
        AudioClip c = AudioClip.Create("MotorDiesel", samples, 1, rate, false);
        c.SetData(d, 0);
        return c;
    }

    private AudioClip GenerarClipSirenaAmbulancia()
    {
        int rate = 22050;
        int samples = rate * 3;
        float[] d = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / rate;
            float tono = Mathf.Sin(2f * Mathf.PI * 0.75f * t) > 0f ? 680f : 880f;
            float env = 0.5f + Mathf.Sin(2f * Mathf.PI * 1.5f * t) * 0.15f;
            d[i] = Mathf.Sin(2f * Mathf.PI * tono * t) * env * 0.35f;
        }
        AudioClip c = AudioClip.Create("SirenaAmbulancia", samples, 1, rate, false);
        c.SetData(d, 0);
        return c;
    }

    private AudioClip GenerarClipFrenos()
    {
        int rate = 22050;
        int samples = (int)(rate * 1.8f);
        float[] d = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / rate;
            float env = Mathf.Clamp01(1f - (t / 1.8f));
            float freq = 2800f + Mathf.Sin(t * 30f) * 450f;
            float s = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.5f;
            float ruido = (Random.value * 2f - 1f) * 0.2f;
            d[i] = (s + ruido) * env * 0.45f;
        }
        AudioClip c = AudioClip.Create("ChillidoFrenos", samples, 1, rate, false);
        c.SetData(d, 0);
        return c;
    }

    private AudioClip GenerarClipDerrape()
    {
        int rate = 22050;
        int samples = (int)(rate * 1.6f);
        float[] d = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / rate;
            float env = Mathf.Sin((t / 1.6f) * Mathf.PI);
            float ruido = (Random.value * 2f - 1f) * 0.4f;
            float tono = Mathf.Sin(2f * Mathf.PI * (1200f - t * 300f) * t) * 0.3f;
            d[i] = (ruido + tono) * env * 0.5f;
        }
        AudioClip c = AudioClip.Create("DerrapeLlantas", samples, 1, rate, false);
        c.SetData(d, 0);
        return c;
    }

    private AudioClip GenerarClipChoqueMetal()
    {
        int rate = 22050;
        int samples = (int)(rate * 1.5f);
        float[] d = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / rate;
            float env = Mathf.Exp(-t * 4.5f);
            float ruido = (Random.value * 2f - 1f) * 0.6f;
            float metal = Mathf.Sin(2f * Mathf.PI * 420f * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.5f;
            d[i] = (ruido + metal) * env * 0.75f;
        }
        AudioClip c = AudioClip.Create("ChoqueMetal", samples, 1, rate, false);
        c.SetData(d, 0);
        return c;
    }

    private AudioClip GenerarClipImpactoFondo()
    {
        int rate = 22050;
        int samples = (int)(rate * 2.2f);
        float[] d = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / rate;
            float env = Mathf.Exp(-t * 2.8f);
            float sub = Mathf.Sin(2f * Mathf.PI * 65f * t) * 0.7f;
            float ruido = (Random.value * 2f - 1f) * 0.35f;
            d[i] = (sub + ruido) * env * 0.85f;
        }
        AudioClip c = AudioClip.Create("ImpactoFondo", samples, 1, rate, false);
        c.SetData(d, 0);
        return c;
    }

    private AudioClip GenerarClipPuertas()
    {
        int rate = 22050;
        int samples = (int)(rate * 0.6f);
        float[] d = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / rate;
            float env = Mathf.Exp(-t * 12f);
            float click = Mathf.Sin(2f * Mathf.PI * 340f * t) * 0.6f + (Random.value * 2f - 1f) * 0.4f;
            d[i] = click * env * 0.6f;
        }
        AudioClip c = AudioClip.Create("PuertasAmbulancia", samples, 1, rate, false);
        c.SetData(d, 0);
        return c;
    }

    private AudioClip GenerarClipAlma()
    {
        int rate = 22050;
        int samples = rate * 3;
        float[] d = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / rate;
            float env = Mathf.Sin((t / 3f) * Mathf.PI);
            float f1 = Mathf.Sin(2f * Mathf.PI * 220f * t) * 0.4f;
            float f2 = Mathf.Sin(2f * Mathf.PI * 440f * t) * 0.3f;
            float f3 = Mathf.Sin(2f * Mathf.PI * 660f * t) * 0.15f;
            d[i] = (f1 + f2 + f3) * env * 0.4f;
        }
        AudioClip c = AudioClip.Create("AlmaEterea", samples, 1, rate, false);
        c.SetData(d, 0);
        return c;
    }
}
