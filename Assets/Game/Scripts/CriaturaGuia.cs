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

    [Header("Narrador / Pista")]
    [Tooltip("Si no es null, el alebrije vuela hacia ese objetivo para indicar el camino (lo asigna GuiaAlebrije).")]
    public Transform apuntarA;
    [Tooltip("Lo activa GuiaAlebrije mientras habla: la luz brilla más y el alebrije late.")]
    public bool hablando;
    public float distanciaPista = 4.5f;

    private float brilloActual = 1f;
    private Vector3 escalaBase;
    private bool escalaGuardada;

    void LateUpdate()
    {
        if (objetivoChofer == null) return;
        if (!escalaGuardada) { escalaBase = transform.localScale; escalaGuardada = true; }

        Vector3 posicionDeseada;
        Quaternion rotacionDeseada;

        if (apuntarA != null)
        {
            // Pista: se adelanta hacia el objetivo, a la altura de los ojos
            Vector3 hacia = apuntarA.position - objetivoChofer.position; hacia.y = 0f;
            if (hacia.sqrMagnitude < 0.01f) hacia = objetivoChofer.forward;
            hacia.Normalize();
            posicionDeseada = objetivoChofer.position + hacia * distanciaPista + Vector3.up * 2.4f;
            rotacionDeseada = Quaternion.LookRotation(hacia);
        }
        else
        {
            // Posición relativa al Chofer (flotando sobre el hombro)
            posicionDeseada = objetivoChofer.position
                + objetivoChofer.right * desplazamiento.x
                + Vector3.up * desplazamiento.y
                + objetivoChofer.forward * desplazamiento.z;
            rotacionDeseada = Quaternion.LookRotation(objetivoChofer.forward);
        }

        // Efecto ondulante de vuelo
        posicionDeseada.y += Mathf.Sin(Time.time * velocidadFlotacion) * alturaFlotacion;

        // Movimiento amortiguado
        transform.position = Vector3.Lerp(transform.position, posicionDeseada, suavizadoSeguimiento * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotacionDeseada, suavizadoSeguimiento * Time.deltaTime);

        // Brillo y latido al hablar
        brilloActual = Mathf.MoveTowards(brilloActual, hablando ? 1.7f : 1f, 3f * Time.deltaTime);
        if (luzEspiritual != null) luzEspiritual.intensity = intensidadLuz * brilloActual;
        float latido = hablando ? 1f + Mathf.Sin(Time.time * 11f) * 0.06f : 1f;
        transform.localScale = escalaBase * latido;
    }
}
