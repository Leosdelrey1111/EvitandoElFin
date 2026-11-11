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

    private static AudioClip clipCampana;

    static AudioClip ClipCampana()
    {
        if (clipCampana != null) return clipCampana;
        int rate = 22050, n = (int)(rate * 1.4f);
        float[] d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate;
            float env = Mathf.Exp(-t * 3.2f);
            d[i] = (Mathf.Sin(2f * Mathf.PI * 523f * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * 784f * t) * 0.3f + Mathf.Sin(2f * Mathf.PI * 1318f * t) * 0.15f) * env * 0.6f;
        }
        clipCampana = AudioClip.Create("CampanaTotem", n, 1, rate, false);
        clipCampana.SetData(d, 0);
        return clipCampana;
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

            // Aviso en pantalla y sonido de campana espiritual
            if (InventarioEspiritual.Instancia != null && GameManager.Instancia != null)
            {
                int tengo = InventarioEspiritual.Instancia.cantidadTotems;
                int pido = GameManager.Instancia.totemsRequeridos;
                AvisosJuego.Mostrar(tengo >= pido
                    ? "¡Tienes los " + pido + " tótems! El portal espiritual se ha abierto."
                    : "Tótem recogido (" + tengo + "/" + pido + ")", 3.5f);
            }
            AudioSource.PlayClipAtPoint(ClipCampana(), transform.position, 0.8f);
            Destroy(gameObject);
        }
    }
}
