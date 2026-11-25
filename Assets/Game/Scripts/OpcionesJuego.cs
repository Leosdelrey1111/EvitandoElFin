using System;
using UnityEngine;

/// <summary>
/// Acciones configurables del jugador (movimiento, habilidades e interfaz).
/// </summary>
public enum AccionJuego
{
    Adelante,
    Atras,
    Izquierda,
    Derecha,
    Correr,
    Saltar,
    Esquivar,
    Golpe,
    Onda,
    Interactuar,
    Pista,
    Codice
}

/// <summary>
/// Opciones del jugador guardadas en PlayerPrefs (volúmenes, sensibilidad, calidad, pantalla completa y controles).
/// Se aplican automáticamente al iniciar cada escena y pueden cambiarse en vivo desde el menú de pausa.
/// </summary>
public static class OpcionesJuego
{
    const string KMaster = "Opc_Master", KMusica = "Opc_Musica", KEfectos = "Opc_Efectos",
                 KSens = "Opc_Sensibilidad", KInvertY = "Opc_InvertirY", KCalidad = "Opc_Calidad", KPantalla = "Opc_PantallaCompleta";

    public static event Action OnCambioOpciones;

    public static void NotificarCambio() { OnCambioOpciones?.Invoke(); }

    public static float Master { get { return PlayerPrefs.GetFloat(KMaster, 0.85f); } set { PlayerPrefs.SetFloat(KMaster, Mathf.Clamp01(value)); AudioListener.volume = Master; NotificarCambio(); } }
    public static float Musica { get { return PlayerPrefs.GetFloat(KMusica, 0.7f); } set { PlayerPrefs.SetFloat(KMusica, Mathf.Clamp01(value)); NotificarCambio(); } }
    public static float Efectos { get { return PlayerPrefs.GetFloat(KEfectos, 0.85f); } set { PlayerPrefs.SetFloat(KEfectos, Mathf.Clamp01(value)); NotificarCambio(); } }
    public static float Sfx { get { return Efectos; } set { Efectos = value; } }
    public static float Voz { get { return PlayerPrefs.GetFloat("Opc_Voz", 0.9f); } set { PlayerPrefs.SetFloat("Opc_Voz", Mathf.Clamp01(value)); NotificarCambio(); } }
    /// <summary>Multiplicador de sensibilidad de la cámara (0.3 a 2).</summary>
    public static float Sensibilidad { get { return PlayerPrefs.GetFloat(KSens, 1f); } set { PlayerPrefs.SetFloat(KSens, Mathf.Clamp(value, 0.3f, 2f)); NotificarCambio(); } }
    public static bool InvertirY { get { return PlayerPrefs.GetInt(KInvertY, 0) == 1; } set { PlayerPrefs.SetInt(KInvertY, value ? 1 : 0); NotificarCambio(); } }
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
        AplicarCalidad();
        if (PlayerPrefs.HasKey(KPantalla)) Screen.fullScreen = PantallaCompleta;
    }

    public static void AplicarCalidad()
    {
        int nivel = Calidad; // 0 = Baja, 1 = Media, 2 = Alta

        switch (nivel)
        {
            case 0: // GAMA BAJA (Dispositivos móviles, laptops básicas, Intel HD integrada)
                // Carga todo el mundo, árboles y modelos pero optimizado al máximo para alto rendimiento
                if (QualitySettings.names.Length > 0) QualitySettings.SetQualityLevel(0, true);
                QualitySettings.maximumLODLevel = 0;           // IMPORTANTE: jamás saltar LOD 0 para no hacer desaparecer árboles y rocas
                QualitySettings.lodBias = 1.0f;                // Mantiene todos los objetos del bosque visibles a distancia
                QualitySettings.globalTextureMipmapLimit = 1;  // Reduce texturas a la mitad (75% menos VRAM) sin verse borrosas
                QualitySettings.shadows = ShadowQuality.HardOnly;
                QualitySettings.shadowDistance = 32f;
                QualitySettings.shadowResolution = ShadowResolution.Low;
                QualitySettings.shadowCascades = 1;
                QualitySettings.antiAliasing = 0;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
                QualitySettings.skinWeights = SkinWeights.TwoBones;
                QualitySettings.softParticles = false;
                QualitySettings.softVegetation = true;
                QualitySettings.realtimeReflectionProbes = false;
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = 60;
                break;

            case 1: // GAMA MEDIA (Equilibrio visual y fluidez a 60 FPS)
                int nivelMedio = Mathf.Clamp(1, 0, QualitySettings.names.Length - 1);
                if (QualitySettings.names.Length > 0) QualitySettings.SetQualityLevel(nivelMedio, true);
                QualitySettings.maximumLODLevel = 0;
                QualitySettings.lodBias = 1.35f;
                QualitySettings.globalTextureMipmapLimit = 0;
                QualitySettings.shadows = ShadowQuality.All;
                QualitySettings.shadowDistance = 55f;
                QualitySettings.shadowResolution = ShadowResolution.Medium;
                QualitySettings.shadowCascades = 2;
                QualitySettings.antiAliasing = 2;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
                QualitySettings.skinWeights = SkinWeights.TwoBones;
                QualitySettings.softParticles = true;
                QualitySettings.softVegetation = true;
                QualitySettings.realtimeReflectionProbes = false;
                QualitySettings.vSyncCount = 1;
                Application.targetFrameRate = 60;
                break;

            case 2: // GAMA ALTA (PC Gamer / Máxima fidelidad visual)
                int maxNivel = Mathf.Max(0, QualitySettings.names.Length - 1);
                QualitySettings.SetQualityLevel(maxNivel, true);
                QualitySettings.maximumLODLevel = 0;
                QualitySettings.lodBias = 2.0f;
                QualitySettings.globalTextureMipmapLimit = 0;
                QualitySettings.shadows = ShadowQuality.All;
                QualitySettings.shadowDistance = 90f;
                QualitySettings.shadowResolution = ShadowResolution.High;
                QualitySettings.shadowCascades = 4;
                QualitySettings.antiAliasing = 4;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
                QualitySettings.skinWeights = SkinWeights.FourBones;
                QualitySettings.softParticles = true;
                QualitySettings.softVegetation = true;
                QualitySettings.realtimeReflectionProbes = true;
                QualitySettings.lodBias = 2.0f;
                QualitySettings.vSyncCount = 1;
                Application.targetFrameRate = -1;
                break;
        }

        // Asegura que en todas las calidades (especialmente Gama Baja) el terreno dibuje todos sus árboles y follaje
        try
        {
            foreach (var t in Terrain.activeTerrains)
            {
                if (t != null)
                {
                    t.drawTreesAndFoliage = true;
                    t.treeDistance = (nivel == 0) ? 220f : ((nivel == 1) ? 350f : 500f);
                    t.treeBillboardDistance = (nivel == 0) ? 120f : ((nivel == 1) ? 200f : 300f);
                    t.detailObjectDistance = (nivel == 0) ? 45f : ((nivel == 1) ? 70f : 100f);
                    t.heightmapPixelError = (nivel == 0) ? 5 : ((nivel == 1) ? 3 : 1);
                }
            }
        }
        catch { }

        Debug.Log($"[OpcionesJuego] Calidad Gráfica ajustada a: {(nivel == 0 ? "BAJA (Gama Baja/Móvil)" : (nivel == 1 ? "MEDIA (Gama Media)" : "ALTA (Gama Alta)"))}");
    }

    public static KeyCode TeclaPorDefecto(AccionJuego a)
    {
        switch (a)
        {
            case AccionJuego.Adelante: return KeyCode.W;
            case AccionJuego.Atras: return KeyCode.S;
            case AccionJuego.Izquierda: return KeyCode.A;
            case AccionJuego.Derecha: return KeyCode.D;
            case AccionJuego.Correr: return KeyCode.LeftShift;
            case AccionJuego.Saltar: return KeyCode.Space;
            case AccionJuego.Esquivar: return KeyCode.LeftControl;
            case AccionJuego.Golpe: return KeyCode.Mouse0;
            case AccionJuego.Onda: return KeyCode.Q;
            case AccionJuego.Interactuar: return KeyCode.E;
            case AccionJuego.Pista: return KeyCode.R;
            case AccionJuego.Codice: return KeyCode.Tab;
            default: return KeyCode.None;
        }
    }

    public static KeyCode ObtenerTecla(AccionJuego a)
    {
        int def = (int)TeclaPorDefecto(a);
        return (KeyCode)PlayerPrefs.GetInt("Opc_Key_" + a, def);
    }

    public static void AsignarTecla(AccionJuego a, KeyCode k)
    {
        PlayerPrefs.SetInt("Opc_Key_" + a, (int)k);
        PlayerPrefs.Save();
        NotificarCambio();
    }

    public static void RestablecerControles()
    {
        foreach (AccionJuego a in Enum.GetValues(typeof(AccionJuego)))
            PlayerPrefs.DeleteKey("Opc_Key_" + a);
        PlayerPrefs.Save();
        NotificarCambio();
    }

    public static string NombreCortoTecla(KeyCode k)
    {
        switch (k)
        {
            case KeyCode.Mouse0: return "CLIC";
            case KeyCode.Mouse1: return "CLIC D";
            case KeyCode.Mouse2: return "RUEDA";
            case KeyCode.Space: return "ESPACIO";
            case KeyCode.LeftControl:
            case KeyCode.RightControl: return "CTRL";
            case KeyCode.LeftShift:
            case KeyCode.RightShift: return "SHIFT";
            case KeyCode.LeftAlt:
            case KeyCode.RightAlt: return "ALT";
            case KeyCode.Tab: return "TAB";
            case KeyCode.Return: return "ENTER";
            case KeyCode.UpArrow: return "ARRIBA";
            case KeyCode.DownArrow: return "ABAJO";
            case KeyCode.LeftArrow: return "IZQ";
            case KeyCode.RightArrow: return "DER";
            default: return k.ToString().ToUpper();
        }
    }

    public static void Guardar() { PlayerPrefs.Save(); }
}
