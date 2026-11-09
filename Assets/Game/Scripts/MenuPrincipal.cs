using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MenuPrincipal : MonoBehaviour
{
    [Header("Configuración de Escenas")]
    [Tooltip("Nombre de la escena del primer nivel.")]
    public string nombreEscenaNivel1 = "SampleScene";

    [Header("Paneles de Menú")]
    public GameObject panelPrincipal;
    public GameObject panelOpciones;
    public GameObject panelControles;

    [Header("Audio (Opcional)")]
    public AudioSource audioSource;
    public AudioClip clipBotonClick;

    void Start()
    {
        // Asegurar que el tiempo corra normalmente si venimos de un menú de pausa
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        MostrarPanelPrincipal();
    }

    public void JugarNivel1()
    {
        ReproducirClick();
        if (!string.IsNullOrEmpty(nombreEscenaNivel1))
        {
            SceneManager.LoadScene(nombreEscenaNivel1);
        }
        else
        {
            Debug.LogError("[MenuPrincipal] No se asignó el nombre de la escena del nivel 1.");
        }
    }

    public void MostrarPanelPrincipal()
    {
        ReproducirClick();
        if (panelPrincipal != null) panelPrincipal.SetActive(true);
        if (panelOpciones != null) panelOpciones.SetActive(false);
        if (panelControles != null) panelControles.SetActive(false);
    }

    public void MostrarPanelOpciones()
    {
        ReproducirClick();
        if (panelPrincipal != null) panelPrincipal.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(true);
        if (panelControles != null) panelControles.SetActive(false);
    }

    public void MostrarPanelControles()
    {
        ReproducirClick();
        if (panelPrincipal != null) panelPrincipal.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(false);
        if (panelControles != null) panelControles.SetActive(true);
    }

    public void AjustarVolumenGeneral(float volumen)
    {
        AudioListener.volume = Mathf.Clamp01(volumen);
    }

    public void SalirDelJuego()
    {
        ReproducirClick();
        Debug.Log("[MenuPrincipal] Saliendo del juego...");
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ReproducirClick()
    {
        if (audioSource != null && clipBotonClick != null)
        {
            audioSource.PlayOneShot(clipBotonClick);
        }
    }
}
