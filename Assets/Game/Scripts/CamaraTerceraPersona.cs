using UnityEngine;

/// <summary>
/// Control de cámara orbital en tercera persona suave y cinematográfico.
/// Permite mover la cámara libremente con el mouse y controla el cursor de forma natural.
/// </summary>
public class CamaraTerceraPersona : MonoBehaviour
{
    [Header("Objetivo")]
    [Tooltip("El Chofer a quien sigue la cámara. Si está vacío, se busca automáticamente.")]
    public Transform objetivo;
    public Vector3 desplazamientoObjetivo = new Vector3(0f, 1.4f, 0f); // Mira al pecho/hombros del Chofer

    [Header("Distancia y Ángulo")]
    public float distancia = 5.0f;
    public float distanciaMin = 2.5f;
    public float distanciaMax = 9.0f;
    public float sensibilidadZoom = 2.5f;

    [Header("Sensibilidad del Mouse")]
    public float sensibilidadX = 140f;
    public float sensibilidadY = 110f;
    public float limiteVerticalMin = -20f; // No meterse bajo el asfalto
    public float limiteVerticalMax = 65f;  // Vista cenital alta

    [Header("Suavizado (Cinematográfico)")]
    public float suavizadoMovimiento = 12f;
    public float suavizadoRotacion = 15f;

    [Header("Colisión con el Terreno / Montañas")]
    public bool evitarAtravesarMontanias = true;
    public float radioColisionCamara = 0.3f;
    public LayerMask capasObstaculos = ~0; // Todo excepto el propio jugador

    private float rotacionX = 0f; // Yaw (Horizontal)
    private float rotacionY = 15f; // Pitch (Vertical)
    private float distanciaDeseada;
    private float distanciaActual;
    private Vector3 posicionSuavizada;

    void Awake()
    {
        // Si hay CinemachineBrain en la cámara, desactivarlo para que este script tenga el control fluido
        var cinemachineBrain = GetComponent("CinemachineBrain");
        if (cinemachineBrain != null && cinemachineBrain is Behaviour mb)
        {
            mb.enabled = false;
        }
    }

    void Start()
    {
        if (objetivo == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) objetivo = player.transform;
        }

        distanciaDeseada = distancia;
        distanciaActual = distancia;

        // Iniciar con la rotación actual de la cámara
        Vector3 angulos = transform.eulerAngles;
        rotacionX = angulos.y;
        rotacionY = angulos.x;

        // Bloquear cursor para control natural estilo juego de PC
        BloquearCursor(true);
    }

    void Update()
    {
        // Liberar cursor al presionar Escape o Alt, bloquear al hacer click
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.LeftAlt))
        {
            BloquearCursor(false);
        }
        else if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            BloquearCursor(true);
        }

        // Zoom con la rueda del ratón
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            distanciaDeseada = Mathf.Clamp(distanciaDeseada - scroll * sensibilidadZoom, distanciaMin, distanciaMax);
        }
    }

    void LateUpdate()
    {
        if (objetivo == null) return;

        // 1. Leer entrada del mouse cuando el cursor está bloqueado
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            rotacionX += Input.GetAxis("Mouse X") * sensibilidadX * Time.deltaTime;
            rotacionY -= Input.GetAxis("Mouse Y") * sensibilidadY * Time.deltaTime;
            rotacionY = Mathf.Clamp(rotacionY, limiteVerticalMin, limiteVerticalMax);
        }

        // 2. Calcular rotación orbital
        Quaternion rotacionObjetivo = Quaternion.Euler(rotacionY, rotacionX, 0f);

        // 3. Punto de mira (centro del Chofer)
        Vector3 puntoPivote = objetivo.position + desplazamientoObjetivo;

        // 4. Prevenir colisión con montañas o terreno
        float distanciaFinal = distanciaDeseada;
        if (evitarAtravesarMontanias)
        {
            Vector3 direccionDesdePivote = rotacionObjetivo * -Vector3.forward;
            RaycastHit hit;
            // Excluir capas del propio jugador
            int mascara = capasObstaculos & ~(1 << objetivo.gameObject.layer);

            if (Physics.SphereCast(puntoPivote, radioColisionCamara, direccionDesdePivote, out hit, distanciaDeseada, mascara))
            {
                distanciaFinal = Mathf.Clamp(hit.distance - 0.1f, distanciaMin, distanciaDeseada);
            }
        }

        distanciaActual = Mathf.Lerp(distanciaActual, distanciaFinal, Time.deltaTime * 10f);

        // 5. Posición deseada
        Vector3 posicionObjetivo = puntoPivote + rotacionObjetivo * (Vector3.back * distanciaActual);

        // 6. Aplicar posición y rotación suavizadas
        transform.position = Vector3.Lerp(transform.position, posicionObjetivo, Time.deltaTime * suavizadoMovimiento);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, Time.deltaTime * suavizadoRotacion);
    }

    public void BloquearCursor(bool bloquear)
    {
        Cursor.lockState = bloquear ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !bloquear;
    }
}
