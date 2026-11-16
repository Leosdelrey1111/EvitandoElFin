using UnityEngine;

/// <summary>
/// Ambiente sonoro nocturno del bosque generado por código (viento, grillos y un colchón grave muy tenue).
/// Sirve de "placeholder" hasta tener los audios finales de REAPER; se puede apagar o reemplazar con tus clips.
/// </summary>
public class AmbienteSonoroBosque : MonoBehaviour
{
    [Range(0f, 1f)] public float volumenAmbiente = 0.35f;
    [Range(0f, 1f)] public float volumenGrave = 0.06f;
    [Tooltip("Si asignas un clip propio, reemplaza al ambiente procedural.")]
    public AudioClip clipPropio;

    void Start()
    {
        AudioSource amb = gameObject.AddComponent<AudioSource>();
        amb.loop = true;
        amb.spatialBlend = 0f;
        amb.volume = volumenAmbiente * Mathf.Lerp(0.4f, 1.2f, OpcionesJuego.Musica);
        amb.clip = clipPropio != null ? clipPropio : GenerarAmbiente();
        amb.Play();

        AudioSource grave = gameObject.AddComponent<AudioSource>();
        grave.loop = true;
        grave.spatialBlend = 0f;
        grave.volume = volumenGrave;
        grave.clip = GenerarGrave();
        grave.Play();
    }

    AudioClip GenerarAmbiente()
    {
        const int rate = 22050;
        const float dur = 8f;
        int n = (int)(rate * dur);
        float[] d = new float[n];
        System.Random rng = new System.Random(321);
        float lp = 0f, lp2 = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / rate;
            float ruido = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp += (ruido - lp) * 0.02f;
            lp2 += (lp - lp2) * 0.05f;
            float mod = 0.55f + 0.30f * Mathf.Sin(2f * Mathf.PI * 0.125f * t) + 0.15f * Mathf.Sin(2f * Mathf.PI * 0.375f * t + 1.2f);
            float viento = lp2 * 9f * mod;

            // Grillos: dos "coros" desfasados
            float c1 = Mathf.Clamp01(Mathf.Sin(2f * Mathf.PI * 11f * t) * 3f) * Mathf.Clamp01(Mathf.Sin(2f * Mathf.PI * 0.5f * t) * 2f + 0.4f);
            float c2 = Mathf.Clamp01(Mathf.Sin(2f * Mathf.PI * 13f * t + 1f) * 3f) * Mathf.Clamp01(Mathf.Sin(2f * Mathf.PI * 0.25f * t + 2f) * 2f + 0.3f);
            float grillos = (Mathf.Sin(2f * Mathf.PI * 4200f * t) * c1 + Mathf.Sin(2f * Mathf.PI * 4800f * t) * c2) * 0.02f;

            d[i] = Mathf.Clamp(viento * 0.5f + grillos, -1f, 1f);
        }
        AudioClip c = AudioClip.Create("AmbienteBosque", n, 1, rate, false);
        c.SetData(d, 0);
        return c;
    }

    AudioClip GenerarGrave()
    {
        const int rate = 22050;
        const float dur = 4f;
        int n = (int)(rate * dur);
        float[] d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / rate;
            d[i] = (Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * 55.5f * t) * 0.4f + Mathf.Sin(2f * Mathf.PI * 82.5f * t) * 0.2f) * 0.5f;
        }
        AudioClip c = AudioClip.Create("GraveBosque", n, 1, rate, false);
        c.SetData(d, 0);
        return c;
    }
}
