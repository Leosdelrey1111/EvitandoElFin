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
    private float tiempoProximoAtaqueArea = 0f;

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
