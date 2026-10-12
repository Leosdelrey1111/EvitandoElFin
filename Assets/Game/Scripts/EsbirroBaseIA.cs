using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PoderEspiritual))]
public class EsbirroBaseIA : MonoBehaviour
{
    public enum TipoEsbirro { Serpiente, Momia, HombrePiedra, Monje, Cuervo, Catrin }

    [Header("Identidad del Enemigo")]
    public TipoEsbirro tipo = TipoEsbirro.Serpiente;

    [Header("Movimiento e IA")]
    public float velocidadPersecucion = 3.2f;
    public float rangoDeteccion = 9f;
    public float rangoAtaque = 1.6f;
    public float gravedad = -15f;

    [Header("Combate")]
    public float danio = 15f;
    public float cooldownAtaque = 1.8f;
    public GameObject hitboxAtaqueEnemigo;

    [Header("Recompensa al Morir (Regla 2 GDD)")]
    public GameObject prefabTotemDrop;
    public int cantidadTotemsADropear = 2;

    private Transform playerTransform;
    private CharacterController controller;
    private PoderEspiritual vida;
    private Vector3 velocidadVertical;
    private float tiempoProximoAtaque = 0f;
    private bool estaMuerto = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        vida = GetComponent<PoderEspiritual>();

        // Suscribirse al evento de muerte
        vida.OnMuerte += Morir;

        // Localizar al jugador por Tag
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }

        if (hitboxAtaqueEnemigo != null)
        {
            HitboxAtaque hitbox = hitboxAtaqueEnemigo.GetComponent<HitboxAtaque>();
            if (hitbox != null)
            {
                hitbox.danio = danio;
                hitbox.tagObjetivo = "Player";
            }
            hitboxAtaqueEnemigo.SetActive(false);
        }
    }

    void Update()
    {
        if (estaMuerto) return;

        AplicarGravedad();

        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
            return;
        }

        float distanciaAlPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanciaAlPlayer <= rangoDeteccion)
        {
            // Mirar hacia el jugador suavemente
            Vector3 direccion = (playerTransform.position - transform.position);
            direccion.y = 0;
            if (direccion.magnitude > 0.1f)
            {
                Quaternion rotacion = Quaternion.LookRotation(direccion.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, rotacion, 8f * Time.deltaTime);
            }

            // Perseguir si está fuera de rango de ataque
            if (distanciaAlPlayer > rangoAtaque)
            {
                controller.Move(direccion.normalized * velocidadPersecucion * Time.deltaTime);
            }
            else
            {
                // Dentro de rango de ataque
                if (Time.time >= tiempoProximoAtaque)
                {
                    StartCoroutine(RutinaAtaque());
                }
            }
        }
    }

    IEnumerator RutinaAtaque()
    {
        tiempoProximoAtaque = Time.time + cooldownAtaque;

        Debug.Log($"[{gameObject.name} ({tipo})] Atacando al Chofer!");
        if (hitboxAtaqueEnemigo != null) hitboxAtaqueEnemigo.SetActive(true);

        yield return new WaitForSeconds(0.3f);

        if (hitboxAtaqueEnemigo != null) hitboxAtaqueEnemigo.SetActive(false);
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

    void Morir()
    {
        if (estaMuerto) return;
        estaMuerto = true;
        Debug.Log($"[{gameObject.name}] Esbirro derrotado.");

        // Dropear tótems de recompensa según el GDD
        if (prefabTotemDrop != null)
        {
            for (int i = 0; i < cantidadTotemsADropear; i++)
            {
                Vector3 dispersion = new Vector3(Random.Range(-0.5f, 0.5f), 0.5f, Random.Range(-0.5f, 0.5f));
                Instantiate(prefabTotemDrop, transform.position + dispersion, Quaternion.identity);
            }
        }
        else if (InventarioEspiritual.Instancia != null)
        {
            // Si no se asignó prefab, se suma directamente al inventario
            InventarioEspiritual.Instancia.AgregarTotems(cantidadTotemsADropear);
        }

        Destroy(gameObject, 0.2f);
    }

    void OnDestroy()
    {
        if (vida != null)
        {
            vida.OnMuerte -= Morir;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangoAtaque);
    }
}
