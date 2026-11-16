using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// Secuencia cinemática de "despertar y levantarse" del Chofer como alma en el Limbo (inicio del Nivel 1).
/// Todo se construye en runtime (overlay, barras de cine, viñeta, iluminación de escaparate, zumbido de oídos).
/// Orden:
///  1. Toma a ras de suelo con parpadeos: el Chofer se ve tirado en el suelo del claro inicial.
///  2. Toma en órbita ascendente: el Chofer apoya los brazos, se incorpora y se levanta del suelo en 3ra persona.
///  3. Enlaza de inmediato con la cinemática narrativa "intro" de Tonalli (sincronizada con voz y texto).
/// </summary>
[DefaultExecutionOrder(50)]
public class DespertarConfusion : MonoBehaviour
{
    [Header("Referencias (se buscan solas si están vacías)")]
    public ChoferController chofer;
    public CamaraTerceraPersona camara;

    [Header("Tiempos")]
    public float duracionNegro = 1.1f;
    public float duracionParpadeos = 3.2f;
    public float duracionLevantarse = 3.4f;
    public float tiempoRecuperacionMareo = 10f;

    [Header("Intensidad del mareo")]
    public float rollMaximo = 7f;
    public float fovMaximo = 7f;

    [Header("Textos")]
    [TextArea] public string textoInicial = "…un zumbido agudo… no se oye nada más…";
    [TextArea] public string textoDespertar = "¿Qué… pasó? ¿Dónde estoy?\nMe duele la cabeza…";
    [TextArea] public string textoAlma = "Veo mi cuerpo en la ambulancia… pero estoy aquí.\nEsto no es mi cuerpo… es mi alma.";
    [TextArea] public string textoControles = "Muévete con W A S D";
    [TextArea] public string textoGuia = "Sígueme… los tótems brillan entre los árboles.\nReúne tres y el portal te mostrará el camino.";
    [TextArea] public string textoCombate = "Click izquierdo: golpear (combo)   ·   Q: onda espiritual   ·   Espacio: saltar   ·   Ctrl / C: esquivar";

    private CanvasGroup fade;
    private Image vignette;
    private RectTransform barraSup, barraInf;
    private TextMeshProUGUI subtitulo;
    private AudioSource audioZumbido;
    private float confusionActual = 1f;
    private float alphaViñetaBase = 0.85f;
    private bool tieneAnimDespertar = false;
    private float distanciaGuardada = 5f;
    private TextMeshProUGUI pistaSaltar;

    private GameObject luzEscaparate;
    private bool camaraCinematicaActiva = false;
    private float barraK = 0f;

    void Awake()
    {
        if (chofer == null) chofer = Object.FindAnyObjectByType<ChoferController>();
        if (camara == null)
        {
            Camera cam = Camera.main;
            if (cam != null) camara = cam.GetComponent<CamaraTerceraPersona>();
        }
        ConstruirOverlay();
    }

    void Start()
    {
        if (textoCombate != null) textoCombate = textoCombate.Replace("Espacio: esquivar", "Espacio: saltar   ·   Ctrl / C: esquivar");
        if (chofer != null)
        {
            chofer.controlBloqueado = true;
            chofer.confusion = 1f;
            AgregarLuzAlma(chofer.gameObject);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Time.timeScale = 1f;

        distanciaGuardada = camara != null ? camara.distancia : 5f;
        EstadoNivel.Cambiar(EstadoDeNivel.Intro);
        PonerChoferEnElSuelo();
        StartCoroutine(Secuencia());
    }

    void Update()
    {
        if (pistaSaltar != null) pistaSaltar.enabled = EstadoNivel.Actual == EstadoDeNivel.Intro;

        // Barras de cine suaves durante el despertar
        float objetivoBarra = (EstadoNivel.Actual == EstadoDeNivel.Intro && camaraCinematicaActiva) ? 1f : 0f;
        barraK = Mathf.MoveTowards(barraK, objetivoBarra, Time.unscaledDeltaTime / 0.5f);
        if (barraSup != null && barraInf != null)
        {
            float h = Mathf.SmoothStep(0f, 1f, barraK) * 110f;
            barraSup.sizeDelta = new Vector2(0f, h);
            barraInf.sizeDelta = new Vector2(0f, h);
        }

        // Intro saltable (Issue #10): ENTER salta la secuencia de despertar
        if (EstadoNivel.Actual == EstadoDeNivel.Intro && Time.timeScale > 0.01f &&
            (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            SaltarIntro();
        }
    }

    void SaltarIntro()
    {
        StopAllCoroutines();
        if (rutinaTexto != null) rutinaTexto = null;

        LevantarChofer();
        if (chofer != null)
        {
            if (chofer.animator != null) chofer.animator.speed = 1f;
            chofer.ReproducirEstado("Locomocion", 0.08f);
        }
        LiberarCamaraCinematica();

        if (camara != null)
        {
            camara.distancia = distanciaGuardada;
            camara.desplazamientoObjetivo = new Vector3(0f, 1.4f, 0f);
            camara.rollExtra = 0f;
            camara.fovExtra = 0f;
        }
        fade.alpha = 0f;
        confusionActual = 0f;
        if (vignette != null) vignette.color = new Color(0f, 0f, 0f, 0f);
        if (audioZumbido != null) audioZumbido.Stop();
        subtitulo.color = new Color(subtitulo.color.r, subtitulo.color.g, subtitulo.color.b, 0f);

        if (chofer != null) { chofer.confusion = 0f; chofer.controlBloqueado = false; }
        EstadoNivel.Cambiar(EstadoDeNivel.Jugando);

        StartCoroutine(TrasSaltarIntro());
    }

    IEnumerator TrasSaltarIntro()
    {
        if (GuiaAlebrije.Instancia != null)
        {
            GuiaAlebrije.Instancia.ContarHistoria("intro", true);
            yield return new WaitForSeconds(1.0f);
            while (GuiaAlebrije.Instancia != null && GuiaAlebrije.Instancia.Ocupado)
                yield return null;
        }
        MostrarTexto(textoControles + "   ·   " + textoCombate, 0.2f);
    }

    void OnDisable()
    {
        if (chofer != null && chofer.animator != null) chofer.animator.speed = 1f;
        LiberarCamaraCinematica();
    }

    /// <summary>
    /// Usa la animación "Despertar" ya configurada en el Animator: empieza en el fotograma 0 (acostado en el suelo, congelado)
    /// y se levanta cuando arranca la toma de levantarse.
    /// </summary>
    void PonerChoferEnElSuelo()
    {
        tieneAnimDespertar = false;
        if (chofer == null) return;

        // Luz cálida/espiritual sobre el Chofer para que se vea claramente acostado en la noche
        if (luzEscaparate == null)
        {
            luzEscaparate = new GameObject("LuzDespertarCine");
            luzEscaparate.transform.position = chofer.transform.position + Vector3.up * 4.2f - chofer.transform.forward * 1.5f;
            Light l = luzEscaparate.AddComponent<Light>();
            l.type = LightType.Spot;
            l.spotAngle = 78f;
            l.color = new Color(0.75f, 0.92f, 1f);
            l.intensity = 10f;
            l.range = 14f;
            l.shadows = LightShadows.None;
            luzEscaparate.transform.rotation = Quaternion.LookRotation(chofer.transform.position - luzEscaparate.transform.position);
        }

        if (chofer.animator == null || chofer.animator.runtimeAnimatorController == null) return;
        if (!chofer.ReproducirEstado("Despertar", 0f)) return;

        tieneAnimDespertar = true;
        chofer.animator.Update(0f);   // aplica la pose inicial (acostado) de inmediato
        chofer.animator.speed = 0f;   // se queda acostado hasta que toque levantarse
    }

    void LevantarChofer()
    {
        if (!tieneAnimDespertar || chofer == null || chofer.animator == null) return;
        chofer.animator.speed = 1f;   // reproduce el resto de su animación "Despertar" y al terminar pasa a Locomocion
    }

    void LiberarCamaraCinematica()
    {
        camaraCinematicaActiva = false;
        if (camara != null) camara.enabled = true;
        if (luzEscaparate != null) { Destroy(luzEscaparate); luzEscaparate = null; }
        if (barraSup != null) barraSup.sizeDelta = Vector2.zero;
        if (barraInf != null) barraInf.sizeDelta = Vector2.zero;
    }

    void LateUpdate()
    {
        // Mareo continuo: inclinación + respiración de FOV + viñeta pulsante
        if (camara != null && camara.enabled)
        {
            camara.rollExtra = Mathf.Sin(Time.time * 1.7f) * rollMaximo * confusionActual
                             + Mathf.Sin(Time.time * 0.9f + 1.3f) * (rollMaximo * 0.5f) * confusionActual;
            camara.fovExtra = (Mathf.Sin(Time.time * 2.1f) * 0.5f + 0.5f) * fovMaximo * confusionActual;
        }

        if (vignette != null)
        {
            float pulso = 0.8f + Mathf.Sin(Time.time * 2.6f) * 0.2f;
            Color c = vignette.color;
            c.a = Mathf.Clamp01(alphaViñetaBase * confusionActual * pulso);
            vignette.color = c;
        }

        if (chofer != null) chofer.confusion = confusionActual;

        if (audioZumbido != null)
            audioZumbido.volume = Mathf.Lerp(0f, 0.16f, confusionActual);
    }

    static float AlturaSuelo(Vector3 p)
    {
        Terrain t = Terrain.activeTerrain;
        return t != null ? t.SampleHeight(p) + t.transform.position.y : p.y;
    }

    IEnumerator Secuencia()
    {
        fade.alpha = 1f;
        confusionActual = 1f;

        Camera cam = Camera.main;
        if (camara != null) camara.enabled = false;
        camaraCinematicaActiva = true;

        Vector3 posChofer = chofer != null ? chofer.transform.position : Vector3.zero;
        Vector3 fwd = chofer != null ? chofer.transform.forward : Vector3.forward;
        Vector3 right = chofer != null ? chofer.transform.right : Vector3.right;
        float sueloBase = AlturaSuelo(posChofer);

        // 1. Breve impacto en negro + zumbido
        MostrarTexto(textoInicial, 0.2f);
        yield return new WaitForSeconds(duracionNegro);

        // 2. TOMA 1 (3ra persona a ras de suelo): Se ve al Chofer tirado en el suelo entre las veladoras mientras parpadea
        MostrarTexto(textoDespertar, 0.3f);
        float t = 0f;
        while (t < duracionParpadeos)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duracionParpadeos);
            float e = Mathf.SmoothStep(0f, 1f, k);

            // Parpadeo que se abre hasta quedar totalmente nítido
            float apertura = Mathf.Clamp01(Mathf.Sin(t * (5.2f - 2.2f * k)) * 0.5f + 0.5f);
            float alphaFade = Mathf.Lerp(0.95f, 0f, k) * (1f - apertura * 0.75f * k);
            fade.alpha = k > 0.82f ? Mathf.Lerp(fade.alpha, 0f, (k - 0.82f) / 0.18f) : Mathf.Clamp01(alphaFade);

            if (cam != null)
            {
                // Cámara baja lateral-frontal enfocando al Chofer acostado en el suelo
                Vector3 camPos = posChofer + fwd * Mathf.Lerp(2.5f, 1.85f, e) + right * Mathf.Lerp(2.0f, 1.45f, e);
                camPos.y = Mathf.Max(AlturaSuelo(camPos) + 0.48f, sueloBase + Mathf.Lerp(0.6f, 0.8f, e));
                Vector3 mira = posChofer + Vector3.up * Mathf.Lerp(0.35f, 0.5f, e);
                float roll = Mathf.Sin(t * 2.1f) * 5.0f * (1f - k * 0.4f);
                cam.transform.SetPositionAndRotation(camPos, Quaternion.LookRotation(mira - camPos, Vector3.up) * Quaternion.Euler(0f, 0f, roll));
                cam.fieldOfView = Mathf.Lerp(54f, 46f, e);
            }
            yield return null;
        }
        fade.alpha = 0f;

        // 3. TOMA 2 (Cinemática de Levantarse): Reproduce su animación "Despertar" mientras la cámara lo enfoca y sube con él
        MostrarTexto(textoAlma, 0.2f);
        LevantarChofer();

        float tLev = 0f;
        bool esperandoAnim = tieneAnimDespertar && chofer != null && chofer.animator != null;
        while (tLev < duracionLevantarse || (esperandoAnim && tLev < 6.2f && chofer.animator.GetCurrentAnimatorStateInfo(0).IsName("Despertar")))
        {
            tLev += Time.deltaTime;
            float k = Mathf.Clamp01(tLev / duracionLevantarse);
            float e = Mathf.SmoothStep(0f, 1f, k);

            if (cam != null)
            {
                // Órbita cinematográfica que acompaña al Chofer desde el suelo hasta quedar detrás de su hombro
                float ang = Mathf.Lerp(0.65f, 2.85f, e);
                float dist = Mathf.Lerp(2.6f, distanciaGuardada * 0.88f, e);
                Vector3 offsetDir = (fwd * Mathf.Cos(ang) + right * Mathf.Sin(ang)).normalized;
                Vector3 camPos = posChofer + offsetDir * dist;
                float hCam = Mathf.Lerp(0.75f, 1.75f, e);
                camPos.y = Mathf.Max(AlturaSuelo(camPos) + 0.45f, sueloBase + hCam);

                Vector3 mira = posChofer + Vector3.up * Mathf.Lerp(0.5f, 1.42f, e);
                float roll = Mathf.Sin(tLev * 1.8f) * 3f * (1f - e);
                cam.transform.SetPositionAndRotation(camPos, Quaternion.LookRotation(mira - camPos, Vector3.up) * Quaternion.Euler(0f, 0f, roll));
                cam.fieldOfView = Mathf.Lerp(46f, 56f, e);
            }

            confusionActual = Mathf.Lerp(1f, 0.45f, e);
            yield return null;
        }

        LiberarCamaraCinematica();

        if (camara != null)
        {
            camara.distancia = distanciaGuardada;
            camara.desplazamientoObjetivo = new Vector3(0f, 1.4f, 0f);
        }

        // 4. Pasar a estado Jugando y disparar de inmediato la cinemática "intro" de Tonalli (sincronizada con voz y texto)
        if (chofer != null) chofer.controlBloqueado = false;
        if (EstadoNivel.Actual == EstadoDeNivel.Intro) EstadoNivel.Cambiar(EstadoDeNivel.Jugando);

        if (GuiaAlebrije.Instancia != null)
        {
            // Ocultar subtítulo local para no encimarse con la caja de diálogo de Tonalli
            SetAlphaTexto(0f);
            GuiaAlebrije.Instancia.ContarHistoria("intro", true);
            StartCoroutine(MostrarControlesTrasIntro());
        }
        else
        {
            MostrarTexto(textoControles, 0.3f);
            StartCoroutine(MensajesGuia());
        }

        // Disipar el mareo restante suavemente
        float r = 0f;
        float confIni = confusionActual;
        while (r < tiempoRecuperacionMareo)
        {
            r += Time.deltaTime;
            confusionActual = Mathf.SmoothStep(confIni, 0f, r / tiempoRecuperacionMareo);
            yield return null;
        }

        confusionActual = 0f;
        if (camara != null) { camara.rollExtra = 0f; camara.fovExtra = 0f; }
        if (chofer != null) chofer.confusion = 0f;
        if (audioZumbido != null) audioZumbido.Stop();
        if (vignette != null) vignette.color = new Color(0f, 0f, 0f, 0f);
    }

    IEnumerator MostrarControlesTrasIntro()
    {
        yield return new WaitForSeconds(0.5f);
        while (CinematicaNarrativa.Activa || (GuiaAlebrije.Instancia != null && GuiaAlebrije.Instancia.Ocupado))
            yield return null;
        MostrarTexto(textoControles + "   ·   " + textoCombate, 0.25f);
    }

    // ---------------------------------------------------------------- UI runtime

    void ConstruirOverlay()
    {
        GameObject cgo = new GameObject("Overlay_Despertar");
        Canvas canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;
        CanvasScaler sc = cgo.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);

        // Viñeta borrosa (oscuridad en los bordes de la visión)
        GameObject vgo = new GameObject("Viñeta");
        vgo.transform.SetParent(cgo.transform, false);
        vignette = vgo.AddComponent<Image>();
        vignette.sprite = CrearSpriteViñeta();
        vignette.color = new Color(0.02f, 0.05f, 0.08f, 0f);
        vignette.raycastTarget = false;
        Estirar(vgo.GetComponent<RectTransform>());

        // Barras de cine durante el despertar
        barraSup = CrearBarraCine("BarraCineSup", cgo.transform, true);
        barraInf = CrearBarraCine("BarraCineInf", cgo.transform, false);

        // Subtítulo
        GameObject sgo = new GameObject("Subtitulo");
        sgo.transform.SetParent(cgo.transform, false);
        RectTransform srt = sgo.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.1f, 0f);
        srt.anchorMax = new Vector2(0.9f, 0f);
        srt.pivot = new Vector2(0.5f, 0f);
        srt.anchoredPosition = new Vector2(0f, 115f);
        srt.sizeDelta = new Vector2(0f, 150f);
        subtitulo = sgo.AddComponent<TextMeshProUGUI>();
        subtitulo.alignment = TextAlignmentOptions.Center;
        subtitulo.fontSize = 34;
        subtitulo.fontStyle = FontStyles.Italic;
        subtitulo.color = new Color(0.85f, 0.95f, 1f, 0f);
        subtitulo.raycastTarget = false;

        // Fade negro (encima de todo)
        GameObject fgo = new GameObject("FadeNegro");
        fgo.transform.SetParent(cgo.transform, false);
        Image img = fgo.AddComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false;
        fade = fgo.AddComponent<CanvasGroup>();
        fade.alpha = 1f;
        fade.blocksRaycasts = false;
        Estirar(fgo.GetComponent<RectTransform>());

        // Pista para saltar la introducción (encima del fade negro)
        GameObject pgo = new GameObject("PistaSaltar");
        pgo.transform.SetParent(cgo.transform, false);
        RectTransform prt = pgo.AddComponent<RectTransform>();
        prt.anchorMin = prt.anchorMax = new Vector2(1f, 0f);
        prt.pivot = new Vector2(1f, 0f);
        prt.anchoredPosition = new Vector2(-36f, 28f);
        prt.sizeDelta = new Vector2(360f, 30f);
        pistaSaltar = pgo.AddComponent<TextMeshProUGUI>();
        pistaSaltar.text = "ENTER  ·  saltar introducción";
        pistaSaltar.fontSize = 18;
        pistaSaltar.alignment = TextAlignmentOptions.Right;
        pistaSaltar.color = new Color(1f, 1f, 1f, 0.65f);
        pistaSaltar.raycastTarget = false;

        // Zumbido de oídos procedural (tono agudo que se va apagando con el mareo)
        audioZumbido = cgo.AddComponent<AudioSource>();
        audioZumbido.clip = CrearZumbido();
        audioZumbido.loop = true;
        audioZumbido.spatialBlend = 0f;
        audioZumbido.volume = 0f;
        audioZumbido.Play();
    }

    RectTransform CrearBarraCine(string nombre, Transform padre, bool arriba)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, arriba ? 1f : 0f);
        rt.anchorMax = new Vector2(1f, arriba ? 1f : 0f);
        rt.pivot = new Vector2(0.5f, arriba ? 1f : 0f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        Image im = go.AddComponent<Image>();
        im.color = new Color(0.01f, 0.01f, 0.03f, 1f);
        im.raycastTarget = false;
        return rt;
    }

    void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    Sprite CrearSpriteViñeta()
    {
        int n = 256;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float dx = (x / (n - 1f) - 0.5f) * 2f;
                float dy = (y / (n - 1f) - 0.5f) * 2f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.SmoothStep(0.30f, 1.15f, d);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }

    AudioClip CrearZumbido()
    {
        int rate = 44100;
        int samples = rate * 2;
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)rate;
            data[i] = (Mathf.Sin(2f * Mathf.PI * 3800f * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * 3815f * t) * 0.4f) * 0.5f;
        }
        AudioClip clip = AudioClip.Create("ZumbidoOidos", samples, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private Coroutine rutinaTexto;

    void MostrarTexto(string texto, float retraso)
    {
        if (rutinaTexto != null) StopCoroutine(rutinaTexto);
        rutinaTexto = StartCoroutine(RutinaTexto(texto, retraso));
    }

    IEnumerator MensajesGuia()
    {
        yield return new WaitForSeconds(6.0f);
        MostrarTexto(textoGuia, 0.2f);
        yield return new WaitForSeconds(7.0f);
        MostrarTexto(textoCombate, 0.2f);
    }

    IEnumerator RutinaTexto(string texto, float retraso)
    {
        float a = subtitulo.color.a;
        while (a > 0f)
        {
            a -= Time.deltaTime * 4f;
            SetAlphaTexto(Mathf.Max(0f, a));
            yield return null;
        }

        if (retraso > 0f) yield return new WaitForSeconds(retraso);

        subtitulo.text = texto;
        subtitulo.maxVisibleCharacters = 0;
        SetAlphaTexto(1f);
        subtitulo.ForceMeshUpdate();
        int total = subtitulo.textInfo.characterCount;
        float t = 0f;
        while (subtitulo.maxVisibleCharacters < total)
        {
            t += Time.deltaTime;
            subtitulo.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(t * 42f));
            yield return null;
        }

        yield return new WaitForSeconds(2.5f);
        a = 1f;
        while (a > 0f)
        {
            a -= Time.deltaTime * 1.8f;
            SetAlphaTexto(Mathf.Max(0f, a));
            yield return null;
        }
    }

    void SetAlphaTexto(float a)
    {
        Color c = subtitulo.color;
        c.a = a;
        subtitulo.color = c;
    }

    void AgregarLuzAlma(GameObject player)
    {
        if (player.transform.Find("LuzAlmaEspiritual") != null) return;
        GameObject la = new GameObject("LuzAlmaEspiritual");
        la.transform.SetParent(player.transform, false);
        la.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        Light l = la.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(0.35f, 0.9f, 1f);
        l.range = 6f;
        l.intensity = 2.2f;
    }
}
