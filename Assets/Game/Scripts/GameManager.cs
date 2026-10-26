using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instancia { get; private set; }

    [Header("Objetivos del Nivel 1")]
    [Tooltip("Cantidad de tótems requeridos para desbloquear el portal de victoria")]
    public int totemsRequeridos = 3;
    private int totemsRecolectados = 0;
    private bool portalDesbloqueado = false;

    [Header("Paneles de Interfaz")]
    public GameObject panelPausa;
    public GameObject panelGameOver;
    public GameObject panelVictoria;

    [Header("Nombres de Escenas")]
    public string nombreMenuPrincipal = "MainMenu";

    [Header("Audio SFX (Opcional)")]
    public AudioSource audioSource;
    public AudioClip clipVictoria;
    public AudioClip clipGameOver;
    public AudioClip clipBoton;

    private bool juegoPausado = false;
    private bool juegoTerminado = false;
    private PoderEspiritual vidaJugador;

    void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        Instancia = this;
    }

    void Start()
    {
        // Reanudar tiempo y bloquear cursor para gameplay
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Ocultar paneles iniciales
        if (panelPausa != null) panelPausa.SetActive(false);
        if (panelGameOver != null) panelGameOver.SetActive(false);
        if (panelVictoria != null) panelVictoria.SetActive(false);

        // Conectar botones en runtime para asegurar funcionalidad independientemente de cómo se crearon
        ConectarBotonesUI();

        // Suscribirse a los eventos del jugador
        ConectarEventosJugador();
    }

    void ConectarBotonesUI()
    {
        // Panel de Pausa
        if (panelPausa != null)
        {
            AsignarAccionBoton(panelPausa.transform, "BtnReanudar", ReanudarJuego);
            AsignarAccionBoton(panelPausa.transform, "BtnReiniciar", ReiniciarNivel);
            AsignarAccionBoton(panelPausa.transform, "BtnMenu", IrAlMenuPrincipal);
        }

        // Panel Game Over
        if (panelGameOver != null)
        {
            AsignarAccionBoton(panelGameOver.transform, "BtnReintentar", ReiniciarNivel);
            AsignarAccionBoton(panelGameOver.transform, "BtnMenu", IrAlMenuPrincipal);
        }

        // Panel Victoria
        if (panelVictoria != null)
        {
            AsignarAccionBoton(panelVictoria.transform, "BtnRejugar", ReiniciarNivel);
            AsignarAccionBoton(panelVictoria.transform, "BtnMenu", IrAlMenuPrincipal);
        }
    }

    void AsignarAccionBoton(Transform padre, string nombreBoton, UnityEngine.Events.UnityAction accion)
    {
        Transform t = padre.Find(nombreBoton);
        if (t != null)
        {
            UnityEngine.UI.Button btn = t.GetComponent<UnityEngine.UI.Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(accion);
            }
        }
    }

    void ConectarEventosJugador()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            vidaJugador = player.GetComponent<PoderEspiritual>();
            if (vidaJugador != null)
            {
                vidaJugador.OnMuerte += ProcesarDerrota;
            }

            InventarioEspiritual inventario = player.GetComponent<InventarioEspiritual>();
            if (inventario != null)
            {
                inventario.OnCambioTotems += AlCambiarTotems;
            }
        }
    }

    void Update()
    {
        // Alternar pausa con tecla Escape (o 'P')
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (!juegoTerminado)
            {
                AlternarPausa();
            }
        }
    }

    public void AlternarPausa()
    {
        if (juegoTerminado) return;

        juegoPausado = !juegoPausado;

        if (panelPausa != null)
        {
            panelPausa.SetActive(juegoPausado);
        }

        Time.timeScale = juegoPausado ? 0f : 1f;
        Cursor.lockState = juegoPausado ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = juegoPausado;
    }

    public void ReanudarJuego()
    {
        ReproducirClick();
        if (juegoPausado)
        {
            AlternarPausa();
        }
    }

    public void AlCambiarTotems(int actual)
    {
        totemsRecolectados = actual;
        if (totemsRecolectados >= totemsRequeridos && !portalDesbloqueado)
        {
            portalDesbloqueado = true;
            Debug.Log("[GameManager] ¡Todos los tótems reunidos! El portal espiritual ahora está activo.");
            // Si el HUD espiritual tiene soporte, se notifica el objetivo
        }
    }

    public bool EstaPortalDesbloqueado()
    {
        return portalDesbloqueado || totemsRecolectados >= totemsRequeridos;
    }

    public void ProcesarVictoria()
    {
        if (juegoTerminado) return;
        juegoTerminado = true;

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (panelVictoria != null)
        {
            panelVictoria.SetActive(true);
        }

        if (audioSource != null && clipVictoria != null)
        {
            audioSource.PlayOneShot(clipVictoria);
        }

        Debug.Log("[GameManager] ¡Nivel 1 Superado con éxito!");
    }

    public void ProcesarDerrota()
    {
        if (juegoTerminado) return;
        juegoTerminado = true;

        StartCoroutine(RutinaGameOver());
    }

    private IEnumerator RutinaGameOver()
    {
        // Breve retraso antes de congelar para ver la animación de caída/derrota
        yield return new WaitForSeconds(1.2f);

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (panelGameOver != null)
        {
            panelGameOver.SetActive(true);
        }

        if (audioSource != null && clipGameOver != null)
        {
            audioSource.PlayOneShot(clipGameOver);
        }

        Debug.Log("[GameManager] El Chofer ha perecido. Fin de la partida.");
    }

    public void ReiniciarNivel()
    {
        ReproducirClick();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void IrAlMenuPrincipal()
    {
        ReproducirClick();
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(nombreMenuPrincipal))
        {
            SceneManager.LoadScene(nombreMenuPrincipal);
        }
        else
        {
            SceneManager.LoadScene(0); // Fallback al índice 0
        }
    }

    public void SalirDelJuego()
    {
        ReproducirClick();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ReproducirClick()
    {
        if (audioSource != null && clipBoton != null)
        {
            audioSource.PlayOneShot(clipBoton);
        }
    }

    void OnDestroy()
    {
        if (vidaJugador != null)
        {
            vidaJugador.OnMuerte -= ProcesarDerrota;
        }
    }
}
