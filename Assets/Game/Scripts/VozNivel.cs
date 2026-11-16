using UnityEngine;

public enum CanalVoz
{
    /// <summary>Tonalli, el alebrije. Se pausa junto con el juego.</summary>
    Guia = 0,
    /// <summary>La Bufa (con eco de piedra). Se pausa junto con el juego.</summary>
    Personaje = 1,
    /// <summary>Tienda y libro de la historia: suenan aunque el juego esté en pausa.</summary>
    Interfaz = 2
}

/// <summary>
/// Reproduce las voces del juego. Los audios viven en Assets/Game/Resources/Voz/&lt;clave&gt;.wav
/// (los genera el menú "9. Generar voces del Nivel 1"; se pueden reemplazar por grabaciones reales con el mismo nombre).
/// Si una frase no tiene audio, simplemente no suena (el texto se muestra igual).
/// </summary>
public static class VozNivel
{
    static AudioSource[] fuentes;

    static void Asegurar()
    {
        if (fuentes != null && fuentes[0] != null) return;
        GameObject go = new GameObject("VozNivel");
        Object.DontDestroyOnLoad(go);
        string[] nombres = { "Guia", "Personaje", "Interfaz" };
        fuentes = new AudioSource[3];
        for (int i = 0; i < fuentes.Length; i++)
        {
            // un objeto por canal: así el canal del jefe puede llevar eco de piedra sin afectar a las demás voces
            GameObject hijo = new GameObject(nombres[i]);
            hijo.transform.SetParent(go.transform, false);
            AudioSource a = hijo.AddComponent<AudioSource>();
            a.playOnAwake = false;
            a.spatialBlend = 0f;
            a.bypassEffects = i != (int)CanalVoz.Personaje;
            if (i == (int)CanalVoz.Personaje)
            {
                // eco de piedra CORTO: el preset StoneCorridor dura ~2.7 s y la cola se montaba sobre la frase siguiente
                AudioReverbFilter eco = hijo.AddComponent<AudioReverbFilter>();
                eco.reverbPreset = AudioReverbPreset.User;
                eco.dryLevel = 0f;
                eco.room = -900f;
                eco.roomHF = -500f;
                eco.decayTime = 0.9f;
                eco.decayHFRatio = 0.6f;
                eco.reflectionsLevel = -1100f;
                eco.reflectionsDelay = 0.012f;
                eco.reverbLevel = -300f;
                eco.reverbDelay = 0.02f;
                eco.diffusion = 100f;
                eco.density = 100f;
            }
            fuentes[i] = a;
        }
        VigilanteVoz v = go.AddComponent<VigilanteVoz>();
        v.fuentes = fuentes;
    }

    public static AudioClip Obtener(string clave)
    {
        return Resources.Load<AudioClip>("Voz/" + clave);
    }

    /// <summary>Reproduce la voz de una frase (cortando lo que dijera ese canal). Devuelve el clip, o null si no hay audio.</summary>
    public static AudioClip Reproducir(string clave, CanalVoz canal = CanalVoz.Guia, float volumen = 1f)
    {
        AudioClip clip = Obtener(clave);
        if (clip == null) return null;
        Asegurar();
        AudioSource f = fuentes[(int)canal];
        f.Stop();
        f.clip = clip;
        f.volume = Mathf.Clamp01(volumen * Mathf.Clamp01(OpcionesJuego.Efectos + 0.15f));
        f.Play();
        return clip;
    }

    public static void Detener(CanalVoz canal)
    {
        if (fuentes == null || fuentes[0] == null) return;
        fuentes[(int)canal].Stop();
    }

    /// <summary>Segundos que lleva sonando la voz de ese canal (0 si no suena). El texto de los diálogos se sincroniza con esto.</summary>
    public static float Tiempo(CanalVoz canal)
    {
        if (fuentes == null || fuentes[0] == null) return 0f;
        return fuentes[(int)canal].time;
    }

    /// <summary>Carga todas las voces en memoria al empezar el nivel para que la primera reproducción no cause un tirón.</summary>
    public static void Precargar()
    {
        Asegurar();
        foreach (AudioClip c in Resources.LoadAll<AudioClip>("Voz"))
            if (c != null && c.loadState == AudioDataLoadState.Unloaded) c.LoadAudioData();
    }

    public static bool Hablando(CanalVoz canal)
    {
        return fuentes != null && fuentes[0] != null && fuentes[(int)canal].isPlaying;
    }

    /// <summary>Pausa las voces del juego (no las de la interfaz) mientras Time.timeScale = 0.</summary>
    class VigilanteVoz : MonoBehaviour
    {
        public AudioSource[] fuentes;

        void Update()
        {
            bool pausa = Time.timeScale <= 0.01f;
            for (int i = 0; i < 2; i++)
            {
                if (fuentes[i] == null) continue;
                if (pausa) { if (fuentes[i].isPlaying) fuentes[i].Pause(); }
                else fuentes[i].UnPause();
            }
        }
    }
}
