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
            efectosPortalActivo.transform.Rotate(Vector3.up, 25f * Time.deltaTime, Space.Self);
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
                    Debug.Log($"[PortalVictoria] El portal está sellado. Necesitas reunir todos los tótems primero ({GameManager.Instancia.totemsRequeridos}).");
                }
            }
        }
    }
}
