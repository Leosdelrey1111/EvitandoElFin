using UnityEngine;
using System;

public class PoderEspiritual : MonoBehaviour
{
    [Header("Configuración de Vida")]
    public float maxPoder = 100f;
    public float actualPoder;

    public event Action OnMuerte;
    public event Action<float> OnCambioPoder;

    void Awake()
    {
        actualPoder = maxPoder;
    }

    public void RecibirDanio(float danio)
    {
        actualPoder = Mathf.Clamp(actualPoder - danio, 0f, maxPoder);
        OnCambioPoder?.Invoke(actualPoder);

        if (actualPoder <= 0f)
        {
            OnMuerte?.Invoke();
        }
    }

    public void Curar(float cantidad)
    {
        actualPoder = Mathf.Clamp(actualPoder + cantidad, 0f, maxPoder);
        OnCambioPoder?.Invoke(actualPoder);
    }
}
