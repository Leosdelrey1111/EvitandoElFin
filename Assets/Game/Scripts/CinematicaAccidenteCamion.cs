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

    [Header("Siguiente Escena")]
    public string escenaNivel1 = "Nivel1_Limbo";

    // Overlay Cinemático (UI)
    private CanvasGroup fadeCanvas;
    private CanvasGroup flashCanvas;
    private RectTransform barraSup;
    private RectTransform barraInf;
    private TextMeshProUGUI textoSubtitulos;
    private Coroutine rutinaSub;

    // Audio Procedural Integrado
    private AudioSource audioSourceMotor;
    private AudioSource audioSourceFx;
    private AudioSource audioSourceSirena;

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
    }

    private void DesactivarUIResidual()
    {
        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
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

        yield return new WaitForSeconds(0.4f);

        // Entrada panorámica con barras negras
        StartCoroutine(AnimarBarras(110f, 1.2f));
        MostrarSub("<size=120%><b>CARRETERA DE GUANAJUATO</b></size>\n<color=#BBBBBB>03:12 AM — RUTA DE LA SIERRA</color>", 3.4f);

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
            float vel = Mathf.Lerp(0.24f, velocidadAvance, aceleracion);
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
                MostrarSub("<color=#FF4444><b>¡LOS FRENOS NO RESPONDEN!</b></color>\n<size=85%><i>El pedal se fue hasta el fondo… ¡la curva está encima!</i></size>", 3.0f);
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

        // Sonido de impacto violento
        ReproducirAudioProcedural(TipoFx.ChoqueMetalValla, 1.0f);
        if (audioSourceMotor != null) audioSourceMotor.Stop();

        // Expulsar fragmentos de valla metálica destrozada
        LanzarFragmentosValla(puntoImpacto, ultimoFwd, ultimoRight);
        CrearVallaRota(puntoImpacto, ultimoFwd, ultimoRight);

        // Posicionar cámara al borde de la carretera observando la caída al precipicio
        Vector3 camBorde = puntoImpacto - ultimoRight * 7.5f - ultimoFwd * 5.0f + Vector3.up * 3.5f;
        if (camaraPrincipal != null) camaraPrincipal.transform.position = camBorde;

        Time.timeScale = 0.20f; // Cámara lenta cinematográfica

        Vector3 velCaida = ultimoFwd * 12.5f + ultimoRight * 10.5f + Vector3.up * 2.8f;
        Vector3 rotCaida = new Vector3(85f, 40f, 65f);
        float tiempoCaida = 0f;
        float gravedad = 26f;
        bool tocoFondo = false;

        while (tiempoCaida < 3.8f && !tocoFondo)
        {
            float dt = Time.deltaTime;
            tiempoCaida += dt;

            if (tiempoCaida > 0.15f)
            {
                Time.timeScale = Mathf.MoveTowards(Time.timeScale, 1f, Time.unscaledDeltaTime * 0.9f);
            }
            shakeCamara = Mathf.MoveTowards(shakeCamara, 0.1f, Time.unscaledDeltaTime * 0.7f);

            velCaida.y -= gravedad * dt;
            if (camionTransform != null)
            {
                camionTransform.position += velCaida * dt;
                camionTransform.Rotate(rotCaida * dt, Space.Self);
            }

            PosicionarCamara(camBorde, camionTransform.position + Vector3.up * 0.5f, 50f);

            if (tiempoCaida > 0.40f)
            {
                RaycastHit hit;
                if (Physics.Raycast(camionTransform.position + Vector3.up * 0.5f, Vector3.down, out hit, 2.8f, ~0, QueryTriggerInteraction.Ignore))
                {
                    tocoFondo = true;
                }
            }
            yield return null;
        }

        // Impacto final en el fondo del valle
        Time.timeScale = 1f;
        StartCoroutine(Flash(1f, 0.9f));
        shakeCamara = 1.3f;
        ReproducirAudioProcedural(TipoFx.GolpeFondoCanon, 1.0f);
        EncenderLucesFreno(false);

        yield return new WaitForSeconds(0.3f);
        shakeCamara = 0f;
        yield return new WaitForSeconds(0.5f);

        // Desvanecimiento suave a negro
        yield return Fade(fadeCanvas, 0f, 1f, 1.0f);
    }

    // ------------------------------------------------------------ FASE 3: RESCATE Y AMBULANCIA

    IEnumerator FaseRescate()
    {
        // La ambulancia se estaciona en el carril derecho (+3.2m) justo antes del accidente
        float sRescate = puntoAccidente - 0.20f;
        Vector3 posAmbulancia, fwdAmbulancia, upAmbulancia, rightAmbulancia;
        Quaternion rotAmbulancia;
        EvaluarRuta(sRescate, 3.2f, out posAmbulancia, out rotAmbulancia, out fwdAmbulancia, out upAmbulancia, out rightAmbulancia);

        // Borde de la carretera donde ocurrió la rotura de la valla
        Vector3 bordeVallaRota, fwdV, upV, rightV;
        Quaternion rotV;
        EvaluarRuta(puntoAccidente, 7.5f, out bordeVallaRota, out rotV, out fwdV, out upV, out rightV);

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

        // Crear camilla y paramédicos
        GameObject camilla = CrearCamilla();
        GameObject med1 = CrearParamedico("Paramedico_1");
        GameObject med2 = CrearParamedico("Paramedico_2");

        // Cámara de rescate encuadrando la ambulancia, el asfalto mojado y las sirenas
        Vector3 centroEscena = (posAmbulancia + bordeVallaRota) * 0.5f;
        Vector3 posCamRescate = centroEscena - rightAmbulancia * 6.5f - fwdAmbulancia * 1.5f + Vector3.up * 2.2f;
        Vector3 miraCamRescate = centroEscena + Vector3.up * 1.1f;

        if (camaraPrincipal != null)
        {
            camaraPrincipal.fieldOfView = 50f;
            camaraPrincipal.transform.position = posCamRescate;
            camaraPrincipal.transform.LookAt(miraCamRescate);
        }

        // Aparece la escena de emergencias
        yield return Fade(fadeCanvas, 1f, 0f, 1.2f);
        MostrarSub("<color=#FFEAA0><b>—¡Aquí está el chofer! ¡Pulso muy débil, súbanlo a la camilla rápido!</b></color>", 3.4f);

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

            camilla.transform.position = posPaso + Vector3.up * (0.85f + rebotePaso);
            camilla.transform.rotation = Quaternion.LookRotation(dirCamilla, Vector3.up);

            med1.transform.position = (posPaso + dirCamilla * 1.25f) + Vector3.up * rebotePaso;
            med1.transform.rotation = Quaternion.LookRotation(dirCamilla, Vector3.up);

            med2.transform.position = (posPaso - dirCamilla * 1.25f) + Vector3.up * rebotePaso;
            med2.transform.rotation = Quaternion.LookRotation(dirCamilla, Vector3.up);

            PosicionarCamara(posCamRescate + rightAmbulancia * Mathf.Sin(tCam * 0.5f) * 0.3f, miraCamRescate, 4f);
            yield return null;
        }

        MostrarSub("<color=#FF5555><b>—¡Código rojo! ¡A urgencias, no tenemos mucho tiempo!</b></color>", 2.4f);

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
        StartCoroutine(AnimarBarras(0f, 1.2f));
        ReproducirAudioProcedural(TipoFx.AlmaEterea, 0.7f);
        yield return new WaitForSeconds(1.2f);

        MostrarSub("<size=115%>Tu cuerpo va camino a urgencias…</size>", 3.4f);
        yield return new WaitForSeconds(4.0f);

        MostrarSub("<size=130%><color=#66FFE0><b>Pero tú sigues aquí.</b></color></size>", 3.6f);
        yield return new WaitForSeconds(4.2f);

        Time.timeScale = 1f;
        if (Application.CanStreamedLevelBeLoaded(escenaNivel1))
        {
            SceneManager.LoadScene(escenaNivel1);
        }
        else
        {
            Debug.LogError("[Cinemática] No se encontró la escena '" + escenaNivel1 + "' en Build Settings.");
        }
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

        Vector3 miraConShake = objetivoMirar;
        if (shakeCamara > 0.001f) miraConShake += Random.insideUnitSphere * shakeCamara * 0.45f;

        Vector3 dir = (miraConShake - ct.position).normalized;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion rotDeseada = Quaternion.LookRotation(dir, Vector3.up);
            ct.rotation = Quaternion.Slerp(ct.rotation, rotDeseada, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
        }

        if (shakeCamara > 0.001f) ct.position += Random.insideUnitSphere * shakeCamara * 0.15f;
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
        GameObject baseC = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseC.transform.SetParent(root.transform, false);
        baseC.transform.localScale = new Vector3(0.75f, 0.12f, 2.0f);
        baseC.GetComponent<MeshRenderer>().sharedMaterial = NuevoMaterial(new Color(0.85f, 0.88f, 0.9f));
        Destroy(baseC.GetComponent<Collider>());

        GameObject cuerpo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        cuerpo.transform.SetParent(root.transform, false);
        cuerpo.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        cuerpo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        cuerpo.transform.localScale = new Vector3(0.38f, 0.8f, 0.3f);
        cuerpo.GetComponent<MeshRenderer>().sharedMaterial = NuevoMaterial(new Color(0.9f, 0.85f, 0.7f));
        Destroy(cuerpo.GetComponent<Collider>());

        GameObject manta = GameObject.CreatePrimitive(PrimitiveType.Cube);
        manta.transform.SetParent(root.transform, false);
        manta.transform.localPosition = new Vector3(0f, 0.22f, -0.15f);
        manta.transform.localScale = new Vector3(0.7f, 0.1f, 1.1f);
        manta.GetComponent<MeshRenderer>().sharedMaterial = NuevoMaterial(new Color(0.8f, 0.82f, 0.9f));
        Destroy(manta.GetComponent<Collider>());

        return root;
    }

    GameObject CrearParamedico(string nombre)
    {
        GameObject root = new GameObject(nombre);
        GameObject cuerpo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        cuerpo.transform.SetParent(root.transform, false);
        cuerpo.transform.localPosition = new Vector3(0f, 0.95f, 0f);
        cuerpo.transform.localScale = new Vector3(0.55f, 0.95f, 0.55f);
        cuerpo.GetComponent<MeshRenderer>().sharedMaterial = NuevoMaterial(new Color(0.12f, 0.38f, 0.28f));
        Destroy(cuerpo.GetComponent<Collider>());

        GameObject cabeza = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cabeza.transform.SetParent(root.transform, false);
        cabeza.transform.localPosition = new Vector3(0f, 1.95f, 0f);
        cabeza.transform.localScale = Vector3.one * 0.32f;
        cabeza.GetComponent<MeshRenderer>().sharedMaterial = NuevoMaterial(new Color(0.8f, 0.62f, 0.5f));
        Destroy(cabeza.GetComponent<Collider>());

        return root;
    }

    Material NuevoMaterial(Color c)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        Material m = new Material(sh);
        m.color = c;
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

        flashCanvas = CrearPantallaCompleta("FlashBlanco", cgo.transform, Color.white);
        flashCanvas.alpha = 0f;

        barraSup = CrearBarraNegra("BarraSuperior", cgo.transform, true);
        barraInf = CrearBarraNegra("BarraInferior", cgo.transform, false);

        GameObject sgo = new GameObject("SubtitulosCinematica");
        sgo.transform.SetParent(cgo.transform, false);
        RectTransform srt = sgo.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.08f, 0f);
        srt.anchorMax = new Vector2(0.92f, 0f);
        srt.pivot = new Vector2(0.5f, 0f);
        srt.anchoredPosition = new Vector2(0f, 130f);
        srt.sizeDelta = new Vector2(0f, 130f);

        textoSubtitulos = sgo.AddComponent<TextMeshProUGUI>();
        textoSubtitulos.alignment = TextAlignmentOptions.Center;
        textoSubtitulos.fontSize = 34;
        textoSubtitulos.color = new Color(1f, 1f, 1f, 0f);
        textoSubtitulos.raycastTarget = false;

        fadeCanvas = CrearPantallaCompleta("FadeNegro", cgo.transform, Color.black);
        fadeCanvas.alpha = 1f;
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

    void MostrarSub(string texto, float duracion)
    {
        if (rutinaSub != null) StopCoroutine(rutinaSub);
        rutinaSub = StartCoroutine(RutinaSub(texto, duracion));
    }

    IEnumerator RutinaSub(string texto, float duracion)
    {
        textoSubtitulos.text = texto;
        textoSubtitulos.maxVisibleCharacters = 0;
        textoSubtitulos.color = Color.white;
        textoSubtitulos.ForceMeshUpdate();

        int total = textoSubtitulos.textInfo.characterCount;
        float t = 0f;
        while (textoSubtitulos.maxVisibleCharacters < total)
        {
            t += Time.unscaledDeltaTime;
            textoSubtitulos.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(t * 38f));
            yield return null;
        }

        yield return new WaitForSecondsRealtime(duracion);

        float a = 1f;
        while (a > 0f)
        {
            a -= Time.unscaledDeltaTime * 2.2f;
            Color c = textoSubtitulos.color;
            c.a = Mathf.Max(0f, a);
            textoSubtitulos.color = c;
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
        AlmaEterea
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
