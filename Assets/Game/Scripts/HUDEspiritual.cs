using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDEspiritual : MonoBehaviour
{
    [Header("Referencias del Jugador")]
    public PoderEspiritual vidaChofer;
    public InventarioEspiritual inventarioChofer;

    [Header("Elementos de la Interfaz")]
    public Slider sliderPoderEspiritual;
    public TextMeshProUGUI textoTotems;
    public TextMeshProUGUI textoRecuerdos;

    void Start()
    {
        // Buscar automáticamente referencias si no se asignaron en el Inspector
        if (vidaChofer == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                vidaChofer = player.GetComponent<PoderEspiritual>();
                inventarioChofer = player.GetComponent<InventarioEspiritual>();
            }
        }

        if (vidaChofer != null)
        {
            if (sliderPoderEspiritual != null)
            {
                sliderPoderEspiritual.maxValue = vidaChofer.maxPoder;
                sliderPoderEspiritual.value = vidaChofer.actualPoder;
            }
            vidaChofer.OnCambioPoder += ActualizarBarraVida;
        }

        if (inventarioChofer != null)
        {
            ActualizarTextoTotems(inventarioChofer.cantidadTotems);
            inventarioChofer.OnCambioTotems += ActualizarTextoTotems;
            inventarioChofer.OnRecuerdoObtenido += NotificarRecuerdo;
        }
    }

    void ActualizarBarraVida(float nuevaVida)
    {
        if (sliderPoderEspiritual != null)
        {
            sliderPoderEspiritual.value = nuevaVida;
        }
    }

    void ActualizarTextoTotems(int totalTotems)
    {
        if (textoTotems != null)
        {
            textoTotems.text = $"Tótems: {totalTotems}";
        }
    }

    void NotificarRecuerdo(string nombreRecuerdo)
    {
        if (textoRecuerdos != null)
        {
            textoRecuerdos.text = $"Recuerdo: {nombreRecuerdo}";
        }
    }

    void OnDestroy()
    {
        if (vidaChofer != null) vidaChofer.OnCambioPoder -= ActualizarBarraVida;
        if (inventarioChofer != null)
        {
            inventarioChofer.OnCambioTotems -= ActualizarTextoTotems;
            inventarioChofer.OnRecuerdoObtenido -= NotificarRecuerdo;
        }
    }
}
