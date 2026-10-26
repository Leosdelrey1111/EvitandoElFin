using UnityEngine;

/// <summary>
/// Proporciona una animación procedural sutil de viento para magueyes, nopales y cactus
/// de la sierra de Guanajuato, dando ese toque "semi-animado" al entorno del juego.
/// </summary>
public class VientoVegetacion : MonoBehaviour
{
    [Header("Configuración del Viento")]
    public float velocidadViento = 1.8f;
    public float intensidadGiro = 2.5f;   // Grados de oscilación
    public float intensidadEscala = 0.03f; // Respiración sutil

    private Vector3 escalaOriginal;
    private Quaternion rotacionOriginal;
    private float desfaseAleatorio;

    void Start()
    {
        escalaOriginal = transform.localScale;
        rotacionOriginal = transform.localRotation;
        // Cada planta oscila con un tiempo ligeramente desfasado para naturalidad
        desfaseAleatorio = Random.Range(0f, 10f);
    }

    void Update()
    {
        float tiempo = (Time.time * velocidadViento) + desfaseAleatorio;

        // Balanceo senoidal suave simulando brisa de sierra
        float balanceoZ = Mathf.Sin(tiempo) * intensidadGiro;
        float balanceoX = Mathf.Cos(tiempo * 0.7f) * (intensidadGiro * 0.6f);

        transform.localRotation = rotacionOriginal * Quaternion.Euler(balanceoX, 0f, balanceoZ);

        // Ligera respiración de escala (efecto orgánico semi-animado)
        if (intensidadEscala > 0f)
        {
            float respiracion = 1f + (Mathf.Sin(tiempo * 1.4f) * intensidadEscala);
            transform.localScale = escalaOriginal * respiracion;
        }
    }
}
