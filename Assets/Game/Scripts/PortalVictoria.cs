using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PortalVictoria : MonoBehaviour
{
    [Header("Efectos Visuales / Audio")]
    public GameObject efectosPortalActivo;
    public AudioSource audioSource;
    public AudioClip clipEntrada;

    [Header("Mensaje")]
    [Tooltip("Distancia mínima para checar")]
    public string tagJugador = "Player";

    private float ultimoAviso = -10f;
    private Vector3 escalaBase = Vector3.zero;
    private bool activado = false;
    private Transform jugador;

    void Start()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
        GameObject p = GameObject.FindGameObjectWithTag(tagJugador);
        if (p != null) jugador = p.transform;
    }

    void Update()
    {
        // El portal siempre es visible para saber a dónde ir, y cambia de intensidad cuando se desbloquea
        if (GameManager.Instancia != null && efectosPortalActivo != null)
        {
            bool desbloqueado = GameManager.Instancia.EstaPortalDesbloqueado();
            
            // Mantenerlo activo y rotar suavemente el óvalo
            efectosPortalActivo.SetActive(true);
            efectosPortalActivo.transform.Rotate(Vector3.up, (desbloqueado ? 60f : 12f) * Time.deltaTime, Space.Self);
            // sellado = vórtice apagado y pequeño; abierto = crece a su tamaño completo
            if (escalaBase == Vector3.zero) escalaBase = efectosPortalActivo.transform.localScale;
            Vector3 meta = desbloqueado ? escalaBase : new Vector3(escalaBase.x * 0.35f, escalaBase.y * 0.35f, escalaBase.z);
            efectosPortalActivo.transform.localScale = Vector3.Lerp(efectosPortalActivo.transform.localScale, meta, 2.5f * Time.deltaTime);
        }

        // Respaldo por proximidad: si el jugador ya está dentro del portal desbloqueado aunque no haya disparado OnTriggerEnter
        if (!activado && GameManager.Instancia != null && GameManager.Instancia.EstaPortalDesbloqueado())
        {
            if (jugador == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag(tagJugador);
                if (p != null) jugador = p.transform;
            }
            if (jugador != null)
            {
                Vector3 d = jugador.position - transform.position;
                float distXZ = new Vector2(d.x, d.z).magnitude;
                if (distXZ <= 3.0f && Mathf.Abs(d.y) <= 4.5f)
                {
                    EntrarPortal();
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(tagJugador)) IntentarCruzar();
    }

    private void OnTriggerStay(Collider other)
    {
        if (!activado && other.CompareTag(tagJugador)) IntentarCruzar();
    }

    void IntentarCruzar()
    {
        if (activado || GameManager.Instancia == null) return;
        if (GameManager.Instancia.EstaPortalDesbloqueado())
        {
            EntrarPortal();
        }
        else if (Time.time - ultimoAviso > 3f)
        {
            ultimoAviso = Time.time;
            bool faltaJefe = GameManager.Instancia.jefeRequerido && GameManager.Instancia.TotemsCompletos();
            AvisosJuego.Mostrar(faltaJefe
                ? "El portal sigue sellado: derrota al jefe para cruzarlo."
                : "El portal está sellado. Reúne los " + GameManager.Instancia.totemsRequeridos + " objetivos para abrirlo.", 3f);
        }
    }

    void EntrarPortal()
    {
        if (activado) return;
        activado = true;
        if (audioSource != null && clipEntrada != null) audioSource.PlayOneShot(clipEntrada);

        string escena = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (escena.Contains("Nivel3") || escena.Contains("Ciudad"))
        {
            if (!CinematicaRegresoAlma.Reproducir(() => { if (GameManager.Instancia != null) GameManager.Instancia.ProcesarVictoria(); }))
            {
                GameManager.Instancia.ProcesarVictoria();
            }
        }
        else
        {
            GameManager.Instancia.ProcesarVictoria();
        }
    }
}
