using UnityEngine;

public class CriaturaGuia : MonoBehaviour
{
    public enum TipoAcompaniante { Alebrije, MomiaPequena, Charamusca }

    [Header("Tipo de Guía")]
    public TipoAcompaniante tipo = TipoAcompaniante.Alebrije;

    [Header("Seguimiento del Chofer")]
    public Transform objetivoChofer;
    public Vector3 desplazamiento = new Vector3(1.2f, 1.8f, -0.8f); // A la derecha y flotando sobre el hombro
    public float suavizadoSeguimiento = 5f;
    public float velocidadFlotacion = 3f;
    public float alturaFlotacion = 0.25f;

    [Header("Luz Espiritual")]
    public Light luzEspiritual;
    public Color colorLuzAlebrije = new Color(0.2f, 0.9f, 1f); // Celeste místico
    public float intensidadLuz = 2.5f;

    void Start()
    {
        if (objetivoChofer == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) objetivoChofer = player.transform;
        }

        if (luzEspiritual == null)
        {
            luzEspiritual = GetComponentInChildren<Light>();
        }

        if (luzEspiritual != null)
        {
            luzEspiritual.color = colorLuzAlebrije;
            luzEspiritual.intensity = intensidadLuz;
        }
    }

    void LateUpdate()
    {
        if (objetivoChofer == null) return;

        // Calcular posición objetivo relativa al Chofer
        Vector3 posicionDeseada = objetivoChofer.position 
            + objetivoChofer.right * desplazamiento.x 
            + Vector3.up * desplazamiento.y 
            + objetivoChofer.forward * desplazamiento.z;

        // Efecto ondulante de vuelo
        float balanceo = Mathf.Sin(Time.time * velocidadFlotacion) * alturaFlotacion;
        posicionDeseada.y += balanceo;

        // Movimiento amortiguado
        transform.position = Vector3.Lerp(transform.position, posicionDeseada, suavizadoSeguimiento * Time.deltaTime);

        // Mirar suavemente hacia donde mira el Chofer o hacia donde camina
        Quaternion rotacionDeseada = Quaternion.LookRotation(objetivoChofer.forward);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotacionDeseada, suavizadoSeguimiento * Time.deltaTime);
    }
}
