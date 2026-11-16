using UnityEngine;

/// <summary>
/// El sendero del Nivel 1 guardado en la escena (puntos en coordenadas de mundo, uno cada ~4 m).
/// Lo usan las cinemáticas narrativas para saber "qué hay adelante en el camino" sin depender del editor.
/// Lo crea Nivel1HistoriaSetup.
/// </summary>
public class SenderoNivel : MonoBehaviour
{
    public static SenderoNivel Instancia { get; private set; }

    public Vector3[] puntos = new Vector3[0];

    void Awake() { Instancia = this; }
    void OnDestroy() { if (Instancia == this) Instancia = null; }

    public bool Valido { get { return puntos != null && puntos.Length > 2; } }

    public int MasCercano(Vector3 p)
    {
        int mejor = 0; float dMejor = float.MaxValue;
        for (int i = 0; i < puntos.Length; i++)
        {
            float dx = puntos[i].x - p.x, dz = puntos[i].z - p.z;
            float d = dx * dx + dz * dz;
            if (d < dMejor) { dMejor = d; mejor = i; }
        }
        return mejor;
    }

    /// <summary>Punto del sendero a 'metros' por delante de la posición dada (hacia el final) y la dirección del camino ahí.</summary>
    public Vector3 Adelante(Vector3 desde, float metros, out Vector3 direccion)
    {
        int i = MasCercano(desde);
        float acum = 0f;
        direccion = Vector3.forward;
        while (i < puntos.Length - 1)
        {
            Vector3 a = puntos[i], b = puntos[i + 1];
            float tramo = Vector3.Distance(new Vector3(a.x, 0f, a.z), new Vector3(b.x, 0f, b.z));
            direccion = new Vector3(b.x - a.x, 0f, b.z - a.z).normalized;
            if (acum + tramo >= metros)
            {
                float k = tramo > 0.001f ? (metros - acum) / tramo : 0f;
                return Vector3.Lerp(a, b, Mathf.Clamp01(k));
            }
            acum += tramo;
            i++;
        }
        return puntos[puntos.Length - 1];
    }
}
