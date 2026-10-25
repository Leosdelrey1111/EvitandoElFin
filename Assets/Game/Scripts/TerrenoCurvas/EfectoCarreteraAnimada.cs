using UnityEngine;

/// <summary>
/// Proporciona un efecto semi-animado místico sobre el asfalto de las Curvas de Guanajuato.
/// Hace que la doble línea y el asfalto pulsen con energía espiritual suave acorde al lore del juego.
/// </summary>
public class EfectoCarreteraAnimada : MonoBehaviour
{
    [Header("Pulso Espiritual de la Carretera")]
    public Color colorEmisionMin = new Color(0.12f, 0.08f, 0.01f);
    public Color colorEmisionMax = new Color(0.85f, 0.65f, 0.15f); // Brillo dorado/ámbar místico
    public float velocidadPulso = 1.8f;

    [Header("Respuesta al Chofer")]
    public bool reaccionarAlJugador = true;
    public float radioCercania = 12f;

    private Material materialInstancia;
    private Transform transformJugador;
    private Renderer rendererPista;

    void Start()
    {
        rendererPista = GetComponent<Renderer>();
        if (rendererPista != null)
        {
            // Instanciar el material para no alterar el asset original en disco
            materialInstancia = rendererPista.material;
            materialInstancia.EnableKeyword("_EMISSION");
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            transformJugador = player.transform;
        }
    }

    void Update()
    {
        if (materialInstancia == null) return;

        // Oscilación senoidal suave
        float ciclo = (Mathf.Sin(Time.time * velocidadPulso) + 1f) * 0.5f;

        // Si el Chofer está cerca o en combate, la carretera late con más intensidad espiritual
        float factorIntensidad = 1f;
        if (reaccionarAlJugador && transformJugador != null)
        {
            // Si el jugador está activo, el pulso es más vivaz
            factorIntensidad = 1.25f;
        }

        Color colorActual = Color.Lerp(colorEmisionMin, colorEmisionMax, ciclo) * factorIntensidad;
        materialInstancia.SetColor("_EmissionColor", colorActual);
    }

    void OnDestroy()
    {
        if (materialInstancia != null && Application.isPlaying)
        {
            Destroy(materialInstancia);
        }
    }
}
