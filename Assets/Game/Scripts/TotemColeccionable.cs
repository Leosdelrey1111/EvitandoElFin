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

    [Header("Identificador Persistente")]
    public string idTotem = "";

    void Start()
    {
        posicionInicial = transform.position;
        if (string.IsNullOrEmpty(idTotem))
        {
            string nom = (transform.parent != null && (name == "Cristal" || name == "Gema")) ? transform.parent.name : gameObject.name;
            idTotem = nom + "_" + Mathf.RoundToInt(transform.position.x) + "_" + Mathf.RoundToInt(transform.position.z);
        }

        // Si estamos continuando el mismo nivel y este tótem ya fue recogido en la partida guardada,
        // marcarlo como ya completado (o dejar el cirio encendido en Nivel 2) sin romper los restantes.
        bool enMismaEscena = SistemaGuardado.Datos != null &&
                             SistemaGuardado.Datos.escenaActual == UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        int totemsGuardados = SistemaGuardado.Datos != null ? SistemaGuardado.Datos.totemsNivelActual : 0;
        if (enMismaEscena && totemsGuardados > 0 && SistemaGuardado.EsTotemRecogido(idTotem))
        {
            recogido = true;
            Transform raiz = transform.parent;
            Transform flama = raiz != null ? raiz.Find("FlamaEncendida") : transform.Find("FlamaEncendida");
            if (flama != null)
            {
                flama.gameObject.SetActive(true);
                Transform ind = transform.Find("IndicadorApagado");
                if (ind != null) ind.gameObject.SetActive(false);
            }
            if (raiz != null && raiz.name.StartsWith("Totem_"))
                Destroy(gameObject);
            else
            {
                Collider col = GetComponent<Collider>();
                if (col != null) col.enabled = false;
                enabled = false;
            }
        }
    }

    /// <summary>Vuelve a tomar la posición actual como centro de la flotación (tras acomodarlo en el suelo).</summary>
    public void Reubicar()
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

    private bool recogido = false;

    void OnTriggerEnter(Collider other)
    {
        if (recogido) return;
        if (other.CompareTag("Player"))
        {
            recogido = true;

            // Sumar al inventario espiritual y guardar avance gradual (tótem + almas + posición)
            if (InventarioEspiritual.Instancia != null)
            {
                InventarioEspiritual.Instancia.AgregarTotems(valorTotem);
                SistemaGuardado.SincronizarAlmas(Mathf.RoundToInt(InventarioEspiritual.Instancia.energiaEspiritual));
                SistemaGuardado.RegistrarCheckpoint("Totem_" + idTotem, other.transform.position, other.transform.rotation,
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
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

            if (raiz != null && raiz.name.StartsWith("Totem_"))
                Destroy(gameObject);
            else
            {
                Collider col = GetComponent<Collider>();
                if (col != null) col.enabled = false;
                enabled = false;
            }
        }
    }

    void DispararNarracionTotem(int numeroTotem)
    {
        string nombreEscena = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string idHistoria = "";

        // Detectar nivel actual y usar historia apropiada
        if (nombreEscena.Contains("Nivel1") || nombreEscena.Contains("Bosque"))
        {
            // Nivel 1: Totems de La Bufa
            switch (numeroTotem)
            {
                case 1: idHistoria = "totem1"; break;
                case 2: idHistoria = "totem2"; break;
                case 3: idHistoria = "totem3"; break;
            }
        }
        else if (nombreEscena.Contains("Nivel2") || nombreEscena.Contains("Panteon"))
        {
            // Nivel 2: Cirios de Santa Paula
            switch (numeroTotem)
            {
                case 1: idHistoria = "cirio1"; break;
                case 2: idHistoria = "cirio2"; break;
                case 3: idHistoria = "cirio3"; break;
            }
        }
        else if (nombreEscena.Contains("Nivel3") || nombreEscena.Contains("Ciudad"))
        {
            // Nivel 3: Sellos Inquisitoriales
            switch (numeroTotem)
            {
                case 1: idHistoria = "sello1"; break;
                case 2: idHistoria = "sello2"; break;
                case 3: idHistoria = "sello3"; break;
            }
        }

        // Disparar historia si existe GuiaAlebrije
        if (!string.IsNullOrEmpty(idHistoria) && GuiaAlebrije.Instancia != null)
        {
            GuiaAlebrije.Instancia.ContarHistoria(idHistoria);
        }
    }
}
