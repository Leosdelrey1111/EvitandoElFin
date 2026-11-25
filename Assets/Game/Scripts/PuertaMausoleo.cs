using UnityEngine;

/// <summary>
/// Puerta del Gran Mausoleo en Nivel 2.
/// Se abre automáticamente cuando el jugador recolecta los 3 Cirios de Santa Paula.
/// </summary>
public class PuertaMausoleo : MonoBehaviour
{
    [Header("Configuración")]
    public int ciriosNecesarios = 3;
    public float velocidadApertura = 2f;

    private bool abierta = false;
    private bool abriendo = false;
    private Vector3 posicionInicial;
    private Vector3 posicionAbierta;
    private Renderer rend;
    private Collider col;

    void Start()
    {
        posicionInicial = transform.localPosition;
        posicionAbierta = posicionInicial + Vector3.up * 6f; // La puerta sube
        rend = GetComponent<Renderer>();
        col = GetComponent<Collider>();
    }

    void Update()
    {
        // Verificar si ya se recolectaron los 3 cirios
        if (!abierta && !abriendo)
        {
            InventarioEspiritual inv = Object.FindAnyObjectByType<InventarioEspiritual>();
            if (inv != null && inv.cantidadTotems >= ciriosNecesarios)
            {
                IniciarApertura();
            }
        }

        // Animar apertura
        if (abriendo)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, posicionAbierta, Time.deltaTime * velocidadApertura);
            
            // Hacer transparente gradualmente
            if (rend != null)
            {
                Color c = rend.material.color;
                c.a = Mathf.Lerp(c.a, 0f, Time.deltaTime * velocidadApertura * 0.5f);
                rend.material.color = c;
            }

            // Cuando llegue arriba, desactivar
            if (Vector3.Distance(transform.localPosition, posicionAbierta) < 0.5f)
            {
                abierta = true;
                abriendo = false;
                if (col != null) col.enabled = false;
                if (rend != null) rend.enabled = false;
                
                // Mensaje
                AvisosJuego.Mostrar("¡La puerta del Mausoleo se ha abierto!", 3f);
            }
        }
    }

    void IniciarApertura()
    {
        abriendo = true;
        
        // Efecto de sonido (opcional)
        AudioSource audio = gameObject.AddComponent<AudioSource>();
        audio.spatialBlend = 0.8f;
        audio.volume = 0.6f;
        
        // Generar sonido de piedra pesada deslizándose
        const int rate = 22050;
        const float dur = 3f;
        int n = (int)(rate * dur);
        float[] d = new float[n];
        System.Random rng = new System.Random(567);
        
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / rate;
            float ruido = (float)(rng.NextDouble() * 2.0 - 1.0);
            float env = 1f - (t / dur); // Fade out
            d[i] = ruido * 0.3f * env;
        }
        
        AudioClip clip = AudioClip.Create("PuertaPiedra", n, 1, rate, false);
        clip.SetData(d, 0);
        audio.clip = clip;
        audio.Play();
        
        Destroy(audio, dur + 0.5f);
        
        // Mensaje narrativo
        if (GuiaAlebrije.Instancia != null)
        {
            GuiaAlebrije.Instancia.Decir(HistoriaNivel1.VozPerfil.Tonalli, "El poder de los tres cirios de Santa Paula ha desbloqueado la entrada al Gran Mausoleo.", "mausoleo_abierto", false);
        }
        else
        {
            AvisosJuego.Mostrar("El poder de los tres cirios ha desbloqueado la entrada al Gran Mausoleo.", 4f);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Si el jugador intenta entrar sin los cirios
        if (!abierta && other.CompareTag("Player"))
        {
            InventarioEspiritual inv = other.GetComponent<InventarioEspiritual>();
            if (inv != null)
            {
                int faltantes = ciriosNecesarios - inv.cantidadTotems;
                if (faltantes > 0)
                {
                    string mensaje = faltantes == 1 
                        ? "Necesitas 1 cirio más para abrir esta puerta." 
                        : $"Necesitas {faltantes} cirios más para abrir esta puerta.";
                    AvisosJuego.Mostrar(mensaje, 2.5f);
                }
            }
        }
    }
}
