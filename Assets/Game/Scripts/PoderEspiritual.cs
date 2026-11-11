using UnityEngine;
using System;

public class PoderEspiritual : MonoBehaviour
{
    [Header("Configuración de Vida")]
    public float maxPoder = 100f;
    public float actualPoder;

    [Header("Estado")]
    [Tooltip("Mientras sea true no se recibe daño (i-frames del esquive).")]
    public bool invulnerable = false;

    public event Action OnMuerte;
    public event Action<float> OnCambioPoder;
    /// <summary>Se dispara con la cantidad de daño realmente recibido (útil para feedback visual).</summary>
    public event Action<float> OnRecibioDanio;

    public bool EstaMuerto { get { return actualPoder <= 0f; } }
    public float Porcentaje { get { return maxPoder <= 0f ? 0f : actualPoder / maxPoder; } }

    void Awake()
    {
        actualPoder = maxPoder;
    }

    public void RecibirDanio(float danio)
    {
        if (invulnerable || actualPoder <= 0f || danio <= 0f) return;

        actualPoder = Mathf.Clamp(actualPoder - danio, 0f, maxPoder);
        OnRecibioDanio?.Invoke(danio);
        OnCambioPoder?.Invoke(actualPoder);

        if (actualPoder <= 0f)
        {
            OnMuerte?.Invoke();
        }
    }

    public void Curar(float cantidad)
    {
        if (actualPoder <= 0f) return;
        actualPoder = Mathf.Clamp(actualPoder + cantidad, 0f, maxPoder);
        OnCambioPoder?.Invoke(actualPoder);
    }
}
