using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class ChoferController : MonoBehaviour
{
    [Header("Velocidades")]
    public float velocidadCaminar = 4f;
    public float velocidadCorrer = 6.5f;
    public float gravedad = -15f;
    public float velocidadRotacion = 12f;

    [Header("Esquive (Dodge Roll)")]
    public float fuerzaEsquive = 10f;
    public float duracionEsquive = 0.35f;
    public float cooldownEsquive = 0.8f;
    private bool puedeEsquivar = true;
    private bool estaEsquivando = false;

    [Header("Animación")]
    public Animator animator;

    private CharacterController controller;
    private Vector3 velocidadVertical;
    private Transform camaraTransform;

    void Start()
    {
        controller = GetComponent<CharacterController>();
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
<<<<<<< Updated upstream
=======

        if (animator != null)
        {
            if (animator.runtimeAnimatorController == null)
            {
                #if UNITY_EDITOR
                RuntimeAnimatorController ctrl = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Game/Animations/Chofer/ChoferAnimV2.controller");
                if (ctrl == null)
                    ctrl = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Game/Animations/Chofer/ChoferAnimController.controller");
                if (ctrl != null) animator.runtimeAnimatorController = ctrl;
                #endif
            }

            if (animator.avatar == null)
            {
                #if UNITY_EDITOR
                var subassets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Game/Models/Chofer/Meshy_AI_chofer_trailer_3d_biped_Character_output.fbx");
                foreach (var obj in subassets)
                {
                    if (obj is Avatar av) { animator.avatar = av; break; }
                }
                #endif
            }
            animator.applyRootMotion = false;
        }

>>>>>>> Stashed changes
        if (Camera.main != null)
        {
            camaraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        if (estaEsquivando) return;

        AplicarGravedad();
<<<<<<< Updated upstream
=======

        if (controlBloqueado)
        {
            if (animator != null) animator.SetFloat("Velocidad", 0f, 0.15f, Time.deltaTime);
            return;
        }

        // Salvaguarda: si el control está libre, el Animator debe tener velocidad normal y estar en locomoción
        if (animator != null)
        {
            if (animator.speed < 0.1f) animator.speed = 1f;
            var infoEstado = animator.GetCurrentAnimatorStateInfo(0);
            if (infoEstado.IsName("Despertar"))
            {
                ReproducirEstado("Locomocion", 0.12f);
            }
        }

>>>>>>> Stashed changes
        ProcesarMovimiento();
        ProcesarEsquive();
    }

    void ProcesarMovimiento()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 direccionEntrada = new Vector3(h, 0f, v).normalized;
        float objetivoVelocidad = 0f;

        if (direccionEntrada.magnitude >= 0.1f)
        {
<<<<<<< Updated upstream
            // Moverse relativo hacia donde apunta la cámara
            Vector3 forwardCam = camaraTransform != null ? camaraTransform.forward : Vector3.forward;
            Vector3 rightCam = camaraTransform != null ? camaraTransform.right : Vector3.right;
            forwardCam.y = 0;
            rightCam.y = 0;
            forwardCam.Normalize();
            rightCam.Normalize();
=======
            // Salvaguarda: si por algún motivo el Animator quedó en pausa o en la animación de suelo "Despertar", ponerse de pie
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                if (animator.speed < 0.1f) animator.speed = 1f;
                var infoEstado = animator.GetCurrentAnimatorStateInfo(0);
                if (infoEstado.IsName("Despertar"))
                {
                    ReproducirEstado("Locomocion", 0.12f);
                }
            }

            Vector3 direccionMovimiento;
>>>>>>> Stashed changes

            Vector3 direccionMovimiento = (forwardCam * v + rightCam * h).normalized;

            // Rotar suavemente hacia la dirección de movimiento
            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccionMovimiento);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, velocidadRotacion * Time.deltaTime);

            // Determinar si corre o camina
            bool corriendo = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            float velocidadActual = corriendo ? velocidadCorrer : velocidadCaminar;

            controller.Move(direccionMovimiento * velocidadActual * Time.deltaTime);

            objetivoVelocidad = corriendo ? 2f : 1f;
        }

        if (animator != null)
        {
            // Amortiguación suave (dampTime) para mezclar transiciones con fluidez profesional
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
