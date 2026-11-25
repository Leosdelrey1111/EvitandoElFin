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
    public string siguienteEscena = "Nivel2_Panteon";

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
        // Estado inicial del nivel (DespertarConfusion lo pasa a Intro y luego a Jugando)
        EstadoNivel.Reiniciar();

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

        // Si venimos de un checkpoint guardado en esta escena
        VerificarCheckpointInicial();

        // Iniciar música base de exploración en loop mientras no estemos peleando contra un jefe
        if (estadoJefe != EstadoJefe.Combate)
        {
            ReproductorMusicaLoop.ReproducirExploracion();
        }
    }

    void VerificarCheckpointInicial()
    {
        if (SistemaGuardado.HayPartidaGuardada() && SistemaGuardado.Datos.escenaActual == SceneManager.GetActiveScene().name)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null && !string.IsNullOrEmpty(SistemaGuardado.Datos.idUltimoCheckpoint))
            {
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                player.transform.position = SistemaGuardado.ObtenerPosicionReaparicion(player.transform.position);
                player.transform.rotation = SistemaGuardado.ObtenerRotacionReaparicion(player.transform.rotation);
                if (cc != null) cc.enabled = true;
                Debug.Log("[GameManager] Jugador reubicado en el checkpoint: " + SistemaGuardado.Datos.idUltimoCheckpoint);
            }
        }
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

        // Panel Game Over (reintentar desde el último checkpoint sin borrar tótems)
        if (panelGameOver != null)
        {
            AsignarAccionBoton(panelGameOver.transform, "BtnReintentar", ReintentarDesdeCheckpoint);
            AsignarAccionBoton(panelGameOver.transform, "BtnMenu", IrAlMenuPrincipal);
        }

        // Panel Victoria
        if (panelVictoria != null)
        {
            AsignarAccionBoton(panelVictoria.transform, "BtnSiguiente", ContinuarSiguienteNivel);
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
        // Tecla F8: Cambiar calidad gráfica al instante en gameplay (Baja -> Media -> Alta)
        if (Input.GetKeyDown(KeyCode.F8))
        {
            int nuevaCalidad = (OpcionesJuego.Calidad + 1) % 3;
            OpcionesJuego.Calidad = nuevaCalidad;
            string textoCalidad = nuevaCalidad == 0 ? "CALIDAD: GAMA BAJA (Optimizado para móviles / laptops básicas)" :
                                 (nuevaCalidad == 1 ? "CALIDAD: GAMA MEDIA (Equilibrio visual a 60 FPS)" :
                                                      "CALIDAD: GAMA ALTA (Máxima fidelidad gráfica)");
            AvisosJuego.Mostrar(textoCalidad, 3f);
        }

        // Alternar pausa con tecla Escape (o 'P')
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (PanelConfiguracion.EsperandoTecla) return;
            if (PanelConfiguracion.Abierto)
            {
                PanelConfiguracion.Instancia.Cerrar();
                return;
            }
            // la tienda del Comerciante usa Esc para cerrarse: no debe abrir también la pausa
            if (!juegoTerminado && !ComercianteMercader.BloqueaPausa && !CodiceHistoria.BloqueaPausa)
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

        if (juegoPausado) EstadoNivel.Pausar();
        else EstadoNivel.Reanudar();
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

    public enum EstadoJefe { Ninguno, Dormido, Combate, Derrotado }

    [Header("Jefe del nivel")]
    [Tooltip("Si es true, el portal solo se abre después de vencer al jefe (además de reunir los tótems).")]
    public bool jefeRequerido = false;
    public EstadoJefe estadoJefe = EstadoJefe.Ninguno;

    public bool TotemsCompletos() { return portalDesbloqueado || totemsRecolectados >= totemsRequeridos; }

    public void CambiarEstadoJefe(EstadoJefe nuevo)
    {
        estadoJefe = nuevo;
        if (nuevo == EstadoJefe.Combate)
        {
            if (ReproductorMusicaLoop.Instancia == null || !ReproductorMusicaLoop.Instancia.EnCombateJefe)
            {
                ReproductorMusicaLoop.IniciarCombateJefeNivelActual();
            }
        }
        else if (nuevo == EstadoJefe.Derrotado)
        {
            ReproductorMusicaLoop.FinalizarCombateJefe();
        }
    }

    public bool EstaPortalDesbloqueado()
    {
        return TotemsCompletos() && (!jefeRequerido || estadoJefe == EstadoJefe.Derrotado);
    }

    public void ProcesarVictoria()
    {
        if (juegoTerminado) return;
        juegoTerminado = true;
        EstadoNivel.Terminar(ResultadoNivel.Victoria);

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

        string esc = SceneManager.GetActiveScene().name;
        int niv = (esc.Contains("Nivel3") || esc.Contains("Ciudad")) ? 3 : ((esc.Contains("Nivel2") || esc.Contains("Panteon")) ? 2 : 1);
        SistemaGuardado.MarcarNivelCompletado(niv);

        Debug.Log("[GameManager] ¡Nivel " + niv + " Superado con éxito!");
    }

    public void ProcesarDerrota()
    {
        if (juegoTerminado) return;
        juegoTerminado = true;
        EstadoNivel.Terminar(ResultadoNivel.Derrota);

        StartCoroutine(RutinaGameOver());
    }

    private IEnumerator RutinaGameOver()
    {
        // Breve retraso antes de congelar para ver la animación de caída/derrota
        yield return new WaitForSeconds(2.6f);

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

    public void ContinuarSiguienteNivel()
    {
        ReproducirClick();
        Time.timeScale = 1f;

        string actual = SceneManager.GetActiveScene().name;
        int nuevoNivel = 2;
        string destino = siguienteEscena;

        if (actual.Contains("Nivel2") || actual.Contains("Panteon"))
        {
            nuevoNivel = 3;
            destino = "Nivel3_Ciudad";
        }
        else if (actual.Contains("Nivel3") || actual.Contains("Ciudad"))
        {
            nuevoNivel = 4;
            destino = nombreMenuPrincipal;
        }

        SistemaGuardado.SincronizarNivel(nuevoNivel, destino);

        if (!string.IsNullOrEmpty(destino) && Application.CanStreamedLevelBeLoaded(destino))
        {
            SceneManager.LoadScene(destino);
        }
        else
        {
            Debug.LogWarning("[GameManager] La escena destino '" + destino + "' no está en Build Settings. Volviendo al Menú Principal.");
            IrAlMenuPrincipal();
        }
    }

    public void ReintentarDesdeCheckpoint()
    {
        ReproducirClick();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReiniciarNivel()
    {
        ReproducirClick();
        Time.timeScale = 1f;
        SistemaGuardado.ReiniciarNivelActual();
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
