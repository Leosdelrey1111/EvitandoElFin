using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDEspiritual : MonoBehaviour
{
    [Header("Referencias del Jugador")]
    public PoderEspiritual vidaChofer;
    public InventarioEspiritual inventarioChofer;
    public SistemaCombateChofer combateChofer;

    [Header("Elementos de la Interfaz")]
    public Slider sliderPoderEspiritual;
    public TextMeshProUGUI textoTotems;
    public TextMeshProUGUI textoAlmas;
    public TextMeshProUGUI textoRecuerdos;
    public TextMeshProUGUI textoObjetivo;

    [Header("Feedback de combate (opcional)")]
    [Tooltip("Mensajes breves en pantalla (tótem recogido, emboscada, etc.)")]
    public TextMeshProUGUI textoAviso;
    [Tooltip("Imagen a pantalla completa que parpadea en rojo al recibir daño.")]
    public Image destelloDanio;
    [Tooltip("Barra que se llena mientras se recarga la Onda Espiritual (Q).")]
    public Image rellenoOnda;
    public TextMeshProUGUI textoOnda;

    [Header("Suavizado")]
    public float velocidadBarra = 6f;

    private float valorObjetivoBarra;
    private float alphaDestello;
    private float avisoHasta;
    private float avisoAlpha;

    void Start()
    {
        // Buscar automáticamente referencias si no se asignaron en el Inspector
        if (vidaChofer == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                vidaChofer = player.GetComponent<PoderEspiritual>();
                inventarioChofer = player.GetComponent<InventarioEspiritual>();
                combateChofer = player.GetComponent<SistemaCombateChofer>();
            }
        }

        if (vidaChofer != null)
        {
            if (sliderPoderEspiritual != null)
            {
                sliderPoderEspiritual.maxValue = vidaChofer.maxPoder;
                sliderPoderEspiritual.value = vidaChofer.actualPoder;
                valorObjetivoBarra = vidaChofer.actualPoder;
            }
            vidaChofer.OnCambioPoder += ActualizarBarraVida;
            vidaChofer.OnRecibioDanio += AlRecibirDanio;
        }

        if (inventarioChofer != null)
        {
            ActualizarTextoTotems(inventarioChofer.cantidadTotems);
            ActualizarTextoAlmas(inventarioChofer.energiaEspiritual);
            inventarioChofer.OnCambioTotems += ActualizarTextoTotems;
            inventarioChofer.OnCambioEnergia += ActualizarTextoAlmas;
            inventarioChofer.OnRecuerdoObtenido += NotificarRecuerdo;
        }

        AvisosJuego.OnAviso += MostrarAviso;
        if (textoAviso != null) textoAviso.alpha = 0f;
        if (destelloDanio != null) { Color c = destelloDanio.color; c.a = 0f; destelloDanio.color = c; destelloDanio.raycastTarget = false; }

        // Texto de ayuda de controles actualizado (salto en Espacio, esquive en Ctrl/C)
        Transform ayuda = transform.Find("TextoControles");
        if (ayuda != null)
        {
            TextMeshProUGUI tmp = ayuda.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.text = "WASD mover  ·  Shift correr  ·  Espacio saltar  ·  Ctrl / C esquivar  ·  Click / F golpear  ·  Q onda espiritual  ·  Esc pausa";
        }

        ActualizarObjetivoUI();
    }

    void Update()
    {
        // Barra de vida con interpolación suave
        if (sliderPoderEspiritual != null)
        {
            sliderPoderEspiritual.value = Mathf.Lerp(sliderPoderEspiritual.value, valorObjetivoBarra, 1f - Mathf.Exp(-velocidadBarra * Time.unscaledDeltaTime));
            Image fill = sliderPoderEspiritual.fillRect != null ? sliderPoderEspiritual.fillRect.GetComponent<Image>() : null;
            if (fill != null && sliderPoderEspiritual.maxValue > 0f)
            {
                float p = sliderPoderEspiritual.value / sliderPoderEspiritual.maxValue;
                fill.color = Color.Lerp(new Color(0.9f, 0.2f, 0.2f), new Color(0.25f, 0.95f, 0.85f), Mathf.Clamp01(p * 1.4f));
            }
        }

        // Destello rojo de daño
        if (destelloDanio != null && alphaDestello > 0.001f)
        {
            alphaDestello = Mathf.MoveTowards(alphaDestello, 0f, 1.8f * Time.unscaledDeltaTime);
            Color c = destelloDanio.color; c.a = alphaDestello; destelloDanio.color = c;
        }

        // Aviso temporal
        if (textoAviso != null)
        {
            float objetivo = Time.unscaledTime < avisoHasta ? 1f : 0f;
            avisoAlpha = Mathf.MoveTowards(avisoAlpha, objetivo, 3f * Time.unscaledDeltaTime);
            textoAviso.alpha = avisoAlpha;
        }

        // Recarga de la Onda Espiritual
        if (combateChofer != null)
        {
            float p = combateChofer.ProgresoAtaqueArea;
            if (rellenoOnda != null) rellenoOnda.fillAmount = p;
            if (textoOnda != null) textoOnda.text = p >= 1f ? "Q  ONDA ESPIRITUAL  LISTA" : "Q  recargando…";
        }
    }

    void ActualizarObjetivoUI()
    {
        if (textoObjetivo != null && GameManager.Instancia != null)
        {
            int totemsReq = GameManager.Instancia.totemsRequeridos;
            int actual = inventarioChofer != null ? inventarioChofer.cantidadTotems : 0;
            if (actual >= totemsReq)
            {
                textoObjetivo.text = "<color=#00FFAA>¡Portal Desbloqueado! Ve a la salida.</color>";
            }
            else
            {
                textoObjetivo.text = $"Objetivo: Reúne los tótems ({actual}/{totemsReq})";
            }
        }
    }

    void ActualizarBarraVida(float nuevaVida)
    {
        valorObjetivoBarra = nuevaVida;
    }

    void AlRecibirDanio(float cantidad)
    {
        alphaDestello = Mathf.Clamp(0.25f + cantidad * 0.012f, 0.25f, 0.6f);
    }

    void ActualizarTextoTotems(int totalTotems)
    {
        if (textoTotems != null)
        {
            textoTotems.text = $"Tótems: {totalTotems}";
        }
        ActualizarObjetivoUI();
    }

    void ActualizarTextoAlmas(float energia)
    {
        if (textoAlmas != null)
        {
            textoAlmas.text = $"Almas: {Mathf.RoundToInt(energia)}";
        }
    }

    void MostrarAviso(string texto, float segundos)
    {
        if (textoAviso == null) return;
        textoAviso.text = texto;
        avisoHasta = Time.unscaledTime + segundos;
    }

    void NotificarRecuerdo(string nombreRecuerdo)
    {
        if (textoRecuerdos != null)
        {
            textoRecuerdos.text = $"Recuerdo: {nombreRecuerdo}";
        }
    }

    void OnDestroy()
    {
        AvisosJuego.OnAviso -= MostrarAviso;
        if (vidaChofer != null)
        {
            vidaChofer.OnCambioPoder -= ActualizarBarraVida;
            vidaChofer.OnRecibioDanio -= AlRecibirDanio;
        }
        if (inventarioChofer != null)
        {
            inventarioChofer.OnCambioTotems -= ActualizarTextoTotems;
            inventarioChofer.OnCambioEnergia -= ActualizarTextoAlmas;
            inventarioChofer.OnRecuerdoObtenido -= NotificarRecuerdo;
        }
    }
}
