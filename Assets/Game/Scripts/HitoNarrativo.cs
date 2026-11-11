using UnityEngine;

/// <summary>
/// Marcador en el mapa: cuando el Chofer entra en su radio, el alebrije cuenta la historia con ese id
/// (ver HistoriaNivel1). Cada hito se dispara una sola vez.
/// </summary>
public class HitoNarrativo : MonoBehaviour
{
    public string idHistoria = "sierra";
    public float radio = 14f;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, radio);
    }
}
