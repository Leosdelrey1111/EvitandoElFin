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

    void Start()
    {
        // Asegurar que el collider sea trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
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
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(tagJugador))
        {
            if (GameManager.Instancia != null)
            {
                if (GameManager.Instancia.EstaPortalDesbloqueado())
                {
                    if (audioSource != null && clipEntrada != null)
                    {
                        audioSource.PlayOneShot(clipEntrada);
                    }
                    GameManager.Instancia.ProcesarVictoria();
                }
                else
                {
                    if (Time.time - ultimoAviso > 3f)
                    {
                        ultimoAviso = Time.time;
                        bool faltaJefe = GameManager.Instancia.jefeRequerido && GameManager.Instancia.TotemsCompletos();
                        AvisosJuego.Mostrar(faltaJefe
                            ? "El portal sigue sellado: el hechizo de La Bufa aún lo retiene."
                            : "El portal está sellado. Reúne los " + GameManager.Instancia.totemsRequeridos + " tótems para abrirlo.", 3f);
                    }
                }
            }
        }
    }
}
