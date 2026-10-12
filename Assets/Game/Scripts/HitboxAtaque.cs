using UnityEngine;

public class HitboxAtaque : MonoBehaviour
{
    public float danio = 25f;
    public string tagObjetivo = "Enemigo"; // O "Player" para enemigos

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(tagObjetivo))
        {
            PoderEspiritual objetivo = other.GetComponent<PoderEspiritual>();
            if (objetivo != null)
            {
                objetivo.RecibirDanio(danio);
                Debug.Log($"Golpe a {other.name} causando {danio} de daño");
            }
        }
    }
}
