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
<<<<<<< Updated upstream
=======
    [Tooltip("Tiempo de aviso antes del golpe (el jugador puede esquivar).")]
    public float tiempoTelegrafiado = 0.55f;
    public Color colorTelegrafiado = new Color(1f, 0.25f, 0.2f);
    [Tooltip("Obsoleto: el daño ahora se aplica por distancia. Se conserva por compatibilidad.")]
>>>>>>> Stashed changes
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

<<<<<<< Updated upstream
        Destroy(gameObject, 0.2f);
=======
        if (OnEsbirroDerrotado != null) OnEsbirroDerrotado(this);
        StartCoroutine(AnimacionMuerte());
    }

    IEnumerator AnimacionMuerte()
    {
        // Se desvanece hundiéndose y encogiéndose
        float dur = 0.7f, t = 0f;
        Vector3 escalaInicial = transform.localScale;
        Vector3 posInicial = transform.position;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = t / dur;
            transform.localScale = escalaInicial * Mathf.Lerp(1f, 0.05f, k * k);
            transform.position = posInicial + Vector3.down * (k * 0.6f);
            transform.Rotate(0f, 360f * Time.deltaTime, 0f);
            yield return null;
        }
        Destroy(gameObject);
    }

    // ============================================================ VISUAL / BARRA DE VIDA

    void LateUpdate()
    {
        if (estado == Estado.Muerto) return;

        AplicarTinte();
        AnimarVisualProcedural();
        ActualizarBarraVida();
    }

    void AplicarTinte()
    {
        if (renderers == null) return;
        bool flash = Time.time < flashHasta;
        if (!flash && !telegrafiando && !mpbSucio) return;

        Color c = Color.white;
        bool usar = false;
        if (flash) { c = new Color(1f, 1f, 1f, 1f); usar = true; }
        else if (telegrafiando)
        {
            float p = 0.5f + 0.5f * Mathf.Sin(Time.time * 28f);
            c = Color.Lerp(colorTelegrafiado, Color.white, p * 0.35f);
            usar = true;
        }

        foreach (Renderer r in renderers)
        {
            if (r == null || r is TrailRenderer || r is ParticleSystemRenderer || r is LineRenderer) continue;
            if (usar)
            {
                r.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", c);
                mpb.SetColor("_Color", c);
                r.SetPropertyBlock(mpb);
            }
            else
            {
                r.SetPropertyBlock(null); // vuelve al color original del material
            }
        }
        mpbSucio = usar;
    }
    private bool mpbSucio = false;

    void AnimarVisualProcedural()
    {
        if (visual == null) return;

        faseAnim += Time.deltaTime * (seMovio ? 9f : 2.5f);

        Vector3 pos = visualPosBase;
        Quaternion rot = visualRotBase;
        Vector3 esc = visualEscalaBase;

        if (tipo == TipoEsbirro.Serpiente)
        {
            // ondulación lateral
            rot = visualRotBase * Quaternion.Euler(0f, Mathf.Sin(faseAnim) * (seMovio ? 14f : 4f), 0f);
        }
        else
        {
            // pisadas pesadas
            pos += Vector3.up * Mathf.Abs(Mathf.Sin(faseAnim * 0.5f)) * (seMovio ? 0.10f : 0.02f);
            rot = visualRotBase * Quaternion.Euler(0f, 0f, Mathf.Sin(faseAnim * 0.5f) * (seMovio ? 3f : 0.6f));
        }

        if (telegrafiando)
        {
            // se agazapa y crece antes de golpear
            esc = Vector3.Scale(visualEscalaBase, new Vector3(1.12f, 0.88f, 1.12f));
            pos += transform.forward * -0.12f;
        }

        visual.localPosition = pos;
        visual.localRotation = rot;
        visual.localScale = esc;
    }

    void CrearBarraVida()
    {
        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) return;

        GameObject raiz = new GameObject("BarraVida");
        raiz.transform.SetParent(transform, false);
        raiz.transform.localPosition = new Vector3(0f, alturaBarraVida, 0f);
        barraRaiz = raiz.transform;

        GameObject fondo = GameObject.CreatePrimitive(PrimitiveType.Quad);
        fondo.name = "Fondo";
        fondo.transform.SetParent(raiz.transform, false);
        fondo.transform.localScale = new Vector3(1.1f, 0.12f, 1f);
        Destroy(fondo.GetComponent<Collider>());
        Material mFondo = new Material(sh); mFondo.color = new Color(0f, 0f, 0f, 0.7f);
        fondo.GetComponent<Renderer>().sharedMaterial = mFondo;
        fondo.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        GameObject relleno = GameObject.CreatePrimitive(PrimitiveType.Quad);
        relleno.name = "Relleno";
        relleno.transform.SetParent(raiz.transform, false);
        relleno.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        Destroy(relleno.GetComponent<Collider>());
        Material mRel = new Material(sh); mRel.color = new Color(0.95f, 0.25f, 0.25f, 1f);
        relleno.GetComponent<Renderer>().sharedMaterial = mRel;
        relleno.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        barraRelleno = relleno.transform;

        raiz.SetActive(false);
    }

    void ActualizarBarraVida()
    {
        if (barraRaiz == null) return;

        bool visible = Time.time < barraVisibleHasta && vida != null && !vida.EstaMuerto;
        if (barraRaiz.gameObject.activeSelf != visible) barraRaiz.gameObject.SetActive(visible);
        if (!visible) return;

        float p = vida.Porcentaje;
        const float ancho = 1.04f;
        barraRelleno.localScale = new Vector3(Mathf.Max(0.001f, ancho * p), 0.08f, 1f);
        barraRelleno.localPosition = new Vector3(-(ancho * (1f - p)) * 0.5f, 0f, -0.01f);

        // billboard hacia la cámara (compensando la escala del enemigo)
        if (Camera.main != null)
        {
            barraRaiz.rotation = Quaternion.LookRotation(barraRaiz.position - Camera.main.transform.position);
        }
>>>>>>> Stashed changes
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
