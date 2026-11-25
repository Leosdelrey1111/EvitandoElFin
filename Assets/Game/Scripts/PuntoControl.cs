using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Altar de Ánimas / Punto de Control (Checkpoint) con temática mexicana (Issue #25).
/// - Se activa cuando el Chofer se acerca a la cruz o altar con veladoras.
/// - Enciende las veladoras con luz cálida de cempasúchil y partículas espirituales.
/// - Cura la salud del Chofer y guarda la posición y el progreso de la partida en disco.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PuntoControl : MonoBehaviour
{
    [Header("Identificación")]
    public string idCheckpoint = "Altar_Cruces_1";
    public string nombreAltar = "Altar de las Ánimas";

    [Header("Efectos")]
    public Light luzAltar;
    public Color colorInactivo = new Color(0.4f, 0.3f, 0.2f);
    public Color colorActivo = new Color(1f, 0.65f, 0.15f); // Dorado Cempasúchil
    public float intensidadActiva = 3f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip clipCampanada;

    private bool activado = false;
    private Collider triggerCol;

    void Awake()
    {
        triggerCol = GetComponent<Collider>();
        if (triggerCol != null) triggerCol.isTrigger = true;

        if (string.IsNullOrEmpty(idCheckpoint))
        {
            idCheckpoint = gameObject.name + "_" + transform.position.GetHashCode();
        }
    }

    void Start()
    {
        if (SistemaGuardado.EsCheckpointActivado(idCheckpoint))
        {
            activado = true;
            AplicarVisualActivo(false);
        }
        else
        {
            AplicarVisualInactivo();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ActivarPunto(other.gameObject);
        }
    }

    public void ActivarPunto(GameObject player)
    {
        bool primerActivacion = !activado;
        activado = true;

        // Guardar progreso en disco
        string escenaActual = SceneManager.GetActiveScene().name;
        Vector3 posAparicion = transform.position + transform.forward * 1.5f + Vector3.up * 0.1f;
        SistemaGuardado.RegistrarCheckpoint(idCheckpoint, posAparicion, transform.rotation, escenaActual);

        // Curar al jugador
        PoderEspiritual salud = player.GetComponent<PoderEspiritual>();
        if (salud != null)
        {
            salud.Curar(100f);
        }

        AplicarVisualActivo(true);

        if (primerActivacion)
        {
            AvisosJuego.Mostrar("* " + nombreAltar + " *\n¡Progreso guardado y alma purificada!", 3.5f);
            ReproducirCampana();
        }
    }

    void AplicarVisualActivo(bool conEfecto)
    {
        if (luzAltar != null)
        {
            luzAltar.color = colorActivo;
            luzAltar.intensity = intensidadActiva;
        }

        // Si no tiene luz, crear una de veladoras
        if (luzAltar == null)
        {
            GameObject lObj = new GameObject("LuzAltar_" + idCheckpoint);
            lObj.transform.SetParent(transform, false);
            lObj.transform.localPosition = new Vector3(0, 1.2f, 0);
            luzAltar = lObj.AddComponent<Light>();
            luzAltar.type = LightType.Point;
            luzAltar.color = colorActivo;
            luzAltar.intensity = intensidadActiva;
            luzAltar.range = 8f;
        }
    }

    void AplicarVisualInactivo()
    {
        if (luzAltar != null)
        {
            luzAltar.color = colorInactivo;
            luzAltar.intensity = 0.8f;
        }
    }

    void ReproducirCampana()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0.5f;
            audioSource.playOnAwake = false;
        }

        if (clipCampanada == null)
        {
            clipCampanada = GenerarSonidoCampana();
        }

        if (clipCampanada != null)
        {
            audioSource.PlayOneShot(clipCampanada, 0.85f * OpcionesJuego.Sfx);
        }
    }

    AudioClip GenerarSonidoCampana()
    {
        int rate = 22050;
        float dur = 2.0f;
        int muestras = (int)(rate * dur);
        float[] d = new float[muestras];
        float f0 = 587.33f; // Re5 (brillante como campana de bronce)

        for (int i = 0; i < muestras; i++)
        {
            float t = i / (float)rate;
            float env = Mathf.Exp(-t * 2.5f);
            float tono = Mathf.Sin(2f * Mathf.PI * f0 * t) * 0.5f +
                         Mathf.Sin(2f * Mathf.PI * f0 * 2.76f * t) * 0.25f +
                         Mathf.Sin(2f * Mathf.PI * f0 * 5.4f * t) * 0.15f;
            d[i] = tono * env;
        }

        AudioClip clip = AudioClip.Create("CampanaAltar", muestras, 1, rate, false);
        clip.SetData(d, 0);
        return clip;
    }
}
