using UnityEngine;

/// <summary>Hace que el objeto siempre mire a la cámara (halos, destellos).</summary>
public class MirarCamara : MonoBehaviour
{
    void LateUpdate()
    {
        Camera c = Camera.main;
        if (c != null) transform.rotation = c.transform.rotation;
    }
}
