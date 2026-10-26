using UnityEngine;

public class TotemColeccionable : MonoBehaviour
{
    [Header("Recompensa")]
    public int valorTotem = 1;
    public float curacionEspiritual = 10f;

    [Header("Efecto de Animación")]
    public float velocidadRotacion = 60f;
    public float amplitudFlotacion = 0.25f;
    public float frecuenciaFlotacion = 2f;

    private Vector3 posicionInicialLocal;

    void Start()
    {
        posicionInicialLocal = transform.localPosition;
    }

    void Update()
    {
        // Giro constante sobre su propio eje local Y (para no orbitar)
        transform.Rotate(Vector3.up, velocidadRotacion * Time.deltaTime, Space.Self);

        // Flotación suave arriba y abajo (estilo místico)
        float nuevoY = posicionInicialLocal.y + Mathf.Sin(Time.time * frecuenciaFlotacion) * amplitudFlotacion;
        transform.localPosition = new Vector3(posicionInicialLocal.x, nuevoY, posicionInicialLocal.z);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Sumar al inventario espiritual
            if (InventarioEspiritual.Instancia != null)
            {
                InventarioEspiritual.Instancia.AgregarTotems(valorTotem);
            }

            // Si aporta curación de vitalidad del alma
            PoderEspiritual vidaChofer = other.GetComponent<PoderEspiritual>();
            if (vidaChofer != null && curacionEspiritual > 0)
            {
                vidaChofer.Curar(curacionEspiritual);
            }

            Debug.Log($"[Tótem] Tótem recogido por el Chofer. (+{valorTotem} Tótem, +{curacionEspiritual} Vida)");
            Destroy(gameObject);
        }
    }
}
