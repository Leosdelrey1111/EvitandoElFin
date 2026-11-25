using UnityEngine;

/// <summary>
/// Componente que se coloca en el GameManager de cada nivel para inicializar
/// sistemas compartidos como el de presentación de enemigos.
/// </summary>
public class InicializadorNivel : MonoBehaviour
{
    void Awake()
    {
        // Asegurar que existe el sistema de presentación de enemigos
        if (SistemaPresentacionEnemigos.Instancia == null)
        {
            GameObject go = new GameObject("SistemaPresentacionEnemigos");
            go.AddComponent<SistemaPresentacionEnemigos>();
        }
    }
}
