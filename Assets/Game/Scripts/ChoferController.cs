using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class ChoferController : MonoBehaviour
{
    [Header("Velocidades")]
    public float velocidadCaminar = 4.5f;
    public float velocidadCorrer = 7.0f;
    public float gravedad = -15f;
    public float velocidadRotacion = 14f;

    public enum ModoControl { RelativoACamara, LibreMundo }
    [Header("Modo de Movimiento")]
    [Tooltip("RelativoACamara: El Chofer avanza hacia donde apunta la cámara con el mouse (natural de acción/RPG). LibreMundo: Ejes globales X/Z.")]
    public ModoControl modoControl = ModoControl.RelativoACamara;

    [Header("Esquive (Dodge Roll)")]
    public float fuerzaEsquive = 10f;
    public float duracionEsquive = 0.35f;
    public float cooldownEsquive = 0.8f;
    private bool puedeEsquivar = true;
    private bool estaEsquivando = false;

    [Header("Estado Aturdido (Despertar del alma)")]
    [Tooltip("Si es true, el Chofer no responde al input (solo gravedad).")]
    public bool controlBloqueado = false;
    [Tooltip("0 = normal, 1 = muy confundido: camina más lento y se desvía como mareado.")]
    [Range(0f, 1f)]
    public float confusion = 0f;

    [Header("Animación")]
    public Animator animator;

    private CharacterController controller;
    private Vector3 velocidadVertical;
    private Transform camaraTransform;

    void Awake()
    {
        CentrarModeloVisual();
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.center = Vector3.zero;
            controller.height = 2f;
            controller.radius = 0.5f;
        }

        CentrarModeloVisual();

        if (animator == null)
        {
            Animator[] anims = GetComponentsInChildren<Animator>();
            foreach (var a in anims)
            {
                if (a.gameObject != this.gameObject && a.runtimeAnimatorController != null)
                {
                    animator = a;
                    break;
                }
            }
            if (animator == null && anims.Length > 0)
            {
                animator = anims[anims.Length - 1];
            }
        }

        if (Camera.main != null)
        {
            camaraTransform = Camera.main.transform;
            CamaraTerceraPersona cam3ra = Camera.main.GetComponent<CamaraTerceraPersona>();
            if (cam3ra == null)
            {
                cam3ra = Camera.main.gameObject.AddComponent<CamaraTerceraPersona>();
            }
            cam3ra.objetivo = transform;
        }
    }

    /// <summary>
    /// Elimina cualquier desfase en X o Z del modelo hijo para que rote sobre su propio cuerpo
    /// y no gire orbitando como satélite alrededor de un eje central.
    /// </summary>
    public void CentrarModeloVisual()
    {
        foreach (Transform hijo in transform)
        {
            if (hijo != transform && (hijo.GetComponentInChildren<Animator>() != null || hijo.name.ToLower().Contains("chofer")))
            {
                hijo.localPosition = new Vector3(0f, -1f, 0f);
                hijo.localRotation = Quaternion.identity;
            }
        }
    }

    void Update()
    {
        if (estaEsquivando) return;

        AplicarGravedad();

        if (controlBloqueado)
        {
            if (animator != null) animator.SetFloat("Velocidad", 0f, 0.15f, Time.deltaTime);
            return;
        }

        ProcesarMovimiento();
        ProcesarEsquive();
    }

    void ProcesarMovimiento()
    {
        // Lectura de entrada con soporte directo para WASD y flechas
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h = -1f;
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h = 1f;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v = 1f;
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v = -1f;

        Vector3 direccionEntrada = new Vector3(h, 0f, v);
        if (direccionEntrada.sqrMagnitude > 1f)
        {
            direccionEntrada.Normalize();
        }

        float objetivoVelocidad = 0f;

        if (direccionEntrada.sqrMagnitude >= 0.01f)
        {
            Vector3 direccionMovimiento;

            if (modoControl == ModoControl.LibreMundo)
            {
                // Movimiento libre en los ejes del mundo (W = +Z, S = -Z, A = -X, D = +X)
                direccionMovimiento = direccionEntrada;
            }
            else
            {
                // Moverse relativo hacia donde apunta la cámara
                Transform cam = camaraTransform != null ? camaraTransform : (Camera.main != null ? Camera.main.transform : null);
                if (cam != null)
                {
                    Vector3 forwardCam = cam.forward;
                    Vector3 rightCam = cam.right;
                    forwardCam.y = 0;
                    rightCam.y = 0;

                    if (forwardCam.sqrMagnitude < 0.001f) forwardCam = Vector3.forward;
                    if (rightCam.sqrMagnitude < 0.001f) rightCam = Vector3.right;

                    forwardCam.Normalize();
                    rightCam.Normalize();

                    direccionMovimiento = (forwardCam * v + rightCam * h).normalized;
                }
                else
                {
                    direccionMovimiento = direccionEntrada;
                }
            }

            if (direccionMovimiento.sqrMagnitude >= 0.001f)
            {
                // Aturdimiento: la dirección se desvía con una oscilación lenta (mareo)
                if (confusion > 0.01f)
                {
                    float desvio = Mathf.Sin(Time.time * 2.3f) * 38f * confusion;
                    direccionMovimiento = Quaternion.Euler(0f, desvio, 0f) * direccionMovimiento;
                }

                // Rotar suavemente hacia la dirección de movimiento
                Quaternion rotacionObjetivo = Quaternion.LookRotation(direccionMovimiento);
                transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, velocidadRotacion * Time.deltaTime);

                // Determinar si corre o camina (no se puede correr aturdido)
                bool corriendo = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && confusion < 0.3f;
                float velocidadActual = corriendo ? velocidadCorrer : velocidadCaminar;
                velocidadActual *= Mathf.Lerp(1f, 0.45f, confusion);

                controller.Move(direccionMovimiento * velocidadActual * Time.deltaTime);
                objetivoVelocidad = corriendo ? 2f : 1f;
            }
        }

        if (animator != null)
        {
            // Amortiguación suave para transiciones de animación
            animator.SetFloat("Velocidad", objetivoVelocidad, 0.15f, Time.deltaTime);
        }
    }

    void AplicarGravedad()
    {
        if (controller.isGrounded && velocidadVertical.y < 0)
        {
            velocidadVertical.y = -2f;
        }

        velocidadVertical.y += gravedad * Time.deltaTime;
        controller.Move(velocidadVertical * Time.deltaTime);
    }

    void ProcesarEsquive()
    {
        if (Input.GetKeyDown(KeyCode.Space) && puedeEsquivar && controller.isGrounded)
        {
            if (animator != null)
            {
                animator.SetTrigger("Esquivar");
            }
            StartCoroutine(RutinaEsquive());
        }
    }

    IEnumerator RutinaEsquive()
    {
        puedeEsquivar = false;
        estaEsquivando = true;

        Vector3 direccionEsquive = transform.forward;
        float tiempo = 0f;

        while (tiempo < duracionEsquive)
        {
            controller.Move(direccionEsquive * fuerzaEsquive * Time.deltaTime);
            tiempo += Time.deltaTime;
            yield return null;
        }

        estaEsquivando = false;
        yield return new WaitForSeconds(cooldownEsquive);
        puedeEsquivar = true;
    }
}
