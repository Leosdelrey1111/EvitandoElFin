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

    [Header("Identificador Persistente")]
    public string idTotem = "";

    void Start()
    {
        posicionInicialLocal = transform.localPosition;
        if (string.IsNullOrEmpty(idTotem))
        {
            string nom = (transform.parent != null && (name == "Cristal" || name == "Gema")) ? transform.parent.name : gameObject.name;
            idTotem = nom + "_" + Mathf.RoundToInt(transform.position.x) + "_" + Mathf.RoundToInt(transform.position.z);
        }

        if (SistemaGuardado.EsTotemRecogido(idTotem))
        {
            Destroy(gameObject);
        }
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

    private bool recogido = false;

    void OnTriggerEnter(Collider other)
    {
        if (recogido) return;
        if (other.CompareTag("Player"))
        {
            recogido = true;

            // Sumar al inventario espiritual
            if (InventarioEspiritual.Instancia != null)
            {
                InventarioEspiritual.Instancia.AgregarTotems(valorTotem);
                SistemaGuardado.RegistrarTotem(idTotem, InventarioEspiritual.Instancia.cantidadTotems);

                // Disparar narración apropiada según el nivel y número de tótem
                DispararNarracionTotem(InventarioEspiritual.Instancia.cantidadTotems);
            }

            // Si aporta curación de vitalidad del alma
            PoderEspiritual vidaChofer = other.GetComponent<PoderEspiritual>();
            if (vidaChofer != null && curacionEspiritual > 0)
            {
                vidaChofer.Curar(curacionEspiritual);
            }

            // Si es un Cirio (Nivel 2), encender la flama en el pedestal en lugar de apagarla
            Transform raiz = transform.parent;
            Transform flama = raiz != null ? raiz.Find("FlamaEncendida") : transform.Find("FlamaEncendida");
            if (flama != null)
            {
                flama.gameObject.SetActive(true);
                Transform ind = transform.Find("IndicadorApagado");
                if (ind != null) ind.gameObject.SetActive(false);
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
