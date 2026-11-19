using UnityEngine;
using System.Collections;

public class SistemaCombateChofer : MonoBehaviour
{
    [Header("Configuración de Ataques Melé (Combo)")]
    [Tooltip("Hitbox asignada como hijo del Chofer")]
    public GameObject hitboxMele;
    public float danioAtaque1 = 20f;
    public float danioAtaque2 = 30f;
    public float danioAtaque3 = 45f;

    [Header("Tiempos de Combo")]
    public float tiempoVentanaCombo = 1.0f; // Tiempo máximo entre ataques para seguir el combo
    public float duracionGolpe = 0.25f;    // Cuánto tiempo está activa la hitbox
    public float cooldownEntreGolpes = 0.35f;

    [Header("Ataque de Área (Onda Espiritual)")]
    public KeyCode teclaAtaqueArea = KeyCode.Q;
    public float radioArea = 5f;
    public float danioArea = 50f;
    public float cooldownArea = 5f;
<<<<<<< Updated upstream
    private float tiempoProximoAtaqueArea = 0f;
=======

    [Header("Feedback")]
    public Color colorEspiritual = new Color(0.35f, 0.95f, 1f, 1f);
    [Range(0f, 1f)] public float volumenSfx = 0.8f;

    // ---------- estado público para el HUD ----------
    public int PasoCombo { get { return comboPaso; } }
    /// <summary>0 = recién usada, 1 = lista para usar de nuevo.</summary>
    public float ProgresoAtaqueArea { get { return cooldownArea <= 0f ? 1f : Mathf.Clamp01(1f - (tiempoProximoAtaqueArea - Time.time) / cooldownArea); } }
    public bool Atacando { get { return atacando; } }

    // ---------- interno ----------
    private ChoferController chofer;
    private CharacterController controller;
    private Animator animator;
    private Transform modelo;
    private Vector3 modeloPosBase;
    private Quaternion modeloRotBase;
>>>>>>> Stashed changes

    private int comboPaso = 0;
    private float tiempoUltimoGolpe = 0f;
    private bool atacando = false;
    private HitboxAtaque scriptHitbox;

    void Start()
    {
        if (hitboxMele != null)
        {
            scriptHitbox = hitboxMele.GetComponent<HitboxAtaque>();
            hitboxMele.SetActive(false);
        }
    }

    void Update()
    {
        // Si pasa demasiado tiempo sin golpear, se reinicia el combo al paso 1
        if (comboPaso > 0 && Time.time - tiempoUltimoGolpe > tiempoVentanaCombo)
        {
            comboPaso = 0;
        }

        // Ataque básico con click izquierdo o tecla F / J
        if ((Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.F)) && !atacando)
        {
            StartCoroutine(EjecutarGolpeCombo());
        }

        // Ataque de área
        if (Input.GetKeyDown(teclaAtaqueArea) && Time.time >= tiempoProximoAtaqueArea && !atacando)
        {
            EjecutarAtaqueArea();
        }
    }

    IEnumerator EjecutarGolpeCombo()
    {
        atacando = true;
        comboPaso++;
        if (comboPaso > 3) comboPaso = 1;

        float danioActual = danioAtaque1;
        if (comboPaso == 2) danioActual = danioAtaque2;
        else if (comboPaso == 3) danioActual = danioAtaque3;

        if (scriptHitbox != null)
        {
            scriptHitbox.danio = danioActual;
            scriptHitbox.tagObjetivo = "Enemigo";
        }

        // Activa la hitbox
        if (hitboxMele != null) hitboxMele.SetActive(true);
        tiempoUltimoGolpe = Time.time;
        Debug.Log($"[Chofer] Ejecutando Golpe {comboPaso} con {danioActual} de daño espiritual.");

        yield return new WaitForSeconds(duracionGolpe);

        // Desactiva la hitbox
        if (hitboxMele != null) hitboxMele.SetActive(false);

        yield return new WaitForSeconds(cooldownEntreGolpes);
        atacando = false;
    }

    void EjecutarAtaqueArea()
    {
        tiempoProximoAtaqueArea = Time.time + cooldownArea;
        Debug.Log("[Chofer] ¡Onda de Energía Espiritual liberada en área!");

        Collider[] golpeados = Physics.OverlapSphere(transform.position, radioArea);
        foreach (Collider col in golpeados)
        {
            if (col.CompareTag("Enemigo"))
            {
                PoderEspiritual vida = col.GetComponent<PoderEspiritual>();
                if (vida != null)
                {
                    vida.RecibirDanio(danioArea);
                    Debug.Log($"Impacto de área en {col.name} causando {danioArea} de daño.");
                }
<<<<<<< Updated upstream
=======
                golpeados.Clear();
                Debug.Log("[Chofer] ¡Onda de Energía Espiritual liberada en área!");
            }
            yield return null;
        }

        if (chofer != null) chofer.multiplicadorVelocidad = 1f;
        yield return new WaitForSeconds(0.1f);
        atacando = false;
    }

    IEnumerator AnimarAnilloArea()
    {
        if (anilloArea == null) yield break;
        anilloArea.gameObject.SetActive(true);
        float dur = 0.45f, t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = t / dur;
            float radio = Mathf.Lerp(0.5f, radioArea, 1f - (1f - k) * (1f - k));
            anilloArea.transform.localScale = new Vector3(radio, 1f, radio);
            anilloArea.widthMultiplier = Mathf.Lerp(0.45f, 0.06f, k);
            Color c = colorEspiritual; c.a = 1f - k;
            anilloArea.startColor = c; anilloArea.endColor = c;
            yield return null;
        }
        anilloArea.gameObject.SetActive(false);
    }

    // ============================================================ ANIMACIÓN PROCEDURAL / CÁMARA

    void LateUpdate()
    {
        if (modelo != null)
        {
            modeloPosBase = new Vector3(0f, -1f, 0f);
            modeloRotBase = Quaternion.identity;

            // Relajar la pose cuando no se ataca
            if (!atacando)
            {
                poseRot = Vector3.Lerp(poseRot, Vector3.zero, 1f - Mathf.Exp(-14f * Time.deltaTime));
                poseAdelante = Mathf.Lerp(poseAdelante, 0f, 1f - Mathf.Exp(-14f * Time.deltaTime));
            }

            Quaternion rot = modeloRotBase * Quaternion.Euler(poseRot);
            Vector3 pos = modeloPosBase + Vector3.forward * poseAdelante;

            // Rodada del esquive (procedural, cuando no hay clip de esquive): gira el cuerpo alrededor de un punto bajo
            if (chofer != null && chofer.rodando)
            {
                Vector3 c = chofer.centroRodada;
                pos = c + chofer.rotacionRodada * (pos - c);
                rot = chofer.rotacionRodada * rot;
            }

            modelo.localRotation = rot;
            modelo.localPosition = pos;
        }

        // Golpe de FOV (se amortigua solo)
        if (camara != null)
        {
            if (golpeFov > 0.01f)
            {
                golpeFov = Mathf.MoveTowards(golpeFov, 0f, 22f * Time.deltaTime);
                camara.fovExtra = golpeFov;
                golpeFovActivo = true;
            }
            else if (golpeFovActivo)
            {
                camara.fovExtra = 0f;
                golpeFovActivo = false;
>>>>>>> Stashed changes
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        // Visualizar el radio del ataque de área en el editor de Unity
        Gizmos.color = new Color(0f, 1f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, radioArea);
    }
}
