using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// Secuencia de "despertar" del Chofer como alma en el Limbo (inicio del Nivel 1).
/// Todo se construye en runtime (overlay, viñeta, zumbido de oídos), no necesita configuración.
/// Orden: pantalla negra -> zumbido -> parpadeos borrosos -> mareo que se disipa -> control devuelto.
/// </summary>
[DefaultExecutionOrder(50)]
public class DespertarConfusion : MonoBehaviour
{
    [Header("Referencias (se buscan solas si están vacías)")]
    public ChoferController chofer;
    public CamaraTerceraPersona camara;

    [Header("Tiempos")]
    public float duracionNegro = 2.0f;
    public float duracionParpadeos = 4.5f;
    public float tiempoRecuperacionMareo = 14f;

    [Header("Intensidad del mareo")]
    public float rollMaximo = 9f;
    public float fovMaximo = 9f;

    [Header("Textos")]
    [TextArea] public string textoInicial = "…un zumbido agudo… no se oye nada más…";
    [TextArea] public string textoDespertar = "¿Qué… pasó? ¿Dónde estoy?\nMe duele la cabeza…";
    [TextArea] public string textoAlma = "Veo mi cuerpo en la ambulancia… pero estoy aquí.\nEsto no es mi cuerpo… es mi alma.";
    [TextArea] public string textoControles = "Muévete con W A S D";

    private CanvasGroup fade;
    private Image vignette;
    private TextMeshProUGUI subtitulo;
    private AudioSource audioZumbido;
    private float confusionActual = 1f;
    private float alphaViñetaBase = 0.9f;

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
        if (chofer != null)
        {
            chofer.controlBloqueado = true;
            chofer.confusion = 1f;
            AgregarLuzAlma(chofer.gameObject);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Time.timeScale = 1f;

        StartCoroutine(Secuencia());
    }

    void LateUpdate()
    {
        // Mareo continuo: inclinación + respiración de FOV + viñeta pulsante
        if (camara != null)
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
            audioZumbido.volume = Mathf.Lerp(0f, 0.18f, confusionActual);
    }

    IEnumerator Secuencia()
    {
        fade.alpha = 1f;
        confusionActual = 1f;

        // 1. Negro + zumbido
        MostrarTexto(textoInicial, 1f);
        yield return new WaitForSeconds(duracionNegro);

        // 2. Parpadeos en PRIMERA PERSONA (desde los ojos del Chofer tirado/despertando)
        MostrarTexto(textoDespertar, 0.6f);

        // Guardar distancia original de la cámara y pasarla a 1ra persona (distancia = 0f)
        float distanciaOriginal = 5.0f;
        if (camara != null)
        {
            distanciaOriginal = camara.distancia;
            camara.distancia = 0.05f; // Pegada a los ojos del Chofer (1ra persona)
            camara.desplazamientoObjetivo = new Vector3(0f, 1.6f, 0.1f);
        }

        float t = 0f;
        while (t < duracionParpadeos)
        {
            t += Time.deltaTime;
            float k = t / duracionParpadeos;
            // Ojos que se abren y se cierran con cada vez más apertura
            float apertura = Mathf.Clamp01(Mathf.Sin(t * (5.5f - 2.5f * k)) * 0.5f + 0.5f);
            float alphaFade = Mathf.Lerp(1f, 0.0f, k * 0.85f) * (1f - apertura * 0.7f * k);
            fade.alpha = Mathf.Clamp01(alphaFade + (1f - k) * 0.25f);
            yield return null;
        }
        fade.alpha = 0f;

        MostrarTexto(textoAlma, 1.5f);
        yield return new WaitForSeconds(2.0f);

        // 3. Transición de Primera Persona a Tercera Persona (Zoom out revelando el cuerpo del alma)
        float tZoom = 0f;
        float duracionZoomOut = 3.5f;
        while (tZoom < duracionZoomOut)
        {
            tZoom += Time.deltaTime;
            float kZoom = Mathf.SmoothStep(0f, 1f, tZoom / duracionZoomOut);
            if (camara != null)
            {
                camara.distancia = Mathf.Lerp(0.05f, distanciaOriginal, kZoom);
                camara.desplazamientoObjetivo = Vector3.Lerp(new Vector3(0f, 1.6f, 0.1f), new Vector3(0f, 1.4f, 0f), kZoom);
            }
            yield return null;
        }

        if (camara != null) camara.distancia = distanciaOriginal;

        // 4. Devolver el control, todavía aturdido; el mareo se disipa poco a poco
        if (chofer != null) chofer.controlBloqueado = false;
        yield return new WaitForSeconds(1.5f);
        MostrarTexto(textoControles, 4f);

        float r = 0f;
        while (r < tiempoRecuperacionMareo)
        {
            r += Time.deltaTime;
            confusionActual = Mathf.SmoothStep(1f, 0f, r / tiempoRecuperacionMareo);
            yield return null;
        }

        confusionActual = 0f;
        if (camara != null) { camara.rollExtra = 0f; camara.fovExtra = 0f; }
        if (chofer != null) chofer.confusion = 0f;
        if (audioZumbido != null) audioZumbido.Stop();
        if (vignette != null) vignette.color = new Color(0f, 0f, 0f, 0f);
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

        // Subtítulo
        GameObject sgo = new GameObject("Subtitulo");
        sgo.transform.SetParent(cgo.transform, false);
        RectTransform srt = sgo.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.1f, 0f);
        srt.anchorMax = new Vector2(0.9f, 0f);
        srt.pivot = new Vector2(0.5f, 0f);
        srt.anchoredPosition = new Vector2(0f, 90f);
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

        // Zumbido de oídos procedural (tono agudo que se va apagando con el mareo)
        audioZumbido = cgo.AddComponent<AudioSource>();
        audioZumbido.clip = CrearZumbido();
        audioZumbido.loop = true;
        audioZumbido.spatialBlend = 0f;
        audioZumbido.volume = 0f;
        audioZumbido.Play();
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
            // Dos senoidales cercanas (batido) = zumbido de conmoción
            data[i] = (Mathf.Sin(2f * Mathf.PI * 3800f * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * 3815f * t) * 0.4f) * 0.5f;
        }
        AudioClip clip = AudioClip.Create("ZumbidoOidos", samples, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    void MostrarTexto(string texto, float retraso)
    {
        StopCoroutine("RutinaTexto");
        StartCoroutine(RutinaTexto(texto, retraso));
    }

    IEnumerator RutinaTexto(string texto, float retraso)
    {
        // Sale el texto anterior
        float a = subtitulo.color.a;
        while (a > 0f)
        {
            a -= Time.deltaTime * 3f;
            SetAlphaTexto(Mathf.Max(0f, a));
            yield return null;
        }

        yield return new WaitForSeconds(retraso);

        subtitulo.text = texto;
        subtitulo.maxVisibleCharacters = 0;
        SetAlphaTexto(1f);
        subtitulo.ForceMeshUpdate();
        int total = subtitulo.textInfo.characterCount;
        float t = 0f;
        while (subtitulo.maxVisibleCharacters < total)
        {
            t += Time.deltaTime;
            subtitulo.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(t * 38f));
            yield return null;
        }

        yield return new WaitForSeconds(2.8f);
        a = 1f;
        while (a > 0f)
        {
            a -= Time.deltaTime * 1.5f;
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
