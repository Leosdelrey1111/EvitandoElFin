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

    private Vector3 posicionInicial;

    void Start()
    {
        posicionInicial = transform.position;
    }

    void Update()
    {
        // Giro constante sobre su propio eje Y
        transform.Rotate(Vector3.up, velocidadRotacion * Time.deltaTime, Space.World);

        // Flotación suave arriba y abajo (estilo místico)
        float nuevoY = posicionInicial.y + Mathf.Sin(Time.time * frecuenciaFlotacion) * amplitudFlotacion;
        transform.position = new Vector3(transform.position.x, nuevoY, transform.position.z);
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
