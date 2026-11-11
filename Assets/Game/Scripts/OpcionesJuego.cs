using UnityEngine;

/// <summary>
/// Opciones del jugador guardadas en PlayerPrefs (volúmenes, sensibilidad, calidad, pantalla completa).
/// Se aplican automáticamente al iniciar cada escena.
/// </summary>
public static class OpcionesJuego
{
    const string KMaster = "Opc_Master", KMusica = "Opc_Musica", KEfectos = "Opc_Efectos",
                 KSens = "Opc_Sensibilidad", KCalidad = "Opc_Calidad", KPantalla = "Opc_PantallaCompleta";

    public static float Master { get { return PlayerPrefs.GetFloat(KMaster, 0.85f); } set { PlayerPrefs.SetFloat(KMaster, Mathf.Clamp01(value)); AudioListener.volume = Master; } }
    public static float Musica { get { return PlayerPrefs.GetFloat(KMusica, 0.7f); } set { PlayerPrefs.SetFloat(KMusica, Mathf.Clamp01(value)); } }
    public static float Efectos { get { return PlayerPrefs.GetFloat(KEfectos, 0.85f); } set { PlayerPrefs.SetFloat(KEfectos, Mathf.Clamp01(value)); } }
    /// <summary>Multiplicador de sensibilidad de la cámara (0.3 a 2).</summary>
    public static float Sensibilidad { get { return PlayerPrefs.GetFloat(KSens, 1f); } set { PlayerPrefs.SetFloat(KSens, Mathf.Clamp(value, 0.3f, 2f)); } }
    /// <summary>0 = Baja, 1 = Media, 2 = Alta.</summary>
    public static int Calidad
    {
        get { return PlayerPrefs.GetInt(KCalidad, 2); }
        set { PlayerPrefs.SetInt(KCalidad, Mathf.Clamp(value, 0, 2)); AplicarCalidad(); }
    }
    public static bool PantallaCompleta
    {
        get { return PlayerPrefs.GetInt(KPantalla, Screen.fullScreen ? 1 : 0) == 1; }
        set { PlayerPrefs.SetInt(KPantalla, value ? 1 : 0); Screen.fullScreen = value; }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void Aplicar()
    {
        AudioListener.volume = Master;
        if (PlayerPrefs.HasKey(KCalidad)) AplicarCalidad();
        if (PlayerPrefs.HasKey(KPantalla)) Screen.fullScreen = PantallaCompleta;
    }

    static void AplicarCalidad()
    {
        int niveles = QualitySettings.names.Length;
        if (niveles <= 0) return;
        int nivel = Calidad == 0 ? 0 : (Calidad == 1 ? niveles / 2 : niveles - 1);
        QualitySettings.SetQualityLevel(nivel, true);
    }

    public static void Guardar() { PlayerPrefs.Save(); }
}
